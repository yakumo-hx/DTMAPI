using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DTMAPI.Core.Diagnostics;

namespace DTMAPI.Core.Runtime
{
    internal sealed class HookStatusPublicationQueue
    {
        private const int DefaultMaxPending = 512;
        private readonly object gate = new object();
        private readonly Dictionary<string, HookStatusPublication> pendingByHookId = new Dictionary<string, HookStatusPublication>(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<string> order = new Queue<string>();
        private long totalEnqueued;
        private long totalCoalesced;
        private long totalDropped;
        private long totalFlushed;
        private long offThreadEnqueued;
        private long trimmedBytes;
        private long diagnosticRevision;
        private int maxDepth;
        private int pendingCount;

        public HookStatusPublicationQueue(int maxPending = DefaultMaxPending)
        {
            MaxPending = Math.Max(8, maxPending);
        }

        public int MaxPending { get; }

        /// <summary>
        /// Cheap producer/consumer hint used by the Runtime frame boundary. A false result may
        /// race with a later enqueue, in which case the next safe boundary drains it.
        /// </summary>
        public bool HasPending => Volatile.Read(ref pendingCount) > 0;

        /// <summary>
        /// Changes only when queue/counter state represented by <see cref="HookStatusQueueSnapshot"/>
        /// changes. Reading it must not construct the detailed diagnostic projection.
        /// </summary>
        public long DiagnosticRevision => Interlocked.Read(ref diagnosticRevision);

        public HookStatusQueueSnapshot Enqueue(string hookId, string status, string source, string details, string phase, int threadId, int runtimeThreadId)
        {
            var publication = new HookStatusPublication(hookId, status, source, details, phase, threadId, DateTimeOffset.Now);
            hookId = publication.HookId;
            lock (gate)
            {
                totalEnqueued++;
                BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedBytes, publication.TrimmedBytes);
                if (threadId != runtimeThreadId)
                    offThreadEnqueued++;

                if (pendingByHookId.ContainsKey(hookId))
                {
                    pendingByHookId[hookId] = publication;
                    totalCoalesced++;
                    Interlocked.Increment(ref diagnosticRevision);
                    return BuildSnapshotNoLock();
                }

                while (pendingByHookId.Count >= MaxPending && order.Count > 0)
                {
                    string oldest = order.Dequeue();
                    if (pendingByHookId.Remove(oldest))
                    {
                        totalDropped++;
                        break;
                    }
                }

                pendingByHookId[hookId] = publication;
                order.Enqueue(hookId);
                if (pendingByHookId.Count > maxDepth)
                    maxDepth = pendingByHookId.Count;
                Volatile.Write(ref pendingCount, pendingByHookId.Count);
                Interlocked.Increment(ref diagnosticRevision);
                return BuildSnapshotNoLock();
            }
        }

        public IReadOnlyList<HookStatusPublication> Drain()
        {
            lock (gate)
            {
                if (pendingByHookId.Count == 0)
                    return Array.Empty<HookStatusPublication>();

                var items = new List<HookStatusPublication>(pendingByHookId.Count);
                while (order.Count > 0)
                {
                    string hookId = order.Dequeue();
                    if (pendingByHookId.TryGetValue(hookId, out HookStatusPublication? publication))
                    {
                        pendingByHookId.Remove(hookId);
                        items.Add(publication);
                    }
                }

                totalFlushed += items.Count;
                Volatile.Write(ref pendingCount, pendingByHookId.Count);
                Interlocked.Increment(ref diagnosticRevision);
                return items;
            }
        }

        public HookStatusQueueSnapshot GetSnapshot()
        {
            lock (gate)
                return BuildSnapshotNoLock();
        }

        private HookStatusQueueSnapshot BuildSnapshotNoLock()
        {
            HookStatusPublication? latest = pendingByHookId.Values.OrderByDescending(item => item.QueuedAt).FirstOrDefault();
            return new HookStatusQueueSnapshot(
                diagnosticRevision,
                MaxPending,
                pendingByHookId.Count,
                maxDepth,
                totalEnqueued,
                totalCoalesced,
                totalDropped,
                totalFlushed,
                offThreadEnqueued,
                trimmedBytes,
                latest?.HookId ?? string.Empty,
                latest?.Phase ?? string.Empty,
                latest?.ThreadId ?? 0);
        }
    }

    internal sealed class HookStatusPublication
    {
        public HookStatusPublication(string hookId, string status, string source, string details, string phase, int threadId, DateTimeOffset queuedAt)
        {
            long trimmed = 0;
            HookId = BoundedDiagnosticScalar.Sanitize(hookId, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            Status = BoundedDiagnosticScalar.Sanitize(status, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            Source = BoundedDiagnosticScalar.Sanitize(source, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            Details = BoundedDiagnosticScalar.Sanitize(details, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            Phase = BoundedDiagnosticScalar.Sanitize(phase, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            ThreadId = threadId;
            QueuedAt = queuedAt;
            TrimmedBytes = trimmed;
        }

        public string HookId { get; }

        public string Status { get; }

        public string Source { get; }

        public string Details { get; }

        public string Phase { get; }

        public int ThreadId { get; }

        public DateTimeOffset QueuedAt { get; }

        public long TrimmedBytes { get; }
    }

    internal sealed class HookStatusQueueSnapshot
    {
        public HookStatusQueueSnapshot(
            long revision,
            int maxPending,
            int pending,
            int maxDepth,
            long totalEnqueued,
            long totalCoalesced,
            long totalDropped,
            long totalFlushed,
            long offThreadEnqueued,
            long trimmedBytes,
            string latestHookId,
            string latestPhase,
            int latestThreadId)
        {
            Revision = Math.Max(0, revision);
            MaxPending = maxPending;
            Pending = pending;
            MaxDepth = maxDepth;
            TotalEnqueued = totalEnqueued;
            TotalCoalesced = totalCoalesced;
            TotalDropped = totalDropped;
            TotalFlushed = totalFlushed;
            OffThreadEnqueued = offThreadEnqueued;
            TrimmedBytes = Math.Max(0, trimmedBytes);
            LatestHookId = latestHookId;
            LatestPhase = latestPhase;
            LatestThreadId = latestThreadId;
        }

        public long Revision { get; }

        public int MaxPending { get; }

        public int Pending { get; }

        public int MaxDepth { get; }

        public long TotalEnqueued { get; }

        public long TotalCoalesced { get; }

        public long TotalDropped { get; }

        public long TotalFlushed { get; }

        public long OffThreadEnqueued { get; }

        public long TrimmedBytes { get; }

        public string LatestHookId { get; }

        public string LatestPhase { get; }

        public int LatestThreadId { get; }

        public bool Success => TotalDropped == 0;

        public string FormatSummary()
        {
            return "status=" + (Success ? "ok" : "warning") +
                "; pending=" + Pending +
                "; maxPending=" + MaxPending +
                "; maxDepth=" + MaxDepth +
                "; enqueued=" + TotalEnqueued +
                "; coalesced=" + TotalCoalesced +
                "; flushed=" + TotalFlushed +
                "; dropped=" + TotalDropped +
                "; offThread=" + OffThreadEnqueued +
                "; trimmedBytes=" + TrimmedBytes +
                "; latestHook=" + SingleLine(LatestHookId) +
                "; latestPhase=" + SingleLine(LatestPhase) +
                "; latestThread=" + LatestThreadId;
        }

        public string FormatOffThreadSummary()
        {
            return OffThreadEnqueued == 0
                ? "status=ok; offThreadHookStatuses=0"
                : "status=observed; offThreadHookStatuses=" + OffThreadEnqueued + "; latestHook=" + SingleLine(LatestHookId) + "; latestThread=" + LatestThreadId;
        }

        private static string SingleLine(string? value)
        {
            string text = value ?? string.Empty;
            return string.IsNullOrWhiteSpace(text)
                ? "-"
                : text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        }
    }
}
