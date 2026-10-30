using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using DTMAPI.Abstractions;
using DTMAPI.BepInExBootstrap;
using DTMAPI.Core.Runtime;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static class PlatformVersionProjectionTests
    {
        public static void RunAll(string repo)
        {
            RuntimeSourceProjectsVersionAuthority(repo);
            FirstPartySourceMetadataIsConsistent(repo);
            ControlledProjectionsRejectDrift();
        }

        private static void RuntimeSourceProjectsVersionAuthority(string repo)
        {
            XDocument authority = XDocument.Load(Path.Combine(repo, "tools/release/dtmapi-runtime-version.props"));
            VersionDimensions expected = ReadAuthority(authority);
            Equal(DtmApiRuntime.ApiVersion, expected.Release, "Core API version");
            Equal(DtmApiRuntime.BinaryVersion, expected.File, "Core binary version");
            foreach (Assembly assembly in new[]
                     {
                         typeof(IDtmHelper).Assembly, typeof(DtmApiRuntime).Assembly,
                         typeof(BootstrapPlugin).Assembly, typeof(DolocTownGameBridge).Assembly,
                         typeof(ConfigMenuRegistry).Assembly
                     })
            {
                ValidateProjection(expected, new VersionDimensions(
                    assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,
                    assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version,
                    assembly.GetName().Version!.ToString()), assembly.GetName().Name!);
            }

            XDocument props = XDocument.Load(Path.Combine(repo, "Directory.Build.props"));
            Require(props.Descendants("Import").Any(node =>
                ((string?)node.Attribute("Project"))?.EndsWith("tools\\release\\dtmapi-runtime-version.props", StringComparison.Ordinal) == true),
                "Shared props must import the Runtime authority.");
            foreach ((string Name, string Value) row in new[]
                     {
                         ("AssemblyVersion", "$(DtmApiAssemblyCompatibilityVersion)"),
                         ("FileVersion", "$(DtmApiBinaryFileVersion)"),
                         ("Version", "$(DtmApiReleaseVersion)"),
                         ("InformationalVersion", "$(DtmApiReleaseVersion)")
                     })
            {
                XElement[] projections = props.Descendants(row.Name).ToArray();
                Require(projections.Length > 0, "Missing MSBuild " + row.Name);
                foreach (XElement projection in projections)
                    Equal(projection.Value, row.Value, "MSBuild " + row.Name);
            }

            // Exact published tree hashes, historical product floors and deterministic info.json bytes
            // remain checked by check-product-catalog.ps1 and check-release-contract.ps1.
            // Publication may legitimately lag source; it is never compared to current compiled DLLs here.
        }

        private static void FirstPartySourceMetadataIsConsistent(string repo)
        {
            string productsRoot = Path.Combine(repo, "products/first-party");
            int checkedProducts = 0;
            foreach (string directory in Directory.EnumerateDirectories(productsRoot))
            {
                string infoPath = Path.Combine(directory, "official-info.json");
                if (!File.Exists(infoPath)) continue;
                string[] manifests = Directory.GetFiles(directory, "manifest.json", SearchOption.AllDirectories)
                    .Where(path => !path.Replace('\\', '/').Contains("/bin/") && !path.Replace('\\', '/').Contains("/obj/"))
                    .Where(path => !path.Replace('\\', '/').Contains("/qa/")) .ToArray();
                if (manifests.Length != 1) continue; // Native content packs need not have a managed manifest.
                ValidateProductProjection(JsonNode.Parse(File.ReadAllText(manifests[0]))!, JsonNode.Parse(File.ReadAllText(infoPath))!);
                checkedProducts++;
            }
            Require(checkedProducts > 0, "No managed product metadata was checked.");
        }

        private static void ValidateProductProjection(JsonNode manifest, JsonNode official)
        {
            Require(!string.IsNullOrWhiteSpace(Text(manifest, "UniqueID")), "Product identity must be present.");
            Require(!string.IsNullOrWhiteSpace(Text(official, "version")), "Official product version must be present.");
            // Manifest v2 takes the product version from native info.json; a legacy
            // Version field, if present, must still agree with that package metadata.
            if (manifest["ManifestVersion"]?.GetValue<int>() != 2 || manifest["Version"] != null)
            {
                Require(!string.IsNullOrWhiteSpace(Text(manifest, "Version")), "Product version must be present.");
                Equal(Text(official, "version"), Text(manifest, "Version"), "Official product version");
            }
            if (manifest["MinimumDTMApiVersion"] != null)
                Require(!string.IsNullOrWhiteSpace(Text(manifest, "MinimumDTMApiVersion")), "Product Runtime floor must be present.");
        }

        private static void ControlledProjectionsRejectDrift()
        {
            XDocument authority = XDocument.Parse("<Project><PropertyGroup>" +
                "<DtmApiVersionAuthoritySchema>1</DtmApiVersionAuthoritySchema>" +
                "<DtmApiReleaseVersion>2.3.4</DtmApiReleaseVersion>" +
                "<DtmApiBinaryFileVersion>2.3.4.0</DtmApiBinaryFileVersion>" +
                "<DtmApiAssemblyCompatibilityVersion>1.0.0.0</DtmApiAssemblyCompatibilityVersion>" +
                "</PropertyGroup></Project>");
            VersionDimensions expected = ReadAuthority(authority);
            ValidateProjection(expected, new VersionDimensions("2.3.4", "2.3.4.0", "1.0.0.0"), "Fixture");
            Reject(() => ValidateProjection(expected, new VersionDimensions("2.3.3", "2.3.4.0", "1.0.0.0"), "Fixture"), "Fixture release");
            Reject(() => ValidateProjection(expected, new VersionDimensions("2.3.4", "2.3.3.0", "1.0.0.0"), "Fixture"), "Fixture file");
            Reject(() => ValidateProjection(expected, new VersionDimensions("2.3.4", "2.3.4.0", "2.3.4.0"), "Fixture"), "Fixture assembly");
            authority.Descendants("DtmApiVersionAuthoritySchema").Single().Value = "99";
            Reject(() => ReadAuthority(authority), "Authority schema");

            JsonNode manifest = JsonNode.Parse("{\"UniqueID\":\"Author.Example\",\"Version\":\"7.2.0\",\"MinimumDTMApiVersion\":\"1.0.0\"}")!;
            JsonNode official = JsonNode.Parse("{\"version\":\"7.2.0\"}")!;
            // A product release and its minimum Runtime remain independent of the Host release.
            ValidateProductProjection(manifest, official);
            foreach (string key in new[] { "Version", "MinimumDTMApiVersion", "UniqueID" })
            {
                JsonNode changed = manifest.DeepClone();
                changed[key] = "";
                Reject(() => ValidateProductProjection(changed, official), key == "Version" ? "Product version" : key == "UniqueID" ? "Product identity" : "Product Runtime floor");
            }
            JsonNode changedOfficial = official.DeepClone();
            changedOfficial["version"] = "7.3.0";
            Reject(() => ValidateProductProjection(manifest, changedOfficial), "Official product version");

            JsonNode currentManifest = JsonNode.Parse("{\"ManifestVersion\":2,\"UniqueID\":\"Author.Example\",\"MinimumDTMApiVersion\":\"0.7.5\"}")!;
            ValidateProductProjection(currentManifest, official);
            changedOfficial["version"] = "";
            Reject(() => ValidateProductProjection(currentManifest, changedOfficial), "Official product version");

        }

        private static VersionDimensions ReadAuthority(XDocument document)
        {
            Equal(document.Descendants("DtmApiVersionAuthoritySchema").Single().Value, "1", "Authority schema");
            string Value(string name) => document.Descendants(name).Single().Value;
            return new VersionDimensions(Value("DtmApiReleaseVersion"), Value("DtmApiBinaryFileVersion"), Value("DtmApiAssemblyCompatibilityVersion"));
        }

        private static void ValidateProjection(VersionDimensions expected, VersionDimensions actual, string label)
        {
            Equal(actual.Release, expected.Release, label + " release");
            Equal(actual.File, expected.File, label + " file");
            Equal(actual.Assembly, expected.Assembly, label + " assembly");
        }

        private sealed record VersionDimensions(string Release, string File, string Assembly);
        private static string Text(JsonNode node, string property) => node[property]?.GetValue<string>() ?? throw new InvalidOperationException("Missing " + property);
        private static void Equal(string actual, string expected, string label) => Require(actual == expected, label + ": expected " + expected + ", got " + actual);
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static void Reject(Action action, string expected)
        {
            try { action(); }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith(expected, StringComparison.Ordinal)) { return; }
            throw new InvalidOperationException("Negative fixture did not reject " + expected);
        }
    }
}
