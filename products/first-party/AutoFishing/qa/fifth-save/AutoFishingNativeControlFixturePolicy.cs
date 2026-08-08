using System;

namespace DTMAPI.GameBridge.DolocTown
{
    internal enum AutoFishingNativeControlFixtureAction
    {
        WaitForNormalState,
        AcquireSession,
        Observe,
        Cast,
        ReelVisible
    }

    internal static class AutoFishingNativeControlFixturePolicy
    {
        internal static AutoFishingNativeControlFixtureAction Decide(
            bool isNormalState,
            bool hasLiveSession,
            string phase,
            bool castAllowed,
            bool castDue,
            bool actionSequenceAlreadyHandled)
        {
            if (!hasLiveSession)
                return isNormalState
                    ? AutoFishingNativeControlFixtureAction.AcquireSession
                    : AutoFishingNativeControlFixtureAction.WaitForNormalState;

            if (actionSequenceAlreadyHandled)
                return AutoFishingNativeControlFixtureAction.Observe;

            if (string.Equals(phase, "BiteReady", StringComparison.Ordinal))
                return AutoFishingNativeControlFixtureAction.ReelVisible;

            bool castablePhase = string.Equals(phase, "Idle", StringComparison.Ordinal) ||
                string.Equals(phase, "PullExited", StringComparison.Ordinal) ||
                string.Equals(phase, "Interrupted", StringComparison.Ordinal);
            if (castAllowed && castDue && castablePhase && isNormalState)
                return AutoFishingNativeControlFixtureAction.Cast;

            return AutoFishingNativeControlFixtureAction.Observe;
        }

        internal static bool HasProgress(string previousPhase, long previousSequence, string phase, long sequence) =>
            !string.Equals(previousPhase ?? string.Empty, phase ?? string.Empty, StringComparison.Ordinal) ||
            previousSequence != sequence;

        internal static bool IsStalled(DateTimeOffset lastProgressAtUtc, DateTimeOffset nowUtc, TimeSpan timeout) =>
            lastProgressAtUtc != DateTimeOffset.MinValue &&
            nowUtc - lastProgressAtUtc > timeout;

        internal static bool HasVerifiedVisibleReelCycle(
            bool reelApplied,
            long queuedStart,
            long queuedEnd,
            long consumedStart,
            long consumedEnd,
            long nativeAcceptedStart,
            long nativeAcceptedEnd) =>
            reelApplied &&
            queuedEnd - queuedStart >= 1 &&
            consumedEnd - consumedStart >= 1 &&
            nativeAcceptedEnd - nativeAcceptedStart >= 1;

        internal static bool HasSufficientVisibleReelUnits(
            long measuredFish,
            long visibleReelConsumedDelta,
            long visibleReelNativeAcceptedDelta) =>
            measuredFish > 0 &&
            visibleReelConsumedDelta >= measuredFish &&
            visibleReelNativeAcceptedDelta >= measuredFish;
    }
}
