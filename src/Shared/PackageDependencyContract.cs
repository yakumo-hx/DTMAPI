using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;

namespace DTMAPI.Internal.Authoring
{
    internal sealed class PackageModDependency
    {
        public PackageModDependency(string id, bool required, PackageVersionRange range)
        { Id = id; Required = required; Range = range; }
        public string Id { get; }
        public bool Required { get; }
        public PackageVersionRange Range { get; }
        public bool LegacyOrderingOnly { get; set; }
    }

    internal sealed class PackageDependencyManifest
    {
        public string OwnerId { get; set; } = string.Empty;
        public IReadOnlyList<PackageModDependency> Dependencies { get; set; } = Array.Empty<PackageModDependency>();
    }

    internal sealed class PackageAssemblyIdentity
    {
        public string Name { get; set; } = string.Empty;
        public string AssemblyVersion { get; set; } = string.Empty;
        public string Culture { get; set; } = string.Empty;
        public string PublicKeyToken { get; set; } = string.Empty;
        public string Key => Name.ToUpperInvariant() + "/" + AssemblyVersion + "/" + Culture.ToUpperInvariant() + "/" + PublicKeyToken;
    }

    internal sealed class PackageAssemblyRecord
    {
        // Derived from PE for preflight only; never serialized as an inventory claim.
        internal IReadOnlyList<string> DefinedTypes = Array.Empty<string>();
        public string Path { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public PackageAssemblyIdentity Identity { get; set; } = new PackageAssemblyIdentity();
        public long Length { get; set; }
        public string Sha256 { get; set; } = string.Empty;
        public string TargetFramework { get; set; } = string.Empty;
        public IReadOnlyList<PackageAssemblyIdentity> References { get; set; } = Array.Empty<PackageAssemblyIdentity>();
        public string Distribution { get; set; } = string.Empty;
        public IReadOnlyList<string> LicenseFiles { get; set; } = Array.Empty<string>();
        public string Source { get; set; } = string.Empty;
    }

    // Linked into the existing Runtime and tools. JSON and PE facts are data; no target code executes here.
    internal static class PackageDependencyContract
    {
        public static PackageDependencyManifest? ReadManifest(byte[] bytes)
        {
            XElement root = ReadJson(bytes);
            Dictionary<string, XElement> fields = Object(root);
            var dependencies = fields.TryGetValue("Dependencies", out var rows)
                ? ReadDependencies(rows, manifest: true).ToList() : new List<PackageModDependency>();
            if (fields.TryGetValue("ContentPackFor", out var binding))
            {
                string host = Id(Text(Required(Object(binding), "HostId")));
                var existing = dependencies.FirstOrDefault(d => d.Id.Equals(host, StringComparison.OrdinalIgnoreCase));
                if (existing == null) dependencies.Add(new PackageModDependency(host, true, new PackageVersionRange("0.0.0", null, true)) { LegacyOrderingOnly = true });
                else if (!existing.Required) dependencies[dependencies.IndexOf(existing)] = new PackageModDependency(host, true, existing.Range) { LegacyOrderingOnly = existing.LegacyOrderingOnly };
            }
            return new PackageDependencyManifest
            {
                OwnerId = Id(Text(Required(fields, "UniqueID"))),
                Dependencies = dependencies
            };
        }

        public static string ResolveFile(string root, string relative)
        {
            relative = Relative(relative);
            string fullRoot = System.IO.Path.GetFullPath(root);
            string path = fullRoot;
            if ((File.GetAttributes(fullRoot) & FileAttributes.ReparsePoint) != 0) Fail("dependency-reparse-path", relative);
            foreach (string part in relative.Split('/'))
            {
                path = System.IO.Path.Combine(path, part);
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) Fail("dependency-reparse-path", relative);
            }
            if (!File.Exists(path)) Fail("dependency-file-missing", relative);
            return path;
        }

        public static string ComputeHash(byte[] bytes)
        { using (SHA256 sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }

        private static IReadOnlyList<PackageModDependency> ReadDependencies(XElement array, bool manifest)
        {
            var result = new List<PackageModDependency>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (XElement item in Array(array))
            {
                string idName = manifest ? "UniqueID" : "uniqueId", requiredName = manifest ? "Required" : "required", rangeName = manifest ? "VersionRange" : "versionRange";
                var fields = Object(item);
                string id = Id(Text(Required(fields, idName)));
                if (!ids.Add(id)) Fail("dependency-id-duplicate", id);
                bool required = fields.TryGetValue(requiredName, out var requiredField) ? Boolean(requiredField)
                    : fields.TryGetValue("IsRequired", out var alias) ? Boolean(alias) : true;
                if (fields.TryGetValue(rangeName, out var rangeField))
                {
                    var range = Object(rangeField);
                    result.Add(new PackageModDependency(id, required, new PackageVersionRange(
                        range.TryGetValue("minimumInclusive", out var minimum) ? Text(minimum) : "0.0.0",
                        range.TryGetValue("maximumExclusive", out var maximum) ? Text(maximum) : null,
                        range.TryGetValue("includePrerelease", out var prerelease) && Boolean(prerelease))));
                }
                else if (fields.TryGetValue("MinimumVersion", out var legacyMinimum) && !string.IsNullOrWhiteSpace(Text(legacyMinimum)))
                    result.Add(new PackageModDependency(id, required, new PackageVersionRange(Text(legacyMinimum), null, true)));
                else result.Add(new PackageModDependency(id, required, new PackageVersionRange("0.0.0", null, true)) { LegacyOrderingOnly = true });
            }
            return result;
        }

        internal static XElement ReadJson(byte[] bytes)
        {
            int offset = bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf ? 3 : 0;
            if (offset == bytes.Length) Fail("dependency-json-invalid", "JSON document is empty (possibly only a UTF-8 BOM).");
            try
            {
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(bytes, offset, bytes.Length - offset, System.Text.Encoding.UTF8,
                    new XmlDictionaryReaderQuotas { MaxDepth = 32, MaxArrayLength = 2097152, MaxStringContentLength = 2097152 }, null))
                {
                    if (reader.MoveToContent() != XmlNodeType.Element) Fail("dependency-json-invalid", "JSON document has no root value.");
                    return XElement.Load(reader);
                }
            }
            catch (Exception ex) when (ex is XmlException || ex is System.Runtime.Serialization.SerializationException)
            { throw new InvalidDataException("dependency-json-invalid: " + ex.Message, ex); }
        }
        internal static Dictionary<string, XElement> Object(XElement element, params string[] fields) => Object(element, fields, System.Array.Empty<string>());
        internal static Dictionary<string, XElement> Object(XElement element, string[] fields, string[] optional)
        {
            Type(element, "object");
            var result = new Dictionary<string, XElement>(StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (XElement child in element.Elements())
            { string name = Name(child); if (!seen.Add(name)) Fail("dependency-duplicate-field", name); result.Add(name, child); }
            if (fields.Length > 0 && (result.Keys.Except(fields, StringComparer.Ordinal).Any() || fields.Except(optional, StringComparer.Ordinal).Except(result.Keys, StringComparer.Ordinal).Any()))
                Fail("dependency-fields-invalid", "Missing/unknown fields; remove old MinimumVersion/IsRequired aliases when migrating.");
            return result;
        }
        internal static XElement Required(Dictionary<string, XElement> fields, string name)
        { if (!fields.TryGetValue(name, out XElement? value)) throw new InvalidDataException("dependency-field-missing: " + name); return value; }
        private static string Name(XElement element) => (string?)element.Attribute("item") ?? element.Name.LocalName;
        private static void Type(XElement element, string type) { if ((string?)element.Attribute("type") != type) Fail("dependency-field-type", Name(element) + ": " + type); }
        internal static string Text(XElement element) { Type(element, "string"); return element.Value; }
        internal static long Number(XElement element) { Type(element, "number"); if (!long.TryParse(element.Value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out long value)) throw new InvalidDataException("dependency-number-invalid"); return value; }
        internal static bool Boolean(XElement element) { Type(element, "boolean"); if (element.Value != "true" && element.Value != "false") Fail("dependency-boolean-invalid", element.Value); return element.Value == "true"; }
        internal static IEnumerable<XElement> Array(XElement element) { Type(element, "array"); return element.Elements(); }
        private static string Id(string value) => PackageDocumentFacts.Id(value);
        internal static string Relative(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.IndexOfAny(new[] { ':', '\0' }) >= 0 || value.StartsWith("/") || value.StartsWith("\\"))
                Fail("dependency-path-invalid", value);
            var parts = new List<string>();
            foreach (string part in value.Replace('\\', '/').Split('/'))
            {
                if (part.Length == 0 || part == ".") continue;
                if (part == "..")
                {
                    if (parts.Count == 0) Fail("dependency-path-outside-package", value);
                    parts.RemoveAt(parts.Count - 1);
                }
                else parts.Add(part);
            }
            if (parts.Count == 0) Fail("dependency-path-invalid", value);
            return string.Join("/", parts);
        }
        private static bool Hex(char c) => c >= '0' && c <= '9' || c >= 'a' && c <= 'f';
        internal static string Hash(string value) { if (value.Length != 64 || value.Any(c => !Hex(c))) Fail("dependency-sha256-invalid", value); return value; }
        private static void Fail(string code, string message) => throw new InvalidDataException(code + ": " + message);
    }
}
