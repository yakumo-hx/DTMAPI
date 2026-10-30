using System;
using System.Linq;
using System.Reflection;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private MonsterDiagnostic? ComposeTemplate(DTMAPI.MonsterContent.Hosting.DefinitionRecord record, ref Template target)
        {
            var rows = record.Definition.AttackComposition;
            if (rows.Count == 0) return null;
            var attacks = target.Proto.AttackBehaviourProtos.ToList();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i]; string field = "attackComposition[" + i + "]";
                if (!templates.TryGetValue(row.SourceTemplate, out var source)) return Error(record, "TemplateMissing", field + ".sourceTemplate", "Source template is absent.");
                var matches = source.Proto.AttackBehaviourProtos.Where(a => a.AttackId.ToString() == row.SourceAttack).ToArray();
                if (matches.Length != 1) return Error(record, "AttackMissing", field + ".sourceAttack", "Source attack must resolve uniquely.");
                if (!Enum.TryParse<MonsterAttackId>(row.TargetAttack, out var slot) || slot == MonsterAttackId.Unset || !Enum.IsDefined(typeof(MonsterAttackId), slot))
                    return Error(record, "AttackSlotMissing", field + ".targetAttack", "Target is not a native attack slot.");
                // Native SO-suffixed inputs are managed serialized objects. Clone scalar slot
                // identity only; bullet/skill resources remain native-owned read-only references.
                if (!(matches[0] is MonsterAttackBehaviourSO)) return Error(record, "CompositionInputUnsupported", field, "Source is not a native serialized attack input.");
                var copy = (IMonsterAttackBehaviour)typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(matches[0], null);
                NativeAccess.Set(copy, "attackId", slot);
                int index = attacks.FindIndex(a => a.AttackId == slot);
                if (index < 0) attacks.Add(copy); else attacks[index] = copy;
            }
            target = Inspect(new MonsterProto(target.Proto.Name, target.Proto.MoverProto, attacks.ToArray()));
            // Inspect and native ValidateInstance own resource/body/factory dependencies.
            // This is mechanism validation, never a whitelist of template pairs.
            return target.Errors.Count == 0 ? null : Error(record, target.Errors[0].Code, "attackComposition." + target.Errors[0].Field, target.Errors[0].Message);
        }
    }
}
