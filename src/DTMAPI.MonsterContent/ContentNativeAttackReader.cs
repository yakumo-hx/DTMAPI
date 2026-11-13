using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MonsterContent
{
    internal static class ContentNativeAttackReader
    {
        internal static List<MonsterNativeAttackParameters> Read(object? value, ContentReadContext context, string fieldPrefix = "nativeAttacks")
        {
            return context.Json.Array(value, fieldPrefix).Select((row, index) => {
                var json = fieldPrefix == "nativeAttacks" ? context.Json : context.Json.MapPath(field =>
                {
                    string suffix = field.StartsWith("nativeAttacks", StringComparison.Ordinal) ? field.Substring("nativeAttacks".Length).TrimStart('[', ']') : "." + field;
                    return fieldPrefix + "[" + index + "]" + suffix;
                });
                var itemContext = fieldPrefix == "nativeAttacks" ? context : context.WithJson(json);
                var a = json.Object(row, "nativeAttacks[]", "attackId", "cooldownGameSeconds startOnCooldown damage criticalRate windupGameSeconds amoebaJump shoot multiShoot salvoShoot scatteringShoot dragonShoot scatteringThrow bombingDive batteringBullet battering fungusShoot sting chomperBite impact bulletTrain shadowImpact chomperMimicry thunderPile landmine timeBomb call randomShoot multiImpact fireSound dashSound throwSound volleySound readySound landSound countdownSound explosionSound");
                float? N(Dictionary<string, object?> data, string key) => !data.TryGetValue(key, out var v) || v == null ? (float?)null : (float)json.Number(v, "nativeAttacks." + key);
                var result = new MonsterNativeAttackParameters { AttackId = json.Text(a["attackId"], "nativeAttacks.attackId"),
                    CooldownGameSeconds = N(a, "cooldownGameSeconds"), Damage = N(a, "damage"), CriticalRate = N(a, "criticalRate"), WindupGameSeconds = N(a, "windupGameSeconds") };
                if (a.TryGetValue("fireSound", out var fire) && fire != null)
                    result.FireSound = itemContext.ReadSound(fire, "nativeAttacks.fireSound");
                if (a.TryGetValue("dashSound", out var dash) && dash != null)
                    result.DashSound = itemContext.ReadSound(dash, "nativeAttacks.dashSound");
                if (a.TryGetValue("throwSound", out var thrown) && thrown != null)
                    result.ThrowSound = itemContext.ReadSound(thrown, "nativeAttacks.throwSound");
                if (a.TryGetValue("volleySound", out var volley) && volley != null)
                    result.VolleySound = itemContext.ReadSound(volley, "nativeAttacks.volleySound");
                if (a.TryGetValue("readySound", out var ready) && ready != null)
                    result.ReadySound = itemContext.ReadSound(ready, "nativeAttacks.readySound");
                if (a.TryGetValue("landSound", out var land) && land != null)
                    result.LandSound = itemContext.ReadSound(land, "nativeAttacks.landSound");
                if (a.TryGetValue("countdownSound", out var countdown) && countdown != null)
                    result.CountdownSound = itemContext.ReadSound(countdown, "nativeAttacks.countdownSound");
                if (a.TryGetValue("explosionSound", out var explosion) && explosion != null)
                    result.ExplosionSound = itemContext.ReadSound(explosion, "nativeAttacks.explosionSound");
                if (a.TryGetValue("startOnCooldown", out var start) && start != null) result.StartOnCooldown = json.Bool(start, "nativeAttacks.startOnCooldown");
                if (a.TryGetValue("amoebaJump", out var jump) && jump != null)
                {
                    var j = json.Object(jump, "amoebaJump", "", "range speed distanceCells");
                    result.AmoebaJump = new MonsterAmoebaJumpParameters { Range = N(j, "range"), Speed = N(j, "speed"),
                        DistanceCells = j.TryGetValue("distanceCells", out var d) ? json.NullableInt(d, "amoebaJump.distanceCells") : null };
                }
                if (a.TryGetValue("randomShoot", out var randomShoot) && randomShoot != null)
                {
                    var s = json.Object(randomShoot, "randomShoot", "", "minBulletCount maxBulletCount shotIntervalGameSeconds projectileSpeed projectileLifetimeGameSeconds");
                    int? I(string key) => s.TryGetValue(key, out var v) ? json.NullableInt(v, "randomShoot." + key) : null;
                    float? R(string key) => !s.TryGetValue(key, out var v) || v == null ? (float?)null : (float)json.Number(v, "randomShoot." + key);
                    result.RandomShoot = new MonsterRandomShootParameters { MinBulletCount = I("minBulletCount"), MaxBulletCount = I("maxBulletCount"),
                        ShotIntervalGameSeconds = R("shotIntervalGameSeconds"), ProjectileSpeed = R("projectileSpeed"), ProjectileLifetimeGameSeconds = R("projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("shoot", out var shoot) && shoot != null)
                {
                    var s = json.Object(shoot, "shoot", "", "windupEnabled recoveryGameSeconds projectileSpeed projectileLifetimeGameSeconds");
                    result.Shoot = new MonsterShootParameters { RecoveryGameSeconds = N(s, "recoveryGameSeconds"), ProjectileSpeed = N(s, "projectileSpeed"),
                        ProjectileLifetimeGameSeconds = N(s, "projectileLifetimeGameSeconds"),
                        WindupEnabled = s.TryGetValue("windupEnabled", out var w) && w != null ? json.Bool(w, "shoot.windupEnabled") : (bool?)null };
                }
                if (a.TryGetValue("multiShoot", out var multiShoot) && multiShoot != null)
                {
                    var s = json.Object(multiShoot, "multiShoot", "", "windupEnabled shotGameSeconds minShotCount maxShotCount fixedDirection projectileSpeed projectileLifetimeGameSeconds");
                    bool? B(string key) => s.TryGetValue(key, out var v) && v != null ? json.Bool(v, "multiShoot." + key) : (bool?)null;
                    int? I(string key) => s.TryGetValue(key, out var v) ? json.NullableInt(v, "multiShoot." + key) : null;
                    result.MultiShoot = new MonsterMultiShootParameters { WindupEnabled = B("windupEnabled"), FixedDirection = B("fixedDirection"),
                        ShotGameSeconds = N(s, "shotGameSeconds"), MinShotCount = I("minShotCount"), MaxShotCount = I("maxShotCount"),
                        ProjectileSpeed = N(s, "projectileSpeed"), ProjectileLifetimeGameSeconds = N(s, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("salvoShoot", out var salvoShoot) && salvoShoot != null)
                {
                    var s = json.Object(salvoShoot, "salvoShoot", "", "windupEnabled bulletCount sectorAngleDegrees shotIntervalGameSeconds projectileSpeed projectileLifetimeGameSeconds");
                    result.SalvoShoot = new MonsterSalvoShootParameters {
                        WindupEnabled = s.TryGetValue("windupEnabled", out var w) && w != null ? json.Bool(w, "salvoShoot.windupEnabled") : (bool?)null,
                        BulletCount = s.TryGetValue("bulletCount", out var c) ? json.NullableInt(c, "salvoShoot.bulletCount") : null,
                        SectorAngleDegrees = N(s, "sectorAngleDegrees"), ShotIntervalGameSeconds = N(s, "shotIntervalGameSeconds"),
                        ProjectileSpeed = N(s, "projectileSpeed"), ProjectileLifetimeGameSeconds = N(s, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("scatteringShoot", out var scatteringShoot) && scatteringShoot != null)
                {
                    var s = json.Object(scatteringShoot, "scatteringShoot", "", "windupEnabled minBulletCount maxBulletCount sectorAngleDegrees burstCount burstIntervalGameSeconds directionMode angleOffsetDegrees reverseAngleOffset projectileSpeed projectileLifetimeGameSeconds");
                    bool? B(string key) => s.TryGetValue(key, out var v) && v != null ? json.Bool(v, "scatteringShoot." + key) : (bool?)null;
                    int? I(string key) => s.TryGetValue(key, out var v) ? json.NullableInt(v, "scatteringShoot." + key) : null;
                    result.ScatteringShoot = new MonsterScatteringShootParameters {
                        WindupEnabled = B("windupEnabled"), MinBulletCount = I("minBulletCount"), MaxBulletCount = I("maxBulletCount"),
                        SectorAngleDegrees = N(s, "sectorAngleDegrees"), BurstCount = I("burstCount"), BurstIntervalGameSeconds = N(s, "burstIntervalGameSeconds"),
                        DirectionMode = s.TryGetValue("directionMode", out var mode) && mode != null ?
                            (MonsterScatteringMode?)Enum.Parse(typeof(MonsterScatteringMode), json.Choice(mode, "scatteringShoot.directionMode", "Fixed", "Random", "Player", "Offset")) : null,
                        AngleOffsetDegrees = N(s, "angleOffsetDegrees"), ReverseAngleOffset = B("reverseAngleOffset"),
                        ProjectileSpeed = N(s, "projectileSpeed"), ProjectileLifetimeGameSeconds = N(s, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("dragonShoot", out var dragonShoot) && dragonShoot != null)
                {
                    var d = json.Object(dragonShoot, "dragonShoot", "", "windupEnabled minBulletCount maxBulletCount sectorCount sectorAngleDegrees shotIntervalGameSeconds projectileSpeed projectileLifetimeGameSeconds");
                    int? I(string key) => d.TryGetValue(key, out var v) ? json.NullableInt(v, "dragonShoot." + key) : null;
                    result.DragonShoot = new MonsterDragonShootParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? json.Bool(w, "dragonShoot.windupEnabled") : (bool?)null,
                        MinBulletCount = I("minBulletCount"), MaxBulletCount = I("maxBulletCount"), SectorCount = I("sectorCount"),
                        SectorAngleDegrees = N(d, "sectorAngleDegrees"), ShotIntervalGameSeconds = N(d, "shotIntervalGameSeconds"),
                        ProjectileSpeed = N(d, "projectileSpeed"), ProjectileLifetimeGameSeconds = N(d, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("scatteringThrow", out var scatteringThrow) && scatteringThrow != null)
                {
                    var d = json.Object(scatteringThrow, "scatteringThrow", "", "windupEnabled minBulletCount maxBulletCount sectorAngleDegrees recoveryGameSeconds projectileSpeed projectileLifetimeGameSeconds");
                    int? I(string key) => d.TryGetValue(key, out var v) ? json.NullableInt(v, "scatteringThrow." + key) : null;
                    result.ScatteringThrow = new MonsterScatteringThrowParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? json.Bool(w, "scatteringThrow.windupEnabled") : (bool?)null,
                        MinBulletCount = I("minBulletCount"), MaxBulletCount = I("maxBulletCount"),
                        SectorAngleDegrees = N(d, "sectorAngleDegrees"), RecoveryGameSeconds = N(d, "recoveryGameSeconds"),
                        ProjectileSpeed = N(d, "projectileSpeed"), ProjectileLifetimeGameSeconds = N(d, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("bombingDive", out var bombingDive) && bombingDive != null)
                {
                    var d = json.Object(bombingDive, "bombingDive", "", "windupEnabled minBulletCount maxBulletCount range travelDistance travelSpeed projectileLifetimeGameSeconds");
                    int? I(string key) => d.TryGetValue(key, out var v) ? json.NullableInt(v, "bombingDive." + key) : null;
                    result.BombingDive = new MonsterBombingDiveParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? json.Bool(w, "bombingDive.windupEnabled") : (bool?)null,
                        MinBulletCount = I("minBulletCount"), MaxBulletCount = I("maxBulletCount"),
                        Range = N(d, "range"), TravelDistance = N(d, "travelDistance"), TravelSpeed = N(d, "travelSpeed"),
                        ProjectileLifetimeGameSeconds = N(d, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("batteringBullet", out var batteringBullet) && batteringBullet != null)
                {
                    var d = json.Object(batteringBullet, "batteringBullet", "", "windupEnabled minBulletCount maxBulletCount attackDistance travelSpeed projectileHorizontalSpeed projectileLifetimeGameSeconds");
                    int? I(string key) => d.TryGetValue(key, out var v) ? json.NullableInt(v, "batteringBullet." + key) : null;
                    result.BatteringBullet = new MonsterBatteringBulletParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? json.Bool(w, "batteringBullet.windupEnabled") : (bool?)null,
                        MinBulletCount = I("minBulletCount"), MaxBulletCount = I("maxBulletCount"),
                        AttackDistance = N(d, "attackDistance"), TravelSpeed = N(d, "travelSpeed"),
                        ProjectileHorizontalSpeed = N(d, "projectileHorizontalSpeed"), ProjectileLifetimeGameSeconds = N(d, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("fungusShoot", out var fungusShoot) && fungusShoot != null)
                {
                    var f = json.Object(fungusShoot, "fungusShoot", "", "windupEnabled projectileSpeed projectileLifetimeGameSeconds");
                    result.FungusShoot = new MonsterFungusShootParameters {
                        WindupEnabled = f.TryGetValue("windupEnabled", out var w) && w != null ? json.Bool(w, "fungusShoot.windupEnabled") : (bool?)null,
                        ProjectileSpeed = N(f, "projectileSpeed"), ProjectileLifetimeGameSeconds = N(f, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("chomperBite", out var chomperBite) && chomperBite != null)
                {
                    var d = json.Object(chomperBite, "chomperBite", "", "windupEnabled range");
                    result.ChomperBite = new MonsterChomperBiteParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? json.Bool(w, "chomperBite.windupEnabled") : (bool?)null,
                        Range = N(d, "range") };
                }
                if (a.TryGetValue("bulletTrain", out var bulletTrain) && bulletTrain != null)
                {
                    var d = json.Object(bulletTrain, "bulletTrain", "", "windupEnabled volleyCount volleyIntervalGameSeconds bulletCount sectorAngleDegrees directionX directionY projectileSpeed projectileLifetimeGameSeconds");
                    int? I(string key) => d.TryGetValue(key, out var v) && v != null ? json.Int(v, "bulletTrain." + key) : (int?)null;
                    result.BulletTrain = new MonsterBulletTrainParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? json.Bool(w, "bulletTrain.windupEnabled") : (bool?)null,
                        VolleyCount = I("volleyCount"), BulletCount = I("bulletCount"), SectorAngleDegrees = I("sectorAngleDegrees"),
                        VolleyIntervalGameSeconds = N(d, "volleyIntervalGameSeconds"), DirectionX = N(d, "directionX"), DirectionY = N(d, "directionY"),
                        ProjectileSpeed = N(d, "projectileSpeed"), ProjectileLifetimeGameSeconds = N(d, "projectileLifetimeGameSeconds") };
                }
                if (a.TryGetValue("impact", out var impact) && impact != null)
                {
                    var d = json.Object(impact, "impact", "", "windupEnabled distance travelSpeed arcPath arcOffset");
                    result.Impact = new MonsterImpactParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? json.Bool(w, "impact.windupEnabled") : (bool?)null,
                        ArcPath = d.TryGetValue("arcPath", out var arc) && arc != null ? json.Bool(arc, "impact.arcPath") : (bool?)null,
                        Distance = N(d, "distance"), TravelSpeed = N(d, "travelSpeed"), ArcOffset = N(d, "arcOffset") };
                }
                if (a.TryGetValue("call", out var call) && call != null)
                {
                    var c = json.Object(call, "call", "", "minCount maxCount roomLimit");
                    int? I(string key) => c.TryGetValue(key, out var v) ? json.NullableInt(v, "call." + key) : null;
                    result.Call = new MonsterCallParameters { MinCount = I("minCount"), MaxCount = I("maxCount"), RoomLimit = I("roomLimit") };
                }
                if (a.TryGetValue("landmine", out var landmine) && landmine != null)
                {
                    var p = json.Object(landmine, "landmine", "", "range recoilGameSeconds throwImpulse liveGameSeconds reactionGameSeconds");
                    result.Landmine = new MonsterLandmineParameters { Range = N(p, "range"), RecoilGameSeconds = N(p, "recoilGameSeconds"), ThrowImpulse = N(p, "throwImpulse"), LiveGameSeconds = N(p, "liveGameSeconds"), ReactionGameSeconds = N(p, "reactionGameSeconds") };
                }
                if (a.TryGetValue("timeBomb", out var timeBomb) && timeBomb != null)
                {
                    var p = json.Object(timeBomb, "timeBomb", "", "range recoilGameSeconds throwImpulse count maxThrowImpulse");
                    result.TimeBomb = new MonsterTimeBombParameters { Range = N(p, "range"), RecoilGameSeconds = N(p, "recoilGameSeconds"), ThrowImpulse = N(p, "throwImpulse"), Count = p.TryGetValue("count", out var count) ? json.NullableInt(count, "timeBomb.count") : null, MaxThrowImpulse = N(p, "maxThrowImpulse") };
                }
                if (a.TryGetValue("thunderPile", out var thunderPile) && thunderPile != null)
                {
                    var d = json.Object(thunderPile, "thunderPile", "", "windupEnabled range minCount maxCount subWindupGameSeconds dropSpeed maxDropGameSeconds waitGameSeconds fadeGameSeconds");
                    int? I(string key) => d.TryGetValue(key, out var v) ? json.NullableInt(v, "thunderPile." + key) : null;
                    result.ThunderPile = new MonsterThunderPileParameters { WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? json.Bool(w, "thunderPile.windupEnabled") : (bool?)null, Range = N(d, "range"), MinCount = I("minCount"), MaxCount = I("maxCount"), SubWindupGameSeconds = N(d, "subWindupGameSeconds"), DropSpeed = N(d, "dropSpeed"), MaxDropGameSeconds = N(d, "maxDropGameSeconds"), WaitGameSeconds = N(d, "waitGameSeconds"), FadeGameSeconds = N(d, "fadeGameSeconds") };
                }
                if (a.TryGetValue("chomperMimicry", out var chomperMimicry) && chomperMimicry != null)
                {
                    var d = json.Object(chomperMimicry, "chomperMimicry", "", "minDecoyGameSeconds maxDecoyGameSeconds");
                    result.ChomperMimicry = new MonsterChomperMimicryParameters { MinDecoyGameSeconds = N(d, "minDecoyGameSeconds"), MaxDecoyGameSeconds = N(d, "maxDecoyGameSeconds") };
                }
                if (a.TryGetValue("multiImpact", out var multiImpact) && multiImpact != null)
                {
                    var d = json.Object(multiImpact, "multiImpact", "", "windupEnabled distance travelSpeed minExtraDashes maxExtraDashes");
                    int? I(string key) => d.TryGetValue(key, out var v) ? json.NullableInt(v, "multiImpact." + key) : null;
                    result.MultiImpact = new MonsterMultiImpactParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? json.Bool(w, "multiImpact.windupEnabled") : (bool?)null,
                        Distance = N(d, "distance"), TravelSpeed = N(d, "travelSpeed"),
                        MinExtraDashes = I("minExtraDashes"), MaxExtraDashes = I("maxExtraDashes") };
                }
                if (a.TryGetValue("shadowImpact", out var shadowImpact) && shadowImpact != null)
                {
                    var d = json.Object(shadowImpact, "shadowImpact", "", "windupEnabled distance travelSpeed");
                    result.ShadowImpact = new MonsterShadowImpactParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? json.Bool(w, "shadowImpact.windupEnabled") : (bool?)null,
                        Distance = N(d, "distance"), TravelSpeed = N(d, "travelSpeed") };
                }
                if (a.TryGetValue("sting", out var sting) && sting != null)
                {
                    var d = json.Object(sting, "sting", "", "windupEnabled distance travelSpeed");
                    result.Sting = new MonsterStingParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? json.Bool(w, "sting.windupEnabled") : (bool?)null,
                        Distance = N(d, "distance"), TravelSpeed = N(d, "travelSpeed") };
                }
                if (a.TryGetValue("battering", out var battering) && battering != null)
                {
                    var d = json.Object(battering, "battering", "", "windupEnabled attackDistance travelSpeed");
                    result.Battering = new MonsterBatteringParameters {
                        WindupEnabled = d.TryGetValue("windupEnabled", out var w) && w != null ? json.Bool(w, "battering.windupEnabled") : (bool?)null,
                        AttackDistance = N(d, "attackDistance"), TravelSpeed = N(d, "travelSpeed") };
                }
                return result;
            }).ToList();
        }
    }
}
