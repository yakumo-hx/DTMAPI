using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DTMAPI.Core.Diagnostics;

namespace DTMAPI.Core.Runtime
{
    internal sealed class ModOwnerLedgerService
    {
        private const int MaxPreviewAggregates = 256;
        private const int MaxRecentFailures = 64;
        private const string PreviewOverflowKey = "\0DTMAPI.ConfigPreviewOverflow";
        private readonly object gate = new object();
        private readonly List<ModOwnerLedgerEntry> recentFailures = new List<ModOwnerLedgerEntry>();
        private readonly Dictionary<string, long> registrationCounts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, long> cleanupCounts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MutableConfigPreviewAggregate> previewAggregates = new Dictionary<string, MutableConfigPreviewAggregate>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ModOwnerTransactionState> activeTransactions = new Dictionary<string, ModOwnerTransactionState>(StringComparer.OrdinalIgnoreCase);
        private int nextTransactionId;
        private int committedTransactions;
        private int rolledBackTransactions;
        private int cleanupFailures;
        private int needsRestart;
        private int trimmedEntries;
        private long trimmedBytes;
        private long configPreviewObservedCount;
        private int configPreviewWarningCount;

        public string BeginTransaction(string ownerId, string phase, int threadId)
        {
            long trimmed = 0;
            ownerId = Normalize(ownerId, ref trimmed);
            string safePhase = BoundedDiagnosticScalar.Sanitize(phase, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            string transactionId;
            lock (gate)
            {
                BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedBytes, trimmed);
                nextTransactionId++;
                transactionId = "MOT-" + nextTransactionId.ToString("0000", CultureInfo.InvariantCulture);
                activeTransactions[ownerId] = new ModOwnerTransactionState(ownerId, transactionId, safePhase, threadId, DateTimeOffset.Now);
                return transactionId;
            }
        }

        public void CommitTransaction(string ownerId, string transactionId, string phase)
        {
            long trimmed = 0;
            ownerId = Normalize(ownerId, ref trimmed);
            transactionId = transactionId ?? string.Empty;
            lock (gate)
            {
                BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedBytes, trimmed);
                if (activeTransactions.TryGetValue(ownerId, out ModOwnerTransactionState state) &&
                    string.Equals(state.TransactionId, transactionId, StringComparison.OrdinalIgnoreCase))
                    activeTransactions.Remove(ownerId);
                committedTransactions++;
            }
        }

        public bool RollbackTransaction(string ownerId, string transactionId, string phase, string result, out string rolledBackTransactionId)
        {
            long trimmed = 0;
            ownerId = Normalize(ownerId, ref trimmed);
            transactionId = transactionId ?? string.Empty;
            string safePhase = BoundedDiagnosticScalar.Sanitize(phase, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            string safeResult = BoundedDiagnosticScalar.Sanitize(result, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            rolledBackTransactionId = string.Empty;
            lock (gate)
            {
                BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedBytes, trimmed);
                if (!activeTransactions.TryGetValue(ownerId, out ModOwnerTransactionState state) ||
                    (!string.IsNullOrWhiteSpace(transactionId) && !string.Equals(state.TransactionId, transactionId, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }

                activeTransactions.Remove(ownerId);
                rolledBackTransactionId = state.TransactionId;
                rolledBackTransactions++;
                AddFailureNoLock(new ModOwnerLedgerEntry(ownerId, state.TransactionId, "Transaction", "Entry", safePhase, "RolledBack", "ScalarOnly", safeResult, DateTimeOffset.Now));
                return true;
            }
        }

        public void RecordRegistration(string ownerId, string kind, string key, string phase, string cleanupPolicy)
        {
            lock (gate)
                IncrementNoLock(registrationCounts, NormalizeKind(kind));
        }

        public void RecordCleanup(string ownerId, string kind, int count, string phase, string result, bool success = true, int failureCount = 1)
        {
            count = Math.Max(0, count);
            string normalizedKind = NormalizeKind(kind);
            int normalizedFailures = Math.Max(1, failureCount);
            long trimmed = 0;
            string safeOwner = success ? string.Empty : Normalize(ownerId, ref trimmed);
            string safePhase = success ? string.Empty : BoundedDiagnosticScalar.Sanitize(phase, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            string safeDetails = string.Empty;
            if (!success)
            {
                string safeResult = BoundedDiagnosticScalar.Sanitize(result, BoundedDiagnosticScalar.DetailsChars - 64, ref trimmed);
                safeDetails = BoundedDiagnosticScalar.Sanitize("failureCount=" + normalizedFailures.ToString(CultureInfo.InvariantCulture) + "; " + safeResult, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            }
            lock (gate)
            {
                BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedBytes, trimmed);
                if (!success)
                {
                    cleanupFailures += normalizedFailures;
                    AddFailureNoLock(new ModOwnerLedgerEntry(safeOwner, GetActiveTransactionIdNoLock(safeOwner), normalizedKind, "count=" + count.ToString(CultureInfo.InvariantCulture), safePhase, "CleanupFailed", "ScalarOnly", safeDetails, DateTimeOffset.Now));
                }
                IncrementNoLock(cleanupCounts, normalizedKind, count);
            }
        }

        public void RecordNeedsRestart(string ownerId, string key, string phase, string details)
        {
            long trimmed = 0;
            string safeOwner = Normalize(ownerId, ref trimmed);
            string safeKey = BoundedDiagnosticScalar.Sanitize(key, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            string safePhase = BoundedDiagnosticScalar.Sanitize(phase, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            string safeDetails = BoundedDiagnosticScalar.Sanitize(details, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            lock (gate)
            {
                BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedBytes, trimmed);
                needsRestart++;
                AddFailureNoLock(new ModOwnerLedgerEntry(safeOwner, GetActiveTransactionIdNoLock(safeOwner), "ExternalSideEffect", safeKey, safePhase, "NeedsRestart", "ScalarOnly", safeDetails, DateTimeOffset.Now));
            }
        }

        public void RecordConfigPreview(string ownerId, string itemId, string kind, string operation, bool success, string details)
        {
            long trimmed = 0;
            string safeOwner = success ? string.Empty : Normalize(ownerId, ref trimmed);
            string safeKind = BoundedDiagnosticScalar.Sanitize(kind, BoundedDiagnosticScalar.IdentifierChars, ref trimmed);
            string safeOperation = BoundedDiagnosticScalar.Sanitize(operation, BoundedDiagnosticScalar.ShortTextChars, ref trimmed);
            string safeDetails = success ? string.Empty : BoundedDiagnosticScalar.Sanitize(details, BoundedDiagnosticScalar.DetailsChars, ref trimmed);
            lock (gate)
            {
                BoundedDiagnosticScalar.AddTrimmedBytes(ref trimmedBytes, trimmed);
                configPreviewObservedCount++;
                if (!success)
                    configPreviewWarningCount++;

                string aggregateKey = safeKind + "\u001f" + safeOperation;
                if (!previewAggregates.TryGetValue(aggregateKey, out MutableConfigPreviewAggregate aggregate))
                {
                    // Reserve one slot for a stable overflow bucket so unique operation/kind
                    // pairs can never grow the retained graph beyond the configured cap.
                    if (previewAggregates.Count >= MaxPreviewAggregates - 1)
                    {
                        aggregateKey = PreviewOverflowKey;
                        if (!previewAggregates.TryGetValue(aggregateKey, out aggregate))
                        {
                            aggregate = new MutableConfigPreviewAggregate("other", "scope", "other", "other", DateTimeOffset.Now);
                            previewAggregates[aggregateKey] = aggregate;
                        }
                    }
                    else
                    {
                        aggregate = new MutableConfigPreviewAggregate("aggregate", "scope", safeKind, safeOperation, DateTimeOffset.Now);
                        previewAggregates[aggregateKey] = aggregate;
                    }
                }
                aggregate.Record(success, DateTimeOffset.Now);

                if (!success)
                {
                    AddFailureNoLock(new ModOwnerLedgerEntry(safeOwner, GetActiveTransactionIdNoLock(safeOwner), "ConfigPreview", safeKind, safeOperation, "Warning", "ScalarOnly", safeDetails, DateTimeOffset.Now));
                }
            }
        }

        public ModOwnerLedgerSnapshot GetSnapshot()
        {
            lock (gate)
            {
                ModOwnerLedgerEntry[] copy = recentFailures.ToArray();
                ModOwnerConfigPreviewAggregate[] previewCopy = previewAggregates.Values
                    .Select(value => value.ToSnapshot())
                    .ToArray();
                return new ModOwnerLedgerSnapshot(
                    copy,
                    previewCopy,
                    new Dictionary<string, long>(registrationCounts, StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, long>(cleanupCounts, StringComparer.OrdinalIgnoreCase),
                    activeTransactions.Count,
                    committedTransactions,
                    rolledBackTransactions,
                    cleanupFailures,
                    needsRestart,
                    configPreviewObservedCount,
                    configPreviewWarningCount,
                    recentFailures.Count(entry => entry.Kind.Equals("ConfigPreview", StringComparison.OrdinalIgnoreCase)),
                    trimmedEntries,
                    trimmedBytes);
            }
        }

        private string GetActiveTransactionIdNoLock(string ownerId)
        {
            return activeTransactions.TryGetValue(ownerId, out ModOwnerTransactionState state) ? state.TransactionId : string.Empty;
        }

        private void AddFailureNoLock(ModOwnerLedgerEntry entry)
        {
            recentFailures.Add(entry);
            if (recentFailures.Count > MaxRecentFailures)
            {
                recentFailures.RemoveAt(0);
                trimmedEntries++;
            }
        }

        private static void IncrementNoLock(Dictionary<string, long> counts, string kind, int count = 1)
        {
            counts[kind] = (counts.TryGetValue(kind, out long existing) ? existing : 0) + Math.Max(0, count);
        }

        private static string NormalizeKind(string kind)
        {
            switch ((kind ?? string.Empty).Trim())
            {
                case "Api": return "Api";
                case "ApiOrLoadedMod": return "ApiOrLoadedMod";
                case "ConfigMenuPage": return "ConfigMenuPage";
                case "ConfigMigration": return "ConfigMigration";
                case "CustomEntity": return "CustomEntity";
                case "Event": return "Event";
                case "EventHandler": return "EventHandler";
                case "GameBridgeOwnerResource": return "GameBridgeOwnerResource";
                case "Input": return "Input";
                case "InputButton": return "InputButton";
                case "InputSnapshotButton": return "InputSnapshotButton";
                case "LoadedCodeMod": return "LoadedCodeMod";
                case "Registry": return "Registry";
                default: return "Other";
            }
        }

        private static string Normalize(string ownerId, ref long trimmed)
        {
            string normalized = string.IsNullOrWhiteSpace(ownerId) ? "unknown" : ownerId.Trim();
            return BoundedDiagnosticScalar.Sanitize(normalized, BoundedDiagnosticScalar.OwnerIdChars, ref trimmed);
        }

        private sealed class ModOwnerTransactionState
        {
            public ModOwnerTransactionState(string ownerId, string transactionId, string phase, int threadId, DateTimeOffset startedAt)
            {
                OwnerId = ownerId;
                TransactionId = transactionId;
                Phase = phase;
                ThreadId = threadId;
                StartedAt = startedAt;
            }

            public string OwnerId { get; }
            public string TransactionId { get; }
            public string Phase { get; }
            public int ThreadId { get; }
            public DateTimeOffset StartedAt { get; }
        }

        private sealed class MutableConfigPreviewAggregate
        {
            public MutableConfigPreviewAggregate(string ownerId, string itemId, string kind, string operation, DateTimeOffset firstObservedAt)
            {
                OwnerId = ownerId;
                ItemId = itemId;
                Kind = kind;
                Operation = operation;
                FirstObservedAt = firstObservedAt;
                LastObservedAt = firstObservedAt;
            }

            public string OwnerId { get; }
            public string ItemId { get; }
            public string Kind { get; }
            public string Operation { get; }
            public DateTimeOffset FirstObservedAt { get; }
            public DateTimeOffset LastObservedAt { get; private set; }
            public long ObservationCount { get; private set; }
            public int WarningCount { get; private set; }
            public void Record(bool success, DateTimeOffset timestamp)
            {
                ObservationCount++;
                if (!success)
                    WarningCount++;
                LastObservedAt = timestamp;
            }

            public ModOwnerConfigPreviewAggregate ToSnapshot()
            {
                return new ModOwnerConfigPreviewAggregate(OwnerId, ItemId, Kind, Operation, ObservationCount, WarningCount, FirstObservedAt, LastObservedAt, string.Empty);
            }
        }
    }

    internal sealed class ModOwnerConfigPreviewAggregate
    {
        public ModOwnerConfigPreviewAggregate(string ownerId, string itemId, string kind, string operation, long observationCount, int warningCount, DateTimeOffset firstObservedAt, DateTimeOffset lastObservedAt, string lastDetails)
        {
            OwnerId = ownerId;
            ItemId = itemId;
            Kind = kind;
            Operation = operation;
            ObservationCount = observationCount;
            WarningCount = warningCount;
            FirstObservedAt = firstObservedAt;
            LastObservedAt = lastObservedAt;
            // Kept for internal snapshot shape compatibility. Aggregate rows retain counts
            // only; arbitrary failure details live exclusively in the bounded recent window.
            LastDetails = string.Empty;
        }

        public string OwnerId { get; }
        public string ItemId { get; }
        public string Kind { get; }
        public string Operation { get; }
        public long ObservationCount { get; }
        public int WarningCount { get; }
        public DateTimeOffset FirstObservedAt { get; }
        public DateTimeOffset LastObservedAt { get; }
        public string LastDetails { get; }
    }

    internal sealed class ModOwnerLedgerEntry
    {
        public ModOwnerLedgerEntry(
            string ownerId,
            string transactionId,
            string kind,
            string key,
            string phase,
            string status,
            string cleanupPolicy,
            string rollbackResult,
            DateTimeOffset timestamp)
        {
            OwnerId = ownerId;
            TransactionId = transactionId;
            Kind = kind;
            Key = key;
            Phase = phase;
            Status = status;
            CleanupPolicy = cleanupPolicy;
            RollbackResult = rollbackResult;
            Timestamp = timestamp;
        }

        public string OwnerId { get; }
        public string TransactionId { get; }
        public string Kind { get; }
        public string Key { get; }
        public string Phase { get; }
        public string Status { get; }
        public string CleanupPolicy { get; }
        public string RollbackResult { get; }
        public DateTimeOffset Timestamp { get; }
    }

    internal sealed class ModOwnerLedgerSnapshot
    {
        public ModOwnerLedgerSnapshot(
            IReadOnlyList<ModOwnerLedgerEntry> entries,
            IReadOnlyList<ModOwnerConfigPreviewAggregate> configPreviewAggregates,
            IReadOnlyDictionary<string, long> registrationCounts,
            IReadOnlyDictionary<string, long> cleanupCounts,
            int activeTransactions,
            int committedTransactions,
            int rolledBackTransactions,
            int cleanupFailures,
            int needsRestart,
            long configPreviewObservedCount,
            int configPreviewWarningCount,
            int recentConfigPreviewFailureCount,
            int trimmedEntries,
            long trimmedBytes)
        {
            Entries = entries;
            ConfigPreviewAggregates = configPreviewAggregates;
            RegistrationCounts = registrationCounts;
            CleanupCounts = cleanupCounts;
            ActiveTransactions = activeTransactions;
            CommittedTransactions = committedTransactions;
            RolledBackTransactions = rolledBackTransactions;
            CleanupFailures = cleanupFailures;
            NeedsRestart = needsRestart;
            ConfigPreviewObservedCount = configPreviewObservedCount;
            ConfigPreviewWarningCount = configPreviewWarningCount;
            RecentConfigPreviewFailureCount = recentConfigPreviewFailureCount;
            TrimmedEntries = trimmedEntries;
            TrimmedBytes = trimmedBytes;
        }

        public IReadOnlyList<ModOwnerLedgerEntry> Entries { get; }
        public IReadOnlyList<ModOwnerConfigPreviewAggregate> ConfigPreviewAggregates { get; }
        public IReadOnlyDictionary<string, long> RegistrationCounts { get; }
        public IReadOnlyDictionary<string, long> CleanupCounts { get; }
        public int ActiveTransactions { get; }
        public int CommittedTransactions { get; }
        public int RolledBackTransactions { get; }
        public int CleanupFailures { get; }
        public int NeedsRestart { get; }
        public long ConfigPreviewObservedCount { get; }
        public int ConfigPreviewWarningCount { get; }
        public int RecentConfigPreviewFailureCount { get; }
        public int TrimmedEntries { get; }
        public long TrimmedBytes { get; }
        public bool Success => ActiveTransactions == 0 && CleanupFailures == 0;

        public string FormatSummary()
        {
            string owners = string.Join("|", Entries.Select(e => e.OwnerId).Where(o => !string.IsNullOrWhiteSpace(o)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(o => o, StringComparer.OrdinalIgnoreCase).Take(12));
            if (string.IsNullOrWhiteSpace(owners))
                owners = "none";
            return "status=" + (Success ? "ok" : "warning") +
                "; owners=" + owners +
                "; recentFailures=" + Entries.Count.ToString(CultureInfo.InvariantCulture) +
                "; activeTransactions=" + ActiveTransactions.ToString(CultureInfo.InvariantCulture) +
                "; committedTransactions=" + CommittedTransactions.ToString(CultureInfo.InvariantCulture) +
                "; rolledBackTransactions=" + RolledBackTransactions.ToString(CultureInfo.InvariantCulture) +
                "; cleanupFailures=" + CleanupFailures.ToString(CultureInfo.InvariantCulture) +
                "; needsRestart=" + NeedsRestart.ToString(CultureInfo.InvariantCulture) +
                "; configPreviewObservations=" + ConfigPreviewObservedCount.ToString(CultureInfo.InvariantCulture) +
                "; configPreviewAggregates=" + ConfigPreviewAggregates.Count.ToString(CultureInfo.InvariantCulture) +
                "; configPreviewWarnings=" + ConfigPreviewWarningCount.ToString(CultureInfo.InvariantCulture) +
                "; retainedPreviewFailures=" + RecentConfigPreviewFailureCount.ToString(CultureInfo.InvariantCulture) +
                "; trimmedRecords=" + TrimmedEntries.ToString(CultureInfo.InvariantCulture) +
                "; trimmedBytes=" + TrimmedBytes.ToString(CultureInfo.InvariantCulture) +
                "; " + FormatKindCounts();
        }

        public string FormatPerOwnerSummary(int maxOwners = 32)
        {
            var groups = Entries
                .GroupBy(e => e.OwnerId, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Max(1, maxOwners))
                .Select(g =>
                {
                    int records = g.Count();
                    return SanitizeMetricKey(g.Key) +
                        "={recentFailures=" + records.ToString(CultureInfo.InvariantCulture) + "}";
                });
            string value = string.Join("; ", groups);
            return string.IsNullOrWhiteSpace(value) ? "none" : value;
        }

        public string FormatRollbackSummary()
        {
            var rollbackEntries = Entries
                .Where(e =>
                    e.Status.Equals("Cleaned", StringComparison.OrdinalIgnoreCase) ||
                    e.Status.Equals("RolledBack", StringComparison.OrdinalIgnoreCase) ||
                    e.Status.Equals("NeedsRestart", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (rollbackEntries.Length == 0)
                return "status=idle; rollbackRecords=0; needsRestart=" + NeedsRestart.ToString(CultureInfo.InvariantCulture);
            return "status=" + (CleanupFailures == 0 ? "ok" : "warning") +
                "; rollbackRecords=" + rollbackEntries.Length.ToString(CultureInfo.InvariantCulture) +
                "; needsRestart=" + NeedsRestart.ToString(CultureInfo.InvariantCulture) +
                "; " + string.Join(", ", rollbackEntries.Take(10).Select(e => e.OwnerId + "/" + e.Kind + "=" + e.RollbackResult));
        }

        public string FormatOwnerRegistrations()
        {
            string value = string.Join("; ", RegistrationCounts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => pair.Key + "=" + pair.Value.ToString(CultureInfo.InvariantCulture)));
            return string.IsNullOrWhiteSpace(value) ? "none" : value;
        }

        private string FormatKindCounts()
        {
            string registrations = string.Join(",", RegistrationCounts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => pair.Key + ":registered=" + pair.Value.ToString(CultureInfo.InvariantCulture)));
            string cleanups = string.Join(",", CleanupCounts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => pair.Key + ":cleaned=" + pair.Value.ToString(CultureInfo.InvariantCulture)));
            string counts = string.Join(",", new[] { registrations, cleanups }.Where(value => !string.IsNullOrWhiteSpace(value)));
            if (ConfigPreviewObservedCount > 0)
                counts = (string.IsNullOrWhiteSpace(counts) ? string.Empty : counts + ",") + "ConfigPreview:" + ConfigPreviewObservedCount.ToString(CultureInfo.InvariantCulture);
            return "kinds=" + (string.IsNullOrWhiteSpace(counts) ? "none" : counts);
        }

        private static int CountKind(IEnumerable<ModOwnerLedgerEntry> entries, string kind)
        {
            return entries.Count(e => e.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase));
        }

        private static string SanitizeMetricKey(string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
            var builder = new System.Text.StringBuilder(text.Length);
            bool previousUnderscore = false;
            foreach (char ch in text)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.')
                {
                    builder.Append(ch);
                    previousUnderscore = false;
                    continue;
                }

                if (!previousUnderscore)
                {
                    builder.Append('_');
                    previousUnderscore = true;
                }
            }

            return builder.ToString().Trim('_');
        }
    }
}
