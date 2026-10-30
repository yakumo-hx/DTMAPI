using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MonsterContent
{
    /// <summary>Copies one native attack input to a target slot. Body, animation and AI stay with the target template.</summary>
    public sealed class MonsterAttackComposition
    {
        public string SourceTemplate { get; set; } = "";
        public string SourceAttack { get; set; } = "";
        public string TargetAttack { get; set; } = "";
        internal MonsterAttackComposition Copy() => (MonsterAttackComposition)MemberwiseClone();
    }
    internal static class CompositionAdmission
    {
        internal static MonsterDiagnostic? Validate(string owner, MonsterDefinition d, IReadOnlyList<MonsterTemplate>? templates = null)
        {
            var targets = new HashSet<string>(StringComparer.Ordinal);
            MonsterDiagnostic Fail(string code, string at, string message) => new MonsterDiagnostic(code, message, owner, owner + ":" + d.LocalId, d.TemplateId, "Composition", at);
            if (d.AttackComposition == null) return Fail("InvalidComposition", "attackComposition", "Expected a list of native slot mappings.");
            for (int i = 0; i < d.AttackComposition.Count; i++)
            {
                var row = d.AttackComposition[i]; string at = "attackComposition[" + i + "]";
                if (row == null || !Hosting.MonsterValidation.ValidId(row.SourceTemplate) || !Hosting.MonsterValidation.ValidId(row.SourceAttack) || !Hosting.MonsterValidation.ValidId(row.TargetAttack) || !targets.Add(row.TargetAttack))
                    return Fail("InvalidComposition", at, "Supply source template/attack and a unique target attack slot.");
                if (!new[] { "AtkNormal", "AtkSkill01", "AtkSkill02", "AtkSkill03", "AtkSkill04" }.Contains(row.TargetAttack)) return Fail("AttackSlotMissing", at + ".targetAttack", "Target is not a native attack slot.");
                if (templates == null) continue;
                var source = templates.FirstOrDefault(t => t.Id == row.SourceTemplate);
                if (source == null) return Fail("TemplateMissing", at + ".sourceTemplate", "Source template is absent from the catalog.");
                if (!source.Actions.AttackIds.Contains(row.SourceAttack)) return Fail("AttackMissing", at + ".sourceAttack", "Source attack is absent from this template.");
            }
            return null;
        }
        internal static MonsterTemplate Project(MonsterTemplate target, MonsterDefinition d, IReadOnlyList<MonsterTemplate> templates)
        {
            if (d.AttackComposition.Count == 0) return target;
            var a = target.Actions; var infos = a.NativeAttacks.ToList(); var ids = a.AttackIds.ToList(); var scatter = a.ScatterAttackIds.ToList();
            foreach (var row in d.AttackComposition)
            {
                var source = templates.First(t => t.Id == row.SourceTemplate);
                if (!ids.Contains(row.TargetAttack)) ids.Add(row.TargetAttack);
                infos.RemoveAll(x => x.AttackId == row.TargetAttack); scatter.Remove(row.TargetAttack);
                var info = source.Actions.NativeAttacks.FirstOrDefault(x => x.AttackId == row.SourceAttack);
                if (info != null) infos.Add(new MonsterNativeAttackInfo(row.TargetAttack, info.Kind, info.Fields,
                    "Source " + row.SourceTemplate + "/" + row.SourceAttack + ". Target retains body/AI/animation; AI must call this slot. " + info.Limitation, info.Sounds));
                if (source.Actions.ScatterAttackIds.Contains(row.SourceAttack)) scatter.Add(row.TargetAttack);
            }
            return target.WithActions(new MonsterActionCapabilities(ids, a.ControlledMovement, a.MovementReason, a.TargetRequirement) {
                NativeAttacks = infos.ToArray(), NativeParts = a.NativeParts, ScatterAttackIds = scatter.ToArray(), Dash = a.Dash,
                DashReason = a.DashReason, ActionAnimation = a.ActionAnimation, PhasedNativeActions = a.PhasedNativeActions });
        }
    }
}
