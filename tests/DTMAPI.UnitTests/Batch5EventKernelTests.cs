using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.Testing;

namespace DTMAPI.UnitTests
{
    /// <summary>
    /// Focused Batch 5 event-kernel gates wired into the tracked unit-test entry point.
    /// </summary>
    public static class Batch5EventKernelTests
    {
        public static void RunAll()
        {
            ZeroListenerFastPathDoesNotAllocateEventArgsOrSnapshots();
            EmptyRuntimeQueuePathIsAllocationFreeAndChangeDriven();
            DiagnosticRevisionsTrackQueueStateButNotDirectDispatches();
            CopyOnWriteSnapshotsChangeOnlyWithSubscriptions();
            CurrentDispatchIsFrozenAndNestedDispatchReadsLatestSnapshot();
            PublicationMembershipFreezesAtPreparation();
            DuplicateRemovalUsesStandardLastRegistrationSemantics();
            OwnerBoundUnsubscribeUsesRegistrationGuard();
            HandlerFailuresAreIsolatedAndQuarantineAllowsExplicitResubscription();
            PerOwnerCountersAreAttributedAndDiagnosticProjectionIsBounded();
            HandlerTimingIsExplicitDiagnosticModeOnly();
            ListenerTransitionsReportFirstLastOwnerAndQuarantineChanges();
            ConcurrentMembershipTransitionsDrainInMutationOrder();
            TransitionCallbackReentryIsRejectedWithoutStrandingLaterTickets();
            QueuePoliciesAreBoundedCoalescedAndFailureIsolated();
            QueuedRecipientsExcludeRemovedOwnersAndLateSubscribers();
        }

        private static void ZeroListenerFastPathDoesNotAllocateEventArgsOrSnapshots()
        {
            EventManager manager = CreateManager(mainThreadBoundaryEnabled: false);
            for (int index = 0; index < 1024; index++)
                manager.DispatchUpdateTicked((ulong)index);

            EventSlotSnapshot before = FindSlot(manager, "GameLoop.UpdateTicked");
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 10_000; index++)
                manager.DispatchUpdateTicked((ulong)index);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
            EventSlotSnapshot after = FindSlot(manager, "GameLoop.UpdateTicked");

            Assert(allocated == 0, "Zero-listener UpdateTicked dispatch must allocate no managed objects after warmup; allocated=" + allocated + ".");
            Assert(after.PublicationCalls - before.PublicationCalls == 10_000, "The publication counter should include every zero-listener call.");
            Assert(after.ZeroListenerBypasses - before.ZeroListenerBypasses == 10_000, "Every zero-listener call should use the fast-path bypass.");
            Assert(after.EventArgsCreated == 0, "Zero listeners must prevent EventArgs construction.");
            Assert(after.SnapshotRebuilds == 0, "Dispatch must not rebuild immutable snapshots.");
            Assert(manager.GetBoundarySnapshot().Queued == 0, "Zero listeners must prevent queue entries.");
        }

        private static void EmptyRuntimeQueuePathIsAllocationFreeAndChangeDriven()
        {
            string root = Path.Combine(
                DtmApiTestSession.Current.RootPath,
                "batch5-empty-runtime-queues",
                Guid.NewGuid().ToString("N"));
            var runtime = new DtmApiRuntime(new TestRuntimeHost(root));
            runtime.FlushRuntimeQueues("Batch5.EmptyRuntime.Warm");
            for (int index = 0; index < 1024; index++)
                runtime.Update();

            IDtmFeatureStatusInfo hookStatusBefore = runtime.Diagnostics.GetFeatureStatuses()
                .Single(status => status.FeatureId == "Refactor.HookStatusQueue");
            IDtmFeatureStatusInfo boundaryStatusBefore = runtime.Diagnostics.GetFeatureStatuses()
                .Single(status => status.FeatureId == "Refactor.EventMainThreadBoundary");
            long hookRevisionBefore = runtime.HookStatusQueueSnapshot.Revision;
            long boundaryRevisionBefore = runtime.EventDispatchBoundarySnapshot.Revision;

            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 10_000; index++)
                runtime.Update();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            IDtmFeatureStatusInfo hookStatusAfter = runtime.Diagnostics.GetFeatureStatuses()
                .Single(status => status.FeatureId == "Refactor.HookStatusQueue");
            IDtmFeatureStatusInfo boundaryStatusAfter = runtime.Diagnostics.GetFeatureStatuses()
                .Single(status => status.FeatureId == "Refactor.EventMainThreadBoundary");
            Assert(allocated == 0, "A warmed empty Core Update path must allocate zero managed bytes across 10k frames; allocated=" + allocated + ".");
            Assert(ReferenceEquals(hookStatusBefore, hookStatusAfter), "An unchanged Hook queue must not replace its detailed feature-status object during empty frames.");
            Assert(ReferenceEquals(boundaryStatusBefore, boundaryStatusAfter), "An unchanged event boundary must not replace its detailed feature-status object during empty frames.");
            Assert(runtime.HookStatusQueueSnapshot.Revision == hookRevisionBefore, "Empty frames must not advance the Hook queue diagnostic revision.");
            Assert(runtime.EventDispatchBoundarySnapshot.Revision == boundaryRevisionBefore, "Zero-listener direct publications must not advance the event-boundary diagnostic revision.");
        }

        private static void DiagnosticRevisionsTrackQueueStateButNotDirectDispatches()
        {
            var hookQueue = new HookStatusPublicationQueue(maxPending: 8);
            long hookInitial = hookQueue.DiagnosticRevision;
            Assert(!hookQueue.HasPending, "A new Hook queue should expose a false scalar pending hint.");
            hookQueue.Enqueue("Batch5.Revision", "ready", "DTMAPI.UnitTests", "revision probe", "UnitTest", 2, 1);
            long hookEnqueued = hookQueue.DiagnosticRevision;
            Assert(hookQueue.HasPending && hookEnqueued > hookInitial, "Hook enqueue should advance the scalar revision and pending hint.");
            hookQueue.Drain();
            long hookDrained = hookQueue.DiagnosticRevision;
            Assert(!hookQueue.HasPending && hookDrained > hookEnqueued, "Non-empty Hook drain should advance the revision and clear the pending hint.");
            hookQueue.Drain();
            Assert(hookQueue.DiagnosticRevision == hookDrained, "An empty Hook drain must not dirty detailed diagnostics.");

            EventManager direct = CreateManager(mainThreadBoundaryEnabled: false);
            long beforeSubscription = direct.BoundaryDiagnosticRevision;
            direct.CreateProxy("DTMAPI.Tests.Batch5.DirectRevision").GameLoop.UpdateTicked += (_, _) => { };
            Assert(direct.BoundaryDiagnosticRevision > beforeSubscription, "A listener membership change must dirty one detailed event snapshot.");
            EventDispatchBoundarySnapshot before = direct.GetBoundarySnapshot();
            for (int index = 0; index < 1000; index++)
                direct.DispatchUpdateTicked((ulong)index);
            EventDispatchBoundarySnapshot after = direct.GetBoundarySnapshot();
            Assert(after.TotalDispatched - before.TotalDispatched == 1000, "Direct dispatch counters must remain live for explicit report snapshots.");
            Assert(after.Revision == before.Revision, "Direct dispatch count/phase changes must not dirty the per-frame detailed boundary diagnostic revision.");

            direct.EnqueueTestDispatch("Batch5.Revision.Queue", EventDispatchPolicy.BoundedFifo, null, () => { });
            long queuedRevision = direct.BoundaryDiagnosticRevision;
            Assert(direct.HasQueuedDispatches && queuedRevision > after.Revision, "A queued observation must advance the event-boundary revision and pending hint.");
            direct.FlushQueuedDispatches();
            Assert(!direct.HasQueuedDispatches && direct.BoundaryDiagnosticRevision > queuedRevision, "Committing a queued observation must advance the boundary revision and clear the pending hint.");
        }

        private static void CopyOnWriteSnapshotsChangeOnlyWithSubscriptions()
        {
            EventManager manager = CreateManager(mainThreadBoundaryEnabled: false);
            IEventsHelper events = manager.CreateProxy("DTMAPI.Tests.Batch5.Cow");
            int calls = 0;
            EventHandler<UpdateTickedEventArgs> first = (_, _) => calls++;
            EventHandler<UpdateTickedEventArgs> second = (_, _) => calls++;

            events.GameLoop.UpdateTicked += first;
            events.GameLoop.UpdateTicked += second;
            EventSlotSnapshot afterAdds = FindSlot(manager, "GameLoop.UpdateTicked");
            Assert(afterAdds.SnapshotRebuilds == 2 && afterAdds.ActiveHandlers == 2, "Each actual subscription should publish exactly one COW snapshot.");

            for (int index = 0; index < 1000; index++)
                manager.DispatchUpdateTicked((ulong)index);
            EventSlotSnapshot afterDispatch = FindSlot(manager, "GameLoop.UpdateTicked");
            Assert(afterDispatch.SnapshotRebuilds == afterAdds.SnapshotRebuilds, "Dispatch must reuse the immutable snapshot.");
            Assert(afterDispatch.HandlerCalls == 2000 && calls == 2000, "Both COW registrations should run for every publication.");

            events.GameLoop.UpdateTicked -= first;
            EventSlotSnapshot afterRemove = FindSlot(manager, "GameLoop.UpdateTicked");
            Assert(afterRemove.SnapshotRebuilds == afterAdds.SnapshotRebuilds + 1 && afterRemove.ActiveHandlers == 1, "A successful removal should publish one replacement snapshot.");
        }

        private static void CurrentDispatchIsFrozenAndNestedDispatchReadsLatestSnapshot()
        {
            EventManager manager = CreateManager(mainThreadBoundaryEnabled: false);
            IEventsHelper events = manager.CreateProxy("DTMAPI.Tests.Batch5.Mutation");
            var trace = new List<string>();
            int depth = 0;
            bool mutate = true;
            EventHandler<UpdateTickedEventArgs> second = (_, _) => trace.Add("B" + depth);
            EventHandler<UpdateTickedEventArgs> added = (_, _) => trace.Add("C" + depth);
            EventHandler<UpdateTickedEventArgs> first = (_, _) =>
            {
                trace.Add("A" + depth);
                if (depth != 0 || !mutate)
                    return;

                events.GameLoop.UpdateTicked -= second;
                events.GameLoop.UpdateTicked += added;
                mutate = false;
                depth = 1;
                manager.DispatchUpdateTicked(2);
                depth = 0;
            };

            events.GameLoop.UpdateTicked += first;
            events.GameLoop.UpdateTicked += second;
            manager.DispatchUpdateTicked(1);
            Assert(string.Join(",", trace) == "A0,A1,C1,B0", "The outer dispatch must retain A/B while nested dispatch reads the new A/C snapshot; actual=" + string.Join(",", trace) + ".");

            trace.Clear();
            manager.DispatchUpdateTicked(3);
            Assert(string.Join(",", trace) == "A0,C0", "The next top-level dispatch should observe the subscription mutations.");
        }

        private static void PublicationMembershipFreezesAtPreparation()
        {
            EventManager manager = CreateManager(mainThreadBoundaryEnabled: false);
            IEventsHelper events = manager.CreateProxy("DTMAPI.Tests.Batch5.PreparedMembership");
            int originalCalls = 0;
            int lateCalls = 0;
            EventHandler<UpdateTickedEventArgs> original = (_, _) => originalCalls++;
            EventHandler<UpdateTickedEventArgs> late = (_, _) => lateCalls++;
            events.GameLoop.UpdateTicked += original;

            using var prepared = new ManualResetEvent(false);
            using var release = new ManualResetEvent(false);
            manager.ConfigurePublicationPreparedCheckpointForTests(eventName =>
            {
                if (!eventName.Equals("GameLoop.UpdateTicked", StringComparison.Ordinal))
                    return;
                prepared.Set();
                if (!release.WaitOne(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("Prepared-membership test did not release the publication barrier.");
            });

            Exception? publicationFailure = null;
            var publisher = new Thread(() =>
            {
                try
                {
                    manager.DispatchUpdateTicked(1);
                }
                catch (Exception ex)
                {
                    publicationFailure = ex;
                }
            });
            publisher.Start();
            Assert(prepared.WaitOne(TimeSpan.FromSeconds(5)), "The dispatch should expose its test barrier after freezing membership.");
            events.GameLoop.UpdateTicked -= original;
            events.GameLoop.UpdateTicked += late;
            release.Set();
            publisher.Join();
            manager.ConfigurePublicationPreparedCheckpointForTests(null);
            if (publicationFailure != null)
                throw new InvalidOperationException("Prepared-membership publication failed.", publicationFailure);

            Assert(originalCalls == 1 && lateCalls == 0, "A publication must retain the member removed after preparation and exclude a subscriber added after preparation.");
            manager.DispatchUpdateTicked(2);
            Assert(originalCalls == 1 && lateCalls == 1, "The next publication must observe the replacement membership snapshot.");
        }

        private static void DuplicateRemovalUsesStandardLastRegistrationSemantics()
        {
            EventManager manager = CreateManager(mainThreadBoundaryEnabled: false);
            IEventsHelper events = manager.CreateProxy("DTMAPI.Tests.Batch5.Duplicate");
            int calls = 0;
            EventHandler<UpdateTickedEventArgs> handler = (_, _) => calls++;
            events.GameLoop.UpdateTicked += handler;
            events.GameLoop.UpdateTicked += handler;
            events.GameLoop.UpdateTicked -= handler;

            manager.DispatchUpdateTicked(1);
            EventSlotSnapshot snapshot = FindSlot(manager, "GameLoop.UpdateTicked");
            Assert(calls == 1 && snapshot.ActiveHandlers == 1, "A single -= must remove only the last matching owner/delegate registration.");
        }

        private static void OwnerBoundUnsubscribeUsesRegistrationGuard()
        {
            int runtimeThreadId = Thread.CurrentThread.ManagedThreadId;
            bool ownerActive = true;
            int guardCalls = 0;
            EventManager manager = CreateManager(mainThreadBoundaryEnabled: false, runtimeThreadId: runtimeThreadId);
            IEventsHelper events = manager.CreateOwnerBoundProxy(
                "DTMAPI.Tests.Batch5.OwnerBoundRemove",
                () =>
                {
                    Interlocked.Increment(ref guardCalls);
                    if (Thread.CurrentThread.ManagedThreadId != runtimeThreadId)
                        throw new InvalidOperationException("owner-bound mutation must use the runtime thread");
                    if (!ownerActive)
                        throw new InvalidOperationException("owner-bound mutation requires an active owner");
                });
            int calls = 0;
            EventHandler<UpdateTickedEventArgs> handler = (_, _) => calls++;
            events.GameLoop.UpdateTicked += handler;

            Exception? offThreadFailure = null;
            var worker = new Thread(() =>
            {
                try
                {
                    events.GameLoop.UpdateTicked -= handler;
                }
                catch (Exception ex)
                {
                    offThreadFailure = ex;
                }
            });
            worker.Start();
            worker.Join();
            Assert(offThreadFailure is InvalidOperationException, "Owner-bound -= must reject the same off-runtime-thread mutation rejected by +=.");
            Assert(FindSlot(manager, "GameLoop.UpdateTicked").ActiveHandlers == 1, "A rejected off-thread unsubscribe must not mutate membership.");

            ownerActive = false;
            bool inactiveRejected = false;
            try
            {
                events.GameLoop.UpdateTicked -= handler;
            }
            catch (InvalidOperationException)
            {
                inactiveRejected = true;
            }
            Assert(inactiveRejected && FindSlot(manager, "GameLoop.UpdateTicked").ActiveHandlers == 1, "Owner-bound -= must also preserve the active-owner lifecycle guard.");

            ownerActive = true;
            manager.DispatchUpdateTicked(1);
            events.GameLoop.UpdateTicked -= handler;
            Assert(calls == 1 && FindSlot(manager, "GameLoop.UpdateTicked").ActiveHandlers == 0, "A valid runtime-thread unsubscribe should remove exactly one registration.");
            Assert(guardCalls == 4, "The shared owner mutation guard should run for add and every non-null remove attempt.");
        }

        private static void HandlerFailuresAreIsolatedAndQuarantineAllowsExplicitResubscription()
        {
            EventManager manager = CreateManager(mainThreadBoundaryEnabled: false);
            IEventsHelper events = manager.CreateProxy("DTMAPI.Tests.Batch5.Quarantine");
            int throwingCalls = 0;
            int stableCalls = 0;
            bool shouldThrow = true;
            EventHandler<UpdateTickedEventArgs> throwing = (_, _) =>
            {
                throwingCalls++;
                if (shouldThrow)
                    throw new InvalidOperationException("batch5-quarantine-probe");
            };
            events.GameLoop.UpdateTicked += throwing;
            events.GameLoop.UpdateTicked += (_, _) => stableCalls++;

            for (int index = 0; index < 5; index++)
                manager.DispatchUpdateTicked((ulong)index);

            EventSlotSnapshot quarantined = FindSlot(manager, "GameLoop.UpdateTicked");
            Assert(throwingCalls == 3, "The failing registration should open its circuit breaker after three consecutive failures.");
            Assert(stableCalls == 5, "A failing handler must not block later handlers in the same snapshot.");
            Assert(quarantined.HandlerCalls == 8 && quarantined.HandlerFailures == 3 && quarantined.HandlerQuarantines == 1, "Per-slot handler counters should report calls, failures and quarantine exactly.");
            Assert(quarantined.ActiveHandlers == 1 && quarantined.QuarantinedHandlers == 1, "Quarantine should remove only the failed registration from the active COW snapshot.");

            shouldThrow = false;
            events.GameLoop.UpdateTicked += throwing;
            manager.DispatchUpdateTicked(6);
            Assert(throwingCalls == 4 && stableCalls == 6, "An explicit later subscription must create a fresh usable registration in the same process.");
        }

        private static void PerOwnerCountersAreAttributedAndDiagnosticProjectionIsBounded()
        {
            EventManager manager = CreateManager(mainThreadBoundaryEnabled: false);
            IEventsHelper first = manager.CreateProxy("DTMAPI.Tests.Batch5.OwnerCounters.A");
            IEventsHelper second = manager.CreateProxy("DTMAPI.Tests.Batch5.OwnerCounters.B");
            first.GameLoop.UpdateTicked += (_, _) => throw new InvalidOperationException("owner-a-first");
            first.GameLoop.UpdateTicked += (_, _) => { };
            second.GameLoop.UpdateTicked += (_, _) => { };
            manager.DispatchUpdateTicked(1);

            EventSlotSnapshot attributed = FindSlot(manager, "GameLoop.UpdateTicked");
            EventOwnerSlotCounts ownerA = attributed.ByOwner["DTMAPI.Tests.Batch5.OwnerCounters.A"];
            EventOwnerSlotCounts ownerB = attributed.ByOwner["DTMAPI.Tests.Batch5.OwnerCounters.B"];
            Assert(ownerA.ActiveHandlers == 2 && ownerA.HandlerCalls == 2 && ownerA.HandlerFailures == 1, "Two handlers owned by A must retain owner-attributed call/failure counts.");
            Assert(ownerB.ActiveHandlers == 1 && ownerB.HandlerCalls == 1 && ownerB.HandlerFailures == 0, "The later healthy owner must retain independent counters after A fails.");

            for (int index = 0; index < 140; index++)
            {
                manager.CreateProxy("DTMAPI.Tests.Batch5.BoundedOwner." + index.ToString("000"))
                    .GameLoop.UpdateTicked += (_, _) => { };
            }
            EventSlotSnapshot bounded = FindSlot(manager, "GameLoop.UpdateTicked");
            Assert(bounded.ByOwner.Count == bounded.NamedOwnerCapacity && bounded.NamedOwnerCapacity == 128, "Per-slot owner diagnostics must never exceed their fixed named-owner capacity.");
            Assert(bounded.TotalOwnerCount == 142 && bounded.TrimmedOwnerCount == 14 && bounded.TrimmedActiveHandlers == 14, "The bounded projection must retain exact omitted-owner and active-handler aggregates.");
            Assert(manager.CountOwnerResources("DTMAPI.Tests.Batch5.BoundedOwner.139") == 1, "Authoritative owner-root counting must remain exact outside the truncated diagnostic projection.");
        }

        private static void HandlerTimingIsExplicitDiagnosticModeOnly()
        {
            EventManager normal = CreateManager(mainThreadBoundaryEnabled: false);
            normal.CreateProxy("DTMAPI.Tests.Batch5.Timing.Default").GameLoop.UpdateTicked += (_, _) => { };
            normal.DispatchUpdateTicked(1);
            EventSlotSnapshot defaultSnapshot = FindSlot(normal, "GameLoop.UpdateTicked");
            Assert(!defaultSnapshot.HandlerTimingEnabled && defaultSnapshot.HandlerTimingSamples == 0, "The default player hot path must not read clocks for handler timing.");

            EventManager diagnostic = CreateManager(
                mainThreadBoundaryEnabled: false,
                options: new EventKernelOptions(measureHandlerTiming: true));
            diagnostic.CreateProxy("DTMAPI.Tests.Batch5.Timing.Explicit").GameLoop.UpdateTicked += (_, _) => Thread.SpinWait(10_000);
            diagnostic.DispatchUpdateTicked(1);
            EventSlotSnapshot explicitSnapshot = FindSlot(diagnostic, "GameLoop.UpdateTicked");
            Assert(explicitSnapshot.HandlerTimingEnabled && explicitSnapshot.HandlerTimingSamples == 1, "Explicit diagnostic mode must record exactly one sample per invoked handler.");
            Assert(explicitSnapshot.HandlerTimingTotalTicks >= explicitSnapshot.HandlerTimingMaxTicks && explicitSnapshot.HandlerTimingMaxTicks > 0, "Explicit timing must expose bounded aggregate total/max ticks without per-call receipts.");

            const int measuredDispatches = 50_000;
            for (int index = 0; index < 1_000; index++)
            {
                normal.DispatchUpdateTicked((ulong)index);
                diagnostic.DispatchUpdateTicked((ulong)index);
            }
            long defaultAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            Stopwatch clock = Stopwatch.StartNew();
            for (int index = 0; index < measuredDispatches; index++)
                normal.DispatchUpdateTicked((ulong)index);
            clock.Stop();
            long defaultElapsedTicks = clock.ElapsedTicks;
            long defaultAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - defaultAllocatedBefore;

            long diagnosticAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            clock.Restart();
            for (int index = 0; index < measuredDispatches; index++)
                diagnostic.DispatchUpdateTicked((ulong)index);
            clock.Stop();
            long diagnosticElapsedTicks = clock.ElapsedTicks;
            long diagnosticAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - diagnosticAllocatedBefore;
            Console.WriteLine(
                "Batch5EventTimingBenchmark: dispatches=" + measuredDispatches +
                "; defaultElapsedTicks=" + defaultElapsedTicks +
                "; diagnosticElapsedTicks=" + diagnosticElapsedTicks +
                "; defaultAllocatedBytes=" + defaultAllocatedBytes +
                "; diagnosticAllocatedBytes=" + diagnosticAllocatedBytes +
                "; forcedGc=false");
        }

        private static void ListenerTransitionsReportFirstLastOwnerAndQuarantineChanges()
        {
            var transitions = new List<EventListenerTransition>();
            EventManager manager = CreateManager(mainThreadBoundaryEnabled: false, listenerTransition: transitions.Add);
            const string owner = "DTMAPI.Tests.Batch5.Transitions";
            IEventsHelper events = manager.CreateProxy(owner);
            EventHandler<UpdateTickedEventArgs> failing = (_, _) => throw new InvalidOperationException("batch5-transition-quarantine");
            EventHandler<UpdateTickedEventArgs> second = (_, _) => { };

            events.GameLoop.UpdateTicked += failing;
            events.GameLoop.UpdateTicked += second;
            events.GameLoop.UpdateTicked -= second;
            manager.DispatchUpdateTicked(1);
            manager.DispatchUpdateTicked(2);
            manager.DispatchUpdateTicked(3);
            events.GameLoop.UpdateTicked += failing;
            manager.RemoveOwner(owner);

            Assert(transitions.Count == 6, "Every actual add/remove/quarantine/owner cleanup should publish one lock-free transition.");
            Assert(transitions[0].Reason == EventListenerTransitionReason.Added && transitions[0].BecameActive, "The first listener should publish 0->1 demand.");
            Assert(transitions[1].Reason == EventListenerTransitionReason.Added && !transitions[1].BecameActive, "A later listener should not duplicate first-listener demand.");
            Assert(transitions[2].Reason == EventListenerTransitionReason.Removed && transitions[2].PreviousCount == 2 && transitions[2].CurrentCount == 1, "Ordinary removal should expose precise counts.");
            Assert(transitions[3].Reason == EventListenerTransitionReason.Quarantined && transitions[3].BecameInactive, "Quarantining the last listener should publish 1->0 demand.");
            Assert(transitions[4].BecameActive, "Explicit resubscription after quarantine should publish a new 0->1 transition.");
            Assert(transitions[5].Reason == EventListenerTransitionReason.OwnerRemoved && transitions[5].BecameInactive, "Owner cleanup should publish the final 1->0 transition outside the slot lock.");
        }

        private static void ConcurrentMembershipTransitionsDrainInMutationOrder()
        {
            using var firstCallbackEntered = new ManualResetEvent(false);
            using var releaseFirstCallback = new ManualResetEvent(false);
            int projectedOwnerDemand = -1;
            var callbackOrder = new List<int>();
            EventManager manager = CreateManager(
                mainThreadBoundaryEnabled: false,
                listenerTransition: transition =>
                {
                    if (transition.EventName != "GameLoop.UpdateTicked")
                        return;
                    if (transition.CurrentOwnerCount == 1 && callbackOrder.Count == 0)
                    {
                        firstCallbackEntered.Set();
                        if (!releaseFirstCallback.WaitOne(TimeSpan.FromSeconds(5)))
                            throw new TimeoutException("Concurrent transition test did not release the first callback.");
                    }
                    callbackOrder.Add(transition.CurrentOwnerCount);
                    Volatile.Write(ref projectedOwnerDemand, transition.CurrentOwnerCount);
                });

            IEventsHelper events = manager.CreateProxy("DTMAPI.Tests.Batch5.ConcurrentTransitions");
            EventHandler<UpdateTickedEventArgs> handler = (_, _) => { };
            Exception? addFailure = null;
            var addThread = new Thread(() =>
            {
                try
                {
                    events.GameLoop.UpdateTicked += handler;
                }
                catch (Exception ex)
                {
                    addFailure = ex;
                }
            });
            addThread.Start();
            Assert(firstCallbackEntered.WaitOne(TimeSpan.FromSeconds(5)), "The first add transition should enter its ordered publisher.");
            Exception? removeFailure = null;
            var removeThread = new Thread(() =>
            {
                try
                {
                    events.GameLoop.UpdateTicked -= handler;
                }
                catch (Exception ex)
                {
                    removeFailure = ex;
                }
            });
            removeThread.Start();
            DateTime removeDeadline = DateTime.UtcNow.AddSeconds(5);
            while (FindSlot(manager, "GameLoop.UpdateTicked").ActiveHandlers != 0 && DateTime.UtcNow < removeDeadline)
                Thread.Yield();
            Assert(FindSlot(manager, "GameLoop.UpdateTicked").ActiveHandlers == 0, "The later removal must commit membership while its transition waits for the earlier publisher.");
            releaseFirstCallback.Set();
            addThread.Join();
            removeThread.Join();
            if (addFailure != null)
                throw new InvalidOperationException("Concurrent add failed.", addFailure);
            if (removeFailure != null)
                throw new InvalidOperationException("Concurrent remove failed.", removeFailure);

            Assert(callbackOrder.SequenceEqual(new[] { 1, 0 }), "Concurrent add/remove callbacks must drain in membership mutation order.");
            Assert(projectedOwnerDemand == 0 && FindSlot(manager, "GameLoop.UpdateTicked").ActiveHandlers == 0, "A delayed add callback must not resurrect event demand after the later removal.");
        }

        private static void TransitionCallbackReentryIsRejectedWithoutStrandingLaterTickets()
        {
            IEventsHelper? events = null;
            bool attemptedReentry = false;
            bool rejectedReentry = false;
            int publishedTransitions = 0;
            EventHandler<UpdateTickedEventArgs> nested = (_, _) => { };
            EventManager manager = CreateManager(
                mainThreadBoundaryEnabled: false,
                listenerTransition: transition =>
                {
                    if (transition.EventName != "GameLoop.UpdateTicked")
                        return;

                    publishedTransitions++;
                    if (attemptedReentry)
                        return;
                    attemptedReentry = true;
                    try
                    {
                        events!.GameLoop.UpdateTicked += nested;
                    }
                    catch (InvalidOperationException)
                    {
                        rejectedReentry = true;
                    }
                });
            events = manager.CreateProxy("DTMAPI.Tests.Batch5.TransitionReentry");

            events.GameLoop.UpdateTicked += (_, _) => { };
            Assert(rejectedReentry && FindSlot(manager, "GameLoop.UpdateTicked").ActiveHandlers == 1, "Same-slot callback reentry must be rejected before it mutates authoritative membership.");

            events.GameLoop.UpdateTicked += nested;
            Assert(publishedTransitions == 2 && FindSlot(manager, "GameLoop.UpdateTicked").ActiveHandlers == 2, "A rejected callback reentry must not strand the publication ticket sequence.");
        }

        private static void QueuePoliciesAreBoundedCoalescedAndFailureIsolated()
        {
            int runtimeThreadId = Thread.CurrentThread.ManagedThreadId;
            var fifoOptions = new EventKernelOptions(queueCapacity: 2, flushBudget: 1);
            EventManager fifo = CreateManager(true, runtimeThreadId, fifoOptions);
            IEventsHelper fifoEvents = fifo.CreateProxy("DTMAPI.Tests.Batch5.Fifo");
            var saveSlots = new List<int?>();
            fifoEvents.Save.SaveLoaded += (_, args) => saveSlots.Add(args.SaveSlot);

            RunOffThread(() =>
            {
                fifo.DispatchSaveLoaded(1, isNewGame: false);
                fifo.DispatchSaveLoaded(2, isNewGame: false);
                fifo.DispatchSaveLoaded(3, isNewGame: false);
            });

            EventDispatchBoundarySnapshot full = fifo.GetBoundarySnapshot();
            Assert(full.Queued == 2 && full.Capacity == 2 && full.MaxQueueDepth == 2, "FIFO queue depth must never exceed its injected capacity.");
            Assert(full.TotalDropped == 1 && full.TotalOverflow == 1, "A full non-coalescing FIFO should deterministically drop the newest publication and report overflow.");
            Assert(fifo.FlushQueuedDispatches() == 1 && saveSlots.SequenceEqual(new int?[] { 1 }), "The flush budget should commit only the first FIFO item.");
            Assert(fifo.FlushQueuedDispatches() == 1 && saveSlots.SequenceEqual(new int?[] { 1, 2 }), "The next safe point should commit the second FIFO item in order.");
            Assert(fifo.FlushQueuedDispatches() == 0, "The dropped third FIFO item must never appear later.");

            EventManager crossPolicy = CreateManager(true, runtimeThreadId, new EventKernelOptions(queueCapacity: 2, flushBudget: 4));
            IEventsHelper crossEvents = crossPolicy.CreateProxy("DTMAPI.Tests.Batch5.CrossPolicy");
            var protectedFifo = new List<int?>();
            int overflowedCoalesceCalls = 0;
            crossEvents.Save.SaveLoaded += (_, args) => protectedFifo.Add(args.SaveSlot);
            crossEvents.Workshop.ModListChanged += (_, _) => overflowedCoalesceCalls++;
            RunOffThread(() =>
            {
                crossPolicy.DispatchSaveLoaded(11, isNewGame: false);
                crossPolicy.DispatchSaveLoaded(12, isNewGame: false);
                crossPolicy.DispatchWorkshopModListChanged(99);
            });
            EventDispatchBoundarySnapshot crossSnapshot = crossPolicy.GetBoundarySnapshot();
            Assert(crossSnapshot.Queued == 2 && crossSnapshot.ByPolicy[EventDispatchPolicy.BoundedFifo].Pending == 2, "A new coalesced item must not evict critical FIFO lifecycle observations.");
            Assert(crossSnapshot.ByPolicy[EventDispatchPolicy.CoalesceLatestByKey].TotalDropped == 1 && !crossSnapshot.Success, "Cross-policy overflow should drop the incoming coalesced item and make boundary health warning.");
            Assert(crossPolicy.FlushQueuedDispatches() == 2 && protectedFifo.SequenceEqual(new int?[] { 11, 12 }) && overflowedCoalesceCalls == 0, "Both protected FIFO items must retain their order across coalesce overflow.");

            EventManager coalesceEviction = CreateManager(true, runtimeThreadId, new EventKernelOptions(queueCapacity: 2, flushBudget: 4));
            int criticalCalls = 0;
            int oldCoalescedCalls = 0;
            int latestCoalescedCalls = 0;
            coalesceEviction.EnqueueTestDispatch("Batch5.Critical", EventDispatchPolicy.BoundedFifo, null, () => criticalCalls++);
            coalesceEviction.EnqueueTestDispatch("Batch5.State", EventDispatchPolicy.CoalesceLatestByKey, "old", () => oldCoalescedCalls++);
            coalesceEviction.EnqueueTestDispatch("Batch5.State", EventDispatchPolicy.CoalesceLatestByKey, "latest", () => latestCoalescedCalls++);
            Assert(coalesceEviction.FlushQueuedDispatches() == 2, "A full mixed queue should retain exactly its bounded capacity.");
            Assert(criticalCalls == 1 && oldCoalescedCalls == 0 && latestCoalescedCalls == 1, "Overflow may evict the oldest coalescable state but must retain the FIFO item and newest state.");

            var coalesceOptions = new EventKernelOptions(queueCapacity: 2, flushBudget: 8);
            EventManager coalesced = CreateManager(true, runtimeThreadId, coalesceOptions);
            IEventsHelper coalescedEvents = coalesced.CreateProxy("DTMAPI.Tests.Batch5.Coalesce");
            var workshopCounts = new List<int>();
            coalescedEvents.Workshop.ModListChanged += (_, args) => workshopCounts.Add(args.ModCount);
            RunOffThread(() =>
            {
                coalesced.DispatchWorkshopModListChanged(10);
                coalesced.DispatchWorkshopModListChanged(20);
                coalesced.DispatchWorkshopModListChanged(30);
            });
            EventDispatchBoundarySnapshot coalescedSnapshot = coalesced.GetBoundarySnapshot();
            Assert(coalescedSnapshot.Queued == 1 && coalescedSnapshot.TotalCoalesced == 2, "Latest-by-key should retain one bounded queue node and count both replacements.");
            Assert(coalesced.FlushQueuedDispatches() == 1 && workshopCounts.SequenceEqual(new[] { 30 }), "Latest-by-key should publish only the newest payload.");

            EventManager longKeys = CreateManager(true, runtimeThreadId, new EventKernelOptions(queueCapacity: 4, flushBudget: 4));
            int longKeyCalls = 0;
            string commonPrefix = new string('K', 300);
            longKeys.EnqueueTestDispatch("Batch5.LongKey", EventDispatchPolicy.CoalesceLatestByKey, commonPrefix + "A", () => longKeyCalls++);
            longKeys.EnqueueTestDispatch("Batch5.LongKey", EventDispatchPolicy.CoalesceLatestByKey, commonPrefix + "B", () => longKeyCalls++);
            EventDispatchBoundarySnapshot longKeySnapshot = longKeys.GetBoundarySnapshot();
            Assert(longKeySnapshot.Queued == 2 && longKeySnapshot.TotalCoalesced == 0, "Hash-suffixed bounded identities must not coalesce distinct keys that share a long prefix.");
            Assert(longKeys.FlushQueuedDispatches() == 2 && longKeyCalls == 2, "Both distinct long-key states must survive the bounded queue.");

            EventManager isolated = CreateManager(true, runtimeThreadId, new EventKernelOptions(queueCapacity: 4, flushBudget: 4));
            int laterCalls = 0;
            isolated.EnqueueTestDispatch("Batch5.QueueFault", EventDispatchPolicy.BoundedFifo, null, () => throw new InvalidOperationException("queue-fault"));
            isolated.EnqueueTestDispatch("Batch5.QueueLater", EventDispatchPolicy.BoundedFifo, null, () => laterCalls++);
            Assert(isolated.FlushQueuedDispatches() == 2 && laterCalls == 1, "A failing queue item must not block a later item in the same bounded flush.");
            Assert(isolated.GetBoundarySnapshot().TotalQueueDispatchFailures == 1, "Queue item failures should be explicit diagnostics counters.");

            EventManager rejected = CreateManager(true, runtimeThreadId);
            IEventsHelper rejectedEvents = rejected.CreateProxy("DTMAPI.Tests.Batch5.DirectReject");
            rejectedEvents.GameLoop.UpdateTicked += (_, _) => { };
            rejectedEvents.GameLoop.GameLaunched += (_, _) => { };
            RunOffThread(() =>
            {
                rejected.DispatchUpdateTicked(1);
                rejected.DispatchGameLaunched();
            });
            EventSlotSnapshot rejectedSlot = FindSlot(rejected, "GameLoop.UpdateTicked");
            EventSlotSnapshot rejectedGameLaunched = FindSlot(rejected, "GameLoop.GameLaunched");
            Assert(rejected.GetBoundarySnapshot().ByPolicy[EventDispatchPolicy.Direct].TotalRejected == 1, "Direct event classes should explicitly reject unexpected non-runtime-thread publications.");
            Assert(rejected.GetBoundarySnapshot().ByPolicy[EventDispatchPolicy.Reject].TotalRejected == 1, "Reject event classes should expose their distinct policy diagnostics.");
            Assert(rejectedSlot.EventArgsCreated == 0 && rejectedGameLaunched.EventArgsCreated == 0, "Rejected publications should stop before EventArgs construction.");

            EventManager warningBound = CreateManager(true, runtimeThreadId, new EventKernelOptions(queueCapacity: 1, flushBudget: 1));
            warningBound.EnqueueTestDispatch("Batch5.Warning.Seed", EventDispatchPolicy.BoundedFifo, null, () => { });
            for (int index = 0; index < 140; index++)
                warningBound.EnqueueTestDispatch("Batch5.Warning." + index, EventDispatchPolicy.BoundedFifo, null, () => { });
            Assert(warningBound.GetBoundarySnapshot().TrimmedWarningKeyCount == 12, "Warning-key diagnostics must retain at most 128 distinct keys and aggregate overflow into a scalar.");
        }

        private static void QueuedRecipientsExcludeRemovedOwnersAndLateSubscribers()
        {
            int runtimeThreadId = Thread.CurrentThread.ManagedThreadId;
            EventManager ordinaryRemoval = CreateManager(true, runtimeThreadId, new EventKernelOptions(queueCapacity: 4, flushBudget: 4));
            IEventsHelper ordinaryEvents = ordinaryRemoval.CreateProxy("DTMAPI.Tests.Batch5.OrdinaryQueuedRemove");
            int frozenOrdinaryCalls = 0;
            EventHandler<SaveLoadedEventArgs> ordinaryHandler = (_, _) => frozenOrdinaryCalls++;
            ordinaryEvents.Save.SaveLoaded += ordinaryHandler;
            RunOffThread(() => ordinaryRemoval.DispatchSaveLoaded(6, isNewGame: false));
            ordinaryEvents.Save.SaveLoaded -= ordinaryHandler;
            Assert(ordinaryRemoval.FlushQueuedDispatches() == 1 && frozenOrdinaryCalls == 1, "Ordinary -= after occurrence must not rewrite a queued publication's frozen membership.");
            ordinaryRemoval.DispatchSaveLoaded(7, isNewGame: false);
            Assert(ordinaryRemoval.FlushQueuedDispatches() == 0 && frozenOrdinaryCalls == 1, "A later publication must observe the ordinary removal.");

            EventManager manager = CreateManager(true, runtimeThreadId, new EventKernelOptions(queueCapacity: 4, flushBudget: 4));
            int removedOwnerCalls = 0;
            int lateSubscriberCalls = 0;
            manager.CreateProxy("DTMAPI.Tests.Batch5.Removed").Save.SaveLoaded += (_, _) => removedOwnerCalls++;

            RunOffThread(() => manager.DispatchSaveLoaded(7, isNewGame: false));
            Assert(manager.RemoveOwner("DTMAPI.Tests.Batch5.Removed") == 1, "Owner cleanup should remove the queued publication's original registration root.");
            manager.CreateProxy("DTMAPI.Tests.Batch5.Late").Save.SaveLoaded += (_, _) => lateSubscriberCalls++;
            Assert(manager.FlushQueuedDispatches() == 1, "The observation itself should still commit at the safe point.");
            Assert(removedOwnerCalls == 0, "Owner cleanup before commit must prevent invocation of a stale delegate.");
            Assert(lateSubscriberCalls == 0, "A listener registered after event occurrence must not receive the stale queued payload.");
        }

        private static EventManager CreateManager(
            bool mainThreadBoundaryEnabled,
            int? runtimeThreadId = null,
            EventKernelOptions? options = null,
            Action<EventListenerTransition>? listenerTransition = null)
        {
            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "batch5-event-kernel");
            var paths = new RuntimePaths(root, root);
            var diagnostics = new DiagnosticsService(paths);
            int ownerThread = runtimeThreadId ?? Thread.CurrentThread.ManagedThreadId;
            return new EventManager(
                diagnostics,
                () => mainThreadBoundaryEnabled,
                () => true,
                () => ownerThread,
                () => "Batch5.EventKernelTests",
                kernelOptions: options,
                listenerTransition: listenerTransition);
        }

        private static EventSlotSnapshot FindSlot(EventManager manager, string eventName)
        {
            return manager.GetHandlerCleanupSnapshot().Slots.Single(slot => slot.EventName == eventName);
        }

        private static void RunOffThread(Action action)
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });
            thread.Start();
            thread.Join();
            if (failure != null)
                throw new InvalidOperationException("Batch 5 worker-thread probe failed.", failure);
        }

        private sealed class TestRuntimeHost : IRuntimeHost
        {
            public TestRuntimeHost(string root)
            {
                GamePath = root;
                PluginPath = Path.Combine(root, "BepInEx", "plugins", "DTMAPI");
            }

            public string GamePath { get; }
            public string PluginPath { get; }
            public string HostName => "Batch5EventKernelTests";
            public void Log(string message) { }
            public void LogWarning(string message) { }
            public void LogError(string message, Exception? exception = null) { }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
