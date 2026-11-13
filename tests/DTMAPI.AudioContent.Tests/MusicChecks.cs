using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.AudioContent;
using DTMAPI.Modules.Audio;

internal static class MusicChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws, Func<byte[]> wav)
    {
        var parsed = AudioContentReader.Parse("{\"resources\":[{\"key\":\"r\",\"path\":\"short.wav\"}],\"events\":[{\"key\":\"a\",\"resource\":\"r\",\"use\":\"music\",\"replaceNativeEvent\":\"PLAY_BGM_23\",\"volumeDb\":-6.0206}]}");
        check(parsed.Events[0].Use == AudioUse.Music, "Music direct/JSON declarations share validated finite recipes.");
        parsed.Events[0].Loop = true; throws(() => AudioValidation.CopyAndValidate(parsed), "Music must terminate through its real primitive End."); parsed.Events[0].Loop = false;
        parsed.Events[0].ReplaceNativeEvent = "STOP_BGM_IMMEDIATE"; throws(() => AudioValidation.CopyAndValidate(parsed), "A music definition cannot replace native Stop intent."); parsed.Events[0].ReplaceNativeEvent = "PLAY_BGM_23";

        var log = new List<string>(); var requests = new MusicRequests(log.Add); var manager = new object(); int completed = 0;
        var original = requests.Begin(manager, "PLAY_BGM_23", _ => completed++);
        requests.Commit(original, 700);
        requests.Stop(manager, true);
        var driver = new Fake(); using var engine = new AudioEngine(driver, new HashSet<uint>(), log.Add);
        using var lease = engine.Register("Music.Owner", 1, parsed, _ => new Source(wav()), () => { }); engine.Tick(true);
        MusicRequests.Request Start()
        {
            var r = requests.Begin(manager, "PLAY_BGM_23", _ => completed++);
            var v = engine.TryPlayMusic(r.Logical, receipt => requests.Observe(r, receipt), () => { if (r.Voice != null) requests.Cancel(r); }, out _)!;
            check(v != null && requests.Commit(r, v.PlayingId, v), "Ordinary accepted request binds its own actual voice identity.");
            return r;
        }
        var a1 = Start();
        requests.Observe(original, new SoundEndReceipt(700, 1)); requests.Pump();
        check(completed == 0 && !a1.Voice!.Ended, "Old same-name original End cannot finish a new replacement request.");
        requests.Stop(manager, true); requests.Stop(manager, true);
        check(driver.Stops.Count == 0, "Selected P observes native intent without forwarding a second stop.");
        a1.Voice!.Stop(5000, 3); a1.Voice.Stop(5000, 3);
        check(driver.Stops.Count == 1 && driver.Stops[0] == (a1.PlayingId, 5000, 3), "Common voice retains 5000ms/curve3 once; duplicate fade does not restart it.");
        var a2 = Start();
        driver.End(a1.PlayingId); requests.Pump(); engine.Tick(true);
        check(completed == 0 && !a2.Voice!.Ended && !driver.Stopped.Contains(a2.PlayingId), "Old owned A1 End cleans A1 without stopping same-name A2.");
        requests.Observe(a2, new SoundEndReceipt(a2.PlayingId + 100, 1)); requests.Observe(a2, new SoundEndReceipt(a2.PlayingId, 2)); requests.Pump();
        check(completed == 0, "Wrong playing ID and non-End receipts cannot complete a request.");
        driver.End(a2.PlayingId); requests.Pump(); requests.Observe(a2, new SoundEndReceipt(a2.PlayingId, 1)); requests.Pump(); engine.Tick(true);
        check(completed == 1, "The matching current End delivers exactly once.");
        var fade = Start(); requests.Stop(manager, true); fade.Voice!.Stop(5000, 3); requests.Stop(manager, false); fade.Voice.Stop(); engine.Tick(true);
        check(driver.Stops.TakeLast(2).Select(s => s.Ms).SequenceEqual(new[] { 5000, 0 }) && driver.Stops.TakeLast(2).All(s => s.Id == fade.PlayingId), "Immediate upgrades the exact fading voice, and Tick never reissues the fade.");
        var otherManager = new object(); var other = requests.Begin(otherManager, "PLAY_BGM_23", _ => completed++); requests.Commit(other, 701);
        requests.Stop(manager, false); requests.Observe(other, new SoundEndReceipt(701, 1)); requests.Pump();
        check(completed == 2, "A manager's stop leaves another manager's native request eligible.");
        var withdrawing = Start(); lease.Dispose();
        requests.Observe(withdrawing, new SoundEndReceipt(withdrawing.PlayingId, 1)); requests.Pump();
        check(completed == 2 && driver.Empty, "Owner withdrawal invalidates first, then releases only owned resources.");

        requests.Post(manager, "A", _ => completed++, early => { requests.Observe(early, new SoundEndReceipt(800, 1)); return 800; }); requests.Pump();
        check(completed == 3, "A receipt arriving inside Post is retained until request acceptance.");
        check(!requests.Post(manager, "A", _ => completed++, zero => { requests.Observe(zero, new SoundEndReceipt(801, 1)); return 0; }), "Post zero is rejected instead of falsely declaring playback."); requests.Pump();
        var cancelled = requests.Begin(manager, "A", _ => completed++); requests.Commit(cancelled, 802); requests.RetireSession(); requests.Observe(cancelled, new SoundEndReceipt(802, 1)); requests.Pump();
        check(completed == 3, "Cancelled/previous-session receipts cannot complete or resurrect playback.");
        var fallback = requests.Begin(manager, "A", _ => completed++);
        using var second = engine.Register("Music.Owner", 1, parsed, _ => new Source(wav()), () => { }); engine.Tick(true);
        driver.ZeroPlay = true;
        var failed = engine.TryPlayMusic("PLAY_BGM_23", r => requests.Observe(fallback, r), () => { if (fallback.Voice != null) requests.Cancel(fallback); }, out var detail);
        check(failed == null && detail.StartsWith("PostFailed:") && driver.Emitters.Count == 0 && driver.Callbacks.Count == 0, "Failed owned Post clears its callback/emitter and keeps fallback eligible.");
        check(requests.Commit(fallback, 900), "A failed replacement can accept the original voice under the same request.");
        requests.Observe(fallback, new SoundEndReceipt(900, 1)); requests.Pump(); check(completed == 4, "Original fallback delivers its actual End once.");
        driver.ZeroPlay = false; driver.EarlyEnd = true;
        var instant = Start(); requests.Pump(); engine.Tick(true);
        check(instant.Voice!.Ended && completed == 5 && driver.Emitters.Count == 0, "Early native End survives voice construction, acceptance and main-thread cleanup.");
        MusicRequests.Request exception = null!;
        throws(() => requests.Post(manager, "A", _ => completed++, r => { exception = r; throw new InvalidOperationException("Post exception"); }), "The actual music Post scope propagates a native Post exception.");
        requests.Observe(exception, new SoundEndReceipt(999, 1)); requests.Pump();
        check(completed == 5, "An exception scope leaves no eligible completion behind.");
    }
}
