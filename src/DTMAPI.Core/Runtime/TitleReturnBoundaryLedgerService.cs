using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DTMAPI.Core.Runtime
{
    internal sealed class TitleReturnBoundaryLedgerService
    {
        private const int MaxEvents = 128;
        private const int MaxSnapshots = 64;
        private const int MaxDeltaSnapshots = 64;
        private readonly object gate = new object();
        private readonly Queue<TitleReturnBoundaryEventEntry> events = new Queue<TitleReturnBoundaryEventEntry>();
        private readonly Queue<TitleReturnObjectGraphSnapshot> objectGraphSnapshots = new Queue<TitleReturnObjectGraphSnapshot>();
        private readonly Queue<TitleReturnObjectGraphDeltaEntry> objectGraphDeltas = new Queue<TitleReturnObjectGraphDeltaEntry>();
        private int nextBoundaryNumber = 1;
        private int nextSequence = 1;
        private string currentBoundaryId = string.Empty;
        private bool currentBoundaryClosed = true;

        public TitleReturnBoundarySnapshot RecordEvent(
            string kind,
            string source,
            string details,
            int? slot,
            string runtimePhase,
            string inputContext,
            string saveLoadSummary,
            string resourceLifecycleSummary,
            string modOwnerSummary,
            int threadId)
        {
            lock (gate)
            {
                string safeKind = kind ?? string.Empty;
                if (ShouldStartBoundary(safeKind))
                {
                    currentBoundaryId = CreateBoundaryIdNoLock();
                    currentBoundaryClosed = false;
                }

                var entry = new TitleReturnBoundaryEventEntry(
                    nextSequence++,
                    currentBoundaryId,
                    safeKind,
                    source ?? string.Empty,
                    details ?? string.Empty,
                    slot,
                    runtimePhase ?? string.Empty,
                    inputContext ?? string.Empty,
                    saveLoadSummary ?? string.Empty,
                    resourceLifecycleSummary ?? string.Empty,
                    modOwnerSummary ?? string.Empty,
                    threadId,
                    DateTimeOffset.Now);
                events.Enqueue(entry);
                while (events.Count > MaxEvents)
                    events.Dequeue();

                if (IsBoundaryClosingEvent(safeKind))
                    currentBoundaryClosed = true;

                return BuildSnapshotNoLock();
            }
        }

        public TitleReturnBoundarySnapshot RecordObjectGraphSnapshot(
            string boundary,
            string reason,
            string source,
            int? slot,
            string runtimePhase,
            string inputContext,
            string saveLoadSummary,
            string resourceLifecycleSummary,
            string modOwnerSummary,
            IReadOnlyList<TitleReturnObjectGraphSection> sections)
        {
            lock (gate)
            {
                if (string.IsNullOrWhiteSpace(currentBoundaryId))
                {
                    currentBoundaryId = CreateBoundaryIdNoLock();
                    currentBoundaryClosed = false;
                }

                var snapshot = new TitleReturnObjectGraphSnapshot(
                    nextSequence++,
                    currentBoundaryId,
                    boundary ?? string.Empty,
                    reason ?? string.Empty,
                    source ?? string.Empty,
                    slot,
                    runtimePhase ?? string.Empty,
                    inputContext ?? string.Empty,
                    saveLoadSummary ?? string.Empty,
                    resourceLifecycleSummary ?? string.Empty,
                    modOwnerSummary ?? string.Empty,
                    sections ?? Array.Empty<TitleReturnObjectGraphSection>(),
                    DateTimeOffset.Now);

                TitleReturnObjectGraphSnapshot? previousInBoundary = objectGraphSnapshots
                    .Reverse()
                    .FirstOrDefault(item => item.BoundaryId.Equals(snapshot.BoundaryId, StringComparison.OrdinalIgnoreCase));
                TitleReturnObjectGraphSnapshot? previousSameBoundary = objectGraphSnapshots
                    .Reverse()
                    .FirstOrDefault(item => item.Boundary.Equals(snapshot.Boundary, StringComparison.OrdinalIgnoreCase));
                TitleReturnObjectGraphDeltaEntry delta = BuildDeltaNoLock(snapshot, previousInBoundary, previousSameBoundary);

                objectGraphSnapshots.Enqueue(snapshot);
                while (objectGraphSnapshots.Count > MaxSnapshots)
                    objectGraphSnapshots.Dequeue();

                objectGraphDeltas.Enqueue(delta);
                while (objectGraphDeltas.Count > MaxDeltaSnapshots)
                    objectGraphDeltas.Dequeue();

                return BuildSnapshotNoLock();
            }
        }

        public TitleReturnBoundarySnapshot GetSnapshot()
        {
            lock (gate)
                return BuildSnapshotNoLock();
        }

        public string GetCurrentBoundaryIdForDiagnostics()
        {
            lock (gate)
                return currentBoundaryId;
        }

        private bool ShouldStartBoundary(string kind)
        {
            if (kind.Equals("ReturnHomeRequested", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.IsNullOrWhiteSpace(currentBoundaryId))
                return true;

            if (currentBoundaryClosed &&
                (kind.Equals("LoadGameNativeEnter", StringComparison.OrdinalIgnoreCase) ||
                 kind.Equals("BeforeNextLoadGame", StringComparison.OrdinalIgnoreCase)))
                return true;

            return false;
        }

        private static bool IsBoundaryClosingEvent(string kind)
        {
            return kind.Equals("SaveLoaded", StringComparison.OrdinalIgnoreCase);
        }

        private string CreateBoundaryIdNoLock()
        {
            string id = "TR-" + nextBoundaryNumber.ToString("0000", CultureInfo.InvariantCulture);
            nextBoundaryNumber++;
            return id;
        }

        private TitleReturnBoundarySnapshot BuildSnapshotNoLock()
        {
            TitleReturnBoundaryEventEntry? latestEvent = events.Count == 0 ? null : events.Last();
            TitleReturnObjectGraphSnapshot? latestObjectGraph = objectGraphSnapshots.Count == 0 ? null : objectGraphSnapshots.Last();
            return new TitleReturnBoundarySnapshot(
                currentBoundaryId,
                currentBoundaryClosed,
                events.ToArray(),
                objectGraphSnapshots.ToArray(),
                objectGraphDeltas.ToArray(),
                latestEvent,
                latestObjectGraph,
                objectGraphDeltas.Count == 0 ? null : objectGraphDeltas.Last());
        }

        private static TitleReturnObjectGraphDeltaEntry BuildDeltaNoLock(
            TitleReturnObjectGraphSnapshot snapshot,
            TitleReturnObjectGraphSnapshot? previousInBoundary,
            TitleReturnObjectGraphSnapshot? previousSameBoundary)
        {
            IReadOnlyDictionary<string, long> metrics = TitleReturnObjectGraphMetricExtractor.Extract(snapshot);
            IReadOnlyDictionary<string, long> previousInBoundaryMetrics = previousInBoundary == null
                ? new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
                : TitleReturnObjectGraphMetricExtractor.Extract(previousInBoundary);
            IReadOnlyDictionary<string, long> previousSameBoundaryMetrics = previousSameBoundary == null
                ? new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
                : TitleReturnObjectGraphMetricExtractor.Extract(previousSameBoundary);

            return new TitleReturnObjectGraphDeltaEntry(
                snapshot.Sequence,
                snapshot.BoundaryId,
                snapshot.Boundary,
                snapshot.Reason,
                snapshot.Slot,
                snapshot.RuntimePhase,
                snapshot.InputContext,
                metrics,
                BuildMetricDeltas(previousInBoundaryMetrics, metrics),
                BuildMetricDeltas(previousSameBoundaryMetrics, metrics),
                previousInBoundary?.Boundary ?? string.Empty,
                previousSameBoundary?.BoundaryId ?? string.Empty,
                snapshot.Timestamp);
        }

        private static IReadOnlyList<TitleReturnObjectGraphMetricDelta> BuildMetricDeltas(
            IReadOnlyDictionary<string, long> previous,
            IReadOnlyDictionary<string, long> current)
        {
            var keys = new HashSet<string>(previous.Keys, StringComparer.OrdinalIgnoreCase);
            foreach (string key in current.Keys)
                keys.Add(key);

            var deltas = new List<TitleReturnObjectGraphMetricDelta>();
            foreach (string key in keys.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
            {
                long previousValue = previous.TryGetValue(key, out long existing) ? existing : 0;
                long currentValue = current.TryGetValue(key, out long observed) ? observed : 0;
                long delta = currentValue - previousValue;
                if (delta != 0)
                    deltas.Add(new TitleReturnObjectGraphMetricDelta(key, previousValue, currentValue, delta));
            }

            return deltas;
        }
    }

    internal sealed class TitleReturnObjectGraphSection
    {
        public TitleReturnObjectGraphSection(string name, string summary)
        {
            Name = name ?? string.Empty;
            Summary = summary ?? string.Empty;
        }

        public string Name { get; }
        public string Summary { get; }

        public string Format()
        {
            return SingleLine(Name) + "={" + SingleLine(Summary) + "}";
        }

        private static string SingleLine(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
        }
    }

    internal sealed class TitleReturnBoundarySnapshot
    {
        public TitleReturnBoundarySnapshot(
            string currentBoundaryId,
            bool currentBoundaryClosed,
            IReadOnlyList<TitleReturnBoundaryEventEntry> events,
            IReadOnlyList<TitleReturnObjectGraphSnapshot> objectGraphSnapshots,
            IReadOnlyList<TitleReturnObjectGraphDeltaEntry> objectGraphDeltas,
            TitleReturnBoundaryEventEntry? latestEvent,
            TitleReturnObjectGraphSnapshot? latestObjectGraph,
            TitleReturnObjectGraphDeltaEntry? latestObjectGraphDelta)
        {
            CurrentBoundaryId = currentBoundaryId ?? string.Empty;
            CurrentBoundaryClosed = currentBoundaryClosed;
            Events = events ?? Array.Empty<TitleReturnBoundaryEventEntry>();
            ObjectGraphSnapshots = objectGraphSnapshots ?? Array.Empty<TitleReturnObjectGraphSnapshot>();
            ObjectGraphDeltas = objectGraphDeltas ?? Array.Empty<TitleReturnObjectGraphDeltaEntry>();
            LatestEvent = latestEvent;
            LatestObjectGraph = latestObjectGraph;
            LatestObjectGraphDelta = latestObjectGraphDelta;
        }

        public string CurrentBoundaryId { get; }
        public bool CurrentBoundaryClosed { get; }
        public IReadOnlyList<TitleReturnBoundaryEventEntry> Events { get; }
        public IReadOnlyList<TitleReturnObjectGraphSnapshot> ObjectGraphSnapshots { get; }
        public IReadOnlyList<TitleReturnObjectGraphDeltaEntry> ObjectGraphDeltas { get; }
        public TitleReturnBoundaryEventEntry? LatestEvent { get; }
        public TitleReturnObjectGraphSnapshot? LatestObjectGraph { get; }
        public TitleReturnObjectGraphDeltaEntry? LatestObjectGraphDelta { get; }
        public string Status => ObjectGraphSnapshots.Count > 0 ? "observing" : "pending";

        public string FormatSummary()
        {
            return "status=" + Status +
                "; currentBoundary=" + (string.IsNullOrWhiteSpace(CurrentBoundaryId) ? "none" : CurrentBoundaryId) +
                "; closed=" + (CurrentBoundaryClosed ? "true" : "false") +
                "; events=" + Events.Count.ToString(CultureInfo.InvariantCulture) +
                "; objectSnapshots=" + ObjectGraphSnapshots.Count.ToString(CultureInfo.InvariantCulture) +
                "; objectDeltas=" + ObjectGraphDeltas.Count.ToString(CultureInfo.InvariantCulture) +
                "; latestEvent=" + (LatestEvent == null ? "none" : LatestEvent.FormatShort()) +
                "; latestSnapshot=" + (LatestObjectGraph == null ? "none" : LatestObjectGraph.FormatShort()) +
                "; latestDelta=" + (LatestObjectGraphDelta == null ? "none" : LatestObjectGraphDelta.FormatShort());
        }

        public string FormatLatestSnapshots(int maxCount = 4)
        {
            if (ObjectGraphSnapshots.Count == 0)
                return "none";

            return string.Join(" | ", ObjectGraphSnapshots
                .Skip(Math.Max(0, ObjectGraphSnapshots.Count - Math.Max(1, maxCount)))
                .Select(snapshot => snapshot.Format())
                .ToArray());
        }

        public string FormatLatestDeltas(int maxCount = 4)
        {
            if (ObjectGraphDeltas.Count == 0)
                return "none";

            return string.Join(" | ", ObjectGraphDeltas
                .Skip(Math.Max(0, ObjectGraphDeltas.Count - Math.Max(1, maxCount)))
                .Select(delta => delta.Format())
                .ToArray());
        }
    }

    internal sealed class TitleReturnBoundaryEventEntry
    {
        public TitleReturnBoundaryEventEntry(
            int sequence,
            string boundaryId,
            string kind,
            string source,
            string details,
            int? slot,
            string runtimePhase,
            string inputContext,
            string saveLoadSummary,
            string resourceLifecycleSummary,
            string modOwnerSummary,
            int threadId,
            DateTimeOffset timestamp)
        {
            Sequence = sequence;
            BoundaryId = boundaryId ?? string.Empty;
            Kind = kind ?? string.Empty;
            Source = source ?? string.Empty;
            Details = details ?? string.Empty;
            Slot = slot;
            RuntimePhase = runtimePhase ?? string.Empty;
            InputContext = inputContext ?? string.Empty;
            SaveLoadSummary = saveLoadSummary ?? string.Empty;
            ResourceLifecycleSummary = resourceLifecycleSummary ?? string.Empty;
            ModOwnerSummary = modOwnerSummary ?? string.Empty;
            ThreadId = threadId;
            Timestamp = timestamp;
        }

        public int Sequence { get; }
        public string BoundaryId { get; }
        public string Kind { get; }
        public string Source { get; }
        public string Details { get; }
        public int? Slot { get; }
        public string RuntimePhase { get; }
        public string InputContext { get; }
        public string SaveLoadSummary { get; }
        public string ResourceLifecycleSummary { get; }
        public string ModOwnerSummary { get; }
        public int ThreadId { get; }
        public DateTimeOffset Timestamp { get; }

        public string FormatShort()
        {
            return Sequence.ToString(CultureInfo.InvariantCulture) +
                ":" + SingleLine(Kind) +
                ":" + (string.IsNullOrWhiteSpace(BoundaryId) ? "none" : BoundaryId) +
                ":slot=" + (Slot.HasValue ? Slot.Value.ToString(CultureInfo.InvariantCulture) : "unknown") +
                ":phase=" + SingleLine(RuntimePhase) +
                ":input=" + SingleLine(InputContext);
        }

        private static string SingleLine(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace(";", ",").Trim();
        }
    }

    internal sealed class TitleReturnObjectGraphSnapshot
    {
        public TitleReturnObjectGraphSnapshot(
            int sequence,
            string boundaryId,
            string boundary,
            string reason,
            string source,
            int? slot,
            string runtimePhase,
            string inputContext,
            string saveLoadSummary,
            string resourceLifecycleSummary,
            string modOwnerSummary,
            IReadOnlyList<TitleReturnObjectGraphSection> sections,
            DateTimeOffset timestamp)
        {
            Sequence = sequence;
            BoundaryId = boundaryId ?? string.Empty;
            Boundary = boundary ?? string.Empty;
            Reason = reason ?? string.Empty;
            Source = source ?? string.Empty;
            Slot = slot;
            RuntimePhase = runtimePhase ?? string.Empty;
            InputContext = inputContext ?? string.Empty;
            SaveLoadSummary = saveLoadSummary ?? string.Empty;
            ResourceLifecycleSummary = resourceLifecycleSummary ?? string.Empty;
            ModOwnerSummary = modOwnerSummary ?? string.Empty;
            Sections = sections ?? Array.Empty<TitleReturnObjectGraphSection>();
            Timestamp = timestamp;
        }

        public int Sequence { get; }
        public string BoundaryId { get; }
        public string Boundary { get; }
        public string Reason { get; }
        public string Source { get; }
        public int? Slot { get; }
        public string RuntimePhase { get; }
        public string InputContext { get; }
        public string SaveLoadSummary { get; }
        public string ResourceLifecycleSummary { get; }
        public string ModOwnerSummary { get; }
        public IReadOnlyList<TitleReturnObjectGraphSection> Sections { get; }
        public DateTimeOffset Timestamp { get; }

        public string FormatShort()
        {
            return Sequence.ToString(CultureInfo.InvariantCulture) +
                ":" + SingleLine(Boundary) +
                ":" + (string.IsNullOrWhiteSpace(BoundaryId) ? "none" : BoundaryId) +
                ":sections=" + Sections.Count.ToString(CultureInfo.InvariantCulture) +
                ":input=" + SingleLine(InputContext);
        }

        public string Format()
        {
            return FormatShort() +
                "; reason=" + SingleLine(Reason) +
                "; saveLoad={" + SingleLine(SaveLoadSummary) + "}" +
                "; resourceLifecycle={" + SingleLine(ResourceLifecycleSummary) + "}" +
                "; modOwner={" + SingleLine(ModOwnerSummary) + "}" +
                "; " + string.Join("; ", Sections.Select(section => section.Format()).ToArray());
        }

        private static string SingleLine(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace(";", ",").Trim();
        }
    }

    internal sealed class TitleReturnObjectGraphDeltaEntry
    {
        public TitleReturnObjectGraphDeltaEntry(
            int sequence,
            string boundaryId,
            string boundary,
            string reason,
            int? slot,
            string runtimePhase,
            string inputContext,
            IReadOnlyDictionary<string, long> metrics,
            IReadOnlyList<TitleReturnObjectGraphMetricDelta> previousSnapshotDeltas,
            IReadOnlyList<TitleReturnObjectGraphMetricDelta> sameBoundaryDeltas,
            string previousSnapshotBoundary,
            string previousSameBoundaryId,
            DateTimeOffset timestamp)
        {
            Sequence = sequence;
            BoundaryId = boundaryId ?? string.Empty;
            Boundary = boundary ?? string.Empty;
            Reason = reason ?? string.Empty;
            Slot = slot;
            RuntimePhase = runtimePhase ?? string.Empty;
            InputContext = inputContext ?? string.Empty;
            Metrics = metrics ?? new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            PreviousSnapshotDeltas = previousSnapshotDeltas ?? Array.Empty<TitleReturnObjectGraphMetricDelta>();
            SameBoundaryDeltas = sameBoundaryDeltas ?? Array.Empty<TitleReturnObjectGraphMetricDelta>();
            PreviousSnapshotBoundary = previousSnapshotBoundary ?? string.Empty;
            PreviousSameBoundaryId = previousSameBoundaryId ?? string.Empty;
            Timestamp = timestamp;
        }

        public int Sequence { get; }
        public string BoundaryId { get; }
        public string Boundary { get; }
        public string Reason { get; }
        public int? Slot { get; }
        public string RuntimePhase { get; }
        public string InputContext { get; }
        public IReadOnlyDictionary<string, long> Metrics { get; }
        public IReadOnlyList<TitleReturnObjectGraphMetricDelta> PreviousSnapshotDeltas { get; }
        public IReadOnlyList<TitleReturnObjectGraphMetricDelta> SameBoundaryDeltas { get; }
        public string PreviousSnapshotBoundary { get; }
        public string PreviousSameBoundaryId { get; }
        public DateTimeOffset Timestamp { get; }

        public int NonZeroMetricCount => Metrics.Count(item => item.Value != 0);
        public int PreviousSnapshotGrowthCount => PreviousSnapshotDeltas.Count(item => item.Delta > 0);
        public int SameBoundaryGrowthCount => SameBoundaryDeltas.Count(item => item.Delta > 0);

        public string FormatShort()
        {
            return Sequence.ToString(CultureInfo.InvariantCulture) +
                ":" + SingleLine(Boundary) +
                ":" + (string.IsNullOrWhiteSpace(BoundaryId) ? "none" : BoundaryId) +
                ":metrics=" + Metrics.Count.ToString(CultureInfo.InvariantCulture) +
                ":nonZero=" + NonZeroMetricCount.ToString(CultureInfo.InvariantCulture) +
                ":prevChanges=" + PreviousSnapshotDeltas.Count.ToString(CultureInfo.InvariantCulture) +
                ":sameBoundaryChanges=" + SameBoundaryDeltas.Count.ToString(CultureInfo.InvariantCulture);
        }

        public string Format()
        {
            return FormatShort() +
                "; slot=" + (Slot.HasValue ? Slot.Value.ToString(CultureInfo.InvariantCulture) : "unknown") +
                "; phase=" + SingleLine(RuntimePhase) +
                "; input=" + SingleLine(InputContext) +
                "; previousSnapshot=" + (string.IsNullOrWhiteSpace(PreviousSnapshotBoundary) ? "none" : SingleLine(PreviousSnapshotBoundary)) +
                "; previousSameBoundary=" + (string.IsNullOrWhiteSpace(PreviousSameBoundaryId) ? "none" : SingleLine(PreviousSameBoundaryId)) +
                "; growthPrev=" + PreviousSnapshotGrowthCount.ToString(CultureInfo.InvariantCulture) +
                "; growthSameBoundary=" + SameBoundaryGrowthCount.ToString(CultureInfo.InvariantCulture) +
                "; prevDelta={" + FormatDeltas(PreviousSnapshotDeltas) + "}" +
                "; sameBoundaryDelta={" + FormatDeltas(SameBoundaryDeltas) + "}" +
                "; nonZero={" + FormatNonZeroMetrics(Metrics) + "}" +
                "; ownerPrevDelta={" + FormatOwnerDeltas(PreviousSnapshotDeltas) + "}" +
                "; ownerSameBoundaryDelta={" + FormatOwnerDeltas(SameBoundaryDeltas) + "}" +
                "; ownerNonZero={" + FormatOwnerNonZeroMetrics(Metrics) + "}";
        }

        private static string FormatDeltas(IReadOnlyList<TitleReturnObjectGraphMetricDelta> deltas, int maxCount = 16)
        {
            if (deltas.Count == 0)
                return "none";

            TitleReturnObjectGraphMetricDelta[] selected = deltas
                .OrderByDescending(delta => Math.Abs(delta.Delta))
                .ThenBy(delta => delta.Key, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Max(1, maxCount))
                .ToArray();
            string suffix = deltas.Count > selected.Length
                ? ", +" + (deltas.Count - selected.Length).ToString(CultureInfo.InvariantCulture) + " more"
                : string.Empty;
            return string.Join(", ", selected.Select(delta => delta.Format()).ToArray()) + suffix;
        }

        private static string FormatNonZeroMetrics(IReadOnlyDictionary<string, long> metrics, int maxCount = 20)
        {
            TitleReturnObjectGraphMetricDelta[] selected = metrics
                .Where(item => item.Value != 0)
                .OrderByDescending(item => Math.Abs(item.Value))
                .ThenBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Max(1, maxCount))
                .Select(item => new TitleReturnObjectGraphMetricDelta(item.Key, 0, item.Value, item.Value))
                .ToArray();
            int nonZeroCount = metrics.Count(item => item.Value != 0);
            if (selected.Length == 0)
                return "none";

            string suffix = nonZeroCount > selected.Length
                ? ", +" + (nonZeroCount - selected.Length).ToString(CultureInfo.InvariantCulture) + " more"
                : string.Empty;
            return string.Join(", ", selected.Select(item => item.Key + "=" + item.CurrentValue.ToString(CultureInfo.InvariantCulture)).ToArray()) + suffix;
        }

        private static string FormatOwnerDeltas(IReadOnlyList<TitleReturnObjectGraphMetricDelta> deltas, int maxCount = 20)
        {
            TitleReturnObjectGraphMetricDelta[] ownerDeltas = deltas
                .Where(delta => IsOwnerMetricKey(delta.Key))
                .ToArray();
            return FormatDeltas(ownerDeltas, maxCount);
        }

        private static string FormatOwnerNonZeroMetrics(IReadOnlyDictionary<string, long> metrics, int maxCount = 32)
        {
            var ownerMetrics = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, long> item in metrics)
            {
                if (IsOwnerMetricKey(item.Key))
                    ownerMetrics[item.Key] = item.Value;
            }

            return FormatNonZeroMetrics(ownerMetrics, maxCount);
        }

        private static bool IsOwnerMetricKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            return key.IndexOf(".byOwner.", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.IndexOf(".uiByOwner.", StringComparison.OrdinalIgnoreCase) >= 0 ||
                key.StartsWith("GameBridge.featureById.", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("BootstrapUi.titleSettings.rootAlive", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("BootstrapUi.debugConsole.rootAlive", StringComparison.OrdinalIgnoreCase);
        }

        private static string SingleLine(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace(";", ",").Trim();
        }
    }

    internal sealed class TitleReturnObjectGraphMetricDelta
    {
        public TitleReturnObjectGraphMetricDelta(string key, long previousValue, long currentValue, long delta)
        {
            Key = key ?? string.Empty;
            PreviousValue = previousValue;
            CurrentValue = currentValue;
            Delta = delta;
        }

        public string Key { get; }
        public long PreviousValue { get; }
        public long CurrentValue { get; }
        public long Delta { get; }

        public string Format()
        {
            string sign = Delta > 0 ? "+" : string.Empty;
            return Key + "=" +
                PreviousValue.ToString(CultureInfo.InvariantCulture) +
                "->" +
                CurrentValue.ToString(CultureInfo.InvariantCulture) +
                "(" + sign + Delta.ToString(CultureInfo.InvariantCulture) + ")";
        }
    }

    internal static class TitleReturnObjectGraphMetricExtractor
    {
        public static IReadOnlyDictionary<string, long> Extract(TitleReturnObjectGraphSnapshot snapshot)
        {
            var metrics = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            if (snapshot == null)
                return metrics;

            AddMetrics(metrics, "SaveLoadRequest", snapshot.SaveLoadSummary);
            AddMetrics(metrics, "ResourceLifecycle", snapshot.ResourceLifecycleSummary);
            AddMetrics(metrics, "ModOwner", snapshot.ModOwnerSummary);
            foreach (TitleReturnObjectGraphSection section in snapshot.Sections)
                AddMetrics(metrics, NormalizeSegment(section.Name), section.Summary);

            metrics["Snapshot.sections"] = snapshot.Sections.Count;
            return metrics;
        }

        private static void AddMetrics(Dictionary<string, long> metrics, string prefix, string text)
        {
            ParseAssignments(metrics, NormalizeSegment(prefix), text ?? string.Empty, 0, text?.Length ?? 0);
        }

        private static void ParseAssignments(Dictionary<string, long> metrics, string prefix, string text, int start, int end)
        {
            int index = start;
            while (index < end)
            {
                SkipSeparators(text, ref index, end);
                if (index >= end)
                    break;

                int keyStart = index;
                while (index < end && text[index] != '=' && text[index] != ';' && text[index] != ',')
                    index++;
                if (index >= end || text[index] != '=')
                {
                    index++;
                    continue;
                }

                string key = NormalizeSegment(text.Substring(keyStart, index - keyStart));
                index++;
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                SkipSpaces(text, ref index, end);
                if (index < end && text[index] == '{')
                {
                    int contentStart = index + 1;
                    int contentEnd = FindMatchingBrace(text, index, end);
                    if (contentEnd > contentStart)
                        ParseAssignments(metrics, Combine(prefix, key), text, contentStart, contentEnd);
                    index = contentEnd < end ? contentEnd + 1 : end;
                    continue;
                }

                int valueStart = index;
                while (index < end && text[index] != ';' && text[index] != ',')
                    index++;
                string value = text.Substring(valueStart, index - valueStart).Trim();
                if (TryParseMetricValue(value, out long metricValue))
                    metrics[Combine(prefix, key)] = metricValue;
            }
        }

        private static void SkipSeparators(string text, ref int index, int end)
        {
            while (index < end && (char.IsWhiteSpace(text[index]) || text[index] == ';' || text[index] == ','))
                index++;
        }

        private static void SkipSpaces(string text, ref int index, int end)
        {
            while (index < end && char.IsWhiteSpace(text[index]))
                index++;
        }

        private static int FindMatchingBrace(string text, int braceIndex, int end)
        {
            int depth = 0;
            for (int i = braceIndex; i < end; i++)
            {
                if (text[i] == '{')
                {
                    depth++;
                    continue;
                }

                if (text[i] != '}')
                    continue;

                depth--;
                if (depth == 0)
                    return i;
            }

            return end;
        }

        private static bool TryParseMetricValue(string value, out long result)
        {
            result = 0;
            string text = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                return true;

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
            {
                result = (long)Math.Round(doubleValue);
                return true;
            }

            if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                result = 1;
                return true;
            }

            if (text.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("none", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("null", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("not-created", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("missing", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("destroyed", StringComparison.OrdinalIgnoreCase))
            {
                result = 0;
                return true;
            }

            if (text.StartsWith("alive", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("ready", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("created", StringComparison.OrdinalIgnoreCase))
            {
                result = 1;
                return true;
            }

            return false;
        }

        private static string Combine(string prefix, string key)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return key;
            if (string.IsNullOrWhiteSpace(key))
                return prefix;
            return prefix + "." + key;
        }

        private static string NormalizeSegment(string value)
        {
            string text = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var builder = new System.Text.StringBuilder(text.Length);
            bool previousUnderscore = false;
            foreach (char ch in text)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(ch);
                    previousUnderscore = false;
                    continue;
                }

                if (ch == '_' || ch == '-' || ch == '.')
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
