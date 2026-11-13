using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class StingParameters : ISting, INativeAttackParameters
        {
            private readonly ISting source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal StingParameters(ISting source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal StingParameters(ISting source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => Read("sting.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.Sting?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public float DashDistance => Read("sting.distance", source.DashDistance, definition?.Sting?.Distance);
            public float DashSpeed => Read("sting.travelSpeed", source.DashSpeed, definition?.Sting?.TravelSpeed);
            public float DashDuration => DashDistance / DashSpeed;
            public DG.Tweening.Ease DashEase => source.DashEase;
            public string DashSound => source.DashSound;
            public bool HasAfterImage => source.HasAfterImage;
        }
        private static MonsterNativeAttackInfo DescribeSting(ISting a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "sting", new[] {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition registration; native next available attack; preserve current remaining wait"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; no live rearming"),
                F("damage", a.Damage, "damage", "StartSting after optional windup; the body window retains its snapshot"),
                F("criticalRate", a.CriticalRate, "fraction", "StartSting body window; changes and withdrawal affect future windows"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "Invoke captures enabled ready tween duration; current wait stays fixed"),
                F("sting.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "Invoke; false starts the body sequence immediately"),
                F("sting.distance", a.DashDistance, "world-units", "native target-distance bound; StartSting captures forward displacement and total distance/speed duration"),
                F("sting.travelSpeed", a.DashSpeed, "world-units-per-game-second", "StartSting captures total out-and-back duration = distance/speed; not constant physical speed") },
                "Invoke fixes the target direction. After optional native recoil, StartSting captures the current origin, forward endpoint, body damage and duration. Outward/return children receive nominal 70/30 percent durations, but the final native SetEase applies OutBack to the whole sequence and replaces the configured dash ease. The body closes at the outward endpoint before the impact effect; return ends at the post-windup origin. There is no additional terrain sweep. Zero distance keeps the native zero-duration path. Curves, recoil distance, sounds and effects remain inherited; HasAfterImage is not consumed by this attack.");
        }
    }
}
