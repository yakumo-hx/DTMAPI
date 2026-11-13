using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class BatteringParameters : IBattering, INativeAttackParameters
        {
            private readonly IBattering source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal BatteringParameters(IBattering source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal BatteringParameters(IBattering source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => Read("battering.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.Battering?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public float DashDistance => Read("battering.attackDistance", source.DashDistance, definition?.Battering?.AttackDistance);
            public float DashSpeed => Read("battering.travelSpeed", source.DashSpeed, definition?.Battering?.TravelSpeed);
            public float DashDuration => DashDistance / DashSpeed;
            public DG.Tweening.Ease DashEase => source.DashEase;
            public string DashSound => source.DashSound;
            public bool HasAfterImage => source.HasAfterImage;
        }
        private static MonsterNativeAttackInfo DescribeBattering(IBattering a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "battering", new[] {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition: registration; native override: next available attack; current remaining wait is preserved"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; existing cooldown is unchanged"),
                F("damage", a.Damage, "damage", "body hit window opening after optional windup; the current window retains its snapshot"),
                F("criticalRate", a.CriticalRate, "fraction", "body hit window opening; changes or withdrawal affect the next window"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "enabled windup tween creation; current duration is fixed"),
                F("battering.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "Invoke after successful native ground endpoint lookup"),
                F("battering.attackDistance", a.DashDistance, "world-units", "horizontal range and edge recovery duration = distance / speed / 3; terrain determines the endpoint"),
                F("battering.travelSpeed", a.DashSpeed, "world-units-per-game-second", "after windup: actual path distance / speed; wall branch duration divided by four; existing motion/recovery stays fixed") },
                "Range requires absolute target height difference < 1.5. Invoke captures the native same-line ground endpoint and wall/edge branch before windup. Wall travel is four times faster with a fixed 0.5-second, three-unit curved rebound; edge travel keeps " + a.DashEase + " then retreats one unit over distance/speed/3. Body damage closes before recovery; there are no impact bullets. Effects, afterimages, sound and curves remain inherited.");
        }
    }
}
