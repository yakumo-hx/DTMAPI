using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using DolocTown;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;

namespace DTMAPI.Internal.HeartMap
{
    // Explicit, bounded diagnostics. No diagnostic object exists during module idle.
    internal sealed class MapDiagnostics : MonoBehaviour
    {
        private const int Capacity = 120000;
        private MapModule? owner;
        private readonly double[] frames = new double[Capacity], ownCpu = new double[Capacity];
        private readonly long[] ownBytes = new long[Capacity], ownCalls = new long[Capacity];
        private readonly Point[] trajectory = new Point[3000];
        private readonly List<Counter> counters = new List<Counter>();
        private int count, repetition, repeats;
        private double started, previous, nextPoint;
        private bool measuring, recording, sampling;
        private string room = "", label = "", batch = "";
        private long allocationStart, heapStart, reflectionStart;
        private int gcStart;
        private TimeSpan cpuStart;
        private Process? process;
        private static double Now => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;

        internal void Begin(MapModule module, bool trace = false)
        {
            if (measuring || recording) { module.Log("diagnostics already active"); return; }
            owner = module; room = DolocAPI.CurrentRoom.RoomId; label = module.ControlLabel;
            measuring = !trace; recording = trace;
            repeats = trace ? 1 : Math.Max(1, Math.Min(3, module.Config.MeasurementRepeats));
            repetition = 1; batch = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            enabled = true;
            if (!trace && module.Panel != null) module.Panel.enabled = false;
            if (!trace)
            {
                module.Log("allocation counter " + MapMetrics.AllocationCounterStatus);
                OpenCounters(module);
            }
            StartWindow();
            module.Log(trace ? "trajectory started (240 seconds, 10 Hz; no input injection)" :
                "performance batch=" + batch + "; control=" + label + "; repeats=" + repeats +
                "; each warmup=60s measure=300s; room=" + room + "; stationary native physics; movement parameters unchanged" +
                "; resolution=" + Screen.width + "x" + Screen.height + "; targetFrameRate=" + Application.targetFrameRate +
                "; vSyncCount=" + QualitySettings.vSyncCount + "; qualityLevel=" + QualitySettings.GetQualityLevel());
            if (!trace) module.LogResources();
        }

        private void StartWindow()
        {
            count = 0; sampling = false; started = previous = Now; nextPoint = started;
            MapMetrics.Collect = false; MapMetrics.Reset();
            if (measuring) owner!.Log("performance warmup repetition=" + repetition + "/" + repeats);
        }

        private void OpenCounters(MapModule module)
        {
            var wanted = new HashSet<string>(StringComparer.Ordinal) {
                "Main Thread", "Render Thread", "Physics2D.Simulate", "Physics2D.FixedUpdate", "Physics2D.Update",
                "GC Allocated In Frame", "GC.Alloc", "Total Used Memory", "Gfx Used Memory", "Draw Calls Count", "Batches Count", "Triangles Count"
            };
            try
            {
                var available = new List<ProfilerRecorderHandle>();
                ProfilerRecorderHandle.GetAvailable(available);
                foreach (var handle in available)
                {
                    var description = ProfilerRecorderHandle.GetDescription(handle);
                    if (!wanted.Contains(description.Name)) continue;
                    var recorder = ProfilerRecorder.StartNew(description.Category, description.Name, 1);
                    if (!recorder.Valid) { recorder.Dispose(); continue; }
                    counters.Add(new Counter(description.Name, description.UnitType.ToString(), recorder));
                    module.Log("performance counter available=" + description.Name + "; unit=" + description.UnitType);
                    wanted.Remove(description.Name);
                }
            }
            catch (Exception ex) { module.Log("performance Unity counters unavailable: " + ex.GetType().Name + ": " + ex.Message); }
            module.Log("performance counters not available (not zero)=" + string.Join(",", wanted));
        }

        private void LateUpdate()
        {
            if (owner == null) { enabled = false; return; }
            if (!DolocAPI.IsDataLoaded || DolocAPI.CurrentRoom?.RoomId != room) { Stop("room-changed"); return; }
            double now = Now;
            if (recording)
            {
                if (now >= nextPoint && count < trajectory.Length)
                {
                    var pos = DolocAPI.AgentPosition; var status = DolocAPI.agent.Status;
                    trajectory[count++] = new Point { Time = now - started, X = pos.x, Y = pos.y, Vx = status.Velocity.x, Vy = status.Velocity.y, Grounded = status.IsGrounded };
                    nextPoint = now + .1;
                }
                if (now - started >= 240) Stop("complete");
                return;
            }
            if (!sampling && now - started >= 60)
            {
                process = Process.GetCurrentProcess(); cpuStart = process.TotalProcessorTime;
                heapStart = GC.GetTotalMemory(false); gcStart = GC.CollectionCount(0);
                reflectionStart = MapMetrics.ReflectionResolutions;
                sampling = true; previous = now;
                MapMetrics.Reset(); MapMetrics.Collect = true;
                allocationStart = MapMetrics.Allocated?.Invoke() ?? -1;
                return;
            }
            if (!sampling) return;
            if (count < Capacity)
            {
                frames[count] = (now - previous) * 1000;
                ownCpu[count] = MapMetrics.Ticks * 1000d / Stopwatch.Frequency;
                ownBytes[count] = MapMetrics.Bytes; ownCalls[count] = MapMetrics.Calls;
                foreach (var counter in counters) counter.Values[count] = counter.Recorder.Count > 0 ? counter.Recorder.LastValue : -1;
                count++;
            }
            MapMetrics.Reset(); previous = now;
            if (now - started >= 360) Stop("complete");
        }

        internal void Stop(string reason)
        {
            if (!measuring && !recording) return;
            MapMetrics.Collect = false;
            var module = owner!;
            bool repeat = false;
            try
            {
            double duration = Math.Max(0, Now - started - 60);
            long allocated = MapMetrics.Allocated == null || !sampling ? -1 : MapMetrics.Allocated() - allocationStart;
            long heap = GC.GetTotalMemory(false);
            int collections = sampling ? GC.CollectionCount(0) - gcStart : -1;
            double cpu = process == null ? 0 : (process.TotalProcessorTime - cpuStart).TotalMilliseconds;
            string prefix = batch + "-" + label + "-r" + repetition;
            Directory.CreateDirectory(module.Config.EvidenceDirectory);
            if (recording)
            {
                var csv = new StringBuilder("seconds,x,y,vx,vy,grounded\n");
                for (int i = 0; i < count; i++)
                {
                    var p = trajectory[i];
                    csv.AppendFormat(CultureInfo.InvariantCulture, "{0:F3},{1:F3},{2:F3},{3:F3},{4:F3},{5}\n", p.Time, p.X, p.Y, p.Vx, p.Vy, p.Grounded);
                }
                File.WriteAllText(Path.Combine(module.Config.EvidenceDirectory, prefix + "-trajectory.csv"), csv.ToString());
                module.Log("trajectory ended reason=" + reason + "; samples=" + count);
            }
            else
            {
                var raw = new StringBuilder("frame,intervalMs,mapCallbackMs,mapAllocatedBytes,mapCallbackCalls");
                foreach (var counter in counters) raw.Append(',').Append(counter.Name);
                raw.Append('\n');
                long totalBytes = 0, totalCalls = 0, allocatedFrames = 0;
                for (int i = 0; i < count; i++)
                {
                    raw.AppendFormat(CultureInfo.InvariantCulture, "{0},{1:F6},{2:F6},{3},{4}", i, frames[i], ownCpu[i], MapMetrics.Allocated == null ? -1 : ownBytes[i], ownCalls[i]);
                    foreach (var counter in counters) raw.Append(',').Append(counter.Values[i]);
                    raw.Append('\n');
                    totalBytes += ownBytes[i]; totalCalls += ownCalls[i];
                    if (ownBytes[i] != 0) allocatedFrames++;
                }
                File.WriteAllText(Path.Combine(module.Config.EvidenceDirectory, prefix + "-frames.csv"), raw.ToString());
                Array.Sort(frames, 0, count); Array.Sort(ownCpu, 0, count);
                var report = new StringBuilder();
                report.AppendFormat(CultureInfo.InvariantCulture,
                    "reason={0}\ncontrol={1}\nroom={2}\nrepetition={3}\nwarmupSeconds=60\nmeasurementSeconds={4:F3}\nframes={5}\ncapacityReached={6}\n" +
                    "frameIntervalP95Ms={7:F6}\nframeIntervalP99Ms={8:F6}\nprocessCpuMs={9:F3}\nprocessOneCorePercent={10:F3}\n" +
                    "mainThreadAllocatedBytes={11}\ngen0Collections={12}\nheapStart={13}\nheapEnd={14}\n",
                    reason, label, room, repetition, duration, count, count == Capacity, Percentile(frames, count, .95), Percentile(frames, count, .99),
                    cpu, duration == 0 ? 0 : cpu / duration / 10, allocated, collections, heapStart, heap);
                report.AppendFormat(CultureInfo.InvariantCulture,
                    "mapCallbacksP95Ms={0:F6}\nmapCallbacksP99Ms={1:F6}\nmapCallbacksMaxMs={2:F6}\nmapCallbackCalls={3}\n" +
                    "mapCallbackAllocatedBytes={4}\nmapCallbackAllocationFrames={5}\nmapReflectionResolutionsDuringSample={6}\nmapDedicatedFrameSubscriptions=0\n",
                    Percentile(ownCpu, count, .95), Percentile(ownCpu, count, .99), Percentile(ownCpu, count, 1), totalCalls,
                    MapMetrics.Allocated == null ? -1 : totalBytes, MapMetrics.Allocated == null ? -1 : allocatedFrames,
                    sampling ? MapMetrics.ReflectionResolutions - reflectionStart : -1);
                report.AppendLine("mapCallbackScope=all owned Harmony callbacks and event handlers; Stopwatch/GC instrumentation included; native callee cost included on event frames");
                report.AppendLine("allocationCounterStatus=" + MapMetrics.AllocationCounterStatus);
                report.AppendLine("diagnosticScope=bounded LateUpdate and Unity recorders; panel disabled; raw frame/process totals include native rendering, physics, Runtime, required equipment mod and diagnostics");
                report.AppendLine("nativeControlScope=same Runtime, required equipment mod, NoNativeSave and paused-calendar harness; map adapter and content absent; not an uninstrumented vanilla player");
                foreach (var counter in counters)
                {
                    int valid = 0;
                    for (int i = 0; i < count; i++) if (counter.Values[i] >= 0) counter.Values[valid++] = counter.Values[i];
                    Array.Sort(counter.Values, 0, valid);
                    report.Append(counter.Name).Append(".unit=").AppendLine(counter.Unit);
                    report.Append(counter.Name).Append(".samples=").AppendLine(valid.ToString(CultureInfo.InvariantCulture));
                    report.Append(counter.Name).Append(".p95=").AppendLine(Percentile(counter.Values, valid, .95).ToString(CultureInfo.InvariantCulture));
                    report.Append(counter.Name).Append(".p99=").AppendLine(Percentile(counter.Values, valid, .99).ToString(CultureInfo.InvariantCulture));
                }
                File.WriteAllText(Path.Combine(module.Config.EvidenceDirectory, prefix + "-performance.txt"), report.ToString());
                module.Log("performance ended reason=" + reason + "; control=" + label + "; repetition=" + repetition + "; seconds=" + duration.ToString("F3") +
                    "; frames=" + count + "; p95FrameMs=" + Percentile(frames, count, .95).ToString("F3") + "; mapP99Ms=" + Percentile(ownCpu, count, .99).ToString("F6") +
                    "; mapAllocated=" + (MapMetrics.Allocated == null ? -1 : totalBytes) + "; totalCpuMs=" + cpu.ToString("F3") + "; mainThreadAllocated=" + allocated);
            }
                repeat = measuring && reason == "complete" && repetition < repeats;
            }
            catch (Exception ex) { module.Log("diagnostic output failed; resources still released; " + ex.Message); }
            finally
            {
                try { process?.Dispose(); } catch (Exception ex) { module.Log("process disposal failed: " + ex.Message); } finally { process = null; }
                if (!repeat)
                {
                    foreach (var counter in counters) { try { counter.Recorder.Dispose(); } catch (Exception ex) { module.Log("recorder disposal failed: " + ex.Message); } }
                    counters.Clear();
                    measuring = recording = sampling = false; owner = null; enabled = false;
                    if (module.Panel != null) module.Panel.enabled = true;
                }
            }
            if (repeat) { repetition++; StartWindow(); }
            else module.Log("diagnostic batch ended reason=" + reason + "; control=" + label);
        }

        private static double Percentile(double[] values, int length, double q) => length == 0 ? 0 : values[Math.Min(length - 1, (int)(length * q))];
        private static long Percentile(long[] values, int length, double q) => length == 0 ? -1 : values[Math.Min(length - 1, (int)(length * q))];
        private sealed class Counter
        {
            internal readonly string Name, Unit;
            internal ProfilerRecorder Recorder;
            internal readonly long[] Values = new long[Capacity];
            internal Counter(string name, string unit, ProfilerRecorder recorder) { Name = name; Unit = unit; Recorder = recorder; }
        }
        private struct Point { internal double Time; internal float X, Y, Vx, Vy; internal bool Grounded; }
    }
}
