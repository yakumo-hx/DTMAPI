using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.AudioContent;
using DTMAPI.Modules.Audio;

internal static class MusicEventChecks
{
    internal static object Run(Action<bool, string> check, Action<Action, string> throws, Func<byte[]> wav)
    {
        var trace = new List<string>();
        AudioRegistration Definition(string id) => AudioValidation.CopyAndValidate(new AudioRegistration {
            Resources = new[] { new AudioResource { Key = id, Path = id + ".wav" } },
            Events = new[] { new AudioEvent { Key = id, Resource = id, Use = AudioUse.Music, NativeEvent = "CUSTOM_" + id } } });
        var parsed = AudioContentReader.Parse("""
            {"resources":[{"key":"A","path":"A.wav"}],"events":[{"key":"A","resource":"A","use":"music","nativeEvent":"CUSTOM_A"}]}
            """);
        check(parsed.Events[0].NativeEvent == "CUSTOM_A", "JSON and direct DTO bind the same native music name.");
        parsed.Events[0].ReplaceNativeEvent = "PLAY_BGM_23";
        throws(() => AudioValidation.CopyAndValidate(parsed), "An event cannot both map added music and request native replacement fallback.");
        throws(() => AudioContentReader.Parse("""{"resources":[],"events":[],"tracks":[]}"""), "Retired Tracks metadata is no longer accepted by the audio codec.");

        var requests = new MusicRequests(trace.Add); var waiting = new WaitingMusic(requests, trace.Add);
        var manager = new State(); var binding = new NativeMusicCancellation(typeof(State));
        var driver = new Fake(); using var engine = new AudioEngine(driver, new HashSet<uint>(), trace.Add);
        var musicInput = Definition("P");
        var source = new Source(wav());
        using var lease = engine.Register("Player", 1, musicInput, _ => source, () => { });
        check(source.Reads == 0, "Added music admission holds descriptors without inspecting media for an item/catalog gate.");
        var overlapping = Definition("Q"); overlapping.Events[0].NativeEvent = "custom_p";
        using (var sibling = engine.Register("Player", 1, overlapping, _ => new Source(wav()), () => { }))
        {
            var captured = engine.CaptureMusicEvent("CUSTOM_P", out _)!;
            sibling.Dispose();
            check(captured(_ => { }, () => { }).Failed, "Same-Owner arbitration captures the newest accepted registration; its withdrawal cannot address the older sibling.");
        }
        using (var later = engine.Register("Later", 2, overlapping, _ => new Source(wav()), () => { }))
        {
            engine.CaptureMusicEvent("CUSTOM_P", out string selectedOwner);
            check(selectedOwner == "Later", "Music bindings use trusted load order, including before media is ready.");
        }
        int ended = 0, attempts = 0;
        var start = engine.CaptureMusicEvent("custom_p", out string mappedOwner) ?? throw new Exception("Music event mapping missing");
        check(mappedOwner == "Player" && engine.CaptureMusicEvent("unknown", out _) == null,
            "Official logical event resolves an audio-only registration before preparation, without any CD/item/shop metadata.");
        bool Post() => waiting.Post(manager, "Player", "P", _ => ended++, (receipt, invalidate) => { attempts++; return start(receipt, invalidate); },
            () => binding.Resume(manager, "P"), () => binding.Release(manager, "P"));
        check(!Post() && waiting.IsWaiting(manager) && !manager.Playing && manager.currentBgmEventName == "" && driver.Callbacks.Count == 0,
            "Cold added track retains the exact native request while reporting no playing voice.");
        requests.Cancel(manager); engine.Tick(true); waiting.Pump();
        check(!waiting.IsWaiting(manager) && driver.Callbacks.Count == 0 && attempts == 1, "Switch/stop revokes the pending request; later readiness cannot post it.");
        // Exercise deferred readiness with the same already-Popped logical track.
        bool ready = false;
        waiting.Post(manager, "Player", "P", _ => ended++, (receipt, invalidate) => ready ? start(receipt, invalidate) : new MusicStart(null, false, "Preparing"),
            () => binding.Resume(manager, "P"), () => binding.Release(manager, "P"));
        waiting.Pump(); ready = true; waiting.Pump();
        check(!waiting.IsWaiting(manager) && manager.Playing && manager.currentBgmEventName == "P" && driver.Callbacks.Count == 1 && ended == 0,
            "Readiness resumes exactly that native request after one real Post, without another selection or fake End.");
        uint id = driver.Callbacks.Keys.Single(); driver.End(id); requests.Pump();
        check(ended == 1, "Only the matching real End completes a resumed added-track request.");
        manager.Clear(); int failures = 0;
        waiting.Post(manager, "Player", "P", _ => ended++, (_, _) => { failures++; return new MusicStart(null, true, "MediaFailed"); },
            () => binding.Resume(manager, "P"), () => binding.Release(manager, "P"));
        for (int i = 0; i < 50; i++) waiting.Pump();
        check(failures == 1 && waiting.IsWaiting(manager) && !manager.Playing && ended == 1, "A failed added track stays truthful and cannot cause a fast Post/Pop loop or forged completion.");
        requests.RetireSession(); waiting.Pump(); check(!waiting.HasWaiting, "Session retirement clears failed and pending requests.");
        lease.Dispose();
        using var replacement = engine.Register("Player", 1, musicInput, _ => new Source(wav()), () => { });
        engine.Tick(true);
        check(start(_ => { }, () => { }).Failed && engine.CaptureMusicEvent("CUSTOM_P", out _) != null,
            "An old waiting request cannot borrow a later registration with the same event name.");
        return new { trace, scope = "Audio event mapping and real shared engine/request code; official content is author-owned" };
    }
    private sealed class State
    {
        private bool isBgmPlaying, isBgmStoped;
        public string currentBgmEventName { get; private set; } = "";
        internal bool Playing => isBgmPlaying;
        internal void Clear() { isBgmPlaying = false; isBgmStoped = false; currentBgmEventName = ""; }
    }
}
