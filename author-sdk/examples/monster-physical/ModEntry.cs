using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterPhysical
{
    public sealed class ModEntry : DtmMod
    {
        private const string Owner = "Samples.MonsterPhysical";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var leases = new List<MonsterDefinitionLease>();
            void Register(MonsterDefinition d) { var lease = Require(api.RegisterDefinition(d)); leases.Add(lease); owned.Register(lease); }
            Register(new MonsterDefinition { LocalId = "large", TemplateId = "amoeba", Health = 500,
                Body = new MonsterBodyParameters { Scale = 2.5f }, Movement = new MonsterMovementParameters { MoveSpeed = 4, JumpSpeed = 8 },
                Appearance = new MonsterAppearance { Tint = "#4080FF" }, Display = new MonsterDisplay { Name = "大体型变形虫", Nameplate = true } });
            Register(new MonsterDefinition { LocalId = "peer", TemplateId = "amoeba",
                Body = new MonsterBodyParameters { Scale = 1.25f }, Movement = new MonsterMovementParameters { MoveSpeed = 2, JumpSpeed = 12 } });
            Register(new MonsterDefinition { LocalId = "sentry", TemplateId = "bee",
                Movement = new MonsterMovementParameters { MoveSpeed = 3, MaxAcceleration = 8 },
                Sight = new MonsterSightParameters { RangeScale = 1.75f, EnemyThreshold = 1, MinAlertAddition = 2, MaxAlertAddition = 3, TooFarDistance = 25 } });
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("physical", command =>
            {
                if (command.Arguments.Count == 4 && command.Arguments[0] == "spawn")
                {
                    var monster = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":" + command.Arguments[1],
                        X = float.Parse(command.Arguments[2], CultureInfo.InvariantCulture), Y = float.Parse(command.Arguments[3], CultureInfo.InvariantCulture) }));
                    command.WriteLine("spawned " + monster.DefinitionId + " " + monster.Handle);
                }
                else if (command.Arguments.Count == 1 && command.Arguments[0] == "withdraw")
                { foreach (var lease in leases) lease.Dispose(); command.WriteLine("withdrawn Withdrawn"); }
                else command.WriteLine("physical spawn <large|peer|sentry> <x> <y> | withdraw");
            }, description: "Spawn independent native body, movement and sight examples.");
        }
        private static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
