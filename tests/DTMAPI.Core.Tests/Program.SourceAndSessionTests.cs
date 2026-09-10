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
                Assert(disabledRuntime.GetIndexedContentItem("dtmapi_test_item") == null, "Default content item lookup should not return disabled official content.");
                IContentItemInfo? disabledAnyItem = disabledRuntime.GetAnyIndexedContentItem("dtmapi_test_item");
                Assert(disabledAnyItem == null, "All-content item lookup must not parse or expose disabled official gameplay content.");
                Assert(!disabledRuntime.GetAllIndexedContentItems().Any(i => i.ItemId == "dtmapi_test_item"), "All-content list must not retain item-level rows from disabled official content.");
                Assert(!disabledRuntime.GetIndexedContentItems().Any(i => i.ItemId == "dtmapi_test_item"), "Default content list should include enabled content only.");
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
                IContentItemInfo? enabledItem = enabledRuntime.GetIndexedContentItem("dtmapi_test_item");
                Assert(enabledItem != null && enabledItem.Enabled && enabledItem.SourceKind == "DTMAPI" && enabledItem.SourceId == "Local.Yuuka_DTMAPI_Test", "Default content item lookup should expose enabled DTMAPI official-local content.");
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

        private static void AuthorFileTreeDigestMatchesFrozenCrossToolVectors()
        {
            string root = Path.Combine(Path.GetTempPath(), "DTMAPI-file-tree-v1", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                Assert(
                    AuthorFileTreeDigest.Compute(root) == "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855",
                    "The empty DTMAPI-FileTree-SHA256-v1 vector should remain frozen across Runtime and Author SDK implementations.");

                File.WriteAllText(Path.Combine(root, "a.txt"), "A", new UTF8Encoding(false));
                string nested = Path.Combine(root, "sub");
                Directory.CreateDirectory(nested);
                File.WriteAllBytes(Path.Combine(nested, "β.bin"), new byte[] { 0x00, 0x01, 0x02, 0xFF });
                Assert(
                    AuthorFileTreeDigest.Compute(root) == "8E7C6E58D4982A1C5D749524A8A56A4C2171D7301144EE5640ABAC7AE74CB4CD",
                    "The mixed text/binary DTMAPI-FileTree-SHA256-v1 vector should remain frozen across Runtime and Author SDK implementations.");

                File.WriteAllBytes(Path.Combine(nested, "β.bin"), new byte[] { 0x00, 0x01, 0x02, 0xFE });
                Assert(
                    AuthorFileTreeDigest.Compute(root) != "8E7C6E58D4982A1C5D749524A8A56A4C2171D7301144EE5640ABAC7AE74CB4CD",
                    "Changing file content should change the frozen source-tree digest.");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
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
            string workshopRoot = Path.Combine(gameDir, "steamapps", "workshop", "content", "2285550", workshopId.ToString(CultureInfo.InvariantCulture));
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
                Assert(localSelected.SelectionReason.Contains("mode=LocalDevelopment", StringComparison.Ordinal), "Selected source should retain its machine-inspectable source-mode decision reason.");
                AuthorSourceSelectionDecision localDecision = localRuntime.AuthorSourceSelectionDecisions.Single(decision => decision.UniqueId == uniqueId);
                Assert(localDecision.Selected?.Source == "Workshop" && localDecision.Candidates.Count == 2, "Player source authority should retain only official Local and native-verified Workshop candidates.");
                Assert(localRuntime.Diagnostics.GetWarnings().Any(warning => warning.Details.Contains("recovery-only", StringComparison.OrdinalIgnoreCase)), "Legacy Local Development state should be diagnosed as recovery-only.");

                string missingLocalRoot = Path.Combine(gameDir, "Mods", "MissingSourceAuthorityOverride");
                WriteAuthorSourceSelectionState(gameDir, uniqueId, "LocalDevelopment", missingLocalRoot, playerReproductionActive: false);
                DtmApiRuntime missingOverrideRuntime = StartSourceAuthorityRuntime(gameDir, workshopId, workshopRoot, nativeSnapshotAvailable: true);
                DiscoveredMod missingOverrideSelected = missingOverrideRuntime.CreateSnapshot().DiscoveredMods.Single(mod => mod.Manifest.UniqueID == uniqueId);
                Assert(missingOverrideSelected.Source == "Workshop", "An unavailable legacy Local Development path should leave official source arbitration unchanged.");
                Assert(missingOverrideRuntime.Diagnostics.GetWarnings().Any(warning => warning.Details.Contains("recovery-only", StringComparison.OrdinalIgnoreCase)), "Ignored legacy Local Development state should be explicit in diagnostics.");

                WriteAuthorSourceSelectionState(gameDir, uniqueId, "LocalDevelopment", localRoot, playerReproductionActive: true);
                DtmApiRuntime reproductionRuntime = StartSourceAuthorityRuntime(gameDir, workshopId, workshopRoot, nativeSnapshotAvailable: true);
                DiscoveredMod reproductionSelected = reproductionRuntime.CreateSnapshot().DiscoveredMods.Single(mod => mod.Manifest.UniqueID == uniqueId);
                Assert(reproductionSelected.Source == "Workshop" && reproductionSelected.NativeSubscriptionVerified, "Player Reproduction should suppress all source overrides and restore native Workshop selection.");

                WriteAuthorSourceSelectionState(gameDir, uniqueId, "WorkshopValidation", workshopRoot, playerReproductionActive: false);
                DtmApiRuntime validationRuntime = StartSourceAuthorityRuntime(gameDir, workshopId, workshopRoot, nativeSnapshotAvailable: true);
                Assert(!validationRuntime.CreateSnapshot().DiscoveredMods.Any(mod => mod.Manifest.UniqueID == uniqueId), "Workshop Validation must fail closed without an active startup-authorized author session.");
                Assert(validationRuntime.Diagnostics.GetWarnings().Any(warning => warning.Details.Contains("no active startup-authorized author session", StringComparison.OrdinalIgnoreCase)), "Workshop Validation should diagnose the missing explicit author session.");

                DtmApiRuntime unavailableRuntime = StartSourceAuthorityRuntime(gameDir, workshopId, workshopRoot, nativeSnapshotAvailable: false);
                Assert(!unavailableRuntime.CreateSnapshot().DiscoveredMods.Any(mod => mod.Manifest.UniqueID == uniqueId), "Workshop Validation must fail closed when the native subscription snapshot is unavailable.");
                Assert(unavailableRuntime.Diagnostics.GetWarnings().Any(warning =>
                    warning.Owner == "DTMAPI.ModScanner" &&
                    warning.Details.Contains("Workshop Validation blocked", StringComparison.OrdinalIgnoreCase)),
                    "Workshop Validation fail-closed behavior should remain visible in diagnostics.");

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
                Assert(mismatchedStateRuntime.Diagnostics.GetWarnings().Any(warning =>
                    warning.Owner == "DTMAPI.AuthorSource" &&
                    warning.Details.Contains("gameRoot does not match", StringComparison.OrdinalIgnoreCase)),
                    "Rejected source state should be reported as an author-source diagnostic.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", previousAuthorRoot);
            }
        }

        private static void ExplicitAuthorSessionSurvivesInitialStartupTitleBoundary()
        {
            string gameDir = NewTempGameDir();
            string authorStateRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-author-state", Guid.NewGuid().ToString("N"));
            string? previousAuthorRoot = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", authorStateRoot);
                string sessionId = Guid.NewGuid().ToString("N");
                string token = new string('A', 48);
                WriteAuthorSessionDescriptor(gameDir, sessionId, token, AuthorSessionProtocol.CreatePipeName(gameDir, sessionId));

                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                runtime.Start();
                Assert(runtime.IsAuthorSessionActive, "The startup descriptor should create an explicit author session before the native startup title transition.");

                runtime.NotifyReturnedToTitle();
                Assert(runtime.IsAuthorSessionActive, "The first startup ReturnHome with no save and no authenticated request must not make an explicitly prepared session unusable before the author can connect.");

                runtime.NotifyReturnedToTitle();
                Assert(!runtime.IsAuthorSessionActive, "A later ReturnedToTitle boundary must still close and release the retained explicit author session.");
            }
            finally
            {
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
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) && runtime.OwnerRequiresRestart(ownerId), "A loaded code owner whose RootPath/Source/OfficialId identity changes must deactivate and remain restart-required in the same process.");
                Assert(AtomicOwnerProbeMod.EntryCount == 1 && CountCoreOwnerRootsForTest(runtime, ownerId) == 0, "Duplicate source handoff must clean all old-owner roots without executing Entry from the replacement source.");
                Assert(!GetContent(runtime).FindAssets("txt").Any(asset => asset.SourceModId == ownerId), "Same-process source handoff must publish neither stale old-source Content nor replacement-source Content before restart.");
                IDtmModStatusInfo restartStatus = runtime.CreateDiagnosticsSnapshot().Mods.Single(status => status.UniqueID == ownerId);
                Assert(restartStatus.StatusCode == "restart-required", "Duplicate source identity handoff should expose restart-required diagnostics.");

                runtime.NotifyWorkshopModListChanged();
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) && AtomicOwnerProbeMod.EntryCount == 1 && CountCoreOwnerRootsForTest(runtime, ownerId) == 0, "Repeated same-process refresh must not re-enter or republish the replacement duplicate source.");

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

        private static void NonCodeContentPackCanDisableAndRepublishInProcess()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.ReloadableContentPack";
                string modDir = Path.Combine(dir, "Mods", "ReloadableContentPack");
                Directory.CreateDirectory(modDir);
                File.WriteAllText(
                    Path.Combine(modDir, "manifest.json"),
                    "{ \"Name\": \"Reloadable Content Pack\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + ownerId + "\", \"Type\": \"ContentPack\" }");
                File.WriteAllText(Path.Combine(modDir, "reloadable-content.txt"), "content-pack-active");

                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                runtime.Start();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == ownerId) == 1 && !runtime.OwnerRequiresRestart(ownerId), "An enabled non-code ContentPack should publish without a restart gate.");
                Assert(CountCoreOwnerRootsForTest(runtime, ownerId) > 0 && GetContent(runtime).FindAssets("txt").Any(asset => asset.SourceModId == ownerId), "The active ContentPack should publish loaded-registry and Content roots.");

                string disabledMarker = Path.Combine(modDir, "dtmapi.disabled");
                File.WriteAllText(disabledMarker, "disable non-code owner");
                runtime.NotifyWorkshopModListChanged();
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) && CountCoreOwnerRootsForTest(runtime, ownerId) == 0, "Disabling a non-code ContentPack should immediately remove all owner roots and Content publication.");
                Assert(!runtime.OwnerRequiresRestart(ownerId), "A non-code owner with zero remaining roots should not require restart.");
                Assert(!GetContent(runtime).FindAssets("txt").Any(asset => asset.SourceModId == ownerId), "Disabled ContentPack assets must not remain in the active Content query root.");

                File.Delete(disabledMarker);
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == ownerId) == 1 && !runtime.OwnerRequiresRestart(ownerId), "The same process should republish a re-enabled non-code ContentPack.");
                Assert(CountCoreOwnerRootsForTest(runtime, ownerId) > 0 &&
                    GetContent(runtime).FindAssets("txt").Count(asset => asset.SourceModId == ownerId && asset.RelativePath.EndsWith("reloadable-content.txt", StringComparison.OrdinalIgnoreCase)) == 1,
                    "Re-enabled ContentPack publication should restore one authoritative Content asset without duplication.");

                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == ownerId) == 1 && !runtime.OwnerRequiresRestart(ownerId), "Repeated ContentPack refresh should remain active and idempotent.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void WorkshopReloadHotLoadsNewlyEnabledCodeModOnceAndLocksDisabledLoadedMod()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            string packageRoot = Path.Combine(persistentRoot, "MODS", "Yuuka_DTMAPI_HotLoad");
            string contentRoot = Path.Combine(packageRoot, "Content", "DTMAPI");
            Directory.CreateDirectory(contentRoot);

            string assemblyPath = typeof(HotLoadProbeMod).Assembly.Location;
            string assemblyName = Path.GetFileName(assemblyPath);
            File.Copy(assemblyPath, Path.Combine(contentRoot, assemblyName), overwrite: true);
            File.WriteAllText(
                Path.Combine(contentRoot, "manifest.json"),
                    "{ \"Name\": \"Hot Load Test\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"DTMAPI.Tests.HotLoad\", \"EntryDll\": \"Content/DTMAPI/" + assemblyName + "\", \"EntryType\": \"" + (typeof(HotLoadProbeMod).FullName ?? nameof(HotLoadProbeMod)) + "\", \"Type\": \"CodeMod\", \"CodeModKind\": \"Strict\" }");
            const string contentItemId = "dtmapi_owner_lifetime_hot_item";
            File.WriteAllText(
                Path.Combine(contentRoot, "item_tbitem.json"),
                "[{ \"id\": \"" + contentItemId + "\", \"sub_type\": \"material\", \"title\": { \"text\": \"Owner 生命周期物品\", \"english\": \"Owner Lifetime Item\" } }]");

            string? previousRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_HotLoad", false);

                var menu = new ConfigMenuRegistry();
                IConfigMenuRuntime menuRuntime = menu;
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), menu);
                runtime.Start();
                Assert(!runtime.CreateSnapshot().LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad"), "Disabled official package should not load at startup.");
                Assert(!GetContent(runtime).FindAssets("json").Any(asset => asset.SourceModId.Equals("DTMAPI.Tests.HotLoad", StringComparison.OrdinalIgnoreCase)) && runtime.GetIndexedContentItem(contentItemId) == null, "A disabled code owner must publish neither content assets nor enabled indexed items.");
                Assert(runtime.GetAnyIndexedContentItem(contentItemId) == null, "All-content diagnostics must not parse or retain item-level rows for a disabled code owner.");

                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_HotLoad", true);
                runtime.NotifyWorkshopModListChanged();
                RuntimeSnapshot loadedSnapshot = runtime.CreateSnapshot();
                Assert(loadedSnapshot.LoadedMods.Count(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad") == 1, "Official reload should hot-load a newly enabled code mod once.");
                Assert(ReadHotLoadEntryCount(gameDir) == 1, "Hot-loaded code mod Entry should run exactly once.");
                Assert(GetContent(runtime).FindAssets("json").Any(asset => asset.SourceModId.Equals("DTMAPI.Tests.HotLoad", StringComparison.OrdinalIgnoreCase)) && runtime.GetIndexedContentItem(contentItemId)?.Enabled == true, "Content assets and indexed items must publish only after atomic owner activation succeeds.");
                IConfigMenuPage loadedPage = menuRuntime.GetPage("DTMAPI.Tests.HotLoad") ?? throw new InvalidOperationException("Hot-loaded mod should register a config page.");
                Assert(!loadedPage.IsLocked, "Hot-loaded enabled page should be editable.");

                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.CreateSnapshot().LoadedMods.Count(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad") == 1, "Repeated official reload must not duplicate loaded mods.");
                Assert(ReadHotLoadEntryCount(gameDir) == 1, "Repeated official reload must not re-run Entry for an already loaded mod.");
                runtime.Diagnostics.RecordError("DTMAPI.Tests.HotLoad", "Mod monitor reported an error. Prior active-owner diagnostic.", "retained-before-disable");
                Assert(runtime.CreateDiagnosticsSnapshot().Mods.Any(status => status.UniqueID == "DTMAPI.Tests.HotLoad" && status.StatusCode == "runtime-diagnostic"), "A loaded owner with a prior runtime error should expose that diagnostic before deactivation.");

                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_HotLoad", false);
                runtime.NotifyWorkshopModListChanged();
                RuntimeSnapshot disabledAfterLoad = runtime.CreateSnapshot();
                Assert(!disabledAfterLoad.LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad"), "Official disable should immediately remove the owner from loaded state.");
                Assert(menuRuntime.GetPage("DTMAPI.Tests.HotLoad") == null, "Official disable should remove the owner config page and callbacks.");
                Assert(runtime.ModRegistry.GetApi<IUnitProbeApi>("DTMAPI.Tests.HotLoad") == null, "Official disable should remove owner API roots.");
                Assert(!runtime.Input.GetRegisteredButtons().Contains("F12", StringComparer.OrdinalIgnoreCase), "Official disable should dispose owner input registrations.");
                Assert(runtime.Config.CountOwner("DTMAPI.Tests.HotLoad") == 0, "Official disable should remove owner config migrations.");
                Assert(runtime.Events.GetHandlerCleanupSnapshot().Slots.All(slot => !slot.ByOwner.ContainsKey("DTMAPI.Tests.HotLoad")), "Official disable should remove owner event roots.");
                Assert(!GetContent(runtime).FindAssets("json").Any(asset => asset.SourceModId.Equals("DTMAPI.Tests.HotLoad", StringComparison.OrdinalIgnoreCase)) && runtime.GetIndexedContentItem(contentItemId) == null, "Official disable should remove both owner content publication roots.");

                WriteOfficialModInfos(persistentRoot, "Local.Yuuka_DTMAPI_HotLoad", true);
                runtime.NotifyWorkshopModListChanged();
                Assert(!runtime.CreateSnapshot().LoadedMods.Any(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad"), "Same-process re-enable should remain inactive until restart.");
                Assert(ReadHotLoadEntryCount(gameDir) == 1, "Same-process re-enable must not execute Entry again.");
                Assert(runtime.OwnerRequiresRestart("DTMAPI.Tests.HotLoad"), "Deactivated loaded assembly should report restart-required.");
                Assert(!GetContent(runtime).FindAssets("json").Any(asset => asset.SourceModId.Equals("DTMAPI.Tests.HotLoad", StringComparison.OrdinalIgnoreCase)) && runtime.GetIndexedContentItem(contentItemId) == null, "Same-process restart-required re-enable must not republish owner content.");
                Assert(runtime.GetAnyIndexedContentItem(contentItemId) == null, "Restart-required re-enable must retain only package-level diagnostics and must not parse inactive owner item rows.");
                IDtmModStatusInfo restartStatus = runtime.CreateDiagnosticsSnapshot().Mods.Single(status => status.UniqueID == "DTMAPI.Tests.HotLoad");
                Assert(restartStatus.StatusCode == "restart-required", "Authoritative restart-required lifecycle state must take precedence over retained prior owner errors.");
                Assert(restartStatus.Reason.Contains("Prior active-owner diagnostic.", StringComparison.Ordinal), "Restart-required diagnostics should preserve prior error details without hiding the lifecycle state.");

                var restartedRuntime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                restartedRuntime.Start();
                Assert(restartedRuntime.CreateSnapshot().LoadedMods.Count(m => m.Manifest.UniqueID == "DTMAPI.Tests.HotLoad") == 1, "A new process-lifetime runtime should load the re-enabled mod.");
                Assert(ReadHotLoadEntryCount(gameDir) == 2, "Restarted runtime should execute Entry exactly once for the new process.");
                Assert(GetContent(restartedRuntime).FindAssets("json").Any(asset => asset.SourceModId.Equals("DTMAPI.Tests.HotLoad", StringComparison.OrdinalIgnoreCase)) && restartedRuntime.GetIndexedContentItem(contentItemId)?.Enabled == true, "A clean process may republish content after the owner activates again.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousRoot);
            }
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

        private static void RequiredReservedRuntimeProviderLossDeactivatesConsumer()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.ReservedDependencies.Missing";
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                RegisterReservedRuntimeProviderProbe(runtime, "DTMAPI.GameBridge.DolocTown", DtmApiRuntime.ApiVersion);
                RegisterReservedRuntimeProviderProbe(runtime, "DTMAPI.DebugConsoleHost", DtmApiRuntime.ApiVersion);
                WriteAtomicProbePackage(
                    dir,
                    ownerId,
                    ", \"Dependencies\": [{ \"UniqueID\": \"DTMAPI.DebugConsoleHost\", \"MinimumVersion\": \"0.5.3-alpha\", \"Required\": true }]");

                AtomicOwnerProbeMod.EntryCount = 0;
                runtime.Start();
                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) && AtomicOwnerProbeMod.EntryCount == 1, "The required-reserved-provider loss fixture should begin active.");
                Assert(runtime.ModRegistry.RemoveOwner("DTMAPI.DebugConsoleHost") > 0 && !runtime.ModRegistry.IsLoaded("DTMAPI.DebugConsoleHost"), "The fixture should remove the required reserved provider from the authoritative registry.");

                runtime.NotifyWorkshopModListChanged();

                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) && runtime.OwnerRequiresRestart(ownerId), "A required reserved provider missing from the registry must deactivate its ordinary consumer and require restart.");
                Assert(CountCoreOwnerRootsForTest(runtime, ownerId) == 0, "Required reserved-provider loss should clean every Core owner root from the consumer.");
                Assert(runtime.ModRegistry.IsLoaded("DTMAPI.ModConfigMenu") && runtime.ModRegistry.IsLoaded("DTMAPI.GameBridge.DolocTown"), "Consumer deactivation must preserve unrelated process-lifetime providers.");
                Assert(AtomicOwnerProbeMod.EntryCount == 1, "Provider loss reconciliation must not repeat consumer Entry.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void RequiredReservedRuntimeProviderVersionMismatchDeactivatesConsumer()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.ReservedDependencies.Version";
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                ManifestModel gameBridge = RegisterReservedRuntimeProviderProbe(runtime, "DTMAPI.GameBridge.DolocTown", DtmApiRuntime.ApiVersion);
                RegisterReservedRuntimeProviderProbe(runtime, "DTMAPI.DebugConsoleHost", DtmApiRuntime.ApiVersion);
                WriteAtomicProbePackage(
                    dir,
                    ownerId,
                    ", \"Dependencies\": [{ \"UniqueID\": \"DTMAPI.GameBridge.DolocTown\", \"MinimumVersion\": \"0.5.3-alpha\", \"Required\": true }]");

                AtomicOwnerProbeMod.EntryCount = 0;
                runtime.Start();
                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) && AtomicOwnerProbeMod.EntryCount == 1, "The reserved-provider version fixture should begin active.");
                gameBridge.Version = "0.5.2-alpha";

                runtime.NotifyWorkshopModListChanged();

                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) && runtime.OwnerRequiresRestart(ownerId), "A reserved provider registry version below the required minimum must deactivate its ordinary consumer.");
                Assert(CountCoreOwnerRootsForTest(runtime, ownerId) == 0, "Reserved-provider version mismatch should clean every consumer owner root.");
                Assert(runtime.ModRegistry.IsLoaded(gameBridge.UniqueID) && runtime.ModRegistry.Get(gameBridge.UniqueID)?.Version == "0.5.2-alpha", "Reconciliation should preserve the process-lifetime provider itself while deactivating the incompatible consumer.");
                Assert(AtomicOwnerProbeMod.EntryCount == 1, "Reserved-provider version mismatch must not repeat consumer Entry.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void OptionalReservedRuntimeProviderLossWarnsWithoutDeactivation()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                const string ownerId = "DTMAPI.Tests.ReservedDependencies.Optional";
                const string optionalProviderId = "DTMAPI.DebugConsoleHost";
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                WriteAtomicProbePackage(
                    dir,
                    ownerId,
                    ", \"Dependencies\": [{ \"UniqueID\": \"" + optionalProviderId + "\", \"MinimumVersion\": \"0.5.3-alpha\", \"Required\": false }]");

                AtomicOwnerProbeMod.EntryCount = 0;
                runtime.Start();
                int activeRoots = CountCoreOwnerRootsForTest(runtime, ownerId);
                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) && !runtime.ModRegistry.IsLoaded(optionalProviderId), "An ordinary Mod should load when its optional reserved provider is absent.");

                runtime.NotifyReturnedToTitle();
                runtime.NotifyWorkshopModListChanged();
                runtime.NotifyWorkshopModListChanged();

                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) && !runtime.OwnerRequiresRestart(ownerId), "Optional reserved-provider loss must remain warning-only across title and repeated Workshop refresh.");
                Assert(AtomicOwnerProbeMod.EntryCount == 1 && CountCoreOwnerRootsForTest(runtime, ownerId) == activeRoots, "Optional reserved-provider loss must preserve Entry-once and all ordinary owner roots.");
                RuntimeSnapshot snapshot = runtime.CreateSnapshot();
                Assert(snapshot.Warnings.Count(warning => warning.Details.Contains(ownerId, StringComparison.OrdinalIgnoreCase) && warning.Details.Contains(optionalProviderId, StringComparison.OrdinalIgnoreCase)) == 1, "Optional reserved-provider loss should publish one bounded warning for the owner/dependency pair.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void SourceDisableCascadesRequiredDependentInReverseOrder()
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
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var cleanupOrder = new OwnerCleanupOrderProbe("DTMAPI.Tests.Disable.CleanupOrder");
                runtime.RegisterModOwnerCleanupParticipant(cleanupOrder);
                AtomicOwnerProbeMod.EntryCount = 0;
                runtime.Start();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == required || mod.Manifest.UniqueID == optional) == 3 && AtomicOwnerProbeMod.EntryCount == 3, "The source-disable fixture should begin with provider and both consumers active.");

                File.WriteAllText(Path.Combine(providerDir, "dtmapi.disabled"), "disabled by owner-lifetime dependency test");
                runtime.NotifyWorkshopModListChanged();

                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == required), "Disabling an ordinary provider should cascade deactivation to its required dependent.");
                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == optional), "Disabling an ordinary provider should keep its optional consumer active.");
                Assert(cleanupOrder.OwnerIds.SequenceEqual(new[] { required, provider }, StringComparer.OrdinalIgnoreCase), "Ordinary provider disable should clean the required dependent before the provider in reverse dependency order.");
                Assert(runtime.OwnerRequiresRestart(provider) && runtime.OwnerRequiresRestart(required) && !runtime.OwnerRequiresRestart(optional), "Disabled provider and required dependent should require restart while the optional consumer remains active.");
                Assert(CountCoreOwnerRootsForTest(runtime, provider) == 0 && CountCoreOwnerRootsForTest(runtime, required) == 0 && CountCoreOwnerRootsForTest(runtime, optional) > 0, "Source-disable cascade should leave zero roots for inactive owners and preserve the optional consumer roots.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void SourceRemovalCascadesRequiredDependentsButKeepsOptionalDependents()
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

                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == required), "Source removal should deactivate the provider and required dependent.");
                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == optional), "Optional dependency disappearance should not deactivate its consumer.");
                Assert(cleanupOrder.OwnerIds.SequenceEqual(new[] { required, provider }, StringComparer.OrdinalIgnoreCase), "Source removal should clean the required dependent before its ordinary provider in reverse dependency order.");
                Assert(runtime.OwnerRequiresRestart(provider) && runtime.OwnerRequiresRestart(required), "Removed provider and cascaded required dependent should remain restart-required.");
                Assert(runtime.ModRegistry.CountOwner(provider) == 0 && runtime.ModRegistry.CountOwner(required) == 0, "Cascaded deactivation should leave no registry roots.");
                IContentQueryHelper content = GetContent(runtime);
                Assert(!content.FindAssets("json").Any(asset => asset.SourceModId.Equals(provider, StringComparison.OrdinalIgnoreCase) || asset.SourceModId.Equals(required, StringComparison.OrdinalIgnoreCase)), "Source removal and required-dependency cascade must remove both inactive owners' content publication.");
                Assert(content.FindAssets("json").Any(asset => asset.SourceModId.Equals(optional, StringComparison.OrdinalIgnoreCase)), "An optional dependent that remains active must retain its content publication.");
                Assert(runtime.CreateSnapshot().Warnings.Any(warning => warning.Owner == optional && warning.Message.Contains("可选", StringComparison.OrdinalIgnoreCase)) ||
                    runtime.CreateSnapshot().Warnings.Any(warning => warning.Details.Contains(optional, StringComparison.OrdinalIgnoreCase)), "Optional dependency change should publish a warning.");
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.ModRegistry.CountOwner(provider) == 0 && runtime.ModRegistry.CountOwner(required) == 0 && runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == optional), "Repeated reconciliation should be idempotent.");
                Assert(!content.FindAssets("json").Any(asset => asset.SourceModId.Equals(provider, StringComparison.OrdinalIgnoreCase) || asset.SourceModId.Equals(required, StringComparison.OrdinalIgnoreCase)), "Repeated reconciliation must not republish restart-required content.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void SourceRefreshDependencyDowngradeCascadesRequiredDependent()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                const string provider = "DTMAPI.Tests.VersionRefresh.Provider";
                const string required = "DTMAPI.Tests.VersionRefresh.Required";
                const string transitive = "DTMAPI.Tests.VersionRefresh.Transitive";
                const string optional = "DTMAPI.Tests.VersionRefresh.Optional";
                string providerDir = WriteAtomicProbePackage(dir, provider, string.Empty);
                WriteAtomicProbePackage(dir, required, ", \"Dependencies\": [{ \"UniqueID\": \"" + provider + "\", \"MinimumVersion\": \"1.0.0\", \"Required\": true }]");
                WriteAtomicProbePackage(dir, transitive, ", \"Dependencies\": [{ \"UniqueID\": \"" + required + "\", \"MinimumVersion\": \"1.0.0\", \"Required\": true }]");
                WriteAtomicProbePackage(dir, optional, ", \"Dependencies\": [{ \"UniqueID\": \"" + provider + "\", \"MinimumVersion\": \"1.0.0\", \"Required\": false }]");
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var cleanupOrder = new OwnerCleanupOrderProbe("DTMAPI.Tests.VersionRefresh.CleanupOrder");
                runtime.RegisterModOwnerCleanupParticipant(cleanupOrder);
                AtomicOwnerProbeMod.EntryCount = 0;
                runtime.Start();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == required || mod.Manifest.UniqueID == transitive || mod.Manifest.UniqueID == optional) == 4, "Version refresh fixture should initially load provider, required chain, and optional dependent.");
                Assert(AtomicOwnerProbeMod.EntryCount == 4, "Version refresh fixture should execute each owner Entry once.");

                string manifestPath = Path.Combine(providerDir, "manifest.json");
                string manifestJson = File.ReadAllText(manifestPath);
                string downgradedJson = manifestJson.Replace("\"Version\": \"1.0.0\"", "\"Version\": \"0.5.0\"");
                Assert(!string.Equals(manifestJson, downgradedJson, StringComparison.Ordinal), "Version refresh fixture should rewrite the discovered provider version.");
                File.WriteAllText(manifestPath, downgradedJson);
                runtime.NotifyWorkshopModListChanged();

                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == provider), "An in-place provider manifest version change must deactivate the resident owner instead of presenting disk metadata as the loaded assembly version.");
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == required), "A refreshed provider version below the required minimum should cascade deactivation to the required dependent.");
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == transitive), "A provider version downgrade should cascade through the required dependency chain.");
                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == optional), "A refreshed provider version below an optional minimum should keep the optional consumer active.");
                Assert(cleanupOrder.OwnerIds.SequenceEqual(new[] { transitive, required, provider }, StringComparer.OrdinalIgnoreCase), "Version downgrade should clean transitive and direct consumers before the updated provider in reverse dependency order.");
                Assert(runtime.OwnerRequiresRestart(provider) && runtime.OwnerRequiresRestart(required) && runtime.OwnerRequiresRestart(transitive), "The resident provider assembly and cascaded required dependency chain should remain inactive until restart.");
                Assert(AtomicOwnerProbeMod.EntryCount == 4, "Dependency reconciliation must not re-run Entry in the same process.");
                Assert(runtime.ModRegistry.CountOwner(provider) == 0 && runtime.Config.CountOwner(provider) == 0 && runtime.Input.CountOwnerResources(provider) == 0 &&
                    runtime.ModRegistry.CountOwner(required) == 0 && runtime.Config.CountOwner(required) == 0 && runtime.Input.CountOwnerResources(required) == 0 &&
                    runtime.ModRegistry.CountOwner(transitive) == 0 && runtime.Config.CountOwner(transitive) == 0 && runtime.Input.CountOwnerResources(transitive) == 0,
                    "Version-handoff deactivation should remove registry, config, and input roots from the provider and required dependency chain.");
                Assert(runtime.Events.GetHandlerCleanupSnapshot().Slots.All(slot => !slot.ByOwner.ContainsKey(provider) && !slot.ByOwner.ContainsKey(required) && !slot.ByOwner.ContainsKey(transitive)), "Version-handoff deactivation should remove event roots from the provider and required dependency chain.");
                IContentQueryHelper content = GetContent(runtime);
                Assert(!content.FindAssets("json").Any(asset => asset.SourceModId.Equals(provider, StringComparison.OrdinalIgnoreCase) || asset.SourceModId.Equals(required, StringComparison.OrdinalIgnoreCase) || asset.SourceModId.Equals(transitive, StringComparison.OrdinalIgnoreCase)), "Version-handoff deactivation must remove provider and required-chain content publication.");
                Assert(content.FindAssets("json").Any(asset => asset.SourceModId.Equals(optional, StringComparison.OrdinalIgnoreCase)), "The optional consumer that remains active must retain its content publication.");
                Assert(runtime.CreateSnapshot().Warnings.Any(warning => warning.Owner == optional && warning.Message.Contains("可选", StringComparison.OrdinalIgnoreCase)) ||
                    runtime.CreateSnapshot().Warnings.Any(warning => warning.Details.Contains(optional, StringComparison.OrdinalIgnoreCase)), "Optional dependency version changes should retain warning-only semantics.");

                runtime.NotifyWorkshopModListChanged();
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == required || mod.Manifest.UniqueID == transitive) && runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == optional), "Repeated version reconciliation should remain idempotent while the updated assembly awaits restart.");
                Assert(AtomicOwnerProbeMod.EntryCount == 4 && cleanupOrder.OwnerIds.Count == 3, "Repeated version reconciliation must neither re-enter nor re-clean any owner.");
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

                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == provider || mod.Manifest.UniqueID == existingConsumer || mod.Manifest.UniqueID == newConsumer), "The refreshed 2.0 disk manifest must neither keep the resident 1.0 provider active nor authorize the new >=2.0 consumer.");
                Assert(AtomicOwnerProbeMod.EntryCount == 2, "In-place upgrade reconciliation must not execute the provider again or enter the new consumer in the old process.");
                Assert(cleanupOrder.OwnerIds.SequenceEqual(new[] { existingConsumer, provider }, StringComparer.OrdinalIgnoreCase), "In-place version handoff must deactivate the existing required consumer before the resident provider.");
                Assert(runtime.OwnerRequiresRestart(provider) && runtime.OwnerRequiresRestart(existingConsumer), "The resident provider assembly and its deactivated consumer must remain restart-required.");
                Assert(!runtime.OwnerRequiresRestart(newConsumer), "A newly discovered consumer that never entered must not manufacture a restart-required owner state.");
                Assert(runtime.ModRegistry.Get(provider) == null && runtime.ModRegistry.CountOwner(provider) == 0 && runtime.ModRegistry.CountOwner(existingConsumer) == 0 && runtime.ModRegistry.CountOwner(newConsumer) == 0, "Version handoff and blocked new-consumer load must leave no registry/API root from the old process graph.");
                Assert(CountCoreOwnerRootsForTest(runtime, provider) == 0 && CountCoreOwnerRootsForTest(runtime, existingConsumer) == 0 && CountCoreOwnerRootsForTest(runtime, newConsumer) == 0, "In-place upgrade reconciliation must leave every affected owner at zero Core roots.");
                Assert(runtime.CreateSnapshot().Errors.Any(error => error.Owner == newConsumer && error.Message.Contains("依赖", StringComparison.OrdinalIgnoreCase)), "The >=2.0 consumer should expose a missing/unavailable dependency diagnostic rather than load against stale resident code.");

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
                    new FakeHost(gameDir, includeLegacyDevelopmentModSourceForTests: false),
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

        private static int ReadHotLoadEntryCount(string gameDir)
        {
            string path = Path.Combine(gameDir, "DTMAPI", "config", "DTMAPI.Tests.HotLoad.hotload.txt");
            return File.Exists(path) && int.TryParse(File.ReadAllText(path), out int value) ? value : 0;
        }
    }
}
