using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class RuntimeThreadAllocationProbe
    {
        private const int CalibrationPayloadBytes = 4096;
        private readonly Func<long>? getAllocatedBytes;
        private readonly Action allocateCalibrationBuffer;
        private long baseline;
        private bool started;

        internal RuntimeThreadAllocationProbe()
            : this(ResolveAllocatedBytesGetter(), AllocateCalibrationBuffer)
        {
        }

        internal RuntimeThreadAllocationProbe(Func<long>? allocatedBytesGetter, Action? calibrationAllocation)
        {
            getAllocatedBytes = allocatedBytesGetter;
            allocateCalibrationBuffer = calibrationAllocation ?? AllocateCalibrationBuffer;
            Result = new RuntimeThreadAllocationResult
            {
                CounterAvailable = getAllocatedBytes != null,
                CounterStatus = getAllocatedBytes == null ? "unavailable" : "untested",
                ProbeStatus = getAllocatedBytes == null ? "blocked-allocation-counter-unavailable" : "pending",
                CalibrationPayloadBytes = CalibrationPayloadBytes,
                FailureReason = getAllocatedBytes == null ? "GC.GetAllocatedBytesForCurrentThread is unavailable." : string.Empty
            };
        }

        internal RuntimeThreadAllocationResult Result { get; }

        internal void Start()
        {
            if (started)
                return;
            started = true;
            if (getAllocatedBytes == null)
                return;
            try
            {
                long before = getAllocatedBytes();
                allocateCalibrationBuffer();
                long after = getAllocatedBytes();
                long delta = Math.Max(0L, after - before);
                Result.CalibrationBefore = before;
                Result.CalibrationAfter = after;
                Result.CalibrationDelta = delta;
                if (delta < CalibrationPayloadBytes)
                {
                    Block("nonfunctional", "blocked-allocation-counter-nonfunctional", "GC.GetAllocatedBytesForCurrentThread did not advance by the known 4096-byte same-thread allocation.");
                    return;
                }
                baseline = getAllocatedBytes();
                Result.CounterFunctional = true;
                Result.CounterStatus = "functional";
                Result.ProbeStatus = "measuring";
            }
            catch (Exception ex)
            {
                Block("error", "blocked-allocation-counter-error", "Allocation counter calibration or baseline failed: " + ex.GetType().Name + ".");
            }
        }

        internal void Complete(long measuredUnits)
        {
            if (!started)
                Start();
            if (!Result.CounterFunctional || getAllocatedBytes == null)
                return;
            try
            {
                long allocated = Math.Max(0L, getAllocatedBytes() - baseline);
                Result.AllocatedBytes = allocated;
                Result.AllocatedBytesPerUnit = measuredUnits > 0 ? (double)allocated / measuredUnits : null;
                Result.ProbeStatus = "completed";
            }
            catch (Exception ex)
            {
                Block("error", "blocked-allocation-counter-error", "Allocation counter completion failed: " + ex.GetType().Name + ".");
            }
        }

        private void Block(string counterStatus, string probeStatus, string reason)
        {
            Result.CounterFunctional = false;
            Result.CounterStatus = counterStatus;
            Result.ProbeStatus = probeStatus;
            Result.FailureReason = reason;
            Result.AllocatedBytes = null;
            Result.AllocatedBytesPerUnit = null;
        }

        internal static Func<long>? ResolveAllocatedBytesGetter()
        {
            try
            {
                MethodInfo? method = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                return method == null || method.ReturnType != typeof(long)
                    ? null
                    : Expression.Lambda<Func<long>>(Expression.Call(method)).Compile();
            }
            catch
            {
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AllocateCalibrationBuffer()
        {
            var buffer = new byte[CalibrationPayloadBytes];
            buffer[0] = 1;
            buffer[buffer.Length - 1] = 1;
            GC.KeepAlive(buffer);
        }
    }

    [DataContract]
    internal sealed class RuntimeThreadAllocationResult
    {
        [DataMember] public bool CounterAvailable { get; set; }
        [DataMember] public bool CounterFunctional { get; set; }
        [DataMember] public string CounterStatus { get; set; } = string.Empty;
        [DataMember] public string ProbeStatus { get; set; } = string.Empty;
        [DataMember] public string FailureReason { get; set; } = string.Empty;
        [DataMember] public int CalibrationPayloadBytes { get; set; }
        [DataMember] public long CalibrationBefore { get; set; }
        [DataMember] public long CalibrationAfter { get; set; }
        [DataMember] public long CalibrationDelta { get; set; }
        [DataMember] public long? AllocatedBytes { get; set; }
        [DataMember] public double? AllocatedBytesPerUnit { get; set; }
    }
}
