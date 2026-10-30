using System;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class G6FixtureOptions
    {
        internal string[] Cases { get; set; } = Array.Empty<string>();
        internal int SaveSlot { get; set; }
        internal int InitialDelaySeconds { get; set; } = 3;
        internal int SaveLoadCycleCount { get; set; }
        internal int SaveLoadCycleInitialTitleIdleSeconds { get; set; }
        internal int SaveLoadCycleIntervalSeconds { get; set; } = 5;
        internal int SaveLoadCycleInSaveSeconds { get; set; } = 5;
        internal bool PreLoadGcProbe { get; set; }
        internal string SaveLoadObjectSnapshotMode { get; set; } = "Full";
        internal string OwnerRootIsolationProfile { get; set; } = "None";
        internal int PendingPressureSeconds { get; set; }
        internal double PendingPressureIntervalSeconds { get; set; } = 2d;
        internal int LongTitleIdleSeconds { get; set; }
    }
}
