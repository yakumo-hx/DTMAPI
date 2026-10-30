using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Authoring;
using DTMAPI.MonsterContent.Hosting;

static partial class Program
{
    static void FireSoundChecks()
    {
        MonsterNativeAttackParameters Attack(string slot = "AtkNormal", float volume = .4f) => new MonsterNativeAttackParameters {
            AttackId = slot, FireSound = new MonsterSound { ResourcePath = SharedAttackSound().ResourcePath, VolumeDb = volume } };
        MonsterNativeAttackInfo Slot(string slot, bool definition, bool native) => new MonsterNativeAttackInfo(slot, "shoot", Array.Empty<MonsterNativeAttackFieldInfo>(),
            sounds: new[] { new MonsterNativeAttackSoundInfo("fireSound", "native post", definition, native) });
        var d = Definition(); d.NativeAttacks.Add(Attack()); d.NativeAttacks[0].FireSound = SharedAttackSound(.4f);
        Check(MonsterValidation.Validate("A", d) == null, "A sound-only slot is valid without a dummy numeric edit.");
        var copy = new DefinitionRecord("A", d, 1); d.NativeAttacks[0].FireSound!.ResourcePath = "audio/changed.wav"; d.NativeAttacks[0].FireSound!.VolumeDb = .9f;
        Check(copy.Definition.NativeAttacks[0].FireSound!.ResourcePath == SharedAttackSound().ResourcePath && copy.Definition.NativeAttacks[0].FireSound!.VolumeDb == .4f, "Root resource path and playback settings freeze before publication.");
        d = copy.Definition;
        var c = AuthorCatalog(); c.Templates[0].Actions.NativeAttacks = new[] { Slot("AtkNormal", true, false), Slot("AtkSkill01", true, true) };
        c.Templates[0].NativeOverrideFields = new[] { "nativeAttacks" };
        c = CatalogCodec.Read(CatalogCodec.Write(c));
        Check(c.Templates[0].Actions.NativeAttacks[0].Fields.Count == 0 && c.Templates[0].EditableFields.Contains("nativeAttacks") && MonsterAdmission.Definition("A", d, c) == null,
            "Non-numeric sound capabilities roundtrip and admit a sound-only definition.");
        var ov = new MonsterNativeOverride { LocalId = "fire", TargetId = d.TemplateId, NativeAttacks = { Attack() } };
        Check(MonsterOverrideAdmission.Validate("A", ov, c)?.Field == "fields.nativeAttacks[0].fireSound", "Definition capability does not grant native firing overrides.");
        c.Templates[0].Actions.NativeAttacks = new[] { Slot("AtkNormal", true, true), Slot("AtkSkill01", true, true) };
        Check(MonsterOverrideAdmission.Validate("A", ov, c) == null, "Exact native slot sound capability admits its resource.");
        foreach (var change in new Action<MonsterNativeAttackParameters>[] { a => a.FireSound!.VolumeDb = float.NaN, a => a.FireSound!.ResourcePath = "", a => a.FireSound!.ResourcePath = "../escape.wav" })
        { var bad = Definition(); bad.NativeAttacks.Add(Attack()); change(bad.NativeAttacks[0]); Check(MonsterValidation.Validate("A", bad)?.Code == "InvalidSound", "Malformed slot resource is rejected before native allocation."); }
        var empty = Definition(); empty.NativeAttacks.Add(Attack()); empty.NativeAttacks[0].Shoot = new MonsterShootParameters();
        Check(MonsterValidation.Validate("A", empty)?.Code == "InvalidNativeAttack", "A sound cannot conceal an explicitly empty specialized numeric group.");
        bool pending = true;
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 7, (_, _, _) => pending ? new MonsterDiagnostic("WaitingNative", "bank") : null);
        registry.Refresh(true, 1); var aLease = registry.Register("A", 1, ov).Value!;
        Check(registry.HasWaitingSounds && registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "fireSound") == null, "Sound-only native wait retains declarations without publishing a winner.");
        pending = false; registry.Refresh(true, 1);
        var original = registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "fireSound");
        Check(original != null && !registry.NativeAttackTargets.Any(), "A fire-only override never requests numeric prototype replacement.");
        string hash = aLease.Snapshot.Fields.Single().RequestedResource!;
        var b = new MonsterNativeOverride { LocalId = "fire", TargetId = d.TemplateId, NativeAttacks = { Attack(volume: 0), Attack("AtkSkill01", .7f) } };
        var bLease = registry.Register("B", 2, b).Value!;
        Check(registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "fireSound")!.VolumeDb == 0 && registry.ResolveAttackSound(d.TemplateId, "AtkSkill01", "fireSound")!.VolumeDb == .7f &&
            bLease.Snapshot.Fields.Single(f => f.Field == "nativeAttacks.AtkNormal.fireSound").RequestedResource != hash, "Each slot selects a whole resource including zero gain, with a settings-sensitive resource hash.");
        b.NativeAttacks[0].FireSound!.VolumeDb = 1;
        Check(registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "fireSound")!.VolumeDb == 0, "Native slot resource settings are frozen independently of caller mutations.");
        var numeric = new MonsterNativeOverride { LocalId = "damage", TargetId = d.TemplateId, NativeAttacks = { new MonsterNativeAttackParameters { AttackId = "AtkNormal", Damage = 20 } } };
        var nLease = registry.Register("C", 3, numeric).Value!;
        Check(registry.NativeAttackTargets.Single() == d.TemplateId && registry.Resolve(d.TemplateId, "nativeAttacks.AtkNormal.damage", 1) == 20 && registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "fireSound")!.VolumeDb == 0,
            "Numeric and resource fields on one slot retain separate winners and value representations.");
        bLease.Dispose(); Check(ReferenceEquals(registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "fireSound"), original) && registry.ResolveAttackSound(d.TemplateId, "AtkSkill01", "fireSound") == null, "Withdrawal restores only surviving slot sources.");
        nLease.Dispose(); aLease.Dispose(); Check(!registry.NativeAttackTargets.Any() && registry.ResolveAttackSound(d.TemplateId, "AtkNormal", "fireSound") == null, "Last withdrawal restores inheritance without numeric adapters.");
        pending = true; var waiting = registry.Register("A", 1, ov).Value!; waiting.Dispose(); Check(!registry.HasWaitingSounds, "Withdrawing the last fire-only waiter leaves no pending sound declaration.");

        var part = new MonsterNativePartParameters { TemplateId = "nian_tail", NativeAttacks = { Attack() } }; part.NativeAttacks[0].FireSound = SharedAttackSound();
        d.NativeParts.Add(part); c.Templates[0].Actions.NativeParts = new[] { new MonsterNativePartInfo("nian_tail", new[] { Slot("AtkNormal", true, false) }) };
        Check(MonsterAdmission.Definition("A", d, c) == null && c.Templates[0].EditableFields.Contains("nativeParts"), "An explicitly admitted internal part supports a sound-only slot.");
        var frozen = new DefinitionRecord("A", d, 1).Definition; part.NativeAttacks[0].FireSound!.ResourcePath = "audio/changed.wav";
        Check(frozen.NativeParts[0].NativeAttacks[0].FireSound!.ResourcePath == ContentCodec.AudioPath(SoundWave()), "Internal part resource path freezes independently.");
        SoundValidation.ReleaseInputs(frozen);
        Check(!SoundValidation.HasSounds(frozen) && frozen.NativeAttacks[0].FireSound == null && frozen.NativeParts[0].NativeAttacks[0].FireSound == null, "Terminal definitions release both root and internal-part resource inputs.");

        var wav = SoundWave(); var resource = Json.Map("assetId", "fire", "volumeDb", .4);
        object Row(bool shared = false) => Json.Map("attackId", "AtkNormal", "fireSound", shared ? Json.Map("assetId", "fire", "volumeDb", .4) : resource);
        var definition = Json.Map("id", "variant", "templateId", d.TemplateId, "overrides", Json.Map(), "behavior", Json.Map("mode", "inherit-native"),
            "drops", Json.Map("mode", "inherit", "minCount", null, "maxCount", null), "nativeAttacks", new[] { Row(true) },
            "nativeParts", new[] { Json.Map("templateId", "nian_tail", "nativeAttacks", new[] { Row() }) });
        var project = Json.Map("format", "dtmapi.monster-project", "version", 4, "pack", Json.Map("id", "Author", "name", "发射声音"), "monsters", new[] { definition },
            "nativeOverrides", new[] { Json.Map("id", "native", "targetId", d.TemplateId, "fields", Json.Map("nativeAttacks", new[] { Row(true) })) },
            "audioAssets", new[] { Json.Map("id", "fire", "name", "fire.wav", "dataUrl", "data:audio/wav;base64," + Convert.ToBase64String(wav)) },
            "assets", Array.Empty<object>(), "dropPools", Array.Empty<object>(), "spawnRules", Array.Empty<object>(), "dependencies", Array.Empty<object>());
        var input = ContentCodec.ReadProject(Json.Write(project)); string payload = ContentCodec.WriteContent(input);
        var roundtrip = ContentCodec.ReadContent(payload, "A", p => input.Audio[p]);
        Check(input.Audio.Count == 1 && roundtrip.Audio.Count == 0 && ContentCodec.WriteContent(roundtrip) == payload && ContentCompiler.Validate(roundtrip, c).Count == 0,
            "Root, part and original override share one hashed WAV through the ordinary compiler/runtime payload.");
        Check(roundtrip.Definitions[0].NativeParts[0].NativeAttacks[0].FireSound!.VolumeDb == .4f && roundtrip.NativeOverrides[0].NativeAttacks[0].FireSound!.ResourcePath == ContentCodec.AudioPath(wav), "All three nested author paths retain their independent sound settings.");
        bool read = false;
        try { ContentCodec.ReadContent(payload.Replace(ContentCodec.AudioPath(wav), "../escape.wav"), "A", _ => { read = true; return wav; }); Check(false, "Traversal must reject."); }
        catch (AuthoringException error) { Check(error.Diagnostic.Code == "InvalidAudio" && !read, "Nested attack audio is path-validated before any package read."); }
        resource["assetId"] = "missing";
        try { ContentCodec.ReadProject(Json.Write(project)); Check(false, "Missing attack audio must reject."); }
        catch (AuthoringException error) { Check(error.Diagnostic.Code == "ResourceMissing" && error.Diagnostic.Field.Contains("fireSound"), "Missing slot resources retain the attack sound field diagnostic."); }
    }
}
