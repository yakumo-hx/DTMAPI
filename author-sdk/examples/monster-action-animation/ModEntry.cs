using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterAnimationCode
{
    // Ordinary Strict author: managed DTOs and per-instance actions, no temporary JSON.
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterAnimationCode";
        readonly Dictionary<MonsterHandle, Behavior> behaviors = new Dictionary<MonsterHandle, Behavior>();
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId) ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            owned.Register(Require(api.RegisterBehavior("animate", () => new Behavior(behaviors))));
            owned.Register(Require(api.RegisterDefinition(Define())));
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("animation", c => {
                var a = c.Arguments;
                if (a.Count == 4 && a[0] == "spawn" && a[1] == "charge") {
                    var instance = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":charge", X = float.Parse(a[2], CultureInfo.InvariantCulture), Y = float.Parse(a[3], CultureInfo.InvariantCulture) }));
                    c.WriteLine("spawned " + instance.DefinitionId + " " + instance.Handle);
                } else if (a.Count == 1 && a[0] == "clear") {
                    foreach (var instance in Require(api.GetInstances())) Require(api.Despawn(instance.Handle)); c.WriteLine("cleared");
                } else if ((a.Count == 2 || a.Count == 3) && a[1] == "charge" && (a[0] == "cast" || a[0] == "cancel")) {
                    float limit = a.Count == 3 ? float.Parse(a[2], CultureInfo.InvariantCulture) : 10;
                    foreach (var instance in Require(api.GetInstances())) if (behaviors.TryGetValue(instance.Handle, out var behavior)) c.WriteLine(a[0] + " " + behavior.Invoke(a[0] == "cancel", limit));
                } else c.WriteLine("animation spawn charge <x> <y> | cast charge [budget] | cancel charge | clear");
            }, description: "One animated scatter action using the ordinary skill API.");
        }
        public static MonsterDefinition Define()
        {
            byte[] png;
            using (var stream = typeof(ModEntry).Assembly.GetManifestResourceStream("ActionAtlas.png") ?? throw new InvalidOperationException("Embedded atlas missing."))
            using (var bytes = new MemoryStream()) { stream.CopyTo(bytes); png = bytes.ToArray(); }
            return new MonsterDefinition { LocalId = "charge", TemplateId = "ball_drone", Health = 300, Behavior = "code-provider", BehaviorId = Owner + ":animate",
                Skills = new List<MonsterSkill> { new MonsterSkill { Id = "fan", Scatter = new MonsterScatter { ProjectileAttackId = "AtkNormal", BulletCount = 1, SpreadDegrees = 0,
                    BulletSpeed = 4, BulletLifetimeGameSeconds = 2, Damage = 11, Range = 12, WindupGameSeconds = .9f, RecoveryGameSeconds = .6f, CooldownGameSeconds = 5 },
                    Animation = new MonsterActionAnimation { Png = png, FrameWidth = 32, FrameHeight = 32, PivotX = .5f, PivotY = .5f, PixelsPerUnit = 16,
                        WindupFrames = new List<int>{0,1,2}, RecoveryFrames = new List<int>{3,4,5} } } },
                Display = new MonsterDisplay { Name = "动画 C#", Nameplate = true, HealthBar = MonsterHealthBar.Always } };
        }
        sealed class Behavior : IMonsterBehavior
        {
            readonly Dictionary<MonsterHandle, Behavior> registry;
            IMonsterBehaviorContext? context; MonsterHandle instance; MonsterActionHandle? action;
            public Behavior(Dictionary<MonsterHandle, Behavior> registry) { this.registry = registry; }
            public void Start(IMonsterBehaviorContext value) { context = value; instance = Require(value.GetSelf()).Handle; registry.Add(instance, this); }
            public void Tick(float gameSeconds) { }
            public string Invoke(bool cancel, float budget)
            {
                if (context == null) return "StaleHandle";
                if (cancel) { if (!action.HasValue) return "NoAction"; var result = context.Cancel(action.Value); return result.Success ? "cancelled=" + result.Value : result.Diagnostic!.Code; }
                var cast = context.UseSkill("fan", budget); if (cast.Success) { action = cast.Value; return "started=" + action; } return cast.Diagnostic!.Code;
            }
            public void Stop() { context = null; registry.Remove(instance); }
        }
        static T Require<T>(MonsterResult<T> r) => r.Success ? r.Value! : throw new InvalidOperationException(r.Diagnostic!.Code + ": " + r.Diagnostic.Message);
    }
}
