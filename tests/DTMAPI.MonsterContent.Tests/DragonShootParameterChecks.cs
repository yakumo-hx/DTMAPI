using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void DragonShootParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkSkill01", CooldownGameSeconds = 3,
            StartOnCooldown = false, Damage = 19, CriticalRate = 0, WindupGameSeconds = .4f,
            DragonShoot = new MonsterDragonShootParameters { WindupEnabled = false, MinBulletCount = 4, MaxBulletCount = 6, SectorCount = 3, SectorAngleDegrees = 90,
                ShotIntervalGameSeconds = .25f, ProjectileSpeed = 7, ProjectileLifetimeGameSeconds = 3 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var input = Parameters(); var frozen = new DefinitionRecord("A", Make(input), 1).Definition;
        input.DragonShoot!.MinBulletCount = 63; input.DragonShoot.SectorAngleDegrees = 360;
        Check(frozen.NativeAttacks[0].DragonShoot!.MinBulletCount == 4 && frozen.NativeAttacks[0].DragonShoot!.SectorAngleDegrees == 90, "Dragon definitions freeze nested count and geometry.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill01", "dragon-shoot", fields) };
        Check(fields.Length == 13 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Thirteen independent Dragon capabilities admit in the definition domain.");
        foreach (var edit in new Action<MonsterDragonShootParameters>[] { p => p.MinBulletCount = 0, p => p.SectorCount = 0, p => p.SectorAngleDegrees = -1, p => p.SectorAngleDegrees = float.NaN,
            p => p.ShotIntervalGameSeconds = -.1f, p => p.ProjectileSpeed = -1,
            p => p.ProjectileLifetimeGameSeconds = float.PositiveInfinity })
        { var row = Parameters(); edit(row.DragonShoot!); Check(MonsterValidation.Validate("A", Make(row))?.Code == "InvalidNativeAttack", "Invalid Dragon bound rejects before native execution."); }
        var empty = Parameters(); empty.DragonShoot = new MonsterDragonShootParameters();
        Check(MonsterValidation.Validate("A", Make(empty))?.Code == "InvalidNativeAttack", "Empty Dragon intent rejects even with common fields.");
        var zero = Parameters(); zero.DragonShoot!.MinBulletCount = 1; zero.DragonShoot.MaxBulletCount = 1; zero.DragonShoot.SectorCount = 1; zero.DragonShoot.SectorAngleDegrees = 0; zero.DragonShoot.ShotIntervalGameSeconds = 0;
        zero.DragonShoot.ProjectileSpeed = 0; zero.DragonShoot.ProjectileLifetimeGameSeconds = 0;
        Check(MonsterAdmission.Definition("A", Make(zero), catalog) == null, "Single bullet, zero angle/timing/projectile and false windup remain explicit.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill01", "dragon-shoot", fields.Where(f => f.Field != "dragonShoot.projectileSpeed")) };
        Check(MonsterAdmission.Definition("A", frozen, catalog)?.Code == "NativeAttackFieldUnsupported", "Unproven Dragon mover speed rejects per field.");
        var inherited = Parameters(); inherited.DragonShoot!.ProjectileSpeed = null;
        Check(MonsterAdmission.Definition("A", Make(inherited), catalog) == null, "Other Dragon fields do not require a speed edit.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill01", "dragon-shoot", fields) };
        var native = new MonsterNativeOverride { LocalId = "volley", TargetId = template.Id }; native.NativeAttacks.Add(zero);
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content); var reopened = ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0,
            "Dragon definitions/overrides survive codec, capability catalog and Compiler roundtrip.");
        foreach (string bad in new[] { payload.Replace("\"windupEnabled\":false", "\"windupEnabled\":0"),
            payload.Replace("\"minBulletCount\":4", "\"minBulletCount\":2.5"), payload.Replace("\"sectorAngleDegrees\":90", "\"unsupportedAngle\":90") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Malformed Dragon input must reject"); } catch (AuthoringException) { Check(true, "Typed bool/integer and unknown Dragon fields reject."); } }
        var inverted = Parameters(); inverted.DragonShoot!.MinBulletCount = 5; inverted.DragonShoot.MaxBulletCount = 2;
        Check(MonsterAdmission.Definition("A", Make(inverted), catalog) == null, "Native reversed range explicitly selects its lower-bound field.");
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 3, (_, _, _) => null); registry.Refresh(true, 1);
        var lease = registry.Register("A", 1, native).Value!; zero.DragonShoot.MinBulletCount = 60; zero.DragonShoot.WindupEnabled = true;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.dragonShoot.minBulletCount", 3) == 1 && registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.dragonShoot.windupEnabled", 1) == 0,
            "Native Dragon registration freezes numeric and false leaves.");
        lease.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.dragonShoot.minBulletCount", 3) == 3, "Dragon final withdrawal resolves current baseline.");
    }
}
