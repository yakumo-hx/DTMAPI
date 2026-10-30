using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void BulletTrainParameterChecks()
    {
        MonsterNativeAttackParameters Parameters() => new MonsterNativeAttackParameters { AttackId = "AtkSkill01", CooldownGameSeconds = 3,
            StartOnCooldown = false, Damage = 19, CriticalRate = 0, WindupGameSeconds = .4f,
            BulletTrain = new MonsterBulletTrainParameters { WindupEnabled = false, VolleyCount = 3, VolleyIntervalGameSeconds = .25f,
                BulletCount = 4, SectorAngleDegrees = 90, DirectionX = -.5f, DirectionY = 0, ProjectileSpeed = 7, ProjectileLifetimeGameSeconds = 3 } };
        MonsterDefinition Make(MonsterNativeAttackParameters p) { var d = Definition(); d.NativeAttacks.Add(p); return d; }
        var input = Parameters(); var frozen = new DefinitionRecord("A", Make(input), 1).Definition;
        input.BulletTrain!.VolleyCount = 63; input.BulletTrain.DirectionX = 1;
        Check(frozen.NativeAttacks[0].BulletTrain!.VolleyCount == 3 && frozen.NativeAttacks[0].BulletTrain!.DirectionX == -.5f,
            "BulletTrain freezes nested schedule and literal direction.");
        var catalog = AuthorCatalog(); var template = catalog.Templates[0]; template.NativeOverrideFields = new[] { "nativeAttacks" };
        var fields = NativeAttackValidation.Fields(Parameters()).Select(f => new MonsterNativeAttackFieldInfo(f.Key, f.Value, "native-unit", "native-read", true, true)).ToArray();
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill01", "bullet-train", fields) };
        Check(fields.Length == 14 && MonsterAdmission.Definition("A", frozen, catalog) == null, "Fourteen BulletTrain fields admit in the exact slot.");
        foreach (var edit in new Action<MonsterBulletTrainParameters>[] { p => p.VolleyCount = 0, p => p.BulletCount = 0,
            p => p.SectorAngleDegrees = -1, p => p.DirectionX = float.NaN, p => p.VolleyIntervalGameSeconds = -.1f,
            p => p.ProjectileSpeed = -1, p => p.ProjectileLifetimeGameSeconds = float.PositiveInfinity })
        { var row = Parameters(); edit(row.BulletTrain!); Check(MonsterValidation.Validate("A", Make(row))?.Code == "InvalidNativeAttack", "Invalid BulletTrain bounds reject before execution."); }
        var empty = Parameters(); empty.BulletTrain = new MonsterBulletTrainParameters();
        Check(MonsterValidation.Validate("A", Make(empty))?.Code == "InvalidNativeAttack", "Empty train intent rejects beside common fields.");
        var zero = Parameters(); zero.BulletTrain!.VolleyCount = 1; zero.BulletTrain.BulletCount = 1; zero.BulletTrain.SectorAngleDegrees = 0;
        zero.BulletTrain.DirectionX = 0; zero.BulletTrain.DirectionY = 0; zero.BulletTrain.VolleyIntervalGameSeconds = 0;
        zero.BulletTrain.ProjectileSpeed = 0; zero.BulletTrain.ProjectileLifetimeGameSeconds = 0;
        Check(MonsterAdmission.Definition("A", Make(zero), catalog) == null, "Single volley/bullet, zero direction/timing/speed/life and false stay explicit.");
        var native = new MonsterNativeOverride { LocalId = "train", TargetId = template.Id }; native.NativeAttacks.Add(zero);
        var content = new ContentInput { OwnerId = "A" }; content.Definitions.Add(frozen); content.NativeOverrides.Add(native);
        string payload = ContentCodec.WriteContent(content); var reopened = ContentCodec.ReadContent(payload, "A", _ => Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == payload && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0,
            "Train definition and native inputs roundtrip through the same Compiler and catalog.");
        foreach (string bad in new[] { payload.Replace("\"windupEnabled\":false", "\"windupEnabled\":0"),
            payload.Replace("\"volleyCount\":3", "\"volleyCount\":2.5"), payload.Replace("\"sectorAngleDegrees\":90", "\"sectorAngleDegrees\":90.5"),
            payload.Replace("\"directionX\":-0.5", "\"flySpeed\":4") })
        { try { ContentCodec.ReadContent(bad, "A", _ => Array.Empty<byte>()); Check(false, "Malformed train input must reject"); } catch (AuthoringException) { Check(true, "Strict bool/count/integer angle and unused FlySpeed reject."); } }
        template.Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkSkill01", "bullet-train", fields.Select(f => new MonsterNativeAttackFieldInfo(f.Field,f.Value,f.Unit,f.ReadAt,true,false))) };
        Check(MonsterAdmission.Definition("A", frozen, catalog) == null && MonsterOverrideAdmission.Validate("A", native, catalog)?.Code == "NativeAttackFieldUnsupported",
            "Definition and native train capability domains are independent.");
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 3, (_, _, _) => null); registry.Refresh(true, 1);
        var lease = registry.Register("A", 1, native).Value!; zero.BulletTrain.DirectionX = 1; zero.BulletTrain.WindupEnabled = true;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.bulletTrain.directionX", 3) == 0 && registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.bulletTrain.windupEnabled", 1) == 0,
            "Native train freezes zero components and explicit disabled windup.");
        var upper = new MonsterNativeOverride { LocalId = "direction", TargetId = template.Id };
        upper.NativeAttacks.Add(new MonsterNativeAttackParameters { AttackId = "AtkSkill01", BulletTrain = new MonsterBulletTrainParameters { DirectionY = -1 } });
        var top = registry.Register("B", 2, upper).Value!;
        Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.bulletTrain.directionX", 3) == 0 && registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.bulletTrain.directionY", 3) == -1,
            "Independent direction leaves compose without replacing the lower schedule.");
        top.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.bulletTrain.directionY", 3) == 0, "Leaf withdrawal reveals lower explicit zero.");
        lease.Dispose(); Check(registry.Resolve(template.Id, "nativeAttacks.AtkSkill01.bulletTrain.directionY", 3) == 3, "Final withdrawal resolves the current baseline.");
    }
}
