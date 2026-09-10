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
using DTMAPI.GameBridge.DolocTown;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void OilOfficialJsonOwnershipIsolatedAndProbabilityMatchesTrackedSemantics()
        {
            string repo = FindRepositoryRoot();
            string oilRoot = Path.Combine(repo, "products", "first-party", "Oil");
            string extensionPath = Path.Combine(oilRoot, "Content", "mod_tbmoditemspawnextension.json");
            string manifestPath = Path.Combine(oilRoot, "manifest.json");
            string officialInfoPath = Path.Combine(oilRoot, "official-info.json");
            string oilManifestVersion;

            using (JsonDocument manifestDocument = JsonDocument.Parse(File.ReadAllText(manifestPath)))
            {
                JsonElement manifest = manifestDocument.RootElement;
                oilManifestVersion = manifest.GetProperty("Version").GetString() ?? string.Empty;
                Assert(manifest.GetProperty("Type").GetString() == "ContentPack", "Oil must be an official JSON-only ContentPack.");
                Assert(!manifest.TryGetProperty("EntryDll", out _), "Oil ContentPack must not retain an EntryDll.");
                Assert(!manifest.TryGetProperty("MinimumDTMApiVersion", out _), "Oil official JSON must not claim a DTMAPI code contract.");
                Assert(!manifest.TryGetProperty("Dependencies", out _), "Oil official JSON must not retain code-layer dependencies.");
            }
            using (JsonDocument officialInfoDocument = JsonDocument.Parse(File.ReadAllText(officialInfoPath)))
            {
                string oilInfoVersion = officialInfoDocument.RootElement.GetProperty("version").GetString() ?? string.Empty;
                Assert(oilManifestVersion == "0.3.1-dtmapi" && oilInfoVersion == oilManifestVersion, "Oil's native info version must equal its current 0.3.1-dtmapi source manifest version; 1.0.0 remains only the blocked future target.");
            }

            Assert(!File.Exists(Path.Combine(oilRoot, "OilMod.csproj")) && !File.Exists(Path.Combine(oilRoot, "ModEntry.cs")), "Oil must not retain an empty CodeMod project or entry point.");
            Assert(!Directory.EnumerateFiles(oilRoot, "*.dll", SearchOption.AllDirectories).Any(), "Oil content-only source must not contain a DLL anywhere in its tree.");
            Assert(!Directory.EnumerateFiles(oilRoot, "*.cs", SearchOption.AllDirectories).Any() &&
                !Directory.EnumerateFiles(oilRoot, "*.csproj", SearchOption.AllDirectories).Any(),
                "Oil content-only source must not retain C# implementation or project files anywhere in its tree.");
            Assert(!File.ReadAllText(Path.Combine(repo, "DTMAPI.sln")).Contains("OilMod.csproj", StringComparison.Ordinal), "The solution must not retain the removed Oil DLL project.");
            Assert(!File.ReadAllText(Path.Combine(repo, "tools", "scripts", "build.ps1")).Contains("OilMod.csproj", StringComparison.Ordinal), "The canonical build must not expect an Oil DLL.");

            string installerSource = File.ReadAllText(Path.Combine(repo, "tools", "scripts", "install-to-game.ps1"));
            string releaseCommonSource = File.ReadAllText(Path.Combine(repo, "tools", "scripts", "release-common.ps1"));
            Assert(installerSource.Contains("must not contain DLL files anywhere under its source root", StringComparison.Ordinal) &&
                installerSource.Contains("nested Content manifests are forbidden", StringComparison.Ordinal) &&
                installerSource.Contains("staged unexpected DLL files", StringComparison.Ordinal) &&
                installerSource.Contains("must stage exactly one canonical Content/DTMAPI/manifest.json", StringComparison.Ordinal) &&
                releaseCommonSource.Contains("Assert-DtmApiManifestInfoVersionParity", StringComparison.Ordinal),
                "The content-only installer must fail closed on source/staged DLLs, nested or duplicate manifests, and explicit manifest/info version drift.");
            int contentCopyIndex = installerSource.IndexOf("Copy-DirectoryContents -Source (Split-Path -Parent $contentSource) -Destination $stagingPath -Include @('Content')", StringComparison.Ordinal);
            int canonicalManifestWriteIndex = installerSource.IndexOf("Write-JsonObject -Path $stagedManifestPath -Value $manifest", StringComparison.Ordinal);
            Assert(contentCopyIndex >= 0 && canonicalManifestWriteIndex > contentCopyIndex, "The installer must write the validated canonical manifest after copying source Content.");

            string catalogCheckerSource = File.ReadAllText(Path.Combine(repo, "tools", "scripts", "check-product-catalog.ps1"));
            Assert(catalogCheckerSource.Contains("Oil content-only recursive DLL count", StringComparison.Ordinal) &&
                catalogCheckerSource.Contains("Oil content-only CSharp/project file count", StringComparison.Ordinal) &&
                catalogCheckerSource.Contains("Oil nested Content manifest count", StringComparison.Ordinal) &&
                catalogCheckerSource.Contains("Oil publish-text current version projection", StringComparison.Ordinal) &&
                catalogCheckerSource.Contains("Oil native info current version projection", StringComparison.Ordinal),
                "Catalog validation must preserve Oil's recursively code-free, single-manifest source boundary and current version projection.");

            string smokeModules = Path.Combine(repo, "tools", "scripts", "game-smoke");
            string profileSource = File.ReadAllText(Path.Combine(smokeModules, "core", "deployment.ps1"));
            string preflightSource = File.ReadAllText(Path.Combine(smokeModules, "phases", "preflight.ps1"));
            string deploySource = File.ReadAllText(Path.Combine(smokeModules, "phases", "deploy-session.ps1"));
            string assessSource = File.ReadAllText(Path.Combine(smokeModules, "phases", "assess-evidence.ps1"));
            string restoreSource = File.ReadAllText(Path.Combine(smokeModules, "phases", "restore-session.ps1"));
            Assert(profileSource.Contains("One or more required profile/extra enabled IDs are missing from mod_infos.json; profile was not applied.", StringComparison.Ordinal) &&
                deploySource.Contains("@($officialModProfileSummary.DisabledIds) -contains 'Local.DTMAPI_Oil'", StringComparison.Ordinal) &&
                deploySource.Contains("official-mod-profile-gate.json", StringComparison.Ordinal) &&
                assessSource.Contains("-not $officialModProfileGateOk", StringComparison.Ordinal) &&
                preflightSource.Contains("-IsolateAllOfficialMods requires an explicit non-Current OfficialModProfile.", StringComparison.Ordinal) &&
                profileSource.Contains("if ($property.Name -like 'Local.*' -or $property.Name -like 'Workshop.*')", StringComparison.Ordinal) &&
                deploySource.Contains("-IsolateAll ([bool]$IsolateAllOfficialMods)", StringComparison.Ordinal) &&
                profileSource.Contains("Official Mod smoke profile apply failed and its backup could not be restored", StringComparison.Ordinal) &&
                restoreSource.Contains("Restore-SmokeOfficialModProfile -Summary $officialModProfileSummary -EvidencePath $evidence -Phase 'run-finally'", StringComparison.Ordinal),
                "Game smoke must fail closed when the requested official Mod profile is missing, incomplete, not fully isolated, Oil-on during Oil-off, or not restored.");

            using JsonDocument extensionDocument = JsonDocument.Parse(File.ReadAllText(extensionPath));
            JsonElement extensionRoot = extensionDocument.RootElement;
            Assert(extensionRoot.ValueKind == JsonValueKind.Array && extensionRoot.GetArrayLength() == 1, "Oil must publish exactly one official item-spawn extension row.");
            JsonElement extension = extensionRoot[0];
            Assert(extension.GetProperty("id").GetString() == "coal_mine_drop", "Oil must extend only the native coal_mine_drop LUT.");
            Assert(extension.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).SequenceEqual(new[] { "extra_items", "id" }, StringComparer.Ordinal), "Oil extension must not redeclare native coal/amber fields.");
            JsonElement extraItems = extension.GetProperty("extra_items");
            Assert(extraItems.ValueKind == JsonValueKind.Array && extraItems.GetArrayLength() == 1, "Oil must append exactly one item to coal_mine_drop.");
            JsonElement oil = extraItems[0];
            Assert(oil.GetProperty("item_name").GetString() == "crude_oil" &&
                oil.GetProperty("spawn_weight").GetInt32() == 25 &&
                oil.GetProperty("min_count").GetInt32() == 0 &&
                oil.GetProperty("max_count").GetInt32() == 0,
                "Oil extension must be crude_oil weight=25, min=0, max=0 (native unlimited).");
            Assert(oil.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal).SequenceEqual(new[] { "item_name", "max_count", "min_count", "spawn_weight" }, StringComparer.Ordinal), "Oil extension must contain only the selected native spawn fields.");

            string fixturePath = Path.Combine(repo, "tests", "DTMAPI.UnitTests", "Fixtures", "oil-coal-drop-semantics.v1.json");
            string fixtureText = File.ReadAllText(fixturePath);
            foreach (string forbiddenPrivateToken in new[] { "spawn_datas", "level_datas", "drop_spawn_entry", "coal_mine_drop", "amber_ore", "ISpawnLut", "Tables.cs" })
                Assert(!fixtureText.Contains(forbiddenPrivateToken, StringComparison.Ordinal), "The tracked Oil semantic fixture must remain normalized and must not copy official schema, identifiers, or reverse-source names.");

            using JsonDocument fixtureDocument = JsonDocument.Parse(fixtureText);
            JsonElement fixtureRoot = fixtureDocument.RootElement;
            Assert(fixtureRoot.GetProperty("format").GetString() == "dtmapi.synthetic-ordered-draw-semantics/v1", "The Oil probability fixture format must remain explicit and versioned.");
            JsonElement orderedDraw = fixtureRoot.GetProperty("orderedDraw");
            JsonElement[] baseEntries = orderedDraw.GetProperty("baseEntries").EnumerateArray().ToArray();
            JsonElement appendedEntry = orderedDraw.GetProperty("appendedEntry");
            Assert(baseEntries.Length == 2 &&
                baseEntries[0].GetProperty("role").GetString() == "uncapped-base" &&
                baseEntries[0].GetProperty("operationCap").ValueKind == JsonValueKind.Null &&
                baseEntries[1].GetProperty("role").GetString() == "single-cap-base" &&
                baseEntries[1].GetProperty("operationCap").GetInt32() == 1 &&
                appendedEntry.GetProperty("role").GetString() == "target-extension" &&
                appendedEntry.GetProperty("operationCap").ValueKind == JsonValueKind.Null,
                "The synthetic Oil fixture must describe one uncapped base interval, one single-cap base interval, then one uncapped target extension.");

            int uncappedBaseWeight = baseEntries[0].GetProperty("weight").GetInt32();
            int cappedBaseWeight = baseEntries[1].GetProperty("weight").GetInt32();
            int targetWeight = appendedEntry.GetProperty("weight").GetInt32();
            Assert(uncappedBaseWeight > 0 && cappedBaseWeight > 0 && targetWeight == oil.GetProperty("spawn_weight").GetInt32(),
                "The synthetic fixture must retain positive base weights and project the tracked Oil extension weight into the target role.");

            JsonElement drawRange = orderedDraw.GetProperty("drawRange");
            int minimumDraws = drawRange.GetProperty("minimum").GetInt32();
            int maximumDraws = drawRange.GetProperty("maximum").GetInt32();
            int monotonicCheckMaximum = drawRange.GetProperty("monotonicCheckMaximum").GetInt32();
            JsonElement semantics = orderedDraw.GetProperty("semantics");
            Assert(minimumDraws == 3 && maximumDraws == 4 && monotonicCheckMaximum == 8 &&
                semantics.GetProperty("replacementAppendsAfterBaseEntries").GetBoolean() &&
                semantics.GetProperty("selectedCappedIntervalFallsThrough").GetBoolean(),
                "The synthetic Oil fixture must retain the reviewed 3-4 draw range, ordered append, and capped-interval fall-through semantics.");

            double totalWeight = uncappedBaseWeight + cappedBaseWeight + targetWeight;
            double ProbabilityAtLeastOneTarget(int draws)
            {
                double noTargetCapAvailable = 1d;
                double noTargetCapConsumed = 0d;
                for (int draw = 0; draw < draws; draw++)
                {
                    double nextNoTargetCapAvailable = noTargetCapAvailable * (uncappedBaseWeight / totalWeight);
                    double nextNoTargetCapConsumed = noTargetCapAvailable * (cappedBaseWeight / totalWeight) + noTargetCapConsumed * (uncappedBaseWeight / totalWeight);
                    noTargetCapAvailable = nextNoTargetCapAvailable;
                    noTargetCapConsumed = nextNoTargetCapConsumed;
                }
                return 1d - noTargetCapAvailable - noTargetCapConsumed;
            }

            JsonElement expectedProbability = fixtureRoot.GetProperty("expectedTargetProbability");
            double threeDraws = ProbabilityAtLeastOneTarget(minimumDraws);
            double fourDraws = ProbabilityAtLeastOneTarget(maximumDraws);
            Assert(Math.Abs(threeDraws - expectedProbability.GetProperty("minimumDraws").GetDouble()) < 0.000000000001d, "The normalized ordered-draw semantics should yield the exact three-draw Oil probability, including capped-interval fall-through.");
            Assert(Math.Abs(fourDraws - expectedProbability.GetProperty("maximumDraws").GetDouble()) < 0.000000000001d, "The normalized ordered-draw semantics should yield the exact four-draw Oil probability, including capped-interval fall-through.");
            Assert(Math.Abs(((threeDraws + fourDraws) / 2d) - expectedProbability.GetProperty("uniformRangeAverage").GetDouble()) < 0.000000000001d, "The unbuffed draw-range average must use the normalized ordered algorithm rather than an independent Bernoulli approximation.");
            double previousProbability = fourDraws;
            foreach (double probability in Enumerable.Range(maximumDraws + 1, monotonicCheckMaximum - maximumDraws).Select(ProbabilityAtLeastOneTarget))
            {
                Assert(probability > previousProbability && probability < 1d, "Collection-buffed 5-8 draw paths must retain strictly increasing normalized Oil opportunity.");
                previousProbability = probability;
            }

            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Assert(bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.OilCoalDropFeature") == null &&
                bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.OilCoalDropService") == null,
                "Oil-specific GameBridge feature/service types must be removed.");

            string bridgeRoot = Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown");
            string nonFixtureBridgeSource = string.Join("\n", Directory.EnumerateFiles(bridgeRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "Smoke" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                    !path.Contains(Path.DirectorySeparatorChar + "QaHost" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                    !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
                    !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                .Select(File.ReadAllText));
            foreach (string forbidden in new[] { "OilCoalDrop", "crude_oil", "OilMod.MiningDrop", "Resources.OilCoalDrop", "rollOilDropFromCoal" })
                Assert(!nonFixtureBridgeSource.Contains(forbidden, StringComparison.OrdinalIgnoreCase), "Ordinary production GameBridge code outside the explicit Smoke/QA fixture seams must not retain Oil product token " + forbidden + ".");

            string toolHookSource = File.ReadAllText(Path.Combine(bridgeRoot, "Compatibility", "ActionCompletion", "ToolColliderHitHookBridge.cs"));
            string callbacksSource = File.ReadAllText(Path.Combine(bridgeRoot, "Hooking", "DolocTownHookCallbacks.cs"));
            Assert(toolHookSource.Contains("TryPatchPostfix", StringComparison.Ordinal) && !toolHookSource.Contains("TryPatchPrefix", StringComparison.Ordinal) && !toolHookSource.Contains("PrefixPatched", StringComparison.Ordinal), "Frozen ActionCompletion compatibility must remain Postfix-only on ToolCollider.");
            Assert(callbacksSource.Contains("ToolCollider.HandleTools.ActionCompletion", StringComparison.Ordinal) && !callbacksSource.Contains("ToolColliderHandleToolsPrefix", StringComparison.Ordinal) && !callbacksSource.Contains("OilCoalDrop", StringComparison.Ordinal), "Frozen ActionCompletion callback dispatch must remain isolated from retired Oil ownership.");

            foreach (string relativeManifest in new[]
            {
                Path.Combine("author-sdk", "samples", "api-demand", "AutoHarvest", "manifest.json"),
                Path.Combine("tests", "mod-fixtures", "qa", "CropHarvesting", "manifest.json")
            })
            {
                using JsonDocument dependencyDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(repo, relativeManifest)));
                JsonElement[] providers = dependencyDocument.RootElement.GetProperty("Dependencies").EnumerateArray()
                    .Where(dependency => dependency.GetProperty("UniqueID").GetString() == "DTMAPI.GameBridge.DolocTown")
                    .ToArray();
                Assert(providers.Length == 1 && providers[0].GetProperty("Required").GetBoolean() && providers[0].GetProperty("MinimumVersion").GetString() == "0.5.1-alpha", relativeManifest + " must declare exactly one required GameBridge provider dependency.");
            }
        }
    }
}
