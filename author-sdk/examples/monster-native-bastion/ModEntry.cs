using System;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterNativeBastion
{
    public sealed class ModEntry : DtmMod
    {
        private const string Owner = "Samples.MonsterNativeBastion";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            // Inherit the native stationary body, aiming line and MultiShoot attack.
            var bastion = Require(api.RegisterDefinition(new MonsterDefinition { LocalId = "bastion", TemplateId = "bastion",
                Display = new MonsterDisplay { Name = "原生固定炮台示例", Nameplate = true } }));
            owned.Register(bastion);
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("bastion", command =>
            {
                if (command.Arguments.Count == 3 && command.Arguments[0] == "spawn")
                {
                    var instance = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":bastion",
                        X = float.Parse(command.Arguments[1], CultureInfo.InvariantCulture), Y = float.Parse(command.Arguments[2], CultureInfo.InvariantCulture) }));
                    command.WriteLine("spawned " + instance.DefinitionId + " " + instance.Handle);
                }
                else if (command.Arguments.Count == 1 && command.Arguments[0] == "withdraw")
                { bastion.Dispose(); command.WriteLine("withdrawn " + bastion.Snapshot.State); }
                else command.WriteLine("bastion spawn <x> <y> | withdraw");
            }, description: "Spawn or withdraw a stationary native bastion.");
        }
        private static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
