using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class MimicryParameters : IChomperMimicry, INativeAttackParameters
        {
            private readonly IChomperMimicry source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal MimicryParameters(IChomperMimicry source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal MimicryParameters(IChomperMimicry source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
            public IMonsterAttackBehaviour Original => source;
            public bool NativeOverride => module != null;
            public bool HasStartCooldownEdit => module == null ? definition?.StartOnCooldown.HasValue == true :
                module.Ready && module.nativeAttackHookConflict == null && !double.IsNaN(module.nativeOverrides.Resolve(target,
                    NativeAttackValidation.Field(AttackId.ToString(), "startOnCooldown"), double.NaN));
            private float Read(string field, float baseline, float? configured) => module == null ? configured ?? baseline :
                !module.Ready || module.nativeAttackHookConflict != null ? baseline :
                (float)module.nativeOverrides.Resolve(target, NativeAttackValidation.Field(AttackId.ToString(), field), baseline);
            public MonsterAttackId AttackId => source.AttackId;
            public float CDDuration => Read("cooldownGameSeconds", source.CDDuration, definition?.CooldownGameSeconds);
            public bool CDOnStart => Read("startOnCooldown", source.CDOnStart ? 1 : 0,
                definition?.StartOnCooldown.HasValue == true ? (definition.StartOnCooldown.Value ? 1f : 0f) : (float?)null) != 0;
            public Vector2 DurationRange => new Vector2(
                Read("chomperMimicry.minDecoyGameSeconds", source.DurationRange.x, definition?.ChomperMimicry?.MinDecoyGameSeconds),
                Read("chomperMimicry.maxDecoyGameSeconds", source.DurationRange.y, definition?.ChomperMimicry?.MaxDecoyGameSeconds));
            public string SkillId => source.SkillId;
            // Present on the interface, but never consumed by this native action.
            public float Damage => source.Damage;
            public float CriticalRate => source.CriticalRate;
            public bool HasReadyAction => source.HasReadyAction;
            public float ReadyDuration => source.ReadyDuration;
        }
        private static MonsterNativeAttackInfo DescribeMimicry(IChomperMimicry a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "chomper-mimicry", new[] {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "registration; native next available attack; preserve remaining cooldown"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; no live rearming"),
                F("chomperMimicry.minDecoyGameSeconds", a.DurationRange.x, "game-seconds", "Invoke samples one independent decoy lifetime after native acquisition"),
                F("chomperMimicry.maxDecoyGameSeconds", a.DurationRange.y, "game-seconds", "Invoke float Range; equal endpoints fixed, reversed endpoints swap natively") },
                "Skill=" + a.SkillId + ". Decoy lifetime is independent of the body's hardcoded 2-3 second drill wait. Active decoy/body snapshots survive parameter changes and withdrawal. Zero duration creates a decoy until the next manager tick; felling ends only the decoy, without damage/drop/energy/chop cost. Damage, CriticalRate, HasReadyAction and ReadyDuration are unused and not editable. Existing self-action policy applies to managed definitions; native overrides do not change original null-target rejection. Resources, sounds, leaf effects and ground relocation stay native.");
        }
    }
}
