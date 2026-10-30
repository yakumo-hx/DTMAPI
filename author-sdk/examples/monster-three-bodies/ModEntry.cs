using System;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterBodiesCode
{
    // Strict author: DTOs, ordinary owner-scoped API and commands only.
    public sealed class ModEntry : DtmMod
    {
        private const string Owner = "Samples.MonsterBodiesCode";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Install the matching Runtime 0.7.5 candidate.");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            foreach (var definition in new[] {
                Define("ground_melee", "amoeba_radiation", "地面近战 C#", 0),
                Define("flying_shooter", "ball_drone", "飞行射手 C#", 3, 4, 12),
                Define("fixed_turret", "fungus", "固定炮台 C#", null) })
                owned.Register(Require(api.RegisterDefinition(definition)));
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("bodies", command => {
                var args = command.Arguments;
                if (args.Count == 4 && args[0] == "spawn") {
                    var instance = Require(api.Spawn(new MonsterSpawnRequest {
                        DefinitionId = Owner + ":" + args[1],
                        X = float.Parse(args[2], CultureInfo.InvariantCulture),
                        Y = float.Parse(args[3], CultureInfo.InvariantCulture)
                    }));
                    command.WriteLine("spawned " + instance.DefinitionId + " " + instance.Handle);
                } else if (args.Count == 1 && args[0] == "clear") {
                    foreach (var instance in Require(api.GetInstances())) Require(api.Despawn(instance.Handle));
                    command.WriteLine("cleared");
                } else if (args.Count == 2 && args[0] == "cancel") {
                    foreach (var instance in Require(api.GetInstances()))
                        if (instance.DefinitionId == Owner + ":" + args[1])
                            command.WriteLine("cancelled=" + Require(api.CancelSequence(instance.Handle)));
                } else {
                    foreach (var instance in Require(api.GetInstances()))
                        command.WriteLine(instance.DefinitionId + " " + instance.Combat?.State + " " + instance.Sequence?.StepId);
                    command.WriteLine("bodies spawn <localId> <x> <y> | cancel <localId> | clear");
                }
            }, description: "Three template bodies using ordinary monster DTOs and finite actions.");
        }

        private static MonsterDefinition Define(string id, string template, string name, float? verticalOffset, float horizontalOffset = 1.25f, float maxTravel = 8)
        {
            var sequence = new MonsterSequence();
            if (verticalOffset.HasValue) sequence.Steps.Add(Move("outbound", horizontalOffset, verticalOffset.Value, maxTravel));
            sequence.Steps.Add(new MonsterSequenceStep { Id = "attack", AttackId = "AtkNormal", MaxGameSeconds = 15 });
            if (verticalOffset.HasValue) sequence.Steps.Add(Move("return", 0, 0, maxTravel));
            return new MonsterDefinition {
                LocalId = id, TemplateId = template, Health = 300,
                Appearance = new MonsterAppearance { Tint = "#B599FF" },
                Display = new MonsterDisplay { Name = name, Nameplate = true, HealthBar = MonsterHealthBar.Always },
                Behavior = "configured-sequence", Sequence = sequence,
                Combat = new MonsterCombatPolicy { Mode = "active", CheckIntervalGameSeconds = .5f, RepeatIntervalGameSeconds = 2 }
            };
        }
        private static MonsterSequenceStep Move(string id, float x, float y, float maxTravel) => new MonsterSequenceStep {
            Id = id, Action = "move-to", OffsetX = x, OffsetY = y, MaxTravel = maxTravel, MaxGameSeconds = 15
        };
        private static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value!
            : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
