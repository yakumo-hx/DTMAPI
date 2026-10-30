using System;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterNativeNian
{
    public sealed class ModEntry : DtmMod
    {
        private const string Owner = "Samples.MonsterNativeNian";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            // One definition owns the complete encounter, including its weak tail and second stage.
            var nian = Require(api.RegisterDefinition(new MonsterDefinition { LocalId = "nian", TemplateId = "nian",
                Display = new MonsterDisplay { Name = "原生年兽与头尾分体示例", Nameplate = true } }));
            owned.Register(nian);
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("nian", command =>
            {
                if (command.Arguments.Count == 3 && command.Arguments[0] == "spawn")
                {
                    var instance = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":nian",
                        X = float.Parse(command.Arguments[1], CultureInfo.InvariantCulture), Y = float.Parse(command.Arguments[2], CultureInfo.InvariantCulture) }));
                    command.WriteLine("spawned " + instance.DefinitionId + " " + instance.Handle);
                }
                else if (command.Arguments.Count == 1 && command.Arguments[0] == "withdraw")
                { nian.Dispose(); command.WriteLine("withdrawn " + nian.Snapshot.State); }
                else command.WriteLine("nian spawn <x> <y> | withdraw");
            }, description: "Spawn or withdraw one complete native Nian encounter, including both second-stage parts.");
        }
        private static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
