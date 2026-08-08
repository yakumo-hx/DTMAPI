using System;
using System.IO;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    /// <summary>
    /// Product-owned QA inputs. They are intentionally outside the Author SDK
    /// production source root and are not consumed by the generic GameBridge QA host.
    /// </summary>
    internal sealed class AutoFishingQaSettings
    {
        internal const string LiveHostStatus = "not-wired-after-product-rehome";

        internal bool AutoFishingPerformanceEnabled { get; set; }
        internal string AutoFishingPerformanceProfile { get; set; } = "FishLoop";
        internal int AutoFishingPerformanceTargetFish { get; set; }
        internal int AutoFishingPerformanceWarmupFish { get; set; } = 5;
        internal int AutoFishingPerformanceZeroWarmupSeconds { get; set; } = 60;
        internal int AutoFishingPerformanceZeroMeasureSeconds { get; set; } = 600;
        internal int AutoFishingPerformanceWarmupFrames { get; set; }
        internal int AutoFishingPerformanceTargetFrames { get; set; }
        internal bool Batch5GcLadderEnabled { get; set; }
        internal string Batch5GcLadderDomain { get; set; } = "None";
        internal string Batch5GcLadderLevel { get; set; } = string.Empty;
        internal string Batch5GcLadderWorkload { get; set; } = string.Empty;
        internal double Batch5GcLadderMultiplier { get; set; } = 1d;
        internal int Batch5GcLadderMeasureSeconds { get; set; } = 600;
        internal int Batch5GcLadderSampleSeconds { get; set; } = 30;
        internal int Batch5GcLadderTargetUnits { get; set; }
        internal string G6AutoFishingScenario { get; set; } = "DefaultLoop";
        internal int SaveSlot { get; set; } = 5;

        internal void NormalizeAndValidate()
        {
            string profile = (AutoFishingPerformanceProfile ?? string.Empty).Trim();
            bool fishLoop = profile.Equals("FishLoop", StringComparison.OrdinalIgnoreCase);
            bool inactiveNoConsumer = profile.Equals("InactiveNoConsumer", StringComparison.OrdinalIgnoreCase);
            bool enabledNoRod = profile.Equals("EnabledNoRod", StringComparison.OrdinalIgnoreCase);
            bool zeroProfile = inactiveNoConsumer || enabledNoRod;
            if (AutoFishingPerformanceEnabled)
            {
                if (!fishLoop && !zeroProfile)
                    throw new InvalidDataException("Unsupported AutoFishing performance profile " + profile + ".");
                if (fishLoop && AutoFishingPerformanceTargetFish <= 0)
                    throw new InvalidDataException("FishLoop performance requires a positive target fish count.");
                if (zeroProfile && AutoFishingPerformanceTargetFish != 0)
                    throw new InvalidDataException(profile + " performance requires target fish count 0.");
                if (AutoFishingPerformanceWarmupFish < 0 || AutoFishingPerformanceZeroWarmupSeconds < 0 || AutoFishingPerformanceZeroMeasureSeconds <= 0)
                    throw new InvalidDataException("AutoFishing performance warm-up or measurement values are invalid.");
                if (AutoFishingPerformanceWarmupFrames < 0 || AutoFishingPerformanceTargetFrames < 0)
                    throw new InvalidDataException("AutoFishing performance frame targets cannot be negative.");
                if (AutoFishingPerformanceTargetFrames > 0 && (!inactiveNoConsumer || AutoFishingPerformanceWarmupFrames <= 0))
                    throw new InvalidDataException("Frame-target performance requires InactiveNoConsumer and a positive warm-up frame count.");
            }

            AutoFishingPerformanceProfile = fishLoop ? "FishLoop" : inactiveNoConsumer ? "InactiveNoConsumer" : "EnabledNoRod";
            Batch5GcLadderDomain = string.IsNullOrWhiteSpace(Batch5GcLadderDomain) ? "None" : Batch5GcLadderDomain.Trim();
            Batch5GcLadderLevel = (Batch5GcLadderLevel ?? string.Empty).Trim().ToUpperInvariant();
            Batch5GcLadderWorkload = (Batch5GcLadderWorkload ?? string.Empty).Trim();
            if (!Batch5GcLadderEnabled)
                return;
            if (!Batch5GcLadderDomain.Equals("AutoFishing", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Product QA GC ladder domain must be AutoFishing.");
            if (SaveSlot != 5)
                throw new InvalidDataException("AutoFishing GC ladder requires save slot 5.");
            if (!Batch5GcLadderWorkload.Equals("FishLoop", StringComparison.Ordinal))
                throw new InvalidDataException("AutoFishing GC ladder workload must be FishLoop.");
            if (AutoFishingPerformanceTargetFish != Batch5GcLadderTargetUnits)
                throw new InvalidDataException("AutoFishing GC ladder target units must match the performance target fish count.");
            if (Batch5GcLadderMeasureSeconds <= 0 || Batch5GcLadderSampleSeconds <= 0 || Batch5GcLadderSampleSeconds > Batch5GcLadderMeasureSeconds)
                throw new InvalidDataException("AutoFishing GC ladder sampling durations are invalid.");
            Batch5GcLadderDomain = "AutoFishing";
        }
    }
}
