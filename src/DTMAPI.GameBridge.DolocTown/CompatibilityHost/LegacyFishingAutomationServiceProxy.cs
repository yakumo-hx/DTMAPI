#pragma warning disable CS0618
using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// Mandatory callback/API broker proxy for the extracted frozen fishing
    /// executor. The legacy type name is retained deliberately for the existing
    /// compatibility topology; this class does not own the heavy native executor.
    /// </summary>
    internal sealed class LegacyFishingAutomationService : IFishingCompatibilityHookRuntime
    {
        private readonly CompatibilityHostBroker broker;
        private Action? update;
        private Action? notifyNativeFrame;
        private Action<string, object?>? notifyPhase;
        private Action<object?>? notifyMiniGameStart;
        private Action<object?>? prepareMiniGameInput;
        private Action<object?>? applyMiniGameTick;
        private Action<object?>? notifyMiniGameStop;
        private Func<object, bool>? applyWait;
        private Func<object, string, bool>? applyWaitWithSource;
        private Action<object, object?>? confirmWaitAccepted;
        private RefFloatString? adjustPullDuration;
        private Action<object?>? applyReadyAutomation;
        private Action<object?>? applyFishingReadyChargeSpeed;
        private StringOutBool? overrideReadyInput;
        private Action<object?>? adjustCastHookPhysics;
        private StringOutBool? overrideMiniGameInput;
        private Func<object, bool>? queueVisibleReel;
        private Action? clearQueuedReel;
        private Action<string>? restoreAnimatorSpeeds;
        private Action<string>? notifyNativeExit;
        private Action<string>? resetRuntimeState;
        internal LegacyFishingAutomationService(DtmApiRuntime runtime) { broker = CompatibilityHostBroker.For(runtime); }
        private object Backend => broker.GetService("FishingAutomation");

        internal int FishingAutomationApplicationCount => Read("FishingAutomationApplicationCount", 0);
        internal string LastFishingAutomationApplicationSummary => Read("LastFishingAutomationApplicationSummary", string.Empty);
        internal string LastFishingMiniGameCompleteSummary => Read("LastFishingMiniGameCompleteSummary", string.Empty);
        internal string LastFishingAnimationSpeedSummary => Read("LastFishingAnimationSpeedSummary", string.Empty);
        internal int FishingAutoCastApplicationCount => Read("FishingAutoCastApplicationCount", 0);
        internal bool HasPendingFishingAutoCast => Read("HasPendingFishingAutoCast", false);
        internal int ReadyAccessorFailureCount => Read("ReadyAccessorFailureCount", 0);
        public int ReadyAccessorBuildCount => Read("ReadyAccessorBuildCount", 0);
        public int ReadyAccessorRebuildCount => Read("ReadyAccessorRebuildCount", 0);
        public int ReadyAccessorBuildFailureCount => Read("ReadyAccessorBuildFailureCount", 0);
        internal long NativeObjectSearchCount => Read("NativeObjectSearchCount", 0L);
        public int ReadyAccessorInvocationFailureCount => Read("ReadyAccessorInvocationFailureCount", 0);
        internal bool HasEnabledOwner => Read("HasEnabledOwner", false);
        public bool HasActiveRuntimeConsumer => Read("HasActiveRuntimeConsumer", false);
        internal int ConfiguredOwnerCount => Read("ConfiguredOwnerCount", 0);
        internal int OwnerStateCount => Read("OwnerStateCount", 0);
        internal int FishingAutoCastPendingStallCount => Read("FishingAutoCastPendingStallCount", 0);
        internal int FishingInstantBiteApplicationCount => Read("FishingInstantBiteApplicationCount", 0);
        internal int FishingSkipMiniGameApplicationCount => Read("FishingSkipMiniGameApplicationCount", 0);
        internal int FishingReadyChargeApplicationCount => Read("FishingReadyChargeApplicationCount", 0);
        internal int FishingReadyChargeTargetApplicationCount => Read("FishingReadyChargeTargetApplicationCount", 0);
        internal string LastFishingAutoCastAttemptSummary => Read("LastFishingAutoCastAttemptSummary", string.Empty);
        internal string LastFishingAutomationLifecycleStatus => Read("LastFishingAutomationLifecycleStatus", "not-observed");
        internal string LastFishingAutomationLifecycleSummary => Read("LastFishingAutomationLifecycleSummary", "not-observed");
        internal long FishingNativeExitObservedCount => Read("FishingNativeExitObservedCount", 0L);
        internal int FishingMiniGameCompleteApplicationCount => Read("FishingMiniGameCompleteApplicationCount", 0);
        public bool FishingHooksInstalled => Read("FishingHooksInstalled", false);

        internal void Update() => (update ??= broker.BindIfServiceLoaded<Action>("FishingAutomation", "Update"))?.Invoke();
        public void SetFishingHooksInstalled(bool installed) => CompatibilityHostBroker.Invoke(Backend, "SetFishingHooksInstalled", installed);
        public void NotifyFishingNativeFrame() => (notifyNativeFrame ??= broker.BindIfServiceLoaded<Action>("FishingAutomation", "NotifyFishingNativeFrame"))?.Invoke();
        public void Configure(IManifest owner, FishingAutomationOptions options) => CompatibilityHostBroker.Invoke(Backend, "Configure", owner, options);
        public void SetEnabled(IManifest owner, bool enabled, string reason) => CompatibilityHostBroker.Invoke(Backend, "SetEnabled", owner, enabled, reason);
        public FishingAutomationState GetState(string uniqueId) => CompatibilityHostBroker.Invoke<FishingAutomationState>(Backend, "GetState", uniqueId);
        internal BridgeFeatureStatus GetStatus(string uniqueId) => CompatibilityHostBroker.Invoke<BridgeFeatureStatus>(Backend, "GetStatus", uniqueId);
        internal bool TryGetEnabledFishingAutomationOwner(out string ownerId) => TryOutString("TryGetEnabledFishingAutomationOwner", out ownerId);
        internal int RemoveOwner(string ownerId, string reason) => Call("RemoveOwner", 0, ownerId, reason);
        internal int RemoveAllOwners(string reason) => Call("RemoveAllOwners", 0, reason);
        internal int CountOwnerResources(string ownerId) => Call("CountOwnerResources", 0, ownerId);
        public void NotifyFishingPhase(string phase, object? source) => (notifyPhase ??= broker.BindIfServiceLoaded<Action<string, object?>>("FishingAutomation", "NotifyFishingPhase"))?.Invoke(phase, source);
        public void NotifyFishingMiniGameStart(object? gameHandle) => (notifyMiniGameStart ??= broker.BindIfServiceLoaded<Action<object?>>("FishingAutomation", "NotifyFishingMiniGameStart"))?.Invoke(gameHandle);
        public void PrepareFishingMiniGameAutomationInput(object? gameHandle) => (prepareMiniGameInput ??= broker.BindIfServiceLoaded<Action<object?>>("FishingAutomation", "PrepareFishingMiniGameAutomationInput"))?.Invoke(gameHandle);
        public void ApplyFishingMiniGameAutomationTick(object? gameHandle) => (applyMiniGameTick ??= broker.BindIfServiceLoaded<Action<object?>>("FishingAutomation", "ApplyFishingMiniGameAutomationTick"))?.Invoke(gameHandle);
        public void NotifyFishingMiniGameStop(object? gameHandle) => (notifyMiniGameStop ??= broker.BindIfServiceLoaded<Action<object?>>("FishingAutomation", "NotifyFishingMiniGameStop"))?.Invoke(gameHandle);
        internal bool ApplyFishingWaitAutomation(object waitState) => (applyWait ??= broker.BindIfServiceLoaded<Func<object, bool>>("FishingAutomation", "ApplyFishingWaitAutomation"))?.Invoke(waitState) ?? false;
        public bool ApplyFishingWaitAutomation(object waitState, string hookSource) => (applyWaitWithSource ??= broker.BindIfServiceLoaded<Func<object, string, bool>>("FishingAutomation", "ApplyFishingWaitAutomation"))?.Invoke(waitState, hookSource) ?? false;
        public void ConfirmFishingWaitNativeReelAccepted(object waitState, object? nextState) => (confirmWaitAccepted ??= broker.BindIfServiceLoaded<Action<object, object?>>("FishingAutomation", "ConfirmFishingWaitNativeReelAccepted"))?.Invoke(waitState, nextState);
        internal static bool TryForceFishingBite(object waitState, bool requireFish, out int rollAttempts, out bool forceFishSatisfied, out bool nativeTipInvoked, out float hookDuration) { rollAttempts = 0; forceFishSatisfied = false; nativeTipInvoked = false; hookDuration = 0; return false; }
        internal static FishingAutomationOptions NormalizeFishingAutomationOptions(FishingAutomationOptions? options)
        {
            options ??= new FishingAutomationOptions();
            return new FishingAutomationOptions {
                BiteWaitMode = Enum.IsDefined(typeof(FishingBiteWaitMode), options.BiteWaitMode) ? options.BiteWaitMode : FishingBiteWaitMode.NativeWait,
                ResultMode = Enum.IsDefined(typeof(FishingResultMode), options.ResultMode) ? options.ResultMode : FishingResultMode.AutoCompleteVisibleMiniGame,
                AnimationMode = Enum.IsDefined(typeof(FishingAnimationMode), options.AnimationMode) ? options.AnimationMode : FishingAnimationMode.Normal,
                RecastDelaySeconds = Clamp(options.RecastDelaySeconds, .05, 10), CastChargeRatio = Clamp(options.CastChargeRatio, 0, 1),
                AnimationMultiplier = Clamp(options.AnimationMultiplier, 1, 4), StopOnManualMove = options.StopOnManualMove, VerboseLogging = options.VerboseLogging };
        }
        public void AdjustFishingPullDurationResult(ref float duration, string source)
        {
            RefFloatString? callback = adjustPullDuration ??= broker.BindIfServiceLoaded<RefFloatString>("FishingAutomation", "AdjustFishingPullDurationResult");
            callback?.Invoke(ref duration, source);
        }
        public void ApplyFishingReadyAutomation(object? readyState) => (applyReadyAutomation ??= broker.BindIfServiceLoaded<Action<object?>>("FishingAutomation", "ApplyFishingReadyAutomation"))?.Invoke(readyState);
        internal void ApplyFishingReadyChargeSpeed(object? readyState)
        {
            (applyFishingReadyChargeSpeed ??= CompatibilityHostBroker.Bind<Action<object?>>(Backend, "ApplyFishingReadyChargeSpeed"))(readyState);
        }
        public bool TryOverrideFishingReadyChargeInput(string inputName, out bool value)
        {
            value = false;
            StringOutBool? callback = overrideReadyInput ??= broker.BindIfServiceLoaded<StringOutBool>("FishingAutomation", "TryOverrideFishingReadyChargeInput");
            return callback != null && callback(inputName, out value);
        }
        public void AdjustFishingCastHookPhysics(object? fishRodRenderer) => (adjustCastHookPhysics ??= broker.BindIfServiceLoaded<Action<object?>>("FishingAutomation", "AdjustFishingCastHookPhysics"))?.Invoke(fishRodRenderer);
        public bool TryOverrideFishingMiniGameInput(string inputName, out bool value)
        {
            value = false;
            StringOutBool? callback = overrideMiniGameInput ??= broker.BindIfServiceLoaded<StringOutBool>("FishingAutomation", "TryOverrideFishingMiniGameInput");
            return callback != null && callback(inputName, out value);
        }
        public bool TryQueueVisibleReelInput(object waitState) => (queueVisibleReel ??= broker.BindIfServiceLoaded<Func<object, bool>>("FishingAutomation", "TryQueueVisibleReelInput"))?.Invoke(waitState) ?? false;
        public void ClearQueuedFishingReelInput() => (clearQueuedReel ??= broker.BindIfServiceLoaded<Action>("FishingAutomation", "ClearQueuedFishingReelInput"))?.Invoke();
        internal string GetFishingAutomationLifecycleSummary() => Call("GetFishingAutomationLifecycleSummary", "compatibilityStatus=inactive/no-consumer, fishingStates=0, fishingOptions=0");
        internal FishingAutomationLifecycleSnapshot GetFishingAutomationLifecycleSnapshot(string boundary) => new FishingAutomationLifecycleSnapshot(CompatibilityHostBroker.Invoke(Backend, "GetFishingAutomationLifecycleSnapshot", boundary)!);
        internal void NotifyFishingEnvironmentReset(string reason) => CallIfLoaded("NotifyFishingEnvironmentReset", reason);
        public void RestoreExperimentalAnimatorSpeeds(string reason) => (restoreAnimatorSpeeds ??= broker.BindIfServiceLoaded<Action<string>>("FishingAutomation", "RestoreExperimentalAnimatorSpeeds"))?.Invoke(reason);
        public void NotifyFishingNativeExit(string reason) => (notifyNativeExit ??= broker.BindIfServiceLoaded<Action<string>>("FishingAutomation", "NotifyFishingNativeExit"))?.Invoke(reason);
        internal void ResetFishingRuntimeState(string reason) => (resetRuntimeState ??= broker.BindIfServiceLoaded<Action<string>>("FishingAutomation", "ResetFishingRuntimeState"))?.Invoke(reason);

        private T Read<T>(string name, T fallback) => broker.TryGetService("FishingAutomation", out object? value) && value != null ? CompatibilityHostBroker.ReadProperty(value, name, fallback) : fallback;
        private T Call<T>(string name, T fallback, params object?[] args) => broker.TryGetService("FishingAutomation", out object? value) && value != null ? CompatibilityHostBroker.Invoke<T>(value, name, args) : fallback;
        private void CallIfLoaded(string name, params object?[] args) { if (broker.TryGetService("FishingAutomation", out object? value) && value != null) CompatibilityHostBroker.Invoke(value, name, args); }
        private bool TryOutString(string method, out string value) { value = string.Empty; if (!broker.TryGetService("FishingAutomation", out object? target) || target == null) return false; object?[] args = { null }; bool ok = CompatibilityHostBroker.Invoke<bool>(target, method, args); value = args[0] as string ?? string.Empty; return ok; }
        private bool TryOutBool(string method, string input, out bool value) { value = false; if (!broker.TryGetService("FishingAutomation", out object? target) || target == null) return false; object?[] args = { input, value }; bool ok = CompatibilityHostBroker.Invoke<bool>(target, method, args); value = args[1] is bool result && result; return ok; }
        private void RefFloat(string method, ref float value, string source) { if (!broker.TryGetService("FishingAutomation", out object? target) || target == null) return; object?[] args = { value, source }; CompatibilityHostBroker.Invoke(target, method, args); if (args[0] is float result) value = result; }
        private delegate void RefFloatString(ref float value, string source);
        private delegate bool StringOutBool(string inputName, out bool value);
        private static double Clamp(double value, double min, double max) => double.IsNaN(value) || double.IsInfinity(value) ? min : Math.Min(max, Math.Max(min, value));

        internal sealed class FishingAutomationLifecycleSnapshot
        {
            private readonly object value;
            internal FishingAutomationLifecycleSnapshot(object value) { this.value = value; }
            private T Read<T>(string name, T fallback = default!) => CompatibilityHostBroker.ReadProperty(value, name, fallback);
            public string Boundary => Read("Boundary", string.Empty); public int OptionOwnerCount => Read("OptionOwnerCount", 0); public int StateOwnerCount => Read("StateOwnerCount", 0); public int EnabledOwnerCount => Read("EnabledOwnerCount", 0);
            public int MiniGameHandleCount => Read("MiniGameHandleCount", 0); public int MiniGameBonusHandleCount => Read("MiniGameBonusHandleCount", 0); public int MiniGameInputHandleCount => Read("MiniGameInputHandleCount", 0); public int MiniGameCompletedHandleCount => Read("MiniGameCompletedHandleCount", 0);
            public int ReadyChargeAppliedCount => Read("ReadyChargeAppliedCount", 0); public int ReadyChargeTargetCount => Read("ReadyChargeTargetCount", 0); public int ReadyChargeReleasedCount => Read("ReadyChargeReleasedCount", 0); public bool HasCurrentReadyChargeState => Read("HasCurrentReadyChargeState", false);
            public int AnimatorSnapshotCount => Read("AnimatorSnapshotCount", 0); public int HookPhysicsSnapshotCount => Read("HookPhysicsSnapshotCount", 0); public bool HasInputOverride => Read("HasInputOverride", false); public bool PendingCast => Read("PendingCast", false); public double PendingCastAgeSeconds => Read("PendingCastAgeSeconds", 0d); public int PendingCastStallCount => Read("PendingCastStallCount", 0); public bool AutoCastBackoff => Read("AutoCastBackoff", false);
            public int LoggedPhaseCount => Read("LoggedPhaseCount", 0); public int FailureCount => Read("FailureCount", 0); public int DiagnosticCount => Read("DiagnosticCount", 0); public int AutoCastAttemptCount => Read("AutoCastAttemptCount", 0); public int AutoCastConfirmedCount => Read("AutoCastConfirmedCount", 0); public int AutomationApplicationCount => Read("AutomationApplicationCount", 0); public int ReadyChargeApplicationCount => Read("ReadyChargeApplicationCount", 0); public int ReadyChargeTargetApplicationCount => Read("ReadyChargeTargetApplicationCount", 0); public int MiniGameCompleteApplicationCount => Read("MiniGameCompleteApplicationCount", 0); public int NativeTransientHandleCount => Read("NativeTransientHandleCount", 0); public int BoundaryClearCount => Read("BoundaryClearCount", 0); public IReadOnlyList<string> OwnerPolicies => Read<IReadOnlyList<string>>("OwnerPolicies", Array.Empty<string>());
            public string FormatShortSummary() => CompatibilityHostBroker.Invoke<string>(value, "FormatShortSummary");
        }
    }
}
