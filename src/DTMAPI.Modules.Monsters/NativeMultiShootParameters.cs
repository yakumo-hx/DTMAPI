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
        private sealed class MultiShootParameters : IMultiShoot, INativeAttackParameters
        {
            private readonly IMultiShoot source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal MultiShootParameters(IMultiShoot source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal MultiShootParameters(IMultiShoot source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => Read("multiShoot.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.MultiShoot?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public float ShootDuration => Read("multiShoot.shotGameSeconds", source.ShootDuration, definition?.MultiShoot?.ShotGameSeconds);
            public Vector2Int BulletCountRange => new Vector2Int(
                (int)Read("multiShoot.minShotCount", source.BulletCountRange.x, definition?.MultiShoot?.MinShotCount),
                (int)Read("multiShoot.maxShotCount", source.BulletCountRange.y, definition?.MultiShoot?.MaxShotCount));
            public bool IsFixedDir => Read("multiShoot.fixedDirection", source.IsFixedDir ? 1 : 0, Flag(definition?.MultiShoot?.FixedDirection)) != 0;
            public float BulletSpeed => Read("multiShoot.projectileSpeed", source.BulletSpeed, definition?.MultiShoot?.ProjectileSpeed);
            public float BulletDuration => Read("multiShoot.projectileLifetimeGameSeconds", source.BulletDuration, definition?.MultiShoot?.ProjectileLifetimeGameSeconds);
            public float BulletDistance => BulletSpeed * BulletDuration;
            public string BulletId => source.BulletId;
            public string BulletMoverId => source.BulletMoverId;
            public bool DisableRecoil => source.DisableRecoil;
            public bool DisableShootEffects => source.DisableShootEffects;
            public string FireSound => source.FireSound;
        }
        private static void MultiShootStarting(MonsterAttackBehaviourMultiShoot __instance)
        {
            if (current == null || !(__instance._proto is MultiShootParameters parameters) || parameters.HasReadyAction) return;
            // Native Invoke only initializes this cache after a windup. Initialize
            // every adapted no-windup volley so a later hot fixed-mode edit also
            // uses this volley's target, never a pooled or previous attack value.
            var controller = NativeAccess.Get<MonsterController>(__instance, "_controller");
            var target = NativeAccess.Get<Transform>(__instance, "<target>k__BackingField");
            NativeAccess.Set(__instance, "fixedDir", controller.DirToTarget(target));
        }
        private static MonsterNativeAttackInfo DescribeMultiShoot(IMultiShoot a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            var fields = new List<MonsterNativeAttackFieldInfo> {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition: registration; native override: next available attack; preserve current remaining wait"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; existing live cooldown is unchanged"),
                F("damage", a.Damage, "damage", "each native projectile hit, including already-fired projectiles"),
                F("criticalRate", a.CriticalRate, "fraction", "each native projectile hit, including already-fired projectiles"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "enabled windup tween creation; existing duration is fixed"),
                F("multiShoot.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "native Invoke; disabled skips windup"),
                F("multiShoot.shotGameSeconds", a.ShootDuration, "game-seconds", "each shot's recoil/delay creation; existing tween duration is fixed"),
                F("multiShoot.minShotCount", a.BulletCountRange.x, "shots", "once after windup, or at Invoke without windup; remaining volley count is fixed"),
                F("multiShoot.maxShotCount", a.BulletCountRange.y, "shots", "same native inclusive dice; max <= min selects min; at least one shot"),
                F("multiShoot.fixedDirection", a.IsFixedDir ? 1 : 0, "boolean", "each shot's recoil creation; fixed direction captured after windup or at no-windup Invoke"),
                F("multiShoot.projectileLifetimeGameSeconds", a.BulletDuration, "game-seconds", "each actual fire queue insertion; prior queue entries/projectiles retain lifetime") };
            bool linear = DolocAPI.assets.bulletMovers.QueryData(a.BulletMoverId, out var mover) && mover is BulletMoverProtoLinear;
            if (linear) fields.Add(F("multiShoot.projectileSpeed", a.BulletSpeed, "world-units-per-game-second", "each actual fire queue insertion; prior queue entries/projectiles retain speed"));
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "multi-shoot", fields,
                "Bullet=" + a.BulletId + "; mover=" + a.BulletMoverId + ". Native recoil fires at start; bastion fires after its delay. Range is speed * lifetime plus native ray mask. Fixed direction supports disabled windup on adapted attacks. Resources, sounds, effects and recoil shape remain inherited." +
                (linear ? "" : " Projectile speed editing requires a proven linear mover."));
        }
    }
}
