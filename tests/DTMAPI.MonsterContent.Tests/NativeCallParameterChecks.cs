using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void NativeCallParameterChecks()
    {
        MonsterNativeAttackParameters Make() => new MonsterNativeAttackParameters { AttackId = "AtkSkill01", CooldownGameSeconds = 4, StartOnCooldown = false,
            WindupGameSeconds = .6f, Call = new MonsterCallParameters { MinCount = 2, MaxCount = 3, RoomLimit = 6 } };
        MonsterDefinition Define(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill01", "call", NativeAttackValidation.Fields(Make()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true))) };
        var input = Make(); var definition = Define(input); var frozen = new DefinitionRecord("A", definition, 1).Definition;
        input.Call!.MinCount = 30; definition.NativeAttacks.Clear();
        Check(frozen.NativeAttacks.Single().Call!.MinCount == 2 && frozen.NativeAttacks.Single().StartOnCooldown == false, "Call author list and nested DTO freeze before publication.");
        Check(template.Actions.NativeAttacks.Single().Fields.Count == 6 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Exactly six consumed Call fields admitted.");
        foreach (string field in new[] { "damage", "critical" }) { var p = Make(); if (field == "damage") p.Damage = 1; else p.CriticalRate = 1;
            Check(MonsterAdmission.Definition("A", Define(p), catalog)?.Code == "NativeAttackFieldUnsupported", "Call has no damage or critical parameter."); }
        var wrong = Make(); wrong.Landmine = new MonsterLandmineParameters { Range = 1 };
        Check(MonsterAdmission.Definition("A", Define(wrong), catalog)?.Code == "NativeAttackFieldUnsupported", "Call rejects fields belonging to another native kind.");
        var native = new MonsterNativeOverride { LocalId = "call", TargetId = template.Id }; native.NativeAttacks.Add(Make());
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content);
        Check(ContentCodec.WriteContent(ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>())) == payload && ContentCompiler.Validate(content, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0, "Both domains roundtrip through current Compiler/catalog.");
        foreach (string malformed in new[] { payload.Replace("\"minCount\":2", "\"minCount\":2.5"), payload.Replace("\"roomLimit\":6", "\"roomLimit\":\"6\""), payload.Replace("\"call\":", "\"nativeCalls\":"), payload.Replace("\"maxCount\":3", "\"monsterId\":\"bee\""), payload.Replace("\"maxCount\":3", "\"windupEnabled\":false") })
        { try { ContentCodec.ReadContent(malformed, "A", _ => Array.Empty<byte>()); Check(false, "Malformed Call must reject"); } catch (AuthoringException) { Check(true, "Call rejects fractional counts, strings, retired paths and unsupported target/ready flag."); } }
        foreach (var change in new Action<MonsterCallParameters>[] { c => c.MinCount = 0, c => c.MaxCount = 0, c => c.RoomLimit = -1 })
        { var p = Make(); change(p.Call!); Check(MonsterValidation.Validate("A", Define(p))?.Code == "InvalidNativeAttack", "Call parameter out of bounds rejects before allocation."); }
        foreach (var c in new[] { new MonsterCallParameters { MinCount = 32 }, new MonsterCallParameters { MaxCount = 1 }, new MonsterCallParameters { MinCount = 32, MaxCount = 1, RoomLimit = 64 }, new MonsterCallParameters { MinCount = 1, MaxCount = 32, RoomLimit = 0 } })
        { var p = Make(); p.Call = c; p.WindupGameSeconds = 0; Check(MonsterAdmission.Definition("A", Define(p), catalog) == null, "Independent leaf inheritance, reversed endpoints and explicit zero cap are valid."); }
        var empty = Make(); empty.Call = new MonsterCallParameters(); Check(MonsterValidation.Validate("A", Define(empty))?.Code == "InvalidNativeAttack", "Empty Call group is not inheritance.");
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 9, (_, _, _) => null); registry.Refresh(true, 1);
        var lower = registry.Register("A", 1, native).Value!; var top = new MonsterNativeOverride { LocalId = "top", TargetId = template.Id };
        var leaf = new MonsterNativeAttackParameters { AttackId = "AtkSkill01", Call = new MonsterCallParameters { MaxCount = 1, RoomLimit = 0 } }; top.NativeAttacks.Add(leaf);
        var upper = registry.Register("B", 2, top).Value!; leaf.Call.RoomLimit = 64;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.call.roomLimit", 9) == 0 && registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.call.minCount", 9) == 2 && registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.call.maxCount", 9) == 1, "Call winner resolves each frozen leaf including zero and reversed composed endpoints.");
        upper.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.call.roomLimit", 9) == 6, "Call leaf withdrawal reveals lower author.");
        lower.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.call.roomLimit", 9) == 9, "Final Call withdrawal resolves live baseline.");
        template.Actions.NativeAttacks = Array.Empty<MonsterNativeAttackInfo>();
        Check(MonsterAdmission.Definition("A", frozen, catalog)?.Code == "NativeAttackUnsupported", "Missing capability never silently accepts retired Call entry.");
    }
}
