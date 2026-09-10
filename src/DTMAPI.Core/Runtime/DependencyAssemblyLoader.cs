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
        public string Sha256 { get; set; } = "";
        public PackageAssemblyIdentity Identity { get; set; } = null!;
    }

    internal sealed class DependencyAssemblyLoader : IDisposable
    {
        private readonly Dictionary<string, DiscoveredMod> mods;
        private readonly Dictionary<string, PackageDependencyBundle> bundles = new Dictionary<string, PackageDependencyBundle>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<string, DependencyLoadedAssembly> loaded;
        private readonly Action<string> log;
        private readonly string gamePath;
        private readonly object sync = new object();
        public PackageDependencyPlan Plan { get; }

        public DependencyAssemblyLoader(IEnumerable<DiscoveredMod> selected, IEnumerable<KeyValuePair<string, string>> processProviders,
            IDictionary<string, DependencyLoadedAssembly> loaded, Action<string> log, string gamePath)
        {
            this.loaded = loaded; this.log = log; this.gamePath = gamePath;
            mods = selected.Where(mod => mod.OfficialEnabled).ToDictionary(mod => mod.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase);
            var nodes = new List<PackagePlanInput>();
            foreach (DiscoveredMod mod in mods.Values.OrderBy(mod => mod.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase))
            {
                var node = new PackagePlanInput { Id = mod.Manifest.UniqueID };
                try { node.Version = PackageSemanticVersion.Parse(mod.Manifest.Version); } catch (InvalidDataException) { }
                if (mod.Manifest.DependencyContract != null) node.Dependencies = mod.Manifest.DependencyContract.Dependencies;
                else node.Dependencies = mod.Manifest.DependencyModels.Select(dependency => new PackageModDependency(dependency.UniqueID, dependency.Required, new PackageVersionRange("0.0.0", null, true)) { LegacyOrderingOnly = true }).ToArray();
                try
                {
                    if (mod.Manifest.DependencyContract != null)
                    {
                        var bundle = DependencyPackageVerifier.Read(mod.Manifest, mod.RootPath, mod.ManifestPath);
                        bundles.Add(node.Id, bundle); node.Assemblies = bundle.Inventory.Assemblies;
                    }
                    else if (mod.Classification.IsCodeMod)
                    {
                        string path = Path.GetFullPath(Path.Combine(mod.RootPath, mod.Manifest.EntryDll));
                        var record = DependencyPackageVerifier.Inspect(File.ReadAllBytes(path));
                        record.Role = "entry"; record.Path = mod.Manifest.EntryDll;
                        node.Assemblies = new[] { record };
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ManagedModClassificationException)
                { node.PreflightFailure = "dependency-preflight-failed: " + node.Id + ": " + ex.Message; }
                nodes.Add(node);
            }
            foreach (var provider in processProviders)
            {
                if (nodes.Any(node => string.Equals(node.Id, provider.Key, StringComparison.OrdinalIgnoreCase))) continue;
                var node = new PackagePlanInput { Id = provider.Key };
                try { node.Version = PackageSemanticVersion.Parse(provider.Value); } catch (InvalidDataException) { }
                nodes.Add(node);
            }
            var residents = new List<PackageResidentAssembly>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                AssemblyName name = assembly.GetName();
                loaded.TryGetValue(name.Name ?? "", out DependencyLoadedAssembly? evidence);
                residents.Add(new PackageResidentAssembly
                {
                    Identity = Identity(name), Sha256 = evidence != null && ReferenceEquals(evidence.Assembly, assembly) ? evidence.Sha256 : "",
                    ProvenHostOrPlanSource = evidence != null && ReferenceEquals(evidence.Assembly, assembly)
                });
            }
            Plan = PackageDependencyPlanner.Plan(nodes, PackageHostReferences.ReservedFor(nodes.SelectMany(node => node.Assemblies)), residents);
            foreach (var item in Plan.Blocked) log("DependencyPlan blocked owner=" + item.Key + "; " + item.Value);
            foreach (string warning in Plan.Warnings) log("DependencyPlan " + warning);
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        }

        public Assembly LoadEntry(string owner, string path)
        {
            if (Plan.Blocked.TryGetValue(owner, out string? failure)) throw new InvalidDataException(failure);
            var rechecked = DependencyPackageVerifier.Read(mods[owner].Manifest, mods[owner].RootPath, mods[owner].ManifestPath);
            if (rechecked.Native != null)
            {
                string nativePath = PackageDependencyContract.ResolveFile(mods[owner].RootPath, NativePackageContract.FileName);
                if (PackageDependencyContract.ComputeHash(File.ReadAllBytes(nativePath)) != mods[owner].Classification.ReferencePolicySha256)
                    throw new InvalidDataException("native-package-changed-after-discovery: " + owner);
                foreach (string warning in NativePackageVerifier.VerifyInstalled(rechecked.Native, gamePath)) log(warning);
            }
            var bundle = bundles[owner];
            // Bind the complete declared closure before constructing the entry type; no directory probing.
            foreach (var record in bundle.Inventory.Assemblies.Where(record => record.Role != "entry").OrderBy(record => record.Identity.Name, StringComparer.OrdinalIgnoreCase)) Bind(record.Identity.Name);
            var entry = bundle.Inventory.Assemblies.Single(record => record.Role == "entry");
            if (!string.Equals(Path.GetFullPath(path), PackageDependencyContract.ResolveFile(mods[owner].RootPath, entry.Path), StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("dependency-entry-path-changed");
            return Bind(entry.Identity.Name);
        }

        private Assembly? Resolve(object? sender, ResolveEventArgs args)
        {
            AssemblyName request = new AssemblyName(args.Name);
            if (request.Name == null || !Plan.AssemblyOwners.ContainsKey(request.Name)) return null;
            var result = Bind(request.Name);
            if (Identity(result.GetName()).Key != Identity(request).Key) throw new InvalidDataException("dependency-resolve-identity-mismatch: " + args.Name);
            return result;
        }

        private Assembly Bind(string name)
        {
            lock (sync)
            {
                if (!Plan.AssemblyOwners.TryGetValue(name, out string? owner) || !bundles.TryGetValue(owner, out PackageDependencyBundle? bundle)) throw new InvalidDataException("dependency-bind-not-in-plan: " + name);
                PackageAssemblyRecord record = bundle.Inventory.Assemblies.Single(item => item.Identity.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                string path = PackageDependencyContract.ResolveFile(mods[owner].RootPath, record.Path);
                if (PackageDependencyContract.ComputeHash(File.ReadAllBytes(path)) != record.Sha256) throw new InvalidDataException("dependency-file-changed-before-load: " + path);
                if (loaded.TryGetValue(name, out DependencyLoadedAssembly? known))
                {
                    if (known.Identity.Key != record.Identity.Key || known.Sha256 != record.Sha256) throw new InvalidDataException("resident-conflict/restart-required: " + name);
                    return known.Assembly;
                }
                if (AppDomain.CurrentDomain.GetAssemblies().Any(assembly => string.Equals(assembly.GetName().Name, name, StringComparison.OrdinalIgnoreCase))) throw new InvalidDataException("resident-conflict/restart-required: " + name);
                Assembly result = Assembly.LoadFrom(path);
                if (Identity(result.GetName()).Key != record.Identity.Key || !string.Equals(result.Location, path, StringComparison.OrdinalIgnoreCase)
                    || PackageDependencyContract.ComputeHash(File.ReadAllBytes(path)) != record.Sha256) throw new InvalidDataException("dependency-loaded-identity-changed/restart-required: " + name);
                loaded.Add(name, new DependencyLoadedAssembly { Assembly = result, Path = path, Sha256 = record.Sha256, Identity = record.Identity });
                log("DependencyAssembly bound name=" + name + "; owner=" + owner + "; sha256=" + record.Sha256 + "; path=" + path);
                return result;
            }
        }

        private static PackageAssemblyIdentity Identity(AssemblyName name) => new PackageAssemblyIdentity
        {
            Name = name.Name ?? "", AssemblyVersion = name.Version?.ToString() ?? "", Culture = name.CultureName ?? "",
            PublicKeyToken = BitConverter.ToString(name.GetPublicKeyToken() ?? Array.Empty<byte>()).Replace("-", "").ToLowerInvariant()
        };
        public void Dispose() => AppDomain.CurrentDomain.AssemblyResolve -= Resolve;
    }
}
