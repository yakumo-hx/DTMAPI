using System;
using System.Runtime.Serialization;

namespace Yuuka.DTMAPI.ActionSpeed
{
    [DataContract]
    internal sealed class ActionSpeedConfig
    {
        [DataMember] public bool Enabled { get; set; }
        [DataMember] public string Profile { get; set; } = "Safe";
        [DataMember] public bool ToolSpeedEnabled { get; set; }
        [DataMember] public double ToolMultiplier { get; set; } = 3;
        [DataMember] public bool BottleFillSpeedEnabled { get; set; }
        [DataMember] public double BottleFillMultiplier { get; set; } = 3;
        [DataMember] public bool EatDrinkSpeedEnabled { get; set; }
        [DataMember] public double EatDrinkMultiplier { get; set; } = 3;
        [DataMember] public bool MachineAddSpeedEnabled { get; set; }
        [DataMember] public double MachineAddMultiplier { get; set; } = 3;
        [DataMember] public bool HarvestSpeedEnabled { get; set; }
        [DataMember] public double HarvestMultiplier { get; set; } = 3;
        [DataMember] public bool PlantSpeedEnabled { get; set; }
        [DataMember] public double PlantMultiplier { get; set; } = 3;
        [DataMember] public bool AutoFillBottle { get; set; }
        [DataMember] public bool AutoFillStrong { get; set; }
        [DataMember] public bool ContinuousDrinkWithRightClick { get; set; }
        [DataMember] public string ContinuousDrinkHoldKey { get; set; } = "None";
        [DataMember] public string MenuKey { get; set; } = "F10";
        [DataMember] public double AutoActionCooldownSeconds { get; set; } = 0.25;
        [DataMember] public string DebugLabel { get; set; } = "DTMAPI";

        internal void Normalize()
        {
            Profile = NormalizeChoice(Profile, "Safe", "Safe", "Fast", "Debug");
            ToolMultiplier = Clamp(ToolMultiplier, 1, 4);
            BottleFillMultiplier = Clamp(BottleFillMultiplier, 1, 4);
            EatDrinkMultiplier = Clamp(EatDrinkMultiplier, 1, 4);
            MachineAddMultiplier = Clamp(MachineAddMultiplier, 1, 4);
            HarvestMultiplier = Clamp(HarvestMultiplier, 1, 4);
            PlantMultiplier = Clamp(PlantMultiplier, 1, 4);
            AutoActionCooldownSeconds = Clamp(AutoActionCooldownSeconds, 0.05, 5);
            if (!AutoFillBottle)
                AutoFillStrong = false;
            MenuKey = NormalizeKey(MenuKey);
            ContinuousDrinkHoldKey = NormalizeKey(ContinuousDrinkHoldKey);
            DebugLabel = DebugLabel ?? string.Empty;
        }

        internal double ResolveAutoFillCooldownSeconds()
        {
            if (Profile.Equals("Debug", StringComparison.OrdinalIgnoreCase))
                return 0.1;
            if (Profile.Equals("Fast", StringComparison.OrdinalIgnoreCase))
                return 0.18;
            return AutoActionCooldownSeconds;
        }

        internal double ResolveAutoFillStrongCooldownSeconds()
        {
            double normal = ResolveAutoFillCooldownSeconds();
            double profileTarget = Profile.Equals("Safe", StringComparison.OrdinalIgnoreCase) ? 0.12 : 0.08;
            return Math.Min(normal, profileTarget);
        }

        private static string NormalizeKey(string value)
        {
            value = (value ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(value) ? "None" : value;
        }

        private static string NormalizeChoice(string value, string fallback, params string[] allowed)
        {
            foreach (string option in allowed)
            {
                if (option.Equals(value, StringComparison.OrdinalIgnoreCase))
                    return option;
            }
            return fallback;
        }

        private static double Clamp(double value, double min, double max) => Math.Min(max, Math.Max(min, value));
    }

    internal sealed class ActionSpeedPolicy
    {
        internal ActionSpeedPolicy() { }

        internal ActionSpeedPolicy(ActionSpeedConfig config)
        {
            Enabled = config.Enabled;
            ToolSpeedEnabled = config.ToolSpeedEnabled;
            ToolMultiplier = config.ToolMultiplier;
            BottleFillSpeedEnabled = config.BottleFillSpeedEnabled;
            BottleFillMultiplier = config.BottleFillMultiplier;
            EatDrinkSpeedEnabled = config.EatDrinkSpeedEnabled;
            EatDrinkMultiplier = config.EatDrinkMultiplier;
            MachineAddSpeedEnabled = config.MachineAddSpeedEnabled;
            MachineAddMultiplier = config.MachineAddMultiplier;
            HarvestSpeedEnabled = config.HarvestSpeedEnabled;
            HarvestMultiplier = config.HarvestMultiplier;
            PlantSpeedEnabled = config.PlantSpeedEnabled;
            PlantMultiplier = config.PlantMultiplier;
            AutoFillBottle = config.AutoFillBottle;
            AutoFillStrong = config.AutoFillStrong;
            AutoFillCooldownSeconds = config.ResolveAutoFillCooldownSeconds();
            AutoFillStrongCooldownSeconds = config.ResolveAutoFillStrongCooldownSeconds();
            ContinuousDrinkWithRightClick = config.ContinuousDrinkWithRightClick;
            VerboseLogging = config.Profile.Equals("Debug", StringComparison.OrdinalIgnoreCase);
        }

        internal bool Enabled { get; set; }
        internal bool ToolSpeedEnabled { get; set; }
        internal double ToolMultiplier { get; set; } = 1;
        internal bool BottleFillSpeedEnabled { get; set; }
        internal double BottleFillMultiplier { get; set; } = 1;
        internal bool EatDrinkSpeedEnabled { get; set; }
        internal double EatDrinkMultiplier { get; set; } = 1;
        internal bool MachineAddSpeedEnabled { get; set; }
        internal double MachineAddMultiplier { get; set; } = 1;
        internal bool HarvestSpeedEnabled { get; set; }
        internal double HarvestMultiplier { get; set; } = 1;
        internal bool PlantSpeedEnabled { get; set; }
        internal double PlantMultiplier { get; set; } = 1;
        internal bool AutoFillBottle { get; set; }
        internal bool AutoFillStrong { get; set; }
        internal double AutoFillCooldownSeconds { get; set; } = 0.25;
        internal double AutoFillStrongCooldownSeconds { get; set; } = 0.12;
        internal bool ContinuousDrinkWithRightClick { get; set; }
        internal bool VerboseLogging { get; set; }
    }
}
