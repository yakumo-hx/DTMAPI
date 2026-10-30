using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterNativeTraining
{
    public sealed class ModEntry : DtmMod
    {
        private const string Owner = "Samples.MonsterNativeTraining";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var definitions = new Dictionary<string, MonsterDefinitionLease>();
            foreach (var id in new[] { "target_01", "scarecrow" })
            {
                // Empty native attacks are intentional. A target child's break event
                // is independent of the root monster's health and death settlement.
                var definition = Require(api.RegisterDefinition(new MonsterDefinition { LocalId = id, TemplateId = id,
                    Display = new MonsterDisplay { Name = id == "target_01" ? "原生移动靶示例" : "原生稻草人示例", Nameplate = true } }));
                definitions.Add(id, definition); owned.Register(definition);
            }
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("training", command =>
            {
                if (command.Arguments.Count == 4 && command.Arguments[0] == "spawn" && definitions.ContainsKey(command.Arguments[1]))
                {
                    var instance = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":" + command.Arguments[1],
                        X = float.Parse(command.Arguments[2], CultureInfo.InvariantCulture), Y = float.Parse(command.Arguments[3], CultureInfo.InvariantCulture) }));
                    command.WriteLine("spawned " + instance.DefinitionId + " " + instance.Handle);
                }
                else if (command.Arguments.Count == 1 && command.Arguments[0] == "withdraw")
                {
                    foreach (var definition in definitions.Values) definition.Dispose();
                    command.WriteLine("withdrawn " + definitions["target_01"].Snapshot.State + " " + definitions["scarecrow"].Snapshot.State);
                }
                else command.WriteLine("training spawn <target_01|scarecrow> <x> <y> | withdraw");
            }, description: "Spawn or withdraw native training objects with no attacks.");
        }
        private static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
