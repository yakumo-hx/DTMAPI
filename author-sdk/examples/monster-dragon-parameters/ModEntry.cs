using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterDragonShootParameters
{
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterDragonShootParameters";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var definitions = new List<MonsterDefinitionLease>();
            foreach (string id in new[] { "sweep", "line" })
            {
                bool line = id == "line";
                var definition = new MonsterDefinition { LocalId = id, TemplateId = "drone_ex", Health = 500, Defense = 0, Behavior = "inherit-native",
                    NativeAttacks = new List<MonsterNativeAttackParameters> { new MonsterNativeAttackParameters {
                        AttackId = "AtkSkill01", CooldownGameSeconds = line ? .75f : 4, StartOnCooldown = !line,
                        Damage = line ? 3 : 17, CriticalRate = line ? 0 : 1, WindupGameSeconds = .6f,
                        DragonShoot = new DTMAPI.MonsterContent.MonsterDragonShootParameters { WindupEnabled = !line, MinBulletCount = line ? 3 : 8, MaxBulletCount = line ? 3 : 8, SectorCount = line ? 1 : 3,
                            SectorAngleDegrees = line ? 360 : 90, ShotIntervalGameSeconds = line ? .15f : .1f,
                            ProjectileSpeed = line ? 10 : 4, ProjectileLifetimeGameSeconds = line ? 1 : 3 }
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
                    native = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "sweep", TargetId = "drone_ex",
                        NativeAttacks = new List<MonsterNativeAttackParameters> { new MonsterNativeAttackParameters {
                            AttackId = "AtkSkill01", CooldownGameSeconds = 6, StartOnCooldown = false, Damage = 23, CriticalRate = 1, WindupGameSeconds = .8f,
                            DragonShoot = new DTMAPI.MonsterContent.MonsterDragonShootParameters { WindupEnabled = false, MinBulletCount = 8, MaxBulletCount = 8, SectorCount = 3, SectorAngleDegrees = 120,
                                ShotIntervalGameSeconds = .2f, ProjectileSpeed = 6, ProjectileLifetimeGameSeconds = 2 }
                        } } })); owned.Register(native); c.WriteLine("enabled " + native.Snapshot.State);
                } else if (a.Count == 1 && a[0] == "withdraw-native") {
                    native?.Dispose(); native = null; c.WriteLine("withdrawn Withdrawn");
                } else if (a.Count == 1 && a[0] == "withdraw") {
                    native?.Dispose(); native = null; foreach (var lease in definitions) lease.Dispose(); c.WriteLine("withdrawn Withdrawn");
                } else c.WriteLine("parameters spawn <sweep|line> <x> <y> | enable-native | withdraw-native | withdraw");
            }, description: "Native DragonShoot endpoint-repeating sweep and complete sequence timing.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
