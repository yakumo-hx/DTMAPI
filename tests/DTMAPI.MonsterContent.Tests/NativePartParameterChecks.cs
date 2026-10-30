using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void NativePartParameterChecks()
    {
        MonsterNativeAttackParameters Make() => new MonsterNativeAttackParameters { AttackId = "AtkNormal", CooldownGameSeconds = 3,
            StartOnCooldown = false, Damage = 7, CriticalRate = .2f, RandomShoot = new MonsterRandomShootParameters {
                MinBulletCount = 2, MaxBulletCount = 4, ShotIntervalGameSeconds = .2f, ProjectileSpeed = 5, ProjectileLifetimeGameSeconds = 1 } };
        MonsterDefinition Define() { var d = Definition(); d.NativeParts.Add(new MonsterNativePartParameters {
            TemplateId = "nian_tail", NativeAttacks = { Make() } }); return d; }
        var original = AuthorCatalog(); var root = original.Templates[0];
        MonsterNativeAttackInfo Slot(bool definition, bool native) => new MonsterNativeAttackInfo("AtkNormal", "random-shoot",
            NativeAttackValidation.Fields(Make()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", definition, native)));
        root.Actions.NativeParts = new[] { new MonsterNativePartInfo("nian_tail", new[] { Slot(true, true) }) };
        var part = new MonsterTemplate("nian_tail", 1, false, false, "fixture", "fixture", 100, 0, "fixture",
            new[] { new MonsterDiagnostic("EncounterPart", "Only created by the complete encounter.", field: "decorator.parent") }) {
            NativeOverrideFields = new[] { "nativeAttacks" }, Actions = new MonsterActionCapabilities(new[] { "AtkNormal" }, false) {
                NativeAttacks = new[] { Slot(false, true) } } };
        var catalog = new MonsterContentCatalog(1, "fixture", "0.1.49", "parts", new[] { root, part }, original.Items, original.DropPools, original.Rooms);
        var input = Define(); var authorPart = input.NativeParts[0]; var authorAttack = authorPart.NativeAttacks[0];
        var frozen = new DefinitionRecord("A", input, 1).Definition;
        authorAttack.RandomShoot!.MinBulletCount = 64; authorPart.TemplateId = "nian_head"; authorPart.NativeAttacks.Clear(); input.NativeParts.Clear();
        Check(frozen.NativeParts[0].TemplateId == "nian_tail" && frozen.NativeParts[0].NativeAttacks[0].RandomShoot!.MinBulletCount == 2,
            "Part identity, list and typed parameters freeze before any native publication.");
        Check(MonsterAdmission.Definition("A", frozen, catalog) == null && root.EditableFields.Contains("nativeParts") && !part.EditableFields.Contains("nativeParts"),
            "Root advertises nested editing without enabling standalone part creation.");
        var standalone = Define(); standalone.TemplateId = "nian_tail";
        Check(MonsterAdmission.Definition("A", standalone, catalog)?.Code == "EncounterPart", "Nested capability cannot erase standalone EncounterPart rejection.");
        var native = new MonsterNativeOverride { LocalId = "tail", TargetId = "nian_tail", NativeAttacks = { Make() } };
        Check(MonsterOverrideAdmission.Validate("A", native, catalog) == null, "Original part override remains separate from standalone definition admission.");
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content), catalogText = CatalogCodec.Write(catalog);
        Check(ContentCodec.WriteContent(ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>())) == payload &&
            CatalogCodec.Write(CatalogCodec.Read(catalogText)) == catalogText && ContentCompiler.Validate(content, CatalogCodec.Read(catalogText)).Count == 0,
            "Compiler, payload and catalog preserve nested definition plus original-part override domains.");
        foreach (var change in new Action<MonsterDefinition>[] {
            d => d.NativeParts = null!, d => d.NativeParts.Add(null!), d => d.NativeParts[0].TemplateId = "",
            d => d.NativeParts.Add(d.NativeParts[0]), d => d.NativeParts[0].NativeAttacks = null!, d => d.NativeParts[0].NativeAttacks.Clear() })
        { var d = Define(); change(d); Check(MonsterValidation.Validate("A", d)?.Code == "InvalidNativePart", "Malformed/duplicate parts reject before Copy/publication."); }
        var missing = Define(); missing.NativeParts[0].TemplateId = "other";
        Check(MonsterAdmission.Definition("A", missing, catalog)?.Field == "nativeParts[0].templateId", "Part must belong to the selected root capability.");
        foreach (var change in new Action<MonsterRandomShootParameters>[] { p => p.MinBulletCount = 0, p => p.ShotIntervalGameSeconds = float.NaN, p => p.ProjectileLifetimeGameSeconds = -1 })
        { var d = Define(); change(d.NativeParts[0].NativeAttacks[0].RandomShoot!); var error = MonsterValidation.Validate("A", d);
            Check(error?.Code == "InvalidNativeAttack" && error.Field.StartsWith("nativeParts[0].nativeAttacks[0].randomShoot.", StringComparison.Ordinal), "Nested numeric diagnostic identifies the actual field."); }
        foreach (var parameters in new[] { new MonsterRandomShootParameters { MinBulletCount = 64, MaxBulletCount = 1, ShotIntervalGameSeconds = 0 },
            new MonsterRandomShootParameters { ProjectileSpeed = 0, ProjectileLifetimeGameSeconds = 0 }, new MonsterRandomShootParameters { MaxBulletCount = 64 } })
        { var d = Define(); d.NativeParts[0].NativeAttacks[0].RandomShoot = parameters;
            Check(MonsterAdmission.Definition("A", d, catalog) == null, "Independent endpoints, reversed range and native zero interval/speed/lifetime remain expressible."); }
        foreach (var change in new Action<MonsterNativeAttackParameters>[] { a => a.WindupGameSeconds = 1,
            a => a.Shoot = new MonsterShootParameters { WindupEnabled = true }, a => a.AttackId = "AtkSkill01" })
        { var d = Define(); change(d.NativeParts[0].NativeAttacks[0]); var error = MonsterAdmission.Definition("A", d, catalog);
            Check(error != null && error.Field.StartsWith("nativeParts[0].nativeAttacks[0]", StringComparison.Ordinal), "No fake windup, foreign group or absent part slot is admitted."); }
        var partOnly = new ContentInput { OwnerId = "A" }; partOnly.Definitions.Add(frozen);
        string partPayload = ContentCodec.WriteContent(partOnly);
        foreach (string malformed in new[] { partPayload.Replace("\"minBulletCount\":2", "\"minBulletCount\":2.5"),
            partPayload.Replace("\"projectileSpeed\":5", "\"projectileSpeed\":\"bad\""),
            partPayload.Replace("\"maxBulletCount\":4", "\"windupEnabled\":false") })
        { try { ContentCodec.ReadContent(malformed, "A", _ => Array.Empty<byte>()); Check(false, "Malformed nested parameters must reject."); }
          catch (AuthoringException e) { Check(e.Diagnostic.Field.StartsWith("nativeParts[0].nativeAttacks[0].randomShoot", StringComparison.Ordinal), "Nested parse error retains part and slot indices: " + e.Diagnostic.Field); } }
        root.Actions.NativeParts = Array.Empty<MonsterNativePartInfo>();
        Check(MonsterAdmission.Definition("A", frozen, catalog)?.Code == "NativePartUnsupported", "Old or incompatible catalog preserves input and rejects unsupported part edits.");
    }
}
