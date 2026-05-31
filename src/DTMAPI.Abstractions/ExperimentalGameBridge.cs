using System;

namespace DTMAPI.Abstractions
{
    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10")]
    public interface IActionCompletionApi
    {
        void Configure(IManifest owner, ActionCompletionOptions options);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10")]
    public interface IFishingAutomationApi
    {
        void Configure(IManifest owner, FishingAutomationOptions options);
        void SetEnabled(IManifest owner, bool enabled, string reason);
        FishingAutomationState GetState(string uniqueId);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.12")]
    public interface IActionSpeedApi
    {
        void Configure(IManifest owner, ActionSpeedOptions options);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10")]
    public interface IItemTooltipApi
    {
        void ConfigureFishRoeProvider(IManifest owner, FishRoeTooltipOptions options, Func<string, FishRoeDisplayInfo?> lookup);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    [DtmApiStatus(DtmApiStatus.Experimental, Since = "0.1.10")]
    public interface IAnimalViewerApi
    {
        void ConfigureSpecialProduceProgress(IManifest owner, AnimalHusbandryProgressOptions options);
        BridgeFeatureStatus GetStatus(string uniqueId);
    }

    public sealed class ActionCompletionOptions
    {
        public bool Enabled { get; set; }
        public bool CompleteTrees { get; set; }
        public bool CompleteOres { get; set; }
        public bool CompleteGarbage { get; set; }
        public bool CompleteWeeds { get; set; }
        public bool CompleteMachineFuel { get; set; }
        public bool CompleteFeeder { get; set; }
        public bool VerboseLogging { get; set; }
    }

    public sealed class FishingAutomationOptions
    {
        public bool AutoRecast { get; set; } = true;
        public bool StopOnManualMove { get; set; } = true;
        public bool RequireSelectedFishingRod { get; set; } = true;
        public double CastReleaseProgress { get; set; }
        public double RecastDelaySeconds { get; set; } = 0.25;
        public bool SkipMiniGame { get; set; }
        public bool InstantBite { get; set; }
        public bool FastAnimations { get; set; }
        public double FastAnimationMultiplier { get; set; } = 3;
        public bool VerboseLogging { get; set; }
    }

    public sealed class ActionSpeedOptions
    {
        public bool Enabled { get; set; }
        public bool ToolSpeedEnabled { get; set; }
        public double ToolMultiplier { get; set; } = 3;
        public bool BottleFillSpeedEnabled { get; set; }
        public double BottleFillMultiplier { get; set; } = 3;
        public bool EatDrinkSpeedEnabled { get; set; }
        public double EatDrinkMultiplier { get; set; } = 3;
        public bool MachineAddSpeedEnabled { get; set; }
        public double MachineAddMultiplier { get; set; } = 3;
        public bool HarvestSpeedEnabled { get; set; }
        public double HarvestMultiplier { get; set; } = 3;
        public bool PlantSpeedEnabled { get; set; }
        public double PlantMultiplier { get; set; } = 3;
        public bool AutoFillBottle { get; set; }
        public bool ContinuousDrinkWithRightClick { get; set; }
        public bool VerboseLogging { get; set; }
    }

    public sealed class FishingAutomationState
    {
        public bool Enabled { get; set; }
        public string Phase { get; set; } = "Idle";
        public string LastReason { get; set; } = string.Empty;
    }

    public sealed class FishRoeTooltipOptions
    {
        public bool Enabled { get; set; } = true;
        public bool LabelFishRoeTitle { get; set; } = true;
        public bool LabelFishRoeDetails { get; set; } = true;
        public int CacheSeconds { get; set; } = 30;
        public bool VerboseLogging { get; set; }
    }

    public sealed class FishRoeDisplayInfo
    {
        public string FishId { get; set; } = string.Empty;
        public string FishTitle { get; set; } = string.Empty;
        public string RoeTitle { get; set; } = string.Empty;
        public string IncubateText { get; set; } = string.Empty;
        public string GrowText { get; set; } = string.Empty;
        public string ParentSummary { get; set; } = string.Empty;
    }

    public sealed class AnimalHusbandryProgressOptions
    {
        public bool Enabled { get; set; } = true;
        public string ProgressLabel { get; set; } = "隐藏产物";
        public DtmColor ProgressColor { get; set; } = new DtmColor(1, 0.58, 0.18, 1);
        public int CacheSeconds { get; set; } = 10;
        public bool VerboseLogging { get; set; }
    }

    public sealed class BridgeFeatureStatus
    {
        public BridgeFeatureStatus(string status, string details)
        {
            Status = string.IsNullOrWhiteSpace(status) ? "unknown" : status;
            Details = details ?? string.Empty;
        }

        public string Status { get; }
        public string Details { get; }
    }

    public struct DtmColor
    {
        public DtmColor(double r, double g, double b, double a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public double R { get; }
        public double G { get; }
        public double B { get; }
        public double A { get; }
    }
}
