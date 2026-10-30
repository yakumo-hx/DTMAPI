using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void BatteringBulletParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkNormal", CooldownGameSeconds = 3,
            StartOnCooldown = false, Damage = 19, CriticalRate = 0, WindupGameSeconds = .4f,
            BatteringBullet = new MonsterBatteringBulletParameters { WindupEnabled = false, MinBulletCount = 4, MaxBulletCount = 6, AttackDistance = 20, TravelSpeed = 4, ProjectileHorizontalSpeed = 8, ProjectileLifetimeGameSeconds = 3 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var input = Parameters(); var frozen = new DefinitionRecord("A", Make(input), 1).Definition;
        input.BatteringBullet!.MinBulletCount = 63; input.BatteringBullet.ProjectileHorizontalSpeed = 30;
        Check(frozen.NativeAttacks[0].BatteringBullet!.MinBulletCount == 4 && frozen.NativeAttacks[0].BatteringBullet!.ProjectileHorizontalSpeed == 8, "BatteringBullet definition freezes nested impact count and horizontal speed.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "battering-bullet", fields) };
        Check(fields.Length == 12 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Twelve independently advertised BatteringBullet fields admit.");
        foreach (var edit in new Action<MonsterBatteringBulletParameters>[] { p => p.MinBulletCount = -1, p => p.MinBulletCount = 0, p => p.MaxBulletCount = 0, p => p.AttackDistance = -1, p => p.AttackDistance = float.NaN,
            p => p.TravelSpeed = 0, p => p.ProjectileHorizontalSpeed = -1, p => p.ProjectileLifetimeGameSeconds = float.PositiveInfinity })
        { var row = Parameters(); edit(row.BatteringBullet!); Check(MonsterValidation.Validate("A", Make(row))?.Code == "InvalidNativeAttack", "Invalid BatteringBullet bound rejects before native execution."); }
        var empty = Parameters(); empty.BatteringBullet = new MonsterBatteringBulletParameters();
        Check(MonsterValidation.Validate("A", Make(empty))?.Code == "InvalidNativeAttack", "Empty specialized intent rejects with common fields present.");
        var zero = Parameters(); zero.BatteringBullet!.MinBulletCount = 1; zero.BatteringBullet.MaxBulletCount = 1; zero.BatteringBullet.AttackDistance = 0;
        zero.BatteringBullet.TravelSpeed = .1f; zero.BatteringBullet.ProjectileHorizontalSpeed = 0; zero.BatteringBullet.ProjectileLifetimeGameSeconds = 0;
        Check(MonsterAdmission.Definition("A", Make(zero), catalog) == null, "Minimum one impact bullet; zero range, horizontal velocity and lifetime remain explicit with positive travel speed.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "battering-bullet", fields.Where(f => f.Field != "batteringBullet.projectileHorizontalSpeed")) };
        Check(MonsterAdmission.Definition("A", frozen, catalog)?.Code == "NativeAttackFieldUnsupported", "Unknown mover rejects only the unadvertised horizontal velocity edit.");
        var inherited = Parameters(); inherited.BatteringBullet!.ProjectileHorizontalSpeed = null;
        Check(MonsterAdmission.Definition("A", Make(inherited), catalog) == null, "Other BatteringBullet parameters remain independent of speed capability.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "battering-bullet", fields) };
        var native = new MonsterNativeOverride { LocalId = "impact", TargetId = template.Id }; native.NativeAttacks.Add(zero);
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content); var reopened = ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0,
            "BatteringBullet definition/native input survives codec and common compiler.");
        foreach (string bad in new[] { payload.Replace("\"windupEnabled\":false", "\"windupEnabled\":0"),
            payload.Replace("\"minBulletCount\":4", "\"minBulletCount\":2.5"), payload.Replace("\"attackDistance\":20", "\"travelDistance\":20"),
            payload.Replace("\"projectileHorizontalSpeed\":8", "\"projectileSpeed\":8") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Malformed BatteringBullet input must reject"); } catch (AuthoringException) { Check(true, "Typed bool/count and invented BatteringBullet fields reject."); } }
        var inverted = Parameters(); inverted.BatteringBullet!.MinBulletCount = 5; inverted.BatteringBullet.MaxBulletCount = 2;
        Check(MonsterAdmission.Definition("A", Make(inverted), catalog) == null, "Native reversed count range retains lower-bound selection.");
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 3, (_, _, _) => null); registry.Refresh(true, 1);
        var lease = registry.Register("A", 1, native).Value!; zero.BatteringBullet.MinBulletCount = 60; zero.BatteringBullet.WindupEnabled = true;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.batteringBullet.minBulletCount", 3) == 1 && registry.Resolve(template.Id, "nativeAttacks.AtkNormal.batteringBullet.windupEnabled", 1) == 0,
            "Native registration freezes minimum count and false windup.");
        lease.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.batteringBullet.minBulletCount", 3) == 3, "Final withdrawal resolves baseline.");
    }
}
