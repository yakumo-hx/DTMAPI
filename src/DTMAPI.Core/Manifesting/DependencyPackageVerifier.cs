using System;
using System.IO;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.Core.Manifesting
{
    internal static class DependencyPackageVerifier
    {
        public static PackageAssemblyRecord Inspect(byte[] bytes)
        {
            PortableAssemblyMetadata value = PortableAssemblyReferenceInspector.Inspect(bytes);
            return new PackageAssemblyRecord
            {
                Identity = value.Identity, References = value.ReferenceIdentities, TargetFramework = value.TargetFramework,
                DefinedTypes = value.DefinedTypes,
                Length = bytes.LongLength, Sha256 = PackageDependencyContract.ComputeHash(bytes)
            };
        }

        public static PackageDependencyBundle Read(ManifestModel manifest, string root, string manifestPath)
        {
            if (!manifest.CodeModKindWasDeclared && manifest.Type != "ContentPack") throw new InvalidDataException("dependency-package-kind-required: New packages must explicitly select Strict or Advanced.");
            string entry = manifest.Type == "ContentPack" ? "" : Path.GetFullPath(Path.Combine(root, manifest.EntryDll));
            var bundle = PackageDependencyBundle.Read(root, manifestPath, manifest.UniqueID, manifest.Version, manifest.Type,
                manifest.CodeModKind, entry, manifest.MinimumDTMApiVersion);
            PackageDependencyVerifier.Verify(root, bundle.Inventory, Inspect, identity => PackageHostReferences.IsStrictHostReference(identity) || (bundle.Native?.AllowsReference(identity) ?? false),
                PackageHostReferences.ReservedFor(bundle.Inventory.Assemblies), bundle.Files, bundle.Native);
            return bundle;
        }
    }
}
