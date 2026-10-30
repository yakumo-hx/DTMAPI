using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterMultiShootParameters
{
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterMultiShootParameters";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var definitions = new List<MonsterDefinitionLease>();
            foreach (string id in new[] { "fixed", "tracking", "bastion" })
            {
                bool quick = id == "tracking", bastion = id == "bastion";
                var definition = new MonsterDefinition { LocalId = id, TemplateId = bastion ? "bastion" : "drone_99", Health = 500,
                    Defense = 0, Behavior = "inherit-native", NativeAttacks = new List<MonsterNativeAttackParameters> { new MonsterNativeAttackParameters {
                        AttackId = "AtkNormal", CooldownGameSeconds = quick ? .75f : bastion ? 2 : 4, StartOnCooldown = !quick && !bastion,
                        Damage = quick ? 3 : bastion ? 11 : 17, CriticalRate = quick || bastion ? 0 : 1, WindupGameSeconds = bastion ? .4f : .6f,
                        MultiShoot = new DTMAPI.MonsterContent.MonsterMultiShootParameters { WindupEnabled = !quick, ShotGameSeconds = quick ? .15f : bastion ? .25f : .3f,
                            MinShotCount = quick || bastion ? 2 : 3, MaxShotCount = quick || bastion ? 2 : 3, FixedDirection = !quick,
                            ProjectileSpeed = quick ? 10 : bastion ? 6 : 4, ProjectileLifetimeGameSeconds = quick ? 1 : bastion ? 2 : 3 }
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
                    native = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "volley", TargetId = "drone_99",
                        NativeAttacks = new List<MonsterNativeAttackParameters> { new MonsterNativeAttackParameters {
                            AttackId = "AtkNormal", CooldownGameSeconds = 6, StartOnCooldown = false, Damage = 23, CriticalRate = 1, WindupGameSeconds = .8f,
                            MultiShoot = new DTMAPI.MonsterContent.MonsterMultiShootParameters { WindupEnabled = false, ShotGameSeconds = .4f, MinShotCount = 3, MaxShotCount = 3,
                                FixedDirection = true, ProjectileSpeed = 6, ProjectileLifetimeGameSeconds = 2 }
                        } } })); owned.Register(native); c.WriteLine("enabled " + native.Snapshot.State);
                } else if (a.Count == 1 && a[0] == "withdraw-native") {
                    native?.Dispose(); native = null; c.WriteLine("withdrawn Withdrawn");
                } else if (a.Count == 1 && a[0] == "withdraw") {
                    native?.Dispose(); native = null; foreach (var lease in definitions) lease.Dispose(); c.WriteLine("withdrawn Withdrawn");
                } else c.WriteLine("parameters spawn <fixed|tracking|bastion> <x> <y> | enable-native | withdraw-native | withdraw");
            }, description: "Native MultiShoot parameters and ordinary/bastion firing semantics.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
