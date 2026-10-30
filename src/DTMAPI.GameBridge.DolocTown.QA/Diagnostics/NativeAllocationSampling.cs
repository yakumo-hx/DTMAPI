using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal static partial class NativeAllocationCalibration
    {
        [DllImport("kernel32")] private static extern uint GetCurrentThreadId();
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void dtmapi_gc_read([Out] long[] values, int length);
        private static void ReadGcEvents(long[] values) => dtmapi_gc_read(values, values.Length);
        private static MethodInfo? samplingBind;
        private static bool sampling;

        // Versioned private buffer contract, shared only by the explicitly requested QA seam.
        private static void ReadSampling(long[] values)
        {
            dtmapi_alloc_read(0, out var row);
            values[0] = row.ThreadBytes; values[1] = row.ThreadCount;
            values[2] = row.AllBytes; values[3] = row.AllCount;
            values[4] = row.ThreadId; values[5] = GetCurrentThreadId();
            values[6] = row.Active; values[7] = row.Status;
        }

        internal static void StartSampling(Action<string> report)
        {
            if (sampling) throw new InvalidOperationException("Allocation batch already started.");
            Type? probe = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var candidate = assembly.GetType("DTMAPI.Modules.Monsters.MonsterPerformanceProbe", false);
                if (candidate == null) continue;
                if (probe != null) throw new InvalidOperationException("Multiple monster probe assemblies loaded.");
                probe = candidate;
            }
            samplingBind = probe?.GetMethod("BindQaReader", BindingFlags.Static | BindingFlags.NonPublic);
            if (samplingBind == null) throw new InvalidOperationException("The current monster QA sampling seam is missing.");
            dtmapi_alloc_read(0, out var idle);
            if (idle.Status != 2 || idle.Active != 0 || idle.ThreadId != GetCurrentThreadId())
                throw new InvalidOperationException("Sampling must keep the calibrated main OS thread binding.");
            Action<long[]> reader = ReadSampling;
            var values = new long[8];
            reader(values);
            for (int i = 0; i < 3; i++) Allocate(i);
            if (dtmapi_alloc_start() != 1) throw new InvalidOperationException("Continuous allocation counting could not start.");
            sampling = true;
            try
            {
                var bytes = new long[3]; var events = new long[3];
                for (int i = 0; i < 3; i++)
                {
                    reader(values); long b = values[0], c = values[1];
                    Allocate(i); reader(values);
                    bytes[i] = values[0] - b; events[i] = values[1] - c;
                }
                if (bytes[0] != 0 || events[0] != 0 || bytes[1] != 4128 || events[1] != 1 || bytes[2] != 65568 || events[2] != 1)
                    throw new InvalidOperationException("Final QA allocation delegate failed known-allocation validation.");
                long overhead = (long)samplingBind.Invoke(null, new object[] { reader })!;
                report("MonsterAllocation reader ready; version=1; read10000Bytes=" + overhead +
                    "; controlBytes=" + bytes[0] + "; smallBytes=" + bytes[1] + "; largeBytes=" + bytes[2] +
                    "; osThread=" + idle.ThreadId + "; continuous=True; profiler=True");
            }
            catch { StopSampling(report); throw; }
        }

        internal static void StopSampling(Action<string> report)
        {
            if (!sampling) return;
            try { samplingBind?.Invoke(null, new object?[] { null }); }
            finally
            {
                dtmapi_alloc_stop(); sampling = false; samplingBind = null;
                report("MonsterAllocation sampling closed; callbacksDrained=False; nativeLibraryRetainedUntilExit=True");
            }
        }
    }
}
