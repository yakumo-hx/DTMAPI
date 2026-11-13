using System;
using System.IO;
using DTMAPI.Core.Runtime;
using DTMAPI.Testing;

namespace DTMAPI.UnitTests
{
    internal static class RuntimeBoundaryTests
    {
        internal static void RunAll()
        {
            InternalMulticastFailuresDoNotBlockLaterListeners();
            NativeFrameBoundaryReusesItsCopyOnWriteSnapshot();
        }

        private static void InternalMulticastFailuresDoNotBlockLaterListeners()
        {
            var runtime = CreateRuntime();
            int laterCalls = 0;
            runtime.NativeGameFrame += () => throw new InvalidOperationException("internal-boundary-probe");
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

        private static DtmApiRuntime CreateRuntime()
        {
            string root = Path.Combine(
                DtmApiTestSession.Current.RootPath,
                "runtime-boundary",
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
            public string HostName => "RuntimeBoundaryTests";
            public void Log(string message) { }
            public void LogWarning(string message) { }
            public void LogError(string message, Exception? exception = null) { }
        }
    }
}
