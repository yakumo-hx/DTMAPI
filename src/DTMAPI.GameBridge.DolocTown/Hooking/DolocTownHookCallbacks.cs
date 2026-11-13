using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public static class DolocTownHookCallbacks
    {
        private const int HookCallbackShortLogLimit = 3;
        private static readonly TimeSpan HookCallbackSummaryInterval = TimeSpan.FromSeconds(30);
        private static readonly Dictionary<string, HookCallbackFailureState> hookCallbackFailures = new Dictionary<string, HookCallbackFailureState>(StringComparer.Ordinal);
        private static DtmApiRuntime? runtime;

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
            SafeCallback("LoadGame.NotifyLoadGameRequested", () => Runtime?.NotifyLoadGameRequested(index));
        }

        public static void LoadGamePostfix(int index, bool __result)
        {
            SafeCallback("LoadGame.NotifyLoadGameReturned", () => Runtime?.NotifyLoadGameReturned(index, __result));
        }

        public static void NormalGameFramePostfix()
        {
            try
            {
                SafeCallback("Platform.ObserveNativeFrame", () => Bridge?.ObservePlatformNativeFrame());
                Runtime?.NotifyNativeGameFrame();
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("NormalGameState.OnUpdate.NativeFrameDrain", ex);
            }
        }

        public static void AfterLoadArchiveDataPostfix(bool isNewGame)
        {
            SafeCallback("SaveLoaded.NotifyGameBridgeFeatures", () => Bridge?.NotifyGameBridgeFeaturesSaveLoaded(isNewGame));
            SafeCallback("SaveLoaded.NotifyRuntime", () => Runtime?.NotifySaveLoaded(isNewGame));
        }

        public static bool SaveGamePrefix(
            int index,
            ref bool __result)
        {
            bool runtimeReady =
                Runtime?.TryNotifySaveSaving(index) ?? true;
            if (runtimeReady)
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
            if (!__result)
                return;
            TryLifecycleCallback(
                "SaveGame.NotifySaveSaved",
                () => (Runtime ?? throw new InvalidOperationException("SaveSaved Runtime callback is unavailable.")).NotifySaveSaved(index));

        }

        public static void ReturnHomePrefix()
        {
            SafeCallback("Platform.ResetTitleObservation", () => Bridge?.ResetPlatformWorldObservation());
            SafeCallback("ReturnHome.NotifyRequested", () => Runtime?.NotifyReturnHomeRequested("Harmony Prefix: DolocAPI.ReturnHome"));
        }

        public static void ReturnHomePostfix()
        {
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

        private static void ResetHookCallbackFailureThrottle()
        {
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

