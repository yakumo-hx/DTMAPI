using System;
using System.Runtime.Serialization;
using global::DTMAPI.Abstractions;

namespace Yuuka.DTMAPI.AutoFishing
{
    [DataContract]
    internal sealed class AutoFishingConfig
    {
        [DataMember] public string ToggleKey { get; set; } = "F6";
        [DataMember] public bool SkipMiniGame { get; set; }
        [DataMember] public bool InstantBite { get; set; }
        [DataMember] public bool FastAnimations { get; set; }
        [DataMember] public double AnimationMultiplier { get; set; } = 3;
        [DataMember] public double CastChargeRatio { get; set; }

        internal void Normalize()
        {
            ToggleKey = string.IsNullOrWhiteSpace(ToggleKey) ? "F6" : DtmKeybindList.Parse(ToggleKey).ToString();
            if (double.IsNaN(AnimationMultiplier) || double.IsInfinity(AnimationMultiplier) || AnimationMultiplier <= 0)
                AnimationMultiplier = 3;
            AnimationMultiplier = Math.Min(4, Math.Max(1, AnimationMultiplier));
            if (double.IsNaN(CastChargeRatio) || double.IsInfinity(CastChargeRatio))
                CastChargeRatio = 0;
            CastChargeRatio = Math.Min(1, Math.Max(0, CastChargeRatio));
        }
    }
}
