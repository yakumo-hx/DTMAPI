using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal readonly struct RuntimePlatformMemorySnapshot
    {
        internal RuntimePlatformMemorySnapshot(long? monoUsed, long? monoHeap, long? unityAllocated, long? unityReserved, long? unityUnusedReserved)
        {
            MonoUsedBytes = monoUsed;
            MonoHeapBytes = monoHeap;
            UnityAllocatedBytes = unityAllocated;
            UnityReservedBytes = unityReserved;
            UnityUnusedReservedBytes = unityUnusedReserved;
        }

        internal long? MonoUsedBytes { get; }
        internal long? MonoHeapBytes { get; }
        internal long? UnityAllocatedBytes { get; }
        internal long? UnityReservedBytes { get; }
        internal long? UnityUnusedReservedBytes { get; }
    }

    internal readonly struct RuntimeMemoryDomainSnapshot
    {
        internal RuntimeMemoryDomainSnapshot(
            long recordCount,
            long ownerRootCount,
            long accessorCount,
            long reelCount,
            long transientCount,
            long? inputOwnerCount = null,
            long? inputButtonCount = null,
            long? inputOwnerRegistrations = null,
            long? eventActiveHandlers = null,
            long? eventDispatchableHandlers = null,
            long? eventQuarantinedHandlers = null,
            long? apiRootCount = null,
            long? resourceRecordCount = null,
            long? resourceSnapshotBuilds = null,
            long? hookStatusCount = null,
            long? demandEntryCount = null,
            long? totalDemand = null)
        {
            RecordCount = recordCount;
            OwnerRootCount = ownerRootCount;
            AccessorCount = accessorCount;
            ReelCount = reelCount;
            TransientCount = transientCount;
            InputOwnerCount = inputOwnerCount;
            InputButtonCount = inputButtonCount;
            InputOwnerRegistrations = inputOwnerRegistrations;
            EventActiveHandlers = eventActiveHandlers;
            EventDispatchableHandlers = eventDispatchableHandlers;
            EventQuarantinedHandlers = eventQuarantinedHandlers;
            ApiRootCount = apiRootCount;
            ResourceRecordCount = resourceRecordCount;
            ResourceSnapshotBuilds = resourceSnapshotBuilds;
            HookStatusCount = hookStatusCount;
            DemandEntryCount = demandEntryCount;
            TotalDemand = totalDemand;
        }

        internal long RecordCount { get; }
        internal long OwnerRootCount { get; }
        internal long AccessorCount { get; }
        internal long ReelCount { get; }
        internal long TransientCount { get; }
        internal long? InputOwnerCount { get; }
        internal long? InputButtonCount { get; }
        internal long? InputOwnerRegistrations { get; }
        internal long? EventActiveHandlers { get; }
        internal long? EventDispatchableHandlers { get; }
        internal long? EventQuarantinedHandlers { get; }
        internal long? ApiRootCount { get; }
        internal long? ResourceRecordCount { get; }
        internal long? ResourceSnapshotBuilds { get; }
        internal long? HookStatusCount { get; }
        internal long? DemandEntryCount { get; }
        internal long? TotalDemand { get; }
    }

    internal sealed class RuntimeMemoryTrendProbe
    {
        private const int DefaultMaxSamples = 64;
        private readonly Func<RuntimePlatformMemorySnapshot>? capturePlatform;
        private readonly Func<RuntimeMemoryDomainSnapshot>? captureDomain;
        private readonly Func<RuntimeProcessMemorySnapshot> captureProcess;
        private readonly TimeSpan sampleInterval;
        private readonly int maxSamples;
        private DateTimeOffset nextSampleAtUtc;
        private bool started;
        private bool completed;

        internal RuntimeMemoryTrendProbe(
            TimeSpan? sampleInterval = null,
            Func<RuntimePlatformMemorySnapshot>? capturePlatform = null,
            Func<RuntimeMemoryDomainSnapshot>? captureDomain = null,
            Func<RuntimeProcessMemorySnapshot>? captureProcess = null,
            int maxSamples = DefaultMaxSamples)
        {
            this.sampleInterval = sampleInterval.GetValueOrDefault(TimeSpan.FromSeconds(30));
            if (this.sampleInterval <= TimeSpan.Zero)
                this.sampleInterval = TimeSpan.FromSeconds(30);
            this.capturePlatform = capturePlatform;
            this.captureDomain = captureDomain;
            this.captureProcess = captureProcess ?? CaptureProcess;
            this.maxSamples = Math.Max(2, maxSamples);
            Result = new RuntimeMemoryTrendResult { SampleIntervalSeconds = this.sampleInterval.TotalSeconds, Status = "pending" };
        }

        internal RuntimeMemoryTrendResult Result { get; }

        internal void Start(DateTimeOffset nowUtc)
        {
            if (started)
                return;
            started = true;
            Result.Status = "measuring";
            Result.StartedAtUtc = nowUtc;
            Capture(nowUtc);
            nextSampleAtUtc = nowUtc + sampleInterval;
        }

        internal bool Observe(DateTimeOffset nowUtc)
        {
            if (!started || completed || nowUtc < nextSampleAtUtc)
                return false;
            Capture(nowUtc);
            nextSampleAtUtc = nowUtc + sampleInterval;
            return true;
        }

        internal void Complete(DateTimeOffset nowUtc)
        {
            if (completed)
                return;
            if (!started)
                Start(nowUtc);
            if (Result.Samples.Count == 0 || Result.Samples[Result.Samples.Count - 1].TimestampUtc != nowUtc)
                Capture(nowUtc);
            completed = true;
            Result.Status = "completed";
            Result.CompletedAtUtc = nowUtc;
            BuildTrends();
        }

        private void Capture(DateTimeOffset nowUtc)
        {
            RuntimeProcessMemorySnapshot process;
            try
            {
                process = captureProcess();
            }
            catch (Exception ex)
            {
                Result.ProcessCaptureFailures++;
                Result.LastError = ex.GetType().Name + ": " + ex.Message;
                process = new RuntimeProcessMemorySnapshot(null, null, GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));
            }

            RuntimePlatformMemorySnapshot platform = default;
            if (capturePlatform != null)
            {
                try { platform = capturePlatform(); }
                catch (Exception ex)
                {
                    Result.PlatformCaptureFailures++;
                    Result.LastError = ex.GetType().Name + ": " + ex.Message;
                }
            }

            RuntimeMemoryDomainSnapshot domain = default;
            if (captureDomain != null)
            {
                try { domain = captureDomain(); }
                catch (Exception ex)
                {
                    Result.DomainCaptureFailures++;
                    Result.LastError = ex.GetType().Name + ": " + ex.Message;
                }
            }

            var sample = new RuntimeMemoryTrendSample
            {
                TimestampUtc = nowUtc,
                ElapsedSeconds = Math.Max(0d, (nowUtc - Result.StartedAtUtc).TotalSeconds),
                MonoUsedBytes = platform.MonoUsedBytes,
                MonoHeapBytes = platform.MonoHeapBytes,
                UnityAllocatedBytes = platform.UnityAllocatedBytes,
                UnityReservedBytes = platform.UnityReservedBytes,
                UnityUnusedReservedBytes = platform.UnityUnusedReservedBytes,
                ProcessPrivateBytes = process.PrivateBytes,
                ProcessWorkingSetBytes = process.WorkingSetBytes,
                ProcessGen0Collections = process.Gen0Collections,
                ProcessGen1Collections = process.Gen1Collections,
                ProcessGen2Collections = process.Gen2Collections,
                DtmApiRecordCount = domain.RecordCount,
                OwnerRootCount = domain.OwnerRootCount,
                AccessorCount = domain.AccessorCount,
                ReelCount = domain.ReelCount,
                NativeTransientCount = domain.TransientCount,
                InputOwnerCount = domain.InputOwnerCount,
                InputButtonCount = domain.InputButtonCount,
                InputOwnerRegistrations = domain.InputOwnerRegistrations,
                EventActiveHandlers = domain.EventActiveHandlers,
                EventDispatchableHandlers = domain.EventDispatchableHandlers,
                EventQuarantinedHandlers = domain.EventQuarantinedHandlers,
                ApiRootCount = domain.ApiRootCount,
                ResourceRecordCount = domain.ResourceRecordCount,
                ResourceSnapshotBuilds = domain.ResourceSnapshotBuilds,
                HookStatusCount = domain.HookStatusCount,
                DemandEntryCount = domain.DemandEntryCount,
                TotalDemand = domain.TotalDemand
            };
            if (Result.Samples.Count == maxSamples)
            {
                Result.Samples.RemoveAt(1);
                Result.TrimmedSamples++;
            }
            Result.Samples.Add(sample);
        }

        private void BuildTrends()
        {
            ApplyTrends(Result.Samples, Result);
            double endSeconds = Result.Samples.Count == 0 ? 0d : Result.Samples[Result.Samples.Count - 1].ElapsedSeconds;
            Result.StartupWindow = BuildWindow(0d, Math.Min(180d, endSeconds));
            Result.StableWindow = BuildWindow(180d, endSeconds);
            Result.TrailingTenMinutesWindow = BuildWindow(Math.Max(0d, endSeconds - 600d), endSeconds);
            Result.Gen2MonoUsedLowWaters = BuildGen2MonoUsedLowWaters();
        }

        private void ApplyTrends(IReadOnlyList<RuntimeMemoryTrendSample> samples, IRuntimeMemoryTrendTarget target)
        {
            target.MonoUsed = BuildNullableTrend(samples, s => s.MonoUsedBytes);
            target.MonoHeap = BuildNullableTrend(samples, s => s.MonoHeapBytes);
            target.UnityAllocated = BuildNullableTrend(samples, s => s.UnityAllocatedBytes);
            target.UnityReserved = BuildNullableTrend(samples, s => s.UnityReservedBytes);
            target.UnityUnusedReserved = BuildNullableTrend(samples, s => s.UnityUnusedReservedBytes);
            target.ProcessPrivate = BuildNullableTrend(samples, s => s.ProcessPrivateBytes);
            target.ProcessWorkingSet = BuildNullableTrend(samples, s => s.ProcessWorkingSetBytes);
            target.ProcessGen0Collections = BuildTrend(samples, s => s.ProcessGen0Collections);
            target.ProcessGen1Collections = BuildTrend(samples, s => s.ProcessGen1Collections);
            target.ProcessGen2Collections = BuildTrend(samples, s => s.ProcessGen2Collections);
            target.DtmApiRecordCount = BuildTrend(samples, s => s.DtmApiRecordCount);
            target.OwnerRootCount = BuildTrend(samples, s => s.OwnerRootCount);
            target.AccessorCount = BuildTrend(samples, s => s.AccessorCount);
            target.ReelCount = BuildTrend(samples, s => s.ReelCount);
            target.NativeTransientCount = BuildTrend(samples, s => s.NativeTransientCount);
            target.InputOwnerCount = BuildNullableTrend(samples, s => s.InputOwnerCount);
            target.InputButtonCount = BuildNullableTrend(samples, s => s.InputButtonCount);
            target.InputOwnerRegistrations = BuildNullableTrend(samples, s => s.InputOwnerRegistrations);
            target.EventActiveHandlers = BuildNullableTrend(samples, s => s.EventActiveHandlers);
            target.EventDispatchableHandlers = BuildNullableTrend(samples, s => s.EventDispatchableHandlers);
            target.EventQuarantinedHandlers = BuildNullableTrend(samples, s => s.EventQuarantinedHandlers);
            target.ApiRootCount = BuildNullableTrend(samples, s => s.ApiRootCount);
            target.ResourceRecordCount = BuildNullableTrend(samples, s => s.ResourceRecordCount);
            target.ResourceSnapshotBuilds = BuildNullableTrend(samples, s => s.ResourceSnapshotBuilds);
            target.HookStatusCount = BuildNullableTrend(samples, s => s.HookStatusCount);
            target.DemandEntryCount = BuildNullableTrend(samples, s => s.DemandEntryCount);
            target.TotalDemand = BuildNullableTrend(samples, s => s.TotalDemand);
        }

        private RuntimeMemoryTrendWindow BuildWindow(double requestedStartSeconds, double requestedEndSeconds)
        {
            var window = new RuntimeMemoryTrendWindow
            {
                RequestedStartSeconds = requestedStartSeconds,
                RequestedEndSeconds = requestedEndSeconds
            };
            if (requestedEndSeconds < requestedStartSeconds || Result.Samples.Count == 0)
                return window;
            double tolerance = Math.Min(1d, sampleInterval.TotalSeconds / 10d);
            var selected = new List<RuntimeMemoryTrendSample>();
            foreach (RuntimeMemoryTrendSample sample in Result.Samples)
            {
                if (sample.ElapsedSeconds + tolerance < requestedStartSeconds || sample.ElapsedSeconds - tolerance > requestedEndSeconds)
                    continue;
                selected.Add(sample);
            }
            window.SampleCount = selected.Count;
            if (selected.Count > 0)
            {
                window.ActualStartSeconds = selected[0].ElapsedSeconds;
                window.ActualEndSeconds = selected[selected.Count - 1].ElapsedSeconds;
                ApplyTrends(selected, window);
            }
            return window;
        }

        private List<RuntimeGen2MonoLowWaterObservation> BuildGen2MonoUsedLowWaters()
        {
            var observations = new List<RuntimeGen2MonoLowWaterObservation>();
            if (Result.Samples.Count < 2)
                return observations;
            long previousGen2 = Result.Samples[0].ProcessGen2Collections;
            RuntimeGen2MonoLowWaterObservation? current = null;
            for (int i = 1; i < Result.Samples.Count; i++)
            {
                RuntimeMemoryTrendSample sample = Result.Samples[i];
                if (sample.ProcessGen2Collections > previousGen2)
                {
                    if (current != null)
                    {
                        current.ClosedAtSeconds = Result.Samples[i - 1].ElapsedSeconds;
                        current.ClosedByNextGen2 = true;
                    }
                    current = new RuntimeGen2MonoLowWaterObservation
                    {
                        Gen2CountBefore = previousGen2,
                        Gen2CountAfter = sample.ProcessGen2Collections,
                        Gen2CollectionsObserved = sample.ProcessGen2Collections - previousGen2,
                        DetectedAtSeconds = sample.ElapsedSeconds,
                        MonoUsedAtDetection = sample.MonoUsedBytes,
                        MonoUsedLowWaterBytes = sample.MonoUsedBytes,
                        MonoUsedLowWaterAtSeconds = sample.MonoUsedBytes.HasValue ? sample.ElapsedSeconds : (double?)null
                    };
                    observations.Add(current);
                }
                else if (current != null && sample.MonoUsedBytes.HasValue &&
                    (!current.MonoUsedLowWaterBytes.HasValue || sample.MonoUsedBytes.Value < current.MonoUsedLowWaterBytes.Value))
                {
                    current.MonoUsedLowWaterBytes = sample.MonoUsedBytes;
                    current.MonoUsedLowWaterAtSeconds = sample.ElapsedSeconds;
                }
                previousGen2 = sample.ProcessGen2Collections;
            }
            if (current != null)
            {
                current.ClosedAtSeconds = Result.Samples[Result.Samples.Count - 1].ElapsedSeconds;
                current.CompletedAtMeasurementEnd = true;
            }
            return observations;
        }

        private RuntimeMemoryMetricTrend BuildTrend(IReadOnlyList<RuntimeMemoryTrendSample> samples, Func<RuntimeMemoryTrendSample, long> selector)
            => BuildNullableTrend(samples, sample => selector(sample));

        private RuntimeMemoryMetricTrend BuildNullableTrend(IReadOnlyList<RuntimeMemoryTrendSample> samples, Func<RuntimeMemoryTrendSample, long?> selector)
        {
            long? start = null;
            long? end = null;
            long? max = null;
            DateTimeOffset startAt = default;
            DateTimeOffset endAt = default;
            int regressionCount = 0;
            double sumX = 0d;
            double sumY = 0d;
            double sumXX = 0d;
            double sumXY = 0d;
            double regressionOriginMinutes = samples.Count == 0 ? 0d : samples[0].ElapsedSeconds / 60d;
            foreach (RuntimeMemoryTrendSample sample in samples)
            {
                long? value = selector(sample);
                if (!value.HasValue)
                    continue;
                if (!start.HasValue)
                {
                    start = value;
                    startAt = sample.TimestampUtc;
                }
                end = value;
                endAt = sample.TimestampUtc;
                max = !max.HasValue || value.Value > max.Value ? value : max;
                double x = sample.ElapsedSeconds / 60d - regressionOriginMinutes;
                double y = value.Value;
                regressionCount++;
                sumX += x;
                sumY += y;
                sumXX += x * x;
                sumXY += x * y;
            }
            double minutes = start.HasValue && end.HasValue ? Math.Max(0d, (endAt - startAt).TotalMinutes) : 0d;
            double denominator = regressionCount * sumXX - sumX * sumX;
            return new RuntimeMemoryMetricTrend
            {
                Start = start,
                End = end,
                Max = max,
                Delta = start.HasValue && end.HasValue ? end.Value - start.Value : (long?)null,
                TrendPerMinute = start.HasValue && end.HasValue && minutes > 0d ? (end.Value - start.Value) / minutes : (double?)null,
                SlopePerMinute = regressionCount >= 2 && Math.Abs(denominator) > 0.0000001d
                    ? (regressionCount * sumXY - sumX * sumY) / denominator
                    : (double?)null
            };
        }

        private static RuntimeProcessMemorySnapshot CaptureProcess()
        {
            using (Process process = Process.GetCurrentProcess())
            {
                process.Refresh();
                long privateBytes = process.PrivateMemorySize64;
                long workingSetBytes = process.WorkingSet64;
                if ((privateBytes <= 0 || workingSetBytes <= 0) && TryCaptureWindowsProcessMemory(process.Handle, out long nativePrivate, out long nativeWorkingSet))
                {
                    if (privateBytes <= 0)
                        privateBytes = nativePrivate;
                    if (workingSetBytes <= 0)
                        workingSetBytes = nativeWorkingSet;
                }
                return new RuntimeProcessMemorySnapshot(
                    privateBytes > 0 ? privateBytes : (long?)null,
                    workingSetBytes > 0 ? workingSetBytes : (long?)null,
                    GC.CollectionCount(0),
                    GC.CollectionCount(1),
                    GC.CollectionCount(2));
            }
        }

        internal static bool TryCaptureWindowsProcessMemory(IntPtr processHandle, out long privateBytes, out long workingSetBytes)
        {
            privateBytes = 0;
            workingSetBytes = 0;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;
            try
            {
                var counters = new ProcessMemoryCountersEx { Size = (uint)Marshal.SizeOf(typeof(ProcessMemoryCountersEx)) };
                if (!GetProcessMemoryInfo(processHandle, ref counters, counters.Size))
                    return false;
                privateBytes = checked((long)counters.PrivateUsage.ToUInt64());
                workingSetBytes = checked((long)counters.WorkingSetSize.ToUInt64());
                return privateBytes > 0 || workingSetBytes > 0;
            }
            catch
            {
                return false;
            }
        }

        [DllImport("psapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetProcessMemoryInfo(IntPtr process, ref ProcessMemoryCountersEx counters, uint size);

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessMemoryCountersEx
        {
            internal uint Size;
            internal uint PageFaultCount;
            internal UIntPtr PeakWorkingSetSize;
            internal UIntPtr WorkingSetSize;
            internal UIntPtr QuotaPeakPagedPoolUsage;
            internal UIntPtr QuotaPagedPoolUsage;
            internal UIntPtr QuotaPeakNonPagedPoolUsage;
            internal UIntPtr QuotaNonPagedPoolUsage;
            internal UIntPtr PagefileUsage;
            internal UIntPtr PeakPagefileUsage;
            internal UIntPtr PrivateUsage;
        }
    }

    internal readonly struct RuntimeProcessMemorySnapshot
    {
        internal RuntimeProcessMemorySnapshot(long? privateBytes, long? workingSetBytes, int gen0Collections, int gen1Collections, int gen2Collections)
        {
            PrivateBytes = privateBytes;
            WorkingSetBytes = workingSetBytes;
            Gen0Collections = gen0Collections;
            Gen1Collections = gen1Collections;
            Gen2Collections = gen2Collections;
        }
        internal long? PrivateBytes { get; }
        internal long? WorkingSetBytes { get; }
        internal int Gen0Collections { get; }
        internal int Gen1Collections { get; }
        internal int Gen2Collections { get; }
    }

    internal interface IRuntimeMemoryTrendTarget
    {
        RuntimeMemoryMetricTrend MonoUsed { get; set; }
        RuntimeMemoryMetricTrend MonoHeap { get; set; }
        RuntimeMemoryMetricTrend UnityAllocated { get; set; }
        RuntimeMemoryMetricTrend UnityReserved { get; set; }
        RuntimeMemoryMetricTrend UnityUnusedReserved { get; set; }
        RuntimeMemoryMetricTrend ProcessPrivate { get; set; }
        RuntimeMemoryMetricTrend ProcessWorkingSet { get; set; }
        RuntimeMemoryMetricTrend ProcessGen0Collections { get; set; }
        RuntimeMemoryMetricTrend ProcessGen1Collections { get; set; }
        RuntimeMemoryMetricTrend ProcessGen2Collections { get; set; }
        RuntimeMemoryMetricTrend DtmApiRecordCount { get; set; }
        RuntimeMemoryMetricTrend OwnerRootCount { get; set; }
        RuntimeMemoryMetricTrend AccessorCount { get; set; }
        RuntimeMemoryMetricTrend ReelCount { get; set; }
        RuntimeMemoryMetricTrend NativeTransientCount { get; set; }
        RuntimeMemoryMetricTrend InputOwnerCount { get; set; }
        RuntimeMemoryMetricTrend InputButtonCount { get; set; }
        RuntimeMemoryMetricTrend InputOwnerRegistrations { get; set; }
        RuntimeMemoryMetricTrend EventActiveHandlers { get; set; }
        RuntimeMemoryMetricTrend EventDispatchableHandlers { get; set; }
        RuntimeMemoryMetricTrend EventQuarantinedHandlers { get; set; }
        RuntimeMemoryMetricTrend ApiRootCount { get; set; }
        RuntimeMemoryMetricTrend ResourceRecordCount { get; set; }
        RuntimeMemoryMetricTrend ResourceSnapshotBuilds { get; set; }
        RuntimeMemoryMetricTrend HookStatusCount { get; set; }
        RuntimeMemoryMetricTrend DemandEntryCount { get; set; }
        RuntimeMemoryMetricTrend TotalDemand { get; set; }
    }

    [DataContract]
    internal sealed class RuntimeMemoryTrendResult : IRuntimeMemoryTrendTarget
    {
        [DataMember] public string Status { get; set; } = string.Empty;
        [DataMember] public DateTimeOffset StartedAtUtc { get; set; }
        [DataMember] public DateTimeOffset CompletedAtUtc { get; set; }
        [DataMember] public double SampleIntervalSeconds { get; set; }
        [DataMember] public int TrimmedSamples { get; set; }
        [DataMember] public int PlatformCaptureFailures { get; set; }
        [DataMember] public int ProcessCaptureFailures { get; set; }
        [DataMember] public int DomainCaptureFailures { get; set; }
        [DataMember] public string LastError { get; set; } = string.Empty;
        [DataMember] public List<RuntimeMemoryTrendSample> Samples { get; set; } = new List<RuntimeMemoryTrendSample>();
        [DataMember] public RuntimeMemoryMetricTrend MonoUsed { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend MonoHeap { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend UnityAllocated { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend UnityReserved { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend UnityUnusedReserved { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ProcessPrivate { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ProcessWorkingSet { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ProcessGen0Collections { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ProcessGen1Collections { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ProcessGen2Collections { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend DtmApiRecordCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend OwnerRootCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend AccessorCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ReelCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend NativeTransientCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend InputOwnerCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend InputButtonCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend InputOwnerRegistrations { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend EventActiveHandlers { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend EventDispatchableHandlers { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend EventQuarantinedHandlers { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ApiRootCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ResourceRecordCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ResourceSnapshotBuilds { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend HookStatusCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend DemandEntryCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend TotalDemand { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryTrendWindow StartupWindow { get; set; } = new RuntimeMemoryTrendWindow();
        [DataMember] public RuntimeMemoryTrendWindow StableWindow { get; set; } = new RuntimeMemoryTrendWindow();
        [DataMember] public RuntimeMemoryTrendWindow TrailingTenMinutesWindow { get; set; } = new RuntimeMemoryTrendWindow();
        [DataMember] public List<RuntimeGen2MonoLowWaterObservation> Gen2MonoUsedLowWaters { get; set; } = new List<RuntimeGen2MonoLowWaterObservation>();
    }

    [DataContract]
    internal sealed class RuntimeMemoryTrendWindow : IRuntimeMemoryTrendTarget
    {
        [DataMember] public double RequestedStartSeconds { get; set; }
        [DataMember] public double RequestedEndSeconds { get; set; }
        [DataMember] public double ActualStartSeconds { get; set; }
        [DataMember] public double ActualEndSeconds { get; set; }
        [DataMember] public int SampleCount { get; set; }
        [DataMember] public RuntimeMemoryMetricTrend MonoUsed { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend MonoHeap { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend UnityAllocated { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend UnityReserved { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend UnityUnusedReserved { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ProcessPrivate { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ProcessWorkingSet { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ProcessGen0Collections { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ProcessGen1Collections { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ProcessGen2Collections { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend DtmApiRecordCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend OwnerRootCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend AccessorCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ReelCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend NativeTransientCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend InputOwnerCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend InputButtonCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend InputOwnerRegistrations { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend EventActiveHandlers { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend EventDispatchableHandlers { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend EventQuarantinedHandlers { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ApiRootCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ResourceRecordCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend ResourceSnapshotBuilds { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend HookStatusCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend DemandEntryCount { get; set; } = new RuntimeMemoryMetricTrend();
        [DataMember] public RuntimeMemoryMetricTrend TotalDemand { get; set; } = new RuntimeMemoryMetricTrend();
    }

    [DataContract]
    internal sealed class RuntimeGen2MonoLowWaterObservation
    {
        [DataMember] public long Gen2CountBefore { get; set; }
        [DataMember] public long Gen2CountAfter { get; set; }
        [DataMember] public long Gen2CollectionsObserved { get; set; }
        [DataMember] public double DetectedAtSeconds { get; set; }
        [DataMember] public double ClosedAtSeconds { get; set; }
        [DataMember] public bool ClosedByNextGen2 { get; set; }
        [DataMember] public bool CompletedAtMeasurementEnd { get; set; }
        [DataMember] public long? MonoUsedAtDetection { get; set; }
        [DataMember] public long? MonoUsedLowWaterBytes { get; set; }
        [DataMember] public double? MonoUsedLowWaterAtSeconds { get; set; }
    }

    [DataContract]
    internal sealed class RuntimeMemoryTrendSample
    {
        [DataMember] public DateTimeOffset TimestampUtc { get; set; }
        [DataMember] public double ElapsedSeconds { get; set; }
        [DataMember] public long? MonoUsedBytes { get; set; }
        [DataMember] public long? MonoHeapBytes { get; set; }
        [DataMember] public long? UnityAllocatedBytes { get; set; }
        [DataMember] public long? UnityReservedBytes { get; set; }
        [DataMember] public long? UnityUnusedReservedBytes { get; set; }
        [DataMember] public long? ProcessPrivateBytes { get; set; }
        [DataMember] public long? ProcessWorkingSetBytes { get; set; }
        [DataMember] public long ProcessGen0Collections { get; set; }
        [DataMember] public long ProcessGen1Collections { get; set; }
        [DataMember] public long ProcessGen2Collections { get; set; }
        [DataMember] public long DtmApiRecordCount { get; set; }
        [DataMember] public long OwnerRootCount { get; set; }
        [DataMember] public long AccessorCount { get; set; }
        [DataMember] public long ReelCount { get; set; }
        [DataMember] public long NativeTransientCount { get; set; }
        [DataMember] public long? InputOwnerCount { get; set; }
        [DataMember] public long? InputButtonCount { get; set; }
        [DataMember] public long? InputOwnerRegistrations { get; set; }
        [DataMember] public long? EventActiveHandlers { get; set; }
        [DataMember] public long? EventDispatchableHandlers { get; set; }
        [DataMember] public long? EventQuarantinedHandlers { get; set; }
        [DataMember] public long? ApiRootCount { get; set; }
        [DataMember] public long? ResourceRecordCount { get; set; }
        [DataMember] public long? ResourceSnapshotBuilds { get; set; }
        [DataMember] public long? HookStatusCount { get; set; }
        [DataMember] public long? DemandEntryCount { get; set; }
        [DataMember] public long? TotalDemand { get; set; }
    }

    [DataContract]
    internal sealed class RuntimeMemoryMetricTrend
    {
        [DataMember] public long? Start { get; set; }
        [DataMember] public long? End { get; set; }
        [DataMember] public long? Max { get; set; }
        [DataMember] public long? Delta { get; set; }
        [DataMember] public double? TrendPerMinute { get; set; }
        [DataMember] public double? SlopePerMinute { get; set; }
    }
}
