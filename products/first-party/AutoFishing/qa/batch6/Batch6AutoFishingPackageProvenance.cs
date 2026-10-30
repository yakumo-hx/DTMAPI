using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    [DataContract]
    internal sealed class Batch6AutoFishingPackageProvenance
    {
        [DataMember(Name = "rootPath", Order = 1)] internal string RootPath { get; set; } = string.Empty;
        [DataMember(Name = "packageSha256", Order = 2)] internal string PackageSha256 { get; set; } = string.Empty;
        [DataMember(Name = "entryDllSha256", Order = 3)] internal string EntryDllSha256 { get; set; } = string.Empty;
        [DataMember(Name = "manifestSha256", Order = 4)] internal string ManifestSha256 { get; set; } = string.Empty;
        [DataMember(Name = "referencePolicySha256", Order = 5)] internal string ReferencePolicySha256 { get; set; } = string.Empty;
        [DataMember(Name = "advancedReferenceReceiptSha256", Order = 6)] internal string AdvancedReferenceReceiptSha256 { get; set; } = string.Empty;
        [DataMember(Name = "packageMarkerSha256", Order = 7)] internal string PackageMarkerSha256 { get; set; } = string.Empty;
        [DataMember(Name = "transactionId", Order = 8)] internal string TransactionId { get; set; } = string.Empty;
        [DataMember(Name = "verified", Order = 9)] internal bool Verified { get; set; }
    }

    internal static class Batch6AutoFishingPackageVerifier
    {
        private const string ContentRelativeRoot = "Content/DTMAPI";
        private const string AdvancedReceiptFile = "dtmapi-advanced-references.json";
        private const string MarkerFile = "dtmapi-package.json";

        internal static Batch6AutoFishingPackageProvenance Verify(string rootPath, Batch6AutoFishingPilotSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new InvalidDataException("The loaded AutoFishing package has no root path.");

            string root = Path.GetFullPath(rootPath);
            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException("The loaded AutoFishing package root is unavailable: " + root);
            string content = Resolve(root, ContentRelativeRoot);
            string manifestPath = Resolve(content, "manifest.json");
            string entryPath = Resolve(content, Batch6AutoFishingPilotSettings.ProductEntryDll);
            string advancedPath = Resolve(content, AdvancedReceiptFile);
            string markerPath = Resolve(content, MarkerFile);
            string deploymentPath = Resolve(root, ".dtmapi-author-receipt.json");
            RequireFile(manifestPath, "manifest");
            RequireFile(entryPath, "entry DLL");
            RequireFile(advancedPath, "Advanced reference receipt");
            RequireFile(markerPath, "package marker");
            RequireFile(deploymentPath, "Author SDK deployment receipt");

            DeploymentReceipt deployment = ReadJson<DeploymentReceipt>(deploymentPath);
            PackageMarker marker = ReadJson<PackageMarker>(markerPath);
            AdvancedReceipt advanced = ReadJson<AdvancedReceipt>(advancedPath);
            RuntimeManifest manifest = ReadJson<RuntimeManifest>(manifestPath);

            string manifestSha = Sha256File(manifestPath);
            string entrySha = Sha256File(entryPath);
            string advancedSha = Sha256File(advancedPath);
            string markerSha = Sha256File(markerPath);
            string packageSha = Batch6AutoFishingPilotSettings.NormalizeSha256(deployment.PackageSha256, "deployment packageSha256");
            string policySha = Batch6AutoFishingPilotSettings.NormalizeSha256(advanced.ReferencePolicySha256, "referencePolicySha256");

            RequireExact(deployment.UniqueId, Batch6AutoFishingPilotSettings.ProductUniqueId, "deployment uniqueId");
            RequireExact(deployment.PackageKind, "CodeMod", "deployment packageKind");
            RequireExact(deployment.CodeModKind, "Advanced", "deployment codeModKind");
            RequireExact(deployment.DestinationRelativePath.Replace('\\', '/'), "Mods/" + Batch6AutoFishingPilotSettings.ProductUniqueId, "deployment destinationRelativePath");
            RequireHash(deployment.ManifestSha256, manifestSha, "deployment manifest hash");
            RequireHash(deployment.EntryDllSha256, entrySha, "deployment entry hash");
            RequireHash(deployment.AdvancedReferenceReceiptSha256, advancedSha, "deployment Advanced receipt hash");
            RequireHash(deployment.PackageMarkerSha256, markerSha, "deployment package marker hash");

            RequireExact(manifest.UniqueID, Batch6AutoFishingPilotSettings.ProductUniqueId, "manifest UniqueID");
            RequireExact(manifest.EntryType, Batch6AutoFishingPilotSettings.ProductEntryType, "manifest EntryType");
            RequireExact(manifest.EntryDll.Replace('\\', '/'), ContentRelativeRoot + "/" + Batch6AutoFishingPilotSettings.ProductEntryDll, "manifest EntryDll");
            RequireExact(manifest.Type, "CodeMod", "manifest Type");
            RequireExact(manifest.CodeModKind, "Advanced", "manifest CodeModKind");

            RequireExact(marker.UniqueId, Batch6AutoFishingPilotSettings.ProductUniqueId, "package marker uniqueId");
            RequireExact(marker.PackageKind, "CodeMod", "package marker packageKind");
            RequireExact(marker.CodeModKind, "Advanced", "package marker codeModKind");
            RequireExact(marker.EntryDllPath.Replace('\\', '/'), ContentRelativeRoot + "/" + Batch6AutoFishingPilotSettings.ProductEntryDll, "package marker entryDllPath");
            RequireExact(marker.ManifestPath.Replace('\\', '/'), ContentRelativeRoot + "/manifest.json", "package marker manifestPath");
            RequireHash(marker.ManifestSha256, manifestSha, "package marker manifest hash");
            RequireHash(marker.EntryDllSha256, entrySha, "package marker entry hash");
            RequireHash(marker.AdvancedReferenceReceiptSha256, advancedSha, "package marker Advanced receipt hash");

            RequireExact(advanced.UniqueId, Batch6AutoFishingPilotSettings.ProductUniqueId, "Advanced receipt uniqueId");
            RequireExact(advanced.CodeModKind, "Advanced", "Advanced receipt codeModKind");
            RequireHash(advanced.ManifestSha256, manifestSha, "Advanced receipt manifest hash");
            RequireHash(advanced.EntryDllSha256, entrySha, "Advanced receipt entry hash");
            RequireHash(packageSha, settings.ExpectedPackageSha256, "expected package hash");
            RequireHash(entrySha, settings.ExpectedEntryDllSha256, "expected entry hash");
            RequireHash(manifestSha, settings.ExpectedManifestSha256, "expected manifest hash");
            RequireHash(policySha, settings.ExpectedReferencePolicySha256, "expected reference policy hash");

            return new Batch6AutoFishingPackageProvenance
            {
                RootPath = root,
                PackageSha256 = packageSha,
                EntryDllSha256 = entrySha,
                ManifestSha256 = manifestSha,
                ReferencePolicySha256 = policySha,
                AdvancedReferenceReceiptSha256 = advancedSha,
                PackageMarkerSha256 = markerSha,
                TransactionId = deployment.TransactionId ?? string.Empty,
                Verified = true
            };
        }

        private static string Resolve(string root, string relative)
        {
            string normalized = relative.Replace('/', Path.DirectorySeparatorChar);
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(Path.Combine(root, normalized));
            if (!path.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("AutoFishing package path escaped its root: " + relative);
            return path;
        }

        private static void RequireFile(string path, string label)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("The loaded AutoFishing package is missing its " + label + ".", path);
        }

        private static T ReadJson<T>(string path)
        {
            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    object? value = new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
                    return value is T typed
                        ? typed
                        : throw new InvalidDataException(Path.GetFileName(path) + " did not contain one expected JSON object.");
                }
            }
            catch (InvalidDataException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Could not read strict AutoFishing provenance JSON " + path + ": " + ex.GetType().Name + ": " + ex.Message, ex);
            }
        }

        private static string Sha256File(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static void RequireHash(string actual, string expected, string label)
        {
            string left = Batch6AutoFishingPilotSettings.NormalizeSha256(actual, label + " actual");
            string right = Batch6AutoFishingPilotSettings.NormalizeSha256(expected, label + " expected");
            if (!left.Equals(right, StringComparison.Ordinal))
                throw new InvalidDataException(label + " mismatch. expected=" + right + "; actual=" + left + ".");
        }

        private static void RequireExact(string? actual, string expected, string label)
        {
            if (!string.Equals(actual ?? string.Empty, expected, StringComparison.Ordinal))
                throw new InvalidDataException(label + " mismatch. expected=" + expected + "; actual=" + (actual ?? string.Empty) + ".");
        }

        [DataContract]
        private sealed class DeploymentReceipt
        {
            [DataMember(Name = "transactionId", IsRequired = true)] internal string TransactionId { get; set; } = string.Empty;
            [DataMember(Name = "uniqueId", IsRequired = true)] internal string UniqueId { get; set; } = string.Empty;
            [DataMember(Name = "packageKind", IsRequired = true)] internal string PackageKind { get; set; } = string.Empty;
            [DataMember(Name = "codeModKind", IsRequired = true)] internal string CodeModKind { get; set; } = string.Empty;
            [DataMember(Name = "destinationRelativePath", IsRequired = true)] internal string DestinationRelativePath { get; set; } = string.Empty;
            [DataMember(Name = "packageSha256", IsRequired = true)] internal string PackageSha256 { get; set; } = string.Empty;
            [DataMember(Name = "manifestSha256", IsRequired = true)] internal string ManifestSha256 { get; set; } = string.Empty;
            [DataMember(Name = "entryDllSha256", IsRequired = true)] internal string EntryDllSha256 { get; set; } = string.Empty;
            [DataMember(Name = "advancedReferenceReceiptSha256", IsRequired = true)] internal string AdvancedReferenceReceiptSha256 { get; set; } = string.Empty;
            [DataMember(Name = "packageMarkerSha256", IsRequired = true)] internal string PackageMarkerSha256 { get; set; } = string.Empty;
        }

        [DataContract]
        private sealed class PackageMarker
        {
            [DataMember(Name = "uniqueId", IsRequired = true)] internal string UniqueId { get; set; } = string.Empty;
            [DataMember(Name = "packageKind", IsRequired = true)] internal string PackageKind { get; set; } = string.Empty;
            [DataMember(Name = "codeModKind", IsRequired = true)] internal string CodeModKind { get; set; } = string.Empty;
            [DataMember(Name = "manifestPath", IsRequired = true)] internal string ManifestPath { get; set; } = string.Empty;
            [DataMember(Name = "manifestSha256", IsRequired = true)] internal string ManifestSha256 { get; set; } = string.Empty;
            [DataMember(Name = "entryDllPath", IsRequired = true)] internal string EntryDllPath { get; set; } = string.Empty;
            [DataMember(Name = "entryDllSha256", IsRequired = true)] internal string EntryDllSha256 { get; set; } = string.Empty;
            [DataMember(Name = "advancedReferenceReceiptSha256", IsRequired = true)] internal string AdvancedReferenceReceiptSha256 { get; set; } = string.Empty;
        }

        [DataContract]
        private sealed class AdvancedReceipt
        {
            [DataMember(Name = "referencePolicySha256", IsRequired = true)] internal string ReferencePolicySha256 { get; set; } = string.Empty;
            [DataMember(Name = "uniqueId", IsRequired = true)] internal string UniqueId { get; set; } = string.Empty;
            [DataMember(Name = "codeModKind", IsRequired = true)] internal string CodeModKind { get; set; } = string.Empty;
            [DataMember(Name = "manifestSha256", IsRequired = true)] internal string ManifestSha256 { get; set; } = string.Empty;
            [DataMember(Name = "entryDllSha256", IsRequired = true)] internal string EntryDllSha256 { get; set; } = string.Empty;
        }

        [DataContract]
        private sealed class RuntimeManifest
        {
            [DataMember(Name = "UniqueID", IsRequired = true)] internal string UniqueID { get; set; } = string.Empty;
            [DataMember(Name = "EntryDll", IsRequired = true)] internal string EntryDll { get; set; } = string.Empty;
            [DataMember(Name = "EntryType", IsRequired = true)] internal string EntryType { get; set; } = string.Empty;
            [DataMember(Name = "Type", IsRequired = true)] internal string Type { get; set; } = string.Empty;
            [DataMember(Name = "CodeModKind", IsRequired = true)] internal string CodeModKind { get; set; } = string.Empty;
        }
    }
}
