using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void ShootParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkNormal", Damage = 17, CriticalRate = .4f,
            CooldownGameSeconds = 3, StartOnCooldown = false, WindupGameSeconds = .6f,
            Shoot = new MonsterShootParameters { WindupEnabled = true, RecoveryGameSeconds = .8f, ProjectileSpeed = 6, ProjectileLifetimeGameSeconds = 4 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var source = Parameters(); var frozen = new DefinitionRecord("A", Make(source), 1).Definition;
        source.Shoot!.ProjectileSpeed = 99; source.Shoot.WindupEnabled = false;
        Check(frozen.NativeAttacks[0].Shoot!.ProjectileSpeed == 6 && frozen.NativeAttacks[0].Shoot!.WindupEnabled == true, "Shoot nested definition input freezes before publication.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "shoot", fields) };
        Check(MonsterAdmission.Definition("A", frozen, catalog) == null, "Shoot admits only declared native parameters.");
        foreach (var edit in new Action<MonsterNativeAttackParameters>[] { p => p.Shoot!.ProjectileSpeed = -1, p => p.Shoot!.ProjectileLifetimeGameSeconds = float.NaN, p => p.Shoot!.RecoveryGameSeconds = -1, p => p.Shoot = new MonsterShootParameters() })
        { var p = Parameters(); edit(p); Check(MonsterValidation.Validate("A", Make(p))?.Code == "InvalidNativeAttack", "Invalid or empty Shoot parameters reject before native invocation."); }
        var zero = Parameters(); zero.Shoot = new MonsterShootParameters { WindupEnabled = false, RecoveryGameSeconds = 0, ProjectileSpeed = 0, ProjectileLifetimeGameSeconds = 0 };
        Check(MonsterAdmission.Definition("A", Make(zero), catalog) == null, "False and zero remain explicit Shoot parameters, not omitted defaults.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "shoot", fields.Where(f => f.Field != "shoot.projectileSpeed")) };
        Check(MonsterAdmission.Definition("A", frozen, catalog)?.Code == "NativeAttackFieldUnsupported", "Unproven mover speed remains rejected independently of other Shoot capabilities.");
        var inheritedSpeed = Parameters(); inheritedSpeed.Shoot!.ProjectileSpeed = null;
        Check(MonsterAdmission.Definition("A", Make(inheritedSpeed), catalog) == null, "A Shoot slot without editable speed can retain native speed and edit other proven fields.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "amoeba-jump", fields.Where(f => !f.Field.StartsWith("shoot."))) };
        Check(MonsterAdmission.Definition("A", frozen, catalog)?.Code == "NativeAttackFieldUnsupported", "Shoot-specific input cannot be silently applied to another mechanism.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "shoot", fields) };
        var value = new MonsterNativeOverride { LocalId = "shot", TargetId = template.Id }; value.NativeAttacks.Add(zero);
        var input = new ContentInput { OwnerId = "A" }; input.Definitions.Add(frozen); input.NativeOverrides.Add(value);
        string payload = ContentCodec.WriteContent(input);
        var reopened = ContentCodec.ReadContent(payload, "A", _ => throw new Exception("No images"));
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0,
            "Both Shoot author domains preserve false/zero, parameters and per-field capabilities through the actual codec/compiler.");
        foreach (string bad in new[] { payload.Replace("\"windupEnabled\":false", "\"windupEnabled\":0"), payload.Replace("\"projectileSpeed\":0", "\"projectileSpeed\":0,\"disableRecoil\":true") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Invalid Shoot payload must reject"); } catch (AuthoringException) { Check(true, "Boolean typing and unused native recoil input reject explicitly."); } }
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 8, (_, _, _) => null); registry.Refresh(true, 1);
        var lease = registry.Register("A", 1, value).Value!; zero.Shoot!.WindupEnabled = true; zero.Shoot.ProjectileSpeed = 45;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.shoot.windupEnabled", 1) == 0 && registry.Resolve(template.Id, "nativeAttacks.AtkNormal.shoot.projectileSpeed", 9) == 0,
            "Native Shoot overrides freeze nested zero/false leaf values.");
        lease.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.shoot.projectileSpeed", 9) == 9, "Shoot withdrawal resolves current native baseline.");
    }
}
