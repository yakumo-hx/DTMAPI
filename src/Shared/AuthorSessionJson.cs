using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Xml;
using System.Xml.Linq;

namespace DTMAPI.Internal
{
    // Linked into the two existing assemblies: no new Runtime dependency or public API.
    internal static class AuthorSessionJson
    {
        public static int Validate(byte[] bytes, string kind)
        {
            var quotas = new XmlDictionaryReaderQuotas { MaxDepth = 32, MaxStringContentLength = 65536, MaxArrayLength = 65536 };
            using (XmlDictionaryReader reader = JsonReaderWriterFactory.CreateJsonReader(bytes, quotas))
            {
                XElement root = XElement.Load(reader);
                if ((string?)root.Attribute("type") != "object") throw new InvalidDataException("Session JSON must be an object.");
                foreach (XElement item in root.DescendantsAndSelf().Where(e => (string?)e.Attribute("type") == "object"))
                {
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (XElement field in item.Elements())
                        if (!seen.Add(Name(field))) throw new InvalidDataException("Duplicate session JSON field.");
                }
                var fields = root.Elements().ToDictionary(Name, StringComparer.Ordinal);
                int schema = fields.TryGetValue("schemaVersion", out XElement? schemaField) ? int.Parse(schemaField.Value, System.Globalization.CultureInfo.InvariantCulture) : 1;
                if (schema == 2 && kind == "descriptor" && fields.ContainsKey("hostVersion"))
                    throw new InvalidDataException("Offline descriptors cannot declare Host identity.");
                string[] required = kind == "descriptor"
                    ? new[] { "schemaVersion", "gameRoot", "sessionId", "token", "pipeName", "createdAtUtc", "expiresAtUtc" }
                    : kind == "request"
                        ? new[] { "gameRoot", "session", "token", "requestId", "operation" }
                        : new[] { "session", "requestId", "operation", "uniqueId", "status", "code", "message", "values" };
                if (schema == 2)
                    required = required.Concat(kind == "response"
                        ? new[] { "schemaVersion", "protocolMajor", "protocolMinor", "gameRoot", "hostVersion", "apiTarget", "acceptedCapabilities", "unsupportedOptionalCapabilities" }
                        : new[] { "schemaVersion", "protocolMajor", "minimumMinor", "maximumMinor", "apiTarget", "minimumRuntimeVersion", "requiredCapabilities", "optionalCapabilities" }).ToArray();
                else if (schema == 1)
                    required = required.Concat(kind == "descriptor" ? new[] { "runtimeVersion" } : new[] { "protocol", "runtime" }).ToArray();
                foreach (string field in required)
                    if (!fields.ContainsKey(field)) throw new InvalidDataException("Missing session JSON field: " + field);
                return schema;
            }
        }

        private static string Name(XElement element) => (string?)element.Attribute("item") ?? element.Name.LocalName;
    }
}
