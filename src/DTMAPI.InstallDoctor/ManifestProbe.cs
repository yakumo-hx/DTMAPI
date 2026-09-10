using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.InstallDoctor;

internal sealed class ManifestProbe
{
    public string Path { get; init; } = string.Empty;
    public string RootPath { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string UniqueId { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string CodeModKind { get; init; } = string.Empty;
    public string EntryDll { get; init; } = string.Empty;
    public string EntryType { get; init; } = string.Empty;
    public string MinimumDtmApiVersion { get; init; } = string.Empty;
    public bool HasExplicitType { get; init; }
    public bool HasExplicitCodeModKind { get; init; }
    public string Error { get; init; } = string.Empty;
    public PackageDependencyManifest? DependencyContract { get; init; }
    public bool NativeContractSelected { get; init; }

    public bool HasStrongDtmApiMarker =>
        UniqueId.Length > 0 &&
        ((HasExplicitType &&
          (Type.Equals("CodeMod", StringComparison.OrdinalIgnoreCase) ||
           Type.Equals("ContentPack", StringComparison.OrdinalIgnoreCase))) ||
         EntryDll.Length > 0 ||
         EntryType.Length > 0 ||
         HasExplicitCodeModKind ||
         MinimumDtmApiVersion.Length > 0);

    public static ManifestProbe Read(string path)
    {
        string fullPath = System.IO.Path.GetFullPath(path);
        string root = DeterminePackageRoot(fullPath);
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
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
            JsonElement rootElement = document.RootElement;
            if (rootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("manifest root must be a JSON object");

            int typeFields = 0;
            int codeModKindFields = 0;
            foreach (JsonProperty property in rootElement.EnumerateObject())
            {
                if (property.Name.Equals("Type", StringComparison.OrdinalIgnoreCase))
                {
                    if (!property.Name.Equals("Type", StringComparison.Ordinal))
                        throw new InvalidDataException("manifest field Type has invalid casing: " + property.Name);
                    typeFields++;
                }
                if (property.Name.Equals("CodeModKind", StringComparison.OrdinalIgnoreCase))
                {
                    if (!property.Name.Equals("CodeModKind", StringComparison.Ordinal))
                        throw new InvalidDataException("manifest field CodeModKind has invalid casing: " + property.Name);
                    codeModKindFields++;
                }
            }
            if (typeFields > 1)
                throw new InvalidDataException("manifest contains duplicate Type fields");
            if (codeModKindFields > 1)
                throw new InvalidDataException("manifest contains duplicate CodeModKind fields");

            bool hasExplicitType = rootElement.TryGetProperty("Type", out JsonElement typeElement);
            if (hasExplicitType && typeElement.ValueKind != JsonValueKind.String)
                throw new InvalidDataException("manifest field Type must be a string");
            bool hasExplicitCodeModKind = rootElement.TryGetProperty("CodeModKind", out JsonElement codeModKindElement);
            if (hasExplicitCodeModKind && codeModKindElement.ValueKind != JsonValueKind.String)
                throw new InvalidDataException("manifest field CodeModKind must be a string");

            return new ManifestProbe
            {
                Path = fullPath,
                RootPath = root,
                NativeContractSelected = rootElement.EnumerateObject().Any(property => property.Name.Equals("NativeContractVersion", StringComparison.OrdinalIgnoreCase))
                    && NativePackageContract.Select(File.ReadAllBytes(fullPath)),
                DependencyContract = rootElement.EnumerateObject().Any(property => property.Name.Equals("DependencyContractVersion", StringComparison.OrdinalIgnoreCase))
                    ? PackageDependencyContract.ReadManifest(File.ReadAllBytes(fullPath)) : null,
                Name = ReadString(rootElement, "Name"),
                UniqueId = ReadString(rootElement, "UniqueID"),
                Version = ReadString(rootElement, "Version"),
                Type = ReadString(rootElement, "Type", "CodeMod"),
                CodeModKind = ReadString(rootElement, "CodeModKind"),
                EntryDll = ReadString(rootElement, "EntryDll"),
                EntryType = ReadString(rootElement, "EntryType"),
                MinimumDtmApiVersion = ReadString(rootElement, "MinimumDTMApiVersion", ReadString(rootElement, "MinimumApiVersion")),
                HasExplicitType = hasExplicitType,
                HasExplicitCodeModKind = hasExplicitCodeModKind
            };
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return new ManifestProbe
            {
                Path = fullPath,
                RootPath = root,
                Error = ex.GetType().Name + ": " + ex.Message
            };
        }
    }

    private static string ReadString(JsonElement root, string name, string fallback = "")
    {
        if (!root.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.String)
            return fallback;
        return value.GetString() ?? fallback;
    }

    private static string DeterminePackageRoot(string manifestPath)
    {
        DirectoryInfo? directory = Directory.GetParent(manifestPath);
        if (directory == null)
            return System.IO.Path.GetDirectoryName(manifestPath) ?? manifestPath;
        if (directory.Name.Equals("DTMAPI", StringComparison.OrdinalIgnoreCase) &&
            directory.Parent?.Name.Equals("Content", StringComparison.OrdinalIgnoreCase) == true &&
            directory.Parent.Parent != null)
            return directory.Parent.Parent.FullName;
        return directory.FullName;
    }
}
