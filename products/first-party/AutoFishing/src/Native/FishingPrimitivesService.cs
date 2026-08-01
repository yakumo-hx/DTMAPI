using System;
using global::DTMAPI.Abstractions;


namespace Yuuka.DTMAPI.AutoFishing
{
    internal sealed class FishingPrimitivesService
    {
        private static readonly TimeSpan PendingCastTimeout = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan PendingCastBackoff = TimeSpan.FromSeconds(2.5);
        private readonly FishingProductContext runtime;
        private readonly AutoFishingNativeRuntime feature;
        private readonly FishingNativeStateCache nativeStateCache;
        private readonly FishingNativeAdapter nativeAdapter;
        private readonly FishingAutoCastScheduler scheduler = new FishingAutoCastScheduler();
        private FishingQaDiagnostics? qaDiagnostics;
        private FishingRuntimeSession? activeSession;
        private FishingPrimitiveHookRuntime? hookRuntime;
        private string lastReleaseReason = string.Empty;
        private string pendingInputFaultReason = string.Empty;

        internal FishingPrimitivesService(FishingProductContext runtime, AutoFishingNativeRuntime feature)
        {
            this.runtime = runtime;
            this.feature = feature;
            nativeStateCache = new FishingNativeStateCache(runtime);
            nativeAdapter = new FishingNativeAdapter(nativeStateCache);
        }

        internal bool HasActiveSession => activeSession?.IsReleased == false && activeSession.IsFaulted == false;
        internal FishingQaDiagnostics EnableQaObservation() => qaDiagnostics ??= new FishingQaDiagnostics();
        internal FishingNativeStateCache NativeStateCache => nativeStateCache;
        internal object? CurrentWaitState => nativeStateCache.WaitState;
        internal int ActiveSessionCount => HasActiveSession ? 1 : 0;
        internal int ActiveInputLeaseCount => activeSession?.HasInputLease == true ? 1 : 0;
        internal int ActiveAnimationLeaseCount => activeSession?.HasAnimationLease == true ? 1 : 0;
        internal bool SchedulerPending => scheduler.HasPendingCast;
        internal bool FishingHooksReady => hookRuntime?.FishingHooksInstalled == true;
        internal string LastReleaseReason => lastReleaseReason;
        internal int NativeAccessorBuildCount => nativeStateCache.AccessorBuilds + nativeAdapter.MiniGameAccessorBuilds + nativeAdapter.TransactionAccessorBuilds + nativeAdapter.UseFishRodAccessorBuilds + nativeAdapter.EnergyGateAccessorBuilds;
        internal int NativeAccessorRebuildCount => nativeStateCache.AccessorRebuilds + nativeAdapter.MiniGameAccessorRebuilds + nativeAdapter.TransactionAccessorRebuilds + nativeAdapter.UseFishRodAccessorRebuilds + nativeAdapter.EnergyGateAccessorRebuilds;
        internal int NativeAccessorBuildFailureCount => nativeStateCache.AccessorBuildFailures + nativeAdapter.MiniGameAccessorBuildFailures + nativeAdapter.TransactionAccessorBuildFailures + nativeAdapter.UseFishRodAccessorBuildFailures + nativeAdapter.EnergyGateAccessorBuildFailures;
        internal int NativeAccessorInvocationFailureCount => nativeStateCache.AccessorInvocationFailures + nativeAdapter.MiniGameAccessorInvocationFailures + nativeAdapter.TransactionAccessorInvocationFailures + nativeAdapter.UseFishRodAccessorInvocationFailures + nativeAdapter.EnergyGateAccessorInvocationFailures;
        internal int NativeAccessorFailureCount => NativeAccessorBuildFailureCount + NativeAccessorInvocationFailureCount;
        internal long NativeFrameRefreshCount => nativeStateCache.FrameRefreshes;
        internal void AttachHookRuntime(FishingPrimitiveHookRuntime runtimeService)
        {
            hookRuntime = runtimeService ?? throw new ArgumentNullException(nameof(runtimeService));
        }

        internal void DetachHookRuntime(FishingPrimitiveHookRuntime runtimeService)
        {
            if (!ReferenceEquals(hookRuntime, runtimeService))
                return;
            hookRuntime = null;
        }

        internal FishingRuntimeSession? TryGetActiveSession() => HasActiveSession ? activeSession : null;

        internal bool TryGetAnimationRequest(out string ownerId, out FishingAnimationLeaseRequest request)
        {
            FishingRuntimeSession? session = TryGetActiveSession();
            if (session == null || !session.HasAnimationLease)
            {
                ownerId = string.Empty;
                request = new FishingAnimationLeaseRequest(1d, 1d, 1d);
                return false;
            }
            ownerId = session.OwnerId;
            request = session.AnimationRequest;
            return true;
        }

        internal bool TryGetReadyPolicy(out string ownerId, out double chargeRatio, out double animationMultiplier)
        {
            FishingRuntimeSession? session = TryGetActiveSession();
            if (session == null)
            {
                ownerId = string.Empty;
                chargeRatio = 0d;
                animationMultiplier = 1d;
                return false;
            }
            ownerId = session.OwnerId;
            chargeRatio = session.CastChargeRatio;
            animationMultiplier = session.HasAnimationLease ? session.AnimationRequest.ReadyMultiplier : 1d;
            return true;
        }

        internal FishingRuntimeSession? StartSession(
            string ownerId,
            Action<FishingPrimitivePhase, FishingPrimitiveTransitionKind, string>? transitionHandler)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("A product-native session requires an owner ID.", nameof(ownerId));
            if (activeSession?.IsReleased == false)
            {
                if (activeSession.IsFaulted)
                {
                    runtime.RuntimeMonitor.Log("AutoFishing product-native session cannot be reused before its pending input fault is closed.", LogLevel.Warn);
                    return null;
                }
                if (activeSession!.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                    return activeSession;
                runtime.RuntimeMonitor.Log("AutoFishing product-native session is already owned by " + activeSession.OwnerId + ".", LogLevel.Warn);
                return null;
            }
            if (pendingInputFaultReason.Length > 0)
            {
                runtime.RuntimeMonitor.Log("AutoFishing product-native session activation is blocked by pending fault=" + pendingInputFaultReason + ".", LogLevel.Warn);
                return null;
            }

            if (!feature.CanActivatePrimitiveSession(out string blockedReason))
            {
                runtime.RuntimeMonitor.Log("AutoFishing product-native session activation was blocked: " + blockedReason, LogLevel.Warn);
                return null;
            }

            FishingPrimitiveActivationResult activation = feature.TryActivatePrimitiveSession(this);
            if (!activation.Success || activation.Runtime == null)
            {
                runtime.RuntimeMonitor.Log("AutoFishing product-native session activation failed status=" + activation.Status + " message=" + activation.Message, LogLevel.Warn);
                return null;
            }

            var session = new FishingRuntimeSession(this, ownerId, transitionHandler);
            try
            {
                nativeStateCache.RefreshFrame();
                scheduler.Reset();
                lastReleaseReason = string.Empty;
                session.Advance(FishingPrimitivePhase.Idle, FishingPrimitiveTransitionKind.SnapshotChanged, "session-acquired", false);
                activeSession = session;
                runtime.RuntimeMonitor.Log("AutoFishing product-native session acquired owner=" + ownerId + " session=" + session.SessionId + ".");
                return session;
            }
            catch (Exception ex)
            {
                feature.RollbackPrimitiveActivation(activation.Runtime, "activation-failed:" + ex.GetType().Name);
                scheduler.Reset();
                nativeStateCache.ClearRuntimeReferences();
                activeSession = null;
                runtime.RuntimeMonitor.Log("AutoFishing product-native activation failed owner=" + ownerId + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
                return null;
            }
        }

        internal void InvalidateEnvironment(string reason)
        {
            nativeAdapter.InvalidateEnvironment(reason ?? string.Empty);
        }

        internal void ResetForLifecycleBoundary(string reason)
        {
            activeSession?.Release("lifecycle:" + (reason ?? string.Empty));
            nativeAdapter.InvalidateEnvironment(reason ?? string.Empty);
        }

        internal int RemoveOwner(string ownerId, string reason)
        {
            if (activeSession == null || !activeSession.OwnerId.Equals(ownerId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                return 0;
            activeSession.Release("owner-cleanup:" + (reason ?? string.Empty));
            return 1;
        }

        internal int CountOwnerResources(string ownerId) =>
            activeSession != null && activeSession.OwnerId.Equals(ownerId ?? string.Empty, StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        internal FishingPrimitiveSnapshot GetSnapshot(FishingRuntimeSession session)
        {
            return session.BuildSnapshot(nativeStateCache.Current);
        }

        internal void RefreshNativeState()
        {
            if (!HasActiveSession)
                return;
            FishingRuntimeSession session = activeSession!;
            FishingNativeAvailability native = nativeStateCache.RefreshFrame();
            hookRuntime?.NotifyFishingNativeFrame();
            ReconcileNativeAvailability(session, native);
        }

        internal void ReconcileNativeAvailability(FishingRuntimeSession session, FishingNativeAvailability native)
        {
            if (!ReferenceEquals(activeSession, session) || session.IsReleased || !native.CanCast)
                return;
            if (session.Phase != FishingPrimitivePhase.ReadyEntered &&
                session.Phase != FishingPrimitivePhase.CastEntered &&
                session.Phase != FishingPrimitivePhase.WaitEntered)
                return;

            // The native agent has already returned to an item-usable non-fishing
            // state without reaching playable Wait. Treat that as an interrupted cast
            // so the product scheduler can retry instead of remaining stuck forever.
            scheduler.Confirm(DateTimeOffset.UtcNow, PendingCastBackoff);
            session.Advance(FishingPrimitivePhase.Interrupted, FishingPrimitiveTransitionKind.Interrupted, "native-cast-exited-before-wait", false);
        }

        internal FishingPrimitiveResult TryCast(FishingRuntimeSession session, long observedSequence, FishingPrimitiveCastRequest request, string reason)
        {
            if (!ValidateSessionOperation(session, observedSequence, out FishingPrimitiveResult rejected))
                return rejected;
            if (hookRuntime?.FishingHooksInstalled != true)
                return Reject(session, "hooks-unavailable", "The complete first-party fishing Hook set is not installed.");
            if (!session.CanCastFromCurrentPhase)
                return Reject(session, "invalid-phase", "TryCast is not valid from " + session.Phase + ".");
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (scheduler.ExpirePending(now, PendingCastTimeout, PendingCastBackoff))
                session.Advance(FishingPrimitivePhase.Interrupted, FishingPrimitiveTransitionKind.Interrupted, "pending-cast-timeout", false);
            if (!scheduler.CanAttempt(now))
                return Reject(session, "not-due", "A cast is pending or the scheduler backoff has not elapsed.");

            session.ApplyCastRequest(request);
            long sequenceBefore = session.Sequence;
            FishingNativeOperationResult native = nativeAdapter.TryCast(reason);
            if (native.Applied)
                qaDiagnostics?.RecordCastApplied();
            if (!native.Applied)
                return new FishingPrimitiveResult(false, native.Status, native.Message, session.Sequence);
            scheduler.MarkAttempt(now);
            if (session.Sequence == sequenceBefore)
                session.Advance(FishingPrimitivePhase.ReadyEntered, FishingPrimitiveTransitionKind.SnapshotChanged, "native-cast-invoked", false);
            return new FishingPrimitiveResult(true, native.Status, native.Message, session.Sequence);
        }

        internal FishingPrimitiveResult TryPrepareNativeBite(FishingRuntimeSession session, long observedSequence, string reason)
        {
            if (!ValidateSessionOperation(session, observedSequence, out FishingPrimitiveResult rejected))
                return rejected;
            object? waitState = nativeStateCache.WaitState;
            if (session.Phase != FishingPrimitivePhase.WaitPlayable || waitState == null)
                return Reject(session, "invalid-phase", "Native bite preparation requires WaitPlayable.");
            bool isFish = false;
            string message = "Fishing automation runtime service is unavailable.";
            if (!nativeAdapter.TryPrepareNativeBite(waitState, out isFish, out message))
                return Reject(session, "native-bite-failed", message);
            qaDiagnostics?.RecordNativeBitePrepared();
            session.Advance(FishingPrimitivePhase.BiteReady, FishingPrimitiveTransitionKind.BiteReady, reason, isFish);
            return new FishingPrimitiveResult(true, "bite-ready", message, session.Sequence);
        }

        internal FishingPrimitiveResult TryReel(FishingRuntimeSession session, long observedSequence, FishingPrimitiveReelMode mode, string reason)
        {
            if (!ValidateSessionOperation(session, observedSequence, out FishingPrimitiveResult rejected))
                return rejected;
            object? waitState = nativeStateCache.WaitState;
            if (session.Phase != FishingPrimitivePhase.BiteReady || waitState == null)
                return Reject(session, "invalid-phase", "TryReel requires BiteReady.");
            if (mode == FishingPrimitiveReelMode.NativeVisibleMiniGame)
            {
                if (hookRuntime?.TryQueueVisibleReelInput(waitState) != true)
                    return Reject(session, "native-reel-input-unavailable", "The scoped native reel input could not be queued.");
                qaDiagnostics?.RecordNativeReel(mode);
                return new FishingPrimitiveResult(true, "reel-input-queued", "Native use-tool edge queued for AgentStateFishingWait.NextState.", session.Sequence);
            }

            long sequenceBefore = session.Sequence;
            if (hookRuntime?.TryQueueVisibleReelInput(waitState) != true)
                return Reject(session, "native-reel-input-unavailable", "The scoped native reel input could not be queued for the skip-result transaction.");
            if (!nativeAdapter.TryReel(waitState, mode, out string nativeState))
            {
                hookRuntime.ClearQueuedFishingReelInput();
                return Reject(session, "native-reel-failed", "The native skip-result transaction did not advance.");
            }
            qaDiagnostics?.RecordNativeReel(mode);
            if (session.Sequence == sequenceBefore)
                session.Advance(FishingPrimitivePhase.PullEntered, FishingPrimitiveTransitionKind.PullEntered, reason + ":" + nativeState, session.IsFishResult);
            return new FishingPrimitiveResult(true, "reel-applied", nativeState, session.Sequence);
        }

        internal bool TryDecideMiniGameInput(object gameHandle, out FishingSyntheticInputAction action, out FishingMiniGameFrame frame)
        {
            action = FishingSyntheticInputAction.Release;
            frame = default;
            FishingRuntimeSession? session = activeSession;
            if (session == null || session.IsReleased || session.IsFaulted)
                return false;
            return session.TryDecideMiniGameInput(gameHandle, nativeAdapter.TryBuildMiniGameFrame, out action, out frame);
        }

        internal bool TryGetPendingInputFault(out string reason)
        {
            reason = pendingInputFaultReason;
            return reason.Length > 0;
        }

        internal void AcknowledgeInputFault(string reason)
        {
            if (pendingInputFaultReason.Equals(reason ?? string.Empty, StringComparison.Ordinal))
                pendingInputFaultReason = string.Empty;
        }

        internal bool TryReadMiniGameStatus(object gameHandle, out FishingNativeMiniGameStatus status)
        {
            return nativeAdapter.TryReadMiniGameStatus(gameHandle, out status);
        }

        internal bool TryReadWaitState(object waitState, out bool isCurrent, out bool waitingForBite, out bool isFish)
        {
            return nativeStateCache.TryReadWaitState(waitState, out isCurrent, out waitingForBite, out isFish);
        }

        internal void NotifyMiniGameStatus(object gameHandle, FishingNativeMiniGameStatus status)
        {
            FishingRuntimeSession? session = activeSession;
            if (session == null || session.IsReleased)
                return;
            if (status == FishingNativeMiniGameStatus.Running)
                session.AdvanceIfChanged(FishingPrimitivePhase.MiniGameRunning, FishingPrimitiveTransitionKind.SnapshotChanged, "native-minigame-running", session.IsFishResult);
        }

        internal void ConfirmNativeProgress(FishingPrimitivePhase phase)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (phase == FishingPrimitivePhase.PullExited)
            {
                scheduler.Confirm(now, TimeSpan.FromSeconds(0.25));
                return;
            }
            if (scheduler.HasPendingCast)
                scheduler.Confirm(now, TimeSpan.Zero);
        }

        internal void NotifyVisibleReelTimedOut(FishingRuntimeSession session, string reason)
        {
            if (!ReferenceEquals(activeSession, session) || session.IsReleased || session.Phase != FishingPrimitivePhase.BiteReady)
                return;
            scheduler.Confirm(DateTimeOffset.UtcNow, PendingCastBackoff);
            session.Advance(FishingPrimitivePhase.Interrupted, FishingPrimitiveTransitionKind.Interrupted, reason, false);
            nativeStateCache.WaitState = null;
        }

        internal void ReleaseAnimationPolicy(FishingRuntimeSession session, string reason)
        {
            if (!ReferenceEquals(activeSession, session) || session.IsReleased)
                return;
            feature.RestorePrimitiveAnimation(reason);
        }

        internal void ReleaseSession(FishingRuntimeSession session, string reason)
        {
            if (!ReferenceEquals(activeSession, session))
                return;
            string releaseReason = reason ?? string.Empty;
            lastReleaseReason = releaseReason;
            try
            {
                feature.ReleasePrimitiveSession(releaseReason);
            }
            finally
            {
                scheduler.Reset();
                nativeStateCache.ClearRuntimeReferences();
                activeSession = null;
            }
            runtime.RuntimeMonitor.Log("AutoFishing product-native session released owner=" + session.OwnerId + " session=" + session.SessionId + " reason=" + releaseReason + ".");
        }

        internal void RecordQaTransition(FishingPrimitiveTransitionKind kind) => qaDiagnostics?.RecordTransition(kind);
        internal void RecordVisibleReelQueued() => qaDiagnostics?.RecordVisibleReelQueued();
        internal void RecordVisibleReelConsumed() => qaDiagnostics?.RecordVisibleReelConsumed();
        internal void RecordVisibleReelNativeAccepted() => qaDiagnostics?.RecordVisibleReelNativeAccepted();
        internal void RecordVisibleReelRetry() => qaDiagnostics?.RecordVisibleReelRetry();
        internal void RecordVisibleReelTimeout() => qaDiagnostics?.RecordVisibleReelTimeout();
        internal void RecordAnimationApplication() => qaDiagnostics?.RecordAnimationApplication();
        internal void RecordReadyChargeApplication() => qaDiagnostics?.RecordReadyChargeApplication();

        internal void RecordTransitionSubscriberFailure(FishingRuntimeSession session, Exception ex)
        {
            runtime.RuntimeMonitor.Log("Fishing primitive transition subscriber failed during release owner=" + session.OwnerId + " session=" + session.SessionId + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
        }

        internal void RecordMiniGameInputFault(
            FishingRuntimeSession session,
            string stage,
            string detail,
            Exception? error)
        {
            if (!ReferenceEquals(activeSession, session) || session.IsReleased || session.IsFaulted)
                return;

            string reason = "minigame-input-fault:" + (error == null ? "FrameAccess" : error.GetType().Name);
            if (pendingInputFaultReason.Length == 0)
                pendingInputFaultReason = reason;
            session.MarkInputFaulted(reason);

            string errorDetail = error == null
                ? (detail ?? string.Empty)
                : error.GetType().Name + ": " + error.Message;
            runtime.RuntimeMonitor.Log(
                "Fishing minigame-input transaction failed owner=" + session.OwnerId +
                " stage=" + (stage ?? string.Empty) +
                " detail=" + errorDetail + ".",
                LogLevel.Warn);

            bool closed = false;
            try
            {
                closed = session.NotifyInputFault(reason);
            }
            catch (Exception closeFailure)
            {
                runtime.RuntimeMonitor.Log(
                    "Fishing minigame-input fault cleanup callback failed owner=" + session.OwnerId +
                    " error=" + closeFailure.GetType().Name + ": " + closeFailure.Message,
                    LogLevel.Warn);
            }
            if (closed)
                AcknowledgeInputFault(reason);
        }

        private bool ValidateSessionOperation(FishingRuntimeSession session, long observedSequence, out FishingPrimitiveResult rejected)
        {
            if (!ReferenceEquals(activeSession, session) || session.IsReleased || session.IsFaulted)
            {
                string status = session.IsFaulted ? "faulted" : "released";
                string message = session.IsFaulted
                    ? "The fishing primitive session is faulted and must be cleaned before reuse."
                    : "The fishing primitive session is released.";
                rejected = new FishingPrimitiveResult(false, status, message, session.Sequence);
                qaDiagnostics?.RecordRejectedOperation();
                return false;
            }
            if (session.Sequence != observedSequence)
            {
                rejected = new FishingPrimitiveResult(false, "stale-snapshot", "The observed fishing snapshot sequence is stale.", session.Sequence);
                qaDiagnostics?.RecordRejectedOperation();
                return false;
            }
            rejected = default;
            return true;
        }

        private FishingPrimitiveResult Reject(FishingRuntimeSession session, string status, string message)
        {
            qaDiagnostics?.RecordRejectedOperation();
            return new FishingPrimitiveResult(false, status, message, session.Sequence);
        }

        internal void NotifyHookPhase(string phase, object? source)
        {
            FishingRuntimeSession? session = TryGetActiveSession();
            if (session == null)
                return;
            switch ((phase ?? string.Empty).Trim())
            {
                case "Ready":
                    ConfirmNativeProgress(FishingPrimitivePhase.ReadyEntered);
                    session.AdvanceIfChanged(FishingPrimitivePhase.ReadyEntered, FishingPrimitiveTransitionKind.ReadyEntered, "native-ready", session.IsFishResult);
                    break;
                case "Cast":
                    ConfirmNativeProgress(FishingPrimitivePhase.CastEntered);
                    session.AdvanceIfChanged(FishingPrimitivePhase.CastEntered, FishingPrimitiveTransitionKind.CastEntered, "native-cast", session.IsFishResult);
                    break;
                case "Wait":
                    ConfirmNativeProgress(FishingPrimitivePhase.WaitEntered);
                    session.AdvanceIfChanged(FishingPrimitivePhase.WaitEntered, FishingPrimitiveTransitionKind.WaitEntered, "native-wait-enter", session.IsFishResult);
                    nativeStateCache.WaitState = source;
                    break;
                case "MiniGame":
                    session.ClearBonusTaps();
                    session.AdvanceIfChanged(FishingPrimitivePhase.MiniGameStarted, FishingPrimitiveTransitionKind.MiniGameStarted, "native-minigame-start", session.IsFishResult);
                    break;
                case "MiniGameStop":
                    session.ClearBonusTaps();
                    session.AdvanceIfChanged(FishingPrimitivePhase.MiniGameStopped, FishingPrimitiveTransitionKind.MiniGameStopped, "native-minigame-stop", session.IsFishResult);
                    break;
                case "Pull":
                    session.ClearBonusTaps();
                    session.AdvanceIfChanged(FishingPrimitivePhase.PullEntered, FishingPrimitiveTransitionKind.PullEntered, "native-pull", session.IsFishResult);
                    break;
                case "Cooldown":
                case "NativeExit":
                    ConfirmNativeProgress(FishingPrimitivePhase.PullExited);
                    session.AdvanceIfChanged(FishingPrimitivePhase.PullExited, FishingPrimitiveTransitionKind.PullExited, "native-fishing-exit", false);
                    nativeStateCache.WaitState = null;
                    session.ClearBonusTaps();
                    break;
            }
        }

        internal bool NotifyHookWaitPlayable(object waitState, bool waitingForBite, bool isFish, string source)
        {
            FishingRuntimeSession? session = TryGetActiveSession();
            if (session == null)
                return false;
            nativeStateCache.WaitState = waitState;
            if (waitingForBite)
                session.AdvanceIfChanged(FishingPrimitivePhase.WaitPlayable, FishingPrimitiveTransitionKind.WaitPlayable, source, isFish);
            else
                session.AdvanceIfChanged(FishingPrimitivePhase.BiteReady, FishingPrimitiveTransitionKind.BiteReady, source, isFish);
            return true;
        }
    }

    internal sealed class FishingRuntimeSession
    {
        private readonly FishingPrimitivesService primitives;
        private readonly FishingMiniGameInputTransaction miniGameInputTransaction = new FishingMiniGameInputTransaction();
        private readonly Action<FishingPrimitivePhase, FishingPrimitiveTransitionKind, string>? transitionHandler;
        private Func<string, bool>? inputFaultHandler;
        private Func<FishingMiniGameFrame, FishingSyntheticInputAction>? inputProvider;
        private FishingAnimationLeaseRequest? animationRequest;

        internal FishingRuntimeSession(
            FishingPrimitivesService primitives,
            string ownerId,
            Action<FishingPrimitivePhase, FishingPrimitiveTransitionKind, string>? transitionHandler)
        {
            this.primitives = primitives;
            this.transitionHandler = transitionHandler;
            OwnerId = ownerId;
            SessionId = Guid.NewGuid().ToString("N");
            Phase = FishingPrimitivePhase.Idle;
        }

        public string OwnerId { get; }
        public string SessionId { get; }
        public bool IsReleased { get; private set; }
        internal bool IsFaulted { get; private set; }
        internal string FaultReason { get; private set; } = string.Empty;
        internal long Sequence { get; private set; }
        internal FishingPrimitivePhase Phase { get; private set; }
        internal bool IsFishResult { get; private set; }
        internal double CastChargeRatio { get; private set; }
        internal bool HasInputLease => inputProvider != null;
        internal bool HasAnimationLease => animationRequest.HasValue;
        internal FishingAnimationLeaseRequest AnimationRequest => animationRequest ?? new FishingAnimationLeaseRequest(1d, 1d, 1d);
        internal bool CanCastFromCurrentPhase => !IsFaulted &&
            (Phase == FishingPrimitivePhase.Idle || Phase == FishingPrimitivePhase.PullExited || Phase == FishingPrimitivePhase.Interrupted);

        public FishingPrimitiveSnapshot GetSnapshot() => primitives.GetSnapshot(this);
        public FishingPrimitiveResult TryCast(long observedSequence, FishingPrimitiveCastRequest request, string reason) => primitives.TryCast(this, observedSequence, request, reason);
        public FishingPrimitiveResult TryPrepareNativeBite(long observedSequence, string reason) => primitives.TryPrepareNativeBite(this, observedSequence, reason);
        public FishingPrimitiveResult TryReel(long observedSequence, FishingPrimitiveReelMode mode, string reason) => primitives.TryReel(this, observedSequence, mode, reason);

        internal void ConfigureAutomation(
            Func<FishingMiniGameFrame, FishingSyntheticInputAction>? provider,
            FishingAnimationLeaseRequest? request,
            string reason)
        {
            if (IsReleased)
                throw new ObjectDisposedException(nameof(FishingRuntimeSession));
            if (IsFaulted)
                throw new InvalidOperationException("A faulted fishing session cannot be reconfigured.");
            if (animationRequest.HasValue)
                primitives.ReleaseAnimationPolicy(this, "reconfigure:" + (reason ?? string.Empty));
            inputProvider = provider;
            animationRequest = request.HasValue
                ? new FishingAnimationLeaseRequest(
                    ClampMultiplier(request.Value.ReadyMultiplier),
                    ClampMultiplier(request.Value.CastHookMultiplier),
                    ClampMultiplier(request.Value.PullMultiplier))
                : (FishingAnimationLeaseRequest?)null;
        }

        public void Release(string reason)
        {
            if (IsReleased)
                return;
            try
            {
                inputProvider = null;
                animationRequest = null;
                IsReleased = true;
                Sequence++;
                try
                {
                    transitionHandler?.Invoke(Phase, FishingPrimitiveTransitionKind.Released, reason ?? string.Empty);
                }
                catch (Exception ex)
                {
                    primitives.RecordTransitionSubscriberFailure(this, ex);
                }
            }
            finally
            {
                if (!IsReleased)
                {
                    IsReleased = true;
                    Sequence++;
                }
                inputProvider = null;
                inputFaultHandler = null;
                animationRequest = null;
                miniGameInputTransaction.Clear();
                primitives.ReleaseSession(this, reason);
            }
        }

        internal FishingPrimitiveSnapshot BuildSnapshot(FishingNativeAvailability native)
        {
            return new FishingPrimitiveSnapshot(
                Sequence,
                Phase,
                CanCastFromCurrentPhase && native.CanCast && native.HasSelectedRod && native.HasFishingPool,
                native.HasSelectedRod,
                native.HasFishingPool,
                Phase == FishingPrimitivePhase.WaitPlayable,
                Phase == FishingPrimitivePhase.BiteReady,
                IsFishResult,
                Phase == FishingPrimitivePhase.MiniGameStarted || Phase == FishingPrimitivePhase.MiniGameRunning,
                native.HorizontalMoveFactorAvailable,
                native.HorizontalMoveFactor);
        }

        internal void Advance(FishingPrimitivePhase phase, FishingPrimitiveTransitionKind kind, string reason, bool isFish)
        {
            if (IsReleased || IsFaulted)
                return;
            Phase = phase;
            IsFishResult = isFish;
            Sequence++;
            primitives.RecordQaTransition(kind);
            transitionHandler?.Invoke(Phase, kind, reason ?? string.Empty);
        }

        internal void AdvanceIfChanged(FishingPrimitivePhase phase, FishingPrimitiveTransitionKind kind, string reason, bool isFish)
        {
            if (Phase == phase && IsFishResult == isFish)
                return;
            Advance(phase, kind, reason, isFish);
        }

        internal void SetInputFaultHandler(Func<string, bool> handler)
        {
            if (IsReleased)
                throw new ObjectDisposedException(nameof(FishingRuntimeSession));
            inputFaultHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        internal bool TryDecideMiniGameInput(
            object gameHandle,
            FishingMiniGameFrameReader frameReader,
            out FishingSyntheticInputAction action,
            out FishingMiniGameFrame frame)
        {
            if (IsReleased || IsFaulted || inputProvider == null)
            {
                action = FishingSyntheticInputAction.Release;
                frame = default;
                return false;
            }
            return miniGameInputTransaction.TryExecute(
                gameHandle,
                Sequence,
                frameReader,
                inputProvider,
                () => AdvanceIfChanged(FishingPrimitivePhase.MiniGameRunning, FishingPrimitiveTransitionKind.SnapshotChanged, "minigame-input", IsFishResult),
                (stage, detail, error) => primitives.RecordMiniGameInputFault(this, stage, detail, error),
                out action,
                out frame);
        }

        internal void MarkInputFaulted(string reason)
        {
            IsFaulted = true;
            FaultReason = reason ?? string.Empty;
            inputProvider = null;
            miniGameInputTransaction.Clear();
        }

        internal bool NotifyInputFault(string reason) => inputFaultHandler?.Invoke(reason ?? string.Empty) == true;

        internal void ClearBonusTaps() => miniGameInputTransaction.Clear();

        internal void ApplyCastRequest(FishingPrimitiveCastRequest request)
        {
            double ratio = request.ChargeRatio;
            if (double.IsNaN(ratio) || double.IsInfinity(ratio))
                ratio = 0d;
            ratio = Math.Max(0d, Math.Min(1d, ratio));
            if (Math.Abs(CastChargeRatio - ratio) < 0.000001d)
                return;
            CastChargeRatio = ratio;
        }

        private static double ClampMultiplier(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 1d;
            return Math.Max(1d, Math.Min(4d, value));
        }
    }

}
