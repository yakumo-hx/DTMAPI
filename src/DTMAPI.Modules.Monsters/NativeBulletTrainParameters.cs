using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class BulletTrainParameters : IBulletTrain, INativeAttackParameters
        {
            private readonly IBulletTrain source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal BulletTrainParameters(IBulletTrain source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal BulletTrainParameters(IBulletTrain source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => Read("bulletTrain.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.BulletTrain?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public int BulletCount => (int)Read("bulletTrain.bulletCount", source.BulletCount, definition?.BulletTrain?.BulletCount);
            public int BulletShootTimes => (int)Read("bulletTrain.volleyCount", source.BulletShootTimes, definition?.BulletTrain?.VolleyCount);
            public int SectorAngle => (int)Read("bulletTrain.sectorAngleDegrees", source.SectorAngle, definition?.BulletTrain?.SectorAngleDegrees);
            public float ShootInterval => Read("bulletTrain.volleyIntervalGameSeconds", source.ShootInterval, definition?.BulletTrain?.VolleyIntervalGameSeconds);
            public Vector2 InitialDirection => new Vector2(
                Read("bulletTrain.directionX", source.InitialDirection.x, definition?.BulletTrain?.DirectionX),
                Read("bulletTrain.directionY", source.InitialDirection.y, definition?.BulletTrain?.DirectionY));
            public float BulletSpeed => Read("bulletTrain.projectileSpeed", source.BulletSpeed, definition?.BulletTrain?.ProjectileSpeed);
            public float BulletDuration => Read("bulletTrain.projectileLifetimeGameSeconds", source.BulletDuration, definition?.BulletTrain?.ProjectileLifetimeGameSeconds);
            public float BulletDistance => BulletSpeed * BulletDuration;
            public float FlySpeed => source.FlySpeed; // Not consumed by native BulletTrain.
            public string BulletId => source.BulletId;
            public string BulletMoverId => source.BulletMoverId;
            public bool DisableRecoil => source.DisableRecoil;
            public bool DisableShootEffects => source.DisableShootEffects;
            public string FireSound => source.FireSound;
        }
        private static MonsterNativeAttackInfo DescribeBulletTrain(IBulletTrain a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "bullet-train", new[] {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition registration; native next available attack; preserve remaining cooldown"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; no live rearming"),
                F("damage", a.Damage, "damage", "each native projectile hit, including previously fired projectiles"),
                F("criticalRate", a.CriticalRate, "fraction", "each native projectile hit"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "Invoke captures enabled ready tween duration"),
                F("bulletTrain.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "Invoke; false immediately constructs movement and volley schedule"),
                F("bulletTrain.volleyCount", a.BulletShootTimes, "volleys", "after windup; complete schedule and body duration captured once"),
                F("bulletTrain.volleyIntervalGameSeconds", a.ShootInterval, "game-seconds", "after windup; first volley after one interval, no trailing interval after last volley"),
                F("bulletTrain.bulletCount", a.BulletCount, "bullets-per-volley", "each Shoot callback; each volley independently reads count/direction/angle"),
                F("bulletTrain.sectorAngleDegrees", a.SectorAngle, "integer-degrees", "each Shoot; half-open sector; one bullet keeps initial vector"),
                F("bulletTrain.directionX", a.InitialDirection.x, "direction-component", "each Shoot; literal unnormalized vector independent of target/facing"),
                F("bulletTrain.directionY", a.InitialDirection.y, "direction-component", "each Shoot; literal vector enters queue; Parabolic uses inherited vertical coefficient and gravity"),
                F("bulletTrain.projectileSpeed", a.BulletSpeed, "horizontal-speed-coefficient", "after windup captures body horizontal displacement; each projectile queue entry captures its Parabolic horizontal coefficient separately"),
                F("bulletTrain.projectileLifetimeGameSeconds", a.BulletDuration, "game-seconds", "each actual fire queue insertion") },
                "Bullet=" + a.BulletId + "; mover=" + a.BulletMoverId + ". Native range check is always true. Body duration = volley count * interval; horizontal displacement = duration * bullet speed toward the player's current side, with native random integer rise 3 or 4. Supplied attack target does not choose this body path. FlySpeed is unused. Each volley reads its own half-open fan from the literal direction vector. Parabolic initial velocity is (direction.x * speed, direction.y * inherited vertical speed), followed by inherited gravity; zero direction is not stationary. Resources, sounds, screen shake and ready recoil remain inherited; no additional terrain sweep.");
        }
    }
}
