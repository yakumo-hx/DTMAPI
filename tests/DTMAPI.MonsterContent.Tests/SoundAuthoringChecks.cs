using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;

static partial class Program
{
    static void SoundAuthoringChecks()
    {
        byte[] wav = SoundWave();
        var catalog = AuthorCatalog(); catalog.Templates[0].SoundEvents = new[] { "hurt" };
        var hurt = Json.Map("assetId", "ouch", "volumeDb", .25);
        var definition = Json.Map("id", "voice", "templateId", "test-template", "overrides", Json.Map(), "behavior", Json.Map("mode", "inherit-native"),
            "drops", Json.Map("mode", "inherit", "minCount", null, "maxCount", null), "sounds", Json.Map("hurt", hurt));
        var asset = Json.Map("id", "ouch", "name", "声音.wav", "dataUrl", "data:audio/wav;base64," + Convert.ToBase64String(wav));
        var project = Json.Map("format", "dtmapi.monster-project", "version", 4, "pack", Json.Map("id", "Author", "name", "声音"),
            "monsters", new[] { definition }, "assets", Array.Empty<object>(), "audioAssets", new[] { asset },
            "dropPools", Array.Empty<object>(), "spawnRules", Array.Empty<object>(), "dependencies", Array.Empty<object>());
        var input = ContentCodec.ReadProject(Json.Write(project));
        Check(input.Audio.Count == 1 && input.Images.Count == 0 && input.Definitions[0].Sounds!.Hurt!.ResourcePath == ContentCodec.AudioPath(wav), "WAV has its own resource namespace and survives project import.");
        Check(ContentCompiler.Validate(input, catalog).Count == 0, "An explicitly supported Hurt event passes ordinary author admission.");
        string payload = ContentCodec.WriteContent(input), path = ContentCodec.AudioPath(wav);
        var read = ContentCodec.ReadContent(payload, "A", p => input.Audio[p]);
        Check(ContentCodec.WriteContent(read) == payload && read.Audio.Count == 0 && read.Definitions[0].Sounds!.Hurt!.ResourcePath == path,
            "Compiler/runtime preserve the Hurt resource identity and settings without retaining its WAV in the runtime DTO.");
        Check(read.Definitions[0].Sounds!.Hurt!.VolumeDb == .25f, "VolumeDb survives and shared positioning inherits the native attenuation.");
        catalog.Templates[0].SoundEvents = Array.Empty<string>();
        Check(ContentCompiler.Validate(input, catalog).Any(d => d.Field.StartsWith("sounds", StringComparison.Ordinal)), "Unsupported sound events are rejected before packaging.");
        void Reject(Action action, string code)
        {
            try { action(); Check(false, "Expected sound authoring failure: " + code); }
            catch (AuthoringException error) { Check(error.Diagnostic.Code == code, "Sound authoring diagnostic: " + code); }
        }
        bool invoked = false;
        Reject(() => ContentCodec.ReadContent(payload.Replace(path, "../escape.wav"), "A", p => { invoked = true; return wav; }), "InvalidAudio");
        Check(!invoked, "Invalid audio paths never reach the package file reader.");
        var different = (byte[])wav.Clone(); different[44] ^= 1;
        invoked = false;
        var deferred = ContentCodec.ReadContent(payload, "A", _ => { invoked = true; return different; });
        Check(!invoked && deferred.Definitions[0].Sounds!.Hurt!.ResourcePath == path, "Hurt media validation and hash matching belong to the common Audio preparation, not a second eager decoder.");
        hurt["assetId"] = "missing";
        Reject(() => ContentCodec.ReadProject(Json.Write(project)), "ResourceMissing"); hurt["assetId"] = "ouch";
        project["audioAssets"] = new[] { asset, asset };
        Reject(() => ContentCodec.ReadProject(Json.Write(project)), "DuplicateId"); project["audioAssets"] = new[] { asset };
        asset["dataUrl"] = "data:audio/wav;base64,AAAA";
        Reject(() => ContentCodec.ReadProject(Json.Write(project)), "InvalidSound");
        asset["dataUrl"] = "data:audio/wav;base64," + Convert.ToBase64String(wav);
        hurt.Remove("volumeDb"); hurt.Remove("maxDistance");
        var defaults = ContentCodec.ReadProject(Json.Write(project)).Definitions[0].Sounds!.Hurt!;
        Check(defaults.VolumeDb == 0, "Omitted optional sound settings use documented defaults.");
        definition["sounds"] = Json.Map("dead", Json.Map("assetId", "ouch", "volumeDb", .6));
        var deathOnly = ContentCodec.ReadProject(Json.Write(project));
        catalog.Templates[0].SoundEvents = new[] { "hurt" };
        Check(ContentCompiler.Validate(deathOnly, catalog).Any(d => d.Code == "SoundUnsupported" && d.Field == "sounds.dead"),
            "A Hurt-capable catalog cannot implicitly admit a death event.");
        catalog.Templates[0].SoundEvents = new[] { "dead" };
        Check(ContentCompiler.Validate(deathOnly, catalog).Count == 0 && deathOnly.Definitions[0].Sounds!.Hurt == null,
            "A death-only project preserves inherited Hurt and passes the exact event capability.");
        definition["sounds"] = Json.Map("hurt", hurt, "dead", Json.Map("assetId", "ouch", "volumeDb", .6));
        var dual = ContentCodec.ReadProject(Json.Write(project));
        var dualRead = ContentCodec.ReadContent(ContentCodec.WriteContent(dual), "A", p => dual.Audio[p]);
        Check(dual.Audio.Count == 1 && dualRead.Audio.Count == 0 && dualRead.Definitions[0].Sounds!.Hurt!.VolumeDb == 0 && dualRead.Definitions[0].Sounds!.Dead!.VolumeDb == .6f,
            "Two events share one packaged WAV while retaining independent playback settings.");
        catalog.Templates[0].SoundEvents = new[] { "hurt", "dead" };
        Check(ContentCompiler.Validate(dualRead, catalog).Count == 0 && CatalogCodec.Read(CatalogCodec.Write(catalog)).Templates[0].SoundEvents.SequenceEqual(new[] { "hurt", "dead" }),
            "The current catalog roundtrips both event capabilities and admits both.");
        definition["sounds"] = Json.Map("humming", Json.Map("assetId", "ouch", "volumeDb", .4, "loop", true));
        var loopOnly = ContentCodec.ReadProject(Json.Write(project));
        Check(ContentCompiler.Validate(loopOnly, catalog).Any(d => d.Code == "SoundUnsupported" && d.Field == "sounds.humming"),
            "Short-event capabilities never imply loop support.");
        catalog.Templates[0].SoundEvents = new[] { "humming" };
        Check(ContentCompiler.Validate(loopOnly, catalog).Count == 0 && loopOnly.Definitions[0].Sounds!.Hurt == null && loopOnly.Definitions[0].Sounds!.Dead == null,
            "A loop-only project preserves inherited short events.");
        definition["sounds"] = Json.Map("humming", Json.Map("assetId", "ouch", "volumeDb", .4, "loop", true), "hurt", hurt,
            "dead", Json.Map("assetId", "ouch", "volumeDb", .6));
        var triple = ContentCodec.ReadProject(Json.Write(project));
        var tripleRead = ContentCodec.ReadContent(ContentCodec.WriteContent(triple), "A", p => triple.Audio[p]);
        catalog.Templates[0].SoundEvents = new[] { "hurt", "dead", "humming" };
        Check(ContentCompiler.Validate(tripleRead, catalog).Count == 0 && tripleRead.Audio.Count == 0 &&
            tripleRead.Definitions[0].Sounds!.Humming!.VolumeDb == .4f && tripleRead.Definitions[0].Sounds!.Humming!.Loop == true &&
            tripleRead.Definitions[0].Sounds!.Dead!.VolumeDb == .6f && tripleRead.Definitions[0].Sounds!.Hurt!.VolumeDb == 0,
            "Three author events share one WAV while retaining event-specific settings through Compiler/runtime.");
        Check(CatalogCodec.Read(CatalogCodec.Write(catalog)).Templates[0].SoundEvents.SequenceEqual(new[] { "hurt", "dead", "humming" }), "Catalog preserves the loop capability.");
        foreach (bool? loop in new bool?[] { null, false, true })
        {
            var humming = Json.Map("assetId", "ouch");
            if (loop.HasValue) humming["loop"] = loop.Value;
            definition["sounds"] = Json.Map("humming", humming);
            var author = ContentCodec.ReadProject(Json.Write(project));
            catalog.Templates[0].HummingLoop = true;
            string encoded = ContentCodec.WriteContent(author, catalog);
            var restored = ContentCodec.ReadContent(encoded, "A", p => author.Audio[p]);
            Check(author.Definitions[0].Sounds!.Humming!.Loop == loop && restored.Definitions[0].Sounds!.Humming!.Loop == (loop ?? true) &&
                ContentCodec.WriteContent(restored) == encoded, "Compiler resolves inherited Humming once from native catalog into an explicit Audio recipe: " + loop);
        }
        foreach (string use in new[] { "hurt", "dead", "humming", "recoveryWarning", "ready", "fire", "dash", "throw", "volley" })
        {
            definition.Remove("sounds"); definition.Remove("nativeAttacks");
            if (SoundValidation.CommonAttackField(use + "Sound")) definition["nativeAttacks"] = new[] { Json.Map("attackId", "AtkNormal", use + "Sound", Json.Map("assetId", "ouch")) };
            else definition["sounds"] = Json.Map(use, Json.Map("assetId", "ouch"));
            var author = ContentCodec.ReadProject(Json.Write(project));
            var restored = ContentCodec.ReadContent(ContentCodec.WriteContent(author), "A", p => author.Audio[p]);
            Check(SoundValidation.CommonEvents(restored.Definitions.Single()).Count() == 1 && restored.Dependencies.Count == 0,
                "Sound contribution supplies module demand without synthetic package dependencies: " + use);
            author.Dependencies.Add(new ContentDependency { Id = DTMAPI.AudioContent.AudioModule.ProviderId, MinimumInclusive = "0.2.1", MaximumExclusive = "0.2.9" });
            restored = ContentCodec.ReadContent(ContentCodec.WriteContent(author), "A", p => author.Audio[p]);
            Check(restored.Dependencies.Count == 0 && author.Dependencies.Single().MinimumInclusive == "0.2.1" && author.Dependencies.Single().MaximumExclusive == "0.2.9",
                "Explicit dependency remains in author metadata for the package manifest: " + use);
        }
        definition.Remove("nativeAttacks");
        definition["sounds"] = Json.Map();
        Reject(() => ContentCodec.ReadProject(Json.Write(project)), "InvalidSound");
    }
}
