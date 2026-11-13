using System;
using System.Diagnostics;

namespace DTMAPI.Modules.Monsters
{
    // Internal QA protocol only. The reader fills a reused buffer, never native types.
    // 0/1 main bytes/events, 2/3 all bytes/events, 4 bound OS thread, 5 caller OS
    // thread, 6 active, 7 profiler status. All-thread fields are auxiliary snapshots.
    internal sealed class AllocationMeasurement
    {
        internal readonly struct Snapshot
        {
            internal readonly long Bytes, Events, AllBytes, AllEvents, Thread;
            internal Snapshot(long[] values)
            { Bytes = values[0]; Events = values[1]; AllBytes = values[2]; AllEvents = values[3]; Thread = values[4]; }
        }
        internal readonly struct Token
        {
            internal readonly AllocationMeasurement? Owner;
            internal readonly Snapshot Before;
            internal readonly long Ticks;
            internal bool Active => Owner != null;
            internal Token(AllocationMeasurement owner, Snapshot before, long ticks)
            { Owner = owner; Before = before; Ticks = ticks; }
        }

        private readonly Action<long[]> reader;
        private readonly Func<long> clock;
        private readonly long[] buffer = new long[8];
        private Snapshot previous;
        private bool hasPrevious;
        internal readonly long[] Calls = new long[6], Ticks = new long[6], Bytes = new long[6], Events = new long[6];
        internal string? Failure { get; private set; }
        internal bool Active { get; private set; }
        internal Snapshot WindowStart { get; private set; }
        internal Snapshot WindowEnd { get; private set; }
        internal long StartTicks { get; private set; }
        internal long EndTicks { get; private set; }
        internal int Frames { get; private set; }
        internal double Seconds => (EndTicks - StartTicks) / (double)Stopwatch.Frequency;

        internal AllocationMeasurement(Action<long[]> reader, Func<long>? clock = null)
        { this.reader = reader ?? throw new ArgumentNullException(nameof(reader)); this.clock = clock ?? Stopwatch.GetTimestamp; }

        internal Snapshot Read()
        {
            if (Failure != null) return default;
            try { reader(buffer); }
            catch (Exception ex) { Invalidate("ReaderException:" + ex.GetType().Name); return default; }
            var value = new Snapshot(buffer);
            if (buffer[6] != 1 || buffer[7] != 2 || value.Thread <= 0 || value.Thread != buffer[5])
                Invalidate("ReaderInactiveOrWrongThread");
            else if (value.Bytes < 0 || value.Events < 0 || value.AllBytes < 0 || value.AllEvents < 0 ||
                hasPrevious && (value.Thread != previous.Thread || value.Bytes < previous.Bytes || value.Events < previous.Events ||
                    value.AllBytes < previous.AllBytes || value.AllEvents < previous.AllEvents))
                Invalidate("ReaderIdentityOrCounterChanged");
            previous = value; hasPrevious = true;
            return value;
        }

        internal void Invalidate(string reason) { if (Failure == null) Failure = reason; Active = false; }
        internal void StartWindow()
        {
            if (Active || StartTicks != 0) { Invalidate("WindowAlreadyStarted"); return; }
            WindowStart = Read(); StartTicks = clock(); Active = Failure == null;
        }
        internal Token Begin()
        {
            if (!Active) return default;
            var before = Read();
            return Active ? new Token(this, before, clock()) : default;
        }
        internal void End(int category, Token token)
        {
            if (!token.Active || !Active) return;
            long end = clock();
            var after = Read();
            if (!Active) return;
            if (!ReferenceEquals(token.Owner, this) || category < 0 || category >= Calls.Length)
            { Invalidate("InvalidPathToken"); return; }
            Calls[category]++; Ticks[category] += end - token.Ticks;
            Bytes[category] += after.Bytes - token.Before.Bytes; Events[category] += after.Events - token.Before.Events;
        }
        internal void Frame() { if (Active) Frames++; }
        internal void StopWindow()
        {
            if (!Active) { Invalidate("WindowNotActive"); return; }
            WindowEnd = Read(); EndTicks = clock(); Active = false;
            if (Frames <= 0) Invalidate("EmptyWindow");
            if (Seconds < 30) Invalidate("ShortWindow");
        }

        internal long VerifyReader()
        {
            // Warm the actual module -> managed delegate -> QA -> native read chain.
            for (int i = 0; i < 100; i++) Read();
            var before = Read();
            for (int i = 0; i < 10000; i++) Read();
            var after = Read();
            if (Failure != null || after.Events != before.Events || after.Bytes != before.Bytes)
                throw new InvalidOperationException("Final allocation reader must be valid and allocation-free: " + Failure);
            return after.Bytes - before.Bytes;
        }
    }
}
