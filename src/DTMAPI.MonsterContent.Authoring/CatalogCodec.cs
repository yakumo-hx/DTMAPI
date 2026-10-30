using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MonsterContent.Authoring
{
    public static class CatalogCodec
    {
        public static object DiagnosticData(MonsterDiagnostic d) => Json.Map("code", d.Code, "message", d.Message, "ownerId", d.OwnerId, "definitionId", d.DefinitionId, "templateId", d.TemplateId, "stage", d.Stage, "field", d.Field, "recovery", d.Recovery);
        private static MonsterDiagnostic Diagnostic(object? value)
        {
            var d = Json.Object(value, "diagnostic", "code message ownerId definitionId templateId stage field recovery");
            string S(string k) => Json.Text(d[k], k);
            return new MonsterDiagnostic(S("code"), S("message"), S("ownerId"), S("definitionId"), S("templateId"), S("stage"), S("field"), S("recovery"));
        }
        private static object ReferenceData(MonsterCatalogEntry r) => Json.Map("id", r.Id, "kind", r.Kind, "source", r.Source, "version", r.Version, "fingerprint", r.Fingerprint, "diagnostics", r.Diagnostics.Select(DiagnosticData).ToArray());
        private static MonsterCatalogEntry Reference(object? value)
        {
            var r = Json.Object(value, "reference", "id kind source version fingerprint diagnostics");
            string S(string k) => Json.Text(r[k], k);
            return new MonsterCatalogEntry(S("id"), S("kind"), S("source"), S("version"), S("fingerprint"), Json.Array(r["diagnostics"], "diagnostics").Select(Diagnostic));
        }
        public static string Write(MonsterContentCatalog catalog) => Json.Write(Json.Map(
            "format", "dtmapi.monster-catalog", "version", 1, "generation", catalog.Generation, "gameVersion", catalog.GameVersion, "moduleVersion", catalog.ModuleVersion, "fingerprint", catalog.Fingerprint,
            "observation", Json.Map("saveSessionEpoch", catalog.Observation.SaveSessionEpoch, "worldReady", catalog.Observation.WorldReady),
            "templates", catalog.Templates.Select(t => Json.Map("id", t.Id, "canInherit", t.CanInherit, "runtimeVerified", t.RuntimeVerified, "body", t.Body, "ai", t.EffectiveAi, "health", t.Health, "defense", t.Defense,
                "nativeParameters", NativeParameterCodec.Write(t.NativeParameters), "expValue", t.ExpValue, "hurtDuration", t.HurtDuration, "editableFields", t.EditableFields, "physical", PhysicalCodec.Info(t.Physical), "soundEvents", t.SoundEvents, "hummingLoop", t.HummingLoop,
                "generationType", t.GenerationType, "volume", t.Volume, "diagnostics", t.Diagnostics.Select(DiagnosticData).ToArray(), "nativeOverrideFields", t.NativeOverrideFields,
                "actions", Json.Map("attackIds", t.Actions.AttackIds, "scatterAttackIds", t.Actions.ScatterAttackIds, "nativeAttacks", t.Actions.NativeAttacks.Select(NativeAttackCodec.Info).ToArray(), "nativeParts", t.Actions.NativeParts.Select(NativePartCodec.Info).ToArray(), "dash", t.Actions.Dash, "dashReason", t.Actions.DashReason, "actionAnimation", t.Actions.ActionAnimation, "phasedNativeActions", t.Actions.PhasedNativeActions, "movement", t.Actions.ControlledMovement, "movementReason", t.Actions.MovementReason, "targetRequirement", t.Actions.TargetRequirement),
                "presentation", Json.Map("fingerprint", t.Presentation.Fingerprint, "limitation", t.Presentation.Limitation, "frames", t.Presentation.Frames.Select(f => Json.Map("id", f.Id, "width", f.Width, "height", f.Height, "pivotX", f.PivotX, "pivotY", f.PivotY, "ppu", f.PixelsPerUnit)).ToArray()))).ToArray(),
            "items", catalog.Items.Select(ReferenceData).ToArray(),
            "dropPools", catalog.DropPools.Select(p => Json.Map("reference", ReferenceData(p.Reference), "source", p.Source.ToString(), "entries", p.Entries.Select(e => Json.Map("itemId", e.ItemId, "weight", e.Weight, "minCount", e.MinCount, "maxCount", e.MaxCount)).ToArray())).ToArray(),
            "rooms", catalog.Rooms.Select(r => Json.Map("reference", ReferenceData(r.Reference), "hostAvailable", r.HostAvailable, "regions", r.Regions.Select(g => Json.Map("id", g.Id, "generationType", g.GenerationType, "capacity", g.Capacity)).ToArray())).ToArray(),
            "behaviors", catalog.Behaviors.Select(b => Json.Map("id", b.Id, "version", b.Version, "available", b.Available, "requirements", b.Requirements == null ? null : Json.Map("movement", b.Requirements.Movement, "attackIds", b.Requirements.AttackIds))).ToArray()));

        public static MonsterContentCatalog Read(string text)
        {
            var c = Json.Object(Json.Read(text), "$", "format version generation gameVersion moduleVersion fingerprint observation templates items dropPools rooms behaviors");
            if (Json.Text(c["format"], "format") != "dtmapi.monster-catalog" || Json.Int(c["version"], "version") != 1) throw new AuthoringException("UnsupportedVersion", "version", "Unsupported catalog.");
            long generation = (long)Json.Number(c["generation"], "generation"); string game = Json.Text(c["gameVersion"], "gameVersion");
            var templates = new List<MonsterTemplate>();
            foreach (var value in Json.Array(c["templates"], "templates"))
            {
                var t = Json.Object(value, "template", "id canInherit runtimeVerified body ai health defense generationType volume diagnostics actions presentation", "nativeOverrideFields hurtDuration expValue editableFields physical soundEvents hummingLoop nativeParameters");
                string S(string k) => Json.Text(t[k], k);
                var template = new MonsterTemplate(S("id"), generation, Json.Bool(t["canInherit"], "canInherit"), Json.Bool(t["runtimeVerified"], "runtimeVerified"), S("body"), S("ai"), Json.Int(t["health"], "health"), Json.Int(t["defense"], "defense"), game, Json.Array(t["diagnostics"], "diagnostics").Select(Diagnostic));
                if (t.TryGetValue("nativeParameters", out var nativeParameters)) template.NativeParameters = NativeParameterCodec.Read(nativeParameters);
                template.ExpValue = t.TryGetValue("expValue", out var exp) ? Json.NullableInt(exp, "expValue") : null;
                template.GenerationType = S("generationType"); template.Volume = Json.Int(t["volume"], "volume");
                if (t.TryGetValue("hummingLoop", out var hummingLoop)) template.HummingLoop = hummingLoop == null ? (bool?)null : Json.Bool(hummingLoop, "hummingLoop");
                if (t.TryGetValue("soundEvents", out var soundEvents)) template.SoundEvents = Array.AsReadOnly(Json.Array(soundEvents, "soundEvents").Select(v => Json.Choice(v, "soundEvents[]", "hurt", "dead", "humming", "recoveryWarning")).ToArray());
                if (t.TryGetValue("physical", out var physical)) template.Physical = PhysicalCodec.ReadInfo(physical);
                if (t.TryGetValue("hurtDuration", out var hurt) && hurt != null) template.HurtDuration = (float)Json.Number(hurt, "hurtDuration");
                if (t.ContainsKey("nativeOverrideFields")) template.NativeOverrideFields = Array.AsReadOnly(Json.Array(t["nativeOverrideFields"], "nativeOverrideFields").Select(v => Json.Choice(v, "nativeOverrideFields[]", "defense", "hurtDuration", "tint", "skin", "drops", "projectileTint", "nativeAttacks", "sounds.hurt", "sounds.dead", "sounds.humming", "sounds.recoveryWarning")).ToArray());
                var a = Json.Object(t["actions"], "actions", "attackIds movement movementReason targetRequirement", "scatterAttackIds dash dashReason actionAnimation phasedNativeActions nativeAttacks nativeParts");
                template.Actions = new MonsterActionCapabilities(Json.Array(a["attackIds"], "attackIds").Select(v => Json.Text(v, "attackIds[]")), Json.Bool(a["movement"], "movement"), Json.Text(a["movementReason"], "movementReason"), Json.Text(a["targetRequirement"], "targetRequirement"));
                if (a.TryGetValue("nativeParts", out var parts)) template.Actions.NativeParts = NativePartCodec.ReadInfo(parts);
                if (a.TryGetValue("nativeAttacks", out var attacks)) template.Actions.NativeAttacks = NativeAttackCodec.ReadInfo(attacks);
                if (a.ContainsKey("scatterAttackIds")) template.Actions.ScatterAttackIds = Array.AsReadOnly(Json.Array(a["scatterAttackIds"], "scatterAttackIds").Select(v => Json.Text(v, "scatterAttackIds[]")).ToArray());
                if (a.ContainsKey("dash")) template.Actions.Dash = Json.Bool(a["dash"], "dash");
                if (a.ContainsKey("actionAnimation")) template.Actions.ActionAnimation = Json.Bool(a["actionAnimation"], "actionAnimation");
                if (a.ContainsKey("phasedNativeActions")) template.Actions.PhasedNativeActions = Json.Bool(a["phasedNativeActions"], "phasedNativeActions");
                if (a.ContainsKey("dashReason")) template.Actions.DashReason = Json.Text(a["dashReason"], "dashReason");
                var p = Json.Object(t["presentation"], "presentation", "fingerprint limitation frames");
                var frames = Json.Array(p["frames"], "frames").Select(v => { var f = Json.Object(v, "frame", "id width height pivotX pivotY ppu"); return new MonsterFrameInfo(Json.Text(f["id"], "id"), Json.Int(f["width"], "width"), Json.Int(f["height"], "height"), (float)Json.Number(f["pivotX"], "pivotX"), (float)Json.Number(f["pivotY"], "pivotY"), (float)Json.Number(f["ppu"], "ppu")); });
                template.Presentation = new MonsterPresentationInfo(Json.Text(p["fingerprint"], "fingerprint"), frames, Json.Text(p["limitation"], "limitation")); templates.Add(template);
                if (t.TryGetValue("editableFields", out var editable) && !Json.Array(editable, "editableFields").Select(v => Json.Text(v, "editableFields[]")).SequenceEqual(template.EditableFields))
                    throw new AuthoringException("CatalogCapabilityMismatch", "editableFields", "Editable fields disagree with this template's capabilities; export the current Host catalog.");
            }
            var pools = Json.Array(c["dropPools"], "dropPools").Select(v =>
            {
                var p = Json.Object(v, "pool", "reference source entries");
                return new MonsterDropPoolInfo(Reference(p["reference"]), (MonsterPoolSource)Enum.Parse(typeof(MonsterPoolSource), Json.Choice(p["source"], "source", "Effective", "Managed")),
                    Json.Array(p["entries"], "entries").Select(row => { var e = Json.Object(row, "entry", "itemId weight minCount maxCount"); return new MonsterDropEntryInfo(Json.Text(e["itemId"], "itemId"), (float)Json.Number(e["weight"], "weight"), Json.Int(e["minCount"], "minCount"), Json.NullableInt(e["maxCount"], "maxCount")); }));
            });
            var rooms = Json.Array(c["rooms"], "rooms").Select(v =>
            {
                var r = Json.Object(v, "room", "reference hostAvailable regions");
                return new MonsterRoomInfo(Reference(r["reference"]), Json.Array(r["regions"], "regions").Select(row => { var g = Json.Object(row, "region", "id generationType capacity"); return new MonsterSpawnRegion(Json.Text(g["id"], "id"), Json.Text(g["generationType"], "generationType"), Json.Int(g["capacity"], "capacity")); }), Json.Bool(r["hostAvailable"], "hostAvailable"));
            });
            var result = new MonsterContentCatalog(generation, game, Json.Text(c["moduleVersion"], "moduleVersion"), Json.Text(c["fingerprint"], "fingerprint"), templates, Json.Array(c["items"], "items").Select(Reference), pools, rooms);
            result.Behaviors = Json.Array(c["behaviors"], "behaviors").Select(v =>
            {
                var b = Json.Object(v, "behavior", "id version available requirements"); MonsterBehaviorRequirements? requirements = null;
                if (b["requirements"] != null) { var r = Json.Object(b["requirements"], "requirements", "movement attackIds"); requirements = new MonsterBehaviorRequirements { Movement = Json.Bool(r["movement"], "movement"), AttackIds = Json.Array(r["attackIds"], "attackIds").Select(a => Json.Text(a, "attackIds[]")).ToList() }; }
                return new MonsterBehaviorInfo(Json.Text(b["id"], "id"), Json.Text(b["version"], "version"), Json.Bool(b["available"], "available"), requirements);
            }).ToArray();
            var observation = Json.Object(c["observation"], "observation", "saveSessionEpoch worldReady");
            result.Observation = new MonsterCatalogObservation(observation["saveSessionEpoch"] == null ? (long?)null : (long)Json.Number(observation["saveSessionEpoch"], "saveSessionEpoch"), Json.Bool(observation["worldReady"], "worldReady"));
            return result;
        }
        public static MonsterContentCatalog WithPools(MonsterContentCatalog c, IEnumerable<MonsterDropPoolInfo> pools) => new MonsterContentCatalog(c.Generation, c.GameVersion, c.ModuleVersion, c.Fingerprint, c.Templates, c.Items, pools, c.Rooms) { Behaviors = c.Behaviors, Observation = c.Observation };
    }
}
