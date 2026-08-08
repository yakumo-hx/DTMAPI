using System;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class Batch5GcLadderFixtureOptions
    {
        internal bool Enabled { get; set; }
        internal string Domain { get; set; } = "None";
        internal string Level { get; set; } = string.Empty;
        internal string Workload { get; set; } = string.Empty;
        internal double Multiplier { get; set; } = 1d;
        internal int TargetUnits { get; set; }
        internal int MeasureSeconds { get; set; }
        internal int SaveSlot { get; set; }
    }

    internal sealed class Batch5GcActiveWindow
    {
        private readonly TimeSpan requiredDuration;
        private readonly int targetUnits;
        private readonly int scheduledUnits;
        private readonly long intervalTicks;

        internal Batch5GcActiveWindow(TimeSpan requiredDuration, int targetUnits)
        {
            if (requiredDuration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(requiredDuration));
            if (targetUnits <= 0)
                throw new ArgumentOutOfRangeException(nameof(targetUnits));

            this.requiredDuration = requiredDuration;
            this.targetUnits = targetUnits;
            scheduledUnits = Math.Max(2, targetUnits);
            intervalTicks = Math.Max(1L, requiredDuration.Ticks / (scheduledUnits - 1));
        }

        internal int CompletedUnits { get; private set; }

        internal DateTimeOffset? FirstUnitAtUtc { get; private set; }

        internal DateTimeOffset? LastUnitAtUtc { get; private set; }

        internal DateTimeOffset? NextUnitAtUtc { get; private set; }

        internal double ActiveDurationSeconds => FirstUnitAtUtc.HasValue && LastUnitAtUtc.HasValue
            ? Math.Max(0d, (LastUnitAtUtc.Value - FirstUnitAtUtc.Value).TotalSeconds)
            : 0d;

        internal bool IsSatisfied =>
            CompletedUnits >= targetUnits &&
            FirstUnitAtUtc.HasValue &&
            LastUnitAtUtc.HasValue &&
            LastUnitAtUtc.Value - FirstUnitAtUtc.Value >= requiredDuration;

        internal bool IsUnitDue(DateTimeOffset nowUtc)
        {
            if (IsSatisfied)
                return false;
            return !NextUnitAtUtc.HasValue || nowUtc >= NextUnitAtUtc.Value;
        }

        internal void RecordUnit(DateTimeOffset completedAtUtc)
        {
            if (LastUnitAtUtc.HasValue && completedAtUtc < LastUnitAtUtc.Value)
                throw new InvalidOperationException("Batch 5 GC active-workload receipts must be monotonic.");

            if (!FirstUnitAtUtc.HasValue)
                FirstUnitAtUtc = completedAtUtc;
            LastUnitAtUtc = completedAtUtc;
            CompletedUnits++;

            if (IsSatisfied)
            {
                NextUnitAtUtc = null;
                return;
            }

            int nextOrdinal = Math.Min(scheduledUnits - 1, CompletedUnits);
            long offsetTicks = nextOrdinal >= scheduledUnits - 1
                ? requiredDuration.Ticks
                : Math.Min(requiredDuration.Ticks, intervalTicks * nextOrdinal);
            NextUnitAtUtc = FirstUnitAtUtc.Value.AddTicks(offsetTicks);
        }
    }

    internal sealed class ActionSpeedGcLadderProgress
    {
        internal string Level { get; set; } = string.Empty;
        internal string Workload { get; set; } = string.Empty;
        internal DateTimeOffset ObservedAtUtc { get; set; }
        internal int CompletedUnits { get; set; }
        internal DateTimeOffset? FirstUnitAtUtc { get; set; }
        internal DateTimeOffset? LastUnitAtUtc { get; set; }
        internal double ActiveDurationSeconds { get; set; }
        internal bool ActiveWindowSatisfied { get; set; }
        internal bool RecoveryVerified { get; set; }
        internal int RecoveryUnits { get; set; }
        internal string BehaviorReceiptKind { get; set; } = string.Empty;
        internal bool FixtureCompleted { get; set; }
    }

    internal static class Batch5GcLadderBehavior
    {
        internal static string ExpectedActionSpeedReceiptKind(string level)
        {
            switch (level)
            {
                case "L0": return "native-control-active-window";
                case "L1": return "enabled-1x-active-window";
                case "L2": return "common-multiplier-active-window";
                case "L3": return "high-multiplier-active-window";
                case "L4": return "disable-recovery-after-active-window";
                case "L5": return "title-cycle-after-active-window";
                default: throw new ArgumentOutOfRangeException(nameof(level), level, "Batch 5 GC ladder level must be L0-L5.");
            }
        }
    }
}
