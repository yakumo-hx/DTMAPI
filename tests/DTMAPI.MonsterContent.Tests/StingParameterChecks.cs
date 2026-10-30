using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void StingParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkSkill01", CooldownGameSeconds = 3,
            StartOnCooldown = false, Damage = 19, CriticalRate = 0, WindupGameSeconds = .4f,
            Sting = new MonsterStingParameters { WindupEnabled = false, Distance = 6, TravelSpeed = 4 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var input = Parameters(); var frozen = new DefinitionRecord("A", Make(input), 1).Definition;
        input.Sting!.Distance = 80; input.Sting.TravelSpeed = 30;
        Check(frozen.NativeAttacks[0].Sting!.Distance == 6 && frozen.NativeAttacks[0].Sting!.TravelSpeed == 4, "Sting registration freezes nested movement values.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill01", "sting", fields) };
        Check(fields.Length == 8 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Eight independently advertised Sting fields admit.");
        foreach (var edit in new Action<MonsterStingParameters>[] { p => p.Distance = -1, p => p.Distance = float.NaN, p => p.TravelSpeed = 0, p => p.TravelSpeed = float.PositiveInfinity })
        { var row = Parameters(); edit(row.Sting!); Check(MonsterValidation.Validate("A", Make(row))?.Code == "InvalidNativeAttack", "Invalid displacement or nominal speed rejects before native allocation."); }
        var empty = Parameters(); empty.Sting = new MonsterStingParameters();
        Check(MonsterValidation.Validate("A", Make(empty))?.Code == "InvalidNativeAttack", "Empty specialized object rejects even with common fields.");
        var zero = Parameters(); zero.Sting!.Distance = 0; zero.Sting.TravelSpeed = .1f; zero.WindupGameSeconds = 0;
        Check(MonsterAdmission.Definition("A", Make(zero), catalog) == null, "Zero windup and displacement remain explicit; speed stays positive.");
        var native = new MonsterNativeOverride { LocalId = "sting", TargetId = template.Id }; native.NativeAttacks.Add(zero);
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content); var reopened = ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0,
            "New/native Sting inputs roundtrip through compiler and exact catalog.");
        foreach (string bad in new[] { payload.Replace("\"windupEnabled\":false", "\"windupEnabled\":0"),
            payload.Replace("\"distance\":6", "\"ease\":6"), payload.Replace("\"distance\":6", "\"hasAfterImage\":6") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Malformed Sting input must reject"); } catch (AuthoringException) { Check(true, "Typed flag, overwritten ease and unused afterimage reject."); } }
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill01", "sting", fields.Select(f => new MonsterNativeAttackFieldInfo(f.Field,f.Value,f.Unit,f.ReadAt,true,false))) };
        Check(MonsterAdmission.Definition("A", frozen, catalog) == null && MonsterOverrideAdmission.Validate("A", native, catalog)?.Code == "NativeAttackFieldUnsupported", "Definition and native author domains are independent.");
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 3, (_, _, _) => null); registry.Refresh(true, 1);
        var lease = registry.Register("A", 1, native).Value!; zero.Sting.Distance = 90; zero.Sting.WindupEnabled = true;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.sting.distance", 3) == 0 && registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.sting.windupEnabled", 1) == 0,
            "Native registration freezes zero displacement and false windup.");
        lease.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.sting.distance", 3) == 3, "Final withdrawal reveals baseline.");
    }
}
