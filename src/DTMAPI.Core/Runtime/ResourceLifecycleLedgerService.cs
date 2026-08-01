using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.Core.Runtime
{
    internal static class ResourceLifetime
    {
        public const string ProcessLifetime = "ProcessLifetime";
        public const string TitleLifetime = "TitleLifetime";
        public const string SaveLifetime = "SaveLifetime";

        public static string Normalize(string value)
        {
            if (string.Equals(value, ProcessLifetime, StringComparison.OrdinalIgnoreCase))
                return ProcessLifetime;
            if (string.Equals(value, TitleLifetime, StringComparison.OrdinalIgnoreCase))
                return TitleLifetime;
            if (string.Equals(value, SaveLifetime, StringComparison.OrdinalIgnoreCase))
                return SaveLifetime;
            return string.IsNullOrWhiteSpace(value) ? "UnknownLifetime" : value.Trim();
        }
    }

    internal static class ResourceOwnership
    {
        public const string DtmapiOwned = "DtmapiOwned";
        public const string NativeOwned = "NativeOwned";
        public const string BorrowedNative = "BorrowedNative";
        public const string ExternalModOwned = "ExternalModOwned";
        public const string Unknown = "Unknown";

        public static string Normalize(string value)
        {
            if (string.Equals(value, DtmapiOwned, StringComparison.OrdinalIgnoreCase))
                return DtmapiOwned;
            if (string.Equals(value, NativeOwned, StringComparison.OrdinalIgnoreCase))
                return NativeOwned;
            if (string.Equals(value, BorrowedNative, StringComparison.OrdinalIgnoreCase))
                return BorrowedNative;
            if (string.Equals(value, ExternalModOwned, StringComparison.OrdinalIgnoreCase))
                return ExternalModOwned;
            if (string.Equals(value, Unknown, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(value))
                return Unknown;
            return value.Trim();
        }
    }

    internal static class ResourceLifecycleStatus
    {
        public const string Declared = "declared";
        public const string Acquired = "acquired";
        public const string Ready = "ready";
        public const string Rebuilt = "rebuilt";
        public const string SkippedThrottle = "skipped-throttle";
        public const string SkippedUnchanged = "skipped-unchanged";
        public const string Released = "released";
        public const string DroppedManagedReference = "dropped-managed-ref";
        public const string Cleaned = "cleaned";

        public static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? Declared : value.Trim();
        }

        public static bool IsAcquireLike(string value)
        {
            string normalized = Normalize(value);
            return string.Equals(normalized, Acquired, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, Ready, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, Rebuilt, StringComparison.OrdinalIgnoreCase);
        }
    }

    internal enum ResourceLifecycleObservationMode
    {
        Detailed,
        AggregatedCurrentState
    }

    internal sealed class ResourceLifecycleLedgerService
    {
        private const int MaxTimeline = 160;
        private const int MaxDiagnostics = 96;
        private readonly object gate = new object();
        private readonly Dictionary<string, MutableResourceLifecycleRecord> records = new Dictionary<string, MutableResourceLifecycleRecord>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> refreshCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> publishCountsByArea = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> skippedRefreshFastPathKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<ResourceLifecycleTimelineEntry> timeline = new Queue<ResourceLifecycleTimelineEntry>();

        internal int CurrentRecordCount
        {
            get
            {
                lock (gate)
                    return records.Count;
            }
        }

        internal int CurrentSnapshotBuildCount
        {
            get
            {
                lock (gate)
                    return snapshotBuilds;
            }
        }
        private readonly Queue<ResourceLifecycleDiagnostic> diagnostics = new Queue<ResourceLifecycleDiagnostic>();
        private int processGeneration = 1;
        private int contentGeneration;
        private int saveGeneration;
        private bool saveGenerationOpen;
        private string currentPhase = RuntimeLifecyclePhase.Unknown;
        private string contentSignature = string.Empty;
        private string returnedToTitleCleanupPlan = "none";
        private int titleIdleResourceGrowthWarnings;
        private int prunedSaveLifetimeRecords;
        private int skippedRefreshCalls;
        private int skippedRefreshFastPathCount;
        private int snapshotBuilds;

        public ResourceLifecycleLedgerUpdate RecordPhase(string phase)
        {
            var newDiagnostics = new List<ResourceLifecycleDiagnostic>();
            lock (gate)
            {
                currentPhase = RuntimeLifecyclePhase.Normalize(phase);
                AddTimelineNoLock("phase", currentPhase, "processGeneration=" + processGeneration + "; contentGeneration=" + contentGeneration + "; saveGeneration=" + saveGeneration + "; saveOpen=" + saveGenerationOpen);
                return BuildUpdateNoLock(newDiagnostics, "phase");
            }
        }

        public ResourceLifecycleLedgerUpdate ObserveContentSignature(string reason, string signature)
        {
            var newDiagnostics = new List<ResourceLifecycleDiagnostic>();
            string normalizedReason = Normalize(reason, "unknown");
            string normalizedSignature = signature ?? string.Empty;
            lock (gate)
            {
                if (!string.Equals(contentSignature, normalizedSignature, StringComparison.Ordinal))
                {
                    contentGeneration++;
                    contentSignature = normalizedSignature;
                    AddTimelineNoLock("content-generation", currentPhase, "reason=" + normalizedReason + "; contentGeneration=" + contentGeneration + "; signatureHash=" + contentSignature.GetHashCode());
                }

                return BuildUpdateNoLock(newDiagnostics, "content");
            }
        }

        public ResourceLifecycleLedgerUpdate BeginSaveGeneration(string reason, int? saveSlot, bool? isNewGame)
        {
            var newDiagnostics = new List<ResourceLifecycleDiagnostic>();
            lock (gate)
            {
                saveGeneration++;
                saveGenerationOpen = true;
                AddTimelineNoLock(
                    "save-generation",
                    currentPhase,
                    "reason=" + Normalize(reason, "unknown") +
                    "; saveGeneration=" + saveGeneration +
                    "; saveSlot=" + FormatNullable(saveSlot) +
                    "; isNewGame=" + FormatNullable(isNewGame));
                return BuildUpdateNoLock(newDiagnostics, "save-generation");
            }
        }

        public ResourceLifecycleLedgerUpdate CloseSaveGeneration(string reason)
        {
            var newDiagnostics = new List<ResourceLifecycleDiagnostic>();
            lock (gate)
            {
                if (saveGenerationOpen)
                    AddTimelineNoLock("save-generation", currentPhase, "reason=" + Normalize(reason, "unknown") + "; saveGeneration=" + saveGeneration + "; closed=true");
                saveGenerationOpen = false;
                PruneClosedSaveLifetimeRecordsNoLock();
                return BuildUpdateNoLock(newDiagnostics, "save-generation");
            }
        }

        public ResourceLifecycleLedgerUpdate RecordResource(
            string resourceKind,
            string resourceId,
            string ownerId,
            string sourcePath,
            string lifetime,
            string ownership,
            string status,
            string releasePolicy,
            int generation = 0,
            ResourceLifecycleObservationMode observationMode = ResourceLifecycleObservationMode.Detailed,
            string aggregationKey = "")
        {
            var newDiagnostics = new List<ResourceLifecycleDiagnostic>();
            string normalizedLifetime = ResourceLifetime.Normalize(lifetime);
            string normalizedOwnership = ResourceOwnership.Normalize(ownership);
            string normalizedStatus = ResourceLifecycleStatus.Normalize(status);
            int resolvedGeneration;
            lock (gate)
            {
                resolvedGeneration = generation > 0 ? generation : ResolveGenerationNoLock(normalizedLifetime);
                if (observationMode == ResourceLifecycleObservationMode.AggregatedCurrentState)
                    return RecordAggregatedResourceNoLock(
                        newDiagnostics,
                        resourceKind,
                        resourceId,
                        ownerId,
                        aggregationKey,
                        normalizedLifetime,
                        normalizedOwnership,
                        normalizedStatus,
                        releasePolicy,
                        resolvedGeneration,
                        isRelease: false);

                string key = BuildRecordKey(resourceKind, resourceId, ownerId, sourcePath, normalizedLifetime, resolvedGeneration);
                bool existed = records.TryGetValue(key, out MutableResourceLifecycleRecord? record);
                if (record == null)
                {
                    record = new MutableResourceLifecycleRecord(
                        Normalize(resourceKind, "unknown"),
                        Normalize(resourceId, "unknown"),
                        Normalize(ownerId, "unknown"),
                        sourcePath ?? string.Empty,
                        normalizedLifetime,
                        normalizedOwnership,
                        resolvedGeneration,
                        normalizedStatus,
                        currentPhase,
                        string.Empty,
                        Normalize(releasePolicy, "report-only"));
                    records[key] = record;
                }
                else
                {
                    record.Status = normalizedStatus;
                    record.Ownership = normalizedOwnership;
                    record.ReleasePolicy = Normalize(releasePolicy, "report-only");
                    record.LastObservedAtPhase = currentPhase;
                    record.ReleasedAtPhase = string.Empty;
                }

                record.ObservationCount++;
                if (ResourceLifecycleStatus.IsAcquireLike(normalizedStatus))
                    record.AcquireCount++;

                if (ShouldWarnTitleIdleGrowthNoLock(record, existed, normalizedStatus))
                {
                    titleIdleResourceGrowthWarnings++;
                    AddDiagnosticNoLock(
                        newDiagnostics,
                        "warning",
                        currentPhase,
                        "Resource acquisition repeated during title idle.",
                        "resource=" + record.ResourceKind + "/" + record.ResourceId + "; owner=" + record.OwnerId + "; lifetime=" + record.Lifetime + "; generation=" + record.Generation + "; count=" + record.AcquireCount);
                }

                AddTimelineNoLock(
                    "resource",
                    currentPhase,
                    "kind=" + record.ResourceKind +
                    "; id=" + record.ResourceId +
                    "; owner=" + record.OwnerId +
                    "; lifetime=" + record.Lifetime +
                    "; ownership=" + record.Ownership +
                    "; generation=" + record.Generation +
                    "; status=" + record.Status);

                return BuildUpdateNoLock(newDiagnostics, record.ResourceKind);
            }
        }

        public ResourceLifecycleLedgerUpdate ReleaseResource(
            string resourceKind,
            string resourceId,
            string ownerId,
            string sourcePath,
            string lifetime,
            string ownership,
            string releasePolicy,
            string status,
            int generation = 0,
            ResourceLifecycleObservationMode observationMode = ResourceLifecycleObservationMode.Detailed,
            string aggregationKey = "")
        {
            var newDiagnostics = new List<ResourceLifecycleDiagnostic>();
            string normalizedLifetime = ResourceLifetime.Normalize(lifetime);
            string normalizedOwnership = ResourceOwnership.Normalize(ownership);
            string normalizedStatus = ResourceLifecycleStatus.Normalize(status);
            lock (gate)
            {
                int resolvedGeneration = generation > 0 ? generation : ResolveGenerationNoLock(normalizedLifetime);
                if (observationMode == ResourceLifecycleObservationMode.AggregatedCurrentState)
                    return RecordAggregatedResourceNoLock(
                        newDiagnostics,
                        resourceKind,
                        resourceId,
                        ownerId,
                        aggregationKey,
                        normalizedLifetime,
                        normalizedOwnership,
                        normalizedStatus,
                        releasePolicy,
                        resolvedGeneration,
                        isRelease: true);

                string key = BuildRecordKey(resourceKind, resourceId, ownerId, sourcePath, normalizedLifetime, resolvedGeneration);
                if (!records.TryGetValue(key, out MutableResourceLifecycleRecord? record))
                {
                    record = new MutableResourceLifecycleRecord(
                        Normalize(resourceKind, "unknown"),
                        Normalize(resourceId, "unknown"),
                        Normalize(ownerId, "unknown"),
                        sourcePath ?? string.Empty,
                        normalizedLifetime,
                        normalizedOwnership,
                        resolvedGeneration,
                        normalizedStatus,
                        string.Empty,
                        currentPhase,
                        Normalize(releasePolicy, "report-only"));
                    records[key] = record;
                }

                record.Status = normalizedStatus;
                record.Ownership = normalizedOwnership;
                record.ReleasePolicy = Normalize(releasePolicy, "report-only");
                record.ReleasedAtPhase = currentPhase;
                record.LastObservedAtPhase = currentPhase;
                record.ReleaseCount++;
                AddTimelineNoLock(
                    "release",
                    currentPhase,
                    "kind=" + record.ResourceKind +
                    "; id=" + record.ResourceId +
                    "; owner=" + record.OwnerId +
                    "; lifetime=" + record.Lifetime +
                    "; ownership=" + record.Ownership +
                    "; generation=" + record.Generation +
                    "; status=" + record.Status);
                return BuildUpdateNoLock(newDiagnostics, record.ResourceKind);
            }
        }

        public ResourceLifecycleLedgerUpdate RecordRefresh(string area, string reason, string result, int resourceCount)
        {
            var newDiagnostics = new List<ResourceLifecycleDiagnostic>();
            string normalizedArea = Normalize(area, "unknown");
            string normalizedReason = Normalize(reason, "unknown");
            string normalizedResult = ResourceLifecycleStatus.Normalize(result);
            lock (gate)
            {
                string key = currentPhase + "::" + normalizedArea + "::" + normalizedResult + "::contentGeneration=" + contentGeneration;
                string fastPathKey = key + "::resources=" + resourceCount;
                bool skippedRefresh =
                    string.Equals(normalizedResult, ResourceLifecycleStatus.SkippedThrottle, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(normalizedResult, ResourceLifecycleStatus.SkippedUnchanged, StringComparison.OrdinalIgnoreCase);
                if (skippedRefresh)
                    skippedRefreshCalls++;

                refreshCounts.TryGetValue(key, out int count);
                count++;
                refreshCounts[key] = count;

                if (string.Equals(normalizedResult, ResourceLifecycleStatus.Rebuilt, StringComparison.OrdinalIgnoreCase) && count > 1)
                {
                    AddDiagnosticNoLock(
                        newDiagnostics,
                        "warning",
                        currentPhase,
                        "Content resource refresh rebuilt more than once for the same phase and content generation.",
                        "area=" + normalizedArea + "; reason=" + normalizedReason + "; generation=" + contentGeneration + "; count=" + count);
                }

                if (skippedRefresh && skippedRefreshFastPathKeys.Contains(fastPathKey))
                {
                    skippedRefreshFastPathCount++;
                    return ResourceLifecycleLedgerUpdate.NoPublish();
                }

                if (skippedRefresh)
                    skippedRefreshFastPathKeys.Add(fastPathKey);

                AddTimelineNoLock(
                    "refresh",
                    currentPhase,
                    "area=" + normalizedArea +
                    "; reason=" + normalizedReason +
                    "; result=" + normalizedResult +
                    "; contentGeneration=" + contentGeneration +
                    "; resources=" + resourceCount +
                    "; count=" + count);
                return BuildUpdateNoLock(newDiagnostics, normalizedArea);
            }
        }

        public ResourceLifecycleLedgerUpdate RecordCleanup(string area, string reason, int clearedCount, string details)
        {
            var newDiagnostics = new List<ResourceLifecycleDiagnostic>();
            lock (gate)
            {
                returnedToTitleCleanupPlan = "area=" + Normalize(area, "unknown") +
                    "; reason=" + Normalize(reason, "unknown") +
                    "; cleared=" + clearedCount +
                    (string.IsNullOrWhiteSpace(details) ? string.Empty : "; " + details.Trim());
                AddTimelineNoLock("cleanup", currentPhase, returnedToTitleCleanupPlan);
                return BuildUpdateNoLock(newDiagnostics, Normalize(area, "unknown"));
            }
        }

        public ResourceLifecycleSnapshot GetSnapshot()
        {
            lock (gate)
                return BuildSnapshotNoLock();
        }

        public int RemoveOwner(string ownerId)
        {
            string normalizedOwner = Normalize(ownerId, "unknown");
            lock (gate)
            {
                string[] keys = records
                    .Where(pair => pair.Value.OwnerId.Equals(normalizedOwner, StringComparison.OrdinalIgnoreCase))
                    .Select(pair => pair.Key)
                    .ToArray();
                foreach (string key in keys)
                    records.Remove(key);
                if (keys.Length > 0)
                    AddTimelineNoLock("owner-cleanup", currentPhase, "owner=" + normalizedOwner + "; removed=" + keys.Length);
                return keys.Length;
            }
        }

        private ResourceLifecycleLedgerUpdate BuildUpdateNoLock(IReadOnlyList<ResourceLifecycleDiagnostic> newDiagnostics, string publishArea)
        {
            string area = Normalize(publishArea, "unknown");
            publishCountsByArea.TryGetValue(area, out int count);
            publishCountsByArea[area] = count + 1;
            return new ResourceLifecycleLedgerUpdate(BuildSnapshotNoLock(), newDiagnostics.ToArray());
        }

        private ResourceLifecycleSnapshot BuildSnapshotNoLock()
        {
            snapshotBuilds++;
            ResourceLifecycleDiagnostic[] diagnosticSnapshot = diagnostics.ToArray();
            int errorCount = diagnosticSnapshot.Count(d => d.Severity.Equals("error", StringComparison.OrdinalIgnoreCase));
            int warningCount = diagnosticSnapshot.Count(d => d.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase));
            string status = errorCount > 0 ? "error" : warningCount > 0 ? "warning" : "ok";
            ResourceLifecycleRecord[] recordSnapshot = records.Values.Select(r => r.ToRecord()).OrderBy(r => r.ResourceKind, StringComparer.OrdinalIgnoreCase).ThenBy(r => r.ResourceId, StringComparer.OrdinalIgnoreCase).ToArray();
            int releasedCurrentSaveRecords = recordSnapshot.Count(r =>
                r.Lifetime.Equals(ResourceLifetime.SaveLifetime, StringComparison.OrdinalIgnoreCase) &&
                r.Generation == saveGeneration &&
                r.Status.Equals(ResourceLifecycleStatus.Released, StringComparison.OrdinalIgnoreCase));
            int aggregatedRecordCount = recordSnapshot.Count(r => r.SourcePath.Equals("aggregated-current-state", StringComparison.OrdinalIgnoreCase));
            return new ResourceLifecycleSnapshot(
                status,
                processGeneration,
                contentGeneration,
                saveGeneration,
                saveGenerationOpen,
                recordSnapshot,
                refreshCounts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
                publishCountsByArea.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
                timeline.ToArray(),
                diagnosticSnapshot,
                currentPhase,
                returnedToTitleCleanupPlan,
                titleIdleResourceGrowthWarnings,
                prunedSaveLifetimeRecords,
                releasedCurrentSaveRecords,
                snapshotBuilds,
                skippedRefreshCalls,
                skippedRefreshFastPathCount,
                aggregatedRecordCount);
        }

        private ResourceLifecycleLedgerUpdate RecordAggregatedResourceNoLock(
            IReadOnlyList<ResourceLifecycleDiagnostic> newDiagnostics,
            string resourceKind,
            string resourceId,
            string ownerId,
            string aggregationKey,
            string lifetime,
            string ownership,
            string status,
            string releasePolicy,
            int generation,
            bool isRelease)
        {
            string normalizedKind = Normalize(resourceKind, "unknown");
            string normalizedOwner = Normalize(ownerId, "unknown");
            string normalizedAggregationKey = Normalize(aggregationKey, normalizedKind);
            string aggregateId = "aggregate:" + normalizedAggregationKey;
            const string aggregateSource = "aggregated-current-state";
            string key = BuildRecordKey(normalizedKind, aggregateId, normalizedOwner, aggregateSource, lifetime, generation);
            if (!records.TryGetValue(key, out MutableResourceLifecycleRecord? record))
            {
                record = new MutableResourceLifecycleRecord(
                    normalizedKind,
                    aggregateId,
                    normalizedOwner,
                    aggregateSource,
                    lifetime,
                    ownership,
                    generation,
                    status,
                    isRelease ? string.Empty : currentPhase,
                    isRelease ? currentPhase : string.Empty,
                    string.Empty);
                records[key] = record;
            }

            record.Status = status;
            record.Ownership = ownership;
            record.LastObservedAtPhase = currentPhase;
            if (string.IsNullOrWhiteSpace(record.FirstSampleId))
                record.FirstSampleId = Normalize(resourceId, "unknown");
            record.LastSampleId = Normalize(resourceId, "unknown");

            if (isRelease)
            {
                record.ReleasedAtPhase = currentPhase;
                record.ReleaseCount++;
            }
            else
            {
                record.ObservationCount++;
                if (ResourceLifecycleStatus.IsAcquireLike(status))
                    record.AcquireCount++;
            }

            record.ReleasePolicy = "aggregated-current-state; " +
                Normalize(releasePolicy, "report-only") +
                "; aggregationKey=" + normalizedAggregationKey +
                "; firstSample=" + record.FirstSampleId +
                "; lastSample=" + record.LastSampleId +
                "; observed=" + record.ObservationCount +
                "; released=" + record.ReleaseCount;

            int eventCount = record.ObservationCount + record.ReleaseCount;
            bool shouldPublish = eventCount <= 2 || eventCount % 100 == 0 || newDiagnostics.Count > 0;
            if (!shouldPublish)
                return ResourceLifecycleLedgerUpdate.NoPublish();

            AddTimelineNoLock(
                isRelease ? "release-aggregate" : "resource-aggregate",
                currentPhase,
                "kind=" + record.ResourceKind +
                "; owner=" + record.OwnerId +
                "; lifetime=" + record.Lifetime +
                "; ownership=" + record.Ownership +
                "; generation=" + record.Generation +
                "; observed=" + record.ObservationCount +
                "; released=" + record.ReleaseCount +
                "; status=" + record.Status +
                "; sample=" + record.LastSampleId);
            return BuildUpdateNoLock(newDiagnostics, record.ResourceKind);
        }

        private bool ShouldWarnTitleIdleGrowthNoLock(MutableResourceLifecycleRecord record, bool existed, string status)
        {
            if (!existed || !ResourceLifecycleStatus.IsAcquireLike(status))
                return false;
            if (saveGenerationOpen)
                return false;
            if (!currentPhase.Equals(RuntimeLifecyclePhase.TitleObserved, StringComparison.OrdinalIgnoreCase) &&
                !currentPhase.Equals(RuntimeLifecyclePhase.ReturnedToTitle, StringComparison.OrdinalIgnoreCase))
                return false;
            return record.AcquireCount > 1;
        }

        private int ResolveGenerationNoLock(string lifetime)
        {
            if (string.Equals(lifetime, ResourceLifetime.ProcessLifetime, StringComparison.OrdinalIgnoreCase))
                return processGeneration;
            if (string.Equals(lifetime, ResourceLifetime.SaveLifetime, StringComparison.OrdinalIgnoreCase))
                return saveGeneration;
            return contentGeneration;
        }

        private void AddDiagnosticNoLock(List<ResourceLifecycleDiagnostic> newDiagnostics, string severity, string phase, string message, string details)
        {
            var diagnostic = new ResourceLifecycleDiagnostic(severity, phase, message, details, DateTimeOffset.Now);
            diagnostics.Enqueue(diagnostic);
            while (diagnostics.Count > MaxDiagnostics)
                diagnostics.Dequeue();
            newDiagnostics.Add(diagnostic);
        }

        private void AddTimelineNoLock(string kind, string phase, string details)
        {
            timeline.Enqueue(new ResourceLifecycleTimelineEntry(kind, phase, details, DateTimeOffset.Now));
            while (timeline.Count > MaxTimeline)
                timeline.Dequeue();
        }

        private void PruneClosedSaveLifetimeRecordsNoLock()
        {
            if (saveGeneration <= 1)
                return;

            string[] staleKeys = records
                .Where(pair =>
                    pair.Value.Lifetime.Equals(ResourceLifetime.SaveLifetime, StringComparison.OrdinalIgnoreCase) &&
                    pair.Value.Generation < saveGeneration)
                .Select(pair => pair.Key)
                .ToArray();

            if (staleKeys.Length == 0)
                return;

            foreach (string key in staleKeys)
                records.Remove(key);

            prunedSaveLifetimeRecords += staleKeys.Length;
            AddTimelineNoLock(
                "prune",
                currentPhase,
                "kind=SaveLifetimeRecords; cleared=" + staleKeys.Length + "; keptGeneration=" + saveGeneration);
        }

        private static string BuildRecordKey(string resourceKind, string resourceId, string ownerId, string sourcePath, string lifetime, int generation)
        {
            return Normalize(resourceKind, "unknown") + "::" +
                Normalize(resourceId, "unknown") + "::" +
                Normalize(ownerId, "unknown") + "::" +
                (sourcePath ?? string.Empty) + "::" +
                ResourceLifetime.Normalize(lifetime) + "::" +
                generation;
        }

        private static string Normalize(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static string FormatNullable(int? value) => value.HasValue ? value.Value.ToString() : "unknown";

        private static string FormatNullable(bool? value) => value.HasValue ? (value.Value ? "true" : "false") : "unknown";

        private sealed class MutableResourceLifecycleRecord
        {
            public MutableResourceLifecycleRecord(
                string resourceKind,
                string resourceId,
                string ownerId,
                string sourcePath,
                string lifetime,
                string ownership,
                int generation,
                string status,
                string acquiredAtPhase,
                string releasedAtPhase,
                string releasePolicy)
            {
                ResourceKind = resourceKind;
                ResourceId = resourceId;
                OwnerId = ownerId;
                SourcePath = sourcePath;
                Lifetime = lifetime;
                Ownership = ownership;
                Generation = generation;
                Status = status;
                AcquiredAtPhase = acquiredAtPhase;
                ReleasedAtPhase = releasedAtPhase;
                LastObservedAtPhase = acquiredAtPhase;
                ReleasePolicy = releasePolicy;
            }

            public string ResourceKind { get; }

            public string ResourceId { get; }

            public string OwnerId { get; }

            public string SourcePath { get; }

            public string Lifetime { get; }

            public string Ownership { get; set; }

            public int Generation { get; }

            public string Status { get; set; }

            public string AcquiredAtPhase { get; }

            public string ReleasedAtPhase { get; set; }

            public string LastObservedAtPhase { get; set; }

            public string ReleasePolicy { get; set; }

            public int ObservationCount { get; set; }

            public int AcquireCount { get; set; }

            public int ReleaseCount { get; set; }

            public string FirstSampleId { get; set; } = string.Empty;

            public string LastSampleId { get; set; } = string.Empty;

            public ResourceLifecycleRecord ToRecord()
            {
                return new ResourceLifecycleRecord(
                    ResourceKind,
                    ResourceId,
                    OwnerId,
                    SourcePath,
                    Lifetime,
                    Ownership,
                    Generation,
                    Status,
                    AcquiredAtPhase,
                    ReleasedAtPhase,
                    ReleasePolicy,
                    ObservationCount,
                    AcquireCount,
                    ReleaseCount);
            }
        }
    }

    internal sealed class ResourceLifecycleLedgerUpdate
    {
        public ResourceLifecycleLedgerUpdate(ResourceLifecycleSnapshot snapshot, IReadOnlyList<ResourceLifecycleDiagnostic> newDiagnostics)
            : this(snapshot, newDiagnostics, shouldPublish: true)
        {
        }

        private ResourceLifecycleLedgerUpdate(ResourceLifecycleSnapshot? snapshot, IReadOnlyList<ResourceLifecycleDiagnostic> newDiagnostics, bool shouldPublish)
        {
            Snapshot = snapshot!;
            NewDiagnostics = newDiagnostics ?? Array.Empty<ResourceLifecycleDiagnostic>();
            ShouldPublish = shouldPublish;
        }

        public static ResourceLifecycleLedgerUpdate NoPublish() => new ResourceLifecycleLedgerUpdate(null, Array.Empty<ResourceLifecycleDiagnostic>(), shouldPublish: false);

        public bool ShouldPublish { get; }

        public ResourceLifecycleSnapshot Snapshot { get; }

        public IReadOnlyList<ResourceLifecycleDiagnostic> NewDiagnostics { get; }
    }

    internal sealed class ResourceLifecycleSnapshot
    {
        public ResourceLifecycleSnapshot(
            string status,
            int processGeneration,
            int contentGeneration,
            int saveGeneration,
            bool saveGenerationOpen,
            IReadOnlyList<ResourceLifecycleRecord> records,
            IReadOnlyDictionary<string, int> refreshCounts,
            IReadOnlyDictionary<string, int> publishCountsByArea,
            IReadOnlyList<ResourceLifecycleTimelineEntry> timeline,
            IReadOnlyList<ResourceLifecycleDiagnostic> diagnostics,
            string currentPhase,
            string returnedToTitleCleanupPlan,
            int titleIdleResourceGrowthWarnings,
            int prunedSaveLifetimeRecords,
            int releasedCurrentSaveRecords,
            int snapshotBuilds,
            int skippedRefreshCalls,
            int skippedRefreshFastPathCount,
            int aggregatedRecordCount)
        {
            Status = status ?? "unknown";
            ProcessGeneration = processGeneration;
            ContentGeneration = contentGeneration;
            SaveGeneration = saveGeneration;
            SaveGenerationOpen = saveGenerationOpen;
            Records = records ?? Array.Empty<ResourceLifecycleRecord>();
            RefreshCounts = refreshCounts ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            PublishCountsByArea = publishCountsByArea ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Timeline = timeline ?? Array.Empty<ResourceLifecycleTimelineEntry>();
            Diagnostics = diagnostics ?? Array.Empty<ResourceLifecycleDiagnostic>();
            CurrentPhase = currentPhase ?? RuntimeLifecyclePhase.Unknown;
            ReturnedToTitleCleanupPlan = string.IsNullOrWhiteSpace(returnedToTitleCleanupPlan) ? "none" : returnedToTitleCleanupPlan;
            TitleIdleResourceGrowthWarnings = titleIdleResourceGrowthWarnings;
            PrunedSaveLifetimeRecords = prunedSaveLifetimeRecords;
            ReleasedCurrentSaveRecords = releasedCurrentSaveRecords;
            SnapshotBuilds = snapshotBuilds;
            SkippedRefreshCalls = skippedRefreshCalls;
            SkippedRefreshFastPathCount = skippedRefreshFastPathCount;
            AggregatedRecordCount = aggregatedRecordCount;
        }

        public string Status { get; }

        public int ProcessGeneration { get; }

        public int ContentGeneration { get; }

        public int SaveGeneration { get; }

        public bool SaveGenerationOpen { get; }

        public IReadOnlyList<ResourceLifecycleRecord> Records { get; }

        public IReadOnlyDictionary<string, int> RefreshCounts { get; }

        public IReadOnlyDictionary<string, int> PublishCountsByArea { get; }

        public IReadOnlyList<ResourceLifecycleTimelineEntry> Timeline { get; }

        public IReadOnlyList<ResourceLifecycleDiagnostic> Diagnostics { get; }

        public string CurrentPhase { get; }

        public string ReturnedToTitleCleanupPlan { get; }

        public int TitleIdleResourceGrowthWarnings { get; }

        public int PrunedSaveLifetimeRecords { get; }

        public int RecordCount => Records.Count;

        public int ReleasedCurrentSaveRecords { get; }

        public int SnapshotBuilds { get; }

        public int SkippedRefreshCalls { get; }

        public int SkippedRefreshFastPathCount { get; }

        public int AggregatedRecordCount { get; }

        public bool Success => !Status.Equals("error", StringComparison.OrdinalIgnoreCase);

        public int ErrorCount => Diagnostics.Count(d => d.Severity.Equals("error", StringComparison.OrdinalIgnoreCase));

        public int WarningCount => Diagnostics.Count(d => d.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase));

        public string FormatSummary()
        {
            string byLifetime = FormatCounts(Records.GroupBy(r => r.Lifetime, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase));
            string byOwnership = FormatCounts(Records.GroupBy(r => r.Ownership, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase));
            return "status=" + Status +
                "; processGeneration=" + ProcessGeneration +
                "; contentGeneration=" + ContentGeneration +
                "; saveGeneration=" + SaveGeneration +
                "; saveOpen=" + (SaveGenerationOpen ? "true" : "false") +
                "; records=" + Records.Count +
                "; recordCount=" + RecordCount +
                "; releasedCurrentSaveRecords=" + ReleasedCurrentSaveRecords +
                "; snapshotBuilds=" + SnapshotBuilds +
                "; byLifetime=" + byLifetime +
                "; byOwnership=" + byOwnership +
                "; refreshes=" + RefreshCounts.Count +
                "; publishCountByArea=" + FormatCounts(PublishCountsByArea) +
                "; skippedRefreshCalls=" + SkippedRefreshCalls +
                "; skippedRefreshFastPath=" + SkippedRefreshFastPathCount +
                "; aggregatedRecords=" + AggregatedRecordCount +
                "; titleIdleGrowthWarnings=" + TitleIdleResourceGrowthWarnings +
                "; prunedSaveLifetimeRecords=" + PrunedSaveLifetimeRecords +
                "; warnings=" + WarningCount +
                "; errors=" + ErrorCount;
        }

        public string FormatReturnedToTitleCleanupPlan() => ReturnedToTitleCleanupPlan;

        public string FormatTitleIdleResourceGrowth()
        {
            return TitleIdleResourceGrowthWarnings == 0
                ? "status=ok; warnings=0"
                : "status=warning; warnings=" + TitleIdleResourceGrowthWarnings;
        }

        private static string FormatCounts(IReadOnlyDictionary<string, int> counts)
        {
            if (counts.Count == 0)
                return "none";
            return string.Join(",", counts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => pair.Key + ":" + pair.Value).ToArray());
        }
    }

    internal sealed class ResourceLifecycleRecord
    {
        public ResourceLifecycleRecord(
            string resourceKind,
            string resourceId,
            string ownerId,
            string sourcePath,
            string lifetime,
            string ownership,
            int generation,
            string status,
            string acquiredAtPhase,
            string releasedAtPhase,
            string releasePolicy,
            int observationCount,
            int acquireCount,
            int releaseCount)
        {
            ResourceKind = resourceKind ?? string.Empty;
            ResourceId = resourceId ?? string.Empty;
            OwnerId = ownerId ?? string.Empty;
            SourcePath = sourcePath ?? string.Empty;
            Lifetime = lifetime ?? string.Empty;
            Ownership = ownership ?? string.Empty;
            Generation = generation;
            Status = status ?? string.Empty;
            AcquiredAtPhase = acquiredAtPhase ?? string.Empty;
            ReleasedAtPhase = releasedAtPhase ?? string.Empty;
            ReleasePolicy = releasePolicy ?? string.Empty;
            ObservationCount = observationCount;
            AcquireCount = acquireCount;
            ReleaseCount = releaseCount;
        }

        public string ResourceKind { get; }

        public string ResourceId { get; }

        public string OwnerId { get; }

        public string SourcePath { get; }

        public string Lifetime { get; }

        public string Ownership { get; }

        public int Generation { get; }

        public string Status { get; }

        public string AcquiredAtPhase { get; }

        public string ReleasedAtPhase { get; }

        public string ReleasePolicy { get; }

        public int ObservationCount { get; }

        public int AcquireCount { get; }

        public int ReleaseCount { get; }
    }

    internal sealed class ResourceLifecycleTimelineEntry
    {
        public ResourceLifecycleTimelineEntry(string kind, string phase, string details, DateTimeOffset timestamp)
        {
            Kind = kind ?? string.Empty;
            Phase = phase ?? RuntimeLifecyclePhase.Unknown;
            Details = details ?? string.Empty;
            Timestamp = timestamp;
        }

        public string Kind { get; }

        public string Phase { get; }

        public string Details { get; }

        public DateTimeOffset Timestamp { get; }
    }

    internal sealed class ResourceLifecycleDiagnostic
    {
        public ResourceLifecycleDiagnostic(string severity, string phase, string message, string details, DateTimeOffset timestamp)
        {
            Severity = string.IsNullOrWhiteSpace(severity) ? "warning" : severity.Trim();
            Phase = string.IsNullOrWhiteSpace(phase) ? RuntimeLifecyclePhase.Unknown : phase.Trim();
            Message = message ?? string.Empty;
            Details = details ?? string.Empty;
            Timestamp = timestamp;
        }

        public string Severity { get; }

        public string Phase { get; }

        public string Message { get; }

        public string Details { get; }

        public DateTimeOffset Timestamp { get; }

        public string Format()
        {
            return Severity + " phase=" + Phase + " message=" + Message + (string.IsNullOrWhiteSpace(Details) ? string.Empty : " details=" + Details);
        }
    }
}
