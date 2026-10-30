using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterDashCode
{
    // Ordinary Strict author: no native references, temporary JSON or editor required.
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterDashCode";
        readonly Dictionary<MonsterHandle, DashBehavior> behaviors = new Dictionary<MonsterHandle, DashBehavior>();
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId) ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            owned.Register(Require(api.RegisterBehavior("dash", () => new DashBehavior(behaviors))));
            foreach (string id in new[] { "short", "long", "window", "early", "zero", "ground" }) owned.Register(Require(api.RegisterDefinition(Define(id))));
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("dash", c => {
                var a = c.Arguments;
                if (a.Count == 4 && a[0] == "spawn") {
                    var instance = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":" + a[1], X = Number(a[2]), Y = Number(a[3]) }));
                    c.WriteLine("spawned " + instance.DefinitionId + " " + instance.Handle);
                } else if (a.Count == 1 && a[0] == "clear") {
                    foreach (var instance in Require(api.GetInstances())) Require(api.Despawn(instance.Handle)); c.WriteLine("cleared");
                } else if (a.Count >= 2) {
                    foreach (var instance in Require(api.GetInstances())) if (instance.DefinitionId == Owner + ":" + a[1] && behaviors.TryGetValue(instance.Handle, out var behavior))
                        c.WriteLine(behavior.Invoke(a[0], a.Count >= 3 ? Number(a[2]) : 10, a.Count >= 4 ? Number(a[3]) : 0));
                } else c.WriteLine("dash spawn <short|long|window|early|zero|ground> <x> <y> | cast <id> [budget] | cancel <id> | move <id> <dx> <dy> | clear");
            }, description: "Bounded dash through ordinary monster actions.");
        }
        static float Number(string value) => float.Parse(value, CultureInfo.InvariantCulture);
        public static MonsterDefinition Define(string id)
        {
            var p = new MonsterDash { Distance = 4, Speed = 4, Damage = 7, Range = 16, WindupGameSeconds = .5f, RecoveryGameSeconds = .3f, CooldownGameSeconds = 2 };
            if (id == "long") { p.Distance = 8; p.Speed = 2; p.Damage = 13; }
            if (id == "window") { p.Distance = 6; p.Speed = 6; p.HitStartFraction = .75f; }
            if (id == "early") { p.Distance = 6; p.Speed = 6; p.HitEndFraction = .25f; }
            if (id == "zero") p.Damage = 0;
            if (id == "ground") { p.Distance = 2; p.Speed = 2; }
            return new MonsterDefinition { LocalId = id, TemplateId = id == "ground" ? "amoeba_radiation" : "ball_drone", Health = 300,
                Behavior = "code-provider", BehaviorId = Owner + ":dash", Skills = new List<MonsterSkill> { new MonsterSkill { Id = "charge", Kind = "dash", Dash = p } },
                Appearance = new MonsterAppearance { Tint = "#B599FF" }, Display = new MonsterDisplay { Name = "冲刺 C# · " + id, Nameplate = true, HealthBar = MonsterHealthBar.Always } };
        }
        sealed class DashBehavior : IMonsterBehavior
        {
            readonly Dictionary<MonsterHandle, DashBehavior> registry;
            IMonsterBehaviorContext? context; MonsterHandle instance; MonsterActionHandle? action;
            public DashBehavior(Dictionary<MonsterHandle, DashBehavior> registry) { this.registry = registry; }
            public void Start(IMonsterBehaviorContext value) { context = value; instance = Require(value.GetSelf()).Handle; registry.Add(instance, this); }
            public void Tick(float gameSeconds) { }
            public string Invoke(string command, float x, float y)
            {
                if (context == null) return "StaleHandle";
                if (command == "cancel") { if (!action.HasValue) return "NoAction"; var r = context.Cancel(action.Value); return r.Success ? "cancelled=" + r.Value : r.Diagnostic!.Code; }
                MonsterResult<MonsterActionHandle> cast;
                if (command == "move") { var self = Require(context.GetSelf()); cast = context.MoveTo(self.X + x, self.Y + y, 5, 16); }
                else if (command == "cast") cast = context.UseSkill("charge", x);
                else return "UnknownCommand";
                if (cast.Success) { action = cast.Value; return "started=" + action; }
                return cast.Diagnostic!.Code;
            }
            public void Stop() { context = null; registry.Remove(instance); }
        }
        static T Require<T>(MonsterResult<T> r) => r.Success ? r.Value! : throw new InvalidOperationException(r.Diagnostic!.Code + ": " + r.Diagnostic.Message);
    }
}
