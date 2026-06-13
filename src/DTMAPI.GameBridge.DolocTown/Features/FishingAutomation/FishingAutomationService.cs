using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class FishingAutomationService : IFishingAutomationApi
    {
        private const int FishingAutomationFailureShortLogLimit = 3;
        private static readonly TimeSpan FishingAutomationFailureSummaryInterval = TimeSpan.FromSeconds(30);

        private enum FishingBiteAutomationAction
        {
            None,
            NativeReel,
            SkipMiniGameNativeResult
        }

        private enum FishingMiniGameInputDecision
        {
            Release,
            HoldStable,
            TapBonus
        }

        private readonly DtmApiRuntime runtime;
        private readonly Dictionary<string, FishingAutomationOptions> fishingOptions = new Dictionary<string, FishingAutomationOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FishingAutomationState> fishingStates = new Dictionary<string, FishingAutomationState>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedFishingPhases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<object, DateTimeOffset> fishingMiniGameStartedAt = new Dictionary<object, DateTimeOffset>();
        private readonly Dictionary<object, HashSet<int>> fishingMiniGameBonusTaps = new Dictionary<object, HashSet<int>>();
        private readonly Dictionary<object, FishingMiniGameInputStats> fishingMiniGameInputStats = new Dictionary<object, FishingMiniGameInputStats>();
        private readonly HashSet<object> fishingMiniGameCompletedHandles = new HashSet<object>();
        private readonly HashSet<object> fishingReadyChargeAppliedStates = new HashSet<object>();
        private readonly HashSet<object> fishingReadyChargeTargetStates = new HashSet<object>();
        private readonly HashSet<object> fishingReadyChargeReleasedStates = new HashSet<object>();
        private readonly Dictionary<object, double> originalAnimatorSpeeds = new Dictionary<object, double>();
        private readonly Dictionary<object, double> originalHookGravityScales = new Dictionary<object, double>();
        private readonly Dictionary<string, FishingAutomationFailureState> fishingAutomationFailures = new Dictionary<string, FishingAutomationFailureState>(StringComparer.Ordinal);
        private FishingMiniGameInputOverride? fishingMiniGameInputOverride;
        private object? currentFishingReadyChargeState;
        private DateTimeOffset lastFishingPullDurationResultScaleAtUtc = DateTimeOffset.MinValue;
        private bool fishingHooksInstalled;
        private DateTimeOffset lastFishingAutoCastAt = DateTimeOffset.MinValue;
        private DateTimeOffset lastFishingFeedbackAt = DateTimeOffset.MinValue;
        private int fishingAutoCastApplications;

        internal FishingAutomationService(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        internal int FishingAutomationApplicationCount { get; private set; }

        internal string LastFishingAutomationApplicationSummary { get; private set; } = string.Empty;

        internal string LastFishingMiniGameCompleteSummary { get; private set; } = string.Empty;

        internal string LastFishingAnimationSpeedSummary { get; private set; } = string.Empty;

        internal int FishingAutoCastApplicationCount => fishingAutoCastApplications;

        internal int FishingInstantBiteApplicationCount { get; private set; }

        internal int FishingSkipMiniGameApplicationCount { get; private set; }

        internal int FishingReadyChargeApplicationCount { get; private set; }

        internal int FishingReadyChargeTargetApplicationCount { get; private set; }

        internal string LastFishingAutoCastAttemptSummary { get; private set; } = string.Empty;

        internal int FishingMiniGameCompleteApplicationCount { get; private set; }

        internal bool SuppressFishingAutoCastForSmoke { get; set; }

        internal bool ForceFishingNoWaterForSmoke { get; set; }

        internal bool ForceFishingNoRodForSmoke { get; set; }

        internal object? FishingPoolOverrideForSmoke { get; set; }

        internal bool ForceFishingFishForSmoke { get; set; }

        internal bool ForceFishingNativeBiteForSmoke { get; set; }

        internal void Update()
        {
            UpdateFishingAutoCast();
        }

        internal void ResetFishingFeedbackCooldownForSmoke()
        {
            lastFishingFeedbackAt = DateTimeOffset.MinValue;
        }

        internal void SetFishingHooksInstalled(bool installed)
        {
            fishingHooksInstalled = installed;
        }

        public void Configure(IManifest owner, FishingAutomationOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            fishingOptions[owner.UniqueID] = NormalizeFishingAutomationOptions(options);
            if (!fishingStates.ContainsKey(owner.UniqueID))
                fishingStates[owner.UniqueID] = new FishingAutomationState();
            runtime.RuntimeMonitor.Log("Fishing automation bridge configured by " + owner.UniqueID + ".");
        }

        public void SetEnabled(IManifest owner, bool enabled, string reason)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (!fishingStates.TryGetValue(owner.UniqueID, out FishingAutomationState state))
            {
                state = new FishingAutomationState();
                fishingStates[owner.UniqueID] = state;
            }
            state.Enabled = enabled;
            state.Phase = enabled ? "Starting" : "Idle";
            state.LastReason = reason ?? string.Empty;
            runtime.RuntimeMonitor.Log("Fishing automation state " + owner.UniqueID + " enabled=" + enabled + " reason=" + state.LastReason);
            if (ShouldShowFishingToggleFeedback(enabled, state.LastReason))
                ShowNativeSmallMessage(FormatFishingToggleFeedback(enabled, state.LastReason), error: false);
        }

        public FishingAutomationState GetState(string uniqueId)
        {
            return fishingStates.TryGetValue(uniqueId ?? string.Empty, out FishingAutomationState state)
                ? state
                : new FishingAutomationState();
        }

        private static bool ShouldShowFishingToggleFeedback(bool enabled, string reason)
        {
            reason = reason ?? string.Empty;
            return reason.StartsWith("hotkey ", StringComparison.OrdinalIgnoreCase) ||
                (!enabled && reason.StartsWith("manual-move ", StringComparison.OrdinalIgnoreCase));
        }

        private static string FormatFishingToggleFeedback(bool enabled, string reason)
        {
            reason = reason ?? string.Empty;
            if (!enabled && reason.StartsWith("manual-move ", StringComparison.OrdinalIgnoreCase))
                return "自动钓鱼：移动取消 / Auto fishing canceled by movement";
            return enabled ? "自动钓鱼：开启 / Auto fishing on" : "自动钓鱼：关闭 / Auto fishing off";
        }

        BridgeFeatureStatus IFishingAutomationApi.GetStatus(string uniqueId)
        {
            return fishingOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(fishingHooksInstalled ? "configured-experimental-hook" : "configured-pending-hook", fishingHooksInstalled ? "Native-loop policy accepted and fishing phase hooks are installed; automatic reel, visible minigame completion, native skip-result routing, and cast/pull animation speed remain experimental." : "Policy accepted; fishing phase and input hooks are not yet installed.")
                : new BridgeFeatureStatus("not-configured", "No fishing automation policy was registered for this mod.");
        }

        internal bool TryGetEnabledFishingAutomationOwner(out string ownerId)
        {
            ownerId = string.Empty;
            foreach (KeyValuePair<string, FishingAutomationState> entry in fishingStates)
            {
                if (entry.Value?.Enabled == true && fishingOptions.ContainsKey(entry.Key))
                {
                    ownerId = entry.Key;
                    return true;
                }
            }
            return false;
        }

        private void UpdateFishingAutoCast()
        {
            double recastDelay = 0.5;
            if (TryFindEnabledFishingOptions(out _, out FishingAutomationOptions throttleOptions, out _))
                recastDelay = Math.Max(0.05, throttleOptions.RecastDelaySeconds);
            if ((DateTimeOffset.Now - lastFishingAutoCastAt).TotalSeconds < recastDelay)
            {
                LastFishingAutoCastAttemptSummary = "skipped=throttle";
                return;
            }

            if (!TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
            {
                LastFishingAutoCastAttemptSummary = "skipped=no-enabled-policy";
                return;
            }

            if (SuppressFishingAutoCastForSmoke && !ForceFishingNoWaterForSmoke && !ForceFishingNoRodForSmoke)
            {
                LastFishingAutoCastAttemptSummary = "skipped=smoke-suppressed";
                return;
            }

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (!ReadStaticBoolMember(dolocApi, "IsNormalState", false))
            {
                state.Phase = "WaitingNormalState";
                state.LastReason = "auto:game-state";
                state.NativeOwner = "DolocAPI.IsNormalState";
                state.LastNativeAction = "wait-normal";
                LastFishingAutoCastAttemptSummary = "skipped=not-normal-state owner=" + ownerId;
                return;
            }

            object? agent = ReadStaticMember(dolocApi, "agent");
            if (agent != null && !ReadBoolMember(agent, "IsCurrentStateSupportUseItem", true))
            {
                state.Phase = "WaitingAction";
                state.LastReason = "auto:busy";
                state.NativeOwner = "BodyController.IsCurrentStateSupportUseItem";
                state.LastNativeAction = "wait-action";
                LastFishingAutoCastAttemptSummary = "skipped=busy owner=" + ownerId + ", state=" + DescribeAgentState(dolocApi);
                return;
            }

            bool forcedNoWater = ForceFishingNoWaterForSmoke;
            object? smokeFishingPool = FishingPoolOverrideForSmoke;
            if (forcedNoWater || (smokeFishingPool == null && !HasFishingPoolInScene()))
            {
                state.Phase = "NoWater";
                state.LastReason = forcedNoWater ? "smoke:no-fishing-pool" : "auto:no-fishing-pool";
                state.NativeOwner = "FishingPool";
                state.LastNativeAction = "find-pool";
                if (forcedNoWater)
                {
                    ForceFishingNoWaterForSmoke = false;
                    runtime.SetHookStatus("Smoke.AutoFishingNoWaterFeedback", "verified", "0.2.3 toast policy", "No fishable-water state reached; player toast intentionally suppressed.");
                }
                LastFishingAutoCastAttemptSummary = "skipped=no-water owner=" + ownerId + ", forced=" + forcedNoWater;
                return;
            }

            bool forcedNoRod = ForceFishingNoRodForSmoke;
            object? rod = forcedNoRod ? null : ResolveFishingRodForAutomation(dolocApi);
            if (rod == null)
            {
                state.Phase = "NoRod";
                state.LastReason = forcedNoRod ? "smoke:no-rod" : "auto:no-selected-rod";
                state.NativeOwner = "DolocAPI.SelectedItem";
                state.LastNativeAction = "resolve-selected-rod";
                if (forcedNoRod)
                {
                    ForceFishingNoRodForSmoke = false;
                    runtime.SetHookStatus("Smoke.AutoFishingNoRodFeedback", "verified", "0.2.3 toast policy", "No fishing-rod state reached; player toast intentionally suppressed.");
                }
                LastFishingAutoCastAttemptSummary = "skipped=no-rod owner=" + ownerId + ", forced=" + forcedNoRod + ", requireSelected=true";
                return;
            }

            MethodInfo? useFishRod = agent?.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "UseFishRod" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsAssignableFrom(rod.GetType()));
            if (useFishRod == null)
            {
                LastFishingAutoCastAttemptSummary = "skipped=missing-UseFishRod owner=" + ownerId + ", agent=" + (agent == null ? "null" : agent.GetType().FullName) + ", rod=" + rod.GetType().FullName;
                return;
            }

            lastFishingAutoCastAt = DateTimeOffset.Now;
            try
            {
                useFishRod.Invoke(agent, new[] { rod });
                if (smokeFishingPool != null)
                {
                    object? cache = agent == null ? null : ReadMember(agent, "FishingCache");
                    if (cache != null)
                        SetMemberValue(cache, "FishingPool", smokeFishingPool);
                }
                fishingAutoCastApplications++;
                state.Phase = "AutoCast";
                state.LastReason = "auto:UseFishRod";
                state.NativeOwner = "BodyController.UseFishRod";
                state.LastNativeAction = "UseFishRod";
                string smokePoolName = smokeFishingPool == null ? "none" : FirstText(ReadStringMember(smokeFishingPool, "PoolName"), smokeFishingPool.GetType().Name);
                LastFishingAutomationApplicationSummary = "owner=" + ownerId + ", behavior=AutoCast, rod=" + ReadStringMember(rod, "name") + ", smokePoolOverride=" + smokePoolName + ", applications=" + fishingAutoCastApplications;
                LastFishingAutoCastAttemptSummary = LastFishingAutomationApplicationSummary;
                runtime.RuntimeMonitor.Log("Fishing automation auto-cast invoked native BodyController.UseFishRod summary=" + LastFishingAutomationApplicationSummary + ".");
                runtime.SetHookStatus("Smoke.AutoFishingAutoCast", "experimental", "BodyController.UseFishRod", LastFishingAutomationApplicationSummary);
                RecordFishingAutomationSuccess("FishingAutomation.AutoCast");
            }
            catch (Exception ex)
            {
                state.Phase = "AutoCastFailed";
                state.LastReason = ex.GetType().Name;
                LastFishingAutoCastAttemptSummary = "failed=" + ex.GetType().Name + ": " + ex.Message;
                RecordFishingAutomationFailure("FishingAutomation.AutoCast", ex, highFrequency: true);
            }
        }

        internal void NotifyFishingPhase(string phase, object? source)
        {
            if (string.IsNullOrWhiteSpace(phase) || fishingStates.Count == 0)
                return;

            string sourceName = source?.GetType().Name ?? "FishingState";
            foreach (KeyValuePair<string, FishingAutomationState> entry in fishingStates)
            {
                FishingAutomationState state = entry.Value ?? new FishingAutomationState();
                state.Phase = phase;
                state.LastReason = "hook:" + sourceName;
                state.NativeOwner = sourceName;
                state.LastNativeAction = "phase:" + phase;
            }

            if (!phase.Equals("Ready", StringComparison.OrdinalIgnoreCase))
                currentFishingReadyChargeState = null;

            if (phase.Equals("Ready", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("Cast", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("Pull", StringComparison.OrdinalIgnoreCase))
                TryApplyFishingAnimationSpeed(phase, source);

            LogOnce(loggedFishingPhases, phase, "Fishing phase hook observed phase=" + phase + " source=" + sourceName + ".");
        }

        internal void NotifyFishingMiniGameStart(object? gameHandle)
        {
            if (gameHandle != null)
            {
                fishingMiniGameStartedAt[gameHandle] = DateTimeOffset.Now;
                if (!fishingMiniGameInputStats.ContainsKey(gameHandle))
                    fishingMiniGameInputStats[gameHandle] = new FishingMiniGameInputStats();
                fishingMiniGameCompletedHandles.Remove(gameHandle);
            }
            NotifyFishingPhase("MiniGame", gameHandle);
        }

        internal void PrepareFishingMiniGameAutomationInput(object? gameHandle)
        {
            fishingMiniGameInputOverride = null;
            if (gameHandle == null)
                return;

            TryPrepareFishingMiniGameAutomationInput(gameHandle);
        }

        internal void ApplyFishingMiniGameAutomationTick(object? gameHandle)
        {
            if (gameHandle == null)
                return;
            ClearFishingMiniGameAutomationInput(gameHandle);
            if (!fishingMiniGameStartedAt.ContainsKey(gameHandle))
                fishingMiniGameStartedAt[gameHandle] = DateTimeOffset.Now;
            ObserveFishingMiniGameAutomationResult(gameHandle);
        }

        internal void NotifyFishingMiniGameStop(object? gameHandle)
        {
            if (gameHandle != null)
            {
                fishingMiniGameStartedAt.Remove(gameHandle);
                fishingMiniGameBonusTaps.Remove(gameHandle);
                fishingMiniGameInputStats.Remove(gameHandle);
                fishingMiniGameCompletedHandles.Remove(gameHandle);
                ClearFishingMiniGameAutomationInput(gameHandle);
            }
            NotifyFishingPhase("MiniGameStop", gameHandle);
        }

        internal bool ApplyFishingWaitAutomation(object waitState)
        {
            return ApplyFishingWaitAutomation(waitState, "AgentStateFishingWait.OnPlay Postfix");
        }

        internal bool ApplyFishingWaitAutomation(object waitState, string hookSource)
        {
            if (waitState == null || fishingStates.Count == 0 || fishingOptions.Count == 0)
                return false;

            if (!TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return false;

            hookSource = FirstText(hookSource, "AgentStateFishingWait.Postfix");
            string operation = hookSource.IndexOf("OnEnter", StringComparison.OrdinalIgnoreCase) >= 0 ? "FishingAutomation.Wait.OnEnter" : "FishingAutomation.Wait.OnPlay";
            try
            {
                if (operation.Equals("FishingAutomation.Wait.OnPlay", StringComparison.OrdinalIgnoreCase) && !IsCurrentFishingState(waitState))
                    return false;

                bool waitingForBite = ReadBoolMember(waitState, "_waitForFishBite", false);
                bool forcedBite = false;
                bool forcedByInstantBite = false;
                bool nativeTipInvoked = false;
                float hookDuration = 0f;
                int rollAttempts = 0;
                bool forceFishSatisfied = true;
                bool instantBite = options.BiteWaitMode == FishingBiteWaitMode.InstantNativeBite;
                bool skipMiniGame = options.ResultMode == FishingResultMode.SkipMiniGameNativeResult;
                if (waitingForBite && (instantBite || ForceFishingNativeBiteForSmoke))
                {
                    bool requireFish = ForceFishingFishForSmoke && !skipMiniGame;
                    if (!TryForceFishingBite(waitState, requireFish, out rollAttempts, out forceFishSatisfied, out nativeTipInvoked, out hookDuration))
                    {
                        state.Phase = instantBite ? "Wait:AutoBiteFailed" : "Wait:NativeBiteSmokeFailed";
                        state.LastReason = instantBite ? "auto:InstantBite:no-fish" : "smoke:NativeBite:no-fish";
                        state.NativeOwner = "AgentStateFishingWait.RollFish";
                        state.LastNativeAction = "force-bite";
                        runtime.SetHookStatus("Smoke.AutoFishingPhase", "failed", hookSource, state.LastReason + "; rollAttempts=" + rollAttempts.ToString(CultureInfo.InvariantCulture) + ".");
                        return false;
                    }

                    forcedBite = true;
                    forcedByInstantBite = instantBite;
                }

                if (ReadBoolMember(waitState, "_waitForFishBite", false))
                {
                    state.Phase = "WaitingForBite";
                    state.LastReason = instantBite ? "auto:instant-bite-pending" : "auto:native-wait";
                    state.NativeOwner = "AgentStateFishingWait";
                    state.LastNativeAction = "wait-for-bite";
                    return false;
                }

                TryReadFishingCacheInfo(waitState, out object? fishProto, out object? pool, out bool isFish);
                string fishId = fishProto == null ? "unknown" : ReadStringMember(fishProto, "Id");
                string poolName = pool == null ? "unknown" : ReadStringMember(pool, "PoolName");
                FishingBiteAutomationAction action = ResolveFishingBiteAutomationAction(options, isFish, forcedByInstantBite);
                string autoHook = action == FishingBiteAutomationAction.None
                    ? string.Empty
                    : TryAdvanceFishingBite(waitState, action);

                if (action != FishingBiteAutomationAction.None && string.IsNullOrWhiteSpace(autoHook))
                    return false;

                if (action == FishingBiteAutomationAction.None)
                    return false;

                string behavior = forcedByInstantBite ? "InstantBite" : forcedBite ? "NativeBiteReadyForSmoke" : "NativeBiteReady";
                state.Phase = "Wait:" + behavior + ":" + autoHook;
                state.LastReason = "auto:" + behavior + "+" + action;
                state.NativeOwner = "AgentStateFishingWait.NextState";
                state.LastNativeAction = action.ToString();
                state.LastResult = autoHook;
                FishingAutomationApplicationCount++;
                if (forcedByInstantBite)
                    FishingInstantBiteApplicationCount++;
                if (action == FishingBiteAutomationAction.SkipMiniGameNativeResult && autoHook.Equals("AgentStateFishingPull", StringComparison.OrdinalIgnoreCase))
                    FishingSkipMiniGameApplicationCount++;

                LastFishingAutomationApplicationSummary = "owner=" + ownerId +
                    ", behavior=" + behavior +
                    ", phase=Wait" +
                    ", action=" + action +
                    ", autoHook=" + FirstText(autoHook, "none") +
                    ", fish=" + fishId +
                    ", isFish=" + isFish +
                    ", pool=" + poolName +
                    ", biteWaitMode=" + options.BiteWaitMode +
                    ", resultMode=" + options.ResultMode +
                    ", forceFishForSmoke=" + ForceFishingFishForSmoke +
                    ", forceNativeBiteForSmoke=" + ForceFishingNativeBiteForSmoke +
                    ", forceFishSatisfied=" + forceFishSatisfied +
                    ", hookDuration=" + hookDuration.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", nativeTipInvoked=" + nativeTipInvoked +
                    ", source=" + hookSource +
                    ", rollAttempts=" + rollAttempts.ToString(CultureInfo.InvariantCulture) +
                    ", applications=" + FishingAutomationApplicationCount.ToString(CultureInfo.InvariantCulture);

                if (forcedByInstantBite)
                {
                    if (options.VerboseLogging || !loggedFishingPhases.Contains("AutoBite:" + ownerId))
                        runtime.RuntimeMonitor.Log("Fishing automation instant-bite applied by " + ownerId + " fish=" + fishId + " pool=" + poolName + ".");
                    loggedFishingPhases.Add("AutoBite:" + ownerId);
                    runtime.SetHookStatus("Smoke.AutoFishingInstantBite", "verified", hookSource, LastFishingAutomationApplicationSummary);
                }

                runtime.SetHookStatus("Smoke.AutoFishingPhase", "verified", hookSource, LastFishingAutomationApplicationSummary);
                if (action == FishingBiteAutomationAction.SkipMiniGameNativeResult && autoHook.Equals("AgentStateFishingPull", StringComparison.OrdinalIgnoreCase))
                    runtime.SetHookStatus("Smoke.AutoFishingMiniGameSkip", "verified", hookSource + " -> AgentStateFishingPull", LastFishingAutomationApplicationSummary);
                else if (action == FishingBiteAutomationAction.NativeReel && autoHook.Equals("AgentStateFishingBattle", StringComparison.OrdinalIgnoreCase))
                    runtime.SetHookStatus("Smoke.AutoFishingMiniGameComplete", "pending", hookSource + " -> FishingGameScrollBar.UpdateGame", LastFishingAutomationApplicationSummary);

                RecordFishingAutomationSuccess(operation);
                return true;
            }
            catch (Exception ex)
            {
                RecordFishingAutomationFailure(operation, ex, true, "Smoke.AutoFishingPhase", hookSource);
                return false;
            }
        }

        private static bool IsCurrentFishingState(object waitState)
        {
            object? body = ReadMember(waitState, "body");
            object? stateManager = body == null ? null : ReadMember(body, "StateManager");
            object? current = stateManager == null ? null : ReadMember(stateManager, "current");
            return current == null || ReferenceEquals(current, waitState);
        }

        private static bool TryReadFishingCacheFish(object waitState, out object? fishProto)
        {
            TryReadFishingCacheInfo(waitState, out fishProto, out _, out bool isFish);
            return fishProto != null && isFish;
        }

        private static bool TryForceFishingBite(object waitState, bool requireFish, out int rollAttempts, out bool forceFishSatisfied, out bool nativeTipInvoked, out float hookDuration)
        {
            rollAttempts = 0;
            forceFishSatisfied = !requireFish;
            nativeTipInvoked = false;
            hookDuration = 0f;
            MethodInfo? rollFish = FindMethodInHierarchy(waitState.GetType(), "RollFish", 0);
            if (rollFish == null)
                return false;

            bool rolled = false;
            while (rollAttempts < 100)
            {
                object? rolledValue = rollFish.Invoke(waitState, null);
                rolled = rolledValue is bool value && value;
                rollAttempts++;
                if (!rolled)
                    continue;

                forceFishSatisfied = !requireFish || TryReadFishingCacheFish(waitState, out _);
                if (forceFishSatisfied)
                    break;
            }

            if (!rolled || !forceFishSatisfied)
                return false;

            WriteBoolMember(waitState, "_waitForFishBite", false);
            WriteBoolMember(waitState, "_hasRolled", true);
            WriteFloatMember(waitState, "_hookProbability", 1f);
            hookDuration = ResolveNativeFishOnHookDuration();
            WriteFloatMember(waitState, "_fishOnHookDuration", hookDuration);
            nativeTipInvoked = TryInvokeFishOnHookTip(waitState);
            TryRefreshFishingRendererAfterBite(waitState);
            return true;
        }

        private static float ResolveNativeFishOnHookDuration()
        {
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? gameManager = ReadStaticMember(dolocApi, "gameManager");
                object? gameInitConfig = gameManager == null ? null : ReadMember(gameManager, "gameInitConfig");
                if (gameInitConfig != null && ReadBoolMember(gameInitConfig, "skipFishingWait", false))
                    return 100f;

                object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
                double pullTiming = globalParameter == null ? 0d : ReadDoubleMember(globalParameter, "PullTiming", 0d);
                if (double.IsNaN(pullTiming) || double.IsInfinity(pullTiming) || pullTiming <= 0d)
                    return 2f;
                return (float)Math.Min(30d, Math.Max(0.25d, pullTiming));
            }
            catch
            {
                return 2f;
            }
        }

        private static bool TryInvokeFishOnHookTip(object waitState)
        {
            try
            {
                MethodInfo? tip = FindMethodInHierarchy(waitState.GetType(), "InvokeFishOnHookTip", 0);
                if (tip == null)
                    return false;
                tip.Invoke(waitState, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void TryReadFishingCacheInfo(object waitState, out object? fishProto, out object? pool, out bool isFish)
        {
            fishProto = null;
            pool = null;
            isFish = false;
            object? body = ReadMember(waitState, "body");
            object? cache = body == null ? null : ReadMember(body, "FishingCache");
            fishProto = cache == null ? null : ReadMember(cache, "FishProto");
            pool = cache == null ? null : ReadMember(cache, "FishingPool");
            isFish = fishProto != null && ReadBoolMember(fishProto, "IsFish", false);
        }

        private static FishingBiteAutomationAction ResolveFishingBiteAutomationAction(FishingAutomationOptions? options, bool isFish, bool instantBiteReady)
        {
            if (options == null)
                return FishingBiteAutomationAction.None;
            if (options.ResultMode == FishingResultMode.SkipMiniGameNativeResult)
                return FishingBiteAutomationAction.SkipMiniGameNativeResult;
            return FishingBiteAutomationAction.NativeReel;
        }

        private bool TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state)
        {
            ownerId = string.Empty;
            options = null!;
            state = null!;
            foreach (KeyValuePair<string, FishingAutomationState> entry in fishingStates)
            {
                FishingAutomationState candidateState = entry.Value ?? new FishingAutomationState();
                if (!candidateState.Enabled)
                    continue;
                if (!fishingOptions.TryGetValue(entry.Key, out FishingAutomationOptions candidateOptions))
                    continue;
                ownerId = entry.Key;
                options = candidateOptions ?? new FishingAutomationOptions();
                state = candidateState;
                return true;
            }
            return false;
        }

        private static FishingAutomationOptions NormalizeFishingAutomationOptions(FishingAutomationOptions? options)
        {
            options ??= new FishingAutomationOptions();
            options.RecastDelaySeconds = ClampSeconds(options.RecastDelaySeconds, 0.05, 10);
            options.AnimationMultiplier = ClampMultiplier(options.AnimationMultiplier);
            options.CastChargeRatio = ClampCastChargeRatio(options.CastChargeRatio);
            if (!Enum.IsDefined(typeof(FishingBiteWaitMode), options.BiteWaitMode))
                options.BiteWaitMode = FishingBiteWaitMode.NativeWait;
            if (!Enum.IsDefined(typeof(FishingResultMode), options.ResultMode))
                options.ResultMode = FishingResultMode.AutoCompleteVisibleMiniGame;
            if (!Enum.IsDefined(typeof(FishingAnimationMode), options.AnimationMode))
                options.AnimationMode = FishingAnimationMode.Normal;
            return options;
        }

        private static void TryRefreshFishingRendererAfterBite(object waitState)
        {
            object? body = ReadMember(waitState, "body");
            object? renderer = body == null ? null : ReadMember(body, "fishRodRenderer");
            object? line = renderer == null ? null : ReadMember(renderer, "Line");
            line?.GetType().GetMethod("UseStraightLine", BindingFlags.Public | BindingFlags.Instance)?.Invoke(line, null);
            renderer?.GetType().GetMethod("EnableFishShadow", BindingFlags.Public | BindingFlags.Instance)?.Invoke(renderer, null);
        }

        private static string TryAdvanceFishingBite(object waitState, FishingBiteAutomationAction action)
        {
            object? body = ReadMember(waitState, "body");
            object? stateManager = body == null ? null : ReadMember(body, "StateManager");
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (stateManager == null)
                return string.Empty;

            bool shouldSkipMiniGame = action == FishingBiteAutomationAction.SkipMiniGameNativeResult;
            bool originalSkip = ReadNativeSkipFishingGame(dolocApi);
            bool wroteSkip = false;
            if (shouldSkipMiniGame && !originalSkip)
            {
                wroteSkip = TryWriteNativeSkipFishingGame(dolocApi, true);
                if (!wroteSkip)
                    return string.Empty;
            }

            object? targetState;
            try
            {
                MethodInfo? nextState = FindMethodInHierarchy(waitState.GetType(), "NextState", 0);
                targetState = nextState?.Invoke(waitState, null);
                if (targetState == null || ReferenceEquals(targetState, waitState))
                    targetState = TryMirrorNativeFishingWaitNextState(waitState, action, dolocApi);
            }
            finally
            {
                if (wroteSkip)
                    TryWriteNativeSkipFishingGame(dolocApi, originalSkip);
            }

            if (targetState == null || ReferenceEquals(targetState, waitState))
                return string.Empty;

            if (!TryOverwriteFishingState(stateManager, targetState))
                return string.Empty;

            return targetState.GetType().Name;
        }

        private static object? TryMirrorNativeFishingWaitNextState(object waitState, FishingBiteAutomationAction action, Type? dolocApi)
        {
            object? body = ReadMember(waitState, "body");
            if (body == null)
                return null;

            object? cache = ReadMember(body, "FishingCache");
            object? fishProto = cache == null ? null : ReadMember(cache, "FishProto");
            if (fishProto == null)
            {
                TryInvokeFishingWaitTip(waitState, "InvokeNoFishTip");
                return GetNativeFishingPullState(waitState, isFailed: true);
            }

            double hookDuration = ReadDoubleMember(waitState, "_fishOnHookDuration", 0d);
            if (hookDuration <= 0d)
            {
                TryInvokeFishingWaitTip(waitState, "InvokeFishRunAwayTip");
                return GetNativeFishingPullState(waitState, isFailed: true);
            }

            TryCostNativeFishingEnergy(dolocApi);
            bool isFish = ReadBoolMember(fishProto, "IsFish", false);
            bool shouldSkipMiniGame = action == FishingBiteAutomationAction.SkipMiniGameNativeResult || ReadNativeSkipFishingGame(dolocApi);
            if (isFish && !shouldSkipMiniGame)
                return GetNativeFishingState(waitState, "DolocTown.AgentStateFishingBattle");

            return GetNativeFishingPullState(waitState, isFailed: false);
        }

        private static bool TryOverwriteFishingState(object stateManager, object targetState)
        {
            MethodInfo? overwrite = stateManager.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "Overwrite" && !m.IsGenericMethodDefinition && m.GetParameters().Length >= 1);
            if (overwrite == null)
                return false;

            ParameterInfo[] parameters = overwrite.GetParameters();
            object?[] args = parameters.Length >= 2 ? new object?[] { targetState, true } : new object?[] { targetState };
            overwrite.Invoke(stateManager, args);
            return true;
        }

        private static object? GetNativeFishingPullState(object waitState, bool isFailed)
        {
            object? pullState = GetNativeFishingState(waitState, "DolocTown.AgentStateFishingPull");
            if (pullState != null)
                WriteBoolMember(pullState, "IsFailed", isFailed);
            return pullState;
        }

        private static object? GetNativeFishingState(object waitState, string stateTypeName)
        {
            Type? stateType = ResolveType(stateTypeName + ", Assembly-CSharp");
            if (stateType == null)
                return null;

            MethodInfo? getState = FindGenericMethodInHierarchy(waitState.GetType(), "GetState", 0);
            if (getState != null)
            {
                try
                {
                    return getState.MakeGenericMethod(stateType).Invoke(waitState, null);
                }
                catch
                {
                }
            }

            object? body = ReadMember(waitState, "body");
            object? stateManager = body == null ? null : ReadMember(body, "StateManager");
            MethodInfo? managerGetState = FindGenericMethodInHierarchy(stateManager?.GetType(), "GetState", 0);
            if (managerGetState == null)
                return null;

            try
            {
                return managerGetState.MakeGenericMethod(stateType).Invoke(stateManager, null);
            }
            catch
            {
                return null;
            }
        }

        private static MethodInfo? FindGenericMethodInHierarchy(Type? type, string name, int parameterCount)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                foreach (MethodInfo method in current.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (method.Name == name && method.IsGenericMethodDefinition && method.GetParameters().Length == parameterCount)
                        return method;
                }
            }
            return null;
        }

        private static bool TryCostNativeFishingEnergy(Type? dolocApi)
        {
            try
            {
                object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
                object? energyCost = globalParameter == null ? null : ReadMember(globalParameter, "FishingEnergyCost");
                if (dolocApi == null || energyCost == null)
                    return false;

                MethodInfo? costEnergy = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "CostEnergy" && m.GetParameters().Length == 1);
                if (costEnergy == null)
                    return false;

                ParameterInfo parameter = costEnergy.GetParameters()[0];
                object convertedCost = Convert.ChangeType(energyCost, parameter.ParameterType, CultureInfo.InvariantCulture);
                object? result = costEnergy.Invoke(null, new[] { convertedCost });
                return result is bool value ? value : true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryInvokeFishingWaitTip(object waitState, string methodName)
        {
            try
            {
                MethodInfo? method = FindMethodInHierarchy(waitState.GetType(), methodName, 0);
                if (method == null)
                    return false;
                method.Invoke(waitState, null);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ReadNativeSkipFishingGame(Type? dolocApi)
        {
            try
            {
                object? gameManager = ReadStaticMember(dolocApi, "gameManager");
                object? gameInitConfig = gameManager == null ? null : ReadMember(gameManager, "gameInitConfig");
                return gameInitConfig != null && ReadBoolMember(gameInitConfig, "skipFishingGame", false);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryWriteNativeSkipFishingGame(Type? dolocApi, bool value)
        {
            try
            {
                object? gameManager = ReadStaticMember(dolocApi, "gameManager");
                object? gameInitConfig = gameManager == null ? null : ReadMember(gameManager, "gameInitConfig");
                return gameInitConfig != null && WriteBoolMember(gameInitConfig, "skipFishingGame", value);
            }
            catch
            {
                return false;
            }
        }

        private void TryApplyFishingAnimationSpeed(string phase, object? stateSource)
        {
            if (stateSource == null || !TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return;
            if (options.AnimationMode != FishingAnimationMode.FastCastPull || options.AnimationMultiplier <= 1)
                return;

            double multiplier = ClampMultiplier(options.AnimationMultiplier);
            var samples = new List<string>();
            var animators = new List<KeyValuePair<object, string>>();
            AddFishingAnimationCandidates(animators, stateSource, "source");
            AddGlobalFishingAnimationCandidates(animators);

            int changed = 0;
            var seen = new HashSet<object>();
            foreach (KeyValuePair<object, string> animator in animators)
            {
                if (!seen.Add(animator.Key))
                    continue;
                changed += ApplyAnimatorSpeed(animator.Key, multiplier, animator.Value, samples);
            }

            int timingChanged = 0;
            if (phase.Equals("Pull", StringComparison.OrdinalIgnoreCase) &&
                (DateTimeOffset.UtcNow - lastFishingPullDurationResultScaleAtUtc).TotalSeconds > 0.5)
            {
                timingChanged += TryScaleFishingPullStateDuration(stateSource, multiplier, samples);
            }

            if (changed <= 0 && timingChanged <= 0)
            {
                state.Phase = phase + ":FastAnimationPending";
                state.LastReason = "auto:FastAnimation:no-reachable-animator";
                LastFishingAutomationApplicationSummary = "owner=" + ownerId + ", behavior=FastAnimation, phase=" + phase + ", status=pending, multiplier=" + multiplier.ToString("0.###") + ", animators=0, timings=0, samples=none, applications=" + FishingAutomationApplicationCount;
                LastFishingAnimationSpeedSummary = LastFishingAutomationApplicationSummary;
                LogOnce(loggedFishingPhases, "FastAnimationPending:" + ownerId + ":" + phase, "Fishing automation animation speed pending by " + ownerId + " phase=" + phase + "; no reachable animator path was writable.");
                runtime.SetHookStatus("Smoke.AutoFishingAnimationSpeed", "pending", "AgentStateFishingCast/Pull.OnEnter", LastFishingAutomationApplicationSummary);
                return;
            }

            FishingAutomationApplicationCount++;
            state.Phase = phase + ":FastAnimation";
            state.LastReason = "auto:FastAnimation";
            LastFishingAutomationApplicationSummary = "owner=" + ownerId + ", behavior=FastAnimation, phase=" + phase + ", multiplier=" + multiplier.ToString("0.###") + ", animators=" + changed + ", timings=" + timingChanged + ", samples=" + string.Join(";", samples.ToArray()) + ", applications=" + FishingAutomationApplicationCount;
            LastFishingAnimationSpeedSummary = LastFishingAutomationApplicationSummary;
            runtime.RuntimeMonitor.Log("Fishing automation animation speed applied by " + ownerId + " phase=" + phase + " multiplier=" + multiplier.ToString("0.###") + " animators=" + changed + " timings=" + timingChanged + ".");
            runtime.SetHookStatus("Smoke.AutoFishingAnimationSpeed", "verified", "AgentStateFishingCast/Pull.OnEnter", LastFishingAutomationApplicationSummary);
        }

        internal void AdjustFishingPullDurationResult(ref float duration, string source)
        {
            if (duration <= 0f || !TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return;
            if (options.AnimationMode != FishingAnimationMode.FastCastPull || options.AnimationMultiplier <= 1)
                return;

            try
            {
                double multiplier = ClampMultiplier(options.AnimationMultiplier);
                float original = duration;
                float scaled = (float)Math.Max(0.01d, original / multiplier);
                duration = scaled;
                lastFishingPullDurationResultScaleAtUtc = DateTimeOffset.UtcNow;
                FishingAutomationApplicationCount++;
                state.Phase = "Pull:FastDuration";
                state.LastReason = "auto:FastAnimation:PullDuration";
                state.NativeOwner = source;
                state.LastNativeAction = "duration=" + original.ToString("0.###", CultureInfo.InvariantCulture) + "->" + scaled.ToString("0.###", CultureInfo.InvariantCulture);
                LastFishingAutomationApplicationSummary = "owner=" + ownerId +
                    ", behavior=FastPullDuration" +
                    ", source=" + source +
                    ", multiplier=" + multiplier.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", duration=" + original.ToString("0.###", CultureInfo.InvariantCulture) + "->" + scaled.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", applications=" + FishingAutomationApplicationCount.ToString(CultureInfo.InvariantCulture);
                LastFishingAnimationSpeedSummary = LastFishingAutomationApplicationSummary;
                runtime.SetHookStatus("Smoke.AutoFishingAnimationSpeed", "verified", source, LastFishingAutomationApplicationSummary);
            }
            catch (Exception ex)
            {
                RecordFishingAutomationFailure("FishingAutomation.Pull.Duration", ex, true, "Smoke.AutoFishingAnimationSpeed", source);
            }
        }

        internal void ApplyFishingReadyAutomation(object? readyState)
        {
            ApplyFishingReadyChargeTarget(readyState);
            ApplyFishingReadyChargeSpeed(readyState);
        }

        internal void ApplyFishingReadyChargeSpeed(object? readyState)
        {
            if (readyState == null || !TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return;
            if (options.AnimationMode != FishingAnimationMode.FastCastPull || options.AnimationMultiplier <= 1)
                return;

            try
            {
                double multiplier = ClampMultiplier(options.AnimationMultiplier);
                object? timer = ReadMember(readyState, "_castTimer");
                if (timer == null)
                {
                    PublishFastReadyChargePending(ownerId, state, multiplier, "no-cast-timer");
                    return;
                }

                double before = ReadDoubleMember(timer, "Progress", double.NaN);
                double fixedDeltaTime = ReadUnityFixedDeltaTime();
                double extraDelta = fixedDeltaTime * (multiplier - 1d);
                if (extraDelta <= 0d || !TryInvokeTick(timer, extraDelta))
                {
                    PublishFastReadyChargePending(ownerId, state, multiplier, "tick-unavailable");
                    return;
                }

                double after = ReadDoubleMember(timer, "Progress", double.NaN);
                var samples = new List<string>();
                if (!double.IsNaN(before) && !double.IsNaN(after))
                    samples.Add("castTimer.Progress:" + before.ToString("0.###", CultureInfo.InvariantCulture) + "->" + after.ToString("0.###", CultureInfo.InvariantCulture));
                samples.Add("extraDt=" + extraDelta.ToString("0.###", CultureInfo.InvariantCulture));
                RefreshFishingReadyChargeBar(readyState, after, samples);

                if (fishingReadyChargeAppliedStates.Add(readyState))
                    FishingReadyChargeApplicationCount++;

                state.Phase = "Ready:FastCharge";
                state.LastReason = "auto:FastAnimation:ReadyCharge";
                state.NativeOwner = "AgentStateFishingReady.OnPlay";
                state.LastNativeAction = string.Join(";", samples.ToArray());
                LastFishingAutomationApplicationSummary = "owner=" + ownerId +
                    ", behavior=FastReadyCharge" +
                    ", source=AgentStateFishingReady.OnPlay" +
                    ", multiplier=" + multiplier.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", samples=" + string.Join(";", samples.ToArray()) +
                    ", readyCharges=" + FishingReadyChargeApplicationCount.ToString(CultureInfo.InvariantCulture) +
                    ", applications=" + FishingAutomationApplicationCount.ToString(CultureInfo.InvariantCulture);
                LastFishingAnimationSpeedSummary = LastFishingAutomationApplicationSummary;
                runtime.SetHookStatus("Smoke.AutoFishingReadyChargeSpeed", "verified", "AgentStateFishingReady.OnPlay", LastFishingAutomationApplicationSummary);
            }
            catch (Exception ex)
            {
                RecordFishingAutomationFailure("FishingAutomation.Ready.ChargeSpeed", ex, true, "Smoke.AutoFishingReadyChargeSpeed", "AgentStateFishingReady.OnPlay");
            }
        }

        private void ApplyFishingReadyChargeTarget(object? readyState)
        {
            if (readyState == null || !TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return;

            currentFishingReadyChargeState = readyState;
            double target = ClampCastChargeRatio(options.CastChargeRatio);
            double progress = ReadReadyChargeProgress(readyState);
            RefreshFishingReadyChargeBar(readyState, progress, new List<string>());

            if (!fishingReadyChargeTargetStates.Add(readyState))
                return;

            FishingReadyChargeTargetApplicationCount++;
            state.Phase = "Ready:ChargeTarget";
            state.LastReason = "auto:ReadyChargeTarget";
            state.NativeOwner = "AgentStateFishingReady.NextState";
            state.LastNativeAction = "target=" + target.ToString("0.###", CultureInfo.InvariantCulture);
            LastFishingAutomationApplicationSummary = "owner=" + ownerId +
                ", behavior=ReadyChargeTarget" +
                ", source=AgentStateFishingReady.NextState" +
                ", target=" + target.ToString("0.###", CultureInfo.InvariantCulture) +
                ", progress=" + progress.ToString("0.###", CultureInfo.InvariantCulture) +
                ", applications=" + FishingReadyChargeTargetApplicationCount.ToString(CultureInfo.InvariantCulture);
            runtime.SetHookStatus("Smoke.AutoFishingCastCharge", "experimental", "AgentStateFishingReady.NextState", LastFishingAutomationApplicationSummary);
        }

        internal bool TryOverrideFishingReadyChargeInput(string inputName, out bool value)
        {
            value = false;
            if (!inputName.Equals("NormalUseToolInProgress", StringComparison.OrdinalIgnoreCase))
                return false;

            object? readyState = currentFishingReadyChargeState;
            if (readyState == null || !TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return false;

            double target = ClampCastChargeRatio(options.CastChargeRatio);
            double releaseThreshold = target >= 0.999d ? 0.995d : target;
            double progress = ReadReadyChargeProgress(readyState);
            bool hold = target > 0d && progress < releaseThreshold;
            value = hold;

            if (!hold && fishingReadyChargeReleasedStates.Add(readyState))
            {
                state.Phase = target <= 0d ? "Ready:NoChargeRelease" : "Ready:ChargeRelease";
                state.LastReason = target <= 0d ? "auto:ReadyChargeTarget:no-charge" : "auto:ReadyChargeTarget:reached";
                state.NativeOwner = "AgentStateFishingReady.NextState";
                state.LastNativeAction = "target=" + target.ToString("0.###", CultureInfo.InvariantCulture) + ";progress=" + progress.ToString("0.###", CultureInfo.InvariantCulture) + ";input=release";
                LastFishingAutomationApplicationSummary = "owner=" + ownerId +
                    ", behavior=ReadyChargeTarget" +
                    ", source=AgentStateFishingReady.NextState" +
                    ", target=" + target.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", progress=" + progress.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", input=release" +
                    ", applications=" + FishingReadyChargeTargetApplicationCount.ToString(CultureInfo.InvariantCulture);
                runtime.SetHookStatus("Smoke.AutoFishingCastCharge", "verified", "AgentStateFishingReady.NextState", LastFishingAutomationApplicationSummary);
            }

            return true;
        }

        private static double ReadReadyChargeProgress(object readyState)
        {
            object? timer = ReadMember(readyState, "_castTimer");
            if (timer == null)
                return 0d;
            double progress = ReadDoubleMember(timer, "Progress", 0d);
            if (double.IsNaN(progress) || double.IsInfinity(progress))
                return 0d;
            return Math.Min(1d, Math.Max(0d, progress));
        }

        private static double ClampCastChargeRatio(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0d;
            return Math.Min(1d, Math.Max(0d, value));
        }

        private void PublishFastReadyChargePending(string ownerId, FishingAutomationState state, double multiplier, string reason)
        {
            state.Phase = "Ready:FastChargePending";
            state.LastReason = "auto:FastAnimation:" + reason;
            state.NativeOwner = "AgentStateFishingReady.OnPlay";
            state.LastNativeAction = reason;
            LastFishingAutomationApplicationSummary = "owner=" + ownerId +
                ", behavior=FastReadyCharge" +
                ", source=AgentStateFishingReady.OnPlay" +
                ", status=pending" +
                ", reason=" + reason +
                ", multiplier=" + multiplier.ToString("0.###", CultureInfo.InvariantCulture) +
                ", applications=" + FishingAutomationApplicationCount.ToString(CultureInfo.InvariantCulture);
            LastFishingAnimationSpeedSummary = LastFishingAutomationApplicationSummary;
            runtime.SetHookStatus("Smoke.AutoFishingReadyChargeSpeed", "pending", "AgentStateFishingReady.OnPlay", LastFishingAutomationApplicationSummary);
        }

        private static double ReadUnityFixedDeltaTime()
        {
            object? value = ReadStaticMember(ResolveType("UnityEngine.Time, UnityEngine.CoreModule"), "fixedDeltaTime") ??
                ReadStaticMember(ResolveType("UnityEngine.Time, UnityEngine"), "fixedDeltaTime");
            if (value == null)
                return 0.02d;
            double fixedDeltaTime = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return fixedDeltaTime > 0d ? fixedDeltaTime : 0.02d;
        }

        private static bool TryInvokeTick(object timer, double deltaTime)
        {
            MethodInfo? tick = FindMethodInHierarchy(timer.GetType(), "Tick", 1);
            if (tick == null)
                return false;

            ParameterInfo parameter = tick.GetParameters()[0];
            object convertedDelta = Convert.ChangeType(deltaTime, parameter.ParameterType, CultureInfo.InvariantCulture);
            tick.Invoke(timer, new[] { convertedDelta });
            return true;
        }

        private static void RefreshFishingReadyChargeBar(object readyState, double progress, List<string> samples)
        {
            if (double.IsNaN(progress))
                return;

            object? powerBar = ReadMember(readyState, "_powerBar");
            if (powerBar == null)
                return;

            int changed = 0;
            if (SetMemberValue(powerBar, "Progress", (float)progress))
                changed++;

            object? body = ReadMember(readyState, "body");
            object? renderer = body == null ? null : ReadMember(body, "fishRodRenderer");
            MethodInfo? getColor = renderer?.GetType().GetMethod("GetCastForceColor", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(float) }, null);
            if (getColor != null)
            {
                object? color = getColor.Invoke(renderer, new object[] { (float)progress });
                if (color != null && SetMemberValue(powerBar, "Color", color))
                    changed++;
            }

            if (changed > 0 && samples.Count < 6)
                samples.Add("powerBar.updated=" + changed.ToString(CultureInfo.InvariantCulture));
        }

        internal void AdjustFishingCastHookPhysics(object? fishRodRenderer)
        {
            if (fishRodRenderer == null || !TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return;
            if (options.AnimationMode != FishingAnimationMode.FastCastPull || options.AnimationMultiplier <= 1)
                return;

            try
            {
                double multiplier = ClampMultiplier(options.AnimationMultiplier);
                object? hook = ReadMember(fishRodRenderer, "Hook") ?? ReadMember(fishRodRenderer, "_hook");
                if (hook == null)
                {
                    PublishFastCastHookPending(ownerId, state, multiplier, "no-hook");
                    return;
                }

                var samples = new List<string>();
                int changed = 0;
                changed += TryScaleVector2Member(hook, "Velocity", multiplier, "hook.Velocity", samples);

                object? rigidbody = TryGetUnityComponent(hook, "UnityEngine.Rigidbody2D, UnityEngine.Physics2DModule") ??
                    TryGetUnityComponent(hook, "UnityEngine.Rigidbody2D, UnityEngine");
                changed += TryScaleHookGravity(rigidbody, multiplier, samples);

                if (changed <= 0)
                {
                    PublishFastCastHookPending(ownerId, state, multiplier, "no-writable-hook-physics");
                    return;
                }

                FishingAutomationApplicationCount++;
                state.Phase = "Cast:FastHookPhysics";
                state.LastReason = "auto:FastAnimation:CastHookPhysics";
                state.NativeOwner = "FishRodRenderer.CastHook";
                state.LastNativeAction = string.Join(";", samples.ToArray());
                LastFishingAutomationApplicationSummary = "owner=" + ownerId +
                    ", behavior=FastCastHookPhysics" +
                    ", source=FishRodRenderer.CastHook" +
                    ", multiplier=" + multiplier.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", physics=" + changed.ToString(CultureInfo.InvariantCulture) +
                    ", samples=" + string.Join(";", samples.ToArray()) +
                    ", applications=" + FishingAutomationApplicationCount.ToString(CultureInfo.InvariantCulture);
                LastFishingAnimationSpeedSummary = LastFishingAutomationApplicationSummary;
                runtime.SetHookStatus("Smoke.AutoFishingAnimationSpeed", "verified", "FishRodRenderer.CastHook", LastFishingAutomationApplicationSummary);
            }
            catch (Exception ex)
            {
                RecordFishingAutomationFailure("FishingAutomation.CastHook.Physics", ex, true, "Smoke.AutoFishingAnimationSpeed", "FishRodRenderer.CastHook");
            }
        }

        private void PublishFastCastHookPending(string ownerId, FishingAutomationState state, double multiplier, string reason)
        {
            state.Phase = "Cast:FastHookPhysicsPending";
            state.LastReason = "auto:FastAnimation:" + reason;
            state.NativeOwner = "FishRodRenderer.CastHook";
            state.LastNativeAction = reason;
            LastFishingAutomationApplicationSummary = "owner=" + ownerId +
                ", behavior=FastCastHookPhysics" +
                ", source=FishRodRenderer.CastHook" +
                ", status=pending" +
                ", reason=" + reason +
                ", multiplier=" + multiplier.ToString("0.###", CultureInfo.InvariantCulture) +
                ", applications=" + FishingAutomationApplicationCount.ToString(CultureInfo.InvariantCulture);
            LastFishingAnimationSpeedSummary = LastFishingAutomationApplicationSummary;
            runtime.SetHookStatus("Smoke.AutoFishingAnimationSpeed", "pending", "FishRodRenderer.CastHook", LastFishingAutomationApplicationSummary);
        }

        private int TryScaleHookGravity(object? rigidbody, double multiplier, List<string> samples)
        {
            if (rigidbody == null)
                return 0;

            object? value = ReadMember(rigidbody, "gravityScale");
            if (value == null)
                return 0;

            double current = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            if (!originalHookGravityScales.TryGetValue(rigidbody, out double original))
            {
                original = current;
                originalHookGravityScales[rigidbody] = original;
            }

            double target = original * multiplier * multiplier;
            if (!SetMemberValue(rigidbody, "gravityScale", (float)target))
                return 0;

            if (samples.Count < 6)
                samples.Add("hook.gravityScale:" + original.ToString("0.###", CultureInfo.InvariantCulture) + "->" + target.ToString("0.###", CultureInfo.InvariantCulture));
            return 1;
        }

        private static int TryScaleVector2Member(object instance, string memberName, double multiplier, string label, List<string> samples)
        {
            object? vector = ReadMember(instance, memberName);
            if (vector == null)
                return 0;

            double x = ReadVectorComponent(vector, "x");
            double y = ReadVectorComponent(vector, "y");
            if (double.IsNaN(x) || double.IsNaN(y))
                return 0;

            object? scaled = CreateVector2(vector.GetType(), x * multiplier, y * multiplier);
            if (scaled == null || !SetMemberValue(instance, memberName, scaled))
                return 0;

            if (samples.Count < 6)
                samples.Add(label + ":(" + x.ToString("0.###", CultureInfo.InvariantCulture) + "," + y.ToString("0.###", CultureInfo.InvariantCulture) + ")->(" + (x * multiplier).ToString("0.###", CultureInfo.InvariantCulture) + "," + (y * multiplier).ToString("0.###", CultureInfo.InvariantCulture) + ")");
            return 1;
        }

        private static double ReadVectorComponent(object vector, string name)
        {
            object? value = ReadMember(vector, name);
            return value == null ? double.NaN : Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        private static object? CreateVector2(Type vectorType, double x, double y)
        {
            ConstructorInfo? constructor = vectorType.GetConstructor(new[] { typeof(float), typeof(float) });
            if (constructor == null)
                return null;
            return constructor.Invoke(new object[] { (float)x, (float)y });
        }

        private static void AddFishingAnimationCandidates(List<KeyValuePair<object, string>> animators, object source, string label)
        {
            AddAnimatorCandidates(animators, source, label);
            AddFishingAnimationMemberCandidates(animators, source, label, "body");
            AddFishingAnimationMemberCandidates(animators, source, label, "Body");
            AddFishingAnimationMemberCandidates(animators, source, label, "fishRodRenderer");
            AddFishingAnimationMemberCandidates(animators, source, label, "FishRodRenderer");
        }

        private static void AddGlobalFishingAnimationCandidates(List<KeyValuePair<object, string>> animators)
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? agent = ReadStaticMember(dolocApi, "agent");
            if (agent != null)
                AddFishingAnimationCandidates(animators, agent, "DolocAPI.agent");
        }

        private static int TryScaleFishingPullStateDuration(object source, double multiplier, List<string> samples)
        {
            object? current = ReadMember(source, "_pullDuration");
            if (current == null)
                return 0;

            double original = Convert.ToDouble(current, CultureInfo.InvariantCulture);
            if (original <= 0d)
                return 0;

            float scaled = (float)Math.Max(0.01d, original / multiplier);
            if (!WriteFloatMember(source, "_pullDuration", scaled))
                return 0;

            if (samples.Count < 6)
                samples.Add("source._pullDuration:" + original.ToString("0.###", CultureInfo.InvariantCulture) + "->" + scaled.ToString("0.###", CultureInfo.InvariantCulture));
            return 1;
        }

        private static void AddFishingAnimationMemberCandidates(List<KeyValuePair<object, string>> animators, object source, string label, string memberName)
        {
            object? member = ReadMember(source, memberName);
            if (member == null)
                return;

            string memberLabel = label + "." + memberName;
            AddAnimatorCandidates(animators, member, memberLabel);
            if (!memberName.Equals("fishRodRenderer", StringComparison.OrdinalIgnoreCase))
            {
                AddFishingAnimationMemberCandidates(animators, member, memberLabel, "fishRodRenderer");
                AddFishingAnimationMemberCandidates(animators, member, memberLabel, "FishRodRenderer");
            }
        }

        private void TryPrepareFishingMiniGameAutomationInput(object gameHandle)
        {
            if (!TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return;
            if (options.ResultMode != FishingResultMode.AutoCompleteVisibleMiniGame)
                return;

            try
            {
                if (!TryReadFishingGameStatus(gameHandle, out _, out string currentStatus) ||
                    !currentStatus.Equals("Running", StringComparison.OrdinalIgnoreCase))
                    return;

                if (!fishingMiniGameStartedAt.ContainsKey(gameHandle))
                    fishingMiniGameStartedAt[gameHandle] = DateTimeOffset.Now;

                object? noteSpawner = ReadMember(gameHandle, "noteSpawner");
                if (noteSpawner == null)
                    return;

                double currentTime = Math.Max(0d, ReadUnityTime() - ReadDoubleMember(gameHandle, "startTime", 0d));
                double delayTime = ReadDoubleMember(noteSpawner, "delayTime", 0d);
                FishingMiniGameInputDecision decision = FishingMiniGameInputDecision.Release;
                string noteType = "None";
                int noteIndex = -1;
                double noteStart = 0d;
                double noteEnd = 0d;

                if (currentTime >= delayTime && TryPeekCurrentFishingNote(gameHandle, noteSpawner, currentTime, out object? note) && note != null)
                {
                    noteType = ReadMember(note, "noteType")?.ToString() ?? "Unknown";
                    noteIndex = ReadIntMember(note, "index", ReadIntMember(gameHandle, "currentNoteIndex", -1));
                    noteStart = ReadDoubleMember(note, "startTime", 0d);
                    noteEnd = ReadDoubleMember(note, "endTime", 0d);
                    bool bonusAlreadyTapped = noteIndex >= 0 &&
                        fishingMiniGameBonusTaps.TryGetValue(gameHandle, out HashSet<int> tapped) &&
                        tapped.Contains(noteIndex);
                    decision = ResolveFishingMiniGameInputDecision(noteType, currentTime, noteStart, noteEnd, bonusAlreadyTapped);
                    if (decision == FishingMiniGameInputDecision.TapBonus && noteIndex >= 0)
                    {
                        if (!fishingMiniGameBonusTaps.TryGetValue(gameHandle, out HashSet<int> tappedNotes))
                        {
                            tappedNotes = new HashSet<int>();
                            fishingMiniGameBonusTaps[gameHandle] = tappedNotes;
                        }
                        tappedNotes.Add(noteIndex);
                    }
                }

                FishingMiniGameInputStats stats = GetFishingMiniGameInputStats(gameHandle);
                stats.Record(decision, noteType);
                fishingMiniGameInputOverride = new FishingMiniGameInputOverride(gameHandle, decision, DateTimeOffset.UtcNow.AddMilliseconds(250));

                state.Phase = "MiniGame:AutoInput";
                state.LastReason = "auto:VisibleMiniGameInput";
                state.NativeOwner = "FishingGameScrollBar.UpdateGame";
                state.LastNativeAction = "decision=" + decision + ", note=" + noteType;
                LastFishingMiniGameCompleteSummary = "owner=" + ownerId +
                    ", behavior=AutoPlayVisibleMiniGame" +
                    ", status=Running" +
                    ", decision=" + decision +
                    ", note=" + noteType +
                    ", noteIndex=" + noteIndex.ToString(CultureInfo.InvariantCulture) +
                    ", currentTime=" + currentTime.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", noteStart=" + noteStart.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", noteEnd=" + noteEnd.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", " + stats.Format();
            }
            catch (Exception ex)
            {
                fishingMiniGameInputOverride = null;
                RecordFishingAutomationFailure("FishingAutomation.MiniGame.Update", ex, true, "Smoke.AutoFishingMiniGameComplete", "FishingGameScrollBar.UpdateGame Prefix");
            }
        }

        private void ObserveFishingMiniGameAutomationResult(object gameHandle)
        {
            if (!TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return;
            if (options.ResultMode != FishingResultMode.AutoCompleteVisibleMiniGame)
                return;

            try
            {
                if (!TryReadFishingGameStatus(gameHandle, out _, out string currentStatus))
                    return;

                if (currentStatus.Equals("Running", StringComparison.OrdinalIgnoreCase))
                {
                    RecordFishingAutomationSuccess("FishingAutomation.MiniGame.Update");
                    return;
                }

                if (!fishingMiniGameCompletedHandles.Add(gameHandle))
                    return;

                fishingMiniGameStartedAt.Remove(gameHandle);
                FishingMiniGameInputStats stats = GetFishingMiniGameInputStats(gameHandle);
                bool success = currentStatus.Equals("Success", StringComparison.OrdinalIgnoreCase);
                FishingAutomationApplicationCount++;
                if (success)
                    FishingMiniGameCompleteApplicationCount++;

                state.Phase = success ? "MiniGame:AutoComplete" : "MiniGame:NativeResult";
                state.LastReason = success ? "auto:VisibleMiniGameInput:success" : "auto:VisibleMiniGameInput:" + currentStatus;
                state.NativeOwner = "FishingGameScrollBar.UpdateGame";
                state.LastNativeAction = "native-status=" + currentStatus;
                state.LastResult = currentStatus;
                LastFishingAutomationApplicationSummary = "owner=" + ownerId +
                    ", behavior=AutoPlayVisibleMiniGame" +
                    ", status=" + currentStatus +
                    ", skip=false" +
                    ", " + stats.Format() +
                    ", applications=" + FishingAutomationApplicationCount.ToString(CultureInfo.InvariantCulture);
                LastFishingMiniGameCompleteSummary = LastFishingAutomationApplicationSummary;
                runtime.RuntimeMonitor.Log("Fishing automation visible minigame input reached native status by " + ownerId + " status=" + currentStatus + " " + stats.Format() + ".");
                runtime.SetHookStatus("Smoke.AutoFishingMiniGameComplete", success ? "verified" : "failed", "FishingGameScrollBar.UpdateGame", LastFishingAutomationApplicationSummary);
                RecordFishingAutomationSuccess("FishingAutomation.MiniGame.Update");
            }
            catch (Exception ex)
            {
                RecordFishingAutomationFailure("FishingAutomation.MiniGame.Update", ex, true, "Smoke.AutoFishingMiniGameComplete", "FishingGameScrollBar.UpdateGame Postfix");
            }
        }

        internal bool TryOverrideFishingMiniGameInput(string inputName, out bool value)
        {
            value = false;
            FishingMiniGameInputOverride? current = fishingMiniGameInputOverride;
            if (current == null)
                return false;
            if (DateTimeOffset.UtcNow > current.ExpiresAtUtc)
            {
                fishingMiniGameInputOverride = null;
                return false;
            }

            switch (current.Decision)
            {
                case FishingMiniGameInputDecision.HoldStable:
                    value = inputName.EndsWith("InProgress", StringComparison.OrdinalIgnoreCase);
                    return true;
                case FishingMiniGameInputDecision.TapBonus:
                    value = !inputName.EndsWith("InProgress", StringComparison.OrdinalIgnoreCase);
                    return true;
                default:
                    value = false;
                    return true;
            }
        }

        private void ClearFishingMiniGameAutomationInput(object gameHandle)
        {
            if (fishingMiniGameInputOverride != null && ReferenceEquals(fishingMiniGameInputOverride.GameHandle, gameHandle))
                fishingMiniGameInputOverride = null;
        }

        private static bool TryReadFishingGameStatus(object gameHandle, out Type? statusType, out string status)
        {
            statusType = null;
            status = string.Empty;
            FieldInfo? statusField = null;
            for (Type? type = gameHandle.GetType(); type != null && statusField == null; type = type.BaseType)
                statusField = type.GetField("currentGameStatus", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (statusField == null || !statusField.FieldType.IsEnum)
                return false;

            statusType = statusField.FieldType;
            object? currentStatus = statusField.GetValue(gameHandle);
            status = currentStatus?.ToString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(status);
        }

        private static bool TryPeekCurrentFishingNote(object gameHandle, object noteSpawner, double currentTime, out object? note)
        {
            note = ReadMember(gameHandle, "currentNote");
            if (note != null && currentTime <= ReadDoubleMember(note, "endTime", -1d))
                return true;

            int noteIndex = ReadIntMember(gameHandle, "currentNoteIndex", 0);
            MethodInfo? getNote = FindMethodInHierarchy(noteSpawner.GetType(), "GetNote", 1);
            if (getNote == null)
                return false;

            note = getNote.Invoke(noteSpawner, new object[] { noteIndex });
            string noteType = note == null ? string.Empty : ReadMember(note, "noteType")?.ToString() ?? string.Empty;
            return note != null && !noteType.Equals("Delay", StringComparison.OrdinalIgnoreCase);
        }

        private static FishingMiniGameInputDecision ResolveFishingMiniGameInputDecision(string noteType, double currentTime, double noteStart, double noteEnd, bool bonusAlreadyTapped)
        {
            if (string.IsNullOrWhiteSpace(noteType) || noteType.Equals("Delay", StringComparison.OrdinalIgnoreCase))
                return FishingMiniGameInputDecision.Release;
            if (currentTime < noteStart || currentTime > noteEnd)
                return FishingMiniGameInputDecision.Release;
            if (noteType.Equals("Stable", StringComparison.OrdinalIgnoreCase))
                return FishingMiniGameInputDecision.HoldStable;
            if (noteType.Equals("Bonus", StringComparison.OrdinalIgnoreCase) && !bonusAlreadyTapped)
                return FishingMiniGameInputDecision.TapBonus;
            return FishingMiniGameInputDecision.Release;
        }

        private static double ReadUnityTime()
        {
            Type? timeType = ResolveType("UnityEngine.Time, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Time, UnityEngine");
            object? value = timeType?.GetProperty("time", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            return value == null ? 0d : Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        private FishingMiniGameInputStats GetFishingMiniGameInputStats(object gameHandle)
        {
            if (!fishingMiniGameInputStats.TryGetValue(gameHandle, out FishingMiniGameInputStats stats))
            {
                stats = new FishingMiniGameInputStats();
                fishingMiniGameInputStats[gameHandle] = stats;
            }
            return stats;
        }

        private static object? ResolveFishingRodForAutomation(Type? dolocApi)
        {
            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            if (selectedItem != null && IsTypeOrBase(selectedItem.GetType(), "DolocTown.ItemFishingRod"))
                return selectedItem;
            return null;
        }

        private static string DescribeAgentState(Type? dolocApi)
        {
            object? agent = ReadStaticMember(dolocApi, "agent");
            object? stateManager = agent == null ? null : ReadMember(agent, "StateManager");
            object? current = stateManager == null ? null : ReadMember(stateManager, "current");
            return current == null ? "unknown" : current.GetType().Name;
        }

        private static bool HasFishingPoolInScene()
        {
            try
            {
                Type? poolType = ResolveType("DolocTown.FishingPool, Assembly-CSharp");
                Type? objectType = ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Object, UnityEngine");
                MethodInfo? findObjects = objectType?.GetMethod("FindObjectsOfType", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type) }, null);
                object? result = poolType == null ? null : findObjects?.Invoke(null, new object[] { poolType });
                if (result is Array array)
                    return array.Length > 0;
                return EnumerateObjects(result).Any();
            }
            catch
            {
                return true;
            }
        }

        private void ShowFishingFeedback(string message, bool error)
        {
            if ((DateTimeOffset.Now - lastFishingFeedbackAt).TotalSeconds < 1.25)
                return;
            lastFishingFeedbackAt = DateTimeOffset.Now;
            ShowNativeSmallMessage(message, error);
        }

        private void LogOnce(HashSet<string> keys, string key, string message)
        {
            if (!keys.Add(key))
                return;
            runtime.RuntimeMonitor.Log(message);
        }

        private static void AddAnimatorCandidates(List<KeyValuePair<object, string>> animators, object owner, string label)
        {
            foreach (string member in new[] { "animator", "_animator", "Animator" })
            {
                object? animator = ReadMember(owner, member);
                if (IsAnimatorCandidate(animator))
                    animators.Add(new KeyValuePair<object, string>(animator!, label + "." + member));
            }

            object? component = TryGetUnityComponent(owner, "UnityEngine.Animator, UnityEngine.AnimationModule")
                ?? TryGetUnityComponent(owner, "UnityEngine.Animator, UnityEngine");
            if (IsAnimatorCandidate(component))
                animators.Add(new KeyValuePair<object, string>(component!, label + ".GetComponent"));

            object? gameObject = ReadMember(owner, "gameObject");
            if (gameObject != null)
            {
                component = TryGetUnityComponent(gameObject, "UnityEngine.Animator, UnityEngine.AnimationModule")
                    ?? TryGetUnityComponent(gameObject, "UnityEngine.Animator, UnityEngine");
                if (IsAnimatorCandidate(component))
                    animators.Add(new KeyValuePair<object, string>(component!, label + ".gameObject.GetComponent"));
            }
        }

        private static bool IsAnimatorCandidate(object? value)
        {
            return value != null && value.GetType().GetProperty("speed", BindingFlags.Public | BindingFlags.Instance) != null;
        }

        private static object? TryGetUnityComponent(object owner, string componentTypeName)
        {
            Type? componentType = ResolveType(componentTypeName);
            if (componentType == null)
                return null;

            MethodInfo? getComponent = owner.GetType().GetMethod("GetComponent", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type) }, null);
            if (getComponent != null)
            {
                try
                {
                    return getComponent.Invoke(owner, new object[] { componentType });
                }
                catch
                {
                }
            }

            MethodInfo? generic = owner.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "GetComponent" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            if (generic == null)
                return null;

            try
            {
                return generic.MakeGenericMethod(componentType).Invoke(owner, null);
            }
            catch
            {
                return null;
            }
        }

        private int ApplyAnimatorSpeed(object? animator, double multiplier, string label, List<string> samples)
        {
            if (animator == null)
                return 0;

            double original = ReadAnimatorSpeed(animator, 1);
            if (!originalAnimatorSpeeds.ContainsKey(animator))
                originalAnimatorSpeeds[animator] = original;
            else
                original = originalAnimatorSpeeds[animator];

            double target = original * multiplier;
            if (!TryWriteAnimatorSpeed(animator, target))
                return 0;

            if (samples.Count < 6)
                samples.Add(label + ":" + original.ToString("0.###") + "->" + target.ToString("0.###"));
            return 1;
        }

        internal void RestoreExperimentalAnimatorSpeeds(string reason)
        {
            if (originalAnimatorSpeeds.Count == 0 && originalHookGravityScales.Count == 0)
                return;

            int restored = 0;
            foreach (KeyValuePair<object, double> entry in new List<KeyValuePair<object, double>>(originalAnimatorSpeeds))
            {
                if (TryWriteAnimatorSpeed(entry.Key, entry.Value))
                    restored++;
            }
            originalAnimatorSpeeds.Clear();

            int hookPhysicsRestored = 0;
            foreach (KeyValuePair<object, double> entry in new List<KeyValuePair<object, double>>(originalHookGravityScales))
            {
                if (SetMemberValue(entry.Key, "gravityScale", (float)entry.Value))
                    hookPhysicsRestored++;
            }
            originalHookGravityScales.Clear();

            runtime.RuntimeMonitor.Log("Experimental fishing animation state restored reason=" + reason + " animators=" + restored + " hookPhysics=" + hookPhysicsRestored + ".");
            runtime.SetHookStatus("Smoke.AutoFishingAnimationSpeedRestore", "experimental", "Fishing/AgentState lifecycle boundary", "reason=" + reason + ", restored=" + restored + ", hookPhysics=" + hookPhysicsRestored);
        }

        internal void ResetFishingRuntimeState(string reason)
        {
            int miniGameHandles = fishingMiniGameStartedAt.Count;
            int miniGameInputHandles = fishingMiniGameInputStats.Count;
            int loggedPhaseCount = loggedFishingPhases.Count;
            int failureEpisodes = fishingAutomationFailures.Count;
            bool smokeOverrides = SuppressFishingAutoCastForSmoke ||
                ForceFishingNoWaterForSmoke ||
                ForceFishingNoRodForSmoke ||
                ForceFishingFishForSmoke ||
                ForceFishingNativeBiteForSmoke ||
                FishingPoolOverrideForSmoke != null;

            fishingMiniGameStartedAt.Clear();
            fishingMiniGameBonusTaps.Clear();
            fishingMiniGameInputStats.Clear();
            fishingMiniGameCompletedHandles.Clear();
            fishingReadyChargeAppliedStates.Clear();
            fishingReadyChargeTargetStates.Clear();
            fishingReadyChargeReleasedStates.Clear();
            fishingMiniGameInputOverride = null;
            currentFishingReadyChargeState = null;
            loggedFishingPhases.Clear();
            fishingAutomationFailures.Clear();
            lastFishingAutoCastAt = DateTimeOffset.MinValue;
            lastFishingFeedbackAt = DateTimeOffset.MinValue;
            lastFishingPullDurationResultScaleAtUtc = DateTimeOffset.MinValue;
            LastFishingAutomationApplicationSummary = string.Empty;
            LastFishingMiniGameCompleteSummary = string.Empty;
            LastFishingAnimationSpeedSummary = string.Empty;
            LastFishingAutoCastAttemptSummary = string.Empty;
            SuppressFishingAutoCastForSmoke = false;
            ForceFishingNoWaterForSmoke = false;
            ForceFishingNoRodForSmoke = false;
            ForceFishingFishForSmoke = false;
            ForceFishingNativeBiteForSmoke = false;
            FishingPoolOverrideForSmoke = null;

            RestoreExperimentalAnimatorSpeeds(reason);
            runtime.RuntimeMonitor.Log("Fishing automation runtime state reset reason=" + reason +
                " miniGameHandles=" + miniGameHandles.ToString(CultureInfo.InvariantCulture) +
                " miniGameInputHandles=" + miniGameInputHandles.ToString(CultureInfo.InvariantCulture) +
                " loggedPhases=" + loggedPhaseCount.ToString(CultureInfo.InvariantCulture) +
                " failureEpisodes=" + failureEpisodes.ToString(CultureInfo.InvariantCulture) +
                " smokeOverrides=" + (smokeOverrides ? "true" : "false") + ".");
        }

        private void RecordFishingAutomationFailure(string operation, Exception ex, bool highFrequency, string? hookId = null, string? source = null)
        {
            FishingAutomationFailurePublication publication = highFrequency
                ? RecordFishingAutomationFailurePublication(operation, ex)
                : new FishingAutomationFailurePublication(true, FishingAutomationFailureLogMode.Full, 1);

            if (publication.RecordDiagnosticsError)
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.FishingAutomation", "Fishing automation service failed: " + operation + ".", ex.ToString());

            if (publication.LogMode == FishingAutomationFailureLogMode.Full)
                runtime.RuntimeMonitor.Log("Fishing automation service failed operation=" + operation + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Error);
            else if (publication.LogMode == FishingAutomationFailureLogMode.Short)
                runtime.RuntimeMonitor.Log("Repeated FishingAutomation service failure operation=" + operation + " count=" + publication.Count.ToString(CultureInfo.InvariantCulture) + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            else if (publication.LogMode == FishingAutomationFailureLogMode.Summary)
                runtime.RuntimeMonitor.Log("Throttled FishingAutomation service failures operation=" + operation + " count=" + publication.Count.ToString(CultureInfo.InvariantCulture) + " lastError=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);

            if (!string.IsNullOrWhiteSpace(hookId) && publication.ShouldPublishHookStatus)
            {
                string actualHookId = hookId!;
                string actualSource = string.IsNullOrWhiteSpace(source) ? operation : source!;
                string details = ex.GetType().Name + ": " + ex.Message + "; operation=" + operation + "; failureCount=" + publication.Count.ToString(CultureInfo.InvariantCulture) + ".";
                runtime.SetHookStatus(actualHookId, "failed", actualSource, details);
            }
        }

        private FishingAutomationFailurePublication RecordFishingAutomationFailurePublication(string operation, Exception ex)
        {
            string key = string.IsNullOrWhiteSpace(operation) ? "<unknown>" : operation;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (!fishingAutomationFailures.TryGetValue(key, out FishingAutomationFailureState state))
            {
                state = new FishingAutomationFailureState();
                fishingAutomationFailures[key] = state;
            }

            state.Count++;
            state.ConsecutiveSuccessCount = 0;
            state.LastError = ex.GetType().Name + ": " + ex.Message;
            state.LastSeenAtUtc = now;

            if (state.Count == 1)
            {
                state.LastPublishedAtUtc = now;
                return new FishingAutomationFailurePublication(true, FishingAutomationFailureLogMode.Full, state.Count);
            }

            if (state.Count <= FishingAutomationFailureShortLogLimit)
            {
                state.LastPublishedAtUtc = now;
                return new FishingAutomationFailurePublication(false, FishingAutomationFailureLogMode.Short, state.Count);
            }

            if (now - state.LastPublishedAtUtc >= FishingAutomationFailureSummaryInterval)
            {
                state.LastPublishedAtUtc = now;
                return new FishingAutomationFailurePublication(false, FishingAutomationFailureLogMode.Summary, state.Count);
            }

            return new FishingAutomationFailurePublication(false, FishingAutomationFailureLogMode.None, state.Count);
        }

        private void RecordFishingAutomationSuccess(string operation)
        {
            string key = string.IsNullOrWhiteSpace(operation) ? "<unknown>" : operation;
            if (!fishingAutomationFailures.TryGetValue(key, out FishingAutomationFailureState state))
                return;

            state.ConsecutiveSuccessCount++;
            if (state.ConsecutiveSuccessCount >= 3)
                fishingAutomationFailures.Remove(key);
        }

        private sealed class FishingMiniGameInputOverride
        {
            public FishingMiniGameInputOverride(object gameHandle, FishingMiniGameInputDecision decision, DateTimeOffset expiresAtUtc)
            {
                GameHandle = gameHandle;
                Decision = decision;
                ExpiresAtUtc = expiresAtUtc;
            }

            public object GameHandle { get; }

            public FishingMiniGameInputDecision Decision { get; }

            public DateTimeOffset ExpiresAtUtc { get; }
        }

        private sealed class FishingMiniGameInputStats
        {
            public int HoldStableFrames { get; private set; }

            public int BonusTapFrames { get; private set; }

            public int ReleaseFrames { get; private set; }

            public string LastNoteType { get; private set; } = string.Empty;

            public void Record(FishingMiniGameInputDecision decision, string noteType)
            {
                LastNoteType = noteType ?? string.Empty;
                switch (decision)
                {
                    case FishingMiniGameInputDecision.HoldStable:
                        HoldStableFrames++;
                        break;
                    case FishingMiniGameInputDecision.TapBonus:
                        BonusTapFrames++;
                        break;
                    default:
                        ReleaseFrames++;
                        break;
                }
            }

            public string Format()
            {
                return "stableHoldFrames=" + HoldStableFrames.ToString(CultureInfo.InvariantCulture) +
                    ", bonusTapFrames=" + BonusTapFrames.ToString(CultureInfo.InvariantCulture) +
                    ", releaseFrames=" + ReleaseFrames.ToString(CultureInfo.InvariantCulture) +
                    ", lastNote=" + (string.IsNullOrWhiteSpace(LastNoteType) ? "none" : LastNoteType);
            }
        }

        private sealed class FishingAutomationFailureState
        {
            public int Count { get; set; }
            public int ConsecutiveSuccessCount { get; set; }
            public string LastError { get; set; } = string.Empty;
            public DateTimeOffset LastSeenAtUtc { get; set; }
            public DateTimeOffset LastPublishedAtUtc { get; set; }
        }

        private readonly struct FishingAutomationFailurePublication
        {
            public FishingAutomationFailurePublication(bool recordDiagnosticsError, FishingAutomationFailureLogMode logMode, int count)
            {
                RecordDiagnosticsError = recordDiagnosticsError;
                LogMode = logMode;
                Count = count;
            }

            public bool RecordDiagnosticsError { get; }

            public FishingAutomationFailureLogMode LogMode { get; }

            public int Count { get; }

            public bool ShouldPublishHookStatus => LogMode != FishingAutomationFailureLogMode.None;
        }

        private enum FishingAutomationFailureLogMode
        {
            None,
            Full,
            Short,
            Summary
        }
    }
}
