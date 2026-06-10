using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using static DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class FishingAutomationService : IFishingAutomationApi
    {
        private readonly DtmApiRuntime runtime;
        private readonly Dictionary<string, FishingAutomationOptions> fishingOptions = new Dictionary<string, FishingAutomationOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FishingAutomationState> fishingStates = new Dictionary<string, FishingAutomationState>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedFishingPhases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<object, DateTimeOffset> fishingMiniGameStartedAt = new Dictionary<object, DateTimeOffset>();
        private readonly Dictionary<object, double> originalAnimatorSpeeds = new Dictionary<object, double>();
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

        internal int FishingAutoCastApplicationCount => fishingAutoCastApplications;

        internal string LastFishingAutoCastAttemptSummary { get; private set; } = string.Empty;

        internal int FishingMiniGameCompleteApplicationCount { get; private set; }

        internal bool SuppressFishingAutoCastForSmoke { get; set; }

        internal bool ForceFishingNoWaterForSmoke { get; set; }

        internal bool ForceFishingNoRodForSmoke { get; set; }

        internal object? FishingPoolOverrideForSmoke { get; set; }

        internal bool ForceFishingFishForSmoke { get; set; }

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
                ? new BridgeFeatureStatus(fishingHooksInstalled ? "configured-experimental-hook" : "configured-pending-hook", fishingHooksInstalled ? "Policy accepted and fishing phase hooks are installed; wait-phase InstantBite has smoke evidence, while broader automation remains experimental." : "Policy accepted; fishing phase and input hooks are not yet installed.")
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
            if ((DateTimeOffset.Now - lastFishingAutoCastAt).TotalSeconds < 0.5)
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
                LastFishingAutoCastAttemptSummary = "skipped=not-normal-state owner=" + ownerId;
                return;
            }

            object? agent = ReadStaticMember(dolocApi, "agent");
            if (agent != null && !ReadBoolMember(agent, "IsCurrentStateSupportUseItem", true))
            {
                state.Phase = "WaitingAction";
                state.LastReason = "auto:busy";
                LastFishingAutoCastAttemptSummary = "skipped=busy owner=" + ownerId + ", state=" + DescribeAgentState(dolocApi);
                return;
            }

            bool forcedNoWater = ForceFishingNoWaterForSmoke;
            object? smokeFishingPool = FishingPoolOverrideForSmoke;
            if (forcedNoWater || (smokeFishingPool == null && !HasFishingPoolInScene()))
            {
                state.Phase = "NoWater";
                state.LastReason = forcedNoWater ? "smoke:no-fishing-pool" : "auto:no-fishing-pool";
                if (forcedNoWater)
                {
                    ForceFishingNoWaterForSmoke = false;
                    runtime.SetHookStatus("Smoke.AutoFishingNoWaterFeedback", "verified", "0.2.3 toast policy", "No fishable-water state reached; player toast intentionally suppressed.");
                }
                LastFishingAutoCastAttemptSummary = "skipped=no-water owner=" + ownerId + ", forced=" + forcedNoWater;
                return;
            }

            bool forcedNoRod = ForceFishingNoRodForSmoke;
            object? rod = forcedNoRod ? null : ResolveFishingRodForAutomation(dolocApi, options);
            if (rod == null)
            {
                state.Phase = "NoRod";
                state.LastReason = forcedNoRod ? "smoke:no-rod" : (options.RequireSelectedFishingRod ? "auto:no-selected-rod" : "auto:no-rod");
                if (forcedNoRod)
                {
                    ForceFishingNoRodForSmoke = false;
                    runtime.SetHookStatus("Smoke.AutoFishingNoRodFeedback", "verified", "0.2.3 toast policy", "No fishing-rod state reached; player toast intentionally suppressed.");
                }
                LastFishingAutoCastAttemptSummary = "skipped=no-rod owner=" + ownerId + ", forced=" + forcedNoRod + ", requireSelected=" + options.RequireSelectedFishingRod;
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
                string smokePoolName = smokeFishingPool == null ? "none" : FirstText(ReadStringMember(smokeFishingPool, "PoolName"), smokeFishingPool.GetType().Name);
                LastFishingAutomationApplicationSummary = "owner=" + ownerId + ", behavior=AutoCast, rod=" + ReadStringMember(rod, "name") + ", smokePoolOverride=" + smokePoolName + ", applications=" + fishingAutoCastApplications;
                LastFishingAutoCastAttemptSummary = LastFishingAutomationApplicationSummary;
                runtime.RuntimeMonitor.Log("Fishing automation auto-cast invoked native BodyController.UseFishRod summary=" + LastFishingAutomationApplicationSummary + ".");
                runtime.SetHookStatus("Smoke.AutoFishingAutoCast", "experimental", "BodyController.UseFishRod", LastFishingAutomationApplicationSummary);
            }
            catch (Exception ex)
            {
                state.Phase = "AutoCastFailed";
                state.LastReason = ex.GetType().Name;
                LastFishingAutoCastAttemptSummary = "failed=" + ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Fishing auto-cast failed.", ex.ToString());
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
            }

            if (phase.Equals("Cast", StringComparison.OrdinalIgnoreCase) || phase.Equals("Pull", StringComparison.OrdinalIgnoreCase))
                TryApplyFishingAnimationSpeed(phase, source);

            LogOnce(loggedFishingPhases, phase, "Fishing phase hook observed phase=" + phase + " source=" + sourceName + ".");
        }

        internal void NotifyFishingMiniGameStart(object? gameHandle)
        {
            if (gameHandle != null)
                fishingMiniGameStartedAt[gameHandle] = DateTimeOffset.Now;
            NotifyFishingPhase("MiniGame", gameHandle);
        }

        internal void ApplyFishingMiniGameAutomationTick(object? gameHandle)
        {
            if (gameHandle == null)
                return;
            if (!fishingMiniGameStartedAt.ContainsKey(gameHandle))
                fishingMiniGameStartedAt[gameHandle] = DateTimeOffset.Now;
            TryApplyFishingMiniGameAutomation(gameHandle);
        }

        internal void NotifyFishingMiniGameStop(object? gameHandle)
        {
            if (gameHandle != null)
                fishingMiniGameStartedAt.Remove(gameHandle);
            NotifyFishingPhase("MiniGameStop", gameHandle);
        }

        internal bool ApplyFishingWaitAutomation(object waitState)
        {
            if (waitState == null || fishingStates.Count == 0 || fishingOptions.Count == 0)
                return false;

            if (!ReadBoolMember(waitState, "_waitForFishBite", false))
                return false;

            if (!TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return false;

            if (!options.InstantBite)
                return false;

            try
            {
                MethodInfo? rollFish = FindMethodInHierarchy(waitState.GetType(), "RollFish", 0);
                if (rollFish == null)
                    return false;

                object? rolledValue = rollFish.Invoke(waitState, null);
                bool rolled = rolledValue is bool value && value;
                int rollAttempts = 1;
                if (ForceFishingFishForSmoke && options.AutoCompleteMiniGame && !options.SkipMiniGame)
                {
                    while ((!rolled || !TryReadFishingCacheFish(waitState, out _)) && rollAttempts < 100)
                    {
                        rolledValue = rollFish.Invoke(waitState, null);
                        rolled = rolledValue is bool retryValue && retryValue;
                        rollAttempts++;
                    }
                }
                if (!rolled)
                {
                    state.Phase = "Wait:AutoBiteFailed";
                    state.LastReason = "auto:InstantBite:no-fish";
                    runtime.SetHookStatus("Smoke.AutoFishingPhase", "failed", "AgentStateFishingWait.OnPlay Postfix", "InstantBite attempted but RollFish returned false.");
                    return false;
                }
                bool forceFishSatisfied = !ForceFishingFishForSmoke || !options.AutoCompleteMiniGame || options.SkipMiniGame || TryReadFishingCacheFish(waitState, out _);

                WriteBoolMember(waitState, "_waitForFishBite", false);
                WriteBoolMember(waitState, "_hasRolled", true);
                WriteFloatMember(waitState, "_hookProbability", 1f);
                WriteFloatMember(waitState, "_fishOnHookDuration", 100f);
                TryRefreshFishingRendererAfterBite(waitState);
                string autoHook = TryAdvanceFishingBite(waitState, options);

                state.Phase = string.IsNullOrWhiteSpace(autoHook) ? "Wait:AutoBite" : "Wait:AutoBite:" + autoHook;
                state.LastReason = string.IsNullOrWhiteSpace(autoHook) ? "auto:InstantBite" : "auto:InstantBite+" + autoHook;
                FishingAutomationApplicationCount++;

                object? body = ReadMember(waitState, "body");
                object? cache = body == null ? null : ReadMember(body, "FishingCache");
                object? fishProto = cache == null ? null : ReadMember(cache, "FishProto");
                object? pool = cache == null ? null : ReadMember(cache, "FishingPool");
                string fishId = fishProto == null ? "unknown" : ReadStringMember(fishProto, "Id");
                string poolName = pool == null ? "unknown" : ReadStringMember(pool, "PoolName");
                bool isFish = fishProto != null && ReadBoolMember(fishProto, "IsFish", false);
                LastFishingAutomationApplicationSummary = "owner=" + ownerId + ", behavior=InstantBite, phase=Wait, autoHook=" + FirstText(autoHook, "none") + ", fish=" + fishId + ", isFish=" + isFish + ", pool=" + poolName + ", autoCompleteMiniGame=" + options.AutoCompleteMiniGame + ", skipMiniGame=" + options.SkipMiniGame + ", forceFishForSmoke=" + ForceFishingFishForSmoke + ", forceFishSatisfied=" + forceFishSatisfied + ", rollAttempts=" + rollAttempts + ", applications=" + FishingAutomationApplicationCount;

                if (options.VerboseLogging || !loggedFishingPhases.Contains("AutoBite:" + ownerId))
                    runtime.RuntimeMonitor.Log("Fishing automation instant-bite applied by " + ownerId + " fish=" + fishId + " pool=" + poolName + ".");
                loggedFishingPhases.Add("AutoBite:" + ownerId);
                runtime.SetHookStatus("Smoke.AutoFishingPhase", "verified", "AgentStateFishingWait.OnPlay Postfix", LastFishingAutomationApplicationSummary);
                if (options.SkipMiniGame && autoHook.Equals("AgentStateFishingPull", StringComparison.OrdinalIgnoreCase))
                    runtime.SetHookStatus("Smoke.AutoFishingMiniGameSkip", "verified", "AgentStateFishingWait.OnPlay -> AgentStateFishingPull", LastFishingAutomationApplicationSummary);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Fishing wait automation failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AutoFishingPhase", "failed", "AgentStateFishingWait.OnPlay Postfix", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private static bool TryReadFishingCacheFish(object waitState, out object? fishProto)
        {
            fishProto = null;
            object? body = ReadMember(waitState, "body");
            object? cache = body == null ? null : ReadMember(body, "FishingCache");
            fishProto = cache == null ? null : ReadMember(cache, "FishProto");
            return fishProto != null && ReadBoolMember(fishProto, "IsFish", false);
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
            options.AutoRecast = true;
            options.RequireSelectedFishingRod = true;
            options.CastReleaseProgress = Math.Min(1, Math.Max(0, options.CastReleaseProgress));
            options.RecastDelaySeconds = ClampSeconds(options.RecastDelaySeconds, 0.05, 10);
            options.FastAnimationMultiplier = ClampMultiplier(options.FastAnimationMultiplier);
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

        private static string TryAdvanceFishingBite(object waitState, FishingAutomationOptions options)
        {
            object? body = ReadMember(waitState, "body");
            object? stateManager = body == null ? null : ReadMember(body, "StateManager");
            object? cache = body == null ? null : ReadMember(body, "FishingCache");
            object? fishProto = cache == null ? null : ReadMember(cache, "FishProto");
            bool isFish = fishProto != null && ReadBoolMember(fishProto, "IsFish", false);
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
            int energy = globalParameter == null ? 0 : ReadIntMember(globalParameter, "FishingEnergyCost", 0);
            MethodInfo? costEnergy = dolocApi?.GetMethod("CostEnergy", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
            if (energy > 0)
                costEnergy?.Invoke(null, new object[] { energy });

            string targetTypeName = isFish && !options.SkipMiniGame
                ? "DolocTown.AgentStateFishingBattle, Assembly-CSharp"
                : "DolocTown.AgentStateFishingPull, Assembly-CSharp";
            Type? targetType = ResolveType(targetTypeName);
            if (stateManager == null || targetType == null)
                return string.Empty;

            MethodInfo? getStateOpen = stateManager.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "GetState" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            object? targetState = getStateOpen?.MakeGenericMethod(targetType).Invoke(stateManager, null);
            if (targetState == null)
                return string.Empty;

            if (targetType.FullName == "DolocTown.AgentStateFishingPull")
                WriteBoolMember(targetState, "IsFailed", false);

            MethodInfo? overwrite = stateManager.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "Overwrite" && !m.IsGenericMethodDefinition && m.GetParameters().Length >= 1);
            if (overwrite == null)
                return string.Empty;

            ParameterInfo[] parameters = overwrite.GetParameters();
            object?[] args = parameters.Length >= 2 ? new object?[] { targetState, true } : new object?[] { targetState };
            overwrite.Invoke(stateManager, args);
            return targetType.Name;
        }

        private void TryApplyFishingAnimationSpeed(string phase, object? stateSource)
        {
            if (stateSource == null || !TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return;
            if (!options.FastAnimations || options.FastAnimationMultiplier <= 1)
                return;

            object? body = ReadMember(stateSource, "body");
            if (body == null)
                return;
            double multiplier = ClampMultiplier(options.FastAnimationMultiplier);
            var samples = new List<string>();
            int changed = ApplyAnimatorSpeed(ReadMember(body, "animator"), multiplier, "body", samples);
            object? fishRodRenderer = ReadMember(body, "fishRodRenderer");
            if (fishRodRenderer != null)
                changed += ApplyAnimatorSpeed(ReadMember(fishRodRenderer, "_animator"), multiplier, "fishRodRenderer", samples);
            if (changed <= 0)
                return;

            FishingAutomationApplicationCount++;
            state.Phase = phase + ":FastAnimation";
            state.LastReason = "auto:FastAnimation";
            LastFishingAutomationApplicationSummary = "owner=" + ownerId + ", behavior=FastAnimation, phase=" + phase + ", multiplier=" + multiplier.ToString("0.###") + ", animators=" + changed + ", samples=" + string.Join(";", samples.ToArray()) + ", applications=" + FishingAutomationApplicationCount;
            runtime.RuntimeMonitor.Log("Fishing automation animation speed applied by " + ownerId + " phase=" + phase + " multiplier=" + multiplier.ToString("0.###") + " animators=" + changed + ".");
            runtime.SetHookStatus("Smoke.AutoFishingAnimationSpeed", "experimental", "AgentStateFishingCast/Pull.OnEnter", LastFishingAutomationApplicationSummary);
        }

        private void TryApplyFishingMiniGameAutomation(object? gameHandle)
        {
            if (gameHandle == null || !TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return;
            if (!options.AutoCompleteMiniGame || options.SkipMiniGame)
                return;

            try
            {
                FieldInfo? statusField = null;
                for (Type? type = gameHandle.GetType(); type != null && statusField == null; type = type.BaseType)
                    statusField = type.GetField("currentGameStatus", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (statusField == null || !statusField.FieldType.IsEnum)
                    return;

                object? currentStatus = statusField.GetValue(gameHandle);
                if (currentStatus != null && !currentStatus.ToString()!.Equals("Running", StringComparison.OrdinalIgnoreCase))
                {
                    fishingMiniGameStartedAt.Remove(gameHandle);
                    return;
                }

                if (!fishingMiniGameStartedAt.TryGetValue(gameHandle, out DateTimeOffset startedAt))
                {
                    fishingMiniGameStartedAt[gameHandle] = DateTimeOffset.Now;
                    return;
                }

                double visibleSeconds = (DateTimeOffset.Now - startedAt).TotalSeconds;
                if (visibleSeconds < 0.75)
                {
                    state.Phase = "MiniGame:Running";
                    state.LastReason = "auto:CompleteMiniGame:waiting";
                    return;
                }

                object success = Enum.Parse(statusField.FieldType, "Success");
                statusField.SetValue(gameHandle, success);
                fishingMiniGameStartedAt.Remove(gameHandle);
                FishingAutomationApplicationCount++;
                FishingMiniGameCompleteApplicationCount++;
                state.Phase = "MiniGame:AutoComplete";
                state.LastReason = "auto:CompleteMiniGame";
                LastFishingAutomationApplicationSummary = "owner=" + ownerId + ", behavior=AutoCompleteMiniGame, status=Success, skip=false, visibleSeconds=" + visibleSeconds.ToString("0.###", CultureInfo.InvariantCulture) + ", applications=" + FishingAutomationApplicationCount;
                LastFishingMiniGameCompleteSummary = LastFishingAutomationApplicationSummary;
                runtime.RuntimeMonitor.Log("Fishing automation completed minigame status by " + ownerId + " through delayed FishingGameScrollBar.UpdateGame currentGameStatus=Success visibleSeconds=" + visibleSeconds.ToString("0.###", CultureInfo.InvariantCulture) + ".");
                runtime.SetHookStatus("Smoke.AutoFishingMiniGameComplete", "verified", "FishingGameScrollBar.UpdateGame Postfix", LastFishingAutomationApplicationSummary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Fishing minigame completion failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AutoFishingMiniGameComplete", "failed", "FishingGameScrollBar.UpdateGame Postfix", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static object? ResolveFishingRodForAutomation(Type? dolocApi, FishingAutomationOptions options)
        {
            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            if (selectedItem != null && IsTypeOrBase(selectedItem.GetType(), "DolocTown.ItemFishingRod"))
                return selectedItem;
            if (options.RequireSelectedFishingRod)
                return null;

            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? inventorySystem = archive == null ? null : ReadMember(archive, "InventorySystem");
            object? inventory = inventorySystem == null ? null : ReadMember(inventorySystem, "inventory");
            MethodInfo? readAll = inventory?.GetType().GetMethod("ReadAll", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            object? all = readAll?.Invoke(inventory, null);
            foreach (object item in EnumerateObjects(all))
            {
                if (IsTypeOrBase(item.GetType(), "DolocTown.ItemFishingRod"))
                    return item;
            }
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
            if (originalAnimatorSpeeds.Count == 0)
                return;

            int restored = 0;
            foreach (KeyValuePair<object, double> entry in new List<KeyValuePair<object, double>>(originalAnimatorSpeeds))
            {
                if (TryWriteAnimatorSpeed(entry.Key, entry.Value))
                    restored++;
            }
            originalAnimatorSpeeds.Clear();
            runtime.RuntimeMonitor.Log("Experimental animator speeds restored reason=" + reason + " restored=" + restored + ".");
            runtime.SetHookStatus("Smoke.AutoFishingAnimationSpeedRestore", "experimental", "Fishing/AgentState lifecycle boundary", "reason=" + reason + ", restored=" + restored);
        }
    }
}
