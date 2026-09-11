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
    // Optional markers never confer ownership. When present, their target and bytes must agree.
    internal static class AuthorPackageMarker
    {
        public const string RelativePath = PackageDependencyBundle.MarkerPath;

        public static void ValidateIfPresent(string root, string manifestPath, string uniqueId, string version,
            string packageKind, string codeModKind, string entryPath, string minimumRuntime, bool usesDependencyContract = false)
        {
            if (usesDependencyContract)
            {
                PackageDependencyBundle.Read(root, manifestPath, uniqueId, version, packageKind, codeModKind, entryPath, minimumRuntime);
                return;
            }
            string markerPath = Path.Combine(root, RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(markerPath)) return;
            try
            {
                byte[] bytes = File.ReadAllBytes(markerPath);
                if (bytes.Length > 65536) throw new InvalidDataException("Package marker exceeds its size limit.");
                int offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
                if (offset == bytes.Length) throw new InvalidDataException("Package marker is empty.");
                using (XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(bytes, offset, bytes.Length - offset,
                    new XmlDictionaryReaderQuotas { MaxDepth = 4, MaxStringContentLength = 65536, MaxArrayLength = 65536 }))
                {
                    XElement json = XElement.Load(reader);
                    if ((string?)json.Attribute("type") != "object") throw new InvalidDataException("Package marker must be an object.");
                    var fields = new Dictionary<string, XElement>(StringComparer.Ordinal);
                    foreach (XElement field in json.Elements())
                    {
                        string name = (string?)field.Attribute("item") ?? field.Name.LocalName;
                        if (fields.ContainsKey(name)) throw new InvalidDataException("Duplicate package marker field: " + name);
                        fields.Add(name, field);
                    }
                    // This filename predates the Author SDK. Existing installer/content/third-party
                    // metadata is not an SDK binding or an ownership receipt. Recognize only that
                    // bounded vocabulary; schema fields or any SDK binding field never fall back.
                    if (!fields.ContainsKey("schemaVersion") && IsLegacyMetadata(fields, uniqueId, version)) return;
                    if (!fields.TryGetValue("schemaVersion", out XElement? schema) || (string?)schema.Attribute("type") != "number" ||
                        (schema.Value != "1" && schema.Value != "2")) throw new InvalidDataException("Unsupported package marker schemaVersion.");
                    bool legacy = schema.Value == "1";
                    string[] expected = legacy
                        ? new[] { "schemaVersion", "owner", "uniqueId", "version", "packageKind", "authorSdkVersion", "targetRuntimeVersion" }
                        : new[] { "schemaVersion", "owner", "uniqueId", "version", "packageKind", "codeModKind", "authorSdkVersion", "targetDtmApiVersion", "manifestPath", "manifestSha256", "entryDllPath", "entryDllSha256", "advancedReferenceReceiptPath", "advancedReferenceReceiptSha256", "authority" };
                    if (expected.Any(name => !fields.ContainsKey(name)) || fields.Keys.Any(name => !expected.Contains(name, StringComparer.Ordinal) && !(legacy && name == "authority")))
                        throw new InvalidDataException("Package marker has missing or unknown fields.");
                    foreach (KeyValuePair<string, XElement> field in fields.Where(pair => pair.Key != "schemaVersion"))
                        if ((string?)field.Value.Attribute("type") != "string") throw new InvalidDataException("Package marker field must be a string: " + field.Key);
                    string Value(string name) => fields[name].Value;
                    if (Value("owner") != "DTMAPI" || Value("uniqueId") != uniqueId || Value("version") != version || Value("packageKind") != packageKind)
                        throw new InvalidDataException("Package marker identity does not match its manifest.");
                    string apiTarget = Value(legacy ? "targetRuntimeVersion" : "targetDtmApiVersion");
                    // Schema 1 allowed an omitted minimum; it advertised metadata only, never a byte binding.
                    string floor = legacy && string.IsNullOrEmpty(minimumRuntime) ? apiTarget : minimumRuntime;
                    if (!AuthorApiTargetCatalog.Current.TryValidateReadablePackageTarget(apiTarget, Value("authorSdkVersion"), floor, out string code, out string reason))
                        throw new InvalidDataException(code + ": " + reason);
                    if (legacy)
                    {
                        if (apiTarget != "0.5.5" || (fields.ContainsKey("authority") && Value("authority") != "metadata-only-not-an-ownership-receipt"))
                            throw new InvalidDataException("Unknown legacy package marker target or authority.");
                        return;
                    }
                    if (Value("codeModKind") != codeModKind || Value("authority") != "dtmapi-author-sdk-package-binding" ||
                        Value("advancedReferenceReceiptPath") != string.Empty || Value("advancedReferenceReceiptSha256") != string.Empty)
                        throw new InvalidDataException("Package marker kind/authority does not match a Strict or ContentPack package.");
                    RequireFile(root, Value("manifestPath"), Value("manifestSha256"), manifestPath);
                    if (packageKind == "ContentPack")
                    {
                        if (Value("entryDllPath") != string.Empty || Value("entryDllSha256") != string.Empty)
                            throw new InvalidDataException("ContentPack marker must not bind code.");
                    }
                    else RequireFile(root, Value("entryDllPath"), Value("entryDllSha256"), entryPath);
                }
            }
            catch (Exception ex) when (ex is XmlException || ex is System.Runtime.Serialization.SerializationException || ex is ArgumentException)
            { throw new InvalidDataException("Package marker could not be read: " + ex.Message, ex); }
        }

        private static bool IsLegacyMetadata(Dictionary<string, XElement> fields, string uniqueId, string version)
        {
            string[] allowed = { "owner", "packageKind", "uniqueId", "generatedBy", "updatedAt", "contentRoot", "version" };
            if (fields.Keys.Any(name => !allowed.Contains(name, StringComparer.Ordinal)) ||
                fields.Values.Any(field => (string?)field.Attribute("type") != "string") ||
                !fields.TryGetValue("uniqueId", out XElement? id) || id.Value != uniqueId) return false;
            if (fields.TryGetValue("version", out XElement? modVersion) && modVersion.Value != version) return false;
            if (fields.TryGetValue("owner", out XElement? owner) && owner.Value != "DTMAPI") return false;
            if (fields.TryGetValue("packageKind", out XElement? kind) && kind.Value != "workshop-mod" && kind.Value != "content-pack") return false;
            if (fields.TryGetValue("contentRoot", out XElement? content) && content.Value != "Content") return false;
            if (fields.TryGetValue("updatedAt", out XElement? updated) &&
                !DateTimeOffset.TryParse(updated.Value, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind, out _)) return false;
            return fields.TryGetValue("generatedBy", out XElement? generator) && !string.IsNullOrWhiteSpace(generator.Value) ||
                fields.Count == 2 && modVersion != null;
        }

        private static void RequireFile(string root, string relative, string hash, string expected)
        {
            if (string.IsNullOrEmpty(relative) || relative.Contains("\\") || relative.Contains(":") ||
                relative.Split('/').Any(part => part.Length == 0 || part == "." || part == ".."))
                throw new InvalidDataException("Package marker path is not a normalized relative path.");
            string path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!string.Equals(path, Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                throw new InvalidDataException("Package marker path does not bind the actual manifest/entry file.");
            using (SHA256 sha = SHA256.Create())
            using (Stream stream = File.OpenRead(path))
            {
                string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
                if (!string.Equals(actual, hash, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Package marker hash differs from the actual manifest/entry bytes.");
            }
        }
    }
}
