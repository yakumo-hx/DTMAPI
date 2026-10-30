using System;
using System.Globalization;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterNativeSkills
{
    public sealed class ModEntry : DtmMod
    {
        private const string Owner = "Samples.MonsterNativeSkills";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var definition = Require(api.RegisterDefinition(new MonsterDefinition
            {
                LocalId = "bomber", TemplateId = "bomber", Behavior = "inherit-native",
                Display = new MonsterDisplay { Name = "原生投放示例", Nameplate = true }
            }));
            owned.Register(definition);
            bool configured = false;
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("skills", command =>
            {
                if (command.Arguments.Count == 1 && command.Arguments[0] == "parameters")
                {
                    if (configured) { command.WriteLine("parameters already registered"); return; }
                    var slots = Require(api.GetCatalog()).Templates.Single(t => t.Id == "bomber").Actions.NativeAttacks;
                    var mine = slots.Single(s => s.Kind == "landmine");
                    var bomb = slots.Single(s => s.Kind == "time-bomb");
                    var edited = new MonsterDefinition { LocalId = "configured", TemplateId = "bomber" };
                    edited.NativeAttacks.Add(new MonsterNativeAttackParameters { AttackId = mine.AttackId, Damage = 21, CriticalRate = 0,
                        Landmine = new MonsterLandmineParameters { Range = 7, RecoilGameSeconds = 1.2f, ThrowImpulse = 3, LiveGameSeconds = .45f, ReactionGameSeconds = .9f } });
                    edited.NativeAttacks.Add(new MonsterNativeAttackParameters { AttackId = bomb.AttackId, Damage = 27, CriticalRate = 1,
                        TimeBomb = new MonsterTimeBombParameters { Range = 9, RecoilGameSeconds = .8f, ThrowImpulse = 2, MaxThrowImpulse = 8, Count = 3 } });
                    owned.Register(Require(api.RegisterDefinition(edited))); configured = true;
                    command.WriteLine("configured: " + mine.AttackId + " Landmine; " + bomb.AttackId + " TimeBomb");
                }
                else if (command.Arguments.Count == 3 && command.Arguments[0] == "spawn")
                {
                    var instance = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":bomber",
                        X = float.Parse(command.Arguments[1], CultureInfo.InvariantCulture), Y = float.Parse(command.Arguments[2], CultureInfo.InvariantCulture) }));
                    command.WriteLine("spawned " + instance.DefinitionId + " " + instance.Handle);
                }
                else if (command.Arguments.Count == 1 && command.Arguments[0] == "withdraw")
                { definition.Dispose(); command.WriteLine("withdrawn " + definition.Snapshot.State); }
                else command.WriteLine("skills parameters | spawn <x> <y> | withdraw; native Landmine and TimeBomb; " + definition.Snapshot.State);
            }, description: "Create a separate bomber definition using native AI and both original skill attacks.");
        }
        private static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
