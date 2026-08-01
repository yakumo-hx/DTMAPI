#pragma warning disable CS0618 // Frozen IFishingAutomationApi compatibility implementation.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.GameBridgeNativeHelpers;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class LegacyFishingAutomationService : IFishingCompatibilityHookRuntime
    {
        private const int FishingAutomationFailureShortLogLimit = 3;
        private const int FishingAutomationDiagnosticInitialPublishLimit = 1;
        private static readonly TimeSpan FishingAutomationFailureSummaryInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan FishingAutomationDiagnosticSummaryInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan FishingAutoCastPendingTimeout = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan FishingAutoCastStallBackoff = TimeSpan.FromSeconds(2.5);

        private enum FishingBiteAutomationAction
        {
            None,
            NativeReel,
            SkipMiniGameNativeResult
        }

        private readonly DtmApiRuntime runtime;
        private readonly Dictionary<string, FishingAutomationOptions> fishingOptions = new Dictionary<string, FishingAutomationOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FishingAutomationState> fishingStates = new Dictionary<string, FishingAutomationState>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> legacyStopOnManualMoveWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedFishingPhases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<object, DateTimeOffset> fishingMiniGameStartedAt = new Dictionary<object, DateTimeOffset>();
        private readonly Dictionary<object, HashSet<int>> fishingMiniGameBonusTaps = new Dictionary<object, HashSet<int>>();
        private readonly Dictionary<object, FishingMiniGameInputStats> fishingMiniGameInputStats = new Dictionary<object, FishingMiniGameInputStats>();
        private readonly HashSet<object> fishingMiniGameCompletedHandles = new HashSet<object>();
        private readonly HashSet<object> fishingReadyChargeAppliedStates = new HashSet<object>();
        private readonly HashSet<object> fishingReadyChargeTargetStates = new HashSet<object>();
        private readonly HashSet<object> fishingReadyChargeReleasedStates = new HashSet<object>();
        private readonly FishingCompatibilityAnimationController animationController = new FishingCompatibilityAnimationController();
        private readonly Dictionary<object, double> originalAnimatorSpeeds;
        private readonly Dictionary<object, double> originalHookGravityScales;
        private readonly Dictionary<string, FishingAutomationFailureState> fishingAutomationFailures = new Dictionary<string, FishingAutomationFailureState>(StringComparer.Ordinal);
        private readonly Dictionary<FishingDiagnosticKey, FishingAutomationDiagnosticState> fishingAutomationDiagnostics = new Dictionary<FishingDiagnosticKey, FishingAutomationDiagnosticState>();
        private readonly FishingCompatibilityInputOverride fishingMiniGameInputOverride = new FishingCompatibilityInputOverride();
        private readonly FishingCompatibilityLifecyclePublicationGate fishingLifecyclePublicationGate = new FishingCompatibilityLifecyclePublicationGate();
        private object? currentFishingReadyChargeState;
        private object? lastFishingReadyChargePendingState;
        private string lastFishingReadyChargePendingReason = string.Empty;
        private DateTimeOffset lastFishingPullDurationResultScaleAtUtc = DateTimeOffset.MinValue;
        private bool fishingHooksInstalled;
        private DateTimeOffset lastFishingAutoCastAt = DateTimeOffset.MinValue;
        private DateTimeOffset lastFishingFeedbackAt = DateTimeOffset.MinValue;
        private DateTimeOffset pendingFishingAutoCastAtUtc = DateTimeOffset.MinValue;
        private DateTimeOffset lastFishingLifecycleEnvironmentResetPublishedAtUtc = DateTimeOffset.MinValue;
        private long nativeObjectSearchCount;
        private DateTimeOffset lastObservedFishingPhaseAtUtc = DateTimeOffset.MinValue;
        private string pendingFishingAutoCastOwnerId = string.Empty;
        private string pendingFishingAutoCastSummary = string.Empty;
        private string pendingFishingAutoCastAgentState = string.Empty;
        private string lastObservedFishingPhase = string.Empty;
        private bool pendingFishingAutoCastStalled;
        private int pendingFishingAutoCastStallCount;
        private DateTimeOffset suppressFishingAutoCastUntilUtc = DateTimeOffset.MinValue;
        private int fishingAutoCastAttempts;
        private int fishingAutoCastApplications;

        internal LegacyFishingAutomationService(DtmApiRuntime runtime)
        {
            this.runtime = runtime;
            originalAnimatorSpeeds = animationController.AnimatorSnapshots;
            originalHookGravityScales = animationController.HookGravitySnapshots;
        }

        internal int FishingAutomationApplicationCount { get; private set; }

        internal string LastFishingAutomationApplicationSummary { get; private set; } = string.Empty;

        internal string LastFishingMiniGameCompleteSummary { get; private set; } = string.Empty;

        internal string LastFishingAnimationSpeedSummary { get; private set; } = string.Empty;

        internal int FishingAutoCastApplicationCount => fishingAutoCastApplications;

        internal bool HasPendingFishingAutoCast => pendingFishingAutoCastAtUtc != DateTimeOffset.MinValue;
        internal int ReadyAccessorFailureCount => animationController.ReadyAccessorFailures;
        public int ReadyAccessorBuildCount => animationController.ReadyAccessorBuilds;
        public int ReadyAccessorRebuildCount => animationController.ReadyAccessorRebuilds;
        public int ReadyAccessorBuildFailureCount => animationController.ReadyAccessorBuildFailures;

        internal long NativeObjectSearchCount => nativeObjectSearchCount;
        public int ReadyAccessorInvocationFailureCount => animationController.ReadyAccessorInvocationFailures;

        internal bool HasEnabledOwner => TryFindEnabledFishingOptions(out _, out _, out _);

        public bool HasActiveRuntimeConsumer => HasEnabledOwner;

        internal int ConfiguredOwnerCount => fishingOptions.Count;

        internal int OwnerStateCount => fishingStates.Count;

        internal int FishingAutoCastPendingStallCount => pendingFishingAutoCastStallCount;

        internal int FishingInstantBiteApplicationCount { get; private set; }

        internal int FishingSkipMiniGameApplicationCount { get; private set; }

        internal int FishingReadyChargeApplicationCount { get; private set; }

        internal int FishingReadyChargeTargetApplicationCount { get; private set; }

        internal string LastFishingAutoCastAttemptSummary { get; private set; } = string.Empty;

        internal string LastFishingAutomationLifecycleStatus { get; private set; } = "not-observed";

        internal string LastFishingAutomationLifecycleSummary { get; private set; } = "AutoFishing lifecycle audit has not observed a boundary yet.";
        internal long FishingNativeExitObservedCount => fishingLifecyclePublicationGate.GetObservedCount(FishingCompatibilityLifecycleEvent.NativeExit);

        internal int FishingMiniGameCompleteApplicationCount { get; private set; }

        internal void Update()
        {
            CheckFishingAutoCastWatchdog(DateTimeOffset.UtcNow);
            UpdateFishingAutoCast();
        }

        public void SetFishingHooksInstalled(bool installed)
        {
            fishingHooksInstalled = installed;
        }

        public bool FishingHooksInstalled => fishingHooksInstalled;

        public void NotifyFishingNativeFrame()
        {
        }

        public void Configure(IManifest owner, FishingAutomationOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            fishingOptions[owner.UniqueID] = NormalizeFishingAutomationOptions(options);
            if (!fishingStates.ContainsKey(owner.UniqueID))
                fishingStates[owner.UniqueID] = new FishingAutomationState();
            if (legacyStopOnManualMoveWarnings.Add(owner.UniqueID))
                runtime.RuntimeMonitor.Log("FishingAutomationOptions.StopOnManualMove was retained for binary compatibility for owner=" + owner.UniqueID + "; movement cancellation remains consumer policy.", LogLevel.Warn);
            ObserveFishingOwnerResource(owner.UniqueID, "Configure");
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
            if (enabled && TryGetEnabledFishingAutomationOwner(out string activeOwnerId) &&
                !activeOwnerId.Equals(owner.UniqueID, StringComparison.OrdinalIgnoreCase))
            {
                state.Enabled = false;
                state.Phase = "Busy";
                state.LastReason = "busy: active-owner=" + activeOwnerId;
                runtime.RuntimeMonitor.Log("Fishing automation enable rejected owner=" + owner.UniqueID + " activeOwner=" + activeOwnerId + ".", LogLevel.Warn);
                return;
            }
            state.Enabled = enabled;
            state.Phase = enabled ? "Starting" : "Idle";
            state.LastReason = reason ?? string.Empty;
            if (!enabled)
            {
                ClearFishingAutomationTransientState("disabled:" + state.LastReason);
                PublishFishingLifecycleBoundary("SetEnabled(false)", "disabled:" + state.LastReason, expectTransientClear: true, logIfOk: true);
            }
            ObserveFishingOwnerStateResource(owner.UniqueID, enabled, "SetEnabled");
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

        internal BridgeFeatureStatus GetStatus(string uniqueId)
        {
            string ownerId = uniqueId ?? string.Empty;
            if (!fishingOptions.ContainsKey(ownerId))
                return new BridgeFeatureStatus("inactive/no-consumer", "No fishing automation policy was registered for this mod.");
            if (fishingStates.TryGetValue(ownerId, out FishingAutomationState state))
            {
                if (state.Enabled)
                    return new BridgeFeatureStatus("active", fishingHooksInstalled ? "Native-loop policy is enabled and fishing phase hooks are installed; automatic reel, visible minigame completion, native skip-result routing, and cast/pull animation speed remain experimental." : "Native-loop policy is enabled, but fishing phase and input hooks are not yet installed.");
                if (string.IsNullOrWhiteSpace(state.LastReason) && string.IsNullOrWhiteSpace(state.NativeOwner) && string.IsNullOrWhiteSpace(state.LastNativeAction))
                    return new BridgeFeatureStatus("configured", fishingHooksInstalled ? "Native-loop policy accepted and fishing phase hooks are installed." : "Policy accepted; fishing phase and input hooks are not yet installed.");
                return new BridgeFeatureStatus("disabled/consumer-present", fishingHooksInstalled ? "Fishing automation policy is configured for this consumer, but the feature is disabled." : "Fishing automation policy is configured for this consumer, but the feature is disabled while hooks are pending.");
            }

            return new BridgeFeatureStatus("configured", fishingHooksInstalled ? "Native-loop policy accepted and fishing phase hooks are installed." : "Policy accepted; fishing phase and input hooks are not yet installed.");
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
            DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
            if (HasPendingFishingAutoCast)
            {
                CheckFishingAutoCastWatchdog(nowUtc);
                if (!HasPendingFishingAutoCast)
                    return;
                LastFishingAutoCastAttemptSummary = "skipped=pending-cast owner=" + pendingFishingAutoCastOwnerId +
                    ", ageSeconds=" + GetPendingFishingAutoCastAgeSeconds(nowUtc).ToString("0.###", CultureInfo.InvariantCulture) +
                    ", stalled=" + pendingFishingAutoCastStalled.ToString(CultureInfo.InvariantCulture);
                return;
            }

            if (suppressFishingAutoCastUntilUtc > nowUtc)
            {
                LastFishingAutoCastAttemptSummary = "skipped=stall-backoff until=" + suppressFishingAutoCastUntilUtc.ToString("O", CultureInfo.InvariantCulture);
                return;
            }

            double recastDelay = 0.5;
            if (TryFindEnabledFishingOptions(out _, out FishingAutomationOptions throttleOptions, out _))
                recastDelay = Math.Max(0.05, throttleOptions.RecastDelaySeconds);
            if ((nowUtc - lastFishingAutoCastAt).TotalSeconds < recastDelay)
            {
                LastFishingAutoCastAttemptSummary = "skipped=throttle";
                return;
            }

            if (!TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
            {
                LastFishingAutoCastAttemptSummary = "skipped=no-enabled-policy";
                return;
            }

            if (!fishingHooksInstalled)
            {
                state.Phase = "WaitingHooks";
                state.LastReason = "auto:hooks-not-installed";
                state.NativeOwner = "FishingCompatibilityHookBridge";
                state.LastNativeAction = "wait-hooks";
                LastFishingAutoCastAttemptSummary = "skipped=hooks-not-installed owner=" + ownerId;
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
            object? currentAgentStateHandle = ReadCurrentAgentState(dolocApi);
            string currentAgentState = DescribeAgentState(currentAgentStateHandle);
            if (currentAgentStateHandle != null &&
                currentAgentState.Equals("AgentStateFishingWait", StringComparison.OrdinalIgnoreCase))
            {
                ApplyFishingWaitAutomation(currentAgentStateHandle, "FishingAutomation.Update Poll");
                LastFishingAutoCastAttemptSummary = "skipped=wait-poll owner=" + ownerId + ", state=" + currentAgentState;
                return;
            }

            if (IsObservedFishingLoopActive(lastObservedFishingPhase))
            {
                state.Phase = "WaitingAction";
                state.LastReason = "auto:fishing-loop-active";
                state.NativeOwner = "FishingAutomationPhase";
                state.LastNativeAction = "wait-" + lastObservedFishingPhase;
                LastFishingAutoCastAttemptSummary = "skipped=fishing-loop-active owner=" + ownerId +
                    ", phase=" + lastObservedFishingPhase +
                    ", observedAt=" + lastObservedFishingPhaseAtUtc.ToString("O", CultureInfo.InvariantCulture);
                return;
            }

            if (HasCurrentFishingReadyChargeState())
            {
                state.Phase = "WaitingAction";
                state.LastReason = "auto:busy-ready-charge";
                state.NativeOwner = "AgentStateFishingReady";
                state.LastNativeAction = "wait-ready-charge";
                LastFishingAutoCastAttemptSummary = "skipped=busy-ready-charge owner=" + ownerId + ", state=" + currentAgentState;
                return;
            }

            if (IsFishingAutomationNativeStateBusy(currentAgentState))
            {
                state.Phase = "WaitingAction";
                state.LastReason = "auto:busy-fishing";
                state.NativeOwner = "AgentStateManager.current";
                state.LastNativeAction = "wait-fishing-state";
                LastFishingAutoCastAttemptSummary = "skipped=busy-fishing owner=" + ownerId + ", state=" + currentAgentState;
                return;
            }

            if (agent != null && !ReadBoolMember(agent, "IsCurrentStateSupportUseItem", true))
            {
                state.Phase = "WaitingAction";
                state.LastReason = "auto:busy";
                state.NativeOwner = "BodyController.IsCurrentStateSupportUseItem";
                state.LastNativeAction = "wait-action";
                LastFishingAutoCastAttemptSummary = "skipped=busy owner=" + ownerId + ", state=" + DescribeAgentState(dolocApi);
                return;
            }

            if (!HasFishingPoolInScene())
            {
                state.Phase = "NoWater";
                state.LastReason = "auto:no-fishing-pool";
                state.NativeOwner = "FishingPool";
                state.LastNativeAction = "find-pool";
                LastFishingAutoCastAttemptSummary = "skipped=no-water owner=" + ownerId;
                return;
            }

            object? rod = ResolveFishingRodForAutomation(dolocApi);
            if (rod == null)
            {
                state.Phase = "NoRod";
                state.LastReason = "auto:no-selected-rod";
                state.NativeOwner = "DolocAPI.SelectedItem";
                state.LastNativeAction = "resolve-selected-rod";
                LastFishingAutoCastAttemptSummary = "skipped=no-rod owner=" + ownerId + ", requireSelected=true";
                return;
            }

            MethodInfo? useFishRod = agent?.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "UseFishRod" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsAssignableFrom(rod.GetType()));
            if (useFishRod == null)
            {
                LastFishingAutoCastAttemptSummary = "skipped=missing-UseFishRod owner=" + ownerId + ", agent=" + (agent == null ? "null" : agent.GetType().FullName) + ", rod=" + rod.GetType().FullName;
                return;
            }

            int nextAttemptCount = fishingAutoCastAttempts + 1;
            string castSummary = "owner=" + ownerId + ", behavior=AutoCast, rod=" + ReadStringMember(rod, "name") + ", attempts=" + nextAttemptCount.ToString(CultureInfo.InvariantCulture) + ", confirmedApplications=" + fishingAutoCastApplications.ToString(CultureInfo.InvariantCulture);
            lastFishingAutoCastAt = nowUtc;
            MarkFishingAutoCastPending(ownerId, castSummary, DescribeAgentState(dolocApi), nowUtc);
            try
            {
                useFishRod.Invoke(agent, new[] { rod });
                fishingAutoCastAttempts++;
                if (HasPendingFishingAutoCast)
                {
                    state.Phase = "AutoCast";
                    state.LastReason = "auto:UseFishRod";
                    state.NativeOwner = "BodyController.UseFishRod";
                    state.LastNativeAction = "UseFishRod";
                    LastFishingAutoCastAttemptSummary = castSummary + ", attempted=true, awaitingPhaseConfirmation=true";
                    PublishFishingAutomationLog("AutoCast.Attempted", "Fishing automation auto-cast attempted native BodyController.UseFishRod summary=" + LastFishingAutoCastAttemptSummary + ".");
                    PublishFishingAutomationHookStatus("Smoke.AutoFishingAutoCast", "pending", "BodyController.UseFishRod", LastFishingAutoCastAttemptSummary, "AutoCast.Pending");
                }
                else
                {
                    PublishFishingAutomationLog("AutoCast.SynchronousAdvance", "Fishing automation auto-cast native BodyController.UseFishRod synchronously advanced to a fishing phase; keeping confirmed phase status.");
                }
                RecordFishingAutomationSuccess("FishingAutomation.AutoCast");
            }
            catch (Exception ex)
            {
                DiscardPendingFishingAutoCast("failed:" + ex.GetType().Name, "Fishing automation auto-cast failure");
                state.Phase = "AutoCastFailed";
                state.LastReason = ex.GetType().Name;
                LastFishingAutoCastAttemptSummary = "failed=" + ex.GetType().Name + ": " + ex.Message;
                RecordFishingAutomationFailure("FishingAutomation.AutoCast", ex, highFrequency: true);
            }
        }

        internal int RemoveOwner(string ownerId, string reason)
        {
            ownerId = ownerId ?? string.Empty;
            bool wasEnabled = fishingStates.TryGetValue(ownerId, out FishingAutomationState state) && state.Enabled;
            int removed = 0;
            if (fishingOptions.Remove(ownerId))
                removed++;
            if (fishingStates.Remove(ownerId))
                removed++;
            if (wasEnabled)
                ClearFishingAutomationTransientState("owner-cleanup:" + FirstText(reason, "unknown"));
            return removed;
        }

        internal int RemoveAllOwners(string reason)
        {
            string[] ownerIds = fishingOptions.Keys
                .Concat(fishingStates.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            int removed = 0;
            foreach (string ownerId in ownerIds)
                removed += RemoveOwner(ownerId, reason);
            return removed;
        }

        internal int CountOwnerResources(string ownerId)
        {
            ownerId ??= string.Empty;
            return (fishingOptions.ContainsKey(ownerId) ? 1 : 0) + (fishingStates.ContainsKey(ownerId) ? 1 : 0);
        }

        private static bool IsFishingAutomationNativeStateBusy(string currentAgentState)
        {
            if (string.IsNullOrWhiteSpace(currentAgentState) || currentAgentState.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                return false;

            return currentAgentState.IndexOf("AgentStateFishing", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool HasCurrentFishingReadyChargeState()
        {
            return currentFishingReadyChargeState != null ||
                fishingReadyChargeTargetStates.Count > 0 ||
                fishingReadyChargeAppliedStates.Count > 0 ||
                fishingReadyChargeReleasedStates.Count > 0;
        }

        private static bool IsObservedFishingLoopActive(string phase)
        {
            if (string.IsNullOrWhiteSpace(phase))
                return false;
            if (phase.Equals("Cooldown", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("NativeExit", StringComparison.OrdinalIgnoreCase))
                return false;
            return phase.Equals("Ready", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("Cast", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("Wait", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("Battle", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("MiniGame", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("MiniGameStop", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("Pull", StringComparison.OrdinalIgnoreCase);
        }

        public void NotifyFishingPhase(string phase, object? source)
        {
            if (string.IsNullOrWhiteSpace(phase))
                return;

            lastObservedFishingPhase = phase;
            lastObservedFishingPhaseAtUtc = DateTimeOffset.UtcNow;
            ConfirmPendingFishingAutoCast(phase, source?.GetType().Name ?? "FishingState");

            if (fishingStates.Count == 0)
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
            {
                ReleaseFishingReadyChargeStateResources("FishingPhase:" + phase);
                fishingReadyChargeAppliedStates.Clear();
                fishingReadyChargeTargetStates.Clear();
                fishingReadyChargeReleasedStates.Clear();
                currentFishingReadyChargeState = null;
                lastFishingReadyChargePendingState = null;
                lastFishingReadyChargePendingReason = string.Empty;
            }

            if (phase.Equals("Ready", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("Cast", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("Pull", StringComparison.OrdinalIgnoreCase))
                TryApplyFishingAnimationSpeed(phase, source);

            LogOnce(loggedFishingPhases, phase, "Fishing phase hook observed phase=" + phase + " source=" + sourceName + ".");
        }

        public void NotifyFishingMiniGameStart(object? gameHandle)
        {
            if (gameHandle != null)
            {
                fishingMiniGameStartedAt[gameHandle] = DateTimeOffset.Now;
                if (!fishingMiniGameInputStats.ContainsKey(gameHandle))
                    fishingMiniGameInputStats[gameHandle] = new FishingMiniGameInputStats();
                fishingMiniGameCompletedHandles.Remove(gameHandle);
                ObserveFishingNativeHandleResource("MiniGameHandle", gameHandle, "FishingGameScrollBar.StartGame", ResourceLifecycleStatus.Acquired);
            }
            NotifyFishingPhase("MiniGame", gameHandle);
        }

        public void PrepareFishingMiniGameAutomationInput(object? gameHandle)
        {
            fishingMiniGameInputOverride.Clear();
            if (gameHandle == null)
                return;

            TryPrepareFishingMiniGameAutomationInput(gameHandle);
        }

        public void ApplyFishingMiniGameAutomationTick(object? gameHandle)
        {
            if (gameHandle == null)
                return;
            ClearFishingMiniGameAutomationInput(gameHandle);
            if (!fishingMiniGameStartedAt.ContainsKey(gameHandle))
            {
                fishingMiniGameStartedAt[gameHandle] = DateTimeOffset.Now;
                ObserveFishingNativeHandleResource("MiniGameHandle", gameHandle, "FishingGameScrollBar.UpdateGame", ResourceLifecycleStatus.Acquired);
            }
            ObserveFishingMiniGameAutomationResult(gameHandle);
        }

        public void NotifyFishingMiniGameStop(object? gameHandle)
        {
            if (gameHandle != null)
            {
                fishingMiniGameStartedAt.Remove(gameHandle);
                fishingMiniGameBonusTaps.Remove(gameHandle);
                fishingMiniGameInputStats.Remove(gameHandle);
                fishingMiniGameCompletedHandles.Remove(gameHandle);
                ClearFishingMiniGameAutomationInput(gameHandle);
                ReleaseFishingNativeHandleResource("MiniGameHandle", gameHandle, "FishingGameScrollBar.StopGame");
            }
            NotifyFishingPhase("MiniGameStop", gameHandle);
            PublishFishingLifecycleBoundary("MiniGameStop", "FishingGameScrollBar.StopGame", expectTransientClear: false, logIfOk: true);
        }

        internal bool ApplyFishingWaitAutomation(object waitState)
        {
            return ApplyFishingWaitAutomation(waitState, "AgentStateFishingWait.OnPlay Postfix");
        }

        public bool ApplyFishingWaitAutomation(object waitState, string hookSource)
        {
            if (waitState == null)
                return false;

            hookSource = FirstText(hookSource, "AgentStateFishingWait.Postfix");
            if (fishingStates.Count == 0 || fishingOptions.Count == 0)
                return false;

            if (!TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return false;

            bool isOnEnterHook = hookSource.IndexOf("OnEnter", StringComparison.OrdinalIgnoreCase) >= 0;
            string operation = isOnEnterHook ? "FishingAutomation.Wait.OnEnter" : "FishingAutomation.Wait.OnPlay";
            try
            {
                if (operation.Equals("FishingAutomation.Wait.OnPlay", StringComparison.OrdinalIgnoreCase) && !IsCurrentFishingState(waitState))
                    return false;
                if (operation.Equals("FishingAutomation.Wait.OnPlay", StringComparison.OrdinalIgnoreCase))
                    ConfirmPendingFishingAutoCast("Wait", hookSource);

                bool waitingForBite = ReadBoolMember(waitState, "_waitForFishBite", false);
                bool forcedByInstantBite = false;
                bool nativeTipInvoked = false;
                float hookDuration = 0f;
                int rollAttempts = 0;
                bool forceFishSatisfied = true;
                bool instantBite = options.BiteWaitMode == FishingBiteWaitMode.InstantNativeBite;
                bool skipMiniGame = options.ResultMode == FishingResultMode.SkipMiniGameNativeResult;
                if (waitingForBite && instantBite)
                {
                    if (!TryForceFishingBite(waitState, requireFish: false, out rollAttempts, out forceFishSatisfied, out nativeTipInvoked, out hookDuration))
                    {
                        state.Phase = "Wait:AutoBiteFailed";
                        state.LastReason = "auto:InstantBite:no-fish";
                        state.NativeOwner = "AgentStateFishingWait.RollFish";
                        state.LastNativeAction = "force-bite";
                        runtime.SetHookStatus("Smoke.AutoFishingPhase", "failed", hookSource, state.LastReason + "; rollAttempts=" + rollAttempts.ToString(CultureInfo.InvariantCulture) + ".");
                        return false;
                    }

                    forcedByInstantBite = true;
                }

                if (ReadBoolMember(waitState, "_waitForFishBite", false))
                {
                    state.Phase = "WaitingForBite";
                    state.LastReason = instantBite ? "auto:instant-bite-pending" : "auto:native-wait";
                    state.NativeOwner = "AgentStateFishingWait";
                    state.LastNativeAction = "wait-for-bite";
                    return false;
                }

                if (isOnEnterHook)
                {
                    string deferredBehavior = forcedByInstantBite ? "InstantBiteReady" : "BiteReady";
                    state.Phase = "Wait:" + deferredBehavior + ":defer-reel";
                    state.LastReason = "auto:" + deferredBehavior + ":defer-until-OnPlay";
                    state.NativeOwner = "AgentStateFishingWait.OnEnter";
                    state.LastNativeAction = "prepare-bite";
                    state.LastResult = "deferred";
                    LastFishingAutomationApplicationSummary = "owner=" + ownerId +
                        ", behavior=" + deferredBehavior +
                        ", phase=Wait.OnEnter" +
                        ", action=defer-reel" +
                        ", forceFishSatisfied=" + forceFishSatisfied +
                        ", hookDuration=" + hookDuration.ToString("0.###", CultureInfo.InvariantCulture) +
                        ", nativeTipInvoked=" + nativeTipInvoked +
                        ", source=" + hookSource +
                        ", rollAttempts=" + rollAttempts.ToString(CultureInfo.InvariantCulture);
                    PublishFishingAutomationHookStatus("Smoke.AutoFishingPhase", "pending", hookSource, LastFishingAutomationApplicationSummary, "Phase.Pending");
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

                string behavior = forcedByInstantBite ? "InstantBite" : "NativeBiteReady";
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
                    PublishFishingAutomationHookStatus("Smoke.AutoFishingInstantBite", "verified", hookSource, LastFishingAutomationApplicationSummary, "InstantBite.Verified");
                }

                PublishFishingAutomationHookStatus("Smoke.AutoFishingPhase", "verified", hookSource, LastFishingAutomationApplicationSummary, "Phase.Verified");
                if (action == FishingBiteAutomationAction.SkipMiniGameNativeResult && autoHook.Equals("AgentStateFishingPull", StringComparison.OrdinalIgnoreCase))
                    PublishFishingAutomationHookStatus("Smoke.AutoFishingMiniGameSkip", "verified", hookSource + " -> AgentStateFishingPull", LastFishingAutomationApplicationSummary, "MiniGameSkip.Verified");
                else if (action == FishingBiteAutomationAction.NativeReel && autoHook.Equals("AgentStateFishingBattle", StringComparison.OrdinalIgnoreCase))
                    PublishFishingAutomationHookStatus("Smoke.AutoFishingMiniGameComplete", "pending", hookSource + " -> FishingGameScrollBar.UpdateGame", LastFishingAutomationApplicationSummary, "MiniGameComplete.Pending");

                RecordFishingAutomationSuccess(operation);
                return true;
            }
            catch (Exception ex)
            {
                RecordFishingAutomationFailure(operation, ex, true, "Smoke.AutoFishingPhase", hookSource);
                return false;
            }
        }

        public void ConfirmFishingWaitNativeReelAccepted(object waitState, object? nextState)
        {
            // The frozen legacy path performs its native NextState transaction directly and does not use the visible-edge retry state.
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

        internal static bool TryForceFishingBite(object waitState, bool requireFish, out int rollAttempts, out bool forceFishSatisfied, out bool nativeTipInvoked, out float hookDuration)
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

        internal static FishingAutomationOptions NormalizeFishingAutomationOptions(FishingAutomationOptions? options)
        {
            options ??= new FishingAutomationOptions();
            return new FishingAutomationOptions
            {
                BiteWaitMode = Enum.IsDefined(typeof(FishingBiteWaitMode), options.BiteWaitMode) ? options.BiteWaitMode : FishingBiteWaitMode.NativeWait,
                ResultMode = Enum.IsDefined(typeof(FishingResultMode), options.ResultMode) ? options.ResultMode : FishingResultMode.AutoCompleteVisibleMiniGame,
                AnimationMode = Enum.IsDefined(typeof(FishingAnimationMode), options.AnimationMode) ? options.AnimationMode : FishingAnimationMode.Normal,
                RecastDelaySeconds = ClampSeconds(options.RecastDelaySeconds, 0.05, 10),
                CastChargeRatio = ClampCastChargeRatio(options.CastChargeRatio),
                AnimationMultiplier = ClampMultiplier(options.AnimationMultiplier),
                StopOnManualMove = options.StopOnManualMove,
                VerboseLogging = options.VerboseLogging
            };
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
            if (stateSource == null || !TryResolveFishingAnimationPolicy(phase, out string ownerId, out double multiplier, out FishingAutomationState state))
                return;

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
                PublishFishingAutomationHookStatus("Smoke.AutoFishingAnimationSpeed", "pending", "AgentStateFishingReady/Cast/Pull.OnEnter", LastFishingAutomationApplicationSummary, "AnimationSpeed.Pending." + phase);
                return;
            }

            FishingAutomationApplicationCount++;
            state.Phase = phase + ":FastAnimation";
            state.LastReason = "auto:FastAnimation";
            LastFishingAutomationApplicationSummary = "owner=" + ownerId + ", behavior=FastAnimation, phase=" + phase + ", multiplier=" + multiplier.ToString("0.###") + ", animators=" + changed + ", timings=" + timingChanged + ", samples=" + string.Join(";", samples.ToArray()) + ", applications=" + FishingAutomationApplicationCount;
            LastFishingAnimationSpeedSummary = LastFishingAutomationApplicationSummary;
            PublishFishingAutomationLog("AnimationSpeed.Applied." + phase, "Fishing automation animation speed applied by " + ownerId + " phase=" + phase + " multiplier=" + multiplier.ToString("0.###") + " animators=" + changed + " timings=" + timingChanged + ".");
            PublishFishingAutomationHookStatus("Smoke.AutoFishingAnimationSpeed", "verified", "AgentStateFishingReady/Cast/Pull.OnEnter", LastFishingAutomationApplicationSummary, "AnimationSpeed.Verified." + phase);
        }

        public void AdjustFishingPullDurationResult(ref float duration, string source)
        {
            if (duration <= 0f || !TryResolveFishingAnimationPolicy("Pull", out string ownerId, out double multiplier, out FishingAutomationState state))
                return;

            try
            {
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
                PublishFishingAutomationHookStatus("Smoke.AutoFishingAnimationSpeed", "verified", source, LastFishingAutomationApplicationSummary, "AnimationSpeed.PullDuration");
            }
            catch (Exception ex)
            {
                RecordFishingAutomationFailure("FishingAutomation.Pull.Duration", ex, true, "Smoke.AutoFishingAnimationSpeed", source);
            }
        }

        public void ApplyFishingReadyAutomation(object? readyState)
        {
            ApplyFishingReadyChargeTarget(readyState);
            ApplyFishingReadyChargeSpeed(readyState);
        }

        internal void ApplyFishingReadyChargeSpeed(object? readyState)
        {
            if (readyState == null || !TryResolveFishingReadyPolicy(out string ownerId, out double chargeRatio, out double animationMultiplier, out FishingAutomationState state))
                return;
            if (animationMultiplier <= 1d)
                return;
            if (ClampCastChargeRatio(chargeRatio) <= 0d)
                return;
            // Once NextState has released the synthetic use input at the requested
            // target, do not keep adding accelerated timer progress while the native
            // fishing_ready animation finishes. That would make FastAnimations change
            // the final cast power instead of only reducing time-to-target.
            if (fishingReadyChargeReleasedStates.Contains(readyState))
                return;

            try
            {
                double multiplier = ClampMultiplier(animationMultiplier);
                if (!animationController.TryAdvanceReadyCharge(readyState, multiplier, out double before, out double after, out double extraDelta, out int powerBarChanges))
                {
                    PublishFastReadyChargePending(readyState, ownerId, state, multiplier, "ready-accessor-unavailable");
                    return;
                }

                if (fishingReadyChargeAppliedStates.Add(readyState))
                {
                    FishingReadyChargeApplicationCount++;
                    lastFishingReadyChargePendingState = null;
                    lastFishingReadyChargePendingReason = string.Empty;
                    string progressSample = !double.IsNaN(before) && !double.IsNaN(after)
                        ? "castTimer.Progress:" + before.ToString("0.###", CultureInfo.InvariantCulture) + "->" + after.ToString("0.###", CultureInfo.InvariantCulture)
                        : "castTimer.Progress=unavailable";
                    state.Phase = "Ready:FastCharge";
                    state.LastReason = "auto:FastAnimation:ReadyCharge";
                    state.NativeOwner = "AgentStateFishingReady.OnPlay";
                    state.LastNativeAction = progressSample + ";extraDt=" + extraDelta.ToString("0.###", CultureInfo.InvariantCulture) + ";powerBar.updated=" + powerBarChanges.ToString(CultureInfo.InvariantCulture);
                    LastFishingAutomationApplicationSummary = "owner=" + ownerId +
                        ", behavior=FastReadyCharge" +
                        ", source=AgentStateFishingReady.OnPlay" +
                        ", multiplier=" + multiplier.ToString("0.###", CultureInfo.InvariantCulture) +
                        ", samples=" + state.LastNativeAction +
                        ", readyCharges=" + FishingReadyChargeApplicationCount.ToString(CultureInfo.InvariantCulture) +
                        ", applications=" + FishingAutomationApplicationCount.ToString(CultureInfo.InvariantCulture);
                    LastFishingAnimationSpeedSummary = LastFishingAutomationApplicationSummary;
                    PublishFishingAutomationHookStatus("Smoke.AutoFishingReadyChargeSpeed", "verified", "AgentStateFishingReady.OnPlay", LastFishingAutomationApplicationSummary, "ReadyChargeSpeed.Verified");
                }
            }
            catch (Exception ex)
            {
                RecordFishingAutomationFailure("FishingAutomation.Ready.ChargeSpeed", ex, true, "Smoke.AutoFishingReadyChargeSpeed", "AgentStateFishingReady.OnPlay");
            }
        }

        private void ApplyFishingReadyChargeTarget(object? readyState)
        {
            if (readyState == null || !TryResolveFishingReadyPolicy(out string ownerId, out double chargeRatio, out _, out FishingAutomationState state))
                return;

            currentFishingReadyChargeState = readyState;
            double target = ClampCastChargeRatio(chargeRatio);
            if (!fishingReadyChargeTargetStates.Add(readyState))
                return;
            double progress = animationController.TryReadReadyProgress(readyState, out double currentProgress) ? currentProgress : 0d;

            ObserveFishingNativeHandleResource("ReadyChargeState", readyState, "AgentStateFishingReady.NextState", ResourceLifecycleStatus.Acquired);
            FishingReadyChargeTargetApplicationCount++;
            string behavior = target <= 0d ? "ReadyNoChargeRelease" : "ReadyChargeTarget";
            state.Phase = target <= 0d ? "Ready:NoChargeTarget" : "Ready:ChargeTarget";
            state.LastReason = target <= 0d ? "auto:ReadyNoChargeRelease" : "auto:ReadyChargeTarget";
            state.NativeOwner = "AgentStateFishingReady.NextState";
            state.LastNativeAction = "target=" + target.ToString("0.###", CultureInfo.InvariantCulture);
            LastFishingAutomationApplicationSummary = "owner=" + ownerId +
                ", behavior=" + behavior +
                ", source=AgentStateFishingReady.NextState" +
                ", target=" + target.ToString("0.###", CultureInfo.InvariantCulture) +
                ", progress=" + progress.ToString("0.###", CultureInfo.InvariantCulture) +
                ", applications=" + FishingReadyChargeTargetApplicationCount.ToString(CultureInfo.InvariantCulture);
            if (target > 0d)
                PublishFishingAutomationHookStatus("Smoke.AutoFishingCastCharge", "experimental", "AgentStateFishingReady.NextState", LastFishingAutomationApplicationSummary, "CastCharge.Target");
        }

        public bool TryOverrideFishingReadyChargeInput(string inputName, out bool value)
        {
            value = false;
            if (!inputName.Equals("NormalUseToolInProgress", StringComparison.OrdinalIgnoreCase))
                return false;

            object? readyState = currentFishingReadyChargeState;
            if (readyState == null || !TryResolveFishingReadyPolicy(out string ownerId, out double chargeRatio, out _, out FishingAutomationState state))
                return false;

            double target = ClampCastChargeRatio(chargeRatio);
            double releaseThreshold = target >= 0.999d ? 0.995d : target;
            bool progressAvailable = animationController.TryReadReadyProgress(readyState, out double progress);
            // At zero charge, release the synthetic use input immediately. The native
            // Ready state still waits for fishing_ready to finish before entering Cast,
            // so its backswing remains native while SetPower receives the exact minimum.
            // Positive targets hold only while the native timer is readable and below
            // the requested threshold; missing reflection data fails open.
            bool hold = target > 0d && progressAvailable && progress < releaseThreshold;
            value = hold;

            if (!hold && fishingReadyChargeReleasedStates.Add(readyState))
            {
                string behavior = target <= 0d ? "ReadyNoChargeRelease" : "ReadyChargeTarget";
                state.Phase = target <= 0d ? "Ready:NoChargeRelease" : "Ready:ChargeRelease";
                state.LastReason = target <= 0d ? "auto:ReadyNoChargeRelease" : "auto:ReadyChargeTarget:reached";
                state.NativeOwner = "AgentStateFishingReady.NextState";
                state.LastNativeAction = "target=" + target.ToString("0.###", CultureInfo.InvariantCulture) + ";progress=" + progress.ToString("0.###", CultureInfo.InvariantCulture) + ";progressAvailable=" + progressAvailable.ToString(CultureInfo.InvariantCulture) + ";input=release";
                LastFishingAutomationApplicationSummary = "owner=" + ownerId +
                    ", behavior=" + behavior +
                    ", source=AgentStateFishingReady.NextState" +
                    ", target=" + target.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", progress=" + progress.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", progressAvailable=" + progressAvailable.ToString(CultureInfo.InvariantCulture) +
                    ", input=release" +
                    ", applications=" + FishingReadyChargeTargetApplicationCount.ToString(CultureInfo.InvariantCulture);
                if (target > 0d)
                    PublishFishingAutomationHookStatus("Smoke.AutoFishingCastCharge", "verified", "AgentStateFishingReady.NextState", LastFishingAutomationApplicationSummary, "CastCharge.Release");
            }

            return true;
        }

        private bool TryResolveFishingReadyPolicy(out string ownerId, out double chargeRatio, out double animationMultiplier, out FishingAutomationState state)
        {
            if (TryFindEnabledFishingOptions(out ownerId, out FishingAutomationOptions options, out state))
            {
                chargeRatio = options.CastChargeRatio;
                animationMultiplier = options.AnimationMode == FishingAnimationMode.FastCastPull ? options.AnimationMultiplier : 1d;
                return true;
            }
            ownerId = string.Empty;
            chargeRatio = 0d;
            animationMultiplier = 1d;
            state = null!;
            return false;
        }

        private static double ClampCastChargeRatio(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0d;
            return Math.Min(1d, Math.Max(0d, value));
        }

        private void PublishFastReadyChargePending(object readyState, string ownerId, FishingAutomationState state, double multiplier, string reason)
        {
            if (ReferenceEquals(lastFishingReadyChargePendingState, readyState) &&
                lastFishingReadyChargePendingReason.Equals(reason, StringComparison.Ordinal))
                return;
            lastFishingReadyChargePendingState = readyState;
            lastFishingReadyChargePendingReason = reason;
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
            PublishFishingAutomationHookStatus("Smoke.AutoFishingReadyChargeSpeed", "pending", "AgentStateFishingReady.OnPlay", LastFishingAutomationApplicationSummary, "ReadyChargeSpeed.Pending." + reason);
        }

        public void AdjustFishingCastHookPhysics(object? fishRodRenderer)
        {
            if (fishRodRenderer == null || !TryResolveFishingAnimationPolicy("Cast", out string ownerId, out double multiplier, out FishingAutomationState state))
                return;

            try
            {
                double hookPhysicsMultiplier = multiplier;
                object? hook = ReadMember(fishRodRenderer, "Hook") ?? ReadMember(fishRodRenderer, "_hook");
                if (hook == null)
                {
                    PublishFastCastHookPending(ownerId, state, multiplier, "no-hook");
                    return;
                }

                var samples = new List<string>();
                int changed = 0;
                changed += TryScaleVector2Member(hook, "Velocity", hookPhysicsMultiplier, "hook.Velocity", samples);

                object? rigidbody = TryGetUnityComponent(hook, "UnityEngine.Rigidbody2D, UnityEngine.Physics2DModule") ??
                    TryGetUnityComponent(hook, "UnityEngine.Rigidbody2D, UnityEngine");
                changed += TryScaleHookGravity(rigidbody, hookPhysicsMultiplier, samples);

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
                    ", physicsMultiplier=" + hookPhysicsMultiplier.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", physics=" + changed.ToString(CultureInfo.InvariantCulture) +
                    ", samples=" + string.Join(";", samples.ToArray()) +
                    ", applications=" + FishingAutomationApplicationCount.ToString(CultureInfo.InvariantCulture);
                LastFishingAnimationSpeedSummary = LastFishingAutomationApplicationSummary;
                PublishFishingAutomationHookStatus("Smoke.AutoFishingAnimationSpeed", "verified", "FishRodRenderer.CastHook", LastFishingAutomationApplicationSummary, "AnimationSpeed.CastHookPhysics");
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
            PublishFishingAutomationHookStatus("Smoke.AutoFishingAnimationSpeed", "pending", "FishRodRenderer.CastHook", LastFishingAutomationApplicationSummary, "AnimationSpeed.CastHookPending." + reason);
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
                ObserveFishingNativeHandleResource("HookPhysicsSnapshot", rigidbody, "FishRodRenderer.CastHook", ResourceLifecycleStatus.Acquired);
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
                fishingMiniGameInputOverride.Set(gameHandle, decision, DateTimeOffset.UtcNow.AddMilliseconds(250));

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
                fishingMiniGameInputOverride.Clear();
                RecordFishingAutomationFailure("FishingAutomation.MiniGame.Update", ex, true, "Smoke.AutoFishingMiniGameComplete", "FishingGameScrollBar.UpdateGame Prefix");
            }
        }

        private bool TryResolveFishingAnimationPolicy(string phase, out string ownerId, out double multiplier, out FishingAutomationState state)
        {
            ownerId = string.Empty;
            multiplier = 1d;
            state = null!;
            if (!TryFindEnabledFishingOptions(out ownerId, out FishingAutomationOptions options, out state) ||
                options.AnimationMode != FishingAnimationMode.FastCastPull ||
                options.AnimationMultiplier <= 1d)
                return false;
            if (phase.Equals("Ready", StringComparison.OrdinalIgnoreCase) && ClampCastChargeRatio(options.CastChargeRatio) <= 0d)
                return false;
            multiplier = ClampMultiplier(options.AnimationMultiplier);
            return true;
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
                PublishFishingAutomationLog("MiniGameComplete.NativeStatus." + currentStatus, "Fishing automation visible minigame input reached native status by " + ownerId + " status=" + currentStatus + " " + stats.Format() + ".");
                if (success)
                    PublishFishingAutomationHookStatus("Smoke.AutoFishingMiniGameComplete", "verified", "FishingGameScrollBar.UpdateGame", LastFishingAutomationApplicationSummary, "MiniGameComplete.Verified");
                else
                    runtime.SetHookStatus("Smoke.AutoFishingMiniGameComplete", "failed", "FishingGameScrollBar.UpdateGame", LastFishingAutomationApplicationSummary);
                RecordFishingAutomationSuccess("FishingAutomation.MiniGame.Update");
            }
            catch (Exception ex)
            {
                RecordFishingAutomationFailure("FishingAutomation.MiniGame.Update", ex, true, "Smoke.AutoFishingMiniGameComplete", "FishingGameScrollBar.UpdateGame Postfix");
            }
        }

        public bool TryOverrideFishingMiniGameInput(string inputName, out bool value)
        {
            value = false;
            FishingCompatibilityInputOverride current = fishingMiniGameInputOverride;
            if (!current.IsActive)
                return false;
            if (DateTimeOffset.UtcNow > current.ExpiresAtUtc)
            {
                current.Clear();
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

        public bool TryQueueVisibleReelInput(object waitState)
        {
            if (waitState == null)
                return false;
            fishingMiniGameInputOverride.Set(waitState, FishingMiniGameInputDecision.TapBonus, DateTimeOffset.UtcNow.AddMilliseconds(250));
            return true;
        }

        public void ClearQueuedFishingReelInput()
        {
            fishingMiniGameInputOverride.Clear();
        }

        private void ClearFishingMiniGameAutomationInput(object gameHandle)
        {
            if (fishingMiniGameInputOverride.IsActive && ReferenceEquals(fishingMiniGameInputOverride.GameHandle, gameHandle))
                fishingMiniGameInputOverride.Clear();
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
            return DescribeAgentState(ReadCurrentAgentState(dolocApi));
        }

        private static object? ReadCurrentAgentState(Type? dolocApi)
        {
            object? agent = ReadStaticMember(dolocApi, "agent");
            object? stateManager = agent == null ? null : ReadMember(agent, "StateManager");
            return stateManager == null ? null : ReadMember(stateManager, "current");
        }

        private static string DescribeAgentState(object? current)
        {
            return current == null ? "unknown" : current.GetType().Name;
        }

        private void MarkFishingAutoCastPending(string ownerId, string summary, string agentState, DateTimeOffset nowUtc)
        {
            pendingFishingAutoCastAtUtc = nowUtc;
            pendingFishingAutoCastOwnerId = ownerId ?? string.Empty;
            pendingFishingAutoCastSummary = summary ?? string.Empty;
            pendingFishingAutoCastAgentState = agentState ?? string.Empty;
            pendingFishingAutoCastStalled = false;
        }

        private bool ConfirmPendingFishingAutoCast(string phase, string source)
        {
            if (!IsFishingAutoCastProgressPhase(phase))
                return false;

            return ClearPendingFishingAutoCastCore(
                "phase=" + (phase ?? string.Empty) + ", source=" + (source ?? string.Empty),
                confirmed: true,
                statusOwner: "Fishing phase progression");
        }

        private bool DiscardPendingFishingAutoCast(string reason, string statusOwner)
        {
            return ClearPendingFishingAutoCastCore(reason, confirmed: false, statusOwner: statusOwner);
        }

        private bool ClearPendingFishingAutoCastCore(string reason, bool confirmed, string statusOwner)
        {
            if (!HasPendingFishingAutoCast)
                return false;

            string summary = "reason=" + (reason ?? string.Empty) +
                ", owner=" + pendingFishingAutoCastOwnerId +
                ", ageSeconds=" + GetPendingFishingAutoCastAgeSeconds(DateTimeOffset.UtcNow).ToString("0.###", CultureInfo.InvariantCulture) +
                ", stalled=" + pendingFishingAutoCastStalled.ToString(CultureInfo.InvariantCulture);
            string confirmedSummary = pendingFishingAutoCastSummary;
            pendingFishingAutoCastAtUtc = DateTimeOffset.MinValue;
            pendingFishingAutoCastOwnerId = string.Empty;
            pendingFishingAutoCastSummary = string.Empty;
            pendingFishingAutoCastAgentState = string.Empty;
            pendingFishingAutoCastStalled = false;
            suppressFishingAutoCastUntilUtc = DateTimeOffset.MinValue;
            if (confirmed)
            {
                fishingAutoCastApplications++;
                LastFishingAutomationApplicationSummary = confirmedSummary +
                    ", confirmedBy=" + (reason ?? string.Empty) +
                    ", confirmedApplications=" + fishingAutoCastApplications.ToString(CultureInfo.InvariantCulture);
                LastFishingAutoCastAttemptSummary = LastFishingAutomationApplicationSummary;
                PublishFishingAutomationHookStatus("Smoke.AutoFishingAutoCast", "verified", "BodyController.UseFishRod -> fishing phase hooks", LastFishingAutomationApplicationSummary, "AutoCast.Verified");
            }
            PublishFishingAutomationHookStatus("Fishing.Automation.AutoCastWatchdog", confirmed ? "cleared" : "discarded", FirstText(statusOwner, "Fishing automation pending cast"), summary, confirmed ? "AutoCastWatchdog.Cleared" : "AutoCastWatchdog.Discarded");
            return true;
        }

        private static bool IsFishingAutoCastProgressPhase(string? phase)
        {
            string value = phase ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return value.Equals("Ready", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Cast", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Wait", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Battle", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Pull", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("MiniGame", StringComparison.OrdinalIgnoreCase);
        }

        private void CheckFishingAutoCastWatchdog(DateTimeOffset nowUtc)
        {
            if (!HasPendingFishingAutoCast)
                return;
            if (pendingFishingAutoCastStalled)
                return;
            if (nowUtc - pendingFishingAutoCastAtUtc < FishingAutoCastPendingTimeout)
                return;

            pendingFishingAutoCastStalled = true;
            pendingFishingAutoCastStallCount++;
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            string summary = "owner=" + pendingFishingAutoCastOwnerId +
                ", ageSeconds=" + GetPendingFishingAutoCastAgeSeconds(nowUtc).ToString("0.###", CultureInfo.InvariantCulture) +
                ", hookInstalled=" + fishingHooksInstalled.ToString(CultureInfo.InvariantCulture) +
                ", agentStateAtCast=" + pendingFishingAutoCastAgentState +
                ", currentAgentState=" + DescribeAgentState(dolocApi) +
                ", caches={" + GetFishingAutomationLifecycleSummary() + "}" +
                ", castSummary={" + pendingFishingAutoCastSummary + "}" +
                ", stalls=" + pendingFishingAutoCastStallCount.ToString(CultureInfo.InvariantCulture);
            runtime.RuntimeMonitor.Log("Fishing automation auto-cast watchdog stalled before native phase progression. " + summary + ".", LogLevel.Warn);
            runtime.SetHookStatus("Fishing.Automation.AutoCastWatchdog", "needs-review", "BodyController.UseFishRod -> fishing phase hooks", summary);
            ReleasePendingFishingAutoCastAfterStall(summary, nowUtc);
        }

        private void ReleasePendingFishingAutoCastAfterStall(string summary, DateTimeOffset nowUtc)
        {
            string ownerId = pendingFishingAutoCastOwnerId;
            pendingFishingAutoCastAtUtc = DateTimeOffset.MinValue;
            pendingFishingAutoCastOwnerId = string.Empty;
            pendingFishingAutoCastSummary = string.Empty;
            pendingFishingAutoCastAgentState = string.Empty;
            pendingFishingAutoCastStalled = false;
            suppressFishingAutoCastUntilUtc = nowUtc + FishingAutoCastStallBackoff;
            if (!string.IsNullOrWhiteSpace(ownerId) && fishingStates.TryGetValue(ownerId, out FishingAutomationState state))
            {
                state.Phase = "StallBackoff";
                state.LastReason = "auto:pending-cast-stall";
                state.NativeOwner = "BodyController.UseFishRod";
                state.LastNativeAction = "watchdog-backoff";
            }
            LastFishingAutoCastAttemptSummary = "stalled=" + summary +
                ", backoffSeconds=" + FishingAutoCastStallBackoff.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
            PublishFishingPendingCastWatchdogBoundary("PendingCastWatchdog", "pending-cast-stall");
        }

        private double GetPendingFishingAutoCastAgeSeconds(DateTimeOffset nowUtc)
        {
            return HasPendingFishingAutoCast
                ? Math.Max(0d, (nowUtc - pendingFishingAutoCastAtUtc).TotalSeconds)
                : 0d;
        }

        internal string GetFishingAutomationLifecycleSummary()
        {
            return GetFishingAutomationLifecycleSnapshot("summary").FormatShortSummary();
        }

        internal FishingAutomationLifecycleSnapshot GetFishingAutomationLifecycleSnapshot(string boundary)
        {
            DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
            int readyChargeStateCount = fishingReadyChargeAppliedStates.Count + fishingReadyChargeTargetStates.Count + fishingReadyChargeReleasedStates.Count;
            string[] ownerPolicies = fishingOptions
                .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .Select(entry => entry.Key +
                    ":charge=" + ClampCastChargeRatio(entry.Value.CastChargeRatio).ToString("0.###", CultureInfo.InvariantCulture) +
                    ";fast=" + (entry.Value.AnimationMode == FishingAnimationMode.FastCastPull).ToString(CultureInfo.InvariantCulture) +
                    ";mult=" + ClampMultiplier(entry.Value.AnimationMultiplier).ToString("0.###", CultureInfo.InvariantCulture) +
                    ";result=" + entry.Value.ResultMode +
                    ";bite=" + entry.Value.BiteWaitMode)
                .ToArray();
            int enabledOwners = fishingStates.Count(entry => entry.Value?.Enabled == true);
            int nativeTransientHandleCount = fishingMiniGameStartedAt.Count +
                fishingMiniGameBonusTaps.Count +
                fishingMiniGameInputStats.Count +
                fishingMiniGameCompletedHandles.Count +
                readyChargeStateCount +
                (currentFishingReadyChargeState == null ? 0 : 1) +
                originalAnimatorSpeeds.Count +
                originalHookGravityScales.Count +
                (fishingMiniGameInputOverride.IsActive ? 1 : 0);
            int boundaryClearCount = nativeTransientHandleCount +
                (HasPendingFishingAutoCast ? 1 : 0) +
                (suppressFishingAutoCastUntilUtc > nowUtc ? 1 : 0);

            return new FishingAutomationLifecycleSnapshot(
                boundary ?? string.Empty,
                fishingOptions.Count,
                fishingStates.Count,
                enabledOwners,
                fishingMiniGameStartedAt.Count,
                fishingMiniGameBonusTaps.Count,
                fishingMiniGameInputStats.Count,
                fishingMiniGameCompletedHandles.Count,
                fishingReadyChargeAppliedStates.Count,
                fishingReadyChargeTargetStates.Count,
                fishingReadyChargeReleasedStates.Count,
                currentFishingReadyChargeState != null,
                originalAnimatorSpeeds.Count,
                originalHookGravityScales.Count,
                fishingMiniGameInputOverride.IsActive,
                HasPendingFishingAutoCast,
                GetPendingFishingAutoCastAgeSeconds(nowUtc),
                pendingFishingAutoCastStallCount,
                suppressFishingAutoCastUntilUtc > nowUtc,
                loggedFishingPhases.Count,
                fishingAutomationFailures.Count,
                fishingAutomationDiagnostics.Count,
                fishingAutoCastAttempts,
                fishingAutoCastApplications,
                FishingAutomationApplicationCount,
                FishingReadyChargeApplicationCount,
                FishingReadyChargeTargetApplicationCount,
                FishingMiniGameCompleteApplicationCount,
                nativeTransientHandleCount,
                boundaryClearCount,
                ownerPolicies);
        }

        internal void NotifyFishingEnvironmentReset(string reason)
        {
            // DolocAPI.SetEnvCamera is too frequent to be a destructive fishing lifecycle
            // boundary. SaveLoaded/ReturnedToTitle and native fishing state exits own cleanup.
            DateTimeOffset nowUtc = DateTimeOffset.UtcNow;
            if (lastFishingLifecycleEnvironmentResetPublishedAtUtc == DateTimeOffset.MinValue ||
                nowUtc - lastFishingLifecycleEnvironmentResetPublishedAtUtc >= TimeSpan.FromSeconds(60))
            {
                lastFishingLifecycleEnvironmentResetPublishedAtUtc = nowUtc;
                PublishFishingLifecycleBoundary("EnvironmentReset", "non-destructive:" + (reason ?? string.Empty), expectTransientClear: false, logIfOk: false);
            }
        }

        private bool HasFishingPoolInScene()
        {
            try
            {
                nativeObjectSearchCount++;
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
            {
                originalAnimatorSpeeds[animator] = original;
                ObserveFishingNativeHandleResource("AnimatorSpeedSnapshot", animator, label, ResourceLifecycleStatus.Acquired);
            }
            else
                original = originalAnimatorSpeeds[animator];

            double target = original * multiplier;
            if (!TryWriteAnimatorSpeed(animator, target))
                return 0;

            if (samples.Count < 6)
                samples.Add(label + ":" + original.ToString("0.###") + "->" + target.ToString("0.###"));
            return 1;
        }

        public void RestoreExperimentalAnimatorSpeeds(string reason)
        {
            if (originalAnimatorSpeeds.Count == 0 && originalHookGravityScales.Count == 0)
                return;

            int restored = 0;
            foreach (KeyValuePair<object, double> entry in new List<KeyValuePair<object, double>>(originalAnimatorSpeeds))
            {
                if (TryWriteAnimatorSpeed(entry.Key, entry.Value))
                {
                    restored++;
                    ReleaseFishingNativeHandleResource("AnimatorSpeedSnapshot", entry.Key, reason);
                }
            }
            originalAnimatorSpeeds.Clear();

            int hookPhysicsRestored = 0;
            foreach (KeyValuePair<object, double> entry in new List<KeyValuePair<object, double>>(originalHookGravityScales))
            {
                if (SetMemberValue(entry.Key, "gravityScale", (float)entry.Value))
                {
                    hookPhysicsRestored++;
                    ReleaseFishingNativeHandleResource("HookPhysicsSnapshot", entry.Key, reason);
                }
            }
            originalHookGravityScales.Clear();

            PublishFishingAutomationLog("AnimationSpeed.Restore." + FirstText(reason, "unknown"), "Experimental fishing animation state restored reason=" + reason + " animators=" + restored + " hookPhysics=" + hookPhysicsRestored + ".");
            PublishFishingAutomationHookStatus("Smoke.AutoFishingAnimationSpeedRestore", "experimental", "Fishing/AgentState lifecycle boundary", "reason=" + reason + ", restored=" + restored + ", hookPhysics=" + hookPhysicsRestored, "AnimationSpeed.Restore." + FirstText(reason, "unknown"));
        }

        public void NotifyFishingNativeExit(string reason)
        {
            PublishFishingLifecycleBoundary("NativeExit", reason, expectTransientClear: true, logIfOk: true);
        }

        internal void ResetFishingRuntimeState(string reason)
        {
            int miniGameHandles = fishingMiniGameStartedAt.Count;
            int miniGameInputHandles = fishingMiniGameInputStats.Count;
            int loggedPhaseCount = loggedFishingPhases.Count;
            int failureEpisodes = fishingAutomationFailures.Count;
            int autoCastAttemptCount = fishingAutoCastAttempts;
            int autoCastApplicationCount = fishingAutoCastApplications;
            bool hadPendingCast = HasPendingFishingAutoCast;
            fishingMiniGameStartedAt.Clear();
            fishingMiniGameBonusTaps.Clear();
            fishingMiniGameInputStats.Clear();
            fishingMiniGameCompletedHandles.Clear();
            fishingReadyChargeAppliedStates.Clear();
            fishingReadyChargeTargetStates.Clear();
            fishingReadyChargeReleasedStates.Clear();
            fishingMiniGameInputOverride.Clear();
            currentFishingReadyChargeState = null;
            lastFishingReadyChargePendingState = null;
            lastFishingReadyChargePendingReason = string.Empty;
            loggedFishingPhases.Clear();
            fishingAutomationFailures.Clear();
            fishingLifecyclePublicationGate.Reset();
            fishingAutoCastAttempts = 0;
            fishingAutoCastApplications = 0;
            lastFishingAutoCastAt = DateTimeOffset.MinValue;
            lastFishingFeedbackAt = DateTimeOffset.MinValue;
            lastFishingPullDurationResultScaleAtUtc = DateTimeOffset.MinValue;
            pendingFishingAutoCastAtUtc = DateTimeOffset.MinValue;
            pendingFishingAutoCastOwnerId = string.Empty;
            pendingFishingAutoCastSummary = string.Empty;
            pendingFishingAutoCastAgentState = string.Empty;
            pendingFishingAutoCastStalled = false;
            suppressFishingAutoCastUntilUtc = DateTimeOffset.MinValue;
            lastObservedFishingPhase = string.Empty;
            lastObservedFishingPhaseAtUtc = DateTimeOffset.MinValue;
            LastFishingAutomationApplicationSummary = string.Empty;
            LastFishingMiniGameCompleteSummary = string.Empty;
            LastFishingAnimationSpeedSummary = string.Empty;
            LastFishingAutoCastAttemptSummary = string.Empty;
            RestoreExperimentalAnimatorSpeeds(reason);
            fishingAutomationDiagnostics.Clear();
            runtime.RuntimeMonitor.Log("Fishing automation runtime state reset reason=" + reason +
                " miniGameHandles=" + miniGameHandles.ToString(CultureInfo.InvariantCulture) +
                " miniGameInputHandles=" + miniGameInputHandles.ToString(CultureInfo.InvariantCulture) +
                " loggedPhases=" + loggedPhaseCount.ToString(CultureInfo.InvariantCulture) +
                " failureEpisodes=" + failureEpisodes.ToString(CultureInfo.InvariantCulture) +
                " autoCastAttempts=" + autoCastAttemptCount.ToString(CultureInfo.InvariantCulture) +
                " autoCastConfirmed=" + autoCastApplicationCount.ToString(CultureInfo.InvariantCulture) +
                " pendingCast=" + (hadPendingCast ? "true" : "false") + ".");
            runtime.ObserveResourceCleanup(
                "AutoFishing",
                reason ?? string.Empty,
                miniGameHandles + miniGameInputHandles + loggedPhaseCount + failureEpisodes + autoCastAttemptCount + autoCastApplicationCount + (hadPendingCast ? 1 : 0),
                "SaveLifetime native/transient fishing automation state cleared; owner policy/state dictionaries intentionally retained.");
            string boundaryReason = FirstText(reason ?? string.Empty, "ResetFishingRuntimeState");
            PublishFishingLifecycleBoundary(boundaryReason, boundaryReason, expectTransientClear: true, logIfOk: true);
        }

        private void ClearFishingAutomationTransientState(string reason)
        {
            DiscardPendingFishingAutoCast(reason, "Fishing automation transient reset");
            fishingMiniGameStartedAt.Clear();
            fishingMiniGameBonusTaps.Clear();
            fishingMiniGameInputStats.Clear();
            fishingMiniGameCompletedHandles.Clear();
            fishingReadyChargeAppliedStates.Clear();
            fishingReadyChargeTargetStates.Clear();
            fishingReadyChargeReleasedStates.Clear();
            fishingMiniGameInputOverride.Clear();
            currentFishingReadyChargeState = null;
            lastFishingReadyChargePendingState = null;
            lastFishingReadyChargePendingReason = string.Empty;
            lastFishingAutoCastAt = DateTimeOffset.MinValue;
            lastFishingPullDurationResultScaleAtUtc = DateTimeOffset.MinValue;
            suppressFishingAutoCastUntilUtc = DateTimeOffset.MinValue;
            lastObservedFishingPhase = string.Empty;
            lastObservedFishingPhaseAtUtc = DateTimeOffset.MinValue;
            LastFishingAutomationApplicationSummary = string.Empty;
            LastFishingMiniGameCompleteSummary = string.Empty;
            LastFishingAnimationSpeedSummary = string.Empty;
            LastFishingAutoCastAttemptSummary = string.Empty;
            RestoreExperimentalAnimatorSpeeds(reason);
        }

        private void PublishFishingLifecycleBoundary(string boundary, string reason, bool expectTransientClear, bool logIfOk)
        {
            int boundaryClearCount = GetFishingBoundaryClearCountFast(DateTimeOffset.UtcNow);
            bool warning = expectTransientClear && boundaryClearCount > 0;
            FishingCompatibilityLifecycleEvent eventKind = ClassifyFishingCompatibilityLifecycleEvent(boundary, reason);
            bool hardBoundary = eventKind == FishingCompatibilityLifecycleEvent.SaveLoaded ||
                eventKind == FishingCompatibilityLifecycleEvent.ReturnedToTitle ||
                eventKind == FishingCompatibilityLifecycleEvent.OwnerCleanup;
            if (!fishingLifecyclePublicationGate.ShouldPublish(eventKind, warning, DateTimeOffset.UtcNow, hardBoundary))
                return;
            FishingAutomationLifecycleSnapshot snapshot = GetFishingAutomationLifecycleSnapshot(boundary);
            string status = warning ? "warning" : "ok";
            string details = "boundary=" + FirstText(boundary, "unknown") +
                ", reason=" + FirstText(reason, "unknown") +
                ", expectation=" + (expectTransientClear ? "transient-clear" : "observe-only") +
                ", " + snapshot.FormatShortSummary();
            LastFishingAutomationLifecycleStatus = status;
            LastFishingAutomationLifecycleSummary = details;
            runtime.SetHookStatus("Fishing.Automation.Lifecycle", status, "LegacyFishingAutomationService." + FirstText(boundary, "unknown"), details);
            runtime.Diagnostics.SetFeatureStatus(
                "Refactor.AutoFishingLifecycle",
                status,
                FirstText(boundary, "unknown"),
                success: !warning,
                failureCount: warning ? 1 : 0,
                lastError: warning ? "AutoFishing transient state retained at boundary." : string.Empty,
                details: details);
            if (warning || logIfOk)
                runtime.RuntimeMonitor.Log("Fishing automation lifecycle boundary status=" + status + " " + details + ".", warning ? LogLevel.Warn : LogLevel.Info);
        }

        private void PublishFishingPendingCastWatchdogBoundary(string boundary, string reason)
        {
            bool pending = HasPendingFishingAutoCast;
            if (!fishingLifecyclePublicationGate.ShouldPublish(FishingCompatibilityLifecycleEvent.PendingCastWatchdog, pending, DateTimeOffset.UtcNow, hardBoundary: pending))
                return;
            FishingAutomationLifecycleSnapshot snapshot = GetFishingAutomationLifecycleSnapshot(boundary);
            bool warning = snapshot.PendingCast;
            string status = warning ? "warning" : "ok";
            string details = "boundary=" + FirstText(boundary, "unknown") +
                ", reason=" + FirstText(reason, "unknown") +
                ", expectation=pending-cast-cleared, " + snapshot.FormatShortSummary();
            LastFishingAutomationLifecycleStatus = status;
            LastFishingAutomationLifecycleSummary = details;
            runtime.SetHookStatus("Fishing.Automation.Lifecycle", status, "LegacyFishingAutomationService." + FirstText(boundary, "unknown"), details);
            runtime.Diagnostics.SetFeatureStatus(
                "Refactor.AutoFishingLifecycle",
                status,
                FirstText(boundary, "unknown"),
                success: !warning,
                failureCount: warning ? 1 : 0,
                lastError: warning ? "AutoFishing pending cast survived watchdog boundary." : string.Empty,
                details: details);
            runtime.RuntimeMonitor.Log("Fishing automation lifecycle boundary status=" + status + " " + details + ".", warning ? LogLevel.Warn : LogLevel.Info);
        }

        private void ObserveFishingOwnerResource(string ownerId, string reason)
        {
            runtime.ObserveResourceLifecycle(
                "AutoFishing.OwnerPolicy",
                FirstText(ownerId, "unknown"),
                FirstText(ownerId, "unknown"),
                "IFishingAutomationApi.Configure",
                ResourceLifetime.ProcessLifetime,
                ResourceOwnership.ExternalModOwned,
                ResourceLifecycleStatus.Declared,
                "owner-config; no native object; retained until process exit");
            PublishFishingLifecycleBoundary("Configure", reason, expectTransientClear: false, logIfOk: false);
        }

        private void ObserveFishingOwnerStateResource(string ownerId, bool enabled, string reason)
        {
            runtime.ObserveResourceLifecycle(
                "AutoFishing.OwnerState",
                FirstText(ownerId, "unknown"),
                FirstText(ownerId, "unknown"),
                "IFishingAutomationApi.SetEnabled",
                ResourceLifetime.ProcessLifetime,
                ResourceOwnership.DtmapiOwned,
                enabled ? ResourceLifecycleStatus.Ready : ResourceLifecycleStatus.Declared,
                "owner state/config; no native object; retained until process exit");
            if (enabled)
                PublishFishingLifecycleBoundary("SetEnabled(true)", reason, expectTransientClear: false, logIfOk: false);
        }

        private int GetFishingBoundaryClearCountFast(DateTimeOffset nowUtc)
        {
            int readyChargeStateCount = fishingReadyChargeAppliedStates.Count + fishingReadyChargeTargetStates.Count + fishingReadyChargeReleasedStates.Count;
            int nativeTransientHandleCount = fishingMiniGameStartedAt.Count +
                fishingMiniGameBonusTaps.Count +
                fishingMiniGameInputStats.Count +
                fishingMiniGameCompletedHandles.Count +
                readyChargeStateCount +
                (currentFishingReadyChargeState == null ? 0 : 1) +
                originalAnimatorSpeeds.Count +
                originalHookGravityScales.Count +
                (fishingMiniGameInputOverride.IsActive ? 1 : 0);
            return nativeTransientHandleCount +
                (HasPendingFishingAutoCast ? 1 : 0) +
                (suppressFishingAutoCastUntilUtc > nowUtc ? 1 : 0);
        }

        private static FishingCompatibilityLifecycleEvent ClassifyFishingCompatibilityLifecycleEvent(string boundary, string reason)
        {
            string value = boundary ?? string.Empty;
            if (value.Equals("Configure", StringComparison.OrdinalIgnoreCase))
                return FishingCompatibilityLifecycleEvent.Configure;
            if (value.StartsWith("SetEnabled(true)", StringComparison.OrdinalIgnoreCase))
                return FishingCompatibilityLifecycleEvent.Enabled;
            if (value.StartsWith("SetEnabled(false)", StringComparison.OrdinalIgnoreCase))
                return FishingCompatibilityLifecycleEvent.Disabled;
            if (value.Equals("MiniGameStop", StringComparison.OrdinalIgnoreCase))
                return FishingCompatibilityLifecycleEvent.MiniGameStop;
            if (value.Equals("NativeExit", StringComparison.OrdinalIgnoreCase))
                return FishingCompatibilityLifecycleEvent.NativeExit;
            if (value.IndexOf("SaveLoaded", StringComparison.OrdinalIgnoreCase) >= 0)
                return FishingCompatibilityLifecycleEvent.SaveLoaded;
            if (value.IndexOf("ReturnedToTitle", StringComparison.OrdinalIgnoreCase) >= 0)
                return FishingCompatibilityLifecycleEvent.ReturnedToTitle;
            if (value.IndexOf("owner-cleanup", StringComparison.OrdinalIgnoreCase) >= 0 || (reason ?? string.Empty).IndexOf("owner-cleanup", StringComparison.OrdinalIgnoreCase) >= 0)
                return FishingCompatibilityLifecycleEvent.OwnerCleanup;
            if (value.IndexOf("EnvironmentReset", StringComparison.OrdinalIgnoreCase) >= 0)
                return FishingCompatibilityLifecycleEvent.EnvironmentReset;
            return FishingCompatibilityLifecycleEvent.Other;
        }

        private void ObserveFishingNativeHandleResource(string kind, object? nativeHandle, string reason, string status)
        {
            if (nativeHandle == null)
                return;
            runtime.ObserveResourceLifecycle(
                "AutoFishing." + FirstText(kind, "NativeHandle"),
                FormatNativeObjectResourceId(kind, nativeHandle),
                ResolveFishingLifecycleOwnerId(),
                FirstText(reason, "native-fishing"),
                ResourceLifetime.SaveLifetime,
                ResourceOwnership.BorrowedNative,
                status,
                "clear DTMAPI key/reference only; do not destroy native object",
                observationMode: ResourceLifecycleObservationMode.AggregatedCurrentState,
                aggregationKey: "fishing-native:" + FirstText(kind, "NativeHandle"));
        }

        private void ReleaseFishingNativeHandleResource(string kind, object? nativeHandle, string reason)
        {
            if (nativeHandle == null)
                return;
            runtime.ReleaseResourceLifecycle(
                "AutoFishing." + FirstText(kind, "NativeHandle"),
                FormatNativeObjectResourceId(kind, nativeHandle),
                ResolveFishingLifecycleOwnerId(),
                FirstText(reason, "native-fishing"),
                ResourceLifetime.SaveLifetime,
                ResourceOwnership.BorrowedNative,
                "clear DTMAPI key/reference only; do not destroy native object",
                ResourceLifecycleStatus.Released,
                observationMode: ResourceLifecycleObservationMode.AggregatedCurrentState,
                aggregationKey: "fishing-native:" + FirstText(kind, "NativeHandle"));
        }

        private void ReleaseFishingReadyChargeStateResources(string reason)
        {
            HashSet<object> readyStates = new HashSet<object>();
            foreach (object readyState in fishingReadyChargeAppliedStates)
                readyStates.Add(readyState);
            foreach (object readyState in fishingReadyChargeTargetStates)
                readyStates.Add(readyState);
            foreach (object readyState in fishingReadyChargeReleasedStates)
                readyStates.Add(readyState);
            if (currentFishingReadyChargeState != null)
                readyStates.Add(currentFishingReadyChargeState);

            foreach (object readyState in readyStates)
                ReleaseFishingNativeHandleResource("ReadyChargeState", readyState, reason);
        }

        private string ResolveFishingLifecycleOwnerId()
        {
            return TryGetEnabledFishingAutomationOwner(out string ownerId)
                ? ownerId
                : "DTMAPI.GameBridge.FishingAutomation";
        }

        private static string FormatNativeObjectResourceId(string kind, object value)
        {
            return FirstText(kind, "NativeHandle") +
                ":" + value.GetType().Name +
                "#" + RuntimeHelpers.GetHashCode(value).ToString("X", CultureInfo.InvariantCulture);
        }

        private void PublishFishingAutomationHookStatus(string hookId, string status, string source, string details, string diagnosticKey)
        {
            FishingDiagnosticKey key = ClassifyFishingDiagnosticKey(hookId, diagnosticKey, isLog: false);
            if (ShouldPublishFishingAutomationDiagnostic(key, status, details, DateTimeOffset.UtcNow))
                runtime.SetHookStatus(hookId, status, source, details);
        }

        private void PublishFishingAutomationLog(string diagnosticKey, string message, LogLevel level = LogLevel.Info)
        {
            FishingDiagnosticKey key = ClassifyFishingDiagnosticKey(string.Empty, diagnosticKey, isLog: true);
            if (ShouldPublishFishingAutomationDiagnostic(key, level.ToString(), message, DateTimeOffset.UtcNow))
                runtime.RuntimeMonitor.Log(message, level);
        }

        private bool ShouldPublishFishingAutomationDiagnostic(FishingDiagnosticKey key, string status, string details, DateTimeOffset nowUtc)
        {
            if (!fishingAutomationDiagnostics.TryGetValue(key, out FishingAutomationDiagnosticState state))
            {
                state = new FishingAutomationDiagnosticState();
                fishingAutomationDiagnostics[key] = state;
            }

            state.ObservedCount++;
            bool statusChanged = !string.Equals(state.LastPublishedStatus, status ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            bool initialSample = state.PublishedCount < FishingAutomationDiagnosticInitialPublishLimit;
            bool intervalSample = state.LastPublishedAtUtc == DateTimeOffset.MinValue ||
                nowUtc - state.LastPublishedAtUtc >= FishingAutomationDiagnosticSummaryInterval;
            if (!statusChanged && !initialSample && !intervalSample)
                return false;

            state.PublishedCount++;
            state.LastPublishedAtUtc = nowUtc;
            state.LastPublishedStatus = status ?? string.Empty;
            state.LastPublishedDetails = details ?? string.Empty;
            return true;
        }

        private static FishingDiagnosticKey ClassifyFishingDiagnosticKey(string hookId, string diagnosticKey, bool isLog)
        {
            string value = string.IsNullOrWhiteSpace(hookId) ? diagnosticKey ?? string.Empty : hookId;
            if (value.IndexOf("AutoCast", StringComparison.OrdinalIgnoreCase) >= 0)
                return FishingDiagnosticKey.AutoCast;
            if (value.IndexOf("InstantBite", StringComparison.OrdinalIgnoreCase) >= 0)
                return FishingDiagnosticKey.InstantBite;
            if (value.IndexOf("MiniGameSkip", StringComparison.OrdinalIgnoreCase) >= 0)
                return FishingDiagnosticKey.MiniGameSkip;
            if (value.IndexOf("MiniGameComplete", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("MiniGame", StringComparison.OrdinalIgnoreCase) >= 0)
                return FishingDiagnosticKey.MiniGame;
            if (value.IndexOf("ReadyCharge", StringComparison.OrdinalIgnoreCase) >= 0)
                return FishingDiagnosticKey.ReadyCharge;
            if (value.IndexOf("CastCharge", StringComparison.OrdinalIgnoreCase) >= 0)
                return FishingDiagnosticKey.CastCharge;
            if (value.IndexOf("Animation", StringComparison.OrdinalIgnoreCase) >= 0)
                return FishingDiagnosticKey.Animation;
            if (value.IndexOf("Phase", StringComparison.OrdinalIgnoreCase) >= 0)
                return FishingDiagnosticKey.Phase;
            return isLog ? FishingDiagnosticKey.Log : FishingDiagnosticKey.Other;
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

        internal sealed class FishingAutomationLifecycleSnapshot
        {
            public FishingAutomationLifecycleSnapshot(
                string boundary,
                int optionOwnerCount,
                int stateOwnerCount,
                int enabledOwnerCount,
                int miniGameHandleCount,
                int miniGameBonusHandleCount,
                int miniGameInputHandleCount,
                int miniGameCompletedHandleCount,
                int readyChargeAppliedCount,
                int readyChargeTargetCount,
                int readyChargeReleasedCount,
                bool hasCurrentReadyChargeState,
                int animatorSnapshotCount,
                int hookPhysicsSnapshotCount,
                bool hasInputOverride,
                bool pendingCast,
                double pendingCastAgeSeconds,
                int pendingCastStallCount,
                bool autoCastBackoff,
                int loggedPhaseCount,
                int failureCount,
                int diagnosticCount,
                int autoCastAttemptCount,
                int autoCastConfirmedCount,
                int automationApplicationCount,
                int readyChargeApplicationCount,
                int readyChargeTargetApplicationCount,
                int miniGameCompleteApplicationCount,
                int nativeTransientHandleCount,
                int boundaryClearCount,
                IReadOnlyList<string> ownerPolicies)
            {
                Boundary = boundary ?? string.Empty;
                OptionOwnerCount = optionOwnerCount;
                StateOwnerCount = stateOwnerCount;
                EnabledOwnerCount = enabledOwnerCount;
                MiniGameHandleCount = miniGameHandleCount;
                MiniGameBonusHandleCount = miniGameBonusHandleCount;
                MiniGameInputHandleCount = miniGameInputHandleCount;
                MiniGameCompletedHandleCount = miniGameCompletedHandleCount;
                ReadyChargeAppliedCount = readyChargeAppliedCount;
                ReadyChargeTargetCount = readyChargeTargetCount;
                ReadyChargeReleasedCount = readyChargeReleasedCount;
                HasCurrentReadyChargeState = hasCurrentReadyChargeState;
                AnimatorSnapshotCount = animatorSnapshotCount;
                HookPhysicsSnapshotCount = hookPhysicsSnapshotCount;
                HasInputOverride = hasInputOverride;
                PendingCast = pendingCast;
                PendingCastAgeSeconds = pendingCastAgeSeconds;
                PendingCastStallCount = pendingCastStallCount;
                AutoCastBackoff = autoCastBackoff;
                LoggedPhaseCount = loggedPhaseCount;
                FailureCount = failureCount;
                DiagnosticCount = diagnosticCount;
                AutoCastAttemptCount = autoCastAttemptCount;
                AutoCastConfirmedCount = autoCastConfirmedCount;
                AutomationApplicationCount = automationApplicationCount;
                ReadyChargeApplicationCount = readyChargeApplicationCount;
                ReadyChargeTargetApplicationCount = readyChargeTargetApplicationCount;
                MiniGameCompleteApplicationCount = miniGameCompleteApplicationCount;
                NativeTransientHandleCount = nativeTransientHandleCount;
                BoundaryClearCount = boundaryClearCount;
                OwnerPolicies = ownerPolicies ?? Array.Empty<string>();
            }

            public string Boundary { get; }

            public int OptionOwnerCount { get; }

            public int StateOwnerCount { get; }

            public int EnabledOwnerCount { get; }

            public int MiniGameHandleCount { get; }

            public int MiniGameBonusHandleCount { get; }

            public int MiniGameInputHandleCount { get; }

            public int MiniGameCompletedHandleCount { get; }

            public int ReadyChargeAppliedCount { get; }

            public int ReadyChargeTargetCount { get; }

            public int ReadyChargeReleasedCount { get; }

            public bool HasCurrentReadyChargeState { get; }

            public int AnimatorSnapshotCount { get; }

            public int HookPhysicsSnapshotCount { get; }

            public bool HasInputOverride { get; }

            public bool PendingCast { get; }

            public double PendingCastAgeSeconds { get; }

            public int PendingCastStallCount { get; }

            public bool AutoCastBackoff { get; }

            public int LoggedPhaseCount { get; }

            public int FailureCount { get; }

            public int DiagnosticCount { get; }

            public int AutoCastAttemptCount { get; }

            public int AutoCastConfirmedCount { get; }

            public int AutomationApplicationCount { get; }

            public int ReadyChargeApplicationCount { get; }

            public int ReadyChargeTargetApplicationCount { get; }

            public int MiniGameCompleteApplicationCount { get; }

            public int NativeTransientHandleCount { get; }

            public int BoundaryClearCount { get; }

            public IReadOnlyList<string> OwnerPolicies { get; }

            public string FormatShortSummary()
            {
                return "boundary=" + FirstText(Boundary, "unknown") +
                    ", ownerOptions=" + OptionOwnerCount.ToString(CultureInfo.InvariantCulture) +
                    ", ownerStates=" + StateOwnerCount.ToString(CultureInfo.InvariantCulture) +
                    ", enabledOwners=" + EnabledOwnerCount.ToString(CultureInfo.InvariantCulture) +
                    ", nativeTransientHandles=" + NativeTransientHandleCount.ToString(CultureInfo.InvariantCulture) +
                    ", boundaryClearCount=" + BoundaryClearCount.ToString(CultureInfo.InvariantCulture) +
                    ", miniGameHandles=" + MiniGameHandleCount.ToString(CultureInfo.InvariantCulture) +
                    ", miniGameInputHandles=" + MiniGameInputHandleCount.ToString(CultureInfo.InvariantCulture) +
                    ", readyChargeStates=" + (ReadyChargeAppliedCount + ReadyChargeTargetCount + ReadyChargeReleasedCount).ToString(CultureInfo.InvariantCulture) +
                    ", currentReadyChargeState=" + HasCurrentReadyChargeState.ToString(CultureInfo.InvariantCulture) +
                    ", animators=" + AnimatorSnapshotCount.ToString(CultureInfo.InvariantCulture) +
                    ", hookPhysics=" + HookPhysicsSnapshotCount.ToString(CultureInfo.InvariantCulture) +
                    ", inputOverride=" + HasInputOverride.ToString(CultureInfo.InvariantCulture) +
                    ", pendingCast=" + PendingCast.ToString(CultureInfo.InvariantCulture) +
                    ", pendingCastAgeSeconds=" + PendingCastAgeSeconds.ToString("0.###", CultureInfo.InvariantCulture) +
                    ", pendingCastStalls=" + PendingCastStallCount.ToString(CultureInfo.InvariantCulture) +
                    ", autoCastBackoff=" + AutoCastBackoff.ToString(CultureInfo.InvariantCulture) +
                    ", loggedPhases=" + LoggedPhaseCount.ToString(CultureInfo.InvariantCulture) +
                    ", failures=" + FailureCount.ToString(CultureInfo.InvariantCulture) +
                    ", diagnostics=" + DiagnosticCount.ToString(CultureInfo.InvariantCulture) +
                    ", autoCastAttempts=" + AutoCastAttemptCount.ToString(CultureInfo.InvariantCulture) +
                    ", autoCastConfirmed=" + AutoCastConfirmedCount.ToString(CultureInfo.InvariantCulture) +
                    ", applications=" + AutomationApplicationCount.ToString(CultureInfo.InvariantCulture) +
                    ", readyChargeApplications=" + ReadyChargeApplicationCount.ToString(CultureInfo.InvariantCulture) +
                    ", readyChargeTargets=" + ReadyChargeTargetApplicationCount.ToString(CultureInfo.InvariantCulture) +
                    ", miniGameComplete=" + MiniGameCompleteApplicationCount.ToString(CultureInfo.InvariantCulture) +
                    ", ownerPolicies=[" + (OwnerPolicies.Count == 0 ? "none" : string.Join("|", OwnerPolicies.ToArray())) + "]";
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

        private sealed class FishingAutomationDiagnosticState
        {
            public int ObservedCount { get; set; }
            public int PublishedCount { get; set; }
            public string LastPublishedStatus { get; set; } = string.Empty;
            public string LastPublishedDetails { get; set; } = string.Empty;
            public DateTimeOffset LastPublishedAtUtc { get; set; }
        }

        private enum FishingDiagnosticKey
        {
            Other,
            AutoCast,
            Phase,
            InstantBite,
            MiniGameSkip,
            MiniGame,
            ReadyCharge,
            CastCharge,
            Animation,
            Log
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
