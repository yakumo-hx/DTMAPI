using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class ThunderParameters : IThunderPile, INativeAttackParameters
        {
            private readonly IThunderPile source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal ThunderParameters(IThunderPile source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal ThunderParameters(IThunderPile source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
            public IMonsterAttackBehaviour Original => source;
            public bool NativeOverride => module != null;
            public bool HasStartCooldownEdit => module == null ? definition?.StartOnCooldown.HasValue == true :
                module.Ready && module.nativeAttackHookConflict == null && !double.IsNaN(module.nativeOverrides.Resolve(target,
                    NativeAttackValidation.Field(AttackId.ToString(), "startOnCooldown"), double.NaN));
            private float Read(string field, float baseline, float? configured) => module == null ? configured ?? baseline :
                !module.Ready || module.nativeAttackHookConflict != null ? baseline :
                (float)module.nativeOverrides.Resolve(target, NativeAttackValidation.Field(AttackId.ToString(), field), baseline);
            private static float? Flag(bool? value) => value.HasValue ? (value.Value ? 1f : 0f) : (float?)null;
            public MonsterAttackId AttackId => source.AttackId;
            public float CDDuration => Read("cooldownGameSeconds", source.CDDuration, definition?.CooldownGameSeconds);
            public bool CDOnStart => Read("startOnCooldown", source.CDOnStart ? 1 : 0, Flag(definition?.StartOnCooldown)) != 0;
            public float Damage => Read("damage", source.Damage, definition?.Damage);
            public float CriticalRate => Read("criticalRate", source.CriticalRate, definition?.CriticalRate);
            public bool HasReadyAction => Read("thunderPile.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.ThunderPile?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public float AttackRange => Read("thunderPile.range", source.AttackRange, definition?.ThunderPile?.Range);
            public Vector2Int CountRange => new Vector2Int(
                (int)Read("thunderPile.minCount", source.CountRange.x, definition?.ThunderPile?.MinCount),
                (int)Read("thunderPile.maxCount", source.CountRange.y, definition?.ThunderPile?.MaxCount));
            public float SubReadyDuration => Read("thunderPile.subWindupGameSeconds", source.SubReadyDuration, definition?.ThunderPile?.SubWindupGameSeconds);
            public float DropSpeed => Read("thunderPile.dropSpeed", source.DropSpeed, definition?.ThunderPile?.DropSpeed);
            public float MaxDropDuration => Read("thunderPile.maxDropGameSeconds", source.MaxDropDuration, definition?.ThunderPile?.MaxDropGameSeconds);
            public float WaitDuration => Read("thunderPile.waitGameSeconds", source.WaitDuration, definition?.ThunderPile?.WaitGameSeconds);
            public float FadeDuration => Read("thunderPile.fadeGameSeconds", source.FadeDuration, definition?.ThunderPile?.FadeGameSeconds);
            public string SkillId => source.SkillId;
        }
        private static MonsterNativeAttackInfo DescribeThunder(IThunderPile a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "thunder-pile", new[] {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "registration; next available attack; preserve remaining cooldown"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; no live rearming"),
                F("damage", a.Damage, "damage", "each DropThunderPile snapshots damage into the new skill"),
                F("criticalRate", a.CriticalRate, "fraction", "each DropThunderPile; current skills retain their configuration"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "Invoke captures enabled initial wait"),
                F("thunderPile.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "Invoke; false retains the ship's 0.35-second recoil delay"),
                F("thunderPile.range", a.AttackRange, "world-units", "strict target-distance gate; does not control the ground strike location"),
                F("thunderPile.minCount", a.CountRange.x, "attempts", "Invoke inclusive integer sample; max <= min selects min, at least one"),
                F("thunderPile.maxCount", a.CountRange.y, "attempts", "Invoke; current count survives later edits; missing ground consumes attempts"),
                F("thunderPile.subWindupGameSeconds", a.SubReadyDuration, "game-seconds", "after initial wait/recoil, captures all per-pile and trailing ready durations"),
                F("thunderPile.dropSpeed", a.DropSpeed, "world-units-per-game-second", "each DropThunderPile snapshots downward speed; minimum 1"),
                F("thunderPile.maxDropGameSeconds", a.MaxDropDuration, "game-seconds", "each DropThunderPile; falling timeout retires without landing/fade; minimum 0.1"),
                F("thunderPile.waitGameSeconds", a.WaitDuration, "game-seconds", "each DropThunderPile; timer restarts on native wall contact; minimum 0.1"),
                F("thunderPile.fadeGameSeconds", a.FadeDuration, "game-seconds", "each DropThunderPile; after landed wait creates native automatic fade") },
                "Skill=" + a.SkillId + ". Ground/camera placement follows the current player at each attempt, independently of the request target. No-ground retries use the native one-second wait. Final ready segment stops without an extra pile. Current skills/count/waits and remaining cooldown survive edits/withdrawal. Wall contact resets landed lifetime; native player/monster hit dedup and Thunder damage properties remain. Resources, sounds, effects, ship/turrets and exact skill ownership stay inherited.");
        }
    }
}
