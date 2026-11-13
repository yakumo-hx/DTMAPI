using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using static DTMAPI.Internal.Authoring.PackageDependencyContract;

namespace DTMAPI.Internal.Authoring
{
    // Runtime and SDK share this reader. Domain schema stays opaque here.
    internal sealed class ContentPackBinding
    {
        internal const string DataDirectory = "PackData";
        public string HostId { get; private set; } = "";
        public string Schema { get; private set; } = "";
        public PackageVersionRange ContractRange { get; private set; } = null!;

        public static ContentPackBinding? Read(byte[] manifest)
        {
            XElement root = ReadJson(manifest);
            string Name(XElement element) => (string?)element.Attribute("item") ?? element.Name.LocalName;
            XElement[] selectors = root.Elements().Where(e => Name(e).Equals("ContentPackFor", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (selectors.Length == 0) return null;
            // Validate the complete selected document before the shared manifest projection.
            StrictJson.Read(new System.Text.UTF8Encoding(false, true).GetString(manifest));
            if (selectors.Length != 1 || Name(selectors[0]) != "ContentPackFor") Fail("selector-invalid", "Duplicate or mis-cased ContentPackFor.");
            var fields = Object(root);
            if (Text(Required(fields, "Type")) != "ContentPack") Fail("type-invalid", "ContentPackFor requires Type=ContentPack.");
            var binding = Object(selectors[0]);
            string host = Text(Required(binding, "HostId")), schema = Text(Required(binding, "Schema"));
            if (string.IsNullOrWhiteSpace(schema))
                Fail("schema-invalid", "Schema must identify the Host-owned format.");
            var dependencies = ReadManifest(manifest);
            if (string.IsNullOrWhiteSpace(host) || dependencies == null || dependencies.OwnerId.Equals(host, StringComparison.OrdinalIgnoreCase))
                Fail("host-dependency", "A content pack must bind a distinct, nonempty Host ID.");
            var range = Object(Required(binding, "ContractVersionRange"));
            bool includePrerelease = range.TryGetValue("includePrerelease", out XElement? prerelease) && Boolean(prerelease);
            return new ContentPackBinding
            {
                HostId = host, Schema = schema,
                ContractRange = new PackageVersionRange(Text(Required(range, "minimumInclusive")),
                    range.TryGetValue("maximumExclusive", out XElement? maximum) ? Text(maximum) : null, includePrerelease)
            };
        }

        public static void ValidatePackage(string root)
        {
            // A Host consumes PackData through its bounded file reader. Other
            // payload belongs to the native loader and is not a Host admission gate.
            if (!Directory.Exists(root)) Fail("package-missing", root);
        }

        private static void Fail(string code, string message) => throw new InvalidDataException("content-host-" + code + ": " + message);
    }
}
