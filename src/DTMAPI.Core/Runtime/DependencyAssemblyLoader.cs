using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Core.Manifesting;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.Core.Runtime
{
    internal sealed class DependencyLoadedAssembly
    {
        public Assembly Assembly { get; set; } = null!;
        public string Path { get; set; } = "";
        public PackageAssemblyIdentity Identity { get; set; } = null!;
        public HashSet<string> Owners { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed class DependencyAssemblyLoader : IDisposable
    {
        private readonly Dictionary<string, RuntimePackage> packages = new Dictionary<string, RuntimePackage>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PackagePlanInput> nodes = new Dictionary<string, PackagePlanInput>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<Assembly, DependencyLoadedAssembly> loaded;
        private readonly Action<string> log;
        private readonly Action<string, Assembly>? onBound;
        private readonly string[] hostDirectories;
        private readonly object sync = new object();
        public PackageDependencyPlan Plan { get; }

        public DependencyAssemblyLoader(IEnumerable<DiscoveredMod> selected, IEnumerable<KeyValuePair<string, string>> processProviders,
            IDictionary<Assembly, DependencyLoadedAssembly> loaded, Action<string> log, string gamePath,
            StartupPackageReadTransaction? packageReads = null,
            Action<string, Assembly>? onBound = null)
        {
            this.loaded = loaded; this.log = log; this.onBound = onBound;
            var providers = processProviders.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            hostDirectories = new[] {
                Path.Combine(gamePath, "DolocTown_Data", "Managed"), Path.Combine(gamePath, "BepInEx", "core"),
                Path.GetDirectoryName(typeof(object).Assembly.Location)!
            }.Concat(RuntimeComponentFiles.Components.Values.Select(folder => Path.Combine(gamePath, "DTMAPI", "components", folder))).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var mod in selected.Where(m => m.OfficialEnabled && !providers.ContainsKey(m.Manifest.UniqueID)))
            {
                var node = new PackagePlanInput { Id = mod.Manifest.UniqueID, OfficialPriority = mod.OfficialPriority < 0 ? (int?)null : mod.OfficialPriority,
                    Dependencies = mod.Manifest.DependencyContract?.Dependencies ?? mod.Manifest.DependencyModels.Select(d =>
                        new PackageModDependency(d.UniqueID, d.Required, new PackageVersionRange(string.IsNullOrEmpty(d.MinimumVersion) ? "0.0.0" : d.MinimumVersion, null, true))
                        { LegacyOrderingOnly = string.IsNullOrEmpty(d.MinimumVersion) }).ToArray() };
                node.Version = mod.Manifest.Version;
                nodes.Add(node.Id, node);
                try { packages.Add(node.Id, packageReads != null ? packageReads.Read(mod.Manifest, mod.RootPath, mod.ManifestPath) : DependencyPackageVerifier.Read(mod.Manifest, mod.RootPath, mod.ManifestPath)); }
                catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException) { node.PreflightFailure = ex.Message; }
            }
            foreach (var provider in providers)
            {
                if (nodes.ContainsKey(provider.Key)) continue;
                var node = new PackagePlanInput { Id = provider.Key, IsProcessProvider = true };
                node.Version = provider.Value;
                nodes.Add(node.Id, node);
            }
            Plan = PackageDependencyPlanner.Plan(nodes.Values);
            foreach (var item in Plan.Blocked) log("DependencyPlan blocked owner=" + item.Key + "; " + item.Value);
            foreach (string warning in Plan.Warnings) log("DependencyPlan " + warning);
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            AppDomain.CurrentDomain.AssemblyLoad += ObserveLoad;
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

        private bool IsHost(string name)
        {
            if (new[] { "DTMAPI.Abstractions", "DTMAPI.Core", "DTMAPI.GameBridge.DolocTown", "DTMAPI.BepInExBootstrap", "DTMAPI.ModConfigMenu" }.Contains(name, StringComparer.OrdinalIgnoreCase)) return true;
            return hostDirectories.Any(directory => File.Exists(Path.Combine(directory, name + ".dll")));
        }

        public Assembly LoadEntry(string owner, string path)
        {
            lock (sync)
            {
                var package = packages[owner];
                var record = package.SelectEntry();
                if (!Path.GetFullPath(path).Equals(package.Resolve(record), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("dependency-entry-path-changed");
                return Bind(owner, record);
            }
        }

        private Assembly? Resolve(object? sender, ResolveEventArgs args) => ResolveRequest(new AssemblyName(args.Name), args.RequestingAssembly);

        internal Assembly? ResolveRequest(AssemblyName request, Assembly? requestingAssembly)
        {
            if (request.Name == null || IsHost(request.Name)) return null;
            lock (sync)
            {
                Assembly? caller = requestingAssembly;
                string? owner = FindOwner(caller);
                if (owner == null) { caller = FindCallingAssembly(); owner = FindOwner(caller); }
                if (owner == null) return null;
                string? provider = FindCandidate(owner, request.Name);
                if (provider == null) return null;
                return Bind(provider, packages[provider].Select(request.Name, sharedOnly: !provider.Equals(owner, StringComparison.OrdinalIgnoreCase),
                    requesterPath: caller == null ? null : AssemblyLoadBinding.Location(caller)));
            }
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
            if (loaded.TryGetValue(assembly, out var binding)) return binding.Owners.Count == 1 ? binding.Owners.Single() : null;
            string location = AssemblyLoadBinding.Location(assembly);
            if (location.Length == 0) return null;
            string[] owners = packages.Where(pair => location.StartsWith(pair.Value.Root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Key).ToArray();
            return owners.Length == 1 ? owners[0] : null;
        }

        private void ObserveLoad(object? sender, AssemblyLoadEventArgs args)
        {
            lock (sync)
            {
                Assembly assembly = args.LoadedAssembly;
                if (IsHost(assembly.GetName().Name ?? "")) return;
                string? owner = FindOwner(assembly) ?? FindOwner(FindCallingAssembly());
                if (owner != null) Remember(owner, assembly);
            }
        }

        private void Remember(string owner, Assembly assembly)
        {
            if (!loaded.TryGetValue(assembly, out var binding))
                loaded.Add(assembly, binding = new DependencyLoadedAssembly { Assembly = assembly, Path = AssemblyLoadBinding.Location(assembly), Identity = AssemblyLoadBinding.Identity(assembly.GetName()) });
            binding.Owners.Add(owner);
            onBound?.Invoke(owner, assembly);
        }

        private Assembly Bind(string owner, PackageAssemblyRecord record)
        {
            if (Plan.Blocked.TryGetValue(owner, out var failure)) throw new InvalidDataException(failure);
            Assembly assembly = AssemblyLoadBinding.BindDependency(owner, packages[owner].Resolve(record), loaded, log);
            Remember(owner, assembly);
            return assembly;
        }


        internal Assembly LoadDeclaredAssembly(string owner, string name)
        {
            lock (sync)
            {
                if (!packages.TryGetValue(owner, out var package) || !package.HasCandidate(name)) throw new InvalidDataException("module-assembly-missing: " + name);
                return Bind(owner, package.Select(name));
            }
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= Resolve;
            AppDomain.CurrentDomain.AssemblyLoad -= ObserveLoad;
        }
    }
}
