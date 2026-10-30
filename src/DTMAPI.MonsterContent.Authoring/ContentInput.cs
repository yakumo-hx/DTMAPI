using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DTMAPI.MonsterContent.Authoring
{
    public sealed class ContentDependency
    {
        public string Id { get; set; } = "";
        public string MinimumInclusive { get; set; } = "0.1.0";
        public string? MaximumExclusive { get; set; }
        public bool IncludePrerelease { get; set; }
    }
    public sealed class ContentInput
    {
        public string OwnerId { get; set; } = "";
        public List<MonsterDefinition> Definitions { get; } = new List<MonsterDefinition>();
        public List<MonsterNativeOverride> NativeOverrides { get; } = new List<MonsterNativeOverride>();
        public List<MonsterDropPool> DropPools { get; } = new List<MonsterDropPool>();
        public List<MonsterSpawnRule> SpawnRules { get; } = new List<MonsterSpawnRule>();
        public List<ContentDependency> Dependencies { get; } = new List<ContentDependency>();
        public Dictionary<string, byte[]> Images { get; } = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        public Dictionary<string, byte[]> Audio { get; } = new Dictionary<string, byte[]>(StringComparer.Ordinal);
    }
    public static partial class ContentCodec
    {
        public static string Hash(byte[] bytes) { using var hash = SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        public static string ImagePath(byte[] bytes) => "images/" + Hash(bytes) + ".png";
        public static string AudioPath(byte[] bytes) => "audio/" + Hash(bytes) + ".wav";
        private static object? Get(Dictionary<string, object?> d, string key) => d.TryGetValue(key, out var v) ? v : null;
        public static ContentInput ReadProject(string text)
        {
            var p = Json.Object(Json.Read(text), "$", "format version pack monsters dropPools spawnRules assets dependencies", "catalog nativeOverrides audioAssets");
            if (Json.Text(p["format"], "format") != "dtmapi.monster-project" || Json.Int(p["version"], "version") != 4)
                throw new AuthoringException("UnsupportedVersion", "version", "Only project v4 compiles. Preserve the source; legacy drafts and priority are not supported.");
            var pack = Json.Object(p["pack"], "pack", "id name", "author version");
            Json.Text(pack["name"], "pack.name");
            foreach (string key in new[] { "author", "version" })
                if (pack.ContainsKey(key)) Json.Text(pack[key], "pack." + key);
            var input = new ContentInput { OwnerId = Json.Text(pack["id"], "pack.id") };
            if (p.ContainsKey("catalog") && p["catalog"] != null)
            {
                var c = Json.Object(p["catalog"], "catalog", "gameVersion moduleVersion fingerprint");
                foreach (string key in c.Keys) Json.Text(c[key], "catalog." + key);
            }
            var assets = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var value in Json.Array(p["assets"], "assets"))
            {
                var a = Json.Object(value, "assets[]", "id name dataUrl width height"); string id = Json.Text(a["id"], "assets.id");
                Json.Text(a["name"], "assets.name"); string data = Json.Text(a["dataUrl"], "assets.dataUrl");
                if (!data.StartsWith("data:image/png;base64,", StringComparison.Ordinal)) throw new AuthoringException("InvalidImage", "assets.dataUrl", "Expected embedded PNG.");
                byte[] bytes; try { bytes = Convert.FromBase64String(data.Substring(22)); } catch (FormatException) { throw new AuthoringException("InvalidImage", "assets.dataUrl", "Invalid base64."); }
                CheckPng(bytes, Json.Int(a["width"], "assets.width"), Json.Int(a["height"], "assets.height"));
                if (!MonsterAdmission.ValidLocalId(id) || assets.ContainsKey(id)) throw new AuthoringException("DuplicateId", "assets.id", "Invalid or duplicate image ID.");
                assets.Add(id, bytes); input.Images[ImagePath(bytes)] = bytes;
            }
            ReadCollections(input, p["dropPools"], p["spawnRules"], p["dependencies"]);
            var audio = p.ContainsKey("audioAssets") ? SoundCodec.ReadAssets(p["audioAssets"], input) : new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var value in Json.Array(p["monsters"], "monsters")) input.Definitions.Add(ReadDefinition(value, input.OwnerId, assets, audio, false));
            ReadOverrides(input, p, assets, audio, false);
            return input;
        }
        public static ContentInput ReadSection(object? section, string owner, Func<string, byte[]> readResource)
        {
            var p = Json.Object(section, "monsters", "", "definitions dropPools spawnRules nativeOverrides");
            var input = new ContentInput { OwnerId = owner };
            object Empty(string key) => p.TryGetValue(key, out var value) ? value! : new List<object?>();
            ReadCollections(input, Empty("dropPools"), Empty("spawnRules"), new List<object?>());
            ReadOverrides(input, p, input.Images, input.Audio, true, readResource);
            foreach (var value in Json.Array(Empty("definitions"), "definitions"))
                input.Definitions.Add(ReadDefinition(value, owner, input.Images, input.Audio, true, readResource));
            return input;
        }
        private static void ReadOverrides(ContentInput input, Dictionary<string, object?> root, IDictionary<string, byte[]> images, IDictionary<string, byte[]> audio, bool payload, Func<string, byte[]>? readImage = null)
        {
            if (!root.ContainsKey("nativeOverrides")) return;
            foreach (var value in Json.Array(root["nativeOverrides"], "nativeOverrides"))
            {
                var row = Json.Object(value, "nativeOverrides[]", "id targetId fields");
                var fields = Json.Object(row["fields"], "nativeOverrides.fields", "", "defense hurtDuration tint skin drops projectileTint nativeAttacks sounds");
                input.NativeOverrides.Add(new MonsterNativeOverride { LocalId = Json.Text(row["id"], "nativeOverrides.id"), TargetId = Json.Text(row["targetId"], "nativeOverrides.targetId"),
                    Defense = Json.NullableInt(Get(fields, "defense"), "nativeOverrides.fields.defense"),
                    HurtDuration = Get(fields, "hurtDuration") == null ? (float?)null : (float)Json.Number(fields["hurtDuration"], "nativeOverrides.fields.hurtDuration"),
                    Tint = Get(fields, "tint") == null ? null : Json.Text(fields["tint"], "nativeOverrides.fields.tint"),
                    ProjectileTint = Get(fields, "projectileTint") == null ? null : Json.Text(fields["projectileTint"], "nativeOverrides.fields.projectileTint"),
                    Sounds = Get(fields, "sounds") == null ? null : SoundCodec.Read(fields["sounds"], audio, payload, readImage, "fields.sounds"),
                    NativeAttacks = fields.TryGetValue("nativeAttacks", out var attackInput) ? NativeAttackCodec.Read(attackInput, audio, payload, readImage) : new List<MonsterNativeAttackParameters>(),
                    Skin = Get(fields, "skin") == null ? null : ReadSkin(fields["skin"], images, payload, readImage),
                    Drops = Get(fields, "drops") == null ? null : ReadDrops(fields["drops"], input.OwnerId, payload) });
            }
        }
        private static void ReadCollections(ContentInput input, object? pools, object? rules, object? dependencies)
        {
            foreach (var value in Json.Array(pools, "dropPools"))
            {
                var p = Json.Object(value, "dropPools[]", "id entries"); var pool = new MonsterDropPool { LocalId = Json.Text(p["id"], "dropPools.id") };
                foreach (var row in Json.Array(p["entries"], "entries"))
                {
                    var e = Json.Object(row, "entries[]", "itemId weight minCount maxCount");
                    pool.Entries.Add(new MonsterDropEntry { ItemId = Json.Text(e["itemId"], "itemId"), Weight = (float)Json.Number(e["weight"], "weight"), MinCount = Json.Int(e["minCount"], "minCount"), MaxCount = Json.NullableInt(e["maxCount"], "maxCount") });
                }
                input.DropPools.Add(pool);
            }
            foreach (var value in Json.Array(rules, "spawnRules"))
            {
                var r = Json.Object(value, "spawnRules[]", "id definitionId roomId regionId count");
                string definition = Json.Text(r["definitionId"], "definitionId");
                input.SpawnRules.Add(new MonsterSpawnRule { LocalId = Json.Text(r["id"], "id"), DefinitionId = definition.Contains(":") ? definition : input.OwnerId + ":" + definition,
                    RoomId = Json.Text(r["roomId"], "roomId"), RegionId = Json.Text(r["regionId"], "regionId"), Count = Json.Int(r["count"], "count") });
            }
            foreach (var value in Json.Array(dependencies, "dependencies"))
            {
                var d = Json.Object(value, "dependencies[]", "id minimumInclusive maximumExclusive includePrerelease");
                input.Dependencies.Add(new ContentDependency { Id = Json.Text(d["id"], "dependencies.id"), MinimumInclusive = Json.Text(d["minimumInclusive"], "minimumInclusive"),
                    MaximumExclusive = d["maximumExclusive"] == null ? null : Json.Text(d["maximumExclusive"], "maximumExclusive"), IncludePrerelease = Json.Bool(d["includePrerelease"], "includePrerelease") });
            }
        }
        private static MonsterSkin ReadSkin(object? value, IDictionary<string, byte[]> images, bool payload, Func<string, byte[]>? readImage)
        {
            var s = Json.Object(value, "skin", payload ? "image templateFingerprint frames" : "assetId templateFingerprint frames");
            string key = Json.Text(s[payload ? "image" : "assetId"], "skin.image"); byte[] bytes;
            if (payload) { ValidateResourcePath(key); bytes = readImage!(key); CheckPng(bytes); if (ImagePath(bytes) != key) throw new AuthoringException("ResourceHashMismatch", "skin.image", "Image bytes differ from their content-addressed path."); images[key] = bytes; }
            else if (!images.TryGetValue(key, out bytes!)) throw new AuthoringException("ResourceMissing", "skin.assetId", "Image ID does not exist.");
            var skin = new MonsterSkin { Png = bytes, TemplateFingerprint = Json.Text(s["templateFingerprint"], "skin.templateFingerprint") };
            foreach (var row in Json.Array(s["frames"], "skin.frames"))
            {
                var f = Json.Object(row, "frames[]", "frameId x y width height");
                skin.Frames.Add(new MonsterFrameMapping { FrameId = Json.Text(f["frameId"], "frameId"), X = Json.Int(f["x"], "x"), Y = Json.Int(f["y"], "y"), Width = Json.Int(f["width"], "width"), Height = Json.Int(f["height"], "height") });
            }
            return skin;
        }
        private static MonsterDrops ReadDrops(object? value, string owner, bool payload)
        {
            var d = Json.Object(value, "drops", "mode minCount maxCount", "source poolId entries");
            string mode = Json.Choice(d["mode"], "drops.mode", "inherit", "append", "replace");
            string source = d.ContainsKey("source") ? Json.Choice(d["source"], "drops.source", "effective", "managed", "local") : "effective";
            if (payload && source == "local") throw new AuthoringException("UnnormalizedReference", "drops.source", "Runtime payload requires owner-qualified managed references.");
            var result = new MonsterDrops { Mode = mode == "inherit" ? MonsterDropMode.Inherit : mode == "append" ? MonsterDropMode.Append : MonsterDropMode.Replace,
                Source = source == "effective" ? MonsterPoolSource.Effective : MonsterPoolSource.Managed,
                PoolId = d.ContainsKey("poolId") ? Json.Text(d["poolId"], "drops.poolId") : "",
                MinCount = Json.NullableInt(d["minCount"], "drops.minCount"), MaxCount = Json.NullableInt(d["maxCount"], "drops.maxCount"),
                Entries = d.ContainsKey("entries") ? ReadEntries(d["entries"]) : null };
            if (source == "local") { if (!MonsterAdmission.ValidLocalId(result.PoolId)) throw new AuthoringException("InvalidDrops", "drops.poolId", "Expected local pool ID."); result.PoolId = owner + ":" + result.PoolId; }
            return result;
        }
        private static object DropsData(MonsterDrops d)
        {
            var row = Json.Map("mode", d.Mode.ToString().ToLowerInvariant(), "minCount", d.MinCount, "maxCount", d.MaxCount);
            if (d.Entries != null) row["entries"] = d.Entries.Select(e => Json.Map("itemId", e.ItemId, "weight", e.Weight, "minCount", e.MinCount, "maxCount", e.MaxCount)).ToArray();
            else if (d.Mode != MonsterDropMode.Inherit) { row["source"] = d.Source.ToString().ToLowerInvariant(); row["poolId"] = d.PoolId; }
            return row;
        }
        private static List<MonsterDropEntry> ReadEntries(object? value) => Json.Array(value, "entries").Select(row => {
            var e = Json.Object(row, "entries[]", "itemId weight minCount maxCount");
            return new MonsterDropEntry { ItemId = Json.Text(e["itemId"], "itemId"), Weight = (float)Json.Number(e["weight"], "weight"), MinCount = Json.Int(e["minCount"], "minCount"), MaxCount = Json.NullableInt(e["maxCount"], "maxCount") };
        }).ToList();
        private static MonsterDefinition ReadDefinition(object? value, string owner, IDictionary<string, byte[]> images, IDictionary<string, byte[]> audio, bool payload, Func<string, byte[]>? readImage = null)
        {
            var m = Json.Object(value, "definitions[]", "id templateId overrides behavior drops", "compendium nativeAttacks nativeParts body movement sight sounds nativeParameters attackComposition");
            var d = new MonsterDefinition { LocalId = Json.Text(m["id"], "id"), TemplateId = Json.Text(m["templateId"], "templateId") };
            if (m.TryGetValue("attackComposition", out var composition)) foreach (var item in Json.Array(composition, "attackComposition"))
            { var row = Json.Object(item, "attackComposition[]", "sourceTemplate sourceAttack targetAttack"); d.AttackComposition.Add(new MonsterAttackComposition {
                SourceTemplate = Json.Text(row["sourceTemplate"], "sourceTemplate"), SourceAttack = Json.Text(row["sourceAttack"], "sourceAttack"), TargetAttack = Json.Text(row["targetAttack"], "targetAttack") }); }
            d.Body = PhysicalCodec.ReadBody(Get(m, "body"));
            d.Movement = PhysicalCodec.ReadMovement(Get(m, "movement"));
            d.Sight = PhysicalCodec.ReadSight(Get(m, "sight"));
            if (m.ContainsKey("sounds")) d.Sounds = SoundCodec.Read(m["sounds"], audio, payload, readImage);
            if (m.TryGetValue("nativeParameters", out var nativeParameters)) d.NativeParameters = NativeParameterCodec.Read(nativeParameters);
            if (m.TryGetValue("nativeParts", out var nativeParts)) d.NativeParts = NativePartCodec.Read(nativeParts, audio, payload, readImage);
            if (m.TryGetValue("nativeAttacks", out var nativeAttacks)) d.NativeAttacks = NativeAttackCodec.Read(nativeAttacks, audio, payload, readImage);
            if (m.ContainsKey("compendium"))
            {
                var c = Json.Object(m["compendium"], "compendium", payload ? "title habitat description category showLocked image" : "title habitat description category showLocked assetId");
                string key = Json.Text(c[payload ? "image" : "assetId"], "compendium.image"); byte[] bytes;
                if (payload) { ValidateResourcePath(key); bytes = readImage!(key); CheckPng(bytes); if (ImagePath(bytes) != key) throw new AuthoringException("ResourceHashMismatch", "compendium.image", "Preview bytes differ from their content-addressed path."); images[key] = bytes; }
                else if (!images.TryGetValue(key, out bytes!)) throw new AuthoringException("ResourceMissing", "compendium.assetId", "Preview image ID does not exist.");
                d.Compendium = new MonsterCompendium { Title = Json.Text(c["title"], "compendium.title"), Habitat = Json.Text(c["habitat"], "compendium.habitat"),
                    Description = Json.Text(c["description"], "compendium.description"), ShowLocked = Json.Bool(c["showLocked"], "compendium.showLocked"), Png = bytes,
                    Category = (MonsterCompendiumCategory)Enum.Parse(typeof(MonsterCompendiumCategory), Json.Choice(c["category"], "compendium.category", "Organism", "Machinery", "Bionics", "Boss")) };
            }
            var o = Json.Object(m["overrides"], "overrides", "", "health defense hurtDuration expValue scale tint display skin");
            d.ExpValue = Json.NullableInt(Get(o, "expValue"), "expValue");
            d.Appearance.Scale = Get(o, "scale") == null ? (float?)null : (float)Json.Number(o["scale"], "scale");
            d.Health = Json.NullableInt(Get(o, "health"), "health"); d.Defense = Json.NullableInt(Get(o, "defense"), "defense");
            d.HurtDuration = Get(o, "hurtDuration") == null ? (float?)null : (float)Json.Number(o["hurtDuration"], "hurtDuration");
            if (o.ContainsKey("tint")) d.Appearance.Tint = Json.Text(o["tint"], "tint");
            if (o.ContainsKey("display"))
            {
                var v = Json.Object(o["display"], "display", "name nameplate healthBar", "bossBar");
                d.Display.Name = Json.Text(v["name"], "display.name"); d.Display.Nameplate = Json.Bool(v["nameplate"], "display.nameplate");
                d.Display.BossBar = v.ContainsKey("bossBar") && Json.Bool(v["bossBar"], "display.bossBar");
                d.Display.HealthBar = (MonsterHealthBar)Enum.Parse(typeof(MonsterHealthBar), Json.Choice(v["healthBar"], "display.healthBar", "Damaged", "Always", "Never"));
            }
            if (Get(o, "skin") != null) d.Appearance.Skin = ReadSkin(o["skin"], images, payload, readImage);
            var b = Json.Object(m["behavior"], "behavior", "mode", "id sequence combat skills healthPhases"); d.Behavior = Json.Choice(b["mode"], "behavior.mode", "inherit-native", "code-provider", "configured-sequence");
            d.BehaviorId = b.ContainsKey("id") ? Json.Text(b["id"], "behavior.id") : "";
            if (b.ContainsKey("healthPhases"))
            {
                const string at = "behavior.healthPhases";
                var p = Json.Object(b["healthPhases"], at, "transition stages");
                d.HealthPhases = new MonsterHealthPhases { Transition = (MonsterPhaseTransition)Enum.Parse(typeof(MonsterPhaseTransition), Json.Choice(p["transition"], at + ".transition", "CancelCurrent", "FinishCurrent")) };
                int index = 0;
                foreach (var row in Json.Array(p["stages"], at + ".stages"))
                {
                    string field = at + ".stages[" + index++ + "]";
                    var s = Json.Object(row, field, "id atOrBelow speedMultiplier damageMultiplier");
                    d.HealthPhases.Stages.Add(new MonsterHealthPhase { Id = Json.Text(s["id"], field + ".id"), AtOrBelow = (float)Json.Number(s["atOrBelow"], field + ".atOrBelow"),
                        SpeedMultiplier = (float)Json.Number(s["speedMultiplier"], field + ".speedMultiplier"), DamageMultiplier = (float)Json.Number(s["damageMultiplier"], field + ".damageMultiplier") });
                }
            }
            if (b.ContainsKey("sequence")) d.Sequence = ReadSequence(b["sequence"]);
            if (b.ContainsKey("skills"))
            {
                int i = 0;
                foreach (var row in Json.Array(b["skills"], "behavior.skills"))
                {
                    string at = "behavior.skills[" + i++ + "]";
                    var skill = Json.Object(row, at, "id kind", "scatter dash animation");
                    string kind = Json.Choice(skill["kind"], at + ".kind", "scatter", "dash");
                    Json.Object(row, at, "id kind " + kind, "animation");
                    MonsterActionAnimation? animation = null;
                    if (skill.ContainsKey("animation"))
                    {
                        var a = Json.Object(skill["animation"], at + ".animation", (payload ? "image" : "assetId") + " frameWidth frameHeight pivotX pivotY pixelsPerUnit windupFrames recoveryFrames");
                        string key = Json.Text(a[payload ? "image" : "assetId"], at + ".animation.image"); byte[] bytes;
                        if (payload) { ValidateResourcePath(key); bytes = readImage!(key); CheckPng(bytes); if (ImagePath(bytes) != key) throw new AuthoringException("ResourceHashMismatch", at + ".animation.image", "Image bytes differ from their content-addressed path."); images[key] = bytes; }
                        else if (!images.TryGetValue(key, out bytes!)) throw new AuthoringException("ResourceMissing", at + ".animation.assetId", "Image ID does not exist.");
                        animation = new MonsterActionAnimation { Png = bytes, FrameWidth = Json.Int(a["frameWidth"], at + ".animation.frameWidth"), FrameHeight = Json.Int(a["frameHeight"], at + ".animation.frameHeight"),
                            PivotX = (float)Json.Number(a["pivotX"], at + ".animation.pivotX"), PivotY = (float)Json.Number(a["pivotY"], at + ".animation.pivotY"), PixelsPerUnit = (float)Json.Number(a["pixelsPerUnit"], at + ".animation.pixelsPerUnit"),
                            WindupFrames = Json.Array(a["windupFrames"], at + ".animation.windupFrames").Select(v => Json.Int(v, at + ".animation.windupFrames[]")).ToList(),
                            RecoveryFrames = Json.Array(a["recoveryFrames"], at + ".animation.recoveryFrames").Select(v => Json.Int(v, at + ".animation.recoveryFrames[]")).ToList() };
                    }
                    if (kind == "dash")
                    {
                        var dash = Json.Object(skill["dash"], at + ".dash", "distance speed hitStartFraction hitEndFraction damage range windupGameSeconds recoveryGameSeconds cooldownGameSeconds");
                        float D(string key) => (float)Json.Number(dash[key], at + ".dash." + key);
                        d.Skills.Add(new MonsterSkill { Id = Json.Text(skill["id"], at + ".id"), Kind = kind, Animation = animation, Dash = new MonsterDash {
                            Distance = D("distance"), Speed = D("speed"), HitStartFraction = D("hitStartFraction"), HitEndFraction = D("hitEndFraction"),
                            Damage = D("damage"), Range = D("range"), WindupGameSeconds = D("windupGameSeconds"), RecoveryGameSeconds = D("recoveryGameSeconds"), CooldownGameSeconds = D("cooldownGameSeconds")
                        } });
                        continue;
                    }
                    var s = Json.Object(skill["scatter"], at + ".scatter", "projectileAttackId bulletCount spreadDegrees bulletSpeed bulletLifetimeGameSeconds damage range windupGameSeconds recoveryGameSeconds cooldownGameSeconds");
                    float N(string key) => (float)Json.Number(s[key], at + ".scatter." + key);
                    d.Skills.Add(new MonsterSkill { Id = Json.Text(skill["id"], at + ".id"), Kind = Json.Choice(skill["kind"], at + ".kind", "scatter"), Animation = animation, Scatter = new MonsterScatter {
                        ProjectileAttackId = Json.Text(s["projectileAttackId"], at + ".scatter.projectileAttackId"), BulletCount = Json.Int(s["bulletCount"], at + ".scatter.bulletCount"),
                        SpreadDegrees = N("spreadDegrees"), BulletSpeed = N("bulletSpeed"), BulletLifetimeGameSeconds = N("bulletLifetimeGameSeconds"), Damage = N("damage"), Range = N("range"),
                        WindupGameSeconds = N("windupGameSeconds"), RecoveryGameSeconds = N("recoveryGameSeconds"), CooldownGameSeconds = N("cooldownGameSeconds")
                    } });
                }
            }
            if (b.ContainsKey("combat"))
            {
                var c = Json.Object(b["combat"], "behavior.combat", "mode checkIntervalGameSeconds repeatIntervalGameSeconds", "targetWaitGameSeconds");
                d.Combat = new MonsterCombatPolicy {
                    Mode = Json.Choice(c["mode"], "behavior.combat.mode", "active", "retaliate", "passive"),
                    CheckIntervalGameSeconds = (float)Json.Number(c["checkIntervalGameSeconds"], "behavior.combat.checkIntervalGameSeconds"),
                    RepeatIntervalGameSeconds = (float)Json.Number(c["repeatIntervalGameSeconds"], "behavior.combat.repeatIntervalGameSeconds"),
                    TargetWaitGameSeconds = c.ContainsKey("targetWaitGameSeconds") ? (float)Json.Number(c["targetWaitGameSeconds"], "behavior.combat.targetWaitGameSeconds") : 5
                };
            }
            d.Drops = ReadDrops(m["drops"], owner, payload);
            return d;
        }
        private static MonsterSequence ReadSequence(object? value)
        {
            const string at = "behavior.sequence";
            var data = Json.Object(value, at, "repeatCount steps");
            var sequence = new MonsterSequence { RepeatCount = Json.Int(data["repeatCount"], at + ".repeatCount") };
            int i = 0;
            foreach (var row in Json.Array(data["steps"], at + ".steps"))
            {
                string field = at + ".steps[" + i++ + "]";
                var s = Json.Object(row, field, "id action maxGameSeconds", "attackId skillId offsetX offsetY maxTravel");
                string action = Json.Choice(s["action"], field + ".action", "attack", "move-to", "skill");
                Json.Object(row, field, action == "attack" ? "id action maxGameSeconds attackId" : action == "skill" ? "id action maxGameSeconds skillId" : "id action maxGameSeconds offsetX offsetY maxTravel");
                var step = new MonsterSequenceStep { Id = Json.Text(s["id"], field + ".id"), Action = action, MaxGameSeconds = (float)Json.Number(s["maxGameSeconds"], field + ".maxGameSeconds") };
                if (action == "attack") step.AttackId = Json.Text(s["attackId"], field + ".attackId");
                else if (action == "skill") step.SkillId = Json.Text(s["skillId"], field + ".skillId");
                else { step.OffsetX = (float)Json.Number(s["offsetX"], field + ".offsetX"); step.OffsetY = (float)Json.Number(s["offsetY"], field + ".offsetY"); step.MaxTravel = (float)Json.Number(s["maxTravel"], field + ".maxTravel"); }
                sequence.Steps.Add(step);
            }
            return sequence;
        }
        public static void ValidateResourcePath(string path)
        {
            if (path.Length != 75 || !path.StartsWith("images/", StringComparison.Ordinal) || !path.EndsWith(".png", StringComparison.Ordinal) || !path.Substring(7, 64).All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'))
                throw new AuthoringException("InvalidResourcePath", "image", "Only content-addressed package-relative PNG paths are accepted.");
        }
        public static void CheckPng(byte[] bytes, int? width = null, int? height = null)
        {
            if (bytes.Length < 33 || !bytes.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) || Encoding.ASCII.GetString(bytes, 12, 4) != "IHDR")
                throw new AuthoringException("InvalidImage", "image", "Expected PNG with IHDR.");
            long Size(int i) => (long)bytes[i] << 24 | (long)bytes[i + 1] << 16 | (long)bytes[i + 2] << 8 | bytes[i + 3];
            long w = Size(16), h = Size(20);
            if (w < 1 || h < 1 || width.HasValue && width != w || height.HasValue && height != h)
                throw new AuthoringException("InvalidImage", "image.dimensions", "PNG dimensions must match and be positive.");
        }
        public static object DefinitionData(MonsterDefinition d)
        {
            var overrides = Json.Map("expValue", d.ExpValue, "scale", d.Appearance.Scale, "health", d.Health, "defense", d.Defense, "tint", d.Appearance.Tint, "display", Json.Map("name", d.Display.Name, "nameplate", d.Display.Nameplate, "healthBar", d.Display.HealthBar.ToString()));
            if (d.HurtDuration.HasValue) overrides["hurtDuration"] = d.HurtDuration.Value;
            if (d.Display.BossBar) ((IDictionary<string, object?>)overrides["display"]!)["bossBar"] = true;
            var skin = d.Appearance.Skin;
            if (skin != null) overrides["skin"] = SkinData(skin);
            var behavior = Json.Map("mode", d.Behavior, "id", d.BehaviorId);
            if (d.HealthPhases != null) behavior["healthPhases"] = Json.Map("transition", d.HealthPhases.Transition.ToString(), "stages", d.HealthPhases.Stages.Select(s =>
                Json.Map("id", s.Id, "atOrBelow", s.AtOrBelow, "speedMultiplier", s.SpeedMultiplier, "damageMultiplier", s.DamageMultiplier)).ToArray());
            if (d.Skills.Count > 0) behavior["skills"] = d.Skills.Select(skill => {
                Dictionary<string, object?> WithAnimation(Dictionary<string, object?> data)
                {
                    var a = skill.Animation;
                    if (a != null) data["animation"] = Json.Map("image", ImagePath(a.Png), "frameWidth", a.FrameWidth, "frameHeight", a.FrameHeight,
                        "pivotX", a.PivotX, "pivotY", a.PivotY, "pixelsPerUnit", a.PixelsPerUnit, "windupFrames", a.WindupFrames, "recoveryFrames", a.RecoveryFrames);
                    return data;
                }
                if (skill.Kind == "dash")
                {
                    var dash = skill.Dash!;
                    return WithAnimation(Json.Map("id", skill.Id, "kind", skill.Kind, "dash", Json.Map("distance", dash.Distance, "speed", dash.Speed,
                        "hitStartFraction", dash.HitStartFraction, "hitEndFraction", dash.HitEndFraction, "damage", dash.Damage, "range", dash.Range,
                        "windupGameSeconds", dash.WindupGameSeconds, "recoveryGameSeconds", dash.RecoveryGameSeconds, "cooldownGameSeconds", dash.CooldownGameSeconds)));
                }
                var s = skill.Scatter!;
                return WithAnimation(Json.Map("id", skill.Id, "kind", skill.Kind, "scatter", Json.Map("projectileAttackId", s.ProjectileAttackId, "bulletCount", s.BulletCount,
                    "spreadDegrees", s.SpreadDegrees, "bulletSpeed", s.BulletSpeed, "bulletLifetimeGameSeconds", s.BulletLifetimeGameSeconds, "damage", s.Damage, "range", s.Range,
                    "windupGameSeconds", s.WindupGameSeconds, "recoveryGameSeconds", s.RecoveryGameSeconds, "cooldownGameSeconds", s.CooldownGameSeconds)));
            }).ToArray();
            if (d.Combat != null) behavior["combat"] = Json.Map("mode", d.Combat.Mode, "checkIntervalGameSeconds", d.Combat.CheckIntervalGameSeconds, "repeatIntervalGameSeconds", d.Combat.RepeatIntervalGameSeconds);
            if (d.Combat?.Mode == "retaliate") ((IDictionary<string, object?>)behavior["combat"]!)["targetWaitGameSeconds"] = d.Combat.TargetWaitGameSeconds;
            if (d.Sequence != null) behavior["sequence"] = Json.Map("repeatCount", d.Sequence.RepeatCount, "steps", d.Sequence.Steps.Select(s => s.Action == "attack"
                ? Json.Map("id", s.Id, "action", s.Action, "maxGameSeconds", s.MaxGameSeconds, "attackId", s.AttackId)
                : s.Action == "skill" ? Json.Map("id", s.Id, "action", s.Action, "maxGameSeconds", s.MaxGameSeconds, "skillId", s.SkillId)
                : Json.Map("id", s.Id, "action", s.Action, "maxGameSeconds", s.MaxGameSeconds, "offsetX", s.OffsetX, "offsetY", s.OffsetY, "maxTravel", s.MaxTravel)).ToArray());
            var data = Json.Map("id", d.LocalId, "templateId", d.TemplateId, "overrides", overrides, "behavior", behavior,
                "drops", DropsData(d.Drops));
            if (d.NativeParameters != null) data["nativeParameters"] = NativeParameterCodec.Write(d.NativeParameters);
            if (d.NativeParts.Count > 0) data["nativeParts"] = d.NativeParts.Select(NativePartCodec.Write).ToArray();
            if (d.AttackComposition.Count > 0) data["attackComposition"] = d.AttackComposition.Select(a => Json.Map("sourceTemplate", a.SourceTemplate, "sourceAttack", a.SourceAttack, "targetAttack", a.TargetAttack)).ToArray();
            if (d.NativeAttacks.Count > 0) data["nativeAttacks"] = d.NativeAttacks.Select(NativeAttackCodec.Write).ToArray();
            if (d.Body != null) data["body"] = PhysicalCodec.Body(d.Body);
            if (d.Movement != null) data["movement"] = PhysicalCodec.Movement(d.Movement);
            if (d.Sight != null) data["sight"] = PhysicalCodec.Sight(d.Sight);
            if (d.Sounds != null) data["sounds"] = SoundCodec.Write(d.Sounds);
            var c = d.Compendium;
            if (c != null) data["compendium"] = Json.Map("title", c.Title, "habitat", c.Habitat, "description", c.Description, "category", c.Category.ToString(), "showLocked", c.ShowLocked, "image", ImagePath(c.Png));
            return data;
        }
        public static object DependencyData(ContentDependency d) => Json.Map("id", d.Id, "minimumInclusive", d.MinimumInclusive, "maximumExclusive", d.MaximumExclusive, "includePrerelease", d.IncludePrerelease);
        public static object PoolData(MonsterDropPool p) => Json.Map("id", p.LocalId, "entries", p.Entries.Select(e => Json.Map("itemId", e.ItemId, "weight", e.Weight, "minCount", e.MinCount, "maxCount", e.MaxCount)).ToArray());
        public static object RuleData(MonsterSpawnRule r) => Json.Map("id", r.LocalId, "definitionId", r.DefinitionId, "roomId", r.RoomId, "regionId", r.RegionId, "count", r.Count);
        private static object SkinData(MonsterSkin skin) => Json.Map("image", ImagePath(skin.Png), "templateFingerprint", skin.TemplateFingerprint, "frames", skin.Frames.Select(f => Json.Map("frameId", f.FrameId, "x", f.X, "y", f.Y, "width", f.Width, "height", f.Height)).ToArray());
        private static object OverrideData(MonsterNativeOverride value)
        {
            var fields = Json.Map("defense", value.Defense, "hurtDuration", value.HurtDuration);
            if (value.Tint != null) fields["tint"] = value.Tint;
            if (value.ProjectileTint != null) fields["projectileTint"] = value.ProjectileTint;
            if (value.Sounds != null) fields["sounds"] = SoundCodec.Write(value.Sounds);
            if (value.NativeAttacks.Count > 0) fields["nativeAttacks"] = value.NativeAttacks.Select(NativeAttackCodec.Write).ToArray();
            if (value.Skin != null) fields["skin"] = SkinData(value.Skin);
            if (value.Drops != null) fields["drops"] = DropsData(value.Drops);
            return Json.Map("id", value.LocalId, "targetId", value.TargetId, "fields", fields);
        }
        internal static object SectionData(ContentInput input)
        {
            var data = Json.Map("definitions", input.Definitions.Select(DefinitionData).ToArray(), "dropPools", input.DropPools.Select(PoolData).ToArray(), "spawnRules", input.SpawnRules.Select(RuleData).ToArray());
            if (input.NativeOverrides.Count != 0) data["nativeOverrides"] = input.NativeOverrides.Select(OverrideData).ToArray();
            return data;
        }
    }
}
