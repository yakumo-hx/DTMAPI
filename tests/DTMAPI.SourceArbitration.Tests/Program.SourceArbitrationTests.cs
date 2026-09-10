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
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void OfficialSourceArbitrationOneOffMatrix()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            var officialRows = new List<OfficialModInfoTestEntry>();
            var subscriptions = new List<NativeWorkshopSubscription>();

            OfficialSourceMatrixFixture localWins = CreateOfficialSourceMatrixFixture(gameDir, persistentRoot, "LocalWins", 3749100001UL);
            File.WriteAllText(Path.Combine(localWins.LocalRoot, "dtmapi.disabled"), "legacy marker must not override official enabled state");
            officialRows.Add(new OfficialModInfoTestEntry(localWins.LocalOfficialId, true, "Local", 10));
            officialRows.Add(new OfficialModInfoTestEntry(localWins.WorkshopOfficialId, false, "Workshop", 20));
            subscriptions.Add(new NativeWorkshopSubscription(localWins.WorkshopId, localWins.WorkshopRoot, false, 20, localWins.WorkshopOfficialId));

            OfficialSourceMatrixFixture workshopWins = CreateOfficialSourceMatrixFixture(gameDir, persistentRoot, "WorkshopWins", 3749100002UL);
            officialRows.Add(new OfficialModInfoTestEntry(workshopWins.LocalOfficialId, false, "Local", 20));
            officialRows.Add(new OfficialModInfoTestEntry(workshopWins.WorkshopOfficialId, true, "Workshop", 10));
            subscriptions.Add(new NativeWorkshopSubscription(workshopWins.WorkshopId, workshopWins.WorkshopRoot, true, 10, workshopWins.WorkshopOfficialId));

            OfficialSourceMatrixFixture allDisabled = CreateOfficialSourceMatrixFixture(gameDir, persistentRoot, "AllDisabled", 3749100003UL);
            officialRows.Add(new OfficialModInfoTestEntry(allDisabled.LocalOfficialId, false, "Local", -1));
            officialRows.Add(new OfficialModInfoTestEntry(allDisabled.WorkshopOfficialId, false, "Workshop", -1));
            subscriptions.Add(new NativeWorkshopSubscription(allDisabled.WorkshopId, allDisabled.WorkshopRoot, false, -1, allDisabled.WorkshopOfficialId));

            OfficialSourceMatrixFixture priorityWins = CreateOfficialSourceMatrixFixture(gameDir, persistentRoot, "PriorityWins", 3749100004UL);
            officialRows.Add(new OfficialModInfoTestEntry(priorityWins.LocalOfficialId, true, "Local", 30));
            officialRows.Add(new OfficialModInfoTestEntry(priorityWins.WorkshopOfficialId, true, "Workshop", 12));
            subscriptions.Add(new NativeWorkshopSubscription(priorityWins.WorkshopId, priorityWins.WorkshopRoot, true, 12, priorityWins.WorkshopOfficialId));

            OfficialSourceMatrixFixture priorityTie = CreateOfficialSourceMatrixFixture(gameDir, persistentRoot, "PriorityTie", 3749100005UL);
            officialRows.Add(new OfficialModInfoTestEntry(priorityTie.LocalOfficialId, true, "Local", 7));
            officialRows.Add(new OfficialModInfoTestEntry(priorityTie.WorkshopOfficialId, true, "Workshop", 7));
            subscriptions.Add(new NativeWorkshopSubscription(priorityTie.WorkshopId, priorityTie.WorkshopRoot, true, 7, priorityTie.WorkshopOfficialId));

            OfficialSourceMatrixFixture priorityMissing = CreateOfficialSourceMatrixFixture(gameDir, persistentRoot, "PriorityMissing", 3749100006UL);
            officialRows.Add(new OfficialModInfoTestEntry(priorityMissing.LocalOfficialId, true, "Local", priority: null));
            officialRows.Add(new OfficialModInfoTestEntry(priorityMissing.WorkshopOfficialId, true, "Workshop", 40));
            subscriptions.Add(new NativeWorkshopSubscription(priorityMissing.WorkshopId, priorityMissing.WorkshopRoot, true, 40, priorityMissing.WorkshopOfficialId));

            OfficialSourceMatrixFixture staleWorkshop = CreateOfficialSourceMatrixFixture(gameDir, persistentRoot, "StaleWorkshop", 3749100007UL);
            officialRows.Add(new OfficialModInfoTestEntry(staleWorkshop.LocalOfficialId, true, "Local", 1));
            officialRows.Add(new OfficialModInfoTestEntry(staleWorkshop.WorkshopOfficialId, true, "Workshop", 99));

            OfficialSourceMatrixFixture gameModsIgnored = CreateOfficialSourceMatrixFixture(gameDir, persistentRoot, "GameModsIgnored", 3749100008UL, includeWorkshop: false, includeGameMods: true);
            officialRows.Add(new OfficialModInfoTestEntry(gameModsIgnored.LocalOfficialId, true, "Local", 1));

            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                WriteOfficialModInfos(persistentRoot, officialRows.ToArray());
                var runtime = new DtmApiRuntime(
                    new FakeHost(gameDir, includeLegacyDevelopmentModSourceForTests: false),
                    new ConfigMenuRegistry());
                runtime.UpdateNativeWorkshopSubscriptions(true, "one-off-official-source-matrix", string.Empty, subscriptions);
                runtime.Start();

                IReadOnlyList<DiscoveredMod> discovered = runtime.CreateSnapshot().DiscoveredMods;
                DiscoveredMod localSelected = discovered.Single(mod => mod.Manifest.UniqueID == localWins.UniqueId);
                Assert(localSelected.Source == "Local" && PathsEqualForTest(localSelected.RootPath, localWins.LocalRoot),
                    "Matrix: Local enabled plus Workshop disabled must select Local.");

                DiscoveredMod workshopSelected = discovered.Single(mod => mod.Manifest.UniqueID == workshopWins.UniqueId);
                Assert(workshopSelected.Source == "Workshop" && PathsEqualForTest(workshopSelected.RootPath, workshopWins.WorkshopRoot),
                    "Matrix: Local disabled plus Workshop enabled must select Workshop.");

                Assert(!discovered.Any(mod => mod.Manifest.UniqueID == allDisabled.UniqueId) &&
                    !runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == allDisabled.UniqueId),
                    "Matrix: two disabled official candidates must select and load neither copy.");

                DiscoveredMod prioritySelected = discovered.Single(mod => mod.Manifest.UniqueID == priorityWins.UniqueId);
                Assert(prioritySelected.Source == "Local" && prioritySelected.OfficialPriority == 30,
                    "Matrix: multiple enabled candidates must use the unique greatest official priority, not Workshop source preference.");

                Assert(!discovered.Any(mod => mod.Manifest.UniqueID == priorityTie.UniqueId) &&
                    runtime.Diagnostics.GetWarnings().Any(warning =>
                        warning.Details.Contains("Duplicate UniqueID " + priorityTie.UniqueId, StringComparison.Ordinal) &&
                        warning.Details.Contains("enable only one copy", StringComparison.OrdinalIgnoreCase)),
                    "Matrix: tied enabled official priorities must block the duplicate and provide one-copy guidance.");

                Assert(!discovered.Any(mod => mod.Manifest.UniqueID == priorityMissing.UniqueId) &&
                    runtime.Diagnostics.GetWarnings().Any(warning =>
                        warning.Details.Contains("Duplicate UniqueID " + priorityMissing.UniqueId, StringComparison.Ordinal) &&
                        warning.Details.Contains("priority/load order is unavailable", StringComparison.OrdinalIgnoreCase)),
                    "Matrix: one missing official priority must block multiple enabled copies even when the other candidate has a numeric priority.");

                DiscoveredMod staleSelected = discovered.Single(mod => mod.Manifest.UniqueID == staleWorkshop.UniqueId);
                Assert(staleSelected.Source == "Local" && PathsEqualForTest(staleSelected.RootPath, staleWorkshop.LocalRoot),
                    "Matrix: a stale numeric Workshop directory outside the current subscription snapshot must not defeat Local.");

                DiscoveredMod gameModsSelected = discovered.Single(mod => mod.Manifest.UniqueID == gameModsIgnored.UniqueId);
                AuthorSourceSelectionDecision gameModsDecision = runtime.AuthorSourceSelectionDecisions.Single(row => row.UniqueId == gameModsIgnored.UniqueId);
                Assert(gameModsSelected.Source == "Local" && PathsEqualForTest(gameModsSelected.RootPath, gameModsIgnored.LocalRoot) &&
                    gameModsDecision.Candidates.Count == 1 &&
                    !gameModsDecision.Candidates.Any(candidate => PathsEqualForTest(candidate.RootPath, gameModsIgnored.GameModsRoot)),
                    "Matrix: a same-ID <game>/Mods tree must not enter player discovery or affect official Local selection.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
            }
        }

        private static OfficialSourceMatrixFixture CreateOfficialSourceMatrixFixture(
            string gameDir,
            string persistentRoot,
            string caseName,
            ulong workshopId,
            bool includeWorkshop = true,
            bool includeGameMods = false)
        {
            string folderName = "DTMAPI_OfficialSource_" + caseName;
            string uniqueId = "DTMAPI.Tests.OfficialSource." + caseName;
            string localRoot = Path.Combine(persistentRoot, "MODS", folderName);
            string workshopRoot = Path.Combine(gameDir, "steamapps", "workshop", "content", "2285550", workshopId.ToString(CultureInfo.InvariantCulture));
            string gameModsRoot = Path.Combine(gameDir, "Mods", folderName);
            WriteSourceAuthorityContentPack(localRoot, uniqueId, caseName + " Local");
            if (includeWorkshop)
                WriteSourceAuthorityContentPack(workshopRoot, uniqueId, caseName + " Workshop");
            if (includeGameMods)
                WriteSourceAuthorityContentPack(gameModsRoot, uniqueId, caseName + " game Mods");
            return new OfficialSourceMatrixFixture(folderName, uniqueId, workshopId, localRoot, workshopRoot, gameModsRoot);
        }

        private sealed class OfficialSourceMatrixFixture
        {
            public OfficialSourceMatrixFixture(
                string folderName,
                string uniqueId,
                ulong workshopId,
                string localRoot,
                string workshopRoot,
                string gameModsRoot)
            {
                FolderName = folderName;
                UniqueId = uniqueId;
                WorkshopId = workshopId;
                LocalRoot = localRoot;
                WorkshopRoot = workshopRoot;
                GameModsRoot = gameModsRoot;
            }

            public string FolderName { get; }
            public string UniqueId { get; }
            public ulong WorkshopId { get; }
            public string LocalRoot { get; }
            public string WorkshopRoot { get; }
            public string GameModsRoot { get; }
            public string LocalOfficialId => "Local." + FolderName;
            public string WorkshopOfficialId => "Workshop." + WorkshopId.ToString(CultureInfo.InvariantCulture);
        }
    }
}
