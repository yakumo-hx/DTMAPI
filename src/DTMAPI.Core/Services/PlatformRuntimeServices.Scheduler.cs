using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DTMAPI.Abstractions;

namespace DTMAPI.Core.Services
{
    internal sealed partial class PlatformRuntimeServices
    {
        // Submission and cancellation may come from any thread; execution stays on the runtime thread.
        private readonly object queueGate = new object();
        private readonly List<CancellationTokenRegistration> detachedRegistrations = new List<CancellationTokenRegistration>();
        private readonly SortedSet<Work> timers = new SortedSet<Work>(Comparer<Work>.Create((a, b) =>
        { int due = a.Due.CompareTo(b.Due); return due != 0 ? due : a.Sequence.CompareTo(b.Sequence); }));
        private long workSequence;
        private ulong tick;
        private int queuedCount;
        private string lastStartedOwner = string.Empty;
        private ulong lastDrainedTick;
        private bool hasDrained;
        private bool draining;
        internal int PendingWorkCount { get { lock (queueGate) return queuedCount; } }

        private void UpdateSchedulerContext(DtmRuntimeContextSnapshot next)
        {
            lock (queueGate)
            {
                Volatile.Write(ref snapshot, next);
                foreach (Owner owner in owners.Values)
                    foreach (Work work in owner.Works.ToArray())
                        if (!IsScopeValid(work.Scope, work.Save, work.World))
                            Finish(work, DtmWorkStatus.Cancelled, "scope-expired", "Execution scope ended before work started.");
            }
            FlushDetachedRegistrations();
        }

        private int CloseSchedulerOwner(Owner owner)
        {
            int removed;
            lock (queueGate)
            {
                owner.Active = false;
                removed = owner.Works.Count;
                foreach (Work work in owner.Works.ToArray())
                    Finish(work, DtmWorkStatus.Cancelled, "owner-closed", "Mod owner closed before work started.");
            }
            FlushDetachedRegistrations();
            return removed;
        }

        // Called at the start of Core Update, before request/queue callbacks can submit NextTick.
        internal void BeginTick(ulong currentTick)
        {
            lock (queueGate)
            {
                if (currentTick < tick) throw new InvalidOperationException("Runtime scheduler ticks cannot move backward.");
                tick = currentTick;
            }
        }

        internal void Drain()
        {
            if (draining || (hasDrained && lastDrainedTick == tick)) return;
            hasDrained = true; lastDrainedTick = tick;
            long cutoff;
            double started = clock();
            lock (queueGate)
            {
                // Keep the frame cutoff while idle without allocating a queue snapshot.
                if (queuedCount == 0 && detachedRegistrations.Count == 0) return;
                cutoff = workSequence;
            }
            draining = true;
            try
            {
                int starts = 0;
                while (starts < FrameStartLimit && clock() - started < FrameBudgetMilliseconds)
                {
                    Work? next = null;
                    lock (queueGate)
                    {
                        double now = clock();
                        while (timers.Count > 0 && timers.Min!.Due <= now)
                        { Work due = timers.Min!; timers.Remove(due); due.Ready = true; }
                        Owner[] active = owners.Values.Where(o => o.Active && o.Works.Count > 0).ToArray();
                        int previous = Array.FindIndex(active, o => string.Equals(o.Id, lastStartedOwner, StringComparison.OrdinalIgnoreCase));
                        for (int offset = 1; offset <= active.Length; offset++)
                        {
                            Owner owner = active[(previous + offset) % active.Length];
                            foreach (Work candidate in owner.Works.ToArray())
                            {
                                if (candidate.Token.IsCancellationRequested)
                                { Finish(candidate, DtmWorkStatus.Cancelled, "cancelled", "Cancellation requested before execution began."); continue; }
                                if (candidate.Deadline.HasValue && now >= candidate.Deadline.Value)
                                { Finish(candidate, DtmWorkStatus.Expired, "queue-expired", "Queue deadline elapsed before work started."); continue; }
                                if (candidate.Sequence > cutoff || !candidate.Ready || candidate.EarliestTick > tick) continue;
                                next = candidate;
                                break;
                            }
                            if (next != null) break;
                        }
                        if (next != null)
                        {
                            Owner owner = next.Owner!;
                            if (!owner.Active || !IsScopeValid(next.Scope, next.Save, next.World))
                            { Finish(next, DtmWorkStatus.Cancelled, "scope-expired", "Scope ended before work started."); next = null; }
                            else
                            {
                                next.State = DtmWorkStatus.Running;
                                owner.Works.Remove(next); queuedCount--;
                                lastStartedOwner = owner.Id;
                                DetachRegistration(next);
                            }
                        }
                    }
                    FlushDetachedRegistrations();
                    if (next == null) break;
                    starts++;
                    Exception? failure = null;
                    try { next.Callback!(); }
                    catch (Exception ex) { failure = ex; Report(next.Owner!.Id, "Scheduled callback failed.", ex); }
                    lock (queueGate)
                        Finish(next, failure == null ? DtmWorkStatus.Succeeded : DtmWorkStatus.Failed,
                            failure == null ? "" : "callback-failed", failure == null ? "" : Bounded(failure.GetType().Name + ": " + failure.Message, 512));
                }
            }
            finally
            {
                draining = false;
                FlushDetachedRegistrations();
            }
        }

        private IDtmScheduledWork Enqueue(Owner owner, Action action, DtmWorkScope scope, CancellationToken token,
            TimeSpan delay, bool nextTick, TimeSpan? queueTimeout)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            ValidateScope(scope);
            if (delay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay));
            if (queueTimeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(queueTimeout));
            Work work;
            lock (queueGate)
            {
                string rejection = !owner.Active || snapshot.Phase == DtmRuntimePhase.ShuttingDown ? "owner-closed" :
                    !IsScopeValid(scope, snapshot.SaveSessionEpoch, snapshot.WorldEpoch) ? "scope-unavailable" :
                    queuedCount >= GlobalCapacity || owner.Works.Count >= OwnerCapacity ? "queue-full" : "";
                if (rejection.Length != 0) return Work.Terminal(DtmWorkStatus.Rejected, rejection, "Work was not queued: " + rejection + ".");
                if (token.IsCancellationRequested) return Work.Terminal(DtmWorkStatus.Cancelled, "cancelled", "Cancellation requested before submission.");
                double now = clock();
                work = new Work(this, owner, action, scope, snapshot.SaveSessionEpoch, snapshot.WorldEpoch,
                    ++workSequence, now + delay.TotalMilliseconds, nextTick ? checked(tick + 1) : tick,
                    queueTimeout.HasValue ? now + queueTimeout.Value.TotalMilliseconds : (double?)null);
                work.Token = token;
                work.Ready = delay == TimeSpan.Zero;
                if (!work.Ready) timers.Add(work);
                owner.Works.Add(work); queuedCount++;
            }
            if (token.CanBeCanceled)
            {
                // Register outside the gate: an already-cancelled token may invoke synchronously.
                CancellationTokenRegistration registration = token.Register(state => ((Work)state!).TryCancel(), work);
                bool dispose;
                lock (queueGate)
                {
                    dispose = work.State != DtmWorkStatus.Queued;
                    if (!dispose) work.Registration = registration;
                }
                if (dispose) registration.Dispose();
            }
            return work;
        }

        private bool Cancel(Work work)
        {
            bool cancelled;
            lock (queueGate)
            {
                cancelled = work.State == DtmWorkStatus.Queued;
                if (cancelled) Finish(work, DtmWorkStatus.Cancelled, "cancelled", "Cancelled before execution began.");
            }
            FlushDetachedRegistrations();
            return cancelled;
        }

        private void Finish(Work work, DtmWorkStatus status, string code, string message)
        {
            if (work.State != DtmWorkStatus.Queued && work.State != DtmWorkStatus.Running) return;
            if (work.State == DtmWorkStatus.Queued) { work.Owner!.Works.Remove(work); queuedCount--; }
            DetachRegistration(work);
            timers.Remove(work);
            work.State = status;
            work.Token = default;
            work.Callback = null;
            work.Owner = null;
            work.Runtime = null;
            work.Source.TrySetResult(new DtmWorkResult(status, code, message));
        }

        private void DetachRegistration(Work work)
        {
            if (work.Registration.HasValue) { detachedRegistrations.Add(work.Registration.Value); work.Registration = null; }
        }

        private void FlushDetachedRegistrations()
        {
            CancellationTokenRegistration[] detached;
            lock (queueGate) { detached = detachedRegistrations.ToArray(); detachedRegistrations.Clear(); }
            // Dispose can wait for a cancellation callback; never do that while holding the gate.
            foreach (CancellationTokenRegistration registration in detached) registration.Dispose();
        }

        private static string Bounded(string text, int limit) => text.Length <= limit ? text : text.Substring(0, limit);

        private sealed class Work : IDtmScheduledWork
        {
            internal PlatformRuntimeServices? Runtime;
            internal Owner? Owner;
            internal Action? Callback;
            internal readonly DtmWorkScope Scope;
            internal readonly long? Save, World;
            internal readonly long Sequence;
            internal readonly double Due;
            internal readonly double? Deadline;
            internal readonly ulong EarliestTick;
            internal CancellationTokenRegistration? Registration;
            internal CancellationToken Token;
            internal bool Ready;
            internal readonly TaskCompletionSource<DtmWorkResult> Source = new TaskCompletionSource<DtmWorkResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            private int state;
            internal DtmWorkStatus State { get => (DtmWorkStatus)Volatile.Read(ref state); set => Volatile.Write(ref state, (int)value); }
            public DtmWorkStatus Status => State;
            public Task<DtmWorkResult> Completion => Source.Task;
            public bool TryCancel() => Volatile.Read(ref Runtime)?.Cancel(this) ?? false;
            private Work() { }
            internal Work(PlatformRuntimeServices runtime, Owner owner, Action callback, DtmWorkScope scope,
                long? save, long? world, long sequence, double due, ulong earliestTick, double? deadline)
            {
                Runtime = runtime; Owner = owner; Callback = callback; Scope = scope; Save = save; World = world;
                Sequence = sequence; Due = due; EarliestTick = earliestTick; Deadline = deadline;
            }
            internal static Work Terminal(DtmWorkStatus status, string code, string message)
            {
                var work = new Work { State = status };
                work.Source.SetResult(new DtmWorkResult(status, code, message));
                return work;
            }
        }

        private sealed partial class Owner
        {
            public IDtmScheduledWork Post(Action action, DtmWorkScope scope = DtmWorkScope.ModOwner, CancellationToken cancellationToken = default, TimeSpan? queueTimeout = null)
                => Runtime.Enqueue(this, action, scope, cancellationToken, TimeSpan.Zero, false, queueTimeout);
            public IDtmScheduledWork NextTick(Action action, DtmWorkScope scope = DtmWorkScope.ModOwner, CancellationToken cancellationToken = default, TimeSpan? queueTimeout = null)
                => Runtime.Enqueue(this, action, scope, cancellationToken, TimeSpan.Zero, true, queueTimeout);
            public IDtmScheduledWork Delay(Action action, TimeSpan delay, DtmWorkScope scope = DtmWorkScope.ModOwner, CancellationToken cancellationToken = default, TimeSpan? queueTimeout = null)
                => Runtime.Enqueue(this, action, scope, cancellationToken, delay, false, queueTimeout);
        }
    }
}
