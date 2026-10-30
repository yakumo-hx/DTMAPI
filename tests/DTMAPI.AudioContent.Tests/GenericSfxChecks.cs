using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DTMAPI.AudioContent;
using DTMAPI.AudioContent.Authoring;
using DTMAPI.AudioContent.Preparation;
using DTMAPI.Modules.Audio;

internal static class GenericSfxChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws, Func<byte[]> wav, string pack)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(pack, "content.json")));
        var definition = AudioContentReader.Parse(document.RootElement.GetProperty("audio").GetRawText());
        check(definition.Events.Length == 5 && definition.Events.All(e => e.Use == AudioUse.Effect && AudioValidation.ValidVolumeDb(e.VolumeDb)), "All five authored examples use the same SFX contract with independent authored volumes.");
        check(definition.Events.Single(e => e.Key == "paper-box").MinimumIntervalMs == 50 && definition.Events.Where(e => e.Key != "paper-box").All(e => e.MinimumIntervalMs == 0), "The optional cooldown remains author data.");
        var manbo = definition.Resources.Single(r => r.Key == "manbo");
        byte[] media = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(pack)!, manbo.Path));
        string json = "{\"resources\":[{\"key\":\"manbo\",\"path\":\"assets/manbo.wav\"}],\"events\":[{\"key\":\"jump\",\"resource\":\"manbo\",\"use\":\"effect\",\"replaceNativeEvent\":\"play_character_jump\"}]}";
        var input = AudioContentReader.Parse(json);
        var driver = new Fake(); using var engine = new AudioEngine(driver, new HashSet<uint>(), _ => { });
        using var lease = engine.Register("Json.Jump", 4, input, _ => new Source(media), () => { });
        engine.Tick(true);
        check(engine.TryReplace("PLAY_CHARACTER_JUMP", 0, out _) && !engine.TryReplace("PLAY_CHARACTER_JUMP_OTHER", 0, out _), "Manbo maps to an additional complete event name through JSON alone.");
        check(AudioEngine.Parent(AudioRoute.Actor) == 0 && AudioEngine.Bus(AudioRoute.Actor) == WwisePcmBank.ActorBus && AudioEngine.Bus(AudioRoute.Ui) == WwisePcmBank.UiBus && AudioEngine.Bus(AudioRoute.Ambience) == WwisePcmBank.AmbienceBus, "SFX chooses a neutral parent and existing category buses.");
        check(AudioContentReader.Parse(json.Replace("play_character_jump", "PLAY_UNLISTED_EVENT")).Events[0].ReplaceNativeEvent == "PLAY_UNLISTED_EVENT", "The static event catalog is not an admission whitelist.");
        foreach (string name in new[] { "STOP_ALL", "PAUSE_BGM", "RESUME_BGM", "PLAY_BGM_23" })
            throws(() => AudioContentReader.Parse(json.Replace("play_character_jump", name)), "SFX does not reinterpret control or music events as PCM sounds.");
        engine.StopSfx(); check(driver.Stopped.Count == 1, "StopAll's common operation stops the owned short voice.");
        var direct = new AudioRegistration { Resources = new[] { new AudioResource { Key = "direct", Path = "direct.wav" } }, Events = new[] { new AudioEvent { Key = "direct", Resource = "direct", Use = AudioUse.Effect, Route = AudioRoute.Ui } } };
        var directLease = engine.Register("Api.Consumer", 9, direct, _ => new Source(wav()), () => { });
        engine.Tick(true); var playback = directLease.GetSound("direct").Play(-4.43697499f); playback.Pause(true); playback.Pause(false); playback.SetVolumeDb(-10.457575f); playback.Stop();
        directLease.Dispose(); check(playback.HasEnded && engine.CountOwnerResources("Api.Consumer") == 0, "A DTO-only consumer registers, controls and withdraws without temporary JSON.");
        var mixed = new AudioRegistration { Resources = new[] { new AudioResource { Key = "shared", Path = "shared.wav" } }, Events = new[] {
            new AudioEvent { Key = "short", Resource = "shared", Use = AudioUse.Effect },
            new AudioEvent { Key = "music", Resource = "shared", Use = AudioUse.Music } } };
        using var mixedLease = engine.Register("Mixed.Consumer", 10, mixed, _ => new Source(wav()), () => { });
        engine.Tick(true); var shortVoice = mixedLease.GetSound("short").Play(0f); uint shortId = driver.Callbacks.Keys.Max();
        var musicVoice = mixedLease.GetSound("music").Play(0f); uint musicId = driver.Callbacks.Keys.Max();
        engine.StopSfx(); driver.Callbacks[shortId]();
        check(driver.Stopped.Contains(shortId) && !driver.Stopped.Contains(musicId) && shortVoice.HasEnded && !musicVoice.HasEnded, "SFX stop follows each voice's use even when SFX and Music share a resource/Bank; native music stop retains its own request boundary.");
        DeclarationBoundaries(check, throws, wav);
    }

    private static void DeclarationBoundaries(Action<bool, string> check, Action<Action, string> throws, Func<byte[]> wav)
    {
        var direct = new AudioRegistration {
            Resources = Enumerable.Range(0, 33).Select(i => new AudioResource { Key = "r" + i, Path = "r" + i + ".wav" }).ToArray(),
            Events = Enumerable.Range(0, 65).Select(i => new AudioEvent { Key = "e" + i, Resource = "r" + i % 33, Use = AudioUse.Effect }).ToArray()
        };
        var accepted = AudioValidation.CopyAndValidate(direct);
        string json = JsonSerializer.Serialize(new {

            resources = direct.Resources.Select(r => new { key = r.Key, path = r.Path }),
            events = direct.Events.Select(e => new { key = e.Key, resource = e.Resource, use = "effect" })
        });
        var parsed = AudioContentReader.Parse(json);
        check(accepted.Resources.Length == 33 && accepted.Events.Length == 65 && parsed.Resources.Length == 33 && parsed.Events.Length == 65,
            "JSON and direct registration accept content beyond the former declaration counts.");
        var driver = new Fake(); using var engine = new AudioEngine(driver, new HashSet<uint>(), _ => { });
        using (var declaration = engine.Register("Large.Declaration", 1, parsed, _ => new Source(wav()), () => { }))
        {
            engine.Tick(true);
            check(engine.Snapshot("Large.Declaration").State == AudioPreparationState.Ready,
                "The complete declaration prepares through the shared Bank writer: " + engine.Snapshot("Large.Declaration").Diagnostic);
            foreach (var evt in parsed.Events)
            {
                var voice = declaration.GetSound(evt.Key).Play(0f);
                driver.End(driver.Callbacks.Keys.Single());
                engine.Tick(true);
                check(voice.HasEnded, "Each declared event can play and finish independently.");
            }
            check(driver.Events.Count == 65 && engine.Snapshot("Large.Declaration").VoiceCount == 0,
                "Declaration count does not become simultaneous voice demand.");
        }
        check(driver.Empty, "The expanded declaration releases its resources normally.");

        void Reject(Action<AudioRegistration> change, string message)
        {
            var invalid = AudioValidation.CopyAndValidate(direct); change(invalid);
            throws(() => AudioValidation.CopyAndValidate(invalid), message);
        }
        Reject(v => v.Resources = Array.Empty<AudioResource>(), "Empty resources remain invalid.");
        Reject(v => v.Events = Array.Empty<AudioEvent>(), "Empty events remain invalid.");
        Reject(v => v.Resources[1].Key = v.Resources[0].Key, "Duplicate resource keys remain invalid.");
        Reject(v => v.Events[1].Key = v.Events[0].Key, "Duplicate event keys remain invalid.");
        Reject(v => v.Events[0].Resource = "missing", "Missing resource references remain invalid.");
        Reject(v => v.Resources[0].Path = "../outside.wav", "Resource paths remain contained.");
        Reject(v => v.Events[0].MinimumIntervalMs = -1, "Direct API negative intervals remain invalid.");

        string intervalJson = "{\"resources\":[{\"key\":\"r\",\"path\":\"r.wav\"}],\"events\":[{\"key\":\"e\",\"resource\":\"r\",\"use\":\"effect\",\"replaceNativeEvent\":\"PLAY_TEST_INTERVAL\",\"minimumIntervalMs\":2000}]}";
        using (engine.Register("Long.Interval", 2, AudioContentReader.Parse(intervalJson), _ => new Source(wav()), () => { }))
        {
            engine.Tick(true);
            check(engine.TryReplace("PLAY_TEST_INTERVAL", 0, out _), "A two-second interval permits its first request.");
            driver.End(driver.Callbacks.Keys.Single()); engine.Tick(true);
            check(!engine.TryReplace("PLAY_TEST_INTERVAL", 1999, out string reason) && reason == "Cooldown", "The complete two-second cooldown retains native fallback.");
            check(engine.TryReplace("PLAY_TEST_INTERVAL", 2000, out _), "The next request starts at the authored two-second boundary.");
            driver.End(driver.Callbacks.Keys.Single()); engine.Tick(true);
        }
        check(AudioContentReader.Parse(intervalJson.Replace("2000", int.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture))).Events[0].MinimumIntervalMs == int.MaxValue,
            "The JSON interval retains the full nonnegative Int32 representation.");
        foreach (string invalid in new[] { "-1", "1.5", "2147483648" })
            throws(() => AudioContentReader.Parse(intervalJson.Replace("2000", invalid)), "JSON intervals must be nonnegative Int32 integers.");
    }
}
