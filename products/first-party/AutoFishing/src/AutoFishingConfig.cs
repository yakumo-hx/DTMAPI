using System;

namespace Yuuka.DTMAPI.AutoFishing
{
    public sealed class AutoFishingConfig
    {
        public const float MaximumAnimationMultiplier = 5f;
        public string ToggleKey { get; set; } = "F6";
        public bool InstantBite { get; set; }
        public bool SkipMiniGame { get; set; }
        public bool FastAnimations { get; set; }
        public float CastChargeRatio { get; set; }
        public float AnimationMultiplier { get; set; } = 3f;

        public void Normalize()
        {
            ToggleKey = ToggleKey ?? "F6";
            CastChargeRatio = Clamp(CastChargeRatio, 0f, 1f, 0f);
            AnimationMultiplier = Clamp(AnimationMultiplier, 1f, MaximumAnimationMultiplier, 3f);
        }

        private static float Clamp(float value, float min, float max, float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));
    }
}
