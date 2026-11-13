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
        private sealed class DragonShootParameters : IDragonShoot, INativeAttackParameters
        {
            private readonly IDragonShoot source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal DragonShootParameters(IDragonShoot source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal DragonShootParameters(IDragonShoot source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => Read("dragonShoot.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.DragonShoot?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public Vector2Int BulletCountRange => new Vector2Int(
                (int)Read("dragonShoot.minBulletCount", source.BulletCountRange.x, definition?.DragonShoot?.MinBulletCount),
                (int)Read("dragonShoot.maxBulletCount", source.BulletCountRange.y, definition?.DragonShoot?.MaxBulletCount));
            public int SectorCount => (int)Read("dragonShoot.sectorCount", source.SectorCount, definition?.DragonShoot?.SectorCount);
            public float SectorAngle => Read("dragonShoot.sectorAngleDegrees", source.SectorAngle, definition?.DragonShoot?.SectorAngleDegrees);
            public float ShootDuration => Read("dragonShoot.shotIntervalGameSeconds", source.ShootDuration, definition?.DragonShoot?.ShotIntervalGameSeconds);
            public float BulletSpeed => Read("dragonShoot.projectileSpeed", source.BulletSpeed, definition?.DragonShoot?.ProjectileSpeed);
            public float BulletDuration => Read("dragonShoot.projectileLifetimeGameSeconds", source.BulletDuration, definition?.DragonShoot?.ProjectileLifetimeGameSeconds);
            public float BulletDistance => BulletSpeed * BulletDuration;
            public string BulletId => source.BulletId;
            public string BulletMoverId => source.BulletMoverId;
            public bool DisableRecoil => source.DisableRecoil;
            public bool DisableShootEffects => source.DisableShootEffects;
            public string FireSound => source.FireSound;
        }
        private static MonsterNativeAttackInfo DescribeDragonShoot(IDragonShoot a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            var fields = new List<MonsterNativeAttackFieldInfo> {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition: registration; native override: next available attack; preserve current remaining wait"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; existing live cooldown is unchanged"),
                F("damage", a.Damage, "damage", "each native projectile hit, including already-fired projectiles"),
                F("criticalRate", a.CriticalRate, "fraction", "each native projectile hit, including already-fired projectiles"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "enabled windup tween creation; existing duration is fixed"),
                F("dragonShoot.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "native Invoke; disabled skips windup"),
                F("dragonShoot.minBulletCount", a.BulletCountRange.x, "bullets", "once at sweep creation after optional windup; inclusive dice, max <= min selects min, at least one bullet"),
                F("dragonShoot.maxBulletCount", a.BulletCountRange.y, "bullets", "same single draw; the current sweep retains its count"),
                F("dragonShoot.sectorCount", a.SectorCount, "directions", "sweep creation; half-open directions traversed forward/backward with both endpoints repeated"),
                F("dragonShoot.sectorAngleDegrees", a.SectorAngle, "degrees", "sweep creation; -angle/2 + i*angle/sectorCount; one sector keeps captured target direction"),
                F("dragonShoot.shotIntervalGameSeconds", a.ShootDuration, "game-seconds", "all intervals fixed at sweep sequence creation, including the interval after its last shot"),
                F("dragonShoot.projectileLifetimeGameSeconds", a.BulletDuration, "game-seconds", "each actual fire queue insertion; prior queue entries/projectiles retain lifetime") };
            bool linear = DolocAPI.assets.bulletMovers.QueryData(a.BulletMoverId, out var mover) && mover is BulletMoverProtoLinear;
            if (linear) fields.Add(F("dragonShoot.projectileSpeed", a.BulletSpeed, "world-units-per-game-second", "each actual fire queue insertion; prior queue entries/projectiles retain speed"));
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "dragon-shoot", fields,
                "Bullet=" + a.BulletId + "; mover=" + a.BulletMoverId + ". Range is speed * lifetime plus native ray mask. First shot is at sequence start; trailing interval remains. Sector directions are half-open; sector count 1 keeps target direction. Direction indices for three sectors repeat 0,1,2,2,1,0. Each endpoint is repeated on reversal. Count, sector geometry and all intervals are captured after optional windup. Resources/mover/sound/effects remain inherited." +
                (linear ? "" : " This mover's speed consumption is unproven; speed editing is unavailable."));
        }
    }
}
