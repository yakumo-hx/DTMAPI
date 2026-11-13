using System.Linq;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class CallParameters : IMonsterAttackBehaviourCall, INativeAttackParameters
        {
            private readonly IMonsterAttackBehaviourCall source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal CallParameters(IMonsterAttackBehaviourCall source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal CallParameters(IMonsterAttackBehaviourCall source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => source.HasReadyAction; // Invoke always performs its native windup.
            public string MonsterId => source.MonsterId;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public Vector2Int CountRange => new Vector2Int(
                (int)Read("call.minCount", source.CountRange.x, definition?.Call?.MinCount),
                (int)Read("call.maxCount", source.CountRange.y, definition?.Call?.MaxCount));
            public int Limitation => (int)Read("call.roomLimit", source.Limitation, definition?.Call?.RoomLimit);
        }
        private static MonsterNativeAttackInfo DescribeCall(IMonsterAttackBehaviourCall a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "call", new[] {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "registration; next available attack; preserve remaining cooldown"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; no live rearming"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "Invoke captures windup duration/path; later edits do not retime it"),
                F("call.minCount", a.CountRange.x, "count", "completion samples one inclusive count; reversed/equal bounds use min, at least one"),
                F("call.maxCount", a.CountRange.y, "count", "completion samples once before room-limit truncation"),
                F("call.roomLimit", a.Limitation, "room-monsters", "completion subtracts current total room count, including parent and unrelated monsters") },
                "Monster=" + a.MonsterId + ". Each count endpoint inherits independently. Zero roomLimit allocates nothing but still winds up and consumes cooldown. HasReadyAction is unused; damage/critical/range are not Call parameters. " +
                "Withdrawal preserves current windup/cooldown and already committed children; pending count/room limit use the restored baseline at completion. Child templates, native spawn positions, effects and existing exact ownership remain inherited. This is not a per-summoner child cap.");
        }

        private static IMonsterAttackBehaviour[] DefinitionAttacks(Template template, System.Collections.Generic.IReadOnlyList<MonsterNativeAttackParameters> values)
        {
            // The asset may expose a covariant MonsterAttackBehaviourSO[] through
            // this interface. Clone preserves that runtime element type and cannot
            // hold our adapter; allocate the declared interface array explicitly.
            var attacks = new IMonsterAttackBehaviour[template.Proto.AttackBehaviourProtos.Length];
            System.Array.Copy(template.Proto.AttackBehaviourProtos, attacks, attacks.Length);
            foreach (var value in values)
            {
                if (!NativeAttackValidation.Fields(value).Any()) continue;
                int index = System.Array.FindIndex(attacks, a => a.AttackId.ToString() == value.AttackId);
                attacks[index] = attacks[index] is IAmoebaJumpAttack jump ? (IMonsterAttackBehaviour)new AmoebaJumpParameters(jump, value) :
                    attacks[index] is IMonsterAttackBehaviourCall call ? new CallParameters(call, value) : attacks[index] is IRandomShoot random ? new RandomShootParameters(random, value) : attacks[index] is IShoot shoot ? new ShootParameters(shoot, value) : attacks[index] is IMultiShoot multi ? new MultiShootParameters(multi, value) :
                    attacks[index] is ISalvoShoot salvo ? new SalvoShootParameters(salvo, value) : attacks[index] is IScatteringShoot scattering ? new ScatteringShootParameters(scattering, value) : attacks[index] is IDragonShoot dragon ? new DragonShootParameters(dragon, value) : attacks[index] is IScatteringThrow throwing ? new ScatteringThrowParameters(throwing, value) : attacks[index] is IBombingDive dive ? new BombingDiveParameters(dive, value) : attacks[index] is IBatteringBullet battering ? new BatteringBulletParameters(battering, value) : attacks[index] is IBattering body ? new BatteringParameters(body, value) : attacks[index] is IFungusShoot fungus ? new FungusShootParameters(fungus, value) : attacks[index] is ISting sting ? new StingParameters(sting, value) : attacks[index] is IMultiImpact repeated ? new MultiImpactParameters(repeated, value) : attacks[index] is IImpact impact ? new ImpactParameters(impact, value) : attacks[index] is IBulletTrain train ? new BulletTrainParameters(train, value) : attacks[index] is IShadowImpact shadow ? new ShadowImpactParameters(shadow, value) : attacks[index] is ILandmine mine ? new LandmineParameters(mine, value) : attacks[index] is ITimeBomb bomb ? new TimeBombParameters(bomb, value) : attacks[index] is IThunderPile thunder ? new ThunderParameters(thunder, value) : attacks[index] is IChomperMimicry mimicry ? new MimicryParameters(mimicry, value) : new ChomperBiteParameters((IChomperBite)attacks[index], value);
            }
            return attacks;
        }

    }
}
