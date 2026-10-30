using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterInstanceCode
{
    // Ordinary CodeMod project is generated as examples/InstanceCode in the kit.
    public sealed class ModEntry : DtmMod
    {
        private const string Owner = "Samples.MonsterInstanceCode";
        private const string Room = "dungeon_多洛可河谷.stage2_巨大的树";
        private const float PauseGameSeconds = 1.5f;
        private readonly Dictionary<MonsterHandle, Patrol> active = new Dictionary<MonsterHandle, Patrol>();

        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Install the matching Runtime 0.7.5 candidate.");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            Action<string> report = message => helper.Monitor.Log("Patrol " + message);
            owned.Register(Require(api.RequestModule()));
            owned.Register(Require(api.RegisterBehavior("patrol", new MonsterBehaviorRequirements { Movement = true },
                () => new Patrol(api, active, report, PauseGameSeconds))));
            foreach (var id in new[] { "visitor", "keeper" })
            {
                owned.Register(Require(api.RegisterDefinition(new MonsterDefinition
                {
                    LocalId = id, TemplateId = "amoeba_radiation", Health = 140,
                    Behavior = "code-provider", BehaviorId = Owner + ":patrol",
                    Appearance = new MonsterAppearance { Tint = id == "visitor" ? "#EDB56A" : "#7ACBD1" },
                    Display = new MonsterDisplay { Name = id == "visitor" ? "短班巡卫" : "留守巡卫", Nameplate = true, HealthBar = MonsterHealthBar.Always }
                })));
                owned.Register(Require(api.RegisterSpawnRule(new MonsterSpawnRule
                {
                    LocalId = id, DefinitionId = Owner + ":" + id, RoomId = Room, Count = 1
                })));
            }
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("patrol", command =>
            {
                command.WriteLine("Each visible execution owns its origin, timer and action; base pause=" + Number(PauseGameSeconds));
                foreach (var patrol in active.Values) command.WriteLine(patrol.Describe());
                foreach (var instance in Require(api.GetInstances()))
                    command.WriteLine(instance.DefinitionId + " " + instance.Handle + " x=" + Number(instance.X) + " y=" + Number(instance.Y));
            }, description: "Read the two independent patrols; no spawn or movement command is needed.");
        }

        private sealed class Patrol : IMonsterBehavior
        {
            private readonly IMonsterContentApi api;
            private readonly Dictionary<MonsterHandle, Patrol> registry;
            private readonly Action<string> report;
            private readonly float basePause;
            private IMonsterBehaviorContext? context;
            private MonsterHandle instance;
            private MonsterActionHandle? action;
            private string definition = "", phase = "new";
            private float originX, originY, wait, clock, direction = 1;
            private int completedLegs;
            private bool visitor;

            public Patrol(IMonsterContentApi api, Dictionary<MonsterHandle, Patrol> registry, Action<string> report, float basePause)
            { this.api = api; this.registry = registry; this.report = report; this.basePause = basePause; }

            public void Start(IMonsterBehaviorContext value)
            {
                context = value;
                var self = Require(value.GetSelf());
                instance = self.Handle; definition = self.DefinitionId; originX = self.X; originY = self.Y;
                visitor = definition == Owner + ":visitor";
                wait = basePause * (visitor ? 1 : 3);
                registry.Add(instance, this);
                Change("waiting");
            }

            public void Tick(float gameSeconds)
            {
                if (context == null) return;
                clock += gameSeconds;
                if (action.HasValue)
                {
                    var state = Require(context.GetAction(action.Value));
                    if (state.State == MonsterActionState.Running) return;
                    report(Describe() + " terminal=" + state.State + " travel=" + Number(state.Travel));
                    action = null;
                    if (state.State == MonsterActionState.Completed) completedLegs++;
                    else { Change("blocked"); return; } // A real failed route stays diagnosable; never teleport or retry each frame.
                    if (visitor && completedLegs == 2)
                    {
                        Change("retiring");
                        Require(api.Despawn(instance)); // Ends only this author's instance, without a kill reward.
                        return;
                    }
                    wait = basePause * (visitor ? 1 : 3);
                    Change("waiting");
                    return;
                }
                if (phase != "waiting") return;
                wait -= gameSeconds;
                if (wait > 0) return;
                bool returning = completedLegs % 2 != 0;
                float targetX = returning ? originX : originX + direction * 3;
                var move = context.MoveTo(targetX, originY, 10, 12);
                // One bounded alternative when the outbound side is outside the native walkable map.
                if (!move.Success && !returning && (move.Diagnostic!.Code == "InvalidPosition" || move.Diagnostic.Code == "PathUnavailable"))
                {
                    direction = -direction;
                    targetX = originX + direction * 3;
                    move = context.MoveTo(targetX, originY, 10, 12);
                }
                if (!move.Success)
                {
                    report(Describe() + " rejected=" + move.Diagnostic!.Code);
                    Change("blocked");
                    return;
                }
                action = move.Value;
                Change(returning ? "returning" : "outbound");
            }

            public void Stop()
            {
                context = null; action = null;
                registry.Remove(instance);
                Change("stopped"); // The Host owns action/native cleanup; a closed context must not be reused here.
            }

            private void Change(string value) { phase = value; report(Describe()); }
            public string Describe() => definition + " instance=" + instance + " phase=" + phase + " clock=" + Number(clock)
                + " wait=" + Number(wait) + " legs=" + completedLegs + " action=" + action + " origin=" + Number(originX) + "," + Number(originY)
                + " basePause=" + Number(basePause);
        }

        private static string Number(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
        private static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value!
            : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
