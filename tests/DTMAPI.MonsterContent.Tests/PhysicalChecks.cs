using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void PhysicalChecks()
    {
        var catalog = AuthorCatalog();
        catalog.Templates[0].Volume = 1;
        var d = new MonsterDefinition { LocalId = "large", TemplateId = "test-template",
            Body = new MonsterBodyParameters { Scale = 2.5f }, Movement = new MonsterMovementParameters { MoveSpeed = 4, Directional = false },
            Sight = new MonsterSightParameters { EnemyThreshold = 4, RangeScale = 1.5f } };
        var input = new ContentInput { OwnerId = "A" }; input.Definitions.Add(d);
        Check(ContentCompiler.Validate(input, catalog).Any(e => e.Code == "BodyUnsupported"), "Old catalog cannot grant geometry editing.");
        var ground = new MonsterMovementParameters { MoveSpeed = 2, JumpSpeed = 10, TargetRadius = .1f, AroundRange = 4, Directional = true };
        var sight = new MonsterSightParameters { RangeScale = 1, EnemyThreshold = 5, MinAlertAddition = .5f, MaxAlertAddition = 2, MaxAlertDuration = 8, TooFarDistance = 40 };
        catalog.Templates[0].Physical = new MonsterPhysicalInfo(true, "", 1, 0, 0, "ground", ground, "MonsterSight", sight);
        ground.MoveSpeed = 90; sight.EnemyThreshold = 90;
        var info = catalog.Templates[0].Physical!; info.Movement!.MoveSpeed = 91;
        var copy = CatalogCodec.Read(CatalogCodec.Write(catalog));
        Check(copy.Templates[0].Physical!.Movement!.MoveSpeed == 2 && copy.Templates[0].Physical!.Sight!.EnemyThreshold == 5,
            "Catalog freezes native baselines and defends them from returned DTO mutation.");
        Check(ContentCompiler.Validate(input, copy).Count == 0 && copy.Templates[0].EditableFields.Contains("body"), "Current mechanism capabilities admit all three scopes.");
        var record = new DefinitionRecord("A", d, 1);
        d.Body.Scale = 1; d.Movement.MoveSpeed = 6; d.Sight.RangeScale = 2;
        Check(record.Definition.Body!.Scale == 2.5f && record.Definition.Movement!.MoveSpeed == 4 && record.Definition.Sight!.RangeScale == 1.5f,
            "Frozen definition owns three independent parameter DTOs.");
        input.Definitions[0] = record.Definition; d = input.Definitions[0];
        string payload = ContentCodec.WriteContent(input);
        var decoded = ContentCodec.ReadContent(payload, "A", _ => throw new Exception("No image expected"));
        Check(ContentCodec.WriteContent(decoded) == payload && decoded.Definitions[0].Movement!.Directional == false,
            "Payload roundtrip preserves nullable inheritance and explicit false.");
        d.Body!.SpawnVolume = 2;
        Check(ContentCompiler.Validate(input, copy).Any(e => e.Field == "body.spawnVolume"), "Explicit slots cannot under-reserve the scaled body.");
        d.Body.SpawnVolume = null;
        input.SpawnRules.Add(new MonsterSpawnRule { LocalId = "natural", DefinitionId = "A:large", RoomId = "room", RegionId = "ground", Count = 3 });
        Check(ContentCompiler.Validate(input, copy).Any(e => e.Code == "SpawnCapacityExceeded"), "Compiler uses rounded scaled slot width, not the original template width.");
        input.SpawnRules[0].Count = 2;
        Check(ContentCompiler.Validate(input, copy).Count == 0, "A fitting scaled batch is admitted.");
        d.Movement!.MaxAcceleration = 8;
        Check(ContentCompiler.Validate(input, copy).Any(e => e.Code == "MovementFieldUnsupported"), "Ground cannot silently ignore context-steering parameters.");
        d.Movement.MaxAcceleration = null; d.Sight!.MinAlertAddition = 3;
        Check(ContentCompiler.Validate(input, copy).Any(e => e.Field == "sight.maxAlertAddition"), "Sight validates authored values against inherited native bounds.");
        d.Sight.MinAlertAddition = null; d.Sight.EnemyThreshold = 10;
        Check(ContentCompiler.Validate(input, copy).Count == 0, "An author may choose an unreachable native alert threshold.");
        d.Sight.EnemyThreshold = 4;
        foreach (float bad in new[] { 0, float.NaN, float.PositiveInfinity })
        {
            d.Body.Scale = bad;
            Check(MonsterValidation.Validate("A", d)?.Field == "body.scale", "Invalid scale is rejected before publication.");
        }
        d.Body = null; d.Movement = new MonsterMovementParameters { TargetRadius = 3 };
        copy.Templates[0].Physical = new MonsterPhysicalInfo(false, "Other body owner", 1, 0, 0, "context-steering",
            new MonsterMovementParameters { TargetRadius = 1, SlowRadius = 2 }, "MonsterSight", info.Sight);
        Check(ContentCompiler.Validate(input, copy).Count == 0, "Authored steering radii retain native behavior without a platform relationship guard.");
    }
}
