using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterCallParameters
{
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterCallParameters";
        static MonsterNativeAttackParameters Parameters(string id, bool native = false) {
            bool air = id == "air", silent = id == "silent";
            return new MonsterNativeAttackParameters { AttackId = air ? "AtkSkill04" : "AtkSkill01",
                CooldownGameSeconds = native ? (air ? 7 : 6) : silent ? .5f : air ? 5 : 4,
                StartOnCooldown = !native && !silent, WindupGameSeconds = native ? (air ? .5f : .4f) : silent ? 0 : air ? .8f : .6f,
                Call = new DTMAPI.MonsterContent.MonsterCallParameters { MinCount = silent ? 1 : native ? (air ? 3 : 2) : air ? 2 : 3,
                    MaxCount = silent ? 1 : native ? (air ? 3 : 2) : air ? 2 : 3, RoomLimit = silent ? 0 : 64 } };
        }
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0"); owned.Register(Require(api.RequestModule()));
            var definitions = new List<MonsterDefinitionLease>(); var overrides = new List<MonsterOverrideLease>();
            foreach (string id in new[] { "seed", "air", "silent" }) {
                var lease = Require(api.RegisterDefinition(new MonsterDefinition { LocalId = id, TemplateId = id == "air" ? "aircraft" : "seed_carrier", Health = 500, Defense = 0,
                    NativeAttacks = new List<MonsterNativeAttackParameters> { Parameters(id) } })); definitions.Add(lease); owned.Register(lease);
            }
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("parameters", c => {
                var a = c.Arguments;
                if (a.Count == 4 && a[0] == "spawn") {
                    var value = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":" + a[1], X = float.Parse(a[2], CultureInfo.InvariantCulture), Y = float.Parse(a[3], CultureInfo.InvariantCulture) })); c.WriteLine("spawned " + value.Handle);
                } else if (a.Count == 1 && a[0] == "enable-native") {
                    if (overrides.Count > 0) { c.WriteLine("already enabled"); return; }
                    foreach (string id in new[] { "seed", "air" }) {
                        var lease = Require(api.RegisterOverride(new MonsterNativeOverride { LocalId = id, TargetId = id == "air" ? "aircraft" : "seed_carrier", NativeAttacks = new List<MonsterNativeAttackParameters> { Parameters(id, true) } }));
                        overrides.Add(lease); owned.Register(lease);
                    } c.WriteLine("enabled both Call templates");
                } else if (a.Count == 1 && (a[0] == "withdraw-native" || a[0] == "withdraw")) {
                    foreach (var lease in overrides) lease.Dispose(); overrides.Clear();
                    if (a[0] == "withdraw") foreach (var lease in definitions) lease.Dispose(); c.WriteLine("withdrawn Withdrawn");
                } else c.WriteLine("parameters spawn <seed|air|silent> <x> <y> | enable-native | withdraw-native | withdraw");
            }, description: "Native Call parameters in both author domains; room limit counts all monsters.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
