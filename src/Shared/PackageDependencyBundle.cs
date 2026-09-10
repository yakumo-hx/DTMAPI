using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static DTMAPI.Internal.Authoring.PackageDependencyContract;

namespace DTMAPI.Internal.Authoring
{
    internal sealed class PackageDependencyBundle
    {
        public const string MarkerPath = "Content/DTMAPI/dtmapi-package.json";
        public PackageDependencyManifest Manifest { get; private set; } = null!;
        public PackageDependencyInventory Inventory { get; private set; } = null!;
        public IReadOnlyList<string> Files { get; private set; } = System.Array.Empty<string>();
        public NativePackageContract? Native { get; private set; }

        public static PackageDependencyBundle Read(string root, string manifestPath, string uniqueId, string version,
            string kind, string codeKind, string entryPath, string minimumRuntime)
        {
            string markerPath = ResolveFile(root, MarkerPath);
            bool nativeSelected = NativePackageContract.Select(File.ReadAllBytes(manifestPath));
            var marker = Object(ReadJson(File.ReadAllBytes(markerPath)),
                new[] {
                "schemaVersion", "owner", "uniqueId", "version", "packageKind", "codeModKind", "authorSdkVersion", "targetDtmApiVersion",
                "manifestPath", "manifestSha256", "entryDllPath", "entryDllSha256", "advancedReferenceReceiptPath", "advancedReferenceReceiptSha256", "authority",
                "dependencyContractVersion", "dependencyInventoryPath", "dependencyInventorySha256", "files",
                "nativeContractVersion", "nativeBuildPath", "nativeBuildSha256" },
                nativeSelected ? System.Array.Empty<string>() : new[] { "nativeContractVersion", "nativeBuildPath", "nativeBuildSha256" });
            if (!nativeSelected && marker.Keys.Any(key => key == "nativeContractVersion" || key == "nativeBuildPath" || key == "nativeBuildSha256"))
                throw new InvalidDataException("native-selector-missing: Native marker cannot fall back to an old package reader.");
            string Value(string field) => Text(Required(marker, field));
            void Reject(string reason) => throw new InvalidDataException("dependency-package-binding-invalid: " + reason);
            if (Number(Required(marker, "schemaVersion")) != 3 || Number(Required(marker, "dependencyContractVersion")) != 1) Reject("Unknown format; no legacy fallback.");
            if (Value("owner") != "DTMAPI" || Value("uniqueId") != uniqueId || Value("version") != version || Value("packageKind") != kind || Value("codeModKind") != codeKind || Value("authority") != "dtmapi-author-sdk-package-binding") Reject("Identity disagrees with manifest.");
            if (!AuthorApiTargetCatalog.Current.TryValidateReadablePackageTarget(Value("targetDtmApiVersion"), Value("authorSdkVersion"), minimumRuntime, out string code, out string reason)) Reject(code + ": " + reason);
            if (!AuthorApiTargetCatalog.Current.GetAvailable(Value("targetDtmApiVersion")).Capabilities.Contains("package-dependencies/1", StringComparer.Ordinal)) Reject("Target does not support dependency contract V1.");
            if (Value("advancedReferenceReceiptPath") != "" || Value("advancedReferenceReceiptSha256") != "") Reject("Dependency V1 does not authorize legacy Advanced receipts.");
            if (Value("dependencyInventoryPath") != PackageDependencyInventory.FileName) Reject("Dependency inventory path.");
            var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in Array(Required(marker, "files")))
            {
                var file = Object(item, "path", "length", "sha256");
                string path = Relative(Text(Required(file, "path"))), hash = Hash(Text(Required(file, "sha256")));
                if (path == MarkerPath || files.ContainsKey(path)) Reject("Circular/duplicate inventory file: " + path);
                byte[] bytes = File.ReadAllBytes(ResolveFile(root, path));
                if (bytes.LongLength != Number(Required(file, "length")) || ComputeHash(bytes) != hash) Reject("File bytes changed: " + path);
                files.Add(path, hash);
            }
            string Bound(string pathField, string hashField)
            {
                string path = Value(pathField);
                if (!files.TryGetValue(path, out string? digest) || digest != Value(hashField)) Reject("Missing/mismatched inventory binding: " + pathField);
                return ResolveFile(root, path);
            }
            if (!string.Equals(Bound("manifestPath", "manifestSha256"), Path.GetFullPath(manifestPath), StringComparison.OrdinalIgnoreCase)) Reject("Manifest path.");
            if (kind == "CodeMod")
            { if (!string.Equals(Bound("entryDllPath", "entryDllSha256"), Path.GetFullPath(entryPath), StringComparison.OrdinalIgnoreCase)) Reject("Entry path."); }
            else if (Value("entryDllPath") != "" || Value("entryDllSha256") != "") Reject("ContentPack entry binding.");
            var manifest = ReadManifest(File.ReadAllBytes(manifestPath)) ?? throw new InvalidDataException("dependency-selector-missing: manifest requires DependencyContractVersion=1.");
            var inventory = ReadInventory(File.ReadAllBytes(Bound("dependencyInventoryPath", "dependencyInventorySha256")));
            RequireProjection(manifest, inventory);
            if (inventory.ApiTarget != Value("targetDtmApiVersion") || inventory.EntryPath != Value("entryDllPath")) Reject("Target/entry projection.");
            NativePackageContract? native = null;
            if (nativeSelected)
            {
                if (Number(Required(marker, "nativeContractVersion")) != NativePackageContract.SelectedVersion(File.ReadAllBytes(manifestPath)) || Value("nativeBuildPath") != NativePackageContract.FileName) Reject("Unknown/mismatched native format/path.");
                if (!AuthorApiTargetCatalog.Current.GetAvailable(inventory.ApiTarget).Capabilities.Contains("native-contract/1", StringComparer.Ordinal)) Reject("Target does not support native contract V1.");
                native = NativePackageContract.Read(File.ReadAllBytes(Bound("nativeBuildPath", "nativeBuildSha256")));
                native.RequireBinding(uniqueId, inventory.ApiTarget, File.ReadAllBytes(manifestPath), File.ReadAllBytes(entryPath), File.ReadAllBytes(ResolveFile(root, PackageDependencyInventory.FileName)));
            }
            else if (files.Keys.Contains(NativePackageContract.FileName, StringComparer.OrdinalIgnoreCase)) Reject("Native provenance requires its manifest selector.");
            return new PackageDependencyBundle { Manifest = manifest, Inventory = inventory, Files = files.Keys.ToArray(), Native = native };
        }
    }
}
