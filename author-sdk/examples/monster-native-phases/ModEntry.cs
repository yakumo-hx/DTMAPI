using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterNativePhases
{
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterNativePhases";
        readonly Dictionary<MonsterHandle, Behavior> behaviors = new Dictionary<MonsterHandle, Behavior>();
        bool automatic = true;
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            owned.Register(Require(api.RegisterBehavior("encounter", new MonsterBehaviorRequirements {
                Movement = true, AttackIds = new List<string> { "AtkNormal" }
            }, () => new Behavior(behaviors, () => automatic))));
            var leases = new List<MonsterDefinitionLease>();
            foreach (var id in new[] { "cancel", "finish" })
            {
                var lease = Require(api.RegisterDefinition(Define(id, id == "cancel" ? MonsterPhaseTransition.CancelCurrent : MonsterPhaseTransition.FinishCurrent)));
                leases.Add(lease); owned.Register(lease);
            }
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("native-phases", c => {
                var a = c.Arguments;
                if (a.Count == 4 && a[0] == "spawn") {
                    var value = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":" + a[1],
                        X = float.Parse(a[2], CultureInfo.InvariantCulture), Y = float.Parse(a[3], CultureInfo.InvariantCulture) }));
                    c.WriteLine("spawned " + value.Handle);
                } else if (a.Count == 2 && a[0] == "auto") {
                    if (a[1] != "on" && a[1] != "off") throw new ArgumentException("Use auto on|off.");
                    automatic = a[1] == "on"; c.WriteLine("auto=" + automatic);
                } else if (a.Count >= 3 && a[0] == "cast") {
                    foreach (var value in Require(api.GetInstances()))
                        if (value.DefinitionId == Owner + ":" + a[1] && behaviors.TryGetValue(value.Handle, out var behavior))
                            c.WriteLine(behavior.Invoke(a[2], a.Count > 3 ? float.Parse(a[3], CultureInfo.InvariantCulture) : 10));
                } else if (a.Count == 1 && a[0] == "status") {
                    foreach (var value in Require(api.GetInstances())) c.WriteLine(value.DefinitionId + " hp=" + value.Health + "/" + value.MaxHealth
                        + " phase=" + value.HealthPhase!.Id + " pending=" + value.HealthPhase.PendingId + " revision=" + value.HealthPhase.Revision);
                } else if (a.Count == 1 && a[0] == "withdraw") {
                    foreach (var lease in leases) lease.Dispose(); c.WriteLine("withdrawn Withdrawn");
                } else c.WriteLine("native-phases spawn <cancel|finish> <x> <y> | auto <on|off> | cast <cancel|finish> <attack|dash|move|cancel> [budget] | status | withdraw");
            }, description: "Native jump, movement and phase-two dash through ordinary Strict APIs.");
        }
        static MonsterDefinition Define(string id, MonsterPhaseTransition transition) => new MonsterDefinition {
            LocalId = id, TemplateId = "amoeba_radiation", Health = 500, Defense = 0,
            Body = new MonsterBodyParameters { Scale = 2 }, Movement = new MonsterMovementParameters { MoveSpeed = 3, JumpSpeed = 9 },
            Sight = new MonsterSightParameters { RangeScale = 1.5f },
            Behavior = "code-provider", BehaviorId = Owner + ":encounter",
            HealthPhases = new MonsterHealthPhases { Transition = transition, Stages = new List<MonsterHealthPhase> {
                new MonsterHealthPhase { Id = "enraged", AtOrBelow = .5f, SpeedMultiplier = 1.5f, DamageMultiplier = 2 } } },
            Skills = new List<MonsterSkill> { new MonsterSkill { Id = "charge", Kind = "dash", Dash = new MonsterDash {
                Distance = 2, Speed = 4, Damage = 11, Range = 16, WindupGameSeconds = .4f, RecoveryGameSeconds = .2f, CooldownGameSeconds = .5f } } },
            Display = new MonsterDisplay { Name = "原招式阶段示例 " + id, Nameplate = true, HealthBar = MonsterHealthBar.Always, BossBar = true }
        };
        sealed class Behavior : IMonsterBehavior
        {
            readonly Dictionary<MonsterHandle, Behavior> registry; readonly Func<bool> automatic;
            IMonsterBehaviorContext? context; MonsterHandle instance; MonsterActionHandle? action; float retry; int next;
            public Behavior(Dictionary<MonsterHandle, Behavior> registry, Func<bool> automatic) { this.registry = registry; this.automatic = automatic; }
            public void Start(IMonsterBehaviorContext value) { context = value; instance = Require(value.GetSelf()).Handle; registry.Add(instance, this); }
            public void Tick(float dt)
            {
                if (context == null || !automatic()) return;
                if (action.HasValue) {
                    var state = context.GetAction(action.Value); if (!state.Success) return;
                    if (state.Value!.State == MonsterActionState.Running) return;
                    if (state.Value.State == MonsterActionState.PhaseChanged) next = 0;
                    else next++; action = null; retry = .25f;
                }
                retry -= dt; if (retry > 0) return;
                var self = context.GetSelf(); var target = context.GetTarget();
                if (!self.Success || !target.Success || target.Value == null) { retry = .25f; return; }
                bool enraged = self.Value!.HealthPhase!.Index > 0;
                string kind = next % (enraged ? 3 : 2) == 0 ? "attack" : enraged && next % 3 == 1 ? "dash" : "move";
                string result = Invoke(kind, 8);
                if (!result.StartsWith("started=", StringComparison.Ordinal)) { next++; retry = .25f; }
            }
            public string Invoke(string kind, float budget)
            {
                if (context == null) return "StaleHandle";
                if (kind == "cancel") { if (!action.HasValue) return "NoAction"; var cancelled = context.Cancel(action.Value); return cancelled.Success ? "cancelled=" + cancelled.Value : cancelled.Diagnostic!.Code; }
                MonsterResult<MonsterActionHandle> result;
                if (kind == "attack") result = context.Attack("AtkNormal", budget);
                else if (kind == "dash") result = context.UseSkill("charge", budget);
                else if (kind == "move") {
                    var self = Require(context.GetSelf()); var target = context.GetTarget();
                    if (!target.Success) return target.Diagnostic!.Code;
                    if (target.Value == null) return "TargetUnavailable";
                    float direction = target.Value.X >= self.X ? 1 : -1;
                    result = context.MoveTo(self.X + direction * 2, self.Y, budget, 6);
                } else return "UnknownAction";
                if (!result.Success) return result.Diagnostic!.Code;
                action = result.Value; return "started=" + action.Value;
            }
            public void Stop() { context = null; registry.Remove(instance); }
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
