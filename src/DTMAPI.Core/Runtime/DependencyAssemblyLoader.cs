using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Core.Manifesting;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.Core.Runtime
{
    internal sealed class DependencyAssemblyLoader : IDisposable
    {
        private readonly Dictionary<string, RuntimePackage> packages = new Dictionary<string, RuntimePackage>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PackagePlanInput> nodes = new Dictionary<string, PackagePlanInput>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Assembly, HashSet<string>> loaded = new Dictionary<Assembly, HashSet<string>>();
        private readonly Action<string> log;
        private readonly HashSet<string> hostNames;
        private readonly object sync = new object();
        public PackageDependencyPlan Plan { get; private set; } = null!;

        public DependencyAssemblyLoader(IEnumerable<DiscoveredMod> selected, IEnumerable<KeyValuePair<string, string>> processProviders,
            Action<string> log, string gamePath, StartupPackageReadTransaction? packageReads = null)
        {
            this.log = log;
            var hostDirectories = new[] {
                Path.Combine(gamePath, "DolocTown_Data", "Managed"), Path.Combine(gamePath, "BepInEx", "core"),
                Path.GetDirectoryName(typeof(object).Assembly.Location)!
            }.Concat(RuntimeComponentFiles.Components.Values.Select(folder => Path.Combine(gamePath, "DTMAPI", "components", folder))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            hostNames = new HashSet<string>(new[] { "DTMAPI.Abstractions", "DTMAPI.Core", "DTMAPI.GameBridge.DolocTown", "DTMAPI.BepInExBootstrap", "DTMAPI.ModConfigMenu" }, StringComparer.OrdinalIgnoreCase);
            foreach (string directory in hostDirectories.Where(Directory.Exists))
                foreach (string file in Directory.EnumerateFiles(directory, "*.dll")) hostNames.Add(Path.GetFileNameWithoutExtension(file));
            UpdatePackages(selected, processProviders, packageReads);
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            AppDomain.CurrentDomain.AssemblyLoad += ObserveLoad;
        }

        internal void UpdatePackages(IEnumerable<DiscoveredMod> selected, IEnumerable<KeyValuePair<string, string>> processProviders,
            StartupPackageReadTransaction? packageReads = null)
        {
            var providers = processProviders.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            var updatedNodes = new Dictionary<string, PackagePlanInput>(StringComparer.OrdinalIgnoreCase);
            var updatedPackages = new Dictionary<string, RuntimePackage>(StringComparer.OrdinalIgnoreCase);
            foreach (var mod in selected.Where(m => m.OfficialEnabled && !providers.ContainsKey(m.Manifest.UniqueID)))
            {
                var node = new PackagePlanInput { Id = mod.Manifest.UniqueID, OfficialPriority = mod.OfficialPriority < 0 ? (int?)null : mod.OfficialPriority,
                    Dependencies = mod.Manifest.DependencyContract?.Dependencies ?? mod.Manifest.DependencyModels.Select(d =>
                        new PackageModDependency(d.UniqueID, d.Required, new PackageVersionRange(string.IsNullOrEmpty(d.MinimumVersion) ? "0.0.0" : d.MinimumVersion, null, true))
                        { LegacyOrderingOnly = string.IsNullOrEmpty(d.MinimumVersion) }).ToArray() };
                node.Version = mod.Manifest.Version;
                updatedNodes.Add(node.Id, node);
                try
                {
                    RuntimePackage? resident = null;
                    lock (sync)
                        if (loaded.Values.Any(owners => owners.Contains(node.Id))) packages.TryGetValue(node.Id, out resident);
                    updatedPackages.Add(node.Id, resident ?? (packageReads != null ? packageReads.Read(mod.Manifest, mod.RootPath, mod.ManifestPath) : DependencyPackageVerifier.Read(mod.Manifest, mod.RootPath, mod.ManifestPath)));
                }
                catch (Exception ex) { node.PreflightFailure = ex.Message; }
            }
            foreach (var provider in providers)
            {
                if (updatedNodes.ContainsKey(provider.Key)) continue;
                var node = new PackagePlanInput { Id = provider.Key, IsProcessProvider = true };
                node.Version = provider.Value;
                updatedNodes.Add(node.Id, node);
            }
            var plan = PackageDependencyPlanner.Plan(updatedNodes.Values);
            lock (sync)
            {
                // Keep packages already used by CLR bindings, including failed entries.
                foreach (var pair in packages.Where(pair => loaded.Values.Any(owners => owners.Contains(pair.Key))))
                    if (!updatedPackages.ContainsKey(pair.Key)) updatedPackages.Add(pair.Key, pair.Value);
                packages.Clear();
                foreach (var pair in updatedPackages) packages.Add(pair.Key, pair.Value);
                nodes.Clear();
                foreach (var pair in updatedNodes) nodes.Add(pair.Key, pair.Value);
                Plan = plan;
            }
            foreach (var item in Plan.Blocked) log("DependencyPlan blocked owner=" + item.Key + "; " + item.Value);
            foreach (string warning in Plan.Warnings) log("DependencyPlan " + warning);
        }

        private string? FindCandidate(string requester, string name)
        {
            if (packages.TryGetValue(requester, out var own) && own.HasCandidate(name)) return requester;
            if (!nodes.TryGetValue(requester, out var node)) return null;
            string[] providers = node.Dependencies.Where(d => !Plan.Blocked.ContainsKey(d.Id))
                .Select(d => d.Id).Where(id => packages.TryGetValue(id, out var provider) && provider.HasCandidate(name, sharedOnly: true)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (providers.Length > 1) throw new InvalidDataException("dependency-provider-ambiguous: " + requester + " -> " + name + "; " + string.Join(", ", providers));
            return providers.SingleOrDefault();
        }

        private static string Location(Assembly assembly) => assembly.IsDynamic ? "" : assembly.Location;

        private bool IsHost(string name) => hostNames.Contains(name);

        public Assembly LoadEntry(string owner, string path)
        {
            var package = packages[owner];
            var candidate = package.SelectEntry();
            if (!Path.GetFullPath(path).Equals(PackageDependencyContract.ResolveFile(package.Root, candidate.Path), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("dependency-entry-path-changed");
            return Bind(owner, candidate);
        }

        private Assembly? Resolve(object? sender, ResolveEventArgs args) => ResolveRequest(new AssemblyName(args.Name), args.RequestingAssembly);

        internal Assembly? ResolveRequest(AssemblyName request, Assembly? requestingAssembly)
        {
            if (request.Name == null || IsHost(request.Name)) return null;
            Assembly? caller = requestingAssembly;
            string? owner = FindOwner(caller);
            if (owner == null) { caller = FindCallingAssembly(); owner = FindOwner(caller); }
            if (owner == null) return null;
            string? provider;
            RuntimePackage.Candidate candidate;
            string? callerPath = caller == null ? null : Location(caller);
            lock (sync)
            {
                provider = FindCandidate(owner, request.Name);
                if (provider == null) return null;
                candidate = packages[provider].Select(request.Name, sharedOnly: !provider.Equals(owner, StringComparison.OrdinalIgnoreCase), requesterPath: callerPath);
            }
            Assembly assembly = Bind(provider, candidate);
            RecordBinding(owner, assembly);
            return assembly;
        }

        private Assembly? FindCallingAssembly()
        {
            // Unity Mono can omit RequestingAssembly for Assembly.Load(string).
            foreach (var frame in new System.Diagnostics.StackTrace(false).GetFrames() ?? Array.Empty<System.Diagnostics.StackFrame>())
            {
                Assembly? assembly = frame.GetMethod()?.DeclaringType?.Assembly;
                if (FindOwner(assembly) != null) return assembly;
            }
            return null;
        }

        private string? FindOwner(Assembly? assembly)
        {
            if (assembly == null) return null;
            lock (sync)
                if (loaded.TryGetValue(assembly, out var boundOwners)) return boundOwners.Count == 1 ? boundOwners.Single() : null;
            string location = Location(assembly);
            if (location.Length == 0) return null;
            lock (sync)
            {
                string[] owners = packages.Where(pair => location.StartsWith(pair.Value.Root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Key).ToArray();
                return owners.Length == 1 ? owners[0] : null;
            }
        }

        private void ObserveLoad(object? sender, AssemblyLoadEventArgs args)
        {
            Assembly assembly = args.LoadedAssembly;
            if (IsHost(assembly.GetName().Name ?? "")) return;
            string? owner = FindOwner(assembly) ?? FindOwner(FindCallingAssembly());
            if (owner != null) RecordBinding(owner, assembly);
        }

        internal void RecordBinding(string owner, Assembly assembly)
        {
            lock (sync)
            {
                if (!loaded.TryGetValue(assembly, out var owners))
                    loaded.Add(assembly, owners = new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                owners.Add(owner);
            }
        }

        internal Dictionary<Assembly, string[]> GetBindings()
        {
            lock (sync) return loaded.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
        }

        private Assembly Bind(string owner, RuntimePackage.Candidate candidate)
        {
            RuntimePackage package;
            lock (sync)
            {
                if (Plan.Blocked.TryGetValue(owner, out var failure)) throw new InvalidDataException(failure);
                package = packages[owner];
            }
            var record = package.ReadCandidate(candidate);
            lock (sync) package.RememberName(record);
            // CLR loading can reenter the resolver or run on another thread.
            // The table lock never spans loading, logging or Mod code.
            Assembly assembly = Assembly.LoadFrom(package.Resolve(record));
            RecordBinding(owner, assembly);
            log("DependencyAssembly bound name=" + assembly.GetName().Name + "; owner=" + owner + "; path=" + Location(assembly));
            return assembly;
        }

        internal Assembly LoadDeclaredAssembly(string owner, string name)
        {
            RuntimePackage.Candidate candidate;
            lock (sync)
            {
                if (!packages.TryGetValue(owner, out var package) || !package.HasCandidate(name)) throw new InvalidDataException("module-assembly-missing: " + name);
                candidate = package.Select(name);
            }
            return Bind(owner, candidate);
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= Resolve;
            AppDomain.CurrentDomain.AssemblyLoad -= ObserveLoad;
        }
    }
}
