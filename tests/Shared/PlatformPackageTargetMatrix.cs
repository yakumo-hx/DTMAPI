using System;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;

namespace DTMAPI.Testing
{
    internal static class PlatformPackageTargetMatrix
    {
        public static void Run(string root, Action<string, string, bool> inspectPackage)
        {
            string content = Path.Combine(root, "Content", "DTMAPI");
            Directory.CreateDirectory(content);
            string manifestPath = Path.Combine(content, "manifest.json");
            string markerPath = Path.Combine(content, "dtmapi-package.json");
            var manifest = new JsonObject
            {
                ["Name"] = "Target Fixture", ["UniqueID"] = "DTMAPI.Tests.Target", ["Version"] = "1.0.0",
                ["Author"] = "DTMAPI", ["Type"] = "ContentPack", ["MinimumDTMApiVersion"] = "0.7.5",
                ["DependencyContractVersion"] = 1, ["Dependencies"] = new JsonArray()
            };
            File.WriteAllText(manifestPath, manifest.ToJsonString());
            inspectPackage(root, "handwritten-content", true);
            foreach (string stale in new[] { "", "{", "\uFEFF", "{\"authorSdkVersion\":\"unknown\",\"manifestSha256\":\"obsolete\",\"uniqueId\":\"Wrong.Owner\"}" })
            {
                byte[] bytes = Encoding.UTF8.GetBytes(stale);
                File.WriteAllBytes(markerPath, bytes);
                inspectPackage(root, "obsolete-marker-ignored", true);
                if (!File.ReadAllBytes(markerPath).AsSpan().SequenceEqual(bytes))
                    throw new InvalidOperationException("Inspection must not rewrite obsolete metadata.");
            }
            File.WriteAllText(Path.Combine(root, "dtmapi-dependencies.json"), "obsolete dependency copy");
            manifest["Name"] = "Edited without an SDK";
            File.WriteAllText(manifestPath, manifest.ToJsonString());
            File.WriteAllText(Path.Combine(content, "data.json"), "{\"value\":2}");
            inspectPackage(root, "editable-content", true);
            string code = Path.Combine(content, "hidden.dll");
            File.WriteAllBytes(code, new byte[] { 1, 2, 3 });
            inspectPackage(root, "content-cannot-carry-code", false);
            File.Delete(code);
        }
    }
}
