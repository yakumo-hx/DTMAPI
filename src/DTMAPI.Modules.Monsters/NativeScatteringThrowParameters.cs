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
        private sealed class ScatteringThrowParameters : IScatteringThrow, INativeAttackParameters
        {
            private readonly IScatteringThrow source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal ScatteringThrowParameters(IScatteringThrow source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal ScatteringThrowParameters(IScatteringThrow source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => Read("scatteringThrow.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.ScatteringThrow?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public Vector2Int BulletCountRange => new Vector2Int(
                (int)Read("scatteringThrow.minBulletCount", source.BulletCountRange.x, definition?.ScatteringThrow?.MinBulletCount),
                (int)Read("scatteringThrow.maxBulletCount", source.BulletCountRange.y, definition?.ScatteringThrow?.MaxBulletCount));
            public float SectorAngle => Read("scatteringThrow.sectorAngleDegrees", source.SectorAngle, definition?.ScatteringThrow?.SectorAngleDegrees);
            public float ShootDuration => Read("scatteringThrow.recoveryGameSeconds", source.ShootDuration, definition?.ScatteringThrow?.RecoveryGameSeconds);
            public float BulletSpeed => Read("scatteringThrow.projectileSpeed", source.BulletSpeed, definition?.ScatteringThrow?.ProjectileSpeed);
            public float BulletDuration => Read("scatteringThrow.projectileLifetimeGameSeconds", source.BulletDuration, definition?.ScatteringThrow?.ProjectileLifetimeGameSeconds);
            public float BulletDistance => BulletSpeed * BulletDuration;
            public string BulletId => source.BulletId;
            public string BulletMoverId => source.BulletMoverId;
            public bool DisableRecoil => source.DisableRecoil;
            public bool DisableShootEffects => source.DisableShootEffects;
            public string FireSound => source.FireSound;
        }
        private static MonsterNativeAttackInfo DescribeScatteringThrow(IScatteringThrow a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            var fields = new List<MonsterNativeAttackFieldInfo> {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition: registration; native override: next available attack; preserve current remaining wait"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; existing live cooldown is unchanged"),
                F("damage", a.Damage, "damage", "each native projectile hit, including already-fired projectiles"),
                F("criticalRate", a.CriticalRate, "fraction", "each native projectile hit, including already-fired projectiles"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "enabled windup tween creation; existing duration is fixed"),
                F("scatteringThrow.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "Invoke; disabled skips windup"),
                F("scatteringThrow.minBulletCount", a.BulletCountRange.x, "bullets", "actual recoil-start callback: one inclusive draw; max <= min selects min, at least one bullet"),
                F("scatteringThrow.maxBulletCount", a.BulletCountRange.y, "bullets", "same volley draw; all bullets enter the queue together"),
                F("scatteringThrow.sectorAngleDegrees", a.SectorAngle, "degrees", "actual recoil-start callback; half-open world-up sector, one bullet ignores angle"),
                F("scatteringThrow.recoveryGameSeconds", a.ShootDuration, "game-seconds", "recoil sequence creation after optional windup; volley fires at recoil start"),
                F("scatteringThrow.projectileLifetimeGameSeconds", a.BulletDuration, "game-seconds", "each fire queue insertion; previously queued projectiles retain lifetime") };
            bool knownSpeed = DolocAPI.assets.bulletMovers.QueryData(a.BulletMoverId, out var mover) && (mover is BulletMoverProtoDrop || mover is BulletMoverProtoLinear);
            if (knownSpeed) fields.Add(F("scatteringThrow.projectileSpeed", a.BulletSpeed, "world-units-per-game-second", "each fire queue insertion; Drop initializes velocity as direction * speed, then native gravity advances it"));
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "scattering-throw", fields,
                "Bullet=" + a.BulletId + "; mover=" + a.BulletMoverId + ". Volley always fans around world-up; target affects facing/recoil only. Range remains speed * lifetime plus native ray mask; SO ValidDistance is unused. Count/angle are read at the actual recoil-start callback, recoil duration at sequence creation. Drop speed is initial velocity, not constant speed. Gravity/recoil shape/resources/sound/effects remain inherited." +
                (knownSpeed ? "" : " This mover's speed consumption is unproven; speed editing is unavailable."));
        }
    }
}
