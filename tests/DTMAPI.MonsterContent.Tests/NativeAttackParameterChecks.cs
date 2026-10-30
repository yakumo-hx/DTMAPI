using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void NativeAttackParameterChecks()
    {
        ShootParameterChecks();
        MultiShootParameterChecks();
        SalvoShootParameterChecks();
        ScatteringShootParameterChecks();
        DragonShootParameterChecks();
        ScatteringThrowParameterChecks();
        BombingDiveParameterChecks();
        BatteringBulletParameterChecks();
        BatteringParameterChecks();
        FungusParameterChecks();
        StingParameterChecks();
        ImpactParameterChecks();
        MultiImpactParameterChecks();
        BulletTrainParameterChecks();
        ShadowImpactParameterChecks();
        MimicryParameterChecks();
        ThunderParameterChecks();
        PlacedParameterChecks();
        ChomperBiteParameterChecks();
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkNormal", CooldownGameSeconds = 3, Damage = 20,
            StartOnCooldown = false, CriticalRate = .2f, WindupGameSeconds = .6f, AmoebaJump = new MonsterAmoebaJumpParameters { Range = 12, Speed = 6, DistanceCells = 4 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var original = Parameters(); var definition = Make(original); var record = new DefinitionRecord("A", definition, 1);
        original.Damage = 999; original.AmoebaJump!.Speed = 999; definition.NativeAttacks.Clear();
        Check(record.Definition.NativeAttacks[0].Damage == 20 && record.Definition.NativeAttacks[0].AmoebaJump!.Speed == 6, "Native attack nested values and list freeze before definition publication.");
        var catalog = AuthorCatalog(); definition = record.Definition;
        Check(MonsterAdmission.Definition("A", definition, catalog)?.Code == "NativeAttackUnsupported", "Missing native attack capabilities reject explicit intent.");
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        catalog.Templates[0].Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "amoeba-jump", fields, "No max height edit") };
        catalog.Templates[0].NativeOverrideFields = new[] { "nativeAttacks" };
        var decoded = CatalogCodec.Read(CatalogCodec.Write(catalog));
        Check(MonsterAdmission.Definition("A", definition, decoded) == null && decoded.Templates[0].Actions.NativeAttacks[0].Fields.Count == 8,
            "Catalog preserves defaults, units, timing and separate definition/override capabilities.");
        foreach (var change in new Action<MonsterNativeAttackParameters>[] { p => p.AttackId = "Unset", p => p.AttackId = "AtkNormal.bad",
            p => p.Damage = float.NaN, p => p.Damage = -1, p => p.CriticalRate = 1.1f, p => p.CooldownGameSeconds = float.PositiveInfinity,
            p => p.AmoebaJump!.Range = -1, p => p.AmoebaJump!.Speed = 0, p => p.AmoebaJump!.DistanceCells = 0 })
        { var p = Parameters(); change(p); Check(MonsterValidation.Validate("A", Make(p))?.Code == "InvalidNativeAttack", "Invalid attack values reject before native consumption."); }
        Check(MonsterValidation.Validate("A", Make(new MonsterNativeAttackParameters { AttackId = "AtkNormal" }))?.Code == "InvalidNativeAttack", "Empty native slots reject.");
        Check(MonsterValidation.Validate("A", Make(new MonsterNativeAttackParameters { AttackId = "AtkNormal", Damage = 1, AmoebaJump = new MonsterAmoebaJumpParameters() }))?.Code == "InvalidNativeAttack", "Empty specialized blocks do not hide an unsupported mechanism.");
        var duplicates = Make(Parameters()); duplicates.NativeAttacks.Add(Parameters());
        Check(MonsterValidation.Validate("A", duplicates)?.Field == "nativeAttacks[1].attackId", "Duplicate native slots reject.");
        foreach (var p in new[] {
            new MonsterNativeAttackParameters { AttackId = "AtkNormal", CooldownGameSeconds = 0, Damage = 0, CriticalRate = 0, WindupGameSeconds = 0, AmoebaJump = new MonsterAmoebaJumpParameters { Range = 0, Speed = .1f, DistanceCells = 1 } },
            new MonsterNativeAttackParameters { AttackId = "AtkNormal", CooldownGameSeconds = 300, Damage = 100000, CriticalRate = 1, WindupGameSeconds = 10, AmoebaJump = new MonsterAmoebaJumpParameters { Range = 100, Speed = 64, DistanceCells = 32 } } })
            Check(MonsterAdmission.Definition("A", Make(p), decoded) == null, "Zero and upper boundaries remain explicit valid values.");
        var native = new MonsterNativeOverride { LocalId = "attack", TargetId = "test-template", NativeAttacks = new List<MonsterNativeAttackParameters> { Parameters() } };
        Check(MonsterOverrideAdmission.Validate("A", native, decoded) == null, "Native override admission validates the selected field domain.");
        catalog.Templates[0].Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "amoeba-jump", fields.Select(f => new MonsterNativeAttackFieldInfo(f.Field, f.Value, f.Unit, f.ReadAt, true, false))) };
        Check(MonsterAdmission.Definition("A", definition, catalog) == null && MonsterOverrideAdmission.Validate("A", native, catalog)?.Code == "NativeAttackFieldUnsupported", "Definition support does not grant native override support.");
        catalog.Templates[0].Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "other", fields.Where(f => !f.Field.StartsWith("amoebaJump."))) };
        Check(MonsterAdmission.Definition("A", definition, catalog)?.Code == "NativeAttackFieldUnsupported", "Specialized parameters require the corresponding actual native mechanism.");

        double baseline = 8;
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => baseline, (_, _, _) => null); registry.Refresh(true, 1);
        var highInput = new MonsterNativeOverride { LocalId = "high", TargetId = "test-template", NativeAttacks = new List<MonsterNativeAttackParameters> { new MonsterNativeAttackParameters { AttackId = "AtkNormal", Damage = 30 } } };
        var high = registry.Register("Later", 2, highInput).Value!;
        var low = registry.Register("Earlier", 1, native).Value!;
        highInput.NativeAttacks[0].Damage = 90; native.NativeAttacks[0].AmoebaJump!.Speed = 50;
        string damage = "nativeAttacks.AtkNormal.damage", speed = "nativeAttacks.AtkNormal.amoebaJump.speed";
        Check(registry.Resolve("test-template", damage, 8) == 30 && registry.Resolve("test-template", speed, 8) == 6, "Fixed load order arbitrates leaf fields independently and freezes nested override values.");
        Check(registry.Resolve("test-template", "nativeAttacks.AtkNormal.startOnCooldown", 1) == 0, "False participates in native arbitration instead of being treated as missing.");
        Check(low.Snapshot.Fields.Single(f => f.Field == damage).State == MonsterOverrideFieldState.Shadowed && low.Snapshot.Fields.Single(f => f.Field == speed).State == MonsterOverrideFieldState.Effective,
            "Diagnostics identify exact leaf winner and shadowed sibling.");
        high.Dispose(); Check(registry.Resolve("test-template", damage, 8) == 20, "Withdrawal reveals the other author's effective leaf value.");
        baseline = 9; low.Dispose();
        Check(registry.Resolve("test-template", damage, baseline) == 9 && low.Snapshot.Fields.Single(f => f.Field == damage).Baseline == 9, "Final withdrawal reads the current native baseline instead of a stale captured value.");
        Check(registry.Resolve("dtm_mc_own", damage, 77) == 77, "Native override target identity cannot affect a distinct definition.");

        var input = new ContentInput { OwnerId = "A" }; input.Definitions.Add(definition);
        input.NativeOverrides.Add(new MonsterNativeOverride { LocalId = "attack", TargetId = "test-template", NativeAttacks = new List<MonsterNativeAttackParameters> { Parameters() } });
        string payload = ContentCodec.WriteContent(input); var reopened = ContentCodec.ReadContent(payload, "A", _ => throw new Exception("No images"));
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, decoded).Count == 0, "Handwritten payload preserves both domains through the same compiler.");
        foreach (string bad in new[] { payload.Replace("\"distanceCells\":4", "\"distanceCells\":4,\"maxJumpHeight\":8"), payload.Replace("\"distanceCells\":4", "\"distanceCells\":4.5") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Unknown and fractional cell fields must reject"); } catch (AuthoringException) { Check(true, "Unused native fields and fractional map distances reject explicitly."); } }
    }
}
