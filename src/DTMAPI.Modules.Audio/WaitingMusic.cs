using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.Modules.Audio
{
    // A suspended Post belonging to the native Pop that already happened. This
    // owns no list, timer, random selection, priority or synthetic completion.
    internal sealed class WaitingMusic
    {
        private sealed class Entry
        {
            internal MusicRequests.Request Request = null!;
            internal string Owner = "";
            internal Func<Action<SoundEndReceipt>, Action, MusicStart> Start = null!;
            internal Func<bool> Resume = null!;
            internal bool Failed;
        }
        private readonly MusicRequests requests;
        private readonly Action<string> log;
        private readonly Dictionary<object, Entry> pending = new Dictionary<object, Entry>();
        internal WaitingMusic(MusicRequests requests, Action<string> log) { this.requests = requests; this.log = log; }
        internal bool IsWaiting(object manager) => pending.TryGetValue(manager, out var entry) && requests.Eligible(entry.Request);
        internal bool HasWaiting => pending.Values.Any(e => requests.Eligible(e.Request));
        internal bool Post(object manager, string owner, string logical, Action<SoundEndReceipt> complete,
            Func<Action<SoundEndReceipt>, Action, MusicStart> start, Func<bool> resume, Action releaseCancelled)
        {
            var entry = new Entry { Request = requests.Begin(manager, logical, complete, releaseCancelled), Owner = owner, Start = start, Resume = resume };
            pending[manager] = entry;
            bool started = Attempt(entry, false);
            if (!started) log("MusicWaiting request=" + entry.Request.Id + "; logical=" + logical + "; playing=0; failed=" + entry.Failed);
            return started;
        }
        private bool Attempt(Entry entry, bool deferred)
        {
            var request = entry.Request;
            if (!requests.Eligible(request)) { pending.Remove(request.Manager); return false; }
            if (entry.Failed) return false;
            MusicStart result;
            try { result = entry.Start(receipt => requests.Observe(request, receipt), () => requests.Withdraw(request)); }
            catch (Exception ex) { result = new MusicStart(null, true, ex.GetBaseException().Message); }
            if (result.Voice == null)
            {
                entry.Failed = result.Failed;
                if (result.Failed) log("MusicTrackFailed request=" + request.Id + "; logical=" + request.Logical + "; reason=" + result.Detail);
                return false;
            }
            var voice = result.Voice;
            // A voice can only publish to its still-current native request.
            // Resume sets native playing/name only after a real nonzero Post.
            try
            {
                if (!requests.Eligible(request) || deferred && !entry.Resume())
                { requests.Cancel(request); voice.Stop(); pending.Remove(request.Manager); return false; }
                requests.Commit(request, voice.PlayingId, voice); pending.Remove(request.Manager);
            }
            catch { requests.Cancel(request); pending.Remove(request.Manager); voice.Stop(); throw; }
            log("MusicTrackStarted request=" + request.Id + "; playing=" + voice.PlayingId + "; deferred=" + deferred);
            return true;
        }
        internal void Pump() { foreach (var entry in pending.Values.ToArray()) Attempt(entry, true); }
        internal void Retry(string owner) { foreach (var entry in pending.Values.Where(e => e.Owner == owner)) entry.Failed = false; }
        internal void Clear() => pending.Clear();
    }
}
