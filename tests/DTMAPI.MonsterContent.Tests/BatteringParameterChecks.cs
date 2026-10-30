using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void BatteringParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkNormal", CooldownGameSeconds = 3,
            StartOnCooldown = false, Damage = 19, CriticalRate = 0, WindupGameSeconds = .4f,
            Battering = new MonsterBatteringParameters { WindupEnabled = false, AttackDistance = 20, TravelSpeed = 4 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var input = Parameters(); var frozen = new DefinitionRecord("A", Make(input), 1).Definition;
        input.Battering!.AttackDistance = 80; input.Battering.TravelSpeed = 30;
        Check(frozen.NativeAttacks[0].Battering!.AttackDistance == 20 && frozen.NativeAttacks[0].Battering!.TravelSpeed == 4, "Battering freezes nested range and travel speed.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "battering", fields) };
        Check(fields.Length == 8 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Eight independently advertised Battering fields admit.");
        foreach (var edit in new Action<MonsterBatteringParameters>[] { p => p.AttackDistance = -1, p => p.AttackDistance = float.NaN, p => p.TravelSpeed = 0, p => p.TravelSpeed = -.1f, p => p.TravelSpeed = float.PositiveInfinity })
        { var row = Parameters(); edit(row.Battering!); Check(MonsterValidation.Validate("A", Make(row))?.Code == "InvalidNativeAttack", "Invalid Battering range/speed rejects before native movement."); }
        var empty = Parameters(); empty.Battering = new MonsterBatteringParameters();
        Check(MonsterValidation.Validate("A", Make(empty))?.Code == "InvalidNativeAttack", "Empty Battering object rejects even with common fields.");
        var zero = Parameters(); zero.Battering!.AttackDistance = 0; zero.Battering.TravelSpeed = .1f;
        Check(MonsterAdmission.Definition("A", Make(zero), catalog) == null, "Zero activation distance and positive minimum speed remain explicit.");
        var native = new MonsterNativeOverride { LocalId = "body", TargetId = template.Id }; native.NativeAttacks.Add(zero);
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content); var reopened = ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0,
            "Battering new/native author input roundtrips and compiles against exact catalog.");
        foreach (string bad in new[] { payload.Replace("\"windupEnabled\":false", "\"windupEnabled\":0"),
            payload.Replace("\"attackDistance\":20", "\"travelDistance\":20"), payload.Replace("\"travelSpeed\":4", "\"projectileSpeed\":4") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Malformed Battering input must reject"); } catch (AuthoringException) { Check(true, "Typed boolean and invented body fields reject."); } }
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "battering", fields.Select(f => new MonsterNativeAttackFieldInfo(f.Field,f.Value,f.Unit,f.ReadAt,true,false))) };
        Check(MonsterAdmission.Definition("A", frozen, catalog) == null && MonsterOverrideAdmission.Validate("A", native, catalog)?.Code == "NativeAttackFieldUnsupported",
            "Definition and native Battering capabilities remain independent.");
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 3, (_, _, _) => null); registry.Refresh(true, 1);
        var lease = registry.Register("A", 1, native).Value!; zero.Battering.AttackDistance = 90; zero.Battering.WindupEnabled = true;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.battering.attackDistance", 3) == 0 && registry.Resolve(template.Id, "nativeAttacks.AtkNormal.battering.windupEnabled", 1) == 0,
            "Native Battering registration freezes zero range and false windup.");
        lease.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.battering.attackDistance", 3) == 3, "Battering final withdrawal restores baseline resolution.");
    }
}
