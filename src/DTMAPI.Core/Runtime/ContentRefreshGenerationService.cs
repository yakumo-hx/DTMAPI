using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.Core.Runtime
{
    internal static class ContentRefreshDiagnosticBounds
    {
        public static string Identity(string value, int maxLength, string fallback)
        {
            string normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0)
                normalized = fallback ?? string.Empty;
            if (normalized.Length <= maxLength)
                return normalized;

            uint hash = 2166136261;
            foreach (char character in normalized)
            {
                hash ^= character;
                hash *= 16777619;
            }
            string suffix = "~" + hash.ToString("x8");
            return normalized.Substring(0, Math.Max(0, maxLength - suffix.Length)) + suffix;
        }

        public static string Text(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }
    }

    internal static class ContentRefreshDomains
    {
        public const string ContentQuery = "ContentQuery";
        public const string CustomAnimals = "CustomAnimals";
        public const string AudioReplacement = "AudioReplacement";

        public static readonly string[] SourceDriven =
        {
            ContentQuery,
            CustomAnimals,
            AudioReplacement
        };
    }

    internal enum ContentRefreshCompletionStatus
    {
        Committed,
        Rejected,
        Removed,
        Unchanged,
        Trimmed
    }

    internal readonly struct ContentRefreshDirtyBatch
    {
        public ContentRefreshDirtyBatch(string domain, long generation, IReadOnlyList<string> ownerIds, IReadOnlyList<string> reasons)
        {
            Domain = ContentRefreshDiagnosticBounds.Identity(domain, 64, "unknown");
            Generation = generation;
            OwnerIds = (ownerIds ?? Array.Empty<string>())
                .Take(256)
                .ToArray();
            Reasons = (reasons ?? Array.Empty<string>())
                .Take(16)
                .Select(reason => ContentRefreshDiagnosticBounds.Text(reason, 192))
                .ToArray();
        }

        public string Domain { get; }
        public long Generation { get; }
        public IReadOnlyList<string> OwnerIds { get; }
        public IReadOnlyList<string> Reasons { get; }
        public string ReasonSummary => Reasons.Count == 0 ? "none" : string.Join(" | ", Reasons);
    }

    internal readonly struct ContentRefreshCompletion
    {
        public ContentRefreshCompletion(
            string ownerId,
            ContentRefreshCompletionStatus status,
            long previousGeneration,
            long currentGeneration,
            long lastGoodGeneration,
            string details)
        {
            OwnerId = ContentRefreshDiagnosticBounds.Identity(ownerId, 128, "all");
            Status = status;
            PreviousGeneration = previousGeneration;
            CurrentGeneration = currentGeneration;
            LastGoodGeneration = lastGoodGeneration;
            Details = ContentRefreshDiagnosticBounds.Text(details, 512);
        }

        public string OwnerId { get; }
        public ContentRefreshCompletionStatus Status { get; }
        public long PreviousGeneration { get; }
        public long CurrentGeneration { get; }
        public long LastGoodGeneration { get; }
        public string Details { get; }

        private static string Bound(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }
    }

    /// <summary>
    /// Keeps per-owner completion diagnostics bounded while the authoritative
    /// content transaction is still free to process every active owner.
    /// </summary>
    internal sealed class ContentRefreshCompletionCollector : IEnumerable<ContentRefreshCompletion>
    {
        private const int MaxNamedCompletions = 254;
        private readonly List<ContentRefreshCompletion> named = new List<ContentRefreshCompletion>();
        private int trimmed;
        private int committed;
        private int rejected;
        private int removed;
        private int unchanged;

        public int Count => named.Count + (trimmed > 0 ? 1 : 0);

        public int TotalCompletionCount => named.Count + trimmed;

        public void Add(ContentRefreshCompletion completion)
        {
            if (named.Count < MaxNamedCompletions)
            {
                named.Add(completion);
                return;
            }

            trimmed++;
            switch (completion.Status)
            {
                case ContentRefreshCompletionStatus.Committed:
                    committed++;
                    break;
                case ContentRefreshCompletionStatus.Rejected:
                    rejected++;
                    break;
                case ContentRefreshCompletionStatus.Removed:
                    removed++;
                    break;
                default:
                    unchanged++;
                    break;
            }
        }

        public IEnumerator<ContentRefreshCompletion> GetEnumerator()
        {
            foreach (ContentRefreshCompletion completion in named)
                yield return completion;
            if (trimmed > 0)
            {
                yield return new ContentRefreshCompletion(
                    "all",
                    ContentRefreshCompletionStatus.Trimmed,
                    0,
                    0,
                    0,
                    "trimmedCompletions=" + trimmed +
                    "; committed=" + committed +
                    "; rejected=" + rejected +
                    "; removed=" + removed +
                    "; unchanged=" + unchanged +
                    "; namedCompletionCapacity=" + MaxNamedCompletions);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal readonly struct ContentRefreshDomainSnapshot
    {
        public ContentRefreshDomainSnapshot(string domain, bool dirty, bool inFlight, long generation, int ownerCount, int reasonCount, bool nextDirty, long nextGeneration)
        {
            Domain = ContentRefreshDiagnosticBounds.Identity(domain, 64, "unknown");
            Dirty = dirty;
            InFlight = inFlight;
            Generation = generation;
            OwnerCount = ownerCount;
            ReasonCount = reasonCount;
            NextDirty = nextDirty;
            NextGeneration = nextGeneration;
        }

        public string Domain { get; }
        public bool Dirty { get; }
        public bool InFlight { get; }
        public long Generation { get; }
        public int OwnerCount { get; }
        public int ReasonCount { get; }
        public bool NextDirty { get; }
        public long NextGeneration { get; }

        public string FormatSummary()
        {
            return "domain=" + Domain +
                ",dirty=" + Dirty +
                ",inFlight=" + InFlight +
                ",generation=" + Generation +
                ",owners=" + OwnerCount +
                ",reasons=" + ReasonCount +
                ",nextDirty=" + NextDirty +
                ",nextGeneration=" + NextGeneration;
        }
    }

    internal sealed class ContentRefreshGenerationSnapshot
    {
        public ContentRefreshGenerationSnapshot(long nextDirtyGeneration, IReadOnlyList<ContentRefreshDomainSnapshot> domains, IReadOnlyList<ContentRefreshReceipt> receipts)
        {
            NextDirtyGeneration = nextDirtyGeneration;
            Domains = domains ?? Array.Empty<ContentRefreshDomainSnapshot>();
            Receipts = receipts ?? Array.Empty<ContentRefreshReceipt>();
        }

        public long NextDirtyGeneration { get; }
        public IReadOnlyList<ContentRefreshDomainSnapshot> Domains { get; }
        public IReadOnlyList<ContentRefreshReceipt> Receipts { get; }

        public string FormatSummary()
        {
            string domainSummary = Domains.Count == 0
                ? "none"
                : string.Join(" | ", Domains.Select(domain => domain.FormatSummary()).Take(16));
            string receiptSummary = Receipts.Count == 0
                ? "none"
                : string.Join(" || ", Receipts.Skip(Math.Max(0, Receipts.Count - 16)).Select(receipt => receipt.FormatSummary()));
            return Bound(
                "nextDirtyGeneration=" + NextDirtyGeneration +
                "; domains=" + Domains.Count +
                "; pending=" + Domains.Count(domain => domain.Dirty) +
                "; inFlight=" + Domains.Count(domain => domain.InFlight) +
                "; receipts=" + Receipts.Count +
                "; domainStates={" + domainSummary + "}" +
                "; latestReceipts={" + receiptSummary + "}",
                4096);
        }

        private static string Bound(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }
    }

    internal readonly struct ContentRefreshReceipt
    {
        public ContentRefreshReceipt(
            string domain,
            string ownerId,
            long dirtyGeneration,
            ContentRefreshCompletionStatus status,
            long previousGeneration,
            long currentGeneration,
            long lastGoodGeneration,
            IReadOnlyList<string> reasons,
            string details)
        {
            Domain = ContentRefreshDiagnosticBounds.Identity(domain, 64, "unknown");
            OwnerId = ContentRefreshDiagnosticBounds.Identity(ownerId, 128, "all");
            DirtyGeneration = dirtyGeneration;
            Status = status;
            PreviousGeneration = previousGeneration;
            CurrentGeneration = currentGeneration;
            LastGoodGeneration = lastGoodGeneration;
            Reasons = (reasons ?? Array.Empty<string>())
                .Take(16)
                .Select(reason => ContentRefreshDiagnosticBounds.Text(reason, 192))
                .ToArray();
            Details = ContentRefreshDiagnosticBounds.Text(details, 512);
        }

        public string Domain { get; }
        public string OwnerId { get; }
        public long DirtyGeneration { get; }
        public ContentRefreshCompletionStatus Status { get; }
        public long PreviousGeneration { get; }
        public long CurrentGeneration { get; }
        public long LastGoodGeneration { get; }
        public IReadOnlyList<string> Reasons { get; }
        public string Details { get; }

        public string FormatSummary()
        {
            return Bound(
                "domain=" + Domain +
                "; owner=" + OwnerId +
                "; dirtyGeneration=" + DirtyGeneration +
                "; status=" + Status +
                "; previous=" + PreviousGeneration +
                "; current=" + CurrentGeneration +
                "; lastGood=" + LastGoodGeneration +
                "; reasons=" + (Reasons.Count == 0 ? "none" : string.Join("|", Reasons)) +
                "; details=" + Details,
                1024);
        }

        private static string Bound(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }
    }

    /// <summary>
    /// Records explicit source-change demand. It intentionally owns no file or Unity work:
    /// consumers build candidates at their reviewed safe boundary and complete the batch with
    /// per-owner commit/reject/remove receipts.
    /// </summary>
    internal sealed class ContentRefreshGenerationService
    {
        private const int MaxReasonsPerBatch = 16;
        private const int MaxOwnersPerBatch = 256;
        private const int MaxAuthoritativeOwnerLength = 128;
        private const int MaxRawOwnerInputsPerBatch = 4096;
        private const int MaxReceipts = 256;
        private const int MaxNamedCompletionReceiptsPerGeneration = MaxReceipts - 1;
        private readonly object gate = new object();
        private readonly Dictionary<string, DomainState> states = new Dictionary<string, DomainState>(StringComparer.OrdinalIgnoreCase);
        private Queue<ContentRefreshReceipt> receipts = new Queue<ContentRefreshReceipt>(MaxReceipts);
        private long nextDirtyGeneration;

        public void MarkDirty(string domain, IEnumerable<string> ownerIds, string reason)
        {
            MarkDirtyBounded(domain, BuildBoundedOwnerInput(ownerIds), reason);
        }

        private void MarkDirtyBounded(string domain, IEnumerable<string> ownerIds, string reason)
        {
            string normalizedDomain = NormalizeRequired(domain, nameof(domain));
            string normalizedReason = Bound(reason, 192);
            lock (gate)
            {
                if (!states.TryGetValue(normalizedDomain, out DomainState? state))
                {
                    state = new DomainState(normalizedDomain);
                    states[normalizedDomain] = state;
                }

                if (state.InFlight)
                {
                    if (!state.NextDirty)
                    {
                        state.NextDirty = true;
                        state.NextGeneration = checked(++nextDirtyGeneration);
                        state.NextOwnerIds.Clear();
                        state.NextReasons.Clear();
                    }

                    MergeDirty(state.NextOwnerIds, state.NextReasons, ownerIds, normalizedReason);
                    return;
                }

                if (!state.Dirty)
                {
                    state.Dirty = true;
                    state.Generation = checked(++nextDirtyGeneration);
                    state.OwnerIds.Clear();
                    state.Reasons.Clear();
                }

                MergeDirty(state.OwnerIds, state.Reasons, ownerIds, normalizedReason);
            }
        }

        public void MarkDirty(IEnumerable<string> domains, IEnumerable<string> ownerIds, string reason)
        {
            string[] owners = BuildBoundedOwnerInput(ownerIds);
            foreach (string domain in domains ?? Array.Empty<string>())
                MarkDirtyBounded(domain, owners, reason);
        }

        public bool TryGetDirty(string domain, out ContentRefreshDirtyBatch batch)
        {
            string normalizedDomain = (domain ?? string.Empty).Trim();
            lock (gate)
            {
                if (!states.TryGetValue(normalizedDomain, out DomainState? state) || !state.Dirty || state.InFlight)
                {
                    batch = default;
                    return false;
                }

                state.InFlight = true;
                batch = new ContentRefreshDirtyBatch(
                    state.Domain,
                    state.Generation,
                    state.OwnerIds.OrderBy(owner => owner, StringComparer.OrdinalIgnoreCase).ToArray(),
                    state.Reasons.ToArray());
                return true;
            }
        }

        public bool IsDirty(string domain)
        {
            string normalizedDomain = (domain ?? string.Empty).Trim();
            lock (gate)
            {
                return states.TryGetValue(normalizedDomain, out DomainState? state) &&
                    (state.Dirty || state.NextDirty);
            }
        }

        public bool Complete(ContentRefreshDirtyBatch batch, IEnumerable<ContentRefreshCompletion> completions)
        {
            return CompleteWithAtomicCommit(batch, completions, null);
        }

        /// <summary>
        /// Closes a matching dirty generation and publishes its consumer snapshot as one
        /// serialized commit. The callback must only swap already-prepared, consumer-owned
        /// state: it runs while the generation gate is held, after every fallible receipt and
        /// successor projection has been prepared, and before either projection becomes
        /// observable. If validation fails, the callback is never invoked. If the callback
        /// throws before publishing, the generation remains in flight so the caller can
        /// abandon and requeue it.
        /// </summary>
        public bool CompleteWithAtomicCommit(
            ContentRefreshDirtyBatch batch,
            IEnumerable<ContentRefreshCompletion> completions,
            Action? publishPreparedSnapshot)
        {
            lock (gate)
            {
                if (!states.TryGetValue(batch.Domain, out DomainState? state) ||
                    !state.Dirty ||
                    !state.InFlight ||
                    state.Generation != batch.Generation)
                {
                    return false;
                }

                string[] reasons = state.Reasons.ToArray();
                ContentRefreshReceipt[] terminalReceipts = BuildTerminalReceipts(state, reasons, completions);
                var nextReceipts = new Queue<ContentRefreshReceipt>(MaxReceipts);
                int receiptsToSkip = Math.Max(0, receipts.Count + terminalReceipts.Length - MaxReceipts);
                int receiptIndex = 0;
                foreach (ContentRefreshReceipt receipt in receipts)
                {
                    if (receiptIndex++ >= receiptsToSkip)
                        nextReceipts.Enqueue(receipt);
                }
                foreach (ContentRefreshReceipt receipt in terminalReceipts)
                    nextReceipts.Enqueue(receipt);

                bool promoteSuccessor = state.NextDirty;
                long nextGeneration = promoteSuccessor ? state.NextGeneration : state.Generation;
                var nextOwnerIds = promoteSuccessor
                    ? new HashSet<string>(state.NextOwnerIds, StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var nextReasons = promoteSuccessor
                    ? new List<string>(state.NextReasons)
                    : new List<string>();

                // This callback is deliberately restricted to a prepared reference/primitive
                // swap and must not call back into this service. The generation gate makes the
                // terminal receipts and their corresponding live snapshot one authority edge.
                publishPreparedSnapshot?.Invoke();

                receipts = nextReceipts;
                state.InFlight = false;
                state.Dirty = promoteSuccessor;
                state.Generation = nextGeneration;
                state.OwnerIds = nextOwnerIds;
                state.Reasons = nextReasons;
                state.NextDirty = false;
                state.NextGeneration = 0;
                state.NextOwnerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                state.NextReasons = new List<string>();
                return true;
            }
        }

        private static ContentRefreshReceipt[] BuildTerminalReceipts(
            DomainState state,
            IReadOnlyList<string> reasons,
            IEnumerable<ContentRefreshCompletion> completions)
        {
            var preparedReceipts = new List<ContentRefreshReceipt>();
            int completionCount = 0;
            int trimmedCount = 0;
            int trimmedCommitted = 0;
            int trimmedRejected = 0;
            int trimmedRemoved = 0;
            int trimmedUnchanged = 0;
            foreach (ContentRefreshCompletion completion in completions ?? Array.Empty<ContentRefreshCompletion>())
            {
                completionCount++;
                if (completionCount <= MaxNamedCompletionReceiptsPerGeneration)
                {
                    preparedReceipts.Add(new ContentRefreshReceipt(
                        state.Domain,
                        completion.OwnerId,
                        state.Generation,
                        completion.Status,
                        completion.PreviousGeneration,
                        completion.CurrentGeneration,
                        completion.LastGoodGeneration,
                        reasons,
                        completion.Details));
                    continue;
                }

                trimmedCount++;
                switch (completion.Status)
                {
                    case ContentRefreshCompletionStatus.Committed:
                        trimmedCommitted++;
                        break;
                    case ContentRefreshCompletionStatus.Rejected:
                        trimmedRejected++;
                        break;
                    case ContentRefreshCompletionStatus.Removed:
                        trimmedRemoved++;
                        break;
                    default:
                        trimmedUnchanged++;
                        break;
                }
            }

            if (completionCount == 0)
            {
                preparedReceipts.Add(new ContentRefreshReceipt(
                    state.Domain,
                    "all",
                    state.Generation,
                    ContentRefreshCompletionStatus.Unchanged,
                    0,
                    0,
                    0,
                    reasons,
                    "No consumer outcome was supplied."));
            }
            else if (trimmedCount > 0)
            {
                preparedReceipts.Add(new ContentRefreshReceipt(
                    state.Domain,
                    "all",
                    state.Generation,
                    ContentRefreshCompletionStatus.Trimmed,
                    0,
                    0,
                    0,
                    reasons,
                    "trimmedCompletions=" + trimmedCount +
                    "; committed=" + trimmedCommitted +
                    "; rejected=" + trimmedRejected +
                    "; removed=" + trimmedRemoved +
                    "; unchanged=" + trimmedUnchanged +
                    "; namedReceiptCapacity=" + MaxNamedCompletionReceiptsPerGeneration));
            }

            return preparedReceipts.ToArray();
        }

        public bool AbandonAndRequeue(ContentRefreshDirtyBatch batch, string details)
        {
            lock (gate)
            {
                if (!states.TryGetValue(batch.Domain, out DomainState? state) ||
                    !state.Dirty ||
                    !state.InFlight ||
                    state.Generation != batch.Generation)
                {
                    return false;
                }

                string boundedDetails = Bound(details, 512);
                AddReceiptUnsafe(new ContentRefreshReceipt(
                    state.Domain,
                    "all",
                    state.Generation,
                    ContentRefreshCompletionStatus.Rejected,
                    0,
                    0,
                    0,
                    state.Reasons.ToArray(),
                    "Unexpected consumer failure; generation closed and successor requeued. " + boundedDetails));

                if (state.NextDirty)
                {
                    MergeDirty(state.OwnerIds, state.Reasons, state.NextOwnerIds, string.Empty);
                    foreach (string nextReason in state.NextReasons)
                    {
                        if (state.Reasons.Count >= MaxReasonsPerBatch)
                            break;
                        if (!state.Reasons.Contains(nextReason, StringComparer.Ordinal))
                            state.Reasons.Add(nextReason);
                    }
                    state.Generation = state.NextGeneration;
                }
                else
                {
                    state.Generation = checked(++nextDirtyGeneration);
                }

                string retryReason = Bound(
                    "successor after abandoned generation " + batch.Generation + ": " + boundedDetails,
                    192);
                if (!state.Reasons.Contains(retryReason, StringComparer.Ordinal))
                {
                    if (state.Reasons.Count < MaxReasonsPerBatch)
                        state.Reasons.Add(retryReason);
                    else if (state.Reasons.Count > 0)
                        state.Reasons[state.Reasons.Count - 1] = retryReason;
                }

                state.InFlight = false;
                state.Dirty = true;
                state.NextDirty = false;
                state.NextGeneration = 0;
                state.NextOwnerIds.Clear();
                state.NextReasons.Clear();
                return true;
            }
        }

        public IReadOnlyList<ContentRefreshReceipt> GetReceipts()
        {
            lock (gate)
                return receipts.ToArray();
        }

        public ContentRefreshGenerationSnapshot GetSnapshot()
        {
            lock (gate)
            {
                ContentRefreshDomainSnapshot[] domainSnapshots = states.Values
                    .OrderBy(state => state.Domain, StringComparer.OrdinalIgnoreCase)
                    .Select(state => new ContentRefreshDomainSnapshot(
                        state.Domain,
                        state.Dirty,
                        state.InFlight,
                        state.Generation,
                        state.OwnerIds.Count,
                        state.Reasons.Count,
                        state.NextDirty,
                        state.NextGeneration))
                    .ToArray();
                return new ContentRefreshGenerationSnapshot(nextDirtyGeneration, domainSnapshots, receipts.ToArray());
            }
        }

        internal long NextDirtyGenerationForTest
        {
            get
            {
                lock (gate)
                    return nextDirtyGeneration;
            }
        }

        private static string NormalizeRequired(string value, string parameterName)
        {
            string normalized = (value ?? string.Empty).Trim();
            if (normalized.Length == 0)
                throw new ArgumentException("A content refresh domain is required.", parameterName);
            return ContentRefreshDiagnosticBounds.Identity(normalized, 64, "unknown");
        }

        private static string[] BuildBoundedOwnerInput(IEnumerable<string> ownerIds)
        {
            var owners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int scanned = 0;
            foreach (string ownerId in ownerIds ?? Array.Empty<string>())
            {
                scanned++;
                if (scanned > MaxRawOwnerInputsPerBatch)
                {
                    // A hostile or unexpectedly repetitive producer must not make
                    // this diagnostic projection unbounded. Conservatively refresh
                    // all owners instead of silently dropping a late distinct owner.
                    return new[] { "all" };
                }

                if (string.IsNullOrWhiteSpace(ownerId))
                    continue;

                // Owner IDs select authoritative content state. They must never use
                // the bounded/hash display identity because consumers compare them
                // with the complete Manifest.UniqueID. If an identity cannot fit the
                // named projection, refresh all owners instead of silently missing it.
                if (ownerId.Length > MaxAuthoritativeOwnerLength ||
                    string.Equals(ownerId, "all", StringComparison.OrdinalIgnoreCase))
                {
                    return new[] { "all" };
                }

                if (!owners.Add(ownerId))
                    continue;
                if (owners.Count > MaxOwnersPerBatch)
                    return new[] { "all" };
            }

            return owners.ToArray();
        }

        private static void MergeDirty(HashSet<string> owners, List<string> reasons, IEnumerable<string> ownerIds, string reason)
        {
            bool addedOwner = false;
            foreach (string ownerId in ownerIds ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(ownerId))
                    continue;

                if (ownerId.Length > MaxAuthoritativeOwnerLength ||
                    string.Equals(ownerId, "all", StringComparison.OrdinalIgnoreCase))
                {
                    owners.Clear();
                    owners.Add("all");
                    addedOwner = true;
                    break;
                }
                if (owners.Contains("all"))
                {
                    addedOwner = true;
                    break;
                }
                if (!owners.Add(ownerId))
                {
                    addedOwner = true;
                    continue;
                }
                if (owners.Count > MaxOwnersPerBatch)
                {
                    owners.Clear();
                    owners.Add("all");
                    addedOwner = true;
                    break;
                }
                addedOwner = true;
            }

            if (!addedOwner && owners.Count == 0)
                owners.Add("all");
            if (reason.Length > 0 && reasons.Count < MaxReasonsPerBatch && !reasons.Contains(reason, StringComparer.Ordinal))
                reasons.Add(reason);
        }

        private static string Bound(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maxLength ? normalized : normalized.Substring(0, maxLength);
        }

        private void AddReceiptUnsafe(ContentRefreshReceipt receipt)
        {
            receipts.Enqueue(receipt);
            while (receipts.Count > MaxReceipts)
                receipts.Dequeue();
        }

        private sealed class DomainState
        {
            public DomainState(string domain)
            {
                Domain = domain;
            }

            public string Domain { get; }
            public bool Dirty { get; set; }
            public bool InFlight { get; set; }
            public long Generation { get; set; }
            public HashSet<string> OwnerIds { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public List<string> Reasons { get; set; } = new List<string>();
            public bool NextDirty { get; set; }
            public long NextGeneration { get; set; }
            public HashSet<string> NextOwnerIds { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public List<string> NextReasons { get; set; } = new List<string>();
        }
    }
}
