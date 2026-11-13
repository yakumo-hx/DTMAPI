using System;
using System.IO;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void DisabledDiscoveredModLocksConfigPage()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
            string dir = NewTempGameDir();
            string persistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT")
                ?? throw new InvalidOperationException("Temporary persistent root was not configured.");
            string modDir = Path.Combine(persistentRoot, "MODS", "Disabled");
            Directory.CreateDirectory(modDir);
            File.WriteAllText(Path.Combine(modDir, "manifest.json"), "{ \"Name\": \"Disabled\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.Disabled\", \"EntryDll\": \"Disabled.dll\" }");
            WriteOfficialModInfos(persistentRoot, "Local.Disabled", enabled: false);

            var menu = new ConfigMenuRegistry();
            IConfigMenuRuntime menuRuntime = menu;
            IManifest manifest = new ManifestModel
            {
                Name = "Disabled",
                Author = "DTMAPI",
                Version = "1.0.0",
                UniqueID = "DTMAPI.Tests.Disabled"
            };
            bool enabled = false;
            int saved = 0;
            int reset = 0;
            menu.Register(manifest, () => reset++, () => saved++);
            menu.AddBoolOption(manifest, () => "Enabled", () => "", () => enabled, value => enabled = value);

            var runtime = new DtmApiRuntime(
                new FakeHost(dir),
                menu);
            runtime.Start();

            IConfigMenuPage page = menuRuntime.GetPage(manifest.UniqueID) ?? throw new InvalidOperationException("Disabled page should exist.");
            Assert(page.IsLocked, "Disabled discovered mod should lock its config page.");

            menuRuntime.BeginEditing(manifest.UniqueID);
            Assert(page.Items[0].TrySetPendingValue("true", out _), "Locked page may stage text but must not apply it.");
            AssertThrows(() => menuRuntime.Save(manifest.UniqueID), "Locked page save should be rejected.");
            AssertThrows(() => menuRuntime.Reset(manifest.UniqueID), "Locked page reset should be rejected.");
            Assert(!enabled && saved == 0 && reset == 0, "Locked page actions must not mutate config.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void UnverifiedWorkshopAndGameModsCannotOverrideOfficialLocalState()
        {
            DiscoveredMod? enabledWorkshop = RunDuplicateSourceSelectionCase(
                "EnabledWorkshop",
                officialLocalEnabled: false,
                workshopEnabled: true,
                localDisabled: true,
                workshopId: 3749000001UL,
                out DtmApiRuntime enabledWorkshopRuntime);
            Assert(enabledWorkshop != null && enabledWorkshop.Source == "Local" && !enabledWorkshop.OfficialEnabled,
                "An unverified Workshop directory cannot become the selected source; the sole official Local row remains visible as disabled diagnostics.");
            Assert(!enabledWorkshopRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == "Yuuka.DTMAPI.Duplicate.EnabledWorkshop"),
                "A Workshop enabled only in stale file state must not load without current native install-path proof.");

            DiscoveredMod? enabledOfficialAndWorkshop = RunDuplicateSourceSelectionCase(
                "EnabledOfficial",
                officialLocalEnabled: true,
                workshopEnabled: true,
                localDisabled: true,
                workshopId: 3749000002UL,
                out _);
            Assert(enabledOfficialAndWorkshop != null && enabledOfficialAndWorkshop.Source == "Local" && enabledOfficialAndWorkshop.OfficialEnabled,
                "An enabled official Local candidate must win when the duplicate Workshop directory lacks current subscription-path proof.");

            DiscoveredMod? allDisabled = RunDuplicateSourceSelectionCase(
                "AllDisabled",
                officialLocalEnabled: false,
                workshopEnabled: false,
                localDisabled: true,
                workshopId: 3749000003UL,
                out DtmApiRuntime allDisabledRuntime);
            Assert(allDisabled != null && !allDisabled.OfficialEnabled &&
                !allDisabledRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == "Yuuka.DTMAPI.Duplicate.AllDisabled"),
                "All-disabled official candidates must remain unloaded even when raw duplicate directories exist.");

            DiscoveredMod? enabledLocal = RunDuplicateSourceSelectionCase(
                "EnabledLocal",
                officialLocalEnabled: false,
                workshopEnabled: false,
                localDisabled: false,
                workshopId: 3749000004UL,
                out DtmApiRuntime enabledLocalRuntime);
            Assert(enabledLocal != null && enabledLocal.Source == "Local" && !enabledLocal.OfficialEnabled &&
                !enabledLocalRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == "Yuuka.DTMAPI.Duplicate.EnabledLocal"),
                "A same-ID package under <game>/Mods must not override disabled official Local/Workshop state.");
            AuthorSourceSelectionDecision decision = enabledLocalRuntime.AuthorSourceSelectionDecisions.Single(row =>
                row.UniqueId == "Yuuka.DTMAPI.Duplicate.EnabledLocal");
            Assert(decision.Candidates.Count == 1 && decision.Candidates.All(candidate => candidate.Source == "Local" &&
                !PathsEqualForTest(candidate.RootPath, Path.Combine(enabledLocalRuntime.Paths.LegacyDevelopmentModsPath, "Yuuka_DTMAPI_Duplicate_EnabledLocal"))),
                "Only the official Local candidate remains when Workshop has no native path proof; the game Mods directory is not a candidate.");
        }

    }
}
