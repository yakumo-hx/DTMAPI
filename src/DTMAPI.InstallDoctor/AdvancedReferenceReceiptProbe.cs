using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DTMAPI.InstallDoctor;

internal sealed class AdvancedReferenceReceiptProbe
{
    internal const string RelativePath = "Content/DTMAPI/dtmapi-advanced-references.json";
    internal const string ReceiptKindValue = "DTMAPI.AdvancedCodeMod.ReferenceReceipt";

    public string Path { get; init; } = string.Empty;
    public int SchemaVersion { get; init; }
    public string ReceiptKind { get; init; } = string.Empty;
    public string ReferencePolicyId { get; init; } = string.Empty;
    public int ReferencePolicyVersion { get; init; }
    public string ReferencePolicySha256 { get; init; } = string.Empty;
    public string UniqueId { get; init; } = string.Empty;
    public string CodeModKind { get; init; } = string.Empty;
    public string TargetFramework { get; init; } = string.Empty;
    public string GameBuildId { get; init; } = string.Empty;
    public string GameAssemblyRelativePath { get; init; } = string.Empty;
    public string GameAssemblySha256 { get; init; } = string.Empty;
    public string ManifestPath { get; init; } = string.Empty;
    public string ManifestSha256 { get; init; } = string.Empty;
    public string EntryDllPath { get; init; } = string.Empty;
    public long EntryDllLength { get; init; }
    public string EntryDllSha256 { get; init; } = string.Empty;
    public string HarmonyOwner { get; init; } = string.Empty;
    public IReadOnlyList<AdvancedReferenceReceiptRow> References { get; init; } = Array.Empty<AdvancedReferenceReceiptRow>();
    public string Error { get; init; } = string.Empty;

    public static AdvancedReferenceReceiptProbe Read(string path)
    {
        string fullPath = System.IO.Path.GetFullPath(path);
        try
        {
            using FileStream stream = new(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 64 * 1024,
                options: FileOptions.SequentialScan);
            using JsonDocument document = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            });
            JsonElement root = document.RootElement;
            RequireObject(root, "receipt root");
            RequireExactProperties(
                root,
                "receipt root",
                "schemaVersion",
                "receiptKind",
                "referencePolicyId",
                "referencePolicyVersion",
                "referencePolicySha256",
                "uniqueId",
                "codeModKind",
                "targetFramework",
                "gameBuildId",
                "gameAssemblyRelativePath",
                "gameAssemblySha256",
                "manifestPath",
                "manifestSha256",
                "entryDllPath",
                "entryDllLength",
                "entryDllSha256",
                "harmonyOwner",
                "references");

            int schemaVersion = RequireInt32(root, "schemaVersion");
            if (schemaVersion != 1)
                throw new InvalidDataException("unsupported schemaVersion " + schemaVersion);
            string receiptKind = RequireString(root, "receiptKind");
            if (!receiptKind.Equals(ReceiptKindValue, StringComparison.Ordinal))
                throw new InvalidDataException("unsupported receiptKind " + receiptKind);
            int policyVersion = RequireInt32(root, "referencePolicyVersion");
            if (policyVersion <= 0)
                throw new InvalidDataException("referencePolicyVersion must be positive");

            string policySha256 = RequireSha256(root, "referencePolicySha256");
            string codeModKind = RequireString(root, "codeModKind");
            if (!codeModKind.Equals("Advanced", StringComparison.Ordinal))
                throw new InvalidDataException("codeModKind must be exactly Advanced");
            string targetFramework = RequireString(root, "targetFramework");

            string gameAssemblyRelativePath = RequireSafeRelativePath(root, "gameAssemblyRelativePath", requireDll: true);
            string gameAssemblySha256 = RequireSha256(root, "gameAssemblySha256");
            string manifestPath = RequireSafeRelativePath(root, "manifestPath", requireDll: false);
            if (!manifestPath.Equals("Content/DTMAPI/manifest.json", StringComparison.Ordinal))
                throw new InvalidDataException("manifestPath must be exactly Content/DTMAPI/manifest.json");
            string entryDllPath = RequireSafeRelativePath(root, "entryDllPath", requireDll: true);
            if (!entryDllPath.StartsWith("Content/DTMAPI/", StringComparison.Ordinal))
                throw new InvalidDataException("entryDllPath must stay under Content/DTMAPI");
            long entryDllLength = RequirePositiveInt64(root, "entryDllLength");

            JsonElement referencesElement = root.GetProperty("references");
            if (referencesElement.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("references must be an array");
            var references = new List<AdvancedReferenceReceiptRow>();
            foreach (JsonElement value in referencesElement.EnumerateArray())
            {
                RequireObject(value, "reference row");
                RequireExactProperties(value, "reference row", "gameRelativePath", "assemblyName", "length", "sha256", "copyLocal");
                if (value.GetProperty("copyLocal").ValueKind is not JsonValueKind.False)
                    throw new InvalidDataException("reference copyLocal must be exactly false");
                references.Add(new AdvancedReferenceReceiptRow
                {
                    GameRelativePath = RequireSafeRelativePath(value, "gameRelativePath", requireDll: true),
                    AssemblyName = RequireString(value, "assemblyName"),
                    Length = RequirePositiveInt64(value, "length"),
                    Sha256 = RequireSha256(value, "sha256"),
                    CopyLocal = false
                });
            }
            if (references.Count == 0)
                throw new InvalidDataException("references must contain at least one row");
            if (references.Select(value => value.GameRelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != references.Count)
                throw new InvalidDataException("references contain a duplicate gameRelativePath");
            if (references.Select(value => value.AssemblyName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != references.Count)
                throw new InvalidDataException("references contain a duplicate assemblyName");

            AdvancedReferenceReceiptRow? gameAssembly = references.SingleOrDefault(value =>
                value.GameRelativePath.Equals(gameAssemblyRelativePath, StringComparison.Ordinal));
            if (gameAssembly == null ||
                !gameAssembly.Sha256.Equals(gameAssemblySha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("game assembly root fields do not match exactly one references row");
            }

            return new AdvancedReferenceReceiptProbe
            {
                Path = fullPath,
                SchemaVersion = schemaVersion,
                ReceiptKind = receiptKind,
                ReferencePolicyId = RequireString(root, "referencePolicyId"),
                ReferencePolicyVersion = policyVersion,
                ReferencePolicySha256 = policySha256,
                UniqueId = RequireString(root, "uniqueId"),
                CodeModKind = codeModKind,
                TargetFramework = targetFramework,
                GameBuildId = RequireString(root, "gameBuildId"),
                GameAssemblyRelativePath = gameAssemblyRelativePath,
                GameAssemblySha256 = gameAssemblySha256,
                ManifestPath = manifestPath,
                ManifestSha256 = RequireSha256(root, "manifestSha256"),
                EntryDllPath = entryDllPath,
                EntryDllLength = entryDllLength,
                EntryDllSha256 = RequireSha256(root, "entryDllSha256"),
                HarmonyOwner = RequireString(root, "harmonyOwner"),
                References = references.ToArray()
            };
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return new AdvancedReferenceReceiptProbe
            {
                Path = fullPath,
                Error = ex.GetType().Name + ": " + ex.Message
            };
        }
    }

    private static void RequireObject(JsonElement value, string context)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException(context + " must be a JSON object");
    }

    private static void RequireExactProperties(JsonElement value, string context, params string[] expected)
    {
        var allowed = new HashSet<string>(expected, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
                throw new InvalidDataException(context + " contains unknown field " + property.Name);
            if (!seen.Add(property.Name))
                throw new InvalidDataException(context + " contains duplicate field " + property.Name);
        }
        string[] missing = expected.Where(name => !seen.Contains(name)).ToArray();
        if (missing.Length > 0)
            throw new InvalidDataException(context + " is missing field(s): " + string.Join(", ", missing));
    }

    private static string RequireString(JsonElement root, string name)
    {
        JsonElement value = root.GetProperty(name);
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new InvalidDataException(name + " must be a non-empty string");
        return value.GetString()!;
    }

    private static int RequireInt32(JsonElement root, string name)
    {
        JsonElement value = root.GetProperty(name);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int result))
            throw new InvalidDataException(name + " must be an integer");
        return result;
    }

    private static long RequirePositiveInt64(JsonElement root, string name)
    {
        JsonElement value = root.GetProperty(name);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out long result) || result <= 0)
            throw new InvalidDataException(name + " must be a positive integer");
        return result;
    }

    private static string RequireSha256(JsonElement root, string name)
    {
        string value = RequireString(root, name);
        if (value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidDataException(name + " must be a 64-character hexadecimal SHA-256");
        return value.ToUpperInvariant();
    }

    private static string RequireSafeRelativePath(JsonElement root, string name, bool requireDll)
    {
        string value = RequireString(root, name);
        if (value.Contains('\\') || System.IO.Path.IsPathRooted(value) || value.StartsWith("/", StringComparison.Ordinal) ||
            value.Split('/').Any(segment => segment is "" or "." or ".." || segment.Contains(':')))
        {
            throw new InvalidDataException(name + " must be one normalized safe relative path");
        }
        if (requireDll && !value.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(name + " must name a DLL");
        return value;
    }
}

internal sealed class AdvancedReferenceReceiptRow
{
    public string GameRelativePath { get; init; } = string.Empty;
    public string AssemblyName { get; init; } = string.Empty;
    public long Length { get; init; }
    public string Sha256 { get; init; } = string.Empty;
    public bool CopyLocal { get; init; }
}
