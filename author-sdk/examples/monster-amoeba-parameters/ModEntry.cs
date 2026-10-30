using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterAmoebaParameters
{
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterAmoebaParameters";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var definitions = new List<MonsterDefinitionLease>();
            foreach (bool shortJump in new[] { false, true })
            {
                var definition = new MonsterDefinition {
                    LocalId = shortJump ? "short" : "long", TemplateId = "amoeba_radiation",
                    Health = 500, Defense = 0, Behavior = "inherit-native",
                    NativeAttacks = new List<MonsterNativeAttackParameters> { new MonsterNativeAttackParameters {
                        AttackId = "AtkNormal", CooldownGameSeconds = shortJump ? .75f : 4,
                        StartOnCooldown = !shortJump, Damage = shortJump ? 3 : 17,
                        CriticalRate = shortJump ? 0 : .35f, WindupGameSeconds = shortJump ? .2f : 1.2f,
                        AmoebaJump = new MonsterAmoebaJumpParameters { Range = shortJump ? 6 : 14,
                            Speed = shortJump ? 8 : 4, DistanceCells = shortJump ? 2 : 6 }
                    } }
                };
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
                    native = Require(api.RegisterOverride(new MonsterNativeOverride {
                        LocalId = "jump", TargetId = "amoeba_radiation",
                        NativeAttacks = new List<MonsterNativeAttackParameters> { new MonsterNativeAttackParameters {
                            AttackId = "AtkNormal", CooldownGameSeconds = 6, StartOnCooldown = false,
                            Damage = 23, CriticalRate = .6f, WindupGameSeconds = .8f,
                            AmoebaJump = new MonsterAmoebaJumpParameters { Range = 13, Speed = 3, DistanceCells = 4 }
                        } }
                    }));
                    owned.Register(native); c.WriteLine("enabled " + native.Snapshot.State);
                } else if (a.Count == 1 && a[0] == "withdraw-native") {
                    native?.Dispose(); native = null; c.WriteLine("withdrawn Withdrawn");
                } else if (a.Count == 1 && a[0] == "withdraw") {
                    native?.Dispose(); native = null;
                    foreach (var lease in definitions) lease.Dispose(); c.WriteLine("withdrawn Withdrawn");
                } else c.WriteLine("parameters spawn <long|short> <x> <y> | enable-native | withdraw-native | withdraw");
            }, description: "Separate new-definition and original-monster native jump parameters.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
