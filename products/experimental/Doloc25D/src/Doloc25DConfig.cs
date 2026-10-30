using System;
using System.Runtime.Serialization;

namespace DTMAPI.Doloc25D
{
    [DataContract]
    public sealed class Doloc25DConfig
    {
        [DataMember] public bool Enabled { get; set; } = true;
        [DataMember] public double Yaw { get; set; } = 15;
        [DataMember] public double Pitch { get; set; } = 8;
        [DataMember] public double ViewScale { get; set; } = 1;
        [DataMember] public string ToggleKey { get; set; } = "F8";
        [DataMember] public string PresetKey { get; set; } = "F9";
        [OnDeserializing] private void Defaults(StreamingContext context)
        { Enabled = true; Yaw = 15; Pitch = 8; ViewScale = 1; ToggleKey = "F8"; PresetKey = "F9"; }
        internal void Normalize()
        {
            Yaw = Clamp(Yaw, -35, 35, 15); Pitch = Clamp(Pitch, 0, 15, 8); ViewScale = Clamp(ViewScale, .85, 1.20, 1);
            ToggleKey = ToggleKey ?? ""; PresetKey = PresetKey ?? "";
        }
        private static double Clamp(double v, double min, double max, double fallback) =>
            double.IsNaN(v) || double.IsInfinity(v) ? fallback : Math.Max(min, Math.Min(max, v));
        internal void Preset(int index)
        {
            switch (index % 5)
            {
                case 0: Yaw = 15; Pitch = 8; ViewScale = 1; break;
                case 1: Yaw = 0; Pitch = 0; ViewScale = 1; break;
                case 2: Yaw = -30; Pitch = 10; ViewScale = 1; break;
                case 3: Yaw = 30; Pitch = 10; ViewScale = 1; break;
                case 4: Yaw = 15; Pitch = 8; ViewScale = .85; break;
            }
        }
    }
}
