using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace DTMAPI.Internal.Authoring
{
    // Pure file/format facts shared by the runtime and optional tools. No load,
    // dependency closure, hash or package admission decision is made here.
    internal static class PackageDocumentFacts
    {
        internal static string Id(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim() != value || value.IndexOfAny(new[] { '/', '\\', ':', '|', '\0' }) >= 0)
                throw new InvalidDataException("dependency-id-invalid: " + value);
            return value;
        }

        internal static string? FindManifest(string root) => new[] {
            Path.Combine(root, "dtmapi.manifest.json"), Path.Combine(root, "manifest.json"),
            Path.Combine(root, "Content", "DTMAPI", "manifest.json")
        }.FirstOrDefault(File.Exists);

        internal static IReadOnlyDictionary<string, string> ReadInfo(byte[] bytes, out string warning)
        {
            var fields = new Dictionary<string, string>(StringComparer.Ordinal);
            warning = "";
            try
            {
                string json = new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF');
                byte[] compatible = Encoding.UTF8.GetBytes(RemoveLineComments(json));
                using (var reader = JsonReaderWriterFactory.CreateJsonReader(compatible, XmlDictionaryReaderQuotas.Max))
                {
                    reader.MoveToContent();
                    XElement root = XElement.Load(reader);
                    foreach (XElement field in root.Elements())
                    {
                        string? kind = (string?)field.Attribute("type");
                        if (kind == "string" || kind == "number") fields[(string?)field.Attribute("item") ?? field.Name.LocalName] = field.Value;
                    }
                }
            }
            catch (Exception ex) when (ex is XmlException || ex is SerializationException || ex is DecoderFallbackException)
            { warning = "info.json display metadata could not be read: " + ex.Message; }
            return fields;
        }

        internal static string RemoveLineComments(string json)
        {
            var builder = new StringBuilder(json.Length);
            bool inString = false, escaped = false;
            for (int index = 0; index < json.Length; index++)
            {
                char current = json[index];
                if (inString)
                {
                    builder.Append(current);
                    if (escaped) escaped = false;
                    else if (current == '\\') escaped = true;
                    else if (current == '"') inString = false;
                    continue;
                }
                if (current == '"') { inString = true; builder.Append(current); continue; }
                if (current == '/' && index + 1 < json.Length && json[index + 1] == '/')
                {
                    index += 2;
                    while (index < json.Length && json[index] != '\r' && json[index] != '\n') index++;
                    if (index < json.Length) builder.Append(json[index]);
                    continue;
                }
                builder.Append(current);
            }
            return builder.ToString();
        }
    }
}
