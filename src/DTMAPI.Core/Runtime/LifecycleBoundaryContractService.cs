using System;
using System.Collections.Generic;
using System.Linq;

namespace DTMAPI.Core.Runtime
{
    internal sealed class LifecycleBoundaryContractService
    {
        private const int MaxDiagnostics = 96;
        private const int MaxTimeline = 128;
        private readonly object gate = new object();
        private readonly Dictionary<string, LifecycleBoundaryPolicy> policies = LifecycleBoundaryPolicy.CreateDefaultPolicies();
        private readonly Dictionary<string, int> phaseCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> registryRefreshCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> hookStatusCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> hookInstallSignalCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> resourceEventCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<LifecycleBoundaryTimelineEntry> timeline = new Queue<LifecycleBoundaryTimelineEntry>();
        private readonly Queue<LifecycleBoundaryDiagnostic> diagnostics = new Queue<LifecycleBoundaryDiagnostic>();
        private string currentPhase = RuntimeLifecyclePhase.Unknown;

        public LifecycleBoundaryContractUpdate RecordPhase(
            string phase,
            int? saveSlot,
            bool? isNewGame,
            int discoveredModCount,
            int loadedModCount,
            int hookStatusCount)
        {
            string normalizedPhase = RuntimeLifecyclePhase.Normalize(phase);
            var newDiagnostics = new List<LifecycleBoundaryDiagnostic>();
            lock (gate)
            {
                phaseCounts.TryGetValue(normalizedPhase, out int currentCount);
                currentCount++;
                phaseCounts[normalizedPhase] = currentCount;
                currentPhase = normalizedPhase;

                if (!policies.ContainsKey(normalizedPhase))
                    AddDiagnosticNoLock(newDiagnostics, "error", normalizedPhase, "Unknown lifecycle phase.", "phase=" + normalizedPhase);

                AddTimelineNoLock(new LifecycleBoundaryTimelineEntry(
                    "phase",
                    normalizedPhase,
                    "count=" + currentCount +
                    "; saveSlot=" + FormatNullable(saveSlot) +
                    "; isNewGame=" + FormatNullable(isNewGame) +
                    "; discovered=" + discoveredModCount +
                    "; loaded=" + loadedModCount +
                    "; hooks=" + hookStatusCount));

                return BuildUpdateNoLock(newDiagnostics);
            }
        }

        public LifecycleBoundaryContractUpdate RecordRegistryRefresh(
            string reason,
            int rowCount,
            int loadedRowCount,
            int customAnimalDefinitionCount,
            int audioReplacementCount)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim();
            var newDiagnostics = new List<LifecycleBoundaryDiagnostic>();
            lock (gate)
            {
                string phase = string.IsNullOrWhiteSpace(currentPhase) ? RuntimeLifecyclePhase.Unknown : currentPhase;
                string key = phase + "::" + normalizedReason;
                registryRefreshCounts.TryGetValue(key, out int count);
                count++;
                registryRefreshCounts[key] = count;

                if (count > 1)
                {
                    AddDiagnosticNoLock(
                        newDiagnostics,
                        "warning",
                        phase,
                        "Shadow registry refresh repeated for the same lifecycle phase and reason.",
                        "reason=" + normalizedReason + "; count=" + count);
                }

                AddTimelineNoLock(new LifecycleBoundaryTimelineEntry(
                    "registry",
                    phase,
                    "reason=" + normalizedReason +
                    "; count=" + count +
                    "; rows=" + rowCount +
                    "; loadedRows=" + loadedRowCount +
                    "; customAnimals=" + customAnimalDefinitionCount +
                    "; audioReplacements=" + audioReplacementCount));

                return BuildUpdateNoLock(newDiagnostics);
            }
        }

        public LifecycleBoundaryContractUpdate RecordHookStatus(string hookId, string status, string source, string details)
        {
            string normalizedHookId = string.IsNullOrWhiteSpace(hookId) ? "unknown" : hookId.Trim();
            string normalizedStatus = string.IsNullOrWhiteSpace(status) ? "unknown" : status.Trim();
            var newDiagnostics = new List<LifecycleBoundaryDiagnostic>();
            lock (gate)
            {
                string phase = string.IsNullOrWhiteSpace(currentPhase) ? RuntimeLifecyclePhase.Unknown : currentPhase;
                string statusKey = normalizedHookId + "::" + normalizedStatus;
                hookStatusCounts.TryGetValue(statusKey, out int statusCount);
                statusCount++;
                hookStatusCounts[statusKey] = statusCount;

                if (LooksLikeHookInstallSignal(normalizedHookId, normalizedStatus, source, details))
                {
                    hookInstallSignalCounts.TryGetValue(normalizedHookId, out int installCount);
                    installCount++;
                    hookInstallSignalCounts[normalizedHookId] = installCount;
                    if (installCount > 1)
                    {
                        AddDiagnosticNoLock(
                            newDiagnostics,
                            "warning",
                            phase,
                            "Hook install signal repeated for the same hook id.",
                            "hookId=" + normalizedHookId + "; count=" + installCount + "; status=" + normalizedStatus);
                    }
                }

                AddTimelineNoLock(new LifecycleBoundaryTimelineEntry(
                    "hook",
                    phase,
                    "hookId=" + normalizedHookId +
                    "; status=" + normalizedStatus +
                    "; statusCount=" + statusCount));

                return BuildUpdateNoLock(newDiagnostics);
            }
        }

        public LifecycleBoundaryContractUpdate RecordResourceEvent(string category, string eventName, string ownerId, string resourceId, string details)
        {
            string normalizedCategory = string.IsNullOrWhiteSpace(category) ? "unknown" : category.Trim();
            string normalizedEventName = string.IsNullOrWhiteSpace(eventName) ? "unknown" : eventName.Trim();
            string normalizedOwnerId = string.IsNullOrWhiteSpace(ownerId) ? "unknown" : ownerId.Trim();
            string normalizedResourceId = string.IsNullOrWhiteSpace(resourceId) ? "unknown" : resourceId.Trim();
            string normalizedDetails = string.IsNullOrWhiteSpace(details) ? string.Empty : details.Trim();
            var newDiagnostics = new List<LifecycleBoundaryDiagnostic>();
            lock (gate)
            {
                string phase = string.IsNullOrWhiteSpace(currentPhase) ? RuntimeLifecyclePhase.Unknown : currentPhase;
                string key = phase + "::" + normalizedCategory + "::" + normalizedEventName;
                resourceEventCounts.TryGetValue(key, out int count);
                count++;
                resourceEventCounts[key] = count;

                AddTimelineNoLock(new LifecycleBoundaryTimelineEntry(
                    "resource",
                    phase,
                    "category=" + normalizedCategory +
                    "; event=" + normalizedEventName +
                    "; count=" + count +
                    "; owner=" + normalizedOwnerId +
                    "; resource=" + normalizedResourceId +
                    (string.IsNullOrEmpty(normalizedDetails) ? string.Empty : "; details=" + normalizedDetails)));

                return BuildUpdateNoLock(newDiagnostics);
            }
        }

        public LifecycleBoundaryContractUpdate RecordReturnedToTitleState(LifecycleBoundaryRuntimeState state)
        {
            var newDiagnostics = new List<LifecycleBoundaryDiagnostic>();
            lock (gate)
            {
                string phase = RuntimeLifecyclePhase.ReturnedToTitle;
                if (state.CurrentLoadingSlot.HasValue)
                {
                    AddDiagnosticNoLock(
                        newDiagnostics,
                        "warning",
                        phase,
                        "ReturnedToTitle cleanup left a current loading slot.",
                        "slot=" + state.CurrentLoadingSlot.Value);
                }

                if (state.ActiveCustomEntityRuntimeInstanceCount > 0)
                {
                    AddDiagnosticNoLock(
                        newDiagnostics,
                        "warning",
                        phase,
                        "ReturnedToTitle cleanup left custom entity runtime instances.",
                        "activeCustomEntityRuntimeInstances=" + state.ActiveCustomEntityRuntimeInstanceCount);
                }

                AddTimelineNoLock(new LifecycleBoundaryTimelineEntry(
                    "retention",
                    phase,
                    "currentLoadingSlot=" + FormatNullable(state.CurrentLoadingSlot) +
                    "; activeCustomEntityRuntimeInstances=" + state.ActiveCustomEntityRuntimeInstanceCount +
                    "; discovered=" + state.DiscoveredModCount +
                    "; loaded=" + state.LoadedModCount +
                    "; hooks=" + state.HookStatusCount));

                return BuildUpdateNoLock(newDiagnostics);
            }
        }

        public LifecycleBoundaryContractSnapshot GetSnapshot()
        {
            lock (gate)
                return BuildSnapshotNoLock();
        }

        public string FormatPolicyCatalog()
        {
            lock (gate)
            {
                return string.Join(" | ", policies.Values
                    .OrderBy(policy => policy.Order)
                    .Select(policy => policy.Format())
                    .ToArray());
            }
        }

        private LifecycleBoundaryContractUpdate BuildUpdateNoLock(IReadOnlyList<LifecycleBoundaryDiagnostic> newDiagnostics)
        {
            return new LifecycleBoundaryContractUpdate(BuildSnapshotNoLock(), newDiagnostics.ToArray());
        }

        private LifecycleBoundaryContractSnapshot BuildSnapshotNoLock()
        {
            LifecycleBoundaryDiagnostic[] diagnosticSnapshot = diagnostics.ToArray();
            int errorCount = diagnosticSnapshot.Count(d => d.Severity.Equals("error", StringComparison.OrdinalIgnoreCase));
            int warningCount = diagnosticSnapshot.Count(d => d.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase));
            string status = errorCount > 0 ? "error" : warningCount > 0 ? "warning" : "ok";
            return new LifecycleBoundaryContractSnapshot(
                status,
                phaseCounts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
                registryRefreshCounts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
                hookStatusCounts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
                hookInstallSignalCounts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
                resourceEventCounts.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase),
                timeline.ToArray(),
                diagnosticSnapshot,
                currentPhase);
        }

        private void AddDiagnosticNoLock(List<LifecycleBoundaryDiagnostic> newDiagnostics, string severity, string phase, string message, string details)
        {
            var diagnostic = new LifecycleBoundaryDiagnostic(severity, phase, message, details, DateTimeOffset.Now);
            diagnostics.Enqueue(diagnostic);
            while (diagnostics.Count > MaxDiagnostics)
                diagnostics.Dequeue();
            newDiagnostics.Add(diagnostic);
        }

        private void AddTimelineNoLock(LifecycleBoundaryTimelineEntry entry)
        {
            timeline.Enqueue(entry);
            while (timeline.Count > MaxTimeline)
                timeline.Dequeue();
        }

        private static bool LooksLikeHookInstallSignal(string hookId, string status, string source, string details)
        {
            if (hookId.StartsWith("Feature.", StringComparison.OrdinalIgnoreCase) ||
                hookId.StartsWith("Smoke.", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!status.Equals("experimental", StringComparison.OrdinalIgnoreCase) &&
                !status.Equals("ready", StringComparison.OrdinalIgnoreCase))
                return false;

            string sourceText = (source ?? string.Empty).ToLowerInvariant();
            string detailsText = (details ?? string.Empty).ToLowerInvariant();
            bool installSource =
                sourceText.IndexOf("harmony", StringComparison.Ordinal) >= 0 ||
                sourceText.IndexOf("unityevent", StringComparison.Ordinal) >= 0 ||
                sourceText.IndexOf("installhooks", StringComparison.Ordinal) >= 0;
            bool installDetails =
                detailsText.IndexOf("hook installed", StringComparison.Ordinal) >= 0 ||
                detailsText.IndexOf("patched the native", StringComparison.Ordinal) >= 0 ||
                detailsText.IndexOf("patched native", StringComparison.Ordinal) >= 0 ||
                detailsText.StartsWith("patched ", StringComparison.Ordinal);
            return installSource && installDetails;
        }

        private static string FormatNullable(int? value)
        {
            return value.HasValue ? value.Value.ToString() : "none";
        }

        private static string FormatNullable(bool? value)
        {
            return value.HasValue ? (value.Value ? "true" : "false") : "none";
        }
    }

    internal sealed class LifecycleBoundaryContractUpdate
    {
        public LifecycleBoundaryContractUpdate(LifecycleBoundaryContractSnapshot snapshot, IReadOnlyList<LifecycleBoundaryDiagnostic> newDiagnostics)
        {
            Snapshot = snapshot;
            NewDiagnostics = newDiagnostics;
        }

        public LifecycleBoundaryContractSnapshot Snapshot { get; }
        public IReadOnlyList<LifecycleBoundaryDiagnostic> NewDiagnostics { get; }
    }

    internal sealed class LifecycleBoundaryContractSnapshot
    {
        public LifecycleBoundaryContractSnapshot(
            string status,
            IReadOnlyDictionary<string, int> phaseCounts,
            IReadOnlyDictionary<string, int> registryRefreshCounts,
            IReadOnlyDictionary<string, int> hookStatusCounts,
            IReadOnlyDictionary<string, int> hookInstallSignalCounts,
            IReadOnlyDictionary<string, int> resourceEventCounts,
            IReadOnlyList<LifecycleBoundaryTimelineEntry> timeline,
            IReadOnlyList<LifecycleBoundaryDiagnostic> diagnostics,
            string currentPhase)
        {
            Status = status ?? string.Empty;
            PhaseCounts = phaseCounts;
            RegistryRefreshCounts = registryRefreshCounts;
            HookStatusCounts = hookStatusCounts;
            HookInstallSignalCounts = hookInstallSignalCounts;
            ResourceEventCounts = resourceEventCounts;
            Timeline = timeline;
            Diagnostics = diagnostics;
            CurrentPhase = currentPhase ?? string.Empty;
        }

        public string Status { get; }
        public IReadOnlyDictionary<string, int> PhaseCounts { get; }
        public IReadOnlyDictionary<string, int> RegistryRefreshCounts { get; }
        public IReadOnlyDictionary<string, int> HookStatusCounts { get; }
        public IReadOnlyDictionary<string, int> HookInstallSignalCounts { get; }
        public IReadOnlyDictionary<string, int> ResourceEventCounts { get; }
        public IReadOnlyList<LifecycleBoundaryTimelineEntry> Timeline { get; }
        public IReadOnlyList<LifecycleBoundaryDiagnostic> Diagnostics { get; }
        public string CurrentPhase { get; }
        public int WarningCount => Diagnostics.Count(d => d.Severity.Equals("warning", StringComparison.OrdinalIgnoreCase));
        public int ErrorCount => Diagnostics.Count(d => d.Severity.Equals("error", StringComparison.OrdinalIgnoreCase));
        public bool Success => ErrorCount == 0;

        public string FormatSummary()
        {
            string phases = PhaseCounts.Count == 0
                ? "none"
                : string.Join(",", PhaseCounts.Select(pair => pair.Key + "=" + pair.Value).ToArray());
            string registry = RegistryRefreshCounts.Count == 0
                ? "none"
                : string.Join(",", RegistryRefreshCounts.Take(8).Select(pair => pair.Key + "=" + pair.Value).ToArray());
            string hookInstalls = HookInstallSignalCounts.Count == 0
                ? "none"
                : string.Join(",", HookInstallSignalCounts.Take(8).Select(pair => pair.Key + "=" + pair.Value).ToArray());
            string resources = ResourceEventCounts.Count == 0
                ? "none"
                : string.Join(",", ResourceEventCounts.Take(8).Select(pair => pair.Key + "=" + pair.Value).ToArray());
            string firstDiagnostics = Diagnostics.Count == 0
                ? "none"
                : string.Join(" || ", Diagnostics.Take(5).Select(d => d.Format()).ToArray());
            LifecycleBoundaryTimelineEntry? last = Timeline.LastOrDefault();
            return "status=" + Status +
                "; currentPhase=" + CurrentPhase +
                "; phases=" + phases +
                "; registryRefreshes=" + registry +
                "; hookInstallSignals=" + hookInstalls +
                "; resources=" + resources +
                "; warnings=" + WarningCount +
                "; errors=" + ErrorCount +
                "; timeline=" + Timeline.Count +
                "; last=" + (last == null ? "none" : last.Format()) +
                "; firstDiagnostics=" + firstDiagnostics;
        }
    }

    internal sealed class LifecycleBoundaryRuntimeState
    {
        public LifecycleBoundaryRuntimeState(int? currentLoadingSlot, int activeCustomEntityRuntimeInstanceCount, int discoveredModCount, int loadedModCount, int hookStatusCount)
        {
            CurrentLoadingSlot = currentLoadingSlot;
            ActiveCustomEntityRuntimeInstanceCount = activeCustomEntityRuntimeInstanceCount;
            DiscoveredModCount = discoveredModCount;
            LoadedModCount = loadedModCount;
            HookStatusCount = hookStatusCount;
        }

        public int? CurrentLoadingSlot { get; }
        public int ActiveCustomEntityRuntimeInstanceCount { get; }
        public int DiscoveredModCount { get; }
        public int LoadedModCount { get; }
        public int HookStatusCount { get; }
    }

    internal sealed class LifecycleBoundaryDiagnostic
    {
        public LifecycleBoundaryDiagnostic(string severity, string phase, string message, string details, DateTimeOffset observedAt)
        {
            Severity = severity ?? string.Empty;
            Phase = phase ?? string.Empty;
            Message = message ?? string.Empty;
            Details = details ?? string.Empty;
            ObservedAt = observedAt;
        }

        public string Severity { get; }
        public string Phase { get; }
        public string Message { get; }
        public string Details { get; }
        public DateTimeOffset ObservedAt { get; }

        public string Format()
        {
            return Severity + " phase=" + Phase + " message=" + Message + " details=" + Details;
        }
    }

    internal sealed class LifecycleBoundaryTimelineEntry
    {
        public LifecycleBoundaryTimelineEntry(string kind, string phase, string details)
        {
            Kind = kind ?? string.Empty;
            Phase = phase ?? string.Empty;
            Details = details ?? string.Empty;
            ObservedAt = DateTimeOffset.Now;
        }

        public string Kind { get; }
        public string Phase { get; }
        public string Details { get; }
        public DateTimeOffset ObservedAt { get; }

        public string Format()
        {
            return Kind + "@" + Phase + " " + Details + " observedAt=" + ObservedAt.ToString("o");
        }
    }

    internal sealed class LifecycleBoundaryPolicy
    {
        public LifecycleBoundaryPolicy(
            int order,
            string phase,
            string retainedCaches,
            string releaseRequired,
            string registryPolicy,
            string hookPolicy,
            string titleBoundaryClear)
        {
            Order = order;
            Phase = phase ?? string.Empty;
            RetainedCaches = retainedCaches ?? string.Empty;
            ReleaseRequired = releaseRequired ?? string.Empty;
            RegistryPolicy = registryPolicy ?? string.Empty;
            HookPolicy = hookPolicy ?? string.Empty;
            TitleBoundaryClear = titleBoundaryClear ?? string.Empty;
        }

        public int Order { get; }
        public string Phase { get; }
        public string RetainedCaches { get; }
        public string ReleaseRequired { get; }
        public string RegistryPolicy { get; }
        public string HookPolicy { get; }
        public string TitleBoundaryClear { get; }

        public string Format()
        {
            return Phase +
                "{retain=" + RetainedCaches +
                "; release=" + ReleaseRequired +
                "; registry=" + RegistryPolicy +
                "; hooks=" + HookPolicy +
                "; titleClear=" + TitleBoundaryClear + "}";
        }

        public static Dictionary<string, LifecycleBoundaryPolicy> CreateDefaultPolicies()
        {
            var policies = new[]
            {
                new LifecycleBoundaryPolicy(
                    0,
                    RuntimeLifecyclePhase.Startup,
                    "runtime services, manifest/content indexes after load, hook install flags",
                    "no save-scoped runtime instances",
                    "DiscoverMods and LoadMods initial are shadow-only and should not repeat for the same reason",
                    "hook ids may retry while targets are missing; installed hook ids should not install twice",
                    "none"),
                new LifecycleBoundaryPolicy(
                    1,
                    RuntimeLifecyclePhase.TitleObserved,
                    "loaded mod registry, content pack metadata, content index, hook install flags, title config UI",
                    "save slot and save-scoped runtime instances must be absent",
                    "no lifecycle-driven registry refresh; official mod list changes are the only legacy hot path",
                    "hook install flags remain process-wide",
                    "title UI frame state only"),
                new LifecycleBoundaryPolicy(
                    2,
                    RuntimeLifecyclePhase.SaveLoaded,
                    "loaded mod registry, content indexes, content pack metadata, hook install flags",
                    "previous save-scoped custom entity runtime instances must be cleared by existing save boundary",
                    "no lifecycle-driven registry refresh",
                    "no hook reinstall on save load",
                    "none"),
                new LifecycleBoundaryPolicy(
                    3,
                    RuntimeLifecyclePhase.ReturnedToTitle,
                    "loaded mod registry, DLL instances, content indexes, content pack metadata, hook install flags",
                    "current loading slot and save-scoped runtime instances must be clear after old boundary actions",
                    "no lifecycle-driven registry refresh",
                    "Harmony hooks remain installed process-wide and must not reinstall",
                    "currentLoadingSlot, custom entity runtime instances, save UI/input transients"),
                new LifecycleBoundaryPolicy(
                    4,
                    RuntimeLifecyclePhase.LogExport,
                    "diagnostic snapshots, latest log path, report path",
                    "no resource release; export is read-only",
                    "no registry refresh during export",
                    "no hook install during export",
                    "none"),
                new LifecycleBoundaryPolicy(
                    5,
                    RuntimeLifecyclePhase.Shutdown,
                    "latest diagnostics and logs until process exit",
                    "no new game resources should be created",
                    "no registry refresh during shutdown",
                    "no hook install during shutdown",
                    "none")
            };
            return policies.ToDictionary(policy => policy.Phase, StringComparer.OrdinalIgnoreCase);
        }
    }

    internal static class RuntimeLifecyclePhase
    {
        public const string Startup = "Startup";
        public const string TitleObserved = "TitleObserved";
        public const string SaveLoaded = "SaveLoaded";
        public const string ReturnedToTitle = "ReturnedToTitle";
        public const string LogExport = "LogExport";
        public const string Shutdown = "Shutdown";
        public const string Unknown = "Unknown";

        public static string Normalize(string phase)
        {
            string value = string.IsNullOrWhiteSpace(phase) ? Unknown : phase.Trim();
            if (value.Equals(Startup, StringComparison.OrdinalIgnoreCase))
                return Startup;
            if (value.Equals(TitleObserved, StringComparison.OrdinalIgnoreCase))
                return TitleObserved;
            if (value.Equals(SaveLoaded, StringComparison.OrdinalIgnoreCase))
                return SaveLoaded;
            if (value.Equals(ReturnedToTitle, StringComparison.OrdinalIgnoreCase))
                return ReturnedToTitle;
            if (value.Equals(LogExport, StringComparison.OrdinalIgnoreCase))
                return LogExport;
            if (value.Equals(Shutdown, StringComparison.OrdinalIgnoreCase))
                return Shutdown;
            return value;
        }
    }
}
