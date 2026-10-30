using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void Completion075Checks()
    {
        var original = AuthorCatalog(); var target = original.Templates[0];
        target.NativeParameters = new MonsterNativeParameters { TrafficLight = new MonsterTrafficLightParameters { HoverSpeedMin = .5f, HoverSpeedMax = 1.2f } };
        target.Physical = new MonsterPhysicalInfo(false, "", 2, 0, 0, "shuttle", new MonsterMovementParameters { WaitGameSeconds = 2, MoveGameSeconds = 3 }, "MonsterSight", new MonsterSightParameters { DisableEmotion = false });
        target.Actions.NativeParts = new[] { new MonsterNativePartInfo("part", Array.Empty<MonsterNativeAttackInfo>()) { Health = 100, Defense = 3, HurtDuration = .2f } };
        var source = new MonsterTemplate("source", 1, true, false, "fixture", "fixture", 100, 0, "fixture", Array.Empty<MonsterDiagnostic>()) {
            Actions = new MonsterActionCapabilities(new[] { "AtkNormal" }, true) { NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "shoot", new[] { new MonsterNativeAttackFieldInfo("damage", 10, "damage", "hit", true, true) }) } } };
        var catalog = new MonsterContentCatalog(1, "fixture", "0.1.70", "completion", new[] { target, source }, original.Items, original.DropPools, original.Rooms);
        var d = Definition(); d.ExpValue = 23; d.Appearance.Scale = 1.7f;
        d.Movement = new MonsterMovementParameters { WaitGameSeconds = 0, MoveGameSeconds = 4 };
        d.Sight = new MonsterSightParameters { DisableEmotion = true };
        d.NativeParameters = new MonsterNativeParameters { TrafficLight = new MonsterTrafficLightParameters { HoverSpeedMax = 2 } };
        d.NativeParts.Add(new MonsterNativePartParameters { TemplateId = "part", Health = 400, Defense = 6, HurtDuration = .4f, Scale = .8f });
        d.AttackComposition.Add(new MonsterAttackComposition { SourceTemplate = "source", SourceAttack = "AtkNormal", TargetAttack = "AtkSkill01" });
        d.NativeAttacks.Add(new MonsterNativeAttackParameters { AttackId = "AtkSkill01", Damage = 17 });
        d.Drops = new MonsterDrops { Mode = MonsterDropMode.Append, MinCount = 4, MaxCount = 4, Entries = new List<MonsterDropEntry> { new MonsterDropEntry { ItemId = "iron", Weight = 30 } } };
        var input = new ContentInput { OwnerId = "A" }; input.Definitions.Add(d);
        Check(ContentCompiler.Validate(input, catalog).Count == 0, "Composed target slot accepts source parameters together with independent visual/physical/part inputs.");
        Check(!target.Actions.AttackIds.Contains("AtkSkill01") && source.Actions.NativeAttacks.Single().Fields.Single().Value == 10, "Composition admission never mutates shared target/source metadata.");
        string text = ContentCodec.WriteContent(input); var reopened = ContentCodec.ReadContent(text, "A", _ => Array.Empty<byte>());
        Check(ContentCodec.WriteContent(reopened) == text && ContentCompiler.Validate(reopened, CatalogCodec.Read(CatalogCodec.Write(catalog))).Count == 0, "New fields survive payload and actual catalog roundtrip without changing inheritance.");
        var frozen = d.Copy(); d.NativeParts[0].Health = 1; d.NativeParameters.TrafficLight!.HoverSpeedMax = 99; d.AttackComposition[0].SourceTemplate = "missing"; d.Drops.Entries![0].Weight = 0;
        Check(frozen.NativeParts[0].Health == 400 && frozen.NativeParameters!.TrafficLight!.HoverSpeedMax == 2 && frozen.AttackComposition[0].SourceTemplate == "source" && frozen.Drops.Entries![0].Weight == 30, "Registration copy freezes all nested author input.");
        Check(MonsterAdmission.Definition("A", d, catalog)?.Code == "TemplateMissing", "Missing composition source reports its real boundary.");
        frozen.NativeParameters!.TrafficLight!.HoverSpeedMax = .2f;
        Check(MonsterAdmission.Definition("A", frozen, catalog)?.Field == "nativeParameters.trafficLight.hoverSpeedMax", "One overridden speed endpoint is validated against its inherited endpoint.");
        frozen.NativeParameters.TrafficLight.HoverSpeedMax = 2; frozen.Movement!.MoveGameSeconds = 0;
        Check(MonsterValidation.Validate("A", frozen)?.Field == "movement.moveGameSeconds", "Shuttle rejects zero duration before native division.");

        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 0, (_, _, value) => MonsterOverrideAdmission.Validate("A", value)); registry.Refresh(true, 1);
        MonsterNativeOverride Drop(string id, MonsterDropMode mode, int count) => new MonsterNativeOverride { LocalId = id, TargetId = "bee", Drops = new MonsterDrops { Mode = mode, MinCount = count, MaxCount = count, Entries = new List<MonsterDropEntry> { new MonsterDropEntry { ItemId = "iron" } } } };
        var a = registry.Register("A", 1, Drop("extra", MonsterDropMode.Append, 2)).Value!;
        var b = registry.Register("B", 2, Drop("extra", MonsterDropMode.Append, 3)).Value!;
        var low = registry.Register("C", 3, Drop("replace", MonsterDropMode.Replace, 5)).Value!;
        var high = registry.Register("D", 4, Drop("replace", MonsterDropMode.Replace, 8)).Value!;
        Check(registry.AppendDrops("bee").Select(x => x.MinCount).SequenceEqual(new int?[] { 2, 3 }) && registry.ResolveDrops("bee")!.MinCount == 8, "Both append contributions coexist with the official-order replacement winner.");
        Check(a.Snapshot.Fields.Single().State == MonsterOverrideFieldState.Effective && b.Snapshot.Fields.Single().WinnerId == "B:extra", "Append diagnostics identify their own effective contribution.");
        b.Dispose(); Check(registry.AppendDrops("bee").Single().MinCount == 2 && registry.ResolveDrops("bee")!.MinCount == 8, "Removing an append changes only that owner's contribution.");
        high.Dispose(); Check(registry.ResolveDrops("bee")!.MinCount == 5 && registry.AppendDrops("bee").Count() == 1, "Removing replacement restores prior replacement and preserves all appends.");
        low.Dispose(); Check(registry.ResolveDrops("bee") == null && registry.AppendDrops("bee").Count() == 1, "No replacement restores native inheritance while keeping appends.");
        a.Dispose(); Check(!registry.AppendDrops("bee").Any(), "Final withdrawal leaves no append intent.");
        var own = Drop("creature", MonsterDropMode.Replace, 6); own.TargetId = "A:creature";
        var policy = registry.Register("A", 1, own, definitionPolicy: true).Value!;
        var external = Drop("external", MonsterDropMode.Replace, 9); external.TargetId = own.TargetId;
        var externalLease = registry.Register("B", 2, external).Value!;
        var extra = Drop("extra", MonsterDropMode.Append, 2); extra.TargetId = own.TargetId;
        var extraLease = registry.Register("B", 2, extra).Value!;
        Check(registry.ResolveDrops(own.TargetId)!.MinCount == 9 && registry.AppendDrops(own.TargetId).Single().MinCount == 2 && registry.ResolveDrops("bee") == null, "Qualified definition uses the same replacement/append arbitration without contaminating its template.");
        externalLease.Dispose(); Check(registry.ResolveDrops(own.TargetId)!.MinCount == 6, "Withdrawing an external replacement restores the definition author's replacement.");
        extra.Defense = 1; Check(MonsterOverrideAdmission.Validate("B", extra)?.Code == "DefinitionDropTargetOnly", "Qualified targets do not silently open native private fields.");
        policy.Dispose(); extraLease.Dispose();
    }
}
