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
        private sealed class BatteringBulletParameters : IBatteringBullet, INativeAttackParameters
        {
            private readonly IBatteringBullet source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal BatteringBulletParameters(IBatteringBullet source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal BatteringBulletParameters(IBatteringBullet source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => Read("batteringBullet.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.BatteringBullet?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public Vector2Int BulletCountRange => new Vector2Int(
                (int)Read("batteringBullet.minBulletCount", source.BulletCountRange.x, definition?.BatteringBullet?.MinBulletCount),
                (int)Read("batteringBullet.maxBulletCount", source.BulletCountRange.y, definition?.BatteringBullet?.MaxBulletCount));
            public float DashDistance => Read("batteringBullet.attackDistance", source.DashDistance, definition?.BatteringBullet?.AttackDistance);
            public float DashSpeed => Read("batteringBullet.travelSpeed", source.DashSpeed, definition?.BatteringBullet?.TravelSpeed);
            public float DashDuration => DashDistance / DashSpeed;
            public DG.Tweening.Ease DashEase => source.DashEase;
            public string DashSound => source.DashSound;
            public bool HasAfterImage => source.HasAfterImage;
            public float BulletSpeed => Read("batteringBullet.projectileHorizontalSpeed", source.BulletSpeed, definition?.BatteringBullet?.ProjectileHorizontalSpeed);
            public float BulletDuration => Read("batteringBullet.projectileLifetimeGameSeconds", source.BulletDuration, definition?.BatteringBullet?.ProjectileLifetimeGameSeconds);
            public float BulletDistance => BulletSpeed * BulletDuration;
            public string BulletId => source.BulletId;
            public string BulletMoverId => source.BulletMoverId;
            public bool DisableRecoil => source.DisableRecoil;
            public bool DisableShootEffects => source.DisableShootEffects;
            public string FireSound => source.FireSound;
        }
        private static MonsterNativeAttackInfo DescribeBatteringBullet(IBatteringBullet a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            var fields = new List<MonsterNativeAttackFieldInfo> {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition: registration; native override: next available attack; preserve current remaining wait"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; existing live cooldown is unchanged"),
                F("damage", a.Damage, "damage", "body damage box captures at dash start; projectile damage reads at each hit"),
                F("criticalRate", a.CriticalRate, "fraction", "body damage box captures at dash start; projectiles read at each hit"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "enabled windup tween creation; existing duration is fixed"),
                F("batteringBullet.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "Invoke after successful native ground endpoint lookup"),
                F("batteringBullet.minBulletCount", a.BulletCountRange.x, "bullets", "impact callback after body damage box closes; one inclusive draw, max <= min selects min; native DiceCount clamps to at least one"),
                F("batteringBullet.maxBulletCount", a.BulletCountRange.y, "bullets", "same impact draw; complete volley enters the fire queue together"),
                F("batteringBullet.attackDistance", a.DashDistance, "world-units", "horizontal range check and edge recovery duration = distance / speed / 3; actual endpoint comes from terrain"),
                F("batteringBullet.travelSpeed", a.DashSpeed, "world-units-per-game-second", "after windup: actual terrain path distance / speed; wall branch duration divided by four; existing sequence is fixed"),
                F("batteringBullet.projectileLifetimeGameSeconds", a.BulletDuration, "game-seconds", "impact fire queue insertion; prior shots retain lifetime") };
            bool parabolic = DolocAPI.assets.bulletMovers.QueryData(a.BulletMoverId, out var mover) && mover is BulletMoverProtoParabolic;
            if (parabolic) fields.Add(F("batteringBullet.projectileHorizontalSpeed", a.BulletSpeed, "world-units-per-game-second", "impact queue insertion; Parabolic horizontal initial velocity = abs(randomDirection.x) * speed; vertical velocity/gravity stay inherited"));
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "battering-bullet", fields,
                "Bullet=" + a.BulletId + "; mover=" + a.BulletMoverId + "; edge travel curve=" + a.DashEase + ". Range requires absolute target height difference < 1.5. Invoke captures terrain endpoint and wall/edge branch before windup. Wall branch moves four times faster then rebounds three units over 0.5 seconds; edge branch retreats one unit over distance/speed/3. Body damage closes before the impact volley; projectile hit damage stays live. Random directions use both absolute inside-unit-circle components, without normalization or facing flip. Native effects/afterimages/sound/curves and vertical projectile motion remain inherited." +
                (parabolic ? "" : " Horizontal speed editing requires the verified Parabolic mover."));
        }
    }
}
