using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void ScatteringThrowParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkNormal", CooldownGameSeconds = 3,
            StartOnCooldown = false, Damage = 19, CriticalRate = 0, WindupGameSeconds = .4f,
            ScatteringThrow = new MonsterScatteringThrowParameters { WindupEnabled = false, MinBulletCount = 4, MaxBulletCount = 6, SectorAngleDegrees = 90,
                RecoveryGameSeconds = .25f, ProjectileSpeed = 7, ProjectileLifetimeGameSeconds = 3 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var input = Parameters(); var frozen = new DefinitionRecord("A", Make(input), 1).Definition;
        input.ScatteringThrow!.MinBulletCount = 63; input.ScatteringThrow.SectorAngleDegrees = 360;
        Check(frozen.NativeAttacks[0].ScatteringThrow!.MinBulletCount == 4 && frozen.NativeAttacks[0].ScatteringThrow!.SectorAngleDegrees == 90, "ScatteringThrow definitions freeze nested count and geometry.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "scattering-throw", fields) };
        Check(fields.Length == 12 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Twelve independent ScatteringThrow capabilities admit in the definition domain.");
        foreach (var edit in new Action<MonsterScatteringThrowParameters>[] { p => p.MinBulletCount = 0, p => p.SectorAngleDegrees = -1, p => p.SectorAngleDegrees = float.NaN,
            p => p.RecoveryGameSeconds = -.1f, p => p.ProjectileSpeed = -1,
            p => p.ProjectileLifetimeGameSeconds = float.PositiveInfinity })
        { var row = Parameters(); edit(row.ScatteringThrow!); Check(MonsterValidation.Validate("A", Make(row))?.Code == "InvalidNativeAttack", "Invalid ScatteringThrow bound rejects before native execution."); }
        var empty = Parameters(); empty.ScatteringThrow = new MonsterScatteringThrowParameters();
        Check(MonsterValidation.Validate("A", Make(empty))?.Code == "InvalidNativeAttack", "Empty ScatteringThrow intent rejects even with common fields.");
        var zero = Parameters(); zero.ScatteringThrow!.MinBulletCount = 1; zero.ScatteringThrow.MaxBulletCount = 1; zero.ScatteringThrow.SectorAngleDegrees = 0; zero.ScatteringThrow.RecoveryGameSeconds = 0;
        zero.ScatteringThrow.ProjectileSpeed = 0; zero.ScatteringThrow.ProjectileLifetimeGameSeconds = 0;
        Check(MonsterAdmission.Definition("A", Make(zero), catalog) == null, "Single bullet, zero angle/timing/projectile and false windup remain explicit.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "scattering-throw", fields.Where(f => f.Field != "scatteringThrow.projectileSpeed")) };
        Check(MonsterAdmission.Definition("A", frozen, catalog)?.Code == "NativeAttackFieldUnsupported", "Unproven ScatteringThrow mover speed rejects per field.");
        var inherited = Parameters(); inherited.ScatteringThrow!.ProjectileSpeed = null;
        Check(MonsterAdmission.Definition("A", Make(inherited), catalog) == null, "Other ScatteringThrow fields do not require a speed edit.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "scattering-throw", fields) };
        var native = new MonsterNativeOverride { LocalId = "volley", TargetId = template.Id }; native.NativeAttacks.Add(zero);
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content); var reopened = ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0,
            "ScatteringThrow definitions/overrides survive codec, capability catalog and Compiler roundtrip.");
        foreach (string bad in new[] { payload.Replace("\"windupEnabled\":false", "\"windupEnabled\":0"),
            payload.Replace("\"minBulletCount\":4", "\"minBulletCount\":2.5"), payload.Replace("\"sectorAngleDegrees\":90", "\"unsupportedAngle\":90") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Malformed ScatteringThrow input must reject"); } catch (AuthoringException) { Check(true, "Typed bool/integer and unknown ScatteringThrow fields reject."); } }
        var inverted = Parameters(); inverted.ScatteringThrow!.MinBulletCount = 5; inverted.ScatteringThrow.MaxBulletCount = 2;
        Check(MonsterAdmission.Definition("A", Make(inverted), catalog) == null, "Native reversed range explicitly selects its lower-bound field.");
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 3, (_, _, _) => null); registry.Refresh(true, 1);
        var lease = registry.Register("A", 1, native).Value!; zero.ScatteringThrow.MinBulletCount = 60; zero.ScatteringThrow.WindupEnabled = true;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.scatteringThrow.minBulletCount", 3) == 1 && registry.Resolve(template.Id, "nativeAttacks.AtkNormal.scatteringThrow.windupEnabled", 1) == 0,
            "Native ScatteringThrow registration freezes numeric and false leaves.");
        lease.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.scatteringThrow.minBulletCount", 3) == 3, "ScatteringThrow final withdrawal resolves current baseline.");
    }
}
