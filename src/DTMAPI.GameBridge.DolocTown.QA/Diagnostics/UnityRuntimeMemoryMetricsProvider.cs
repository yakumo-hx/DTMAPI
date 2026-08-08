using System;
using System.Linq.Expressions;
using System.Reflection;
namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class UnityRuntimeMemoryMetricsProvider
    {
        private readonly Func<long>? getMonoUsed;
        private readonly Func<long>? getMonoHeap;
        private readonly Func<long>? getUnityAllocated;
        private readonly Func<long>? getUnityReserved;
        private readonly Func<long>? getUnityUnusedReserved;

        internal UnityRuntimeMemoryMetricsProvider()
            : this(Type.GetType("UnityEngine.Profiling.Profiler, UnityEngine.CoreModule", throwOnError: false))
        {
        }

        internal UnityRuntimeMemoryMetricsProvider(Type? profilerType)
        {
            getMonoUsed = Build(profilerType, "GetMonoUsedSizeLong");
            getMonoHeap = Build(profilerType, "GetMonoHeapSizeLong");
            getUnityAllocated = Build(profilerType, "GetTotalAllocatedMemoryLong");
            getUnityReserved = Build(profilerType, "GetTotalReservedMemoryLong");
            getUnityUnusedReserved = Build(profilerType, "GetTotalUnusedReservedMemoryLong");
            AvailableMetricCount = Count(getMonoUsed, getMonoHeap, getUnityAllocated, getUnityReserved, getUnityUnusedReserved);
        }

        internal int AvailableMetricCount { get; }
        internal int InvocationFailures { get; private set; }

        internal RuntimePlatformMemorySnapshot Capture()
        {
            return new RuntimePlatformMemorySnapshot(
                Invoke(getMonoUsed),
                Invoke(getMonoHeap),
                Invoke(getUnityAllocated),
                Invoke(getUnityReserved),
                Invoke(getUnityUnusedReserved));
        }

        private long? Invoke(Func<long>? getter)
        {
            if (getter == null)
                return null;
            try { return getter(); }
            catch
            {
                InvocationFailures++;
                return null;
            }
        }

        private static Func<long>? Build(Type? type, string name)
        {
            try
            {
                MethodInfo? method = type?.GetMethod(name, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                return method == null || method.ReturnType != typeof(long)
                    ? null
                    : Expression.Lambda<Func<long>>(Expression.Call(method)).Compile();
            }
            catch
            {
                return null;
            }
        }

        private static int Count(params object?[] values)
        {
            int count = 0;
            for (int i = 0; i < values.Length; i++)
                if (values[i] != null) count++;
            return count;
        }
    }
}
