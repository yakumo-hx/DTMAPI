using System;
using System.Collections.Generic;

namespace DTMAPI.GameBridge.DolocTown
{
    internal enum FishingCompatibilityLifecycleEvent
    {
        Other,
        Configure,
        Enabled,
        Disabled,
        MiniGameStop,
        NativeExit,
        SaveLoaded,
        ReturnedToTitle,
        OwnerCleanup,
        PendingCastWatchdog,
        EnvironmentReset
    }

    internal sealed class FishingCompatibilityLifecyclePublicationGate
    {
        private static readonly TimeSpan SummaryInterval = TimeSpan.FromSeconds(30);
        private readonly Dictionary<FishingCompatibilityLifecycleEvent, State> states = new Dictionary<FishingCompatibilityLifecycleEvent, State>();

        internal long ObservedCount { get; private set; }
        internal long PublishedCount { get; private set; }
        internal long SuppressedCount { get; private set; }

        internal bool ShouldPublish(FishingCompatibilityLifecycleEvent kind, bool warning, DateTimeOffset nowUtc, bool hardBoundary)
        {
            ObservedCount++;
            if (!states.TryGetValue(kind, out State state))
            {
                state = new State();
                states[kind] = state;
            }
            state.Observed++;
            bool changed = state.HasPublished && state.LastWarning != warning;
            bool due = state.LastPublishedAtUtc == DateTimeOffset.MinValue || nowUtc - state.LastPublishedAtUtc >= SummaryInterval;
            if (!warning && !hardBoundary && !changed && !due)
            {
                state.Suppressed++;
                SuppressedCount++;
                return false;
            }
            state.HasPublished = true;
            state.LastWarning = warning;
            state.LastPublishedAtUtc = nowUtc;
            state.Published++;
            PublishedCount++;
            return true;
        }

        internal void Reset()
        {
            states.Clear();
            ObservedCount = 0;
            PublishedCount = 0;
            SuppressedCount = 0;
        }

        internal long GetObservedCount(FishingCompatibilityLifecycleEvent kind)
        {
            return states.TryGetValue(kind, out State state) ? state.Observed : 0L;
        }

        private sealed class State
        {
            internal long Observed;
            internal long Published;
            internal long Suppressed;
            internal bool HasPublished;
            internal bool LastWarning;
            internal DateTimeOffset LastPublishedAtUtc;
        }
    }
}
