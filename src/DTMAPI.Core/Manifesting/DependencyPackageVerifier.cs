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
                Identity = value.Identity, References = value.ReferenceIdentities,
                TargetFramework = value.TargetFramework, DefinedTypes = value.DefinedTypes,
                Length = bytes.LongLength
            };
        }
        public static RuntimePackage Read(ManifestModel manifest, string root, string manifestPath) => new RuntimePackage(manifest, root);
    }
}
