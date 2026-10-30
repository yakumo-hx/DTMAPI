using System;
using System.Globalization;
using System.IO;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterCompendiumCode
{
    // Strict consumer: DTO registration and ordinary instance API, no JSON/native types.
    public sealed class ModEntry : DtmMod
    {
        const string Owner = "Samples.MonsterCompendiumCode";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId) ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var lease = Require(api.RegisterDefinition(Define())); owned.Register(lease);
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("compendium", c => {
                var a = c.Arguments;
                if (a.Count == 3 && a[0] == "spawn") {
                    var s = Require(api.Spawn(new MonsterSpawnRequest { DefinitionId = Owner + ":lantern", X = float.Parse(a[1], CultureInfo.InvariantCulture), Y = float.Parse(a[2], CultureInfo.InvariantCulture) }));
                    c.WriteLine("spawned " + s.Handle);
                } else if(a.Count == 1 && a[0] == "withdraw") { lease.Dispose(); c.WriteLine("withdrawn " + lease.Snapshot.State); }
                else if(a.Count == 1 && a[0] == "clear") { foreach(var s in Require(api.GetInstances())) Require(api.Despawn(s.Handle)); c.WriteLine("cleared"); }
                else c.WriteLine("compendium spawn <x> <y> | clear | withdraw");
            }, description: "Native compendium entry registered through Strict C#.");
        }
        public static MonsterDefinition Define()
        {
            byte[] png;
            using(var stream = typeof(ModEntry).Assembly.GetManifestResourceStream("CompendiumPreview.png") ?? throw new InvalidOperationException("Embedded preview missing."))
            using(var bytes = new MemoryStream()) { stream.CopyTo(bytes); png = bytes.ToArray(); }
            return new MonsterDefinition { LocalId = "lantern", TemplateId = "bee", Health = 40,
                Display = new MonsterDisplay { Name = "青灯蛾·C#", Nameplate = true, HealthBar = MonsterHealthBar.Always },
                Compendium = new MonsterCompendium { Title = "青灯蛾·C#", Habitat = "旧温室的阴影里", Description = "这份条目由普通 C# API 注册。\n图文与配置版独立，击杀和已读进度使用自己的内容 ID。", Png = png } };
        }
        static T Require<T>(MonsterResult<T> r) => r.Success ? r.Value! : throw new InvalidOperationException(r.Diagnostic!.Code + ": " + r.Diagnostic.Message);
    }
}
