using System;
using System.Linq;
using DTMAPI.MonsterContent;
using DTMAPI.MonsterContent.Hosting;
using DTMAPI.MonsterContent.Authoring;

static partial class Program
{
    static void NativeSoundChecks()
    {
        MonsterNativeOverride Input(string id = "hurt", float gain = .4f) => new MonsterNativeOverride {
            LocalId = id, TargetId = "test-template", Sounds = new MonsterSounds { Hurt = new MonsterSound { ResourcePath = "audio/native.wav", VolumeDb = gain } } };
        var catalog = AuthorCatalog(); catalog.Templates[0].SoundEvents = new[] { "hurt", "dead", "humming" };
        Check(MonsterOverrideAdmission.Validate("A", Input(), catalog)?.Code == "NativeOverrideUnsupported", "New-definition sound support cannot grant native override support.");
        catalog.Templates[0].NativeOverrideFields = new[] { "sounds.hurt" };
        catalog = CatalogCodec.Read(CatalogCodec.Write(catalog));
        Check(MonsterOverrideAdmission.Validate("A", Input(), catalog) == null, "Exact native Hurt capability is independent of definition inheritance.");
        foreach (var mutate in new Action<MonsterNativeOverride>[] { v => v.Sounds = new MonsterSounds(), v => v.Sounds!.Hurt!.VolumeDb = float.NaN,
            v => v.Sounds!.Hurt!.ResourcePath = "", v => v.Sounds!.Hurt!.ResourcePath = "../escape.wav" })
        { var value = Input(); mutate(value); Check(MonsterOverrideAdmission.Validate("A", value)?.Code == "InvalidSound", "Malformed native sound intent is rejected before registration."); }
        var hummingOnly = Input("humming"); hummingOnly.Sounds!.Humming = hummingOnly.Sounds.Hurt; hummingOnly.Sounds.Hurt = null;
        Check(MonsterOverrideAdmission.Validate("A", hummingOnly, catalog)?.Code == "NativeOverrideUnsupported", "Other native events do not grant Humming support.");
        var deadOnly = Input("dead"); deadOnly.Sounds!.Dead = deadOnly.Sounds.Hurt; deadOnly.Sounds.Hurt = null;
        Check(MonsterOverrideAdmission.Validate("A", deadOnly, catalog)?.Code == "NativeOverrideUnsupported", "Native Hurt does not grant the independent Dead capability.");
        catalog.Templates[0].NativeOverrideFields = new[] { "sounds.hurt", "sounds.dead" };
        catalog = CatalogCodec.Read(CatalogCodec.Write(catalog));
        Check(MonsterOverrideAdmission.Validate("A", deadOnly, catalog) == null, "Exact native Dead capability roundtrips independently.");
        Check(MonsterOverrideAdmission.Validate("A", hummingOnly, catalog)?.Code == "NativeOverrideUnsupported", "Hurt plus Dead still do not grant Humming.");
        catalog.Templates[0].NativeOverrideFields = new[] { "sounds.hurt", "sounds.dead", "sounds.humming" };
        catalog = CatalogCodec.Read(CatalogCodec.Write(catalog));
        Check(MonsterOverrideAdmission.Validate("A", hummingOnly, catalog) == null, "Exact original Humming capability roundtrips independently.");
        bool pending = true; string seenOwner = "", seenId = "";
        var registry = new NativeOverrideRegistry(() => { }, (_, _) => 2, (owner, id, v) => {
            seenOwner = owner; seenId = id; return pending ? new MonsterDiagnostic("WaitingNative", "native sound resource not ready") : null; });
        registry.Refresh(true, 1);
        var original = Input(); var a = registry.Register("A", 2, original).Value!;
        Check(a.Snapshot.State == MonsterOverrideState.WaitingNative && registry.HasWaitingSounds && registry.ResolveHurtSound(original.TargetId) == null,
            "Deferred native sound preparation retains a lease without publishing a winner.");
        Check(seenOwner == "A" && seenId == "A:hurt", "Native resource admission receives trusted owner and registration identity.");
        original.Sounds!.Hurt!.VolumeDb = .9f; original.Sounds.Hurt.ResourcePath = "audio/changed.wav";
        pending = false; registry.Refresh(true, 1);
        var frozen = registry.ResolveHurtSound(original.TargetId)!;
        Check(a.Snapshot.State == MonsterOverrideState.Ready && !registry.HasWaitingSounds && frozen.VolumeDb == .4f && frozen.ResourcePath == "audio/native.wav", "Waiting registrations freeze independent resource paths and settings.");
        string hashA = a.Snapshot.Fields.Single().RequestedResource!;
        var b = registry.Register("B", 3, Input(gain: 0)).Value!;
        Check(registry.ResolveHurtSound(original.TargetId)!.VolumeDb == 0 && a.Snapshot.Fields.Single().State == MonsterOverrideFieldState.Shadowed &&
            b.Snapshot.Fields.Single().WinnerId == "B:hurt" && b.Snapshot.Fields.Single().RequestedResource != hashA, "Higher owner order selects the complete sound, including explicit silence and settings identity.");
        b.Dispose(); Check(ReferenceEquals(registry.ResolveHurtSound(original.TargetId), frozen), "Withdraw restores the previous frozen sound resource.");
        var later = registry.Register("A", 2, Input("later", .7f)).Value!;
        Check(registry.ResolveHurtSound(original.TargetId)!.VolumeDb == .7f, "Same-owner later declarations arbitrate by the trusted declaration order.");
        later.Dispose(); a.Dispose(); Check(registry.ResolveHurtSound(original.TargetId) == null, "Withdrawing the last sound restores native inheritance.");

        var both = Input("both"); both.Sounds!.Dead = new MonsterSound { ResourcePath = "audio/native.wav", VolumeDb = .3f };
        both.Sounds.Humming = new MonsterSound { ResourcePath = "audio/native.wav", VolumeDb = .25f };
        var bothLease = registry.Register("A", 2, both).Value!;
        both.Sounds.Dead.ResourcePath = "audio/changed.wav"; both.Sounds.Dead.VolumeDb = .9f;
        both.Sounds.Humming.ResourcePath = "audio/changed.wav"; both.Sounds.Humming.VolumeDb = .9f;
        Check(registry.ResolveDeadSound("test-template")!.VolumeDb == .3f && registry.ResolveDeadSound("test-template")!.ResourcePath == "audio/native.wav", "Original Dead freezes settings and PCM independently.");
        Check(registry.ResolveHummingSound("test-template")!.VolumeDb == .25f && registry.ResolveHummingSound("test-template")!.ResourcePath == "audio/native.wav", "Original Humming freezes its own settings and PCM.");
        var hurtWinner = registry.Register("B", 3, Input(gain: .7f)).Value!;
        Check(registry.ResolveHurtSound("test-template")!.VolumeDb == .7f && registry.ResolveDeadSound("test-template")!.VolumeDb == .3f &&
            bothLease.Snapshot.Fields.Single(f => f.Field == "sounds.dead").State == MonsterOverrideFieldState.Effective, "Hurt and Dead select independent owners rather than one sounds-object winner.");
        deadOnly.Sounds.Dead!.VolumeDb = 0; var deadWinner = registry.Register("C", 4, deadOnly).Value!;
        Check(registry.ResolveDeadSound("test-template")!.VolumeDb == 0 && registry.ResolveHurtSound("test-template")!.VolumeDb == .7f &&
            deadWinner.Snapshot.Fields.Single().RequestedResource != bothLease.Snapshot.Fields.Single(f => f.Field == "sounds.dead").RequestedResource, "Explicit Dead silence has its own SET identity and does not change Hurt.");
        hummingOnly.Sounds.Humming!.VolumeDb = 0; var hummingWinner = registry.Register("D", 5, hummingOnly).Value!;
        Check(registry.ResolveHummingSound("test-template")!.VolumeDb == 0 && registry.ResolveDeadSound("test-template")!.VolumeDb == 0 && registry.ResolveHurtSound("test-template")!.VolumeDb == .7f &&
            hummingWinner.Snapshot.Fields.Single().RequestedResource != bothLease.Snapshot.Fields.Single(f => f.Field == "sounds.humming").RequestedResource, "Humming silence has its own whole-field winner and resource identity, preserving both other event winners.");
        hummingWinner.Dispose(); Check(registry.ResolveHummingSound("test-template")!.VolumeDb == .25f, "Humming withdrawal reveals only the previous Humming source.");
        deadWinner.Dispose(); Check(registry.ResolveDeadSound("test-template")!.VolumeDb == .3f, "Dead withdrawal restores only the prior Dead source.");
        hurtWinner.Dispose(); bothLease.Dispose(); Check(registry.ResolveDeadSound("test-template") == null && registry.ResolveHurtSound("test-template") == null && registry.ResolveHummingSound("test-template") == null, "All native sound events return to inheritance after withdrawal.");
        pending = true; var waitingLoop = registry.Register("A", 2, hummingOnly).Value!;
        Check(registry.HasWaitingSounds && waitingLoop.Snapshot.State == MonsterOverrideState.WaitingNative && registry.ResolveHummingSound("test-template") == null, "A native Humming-only waiter publishes no winner.");
        waitingLoop.Dispose(); Check(!registry.HasWaitingSounds, "Withdraw cancels the last native Humming-only waiting declaration.");

        bool fail = false; int refreshes = 0;
        var retries = new NativeOverrideRegistry(() => { }, (_, _) => 0, (_, _, _) => null, () => { refreshes++; if (fail) { fail = false; throw new Exception("retirement"); } });
        retries.Refresh(true, 1); var lease = retries.Register("A", 1, Input()).Value!;
        fail = true; try { lease.Dispose(); Check(false, "Expected retained cleanup"); } catch (Exception) { Check(true, "A failed native retirement does not consume the lease action."); }
        int first = refreshes; lease.Dispose();
        Check(refreshes == first + 1 && retries.CountOwner("A") == 0, "A previously withdrawn registration still retries the native changed callback.");
        first = refreshes; retries.RemoveOwner("A"); Check(refreshes == first, "An already released Owner has no remaining native work.");
        fail = true; var failed = retries.Register("A", 1, Input());
        Check(!failed.Success && failed.Diagnostic!.Code == "NativeOverrideApplyFailed" && retries.CountOwner("A") == 1 && retries.ResolveHurtSound("test-template") == null,
            "Failed native application retains its cleanup lease and removes the failed candidate from arbitration.");
        failed.Value!.Dispose(); Check(retries.CountOwner("A") == 0, "Returned failure lease completes pending cleanup.");

        var project = Json.Map("format", "dtmapi.monster-project", "version", 4, "pack", Json.Map("id", "A", "name", "原版受伤"),
            "monsters", Array.Empty<object>(), "assets", Array.Empty<object>(), "dropPools", Array.Empty<object>(), "spawnRules", Array.Empty<object>(), "dependencies", Array.Empty<object>(),
            "audioAssets", new[] { Json.Map("id", "ouch", "name", "原创.wav", "dataUrl", "data:audio/wav;base64," + Convert.ToBase64String(SoundWave())) },
            "nativeOverrides", new[] { Json.Map("id", "hurt", "targetId", "test-template", "fields", Json.Map("sounds", Json.Map("hurt", Json.Map("assetId", "ouch", "volumeDb", .3)))) });
        var input = ContentCodec.ReadProject(Json.Write(project)); var payload = ContentCodec.WriteContent(input);
        var read = ContentCodec.ReadContent(payload, "A", p => input.Audio[p]);
        Check(ContentCompiler.Validate(read, catalog).Count == 0 && read.Audio.Count == 0 && read.Definitions.Count == 0 && read.NativeOverrides[0].Sounds!.Hurt!.VolumeDb == .3f &&
            ContentCodec.WriteContent(read) == payload, "An override-only editor project compiles and roundtrips its PCM and settings through normal PackData.");
        var dual = Json.Map("hurt", Json.Map("assetId", "ouch", "volumeDb", .5));
        project["monsters"] = new[] { Json.Map("id", "own", "templateId", "test-template", "overrides", Json.Map(), "behavior", Json.Map("mode", "inherit-native"),
            "drops", Json.Map("mode", "inherit", "minCount", null, "maxCount", null), "sounds", dual) };
        input = ContentCodec.ReadProject(Json.Write(project)); read = ContentCodec.ReadContent(ContentCodec.WriteContent(input), "A", p => input.Audio[p]);
        Check(read.Audio.Count == 0 && read.NativeOverrides[0].Sounds!.Hurt!.VolumeDb == .3f && read.Definitions[0].Sounds!.Hurt!.VolumeDb == .5f,
            "Native and new-definition events share packaged PCM bytes while retaining independent settings.");
        var nativeFields = (System.Collections.Generic.Dictionary<string, object?>)((System.Collections.Generic.Dictionary<string, object?>[])project["nativeOverrides"]!)[0]["fields"]!;
        ((System.Collections.Generic.Dictionary<string, object?>)nativeFields["sounds"]!)["dead"] = Json.Map("assetId", "ouch", "volumeDb", .8);
        ((System.Collections.Generic.Dictionary<string, object?>)nativeFields["sounds"]!)["humming"] = Json.Map("assetId", "ouch", "volumeDb", .2);
        input = ContentCodec.ReadProject(Json.Write(project)); read = ContentCodec.ReadContent(ContentCodec.WriteContent(input), "A", p => input.Audio[p]);
        Check(ContentCompiler.Validate(read, catalog).Count == 0 && read.Audio.Count == 0 && read.NativeOverrides[0].Sounds!.Dead!.VolumeDb == .8f &&
            read.NativeOverrides[0].Sounds!.Hurt!.VolumeDb == .3f && read.NativeOverrides[0].Sounds!.Humming!.VolumeDb == .2f,
            "All original events and independent definitions share one packaged WAV while keeping each event's author settings.");
    }
}
