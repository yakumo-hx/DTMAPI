using System;

namespace DTMAPI.Internal.HeartMap
{
    internal enum MapPhase { Preparing, Publishing, Entering, Exiting, Recovering, Releasing, Finished }
    internal enum MapResult { Entered, Exited, FailedBeforeTransition, FailedRecovered, Cancelled, CancelledAtTitle, FaultedRecovery, FaultedUnload }

    // Used by the runtime, with an externally supplied monotonic clock in tests.
    // No native references survive in a result or a retired callback ticket.
    internal sealed class MapRequest
    {
        internal readonly long Id;
        internal readonly int Generation;
        internal readonly string Target;
        internal readonly bool IsExit;
        internal MapPhase Phase { get; private set; } = MapPhase.Preparing;
        internal MapResult? Result { get; private set; }
        internal string Detail { get; private set; } = "";
        internal double Deadline { get; private set; }
        internal int CompletionCount { get; private set; }
        internal NativeTicket? Ticket { get; private set; }
        internal bool RecoveryAttempted { get; private set; }
        internal bool IsDone => Result.HasValue;
        internal MapRequest(long id, int generation, string target, bool isExit, double now)
        { Id = id; Generation = generation; Target = target; IsExit = isExit; Deadline = now + 20; }
        internal bool IsCurrent(int generation) => !IsDone && generation == Generation;
        internal bool Expired(double now) => !IsDone && now >= Deadline;
        internal NativeTicket BeginNative(double now)
        {
            if (IsDone || Phase != MapPhase.Preparing) throw new InvalidOperationException("Request is not prepared.");
            Phase = MapPhase.Publishing; Deadline = now + 8;
            return Ticket = new NativeTicket(this);
        }
        internal void Published()
        {
            if (Phase == MapPhase.Publishing) Phase = IsExit ? MapPhase.Exiting : MapPhase.Entering;
        }
        internal NativeTicket? BeginRecovery(string detail, double now)
        {
            if (IsDone || RecoveryAttempted) return null;
            Ticket?.Cancel(); RecoveryAttempted = true; Detail = detail;
            Phase = MapPhase.Recovering; Deadline = now + 8;
            return Ticket = new NativeTicket(this);
        }
        internal void Releasing(double now)
        {
            if (IsDone) return;
            Ticket?.Cancel(); Phase = MapPhase.Releasing; Deadline = now + 8;
        }
        internal bool Finish(MapResult result, string detail = "")
        {
            if (IsDone) return false;
            Ticket?.Cancel(); Ticket = null;
            Result = result; if (detail.Length > 0) Detail = detail;
            Phase = MapPhase.Finished; CompletionCount++;
            return true;
        }
    }
    internal sealed class NativeTicket
    {
        private readonly MapRequest request;
        private bool cancelled, fade, load;
        internal bool CallbackReceived { get; private set; }
        internal NativeTicket(MapRequest request) { this.request = request; }
        internal bool Valid(int generation) => !cancelled && request.IsCurrent(generation) && ReferenceEquals(request.Ticket, this);
        internal bool TakeFade(int generation) { if (!Valid(generation) || fade) return false; fade = true; return true; }
        internal bool TakeLoad(int generation) { if (!Valid(generation) || load) return false; load = true; return true; }
        internal bool Complete(int generation) { if (!Valid(generation) || CallbackReceived) return false; CallbackReceived = true; return true; }
        internal void Cancel() { cancelled = true; }
    }
}
