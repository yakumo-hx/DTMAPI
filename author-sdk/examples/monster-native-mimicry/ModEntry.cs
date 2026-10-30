using System;
using System.Globalization;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterNativeMimicry
{
    public sealed class ModEntry : DtmMod
    {
        private const string Owner = "Samples.MonsterNativeMimicry";
        private IMonsterBehaviorContext? active;
        private MonsterActionHandle? action;

        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId)
                ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            owned.Register(Require(api.RegisterBehavior("manual", () => new Manual(this))));
            var chomper = Require(api.RegisterDefinition(new MonsterDefinition { LocalId = "chomper", TemplateId = "chomper",
                Display = new MonsterDisplay { Name = "原生拟态诱饵示例", Nameplate = true } }));
            var controlled = Require(api.RegisterDefinition(new MonsterDefinition { LocalId = "controlled", TemplateId = "chomper",
                Behavior = "code-provider", BehaviorId = Owner + ":manual" }));
            owned.Register(chomper); owned.Register(controlled);
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("mimicry", command =>
            {
                if (command.Arguments.Count == 3 && (command.Arguments[0] == "spawn" || command.Arguments[0] == "control"))
                {
                    var local = command.Arguments[0] == "spawn" ? "chomper" : "controlled";
                    var instance = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":" + local,
                        X = float.Parse(command.Arguments[1], CultureInfo.InvariantCulture), Y = float.Parse(command.Arguments[2], CultureInfo.InvariantCulture) }));
                    command.WriteLine("spawned " + instance.DefinitionId + " " + instance.Handle);
                }
                else if (command.Arguments.Count == 1 && command.Arguments[0] == "attacks")
                    command.WriteLine(active == null ? "No controlled instance; use mimicry control <x> <y>." :
                        string.Join("; ", Require(active.GetAttacks()).Select(a => a.Id + " ready=" + a.Available + " inRange=" + a.TargetInRange)));
                else if (command.Arguments.Count == 2 && command.Arguments[0] == "attack")
                {
                    if (active == null) throw new InvalidOperationException("No controlled instance.");
                    action = Require(active.Attack(command.Arguments[1], 8));
                    command.WriteLine("attack " + action);
                }
                else if (command.Arguments.Count == 1 && command.Arguments[0] == "cancel")
                    command.WriteLine(active == null || !action.HasValue ? "No action." : "cancelled " + Require(active.Cancel(action.Value)));
                else if (command.Arguments.Count == 1 && command.Arguments[0] == "status")
                    command.WriteLine(active == null || !action.HasValue ? "No action." : "state " + Require(active.GetAction(action.Value)).State);
                else if (command.Arguments.Count == 1 && command.Arguments[0] == "withdraw")
                { controlled.Dispose(); chomper.Dispose(); command.WriteLine("withdrawn " + chomper.Snapshot.State); }
                else command.WriteLine("mimicry spawn|control <x> <y> | attacks | attack <attackId> | cancel | status | withdraw");
            }, description: "Inherit chomper or request/cancel its existing native attacks from an ordinary C# behavior.");
        }
        private sealed class Manual : IMonsterBehavior
        {
            private readonly ModEntry entry;
            private IMonsterBehaviorContext? context;
            internal Manual(ModEntry entry) { this.entry = entry; }
            public void Start(IMonsterBehaviorContext value) { context = value; entry.active = value; entry.action = null; }
            public void Tick(float seconds) { }
            public void Stop() { if (ReferenceEquals(entry.active, context)) { entry.active = null; entry.action = null; } }
        }
        private static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
