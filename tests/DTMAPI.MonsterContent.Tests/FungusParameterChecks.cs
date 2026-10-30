using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void FungusParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkNormal", CooldownGameSeconds = 3,
            StartOnCooldown = false, Damage = 19, CriticalRate = 0, WindupGameSeconds = .4f,
            FungusShoot = new MonsterFungusShootParameters { WindupEnabled = false, ProjectileSpeed = 6, ProjectileLifetimeGameSeconds = 3 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var input = Parameters(); var frozen = new DefinitionRecord("A", Make(input), 1).Definition;
        input.FungusShoot!.ProjectileSpeed = 80; input.FungusShoot.ProjectileLifetimeGameSeconds = 30;
        Check(frozen.NativeAttacks[0].FungusShoot!.ProjectileSpeed == 6 && frozen.NativeAttacks[0].FungusShoot!.ProjectileLifetimeGameSeconds == 3, "Fungus registration freezes nested projectile values.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "fungus-shoot", fields) };
        Check(fields.Length == 8 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Eight independently advertised Fungus fields admit.");
        foreach (var edit in new Action<MonsterFungusShootParameters>[] { p => p.ProjectileSpeed = -1, p => p.ProjectileSpeed = float.NaN, p => p.ProjectileLifetimeGameSeconds = -1, p => p.ProjectileLifetimeGameSeconds = float.PositiveInfinity })
        { var row = Parameters(); edit(row.FungusShoot!); Check(MonsterValidation.Validate("A", Make(row))?.Code == "InvalidNativeAttack", "Invalid initial speed/lifetime rejects before native allocation."); }
        var empty = Parameters(); empty.FungusShoot = new MonsterFungusShootParameters();
        Check(MonsterValidation.Validate("A", Make(empty))?.Code == "InvalidNativeAttack", "Empty specialized object rejects even with common fields.");
        var zero = Parameters(); zero.FungusShoot!.ProjectileSpeed = 0; zero.FungusShoot.ProjectileLifetimeGameSeconds = 0; zero.WindupGameSeconds = 0;
        Check(MonsterAdmission.Definition("A", Make(zero), catalog) == null, "Zero windup, initial speed and lifetime remain explicit.");
        var native = new MonsterNativeOverride { LocalId = "spores", TargetId = template.Id }; native.NativeAttacks.Add(zero);
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content); var reopened = ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0,
            "New/native Fungus inputs roundtrip through compiler and exact catalog.");
        foreach (string bad in new[] { payload.Replace("\"windupEnabled\":false", "\"windupEnabled\":0"),
            payload.Replace("\"projectileSpeed\":6", "\"bulletCount\":6"), payload.Replace("\"projectileSpeed\":6", "\"sectorAngleDegrees\":6") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Malformed Fungus input must reject"); } catch (AuthoringException) { Check(true, "Typed flag and fixed native fan fields reject."); } }
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "fungus-shoot", fields.Select(f => new MonsterNativeAttackFieldInfo(f.Field,f.Value,f.Unit,f.ReadAt,true,false))) };
        Check(MonsterAdmission.Definition("A", frozen, catalog) == null && MonsterOverrideAdmission.Validate("A", native, catalog)?.Code == "NativeAttackFieldUnsupported", "Definition and native author domains are independent.");
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 3, (_, _, _) => null); registry.Refresh(true, 1);
        var lease = registry.Register("A", 1, native).Value!; zero.FungusShoot.ProjectileSpeed = 90; zero.FungusShoot.WindupEnabled = true;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.fungusShoot.projectileSpeed", 3) == 0 && registry.Resolve(template.Id, "nativeAttacks.AtkNormal.fungusShoot.windupEnabled", 1) == 0,
            "Native registration freezes zero speed and false windup.");
        lease.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.fungusShoot.projectileSpeed", 3) == 3, "Final withdrawal reveals baseline.");
    }
}
