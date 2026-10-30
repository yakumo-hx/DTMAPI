using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MonsterContent.Authoring
{
    internal static class NativeAttackCodec
    {
        internal static List<MonsterNativeAttackParameters> Read(object? value, IDictionary<string, byte[]> assets, bool payload, Func<string, byte[]>? readBytes, string fieldPrefix = "nativeAttacks")
        {
            return Json.Array(value, fieldPrefix).Select((row, index) => {
                try
                {
                var a = Json.Object(row, "nativeAttacks[]", "attackId", "cooldownGameSeconds startOnCooldown damage criticalRate windupGameSeconds amoebaJump shoot multiShoot salvoShoot scatteringShoot dragonShoot scatteringThrow bombingDive batteringBullet battering fungusShoot sting chomperBite impact bulletTrain shadowImpact chomperMimicry thunderPile landmine timeBomb call randomShoot multiImpact fireSound dashSound throwSound volleySound readySound landSound countdownSound explosionSound");
                float? N(Dictionary<string, object?> data, string key) => !data.TryGetValue(key, out var v) || v == null ? (float?)null : (float)Json.Number(v, "nativeAttacks." + key);
                var result = new MonsterNativeAttackParameters { AttackId = Json.Text(a["attackId"], "nativeAttacks.attackId"),
                    CooldownGameSeconds = N(a, "cooldownGameSeconds"), Damage = N(a, "damage"), CriticalRate = N(a, "criticalRate"), WindupGameSeconds = N(a, "windupGameSeconds") };
                if (a.TryGetValue("fireSound", out var fire) && fire != null)
                    result.FireSound = SoundCodec.ReadWav(fire, "nativeAttacks.fireSound", assets, payload, readBytes);
                if (a.TryGetValue("dashSound", out var dash) && dash != null)
                    result.DashSound = SoundCodec.ReadWav(dash, "nativeAttacks.dashSound", assets, payload, readBytes);
                if (a.TryGetValue("throwSound", out var thrown) && thrown != null)
                    result.ThrowSound = SoundCodec.ReadWav(thrown, "nativeAttacks.throwSound", assets, payload, readBytes);
                if (a.TryGetValue("volleySound", out var volley) && volley != null)
                    result.VolleySound = SoundCodec.ReadWav(volley, "nativeAttacks.volleySound", assets, payload, readBytes);
                if (a.TryGetValue("readySound", out var ready) && ready != null)
                    result.ReadySound = SoundCodec.ReadWav(ready, "nativeAttacks.readySound", assets, payload, readBytes);
                if (a.TryGetValue("landSound", out var land) && land != null)
                    result.LandSound = SoundCodec.ReadWav(land, "nativeAttacks.landSound", assets, payload, readBytes);
                if (a.TryGetValue("countdownSound", out var countdown) && countdown != null)
                    result.CountdownSound = SoundCodec.ReadWav(countdown, "nativeAttacks.countdownSound", assets, payload, readBytes);
                if (a.TryGetValue("explosionSound", out var explosion) && explosion != null)
                    result.ExplosionSound = SoundCodec.ReadWav(explosion, "nativeAttacks.explosionSound", assets, payload, readBytes);
                if (a.TryGetValue("startOnCooldown", out var start) && start != null) result.StartOnCooldown = Json.Bool(start, "nativeAttacks.startOnCooldown");
                if (a.TryGetValue("amoebaJump", out var jump) && jump != null)
                {
                    var j = Json.Object(jump, "amoebaJump", "", "range speed distanceCells");
                    result.AmoebaJump = new MonsterAmoebaJumpParameters { Range = N(j, "range"), Speed = N(j, "speed"),
                        DistanceCells = j.TryGetValue("distanceCells", out var d) ? Json.NullableInt(d, "amoebaJump.distanceCells") : null };
                }
                if (a.TryGetValue("randomShoot", out var randomShoot) && randomShoot != null)
                {
                    var s = Json.Object(randomShoot, "randomShoot", "", "minBulletCount maxBulletCount shotIntervalGameSeconds projectileSpeed projectileLifetimeGameSeconds");
                    int? I(string key) => s.TryGetValue(key, out var v) ? Json.NullableInt(v, "randomShoot." + key) : null;
                    float? R(string key) => !s.TryGetValue(key, out var v) || v == null ? (float?)null : (float)Json.Number(v, "randomShoot." + key);
                    result.RandomShoot = new MonsterRandomShootParameters { MinBulletCount = I("minBulletCount"), MaxBulletCount = I("maxBulletCount"),
                        ShotIntervalGameSeconds = R("shotIntervalGameSeconds"), ProjectileSpeed = R("projectileSpeed"), ProjectileLifetimeGameSeconds = R("projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("shoot", out var shoot) && shoot != null)
                {
                    var s = Json.Object(shoot, "shoot", "", "windupEnabled recoveryGameSeconds projectileSpeed projectileLifetimeGameSeconds");
                    result.Shoot = new MonsterShootParameters { RecoveryGameSeconds = N(s, "recoveryGameSeconds"), ProjectileSpeed = N(s, "projectileSpeed"),
                        ProjectileLifetimeGameSeconds = N(s, "projectileLifetimeGameSeconds"),
                        WindupEnabled = s.TryGetValue("windupEnabled", out var w) && w != null ? Json.Bool(w, "shoot.windupEnabled") : (bool?)null };
                }
                if (a.TryGetValue("multiShoot", out var multiShoot) && multiShoot != null)
                {
                    var s = Json.Object(multiShoot, "multiShoot", "", "windupEnabled shotGameSeconds minShotCount maxShotCount fixedDirection projectileSpeed projectileLifetimeGameSeconds");
                    bool? B(string key) => s.TryGetValue(key, out var v) && v != null ? Json.Bool(v, "multiShoot." + key) : (bool?)null;
                    int? I(string key) => s.TryGetValue(key, out var v) ? Json.NullableInt(v, "multiShoot." + key) : null;
                    result.MultiShoot = new MonsterMultiShootParameters { WindupEnabled = B("windupEnabled"), FixedDirection = B("fixedDirection"),
                        ShotGameSeconds = N(s, "shotGameSeconds"), MinShotCount = I("minShotCount"), MaxShotCount = I("maxShotCount"),
                        ProjectileSpeed = N(s, "projectileSpeed"), ProjectileLifetimeGameSeconds = N(s, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("salvoShoot", out var salvoShoot) && salvoShoot != null)
                {
                    var s = Json.Object(salvoShoot, "salvoShoot", "", "windupEnabled bulletCount sectorAngleDegrees shotIntervalGameSeconds projectileSpeed projectileLifetimeGameSeconds");
                    result.SalvoShoot = new MonsterSalvoShootParameters {
                        WindupEnabled = s.TryGetValue("windupEnabled", out var w) && w != null ? Json.Bool(w, "salvoShoot.windupEnabled") : (bool?)null,
                        BulletCount = s.TryGetValue("bulletCount", out var c) ? Json.NullableInt(c, "salvoShoot.bulletCount") : null,
                        SectorAngleDegrees = N(s, "sectorAngleDegrees"), ShotIntervalGameSeconds = N(s, "shotIntervalGameSeconds"),
                        ProjectileSpeed = N(s, "projectileSpeed"), ProjectileLifetimeGameSeconds = N(s, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("scatteringShoot", out var scatteringShoot) && scatteringShoot != null)
                {
                    var s = Json.Object(scatteringShoot, "scatteringShoot", "", "windupEnabled minBulletCount maxBulletCount sectorAngleDegrees burstCount burstIntervalGameSeconds directionMode angleOffsetDegrees reverseAngleOffset projectileSpeed projectileLifetimeGameSeconds");
                    bool? B(string key) => s.TryGetValue(key, out var v) && v != null ? Json.Bool(v, "scatteringShoot." + key) : (bool?)null;
                    int? I(string key) => s.TryGetValue(key, out var v) ? Json.NullableInt(v, "scatteringShoot." + key) : null;
                    result.ScatteringShoot = new MonsterScatteringShootParameters {
                        WindupEnabled = B("windupEnabled"), MinBulletCount = I("minBulletCount"), MaxBulletCount = I("maxBulletCount"),
                        SectorAngleDegrees = N(s, "sectorAngleDegrees"), BurstCount = I("burstCount"), BurstIntervalGameSeconds = N(s, "burstIntervalGameSeconds"),
                        DirectionMode = s.TryGetValue("directionMode", out var mode) && mode != null ?
                            (MonsterScatteringMode?)Enum.Parse(typeof(MonsterScatteringMode), Json.Choice(mode, "scatteringShoot.directionMode", "Fixed", "Random", "Player", "Offset")) : null,
                        AngleOffsetDegrees = N(s, "angleOffsetDegrees"), ReverseAngleOffset = B("reverseAngleOffset"),
                        ProjectileSpeed = N(s, "projectileSpeed"), ProjectileLifetimeGameSeconds = N(s, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("dragonShoot", out var dragonShoot) && dragonShoot != null)
                {
                    var d = Json.Object(dragonShoot, "dragonShoot", "", "windupEnabled minBulletCount maxBulletCount sectorCount sectorAngleDegrees shotIntervalGameSeconds projectileSpeed projectileLifetimeGameSeconds");
                    int? I(string key) => d.TryGetValue(key, out var v) ? Json.NullableInt(v, "dragonShoot." + key) : null;
                    result.DragonShoot = new MonsterDragonShootParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? Json.Bool(w, "dragonShoot.windupEnabled") : (bool?)null,
                        MinBulletCount = I("minBulletCount"), MaxBulletCount = I("maxBulletCount"), SectorCount = I("sectorCount"),
                        SectorAngleDegrees = N(d, "sectorAngleDegrees"), ShotIntervalGameSeconds = N(d, "shotIntervalGameSeconds"),
                        ProjectileSpeed = N(d, "projectileSpeed"), ProjectileLifetimeGameSeconds = N(d, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("scatteringThrow", out var scatteringThrow) && scatteringThrow != null)
                {
                    var d = Json.Object(scatteringThrow, "scatteringThrow", "", "windupEnabled minBulletCount maxBulletCount sectorAngleDegrees recoveryGameSeconds projectileSpeed projectileLifetimeGameSeconds");
                    int? I(string key) => d.TryGetValue(key, out var v) ? Json.NullableInt(v, "scatteringThrow." + key) : null;
                    result.ScatteringThrow = new MonsterScatteringThrowParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? Json.Bool(w, "scatteringThrow.windupEnabled") : (bool?)null,
                        MinBulletCount = I("minBulletCount"), MaxBulletCount = I("maxBulletCount"),
                        SectorAngleDegrees = N(d, "sectorAngleDegrees"), RecoveryGameSeconds = N(d, "recoveryGameSeconds"),
                        ProjectileSpeed = N(d, "projectileSpeed"), ProjectileLifetimeGameSeconds = N(d, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("bombingDive", out var bombingDive) && bombingDive != null)
                {
                    var d = Json.Object(bombingDive, "bombingDive", "", "windupEnabled minBulletCount maxBulletCount range travelDistance travelSpeed projectileLifetimeGameSeconds");
                    int? I(string key) => d.TryGetValue(key, out var v) ? Json.NullableInt(v, "bombingDive." + key) : null;
                    result.BombingDive = new MonsterBombingDiveParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? Json.Bool(w, "bombingDive.windupEnabled") : (bool?)null,
                        MinBulletCount = I("minBulletCount"), MaxBulletCount = I("maxBulletCount"),
                        Range = N(d, "range"), TravelDistance = N(d, "travelDistance"), TravelSpeed = N(d, "travelSpeed"),
                        ProjectileLifetimeGameSeconds = N(d, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("batteringBullet", out var batteringBullet) && batteringBullet != null)
                {
                    var d = Json.Object(batteringBullet, "batteringBullet", "", "windupEnabled minBulletCount maxBulletCount attackDistance travelSpeed projectileHorizontalSpeed projectileLifetimeGameSeconds");
                    int? I(string key) => d.TryGetValue(key, out var v) ? Json.NullableInt(v, "batteringBullet." + key) : null;
                    result.BatteringBullet = new MonsterBatteringBulletParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? Json.Bool(w, "batteringBullet.windupEnabled") : (bool?)null,
                        MinBulletCount = I("minBulletCount"), MaxBulletCount = I("maxBulletCount"),
                        AttackDistance = N(d, "attackDistance"), TravelSpeed = N(d, "travelSpeed"),
                        ProjectileHorizontalSpeed = N(d, "projectileHorizontalSpeed"), ProjectileLifetimeGameSeconds = N(d, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("fungusShoot", out var fungusShoot) && fungusShoot != null)
                {
                    var f = Json.Object(fungusShoot, "fungusShoot", "", "windupEnabled projectileSpeed projectileLifetimeGameSeconds");
                    result.FungusShoot = new MonsterFungusShootParameters {
                        WindupEnabled = f.TryGetValue("windupEnabled", out var w) && w != null ? Json.Bool(w, "fungusShoot.windupEnabled") : (bool?)null,
                        ProjectileSpeed = N(f, "projectileSpeed"), ProjectileLifetimeGameSeconds = N(f, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("chomperBite", out var chomperBite) && chomperBite != null)
                {
                    var d = Json.Object(chomperBite, "chomperBite", "", "windupEnabled range");
                    result.ChomperBite = new MonsterChomperBiteParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? Json.Bool(w, "chomperBite.windupEnabled") : (bool?)null,
                        Range = N(d, "range") };
                }
                if (a.TryGetValue("bulletTrain", out var bulletTrain) && bulletTrain != null)
                {
                    var d = Json.Object(bulletTrain, "bulletTrain", "", "windupEnabled volleyCount volleyIntervalGameSeconds bulletCount sectorAngleDegrees directionX directionY projectileSpeed projectileLifetimeGameSeconds");
                    int? I(string key) => d.TryGetValue(key, out var v) && v != null ? Json.Int(v, "bulletTrain." + key) : (int?)null;
                    result.BulletTrain = new MonsterBulletTrainParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? Json.Bool(w, "bulletTrain.windupEnabled") : (bool?)null,
                        VolleyCount = I("volleyCount"), BulletCount = I("bulletCount"), SectorAngleDegrees = I("sectorAngleDegrees"),
                        VolleyIntervalGameSeconds = N(d, "volleyIntervalGameSeconds"), DirectionX = N(d, "directionX"), DirectionY = N(d, "directionY"),
                        ProjectileSpeed = N(d, "projectileSpeed"), ProjectileLifetimeGameSeconds = N(d, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("impact", out var impact) && impact != null)
                {
                    var d = Json.Object(impact, "impact", "", "windupEnabled distance travelSpeed arcPath arcOffset");
                    result.Impact = new MonsterImpactParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? Json.Bool(w, "impact.windupEnabled") : (bool?)null,
                        ArcPath = d.TryGetValue("arcPath", out var arc) && arc != null ? Json.Bool(arc, "impact.arcPath") : (bool?)null,
                        Distance = N(d, "distance"), TravelSpeed = N(d, "travelSpeed"), ArcOffset = N(d, "arcOffset") };
                }
                if (a.TryGetValue("call", out var call) && call != null)
                {
                    var c = Json.Object(call, "call", "", "minCount maxCount roomLimit");
                    int? I(string key) => c.TryGetValue(key, out var v) ? Json.NullableInt(v, "call." + key) : null;
                    result.Call = new MonsterCallParameters { MinCount = I("minCount"), MaxCount = I("maxCount"), RoomLimit = I("roomLimit") };
                }
                if (a.TryGetValue("landmine", out var landmine) && landmine != null)
                {
                    var p = Json.Object(landmine, "landmine", "", "range recoilGameSeconds throwImpulse liveGameSeconds reactionGameSeconds");
                    result.Landmine = new MonsterLandmineParameters { Range = N(p, "range"), RecoilGameSeconds = N(p, "recoilGameSeconds"), ThrowImpulse = N(p, "throwImpulse"), LiveGameSeconds = N(p, "liveGameSeconds"), ReactionGameSeconds = N(p, "reactionGameSeconds") };
                }
                if (a.TryGetValue("timeBomb", out var timeBomb) && timeBomb != null)
                {
                    var p = Json.Object(timeBomb, "timeBomb", "", "range recoilGameSeconds throwImpulse count maxThrowImpulse");
                    result.TimeBomb = new MonsterTimeBombParameters { Range = N(p, "range"), RecoilGameSeconds = N(p, "recoilGameSeconds"), ThrowImpulse = N(p, "throwImpulse"), Count = p.TryGetValue("count", out var count) ? Json.NullableInt(count, "timeBomb.count") : null, MaxThrowImpulse = N(p, "maxThrowImpulse") };
                }
                if (a.TryGetValue("thunderPile", out var thunderPile) && thunderPile != null)
                {
                    var d = Json.Object(thunderPile, "thunderPile", "", "windupEnabled range minCount maxCount subWindupGameSeconds dropSpeed maxDropGameSeconds waitGameSeconds fadeGameSeconds");
                    int? I(string key) => d.TryGetValue(key, out var v) ? Json.NullableInt(v, "thunderPile." + key) : null;
                    result.ThunderPile = new MonsterThunderPileParameters { WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? Json.Bool(w, "thunderPile.windupEnabled") : (bool?)null, Range = N(d, "range"), MinCount = I("minCount"), MaxCount = I("maxCount"), SubWindupGameSeconds = N(d, "subWindupGameSeconds"), DropSpeed = N(d, "dropSpeed"), MaxDropGameSeconds = N(d, "maxDropGameSeconds"), WaitGameSeconds = N(d, "waitGameSeconds"), FadeGameSeconds = N(d, "fadeGameSeconds") };
                }
                if (a.TryGetValue("chomperMimicry", out var chomperMimicry) && chomperMimicry != null)
                {
                    var d = Json.Object(chomperMimicry, "chomperMimicry", "", "minDecoyGameSeconds maxDecoyGameSeconds");
                    result.ChomperMimicry = new MonsterChomperMimicryParameters { MinDecoyGameSeconds = N(d, "minDecoyGameSeconds"), MaxDecoyGameSeconds = N(d, "maxDecoyGameSeconds") };
                }
                if (a.TryGetValue("multiImpact", out var multiImpact) && multiImpact != null)
                {
                    var d = Json.Object(multiImpact, "multiImpact", "", "windupEnabled distance travelSpeed minExtraDashes maxExtraDashes");
                    int? I(string key) => d.TryGetValue(key, out var v) ? Json.NullableInt(v, "multiImpact." + key) : null;
                    result.MultiImpact = new MonsterMultiImpactParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? Json.Bool(w, "multiImpact.windupEnabled") : (bool?)null,
                        Distance = N(d, "distance"), TravelSpeed = N(d, "travelSpeed"),
                        MinExtraDashes = I("minExtraDashes"), MaxExtraDashes = I("maxExtraDashes") };
                }
                if (a.TryGetValue("shadowImpact", out var shadowImpact) && shadowImpact != null)
                {
                    var d = Json.Object(shadowImpact, "shadowImpact", "", "windupEnabled distance travelSpeed");
                    result.ShadowImpact = new MonsterShadowImpactParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? Json.Bool(w, "shadowImpact.windupEnabled") : (bool?)null,
                        Distance = N(d, "distance"), TravelSpeed = N(d, "travelSpeed") };
                }
                if (a.TryGetValue("sting", out var sting) && sting != null)
                {
                    var d = Json.Object(sting, "sting", "", "windupEnabled distance travelSpeed");
                    result.Sting = new MonsterStingParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? Json.Bool(w, "sting.windupEnabled") : (bool?)null,
                        Distance = N(d, "distance"), TravelSpeed = N(d, "travelSpeed") };
                }
                if (a.TryGetValue("battering", out var battering) && battering != null)
                {
                    var d = Json.Object(battering, "battering", "", "windupEnabled attackDistance travelSpeed");
                    result.Battering = new MonsterBatteringParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? Json.Bool(w, "battering.windupEnabled") : (bool?)null,
                        AttackDistance = N(d, "attackDistance"), TravelSpeed = N(d, "travelSpeed") };
                }
                return result;
                }
                catch (AuthoringException e) when (fieldPrefix != "nativeAttacks")
                {
                    string field = e.Diagnostic.Field;
                    string suffix = field.StartsWith("nativeAttacks", StringComparison.Ordinal) ? field.Substring("nativeAttacks".Length).TrimStart('[', ']') : "." + field;
                    throw new AuthoringException(e.Diagnostic.Code, fieldPrefix + "[" + index + "]" + suffix, e.Message);
                }
            }).ToList();
        }
        internal static object Write(MonsterNativeAttackParameters a)
        {
            var row = Json.Map("attackId", a.AttackId, "cooldownGameSeconds", a.CooldownGameSeconds, "startOnCooldown", a.StartOnCooldown, "damage", a.Damage,
                "criticalRate", a.CriticalRate, "windupGameSeconds", a.WindupGameSeconds);
            foreach (var sound in NativeAttackValidation.Sounds(a)) row[sound.Key] = SoundCodec.WriteWav(sound.Value);
            if (a.AmoebaJump != null) row["amoebaJump"] = Json.Map("range", a.AmoebaJump.Range, "speed", a.AmoebaJump.Speed, "distanceCells", a.AmoebaJump.DistanceCells);
            if (a.Shoot != null) row["shoot"] = Json.Map("windupEnabled", a.Shoot.WindupEnabled, "recoveryGameSeconds", a.Shoot.RecoveryGameSeconds,
                "projectileSpeed", a.Shoot.ProjectileSpeed, "projectileLifetimeGameSeconds", a.Shoot.ProjectileLifetimeGameSeconds);
            if (a.MultiShoot != null) row["multiShoot"] = Json.Map("windupEnabled", a.MultiShoot.WindupEnabled, "shotGameSeconds", a.MultiShoot.ShotGameSeconds,
                "minShotCount", a.MultiShoot.MinShotCount, "maxShotCount", a.MultiShoot.MaxShotCount, "fixedDirection", a.MultiShoot.FixedDirection,
                "projectileSpeed", a.MultiShoot.ProjectileSpeed, "projectileLifetimeGameSeconds", a.MultiShoot.ProjectileLifetimeGameSeconds);
            if (a.SalvoShoot != null) row["salvoShoot"] = Json.Map("windupEnabled", a.SalvoShoot.WindupEnabled, "bulletCount", a.SalvoShoot.BulletCount,
                "sectorAngleDegrees", a.SalvoShoot.SectorAngleDegrees, "shotIntervalGameSeconds", a.SalvoShoot.ShotIntervalGameSeconds,
                "projectileSpeed", a.SalvoShoot.ProjectileSpeed, "projectileLifetimeGameSeconds", a.SalvoShoot.ProjectileLifetimeGameSeconds);
            if (a.ScatteringShoot != null) row["scatteringShoot"] = Json.Map("windupEnabled", a.ScatteringShoot.WindupEnabled,
                "minBulletCount", a.ScatteringShoot.MinBulletCount, "maxBulletCount", a.ScatteringShoot.MaxBulletCount, "sectorAngleDegrees", a.ScatteringShoot.SectorAngleDegrees,
                "burstCount", a.ScatteringShoot.BurstCount, "burstIntervalGameSeconds", a.ScatteringShoot.BurstIntervalGameSeconds, "directionMode", a.ScatteringShoot.DirectionMode?.ToString(),
                "angleOffsetDegrees", a.ScatteringShoot.AngleOffsetDegrees, "reverseAngleOffset", a.ScatteringShoot.ReverseAngleOffset,
                "projectileSpeed", a.ScatteringShoot.ProjectileSpeed, "projectileLifetimeGameSeconds", a.ScatteringShoot.ProjectileLifetimeGameSeconds);
            if (a.DragonShoot != null) row["dragonShoot"] = Json.Map("windupEnabled", a.DragonShoot.WindupEnabled,
                "minBulletCount", a.DragonShoot.MinBulletCount, "maxBulletCount", a.DragonShoot.MaxBulletCount, "sectorCount", a.DragonShoot.SectorCount,
                "sectorAngleDegrees", a.DragonShoot.SectorAngleDegrees, "shotIntervalGameSeconds", a.DragonShoot.ShotIntervalGameSeconds,
                "projectileSpeed", a.DragonShoot.ProjectileSpeed, "projectileLifetimeGameSeconds", a.DragonShoot.ProjectileLifetimeGameSeconds);
            if (a.ScatteringThrow != null) row["scatteringThrow"] = Json.Map("windupEnabled", a.ScatteringThrow.WindupEnabled,
                "minBulletCount", a.ScatteringThrow.MinBulletCount, "maxBulletCount", a.ScatteringThrow.MaxBulletCount,
                "sectorAngleDegrees", a.ScatteringThrow.SectorAngleDegrees, "recoveryGameSeconds", a.ScatteringThrow.RecoveryGameSeconds,
                "projectileSpeed", a.ScatteringThrow.ProjectileSpeed, "projectileLifetimeGameSeconds", a.ScatteringThrow.ProjectileLifetimeGameSeconds);
            if (a.BombingDive != null) row["bombingDive"] = Json.Map("windupEnabled", a.BombingDive.WindupEnabled,
                "minBulletCount", a.BombingDive.MinBulletCount, "maxBulletCount", a.BombingDive.MaxBulletCount,
                "range", a.BombingDive.Range, "travelDistance", a.BombingDive.TravelDistance, "travelSpeed", a.BombingDive.TravelSpeed,
                "projectileLifetimeGameSeconds", a.BombingDive.ProjectileLifetimeGameSeconds);
            if (a.BatteringBullet != null) row["batteringBullet"] = Json.Map("windupEnabled", a.BatteringBullet.WindupEnabled,
                "minBulletCount", a.BatteringBullet.MinBulletCount, "maxBulletCount", a.BatteringBullet.MaxBulletCount,
                "attackDistance", a.BatteringBullet.AttackDistance, "travelSpeed", a.BatteringBullet.TravelSpeed,
                "projectileHorizontalSpeed", a.BatteringBullet.ProjectileHorizontalSpeed, "projectileLifetimeGameSeconds", a.BatteringBullet.ProjectileLifetimeGameSeconds);
            if (a.FungusShoot != null) row["fungusShoot"] = Json.Map("windupEnabled", a.FungusShoot.WindupEnabled,
                "projectileSpeed", a.FungusShoot.ProjectileSpeed, "projectileLifetimeGameSeconds", a.FungusShoot.ProjectileLifetimeGameSeconds);
            if (a.ChomperBite != null) row["chomperBite"] = Json.Map("windupEnabled", a.ChomperBite.WindupEnabled,
                "range", a.ChomperBite.Range);
            if (a.Sting != null) row["sting"] = Json.Map("windupEnabled", a.Sting.WindupEnabled,
                "distance", a.Sting.Distance, "travelSpeed", a.Sting.TravelSpeed);
            if (a.BulletTrain != null) row["bulletTrain"] = Json.Map("windupEnabled", a.BulletTrain.WindupEnabled,
                "volleyCount", a.BulletTrain.VolleyCount, "volleyIntervalGameSeconds", a.BulletTrain.VolleyIntervalGameSeconds,
                "bulletCount", a.BulletTrain.BulletCount, "sectorAngleDegrees", a.BulletTrain.SectorAngleDegrees,
                "directionX", a.BulletTrain.DirectionX, "directionY", a.BulletTrain.DirectionY,
                "projectileSpeed", a.BulletTrain.ProjectileSpeed, "projectileLifetimeGameSeconds", a.BulletTrain.ProjectileLifetimeGameSeconds);
            if (a.Impact != null) row["impact"] = Json.Map("windupEnabled", a.Impact.WindupEnabled,
                "distance", a.Impact.Distance, "travelSpeed", a.Impact.TravelSpeed, "arcPath", a.Impact.ArcPath, "arcOffset", a.Impact.ArcOffset);
            if (a.RandomShoot != null) row["randomShoot"] = Json.Map("minBulletCount", a.RandomShoot.MinBulletCount,
                "maxBulletCount", a.RandomShoot.MaxBulletCount, "shotIntervalGameSeconds", a.RandomShoot.ShotIntervalGameSeconds,
                "projectileSpeed", a.RandomShoot.ProjectileSpeed, "projectileLifetimeGameSeconds", a.RandomShoot.ProjectileLifetimeGameSeconds);
            if (a.Call != null) row["call"] = Json.Map("minCount", a.Call.MinCount, "maxCount", a.Call.MaxCount, "roomLimit", a.Call.RoomLimit);
            if (a.Landmine != null) row["landmine"] = Json.Map("range", a.Landmine.Range, "recoilGameSeconds", a.Landmine.RecoilGameSeconds, "throwImpulse", a.Landmine.ThrowImpulse, "liveGameSeconds", a.Landmine.LiveGameSeconds, "reactionGameSeconds", a.Landmine.ReactionGameSeconds);
            if (a.TimeBomb != null) row["timeBomb"] = Json.Map("range", a.TimeBomb.Range, "recoilGameSeconds", a.TimeBomb.RecoilGameSeconds, "throwImpulse", a.TimeBomb.ThrowImpulse, "count", a.TimeBomb.Count, "maxThrowImpulse", a.TimeBomb.MaxThrowImpulse);
            if (a.ThunderPile != null) row["thunderPile"] = Json.Map("windupEnabled", a.ThunderPile.WindupEnabled, "range", a.ThunderPile.Range, "minCount", a.ThunderPile.MinCount, "maxCount", a.ThunderPile.MaxCount, "subWindupGameSeconds", a.ThunderPile.SubWindupGameSeconds, "dropSpeed", a.ThunderPile.DropSpeed, "maxDropGameSeconds", a.ThunderPile.MaxDropGameSeconds, "waitGameSeconds", a.ThunderPile.WaitGameSeconds, "fadeGameSeconds", a.ThunderPile.FadeGameSeconds);
            if (a.ChomperMimicry != null) row["chomperMimicry"] = Json.Map("minDecoyGameSeconds", a.ChomperMimicry.MinDecoyGameSeconds, "maxDecoyGameSeconds", a.ChomperMimicry.MaxDecoyGameSeconds);
            if (a.MultiImpact != null) row["multiImpact"] = Json.Map("windupEnabled", a.MultiImpact.WindupEnabled,
                "distance", a.MultiImpact.Distance, "travelSpeed", a.MultiImpact.TravelSpeed,
                "minExtraDashes", a.MultiImpact.MinExtraDashes, "maxExtraDashes", a.MultiImpact.MaxExtraDashes);
            if (a.ShadowImpact != null) row["shadowImpact"] = Json.Map("windupEnabled", a.ShadowImpact.WindupEnabled,
                "distance", a.ShadowImpact.Distance, "travelSpeed", a.ShadowImpact.TravelSpeed);
            if (a.Battering != null) row["battering"] = Json.Map("windupEnabled", a.Battering.WindupEnabled,
                "attackDistance", a.Battering.AttackDistance, "travelSpeed", a.Battering.TravelSpeed);
            return row;
        }
        internal static object Info(MonsterNativeAttackInfo a) => Json.Map("attackId", a.AttackId, "kind", a.Kind, "limitation", a.Limitation,
            "sounds", a.Sounds.Select(s => Json.Map("field", s.Field, "readAt", s.ReadAt, "definition", s.Definition, "nativeOverride", s.NativeOverride)).ToArray(),
            "fields", a.Fields.Select(f => Json.Map("field", f.Field, "value", f.Value, "unit", f.Unit, "readAt", f.ReadAt, "definition", f.Definition, "nativeOverride", f.NativeOverride)).ToArray());
        internal static IReadOnlyList<MonsterNativeAttackInfo> ReadInfo(object? value) => Array.AsReadOnly(Json.Array(value, "nativeAttacks").Select(row => {
            var a = Json.Object(row, "nativeAttacks[]", "attackId kind limitation fields", "sounds");
            return new MonsterNativeAttackInfo(Json.Text(a["attackId"], "attackId"), Json.Text(a["kind"], "kind"),
                Json.Array(a["fields"], "nativeAttacks.fields").Select(field => {
                    var f = Json.Object(field, "nativeAttacks.fields[]", "field value unit readAt definition nativeOverride");
                    return new MonsterNativeAttackFieldInfo(Json.Text(f["field"], "field"), Json.Number(f["value"], "value"), Json.Text(f["unit"], "unit"),
                        Json.Text(f["readAt"], "readAt"), Json.Bool(f["definition"], "definition"), Json.Bool(f["nativeOverride"], "nativeOverride"));
                }), Json.Text(a["limitation"], "limitation"), a.TryGetValue("sounds", out var sounds) ? Json.Array(sounds, "nativeAttacks.sounds").Select(sound => {
                    var s = Json.Object(sound, "nativeAttacks.sounds[]", "field readAt definition nativeOverride");
                    return new MonsterNativeAttackSoundInfo(Json.Text(s["field"], "field"), Json.Text(s["readAt"], "readAt"), Json.Bool(s["definition"], "definition"), Json.Bool(s["nativeOverride"], "nativeOverride"));
                }) : Array.Empty<MonsterNativeAttackSoundInfo>());
        }).ToArray());
    }
}
