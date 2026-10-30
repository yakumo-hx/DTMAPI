using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterScatteringShootParameters
{
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterScatteringShootParameters";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var definitions = new List<MonsterDefinitionLease>();
            foreach (string id in new[] { "fan", "ring" })
            {
                bool ring = id == "ring";
                var definition = new MonsterDefinition { LocalId = id, TemplateId = "drone", Health = 500, Defense = 0, Behavior = "inherit-native",
                    NativeAttacks = new List<MonsterNativeAttackParameters> { new MonsterNativeAttackParameters {
                        AttackId = "AtkSkill01", CooldownGameSeconds = ring ? .75f : 4, StartOnCooldown = !ring,
                        Damage = ring ? 3 : 17, CriticalRate = ring ? 0 : 1, WindupGameSeconds = ring ? .15f : .2f,
                        ScatteringShoot = new DTMAPI.MonsterContent.MonsterScatteringShootParameters { WindupEnabled = !ring, MinBulletCount = ring ? 6 : 3, MaxBulletCount = ring ? 6 : 3,
                            SectorAngleDegrees = ring ? 360 : 90, BurstCount = ring ? 2 : 3, BurstIntervalGameSeconds = ring ? .1f : .15f,
                            DirectionMode = MonsterScatteringMode.Offset, AngleOffsetDegrees = ring ? 15 : 20, ReverseAngleOffset = ring,
                            ProjectileSpeed = ring ? 10 : 4, ProjectileLifetimeGameSeconds = ring ? 1 : 3 }
                    } } };
                var lease = Require(api.RegisterDefinition(definition)); definitions.Add(lease); owned.Register(lease);
            }
            MonsterOverrideLease? native = null;
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("parameters", c => {
                var a = c.Arguments;
                if (a.Count == 4 && a[0] == "spawn") {
                    var value = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":" + a[1],
                        X = float.Parse(a[2], CultureInfo.InvariantCulture), Y = float.Parse(a[3], CultureInfo.InvariantCulture) }));
                    c.WriteLine("spawned " + value.Handle);
                } else if (a.Count == 1 && a[0] == "enable-native") {
                    if (native != null) { c.WriteLine("already enabled"); return; }
                    native = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "bursts", TargetId = "drone",
                        NativeAttacks = new List<MonsterNativeAttackParameters> { new MonsterNativeAttackParameters {
                            AttackId = "AtkSkill01", CooldownGameSeconds = 6, StartOnCooldown = false, Damage = 23, CriticalRate = 1, WindupGameSeconds = .2f,
                            ScatteringShoot = new DTMAPI.MonsterContent.MonsterScatteringShootParameters { WindupEnabled = false, MinBulletCount = 3, MaxBulletCount = 3, SectorAngleDegrees = 120,
                                BurstCount = 3, BurstIntervalGameSeconds = .3f, DirectionMode = MonsterScatteringMode.Offset, AngleOffsetDegrees = 15, ReverseAngleOffset = true, ProjectileSpeed = 6, ProjectileLifetimeGameSeconds = 2 }
                        } } })); owned.Register(native); c.WriteLine("enabled " + native.Snapshot.State);
                } else if (a.Count == 1 && a[0] == "withdraw-native") {
                    native?.Dispose(); native = null; c.WriteLine("withdrawn Withdrawn");
                } else if (a.Count == 1 && a[0] == "withdraw") {
                    native?.Dispose(); native = null; foreach (var lease in definitions) lease.Dispose(); c.WriteLine("withdrawn Withdrawn");
                } else c.WriteLine("parameters spawn <fan|ring> <x> <y> | enable-native | withdraw-native | withdraw");
            }, description: "Native ScatteringShoot per-burst count/sector and captured direction/timing.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
