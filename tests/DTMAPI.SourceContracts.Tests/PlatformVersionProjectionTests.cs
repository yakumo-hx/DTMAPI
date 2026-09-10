using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Authoring.Contracts;
using DTMAPI.BepInExBootstrap;
using DTMAPI.Core.Runtime;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.UnitTests
{
    internal static class PlatformVersionProjectionTests
    {
        public static void RunAll(string repo)
        {
            RuntimeSourceProjectsVersionAuthority(repo);
            FirstPartySourceMetadataProjectsCatalog(repo);
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

            JsonNode catalog = ReadJson(repo, "tools/release/dtmapi-product-catalog.json");
            JsonNode source = catalog["runtime"]!["currentSourceBaseline"]!;
            ValidateProjection(expected, new VersionDimensions(Text(source, "releaseVersion"),
                Text(source, "binaryFileVersion"), Text(source, "assemblyCompatibilityIdentity")), "Catalog source");

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
                Equal(props.Descendants(row.Name).Single().Value, row.Value, "MSBuild " + row.Name);

            // The frozen compiler payload is independently owned; it is not a Host wire version.
            JsonNode schema = ReadJson(repo, "author-sdk/schemas/dtmapi-author.schema.json");
            JsonNode targets = ReadJson(repo, "author-sdk/target-catalog.json");
            ValidateAuthorTargetProjection(schema, targets, AuthorSdkContract.SdkVersion, AuthorSdkContract.TargetRuntimeVersion);
            using Stream embedded = typeof(DtmApiRuntime).Assembly.GetManifestResourceStream(AuthorApiTargetCatalog.ResourceName)!;
            Require(JsonNode.DeepEquals(targets, JsonNode.Parse(embedded)), "Compiled Core target catalog must match its tracked source.");
            AuthorApiTarget selected = AuthorApiTargetCatalog.Current.GetAvailable(Text(targets, "defaultTarget"));
            Require(AuthorApiTargetCatalog.Current.TryValidatePackageTarget(selected.ApiTarget, AuthorSdkContract.SdkVersion,
                selected.MinimumRuntimeVersion, out _, out _), "Default SDK/target combination must be accepted by the Runtime package reader.");

            // Exact published tree hashes, historical product floors and deterministic info.json bytes
            // remain checked by check-product-catalog.ps1 and check-release-contract.ps1.
            // Publication may legitimately lag source; it is never compared to current compiled DLLs here.
        }

        private static void FirstPartySourceMetadataProjectsCatalog(string repo)
        {
            JsonNode catalog = ReadJson(repo, "tools/release/dtmapi-product-catalog.json");
            JsonNode[] products = catalog["products"]!.AsArray().Where(p => Text(p!, "role") == "PublishedProduct").Select(p => p!).ToArray();
            JsonNode copy = ReadJson(repo, "tools/release/dtmapi-mod-publish-zh.json");
            Require(copy["schemaVersion"]!.GetValue<int>() == 2, "Publish copy must use the Catalog-keyed schema.");
            string[] publishedIds = products.Select(product => Text(product, "catalogId")).ToArray();
            JsonNode[] rows = copy["mods"]!.AsArray()
                .Where(p => publishedIds.Contains(Text(p!, "catalogId"))).Select(p => p!).ToArray();
            Require(products.Length > 0 && products.Length == rows.Length, "Publish text must have one row per public product.");
            foreach (JsonNode product in products)
            {
                JsonNode manifest = ReadJson(repo, Text(product, "sourceManifest"));
                JsonNode official = ReadJson(repo, Path.Combine(Text(product, "sourceRoot"), "official-info.json"));
                JsonNode[] matches = rows.Where(row => Text(row, "catalogId") == Text(product, "catalogId")).ToArray();
                Require(matches.Length == 1, "Publish text must uniquely identify " + Text(product, "catalogId"));
                ValidateProductProjection(product, manifest, official, matches[0]);
            }
        }

        private static void ValidateProductProjection(JsonNode product, JsonNode manifest, JsonNode official, JsonNode publish)
        {
            Equal(Text(manifest, "UniqueID"), Text(product, "uniqueId"), "Product identity");
            Equal(Text(manifest, "Version"), Text(product, "sourceVersion"), "Product version");
            Equal(Text(manifest, "MinimumDTMApiVersion"), Text(product, "sourceMinimumDtmApiVersion"), "Product Runtime floor");
            Equal(Text(official, "version"), Text(manifest, "Version"), "Official product version");
            Equal(Text(publish, "catalogId"), Text(product, "catalogId"), "Publish Catalog identity");
            foreach (string field in new[] { "uniqueId", "manifestName", "version", "modName", "gameDescription", "scope", "sourcePath", "workshopId" })
                Require(publish[field] == null, "Publish generated field must be projected from its owner: " + field);
            Require(!string.IsNullOrWhiteSpace(Text(publish, "steamName")) &&
                !string.IsNullOrWhiteSpace(Text(publish, "steamDescription")), "Independent Steam copy must be present.");
        }

        private static void ValidateAuthorTargetProjection(JsonNode schema, JsonNode catalog, string sdkVersion, string legacyTarget)
        {
            JsonNode legacy = schema["$defs"]!["schema1"]!["properties"]!["targetRuntimeVersion"]!;
            Equal(Text(legacy, "const"), legacyTarget, "Legacy SDK compiler target");
            JsonNode current = schema["$defs"]!["schema2"]!["properties"]!["targetDtmApiVersion"]!;
            Equal(Text(current, "type"), "string", "SDK target shape");
            Require(current["const"] == null && current["enum"] == null, "SDK target shape must route selection through the catalog.");
            string pattern = Text(current, "pattern");
            Require(new[] { "0.5.5", "0.7.0", "12.34.56" }.All(value => Regex.IsMatch(value, pattern)) &&
                new[] { "", "0.5", "0.5.5.0", "0.5.5-preview", "prefix0.5.5", "0.5.5suffix" }.All(value => !Regex.IsMatch(value, pattern)),
                "SDK target shape must accept exact numeric API targets only.");
            JsonNode[] matches = catalog["targets"]!.AsArray().Where(row => Text(row!, "apiTarget") == Text(catalog, "defaultTarget")).Select(row => row!).ToArray();
            Require(matches.Length == 1 && Text(matches[0], "state") == "available", "SDK target default must identify exactly one available payload.");
            Require(matches[0]["sdkVersions"]!.AsArray().Any(value => value!.GetValue<string>() == sdkVersion),
                "SDK target default must accept the actual SDK release.");
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

            JsonNode product = JsonNode.Parse("{\"catalogId\":\"example\",\"uniqueId\":\"Author.Example\",\"sourceVersion\":\"7.2.0\",\"sourceMinimumDtmApiVersion\":\"1.0.0\"}")!;
            JsonNode manifest = JsonNode.Parse("{\"UniqueID\":\"Author.Example\",\"Name\":\"Example\",\"Version\":\"7.2.0\",\"MinimumDTMApiVersion\":\"1.0.0\"}")!;
            JsonNode official = JsonNode.Parse("{\"version\":\"7.2.0\",\"author\":\"Author\",\"name\":\"Example\",\"description\":\"Description\"}")!;
            JsonNode publish = JsonNode.Parse("{\"catalogId\":\"example\",\"author\":\"Author\",\"steamName\":\"Steam title\",\"steamDescription\":\"Steam copy\"}")!;
            // A product release and its minimum Runtime remain independent of the current Host release.
            ValidateProductProjection(product, manifest, official, publish);
            foreach ((string Key, string Value, string Error) mutation in new[]
                     { ("Version", "2.3.4", "Product version"), ("MinimumDTMApiVersion", "2.3.4", "Product Runtime floor"), ("UniqueID", "Author.Other", "Product identity") })
            {
                JsonNode changed = manifest.DeepClone();
                changed[mutation.Key] = mutation.Value;
                Reject(() => ValidateProductProjection(product, changed, official, publish), mutation.Error);
            }
            JsonNode changedPublish = publish.DeepClone();
            changedPublish["gameDescription"] = "tampered";
            Reject(() => ValidateProductProjection(product, manifest, official, changedPublish), "Publish generated field");
            JsonNode changedOfficial = official.DeepClone();
            changedOfficial["version"] = "7.3.0";
            Reject(() => ValidateProductProjection(product, manifest, changedOfficial, publish), "Official product version");

            JsonNode targetSchema = JsonNode.Parse("{\"$defs\":{\"schema1\":{\"properties\":{\"targetRuntimeVersion\":{\"const\":\"1.2.3\"}}},\"schema2\":{\"properties\":{\"targetDtmApiVersion\":{\"type\":\"string\",\"pattern\":\"^[0-9]+\\\\.[0-9]+\\\\.[0-9]+$\"}}}}}")!;
            JsonNode targetCatalog = JsonNode.Parse("{\"defaultTarget\":\"1.2.3\",\"targets\":[{\"apiTarget\":\"1.2.3\",\"state\":\"available\",\"sdkVersions\":[\"4.5.6\"]},{\"apiTarget\":\"2.0.0\",\"state\":\"planned\",\"sdkVersions\":[\"4.5.6\"]}]}")!;
            ValidateAuthorTargetProjection(targetSchema, targetCatalog, "4.5.6", "1.2.3");
            foreach (string defaultTarget in new[] { "2.0.0", "9.9.9" })
            {
                JsonNode changed = targetCatalog.DeepClone();
                changed["defaultTarget"] = defaultTarget;
                Reject(() => ValidateAuthorTargetProjection(targetSchema, changed, "4.5.6", "1.2.3"), "SDK target default");
            }
            Reject(() => ValidateAuthorTargetProjection(targetSchema, targetCatalog, "4.5.7", "1.2.3"), "SDK target default");
            JsonNode changedSchema = targetSchema.DeepClone();
            changedSchema["$defs"]!["schema1"]!["properties"]!["targetRuntimeVersion"]!["const"] = "2.0.0";
            Reject(() => ValidateAuthorTargetProjection(changedSchema, targetCatalog, "4.5.6", "1.2.3"), "Legacy SDK compiler target");
            foreach ((string Field, string Value) mutation in new[] { ("type", "number"), ("pattern", ".*") })
            {
                changedSchema = targetSchema.DeepClone();
                changedSchema["$defs"]!["schema2"]!["properties"]!["targetDtmApiVersion"]![mutation.Field] = mutation.Value;
                Reject(() => ValidateAuthorTargetProjection(changedSchema, targetCatalog, "4.5.6", "1.2.3"), "SDK target shape");
            }
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
        private static JsonNode ReadJson(string repo, string relative) => JsonNode.Parse(File.ReadAllText(Path.Combine(repo, relative)))!;
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
