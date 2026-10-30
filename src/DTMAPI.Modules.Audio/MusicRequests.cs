using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.Modules.Audio
{
    // Request eligibility is independent from the lifetime of its actual voice.
    // This adapter has no playlist, clock, next-track or media-preparation loop.
    internal sealed class MusicRequests
    {
        internal sealed class Request
        {
            internal readonly object Manager;
            internal readonly string Logical;
            internal readonly long Id, Session;
            internal readonly Action<SoundEndReceipt> Complete;
            internal readonly Action? ReleaseCancelled;
            internal bool Accepted, Cancelled, Delivered;
            internal uint PlayingId;
            internal OwnedSoundBank.Voice? Voice;
            internal Request(object manager, string logical, long id, long session, Action<SoundEndReceipt> complete, Action? releaseCancelled)
            { Manager = manager; Logical = logical; Id = id; Session = session; Complete = complete; ReleaseCancelled = releaseCancelled; }
        }
        private readonly Dictionary<object, Request> current = new Dictionary<object, Request>();
        private readonly ConcurrentQueue<Tuple<Request, SoundEndReceipt>> receipts = new ConcurrentQueue<Tuple<Request, SoundEndReceipt>>();
        private readonly Action<string> log;
        private long sequence, session;
        internal MusicRequests(Action<string> log) { this.log = log; }
        internal bool HasNativeCurrent => current.Values.Any(r => r.Accepted && r.Voice == null);
        internal bool HasPendingCancellation => current.Values.Any(r => r.Cancelled && r.Accepted && r.Voice != null);
        internal Request Begin(object manager, string logical, Action<SoundEndReceipt> complete, Action? releaseCancelled = null)
        {
            Cancel(manager);
            var request = new Request(manager, logical, ++sequence, session, complete, releaseCancelled);
            current[manager] = request;
            log("MusicRequest request=" + request.Id + "; session=" + session + "; logical=" + logical);
            return request;
        }
        internal bool Eligible(Request request) => !request.Cancelled && !request.Delivered && request.Session == session &&
            current.TryGetValue(request.Manager, out var active) && ReferenceEquals(active, request);
        internal bool Post(object manager, string logical, Action<SoundEndReceipt> complete, Func<Request, uint> post, Action? releaseCancelled = null)
        {
            var request = Begin(manager, logical, complete, releaseCancelled);
            bool accepted = false;
            try { accepted = Commit(request, post(request), request.Voice); return accepted; }
            finally
            {
                if (!accepted) { Cancel(request); request.Voice?.Stop(); }
            }
        }
        internal bool Commit(Request request, uint playingId, OwnedSoundBank.Voice? voice = null)
        {
            if (!Eligible(request) || playingId == 0) { Cancel(request); return false; }
            request.PlayingId = playingId; request.Voice = voice; request.Accepted = true;
            log("MusicAccepted request=" + request.Id + "; playing=" + playingId + "; owned=" + (voice != null));
            return true;
        }
        internal void Observe(Request request, SoundEndReceipt receipt) => receipts.Enqueue(Tuple.Create(request, receipt));
        internal void Cancel(Request request)
        {
            request.Cancelled = true;
            if (current.TryGetValue(request.Manager, out var active) && ReferenceEquals(active, request)) current.Remove(request.Manager);
        }
        internal void Withdraw(Request request)
        {
            if (!Eligible(request) || !request.Accepted || request.Voice == null) return;
            // Keep the current identity while native termination is pending.
            // No End may complete it, and a newer request/native stop/session
            // retirement can still revoke this cancellation handoff.
            request.Cancelled = true;
        }
        internal void Cancel(object manager) { if (current.TryGetValue(manager, out var request)) Cancel(request); }
        internal void RetireSession()
        {
            foreach (var request in current.Values) request.Cancelled = true;
            current.Clear(); session++;
        }
        internal void Stop(object manager, bool fade)
        {
            // P's native MUSIC parent owns the real fade/stop on every emitter.
            // This observation only invalidates request eligibility; it sends
            // no second native stop and never substitutes an envelope/clock.
            Cancel(manager);
            log("MusicStop fade=" + fade + "; route=P; forwarded=0");
        }
        internal void Pump()
        {
            while (receipts.TryDequeue(out var entry))
            {
                var request = entry.Item1; var receipt = entry.Item2;
                bool accepted = request.Accepted && Eligible(request) && receipt.Type == SoundEndReceipt.EndOfEvent &&
                    receipt.PlayingId != 0 && receipt.PlayingId == request.PlayingId;
                log("MusicEnd request=" + request.Id + "; logical=" + request.Logical + "; playing=" + receipt.PlayingId +
                    "; expected=" + request.PlayingId + "; delivered=" + accepted);
                if (!accepted) continue;
                request.Delivered = true; current.Remove(request.Manager);
                request.Complete(receipt);
            }
            foreach (var request in current.Values.ToArray())
            {
                if (!request.Cancelled || request.Voice?.Ended != true) continue;
                // A real End or successful owned Bank unload is required.
                // Failure retains this request for the next pump/retry.
                request.ReleaseCancelled?.Invoke();
                if (current.TryGetValue(request.Manager, out var active) && ReferenceEquals(active, request)) current.Remove(request.Manager);
                log("MusicCancelled request=" + request.Id + "; logical=" + request.Logical + "; playing=" + request.PlayingId);
            }
        }
    }
}
