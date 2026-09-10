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
        public PackageSemanticVersion Version { get; set; } = null!;
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

    internal sealed class PackageDependencyInventory
    {
        public const string FileName = "dtmapi-dependencies.json";
        public string OwnerId { get; set; } = string.Empty;
        public string ApiTarget { get; set; } = string.Empty;
        public string EntryPath { get; set; } = string.Empty;
        public IReadOnlyList<PackageModDependency> Dependencies { get; set; } = Array.Empty<PackageModDependency>();
        public IReadOnlyList<PackageAssemblyRecord> Assemblies { get; set; } = Array.Empty<PackageAssemblyRecord>();
    }

    // Linked into the existing Runtime and tools. JSON and PE facts are data; no target code executes here.
    internal static class PackageDependencyContract
    {
        public static PackageDependencyManifest? ReadManifest(byte[] bytes)
        {
            XElement root = ReadJson(bytes);
            XElement[] selector = root.Elements().Where(e => string.Equals(Name(e), "DependencyContractVersion", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (selector.Length == 0) return null; // Preserve the original reader and aliases for old packages.
            if (selector.Length != 1 || Name(selector[0]) != "DependencyContractVersion") Fail("dependency-contract-invalid", "Duplicate or mis-cased version selector.");
            Dictionary<string, XElement> fields = Object(root);
            if (Number(Required(fields, "DependencyContractVersion")) != 1) Fail("dependency-contract-unsupported", "Expected DependencyContractVersion=1; no legacy fallback.");
            return new PackageDependencyManifest
            {
                OwnerId = Id(Text(Required(fields, "UniqueID"))),
                Version = PackageSemanticVersion.Parse(Text(Required(fields, "Version"))),
                Dependencies = ReadDependencies(Required(fields, "Dependencies"), manifest: true)
            };
        }

        public static PackageDependencyInventory ReadInventory(byte[] bytes)
        {
            Dictionary<string, XElement> fields = Object(ReadJson(bytes), "schemaVersion", "ownerId", "apiTarget", "entryPath", "dependencies", "assemblies");
            if (Number(Required(fields, "schemaVersion")) != 1) Fail("dependency-inventory-unsupported", "Expected schemaVersion=1.");
            var records = new List<PackageAssemblyRecord>();
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (XElement item in Array(Required(fields, "assemblies")))
            {
                var value = Object(item, "path", "role", "name", "assemblyVersion", "culture", "publicKeyToken", "length", "sha256", "targetFramework", "references", "distribution", "licenseFiles", "source");
                var record = new PackageAssemblyRecord
                {
                    Path = Relative(Text(Required(value, "path"))), Role = Text(Required(value, "role")),
                    Identity = ReadIdentity(value), Length = Number(Required(value, "length")),
                    Sha256 = Hash(Text(Required(value, "sha256"))), TargetFramework = Text(Required(value, "targetFramework")),
                    References = Array(Required(value, "references")).Select(reference => ReadIdentity(Object(reference, "name", "assemblyVersion", "culture", "publicKeyToken"))).ToArray(),
                    Distribution = Text(Required(value, "distribution")),
                    LicenseFiles = Array(Required(value, "licenseFiles")).Select(file => Relative(Text(file))).ToArray(),
                    Source = Text(Required(value, "source"))
                };
                if (!paths.Add(record.Path) || !names.Add(record.Identity.Name)) Fail("dependency-assembly-duplicate", record.Path);
                if (record.Role != "entry" && record.Role != "shared-contract" && record.Role != "private-managed") Fail("dependency-role-invalid", record.Role);
                string prefix = record.Role == "shared-contract" ? "lib/shared/" : "lib/private/";
                if (record.Role != "entry" && !record.Path.StartsWith(prefix, StringComparison.Ordinal)) Fail("dependency-library-path", record.Path);
                if (!string.Equals(System.IO.Path.GetFileNameWithoutExtension(record.Path), record.Identity.Name, StringComparison.Ordinal) || !record.Path.EndsWith(".dll", StringComparison.Ordinal)) Fail("dependency-identity-filename", record.Path);
                if (record.Length <= 0 || record.TargetFramework != ".NETStandard,Version=v2.0") Fail("dependency-framework-invalid", record.Path);
                if (record.References.Select(r => r.Key).Distinct(StringComparer.Ordinal).Count() != record.References.Count) Fail("dependency-reference-duplicate", record.Path);
                if (record.Distribution != "self-authored" && record.Distribution != "licensed-third-party") Fail("dependency-distribution-invalid", record.Path);
                if (record.Distribution == "licensed-third-party" && record.LicenseFiles.Count == 0) Fail("dependency-license-missing", record.Path);
                if (string.IsNullOrWhiteSpace(record.Source) || record.Source.Contains(":\\") || record.Source.StartsWith("/", StringComparison.Ordinal)) Fail("dependency-source-invalid", record.Path);
                records.Add(record);
            }
            string entry = Text(Required(fields, "entryPath"));
            if (entry.Length == 0)
            { if (records.Count != 0) Fail("dependency-contentpack-executable", "ContentPack cannot contain assemblies."); }
            else
            { Relative(entry); if (records.Count(r => r.Role == "entry") != 1 || records.Single(r => r.Role == "entry").Path != entry) Fail("dependency-entry-mismatch", entry); }
            return new PackageDependencyInventory
            {
                OwnerId = Id(Text(Required(fields, "ownerId"))), ApiTarget = Text(Required(fields, "apiTarget")), EntryPath = entry,
                Dependencies = ReadDependencies(Required(fields, "dependencies"), manifest: false), Assemblies = records
            };
        }

        public static void RequireProjection(PackageDependencyManifest manifest, PackageDependencyInventory inventory)
        {
            if (!string.Equals(manifest.OwnerId, inventory.OwnerId, StringComparison.Ordinal)) Fail("dependency-owner-mismatch", inventory.OwnerId);
            string Key(PackageModDependency d) => d.Id.ToUpperInvariant() + "|" + d.Required + "|" + d.Range.Minimum.Text + "|" + d.Range.Maximum?.Text + "|" + d.Range.IncludePrerelease;
            if (!manifest.Dependencies.Select(Key).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(inventory.Dependencies.Select(Key).OrderBy(x => x, StringComparer.Ordinal)))
                Fail("dependency-projection-mismatch", manifest.OwnerId);
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
                var fields = Object(item, idName, requiredName, rangeName);
                string id = Id(Text(Required(fields, idName)));
                if (!ids.Add(id)) Fail("dependency-id-duplicate", id);
                var range = Object(Required(fields, rangeName), new[] { "minimumInclusive", "maximumExclusive", "includePrerelease" }, new[] { "maximumExclusive" });
                result.Add(new PackageModDependency(id, Boolean(Required(fields, requiredName)),
                    new PackageVersionRange(Text(Required(range, "minimumInclusive")), range.TryGetValue("maximumExclusive", out XElement? maximum) ? Text(maximum) : null, Boolean(Required(range, "includePrerelease")))));
            }
            return result;
        }

        private static PackageAssemblyIdentity ReadIdentity(Dictionary<string, XElement> fields)
        {
            string version = Text(Required(fields, "assemblyVersion"));
            if (!System.Version.TryParse(version, out Version? parsed) || parsed.Build < 0 || parsed.Revision < 0 || parsed.ToString() != version) Fail("dependency-clr-version-invalid", version);
            string token = Text(Required(fields, "publicKeyToken"));
            if (token.Length != 0 && (token.Length != 16 || token.Any(c => !Hex(c)))) Fail("dependency-token-invalid", token);
            string name = Text(Required(fields, "name")), culture = Text(Required(fields, "culture"));
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(new[] { '/', '\\', ':', ',' }) >= 0 || culture == "neutral" || token == "null") Fail("dependency-identity-invalid", name);
            return new PackageAssemblyIdentity { Name = name, AssemblyVersion = version, Culture = culture, PublicKeyToken = token };
        }

        internal static XElement ReadJson(byte[] bytes)
        {
            if (bytes.Length > 2 * 1024 * 1024) Fail("dependency-document-too-large", "2 MiB limit.");
            int offset = bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf ? 3 : 0;
            try
            { using (var reader = JsonReaderWriterFactory.CreateJsonReader(bytes, offset, bytes.Length - offset, System.Text.Encoding.UTF8,
                new XmlDictionaryReaderQuotas { MaxDepth = 32, MaxArrayLength = 2097152, MaxStringContentLength = 2097152 }, null)) return XElement.Load(reader); }
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
        private static bool Boolean(XElement element) { Type(element, "boolean"); if (element.Value != "true" && element.Value != "false") Fail("dependency-boolean-invalid", element.Value); return element.Value == "true"; }
        internal static IEnumerable<XElement> Array(XElement element) { Type(element, "array"); return element.Elements(); }
        private static string Id(string value) { if (string.IsNullOrWhiteSpace(value) || value.Trim() != value || value.IndexOfAny(new[] { '/', '\\', ':', '|', '\0' }) >= 0) Fail("dependency-id-invalid", value); return value; }
        internal static string Relative(string value) { if (string.IsNullOrEmpty(value) || value.IndexOfAny(new[] { '\\', ':', '\0' }) >= 0 || value.Split('/').Any(p => p.Length == 0 || p == "." || p == ".." || p.TrimEnd('.', ' ') != p)) Fail("dependency-path-invalid", value); return value; }
        private static bool Hex(char c) => c >= '0' && c <= '9' || c >= 'a' && c <= 'f';
        internal static string Hash(string value) { if (value.Length != 64 || value.Any(c => !Hex(c))) Fail("dependency-sha256-invalid", value); return value; }
        private static void Fail(string code, string message) => throw new InvalidDataException(code + ": " + message);
    }
}
