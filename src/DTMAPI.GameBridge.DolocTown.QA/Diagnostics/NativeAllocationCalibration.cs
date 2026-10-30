using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    // Explicit native-profiler QA process only. Never installs a profiler after Mono startup.
    internal static partial class NativeAllocationCalibration
    {
        private const string Library = "mono-profiler-dtmapi_alloc.dll";
        [StructLayout(LayoutKind.Sequential)]
        private struct Snapshot
        {
            internal long AllBytes, AllCount, ThreadBytes, ThreadCount;
            internal int ThreadId, Active, Status, Reserved;
        }
        private struct Row
        {
            internal long Bytes, Count, AllBytes;
            internal int ThreadId;
        }
        [DllImport("kernel32", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string name);
        [DllImport("kernel32", CharSet = CharSet.Unicode)] private static extern int GetModuleFileName(IntPtr module, StringBuilder path, int capacity);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int dtmapi_alloc_bind_thread(int lane);
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern int dtmapi_alloc_start();
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void dtmapi_alloc_stop();
        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void dtmapi_alloc_read(int lane, out Snapshot snapshot);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Allocate(int kind)
        {
            switch (kind)
            {
                case 1: GC.KeepAlive(new byte[4096]); break;
                case 2: GC.KeepAlive(new byte[65536]); break;
                case 3: GC.KeepAlive(new object()); break;
                case 4: GC.KeepAlive(new object[8]); break;
                case 5: GC.KeepAlive(new string('x', 512)); break;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Row Measure(int lane, int kind)
        {
            dtmapi_alloc_read(lane, out var before);
            Allocate(kind);
            dtmapi_alloc_read(lane, out var after);
            return new Row { Bytes = after.ThreadBytes - before.ThreadBytes, Count = after.ThreadCount - before.ThreadCount,
                AllBytes = after.AllBytes - before.AllBytes, ThreadId = after.ThreadId };
        }

        private static bool ValidTriplet(Row control, Row small, Row large) => control.Bytes == 0 && control.Count == 0 &&
            small.Count == 1 && large.Count == 1 && small.Bytes >= 4096 && small.Bytes <= 4224 &&
            large.Bytes - small.Bytes == 65536 - 4096;

        internal static bool Run(Action<string> report)
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT || IntPtr.Size != 8 || GetModuleHandle(Library) == IntPtr.Zero)
            { report("NativeAllocation unavailable: startup profiler not loaded (Windows x64 QA only)."); return false; }
            var path = new StringBuilder(1024);
            GetModuleFileName(GetModuleHandle(Library), path, path.Capacity);
            dtmapi_alloc_read(0, out var initial);
            report("NativeAllocation startup status=" + initial.Status + "; active=" + initial.Active + "; path=" + path +
                "; unity=" + Type.GetType("UnityEngine.Application, UnityEngine.CoreModule")?.GetProperty("unityVersion")?.GetValue(null, null) +
                "; mono=" + typeof(object).Assembly.GetType("Mono.Runtime")?.GetMethod("GetDisplayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.Invoke(null, null) +
                "; clr=" + Environment.Version + "; pointerBytes=" + IntPtr.Size);
            if (initial.Status != 2 || initial.Active != 0) { report("NativeAllocation unavailable: startup/idle prerequisite failed."); return false; }

            var rows = new Row[18];
            var workerRows = new Row[3];
            Row repeat = default, stopped = default;
            long readsBytes = -1, mainDuringWorker = -1, allDuringWorker = -1;
            int workerBound = 0;
            bool workerCompleted = false, reenabled = false;
            Exception? workerFailure = null;
            using (var ready = new ManualResetEvent(false))
            using (var release = new ManualResetEvent(false))
            using (var completed = new ManualResetEvent(false))
            {
                // Pre-JIT every measured allocation shape before installing the callback.
                // This also tests previously compiled call sites, not just fresh JIT code.
                for (int kind = 0; kind < 6; kind++) Measure(0, kind);
                ready.Set(); ready.WaitOne(0); ready.Reset();
                release.Set(); release.WaitOne(0); release.Reset();
                completed.Set(); completed.WaitOne(0); completed.Reset();
                var worker = new Thread(() =>
                {
                    try
                    {
                        workerBound = dtmapi_alloc_bind_thread(1);
                        for (int kind = 0; kind < 3; kind++) Measure(1, kind);
                        ready.Set();
                        if (!release.WaitOne(5000)) return;
                        for (int kind = 0; kind < 3; kind++) workerRows[kind] = Measure(1, kind);
                    }
                    catch (Exception ex) { workerFailure = ex; }
                    finally { completed.Set(); }
                }) { IsBackground = true, Name = "DTMAPI allocation calibration" };
                worker.Start();
                try
                {
                    if (!ready.WaitOne(5000) || workerBound != 1 || dtmapi_alloc_bind_thread(0) != 1)
                        throw new InvalidOperationException("Calibration thread binding failed.");
                    if (dtmapi_alloc_start() != 1) throw new InvalidOperationException("Startup profiler could not start.");
                    for (int round = 0; round < 3; round++)
                        for (int kind = 0; kind < 6; kind++) rows[round * 6 + kind] = Measure(0, kind);
                    dtmapi_alloc_read(0, out var readBefore);
                    for (int i = 0; i < 10000; i++) dtmapi_alloc_read(0, out _);
                    dtmapi_alloc_read(0, out var readAfter);
                    readsBytes = readAfter.ThreadBytes - readBefore.ThreadBytes;
                    dtmapi_alloc_read(0, out var beforeWorker);
                    release.Set();
                    workerCompleted = completed.WaitOne(5000);
                    dtmapi_alloc_read(0, out var afterWorker);
                    mainDuringWorker = afterWorker.ThreadBytes - beforeWorker.ThreadBytes;
                    allDuringWorker = afterWorker.AllBytes - beforeWorker.AllBytes;
                }
                finally
                {
                    dtmapi_alloc_stop();
                    release.Set();
                    // No Unity or game calls run on the worker; it only allocates and reads.
                    if (!worker.Join(5000)) throw new InvalidOperationException("Calibration worker did not exit.");
                }
            }
            stopped = Measure(0, 2);
            try
            {
                reenabled = dtmapi_alloc_start() == 1;
                if (reenabled) repeat = Measure(0, 1);
            }
            finally { dtmapi_alloc_stop(); }
            dtmapi_alloc_read(0, out var final);

            bool valid = readsBytes == 0 && workerCompleted && workerFailure == null &&
                ValidTriplet(workerRows[0], workerRows[1], workerRows[2]) && mainDuringWorker == 0 &&
                allDuringWorker >= workerRows[1].Bytes + workerRows[2].Bytes &&
                workerRows[1].ThreadId != rows[1].ThreadId &&
                stopped.Bytes == 0 && stopped.Count == 0 && reenabled && repeat.Count == 1 && repeat.Bytes == rows[1].Bytes && final.Active == 0;
            for (int round = 0; round < 3; round++)
            {
                valid &= ValidTriplet(rows[round * 6], rows[round * 6 + 1], rows[round * 6 + 2]);
                valid &= rows[round * 6 + 3].Count == 1 && rows[round * 6 + 3].Bytes == 16;
                valid &= rows[round * 6 + 4].Count == 1 && rows[round * 6 + 4].Bytes == 96;
                valid &= rows[round * 6 + 5].Count == 1 && rows[round * 6 + 5].Bytes >= 1046 && rows[round * 6 + 5].Bytes <= 1056;
                for (int kind = 0; kind < 6; kind++) WriteRow(report, "main", round, kind, rows[round * 6 + kind]);
            }
            for (int kind = 0; kind < 3; kind++) WriteRow(report, "worker", 0, kind, workerRows[kind]);
            report("NativeAllocation checks read10000Bytes=" + readsBytes + "; mainDuringWorker=" + mainDuringWorker +
                "; allDuringWorker=" + allDuringWorker + "; stoppedBytes=" + stopped.Bytes + "; restartBytes=" + repeat.Bytes +
                "; activeAfter=" + final.Active + "; workerCompleted=" + workerCompleted + "; workerFailure=" + workerFailure?.GetType().Name);
            report("NativeAllocation result=" + (valid ? "calibrated" : "unavailable") +
                "; scope=all-reported-Mono-managed-object-events plus selected-OS-thread; unit=mono_object_get_size bytes including object headers; nativeUnityMemory=False; sevenGroupsRun=False");
            return valid;
        }

        private static void WriteRow(Action<string> report, string lane, int round, int kind, Row row) =>
            report("NativeAllocation sample lane=" + lane + "; round=" + round + "; kind=" + kind + "; bytes=" + row.Bytes +
                "; count=" + row.Count + "; allBytes=" + row.AllBytes + "; osThread=" + row.ThreadId);
    }
}
