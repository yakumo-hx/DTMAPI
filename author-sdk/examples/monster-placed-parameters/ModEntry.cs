using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterPlacedParameters
{
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterPlacedParameters";
        static List<MonsterNativeAttackParameters> Parameters(bool brief, bool native = false) => new List<MonsterNativeAttackParameters> {
            new MonsterNativeAttackParameters { AttackId = "AtkNormal", CooldownGameSeconds = native ? 6 : brief ? .75f : 4,
                StartOnCooldown = !native && !brief, Damage = native ? 23 : brief ? 3 : 19, CriticalRate = native || brief ? 1 : 0,
                Landmine = new MonsterLandmineParameters { Range = native ? 35 : brief ? 30 : 40, RecoilGameSeconds = native ? .4f : brief ? 0 : .6f,
                    ThrowImpulse = native ? 4 : brief ? 0 : 3, LiveGameSeconds = native ? .6f : brief ? 0 : .4f, ReactionGameSeconds = native ? .6f : brief ? 0 : .3f } },
            new MonsterNativeAttackParameters { AttackId = "AtkSkill01", CooldownGameSeconds = native ? 7 : brief ? .5f : 5,
                StartOnCooldown = !native && !brief, Damage = native ? 31 : brief ? 5 : 29, CriticalRate = native || brief ? 0 : 1,
                TimeBomb = new MonsterTimeBombParameters { Range = native ? 35 : brief ? 30 : 40, RecoilGameSeconds = native ? .5f : brief ? 0 : .8f,
                    ThrowImpulse = native ? 3 : brief ? 0 : 2, MaxThrowImpulse = native ? 9 : brief ? 0 : 8, Count = native ? 2 : brief ? 0 : 3 } }
        };
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var definitions = new List<MonsterDefinitionLease>();
            foreach (string id in new[] { "charge", "brief" }) {
                var lease = Require(api.RegisterDefinition(new MonsterDefinition { LocalId = id, TemplateId = "bomber", Health = 500,
                    Defense = 0, Behavior = "inherit-native", NativeAttacks = Parameters(id == "brief") }));
                definitions.Add(lease); owned.Register(lease);
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
                    native = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "placement", TargetId = "bomber", NativeAttacks = Parameters(false, true) }));
                    owned.Register(native); c.WriteLine("enabled " + native.Snapshot.State);
                } else if (a.Count == 1 && a[0] == "withdraw-native") {
                    native?.Dispose(); native = null; c.WriteLine("withdrawn Withdrawn");
                } else if (a.Count == 1 && a[0] == "withdraw") {
                    native?.Dispose(); native = null; foreach (var lease in definitions) lease.Dispose(); c.WriteLine("withdrawn Withdrawn");
                } else c.WriteLine("parameters spawn <charge|brief> <x> <y> | enable-native | withdraw-native | withdraw");
            }, description: "Landmine and TimeBomb native placement parameters in both author domains.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
