using System;
using System.Collections.Generic;
using DTMAPI.AudioContent;
using DTMAPI.Modules.Audio;

internal static class MusicWithdrawalChecks
{
    internal static object Run(Action<bool, string> check, Action<Action, string> throws, Func<byte[]> wav)
    {
        var trace = new List<string>(); var requests = new MusicRequests(trace.Add);
        var driver = new Fake(); using var engine = new AudioEngine(driver, new HashSet<uint>(), trace.Add);
        var manager = new NativeState(); var binding = new NativeMusicCancellation(typeof(NativeState));
        int natural = 0, cancelled = 0; bool failHandoff = false;
        IAudioRegistration Register(string key, string replacement = "PLAY_BGM_23") => engine.Register("Music.Withdrawal", 1,
            new AudioRegistration {
                Resources = new[] { new AudioResource { Key = key + "r", Path = "same.wav" } },
                Events = new[] { new AudioEvent { Key = key, Resource = key + "r", Use = AudioUse.Music, ReplaceNativeEvent = replacement } }
            }, _ => new Source(wav()), () => { });
        MusicRequests.Request Start()
        {
            MusicRequests.Request started = null!;
            bool ok = requests.Post(manager, "PLAY_BGM_23", _ => natural++, request => {
                started = request;
                request.Voice = engine.TryPlayMusic(request.Logical, receipt => requests.Observe(request, receipt), () => requests.Withdraw(request), out _);
                return request.Voice?.PlayingId ?? 0;
            }, () => {
                if (failHandoff) { failHandoff = false; throw new InvalidOperationException("cancellation handoff failure"); }
                if (binding.Release(manager, "PLAY_BGM_23")) cancelled++;
            });
            check(ok, "A real accepted engine voice is bound before the simulated native successful-Post state.");
            manager.Accept("PLAY_BGM_23"); return started;
        }

        var directLease = Register("direct"); engine.Tick(true); var direct = Start();
        directLease.Dispose();
        check(!requests.Eligible(direct) && requests.HasPendingCancellation && direct.Voice!.Ended && driver.Empty,
            "Direct withdrawal revokes End eligibility but retains the current cancellation until real owned unload termination.");
        check(manager.Playing && manager.currentBgmEventName == "PLAY_BGM_23", "Native playback gate stays owned until the cancellation pump, not a forged End.");
        requests.Observe(direct, new SoundEndReceipt(direct.PlayingId, 1)); requests.Pump();
        check(!manager.Playing && manager.currentBgmEventName == "" && cancelled == 1 && natural == 0 && !requests.HasPendingCancellation,
            "Terminated current withdrawal releases the native playback gate once without natural completion.");
        check(ReferenceEquals(manager.Playlist, manager.InitialPlaylist) && manager.Timer == 37 && !manager.Stopped,
            "Cancellation leaves the native playlist, wait timer and stopped flag unchanged.");
        requests.Pump(); check(cancelled == 1, "Repeated cleanup/pump does not repeat a successful cancellation handoff.");

        var failedLease = Register("failed"); engine.Tick(true); var failed = Start(); driver.Fail = "unload";
        throws(failedLease.Dispose, "Failed owned unload retains the real registration/Bank/voice responsibility."); requests.Pump();
        check(manager.Playing && !failed.Voice!.Ended && requests.HasPendingCancellation && engine.Snapshot("Music.Withdrawal").CleanupPendingCount == 1 && cancelled == 1,
            "An unconfirmed voice cannot release the native gate while failed cleanup remains retryable.");
        failedLease.Dispose(); failHandoff = true;
        throws(requests.Pump, "A cancellation adapter failure remains observable after native voice termination.");
        check(requests.HasPendingCancellation && manager.Playing && driver.Empty, "Native handoff responsibility survives an otherwise empty media ledger.");
        requests.Pump(); check(cancelled == 2 && !manager.Playing && !requests.HasPendingCancellation && natural == 0,
            "Retry completes only the retained current cancellation, never a natural End.");

        // Compatible independent contributions share one Bank. Stop is then
        // asynchronous; an independent direct voice is outside this request.
        var sharedLease = Register("shared"); var companion = Register("companion", ""); engine.Tick(true);
        var shared = Start(); var independent = companion.GetSound("companion").Play(0f);
        driver.Fail = "stop"; throws(sharedLease.Dispose, "An exact voice Stop failure retains the shared lease."); requests.Pump();
        check(manager.Playing && !shared.Voice!.Ended && !independent.HasEnded, "Failed/asynchronous Stop neither releases native playback nor stops the companion.");
        engine.Tick(true); driver.End(shared.PlayingId); engine.Tick(true); requests.Pump(); sharedLease.Dispose();
        check(!manager.Playing && !independent.HasEnded && cancelled == 3 && engine.Snapshot("Music.Withdrawal").BankCount == 1,
            "Real End releases the withdrawn request while the companion's shared Bank/voice stays active.");
        bool companionSurvived = !independent.HasEnded;
        companion.Dispose();

        var oldLease = Register("old"); engine.Tick(true); var old = Start();
        requests.Stop(manager, true); // Actual native Pause/Stop has already revoked this request.
        var nextLease = engine.Register("Music.NewOwner", 2, new AudioRegistration {
            Resources = new[] { new AudioResource { Key = "newr", Path = "same.wav" } },
            Events = new[] { new AudioEvent { Key = "new", Resource = "newr", Use = AudioUse.Music, ReplaceNativeEvent = "PLAY_BGM_23" } }
        }, _ => new Source(wav()), () => { }); engine.Tick(true); var next = Start();
        oldLease.Dispose(); requests.Observe(old, new SoundEndReceipt(old.PlayingId, 1)); requests.Pump();
        check(manager.Playing && manager.currentBgmEventName == "PLAY_BGM_23" && requests.Eligible(next) && !next.Voice!.Ended && cancelled == 3,
            "Withdrawing an old faded/same-name request cannot release the newer accepted request's native gate.");
        requests.RetireSession(); nextLease.Dispose(); requests.Pump();
        check(cancelled == 3 && !requests.HasPendingCancellation && driver.Empty, "Retired session cleanup cannot perform a native cancellation handoff.");

        manager.Accept("NEW_NATIVE"); check(!binding.Release(manager, "PLAY_BGM_23") && manager.Playing, "A different native logical request is never cleared.");
        manager.Accept("PLAY_BGM_23"); manager.Stop(); check(!binding.Release(manager, "PLAY_BGM_23") && manager.Stopped,
            "A native stopped manager is never resumed by an owned cancellation.");
        return new { naturalCompletions = natural, cancellationHandoffs = cancelled, independentVoiceSurvived = companionSurvived,
            nativePlaylistPreserved = ReferenceEquals(manager.Playlist, manager.InitialPlaylist), trace };
    }
    private sealed class NativeState
    {
        private bool isBgmPlaying, isBgmStoped;
        public string currentBgmEventName { get; private set; } = "";
        internal readonly object InitialPlaylist = new object();
        internal object Playlist;
        internal int Timer = 37;
        internal bool Playing => isBgmPlaying;
        internal bool Stopped => isBgmStoped;
        internal NativeState() { Playlist = InitialPlaylist; }
        internal void Accept(string logical) { currentBgmEventName = logical; isBgmPlaying = true; isBgmStoped = false; }
        internal void Stop() { isBgmStoped = true; }
    }
}
