using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static MonsterDefinition CompendiumDefinition()
    {
        var d = Definition();
        d.Compendium = new MonsterCompendium { Title = "青灯蛾", Habitat = "废弃温室", Description = "翅膀散发微光。\n这是原创图鉴说明。", Category = MonsterCompendiumCategory.Organism,
            Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLttAAAAABJRU5ErkJggg==") };
        return d;
    }
    static void CompendiumChecks()
    {
        Check(MonsterValidation.Validate("A", CompendiumDefinition()) == null, "Inherited definitions can opt into the compendium without code behavior.");
        foreach (var edit in new Action<MonsterCompendium>[] {
            c => c.Title = " ", c => c.Description = "<sprite=0>", c => c.Title = "x\0y",
            c => c.Category = (MonsterCompendiumCategory)20, c => c.Png = null!, c => c.Png[0] = 0 })
        {
            var d = CompendiumDefinition(); edit(d.Compendium!);
            Check(MonsterValidation.Validate("A", d)!.Code == "InvalidCompendium", "Invalid text, categories and PNG format reject before publication.");
        }
        var original = CompendiumDefinition(); var registry = new DefinitionRegistry(new Publisher { Ready = true }, () => { });
        var first = registry.Register("A", original).Value!;
        var second = registry.Register("B", CompendiumDefinition()).Value!;
        original.Compendium!.Png[0] = 0; original.Compendium.Title = "changed";
        var frozen = registry.Find("A", "A:variant")!.Definition.Compendium!;
        Check(frozen.Png[0] == 137 && frozen.Title == "青灯蛾", "Registered preview and text are frozen independently of author buffers.");
        Check(registry.Find("A", "A:variant")!.NativeId != registry.Find("B", "B:variant")!.NativeId, "Same local compendium IDs have distinct native progress keys.");
        first.Dispose();
        Check(registry.Find("A", "A:variant") == null && second.Snapshot.State == MonsterDefinitionState.Creatable, "Withdrawal releases its frozen preview without changing another owner.");
        var input = new ContentInput { OwnerId = "A" }; input.Definitions.Add(CompendiumDefinition());
        string payload = ContentCodec.WriteContent(input);
        byte[] png = input.Definitions[0].Compendium!.Png;
        Check(ContentCodec.WriteContent(ContentCodec.ReadContent(payload, "A", _ => png)) == payload, "Runtime payload preserves all compendium fields and content-addressed image bytes.");
        foreach (string bad in new[] { payload.Replace("Organism", "Default"), payload.Replace("\"showLocked\":true", "\"showLocked\":true,\"unlockOnView\":true") })
        {
            try { ContentCodec.ReadContent(bad, "A", _ => png); throw new Exception("Expected compendium rejection."); }
            catch (AuthoringException) { Check(true, "Unknown compendium intent is never silently discarded."); }
        }
        try { ContentCodec.ReadContent(payload, "A", _ => png.Concat(new byte[] { 0 }).ToArray()); throw new Exception("Expected hash rejection."); }
        catch (AuthoringException ex) { Check(ex.Diagnostic.Code == "ResourceHashMismatch", "A changed PNG cannot reuse a content-addressed preview path."); }
        Check(MonsterAdmission.Definition("A", CompendiumDefinition(), AuthorCatalog())!.Code == "CompendiumUnsupported", "Old catalogs cannot grant compendium capability.");
    }
}
