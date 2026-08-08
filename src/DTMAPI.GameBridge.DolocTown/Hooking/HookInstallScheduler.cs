using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class HookInstallScheduler
    {
        private readonly object gate = new object();
        private readonly Dictionary<string, HookInstallRequest> pendingByKey = new Dictionary<string, HookInstallRequest>(StringComparer.OrdinalIgnoreCase);
        private long totalRequested;
        private long totalCoalesced;
        private long totalProcessed;
        private long offThreadRequests;
        private string latestReason = string.Empty;
        private string latestPhase = string.Empty;
        private int latestThreadId;
        private DateTimeOffset latestRequestedAt = DateTimeOffset.MinValue;

        internal bool HasPending
        {
            get
            {
                lock (gate)
                    return pendingByKey.Count != 0;
            }
        }

        public HookInstallSchedulerSnapshot Request(string reason, string phase, int threadId, int runtimeThreadId)
        {
            reason = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim();
            phase = phase ?? string.Empty;
            string key = GetRequestKey(reason);
            lock (gate)
            {
                totalRequested++;
                if (threadId != runtimeThreadId)
                    offThreadRequests++;

                latestReason = reason;
                latestPhase = phase;
                latestThreadId = threadId;
                latestRequestedAt = DateTimeOffset.Now;

                if (pendingByKey.TryGetValue(key, out HookInstallRequest? existing))
                {
                    existing.Count++;
                    existing.LastReason = reason;
                    existing.LastPhase = phase;
                    existing.LastThreadId = threadId;
                    existing.LastRequestedAt = latestRequestedAt;
                    totalCoalesced++;
                    return BuildSnapshotNoLock(false, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, legacyAllReady: false, assemblyLoadSubscribed: false, retryTimerAlive: false);
                }

                pendingByKey[key] = new HookInstallRequest(key, reason, phase, threadId, latestRequestedAt);
                return BuildSnapshotNoLock(false, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, legacyAllReady: false, assemblyLoadSubscribed: false, retryTimerAlive: false);
            }
        }

        public bool TryConsumePending(out HookInstallRequest[] requests)
        {
            lock (gate)
            {
                if (pendingByKey.Count == 0)
                {
                    requests = Array.Empty<HookInstallRequest>();
                    return false;
                }

                requests = pendingByKey.Values.OrderBy(request => request.FirstRequestedAt).ToArray();
                pendingByKey.Clear();
                totalProcessed += requests.Length;
                return true;
            }
        }

        public HookInstallRequest[] ConsumePending()
        {
            return TryConsumePending(out HookInstallRequest[] requests)
                ? requests
                : Array.Empty<HookInstallRequest>();
        }

        public HookInstallSchedulerSnapshot GetSnapshot(
            bool coreReady,
            string coreSummary,
            string featureSummary,
            string smokeDiagnosticsSummary,
            string assemblySummary,
            string retrySummary,
            bool legacyAllReady,
            bool assemblyLoadSubscribed,
            bool retryTimerAlive)
        {
            lock (gate)
                return BuildSnapshotNoLock(coreReady, coreSummary, featureSummary, smokeDiagnosticsSummary, assemblySummary, retrySummary, legacyAllReady, assemblyLoadSubscribed, retryTimerAlive);
        }

        private HookInstallSchedulerSnapshot BuildSnapshotNoLock(
            bool coreReady,
            string coreSummary,
            string featureSummary,
            string smokeDiagnosticsSummary,
            string assemblySummary,
            string retrySummary,
            bool legacyAllReady,
            bool assemblyLoadSubscribed,
            bool retryTimerAlive)
        {
            return new HookInstallSchedulerSnapshot(
                pendingByKey.Count,
                totalRequested,
                totalCoalesced,
                totalProcessed,
                offThreadRequests,
                latestReason,
                latestPhase,
                latestThreadId,
                latestRequestedAt,
                coreReady,
                coreSummary ?? string.Empty,
                featureSummary ?? string.Empty,
                smokeDiagnosticsSummary ?? string.Empty,
                assemblySummary ?? string.Empty,
                retrySummary ?? string.Empty,
                legacyAllReady,
                assemblyLoadSubscribed,
                retryTimerAlive);
        }

        private static string GetRequestKey(string reason)
        {
            if (reason.StartsWith("AssemblyLoad", StringComparison.OrdinalIgnoreCase))
                return "AssemblyLoad";
            if (reason.StartsWith("RetryTimer", StringComparison.OrdinalIgnoreCase))
                return "RetryTimer";
            if (reason.StartsWith("Initialize", StringComparison.OrdinalIgnoreCase))
                return "Initialize";
            return reason;
        }
    }

    internal sealed class HookInstallRequest
    {
        public HookInstallRequest(string key, string reason, string phase, int threadId, DateTimeOffset requestedAt)
        {
            Key = key;
            FirstReason = reason;
            LastReason = reason;
            FirstPhase = phase;
            LastPhase = phase;
            FirstThreadId = threadId;
            LastThreadId = threadId;
            FirstRequestedAt = requestedAt;
            LastRequestedAt = requestedAt;
            Count = 1;
        }

        public string Key { get; }

        public string FirstReason { get; }

        public string LastReason { get; set; }

        public string FirstPhase { get; }

        public string LastPhase { get; set; }

        public int FirstThreadId { get; }

        public int LastThreadId { get; set; }

        public DateTimeOffset FirstRequestedAt { get; }

        public DateTimeOffset LastRequestedAt { get; set; }

        public int Count { get; set; }
    }

    internal sealed class HookInstallSchedulerSnapshot
    {
        public HookInstallSchedulerSnapshot(
            int pending,
            long totalRequested,
            long totalCoalesced,
            long totalProcessed,
            long offThreadRequests,
            string latestReason,
            string latestPhase,
            int latestThreadId,
            DateTimeOffset latestRequestedAt,
            bool coreReady,
            string coreSummary,
            string featureSummary,
            string smokeDiagnosticsSummary,
            string assemblySummary,
            string retrySummary,
            bool legacyAllReady,
            bool assemblyLoadSubscribed,
            bool retryTimerAlive)
        {
            Pending = pending;
            TotalRequested = totalRequested;
            TotalCoalesced = totalCoalesced;
            TotalProcessed = totalProcessed;
            OffThreadRequests = offThreadRequests;
            LatestReason = latestReason;
            LatestPhase = latestPhase;
            LatestThreadId = latestThreadId;
            LatestRequestedAt = latestRequestedAt;
            CoreReady = coreReady;
            CoreSummary = coreSummary;
            FeatureSummary = featureSummary;
            SmokeDiagnosticsSummary = smokeDiagnosticsSummary;
            AssemblySummary = assemblySummary;
            RetrySummary = retrySummary;
            LegacyAllReady = legacyAllReady;
            AssemblyLoadSubscribed = assemblyLoadSubscribed;
            RetryTimerAlive = retryTimerAlive;
        }

        public int Pending { get; }

        public long TotalRequested { get; }

        public long TotalCoalesced { get; }

        public long TotalProcessed { get; }

        public long OffThreadRequests { get; }

        public string LatestReason { get; }

        public string LatestPhase { get; }

        public int LatestThreadId { get; }

        public DateTimeOffset LatestRequestedAt { get; }

        public bool CoreReady { get; }

        public string CoreSummary { get; }

        public string FeatureSummary { get; }

        public string SmokeDiagnosticsSummary { get; }

        public string AssemblySummary { get; }

        public string RetrySummary { get; }

        public bool LegacyAllReady { get; }

        public bool AssemblyLoadSubscribed { get; }

        public bool RetryTimerAlive { get; }

        public string FormatSummary()
        {
            return "status=ok" +
                "; pending=" + Pending +
                "; requested=" + TotalRequested +
                "; coalesced=" + TotalCoalesced +
                "; processed=" + TotalProcessed +
                "; offThreadRequests=" + OffThreadRequests +
                "; latestReason=" + SingleLine(LatestReason) +
                "; latestPhase=" + SingleLine(LatestPhase) +
                "; latestThread=" + LatestThreadId +
                "; coreReady=" + (CoreReady ? "true" : "false") +
                "; legacyAllReady=" + (LegacyAllReady ? "true" : "false") +
                "; assemblyLoadSubscribed=" + (AssemblyLoadSubscribed ? "true" : "false") +
                "; retryTimerAlive=" + (RetryTimerAlive ? "true" : "false");
        }

        public string FormatOffThreadSummary()
        {
            return OffThreadRequests == 0
                ? "status=ok; offThreadHookInstallRequests=0"
                : "status=observed; offThreadHookInstallRequests=" + OffThreadRequests + "; latestReason=" + SingleLine(LatestReason) + "; latestThread=" + LatestThreadId;
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
