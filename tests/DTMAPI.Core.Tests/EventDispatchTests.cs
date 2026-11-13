using System;
using System.Collections.Generic;
using System.IO;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.Testing;

namespace DTMAPI.UnitTests
{
    public static class EventDispatchTests
    {
        public static void RunAll()
        {
            ZeroListenerDispatchDoesNotAllocate();
            CurrentDispatchIsFrozenAndNestedDispatchReadsLatestSnapshot();
            DuplicateRemovalUsesStandardLastRegistrationSemantics();
            OwnerBoundUnsubscribeDoesNotRegisterNewWork();
            HandlerFailuresAreIsolatedAndRemainSubscribed();
            ReleaseSettlementFreezesAllRecipientsOnce();
            SaveSavingReportsCallbackFailure();
        }

        private static void ZeroListenerDispatchDoesNotAllocate()
        {
            EventManager manager = CreateManager();
            for (int index = 0; index < 1024; index++)
                manager.DispatchUpdateTicked((ulong)index);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 10_000; index++)
                manager.DispatchUpdateTicked((ulong)index);
            Assert(GC.GetAllocatedBytesForCurrentThread() - before == 0, "A zero-listener update must not allocate event arguments.");
        }

        private static void OwnerBoundUnsubscribeDoesNotRegisterNewWork()
        {
            bool active = true;
            EventManager manager = CreateManager();
            IEventsHelper events = manager.CreateOwnerBoundProxy("Owner", () =>
            {
                if (!active)
                    throw new InvalidOperationException("owner inactive");
            });
            int calls = 0;
            EventHandler<UpdateTickedEventArgs> handler = (_, _) => calls++;
            events.GameLoop.UpdateTicked += handler;
            active = false;
            events.GameLoop.UpdateTicked -= handler;
            manager.DispatchUpdateTicked(1);
            Assert(calls == 0, "An inactive owner must still be able to release its registration.");
            bool rejected = false;
            try { events.GameLoop.UpdateTicked += handler; }
            catch (InvalidOperationException) { rejected = true; }
            Assert(rejected, "An inactive owner must not register new work.");
        }

        private static void SaveSavingReportsCallbackFailure()
        {
            EventManager manager = CreateManager();
            IEventsHelper events = manager.CreateProxy("Owner");
            EventHandler<SaveSavingEventArgs> failing = (_, _) => throw new InvalidOperationException("save callback failed");
            events.Save.SaveSaving += failing;
            Assert(!manager.TryDispatchSaveSaving(12), "The native-save boundary must still see a Mod callback failure.");
            events.Save.SaveSaving -= failing;
            Assert(manager.TryDispatchSaveSaving(12), "Removing the failing callback must restore success.");
        }

        private static EventManager CreateManager()
        {
            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "event-dispatch");
            var diagnostics = new DiagnosticsService(new RuntimePaths(root, root));
            return new EventManager(diagnostics);
        }

        private static void ReleaseSettlementFreezesAllRecipientsOnce()
        {
            EventManager manager = CreateManager();
            IEventsHelper first = manager.CreateProxy("Owner.A");
            IEventsHelper second = manager.CreateProxy("Owner.B");
            var trace = new List<string>();
            bool addLate = true;
            first.Input.ButtonReleased += (_, _) =>
            {
                trace.Add("A");
                if (addLate)
                {
                    addLate = false;
                    second.Input.ButtonReleased += (_, _) => trace.Add("late");
                }
            };
            second.Input.ButtonReleased += (_, _) => trace.Add("B");
            manager.DispatchButtonReleasedToOwners(new[] { "Owner.A", "Owner.B", "owner.a" }, "F10");
            Assert(string.Join(",", trace) == "A,B", "One release freezes every recipient once, excluding listeners added by an earlier owner and duplicate owners.");

            trace.Clear();
            manager.DispatchButtonReleasedToOwners(new[] { "Owner.B", "Owner.A" }, "F10");
            Assert(string.Join(",", trace) == "B,late,A", "The next release sees the new membership while retaining recipient and registration order.");
        }

        private static void CurrentDispatchIsFrozenAndNestedDispatchReadsLatestSnapshot()
        {
            EventManager manager = CreateManager();
            IEventsHelper events = manager.CreateProxy("DTMAPI.Tests.Mutation");
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

        private static void DuplicateRemovalUsesStandardLastRegistrationSemantics()
        {
            EventManager manager = CreateManager();
            IEventsHelper events = manager.CreateProxy("DTMAPI.Tests.Duplicate");
            int calls = 0;
            EventHandler<UpdateTickedEventArgs> handler = (_, _) => calls++;
            events.GameLoop.UpdateTicked += handler;
            events.GameLoop.UpdateTicked += handler;
            events.GameLoop.UpdateTicked -= handler;

            manager.DispatchUpdateTicked(1);

            Assert(calls == 1, "A single -= must remove only the last matching owner/delegate registration.");
        }

        private static void HandlerFailuresAreIsolatedAndRemainSubscribed()
        {
            EventManager manager = CreateManager();
            IEventsHelper events = manager.CreateProxy("DTMAPI.Tests.EventFailure");
            int throwingCalls = 0;
            int stableCalls = 0;
            bool shouldThrow = true;
            EventHandler<UpdateTickedEventArgs> throwing = (_, _) =>
            {
                throwingCalls++;
                if (shouldThrow)
                    throw new InvalidOperationException("event-failure-probe");
            };
            events.GameLoop.UpdateTicked += throwing;
            events.GameLoop.UpdateTicked += (_, _) => stableCalls++;

            for (int index = 0; index < 5; index++)
                manager.DispatchUpdateTicked((ulong)index);

            Assert(throwingCalls == 5, "A callback failure must not silently unsubscribe the Mod from later events.");
            Assert(stableCalls == 5, "A failing handler must not block later handlers in the same snapshot.");

            shouldThrow = false;
            manager.DispatchUpdateTicked(6);
            Assert(throwingCalls == 6 && stableCalls == 6, "The original subscription must remain usable after the callback recovers.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
