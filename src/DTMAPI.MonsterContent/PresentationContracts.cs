using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MonsterContent
{
    public enum MonsterHealthBar { Damaged, Always, Never }
    public sealed class MonsterDisplay
    {
        public string Name { get; set; } = "";
        public bool Nameplate { get; set; } = true;
        public bool BossBar { get; set; }
        public MonsterHealthBar HealthBar { get; set; } = MonsterHealthBar.Damaged;
        internal MonsterDisplay Copy() => new MonsterDisplay { Name = Name, Nameplate = Nameplate, HealthBar = HealthBar, BossBar = BossBar };
    }
    public sealed class MonsterAppearance
    {
        /// <summary>Visual geometry multiplier only; null inherits. Does not scale physics or attack anchors.</summary>
        public float? Scale { get; set; }
        public string Tint { get; set; } = "#FFFFFF";
        public MonsterSkin? Skin { get; set; }
        internal MonsterAppearance Copy() => new MonsterAppearance { Scale = Scale, Tint = Tint, Skin = Skin?.Copy() };
    }
    /// <summary>Optional root-renderer frame mapping. Rectangles use PNG top-left coordinates.
    /// Native pivot and pixels-per-unit are preserved; dimensions are authored. Missing frames show no image.</summary>
    public sealed class MonsterSkin
    {
        public string TemplateFingerprint { get; set; } = "";
        public byte[] Png { get; set; } = Array.Empty<byte>();
        public List<MonsterFrameMapping> Frames { get; set; } = new List<MonsterFrameMapping>();
        internal MonsterSkin Copy() => new MonsterSkin { TemplateFingerprint = TemplateFingerprint, Png = (byte[])Png.Clone(), Frames = Frames.Select(f => f.Copy()).ToList() };
    }
    public sealed class MonsterFrameMapping
    {
        public string FrameId { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        internal MonsterFrameMapping Copy() => new MonsterFrameMapping { FrameId = FrameId, X = X, Y = Y, Width = Width, Height = Height };
    }
    public sealed class MonsterFrameInfo
    {
        public MonsterFrameInfo(string id, int width, int height, float pivotX, float pivotY, float pixelsPerUnit)
        { Id = id; Width = width; Height = height; PivotX = pivotX; PivotY = pivotY; PixelsPerUnit = pixelsPerUnit; }
        public string Id { get; }
        public int Width { get; }
        public int Height { get; }
        public float PivotX { get; }
        public float PivotY { get; }
        public float PixelsPerUnit { get; }
    }
    public sealed class MonsterPresentationInfo
    {
        public MonsterPresentationInfo(string fingerprint, IEnumerable<MonsterFrameInfo> frames, string limitation = "")
        { Fingerprint = fingerprint; Frames = frames.ToList().AsReadOnly(); Limitation = limitation; }
        public string Fingerprint { get; }
        public IReadOnlyList<MonsterFrameInfo> Frames { get; }
        public string Limitation { get; }
        public bool CanMapAllFrames => Fingerprint.Length != 0 && Frames.Count != 0 && Limitation.Length == 0;
    }
    internal static class PresentationValidation
    {
        internal static MonsterDiagnostic? Validate(string owner, MonsterDefinition definition)
        {
            MonsterDiagnostic Error(string field, string message) => new MonsterDiagnostic("InvalidPresentation", message, owner, owner + ":" + definition.LocalId, definition.TemplateId, "Validate", field, "Correct the presentation input.");
            var display = definition.Display;
            if (display == null || display.Name == null || !Enum.IsDefined(typeof(MonsterHealthBar), display.HealthBar)) return Error("display", "Display requires a name and a defined health-bar mode.");
            var appearance = definition.Appearance;
            if (appearance == null || appearance.Tint == null || appearance.Tint.Length != 7 || appearance.Tint[0] != '#' || !appearance.Tint.Skip(1).All(Uri.IsHexDigit)) return Error("appearance.tint", "Tint must be #RRGGBB.");
            if (appearance.Scale.HasValue && (!ContentValidation.Finite(appearance.Scale.Value) || appearance.Scale <= 0)) return Error("appearance.scale", "Expected a positive finite visual multiplier.");
            return ValidateSkin(appearance.Skin, Error, "appearance.skin.");
        }
        internal static MonsterDiagnostic? ValidateSkin(MonsterSkin? skin, Func<string, string, MonsterDiagnostic> error, string prefix)
        {
            MonsterDiagnostic Error(string field, string message) => error(prefix + field, message);
            if (skin == null) return null;
            if (skin.Png == null || skin.Png.Length < 33 || !skin.Png.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) || skin.Png[12] != 73 || skin.Png[13] != 72 || skin.Png[14] != 68 || skin.Png[15] != 82) return Error("png", "Expected PNG with IHDR.");
            long Read(int offset) => (long)skin.Png[offset] << 24 | (long)skin.Png[offset + 1] << 16 | (long)skin.Png[offset + 2] << 8 | skin.Png[offset + 3];
            long width = Read(16), height = Read(20);
            if (width < 1 || height < 1) return Error("png.dimensions", "PNG dimensions must be positive, got " + width + "x" + height + ".");
            if (skin.Frames == null) return Error("frames", "Expected a frame mapping list; missing template frames show no image.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var f in skin.Frames)
            {
                if (f == null || string.IsNullOrEmpty(f.FrameId)) return Error("frames", "Frame ID is required.");
                if (!ids.Add(f.FrameId)) return Error("frames." + f.FrameId, "Duplicate frame: " + f.FrameId);
                if (f.X < 0 || f.Y < 0 || f.Width <= 0 || f.Height <= 0)
                    return Error("frames." + f.FrameId, "Frame rectangles require nonnegative coordinates and positive dimensions.");
            }
            return null;
        }
    }
}
