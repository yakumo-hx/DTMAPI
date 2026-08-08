using System;
using System.IO;
using System.Linq;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;
using DTMAPI.Testing;

namespace DTMAPI.UnitTests
{
    internal static class Batch5RuntimeBoundaryTests
    {
        internal static void RunAll()
        {
            InternalMulticastFailuresDoNotBlockLaterListeners();
            NativeFrameBoundaryReusesItsCopyOnWriteSnapshot();
            OffThreadSaveProducerIsRejectedBeforeStateMutation();
            EventMembershipPublishesOwnerAttributedDemand();
        }

        private static void InternalMulticastFailuresDoNotBlockLaterListeners()
        {
            var runtime = CreateRuntime();
            int laterCalls = 0;
            runtime.NativeGameFrame += () => throw new InvalidOperationException("batch5-internal-boundary-probe");
            runtime.NativeGameFrame += () => laterCalls++;

            runtime.NotifyNativeGameFrame();

            Assert(laterCalls == 1, "A failed internal boundary listener must not block a later listener.");
            Assert(runtime.Diagnostics.GetErrors().Count == 1, "The isolated internal listener failure should remain diagnosable.");
        }

        private static void NativeFrameBoundaryReusesItsCopyOnWriteSnapshot()
        {
            var runtime = CreateRuntime();
            int calls = 0;
            runtime.NativeGameFrame += () => calls++;
            for (int index = 0; index < 256; index++)
                runtime.NotifyNativeGameFrame();

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 10_000; index++)
                runtime.NotifyNativeGameFrame();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert(calls == 10_256, "The stable native-frame boundary listener should run once per publication.");
            Assert(allocated == 0, "A stable internal native-frame snapshot should allocate zero managed bytes across 10k publications; allocated=" + allocated + ".");
        }

        private static void OffThreadSaveProducerIsRejectedBeforeStateMutation()
        {
            var runtime = CreateRuntime();
            Exception? failure = null;
            var worker = new Thread(() =>
            {
                try
                {
                    runtime.NotifySaveLoaded(isNewGame: false);
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });
            worker.Start();
            worker.Join();

            if (failure != null)
                throw new InvalidOperationException("Off-thread producer probe threw unexpectedly.", failure);
            Assert(runtime.CurrentRuntimePhase == "Constructing", "Rejected SaveLoaded must not mutate the runtime phase.");
            Assert(runtime.SaveLoadRequestSnapshot.SaveLoadedDispatchCount == 0, "Rejected SaveLoaded must not mutate save-load coordination state.");
            Assert(runtime.EventDispatchBoundarySnapshot.TotalRejected == 1, "The rejected producer should emit one bounded event-boundary receipt.");
        }

        private static void EventMembershipPublishesOwnerAttributedDemand()
        {
            var runtime = CreateRuntime();
            const string owner = "DTMAPI.Tests.Batch5.EventDemand";
            IEventsHelper events = runtime.Events.CreateProxy(owner);
            Assert(runtime.RuntimeDemandSnapshot.TotalDemand == 0, "Creating or looking up an events facade must remain passive.");

            EventHandler<UpdateTickedEventArgs> handler = (_, _) => { };
            events.GameLoop.UpdateTicked += handler;
            events.GameLoop.UpdateTicked += handler;
            RuntimeCapabilityDemandSnapshot active = runtime.RuntimeDemandSnapshot.Capabilities.Single(capability => capability.CapabilityId == "Event.GameLoop.UpdateTicked");
            Assert(active.TotalDemand == 2 && active.ByOwner[owner] == 2 && active.BySource[RuntimeDemandSourceType.EventSubscription] == 2, "Duplicate subscriptions should publish exact owner-attributed event demand.");

            events.GameLoop.UpdateTicked -= handler;
            Assert(runtime.DemandCoordinator.GetDemandCount("Event.GameLoop.UpdateTicked") == 1, "Standard -= should release one matching demand registration.");
            runtime.Events.RemoveOwner(owner);
            Assert(!runtime.DemandCoordinator.HasDemand("Event.GameLoop.UpdateTicked"), "Owner event cleanup should release the final event demand.");
        }

        private static DtmApiRuntime CreateRuntime()
        {
            string root = Path.Combine(
                DtmApiTestSession.Current.RootPath,
                "batch5-runtime-boundary",
                Guid.NewGuid().ToString("N"));
            return new DtmApiRuntime(new TestRuntimeHost(root));
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
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
            public string HostName => "Batch5RuntimeBoundaryTests";
            public void Log(string message) { }
            public void LogWarning(string message) { }
            public void LogError(string message, Exception? exception = null) { }
        }
    }
}
