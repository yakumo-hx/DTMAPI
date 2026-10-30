using System;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterNativeTrafficLight
{
    public sealed class ModEntry : DtmMod
    {
        private const string Owner = "Samples.MonsterNativeTrafficLight";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            // Inherit native global light timing, player-activity checks and all three attacks.
            var traffic_light = Require(api.RegisterDefinition(new MonsterDefinition { LocalId = "traffic_light", TemplateId = "traffic_light",
                Display = new MonsterDisplay { Name = "原生交通灯示例", Nameplate = true } }));
            owned.Register(traffic_light);
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("traffic_light", command =>
            {
                if (command.Arguments.Count == 3 && command.Arguments[0] == "spawn")
                {
                    var instance = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":traffic_light",
                        X = float.Parse(command.Arguments[1], CultureInfo.InvariantCulture), Y = float.Parse(command.Arguments[2], CultureInfo.InvariantCulture) }));
                    command.WriteLine("spawned " + instance.DefinitionId + " " + instance.Handle);
                }
                else if (command.Arguments.Count == 1 && command.Arguments[0] == "withdraw")
                { traffic_light.Dispose(); command.WriteLine("withdrawn " + traffic_light.Snapshot.State); }
                else command.WriteLine("traffic_light spawn <x> <y> | withdraw");
            }, description: "Spawn or withdraw a native traffic light using the shared battle clock.");
        }
        private static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
