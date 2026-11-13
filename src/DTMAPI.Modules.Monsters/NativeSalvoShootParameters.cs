using System.Collections.Generic;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class SalvoShootParameters : ISalvoShoot, INativeAttackParameters
        {
            private readonly ISalvoShoot source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal SalvoShootParameters(ISalvoShoot source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal SalvoShootParameters(ISalvoShoot source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => Read("salvoShoot.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.SalvoShoot?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public int BulletCount => (int)Read("salvoShoot.bulletCount", source.BulletCount, definition?.SalvoShoot?.BulletCount);
            public float SectorAngle => Read("salvoShoot.sectorAngleDegrees", source.SectorAngle, definition?.SalvoShoot?.SectorAngleDegrees);
            public float ShootInterval => Read("salvoShoot.shotIntervalGameSeconds", source.ShootInterval, definition?.SalvoShoot?.ShotIntervalGameSeconds);
            public float BulletSpeed => Read("salvoShoot.projectileSpeed", source.BulletSpeed, definition?.SalvoShoot?.ProjectileSpeed);
            public float BulletDuration => Read("salvoShoot.projectileLifetimeGameSeconds", source.BulletDuration, definition?.SalvoShoot?.ProjectileLifetimeGameSeconds);
            public float BulletDistance => BulletSpeed * BulletDuration;
            public string BulletId => source.BulletId;
            public string BulletMoverId => source.BulletMoverId;
            public bool DisableRecoil => source.DisableRecoil;
            public bool DisableShootEffects => source.DisableShootEffects;
            public string FireSound => source.FireSound;
        }
        private static MonsterNativeAttackInfo DescribeSalvoShoot(ISalvoShoot a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            var fields = new List<MonsterNativeAttackFieldInfo> {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition: registration; native override: next available attack; preserve current remaining wait"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; existing live cooldown is unchanged"),
                F("damage", a.Damage, "damage", "each native projectile hit, including already-fired projectiles"),
                F("criticalRate", a.CriticalRate, "fraction", "each native projectile hit, including already-fired projectiles"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "enabled windup tween creation; existing duration is fixed"),
                F("salvoShoot.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "native Invoke; disabled skips windup"),
                F("salvoShoot.bulletCount", a.BulletCount, "bullets", "volley sequence creation after windup, or at Invoke without windup; all directions/count fixed"),
                F("salvoShoot.sectorAngleDegrees", a.SectorAngle, "degrees", "volley sequence creation; -angle/2 + i*angle/count; one bullet keeps captured direction"),
                F("salvoShoot.shotIntervalGameSeconds", a.ShootInterval, "game-seconds", "all intervals fixed at volley sequence creation, including the interval after its last shot"),
                F("salvoShoot.projectileLifetimeGameSeconds", a.BulletDuration, "game-seconds", "each actual fire queue insertion; prior queue entries/projectiles retain lifetime") };
            bool linear = DolocAPI.assets.bulletMovers.QueryData(a.BulletMoverId, out var mover) && mover is BulletMoverProtoLinear;
            if (linear) fields.Add(F("salvoShoot.projectileSpeed", a.BulletSpeed, "world-units-per-game-second", "each actual fire queue insertion; prior queue entries/projectiles retain speed"));
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "salvo-shoot", fields,
                "Bullet=" + a.BulletId + "; mover=" + a.BulletMoverId + ". Range is speed * lifetime plus native ray mask. First shot is at sequence start; trailing interval remains. Count 1 keeps target direction; angle 0 aligns all shots; angle 360 forms a ring without a duplicate endpoint. Resources/mover/sound/effects remain inherited." +
                (linear ? "" : " This mover's speed consumption is unproven; speed editing is unavailable."));
        }
    }
}
