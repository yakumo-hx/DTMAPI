using System;
using System.Linq;
namespace DTMAPI.MonsterContent.Hosting
{
    internal static class MonsterValidation
    {
        public static MonsterDiagnostic? Validate(string owner, MonsterDefinition? definition)
        {
            string field = definition == null ? "definition" : !ValidId(definition.LocalId) ? "localId" :
                string.IsNullOrWhiteSpace(definition.TemplateId) ? "templateId" : definition.ExpValue < 0 ? "expValue" : definition.Health <= 0 ? "health" : definition.Defense < 0 ? "defense" :
                definition.HurtDuration.HasValue && !MonsterOverrideAdmission.ValidHurtDuration(definition.HurtDuration.Value) ? "hurtDuration" :
                definition.Behavior != "inherit-native" && definition.Behavior != "code-provider" && definition.Behavior != "configured-sequence" ? "behavior" :
                definition.Behavior == "code-provider" && !ValidQualifiedId(definition.BehaviorId) ? "behaviorId" :
                definition.Behavior != "code-provider" && !string.IsNullOrEmpty(definition.BehaviorId) ? "behaviorId" : "";
            return field.Length == 0 ? CompositionAdmission.Validate(owner, definition!) ?? NativeParameterValidation.Validate(owner, definition!) ?? SoundValidation.Validate(owner, definition!) ?? PhysicalValidation.Validate(owner, definition!) ?? NativeAttackValidation.Validate(owner, definition!.LocalId, definition.TemplateId, definition.NativeAttacks) ?? NativePartValidation.Validate(owner, definition!) ?? SkillValidation.Validate(owner, definition!) ?? SequenceValidation.Validate(owner, definition!) ?? HealthPhaseValidation.Validate(owner, definition!) ?? CompendiumValidation.Validate(owner, definition!) ?? ContentValidation.Drops(owner, definition!.Drops) ?? PresentationValidation.Validate(owner, definition) : new MonsterDiagnostic("InvalidDefinition", "Invalid or unsupported definition field: " + field, owner,
                definition == null ? "" : owner + ":" + definition.LocalId, definition?.TemplateId ?? "", "Validate", field, "Use inherit-native or code-provider with an owner-qualified BehaviorId.");
        }
        internal static bool ValidId(string value) => !string.IsNullOrEmpty(value) && value.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.');
        internal static bool ValidQualifiedId(string value) => value != null && value.Split(':').Length == 2 && value.Split(':').All(ValidId);
    }
}
