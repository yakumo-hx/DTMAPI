using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.AudioContent;
using DTMAPI.AudioContent.Authoring;
using DTMAPI.AudioContent.Preparation;
using DTMAPI.Modules.Audio;

internal static class VolumeChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws, Func<byte[]> wav)
    {
        check(AudioValidation.ToLinearVolume(0) == 1 && AudioValidation.ToLinearVolume(float.NegativeInfinity, 12) == 0,
            "0 dB is unity and explicit mute remains exact.");
        check(Math.Abs(AudioValidation.ToLinearVolume(-6.0206f) - .5f) < 1e-6 && AudioValidation.ToLinearVolume(6) > 1,
            "dB supports attenuation and amplification without a 0..1 author cap.");
        throws(() => AudioValidation.ToLinearVolume(700, 700), "Combined numeric overflow is rejected.");
        string json = "{\"resources\":[{\"key\":\"r\",\"path\":\"r.wav\"}],\"events\":[{\"key\":\"e\",\"resource\":\"r\",\"use\":\"effect\",\"volumeDb\":\"-inf\"}]}";
        check(float.IsNegativeInfinity(AudioContentReader.Parse(json).Events[0].VolumeDb), "JSON mute is explicit and unambiguous.");
        foreach (string value in new[] { "1e300", "-1e300", "\"NaN\"", "null", "true" })
            throws(() => AudioContentReader.Parse(json.Replace("\"-inf\"", value)), "Invalid dB representation is rejected: " + value);
        throws(() => AudioContentReader.Parse("{\"schemaVersion\":2," + json.Substring(1)), "Section does not accept an independent legacy schema.");
        throws(() => AudioContentReader.Parse(json.Replace("volumeDb", "gain")), "Legacy gain is not a second control field.");
        foreach (var channel in new[] { AudioRoute.Actor, AudioRoute.Ui, AudioRoute.Ambience })
        {
            var driver = new Fake(); using var engine = new AudioEngine(driver, new HashSet<uint>(), _ => { });
            using var lease = engine.Register("Volume.Test", 0, new AudioRegistration {
                Resources = new[] { new AudioResource { Key = "r", Path = "r.wav" } },
                Events = new[] { new AudioEvent { Key = "e", Resource = "r", Route = channel, VolumeDb = -12, ReplaceNativeEvent = "PLAY_VOLUME_TEST" } }
            }, _ => new Source(wav()), () => { });
            engine.Tick(true); var playback = lease.GetSound("e").Play(6);
            float expected = (float)Math.Pow(10, -6d / 20);
            check(Math.Abs(driver.Gains.Values.Single() - expected) < 1e-6, "Declaration and request offsets add once: " + channel);
            playback.SetVolumeDb(-6); playback.SetVolumeDb(6);
            check(Math.Abs(driver.Gains.Values.Single() - expected) < 1e-6, "Dynamic offset replaces its predecessor: " + channel);
            playback.SetVolumeDb(float.NegativeInfinity);
            check(driver.Gains.Values.Single() == 0, "A live voice can be muted: " + channel);
            playback.SetVolumeDb(0);
            check(Math.Abs(driver.Gains.Values.Single() - AudioValidation.ToLinearVolume(-12)) < 1e-6, "Unmuting restores declaration volume: " + channel);
            playback.Stop(); engine.Tick(true);
            object[] previousEmitters = driver.Gains.Keys.ToArray();
            check(engine.TryReplace("PLAY_VOLUME_TEST", 100, out _) && Math.Abs(driver.Gains.Single(p => !previousEmitters.Contains(p.Key)).Value - AudioValidation.ToLinearVolume(-12)) < 1e-6,
                "Native replacement adds zero dB to the declaration: " + channel);
            engine.StopSfx(); engine.Tick(true);
        }
        var plain = new WwisePcmBank.Recipe("volume.sfx", 0, WwisePcmBank.ActorBus);
        var native = new WwisePcmBank.Recipe("volume.music", WwisePcmBank.MusicParent);
        byte[] data = wav(), sfx = WwisePcmBank.Build(data, 11, 12, new[] { plain }), music = WwisePcmBank.Build(data, 11, 12, new[] { native });
        check(sfx.Length == music.Length + 1, "Only parentless recipes acquire the extra routing byte.");
        check(sfx.Length == WwisePcmBank.Estimate(PcmWav.Inspect(data).DataLength, 1, routedRecipes: 1), "Routing layout and reservation lengths agree.");
    }
}
