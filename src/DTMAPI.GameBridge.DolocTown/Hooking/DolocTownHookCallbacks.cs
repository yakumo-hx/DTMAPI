using System;
using System.Collections.Generic;
using System.Diagnostics;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public static class DolocTownHookCallbacks
    {
        private const int HookCallbackShortLogLimit = 3;
        private static readonly TimeSpan HookCallbackSummaryInterval = TimeSpan.FromSeconds(30);
        private static readonly object hookCallbackFailureGate = new object();
        private static readonly object nativeContinuationProbeGate = new object();
        private static readonly Dictionary<string, HookCallbackFailureState> hookCallbackFailures = new Dictionary<string, HookCallbackFailureState>(StringComparer.Ordinal);
        private static DtmApiRuntime? runtime;
        private static Stopwatch? nativeContinuationProbeStopwatch;

        public static DtmApiRuntime? Runtime
        {
            get => runtime;
            set
            {
                runtime = value;
                ResetHookCallbackFailureThrottle();
            }
        }

        public static DolocTownGameBridge? Bridge { get; set; }
        public static void LoadGamePrefix(int index)
        {
            SafeCallback("Platform.ResetLoadObservation", () => Bridge?.ResetPlatformWorldObservation());
            StartNativeContinuationProbeStopwatch();
            SafeCallback("LoadGame.NotifyLoadGameRequested", () => Runtime?.NotifyLoadGameRequested(index));
            RecordNativeContinuationProbe("DolocAPI.LoadGame", "Enter");
        }

        public static void LoadGamePostfix(int index, bool __result)
        {
            RecordNativeContinuationProbe("DolocAPI.LoadGame", "Exit");
            SafeCallback("LoadGame.NotifyLoadGameReturned", () => Runtime?.NotifyLoadGameReturned(index, __result));
        }

        public static void NormalGameFramePostfix()
        {
            try
            {
                SafeCallback("Lifecycle.ObserveNativeFrame", () => Bridge?.ObserveFirstNativeWorldFrame());
                SafeCallback("Platform.ObserveNativeFrame", () => Bridge?.ObservePlatformNativeFrame());
                Runtime?.NotifyNativeGameFrame();
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("NormalGameState.OnUpdate.NativeFrameDrain", ex);
            }
        }

        public static void NativeContinuationAfterLoadArchiveDataPrefix()
        {
            RecordNativeContinuationProbe("DolocAPI.AfterLoadArchiveData", "Enter");
        }

        public static void NativeContinuationAfterLoadArchiveDataPostfix()
        {
            RecordNativeContinuationProbe("DolocAPI.AfterLoadArchiveData", "Exit");
        }

        public static void NativeContinuationVersionPatcherLoadAllPrefix()
        {
            RecordNativeContinuationProbe("DolocTown.VersionPatcher.LoadAllVersionPatches", "Enter");
        }

        public static void NativeContinuationVersionPatcherLoadAllPostfix()
        {
            RecordNativeContinuationProbe("DolocTown.VersionPatcher.LoadAllVersionPatches", "Exit");
        }

        public static void NativeContinuationVersionPatcherLoadBeyondPrefix()
        {
            RecordNativeContinuationProbe("DolocTown.VersionPatcher.LoadAllVersionPatchesBeyond", "Enter");
        }

        public static void NativeContinuationVersionPatcherLoadBeyondPostfix()
        {
            RecordNativeContinuationProbe("DolocTown.VersionPatcher.LoadAllVersionPatchesBeyond", "Exit");
        }

        public static void NativeContinuationMapManagerInitPrefix()
        {
            RecordNativeContinuationProbe("DolocTown.MapManager.Init", "Enter");
        }

        public static void NativeContinuationMapManagerInitPostfix()
        {
            RecordNativeContinuationProbe("DolocTown.MapManager.Init", "Exit");
        }

        public static void AfterLoadArchiveDataPostfix(bool isNewGame)
        {
            SafeCallback("Lifecycle.ObserveSaveLoaded", () => Bridge?.ObserveSaveLoadedNativeState());
            Stopwatch breadcrumb = Stopwatch.StartNew();
            Runtime?.RecordSaveLoadedActivationBreadcrumb("Hook.Enter", breadcrumb);
            Runtime?.RecordSaveLoadedActivationBreadcrumb("Hook.BeforeGameBridgeFeatureDispatch", breadcrumb);
            SafeCallback("SaveLoaded.NotifyGameBridgeFeatures", () => Bridge?.NotifyGameBridgeFeaturesSaveLoaded(isNewGame));
            Runtime?.RecordSaveLoadedActivationBreadcrumb("Hook.AfterGameBridgeFeatureDispatch", breadcrumb);
            Runtime?.RecordSaveLoadedActivationBreadcrumb("Hook.BeforeRuntimeNotifySaveLoaded", breadcrumb);
            SafeCallback("SaveLoaded.NotifyRuntime", () => Runtime?.NotifySaveLoaded(isNewGame));
            Runtime?.RecordSaveLoadedActivationBreadcrumb("Hook.AfterRuntimeNotifySaveLoaded", breadcrumb);
            Runtime?.RecordSaveLoadedActivationBreadcrumb("Hook.Exit", breadcrumb);
        }

        public static bool SaveGamePrefix(
            int index,
            ref bool __result)
        {
            bool equipmentReady =
                TryLifecycleCallback(
                    "SaveGame.NotifyEquipmentSlotsSaveSaving",
                    () => Bridge?
                        .NotifyEquipmentSlotsSaveSaving(index));
            bool runtimeReady =
                Runtime?.TryNotifySaveSaving(index) ?? true;
            if (equipmentReady && runtimeReady)
                return true;

            __result = false;
            RecordLifecycleCallbackFailure(
                "SaveGame.SaveSavingFailClosed",
                new InvalidOperationException(
                    "A SaveSaving participant failed before the native save boundary; SaveGame was canceled."));
            return false;
        }

        public static void SaveGamePostfix(int index, bool __result)
        {
            RunSaveSavedIfNativeSucceeded(
                __result,
                () => (Runtime ?? throw new InvalidOperationException("SaveSaved Runtime callback is unavailable.")).NotifySaveSaved(index),
                () => (Bridge ?? throw new InvalidOperationException("SaveSaved GameBridge callback is unavailable.")).NotifyEquipmentSlotsSaveSaved(index),
                () => Bridge?.QaHostSaveSavedNotification?.Invoke(index));
        }

        public static void ReturnHomePrefix()
        {
            SafeCallback("Platform.ResetTitleObservation", () => Bridge?.ResetPlatformWorldObservation());
            SafeCallback("ReturnHome.NotifyRequested", () => Runtime?.NotifyReturnHomeRequested("Harmony Prefix: DolocAPI.ReturnHome"));
        }

        public static void ReturnHomePostfix()
        {
            SafeCallback("ReturnHome.NotifyNativePostfix", () => Runtime?.NotifyReturnHomeNativePostfix("Harmony Postfix: DolocAPI.ReturnHome"));
            SafeCallback("ReturnedToTitle.NotifyGameBridgeFeatures", () => Bridge?.NotifyGameBridgeFeaturesReturnedToTitle());
            SafeCallback("ReturnedToTitle.NotifyRuntime", () => Runtime?.NotifyReturnedToTitle());
        }

        public static void PlatformNewGamePrefix(int index)
        {
            SafeCallback("Platform.NewGame.Start", () => { Bridge?.ResetPlatformWorldObservation(); Runtime?.NotifyPlatformNewGameStarting(index); });
        }
        public static void PlatformNewGamePostfix() => SafeCallback("Platform.NewGame.End", () => Runtime?.NotifyPlatformNewGameCompleted());
        // Void finalizer observes failure without swallowing or replacing the native exception.
        public static void PlatformLoadFinalizer(Exception? __exception)
        {
            if (__exception != null) SafeCallback("Platform.Load.Exception", () => { Bridge?.ResetPlatformWorldObservation(); Runtime?.NotifyPlatformLoadFailure(); });
        }
        public static void PlatformWorldTransitionPrefix() => SafeCallback("Platform.World.Start", () => Bridge?.ObservePlatformWorldTransition());
        public static void PlatformRoomEnteredPostfix(object __instance) => SafeCallback("Platform.Room.Entered", () => Bridge?.ObservePlatformRoomEntered(__instance));

        public static void DolocApiSetEnvCameraPostfix()
        {
            DolocTownGameBridge? bridge = Bridge;
            if (bridge?.HasEnvironmentResetDemand != true)
                return;
            try
            {
                bridge.NotifyGameBridgeFeaturesEnvironmentReset("DolocAPI.SetEnvCamera");
            }
            catch (Exception ex)
            {
                RecordLifecycleCallbackFailure("EnvironmentReset.NotifyFeatures", ex);
            }
        }

        public static void HomePageRenderTextMenuPostfix(object __instance)
        {
            SafePostfix("UI.NativeLayoutRepair.HomePageRenderTextMenu", () =>
            {
                Bridge?.NativeUiLayoutRepairService?.RepairHomePageTextMenu(GameBridgeNativeHelpers.ReadMember(__instance, "textMenu"), "HomePageUiState.RenderTextMenu");
                Bridge?.QaHostUiObservationNotification?.Invoke("HomePageUiState.RenderTextMenu");
            });
        }

        public static void MenuUiSetCapacityPostfix(object __instance, int __0)
        {
            SafePostfix("UI.NativeLayoutRepair.MenuUiSetCapacity", () =>
            {
                Bridge?.NativeUiLayoutRepairService?.RepairMainMenu(__instance, "MenuUI.SetCapacity");
                Bridge?.QaHostUiObservationNotification?.Invoke("MenuUI.SetCapacity");
            });
        }

        public static void MainMenuPanelOnStartShowPostfix(object __instance)
        {
            SafePostfix("UI.NativeLayoutRepair.MainMenuPanelOnStartShow", () =>
            {
                Bridge?.NativeUiLayoutRepairService?.UpdateActiveMenuLayout();
                Bridge?.QaHostUiObservationNotification?.Invoke("MainMenuPanel.OnStartShow");
            });
        }

        public static void GameDataPanelSetCapacityPostfix(object __instance, int __0)
        {
            SafePostfix("UI.NativeLayoutRepair.GameDataPanelSetCapacity", () => Bridge?.QaHostUiObservationNotification?.Invoke("GameDataPanel.SetCapacity"));
        }

        public static void ReloadModsPostfix(object __instance)
        {
            SafePostfix(
                "Workshop.ModManager.ReloadMods",
                () => (Bridge ?? throw new InvalidOperationException(
                        "Workshop GameBridge callback is unavailable."))
                    .HandleNativeModManagerReloaded(__instance));
        }

        public static void ModUiStateRegisterPrefix(object __instance)
        {
            SafePostfix(
                "Workshop.ModUiState.Register",
                () => (Bridge ?? throw new InvalidOperationException(
                        "Workshop GameBridge callback is unavailable."))
                    .BeginOfficialModUiTransaction(__instance));
        }

        public static void ModUiStateHidePrefix(object __instance)
        {
            SafePostfix(
                "Workshop.ModUiState.Hide",
                () => (Bridge ?? throw new InvalidOperationException(
                        "Workshop GameBridge callback is unavailable."))
                    .BeginOfficialModUiClose(__instance));
        }

        public static void SaveModManagerPostfix(
            object __0,
            bool __result)
        {
            SafePostfix(
                "Workshop.DataPersistenceManager.SaveModManager",
                () => (Bridge ?? throw new InvalidOperationException(
                        "Workshop GameBridge callback is unavailable."))
                    .ObserveOfficialModManagerSave(__0, __result));
        }

        public static void ModUiStateCloseTransactionPostfix(
            object __instance)
        {
            SafePostfix(
                "Workshop.ModUiState.Hide.CloseTransaction",
                () => (Bridge ?? throw new InvalidOperationException(
                        "Workshop GameBridge callback is unavailable."))
                    .CompleteOfficialModUiClose(__instance));
        }

        public static void ModDataConstructorPostfix(object __instance, object __0)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.WorkshopAuthoring))
                return;
            try
            {
                Bridge?.TryMarkDtmapiLocalUploadData(__instance, __0);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Workshop.LocalUploadPlan.Display", ex);
            }
        }

        public static bool SteamWorkshopUploaderResolveUploadPlanPrefix(object __instance, object __0, object __1)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.WorkshopAuthoring))
                return true;
            try
            {
                return Bridge?.TryResolveDtmapiUploadPlanIfNativeBusy(__instance, __0, __1) ?? true;
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Workshop.LocalUploadPlanBusyFallback.ResolveUploadPlanPrefix", ex);
                return true;
            }
        }

        public static void SteamWorkshopUploaderResolveUploadPlanPostfix(object __instance, object __0, object __1)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.WorkshopAuthoring))
                return;
            try
            {
                Bridge?.TrackDtmapiUploadPlanFallbackAfterNativeQuery(__instance, __0, __1);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Workshop.LocalUploadPlanKnownIdFallback.TrackResolveUploadPlan", ex);
            }
        }

        private static void StartNativeContinuationProbeStopwatch()
        {
            lock (nativeContinuationProbeGate)
                nativeContinuationProbeStopwatch = Stopwatch.StartNew();
        }

        private static long GetNativeContinuationProbeElapsedMs()
        {
            lock (nativeContinuationProbeGate)
                return nativeContinuationProbeStopwatch?.ElapsedMilliseconds ?? 0;
        }

        private static void RecordNativeContinuationProbe(string method, string phase, Exception? exception = null)
        {
            try
            {
                Runtime?.RecordNativeLoadContinuationBreadcrumb(method, phase, GetNativeContinuationProbeElapsedMs(), exception?.GetType().Name);
            }
            catch
            {
                // Probe breadcrumbs must not affect native save-load behavior.
            }
        }

        private static bool HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand demand) =>
            Bridge?.HasRetainedCallbackDemand(demand) == true;

        private static void SafeCallback(string operation, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                RecordLifecycleCallbackFailure(operation, ex);
            }
        }

        private static bool TryLifecycleCallback(string operation, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                RecordLifecycleCallbackFailure(operation, ex);
                return false;
            }
        }

        private static bool TryLifecycleReceipt(string operation, Func<bool> action)
        {
            try
            {
                // A false receipt is an expected degraded result whose production
                // owner has already published its warning/status. It gates only the
                // optional QA completion and must not be promoted to a second Error.
                return action();
            }
            catch (Exception ex)
            {
                RecordLifecycleCallbackFailure(operation, ex);
                return false;
            }
        }

        private static void RunSaveSavedQaLifecycle(
            Action runtimeNotification,
            Action productionPersistence,
            Action qaNotification)
        {
            bool runtimeSucceeded = TryLifecycleCallback("SaveGame.NotifySaveSaved", runtimeNotification);
            bool productionSucceeded = TryLifecycleCallback("SaveGame.NotifyEquipmentSlotsSaved", productionPersistence);
            if (runtimeSucceeded && productionSucceeded)
                SafeCallback("SaveGame.NotifyQaHostSaved", qaNotification);
        }

        private static void RunSaveSavedIfNativeSucceeded(
            bool nativeSaveSucceeded,
            Action runtimeNotification,
            Action productionPersistence,
            Action qaNotification)
        {
            if (!nativeSaveSucceeded)
                return;
            RunSaveSavedQaLifecycle(runtimeNotification, productionPersistence, qaNotification);
        }

        private static void RunWorkshopQaLifecycle(Func<bool> captureReceipt, Action runtimeNotification, Action qaNotification)
        {
            bool captureSucceeded = TryLifecycleReceipt("Workshop.CaptureNativeSubscriptions", captureReceipt);
            bool runtimeSucceeded = TryLifecycleCallback("Workshop.NotifyModListChanged", runtimeNotification);
            if (captureSucceeded && runtimeSucceeded)
                SafeCallback("Workshop.NotifyQaHostReloadCompleted", qaNotification);
        }

        internal static void RunSaveSavedQaLifecycleForTests(Action runtimeNotification, Action productionPersistence, Action qaNotification)
        {
            RunSaveSavedQaLifecycle(
                runtimeNotification ?? throw new ArgumentNullException(nameof(runtimeNotification)),
                productionPersistence ?? throw new ArgumentNullException(nameof(productionPersistence)),
                qaNotification ?? throw new ArgumentNullException(nameof(qaNotification)));
        }

        internal static void RunSaveSavedIfNativeSucceededForTests(
            bool nativeSaveSucceeded,
            Action runtimeNotification,
            Action productionPersistence,
            Action qaNotification)
        {
            RunSaveSavedIfNativeSucceeded(
                nativeSaveSucceeded,
                runtimeNotification ?? throw new ArgumentNullException(nameof(runtimeNotification)),
                productionPersistence ?? throw new ArgumentNullException(nameof(productionPersistence)),
                qaNotification ?? throw new ArgumentNullException(nameof(qaNotification)));
        }

        internal static void RunWorkshopQaLifecycleForTests(Func<bool> captureReceipt, Action runtimeNotification, Action qaNotification)
        {
            RunWorkshopQaLifecycle(
                captureReceipt ?? throw new ArgumentNullException(nameof(captureReceipt)),
                runtimeNotification ?? throw new ArgumentNullException(nameof(runtimeNotification)),
                qaNotification ?? throw new ArgumentNullException(nameof(qaNotification)));
        }

        private static void SafePostfix(string operation, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure(operation, ex);
            }
        }

        private static T SafeResult<T>(string operation, T fallback, Func<T> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure(operation, ex);
                return fallback;
            }
        }

        private static bool SafePrefix(string operation, Func<bool> action, bool fallback = true)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure(operation, ex);
                return fallback;
            }
        }

        private static void RecordLifecycleCallbackFailure(string operation, Exception ex)
        {
            try
            {
                Runtime?.Diagnostics.RecordError("DTMAPI.GameBridge.Lifecycle", "Lifecycle callback failed: " + operation + ".", ex.ToString());
                Runtime?.RuntimeMonitor.Log("Lifecycle callback failed operation=" + operation + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Error);
            }
            catch
            {
                // Harmony callbacks must never rethrow diagnostics failures into native gameplay.
            }
        }

        private static void RecordHookCallbackFailure(string operation, Exception ex)
        {
            try
            {
                HookCallbackFailurePublication publication = RecordHookCallbackFailureState(operation, ex);
                if (publication.RecordDiagnosticsError)
                    Runtime?.Diagnostics.RecordError("DTMAPI.GameBridge.HookCallback", "Hook callback failed: " + operation + ".", ex.ToString());

                if (publication.LogMode == HookCallbackFailureLogMode.Full)
                    Runtime?.RuntimeMonitor.Log("Hook callback failed operation=" + operation + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Error);
                else if (publication.LogMode == HookCallbackFailureLogMode.Short)
                    Runtime?.RuntimeMonitor.Log("Repeated hook callback failure operation=" + operation + " count=" + publication.Count + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
                else if (publication.LogMode == HookCallbackFailureLogMode.Summary)
                    Runtime?.RuntimeMonitor.Log("Throttled hook callback failures operation=" + operation + " count=" + publication.Count + " lastError=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            }
            catch
            {
                // Harmony callbacks must never rethrow diagnostics failures into native gameplay.
            }
        }

        private static HookCallbackFailurePublication RecordHookCallbackFailureState(string operation, Exception ex)
        {
            string key = string.IsNullOrWhiteSpace(operation) ? "<unknown>" : operation;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            lock (hookCallbackFailureGate)
            {
                if (!hookCallbackFailures.TryGetValue(key, out HookCallbackFailureState? state))
                {
                    state = new HookCallbackFailureState();
                    hookCallbackFailures[key] = state;
                }

                state.Count++;
                state.LastError = ex.GetType().Name + ": " + ex.Message;
                state.LastSeenAtUtc = now;

                if (state.Count == 1)
                {
                    state.LastPublishedAtUtc = now;
                    return new HookCallbackFailurePublication(true, HookCallbackFailureLogMode.Full, state.Count);
                }

                if (state.Count <= HookCallbackShortLogLimit)
                {
                    state.LastPublishedAtUtc = now;
                    return new HookCallbackFailurePublication(false, HookCallbackFailureLogMode.Short, state.Count);
                }

                if (now - state.LastPublishedAtUtc >= HookCallbackSummaryInterval)
                {
                    state.LastPublishedAtUtc = now;
                    return new HookCallbackFailurePublication(false, HookCallbackFailureLogMode.Summary, state.Count);
                }

                return new HookCallbackFailurePublication(false, HookCallbackFailureLogMode.None, state.Count);
            }
        }

        private static void ResetHookCallbackFailureThrottle()
        {
            lock (hookCallbackFailureGate)
                hookCallbackFailures.Clear();
        }

        private sealed class HookCallbackFailureState
        {
            public int Count { get; set; }
            public string LastError { get; set; } = string.Empty;
            public DateTimeOffset LastSeenAtUtc { get; set; }
            public DateTimeOffset LastPublishedAtUtc { get; set; }
        }

        private readonly struct HookCallbackFailurePublication
        {
            public HookCallbackFailurePublication(bool recordDiagnosticsError, HookCallbackFailureLogMode logMode, int count)
            {
                RecordDiagnosticsError = recordDiagnosticsError;
                LogMode = logMode;
                Count = count;
            }

            public bool RecordDiagnosticsError { get; }

            public HookCallbackFailureLogMode LogMode { get; }

            public int Count { get; }
        }

        private enum HookCallbackFailureLogMode
        {
            None,
            Full,
            Short,
            Summary
        }
    }
}
