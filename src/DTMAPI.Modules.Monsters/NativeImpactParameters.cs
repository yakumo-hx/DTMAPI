using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class ImpactParameters : IImpact, INativeAttackParameters
        {
            private readonly IImpact source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal ImpactParameters(IImpact source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal ImpactParameters(IImpact source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => Read("impact.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.Impact?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public float DashDistance => Read("impact.distance", source.DashDistance, definition?.Impact?.Distance);
            public float DashSpeed => Read("impact.travelSpeed", source.DashSpeed, definition?.Impact?.TravelSpeed);
            public float DashDuration => DashDistance / DashSpeed;
            public ImpactPath Path
            {
                get
                {
                    float value = Read("impact.arcPath", float.NaN, Flag(definition?.Impact?.ArcPath));
                    // Null/withdrawal preserves the raw native path, including its Sin fallback.
                    return float.IsNaN(value) ? source.Path : value != 0 ? ImpactPath.Arc : ImpactPath.Linear;
                }
            }
            public float ArcDistance => Read("impact.arcOffset", source.ArcDistance, definition?.Impact?.ArcOffset);
            public DG.Tweening.Ease DashEase => source.DashEase;
            public string DashSound => source.DashSound;
            public bool HasAfterImage => source.HasAfterImage;
        }
        private static MonsterNativeAttackInfo DescribeImpact(IImpact a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "impact", new[] {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition registration; native next available attack; preserve current remaining wait"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; no live rearming"),
                F("damage", a.Damage, "damage", "StartImpact after optional windup; body window retains its snapshot"),
                F("criticalRate", a.CriticalRate, "fraction", "StartImpact body window; changes and withdrawal affect future windows"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "Invoke captures enabled ready tween duration; current wait stays fixed"),
                F("impact.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "Invoke; false starts the body path immediately"),
                F("impact.distance", a.DashDistance, "world-units", "native target-distance bound; StartImpact captures endpoint and distance/speed duration"),
                F("impact.travelSpeed", a.DashSpeed, "world-units-per-game-second", "StartImpact captures duration = distance/speed; not constant speed or arc length"),
                F("impact.arcPath", a.Path == ImpactPath.Arc ? 1 : 0, "boolean", "StartImpact captures Linear/Arc; null preserves the original path including Sin's linear fallback"),
                F("impact.arcOffset", a.ArcDistance, "control-point-world-units", "StartImpact; signed perpendicular offset from chord midpoint; consumed only by Arc") },
                "Invoke captures the optional native recoil wait. StartImpact then captures current origin, current target direction, endpoint, body damage and path duration. Arc uses the native control-point interpolation and inherited easing; control offset is not maximum arc height. Positive offset is left of the travel direction, negative is right. Linear ignores offset; Sin remains a native linear fallback and is not an author option. There is no additional terrain sweep or return leg. Zero distance keeps the native zero-duration path. Recoil, easing, sounds and afterimages remain inherited.");
        }
    }
}
