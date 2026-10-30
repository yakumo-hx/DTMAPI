using System;
using System.Collections.Generic;
using System.Globalization;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterScatterCode
{
    // Ordinary Strict author. No game assemblies, JSON or editor dependency.
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterScatterCode";
        readonly Dictionary<MonsterHandle, ScatterBehavior> behaviors = new Dictionary<MonsterHandle, ScatterBehavior>();
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId) ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            owned.Register(Require(api.RegisterBehavior("scatter", () => new ScatterBehavior(behaviors))));
            foreach (string id in new[] { "narrow", "wide", "zero", "short_range", "recovery" }) owned.Register(Require(api.RegisterDefinition(Define(id))));
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("scatter", c => {
                var a = c.Arguments;
                if (a.Count == 4 && a[0] == "spawn") {
                    var instance = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":" + a[1], X = float.Parse(a[2], CultureInfo.InvariantCulture), Y = float.Parse(a[3], CultureInfo.InvariantCulture) }));
                    c.WriteLine("spawned " + instance.DefinitionId + " " + instance.Handle);
                } else if (a.Count == 1 && a[0] == "clear") {
                    foreach (var instance in Require(api.GetInstances())) Require(api.Despawn(instance.Handle));
                    c.WriteLine("cleared");
                } else if (a.Count == 2 && (a[0] == "cast" || a[0] == "cancel")) {
                    foreach (var instance in Require(api.GetInstances())) if (instance.DefinitionId == Owner + ":" + a[1] && behaviors.TryGetValue(instance.Handle, out var behavior))
                        c.WriteLine(a[0] + " " + behavior.Invoke(a[0] == "cancel"));
                } else c.WriteLine("scatter spawn <narrow|wide|zero|short_range|recovery> <x> <y> | cast <id> | cancel <id> | clear");
            }, description: "Scatter through the ordinary per-instance skill API.");
        }
        public static MonsterDefinition Define(string id)
        {
            var s = new MonsterScatter { ProjectileAttackId = "AtkNormal", BulletCount = 3, SpreadDegrees = 30, BulletSpeed = 4, BulletLifetimeGameSeconds = 2,
                Damage = 7, Range = 12, WindupGameSeconds = .6f, RecoveryGameSeconds = .2f, CooldownGameSeconds = 5 };
            if (id == "wide") { s.BulletCount = 7; s.SpreadDegrees = 100; s.BulletSpeed = 8; s.BulletLifetimeGameSeconds = 1.5f; s.Damage = 13; s.WindupGameSeconds = 1; s.RecoveryGameSeconds = .5f; s.CooldownGameSeconds = 4; }
            if (id == "zero") { s.BulletCount = 1; s.SpreadDegrees = 180; s.Damage = 0; }
            if (id == "short_range") s.Range = 1;
            if (id == "recovery") { s.BulletCount = 1; s.BulletLifetimeGameSeconds = .2f; s.RecoveryGameSeconds = 2; }
            return new MonsterDefinition { LocalId = id, TemplateId = "ball_drone", Health = 300, Behavior = "code-provider", BehaviorId = Owner + ":scatter",
                Skills = new List<MonsterSkill> { new MonsterSkill { Id = "fan", Scatter = s } },
                Appearance = new MonsterAppearance { Tint = "#B599FF" }, Display = new MonsterDisplay { Name = "散射 C# · " + id, Nameplate = true, HealthBar = MonsterHealthBar.Always } };
        }
        sealed class ScatterBehavior : IMonsterBehavior
        {
            readonly Dictionary<MonsterHandle, ScatterBehavior> registry;
            IMonsterBehaviorContext? context; MonsterHandle instance; MonsterActionHandle? action;
            public ScatterBehavior(Dictionary<MonsterHandle, ScatterBehavior> registry) { this.registry = registry; }
            public void Start(IMonsterBehaviorContext value) { context = value; instance = Require(value.GetSelf()).Handle; registry.Add(instance, this); }
            public void Tick(float gameSeconds) { } // The sample's commands decide when to cast.
            public string Invoke(bool cancel)
            {
                if (context == null) return "StaleHandle";
                if (cancel) { if (!action.HasValue) return "NoAction"; var result = context.Cancel(action.Value); return result.Success ? "cancelled=" + result.Value : result.Diagnostic!.Code; }
                var cast = context.UseSkill("fan", 10);
                if (cast.Success) { action = cast.Value; return "started=" + action; }
                return cast.Diagnostic!.Code;
            }
            public void Stop() { context = null; registry.Remove(instance); }
        }
        static T Require<T>(MonsterResult<T> r) => r.Success ? r.Value! : throw new InvalidOperationException(r.Diagnostic!.Code + ": " + r.Diagnostic.Message);
    }
}
