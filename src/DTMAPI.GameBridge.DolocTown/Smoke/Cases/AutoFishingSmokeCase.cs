using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private static readonly ConditionalWeakTable<DolocTownGameBridge, AutoFishingSmokeFlowProgress> AutoFishingSmokeFlows = new ConditionalWeakTable<DolocTownGameBridge, AutoFishingSmokeFlowProgress>();

        private bool TryExerciseAutoFishingAutoCastForSmoke(Type dolocApi, FishingAutomationService fishingService, AutoFishingSmokeFlowProgress progress, out string summary)
        {
            summary = string.Empty;

            try
            {
                fishingService.SuppressFishingAutoCastForSmoke = false;
                object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
                if (selectedItem == null || !IsTypeOrBase(selectedItem.GetType(), "DolocTown.ItemFishingRod"))
                {
                    summary = "real fifth-save fixture missing selected fishing rod. selected=" + (selectedItem == null ? "null" : selectedItem.GetType().FullName) + ".";
                    return false;
                }

                string poolSummary = DescribeActiveFishingPoolsForSmoke();
                int afterAutoCast = fishingService.FishingAutoCastApplicationCount;
                for (int attempt = 1; attempt <= 40; attempt++)
                {
                    UpdateRuntimeAutomation();
                    afterAutoCast = fishingService.FishingAutoCastApplicationCount;
                    CaptureAutoFishingFlowProgress(fishingService, progress);
                    if (afterAutoCast > progress.InitialAutoCastCount)
                        break;
                    Thread.Sleep(125);
                }
                string bridgeSummary = fishingService.LastFishingAutomationApplicationSummary;
                string attemptSummary = fishingService.LastFishingAutoCastAttemptSummary;
                string currentState = ReadCurrentAgentStateForSmoke(dolocApi);
                bool stateAlreadyAdvanced = IsFishingStateAtOrAfterAutoCast(currentState);
                bool summaryObservedAutoCast = bridgeSummary.IndexOf("AutoCast", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    attemptSummary.IndexOf("AutoCast", StringComparison.OrdinalIgnoreCase) >= 0;
                bool invoked = afterAutoCast > progress.InitialAutoCastCount ||
                    (afterAutoCast > 0 && stateAlreadyAdvanced);
                if (!invoked)
                {
                    summary = "fixture={" + poolSummary + "}, selectedRod=" + ReadStringMember(selectedItem, "name", selectedItem.GetType().Name) +
                        ", autoCastDelta=" + (afterAutoCast - progress.InitialAutoCastCount) +
                        ", currentState=" + currentState +
                        ", alreadyAdvanced=" + stateAlreadyAdvanced +
                        ", summaryObservedAutoCast=" + summaryObservedAutoCast +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary) +
                        ", lastAttempt=" + (string.IsNullOrWhiteSpace(attemptSummary) ? "none" : attemptSummary);
                    return false;
                }

                progress.AutoCastObserved = true;
                summary = "fixture={" + poolSummary + "}, selectedRod=" + ReadStringMember(selectedItem, "name", selectedItem.GetType().Name) +
                    ", autoCastDelta=" + (afterAutoCast - progress.InitialAutoCastCount) +
                    ", currentState=" + currentState +
                    ", alreadyAdvanced=" + stateAlreadyAdvanced +
                    ", summaryObservedAutoCast=" + summaryObservedAutoCast +
                    ", bridge=" + bridgeSummary;
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-fishing auto-cast exercise failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-fishing auto-cast exercise failed.", ex.ToString());
                return false;
            }
            finally
            {
                fishingService.SuppressFishingAutoCastForSmoke = false;
            }
        }

        private static bool IsFishingStateAtOrAfterAutoCast(string currentState)
        {
            return currentState.IndexOf("AgentStateFishingCast", StringComparison.OrdinalIgnoreCase) >= 0 ||
                currentState.IndexOf("AgentStateFishingWait", StringComparison.OrdinalIgnoreCase) >= 0 ||
                currentState.IndexOf("AgentStateFishingBattle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                currentState.IndexOf("AgentStateFishingPull", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private SmokeAttemptResult TryExerciseAutoFishingPhaseForSmoke()
        {
            FishingAutomationService? fishingService = FishingAutomationService;
            try
            {
                if (fishingService == null)
                    throw new InvalidOperationException("FishingAutomation service was not registered.");

                string scenario = GetAutoFishingScenarioForSmoke();
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new MissingMemberException("DolocAPI was not visible.");

                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    LogAutoFishingPending("Waiting for NormalGameState before auto-fishing smoke. context=" + runtime.UI.InputContext + ".");
                    return SmokeAttemptResult.Pending;
                }

                if (!autoFishingHotkeyInjected)
                {
                    if (fishingService.TryGetEnabledFishingAutomationOwner(out string externallyEnabledOwner))
                    {
                        autoFishingHotkeyInjected = true;
                        runtime.RuntimeMonitor.Log("Smoke automation observed AutoFishing already enabled before synthetic input; treating F6 input chain as external/real path. owner=" + externallyEnabledOwner + ".");
                        runtime.SetHookStatus("Smoke.AutoFishingHotkey", "verified", "Unity input polling -> DTMAPI input event", "AutoFishing enabled by registered hotkey before bridge fallback; owner=" + externallyEnabledOwner + ".");
                    }
                    else if (smokeSettings?.AutoFishingExternalHotkeyRequired == true)
                    {
                        LogAutoFishingPending("Waiting for external F6 keypress to enable AutoFishing. context=" + runtime.UI.InputContext + ", menuOpen=" + runtime.UI.IsOpen + ".");
                        return SmokeAttemptResult.Pending;
                    }
                    else if (runtime.UI.BlocksGameplayHotkeys)
                    {
                        LogAutoFishingPending("Waiting for gameplay hotkeys before synthetic F6 toggle. context=" + runtime.UI.InputContext + ", menuOpen=" + runtime.UI.IsOpen + ".");
                        return SmokeAttemptResult.Pending;
                    }
                    else
                    {
                        runtime.RecordInputPressed("F6");
                        runtime.RecordInputReleased("F6");
                        autoFishingHotkeyInjected = true;
                        runtime.RuntimeMonitor.Log("Smoke automation dispatched AutoFishing toggle key F6 through DTMAPI input service.");
                        runtime.SetHookStatus("Smoke.AutoFishingHotkey", "verified", "DtmApiRuntime.RecordInputPressed", "Dispatched F6 through the same registered input event path used by gameplay hotkeys.");
                    }
                }

                if (!fishingService.TryGetEnabledFishingAutomationOwner(out string ownerId))
                    throw new InvalidOperationException("AutoFishing policy was not enabled after the F6 input dispatch. Is the official AutoFishing package enabled?");

                AutoFishingSmokeFlowProgress progress = AutoFishingSmokeFlows.GetValue(this, _ => new AutoFishingSmokeFlowProgress());
                progress.SetOwner(ownerId);
                if (!autoExerciseAutoFishingPhaseStarted)
                {
                    autoFishingApplicationBaseline = fishingService.FishingAutomationApplicationCount;
                    autoFishingMiniGameCompleteBaseline = fishingService.FishingMiniGameCompleteApplicationCount;
                    autoFishingPhaseStartedAt = DateTimeOffset.Now;
                    autoExerciseAutoFishingPhaseStarted = true;
                    progress.Reset(scenario, fishingService.FishingAutoCastApplicationCount, autoFishingApplicationBaseline, autoFishingMiniGameCompleteBaseline, fishingService.FishingInstantBiteApplicationCount, fishingService.FishingSkipMiniGameApplicationCount, fishingService.FishingReadyChargeApplicationCount, DateTimeOffset.Now);
                    progress.SetOwner(ownerId);
                    runtime.RuntimeMonitor.Log("Smoke automation starting real AutoFishing loop evidence. owner=" + ownerId + ", scenario=" + scenario + ", currentState=" + ReadCurrentAgentStateForSmoke(dolocApi) + ", fixture=" + DescribeActiveFishingPoolsForSmoke() + ".");
                    runtime.SetHookStatus("Smoke.AutoFishingPhase", "pending", "native fishing loop", "Using the real fifth-save fixture; waiting for AutoCast -> Wait -> BiteReady -> Battle/Pull -> PullExit -> next AutoCast scenario=" + scenario + ".");
                }
                else if (!progress.Scenario.Equals(scenario, StringComparison.OrdinalIgnoreCase))
                {
                    progress.Reset(scenario, fishingService.FishingAutoCastApplicationCount, fishingService.FishingAutomationApplicationCount, fishingService.FishingMiniGameCompleteApplicationCount, fishingService.FishingInstantBiteApplicationCount, fishingService.FishingSkipMiniGameApplicationCount, fishingService.FishingReadyChargeApplicationCount, DateTimeOffset.Now);
                    progress.SetOwner(ownerId);
                    autoFishingPhaseStartedAt = DateTimeOffset.Now;
                }

                CaptureAutoFishingFlowProgress(fishingService, progress);

                if (!autoExerciseAutoFishingAutoCastVerified)
                {
                    if (!TryExerciseAutoFishingAutoCastForSmoke(dolocApi, fishingService, progress, out string autoCastSummary))
                    {
                        if ((DateTimeOffset.Now - autoFishingPhaseStartedAt).TotalSeconds > 12)
                            throw new InvalidOperationException("AutoFishing real auto-cast path failed on fifth-save fixture. " + autoCastSummary);
                        LogAutoFishingPending("Waiting for real BodyController.UseFishRod auto-cast. " + autoCastSummary);
                        return SmokeAttemptResult.Pending;
                    }
                    autoExerciseAutoFishingAutoCastVerified = true;
                    runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingAutoCast OK " + autoCastSummary);
                    runtime.SetHookStatus("Smoke.AutoFishingAutoCast", "verified", "BodyController.UseFishRod", autoCastSummary);
                    return SmokeAttemptResult.Pending;
                }

                CaptureAutoFishingFlowProgress(fishingService, progress);

                if (progress.BiteReadyObserved && !autoExerciseAutoFishingPhaseVerified)
                {
                    string summary = fishingService.LastFishingAutomationApplicationSummary;
                    autoExerciseAutoFishingPhaseVerified = true;
                    runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingPhase OK " + BuildAutoFishingFlowSummary(progress, summary));
                    runtime.SetHookStatus("Smoke.AutoFishingPhase", "verified", "native fishing loop", BuildAutoFishingFlowSummary(progress, summary));
                    if (ScenarioRequestsInstantBite(scenario))
                        runtime.SetHookStatus("Smoke.AutoFishingInstantBite", "verified", "AgentStateFishingWait.OnPlay Postfix", summary);
                }

                if (ScenarioRequestsCompletion(scenario) && fishingService.FishingMiniGameCompleteApplicationCount > autoFishingMiniGameCompleteBaseline && !progress.MiniGameCompleteObserved)
                {
                    progress.MiniGameCompleteObserved = true;
                    string completeSummary = string.IsNullOrWhiteSpace(fishingService.LastFishingMiniGameCompleteSummary)
                        ? fishingService.LastFishingAutomationApplicationSummary
                        : fishingService.LastFishingMiniGameCompleteSummary;
                    runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingMiniGameComplete OK " + completeSummary);
                    runtime.SetHookStatus("Smoke.AutoFishingMiniGameComplete", "verified", "FishingGameScrollBar.UpdateGame Postfix", completeSummary);
                }

                if (ScenarioRequestsAnimationSpeed(scenario) && progress.ReadyChargeObserved && progress.AnimationSpeedObserved)
                {
                    runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingFastAnimations OK " + fishingService.LastFishingAnimationSpeedSummary);
                    runtime.SetHookStatus("Smoke.AutoFishingAnimationSpeed", "verified", "AgentStateFishingReady/Cast/Pull", BuildAutoFishingFlowSummary(progress, fishingService.LastFishingAnimationSpeedSummary));
                }

                if (IsAutoFishingFlowComplete(progress, fishingService))
                {
                    VerifyRequiredAutoFishingScenarioEvidence(scenario, progress, fishingService);
                    VerifyAutoFishingReportExportForSmoke(scenario);
                    return SmokeAttemptResult.Succeeded;
                }

                double elapsed = (DateTimeOffset.Now - autoFishingPhaseStartedAt).TotalSeconds;
                if (elapsed > 60)
                    throw new TimeoutException("AutoFishing real fifth-save loop did not complete within 60 seconds. " + BuildAutoFishingFlowSummary(progress, fishingService.LastFishingAutomationApplicationSummary));

                LogAutoFishingPending("Waiting for real fishing loop scenario=" + scenario + ". " + BuildAutoFishingFlowSummary(progress, fishingService.LastFishingAutomationApplicationSummary));
                return SmokeAttemptResult.Pending;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-fishing phase exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AutoFishingPhase", "failed", "native fishing loop", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private string GetAutoFishingScenarioForSmoke()
        {
            string scenario = smokeSettings?.AutoFishingScenario ?? string.Empty;
            if (string.IsNullOrWhiteSpace(scenario))
                return smokeSettings?.AutoExerciseAutoFishingMiniGameComplete == true ? "CombinedInstantComplete" : "DefaultLoop";
            return scenario.Trim();
        }

        private static bool ScenarioRequestsInstantBite(string scenario)
        {
            return scenario.Equals("InstantBite", StringComparison.OrdinalIgnoreCase) ||
                scenario.Equals("CombinedInstantSkip", StringComparison.OrdinalIgnoreCase) ||
                scenario.Equals("CombinedInstantComplete", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ScenarioRequestsSkip(string scenario)
        {
            return scenario.Equals("SkipMiniGame", StringComparison.OrdinalIgnoreCase) ||
                scenario.Equals("CombinedInstantSkip", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ScenarioRequestsCompletion(string scenario)
        {
            return scenario.Equals("DefaultLoop", StringComparison.OrdinalIgnoreCase) ||
                scenario.Equals("InstantBite", StringComparison.OrdinalIgnoreCase) ||
                scenario.Equals("FastAnimations", StringComparison.OrdinalIgnoreCase) ||
                scenario.Equals("CombinedInstantComplete", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ScenarioRequestsAnimationSpeed(string scenario)
        {
            return scenario.Equals("FastAnimations", StringComparison.OrdinalIgnoreCase) ||
                scenario.Equals("CombinedInstantSkip", StringComparison.OrdinalIgnoreCase) ||
                scenario.Equals("CombinedInstantComplete", StringComparison.OrdinalIgnoreCase);
        }

        private void CaptureAutoFishingFlowProgress(FishingAutomationService fishingService, AutoFishingSmokeFlowProgress progress)
        {
            FishingAutomationState state = string.IsNullOrWhiteSpace(progress.OwnerId)
                ? new FishingAutomationState()
                : fishingService.GetState(progress.OwnerId);
            string phase = state.Phase ?? string.Empty;
            string lastSummary = fishingService.LastFishingAutomationApplicationSummary ?? string.Empty;
            string animationSummary = fishingService.LastFishingAnimationSpeedSummary ?? string.Empty;

            if (phase.IndexOf("Wait", StringComparison.OrdinalIgnoreCase) >= 0 || phase.Equals("WaitingForBite", StringComparison.OrdinalIgnoreCase))
                progress.WaitObserved = true;

            if (lastSummary.IndexOf("behavior=NativeBiteReady", StringComparison.OrdinalIgnoreCase) >= 0 ||
                lastSummary.IndexOf("behavior=InstantBite", StringComparison.OrdinalIgnoreCase) >= 0)
                progress.BiteReadyObserved = true;

            if (lastSummary.IndexOf("behavior=InstantBite", StringComparison.OrdinalIgnoreCase) >= 0)
                progress.InstantBiteObserved = true;

            if (fishingService.FishingInstantBiteApplicationCount > progress.InitialInstantBiteCount)
                progress.InstantBiteObserved = true;

            bool observedBattleOrPull = phase.IndexOf("Battle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                phase.IndexOf("Pull", StringComparison.OrdinalIgnoreCase) >= 0 ||
                phase.IndexOf("MiniGame", StringComparison.OrdinalIgnoreCase) >= 0 ||
                lastSummary.IndexOf("autoHook=AgentStateFishingBattle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                lastSummary.IndexOf("autoHook=AgentStateFishingPull", StringComparison.OrdinalIgnoreCase) >= 0;
            if (observedBattleOrPull)
            {
                progress.BattleOrPullObserved = true;
                if (progress.BattleOrPullAutoCastCount < 0)
                    progress.BattleOrPullAutoCastCount = fishingService.FishingAutoCastApplicationCount;
            }

            if (lastSummary.IndexOf("action=SkipMiniGameNativeResult", StringComparison.OrdinalIgnoreCase) >= 0 &&
                lastSummary.IndexOf("autoHook=AgentStateFishingPull", StringComparison.OrdinalIgnoreCase) >= 0)
                progress.SkipObserved = true;

            if (fishingService.FishingSkipMiniGameApplicationCount > progress.InitialSkipMiniGameCount)
                progress.SkipObserved = true;

            if (phase.Equals("Cooldown", StringComparison.OrdinalIgnoreCase) || phase.IndexOf("Cooldown", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                progress.PullExitObserved = true;
                if (progress.PullExitAutoCastCount < 0)
                    progress.PullExitAutoCastCount = fishingService.FishingAutoCastApplicationCount;
            }

            if (progress.PullExitObserved && progress.PullExitAutoCastCount >= 0 &&
                fishingService.FishingAutoCastApplicationCount > progress.PullExitAutoCastCount)
                progress.NextAutoCastObserved = true;

            if (!progress.PullExitObserved && progress.BattleOrPullObserved && progress.BattleOrPullAutoCastCount >= 0 &&
                fishingService.FishingAutoCastApplicationCount > progress.BattleOrPullAutoCastCount)
            {
                progress.PullExitObserved = true;
                progress.PullExitAutoCastCount = progress.BattleOrPullAutoCastCount;
                progress.NextAutoCastObserved = true;
            }

            if (IsFastAnimationOwnerEvidence(animationSummary))
                progress.AnimationSpeedObserved = true;
            if (IsFastReadyChargeEvidence(animationSummary))
                progress.ReadyChargeObserved = true;
            if (fishingService.FishingReadyChargeApplicationCount > progress.InitialReadyChargeCount)
                progress.ReadyChargeObserved = true;

            progress.LastPhase = phase;
            progress.LastSummary = lastSummary;
            progress.LastReason = state.LastReason ?? string.Empty;
        }

        private static bool IsAutoFishingFlowComplete(AutoFishingSmokeFlowProgress progress, FishingAutomationService fishingService)
        {
            return progress.AutoCastObserved &&
                progress.WaitObserved &&
                progress.BiteReadyObserved &&
                progress.BattleOrPullObserved &&
                progress.PullExitObserved &&
                progress.NextAutoCastObserved &&
                fishingService.FishingAutoCastApplicationCount >= progress.InitialAutoCastCount + 2;
        }

        private static bool IsFastAnimationOwnerEvidence(string animationSummary)
        {
            if (string.IsNullOrWhiteSpace(animationSummary) ||
                animationSummary.IndexOf("status=pending", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return animationSummary.IndexOf("behavior=FastCastHookPhysics", StringComparison.OrdinalIgnoreCase) >= 0 ||
                animationSummary.IndexOf("behavior=FastPullDuration", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsFastReadyChargeEvidence(string animationSummary)
        {
            if (string.IsNullOrWhiteSpace(animationSummary) ||
                animationSummary.IndexOf("status=pending", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            return animationSummary.IndexOf("behavior=FastReadyCharge", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void VerifyRequiredAutoFishingScenarioEvidence(string scenario, AutoFishingSmokeFlowProgress progress, FishingAutomationService fishingService)
        {
            string summary = BuildAutoFishingFlowSummary(progress, fishingService.LastFishingAutomationApplicationSummary);
            if (ScenarioRequestsInstantBite(scenario) && !progress.InstantBiteObserved)
                throw new InvalidOperationException("Scenario " + scenario + " did not observe InstantBite evidence. " + summary);
            if (ScenarioRequestsSkip(scenario) && !progress.SkipObserved)
                throw new InvalidOperationException("Scenario " + scenario + " did not observe SkipMiniGame pull evidence. " + summary);
            if (ScenarioRequestsCompletion(scenario) && !progress.MiniGameCompleteObserved)
                throw new InvalidOperationException("Scenario " + scenario + " did not observe visible minigame auto-complete evidence. " + summary);
            if (ScenarioRequestsAnimationSpeed(scenario) && (!progress.ReadyChargeObserved || !progress.AnimationSpeedObserved))
                throw new InvalidOperationException("Scenario " + scenario + " did not observe ready-charge plus cast/pull fast animation evidence. " + summary);

            runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingLoop OK " + summary);
            runtime.SetHookStatus("Smoke.AutoFishingPhase", "verified", "native fishing loop", summary);
        }

        private string BuildAutoFishingFlowSummary(AutoFishingSmokeFlowProgress progress, string fallbackSummary)
        {
            return "scenario=" + progress.Scenario +
                ", flow=AutoCast:" + progress.AutoCastObserved +
                "->Wait:" + progress.WaitObserved +
                "->BiteReady:" + progress.BiteReadyObserved +
                "->BattleOrPull:" + progress.BattleOrPullObserved +
                "->PullExit:" + progress.PullExitObserved +
                "->NextAutoCast:" + progress.NextAutoCastObserved +
                ", instantBite=" + progress.InstantBiteObserved +
                ", skip=" + progress.SkipObserved +
                ", readyCharge=" + progress.ReadyChargeObserved +
                ", fastAnimation=" + progress.AnimationSpeedObserved +
                ", autoCast=" + progress.InitialAutoCastCount + "->" + (FishingAutomationService?.FishingAutoCastApplicationCount ?? -1) +
                ", applications=" + progress.InitialApplicationCount + "->" + (FishingAutomationService?.FishingAutomationApplicationCount ?? -1) +
                ", readyCharges=" + progress.InitialReadyChargeCount + "->" + (FishingAutomationService?.FishingReadyChargeApplicationCount ?? -1) +
                ", miniGameComplete=" + progress.InitialMiniGameCompleteCount + "->" + (FishingAutomationService?.FishingMiniGameCompleteApplicationCount ?? -1) +
                ", phase=" + GameBridgeNativeHelpers.FirstText(progress.LastPhase, "unknown") +
                ", reason=" + GameBridgeNativeHelpers.FirstText(progress.LastReason, "-") +
                ", last=" + GameBridgeNativeHelpers.FirstText(progress.LastSummary, fallbackSummary, "none");
        }

        private string DescribeActiveFishingPoolsForSmoke()
        {
            Type? fishingPoolType = patcher?.ResolveType("DolocTown.FishingPool, Assembly-CSharp");
            if (fishingPoolType == null)
                return "fishingPoolType=missing";

            object[] activePools = FindUnityObjects(fishingPoolType);
            List<string> names = new List<string>();
            foreach (object pool in activePools.Take(6))
                names.Add(GameBridgeNativeHelpers.FirstText(ReadStringMember(pool, "PoolName", string.Empty), pool.GetType().Name));

            return "activeScenePools=" + activePools.Length + (names.Count == 0 ? string.Empty : ", sample=" + string.Join("|", names));
        }

        private sealed class AutoFishingSmokeFlowProgress
        {
            internal string Scenario { get; private set; } = string.Empty;
            internal string OwnerId { get; private set; } = string.Empty;
            internal int InitialAutoCastCount { get; private set; }
            internal int InitialApplicationCount { get; private set; }
            internal int InitialMiniGameCompleteCount { get; private set; }
            internal int InitialInstantBiteCount { get; private set; }
            internal int InitialSkipMiniGameCount { get; private set; }
            internal int InitialReadyChargeCount { get; private set; }
            internal DateTimeOffset StartedAt { get; private set; }
            internal bool AutoCastObserved { get; set; }
            internal bool WaitObserved { get; set; }
            internal bool BiteReadyObserved { get; set; }
            internal bool InstantBiteObserved { get; set; }
            internal bool BattleOrPullObserved { get; set; }
            internal bool PullExitObserved { get; set; }
            internal bool NextAutoCastObserved { get; set; }
            internal bool SkipObserved { get; set; }
            internal bool MiniGameCompleteObserved { get; set; }
            internal bool ReadyChargeObserved { get; set; }
            internal bool AnimationSpeedObserved { get; set; }
            internal int BattleOrPullAutoCastCount { get; set; } = -1;
            internal int PullExitAutoCastCount { get; set; } = -1;
            internal string LastPhase { get; set; } = string.Empty;
            internal string LastReason { get; set; } = string.Empty;
            internal string LastSummary { get; set; } = string.Empty;

            internal void Reset(string scenario, int initialAutoCastCount, int initialApplicationCount, int initialMiniGameCompleteCount, int initialInstantBiteCount, int initialSkipMiniGameCount, int initialReadyChargeCount, DateTimeOffset startedAt)
            {
                Scenario = scenario;
                OwnerId = string.Empty;
                InitialAutoCastCount = initialAutoCastCount;
                InitialApplicationCount = initialApplicationCount;
                InitialMiniGameCompleteCount = initialMiniGameCompleteCount;
                InitialInstantBiteCount = initialInstantBiteCount;
                InitialSkipMiniGameCount = initialSkipMiniGameCount;
                InitialReadyChargeCount = initialReadyChargeCount;
                StartedAt = startedAt;
                AutoCastObserved = false;
                WaitObserved = false;
                BiteReadyObserved = false;
                InstantBiteObserved = false;
                BattleOrPullObserved = false;
                PullExitObserved = false;
                NextAutoCastObserved = false;
                SkipObserved = false;
                MiniGameCompleteObserved = false;
                ReadyChargeObserved = false;
                AnimationSpeedObserved = false;
                BattleOrPullAutoCastCount = -1;
                PullExitAutoCastCount = -1;
                LastPhase = string.Empty;
                LastReason = string.Empty;
                LastSummary = string.Empty;
            }

            internal void SetOwner(string ownerId)
            {
                OwnerId = ownerId ?? string.Empty;
            }
        }

        private void LogAutoFishingPending(string message)
        {
            if ((DateTimeOffset.Now - lastAutoFishingReadinessLog).TotalSeconds < 5)
                return;
            lastAutoFishingReadinessLog = DateTimeOffset.Now;
            runtime.RuntimeMonitor.Log("Smoke auto-fishing waiting: " + message);
            runtime.SetHookStatus("Smoke.AutoFishingPhase", "pending", "native fishing loop", message);
        }

        private void VerifyAutoFishingReportExportForSmoke(string scenario)
        {
            if (autoFishingReportExported)
                return;

            if (!TryVerifyDiagnosticsSnapshotForSmoke("AutoFishing " + scenario + " report export", "FishingAutomation"))
                throw new InvalidOperationException("AutoFishing smoke report export verification failed for " + scenario + ".");

            autoFishingReportExported = true;
        }

    }
}
