using System;
using System.Collections.Generic;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;
using DTMAPI.Modules.Monsters;

static partial class Program
{
    static MonsterDefinition ScatterDefinition()
    {
        var d = Definition("scatter"); d.Behavior = "configured-sequence";
        d.Skills.Add(new MonsterSkill { Id = "fan", Scatter = new MonsterScatter { ProjectileAttackId = "AtkNormal" } });
        d.Sequence = new MonsterSequence { Steps = new List<MonsterSequenceStep> { new MonsterSequenceStep { Id = "fire", Action = "skill", SkillId = "fan", MaxGameSeconds = 8 } } };
        return d;
    }
    static void SkillChecks()
    {
        DashChecks();
        ActionAnimationChecks();
        var catalog = AuthorCatalog(); var d = ScatterDefinition();
        Check(MonsterAdmission.Definition("A", d, catalog)!.Code == "ScatterUnsupported", "An old catalog or a nonprojectile attack cannot imply scatter support.");
        catalog.Templates[0].Actions.ScatterAttackIds = Array.AsReadOnly(new[] { "AtkNormal" });
        Check(MonsterAdmission.Definition("A", d, catalog) == null, "Scatter admits a declared template capability through ordinary definition admission.");
        Check(CatalogCodec.Read(CatalogCodec.Write(catalog)).Templates[0].Actions.ScatterAttackIds[0] == "AtkNormal", "Scatter capability survives a real catalog roundtrip.");
        var publisher = new Publisher { Ready = true }; var registry = new DefinitionRegistry(publisher, () => { });
        Check(registry.Register("A", d).Success, "Ordinary DTO registration accepts a scatter definition.");
        d.Skills[0].Scatter!.Damage = 99; d.Skills[0].Id = "changed";
        Check(publisher.Last!.Definition.Skills[0].Id == "fan" && publisher.Last.Definition.Skills[0].Scatter!.Damage == 10, "Accepted skill identity and nested parameters are frozen independently of author mutation.");
        foreach (var change in new Action<MonsterDefinition>[] {
            x => x.Skills[0].Kind = "dash", x => x.Skills[0].Scatter = null, x => x.Skills.Add(x.Skills[0]),
            x => x.Skills[0].Scatter!.SpreadDegrees = float.NaN,
            x => x.Skills[0].Scatter!.BulletSpeed = 0, x => x.Skills[0].Scatter!.BulletLifetimeGameSeconds = 0,
            x => x.Skills[0].Scatter!.Damage = -1, x => x.Skills[0].Scatter!.Range = float.PositiveInfinity,
            x => x.Skills[0].Scatter!.WindupGameSeconds = -1, x => x.Skills[0].Scatter!.CooldownGameSeconds = 0, x => x.Behavior = "inherit-native"
        }) { var bad = ScatterDefinition(); change(bad); Check(MonsterValidation.Validate("A", bad)!.Code == "InvalidSkill", "Invalid skill input is rejected before publication."); }
        d = ScatterDefinition(); d.Skills[0].Scatter!.Damage = 0; d.Skills[0].Scatter!.BulletCount = 1; d.Skills[0].Scatter!.SpreadDegrees = 180;
        Check(MonsterValidation.Validate("A", d) == null, "Zero damage and a single centered bullet are explicit supported inputs.");
        d.Sequence!.Steps[0].SkillId = "missing";
        Check(MonsterValidation.Validate("A", d)!.Field.EndsWith("skillId"), "A missing definition-local skill is rejected with its step field.");
        d.Sequence.Steps[0].SkillId = "fan"; d.Sequence.Steps[0].AttackId = "AtkNormal";
        Check(MonsterValidation.Validate("A", d)!.Code == "InvalidSequence", "A skill step cannot silently carry an unused native attack.");
        var input = new ContentInput { OwnerId = "A" }; input.Definitions.Add(ScatterDefinition());
        var payload = ContentCodec.WriteContent(input);
        Check(ContentCodec.WriteContent(ContentCodec.ReadContent(payload, "A", _ => throw new Exception())) == payload, "Every scatter parameter and skill reference survives runtime serialization.");
        foreach (string bad in new[] { payload.Replace("\"bulletCount\":5", "\"bulletCount\":1.5"), payload.Replace("\"spreadDegrees\":60", "\"spreadDegrees\":60,\"tracking\":true"), payload.Replace("\"skillId\":\"fan\"", "\"skillId\":\"fan\",\"offsetX\":0") })
        { try { ContentCodec.ReadContent(bad, "A", _ => throw new Exception()); throw new Exception("Expected rejection"); } catch (AuthoringException) { Check(true, "Malformed or unknown scatter parameters never silently fall back."); } }
        var a = new CombatContext { Target = 1, Rejection = "SkillCoolingDown" }; var b = new CombatContext { Target = 1 };
        var one = new ConfiguredSequence(ScatterDefinition().Sequence!); var two = new ConfiguredSequence(ScatterDefinition().Sequence!);
        one.Start(a); two.Start(b); one.Tick(1); one.Tick(0); two.Tick(.1f);
        a.Rejection = ""; one.Tick(.1f);
        Check(a.LastLimit == 7 && b.LastLimit == 8, "Skill cooldown waiting consumes only its own step's game-time budget.");
        Check(one.Cancel("Cancelled").Success && a.Cancels == 1 && b.Cancels == 0, "Cancelling a skill goes through its context token without stopping another instance.");
        one.Tick(10); Check(a.Starts == 2, "Cancelled skill decisions cannot emit again.");
        b.ActionState = MonsterActionState.Completed; two.Tick(.1f);
        Check(two.Snapshot.State == MonsterSequenceState.Completed, "Ordinary skill completion advances the existing finite scheduler.");
    }
}
