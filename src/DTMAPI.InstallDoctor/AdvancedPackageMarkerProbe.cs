using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DTMAPI.InstallDoctor;

internal sealed class AdvancedPackageMarkerProbe
{
    internal const string RelativePath = "Content/DTMAPI/dtmapi-package.json";

    public string Path { get; init; } = string.Empty;
    public int SchemaVersion { get; init; }
    public string Owner { get; init; } = string.Empty;
    public string UniqueId { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string PackageKind { get; init; } = string.Empty;
    public string CodeModKind { get; init; } = string.Empty;
    public string AuthorSdkVersion { get; init; } = string.Empty;
    public string TargetDtmApiVersion { get; init; } = string.Empty;
    public string ManifestPath { get; init; } = string.Empty;
    public string ManifestSha256 { get; init; } = string.Empty;
    public string EntryDllPath { get; init; } = string.Empty;
    public string EntryDllSha256 { get; init; } = string.Empty;
    public string AdvancedReferenceReceiptPath { get; init; } = string.Empty;
    public string AdvancedReferenceReceiptSha256 { get; init; } = string.Empty;
    public string Authority { get; init; } = string.Empty;
    public string Error { get; init; } = string.Empty;

    public static AdvancedPackageMarkerProbe Read(string path)
    {
        string fullPath = System.IO.Path.GetFullPath(path);
        try
        {
            using FileStream stream = new(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 32 * 1024,
                options: FileOptions.SequentialScan);
            using JsonDocument document = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow
            });
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("package marker root must be a JSON object");
            RequireExactProperties(
                root,
                "schemaVersion",
                "owner",
                "uniqueId",
                "version",
                "packageKind",
                "codeModKind",
                "authorSdkVersion",
                "targetDtmApiVersion",
                "manifestPath",
                "manifestSha256",
                "entryDllPath",
                "entryDllSha256",
                "advancedReferenceReceiptPath",
                "advancedReferenceReceiptSha256",
                "authority");

            return new AdvancedPackageMarkerProbe
            {
                Path = fullPath,
                SchemaVersion = RequireInt32(root, "schemaVersion"),
                Owner = RequireString(root, "owner"),
                UniqueId = RequireString(root, "uniqueId"),
                Version = RequireString(root, "version"),
                PackageKind = RequireString(root, "packageKind"),
                CodeModKind = RequireString(root, "codeModKind"),
                AuthorSdkVersion = RequireString(root, "authorSdkVersion"),
                TargetDtmApiVersion = RequireString(root, "targetDtmApiVersion"),
                ManifestPath = RequireSafeRelativePath(root, "manifestPath"),
                ManifestSha256 = RequireSha256(root, "manifestSha256"),
                EntryDllPath = RequireSafeRelativePath(root, "entryDllPath"),
                EntryDllSha256 = RequireSha256(root, "entryDllSha256"),
                AdvancedReferenceReceiptPath = RequireSafeRelativePath(root, "advancedReferenceReceiptPath"),
                AdvancedReferenceReceiptSha256 = RequireSha256(root, "advancedReferenceReceiptSha256"),
                Authority = RequireString(root, "authority")
            };
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return new AdvancedPackageMarkerProbe
            {
                Path = fullPath,
                Error = ex.GetType().Name + ": " + ex.Message
            };
        }
    }

    private static void RequireExactProperties(JsonElement root, params string[] expected)
    {
        var allowed = new HashSet<string>(expected, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonProperty property in root.EnumerateObject())
        {
            if (!allowed.Contains(property.Name) || !seen.Add(property.Name))
                throw new InvalidDataException("package marker contains unknown or duplicate field " + property.Name);
        }
        string[] missing = expected.Where(name => !seen.Contains(name)).ToArray();
        if (missing.Length > 0)
            throw new InvalidDataException("package marker is missing field(s): " + string.Join(", ", missing));
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

    private static string RequireSha256(JsonElement root, string name)
    {
        string value = RequireString(root, name);
        if (value.Length != 64 || value.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidDataException(name + " must be a 64-character hexadecimal SHA-256");
        return value.ToUpperInvariant();
    }

    private static string RequireSafeRelativePath(JsonElement root, string name)
    {
        string value = RequireString(root, name);
        if (value.Contains('\\') || System.IO.Path.IsPathRooted(value) || value.StartsWith("/", StringComparison.Ordinal) ||
            value.Split('/').Any(segment => segment is "" or "." or ".."))
        {
            throw new InvalidDataException(name + " must be one normalized safe relative path");
        }
        return value;
    }
}
