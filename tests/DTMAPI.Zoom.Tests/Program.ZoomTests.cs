#pragma warning disable CS0618 // Frozen compatibility contracts are intentionally exercised.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void ZoomProductArchitectureBoundariesRemainOrdinary()
        {
            string repo = FindRepositoryRoot();
            string productDirectory = Path.Combine(repo, "products", "first-party", "Zoom");
            Assert(Directory.Exists(productDirectory), "Zoom must live under the admitted Advanced product source root.");
            foreach (string retiredRoot in new[]
            {
                Path.Combine(repo, "first-party-mods", "ZoomMod"),
                Path.Combine(repo, "testmods", "ZoomMod")
            })
            {
                Assert(!File.Exists(Path.Combine(retiredRoot, "ZoomMod.csproj")) &&
                    !File.Exists(Path.Combine(retiredRoot, "ModEntry.cs")) &&
                    !File.Exists(Path.Combine(retiredRoot, "manifest.json")),
                    "Deleted Strict/test Zoom source must not remain as a competing production authority; ignored build artifacts alone are not source authority.");
            }

            string project = File.ReadAllText(Path.Combine(productDirectory, "DTMAPI.Zoom.csproj"));
            Assert(project.Contains("<AssemblyName>DTMAPI.Zoom</AssemblyName>", StringComparison.Ordinal) &&
                project.Contains("DTMAPI_AUTHOR_SDK_ROOT", StringComparison.Ordinal) &&
                project.Contains("DTMAPI.Author.props", StringComparison.Ordinal) &&
                !project.Contains("<ProjectReference", StringComparison.Ordinal),
                "Zoom must use the Advanced Author SDK instead of a hand-authored project-reference lane.");
            string productSource = string.Join("\n", Directory.EnumerateFiles(productDirectory, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                    !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .Select(File.ReadAllText));
            Assert(productSource.Contains("new Harmony(", StringComparison.Ordinal) &&
                productSource.Contains("dtmapi.mod.dtmapi.zoommod", StringComparison.Ordinal) &&
                productSource.Contains("SetEnvCamera(Vector2,Vector2,bool,bool,bool)", StringComparison.Ordinal) &&
                productSource.Contains("parameters[0].ParameterType.FullName ==", StringComparison.Ordinal) &&
                !productSource.Contains("ICameraViewApi", StringComparison.Ordinal) &&
                !productSource.Contains("ICameraZoomApi", StringComparison.Ordinal),
                "Advanced Zoom must own its exact ProductNative Hook and must not consume its frozen compatibility API.");
            Assert(productSource.Contains("public sealed class ModEntry : DtmMod, IDisposable", StringComparison.Ordinal) &&
                productSource.Contains("runtime.DeactivateOwner(", StringComparison.Ordinal) &&
                productSource.Contains("helper.Events.Save.SaveLoaded -=", StringComparison.Ordinal) &&
                productSource.Contains("OnSaveLoaded;", StringComparison.Ordinal) &&
                productSource.Contains("helper.Events.GameLoop.ReturnedToTitle -=", StringComparison.Ordinal) &&
                productSource.Contains("OnReturnedToTitle;", StringComparison.Ordinal) &&
                productSource.Contains("helper.Events.Input.KeybindPressed +=", StringComparison.Ordinal) &&
                productSource.Contains("OnKeybindPressed;", StringComparison.Ordinal) &&
                !productSource.Contains("helper.Events.GameLoop.UpdateTicked += OnUpdateTicked", StringComparison.Ordinal),
                "Advanced Zoom must preserve Loader/config owner cleanup and event-driven input without a permanent updater.");
            Assert(CountTextOccurrences(productSource, "RegisterKeybind(") == 2 &&
                !productSource.Contains("RegisterApi<", StringComparison.Ordinal),
                "Zoom must retain exactly two owner-bound input registrations and expose no public provider.");
            Assert(productSource.Contains("public bool Enabled { get; set; } = true", StringComparison.Ordinal) &&
                productSource.Contains("public double MaxViewScale { get; set; } = 4d", StringComparison.Ordinal) &&
                productSource.Contains("public double Step { get; set; } = 0.25d", StringComparison.Ordinal) &&
                productSource.Contains("\"Equals, KeypadPlus\"", StringComparison.Ordinal) &&
                productSource.Contains("\"Minus, KeypadMinus\"", StringComparison.Ordinal),
                "Advanced Zoom must preserve its enabled/range/step and canonical +/- config defaults.");

            string manifest = File.ReadAllText(Path.Combine(productDirectory, "manifest.json"));
            string officialInfo = File.ReadAllText(Path.Combine(productDirectory, "official-info.json"));
            string author = File.ReadAllText(Path.Combine(productDirectory, "dtmapi.author.json"));
            Assert(manifest.Contains("\"Version\": \"1.0.0\"", StringComparison.Ordinal) &&
                manifest.Contains("\"UniqueID\": \"DTMAPI.ZoomMod\"", StringComparison.Ordinal) &&
                manifest.Contains("\"EntryDll\": \"DTMAPI.Zoom.dll\"", StringComparison.Ordinal) &&
                manifest.Contains("\"MinimumDTMApiVersion\": \"0.5.5\"", StringComparison.Ordinal) &&
                manifest.Contains("\"CodeModKind\": \"Advanced\"", StringComparison.Ordinal) &&
                author.Contains("\"referencePolicyId\": \"doloctown-23762374-zoom-v1\"", StringComparison.Ordinal) &&
                author.Contains("\"0Harmony\"", StringComparison.Ordinal) &&
                author.Contains("\"Assembly-CSharp\"", StringComparison.Ordinal),
                "Zoom identity and native references must be generated through its exact admitted Advanced policy.");
            Assert(officialInfo.Contains("\"version\": \"1.0.0\"", StringComparison.Ordinal),
                "Current Zoom source metadata must project the admitted 1.0.0 product version.");
            string[] i18nFiles = Directory.GetFiles(Path.Combine(productDirectory, "i18n"), "*.json", SearchOption.TopDirectoryOnly)
                .Select(path => Path.GetFileName(path) ?? string.Empty)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Assert(i18nFiles.SequenceEqual(new[] { "english.json", "schinese.json" }, StringComparer.OrdinalIgnoreCase), "Zoom should preserve its English and Simplified Chinese translation assets.");
            foreach (string i18nFile in i18nFiles)
            {
                string translation = File.ReadAllText(Path.Combine(productDirectory, "i18n", i18nFile));
                foreach (string key in new[] { "mod.loaded", "config.enabled.name", "config.maxScale.name", "config.step.name", "config.increaseKey.name", "config.decreaseKey.name", "config.status" })
                    Assert(translation.Contains("\"" + key + "\"", StringComparison.Ordinal), "Zoom translation " + i18nFile + " should preserve key " + key + ".");
            }

            string solution = File.ReadAllText(Path.Combine(repo, "DTMAPI.sln"));
            string build = File.ReadAllText(Path.Combine(repo, "tools", "scripts", "build.ps1"));
            using JsonDocument catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(repo, "tools", "release", "dtmapi-product-catalog.json")));
            JsonElement zoom = catalog.RootElement.GetProperty("products").EnumerateArray()
                .Single(product => product.GetProperty("catalogId").GetString() == "zoom");
            using JsonDocument publish = JsonDocument.Parse(File.ReadAllText(Path.Combine(repo, "tools", "release", "dtmapi-mod-publish-zh.json")));
            JsonElement zoomPublish = publish.RootElement.GetProperty("mods").EnumerateArray()
                .Single(product => product.GetProperty("catalogId").GetString() == "zoom");
            Assert(!solution.Contains("first-party-mods\\ZoomMod", StringComparison.Ordinal) &&
                !build.Contains("first-party-mods\\ZoomMod", StringComparison.Ordinal),
                "Solution/build authorities must not retain the deleted Strict Zoom tree; Advanced products are Catalog/SDK-built rather than hand-added to the solution.");
            Assert(zoom.GetProperty("sourceRoot").GetString() == "products/first-party/Zoom" &&
                zoom.GetProperty("productionSourceRoot").GetString() == "products/first-party/Zoom/src" &&
                zoom.GetProperty("sourceManifest").GetString() == "products/first-party/Zoom/manifest.json" &&
                zoom.GetProperty("project").GetString() == "DTMAPI.Zoom" &&
                zoom.GetProperty("codeModKind").GetString() == "Advanced" &&
                zoom.GetProperty("productionBuildAuthority").GetString() == "DTMAPI Author SDK build/pack/deploy",
                "Catalog/Author SDK metadata must remain the Advanced Zoom build authority.");
            Assert(zoom.GetProperty("officialFolder").GetString() == "DTMAPI_Zoom" &&
                zoom.GetProperty("sourceDll").GetString() == "DTMAPI.Zoom.dll" &&
                zoom.GetProperty("packageDll").GetString() == "DTMAPI.Zoom.dll" &&
                zoom.GetProperty("uniqueId").GetString() == "DTMAPI.ZoomMod" &&
                zoom.GetProperty("referencePolicyId").GetString() == "doloctown-23762374-zoom-v1" &&
                zoom.GetProperty("canonicalHarmonyOwner").GetString() == "dtmapi.mod.dtmapi.zoommod",
                "Zoom release wiring must preserve identity while using the Advanced product source.");
            Assert(!string.IsNullOrWhiteSpace(zoomPublish.GetProperty("steamName").GetString()) &&
                !string.IsNullOrWhiteSpace(zoomPublish.GetProperty("steamDescription").GetString()) &&
                !zoomPublish.TryGetProperty("uniqueId", out _) &&
                !zoomPublish.TryGetProperty("officialInfoPath", out _),
                "Zoom publish wording must bind the exact Catalog row without owning a second copy of product identity or source paths.");
        }
    }
}
