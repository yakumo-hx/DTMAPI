using System;

namespace DTMAPI.GameBridge.DolocTown
{
    internal enum AutoFishingPerformanceFixtureUpdate
    {
        None,
        MeasurementStarted,
        MeasurementCompleted
    }

    internal interface IAutoFishingPerformanceFixture
    {
        bool Enabled { get; }

        string Profile { get; }

        bool TerminalSucceeded { get; }

        AutoFishingPerformanceFixtureUpdate Observe(AutoFishingPerformanceFixtureSnapshot snapshot);

        void MarkMovementCancellation(string source);

        void MarkBatch5DisableRecovery(int nativeRecoveryUnits, string source);

        void MarkBatch5TitleReloadCycle(int saveLoadCount, bool reenabledAfterReload, bool disabledAfterReload, string source);

        void RecordBatch5NativeVitals(AutoFishingNativeVitalsReceipt receipt);

        bool CompleteTitleCleanup(AutoFishingPerformanceTitleCleanupSnapshot snapshot);
    }

    internal struct AutoFishingPerformanceFixtureSnapshot
    {
        internal DateTimeOffset ObservedAtUtc { get; set; }
        internal long PullExited { get; set; }
        internal int AccessorBuilds { get; set; }
        internal int AccessorRebuilds { get; set; }
        internal int AccessorBuildFailures { get; set; }
        internal int AccessorInvocationFailures { get; set; }
        internal long Casts { get; set; }
        internal long Fish { get; set; }
        internal int LegacyOptions { get; set; }
        internal int LegacyStates { get; set; }
        internal int Sessions { get; set; }
        internal int InputLeases { get; set; }
        internal int AnimationLeases { get; set; }
        internal int HookRuntimes { get; set; }
        internal int NativeTransient { get; set; }
        internal int NativeReferences { get; set; }
        internal bool SelectedRod { get; set; }
        internal bool HorizontalMoveFactorAvailable { get; set; }
        internal bool SchedulerPending { get; set; }
        internal long VisibleReelQueued { get; set; }
        internal long VisibleReelConsumed { get; set; }
        internal long VisibleReelNativeAccepted { get; set; }
        internal long VisibleReelRetries { get; set; }
        internal long VisibleReelTimeouts { get; set; }
        internal long NativeTryCastEnergyGateUnavailable { get; set; }
        internal long NativeTryCastInsufficientEnergy { get; set; }
    }

    internal sealed class AutoFishingPerformanceTitleCleanupSnapshot
    {
        internal int NativeTransient { get; set; }
        internal int Sessions { get; set; }
        internal int InputLeases { get; set; }
        internal int AnimationLeases { get; set; }
        internal bool SchedulerPending { get; set; }
        internal int LegacyOptions { get; set; }
        internal int LegacyStates { get; set; }
        internal int HookRuntimes { get; set; }
        internal int NativeReferences { get; set; }
        internal int VisibleReelPending { get; set; }
        internal bool CallbackRuntime { get; set; }
        internal long NativeTryCastEnergyGateUnavailable { get; set; }
        internal long NativeTryCastInsufficientEnergy { get; set; }
    }
}
