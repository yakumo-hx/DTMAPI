using System;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterNativeGroups
{
    public sealed class ModEntry : DtmMod
    {
        private const string Owner = "Samples.MonsterNativeGroups";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            // Inherit the native room-wide FwbDroneGroup, including original drones.
            var definition = Require(api.RegisterDefinition(new MonsterDefinition { LocalId = "drone", TemplateId = "fwb_drone",
                Display = new MonsterDisplay { Name = "原生协同无人机示例", Nameplate = true } }));
            owned.Register(definition);
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("group", command =>
            {
                if (command.Arguments.Count == 3 && command.Arguments[0] == "spawn")
                {
                    var instance = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":drone",
                        X = float.Parse(command.Arguments[1], CultureInfo.InvariantCulture), Y = float.Parse(command.Arguments[2], CultureInfo.InvariantCulture) }));
                    command.WriteLine("spawned " + instance.DefinitionId + " " + instance.Handle);
                }
                else if (command.Arguments.Count == 1 && command.Arguments[0] == "withdraw")
                {
                    definition.Dispose(); command.WriteLine("withdrawn " + definition.Snapshot.State);
                }
                else command.WriteLine("group spawn <x> <y> | withdraw");
            }, description: "Spawn or withdraw an inherited native group drone.");
        }
        private static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
