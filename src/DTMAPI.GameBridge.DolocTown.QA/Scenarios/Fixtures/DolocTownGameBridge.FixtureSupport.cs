using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private bool TryAutoLoadSave(int humanSlot)
        {
            int gameIndex = Math.Max(0, humanSlot - 1);
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                if (TryAutoLoadSaveViaOfficialUi(patcher, humanSlot, gameIndex, out bool waitForOfficialUi))
                    return true;
                if (waitForOfficialUi)
                    return false;

                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? loadGame = FindMethod(dolocApi, "LoadGame", 1);
                if (loadGame == null)
                    throw new MissingMethodException("DolocAPI.LoadGame(int) was not found.");
                InvokeSmokeLoadGame(
                    loadGame,
                    gameIndex,
                    "SmokeHarness.TryAutoLoadSave",
                    "Smoke automation loading save slot " + humanSlot + " using direct DolocAPI.LoadGame fallback, game index " + gameIndex + ".",
                    "Requested direct save-load fallback for slot " + humanSlot + ".");
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-load failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AutoLoadSave", "failed", "DolocAPI.LoadGame", ex.GetType().Name + ": " + ex.Message);
                runtime.RuntimeMonitor.Log("Smoke auto-load target diagnostic:" + Environment.NewLine + (patcher?.BuildTypeResolutionReport(
                    "DolocAPI, Assembly-CSharp",
                    "DolocTown.GameData.DataPersistenceManager, Assembly-CSharp") ?? "Hook patcher was unavailable."));
                return true;
            }
        }

        private static object? GetCurrentUiStateForFixture(Type dolocApi)
        {
            object? userInput = ReadStaticMember(dolocApi, "userInput");
            return userInput == null ? null : ReadMember(userInput, "CurrentState");
        }

        private FixtureAttemptResult TryExerciseSaveLoadCycleForFixture(double smokeSeconds)
        {
            try
            {
                if (g6FixtureState == null)
                    return FixtureAttemptResult.Pending;

                DateTimeOffset now = DateTimeOffset.Now;
                int requestedCycles = GetRequestedSaveLoadCycleCount();
                int initialIdleSeconds = Math.Max(1, g6FixtureState.SaveLoadCycleInitialTitleIdleSeconds > 0 ? g6FixtureState.SaveLoadCycleInitialTitleIdleSeconds : g6FixtureState.AutoLoadDelaySeconds);
                int intervalSeconds = Math.Max(1, g6FixtureState.SaveLoadCycleIntervalSeconds);
                int inSaveSeconds = Math.Max(1, g6FixtureState.SaveLoadCycleInSaveSeconds);

                if (saveLoadCycleStartedAt == default)
                {
                    saveLoadCycleStartedAt = now;
                    runtime.SetHookStatus("Smoke.SaveLoadCycle", "pending", "DolocAPI.LoadGame/ReturnHome", "Waiting to start save/load cycle smoke. cycles=" + requestedCycles.ToString(CultureInfo.InvariantCulture) + ", initialIdleSeconds=" + initialIdleSeconds.ToString(CultureInfo.InvariantCulture) + ", intervalSeconds=" + intervalSeconds.ToString(CultureInfo.InvariantCulture) + ", inSaveSeconds=" + inSaveSeconds.ToString(CultureInfo.InvariantCulture) + ".");
                }

                if (saveLoadCycleStage == 0)
                {
                    if (smokeSeconds < initialIdleSeconds)
                    {
                        LogSaveLoadCyclePending("Waiting initial title idle before first save load. elapsedSeconds=" + smokeSeconds.ToString("0", CultureInfo.InvariantCulture) + "/" + initialIdleSeconds.ToString(CultureInfo.InvariantCulture) + ".");
                        return FixtureAttemptResult.Pending;
                    }
                    if (!IsSaveLoadCycleHomePageStable(now, 2, "first save load"))
                        return FixtureAttemptResult.Pending;

                    runtime.NotifyTitleStable("SmokeHarness.SaveLoadCycle", "HomePageUiState stable before first save load.");
                    RunPreLoadForcedGcProbeIfNeeded(now);
                    RequestSaveLoadCycleLoad(now);
                    return FixtureAttemptResult.Pending;
                }

                if (saveLoadCycleStage == 1)
                {
                    if (autoLoadOfficialPathRequested && !modChangePromptConfirmed)
                        TryConfirmModChangePrompt();
                    if (autoLoadOfficialPathRequested && modChangePromptConfirmed && saveLoadCycleSaveLoadedAt == default)
                        TryAutoLoadSaveDirectFallbackAfterModChange();
                    if (saveLoadCycleSaveLoadedAt != default)
                    {
                        saveLoadCycleStage = 2;
                        saveLoadCycleStageAt = now;
                        return FixtureAttemptResult.Pending;
                    }
                    if ((now - saveLoadCycleLoadRequestedAt).TotalSeconds > 180)
                    {
                        runtime.NotifySaveLoadTimeout("DTMAPI.Smoke", "SmokeHarness.SaveLoadCycle.WaitSaveLoaded", "SaveLoaded was not observed within 180 seconds during cycle " + (saveLoadCycleCompletedCount + 1).ToString(CultureInfo.InvariantCulture) + "; current context=" + runtime.UI.InputContext + ".");
                        throw new TimeoutException("SaveLoaded was not observed within 180 seconds during save/load cycle smoke; current context=" + runtime.UI.InputContext + ".");
                    }
                    LogSaveLoadCyclePending("Waiting for SaveLoaded for cycle " + (saveLoadCycleCompletedCount + 1).ToString(CultureInfo.InvariantCulture) + "/" + requestedCycles.ToString(CultureInfo.InvariantCulture) + ".");
                    return FixtureAttemptResult.Pending;
                }

                if (saveLoadCycleStage == 2)
                {
                    if (saveLoadCycleSaveLoadedAt == default)
                    {
                        LogSaveLoadCyclePending("Waiting for SaveLoaded timestamp before ReturnHome.");
                        return FixtureAttemptResult.Pending;
                    }
                    if (!runtime.UI.InputContext.Equals("Gameplay", StringComparison.OrdinalIgnoreCase))
                    {
                        if ((now - saveLoadCycleSaveLoadedAt).TotalSeconds > 180)
                            throw new TimeoutException("Save/load cycle did not settle into Gameplay within 180 seconds; current context=" + runtime.UI.InputContext + ".");
                        LogSaveLoadCyclePending("Waiting for Gameplay before ReturnHome. context=" + runtime.UI.InputContext + ".");
                        return FixtureAttemptResult.Pending;
                    }
                    if ((now - saveLoadCycleSaveLoadedAt).TotalSeconds < inSaveSeconds)
                        return FixtureAttemptResult.Pending;
                    if (!TryGetStaticBoolProperty(patcher?.ResolveType("DolocAPI, Assembly-CSharp")!, "IsNormalState"))
                    {
                        LogSaveLoadCyclePending("Waiting for NormalGameState before ReturnHome. context=" + runtime.UI.InputContext + ".");
                        return FixtureAttemptResult.Pending;
                    }

                    TryReturnHomeForSaveLoadCycleSmoke();
                    saveLoadCycleStage = 3;
                    saveLoadCycleStageAt = now;
                    return FixtureAttemptResult.Pending;
                }

                if (saveLoadCycleStage == 3)
                {
                    bool homePageActive = runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase);
                    if (!HasContinuousStableObservationForFixture(homePageActive, now, 1.5, ref saveLoadCycleHomePageObservedAt))
                    {
                        if (!homePageActive && (now - saveLoadCycleStageAt).TotalSeconds > 60)
                            throw new TimeoutException("DolocAPI.ReturnHome did not settle on HomePageUiState within 60 seconds during save/load cycle smoke; current context=" + runtime.UI.InputContext + ".");
                        LogSaveLoadCyclePending(homePageActive
                            ? "HomePageUiState observed after ReturnHome; waiting for a continuous stable title window."
                            : "Waiting for HomePageUiState after ReturnHome. context=" + runtime.UI.InputContext + ".");
                        return FixtureAttemptResult.Pending;
                    }

                    runtime.NotifyTitleStable("SmokeHarness.SaveLoadCycle", "HomePageUiState stable after ReturnHome for completed cycle " + (saveLoadCycleCompletedCount + 1).ToString(CultureInfo.InvariantCulture) + ".");
                    saveLoadCycleCompletedCount++;
                    runtime.RuntimeMonitor.Log("Smoke save/load cycle completed " + saveLoadCycleCompletedCount.ToString(CultureInfo.InvariantCulture) + "/" + requestedCycles.ToString(CultureInfo.InvariantCulture) + ".");
                    if (saveLoadCycleCompletedCount >= requestedCycles)
                    {
                        string summary = "cycles=" + saveLoadCycleCompletedCount.ToString(CultureInfo.InvariantCulture) +
                            ", slot=" + g6FixtureState.AutoLoadSaveSlot.ToString(CultureInfo.InvariantCulture) +
                            ", initialIdleSeconds=" + initialIdleSeconds.ToString(CultureInfo.InvariantCulture) +
                            ", intervalSeconds=" + intervalSeconds.ToString(CultureInfo.InvariantCulture) +
                            ", inSaveSeconds=" + inSaveSeconds.ToString(CultureInfo.InvariantCulture) +
                            ", elapsedSeconds=" + (now - saveLoadCycleStartedAt).TotalSeconds.ToString("0", CultureInfo.InvariantCulture) + ".";
                        runtime.RuntimeMonitor.Log("Smoke exercise SaveLoadCycle OK " + summary);
                        runtime.SetHookStatus("Smoke.SaveLoadCycle", "verified", "DolocAPI.LoadGame -> SaveLoaded -> DolocAPI.ReturnHome", summary);
                        return FixtureAttemptResult.Succeeded;
                    }

                    saveLoadCycleSaveLoadedAt = default;
                    saveLoadCycleLoadRequestedAt = default;
                    saveLoadCycleStage = 4;
                    saveLoadCycleStageAt = now;
                    runtime.SetHookStatus("Smoke.SaveLoadCycle", "pending", "HomePageUiState", "Cycle " + saveLoadCycleCompletedCount.ToString(CultureInfo.InvariantCulture) + "/" + requestedCycles.ToString(CultureInfo.InvariantCulture) + " returned to title; waiting interval before next load.");
                    return FixtureAttemptResult.Pending;
                }

                if (saveLoadCycleStage == 4)
                {
                    if ((now - saveLoadCycleStageAt).TotalSeconds < intervalSeconds)
                        return FixtureAttemptResult.Pending;
                    if (!IsSaveLoadCycleHomePageStable(now, 2, "next save load"))
                        return FixtureAttemptResult.Pending;

                    runtime.NotifyTitleStable("SmokeHarness.SaveLoadCycle", "HomePageUiState stable before next save load.");
                    RequestSaveLoadCycleLoad(now);
                    return FixtureAttemptResult.Pending;
                }

                return FixtureAttemptResult.Pending;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke save/load cycle failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.SaveLoadCycle", "failed", "DolocAPI.LoadGame/ReturnHome", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private FixtureAttemptResult TryExerciseSaveLoadCyclePendingPressureForFixture()
        {
            try
            {
                if (g6FixtureState == null)
                    return FixtureAttemptResult.Pending;

                DateTimeOffset now = DateTimeOffset.Now;
                int pressureSeconds = Math.Max(1, g6FixtureState.SaveLoadCyclePendingPressureSeconds);
                double intervalSeconds = Math.Max(0.1, g6FixtureState.SaveLoadCyclePendingPressureIntervalSeconds);
                int expectedPublications = Math.Max(1, (int)Math.Ceiling(pressureSeconds / intervalSeconds));

                if (saveLoadCyclePendingPressureStage == 0)
                {
                    if (!IsSaveLoadCyclePendingPressureHomePageStable(now, 2))
                        return FixtureAttemptResult.Pending;

                    saveLoadCyclePendingPressureStage = 1;
                    saveLoadCyclePendingPressureStartedAt = now;
                    saveLoadCyclePendingPressureLastPublishedAt = DateTimeOffset.MinValue;
                    runtime.RuntimeMonitor.Log("Smoke save/load cycle pending pressure started. seconds=" + pressureSeconds.ToString(CultureInfo.InvariantCulture) + ", intervalSeconds=" + intervalSeconds.ToString("0.###", CultureInfo.InvariantCulture) + ", expectedPublications=" + expectedPublications.ToString(CultureInfo.InvariantCulture) + ".");
                    runtime.SetHookStatus("Smoke.SaveLoadCyclePendingPressure", "pending", "DTMAPI save/load cycle pending pressure", "Publishing Smoke.SaveLoadCycle=pending before a single save load.");
                    return FixtureAttemptResult.Pending;
                }

                if (saveLoadCyclePendingPressureStage == 1)
                {
                    double elapsedSeconds = (now - saveLoadCyclePendingPressureStartedAt).TotalSeconds;
                    int targetPublications = elapsedSeconds >= pressureSeconds
                        ? expectedPublications
                        : Math.Min(expectedPublications, (int)Math.Floor(elapsedSeconds / intervalSeconds) + 1);
                    while (saveLoadCyclePendingPressurePublishedCount < targetPublications)
                    {
                        PublishSaveLoadCyclePendingPressureSample(now, expectedPublications, pressureSeconds, elapsedSeconds);
                    }

                    if (elapsedSeconds < pressureSeconds)
                        return FixtureAttemptResult.Pending;

                    if (!IsSaveLoadCyclePendingPressureHomePageStable(now, 1))
                        return FixtureAttemptResult.Pending;

                    if (!RequestSaveLoadCyclePendingPressureLoad(now, expectedPublications))
                        return FixtureAttemptResult.Pending;

                    return FixtureAttemptResult.Pending;
                }

                if (saveLoadCyclePendingPressureStage == 2)
                {
                    if (autoLoadOfficialPathRequested && !modChangePromptConfirmed)
                        TryConfirmModChangePrompt();
                    if (autoLoadOfficialPathRequested && modChangePromptConfirmed && saveLoadCyclePendingPressureSaveLoadedAt == default)
                        TryAutoLoadSaveDirectFallbackAfterModChange();
                    if (saveLoadCyclePendingPressureSaveLoadedAt != default)
                    {
                        saveLoadCyclePendingPressureStage = 3;
                        return FixtureAttemptResult.Pending;
                    }
                    if ((now - saveLoadCyclePendingPressureLoadRequestedAt).TotalSeconds > 180)
                    {
                        runtime.NotifySaveLoadTimeout("DTMAPI.Smoke", "SmokeHarness.SaveLoadCyclePendingPressure.WaitSaveLoaded", "SaveLoaded was not observed within 180 seconds after pending pressure; current context=" + runtime.UI.InputContext + ".");
                        throw new TimeoutException("SaveLoaded was not observed within 180 seconds after save/load cycle pending pressure; current context=" + runtime.UI.InputContext + ".");
                    }

                    LogSaveLoadCyclePendingPressureStatus("Waiting for SaveLoaded after pending pressure. published=" + saveLoadCyclePendingPressurePublishedCount.ToString(CultureInfo.InvariantCulture) + ".");
                    return FixtureAttemptResult.Pending;
                }

                if (saveLoadCyclePendingPressureStage == 3)
                {
                    string summary = "published=" + saveLoadCyclePendingPressurePublishedCount.ToString(CultureInfo.InvariantCulture) +
                        ", expected=" + expectedPublications.ToString(CultureInfo.InvariantCulture) +
                        ", slot=" + g6FixtureState.AutoLoadSaveSlot.ToString(CultureInfo.InvariantCulture) +
                        ", pressureSeconds=" + pressureSeconds.ToString(CultureInfo.InvariantCulture) +
                        ", intervalSeconds=" + intervalSeconds.ToString("0.###", CultureInfo.InvariantCulture) +
                        ", elapsedSeconds=" + (now - saveLoadCyclePendingPressureStartedAt).TotalSeconds.ToString("0", CultureInfo.InvariantCulture) + ".";
                    runtime.RuntimeMonitor.Log("Smoke exercise SaveLoadCyclePendingPressure OK " + summary);
                    runtime.SetHookStatus("Smoke.SaveLoadCyclePendingPressure", "verified", "Smoke.SaveLoadCycle pending pressure -> SaveLoaded", summary);
                    return FixtureAttemptResult.Succeeded;
                }

                return FixtureAttemptResult.Pending;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke save/load cycle pending pressure failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.SaveLoadCyclePendingPressure", "failed", "Smoke.SaveLoadCycle pending pressure -> SaveLoaded", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private void PublishSaveLoadCyclePendingPressureSample(DateTimeOffset now, int expectedPublications, int pressureSeconds, double elapsedSeconds)
        {
            saveLoadCyclePendingPressurePublishedCount++;
            saveLoadCyclePendingPressureLastPublishedAt = now;
            runtime.SetHookStatus(
                "Smoke.SaveLoadCycle",
                "pending",
                "DTMAPI save/load cycle pending pressure",
                "Pressure pending sample " + saveLoadCyclePendingPressurePublishedCount.ToString(CultureInfo.InvariantCulture) + "/" + expectedPublications.ToString(CultureInfo.InvariantCulture) + "; elapsedSeconds=" + Math.Min(pressureSeconds, elapsedSeconds).ToString("0", CultureInfo.InvariantCulture) + "/" + pressureSeconds.ToString(CultureInfo.InvariantCulture) + ".");
        }

        private int GetRequestedSaveLoadCycleCount()
        {
            int configured = g6FixtureState?.SaveLoadCycleCount ?? 0;
            return Math.Max(1, configured);
        }

        private void RunPreLoadForcedGcProbeIfNeeded(DateTimeOffset now)
        {
            if (g6FixtureState == null || !g6FixtureState.AutoExercisePreLoadGcProbe || saveLoadCyclePreLoadGcProbeCompleted)
                return;

            saveLoadCyclePreLoadGcProbeCompleted = true;
            int humanSlot = Math.Max(1, g6FixtureState.AutoLoadSaveSlot);
            int gameIndex = Math.Max(0, humanSlot - 1);
            string summary = "cycle=1/" + GetRequestedSaveLoadCycleCount().ToString(CultureInfo.InvariantCulture) +
                ", slot=" + humanSlot.ToString(CultureInfo.InvariantCulture) +
                ", elapsedSeconds=" + (now - saveLoadCycleStartedAt).TotalSeconds.ToString("0", CultureInfo.InvariantCulture) +
                ", boundary=before-first-post-idle-load.";

            runtime.RuntimeMonitor.Log("Smoke pre-load forced GC probe starting. " + summary);
            runtime.SetHookStatus("Smoke.PreLoadForcedGCProbe", "pending", "GC.Collect before native LoadGame", "Starting forced managed GC before first post-idle LoadGame. " + summary);
            runtime.NotifyPreLoadForcedGcProbeStarting(
                "SmokeHarness.SaveLoadCycle",
                gameIndex,
                "Before forced managed GC before first post-idle LoadGame. " + summary);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            runtime.NotifyPreLoadForcedGcProbeCompleted(
                "SmokeHarness.SaveLoadCycle",
                gameIndex,
                "After forced managed GC before first post-idle LoadGame. " + summary);
            runtime.SetHookStatus("Smoke.PreLoadForcedGCProbe", "verified", "GC.Collect before native LoadGame", "Forced managed GC completed before first post-idle LoadGame. " + summary);
            runtime.RuntimeMonitor.Log("Smoke pre-load forced GC probe completed. " + summary);
        }

        private void RequestSaveLoadCycleLoad(DateTimeOffset now)
        {
            if (g6FixtureState == null || g6FixtureState.AutoLoadSaveSlot <= 0)
                throw new InvalidOperationException("Save/load cycle smoke requires AutoLoadSaveSlot > 0.");

            int cycleNumber = saveLoadCycleCompletedCount + 1;
            saveLoadCycleSaveLoadedAt = default;
            saveLoadCycleLoadRequestedAt = now;
            saveLoadCycleHomePageObservedAt = default;
            autoLoadOfficialPathRequested = false;
            modChangePromptConfirmed = false;
            pendingAutoLoadGameIndex = null;
            pendingAutoLoadGameDataState = null;
            autoLoadDirectFallbackAfterModChangeAttempted = false;
            saveLoadCycleStage = 1;
            saveLoadCycleStageAt = now;
            bool requested = TryAutoLoadSave(g6FixtureState.AutoLoadSaveSlot);
            if (!requested)
                LogSaveLoadCyclePending("Waiting for existing auto-load path to request cycle " + cycleNumber.ToString(CultureInfo.InvariantCulture) + "/" + GetRequestedSaveLoadCycleCount().ToString(CultureInfo.InvariantCulture) + ".");
            runtime.SetHookStatus("Smoke.SaveLoadCycle", "pending", "GameDataUiState/DolocAPI.LoadGame", "Requested cycle " + cycleNumber.ToString(CultureInfo.InvariantCulture) + "/" + GetRequestedSaveLoadCycleCount().ToString(CultureInfo.InvariantCulture) + " for save slot " + g6FixtureState.AutoLoadSaveSlot.ToString(CultureInfo.InvariantCulture) + " through existing auto-load path.");
        }

        private bool RequestSaveLoadCyclePendingPressureLoad(DateTimeOffset now, int expectedPublications)
        {
            if (g6FixtureState == null || g6FixtureState.AutoLoadSaveSlot <= 0)
                throw new InvalidOperationException("Save/load cycle pending pressure smoke requires AutoLoadSaveSlot > 0.");

            autoLoadOfficialPathRequested = false;
            modChangePromptConfirmed = false;
            pendingAutoLoadGameIndex = null;
            pendingAutoLoadGameDataState = null;
            autoLoadDirectFallbackAfterModChangeAttempted = false;
            bool requested = TryAutoLoadSave(g6FixtureState.AutoLoadSaveSlot);
            if (!requested)
            {
                LogSaveLoadCyclePendingPressureStatus("Waiting for existing auto-load path after pressure. published=" + saveLoadCyclePendingPressurePublishedCount.ToString(CultureInfo.InvariantCulture) + "/" + expectedPublications.ToString(CultureInfo.InvariantCulture) + ".");
                return false;
            }

            saveLoadCyclePendingPressureStage = 2;
            saveLoadCyclePendingPressureLoadRequestedAt = now;
            runtime.RuntimeMonitor.Log("Smoke save/load cycle pending pressure requested save load after " + saveLoadCyclePendingPressurePublishedCount.ToString(CultureInfo.InvariantCulture) + " pending publications.");
            runtime.SetHookStatus("Smoke.SaveLoadCyclePendingPressure", "pending", "GameDataUiState/DolocAPI.LoadGame", "Requested save slot " + g6FixtureState.AutoLoadSaveSlot.ToString(CultureInfo.InvariantCulture) + " after publishing " + saveLoadCyclePendingPressurePublishedCount.ToString(CultureInfo.InvariantCulture) + "/" + expectedPublications.ToString(CultureInfo.InvariantCulture) + " Smoke.SaveLoadCycle pending samples.");
            return true;
        }

        private bool IsSaveLoadCycleHomePageStable(DateTimeOffset now, double requiredSeconds, string purpose)
        {
            bool homePageActive = runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase);
            DateTimeOffset observedBefore = saveLoadCycleHomePageObservedAt;
            if (!HasContinuousStableObservationForFixture(homePageActive, now, requiredSeconds, ref saveLoadCycleHomePageObservedAt))
            {
                if (!homePageActive)
                    LogSaveLoadCyclePending("Waiting for HomePageUiState before " + purpose + ". context=" + runtime.UI.InputContext + ".");
                else if (observedBefore == default)
                    LogSaveLoadCyclePending("HomePageUiState observed before " + purpose + "; waiting for stable title window.");
                return false;
            }

            return true;
        }

        private bool IsSaveLoadCyclePendingPressureHomePageStable(DateTimeOffset now, double requiredSeconds)
        {
            bool homePageActive = runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase);
            DateTimeOffset observedBefore = saveLoadCyclePendingPressureHomePageObservedAt;
            if (!HasContinuousStableObservationForFixture(homePageActive, now, requiredSeconds, ref saveLoadCyclePendingPressureHomePageObservedAt))
            {
                if (!homePageActive)
                    LogSaveLoadCyclePendingPressureStatus("Waiting for HomePageUiState before pending pressure. context=" + runtime.UI.InputContext + ".");
                else if (observedBefore == default)
                    LogSaveLoadCyclePendingPressureStatus("HomePageUiState observed before pending pressure; waiting for stable title window.");
                return false;
            }

            return true;
        }

        internal static bool HasContinuousStableObservationForFixture(
            bool expectedStateActive,
            DateTimeOffset now,
            double requiredSeconds,
            ref DateTimeOffset observedAt)
        {
            if (!expectedStateActive)
            {
                observedAt = default;
                return false;
            }

            if (observedAt == default)
            {
                observedAt = now;
                return requiredSeconds <= 0;
            }

            return (now - observedAt).TotalSeconds >= Math.Max(0, requiredSeconds);
        }

        private void TryReturnHomeForSaveLoadCycleSmoke()
        {
            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? returnHome = FindMethod(dolocApi, "ReturnHome", 1);
            if (returnHome == null)
                throw new MissingMethodException("DolocAPI.ReturnHome(bool) was not found.");

            runtime.RuntimeMonitor.Log("Smoke save/load cycle requesting DolocAPI.ReturnHome after save load.");
            runtime.SetHookStatus("Smoke.SaveLoadCycle", "pending", "DolocAPI.ReturnHome", "Requested return to title for cycle " + (saveLoadCycleCompletedCount + 1).ToString(CultureInfo.InvariantCulture) + "/" + GetRequestedSaveLoadCycleCount().ToString(CultureInfo.InvariantCulture) + ".");
            returnHome.Invoke(null, new object[] { false });
        }

        private void LogSaveLoadCyclePending(string message)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            TimeSpan interval = message.StartsWith("Waiting initial title idle before first save load.", StringComparison.Ordinal)
                ? SaveLoadCycleLongIdlePendingLogInterval
                : SaveLoadCyclePendingLogInterval;
            if (now - lastSaveLoadCycleReadinessLog < interval)
                return;
            lastSaveLoadCycleReadinessLog = now;
            runtime.RuntimeMonitor.Log("Smoke save/load cycle waiting: " + message);
            runtime.SetHookStatus("Smoke.SaveLoadCycle", "pending", "DTMAPI save/load cycle smoke", message);
        }

        private void LogSaveLoadCyclePendingPressureStatus(string message)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            if (now - lastSaveLoadCyclePendingPressureReadinessLog < SaveLoadCyclePendingLogInterval)
                return;
            lastSaveLoadCyclePendingPressureReadinessLog = now;
            runtime.RuntimeMonitor.Log("Smoke save/load cycle pending pressure waiting: " + message);
            runtime.SetHookStatus("Smoke.SaveLoadCyclePendingPressure", "pending", "DTMAPI save/load cycle pending pressure", message);
        }

        private static bool TryCaptureScreenshot(string path)
        {
            try
            {
                Type? screenCapture = Type.GetType("UnityEngine.ScreenCapture, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.ScreenCapture, UnityEngine");
                MethodInfo? captureTexture = screenCapture?.GetMethod("CaptureScreenshotAsTexture", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                object? texture = captureTexture?.Invoke(null, null);
                if (texture != null)
                {
                    try
                    {
                        MethodInfo? encodeToPng = texture.GetType().GetMethod("EncodeToPNG", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                        if (encodeToPng?.Invoke(texture, null) is byte[] bytes && bytes.Length > 0)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                            File.WriteAllBytes(path, bytes);
                            return true;
                        }
                    }
                    finally
                    {
                        TryDestroyUnityObject(texture);
                    }
                }

                if (TryCaptureScreenshotWithReadPixels(path))
                    return true;

                MethodInfo? capture = screenCapture?.GetMethod("CaptureScreenshot", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (capture == null)
                    return false;
                capture.Invoke(null, new object[] { path });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryCaptureScreenshotWithReadPixels(string path)
        {
            object? texture = null;
            try
            {
                Type? screenType = Type.GetType("UnityEngine.Screen, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Screen, UnityEngine");
                Type? texture2DType = Type.GetType("UnityEngine.Texture2D, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Texture2D, UnityEngine");
                Type? textureFormatType = Type.GetType("UnityEngine.TextureFormat, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.TextureFormat, UnityEngine");
                Type? rectType = Type.GetType("UnityEngine.Rect, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Rect, UnityEngine");
                if (screenType == null || texture2DType == null || textureFormatType == null || rectType == null)
                    return false;

                int width = Math.Max(1, Convert.ToInt32(screenType.GetProperty("width", BindingFlags.Public | BindingFlags.Static)?.GetValue(null), System.Globalization.CultureInfo.InvariantCulture));
                int height = Math.Max(1, Convert.ToInt32(screenType.GetProperty("height", BindingFlags.Public | BindingFlags.Static)?.GetValue(null), System.Globalization.CultureInfo.InvariantCulture));
                object format = Enum.Parse(textureFormatType, "RGB24");
                texture = Activator.CreateInstance(texture2DType, width, height, format, false);
                if (texture == null)
                    return false;

                object rect = Activator.CreateInstance(rectType, 0f, 0f, (float)width, (float)height)!;
                MethodInfo? readPixels = texture2DType.GetMethod("ReadPixels", BindingFlags.Public | BindingFlags.Instance, null, new[] { rectType, typeof(int), typeof(int) }, null);
                MethodInfo? apply = texture2DType.GetMethod("Apply", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                MethodInfo? encodeToPng = texture2DType.GetMethod("EncodeToPNG", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (readPixels == null || apply == null || encodeToPng == null)
                    return false;

                readPixels.Invoke(texture, new object[] { rect, 0, 0 });
                apply.Invoke(texture, null);
                if (encodeToPng.Invoke(texture, null) is byte[] bytes && bytes.Length > 0)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                    File.WriteAllBytes(path, bytes);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (texture != null)
                    TryDestroyUnityObject(texture);
            }
        }

        private static void TryDestroyUnityObject(object instance)
        {
            try
            {
                Type? objectType = Type.GetType("UnityEngine.Object, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Object, UnityEngine");
                MethodInfo? destroy = objectType?.GetMethod("Destroy", BindingFlags.Public | BindingFlags.Static, null, new[] { objectType }, null);
                destroy?.Invoke(null, new[] { instance });
            }
            catch
            {
            }
        }

        private static ManifestModel CreateFixtureManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI Smoke",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.Smoke",
                Type = "Smoke"
            };
        }

        private bool TryReloadOfficialModsAndConfig(string statusKey, string reason, out string detail)
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                object? modManager = dolocApi?.GetProperty("modManager", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                MethodInfo? reloadMods = modManager?.GetType().GetMethod("ReloadMods", BindingFlags.Public | BindingFlags.Instance);
                Type? dolocConfig = patcher.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
                MethodInfo? reloadConfig = dolocConfig?.GetMethod("Reload", BindingFlags.Public | BindingFlags.Static);
                if (modManager == null || reloadMods == null)
                    throw new MissingMethodException("DolocAPI.modManager.ReloadMods() was not found.");
                if (reloadConfig == null)
                    throw new MissingMethodException("DolocConfig.Reload() was not found.");

                detail = "official ModManager.ReloadMods + DolocConfig.Reload requested for " + reason + ".";
                runtime.RuntimeMonitor.Log(detail);
                runtime.SetHookStatus(statusKey, "pending", "DolocAPI.modManager.ReloadMods + DolocConfig.Reload", detail);
                reloadMods.Invoke(modManager, null);
                reloadConfig.Invoke(null, null);
                return true;
            }
            catch (Exception ex)
            {
                Exception root = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                detail = root.GetType().Name + ": " + root.Message;
                runtime.SetHookStatus(statusKey, "failed", "DolocAPI.modManager.ReloadMods + DolocConfig.Reload", detail);
                return false;
            }
        }

        private static (int x, int y) ReadVector2IntForFixture(object? vector)
        {
            if (vector == null)
                return (0, 0);
            return (ReadIntMember(vector, "x", 0), ReadIntMember(vector, "y", 0));
        }

        private object? TryCreateTransientResinCollectorForFixture(Type dolocApi, object room, out object? transientResource, out string summary)
        {
            transientResource = null;
            summary = string.Empty;

            Type? hostType = patcher?.ResolveType("DolocTown.IEquipmentHost, Assembly-CSharp");
            if (hostType == null || !hostType.IsInstanceOfType(room))
            {
                summary = "current room is not an IEquipmentHost. room=" + DescribeRoomForFixture(room);
                return null;
            }

            object? proto = QueryEquipmentProtoForFixture(dolocApi, "resin_collector");
            if (proto == null)
            {
                summary = "resin_collector proto missing.";
                return null;
            }

            if (!TryFindResinCollectorPlacementForFixture(room, proto, out object? decalHost, out int decalSlotIndex, out object? worldPosition, out object? anchor, out string placementSummary))
            {
                transientResource = TryCreateTransientResinTreeHostForFixture(room, proto, out string resourceSummary);
                if (transientResource == null ||
                    !TryFindResinCollectorPlacementForFixture(room, proto, out decalHost, out decalSlotIndex, out worldPosition, out anchor, out placementSummary))
                {
                    summary = "decal placement unavailable. initial={" + placementSummary + "}, resource={" + resourceSummary + "}";
                    return null;
                }

                placementSummary = placementSummary + ", transientResource={" + resourceSummary + "}";
            }

            MethodInfo? createEquipment = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "CreateEquipment" && m.GetParameters().Length == 6);
            if (createEquipment == null)
            {
                summary = "IEquipmentHost.CreateEquipment was not found.";
                return null;
            }

            try
            {
                object? collector = createEquipment.Invoke(room, new object?[] { worldPosition, anchor, proto, false, decalHost, decalSlotIndex });
                if (collector == null)
                {
                    summary = "CreateEquipment returned null. placement={" + placementSummary + "}";
                    return null;
                }

                if (!IsTypeOrBase(collector.GetType(), "DolocTown.ResinCollector"))
                {
                    summary = "CreateEquipment returned wrong type " + collector.GetType().FullName + ". placement={" + placementSummary + "}";
                    TryRemoveTransientEquipmentForFixture(room, collector);
                    return null;
                }

                summary = "transient-decal:resin_collector, target=" + DescribeEquipmentForFixture(collector) +
                    ", placement={" + placementSummary + "}";
                return collector;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = "CreateEquipment threw " + ex.InnerException.GetType().Name + ": " + ex.InnerException.Message + ". placement={" + placementSummary + "}";
                return null;
            }
            catch (Exception ex)
            {
                summary = "CreateEquipment threw " + ex.GetType().Name + ": " + ex.Message + ". placement={" + placementSummary + "}";
                return null;
            }
        }

        private object? TryCreateTransientResinTreeHostForFixture(object room, object resinCollectorProto, out string summary)
        {
            summary = string.Empty;
            Type? resourceHostType = patcher?.ResolveType("DolocTown.IDungeonResourceHost, Assembly-CSharp");
            MethodInfo? createResource = FindMethod(resourceHostType, "CreateDungeonResource", 1);
            if (resourceHostType == null || createResource == null || !resourceHostType.IsInstanceOfType(room))
            {
                summary = "room is not an IDungeonResourceHost. room=" + DescribeRoomForFixture(room);
                return null;
            }

            int attempted = 0;
            List<string> notes = new List<string>();
            foreach (object proto in FindOneActionResourceProtosForFixture("Tree"))
            {
                attempted++;
                object? resource = null;
                try
                {
                    resource = createResource.Invoke(room, new object[] { proto });
                    if (resource == null)
                    {
                        notes.Add(ReadStringMember(proto, "Id", "tree") + ":null");
                        continue;
                    }

                    FindMethod(resource.GetType(), "SetMaxGrowthLevel", 1)?.Invoke(resource, new object[] { false });
                    FindMethod(resource.GetType(), "SetMaxHealth", 0)?.Invoke(resource, null);

                    if (AcceptsResinCollectorHostForFixture(room, resinCollectorProto, resource, out string hostFilterSummary) &&
                        TryFindResinCollectorPlacementForFixture(room, resinCollectorProto, out object? host, out int slotIndex, out _, out _, out string placementSummary) &&
                        ReferenceEquals(host, resource))
                    {
                        summary = "created transient tree host " + DescribeDecalHostForFixture(resource) +
                            ", slot=" + slotIndex +
                            ", attempted=" + attempted +
                            ", hostFilter={" + hostFilterSummary + "}, placement={" + placementSummary + "}";
                        return resource;
                    }

                    notes.Add(ReadStringMember(resource, "ResourceName", resource.GetType().Name) + ":not-usable");
                    TryRemoveTransientDungeonResourceForFixture(room, resource);
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    notes.Add(ReadStringMember(proto, "Id", "tree") + ":" + ex.InnerException.GetType().Name);
                    if (resource != null)
                        TryRemoveTransientDungeonResourceForFixture(room, resource);
                }
                catch (Exception ex)
                {
                    notes.Add(ReadStringMember(proto, "Id", "tree") + ":" + ex.GetType().Name);
                    if (resource != null)
                        TryRemoveTransientDungeonResourceForFixture(room, resource);
                }
            }

            summary = "no usable transient tree host. attempted=" + attempted + ", notes=" + string.Join("|", notes.Take(8));
            return null;
        }

        private bool TryFindResinCollectorPlacementForFixture(
            object room,
            object proto,
            out object? decalHost,
            out int decalSlotIndex,
            out object? worldPosition,
            out object? anchor,
            out string summary)
        {
            decalHost = null;
            decalSlotIndex = -1;
            worldPosition = null;
            anchor = null;
            summary = string.Empty;

            object? terrain = ReadMember(room, "DM_terrain");
            MethodInfo? getCachedDecalHosts = terrain == null ? null : FindMethod(terrain.GetType(), "GetCachedDecalHosts", 1);
            if (terrain == null || getCachedDecalHosts == null)
            {
                summary = "DM_terrain.GetCachedDecalHosts unavailable.";
                return false;
            }

            object? fitSlots = ReadMember(proto, "FitSlots");
            if (!(fitSlots is IEnumerable fitSlotEnumerable))
            {
                summary = "resin_collector FitSlots unavailable.";
                return false;
            }

            int hostCount = 0;
            int occupiedCount = 0;
            List<string> notes = new List<string>();
            foreach (object fitSlot in fitSlotEnumerable)
            {
                object? slotType = ReadMember(fitSlot, "SlotType");
                if (slotType == null)
                    continue;

                object? hosts = getCachedDecalHosts.Invoke(terrain, new[] { slotType });
                if (!(hosts is IEnumerable hostEnumerable))
                    continue;

                foreach (object host in hostEnumerable)
                {
                    if (host == null || IsRemoved(host))
                        continue;
                    hostCount++;
                    if (!AcceptsResinCollectorHostForFixture(room, proto, host, out string hostFilterSummary))
                    {
                        notes.Add(DescribeDecalHostForFixture(host) + ":host-filter=" + hostFilterSummary);
                        continue;
                    }

                    foreach ((int slotIndex, object slotWorldPosition) in GetDecalSlotPositionsForFixture(host, slotType))
                    {
                        if (IsDecalSlotOccupiedForFixture(host, slotIndex))
                        {
                            occupiedCount++;
                            continue;
                        }

                        object? slotAnchor = CreateDecalAnchorForFixture(room, proto, slotWorldPosition) ?? ReadMember(host, "Anchor");
                        if (slotAnchor == null)
                        {
                            notes.Add(DescribeDecalHostForFixture(host) + ":anchor-unavailable");
                            continue;
                        }

                        decalHost = host;
                        decalSlotIndex = slotIndex;
                        worldPosition = slotWorldPosition;
                        anchor = slotAnchor;
                        summary = "host=" + DescribeDecalHostForFixture(host) +
                            ", slotType=" + slotType +
                            ", slot=" + slotIndex +
                            ", anchor=" + ReadIntMember(slotAnchor, "x", 0) + "," + ReadIntMember(slotAnchor, "y", 0) +
                            ", world=" + FormatVectorForFixture(slotWorldPosition) +
                            ", hostFilter={" + hostFilterSummary + "}" +
                            ", scannedHosts=" + hostCount +
                            ", occupiedSlots=" + occupiedCount;
                        return true;
                    }
                }
            }

            summary = "no free accepted decal slot. scannedHosts=" + hostCount + ", occupiedSlots=" + occupiedCount + ", notes=" + string.Join("|", notes.Take(6));
            return false;
        }

        private IEnumerable<(int slotIndex, object worldPosition)> GetDecalSlotPositionsForFixture(object decalHost, object slotType)
        {
            Type? extensionType = patcher?.ResolveType("DolocTown.DecalHostExtension, Assembly-CSharp");
            MethodInfo? getSlotWorldPos = extensionType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "GetSlotWorldPos" && m.GetParameters().Length == 2);
            object? positions = getSlotWorldPos?.Invoke(null, new[] { decalHost, slotType });
            if (!(positions is IEnumerable enumerable))
                yield break;

            foreach (object tuple in enumerable)
            {
                object? slotIndexValue = ReadMember(tuple, "Item1");
                object? worldPosition = ReadMember(tuple, "Item2");
                if (slotIndexValue == null || worldPosition == null)
                    continue;
                yield return (Convert.ToInt32(slotIndexValue), worldPosition);
            }
        }

        private bool AcceptsResinCollectorHostForFixture(object room, object proto, object decalHost, out string summary)
        {
            summary = string.Empty;
            Type? equipmentManager = patcher?.ResolveType("DolocTown.EquipmentManager, Assembly-CSharp");
            MethodInfo? createDisposeEquipment = equipmentManager?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "CreateDisposeEquipment" && m.GetParameters().Length == 3);
            if (createDisposeEquipment == null)
            {
                summary = "CreateDisposeEquipment unavailable; using host type fallback.";
                return IsTypeOrBase(decalHost.GetType(), "DolocTown.DungeonResourceTree") || IsTypeOrBase(decalHost.GetType(), "DolocTown.PlantBasinTree");
            }

            try
            {
                object? disposable = createDisposeEquipment.Invoke(null, new object[] { room, proto, false });
                MethodInfo? hostFilter = disposable == null ? null : FindMethod(disposable.GetType(), "HostFilter", 1);
                object? result = hostFilter?.Invoke(disposable, new[] { decalHost });
                bool accepted = result is bool ok && ok;
                summary = accepted ? "accepted" : "rejected";
                return accepted;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private bool IsDecalSlotOccupiedForFixture(object decalHost, int slotIndex)
        {
            object? attachedDecals = ReadMember(decalHost, "AttachedDecals");
            if (attachedDecals == null)
                return false;

            MethodInfo? containsKey = attachedDecals.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "ContainsKey" && m.GetParameters().Length == 1);
            if (containsKey != null)
            {
                object? result = containsKey.Invoke(attachedDecals, new object[] { slotIndex });
                if (result is bool occupied)
                    return occupied;
            }

            if (attachedDecals is IDictionary dictionary)
                return dictionary.Contains(slotIndex);

            return false;
        }

        private object? CreateDecalAnchorForFixture(object room, object proto, object worldPosition)
        {
            object? roomPosition = ReadMember(room, "RoomPosition");
            object? decalPosition = CreateVector2ForFixture(
                ReadDoubleMember(worldPosition, "x", 0),
                ReadDoubleMember(worldPosition, "y", 0));
            if (roomPosition == null || decalPosition == null)
                return null;

            MethodInfo? getAnchorByPosition = proto.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "GetAnchorTile" && m.GetParameters().Length == 2);
            if (getAnchorByPosition != null)
                return getAnchorByPosition.Invoke(proto, new[] { roomPosition, decalPosition });

            MethodInfo? getCoveredTile = FindMethod(proto.GetType(), "GetCoveredTileLBRT", 2);
            MethodInfo? getAnchorByLbrt = FindMethod(proto.GetType(), "GetAnchorTile", 1);
            object? lbrt = getCoveredTile?.Invoke(proto, new[] { roomPosition, decalPosition });
            return lbrt == null ? null : getAnchorByLbrt?.Invoke(proto, new[] { lbrt });
        }

        private static string DescribeDecalHostForFixture(object decalHost)
        {
            return ReadStringMember(decalHost, "Name", ReadStringMember(decalHost, "ResourceName", decalHost.GetType().Name)) +
                "/" + (decalHost.GetType().FullName ?? decalHost.GetType().Name) +
                "/index=" + ReadIntMember(decalHost, "index", -1) +
                "/level=" + ReadIntMember(decalHost, "currentLevel", -1);
        }

        private static string FormatVectorForFixture(object vector)
        {
            return ReadDoubleMember(vector, "x", 0).ToString("0.###") + "," +
                ReadDoubleMember(vector, "y", 0).ToString("0.###") + "," +
                ReadDoubleMember(vector, "z", 0).ToString("0.###");
        }

        private bool TryCreateTransientDandelionVegetationForFixture(Type dolocApi, object room, out object? vegetation, out object? renderer, out string summary)
        {
            vegetation = null;
            renderer = null;
            summary = string.Empty;

            Type? hostType = patcher?.ResolveType("DolocTown.IVegetationHost, Assembly-CSharp");
            if (hostType == null || !hostType.IsInstanceOfType(room))
            {
                summary = "current room is not an IVegetationHost. room=" + DescribeRoomForFixture(room);
                return false;
            }

            MethodInfo? createVegetation = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "CreateVegetationNoRender" && m.GetParameters().Length == 2);
            if (createVegetation == null)
            {
                summary = "IVegetationHost.CreateVegetationNoRender was not found.";
                return false;
            }

            IReadOnlyList<object> protos = FindDandelionVegetationProtosForFixture(out string protoSummary);
            List<string> notes = new List<string>();
            foreach (object proto in protos)
            {
                string protoId = ReadStringMember(proto, "Id", proto.GetType().Name);
                object? candidate = null;
                try
                {
                    candidate = createVegetation.Invoke(room, new object[] { proto, false });
                    if (candidate == null)
                    {
                        candidate = TryCreateDirectTransientVegetationForFixture(room, proto, out string directSummary);
                        if (candidate == null)
                        {
                            notes.Add(protoId + ":create-null,direct={" + directSummary + "}");
                            continue;
                        }

                        notes.Add(protoId + ":direct-create(" + directSummary + ")");
                    }

                    if (!TrySetVegetationMaxGrowthForFixture(candidate, out string growthSummary))
                    {
                        notes.Add(protoId + ":growth-failed(" + growthSummary + ")");
                        TryRemoveTransientVegetationForFixture(room, candidate);
                        continue;
                    }

                    if (!TryRenderVegetationForFixture(dolocApi, room, candidate, out renderer, out string renderSummary) || renderer == null)
                    {
                        notes.Add(protoId + ":render-failed(" + renderSummary + ")");
                        TryRemoveTransientVegetationForFixture(room, candidate);
                        continue;
                    }

                    vegetation = candidate;
                    summary = "transient:" + DescribeVegetationForFixture(candidate) + ", proto={" + protoSummary + "}, growth={" + growthSummary + "}, render={" + renderSummary + "}, notes=" + string.Join("|", notes.Take(8));
                    return true;
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    notes.Add(protoId + ":" + ex.InnerException.GetType().Name);
                    if (candidate != null)
                        TryRemoveTransientVegetationForFixture(room, candidate);
                }
                catch (Exception ex)
                {
                    notes.Add(protoId + ":" + ex.GetType().Name);
                    if (candidate != null)
                        TryRemoveTransientVegetationForFixture(room, candidate);
                }
            }

            summary = "No dandelion vegetation target available. proto={" + protoSummary + "}, transientNotes=" + string.Join("|", notes.Take(8));
            return false;
        }

        private bool TryCreateTransientMatureVegetationForFixture(Type dolocApi, object room, out object? vegetation, out object? renderer, out string summary)
        {
            vegetation = null;
            renderer = null;
            summary = string.Empty;

            Type? hostType = patcher?.ResolveType("DolocTown.IVegetationHost, Assembly-CSharp");
            if (hostType == null || !hostType.IsInstanceOfType(room))
            {
                summary = "current room is not an IVegetationHost. room=" + DescribeRoomForFixture(room);
                return false;
            }

            MethodInfo? createVegetation = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "CreateVegetationNoRender" && m.GetParameters().Length == 2);
            if (createVegetation == null)
            {
                summary = "IVegetationHost.CreateVegetationNoRender was not found.";
                return false;
            }

            IReadOnlyList<object> protos = FindHarvestableVegetationProtosForFixture(out string protoSummary);
            List<string> notes = new List<string>();
            foreach (object proto in protos)
            {
                string protoId = ReadStringMember(proto, "Id", proto.GetType().Name);
                object? candidate = null;
                try
                {
                    candidate = createVegetation.Invoke(room, new object[] { proto, false });
                    if (candidate == null)
                    {
                        candidate = TryCreateDirectTransientVegetationForFixture(room, proto, out string directSummary);
                        if (candidate == null)
                        {
                            notes.Add(protoId + ":create-null,direct={" + directSummary + "}");
                            continue;
                        }

                        notes.Add(protoId + ":direct-create(" + directSummary + ")");
                    }

                    if (!TrySetVegetationMaxGrowthForFixture(candidate, out string growthSummary))
                    {
                        notes.Add(protoId + ":growth-failed(" + growthSummary + ")");
                        TryRemoveTransientVegetationForFixture(room, candidate);
                        continue;
                    }

                    if (!TryRenderVegetationForFixture(dolocApi, room, candidate, out renderer, out string renderSummary) || renderer == null)
                    {
                        notes.Add(protoId + ":render-failed(" + renderSummary + ")");
                        TryRemoveTransientVegetationForFixture(room, candidate);
                        continue;
                    }

                    vegetation = candidate;
                    summary = "transient:" + DescribeVegetationForFixture(candidate) + ", proto={" + protoSummary + "}, growth={" + growthSummary + "}, render={" + renderSummary + "}";
                    return true;
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    notes.Add(protoId + ":" + ex.InnerException.GetType().Name);
                    if (candidate != null)
                        TryRemoveTransientVegetationForFixture(room, candidate);
                }
                catch (Exception ex)
                {
                    notes.Add(protoId + ":" + ex.GetType().Name);
                    if (candidate != null)
                        TryRemoveTransientVegetationForFixture(room, candidate);
                }
            }

            if (TryFindExistingMatureVegetationForFixture(dolocApi, room, out vegetation, out renderer, out string existingSummary))
            {
                summary = "existing:" + existingSummary + ", proto={" + protoSummary + "}, transientNotes=" + string.Join("|", notes.Take(8));
                return true;
            }

            summary = "No mature vegetation target available. proto={" + protoSummary + "}, transientNotes=" + string.Join("|", notes.Take(8)) + ", existing={" + existingSummary + "}";
            return false;
        }

        private object? TryCreateDirectTransientVegetationForFixture(object room, object proto, out string summary)
        {
            summary = string.Empty;
            object? manager = ReadMember(room, "DM_vegetation");
            MethodInfo? createVegetation = manager?.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "CreateVegetation" && m.GetParameters().Length == 3);
            if (manager == null || createVegetation == null)
            {
                summary = "DM_vegetation.CreateVegetation unavailable.";
                return null;
            }

            object? agentCell = ReadStaticMember(patcher?.ResolveType("DolocAPI, Assembly-CSharp"), "AgentRoomCellPosition");
            int anchorX = agentCell == null ? 4 : Math.Max(1, ReadIntMember(agentCell, "x", 4) + 3);
            int anchorY = agentCell == null ? 4 : Math.Max(1, ReadIntMember(agentCell, "y", 4));
            object? gridSize = ReadMember(room, "RoomGridSize");
            int width = Math.Max(1, ReadIntMember(proto, "Width", 1));
            if (gridSize != null)
            {
                anchorX = Math.Min(Math.Max(1, ReadIntMember(gridSize, "x", anchorX + width + 2) - width - 1), anchorX);
                anchorY = Math.Min(Math.Max(1, ReadIntMember(gridSize, "y", anchorY + 2) - 2), anchorY);
            }

            object? anchor = CreateVector2IntForFixture(anchorX, anchorY);
            object? position = CreateEquipmentWorldPositionForFixture(room, anchorX, anchorY, width);
            if (anchor == null || position == null)
            {
                summary = "Could not create anchor/position for direct vegetation.";
                return null;
            }

            object? vegetation = createVegetation.Invoke(manager, new[] { proto, anchor, position });
            if (vegetation == null)
            {
                summary = "CreateVegetation returned null.";
                return null;
            }

            WriteObjectMember(vegetation, "Host", room);
            object? terrain = ReadMember(room, "DM_terrain");
            MethodInfo? fillContent = terrain == null ? null : FindMethod(terrain.GetType(), "FillContent", 1);
            string terrainSummary = "terrain-fill-skipped";
            if (fillContent != null)
            {
                try
                {
                    fillContent.Invoke(terrain, new[] { vegetation });
                    terrainSummary = "terrain-fill-ok";
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    terrainSummary = "terrain-fill-" + ex.InnerException.GetType().Name;
                }
                catch (Exception ex)
                {
                    terrainSummary = "terrain-fill-" + ex.GetType().Name;
                }
            }

            summary = "anchor=" + anchorX + "," + anchorY + ", " + terrainSummary;
            return vegetation;
        }

        private IReadOnlyList<object> FindHarvestableVegetationProtosForFixture(out string summary)
        {
            List<object> preferred = new List<object>();
            List<object> fallback = new List<object>();
            Type? dolocConfig = patcher?.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? tbVegetation = tables == null ? null : ReadMember(tables, "TbVegetation");
            object? dataList = tbVegetation == null ? null : ReadMember(tbVegetation, "DataList");
            if (!(dataList is IEnumerable enumerable))
            {
                summary = "TbVegetation.DataList unavailable.";
                return preferred;
            }

            int scanned = 0;
            foreach (object proto in enumerable)
            {
                scanned++;
                object? function = ReadMember(proto, "Function");
                string functionName = function == null ? string.Empty : function.GetType().Name;
                if (functionName.IndexOf("VegetationFuncCrop", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    preferred.Add(proto);
                    continue;
                }
                if (functionName.IndexOf("VegetationFuncGrowLuminous", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    functionName.IndexOf("VegetationFuncBerryThicket", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    fallback.Add(proto);
                }
            }

            preferred.AddRange(fallback);
            summary = "scanned=" + scanned + ", harvestable=" + preferred.Count + ", sample=" + string.Join("|", preferred.Take(5).Select(proto => ReadStringMember(proto, "Id", proto.GetType().Name)));
            return preferred;
        }

        private IReadOnlyList<object> FindDandelionVegetationProtosForFixture(out string summary)
        {
            List<object> preferred = new List<object>();
            List<object> fallback = new List<object>();
            Type? dolocConfig = patcher?.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? tbVegetation = tables == null ? null : ReadMember(tables, "TbVegetation");
            object? dataList = tbVegetation == null ? null : ReadMember(tbVegetation, "DataList");
            if (!(dataList is IEnumerable enumerable))
            {
                summary = "TbVegetation.DataList unavailable.";
                return preferred;
            }

            int scanned = 0;
            foreach (object proto in enumerable)
            {
                scanned++;
                string id = ReadStringMember(proto, "Id", proto.GetType().Name);
                object? function = ReadMember(proto, "Function");
                string functionName = function == null ? string.Empty : function.GetType().Name;
                if (!TryGetFirstVegetationToolConstraint(proto, out string toolType, out _, out _))
                    continue;

                if (functionName.IndexOf("VegetationFuncDandelion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    id.IndexOf("dandelion", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    preferred.Add(proto);
                    continue;
                }

                if (functionName.IndexOf("VegetationFuncGrow", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    toolType.Equals("SICKLE", StringComparison.OrdinalIgnoreCase))
                {
                    fallback.Add(proto);
                }
            }

            summary = "scanned=" + scanned +
                ", dandelion=" + preferred.Count +
                ", fallback=" + fallback.Count +
                ", sample=" + string.Join("|", preferred.Concat(fallback).Take(4).Select(proto => ReadStringMember(proto, "Id", proto.GetType().Name)));
            return preferred.Count > 0 ? preferred : fallback;
        }

        private bool TryFindExistingMatureVegetationForFixture(Type dolocApi, object room, out object? vegetation, out object? renderer, out string summary)
        {
            vegetation = null;
            renderer = null;
            object? manager = ReadMember(room, "DM_vegetation");
            object? allDatas = manager == null ? null : ReadMember(manager, "AllDatas");
            int scanned = 0;
            if (allDatas is IEnumerable enumerable)
            {
                foreach (object candidate in enumerable)
                {
                    scanned++;
                    string typeName = candidate.GetType().FullName ?? candidate.GetType().Name;
                    if (typeName.IndexOf("VegetationCrop", StringComparison.OrdinalIgnoreCase) < 0 &&
                        typeName.IndexOf("VegetationGrowLuminous", StringComparison.OrdinalIgnoreCase) < 0 &&
                        typeName.IndexOf("VegetationBerryThicket", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    if (!TrySetVegetationMaxGrowthForFixture(candidate, out string growthSummary))
                        continue;

                    if (!TryRenderVegetationForFixture(dolocApi, room, candidate, out renderer, out string renderSummary) || renderer == null)
                        continue;

                    vegetation = candidate;
                    summary = DescribeVegetationForFixture(candidate) + ", scanned=" + scanned + ", growth={" + growthSummary + "}, render={" + renderSummary + "}";
                    return true;
                }
            }

            summary = "no existing harvestable vegetation. scanned=" + scanned;
            return false;
        }

        private bool TrySetVegetationMaxGrowthForFixture(object vegetation, out string summary)
        {
            int maxLevel = ReadIntMember(vegetation, "maxLevel", -1);
            if (maxLevel < 0)
            {
                summary = "maxLevel unavailable. target=" + DescribeVegetationForFixture(vegetation);
                return false;
            }

            MethodInfo? setGrowthLevel = FindMethod(vegetation.GetType(), "SetGrowthLevel", 2);
            if (setGrowthLevel == null)
            {
                summary = "SetGrowthLevel(int,bool) unavailable. target=" + DescribeVegetationForFixture(vegetation);
                return false;
            }

            int before = ReadIntMember(vegetation, "currentLevel", -1);
            setGrowthLevel.Invoke(vegetation, new object[] { maxLevel, false });
            int after = ReadIntMember(vegetation, "currentLevel", -1);
            bool ok = after >= maxLevel && maxLevel > 0;
            summary = "level=" + before + "->" + after + "/" + maxLevel;
            return ok;
        }

        private bool TryRenderVegetationForFixture(Type dolocApi, object room, object vegetation, out object? renderer, out string summary)
        {
            renderer = ReadMember(vegetation, "Renderer");
            if (renderer != null)
            {
                summary = "already-rendered:" + renderer.GetType().Name;
                return true;
            }

            Type? hostType = patcher?.ResolveType("DolocTown.IVegetationHost, Assembly-CSharp");
            Type? rendererType = patcher?.ResolveType("DolocTown.VegetationRenderer, Assembly-CSharp");
            object? entitySystem = ReadStaticMember(dolocApi, "EntitySystem");
            MethodInfo? next = entitySystem?.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "Next" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            MethodInfo? renderVegetation = hostType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "RenderVegetation" && m.GetParameters().Length == 2);
            if (hostType == null || rendererType == null || entitySystem == null || next == null || renderVegetation == null)
            {
                summary = "render dependencies unavailable. hostType=" + (hostType != null) + ", rendererType=" + (rendererType != null) + ", entitySystem=" + (entitySystem != null) + ", next=" + (next != null) + ", render=" + (renderVegetation != null);
                return false;
            }

            renderer = next.MakeGenericMethod(rendererType).Invoke(entitySystem, null);
            if (renderer == null)
            {
                summary = "EntitySystem.Next<VegetationRenderer>() returned null.";
                return false;
            }

            renderVegetation.Invoke(room, new[] { renderer, vegetation });
            object? attached = ReadMember(vegetation, "Renderer");
            bool ok = ReferenceEquals(attached, renderer);
            summary = "rendered=" + ok + ", renderer=" + renderer.GetType().FullName;
            return ok;
        }

        private bool TryInteractWithCurrentInteractableForFixture(Type dolocApi, object interactable, out string summary)
        {
            summary = string.Empty;
            object? gameStateManager = ReadStaticMember(dolocApi, "gameStateManager");
            object? normalGameState = gameStateManager == null ? null : ReadMember(gameStateManager, "normalGameState");
            object? agentController = normalGameState == null ? null : ReadMember(normalGameState, "AgentController");
            object? interactableManager = agentController == null ? null : ReadMember(agentController, "interactableManager");
            MethodInfo? touch = interactableManager == null ? null : FindMethod(interactableManager.GetType(), "Touch", 1);
            MethodInfo? tryInteract = interactableManager == null ? null : FindMethod(interactableManager.GetType(), "TryInteract", 1);
            if (interactableManager == null || touch == null || tryInteract == null)
            {
                summary = "InteractableManagerEx Touch/TryInteract unavailable.";
                return false;
            }

            touch.Invoke(interactableManager, new[] { interactable });
            object? result = tryInteract.Invoke(interactableManager, new object[] { false });
            summary = "manager=" + interactableManager.GetType().FullName + ", target=" + interactable.GetType().FullName + ", result=" + (result is bool ok && ok);
            return result is bool interacted && interacted;
        }

        private void TryRemoveTransientVegetationForFixture(object? room, object vegetation)
        {
            try
            {
                Type? hostType = patcher?.ResolveType("DolocTown.IVegetationHost, Assembly-CSharp");
                MethodInfo? removeVegetation = hostType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "RemoveVegetation" && m.GetParameters().Length == 1);
                if (room != null && hostType != null && hostType.IsInstanceOfType(room) && removeVegetation != null)
                    removeVegetation.Invoke(room, new[] { vegetation });
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke transient vegetation removal failed.", ex.ToString());
            }
        }

        private static string DescribeVegetationForFixture(object vegetation)
        {
            object? anchor = ReadMember(vegetation, "Anchor");
            string name = ReadStringMember(vegetation, "VegetationName", vegetation.GetType().Name);
            int index = ReadIntMember(vegetation, "index", -1);
            int level = ReadIntMember(vegetation, "currentLevel", -1);
            int maxLevel = ReadIntMember(vegetation, "maxLevel", -1);
            string anchorText = anchor == null ? "unknown" : ReadIntMember(anchor, "x", 0) + "," + ReadIntMember(anchor, "y", 0);
            return name + "/" + (vegetation.GetType().FullName ?? vegetation.GetType().Name) + "/index=" + index + "/anchor=" + anchorText + "/level=" + level + "/" + maxLevel;
        }

        private static bool SummaryContainsMultiplier(string summary, double multiplier)
        {
            return summary.IndexOf("multiplier=" + multiplier.ToString("0.###"), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool SummaryContainsKind(string summary, string kind)
        {
            return summary.IndexOf("kind=" + kind, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void SetConfigPendingValue(IConfigMenuPage page, string kind, string value, params string[] nameFragments)
        {
            IConfigMenuItem? item = page.Items.FirstOrDefault(candidate =>
                candidate.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase) &&
                nameFragments.Any(fragment => candidate.Name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0));
            if (item == null)
                throw new InvalidOperationException("Could not find config item kind=" + kind + " names=" + string.Join("/", nameFragments) + ".");
            if (!item.TrySetPendingValue(value, out string error))
                throw new InvalidOperationException("Could not stage config value for " + item.Name + ": " + error);
        }

        private void LogOneActionFuelFeedPending(string message)
        {
            if ((DateTimeOffset.Now - lastOneActionReadinessLog).TotalSeconds < 5)
                return;
            lastOneActionReadinessLog = DateTimeOffset.Now;
            runtime.RuntimeMonitor.Log("Smoke one-action fuel/feed waiting: " + message);
            runtime.SetHookStatus("Smoke.OneActionFuelFeed", "pending", "AgentStateInteract.OnExit", message);
        }

        private void LogOneActionVegetationPending(string message)
        {
            if ((DateTimeOffset.Now - lastOneActionReadinessLog).TotalSeconds < 5)
                return;
            lastOneActionReadinessLog = DateTimeOffset.Now;
            runtime.RuntimeMonitor.Log("Smoke one-action vegetation waiting: " + message);
            runtime.SetHookStatus("Smoke.OneActionVegetation", "pending", "ToolCollider.HandleTools -> VegetationRenderer.OnFell", message);
        }

        private string? SelectFillItemIdForFixture(Type dolocApi, object equipment, string kind, IReadOnlyList<string> itemIds, out string summary)
        {
            List<string> notes = new List<string>();
            MethodInfo? isSuitableFuel = kind.Equals("FuelMachine", StringComparison.OrdinalIgnoreCase)
                ? FindMethod(equipment.GetType(), "IsSuitableFuel", 1)
                : null;
            MethodInfo? isAnimalFeeds = kind.Equals("Feeder", StringComparison.OrdinalIgnoreCase)
                ? FindMethod(equipment.GetType(), "IsAnimalFeeds", 2)
                : null;

            foreach (string itemId in itemIds)
            {
                object? item = GenerateItemForFixture(dolocApi, itemId, 1);
                if (item == null)
                {
                    notes.Add(itemId + ":missing-item");
                    continue;
                }

                if (isSuitableFuel != null)
                {
                    object? result = isSuitableFuel.Invoke(equipment, new object[] { item });
                    if (result is bool suitable && suitable)
                    {
                        summary = "selected=" + itemId + ", validator=IsSuitableFuel";
                        return itemId;
                    }
                    notes.Add(itemId + ":not-fuel");
                    continue;
                }

                if (isAnimalFeeds != null)
                {
                    object? proto = ReadMember(item, "proto");
                    object?[] args = { proto, 0 };
                    object? result = proto == null ? null : isAnimalFeeds.Invoke(equipment, args);
                    if (result is bool isFeed && isFeed)
                    {
                        summary = "selected=" + itemId + ", validator=IsAnimalFeeds, energy=" + args[1];
                        return itemId;
                    }
                    notes.Add(itemId + ":not-feed");
                }
            }

            summary = "no candidate accepted. kind=" + kind + ", notes=" + string.Join("|", notes);
            return null;
        }

        private bool TryPlaceSmokeItemInQuickSlot(
            Type dolocApi,
            object item,
            int slot,
            out object? inventory,
            out object? originalSlotItem,
            out string summary,
            Action<object, object?>? publishRestoreReceipt = null)
        {
            inventory = null;
            originalSlotItem = null;
            summary = string.Empty;

            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? inventorySystem = archive == null ? null : ReadMember(archive, "InventorySystem");
            inventory = inventorySystem == null ? null : ReadMember(inventorySystem, "inventory");
            if (inventory == null)
            {
                summary = "InventorySystem.inventory was not available.";
                return false;
            }

            MethodInfo? read = FindMethod(inventory.GetType(), "Read", 1);
            MethodInfo? take = FindMethod(inventory.GetType(), "Take", 1);
            MethodInfo? placeItemAt = FindMethod(inventory.GetType(), "PlaceItemAt", 2);
            if (read == null || take == null || placeItemAt == null)
            {
                summary = "LinearInventory Read/Take/PlaceItemAt methods were not available.";
                return false;
            }

            originalSlotItem = read.Invoke(inventory, new object[] { slot });
            // Publish the rollback receipt before the first inventory mutation. The
            // cross-frame callers can then retry cleanup even if a later placement,
            // quick-select, or SelectedItem verification step fails.
            publishRestoreReceipt?.Invoke(inventory, originalSlotItem);
            if (originalSlotItem != null)
                take.Invoke(inventory, new object[] { slot });

            object? leftover = placeItemAt.Invoke(inventory, new object[] { slot, item });
            if (leftover != null && ReadIntMember(leftover, "count", 0) > 0)
            {
                summary = "Could not place full smoke stack in quick slot " + slot + "; leftover=" + ReadIntMember(leftover, "count", 0) + ".";
                return false;
            }

            object? uiSystem = ReadStaticMember(dolocApi, "uiSystem");
            object? quickInventory = uiSystem == null ? null : ReadMember(uiSystem, "inventoryQuick");
            if (quickInventory == null || !WriteIntMember(quickInventory, "selectedIndex", slot))
            {
                summary = "Could not select quick inventory slot " + slot + ".";
                return false;
            }

            FindMethod(dolocApi, "QuickSelectCurrentItem", 0)?.Invoke(null, null);
            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            int count = selectedItem == null ? 0 : ReadIntMember(selectedItem, "count", 0);
            if (selectedItem == null || count <= 0)
            {
                summary = "SelectedItem was not available after quick slot placement.";
                return false;
            }

            summary = "quickSlot=" + slot + ", selected=" + ReadStringMember(selectedItem, "name", selectedItem.GetType().Name) + ", count=" + count;
            return true;
        }

        private bool RestoreSmokeQuickSlot(Type dolocApi, object? inventory, int slot, object? originalSlotItem)
        {
            try
            {
                FindMethod(dolocApi, "QuickDeselectCurrentItem", 0)?.Invoke(null, null);
                if (inventory == null)
                    return true;
                MethodInfo? read = FindMethod(inventory.GetType(), "Read", 1);
                MethodInfo? take = FindMethod(inventory.GetType(), "Take", 1);
                MethodInfo? swapItem = FindMethod(inventory.GetType(), "SwapItem", 2);
                if (read == null || take == null || (originalSlotItem != null && swapItem == null))
                    return false;

                take.Invoke(inventory, new object[] { slot });
                if (originalSlotItem != null)
                    swapItem!.Invoke(inventory, new object[] { slot, originalSlotItem });

                object? restored = read.Invoke(inventory, new object[] { slot });
                if (originalSlotItem == null)
                    return restored == null || ReadIntMember(restored, "count", 0) <= 0;
                if (ReferenceEquals(restored, originalSlotItem))
                    return true;
                if (restored == null)
                    return false;

                string expectedName = ReadStringMember(originalSlotItem, "name", string.Empty);
                string actualName = ReadStringMember(restored, "name", string.Empty);
                int expectedCount = ReadIntMember(originalSlotItem, "count", -1);
                int actualCount = ReadIntMember(restored, "count", -1);
                return !string.IsNullOrWhiteSpace(expectedName) &&
                    expectedName.Equals(actualName, StringComparison.OrdinalIgnoreCase) &&
                    expectedCount == actualCount;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke quick-slot restore failed.", ex.ToString());
                return false;
            }
        }

        private bool TrySelectEquipmentForFixture(Type dolocApi, object equipment, object anchor, out string summary)
        {
            summary = string.Empty;
            object? gameStateManager = ReadStaticMember(dolocApi, "gameStateManager");
            object? normalGameState = gameStateManager == null ? null : ReadMember(gameStateManager, "normalGameState");
            object? agentController = normalGameState == null ? null : ReadMember(normalGameState, "AgentController");
            object? scanner = agentController == null ? null : ReadMember(agentController, "RoomScanner");
            MethodInfo? onPosChanged = scanner == null ? null : FindMethod(scanner.GetType(), "OnPosChanged", 1);
            if (scanner == null || onPosChanged == null)
            {
                summary = "RoomScanner.OnPosChanged unavailable.";
                return false;
            }

            onPosChanged.Invoke(scanner, new[] { anchor });
            object? selected = ReadStaticMember(dolocApi, "SelectedEquipment");
            if (ReferenceEquals(selected, equipment))
            {
                summary = "scanner-selected";
                return true;
            }

            if (WriteObjectMember(scanner, "_currentEquipment", equipment) || WriteObjectMember(scanner, "CurrentEquipment", equipment))
            {
                selected = ReadStaticMember(dolocApi, "SelectedEquipment");
                if (ReferenceEquals(selected, equipment))
                {
                    summary = "scanner-forced-current-equipment";
                    return true;
                }
            }

            summary = "selected=" + (selected == null ? "null" : DescribeEquipmentForFixture(selected)) + ", expected=" + DescribeEquipmentForFixture(equipment);
            return false;
        }

        private void TryClearRoomScannerSelectionForFixture(Type dolocApi)
        {
            try
            {
                object? gameStateManager = ReadStaticMember(dolocApi, "gameStateManager");
                object? normalGameState = gameStateManager == null ? null : ReadMember(gameStateManager, "normalGameState");
                object? agentController = normalGameState == null ? null : ReadMember(normalGameState, "AgentController");
                object? scanner = agentController == null ? null : ReadMember(agentController, "RoomScanner");
                FindMethod(scanner?.GetType(), "ClearBuffer", 0)?.Invoke(scanner, null);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke room scanner clear failed.", ex.ToString());
            }
        }

        private bool TryPointAgentCellTipAtEquipmentForFixture(Type dolocApi, object equipment, out string summary)
        {
            summary = string.Empty;
            object? anchor = ReadMember(equipment, "Anchor");
            object? agentCell = ReadStaticMember(dolocApi, "AgentRoomCellPosition");
            object? uiSystem = ReadStaticMember(dolocApi, "uiSystem");
            object? basicTip = uiSystem == null ? null : ReadMember(uiSystem, "basicTip");
            object? cellTip = basicTip == null ? null : ReadMember(basicTip, "AgentCellTip");
            if (anchor == null || agentCell == null || cellTip == null)
            {
                summary = "anchor/AgentRoomCellPosition/AgentCellTip unavailable.";
                return false;
            }

            int dx = ReadIntMember(anchor, "x", 0) - ReadIntMember(agentCell, "x", 0);
            int dy = ReadIntMember(anchor, "y", 0) - ReadIntMember(agentCell, "y", 0);
            object? offset = CreateVector2IntForFixture(dx, dy);
            if (offset == null || !WriteObjectMember(cellTip, "oringinOffset", offset))
            {
                summary = "Could not write AgentCellTip offset dx=" + dx + ",dy=" + dy + ".";
                return false;
            }
            WriteBoolMember(cellTip, "flipWhenFaceLeft", false);

            object? room = ReadStaticMember(dolocApi, "CurrentRoom");
            Type? hostType = patcher?.ResolveType("DolocTown.IEquipmentHost, Assembly-CSharp");
            MethodInfo? getEquipment = hostType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "GetEquipment")
                        return false;
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType.FullName == "UnityEngine.Vector2Int";
                });
            object? cellAnchor = ReadMember(cellTip, "CellAnchor");
            object? selected = room == null || hostType == null || getEquipment == null || cellAnchor == null ? null : getEquipment.Invoke(room, new[] { cellAnchor });
            if (!ReferenceEquals(selected, equipment))
            {
                summary = "cellTip anchor selected=" + (selected == null ? "null" : DescribeEquipmentForFixture(selected)) + ", expected=" + DescribeEquipmentForFixture(equipment) + ", offset=" + dx + "," + dy + ".";
                return false;
            }

            summary = "offset=" + dx + "," + dy + ", anchor=" + ReadIntMember(anchor, "x", 0) + "," + ReadIntMember(anchor, "y", 0);
            return true;
        }

        private bool TryInvokeUseItemContinuesForFixture(Type dolocApi, float dt, out string summary)
        {
            return TryInvokeAgentControllerContinuesForFixture(dolocApi, "UseItemContinues", dt, out summary);
        }

        private bool TryInvokeAgentControllerContinuesForFixture(Type dolocApi, string methodName, float dt, out string summary)
        {
            summary = string.Empty;
            object? gameStateManager = ReadStaticMember(dolocApi, "gameStateManager");
            object? normalGameState = gameStateManager == null ? null : ReadMember(gameStateManager, "normalGameState");
            object? agentController = normalGameState == null ? null : ReadMember(normalGameState, "AgentController");
            MethodInfo? continuesMethod = agentController == null ? null : FindMethod(agentController.GetType(), methodName, 1);
            if (agentController == null || continuesMethod == null)
            {
                summary = "AgentController." + methodName + "(float) unavailable.";
                return false;
            }

            int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
            int beforeContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
            continuesMethod.Invoke(agentController, new object[] { dt });
            int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
            int afterContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
            summary = "method=" + methodName +
                ", dt=" + dt.ToString("0.###") +
                ", currentState=" + ReadCurrentAgentStateForFixture(dolocApi) +
                ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                ", continuous=" + (experimentalApi?.LastActionSpeedContinuousUseSummary ?? "none");
            return true;
        }

        private bool TryRunCurrentAgentInteractExitForFixture(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? agent = ReadStaticMember(dolocApi, "agent");
            object? stateManager = agent == null ? null : ReadMember(agent, "StateManager");
            object? current = stateManager == null ? null : ReadMember(stateManager, "current");
            if (current == null)
            {
                summary = "AgentStateManager.current unavailable.";
                return false;
            }
            if (!IsTypeOrBase(current.GetType(), "DolocTown.AgentStateInteract"))
            {
                summary = "current state is " + (current.GetType().FullName ?? current.GetType().Name) + ", expected DolocTown.AgentStateInteract.";
                return false;
            }

            MethodInfo? onExit = FindMethod(current.GetType(), "OnExit", 0);
            if (onExit == null)
            {
                summary = "AgentStateInteract.OnExit unavailable.";
                return false;
            }

            ActionCompletionService? actionCompletion = ActionCompletionService;
            int before = actionCompletion?.OneActionApplicationCount ?? 0;
            onExit.Invoke(current, null);
            int after = actionCompletion?.OneActionApplicationCount ?? 0;
            summary = "state=" + current.GetType().Name + ", oneActionDelta=" + (after - before);
            return true;
        }

        private void TryEnterIdleStateForFixture(Type dolocApi)
        {
            try
            {
                Type? idleType = patcher?.ResolveType("DolocTown.AgentStateIdle, Assembly-CSharp");
                object? agent = ReadStaticMember(dolocApi, "agent");
                object? stateManager = agent == null ? null : ReadMember(agent, "StateManager");
                MethodInfo? overwrite = stateManager?.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "Overwrite" || !m.IsGenericMethodDefinition)
                            return false;
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType == typeof(bool);
                    });
                if (idleType != null && overwrite != null)
                    overwrite.MakeGenericMethod(idleType).Invoke(stateManager, new object[] { false });
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke idle-state restore failed.", ex.ToString());
            }
        }

        private static string ReadCurrentAgentStateForFixture(Type dolocApi)
        {
            object? agent = ReadStaticMember(dolocApi, "agent");
            object? stateManager = agent == null ? null : ReadMember(agent, "StateManager");
            object? current = stateManager == null ? null : ReadMember(stateManager, "current");
            return current == null ? "unknown" : current.GetType().Name;
        }

        private int ReadQuickSlotItemCount(object? inventory, int slot)
        {
            if (inventory == null)
                return 0;
            MethodInfo? read = FindMethod(inventory.GetType(), "Read", 1);
            object? item = read?.Invoke(inventory, new object[] { slot });
            return item == null ? 0 : ReadIntMember(item, "count", 0);
        }

        private static int ReadInventoryItemCount(object? inventory, int slot)
        {
            if (inventory == null)
                return 0;
            MethodInfo? read = FindMethod(inventory.GetType(), "Read", 1);
            object? item = read?.Invoke(inventory, new object[] { slot });
            return item == null ? 0 : ReadIntMember(item, "count", 0);
        }

        private string ReadQuickSlotItemName(object? inventory, int slot)
        {
            if (inventory == null)
                return string.Empty;
            MethodInfo? read = FindMethod(inventory.GetType(), "Read", 1);
            object? item = read?.Invoke(inventory, new object[] { slot });
            return item == null ? string.Empty : ReadStringMember(item, "name", item.GetType().Name);
        }

        private static string ReadSeedTypeForFixture(object seed)
        {
            object? seedProto = ReadMember(seed, "seedProto");
            if (seedProto == null)
                return "unknown";
            string seedType = ReadStringMember(seedProto, "SeedType", string.Empty);
            if (!string.IsNullOrWhiteSpace(seedType))
                return seedType;
            object? seedTypeRef = ReadMember(seedProto, "SeedType_Ref");
            return seedTypeRef == null ? "unknown" : ReadStringMember(seedTypeRef, "Id", "unknown");
        }

        private static string ReadPlantBasinSeedTypeForFixture(object basin)
        {
            object? seedTypeInfo = ReadMember(basin, "SeedTypeInfo");
            return seedTypeInfo == null ? "unknown" : ReadStringMember(seedTypeInfo, "Id", "unknown");
        }

        private bool TrySetCropMatureForFixture(object crop, out string summary)
        {
            object? seedProto = ReadMember(crop, "seedProto");
            int matureLevel = seedProto == null ? -1 : ReadIntMember(seedProto, "MatureLevel", -1);
            if (matureLevel < 0)
            {
                summary = "Crop seedProto.MatureLevel unavailable.";
                return false;
            }

            MethodInfo? debugSetLevel = FindMethod(crop.GetType(), "DEBUG_SetLevel", 2);
            if (debugSetLevel == null)
            {
                summary = "Crop.DEBUG_SetLevel(bool,int) unavailable.";
                return false;
            }

            int beforeLevel = ReadIntMember(crop, "CurrentLevel", -1);
            bool beforeMature = ReadBoolMember(crop, "isMature", false);
            debugSetLevel.Invoke(crop, new object[] { false, matureLevel });
            int afterLevel = ReadIntMember(crop, "CurrentLevel", -1);
            bool afterMature = ReadBoolMember(crop, "isMature", false);
            summary = "level=" + beforeLevel + "->" + afterLevel + "/" + matureLevel + ", mature=" + beforeMature + "->" + afterMature;
            return afterMature && afterLevel >= matureLevel;
        }

        private object? QueryEquipmentProtoForFixture(Type dolocApi, string equipmentId)
        {
            MethodInfo? queryEquipment = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "QueryEquipment")
                        return false;
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length == 2 && parameters[0].ParameterType == typeof(string) && parameters[1].IsOut;
                });
            if (queryEquipment == null)
                return null;
            object?[] args = { equipmentId, null };
            object? result = queryEquipment.Invoke(null, args);
            return result is bool ok && ok ? args[1] : null;
        }

        private IEnumerable<(int x, int y)> EnumerateSmokeEquipmentAnchors(Type dolocApi, object room, int width, int height)
        {
            object? agentCell = ReadStaticMember(dolocApi, "AgentRoomCellPosition");
            object? gridSize = ReadMember(room, "RoomGridSize");
            int fallbackX = agentCell == null ? 4 : ReadIntMember(agentCell, "x", 4);
            int fallbackY = agentCell == null ? 4 : ReadIntMember(agentCell, "y", 4);
            int maxX = gridSize == null ? fallbackX + 24 : Math.Max(1, ReadIntMember(gridSize, "x", fallbackX + 24) - width - 1);
            int maxY = gridSize == null ? fallbackY + 12 : Math.Max(1, ReadIntMember(gridSize, "y", fallbackY + 12) - height - 1);
            object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
            bool isCurrentRoom = ReferenceEquals(room, currentRoom);
            int baseX = isCurrentRoom ? fallbackX : Math.Max(1, maxX / 2);
            int baseY = isCurrentRoom ? fallbackY : Math.Max(1, maxY / 2);

            (int dx, int dy)[] offsets =
            {
                (2, 0), (4, 0), (6, 0), (-4, 0), (-6, 0),
                (0, 2), (2, 2), (4, 2), (-4, 2), (0, -2),
                (8, 2), (-8, 2), (2, 4), (-2, 4), (6, 4)
            };

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach ((int dx, int dy) in offsets)
            {
                int x = Math.Max(1, Math.Min(maxX, baseX + dx));
                int y = Math.Max(1, Math.Min(maxY, baseY + dy));
                string key = x + "," + y;
                if (seen.Add(key))
                    yield return (x, y);
            }

            // Cross-room fixtures cannot reuse the player's current-room cell as a
            // sufficient search origin. Exhaust the bounded native grid only after
            // the near-origin candidates, and let GetEquipment reject every occupied
            // footprint before a transient object is created.
            for (int y = 1; y <= maxY; y++)
            {
                for (int x = 1; x <= maxX; x++)
                {
                    string key = x + "," + y;
                    if (seen.Add(key))
                        yield return (x, y);
                }
            }
        }

        private void TryRemoveTransientDungeonResourceForFixture(object? room, object resource)
        {
            try
            {
                Type? hostType = patcher?.ResolveType("DolocTown.IDungeonResourceHost, Assembly-CSharp");
                MethodInfo? removeResource = hostType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "RemoveDungeonResource" && m.GetParameters().Length == 1);
                if (room != null && hostType != null && hostType.IsInstanceOfType(room) && removeResource != null)
                    removeResource.Invoke(room, new object[] { resource });
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke transient dungeon resource removal failed.", ex.ToString());
            }
        }

        private bool TryRenderAllResourcesForFixture(object room)
        {
            try
            {
                Type? hostType = patcher?.ResolveType("DolocTown.IDungeonResourceHost, Assembly-CSharp");
                MethodInfo? renderAllResources = FindMethod(hostType, "RenderAllResources", 0);
                if (renderAllResources == null || hostType == null || !hostType.IsInstanceOfType(room))
                    return false;
                renderAllResources.Invoke(room, null);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action resource render probe failed.", ex.ToString());
                return false;
            }
        }

        private bool TryEnterMainFarmForOneActionSmoke(Type dolocApi, object currentRoom, out string summary)
        {
            summary = string.Empty;
            try
            {
                object? archiveHandle = dolocApi.GetProperty("archiveHandle", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                object? mainFarm = archiveHandle == null ? null : ReadMember(archiveHandle, "MainFarm");
                object? geometry = mainFarm == null ? null : ReadMember(mainFarm, "Geometry");
                object? entryPosition = geometry == null ? null : ReadMember(geometry, "DefaultEntryPosition");
                if (mainFarm == null || entryPosition == null)
                    return false;

                MethodInfo? enterFarm = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] parameters = m.GetParameters();
                        return m.Name == "EnterFarm" &&
                            parameters.Length == 3 &&
                            parameters[0].ParameterType == typeof(string);
                    });
                if (enterFarm == null)
                    return false;

                object? result = enterFarm.Invoke(null, new object?[] { string.Empty, entryPosition, null });
                if (result is bool entered && !entered)
                    return false;

                autoExerciseOneActionMainFarmRequested = true;
                summary = "Current room has no rendered one-action resource; requested official main farm transition for smoke. from=" + DescribeRoomForFixture(currentRoom) + ", to=" + DescribeRoomForFixture(mainFarm);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action main farm transition failed.", ex.ToString());
                return false;
            }
        }

        private bool TryCreateTransientOneActionResourceForFixture(object room, out string summary)
        {
            return TryCreateTransientOneActionResourceForFixture(room, null, out summary);
        }

        private bool TryCreateTransientOneActionResourceForFixture(object room, string? targetKind, out string summary)
        {
            return TryCreateTransientOneActionResourceForFixture(room, targetKind, null, out summary);
        }

        private bool TryCreateTransientOneActionResourceForFixture(object room, string? targetKind, string? resourceNameContains, out string summary)
        {
            return TryCreateTransientOneActionResourceForFixture(room, targetKind, resourceNameContains, out _, out summary);
        }

        private bool TryCreateTransientOneActionResourceForFixture(object room, string? targetKind, string? resourceNameContains, out object? createdResource, out string summary)
        {
            createdResource = null;
            summary = string.Empty;
            Type? hostType = patcher?.ResolveType("DolocTown.IDungeonResourceHost, Assembly-CSharp");
            MethodInfo? createResource = FindMethod(hostType, "CreateDungeonResource", 1);
            if (hostType == null || createResource == null || !hostType.IsInstanceOfType(room))
                return false;

            int attempted = 0;
            foreach (object proto in FindOneActionResourceProtosForFixture(targetKind, resourceNameContains))
            {
                attempted++;
                try
                {
                    object? resource = createResource.Invoke(room, new object[] { proto });
                    if (resource == null)
                        continue;
                    createdResource = resource;
                    string resourceName = ReadStringMember(resource, "ResourceName", resource.GetType().Name);
                    string resourceClass = ReadResourceClass(resource);
                    string kind = GetOneActionResourceKind(resource);
                    summary = "Smoke created transient one-action resource through IDungeonResourceHost.CreateDungeonResource. room=" + DescribeRoomForFixture(room) + ", targetKind=" + (targetKind ?? "any") + ", resourceNameContains=" + (resourceNameContains ?? "any") + ", resource=" + resourceName + ", kind=" + kind + ", class=" + resourceClass + ", attemptedProtos=" + attempted;
                    return true;
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Transient one-action resource create failed for a candidate.", ex.InnerException.ToString());
                }
                catch (Exception ex)
                {
                    runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Transient one-action resource create failed for a candidate.", ex.ToString());
                }
            }

            summary = "No transient one-action resource candidate could be created. room=" + DescribeRoomForFixture(room) + ", targetKind=" + (targetKind ?? "any") + ", resourceNameContains=" + (resourceNameContains ?? "any") + ", attemptedProtos=" + attempted;
            return false;
        }

        private IEnumerable<object> FindOneActionResourceProtosForFixture(string? targetKind = null, string? resourceNameContains = null)
        {
            Type? dolocConfig = patcher?.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? tbResource = tables == null ? null : ReadMember(tables, "TbResource");
            object? dataList = tbResource == null ? null : ReadMember(tbResource, "DataList");
            if (!(dataList is IEnumerable enumerable))
                yield break;

            foreach (object proto in enumerable)
            {
                string id = ReadStringMember(proto, "Id", string.Empty);
                if (string.IsNullOrWhiteSpace(id))
                    id = ReadStringMember(proto, "id", string.Empty);
                if (!string.IsNullOrWhiteSpace(resourceNameContains) &&
                    id.IndexOf(resourceNameContains, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                string resourceClass = ReadMember(proto, "ResourceClass")?.ToString() ?? string.Empty;
                object? resourceTypeRef = ReadMember(proto, "ResourceType_Ref");
                string classTypeName = resourceTypeRef == null ? string.Empty : ReadStringMember(resourceTypeRef, "ClassTypeName", string.Empty);
                string kind = GetOneActionProtoKind(resourceClass, classTypeName);
                if (!string.IsNullOrWhiteSpace(kind) && (string.IsNullOrWhiteSpace(targetKind) || kind.Equals(targetKind, StringComparison.OrdinalIgnoreCase)))
                    yield return proto;
            }
        }

        private static bool IsMainFarmRoomForFixture(Type dolocApi, object room)
        {
            object? archiveHandle = dolocApi.GetProperty("archiveHandle", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? mainFarm = archiveHandle == null ? null : ReadMember(archiveHandle, "MainFarm");
            return mainFarm != null && ReferenceEquals(mainFarm, room);
        }

        private static string DescribeRoomForFixture(object room)
        {
            return "type=" + room.GetType().Name +
                ", roomId=" + ReadStringMember(room, "RoomId", string.Empty) +
                ", title=" + ReadStringMember(room, "Title", string.Empty) +
                ", inHouse=" + ReadBoolMember(room, "IsInHouse", false) +
                ", renderNow=" + (ReadBoolMember(room, "IsRenderNow", false) || ReadBoolMember(room, "isRenderNow", false)) +
                ", resources=" + CountRoomResourcesForFixture(room);
        }

        private static int CountRoomResourcesForFixture(object room)
        {
            object? manager = ReadMember(room, "DM_dungeonResource");
            object? total = manager == null ? null : ReadMember(manager, "ResourceTotalCount");
            if (total != null)
                return Convert.ToInt32(total);
            object? resources = manager == null ? null : ReadMember(manager, "AllDungeonResources");
            if (!(resources is IEnumerable enumerable))
                return -1;
            int count = 0;
            foreach (object _ in enumerable)
                count++;
            return count;
        }

        private object[] FindUnityObjects(Type type, bool includeInactive = false)
        {
            Type? unityObjectType = patcher?.ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ??
                patcher?.ResolveType("UnityEngine.Object, UnityEngine");
            if (unityObjectType == null)
                return Array.Empty<object>();

            if (includeInactive)
            {
                MethodInfo? includeInactiveFindObjectsOfType = unityObjectType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "FindObjectsOfType" || m.IsGenericMethodDefinition)
                            return false;
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 2 &&
                            parameters[0].ParameterType == typeof(Type) &&
                            parameters[1].ParameterType == typeof(bool);
                    });
                object[] foundWithInactive = ToObjectArray(includeInactiveFindObjectsOfType?.Invoke(null, new object[] { type, true }));
                if (foundWithInactive.Length > 0)
                    return foundWithInactive;

                MethodInfo? findObjectsByType = unityObjectType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "FindObjectsByType" || m.IsGenericMethodDefinition)
                            return false;
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 3 &&
                            parameters[0].ParameterType == typeof(Type) &&
                            parameters[1].ParameterType.IsEnum &&
                            parameters[2].ParameterType.IsEnum;
                    });
                if (findObjectsByType != null)
                {
                    ParameterInfo[] parameters = findObjectsByType.GetParameters();
                    object include = Enum.Parse(parameters[1].ParameterType, "Include");
                    object none = Enum.Parse(parameters[2].ParameterType, "None");
                    object[] foundByType = ToObjectArray(findObjectsByType.Invoke(null, new[] { type, include, none }));
                    if (foundByType.Length > 0)
                        return foundByType;
                }

                Type? resourcesType = patcher?.ResolveType("UnityEngine.Resources, UnityEngine.CoreModule") ??
                    patcher?.ResolveType("UnityEngine.Resources, UnityEngine");
                MethodInfo? findObjectsOfTypeAll = resourcesType?.GetMethod("FindObjectsOfTypeAll", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type) }, null);
                object[] allObjects = ToObjectArray(findObjectsOfTypeAll?.Invoke(null, new object[] { type }));
                if (allObjects.Length > 0)
                    return allObjects;
            }

            MethodInfo? findObjectsOfType = unityObjectType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "FindObjectsOfType" || m.IsGenericMethodDefinition)
                        return false;
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType == typeof(Type);
                });
            return ToObjectArray(findObjectsOfType?.Invoke(null, new object[] { type }));
        }

        private static object[] ToObjectArray(object? found)
        {
            if (!(found is Array array))
                return Array.Empty<object>();
            return array.Cast<object>().Where(o => o != null).ToArray();
        }

        private object? FindFirstUnityObject(Type type)
        {
            return FindUnityObjects(type).FirstOrDefault();
        }

        private object? FindToolColliderForFixture(Type dolocApi, Type toolColliderType)
        {
            object? activeCollider = FindFirstUnityObject(toolColliderType);
            if (activeCollider != null)
                return activeCollider;

            object? agent = dolocApi.GetProperty("agent", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? toolRenderer = agent == null ? null : ReadMember(agent, "ToolRenderer");
            if (toolRenderer == null)
                return null;

            toolRenderer.GetType().GetMethod("Init", BindingFlags.Public | BindingFlags.Instance)?.Invoke(toolRenderer, null);
            return ReadMember(toolRenderer, "_collider");
        }

        private static object? GenerateItemForFixture(Type dolocApi, string itemId)
        {
            return GenerateItemForFixture(dolocApi, itemId, 1);
        }

        private static object? GenerateItemForFixture(Type dolocApi, string itemId, int count)
        {
            MethodInfo? generateItem = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    ParameterInfo[] p = m.GetParameters();
                    return m.Name == "GenerateItem" &&
                        p.Length >= 1 &&
                        p.Length <= 2 &&
                        p[0].ParameterType == typeof(string) &&
                        (p.Length == 1 || p[1].ParameterType == typeof(int));
                });
            if (generateItem == null)
                return null;
            return generateItem.GetParameters().Length == 1
                ? generateItem.Invoke(null, new object[] { itemId })
                : generateItem.Invoke(null, new object[] { itemId, Math.Max(1, count) });
        }

        private static bool TryChooseToolIdForResource(object resource, out string toolId)
        {
            toolId = string.Empty;
            string kind = GetOneActionResourceKind(resource);
            if (kind.Equals("Tree", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_axe";
                return true;
            }
            if (kind.Equals("Ore", StringComparison.OrdinalIgnoreCase) ||
                kind.Equals("Garbage", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_pickaxe";
                return true;
            }
            if (kind.Equals("Weeds", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_sickle";
                return true;
            }
            return false;
        }

        private static bool TryChooseToolIdsForVegetation(object vegetation, out string expectedToolId, out string expectedToolType, out int expectedMinLevel, out string wrongToolId, out string summary)
        {
            expectedToolId = string.Empty;
            expectedToolType = string.Empty;
            expectedMinLevel = -1;
            wrongToolId = string.Empty;
            summary = string.Empty;

            object? proto = ReadMember(vegetation, "proto");
            if (proto == null)
            {
                summary = "vegetation proto unavailable. target=" + DescribeVegetationForFixture(vegetation);
                return false;
            }

            if (!TryGetFirstVegetationToolConstraint(proto, out expectedToolType, out expectedMinLevel, out summary))
                return false;

            if (!TryGetToolIdForToolType(expectedToolType, out expectedToolId) ||
                !TryGetWrongToolIdForToolType(expectedToolType, out wrongToolId))
            {
                summary = "No smoke tool mapping for vegetation toolType=" + expectedToolType + ".";
                return false;
            }

            summary = "toolType=" + expectedToolType + ", minLevel=" + expectedMinLevel + ", expectedTool=" + expectedToolId + ", wrongTool=" + wrongToolId;
            return true;
        }

        private static bool TryGetFirstVegetationToolConstraint(object proto, out string toolType, out int minLevel, out string summary)
        {
            toolType = string.Empty;
            minLevel = -1;
            summary = string.Empty;

            object? constraints = ReadMember(proto, "ToolConstraints");
            if (!(constraints is IEnumerable enumerable))
            {
                summary = "ToolConstraints unavailable for vegetation proto " + ReadStringMember(proto, "Id", proto.GetType().Name) + ".";
                return false;
            }

            foreach (object constraint in enumerable)
            {
                toolType = ReadMember(constraint, "ToolType")?.ToString() ?? string.Empty;
                minLevel = ReadIntMember(constraint, "ToolLevel", -1);
                if (!string.IsNullOrWhiteSpace(toolType))
                {
                    summary = "proto=" + ReadStringMember(proto, "Id", proto.GetType().Name) + ", toolType=" + toolType + ", minLevel=" + minLevel;
                    return true;
                }
            }

            summary = "No tool constraint entries for vegetation proto " + ReadStringMember(proto, "Id", proto.GetType().Name) + ".";
            return false;
        }

        private static bool TryGetToolIdForToolType(string toolType, out string toolId)
        {
            toolId = string.Empty;
            if (toolType.Equals("AXE", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_axe";
                return true;
            }
            if (toolType.Equals("PICKAXE", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_pickaxe";
                return true;
            }
            if (toolType.Equals("SICKLE", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_sickle";
                return true;
            }
            return false;
        }

        private static bool TryGetWrongToolIdForToolType(string toolType, out string toolId)
        {
            toolId = string.Empty;
            if (toolType.Equals("AXE", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_pickaxe";
                return true;
            }
            if (toolType.Equals("PICKAXE", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_axe";
                return true;
            }
            if (toolType.Equals("SICKLE", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_pickaxe";
                return true;
            }
            return false;
        }

        private static bool TryChooseMismatchedToolIdForResource(object resource, out string toolId, out string expectedToolId)
        {
            toolId = string.Empty;
            expectedToolId = string.Empty;
            string kind = GetOneActionResourceKind(resource);
            if (kind.Equals("Tree", StringComparison.OrdinalIgnoreCase))
            {
                expectedToolId = "old_axe";
                toolId = "old_pickaxe";
                return true;
            }
            if (kind.Equals("Ore", StringComparison.OrdinalIgnoreCase) ||
                kind.Equals("Garbage", StringComparison.OrdinalIgnoreCase))
            {
                expectedToolId = "old_pickaxe";
                toolId = "old_axe";
                return true;
            }
            if (kind.Equals("Weeds", StringComparison.OrdinalIgnoreCase))
            {
                expectedToolId = "old_sickle";
                toolId = "old_pickaxe";
                return true;
            }
            return false;
        }

        private static string GetOneActionResourceKind(object resource)
        {
            Type resourceType = resource.GetType();
            string typeName = resourceType.FullName ?? resourceType.Name;
            string resourceClass = ReadResourceClass(resource);
            if (IsOneActionTreeResource(resourceType))
                return "Tree";
            if (IsTypeOrBase(resourceType, "DolocTown.DungeonResourceOre") || resourceClass.Equals("Ore", StringComparison.OrdinalIgnoreCase))
                return "Ore";
            if (typeName.IndexOf("Garbage", StringComparison.OrdinalIgnoreCase) >= 0 || resourceClass.Equals("Garbage", StringComparison.OrdinalIgnoreCase))
                return "Garbage";
            if (IsTypeOrBase(resourceType, "DolocTown.DungeonResourceWeeds"))
                return "Weeds";
            return string.Empty;
        }

        private static string GetOneActionProtoKind(string resourceClass, string classTypeName)
        {
            if (ClassNameMatches(classTypeName, "DolocTown.DungeonResourceTree") ||
                ClassNameMatches(classTypeName, "DolocTown.DungeonResourceTreeTrunk"))
                return "Tree";
            if (ClassNameMatches(classTypeName, "DolocTown.DungeonResourceOre") ||
                resourceClass.Equals("Ore", StringComparison.OrdinalIgnoreCase))
                return "Ore";
            if (classTypeName.IndexOf("Garbage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                resourceClass.Equals("Garbage", StringComparison.OrdinalIgnoreCase))
                return "Garbage";
            if (ClassNameMatches(classTypeName, "DolocTown.DungeonResourceWeeds") ||
                ClassNameMatches(classTypeName, "DolocTown.DungeonResourceWeedsSmall"))
                return "Weeds";
            return string.Empty;
        }

        private static bool IsOneActionTreeResource(Type type)
        {
            return IsTypeOrBase(type, "DolocTown.DungeonResourceTree") ||
                IsTypeOrBase(type, "DolocTown.DungeonResourceTreeTrunk");
        }

        private static bool ClassNameMatches(string classTypeName, string fullName)
        {
            string simpleName = fullName.Substring(fullName.LastIndexOf('.') + 1);
            return classTypeName.Equals(fullName, StringComparison.Ordinal) ||
                classTypeName.Equals(simpleName, StringComparison.Ordinal) ||
                classTypeName.EndsWith("." + simpleName, StringComparison.Ordinal);
        }

        private static bool IsRemoved(object instance)
        {
            object? value = ReadMember(instance, "IsRemoved");
            return value is bool removed && removed;
        }

        private static bool IsCoalResourceNameForFixture(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                return false;
            string normalized = resourceName.Trim().ToLowerInvariant();
            return normalized.Equals("coal", StringComparison.Ordinal) ||
                normalized.Equals("coal_ore", StringComparison.Ordinal) ||
                normalized.Contains("coal");
        }

        private static int ReadIntMember(object instance, string name, int fallback)
        {
            object? value = ReadMember(instance, name);
            return value == null ? fallback : Convert.ToInt32(value);
        }

        private static int ReadAnyIntMember(object instance, int fallback, params string[] names)
        {
            object? value = ReadAnyMember(instance, names);
            if (value == null)
                return fallback;

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return fallback;
            }
        }

        private static bool ReadBoolMember(object instance, string name, bool fallback)
        {
            object? value = ReadMember(instance, name);
            return value == null ? fallback : Convert.ToBoolean(value);
        }

        private static bool ReadAnyBoolMember(object instance, bool fallback, params string[] names)
        {
            object? value = ReadAnyMember(instance, names);
            if (value == null)
                return fallback;

            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return fallback;
            }
        }

        private static bool WriteIntMember(object instance, string name, int value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(int))
                {
                    field.SetValue(instance, value);
                    return true;
                }
                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && property.PropertyType == typeof(int))
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
        }

        private static bool WriteBoolMember(object instance, string name, bool value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(bool))
                {
                    field.SetValue(instance, value);
                    return true;
                }
                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
        }

        private static bool WriteStaticBoolMember(Type type, string name, bool value)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                FieldInfo? field = current.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) ??
                    current.GetField("<" + name + ">k__BackingField", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null && field.FieldType == typeof(bool))
                {
                    field.SetValue(null, value);
                    return true;
                }

                PropertyInfo? property = current.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
                {
                    property.SetValue(null, value);
                    return true;
                }
            }
            return false;
        }

        private static bool WriteObjectMember(object instance, string name, object value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && IsAssignableToMember(field.FieldType, value))
                {
                    field.SetValue(instance, value);
                    return true;
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && IsAssignableToMember(property.PropertyType, value))
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
        }

        private static bool IsAssignableToMember(Type memberType, object value)
        {
            if (value == null)
                return !memberType.IsValueType;
            return memberType.IsInstanceOfType(value) || memberType == value.GetType();
        }

        private static string ReadStringMember(object instance, string name, string fallback = "")
        {
            object? value = ReadMember(instance, name);
            return value as string ?? fallback;
        }

        private static string ReadAnyStringMember(object instance, string fallback, params string[] names)
        {
            object? value = ReadAnyMember(instance, names);
            return value as string ?? value?.ToString() ?? fallback;
        }

        private static IEnumerable<string> ReadStringValues(object? value)
        {
            if (value == null)
                yield break;
            if (value is string text)
            {
                if (!string.IsNullOrWhiteSpace(text))
                    yield return text;
                yield break;
            }
            if (value is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    string? itemText = item?.ToString();
                    if (!string.IsNullOrWhiteSpace(itemText))
                        yield return itemText!;
                }
                yield break;
            }

            string? fallback = value.ToString();
            if (!string.IsNullOrWhiteSpace(fallback))
                yield return fallback!;
        }

        private static string ReadResourceClass(object resource)
        {
            object? proto = ReadMember(resource, "Proto");
            object? resourceClass = proto == null ? null : ReadMember(proto, "ResourceClass");
            return resourceClass?.ToString() ?? string.Empty;
        }

        private static object? ReadStaticMember(Type? type, string name)
        {
            if (type == null)
                return null;
            object? value = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            if (value != null)
                return value;
            return type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
        }

        private static object? ReadMember(object instance, string name)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                object? value = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance) ??
                    type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);
                if (value != null)
                    return value;
            }
            return null;
        }

        private static bool SetMemberValue(object instance, string name, object value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && (value == null || field.FieldType.IsInstanceOfType(value)))
                {
                    field.SetValue(instance, value);
                    return true;
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && (value == null || property.PropertyType.IsInstanceOfType(value)))
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
        }

        private static object? ReadAnyMember(object instance, params string[] names)
        {
            foreach (string name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                object? value = ReadMember(instance, name);
                if (value != null)
                    return value;
            }

            return null;
        }

        private static bool IsTypeOrBase(Type type, string fullName)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                if (string.Equals(current.FullName, fullName, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static double ReadDoubleMember(object? instance, string name, double fallback)
        {
            if (instance == null)
                return fallback;
            object? value = ReadMember(instance, name);
            return value == null ? fallback : Convert.ToDouble(value);
        }

        private static string FormatRatio(double value)
        {
            if (double.IsNaN(value) || value < 0)
                return "unknown";
            return value.ToString("0.###");
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return string.Empty;
        }

        private static bool ContainsIgnoreCase(string value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(search) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsAny(IEnumerable<string> values, params string[] searches)
        {
            foreach (string value in values)
            {
                foreach (string search in searches)
                {
                    if (ContainsIgnoreCase(value, search))
                        return true;
                }
            }

            return false;
        }

        private static MethodInfo? FindMethod(Type? type, string name, int parameterCount)
        {
            if (type == null)
                return null;
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (!method.Name.Equals(name, StringComparison.Ordinal) || method.GetParameters().Length != parameterCount)
                    continue;
                return method;
            }
            return null;
        }

        private bool TryAutoLoadSaveViaOfficialUi(HarmonyReflectionPatcher patcher, int humanSlot, int gameIndex, out bool waitForOfficialUi)
        {
            waitForOfficialUi = false;
            Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
            Type? homePageUiState = patcher.ResolveType("DolocTown.HomePageUiState, Assembly-CSharp");
            Type? gameDataUiState = patcher.ResolveType("DolocTown.GameDataUiState, Assembly-CSharp");
            if (dolocApi == null || homePageUiState == null || gameDataUiState == null)
                return false;

            if (!IsUiStateActive(dolocApi, homePageUiState))
            {
                waitForOfficialUi = true;
                if ((DateTimeOffset.Now - lastAutoLoadReadinessLog).TotalSeconds >= 5)
                {
                    lastAutoLoadReadinessLog = DateTimeOffset.Now;
                    runtime.RuntimeMonitor.Log("Smoke automation waiting for HomePageUiState before loading save slot " + humanSlot + ". context=" + runtime.UI.InputContext + ".");
                    runtime.SetHookStatus("Smoke.AutoLoadSave", "pending", "HomePageUiState", "Waiting for official home/save UI to become active. context=" + runtime.UI.InputContext + ".");
                }
                return false;
            }

            MethodInfo? enterUi = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "EnterUI" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            if (enterUi == null)
                return false;

            runtime.RuntimeMonitor.Log($"Smoke automation selecting save slot {humanSlot} through GameDataUiState official path.");
            object? state = enterUi.MakeGenericMethod(gameDataUiState).Invoke(null, null);
            if (state == null)
                throw new InvalidOperationException("GameDataUiState could not be entered.");
            pendingAutoLoadGameDataState = state;

            MethodInfo? select = gameDataUiState.GetMethod("OnDataSlotSelect", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo? confirm = gameDataUiState.GetMethod("OnConfirm", BindingFlags.Instance | BindingFlags.NonPublic);
            if (select == null || confirm == null)
                throw new MissingMethodException("GameDataUiState select/confirm methods were not found.");

            select.Invoke(state, new object[] { gameIndex });
            confirm.Invoke(state, null);
            autoLoadOfficialPathRequested = true;
            modChangePromptConfirmed = false;
            modChangePromptConfirmedAt = DateTimeOffset.MinValue;
            autoLoadDirectFallbackAfterModChangeAttempted = false;
            pendingAutoLoadGameIndex = gameIndex;
            runtime.SetHookStatus("Smoke.AutoLoadSave", "pending", "GameDataUiState.OnConfirm", "Requested official save UI load path for slot " + humanSlot + ".");
            return true;
        }

        private void TryConfirmModChangePrompt()
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? modChangeListUiState = patcher.ResolveType("DolocTown.ModChangeListUiState, Assembly-CSharp");
                if (dolocApi == null || modChangeListUiState == null)
                    return;

                object? state = GetExistingUiState(dolocApi, modChangeListUiState);
                if (state == null)
                    return;

                if (pendingAutoLoadGameIndex.HasValue)
                {
                    RestoreGameDataSelection(dolocApi, pendingAutoLoadGameIndex.Value);
                    WrapModChangeConfirmForFixture(dolocApi, modChangeListUiState, state, pendingAutoLoadGameIndex.Value);
                }

                MethodInfo? confirm = modChangeListUiState.GetMethod("OnConfirm", BindingFlags.Instance | BindingFlags.NonPublic);
                if (confirm == null)
                    return;

                runtime.RuntimeMonitor.Log("Smoke automation confirming ModChangeListUiState before save load.");
                confirm.Invoke(state, null);
                modChangePromptConfirmed = true;
                modChangePromptConfirmedAt = DateTimeOffset.Now;
                runtime.SetHookStatus("Smoke.AutoLoadSave", "pending", "ModChangeListUiState.OnConfirm", "Confirmed official mod-change prompt.");
            }
            catch (Exception ex)
            {
                modChangePromptConfirmed = true;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke mod-change confirmation failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AutoLoadSave", "failed", "ModChangeListUiState.OnConfirm", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void TryAutoLoadSaveDirectFallbackAfterModChange()
        {
            if (autoLoadDirectFallbackAfterModChangeAttempted || !pendingAutoLoadGameIndex.HasValue)
                return;
            if (modChangePromptConfirmedAt == DateTimeOffset.MinValue || (DateTimeOffset.Now - modChangePromptConfirmedAt).TotalSeconds < 15)
                return;

            autoLoadDirectFallbackAfterModChangeAttempted = true;
            int gameIndex = pendingAutoLoadGameIndex.Value;
            int humanSlot = gameIndex + 1;
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? loadGame = FindMethod(dolocApi, "LoadGame", 1);
                if (loadGame == null)
                    throw new MissingMethodException("DolocAPI.LoadGame(int) was not found.");

                InvokeSmokeLoadGame(
                    loadGame,
                    gameIndex,
                    "SmokeHarness.ModChangeDirectFallback",
                    "Smoke automation confirmed ModChangeListUiState but did not observe SaveLoaded within 15 seconds; falling back to direct DolocAPI.LoadGame for save slot " + humanSlot + ".",
                    "Fallback after ModChangeListUiState confirmation did not reach SaveLoaded for slot " + humanSlot + ".");
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-load direct fallback after mod-change confirmation failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AutoLoadSave", "failed", "DolocAPI.LoadGame", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void WrapModChangeConfirmForFixture(Type dolocApi, Type modChangeListUiState, object modChangeState, int gameIndex)
        {
            FieldInfo? onConfirm = modChangeListUiState.GetField("onConfirm", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(onConfirm?.GetValue(modChangeState) is Action originalConfirm))
                return;

            onConfirm.SetValue(modChangeState, new Action(() =>
            {
                RestoreGameDataSelection(dolocApi, gameIndex);
                runtime.RuntimeMonitor.Log("Smoke automation restored save slot " + (gameIndex + 1) + " immediately before official load confirm.");
                originalConfirm();
            }));
            runtime.RuntimeMonitor.Log("Smoke automation wrapped ModChangeListUiState confirmation for save slot " + (gameIndex + 1) + ".");
        }

        private void RestoreGameDataSelection(Type dolocApi, int gameIndex)
        {
            Type? gameDataUiState = patcher?.ResolveType("DolocTown.GameDataUiState, Assembly-CSharp");
            if (gameDataUiState == null)
                return;

            object? gameDataState = pendingAutoLoadGameDataState;
            if (gameDataState == null || !gameDataUiState.IsInstanceOfType(gameDataState))
                gameDataState = GetExistingUiState(dolocApi, gameDataUiState);
            if (gameDataState == null)
            {
                runtime.RuntimeMonitor.Log("Smoke automation could not restore GameDataUiState selection; state was not cached.");
                return;
            }

            FieldInfo? currentIndex = gameDataUiState.GetField("currentIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            currentIndex?.SetValue(gameDataState, gameIndex);

            MethodInfo? select = gameDataUiState.GetMethod("OnDataSlotSelect", BindingFlags.Instance | BindingFlags.NonPublic);
            select?.Invoke(gameDataState, new object[] { gameIndex });
            runtime.RuntimeMonitor.Log("Smoke automation restored GameDataUiState selection to save slot " + (gameIndex + 1) + ".");
        }

        private void InvokeSmokeLoadGame(MethodInfo loadGame, int gameIndex, string source, string logMessage, string hookDetails)
        {
            if (runtime.ShouldSuppressDtmapiLoadGameRequest(gameIndex, "DTMAPI.Smoke", source, out string coordinatorSummary))
            {
                runtime.SetHookStatus("Smoke.AutoLoadSave", "pending", "SaveLoadRequestCoordinator", "Suppressed duplicate smoke LoadGame for slot " + (gameIndex + 1) + ". " + coordinatorSummary);
                return;
            }

            runtime.RuntimeMonitor.Log(logMessage);
            runtime.SetHookStatus("Smoke.AutoLoadSave", "pending", "DolocAPI.LoadGame", hookDetails);
            loadGame.Invoke(null, new object[] { gameIndex });
        }

        private enum FixtureAttemptResult
        {
            Pending,
            Failed,
            Succeeded
        }

        private sealed class G6FixtureState
        {
            public int AutoLoadSaveSlot { get; set; }
            public int AutoLoadDelaySeconds { get; set; } = 3;
            public bool AutoExerciseSaveLoadCycle { get; set; }
            public int SaveLoadCycleCount { get; set; }
            public int SaveLoadCycleInitialTitleIdleSeconds { get; set; }
            public int SaveLoadCycleIntervalSeconds { get; set; } = 5;
            public int SaveLoadCycleInSaveSeconds { get; set; } = 5;
            public bool AutoExercisePreLoadGcProbe { get; set; }
            public string SaveLoadObjectSnapshotMode { get; set; } = "Full";
            public bool AutoExerciseSaveLoadCyclePendingPressure { get; set; }
            public int SaveLoadCyclePendingPressureSeconds { get; set; }
            public double SaveLoadCyclePendingPressureIntervalSeconds { get; set; } = 2d;
        }

    }
}
