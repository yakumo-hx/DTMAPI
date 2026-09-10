using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.Core.Services
{
    // One Runtime-owned executor. Facades retain their activation, never just an owner name.
    internal sealed partial class PlatformRuntimeServices : IModOwnerCleanupParticipant
    {
        private readonly object gate = new object();
        private readonly Func<bool> isMainThread;
        private readonly Func<double> clock;
        private readonly Action<string, string, Exception> logError;
        internal Action<DtmRuntimeContextSnapshot>? ContextPublished { get; set; }
        private readonly Dictionary<string, Owner> owners = new Dictionary<string, Owner>(StringComparer.OrdinalIgnoreCase);
        private DtmRuntimeContextSnapshot snapshot = new DtmRuntimeContextSnapshot(Guid.NewGuid(), 0, DtmRuntimePhase.Starting, null, null);
        private long saveSequence, worldSequence, resourceSequence;
        private bool nativeSaveSucceeded;
        private bool afterLoadObserved;
        private bool draining;
        internal int GlobalCapacity { get; set; } = 1024;
        internal int OwnerCapacity { get; set; } = 128;
        internal int FrameStartLimit { get; set; } = 64;
        internal double FrameBudgetMilliseconds { get; set; } = 2;
        public string ParticipantId => "Core.PlatformRuntimeServices";

        internal PlatformRuntimeServices(Func<bool> isMainThread, Action<string, string, Exception> logError, Func<double>? clock = null)
        {
            this.isMainThread = isMainThread;
            this.logError = logError;
            this.clock = clock ?? (() => Stopwatch.GetTimestamp() * 1000d / Stopwatch.Frequency);
        }

        private void RequireMainThread()
        { if (!isMainThread()) throw new InvalidOperationException("This operation requires the Runtime thread."); }

        internal object? Resolve(string ownerId, Type contract)
        {
            RequireMainThread();
            if (contract != typeof(IDtmRuntimeContext) && contract != typeof(IDtmScheduler) &&
                contract != typeof(IDtmOwnedResources) && contract != typeof(IDtmCommands)) return null;
            lock (gate)
            {
                if (snapshot.Phase == DtmRuntimePhase.ShuttingDown) throw new ObjectDisposedException(nameof(PlatformRuntimeServices));
                if (!owners.TryGetValue(ownerId, out Owner owner)) owners.Add(ownerId, owner = new Owner(this, ownerId));
                owner.RequireActive();
                return owner;
            }
        }

        internal DtmRuntimeContextSnapshot Snapshot { get { lock (gate) return snapshot; } }
        internal long BeginSaveAttempt()
        {
            RequireMainThread();
            long epoch;
            lock (gate) { epoch = ++saveSequence; nativeSaveSucceeded = afterLoadObserved = false; }
            Publish(DtmRuntimePhase.LoadingSave, epoch, null);
            return epoch;
        }

        internal void ObserveAfterLoad()
        {
            RequireMainThread();
            lock (gate)
            {
                if (!snapshot.SaveSessionEpoch.HasValue || snapshot.Phase == DtmRuntimePhase.ShuttingDown) return;
                afterLoadObserved = true;
            }
            Publish(DtmRuntimePhase.LoadingWorld, Snapshot.SaveSessionEpoch, null);
        }

        internal void CompleteSaveAttempt(bool succeeded)
        {
            RequireMainThread();
            lock (gate) nativeSaveSucceeded = succeeded && snapshot.SaveSessionEpoch.HasValue;
            if (!succeeded)
            {
                lock (gate) afterLoadObserved = false;
                // A native false can leave partial loaded data. Do not publish Title or keep a save scope.
                Publish(DtmRuntimePhase.LoadingSave, null, null);
            }
        }

        internal void BeginWorldTransition()
        {
            RequireMainThread();
            if (Snapshot.SaveSessionEpoch.HasValue) Publish(DtmRuntimePhase.LoadingWorld, Snapshot.SaveSessionEpoch, null);
        }

        internal void ObserveWorldReady()
        {
            RequireMainThread();
            long? save;
            lock (gate)
            {
                if (!nativeSaveSucceeded || !afterLoadObserved || snapshot.Phase != DtmRuntimePhase.LoadingWorld) return;
                save = snapshot.SaveSessionEpoch;
            }
            if (save.HasValue) Publish(DtmRuntimePhase.WorldReady, save, ++worldSequence);
        }

        internal void ReturnToTitle(bool completed)
        {
            RequireMainThread();
            lock (gate) nativeSaveSucceeded = afterLoadObserved = false;
            Publish(completed ? DtmRuntimePhase.Title : DtmRuntimePhase.ReturningToTitle, null, null);
        }

        internal void Shutdown()
        {
            RequireMainThread();
            Publish(DtmRuntimePhase.ShuttingDown, null, null);
        }

        private void Publish(DtmRuntimePhase phase, long? save, long? world)
        {
            RequireMainThread();
            DtmRuntimeContextSnapshot next;
            Resource[] cleanup;
            Subscription[] listeners;
            lock (gate)
            {
                if (snapshot.Phase == DtmRuntimePhase.ShuttingDown ||
                    (snapshot.Phase == phase && snapshot.SaveSessionEpoch == save && snapshot.WorldEpoch == world)) return;
                snapshot = next = new DtmRuntimeContextSnapshot(snapshot.RuntimeInstanceId, snapshot.Revision + 1, phase, save, world);
                foreach (Owner owner in owners.Values)
                    foreach (Work work in owner.Works.ToArray())
                        if (!IsScopeValid(work.Scope, work.Save, work.World)) Finish(work, DtmWorkStatus.Cancelled, "scope-expired", "Execution scope ended before work started.");
                cleanup = owners.Values.SelectMany(o => o.Resources)
                    .Where(r => r.Scope != DtmWorkScope.ModOwner && !IsScopeValid(r.Scope, r.Save, r.World))
                    .OrderByDescending(r => r.Sequence).ToArray();
                foreach (Resource resource in cleanup) resource.RequestDisposeLocked();
                listeners = owners.Values.SelectMany(o => o.Subscriptions).ToArray();
            }
            FlushDetachedRegistrations();
            try { ContextPublished?.Invoke(next); } catch { }
            foreach (Resource resource in cleanup) DisposeResource(resource);
            foreach (Subscription listener in listeners)
            {
                Action<DtmRuntimeContextSnapshot>? callback;
                lock (gate)
                {
                    if (snapshot.Revision != next.Revision) break; // A handler may cause a newer native transition.
                    callback = listener.Owner.Active ? listener.Callback : null;
                }
                if (callback == null) continue;
                try { callback(next); } catch (Exception ex) { Report(listener.Owner.Id, "Context callback failed.", ex); }
            }
        }

        private bool IsScopeValid(DtmWorkScope scope, long? save, long? world)
        {
            if (snapshot.Phase == DtmRuntimePhase.ShuttingDown) return false;
            switch (scope)
            {
                case DtmWorkScope.ModOwner: return true;
                case DtmWorkScope.CurrentSaveSession: return save.HasValue && save == snapshot.SaveSessionEpoch;
                case DtmWorkScope.CurrentWorld: return save.HasValue && save == snapshot.SaveSessionEpoch && world.HasValue && world == snapshot.WorldEpoch && snapshot.IsWorldReady;
                default: return false;
            }
        }

        private static void ValidateScope(DtmWorkScope scope)
        { if (scope < DtmWorkScope.ModOwner || scope > DtmWorkScope.CurrentWorld) throw new ArgumentOutOfRangeException(nameof(scope)); }

        private void Report(string owner, string message, Exception error)
        { try { logError(owner, message, error); } catch { /* Diagnostics must not strand work or resource cleanup. */ } }

        public ModOwnerCleanupParticipantResult RemoveOwner(string ownerId, ModOwnerCleanupReason reason)
        {
            RequireMainThread();
            Owner owner;
            Resource[] resources;
            int removed;
            lock (gate)
            {
                if (!owners.TryGetValue(ownerId, out owner)) return new ModOwnerCleanupParticipantResult(0, "No platform services for owner.");
                owner.Active = false;
                removed = owner.Works.Count + owner.Subscriptions.Count + owner.Commands.Count;
                foreach (Work work in owner.Works.ToArray()) Finish(work, DtmWorkStatus.Cancelled, "owner-closed", "Mod owner closed before work started.");
                foreach (Subscription subscription in owner.Subscriptions) subscription.Callback = null;
                owner.Subscriptions.Clear();
                foreach (Command command in owner.Commands.ToArray()) RemoveCommandLocked(command);
                resources = owner.Resources.OrderByDescending(r => r.Sequence).ToArray();
                foreach (Resource resource in resources) resource.RequestDisposeLocked();
            }
            FlushDetachedRegistrations();
            foreach (Resource resource in resources) DisposeResource(resource);
            lock (gate)
            {
                removed += resources.Length - owner.Resources.Count;
                int remaining = owner.Resources.Count;
                // An in-flight callback owns its stack until return; no pending platform root is retained for it.
                if (remaining == 0) owners.Remove(ownerId);
                return new ModOwnerCleanupParticipantResult(removed, remaining,
                    owner.Resources.Count(r => r.State == DtmResourceStatus.Failed), "Platform owner closed; failed resource disposals remain retryable.");
            }
        }

        private sealed partial class Owner : IDtmRuntimeContext, IDtmScheduler, IDtmOwnedResources, IDtmCommands
        {
            internal readonly PlatformRuntimeServices Runtime;
            internal readonly string Id;
            internal bool Active = true;
            internal readonly List<Work> Works = new List<Work>();
            internal readonly List<Resource> Resources = new List<Resource>();
            internal readonly List<Subscription> Subscriptions = new List<Subscription>();
            internal readonly List<Command> Commands = new List<Command>();
            internal Owner(PlatformRuntimeServices runtime, string id) { Runtime = runtime; Id = id; }
            internal void RequireActive() { if (!Active) throw new ObjectDisposedException("Mod owner " + Id); }
            public DtmRuntimeContextSnapshot Snapshot { get { lock (Runtime.gate) { RequireActive(); return Runtime.snapshot; } } }
            public bool IsMainThread => Runtime.isMainThread();
            public IDisposable Subscribe(Action<DtmRuntimeContextSnapshot> changed)
            {
                if (changed == null) throw new ArgumentNullException(nameof(changed));
                Runtime.RequireMainThread();
                lock (Runtime.gate)
                {
                    RequireActive();
                    if (Runtime.snapshot.Phase == DtmRuntimePhase.ShuttingDown) throw new ObjectDisposedException(nameof(PlatformRuntimeServices));
                    if (Subscriptions.Count >= 128) throw new InvalidOperationException("Context subscription capacity exceeded.");
                    var result = new Subscription(this, changed);
                    Subscriptions.Add(result);
                    return result;
                }
            }
            public IDtmOwnedResource Register(IDisposable resource, DtmWorkScope scope = DtmWorkScope.ModOwner)
            {
                if (resource == null) throw new ArgumentNullException(nameof(resource));
                ValidateScope(scope);
                Runtime.RequireMainThread();
                lock (Runtime.gate)
                {
                    RequireActive();
                    if (!Runtime.IsScopeValid(scope, Runtime.snapshot.SaveSessionEpoch, Runtime.snapshot.WorldEpoch)) throw new InvalidOperationException("Resource scope is unavailable.");
                    if (Resources.Count >= 128) throw new InvalidOperationException("Owned resource capacity exceeded.");
                    var result = new Resource(this, resource, scope, ++Runtime.resourceSequence);
                    Resources.Add(result);
                    return result;
                }
            }
        }

        private sealed class Subscription : IDisposable
        {
            internal readonly Owner Owner;
            internal Action<DtmRuntimeContextSnapshot>? Callback;
            internal Subscription(Owner owner, Action<DtmRuntimeContextSnapshot> callback) { Owner = owner; Callback = callback; }
            public void Dispose() { lock (Owner.Runtime.gate) { Callback = null; Owner.Subscriptions.Remove(this); } }
        }

        private sealed class Resource : IDtmOwnedResource
        {
            internal readonly Owner Owner;
            internal readonly long Sequence;
            internal readonly DtmWorkScope Scope;
            internal readonly long? Save, World;
            internal IDisposable? Value;
            internal DtmResourceStatus State;
            internal Resource(Owner owner, IDisposable value, DtmWorkScope scope, long sequence)
            {
                Owner = owner; Value = value; Scope = scope; Sequence = sequence;
                Save = owner.Runtime.snapshot.SaveSessionEpoch; World = owner.Runtime.snapshot.WorldEpoch;
            }
            public DtmResourceStatus Status { get { lock (Owner.Runtime.gate) return State; } }
            internal void RequestDisposeLocked()
            {
                if (State == DtmResourceStatus.Registered || State == DtmResourceStatus.Failed) State = DtmResourceStatus.DisposeQueued;
            }
            public bool Unregister()
            {
                lock (Owner.Runtime.gate)
                {
                    if (State != DtmResourceStatus.Registered) return false;
                    State = DtmResourceStatus.Unregistered; Value = null; Owner.Resources.Remove(this); return true;
                }
            }
            public void Dispose()
            {
                lock (Owner.Runtime.gate) RequestDisposeLocked();
                if (Owner.Runtime.isMainThread()) Owner.Runtime.DisposeResource(this);
            }
        }

        private void DisposeResource(Resource resource)
        {
            IDisposable? value;
            lock (gate)
            {
                if (resource.State != DtmResourceStatus.DisposeQueued) return;
                resource.State = DtmResourceStatus.Disposing;
                value = resource.Value;
            }
            try
            {
                value?.Dispose();
                lock (gate) { resource.State = DtmResourceStatus.Disposed; resource.Value = null; resource.Owner.Resources.Remove(resource); }
            }
            catch (Exception ex)
            {
                lock (gate) resource.State = DtmResourceStatus.Failed;
                Report(resource.Owner.Id, "Owned resource disposal failed; retained for explicit/owner cleanup retry.", ex);
            }
        }
    }
}
