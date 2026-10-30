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
        private sealed class BombingDiveParameters : IBombingDive, INativeAttackParameters
        {
            private readonly IBombingDive source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal BombingDiveParameters(IBombingDive source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal BombingDiveParameters(IBombingDive source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => Read("bombingDive.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.BombingDive?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public Vector2Int BulletCountRange => new Vector2Int(
                (int)Read("bombingDive.minBulletCount", source.BulletCountRange.x, definition?.BombingDive?.MinBulletCount),
                (int)Read("bombingDive.maxBulletCount", source.BulletCountRange.y, definition?.BombingDive?.MaxBulletCount));
            public float ValidDistance => Read("bombingDive.range", source.ValidDistance, definition?.BombingDive?.Range);
            public float DashDistance => Read("bombingDive.travelDistance", source.DashDistance, definition?.BombingDive?.TravelDistance);
            public float DashSpeed => Read("bombingDive.travelSpeed", source.DashSpeed, definition?.BombingDive?.TravelSpeed);
            public float DashDuration => DashDistance / DashSpeed;
            public DG.Tweening.Ease DashEase => source.DashEase;
            public string DashSound => source.DashSound;
            public bool HasAfterImage => source.HasAfterImage;
            public float BulletSpeed => source.BulletSpeed;
            public float BulletDuration => Read("bombingDive.projectileLifetimeGameSeconds", source.BulletDuration, definition?.BombingDive?.ProjectileLifetimeGameSeconds);
            public float BulletDistance => BulletSpeed * BulletDuration;
            public string BulletId => source.BulletId;
            public string BulletMoverId => source.BulletMoverId;
            public bool DisableRecoil => source.DisableRecoil;
            public bool DisableShootEffects => source.DisableShootEffects;
            public string FireSound => source.FireSound;
        }
        private static MonsterNativeAttackInfo DescribeBombingDive(IBombingDive a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            var fields = new List<MonsterNativeAttackFieldInfo> {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition: registration; native override: next available attack; preserve current remaining wait"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; existing live cooldown is unchanged"),
                F("damage", a.Damage, "damage", "each native projectile hit, including already-fired projectiles"),
                F("criticalRate", a.CriticalRate, "fraction", "each native projectile hit, including already-fired projectiles"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "enabled windup tween creation; existing duration is fixed"),
                F("bombingDive.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "Invoke; disabled creates the travel/fire sequence immediately"),
                F("bombingDive.minBulletCount", a.BulletCountRange.x, "bullets", "sequence creation after optional windup: one inclusive draw; max <= min selects min"),
                F("bombingDive.maxBulletCount", a.BulletCountRange.y, "bullets", "same draw; complete fire timing is captured using travel duration / count"),
                F("bombingDive.range", a.ValidDistance, "world-units", "each native range check: FirePosition to target center; target above host still rejects"),
                F("bombingDive.travelDistance", a.DashDistance, "world-units", "native horizontal wall check and travel sequence creation; old path remains fixed"),
                F("bombingDive.travelSpeed", a.DashSpeed, "world-units-per-game-second", "sequence creation: duration = distance / positive speed; native curve is inherited"),
                F("bombingDive.projectileLifetimeGameSeconds", a.BulletDuration, "game-seconds", "each fire queue insertion; previously queued projectiles retain lifetime") };
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "bombing-dive", fields,
                "Bullet=" + a.BulletId + "; mover=" + a.BulletMoverId + "; travel curve=" + a.DashEase + ". Moves horizontally toward the target side, firing upward from the current FirePosition. Range uses ValidDistance, target height and a horizontal ground ray, not bullet speed * lifetime. No extra collision check during travel. Direction/path/count/timing are captured after windup; every shot has a following interval. DashSound/HasAfterImage are not consumed here. Curves/backstep/resources/sound/effects remain inherited. The parabolic mover multiplies horizontal BulletSpeed by direction.x=0; vertical speed and gravity come from the inherited mover. Projectile speed editing is unavailable.");
        }
    }
}
