using System;
using System.IO;
using DTMAPI.Core.Manifesting;
using DTMAPI.Testing;
namespace DTMAPI.UnitTests
{
    internal static class PlatformSdkTargetRuntimeTests
    {
        public static void RunAll()
        {
            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "runtime-without-sdk"); Directory.CreateDirectory(root);
            string path = Path.Combine(root, "manifest.json");
            foreach (string version in new[] { "0.6.4", "0.7.0", "0.7.5" })
            {
                File.WriteAllText(path, "{\"UniqueID\":\"Test.OldPackage\",\"Version\":\"1.0.0\",\"Type\":\"ContentPack\",\"MinimumDTMApiVersion\":\"" + version + "\",\"NativeContractVersion\":999,\"DependencyContractVersion\":999}");
                File.WriteAllText(Path.Combine(root, "dtmapi-package.json"), "broken generator receipt");
                var manifest = new ManifestReader().Read(path);
                if (!new ManagedModClassifier(root).Classify(manifest, root, path, "Local", false, null).IsContentPack)
                    throw new InvalidOperationException("Supported packages must not depend on an SDK target catalog or proof version.");
            }
        }
    }
}
