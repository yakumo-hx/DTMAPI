using System;
using System.Globalization;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterNativeCalls
{
    public sealed class ModEntry : DtmMod
    {
        private const string Owner = "Samples.MonsterNativeCalls";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var definition = Require(api.RegisterDefinition(new MonsterDefinition
            {
                LocalId = "carrier", TemplateId = "aircraft", Behavior = "inherit-native",
                Display = new MonsterDisplay { Name = "原生召唤示例", Nameplate = true }
            }));
            owned.Register(definition);
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("calls", command =>
            {
                if (command.Arguments.Count == 1 && command.Arguments[0] == "parameters")
                {
                    var catalog = Require(api.GetCatalog());
                    foreach (string template in new[] { "aircraft", "seed_carrier" })
                    {
                        var slot = catalog.Templates.Single(t => t.Id == template).Actions.NativeAttacks.Single(a => a.Kind == "call");
                        var parameters = new MonsterNativeAttackParameters { AttackId = slot.AttackId, WindupGameSeconds = 1.25f, Call = new MonsterCallParameters { MinCount = 3, MaxCount = 3, RoomLimit = 6 } };
                        var configured = new MonsterDefinition { LocalId = template + "-configured", TemplateId = template };
                        configured.NativeAttacks.Add(parameters);
                        owned.Register(Require(api.RegisterDefinition(configured)));
                        command.WriteLine(template + "-configured: " + slot.AttackId + "; count=3, windup=1.25 game seconds, roomLimit=6");
                    }
                }
                else if (command.Arguments.Count == 3 && command.Arguments[0] == "spawn")
                {
                    var instance = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":carrier",
                        X = float.Parse(command.Arguments[1], CultureInfo.InvariantCulture), Y = float.Parse(command.Arguments[2], CultureInfo.InvariantCulture) }));
                    command.WriteLine("spawned " + instance.DefinitionId + " " + instance.Handle);
                }
                else if (command.Arguments.Count == 1 && command.Arguments[0] == "withdraw")
                { definition.Dispose(); command.WriteLine("withdrawn " + definition.Snapshot.State); }
                else command.WriteLine("calls parameters (once, after WorldReady) | spawn <x> <y> | withdraw; native aircraft -> bomber; " + definition.Snapshot.State);
            }, description: "Create an aircraft definition with its native Call and independent bomber children.");
        }
        private static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
