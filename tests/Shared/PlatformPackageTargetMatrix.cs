using System;
using System.IO;
using System.Security.Cryptography;
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
            void inspect(string packageRoot, string name, bool accepted)
            {
                inspectPackage(packageRoot, name, accepted);
                if (!File.Exists(markerPath)) return;
                byte[] original = File.ReadAllBytes(markerPath);
                byte[] encoded = new byte[original.Length + 3];
                new byte[] { 0xEF, 0xBB, 0xBF }.CopyTo(encoded, 0);
                original.CopyTo(encoded, 3);
                File.WriteAllBytes(markerPath, encoded);
                inspectPackage(packageRoot, name + "-utf8-bom", accepted);
                if (!File.ReadAllBytes(markerPath).AsSpan().SequenceEqual(encoded))
                    throw new InvalidOperationException("Package inspection must preserve the original BOM bytes: " + name);
                File.WriteAllBytes(markerPath, original);
            }
            JsonObject Manifest(string floor) => new JsonObject
            {
                ["Name"] = "Target Fixture", ["UniqueID"] = "DTMAPI.Tests.Target", ["Version"] = "1.0.0",
                ["Author"] = "DTMAPI", ["Type"] = "ContentPack", ["MinimumDTMApiVersion"] = floor
            };
            JsonObject Marker() => new JsonObject
            {
                ["schemaVersion"] = 2, ["owner"] = "DTMAPI", ["uniqueId"] = "DTMAPI.Tests.Target", ["version"] = "1.0.0",
                ["packageKind"] = "ContentPack", ["codeModKind"] = "", ["authorSdkVersion"] = "0.1.0", ["targetDtmApiVersion"] = "0.5.5",
                ["manifestPath"] = "Content/DTMAPI/manifest.json", ["manifestSha256"] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(manifestPath))),
                ["entryDllPath"] = "", ["entryDllSha256"] = "", ["advancedReferenceReceiptPath"] = "", ["advancedReferenceReceiptSha256"] = "",
                ["authority"] = "dtmapi-author-sdk-package-binding"
            };
            void Check(string name, bool accepted, Action<JsonObject>? mutate = null, string floor = "0.5.5")
            {
                File.WriteAllText(manifestPath, Manifest(floor).ToJsonString());
                JsonObject marker = Marker();
                mutate?.Invoke(marker);
                File.WriteAllText(markerPath, marker.ToJsonString());
                inspect(root, name, accepted);
            }
            Check("current-schema2", true);
            Check("runtime-release-is-not-api-target", false, marker => marker["targetDtmApiVersion"] = "0.6.1");
            Check("internal-target", true, marker => { marker["targetDtmApiVersion"] = "0.6.2"; marker["authorSdkVersion"] = "0.6.2"; }, "0.6.2");
            Check("complete-m3-target", true, marker => { marker["targetDtmApiVersion"] = "0.7.0"; marker["authorSdkVersion"] = "0.7.0"; }, "0.7.0");
            Check("unknown-target", false, marker => { marker["targetDtmApiVersion"] = "0.8.0"; marker["authorSdkVersion"] = "0.2.0"; }, "0.8.0");
            Check("internal-old-sdk", false, marker => marker["targetDtmApiVersion"] = "0.6.2", "0.6.2");
            Check("internal-low-floor", false, marker => { marker["targetDtmApiVersion"] = "0.6.2"; marker["authorSdkVersion"] = "0.6.2"; }, "0.6.1");
            Check("unknown-sdk", false, marker => marker["authorSdkVersion"] = "9.0.0");
            Check("historically-issued-low-floor", true, floor: "0.5.4");
            Check("floor-too-high", false, floor: "0.7.0");
            Check("newer-supported-floor", true, floor: "0.6.1");
            Check("hash-rebound", false, marker => marker["manifestSha256"] = new string('0', 64));
            Check("path-rebound", false, marker => marker["manifestPath"] = "Content/DTMAPI/../DTMAPI/manifest.json");
            Check("identity-rebound", false, marker => marker["uniqueId"] = "Another.Owner");
            Check("extra-field", false, marker => marker["extra"] = true);
            Check("code-in-content", false, marker => marker["entryDllPath"] = "Content/DTMAPI/entry.dll");
            Check("restore", true);
            string valid = File.ReadAllText(markerPath);
            File.WriteAllText(markerPath, valid.Replace("\"schemaVersion\":2", "\"schemaVersion\":2,\"schemaVersion\":2", StringComparison.Ordinal));
            inspect(root, "duplicate-field", false);
            File.WriteAllText(markerPath, valid);
            File.AppendAllText(manifestPath, " ");
            inspect(root, "changed-manifest-bytes", false);
            File.WriteAllText(manifestPath, Manifest("0.5.5").ToJsonString());
            var legacy = new JsonObject
            {
                ["schemaVersion"] = 1, ["owner"] = "DTMAPI", ["uniqueId"] = "DTMAPI.Tests.Target", ["version"] = "1.0.0",
                ["packageKind"] = "ContentPack", ["authorSdkVersion"] = "0.1.0", ["targetRuntimeVersion"] = "0.5.5"
            };
            File.WriteAllText(markerPath, legacy.ToJsonString());
            inspect(root, "legacy-schema1", true);
            legacy["authority"] = "metadata-only-not-an-ownership-receipt";
            File.WriteAllText(markerPath, legacy.ToJsonString());
            inspect(root, "legacy-schema1-with-authority", true);
            File.WriteAllText(manifestPath, Manifest("0.5.4").ToJsonString());
            inspect(root, "legacy-schema1-low-floor", true);
            File.Delete(markerPath);
            inspect(root, "unmarked-compatible-content", true);
            var oldMetadata = new JsonObject
            {
                ["owner"] = "DTMAPI", ["uniqueId"] = "DTMAPI.Tests.Target",
                ["generatedBy"] = "tools/scripts/install-to-game.ps1", ["updatedAt"] = "2026-07-12T17:03:27.2248442+08:00"
            };
            void LegacyMetadata(string name, bool accepted, JsonObject marker)
            {
                File.WriteAllText(markerPath, marker.ToJsonString());
                inspect(root, name, accepted);
            }
            LegacyMetadata("pre-sdk-installer-metadata", true, oldMetadata);
            oldMetadata["packageKind"] = "workshop-mod";
            oldMetadata["generatedBy"] = "tools/scripts/build-release-workshop-packages.ps1";
            LegacyMetadata("pre-sdk-workshop-metadata", true, oldMetadata);
            oldMetadata["packageKind"] = "content-pack"; oldMetadata["contentRoot"] = "Content";
            oldMetadata["generatedBy"] = "manual-local-existing-assets";
            LegacyMetadata("pre-sdk-content-metadata", true, oldMetadata);
            oldMetadata.Remove("owner"); oldMetadata.Remove("packageKind"); oldMetadata.Remove("updatedAt");
            LegacyMetadata("pre-sdk-asset-generator-metadata", true, oldMetadata);
            var thirdPartyMetadata = new JsonObject { ["uniqueId"] = "DTMAPI.Tests.Target", ["version"] = "1.0.0" };
            LegacyMetadata("pre-sdk-third-party-metadata", true, thirdPartyMetadata);
            thirdPartyMetadata["version"] = "2.0.0";
            LegacyMetadata("pre-sdk-version-mismatch", false, thirdPartyMetadata);
            thirdPartyMetadata["version"] = "1.0.0"; thirdPartyMetadata["uniqueId"] = "Other.Owner";
            LegacyMetadata("pre-sdk-identity-mismatch", false, thirdPartyMetadata);
            oldMetadata["schemaVersion"] = 999;
            LegacyMetadata("legacy-fields-cannot-hide-new-schema", false, oldMetadata);
            oldMetadata.Remove("schemaVersion"); oldMetadata["authorSdkVersion"] = "0.7.0";
            LegacyMetadata("stripped-schema-cannot-downgrade-sdk", false, oldMetadata);
            oldMetadata.Remove("authorSdkVersion"); oldMetadata["entryDllSha256"] = new string('0', 64);
            LegacyMetadata("legacy-fields-cannot-hide-binding", false, oldMetadata);
            oldMetadata.Remove("entryDllSha256"); oldMetadata["unknown"] = "value";
            LegacyMetadata("unknown-legacy-metadata-rejected", false, oldMetadata);
            oldMetadata.Remove("unknown"); oldMetadata["generatedBy"] = 1;
            LegacyMetadata("non-string-legacy-metadata-rejected", false, oldMetadata);
            JsonObject codeManifest = Manifest("0.5.5");
            codeManifest["Type"] = "CodeMod";
            codeManifest["EntryDll"] = "missing.dll";
            File.WriteAllText(manifestPath, codeManifest.ToJsonString());
            foreach (string target in new[] { "0.6.1", "0.7.0" })
            {
                JsonObject marker = Marker();
                marker["packageKind"] = "CodeMod";
                marker["codeModKind"] = "Strict";
                marker["targetDtmApiVersion"] = target;
                File.WriteAllText(markerPath, marker.ToJsonString());
                inspect(root, "deleted-kind-cannot-bypass-target-" + target, false);
            }
            JsonObject rebound = Marker();
            rebound["packageKind"] = "CodeMod";
            rebound["codeModKind"] = "Strict";
            File.WriteAllText(markerPath, rebound.ToJsonString());
            inspect(root, "deleted-kind-cannot-bypass-schema2-binding", false);

            File.WriteAllText(manifestPath, Manifest("0.5.5").ToJsonString());
            string metadata = "{\"uniqueId\":\"DTMAPI.Tests.Target\",\"version\":\"1.0.0\"}";
            foreach (var invalid in new[]
            {
                (Name: "empty-marker", Bytes: Array.Empty<byte>()),
                (Name: "truncated-bom", Bytes: new byte[] { 0xEF, 0xBB }),
                (Name: "double-bom", Bytes: Encoding.UTF8.GetBytes("\uFEFF\uFEFF" + metadata)),
                (Name: "literal-mojibake", Bytes: Encoding.UTF8.GetBytes("\u00EF\u00BB\u00BF" + metadata)),
                (Name: "embedded-bom", Bytes: Encoding.UTF8.GetBytes("{\uFEFF" + metadata.Substring(1))),
                (Name: "invalid-json-token", Bytes: Encoding.UTF8.GetBytes("{\"uniqueId\":!}")),
                // The existing limit applies to original bytes, including the preamble.
                (Name: "bom-over-size-limit", Bytes: Encoding.UTF8.GetBytes("\uFEFF" + metadata.PadRight(65536)))
            })
            {
                File.WriteAllBytes(markerPath, invalid.Bytes);
                inspectPackage(root, invalid.Name, false);
            }
            File.WriteAllBytes(markerPath, new byte[] { 0xEF, 0xBB, 0xBF });
            inspectPackage(root, "bom-only", false);
        }
    }
}
