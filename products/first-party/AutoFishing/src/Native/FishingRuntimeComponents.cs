using System;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal sealed class FishingAutoCastScheduler
    {
        private DateTimeOffset nextDueAtUtc = DateTimeOffset.MinValue;
        private DateTimeOffset pendingSinceUtc = DateTimeOffset.MinValue;

        internal bool HasPendingCast => pendingSinceUtc != DateTimeOffset.MinValue;
        internal DateTimeOffset NextDueAtUtc => nextDueAtUtc;

        internal void Schedule(DateTimeOffset nowUtc, TimeSpan delay)
        {
            nextDueAtUtc = nowUtc + (delay < TimeSpan.Zero ? TimeSpan.Zero : delay);
        }

        internal bool CanAttempt(DateTimeOffset nowUtc)
        {
            return !HasPendingCast && (nextDueAtUtc == DateTimeOffset.MinValue || nowUtc >= nextDueAtUtc);
        }

        internal void MarkAttempt(DateTimeOffset nowUtc)
        {
            pendingSinceUtc = nowUtc;
            nextDueAtUtc = DateTimeOffset.MaxValue;
        }

        internal void Confirm(DateTimeOffset nowUtc, TimeSpan nextDelay)
        {
            pendingSinceUtc = DateTimeOffset.MinValue;
            Schedule(nowUtc, nextDelay);
        }

        internal bool ExpirePending(DateTimeOffset nowUtc, TimeSpan timeout, TimeSpan backoff)
        {
            if (!HasPendingCast || nowUtc - pendingSinceUtc < timeout)
                return false;
            pendingSinceUtc = DateTimeOffset.MinValue;
            Schedule(nowUtc, backoff);
            return true;
        }

        internal void Reset()
        {
            nextDueAtUtc = DateTimeOffset.MinValue;
            pendingSinceUtc = DateTimeOffset.MinValue;
        }
    }

    internal sealed class FishingQaDiagnostics
    {
        internal long TransitionCount { get; private set; }
        internal long CastAppliedCount { get; private set; }
        internal long PullEnteredCount { get; private set; }
        internal long PullExitedCount { get; private set; }
        internal long NativeBitePreparedCount { get; private set; }
        internal long NativeVisibleReelCount { get; private set; }
        internal long NativeSkipReelCount { get; private set; }
        internal long VisibleReelQueued { get; private set; }
        internal long VisibleReelConsumed { get; private set; }
        internal long VisibleReelNativeAccepted { get; private set; }
        internal long VisibleReelRetries { get; private set; }
        internal long VisibleReelTimeouts { get; private set; }
        internal int AnimationApplicationCount { get; private set; }
        internal int ReadyChargeApplicationCount { get; private set; }
        internal long RejectedOperationCount { get; private set; }

        internal void RecordTransition(FishingPrimitiveTransitionKind kind)
        {
            TransitionCount++;
            if (kind == FishingPrimitiveTransitionKind.PullEntered)
                PullEnteredCount++;
            else if (kind == FishingPrimitiveTransitionKind.PullExited)
                PullExitedCount++;
        }

        internal void RecordCastApplied() => CastAppliedCount++;
        internal void RecordNativeBitePrepared() => NativeBitePreparedCount++;
        internal void RecordNativeReel(FishingPrimitiveReelMode mode)
        {
            if (mode == FishingPrimitiveReelMode.SkipMiniGameNativeResult)
                NativeSkipReelCount++;
            else
                NativeVisibleReelCount++;
        }
        internal void RecordVisibleReelQueued() => VisibleReelQueued++;
        internal void RecordVisibleReelConsumed() => VisibleReelConsumed++;
        internal void RecordVisibleReelNativeAccepted() => VisibleReelNativeAccepted++;
        internal void RecordVisibleReelRetry() => VisibleReelRetries++;
        internal void RecordVisibleReelTimeout() => VisibleReelTimeouts++;
        internal void RecordAnimationApplication() => AnimationApplicationCount++;
        internal void RecordReadyChargeApplication() => ReadyChargeApplicationCount++;
        internal void RecordRejectedOperation() => RejectedOperationCount++;
    }
}
