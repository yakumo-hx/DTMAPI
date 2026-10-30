using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterBatteringBulletParameters
{
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterBatteringBulletParameters";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var definitions = new List<MonsterDefinitionLease>();
            foreach (string id in new[] { "impact", "quiet" })
            {
                bool quiet = id == "quiet";
                var definition = new MonsterDefinition { LocalId = id, TemplateId = "battering_ram", Health = 500, Defense = 0, Behavior = "inherit-native",
                    NativeAttacks = new List<MonsterNativeAttackParameters> { new MonsterNativeAttackParameters {
                        AttackId = "AtkSkill01", CooldownGameSeconds = quiet ? .75f : 4, StartOnCooldown = !quiet,
                        Damage = quiet ? 3 : 17, CriticalRate = quiet ? 0 : 1, WindupGameSeconds = .6f,
                        BatteringBullet = new DTMAPI.MonsterContent.MonsterBatteringBulletParameters { WindupEnabled = !quiet,
                            MinBulletCount = quiet ? 1 : 3, MaxBulletCount = quiet ? 1 : 3, AttackDistance = quiet ? 4 : 20,
                            TravelSpeed = quiet ? 16 : 8, ProjectileHorizontalSpeed = quiet ? 0 : 4, ProjectileLifetimeGameSeconds = quiet ? 1 : 3 }
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
                    native = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "impact", TargetId = "battering_ram",
                        NativeAttacks = new List<MonsterNativeAttackParameters> { new MonsterNativeAttackParameters {
                            AttackId = "AtkSkill01", CooldownGameSeconds = 6, StartOnCooldown = false, Damage = 23, CriticalRate = 1, WindupGameSeconds = .8f,
                            BatteringBullet = new DTMAPI.MonsterContent.MonsterBatteringBulletParameters { WindupEnabled = false,
                                MinBulletCount = 5, MaxBulletCount = 5, AttackDistance = 24, TravelSpeed = 12,
                                ProjectileHorizontalSpeed = 7, ProjectileLifetimeGameSeconds = 2 }
                        } } })); owned.Register(native); c.WriteLine("enabled " + native.Snapshot.State);
                } else if (a.Count == 1 && a[0] == "withdraw-native") {
                    native?.Dispose(); native = null; c.WriteLine("withdrawn Withdrawn");
                } else if (a.Count == 1 && a[0] == "withdraw") {
                    native?.Dispose(); native = null; foreach (var lease in definitions) lease.Dispose(); c.WriteLine("withdrawn Withdrawn");
                } else c.WriteLine("parameters spawn <impact|quiet> <x> <y> | enable-native | withdraw-native | withdraw");
            }, description: "Native terrain-ended body dash followed by parabolic impact bullets.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
