using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void BombingDiveParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkNormal", CooldownGameSeconds = 3,
            StartOnCooldown = false, Damage = 19, CriticalRate = 0, WindupGameSeconds = .4f,
            BombingDive = new MonsterBombingDiveParameters { WindupEnabled = false, MinBulletCount = 4, MaxBulletCount = 6, Range = 20, TravelDistance = 6, TravelSpeed = 4, ProjectileLifetimeGameSeconds = 3 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var input = Parameters(); var frozen = new DefinitionRecord("A", Make(input), 1).Definition;
        input.BombingDive!.MinBulletCount = 63; input.BombingDive.TravelDistance = 30;
        Check(frozen.NativeAttacks[0].BombingDive!.MinBulletCount == 4 && frozen.NativeAttacks[0].BombingDive!.TravelDistance == 6, "BombingDive definitions freeze nested count and geometry.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "bombing-dive", fields) };
        Check(fields.Length == 12 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Twelve independent BombingDive capabilities admit in the definition domain.");
        foreach (var edit in new Action<MonsterBombingDiveParameters>[] { p => p.MinBulletCount = 0, p => p.Range = -1, p => p.Range = float.NaN, p => p.TravelDistance = -.1f, p => p.TravelSpeed = 0, p => p.ProjectileLifetimeGameSeconds = float.PositiveInfinity })
        { var row = Parameters(); edit(row.BombingDive!); Check(MonsterValidation.Validate("A", Make(row))?.Code == "InvalidNativeAttack", "Invalid BombingDive bound rejects before native execution."); }
        var empty = Parameters(); empty.BombingDive = new MonsterBombingDiveParameters();
        Check(MonsterValidation.Validate("A", Make(empty))?.Code == "InvalidNativeAttack", "Empty BombingDive intent rejects even with common fields.");
        var zero = Parameters(); zero.BombingDive!.MinBulletCount = 1; zero.BombingDive.MaxBulletCount = 1; zero.BombingDive.Range = 0; zero.BombingDive.TravelDistance = 0; zero.BombingDive.TravelSpeed = .1f;
        zero.BombingDive.ProjectileLifetimeGameSeconds = 0;
        Check(MonsterAdmission.Definition("A", Make(zero), catalog) == null, "Single bullet, zero range/distance/projectile, positive speed and false windup remain explicit.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "bombing-dive", fields.Where(f => f.Field != "bombingDive.travelSpeed")) };
        Check(MonsterAdmission.Definition("A", frozen, catalog)?.Code == "NativeAttackFieldUnsupported", "Unsupported BombingDive travel speed rejects per field.");
        var inherited = Parameters(); inherited.BombingDive!.TravelSpeed = null;
        Check(MonsterAdmission.Definition("A", Make(inherited), catalog) == null, "Other BombingDive fields do not require a speed edit.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "bombing-dive", fields) };
        var native = new MonsterNativeOverride { LocalId = "volley", TargetId = template.Id }; native.NativeAttacks.Add(zero);
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content); var reopened = ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0,
            "BombingDive definitions/overrides survive codec, capability catalog and Compiler roundtrip.");
        foreach (string bad in new[] { payload.Replace("\"windupEnabled\":false", "\"windupEnabled\":0"),
            payload.Replace("\"minBulletCount\":4", "\"minBulletCount\":2.5"), payload.Replace("\"range\":20", "\"unsupportedRange\":20"), payload.Replace("\"range\":20", "\"projectileSpeed\":20") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Malformed BombingDive input must reject"); } catch (AuthoringException) { Check(true, "Typed bool/integer and unknown BombingDive fields reject."); } }
        var inverted = Parameters(); inverted.BombingDive!.MinBulletCount = 5; inverted.BombingDive.MaxBulletCount = 2;
        Check(MonsterAdmission.Definition("A", Make(inverted), catalog) == null, "Native reversed range explicitly selects its lower-bound field.");
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 3, (_, _, _) => null); registry.Refresh(true, 1);
        var lease = registry.Register("A", 1, native).Value!; zero.BombingDive.MinBulletCount = 60; zero.BombingDive.WindupEnabled = true;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.bombingDive.minBulletCount", 3) == 1 && registry.Resolve(template.Id, "nativeAttacks.AtkNormal.bombingDive.windupEnabled", 1) == 0,
            "Native BombingDive registration freezes numeric and false leaves.");
        lease.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkNormal.bombingDive.minBulletCount", 3) == 3, "BombingDive final withdrawal resolves current baseline.");
    }
}
