using System;
using System.Collections.Generic;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class ShootParameters : IShoot, INativeAttackParameters
        {
            private readonly IShoot source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal ShootParameters(IShoot source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal ShootParameters(IShoot source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => Read("shoot.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.Shoot?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public float ShootDuration => Read("shoot.recoveryGameSeconds", source.ShootDuration, definition?.Shoot?.RecoveryGameSeconds);
            public float BulletSpeed => Read("shoot.projectileSpeed", source.BulletSpeed, definition?.Shoot?.ProjectileSpeed);
            public float BulletDuration => Read("shoot.projectileLifetimeGameSeconds", source.BulletDuration, definition?.Shoot?.ProjectileLifetimeGameSeconds);
            public float BulletDistance => BulletSpeed * BulletDuration;
            public string BulletId => source.BulletId;
            public string BulletMoverId => source.BulletMoverId;
            public bool DisableRecoil => source.DisableRecoil;
            public bool DisableShootEffects => source.DisableShootEffects;
            public string FireSound => source.FireSound;
        }
        private static MonsterNativeAttackInfo DescribeShoot(IShoot a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            var fields = new List<MonsterNativeAttackFieldInfo> {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition: registration; native override: next available attack; current cooldown keeps its remaining time"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "native attack registration at spawn/reentry; no change to an existing live cooldown"),
                F("damage", a.Damage, "damage", "each native projectile hit, including already-fired projectiles"),
                F("criticalRate", a.CriticalRate, "fraction", "each native projectile hit, including already-fired projectiles"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "windup tween creation when enabled; an existing tween retains its duration"),
                F("shoot.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "native Shoot.Invoke; disabled skips windup"),
                F("shoot.recoveryGameSeconds", a.ShootDuration, "game-seconds", "recoil/return sequence creation after windup; an existing sequence retains its duration"),
                F("shoot.projectileLifetimeGameSeconds", a.BulletDuration, "game-seconds", "fire queue insertion; existing queue entries/projectiles retain their lifetime; range is speed times lifetime") };
            bool linear = DolocAPI.assets.bulletMovers.QueryData(a.BulletMoverId, out var mover) && mover is BulletMoverProtoLinear;
            if (linear) fields.Add(F("shoot.projectileSpeed", a.BulletSpeed, "world-units-per-game-second", "fire queue insertion; existing queue entries/projectiles retain their speed; range is speed times lifetime"));
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "shoot", fields,
                "Bullet=" + a.BulletId + "; mover=" + a.BulletMoverId + ". Native range derives from speed * lifetime plus line-of-sight. Recoil distance/ratio are fixed; DisableRecoil is unused here. Bullet/mover resources, sounds and effects remain inherited." +
                (linear ? "" : " Projectile speed editing requires a proven linear mover."));
        }
    }
}
