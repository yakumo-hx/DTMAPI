using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MonsterContent
{
    /// <summary>Edits one existing native slot. Null fields inherit; only the catalog's exact field capabilities are admitted.</summary>
    public sealed class MonsterNativeAttackParameters
    {
        public string AttackId { get; set; } = "";
        public float? CooldownGameSeconds { get; set; }
        /// <summary>Whether native attack registration starts the slot on cooldown, including controller reentry. Does not rearm a live slot.</summary>
        public bool? StartOnCooldown { get; set; }
        public float? Damage { get; set; }
        public float? CriticalRate { get; set; }
        public float? WindupGameSeconds { get; set; }
        /// <summary>Replaces this slot's native FireSound event. Each native post has an independent one-shot voice.</summary>
        public MonsterSound? FireSound { get; set; }
        /// <summary>Replaces each actual native DashSound post, including each MultiImpact segment.</summary>
        public MonsterSound? DashSound { get; set; }
        /// <summary>Replaces each successful native Landmine/TimeBomb placement sound; not the skill explosion.</summary>
        public MonsterSound? ThrowSound { get; set; }
        /// <summary>Replaces the BulletTrain per-volley event, independently from per-bullet FireSound.</summary>
        public MonsterSound? VolleySound { get; set; }
        /// <summary>Native preparation one-shot. Shared ResourcePath survives ordinary cancel; actual 104 stops all related countdown requests in the same rental. Legacy WAV keeps its previous policy.</summary>
        public MonsterSound? ReadySound { get; set; }
        /// <summary>ResourcePath replaces this slot's Landmine/TimeBomb landing event at its independent skill; native Monster spatial route.</summary>
        public MonsterSound? LandSound { get; set; }
        /// <summary>ResourcePath plays once per native skill countdown step; 104 stops only this skill rental's countdown requests, recycle also stops landing.</summary>
        public MonsterSound? CountdownSound { get; set; }
        /// <summary>ResourcePath selects finite MonsterExplosion: native Monster positioning, SFX_ACTOR output; natural skill/Boom recycle preserves the frozen tail, explicit cancellation stops it.</summary>
        public MonsterSound? ExplosionSound { get; set; }
        public MonsterAmoebaJumpParameters? AmoebaJump { get; set; }
        public MonsterShootParameters? Shoot { get; set; }
        public MonsterRandomShootParameters? RandomShoot { get; set; }
        public MonsterMultiShootParameters? MultiShoot { get; set; }
        public MonsterSalvoShootParameters? SalvoShoot { get; set; }
        public MonsterScatteringShootParameters? ScatteringShoot { get; set; }
        public MonsterDragonShootParameters? DragonShoot { get; set; }
        public MonsterScatteringThrowParameters? ScatteringThrow { get; set; }
        public MonsterBombingDiveParameters? BombingDive { get; set; }
        public MonsterBatteringBulletParameters? BatteringBullet { get; set; }
        public MonsterBatteringParameters? Battering { get; set; }
        public MonsterFungusShootParameters? FungusShoot { get; set; }
        public MonsterStingParameters? Sting { get; set; }
        public MonsterImpactParameters? Impact { get; set; }
        public MonsterMultiImpactParameters? MultiImpact { get; set; }
        public MonsterBulletTrainParameters? BulletTrain { get; set; }
        public MonsterShadowImpactParameters? ShadowImpact { get; set; }
        public MonsterCallParameters? Call { get; set; }
        public MonsterLandmineParameters? Landmine { get; set; }
        public MonsterTimeBombParameters? TimeBomb { get; set; }
        public MonsterThunderPileParameters? ThunderPile { get; set; }
        public MonsterChomperMimicryParameters? ChomperMimicry { get; set; }
        public MonsterChomperBiteParameters? ChomperBite { get; set; }
        internal MonsterNativeAttackParameters Copy()
        { var result = (MonsterNativeAttackParameters)MemberwiseClone(); result.FireSound = FireSound?.Copy(); result.DashSound = DashSound?.Copy(); result.ThrowSound = ThrowSound?.Copy(); result.VolleySound = VolleySound?.Copy(); result.ReadySound = ReadySound?.Copy(); result.LandSound = LandSound?.Copy(); result.CountdownSound = CountdownSound?.Copy(); result.ExplosionSound = ExplosionSound?.Copy(); result.AmoebaJump = AmoebaJump?.Copy(); result.Shoot = Shoot?.Copy(); result.RandomShoot = RandomShoot?.Copy(); result.MultiShoot = MultiShoot?.Copy(); result.SalvoShoot = SalvoShoot?.Copy(); result.ScatteringShoot = ScatteringShoot?.Copy(); result.DragonShoot = DragonShoot?.Copy(); result.ScatteringThrow = ScatteringThrow?.Copy(); result.BombingDive = BombingDive?.Copy(); result.BatteringBullet = BatteringBullet?.Copy(); result.Battering = Battering?.Copy(); result.FungusShoot = FungusShoot?.Copy(); result.Sting = Sting?.Copy(); result.Impact = Impact?.Copy(); result.MultiImpact = MultiImpact?.Copy(); result.BulletTrain = BulletTrain?.Copy(); result.ShadowImpact = ShadowImpact?.Copy(); result.Call = Call?.Copy(); result.Landmine = Landmine?.Copy(); result.TimeBomb = TimeBomb?.Copy(); result.ThunderPile = ThunderPile?.Copy(); result.ChomperMimicry = ChomperMimicry?.Copy(); result.ChomperBite = ChomperBite?.Copy(); return result; }
    }
    /// <summary>Native Call parameters. Null inherits each leaf; the limit counts all monsters in the room.</summary>
    public sealed class MonsterCallParameters
    {
        public int? MinCount { get; set; }
        public int? MaxCount { get; set; }
        /// <summary>Zero suppresses allocation; this is not a per-summoner limit.</summary>
        public int? RoomLimit { get; set; }
        internal MonsterCallParameters Copy() => (MonsterCallParameters)MemberwiseClone();
    }
    /// <summary>Native placement parameters. Recoil follows placement; impulse is not velocity.</summary>
    public sealed class MonsterLandmineParameters
    {
        public float? Range { get; set; }
        public float? RecoilGameSeconds { get; set; }
        public float? ThrowImpulse { get; set; }
        public float? LiveGameSeconds { get; set; }
        public float? ReactionGameSeconds { get; set; }
        internal MonsterLandmineParameters Copy() => (MonsterLandmineParameters)MemberwiseClone();
    }
    /// <summary>Native placement parameters. Recoil follows placement; impulse is not velocity.</summary>
    public sealed class MonsterTimeBombParameters
    {
        public float? Range { get; set; }
        public float? RecoilGameSeconds { get; set; }
        public float? ThrowImpulse { get; set; }
        public int? Count { get; set; }
        public float? MaxThrowImpulse { get; set; }
        internal MonsterTimeBombParameters Copy() => (MonsterTimeBombParameters)MemberwiseClone();
    }
    public sealed class MonsterThunderPileParameters
    {
        public bool? WindupEnabled { get; set; }
        public float? Range { get; set; }
        public int? MinCount { get; set; }
        public int? MaxCount { get; set; }
        public float? SubWindupGameSeconds { get; set; }
        public float? DropSpeed { get; set; }
        public float? MaxDropGameSeconds { get; set; }
        public float? WaitGameSeconds { get; set; }
        public float? FadeGameSeconds { get; set; }
        internal MonsterThunderPileParameters Copy() => (MonsterThunderPileParameters)MemberwiseClone();
    }
    public sealed class MonsterChomperMimicryParameters
    {
        public float? MinDecoyGameSeconds { get; set; }
        public float? MaxDecoyGameSeconds { get; set; }
        internal MonsterChomperMimicryParameters Copy() => (MonsterChomperMimicryParameters)MemberwiseClone();
    }
    public sealed class MonsterShadowImpactParameters
    {
        public bool? WindupEnabled { get; set; }
        public float? Distance { get; set; }
        public float? TravelSpeed { get; set; }
        internal MonsterShadowImpactParameters Copy() => (MonsterShadowImpactParameters)MemberwiseClone();
    }
    public sealed class MonsterBulletTrainParameters
    {
        public bool? WindupEnabled { get; set; }
        public int? VolleyCount { get; set; }
        public float? VolleyIntervalGameSeconds { get; set; }
        public int? BulletCount { get; set; }
        public int? SectorAngleDegrees { get; set; }
        /// <summary>Literal native direction component; not normalized or rotated toward the target.</summary>
        public float? DirectionX { get; set; }
        public float? DirectionY { get; set; }
        /// <summary>Also determines the native body's horizontal distance at sequence construction.</summary>
        public float? ProjectileSpeed { get; set; }
        public float? ProjectileLifetimeGameSeconds { get; set; }
        internal MonsterBulletTrainParameters Copy() => (MonsterBulletTrainParameters)MemberwiseClone();
    }
    public sealed class MonsterImpactParameters
    {
        public bool? WindupEnabled { get; set; }
        /// <summary>Target-distance bound and displacement captured after optional windup.</summary>
        public float? Distance { get; set; }
        /// <summary>Distance / duration, not arc length or constant physical velocity.</summary>
        public float? TravelSpeed { get; set; }
        /// <summary>True selects native Arc, false Linear; null preserves the raw native path.</summary>
        public bool? ArcPath { get; set; }
        /// <summary>Signed control-point offset from the chord midpoint, positive left of travel. Not maximum arc height.</summary>
        public float? ArcOffset { get; set; }
        internal MonsterImpactParameters Copy() => (MonsterImpactParameters)MemberwiseClone();
    }
    /// <summary>Native repeated straight dashes. Drawn extra dashes plus one gives the actual total.</summary>
    public sealed class MonsterMultiImpactParameters
    {
        public bool? WindupEnabled { get; set; }
        public float? Distance { get; set; }
        /// <summary>Distance divided by duration for each leg; inherited easing remains active.</summary>
        public float? TravelSpeed { get; set; }
        public int? MinExtraDashes { get; set; }
        public int? MaxExtraDashes { get; set; }
        internal MonsterMultiImpactParameters Copy() => (MonsterMultiImpactParameters)MemberwiseClone();
    }
    public sealed class MonsterChomperBiteParameters
    {
        public bool? WindupEnabled { get; set; }
        /// <summary>Target-distance gate only; does not resize the native animation's damage collider.</summary>
        public float? Range { get; set; }
        internal MonsterChomperBiteParameters Copy() => (MonsterChomperBiteParameters)MemberwiseClone();
    }
    public sealed class MonsterStingParameters
    {
        public bool? WindupEnabled { get; set; }
        /// <summary>Both the target-distance bound and forward displacement; native return keeps its original endpoint.</summary>
        public float? Distance { get; set; }
        /// <summary>Nominal distance divided by total out-and-back duration, not constant physical velocity.</summary>
        public float? TravelSpeed { get; set; }
        internal MonsterStingParameters Copy() => (MonsterStingParameters)MemberwiseClone();
    }
    public sealed class MonsterFungusShootParameters
    {
        public bool? WindupEnabled { get; set; }
        /// <summary>Initial speed for the native Drop mover; gravity remains inherited.</summary>
        public float? ProjectileSpeed { get; set; }
        public float? ProjectileLifetimeGameSeconds { get; set; }
        internal MonsterFungusShootParameters Copy() => (MonsterFungusShootParameters)MemberwiseClone();
    }
    public sealed class MonsterBatteringParameters
    {
        public bool? WindupEnabled { get; set; }
        /// <summary>Horizontal activation range and edge-recovery numerator; terrain determines the endpoint.</summary>
        public float? AttackDistance { get; set; }
        public float? TravelSpeed { get; set; }
        internal MonsterBatteringParameters Copy() => (MonsterBatteringParameters)MemberwiseClone();
    }
    public sealed class MonsterBatteringBulletParameters
    {
        public bool? WindupEnabled { get; set; }
        /// <summary>Inclusive count drawn at impact; native DiceCount requires at least one bullet.</summary>
        public int? MinBulletCount { get; set; }
        public int? MaxBulletCount { get; set; }
        /// <summary>Horizontal activation range and edge recovery duration numerator; terrain determines the actual endpoint.</summary>
        public float? AttackDistance { get; set; }
        /// <summary>Terrain path distance / duration. Native wall branch travels four times faster.</summary>
        public float? TravelSpeed { get; set; }
        /// <summary>Parabolic horizontal initial-velocity coefficient; vertical speed and gravity remain native.</summary>
        public float? ProjectileHorizontalSpeed { get; set; }
        public float? ProjectileLifetimeGameSeconds { get; set; }
        internal MonsterBatteringBulletParameters Copy() => (MonsterBatteringBulletParameters)MemberwiseClone();
    }
    public sealed class MonsterBombingDiveParameters
    {
        public bool? WindupEnabled { get; set; }
        public int? MinBulletCount { get; set; }
        public int? MaxBulletCount { get; set; }
        /// <summary>Native distance to the target center from FirePosition. Targets above the host still reject.</summary>
        public float? Range { get; set; }
        /// <summary>Horizontal distance captured after optional windup; the native path curve is inherited.</summary>
        public float? TravelDistance { get; set; }
        /// <summary>Positive distance / duration speed; it also sets the spacing in time between bullets.</summary>
        public float? TravelSpeed { get; set; }
        public float? ProjectileLifetimeGameSeconds { get; set; }
        internal MonsterBombingDiveParameters Copy() => (MonsterBombingDiveParameters)MemberwiseClone();
    }
    public sealed class MonsterScatteringThrowParameters
    {
        public bool? WindupEnabled { get; set; }
        public int? MinBulletCount { get; set; }
        public int? MaxBulletCount { get; set; }
        /// <summary>Half-open sector around world-up, read when the recoil sequence emits its volley.</summary>
        public float? SectorAngleDegrees { get; set; }
        /// <summary>Recoil duration fixed after windup; bullets fire at the start of this interval.</summary>
        public float? RecoveryGameSeconds { get; set; }
        /// <summary>Initial velocity magnitude for the native Drop mover; gravity remains inherited.</summary>
        public float? ProjectileSpeed { get; set; }
        public float? ProjectileLifetimeGameSeconds { get; set; }
        internal MonsterScatteringThrowParameters Copy() => (MonsterScatteringThrowParameters)MemberwiseClone();
    }
    public sealed class MonsterDragonShootParameters
    {
        public bool? WindupEnabled { get; set; }
        public int? MinBulletCount { get; set; }
        public int? MaxBulletCount { get; set; }
        /// <summary>Number of half-open sector directions; the sweep repeats both endpoints when reversing.</summary>
        public int? SectorCount { get; set; }
        public float? SectorAngleDegrees { get; set; }
        /// <summary>All intervals are fixed after windup, including the interval after the final bullet.</summary>
        public float? ShotIntervalGameSeconds { get; set; }
        public float? ProjectileSpeed { get; set; }
        public float? ProjectileLifetimeGameSeconds { get; set; }
        internal MonsterDragonShootParameters Copy() => (MonsterDragonShootParameters)MemberwiseClone();
    }
    public enum MonsterScatteringMode { Fixed, Random, Player, Offset }
    public sealed class MonsterScatteringShootParameters
    {
        /// <summary>Gates only the extra initial windup; every burst still uses WindupGameSeconds.</summary>
        public bool? WindupEnabled { get; set; }
        public int? MinBulletCount { get; set; }
        public int? MaxBulletCount { get; set; }
        public float? SectorAngleDegrees { get; set; }
        public int? BurstCount { get; set; }
        /// <summary>Between bursts only; no trailing interval after the last burst.</summary>
        public float? BurstIntervalGameSeconds { get; set; }
        /// <summary>Native Player currently captures direction exactly like Fixed; it does not track per burst.</summary>
        public MonsterScatteringMode? DirectionMode { get; set; }
        /// <summary>Native cumulative offset: relative directions 0, 2a, 5a, 9a; with reversal 0, 2a, a, 3a.</summary>
        public float? AngleOffsetDegrees { get; set; }
        public bool? ReverseAngleOffset { get; set; }
        public float? ProjectileSpeed { get; set; }
        public float? ProjectileLifetimeGameSeconds { get; set; }
        internal MonsterScatteringShootParameters Copy() => (MonsterScatteringShootParameters)MemberwiseClone();
    }
    public sealed class MonsterSalvoShootParameters
    {
        public bool? WindupEnabled { get; set; }
        public int? BulletCount { get; set; }
        /// <summary>Native half-open sector: -angle/2 + i*angle/count; one bullet keeps the captured target direction.</summary>
        public float? SectorAngleDegrees { get; set; }
        /// <summary>Fixed at volley creation; includes the interval following the last bullet.</summary>
        public float? ShotIntervalGameSeconds { get; set; }
        public float? ProjectileSpeed { get; set; }
        public float? ProjectileLifetimeGameSeconds { get; set; }
        internal MonsterSalvoShootParameters Copy() => (MonsterSalvoShootParameters)MemberwiseClone();
    }
    public sealed class MonsterMultiShootParameters
    {
        public bool? WindupEnabled { get; set; }
        public float? ShotGameSeconds { get; set; }
        public int? MinShotCount { get; set; }
        public int? MaxShotCount { get; set; }
        public bool? FixedDirection { get; set; }
        public float? ProjectileSpeed { get; set; }
        public float? ProjectileLifetimeGameSeconds { get; set; }
        internal MonsterMultiShootParameters Copy() => (MonsterMultiShootParameters)MemberwiseClone();
    }
    /// <summary>Random radial bullets. Count and interval are captured at Invoke; no native windup is used.</summary>
    public sealed class MonsterRandomShootParameters
    {
        public int? MinBulletCount { get; set; }
        public int? MaxBulletCount { get; set; }
        public float? ShotIntervalGameSeconds { get; set; }
        public float? ProjectileSpeed { get; set; }
        public float? ProjectileLifetimeGameSeconds { get; set; }
        internal MonsterRandomShootParameters Copy() => (MonsterRandomShootParameters)MemberwiseClone();
    }
    public sealed class MonsterShootParameters
    {
        public bool? WindupEnabled { get; set; }
        /// <summary>Native recoil and return sequence duration after the shot is queued.</summary>
        public float? RecoveryGameSeconds { get; set; }
        public float? ProjectileSpeed { get; set; }
        public float? ProjectileLifetimeGameSeconds { get; set; }
        internal MonsterShootParameters Copy() => (MonsterShootParameters)MemberwiseClone();
    }
    public sealed class MonsterAmoebaJumpParameters
    {
        public float? Range { get; set; }
        /// <summary>Native path-distance / tween-duration speed; distinct from the Ground mover's jump speed.</summary>
        public float? Speed { get; set; }
        /// <summary>Maximum native ground-map jump distance in cells. Does not change the native height search.</summary>
        public int? DistanceCells { get; set; }
        internal MonsterAmoebaJumpParameters Copy() => (MonsterAmoebaJumpParameters)MemberwiseClone();
    }
    public sealed class MonsterNativeAttackFieldInfo
    {
        public MonsterNativeAttackFieldInfo(string field, double value, string unit, string readAt, bool definition, bool nativeOverride)
        { Field = field; Value = value; Unit = unit; ReadAt = readAt; Definition = definition; NativeOverride = nativeOverride; }
        public string Field { get; }
        public double Value { get; }
        public string Unit { get; }
        public string ReadAt { get; }
        public bool Definition { get; }
        public bool NativeOverride { get; }
    }
    public sealed class MonsterNativeAttackSoundInfo
    {
        public MonsterNativeAttackSoundInfo(string field, string readAt, bool definition, bool nativeOverride)
        { Field = field; ReadAt = readAt; Definition = definition; NativeOverride = nativeOverride; }
        public string Field { get; }
        public string ReadAt { get; }
        public bool Definition { get; }
        public bool NativeOverride { get; }
    }
    public sealed class MonsterNativeAttackInfo
    {
        public MonsterNativeAttackInfo(string attackId, string kind, IEnumerable<MonsterNativeAttackFieldInfo> fields, string limitation = "", IEnumerable<MonsterNativeAttackSoundInfo>? sounds = null)
        { AttackId = attackId; Kind = kind; Fields = Array.AsReadOnly(fields.ToArray()); Limitation = limitation; Sounds = Array.AsReadOnly((sounds ?? Array.Empty<MonsterNativeAttackSoundInfo>()).ToArray()); }
        public string AttackId { get; }
        public string Kind { get; }
        public IReadOnlyList<MonsterNativeAttackFieldInfo> Fields { get; }
        public string Limitation { get; }
        public IReadOnlyList<MonsterNativeAttackSoundInfo> Sounds { get; }
    }
    internal static class NativeAttackValidation
    {
        internal static MonsterSound? Sound(MonsterNativeAttackParameters? value, string field) =>
            field == "fireSound" ? value?.FireSound : field == "dashSound" ? value?.DashSound : field == "throwSound" ? value?.ThrowSound : field == "volleySound" ? value?.VolleySound : field == "readySound" ? value?.ReadySound : field == "landSound" ? value?.LandSound : field == "countdownSound" ? value?.CountdownSound : field == "explosionSound" ? value?.ExplosionSound : null;
        internal static IEnumerable<KeyValuePair<string, MonsterSound>> Sounds(MonsterNativeAttackParameters value)
        {
            if (value.FireSound != null) yield return new KeyValuePair<string, MonsterSound>("fireSound", value.FireSound);
            if (value.DashSound != null) yield return new KeyValuePair<string, MonsterSound>("dashSound", value.DashSound);
            if (value.ThrowSound != null) yield return new KeyValuePair<string, MonsterSound>("throwSound", value.ThrowSound);
            if (value.VolleySound != null) yield return new KeyValuePair<string, MonsterSound>("volleySound", value.VolleySound);
            if (value.ReadySound != null) yield return new KeyValuePair<string, MonsterSound>("readySound", value.ReadySound);
            if (value.LandSound != null) yield return new KeyValuePair<string, MonsterSound>("landSound", value.LandSound);
            if (value.CountdownSound != null) yield return new KeyValuePair<string, MonsterSound>("countdownSound", value.CountdownSound);
            if (value.ExplosionSound != null) yield return new KeyValuePair<string, MonsterSound>("explosionSound", value.ExplosionSound);
        }
        internal static IEnumerable<KeyValuePair<string, double>> Fields(MonsterNativeAttackParameters value)
        {
            if (value.RandomShoot?.MinBulletCount.HasValue == true) yield return Pair("randomShoot.minBulletCount", value.RandomShoot.MinBulletCount.Value);
            if (value.RandomShoot?.MaxBulletCount.HasValue == true) yield return Pair("randomShoot.maxBulletCount", value.RandomShoot.MaxBulletCount.Value);
            if (value.RandomShoot?.ShotIntervalGameSeconds.HasValue == true) yield return Pair("randomShoot.shotIntervalGameSeconds", value.RandomShoot.ShotIntervalGameSeconds.Value);
            if (value.RandomShoot?.ProjectileSpeed.HasValue == true) yield return Pair("randomShoot.projectileSpeed", value.RandomShoot.ProjectileSpeed.Value);
            if (value.RandomShoot?.ProjectileLifetimeGameSeconds.HasValue == true) yield return Pair("randomShoot.projectileLifetimeGameSeconds", value.RandomShoot.ProjectileLifetimeGameSeconds.Value);
            if (value.CooldownGameSeconds.HasValue) yield return Pair("cooldownGameSeconds", value.CooldownGameSeconds.Value);
            if (value.StartOnCooldown.HasValue) yield return Pair("startOnCooldown", value.StartOnCooldown.Value ? 1 : 0);
            if (value.Damage.HasValue) yield return Pair("damage", value.Damage.Value);
            if (value.CriticalRate.HasValue) yield return Pair("criticalRate", value.CriticalRate.Value);
            if (value.WindupGameSeconds.HasValue) yield return Pair("windupGameSeconds", value.WindupGameSeconds.Value);
            if (value.AmoebaJump?.Range.HasValue == true) yield return Pair("amoebaJump.range", value.AmoebaJump.Range.Value);
            if (value.AmoebaJump?.Speed.HasValue == true) yield return Pair("amoebaJump.speed", value.AmoebaJump.Speed.Value);
            if (value.AmoebaJump?.DistanceCells.HasValue == true) yield return Pair("amoebaJump.distanceCells", value.AmoebaJump.DistanceCells.Value);
            if (value.Shoot?.WindupEnabled.HasValue == true) yield return Pair("shoot.windupEnabled", value.Shoot.WindupEnabled.Value ? 1 : 0);
            if (value.Shoot?.RecoveryGameSeconds.HasValue == true) yield return Pair("shoot.recoveryGameSeconds", value.Shoot.RecoveryGameSeconds.Value);
            if (value.Shoot?.ProjectileSpeed.HasValue == true) yield return Pair("shoot.projectileSpeed", value.Shoot.ProjectileSpeed.Value);
            if (value.Shoot?.ProjectileLifetimeGameSeconds.HasValue == true) yield return Pair("shoot.projectileLifetimeGameSeconds", value.Shoot.ProjectileLifetimeGameSeconds.Value);
            if (value.MultiShoot?.WindupEnabled.HasValue == true) yield return Pair("multiShoot.windupEnabled", value.MultiShoot.WindupEnabled.Value ? 1 : 0);
            if (value.MultiShoot?.ShotGameSeconds.HasValue == true) yield return Pair("multiShoot.shotGameSeconds", value.MultiShoot.ShotGameSeconds.Value);
            if (value.MultiShoot?.MinShotCount.HasValue == true) yield return Pair("multiShoot.minShotCount", value.MultiShoot.MinShotCount.Value);
            if (value.MultiShoot?.MaxShotCount.HasValue == true) yield return Pair("multiShoot.maxShotCount", value.MultiShoot.MaxShotCount.Value);
            if (value.MultiShoot?.FixedDirection.HasValue == true) yield return Pair("multiShoot.fixedDirection", value.MultiShoot.FixedDirection.Value ? 1 : 0);
            if (value.MultiShoot?.ProjectileSpeed.HasValue == true) yield return Pair("multiShoot.projectileSpeed", value.MultiShoot.ProjectileSpeed.Value);
            if (value.MultiShoot?.ProjectileLifetimeGameSeconds.HasValue == true) yield return Pair("multiShoot.projectileLifetimeGameSeconds", value.MultiShoot.ProjectileLifetimeGameSeconds.Value);
            if (value.SalvoShoot?.WindupEnabled.HasValue == true) yield return Pair("salvoShoot.windupEnabled", value.SalvoShoot.WindupEnabled.Value ? 1 : 0);
            if (value.SalvoShoot?.BulletCount.HasValue == true) yield return Pair("salvoShoot.bulletCount", value.SalvoShoot.BulletCount.Value);
            if (value.SalvoShoot?.SectorAngleDegrees.HasValue == true) yield return Pair("salvoShoot.sectorAngleDegrees", value.SalvoShoot.SectorAngleDegrees.Value);
            if (value.SalvoShoot?.ShotIntervalGameSeconds.HasValue == true) yield return Pair("salvoShoot.shotIntervalGameSeconds", value.SalvoShoot.ShotIntervalGameSeconds.Value);
            if (value.SalvoShoot?.ProjectileSpeed.HasValue == true) yield return Pair("salvoShoot.projectileSpeed", value.SalvoShoot.ProjectileSpeed.Value);
            if (value.SalvoShoot?.ProjectileLifetimeGameSeconds.HasValue == true) yield return Pair("salvoShoot.projectileLifetimeGameSeconds", value.SalvoShoot.ProjectileLifetimeGameSeconds.Value);
            if (value.ScatteringShoot?.WindupEnabled.HasValue == true) yield return Pair("scatteringShoot.windupEnabled", value.ScatteringShoot.WindupEnabled.Value ? 1 : 0);
            if (value.ScatteringShoot?.MinBulletCount.HasValue == true) yield return Pair("scatteringShoot.minBulletCount", value.ScatteringShoot.MinBulletCount.Value);
            if (value.ScatteringShoot?.MaxBulletCount.HasValue == true) yield return Pair("scatteringShoot.maxBulletCount", value.ScatteringShoot.MaxBulletCount.Value);
            if (value.ScatteringShoot?.SectorAngleDegrees.HasValue == true) yield return Pair("scatteringShoot.sectorAngleDegrees", value.ScatteringShoot.SectorAngleDegrees.Value);
            if (value.ScatteringShoot?.BurstCount.HasValue == true) yield return Pair("scatteringShoot.burstCount", value.ScatteringShoot.BurstCount.Value);
            if (value.ScatteringShoot?.BurstIntervalGameSeconds.HasValue == true) yield return Pair("scatteringShoot.burstIntervalGameSeconds", value.ScatteringShoot.BurstIntervalGameSeconds.Value);
            if (value.ScatteringShoot?.DirectionMode.HasValue == true) yield return Pair("scatteringShoot.directionMode", (int)value.ScatteringShoot.DirectionMode.Value);
            if (value.ScatteringShoot?.AngleOffsetDegrees.HasValue == true) yield return Pair("scatteringShoot.angleOffsetDegrees", value.ScatteringShoot.AngleOffsetDegrees.Value);
            if (value.ScatteringShoot?.ReverseAngleOffset.HasValue == true) yield return Pair("scatteringShoot.reverseAngleOffset", value.ScatteringShoot.ReverseAngleOffset.Value ? 1 : 0);
            if (value.ScatteringShoot?.ProjectileSpeed.HasValue == true) yield return Pair("scatteringShoot.projectileSpeed", value.ScatteringShoot.ProjectileSpeed.Value);
            if (value.ScatteringShoot?.ProjectileLifetimeGameSeconds.HasValue == true) yield return Pair("scatteringShoot.projectileLifetimeGameSeconds", value.ScatteringShoot.ProjectileLifetimeGameSeconds.Value);
            if (value.DragonShoot?.WindupEnabled.HasValue == true) yield return Pair("dragonShoot.windupEnabled", value.DragonShoot.WindupEnabled.Value ? 1 : 0);
            if (value.DragonShoot?.MinBulletCount.HasValue == true) yield return Pair("dragonShoot.minBulletCount", value.DragonShoot.MinBulletCount.Value);
            if (value.DragonShoot?.MaxBulletCount.HasValue == true) yield return Pair("dragonShoot.maxBulletCount", value.DragonShoot.MaxBulletCount.Value);
            if (value.DragonShoot?.SectorCount.HasValue == true) yield return Pair("dragonShoot.sectorCount", value.DragonShoot.SectorCount.Value);
            if (value.DragonShoot?.SectorAngleDegrees.HasValue == true) yield return Pair("dragonShoot.sectorAngleDegrees", value.DragonShoot.SectorAngleDegrees.Value);
            if (value.DragonShoot?.ShotIntervalGameSeconds.HasValue == true) yield return Pair("dragonShoot.shotIntervalGameSeconds", value.DragonShoot.ShotIntervalGameSeconds.Value);
            if (value.DragonShoot?.ProjectileSpeed.HasValue == true) yield return Pair("dragonShoot.projectileSpeed", value.DragonShoot.ProjectileSpeed.Value);
            if (value.DragonShoot?.ProjectileLifetimeGameSeconds.HasValue == true) yield return Pair("dragonShoot.projectileLifetimeGameSeconds", value.DragonShoot.ProjectileLifetimeGameSeconds.Value);
            if (value.ScatteringThrow?.WindupEnabled.HasValue == true) yield return Pair("scatteringThrow.windupEnabled", value.ScatteringThrow.WindupEnabled.Value ? 1 : 0);
            if (value.ScatteringThrow?.MinBulletCount.HasValue == true) yield return Pair("scatteringThrow.minBulletCount", value.ScatteringThrow.MinBulletCount.Value);
            if (value.ScatteringThrow?.MaxBulletCount.HasValue == true) yield return Pair("scatteringThrow.maxBulletCount", value.ScatteringThrow.MaxBulletCount.Value);
            if (value.ScatteringThrow?.SectorAngleDegrees.HasValue == true) yield return Pair("scatteringThrow.sectorAngleDegrees", value.ScatteringThrow.SectorAngleDegrees.Value);
            if (value.ScatteringThrow?.RecoveryGameSeconds.HasValue == true) yield return Pair("scatteringThrow.recoveryGameSeconds", value.ScatteringThrow.RecoveryGameSeconds.Value);
            if (value.ScatteringThrow?.ProjectileSpeed.HasValue == true) yield return Pair("scatteringThrow.projectileSpeed", value.ScatteringThrow.ProjectileSpeed.Value);
            if (value.ScatteringThrow?.ProjectileLifetimeGameSeconds.HasValue == true) yield return Pair("scatteringThrow.projectileLifetimeGameSeconds", value.ScatteringThrow.ProjectileLifetimeGameSeconds.Value);
            if (value.BombingDive?.WindupEnabled.HasValue == true) yield return Pair("bombingDive.windupEnabled", value.BombingDive.WindupEnabled.Value ? 1 : 0);
            if (value.BombingDive?.MinBulletCount.HasValue == true) yield return Pair("bombingDive.minBulletCount", value.BombingDive.MinBulletCount.Value);
            if (value.BombingDive?.MaxBulletCount.HasValue == true) yield return Pair("bombingDive.maxBulletCount", value.BombingDive.MaxBulletCount.Value);
            if (value.BombingDive?.ProjectileLifetimeGameSeconds.HasValue == true) yield return Pair("bombingDive.projectileLifetimeGameSeconds", value.BombingDive.ProjectileLifetimeGameSeconds.Value);
            if (value.BombingDive?.Range.HasValue == true) yield return Pair("bombingDive.range", value.BombingDive.Range.Value);
            if (value.BombingDive?.TravelDistance.HasValue == true) yield return Pair("bombingDive.travelDistance", value.BombingDive.TravelDistance.Value);
            if (value.BombingDive?.TravelSpeed.HasValue == true) yield return Pair("bombingDive.travelSpeed", value.BombingDive.TravelSpeed.Value);
            if (value.FungusShoot?.WindupEnabled.HasValue == true) yield return Pair("fungusShoot.windupEnabled", value.FungusShoot.WindupEnabled.Value ? 1 : 0);
            if (value.FungusShoot?.ProjectileSpeed.HasValue == true) yield return Pair("fungusShoot.projectileSpeed", value.FungusShoot.ProjectileSpeed.Value);
            if (value.FungusShoot?.ProjectileLifetimeGameSeconds.HasValue == true) yield return Pair("fungusShoot.projectileLifetimeGameSeconds", value.FungusShoot.ProjectileLifetimeGameSeconds.Value);
            if (value.ChomperBite?.WindupEnabled.HasValue == true) yield return Pair("chomperBite.windupEnabled", value.ChomperBite.WindupEnabled.Value ? 1 : 0);
            if (value.ChomperBite?.Range.HasValue == true) yield return Pair("chomperBite.range", value.ChomperBite.Range.Value);
            if (value.Sting?.WindupEnabled.HasValue == true) yield return Pair("sting.windupEnabled", value.Sting.WindupEnabled.Value ? 1 : 0);
            if (value.Sting?.Distance.HasValue == true) yield return Pair("sting.distance", value.Sting.Distance.Value);
            if (value.Sting?.TravelSpeed.HasValue == true) yield return Pair("sting.travelSpeed", value.Sting.TravelSpeed.Value);
            if (value.BulletTrain?.WindupEnabled.HasValue == true) yield return Pair("bulletTrain.windupEnabled", value.BulletTrain.WindupEnabled.Value ? 1 : 0);
            if (value.BulletTrain?.VolleyCount.HasValue == true) yield return Pair("bulletTrain.volleyCount", value.BulletTrain.VolleyCount.Value);
            if (value.BulletTrain?.VolleyIntervalGameSeconds.HasValue == true) yield return Pair("bulletTrain.volleyIntervalGameSeconds", value.BulletTrain.VolleyIntervalGameSeconds.Value);
            if (value.BulletTrain?.BulletCount.HasValue == true) yield return Pair("bulletTrain.bulletCount", value.BulletTrain.BulletCount.Value);
            if (value.BulletTrain?.SectorAngleDegrees.HasValue == true) yield return Pair("bulletTrain.sectorAngleDegrees", value.BulletTrain.SectorAngleDegrees.Value);
            if (value.BulletTrain?.DirectionX.HasValue == true) yield return Pair("bulletTrain.directionX", value.BulletTrain.DirectionX.Value);
            if (value.BulletTrain?.DirectionY.HasValue == true) yield return Pair("bulletTrain.directionY", value.BulletTrain.DirectionY.Value);
            if (value.BulletTrain?.ProjectileSpeed.HasValue == true) yield return Pair("bulletTrain.projectileSpeed", value.BulletTrain.ProjectileSpeed.Value);
            if (value.BulletTrain?.ProjectileLifetimeGameSeconds.HasValue == true) yield return Pair("bulletTrain.projectileLifetimeGameSeconds", value.BulletTrain.ProjectileLifetimeGameSeconds.Value);
            if (value.MultiImpact?.WindupEnabled.HasValue == true) yield return Pair("multiImpact.windupEnabled", value.MultiImpact.WindupEnabled.Value ? 1 : 0);
            if (value.MultiImpact?.Distance.HasValue == true) yield return Pair("multiImpact.distance", value.MultiImpact.Distance.Value);
            if (value.MultiImpact?.TravelSpeed.HasValue == true) yield return Pair("multiImpact.travelSpeed", value.MultiImpact.TravelSpeed.Value);
            if (value.MultiImpact?.MinExtraDashes.HasValue == true) yield return Pair("multiImpact.minExtraDashes", value.MultiImpact.MinExtraDashes.Value);
            if (value.MultiImpact?.MaxExtraDashes.HasValue == true) yield return Pair("multiImpact.maxExtraDashes", value.MultiImpact.MaxExtraDashes.Value);
            if (value.Impact?.WindupEnabled.HasValue == true) yield return Pair("impact.windupEnabled", value.Impact.WindupEnabled.Value ? 1 : 0);
            if (value.Call?.MinCount.HasValue == true) yield return Pair("call.minCount", value.Call.MinCount.Value);
            if (value.Call?.MaxCount.HasValue == true) yield return Pair("call.maxCount", value.Call.MaxCount.Value);
            if (value.Call?.RoomLimit.HasValue == true) yield return Pair("call.roomLimit", value.Call.RoomLimit.Value);
            if (value.Landmine?.Range.HasValue == true) yield return Pair("landmine.range", value.Landmine.Range.Value);
            if (value.Landmine?.RecoilGameSeconds.HasValue == true) yield return Pair("landmine.recoilGameSeconds", value.Landmine.RecoilGameSeconds.Value);
            if (value.Landmine?.ThrowImpulse.HasValue == true) yield return Pair("landmine.throwImpulse", value.Landmine.ThrowImpulse.Value);
            if (value.Landmine?.LiveGameSeconds.HasValue == true) yield return Pair("landmine.liveGameSeconds", value.Landmine.LiveGameSeconds.Value);
            if (value.Landmine?.ReactionGameSeconds.HasValue == true) yield return Pair("landmine.reactionGameSeconds", value.Landmine.ReactionGameSeconds.Value);
            if (value.TimeBomb?.Range.HasValue == true) yield return Pair("timeBomb.range", value.TimeBomb.Range.Value);
            if (value.TimeBomb?.RecoilGameSeconds.HasValue == true) yield return Pair("timeBomb.recoilGameSeconds", value.TimeBomb.RecoilGameSeconds.Value);
            if (value.TimeBomb?.ThrowImpulse.HasValue == true) yield return Pair("timeBomb.throwImpulse", value.TimeBomb.ThrowImpulse.Value);
            if (value.TimeBomb?.Count.HasValue == true) yield return Pair("timeBomb.count", value.TimeBomb.Count.Value);
            if (value.TimeBomb?.MaxThrowImpulse.HasValue == true) yield return Pair("timeBomb.maxThrowImpulse", value.TimeBomb.MaxThrowImpulse.Value);
            if (value.ThunderPile?.WindupEnabled.HasValue == true) yield return Pair("thunderPile.windupEnabled", value.ThunderPile.WindupEnabled.Value ? 1 : 0);
            if (value.ThunderPile?.Range.HasValue == true) yield return Pair("thunderPile.range", value.ThunderPile.Range.Value);
            if (value.ThunderPile?.MinCount.HasValue == true) yield return Pair("thunderPile.minCount", value.ThunderPile.MinCount.Value);
            if (value.ThunderPile?.MaxCount.HasValue == true) yield return Pair("thunderPile.maxCount", value.ThunderPile.MaxCount.Value);
            if (value.ThunderPile?.SubWindupGameSeconds.HasValue == true) yield return Pair("thunderPile.subWindupGameSeconds", value.ThunderPile.SubWindupGameSeconds.Value);
            if (value.ThunderPile?.DropSpeed.HasValue == true) yield return Pair("thunderPile.dropSpeed", value.ThunderPile.DropSpeed.Value);
            if (value.ThunderPile?.MaxDropGameSeconds.HasValue == true) yield return Pair("thunderPile.maxDropGameSeconds", value.ThunderPile.MaxDropGameSeconds.Value);
            if (value.ThunderPile?.WaitGameSeconds.HasValue == true) yield return Pair("thunderPile.waitGameSeconds", value.ThunderPile.WaitGameSeconds.Value);
            if (value.ThunderPile?.FadeGameSeconds.HasValue == true) yield return Pair("thunderPile.fadeGameSeconds", value.ThunderPile.FadeGameSeconds.Value);
            if (value.ChomperMimicry?.MinDecoyGameSeconds.HasValue == true) yield return Pair("chomperMimicry.minDecoyGameSeconds", value.ChomperMimicry.MinDecoyGameSeconds.Value);
            if (value.ChomperMimicry?.MaxDecoyGameSeconds.HasValue == true) yield return Pair("chomperMimicry.maxDecoyGameSeconds", value.ChomperMimicry.MaxDecoyGameSeconds.Value);
            if (value.ShadowImpact?.WindupEnabled.HasValue == true) yield return Pair("shadowImpact.windupEnabled", value.ShadowImpact.WindupEnabled.Value ? 1 : 0);
            if (value.ShadowImpact?.Distance.HasValue == true) yield return Pair("shadowImpact.distance", value.ShadowImpact.Distance.Value);
            if (value.ShadowImpact?.TravelSpeed.HasValue == true) yield return Pair("shadowImpact.travelSpeed", value.ShadowImpact.TravelSpeed.Value);
            if (value.Impact?.Distance.HasValue == true) yield return Pair("impact.distance", value.Impact.Distance.Value);
            if (value.Impact?.TravelSpeed.HasValue == true) yield return Pair("impact.travelSpeed", value.Impact.TravelSpeed.Value);
            if (value.Impact?.ArcPath.HasValue == true) yield return Pair("impact.arcPath", value.Impact.ArcPath.Value ? 1 : 0);
            if (value.Impact?.ArcOffset.HasValue == true) yield return Pair("impact.arcOffset", value.Impact.ArcOffset.Value);
            if (value.Battering?.WindupEnabled.HasValue == true) yield return Pair("battering.windupEnabled", value.Battering.WindupEnabled.Value ? 1 : 0);
            if (value.Battering?.AttackDistance.HasValue == true) yield return Pair("battering.attackDistance", value.Battering.AttackDistance.Value);
            if (value.Battering?.TravelSpeed.HasValue == true) yield return Pair("battering.travelSpeed", value.Battering.TravelSpeed.Value);
            if (value.BatteringBullet?.WindupEnabled.HasValue == true) yield return Pair("batteringBullet.windupEnabled", value.BatteringBullet.WindupEnabled.Value ? 1 : 0);
            if (value.BatteringBullet?.MinBulletCount.HasValue == true) yield return Pair("batteringBullet.minBulletCount", value.BatteringBullet.MinBulletCount.Value);
            if (value.BatteringBullet?.MaxBulletCount.HasValue == true) yield return Pair("batteringBullet.maxBulletCount", value.BatteringBullet.MaxBulletCount.Value);
            if (value.BatteringBullet?.AttackDistance.HasValue == true) yield return Pair("batteringBullet.attackDistance", value.BatteringBullet.AttackDistance.Value);
            if (value.BatteringBullet?.TravelSpeed.HasValue == true) yield return Pair("batteringBullet.travelSpeed", value.BatteringBullet.TravelSpeed.Value);
            if (value.BatteringBullet?.ProjectileHorizontalSpeed.HasValue == true) yield return Pair("batteringBullet.projectileHorizontalSpeed", value.BatteringBullet.ProjectileHorizontalSpeed.Value);
            if (value.BatteringBullet?.ProjectileLifetimeGameSeconds.HasValue == true) yield return Pair("batteringBullet.projectileLifetimeGameSeconds", value.BatteringBullet.ProjectileLifetimeGameSeconds.Value);
        }
        private static KeyValuePair<string, double> Pair(string field, double value) => new KeyValuePair<string, double>(field, value);
        internal static bool IsAttackField(string field) => field.StartsWith("nativeAttacks.", StringComparison.Ordinal);
        internal static string Field(string attackId, string field) => "nativeAttacks." + attackId + "." + field;
        internal static double Value(IEnumerable<MonsterNativeAttackParameters> values, string field)
        {
            foreach (var value in values)
                foreach (var pair in Fields(value)) if (Field(value.AttackId, pair.Key) == field) return pair.Value;
            throw new InvalidOperationException("Native attack field was not declared: " + field);
        }
        internal static MonsterDiagnostic? Validate(string owner, string id, string template,
            IReadOnlyList<MonsterNativeAttackParameters>? values, IReadOnlyList<MonsterNativeAttackInfo>? catalog = null, bool nativeOverride = false, string? fieldPrefix = null)
        {
            string prefix = fieldPrefix ?? (nativeOverride ? "fields.nativeAttacks" : "nativeAttacks");
            MonsterDiagnostic Error(string code, string at, string message) => new MonsterDiagnostic(code, message, owner, owner + ":" + id, template, "Validate", at);
            if (values == null || values.Count > 5) return Error("InvalidNativeAttack", prefix, "Expected at most five existing native slots.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < values.Count; i++)
            {
                var value = values[i]; string at = prefix + "[" + i + "]";
                if (value == null || !new[] { "AtkNormal", "AtkSkill01", "AtkSkill02", "AtkSkill03", "AtkSkill04" }.Contains(value.AttackId) || !ids.Add(value.AttackId))
                    return Error("InvalidNativeAttack", at + ".attackId", "Select a distinct enabled native slot.");
                var fields = Fields(value).ToArray();
                if (fields.Length == 0 && !Sounds(value).Any() || value.AmoebaJump != null && !fields.Any(f => f.Key.StartsWith("amoebaJump.", StringComparison.Ordinal)) ||
                    value.RandomShoot != null && !fields.Any(f => f.Key.StartsWith("randomShoot.", StringComparison.Ordinal)) ||
                    value.Shoot != null && !fields.Any(f => f.Key.StartsWith("shoot.", StringComparison.Ordinal)) ||
                    value.MultiShoot != null && !fields.Any(f => f.Key.StartsWith("multiShoot.", StringComparison.Ordinal)) ||
                    value.SalvoShoot != null && !fields.Any(f => f.Key.StartsWith("salvoShoot.", StringComparison.Ordinal)) ||
                    value.ScatteringShoot != null && !fields.Any(f => f.Key.StartsWith("scatteringShoot.", StringComparison.Ordinal)) ||
                    value.DragonShoot != null && !fields.Any(f => f.Key.StartsWith("dragonShoot.", StringComparison.Ordinal)) ||
                    value.ScatteringThrow != null && !fields.Any(f => f.Key.StartsWith("scatteringThrow.", StringComparison.Ordinal)) ||
                    value.BombingDive != null && !fields.Any(f => f.Key.StartsWith("bombingDive.", StringComparison.Ordinal)) ||
                    value.BatteringBullet != null && !fields.Any(f => f.Key.StartsWith("batteringBullet.", StringComparison.Ordinal)) ||
                    value.ChomperBite != null && !fields.Any(f => f.Key.StartsWith("chomperBite.", StringComparison.Ordinal)) ||
                    value.Sting != null && !fields.Any(f => f.Key.StartsWith("sting.", StringComparison.Ordinal)) ||
                    value.MultiImpact != null && !fields.Any(f => f.Key.StartsWith("multiImpact.", StringComparison.Ordinal)) ||
                    value.Impact != null && !fields.Any(f => f.Key.StartsWith("impact.", StringComparison.Ordinal)) ||
                    value.BulletTrain != null && !fields.Any(f => f.Key.StartsWith("bulletTrain.", StringComparison.Ordinal)) ||
                    value.Call != null && !fields.Any(f => f.Key.StartsWith("call.", StringComparison.Ordinal)) ||
                    value.Landmine != null && !fields.Any(f => f.Key.StartsWith("landmine.", StringComparison.Ordinal)) ||
                    value.TimeBomb != null && !fields.Any(f => f.Key.StartsWith("timeBomb.", StringComparison.Ordinal)) ||
                    value.ThunderPile != null && !fields.Any(f => f.Key.StartsWith("thunderPile.", StringComparison.Ordinal)) ||
                    value.ChomperMimicry != null && !fields.Any(f => f.Key.StartsWith("chomperMimicry.", StringComparison.Ordinal)) ||
                    value.ShadowImpact != null && !fields.Any(f => f.Key.StartsWith("shadowImpact.", StringComparison.Ordinal)) ||
                    value.FungusShoot != null && !fields.Any(f => f.Key.StartsWith("fungusShoot.", StringComparison.Ordinal)) ||
                    value.Battering != null && !fields.Any(f => f.Key.StartsWith("battering.", StringComparison.Ordinal)))
                    return Error("InvalidNativeAttack", at, "Set a parameter or omit the empty slot/specialized object.");
                foreach (var field in fields)
                {
                    bool extraDashes = field.Key == "multiImpact.minExtraDashes" || field.Key == "multiImpact.maxExtraDashes";
                    bool shotCount = field.Key == "randomShoot.minBulletCount" || field.Key == "randomShoot.maxBulletCount" || field.Key == "call.minCount" || field.Key == "call.maxCount" || field.Key == "thunderPile.minCount" || field.Key == "thunderPile.maxCount" || field.Key == "bulletTrain.volleyCount" || field.Key == "bulletTrain.bulletCount" || field.Key == "multiShoot.minShotCount" || field.Key == "multiShoot.maxShotCount" || field.Key == "salvoShoot.bulletCount" ||
                        field.Key == "scatteringShoot.minBulletCount" || field.Key == "scatteringShoot.maxBulletCount" || field.Key == "scatteringShoot.burstCount" ||
                        field.Key == "dragonShoot.minBulletCount" || field.Key == "dragonShoot.maxBulletCount" || field.Key == "dragonShoot.sectorCount" || field.Key == "scatteringThrow.minBulletCount" || field.Key == "scatteringThrow.maxBulletCount" || field.Key == "bombingDive.minBulletCount" || field.Key == "bombingDive.maxBulletCount" || field.Key == "batteringBullet.minBulletCount" || field.Key == "batteringBullet.maxBulletCount";
                    double min = field.Key == "thunderPile.dropSpeed" ? float.Epsilon : field.Key == "thunderPile.maxDropGameSeconds" || field.Key == "thunderPile.waitGameSeconds" ? float.Epsilon : (field.Key == "amoebaJump.speed" || field.Key == "bombingDive.travelSpeed" || field.Key == "batteringBullet.travelSpeed" || field.Key == "battering.travelSpeed" || field.Key == "sting.travelSpeed" || field.Key == "impact.travelSpeed" || field.Key == "shadowImpact.travelSpeed" || field.Key == "multiImpact.travelSpeed") ? float.Epsilon : field.Key == "amoebaJump.distanceCells" || shotCount || extraDashes ? 1 : field.Key == "impact.arcOffset" || field.Key == "bulletTrain.directionX" || field.Key == "bulletTrain.directionY" ? double.NegativeInfinity : 0;
                    double max = field.Key == "criticalRate" ? 1 : field.Key == "scatteringShoot.directionMode" ? 3 : double.PositiveInfinity;
                    if (double.IsNaN(field.Value) || double.IsInfinity(field.Value) || field.Value < min || field.Value > max)
                        return Error("InvalidNativeAttack", at + "." + field.Key, "Value outside the admitted range " + min + ".." + max + ".");
                }
                foreach (var sound in Sounds(value))
                {
                    var soundError = SoundValidation.ValidateResource(owner, id, template, sound.Value, at + "." + sound.Key);
                    if (soundError != null) return soundError;
                }
                if (catalog == null) continue;
                var matching = catalog.Where(s => s.AttackId == value.AttackId).ToArray();
                if (matching.Length != 1) return Error("NativeAttackUnsupported", at + ".attackId", "Refresh the current Host catalog; this slot has no unambiguous parameter capability.");
                var slot = matching[0];
                foreach (var sound in Sounds(value))
                    if (!slot.Sounds.Any(s => s.Field == sound.Key && (nativeOverride ? s.NativeOverride : s.Definition)))
                        return Error("NativeAttackFieldUnsupported", at + "." + sound.Key, "This slot has no admitted native sound boundary in the selected domain.");
                foreach (var field in fields)
                    if (!slot.Fields.Any(f => f.Field == field.Key && (nativeOverride ? f.NativeOverride : f.Definition)))
                        return Error("NativeAttackFieldUnsupported", at + "." + field.Key, "This native slot does not support the requested field in the selected definition/override domain.");
            }
            return null;
        }
    }
}
