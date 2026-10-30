using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void AttackEventSoundChecks()
    {
        foreach (var field in new[] { "throwSound", "volleySound", "readySound", "landSound", "countdownSound", "explosionSound" })
        {
            MonsterSound Sound(float volume = .3f) => new MonsterSound { ResourcePath = SharedAttackSound().ResourcePath, VolumeDb = volume };
            MonsterNativeAttackParameters Row(float volume = .3f)
            {
                var row = new MonsterNativeAttackParameters { AttackId = "AtkNormal" };
                if (field == "throwSound") row.ThrowSound = Sound(volume); else if (field == "volleySound") row.VolleySound = Sound(volume);
                else if (field == "landSound") row.LandSound = Sound(volume); else if (field == "countdownSound") row.CountdownSound = Sound(volume);
                else if (field == "explosionSound") row.ExplosionSound = Sound(volume); else row.ReadySound = Sound(volume);
                return row;
            }
            var definition = Definition(); definition.NativeAttacks.Add(Row());
            var frozen = new DefinitionRecord("A", definition, 1).Definition;
            NativeAttackValidation.Sound(definition.NativeAttacks[0], field)!.ResourcePath = "audio/changed.wav";
            NativeAttackValidation.Sound(definition.NativeAttacks[0], field)!.VolumeDb = 1;
            Check(NativeAttackValidation.Sound(frozen.NativeAttacks[0], field)!.ResourcePath == SharedAttackSound().ResourcePath && NativeAttackValidation.Sound(frozen.NativeAttacks[0], field)!.VolumeDb == .3f,
                "Event sound registration freezes resource paths and settings: " + field);
            var c = AuthorCatalog(); c.Templates[0].NativeOverrideFields = new[] { "nativeAttacks" };
            c.Templates[0].Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "event", Array.Empty<MonsterNativeAttackFieldInfo>(), sounds:
                new[] { new MonsterNativeAttackSoundInfo(field, "exact native event", true, false), new MonsterNativeAttackSoundInfo("fireSound", "each bullet", true, true) }) };
            c = CatalogCodec.Read(CatalogCodec.Write(c));
            Check(MonsterAdmission.Definition("A", frozen, c) == null, "Event sound-only definition needs no numeric changes: " + field);
            var ov = new MonsterNativeOverride { LocalId = "event", TargetId = definition.TemplateId, NativeAttacks = { Row() } };
            Check(MonsterOverrideAdmission.Validate("A", ov, c)?.Field == "fields.nativeAttacks[0]." + field, "Original domain requires its own event capability: " + field);
            var other = field == "throwSound" ? "volleySound" : "throwSound";
            c.Templates[0].Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "event", Array.Empty<MonsterNativeAttackFieldInfo>(), sounds:
                new[] { new MonsterNativeAttackSoundInfo(other, "different event", true, true) }) };
            Check(MonsterAdmission.Definition("A", frozen, c)?.Field == "nativeAttacks[0]." + field, "Different event capability cannot authorize this input: " + field);
            c.Templates[0].Actions.NativeAttacks = new[] { new MonsterNativeAttackInfo("AtkNormal", "event", Array.Empty<MonsterNativeAttackFieldInfo>(), sounds:
                new[] { new MonsterNativeAttackSoundInfo(field, "native event", true, true), new MonsterNativeAttackSoundInfo("fireSound", "each bullet", true, true) }) };
            foreach (var mutate in new Action<MonsterSound>[] { s => s.VolumeDb = float.NaN, s => s.ResourcePath = "", s => s.ResourcePath = "../escape.wav" })
            {
                var bad = Definition(); bad.NativeAttacks.Add(Row()); mutate(NativeAttackValidation.Sound(bad.NativeAttacks[0], field)!);
                var error = MonsterValidation.Validate("A", bad);
                Check(error?.Code == "InvalidSound" && error.Field.StartsWith("nativeAttacks[0]." + field), "Invalid event sound fails before allocation at its field: " + field);
            }
            bool waiting = true;
            var registry = new NativeOverrideRegistry(() => { }, (_, _) => 4, (_, _, _) => waiting ? new MonsterDiagnostic("WaitingNative", "bank") : null);
            registry.Refresh(true, 1); var first = registry.Register("A", 1, ov).Value!;
            Check(registry.HasWaitingSounds && registry.ResolveAttackSound(definition.TemplateId, "AtkNormal", field) == null, "Event sound-only native wait publishes no winner: " + field);
            waiting = false; registry.Refresh(true, 1);
            var original = registry.ResolveAttackSound(definition.TemplateId, "AtkNormal", field);
            var fireLease = registry.Register("B", 2, new MonsterNativeOverride { LocalId = "fire", TargetId = definition.TemplateId,
                NativeAttacks = { new MonsterNativeAttackParameters { AttackId = "AtkNormal", FireSound = Sound(.6f) } } }).Value!;
            var fire = registry.ResolveAttackSound(definition.TemplateId, "AtkNormal", "fireSound");
            var winner = registry.Register("C", 3, new MonsterNativeOverride { LocalId = "event", TargetId = definition.TemplateId, NativeAttacks = { Row(0) } }).Value!;
            Check(!registry.NativeAttackTargets.Any() && registry.ResolveAttackSound(definition.TemplateId, "AtkNormal", field)!.VolumeDb == 0 && ReferenceEquals(fire, registry.ResolveAttackSound(definition.TemplateId, "AtkNormal", "fireSound")),
                "Event zero overrides only its whole resource, preserving numeric holders and Fire: " + field);
            Check(winner.Snapshot.Fields.Single().RequestedResource != first.Snapshot.Fields.Single().RequestedResource, "Event settings contribute to resource identity: " + field);
            winner.Dispose(); Check(ReferenceEquals(original, registry.ResolveAttackSound(definition.TemplateId, "AtkNormal", field)), "Event winner withdrawal restores the surviving declaration: " + field);
            first.Dispose(); Check(registry.ResolveAttackSound(definition.TemplateId, "AtkNormal", field) == null && ReferenceEquals(fire, registry.ResolveAttackSound(definition.TemplateId, "AtkNormal", "fireSound")), "Last event withdrawal keeps Fire: " + field);
            fireLease.Dispose(); waiting = true;
            var pending = registry.Register("A", 1, ov).Value!; pending.Dispose(); Check(!registry.HasWaitingSounds, "Event waiter disposal cancels demand: " + field);
            SoundValidation.ReleaseInputs(frozen); Check(!SoundValidation.HasSounds(frozen) && NativeAttackValidation.Sound(frozen.NativeAttacks[0], field) == null, "Terminal cleanup releases event resource inputs: " + field);

            var sound = Json.Map("assetId", "tone", "volumeDb", .3);
            if (!SoundValidation.CommonAttackField(field)) sound["maxDistance"] = 12;
            var row = Json.Map("attackId", "AtkNormal", field, sound, "fireSound", Json.Map("assetId", "tone", "volumeDb", .6));
            var overrideRow = Json.Map("attackId", "AtkNormal", field, Json.Map("assetId", "tone", "volumeDb", .3),
                "fireSound", Json.Map("assetId", "tone", "volumeDb", .6));
            var project = Json.Map("format", "dtmapi.monster-project", "version", 4, "pack", Json.Map("id", "Author", "name", "攻击事件声音"),
                "monsters", new[] { Json.Map("id", "variant", "templateId", definition.TemplateId, "overrides", Json.Map(), "behavior", Json.Map("mode", "inherit-native"),
                    "drops", Json.Map("mode", "inherit", "minCount", null, "maxCount", null), "nativeAttacks", new[] { row }) },
                "nativeOverrides", new[] { Json.Map("id", "original", "targetId", definition.TemplateId, "fields", Json.Map("nativeAttacks", new[] { overrideRow })) },
                "audioAssets", new[] { Json.Map("id", "tone", "name", "tone.wav", "dataUrl", "data:audio/wav;base64," + Convert.ToBase64String(SoundWave())) },
                "assets", Array.Empty<object>(), "dropPools", Array.Empty<object>(), "spawnRules", Array.Empty<object>(), "dependencies", Array.Empty<object>());
            var input = ContentCodec.ReadProject(Json.Write(project)); var payload = ContentCodec.WriteContent(input);
            var parsed = ContentCodec.ReadContent(payload, "A", p => input.Audio[p]);
            Check(input.Audio.Count == 1 && ContentCodec.WriteContent(parsed) == payload && ContentCompiler.Validate(parsed, c).Count == 0, "Event and Fire reuse PCM while preserving separate settings in both author domains: " + field);
            Check(NativeAttackValidation.Sound(parsed.Definitions[0].NativeAttacks[0], field)!.VolumeDb == .3f && parsed.NativeOverrides[0].NativeAttacks[0].FireSound!.VolumeDb == .6f, "Compiler keeps exact event settings: " + field);
            sound["assetId"] = "missing";
            try { ContentCodec.ReadProject(Json.Write(project)); Check(false, "Missing event WAV rejects."); }
            catch (AuthoringException error) { Check(error.Diagnostic.Code == "ResourceMissing" && error.Diagnostic.Field.Contains(field), "Missing event WAV retains the exact field: " + field); }
        }
    }
}
