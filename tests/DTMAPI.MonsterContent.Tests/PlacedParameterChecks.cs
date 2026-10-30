using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void PlacedParameterChecks()
    {
        MonsterNativeAttackParameters Make(bool mine) => new MonsterNativeAttackParameters { AttackId = mine ? "AtkNormal" : "AtkSkill01",
            CooldownGameSeconds = 4, StartOnCooldown = false, Damage = 21, CriticalRate = 1,
            Landmine = mine ? new MonsterLandmineParameters { Range = 7, RecoilGameSeconds = 1.2f, ThrowImpulse = 3, LiveGameSeconds = .4f, ReactionGameSeconds = .9f } : null,
            TimeBomb = mine ? null : new MonsterTimeBombParameters { Range = 9, RecoilGameSeconds = .8f, ThrowImpulse = 2, MaxThrowImpulse = 8, Count = 3 } };
        MonsterDefinition Define(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        template.Actions.NativeAttacks = new[] { true, false }.Select(mine => new MonsterNativeAttackInfo(Make(mine).AttackId, mine ? "landmine" : "time-bomb",
            NativeAttackValidation.Fields(Make(mine)).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)))).ToArray();
        foreach (bool mine in new[] { true, false })
        {
            var input = Make(mine); var frozen = new DefinitionRecord("A", Define(input), 1).Definition;
            if (mine) input.Landmine!.ThrowImpulse = 90; else input.TimeBomb!.Count = 30;
            Check(mine ? frozen.NativeAttacks[0].Landmine!.ThrowImpulse == 3 : frozen.NativeAttacks[0].TimeBomb!.Count == 3, "Nested placed configuration freezes independently.");
            Check(template.Actions.NativeAttacks.Single(s => s.AttackId == input.AttackId).Fields.Count == 9 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Nine actually consumed placed fields admitted.");
            var wrong = Make(mine); wrong.WindupGameSeconds = 0;
            Check(MonsterAdmission.Definition("A", Define(wrong), catalog)?.Code == "NativeAttackFieldUnsupported", "Unconsumed windup rejected rather than relabeled recoil.");
            wrong = Make(mine); if (mine) wrong.TimeBomb = new MonsterTimeBombParameters { Count = 1 }; else wrong.Landmine = new MonsterLandmineParameters { LiveGameSeconds = 1 };
            Check(MonsterAdmission.Definition("A", Define(wrong), catalog)?.Code == "NativeAttackFieldUnsupported", "Kind-specific fields reject on the other slot.");
            var native = new MonsterNativeOverride { LocalId = mine ? "mine" : "bomb", TargetId = template.Id }; native.NativeAttacks.Add(Make(mine));
            var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
            string payload = ContentCodec.WriteContent(content);
            Check(ContentCodec.WriteContent(ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>())) == payload && ContentCompiler.Validate(content, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0, "Placed payload and exact catalog roundtrip in both domains.");
            foreach (string invalid in new[] { payload.Replace("\"recoilGameSeconds\":", "\"windupGameSeconds\":"), payload.Replace("\"throwImpulse\":3", "\"throwImpulse\":\"3\"").Replace("\"throwImpulse\":2", "\"throwImpulse\":\"2\""), payload.Replace("\"landmine\":", "\"obsoleteLandmine\":").Replace("\"timeBomb\":", "\"obsoleteTimeBomb\":") })
            { try { ContentCodec.ReadContent(invalid, "A", _ => Array.Empty<byte>()); Check(false, "Malformed placed parameter must reject"); } catch (AuthoringException) { Check(true, "Strict typed groups reject unsupported keys and numeric strings."); } }
            var registry = new NativeOverrideRegistry(() => { }, (_, _) => 8, (_, _, _) => null); registry.Refresh(true, 1);
            var lower = registry.Register("A", 1, native).Value!;
            var top = new MonsterNativeOverride { LocalId = "top", TargetId = template.Id }; var leaf = new MonsterNativeAttackParameters { AttackId = input.AttackId, Damage = 0 }; top.NativeAttacks.Add(leaf);
            var upper = registry.Register("B", 2, top).Value!; leaf.Damage = 100;
            Check(registry.Resolve(template.Id, "nativeAttacks." + input.AttackId + ".damage", 8) == 0, "Placed leaf arbitration freezes explicit zero independently of other fields.");
            upper.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks." + input.AttackId + ".damage", 8) == 21, "Placed leaf withdrawal reveals lower declaration.");
            lower.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks." + input.AttackId + ".damage", 8) == 8, "Final placed withdrawal resolves current baseline.");
        }
        foreach (var change in new Action<MonsterLandmineParameters>[] { p => p.Range = -1, p => p.LiveGameSeconds = float.NaN, p => p.ThrowImpulse = float.PositiveInfinity })
        { var p = Make(true); change(p.Landmine!); Check(MonsterValidation.Validate("A", Define(p))?.Code == "InvalidNativeAttack", "Landmine bound rejects before allocation."); }
        foreach (var change in new Action<MonsterTimeBombParameters>[] { p => p.Count = -1, p => p.RecoilGameSeconds = -1, p => p.ThrowImpulse = -1, p => p.MaxThrowImpulse = float.NaN })
        { var p = Make(false); change(p.TimeBomb!); Check(MonsterValidation.Validate("A", Define(p))?.Code == "InvalidNativeAttack", "TimeBomb bound rejects before allocation."); }
        foreach (float bound in new[] { 0f, 1f })
        {
            var p = Make(true); p.Landmine = new MonsterLandmineParameters { Range = 100 * bound, RecoilGameSeconds = 10 * bound, ThrowImpulse = 100 * bound, LiveGameSeconds = 300 * bound, ReactionGameSeconds = 30 * bound };
            Check(MonsterAdmission.Definition("A", Define(p), catalog) == null, "Landmine zero and upper bounds are explicit values.");
            p = Make(false); p.TimeBomb = new MonsterTimeBombParameters { Range = 100 * bound, RecoilGameSeconds = 10 * bound, ThrowImpulse = 100 * bound, MaxThrowImpulse = 0, Count = (int)(32 * bound) };
            Check(MonsterAdmission.Definition("A", Define(p), catalog) == null, "TimeBomb permits zero and descending nonnegative impulse endpoints.");
        }
        var empty = Make(true); empty.Landmine = new MonsterLandmineParameters(); Check(MonsterValidation.Validate("A", Define(empty))?.Code == "InvalidNativeAttack", "Empty specialized object is not inheritance.");
        var bombContent = new ContentInput { OwnerId = "A" }; bombContent.Definitions.Add(Define(Make(false))); string bombPayload = ContentCodec.WriteContent(bombContent);
        try { ContentCodec.ReadContent(bombPayload.Replace("\"count\":3", "\"count\":3.5"), "A", _ => Array.Empty<byte>()); Check(false, "Fractional bomb count must reject"); } catch (AuthoringException) { Check(true, "Bomb count requires integer."); }
        foreach (string invalid in new[] { bombPayload.Replace("\"nativeAttacks\":", "\"nativeSkills\":"), bombPayload.Replace("\"timeBomb\":", "\"skillId\":") })
        { try { ContentCodec.ReadContent(invalid, "A", _ => Array.Empty<byte>()); Check(false, "Retired internal path must reject"); } catch (AuthoringException) { Check(true, "Old nativeSkills and unsupported resource path reject without migration."); } }
    }
}
