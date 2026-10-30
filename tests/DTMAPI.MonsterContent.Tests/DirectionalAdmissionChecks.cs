using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.MonsterContent;

static partial class Program
{
    static void DirectionalAdmissionChecks()
    {
        var catalog = AuthorCatalog();
        void Equivalent(MonsterDefinition input, string? expected = null)
        {
            var full = MonsterAdmission.Definition("Author", input, catalog);
            var selected = MonsterAdmission.Definition("Author", input,
                catalog.Templates.FirstOrDefault(t => t.Id == input.TemplateId),
                catalog.Behaviors.FirstOrDefault(b => b.Id == input.BehaviorId),
                catalog.DropPools.FirstOrDefault(p => p.Reference.Id == input.Drops.PoolId && p.Source == input.Drops.Source));
            string Identity(MonsterDiagnostic? d) => d == null ? "success" : string.Join("|", d.Code, d.Field, d.Stage, d.OwnerId, d.DefinitionId, d.TemplateId, d.Message, d.Recovery);
            Check(Identity(full) == Identity(selected), "Full and selected admission preserve complete diagnostics: " + (expected ?? "success"));
            Check(full?.Code == expected, "Selected admission expected result: " + (expected ?? "success"));
        }
        Equivalent(Definition());
        var code = Definition(); code.Behavior = "code-provider"; code.BehaviorId = "Provider:patrol";
        Equivalent(code);
        code.BehaviorId = "Provider:absent"; Equivalent(code, "BehaviorUnavailable"); code.BehaviorId = "Provider:patrol";
        catalog.Behaviors = new[] { new MonsterBehaviorInfo("Provider:patrol", "1", false, null) };
        Equivalent(code, "BehaviorUnavailable"); catalog = AuthorCatalog();
        catalog.Templates[0].Actions = new MonsterActionCapabilities(new[] { "AtkNormal" }, false, "unsupported mover");
        Equivalent(code, "MovementUnsupported");
        catalog.Templates[0].Actions = new MonsterActionCapabilities(Array.Empty<string>(), true);
        Equivalent(code, "AttackMissing"); catalog = AuthorCatalog();
        var invalid = Definition(); invalid.Health = 0; Equivalent(invalid, "InvalidDefinition");
        invalid = Definition(); invalid.TemplateId = "absent"; Equivalent(invalid, "TemplateMissing");
        var pool = Definition(); pool.Drops = new MonsterDrops { Mode = MonsterDropMode.Replace, PoolId = "effective" };
        Equivalent(pool); pool.Drops.PoolId = "missing"; Equivalent(pool, "DropPoolMissing");
        pool.Drops.Source = MonsterPoolSource.Managed; pool.Drops.PoolId = "Other:missing"; Equivalent(pool, "DropPoolUnavailable");
        var managed = new MonsterDropPoolInfo(new MonsterCatalogEntry("Author:loot", "DropPool", "Author", "1", "p"), MonsterPoolSource.Managed, Array.Empty<MonsterDropEntryInfo>());
        catalog = new MonsterContentCatalog(1, "fixture", "1", "f", catalog.Templates, catalog.Items, new[] { managed }, catalog.Rooms);
        pool.Drops.PoolId = "Author:loot"; Equivalent(pool);
        var closed = new MonsterDropPoolInfo(new MonsterCatalogEntry("Author:loot", "DropPool", "Author", "1", "p", new[] { new MonsterDiagnostic("Closed", "closed") }), MonsterPoolSource.Managed, Array.Empty<MonsterDropEntryInfo>());
        catalog = new MonsterContentCatalog(1, "fixture", "1", "f", catalog.Templates, catalog.Items, new[] { closed }, catalog.Rooms);
        Equivalent(pool, "DropPoolUnavailable");
        catalog = AuthorCatalog();
        var visual = Definition();
        catalog.Templates[0].Presentation = new MonsterPresentationInfo("fixture", new[] { new MonsterFrameInfo("idle", 1, 1, .5f, .5f, 16) });
        visual.Appearance.Skin = new MonsterSkin { TemplateFingerprint = "fixture", Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQIHWP4z8DwHwAFgAI/ScLttAAAAABJRU5ErkJggg=="), Frames = new List<MonsterFrameMapping> { new MonsterFrameMapping { FrameId = "idle", Width = 1, Height = 1 } } };
        Equivalent(visual);
        visual.Appearance.Skin.TemplateFingerprint = "stale"; Equivalent(visual);
        visual.Appearance.Skin.TemplateFingerprint = "fixture"; visual.Appearance.Skin.Frames[0].FrameId = "absent"; Equivalent(visual);
        // Each invocation consumes fresh lease metadata even if static generation
        // and fingerprint are unchanged; no whole-catalog cache is involved.
        catalog = AuthorCatalog(); Equivalent(code);
        catalog.Behaviors = Array.Empty<MonsterBehaviorInfo>(); Equivalent(code, "BehaviorUnavailable");
    }
}
