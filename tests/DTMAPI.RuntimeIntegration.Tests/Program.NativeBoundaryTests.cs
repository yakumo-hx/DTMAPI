#pragma warning disable CS0618 // Frozen compatibility contracts are intentionally exercised.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
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

        private static void HookRetriesUseTheFrameClockAndClosePermanently()
        {
            double now = 10;
            var scheduler = new HookInstallScheduler(() => now);
            scheduler.StartRetry();
            now = 11;
            scheduler.PollRetry();
            Assert(!scheduler.TryConsumePending(), "No Hook retry may run before its two-second deadline.");
            now = 12;
            scheduler.PollRetry();
            scheduler.PollRetry();
            Assert(scheduler.TryConsumePending(), "A due frame schedules one retry, without a background worker or duplicate due work.");
            Assert(!scheduler.TryConsumePending(), "A pending retry is consumed once.");
            scheduler.StopRetry();
            now = 30;
            scheduler.PollRetry();
            Assert(!scheduler.TryConsumePending() && !scheduler.RetryActive, "Stopped retries cannot bleed into a later session.");
            scheduler.StartRetry();
            scheduler.Close();
            scheduler.Request();
            scheduler.StartRetry();
            now = 60;
            scheduler.PollRetry();
            Assert(!scheduler.TryConsumePending() && !scheduler.RetryActive, "A closed scheduler rejects every late request and cannot restart.");

            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
            var bridge = new DolocTownGameBridge(runtime);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            foreach (string field in new[]
            {
                "saveLoadedPatched", "loadRequestedPatched", "loadReturnedPatched", "nativeGameFramePatched",
                "saveSavingPatched", "saveSavedPatched", "returnHomeRequestedPatched", "returnHomePatched",
                "workshopReloadPatched", "workshopModUiRegisterPatched", "workshopModUiHidePatched",
                "workshopModManagerSavePatched", "workshopModUiCloseTransactionPatched"
            })
                typeof(DolocTownGameBridge).GetField(field, flags)!.SetValue(bridge, true);
            var bridgeScheduler = (HookInstallScheduler)typeof(DolocTownGameBridge)
                .GetField("hookInstallScheduler", flags)!.GetValue(bridge)!;
            MethodInfo process = typeof(DolocTownGameBridge).GetMethod("ProcessPendingHookInstallRequests", flags)!;
            bridgeScheduler.StartRetry();
            bridgeScheduler.Request();
            process.Invoke(bridge, null);
            Assert(bridgeScheduler.RetryActive,
                "Installed save/frame/workshop hooks must not stop retries while platform world boundaries remain unavailable.");

            foreach (string field in new[]
            {
                "platformNewGamePrefix", "platformNewGamePostfix", "platformNewGameFinalizer", "platformLoadFinalizer",
                "platformSceneStart", "platformDungeonStart", "platformQuitRoom", "platformRoomEnterPrefix", "platformRoomEnterPostfix"
            })
                typeof(DolocTownGameBridge).GetField(field, flags)!.SetValue(bridge, true);
            bridgeScheduler.Request();
            process.Invoke(bridge, null);
            Assert(!bridgeScheduler.RetryActive, "Installing the remaining platform world boundaries completes and stops Hook retries.");
            bridgeScheduler.Close();
        }

        private static bool LatchInputSamples(IReadOnlyList<InputButtonSample> samples, int unityFrame = -1)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            Type input = typeof(ReflectedUnityInput);
            if (!(bool)input.GetMethod("BeginLatchFrame", flags)!.Invoke(null, new object[] { unityFrame })!)
                return false;
            MethodInfo latch = input.GetMethod("LatchSample", flags)!;
            foreach (InputButtonSample sample in samples)
                latch.Invoke(null, new object[] { sample });
            input.GetMethod("PruneUnobservedLatchedButtons", flags)!.Invoke(null, null);
            return true;
        }

        private static void InputNativeEdgeLatchRetainsAndConsumesShortTapOnce()
        {
            ReflectedUnityInput.ClearTransientState();
            var samples = new[] { new InputButtonSample("F6", false, true, true) };
            LatchInputSamples(samples);

            InputButtonSample first = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(first.PressedEdge && first.ReleasedEdge && !first.IsDownNow, "The frame latch should retain both edges of a short tap until Core consumes it.");

            InputButtonSample consumed = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(!consumed.PressedEdge && !consumed.ReleasedEdge, "A latched edge should be consumed exactly once.");

            LatchInputSamples(samples);
            LatchInputSamples(new[] { new InputButtonSample("F7", false, false, false) });
            InputButtonSample expired = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(!expired.PressedEdge && !expired.ReleasedEdge, "A button omitted by the next watched frame should not retain a stale edge.");
            ReflectedUnityInput.ClearTransientState();
        }

        private static void InputNativeEdgeLatchRejectsDuplicateSourcesAndHeldPress()
        {
            ReflectedUnityInput.ClearTransientState();
            bool firstSource = LatchInputSamples(
                new[] { new InputButtonSample("F6", true, true, false) },
                100);
            bool duplicateSource = LatchInputSamples(
                new[] { new InputButtonSample("F6", true, true, false) },
                100);
            Assert(firstSource && !duplicateSource, "Only one input source should latch registered buttons for a Unity frame.");

            InputButtonSample first = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(first.PressedEdge && first.IsDownNow, "The first source should deliver the physical press.");

            LatchInputSamples(
                new[] { new InputButtonSample("F6", true, true, false) },
                101);
            InputButtonSample repeated = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(!repeated.PressedEdge && repeated.IsDownNow, "A repeated backend pressed flag before release must not become a second physical press.");

            LatchInputSamples(
                new[] { new InputButtonSample("F6", false, false, true) },
                102);
            InputButtonSample released = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(released.ReleasedEdge && !released.IsDownNow, "The held cycle should close on release.");

            LatchInputSamples(
                new[] { new InputButtonSample("F6", true, true, false) },
                103);
            InputButtonSample nextPress = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(nextPress.PressedEdge && nextPress.IsDownNow, "A new press after release must still dispatch normally.");
            ReflectedUnityInput.ClearTransientState();

            LatchInputSamples(new[] { new InputButtonSample("F6", true, false, false) });
            InputButtonSample firstHeld = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(firstHeld.IsDownNow && !firstHeld.PressedEdge, "First observing a held key without a native edge must not invent a press.");
            LatchInputSamples(Array.Empty<InputButtonSample>());
            LatchInputSamples(new[] { new InputButtonSample("F6", true, false, false) });
            InputButtonSample resumedHeld = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(resumedHeld.IsDownNow && !resumedHeld.PressedEdge, "Resuming a pruned held key must preserve the absence of a new edge.");
            LatchInputSamples(Array.Empty<InputButtonSample>());
            LatchInputSamples(new[] { new InputButtonSample("F6", true, true, false) });
            Assert(ReflectedUnityInput.SampleButtonCached("F6").PressedEdge, "A genuine first press after pruning must remain deliverable.");
            LatchInputSamples(new[] { new InputButtonSample("F6", false, false, false) });
            Assert(ReflectedUnityInput.SampleButtonCached("F6").ReleasedEdge, "Continuous sampled release remains inferable.");
            LatchInputSamples(new[] { new InputButtonSample("F6", true, false, false) });
            Assert(ReflectedUnityInput.SampleButtonCached("F6").PressedEdge, "Continuous sampled neutral-to-held transition remains inferable.");
            LatchInputSamples(new[] { new InputButtonSample("F6", true, true, true) });
            InputButtonSample rapid = ReflectedUnityInput.SampleButtonCached("F6");
            Assert(rapid.PressedEdge && rapid.ReleasedEdge && rapid.IsDownNow, "A real rapid release/repress retains both edges.");
            ReflectedUnityInput.ClearTransientState();
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
            HiddenTitleSettingsSettleAndStillProcessNewBoundaryWork();
        }

        private static void HiddenTitleSettingsSettleAndStillProcessNewBoundaryWork()
        {
            var menu = new ConfigMenuRegistry();
            var host = new FakeHost(NewTempGameDir());
            var runtime = new DtmApiRuntime(host, menu);
            // This fixture intentionally does not Start the runtime, whose constructor installs a NullMonitor.
            typeof(DtmApiRuntime).GetProperty(nameof(DtmApiRuntime.RuntimeMonitor))!.SetValue(runtime,
                new FileMonitor(host, "DTMAPI", Path.Combine(host.GamePath, "hidden-title.log")));
            var titleHost = CreateMountedTitleUi(runtime, menu, out FakeTitleGameObject panel, out FakeTitleGameObject button);
            runtime.UI.SetUiContext("NormalGameState", false, true, "hidden title regression");
            runtime.UI.OpenDtmApiStatusPage();
            titleHost.Update();
            Assert(!runtime.UI.IsOpen && !panel.activeInHierarchy && button.activeInHierarchy,
                "Leaving title must close its owned menu and hide the panel.");
            int resets = panel.SetActiveCalls;
            titleHost.Update();
            titleHost.Update();
            Assert(panel.SetActiveCalls == resets && button.SetActiveCalls == resets,
                "A settled hidden title must not repeat panel or button SetActive each frame.");

            runtime.UI.OpenCustomMenu("DTMAPI.DebugConsole");
            titleHost.Update();
            Assert(runtime.UI.IsOpen && runtime.UI.ActiveMenuId == "DTMAPI.DebugConsole" && panel.SetActiveCalls == resets,
                "Another owner's menu must remain open while the title host is idle.");
            runtime.UI.Close();
            runtime.UI.OpenDtmApiStatusPage();
            titleHost.Update();
            Assert(!runtime.UI.IsOpen && panel.SetActiveCalls == ++resets,
                "A new owned menu request while already hidden must still be closed.");

            var barrier = GetPrivateField<EditRenderBarrier>(titleHost, "editRenderBarrier");
            barrier.Defer();
            titleHost.Update();
            Assert(!barrier.IsPending && panel.SetActiveCalls == ++resets,
                "A deferred edit arriving while hidden must not survive into the next title session.");

            var cancellation = new CancellationTokenSource();
            CancellationToken token = cancellation.Token;
            var pending = new System.Threading.Tasks.TaskCompletionSource<DtmCommandResult>();
            SetPrivateField(titleHost, "platformCommandCancellation", cancellation);
            SetPrivateField(titleHost, "pendingPlatformCommand", pending.Task);
            titleHost.Update();
            Assert(token.IsCancellationRequested && panel.SetActiveCalls == ++resets,
                "An unfinished command arriving while hidden must still be cancelled.");

            var completed = System.Threading.Tasks.Task.FromResult(new DtmCommandResult(
                "hidden-completion", "DTMAPI.Tests", new DtmWorkResult(DtmWorkStatus.Succeeded), new[] { "done" }));
            SetPrivateField(titleHost, "pendingPlatformCommand", completed);
            titleHost.Update();
            int completionLogs = host.Logs.Count(line => line.Contains("Mod command request=hidden-completion", StringComparison.Ordinal));
            int outputLines = GetPrivateField<string[]>(titleHost, "platformCommandOutput").Length;
            string completionStatus = GetPrivateField<string>(titleHost, "statusMessage");
            resets++;
            Assert(completionLogs == 1, "Command polling must publish its completed result before cleanup; actual completion logs=" + completionLogs + ".");
            Assert(outputLines == 0, "Completed command output must not leak into the next title session; actual lines=" + outputLines + ".");
            Assert(completionStatus.Length == 0, "Completed command status must not leak into the next title session; actual status=" + completionStatus + ".");
            Assert(panel.SetActiveCalls == resets, "A completed command while hidden must run one boundary cleanup; expected=" + resets + "; actual=" + panel.SetActiveCalls + ".");

            var manifest = new ManifestModel { Name = "Hidden draft", Author = "DTMAPI", Version = "1.0.0", UniqueID = "DTMAPI.Tests.HiddenDraft" };
            bool enabled = false;
            menu.Register(manifest, () => enabled = false, () => { });
            menu.AddBoolOption(manifest, () => "Enabled", () => "", () => enabled, value => enabled = value);
            IConfigMenuRuntime menuRuntime = menu;
            IConfigMenuPage page = menuRuntime.GetPage(manifest.UniqueID)!;
            menuRuntime.BeginEditing(manifest.UniqueID);
            Assert(page.Items[0].TrySetPendingValue("true", out _), "The test must create a real pending config draft.");
            SetPrivateField(titleHost, "selectedConfigModId", manifest.UniqueID);
            titleHost.Update();
            Assert(!page.HasPendingChanges && !enabled && panel.SetActiveCalls == ++resets,
                "A new selected-page draft while already hidden must be cancelled without applying it.");
            titleHost.Update();
            Assert(panel.SetActiveCalls == resets, "A cancelled draft must return the hidden host to its settled state.");

            titleHost.ResetForSaveBoundary(1, false);
            Assert(panel.SetActiveCalls == ++resets, "An explicit Save boundary must still run cleanup even after the title has settled.");
            titleHost.ResetForTitleBoundary();
            Assert(panel.SetActiveCalls == ++resets, "An explicit Title boundary must still run cleanup even after the title has settled.");
            panel.Destroyed = true;
            titleHost.Update();
            Assert(!GetPrivateField<bool>(titleHost, "initialized"),
                "Hidden steady state must keep detecting native destruction of mounted UI objects.");
            titleHost.Shutdown("hidden boundary test");
            Assert(GetPrivateField<bool>(titleHost, "inputOwnerClosed"), "Shutdown must still close the title input owner.");
        }

        private static ReflectedTitleMenuSettingsUi CreateMountedTitleUi(DtmApiRuntime runtime, ConfigMenuRegistry menu,
            out FakeTitleGameObject panel, out FakeTitleGameObject button)
        {
            var titleHost = new ReflectedTitleMenuSettingsUi(runtime, menu);
            SetPrivateField(titleHost, "inputSettingsRegistered", true);
            SetPrivateField(titleHost, "initialized", true);
            SetPrivateField(titleHost, "objectType", typeof(FakeTitleUnityObject));
            SetPrivateField(titleHost, "gameObjectType", typeof(FakeTitleGameObject));
            SetPrivateField(titleHost, "root", new FakeTitleGameObject());
            SetPrivateField(titleHost, "panelRoot", panel = new FakeTitleGameObject());
            SetPrivateField(titleHost, "titleButtonRoot", button = new FakeTitleGameObject());
            SetPrivateField(titleHost, "panelContentRoot", new FakeTitleGameObject());
            return titleHost;
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
            TitleSettingsBorrowedEventSystemFollowsNativeLifetime();
        }

        private static void TitleSettingsBorrowedEventSystemFollowsNativeLifetime()
        {
            FakeTitleUnityObject.SceneSystems.Clear();
            FakeTitleUnityObject.FindOneCalls = FakeTitleUnityObject.FindAllCalls = 0;
            var menu = new ConfigMenuRegistry();
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), menu);
            var titleHost = CreateMountedTitleUi(runtime, menu, out _, out _);
            SetPrivateField(titleHost, "eventSystemType", typeof(FakeTitleEventSystem));
            MethodInfo ensure = typeof(ReflectedTitleMenuSettingsUi).GetMethod("EnsureEventSystem", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var first = new FakeTitleEventSystem();
            FakeTitleUnityObject.SceneSystems.Add(first);
            ensure.Invoke(titleHost, null);
            ensure.Invoke(titleHost, null);
            Assert(FakeTitleUnityObject.FindOneCalls == 1 && FakeTitleUnityObject.FindAllCalls == 0,
                "An alive active borrowed scene EventSystem must avoid repeated global searches.");
            first.Destroyed = true;
            var second = new FakeTitleEventSystem();
            FakeTitleUnityObject.SceneSystems.Add(second);
            ensure.Invoke(titleHost, null);
            Assert(ReferenceEquals(GetPrivateField<object>(titleHost, "borrowedEventSystem"), second) && FakeTitleUnityObject.FindOneCalls == 2,
                "A Unity-destroyed borrowed component must be replaced by the scene's new EventSystem.");
            second.gameObject.activeInHierarchy = false;
            var third = new FakeTitleEventSystem();
            FakeTitleUnityObject.SceneSystems.Add(third);
            ensure.Invoke(titleHost, null);
            Assert(ReferenceEquals(GetPrivateField<object>(titleHost, "borrowedEventSystem"), third) && FakeTitleUnityObject.FindOneCalls == 3,
                "A deactivated scene EventSystem must stop suppressing a fresh active scene lookup.");
            titleHost.ResetForSaveBoundary(1, false);
            ensure.Invoke(titleHost, null);
            titleHost.ResetForTitleBoundary();
            ensure.Invoke(titleHost, null);
            Assert(!third.Destroyed && !third.gameObject.Destroyed && FakeTitleUnityObject.FindOneCalls == 5,
                "Save and Title boundaries must invalidate the borrowed reference without destroying the native owner.");

            titleHost.ResetForTitleBoundary();
            FakeTitleUnityObject.SceneSystems.Clear();
            var owned = new FakeTitleEventSystem();
            FakeTitleUnityObject.SceneSystems.Add(owned);
            SetPrivateField(titleHost, "eventSystemRoot", owned.gameObject);
            SetPrivateField(titleHost, "eventSystemComponent", owned);
            ensure.Invoke(titleHost, null);
            Assert(!owned.gameObject.Destroyed && FakeTitleUnityObject.FindAllCalls == 1,
                "An owned fallback must remain alive when no native scene EventSystem exists.");
            FakeTitleUnityObject.SceneSystems.Add(third);
            ensure.Invoke(titleHost, null);
            int finds = FakeTitleUnityObject.FindOneCalls + FakeTitleUnityObject.FindAllCalls;
            ensure.Invoke(titleHost, null);
            Assert(owned.gameObject.Destroyed && !third.Destroyed && !third.gameObject.Destroyed &&
                ReferenceEquals(GetPrivateField<object>(titleHost, "borrowedEventSystem"), third) &&
                FakeTitleUnityObject.FindOneCalls + FakeTitleUnityObject.FindAllCalls == finds,
                "The fallback must still yield to a newly available native EventSystem, then reuse it without more global searches.");
            titleHost.Shutdown("borrowed EventSystem test");
            Assert(!third.Destroyed && !third.gameObject.Destroyed, "Shutdown must never destroy a borrowed native EventSystem.");
            FakeTitleUnityObject.SceneSystems.Clear();
        }

        // These doubles model Unity's destroyed-object equality and active-only scene searches.
        private class FakeTitleUnityObject
        {
            internal static readonly List<FakeTitleEventSystem> SceneSystems = new List<FakeTitleEventSystem>();
            internal static int FindOneCalls, FindAllCalls;
            internal bool Destroyed;
            public static FakeTitleUnityObject? FindObjectOfType(Type type)
            {
                FindOneCalls++;
                return SceneSystems.FirstOrDefault(item => type.IsInstanceOfType(item) && !item.Destroyed && !item.gameObject.Destroyed && item.gameObject.activeInHierarchy);
            }
            public static FakeTitleUnityObject[] FindObjectsOfType(Type type)
            {
                FindAllCalls++;
                return SceneSystems.Where(item => type.IsInstanceOfType(item) && !item.Destroyed && !item.gameObject.Destroyed && item.gameObject.activeInHierarchy).ToArray();
            }
            public static void Destroy(FakeTitleUnityObject value) => value.Destroyed = true;
            public static bool operator ==(FakeTitleUnityObject? left, FakeTitleUnityObject? right)
            {
                bool leftNull = left is null || left.Destroyed;
                bool rightNull = right is null || right.Destroyed;
                return leftNull || rightNull ? leftNull && rightNull : ReferenceEquals(left, right);
            }
            public static bool operator !=(FakeTitleUnityObject? left, FakeTitleUnityObject? right) => !(left == right);
            public override bool Equals(object? value) => ReferenceEquals(this, value);
            public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
        }
        private sealed class FakeTitleGameObject : FakeTitleUnityObject
        {
            public bool activeInHierarchy { get; set; } = true;
            internal int SetActiveCalls;
            public void SetActive(bool active) { SetActiveCalls++; activeInHierarchy = active; }
        }
        private sealed class FakeTitleEventSystem : FakeTitleUnityObject
        {
            public FakeTitleGameObject gameObject { get; } = new FakeTitleGameObject();
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
                "DTMAPI.GameBridge.DolocTown.CropHarvestingService"
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
                Assert(File.ReadAllText(snapshotPath).Contains(
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
                Assert(File.ReadAllText(snapshotPath).Contains(
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
                bridge.Update();
                Assert(File.ReadAllText(snapshotPath).Contains(committedId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal),
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
                Assert(File.ReadAllText(snapshotPath).Contains(
                        committedId.ToString(CultureInfo.InvariantCulture),
                        StringComparison.Ordinal),
                    "A successful close must defer publication until the next GameBridge frame.");

                bridge.Update();
                string committedText = File.ReadAllText(snapshotPath);
                Assert(committedText.Contains(
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
                using JsonDocument persistedSnapshot = JsonDocument.Parse(failingPersisted);
                JsonElement persistedSubscription = persistedSnapshot.RootElement.GetProperty("subscriptions").EnumerateArray()
                    .Single(row => row.GetProperty("workshopId").GetString() == failingWorkshopId.ToString(CultureInfo.InvariantCulture));
                Assert(persistedSnapshot.RootElement.GetProperty("available").GetBoolean() &&
                    !persistedSubscription.GetProperty("nativeInfoAvailable").GetBoolean() &&
                    persistedSubscription.GetProperty("installPath").GetString() == failingWorkshopRoot &&
                    persistedSnapshot.RootElement.GetProperty("failure").GetString()!.Contains("GetAllValidModInfos enrichment failed", StringComparison.Ordinal),
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
                Assert(!runtime.DiscoveredMods.Any(mod => mod.Manifest.UniqueID == uniqueId), "A subscribed ID without a native installed root must not authorize a stale numeric Workshop directory in Workshop Validation mode.");
                Assert(runtime.Diagnostics.GetWarnings().Any(warning =>
                    warning.Details.Contains("原生订阅目录不可用", StringComparison.Ordinal) ||
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

        private static void WorkshopValidationUsesNativeSourceSelection()
        {
            string gameDir = NewTempGameDir();
            string persistentRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-persistent", Guid.NewGuid().ToString("N"));
            string authorStateRoot = Path.Combine(Path.GetTempPath(), "DTMAPI-tests-author-state", Guid.NewGuid().ToString("N"));
            const string uniqueId = "DTMAPI.Tests.WorkshopValidation";
            const ulong workshopId = 3749000022UL;
            string workshopRoot = Path.Combine(gameDir, "steamapps", "workshop", "content", "2285550", workshopId.ToString(CultureInfo.InvariantCulture));
            WriteSourceAuthorityContentPack(workshopRoot, uniqueId, "Workshop source");
            const string localShadowFolder = "ValidationLocalShadow";
            string localShadowPackage = Path.Combine(persistentRoot, "MODS", localShadowFolder);
            string localShadowRoot = Path.Combine(localShadowPackage, "Content", "DTMAPI");
            WriteSourceAuthorityContentPack(localShadowRoot, uniqueId, "Local duplicate");
            File.WriteAllText(Path.Combine(localShadowPackage, "info.json"), "{ \"name\": \"Local duplicate\" }");

            string? previousPersistentRoot = Environment.GetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT");
            string? previousAuthorRoot = Environment.GetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT");
            try
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", persistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", authorStateRoot);
                WriteOfficialModInfos(
                    persistentRoot,
                    new OfficialModInfoTestEntry("Workshop." + workshopId.ToString(CultureInfo.InvariantCulture), true, "Workshop"),
                    new OfficialModInfoTestEntry("Local." + localShadowFolder, true, "Local"));
                WriteAuthorSourceSelectionState(
                    gameDir,
                    uniqueId,
                    "WorkshopValidation",
                    string.Empty,
                    playerReproductionActive: false,
                    expectedTreeSha256: string.Empty);

                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                string officialId = "Workshop." + workshopId.ToString(CultureInfo.InvariantCulture);
                var enabledModManager = new FakeNativeModManager(
                    new FakePublishedFileId(workshopId),
                    workshopRoot,
                    new FakeNativeModInfo(officialId, enabled: true, priority: -1, workshopId, workshopRoot));
                Assert(bridge.CaptureNativeWorkshopSubscriptions("unit-test-validation-startup", enabledModManager, requireSteamInitialized: false), "Workshop Validation should use the official subscription and installed-root snapshot.");
                runtime.Start();
                DiscoveredMod selected = runtime.DiscoveredMods.Single(mod => mod.Manifest.UniqueID == uniqueId);
                Assert(selected.Source == "Workshop" && selected.NativeSubscriptionVerified && selected.OfficialEnabled && PathsEqualForTest(selected.RootPath, workshopRoot), "Retained Workshop Validation state should select the enabled native-installed source during ordinary startup.");
                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == uniqueId), "The selected native Workshop content package should load.");

                var disabledModManager = new FakeNativeModManager(
                    new FakePublishedFileId(workshopId),
                    workshopRoot,
                    new FakeNativeModInfo(officialId, enabled: false, priority: -1, workshopId, workshopRoot));
                Assert(bridge.CaptureNativeWorkshopSubscriptions("unit-test-validation-disabled", disabledModManager, requireSteamInitialized: false), "The updated official state should capture native disablement.");
                runtime.NotifyWorkshopModListChanged();
                Assert(!runtime.DiscoveredMods.Any(mod => mod.Manifest.UniqueID == uniqueId), "Workshop Validation must reject the disabled Workshop source instead of selecting the Local duplicate.");
                Assert(runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == uniqueId) && runtime.ModRegistry.IsLoaded(uniqueId), "Native disablement changes the next startup and preserves the admitted content package.");
                Assert(runtime.CreateDiagnosticsSnapshot().Mods.Single(mod => mod.UniqueID == uniqueId).StatusCode == "restart-required", "A resident package removed from the next selection must remain visible with a restart requirement.");
                runtime.NotifyRuntimeShutdown("unit-workshop-validation");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DTMAPI_DOLOC_PERSISTENT_ROOT", previousPersistentRoot);
                Environment.SetEnvironmentVariable("DTMAPI_AUTHOR_STATE_ROOT", previousAuthorRoot);
            }
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

        private static void ItemDisplayNameApiReadsCurrentNativeTitles()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                var service = new ItemDisplayNameService(runtime);
                DolocAPI.ItemTitles = new Dictionary<string, NativeItemTitle>(StringComparer.Ordinal)
                {
                    ["fish"] = new NativeItemTitle { Title = "鱼" }
                };
                Assert(service.TryGetDisplayName("fish", out string first) && first == "鱼",
                    "The API reads the current native item title.");
                DolocAPI.ItemTitles["fish"].Title = "Fish";
                Assert(service.TryGetDisplayName("fish", out string translated) && translated == "Fish",
                    "Language or table changes are visible on the next query without a lifecycle notification.");
                Assert(!service.TryGetDisplayName("FISH", out _) && !service.TryGetDisplayName(" fish ", out _),
                    "Item ids retain the native exact-key semantics.");
                Assert(!service.TryGetDisplayName("missing", out string missing) && missing.Length == 0,
                    "An unknown item allows the caller to use its fallback.");
                DolocAPI.ItemTitles["missing"] = new NativeItemTitle { Title = "New item" };
                Assert(service.TryGetDisplayName("missing", out string added) && added == "New item",
                    "A newly available native item is read after an earlier failed lookup.");
                DolocAPI.ItemTitles = null;
                Assert(!service.TryGetDisplayName("fish", out string unavailable) && unavailable.Length == 0,
                    "Unavailable native tables allow the caller to use its fallback.");
                DolocAPI.ItemTitles = new Dictionary<string, NativeItemTitle>(StringComparer.Ordinal)
                {
                    ["fish"] = new NativeItemTitle { Title = "Fresh native table" }
                };
                Assert(service.TryGetDisplayName("fish", out string refreshed) && refreshed == "Fresh native table",
                    "A rebuilt native table is queried without retaining the previous title.");

                Exception? offThreadFailure = null;
                var thread = new Thread(() =>
                {
                    try { service.TryGetDisplayName("fish", out _); }
                    catch (Exception ex) { offThreadFailure = ex; }
                });
                thread.Start();
                Assert(thread.Join(TimeSpan.FromSeconds(5)) && offThreadFailure is InvalidOperationException,
                    "Native title queries still require the Runtime/main thread.");
            }
            finally
            {
                DolocAPI.ItemTitles = null;
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void GameBridgeFeatureFailureThrottleRecordsOneDiagnosticsErrorButKeepsFailureCount()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
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

                Assert(runtime.Diagnostics.GetErrors().Count(e => e.Owner == "DTMAPI.GameBridge.Feature.UnitFeature") == 1, "Repeated feature-host failures for the same operation should record only the first diagnostics error.");

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

        private static void HarmonyTargetSignatureSelectsExactOverload()
        {
            string dir = NewTempGameDir();
            var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
            var patcher = new HarmonyPatcher(runtime);
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

public sealed class NativeItemTitle
{
    public string Title { get; set; } = string.Empty;
}

public static partial class DolocAPI
{
    public static Dictionary<string, NativeItemTitle>? ItemTitles;
    public static bool QueryItemProto(string id, out NativeItemTitle? item)
    {
        item = null;
        return ItemTitles != null && ItemTitles.TryGetValue(id, out item);
    }
}

