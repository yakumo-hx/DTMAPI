using System.Collections.Generic;
using DTMAPI.MonsterContent;
using DolocTown;
using DolocTown.GameData;
using DolocTown.MonsterAttackBehaviours;
using UnityEngine;

namespace DTMAPI.Modules.Monsters
{
    internal sealed partial class NativeModule
    {
        private sealed class RandomShootParameters : IRandomShoot, INativeAttackParameters
        {
            private readonly IRandomShoot source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal RandomShootParameters(IRandomShoot source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal RandomShootParameters(IRandomShoot source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public float Damage => Read("damage", source.Damage, definition?.Damage);
            public float CriticalRate => Read("criticalRate", source.CriticalRate, definition?.CriticalRate);
            public bool HasReadyAction => source.HasReadyAction;
            public float ReadyDuration => source.ReadyDuration;
            public Vector2Int BulletCountRange => new Vector2Int(
                (int)Read("randomShoot.minBulletCount", source.BulletCountRange.x, definition?.RandomShoot?.MinBulletCount),
                (int)Read("randomShoot.maxBulletCount", source.BulletCountRange.y, definition?.RandomShoot?.MaxBulletCount));
            public float ShootInterval => Read("randomShoot.shotIntervalGameSeconds", source.ShootInterval, definition?.RandomShoot?.ShotIntervalGameSeconds);
            public float BulletSpeed => Read("randomShoot.projectileSpeed", source.BulletSpeed, definition?.RandomShoot?.ProjectileSpeed);
            public float BulletDuration => Read("randomShoot.projectileLifetimeGameSeconds", source.BulletDuration, definition?.RandomShoot?.ProjectileLifetimeGameSeconds);
            public float BulletDistance => BulletSpeed * BulletDuration;
            public string BulletId => source.BulletId;
            public string BulletMoverId => source.BulletMoverId;
            public bool DisableRecoil => source.DisableRecoil;
            public bool DisableShootEffects => source.DisableShootEffects;
            public string FireSound => source.FireSound;
        }
        private static MonsterNativeAttackInfo DescribeRandomShoot(IRandomShoot a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            var fields = new List<MonsterNativeAttackFieldInfo> {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "registration; next available attack; preserve remaining cooldown"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; no live rearming"),
                F("damage", a.Damage, "damage", "each projectile hit, including already-fired projectiles"),
                F("criticalRate", a.CriticalRate, "fraction", "each projectile hit"),
                F("randomShoot.minBulletCount", a.BulletCountRange.x, "count", "Invoke samples one inclusive count; reversed/equal bounds use min, at least one"),
                F("randomShoot.maxBulletCount", a.BulletCountRange.y, "count", "Invoke samples once; current count is unchanged by later edits"),
                F("randomShoot.shotIntervalGameSeconds", a.ShootInterval, "game-seconds", "Invoke resets timer and captures interval; at most one bullet per update, including zero interval"),
                F("randomShoot.projectileLifetimeGameSeconds", a.BulletDuration, "game-seconds", "each fire queue insertion; existing bullets retain lifetime") };
            bool linear = DolocAPI.assets.bulletMovers.QueryData(a.BulletMoverId, out var mover) && mover is BulletMoverProtoLinear;
            if (linear) fields.Add(F("randomShoot.projectileSpeed", a.BulletSpeed, "world-units-per-game-second", "each fire queue insertion; existing bullets retain speed"));
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "random-shoot", fields,
                "Bullet=" + a.BulletId + "; mover=" + a.BulletMoverId + ". Native direction is randomized per shot. Interval includes the wait before the first shot; timer retains excess time without a catch-up loop. " +
                "No windup, recoil or range parameter is consumed. Outer target/cooldown gates still apply. Part identity, resources, sounds and effects remain inherited." +
                (linear ? "" : " Projectile speed editing requires a proven linear mover."));
        }
    }
}
