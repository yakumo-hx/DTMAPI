using System;
using System.Collections.Generic;
using global::DTMAPI.Abstractions;


namespace Yuuka.DTMAPI.AutoFishing
{
    internal sealed class FishingPrimitiveHookRuntime
    {
        private readonly FishingProductContext runtime;
        private readonly FishingPrimitivesService primitives;
        private readonly FishingAnimationController readyAnimation = new FishingAnimationController();
        private readonly FishingAnimationNativeCache animationNativeCache = new FishingAnimationNativeCache();
        private readonly FishingInputOverride inputOverride = new FishingInputOverride();
        private readonly FishingVisibleReelInputState visibleReelInput;
        private readonly Func<DateTimeOffset> clock;
        private readonly HashSet<object> readyTargets = new HashSet<object>();
        private readonly HashSet<object> readyReleased = new HashSet<object>();
        private readonly Dictionary<object, double> animatorSpeeds = new Dictionary<object, double>();
        private readonly Dictionary<object, double> hookGravityScales = new Dictionary<object, double>();
        private readonly Dictionary<object, FishingHookVelocityOriginal> hookVelocities = new Dictionary<object, FishingHookVelocityOriginal>();
        private readonly Dictionary<object, double> pullDurations = new Dictionary<object, double>();
        private object? currentReadyState;
        private DateTimeOffset lastPullDurationScaleAtUtc = DateTimeOffset.MinValue;
        private bool fishingHooksInstalled;

        internal FishingPrimitiveHookRuntime(FishingProductContext runtime, FishingPrimitivesService primitives, Func<DateTimeOffset>? clock = null)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.primitives = primitives ?? throw new ArgumentNullException(nameof(primitives));
            this.clock = clock ?? (() => DateTimeOffset.UtcNow);
            visibleReelInput = new FishingVisibleReelInputState(this.clock);
        }

        public bool HasActiveRuntimeConsumer => fishingHooksInstalled && primitives.HasActiveSession;
        public bool FishingHooksInstalled => fishingHooksInstalled;
        public void SetFishingHooksInstalled(bool installed)
        {
            fishingHooksInstalled = installed;
            if (!installed)
                visibleReelInput.Clear();
        }

        public void NotifyFishingNativeFrame()
        {
            FishingRuntimeSession? session = primitives.TryGetActiveSession();
            if (session == null)
            {
                visibleReelInput.Clear();
                return;
            }
            FishingVisibleReelFrameResult result = visibleReelInput.OnNativeFrame(primitives.CurrentWaitState, session.Phase == FishingPrimitivePhase.BiteReady);
            if (result == FishingVisibleReelFrameResult.Retried)
                primitives.RecordVisibleReelRetry();
            if (result == FishingVisibleReelFrameResult.TimedOut)
            {
                primitives.RecordVisibleReelTimeout();
                primitives.NotifyVisibleReelTimedOut(session, "visible-reel-input-timeout");
            }
        }

        public void NotifyFishingPhase(string phase, object? source)
        {
            if (string.IsNullOrWhiteSpace(phase) || !HasActiveRuntimeConsumer)
                return;
            primitives.NotifyHookPhase(phase, source);
            if (phase.Equals("MiniGame", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("Pull", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("Cooldown", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("NativeExit", StringComparison.OrdinalIgnoreCase))
            {
                visibleReelInput.Clear();
            }
            if (!phase.Equals("Ready", StringComparison.OrdinalIgnoreCase))
                ClearReadyState();
            if (source != null && (phase.Equals("Ready", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("Cast", StringComparison.OrdinalIgnoreCase) ||
                phase.Equals("Pull", StringComparison.OrdinalIgnoreCase)))
            {
                ApplyAnimationSpeed(phase, source);
            }
        }

        public void ApplyFishingReadyAutomation(object readyState)
        {
            if (readyState == null || !primitives.TryGetReadyPolicy(out _, out double chargeRatio, out double multiplier))
                return;
            currentReadyState = readyState;
            readyTargets.Add(readyState);
            double target = ClampRatio(chargeRatio);
            if (target <= 0d || multiplier <= 1d || readyReleased.Contains(readyState))
                return;
            if (readyAnimation.TryAdvanceReadyCharge(readyState, ClampMultiplier(multiplier), out _, out _, out _, out _))
                primitives.RecordReadyChargeApplication();
        }

        public bool TryOverrideFishingReadyChargeInput(string inputName, out bool value)
        {
            value = false;
            if (!inputName.Equals("NormalUseToolInProgress", StringComparison.OrdinalIgnoreCase) || currentReadyState == null ||
                !primitives.TryGetReadyPolicy(out _, out double chargeRatio, out _))
                return false;
            double target = ClampRatio(chargeRatio);
            double threshold = target >= 0.999d ? 0.995d : target;
            bool available = readyAnimation.TryReadReadyProgress(currentReadyState, out double progress);
            value = target > 0d && available && progress < threshold;
            if (!value)
                readyReleased.Add(currentReadyState);
            return true;
        }

        public bool ApplyFishingWaitAutomation(object waitState, string hookSource)
        {
            if (!HasActiveRuntimeConsumer || waitState == null || (hookSource ?? string.Empty).IndexOf("OnPlay", StringComparison.OrdinalIgnoreCase) < 0)
                return false;
            if (!primitives.TryReadWaitState(waitState, out bool current, out bool waitingForBite, out bool isFish) || !current)
                return false;
            primitives.NotifyHookWaitPlayable(waitState, waitingForBite, isFish, hookSource ?? string.Empty);
            if (waitingForBite)
                visibleReelInput.Clear();
            return true;
        }

        public void ConfirmFishingWaitNativeReelAccepted(object waitState, object? nextState)
        {
            if (!HasActiveRuntimeConsumer || waitState == null)
                return;
            if (visibleReelInput.TryConfirmNativeEffectEntered(waitState, nextState))
                primitives.RecordVisibleReelNativeAccepted();
        }

        public void NotifyFishingMiniGameStart(object gameHandle)
        {
            visibleReelInput.Clear();
            if (gameHandle != null && HasActiveRuntimeConsumer)
                primitives.NotifyHookPhase("MiniGame", gameHandle);
        }

        public void PrepareFishingMiniGameAutomationInput(object gameHandle)
        {
            inputOverride.Clear();
            if (gameHandle == null || !HasActiveRuntimeConsumer || !primitives.TryDecideMiniGameInput(gameHandle, out FishingSyntheticInputAction action, out _))
                return;
            FishingMiniGameInputDecision decision = action == FishingSyntheticInputAction.Hold
                ? FishingMiniGameInputDecision.HoldStable
                : action == FishingSyntheticInputAction.TapBonus
                    ? FishingMiniGameInputDecision.TapBonus
                    : FishingMiniGameInputDecision.Release;
            inputOverride.Set(gameHandle, decision, clock().AddMilliseconds(250));
        }

        public void ApplyFishingMiniGameAutomationTick(object gameHandle)
        {
            if (gameHandle == null || !HasActiveRuntimeConsumer)
                return;
            if (inputOverride.IsActive && ReferenceEquals(inputOverride.GameHandle, gameHandle))
                inputOverride.Clear();
            if (primitives.TryReadMiniGameStatus(gameHandle, out FishingNativeMiniGameStatus status))
                primitives.NotifyMiniGameStatus(gameHandle, status);
        }

        public void NotifyFishingMiniGameStop(object gameHandle)
        {
            if (gameHandle != null && inputOverride.IsActive && ReferenceEquals(inputOverride.GameHandle, gameHandle))
                inputOverride.Clear();
            if (HasActiveRuntimeConsumer)
                primitives.NotifyHookPhase("MiniGameStop", gameHandle);
        }

        public bool TryOverrideFishingMiniGameInput(string inputName, out bool value)
        {
            value = false;
            if (visibleReelInput.TryConsume(inputName, out value))
            {
                primitives.RecordVisibleReelConsumed();
                return true;
            }
            if (!inputOverride.IsActive)
                return false;
            if (clock() > inputOverride.ExpiresAtUtc)
            {
                inputOverride.Clear();
                return false;
            }
            value = inputOverride.Decision == FishingMiniGameInputDecision.HoldStable
                ? inputName.EndsWith("InProgress", StringComparison.OrdinalIgnoreCase)
                : inputOverride.Decision == FishingMiniGameInputDecision.TapBonus && !inputName.EndsWith("InProgress", StringComparison.OrdinalIgnoreCase);
            return true;
        }

        public bool TryQueueVisibleReelInput(object waitState)
        {
            if (waitState == null || !HasActiveRuntimeConsumer || !fishingHooksInstalled)
                return false;
            bool queued = visibleReelInput.Queue(waitState, out bool newlyQueued);
            if (newlyQueued)
                primitives.RecordVisibleReelQueued();
            return queued;
        }

        public void ClearQueuedFishingReelInput()
        {
            visibleReelInput.Clear();
        }

        public void AdjustFishingCastHookPhysics(object renderer)
        {
            if (renderer == null || !TryGetMultiplier("Cast", out double multiplier))
                return;
            try
            {
                if (!animationNativeCache.TryGetHook(renderer, out object? hook) || hook == null)
                    return;
                bool changed = false;
                if (hookVelocities.TryGetValue(hook, out FishingHookVelocityOriginal velocityOriginal))
                    animationNativeCache.TryRestoreHookVelocity(hook, velocityOriginal.X, velocityOriginal.Y);
                if (animationNativeCache.TryScaleHookVelocity(hook, multiplier, out float originalX, out float originalY))
                {
                    if (!hookVelocities.ContainsKey(hook))
                        hookVelocities[hook] = new FishingHookVelocityOriginal(originalX, originalY);
                    changed = true;
                }
                if (animationNativeCache.TryGetHookBody(hook, out object? body) && body != null && animationNativeCache.TryReadGravity(body, out double current))
                {
                    if (!hookGravityScales.TryGetValue(body, out double original))
                        hookGravityScales[body] = original = current;
                    changed |= animationNativeCache.TryWriteGravity(body, original * multiplier * multiplier);
                }
                if (changed)
                    primitives.RecordAnimationApplication();
            }
            catch (Exception ex)
            {
                runtime.RuntimeMonitor.Log("First-party fishing CastHook animation adjustment failed: " + ex.GetType().Name + ": " + ex.Message, global::DTMAPI.Abstractions.LogLevel.Warn);
            }
        }

        public void AdjustFishingPullDurationResult(ref float result, string source)
        {
            if (result <= 0f || !TryGetMultiplier("Pull", out double multiplier))
                return;
            result = (float)Math.Max(0.01d, result / multiplier);
            lastPullDurationScaleAtUtc = clock();
            primitives.RecordAnimationApplication();
        }

        public void RestoreExperimentalAnimatorSpeeds(string reason)
        {
            foreach (KeyValuePair<object, double> item in animatorSpeeds)
                animationNativeCache.TryWriteAnimatorSpeed(item.Key, item.Value);
            animatorSpeeds.Clear();
            foreach (KeyValuePair<object, FishingHookVelocityOriginal> item in hookVelocities)
                animationNativeCache.TryRestoreHookVelocity(item.Key, item.Value.X, item.Value.Y);
            hookVelocities.Clear();
            foreach (KeyValuePair<object, double> item in hookGravityScales)
                animationNativeCache.TryWriteGravity(item.Key, item.Value);
            hookGravityScales.Clear();
            foreach (KeyValuePair<object, double> item in pullDurations)
                animationNativeCache.TryWritePullDuration(item.Key, item.Value);
            pullDurations.Clear();
            animationNativeCache.ClearCandidates();
        }

        public void NotifyFishingNativeExit(string source)
        {
            RestoreExperimentalAnimatorSpeeds(source);
            inputOverride.Clear();
            visibleReelInput.Clear();
            ClearReadyState();
        }

        internal void Reset(string reason)
        {
            RestoreExperimentalAnimatorSpeeds(reason);
            inputOverride.Clear();
            visibleReelInput.Clear();
            fishingHooksInstalled = false;
            ClearReadyState();
        }

        private void ClearReadyState()
        {
            readyTargets.Clear();
            readyReleased.Clear();
            currentReadyState = null;
        }

        private void ApplyAnimationSpeed(string phase, object source)
        {
            if (!TryGetMultiplier(phase, out double multiplier))
                return;
            int changed = 0;
            int candidateCount = animationNativeCache.CollectAnimatorCandidates(source);
            for (int i = 0; i < candidateCount; i++)
            {
                object animator = animationNativeCache.GetAnimatorCandidate(i);
                if (!animationNativeCache.TryReadAnimatorSpeed(animator, out double current))
                    continue;
                if (!animatorSpeeds.TryGetValue(animator, out double original))
                    animatorSpeeds[animator] = original = current;
                if (animationNativeCache.TryWriteAnimatorSpeed(animator, original * multiplier))
                    changed++;
            }
            animationNativeCache.ClearCandidates();
            if (phase.Equals("Pull", StringComparison.OrdinalIgnoreCase) &&
                clock() - lastPullDurationScaleAtUtc > TimeSpan.FromMilliseconds(500) &&
                animationNativeCache.TryReadPullDuration(source, out double duration))
            {
                if (duration > 0d)
                {
                    if (!pullDurations.TryGetValue(source, out double original))
                        pullDurations[source] = original = duration;
                    if (animationNativeCache.TryWritePullDuration(source, Math.Max(0.01d, original / multiplier)))
                        changed++;
                }
            }
            if (changed > 0)
                primitives.RecordAnimationApplication();
        }

        private bool TryGetMultiplier(string phase, out double multiplier)
        {
            multiplier = 1d;
            if (!primitives.TryGetAnimationRequest(out _, out FishingAnimationLeaseRequest request))
                return false;
            multiplier = phase.Equals("Ready", StringComparison.OrdinalIgnoreCase)
                ? request.ReadyMultiplier
                : phase.Equals("Cast", StringComparison.OrdinalIgnoreCase)
                    ? request.CastHookMultiplier
                    : phase.Equals("Pull", StringComparison.OrdinalIgnoreCase)
                        ? request.PullMultiplier
                        : 1d;
            multiplier = ClampMultiplier(multiplier);
            return multiplier > 1d;
        }

        private static double ClampRatio(double value) => double.IsNaN(value) || double.IsInfinity(value) ? 0d : Math.Max(0d, Math.Min(1d, value));
        private static double ClampMultiplier(double value) => double.IsNaN(value) || double.IsInfinity(value) ? 1d : Math.Max(1d, Math.Min(4d, value));

        private readonly struct FishingHookVelocityOriginal
        {
            internal FishingHookVelocityOriginal(float x, float y)
            {
                X = x;
                Y = y;
            }
            internal float X { get; }
            internal float Y { get; }
        }

    }
}
