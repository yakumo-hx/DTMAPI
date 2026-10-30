using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void NativeContentChecks()
    {
        MonsterNativeOverride Input() => new MonsterNativeOverride {
            LocalId = "native", TargetId = "test-template", ProjectileTint = "#4080FF",
            Skin = new MonsterSkin { TemplateFingerprint = "fixture", Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLttAAAAABJRU5ErkJggg=="),
                Frames = new List<MonsterFrameMapping> { new MonsterFrameMapping { FrameId = "idle", Width = 1, Height = 1 } } },
            Drops = new MonsterDrops { Mode = MonsterDropMode.Replace, MinCount = 1, MaxCount = 1, Entries = new List<MonsterDropEntry> { new MonsterDropEntry { ItemId = "iron", Weight = 1 } } }
        };
        var catalog = AuthorCatalog(); var t = catalog.Templates[0];
        t.NativeOverrideFields = new[] { "skin", "drops", "projectileTint" };
        t.Presentation = new MonsterPresentationInfo("fixture", new[] { new MonsterFrameInfo("idle", 1, 1, .5f, .5f, 16) });
        Check(MonsterOverrideAdmission.Validate("A", Input(), catalog) == null, "Independent original fields admit from their own capabilities.");
        var input = new ContentInput { OwnerId = "A" }; input.NativeOverrides.Add(Input());
        string payload = ContentCodec.WriteContent(input);
        var parsed = ContentCodec.ReadContent(payload, "A", _ => input.NativeOverrides[0].Skin!.Png);
        Check(ContentCodec.WriteContent(parsed) == payload && ContentCompiler.Validate(parsed, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0,
            "All native resources survive payload/catalog roundtrip with content-addressed images.");
        try { ContentCodec.ReadContent(payload, "A", _ => new byte[33]); Check(false, "Resource substitution must fail"); } catch (AuthoringException) { Check(true, "Foreign bytes fail content addressing"); }
        void Reject(Action<MonsterNativeOverride> change, string code)
        { var value = Input(); change(value); Check(MonsterOverrideAdmission.Validate("A", value, catalog)?.Code == code, "Native resource rejection: " + code); }
        Reject(v => v.Skin!.Frames.Add(v.Skin.Frames[0]), "InvalidNativeSkin");
        Reject(v => v.Drops!.Entries![0].ItemId = "missing", "ItemMissing");
        Reject(v => v.Drops!.Entries![0].MaxCount = 0, "InvalidDropPool");
        Reject(v => v.ProjectileTint = "blue", "InvalidNativeOverride");
        var original = Input(); var registry = new NativeOverrideRegistry(() => { }, (_, _) => 2, (_, _, v) => MonsterOverrideAdmission.Validate("A", v, catalog));
        var low = registry.Register("A", 1, original).Value!;
        original.Skin!.Png[0] = 0; original.Skin.Frames[0].X = 99; original.Drops!.Entries![0].ItemId = "mutated";
        registry.Refresh(true, 1);
        Check(low.Snapshot.State == MonsterOverrideState.Ready && registry.ResolveSkin("test-template")!.Png[0] == 137 && registry.ResolveDrops("test-template")!.Entries![0].ItemId == "iron", "Nested assets and drop entries freeze before caller mutation.");
        var highValue = Input(); highValue.Skin = null; highValue.ProjectileTint = null; highValue.Drops!.MaxCount = 2;
        var high = registry.Register("B", 2, highValue).Value!;
        Check(registry.ResolveDrops("test-template")!.MaxCount == 2 && registry.ResolveSkin("test-template") != null && registry.ResolveProjectileTint("test-template") == "#4080FF", "Extra draw SET does not replace independent skin/tint.");
        var extra = low.Snapshot.Fields.Single(f => f.Field == "drops");
        Check(extra.State == MonsterOverrideFieldState.Shadowed && extra.WinnerId == "B:native" && extra.RequestedResource != extra.EffectiveResource && extra.Requested == null, "Resource diagnostic distinguishes declarations without inventing a numeric baseline.");
        high.Dispose(); Check(registry.ResolveDrops("test-template")!.MaxCount == 1, "Withdrawal restores lower extra pool; it never merges pools.");
        registry.Refresh(false, 2); Check(registry.ResolveSkin("test-template") == null && registry.ResolveDrops("test-template") == null && registry.ResolveProjectileTint("test-template") == null, "Unavailable generation exposes no native content.");
        registry.Refresh(true, 3); low.Dispose(); Check(!registry.ReadyValues.Any(), "Final withdrawal clears all resource intent.");
    }
}
