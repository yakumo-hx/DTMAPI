using System;

namespace Yuuka.DTMAPI.AutoFishing
{
    internal enum FishingVisibleReelFrameResult
    {
        None,
        Retried,
        TimedOut
    }

    internal sealed class FishingVisibleReelInputState
    {
        private static readonly TimeSpan EdgeLifetime = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan ConfirmationLifetime = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan TotalLifetime = TimeSpan.FromSeconds(5);
        private readonly Func<DateTimeOffset> clock;
        private object? waitState;
        private DateTimeOffset startedAtUtc;
        private DateTimeOffset edgeExpiresAtUtc;
        private DateTimeOffset consumedAtUtc;
        private bool edgeArmed;
        private bool awaitingConfirmation;

        internal FishingVisibleReelInputState(Func<DateTimeOffset>? clock = null)
        {
            this.clock = clock ?? (() => DateTimeOffset.UtcNow);
        }

        internal bool IsPending => waitState != null;
        internal object? WaitState => waitState;
        internal bool Queue(object state, out bool newlyQueued)
        {
            newlyQueued = false;
            if (state == null)
                return false;
            DateTimeOffset now = clock();
            if (ReferenceEquals(waitState, state) && IsPending)
                return true;
            waitState = state;
            startedAtUtc = now;
            consumedAtUtc = DateTimeOffset.MinValue;
            awaitingConfirmation = false;
            Arm(now);
            newlyQueued = true;
            return true;
        }

        internal FishingVisibleReelFrameResult OnNativeFrame(object? activeWaitState, bool biteReady)
        {
            if (!IsPending)
                return FishingVisibleReelFrameResult.None;
            if (!biteReady || activeWaitState == null || !ReferenceEquals(waitState, activeWaitState))
            {
                Clear();
                return FishingVisibleReelFrameResult.None;
            }

            DateTimeOffset now = clock();
            if (now - startedAtUtc >= TotalLifetime)
            {
                Clear();
                return FishingVisibleReelFrameResult.TimedOut;
            }

            if (edgeArmed)
            {
                if (now <= edgeExpiresAtUtc)
                    return FishingVisibleReelFrameResult.None;
                Arm(now);
                return FishingVisibleReelFrameResult.Retried;
            }

            if (awaitingConfirmation && now - consumedAtUtc >= ConfirmationLifetime)
            {
                awaitingConfirmation = false;
                Arm(now);
                return FishingVisibleReelFrameResult.Retried;
            }
            return FishingVisibleReelFrameResult.None;
        }

        internal bool TryConsume(string inputName, out bool value)
        {
            value = false;
            if (!edgeArmed || !IsVisibleReelInput(inputName))
                return false;
            DateTimeOffset now = clock();
            if (now > edgeExpiresAtUtc)
                return false;
            edgeArmed = false;
            awaitingConfirmation = true;
            consumedAtUtc = now;
            value = true;
            return true;
        }

        internal bool TryConfirmNativeEffectEntered(object state, object? nextState)
        {
            if (!awaitingConfirmation || waitState == null || !ReferenceEquals(waitState, state) || nextState == null || ReferenceEquals(state, nextState))
                return false;
            Clear();
            return true;
        }

        internal void Clear()
        {
            waitState = null;
            startedAtUtc = DateTimeOffset.MinValue;
            edgeExpiresAtUtc = DateTimeOffset.MinValue;
            consumedAtUtc = DateTimeOffset.MinValue;
            edgeArmed = false;
            awaitingConfirmation = false;
        }

        private void Arm(DateTimeOffset now)
        {
            edgeArmed = true;
            edgeExpiresAtUtc = now + EdgeLifetime;
        }

        private static bool IsVisibleReelInput(string inputName)
        {
            return inputName.Equals("NormalUseTool", StringComparison.OrdinalIgnoreCase) ||
                inputName.Equals("NormalUseItem", StringComparison.OrdinalIgnoreCase) ||
                inputName.Equals("NormalFishing", StringComparison.OrdinalIgnoreCase);
        }
    }
}
