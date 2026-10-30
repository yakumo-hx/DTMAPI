using System;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterCode
{
    // Ordinary Strict CodeMod: no game assemblies, reflection, JSON or editor required.
    public sealed class ModEntry : DtmMod
    {
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Install the matching Runtime 0.7.5 candidate.");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var definition = Require(api.RegisterDefinition(new MonsterDefinition
            {
                LocalId = "variant", TemplateId = "bee", Health = 90, Defense = 2,
                Appearance = new MonsterAppearance { Tint = "#B599FF" },
                Display = new MonsterDisplay { Name = "暮色巡游蜂", Nameplate = true, HealthBar = MonsterHealthBar.Always }
            }));
            owned.Register(definition);
            owned.Register(Require(api.RegisterSpawnRule(new MonsterSpawnRule
            {
                LocalId = "ruins", DefinitionId = "Samples.MonsterCode:variant",
                RoomId = "dungeon_旧城市废墟.住宅区-3", RegionId = "", Count = 1
            })));
            MonsterHandle last = default;
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("monster-example", command =>
            {
                var args = command.Arguments;
                string action = args.Count == 0 ? "status" : args[0];
                if (action == "spawn" && args.Count == 3)
                {
                    var result = api.Spawn(new MonsterSpawnRequest
                    {
                        DefinitionId = definition.Snapshot.Id,
                        X = float.Parse(args[1], CultureInfo.InvariantCulture),
                        Y = float.Parse(args[2], CultureInfo.InvariantCulture)
                    });
                    if (result.Success) { last = result.Value!.Handle; command.WriteLine("spawned hp=" + result.Value.Health + " id=" + result.Value.DefinitionId); }
                    else command.WriteLine(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
                }
                else if (action == "clear")
                {
                    var result = api.Despawn(last);
                    command.WriteLine(result.Success ? "despawned" : result.Diagnostic!.Code);
                }
                else command.WriteLine(definition.Snapshot.Id + " " + definition.Snapshot.State + " " + definition.Snapshot.Diagnostic?.Code);
            });
            helper.Monitor.Log("Monster example registered: Samples.MonsterCode:variant; inherited native behavior; hp=90.");
        }

        private static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value!
            : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message + " " + result.Diagnostic.Recovery);
    }
}
