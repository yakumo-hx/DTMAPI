using DTMAPI.Authoring.Contracts;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DTMAPI.AuthorSdk;

internal sealed class StagedDeploymentPackage
{
    public required string PackagePath { get; init; }
    public required string PackageSha256 { get; init; }
    public required string StagingPath { get; init; }
    public required RuntimeManifest Manifest { get; init; }
    public required string PackageKind { get; init; }
    public required string CodeModKind { get; init; }
    public required DeploymentReceipt Receipt { get; init; }
    public required DeploymentRecord Record { get; init; }
}

internal sealed class ValidatedPackageBinding
{
    public required string CodeModKind { get; init; }
    public required string ManifestSha256 { get; init; }
    public required string EntryDllSha256 { get; init; }
    public required string AdvancedReferenceReceiptSha256 { get; init; }
    public required string PackageMarkerSha256 { get; init; }
}

internal static class DeploymentPackage
{
    private const int MaximumEntries = 10000;
    private const long MaximumUncompressedBytes = 512L * 1024L * 1024L;
    private static readonly Regex UniqueIdPattern = new(@"^[A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z0-9][A-Za-z0-9_-]*)+$", RegexOptions.CultureInvariant | RegexOptions.Compiled);
    private static readonly Regex MinimumVersionPattern = new(@"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static StagedDeploymentPackage ExtractAndReceipt(string packagePath, string stagingPath, string gameRoot, string gameRootKey, string transactionId)
    {
        string fullPackage = Path.GetFullPath(packagePath);
        if (!File.Exists(fullPackage))
            throw new FileNotFoundException("Deployment package was not found.", fullPackage);
        if (!fullPackage.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("deploy/update accepts one deterministic .zip package.");
        if (Directory.Exists(stagingPath) || File.Exists(stagingPath))
            throw new InvalidDataException("Transaction staging path already exists: " + stagingPath);
        Directory.CreateDirectory(stagingPath);
        try
        {
            ExtractZip(fullPackage, stagingPath);
            string manifestPath = Path.Combine(stagingPath, "Content", "DTMAPI", "manifest.json");
            string infoPath = Path.Combine(stagingPath, "info.json");
            if (!File.Exists(manifestPath) || !File.Exists(infoPath))
                throw new InvalidDataException("Package must contain info.json and exactly one Content/DTMAPI/manifest.json.");
            if (Directory.EnumerateFiles(stagingPath, "manifest.json", SearchOption.AllDirectories).Count() != 1)
                throw new InvalidDataException("Package must contain exactly one manifest.json.");
            if (File.Exists(Path.Combine(stagingPath, DeploymentTree.ReceiptFileName)))
                throw new InvalidDataException("Input package already contains the reserved Author SDK deployment receipt.");

            byte[] manifestBytes = File.ReadAllBytes(manifestPath);
            RuntimeManifest manifest = JsonSerializer.Deserialize<RuntimeManifest>(manifestBytes, JsonSupport.RuntimeManifest)
                ?? throw new InvalidDataException("Packaged manifest.json must contain one object.");
            if (!Enum.TryParse(manifest.Type, false, out AuthorProjectKind kind) || kind is not (AuthorProjectKind.CodeMod or AuthorProjectKind.ContentPack))
                throw new InvalidDataException("Deployment package Type must be exactly CodeMod or ContentPack.");
            using JsonDocument manifestDocument = JsonDocument.Parse(manifestBytes);
            bool declaresCodeModKind = manifestDocument.RootElement.TryGetProperty("CodeModKind", out _);
            AuthorCodeModKind codeModKind = AuthorCodeModKind.Strict;
            if (declaresCodeModKind && !Enum.TryParse(manifest.CodeModKind, ignoreCase: false, out codeModKind))
                throw new InvalidDataException("Deployment package CodeModKind must be exactly Strict or Advanced.");
            if (kind == AuthorProjectKind.ContentPack && declaresCodeModKind)
                throw new InvalidDataException("ContentPack deployment cannot declare CodeModKind.");
            ValidateIdentity(manifest);
            ValidateInfoParity(infoPath, manifest);
            ValidatedPackageBinding binding = ValidateKindPayload(stagingPath, manifest, manifestBytes, kind, codeModKind, gameRoot);

            DeploymentTreeInventory payload = DeploymentTree.Create(stagingPath, excludeReceipt: true);
            string destinationRelative = "Mods/" + manifest.UniqueID;
            var receipt = new DeploymentReceipt
            {
                SchemaVersion = AuthorSdkContract.DeploymentSchemaVersion,
                TransactionId = transactionId,
                GameRootKey = gameRootKey,
                UniqueId = manifest.UniqueID,
                PackageKind = kind.ToString(),
                CodeModKind = binding.CodeModKind,
                DestinationRelativePath = destinationRelative,
                PackageSha256 = PathSafety.Sha256File(fullPackage),
                ManifestSha256 = binding.ManifestSha256,
                EntryDllSha256 = binding.EntryDllSha256,
                AdvancedReferenceReceiptSha256 = binding.AdvancedReferenceReceiptSha256,
                PackageMarkerSha256 = binding.PackageMarkerSha256,
                PayloadTreeSha256 = payload.TreeSha256,
                PayloadFileCount = payload.Files.Count,
                PayloadDirectoryCount = payload.Directories.Count
            };
            string receiptPath = Path.Combine(stagingPath, DeploymentTree.ReceiptFileName);
            AtomicStateFile.Write(receiptPath, receipt);
            DeploymentTreeInventory full = DeploymentTree.Create(stagingPath);
            var record = new DeploymentRecord
            {
                TransactionId = transactionId,
                PackageSha256 = receipt.PackageSha256,
                PayloadTreeSha256 = payload.TreeSha256,
                ReceiptSha256 = PathSafety.Sha256File(receiptPath),
                Inventory = full
            };
            return new StagedDeploymentPackage
            {
                PackagePath = fullPackage,
                PackageSha256 = receipt.PackageSha256,
                StagingPath = stagingPath,
                Manifest = manifest,
                PackageKind = kind.ToString(),
                CodeModKind = binding.CodeModKind,
                Receipt = receipt,
                Record = record
            };
        }
        catch
        {
            // Keep the same-volume staging tree as recovery evidence; never guess whether it is safe to delete.
            throw;
        }
    }

    public static ValidatedPackageBinding ValidateInstalledPackage(
        string root,
        string gameRoot,
        bool legacyDeployment = false,
        string? expectedUniqueId = null,
        string? expectedPackageKind = null)
    {
        string manifestPath = Path.Combine(root, "Content", "DTMAPI", "manifest.json");
        if (!File.Exists(manifestPath))
            throw new InvalidDataException("Installed package manifest is missing.");
        byte[] manifestBytes = File.ReadAllBytes(manifestPath);
        RuntimeManifest manifest = JsonSerializer.Deserialize<RuntimeManifest>(manifestBytes, JsonSupport.RuntimeManifest)
            ?? throw new InvalidDataException("Installed package manifest must contain one object.");
        if (!Enum.TryParse(manifest.Type, ignoreCase: false, out AuthorProjectKind kind) || kind is not (AuthorProjectKind.CodeMod or AuthorProjectKind.ContentPack))
            throw new InvalidDataException("Installed package Type is invalid.");
        ValidateIdentity(manifest);
        if ((expectedUniqueId != null && !manifest.UniqueID.Equals(expectedUniqueId, StringComparison.Ordinal))
            || (expectedPackageKind != null && !manifest.Type.Equals(expectedPackageKind, StringComparison.Ordinal)))
            throw new InvalidDataException("Installed package manifest identity/kind does not match the deployment journal.");
        using JsonDocument document = JsonDocument.Parse(manifestBytes);
        bool declaresKind = document.RootElement.TryGetProperty("CodeModKind", out _);
        if (legacyDeployment && declaresKind)
            throw new InvalidDataException("Legacy schema-1 deployments cannot declare CodeModKind and are never Advanced CodeMods.");
        AuthorCodeModKind codeModKind = AuthorCodeModKind.Strict;
        if (declaresKind && !Enum.TryParse(manifest.CodeModKind, ignoreCase: false, out codeModKind))
            throw new InvalidDataException("Installed package CodeModKind is invalid.");
        if (kind == AuthorProjectKind.ContentPack && declaresKind)
            throw new InvalidDataException("Installed ContentPack cannot declare CodeModKind.");
        return ValidateKindPayload(root, manifest, manifestBytes, kind, codeModKind, gameRoot, legacyDeployment);
    }

    private static void ExtractZip(string packagePath, string stagingPath)
    {
        using ZipArchive archive = ZipFile.OpenRead(packagePath);
        if (archive.Entries.Count > MaximumEntries)
            throw new InvalidDataException("Package exceeds the maximum entry count.");
        long total = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string name = entry.FullName;
            if (name.Contains("\\", StringComparison.Ordinal))
                throw new InvalidDataException("ZIP entry paths must use forward slashes: " + name);
            bool directory = name.EndsWith("/", StringComparison.Ordinal);
            string normalized = directory ? name.TrimEnd('/') : name;
            if (normalized.Length == 0)
                continue;
            if (Path.IsPathRooted(normalized) || normalized.Contains(":", StringComparison.Ordinal) || normalized.Split('/').Any(segment => segment is "" or "." or ".."))
                throw new InvalidDataException("Unsafe ZIP entry path: " + name);
            if (!seen.Add(normalized))
                throw new InvalidDataException("Case-insensitive duplicate ZIP entry: " + normalized);
            int unixMode = (entry.ExternalAttributes >> 16) & 0xF000;
            if (unixMode == 0xA000)
                throw new InvalidDataException("Symbolic-link ZIP entries are not accepted: " + normalized);
            total = checked(total + entry.Length);
            if (total > MaximumUncompressedBytes)
                throw new InvalidDataException("Package exceeds the maximum uncompressed size.");
            string target = PathSafety.ResolveUnderRoot(stagingPath, normalized, "ZIP entry");
            if (directory)
            {
                Directory.CreateDirectory(target);
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            using Stream source = entry.Open();
            using var destination = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            source.CopyTo(destination);
            destination.Flush(true);
        }
    }

    private static void ValidateIdentity(RuntimeManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.UniqueID) || !UniqueIdPattern.IsMatch(manifest.UniqueID) || manifest.UniqueID.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || manifest.UniqueID.Contains('/') || manifest.UniqueID.Contains('\\') || manifest.UniqueID is "." or "..")
            throw new InvalidDataException("Packaged UniqueID is empty or path-unsafe.");
        if (string.IsNullOrWhiteSpace(manifest.Version) || string.IsNullOrWhiteSpace(manifest.Name) || string.IsNullOrWhiteSpace(manifest.Author))
            throw new InvalidDataException("Packaged manifest identity/version fields are incomplete.");
        string minimumVersion = manifest.MinimumDTMApiVersion ?? string.Empty;
        if (!MinimumVersionPattern.IsMatch(minimumVersion) ||
            CompareNumericVersion(minimumVersion, AuthorSdkContract.HighestSupportedRuntimeVersion) > 0)
        {
            throw new InvalidDataException(
                "Author SDK deployment requires a numeric major.minor.patch MinimumDTMApiVersion no newer than " +
                AuthorSdkContract.HighestSupportedRuntimeVersion + ".");
        }
    }

    private static int CompareNumericVersion(string left, string right)
    {
        string[] a = left.Split('.');
        string[] b = right.Split('.');
        for (int index = 0; index < 3; index++)
        {
            int comparison = a[index].Length.CompareTo(b[index].Length);
            if (comparison == 0)
                comparison = string.CompareOrdinal(a[index], b[index]);
            if (comparison != 0)
                return comparison;
        }
        return 0;
    }

    private static void ValidateInfoParity(string infoPath, RuntimeManifest manifest)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(infoPath, Encoding.UTF8));
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("info.json must contain one object.");
        string version = document.RootElement.TryGetProperty("version", out JsonElement value) ? value.GetString() ?? string.Empty : string.Empty;
        if (!version.Equals(manifest.Version, StringComparison.Ordinal))
            throw new InvalidDataException("Package info.json version does not exactly match manifest Version.");
    }

    private static ValidatedPackageBinding ValidateKindPayload(string root, RuntimeManifest manifest, byte[] manifestBytes, AuthorProjectKind kind, AuthorCodeModKind codeModKind, string gameRoot, bool legacyDeployment = false)
    {
        string manifestSha256 = PathSafety.Sha256Bytes(manifestBytes);
        string receiptPath = Path.Combine(root, AuthorSdkContract.AdvancedReferenceReceiptPath.Replace('/', Path.DirectorySeparatorChar));
        string markerPath = Path.Combine(root, "Content", "DTMAPI", "dtmapi-package.json");
        if (!File.Exists(markerPath))
            throw new InvalidDataException("Package is missing the SDK package-binding marker.");
        string[] dlls = Directory.EnumerateFiles(root, "*.dll", SearchOption.AllDirectories).ToArray();
        if (kind == AuthorProjectKind.ContentPack)
        {
            ManagedAssemblyInspector.ValidateNoBundledNativePayloads(root, string.Empty);
            if (!string.IsNullOrWhiteSpace(manifest.EntryDll) || !string.IsNullOrWhiteSpace(manifest.EntryType) || dlls.Length != 0)
                throw new InvalidDataException("ContentPack deployment cannot contain or declare DLL code.");
            if (File.Exists(receiptPath))
                throw new InvalidDataException("ContentPack deployment cannot contain an Advanced reference receipt.");
            if (legacyDeployment)
                ValidateLegacyPackageMarker(markerPath, manifest);
            else
                ValidatePackageMarker(markerPath, manifest, string.Empty, manifestSha256, string.Empty, string.Empty);
            return new ValidatedPackageBinding
            {
                CodeModKind = string.Empty,
                ManifestSha256 = manifestSha256,
                EntryDllSha256 = string.Empty,
                AdvancedReferenceReceiptSha256 = string.Empty,
                PackageMarkerSha256 = PathSafety.Sha256File(markerPath)
            };
        }
        if (string.IsNullOrWhiteSpace(manifest.EntryDll) || string.IsNullOrWhiteSpace(manifest.EntryType))
            throw new InvalidDataException("CodeMod deployment requires EntryDll and EntryType.");
        string normalized = manifest.EntryDll.Replace('\\', '/');
        if (!normalized.StartsWith("Content/DTMAPI/", StringComparison.Ordinal) || normalized.Split('/').Any(segment => segment is "" or "." or ".."))
            throw new InvalidDataException("Packaged CodeMod EntryDll must stay under Content/DTMAPI.");
        ManagedAssemblyInspector.ValidateNoBundledNativePayloads(root, normalized);
        string entryPath = PathSafety.ResolveUnderRoot(root, normalized, "EntryDll");
        if (!File.Exists(entryPath) || !entryPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Packaged CodeMod EntryDll is missing.");
        if (dlls.Length != 1 || !Path.GetFullPath(dlls[0]).Equals(Path.GetFullPath(entryPath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("CodeMod package must contain exactly its declared EntryDll and no additional DLLs.");
        AdvancedReferencePolicyEntry[] trackedReferences = AdvancedReferenceAssets.LoadAllTrackedPolicies()
            .SelectMany(value => value.Policy.References)
            .ToArray();
        string entryAssemblyName = System.Reflection.AssemblyName.GetAssemblyName(entryPath).Name ?? string.Empty;
        if (trackedReferences.Any(reference => reference.AssemblyName.Equals(entryAssemblyName, StringComparison.Ordinal)
                                                       || Path.GetFileName(reference.GameRelativePath).Equals(Path.GetFileName(entryPath), StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("CodeMod EntryDll cannot impersonate or bundle a tracked native/game reference assembly.");

        byte[] entryBytes = File.ReadAllBytes(entryPath);
        string entrySha256 = PathSafety.Sha256Bytes(entryBytes);
        string advancedReceiptSha256 = string.Empty;
        if (codeModKind == AuthorCodeModKind.Advanced)
        {
            if (!File.Exists(receiptPath))
                throw new InvalidDataException("Advanced CodeMod deployment requires the SDK reference receipt.");
            AdvancedReferenceReceipt receipt = AdvancedReferenceAssets.ReadReceipt(receiptPath);
            AdvancedReferenceAssets.ValidateReceipt(
                receipt,
                manifest,
                AuthorSdkContract.PackageManifestPath,
                manifestBytes,
                normalized,
                entryBytes,
                gameRoot);
            ManagedAssemblyInspector.ValidateCodeMod(entryPath, codeModKind, receipt.References.Select(reference => reference.AssemblyName), Path.GetFileNameWithoutExtension(entryPath));
            advancedReceiptSha256 = PathSafety.Sha256File(receiptPath);
        }
        else
        {
            if (File.Exists(receiptPath))
                throw new InvalidDataException("Strict CodeMod deployment cannot contain an Advanced reference receipt.");
            ManagedAssemblyInspector.ValidateCodeMod(entryPath, codeModKind, Array.Empty<string>(), Path.GetFileNameWithoutExtension(entryPath));
        }
        if (legacyDeployment)
            ValidateLegacyPackageMarker(markerPath, manifest);
        else
            ValidatePackageMarker(markerPath, manifest, codeModKind.ToString(), manifestSha256, entrySha256, advancedReceiptSha256);
        return new ValidatedPackageBinding
        {
            CodeModKind = codeModKind.ToString(),
            ManifestSha256 = manifestSha256,
            EntryDllSha256 = entrySha256,
            AdvancedReferenceReceiptSha256 = advancedReceiptSha256,
            PackageMarkerSha256 = PathSafety.Sha256File(markerPath)
        };
    }

    private static void ValidatePackageMarker(string markerPath, RuntimeManifest manifest, string codeModKind, string manifestSha256, string entryDllSha256, string advancedReceiptSha256)
    {
        string markerText = File.ReadAllText(markerPath, Encoding.UTF8);
        ValidateExactMarkerProperties(markerText, markerPath, new[]
        {
            "advancedReferenceReceiptPath", "advancedReferenceReceiptSha256", "authorSdkVersion", "authority",
            "codeModKind", "entryDllPath", "entryDllSha256", "manifestPath", "manifestSha256", "owner",
            "packageKind", "schemaVersion", "targetDtmApiVersion", "uniqueId", "version"
        });
        JsonObject marker = JsonNode.Parse(markerText)?.AsObject()
            ?? throw new InvalidDataException("dtmapi-package.json must contain one object.");
        string entryPath = manifest.Type.Equals(AuthorProjectKind.CodeMod.ToString(), StringComparison.Ordinal) ? manifest.EntryDll.Replace('\\', '/') : string.Empty;
        string receiptPath = advancedReceiptSha256.Length == 0 ? string.Empty : AuthorSdkContract.AdvancedReferenceReceiptPath;
        if (marker["schemaVersion"]?.GetValue<int>() != 2
            || !Value(marker, "owner").Equals("DTMAPI", StringComparison.Ordinal)
            || !Value(marker, "uniqueId").Equals(manifest.UniqueID, StringComparison.Ordinal)
            || !Value(marker, "version").Equals(manifest.Version, StringComparison.Ordinal)
            || !Value(marker, "packageKind").Equals(manifest.Type, StringComparison.Ordinal)
            || !Value(marker, "codeModKind").Equals(codeModKind, StringComparison.Ordinal)
            || !Value(marker, "authorSdkVersion").Equals(AuthorSdkContract.SdkVersion, StringComparison.Ordinal)
            || !Value(marker, "targetDtmApiVersion").Equals(AuthorSdkContract.TargetRuntimeVersion, StringComparison.Ordinal)
            || !Value(marker, "manifestPath").Equals(AuthorSdkContract.PackageManifestPath, StringComparison.Ordinal)
            || !Value(marker, "manifestSha256").Equals(manifestSha256, StringComparison.OrdinalIgnoreCase)
            || !Value(marker, "entryDllPath").Equals(entryPath, StringComparison.Ordinal)
            || !Value(marker, "entryDllSha256").Equals(entryDllSha256, StringComparison.OrdinalIgnoreCase)
            || !Value(marker, "advancedReferenceReceiptPath").Equals(receiptPath, StringComparison.Ordinal)
            || !Value(marker, "advancedReferenceReceiptSha256").Equals(advancedReceiptSha256, StringComparison.OrdinalIgnoreCase)
            || !Value(marker, "authority").Equals("dtmapi-author-sdk-package-binding", StringComparison.Ordinal))
            throw new InvalidDataException("dtmapi-package.json does not bind the exact manifest/identity/entry/reference receipt.");
    }

    private static void ValidateLegacyPackageMarker(string markerPath, RuntimeManifest manifest)
    {
        string markerText = File.ReadAllText(markerPath, Encoding.UTF8);
        ValidateExactMarkerProperties(markerText, markerPath, new[]
        {
            "authorSdkVersion", "authority", "owner", "packageKind", "schemaVersion", "targetRuntimeVersion",
            "uniqueId", "version"
        });
        JsonObject marker = JsonNode.Parse(markerText)?.AsObject()
            ?? throw new InvalidDataException("Legacy dtmapi-package.json must contain one object.");
        if (marker["schemaVersion"]?.GetValue<int>() != 1
            || !Value(marker, "owner").Equals("DTMAPI", StringComparison.Ordinal)
            || !Value(marker, "uniqueId").Equals(manifest.UniqueID, StringComparison.Ordinal)
            || !Value(marker, "version").Equals(manifest.Version, StringComparison.Ordinal)
            || !Value(marker, "packageKind").Equals(manifest.Type, StringComparison.Ordinal)
            || !Value(marker, "authorSdkVersion").Equals(AuthorSdkContract.SdkVersion, StringComparison.Ordinal)
            || !Value(marker, "targetRuntimeVersion").Equals(AuthorSdkContract.TargetRuntimeVersion, StringComparison.Ordinal)
            || !Value(marker, "authority").Equals("metadata-only-not-an-ownership-receipt", StringComparison.Ordinal))
            throw new InvalidDataException("Legacy dtmapi-package.json does not match the exact historical Strict/ContentPack identity.");
    }

    private static void ValidateExactMarkerProperties(string text, string markerPath, string[] expected)
    {
        using JsonDocument document = JsonDocument.Parse(text, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        });
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException(Path.GetFileName(markerPath) + " must contain one JSON object.");
        string[] actual = document.RootElement.EnumerateObject().Select(property => property.Name).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
            throw new InvalidDataException(Path.GetFileName(markerPath) + " has an unknown, missing, duplicate, or wrong-version binding field.");
    }

    private static string Value(JsonObject value, string property) => value[property]?.GetValue<string>() ?? string.Empty;
}
