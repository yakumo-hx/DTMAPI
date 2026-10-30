using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MonsterContent
{
    public sealed class MonsterNativeOverride
    {
        public string LocalId { get; set; } = "";
        public string TargetId { get; set; } = "";
        public int? Defense { get; set; }
        public float? HurtDuration { get; set; }
        /// <summary>Optional #RRGGBB multiplier for the native target's root sprite, applied independently of numeric fields.</summary>
        public string? Tint { get; set; }
        public MonsterSkin? Skin { get; set; }
        /// <summary>Unified native drop policy. All append contributions run; only replacements use official load/declaration order.</summary>
        public MonsterDrops? Drops { get; set; }
        /// <summary>Optional #RRGGBB multiplier for projectiles emitted by this native monster.</summary>
        public string? ProjectileTint { get; set; }
        /// <summary>Owner-relative shared Audio replacements. Each event is one SET field; Humming is consumed at the next native Run.</summary>
        public MonsterSounds? Sounds { get; set; }
        public List<MonsterNativeAttackParameters> NativeAttacks { get; set; } = new List<MonsterNativeAttackParameters>();
        internal MonsterNativeOverride Copy()
        { var copy = (MonsterNativeOverride)MemberwiseClone(); copy.Skin = Skin?.Copy(); copy.Drops = Drops?.Copy(); copy.Sounds = Sounds?.Copy(); copy.NativeAttacks = NativeAttacks.Select(a => a.Copy()).ToList(); return copy; }
    }
    public enum MonsterOverrideState { WaitingNative, Ready, Rejected, Withdrawn, CleanupPending }
    public enum MonsterOverrideFieldState { Effective, Shadowed, Inactive }
    public sealed class MonsterOverrideFieldSnapshot
    {
        internal MonsterOverrideFieldSnapshot(string field, double requested, double? baseline, double? effective,
            string winner, MonsterOverrideFieldState state)
        { Field = field; Requested = requested; Baseline = baseline; Effective = effective; WinnerId = winner; State = state; }
        public string Field { get; }
        internal MonsterOverrideFieldSnapshot(string field, string requested, string? effective, string winner, MonsterOverrideFieldState state)
        {
            Field = field; WinnerId = winner; State = state;
            if (field == "tint" || field == "projectileTint") { RequestedTint = requested; EffectiveTint = effective; }
            else { RequestedResource = requested; EffectiveResource = effective; }
        }
        /// <summary>Content identity for skin/drops; path and settings for sounds. WinnerId disambiguates equal resources.</summary>
        public string? RequestedResource { get; }
        public string? EffectiveResource { get; }
        /// <summary>Numeric values are null for colors and resources. A renderer's original color is instance-specific, not a catalog scalar.</summary>
        public double? Requested { get; }
        public double? Baseline { get; }
        public double? Effective { get; }
        public string? RequestedTint { get; }
        public string? EffectiveTint { get; }
        public string WinnerId { get; }
        public MonsterOverrideFieldState State { get; }
    }
    public sealed class MonsterOverrideSnapshot
    {
        internal MonsterOverrideSnapshot(string owner, string id, string target, int loadIndex, long declarationOrder, MonsterOverrideState state,
            long generation, MonsterDiagnostic? diagnostic, IEnumerable<MonsterOverrideFieldSnapshot> fields)
        { OwnerId = owner; Id = id; TargetId = target; LoadIndex = loadIndex; DeclarationOrder = declarationOrder; State = state; Generation = generation; Diagnostic = diagnostic; Fields = Array.AsReadOnly(fields.ToArray()); }
        public string OwnerId { get; }
        public string Id { get; }
        public string TargetId { get; }
        public int LoadIndex { get; }
        public long DeclarationOrder { get; }
        public MonsterOverrideState State { get; }
        public long Generation { get; }
        public MonsterDiagnostic? Diagnostic { get; }
        public IReadOnlyList<MonsterOverrideFieldSnapshot> Fields { get; }
    }
    public sealed class MonsterOverrideLease : IDisposable
    {
        private readonly Func<MonsterOverrideSnapshot> snapshot;
        private readonly MonsterLease lease;
        internal MonsterOverrideLease(Func<MonsterOverrideSnapshot> snapshot, Action release)
        { this.snapshot = snapshot; lease = new MonsterLease(release); }
        public MonsterOverrideSnapshot Snapshot => snapshot();
        public void Dispose() => lease.Dispose();
    }

    public static class MonsterOverrideAdmission
    {
        internal static IEnumerable<string> Fields(MonsterNativeOverride value)
        {
            if (value.Defense.HasValue) yield return "defense";
            if (value.HurtDuration.HasValue) yield return "hurtDuration";
            if (value.Tint != null) yield return "tint";
            if (value.Skin != null) yield return "skin";
            if (value.Drops != null) yield return "drops";
            if (value.ProjectileTint != null) yield return "projectileTint";
            if (value.Sounds?.Hurt != null) yield return "sounds.hurt";
            if (value.Sounds?.Dead != null) yield return "sounds.dead";
            if (value.Sounds?.Humming != null) yield return "sounds.humming";
            if (value.Sounds?.RecoveryWarning != null) yield return "sounds.recoveryWarning";
            foreach (var attack in value.NativeAttacks)
            {
                foreach (var field in NativeAttackValidation.Fields(attack)) yield return NativeAttackValidation.Field(attack.AttackId, field.Key);
                foreach (var sound in NativeAttackValidation.Sounds(attack)) yield return NativeAttackValidation.Field(attack.AttackId, sound.Key);
            }
        }
        internal static bool ValidHurtDuration(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;
        internal static bool ValidTint(string value) => value.Length == 7 && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);
        public static MonsterDiagnostic? Validate(string owner, MonsterNativeOverride? value, MonsterContentCatalog? catalog = null)
        {
            string field = value == null ? "nativeOverrides" : !Hosting.MonsterValidation.ValidId(value.LocalId) ? "id" :
                !(Hosting.MonsterValidation.ValidId(value.TargetId) || Hosting.MonsterValidation.ValidQualifiedId(value.TargetId)) || value.TargetId.StartsWith("dtm_mc_", StringComparison.Ordinal) ? "targetId" :
                value.Defense < 0 ? "fields.defense" :
                value.HurtDuration.HasValue && !ValidHurtDuration(value.HurtDuration.Value) ? "fields.hurtDuration" :
                value.Tint != null && !ValidTint(value.Tint) ? "fields.tint" :
                value.ProjectileTint != null && !ValidTint(value.ProjectileTint) ? "fields.projectileTint" :
                value.NativeAttacks == null ? "fields.nativeAttacks" :
                !value.Defense.HasValue && !value.HurtDuration.HasValue && value.Tint == null && value.Skin == null && value.Drops == null && value.ProjectileTint == null && value.Sounds == null && value.NativeAttacks.Count == 0 ? "fields" : "";
            MonsterDiagnostic Error(string code, string at, string message) => new MonsterDiagnostic(code, message, owner, owner + ":" + value?.LocalId, value?.TargetId ?? "", "NativeOverride", at);
            if (field.Length > 0) return Error("InvalidNativeOverride", field, "Expected a native target and nonnegative defense, finite nonnegative hurtDuration in game seconds, or tint #RRGGBB.");
            bool definitionTarget = Hosting.MonsterValidation.ValidQualifiedId(value!.TargetId);
            if (definitionTarget && Fields(value).Any(f => f != "drops")) return Error("DefinitionDropTargetOnly", "fields", "A qualified definition target accepts only drops.");
            var soundError = SoundValidation.Validate(owner, value!.LocalId, value.TargetId, value.Sounds, "fields.sounds");
            if (soundError != null) return soundError;
            var attacks = NativeAttackValidation.Validate(owner, value!.LocalId, value.TargetId, value.NativeAttacks, nativeOverride: true);
            if (attacks != null) return attacks;
            var skinError = PresentationValidation.ValidateSkin(value!.Skin, (at, message) => Error("InvalidNativeSkin", at, message), "fields.skin.");
            if (skinError != null) return skinError;
            var drops = value.Drops;
            if (drops != null)
            {
                var invalid = ContentValidation.Drops(owner, drops);
                if (invalid != null) return Error(invalid.Code, "fields.drops." + invalid.Field, invalid.Message);
            }
            if (catalog != null)
            {
                var template = catalog.Templates.FirstOrDefault(t => t.Id == value!.TargetId);
                if (!definitionTarget && template == null) return Error("OverrideTargetMissing", "targetId", "Target is absent from this native directory.");
                if (!definitionTarget && Fields(value!).Any(f => !template!.NativeOverrideFields.Contains(NativeAttackValidation.IsAttackField(f) ? "nativeAttacks" : f)))
                    return Error("NativeOverrideUnsupported", "fields", "Refresh the current Host catalog; the target must support the requested native fields.");
                if (!definitionTarget) attacks = NativeAttackValidation.Validate(owner, value!.LocalId, value.TargetId, value.NativeAttacks, template!.Actions.NativeAttacks, nativeOverride: true);
                if (attacks != null) return attacks;
                var mapping = definitionTarget ? null : Hosting.SkinMappingAdmission.Validate(owner, value!.LocalId, value.TargetId, value.Skin, template!.Presentation, "fields.skin.");
                if (mapping != null) return mapping;
                if (drops?.ReferencesPool == true && !catalog.DropPools.Any(p => p.Source == drops.Source && p.Reference.Id == drops.PoolId && p.Reference.CanUse))
                    return Error("DropPoolMissing", "fields.drops.poolId", "Referenced drop pool is absent or unavailable.");
                if (drops?.Entries != null && drops.Entries.Any(e => !catalog.Items.Any(i => i.Id == e.ItemId)))
                    return Error("ItemMissing", "fields.drops.entries", "Additional drops require items in the current directory.");
            }
            return null;
        }
    }
}
