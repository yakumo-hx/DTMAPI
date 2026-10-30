using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterScatteringThrowParameters
{
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterScatteringThrowParameters";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var definitions = new List<MonsterDefinitionLease>();
            foreach (string id in new[] { "fan", "rise" })
            {
                bool rise = id == "rise";
                var definition = new MonsterDefinition { LocalId = id, TemplateId = "seed_carrier", Health = 500, Defense = 0, Behavior = "inherit-native",
                    NativeAttacks = new List<MonsterNativeAttackParameters> { new MonsterNativeAttackParameters {
                        AttackId = "AtkNormal", CooldownGameSeconds = rise ? .75f : 4, StartOnCooldown = !rise,
                        Damage = rise ? 3 : 17, CriticalRate = rise ? 0 : 1, WindupGameSeconds = .6f,
                        ScatteringThrow = new DTMAPI.MonsterContent.MonsterScatteringThrowParameters { WindupEnabled = !rise, MinBulletCount = rise ? 1 : 3, MaxBulletCount = rise ? 1 : 3,
                            SectorAngleDegrees = rise ? 360 : 90, RecoveryGameSeconds = rise ? .15f : .4f,
                            ProjectileSpeed = rise ? 10 : 4, ProjectileLifetimeGameSeconds = rise ? 1 : 3 }
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
                    native = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "fan", TargetId = "seed_carrier",
                        NativeAttacks = new List<MonsterNativeAttackParameters> { new MonsterNativeAttackParameters {
                            AttackId = "AtkNormal", CooldownGameSeconds = 6, StartOnCooldown = false, Damage = 23, CriticalRate = 1, WindupGameSeconds = .8f,
                            ScatteringThrow = new DTMAPI.MonsterContent.MonsterScatteringThrowParameters { WindupEnabled = false, MinBulletCount = 5, MaxBulletCount = 5, SectorAngleDegrees = 120,
                                RecoveryGameSeconds = .4f, ProjectileSpeed = 6, ProjectileLifetimeGameSeconds = 2 }
                        } } })); owned.Register(native); c.WriteLine("enabled " + native.Snapshot.State);
                } else if (a.Count == 1 && a[0] == "withdraw-native") {
                    native?.Dispose(); native = null; c.WriteLine("withdrawn Withdrawn");
                } else if (a.Count == 1 && a[0] == "withdraw") {
                    native?.Dispose(); native = null; foreach (var lease in definitions) lease.Dispose(); c.WriteLine("withdrawn Withdrawn");
                } else c.WriteLine("parameters spawn <fan|rise> <x> <y> | enable-native | withdraw-native | withdraw");
            }, description: "Native upward fan with Drop initial velocity and recoil timing.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
