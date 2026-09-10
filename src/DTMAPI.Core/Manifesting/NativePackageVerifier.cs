using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.Core.Manifesting
{
    internal static class NativePackageVerifier
    {
        private sealed class HostEvidence { internal string Sha256 = ""; internal string PreloaderSourcePath = ""; }
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Assembly, HostEvidence> ObservedHosts = new System.Runtime.CompilerServices.ConditionalWeakTable<Assembly, HostEvidence>();
        // Bootstrap calls this before managed Mod discovery, using the existing loader's
        // origin map. This binds the original file, not the post-patch executable bytes.
        internal static void ObservePreloaderOrigin(Assembly assembly, string sourcePath)
        {
            if (assembly.IsDynamic || !string.IsNullOrEmpty(assembly.Location))
                throw new InvalidDataException("native-preloader-origin-invalid: Expected an in-memory host assembly.");
            string path = Path.GetFullPath(sourcePath);
            byte[] bytes = File.ReadAllBytes(path);
            var metadata = PortableAssemblyReferenceInspector.Inspect(bytes);
            if (NativePackageContract.IdentityString(DependencyPackageVerifier.Inspect(bytes).Identity) != assembly.FullName ||
                !Guid.TryParse(metadata.ModuleMvid, out Guid mvid) || assembly.ManifestModule.ModuleVersionId != mvid)
                throw new InvalidDataException("native-preloader-origin-invalid: Original identity/MVID differs from the resident assembly.");
            string hash = PackageDependencyContract.ComputeHash(bytes);
            lock (ObservedHosts)
            {
                if (ObservedHosts.TryGetValue(assembly, out HostEvidence? previous))
                {
                    if (previous.Sha256 != hash || !string.Equals(previous.PreloaderSourcePath, path, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("native-host-resident-conflict: Preloader origin changed; restart required.");
                }
                else ObservedHosts.Add(assembly, new HostEvidence { Sha256 = hash, PreloaderSourcePath = path });
            }
        }

        internal static IReadOnlyList<string> VerifyInstalled(NativePackageContract contract, string gameRoot)
        {
            var hosts = new Dictionary<string, Assembly>(StringComparer.Ordinal);
            var preloaderWarnings = new HashSet<string>(StringComparer.Ordinal);
            var warnings = contract.VerifyHost(gameRoot,
                bytes => NativePackageContract.IdentityString(DependencyPackageVerifier.Inspect(bytes).Identity),
                (bytes, member) =>
                {
                    if (!hosts.TryGetValue(member.AssemblyIdentity, out Assembly? assembly))
                    {
                        NativeHostReference reference = contract.References.Single(item => item.AssemblyIdentity == member.AssemblyIdentity);
                        string path = PackageDependencyContract.ResolveFile(gameRoot, reference.GameRelativePath);
                        Assembly[] resident = AppDomain.CurrentDomain.GetAssemblies().Where(item => item.GetName().FullName == member.AssemblyIdentity).ToArray();
                        if (resident.Length > 1) throw new InvalidDataException("native-host-resident-conflict: " + member.AssemblyIdentity);
                        assembly = resident.Length == 1 ? resident[0] : Assembly.LoadFrom(path);
                        string hash = PackageDependencyContract.ComputeHash(bytes);
                        string location = assembly.IsDynamic ? "" : assembly.Location;
                        if (!assembly.IsDynamic && string.IsNullOrEmpty(location))
                        {
                            lock (ObservedHosts)
                            {
                                if (!ObservedHosts.TryGetValue(assembly, out HostEvidence? origin) || string.IsNullOrEmpty(origin.PreloaderSourcePath) || origin.Sha256 != hash)
                                    throw new InvalidDataException("native-host-origin-unavailable: In-memory host has no unchanged Bootstrap/preloader origin; restart with the supported loader: " + member.AssemblyIdentity);
                                location = origin.PreloaderSourcePath;
                            }
                            preloaderWarnings.Add("native-host-preloader-patched: " + member.AssemblyIdentity + "; original file identity/hash/MVID bound; resident required signatures verified; transformed method bodies are loader-owned.");
                        }
                        if (assembly.IsDynamic || string.IsNullOrEmpty(location) || !string.Equals(Path.GetFullPath(location), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase) ||
                            PackageDependencyContract.ComputeHash(File.ReadAllBytes(location)) != hash ||
                            !Guid.TryParse(PortableAssemblyReferenceInspector.Inspect(bytes).ModuleMvid, out Guid mvid) || mvid != assembly.ManifestModule.ModuleVersionId)
                            throw new InvalidDataException("native-host-resident-conflict: Host origin changed; restart required: " + member.AssemblyIdentity);
                        lock (ObservedHosts)
                        {
                            if (ObservedHosts.TryGetValue(assembly, out HostEvidence? evidence))
                            { if (evidence.Sha256 != hash) throw new InvalidDataException("native-host-resident-conflict: Host bytes replaced after verification; restart required: " + member.AssemblyIdentity); }
                            else ObservedHosts.Add(assembly, new HostEvidence { Sha256 = hash });
                        }
                        hosts.Add(member.AssemblyIdentity, assembly);
                    }
                    return NativePackageContract.Contains(assembly, member);
                });
            return warnings.Concat(preloaderWarnings.OrderBy(item => item, StringComparer.Ordinal)).ToArray();
        }
    }
}
