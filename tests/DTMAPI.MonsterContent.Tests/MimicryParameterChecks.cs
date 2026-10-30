using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void MimicryParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkSkill01", CooldownGameSeconds = 3,
            StartOnCooldown = false, ChomperMimicry = new MonsterChomperMimicryParameters { MinDecoyGameSeconds = 6, MaxDecoyGameSeconds = 9 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var input = Parameters(); var frozen = new DefinitionRecord("A", Make(input), 1).Definition;
        input.ChomperMimicry!.MinDecoyGameSeconds = 80; input.ChomperMimicry.MaxDecoyGameSeconds = 100;
        Check(frozen.NativeAttacks[0].ChomperMimicry!.MinDecoyGameSeconds == 6 && frozen.NativeAttacks[0].ChomperMimicry!.MaxDecoyGameSeconds == 9, "Mimicry registration freezes both decoy duration endpoints.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill01", "chomper-mimicry", fields) };
        Check(fields.Length == 4 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Exactly four consumed Mimicry fields are advertised.");
        foreach (var edit in new Action<MonsterChomperMimicryParameters>[] { p => p.MinDecoyGameSeconds = -1, p => p.MaxDecoyGameSeconds = -1,
            p => p.MinDecoyGameSeconds = float.NaN, p => p.MaxDecoyGameSeconds = float.PositiveInfinity })
        { var row = Parameters(); edit(row.ChomperMimicry!); Check(MonsterValidation.Validate("A", Make(row))?.Code == "InvalidNativeAttack", "Invalid decoy duration rejects before native allocation."); }
        var empty = Parameters(); empty.ChomperMimicry = new MonsterChomperMimicryParameters();
        Check(MonsterValidation.Validate("A", Make(empty))?.Code == "InvalidNativeAttack", "Empty Mimicry object rejects even with common cooldown.");
        foreach (var edit in new Action<MonsterNativeAttackParameters>[] { p => p.Damage = 1, p => p.CriticalRate = 0, p => p.WindupGameSeconds = 0 })
        { var row = Parameters(); edit(row); var ov = new MonsterNativeOverride { LocalId = "unused", TargetId = template.Id }; ov.NativeAttacks.Add(row);
            Check(MonsterAdmission.Definition("A", Make(row), catalog)?.Code == "NativeAttackFieldUnsupported" && MonsterOverrideAdmission.Validate("A", ov, catalog)?.Code == "NativeAttackFieldUnsupported", "Unused damage/critical/windup reject in both domains."); }
        var zero = Parameters(); zero.ChomperMimicry!.MinDecoyGameSeconds = 0; zero.ChomperMimicry.MaxDecoyGameSeconds = 0;
        Check(MonsterAdmission.Definition("A", Make(zero), catalog) == null, "Zero lifetime is an explicit immediate-next-tick decoy.");
        var reversed = Parameters(); reversed.ChomperMimicry!.MinDecoyGameSeconds = 300; reversed.ChomperMimicry.MaxDecoyGameSeconds = 0;
        Check(MonsterAdmission.Definition("A", Make(reversed), catalog) == null, "Reversed float endpoints preserve native Range semantics.");
        var native = new MonsterNativeOverride { LocalId = "mimicry", TargetId = template.Id }; native.NativeAttacks.Add(zero);
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content); var reopened = ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0, "Mimicry compiler/catalog roundtrip preserves both domains.");
        foreach (string bad in new[] { payload.Replace("\"minDecoyGameSeconds\":6", "\"minDecoyGameSeconds\":\"6\""),
            payload.Replace("\"minDecoyGameSeconds\":6", "\"bodyWaitGameSeconds\":6"), payload.Replace("\"minDecoyGameSeconds\":6", "\"damage\":6") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Malformed Mimicry input must reject"); } catch (AuthoringException) { Check(true, "Strict float and unexposed fields reject."); } }
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill01", "chomper-mimicry", fields.Select(f => new MonsterNativeAttackFieldInfo(f.Field,f.Value,f.Unit,f.ReadAt,true,false))) };
        Check(MonsterAdmission.Definition("A", frozen, catalog) == null && MonsterOverrideAdmission.Validate("A", native, catalog)?.Code == "NativeAttackFieldUnsupported", "Mimicry author domains remain independently admitted.");
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 3, (_, _, _) => null); registry.Refresh(true, 1);
        var lease = registry.Register("A", 1, native).Value!; zero.ChomperMimicry.MinDecoyGameSeconds = 90;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.chomperMimicry.minDecoyGameSeconds", 3) == 0, "Native registration freezes explicit zero.");
        var winner = new MonsterNativeOverride { LocalId = "min", TargetId = template.Id }; winner.NativeAttacks.Add(new MonsterNativeAttackParameters { AttackId = "AtkSkill01", ChomperMimicry = new MonsterChomperMimicryParameters { MinDecoyGameSeconds = 7 } });
        var top = registry.Register("B", 2, winner).Value!;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.chomperMimicry.minDecoyGameSeconds", 3) == 7 && registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.chomperMimicry.maxDecoyGameSeconds", 3) == 0, "Single-endpoint override leaves the other endpoint under its own winner.");
        top.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.chomperMimicry.minDecoyGameSeconds", 3) == 0, "Leaf withdrawal reveals lower zero.");
        lease.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.chomperMimicry.minDecoyGameSeconds", 3) == 3, "Final withdrawal reveals raw baseline.");
    }
}
