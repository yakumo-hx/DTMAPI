using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void ChomperBiteParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkNormal", CooldownGameSeconds = 3,
            StartOnCooldown = false, Damage = 19, CriticalRate = 0, WindupGameSeconds = .4f,
            ChomperBite = new MonsterChomperBiteParameters { WindupEnabled = false, Range = 6 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var input = Parameters(); var frozen = new DefinitionRecord("A", Make(input), 1).Definition;
        input.ChomperBite!.Range = 80; input.ChomperBite.WindupEnabled = true;
        Check(frozen.NativeAttacks[0].ChomperBite!.Range == 6 && frozen.NativeAttacks[0].ChomperBite!.WindupEnabled == false, "Bite registration freezes nested range and false flag.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "chomper-bite", fields) };
        Check(fields.Length == 7 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Seven independently advertised Bite fields admit.");
        foreach (float range in new[] { -1f, float.NaN, float.PositiveInfinity })
        { var row = Parameters(); row.ChomperBite!.Range = range; Check(MonsterValidation.Validate("A", Make(row))?.Code == "InvalidNativeAttack", "Invalid target range rejects before native allocation."); }
        var empty = Parameters(); empty.ChomperBite = new MonsterChomperBiteParameters();
        Check(MonsterValidation.Validate("A", Make(empty))?.Code == "InvalidNativeAttack", "Empty specialized object rejects even with common fields.");
        var zero = Parameters(); zero.ChomperBite!.Range = 0; zero.WindupGameSeconds = 0;
        Check(MonsterAdmission.Definition("A", Make(zero), catalog) == null, "Zero windup and target range remain explicit.");
        var native = new MonsterNativeOverride { LocalId = "bite", TargetId = template.Id }; native.NativeAttacks.Add(zero);
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content); var reopened = ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0,
            "New/native Bite inputs roundtrip through compiler and exact catalog.");
        foreach (string bad in new[] { payload.Replace("\"windupEnabled\":false", "\"windupEnabled\":0"),
            payload.Replace("\"range\":6", "\"animName\":6"), payload.Replace("\"range\":6", "\"damageBoxSize\":6") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Malformed Bite input must reject"); } catch (AuthoringException) { Check(true, "Typed flag, clip substitution and invented damage box field reject."); } }
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "chomper-bite", fields.Select(f => new MonsterNativeAttackFieldInfo(f.Field,f.Value,f.Unit,f.ReadAt,true,false))) };
        Check(MonsterAdmission.Definition("A", frozen, catalog) == null && MonsterOverrideAdmission.Validate("A", native, catalog)?.Code == "NativeAttackFieldUnsupported", "Definition and native author domains remain independent.");
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 3, (_, _, _) => null); registry.Refresh(true, 1);
        var lease = registry.Register("A", 1, native).Value!; zero.ChomperBite.Range = 90; zero.ChomperBite.WindupEnabled = true;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.chomperBite.range", 3) == 0 && registry.Resolve(template.Id, "nativeAttacks.AtkNormal.chomperBite.windupEnabled", 1) == 0,
            "Native registration freezes zero range and false windup.");
        lease.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.chomperBite.range", 3) == 3, "Final withdrawal reveals baseline.");
    }
}
