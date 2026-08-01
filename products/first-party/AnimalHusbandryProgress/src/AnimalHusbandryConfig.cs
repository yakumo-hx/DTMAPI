using System;
using System.Runtime.Serialization;

namespace Yuuka.DTMAPI.AnimalHusbandryProgress
{
    [DataContract]
    public sealed class AnimalHusbandryConfig
    {
        [DataMember] public bool Enabled { get; set; } = true;
        [DataMember] public string ColorPreset { get; set; } = "Orange";
        [DataMember] public string ProgressColorHex { get; set; } = "FF942E";
        [DataMember] public int CacheSeconds { get; set; } = 0;
        [DataMember] public bool VerboseLogging { get; set; }

        internal AnimalHusbandryConfig Copy() => new AnimalHusbandryConfig
        {
            Enabled = Enabled,
            ColorPreset = ColorPreset,
            ProgressColorHex = ProgressColorHex,
            CacheSeconds = 0,
            VerboseLogging = VerboseLogging
        };

        internal void Normalize()
        {
            ColorPreset = NormalizeChoice(ColorPreset, "Orange", "Orange", "Green", "Blue", "Pink", "White", "Custom");
            if (!ColorPreset.Equals("Custom", StringComparison.OrdinalIgnoreCase))
                ProgressColorHex = ColorPresetToHex(ColorPreset);
            ProgressColorHex = NormalizeHex(ProgressColorHex);
            CacheSeconds = 0;
        }

        private static string NormalizeChoice(string value, string fallback, params string[] allowed)
        {
            foreach (string option in allowed)
                if (option.Equals(value ?? string.Empty, StringComparison.OrdinalIgnoreCase)) return option;
            return fallback;
        }

        private static string ColorPresetToHex(string preset)
        {
            if (preset.Equals("Green", StringComparison.OrdinalIgnoreCase)) return "70C978";
            if (preset.Equals("Blue", StringComparison.OrdinalIgnoreCase)) return "65A6FF";
            if (preset.Equals("Pink", StringComparison.OrdinalIgnoreCase)) return "FF7AC8";
            if (preset.Equals("White", StringComparison.OrdinalIgnoreCase)) return "F0F0F0";
            return "FF942E";
        }

        internal static string NormalizeHex(string value)
        {
            value = (value ?? string.Empty).Trim().TrimStart('#');
            if (value.Length != 6) return "FF942E";
            for (int i = 0; i < value.Length; i++) if (!Uri.IsHexDigit(value[i])) return "FF942E";
            return value.ToUpperInvariant();
        }
    }
}
