using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed partial class QaScenarioController
    {
        private static readonly ConditionalWeakTable<QaScenarioController, AutoFishingSmokeFlowProgress> AutoFishingSmokeFlows = new ConditionalWeakTable<QaScenarioController, AutoFishingSmokeFlowProgress>();
        private bool autoFishingPerformanceAwaitingTitleCleanup;
        private bool autoFishingPerformanceTitleCleanupObserved;
        private bool autoFishingPerformanceTitleCleanupPassed;
        private DateTimeOffset autoFishingPerformanceReturnHomeRequestedAtUtc = DateTimeOffset.MinValue;
        private bool autoFishingPerformanceNoRodDeselected;
        private int autoFishingPerformanceOriginalSelectedIndex = int.MinValue;
        private int autoFishingPerformanceNoRodSelectedIndex = int.MinValue;
        private bool autoFishingPerformanceAwaitingMovementCancel;
        private DateTimeOffset autoFishingPerformanceMovementCancelRequestedAtUtc = DateTimeOffset.MinValue;
        private bool autoFishingPerformanceMeasurementCompleted;
        private bool autoFishingMonoGateAwaitingTitleCleanup;
        private bool autoFishingMonoGateTitleCleanupObserved;
        private bool autoFishingMonoGatePassed;
        private int autoFishingMonoGateAccessorFailures;
        private int autoFishingMonoGateAccessorBuilds;
        private int autoFishingMonoGateAccessorRebuilds;
        private int autoFishingMonoGateAccessorBuildFailures;
        private int autoFishingMonoGateAccessorInvocationFailures;
        private int autoFishingMonoGateVisibleReelPending;
        private int autoFishingMonoGateLegacyMaps;
        private DateTimeOffset autoFishingMonoGateReturnHomeRequestedAtUtc = DateTimeOffset.MinValue;
        private FishingNativeControlQaSession? autoFishingNativeControlSession;
        private DateTimeOffset autoFishingNativeControlNextCastAtUtc = DateTimeOffset.MinValue;
        private DateTimeOffset autoFishingNativeControlNextReelAtUtc = DateTimeOffset.MinValue;
        private long autoFishingNativeControlLastActionSequence = -1;
        private DateTimeOffset autoFishingNativeControlLastProgressAtUtc = DateTimeOffset.MinValue;
        private string autoFishingNativeControlLastProgressPhase = string.Empty;
        private long autoFishingNativeControlLastProgressSequence = -1;
        private bool autoFishingBatch5DisableRecoveryStarted;
        private bool autoFishingBatch5DisableRecoveryCastApplied;
        private bool autoFishingBatch5DisableRecoveryVisibleReelApplied;
        private long autoFishingBatch5DisableRecoveryVisibleReelQueuedStart;
        private long autoFishingBatch5DisableRecoveryVisibleReelConsumedStart;
        private long autoFishingBatch5DisableRecoveryVisibleReelNativeAcceptedStart;
        private DateTimeOffset autoFishingBatch5DisableRecoveryStartedAtUtc = DateTimeOffset.MinValue;
        private int autoFishingBatch5TitleCycleStage;
        private int autoFishingBatch5TitleCycleSaveLoads;
        private DateTimeOffset autoFishingBatch5TitleCycleStageStartedAtUtc = DateTimeOffset.MinValue;
        private bool autoFishingBatch5TitleCycleReenabled;
        private bool autoFishingBatch5TitleCycleDisabled;
        private AutoFishingNativeVitalsCommandAdapter? autoFishingBatch5NativeVitals;
        private bool autoFishingBatch5NativeVitalsPrepared;
        private string autoFishingBatch5NativeVitalsContext = string.Empty;
        private DateTimeOffset autoFishingBatch5NativeVitalsNextMaintenanceAtUtc = DateTimeOffset.MinValue;
        private bool autoFishingBatch5NativeVitalsL4RecoveryPrepared;
        private bool autoFishingBatch5NativeVitalsFinalObserved;

        private FixtureAttemptResult TryExerciseAutoFishingNativeControlCore()
        {
            try
            {
                if (autoFishingPerformanceAwaitingTitleCleanup)
                {
                    if (autoFishingPerformanceTitleCleanupObserved)
                        return autoFishingPerformanceTitleCleanupPassed ? FixtureAttemptResult.Succeeded : FixtureAttemptResult.Failed;
                    if (DateTimeOffset.UtcNow - autoFishingPerformanceReturnHomeRequestedAtUtc > TimeSpan.FromSeconds(60))
                        throw new TimeoutException("AutoFishing native-control performance did not observe ReturnedToTitle cleanup within 60 seconds.");
                    return FixtureAttemptResult.Pending;
                }

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new MissingMemberException("DolocAPI was not visible.");

                DateTimeOffset now = DateTimeOffset.UtcNow;
                bool isNormalState = TryGetStaticBoolProperty(dolocApi, "IsNormalState");
                bool hasLiveSession = autoFishingNativeControlSession != null && !autoFishingNativeControlSession.IsReleased;
                AutoFishingNativeControlFixtureAction entryAction = AutoFishingNativeControlFixturePolicy.Decide(
                    isNormalState,
                    hasLiveSession,
                    string.Empty,
                    castAllowed: false,
                    castDue: false,
                    actionSequenceAlreadyHandled: false);
                EnsureAutoFishingNativeControlProgressWatchdogStarted(now);
                if (entryAction == AutoFishingNativeControlFixtureAction.WaitForNormalState)
                {
                    ThrowIfAutoFishingNativeControlStalled(now, isNormalState, "entry");
                    LogAutoFishingPending("Waiting for NormalGameState before Batch 5 native-control fishing. context=" + runtime.UI.InputContext + ".");
                    return FixtureAttemptResult.Pending;
                }

                EnsureAutoFishingBatch5NativeVitalsPrepared(dolocApi);
                MaintainAutoFishingBatch5NativeVitals();

                IAutoFishingPerformanceFixture performance = GetAutoFishingPerformanceFixture()
                    ?? throw new InvalidOperationException("AutoFishing native-control ladder requires the QA performance owner.");
                if (!performance.Profile.Equals("FishLoop", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("AutoFishing native-control ladder accepts only the positive FishLoop profile.");

                FishingPrimitivesService primitives = fishingAutomationFeature?.Primitives
                    ?? throw new InvalidOperationException("Fishing primitives were unavailable during native-control fishing.");
                if (autoFishingNativeControlSession == null || autoFishingNativeControlSession.IsReleased)
                {
                    var owner = new ManifestModel
                    {
                        Name = "Batch 5 AutoFishing native control",
                        Author = "DTMAPI QA",
                        Version = DtmApiRuntime.ApiVersion,
                        UniqueID = "DTMAPI.QA.Batch5.AutoFishingNativeControl",
                        Type = "CodeMod"
                    };
                    FishingNativeControlQaAcquireResult acquired = primitives.AcquireNativeControlForQa(owner);
                    if (!acquired.Success || acquired.Session == null)
                        throw new InvalidOperationException("Native-control fishing session acquisition failed status=" + acquired.Status + "; message=" + acquired.Message);
                    autoFishingNativeControlSession = acquired.Session;
                    autoFishingNativeControlNextCastAtUtc = now;
                    autoFishingNativeControlNextReelAtUtc = now;
                    autoFishingNativeControlLastActionSequence = -1;
                    ResetAutoFishingNativeControlProgressWatchdog(now);
                    runtime.RuntimeMonitor.Log("Batch 5 AutoFishing native-control session acquired without the AutoFishing product toggle or animation lease. owner=" + acquired.Session.OwnerId + ".");
                }

                AutoFishingPerformanceFixtureUpdate update = performance.Observe(CaptureAutoFishingPerformanceSnapshot());
                if (update == AutoFishingPerformanceFixtureUpdate.MeasurementCompleted)
                {
                    ObserveAutoFishingBatch5MeasurementEndVitals();
                    autoFishingPerformanceMeasurementCompleted = true;
                    CleanupAutoFishingNativeControl("performance-complete");
                    runtime.SetHookStatus(
                        "Smoke.AutoFishingNativeControl",
                        "verified",
                        "QA-owned first-party primitive native 1x driver",
                        "productToggled=false; animationLease=false; targetFish=" + batch5GcLadder.TargetUnits.ToString(CultureInfo.InvariantCulture) + "; owner=qa; forcedGc=false");
                    BeginPrimitiveAutoFishingPerformanceTitleCleanup(GetAutoFishingToggleButtonForFixture(), dolocApi, stopProduct: false);
                    return FixtureAttemptResult.Pending;
                }

                FishingNativeControlQaSnapshot snapshot = autoFishingNativeControlSession.GetSnapshot();
                bool snapshotProgressed = ObserveAutoFishingNativeControlProgress(snapshot, now, isNormalState, "native-control");
                if (snapshotProgressed &&
                    (snapshot.Phase.Equals("PullExited", StringComparison.Ordinal) ||
                        snapshot.Phase.Equals("Interrupted", StringComparison.Ordinal)))
                {
                    autoFishingNativeControlNextCastAtUtc = now.AddMilliseconds(250);
                }
                if (snapshotProgressed && snapshot.Phase.Equals("BiteReady", StringComparison.Ordinal))
                    autoFishingNativeControlNextReelAtUtc = now;

                FishingNativeControlQaOperation result = default;
                bool actionAttempted = false;
                AutoFishingNativeControlFixtureAction action = AutoFishingNativeControlFixturePolicy.Decide(
                    isNormalState,
                    hasLiveSession: true,
                    snapshot.Phase,
                    castAllowed: true,
                    castDue: now >= autoFishingNativeControlNextCastAtUtc,
                    actionSequenceAlreadyHandled: autoFishingNativeControlLastActionSequence == snapshot.Sequence);
                if (action == AutoFishingNativeControlFixtureAction.Cast)
                {
                    result = autoFishingNativeControlSession.TryCast(snapshot.Sequence, 0d, "qa:native-control-cast");
                    actionAttempted = true;
                    if (result.Applied)
                        autoFishingNativeControlNextCastAtUtc = DateTimeOffset.MaxValue;
                    else
                        autoFishingNativeControlNextCastAtUtc = now.AddSeconds(result.Status.Equals("cast-failed", StringComparison.OrdinalIgnoreCase) ? 2.5d : 0.5d);
                }
                else if (action == AutoFishingNativeControlFixtureAction.ReelVisible && now >= autoFishingNativeControlNextReelAtUtc)
                {
                    result = autoFishingNativeControlSession.TryReelVisible(snapshot.Sequence, "qa:native-control-visible-reel");
                    actionAttempted = true;
                    autoFishingNativeControlNextReelAtUtc = result.Applied ? DateTimeOffset.MaxValue : now.AddSeconds(2);
                }

                if (actionAttempted && result.Applied)
                    autoFishingNativeControlLastActionSequence = snapshot.Sequence;
                if (actionAttempted)
                {
                    runtime.RuntimeMonitor.Log(
                        "Batch 5 AutoFishing native-control action=" + action +
                        "; phase=" + snapshot.Phase +
                        "; sequence=" + snapshot.Sequence.ToString(CultureInfo.InvariantCulture) +
                        "; normalState=" + isNormalState +
                        "; applied=" + result.Applied +
                        "; status=" + result.Status + ".");
                }
                return FixtureAttemptResult.Pending;
            }
            catch (Exception ex)
            {
                try { CleanupAutoFishingNativeControl("failure"); } catch { }
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Batch 5 AutoFishing native-control exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AutoFishingNativeControl", "failed", "QA-owned first-party primitive native 1x driver", ex.GetType().Name + ": " + ex.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private void CleanupAutoFishingNativeControl(string reason)
        {
            if (autoFishingNativeControlSession != null)
            {
                autoFishingNativeControlSession.Release("qa-native-control:" + (reason ?? string.Empty));
                autoFishingNativeControlSession = null;
            }
            autoFishingNativeControlLastActionSequence = -1;
            autoFishingNativeControlNextCastAtUtc = DateTimeOffset.MinValue;
            autoFishingNativeControlNextReelAtUtc = DateTimeOffset.MinValue;
            autoFishingNativeControlLastProgressAtUtc = DateTimeOffset.MinValue;
            autoFishingNativeControlLastProgressPhase = string.Empty;
            autoFishingNativeControlLastProgressSequence = -1;
            autoFishingBatch5DisableRecoveryVisibleReelApplied = false;
            autoFishingBatch5DisableRecoveryVisibleReelQueuedStart = 0;
            autoFishingBatch5DisableRecoveryVisibleReelConsumedStart = 0;
            autoFishingBatch5DisableRecoveryVisibleReelNativeAcceptedStart = 0;
        }

        private FixtureAttemptResult TryAdvanceAutoFishingBatch5DisableRecovery(string toggleButton, Type dolocApi)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            bool isNormalState = TryGetStaticBoolProperty(dolocApi, "IsNormalState");
            bool hasLiveSession = autoFishingNativeControlSession != null && !autoFishingNativeControlSession.IsReleased;
            AutoFishingNativeControlFixtureAction entryAction = AutoFishingNativeControlFixturePolicy.Decide(
                isNormalState,
                hasLiveSession,
                string.Empty,
                castAllowed: false,
                castDue: false,
                actionSequenceAlreadyHandled: false);
            EnsureAutoFishingNativeControlProgressWatchdogStarted(
                autoFishingBatch5DisableRecoveryStartedAtUtc == DateTimeOffset.MinValue
                    ? now
                    : autoFishingBatch5DisableRecoveryStartedAtUtc);
            if (entryAction == AutoFishingNativeControlFixtureAction.WaitForNormalState)
            {
                ThrowIfAutoFishingNativeControlStalled(now, isNormalState, "batch5-l4-entry");
                LogAutoFishingPending("Waiting for NormalGameState during Batch 5 L4 native-control recovery.");
                return FixtureAttemptResult.Pending;
            }

            EnsureAutoFishingBatch5NativeVitalsPrepared(dolocApi);
            EnsureAutoFishingBatch5L4RecoveryVitalsPrepared();
            MaintainAutoFishingBatch5NativeVitals();

            FishingPrimitivesService primitives = fishingAutomationFeature?.Primitives
                ?? throw new InvalidOperationException("Fishing primitives were unavailable during Batch 5 L4 recovery.");
            if (autoFishingNativeControlSession == null || autoFishingNativeControlSession.IsReleased)
            {
                var owner = new ManifestModel
                {
                    Name = "Batch 5 AutoFishing disable recovery",
                    Author = "DTMAPI QA",
                    Version = DtmApiRuntime.ApiVersion,
                    UniqueID = "DTMAPI.QA.Batch5.AutoFishingDisableRecovery",
                    Type = "CodeMod"
                };
                FishingNativeControlQaAcquireResult acquired = primitives.AcquireNativeControlForQa(owner);
                if (!acquired.Success || acquired.Session == null)
                    throw new InvalidOperationException("Batch 5 L4 native-control recovery acquisition failed status=" + acquired.Status + "; message=" + acquired.Message);
                autoFishingNativeControlSession = acquired.Session;
                autoFishingNativeControlNextCastAtUtc = now;
                autoFishingNativeControlNextReelAtUtc = now;
                autoFishingNativeControlLastActionSequence = -1;
                ResetAutoFishingNativeControlProgressWatchdog(now);
            }

            FishingNativeControlQaSnapshot snapshot = autoFishingNativeControlSession.GetSnapshot();
            bool snapshotProgressed = ObserveAutoFishingNativeControlProgress(snapshot, now, isNormalState, "batch5-l4-recovery");
            if (autoFishingBatch5DisableRecoveryCastApplied && snapshot.Phase.Equals("PullExited", StringComparison.Ordinal))
            {
                FishingVisibleReelTelemetry visibleReel = primitives.VisibleReelTelemetry;
                bool visibleReelVerified = AutoFishingNativeControlFixturePolicy.HasVerifiedVisibleReelCycle(
                    autoFishingBatch5DisableRecoveryVisibleReelApplied,
                    autoFishingBatch5DisableRecoveryVisibleReelQueuedStart,
                    visibleReel.Queued,
                    autoFishingBatch5DisableRecoveryVisibleReelConsumedStart,
                    visibleReel.Consumed,
                    autoFishingBatch5DisableRecoveryVisibleReelNativeAcceptedStart,
                    visibleReel.NativeAccepted);
                if (!visibleReelVerified)
                {
                    throw new InvalidOperationException(
                        "Batch 5 AutoFishing L4 reached PullExited without a verified QA visible-reel cycle. reelApplied=" + autoFishingBatch5DisableRecoveryVisibleReelApplied +
                        "; queuedDelta=" + Math.Max(0L, visibleReel.Queued - autoFishingBatch5DisableRecoveryVisibleReelQueuedStart).ToString(CultureInfo.InvariantCulture) +
                        "; consumedDelta=" + Math.Max(0L, visibleReel.Consumed - autoFishingBatch5DisableRecoveryVisibleReelConsumedStart).ToString(CultureInfo.InvariantCulture) +
                        "; nativeAcceptedDelta=" + Math.Max(0L, visibleReel.NativeAccepted - autoFishingBatch5DisableRecoveryVisibleReelNativeAcceptedStart).ToString(CultureInfo.InvariantCulture) + ".");
                }
                long queuedDelta = Math.Max(0L, visibleReel.Queued - autoFishingBatch5DisableRecoveryVisibleReelQueuedStart);
                long consumedDelta = Math.Max(0L, visibleReel.Consumed - autoFishingBatch5DisableRecoveryVisibleReelConsumedStart);
                long nativeAcceptedDelta = Math.Max(0L, visibleReel.NativeAccepted - autoFishingBatch5DisableRecoveryVisibleReelNativeAcceptedStart);
                CleanupAutoFishingNativeControl("batch5-l4-native-unit-complete");
                FishingSmokeCleanupSnapshot cleanup = CaptureFishingSmokeCleanupSnapshot();
                FishingSmokeCleanupAssertions.AssertCleared("Batch 5 AutoFishing L4 product-disabled native recovery", cleanup);
                IAutoFishingPerformanceFixture performance = GetAutoFishingPerformanceFixture()
                    ?? throw new InvalidOperationException("Batch 5 AutoFishing L4 lost the QA performance owner before its recovery receipt.");
                string source = "product-disabled -> QA native-control cast -> visible reel queued=" + queuedDelta.ToString(CultureInfo.InvariantCulture) +
                    "/consumed=" + consumedDelta.ToString(CultureInfo.InvariantCulture) +
                    "/nativeAccepted=" + nativeAcceptedDelta.ToString(CultureInfo.InvariantCulture) +
                    " -> PullExited -> release -> zero cleanup";
                performance.MarkBatch5DisableRecovery(1, source);
                runtime.SetHookStatus(
                    "Smoke.Batch5GcLadder.AutoFishingDisableRecovery",
                    "verified",
                    "first-party product disable + QA-owned native-control recovery",
                    "nativeRecoveryUnits=1; source=" + source + "; forcedGc=false; owner=qa");
                BeginPrimitiveAutoFishingPerformanceTitleCleanup(toggleButton, dolocApi, stopProduct: false);
                return FixtureAttemptResult.Pending;
            }
            if (snapshotProgressed && snapshot.Phase.Equals("Interrupted", StringComparison.Ordinal))
            {
                autoFishingBatch5DisableRecoveryCastApplied = false;
                autoFishingBatch5DisableRecoveryVisibleReelApplied = false;
                autoFishingNativeControlNextCastAtUtc = now.AddMilliseconds(250);
            }
            if (snapshotProgressed && snapshot.Phase.Equals("BiteReady", StringComparison.Ordinal))
                autoFishingNativeControlNextReelAtUtc = now;
            FishingNativeControlQaOperation operation = default;
            bool attempted = false;
            AutoFishingNativeControlFixtureAction action = AutoFishingNativeControlFixturePolicy.Decide(
                isNormalState,
                hasLiveSession: true,
                snapshot.Phase,
                castAllowed: !autoFishingBatch5DisableRecoveryCastApplied,
                castDue: now >= autoFishingNativeControlNextCastAtUtc,
                actionSequenceAlreadyHandled: autoFishingNativeControlLastActionSequence == snapshot.Sequence);
            if (action == AutoFishingNativeControlFixtureAction.Cast)
            {
                operation = autoFishingNativeControlSession.TryCast(snapshot.Sequence, 0d, "qa:batch5-l4-recovery-cast");
                attempted = true;
                if (operation.Applied)
                {
                    autoFishingBatch5DisableRecoveryCastApplied = true;
                    autoFishingBatch5DisableRecoveryVisibleReelApplied = false;
                    FishingVisibleReelTelemetry visibleReel = primitives.VisibleReelTelemetry;
                    autoFishingBatch5DisableRecoveryVisibleReelQueuedStart = visibleReel.Queued;
                    autoFishingBatch5DisableRecoveryVisibleReelConsumedStart = visibleReel.Consumed;
                    autoFishingBatch5DisableRecoveryVisibleReelNativeAcceptedStart = visibleReel.NativeAccepted;
                    autoFishingNativeControlNextCastAtUtc = DateTimeOffset.MaxValue;
                }
                else
                {
                    autoFishingNativeControlNextCastAtUtc = now.AddMilliseconds(500);
                }
            }
            else if (autoFishingBatch5DisableRecoveryCastApplied &&
                action == AutoFishingNativeControlFixtureAction.ReelVisible &&
                now >= autoFishingNativeControlNextReelAtUtc)
            {
                operation = autoFishingNativeControlSession.TryReelVisible(snapshot.Sequence, "qa:batch5-l4-recovery-visible-reel");
                attempted = true;
                if (operation.Applied)
                    autoFishingBatch5DisableRecoveryVisibleReelApplied = true;
                autoFishingNativeControlNextReelAtUtc = operation.Applied ? DateTimeOffset.MaxValue : now.AddSeconds(2);
            }
            if (attempted && operation.Applied)
                autoFishingNativeControlLastActionSequence = snapshot.Sequence;
            if (attempted)
            {
                runtime.RuntimeMonitor.Log(
                    "Batch 5 AutoFishing L4 native-control action=" + action +
                    "; phase=" + snapshot.Phase +
                    "; sequence=" + snapshot.Sequence.ToString(CultureInfo.InvariantCulture) +
                    "; normalState=" + isNormalState +
                    "; applied=" + operation.Applied +
                    "; status=" + operation.Status + ".");
            }
            return FixtureAttemptResult.Pending;
        }

        private void EnsureAutoFishingNativeControlProgressWatchdogStarted(DateTimeOffset nowUtc)
        {
            if (autoFishingNativeControlLastProgressAtUtc == DateTimeOffset.MinValue)
                autoFishingNativeControlLastProgressAtUtc = nowUtc;
        }

        private void ResetAutoFishingNativeControlProgressWatchdog(DateTimeOffset nowUtc)
        {
            autoFishingNativeControlLastProgressAtUtc = nowUtc;
            autoFishingNativeControlLastProgressPhase = string.Empty;
            autoFishingNativeControlLastProgressSequence = -1;
        }

        private bool ObserveAutoFishingNativeControlProgress(
            FishingNativeControlQaSnapshot snapshot,
            DateTimeOffset nowUtc,
            bool isNormalState,
            string context)
        {
            if (AutoFishingNativeControlFixturePolicy.HasProgress(
                autoFishingNativeControlLastProgressPhase,
                autoFishingNativeControlLastProgressSequence,
                snapshot.Phase,
                snapshot.Sequence))
            {
                autoFishingNativeControlLastProgressAtUtc = nowUtc;
                autoFishingNativeControlLastProgressPhase = snapshot.Phase;
                autoFishingNativeControlLastProgressSequence = snapshot.Sequence;
                return true;
            }

            ThrowIfAutoFishingNativeControlStalled(nowUtc, isNormalState, context);
            return false;
        }

        private void ThrowIfAutoFishingNativeControlStalled(DateTimeOffset nowUtc, bool isNormalState, string context)
        {
            if (!AutoFishingNativeControlFixturePolicy.IsStalled(
                autoFishingNativeControlLastProgressAtUtc,
                nowUtc,
                TimeSpan.FromSeconds(90)))
            {
                return;
            }

            throw new TimeoutException(
                "Batch 5 AutoFishing native-control fixture made no progress within 90 seconds. context=" + context +
                "; phase=" + (string.IsNullOrWhiteSpace(autoFishingNativeControlLastProgressPhase) ? "none" : autoFishingNativeControlLastProgressPhase) +
                "; sequence=" + autoFishingNativeControlLastProgressSequence.ToString(CultureInfo.InvariantCulture) +
                "; normalState=" + isNormalState + ".");
        }

        private bool IsBatch5AutoFishingLevel(string level) =>
            batch5GcLadder.Enabled &&
            batch5GcLadder.Domain.Equals("AutoFishing", StringComparison.OrdinalIgnoreCase) &&
            batch5GcLadder.Level.Equals(level, StringComparison.Ordinal);

        private bool IsBatch5AutoFishingFixture() =>
            batch5GcLadder.Enabled &&
            batch5GcLadder.Domain.Equals("AutoFishing", StringComparison.OrdinalIgnoreCase);

        private void EnsureAutoFishingBatch5NativeVitalsPrepared(Type dolocApi)
        {
            if (!IsBatch5AutoFishingFixture() || autoFishingBatch5NativeVitalsPrepared)
                return;

            try
            {
                autoFishingBatch5NativeVitals ??= AutoFishingNativeVitalsCommandAdapter.Create(dolocApi);
                int saveLoadOrdinal = 1;
                string loadKind = AutoFishingNativeVitalsReceipt.InitialSaveWorkloadPhase;
                if (IsBatch5AutoFishingLevel("L5"))
                {
                    saveLoadOrdinal = autoFishingBatch5TitleCycleSaveLoads;
                    if (saveLoadOrdinal == 1 && autoFishingBatch5TitleCycleStage < 2)
                        loadKind = AutoFishingNativeVitalsReceipt.InitialSaveWorkloadPhase;
                    else if (saveLoadOrdinal == 2 && autoFishingBatch5TitleCycleStage >= 2)
                        loadKind = AutoFishingNativeVitalsReceipt.PostReloadWorkloadPhase;
                    else
                        throw new InvalidOperationException("Batch 5 AutoFishing L5 reached workload preparation with invalid save-load ordinal/stage state. ordinal=" + saveLoadOrdinal.ToString(CultureInfo.InvariantCulture) + "; stage=" + autoFishingBatch5TitleCycleStage.ToString(CultureInfo.InvariantCulture) + ".");
                }
                autoFishingBatch5NativeVitalsContext =
                    "Batch5 AutoFishing " + batch5GcLadder.Level + " " + loadKind + " workload";
                AutoFishingNativeVitalsReceipt receipt = autoFishingBatch5NativeVitals.PrepareWorkload(
                    autoFishingBatch5NativeVitalsContext,
                    saveLoadOrdinal,
                    loadKind);
                RecordAutoFishingBatch5NativeVitals(receipt);
                autoFishingBatch5NativeVitalsPrepared = true;
                autoFishingBatch5NativeVitalsNextMaintenanceAtUtc = DateTimeOffset.UtcNow.AddMilliseconds(250);
                runtime.SetHookStatus(
                    "Smoke.Batch5GcLadder.AutoFishingNativeVitals",
                    "verified",
                    "official DolocAPI command registry + native percent readback",
                    "context=" + receipt.Context +
                    "; source=" + receipt.Source +
                    "; energyPercent=" + receipt.EnergyPercentAfter.ToString("0.###", CultureInfo.InvariantCulture) +
                    "; spiritPercent=" + receipt.SpiritPercentAfter.ToString("0.###", CultureInfo.InvariantCulture) +
                    "; fishingEnergyCost=" + receipt.FishingEnergyCost.ToString(CultureInfo.InvariantCulture) +
                    "; saveLoadOrdinal=" + receipt.SaveLoadOrdinal.ToString(CultureInfo.InvariantCulture) +
                    "; workloadPhase=" + receipt.WorkloadPhase +
                    "; delegates=" + receipt.ComposeEnergyDelegateIdentity + "," + receipt.ComposeSpiritDelegateIdentity + "," + receipt.GetEnergyPercentDelegateIdentity + "," + receipt.GetSpiritPercentDelegateIdentity +
                    "; directField=false; creativeMode=false; owner=qa");
            }
            catch (Exception ex)
            {
                runtime.SetHookStatus(
                    "Smoke.Batch5GcLadder.AutoFishingNativeVitals",
                    "failed",
                    "official DolocAPI command registry + native percent readback",
                    ex.GetType().Name + ": " + ex.Message + "; directField=false; creativeMode=false; owner=qa");
                throw;
            }
        }

        private void MaintainAutoFishingBatch5NativeVitals()
        {
            if (!IsBatch5AutoFishingFixture() || !autoFishingBatch5NativeVitalsPrepared || autoFishingBatch5NativeVitals == null)
                return;

            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (now < autoFishingBatch5NativeVitalsNextMaintenanceAtUtc)
                return;
            autoFishingBatch5NativeVitalsNextMaintenanceAtUtc = now.AddMilliseconds(250);

            try
            {
                if (!autoFishingBatch5NativeVitals.TryMaintainWorkload(autoFishingBatch5NativeVitalsContext, out AutoFishingNativeVitalsReceipt? receipt) || receipt == null)
                    return;

                RecordAutoFishingBatch5NativeVitals(receipt);
                runtime.RuntimeMonitor.Log(
                    "Batch 5 AutoFishing official native vitals maintenance applied. context=" + receipt.Context +
                    "; energyCommand=" + receipt.EnergyCommandInvoked +
                    "; spiritCommand=" + receipt.SpiritCommandInvoked +
                    "; energyPercentBefore=" + receipt.EnergyPercentBefore.ToString("0.###", CultureInfo.InvariantCulture) +
                    "; energyPercentAfter=" + receipt.EnergyPercentAfter.ToString("0.###", CultureInfo.InvariantCulture) +
                    "; spiritPercentBefore=" + receipt.SpiritPercentBefore.ToString("0.###", CultureInfo.InvariantCulture) +
                    "; spiritPercentAfter=" + receipt.SpiritPercentAfter.ToString("0.###", CultureInfo.InvariantCulture) + ".");
                if (receipt.NativeEnergyInsufficientObserved)
                {
                    throw new InvalidOperationException(
                        "Batch 5 AutoFishing observed native fishing energy below FishingEnergyCost during an active workload; " +
                        "the run cannot be accepted as a lifecycle/performance result.");
                }
            }
            catch (Exception ex)
            {
                runtime.SetHookStatus(
                    "Smoke.Batch5GcLadder.AutoFishingNativeVitals",
                    "failed",
                    "official DolocAPI command registry + native percent readback",
                    ex.GetType().Name + ": " + ex.Message + "; directField=false; creativeMode=false; owner=qa");
                throw;
            }
        }

        private void EnsureAutoFishingBatch5L4RecoveryVitalsPrepared()
        {
            if (!IsBatch5AutoFishingLevel("L4") || autoFishingBatch5NativeVitalsL4RecoveryPrepared)
                return;
            if (!autoFishingBatch5NativeVitalsPrepared || autoFishingBatch5NativeVitals == null)
                throw new InvalidOperationException("Batch 5 AutoFishing L4 recovery reached its vitals checkpoint before workload preparation.");

            try
            {
                string context = AutoFishingNativeVitalsReceipt.OfficialL4RecoveryCheckpointContext;
                AutoFishingNativeVitalsReceipt receipt = autoFishingBatch5NativeVitals.PrepareL4RecoveryCheckpoint(context);
                RecordAutoFishingBatch5NativeVitals(receipt);
                autoFishingBatch5NativeVitalsL4RecoveryPrepared = true;
                autoFishingBatch5NativeVitalsNextMaintenanceAtUtc = DateTimeOffset.UtcNow.AddMilliseconds(250);
                runtime.RuntimeMonitor.Log(
                    "Batch 5 AutoFishing L4 recovery official native vitals checkpoint verified. source=" + receipt.Source +
                    "; energyPercent=" + receipt.EnergyPercentAfter.ToString("0.###", CultureInfo.InvariantCulture) +
                    "; spiritPercent=" + receipt.SpiritPercentAfter.ToString("0.###", CultureInfo.InvariantCulture) + ".");
            }
            catch (Exception ex)
            {
                PublishAutoFishingBatch5NativeVitalsFailure(ex);
                throw;
            }
        }

        private void ObserveAutoFishingBatch5MeasurementEndVitals()
        {
            if (!IsBatch5AutoFishingFixture() || autoFishingBatch5NativeVitalsFinalObserved)
                return;
            if (!autoFishingBatch5NativeVitalsPrepared || autoFishingBatch5NativeVitals == null)
                throw new InvalidOperationException("Batch 5 AutoFishing measurement ended before its official native vitals preparation.");

            try
            {
                AutoFishingNativeVitalsReceipt receipt = autoFishingBatch5NativeVitals.ObserveMeasurementEnd(
                    "Batch5 AutoFishing " + batch5GcLadder.Level + " measurement end");
                RecordAutoFishingBatch5NativeVitals(receipt);
                autoFishingBatch5NativeVitalsFinalObserved = true;
                if (receipt.NativeEnergyInsufficientObserved)
                {
                    throw new InvalidOperationException(
                        "Batch 5 AutoFishing measurement ended below the native FishingEnergyCost gate; " +
                        "the performance result cannot be accepted.");
                }
                runtime.RuntimeMonitor.Log(
                    "Batch 5 AutoFishing measurement-end official native vitals readback verified. context=" + receipt.Context +
                    "; energyPercent=" + receipt.EnergyPercentAfter.ToString("0.###", CultureInfo.InvariantCulture) +
                    "; spiritPercent=" + receipt.SpiritPercentAfter.ToString("0.###", CultureInfo.InvariantCulture) +
                    "; enoughForCast=true.");
            }
            catch (Exception ex)
            {
                PublishAutoFishingBatch5NativeVitalsFailure(ex);
                throw;
            }
        }

        private void PublishAutoFishingBatch5NativeVitalsFailure(Exception ex)
        {
            runtime.SetHookStatus(
                "Smoke.Batch5GcLadder.AutoFishingNativeVitals",
                "failed",
                "official DolocAPI command registry + native percent readback",
                ex.GetType().Name + ": " + ex.Message + "; directField=false; creativeMode=false; owner=qa");
        }

        private void RecordAutoFishingBatch5NativeVitals(AutoFishingNativeVitalsReceipt receipt)
        {
            IAutoFishingPerformanceFixture performance = GetAutoFishingPerformanceFixture()
                ?? throw new InvalidOperationException("Batch 5 AutoFishing native vitals require the QA performance owner.");
            performance.RecordBatch5NativeVitals(receipt);
        }

        private void ResetAutoFishingBatch5NativeVitals()
        {
            autoFishingBatch5NativeVitals = null;
            autoFishingBatch5NativeVitalsPrepared = false;
            autoFishingBatch5NativeVitalsContext = string.Empty;
            autoFishingBatch5NativeVitalsNextMaintenanceAtUtc = DateTimeOffset.MinValue;
            autoFishingBatch5NativeVitalsL4RecoveryPrepared = false;
            autoFishingBatch5NativeVitalsFinalObserved = false;
        }

        private void NotifyAutoFishingBatch5SaveLoadedForFixture()
        {
            if (!IsBatch5AutoFishingFixture())
                return;

            ResetAutoFishingBatch5NativeVitals();
            if (!IsBatch5AutoFishingLevel("L5"))
                return;
            autoFishingBatch5TitleCycleSaveLoads++;
            if (autoFishingBatch5TitleCycleStage != 1 || autoFishingBatch5TitleCycleSaveLoads < 2)
                return;

            autoLoadOfficialPathRequested = false;
            modChangePromptConfirmed = false;
            pendingAutoLoadGameIndex = null;
            pendingAutoLoadGameDataState = null;
            autoLoadDirectFallbackAfterModChangeAttempted = false;
            AutoFishingSmokeFlows.Remove(this);
            autoExerciseAutoFishingPhaseStarted = false;
            autoExerciseAutoFishingPhaseVerified = false;
            autoFishingHotkeyInjected = false;
            autoFishingPhaseStartedAt = DateTimeOffset.MinValue;
            autoFishingBatch5TitleCycleStage = 2;
            autoFishingBatch5TitleCycleStageStartedAtUtc = DateTimeOffset.UtcNow;
            runtime.SetHookStatus(
                "Smoke.Batch5GcLadder.AutoFishingTitleCycle",
                "pending",
                "title -> configured-save reload -> product re-enable/disable",
                "title=true; saveLoads=" + autoFishingBatch5TitleCycleSaveLoads.ToString(CultureInfo.InvariantCulture) + "; reload=true; awaitingReenable=true; owner=qa; forcedGc=false");
        }

        private FixtureAttemptResult BeginAutoFishingBatch5TitleReload()
        {
            if (!autoFishingPerformanceTitleCleanupPassed)
                return FixtureAttemptResult.Failed;
            if (autoFishingBatch5TitleCycleSaveLoads != 1)
                throw new InvalidOperationException("Batch 5 AutoFishing L5 expected exactly one initial SaveLoaded receipt before requesting its title reload.");

            autoFishingPerformanceAwaitingTitleCleanup = false;
            autoFishingPerformanceTitleCleanupObserved = false;
            autoFishingPerformanceTitleCleanupPassed = false;
            autoLoadOfficialPathRequested = false;
            modChangePromptConfirmed = false;
            pendingAutoLoadGameIndex = null;
            pendingAutoLoadGameDataState = null;
            autoLoadDirectFallbackAfterModChangeAttempted = false;
            autoFishingBatch5TitleCycleStage = 1;
            autoFishingBatch5TitleCycleStageStartedAtUtc = DateTimeOffset.UtcNow;
            runtime.SetHookStatus(
                "Smoke.Batch5GcLadder.AutoFishingTitleCycle",
                "pending",
                "title -> configured-save reload -> product re-enable/disable",
                "title=true; initialCleanup=true; reloadRequested=true; saveSlot=" + batch5GcLadder.SaveSlot.ToString(CultureInfo.InvariantCulture) + "; owner=qa; forcedGc=false");
            TryAutoLoadSave(batch5GcLadder.SaveSlot);
            return FixtureAttemptResult.Pending;
        }

        private FixtureAttemptResult TryAdvanceAutoFishingBatch5TitleReload()
        {
            if (autoFishingBatch5TitleCycleStage != 1)
                return FixtureAttemptResult.Pending;
            if (DateTimeOffset.UtcNow - autoFishingBatch5TitleCycleStageStartedAtUtc > TimeSpan.FromSeconds(90))
                throw new TimeoutException("Batch 5 AutoFishing L5 did not reload save slot " + batch5GcLadder.SaveSlot.ToString(CultureInfo.InvariantCulture) + " within 90 seconds after its first title cleanup.");
            if (autoFishingBatch5TitleCycleSaveLoads >= 2)
                return FixtureAttemptResult.Pending;
            if (!autoLoadOfficialPathRequested)
            {
                TryAutoLoadSave(batch5GcLadder.SaveSlot);
                return FixtureAttemptResult.Pending;
            }
            if (!modChangePromptConfirmed)
                TryConfirmModChangePrompt();
            if (modChangePromptConfirmed)
                TryAutoLoadSaveDirectFallbackAfterModChange();
            return FixtureAttemptResult.Pending;
        }

        private void DispatchQaInputFrame(string button, bool isDownNow, bool pressedEdge, bool releasedEdge)
        {
            string normalized = DtmButton.Normalize(button);
            if (string.IsNullOrWhiteSpace(normalized))
                return;

            // The optional QA host owns synthetic sampling. Core keeps only the
            // neutral frame recorder used by the real Unity input sampler.
            runtime.Input.ClearFrame();
            runtime.RecordInputFrame(new[] { new InputButtonSample(normalized, isDownNow, pressedEdge, releasedEdge) });
        }

        private void DispatchQaInputTap(string button)
        {
            DispatchQaInputFrame(button, isDownNow: false, pressedEdge: true, releasedEdge: true);
        }

        private FixtureAttemptResult TryExerciseAutoFishingMovementCancelPrimitiveCore()
        {
            const string reason = "Retired historical Batch 5 movement fixture: session disappearance, a blind delay, and one fixed A input cannot prove Ready/Cast/Wait/BiteReady/MiniGame or an actual rebound binding. Use the formal Batch 6 behavior matrix.";
            runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-fishing movement-cancel exercise is retired.", reason);
            runtime.SetHookStatus("Smoke.AutoFishingMovementCancel", "failed", "retired historical fixture", reason);
            autoFishingHotkeyInjected = false;
            runtime.Input.ClearFrame();
            return FixtureAttemptResult.Failed;
        }

        private FixtureAttemptResult TryExerciseAutoFishingPrimitiveCore()
        {
            try
            {
                if (autoFishingMonoGateAwaitingTitleCleanup)
                {
                    if (autoFishingMonoGateTitleCleanupObserved)
                        return autoFishingMonoGatePassed ? FixtureAttemptResult.Succeeded : FixtureAttemptResult.Failed;
                    if (DateTimeOffset.UtcNow - autoFishingMonoGateReturnHomeRequestedAtUtc > TimeSpan.FromSeconds(60))
                        throw new TimeoutException("AutoFishing Mono gate did not observe ReturnedToTitle cleanup within 60 seconds.");
                    LogAutoFishingPending("Waiting for ReturnedToTitle cleanup after the one-fish Mono gate.");
                    return FixtureAttemptResult.Pending;
                }
                if (autoFishingPerformanceAwaitingTitleCleanup)
                {
                    if (autoFishingPerformanceTitleCleanupObserved)
                    {
                        if (IsBatch5AutoFishingLevel("L5") && autoFishingBatch5TitleCycleStage == 0)
                            return BeginAutoFishingBatch5TitleReload();
                        if (IsBatch5AutoFishingLevel("L5") && autoFishingBatch5TitleCycleStage >= 3)
                        {
                            if (!autoFishingPerformanceTitleCleanupPassed)
                                return FixtureAttemptResult.Failed;
                            autoFishingBatch5TitleCycleStage = 4;
                            runtime.SetHookStatus(
                                "Smoke.Batch5GcLadder.AutoFishingTitleCycle",
                                "verified",
                                "title -> configured-save reload -> product re-enable/disable -> final title cleanup",
                                "title=true; reload=true; saveLoads=" + autoFishingBatch5TitleCycleSaveLoads.ToString(CultureInfo.InvariantCulture) + "; reenabled=" + autoFishingBatch5TitleCycleReenabled + "; disabled=" + autoFishingBatch5TitleCycleDisabled + "; finalCleanup=true; owner=qa; forcedGc=false");
                            return FixtureAttemptResult.Succeeded;
                        }
                        return autoFishingPerformanceTitleCleanupPassed ? FixtureAttemptResult.Succeeded : FixtureAttemptResult.Failed;
                    }
                    if (DateTimeOffset.UtcNow - autoFishingPerformanceReturnHomeRequestedAtUtc > TimeSpan.FromSeconds(60))
                        throw new TimeoutException("AutoFishing performance probe did not observe ReturnedToTitle cleanup within 60 seconds.");
                    LogAutoFishingPending("Waiting for ReturnedToTitle cleanup after AutoFishing performance measurement.");
                    return FixtureAttemptResult.Pending;
                }
                if (IsBatch5AutoFishingLevel("L5") && autoFishingBatch5TitleCycleStage == 1)
                    return TryAdvanceAutoFishingBatch5TitleReload();
                string scenario = GetAutoFishingScenarioForFixture();
                string toggleButton = GetAutoFishingToggleButtonForFixture();
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new MissingMemberException("DolocAPI was not visible.");

                if (IsBatch5AutoFishingLevel("L4") && autoFishingBatch5DisableRecoveryStarted)
                    return TryAdvanceAutoFishingBatch5DisableRecovery(toggleButton, dolocApi);

                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    LogAutoFishingPending("Waiting for NormalGameState before auto-fishing smoke. context=" + runtime.UI.InputContext + ".");
                    return FixtureAttemptResult.Pending;
                }

                EnsureAutoFishingBatch5NativeVitalsPrepared(dolocApi);
                MaintainAutoFishingBatch5NativeVitals();

                IAutoFishingPerformanceFixture? performanceFixture = IsBatch5AutoFishingLevel("L5") && autoFishingBatch5TitleCycleStage == 2
                    ? null
                    : GetAutoFishingPerformanceFixture();
                string performanceProfile = GetAutoFishingPerformanceProfile(performanceFixture);
                if (performanceFixture != null && performanceProfile.Equals("InactiveNoConsumer", StringComparison.OrdinalIgnoreCase))
                    return TryExerciseAutoFishingZeroPerformanceProfileForFixture(performanceFixture, performanceProfile, null, toggleButton, dolocApi);
                if (performanceFixture != null && performanceProfile.Equals("EnabledNoRod", StringComparison.OrdinalIgnoreCase) && !autoFishingPerformanceNoRodDeselected)
                {
                    PrepareEnabledNoRodSelection(dolocApi);
                    autoFishingPerformanceNoRodDeselected = true;
                    runtime.RuntimeMonitor.Log("AutoFishing performance EnabledNoRod deselected the current item through DolocAPI.QuickDeselectCurrentItem and temporarily selected non-rod quick slot=" + autoFishingPerformanceNoRodSelectedIndex + " before typed enable.");
                    return FixtureAttemptResult.Pending;
                }

                if (!autoFishingHotkeyInjected)
                {
                    FishingRuntimeSession? alreadyActivePrimitive = fishingAutomationFeature?.Primitives.TryGetActiveSession();
                    if (alreadyActivePrimitive != null)
                    {
                        autoFishingHotkeyInjected = true;
                        runtime.RuntimeMonitor.Log("Smoke automation observed first-party AutoFishing already enabled before synthetic input. owner=" + alreadyActivePrimitive.OwnerId + ".");
                        runtime.SetHookStatus("Smoke.AutoFishingHotkey", "verified", "Unity input polling -> AutoFishing KeybindPressed", "First-party primitive session already active; owner=" + alreadyActivePrimitive.OwnerId + ".");
                    }
                    else if (runtime.UI.BlocksGameplayHotkeys)
                    {
                        LogAutoFishingPending("Waiting for gameplay hotkeys before synthetic " + toggleButton + " toggle. context=" + runtime.UI.InputContext + ", menuOpen=" + runtime.UI.IsOpen + ".");
                        return FixtureAttemptResult.Pending;
                    }
                    else
                    {
                        DispatchQaInputTap(toggleButton);
                        runtime.Update();
                        autoFishingHotkeyInjected = true;
                        runtime.RuntimeMonitor.Log("Smoke automation dispatched AutoFishing toggle key " + toggleButton + " through DTMAPI's typed keybind frame.");
                        runtime.SetHookStatus("Smoke.AutoFishingHotkey", "verified", "QA-owned input sample + KeybindPressed", "Dispatched " + toggleButton + " through the same Gameplay-scoped typed keybind used by the product.");
                    }
                }

                FishingRuntimeSession? firstPartySession = fishingAutomationFeature?.Primitives.TryGetActiveSession();
                if (firstPartySession != null)
                {
                    if (IsBatch5AutoFishingLevel("L5") && autoFishingBatch5TitleCycleStage == 2)
                        autoFishingBatch5TitleCycleReenabled = true;
                    if (performanceFixture != null && performanceProfile.Equals("EnabledNoRod", StringComparison.OrdinalIgnoreCase))
                        return TryExerciseAutoFishingZeroPerformanceProfileForFixture(performanceFixture, performanceProfile, firstPartySession, toggleButton, dolocApi);
                    return TryExerciseFirstPartyAutoFishingPhaseForFixture(firstPartySession, scenario, toggleButton, dolocApi, performanceFixture);
                }
                if (performanceFixture != null && performanceProfile.Equals("EnabledNoRod", StringComparison.OrdinalIgnoreCase) && autoFishingPerformanceAwaitingMovementCancel)
                    return TryExerciseAutoFishingZeroPerformanceProfileForFixture(performanceFixture, performanceProfile, null, toggleButton, dolocApi);
                throw new InvalidOperationException("The first-party primitive session was not activated after the " + toggleButton + " typed keybind input. Is the official AutoFishing package enabled?");
            }
            catch (Exception ex)
            {
                Exception failure = ex;
                try
                {
                    RestoreEnabledNoRodSelectionIfNeeded("phase-failure");
                }
                catch (Exception restoreEx)
                {
                    failure = new AggregateException("AutoFishing phase failed and EnabledNoRod selection restoration also failed.", ex, restoreEx);
                }
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-fishing phase exercise failed.", failure.ToString());
                runtime.SetHookStatus("Smoke.AutoFishingPhase", "failed", "native fishing loop", failure.GetType().Name + ": " + failure.Message);
                return FixtureAttemptResult.Failed;
            }
        }

        private string GetAutoFishingScenarioForFixture()
        {
            string scenario = g6FixtureState?.AutoFishingScenario ?? string.Empty;
            if (string.IsNullOrWhiteSpace(scenario))
                return g6FixtureState?.AutoExerciseAutoFishingMiniGameComplete == true ? "CombinedInstantComplete" : "DefaultLoop";
            return scenario.Trim();
        }

        private IAutoFishingPerformanceFixture? GetAutoFishingPerformanceFixture()
        {
            IAutoFishingPerformanceFixture? fixture = performanceFixture;
            bool qaMeasurementEnabled = fixture?.Enabled == true;
            return qaMeasurementEnabled ? fixture : null;
        }

        private static string GetAutoFishingPerformanceProfile(IAutoFishingPerformanceFixture? fixture)
        {
            string profile = fixture?.Profile ?? string.Empty;
            return string.IsNullOrWhiteSpace(profile) ? "FishLoop" : profile;
        }

        private FixtureAttemptResult TryExerciseAutoFishingZeroPerformanceProfileForFixture(IAutoFishingPerformanceFixture performanceFixture, string profile, FishingRuntimeSession? session, string toggleButton, Type dolocApi)
        {
            FishingPrimitivesService primitives = fishingAutomationFeature?.Primitives
                ?? throw new InvalidOperationException("Fishing primitives were unavailable during the zero-fish performance profile.");

            if (autoFishingPerformanceAwaitingMovementCancel)
            {
                if (primitives.HasActiveSession)
                {
                    if (DateTimeOffset.UtcNow - autoFishingPerformanceMovementCancelRequestedAtUtc > TimeSpan.FromSeconds(30))
                        throw new TimeoutException("EnabledNoRod did not observe current native movement cancellation within 30 seconds of the external movement gate.");
                    return FixtureAttemptResult.Pending;
                }
                autoFishingPerformanceAwaitingMovementCancel = false;
                if (!primitives.LastReleaseReason.StartsWith("manual-move inputMultiplier=", StringComparison.Ordinal) &&
                    !primitives.LastReleaseReason.StartsWith("native-move VelocityX=", StringComparison.Ordinal))
                    throw new InvalidOperationException("EnabledNoRod session ended without current native movement cancellation. releaseReason=" + primitives.LastReleaseReason + ".");
                performanceFixture.MarkMovementCancellation("native MoveModifier.inputMultiplier+VelocityX");
                RestoreEnabledNoRodSelection(dolocApi, "native-movement-cancel");
                runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingMovementCancel OK owner=Yuuka.DTMAPI.AutoFishing, source=native MoveModifier.inputMultiplier+VelocityX.");
                BeginPrimitiveAutoFishingPerformanceTitleCleanup(toggleButton, dolocApi, stopProduct: false);
                return FixtureAttemptResult.Pending;
            }

            if (profile.Equals("InactiveNoConsumer", StringComparison.OrdinalIgnoreCase) &&
                (session != null || primitives.HasActiveSession || fishingAutomationFeature?.PrimitiveHookRuntime != null))
            {
                throw new InvalidOperationException("InactiveNoConsumer created a primitive fishing session or Hook runtime.");
            }
            if (profile.Equals("EnabledNoRod", StringComparison.OrdinalIgnoreCase))
            {
                if (session == null)
                    throw new InvalidOperationException("EnabledNoRod lost its primitive session before native movement cancellation.");
                FishingRuntimeSessionObservation snapshot = session.CaptureObservation();
                if (snapshot.HasSelectedRod)
                    throw new InvalidOperationException("EnabledNoRod still reports a selected fishing rod after QuickDeselectCurrentItem.");
            }

            AutoFishingPerformanceFixtureUpdate update = performanceFixture.Observe(CaptureAutoFishingPerformanceSnapshot());
            if (update != AutoFishingPerformanceFixtureUpdate.MeasurementCompleted || autoFishingPerformanceMeasurementCompleted)
                return FixtureAttemptResult.Pending;

            autoFishingPerformanceMeasurementCompleted = true;

            if (profile.Equals("EnabledNoRod", StringComparison.OrdinalIgnoreCase))
            {
                autoFishingPerformanceAwaitingMovementCancel = true;
                autoFishingPerformanceMovementCancelRequestedAtUtc = DateTimeOffset.UtcNow;
                runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingMovementCancel awaiting native input owner=" + (session?.OwnerId ?? string.Empty) + ", profile=EnabledNoRod, source=Agent.Status.MoveModifier.inputMultiplier+VelocityX.");
                runtime.SetHookStatus("Smoke.AutoFishingMovementCancel", "pending", "Agent.Status.MoveModifier+VelocityX", "EnabledNoRod measurement complete; awaiting external physical A input.");
                return FixtureAttemptResult.Pending;
            }

            BeginPrimitiveAutoFishingPerformanceTitleCleanup(toggleButton, dolocApi, stopProduct: false);
            return FixtureAttemptResult.Pending;
        }

        private void PrepareEnabledNoRodSelection(Type dolocApi)
        {
            MethodInfo? deselect = FindMethod(dolocApi, "QuickDeselectCurrentItem", 0);
            if (deselect == null)
                throw new MissingMethodException("DolocAPI.QuickDeselectCurrentItem() was not found for EnabledNoRod.");

            object? uiSystem = ReadStaticMember(dolocApi, "uiSystem");
            object? quickInventory = uiSystem == null ? null : ReadMember(uiSystem, "inventoryQuick");
            if (quickInventory == null)
                throw new MissingMemberException("DolocAPI.uiSystem.inventoryQuick was not available for EnabledNoRod.");

            Delegate? itemGetter = ReadMember(quickInventory, "itemGetter") as Delegate;
            int capacity = ReadIntMember(quickInventory, "totalCapacity", 0);
            int originalIndex = ReadIntMember(quickInventory, "selectedIndex", int.MinValue);
            if (itemGetter == null || capacity <= 0 || originalIndex == int.MinValue)
                throw new InvalidOperationException("EnabledNoRod could not inspect the native quick inventory selection.");

            int noRodIndex = int.MinValue;
            for (int index = 0; index < capacity; index++)
            {
                object? item = itemGetter.DynamicInvoke(index);
                if (item == null || !IsTypeOrBase(item.GetType(), "DolocTown.ItemFishingRod"))
                {
                    noRodIndex = index;
                    break;
                }
            }
            if (noRodIndex == int.MinValue)
                throw new InvalidOperationException("EnabledNoRod did not find an empty or non-fishing-rod quick inventory slot.");

            // Arm the restoration receipt before the first native mutation. Any
            // subsequent failure is retried by the phase, title, and shutdown paths.
            autoFishingPerformanceOriginalSelectedIndex = originalIndex;
            autoFishingPerformanceNoRodSelectedIndex = noRodIndex;
            deselect.Invoke(null, null);
            if (!WriteIntMember(quickInventory, "selectedIndex", noRodIndex))
                throw new InvalidOperationException("EnabledNoRod could not temporarily change the native quick inventory selection.");

            object? selected = ReadStaticMember(dolocApi, "SelectedItem");
            if (selected != null && IsTypeOrBase(selected.GetType(), "DolocTown.ItemFishingRod"))
                throw new InvalidOperationException("EnabledNoRod native quick inventory still selected a fishing rod after the temporary slot change.");

        }

        private void RestoreEnabledNoRodSelection(Type dolocApi, string reason)
        {
            if (autoFishingPerformanceOriginalSelectedIndex == int.MinValue)
                return;
            int originalIndex = autoFishingPerformanceOriginalSelectedIndex;
            MethodInfo? deselect = FindMethod(dolocApi, "QuickDeselectCurrentItem", 0);
            MethodInfo? select = FindMethod(dolocApi, "QuickSelectCurrentItem", 0);
            if (deselect == null || select == null)
                throw new MissingMethodException("DolocAPI quick select lifecycle methods were not found after EnabledNoRod.");

            object? uiSystem = ReadStaticMember(dolocApi, "uiSystem");
            object? quickInventory = uiSystem == null ? null : ReadMember(uiSystem, "inventoryQuick");
            if (quickInventory == null)
                throw new MissingMemberException("DolocAPI.uiSystem.inventoryQuick was not available while restoring EnabledNoRod.");

            deselect.Invoke(null, null);
            if (!WriteIntMember(quickInventory, "selectedIndex", originalIndex))
                throw new InvalidOperationException("EnabledNoRod could not restore the native quick inventory selection.");
            select.Invoke(null, null);
            int restoredIndex = ReadIntMember(quickInventory, "selectedIndex", int.MinValue);
            if (restoredIndex != originalIndex)
                throw new InvalidOperationException("EnabledNoRod native quick inventory restoration did not retain the original selectedIndex. expected=" + originalIndex + "; actual=" + restoredIndex + ".");

            autoFishingPerformanceOriginalSelectedIndex = int.MinValue;
            autoFishingPerformanceNoRodSelectedIndex = int.MinValue;
            runtime.RuntimeMonitor.Log("EnabledNoRod restored native quick inventory selectedIndex=" + originalIndex + "; reason=" + (reason ?? string.Empty) + ".");
        }

        private void RestoreEnabledNoRodSelectionIfNeeded(string reason)
        {
            if (autoFishingPerformanceOriginalSelectedIndex == int.MinValue)
                return;
            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp")
                ?? throw new MissingMemberException("DolocAPI was unavailable while restoring EnabledNoRod selection at " + (reason ?? string.Empty) + ".");
            RestoreEnabledNoRodSelection(dolocApi, reason);
        }

        internal bool EnabledNoRodSelectionRestorePendingForTests => autoFishingPerformanceOriginalSelectedIndex != int.MinValue;

        internal void PrepareEnabledNoRodSelectionForTests(Type dolocApi) => PrepareEnabledNoRodSelection(dolocApi);

        internal void RestoreEnabledNoRodSelectionForTests(Type dolocApi, string reason) => RestoreEnabledNoRodSelection(dolocApi, reason);

        private FishingPerformanceTelemetrySnapshot CaptureFishingPerformanceTelemetry()
        {
            FishingPrimitivesService? primitives = fishingAutomationFeature?.Primitives;
            FishingPrimitiveHookRuntime? hookRuntime = fishingAutomationFeature?.PrimitiveHookRuntime;
            FishingNativeStateCache? native = primitives?.NativeStateCache;
            FishingVisibleReelTelemetry visibleReel = primitives?.VisibleReelTelemetry ?? default;
            int nativeReferences = 0;
            if (native?.Agent != null) nativeReferences++;
            if (native?.SelectedRod != null) nativeReferences++;
            if (native?.WaitState != null) nativeReferences++;
            if (native?.MiniGameHandle != null) nativeReferences++;
            return new FishingPerformanceTelemetrySnapshot(
                (primitives?.NativeAccessorBuildCount ?? 0) + (hookRuntime?.ReadyAccessorBuildCount ?? 0),
                (primitives?.NativeAccessorRebuildCount ?? 0) + (hookRuntime?.ReadyAccessorRebuildCount ?? 0),
                (primitives?.NativeAccessorBuildFailureCount ?? 0) + (hookRuntime?.ReadyAccessorBuildFailureCount ?? 0),
                (primitives?.NativeAccessorInvocationFailureCount ?? 0) + (hookRuntime?.ReadyAccessorInvocationFailureCount ?? 0),
                primitives?.Diagnostics.CastAppliedCount ?? 0,
                primitives?.Diagnostics.PullExitedCount ?? 0,
                fishingAutomationFeature?.ConfiguredOwnerCount ?? 0,
                fishingAutomationFeature?.ConfiguredStateCount ?? 0,
                primitives?.ActiveSessionCount ?? 0,
                primitives?.ActiveInputLeaseCount ?? 0,
                primitives?.ActiveAnimationLeaseCount ?? 0,
                hookRuntime == null ? 0 : 1,
                fishingAutomationFeature?.HookRuntimeTransientCount ?? 0,
                nativeReferences,
                native?.Current.HasSelectedRod == true,
                native?.Current.NativeMovementAvailable == true,
                visibleReel.Queued,
                visibleReel.Consumed,
                visibleReel.NativeAccepted,
                visibleReel.Retries,
                visibleReel.Timeouts,
                primitives?.Diagnostics.NativeEnergyGateUnavailableRejectionCount ?? 0,
                primitives?.Diagnostics.NativeInsufficientEnergyRejectionCount ?? 0);
        }

        private AutoFishingPerformanceFixtureSnapshot CaptureAutoFishingPerformanceSnapshot()
        {
            FishingPerformanceTelemetrySnapshot value = CaptureFishingPerformanceTelemetry();
            return new AutoFishingPerformanceFixtureSnapshot
            {
                ObservedAtUtc = DateTimeOffset.UtcNow,
                PullExited = value.Fish,
                AccessorBuilds = value.AccessorBuilds,
                AccessorRebuilds = value.AccessorRebuilds,
                AccessorBuildFailures = value.AccessorBuildFailures,
                AccessorInvocationFailures = value.AccessorInvocationFailures,
                Casts = value.Casts,
                Fish = value.Fish,
                LegacyOptions = value.LegacyOptions,
                LegacyStates = value.LegacyStates,
                Sessions = value.Sessions,
                InputLeases = value.InputLeases,
                AnimationLeases = value.AnimationLeases,
                HookRuntimes = value.HookRuntimes,
                NativeTransient = value.NativeTransient,
                NativeReferences = value.NativeReferences,
                SelectedRod = value.SelectedRod,
                NativeMovementAvailable = value.NativeMovementAvailable,
                SchedulerPending = fishingAutomationFeature?.Primitives.SchedulerPending == true,
                VisibleReelQueued = value.VisibleReelQueued,
                VisibleReelConsumed = value.VisibleReelConsumed,
                VisibleReelNativeAccepted = value.VisibleReelNativeAccepted,
                VisibleReelRetries = value.VisibleReelRetries,
                VisibleReelTimeouts = value.VisibleReelTimeouts,
                NativeTryCastEnergyGateUnavailable = value.NativeTryCastEnergyGateUnavailable,
                NativeTryCastInsufficientEnergy = value.NativeTryCastInsufficientEnergy
            };
        }

        private void BeginPrimitiveAutoFishingPerformanceTitleCleanup(string toggleButton, Type dolocApi, bool stopProduct)
        {
            if (autoFishingPerformanceAwaitingTitleCleanup)
                return;
            FishingPrimitivesService primitives = fishingAutomationFeature?.Primitives
                ?? throw new InvalidOperationException("Fishing primitives were unavailable during performance cleanup.");
            if (stopProduct && primitives.TryGetActiveSession() is FishingRuntimeSession active)
                StopPrimitiveAutoFishingForFixture(toggleButton, active.OwnerId, "performance-complete");
            MethodInfo? returnHome = FindMethod(dolocApi, "ReturnHome", 1);
            if (returnHome == null)
                throw new MissingMethodException("DolocAPI.ReturnHome(bool) was not found for AutoFishing performance cleanup.");
            autoFishingPerformanceAwaitingTitleCleanup = true;
            autoFishingPerformanceReturnHomeRequestedAtUtc = DateTimeOffset.UtcNow;
            runtime.RuntimeMonitor.Log("AutoFishing QA performance measurement complete; the production fixture seam is returning to title for cleanup verification.");
            returnHome.Invoke(null, new object[] { false });
        }

        private readonly struct FishingPerformanceTelemetrySnapshot
        {
            internal FishingPerformanceTelemetrySnapshot(int accessorBuilds, int accessorRebuilds, int accessorBuildFailures, int accessorInvocationFailures, long casts, long fish, int legacyOptions, int legacyStates, int sessions, int inputLeases, int animationLeases, int hookRuntimes, int nativeTransient, int nativeReferences, bool selectedRod, bool nativeMovementAvailable, long visibleReelQueued, long visibleReelConsumed, long visibleReelNativeAccepted, long visibleReelRetries, long visibleReelTimeouts, long nativeTryCastEnergyGateUnavailable, long nativeTryCastInsufficientEnergy)
            {
                AccessorBuilds = accessorBuilds;
                AccessorRebuilds = accessorRebuilds;
                AccessorBuildFailures = accessorBuildFailures;
                AccessorInvocationFailures = accessorInvocationFailures;
                Casts = casts;
                Fish = fish;
                LegacyOptions = legacyOptions;
                LegacyStates = legacyStates;
                Sessions = sessions;
                InputLeases = inputLeases;
                AnimationLeases = animationLeases;
                HookRuntimes = hookRuntimes;
                NativeTransient = nativeTransient;
                NativeReferences = nativeReferences;
                SelectedRod = selectedRod;
                NativeMovementAvailable = nativeMovementAvailable;
                VisibleReelQueued = visibleReelQueued;
                VisibleReelConsumed = visibleReelConsumed;
                VisibleReelNativeAccepted = visibleReelNativeAccepted;
                VisibleReelRetries = visibleReelRetries;
                VisibleReelTimeouts = visibleReelTimeouts;
                NativeTryCastEnergyGateUnavailable = nativeTryCastEnergyGateUnavailable;
                NativeTryCastInsufficientEnergy = nativeTryCastInsufficientEnergy;
            }
            internal int AccessorBuilds { get; }
            internal int AccessorRebuilds { get; }
            internal int AccessorBuildFailures { get; }
            internal int AccessorInvocationFailures { get; }
            internal long Casts { get; }
            internal long Fish { get; }
            internal int LegacyOptions { get; }
            internal int LegacyStates { get; }
            internal int Sessions { get; }
            internal int InputLeases { get; }
            internal int AnimationLeases { get; }
            internal int HookRuntimes { get; }
            internal int NativeTransient { get; }
            internal int NativeReferences { get; }
            internal bool SelectedRod { get; }
            internal bool NativeMovementAvailable { get; }
            internal long VisibleReelQueued { get; }
            internal long VisibleReelConsumed { get; }
            internal long VisibleReelNativeAccepted { get; }
            internal long VisibleReelRetries { get; }
            internal long VisibleReelTimeouts { get; }
            internal long NativeTryCastEnergyGateUnavailable { get; }
            internal long NativeTryCastInsufficientEnergy { get; }
        }

        private FixtureAttemptResult TryExerciseFirstPartyAutoFishingPhaseForFixture(FishingRuntimeSession session, string scenario, string toggleButton, Type dolocApi, IAutoFishingPerformanceFixture? performanceFixture)
        {
            FishingPrimitivesService primitives = fishingAutomationFeature?.Primitives
                ?? throw new InvalidOperationException("Fishing primitives were unavailable during first-party smoke.");
            FishingAutomationDiagnostics diagnostics = primitives.Diagnostics;
            FishingPrimitiveHookRuntime hookRuntime = fishingAutomationFeature?.PrimitiveHookRuntime
                ?? throw new InvalidOperationException("FishingPrimitiveHookRuntime was not active for the first-party session.");
            AutoFishingSmokeFlowProgress progress = AutoFishingSmokeFlows.GetValue(this, _ => new AutoFishingSmokeFlowProgress());
            if (!autoExerciseAutoFishingPhaseStarted)
            {
                autoExerciseAutoFishingPhaseStarted = true;
                autoFishingPhaseStartedAt = DateTimeOffset.Now;
                progress.Reset(scenario, diagnostics.InstantBiteCommittedCount);
                progress.SetOwner(session.OwnerId);
                runtime.RuntimeMonitor.Log("Smoke automation starting first-party primitive AutoFishing loop. owner=" + session.OwnerId + ", scenario=" + scenario + ".");
            }

            FishingRuntimeSessionObservation snapshot = session.CaptureObservation();
            progress.AutoCastObserved |= diagnostics.CastAppliedCount > 0;
            progress.WaitObserved |= diagnostics.WaitPlayableCount > 0;
            progress.BiteReadyObserved |= diagnostics.BiteReadyCount > 0;
            progress.InstantBiteObserved |= diagnostics.InstantBiteCommittedCount > progress.InstantBiteCommittedBaseline;
            progress.BattleOrPullObserved |= diagnostics.MiniGameStartedCount > 0 || diagnostics.PullEnteredCount > 0;
            progress.PullExitObserved |= diagnostics.PullExitedCount > 0;
            progress.NextAutoCastObserved |= diagnostics.CastAppliedCount > 1;
            progress.SkipObserved |= diagnostics.NativeSkipReelCount > 0;
            progress.MiniGameCompleteObserved |= diagnostics.MiniGameStartedCount > 0 && diagnostics.MiniGameStoppedCount > 0 && diagnostics.NativeVisibleReelCount > 0;
            progress.AnimationSpeedObserved |= hookRuntime.AnimationApplicationCount > 0;
            progress.ReadyChargeObserved |= hookRuntime.ReadyChargeApplicationCount > 0;
            progress.LastPhase = snapshot.Phase;
            progress.LastSummary = diagnostics.FormatSummary() + ", animation={" + hookRuntime.LastAnimationSummary + "}";

            bool performanceFishLoop = performanceFixture != null &&
                performanceFixture.Profile.Equals("FishLoop", StringComparison.OrdinalIgnoreCase);
            if (performanceFixture != null && !performanceFishLoop)
                throw new InvalidOperationException("Only the FishLoop QA performance profile may enter the positive fishing loop.");
            if (performanceFishLoop &&
                performanceFixture!.Observe(CaptureAutoFishingPerformanceSnapshot()) == AutoFishingPerformanceFixtureUpdate.MeasurementCompleted)
            {
                ObserveAutoFishingBatch5MeasurementEndVitals();
                autoFishingPerformanceMeasurementCompleted = true;
            }

            if (ScenarioRequestsInstantBite(scenario) && progress.InstantBiteObserved && !autoExerciseAutoFishingPhaseVerified)
            {
                autoExerciseAutoFishingPhaseVerified = true;
                runtime.SetHookStatus("Smoke.AutoFishingInstantBite", "verified", "FishingPrimitiveHookRuntime Wait routing", progress.LastSummary);
            }
            if (progress.AnimationSpeedObserved && !progress.AnimationSpeedPublished)
            {
                progress.AnimationSpeedPublished = true;
                runtime.SetHookStatus("Smoke.AutoFishingAnimationSpeed", "verified", "FishingPrimitiveHookRuntime animation lease", hookRuntime.LastAnimationSummary);
            }
            if (progress.ReadyChargeObserved && !progress.ReadyChargePublished &&
                primitives.TryGetReadyPolicy(out string chargeOwner, out double chargeTarget, out _) && chargeTarget > 0d)
            {
                progress.ReadyChargePublished = true;
                runtime.SetHookStatus(
                    "Smoke.AutoFishingCastCharge",
                    "verified",
                    "FishingPrimitiveHookRuntime Ready charge",
                    "owner=" + chargeOwner + ", behavior=ReadyChargeTarget, source=FishingAnimationController typed Ready timer, target=" + chargeTarget.ToString("0.###", CultureInfo.InvariantCulture) + ".");
            }

            bool monoGate = g6FixtureState?.AutoFishingMonoGate == true;
            bool instantBiteEvidence = ScenarioRequestsInstantBite(scenario)
                ? progress.InstantBiteObserved
                : diagnostics.InstantBiteCommittedCount == progress.InstantBiteCommittedBaseline;
            bool complete = progress.AutoCastObserved && progress.WaitObserved && progress.BiteReadyObserved && instantBiteEvidence &&
                progress.BattleOrPullObserved && progress.PullExitObserved &&
                (monoGate
                    ? (ScenarioRequestsSkip(scenario)
                        ? progress.SkipObserved && diagnostics.NativeVisibleReelCount == 0
                        : progress.MiniGameCompleteObserved && diagnostics.NativeSkipReelCount == 0)
                    : progress.NextAutoCastObserved);
            if (complete)
            {
                string summary = BuildAutoFishingFlowSummary(progress, progress.LastSummary);
                if (!progress.CompletionPublished)
                {
                    progress.CompletionPublished = true;
                    runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingLoop OK " + summary);
                    runtime.SetHookStatus("Smoke.AutoFishingPhase", "verified", "first-party primitive native fishing loop", summary);
                    if (progress.MiniGameCompleteObserved)
                        runtime.SetHookStatus("Smoke.AutoFishingMiniGameComplete", "verified", "FishingPrimitiveHookRuntime visible minigame", summary);
                    if (progress.SkipObserved)
                    {
                        if (primitives.ActiveInputLeaseCount != 0)
                            throw new InvalidOperationException("SkipMiniGame left a synthetic-input lease active. " + primitives.FormatDiagnosticsSummary());
                        runtime.SetHookStatus("Smoke.AutoFishingMiniGameSkip", "verified", "FishingPrimitiveHookRuntime native skip result", summary);
                    }
                }
                if (performanceFishLoop)
                {
                    if (!autoFishingPerformanceMeasurementCompleted)
                    {
                        LogAutoFishingPending("Positive FishLoop action passed its ordinary loop gate and remains active until the QA-owned target measurement completes. " + summary);
                        return FixtureAttemptResult.Pending;
                    }
                    if (IsBatch5AutoFishingLevel("L4") && !autoFishingBatch5DisableRecoveryStarted)
                    {
                        StopPrimitiveAutoFishingForFixture(toggleButton, session.OwnerId, "batch5-l4-disable-before-native-recovery");
                        autoFishingBatch5DisableRecoveryStarted = true;
                        autoFishingBatch5DisableRecoveryCastApplied = false;
                        autoFishingBatch5DisableRecoveryStartedAtUtc = DateTimeOffset.UtcNow;
                        runtime.SetHookStatus(
                            "Smoke.Batch5GcLadder.AutoFishingDisableRecovery",
                            "pending",
                            "first-party product disable + QA-owned native-control recovery",
                            "measurementComplete=true; productDisabled=true; awaitingNativeRecoveryUnit=true; owner=qa; forcedGc=false");
                        return FixtureAttemptResult.Pending;
                    }
                    BeginPrimitiveAutoFishingPerformanceTitleCleanup(toggleButton, dolocApi, stopProduct: true);
                    return FixtureAttemptResult.Pending;
                }
                if (monoGate)
                {
                    BeginPrimitiveAutoFishingMonoGateTitleCleanup(toggleButton, primitives, hookRuntime);
                    return FixtureAttemptResult.Pending;
                }
                VerifyAutoFishingReportExportForFixture(scenario);
                StopPrimitiveAutoFishingForFixture(toggleButton, session.OwnerId, "loop-complete");
                if (IsBatch5AutoFishingLevel("L5") && autoFishingBatch5TitleCycleStage == 2)
                {
                    autoFishingBatch5TitleCycleDisabled = true;
                    IAutoFishingPerformanceFixture performance = GetAutoFishingPerformanceFixture()
                        ?? throw new InvalidOperationException("Batch 5 AutoFishing L5 lost the QA performance owner after its reload loop.");
                    string source = "title -> save-slot-" + batch5GcLadder.SaveSlot.ToString(CultureInfo.InvariantCulture) + " reload -> typed product re-enable -> one native loop -> typed product disable";
                    performance.MarkBatch5TitleReloadCycle(
                        autoFishingBatch5TitleCycleSaveLoads,
                        autoFishingBatch5TitleCycleReenabled,
                        autoFishingBatch5TitleCycleDisabled,
                        source);
                    autoFishingBatch5TitleCycleStage = 3;
                    autoFishingBatch5TitleCycleStageStartedAtUtc = DateTimeOffset.UtcNow;
                    runtime.SetHookStatus(
                        "Smoke.Batch5GcLadder.AutoFishingTitleCycle",
                        "pending",
                        "title -> configured-save reload -> product re-enable/disable",
                        "title=true; reload=true; saveLoads=" + autoFishingBatch5TitleCycleSaveLoads.ToString(CultureInfo.InvariantCulture) + "; reenabled=true; loop=true; disabled=true; awaitingFinalTitleCleanup=true; owner=qa; forcedGc=false");
                    BeginPrimitiveAutoFishingPerformanceTitleCleanup(toggleButton, dolocApi, stopProduct: false);
                    return FixtureAttemptResult.Pending;
                }
                return FixtureAttemptResult.Succeeded;
            }

            if (!performanceFishLoop && (DateTimeOffset.Now - autoFishingPhaseStartedAt).TotalSeconds > 60)
                throw new TimeoutException("First-party AutoFishing loop did not complete within 60 seconds. " + BuildAutoFishingFlowSummary(progress, progress.LastSummary));
            LogAutoFishingPending("Waiting for first-party AutoFishing loop scenario=" + scenario + ". " + BuildAutoFishingFlowSummary(progress, progress.LastSummary));
            return FixtureAttemptResult.Pending;
        }

        private void BeginPrimitiveAutoFishingMonoGateTitleCleanup(string toggleButton, FishingPrimitivesService primitives, FishingPrimitiveHookRuntime hookRuntime)
        {
            if (autoFishingMonoGateAwaitingTitleCleanup)
                return;
            autoFishingMonoGateAccessorBuilds = primitives.NativeAccessorBuildCount + hookRuntime.ReadyAccessorBuildCount;
            autoFishingMonoGateAccessorRebuilds = primitives.NativeAccessorRebuildCount + hookRuntime.ReadyAccessorRebuildCount;
            autoFishingMonoGateAccessorBuildFailures = primitives.NativeAccessorBuildFailureCount + hookRuntime.ReadyAccessorBuildFailureCount;
            autoFishingMonoGateAccessorInvocationFailures = primitives.NativeAccessorInvocationFailureCount + hookRuntime.ReadyAccessorInvocationFailureCount;
            autoFishingMonoGateAccessorFailures = autoFishingMonoGateAccessorBuildFailures + autoFishingMonoGateAccessorInvocationFailures;
            autoFishingMonoGateVisibleReelPending = hookRuntime.VisibleReelPending ? 1 : 0;
            autoFishingMonoGateLegacyMaps = (fishingAutomationFeature?.ConfiguredOwnerCount ?? 0) + (fishingAutomationFeature?.ConfiguredStateCount ?? 0);
            string ownerId = primitives.TryGetActiveSession()?.OwnerId ?? string.Empty;
            StopPrimitiveAutoFishingForFixture(toggleButton, ownerId, "mono-gate-one-fish-complete");
            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? returnHome = FindMethod(dolocApi, "ReturnHome", 1);
            if (returnHome == null)
                throw new MissingMethodException("DolocAPI.ReturnHome(bool) was not found for the first-party AutoFishing Mono gate.");
            autoFishingMonoGateAwaitingTitleCleanup = true;
            autoFishingMonoGateReturnHomeRequestedAtUtc = DateTimeOffset.UtcNow;
            runtime.SetHookStatus("Smoke.AutoFishingMonoGate", "pending", "one fish -> primitive release -> DolocAPI.ReturnHome", "First-party visible-minigame fish completed; waiting for title cleanup.");
            returnHome.Invoke(null, new object[] { false });
        }

        private void StopPrimitiveAutoFishingForFixture(string toggleButton, string ownerId, string reason)
        {
            DispatchQaInputFrame(toggleButton, isDownNow: false, pressedEdge: false, releasedEdge: true);
            runtime.Update();
            DispatchQaInputTap(toggleButton);
            runtime.Update();
            if (fishingAutomationFeature?.Primitives == null)
                throw new InvalidOperationException("Fishing primitives were unavailable during product cleanup.");
            FishingSmokeCleanupSnapshot cleanup = CaptureFishingSmokeCleanupSnapshot();
            FishingSmokeCleanupAssertions.AssertCleared("First-party AutoFishing owner=" + ownerId + ", reason=" + reason, cleanup);
            string lifecycle = fishingAutomationFeature!.GetFishingAutomationLifecycleSummary();
            string details = "owner=" + ownerId + ", reason=" + reason + ", lifecycle={" + lifecycle + "}";
            runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingCleanup OK " + details);
            runtime.SetHookStatus("Smoke.AutoFishingLifecycle", "verified", "first-party primitive stop + cleanup assertion", details);
        }

        private static bool ScenarioRequestsSkip(string scenario)
        {
            return scenario.Equals("SkipMiniGame", StringComparison.OrdinalIgnoreCase) ||
                scenario.Equals("InstantSkip", StringComparison.OrdinalIgnoreCase) ||
                scenario.Equals("CombinedInstantSkip", StringComparison.OrdinalIgnoreCase);
        }

        private void FinalizeAutoFishingMonoGateTitleCleanup()
        {
            if (!autoFishingMonoGateAwaitingTitleCleanup)
                return;
            FishingPrimitivesService? primitives = fishingAutomationFeature?.Primitives;
            int nativeTransient = fishingAutomationFeature?.HookRuntimeTransientCount ?? 0;
            int sessions = primitives?.ActiveSessionCount ?? 0;
            int inputLeases = primitives?.ActiveInputLeaseCount ?? 0;
            int animationLeases = primitives?.ActiveAnimationLeaseCount ?? 0;
            bool schedulerPending = primitives?.SchedulerPending == true;
            bool nativeReferences = primitives?.NativeStateCache.WaitState != null || primitives?.NativeStateCache.MiniGameHandle != null ||
                primitives?.NativeStateCache.Agent != null || primitives?.NativeStateCache.SelectedRod != null;
            bool callbackRuntime = FishingAutomationCallbackService != null;
            FishingVisibleReelTelemetry visibleReel = primitives?.VisibleReelTelemetry ?? default;
            autoFishingMonoGatePassed = autoFishingMonoGateAccessorFailures == 0 && autoFishingMonoGateVisibleReelPending == 0 && autoFishingMonoGateLegacyMaps == 0 && nativeTransient == 0 && sessions == 0 && inputLeases == 0 &&
                animationLeases == 0 && !schedulerPending && !nativeReferences && !callbackRuntime && visibleReel.Queued > 0 && visibleReel.Consumed > 0 && visibleReel.Timeouts == 0;
            string summary = "functionalFish=1, accessorBuilds=" + autoFishingMonoGateAccessorBuilds.ToString(CultureInfo.InvariantCulture) +
                ", accessorRebuilds=" + autoFishingMonoGateAccessorRebuilds.ToString(CultureInfo.InvariantCulture) +
                ", accessorBuildFailures=" + autoFishingMonoGateAccessorBuildFailures.ToString(CultureInfo.InvariantCulture) +
                ", accessorInvocationFailures=" + autoFishingMonoGateAccessorInvocationFailures.ToString(CultureInfo.InvariantCulture) +
                ", accessorFailures=" + autoFishingMonoGateAccessorFailures.ToString(CultureInfo.InvariantCulture) +
                ", visibleReelPending=" + autoFishingMonoGateVisibleReelPending.ToString(CultureInfo.InvariantCulture) +
                ", visibleReelQueued=" + visibleReel.Queued.ToString(CultureInfo.InvariantCulture) +
                ", visibleReelConsumed=" + visibleReel.Consumed.ToString(CultureInfo.InvariantCulture) +
                ", visibleReelRetries=" + visibleReel.Retries.ToString(CultureInfo.InvariantCulture) +
                ", visibleReelTimeouts=" + visibleReel.Timeouts.ToString(CultureInfo.InvariantCulture) +
                ", legacyMaps=" + autoFishingMonoGateLegacyMaps.ToString(CultureInfo.InvariantCulture) +
                ", nativeTransient=" + nativeTransient.ToString(CultureInfo.InvariantCulture) +
                ", sessions=" + sessions.ToString(CultureInfo.InvariantCulture) +
                ", inputLeases=" + inputLeases.ToString(CultureInfo.InvariantCulture) +
                ", animationLeases=" + animationLeases.ToString(CultureInfo.InvariantCulture) +
                ", schedulerPending=" + schedulerPending.ToString(CultureInfo.InvariantCulture) +
                ", nativeReferences=" + nativeReferences.ToString(CultureInfo.InvariantCulture) +
                ", callbackRuntime=" + callbackRuntime.ToString(CultureInfo.InvariantCulture);
            runtime.SetHookStatus("Smoke.AutoFishingMonoGate", autoFishingMonoGatePassed ? "verified" : "failed", "ReturnedToTitle fishing cleanup", summary);
            runtime.RuntimeMonitor.Log("AutoFishing one-fish Mono gate " + (autoFishingMonoGatePassed ? "passed" : "failed") + ". " + summary);
            autoFishingMonoGateTitleCleanupObserved = true;
        }

        private static bool ScenarioRequestsInstantBite(string scenario)
        {
            return scenario.Equals("InstantBite", StringComparison.OrdinalIgnoreCase) ||
                scenario.Equals("InstantSkip", StringComparison.OrdinalIgnoreCase) ||
                scenario.Equals("CombinedInstantSkip", StringComparison.OrdinalIgnoreCase) ||
                scenario.Equals("CombinedInstantComplete", StringComparison.OrdinalIgnoreCase);
        }

        private void FinalizeAutoFishingPerformanceTitleCleanup()
        {
            if (!autoFishingPerformanceAwaitingTitleCleanup)
                return;
            FishingPrimitivesService? primitives = fishingAutomationFeature?.Primitives;
            FishingSmokeCleanupSnapshot cleanup = CaptureFishingSmokeCleanupSnapshot();
            IAutoFishingPerformanceFixture performanceFixture = GetAutoFishingPerformanceFixture()
                ?? throw new InvalidOperationException("AutoFishing performance title cleanup lost its staged QA owner.");
            autoFishingPerformanceTitleCleanupPassed = performanceFixture.CompleteTitleCleanup(
                new AutoFishingPerformanceTitleCleanupSnapshot
                {
                    NativeTransient = cleanup.NativeTransient,
                    Sessions = cleanup.Sessions,
                    InputLeases = cleanup.InputLeases,
                    AnimationLeases = cleanup.AnimationLeases,
                    SchedulerPending = cleanup.SchedulerPending,
                    LegacyOptions = cleanup.LegacyOptions,
                    LegacyStates = cleanup.LegacyStates,
                    HookRuntimes = cleanup.HookRuntimes,
                    NativeReferences = cleanup.NativeReferences,
                    VisibleReelPending = fishingAutomationFeature?.PrimitiveHookRuntime?.VisibleReelPending == true ? 1 : 0,
                    CallbackRuntime = cleanup.CallbackRuntime,
                    NativeTryCastEnergyGateUnavailable = primitives?.Diagnostics.NativeEnergyGateUnavailableRejectionCount ?? 0,
                    NativeTryCastInsufficientEnergy = primitives?.Diagnostics.NativeInsufficientEnergyRejectionCount ?? 0
                });
            autoFishingPerformanceTitleCleanupObserved = true;
        }

        private string GetAutoFishingToggleButtonForFixture()
        {
            string configured = string.IsNullOrWhiteSpace(g6FixtureState?.AutoFishingToggleKey) ? "F6" : g6FixtureState!.AutoFishingToggleKey.Trim();
            DtmKeybindList parsed = DtmKeybindList.Parse(configured);
            if (parsed.Keybinds.Count != 1 || parsed.Keybinds[0].Buttons.Count != 1)
                throw new InvalidOperationException("AutoFishing smoke requires one simple toggle button; configured binding=" + parsed + ".");
            return parsed.Keybinds[0].Buttons[0].Id;
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
                ", instantBiteBaseline=" + progress.InstantBiteCommittedBaseline.ToString(CultureInfo.InvariantCulture) +
                ", skip=" + progress.SkipObserved +
                ", readyCharge=" + progress.ReadyChargeObserved +
                ", fastAnimation=" + progress.AnimationSpeedObserved +
                ", phase=" + GameBridgeNativeHelpers.FirstText(progress.LastPhase, "unknown") +
                ", last=" + GameBridgeNativeHelpers.FirstText(progress.LastSummary, fallbackSummary, "none");
        }

        private sealed class AutoFishingSmokeFlowProgress
        {
            internal string Scenario { get; private set; } = string.Empty;
            internal string OwnerId { get; private set; } = string.Empty;
            internal bool AutoCastObserved { get; set; }
            internal bool WaitObserved { get; set; }
            internal bool BiteReadyObserved { get; set; }
            internal bool InstantBiteObserved { get; set; }
            internal long InstantBiteCommittedBaseline { get; private set; }
            internal bool BattleOrPullObserved { get; set; }
            internal bool PullExitObserved { get; set; }
            internal bool NextAutoCastObserved { get; set; }
            internal bool SkipObserved { get; set; }
            internal bool MiniGameCompleteObserved { get; set; }
            internal bool ReadyChargeObserved { get; set; }
            internal bool ReadyChargePublished { get; set; }
            internal bool AnimationSpeedObserved { get; set; }
            internal bool AnimationSpeedPublished { get; set; }
            internal bool CompletionPublished { get; set; }
            internal string LastPhase { get; set; } = string.Empty;
            internal string LastSummary { get; set; } = string.Empty;

            internal void Reset(string scenario, long instantBiteCommittedBaseline)
            {
                Scenario = scenario;
                OwnerId = string.Empty;
                InstantBiteCommittedBaseline = instantBiteCommittedBaseline;
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
                ReadyChargePublished = false;
                AnimationSpeedObserved = false;
                AnimationSpeedPublished = false;
                CompletionPublished = false;
                LastPhase = string.Empty;
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

        private void VerifyAutoFishingReportExportForFixture(string scenario)
        {
            if (autoFishingReportExported)
                return;

            if (!TryVerifyDiagnosticsSnapshotForFixture("AutoFishing " + scenario + " report export", "FishingAutomation"))
                throw new InvalidOperationException("AutoFishing smoke report export verification failed for " + scenario + ".");

            autoFishingReportExported = true;
        }

    }
}
