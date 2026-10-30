using System;
using System.Diagnostics;
using System.Reflection;

namespace DTMAPI.Internal.HeartMap
{
    // Only the explicitly enabled, bounded diagnostic consumes/reset these counters.
    // No timer, reflection, allocation or frame subscription when Collect is false.
    internal static class MapMetrics
    {
        internal static bool Collect;
        internal static long Ticks, Bytes, Calls, ReflectionResolutions;
        internal static string AllocationCounterStatus = "not-calibrated";
        internal static readonly Func<long>? Allocated = ResolveAllocationCounter();
        private static Func<long>? ResolveAllocationCounter()
        {
            var method = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread", BindingFlags.Public | BindingFlags.Static);
            if (method == null) { AllocationCounterStatus = "unavailable: method absent"; return null; }
            try
            {
                var read = (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
                // Some shipping Mono versions expose this API but always return zero.
                // Calibrate once at initialization, outside all sampled windows.
                foreach (int size in new[] { 4096, 16384 })
                {
                    long before = read();
                    var probe = new byte[size];
                    GC.KeepAlive(probe);
                    long delta = read() - before;
                    if (delta < size)
                    {
                        AllocationCounterStatus = "unavailable: calibration delta=" + delta + "; probeBytes=" + size;
                        return null;
                    }
                }
                AllocationCounterStatus = "calibrated: 4096 and 16384 byte probes observed";
                return read;
            }
            catch (Exception ex) { AllocationCounterStatus = "unavailable: " + ex.GetType().Name; return null; }
        }
        internal static Sample Measure() => Collect ? new Sample(true) : default;
        internal static void Reset() { Ticks = Bytes = Calls = 0; }
        internal readonly struct Sample : IDisposable
        {
            private readonly long began, allocated;
            internal Sample(bool unused) { allocated = Allocated?.Invoke() ?? 0; began = Stopwatch.GetTimestamp(); }
            public void Dispose()
            {
                if (began == 0) return;
                Ticks += Stopwatch.GetTimestamp() - began;
                Bytes += (Allocated?.Invoke() ?? 0) - allocated;
                Calls++;
            }
        }
    }
}
