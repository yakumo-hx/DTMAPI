using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void ImpactParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkSkill01", CooldownGameSeconds = 3,
            StartOnCooldown = false, Damage = 19, CriticalRate = 0, WindupGameSeconds = .4f,
            Impact = new MonsterImpactParameters { WindupEnabled = false, Distance = 6, TravelSpeed = 4, ArcPath = true, ArcOffset = -8 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var input = Parameters(); var frozen = new DefinitionRecord("A", Make(input), 1).Definition;
        input.Impact!.Distance = 80; input.Impact.ArcOffset = 30; input.Impact.ArcPath = false;
        Check(frozen.NativeAttacks[0].Impact!.Distance == 6 && frozen.NativeAttacks[0].Impact!.ArcOffset == -8 && frozen.NativeAttacks[0].Impact!.ArcPath == true,
            "Impact registration freezes its nested signed path settings.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill01", "impact", fields) };
        Check(fields.Length == 10 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Ten independent Impact fields admit in the exact slot.");
        foreach (var edit in new Action<MonsterImpactParameters>[] { p => p.Distance = -1, p => p.Distance = float.NaN,
            p => p.TravelSpeed = 0, p => p.TravelSpeed = float.PositiveInfinity,
            p => p.ArcOffset = float.NaN, p => p.ArcOffset = float.NegativeInfinity })
        { var row = Parameters(); edit(row.Impact!); Check(MonsterValidation.Validate("A", Make(row))?.Code == "InvalidNativeAttack", "Invalid Impact geometry/speed rejects before allocation."); }
        var empty = Parameters(); empty.Impact = new MonsterImpactParameters();
        Check(MonsterValidation.Validate("A", Make(empty))?.Code == "InvalidNativeAttack", "Empty Impact intent rejects even beside common fields.");
        var zero = Parameters(); zero.Impact!.Distance = 0; zero.Impact.TravelSpeed = .1f; zero.Impact.ArcPath = false; zero.Impact.ArcOffset = -100; zero.WindupGameSeconds = 0;
        Check(MonsterAdmission.Definition("A", Make(zero), catalog) == null, "Zero displacement/wait and negative control offset remain explicit with positive speed.");
        var native = new MonsterNativeOverride { LocalId = "impact", TargetId = template.Id }; native.NativeAttacks.Add(zero);
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content); var reopened = ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0,
            "New/native Impact inputs roundtrip through compiler and exact catalog.");
        foreach (string bad in new[] { payload.Replace("\"arcPath\":true", "\"arcPath\":1"), payload.Replace("\"windupEnabled\":false", "\"windupEnabled\":0"),
            payload.Replace("\"arcOffset\":-8", "\"path\":\"Sin\""), payload.Replace("\"arcOffset\":-8", "\"dashEase\":2") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Malformed Impact input must reject"); } catch (AuthoringException) { Check(true, "Untyped flags and unexposed path/easing reject."); } }
        var partial = Parameters(); partial.Impact!.ArcPath = null; partial.Impact.ArcOffset = null;
        var partialContent = new ContentInput { OwnerId = "A" }; partialContent.Definitions.Add(Make(partial));
        var partialRoundtrip = ContentCodec.ReadContent(ContentCodec.WriteContent(partialContent), "A", _ => Array.Empty<byte>());
        Check(partialRoundtrip.Definitions[0].NativeAttacks[0].Impact!.ArcPath == null && partialRoundtrip.Definitions[0].NativeAttacks[0].Impact!.ArcOffset == null,
            "Omitted/null path stays inherited instead of being coerced to Linear.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill01", "impact", fields.Select(f => new MonsterNativeAttackFieldInfo(f.Field,f.Value,f.Unit,f.ReadAt,true,false))) };
        Check(MonsterAdmission.Definition("A", frozen, catalog) == null && MonsterOverrideAdmission.Validate("A", native, catalog)?.Code == "NativeAttackFieldUnsupported", "Impact definition/native capabilities remain separate.");
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 3, (_, _, _) => null); registry.Refresh(true, 1);
        var lease = registry.Register("A", 1, native).Value!; zero.Impact.ArcOffset = 90; zero.Impact.ArcPath = true;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.impact.arcOffset", 3) == -100 && registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.impact.arcPath", 1) == 0,
            "Native registration freezes signed offset and explicit Linear independently.");
        var winner = new MonsterNativeOverride { LocalId = "arc", TargetId = template.Id };
        winner.NativeAttacks.Add(new MonsterNativeAttackParameters { AttackId = "AtkSkill01", Impact = new MonsterImpactParameters { ArcPath = true } });
        var top = registry.Register("B", 2, winner).Value!;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.impact.arcPath", 0) == 1 && registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.impact.arcOffset", 3) == -100,
            "A path-only winner preserves a lower author's control offset.");
        top.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.impact.arcPath", 1) == 0, "Leaf withdrawal reveals explicit lower Linear.");
        lease.Dispose(); Check(double.IsNaN(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.impact.arcPath", double.NaN)), "Final path withdrawal exposes absence for exact native enum inheritance.");
    }
}
