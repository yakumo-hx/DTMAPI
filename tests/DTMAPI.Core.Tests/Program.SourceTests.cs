#pragma warning disable CS0618 // Frozen compatibility contracts are intentionally exercised.
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void OfficialLocalModPackagesRespectOfficialEnablement()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            string packageRoot = Path.Combine(persistentRoot, "MODS", "Yuuka_DTMAPI_Test");
            string contentRoot = Path.Combine(packageRoot, "Content", "DTMAPI");
            Directory.CreateDirectory(contentRoot);
            File.WriteAllText(
                Path.Combine(contentRoot, "manifest.json"),
                "{ \"Name\": \"Official Test\", \"Author\": \"Yuuka\", \"Version\": \"1.0.0\", \"UniqueID\": \"Yuuka.DTMAPI.Test\", \"Type\": \"ContentPack\" }",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            File.WriteAllText(
                Path.Combine(contentRoot, "item_tbitem.json"),
                "[{ \"id\": \"dtmapi_test_item\", \"sub_type\": \"material\", \"title\": { \"text\": \"测试物品\", \"english\": \"Test Item\" }, \"ui_sprite_asset\": { \"url\": \"icon_item_dtmapi_test_item\" } }]");

            string? previousRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_Test", false);
                var disabledRuntime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                disabledRuntime.Start();
                RuntimeSnapshot disabledSnapshot = disabledRuntime.CreateSnapshot();
                DiscoveredMod disabled = disabledSnapshot.DiscoveredMods.Single(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test");
                Assert(disabled.Source == "Local", "Official upload package should use Doloc Town's Local source name.");
                Assert(!disabled.OfficialEnabled, "Official disabled state should prevent loading.");
                Assert(!disabledSnapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test"), "Official-disabled package must not load.");
                IContentQueryHelper disabledContent = GetContent(disabledRuntime);
                Assert(!disabledContent.FindAssets("json").Any(a => a.RelativePath.EndsWith("item_tbitem.json", StringComparison.OrdinalIgnoreCase)), "Default content asset lookup should not expose disabled official package files.");
                Assert(!disabledContent.TryReadTextAsset(Path.Combine("Content", "DTMAPI", "item_tbitem.json"), out _), "Default text asset lookup should not read disabled official package files.");
                IDtmModStatusInfo disabledStatus = disabledRuntime.CreateDiagnosticsSnapshot().Mods.Single(m => m.UniqueID == "Yuuka.DTMAPI.Test");
                Assert(!disabledStatus.Loaded && disabledStatus.Status == "disabled" && disabledStatus.StatusCode == "disabled" && !disabledStatus.OfficialEnabled && disabledStatus.OfficialEnablementManaged && disabledStatus.EnablementReason.Contains("官方"), "Diagnostics snapshot should expose official disabled mod status and enablement reason.");

                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_Test", true);
                var enabledRuntime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                enabledRuntime.Start();
                RuntimeSnapshot enabledSnapshot = enabledRuntime.CreateSnapshot();
                Assert(enabledSnapshot.DiscoveredMods.Single(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test").OfficialEnabled, "Official enabled state should be honored.");
                Assert(enabledSnapshot.LoadedMods.Any(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test"), "Official-enabled content package should load/index.");
                IContentQueryHelper enabledContent = GetContent(enabledRuntime);
                Assert(enabledContent.FindAssets("json").Any(a => a.RelativePath.EndsWith("item_tbitem.json", StringComparison.OrdinalIgnoreCase)), "Default content asset lookup should expose enabled official package files.");
                Assert(enabledContent.TryReadTextAsset(Path.Combine("Content", "DTMAPI", "item_tbitem.json"), out string enabledText) && enabledText.Contains("dtmapi_test_item", StringComparison.OrdinalIgnoreCase), "Default text asset lookup should read enabled official package files.");
                IDtmModStatusInfo enabledStatus = enabledRuntime.CreateDiagnosticsSnapshot().Mods.Single(m => m.UniqueID == "Yuuka.DTMAPI.Test");
                Assert(enabledStatus.Loaded && enabledStatus.Status == "loaded" && enabledStatus.StatusCode == "loaded" && enabledStatus.Source == "Local", "Diagnostics snapshot should expose official loaded Mod status with the native Local source name.");

                File.Delete(Path.Combine(persistentRoot, "SAVE", "mod_infos.json"));
                var unknownRuntime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                unknownRuntime.Start();
                DiscoveredMod unknown = unknownRuntime.CreateSnapshot().DiscoveredMods.Single(m => m.Manifest.UniqueID == "Yuuka.DTMAPI.Test");
                Assert(!unknown.OfficialEnabled, "Missing official enablement data should not default to enabled.");
                Assert(unknown.EnablementReason.IndexOf("官方启用状态文件", StringComparison.OrdinalIgnoreCase) >= 0, "Missing official state should have a clear Chinese-first reason.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousRoot);
            }
        }

        private static void NativeWorkshopAuthorityAndAuthorSourceModesSelectDeterministically()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            string authorStateRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-author-state", Guid.NewGuid().ToString("N"));
            const string folderName = "Yuuka_DTMAPI_SourceAuthority";
            const string uniqueId = "Yuuka.DTMAPI.SourceAuthority";
            const ulong workshopId = 3749000020UL;

            string officialRoot = Path.Combine(persistentRoot, "MODS", folderName);
            string workshopRoot = Path.Combine(gameDir, "native-library", "installed-package", workshopId.ToString(CultureInfo.InvariantCulture));
            string localRoot = Path.Combine(gameDir, "Mods", folderName);
            WriteSourceAuthorityContentPack(officialRoot, uniqueId, "Official source");
            WriteSourceAuthorityContentPack(workshopRoot, uniqueId, "Workshop source");
            WriteSourceAuthorityContentPack(localRoot, uniqueId, "Local source");

            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            string? previousAuthorRoot = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", authorStateRoot);
                WriteOfficialModInfos(
                    persistentRoot,
                    new OfficialModInfoTestEntry("Local." + folderName, true, "Local", priority: 10),
                    new OfficialModInfoTestEntry("Workshop." + workshopId.ToString(CultureInfo.InvariantCulture), true, "Workshop", priority: 20));

                DtmApiRuntime playerRuntime = StartSourceAuthorityRuntime(gameDir, workshopId, workshopRoot, nativeSnapshotAvailable: true);
                DiscoveredMod playerSelected = playerRuntime.CreateSnapshot().DiscoveredMods.Single(mod => mod.Manifest.UniqueID == uniqueId);
                Assert(playerSelected.Source == "Workshop" && playerSelected.NativeSubscriptionVerified, "Player/Workshop mode should prefer the enabled native-subscribed Workshop source.");
                Assert(File.Exists(AuthorSourceStateStore.GetWorkshopSnapshotPath(gameDir)), "Native Workshop authority should be persisted outside packages and keyed by game root.");

                DtmApiRuntime disabledWorkshopRuntime = StartSourceAuthorityRuntime(gameDir, workshopId, workshopRoot, nativeSnapshotAvailable: true, nativeEnabled: false);
                DiscoveredMod disabledWorkshopSelected = disabledWorkshopRuntime.CreateSnapshot().DiscoveredMods.Single(mod => mod.Manifest.UniqueID == uniqueId);
                Assert(
                    disabledWorkshopSelected.Source == "Local" && disabledWorkshopSelected.OfficialEnabled && PathsEqualForTest(disabledWorkshopSelected.RootPath, officialRoot),
                    "A disabled native-subscribed Workshop duplicate must not shadow the enabled official Local candidate.");
                Assert(disabledWorkshopRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == uniqueId), "The enabled official Local candidate should remain active when Workshop is disabled.");

                WriteAuthorSourceSelectionState(gameDir, uniqueId, "LocalDevelopment", localRoot, playerReproductionActive: false);
                DtmApiRuntime localRuntime = StartSourceAuthorityRuntime(gameDir, workshopId, workshopRoot, nativeSnapshotAvailable: true);
                DiscoveredMod localSelected = localRuntime.CreateSnapshot().DiscoveredMods.Single(mod => mod.Manifest.UniqueID == uniqueId);
                Assert(localSelected.Source == "Workshop" && PathsEqualForTest(localSelected.RootPath, workshopRoot), "Legacy Local Development state must not override official enabled/load-order selection.");
                AuthorSourceSelectionDecision localDecision = localRuntime.AuthorSourceSelectionDecisions.Single(decision => decision.UniqueId == uniqueId);
                Assert(localDecision.Selected?.Source == "Workshop" && localDecision.Candidates.Count == 2, "Player source authority should retain only official Local and native-verified Workshop candidates.");

                string missingLocalRoot = Path.Combine(gameDir, "Mods", "MissingSourceAuthorityOverride");
                WriteAuthorSourceSelectionState(gameDir, uniqueId, "LocalDevelopment", missingLocalRoot, playerReproductionActive: false);
                DtmApiRuntime missingOverrideRuntime = StartSourceAuthorityRuntime(gameDir, workshopId, workshopRoot, nativeSnapshotAvailable: true);
                DiscoveredMod missingOverrideSelected = missingOverrideRuntime.CreateSnapshot().DiscoveredMods.Single(mod => mod.Manifest.UniqueID == uniqueId);
                Assert(missingOverrideSelected.Source == "Workshop", "An unavailable legacy Local Development path should leave official source arbitration unchanged.");

                WriteAuthorSourceSelectionState(gameDir, uniqueId, "LocalDevelopment", localRoot, playerReproductionActive: true);
                DtmApiRuntime reproductionRuntime = StartSourceAuthorityRuntime(gameDir, workshopId, workshopRoot, nativeSnapshotAvailable: true);
                DiscoveredMod reproductionSelected = reproductionRuntime.CreateSnapshot().DiscoveredMods.Single(mod => mod.Manifest.UniqueID == uniqueId);
                Assert(reproductionSelected.Source == "Workshop" && reproductionSelected.NativeSubscriptionVerified, "Player Reproduction should suppress all source overrides and restore native Workshop selection.");

                WriteAuthorSourceSelectionState(gameDir, uniqueId, "WorkshopValidation", workshopRoot, playerReproductionActive: false);
                DtmApiRuntime validationRuntime = StartSourceAuthorityRuntime(gameDir, workshopId, workshopRoot, nativeSnapshotAvailable: true);
                DiscoveredMod validationSelected = validationRuntime.CreateSnapshot().DiscoveredMods.Single(mod => mod.Manifest.UniqueID == uniqueId);
                Assert(validationSelected.Source == "Workshop" && validationSelected.NativeSubscriptionVerified && PathsEqualForTest(validationSelected.RootPath, workshopRoot), "Workshop Validation should select the enabled native-installed Workshop source during ordinary startup.");
                Assert(validationRuntime.LoadedMods.Any(mod => mod.Manifest.UniqueID == uniqueId), "Retained Workshop Validation state must not block an enabled, subscribed content package.");

                DtmApiRuntime disabledValidationRuntime = StartSourceAuthorityRuntime(gameDir, workshopId, workshopRoot, nativeSnapshotAvailable: true, nativeEnabled: false);
                Assert(!disabledValidationRuntime.DiscoveredMods.Any(mod => mod.Manifest.UniqueID == uniqueId), "Workshop Validation must reject a native-disabled Workshop source instead of falling back to its enabled Local duplicate.");

                DtmApiRuntime unavailableRuntime = StartSourceAuthorityRuntime(gameDir, workshopId, workshopRoot, nativeSnapshotAvailable: false);
                Assert(!unavailableRuntime.CreateSnapshot().DiscoveredMods.Any(mod => mod.Manifest.UniqueID == uniqueId), "Workshop Validation must fail closed when the native subscription snapshot is unavailable.");

                string otherGameRoot = NewTempGameDir();
                Assert(!string.Equals(
                    AuthorSourceStateStore.GetInstallationStateRoot(gameDir),
                    AuthorSourceStateStore.GetInstallationStateRoot(otherGameRoot),
                    StringComparison.OrdinalIgnoreCase),
                    "Author source state for different game roots must not share an installation journal directory.");

                WriteAuthorSourceSelectionState(gameDir, uniqueId, "LocalDevelopment", localRoot, playerReproductionActive: false, storedGameRoot: otherGameRoot);
                DtmApiRuntime mismatchedStateRuntime = StartSourceAuthorityRuntime(gameDir, workshopId, workshopRoot, nativeSnapshotAvailable: true);
                DiscoveredMod mismatchSelected = mismatchedStateRuntime.CreateSnapshot().DiscoveredMods.Single(mod => mod.Manifest.UniqueID == uniqueId);
                Assert(mismatchSelected.Source == "Workshop", "A source-state file for another game root should fail safely and must not activate its override.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", previousAuthorRoot);
            }
        }

        private static void DuplicateCodeModSourceIdentityChangeRequiresRestart()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            const string ownerId = "DTMAPI.Tests.DuplicateIdentitySwitch";
            const string packageFolder = "DTMAPI_DuplicateIdentitySwitch";
            const ulong workshopId = 3749000010UL;
            string officialId = "Local." + packageFolder;
            string workshopOfficialId = "Workshop." + workshopId.ToString(CultureInfo.InvariantCulture);
            string officialRoot = Path.Combine(persistentRoot, "MODS", packageFolder);
            string workshopRoot = Path.Combine(gameDir, "steamapps", "workshop", "content", "2285550", workshopId.ToString(CultureInfo.InvariantCulture));
            WriteDuplicateCodeProbePackage(officialRoot, ownerId, "official-source");
            WriteDuplicateCodeProbePackage(workshopRoot, ownerId, "workshop-source");
            PrepareFileSinkFailure(gameDir);

            string? previousRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                WriteOfficialModInfos(
                    persistentRoot,
                    new OfficialModInfoTestEntry(officialId, true, "Local"),
                    new OfficialModInfoTestEntry(workshopOfficialId, false, "Workshop"));
                AtomicOwnerProbeMod.EntryCount = 0;

                var runtime = new DtmApiRuntime(new ThrowingLogHost(gameDir), new ConfigMenuRegistry());
                runtime.UpdateNativeWorkshopSubscriptions(
                    available: true,
                    source: "unit-test-no-active-workshop-subscription",
                    failure: string.Empty,
                    subscriptions: Array.Empty<NativeWorkshopSubscription>());
                runtime.Start();
                DiscoveredMod initialLoaded = runtime.LoadedMods.Single(mod => mod.Manifest.UniqueID == ownerId);
                Assert(initialLoaded.Source == "Local" && initialLoaded.OfficialId == officialId && PathsEqual(initialLoaded.RootPath, officialRoot), "The duplicate-source fixture should initially load the enabled official Local identity.");
                Assert(AtomicOwnerProbeMod.EntryCount == 1 && CountCoreOwnerRootsForTest(runtime, ownerId) > 0, "The initial duplicate owner should execute Entry once and publish ordinary roots.");
                IContentAssetInfo initialAsset = GetContent(runtime).FindAssets("txt").Single(asset => asset.SourceModId == ownerId && asset.RelativePath.EndsWith("source-identity.txt", StringComparison.OrdinalIgnoreCase));
                Assert(File.ReadAllText(initialAsset.SourcePath) == "official-source" && IsPathUnder(initialAsset.SourcePath, officialRoot), "Initial Content publication should come only from the loaded official Local root.");

                WriteOfficialModInfos(
                    persistentRoot,
                    new OfficialModInfoTestEntry(officialId, false, "Local"),
                    new OfficialModInfoTestEntry(workshopOfficialId, true, "Workshop"));
                runtime.UpdateNativeWorkshopSubscriptions(
                    available: true,
                    source: "unit-test-workshop-subscription-enabled",
                    failure: string.Empty,
                    subscriptions: new[]
                    {
                        new NativeWorkshopSubscription(
                            workshopId,
                            workshopRoot,
                            nativeEnabled: true,
                            nativePriority: -1,
                            nativeOfficialId: workshopOfficialId)
                    });
                runtime.NotifyWorkshopModListChanged();

                DiscoveredMod selectedAfterRefresh = runtime.DiscoveredMods.Single(mod => mod.Manifest.UniqueID == ownerId);
                Assert(selectedAfterRefresh.Source == "Workshop" && selectedAfterRefresh.OfficialId == workshopOfficialId && PathsEqual(selectedAfterRefresh.RootPath, workshopRoot), "Refresh should select the newly enabled Workshop duplicate identity.");
                Assert(runtime.LoadedMods.Single(mod => mod.Manifest.UniqueID == ownerId).RootPath == initialLoaded.RootPath && !runtime.OwnerRequiresRestart(ownerId), "A changed official source selection must retain the admitted source and active owner in this process.");
                Assert(AtomicOwnerProbeMod.EntryCount == 1 && CountCoreOwnerRootsForTest(runtime, ownerId) > 0, "Source handoff keeps resident roots without executing replacement Entry.");
                Assert(GetContent(runtime).FindAssets("txt").Where(asset => asset.SourceModId == ownerId).All(asset => IsPathUnder(asset.SourcePath, officialRoot)), "Package resources continue to refer to the admitted source.");
                IDtmModStatusInfo restartStatus = runtime.CreateDiagnosticsSnapshot().Mods.Single(status => status.UniqueID == ownerId);
                Assert(restartStatus.StatusCode == "restart-required", "Duplicate source identity handoff should expose restart-required diagnostics.");

                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) && AtomicOwnerProbeMod.EntryCount == 1 && CountCoreOwnerRootsForTest(runtime, ownerId) > 0, "Repeated refresh preserves the resident source and never enters the replacement.");

                var restartedRuntime = new DtmApiRuntime(new ThrowingLogHost(gameDir), new ConfigMenuRegistry());
                restartedRuntime.UpdateNativeWorkshopSubscriptions(
                    available: true,
                    source: "unit-test-workshop-subscription-enabled-after-restart",
                    failure: string.Empty,
                    subscriptions: new[]
                    {
                        new NativeWorkshopSubscription(
                            workshopId,
                            workshopRoot,
                            nativeEnabled: true,
                            nativePriority: -1,
                            nativeOfficialId: workshopOfficialId)
                    });
                restartedRuntime.Start();
                DiscoveredMod restartedLoaded = restartedRuntime.LoadedMods.Single(mod => mod.Manifest.UniqueID == ownerId);
                Assert(restartedLoaded.Source == "Workshop" && restartedLoaded.OfficialId == workshopOfficialId && PathsEqual(restartedLoaded.RootPath, workshopRoot), "A clean runtime should load the selected Workshop replacement identity.");
                Assert(AtomicOwnerProbeMod.EntryCount == 2 && !restartedRuntime.OwnerRequiresRestart(ownerId), "The replacement source should execute Entry only in the clean runtime lifetime.");
                IContentAssetInfo restartedAsset = GetContent(restartedRuntime).FindAssets("txt").Single(asset => asset.SourceModId == ownerId && asset.RelativePath.EndsWith("source-identity.txt", StringComparison.OrdinalIgnoreCase));
                Assert(File.ReadAllText(restartedAsset.SourcePath) == "workshop-source" && IsPathUnder(restartedAsset.SourcePath, workshopRoot), "The clean runtime should publish Content only from the selected Workshop replacement root.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousRoot);
            }
        }

        private static void ContentPackSelectionChangesWaitForRestart()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.ReloadableContentPack";
                string modDir = CreateEnabledFixtureModDirectory(dir, "ReloadableContentPack");
                Directory.CreateDirectory(modDir);
                File.WriteAllText(
                    Path.Combine(modDir, "manifest.json"),
                    "{ \"Name\": \"Reloadable Content Pack\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + ownerId + "\", \"Type\": \"ContentPack\" }");
                File.WriteAllText(Path.Combine(modDir, "reloadable-content.txt"), "content-pack-active");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == ownerId) == 1 && !runtime.OwnerRequiresRestart(ownerId), "An enabled non-code ContentPack should publish without a restart gate.");
                Assert(CountCoreOwnerRootsForTest(runtime, ownerId) > 0 && GetContent(runtime).FindAssets("txt").Any(asset => asset.SourceModId == ownerId), "The active ContentPack should publish loaded-registry and Content roots.");

                SetFixtureModEnabled(modDir, false);
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) && CountCoreOwnerRootsForTest(runtime, ownerId) > 0, "Disabling a ContentPack changes the next startup while its current contributions remain active.");
                Assert(!runtime.OwnerRequiresRestart(ownerId), "The resident ContentPack must remain an active owner, not a closed owner.");
                Assert(GetContent(runtime).FindAssets("txt").Any(asset => asset.SourceModId == ownerId), "The resident ContentPack retains its package resources.");

                SetFixtureModEnabled(modDir, true);
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == ownerId) == 1 && !runtime.OwnerRequiresRestart(ownerId), "Re-enabling the resident pack preserves its existing contribution.");
                Assert(CountCoreOwnerRootsForTest(runtime, ownerId) > 0 &&
                    GetContent(runtime).FindAssets("txt").Count(asset => asset.SourceModId == ownerId && asset.RelativePath.EndsWith("reloadable-content.txt", StringComparison.OrdinalIgnoreCase)) == 1,
                    "Re-enabling retains one package asset without duplication.");

                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == ownerId) == 1 && !runtime.OwnerRequiresRestart(ownerId), "Repeated ContentPack refresh should remain active and idempotent.");
                string manifestPath = Path.Combine(modDir, "manifest.json");
                File.WriteAllText(manifestPath, File.ReadAllText(manifestPath).Replace("1.0.0", "2.0.0"));
                runtime.NotifyWorkshopModListChanged();
                IDtmModStatusInfo updated = runtime.CreateDiagnosticsSnapshot().Mods.Single(mod => mod.UniqueID == ownerId);
                Assert(runtime.ModRegistry.Get(ownerId)?.Version == "1.0.0" && updated.Loaded && updated.StatusCode == "restart-required",
                    "A same-source ContentPack version change preserves resident admission and requests restart.");
                runtime.NotifyRuntimeShutdown("content-pack selection test");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void WorkshopSelectionChangesRequireRestartAndPreserveResidentCode()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string gameDir = NewTempGameDir();
                const string owner = "DTMAPI.Tests.ColdStart";
                string package = WriteAtomicProbePackage(gameDir, owner, string.Empty);
                File.WriteAllText(Path.Combine(package, "resident.txt"), "resident resource");
                SetFixtureModEnabled(package, false);
                AtomicOwnerProbeMod.EntryCount = 0;
                var inactive = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                inactive.Start();
                SetFixtureModEnabled(package, true);
                inactive.NotifyWorkshopModListChanged();
                Assert(inactive.DiscoveredMods.Any(mod => mod.Manifest.UniqueID == owner) &&
                    inactive.ModRegistry.IsLoaded(owner) && AtomicOwnerProbeMod.EntryCount == 1,
                    "Enabling a package executes Entry once in the current process.");
                Assert(inactive.Content.FindAssets("txt").Any(asset => asset.SourceModId == owner),
                    "Newly loaded packages publish resources before restart.");
                inactive.NotifyRuntimeShutdown("cold-start selection test");

                AtomicOwnerProbeMod.EntryCount = 0;
                var menu = new ConfigMenuRegistry();
                IConfigMenuRuntime menuRuntime = menu;
                var resident = new DtmApiRuntime(new FakeHost(gameDir), menu);
                resident.Start();
                IUnitProbeApi api = resident.ModRegistry.GetApi<IUnitProbeApi>(owner)!;
                Assert(resident.Content.FindAssets("txt").Any(asset => asset.SourceModId == owner), "The admitted package publishes its resource.");
                int roots = CountCoreOwnerRootsForTest(resident, owner);
                Assert(api != null && AtomicOwnerProbeMod.EntryCount == 1, "Restart admits the enabled code once.");
                SetFixtureModEnabled(package, false);
                int notifications = 0;
                resident.Events.CreateProxy("SelectionObserver").Workshop.ModListChanged += (_, args) =>
                {
                    notifications++;
                    Assert(args.ModCount == resident.DiscoveredMods.Count && resident.ModRegistry.IsLoaded(owner),
                        "The event reports discovered package count and the actual resident registry.");
                };
                resident.NotifyWorkshopModListChanged();
                resident.NotifyWorkshopModListChanged();
                Assert(notifications == 2 && AtomicOwnerProbeMod.EntryCount == 1 &&
                    ReferenceEquals(api, resident.ModRegistry.GetApi<IUnitProbeApi>(owner)) &&
                    CountCoreOwnerRootsForTest(resident, owner) == roots,
                    "Official disable preserves Entry-once, the current API and all owner registrations.");
                Assert(menuRuntime.GetPage(owner)?.IsLocked == false &&
                    resident.Content.TryReadTextAsset("resident.txt", out string text) && text == "resident resource",
                    "Resident configuration and package resources remain usable until shutdown.");
                IDtmModStatusInfo status = resident.CreateDiagnosticsSnapshot().Mods.Single(mod => mod.UniqueID == owner);
                Assert(status.Loaded && !status.OfficialEnabled && status.Status == "loaded" &&
                    status.StatusCode == "restart-required" && status.Reason.Contains("重启后停用", StringComparison.Ordinal),
                    "Status distinguishes official selection from active process state.");
                resident.NotifyRuntimeShutdown("cold-start resident test");
                var restarted = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                restarted.Start();
                Assert(!restarted.ModRegistry.IsLoaded(owner) && AtomicOwnerProbeMod.EntryCount == 1,
                    "The next startup applies the disabled selection.");
                restarted.NotifyRuntimeShutdown("cold-start disabled test");
            }
            finally { RestorePersistentRoot(previousRoot); }
        }

        private static void ReservedRuntimeDependenciesSurviveTitleAndWorkshopRefresh()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.ReservedDependencies.Active";
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                RegisterReservedRuntimeProviderProbe(runtime, "DTMAPI.GameBridge.DolocTown", DtmApiRuntime.ApiVersion);
                RegisterReservedRuntimeProviderProbe(runtime, "DTMAPI.DebugConsoleHost", DtmApiRuntime.ApiVersion);
                WriteAtomicProbePackage(
                    dir,
                    ownerId,
                    ", \"Dependencies\": [" +
                    "{ \"UniqueID\": \"DTMAPI.ModConfigMenu\", \"MinimumVersion\": \"0.5.3-alpha\", \"Required\": true }," +
                    "{ \"UniqueID\": \"DTMAPI.GameBridge.DolocTown\", \"MinimumVersion\": \"0.5.3-alpha\", \"Required\": true }," +
                    "{ \"UniqueID\": \"DTMAPI.DebugConsoleHost\", \"MinimumVersion\": \"0.5.3-alpha\", \"Required\": true }]");

                AtomicOwnerProbeMod.EntryCount = 0;
                runtime.Start();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == ownerId) == 1 && AtomicOwnerProbeMod.EntryCount == 1, "An ordinary Mod should load once when all required process-lifetime reserved providers are present in the registry.");
                Assert(runtime.ModRegistry.IsLoaded("DTMAPI.ModConfigMenu") && runtime.ModRegistry.IsLoaded("DTMAPI.GameBridge.DolocTown") && runtime.ModRegistry.IsLoaded("DTMAPI.DebugConsoleHost"), "The fixture should expose all three process-lifetime reserved provider manifests through the registry.");
                int activeRoots = CountCoreOwnerRootsForTest(runtime, ownerId);
                Assert(activeRoots > 0, "The reserved-provider consumer should publish ordinary owner roots after Entry.");

                runtime.NotifyReturnedToTitle();
                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) && CountCoreOwnerRootsForTest(runtime, ownerId) == activeRoots, "ReturnedToTitle must preserve an ordinary Mod and its process-lifetime services when reserved dependencies remain registered.");
                runtime.NotifyWorkshopModListChanged();
                runtime.NotifyWorkshopModListChanged();

                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == ownerId) == 1, "Workshop refresh must not deactivate a consumer merely because process-lifetime reserved providers are absent from mod discovery.");
                Assert(AtomicOwnerProbeMod.EntryCount == 1 && !runtime.OwnerRequiresRestart(ownerId), "Reserved-provider reconciliation must neither repeat Entry nor mark the still-valid consumer restart-required.");
                Assert(CountCoreOwnerRootsForTest(runtime, ownerId) == activeRoots, "Repeated Workshop refresh should preserve the consumer's authoritative owner-root count.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void ExplicitOwnerDeactivationCascadesRequiredDependentInReverseOrder()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                const string provider = "DTMAPI.Tests.Disable.Provider";
                const string required = "DTMAPI.Tests.Disable.Required";
                const string optional = "DTMAPI.Tests.Disable.Optional";
                string providerDir = WriteAtomicProbePackage(dir, provider, string.Empty);
                WriteAtomicProbePackage(dir, required, ", \"Dependencies\": [{ \"UniqueID\": \"" + provider + "\", \"MinimumVersion\": \"1.0.0\", \"Required\": true }]");
                WriteAtomicProbePackage(dir, optional, ", \"Dependencies\": [{ \"UniqueID\": \"" + provider + "\", \"MinimumVersion\": \"1.0.0\", \"Required\": false }]");
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry(), new BindingPatchCleanup(System.Threading.Thread.CurrentThread.ManagedThreadId));
                var cleanupOrder = new OwnerCleanupOrderProbe("DTMAPI.Tests.Disable.CleanupOrder");
                runtime.RegisterModOwnerCleanupParticipant(cleanupOrder);
                AtomicOwnerProbeMod.EntryCount = 0;
                runtime.Start();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == required || mod.Manifest.UniqueID == optional) == 3 && AtomicOwnerProbeMod.EntryCount == 3, "The source-disable fixture should begin with provider and both consumers active.");

                runtime.DeactivateOwner(provider, ModOwnerCleanupReason.EntryFailed, shutdown: false);

                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == required), "Explicitly deactivating an ordinary provider should cascade deactivation to its required dependent.");
                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == optional), "Explicitly deactivating an ordinary provider should keep its optional consumer active.");
                Assert(cleanupOrder.OwnerIds.SequenceEqual(new[] { required, provider }, StringComparer.OrdinalIgnoreCase), "Explicit provider deactivation should clean the required dependent before the provider in reverse dependency order.");
                Assert(runtime.OwnerRequiresRestart(provider) && runtime.OwnerRequiresRestart(required) && !runtime.OwnerRequiresRestart(optional), "Disabled provider and required dependent should require restart while the optional consumer remains active.");
                Assert(CountCoreOwnerRootsForTest(runtime, provider) == 0 && CountCoreOwnerRootsForTest(runtime, required) == 0 && CountCoreOwnerRootsForTest(runtime, optional) > 0, "Explicit deactivation should leave zero roots for inactive owners and preserve the optional consumer roots.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void SourceRemovalPreservesResidentProvidersAndDependents()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                const string provider = "DTMAPI.Tests.Cascade.Provider";
                const string required = "DTMAPI.Tests.Cascade.Required";
                const string optional = "DTMAPI.Tests.Cascade.Optional";
                string providerDir = WriteAtomicProbePackage(dir, provider, string.Empty);
                WriteAtomicProbePackage(dir, required, ", \"Dependencies\": [{ \"UniqueID\": \"" + provider + "\", \"MinimumVersion\": \"1.0.0\", \"Required\": true }]");
                WriteAtomicProbePackage(dir, optional, ", \"Dependencies\": [{ \"UniqueID\": \"" + provider + "\", \"MinimumVersion\": \"1.0.0\", \"Required\": false }]");
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var cleanupOrder = new OwnerCleanupOrderProbe("DTMAPI.Tests.Cascade.CleanupOrder");
                runtime.RegisterModOwnerCleanupParticipant(cleanupOrder);
                runtime.Start();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == required || mod.Manifest.UniqueID == optional) == 3, "Cascade fixture should initially load provider and both dependents.");

                Directory.Delete(providerDir, recursive: true);
                runtime.NotifyWorkshopModListChanged();

                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == required || mod.Manifest.UniqueID == optional) == 3,
                    "Removing a package directory cannot unload its resident code or dependent code.");
                Assert(cleanupOrder.OwnerIds.Count == 0 && runtime.ModRegistry.GetApi<IUnitProbeApi>(provider) != null &&
                    runtime.ModRegistry.GetApi<IUnitProbeApi>(required) != null,
                    "Source removal does not close owners or remove API roots.");
                IDtmModStatusInfo status = runtime.CreateDiagnosticsSnapshot().Mods.Single(mod => mod.UniqueID == provider);
                Assert(status.Loaded && status.StatusCode == "restart-required" && !status.OfficialEnabled,
                    "A removed source remains visible as resident, with next-startup removal reported.");
                runtime.NotifyRuntimeShutdown("source removal test");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void SourceRefreshInPlaceUpgradeCannotAuthorizeResidentProviderVersion()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                const string provider = "DTMAPI.Tests.InPlaceUpgrade.Provider";
                const string existingConsumer = "DTMAPI.Tests.InPlaceUpgrade.ExistingConsumer";
                const string newConsumer = "DTMAPI.Tests.InPlaceUpgrade.NewConsumer";
                string providerDir = WriteAtomicProbePackage(dir, provider, string.Empty);
                WriteAtomicProbePackage(dir, existingConsumer, ", \"Dependencies\": [{ \"UniqueID\": \"" + provider + "\", \"MinimumVersion\": \"1.0.0\", \"Required\": true }]");
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var cleanupOrder = new OwnerCleanupOrderProbe("DTMAPI.Tests.InPlaceUpgrade.CleanupOrder");
                runtime.RegisterModOwnerCleanupParticipant(cleanupOrder);
                AtomicOwnerProbeMod.EntryCount = 0;
                runtime.Start();

                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == existingConsumer) == 2 && AtomicOwnerProbeMod.EntryCount == 2, "The in-place upgrade fixture should begin with resident provider 1.0 and its existing consumer active.");
                Assert(runtime.ModRegistry.Get(provider)?.Version == "1.0.0", "The loaded registry manifest must expose the resident provider version before refresh.");

                string manifestPath = Path.Combine(providerDir, "manifest.json");
                string versionOneManifest = File.ReadAllText(manifestPath);
                string versionTwoManifest = versionOneManifest.Replace("\"Version\": \"1.0.0\"", "\"Version\": \"2.0.0\"");
                Assert(!string.Equals(versionOneManifest, versionTwoManifest, StringComparison.Ordinal), "The fixture must update the provider manifest in its original directory.");
                File.WriteAllText(manifestPath, versionTwoManifest);
                WriteAtomicProbePackage(dir, newConsumer, ", \"Dependencies\": [{ \"UniqueID\": \"" + provider + "\", \"MinimumVersion\": \"2.0.0\", \"Required\": true }]");

                runtime.NotifyWorkshopModListChanged();

                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == existingConsumer) == 2 &&
                    !runtime.ModRegistry.IsLoaded(newConsumer) && runtime.ModRegistry.Get(provider)?.Version == "1.0.0",
                    "A changed disk manifest keeps the canonical resident version and does not admit new consumers.");
                Assert(AtomicOwnerProbeMod.EntryCount == 2 && cleanupOrder.OwnerIds.Count == 0 &&
                    !runtime.OwnerRequiresRestart(provider) && !runtime.OwnerRequiresRestart(existingConsumer),
                    "Refresh neither closes resident owners nor reruns Entry.");
                Assert(runtime.CreateDiagnosticsSnapshot().Mods.Single(mod => mod.UniqueID == provider).StatusCode == "restart-required",
                    "The selected version change is visible as a next-startup action.");
                runtime.NotifyRuntimeShutdown("resident version test");

                AtomicOwnerProbeMod.EntryCount = 0;
                var restartedRuntime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                restartedRuntime.Start();
                Assert(restartedRuntime.ModRegistry.Get(provider)?.Version == "2.0.0", "A clean runtime may publish the updated provider's 2.0 canonical manifest.");
                Assert(restartedRuntime.LoadedMods.Count(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == existingConsumer || mod.Manifest.UniqueID == newConsumer) == 3 && AtomicOwnerProbeMod.EntryCount == 3, "Only the clean runtime may enter provider 2.0 and both compatible consumers.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static DiscoveredMod? RunDuplicateSourceSelectionCase(
            string caseName,
            bool officialLocalEnabled,
            bool workshopEnabled,
            bool localDisabled,
            ulong workshopId,
            out DtmApiRuntime runtime)
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            string folderName = "Yuuka_DTMAPI_Duplicate_" + caseName;
            string uniqueId = "Yuuka.DTMAPI.Duplicate." + caseName;

            string officialContentRoot = Path.Combine(persistentRoot, "MODS", folderName, "Content", "DTMAPI");
            Directory.CreateDirectory(officialContentRoot);
            File.WriteAllText(Path.Combine(officialContentRoot, "manifest.json"), DuplicateSourceManifest("Official " + caseName, uniqueId));

            string workshopContentRoot = Path.Combine(gameDir, "steamapps", "workshop", "content", "2285550", workshopId.ToString(CultureInfo.InvariantCulture), "Content", "DTMAPI");
            Directory.CreateDirectory(workshopContentRoot);
            File.WriteAllText(Path.Combine(workshopContentRoot, "manifest.json"), DuplicateSourceManifest("Workshop " + caseName, uniqueId));

            string localRoot = Path.Combine(gameDir, "Mods", folderName);
            Directory.CreateDirectory(localRoot);
            File.WriteAllText(Path.Combine(localRoot, "manifest.json"), DuplicateSourceManifest("Local " + caseName, uniqueId));
            if (localDisabled)
                File.WriteAllText(Path.Combine(localRoot, "dtmapi.disabled"), string.Empty);

            string? previousRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                WriteOfficialModInfos(
                    persistentRoot,
                    new OfficialModInfoTestEntry("Local." + folderName, officialLocalEnabled, "Local"),
                    new OfficialModInfoTestEntry("Workshop." + workshopId.ToString(CultureInfo.InvariantCulture), workshopEnabled, "Workshop"));

                runtime = new DtmApiRuntime(
                    new FakeHost(gameDir),
                    new ConfigMenuRegistry());
                runtime.Start();
                return runtime.CreateSnapshot().DiscoveredMods.SingleOrDefault(m => string.Equals(m.Manifest.UniqueID, uniqueId, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousRoot);
            }
        }

        private static void WriteDuplicateCodeProbePackage(string rootPath, string ownerId, string sourceIdentity)
        {
            string contentRoot = Path.Combine(rootPath, "Content", "DTMAPI");
            Directory.CreateDirectory(contentRoot);
            string assemblyPath = typeof(AtomicOwnerProbeMod).Assembly.Location;
            string assemblyName = Path.GetFileName(assemblyPath);
            File.Copy(assemblyPath, Path.Combine(contentRoot, assemblyName), overwrite: true);
            File.WriteAllText(
                Path.Combine(contentRoot, "manifest.json"),
                    "{ \"Name\": \"Duplicate Code Source Probe\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + ownerId + "\", \"EntryDll\": \"Content/DTMAPI/" + assemblyName + "\", \"EntryType\": \"" + (typeof(AtomicOwnerProbeMod).FullName ?? nameof(AtomicOwnerProbeMod)) + "\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\" }");
            File.WriteAllText(Path.Combine(contentRoot, "source-identity.txt"), sourceIdentity);
        }

        private static string DuplicateSourceManifest(string name, string uniqueId)
        {
            return "{ \"Name\": \"" + name + "\", \"Author\": \"Yuuka\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + uniqueId + "\", \"Type\": \"ContentPack\" }";
        }


    }
}
