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
        private sealed class FungusShootParameters : IFungusShoot, INativeAttackParameters
        {
            private readonly IFungusShoot source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal FungusShootParameters(IFungusShoot source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal FungusShootParameters(IFungusShoot source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => Read("fungusShoot.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.FungusShoot?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public float BulletSpeed => Read("fungusShoot.projectileSpeed", source.BulletSpeed, definition?.FungusShoot?.ProjectileSpeed);
            public float BulletDuration => Read("fungusShoot.projectileLifetimeGameSeconds", source.BulletDuration, definition?.FungusShoot?.ProjectileLifetimeGameSeconds);
            public float BulletDistance => BulletSpeed * BulletDuration;
            public string BulletId => source.BulletId;
            public string BulletMoverId => source.BulletMoverId;
            public bool DisableRecoil => source.DisableRecoil;
            public bool DisableShootEffects => source.DisableShootEffects;
            public string FireSound => source.FireSound;
        }
        private static void FungusShootStarted(MonsterAttackBehaviourFungusShoot __instance)
        {
            if (current == null || !(__instance._proto is FungusShootParameters parameters) ||
                parameters.HasReadyAction && parameters.ReadyDuration > 0) return;
            // The native immediate overload does not initialize the helper name or old wait.
            // A zero-duration ready overload initializes the name but never starts animation.
            // Limit this correction to adapted attacks; retain the real clip and its event.
            var waiter = NativeAccess.Get<MonsterAttackHelperAnim>(__instance, "waiter");
            NativeAccess.Set(waiter, "shouldWait", false);
            NativeAccess.Set(waiter, "waitDuration", 0f);
            NativeAccess.Set(waiter, "callbackOnReady", null);
            waiter.Invoke(0f, "attack");
            if (parameters.HasReadyAction) waiter.Invoke("attack");
            // Commit Animator.Play before a same-frame native Update can inspect the old idle state.
            NativeAccess.Get<UnityEngine.Animator>(waiter, "animator").Update(0f);
        }
        private static MonsterNativeAttackInfo DescribeFungusShoot(IFungusShoot a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            var fields = new List<MonsterNativeAttackFieldInfo> {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition registration; native next available attack; preserve the current remaining wait"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; no live cooldown rearming"),
                F("damage", a.Damage, "damage", "each native projectile hit, including already-fired projectiles"),
                F("criticalRate", a.CriticalRate, "fraction", "each native projectile hit, including already-fired projectiles"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "Invoke captures enabled wait before the attack animation; current wait stays fixed"),
                F("fungusShoot.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "Invoke; disabled starts the native attack animation immediately"),
                F("fungusShoot.projectileLifetimeGameSeconds", a.BulletDuration, "game-seconds", "native animation event; fire queue insertion fixes each lifetime") };
            bool drop = DolocAPI.assets.bulletMovers.QueryData(a.BulletMoverId, out var mover) && mover is BulletMoverProtoDrop;
            if (drop) fields.Add(F("fungusShoot.projectileSpeed", a.BulletSpeed, "world-units-per-game-second", "native animation event; direction times speed initializes Drop velocity; gravity stays inherited"));
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "fungus-shoot", fields,
                "Bullet=" + a.BulletId + "; mover=" + a.BulletMoverId + ". The native attack animation event chooses target left/right and emits exactly three half-open sector directions across 90 degrees around normalized(sign(dx),1). Range is speed * lifetime plus native ray testing. Zero windup starts the real animation; it does not skip the animation delay. The current fungus prefab has the base MonsterDecorator, not the unused fungus-specific shield/felling decorator. Damage handling, clip events, count, angle, resources, gravity and effects remain inherited." +
                (drop ? "" : " Projectile speed editing requires the proven Drop mover."));
        }
    }
}
