using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void ScatteringShootParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkSkill01", CooldownGameSeconds = 3,
            StartOnCooldown = false, Damage = 19, CriticalRate = 0, WindupGameSeconds = .4f,
            ScatteringShoot = new MonsterScatteringShootParameters { WindupEnabled = false, MinBulletCount = 4, MaxBulletCount = 6,
                SectorAngleDegrees = 90, BurstCount = 3, BurstIntervalGameSeconds = .25f, DirectionMode = MonsterScatteringMode.Offset,
                AngleOffsetDegrees = 20, ReverseAngleOffset = false, ProjectileSpeed = 7, ProjectileLifetimeGameSeconds = 3 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var input = Parameters(); var frozen = new DefinitionRecord("A", Make(input), 1).Definition;
        input.ScatteringShoot!.MinBulletCount = 63; input.ScatteringShoot.DirectionMode = MonsterScatteringMode.Random;
        Check(frozen.NativeAttacks[0].ScatteringShoot!.MinBulletCount == 4 && frozen.NativeAttacks[0].ScatteringShoot!.DirectionMode == MonsterScatteringMode.Offset, "Scattering definitions freeze nested counts and enum.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill01", "scattering-shoot", fields) };
        Check(fields.Length == 16 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Sixteen independent Scattering capabilities admit in definition domain.");
        foreach (var edit in new Action<MonsterScatteringShootParameters>[] { p => p.MinBulletCount = 0, p => p.BurstCount = 0, p => p.SectorAngleDegrees = -1, p => p.AngleOffsetDegrees = -1, p => p.AngleOffsetDegrees = float.NaN, p => p.DirectionMode = (MonsterScatteringMode)(-1), p => p.DirectionMode = (MonsterScatteringMode)4,
            p => p.BurstIntervalGameSeconds = -.1f, p => p.ProjectileSpeed = -1,
            p => p.ProjectileLifetimeGameSeconds = float.PositiveInfinity })
        { var row = Parameters(); edit(row.ScatteringShoot!); Check(MonsterValidation.Validate("A", Make(row))?.Code == "InvalidNativeAttack", "Invalid Scattering bound/enum rejects before native execution."); }
        var empty = Parameters(); empty.ScatteringShoot = new MonsterScatteringShootParameters();
        Check(MonsterValidation.Validate("A", Make(empty))?.Code == "InvalidNativeAttack", "Empty Scattering intent rejects even with common fields.");
        var zero = Parameters(); zero.ScatteringShoot!.MinBulletCount = 5; zero.ScatteringShoot.MaxBulletCount = 2; zero.ScatteringShoot.SectorAngleDegrees = 0;
        zero.ScatteringShoot.BurstIntervalGameSeconds = 0; zero.ScatteringShoot.AngleOffsetDegrees = 0; zero.ScatteringShoot.ProjectileSpeed = 0; zero.ScatteringShoot.ProjectileLifetimeGameSeconds = 0;
        Check(MonsterAdmission.Definition("A", Make(zero), catalog) == null, "Native inverted count range, zero geometry/timing/projectile and false flags remain explicit.");
        foreach (MonsterScatteringMode mode in Enum.GetValues(typeof(MonsterScatteringMode)))
        {
            var row = Parameters(); row.ScatteringShoot!.DirectionMode = mode; var c = new ContentInput { OwnerId = "A" }; c.Definitions.Add(Make(row));
            var json = ContentCodec.WriteContent(c);
            Check(json.Contains("\"directionMode\":\"" + mode + "\"") && ContentCodec.ReadContent(json, "A", _ => Array.Empty<byte>()).Definitions[0].NativeAttacks[0].ScatteringShoot!.DirectionMode == mode, "All named modes roundtrip without raw game types or numeric JSON enums.");
        }
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill01", "scattering-shoot", fields.Where(f => f.Field != "scatteringShoot.projectileSpeed")) };
        Check(MonsterAdmission.Definition("A", frozen, catalog)?.Code == "NativeAttackFieldUnsupported", "Unproven Scattering mover speed rejects per leaf.");
        var inherited = Parameters(); inherited.ScatteringShoot!.ProjectileSpeed = null;
        Check(MonsterAdmission.Definition("A", Make(inherited), catalog) == null, "Other Scattering fields do not require speed editing.");
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill01", "scattering-shoot", fields) };
        var native = new MonsterNativeOverride { LocalId = "bursts", TargetId = template.Id }; native.NativeAttacks.Add(zero);
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content); var reopened = ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0, "Scattering definition/override and capability catalog survive Compiler roundtrip.");
        foreach (string bad in new[] { payload.Replace("\"windupEnabled\":false", "\"windupEnabled\":0"), payload.Replace("\"minBulletCount\":4", "\"minBulletCount\":2.5"),
            payload.Replace("\"directionMode\":\"Offset\"", "\"directionMode\":3"), payload.Replace("\"directionMode\":\"Offset\"", "\"directionMode\":\"Tracking\""),
            payload.Replace("\"burstIntervalGameSeconds\":0", "\"shootDuration\":0") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Malformed Scattering input must reject"); } catch (AuthoringException) { Check(true, "Typed boolean/count/enum and unused timing fields reject."); } }
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 0, (_, _, _) => null); registry.Refresh(true, 1);
        var lease = registry.Register("A", 1, native).Value!; zero.ScatteringShoot.DirectionMode = MonsterScatteringMode.Random; zero.ScatteringShoot.WindupEnabled = true;
        string direction = "nativeAttacks.AtkSkill01.scatteringShoot.directionMode", count = "nativeAttacks.AtkSkill01.scatteringShoot.minBulletCount";
        Check(registry.Resolve(template.Id, direction, 0) == 3 && registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.scatteringShoot.windupEnabled", 1) == 0, "Native Scattering registration freezes enum and false leaves.");
        var later = new MonsterNativeOverride { LocalId = "direction", TargetId = template.Id }; later.NativeAttacks.Add(new MonsterNativeAttackParameters { AttackId = "AtkSkill01", ScatteringShoot = new MonsterScatteringShootParameters { DirectionMode = MonsterScatteringMode.Player } });
        var top = registry.Register("B", 2, later).Value!;
        Check(registry.Resolve(template.Id, direction, 0) == 2 && registry.Resolve(template.Id, count, 0) == 5, "Per-leaf enum winner leaves the count owner intact.");
        top.Dispose(); Check(registry.Resolve(template.Id, direction, 0) == 3, "Enum withdrawal reveals earlier source.");
        lease.Dispose(); Check(registry.Resolve(template.Id, direction, 0) == 0, "Final enum withdrawal restores current baseline.");
    }
}
