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
using DTMAPI.BepInExBootstrap;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static string ComputeFileSha256(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha = SHA256.Create())
                return Convert.ToHexString(sha.ComputeHash(stream));
        }

        private static string FindRepositoryFile(string relativePath)
        {
            foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                var current = new DirectoryInfo(Path.GetFullPath(start));
                while (current != null)
                {
                    string candidate = Path.Combine(current.FullName, relativePath);
                    if (File.Exists(candidate))
                        return candidate;
                    current = current.Parent;
                }
            }
            throw new FileNotFoundException("Could not locate repository QA build output.", relativePath);
        }

        private static void HookInstallSchedulerCoalescesExternalRequests()
        {
            int runtimeThreadId = Thread.CurrentThread.ManagedThreadId;
            var scheduler = new HookInstallScheduler();

            scheduler.Request("Initialize", "Startup", runtimeThreadId, runtimeThreadId);
            scheduler.Request("AssemblyLoad:Assembly-CSharp", "Startup", runtimeThreadId + 100, runtimeThreadId);
            scheduler.Request("AssemblyLoad:UnityEngine", "Startup", runtimeThreadId + 101, runtimeThreadId);
            scheduler.Request("RetryTimer", "TitleObserved", runtimeThreadId + 102, runtimeThreadId);
            scheduler.Request("RetryTimer elapsed", "TitleObserved", runtimeThreadId + 103, runtimeThreadId);

            HookInstallSchedulerSnapshot pending = scheduler.GetSnapshot(
                coreReady: false,
                coreSummary: "ready=0/6",
                featureSummary: "ready=0/1",
                smokeDiagnosticsSummary: "ready=0/1",
                assemblySummary: "subscribed=true",
                retrySummary: "alive=true",
                legacyAllReady: false,
                assemblyLoadSubscribed: true,
                retryTimerAlive: true);
            Assert(pending.Pending == 3, "Hook scheduler should coalesce Initialize, AssemblyLoad, and RetryTimer request keys.");
            Assert(pending.TotalRequested == 5, "Hook scheduler should count every external request.");
            Assert(pending.TotalCoalesced == 2, "Hook scheduler should count coalesced AssemblyLoad/RetryTimer requests.");
            Assert(pending.OffThreadRequests == 4, "Hook scheduler should record off-thread hook install requests.");

            HookInstallRequest[] requests = scheduler.ConsumePending();
            Assert(requests.Length == 3, "Consuming pending requests should return one entry per install key.");
            Assert(requests.Any(r => r.Key == "AssemblyLoad" && r.Count == 2), "AssemblyLoad requests should be merged under one key.");
            Assert(requests.Any(r => r.Key == "RetryTimer" && r.Count == 2), "Retry timer requests should be merged under one key.");
            Assert(scheduler.GetSnapshot(true, "ready=6/6", "ready=1/1", "ready=1/1", "subscribed=false", "alive=false", true, false, false).Pending == 0, "Scheduler pending queue should be empty after consume.");
        }

        private static void InputNativeEdgeLatchRetainsAndConsumesShortTapOnce()
        {
            ReflectedUnityInput.ClearTransientState();
            var samples = new[] { new InputButtonSample("F6", false, true, true) };
            ReflectedUnityInput.LatchInputSamplesForTest(samples);

            InputButtonSample first = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(first.PressedEdge && first.ReleasedEdge && !first.IsDownNow, "The frame latch should retain both edges of a short tap until Core consumes it.");

            InputButtonSample consumed = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(!consumed.PressedEdge && !consumed.ReleasedEdge, "A latched edge should be consumed exactly once.");

            ReflectedUnityInput.LatchInputSamplesForTest(samples);
            ReflectedUnityInput.LatchInputSamplesForTest(new[] { new InputButtonSample("F7", false, false, false) });
            InputButtonSample expired = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(!expired.PressedEdge && !expired.ReleasedEdge, "A button omitted by the next watched frame should not retain a stale edge.");
            ReflectedUnityInput.ClearTransientState();
        }

        private static void InputNativeEdgeLatchRejectsDuplicateSourcesAndHeldPress()
        {
            ReflectedUnityInput.ClearTransientState();
            bool firstSource = ReflectedUnityInput.LatchInputSamplesForFrameForTest(
                new[] { new InputButtonSample("F6", true, true, false) },
                100);
            bool duplicateSource = ReflectedUnityInput.LatchInputSamplesForFrameForTest(
                new[] { new InputButtonSample("F6", true, true, false) },
                100);
            Assert(firstSource && !duplicateSource, "Only one input source should latch registered buttons for a Unity frame.");

            InputButtonSample first = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(first.PressedEdge && first.IsDownNow, "The first source should deliver the physical press.");

            ReflectedUnityInput.LatchInputSamplesForFrameForTest(
                new[] { new InputButtonSample("F6", true, true, false) },
                101);
            InputButtonSample repeated = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(!repeated.PressedEdge && repeated.IsDownNow, "A repeated backend pressed flag before release must not become a second physical press.");

            ReflectedUnityInput.LatchInputSamplesForFrameForTest(
                new[] { new InputButtonSample("F6", false, false, true) },
                102);
            InputButtonSample released = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(released.ReleasedEdge && !released.IsDownNow, "The held cycle should close on release.");

            ReflectedUnityInput.LatchInputSamplesForFrameForTest(
                new[] { new InputButtonSample("F6", true, true, false) },
                103);
            InputButtonSample nextPress = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(nextPress.PressedEdge && nextPress.IsDownNow, "A new press after release must still dispatch normally.");
            ReflectedUnityInput.ClearTransientState();
        }

        private static void GameBridgeOwnerCleanupRemovesConfiguredFishingPolicy()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                var owner = new ManifestModel
                {
                    Name = "Failed AutoFishing Consumer",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.FailedAutoFishingConsumer",
                    Type = "CodeMod"
                };
                IFishingAutomationApi api = runtime.ModRegistry.GetApi<IFishingAutomationApi>("DTMAPI.GameBridge.DolocTown")
                    ?? throw new InvalidOperationException("Fishing compatibility API should be registered by GameBridge.");
                api.Configure(owner, new FishingAutomationOptions());
                Assert(api.GetStatus(owner.UniqueID).Status == "configured", "Configure should store a compatibility policy without activation.");

                MethodInfo cleanup = typeof(DtmApiRuntime).GetMethod("CleanupFailedCodeModOwner", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("CleanupFailedCodeModOwner should exist.");
                string summary = (string)(cleanup.Invoke(runtime, new object[] { owner.UniqueID, string.Empty }) ?? string.Empty);

                Assert(api.GetStatus(owner.UniqueID).Status == "inactive/no-consumer", "Core-to-GameBridge owner cleanup should remove configured fishing policy after failed Entry.");
                Assert(summary.Contains("participantResourcesRemoved=3", StringComparison.Ordinal), "Failed owner cleanup should report removal of the frozen compatibility option, status, and one-time warning roots without a ProductNative state root.");
                Assert(!runtime.ResourceLifecycleSnapshot.Records.Any(record => record.OwnerId.Equals(owner.UniqueID, StringComparison.OrdinalIgnoreCase)), "Failed owner cleanup should leave no owner-scoped fishing lifecycle record.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void TitleSettingsBoundaryClosesOnlyItsOwnedRuntimeMenu()
        {
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
            var titleHost = new ReflectedTitleMenuSettingsUi(runtime, new ConfigMenuRegistry());
            MethodInfo resetBoundary = typeof(ReflectedTitleMenuSettingsUi).GetMethod("ResetBoundaryState", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Title settings ResetBoundaryState should exist.");

            runtime.UI.OpenCustomMenu("DTMAPI.DebugConsole");
            resetBoundary.Invoke(titleHost, new object[] { "title hidden" });
            Assert(runtime.UI.IsOpen && runtime.UI.ActiveMenuId == "DTMAPI.DebugConsole", "The hidden title host must not close another UI owner's custom runtime menu.");

            runtime.UI.Close();
            runtime.UI.OpenDtmApiStatusPage();
            resetBoundary.Invoke(titleHost, new object[] { "title hidden" });
            Assert(!runtime.UI.IsOpen, "The hidden title host must still close its own DTMAPI manager page.");
        }

        private static void TitleSettingsEventSystemDoesNotChooseInputSystemModuleWhenNotReady()
        {
            Type uiType = typeof(ReflectedTitleMenuSettingsUi);
            MethodInfo select = uiType.GetMethod("TrySelectEventSystemInputModule", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("TrySelectEventSystemInputModule should exist.");

            object ui = RuntimeHelpers.GetUninitializedObject(uiType);
            SetPrivateField(ui, "inputSystemUiInputModuleType", typeof(FakeInputSystemUiInputModule));
            SetPrivateField(ui, "standaloneInputModuleType", typeof(FakeStandaloneInputModule));
            object?[] args = { null, null };
            bool selected = (bool)(select.Invoke(ui, args) ?? false);
            Assert(selected, "Title UI should keep a fallback path when Unity Input System is not initialized.");
            Assert(ReferenceEquals(args[0], typeof(FakeStandaloneInputModule)), "Title UI should not select InputSystemUIInputModule before Unity Input System is initialized.");
            Assert(((string?)args[1] ?? string.Empty).Contains("StandaloneInputModule", StringComparison.Ordinal), "Title UI fallback source should be visible in diagnostics.");

            SetPrivateField(ui, "standaloneInputModuleType", null);
            args = new object?[] { null, null };
            selected = (bool)(select.Invoke(ui, args) ?? true);
            Assert(!selected, "Title UI should defer EventSystem creation instead of creating InputSystemUIInputModule when no safe module is available.");
            Assert(((string?)args[1] ?? string.Empty).Contains("not initialized", StringComparison.OrdinalIgnoreCase), "Deferred title EventSystem creation should explain the Input System state.");
        }

        private sealed class FakeInputSystemUiInputModule { }
        private sealed class FakeStandaloneInputModule { }

        private static void TitleSettingsConfigPagerPreservesExplicitNavigation()
        {
            Assert(
                !ReflectedTitleMenuSettingsUi.ShouldFollowConfigListSelection(
                    DtmOverlayPage.Config,
                    12,
                    "Config.Mod.01",
                    DtmOverlayPage.Config,
                    12,
                    "Config.Mod.01"),
                "An ordinary dirty render in the same Config session must not reinterpret manual list paging as a selection request.");
            Assert(
                ReflectedTitleMenuSettingsUi.ResolveConfigListPageIndex(1, 16, 0, followSelection: false) == 1,
                "Manual Next from page one must remain on the partial second page even while the selected Mod belongs to page one.");

            Assert(
                ReflectedTitleMenuSettingsUi.ShouldFollowConfigListSelection(
                    DtmOverlayPage.Config,
                    12,
                    "Config.Mod.01",
                    DtmOverlayPage.Config,
                    13,
                    "Config.Mod.01"),
                "A new overlay session must bring the selected Config Mod back into view even when its requested id did not change.");
            Assert(
                ReflectedTitleMenuSettingsUi.ShouldFollowConfigListSelection(
                    DtmOverlayPage.Config,
                    13,
                    "Config.Mod.01",
                    DtmOverlayPage.Config,
                    13,
                    "Config.Mod.16"),
                "A changed explicit config request must follow the requested Mod.");
            Assert(
                ReflectedTitleMenuSettingsUi.ResolveConfigListPageIndex(0, 16, 15, followSelection: true) == 1,
                "An explicit request for the sixteenth Config Mod must reveal the second page.");
            Assert(
                ReflectedTitleMenuSettingsUi.ResolveConfigListPageIndex(9, 14, 0, followSelection: false) == 0,
                "A stale manually selected list page must still clamp after the number of Config Mods shrinks.");

            float pagerRight = ReflectedTitleMenuSettingsUi.ConfigListPagerX +
                ReflectedTitleMenuSettingsUi.ConfigListPagerLabelWidth +
                ReflectedTitleMenuSettingsUi.ConfigListPagerGap +
                ReflectedTitleMenuSettingsUi.ConfigListPagerButtonWidth +
                ReflectedTitleMenuSettingsUi.ConfigListPagerGap +
                ReflectedTitleMenuSettingsUi.ConfigListPagerButtonWidth;
            Assert(
                pagerRight < ReflectedTitleMenuSettingsUi.ConfigDetailPaneX,
                "The Config Mod pager must remain wholly inside the left column instead of overlapping the detail title.");
            Assert(
                ReflectedTitleMenuSettingsUi.ConfigListPagerButtonWidth > 30f &&
                ReflectedTitleMenuSettingsUi.ConfigListPagerHeight > 22f,
                "The Config Mod pager must enlarge both dimensions of the old 30x22 hit targets.");
            Assert(
                ReflectedTitleMenuSettingsUi.ConfigListPagedRowStartY <=
                    ReflectedTitleMenuSettingsUi.ConfigListPagerY - ReflectedTitleMenuSettingsUi.ConfigListPagerHeight - 8f &&
                ReflectedTitleMenuSettingsUi.ConfigListSinglePageRowStartY == ReflectedTitleMenuSettingsUi.ConfigListPagerY,
                "Paged Config rows must start below their dedicated navigation row while a single-page list retains the original compact row position.");
        }

        private static void GameBridgeOwnerRetainingServicesExposeCleanupBoundary()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            string[] ownerRetainingTypes =
            {
                "DTMAPI.GameBridge.DolocTown.ActionSpeedService",
                "DTMAPI.GameBridge.DolocTown.ActionCompletionService",
                "DTMAPI.GameBridge.DolocTown.AnimalViewerService",
                "DTMAPI.GameBridge.DolocTown.AudioReplacementService",
                "DTMAPI.GameBridge.DolocTown.CameraFeature",
                "DTMAPI.GameBridge.DolocTown.ChestLocatorEnhancerService",
                "DTMAPI.GameBridge.DolocTown.CropHarvestingService",
                "DTMAPI.GameBridge.DolocTown.CustomAnimalAnimatorBridgeService",
                "DTMAPI.GameBridge.DolocTown.DebugActionCompatibilityProxy",
                "DTMAPI.GameBridge.DolocTown.DebugConsoleCompatibilityProxy",
                "DTMAPI.GameBridge.DolocTown.FishRoeTooltipService",
                "DTMAPI.GameBridge.DolocTown.SaveSlotsService",
                "DTMAPI.GameBridge.DolocTown.FishingAutomationCompatibilityFeature"
            };
            foreach (string typeName in ownerRetainingTypes)
            {
                Type type = bridgeAssembly.GetType(typeName) ?? throw new InvalidOperationException("Owner-retaining GameBridge type missing: " + typeName + ".");
                Assert(type.GetMethod("RemoveOwner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null, typeName + " should expose the GameBridge owner cleanup boundary.");
            }
        }

        private static void
            SaveSavingParticipantFailureCancelsNativeBoundary()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime =
                    new DtmApiRuntime(
                        new FakeHost(dir),
                        new ConfigMenuRegistry());
                IEventsHelper events =
                    CreateEventsProxy(
                        runtime,
                        "DTMAPI.Tests.SaveSavingFailure");
                events.Save.SaveSaving += (_, _) =>
                    throw new InvalidOperationException(
                        "prepared-sidecar-write-failed");
                runtime.Start();

                Assert(
                    !runtime.TryNotifySaveSaving(2),
                    "A failed SaveSaving participant must veto the native save boundary.");
                Assert(
                    runtime.CreateSnapshot().Errors.Any(
                        error =>
                            error.Owner ==
                                "DTMAPI.Tests.SaveSavingFailure" &&
                            error.Message.Contains(
                                "Save.SaveSaving",
                                StringComparison.Ordinal)),
                    "The owner whose SaveSaving preparation failed must remain visible in diagnostics.");

                MethodInfo prefix =
                    typeof(DolocTownHookCallbacks)
                        .GetMethod(
                            "SaveGamePrefix",
                            BindingFlags.Public |
                            BindingFlags.Static)
                    ?? throw new MissingMethodException(
                        "DolocTownHookCallbacks",
                        "SaveGamePrefix");
                ParameterInfo[] parameters =
                    prefix.GetParameters();
                Assert(
                    prefix.ReturnType == typeof(bool) &&
                    parameters.Length == 2 &&
                    parameters[0].ParameterType ==
                        typeof(int) &&
                    parameters[1].ParameterType ==
                        typeof(bool).MakeByRefType(),
                    "The native SaveGame prefix must be able to return false and set a false result when preparation fails.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void FrameDriverHealthPolicyDistinguishesGlobalPauseAndSingleDriverStall()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset stale = now - TimeSpan.FromSeconds(3);
            var queued = new FrameDriverHealthSnapshot(
                inputSystemSubscribed: true,
                inputSystemCallbackSeen: true,
                playerLoopCallbackSeen: true,
                nativeCallbackSeen: true,
                unityUpdateCallbackSeen: true,
                inputSystemCallbackCount: 5,
                playerLoopCallbackCount: 10,
                nativeCallbackCount: 20,
                unityUpdateCallbackCount: 30,
                lastInputSystemCallbackAt: stale,
                lastInputSystemSubscribeAttemptAt: stale,
                lastPlayerLoopCallbackAt: stale,
                lastNativeCallbackAt: stale,
                lastUnityUpdateCallbackAt: stale);

            Assert(FrameDriverHealthPolicy.GetProbeKind(queued, now) == FrameDriverHealthProbeKind.ObserveGlobalPause, "A global frame gap should queue one main-thread health observation without assigning fault.");
            Assert(FrameDriverHealthPolicy.Evaluate(FrameDriverHealthProbeKind.ObserveGlobalPause, queued, queued, now) == FrameDriverHealthDecision.GlobalPause, "Unchanged stale counts across every Unity source must be classified as a global pause, not an InputSystem failure.");

            var siblingAdvanced = new FrameDriverHealthSnapshot(
                inputSystemSubscribed: true,
                inputSystemCallbackSeen: true,
                playerLoopCallbackSeen: true,
                nativeCallbackSeen: true,
                unityUpdateCallbackSeen: true,
                inputSystemCallbackCount: 5,
                playerLoopCallbackCount: 11,
                nativeCallbackCount: 20,
                unityUpdateCallbackCount: 31,
                lastInputSystemCallbackAt: stale,
                lastInputSystemSubscribeAttemptAt: stale,
                lastPlayerLoopCallbackAt: now,
                lastNativeCallbackAt: stale,
                lastUnityUpdateCallbackAt: now);
            Assert(FrameDriverHealthPolicy.GetProbeKind(siblingAdvanced, now) == FrameDriverHealthProbeKind.VerifyIsolatedInputStall, "A stale InputSystem callback with a progressing sibling must queue isolated-driver recovery.");
            Assert(FrameDriverHealthPolicy.Evaluate(FrameDriverHealthProbeKind.VerifyIsolatedInputStall, queued, siblingAdvanced, now) == FrameDriverHealthDecision.RecoverInputSystem, "Sibling progress with unchanged stale InputSystem count must select only InputSystem recovery.");
            Assert(FrameDriverHealthPolicy.Evaluate(FrameDriverHealthProbeKind.ObserveGlobalPause, queued, siblingAdvanced, now) == FrameDriverHealthDecision.GlobalPause, "A probe queued during a global pause must not recover InputSystem merely because sibling callbacks resume earlier in the same Unity frame.");

            var inputRecovered = new FrameDriverHealthSnapshot(
                inputSystemSubscribed: true,
                inputSystemCallbackSeen: true,
                playerLoopCallbackSeen: true,
                nativeCallbackSeen: true,
                unityUpdateCallbackSeen: true,
                inputSystemCallbackCount: 6,
                playerLoopCallbackCount: 11,
                nativeCallbackCount: 20,
                unityUpdateCallbackCount: 31,
                lastInputSystemCallbackAt: now,
                lastInputSystemSubscribeAttemptAt: stale,
                lastPlayerLoopCallbackAt: now,
                lastNativeCallbackAt: stale,
                lastUnityUpdateCallbackAt: now);
            Assert(FrameDriverHealthPolicy.Evaluate(FrameDriverHealthProbeKind.VerifyIsolatedInputStall, queued, inputRecovered, now) == FrameDriverHealthDecision.NoAction, "InputSystem progress after queueing must cancel stale recovery before unsubscribe.");

            var missingSubscription = new FrameDriverHealthSnapshot(
                inputSystemSubscribed: false,
                inputSystemCallbackSeen: false,
                playerLoopCallbackSeen: true,
                nativeCallbackSeen: false,
                unityUpdateCallbackSeen: true,
                inputSystemCallbackCount: 0,
                playerLoopCallbackCount: 12,
                nativeCallbackCount: 0,
                unityUpdateCallbackCount: 40,
                lastInputSystemCallbackAt: DateTimeOffset.MinValue,
                lastInputSystemSubscribeAttemptAt: now - TimeSpan.FromSeconds(11),
                lastPlayerLoopCallbackAt: now,
                lastNativeCallbackAt: DateTimeOffset.MinValue,
                lastUnityUpdateCallbackAt: now);
            Assert(FrameDriverHealthPolicy.GetProbeKind(missingSubscription, now) == FrameDriverHealthProbeKind.RetryMissingSubscription, "A missing InputSystem subscription must retry on its bounded cadence even while sibling drivers remain healthy.");
            Assert(FrameDriverHealthPolicy.Evaluate(FrameDriverHealthProbeKind.RetryMissingSubscription, queued, missingSubscription, now) == FrameDriverHealthDecision.NoAction, "A missing subscription is a retry condition, not a false isolated-stall diagnosis.");
        }

        private static void OfficialModUiWorkshopRefreshCommitsOnlyAfterSuccessfulCloseSave()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(
                Path.GetTempPath(),
                "DTMAPI-tests-persistent",
                Guid.NewGuid().ToString("N"));
            string authorStateRoot = Path.Combine(
                Path.GetTempPath(),
                "DTMAPI-tests-author-state",
                Guid.NewGuid().ToString("N"));
            string? previousPersistentRoot = Environment.GetEnvironmentVariable(
                "DTMAPI_DOLOC_PERSISTENT_ROOT");
            string? previousAuthorRoot = Environment.GetEnvironmentVariable(
                "DTMAPI_AUTHOR_STATE_ROOT");
            const ulong committedId = 3749000101UL;
            const ulong candidateId = 3749000102UL;
            try
            {
                Environment.SetEnvironmentVariable(
                    "DTMAPI_DOLOC_PERSISTENT_ROOT",
                    persistentRoot);
                Environment.SetEnvironmentVariable(
                    "DTMAPI_AUTHOR_STATE_ROOT",
                    authorStateRoot);

                var runtime = new DtmApiRuntime(
                    new FakeHost(gameDir),
                    new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                var committedManager = new FakeNativeModManager(
                    new FakePublishedFileId(committedId),
                    Path.Combine(gameDir, committedId.ToString(CultureInfo.InvariantCulture)),
                    () => new ArrayList());
                var candidateManager = new FakeNativeModManager(
                    new FakePublishedFileId(candidateId),
                    Path.Combine(gameDir, candidateId.ToString(CultureInfo.InvariantCulture)),
                    () => new ArrayList());

                Assert(bridge.CaptureNativeWorkshopSubscriptions(
                        "unit-official-ui-initial",
                        committedManager,
                        requireSteamInitialized: false),
                    "The official-UI transaction fixture must publish an initial committed native snapshot.");
                runtime.Start();
                string snapshotPath = AuthorSourceStateStore.GetWorkshopSnapshotPath(gameDir);
                Assert(File.ReadAllText(snapshotPath).Contains(
                        committedId.ToString(CultureInfo.InvariantCulture),
                        StringComparison.Ordinal),
                    "The initial committed Workshop source authority should be observable before opening the official UI.");

                object previewUi = new object();
                bridge.BeginOfficialModUiTransaction(previewUi);
                bridge.HandleNativeModManagerReloaded(
                    candidateManager,
                    requireSteamInitialized: false);
                Assert(bridge.OfficialModUiTransactionActiveForTests &&
                    !bridge.OfficialModUiCommitPendingForTests &&
                    File.ReadAllText(snapshotPath).Contains(
                        committedId.ToString(CultureInfo.InvariantCulture),
                        StringComparison.Ordinal),
                    "Opening the official UI must stage only a preview and retain the committed source snapshot.");

                bridge.BeginOfficialModUiClose(previewUi);
                bridge.HandleNativeModManagerReloaded(
                    candidateManager,
                    requireSteamInitialized: false);
                bridge.ObserveOfficialModManagerSave(candidateManager, succeeded: false);
                bridge.CompleteOfficialModUiClose(previewUi);
                bridge.Update();
                Assert(!bridge.OfficialModUiTransactionActiveForTests &&
                    !bridge.OfficialModUiCommitPendingForTests &&
                    File.ReadAllText(snapshotPath).Contains(
                        committedId.ToString(CultureInfo.InvariantCulture),
                        StringComparison.Ordinal),
                    "A failed native SaveModManager result must discard the close candidate without refreshing DTMAPI.");

                object mismatchedUi = new object();
                bridge.BeginOfficialModUiTransaction(mismatchedUi);
                bridge.HandleNativeModManagerReloaded(
                    candidateManager,
                    requireSteamInitialized: false);
                bridge.BeginOfficialModUiClose(mismatchedUi);
                bridge.HandleNativeModManagerReloaded(
                    candidateManager,
                    requireSteamInitialized: false);
                bridge.ObserveOfficialModManagerSave(committedManager, succeeded: true);
                bridge.CompleteOfficialModUiClose(mismatchedUi);
                Assert(!bridge.OfficialModUiCommitPendingForTests,
                    "A successful save for a different ModManager instance must not authorize the official UI candidate.");

                object committedUi = new object();
                bridge.BeginOfficialModUiTransaction(committedUi);
                bridge.HandleNativeModManagerReloaded(
                    candidateManager,
                    requireSteamInitialized: false);
                bridge.BeginOfficialModUiClose(committedUi);
                bridge.HandleNativeModManagerReloaded(
                    candidateManager,
                    requireSteamInitialized: false);
                bridge.ObserveOfficialModManagerSave(candidateManager, succeeded: true);
                bridge.CompleteOfficialModUiClose(committedUi);
                Assert(bridge.OfficialModUiCommitPendingForTests &&
                    File.ReadAllText(snapshotPath).Contains(
                        committedId.ToString(CultureInfo.InvariantCulture),
                        StringComparison.Ordinal),
                    "A successful close must defer publication until the next GameBridge frame.");

                bridge.Update();
                string committedText = File.ReadAllText(snapshotPath);
                Assert(!bridge.OfficialModUiCommitPendingForTests &&
                    committedText.Contains(
                        candidateId.ToString(CultureInfo.InvariantCulture),
                        StringComparison.Ordinal) &&
                    !committedText.Contains(
                        committedId.ToString(CultureInfo.InvariantCulture),
                        StringComparison.Ordinal),
                    "The next GameBridge frame must publish exactly the successful close candidate.");
                runtime.NotifyRuntimeShutdown("unit-official-mod-ui-commit");
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    "DTMAPI_DOLOC_PERSISTENT_ROOT",
                    previousPersistentRoot);
                Environment.SetEnvironmentVariable(
                    "DTMAPI_AUTHOR_STATE_ROOT",
                    previousAuthorRoot);
            }
        }

        private static void NativeWorkshopOptionalInfoFailureRetainsRequiredAuthorityWithoutInventingEnablement()
        {
            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            string? previousAuthorRoot = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT");
            try
            {
                const ulong failingWorkshopId = 3749000025UL;
                string failingGameDir = NewTempGameDir();
                string failingPersistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
                string failingAuthorRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-author-state", Guid.NewGuid().ToString("N"));
                string failingUniqueId = "Yuuka.DTMAPI.NativeInfoFailure";
                string failingWorkshopRoot = Path.Combine(failingGameDir, "steamapps", "workshop", "content", "2285550", failingWorkshopId.ToString(CultureInfo.InvariantCulture));
                WriteSourceAuthorityContentPack(failingWorkshopRoot, failingUniqueId, "Optional native-info failure");
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", failingPersistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", failingAuthorRoot);
                WriteOfficialModInfos(failingPersistentRoot, new OfficialModInfoTestEntry("Workshop." + failingWorkshopId.ToString(CultureInfo.InvariantCulture), true, "Workshop"));

                var failingRuntime = new DtmApiRuntime(new FakeHost(failingGameDir), new ConfigMenuRegistry());
                var failingBridge = new DolocTownGameBridge(failingRuntime);
                var failingManager = new FakeNativeModManager(
                    new FakePublishedFileId(failingWorkshopId),
                    failingWorkshopRoot,
                    () => throw new NullReferenceException("unit-native-info-not-ready"));
                Assert(failingBridge.CaptureNativeWorkshopSubscriptions("unit-test-native-info-failure", failingManager, requireSteamInitialized: false),
                    "Optional GetAllValidModInfos failure must not discard subscribed ID and exact install-root authority.");
                failingRuntime.Start();
                DiscoveredMod failingSelected = failingRuntime.CreateSnapshot().DiscoveredMods.Single(mod => mod.Manifest.UniqueID == failingUniqueId);
                Assert(failingSelected.NativeSubscriptionVerified && failingSelected.OfficialEnabled,
                    "Verified subscription/root should retain the official enabled state when optional native-info enrichment fails.");
                string failingPersisted = File.ReadAllText(AuthorSourceStateStore.GetWorkshopSnapshotPath(failingGameDir));
                Assert(failingPersisted.Contains("\"available\":true", StringComparison.OrdinalIgnoreCase) &&
                    failingPersisted.Contains("\"nativeInfoAvailable\":false", StringComparison.OrdinalIgnoreCase) &&
                    failingPersisted.Contains(JsonEscapeForTest(failingWorkshopRoot), StringComparison.OrdinalIgnoreCase) &&
                    failingPersisted.Contains("GetAllValidModInfos enrichment failed", StringComparison.Ordinal),
                    "Partial native-info failure evidence must persist an available required snapshot, exact root, nullable enablement and stage-qualified failure.");
                failingRuntime.NotifyRuntimeShutdown("unit-native-info-failure");

                WriteOfficialModInfos(failingPersistentRoot, new OfficialModInfoTestEntry("Workshop." + failingWorkshopId.ToString(CultureInfo.InvariantCulture), false, "Workshop"));
                var disabledRuntime = new DtmApiRuntime(new FakeHost(failingGameDir), new ConfigMenuRegistry());
                var disabledBridge = new DolocTownGameBridge(disabledRuntime);
                Assert(disabledBridge.CaptureNativeWorkshopSubscriptions("unit-test-native-info-failure-disabled", failingManager, requireSteamInitialized: false),
                    "The required snapshot should remain capturable for an officially disabled row.");
                disabledRuntime.Start();
                DiscoveredMod disabledSelected = disabledRuntime.CreateSnapshot().DiscoveredMods.Single(mod => mod.Manifest.UniqueID == failingUniqueId);
                Assert(disabledSelected.NativeSubscriptionVerified && !disabledSelected.OfficialEnabled,
                    "Unknown optional native enablement must never be invented as true over an official disabled state.");
                disabledRuntime.NotifyRuntimeShutdown("unit-native-info-failure-disabled");

                const ulong emptyWorkshopId = 3749000026UL;
                string emptyGameDir = NewTempGameDir();
                string emptyPersistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
                string emptyAuthorRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-author-state", Guid.NewGuid().ToString("N"));
                string emptyUniqueId = "Yuuka.DTMAPI.NativeInfoEmpty";
                string emptyWorkshopRoot = Path.Combine(emptyGameDir, "steamapps", "workshop", "content", "2285550", emptyWorkshopId.ToString(CultureInfo.InvariantCulture));
                WriteSourceAuthorityContentPack(emptyWorkshopRoot, emptyUniqueId, "Successful empty native-info snapshot");
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", emptyPersistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", emptyAuthorRoot);
                WriteOfficialModInfos(emptyPersistentRoot, new OfficialModInfoTestEntry("Workshop." + emptyWorkshopId.ToString(CultureInfo.InvariantCulture), true, "Workshop"));
                var emptyRuntime = new DtmApiRuntime(new FakeHost(emptyGameDir), new ConfigMenuRegistry());
                var emptyBridge = new DolocTownGameBridge(emptyRuntime);
                var emptyManager = new FakeNativeModManager(new FakePublishedFileId(emptyWorkshopId), emptyWorkshopRoot, () => new ArrayList());
                Assert(emptyBridge.CaptureNativeWorkshopSubscriptions("unit-test-native-info-empty", emptyManager, requireSteamInitialized: false), "A successful empty native-info snapshot should retain required subscription authority.");
                emptyRuntime.Start();
                DiscoveredMod emptySelected = emptyRuntime.CreateSnapshot().DiscoveredMods.Single(mod => mod.Manifest.UniqueID == emptyUniqueId);
                Assert(emptySelected.NativeSubscriptionVerified && !emptySelected.OfficialEnabled,
                    "A successful native-info call with no matching valid row must remain fail-closed instead of using the partial-failure fallback.");
                emptyRuntime.NotifyRuntimeShutdown("unit-native-info-empty");

                const ulong nullRowWorkshopId = 3749000027UL;
                string nullRowGameDir = NewTempGameDir();
                string nullRowPersistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
                string nullRowAuthorRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-author-state", Guid.NewGuid().ToString("N"));
                string nullRowUniqueId = "Yuuka.DTMAPI.NativeInfoNullRow";
                string nullRowWorkshopRoot = Path.Combine(nullRowGameDir, "steamapps", "workshop", "content", "2285550", nullRowWorkshopId.ToString(CultureInfo.InvariantCulture));
                WriteSourceAuthorityContentPack(nullRowWorkshopRoot, nullRowUniqueId, "Null native-info row");
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", nullRowPersistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", nullRowAuthorRoot);
                WriteOfficialModInfos(nullRowPersistentRoot, new OfficialModInfoTestEntry("Workshop." + nullRowWorkshopId.ToString(CultureInfo.InvariantCulture), false, "Workshop"));
                var nullRowRuntime = new DtmApiRuntime(new FakeHost(nullRowGameDir), new ConfigMenuRegistry());
                var nullRowBridge = new DolocTownGameBridge(nullRowRuntime);
                var nullRowManager = new FakeNativeModManager(
                    new FakePublishedFileId(nullRowWorkshopId),
                    nullRowWorkshopRoot,
                    () => new ArrayList
                    {
                        null,
                        new FakeNativeModInfo("Workshop." + nullRowWorkshopId.ToString(CultureInfo.InvariantCulture), true, 23, nullRowWorkshopId, nullRowWorkshopRoot)
                    });
                Assert(nullRowBridge.CaptureNativeWorkshopSubscriptions("unit-test-native-info-null-row", nullRowManager, requireSteamInitialized: false), "Null optional native-info rows should be skipped without poisoning later valid rows.");
                nullRowRuntime.Start();
                DiscoveredMod nullRowSelected = nullRowRuntime.CreateSnapshot().DiscoveredMods.Single(mod => mod.Manifest.UniqueID == nullRowUniqueId);
                Assert(nullRowSelected.NativeSubscriptionVerified && nullRowSelected.OfficialEnabled,
                    "A later valid native row should supersede stale official disablement after a null row is skipped.");
                string nullRowPersisted = File.ReadAllText(AuthorSourceStateStore.GetWorkshopSnapshotPath(nullRowGameDir));
                Assert(nullRowPersisted.Contains("\"nativeInfoAvailable\":true", StringComparison.OrdinalIgnoreCase) &&
                    nullRowPersisted.Contains("\"nativePriority\":23", StringComparison.OrdinalIgnoreCase),
                    "Null-row handling must still persist the later valid native enablement and priority.");
                nullRowRuntime.NotifyRuntimeShutdown("unit-native-info-null-row");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", previousAuthorRoot);
            }
        }

        private static void NativeWorkshopBridgeUsesCurrentModManagerStateInsteadOfStaleEnablementFile()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            string authorStateRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-author-state", Guid.NewGuid().ToString("N"));
            const string uniqueId = "Yuuka.DTMAPI.NativeSnapshot";
            const ulong workshopId = 3749000021UL;
            string workshopRoot = Path.Combine(gameDir, "steamapps", "workshop", "content", "2285550", workshopId.ToString(CultureInfo.InvariantCulture));
            WriteSourceAuthorityContentPack(workshopRoot, uniqueId, "Native snapshot source");

            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            string? previousAuthorRoot = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", authorStateRoot);
                WriteOfficialModInfos(
                    persistentRoot,
                    new OfficialModInfoTestEntry("Workshop." + workshopId.ToString(CultureInfo.InvariantCulture), false, "Workshop"));

                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                var modManager = new FakeNativeModManager(
                    new FakePublishedFileId(workshopId),
                    workshopRoot,
                    new FakeNativeModInfo(
                        "Workshop." + workshopId.ToString(CultureInfo.InvariantCulture),
                        enabled: true,
                        priority: 17,
                        workshopId,
                        workshopRoot));

                Assert(bridge.CaptureNativeWorkshopSubscriptions("unit-test-current-mod-manager", modManager, requireSteamInitialized: false), "GameBridge should capture a supplied current ModManager snapshot.");
                runtime.Start();
                DiscoveredMod selected = runtime.CreateSnapshot().DiscoveredMods.Single(mod => mod.Manifest.UniqueID == uniqueId);
                Assert(selected.Source == "Workshop" && selected.NativeSubscriptionVerified && selected.OfficialEnabled, "Current ModManager enabled state should supersede a stale disabled mod_infos.json row.");

                string persisted = File.ReadAllText(AuthorSourceStateStore.GetWorkshopSnapshotPath(gameDir));
                Assert(persisted.Contains("\"nativeInfoAvailable\":true", StringComparison.OrdinalIgnoreCase) &&
                    persisted.Contains("\"nativeEnabled\":true", StringComparison.OrdinalIgnoreCase) &&
                    persisted.Contains("\"nativePriority\":17", StringComparison.OrdinalIgnoreCase),
                    "Persisted native source evidence should include valid-info availability, enablement and priority.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", previousAuthorRoot);
            }
        }

        private static void NativeWorkshopSubscriptionWithoutInstalledRootDoesNotAuthorizeRawDirectory()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            string authorStateRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-author-state", Guid.NewGuid().ToString("N"));
            const string uniqueId = "Yuuka.DTMAPI.UninstalledWorkshop";
            const ulong workshopId = 3749000024UL;
            string rawWorkshopRoot = Path.Combine(gameDir, "steamapps", "workshop", "content", "2285550", workshopId.ToString(CultureInfo.InvariantCulture));
            WriteSourceAuthorityContentPack(rawWorkshopRoot, uniqueId, "Raw numeric Workshop directory");

            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            string? previousAuthorRoot = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", authorStateRoot);
                WriteOfficialModInfos(
                    persistentRoot,
                    new OfficialModInfoTestEntry("Workshop." + workshopId.ToString(CultureInfo.InvariantCulture), true, "Workshop"));
                string treeSha256 = AuthorFileTreeDigest.Compute(rawWorkshopRoot);
                WriteAuthorSourceSelectionState(gameDir, uniqueId, "WorkshopValidation", rawWorkshopRoot, playerReproductionActive: false, expectedTreeSha256: treeSha256);
                string sessionId = Guid.NewGuid().ToString("N");
                string token = new string('E', 48);
                WriteAuthorSessionDescriptor(gameDir, sessionId, token, AuthorSessionProtocol.CreatePipeName(gameDir, sessionId));

                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                var modManager = new FakeNativeModManager(
                    new FakePublishedFileId(workshopId),
                    installPath: string.Empty,
                    nativeInfo: new FakeNativeModInfo(
                        "Workshop." + workshopId.ToString(CultureInfo.InvariantCulture),
                        enabled: true,
                        priority: -1,
                        workshopId,
                        rootPath: string.Empty));

                Assert(bridge.CaptureNativeWorkshopSubscriptions("unit-test-subscribed-no-installed-root", modManager, requireSteamInitialized: false), "The native snapshot should retain subscribed-known evidence even when no installed root is available.");
                runtime.Start();
                Assert(runtime.IsAuthorSessionActive, "The raw-directory authorization fixture should have a valid explicit session so the native-root gate is exercised.");
                Assert(!runtime.DiscoveredMods.Any(mod => mod.Manifest.UniqueID == uniqueId), "A subscribed ID without a native installed root must not authorize a stale numeric Workshop directory in Workshop Validation mode.");
                Assert(runtime.Diagnostics.GetWarnings().Any(warning =>
                    warning.Details.Contains("did not provide an installed root", StringComparison.OrdinalIgnoreCase) ||
                    warning.Details.Contains("no native-subscribed installed source", StringComparison.OrdinalIgnoreCase)),
                    "Missing native installed-root authority should be explicit in diagnostics.");
                runtime.NotifyRuntimeShutdown("unit-subscribed-no-installed-root");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", previousAuthorRoot);
            }
        }

        private static void VerifyAuthorAssemblyObservationSeparatesDiskAndResident()
        {
            string path = typeof(DolocTownGameBridge).Assembly.Location;
            DiscoveredMod Selected(string root, string entry) => new DiscoveredMod(
                new ManifestModel { UniqueID = "Tests.IdentityObservation", EntryDll = entry },
                root, Path.Combine(root, "manifest.json"), "Local", null, true, false,
                "Local.Tests.IdentityObservation", true, "test");
            var resident = AuthorAssemblyObservation.Read(Selected(Path.GetDirectoryName(path)!, Path.GetFileName(path)))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            Assert(resident["residentMatchesDiskMvid"] == "True" &&
                resident["residentEntryMvid"] == typeof(DolocTownGameBridge).Module.ModuleVersionId.ToString("D") &&
                resident["diskEntrySha256"] == ComputeFileSha256(path), "A loaded module has an independent resident MVID and disk hash.");
            string copyRoot = NewTempGameDir();
            string copy = Path.Combine(copyRoot, "never-loaded.dll");
            File.Copy(path, copy);
            var diskOnly = AuthorAssemblyObservation.Read(Selected(copyRoot, "never-loaded.dll"))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            Assert(diskOnly["diskEntryMvid"] == resident["diskEntryMvid"] &&
                diskOnly["residentEntryMvid"] == "unavailable" && diskOnly["residentMatchesDiskMvid"] == "unavailable",
                "Identical disk bytes at another path do not prove a resident assembly.");
            File.Delete(copy);
            var missing = AuthorAssemblyObservation.Read(Selected(copyRoot, "never-loaded.dll"))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            Assert(missing["diskEntrySha256"] == "unavailable" && missing["residentMatchesDiskMvid"] == "unavailable",
                "Missing code produces unavailable observation rather than fabricated match.");
        }

        private static void ExplicitAuthorSessionAuthorizesWorkshopValidationAndClosesAtTitle()
        {
            VerifyAuthorAssemblyObservationSeparatesDiskAndResident();
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            string authorStateRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-author-state", Guid.NewGuid().ToString("N"));
            const string uniqueId = "Yuuka.DTMAPI.SessionValidation";
            const ulong workshopId = 3749000022UL;
            string workshopRoot = Path.Combine(gameDir, "steamapps", "workshop", "content", "2285550", workshopId.ToString(CultureInfo.InvariantCulture));
            WriteSourceAuthorityContentPack(workshopRoot, uniqueId, "Explicit session Workshop source");
            string localShadowRoot = Path.Combine(gameDir, "Mods", "Session;ValidationLocalShadow");
            WriteSourceAuthorityContentPack(localShadowRoot, uniqueId, "Explicit session local shadow");

            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            string? previousAuthorRoot = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", authorStateRoot);
                WriteOfficialModInfos(
                    persistentRoot,
                    new OfficialModInfoTestEntry("Workshop." + workshopId.ToString(CultureInfo.InvariantCulture), true, "Workshop"));
                string treeSha256 = AuthorFileTreeDigest.Compute(workshopRoot);
                WriteAuthorSourceSelectionState(
                    gameDir,
                    uniqueId,
                    "WorkshopValidation",
                    string.Empty,
                    playerReproductionActive: false,
                    expectedTreeSha256: string.Empty);

                string sessionId = Guid.NewGuid().ToString("N");
                string token = new string('A', 48);
                string pipeName = AuthorSessionProtocol.CreatePipeName(gameDir, sessionId);
                WriteAuthorSessionDescriptor(gameDir, sessionId, token, pipeName);

                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                _ = new DolocTownGameBridge(runtime);
                runtime.UpdateNativeWorkshopSubscriptions(
                    available: true,
                    source: "unit-test-native-owner",
                    failure: string.Empty,
                    subscriptions: new[]
                    {
                        new NativeWorkshopSubscription(
                            workshopId,
                            workshopRoot,
                            nativeEnabled: true,
                            nativePriority: -1,
                            nativeOfficialId: "Workshop." + workshopId.ToString(CultureInfo.InvariantCulture))
                    });
                runtime.Start();
                Assert(runtime.IsAuthorSessionActive, "A valid one-shot startup descriptor should create an explicit author-session listener.");
                Assert(!File.Exists(AuthorSourceStateStore.GetAuthorSessionPath(gameDir)), "The startup descriptor should be consumed and removed before the listener starts.");
                DiscoveredMod selected = runtime.CreateSnapshot().DiscoveredMods.Single(mod => mod.Manifest.UniqueID == uniqueId);
                Assert(selected.Source == "Workshop" && selected.NativeSubscriptionVerified, "The CLI-produced Workshop Validation state plus an active author session and native snapshot should authorize the native-installed Workshop source without inventing an offline tree/path assertion.");

                string requestId = Guid.NewGuid().ToString("N");
                string request = BuildAuthorSessionRequest(gameDir, sessionId, token, requestId, uniqueId, workshopRoot, treeSha256, "get-source-snapshot");
                string response = string.Empty;
                Exception? clientFailure = null;
                var clientThread = new Thread(() =>
                {
                    try
                    {
                        response = SendAuthorSessionRequest(pipeName, request);
                    }
                    catch (Exception ex)
                    {
                        clientFailure = ex;
                    }
                }) { IsBackground = true };
                clientThread.Start();

                DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(5);
                while ((runtime.AuthorSessionSnapshot?.Pending ?? 0) == 0 && clientFailure == null && DateTimeOffset.UtcNow < deadline)
                    Thread.Sleep(10);
                Assert((runtime.AuthorSessionSnapshot?.Pending ?? 0) == 1, "Authenticated pipe work should be queued without executing on the listener thread.");

                string concurrentRequest = BuildAuthorSessionRequest(
                    gameDir,
                    sessionId,
                    token,
                    Guid.NewGuid().ToString("N"),
                    uniqueId,
                    workshopRoot,
                    treeSha256,
                    "get-source-snapshot");
                string concurrent = SendAuthorSessionRequest(pipeName, concurrentRequest);
                Assert(concurrent.Contains("owner-request-concurrent", StringComparison.Ordinal), "A second in-flight request for the same owner should be rejected before Runtime mutation.");

                runtime.Update();
                Assert(clientThread.Join(TimeSpan.FromSeconds(5)), "The source-snapshot client should receive a bounded Runtime-thread response.");
                if (clientFailure != null)
                    throw new InvalidOperationException("Author-session client failed.", clientFailure);
                using (JsonDocument responseDocument = JsonDocument.Parse(response))
                {
                    JsonElement root = responseDocument.RootElement;
                    Assert(root.GetProperty("requestId").GetString() == requestId && root.GetProperty("status").GetString() == "ok" && root.GetProperty("code").GetString() == "source-snapshot", "Author-session response should preserve request identity and report the source snapshot result.");
                    Dictionary<string, string> values = root.GetProperty("values")
                        .EnumerateArray()
                        .ToDictionary(
                            value => value.GetProperty("key").GetString() ?? string.Empty,
                            value => value.GetProperty("value").GetString() ?? string.Empty,
                            StringComparer.Ordinal);
                    Assert(values["shadowedCount"] == "1" && values["shadowedReturned"] == "1" && values["shadowedTruncated"] == "False", "Source snapshots should report bounded shadow candidate counts without an opaque delimiter payload.");
                    Assert(values["shadowed0Source"] == "Local" && PathsEqualForTest(values["shadowed0Root"], localShadowRoot), "Each shadowed source should retain a separately serialized, reversible source/root identity.");
                    Assert(values.ContainsKey("shadowed0OfficialEnabled") && values.ContainsKey("shadowed0NativeVerified") && values.ContainsKey("shadowed0EnablementReason") && values["shadowed0ShadowReason"].Contains("candidate-not-selected", StringComparison.Ordinal), "Each shadowed source should include enablement, native-owner and explicit shadow-reason fields.");
                    Assert(!values.ContainsKey("shadowed"), "The retired delimiter-based shadowed value must not remain in the protocol response.");
                    Assert(values.ContainsKey("officialEnabled") && values.ContainsKey("ownerActive") && values["residentEntryMvid"] == "unavailable", "Content-only source status distinguishes enablement/activation from unavailable resident code identity.");
                    Assert(values.Count <= 32, "Snapshot additions remain inside the existing bounded protocol.");
                    Assert(!response.Contains(token, StringComparison.Ordinal), "Author-session responses must never echo the bearer token.");
                }

                string replay = SendAuthorSessionRequest(pipeName, request);
                Assert(replay.Contains("request-replay", StringComparison.Ordinal), "A reused requestId should be rejected deterministically as a replay.");
                string wrongTokenRequest = BuildAuthorSessionRequest(gameDir, sessionId, new string('B', 48), Guid.NewGuid().ToString("N"), uniqueId, workshopRoot, treeSha256, "get-source-snapshot");
                string wrongToken = SendAuthorSessionRequest(pipeName, wrongTokenRequest);
                Assert(wrongToken.Contains("authentication-failed", StringComparison.Ordinal), "A wrong author-session token should fail before Runtime queueing.");

                string wrongRuntimeRequest = BuildAuthorSessionRequest(
                    gameDir,
                    sessionId,
                    token,
                    Guid.NewGuid().ToString("N"),
                    uniqueId,
                    workshopRoot,
                    treeSha256,
                    "get-source-snapshot",
                    runtimeVersion: "0.5.4");
                string wrongRuntime = SendAuthorSessionRequest(pipeName, wrongRuntimeRequest);
                Assert(wrongRuntime.Contains("runtime-mismatch", StringComparison.Ordinal), "A request for another Runtime version should fail before Runtime queueing.");

                string wrongGameRootRequest = BuildAuthorSessionRequest(
                    gameDir,
                    sessionId,
                    token,
                    Guid.NewGuid().ToString("N"),
                    uniqueId,
                    workshopRoot,
                    treeSha256,
                    "get-source-snapshot",
                    requestGameRoot: gameDir + "-other");
                string wrongGameRoot = SendAuthorSessionRequest(pipeName, wrongGameRootRequest);
                Assert(wrongGameRoot.Contains("game-root-mismatch", StringComparison.Ordinal), "A request for another game root should fail before Runtime queueing.");

                string selectedRootMismatch = SendAuthorSessionRequestThroughRuntime(
                    runtime,
                    pipeName,
                    BuildAuthorSessionRequest(gameDir, sessionId, token, Guid.NewGuid().ToString("N"), uniqueId, workshopRoot + "-other", treeSha256, "get-source-snapshot"));
                Assert(selectedRootMismatch.Contains("selected-root-mismatch", StringComparison.Ordinal), "Runtime should revalidate the selected source root on its own thread.");

                string treeHashMismatch = SendAuthorSessionRequestThroughRuntime(
                    runtime,
                    pipeName,
                    BuildAuthorSessionRequest(gameDir, sessionId, token, Guid.NewGuid().ToString("N"), uniqueId, workshopRoot, new string('0', 64), "get-source-snapshot"));
                Assert(treeHashMismatch.Contains("source-tree-hash-mismatch", StringComparison.Ordinal), "Runtime should rehash the selected tree immediately before an author operation.");

                string restartRequired = SendAuthorSessionRequestThroughRuntime(
                    runtime,
                    pipeName,
                    BuildAuthorSessionRequest(gameDir, sessionId, token, Guid.NewGuid().ToString("N"), uniqueId, workshopRoot, treeSha256, "reload-content"));
                Assert(restartRequired.Contains("restart-required", StringComparison.Ordinal), "Content without the reviewed Audio owner should return a deterministic restart-required result.");

                runtime.UpdateNativeWorkshopSubscriptions(
                    available: true,
                    source: "unit-test-native-owner-disabled",
                    failure: string.Empty,
                    subscriptions: new[]
                    {
                        new NativeWorkshopSubscription(
                            workshopId,
                            workshopRoot,
                            nativeEnabled: false,
                            nativePriority: -1,
                            nativeOfficialId: "Workshop." + workshopId.ToString(CultureInfo.InvariantCulture))
                    });
                runtime.NotifyWorkshopModListChanged();
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == uniqueId), "Native disablement should deactivate the author-session content owner before another reload request.");
                string disabledReload = SendAuthorSessionRequestThroughRuntime(
                    runtime,
                    pipeName,
                    BuildAuthorSessionRequest(gameDir, sessionId, token, Guid.NewGuid().ToString("N"), uniqueId, workshopRoot, treeSha256, "reload-content"));
                Assert(disabledReload.Contains("source-not-selected", StringComparison.Ordinal), "Enabled-first arbitration must remove a native-disabled Workshop source from author-session selection instead of allowing the session to reactivate it.");

                runtime.NotifyReturnedToTitle();
                Assert(!runtime.IsAuthorSessionActive, "Returned-to-title should close and release the explicit author session.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", previousAuthorRoot);
            }
        }

        private static void ExplicitAuthorSessionClosesAtRuntimeShutdown()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            string authorStateRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-author-state", Guid.NewGuid().ToString("N"));
            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            string? previousAuthorRoot = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", authorStateRoot);
                string sessionId = Guid.NewGuid().ToString("N");
                string token = new string('C', 48);
                WriteAuthorSessionDescriptor(gameDir, sessionId, token, AuthorSessionProtocol.CreatePipeName(gameDir, sessionId));

                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                _ = new DolocTownGameBridge(runtime);
                runtime.Start();
                Assert(runtime.IsAuthorSessionActive, "A valid startup descriptor should activate the author session before the shutdown boundary.");
                Assert(runtime.DiscoveredMods.Count == 0, "The shutdown session fixture must remain isolated from the player's real LocalLow package root.");
                runtime.NotifyRuntimeShutdown("unit-author-session-shutdown");
                Assert(!runtime.IsAuthorSessionActive, "Runtime shutdown should close and release the explicit author session.");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", previousAuthorRoot);
            }
        }

        private static void ExplicitAuthorSessionExpiryRevokesWorkshopValidation()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            string authorStateRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-author-state", Guid.NewGuid().ToString("N"));
            const string uniqueId = "Yuuka.DTMAPI.SessionExpiry";
            const ulong workshopId = 3749000025UL;
            string workshopRoot = Path.Combine(gameDir, "steamapps", "workshop", "content", "2285550", workshopId.ToString(CultureInfo.InvariantCulture));
            WriteSourceAuthorityContentPack(workshopRoot, uniqueId, "Expiring Workshop Validation source");

            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            string? previousAuthorRoot = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", authorStateRoot);
                WriteOfficialModInfos(
                    persistentRoot,
                    new OfficialModInfoTestEntry("Workshop." + workshopId.ToString(CultureInfo.InvariantCulture), true, "Workshop"));
                string treeSha256 = AuthorFileTreeDigest.Compute(workshopRoot);
                WriteAuthorSourceSelectionState(gameDir, uniqueId, "WorkshopValidation", workshopRoot, playerReproductionActive: false, expectedTreeSha256: treeSha256);
                string sessionId = Guid.NewGuid().ToString("N");
                string token = new string('F', 48);
                string pipeName = AuthorSessionProtocol.CreatePipeName(gameDir, sessionId);
                WriteAuthorSessionDescriptor(gameDir, sessionId, token, pipeName, TimeSpan.FromMilliseconds(1500));

                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                _ = new DolocTownGameBridge(runtime);
                runtime.UpdateNativeWorkshopSubscriptions(
                    available: true,
                    source: "unit-test-native-owner",
                    failure: string.Empty,
                    subscriptions: new[]
                    {
                        new NativeWorkshopSubscription(
                            workshopId,
                            workshopRoot,
                            nativeEnabled: true,
                            nativePriority: -1,
                            nativeOfficialId: "Workshop." + workshopId.ToString(CultureInfo.InvariantCulture))
                    });
                runtime.Start();
                Assert(runtime.IsAuthorSessionActive && runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == uniqueId), "Workshop Validation should be authorized only while the short-lived explicit session is current.");

                Thread.Sleep(1700);
                Assert(!runtime.IsAuthorSessionActive, "Session activity must fail closed immediately after expiresAt even before another request arrives.");
                runtime.NotifyWorkshopModListChanged();
                Assert(!runtime.DiscoveredMods.Any(mod => mod.Manifest.UniqueID == uniqueId) && !runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == uniqueId), "An expired session must revoke Workshop Validation during a later native reload/discovery.");
                runtime.Update();
                Assert(runtime.AuthorSessionSnapshot == null || !runtime.AuthorSessionSnapshot.Running, "The Runtime update boundary should close and release an expired author-session host with no queued requests.");

                bool pipeConnected = false;
                try
                {
                    using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None);
                    client.Connect(200);
                    pipeConnected = client.IsConnected;
                }
                catch (TimeoutException)
                {
                    pipeConnected = false;
                }
                catch (IOException)
                {
                    pipeConnected = false;
                }
                Assert(!pipeConnected, "The expired session pipe should be released after the Runtime update closes the host.");
                runtime.NotifyRuntimeShutdown("unit-expired-author-session");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", previousAuthorRoot);
            }
        }

        private static void ExplicitAuthorSessionCannotReloadDependencyBlockedContent()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            string authorStateRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-author-state", Guid.NewGuid().ToString("N"));
            const string uniqueId = "Yuuka.DTMAPI.SessionDependencyBlocked";
            const ulong workshopId = 3749000023UL;
            string workshopRoot = Path.Combine(gameDir, "steamapps", "workshop", "content", "2285550", workshopId.ToString(CultureInfo.InvariantCulture));
            Directory.CreateDirectory(workshopRoot);
            File.WriteAllText(
                Path.Combine(workshopRoot, "manifest.json"),
                "{ \"Name\": \"Dependency-blocked author content\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + uniqueId +
                "\", \"Type\": \"ContentPack\", \"Dependencies\": [{ \"UniqueID\": \"DTMAPI.Tests.MissingAuthorDependency\", \"Required\": true }] }");

            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            string? previousAuthorRoot = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", authorStateRoot);
                WriteOfficialModInfos(
                    persistentRoot,
                    new OfficialModInfoTestEntry("Workshop." + workshopId.ToString(CultureInfo.InvariantCulture), true, "Workshop"));
                string treeSha256 = AuthorFileTreeDigest.Compute(workshopRoot);
                WriteAuthorSourceSelectionState(gameDir, uniqueId, "WorkshopValidation", workshopRoot, playerReproductionActive: false, expectedTreeSha256: treeSha256);

                string sessionId = Guid.NewGuid().ToString("N");
                string token = new string('D', 48);
                string pipeName = AuthorSessionProtocol.CreatePipeName(gameDir, sessionId);
                WriteAuthorSessionDescriptor(gameDir, sessionId, token, pipeName);

                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                _ = new DolocTownGameBridge(runtime);
                runtime.UpdateNativeWorkshopSubscriptions(
                    available: true,
                    source: "unit-test-native-owner",
                    failure: string.Empty,
                    subscriptions: new[]
                    {
                        new NativeWorkshopSubscription(
                            workshopId,
                            workshopRoot,
                            nativeEnabled: true,
                            nativePriority: -1,
                            nativeOfficialId: "Workshop." + workshopId.ToString(CultureInfo.InvariantCulture))
                    });
                runtime.Start();
                Assert(runtime.IsAuthorSessionActive, "The dependency-blocked fixture should still establish its valid explicit session.");
                Assert(runtime.DiscoveredMods.Any(mod => mod.Manifest.UniqueID == uniqueId) && !runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == uniqueId), "Missing required dependencies should keep the selected content source inactive.");

                string response = SendAuthorSessionRequestThroughRuntime(
                    runtime,
                    pipeName,
                    BuildAuthorSessionRequest(gameDir, sessionId, token, Guid.NewGuid().ToString("N"), uniqueId, workshopRoot, treeSha256, "reload-content"));
                Assert(response.Contains("source-not-active", StringComparison.Ordinal), "An explicit session must not bypass Runtime dependency gates when reloading content.");
                runtime.NotifyRuntimeShutdown("unit-dependency-blocked-author-session");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", previousAuthorRoot);
            }
        }

        private static string BuildAuthorSessionRequest(
            string gameDir,
            string sessionId,
            string token,
            string requestId,
            string uniqueId,
            string selectedRoot,
            string expectedTreeSha256,
            string operation,
            string? runtimeVersion = null,
            string? requestGameRoot = null)
        {
            return "{ \"protocol\": \"dtmapi-author-session/1\", \"runtime\": \"" + (runtimeVersion ?? AuthorSessionProtocol.LegacyWireVersion) +
                "\", \"gameRoot\": \"" + JsonEscapeForTest(requestGameRoot ?? gameDir) +
                "\", \"session\": \"" + sessionId +
                "\", \"token\": \"" + token +
                "\", \"requestId\": \"" + requestId +
                "\", \"uniqueId\": \"" + JsonEscapeForTest(uniqueId) +
                "\", \"selectedRoot\": \"" + JsonEscapeForTest(selectedRoot) +
                "\", \"expectedTreeSha256\": \"" + expectedTreeSha256 +
                "\", \"operation\": \"" + operation + "\" }";
        }

        private static string SendAuthorSessionRequestThroughRuntime(DtmApiRuntime runtime, string pipeName, string request)
        {
            string response = string.Empty;
            Exception? clientFailure = null;
            var clientThread = new Thread(() =>
            {
                try
                {
                    response = SendAuthorSessionRequest(pipeName, request);
                }
                catch (Exception ex)
                {
                    clientFailure = ex;
                }
            }) { IsBackground = true };
            clientThread.Start();

            DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(5);
            while ((runtime.AuthorSessionSnapshot?.Pending ?? 0) == 0 && clientFailure == null && DateTimeOffset.UtcNow < deadline)
                Thread.Sleep(10);
            Assert((runtime.AuthorSessionSnapshot?.Pending ?? 0) == 1, "The authenticated author request should reach the bounded Runtime queue.");
            runtime.Update();
            Assert(clientThread.Join(TimeSpan.FromSeconds(5)), "The author request should receive a bounded Runtime-thread response.");
            if (clientFailure != null)
                throw new InvalidOperationException("Author-session client failed.", clientFailure);
            return response;
        }

        private static string SendAuthorSessionRequest(string pipeName, string request)
        {
            using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None);
            client.Connect(5000);
            using var writer = new StreamWriter(client, new UTF8Encoding(false), 4096, leaveOpen: true) { AutoFlush = true };
            writer.WriteLine(request);
            using var reader = new StreamReader(client, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: false, 4096, leaveOpen: true);
            return reader.ReadLine() ?? throw new InvalidDataException("Author-session pipe returned no response frame.");
        }

        private static void HookCallbackSafeFallbacksReturnFallbacksAndRecordDiagnostics()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                Type callbacks = typeof(DolocTownHookCallbacks);
                callbacks.GetProperty(nameof(DolocTownHookCallbacks.Runtime))!.SetValue(null, runtime);
                callbacks.GetProperty(nameof(DolocTownHookCallbacks.Bridge))!.SetValue(null, null);

                MethodInfo safeResult = callbacks.GetMethod("SafeResult", BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(typeof(string));
                MethodInfo safePrefix = callbacks.GetMethod("SafePrefix", BindingFlags.NonPublic | BindingFlags.Static)!;
                MethodInfo safePostfix = callbacks.GetMethod("SafePostfix", BindingFlags.NonPublic | BindingFlags.Static)!;

                string result = (string)safeResult.Invoke(null, new object[] { "Test.SafeResult", "fallback", new Func<string>(() => throw new InvalidOperationException("safe-result-boom")) })!;
                bool prefix = (bool)safePrefix.Invoke(null, new object[] { "Test.SafePrefix", new Func<bool>(() => throw new InvalidOperationException("safe-prefix-boom")), true })!;
                safePostfix.Invoke(null, new object[] { "Test.SafePostfix", new Action(() => throw new InvalidOperationException("safe-postfix-boom")) });
                for (int i = 0; i < 6; i++)
                    safePostfix.Invoke(null, new object[] { "Test.SafePostfix.Repeated", new Action(() => throw new InvalidOperationException("safe-postfix-repeat-boom")) });

                Assert(result == "fallback", "SafeResult should return fallback when a hook callback throws.");
                Assert(prefix, "SafePrefix should use the native-pass fallback when a hook callback throws.");
                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.HookCallback") == 4, "Hook callback safe helpers should record one diagnostics error per failed operation.");
                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Message.Contains("Test.SafePostfix.Repeated", StringComparison.Ordinal)) == 1, "Repeated hook callback failures should be throttled in diagnostics.");
            }
            finally
            {
                DolocTownHookCallbacks.Runtime = null;
                DolocTownHookCallbacks.Bridge = null;
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void NativeUiLayoutRepairKeepsExactRepairsAndRetiresGenericHooks()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var feature = new NativeUiLayoutDiagnosticsFeature(runtime);

                var homeMenu = new DolocTown.UI.HomePageTextMenu();
                DolocAPI.userInput = new DolocAPI.FakeUserInput
                {
                    CurrentState = new DolocTown.HomePageUiState { textMenu = homeMenu }
                };
                feature.Update();
                Assert(homeMenu.slotLayoutGroup.constraintCount == 1 && homeMenu.RebuildCount == 1,
                    "The mandatory active-layout updater must restore the exact HomePage menu without a generic ResetLayoutSize hook.");

                var mainMenu = new DolocTown.UI.MenuUI();
                for (int i = 0; i < 8; i++)
                    mainMenu.slots.Add(new DolocTown.UI.MenuButton { isVisible = i != 3 });
                var activeMainState = new DolocTown.MainMenuUiState { panel = new DolocTown.UI.MainMenuPanel { menu = mainMenu } };
                DolocAPI.userInput = new DolocAPI.FakeUserInput { CurrentState = activeMainState };
                feature.Update();
                Assert(mainMenu.slotLayoutGroup.constraintCount == 7 && mainMenu.RebuildCount == 1,
                    "The mandatory active-layout updater must restore the exact MenuUI visible icon count without a generic ResetLayoutSize hook.");

                string repo = FindRepositoryRoot();
                string featureSource = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown", "Features", "UiDiagnostics", "NativeUiLayoutDiagnosticsFeature.cs"));
                string callbackSource = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown", "Hooking", "DolocTownHookCallbacks.cs"));
                string repairSource = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown", "Features", "UiDiagnostics", "NativeUiLayoutRepairService.cs"));
                string patcherSource = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown", "Hooking", "HarmonyReflectionPatcher.cs"));
                string bootstrapSource = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.BepInExBootstrap", "BootstrapPlugin.cs"));
                string demandSource = File.ReadAllText(Path.Combine(repo, "src", "DTMAPI.GameBridge.DolocTown", "Demand", "DolocTownGameBridge.Demand.cs"));
                string featureCompact = new string(featureSource.Where(character => !char.IsWhiteSpace(character)).ToArray());
                string bootstrapCompact = new string(bootstrapSource.Where(character => !char.IsWhiteSpace(character)).ToArray());
                string demandCompact = new string(demandSource.Where(character => !char.IsWhiteSpace(character)).ToArray());
                Assert(!featureSource.Contains("DolocGridUI`1", StringComparison.Ordinal) &&
                    !featureSource.Contains("ResetLayoutSize", StringComparison.Ordinal) &&
                    !featureSource.Contains("GridLayoutConstraintCountPrefixPatched", StringComparison.Ordinal) &&
                    !callbackSource.Contains("GridLayoutGroupConstraintCountSetPrefix", StringComparison.Ordinal) &&
                    !repairSource.Contains("NormalizeGridLayoutConstraintCount", StringComparison.Ordinal),
                    "Native UI production source must contain neither generic DolocGridUI attempts nor the unreachable global GridLayoutGroup setter lane.");
                Assert(featureCompact.Contains(
                        "patcher.TryPatchPostfix(\"DolocTown.HomePageUiState,Assembly-CSharp\",\"RenderTextMenu\",typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.HomePageRenderTextMenuPostfix),BindingFlags.Public|BindingFlags.Static),0)",
                        StringComparison.Ordinal) &&
                    featureCompact.Contains(
                        "patcher.TryPatchPostfix(\"DolocTown.UI.MainMenuPanel,Assembly-CSharp\",\"OnStartShow\",typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.MainMenuPanelOnStartShowPostfix),BindingFlags.Public|BindingFlags.Static),0)",
                        StringComparison.Ordinal) &&
                    featureCompact.Contains(
                        "patcher.TryPatchPostfix(\"DolocTown.UI.MenuUI,Assembly-CSharp\",\"SetCapacity\",typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.MenuUiSetCapacityPostfix),BindingFlags.Public|BindingFlags.Static),1)",
                        StringComparison.Ordinal) &&
                    featureCompact.Contains(
                        "patcher.TryPatchPostfix(\"DolocTown.UI.GameDataPanel,Assembly-CSharp\",\"SetCapacity\",typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.GameDataPanelSetCapacityPostfix),BindingFlags.Public|BindingFlags.Static),1)",
                        StringComparison.Ordinal) &&
                    featureCompact.Contains("publicvoidUpdate(){RepairService.UpdateActiveMenuLayout();}", StringComparison.Ordinal),
                    "Native UI production source must retain all four exact target/method/callback tuples plus the active-layout dispatch.");
                Assert(demandCompact.Contains(
                        "ConfigureDemandRoute(GameBridgeDemandRoutes.NativeUiLayoutDiagnostics,TimeSpan.FromMilliseconds(250),()=>nativeUiLayoutDiagnosticsFeature?.Update()",
                        StringComparison.Ordinal),
                    "The mandatory active-layout repair must remain wired to the 250ms demand cadence.");
                Assert(!callbackSource.Contains("DolocGridUiResetLayoutSizePostfix", StringComparison.Ordinal) &&
                    !callbackSource.Contains("DolocGridUiSetCapacityPostfix", StringComparison.Ordinal) &&
                    !callbackSource.Contains("HomePageTextMenuResetLayoutSize", StringComparison.Ordinal) &&
                    !callbackSource.Contains("MenuUiResetLayoutSize", StringComparison.Ordinal),
                    "Callbacks dedicated to the six retired generic UI hooks must be absent.");
                Assert(!patcherSource.Contains("TryPatchClosedGeneric", StringComparison.Ordinal),
                    "The patcher helper used only by retired generic UI hooks must be absent.");
                Assert(!bootstrapSource.Contains("TryStartCoroutineLoop", StringComparison.Ordinal) &&
                    !bootstrapSource.Contains("RuntimeCoroutine", StringComparison.Ordinal) &&
                    !bootstrapSource.Contains("TickFromUnity(\"Coroutine\")", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("privatevoidOnPlayerLoopUpdate()", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("TickFromUnity(\"PlayerLoop\");", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("privatevoidOnInputSystemAfterUpdate()", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("TickFromUnity(\"InputSystemAfterUpdate\");", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("publicvoidUpdate()", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("TickFromUnity(\"Update\");", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("privatevoidOnNativeGameFrame()", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("TickFromUnity(\"NativeGameUpdate\");", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("runtime.SaveSessionLoaded+=(slot,isNewGame)=>ReinstallPlayerLoopFrameDriver(\"SaveLoaded\")", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("runtime.ReturnedToTitleBoundary+=()=>ReinstallPlayerLoopFrameDriver(\"ReturnedToTitle\")", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("runtime.NativeLoadGameReturned+=(slot,result)=>ReinstallPlayerLoopFrameDriver(\"LoadGameReturned:\"", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("StartFallbackPump();", StringComparison.Ordinal),
                    "Bootstrap must retire the coroutine driver while retaining exact PlayerLoop, InputSystem, Update, native-frame, lifecycle-reinstall and health-pump wiring.");
                Assert(bootstrapSource.Contains("RecordWarning(\"DTMAPI.BepInExBootstrap\", \"Fallback health pump is unavailable.\"", StringComparison.Ordinal) &&
                    !bootstrapSource.Contains("RecordError(\"DTMAPI.BepInExBootstrap\", \"Failed to start fallback tick pump.\"", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("if(Volatile.Read(refshutdownStarted)!=0||!initialized||runtime==null)return;", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("if(Volatile.Read(refshutdownStarted)==0&&initialized)RunFallbackHealthCheck();", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("privatevoidRunFallbackHealthCheck(){if(Volatile.Read(refshutdownStarted)!=0||!initialized)return;", StringComparison.Ordinal) &&
                    bootstrapCompact.Contains("if(Interlocked.Exchange(refshutdownStarted,1)!=0)return;initialized=false;", StringComparison.Ordinal),
                    "The optional health pump must warn rather than turn player status red, and shutdown must close queued health callbacks before driver cleanup.");
            }
            finally
            {
                DolocAPI.userInput = null;
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void CustomEntityApisAreExperimentalFrozenAndCoreOwned()
        {
            Type[] apiTypes =
            {
                typeof(ICustomAnimalApi),
                typeof(ICustomMonsterApi),
                typeof(ICustomAttackApi),
                typeof(ICustomDroneApi)
            };
            foreach (Type apiType in apiTypes)
            {
                DtmApiStatusAttribute stability = apiType.GetCustomAttribute<DtmApiStatusAttribute>()
                    ?? throw new InvalidOperationException(apiType.Name + " must publish explicit stability metadata.");
                DtmApiDispositionAttribute disposition = apiType.GetCustomAttribute<DtmApiDispositionAttribute>()
                    ?? throw new InvalidOperationException(apiType.Name + " must publish explicit disposition metadata.");
                ObsoleteAttribute warning = apiType.GetCustomAttribute<ObsoleteAttribute>()
                    ?? throw new InvalidOperationException(apiType.Name + " must warn new source consumers while retaining its ABI.");
                Assert(stability.Status == DtmApiStatus.Experimental,
                    apiType.Name + " must be Experimental because its native runtime verbs remain blocked.");
                Assert(disposition.Disposition == DtmApiDisposition.Frozen && disposition.Since == "0.5.5",
                    apiType.Name + " must be Frozen for adoption while retaining its existing binary identity.");
                Assert(!warning.IsError && (warning.Message ?? string.Empty).Contains("frozen", StringComparison.OrdinalIgnoreCase),
                    apiType.Name + " must use a non-error frozen source warning.");
            }

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                runtime.Start();
                _ = new DolocTownGameBridge(runtime);
                Assert(runtime.ModRegistry.GetApi<ICustomAnimalApi>("DTMAPI") != null &&
                    runtime.ModRegistry.GetApi<ICustomMonsterApi>("DTMAPI") != null &&
                    runtime.ModRegistry.GetApi<ICustomAttackApi>("DTMAPI") != null &&
                    runtime.ModRegistry.GetApi<ICustomDroneApi>("DTMAPI") != null,
                    "The frozen registry ABI must remain available from its DTMAPI Core provider.");
                Assert(runtime.ModRegistry.GetApi<ICustomAnimalApi>("DTMAPI.GameBridge.DolocTown") == null &&
                    runtime.ModRegistry.GetApi<ICustomMonsterApi>("DTMAPI.GameBridge.DolocTown") == null &&
                    runtime.ModRegistry.GetApi<ICustomAttackApi>("DTMAPI.GameBridge.DolocTown") == null &&
                    runtime.ModRegistry.GetApi<ICustomDroneApi>("DTMAPI.GameBridge.DolocTown") == null,
                    "GameBridge must not advertise a second CustomEntity provider identity for the same Core registry instance.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void GameBridgeFeatureContractsReflectDemandRouting()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                object[] features = GetGameBridgeFeaturesForTests(bridge);
                Assert(features.Length == 14, "All current GameBridge features should be represented in the component contract list; product rehomes must not leave a phantom feature count.");

                var featureIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (object feature in features)
                {
                    string id = GetReflectedProperty<string>(feature, "Id");
                    object contract = GetReflectedProperty<object>(feature, "Contract");
                    string contractId = GetReflectedProperty<string>(contract, "FeatureId");
                    bool canAutoPause = GetReflectedProperty<bool>(contract, "CanAutoPauseAfterFailure");
                    MethodInfo validate = contract.GetType().GetMethod("Validate", BindingFlags.Public | BindingFlags.Instance)
                        ?? throw new InvalidOperationException("GameBridge feature contract should expose validation.");
                    string validation = (string)(validate.Invoke(contract, new object[] { id }) ?? string.Empty);
                    MethodInfo format = contract.GetType().GetMethod("Format", BindingFlags.Public | BindingFlags.Instance)
                        ?? throw new InvalidOperationException("GameBridge feature contract should expose formatting.");
                    string formatted = (string)(format.Invoke(contract, Array.Empty<object>()) ?? string.Empty);

                    Assert(!string.IsNullOrWhiteSpace(id), "GameBridge feature ids must be non-empty.");
                    Assert(id.Equals(contractId, StringComparison.OrdinalIgnoreCase), "GameBridge feature contract ids must match their feature ids.");
                    Assert(string.IsNullOrWhiteSpace(validation), "GameBridge feature contract should validate: " + id + " -> " + validation);
                    Assert(!canAutoPause, "Phase 7 only declares auto-pause capability; no feature should auto-pause yet.");
                    Assert(contract.GetType().GetProperty("UpdateBucket", BindingFlags.Public | BindingFlags.Instance) == null,
                        "Feature contracts must not retain the dead update-bucket projection: " + id);
                    Assert(!formatted.Contains("updateBucket", StringComparison.OrdinalIgnoreCase),
                        "Formatted feature contracts must report lifecycle requirements only: " + id);
                    featureIds.Add(id);
                }

                Assert(featureIds.Count == features.Length, "Every GameBridge feature contract id should be unique.");
                Assert(typeof(DolocTownGameBridge).GetMethod("UpdateGameBridgeFeatures", BindingFlags.NonPublic | BindingFlags.Instance) == null,
                    "The unused feature-wide update broadcaster must be physically removed.");
                Assert(typeof(DolocTownGameBridge).Assembly.GetType("DTMAPI.GameBridge.DolocTown.GameBridgeFeatureUpdateBucket", throwOnError: false) == null,
                    "The dead update-bucket enum must be physically removed.");
                string demandRouting = bridge.FormatGameBridgeDemandRoutingSummaryForTests();
                Assert(demandRouting.Contains("activeUpdaters=0", StringComparison.Ordinal) &&
                    demandRouting.Contains("dispatches=0", StringComparison.Ordinal) &&
                    demandRouting.Contains("ids=", StringComparison.Ordinal),
                    "GameBridge diagnostics should expose the real demand-routed updater model.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void ItemDisplayNameApiCachesOnlySuccessfulMainThreadLookups()
        {
            string? previousRoot = UseTempPersistentRoot();
            DolocTownGameBridge? bridge = null;
            DolocTownGameBridge? cameraDisabledBridge = null;
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var calls = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var service = new ItemDisplayNameService(runtime, itemId =>
                {
                    calls.TryGetValue(itemId, out int count);
                    calls[itemId] = count + 1;
                    if (itemId.Equals("missing", StringComparison.OrdinalIgnoreCase))
                        return string.Empty;
                    if (itemId.Equals("transient", StringComparison.OrdinalIgnoreCase) && count == 0)
                        throw new InvalidOperationException("transient native lookup failure");
                    return itemId + "-title-" + (count + 1).ToString(CultureInfo.InvariantCulture);
                }, () => true);

                Assert(service.TryGetDisplayName("fish", out string first) && first == "fish-title-1",
                    "IItemDisplayNameApi should return a successful non-empty native title.");
                Assert(service.TryGetDisplayName("FISH", out string cached) && cached == first && calls["fish"] == 1,
                    "Successful item display names should be cached case-insensitively.");

                Assert(!service.TryGetDisplayName("missing", out string missing1) && missing1.Length == 0,
                    "Missing item IDs should fail closed.");
                Assert(!service.TryGetDisplayName("missing", out string missing2) && missing2.Length == 0 && calls["missing"] == 2,
                    "Failed or empty item lookups must not be negatively cached.");
                Assert(!service.TryGetDisplayName("transient", out string transientFailure) && transientFailure.Length == 0,
                    "Native lookup exceptions should fail closed.");
                Assert(service.TryGetDisplayName("transient", out string recovered) && recovered == "transient-title-2" && calls["transient"] == 2,
                    "A transient lookup failure must be retried instead of retained as a negative cache entry.");

                foreach (string boundary in new[] { "SaveLoaded", "ReturnedToTitle" })
                {
                    service.Clear(boundary);
                    int before = calls["fish"];
                    Assert(service.TryGetDisplayName("fish", out string afterBoundary) && calls["fish"] == before + 1 && afterBoundary != first,
                        "The shared item display-name cache should clear at " + boundary + ".");
                }
                service.Clear("standalone fixture complete");

                bridge = new DolocTownGameBridge(runtime);
                var bridgeFeature = GetPrivateField<ItemDisplayNameFeature>(bridge, "itemDisplayNameFeature");
                int bridgeResolverCalls = 0;
                SetPrivateField(bridgeFeature.Service, "resolveDisplayName", new Func<string, string?>(itemId =>
                {
                    bridgeResolverCalls++;
                    return itemId + "-bridge-title";
                }));
                var bridgeCache = GetPrivateField<Dictionary<string, string>>(bridgeFeature.Service, "cache");
                DolocTownHookCallbacks.Runtime = runtime;
                DolocTownHookCallbacks.Bridge = bridge;

                int resetBefore = bridge.EnvironmentResetCountForTests;
                DolocTownHookCallbacks.DolocApiSetEnvCameraPostfix();
                Assert(bridge.EnvironmentResetCountForTests == resetBefore,
                    "The retained SetEnvCamera callback should remain asleep before ItemDisplayName has a successful cached result.");

                Assert(bridgeFeature.Service.TryGetDisplayName("unit-environment-reset-cache", out string bridgeTitle) &&
                    bridgeTitle == "unit-environment-reset-cache-bridge-title" && bridgeResolverCalls == 1,
                    "The bridge-owned ItemDisplayName service should return a successful native title while arming its cache lifecycle route.");
                Assert(bridgeCache.Count == 0,
                    "ItemDisplayName must fail closed by retaining no cache entry before the physical environment-reset Hook is ready.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset) == 1 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.Camera) == 0,
                    "A successful ItemDisplayName lookup should arm only its environment-reset route without activating Camera demand.");
                Assert(bridge.HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ItemDisplayNameEnvironmentReset),
                    "The ItemDisplayName environment-reset callback bit must publish synchronously before any cache entry is retained.");
                RuntimeCapabilityDescriptor itemResetDescriptor = GameBridgeDemandRoutes.Catalog.Single(item =>
                    item.CapabilityId.Equals(GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset, StringComparison.OrdinalIgnoreCase));
                RuntimeCapabilityDescriptor cameraDescriptor = GameBridgeDemandRoutes.Catalog.Single(item =>
                    item.CapabilityId.Equals(GameBridgeDemandRoutes.Camera, StringComparison.OrdinalIgnoreCase));
                Assert(itemResetDescriptor.UpdaterCadence == "none" &&
                    itemResetDescriptor.HookRouteId == "camera.set-env" &&
                    cameraDescriptor.HookRouteId == "camera.compatibility.set-env" &&
                    itemResetDescriptor.HookRouteId != cameraDescriptor.HookRouteId,
                    "ItemDisplayName must retain its shared environment-reset SetEnvCamera route without inheriting the frozen Camera compatibility updater or owner.");

                string bridgeFeatureSource = File.ReadAllText(Path.Combine(
                    FindRepositoryRoot(),
                    "src",
                    "DTMAPI.GameBridge.DolocTown",
                    "DolocTownGameBridge.Features.cs"));
                Assert(bridgeFeatureSource.Contains(
                    "InstallDemandedEnvironmentResetHook(patcher);",
                    StringComparison.Ordinal),
                    "ItemDisplayName demand must retain its dedicated shared environment-reset physical installer.");
                string environmentResetHookSource = File.ReadAllText(Path.Combine(
                    FindRepositoryRoot(),
                    "src",
                    "DTMAPI.GameBridge.DolocTown",
                    "Features",
                    "EnvironmentReset",
                    "EnvironmentResetHookBridge.cs"));
                Assert(environmentResetHookSource.Contains("\"DolocAPI, Assembly-CSharp\"", StringComparison.Ordinal) &&
                    environmentResetHookSource.Contains("\"SetEnvCamera\"", StringComparison.Ordinal) &&
                    environmentResetHookSource.Contains(nameof(DolocTownHookCallbacks.DolocApiSetEnvCameraPostfix), StringComparison.Ordinal),
                    "The shared physical owner must keep the exact DolocAPI.SetEnvCamera Postfix target and production callback.");

                MethodInfo demandedPhysicalCounts = typeof(DolocTownGameBridge).GetMethod(
                    "GetDemandedHookRouteCounts",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("Demanded physical Hook count helper should exist.");
                object?[] itemOnlyPhysicalArgs = { 0, 0 };
                demandedPhysicalCounts.Invoke(bridge, itemOnlyPhysicalArgs);
                int itemOnlyPhysicalTotal = (int)(itemOnlyPhysicalArgs[1] ?? 0);

                const string cameraRouteOwner = "DTMAPI.Tests.ItemDisplayName.SharedHookDedup";
                GameBridgeDemandRoutes.SetOwnerDemand(
                    runtime,
                    GameBridgeDemandRoutes.Camera,
                    cameraRouteOwner,
                    RuntimeDemandSourceType.CapabilityLease,
                    RuntimeDemandLifetime.Owner,
                    "shared-hook-dedup",
                    true,
                    "unit shared physical Hook request deduplication");

                bridge.CommitPendingDemandRoutesForTests();
                RuntimeCapabilityDemandSnapshot resetRoute = runtime.DemandCoordinator.GetSnapshot().Capabilities.Single(item =>
                    item.CapabilityId.Equals(GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset, StringComparison.OrdinalIgnoreCase));
                Assert(resetRoute.LifecycleState == RuntimeCapabilityLifecycleState.Activating &&
                    resetRoute.PatchState == RuntimeCapabilityPatchState.NotInstalled,
                    "The cache-lifecycle route should request its shared native Hook and remain activating until that physical Hook is installed.");
                RuntimeCapabilityDemandSnapshot cameraRoute = runtime.DemandCoordinator.GetSnapshot().Capabilities.Single(item =>
                    item.CapabilityId.Equals(GameBridgeDemandRoutes.Camera, StringComparison.OrdinalIgnoreCase));
                Assert(cameraRoute.LifecycleState == RuntimeCapabilityLifecycleState.Activating &&
                    cameraRoute.PatchState == RuntimeCapabilityPatchState.NotInstalled,
                    "Frozen Camera compatibility should report activating while its separately owned physical Hook is unavailable.");
                Assert(bridge.OptionalHookInstallRequestCountForTests == 2,
                    "Simultaneous Camera compatibility and ItemDisplayName demand should request their two distinct physical owners exactly once each.");
                object?[] demandedPhysicalArgs = { 0, 0 };
                demandedPhysicalCounts.Invoke(bridge, demandedPhysicalArgs);
                Assert((int)(demandedPhysicalArgs[1] ?? 0) == itemOnlyPhysicalTotal + 1,
                    "Adding frozen Camera compatibility beside ItemDisplayName must add its separate compatibility HookRouteId.");

                Assert(bridgeFeature.Service.TryGetDisplayName("unit-environment-reset-cache", out bridgeTitle) &&
                    bridgeResolverCalls == 2 && bridgeCache.Count == 0,
                    "Repeated successful lookups must remain uncached while the physical environment-reset Hook is unavailable.");

                var sharedEnvironmentResetHook = GetPrivateField<EnvironmentResetHookBridge>(bridge, "environmentResetHookBridge");
                SetPrivateProperty(sharedEnvironmentResetHook, "SetEnvCameraPatched", true);
                bridge.RefreshDemandRoutePatchStatesForTests("unit shared environment-reset Hook ready");
                resetRoute = runtime.DemandCoordinator.GetSnapshot().Capabilities.Single(item =>
                    item.CapabilityId.Equals(GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset, StringComparison.OrdinalIgnoreCase));
                Assert(resetRoute.LifecycleState == RuntimeCapabilityLifecycleState.Active &&
                    resetRoute.PatchState == RuntimeCapabilityPatchState.Installed,
                    "The ItemDisplayName cache route may become active only after its shared physical Hook reports installed.");
                cameraRoute = runtime.DemandCoordinator.GetSnapshot().Capabilities.Single(item =>
                    item.CapabilityId.Equals(GameBridgeDemandRoutes.Camera, StringComparison.OrdinalIgnoreCase));
                Assert(cameraRoute.LifecycleState == RuntimeCapabilityLifecycleState.Activating &&
                    cameraRoute.PatchState == RuntimeCapabilityPatchState.NotInstalled,
                    "ItemDisplayName shared-Hook readiness must not claim that the separate Camera compatibility owner is installed.");
                CameraFeature cameraFeature = bridge.CameraFeature
                    ?? throw new InvalidOperationException("Camera compatibility feature should exist for the distinct-owner fixture.");
                SetPrivateProperty(cameraFeature.HookBridge, "SetEnvCameraPatched", true);
                bridge.RefreshDemandRoutePatchStatesForTests("unit Camera compatibility Hook ready");
                cameraRoute = runtime.DemandCoordinator.GetSnapshot().Capabilities.Single(item =>
                    item.CapabilityId.Equals(GameBridgeDemandRoutes.Camera, StringComparison.OrdinalIgnoreCase));
                Assert(cameraRoute.LifecycleState == RuntimeCapabilityLifecycleState.Active &&
                    cameraRoute.PatchState == RuntimeCapabilityPatchState.Installed,
                    "Camera compatibility may become active only after its own exact Hook owner reports installed.");
                Assert(bridgeFeature.Service.TryGetDisplayName("unit-environment-reset-cache", out bridgeTitle) &&
                    bridgeResolverCalls == 3 && bridgeCache.ContainsKey("unit-environment-reset-cache"),
                    "A successful title may enter the positive cache after the shared physical Hook becomes ready.");
                Assert(bridgeFeature.Service.TryGetDisplayName("UNIT-ENVIRONMENT-RESET-CACHE", out string cachedBridgeTitle) &&
                    cachedBridgeTitle == bridgeTitle && bridgeResolverCalls == 3,
                    "The Hook-guarded bridge cache should serve later case-insensitive hits without another native lookup.");
                GameBridgeDemandRoutes.SetOwnerDemand(
                    runtime,
                    GameBridgeDemandRoutes.Camera,
                    cameraRouteOwner,
                    RuntimeDemandSourceType.CapabilityLease,
                    RuntimeDemandLifetime.Owner,
                    "shared-hook-dedup",
                    false,
                    "unit shared physical Hook request deduplication complete");
                bridge.CommitPendingDemandRoutesForTests();

                DolocTownHookCallbacks.DolocApiSetEnvCameraPostfix();
                Assert(!bridgeCache.ContainsKey("unit-environment-reset-cache") &&
                    bridge.EnvironmentResetCountForTests == resetBefore + 1,
                    "The real SetEnvCamera callback should enter GameBridge and invalidate the shared item display-name cache.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset) == 0 &&
                    !bridge.HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ItemDisplayNameEnvironmentReset),
                    "EnvironmentReset should release the ItemDisplayName cache-owned route synchronously after clearing the final entry.");
                IHookStatusInfo cacheClearStatus = runtime.Diagnostics.GetHookStatuses().Single(item =>
                    item.HookId.Equals("SharedNative.ItemDisplayNameEnvironmentReset", StringComparison.OrdinalIgnoreCase));
                Assert(cacheClearStatus.Status == "cleared" &&
                    cacheClearStatus.Source.Contains("EnvironmentReset DolocAPI.SetEnvCamera", StringComparison.OrdinalIgnoreCase) &&
                    cacheClearStatus.Details.Contains("demandReleased=true", StringComparison.OrdinalIgnoreCase),
                    "The real SetEnvCamera route should publish the cache and demand clear evidence used by bounded game acceptance.");
                DolocTownHookCallbacks.DolocApiSetEnvCameraPostfix();
                Assert(bridge.EnvironmentResetCountForTests == resetBefore + 1,
                    "The retained SetEnvCamera callback should return to its zero-demand fast path after cache invalidation.");
                Assert(runtime.Diagnostics.GetHookStatuses().Single(item =>
                        item.HookId.Equals("SharedNative.ItemDisplayNameEnvironmentReset", StringComparison.OrdinalIgnoreCase)).Source == cacheClearStatus.Source,
                    "A later zero-demand callback must not overwrite the causal SetEnvCamera cache-clear evidence.");

                string cameraDisabledDir = NewTempGameDir();
                var cameraDisabledRuntime = new DtmApiRuntime(new FakeHost(cameraDisabledDir), new ConfigMenuRegistry());
                const string cameraDisabledRunId = "51515151515151515151515151515151";
                var cameraDisabledPrepared = new PreparedQaHost(
                    new PassiveQaHostFactory(),
                    new QaHostPreparationContext(cameraDisabledRuntime, cameraDisabledRunId, cameraDisabledRuntime.Paths.DtmApiPath, Array.Empty<byte>()),
                    new GameBridgeFixtureStartupOptions(cameraDisabledRunId, disabledFeatureIds: new[] { "Camera" }));
                cameraDisabledBridge = new DolocTownGameBridge(cameraDisabledRuntime, null, cameraDisabledPrepared);
                Assert(cameraDisabledBridge.CameraFeature == null,
                    "The QA isolation fixture must actually omit CameraFeature for the shared-owner independence proof.");
                var disabledSharedHook = GetPrivateField<EnvironmentResetHookBridge>(cameraDisabledBridge, "environmentResetHookBridge");
                var disabledItemFeature = GetPrivateField<ItemDisplayNameFeature>(cameraDisabledBridge, "itemDisplayNameFeature");
                SetPrivateField(disabledItemFeature.Service, "resolveDisplayName", new Func<string, string?>(itemId => itemId + "-camera-disabled-title"));
                var cameraDisabledCache = GetPrivateField<Dictionary<string, string>>(disabledItemFeature.Service, "cache");
                Assert(disabledItemFeature.Service.TryGetDisplayName("camera-disabled-item", out string cameraDisabledTitle) &&
                    cameraDisabledTitle == "camera-disabled-item-camera-disabled-title" &&
                    cameraDisabledCache.Count == 0,
                    "Camera-disabled ItemDisplayName should return the native title but remain uncached before its independent shared Hook is ready.");
                cameraDisabledBridge.CommitPendingDemandRoutesForTests();
                Assert(cameraDisabledBridge.OptionalHookInstallRequestCountForTests == 1,
                    "Item-only demand must request the shared physical environment-reset Hook when CameraFeature is absent.");
                cameraDisabledBridge.UpdateRuntimeAutomation();
                IHookStatusInfo disabledHookStatus = cameraDisabledRuntime.Diagnostics.GetHookStatuses().Single(item =>
                    item.HookId.Equals("SharedNative.EnvironmentReset", StringComparison.OrdinalIgnoreCase));
                Assert(!disabledSharedHook.SetEnvCameraPatched &&
                    disabledHookStatus.Status == "pending" &&
                    disabledHookStatus.Details.Contains("remains uncached", StringComparison.OrdinalIgnoreCase),
                    "The Camera-disabled Item-only route must reach the shared physical installer and remain fail-closed when the native target is unavailable.");
                SetPrivateProperty(disabledSharedHook, "SetEnvCameraPatched", true);
                cameraDisabledBridge.RefreshDemandRoutePatchStatesForTests("unit Camera-disabled shared Hook ready");
                Assert(disabledItemFeature.Service.TryGetDisplayName("camera-disabled-item", out cameraDisabledTitle) &&
                    cameraDisabledCache.Count == 1,
                    "ItemDisplayName must retain its guarded positive cache after the independent shared Hook becomes ready with CameraFeature absent.");
                DolocTownHookCallbacks.Runtime = cameraDisabledRuntime;
                DolocTownHookCallbacks.Bridge = cameraDisabledBridge;
                int cameraDisabledResetBefore = cameraDisabledBridge.EnvironmentResetCountForTests;
                DolocTownHookCallbacks.DolocApiSetEnvCameraPostfix();
                Assert(cameraDisabledCache.Count == 0 &&
                    cameraDisabledBridge.EnvironmentResetCountForTests == cameraDisabledResetBefore + 1 &&
                    cameraDisabledRuntime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset) == 0 &&
                    !cameraDisabledBridge.HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ItemDisplayNameEnvironmentReset),
                    "The real static callback entry must clear ItemDisplayName and release its route even when CameraFeature is absent.");

                Exception? offThreadFailure = null;
                var thread = new Thread(() =>
                {
                    try
                    {
                        service.TryGetDisplayName("fish", out _);
                    }
                    catch (Exception ex)
                    {
                        offThreadFailure = ex;
                    }
                });
                thread.Start();
                Assert(thread.Join(TimeSpan.FromSeconds(5)), "Off-thread item display-name lookup should finish promptly.");
                Assert(offThreadFailure is InvalidOperationException,
                    "IItemDisplayNameApi must reject calls outside the DTMAPI Runtime/main thread.");
            }
            finally
            {
                try
                {
                    cameraDisabledBridge?.Shutdown("ItemDisplayName Camera-disabled focused Unit cleanup");
                    bridge?.Shutdown("ItemDisplayName focused Unit cleanup");
                }
                finally
                {
                    DolocTownHookCallbacks.Runtime = null;
                    DolocTownHookCallbacks.Bridge = null;
                }
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void GameBridgeFeatureFinalHealthSnapshotAggregatesCounters()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                runtime.Start();
                int expectedFeatureCount = GetGameBridgeFeaturesForTests(bridge).Length;

                string summary = bridge.BuildGameBridgeFinalHealthSummaryForTests("unit");
                Assert(summary.Contains("reason=unit", StringComparison.Ordinal), "Final health summary should include the boundary reason.");
                Assert(summary.Contains("featureCount=" + expectedFeatureCount.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal), "Final health summary should include feature count.");
                Assert(summary.Contains("autoDisabledFeatures=0", StringComparison.Ordinal), "Phase 7 final health summary should report no auto-disabled features.");
                Assert(summary.Contains("demandRouting={", StringComparison.Ordinal), "Final health summary should include demand routing state.");
                Assert(summary.Contains("featureFanout={", StringComparison.Ordinal), "Final health summary should include direct feature fanout counters.");
                Assert(!summary.Contains("buckets={", StringComparison.Ordinal), "Final health summary must not retain the retired bucket distribution.");
                Assert(!summary.Contains("updateDispatch={", StringComparison.Ordinal), "Final health summary must not retain the retired update-dispatch model.");
                Assert(summary.Contains("nativeUi={", StringComparison.Ordinal), "Final health summary should include native handle/UI counters.");
                Assert(summary.Contains("ownerCounters={", StringComparison.Ordinal), "Final health summary should include owner input/event counters.");
                Assert(summary.Contains("resourceLedger={", StringComparison.Ordinal), "Final health summary should include resource ledger summary.");
                Assert(summary.Contains("saveLoad={", StringComparison.Ordinal), "Final health summary should include SaveLoad summary.");
                Assert(summary.Contains("disposeGraph={cleanupFailures=0; needsRestart=0", StringComparison.Ordinal), "Final health summary should include dispose graph cleanup status.");

                string reportPath = runtime.ExportLogs();
                Assert(File.Exists(reportPath), "ExportLogs should write a diagnostics report.");
                IDtmFeatureStatusInfo status = runtime.Diagnostics.GetFeatureStatuses().Single(s => s.FeatureId == "Refactor.GameBridgeFinalHealthSnapshot");
                Assert(status.Success && status.Status == "ok", "LogExport should publish an ok GameBridge final health feature status when cleanup counters are clear.");
                IHookStatusInfo hook = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Refactor.GameBridgeFinalHealthSnapshot");
                Assert(hook.Status == "ok" && hook.Details.Contains("featureCount=" + expectedFeatureCount.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal), "LogExport should publish the GameBridge final health hook status on the runtime queue.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static object[] GetGameBridgeFeaturesForTests(DolocTownGameBridge bridge)
        {
            FieldInfo featuresField = typeof(DolocTownGameBridge).GetField("features", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("GameBridge should keep an internal feature list.");
            var features = (IEnumerable)featuresField.GetValue(bridge)!;
            return features.Cast<object>().ToArray();
        }

        private static T GetReflectedProperty<T>(object instance, string propertyName)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("Expected reflected property " + propertyName + " on " + instance.GetType().FullName + ".");
            object? value = property.GetValue(instance);
            if (value is T typed)
                return typed;
            throw new InvalidOperationException("Reflected property " + propertyName + " was not " + typeof(T).Name + ".");
        }

        private static void GameBridgeFeatureFailureThrottleRecordsOneDiagnosticsErrorButKeepsFailureCount()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                IEventsHelper events = CreateEventsProxy(runtime, "DTMAPI.Tests.FeatureStatusObserver");
                int featureHookStatusEvents = 0;
                events.Diagnostics.HookStatusChanged += (_, e) =>
                {
                    if (e.HookId == "Feature.UnitFeature")
                        featureHookStatusEvents++;
                };

                MethodInfo recordFailure = typeof(DolocTownGameBridge).GetMethod("RecordGameBridgeFeatureDispatchFailure", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("Feature failure throttle helper should exist.");
                MethodInfo publishStatus = typeof(DolocTownGameBridge)
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Single(m => m.Name == "PublishGameBridgeFeatureStatusIfNeeded" && m.GetParameters().Length == 5);
                MethodInfo formatStatus = typeof(DolocTownGameBridge).GetMethod("FormatGameBridgeFeatureStatus", BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new InvalidOperationException("Feature status formatter should exist.");

                for (int i = 0; i < 6; i++)
                {
                    object?[] args = new object?[] { "UnitFeature", "Update", new InvalidOperationException("feature-update-boom"), null };
                    object dispatchStatus = recordFailure.Invoke(bridge, args) ?? throw new InvalidOperationException("Feature status should be returned.");
                    object publication = args[3] ?? throw new InvalidOperationException("Feature failure publication should be returned.");
                    PropertyInfo shouldPublishHookStatusProperty = publication.GetType().GetProperty("ShouldPublishHookStatus", BindingFlags.NonPublic | BindingFlags.Instance)
                        ?? throw new InvalidOperationException("Feature failure publication should expose hook-status publication decision.");
                    bool shouldPublishHookStatus = (bool)shouldPublishHookStatusProperty.GetValue(publication)!;
                    string details = "Update failed: InvalidOperationException: feature-update-boom. " + (string)formatStatus.Invoke(null, new[] { dispatchStatus })!;
                    publishStatus.Invoke(bridge, new object[] { dispatchStatus, "failed", "Update", details, shouldPublishHookStatus });
                }

                runtime.FlushRuntimeQueues("UnitTest.FeatureFailureThrottle");

                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.Feature.UnitFeature") == 1, "Repeated feature-host failures for the same operation should record only the first diagnostics error.");
                Assert(featureHookStatusEvents == 1, "Repeated feature-host failures should coalesce Feature.UnitFeature hook status publications until runtime queue flush.");

                FieldInfo featureStatusesField = typeof(DolocTownGameBridge).GetField("featureStatuses", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("Feature status dictionary should exist.");
                var featureStatuses = (IDictionary)featureStatusesField.GetValue(bridge)!;
                object status = featureStatuses["UnitFeature"] ?? throw new InvalidOperationException("Feature status should be recorded.");
                PropertyInfo failureCountProperty = status.GetType().GetProperty("FailureCount", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("Feature status failure count should be readable.");
                PropertyInfo lastErrorProperty = status.GetType().GetProperty("LastError", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("Feature status last error should be readable.");

                Assert((int)failureCountProperty.GetValue(status)! == 6, "Feature status failure count should still grow for repeated failures.");
                Assert(((string)lastErrorProperty.GetValue(status)!).Contains("feature-update-boom"), "Feature status should keep the latest error text.");
                IDtmFeatureStatusInfo diagnosticStatus = runtime.Diagnostics.GetFeatureStatuses().Single(s => s.FeatureId == "UnitFeature");
                Assert(diagnosticStatus.FailureCount == 6, "Diagnostics feature snapshot should keep the latest failure count even when hook-status publication is throttled.");
                Assert(diagnosticStatus.LastError.Contains("feature-update-boom"), "Diagnostics feature snapshot should keep the latest error text.");
                IHookStatusInfo hookStatus = runtime.Diagnostics.GetHookStatuses().Single(s => s.HookId == "Feature.UnitFeature");
                Assert(hookStatus.Details.Contains("failureCount=3"), "Published hook status should stop at the last allowed short-warning publication.");
                Assert(hookStatus.Details.Contains("consecutiveFailureCount=3"), "Published hook status should expose the consecutive failure count from the last allowed short-warning publication.");
                Assert(!hookStatus.Details.Contains("failureCount=6"), "Suppressed repeated failures should not rewrite the hook status details every time failureCount changes.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void GameBridgeFeatureFailureRecoveryStartsNewDiagnosticsEpisodeAfterStableSuccess()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                MethodInfo recordFailure = typeof(DolocTownGameBridge).GetMethod("RecordGameBridgeFeatureDispatchFailure", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("Feature failure throttle helper should exist.");
                MethodInfo recordSuccess = typeof(DolocTownGameBridge).GetMethod("RecordGameBridgeFeatureSuccess", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("Feature success helper should exist.");
                MethodInfo publishStatus = typeof(DolocTownGameBridge)
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Single(m => m.Name == "PublishGameBridgeFeatureStatusIfNeeded" && m.GetParameters().Length == 5);
                MethodInfo formatStatus = typeof(DolocTownGameBridge).GetMethod("FormatGameBridgeFeatureStatus", BindingFlags.NonPublic | BindingFlags.Static)
                    ?? throw new InvalidOperationException("Feature status formatter should exist.");

                void PublishFailure(string message)
                {
                    object?[] args = new object?[] { "UnitFeature", "Update", new InvalidOperationException(message), null };
                    object dispatchStatus = recordFailure.Invoke(bridge, args) ?? throw new InvalidOperationException("Feature status should be returned.");
                    object publication = args[3] ?? throw new InvalidOperationException("Feature failure publication should be returned.");
                    PropertyInfo shouldPublishHookStatusProperty = publication.GetType().GetProperty("ShouldPublishHookStatus", BindingFlags.NonPublic | BindingFlags.Instance)
                        ?? throw new InvalidOperationException("Feature failure publication should expose hook-status publication decision.");
                    bool shouldPublishHookStatus = (bool)shouldPublishHookStatusProperty.GetValue(publication)!;
                    string details = "Update failed: InvalidOperationException: " + message + ". " + (string)formatStatus.Invoke(null, new[] { dispatchStatus })!;
                    publishStatus.Invoke(bridge, new object[] { dispatchStatus, "failed", "Update", details, shouldPublishHookStatus });
                }

                void PublishSuccess()
                {
                    object dispatchStatus = recordSuccess.Invoke(bridge, new object[] { "UnitFeature", "Update" })
                        ?? throw new InvalidOperationException("Feature status should be returned.");
                    string details = "Update recovered. " + (string)formatStatus.Invoke(null, new[] { dispatchStatus })!;
                    publishStatus.Invoke(bridge, new object[] { dispatchStatus, "ready", "Update", details, false });
                }

                for (int i = 0; i < 6; i++)
                    PublishFailure("feature-update-boom");
                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.Feature.UnitFeature") == 1, "The initial repeated failure episode should record one diagnostics error.");

                for (int i = 0; i < 3; i++)
                    PublishSuccess();

                IDtmFeatureStatusInfo recoveredStatus = runtime.Diagnostics.GetFeatureStatuses().Single(s => s.FeatureId == "UnitFeature");
                Assert(recoveredStatus.Success, "Recovered feature status should report success after stable successes.");
                Assert(recoveredStatus.FailureCount == 6, "Recovered feature status should preserve cumulative failure count.");
                Assert(recoveredStatus.Details.Contains("consecutiveFailureCount=0"), "Recovered feature status should reset the consecutive failure count.");
                Assert(recoveredStatus.Details.Contains("lastRecoveredAt=") && !recoveredStatus.Details.Contains("lastRecoveredAt=none"), "Recovered feature status should record the recovery timestamp.");

                PublishFailure("feature-update-boom-after-recovery");

                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.Feature.UnitFeature") == 2, "A new failure after stable recovery should start a fresh diagnostics episode.");
                IDtmFeatureStatusInfo failedAgainStatus = runtime.Diagnostics.GetFeatureStatuses().Single(s => s.FeatureId == "UnitFeature");
                Assert(failedAgainStatus.FailureCount == 7, "FailureCount should remain cumulative across recovery episodes.");
                Assert(failedAgainStatus.Details.Contains("consecutiveFailureCount=1"), "A new episode should start with one consecutive failure.");
                Assert(failedAgainStatus.LastError.Contains("feature-update-boom-after-recovery"), "Diagnostics feature status should keep the newest post-recovery error.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void GameBridgeFeatureExceptionSummaryUnwrapsTargetInvocation()
        {
            MethodInfo formatSummary = typeof(DolocTownGameBridge).GetMethod("FormatGameBridgeExceptionSummary", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("Feature exception summary helper should exist.");
            MethodInfo formatDetails = typeof(DolocTownGameBridge).GetMethod("FormatGameBridgeExceptionDetails", BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("Feature exception details helper should exist.");

            var root = new InvalidOperationException("animal clone missing");
            var wrapped = new TargetInvocationException("outer reflection call", new TargetInvocationException("inner reflection call", root));
            string summary = (string)(formatSummary.Invoke(null, new object[] { wrapped }) ?? string.Empty);
            string details = (string)(formatDetails.Invoke(null, new object[] { wrapped }) ?? string.Empty);

            Assert(summary.StartsWith("InvalidOperationException: animal clone missing", StringComparison.Ordinal), "Feature exception summaries should start with the unwrapped root cause.");
            Assert(summary.Contains("outer TargetInvocationException", StringComparison.Ordinal), "Feature exception summaries should keep the outer reflection wrapper as context.");
            Assert(details.Contains("RootCause: System.InvalidOperationException: animal clone missing", StringComparison.Ordinal), "Feature exception details should put the root cause before wrapper stack traces.");
            Assert(details.Contains("OuterException: System.Reflection.TargetInvocationException", StringComparison.Ordinal), "Feature exception details should still include the reflection wrapper.");
        }

        private static void GameBridgeEnvironmentResetStepFailuresAreIsolated()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                MethodInfo runStep = typeof(DolocTownGameBridge).GetMethod("RunEnvironmentResetStep", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?? throw new InvalidOperationException("EnvironmentReset isolation helper should exist.");

                runStep.Invoke(bridge, new object[] { "Unit.ThrowingStep", new Action(() => throw new InvalidOperationException("fanout-boom")) });
                runStep.Invoke(bridge, new object[] { "Unit.HealthyStep", new Action(() => { }) });

                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.EnvironmentReset") == 1, "EnvironmentReset pre-step failures should be recorded without throwing through the caller.");
                IHookStatusInfo status = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Runtime.EnvironmentResetFanout");
                Assert(status.Status == "degraded" && status.Source == "Unit.ThrowingStep", "EnvironmentReset pre-step failure should publish a degraded hook status.");

                runStep.Invoke(bridge, new object[] { "Unit.ThrowingStep", new Action(() => { }) });
                runStep.Invoke(bridge, new object[] { "Unit.ThrowingStep", new Action(() => { }) });
                runStep.Invoke(bridge, new object[] { "Unit.ThrowingStep", new Action(() => { }) });
                IHookStatusInfo recovered = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Runtime.EnvironmentResetFanout");
                Assert(recovered.Status == "recovered" && recovered.Source == "Unit.ThrowingStep", "EnvironmentReset fanout should publish recovery after a failed step becomes stable again.");

                runStep.Invoke(bridge, new object[] { "Unit.ThrowingStep", new Action(() => throw new ArgumentException("different-fanout-boom")) });
                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.EnvironmentReset") == 2, "EnvironmentReset failure throttling should record a new diagnostic when the exception fingerprint changes.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void HarmonyTargetSignatureSelectsExactOverload()
        {
            string dir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
            var patcher = new HarmonyReflectionPatcher(runtime);
            string targetTypeName = typeof(HarmonySignatureProbe).AssemblyQualifiedName ?? throw new InvalidOperationException("Probe type should have an assembly-qualified name.");

            MethodInfo? loose = patcher.ResolveMethod(targetTypeName, nameof(HarmonySignatureProbe.Overload), 1);
            Assert(loose != null && loose.GetParameters().Length == 1, "Loose Harmony target lookup should keep the old parameter-count behavior.");

            MethodInfo? intOverload = patcher.ResolveMethod(
                targetTypeName,
                nameof(HarmonySignatureProbe.Overload),
                HarmonyTargetSignature.Exact(typeof(HarmonySignatureProbe), typeof(int), typeof(int)));
            Assert(intOverload != null && intOverload.ReturnType == typeof(int) && intOverload.GetParameters()[0].ParameterType == typeof(int), "Exact Harmony signature should select the int overload.");

            MethodInfo? stringOverload = patcher.ResolveMethod(
                targetTypeName,
                nameof(HarmonySignatureProbe.Overload),
                HarmonyTargetSignature.Exact(typeof(HarmonySignatureProbe), typeof(string), typeof(string)));
            Assert(stringOverload != null && stringOverload.ReturnType == typeof(string) && stringOverload.GetParameters()[0].ParameterType == typeof(string), "Exact Harmony signature should select the string overload.");

            MethodInfo? typeNameOverload = patcher.ResolveMethod(
                targetTypeName,
                nameof(HarmonySignatureProbe.Overload),
                HarmonyTargetSignature.Exact(
                    typeof(HarmonySignatureProbe).FullName ?? string.Empty,
                    typeof(string).FullName ?? string.Empty,
                    typeof(string).FullName ?? string.Empty));
            Assert(typeNameOverload == stringOverload, "Harmony signature matching should support runtime type-name signatures for game types.");

            MethodInfo? wrongReturn = patcher.ResolveMethod(
                targetTypeName,
                nameof(HarmonySignatureProbe.Overload),
                new HarmonyTargetSignature(returnType: typeof(bool), parameterTypes: new[] { typeof(int) }));
            Assert(wrongReturn == null, "Harmony signature matching should reject the right parameter list with the wrong return type.");

            MethodInfo? wrongDeclaringType = patcher.ResolveMethod(
                targetTypeName,
                nameof(HarmonySignatureProbe.Overload),
                HarmonyTargetSignature.Exact(typeof(string), typeof(int), typeof(int)));
            Assert(wrongDeclaringType == null, "Harmony signature matching should reject the right shape on the wrong declaring type.");
        }

        private readonly struct FakePublishedFileId
        {
            public FakePublishedFileId(ulong value)
            {
                m_PublishedFileId = value;
            }

#pragma warning disable IDE1006
            public readonly ulong m_PublishedFileId;
#pragma warning restore IDE1006
        }

        private sealed class FakeNativeModManager
        {
            private readonly FakePublishedFileId publishedFileId;
            private readonly string installPath;
            private readonly Func<IEnumerable> nativeInfoFactory;

            public FakeNativeModManager(FakePublishedFileId publishedFileId, string installPath, FakeNativeModInfo nativeInfo)
                : this(publishedFileId, installPath, () => new ArrayList { nativeInfo })
            {
            }

            public FakeNativeModManager(FakePublishedFileId publishedFileId, string installPath, Func<IEnumerable> nativeInfoFactory)
            {
                this.publishedFileId = publishedFileId;
                this.installPath = installPath;
                this.nativeInfoFactory = nativeInfoFactory;
            }

            public List<FakePublishedFileId> GetSubscribedMods() => new List<FakePublishedFileId> { publishedFileId };

            public bool GetSubscribedModDirectory(FakePublishedFileId id, out string directory)
            {
                directory = id.m_PublishedFileId == publishedFileId.m_PublishedFileId ? installPath : string.Empty;
                return directory.Length > 0;
            }

            public IEnumerable GetAllValidModInfos() => nativeInfoFactory();
        }

        private enum FakeNativeModSource
        {
            Workshop
        }

        private sealed class FakeNativeModInfo
        {
            public FakeNativeModInfo(string id, bool enabled, int priority, ulong workshopId, string rootPath)
            {
                this.id = id;
                this.enabled = enabled;
                this.priority = priority;
                this.workshopId = workshopId;
                this.rootPath = rootPath;
            }

#pragma warning disable IDE1006
            public readonly string id;
            public readonly bool enabled;
            public readonly int priority;
            public readonly FakeNativeModSource source = FakeNativeModSource.Workshop;
            public ulong workshopId { get; }
            public string rootPath { get; }
#pragma warning restore IDE1006
        }

        private static void SetPrivateProperty(object target, string propertyName, object? value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Expected property '" + propertyName + "' on " + target.GetType().FullName + ".");
            property.SetValue(target, value);
        }

        private sealed class HarmonySignatureProbe
        {
            public int Overload(int value) => value;

            public string Overload(string value) => value;

            public int Overload(int value, string suffix) => value;
        }
    }
}
