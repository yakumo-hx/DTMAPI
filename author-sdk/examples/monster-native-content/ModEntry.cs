using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.MonsterContent;

namespace Samples.MonsterNativeContent
{
    // Diagnostic geometry only. MC-30 supplies its own complete authored atlas.
    public sealed class ModEntry : DtmMod
    {
        const string DiagnosticPng = "iVBORw0KGgoAAAANSUhEUgAAAQAAAAEACAYAAABccqhmAAADH0lEQVR4nO3UIQEAIBDAwE+KJD8NIAZiJ85Pbc5eF2ia3wHAPwYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQYAYQ9+vCh+f80gqwAAAABJRU5ErkJggg==";
        public override void Entry(IDtmHelper helper)
        {
            var api = helper.ModRegistry.GetApi<IMonsterContentApi>(MonsterModule.ProviderId) ?? throw new InvalidOperationException("Update DTMAPI Runtime to the matching candidate");
            var owned = helper.GetRequiredService<IDtmOwnedResources>("0.7.0", "0.7.0");
            owned.Register(Require(api.RequestModule()));
            var leases = new List<MonsterOverrideLease>();
            void Add(MonsterNativeOverride value) { var lease = Require(api.RegisterOverride(value)); leases.Add(lease); owned.Register(lease); }
            Add(new MonsterNativeOverride { LocalId = "blue-projectiles", TargetId = "drone", ProjectileTint = "#4080FF" });
            Add(new MonsterNativeOverride { LocalId = "extra-seed", TargetId = "seed_carrier", Drops = new MonsterDrops { Mode = MonsterDropMode.Append,
                MinCount = 1, MaxCount = 1, Entries = new List<MonsterDropEntry> { new MonsterDropEntry { ItemId = "seed_corn", Weight = 1 } }
            } });
            bool skinAdded = false;
            helper.GetRequiredService<IDtmCommands>("0.7.0", "0.7.0").Register("mc29", command => {
                if (command.Arguments.Count == 1 && command.Arguments[0] == "apply-skin" && !skinAdded)
                {
                    var template = Require(api.GetCatalog()).Templates.Single(t => t.Id == "bee");
                    Add(new MonsterNativeOverride { LocalId = "body", TargetId = "bee", Skin = new MonsterSkin {
                        TemplateFingerprint = template.Presentation.Fingerprint, Png = Convert.FromBase64String(DiagnosticPng),
                        Frames = template.Presentation.Frames.Select(f => new MonsterFrameMapping { FrameId = f.Id, Width = f.Width, Height = f.Height }).ToList()
                    } });
                    skinAdded = true; command.WriteLine("skin registered"); return;
                }
                if (command.Arguments.Count == 1 && command.Arguments[0] == "withdraw")
                { foreach (var lease in leases) lease.Dispose(); command.WriteLine("native content withdrawn"); return; }
                foreach (var row in Require(api.GetOverrides()).Where(r => r.OwnerId == "Samples.MonsterNativeContent"))
                    command.WriteLine(row.Id + " " + row.State + " " + string.Join("; ", row.Fields.Select(f => f.Field + "=" + (f.EffectiveTint ?? f.EffectiveResource) + " winner=" + f.WinnerId)));
            }, description: "status | apply-skin | withdraw; original drops remain, extra seeds are independent draws.");
        }
        static T Require<T>(MonsterResult<T> result) => result.Success ? result.Value! : throw new InvalidOperationException(result.Diagnostic!.Code + ": " + result.Diagnostic.Message);
    }
}
