using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void RecoverySoundChecks()
    {
        MonsterSound Sound(float volume = .4f) => SharedAttackSound(volume);
        MonsterDefinition Define(bool rootSound = true)
        {
            var value = Definition();
            if (rootSound) value.Sounds = new MonsterSounds { RecoveryWarning = Sound() };
            value.NativeParts.Add(new MonsterNativePartParameters { TemplateId = "nian_head", Sounds = new MonsterSounds { RecoveryWarning = Sound(.6f) } });
            value.NativeParts.Add(new MonsterNativePartParameters { TemplateId = "nian_tail", Sounds = new MonsterSounds { RecoveryWarning = Sound(.8f) } });
            return value;
        }
        var original = AuthorCatalog(); var root = original.Templates[0];
        root.SoundEvents = new[] { "hurt", "dead", "humming", "recoveryWarning" };
        root.NativeOverrideFields = new[] { "sounds.recoveryWarning", "sounds.hurt" };
        root.Actions.NativeParts = new[] { "nian_head", "nian_tail" }.Select(id => new MonsterNativePartInfo(id, Array.Empty<MonsterNativeAttackInfo>(), new[] { "recoveryWarning" })).ToArray();
        var parts = new[] { "nian_head", "nian_tail" }.Select(id => new MonsterTemplate(id, 1, false, false, "fixture", "fixture", 100, 0, "fixture",
            new[] { new MonsterDiagnostic("EncounterPart", "A complete encounter owns this part.") }) { NativeOverrideFields = new[] { "sounds.recoveryWarning" } }).ToArray();
        var catalog = new MonsterContentCatalog(1, "fixture", MonsterModule.ContractVersion, "recovery", new[] { root }.Concat(parts), original.Items, original.DropPools, original.Rooms);
        var value = Define();
        Check(MonsterValidation.Validate("A", value) == null && MonsterAdmission.Definition("A", value, catalog) == null,
            "Recovery root and sound-only parts are admitted without attack parameters.");
        Check(root.EditableFields.Contains("nativeParts"), "A sound-only part capability exposes the part authoring entry.");
        var frozen = new DefinitionRecord("A", value, 1).Definition;
        value.Sounds!.RecoveryWarning!.ResourcePath = "audio/changed.wav"; value.Sounds.RecoveryWarning.VolumeDb = 0;
        value.NativeParts[0].Sounds!.RecoveryWarning!.ResourcePath = "audio/changed.wav"; value.NativeParts[0].Sounds!.RecoveryWarning!.VolumeDb = 0;
        value.NativeParts.Clear();
        Check(frozen.Sounds!.RecoveryWarning!.VolumeDb == .4f && frozen.NativeParts[0].Sounds!.RecoveryWarning!.VolumeDb == .6f &&
            frozen.NativeParts[1].Sounds!.RecoveryWarning!.VolumeDb == .8f && frozen.NativeParts.All(p => p.Sounds!.RecoveryWarning!.ResourcePath == ContentCodec.AudioPath(SoundWave())) &&
            frozen.Sounds.RecoveryWarning.ResourcePath == ContentCodec.AudioPath(SoundWave()), "Root, head and tail freeze their own resource paths and settings.");
        var roundCatalog = CatalogCodec.Read(CatalogCodec.Write(catalog));
        Check(roundCatalog.Templates[0].SoundEvents.Count == 4 && roundCatalog.Templates[0].Actions.NativeParts.All(p => p.SoundEvents.SequenceEqual(new[] { "recoveryWarning" })) &&
            MonsterAdmission.Definition("A", frozen, roundCatalog) == null, "Catalog roundtrip preserves exact root and nested sound capabilities.");
        root.SoundEvents = new[] { "hurt", "dead", "humming" };
        Check(MonsterAdmission.Definition("A", Define(), catalog)?.Field == "sounds.recoveryWarning", "Existing root sound events cannot grant recovery support.");
        Check(MonsterAdmission.Definition("A", Define(false), catalog) == null, "A part-only declaration does not require root recovery support.");
        root.SoundEvents = new[] { "recoveryWarning" };
        root.Actions.NativeParts = root.Actions.NativeParts.Select(p => new MonsterNativePartInfo(p.TemplateId, p.NativeAttacks)).ToArray();
        Check(MonsterAdmission.Definition("A", Define(), catalog)?.Field == "nativeParts[0].sounds.recoveryWarning", "Root and original-part support cannot grant an absent nested capability.");
        root.Actions.NativeParts = roundCatalog.Templates[0].Actions.NativeParts;
        foreach (string kind in new[] { "hurt", "dead", "humming" })
        {
            var d = Define(false); var sounds = d.NativeParts[0].Sounds!;
            if (kind == "hurt") sounds.Hurt = Sound(); else if (kind == "dead") sounds.Dead = Sound(); else sounds.Humming = Sound();
            Check(MonsterAdmission.Definition("A", d, catalog)?.Field == "nativeParts[0].sounds." + kind,
                "A recovery-capable part explicitly rejects unimplemented " + kind + ".");
        }
        var standalone = Define(false); standalone.TemplateId = "nian_head";
        Check(MonsterAdmission.Definition("A", standalone, catalog)?.Code == "EncounterPart", "Part audio never authorizes standalone part definitions.");
        foreach (var change in new Action<MonsterSound>[] { s => s.ResourcePath = "", s => s.VolumeDb = float.NaN, s => s.ResourcePath = "../escape.wav" })
        {
            var d = Define(false); change(d.NativeParts[0].Sounds!.RecoveryWarning!);
            var error = MonsterValidation.Validate("A", d);
            Check(error?.Code == "InvalidSound" && error.Field.StartsWith("nativeParts[0].sounds.recoveryWarning", StringComparison.Ordinal), "Malformed nested audio reports the complete part/event path.");
        }
        var publisher = new Publisher { Ready = true, Generation = 1, Failure = "WaitingNative" };
        var definitions = new DefinitionRegistry(publisher, () => { });
        var waiting = definitions.Register("PartOnly", Define(false)).Value!;
        Check(waiting.Snapshot.State == MonsterDefinitionState.WaitingNative && SoundValidation.HasSounds(publisher.Last!.Definition), "Part-only audio participates in deferred native preparation.");
        waiting.Dispose(); int publishes = publisher.Publishes; publisher.Failure = null; definitions.Refresh();
        Check(publisher.Publishes == publishes && publisher.Last!.Definition.NativeParts.All(p => p.Sounds == null) && !SoundValidation.HasSounds(publisher.Last.Definition),
            "Withdrawal of part-only waiting input releases resource inputs and cannot publish on later completion.");

        MonsterNativeOverride Native(string target = "test-template", float volume = .4f) => new MonsterNativeOverride { LocalId = "warning", TargetId = target, Sounds = new MonsterSounds { RecoveryWarning = Sound(volume) } };
        Check(MonsterOverrideAdmission.Validate("A", Native("nian_tail"), catalog) == null, "Original tail recovery override is admitted independently of standalone inheritance.");
        parts[1].NativeOverrideFields = Array.Empty<string>();
        Check(MonsterOverrideAdmission.Validate("A", Native("nian_tail"), catalog)?.Code == "NativeOverrideUnsupported", "Nested part recovery does not grant original target override capability.");
        parts[1].NativeOverrideFields = new[] { "sounds.recoveryWarning" };
        bool pending = true;
        var overrides = new NativeOverrideRegistry(() => { }, (_, _) => 0, (_, _, _) => pending ? new MonsterDiagnostic("WaitingNative", "native sound resource not ready") : null);
        overrides.Refresh(true, 1); var native = Native(); var a = overrides.Register("A", 1, native).Value!;
        Check(a.Snapshot.State == MonsterOverrideState.WaitingNative && overrides.HasWaitingSounds && overrides.ResolveRecoverySound(native.TargetId) == null, "Recovery-only override waits without a published winner.");
        native.Sounds!.RecoveryWarning!.VolumeDb = .9f; native.Sounds.RecoveryWarning.ResourcePath = "audio/changed.wav";
        pending = false; overrides.Refresh(true, 1); var first = overrides.ResolveRecoverySound(native.TargetId)!;
        Check(first.VolumeDb == .4f && first.ResourcePath == ContentCodec.AudioPath(SoundWave()), "Waiting original warning freezes resource paths and settings.");
        var b = overrides.Register("B", 2, Native(volume: 0)).Value!;
        var hurt = Native(); hurt.Sounds = new MonsterSounds { Hurt = SharedAttackSound(.7f) }; var h = overrides.Register("H", 3, hurt).Value!;
        Check(overrides.ResolveRecoverySound(native.TargetId)!.VolumeDb == 0 && overrides.ResolveHurtSound(native.TargetId)!.VolumeDb == .7f &&
            b.Snapshot.Fields.Single().RequestedResource != a.Snapshot.Fields.Single().RequestedResource && a.Snapshot.Fields.Single().State == MonsterOverrideFieldState.Shadowed,
            "Explicit warning silence and resource identity arbitrate independently of Hurt.");
        var tail = overrides.Register("T", 3, Native("nian_tail", .8f)).Value!;
        b.Dispose(); Check(ReferenceEquals(overrides.ResolveRecoverySound(native.TargetId), first) && overrides.ResolveRecoverySound("nian_tail")!.VolumeDb == .8f, "Withdrawal reveals the previous warning without changing another target.");
        a.Dispose(); h.Dispose(); tail.Dispose();
        pending = true; var cancelled = overrides.Register("C", 1, Native()).Value!; cancelled.Dispose();
        Check(!overrides.HasWaitingSounds && overrides.ResolveRecoverySound(native.TargetId) == null, "Withdrawing the last recovery-only waiter leaves no pending original sound.");

        object JsonSound(double volume) => Json.Map("recoveryWarning", Json.Map("assetId", "tone", "volumeDb", volume));
        var partRow = Json.Map("templateId", "nian_head", "sounds", JsonSound(.6));
        var project = Json.Map("format", "dtmapi.monster-project", "version", 4, "pack", Json.Map("id", "A", "name", "Recovery"),
            "monsters", new[] { Json.Map("id", "own", "templateId", "test-template", "overrides", Json.Map(), "behavior", Json.Map("mode", "inherit-native"),
                "drops", Json.Map("mode", "inherit", "minCount", null, "maxCount", null), "sounds", Json.Map("recoveryWarning", Json.Map("assetId", "tone", "volumeDb", .4)), "nativeParts", new[] { partRow }) },
            "nativeOverrides", new[] { Json.Map("id", "original", "targetId", "nian_tail", "fields", Json.Map("sounds", Json.Map("recoveryWarning", Json.Map("assetId", "tone", "volumeDb", .8)))) },
            "assets", Array.Empty<object>(), "dropPools", Array.Empty<object>(), "spawnRules", Array.Empty<object>(), "dependencies", Array.Empty<object>(),
            "audioAssets", new[] { Json.Map("id", "tone", "name", "原创.wav", "dataUrl", "data:audio/wav;base64," + Convert.ToBase64String(SoundWave())) });
        var content = ContentCodec.ReadProject(Json.Write(project)); string payload = ContentCodec.WriteContent(content);
        var back = ContentCodec.ReadContent(payload, "A", path => content.Audio[path]);
        Check(ContentCompiler.Validate(back, catalog).Count == 0 && back.Audio.Count == 0 && ContentCodec.WriteContent(back) == payload &&
            back.Definitions[0].Sounds!.RecoveryWarning!.VolumeDb == .4f && back.Definitions[0].NativeParts[0].Sounds!.RecoveryWarning!.VolumeDb == .6f &&
            back.NativeOverrides[0].Sounds!.RecoveryWarning!.VolumeDb == .8f, "Root, sound-only part and original target share packaged PCM but retain independent event settings.");
        partRow["sounds"] = Json.Map("recoveryWarning", Json.Map("assetId", "missing"));
        try { ContentCodec.ReadProject(Json.Write(project)); Check(false, "Expected missing part audio."); }
        catch (AuthoringException e) { Check(e.Diagnostic.Code == "ResourceMissing" && e.Diagnostic.Field == "nativeParts[0].sounds.recoveryWarning.assetId", "Missing part audio retains the precise Compiler diagnostic path."); }
    }
}
