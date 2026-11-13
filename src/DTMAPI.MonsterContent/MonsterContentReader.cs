using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.MonsterContent
{
    internal static class MonsterContentReader
    {
        internal static MonsterContentData ReadSection(object? section, string owner, Func<string, byte[]> readResource) =>
            ReadSection(section, owner, readResource, new ContentJson((code, field, message) => new System.IO.InvalidDataException(message)));

        internal static MonsterContentData ReadSection(object? section, string owner, Func<string, byte[]> readResource, ContentJson json)
        {
            var root = json.Object(section, "monsters", "", "definitions dropPools spawnRules nativeOverrides");
            var input = new MonsterContentData { OwnerId = owner };
            var context = new ContentReadContext(json, "image",
                (key, field, message, reader) =>
                {
                    ContentImages.ValidateResourcePath(key, reader);
                    byte[] bytes = readResource(key);
                    ContentImages.CheckPng(bytes, reader);
                    if (ContentImages.ImagePath(bytes) != key) throw reader.Error("ResourceHashMismatch", field, message);
                    input.Images[key] = bytes;
                    return bytes;
                },
                ContentSoundReader.ReadEvent,
                (source, reader) =>
                {
                    if (source == "local") throw reader.Error("UnnormalizedReference", "drops.source", "Runtime payload requires owner-qualified managed references.");
                },
                (source, id, reader) => id);
            object Empty(string key) => root.TryGetValue(key, out var value) ? value! : new List<object?>();
            ReadCollections(input, Empty("dropPools"), Empty("spawnRules"), json);
            ReadOverrides(input, root, context);
            foreach (var value in json.Array(Empty("definitions"), "definitions")) input.Definitions.Add(ReadDefinition(value, context));
            return input;
        }
        private static object? Get(Dictionary<string, object?> values, string key) => values.TryGetValue(key, out var value) ? value : null;
        internal static void ReadOverrides(MonsterContentData input, Dictionary<string, object?> root, ContentReadContext context)
        {
            var json = context.Json;
            if (!root.ContainsKey("nativeOverrides")) return;
            foreach (var value in json.Array(root["nativeOverrides"], "nativeOverrides"))
            {
                var row = json.Object(value, "nativeOverrides[]", "id targetId fields");
                var fields = json.Object(row["fields"], "nativeOverrides.fields", "", "defense hurtDuration tint skin drops projectileTint nativeAttacks sounds");
                input.NativeOverrides.Add(new MonsterNativeOverride { LocalId = json.Text(row["id"], "nativeOverrides.id"), TargetId = json.Text(row["targetId"], "nativeOverrides.targetId"),
                    Defense = json.NullableInt(Get(fields, "defense"), "nativeOverrides.fields.defense"),
                    HurtDuration = Get(fields, "hurtDuration") == null ? (float?)null : (float)json.Number(fields["hurtDuration"], "nativeOverrides.fields.hurtDuration"),
                    Tint = Get(fields, "tint") == null ? null : json.Text(fields["tint"], "nativeOverrides.fields.tint"),
                    ProjectileTint = Get(fields, "projectileTint") == null ? null : json.Text(fields["projectileTint"], "nativeOverrides.fields.projectileTint"),
                    Sounds = Get(fields, "sounds") == null ? null : ContentSoundReader.Read(fields["sounds"], context, "fields.sounds"),
                    NativeAttacks = fields.TryGetValue("nativeAttacks", out var attackInput) ? ContentNativeAttackReader.Read(attackInput, context) : new List<MonsterNativeAttackParameters>(),
                    Skin = Get(fields, "skin") == null ? null : ReadSkin(fields["skin"], context),
                    Drops = Get(fields, "drops") == null ? null : ReadDrops(fields["drops"], context) });
            }
        }
        internal static void ReadCollections(MonsterContentData input, object? pools, object? rules, ContentJson json)
        {
            foreach (var value in json.Array(pools, "dropPools"))
            {
                var p = json.Object(value, "dropPools[]", "id entries"); var pool = new MonsterDropPool { LocalId = json.Text(p["id"], "dropPools.id") };
                foreach (var row in json.Array(p["entries"], "entries"))
                {
                    var e = json.Object(row, "entries[]", "itemId weight minCount maxCount");
                    pool.Entries.Add(new MonsterDropEntry { ItemId = json.Text(e["itemId"], "itemId"), Weight = (float)json.Number(e["weight"], "weight"), MinCount = json.Int(e["minCount"], "minCount"), MaxCount = json.NullableInt(e["maxCount"], "maxCount") });
                }
                input.DropPools.Add(pool);
            }
            foreach (var value in json.Array(rules, "spawnRules"))
            {
                var r = json.Object(value, "spawnRules[]", "id definitionId roomId regionId count");
                string definition = json.Text(r["definitionId"], "definitionId");
                input.SpawnRules.Add(new MonsterSpawnRule { LocalId = json.Text(r["id"], "id"), DefinitionId = definition.Contains(":") ? definition : input.OwnerId + ":" + definition,
                    RoomId = json.Text(r["roomId"], "roomId"), RegionId = json.Text(r["regionId"], "regionId"), Count = json.Int(r["count"], "count") });
            }
        }
        private static MonsterSkin ReadSkin(object? value, ContentReadContext context)
        {
            var json = context.Json;
            var s = json.Object(value, "skin", context.ImageField + " templateFingerprint frames");
            string key = json.Text(s[context.ImageField], "skin.image");
            byte[] bytes = context.ReadImage(key, "skin." + context.ImageField, "Image bytes differ from their content-addressed path.");
            var skin = new MonsterSkin { Png = bytes, TemplateFingerprint = json.Text(s["templateFingerprint"], "skin.templateFingerprint") };
            foreach (var row in json.Array(s["frames"], "skin.frames"))
            {
                var f = json.Object(row, "frames[]", "frameId x y width height");
                skin.Frames.Add(new MonsterFrameMapping { FrameId = json.Text(f["frameId"], "frameId"), X = json.Int(f["x"], "x"), Y = json.Int(f["y"], "y"), Width = json.Int(f["width"], "width"), Height = json.Int(f["height"], "height") });
            }
            return skin;
        }
        private static MonsterDrops ReadDrops(object? value, ContentReadContext context)
        {
            var json = context.Json;
            var d = json.Object(value, "drops", "mode minCount maxCount", "source poolId entries");
            string mode = json.Choice(d["mode"], "drops.mode", "inherit", "append", "replace");
            string source = d.ContainsKey("source") ? json.Choice(d["source"], "drops.source", "effective", "managed", "local") : "effective";
            context.CheckDropSource(source);
            var result = new MonsterDrops { Mode = mode == "inherit" ? MonsterDropMode.Inherit : mode == "append" ? MonsterDropMode.Append : MonsterDropMode.Replace,
                Source = source == "effective" ? MonsterPoolSource.Effective : MonsterPoolSource.Managed,
                PoolId = d.ContainsKey("poolId") ? json.Text(d["poolId"], "drops.poolId") : "",
                MinCount = json.NullableInt(d["minCount"], "drops.minCount"), MaxCount = json.NullableInt(d["maxCount"], "drops.maxCount"),
                Entries = d.ContainsKey("entries") ? ReadEntries(d["entries"], json) : null };
            result.PoolId = context.ResolvePool(source, result.PoolId);
            return result;
        }
        private static List<MonsterDropEntry> ReadEntries(object? value, ContentJson json) => json.Array(value, "entries").Select(row => {
            var e = json.Object(row, "entries[]", "itemId weight minCount maxCount");
            return new MonsterDropEntry { ItemId = json.Text(e["itemId"], "itemId"), Weight = (float)json.Number(e["weight"], "weight"), MinCount = json.Int(e["minCount"], "minCount"), MaxCount = json.NullableInt(e["maxCount"], "maxCount") };
        }).ToList();
        internal static MonsterDefinition ReadDefinition(object? value, ContentReadContext context)
        {
            var json = context.Json;
            var m = json.Object(value, "definitions[]", "id templateId overrides behavior drops", "compendium nativeAttacks nativeParts body movement sight sounds nativeParameters attackComposition");
            var d = new MonsterDefinition { LocalId = json.Text(m["id"], "id"), TemplateId = json.Text(m["templateId"], "templateId") };
            if (m.TryGetValue("attackComposition", out var composition)) foreach (var item in json.Array(composition, "attackComposition"))
            { var row = json.Object(item, "attackComposition[]", "sourceTemplate sourceAttack targetAttack"); d.AttackComposition.Add(new MonsterAttackComposition {
                SourceTemplate = json.Text(row["sourceTemplate"], "sourceTemplate"), SourceAttack = json.Text(row["sourceAttack"], "sourceAttack"), TargetAttack = json.Text(row["targetAttack"], "targetAttack") }); }
            d.Body = ContentPhysicalReader.ReadBody(Get(m, "body"), json);
            d.Movement = ContentPhysicalReader.ReadMovement(Get(m, "movement"), json);
            d.Sight = ContentPhysicalReader.ReadSight(Get(m, "sight"), json);
            if (m.ContainsKey("sounds")) d.Sounds = ContentSoundReader.Read(m["sounds"], context);
            if (m.TryGetValue("nativeParameters", out var nativeParameters)) d.NativeParameters = ContentNativeParameterReader.Read(nativeParameters, json);
            if (m.TryGetValue("nativeParts", out var nativeParts)) d.NativeParts = ContentNativePartReader.Read(nativeParts, context);
            if (m.TryGetValue("nativeAttacks", out var nativeAttacks)) d.NativeAttacks = ContentNativeAttackReader.Read(nativeAttacks, context);
            if (m.ContainsKey("compendium"))
            {
                var c = json.Object(m["compendium"], "compendium", "title habitat description category showLocked " + context.ImageField);
                string key = json.Text(c[context.ImageField], "compendium.image");
                byte[] bytes = context.ReadImage(key, "compendium." + context.ImageField, "Preview bytes differ from their content-addressed path.");
                d.Compendium = new MonsterCompendium { Title = json.Text(c["title"], "compendium.title"), Habitat = json.Text(c["habitat"], "compendium.habitat"),
                    Description = json.Text(c["description"], "compendium.description"), ShowLocked = json.Bool(c["showLocked"], "compendium.showLocked"), Png = bytes,
                    Category = (MonsterCompendiumCategory)Enum.Parse(typeof(MonsterCompendiumCategory), json.Choice(c["category"], "compendium.category", "Organism", "Machinery", "Bionics", "Boss")) };
            }
            var o = json.Object(m["overrides"], "overrides", "", "health defense hurtDuration expValue scale tint display skin");
            d.ExpValue = json.NullableInt(Get(o, "expValue"), "expValue");
            d.Appearance.Scale = Get(o, "scale") == null ? (float?)null : (float)json.Number(o["scale"], "scale");
            d.Health = json.NullableInt(Get(o, "health"), "health"); d.Defense = json.NullableInt(Get(o, "defense"), "defense");
            d.HurtDuration = Get(o, "hurtDuration") == null ? (float?)null : (float)json.Number(o["hurtDuration"], "hurtDuration");
            if (o.ContainsKey("tint")) d.Appearance.Tint = json.Text(o["tint"], "tint");
            if (o.ContainsKey("display"))
            {
                var v = json.Object(o["display"], "display", "name nameplate healthBar", "bossBar");
                d.Display.Name = json.Text(v["name"], "display.name"); d.Display.Nameplate = json.Bool(v["nameplate"], "display.nameplate");
                d.Display.BossBar = v.ContainsKey("bossBar") && json.Bool(v["bossBar"], "display.bossBar");
                d.Display.HealthBar = (MonsterHealthBar)Enum.Parse(typeof(MonsterHealthBar), json.Choice(v["healthBar"], "display.healthBar", "Damaged", "Always", "Never"));
            }
            if (Get(o, "skin") != null) d.Appearance.Skin = ReadSkin(o["skin"], context);
            var b = json.Object(m["behavior"], "behavior", "mode", "id sequence combat skills healthPhases"); d.Behavior = json.Choice(b["mode"], "behavior.mode", "inherit-native", "code-provider", "configured-sequence");
            d.BehaviorId = b.ContainsKey("id") ? json.Text(b["id"], "behavior.id") : "";
            if (b.ContainsKey("healthPhases"))
            {
                const string at = "behavior.healthPhases";
                var p = json.Object(b["healthPhases"], at, "transition stages");
                d.HealthPhases = new MonsterHealthPhases { Transition = (MonsterPhaseTransition)Enum.Parse(typeof(MonsterPhaseTransition), json.Choice(p["transition"], at + ".transition", "CancelCurrent", "FinishCurrent")) };
                int index = 0;
                foreach (var row in json.Array(p["stages"], at + ".stages"))
                {
                    string field = at + ".stages[" + index++ + "]";
                    var s = json.Object(row, field, "id atOrBelow speedMultiplier damageMultiplier");
                    d.HealthPhases.Stages.Add(new MonsterHealthPhase { Id = json.Text(s["id"], field + ".id"), AtOrBelow = (float)json.Number(s["atOrBelow"], field + ".atOrBelow"),
                        SpeedMultiplier = (float)json.Number(s["speedMultiplier"], field + ".speedMultiplier"), DamageMultiplier = (float)json.Number(s["damageMultiplier"], field + ".damageMultiplier") });
                }
            }
            if (b.ContainsKey("sequence")) d.Sequence = ReadSequence(b["sequence"], json);
            if (b.ContainsKey("skills"))
            {
                int i = 0;
                foreach (var row in json.Array(b["skills"], "behavior.skills"))
                {
                    string at = "behavior.skills[" + i++ + "]";
                    var skill = json.Object(row, at, "id kind", "scatter dash animation");
                    string kind = json.Choice(skill["kind"], at + ".kind", "scatter", "dash");
                    json.Object(row, at, "id kind " + kind, "animation");
                    MonsterActionAnimation? animation = null;
                    if (skill.ContainsKey("animation"))
                    {
                        var a = json.Object(skill["animation"], at + ".animation", context.ImageField + " frameWidth frameHeight pivotX pivotY pixelsPerUnit windupFrames recoveryFrames");
                        string key = json.Text(a[context.ImageField], at + ".animation.image");
                        byte[] bytes = context.ReadImage(key, at + ".animation." + context.ImageField, "Image bytes differ from their content-addressed path.");
                        animation = new MonsterActionAnimation { Png = bytes, FrameWidth = json.Int(a["frameWidth"], at + ".animation.frameWidth"), FrameHeight = json.Int(a["frameHeight"], at + ".animation.frameHeight"),
                            PivotX = (float)json.Number(a["pivotX"], at + ".animation.pivotX"), PivotY = (float)json.Number(a["pivotY"], at + ".animation.pivotY"), PixelsPerUnit = (float)json.Number(a["pixelsPerUnit"], at + ".animation.pixelsPerUnit"),
                            WindupFrames = json.Array(a["windupFrames"], at + ".animation.windupFrames").Select(v => json.Int(v, at + ".animation.windupFrames[]")).ToList(),
                            RecoveryFrames = json.Array(a["recoveryFrames"], at + ".animation.recoveryFrames").Select(v => json.Int(v, at + ".animation.recoveryFrames[]")).ToList() };
                    }
                    if (kind == "dash")
                    {
                        var dash = json.Object(skill["dash"], at + ".dash", "distance speed hitStartFraction hitEndFraction damage range windupGameSeconds recoveryGameSeconds cooldownGameSeconds");
                        float D(string key) => (float)json.Number(dash[key], at + ".dash." + key);
                        d.Skills.Add(new MonsterSkill { Id = json.Text(skill["id"], at + ".id"), Kind = kind, Animation = animation, Dash = new MonsterDash {
                            Distance = D("distance"), Speed = D("speed"), HitStartFraction = D("hitStartFraction"), HitEndFraction = D("hitEndFraction"),
                            Damage = D("damage"), Range = D("range"), WindupGameSeconds = D("windupGameSeconds"), RecoveryGameSeconds = D("recoveryGameSeconds"), CooldownGameSeconds = D("cooldownGameSeconds")
                        } });
                        continue;
                    }
                    var s = json.Object(skill["scatter"], at + ".scatter", "projectileAttackId bulletCount spreadDegrees bulletSpeed bulletLifetimeGameSeconds damage range windupGameSeconds recoveryGameSeconds cooldownGameSeconds");
                    float N(string key) => (float)json.Number(s[key], at + ".scatter." + key);
                    d.Skills.Add(new MonsterSkill { Id = json.Text(skill["id"], at + ".id"), Kind = json.Choice(skill["kind"], at + ".kind", "scatter"), Animation = animation, Scatter = new MonsterScatter {
                        ProjectileAttackId = json.Text(s["projectileAttackId"], at + ".scatter.projectileAttackId"), BulletCount = json.Int(s["bulletCount"], at + ".scatter.bulletCount"),
                        SpreadDegrees = N("spreadDegrees"), BulletSpeed = N("bulletSpeed"), BulletLifetimeGameSeconds = N("bulletLifetimeGameSeconds"), Damage = N("damage"), Range = N("range"),
                        WindupGameSeconds = N("windupGameSeconds"), RecoveryGameSeconds = N("recoveryGameSeconds"), CooldownGameSeconds = N("cooldownGameSeconds")
                    } });
                }
            }
            if (b.ContainsKey("combat"))
            {
                var c = json.Object(b["combat"], "behavior.combat", "mode checkIntervalGameSeconds repeatIntervalGameSeconds", "targetWaitGameSeconds");
                d.Combat = new MonsterCombatPolicy {
                    Mode = json.Choice(c["mode"], "behavior.combat.mode", "active", "retaliate", "passive"),
                    CheckIntervalGameSeconds = (float)json.Number(c["checkIntervalGameSeconds"], "behavior.combat.checkIntervalGameSeconds"),
                    RepeatIntervalGameSeconds = (float)json.Number(c["repeatIntervalGameSeconds"], "behavior.combat.repeatIntervalGameSeconds"),
                    TargetWaitGameSeconds = c.ContainsKey("targetWaitGameSeconds") ? (float)json.Number(c["targetWaitGameSeconds"], "behavior.combat.targetWaitGameSeconds") : 5
                };
            }
            d.Drops = ReadDrops(m["drops"], context);
            return d;
        }
        private static MonsterSequence ReadSequence(object? value, ContentJson json)
        {
            const string at = "behavior.sequence";
            var data = json.Object(value, at, "repeatCount steps");
            var sequence = new MonsterSequence { RepeatCount = json.Int(data["repeatCount"], at + ".repeatCount") };
            int i = 0;
            foreach (var row in json.Array(data["steps"], at + ".steps"))
            {
                string field = at + ".steps[" + i++ + "]";
                var s = json.Object(row, field, "id action maxGameSeconds", "attackId skillId offsetX offsetY maxTravel");
                string action = json.Choice(s["action"], field + ".action", "attack", "move-to", "skill");
                json.Object(row, field, action == "attack" ? "id action maxGameSeconds attackId" : action == "skill" ? "id action maxGameSeconds skillId" : "id action maxGameSeconds offsetX offsetY maxTravel");
                var step = new MonsterSequenceStep { Id = json.Text(s["id"], field + ".id"), Action = action, MaxGameSeconds = (float)json.Number(s["maxGameSeconds"], field + ".maxGameSeconds") };
                if (action == "attack") step.AttackId = json.Text(s["attackId"], field + ".attackId");
                else if (action == "skill") step.SkillId = json.Text(s["skillId"], field + ".skillId");
                else { step.OffsetX = (float)json.Number(s["offsetX"], field + ".offsetX"); step.OffsetY = (float)json.Number(s["offsetY"], field + ".offsetY"); step.MaxTravel = (float)json.Number(s["maxTravel"], field + ".maxTravel"); }
                sequence.Steps.Add(step);
            }
            return sequence;
        }
    }
}
