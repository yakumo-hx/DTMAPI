using System;
using System.Linq;

namespace DTMAPI.MonsterContent
{
    public enum MonsterCompendiumCategory { Organism, Machinery, Bionics, Boss }

    /// <summary>Optional native compendium entry. Defeat unlocks through the native collection
    /// record; viewing never unlocks it. Progress is committed only by native SaveGame.</summary>
    public sealed class MonsterCompendium
    {
        public string Title { get; set; } = "";
        public string Habitat { get; set; } = "";
        public string Description { get; set; } = "";
        public MonsterCompendiumCategory Category { get; set; } = MonsterCompendiumCategory.Organism;
        /// <summary>Show an unknown silhouette before the first defeat.</summary>
        public bool ShowLocked { get; set; } = true;
        /// <summary>Original complete PNG preview. Dimensions and resource cost are authored.</summary>
        public byte[] Png { get; set; } = Array.Empty<byte>();
        internal MonsterCompendium Copy() => new MonsterCompendium {
            Title = Title, Habitat = Habitat, Description = Description, Category = Category,
            ShowLocked = ShowLocked, Png = (byte[])Png.Clone()
        };
    }
    internal static class CompendiumValidation
    {
        internal static MonsterDiagnostic? Validate(string owner, MonsterDefinition definition)
        {
            var c = definition.Compendium;
            if (c == null) return null;
            MonsterDiagnostic Error(string field, string message) => new MonsterDiagnostic("InvalidCompendium", message, owner,
                owner + ":" + definition.LocalId, definition.TemplateId, "Validate", "compendium." + field);
            if (!Text(c.Title, false)) return Error("title", "Expected nonempty plain text.");
            if (!Text(c.Habitat, true)) return Error("habitat", "Expected plain text.");
            if (!Text(c.Description, true)) return Error("description", "Expected plain text.");
            if (!Enum.IsDefined(typeof(MonsterCompendiumCategory), c.Category)) return Error("category", "Select a supported compendium category.");
            var png = c.Png;
            if (png == null || png.Length < 33 || !png.Take(8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}) || png[12] != 73 || png[13] != 72 || png[14] != 68 || png[15] != 82)
                return Error("png", "Expected a PNG with IHDR.");
            long Size(int i) => (long)png[i] << 24 | (long)png[i + 1] << 16 | (long)png[i + 2] << 8 | png[i + 3];
            if (Size(16) < 1 || Size(20) < 1) return Error("png", "Preview dimensions must be positive.");
            return null;
        }
        private static bool Text(string value, bool empty) => value != null && (empty || !string.IsNullOrWhiteSpace(value))
            && !value.Any(c => c == '<' || c == '>' || char.IsControl(c) && c != '\n');
    }
}
