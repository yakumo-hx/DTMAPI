using System;
using System.Collections.Generic;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static MonsterDefinition DashDefinition()
    {
        var d = ScatterDefinition(); d.LocalId = "dash";
        d.Skills[0] = new MonsterSkill { Id = "fan", Kind = "dash", Dash = new MonsterDash() };
        return d;
    }
    static void DashChecks()
    {
        var catalog = AuthorCatalog(); var d = DashDefinition();
        Check(MonsterAdmission.Definition("A", d, catalog)!.Code == "DashUnsupported", "Movement alone or an older catalog cannot imply dash support.");
        catalog.Templates[0].Actions.Dash = true;
        Check(MonsterAdmission.Definition("A", d, catalog) == null, "Both author entrypoints use explicit dash capability admission.");
        Check(CatalogCodec.Read(CatalogCodec.Write(catalog)).Templates[0].Actions.Dash, "Dash capability survives the catalog boundary.");
        var publisher = new Publisher { Ready = true }; var registry = new DefinitionRegistry(publisher, () => { });
        Check(registry.Register("A", d).Success, "Ordinary DTO registration accepts dash.");
        d.Skills[0].Dash!.Distance = 30;
        Check(publisher.Last!.Definition.Skills[0].Dash!.Distance == 6, "Dash parameters are frozen against later author mutation.");
        foreach (var change in new Action<MonsterDash>[] {
            s => s.Distance = 0, s => s.Speed = float.NaN, s => s.HitStartFraction = -.1f, s => s.HitEndFraction = 1.1f,
            s => s.HitStartFraction = 1, s => { s.HitStartFraction = .8f; s.HitEndFraction = .4f; },
            s => s.Damage = -1, s => s.Range = float.PositiveInfinity, s => s.WindupGameSeconds = -1,
            s => s.CooldownGameSeconds = 0
        }) { var bad = DashDefinition(); change(bad.Skills[0].Dash!); Check(MonsterValidation.Validate("A", bad)!.Code == "InvalidSkill", "Invalid dash physics/window rejected before publication."); }
        d = DashDefinition(); d.Skills[0].Scatter = new MonsterScatter();
        Check(MonsterValidation.Validate("A", d)!.Code == "InvalidSkill", "Mixed skill parameter blocks are rejected in C#.");
        var input = new ContentInput { OwnerId = "A" }; input.Definitions.Add(DashDefinition());
        string payload = ContentCodec.WriteContent(input);
        Check(ContentCodec.WriteContent(ContentCodec.ReadContent(payload, "A", _ => throw new Exception())) == payload, "Every dash field survives the actual payload codec.");
        foreach (var bad in new[] { payload.Replace("\"distance\":6", "\"distance\":6,\"duration\":2"), payload.Replace("\"kind\":\"dash\"", "\"kind\":\"scatter\""), payload.Replace("\"kind\":\"dash\"", "\"kind\":\"dash\",\"scatter\":{}") })
        { try { ContentCodec.ReadContent(bad, "A", _ => throw new Exception()); throw new Exception("Expected rejection"); } catch (AuthoringException) { Check(true, "Unknown fields and cross-kind payloads cannot silently lose intent."); } }
        d = DashDefinition(); d.Skills[0].Dash!.Damage = 0;
        Check(MonsterValidation.Validate("A", d) == null, "Zero-damage dash remains a valid movement skill.");
    }
}
