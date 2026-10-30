using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class ShadowImpactParameters : IShadowImpact, INativeAttackParameters
        {
            private readonly IShadowImpact source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal ShadowImpactParameters(IShadowImpact source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal ShadowImpactParameters(IShadowImpact source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => Read("shadowImpact.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.ShadowImpact?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public float DashDistance => Read("shadowImpact.distance", source.DashDistance, definition?.ShadowImpact?.Distance);
            public float DashSpeed => Read("shadowImpact.travelSpeed", source.DashSpeed, definition?.ShadowImpact?.TravelSpeed);
            public float DashDuration => DashDistance / DashSpeed;
            public string SkillId => source.SkillId;
            public ImpactPath Path => source.Path; // Unused by this native execution path.
            public float ArcDistance => source.ArcDistance;
            public DG.Tweening.Ease DashEase => source.DashEase;
            public string DashSound => source.DashSound;
            public bool HasAfterImage => source.HasAfterImage;
        }
        private static MonsterNativeAttackInfo DescribeShadowImpact(IShadowImpact a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "shadow-impact", new[] {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition registration; native next available attack; preserve remaining cooldown"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; no live rearming"),
                F("damage", a.Damage, "damage", "StartImpact after optional windup; body and both ShadowBox objects capture damage"),
                F("criticalRate", a.CriticalRate, "fraction", "StartImpact body/shadow configuration; current objects retain snapshots"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "Invoke captures enabled ready wait"),
                F("shadowImpact.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "Invoke; false immediately configures body and two shadows"),
                F("shadowImpact.distance", a.DashDistance, "world-units", "target-distance bound; StartImpact captures body endpoint and two shadow endpoints/control points"),
                F("shadowImpact.travelSpeed", a.DashSpeed, "world-units-per-game-second", "StartImpact duration = distance/speed for body and both shadows; not uniform speed or curve length") },
                "Skill=" + a.SkillId + ". Invoke captures the ready wait. StartImpact reads current target direction and configures body plus two independent ShadowBox objects. Body moves straight; shadows use fixed +/-15-degree endpoints and a forward control point with native Bezier/easing. Current paths and damage survive parameter changes/withdrawal. Shadow count, colors, sounds, afterimages and native player/monster hit deduplication remain inherited. Path/ArcDistance/CalcControlPoint are unused. No extra terrain sweep; zero distance keeps the native zero-duration path.");
        }
    }
}
