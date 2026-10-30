using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void DashSoundChecks()
    {
        MonsterSound Sound(float volume = .3f) => SharedAttackSound(volume);
        MonsterNativeAttackParameters Dash(string slot = "AtkNormal", float volume = .3f) => new MonsterNativeAttackParameters { AttackId = slot, DashSound = Sound(volume) };
        MonsterNativeAttackInfo Slot(string slot, bool definition = true, bool native = true, bool fire = false) => new MonsterNativeAttackInfo(slot, "multi-impact",
            Array.Empty<MonsterNativeAttackFieldInfo>(), sounds: new[] { new MonsterNativeAttackSoundInfo("dashSound", "each segment", definition, native) }
                .Concat(fire ? new[] { new MonsterNativeAttackSoundInfo("fireSound", "each shot", definition, native) } : Array.Empty<MonsterNativeAttackSoundInfo>()));
        var d = Definition(); d.NativeAttacks.Add(Dash()); d.NativeAttacks[0].DashSound = SharedAttackSound(.3f);
        d.NativeParts.Add(new MonsterNativePartParameters { TemplateId = "nian_head", NativeAttacks = { Dash() } });
        var frozen = new DefinitionRecord("A", d, 1).Definition;
        d.NativeAttacks[0].DashSound!.VolumeDb = 1; d.NativeParts[0].NativeAttacks[0].DashSound!.ResourcePath = "audio/changed.wav";
        Check(frozen.NativeAttacks[0].DashSound!.VolumeDb == .3f && frozen.NativeParts[0].NativeAttacks[0].DashSound!.ResourcePath == ContentCodec.AudioPath(SoundWave()), "Root and explicit head freeze independent dash inputs.");
        var c = AuthorCatalog(); c.Templates[0].NativeOverrideFields = new[] { "nativeAttacks" };
        c.Templates[0].Actions.NativeAttacks = new[] { Slot("AtkNormal", native: false), Slot("AtkSkill01") };
        c.Templates[0].Actions.NativeParts = new[] { new MonsterNativePartInfo("nian_head", new[] { Slot("AtkNormal", native: false) }) };
        c = CatalogCodec.Read(CatalogCodec.Write(c));
        Check(MonsterAdmission.Definition("A", frozen, c) == null && SoundValidation.HasSounds(frozen), "Dash-only root and head are admitted without numeric edits.");
        var ov = new MonsterNativeOverride { LocalId = "dash", TargetId = d.TemplateId, NativeAttacks = { Dash() } };
        Check(MonsterOverrideAdmission.Validate("A", ov, c)?.Field == "fields.nativeAttacks[0].dashSound", "Definition dash capability cannot grant the original domain.");
        c.Templates[0].Actions.NativeAttacks = new[] { Slot("AtkNormal", fire: true), Slot("AtkSkill01") };
        c = CatalogCodec.Read(CatalogCodec.Write(c));
        Check(c.Templates[0].Actions.NativeAttacks[0].Sounds.Count == 2 && MonsterOverrideAdmission.Validate("A", ov, c) == null, "Catalog keeps distinct sound fields on one slot.");
        frozen.NativeParts[0].NativeAttacks[0].FireSound = Sound();
        Check(MonsterAdmission.Definition("A", frozen, c)?.Field == "nativeParts[0].nativeAttacks[0].fireSound", "Root sound capability cannot authorize a different internal-part resource.");
        frozen.NativeParts[0].NativeAttacks[0].FireSound = null;
        foreach (var change in new Action<MonsterSound>[] { s => s.VolumeDb = float.NaN, s => s.ResourcePath = "", s => s.ResourcePath = "../escape.wav" })
        {
            var bad = Definition(); bad.NativeAttacks.Add(Dash()); change(bad.NativeAttacks[0].DashSound!);
            var error = MonsterValidation.Validate("A", bad);
            Check(error?.Code == "InvalidSound" && error.Field.StartsWith("nativeAttacks[0].dashSound"), "Malformed dash resources reject at the exact field before allocation.");
        }
        bool waiting = true;
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 4, (_, _, _) => waiting ? new MonsterDiagnostic("WaitingNative", "bank") : null);
        registry.Refresh(true, 1); var first = registry.Register("A", 1, ov).Value!;
        Check(registry.HasWaitingSounds && registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "dashSound") == null, "Dash-only native wait retains records without a published winner.");
        waiting = false; registry.Refresh(true, 1);
        var original = registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "dashSound");
        var fire = new MonsterNativeOverride { LocalId = "fire", TargetId = d.TemplateId, NativeAttacks = { new MonsterNativeAttackParameters { AttackId = "AtkNormal", FireSound = Sound(.6f) } } };
        var fireLease = registry.Register("B", 2, fire).Value!;
        var fireInput = registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "fireSound");
        var later = new MonsterNativeOverride { LocalId = "dash", TargetId = d.TemplateId, NativeAttacks = { Dash(volume: 0), Dash("AtkSkill01", .7f) } };
        var laterLease = registry.Register("C", 3, later).Value!;
        Check(!registry.NativeAttackTargets.Any() && registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "dashSound")!.VolumeDb == 0 && ReferenceEquals(fireInput, registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "fireSound")),
            "Dash zero wins without replacing numeric holders or another sound field.");
        Check(laterLease.Snapshot.Fields.Single(f => f.Field == "nativeAttacks.AtkNormal.dashSound").RequestedResource != first.Snapshot.Fields.Single().RequestedResource &&
            registry.ResolveAttackSound(d.TemplateId, "AtkSkill01", "dashSound")!.VolumeDb == .7f, "Dash resource hashes include settings and each slot chooses independently.");
        later.NativeAttacks[0].DashSound!.VolumeDb = 1;
        Check(registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "dashSound")!.VolumeDb == 0, "Original dash inputs freeze before publication.");
        laterLease.Dispose();
        Check(ReferenceEquals(original, registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "dashSound")) && ReferenceEquals(fireInput, registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "fireSound")) &&
            registry.ResolveAttackSound(d.TemplateId, "AtkSkill01", "dashSound") == null, "Dash withdrawal restores its own surviving sources only.");
        first.Dispose(); Check(registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "dashSound") == null && ReferenceEquals(fireInput, registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "fireSound")), "Last dash withdrawal preserves the independent fire source.");
        fireLease.Dispose(); waiting = true;
        var pending = registry.Register("A", 1, ov).Value!; pending.Dispose(); Check(!registry.HasWaitingSounds, "Last dash waiter withdrawal leaves no pending sound declaration.");
        SoundValidation.ReleaseInputs(frozen);
        Check(!SoundValidation.HasSounds(frozen) && frozen.NativeAttacks[0].DashSound == null && frozen.NativeParts[0].NativeAttacks[0].DashSound == null, "Terminal root and head release all PCM inputs.");

        var wav = SoundWave(); var resource = Json.Map("assetId", "dash", "volumeDb", .3, "loop", false);
        object Row(bool both = false, bool shared = false) => both ? Json.Map("attackId", "AtkNormal", "dashSound", shared ? Json.Map("assetId", "dash", "volumeDb", .3) : resource, "fireSound", shared ? Json.Map("assetId", "dash", "volumeDb", .6) : Json.Map("assetId", "dash", "volumeDb", .6, "maxDistance", 18)) : Json.Map("attackId", "AtkNormal", "dashSound", resource);
        var definition = Json.Map("id", "variant", "templateId", d.TemplateId, "overrides", Json.Map(), "behavior", Json.Map("mode", "inherit-native"),
            "drops", Json.Map("mode", "inherit", "minCount", null, "maxCount", null), "nativeAttacks", new[] { Row(true, true) },
            "nativeParts", new[] { Json.Map("templateId", "nian_head", "nativeAttacks", new[] { Row() }) });
        var project = Json.Map("format", "dtmapi.monster-project", "version", 4, "pack", Json.Map("id", "Author", "name", "冲撞声音"), "monsters", new[] { definition },
            "nativeOverrides", new[] { Json.Map("id", "native", "targetId", d.TemplateId, "fields", Json.Map("nativeAttacks", new[] { Row(true, true) })) },
            "audioAssets", new[] { Json.Map("id", "dash", "name", "dash.wav", "dataUrl", "data:audio/wav;base64," + Convert.ToBase64String(wav)) },
            "assets", Array.Empty<object>(), "dropPools", Array.Empty<object>(), "spawnRules", Array.Empty<object>(), "dependencies", Array.Empty<object>());
        var input = ContentCodec.ReadProject(Json.Write(project)); var payload = ContentCodec.WriteContent(input);
        var roundtrip = ContentCodec.ReadContent(payload, "A", p => input.Audio[p]);
        Check(input.Audio.Count == 1 && roundtrip.Audio.Count == 0 && ContentCodec.WriteContent(roundtrip) == payload && ContentCompiler.Validate(roundtrip, c).Count == 0,
            "Fire and dash in root, head and original paths share hashed PCM without sharing their settings.");
        Check(roundtrip.Definitions[0].NativeAttacks[0].FireSound!.VolumeDb == .6f && roundtrip.Definitions[0].NativeAttacks[0].DashSound!.VolumeDb == .3f &&
            roundtrip.Definitions[0].NativeParts[0].NativeAttacks[0].DashSound!.ResourcePath == ContentCodec.AudioPath(wav), "Compiler preserves the exact nested sound fields.");
        resource["assetId"] = "missing";
        try { ContentCodec.ReadProject(Json.Write(project)); Check(false, "Missing dash PCM must reject."); }
        catch (AuthoringException error) { Check(error.Diagnostic.Code == "ResourceMissing" && error.Diagnostic.Field.Contains("dashSound"), "Missing dash PCM retains the precise resource field."); }
    }
}
