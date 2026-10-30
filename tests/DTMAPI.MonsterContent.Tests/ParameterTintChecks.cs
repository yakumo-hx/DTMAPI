using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void ParameterTintChecks()
    {
        foreach (float? duration in new float?[] { null, 0, .75f, 10 })
        {
            var definition = Definition(); definition.HurtDuration = duration;
            Check(MonsterValidation.Validate("A", definition) == null, "Native hurt interval admits inherit, zero and bounded game seconds.");
            var record = new DefinitionRecord("A", definition, 1);
            definition.HurtDuration = 9;
            Check(record.Definition.HurtDuration == duration, "Definition publication freezes the selected interval.");
        }
        foreach (float duration in new[] { -.1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            var definition = Definition(); definition.HurtDuration = duration;
            Check(MonsterValidation.Validate("A", definition)?.Field == "hurtDuration", "Invalid intervals fail at the authored field.");
        }

        var catalog = AuthorCatalog();
        var input = new ContentInput { OwnerId = "A" };
        input.Definitions.Add(new MonsterDefinition { LocalId = "slow", TemplateId = "test-template", HurtDuration = 1.25f });
        input.Definitions.Add(new MonsterDefinition { LocalId = "inherited", TemplateId = "test-template" });
        input.NativeOverrides.Add(new MonsterNativeOverride { LocalId = "blue", TargetId = "test-template", Tint = "#4080FF" });
        Check(ContentCompiler.Validate(input, catalog).Any(d => d.Code == "NativeOverrideUnsupported"), "Old catalog cannot grant native tint.");
        catalog.Templates[0].NativeOverrideFields = new[] { "defense", "hurtDuration", "tint" };
        Check(ContentCompiler.Validate(input, catalog).Any(d => d.Code == "DefinitionFieldUnsupported" && d.Field == "hurtDuration"), "New-definition editing needs its own catalog capability.");
        catalog.Templates[0].HurtDuration = .35f;
        var parsedCatalog = CatalogCodec.Read(CatalogCodec.Write(catalog));
        Check(parsedCatalog.Templates[0].HurtDuration == .35f && parsedCatalog.Templates[0].EditableFields.Contains("hurtDuration") && !parsedCatalog.Templates[0].RuntimeVerified,
            "Catalog distinguishes a raw parameter and editable scopes from runtime observation.");
        Check(ContentCompiler.Validate(input, parsedCatalog).Count == 0, "Current catalog admits both scopes independently.");
        var payload = ContentCodec.WriteContent(input);
        var parsed = ContentCodec.ReadContent(payload, "A", _ => throw new Exception("No image expected"));
        Check(parsed.Definitions[0].HurtDuration == 1.25f && parsed.Definitions[1].HurtDuration == null && parsed.NativeOverrides[0].Tint == "#4080FF" && ContentCodec.WriteContent(parsed) == payload,
            "Author payload preserves explicit/inherited intervals and independent native tint.");
        const string project = "{\"format\":\"dtmapi.monster-project\",\"version\":4,\"pack\":{\"id\":\"A\",\"name\":\"Parameters\"},\"monsters\":[{\"id\":\"slow\",\"templateId\":\"test-template\",\"overrides\":{\"hurtDuration\":1.25},\"behavior\":{\"mode\":\"inherit-native\"},\"drops\":{\"mode\":\"inherit\",\"minCount\":null,\"maxCount\":null}},{\"id\":\"inherited\",\"templateId\":\"test-template\",\"overrides\":{},\"behavior\":{\"mode\":\"inherit-native\"},\"drops\":{\"mode\":\"inherit\",\"minCount\":null,\"maxCount\":null}}],\"nativeOverrides\":[{\"id\":\"blue\",\"targetId\":\"test-template\",\"fields\":{\"tint\":\"#4080FF\"}}],\"dropPools\":[],\"spawnRules\":[],\"assets\":[],\"dependencies\":[]}";
        Check(ContentCodec.WriteContent(ContentCodec.ReadProject(project)) == payload, "Editor/handwritten project and direct C# have identical normalized semantics.");

        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 2, (_, _, _) => null);
        var blue = new MonsterNativeOverride { LocalId = "blue", TargetId = "test-template", Tint = "#4080FF" };
        var high = registry.Register("Later", 2, blue).Value!;
        var low = registry.Register("Earlier", 1, new MonsterNativeOverride { LocalId = "red", TargetId = "test-template", Tint = "#FF0000", Defense = 17 }).Value!;
        blue.Tint = "#00FF00";
        Check(registry.ResolveTint("test-template") == null, "Waiting-native declarations do not tint live objects.");
        registry.Refresh(true, 1);
        Check(registry.ResolveTint("test-template") == "#4080FF" && registry.Resolve("test-template", "defense", 2) == 17 && registry.ResolveTint("dtm_mc_variant") == null,
            "Tint freezes caller input, respects load order despite reverse registration, and does not replace another field or definition.");
        var row = low.Snapshot.Fields.Single(f => f.Field == "tint");
        Check(row.Requested == null && row.Baseline == null && row.RequestedTint == "#FF0000" && row.EffectiveTint == "#4080FF" && row.WinnerId == "Later:blue" && row.State == MonsterOverrideFieldState.Shadowed,
            "Tint diagnostics report color intent/winner without inventing one baseline for all renderer instances.");
        foreach (string bad in new[] { "", "blue", "#fff", "#12345G", "#00000000" })
            Check(!registry.Register("Invalid", 9, new MonsterNativeOverride { LocalId = "bad", TargetId = "test-template", Tint = bad, Defense = 99 }).Success && registry.Resolve("test-template", "defense", 2) == 17,
                "Invalid tint rejects the whole declaration, including its numeric field.");
        var black = registry.Register("Later", 2, new MonsterNativeOverride { LocalId = "black", TargetId = "test-template", Tint = "#000000" }).Value!;
        Check(registry.ResolveTint("test-template") == "#000000", "Black is an explicit valid color, not an unset value.");
        black.Dispose(); high.Dispose();
        Check(registry.ResolveTint("test-template") == "#FF0000", "Withdrawal reveals the remaining author's color.");
        registry.Refresh(false, 2);
        Check(registry.ResolveTint("test-template") == null && !registry.TintTargets.Any(), "Unavailable native generation removes effective tint.");
        registry.Refresh(true, 3); low.Dispose();
        Check(registry.ResolveTint("test-template") == null, "Final withdrawal restores native instance color by removing the multiplier.");
    }
}
