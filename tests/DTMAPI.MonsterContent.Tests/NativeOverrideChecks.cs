using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Hosting;
using DTMAPI.MonsterContent.Authoring;

static partial class Program
{
    static void NativeOverrideChecks()
    {
        MonsterNativeOverride Input() => new MonsterNativeOverride { LocalId = "armor", TargetId = "test-template", Defense = 12 };
        foreach (var change in new Action<MonsterNativeOverride>[] { v => v.LocalId = "", v => v.TargetId = "dtm_mc_invalid",
            v => v.Defense = -1, v => v.Defense = null, v => v.HurtDuration = float.NaN,
            v => v.HurtDuration = float.PositiveInfinity, v => v.HurtDuration = -1 })
        { var input = Input(); change(input); Check(MonsterOverrideAdmission.Validate("A", input)?.Code == "InvalidNativeOverride", "Invalid native field intent rejects before publication."); }
        var registry = new NativeOverrideRegistry(() => { }, (_, field) => field == "defense" ? 2 : .5, (_, _, _) => null);
        var value = Input(); var lease = registry.Register("A", 1, value).Value!;
        value.Defense = 999; registry.Refresh(true, 1);
        Check(registry.Resolve(value.TargetId, "defense", 2) == 12, "Accepted declarations freeze the caller's values.");
        Check(registry.Resolve("untouched", "defense", 44) == 44, "Other native targets retain their baseline.");
        Check(!registry.Register("A", 1, Input()).Success && registry.CountOwner("A") == 1, "Duplicates do not replace an existing lease.");
        Check(!registry.Register("A", 1, new MonsterNativeOverride { LocalId = "bad", TargetId = "test-template", Defense = -1, HurtDuration = .1f }).Success && registry.Resolve(value.TargetId, "hurtDuration", .5) == .5,
            "One invalid field rejects the whole declaration.");
        registry.Clear(); Check(lease.Snapshot.State == MonsterOverrideState.Withdrawn, "Module retirement withdraws all registrations.");

        var catalog = AuthorCatalog(); var content = new ContentInput { OwnerId = "A" }; content.NativeOverrides.Add(Input());
        Check(ContentCompiler.Validate(content, catalog).Single().Code == "NativeOverrideUnsupported", "Old catalogs cannot grant native override capability.");
        catalog.Templates[0].NativeOverrideFields = new[] { "defense", "hurtDuration" };
        Check(ContentCompiler.Validate(content, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0, "An override-only project validates against the serialized native capability.");
        string payload = ContentCodec.WriteContent(content);
        Check(ContentCodec.WriteContent(ContentCodec.ReadContent(payload, "A", _ => throw new Exception("No image"))) == payload, "Override payload roundtrips independently of new definitions.");
        foreach (string bad in new[] { payload.Replace("\"defense\":12", "\"defense\":12,\"health\":80"), payload.Replace("\"fields\":", "\"priority\":10,\"fields\":"), payload.Replace("\"schemaVersion\":1", "\"schemaVersion\":2") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); throw new Exception("Expected rejection"); } catch (AuthoringException) { Check(true, "Unsupported fields, old priority and old payload schemas reject explicitly."); } }
        var project = "{\"format\":\"dtmapi.monster-project\",\"version\":4,\"pack\":{\"id\":\"A\",\"name\":\"Overrides\"},\"monsters\":[],\"dropPools\":[],\"spawnRules\":[],\"assets\":[],\"dependencies\":[],\"nativeOverrides\":[{\"id\":\"armor\",\"targetId\":\"test-template\",\"fields\":{\"defense\":12}}]}";
        Check(ContentCodec.WriteContent(ContentCodec.ReadProject(project)) == payload, "Editor project and direct DTO produce identical normalized input.");
        foreach (string bad in new[] { project.Replace("\"version\":4", "\"version\":2"), project.Replace("\"fields\":", "\"priority\":10,\"fields\":") })
        { try { ContentCodec.ReadProject(bad); throw new Exception("Expected rejection"); } catch (AuthoringException) { Check(true, "Old projects and priority are not silently reinterpreted."); } }
    }
}
