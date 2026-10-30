using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MonsterContent
{
    /// <summary>Parameters for a native part created by this definition's complete encounter. Does not register a standalone monster.</summary>
    public sealed class MonsterNativePartParameters
    {
        public string TemplateId { get; set; } = "";
        public int? Health { get; set; }
        public int? Defense { get; set; }
        public float? HurtDuration { get; set; }
        public float? Scale { get; set; }
        public MonsterNativeParameters? NativeParameters { get; set; }
        public List<MonsterNativeAttackParameters> NativeAttacks { get; set; } = new List<MonsterNativeAttackParameters>();
        public MonsterSounds? Sounds { get; set; }
        internal MonsterNativePartParameters Copy() => new MonsterNativePartParameters {
            TemplateId = TemplateId, Health = Health, Defense = Defense, HurtDuration = HurtDuration, Scale = Scale, NativeParameters = NativeParameters?.Copy(), NativeAttacks = NativeAttacks.Select(a => a.Copy()).ToList(), Sounds = Sounds?.Copy() };
    }
    public sealed class MonsterNativePartInfo
    {
        public MonsterNativePartInfo(string templateId, IEnumerable<MonsterNativeAttackInfo> nativeAttacks, IEnumerable<string>? soundEvents = null)
        { TemplateId = templateId; NativeAttacks = Array.AsReadOnly(nativeAttacks.ToArray()); SoundEvents = Array.AsReadOnly((soundEvents ?? Array.Empty<string>()).ToArray()); }
        public int? Health { get; internal set; }
        public int? Defense { get; internal set; }
        public float? HurtDuration { get; internal set; }
        public MonsterNativeParameters? NativeParameters { get; internal set; }
        public string TemplateId { get; }
        public IReadOnlyList<MonsterNativeAttackInfo> NativeAttacks { get; }
        public IReadOnlyList<string> SoundEvents { get; }
    }
    internal static class NativePartValidation
    {
        internal static MonsterDiagnostic? Validate(string owner, MonsterDefinition definition,
            IReadOnlyList<MonsterNativePartInfo>? catalog = null)
        {
            MonsterDiagnostic Error(string code, string field, string message) => new MonsterDiagnostic(code, message,
                owner, owner + ":" + definition.LocalId, definition.TemplateId, "Validate", field);
            var parts = definition.NativeParts;
            if (parts == null) return Error("InvalidNativePart", "nativeParts", "Expected a list of distinct native encounter parts.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < parts.Count; i++)
            {
                var part = parts[i]; string at = "nativeParts[" + i + "]";
                if (part == null || !MonsterAdmission.ValidLocalId(part.TemplateId) || !ids.Add(part.TemplateId))
                    return Error("InvalidNativePart", at + ".templateId", "Select a distinct native part template.");
                if (part.NativeAttacks == null || part.NativeAttacks.Count == 0 && part.Sounds == null && !part.Health.HasValue && !part.Defense.HasValue && !part.HurtDuration.HasValue && !part.Scale.HasValue && part.NativeParameters == null)
                    return Error("InvalidNativePart", at, "Set supported part attack parameters or sounds, or omit the part.");
                var target = catalog?.FirstOrDefault(p => p.TemplateId == part.TemplateId);
                if (catalog != null && target == null)
                    return Error("NativePartUnsupported", at + ".templateId", "This root template does not advertise parameters for the selected part. Refresh the Host catalog.");
                if (part.Health <= 0 || part.Defense < 0 || part.HurtDuration.HasValue && !MonsterOverrideAdmission.ValidHurtDuration(part.HurtDuration.Value) || part.Scale.HasValue && (!ContentValidation.Finite(part.Scale.Value) || part.Scale <= 0)) return Error("InvalidNativePart", at, "Part health and visual scale must be positive; defense and hurt duration nonnegative.");
                var parameters = NativeParameterValidation.Validate(owner, new MonsterDefinition { LocalId = definition.LocalId, TemplateId = part.TemplateId, NativeParameters = part.NativeParameters }, target?.NativeParameters, catalog != null);
                if (parameters != null) return parameters;
                var error = NativeAttackValidation.Validate(owner, definition.LocalId, definition.TemplateId,
                    part.NativeAttacks, target?.NativeAttacks, fieldPrefix: at + ".nativeAttacks");
                if (error != null) return error;
                error = SoundValidation.Validate(owner, definition.LocalId, definition.TemplateId, part.Sounds, at + ".sounds");
                if (error != null) return error;
                if (target != null)
                    foreach (var sound in SoundValidation.Events(part.Sounds))
                        if (!target.SoundEvents.Contains(sound.Key)) return Error("SoundUnsupported", at + ".sounds." + sound.Key,
                            "This root catalog does not advertise the requested sound for the selected part.");
            }
            return null;
        }
    }
}
