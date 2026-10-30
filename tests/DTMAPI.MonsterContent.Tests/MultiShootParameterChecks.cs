using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void MultiShootParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkNormal", CooldownGameSeconds = 3,
            StartOnCooldown = false, Damage = 19, CriticalRate = 0, WindupGameSeconds = .4f,
            MultiShoot = new MonsterMultiShootParameters { WindupEnabled = false, ShotGameSeconds = .25f, MinShotCount = 2, MaxShotCount = 4,
                FixedDirection = true, ProjectileSpeed = 7, ProjectileLifetimeGameSeconds = 3 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var input = Parameters(); var frozen = new DefinitionRecord("A", Make(input), 1).Definition;
        input.MultiShoot!.MinShotCount = 63; input.MultiShoot.FixedDirection = false;
        Check(frozen.NativeAttacks[0].MultiShoot!.MinShotCount == 2 && frozen.NativeAttacks[0].MultiShoot!.FixedDirection == true, "MultiShoot definitions freeze nested count/direction input.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "multi-shoot", fields) };
        Check(fields.Length == 12 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Twelve independent MultiShoot capabilities admit in the definition domain.");
        foreach (var edit in new Action<MonsterMultiShootParameters>[] { p => p.MinShotCount = 0, p => p.MaxShotCount = -1,
            p => p.ShotGameSeconds = float.NaN, p => p.ProjectileSpeed = -1,
            p => p.ProjectileLifetimeGameSeconds = float.PositiveInfinity })
        { var row = Parameters(); edit(row.MultiShoot!); Check(MonsterValidation.Validate("A", Make(row))?.Code == "InvalidNativeAttack", "Invalid MultiShoot bound rejects before native execution."); }
        var empty = Parameters(); empty.MultiShoot = new MonsterMultiShootParameters();
        Check(MonsterValidation.Validate("A", Make(empty))?.Code == "InvalidNativeAttack", "Empty MultiShoot intent rejects even with common fields.");
        var inverted = Parameters(); inverted.MultiShoot!.MinShotCount = 5; inverted.MultiShoot.MaxShotCount = 2;
        Check(MonsterAdmission.Definition("A", Make(inverted), catalog) == null, "Native max <= min fixed-minimum semantics remain expressible.");
        var zero = Parameters(); zero.MultiShoot!.ShotGameSeconds = 0; zero.MultiShoot.ProjectileSpeed = 0; zero.MultiShoot.ProjectileLifetimeGameSeconds = 0;
        Check(MonsterAdmission.Definition("A", Make(zero), catalog) == null, "Zero timing/projectile values and false windup remain explicit.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "multi-shoot", fields.Where(f => f.Field != "multiShoot.projectileSpeed")) };
        Check(MonsterAdmission.Definition("A", frozen, catalog)?.Code == "NativeAttackFieldUnsupported", "Unproven MultiShoot mover speed rejects per field.");
        var inherited = Parameters(); inherited.MultiShoot!.ProjectileSpeed = null;
        Check(MonsterAdmission.Definition("A", Make(inherited), catalog) == null, "Other MultiShoot fields do not require a speed edit.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "multi-shoot", fields) };
        var native = new MonsterNativeOverride { LocalId = "volley", TargetId = template.Id }; native.NativeAttacks.Add(zero);
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content); var reopened = ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0,
            "MultiShoot definitions/overrides survive actual codec, capability catalog and Compiler roundtrip.");
        foreach (string bad in new[] { payload.Replace("\"fixedDirection\":true", "\"fixedDirection\":1"),
            payload.Replace("\"minShotCount\":2", "\"minShotCount\":2.5"), payload.Replace("\"shotGameSeconds\":0", "\"unsupportedInterval\":0") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Malformed MultiShoot input must reject"); } catch (AuthoringException) { Check(true, "Typed bool/integer and unknown MultiShoot fields reject."); } }
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 3, (_, _, _) => null); registry.Refresh(true, 1);
        var lease = registry.Register("A", 1, native).Value!; zero.MultiShoot.MinShotCount = 60; zero.MultiShoot.WindupEnabled = true;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.multiShoot.minShotCount", 3) == 2 && registry.Resolve(template.Id, "nativeAttacks.AtkNormal.multiShoot.windupEnabled", 1) == 0,
            "Native MultiShoot registration freezes both numeric and false leaves.");
        lease.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.multiShoot.minShotCount", 3) == 3, "MultiShoot final withdrawal resolves current baseline.");
    }
}
