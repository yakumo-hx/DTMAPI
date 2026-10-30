using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MonsterContent
{
    /// <summary>Visual-only grid atlas for a scatter action. Grid indices start at the PNG top left.
    /// Skill windup/recovery own duration; recovery frame zero is displayed by the firing step.
    /// Pivot is normalized from the bottom left. No collider, transform, muzzle or damage events.</summary>
    public sealed class MonsterActionAnimation
    {
        public byte[] Png { get; set; } = Array.Empty<byte>();
        public int FrameWidth { get; set; } = 32;
        public int FrameHeight { get; set; } = 32;
        public float PivotX { get; set; } = .5f;
        public float PivotY { get; set; } = .5f;
        public float PixelsPerUnit { get; set; } = 16;
        public List<int> WindupFrames { get; set; } = new List<int>();
        public List<int> RecoveryFrames { get; set; } = new List<int>();
        internal MonsterActionAnimation Copy() => new MonsterActionAnimation {
            Png = (byte[])Png.Clone(), FrameWidth = FrameWidth, FrameHeight = FrameHeight,
            PivotX = PivotX, PivotY = PivotY, PixelsPerUnit = PixelsPerUnit,
            WindupFrames = WindupFrames.ToList(), RecoveryFrames = RecoveryFrames.ToList()
        };
        internal int FrameAt(float elapsed, MonsterScatter timing, bool fired, float firedAt)
        {
            var frames = fired ? RecoveryFrames : WindupFrames;
            float phaseTime = fired ? elapsed - firedAt : elapsed;
            float duration = fired ? timing.RecoveryGameSeconds : timing.WindupGameSeconds;
            int index = (int)(Math.Max(0, Math.Min(1, phaseTime / duration)) * frames.Count);
            return frames[Math.Min(frames.Count - 1, index)];
        }
    }
    internal static class ActionAnimationValidation
    {
        internal static MonsterDiagnostic? Validate(string owner, MonsterDefinition definition, MonsterSkill skill, string field)
        {
            var a = skill.Animation;
            if (a == null) return null;
            MonsterDiagnostic Error(string suffix, string message) => new MonsterDiagnostic("InvalidActionAnimation", message, owner, owner + ":" + definition.LocalId, definition.TemplateId, "Validate", field + ".animation" + suffix);
            if (skill.Kind != "scatter" || skill.Scatter == null) return Error("", "This candidate animates scatter only.");
            if (!(skill.Scatter.WindupGameSeconds > 0) || !(skill.Scatter.RecoveryGameSeconds > 0)) return Error("", "Animated scatter needs positive windup and recovery durations; those skill parameters own timing.");
            if (a.Png == null || a.Png.Length < 33 || !a.Png.Take(8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) || a.Png[12] != 73 || a.Png[13] != 72 || a.Png[14] != 68 || a.Png[15] != 82) return Error(".png", "Expected a PNG with IHDR.");
            long Size(int i) => (long)a.Png[i] << 24 | (long)a.Png[i+1] << 16 | (long)a.Png[i+2] << 8 | a.Png[i+3];
            long w = Size(16), h = Size(20);
            if (w < 1 || h < 1) return Error(".png", "PNG dimensions must be positive.");
            if (a.FrameWidth < 1 || a.FrameHeight < 1 || w % a.FrameWidth != 0 || h % a.FrameHeight != 0 || a.FrameWidth > w || a.FrameHeight > h) return Error(".frameWidth", "Frame dimensions must divide the atlas exactly.");
            if (!In(a.PivotX, 0, 1) || !In(a.PivotY, 0, 1)) return Error(".pivotX", "Normalized pivot must be in [0, 1].");
            if (!ContentValidation.Finite(a.PixelsPerUnit) || a.PixelsPerUnit <= 0) return Error(".pixelsPerUnit", "Pixels per world unit must be positive and finite.");
            long count = (w / a.FrameWidth) * (h / a.FrameHeight);
            foreach (var pair in new[] { (".windupFrames", a.WindupFrames), (".recoveryFrames", a.RecoveryFrames) })
                if (pair.Item2 == null || pair.Item2.Count < 1 || pair.Item2.Any(i => i < 0 || i >= count)) return Error(pair.Item1, "Expected a nonempty list of in-bounds grid indices.");
            return null;
        }
        private static bool In(float n, float min, float max) => ContentValidation.Finite(n) && n >= min && n <= max;
    }
}
