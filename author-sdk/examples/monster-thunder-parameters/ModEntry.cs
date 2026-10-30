using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterThunderParameters
{
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterThunderParameters";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var definitions = new List<MonsterDefinitionLease>();
            foreach (string id in new[] { "charge", "brief" })
            {
                bool brief = id == "brief";
                var definition = new MonsterDefinition { LocalId = id, TemplateId = "space_ship", Health = 500, Defense = 0, Behavior = "inherit-native",
                    NativeAttacks = new List<MonsterNativeAttackParameters> { new MonsterNativeAttackParameters {
                        AttackId = "AtkSkill03", CooldownGameSeconds = brief ? .75f : 4, StartOnCooldown = !brief, Damage = brief ? 3 : 19, CriticalRate = brief ? 0 : 1, WindupGameSeconds = .6f,
                        ThunderPile = new MonsterThunderPileParameters { WindupEnabled = !brief, Range = brief ? 60 : 100, MinCount = brief ? 1 : 3, MaxCount = brief ? 1 : 3, SubWindupGameSeconds = brief ? 0 : .2f, DropSpeed = brief ? 7 : 12, MaxDropGameSeconds = brief ? .3f : 2, WaitGameSeconds = brief ? .1f : .4f, FadeGameSeconds = brief ? 0 : .3f }
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
                    native = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = "charge", TargetId = "space_ship",
                        NativeAttacks = new List<MonsterNativeAttackParameters> { new MonsterNativeAttackParameters { AttackId = "AtkSkill03", CooldownGameSeconds = 6, StartOnCooldown = false, Damage = 23, CriticalRate = 1, WindupGameSeconds = .4f,
                            ThunderPile = new MonsterThunderPileParameters { WindupEnabled = true, Range = 90, MinCount = 2, MaxCount = 2, SubWindupGameSeconds = .3f, DropSpeed = 10, MaxDropGameSeconds = 1.5f, WaitGameSeconds = .6f, FadeGameSeconds = .4f }
                        } } })); owned.Register(native); c.WriteLine("enabled " + native.Snapshot.State);
                } else if (a.Count == 1 && a[0] == "withdraw-native") {
                    native?.Dispose(); native = null; c.WriteLine("withdrawn Withdrawn");
                } else if (a.Count == 1 && a[0] == "withdraw") {
                    native?.Dispose(); native = null; foreach (var lease in definitions) lease.Dispose(); c.WriteLine("withdrawn Withdrawn");
                } else c.WriteLine("parameters spawn <charge|brief> <x> <y> | enable-native | withdraw-native | withdraw");
            }, description: "Native Thunder counts, timing and per-pile configuration.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
