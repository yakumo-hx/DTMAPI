using System.Collections.Generic;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private abstract class PlacedParameters : IMonsterAttackBehaviourSkill, INativeAttackParameters
        {
            private readonly IMonsterAttackBehaviourSkill source;
            protected readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            protected PlacedParameters(IMonsterAttackBehaviourSkill source, MonsterNativeAttackParameters value)
            { this.source = source; definition = value; }
            protected PlacedParameters(IMonsterAttackBehaviourSkill source, NativeModule module, string target)
            { this.source = source; this.module = module; this.target = target; }
            public IMonsterAttackBehaviour Original => source;
            public bool NativeOverride => module != null;
            public bool HasStartCooldownEdit => module == null ? definition?.StartOnCooldown.HasValue == true :
                module.Ready && module.nativeAttackHookConflict == null && !double.IsNaN(module.nativeOverrides.Resolve(target,
                    NativeAttackValidation.Field(AttackId.ToString(), "startOnCooldown"), double.NaN));
            protected float Read(string field, float baseline, float? configured) => module == null ? configured ?? baseline :
                !module.Ready || module.nativeAttackHookConflict != null ? baseline :
                (float)module.nativeOverrides.Resolve(target, NativeAttackValidation.Field(AttackId.ToString(), field), baseline);
            public MonsterAttackId AttackId => source.AttackId;
            public float CDDuration => Read("cooldownGameSeconds", source.CDDuration, definition?.CooldownGameSeconds);
            public bool CDOnStart => Read("startOnCooldown", source.CDOnStart ? 1 : 0,
                definition?.StartOnCooldown.HasValue == true ? (definition.StartOnCooldown.Value ? 1f : 0f) : (float?)null) != 0;
            public float Damage => Read("damage", source.Damage, definition?.Damage);
            public float CriticalRate => Read("criticalRate", source.CriticalRate, definition?.CriticalRate);
            public bool HasReadyAction => source.HasReadyAction; // Neither native placement consumer reads this flag.
            public abstract float ReadyDuration { get; }
            public string SkillId => source.SkillId;
        }
        private sealed class LandmineParameters : PlacedParameters, ILandmine
        {
            private readonly ILandmine source;
            internal LandmineParameters(ILandmine source, MonsterNativeAttackParameters value) : base(source, value) { this.source = source; }
            internal LandmineParameters(ILandmine source, NativeModule module, string target) : base(source, module, target) { this.source = source; }
            public override float ReadyDuration => Read("landmine.recoilGameSeconds", source.ReadyDuration, definition?.Landmine?.RecoilGameSeconds);
            public float ValidDistance => Read("landmine.range", source.ValidDistance, definition?.Landmine?.Range);
            public float MoveSpeed => Read("landmine.throwImpulse", source.MoveSpeed, definition?.Landmine?.ThrowImpulse);
            public float LiveDuration => Read("landmine.liveGameSeconds", source.LiveDuration, definition?.Landmine?.LiveGameSeconds);
            public float ReflectionTime => Read("landmine.reactionGameSeconds", source.ReflectionTime, definition?.Landmine?.ReactionGameSeconds);
        }
        private sealed class TimeBombParameters : PlacedParameters, ITimeBomb
        {
            private readonly ITimeBomb source;
            internal TimeBombParameters(ITimeBomb source, MonsterNativeAttackParameters value) : base(source, value) { this.source = source; }
            internal TimeBombParameters(ITimeBomb source, NativeModule module, string target) : base(source, module, target) { this.source = source; }
            public override float ReadyDuration => Read("timeBomb.recoilGameSeconds", source.ReadyDuration, definition?.TimeBomb?.RecoilGameSeconds);
            public float ValidDistance => Read("timeBomb.range", source.ValidDistance, definition?.TimeBomb?.Range);
            public float MoveSpeed => Read("timeBomb.throwImpulse", source.MoveSpeed, definition?.TimeBomb?.ThrowImpulse);
            public float MaxMoveSpeed => Read("timeBomb.maxThrowImpulse", source.MaxMoveSpeed, definition?.TimeBomb?.MaxThrowImpulse);
            public int Count => (int)Read("timeBomb.count", source.Count, definition?.TimeBomb?.Count);
        }
        private static MonsterNativeAttackInfo DescribePlaced(IMonsterAttackBehaviourSkill a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            var fields = new List<MonsterNativeAttackFieldInfo> {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "registration; next available attack; preserve remaining cooldown"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; no live rearming"),
                F("damage", a.Damage, "damage", "each Drop snapshots damage into its skill; later explosion uses that snapshot"),
                F("criticalRate", a.CriticalRate, "fraction", "each Drop snapshots critical chance; active skills survive edits/withdrawal") };
            if (a is ILandmine mine)
            {
                fields.Add(F("landmine.range", mine.ValidDistance, "world-units", "native fire-position target-distance gate, inclusive"));
                fields.Add(F("landmine.recoilGameSeconds", a.ReadyDuration, "game-seconds", "Invoke captures recoil duration/path; placement is the first sequence callback"));
                fields.Add(F("landmine.throwImpulse", mine.MoveSpeed, "rigidbody-impulse", "Drop reads horizontal impulse; direction was captured at Invoke"));
                fields.Add(F("landmine.liveGameSeconds", mine.LiveDuration, "game-seconds", "Drop snapshots; native ground ray starts lifetime, trigger checked before increment"));
                fields.Add(F("landmine.reactionGameSeconds", mine.ReflectionTime, "game-seconds", "Drop snapshots; native timer interval is input * 0.333333 per countdown sprite"));
            }
            else
            {
                var bomb = (ITimeBomb)a;
                fields.Add(F("timeBomb.range", bomb.ValidDistance, "world-units", "native fire-position target-distance gate, inclusive"));
                fields.Add(F("timeBomb.recoilGameSeconds", a.ReadyDuration, "game-seconds", "Invoke captures recoil duration/path; allocation starts at the first sequence callback"));
                fields.Add(F("timeBomb.throwImpulse", bomb.MoveSpeed, "rigidbody-impulse", "_Start computes step; each loop iteration reads the current starting impulse"));
                fields.Add(F("timeBomb.count", bomb.Count, "count", "_Start branch/step and loop condition; zero allocates nothing, one uses starting impulse"));
                fields.Add(F("timeBomb.maxThrowImpulse", bomb.MaxMoveSpeed, "rigidbody-impulse", "_Start captures step=(max-start)/count; multi-bomb distribution excludes max, descending endpoints allowed"));
            }
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), a is ILandmine ? "landmine" : "time-bomb", fields,
                "Skill=" + a.SkillId + ". HasReadyAction is not consumed; recoil is not windup. Direction and recoil path/duration are captured at Invoke; placement configuration is read at the initial callback. " +
                "Landmine lifetime starts after native ground detection; reaction advances one sprite per timer tick. TimeBomb keeps its fixed landing countdown and no-ground insurance; no item refund is introduced. " +
                "Current skills/explosions, recoil and remaining cooldown survive parameter withdrawal. Resources, sounds/effects, shared managers and exact skill/Boom ownership remain inherited.");
        }
    }
}
