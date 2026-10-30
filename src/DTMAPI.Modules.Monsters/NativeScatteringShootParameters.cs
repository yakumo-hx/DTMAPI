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
        private sealed class ScatteringShootParameters : IScatteringShoot, INativeAttackParameters
        {
            private readonly IScatteringShoot source;
            private readonly MonsterNativeAttackParameters? definition;
            private readonly NativeModule? module;
            private readonly string target = "";
            internal ScatteringShootParameters(IScatteringShoot source, MonsterNativeAttackParameters value) { this.source = source; definition = value; }
            internal ScatteringShootParameters(IScatteringShoot source, NativeModule module, string target) { this.source = source; this.module = module; this.target = target; }
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
            public bool HasReadyAction => Read("scatteringShoot.windupEnabled", source.HasReadyAction ? 1 : 0, Flag(definition?.ScatteringShoot?.WindupEnabled)) != 0;
            public float ReadyDuration => Read("windupGameSeconds", source.ReadyDuration, definition?.WindupGameSeconds);
            public Vector2Int BulletCountRange => new Vector2Int(
                (int)Read("scatteringShoot.minBulletCount", source.BulletCountRange.x, definition?.ScatteringShoot?.MinBulletCount),
                (int)Read("scatteringShoot.maxBulletCount", source.BulletCountRange.y, definition?.ScatteringShoot?.MaxBulletCount));
            public float SectorAngle => Read("scatteringShoot.sectorAngleDegrees", source.SectorAngle, definition?.ScatteringShoot?.SectorAngleDegrees);
            public int Times => (int)Read("scatteringShoot.burstCount", source.Times, definition?.ScatteringShoot?.BurstCount);
            public float TimeInterval => Read("scatteringShoot.burstIntervalGameSeconds", source.TimeInterval, definition?.ScatteringShoot?.BurstIntervalGameSeconds);
            public IScatteringShoot.MultiScatteringType ScatteringType => (IScatteringShoot.MultiScatteringType)(int)Read("scatteringShoot.directionMode", (int)source.ScatteringType,
                definition?.ScatteringShoot?.DirectionMode.HasValue == true ? (float)definition.ScatteringShoot.DirectionMode.Value : (float?)null);
            public float AngleOffset => Read("scatteringShoot.angleOffsetDegrees", source.AngleOffset, definition?.ScatteringShoot?.AngleOffsetDegrees);
            public bool RevertAngleOffset => Read("scatteringShoot.reverseAngleOffset", source.RevertAngleOffset ? 1 : 0, Flag(definition?.ScatteringShoot?.ReverseAngleOffset)) != 0;
            public float BulletSpeed => Read("scatteringShoot.projectileSpeed", source.BulletSpeed, definition?.ScatteringShoot?.ProjectileSpeed);
            public float BulletDuration => Read("scatteringShoot.projectileLifetimeGameSeconds", source.BulletDuration, definition?.ScatteringShoot?.ProjectileLifetimeGameSeconds);
            public float BulletDistance => BulletSpeed * BulletDuration;
            public float ShootDuration => source.ShootDuration; // Present in the native interface, unused by this executor.
            public string BulletId => source.BulletId;
            public string BulletMoverId => source.BulletMoverId;
            public bool DisableRecoil => source.DisableRecoil;
            public bool DisableShootEffects => source.DisableShootEffects;
            public string FireSound => source.FireSound;
        }
        private static MonsterNativeAttackInfo DescribeScatteringShoot(IScatteringShoot a, bool definition, bool native)
        {
            MonsterNativeAttackFieldInfo F(string field, double value, string unit, string at) => new MonsterNativeAttackFieldInfo(field, value, unit, at, definition, native);
            var fields = new List<MonsterNativeAttackFieldInfo> {
                F("cooldownGameSeconds", a.CDDuration, "game-seconds", "definition: registration; native override: next available attack; preserve current remaining wait"),
                F("startOnCooldown", a.CDOnStart ? 1 : 0, "boolean", "spawn/reentry registration; existing live cooldown is unchanged"),
                F("damage", a.Damage, "damage", "each native projectile hit, including already-fired projectiles"),
                F("criticalRate", a.CriticalRate, "fraction", "each native projectile hit, including already-fired projectiles"),
                F("windupGameSeconds", a.ReadyDuration, "game-seconds", "initial windup creation when enabled; all per-burst windups fixed at sequence creation even when initial windup is disabled"),
                F("scatteringShoot.windupEnabled", a.HasReadyAction ? 1 : 0, "boolean", "Invoke; gates only extra initial windup, never the per-burst windups"),
                F("scatteringShoot.minBulletCount", a.BulletCountRange.x, "bullets-per-burst", "each actual burst: inclusive random range; max <= min selects min; at least one bullet"),
                F("scatteringShoot.maxBulletCount", a.BulletCountRange.y, "bullets-per-burst", "each actual burst; an already-queued burst retains its count"),
                F("scatteringShoot.sectorAngleDegrees", a.SectorAngle, "degrees", "each actual burst; half-open sector -angle/2 + i*angle/count; one bullet keeps captured direction"),
                F("scatteringShoot.burstCount", a.Times, "bursts", "sequence creation after optional initial windup; fixed for this attack"),
                F("scatteringShoot.burstIntervalGameSeconds", a.TimeInterval, "game-seconds", "all intervals fixed at sequence creation; no trailing interval after the last burst"),
                F("scatteringShoot.directionMode", (int)a.ScatteringType, "enum:Fixed,Random,Player,Offset", "sequence creation: Fixed and Player both capture target once; Random draws each direction at construction; Offset rotates cumulatively"),
                F("scatteringShoot.angleOffsetDegrees", a.AngleOffset, "degrees", "sequence creation in Offset mode; relative directions 0, 2a, 5a, 9a; with reversal 0, 2a, a, 3a"),
                F("scatteringShoot.reverseAngleOffset", a.RevertAngleOffset ? 1 : 0, "boolean", "sequence creation in Offset mode; negate accumulator after each rotation"),
                F("scatteringShoot.projectileLifetimeGameSeconds", a.BulletDuration, "game-seconds", "each actual fire queue insertion; prior queue entries/projectiles retain lifetime") };
            bool linear = DolocAPI.assets.bulletMovers.QueryData(a.BulletMoverId, out var mover) && mover is BulletMoverProtoLinear;
            if (linear) fields.Add(F("scatteringShoot.projectileSpeed", a.BulletSpeed, "world-units-per-game-second", "each actual fire queue insertion; linear mover normalizes nonzero random directions"));
            return new MonsterNativeAttackInfo(a.AttackId.ToString(), "scattering-shoot", fields,
                "Bullet=" + a.BulletId + "; mover=" + a.BulletMoverId + ". Every burst has ReadyDuration; initial HasReadyAction adds one extra windup. Player is identical to Fixed in this native executor. ShootDuration is unused. Burst count/timing/directions are captured, but bullet count/sector are read per burst. Resources/mover/sound/effects remain inherited." +
                (linear ? "" : " This mover's speed consumption is unproven; speed editing is unavailable."));
        }
    }
}
