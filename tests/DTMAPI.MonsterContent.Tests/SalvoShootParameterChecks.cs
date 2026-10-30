using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void SalvoShootParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkNormal", CooldownGameSeconds = 3,
            StartOnCooldown = false, Damage = 19, CriticalRate = 0, WindupGameSeconds = .4f,
            SalvoShoot = new MonsterSalvoShootParameters { WindupEnabled = false, BulletCount = 4, SectorAngleDegrees = 90,
                ShotIntervalGameSeconds = .25f, ProjectileSpeed = 7, ProjectileLifetimeGameSeconds = 3 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var input = Parameters(); var frozen = new DefinitionRecord("A", Make(input), 1).Definition;
        input.SalvoShoot!.BulletCount = 63; input.SalvoShoot.SectorAngleDegrees = 360;
        Check(frozen.NativeAttacks[0].SalvoShoot!.BulletCount == 4 && frozen.NativeAttacks[0].SalvoShoot!.SectorAngleDegrees == 90, "Salvo definitions freeze nested count and geometry.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "salvo-shoot", fields) };
        Check(fields.Length == 11 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Eleven independent Salvo capabilities admit in the definition domain.");
        foreach (var edit in new Action<MonsterSalvoShootParameters>[] { p => p.BulletCount = 0, p => p.SectorAngleDegrees = -1, p => p.SectorAngleDegrees = float.NaN,
            p => p.ShotIntervalGameSeconds = -.1f, p => p.ProjectileSpeed = -1,
            p => p.ProjectileLifetimeGameSeconds = float.PositiveInfinity })
        { var row = Parameters(); edit(row.SalvoShoot!); Check(MonsterValidation.Validate("A", Make(row))?.Code == "InvalidNativeAttack", "Invalid Salvo bound rejects before native execution."); }
        var empty = Parameters(); empty.SalvoShoot = new MonsterSalvoShootParameters();
        Check(MonsterValidation.Validate("A", Make(empty))?.Code == "InvalidNativeAttack", "Empty Salvo intent rejects even with common fields.");
        var zero = Parameters(); zero.SalvoShoot!.BulletCount = 1; zero.SalvoShoot.SectorAngleDegrees = 0; zero.SalvoShoot.ShotIntervalGameSeconds = 0;
        zero.SalvoShoot.ProjectileSpeed = 0; zero.SalvoShoot.ProjectileLifetimeGameSeconds = 0;
        Check(MonsterAdmission.Definition("A", Make(zero), catalog) == null, "Single bullet, zero angle/timing/projectile and false windup remain explicit.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "salvo-shoot", fields.Where(f => f.Field != "salvoShoot.projectileSpeed")) };
        Check(MonsterAdmission.Definition("A", frozen, catalog)?.Code == "NativeAttackFieldUnsupported", "Unproven Salvo mover speed rejects per field.");
        var inherited = Parameters(); inherited.SalvoShoot!.ProjectileSpeed = null;
        Check(MonsterAdmission.Definition("A", Make(inherited), catalog) == null, "Other Salvo fields do not require a speed edit.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "salvo-shoot", fields) };
        var native = new MonsterNativeOverride { LocalId = "volley", TargetId = template.Id }; native.NativeAttacks.Add(zero);
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content); var reopened = ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0,
            "Salvo definitions/overrides survive codec, capability catalog and Compiler roundtrip.");
        foreach (string bad in new[] { payload.Replace("\"windupEnabled\":false", "\"windupEnabled\":0"),
            payload.Replace("\"bulletCount\":4", "\"bulletCount\":2.5"), payload.Replace("\"sectorAngleDegrees\":90", "\"unsupportedAngle\":90") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Malformed Salvo input must reject"); } catch (AuthoringException) { Check(true, "Typed bool/integer and unknown Salvo fields reject."); } }
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 3, (_, _, _) => null); registry.Refresh(true, 1);
        var lease = registry.Register("A", 1, native).Value!; zero.SalvoShoot.BulletCount = 60; zero.SalvoShoot.WindupEnabled = true;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.salvoShoot.bulletCount", 3) == 1 && registry.Resolve(template.Id, "nativeAttacks.AtkNormal.salvoShoot.windupEnabled", 1) == 0,
            "Native Salvo registration freezes numeric and false leaves.");
        lease.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.salvoShoot.bulletCount", 3) == 3, "Salvo final withdrawal resolves current baseline.");
    }
}
