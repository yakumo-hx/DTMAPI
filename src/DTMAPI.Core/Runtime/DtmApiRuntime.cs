using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Services;

namespace DTMAPI.Core.Runtime
{
    public sealed class DtmApiRuntime : IDtmDiagnosticsApi
    {
        public const string ApiVersion = DtmApiBuildVersion.ReleaseVersion;
        public const string BinaryVersion = DtmApiBuildVersion.BinaryFileVersion;
        private const int MaxRefactorScaffoldWarningKeys = 512;

        private readonly IRuntimeHost host;
        private readonly IDtmConfigMenuApi? configMenuApi;
        private readonly IConfigMenuRuntime? configMenuRuntime;
        private readonly List<DiscoveredMod> discoveredMods = new List<DiscoveredMod>();
        private readonly List<DiscoveredMod> loadedMods = new List<DiscoveredMod>();
        private DiscoveredMod[] discoveredModsSnapshot = Array.Empty<DiscoveredMod>();
        private DiscoveredMod[] loadedModsSnapshot = Array.Empty<DiscoveredMod>();
        private IReadOnlyList<DiscoveredMod> discoveredModsView = Array.AsReadOnly(Array.Empty<DiscoveredMod>());
        private IReadOnlyList<DiscoveredMod> loadedModsView = Array.AsReadOnly(Array.Empty<DiscoveredMod>());
        private readonly Dictionary<string, DiscoveredMod> discoveredById = new Dictionary<string, DiscoveredMod>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DtmMod> modInstances = new Dictionary<string, DtmMod>(StringComparer.OrdinalIgnoreCase);
        // Keep the constructed instance from the first successful Activator call until
        // owner deactivation has given an optional IDisposable implementation a chance
        // to restore product-private native/static state.  modInstances remains the
        // publication authority exposed to ordinary runtime/Manager projections.
        private readonly Dictionary<string, DtmMod> modLifecycleInstances = new Dictionary<string, DtmMod>(StringComparer.OrdinalIgnoreCase);
        private readonly LifecycleObservationService lifecycleObservation = new LifecycleObservationService();
        private readonly LifecycleBoundaryContractService lifecycleBoundaryContract = new LifecycleBoundaryContractService();
        private readonly ShadowContentRegistry shadowContentRegistry = new ShadowContentRegistry();
        private readonly ContentManifestRegistry contentManifestRegistry = new ContentManifestRegistry();
        private readonly ResourceLifecycleLedgerService resourceLifecycleLedger = new ResourceLifecycleLedgerService();
        private readonly ContentRefreshGenerationService contentRefreshGenerations = new ContentRefreshGenerationService();
        private readonly RuntimeDemandCoordinator demandCoordinator = new RuntimeDemandCoordinator();
        private readonly RuntimeBoundarySlot<Action<int?, bool>> saveSessionLoadedBoundary = new RuntimeBoundarySlot<Action<int?, bool>>();
        private readonly RuntimeBoundarySlot<Action> returnedToTitleBoundary = new RuntimeBoundarySlot<Action>();
        private readonly RuntimeBoundarySlot<Action<int, bool>> nativeLoadGameReturnedBoundary = new RuntimeBoundarySlot<Action<int, bool>>();
        private readonly RuntimeBoundarySlot<Action> nativeGameFrameBoundary = new RuntimeBoundarySlot<Action>();
        private readonly RuntimeBoundarySlot<Action> logExportBoundary = new RuntimeBoundarySlot<Action>();
        private readonly RuntimeBoundarySlot<Action<string>> runtimeShutdownBoundary = new RuntimeBoundarySlot<Action<string>>();
        private readonly SaveLoadRequestCoordinatorService saveLoadRequestCoordinator = new SaveLoadRequestCoordinatorService();
        private readonly TitleReturnBoundaryLedgerService titleReturnBoundaryLedger = new TitleReturnBoundaryLedgerService();
        private readonly HookStatusPublicationQueue hookStatusQueue = new HookStatusPublicationQueue();
        private readonly PlayerDoctorRuntimeService playerDoctor = new PlayerDoctorRuntimeService();
        private readonly ModOwnerLedgerService modOwnerLedger = new ModOwnerLedgerService();
        private readonly ModOwnerLifecycleCoordinator modOwnerLifecycle = new ModOwnerLifecycleCoordinator();
        private readonly AdvancedHarmonySupervisor advancedHarmonySupervisor = new AdvancedHarmonySupervisor();
        private readonly List<Func<string>> runtimeReportContextProviders = new List<Func<string>>();
        private readonly List<Func<TitleReturnObjectGraphSection>> titleReturnObjectGraphProviders = new List<Func<TitleReturnObjectGraphSection>>();
        private readonly List<IModOwnerCleanupParticipant> modOwnerCleanupParticipants = new List<IModOwnerCleanupParticipant>();
        private readonly HashSet<string> processLifetimeProviderIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loadedAssemblyOwnerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> refactorScaffoldWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool refactorScaffoldWarningKeysCapped;
        private readonly DateTimeOffset startedAt = DateTimeOffset.Now;
        private DateTimeOffset lastSecondTick = DateTimeOffset.Now;
        private RuntimeSubsystemOptions refactorOptions = RuntimeSubsystemOptions.Default();
        private ShadowContentRegistrySnapshot? latestShadowContentRegistrySnapshot;
        private ContentManifestRegistrySnapshot? latestContentManifestRegistrySnapshot;
        private IReadOnlyList<string> latestManifestScannerErrors = Array.Empty<string>();
        private IReadOnlyList<string> latestManifestScannerWarnings = Array.Empty<string>();
        private ModScannerDiagnosticTotals latestManifestScannerDiagnosticTotals = new ModScannerDiagnosticTotals(0, 0, 0, 0, 0);
        private NativeWorkshopSubscriptionSnapshot nativeWorkshopSubscriptions = NativeWorkshopSubscriptionSnapshot.Unavailable("GameBridge has not captured the native subscription owner yet.");
        private AuthorSourceSelectionState? latestAuthorSourceState;
        private IReadOnlyList<AuthorSourceSelectionDecision> latestAuthorSourceSelectionDecisions = Array.Empty<AuthorSourceSelectionDecision>();
        private AuthorSessionHost? authorSessionHost;
        private AuthorSessionDescriptorLoadResult? authorSessionDescriptorLoad;
        private Func<AuthorSessionRequest, AuthorSessionOperationResult>? authorSessionOperationHandler;
        private string latestSourceSelectionSummary = "not-run";
        private string currentRuntimePhase = "Constructing";
        private SaveLoadObjectSnapshotMode saveLoadObjectSnapshotMode = SaveLoadObjectSnapshotMode.Lite;
        private ulong updateTick;
        private uint secondTick;
        private int? currentLoadingSlot;
        private int observedSaveLoadedCount;
        private bool authorSessionStartupTitleBoundarySeen;
        private int runtimeThreadId;
        private long lastPublishedHookStatusQueueRevision = -1;
        private long lastPublishedEventBoundaryRevision = -1;
        private string lastLoggedLifecycleBoundaryContractStatus = string.Empty;
        private bool started;
        private bool configPreviewSuccessStatusPublished;
        private Action<ModLoadCheckpoint>? modLoadCheckpointForTests;
        private Action<IMonitor>? modCompletionLogForTests;
        private Action? modTransactionBeginDiagnosticForTests;
        private Action? modTransactionCommitDiagnosticForTests;
        private bool advancedHarmonyParticipantRegistered;

        public DtmApiRuntime(IRuntimeHost host, IDtmConfigMenuApi? configMenuApi = null)
        {
            this.host = host;
            this.configMenuApi = configMenuApi;
            configMenuRuntime = configMenuApi as IConfigMenuRuntime;
            runtimeThreadId = Thread.CurrentThread.ManagedThreadId;
            Paths = new RuntimePaths(host.GamePath, host.PluginPath);
            Diagnostics = new DiagnosticsService(Paths);
            Events = new EventManager(
                Diagnostics,
                () => refactorOptions.EventMainThreadBoundary,
                () => refactorOptions.EventHandlerQuarantine,
                () => runtimeThreadId,
                () => currentRuntimePhase,
                RecordModOwnerRegistration,
                RecordModOwnerCleanup,
                listenerTransition: OnEventListenerTransition,
                eventHandlerTimingEnabled: () => refactorOptions.EventHandlerTimingDiagnostics);
            Config = new ConfigService(Paths, Diagnostics);
            ModRegistry = new ModRegistryService(RecordModOwnerRegistration, RecordModOwnerCleanup);
            Workshop = new WorkshopService();
            Content = new ContentQueryService(Paths);
            Input = new InputService(() => refactorOptions.OwnerBoundInput, RecordModOwnerRegistration, RecordModOwnerCleanup);
            CustomEntities = new CustomEntityRegistryService(Diagnostics, RecordModOwnerRegistration);
            configMenuRuntime?.ConfigureDiagnostics(RecordModOwnerRegistration, RecordModOwnerCleanup, RecordConfigPreviewAudit);
            UI = new UiRuntimeService(
                ExportLogs,
                Events.DispatchMenuOpened,
                Events.DispatchMenuClosed,
                Diagnostics.RecordError,
                (message, level) => RuntimeMonitor.Log(message, level));
            UI.ManagerModelProvider = new DtmManagerRuntimeModelProvider(
                this,
                ExportLogs,
                () => ManagerInstallStateSummary.FromRuntimePaths(Paths),
                () => LatestContentManifestRegistrySnapshot);
        }

        public RuntimePaths Paths { get; }
        public DiagnosticsService Diagnostics { get; }
        public UiRuntimeService UI { get; }
        internal IConfigMenuRuntime? ConfigMenuRuntime => configMenuRuntime;
        internal EventManager Events { get; }
        internal ConfigService Config { get; }
        internal ModRegistryService ModRegistry { get; }
        internal WorkshopService Workshop { get; }
        internal ContentQueryService Content { get; }
        internal string SaveLoadObjectSnapshotModeForDiagnostics => saveLoadObjectSnapshotMode.ToString();
        internal InputService Input { get; }
        public CustomEntityRegistryService CustomEntities { get; }
        public IMonitor RuntimeMonitor { get; private set; } = NullMonitor.Instance;
        public IReadOnlyList<DiscoveredMod> DiscoveredMods => Volatile.Read(ref discoveredModsView);
        public IReadOnlyList<DiscoveredMod> LoadedMods => Volatile.Read(ref loadedModsView);
        public DateTimeOffset StartedAt => startedAt;
        internal RuntimeSubsystemOptions RefactorOptions => refactorOptions;
        internal LifecycleObservationSnapshot LifecycleObservationSnapshot => lifecycleObservation.GetSnapshot();
        internal LifecycleBoundaryContractSnapshot LifecycleBoundaryContractSnapshot => lifecycleBoundaryContract.GetSnapshot();
        internal ShadowContentRegistrySnapshot? LatestShadowContentRegistrySnapshot => latestShadowContentRegistrySnapshot;
        internal ContentManifestRegistrySnapshot? LatestContentManifestRegistrySnapshot => latestContentManifestRegistrySnapshot;
        internal ResourceLifecycleSnapshot ResourceLifecycleSnapshot => resourceLifecycleLedger.GetSnapshot();
        internal ContentRefreshGenerationService ContentRefreshGenerations => contentRefreshGenerations;
        internal RuntimeDemandCoordinator DemandCoordinator => demandCoordinator;
        internal RuntimeDemandSnapshot RuntimeDemandSnapshot => demandCoordinator.GetSnapshot();
        internal int RuntimeMemoryRecordCount => resourceLifecycleLedger.CurrentRecordCount;
        internal int RuntimeMemoryResourceSnapshotBuildCount => resourceLifecycleLedger.CurrentSnapshotBuildCount;

        internal ulong RuntimeUpdateTickCount => updateTick;

        internal bool RuntimeEventQueueHasPending => Events.HasQueuedDispatches;

        internal long RuntimeEventQueueDiagnosticRevision => Events.BoundaryDiagnosticRevision;

        internal bool RuntimeHookStatusQueueHasPending => hookStatusQueue.HasPending;

        internal long RuntimeHookStatusQueueDiagnosticRevision => hookStatusQueue.DiagnosticRevision;
        internal int RuntimeMemoryOwnerRootCount
        {
            get
            {
                InputOwnerSnapshot input = Input.GetOwnerSnapshot();
                EventHandlerCleanupSnapshot events = Events.GetHandlerCleanupSnapshot();
                int configPages = configMenuRuntime?.GetPages().Count ?? 0;
                int customEntities = CustomEntities.GetAnimalSnapshot().RegisteredDefinitionCount + CustomEntities.GetAnimalSnapshot().ActiveRuntimeInstanceCount +
                    CustomEntities.GetMonsterSnapshot().RegisteredDefinitionCount + CustomEntities.GetMonsterSnapshot().ActiveRuntimeInstanceCount +
                    CustomEntities.GetAttackSnapshot().RegisteredDefinitionCount + CustomEntities.GetAttackSnapshot().ActiveRuntimeInstanceCount +
                    CustomEntities.GetDroneSnapshot().RegisteredDefinitionCount + CustomEntities.GetDroneSnapshot().ActiveRuntimeInstanceCount;
                return input.OwnerRegistrations + events.ActiveHandlers + configPages + Config.TotalMigrationCount +
                    ModRegistry.TotalRootCount + customEntities + demandCoordinator.DemandEntryCount;
            }
        }
        internal SaveLoadRequestSnapshot SaveLoadRequestSnapshot => saveLoadRequestCoordinator.GetSnapshot();
        internal TitleReturnBoundarySnapshot TitleReturnBoundarySnapshot => titleReturnBoundaryLedger.GetSnapshot();
        internal HookStatusQueueSnapshot HookStatusQueueSnapshot => hookStatusQueue.GetSnapshot();
        internal EventDispatchBoundarySnapshot EventDispatchBoundarySnapshot => Events.GetBoundarySnapshot();
        internal IReadOnlyList<AuthorSourceSelectionDecision> AuthorSourceSelectionDecisions => latestAuthorSourceSelectionDecisions;
        internal bool IsAuthorSessionActive
        {
            get
            {
                AuthorSessionHost? host = authorSessionHost;
                return host != null && DateTimeOffset.UtcNow < host.ExpiresAtUtc && host.GetSnapshot().Running;
            }
        }
        internal AuthorSessionHostSnapshot? AuthorSessionSnapshot => authorSessionHost?.GetSnapshot();

        internal void ConfigureAuthorSessionHandler(Func<AuthorSessionRequest, AuthorSessionOperationResult> handler)
        {
            authorSessionOperationHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        internal void UpdateNativeWorkshopSubscriptions(bool available, string source, string failure, IEnumerable<NativeWorkshopSubscription> subscriptions)
        {
            NativeWorkshopSubscriptionSnapshot snapshot = available
                ? NativeWorkshopSubscriptionSnapshot.Captured(source, failure, subscriptions ?? Array.Empty<NativeWorkshopSubscription>())
                : NativeWorkshopSubscriptionSnapshot.Unavailable(failure);
            nativeWorkshopSubscriptions = snapshot;
            try
            {
                AuthorSourceStateStore.WriteWorkshopSnapshot(Paths.GamePath, snapshot);
            }
            catch (Exception ex)
            {
                RuntimeMonitor.Log("Failed to persist native Workshop subscription snapshot: " + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            }
            RuntimeMonitor.Log("Native Workshop subscription snapshot " + snapshot.FormatSummary() + ".");
        }
        internal ModOwnerLedgerSnapshot ModOwnerLedgerSnapshot => modOwnerLedger.GetSnapshot();
        internal void ConfigureModLoadCheckpointForTests(Action<ModLoadCheckpoint>? checkpoint) => modLoadCheckpointForTests = checkpoint;
        internal void ConfigureModCompletionLogForTests(Action<IMonitor>? completionLog) => modCompletionLogForTests = completionLog;
        internal void ConfigureAdvancedHarmonyInspectorForTests(IAdvancedHarmonyInspector inspector) => advancedHarmonySupervisor.ConfigureInspectorForTests(inspector);
        internal void ConfigureModTransactionDiagnosticsForTests(Action? begin, Action? commit)
        {
            modTransactionBeginDiagnosticForTests = begin;
            modTransactionCommitDiagnosticForTests = commit;
        }
        internal bool OwnerRequiresRestart(string ownerId) => modOwnerLifecycle.RequiresRestart(ownerId);
        internal bool ResourceLifecycleCleanupEnabled => refactorOptions.ResourceLifecycleCleanup;
        internal bool ResourceLifecycleTitleAssetReleaseEnabled => refactorOptions.ResourceLifecycleTitleAssetRelease;
        internal int ResourceLifecycleContentGeneration => resourceLifecycleLedger.GetSnapshot().ContentGeneration;
        internal int ResourceLifecycleSaveGeneration => resourceLifecycleLedger.GetSnapshot().SaveGeneration;
        internal bool IsRuntimeThread => Thread.CurrentThread.ManagedThreadId == runtimeThreadId;
        internal int RuntimeThreadId => runtimeThreadId;
        internal string CurrentRuntimePhase => currentRuntimePhase;
        public event Action<int?, bool>? SaveSessionLoaded
        {
            add => saveSessionLoadedBoundary.Add(value);
            remove => saveSessionLoadedBoundary.Remove(value);
        }

        public event Action? ReturnedToTitleBoundary
        {
            add => returnedToTitleBoundary.Add(value);
            remove => returnedToTitleBoundary.Remove(value);
        }

        internal event Action<int, bool>? NativeLoadGameReturned
        {
            add => nativeLoadGameReturnedBoundary.Add(value);
            remove => nativeLoadGameReturnedBoundary.Remove(value);
        }

        internal event Action? NativeGameFrame
        {
            add => nativeGameFrameBoundary.Add(value);
            remove => nativeGameFrameBoundary.Remove(value);
        }

        internal event Action? LogExportBoundary
        {
            add => logExportBoundary.Add(value);
            remove => logExportBoundary.Remove(value);
        }

        internal event Action<string>? RuntimeShutdownBoundary
        {
            add => runtimeShutdownBoundary.Add(value);
            remove => runtimeShutdownBoundary.Remove(value);
        }

        private void OnEventListenerTransition(EventListenerTransition transition)
        {
            if (transition == null || string.IsNullOrWhiteSpace(transition.EventName) || string.IsNullOrWhiteSpace(transition.Owner))
                return;

            string capabilityId = "Event." + transition.EventName;
            demandCoordinator.RegisterCapability(new RuntimeCapabilityDescriptor(
                capabilityId,
                RuntimeCapabilityOutcome.DemandActivated,
                "publication-only",
                string.Empty,
                "Owned event subscription demand; zero listeners bypass EventArgs, native preparation and queue work."));

            string reason = transition.Reason + " " + transition.PreviousCount.ToString(CultureInfo.InvariantCulture) +
                "->" + transition.CurrentCount.ToString(CultureInfo.InvariantCulture) +
                "; owner=" + transition.PreviousOwnerCount.ToString(CultureInfo.InvariantCulture) +
                "->" + transition.CurrentOwnerCount.ToString(CultureInfo.InvariantCulture);
            demandCoordinator.SetDemand(
                capabilityId,
                transition.Owner,
                RuntimeDemandSourceType.EventSubscription,
                RuntimeDemandLifetime.Owner,
                "listener",
                transition.CurrentOwnerCount,
                reason);
        }

        private bool RejectOffThreadRuntimeProducer(string eventName, string producerOwner)
        {
            if (!refactorOptions.EventMainThreadBoundary || IsRuntimeThread)
                return false;

            Events.RecordRejectedExternalEvent(
                eventName,
                producerOwner,
                "Runtime/native lifecycle producers must be rejected before mutating runtime, save, title, content, UI, or Unity-facing state.");
            RuntimeMonitor.LogOnce(
                "off-thread-runtime-producer-" + eventName,
                "Rejected off-thread runtime producer before state mutation. event=" + eventName +
                    " producer=" + producerOwner +
                    " thread=" + Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture) + ".",
                LogLevel.Warn);
            return true;
        }

        private void InvokeRuntimeBoundary(RuntimeBoundarySlot<Action> boundary, string boundaryName)
        {
            Action[] handlers = boundary.Snapshot;
            for (int index = 0; index < handlers.Length; index++)
            {
                Action handler = handlers[index];
                try
                {
                    handler();
                }
                catch (Exception ex)
                {
                    RecordRuntimeBoundaryFailure(boundaryName, handler, ex);
                }
            }
        }

        private void InvokeRuntimeBoundary<T>(RuntimeBoundarySlot<Action<T>> boundary, string boundaryName, T value)
        {
            Action<T>[] handlers = boundary.Snapshot;
            for (int index = 0; index < handlers.Length; index++)
            {
                Action<T> handler = handlers[index];
                try
                {
                    handler(value);
                }
                catch (Exception ex)
                {
                    RecordRuntimeBoundaryFailure(boundaryName, handler, ex);
                }
            }
        }

        private void InvokeRuntimeBoundary<TFirst, TSecond>(
            RuntimeBoundarySlot<Action<TFirst, TSecond>> boundary,
            string boundaryName,
            TFirst first,
            TSecond second)
        {
            Action<TFirst, TSecond>[] handlers = boundary.Snapshot;
            for (int index = 0; index < handlers.Length; index++)
            {
                Action<TFirst, TSecond> handler = handlers[index];
                try
                {
                    handler(first, second);
                }
                catch (Exception ex)
                {
                    RecordRuntimeBoundaryFailure(boundaryName, handler, ex);
                }
            }
        }

        private void RecordRuntimeBoundaryFailure(string boundaryName, Delegate handler, Exception ex)
        {
            string target = handler.Method.DeclaringType?.FullName ?? handler.Method.Name;
            Diagnostics.RecordError(
                "DTMAPI.Runtime",
                boundaryName + " boundary listener failed; later listeners remain eligible.",
                "handler=" + target + "." + handler.Method.Name + "; " + ex);
        }

        internal void AddRuntimeReportContextProvider(Func<string> provider)
        {
            if (provider == null)
                return;

            if (!runtimeReportContextProviders.Contains(provider))
                runtimeReportContextProviders.Add(provider);
        }

        internal void AddTitleReturnObjectGraphProvider(Func<TitleReturnObjectGraphSection> provider)
        {
            if (provider == null)
                return;

            if (!titleReturnObjectGraphProviders.Contains(provider))
                titleReturnObjectGraphProviders.Add(provider);
        }

        internal void RegisterModOwnerCleanupParticipant(IModOwnerCleanupParticipant participant)
        {
            if (participant == null)
                throw new ArgumentNullException(nameof(participant));
            string participantId = string.IsNullOrWhiteSpace(participant.ParticipantId)
                ? participant.GetType().FullName ?? participant.GetType().Name
                : participant.ParticipantId.Trim();
            if (modOwnerCleanupParticipants.Any(existing => existing.ParticipantId.Equals(participantId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("A mod-owner cleanup participant is already registered as " + participantId + ".");
            modOwnerCleanupParticipants.Add(participant);
        }

        internal ModOwnerParticipantCleanupSummary CleanupModOwnerParticipants(string ownerId, ModOwnerCleanupReason reason)
        {
            ownerId = string.IsNullOrWhiteSpace(ownerId) ? "unknown" : ownerId.Trim();
            var entries = new List<ModOwnerParticipantCleanupEntry>(modOwnerCleanupParticipants.Count);
            foreach (IModOwnerCleanupParticipant participant in modOwnerCleanupParticipants)
            {
                string participantId = string.IsNullOrWhiteSpace(participant.ParticipantId)
                    ? participant.GetType().FullName ?? participant.GetType().Name
                    : participant.ParticipantId.Trim();
                try
                {
                    ModOwnerCleanupParticipantResult result = participant.RemoveOwner(ownerId, reason);
                    int participantFailures = Math.Max(result.FailureCount, result.RemainingResources > 0 ? 1 : 0);
                    entries.Add(new ModOwnerParticipantCleanupEntry(participantId, result.RemovedResources, success: participantFailures == 0, result.Details, result.RemainingResources, participantFailures));
                    if (result.RemovedResources > 0)
                        RecordModOwnerCleanup(ownerId, "GameBridgeOwnerResource", result.RemovedResources, participantId + ": " + result.Details);
                    if (result.RemainingResources > 0 || result.FailureCount > 0)
                        RecordModOwnerCleanupFailure(ownerId, "GameBridgeOwnerResource", participantId + ": " + result.Details, participantFailures);
                }
                catch (Exception ex)
                {
                    entries.Add(new ModOwnerParticipantCleanupEntry(participantId, 0, success: false, ex.GetType().Name + ": " + ex.Message));
                    RecordModOwnerCleanupFailure(ownerId, "GameBridgeOwnerResource", participantId + ": " + ex.GetType().Name + ": " + ex.Message);
                    try
                    {
                        RuntimeMonitor.Log("Mod-owner cleanup participant " + participantId + " failed owner=" + ownerId + " reason=" + reason + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
                    }
                    catch
                    {
                        // A diagnostic sink must not prevent later cleanup participants.
                    }
                }
            }

            int lifecycleRecordsRemoved = resourceLifecycleLedger.RemoveOwner(ownerId);
            if (lifecycleRecordsRemoved > 0)
                entries.Add(new ModOwnerParticipantCleanupEntry("DTMAPI.Core.ResourceLifecycleLedger", lifecycleRecordsRemoved, success: true, "Removed owner-scoped active lifecycle records."));

            var summary = new ModOwnerParticipantCleanupSummary(ownerId, reason, entries);
            try
            {
                RuntimeMonitor.Log("Mod-owner cleanup participants " + summary.FormatSummary() + ".", summary.FailureCount == 0 ? LogLevel.Info : LogLevel.Warn);
            }
            catch
            {
                // Participant cleanup is authoritative; summary logging is best-effort.
            }
            return summary;
        }

        internal void ConfigureSaveLoadObjectSnapshotMode(string? mode, string source)
        {
            SaveLoadObjectSnapshotMode parsed = ParseSaveLoadObjectSnapshotMode(mode);
            saveLoadObjectSnapshotMode = parsed;
            RuntimeMonitor.Log(
                "SaveLoad object snapshot mode configured mode=" + parsed +
                " source=" + (source ?? string.Empty) +
                " smokeOnly=true.");
        }

        internal string SuppressOwnerRoots(string[] targetOwners, string[] targetRootTypes)
        {
            if (targetOwners == null || targetOwners.Length == 0)
                throw new ArgumentException("At least one owner is required.", nameof(targetOwners));
            if (targetRootTypes == null || targetRootTypes.Length == 0)
                throw new ArgumentException("At least one owner-root type is required.", nameof(targetRootTypes));
            string[] owners = targetOwners.Where(owner => !string.IsNullOrWhiteSpace(owner)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            string[] rootTypes = targetRootTypes.Where(type => !string.IsNullOrWhiteSpace(type)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (owners.Length == 0 || rootTypes.Length == 0)
                throw new ArgumentException("Owner and owner-root type values must be non-empty.");

            Dictionary<string, OwnerRootIsolationCounts> before = CaptureOwnerRootCounts(owners);
            int inputButtonsRemoved = 0;
            int eventHandlersRemoved = 0;
            int configPagesRemoved = 0;

            foreach (string owner in owners)
            {
                if (rootTypes.Contains("InputButton", StringComparer.OrdinalIgnoreCase))
                    inputButtonsRemoved += Input.RemoveOwner(owner);
                if (rootTypes.Contains("EventHandler", StringComparer.OrdinalIgnoreCase))
                    eventHandlersRemoved += Events.RemoveOwner(owner);
                if (rootTypes.Contains("ConfigPage", StringComparer.OrdinalIgnoreCase))
                    configPagesRemoved += configMenuRuntime?.RemoveOwner(owner) ?? 0;
            }

            Dictionary<string, OwnerRootIsolationCounts> after = CaptureOwnerRootCounts(owners);
            string unexpectedRemaining = FormatUnexpectedRemainingSuppressedRoots(after, rootTypes);
            string unexpectedDisabledCodeOwners = FormatUnexpectedDisabledCodeOwners(owners);
            bool suppressionSucceeded = string.Equals(unexpectedRemaining, "none", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(unexpectedDisabledCodeOwners, "none", StringComparison.OrdinalIgnoreCase);

            string summary = "targetOwners=" + string.Join("|", owners.Select(SanitizeMetricValue)) +
                "; targetRootTypes=" + string.Join("|", rootTypes.Select(SanitizeMetricValue)) +
                "; removed={InputButton=" + inputButtonsRemoved.ToString(CultureInfo.InvariantCulture) +
                "; EventHandler=" + eventHandlersRemoved.ToString(CultureInfo.InvariantCulture) +
                "; ConfigPage=" + configPagesRemoved.ToString(CultureInfo.InvariantCulture) + "}" +
                "; before={" + FormatOwnerRootCounts(before) + "}" +
                "; after={" + FormatOwnerRootCounts(after) + "}" +
                "; suppressionSucceeded=" + suppressionSucceeded.ToString(CultureInfo.InvariantCulture) +
                "; unexpectedRemainingSuppressedRoots=" + unexpectedRemaining +
                "; unexpectedDisabledCodeOwners=" + unexpectedDisabledCodeOwners;
            RuntimeMonitor.Log("Owner-root suppression " + summary + ".");
            PublishModOwnerLifecycleStatus("SuppressOwnerRoots");
            return summary;
        }

        public void RecordSaveLoadedActivationBreadcrumb(string step, Stopwatch stopwatch)
        {
            string safeStep = string.IsNullOrWhiteSpace(step) ? "unknown" : step.Trim();
            long elapsedMs = stopwatch == null ? 0 : stopwatch.ElapsedMilliseconds;
            string requestId = saveLoadRequestCoordinator.GetCurrentRequestIdForDiagnostics();
            string boundaryId = titleReturnBoundaryLedger.GetCurrentBoundaryIdForDiagnostics();
            long totalMemory = GC.GetTotalMemory(false);
            RuntimeMonitor.Log(
                "SaveLoaded.Step=" + safeStep +
                " elapsedMs=" + elapsedMs.ToString(CultureInfo.InvariantCulture) +
                " gc0=" + GC.CollectionCount(0).ToString(CultureInfo.InvariantCulture) +
                " gc1=" + GC.CollectionCount(1).ToString(CultureInfo.InvariantCulture) +
                " gc2=" + GC.CollectionCount(2).ToString(CultureInfo.InvariantCulture) +
                " totalMemory=" + totalMemory.ToString(CultureInfo.InvariantCulture) +
                " requestId=" + (string.IsNullOrWhiteSpace(requestId) ? "none" : requestId) +
                " boundaryId=" + (string.IsNullOrWhiteSpace(boundaryId) ? "none" : boundaryId) +
                " slot=" + (currentLoadingSlot.HasValue ? currentLoadingSlot.Value.ToString(CultureInfo.InvariantCulture) : "none") +
                " phase=" + currentRuntimePhase + ".");
        }

        public void RecordNativeLoadContinuationBreadcrumb(string method, string phase, long elapsedMs, string? exceptionType = null)
        {
            string safeMethod = string.IsNullOrWhiteSpace(method) ? "unknown" : method.Trim();
            string safePhase = string.IsNullOrWhiteSpace(phase) ? "unknown" : phase.Trim();
            string requestId = saveLoadRequestCoordinator.GetCurrentRequestIdForDiagnostics();
            string boundaryId = titleReturnBoundaryLedger.GetCurrentBoundaryIdForDiagnostics();
            SaveLoadRequestDiagnosticCounts counts = saveLoadRequestCoordinator.GetDiagnosticCounts();
            string threadName = Thread.CurrentThread.Name ?? string.Empty;
            RuntimeMonitor.Log(
                "NativeContinuation.Step=" + safeMethod + "." + safePhase +
                " method=" + safeMethod +
                " phase=" + safePhase +
                " elapsedMs=" + Math.Max(0, elapsedMs).ToString(CultureInfo.InvariantCulture) +
                " gc0=" + GC.CollectionCount(0).ToString(CultureInfo.InvariantCulture) +
                " gc1=" + GC.CollectionCount(1).ToString(CultureInfo.InvariantCulture) +
                " gc2=" + GC.CollectionCount(2).ToString(CultureInfo.InvariantCulture) +
                " totalMemory=" + GC.GetTotalMemory(false).ToString(CultureInfo.InvariantCulture) +
                " requestId=" + (string.IsNullOrWhiteSpace(requestId) ? "none" : requestId) +
                " boundaryId=" + (string.IsNullOrWhiteSpace(boundaryId) ? "none" : boundaryId) +
                " slot=" + (currentLoadingSlot.HasValue ? currentLoadingSlot.Value.ToString(CultureInfo.InvariantCulture) : "none") +
                " runtimePhase=" + currentRuntimePhase +
                " threadId=" + Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture) +
                " threadName=" + (string.IsNullOrWhiteSpace(threadName) ? "none" : SingleLine(threadName)) +
                " nativeEnter=" + counts.NativeEnterCount.ToString(CultureInfo.InvariantCulture) +
                " nativeReturn=" + counts.NativeReturnCount.ToString(CultureInfo.InvariantCulture) +
                " saveLoaded=" + counts.SaveLoadedDispatchCount.ToString(CultureInfo.InvariantCulture) +
                " exceptionType=" + (string.IsNullOrWhiteSpace(exceptionType) ? "none" : SingleLine(exceptionType)) +
                ".");
        }

        public void Start()
        {
            if (started)
                return;
            started = true;
            runtimeThreadId = Thread.CurrentThread.ManagedThreadId;
            currentRuntimePhase = "Startup";
            Stopwatch startup = Stopwatch.StartNew();
            Paths.Ensure();
            string latestLog = Path.Combine(Paths.LogsPath, "latest.log");
            string? logRotationWarning = LatestLogRotator.TryRotateLatest(latestLog);
            Diagnostics.LatestLogPath = latestLog;
            RuntimeMonitor = new FileMonitor(host, "DTMAPI", latestLog);
            if (!string.IsNullOrWhiteSpace(logRotationWarning))
                RuntimeMonitor.Log("DTMAPI latest log rotation warning: " + logRotationWarning, LogLevel.Warn);
            RuntimeMonitor.Log("DTMAPI runtime starting.");
            RuntimeMonitor.Log("Startup segment Core.PathsAndLog elapsedMs=" + startup.ElapsedMilliseconds + ".");
            RuntimeMonitor.Log("Host = " + host.HostName);
            RuntimeMonitor.Log("GamePath = " + Paths.GamePath);
            RuntimeMonitor.Log("ModsPath = " + Paths.ModsPath);
            InitializeAuthorSession();
            LoadRefactorScaffoldOptions();
            ObserveLifecycle("Startup", currentLoadingSlot, null);

            IManifest runtimeManifest = CreateRuntimeManifest();
            RegisterRuntimeApi<IDtmDiagnosticsApi>(runtimeManifest, this);
#pragma warning disable CS0618 // The DTMAPI Core provider intentionally retains the frozen CustomEntity ABI.
            RegisterRuntimeApi<ICustomAnimalApi>(runtimeManifest, CustomEntities, OwnerBoundCustomEntityApis.ForAnimal(CustomEntities));
            RegisterRuntimeApi<ICustomMonsterApi>(runtimeManifest, CustomEntities, OwnerBoundCustomEntityApis.ForMonster(CustomEntities));
            RegisterRuntimeApi<ICustomAttackApi>(runtimeManifest, CustomEntities, OwnerBoundCustomEntityApis.ForAttack(CustomEntities));
            RegisterRuntimeApi<ICustomDroneApi>(runtimeManifest, CustomEntities, OwnerBoundCustomEntityApis.ForDrone(CustomEntities));
#pragma warning restore CS0618
            if (configMenuApi != null)
            {
                IManifest configMenuManifest = CreateConfigMenuManifest();
                RegisterRuntimeApi<IDtmConfigMenuApi>(configMenuManifest, configMenuApi);
            }

            DiscoverMods();
            long afterDiscovery = startup.ElapsedMilliseconds;
            LoadMods(initialLoad: true);
            RuntimeMonitor.Log("Startup segment ModLoad elapsedMs=" + (startup.ElapsedMilliseconds - afterDiscovery) + " totalMs=" + startup.ElapsedMilliseconds + ".");
            RunPlayerDoctorAtStartup();
            RefreshConfigPageLocks();
            SetHookStatus("GameLoop.GameLaunched", "verified", "DTMAPI.Core", "Dispatched after DTMAPI mod Entry completed.");
            Events.DispatchGameLaunched();
            FlushRuntimeQueues("Start.GameLaunched");
            RuntimeMonitor.Log("GameLaunched dispatched.");
            ObserveLifecycle("TitleObserved", currentLoadingSlot, null);
            FlushRuntimeQueues("Start.TitleObserved");
            RuntimeMonitor.Log("Startup segment Core.Start totalMs=" + startup.ElapsedMilliseconds + ".");
        }

        public void Update()
        {
            if (Thread.CurrentThread.ManagedThreadId != runtimeThreadId)
            {
                Events.RecordRejectedExternalEvent("GameLoop.UpdateTicked", "DTMAPI.Core", "TimerFallback must not deliver mod update callbacks.");
                RuntimeMonitor.LogOnce(
                    "timer-fallback-runtime-update-skipped",
                    "Skipped ordinary runtime.Update dispatch from a non-runtime thread; TimerFallback must not deliver mod update callbacks.",
                    LogLevel.Warn);
                return;
            }

            currentRuntimePhase = "Update";
            try
            {
                ProcessAuthorSessionRequests();
                FlushRuntimeQueues("Update.Begin");
                updateTick++;
                Events.DispatchUpdateTicked(updateTick);
                DateTimeOffset now = DateTimeOffset.Now;
                if ((now - lastSecondTick).TotalSeconds >= 1)
                {
                    secondTick++;
                    lastSecondTick = now;
                    Events.DispatchOneSecondUpdateTicked(secondTick);
                    AuditAdvancedHarmonyOwners();
                }
                FlushRuntimeQueues("Update.End");
            }
            finally
            {
                Input.ClearFrame();
            }
        }

        private void AuditAdvancedHarmonyOwners()
        {
            IReadOnlyList<AdvancedHarmonyAuditIssue> issues;
            try
            {
                issues = advancedHarmonySupervisor.AuditActiveOwners();
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError(
                    "DTMAPI.AdvancedHarmony",
                    "Advanced Harmony late audit failed.",
                    "advanced-harmony-unavailable: " + ex.GetType().Name + ": " + ex.Message);
                RuntimeMonitor.Log("Advanced Harmony late audit failed: " + ex.GetType().Name + ": " + ex.Message, LogLevel.Error);
                return;
            }

            foreach (AdvancedHarmonyAuditIssue issue in issues)
            {
                string diagnosticOwner = string.IsNullOrWhiteSpace(issue.OwnerId) ? "DTMAPI.AdvancedHarmony" : issue.OwnerId;
                Diagnostics.RecordError(
                    diagnosticOwner,
                    "Advanced Harmony supervision violation.",
                    issue.Code + ": " + issue.Details);
                RuntimeMonitor.Log(
                    "Advanced Harmony supervision violation owner=" + diagnosticOwner + "; code=" + issue.Code + "; " + issue.Details,
                    LogLevel.Error);
                if (!string.IsNullOrWhiteSpace(issue.OwnerId) && modOwnerLifecycle.IsActive(issue.OwnerId))
                    DeactivateOwner(issue.OwnerId, ModOwnerCleanupReason.EntryFailed, shutdown: false, transactionId: string.Empty);
            }
        }

        internal IReadOnlyList<string> GetInputButtonsToSample() => Input.GetButtonsToSample(CreateInputAudienceSnapshot());

        internal InputFrameResult RecordInputFrame(IReadOnlyList<InputButtonSample> buttonSamples)
        {
            if (refactorOptions.EventMainThreadBoundary && !IsRuntimeThread)
            {
                Events.RecordRejectedExternalEvent("Input.Frame", "DTMAPI.Core.Input", "Input frames must originate on the runtime thread.");
                return new InputFrameResult(0, 0, 0, 0, 0);
            }

            InputAudienceSnapshot audience = CreateInputAudienceSnapshot();
            return Input.RecordFrame(
                audience,
                buttonSamples,
                dispatch =>
                {
                    RuntimeMonitor.Log("Input " + dispatch.Button + " pressed dispatched to DTMAPI audience=" + audience.Mode + " owner=" + (dispatch.TargetOwnerId.Length == 0 ? "broadcast" : dispatch.TargetOwnerId) + ".");
                    return dispatch.IsBroadcast
                        ? Events.DispatchButtonPressedWithReceipt(dispatch.Button)
                        : Events.DispatchButtonPressedToOwnerWithReceipt(dispatch.TargetOwnerId, dispatch.Button);
                },
                dispatch =>
                {
                    RuntimeMonitor.Log("Input " + dispatch.Button + " released dispatched to DTMAPI audience=" + audience.Mode + " settlement=" + dispatch.IsSettlement + ".");
                    if (dispatch.IsSettlement)
                        Events.DispatchButtonReleasedToOwners(dispatch.RecipientOwnerIds, dispatch.Button);
                    else if (dispatch.IsBroadcast)
                        Events.DispatchButtonReleased(dispatch.Button);
                    else
                        Events.DispatchButtonReleasedToOwners(new[] { dispatch.TargetOwnerId }, dispatch.Button);
                },
                (keybind, targetOwnerId) =>
                {
                    RuntimeMonitor.Log("Keybind " + keybind.OwnerId + "/" + keybind.KeybindId + " pressed trigger=" + keybind.TriggerButton + " context=" + UI.InputContext + ".");
                    return targetOwnerId.Length == 0
                        ? Events.DispatchKeybindPressedWithReceipt(keybind.OwnerId, keybind.KeybindId, keybind.Keybinds, keybind.TriggerButton)
                        : Events.DispatchKeybindPressedToOwnerWithReceipt(targetOwnerId, keybind.OwnerId, keybind.KeybindId, keybind.Keybinds, keybind.TriggerButton);
                },
                dispatch =>
                {
                    InputKeybindDispatch keybind = dispatch.Keybind;
                    RuntimeMonitor.Log("Keybind " + keybind.OwnerId + "/" + keybind.KeybindId + " released trigger=" + keybind.TriggerButton + " context=" + UI.InputContext + ".");
                    if (dispatch.IsSettlement)
                        Events.DispatchKeybindReleasedToOwners(dispatch.RecipientOwnerIds, keybind.OwnerId, keybind.KeybindId, keybind.Keybinds, keybind.TriggerButton);
                    else if (dispatch.TargetOwnerId.Length == 0)
                        Events.DispatchKeybindReleased(keybind.OwnerId, keybind.KeybindId, keybind.Keybinds, keybind.TriggerButton);
                    else
                        Events.DispatchKeybindReleasedToOwners(new[] { dispatch.TargetOwnerId }, keybind.OwnerId, keybind.KeybindId, keybind.Keybinds, keybind.TriggerButton);
                });
        }

        internal bool TryDispatchLegacyModalButtonPressed(string menuId, string ownerId, string button)
        {
            menuId = string.IsNullOrWhiteSpace(menuId) ? string.Empty : menuId.Trim();
            ownerId = string.IsNullOrWhiteSpace(ownerId) ? string.Empty : ownerId.Trim();
            button = DtmButton.Normalize(button);
            if (!IsRuntimeThread || menuId.Length == 0 || ownerId.Length == 0 || button.Length == 0)
                return false;
            if (!UI.IsOpen || !UI.ActiveMenuId.Equals(menuId, StringComparison.OrdinalIgnoreCase) ||
                !UI.ActiveMenuOwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase))
                return false;
            if (GetCurrentInputScope() != DtmInputScope.SaveLoaded || Input.IsSuppressed(button))
                return false;
            if (!Input.HasOwnerLegacyButtonRegistration(ownerId, button) || Input.HasOwnerTypedKeybindForButton(ownerId, button))
                return false;

            int dispatchedHandlers = Events.DispatchButtonPressedToOwner(ownerId, button);
            if (dispatchedHandlers <= 0)
                return false;

            RuntimeMonitor.Log("Legacy modal input ButtonPressed dispatched owner=" + ownerId + " menu=" + menuId + " button=" + button + " handlers=" + dispatchedHandlers + ".");
            return true;
        }

        public bool IsInputDown(string button) => Input.IsDown(button);

        private DtmInputScope GetCurrentInputScope()
        {
            string context = UI.InputContext ?? string.Empty;
            if (context.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0 ||
                context.IndexOf("Home", StringComparison.OrdinalIgnoreCase) >= 0)
                return DtmInputScope.Title;
            return UI.GameplayHotkeysAllowed && !UI.IsOpen ? DtmInputScope.Gameplay : DtmInputScope.SaveLoaded;
        }

        private bool IsInputDispatchAllowed(DtmInputScope scope)
        {
            DtmInputScope currentScope = GetCurrentInputScope();
            if (scope == DtmInputScope.Always)
                return true;
            if (scope == DtmInputScope.Title)
                return currentScope == DtmInputScope.Title && !UI.IsOpen;
            if (scope == DtmInputScope.SaveLoaded)
                return currentScope == DtmInputScope.SaveLoaded || currentScope == DtmInputScope.Gameplay;
            if (scope == DtmInputScope.Gameplay)
                return currentScope == DtmInputScope.Gameplay && !UI.BlocksGameplayHotkeys;
            return false;
        }

        private InputAudienceSnapshot CreateInputAudienceSnapshot()
        {
            DtmInputScope scope = GetCurrentInputScope();
            if (UI.IsOpen)
            {
                return UI.ActiveMenuOwnerId.Length > 0
                    ? new InputAudienceSnapshot(InputAudienceMode.OwnerModal, UI.ActiveMenuOwnerId, scope, UI.OverlaySessionSequence)
                    : new InputAudienceSnapshot(InputAudienceMode.PlatformModal, string.Empty, scope, UI.OverlaySessionSequence);
            }

            if (!UI.GameplayHotkeysAllowed && scope != DtmInputScope.Title)
                return new InputAudienceSnapshot(InputAudienceMode.PlatformModal, string.Empty, scope, UI.OverlaySessionSequence);
            return new InputAudienceSnapshot(InputAudienceMode.Normal, string.Empty, scope, UI.OverlaySessionSequence);
        }

        public void NotifyReturnHomeRequested(string source)
        {
            RecordTitleReturnBoundaryEvent("ReturnHomeRequested", source, "DolocAPI.ReturnHome entered.", currentLoadingSlot);
            CaptureTitleReturnObjectGraphSnapshot("BeforeReturnHome", source, "Before native ReturnHome cleanup and title transition.", currentLoadingSlot);
        }

        public void NotifyReturnHomeNativePostfix(string source)
        {
            RecordTitleReturnBoundaryEvent("ReturnHomeNativePostfix", source, "DolocAPI.ReturnHome postfix entered.", currentLoadingSlot);
        }

        public void NotifyTitleStable(string source, string details)
        {
            RecordTitleReturnBoundaryEvent("TitleStable", source, details, currentLoadingSlot);
        }

        public void NotifyPreLoadForcedGcProbeStarting(string source, int slot, string details)
        {
            RecordTitleReturnBoundaryEvent("BeforePreLoadForcedGC", source, details, slot);
            CaptureTitleReturnObjectGraphSnapshot("BeforePreLoadForcedGC", source, details, slot);
        }

        public void NotifyPreLoadForcedGcProbeCompleted(string source, int slot, string details)
        {
            RecordTitleReturnBoundaryEvent("PreLoadForcedGC", source, details, slot);
            CaptureTitleReturnObjectGraphSnapshot("PreLoadForcedGC", source, details, slot);
        }

        public void NotifyLoadGameRequested(int slot)
        {
            if (RejectOffThreadRuntimeProducer("Save.LoadGameRequested", "DTMAPI.Core"))
                return;
            currentLoadingSlot = slot;
            RecordTitleReturnBoundaryEvent("BeforeNextLoadGame", "Harmony LoadGame Prefix", "Before native LoadGame request is recorded.", slot);
            CaptureTitleReturnObjectGraphSnapshot("BeforeNextLoadGame", "Harmony LoadGame Prefix", "Before next native LoadGame enter.", slot);
            if (!refactorOptions.SaveLoadRequestCoordinator)
            {
                RuntimeMonitor.Log($"LoadGame requested for slot/index {slot}.");
                RecordTitleReturnBoundaryEvent("LoadGameNativeEnter", "Harmony LoadGame Prefix", "Native LoadGame entered without SaveLoad coordinator.", slot);
                CaptureTitleReturnObjectGraphSnapshot("LoadGameNativeEnter", "Harmony LoadGame Prefix", "After native LoadGame prefix enter without SaveLoad coordinator.", slot);
                return;
            }

            SaveLoadRequestUpdate update = saveLoadRequestCoordinator.RecordNativeEnter(
                slot,
                "NativeGame",
                "Harmony LoadGame Prefix",
                Thread.CurrentThread.ManagedThreadId,
                currentRuntimePhase);
            PublishSaveLoadRequestCoordinatorUpdate("NativeEnter", update);
            RecordTitleReturnBoundaryEvent("LoadGameNativeEnter", "Harmony LoadGame Prefix", "Native LoadGame entered. " + update.Snapshot.FormatSummary(), slot);
            CaptureTitleReturnObjectGraphSnapshot("LoadGameNativeEnter", "Harmony LoadGame Prefix", "After native LoadGame prefix enter.", slot);
            RuntimeMonitor.Log("LoadGame requested for slot/index " + slot.ToString(CultureInfo.InvariantCulture) + ". requestId=" + update.Snapshot.ActiveRequestId + ".");
        }

        public void NotifyLoadGameReturned(int slot, bool result)
        {
            if (RejectOffThreadRuntimeProducer("Save.LoadGameReturned", "DTMAPI.Core"))
                return;
            if (refactorOptions.SaveLoadRequestCoordinator)
            {
                SaveLoadRequestUpdate update = saveLoadRequestCoordinator.RecordNativeReturn(
                    slot,
                    result,
                    "NativeGame",
                    "Harmony LoadGame Postfix",
                    Thread.CurrentThread.ManagedThreadId,
                    currentRuntimePhase);
                PublishSaveLoadRequestCoordinatorUpdate("NativeReturn", update);
                RecordTitleReturnBoundaryEvent("LoadGameNativeReturn", "Harmony LoadGame Postfix", "Native LoadGame returned result=" + result.ToString(CultureInfo.InvariantCulture) + ". " + update.Snapshot.FormatSummary(), slot);
            }
            InvokeRuntimeBoundary(nativeLoadGameReturnedBoundary, "Native LoadGame return", slot, result);
        }

        internal void BeginOwnerEntry(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("Owner id is required.", nameof(ownerId));
            modOwnerLifecycle.BeginEntry(ownerId);
        }

        internal void ActivateOwnerEntry(IManifest manifest)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));
            ModRegistry.AddLoaded(manifest);
            modOwnerLifecycle.Activate(manifest.UniqueID);
        }

        internal bool HasOwnerInstance(string ownerId) => modInstances.ContainsKey(ownerId ?? string.Empty);

        internal void NotifyNativeGameFrame()
        {
            if (RejectOffThreadRuntimeProducer("GameLoop.NativeFrame", "DTMAPI.Core"))
                return;
            InvokeRuntimeBoundary(nativeGameFrameBoundary, "Native game frame");
        }

        internal bool ShouldSuppressDtmapiLoadGameRequest(int slot, string owner, string source, out string summary)
        {
            summary = string.Empty;
            currentLoadingSlot = slot;
            if (!refactorOptions.SaveLoadRequestCoordinator)
                return false;

            SaveLoadRequestDecision decision = saveLoadRequestCoordinator.TryBeginDtmapiRequest(
                slot,
                owner,
                source,
                Thread.CurrentThread.ManagedThreadId,
                currentRuntimePhase);
            summary = decision.Update.Snapshot.FormatSummary();
            PublishSaveLoadRequestCoordinatorUpdate(decision.Suppressed ? "DtmapiDuplicateSuppressed" : "DtmapiRequest", decision.Update);
            if (decision.Suppressed)
            {
                RuntimeMonitor.Log("Suppressed duplicate DTMAPI LoadGame request for slot/index " + slot.ToString(CultureInfo.InvariantCulture) +
                    ". requestId=" + decision.RequestId + " owner=" + (owner ?? string.Empty) + " source=" + (source ?? string.Empty) + ".");
            }
            return decision.Suppressed;
        }

        internal void NotifySaveLoadTimeout(string owner, string source, string details)
        {
            if (!refactorOptions.SaveLoadRequestCoordinator)
                return;

            PublishSaveLoadRequestCoordinatorUpdate(
                "Timeout",
                saveLoadRequestCoordinator.RecordTimeout(
                    owner,
                    source,
                    details,
                    Thread.CurrentThread.ManagedThreadId,
                    currentRuntimePhase));
        }

        public void NotifySaveLoaded(bool isNewGame)
        {
            if (RejectOffThreadRuntimeProducer("Save.SaveLoaded", "DTMAPI.Core"))
                return;
            Stopwatch breadcrumb = Stopwatch.StartNew();
            RecordSaveLoadedActivationBreadcrumb("Runtime.Enter", breadcrumb);
            currentRuntimePhase = RuntimeLifecyclePhase.SaveLoaded;
            RuntimeMonitor.Log($"SaveLoaded hook dispatched. slot/index={currentLoadingSlot?.ToString() ?? "unknown"} isNewGame={isNewGame}");
            if (refactorOptions.SaveLoadRequestCoordinator)
            {
                RecordSaveLoadedActivationBreadcrumb("Runtime.BeforeSaveLoadRecord", breadcrumb);
                SaveLoadRequestUpdate update = saveLoadRequestCoordinator.RecordSaveLoaded(
                        currentLoadingSlot,
                        isNewGame,
                        "NativeGame",
                        "DolocAPI.AfterLoadArchiveData",
                        Thread.CurrentThread.ManagedThreadId,
                        currentRuntimePhase);
                RecordSaveLoadedActivationBreadcrumb("Runtime.AfterSaveLoadRecord", breadcrumb);
                PublishSaveLoadRequestCoordinatorUpdate("SaveLoaded", update);
                RecordTitleReturnBoundaryEvent("SaveLoaded", "DolocAPI.AfterLoadArchiveData", "SaveLoaded dispatched isNewGame=" + isNewGame.ToString(CultureInfo.InvariantCulture) + ". " + update.Snapshot.FormatSummary(), currentLoadingSlot);
            }
            observedSaveLoadedCount++;
            // Save transitions invalidate frame-local/down-edge state and demand-local
            // snapshot watches, but persistent owner registrations remain process
            // services. Match ReturnedToTitle before any ordinary SaveLoaded callback
            // can observe stale input from the previous native environment.
            Input.ClearTransientState();
            RecordSaveLoadedActivationBreadcrumb("Runtime.BeforeLifecycleObservation", breadcrumb);
            ObserveLifecycle(RuntimeLifecyclePhase.SaveLoaded, currentLoadingSlot, isNewGame);
            CustomEntities.BeginSaveSession(currentLoadingSlot, isNewGame);
            RecordSaveLoadedActivationBreadcrumb("Runtime.AfterLifecycleObservation", breadcrumb);
            RecordSaveLoadedActivationBreadcrumb("Runtime.BeforeSaveSessionLoaded", breadcrumb);
            InvokeRuntimeBoundary(saveSessionLoadedBoundary, "Save session", currentLoadingSlot, isNewGame);
            RecordSaveLoadedActivationBreadcrumb("Runtime.AfterSaveSessionLoaded", breadcrumb);
            RecordSaveLoadedActivationBreadcrumb("Runtime.BeforeRuntimeEventDispatch", breadcrumb);
            Events.DispatchSaveLoaded(currentLoadingSlot, isNewGame);
            RecordSaveLoadedActivationBreadcrumb("Runtime.AfterRuntimeEventDispatch", breadcrumb);
            RecordSaveLoadedActivationBreadcrumb("Runtime.BeforeQueueFlush", breadcrumb);
            FlushRuntimeQueues("SaveLoaded");
            RecordSaveLoadedActivationBreadcrumb("Runtime.AfterQueueFlush", breadcrumb);
            RecordSaveLoadedActivationBreadcrumb("Runtime.BeforeObjectSnapshot", breadcrumb);
            CaptureTitleReturnObjectGraphSnapshot(
                "SaveLoaded",
                "DtmApiRuntime.NotifySaveLoaded",
                "After SaveLoaded runtime cleanup, public event dispatch, and queue flush.",
                currentLoadingSlot);
            RecordSaveLoadedActivationBreadcrumb("Runtime.AfterObjectSnapshot", breadcrumb);
            RecordSaveLoadedActivationBreadcrumb("Runtime.Exit", breadcrumb);
        }

        public void NotifySaveSaving(int? slot)
        {
            _ = TryNotifySaveSaving(slot);
        }

        public bool TryNotifySaveSaving(int? slot)
        {
            if (RejectOffThreadRuntimeProducer("Save.SaveSaving", "DTMAPI.Core"))
                return false;
            currentRuntimePhase = "SaveSaving";
            RuntimeMonitor.Log($"SaveSaving hook dispatched. slot/index={(slot ?? currentLoadingSlot)?.ToString() ?? "unknown"}");
            bool succeeded =
                Events.TryDispatchSaveSaving(
                    slot ?? currentLoadingSlot);
            FlushRuntimeQueues("SaveSaving");
            if (!succeeded)
            {
                RuntimeMonitor.Log(
                    "SaveSaving participant failure canceled the native SaveGame boundary.",
                    LogLevel.Error);
            }
            return succeeded;
        }

        public void NotifySaveSaved(int? slot)
        {
            if (RejectOffThreadRuntimeProducer("Save.SaveSaved", "DTMAPI.Core"))
                return;
            currentRuntimePhase = "SaveSaved";
            RuntimeMonitor.Log($"SaveSaved hook dispatched. slot/index={(slot ?? currentLoadingSlot)?.ToString() ?? "unknown"}");
            Events.DispatchSaveSaved(slot ?? currentLoadingSlot);
            FlushRuntimeQueues("SaveSaved");
        }

        public void NotifyReturnedToTitle()
        {
            if (RejectOffThreadRuntimeProducer("GameLoop.ReturnedToTitle", "DTMAPI.Core"))
                return;
            currentRuntimePhase = "ReturnedToTitle";
            AuthorSessionHostSnapshot? authorSnapshot = AuthorSessionSnapshot;
            bool retainForStartupTitleBoundary =
                !authorSessionStartupTitleBoundarySeen &&
                observedSaveLoadedCount == 0 &&
                currentLoadingSlot == null &&
                authorSnapshot != null &&
                authorSnapshot.Parsed == 0;
            authorSessionStartupTitleBoundarySeen = true;
            if (retainForStartupTitleBoundary)
            {
                RuntimeMonitor.Log(
                    "Explicit author session retained across the initial startup ReturnHome boundary; " +
                    "no save or authenticated request had been observed. The next ReturnedToTitle or shutdown still closes it.");
            }
            else
            {
                CloseAuthorSession("ReturnedToTitle");
            }
            RuntimeMonitor.Log("ReturnedToTitle hook dispatched.");
            RecordTitleReturnBoundaryEvent("ReturnedToTitleRuntimeStart", "DtmApiRuntime.NotifyReturnedToTitle", "Runtime ReturnedToTitle boundary started.", currentLoadingSlot);
            ObserveLifecycle("ReturnedToTitle", currentLoadingSlot, null);
            currentLoadingSlot = null;
            Input.ClearTransientState();
            CustomEntities.ClearRuntimeInstances("returned-to-title");
            InvokeRuntimeBoundary(returnedToTitleBoundary, "Returned-to-title");
            Events.DispatchReturnedToTitle();
            ObserveReturnedToTitleRuntimeState("ReturnedToTitle post-boundary");
            FlushRuntimeQueues("ReturnedToTitle");
            RecordTitleReturnBoundaryEvent("ReturnedToTitleRuntimeEnd", "DtmApiRuntime.NotifyReturnedToTitle", "Runtime ReturnedToTitle boundary completed.", currentLoadingSlot);
            CaptureTitleReturnObjectGraphSnapshot("AfterReturnedToTitleComplete", "DtmApiRuntime.NotifyReturnedToTitle", "After ReturnedToTitle runtime cleanup, public event dispatch, and queue flush.", currentLoadingSlot);
        }

        public void NotifyWorkshopModListChanged()
        {
            if (RejectOffThreadRuntimeProducer("Workshop.ModListChanged", "DTMAPI.Core"))
                return;
            currentRuntimePhase = "WorkshopModListChanged";
            DiscoverMods();
            int hotLoaded = LoadMods(initialLoad: false);
            RuntimeMonitor.Log($"Workshop ModListChanged hook dispatched. discoveredMods={discoveredMods.Count} hotLoaded={hotLoaded}");
            Events.DispatchWorkshopModListChanged(discoveredMods.Count);
            FlushRuntimeQueues("WorkshopModListChanged");
        }

        public void SetHookStatus(string hookId, string status, string source, string details)
        {
            if (Diagnostics.SetHookStatus(hookId, status, source, details))
            {
                if (refactorOptions.HookStatusQueue)
                {
                    hookStatusQueue.Enqueue(
                        hookId,
                        status,
                        source,
                        details,
                        currentRuntimePhase,
                        Thread.CurrentThread.ManagedThreadId,
                        runtimeThreadId);
                }
                else
                {
                    PublishHookStatusChange(new HookStatusPublication(
                        hookId ?? string.Empty,
                        status ?? string.Empty,
                        source ?? string.Empty,
                        details ?? string.Empty,
                        currentRuntimePhase,
                        Thread.CurrentThread.ManagedThreadId,
                        DateTimeOffset.Now));
                }
            }
        }

        internal void FlushRuntimeQueues(string reason)
        {
            if (refactorOptions.EventMainThreadBoundary && !IsRuntimeThread)
            {
                Events.RecordRejectedExternalEvent(
                    "Runtime.FlushQueues",
                    "DTMAPI.Core",
                    "Runtime queues must be flushed from the runtime thread.");
                return;
            }

            if (refactorOptions.HookStatusQueue)
            {
                IReadOnlyList<HookStatusPublication> publications = hookStatusQueue.HasPending
                    ? hookStatusQueue.Drain()
                    : Array.Empty<HookStatusPublication>();
                foreach (HookStatusPublication publication in publications)
                    PublishHookStatusChange(publication);
                HookStatusQueueSnapshot? queueSnapshot = PublishHookStatusQueueFeatureStatus(reason ?? string.Empty);
                if (publications.Count > 0)
                    RuntimeMonitor.Log("Refactor hook status queue flushed reason=" + (reason ?? string.Empty) + " count=" + publications.Count + ". " + (queueSnapshot ?? hookStatusQueue.GetSnapshot()).FormatSummary() + ".");
            }

            if (refactorOptions.EventMainThreadBoundary)
            {
                int flushedEvents = Events.HasQueuedDispatches ? Events.FlushQueuedDispatches() : 0;
                EventDispatchBoundarySnapshot? boundarySnapshot = PublishEventBoundaryFeatureStatus(reason ?? string.Empty);
                if (flushedEvents > 0)
                    RuntimeMonitor.Log("Refactor event main-thread boundary flushed reason=" + (reason ?? string.Empty) + " count=" + flushedEvents + ". " + (boundarySnapshot ?? Events.GetBoundarySnapshot()).FormatSummary() + ".");
            }
        }

        private void PublishHookStatusChange(HookStatusPublication publication)
        {
            Events.DispatchHookStatusChanged(publication.HookId, publication.Status);
            RuntimeMonitor.Log("Hook status: " + publication.HookId + " = " + publication.Status + ". " + publication.Details);
            ObserveLifecycleHookStatus(publication.HookId, publication.Status, publication.Source, publication.Details);
        }

        private HookStatusQueueSnapshot? PublishHookStatusQueueFeatureStatus(string operation, bool force = false)
        {
            if (!force && hookStatusQueue.DiagnosticRevision == lastPublishedHookStatusQueueRevision)
                return null;

            HookStatusQueueSnapshot snapshot = hookStatusQueue.GetSnapshot();
            if (!force && snapshot.Revision == lastPublishedHookStatusQueueRevision)
                return null;
            lastPublishedHookStatusQueueRevision = snapshot.Revision;
            Diagnostics.SetFeatureStatus(
                "Refactor.HookStatusQueue",
                refactorOptions.HookStatusQueue ? (snapshot.Success ? "queued-main-thread-flush" : "warning") : "disabled",
                operation ?? string.Empty,
                success: snapshot.Success,
                failureCount: snapshot.TotalDropped > int.MaxValue ? int.MaxValue : (int)snapshot.TotalDropped,
                lastError: snapshot.Success ? string.Empty : "Hook status publication queue dropped entries.",
                details: refactorOptions.HookStatusQueue ? snapshot.FormatSummary() : "Hook status queue is disabled by refactor-scaffold feature flag.");
            return snapshot;
        }

        private EventDispatchBoundarySnapshot? PublishEventBoundaryFeatureStatus(string operation, bool force = false)
        {
            if (!force && Events.BoundaryDiagnosticRevision == lastPublishedEventBoundaryRevision)
                return null;

            EventDispatchBoundarySnapshot snapshot = Events.GetBoundarySnapshot();
            if (!force && snapshot.Revision == lastPublishedEventBoundaryRevision)
                return null;
            lastPublishedEventBoundaryRevision = snapshot.Revision;
            bool success = snapshot.Success;
            long failureCount = snapshot.TotalRejected + snapshot.TotalDropped +
                snapshot.TotalQueueDispatchFailures + snapshot.TrimmedWarningKeyCount;
            Diagnostics.SetFeatureStatus(
                "Refactor.EventMainThreadBoundary",
                refactorOptions.EventMainThreadBoundary ? (success ? "main-thread-boundary" : "warning") : "disabled",
                operation ?? string.Empty,
                success: success,
                failureCount: failureCount > int.MaxValue ? int.MaxValue : (int)failureCount,
                lastError: success ? string.Empty : "Event boundary rejected, dropped, overflowed, failed, or trimmed a diagnostic publication.",
                details: refactorOptions.EventMainThreadBoundary ? snapshot.FormatSummary() : "Event main-thread boundary is disabled by refactor-scaffold feature flag.");
            return snapshot;
        }

        private string BeginModLoadTransaction(string ownerId)
        {
            return modOwnerLedger.BeginTransaction(ownerId, currentRuntimePhase, Thread.CurrentThread.ManagedThreadId);
        }

        private void CommitModLoadTransaction(string ownerId, string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                return;

            modOwnerLedger.CommitTransaction(ownerId, transactionId, currentRuntimePhase);
            PublishModLoadTransactionDiagnosticBestEffort(ownerId, transactionId, "CommitTransaction", modTransactionCommitDiagnosticForTests);
        }

        private void RollbackModLoadTransaction(string ownerId, string transactionId, string summary)
        {
            if (modOwnerLedger.RollbackTransaction(ownerId, transactionId, currentRuntimePhase, summary ?? string.Empty, out string rolledBackTransactionId))
                PublishModLoadTransactionDiagnosticBestEffort(ownerId, rolledBackTransactionId, "RollbackTransaction", null, summary);
        }

        private void PublishModLoadTransactionDiagnosticBestEffort(string ownerId, string transactionId, string operation, Action? testHook, string? details = null)
        {
            try
            {
                testHook?.Invoke();
                RuntimeMonitor.Log(
                    "Refactor mod load transaction " + operation + " owner=" + ownerId + " transactionId=" + transactionId +
                    (string.IsNullOrWhiteSpace(details) ? "." : ". " + details + "."));
                PublishModOwnerLifecycleStatus(operation);
            }
            catch (Exception ex)
            {
                try
                {
                    Diagnostics.RecordWarning(ownerId, "Mod load transaction diagnostic failed after state mutation.", operation + ": " + ex.GetType().Name + ": " + ex.Message);
                }
                catch
                {
                    // Transaction state mutation is authoritative; diagnostics are best-effort.
                }
            }
        }

        private void RecordModOwnerRegistration(string ownerId, string kind, string key, string details)
        {
            if (!refactorOptions.ModOwnerLedger)
                return;

            modOwnerLedger.RecordRegistration(ownerId, kind, key, currentRuntimePhase, details);
        }

        private void RecordModOwnerCleanup(string ownerId, string kind, int count, string details)
        {
            if (!refactorOptions.ModOwnerLedger)
                return;

            modOwnerLedger.RecordCleanup(ownerId, kind, count, currentRuntimePhase, details);
        }

        private void RecordModOwnerCleanupFailure(string ownerId, string kind, string details, int failureCount = 1)
        {
            if (!refactorOptions.ModOwnerLedger)
                return;

            try
            {
                modOwnerLedger.RecordCleanup(ownerId, kind, 0, currentRuntimePhase, details, success: false, failureCount: failureCount);
            }
            catch
            {
                // Owner cleanup is authoritative; diagnostics are best-effort.
            }
        }

        private void RecordModOwnerNeedsRestart(string ownerId, string key, string details)
        {
            if (!refactorOptions.ModOwnerLedger)
                return;

            modOwnerLedger.RecordNeedsRestart(ownerId, key, currentRuntimePhase, details);
        }

        private void RecordConfigPreviewAudit(string ownerId, string itemId, string kind, string operation, bool success, string details)
        {
            if (!refactorOptions.ConfigPreviewAudit || !refactorOptions.ModOwnerLedger)
                return;

            modOwnerLedger.RecordConfigPreview(ownerId, itemId, kind, operation, success, details);
            if (!success)
            {
                RuntimeMonitor.Log("Refactor config preview audit warning owner=" + ownerId + " item=" + itemId + " operation=" + operation + " " + details + ".", LogLevel.Warn);
                PublishModOwnerLifecycleStatus("ConfigPreviewAuditWarning");
            }
            else if (!configPreviewSuccessStatusPublished)
            {
                configPreviewSuccessStatusPublished = true;
                Diagnostics.SetFeatureStatus(
                    "Refactor.ConfigPreviewAudit",
                    "observing",
                    "ConfigPreviewAggregate",
                    success: true,
                    failureCount: 0,
                    lastError: string.Empty,
                    details: "Successful config preview scopes are aggregated; only bounded recent failures are retained.");
            }
        }

        private void PublishModOwnerLifecycleStatus(string operation)
        {
            if (!refactorOptions.ModOwnerLedger)
            {
                Diagnostics.SetFeatureStatus(
                    "Refactor.ModOwnerLifecycle",
                    "disabled",
                    operation ?? string.Empty,
                    success: true,
                    failureCount: 0,
                    lastError: string.Empty,
                    details: "Mod owner ledger is disabled by refactor-scaffold feature flag.");
                return;
            }

            ModOwnerLedgerSnapshot snapshot = modOwnerLedger.GetSnapshot();
            InputOwnerSnapshot inputSnapshot = Input.GetOwnerSnapshot();
            EventHandlerCleanupSnapshot eventSnapshot = Events.GetHandlerCleanupSnapshot();
            int configPreviewWarnings = snapshot.ConfigPreviewWarningCount;
            string summary = snapshot.FormatSummary() +
                "; input={" + inputSnapshot.FormatSummary() + "}" +
                "; events={" + eventSnapshot.FormatSummary() + "}" +
                "; ownerRegistrations={" + snapshot.FormatOwnerRegistrations() + "}";
            bool success = snapshot.Success && configPreviewWarnings == 0;

            Diagnostics.SetFeatureStatus(
                "Refactor.ModOwnerLifecycle",
                success ? "ok" : "warning",
                operation ?? string.Empty,
                success: success,
                failureCount: snapshot.CleanupFailures + configPreviewWarnings,
                lastError: success ? string.Empty : "Mod owner ledger has cleanup or config-preview warnings.",
                details: summary);
            Diagnostics.SetFeatureStatus(
                "Refactor.ModLoadTransaction",
                refactorOptions.ModLoadTransaction ? (snapshot.ActiveTransactions == 0 ? "closed" : "active") : "disabled",
                operation ?? string.Empty,
                success: !refactorOptions.ModLoadTransaction || snapshot.ActiveTransactions == 0,
                failureCount: snapshot.ActiveTransactions,
                lastError: snapshot.ActiveTransactions == 0 ? string.Empty : "A mod load transaction is still active.",
                details: refactorOptions.ModLoadTransaction ? snapshot.FormatSummary() : "Mod load transaction is disabled by refactor-scaffold feature flag.");
            Diagnostics.SetFeatureStatus(
                "Refactor.OwnerBoundInput",
                refactorOptions.OwnerBoundInput ? "ok" : "disabled",
                operation ?? string.Empty,
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: inputSnapshot.FormatSummary());
            Diagnostics.SetFeatureStatus(
                "Refactor.EventHandlerCleanup",
                refactorOptions.EventHandlerQuarantine ? "ok" : "disabled",
                operation ?? string.Empty,
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: eventSnapshot.FormatSummary());
            Diagnostics.SetFeatureStatus(
                "Refactor.ConfigPreviewAudit",
                refactorOptions.ConfigPreviewAudit ? (configPreviewWarnings == 0 ? "observing" : "warning") : "disabled",
                operation ?? string.Empty,
                success: configPreviewWarnings == 0,
                failureCount: configPreviewWarnings,
                lastError: configPreviewWarnings == 0 ? string.Empty : "Config preview setter/restore warning observed.",
                details: "configPreviewWarnings=" + configPreviewWarnings.ToString(CultureInfo.InvariantCulture) + "; " + snapshot.FormatSummary());
            Diagnostics.SetFeatureStatus(
                "Refactor.FailedModRollback",
                snapshot.RolledBackTransactions == 0 ? "idle" : (snapshot.CleanupFailures == 0 ? "rolled-back" : "warning"),
                operation ?? string.Empty,
                success: snapshot.CleanupFailures == 0,
                failureCount: snapshot.CleanupFailures,
                lastError: snapshot.CleanupFailures == 0 ? string.Empty : "Failed mod rollback had cleanup failures.",
                details: snapshot.FormatRollbackSummary());

            SetHookStatus("Refactor.ModOwnerLifecycle", success ? "ok" : "warning", "ModOwnerLedger." + (operation ?? string.Empty), summary);
            SetHookStatus("Refactor.ModLoadTransaction", refactorOptions.ModLoadTransaction ? (snapshot.ActiveTransactions == 0 ? "closed" : "active") : "disabled", "ModOwnerLedger." + (operation ?? string.Empty), snapshot.FormatSummary());
            SetHookStatus("Refactor.OwnerBoundInput", refactorOptions.OwnerBoundInput ? "ok" : "disabled", "ModOwnerLedger." + (operation ?? string.Empty), inputSnapshot.FormatSummary());
            SetHookStatus("Refactor.EventHandlerCleanup", refactorOptions.EventHandlerQuarantine ? "ok" : "disabled", "ModOwnerLedger." + (operation ?? string.Empty), eventSnapshot.FormatSummary());
            SetHookStatus("Refactor.ConfigPreviewAudit", refactorOptions.ConfigPreviewAudit ? (configPreviewWarnings == 0 ? "observing" : "warning") : "disabled", "ModOwnerLedger." + (operation ?? string.Empty), "configPreviewWarnings=" + configPreviewWarnings.ToString(CultureInfo.InvariantCulture));
            SetHookStatus("Refactor.FailedModRollback", snapshot.RolledBackTransactions == 0 ? "idle" : (snapshot.CleanupFailures == 0 ? "rolled-back" : "warning"), "ModOwnerLedger." + (operation ?? string.Empty), snapshot.FormatRollbackSummary());
        }

        internal void ObserveLifecycleResourceEvent(string category, string eventName, string ownerId, string resourceId, string details)
        {
            if (!refactorOptions.LifecycleObservation)
                return;

            try
            {
                PublishLifecycleBoundaryContractUpdate(
                    "Resource:" + (category ?? string.Empty) + ":" + (eventName ?? string.Empty),
                    lifecycleBoundaryContract.RecordResourceEvent(
                        category ?? string.Empty,
                        eventName ?? string.Empty,
                        ownerId ?? string.Empty,
                        resourceId ?? string.Empty,
                        details ?? string.Empty));
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError("DTMAPI.RefactorScaffold", "Lifecycle boundary resource observation failed.", ex.ToString());
                Diagnostics.SetFeatureStatus(
                    "Refactor.LifecycleBoundaryContract",
                    "error",
                    "Resource:" + (category ?? string.Empty) + ":" + (eventName ?? string.Empty),
                    success: false,
                    failureCount: 1,
                    lastError: ex.GetType().Name + ": " + ex.Message,
                    details: "Lifecycle boundary resource observation failed without changing runtime behavior.");
            }
        }

        internal void ObserveResourceLifecycle(
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
            if (!refactorOptions.ResourceLifecycleLedger)
                return;

            try
            {
                PublishResourceLifecycleLedgerUpdate(
                    "Resource:" + (resourceKind ?? string.Empty) + ":" + (resourceId ?? string.Empty),
                    resourceLifecycleLedger.RecordResource(
                        resourceKind ?? string.Empty,
                        resourceId ?? string.Empty,
                        ownerId ?? string.Empty,
                        sourcePath ?? string.Empty,
                        lifetime ?? string.Empty,
                        ownership ?? string.Empty,
                        status ?? string.Empty,
                        releasePolicy ?? string.Empty,
                        generation,
                        observationMode,
                        aggregationKey ?? string.Empty));
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError("DTMAPI.RefactorScaffold", "Resource lifecycle ledger observation failed.", ex.ToString());
                Diagnostics.SetFeatureStatus(
                    "Refactor.ResourceLifecycleLedger",
                    "error",
                    resourceKind ?? string.Empty,
                    success: false,
                    failureCount: 1,
                    lastError: ex.GetType().Name + ": " + ex.Message,
                    details: "Resource lifecycle ledger observation failed without changing runtime behavior.");
            }
        }

        internal void ReleaseResourceLifecycle(
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
            if (!refactorOptions.ResourceLifecycleLedger)
                return;

            try
            {
                PublishResourceLifecycleLedgerUpdate(
                    "Release:" + (resourceKind ?? string.Empty) + ":" + (resourceId ?? string.Empty),
                    resourceLifecycleLedger.ReleaseResource(
                        resourceKind ?? string.Empty,
                        resourceId ?? string.Empty,
                        ownerId ?? string.Empty,
                        sourcePath ?? string.Empty,
                        lifetime ?? string.Empty,
                        ownership ?? string.Empty,
                        releasePolicy ?? string.Empty,
                        status ?? string.Empty,
                        generation,
                        observationMode,
                        aggregationKey ?? string.Empty));
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError("DTMAPI.RefactorScaffold", "Resource lifecycle ledger release observation failed.", ex.ToString());
                Diagnostics.SetFeatureStatus(
                    "Refactor.ResourceLifecycleLedger",
                    "error",
                    resourceKind ?? string.Empty,
                    success: false,
                    failureCount: 1,
                    lastError: ex.GetType().Name + ": " + ex.Message,
                    details: "Resource lifecycle ledger release observation failed without changing runtime behavior.");
            }
        }

        internal void ObserveResourceRefresh(string area, string reason, string result, int resourceCount)
        {
            if (!refactorOptions.ResourceLifecycleLedger)
                return;

            try
            {
                PublishResourceLifecycleLedgerUpdate(
                    "Refresh:" + (area ?? string.Empty) + ":" + (result ?? string.Empty),
                    resourceLifecycleLedger.RecordRefresh(area ?? string.Empty, reason ?? string.Empty, result ?? string.Empty, resourceCount));
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError("DTMAPI.RefactorScaffold", "Resource lifecycle refresh observation failed.", ex.ToString());
                Diagnostics.SetFeatureStatus(
                    "Refactor.ResourceLifecycleLedger",
                    "error",
                    area ?? string.Empty,
                    success: false,
                    failureCount: 1,
                    lastError: ex.GetType().Name + ": " + ex.Message,
                    details: "Resource lifecycle refresh observation failed without changing runtime behavior.");
            }
        }

        internal void ObserveResourceCleanup(string area, string reason, int clearedCount, string details)
        {
            if (!refactorOptions.ResourceLifecycleLedger)
                return;

            try
            {
                PublishResourceLifecycleLedgerUpdate(
                    "Cleanup:" + (area ?? string.Empty),
                    resourceLifecycleLedger.RecordCleanup(area ?? string.Empty, reason ?? string.Empty, clearedCount, details ?? string.Empty));
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError("DTMAPI.RefactorScaffold", "Resource lifecycle cleanup observation failed.", ex.ToString());
                Diagnostics.SetFeatureStatus(
                    "Refactor.ResourceLifecycleLedger",
                    "error",
                    area ?? string.Empty,
                    success: false,
                    failureCount: 1,
                    lastError: ex.GetType().Name + ": " + ex.Message,
                    details: "Resource lifecycle cleanup observation failed without changing runtime behavior.");
            }
        }

        public void RegisterRuntimeApi<TApi>(IManifest owner, TApi api) where TApi : class
        {
            RegisterRuntimeApi(owner, api, ownerBoundFactory: null);
        }

        internal void RegisterRuntimeApi<TApi>(IManifest owner, TApi api, IOwnerBoundApiFactory? ownerBoundFactory) where TApi : class
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (api == null)
                throw new ArgumentNullException(nameof(api));
            if (string.IsNullOrWhiteSpace(owner.UniqueID))
                throw new ArgumentException("Manifest UniqueID is required.", nameof(owner));
            if (!processLifetimeProviderIds.Contains(owner.UniqueID) && ModRegistry.IsLoaded(owner.UniqueID))
            {
                throw new InvalidOperationException(
                    "Process-lifetime owner '" + owner.UniqueID + "' conflicts with an already loaded source-managed owner. " +
                    "A process provider can't share or replace an ordinary owner in the same process.");
            }
            ModRegistry.RegisterProcessLifetimeApiForOwner(owner, api, ownerBoundFactory);
            processLifetimeProviderIds.Add(owner.UniqueID);
            RuntimeMonitor.Log("Registered runtime API " + typeof(TApi).FullName + " from " + owner.UniqueID + ".");
        }

        public string ExportLogs()
        {
            currentRuntimePhase = "LogExport";
            ObserveLifecycle("LogExport", currentLoadingSlot, null);
            InvokeRuntimeBoundary(logExportBoundary, "Log export");
            FlushRuntimeQueues("LogExport.BeforeExport");
            string report = Diagnostics.ExportLogs(BuildRuntimeReportContext());
            Events.DispatchLogExported(report);
            RuntimeMonitor.Log("Exported DTMAPI logs to " + report);
            FlushRuntimeQueues("LogExport.AfterExport");
            return report;
        }

        public void NotifyRuntimeShutdown(string reason)
        {
            currentRuntimePhase = "Shutdown";
            CloseAuthorSession("Shutdown:" + (reason ?? string.Empty));
            RuntimeMonitor.Log("Runtime shutdown observed by DTMAPI. reason=" + (string.IsNullOrWhiteSpace(reason) ? "unknown" : reason) + ".");
            ObserveLifecycle("Shutdown", currentLoadingSlot, null);
            var shutdownOwners = loadedMods.Select(mod => mod.Manifest.UniqueID)
                .Concat(modInstances.Keys)
                .Concat(modOwnerLifecycle.GetOwners().Reverse())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Reverse()
                .ToArray();
            foreach (string ownerId in shutdownOwners)
            {
                try
                {
                    DeactivateOwner(ownerId, ModOwnerCleanupReason.RuntimeShutdown, shutdown: true, transactionId: string.Empty);
                }
                catch (Exception ex)
                {
                    TryLogOwnerCleanupDiagnostic("Runtime shutdown owner cleanup failed owner=" + ownerId + ": " + ex.GetType().Name + ": " + ex.Message);
                }
            }
            InvokeRuntimeBoundary(runtimeShutdownBoundary, "Runtime shutdown", reason ?? string.Empty);
            FlushRuntimeQueues("Shutdown");
        }

        private void LoadRefactorScaffoldOptions()
        {
            refactorOptions = RuntimeSubsystemOptions.Load(Paths, out string summary, out string warning);
            var enforced = new List<string>();
            if (!refactorOptions.ModLoadTransaction) { refactorOptions.ModLoadTransactionValue = true; enforced.Add("ModLoadTransaction"); }
            if (!refactorOptions.OwnerBoundInput) { refactorOptions.OwnerBoundInputValue = true; enforced.Add("OwnerBoundInput"); }
            if (!refactorOptions.EventHandlerQuarantine) { refactorOptions.EventHandlerQuarantineValue = true; enforced.Add("EventHandlerQuarantine"); }
            if (enforced.Count > 0)
            {
                string enforcedMessage = "Legacy lifecycle correctness flags set to false were ignored; enforced=" + string.Join(",", enforced) + ".";
                RuntimeMonitor.Log(enforcedMessage, LogLevel.Warn);
                Diagnostics.RecordWarning("DTMAPI.Runtime", "Owner lifetime correctness is enforced.", enforcedMessage);
                summary += "; enforced=" + string.Join(",", enforced);
            }
            RuntimeMonitor.Log("Refactor scaffold flags: " + summary + ".");
            Diagnostics.SetFeatureStatus(
                RuntimeSubsystemOptions.FeatureStatusId,
                "configured",
                "LoadRefactorScaffoldOptions",
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: summary);
            Diagnostics.SetFeatureStatus(
                "Refactor.ShadowResourceLoader",
                refactorOptions.ShadowResourceLoader ? "configured" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: refactorOptions.ShadowResourceLoader ? "Shadow resource loader flag is enabled, but first-stage implementation does not take over resource loading." : "Shadow resource loader is disabled by first-stage guardrail.");
            Diagnostics.SetFeatureStatus(
                "Refactor.RegistryTakesOver",
                refactorOptions.RegistryTakesOver ? "blocked" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: !refactorOptions.RegistryTakesOver,
                failureCount: refactorOptions.RegistryTakesOver ? 1 : 0,
                lastError: refactorOptions.RegistryTakesOver ? "First-stage guardrail forbids registry takeover." : string.Empty,
                details: refactorOptions.RegistryTakesOver ? "RegistryTakesOver=true was requested, but first-stage scaffold remains shadow-only." : "Registry takeover is disabled by first-stage guardrail.");
            Diagnostics.SetFeatureStatus(
                "Refactor.ResourceLifecycleLedger",
                refactorOptions.ResourceLifecycleLedger ? "observing" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: refactorOptions.ResourceLifecycleLedger ? "Resource lifecycle ledger records internal ownership/generation diagnostics only." : "Resource lifecycle ledger is disabled by refactor-scaffold feature flag.");
            Diagnostics.SetFeatureStatus(
                "Refactor.ResourceLifecycleCleanup",
                refactorOptions.ResourceLifecycleCleanup ? "save-lifetime-only" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: refactorOptions.ResourceLifecycleCleanup ? "Cleanup is limited to DTMAPI-owned SaveLifetime state and existing content-generation replacement cleanup." : "Resource lifecycle cleanup is disabled by refactor-scaffold feature flag.");
            Diagnostics.SetFeatureStatus(
                "Refactor.ResourceLifecycleTitleAssetRelease",
                refactorOptions.ResourceLifecycleTitleAssetRelease ? "blocked" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: !refactorOptions.ResourceLifecycleTitleAssetRelease,
                failureCount: refactorOptions.ResourceLifecycleTitleAssetRelease ? 1 : 0,
                lastError: refactorOptions.ResourceLifecycleTitleAssetRelease ? "Third-stage guardrail forbids title-level Unity/native asset release." : string.Empty,
                details: refactorOptions.ResourceLifecycleTitleAssetRelease ? "ResourceLifecycleTitleAssetRelease=true was requested, but this stage keeps title assets report-only." : "Title-level Unity/native asset release is disabled by third-stage guardrail.");
            Diagnostics.SetFeatureStatus(
                "Refactor.ContentManifestRegistry",
                refactorOptions.ContentManifestRegistry ? "authoritative-index" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: refactorOptions.ContentManifestRegistry
                    ? "Content/manifest registry is enabled as an internal authoritative diagnostic index only; RegistryTakesOver remains guarded off."
                    : "Content/manifest registry is disabled by refactor-scaffold feature flag.");
            Diagnostics.SetFeatureStatus(
                "Refactor.HookInstallScheduler",
                refactorOptions.HookInstallScheduler ? "scheduled-main-thread" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: refactorOptions.HookInstallScheduler ? "Hook install requests are scheduled and executed on the runtime thread." : "Hook install scheduler is disabled by refactor-scaffold feature flag; legacy direct install path is used.");
            PublishHookStatusQueueFeatureStatus("LoadRefactorScaffoldOptions", force: true);
            PublishEventBoundaryFeatureStatus("LoadRefactorScaffoldOptions", force: true);
            Diagnostics.SetFeatureStatus(
                "Refactor.HookReadinessLayers",
                refactorOptions.HookReadinessLayers ? "observing" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: refactorOptions.HookReadinessLayers ? "Core, feature, and smoke/diagnostics hook readiness are reported separately." : "Layered hook readiness is disabled by refactor-scaffold feature flag.");
            Diagnostics.SetFeatureStatus(
                "Refactor.SaveLoadRequestCoordinator",
                refactorOptions.SaveLoadRequestCoordinator ? "observing" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: refactorOptions.SaveLoadRequestCoordinator ? "SaveLoad request coordinator records internal LoadGame/SaveLoaded boundaries and suppresses DTMAPI/smoke duplicates only." : "SaveLoad request coordinator is disabled by refactor-scaffold feature flag; legacy LoadGame slot tracking is used.");
            Diagnostics.SetFeatureStatus(
                "Refactor.ModOwnerLedger",
                refactorOptions.ModOwnerLedger ? "observing" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: refactorOptions.ModOwnerLedger ? "Mod owner ledger records internal registration, cleanup, transaction, and rollback attribution." : "Mod owner ledger is disabled by refactor-scaffold feature flag.");
            Diagnostics.SetFeatureStatus(
                "Refactor.ModLoadTransaction",
                refactorOptions.ModLoadTransaction ? "observing" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: refactorOptions.ModLoadTransaction ? "Code mod Entry registrations are grouped under an internal load transaction." : "Mod load transaction is disabled by refactor-scaffold feature flag.");
            Diagnostics.SetFeatureStatus(
                "Refactor.OwnerBoundInput",
                refactorOptions.OwnerBoundInput ? "owner-bound" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: refactorOptions.OwnerBoundInput ? "DtmHelper.Input uses owner-bound registration proxies without changing IInputHelper." : "Owner-bound input is disabled by refactor-scaffold feature flag.");
            Diagnostics.SetFeatureStatus(
                "Refactor.EventHandlerCleanup",
                refactorOptions.EventHandlerQuarantine ? "quarantine" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: refactorOptions.EventHandlerQuarantine ? "High-frequency failed event handlers are quarantined outside the active hot path." : "Event handler quarantine is disabled by refactor-scaffold feature flag.");
            Diagnostics.SetFeatureStatus(
                "Refactor.ConfigPreviewAudit",
                refactorOptions.ConfigPreviewAudit ? "observing" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: refactorOptions.ConfigPreviewAudit ? "Config preview side effects are audited without changing preview behavior." : "Config preview audit is disabled by refactor-scaffold feature flag.");
            Diagnostics.SetFeatureStatus(
                "Refactor.GameBridgeFeatureContracts",
                refactorOptions.GameBridgeFeatureContracts ? "observing" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: refactorOptions.GameBridgeFeatureContracts ? "GameBridge feature contracts are diagnosed without changing lifecycle fanout." : "GameBridge feature contract diagnostics are disabled by refactor-scaffold feature flag.");
            Diagnostics.SetFeatureStatus(
                "Refactor.GameBridgeFinalHealthSnapshot",
                refactorOptions.GameBridgeFinalHealthSnapshot ? "observing" : "disabled",
                "LoadRefactorScaffoldOptions",
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: refactorOptions.GameBridgeFinalHealthSnapshot ? "GameBridge final health snapshots are written at save/title/export/shutdown boundaries." : "GameBridge final health snapshots are disabled by refactor-scaffold feature flag.");
            if (refactorOptions.ModOwnerLedger)
                PublishModOwnerLifecycleStatus("LoadRefactorScaffoldOptions");
            if (!string.IsNullOrWhiteSpace(warning))
                RecordRefactorScaffoldWarningOnce("options-load", "Refactor scaffold options warning.", warning);
        }

        private void ObserveLifecycle(string phase, int? saveSlot, bool? isNewGame)
        {
            if (!refactorOptions.LifecycleObservation)
            {
                Diagnostics.SetFeatureStatus(
                    "Refactor.LifecycleObservation",
                    "disabled",
                    phase ?? string.Empty,
                    success: true,
                    failureCount: 0,
                    lastError: string.Empty,
                    details: "Lifecycle observation is disabled by refactor-scaffold feature flag.");
                Diagnostics.SetFeatureStatus(
                    "Refactor.LifecycleBoundaryContract",
                    "disabled",
                    phase ?? string.Empty,
                    success: true,
                    failureCount: 0,
                    lastError: string.Empty,
                    details: "Lifecycle boundary contract diagnostics are disabled with lifecycle observation.");
                return;
            }

            try
            {
                LifecycleObservationEntry entry = lifecycleObservation.Record(
                    phase,
                    Thread.CurrentThread.ManagedThreadId,
                    saveSlot,
                    isNewGame,
                    discoveredMods.Count,
                    loadedMods.Count,
                    Diagnostics.GetHookStatuses().Count);
                string detail = lifecycleObservation.FormatSummary();
                RuntimeMonitor.Log(
                    "Refactor lifecycle observation phase=" + entry.Phase +
                    " count=" + entry.Count +
                    " thread=" + entry.ThreadId +
                    " slot=" + (entry.SaveSlot.HasValue ? entry.SaveSlot.Value.ToString() : "unknown") +
                    " isNewGame=" + (entry.IsNewGame.HasValue ? (entry.IsNewGame.Value ? "true" : "false") : "unknown") +
                    " discovered=" + entry.DiscoveredModCount +
                    " loaded=" + entry.LoadedModCount +
                    " hooks=" + entry.HookStatusCount + ".");
                Diagnostics.SetFeatureStatus(
                    "Refactor.LifecycleObservation",
                    "observing",
                    entry.Phase,
                    success: true,
                    failureCount: 0,
                    lastError: string.Empty,
                    details: detail);
                PublishLifecycleBoundaryContractUpdate(
                    "Phase:" + entry.Phase,
                    lifecycleBoundaryContract.RecordPhase(
                        entry.Phase,
                        saveSlot,
                        isNewGame,
                        discoveredMods.Count,
                        loadedMods.Count,
                        Diagnostics.GetHookStatuses().Count));
                ObserveResourceLifecyclePhase(entry.Phase, saveSlot, isNewGame);
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError("DTMAPI.RefactorScaffold", "Lifecycle observation failed.", ex.ToString());
                Diagnostics.SetFeatureStatus(
                    "Refactor.LifecycleObservation",
                    "error",
                    phase ?? string.Empty,
                    success: false,
                    failureCount: 1,
                    lastError: ex.GetType().Name + ": " + ex.Message,
                    details: "Lifecycle observation failed without changing runtime behavior.");
            }
        }

        private void ObserveResourceLifecyclePhase(string phase, int? saveSlot, bool? isNewGame)
        {
            if (!refactorOptions.ResourceLifecycleLedger)
                return;

            try
            {
                PublishResourceLifecycleLedgerUpdate(
                    "Phase:" + (phase ?? string.Empty),
                    resourceLifecycleLedger.RecordPhase(phase ?? string.Empty));

                string normalizedPhase = RuntimeLifecyclePhase.Normalize(phase ?? string.Empty);
                if (normalizedPhase.Equals(RuntimeLifecyclePhase.SaveLoaded, StringComparison.OrdinalIgnoreCase))
                {
                    PublishResourceLifecycleLedgerUpdate(
                        "SaveGeneration:" + normalizedPhase,
                        resourceLifecycleLedger.BeginSaveGeneration(normalizedPhase, saveSlot, isNewGame));
                }
                else if (normalizedPhase.Equals(RuntimeLifecyclePhase.ReturnedToTitle, StringComparison.OrdinalIgnoreCase))
                {
                    PublishResourceLifecycleLedgerUpdate(
                        "SaveGeneration:ReturnedToTitle",
                        resourceLifecycleLedger.CloseSaveGeneration("ReturnedToTitle"));
                }
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError("DTMAPI.RefactorScaffold", "Resource lifecycle phase observation failed.", ex.ToString());
                Diagnostics.SetFeatureStatus(
                    "Refactor.ResourceLifecycleLedger",
                    "error",
                    phase ?? string.Empty,
                    success: false,
                    failureCount: 1,
                    lastError: ex.GetType().Name + ": " + ex.Message,
                    details: "Resource lifecycle phase observation failed without changing runtime behavior.");
            }
        }

        private void RefreshShadowContentRegistry(string reason)
        {
            if (!refactorOptions.ShadowContentRegistry)
            {
                Diagnostics.SetFeatureStatus(
                    "Refactor.ShadowContentRegistry",
                    "disabled",
                    reason ?? string.Empty,
                    success: true,
                    failureCount: 0,
                    lastError: string.Empty,
                    details: "Shadow content registry is disabled by refactor-scaffold feature flag.");
                return;
            }

            try
            {
                latestShadowContentRegistrySnapshot = shadowContentRegistry.Build(discoveredModsSnapshot, loadedModsSnapshot, reason);
                string summary = latestShadowContentRegistrySnapshot.FormatSummary();
                RuntimeMonitor.Log("Refactor shadow content registry: " + summary + ".");
                ObserveLifecycleRegistryRefresh(reason, latestShadowContentRegistrySnapshot);
                Diagnostics.SetFeatureStatus(
                    "Refactor.ShadowContentRegistry",
                    latestShadowContentRegistrySnapshot.DiffCount == 0 ? "shadow-ok" : "shadow-diff",
                    reason ?? string.Empty,
                    success: latestShadowContentRegistrySnapshot.DiffCount == 0,
                    failureCount: latestShadowContentRegistrySnapshot.DiffCount,
                    lastError: latestShadowContentRegistrySnapshot.DiffCount == 0 ? string.Empty : "Shadow registry diagnostics differ from expected content shape.",
                    details: summary);
                foreach (string diagnostic in latestShadowContentRegistrySnapshot.Diagnostics.Take(8))
                    RecordRefactorScaffoldWarningOnce("shadow-content-" + diagnostic, "Shadow content registry diagnostic.", diagnostic);
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError("DTMAPI.RefactorScaffold", "Shadow content registry failed.", ex.ToString());
                Diagnostics.SetFeatureStatus(
                    "Refactor.ShadowContentRegistry",
                    "error",
                    reason ?? string.Empty,
                    success: false,
                    failureCount: 1,
                    lastError: ex.GetType().Name + ": " + ex.Message,
                    details: "Shadow content registry failed without changing runtime behavior.");
            }
        }

        private void RefreshContentManifestRegistry(string reason)
        {
            if (!refactorOptions.ContentManifestRegistry)
                return;

            try
            {
                latestContentManifestRegistrySnapshot = contentManifestRegistry.Build(
                    discoveredModsSnapshot,
                    loadedModsSnapshot,
                    latestManifestScannerErrors,
                    latestManifestScannerWarnings,
                    latestManifestScannerDiagnosticTotals,
                    reason,
                    ModRegistry.GetAll());
                string summary = latestContentManifestRegistrySnapshot.FormatSummary();
                RuntimeMonitor.Log("Refactor content manifest registry: " + summary + ".");

                bool diffsOk = latestContentManifestRegistrySnapshot.DiffCount == 0;
                bool manifestOk = latestContentManifestRegistrySnapshot.ManifestDiagnosticCount == 0;
                bool dependencyOk = latestContentManifestRegistrySnapshot.DependencyErrorCount == 0 &&
                    latestContentManifestRegistrySnapshot.ApiTooNewCount == 0;
                bool ownershipOk = latestContentManifestRegistrySnapshot.Rows.All(r => !string.IsNullOrWhiteSpace(r.OwnerId));

                SetHookStatus(
                    "Refactor.ContentRegistry",
                    diffsOk ? "indexed" : "warning",
                    "ContentManifestRegistry." + (reason ?? string.Empty),
                    summary + "; RegistryTakesOver=" + (refactorOptions.RegistryTakesOver ? "true" : "false"));
                SetHookStatus(
                    "Refactor.ManifestRegistry",
                    manifestOk ? "ok" : "warning",
                    "ContentManifestRegistry." + (reason ?? string.Empty),
                    latestContentManifestRegistrySnapshot.FormatManifestSummary());
                SetHookStatus(
                    "Refactor.DependencyCompatibility",
                    dependencyOk ? "ok" : "warning",
                    "ContentManifestRegistry." + (reason ?? string.Empty),
                    latestContentManifestRegistrySnapshot.FormatDependencySummary());
                SetHookStatus(
                    "Refactor.ContentPackOwnership",
                    ownershipOk ? "ok" : "warning",
                    "ContentManifestRegistry." + (reason ?? string.Empty),
                    latestContentManifestRegistrySnapshot.FormatOwnershipSummary());
                SetHookStatus(
                    "Refactor.RegistryDiffs",
                    diffsOk ? "ok" : "warning",
                    "ContentManifestRegistry." + (reason ?? string.Empty),
                    latestContentManifestRegistrySnapshot.FormatDiffSummary());

                Diagnostics.SetFeatureStatus(
                    "Refactor.ContentManifestRegistry",
                    diffsOk ? "authoritative-index" : "diff",
                    reason ?? string.Empty,
                    success: diffsOk,
                    failureCount: latestContentManifestRegistrySnapshot.DiffCount,
                    lastError: diffsOk ? string.Empty : "Content/manifest registry diff detected.",
                    details: summary);
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError("DTMAPI.RefactorScaffold", "Content/manifest registry failed.", ex.ToString());
                Diagnostics.SetFeatureStatus(
                    "Refactor.ContentManifestRegistry",
                    "error",
                    reason ?? string.Empty,
                    success: false,
                    failureCount: 1,
                    lastError: ex.GetType().Name + ": " + ex.Message,
                    details: "Content/manifest registry failed without changing runtime behavior.");
                SetHookStatus(
                    "Refactor.ContentRegistry",
                    "error",
                    "ContentManifestRegistry." + (reason ?? string.Empty),
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void ObserveResourceContentSignature(string reason)
        {
            if (!refactorOptions.ResourceLifecycleLedger)
                return;

            try
            {
                PublishResourceLifecycleLedgerUpdate(
                    "ContentGeneration:" + (reason ?? string.Empty),
                    resourceLifecycleLedger.ObserveContentSignature(reason ?? string.Empty, BuildResourceContentSignature()));
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError("DTMAPI.RefactorScaffold", "Resource lifecycle content signature observation failed.", ex.ToString());
                Diagnostics.SetFeatureStatus(
                    "Refactor.ResourceLifecycleLedger",
                    "error",
                    reason ?? string.Empty,
                    success: false,
                    failureCount: 1,
                    lastError: ex.GetType().Name + ": " + ex.Message,
                    details: "Resource lifecycle content signature observation failed without changing runtime behavior.");
            }
        }

        private string BuildResourceContentSignature()
        {
            return string.Join("|", loadedMods
                .OrderBy(mod => mod.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase)
                .Select(mod =>
                {
                    string customAnimalsPath = Path.Combine(mod.RootPath, "Content", "DTMAPI", "custom-animals.json");
                    string audioReplacementsPath = Path.Combine(mod.RootPath, "Content", "DTMAPI", "audio-replacements.json");
                    string manifestPath = Path.Combine(mod.RootPath, "manifest.json");
                    return SingleLine(mod.Manifest.UniqueID) +
                        "@" + SingleLine(mod.RootPath) +
                        "#identity=" + SingleLine(mod.Classification.IdentityName) +
                        "#enabled=" + (mod.OfficialEnabled ? "true" : "false") +
                        "#manifest=" + GetFileStamp(manifestPath) +
                        "#animals=" + GetFileStamp(customAnimalsPath) +
                        "#audio=" + GetFileStamp(audioReplacementsPath);
                })
                .ToArray());
        }

        private static string GetFileStamp(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                    return "0";
                var info = new FileInfo(path);
                return info.LastWriteTimeUtc.Ticks.ToString() + ":" + info.Length.ToString();
            }
            catch
            {
                return "unknown";
            }
        }

        private void RecordRefactorScaffoldWarningOnce(string key, string message, string details)
        {
            string normalizedKey = string.IsNullOrWhiteSpace(key) ? message + details : key;
            if (refactorScaffoldWarnings.Count >= MaxRefactorScaffoldWarningKeys)
            {
                if (!refactorScaffoldWarningKeysCapped)
                {
                    refactorScaffoldWarningKeysCapped = true;
                    Diagnostics.RecordWarning("DTMAPI.RefactorScaffold", "Refactor warning key cap reached.", "Additional unique warning keys are suppressed; cap=" + MaxRefactorScaffoldWarningKeys.ToString(CultureInfo.InvariantCulture) + ".");
                    RuntimeMonitor.Log("Refactor warning key cap reached; additional unique keys are suppressed. cap=" + MaxRefactorScaffoldWarningKeys.ToString(CultureInfo.InvariantCulture) + ".", LogLevel.Warn);
                }
                return;
            }
            if (!refactorScaffoldWarnings.Add(normalizedKey))
                return;
            Diagnostics.RecordWarning("DTMAPI.RefactorScaffold", message, details);
            RuntimeMonitor.Log(message + " " + details, LogLevel.Warn);
        }

        private void ObserveLifecycleRegistryRefresh(string reason, ShadowContentRegistrySnapshot snapshot)
        {
            if (!refactorOptions.LifecycleObservation)
                return;

            try
            {
                PublishLifecycleBoundaryContractUpdate(
                    "RegistryRefresh:" + (reason ?? string.Empty),
                    lifecycleBoundaryContract.RecordRegistryRefresh(
                        reason ?? string.Empty,
                        snapshot.Rows.Count,
                        snapshot.Rows.Count(row => row.LoadedByOldSystem),
                        snapshot.Rows.Sum(row => row.CustomAnimals.Count),
                        snapshot.Rows.Sum(row => row.AudioReplacements.Count)));
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError("DTMAPI.RefactorScaffold", "Lifecycle boundary registry observation failed.", ex.ToString());
                Diagnostics.SetFeatureStatus(
                    "Refactor.LifecycleBoundaryContract",
                    "error",
                    "RegistryRefresh:" + (reason ?? string.Empty),
                    success: false,
                    failureCount: 1,
                    lastError: ex.GetType().Name + ": " + ex.Message,
                    details: "Lifecycle boundary registry observation failed without changing runtime behavior.");
            }
        }

        private void ObserveLifecycleHookStatus(string hookId, string status, string source, string details)
        {
            if (!refactorOptions.LifecycleObservation)
                return;

            try
            {
                PublishLifecycleBoundaryContractUpdate(
                    "HookStatus:" + (hookId ?? string.Empty),
                    lifecycleBoundaryContract.RecordHookStatus(hookId ?? string.Empty, status ?? string.Empty, source ?? string.Empty, details ?? string.Empty));
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError("DTMAPI.RefactorScaffold", "Lifecycle boundary hook observation failed.", ex.ToString());
                Diagnostics.SetFeatureStatus(
                    "Refactor.LifecycleBoundaryContract",
                    "error",
                    "HookStatus:" + (hookId ?? string.Empty),
                    success: false,
                    failureCount: 1,
                    lastError: ex.GetType().Name + ": " + ex.Message,
                    details: "Lifecycle boundary hook observation failed without changing runtime behavior.");
            }
        }

        private void ObserveReturnedToTitleRuntimeState(string operation)
        {
            if (!refactorOptions.LifecycleObservation)
                return;

            try
            {
                PublishLifecycleBoundaryContractUpdate(
                    operation,
                    lifecycleBoundaryContract.RecordReturnedToTitleState(BuildLifecycleBoundaryRuntimeState()));
            }
            catch (Exception ex)
            {
                Diagnostics.RecordError("DTMAPI.RefactorScaffold", "Lifecycle boundary returned-to-title state observation failed.", ex.ToString());
                Diagnostics.SetFeatureStatus(
                    "Refactor.LifecycleBoundaryContract",
                    "error",
                    operation ?? string.Empty,
                    success: false,
                    failureCount: 1,
                    lastError: ex.GetType().Name + ": " + ex.Message,
                    details: "Lifecycle boundary returned-to-title state observation failed without changing runtime behavior.");
            }
        }

        private LifecycleBoundaryRuntimeState BuildLifecycleBoundaryRuntimeState()
        {
            int activeCustomEntityRuntimeInstances =
                CustomEntities.GetAnimalSnapshot().ActiveRuntimeInstanceCount +
                CustomEntities.GetMonsterSnapshot().ActiveRuntimeInstanceCount +
                CustomEntities.GetAttackSnapshot().ActiveRuntimeInstanceCount +
                CustomEntities.GetDroneSnapshot().ActiveRuntimeInstanceCount;
            return new LifecycleBoundaryRuntimeState(
                currentLoadingSlot,
                activeCustomEntityRuntimeInstances,
                discoveredMods.Count,
                loadedMods.Count,
                Diagnostics.GetHookStatuses().Count);
        }

        private void PublishLifecycleBoundaryContractUpdate(string operation, LifecycleBoundaryContractUpdate update)
        {
            LifecycleBoundaryContractSnapshot snapshot = update.Snapshot;
            string summary = snapshot.FormatSummary();
            bool statusChanged = !snapshot.Status.Equals(lastLoggedLifecycleBoundaryContractStatus, StringComparison.OrdinalIgnoreCase);
            bool lifecycleBoundary = (operation ?? string.Empty).StartsWith("Phase:", StringComparison.OrdinalIgnoreCase) ||
                (operation ?? string.Empty).IndexOf("ReturnedToTitle", StringComparison.OrdinalIgnoreCase) >= 0;
            if (statusChanged || lifecycleBoundary || update.NewDiagnostics.Count > 0)
            {
                RuntimeMonitor.Log("Refactor lifecycle boundary contract status=" + snapshot.Status + " operation=" + operation + " " + summary + ".");
                lastLoggedLifecycleBoundaryContractStatus = snapshot.Status;
            }
            string lastError = snapshot.Diagnostics.FirstOrDefault(d => d.Severity.Equals("error", StringComparison.OrdinalIgnoreCase))?.Format() ?? string.Empty;
            Diagnostics.SetFeatureStatus(
                "Refactor.LifecycleBoundaryContract",
                snapshot.Status,
                operation ?? string.Empty,
                success: snapshot.Success,
                failureCount: snapshot.ErrorCount,
                lastError: lastError,
                details: summary);

            foreach (LifecycleBoundaryDiagnostic diagnostic in update.NewDiagnostics.Take(8))
                RecordRefactorScaffoldWarningOnce("lifecycle-boundary-" + diagnostic.Format(), "Lifecycle boundary contract diagnostic.", diagnostic.Format());
        }

        private void PublishResourceLifecycleLedgerUpdate(string operation, ResourceLifecycleLedgerUpdate update)
        {
            if (update == null || !update.ShouldPublish)
                return;

            ResourceLifecycleSnapshot snapshot = update.Snapshot;
            string summary = snapshot.FormatSummary();
            bool skippedRefresh = (operation ?? string.Empty).IndexOf(":skipped-", StringComparison.OrdinalIgnoreCase) >= 0;
            bool shouldLog =
                update.NewDiagnostics.Count > 0 ||
                (operation ?? string.Empty).IndexOf("Generation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (operation ?? string.Empty).IndexOf("Cleanup", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (operation ?? string.Empty).IndexOf("Release", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ((operation ?? string.Empty).IndexOf("Refresh", StringComparison.OrdinalIgnoreCase) >= 0 && !skippedRefresh);
            if (shouldLog)
                RuntimeMonitor.Log("Refactor resource lifecycle ledger status=" + snapshot.Status + " operation=" + operation + " " + summary + ".");

            string lastError = snapshot.Diagnostics.FirstOrDefault(d => d.Severity.Equals("error", StringComparison.OrdinalIgnoreCase))?.Format() ?? string.Empty;
            Diagnostics.SetFeatureStatus(
                "Refactor.ResourceLifecycleLedger",
                snapshot.Status,
                operation ?? string.Empty,
                success: snapshot.Success,
                failureCount: snapshot.ErrorCount,
                lastError: lastError,
                details: summary);
            Diagnostics.SetFeatureStatus(
                "Refactor.ResourceLifecycleCleanup",
                refactorOptions.ResourceLifecycleCleanup ? "save-lifetime-only" : "disabled",
                operation ?? string.Empty,
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: refactorOptions.ResourceLifecycleCleanup
                    ? "Cleanup enabled for DTMAPI-owned SaveLifetime state only. " + snapshot.FormatReturnedToTitleCleanupPlan()
                    : "Resource lifecycle cleanup is disabled by refactor-scaffold feature flag.");

            foreach (ResourceLifecycleDiagnostic diagnostic in update.NewDiagnostics.Take(8))
                RecordRefactorScaffoldWarningOnce("resource-lifecycle-" + diagnostic.Format(), "Resource lifecycle ledger diagnostic.", diagnostic.Format());
        }

        private void PublishSaveLoadRequestCoordinatorUpdate(string operation, SaveLoadRequestUpdate update)
        {
            SaveLoadRequestSnapshot snapshot = update.Snapshot;
            string summary = snapshot.FormatSummary();
            bool warning = snapshot.Status.Equals("warning", StringComparison.OrdinalIgnoreCase);
            string status = warning ? "warning" : "ok";
            RuntimeMonitor.Log("Refactor save-load request coordinator status=" + status + " operation=" + (operation ?? string.Empty) + " " + summary + ".");
            Diagnostics.SetFeatureStatus(
                "Refactor.SaveLoadRequestCoordinator",
                status,
                operation ?? string.Empty,
                success: !warning,
                failureCount: warning ? 1 : 0,
                lastError: warning ? "SaveLoad request coordinator observed duplicate or timeout state." : string.Empty,
                details: summary);
            SetHookStatus("Refactor.SaveLoadRequestCoordinator", status, "SaveLoadRequestCoordinator." + (operation ?? string.Empty), summary);
            SetHookStatus(
                "Refactor.SaveLoadBoundary",
                snapshot.BoundaryStatus,
                "SaveLoadRequestCoordinator." + (operation ?? string.Empty),
                summary);
            if (snapshot.DuplicateRequests > 0)
            {
                SetHookStatus(
                    "Refactor.DuplicateLoadRequests",
                    snapshot.HasUnsuppressedDuplicates ? "warning" : "suppressed",
                    "SaveLoadRequestCoordinator." + (operation ?? string.Empty),
                    summary);
            }
        }

        private void RecordTitleReturnBoundaryEvent(string kind, string source, string details, int? slot)
        {
            TitleReturnBoundarySnapshot snapshot = titleReturnBoundaryLedger.RecordEvent(
                kind,
                source,
                details,
                slot,
                currentRuntimePhase,
                UI.InputContext,
                saveLoadRequestCoordinator.GetSnapshot().FormatSummary(),
                resourceLifecycleLedger.GetSnapshot().FormatSummary(),
                BuildModOwnerObjectGraphSummary(),
                Thread.CurrentThread.ManagedThreadId);
            PublishTitleReturnBoundaryLedgerUpdate(kind, snapshot);
        }

        private void CaptureTitleReturnObjectGraphSnapshot(string boundary, string source, string reason, int? slot)
        {
            if (saveLoadObjectSnapshotMode == SaveLoadObjectSnapshotMode.Off)
            {
                RuntimeMonitor.Log(
                    "TitleReturn object graph snapshot skipped boundary=" + (boundary ?? string.Empty) +
                    " source=" + (source ?? string.Empty) +
                    " mode=Off smokeOnly=true reason=" + SingleLine(reason ?? string.Empty) + ".");
                return;
            }

            if (saveLoadObjectSnapshotMode == SaveLoadObjectSnapshotMode.Lite)
            {
                TitleReturnBoundarySnapshot liteSnapshot = titleReturnBoundaryLedger.RecordObjectGraphSnapshot(
                    boundary,
                    reason,
                    source,
                    slot,
                    currentRuntimePhase,
                    UI.InputContext,
                    "mode=Lite; requestId=" + DiagnosticOrNone(saveLoadRequestCoordinator.GetCurrentRequestIdForDiagnostics()),
                    "mode=Lite",
                    "mode=Lite",
                    BuildLiteTitleReturnObjectGraphSections());
                PublishTitleReturnBoundaryLedgerUpdate("Snapshot:" + (boundary ?? string.Empty), liteSnapshot);
                return;
            }

            TitleReturnBoundarySnapshot snapshot = titleReturnBoundaryLedger.RecordObjectGraphSnapshot(
                boundary,
                reason,
                source,
                slot,
                currentRuntimePhase,
                UI.InputContext,
                saveLoadRequestCoordinator.GetSnapshot().FormatSummary(),
                resourceLifecycleLedger.GetSnapshot().FormatSummary(),
                BuildModOwnerObjectGraphSummary(),
                BuildTitleReturnObjectGraphSections());
            PublishTitleReturnBoundaryLedgerUpdate("Snapshot:" + (boundary ?? string.Empty), snapshot);
        }

        private IReadOnlyList<TitleReturnObjectGraphSection> BuildLiteTitleReturnObjectGraphSections()
        {
            return new[]
            {
                new TitleReturnObjectGraphSection(
                    "Runtime",
                    "snapshotMode=Lite; inputContext=" + UI.InputContext + "; phase=" + currentRuntimePhase + "; currentLoadingSlot=" + (currentLoadingSlot.HasValue ? currentLoadingSlot.Value.ToString(CultureInfo.InvariantCulture) : "none")),
                new TitleReturnObjectGraphSection(
                    "SaveLoad",
                    "requestId=" + DiagnosticOrNone(saveLoadRequestCoordinator.GetCurrentRequestIdForDiagnostics())),
                new TitleReturnObjectGraphSection(
                    "Boundary",
                    "boundaryId=" + DiagnosticOrNone(titleReturnBoundaryLedger.GetCurrentBoundaryIdForDiagnostics()))
            };
        }

        private IReadOnlyList<TitleReturnObjectGraphSection> BuildTitleReturnObjectGraphSections()
        {
            var sections = new List<TitleReturnObjectGraphSection>();
            sections.Add(new TitleReturnObjectGraphSection("Runtime", "snapshotMode=Full; inputContext=" + UI.InputContext + "; phase=" + currentRuntimePhase + "; currentLoadingSlot=" + (currentLoadingSlot.HasValue ? currentLoadingSlot.Value.ToString(CultureInfo.InvariantCulture) : "none")));
            sections.Add(new TitleReturnObjectGraphSection("OwnerRoots", BuildOwnerRootsObjectGraphSummary()));

            foreach (Func<TitleReturnObjectGraphSection> provider in titleReturnObjectGraphProviders.ToArray())
            {
                try
                {
                    TitleReturnObjectGraphSection section = provider();
                    if (section != null && !string.IsNullOrWhiteSpace(section.Name))
                        sections.Add(section);
                }
                catch (Exception ex)
                {
                    sections.Add(new TitleReturnObjectGraphSection("ProviderError", SingleLine(ex.GetType().Name + ": " + ex.Message)));
                }
            }

            return sections;
        }

        private string BuildModOwnerObjectGraphSummary()
        {
            ModOwnerLedgerSnapshot snapshot = modOwnerLedger.GetSnapshot();
            return snapshot.FormatSummary() + "; byOwner={" + snapshot.FormatPerOwnerSummary() + "}";
        }

        private string BuildOwnerRootsObjectGraphSummary()
        {
            InputOwnerSnapshot inputSnapshot = Input.GetOwnerSnapshot();
            EventHandlerCleanupSnapshot eventSnapshot = Events.GetHandlerCleanupSnapshot();
            return "input={" + inputSnapshot.FormatSummary() + "}" +
                "; events={" + eventSnapshot.FormatSummary() + "}" +
                "; configPages={byOwner={" + BuildConfigPageOwnerSummary() + "}}";
        }

        private string BuildConfigPageOwnerSummary()
        {
            if (configMenuRuntime == null)
                return "none";

            try
            {
                string value = string.Join("; ", configMenuRuntime.GetPages()
                    .GroupBy(page => page.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase)
                    .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                    .Take(32)
                    .Select(group => SanitizeMetricKey(group.Key) + "=" + group.Count().ToString(CultureInfo.InvariantCulture)));
                return string.IsNullOrWhiteSpace(value) ? "none" : value;
            }
            catch (Exception ex)
            {
                return "error_" + SanitizeMetricKey(ex.GetType().Name) + "=1";
            }
        }

        private void PublishTitleReturnBoundaryLedgerUpdate(string operation, TitleReturnBoundarySnapshot snapshot)
        {
            string summary = snapshot.FormatSummary();
            RuntimeMonitor.Log("TitleReturn boundary ledger operation=" + (operation ?? string.Empty) + " " + summary + ".");
            if (snapshot.LatestObjectGraph != null)
                RuntimeMonitor.Log("TitleReturn object graph snapshot " + snapshot.LatestObjectGraph.Format() + ".");
            if ((operation ?? string.Empty).StartsWith("Snapshot:", StringComparison.OrdinalIgnoreCase) && snapshot.LatestObjectGraphDelta != null)
                RuntimeMonitor.Log("SaveLoad cycle object delta " + snapshot.LatestObjectGraphDelta.Format() + ".");

            Diagnostics.SetFeatureStatus(
                "Refactor.TitleReturnBoundaryLedger",
                snapshot.Status,
                operation ?? string.Empty,
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: summary);
            SetHookStatus(
                "Refactor.TitleReturnBoundaryLedger",
                snapshot.Status,
                "TitleReturnBoundaryLedger." + (operation ?? string.Empty),
                summary);
            SetHookStatus(
                "Refactor.TitleReturnObjectGraphSnapshot",
                snapshot.ObjectGraphSnapshots.Count > 0 ? "captured" : "pending",
                "TitleReturnBoundaryLedger." + (operation ?? string.Empty),
                snapshot.FormatLatestSnapshots());
            SetHookStatus(
                "Refactor.SaveLoadCycleObjectDeltaLedger",
                snapshot.ObjectGraphDeltas.Count > 0 ? "captured" : "pending",
                "TitleReturnBoundaryLedger." + (operation ?? string.Empty),
                snapshot.FormatLatestDeltas());
        }

        private string BuildRuntimeReportContext()
        {
            IDtmDiagnosticsSnapshot snapshot = CreateDiagnosticsSnapshot();
            PlayerDoctorRuntimeResult playerDoctorResult = playerDoctor.GetLatest();
            var builder = new StringBuilder();
            builder.AppendLine("RuntimeContextGenerated: " + DateTimeOffset.Now.ToString("o"));
            builder.AppendLine("DTMAPIVersion: " + ApiVersion);
            builder.AppendLine("BinaryVersion: " + BinaryVersion);
            builder.AppendLine("StartedAt: " + StartedAt.ToString("o"));
            builder.AppendLine("DiscoveredMods: " + snapshot.Mods.Count);
            builder.AppendLine("LoadedMods: " + snapshot.LoadedMods.Count);
            builder.AppendLine("Errors: " + snapshot.Errors.Count);
            builder.AppendLine("Warnings: " + snapshot.Warnings.Count);
            builder.AppendLine("Hooks: " + snapshot.HookStatuses.Count);
            builder.AppendLine("Features: " + snapshot.FeatureStatuses.Count);
            builder.AppendLine("PlayerDoctor: status=" + playerDoctorResult.Status + "; exitCode=" + playerDoctorResult.ExitCode.ToString(CultureInfo.InvariantCulture) + "; " + SingleLine(playerDoctorResult.Summary));
            builder.AppendLine("RefactorScaffoldFlags: " + refactorOptions.ToSummary());
            builder.AppendLine("SaveLoadObjectSnapshotMode: " + saveLoadObjectSnapshotMode);
            builder.AppendLine("LifecycleObservation: " + (refactorOptions.LifecycleObservation ? lifecycleObservation.FormatSummary() : "disabled"));
            builder.AppendLine("LifecycleBoundaryContract: " + (refactorOptions.LifecycleObservation ? lifecycleBoundaryContract.GetSnapshot().FormatSummary() : "disabled"));
            builder.AppendLine("LifecycleBoundaryPolicies: " + (refactorOptions.LifecycleObservation ? lifecycleBoundaryContract.FormatPolicyCatalog() : "disabled"));
            builder.AppendLine("ShadowContentRegistry: " + (latestShadowContentRegistrySnapshot == null ? "not-run" : latestShadowContentRegistrySnapshot.FormatSummary()));
            builder.AppendLine("ContentRegistry: " + (refactorOptions.ContentManifestRegistry ? latestContentManifestRegistrySnapshot == null ? "not-run" : latestContentManifestRegistrySnapshot.FormatSummary() : "disabled"));
            builder.AppendLine("ManifestScannerDiagnostics: " + latestManifestScannerDiagnosticTotals.FormatSummary());
            builder.AppendLine("ManifestRegistry: " + (refactorOptions.ContentManifestRegistry ? latestContentManifestRegistrySnapshot == null ? "not-run" : latestContentManifestRegistrySnapshot.FormatManifestSummary() : "disabled"));
            builder.AppendLine("DependencyCompatibility: " + (refactorOptions.ContentManifestRegistry ? latestContentManifestRegistrySnapshot == null ? "not-run" : latestContentManifestRegistrySnapshot.FormatDependencySummary() : "disabled"));
            builder.AppendLine("ContentPackOwnership: " + (refactorOptions.ContentManifestRegistry ? latestContentManifestRegistrySnapshot == null ? "not-run" : latestContentManifestRegistrySnapshot.FormatOwnershipSummary() : "disabled"));
            builder.AppendLine("RegistryDiffs: " + (refactorOptions.ContentManifestRegistry ? latestContentManifestRegistrySnapshot == null ? "not-run" : latestContentManifestRegistrySnapshot.FormatDiffSummary() : "disabled"));
            builder.AppendLine("ContentRefreshGenerations: " + contentRefreshGenerations.GetSnapshot().FormatSummary());
            ResourceLifecycleSnapshot resourceSnapshot = resourceLifecycleLedger.GetSnapshot();
            builder.AppendLine("ResourceLifecycleLedgerSummary: " + (refactorOptions.ResourceLifecycleLedger ? resourceSnapshot.FormatSummary() : "disabled"));
            builder.AppendLine("ReturnedToTitleCleanupPlan: " + (refactorOptions.ResourceLifecycleLedger ? resourceSnapshot.FormatReturnedToTitleCleanupPlan() : "disabled"));
            builder.AppendLine("TitleIdleResourceGrowth: " + (refactorOptions.ResourceLifecycleLedger ? resourceSnapshot.FormatTitleIdleResourceGrowth() : "disabled"));
            builder.AppendLine("SaveLoadRequestCoordinator: " + (refactorOptions.SaveLoadRequestCoordinator ? saveLoadRequestCoordinator.GetSnapshot().FormatSummary() : "disabled"));
            TitleReturnBoundarySnapshot titleReturnSnapshot = titleReturnBoundaryLedger.GetSnapshot();
            builder.AppendLine("TitleReturnBoundaryLedger: " + titleReturnSnapshot.FormatSummary());
            builder.AppendLine("TitleReturnObjectGraphSnapshots: " + titleReturnSnapshot.FormatLatestSnapshots());
            builder.AppendLine("SaveLoadCycleObjectDeltaLedger: " + titleReturnSnapshot.FormatLatestDeltas());
            builder.AppendLine("HookStatusQueue: " + (refactorOptions.HookStatusQueue ? hookStatusQueue.GetSnapshot().FormatSummary() : "disabled"));
            builder.AppendLine("OffThreadHookRequests: " + (refactorOptions.HookStatusQueue ? hookStatusQueue.GetSnapshot().FormatOffThreadSummary() : "disabled"));
            builder.AppendLine("EventMainThreadBoundary: " + (refactorOptions.EventMainThreadBoundary ? Events.GetBoundarySnapshot().FormatSummary() : "disabled"));
            builder.AppendLine("RuntimeDemand: " + demandCoordinator.GetSnapshot().FormatSummary());
            builder.AppendLine("HookInstallScheduler: " + GetFeatureStatusDetails("Refactor.HookInstallScheduler"));
            builder.AppendLine("CoreHookReadiness: " + GetHookStatusDetails("Refactor.CoreHookReadiness"));
            builder.AppendLine("FeatureHookReadiness: " + GetHookStatusDetails("Refactor.FeatureHookReadiness"));
            builder.AppendLine("AssemblyLoadSubscription: " + GetHookStatusDetails("Refactor.AssemblyLoadSubscription"));
            builder.AppendLine("RetryTimerAlive: " + GetHookStatusDetails("Refactor.RetryTimerAlive"));
            ModOwnerLedgerSnapshot ownerSnapshot = modOwnerLedger.GetSnapshot();
            InputOwnerSnapshot inputSnapshot = Input.GetOwnerSnapshot();
            EventHandlerCleanupSnapshot eventSnapshot = Events.GetHandlerCleanupSnapshot();
            builder.AppendLine("ModOwnerLifecycle: " + (refactorOptions.ModOwnerLedger ? ownerSnapshot.FormatSummary() : "disabled"));
            builder.AppendLine("ModOwnerLifecycleSummary: " + (refactorOptions.ModOwnerLedger ? ownerSnapshot.FormatOwnerRegistrations() : "disabled"));
            builder.AppendLine("ModLoadTransaction: " + (refactorOptions.ModLoadTransaction ? ownerSnapshot.FormatSummary() : "disabled"));
            builder.AppendLine("OwnerBoundInput: " + (refactorOptions.OwnerBoundInput ? inputSnapshot.FormatSummary() : "disabled"));
            builder.AppendLine("InputLocalSnapshot: " + Input.GetLocalSnapshotDiagnostics().FormatSummary());
            builder.AppendLine("EventHandlerCleanup: " + (refactorOptions.EventHandlerQuarantine ? eventSnapshot.FormatSummary() : "disabled") +
                "; timingEnableSource=refactor-scaffold.json:EventHandlerTimingDiagnostics|env:DTMAPI_REFACTOR_EVENT_HANDLER_TIMING_DIAGNOSTICS");
            builder.AppendLine("ConfigPreviewAudit: " + (refactorOptions.ConfigPreviewAudit ? GetFeatureStatusDetails("Refactor.ConfigPreviewAudit") : "disabled"));
            builder.AppendLine("FailedModRollback: " + (refactorOptions.ModOwnerLedger ? ownerSnapshot.FormatRollbackSummary() : "disabled"));
            foreach (Func<string> provider in runtimeReportContextProviders.ToArray())
            {
                try
                {
                    string extra = provider();
                    if (!string.IsNullOrWhiteSpace(extra))
                        builder.AppendLine(extra.TrimEnd('\r', '\n'));
                }
                catch (Exception ex)
                {
                    builder.AppendLine("RuntimeReportContextProviderError: " + SingleLine(ex.GetType().Name + ": " + ex.Message));
                }
            }
            builder.AppendLine();

            foreach (IDtmModStatusInfo mod in snapshot.Mods.OrderBy(m => m.UniqueID, StringComparer.OrdinalIgnoreCase))
            {
                builder.Append("MOD ");
                builder.Append(SingleLine(mod.UniqueID));
                builder.Append(" statusCode=");
                builder.Append(SingleLine(mod.StatusCode));
                builder.Append(" loaded=");
                builder.Append(mod.Loaded ? "true" : "false");
                builder.Append(" officialEnabled=");
                builder.Append(mod.OfficialEnabled ? "true" : "false");
                builder.Append(" source=");
                builder.Append(SingleLine(mod.Source));
                builder.Append(" version=");
                builder.Append(SingleLine(mod.Version));
                builder.Append(" name=");
                builder.Append(SingleLine(mod.Name));
                if (!string.IsNullOrWhiteSpace(mod.Reason))
                {
                    builder.Append(" reason=");
                    builder.Append(SingleLine(mod.Reason));
                }

                builder.AppendLine();
            }

            foreach (IDtmErrorInfo error in snapshot.Errors.Where(e => snapshot.Mods.Any(m => m.UniqueID.Equals(e.Owner, StringComparison.OrdinalIgnoreCase))))
            {
                IDtmModStatusInfo? mod = snapshot.Mods.FirstOrDefault(m => m.UniqueID.Equals(error.Owner, StringComparison.OrdinalIgnoreCase));
                bool loadFailure = mod == null || !mod.Loaded || IsModLoadFailureStatusCode(mod.StatusCode);
                builder.Append(loadFailure ? "MOD-LOAD-FAILURE owner=" : "MOD-RUNTIME-DIAGNOSTIC owner=");
                builder.Append(SingleLine(error.Owner));
                builder.Append(" statusCode=");
                builder.Append(SingleLine(mod?.StatusCode ?? "unknown-error"));
                builder.Append(" loaded=");
                builder.Append(mod != null && mod.Loaded ? "true" : "false");
                builder.Append(" message=");
                builder.Append(SingleLine(error.Message));
                builder.Append(" details=");
                builder.Append(SingleLine(error.Details));
                if (loadFailure && (IsCodeLoadSideEffectStatusCode(mod?.StatusCode) || IsCodeLoadSideEffectError(error)))
                {
                    bool assemblyLoaded = loadedAssemblyOwnerIds.Contains(error.Owner);
                    bool restartRequired = modOwnerLifecycle.RequiresRestart(error.Owner);
                    builder.Append(" partialSideEffectsPossible=");
                    builder.Append(assemblyLoaded ? "true" : "false");
                    builder.Append(" restartRequired=");
                    builder.Append(restartRequired ? "true" : "false");
                }
                builder.AppendLine();
            }

            AppendManifestAndLegacyFailureContext(builder, snapshot);

            bool applicationQuitObserved = false;
            if (!string.IsNullOrWhiteSpace(Diagnostics.LatestLogPath) && File.Exists(Diagnostics.LatestLogPath))
            {
                try
                {
                    applicationQuitObserved = File.ReadLines(Diagnostics.LatestLogPath)
                        .Any(line => line.IndexOf("Unity OnApplicationQuit observed by DTMAPI bootstrap", StringComparison.OrdinalIgnoreCase) >= 0);
                }
                catch
                {
                    applicationQuitObserved = false;
                }
            }

            builder.AppendLine("OnApplicationQuitObserved: " + (applicationQuitObserved ? "true" : "false"));
            return builder.ToString();
        }

        private void RunPlayerDoctorAtStartup()
        {
            PlayerDoctorRuntimeResult result = playerDoctor.RunOnce(Paths, ApiVersion);
            string status = result.Status;
            bool success = result.Completed && result.ExitCode == 0;
            int failureCount = result.Completed && result.ExitCode == 2 ? result.ErrorCount : (result.Completed ? 0 : 1);
            string lastError = success ? string.Empty : result.Summary;
            Diagnostics.SetFeatureStatus(
                "Diagnostics.PlayerDoctor",
                status,
                "StartupOnce",
                success,
                failureCount,
                lastError,
                result.Summary);

            if (result.Status.Equals("findings", StringComparison.OrdinalIgnoreCase))
            {
                Diagnostics.RecordError(
                    "DTMAPI.PlayerDoctor",
                    "Player Doctor found installation or compatibility errors.",
                    result.Summary + " Run 3_check_dtmapi_status.bat for the full offline report.");
                RuntimeMonitor.Log("Player Doctor startup findings: " + result.Summary, LogLevel.Warn);
            }
            else if (result.Status.Equals("warnings", StringComparison.OrdinalIgnoreCase))
            {
                Diagnostics.RecordWarning(
                    "DTMAPI.PlayerDoctor",
                    "Player Doctor completed with warnings.",
                    result.Summary + " Run 3_check_dtmapi_status.bat for the full offline report.");
                RuntimeMonitor.Log("Player Doctor startup warnings: " + result.Summary, LogLevel.Warn);
            }
            else if (result.Status.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
                     result.Status.Equals("timeout", StringComparison.OrdinalIgnoreCase) ||
                     result.Status.Equals("unavailable", StringComparison.OrdinalIgnoreCase))
            {
                Diagnostics.RecordWarning(
                    "DTMAPI.PlayerDoctor",
                    "Player Doctor startup summary is unavailable.",
                    result.Summary + " Runtime loading continues; run 3_check_dtmapi_status.bat for the offline path.");
                RuntimeMonitor.Log("Player Doctor startup summary unavailable: " + result.Summary, LogLevel.Warn);
            }
            else
            {
                RuntimeMonitor.Log("Player Doctor startup status: " + result.Status + "; " + result.Summary);
            }
        }

        private string GetHookStatusDetails(string hookId)
        {
            IHookStatusInfo? status = Diagnostics.GetHookStatuses().FirstOrDefault(h => h.HookId.Equals(hookId, StringComparison.OrdinalIgnoreCase));
            if (status == null)
                return "not-run";
            return "status=" + SingleLine(status.Status) + "; source=" + SingleLine(status.Source) + "; details=" + SingleLine(status.Details);
        }

        private string GetFeatureStatusDetails(string featureId)
        {
            IDtmFeatureStatusInfo? status = Diagnostics.GetFeatureStatuses().FirstOrDefault(f => f.FeatureId.Equals(featureId, StringComparison.OrdinalIgnoreCase));
            if (status == null)
                return "not-run";
            return "status=" + SingleLine(status.Status) + "; operation=" + SingleLine(status.LastOperation) + "; success=" + (status.Success ? "true" : "false") + "; details=" + SingleLine(status.Details);
        }

        private void AppendManifestAndLegacyFailureContext(StringBuilder builder, IDtmDiagnosticsSnapshot snapshot)
        {
            foreach (IDtmErrorInfo error in snapshot.Errors.Where(IsManifestScannerError).Take(30))
            {
                builder.Append("MANIFEST-LOAD-FAILURE owner=");
                builder.Append(SingleLine(error.Owner));
                builder.Append(" message=");
                builder.Append(SingleLine(error.Message));
                builder.Append(" details=");
                builder.Append(SingleLine(error.Details));
                builder.AppendLine();
            }

            IEnumerable<string> legacyFailureLines = ReadDiagnosticLogBoundaryLines(Diagnostics.LatestLogPath, 600, 1200)
                .Concat(ReadDiagnosticLogBoundaryLines(Path.Combine(Paths.GamePath, "BepInEx", "LogOutput.log"), 600, 1200));
            foreach (string line in legacyFailureLines.Where(IsLegacyBepInExFailureLine).Distinct(StringComparer.Ordinal).Take(40))
            {
                builder.Append("BEPINEX-OR-MOD-ERROR-CANDIDATE line=");
                builder.Append(SingleLine(line));
                builder.AppendLine();
            }
        }

        private static bool IsManifestScannerError(IDtmErrorInfo error)
        {
            return error.Owner.Equals("DTMAPI.ModScanner", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLegacyBepInExFailureLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            if (line.IndexOf("Hook status", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.IndexOf("verified", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            if (line.IndexOf("Failed to patch", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.IndexOf("HarmonyException", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.IndexOf("Owner can't be an array or an interface", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            bool explicitErrorPrefix = line.IndexOf("[Error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.IndexOf("] Error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                line.IndexOf(" BepInEx", StringComparison.OrdinalIgnoreCase) >= 0 && line.IndexOf(" Error", StringComparison.OrdinalIgnoreCase) >= 0;
            bool exceptionWithPluginHost = line.IndexOf("Exception", StringComparison.OrdinalIgnoreCase) >= 0 &&
                ContainsAny(line, "HarmonyLib", "BepInEx", "Chainloader", "Plugin");

            return explicitErrorPrefix || exceptionWithPluginHost;
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            foreach (string needle in needles)
            {
                if (value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static IEnumerable<string> ReadDiagnosticLogBoundaryLines(string path, int headLines, int tailLines)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                yield break;

            string[] allLines;
            try
            {
                allLines = File.ReadAllLines(path);
            }
            catch
            {
                yield break;
            }

            int head = Math.Max(0, headLines);
            int tail = Math.Max(0, tailLines);
            int headEnd = Math.Min(head, allLines.Length);
            int tailStart = Math.Max(headEnd, allLines.Length - tail);

            for (int i = 0; i < headEnd; i++)
                yield return allLines[i];
            for (int i = tailStart; i < allLines.Length; i++)
                yield return allLines[i];
        }

        private static bool IsModLoadFailureStatusCode(string? statusCode)
        {
            if (string.IsNullOrWhiteSpace(statusCode))
                return false;

            return string.Equals(statusCode, "code-load-error", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(statusCode, "entry-dll-error", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(statusCode, "missing-dependency", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(statusCode, "api-too-new", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(statusCode, "manifest-error", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCodeLoadSideEffectStatusCode(string? statusCode)
        {
            return string.Equals(statusCode, "code-load-error", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCodeLoadSideEffectError(IDtmErrorInfo error)
        {
            string combined = (error?.Message ?? string.Empty) + " " + (error?.Details ?? string.Empty);
            return combined.IndexOf("Failed to load code mod", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsReservedRuntimeOwnerId(string uniqueId)
        {
            return processLifetimeProviderIds.Contains(uniqueId) ||
                uniqueId.Equals("DTMAPI", StringComparison.OrdinalIgnoreCase) ||
                uniqueId.Equals("DTMAPI.ModConfigMenu", StringComparison.OrdinalIgnoreCase) ||
                uniqueId.Equals("DTMAPI.GameBridge.DolocTown", StringComparison.OrdinalIgnoreCase) ||
                uniqueId.Equals("DTMAPI.DebugConsoleHost", StringComparison.OrdinalIgnoreCase) ||
                uniqueId.StartsWith("DTMAPI.Smoke.", StringComparison.OrdinalIgnoreCase);
        }

        private static string SingleLine(string? value, int maxChars = 1200)
        {
            string singleLine = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (string.IsNullOrWhiteSpace(singleLine))
                return "-";
            if (singleLine.Length <= maxChars)
                return singleLine;
            return singleLine.Substring(0, maxChars) + "...[truncated]";
        }

        private static string DiagnosticOrNone(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "none";
            return value!.Trim();
        }

        private static string SanitizeMetricKey(string value)
        {
            string text = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
            var builder = new StringBuilder(text.Length);
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

        public RuntimeSnapshot CreateSnapshot()
        {
            return RuntimeSnapshotFactory.CreateRuntimeSnapshot(
                startedAt,
                Paths,
                discoveredModsSnapshot,
                loadedModsSnapshot,
                ModRegistry.GetAll(),
                Diagnostics.GetErrors(),
                Diagnostics.GetWarnings(),
                Diagnostics.GetHookStatuses(),
                Diagnostics.GetFeatureStatuses(),
                configMenuRuntime?.GetPages() ?? new IConfigMenuPage[0],
                Diagnostics.GetLatestLogPath(),
                Diagnostics.GetLatestReportPath(),
                UI.LastExportPath);
        }

        public IDtmDiagnosticsSnapshot CreateDiagnosticsSnapshot()
        {
            IReadOnlyList<IDtmErrorInfo> errors = Diagnostics.GetErrors();
            return RuntimeSnapshotFactory.CreateDiagnosticsSnapshot(
                startedAt,
                discoveredModsSnapshot,
                loadedModsSnapshot,
                errors,
                Diagnostics.GetWarnings(),
                Diagnostics.GetHookStatuses(),
                Diagnostics.GetFeatureStatuses(),
                Diagnostics.GetLatestLogPath(),
                Diagnostics.GetLatestReportPath(),
                modOwnerLifecycle.GetRestartRequiredOwners());
        }

        IDtmDiagnosticsSnapshot IDtmDiagnosticsApi.GetSnapshot() => CreateDiagnosticsSnapshot();

        public IReadOnlyList<IContentItemInfo> GetIndexedContentItems() => Content.GetIndexedItems();

        public IReadOnlyList<IContentItemInfo> GetAllIndexedContentItems() => Content.GetAllIndexedItems();

        public IContentItemInfo? GetIndexedContentItem(string itemId) => Content.GetIndexedItem(itemId);

        public IContentItemInfo? GetAnyIndexedContentItem(string itemId) => Content.GetAnyIndexedItem(itemId);

        private void InitializeAuthorSession()
        {
            authorSessionDescriptorLoad = AuthorSessionDescriptorStore.ConsumeStartup(Paths.GamePath, ApiVersion);
            if (!authorSessionDescriptorLoad.Present)
            {
                RuntimeMonitor.Log("Author session disabled: no startup descriptor; no listener, watcher, timer or file poll was created.");
                return;
            }

            if (!authorSessionDescriptorLoad.Accepted || authorSessionDescriptorLoad.Descriptor == null)
            {
                string details = authorSessionDescriptorLoad.Code + ": " + authorSessionDescriptorLoad.Message;
                Diagnostics.RecordWarning("DTMAPI.AuthorSession", "Author session startup descriptor was rejected.", details);
                RuntimeMonitor.Log("Author session descriptor rejected: " + details, LogLevel.Warn);
                return;
            }

            if (!AuthorSessionHost.TryCreate(
                authorSessionDescriptorLoad.Descriptor,
                Paths.GamePath,
                ApiVersion,
                out AuthorSessionHost? host,
                out AuthorSessionValidationResult validation) || host == null)
            {
                string details = validation.Code + ": " + validation.Message;
                Diagnostics.RecordWarning("DTMAPI.AuthorSession", "Author session host validation failed.", details);
                RuntimeMonitor.Log("Author session host validation failed: " + details, LogLevel.Warn);
                return;
            }

            AuthorSessionValidationResult start = host.Start();
            if (!start.Accepted)
            {
                host.Close("start-rejected:" + start.Code);
                string details = start.Code + ": " + start.Message;
                Diagnostics.RecordWarning("DTMAPI.AuthorSession", "Author session listener did not start.", details);
                RuntimeMonitor.Log("Author session listener did not start: " + details, LogLevel.Warn);
                return;
            }

            authorSessionHost = host;
            RuntimeMonitor.Log(
                "Explicit author session started session=" + host.SessionId +
                "; pipe=" + host.PipeName +
                "; expiresAtUtc=" + host.ExpiresAtUtc.ToString("O", CultureInfo.InvariantCulture) +
                "; ordinaryPlayerFacilities=false.");
        }

        private void ProcessAuthorSessionRequests()
        {
            AuthorSessionHost? host = authorSessionHost;
            if (host == null)
                return;
            if (DateTimeOffset.UtcNow >= host.ExpiresAtUtc)
            {
                CloseAuthorSession("Expired");
                return;
            }

            AuthorSessionProcessResult result = host.ProcessPending(request =>
            {
                Func<AuthorSessionRequest, AuthorSessionOperationResult>? handler = authorSessionOperationHandler;
                return handler == null
                    ? AuthorSessionOperationResult.Error("runtime-handler-unavailable", "The GameBridge author-session handler is unavailable.")
                    : handler(request);
            });
            if (!result.Success || result.Failed > 0)
            {
                RuntimeMonitor.LogOnce(
                    "author-session-process-" + result.Code,
                    "Author session Runtime processing status=" + result.Code +
                    "; handled=" + result.Handled.ToString(CultureInfo.InvariantCulture) +
                    "; failed=" + result.Failed.ToString(CultureInfo.InvariantCulture) +
                    "; message=" + result.Message,
                    LogLevel.Warn);
            }
        }

        private void CloseAuthorSession(string reason)
        {
            AuthorSessionHost? host = authorSessionHost;
            if (host == null)
                return;
            host.Close(reason ?? string.Empty);
            RuntimeMonitor.Log("Explicit author session closed reason=" + (reason ?? string.Empty) + "; " + host.GetSnapshot().FormatSummary() + ".");
            authorSessionHost = null;
        }

        private void DiscoverMods()
        {
            Stopwatch scan = Stopwatch.StartNew();
            discoveredMods.Clear();
            discoveredById.Clear();
            latestAuthorSourceState = AuthorSourceStateStore.Load(Paths.GamePath);
            if (!string.IsNullOrWhiteSpace(latestAuthorSourceState.LoadFailure))
            {
                string details = "Rejected author source state at " + latestAuthorSourceState.SourcePath + ": " + latestAuthorSourceState.LoadFailure;
                Diagnostics.RecordWarning("DTMAPI.AuthorSource", "Author source state was rejected.", details);
                RuntimeMonitor.Log(details, LogLevel.Warn);
            }
            var scanner = new ModScanner(Paths, nativeWorkshopSubscriptions, latestAuthorSourceState, IsAuthorSessionActive);
            IReadOnlyList<DiscoveredMod> discovered = scanner.Discover();
            latestManifestScannerErrors = scanner.Errors;
            latestManifestScannerWarnings = scanner.Warnings;
            latestManifestScannerDiagnosticTotals = scanner.DiagnosticTotals;
            latestSourceSelectionSummary = scanner.SourceSelectionSummary;
            latestAuthorSourceSelectionDecisions = scanner.SourceSelectionDecisions;
            foreach (DiscoveredMod mod in discovered)
            {
                discoveredMods.Add(mod);
                if (!string.IsNullOrWhiteSpace(mod.Manifest.UniqueID) && !discoveredById.ContainsKey(mod.Manifest.UniqueID))
                    discoveredById.Add(mod.Manifest.UniqueID, mod);
            }
            PublishDiscoveredModsSnapshot();
            foreach (string error in latestManifestScannerErrors)
            {
                Diagnostics.RecordError("DTMAPI.ModScanner", "Manifest discovery failed.", error);
                RuntimeMonitor.Log("Manifest discovery failed: " + error, LogLevel.Warn);
            }
            foreach (string warning in latestManifestScannerWarnings)
            {
                Diagnostics.RecordWarning("DTMAPI.ModScanner", "Manifest discovery warning.", warning);
                RuntimeMonitor.Log("Manifest discovery warning: " + warning, LogLevel.Warn);
            }
            Workshop.SetMods(discoveredMods);
            Stopwatch content = Stopwatch.StartNew();
            RefreshShadowContentRegistry("DiscoverMods");
            RefreshContentManifestRegistry("DiscoverMods");
            RuntimeMonitor.Log($"Discovered {discoveredMods.Count} DTMAPI-capable mod folder(s).");
            RuntimeMonitor.Log("Manifest scanner diagnostics " + latestManifestScannerDiagnosticTotals.FormatSummary() + ".");
            RuntimeMonitor.Log("Author source state " + latestAuthorSourceState.FormatSummary() + ".");
            RuntimeMonitor.Log("Source authority " + latestSourceSelectionSummary + "; native={" + nativeWorkshopSubscriptions.FormatSummary() + "}.");
            RuntimeMonitor.Log(
                "Official local MODS root = " + scanner.OfficialLocalModsRoot +
                "; exists=" + scanner.OfficialLocalModsRootExists +
                "; folders=" + scanner.OfficialLocalDirectoryCount +
                "; enablementFile=" + scanner.OfficialEnablementFilePath +
                "; enablementFileExists=" + scanner.OfficialEnablementFileExists +
                "; enablementEntries=" + scanner.OfficialEnablementEntryCount + ".");
            RuntimeMonitor.Log("Startup segment ManifestScan elapsedMs=" + scanner.ManifestScanElapsedMilliseconds + "; OfficialModsScan elapsedMs=" + scanner.OfficialModsScanElapsedMilliseconds + "; WorkshopScan elapsedMs=" + scanner.WorkshopScanElapsedMilliseconds + "; ContentQueryIndex elapsedMs=" + content.ElapsedMilliseconds + "; DiscoverMods totalMs=" + scan.ElapsedMilliseconds + ".");
        }

        private int LoadMods(bool initialLoad)
        {
            DiscoveredMod[] previousLoadedSnapshot = loadedModsSnapshot;
            if (initialLoad)
            {
                loadedMods.Clear();
                modInstances.Clear();
                PublishLoadedModsSnapshot();
            }
            else
            {
                ReconcileActiveOwners();
            }

            int loadedNow = 0;
            HashSet<string> dependencyCycleBlockedIds;
            foreach (DiscoveredMod mod in OrderMods(discoveredMods, out dependencyCycleBlockedIds))
            {
                bool alreadyLoaded = IsLoadedMod(mod.Manifest.UniqueID);
                if (!mod.OfficialEnabled)
                {
                    string reason = string.IsNullOrWhiteSpace(mod.EnablementReason) ? "已由来源管理路径禁用。" : mod.EnablementReason;
                    RuntimeMonitor.Log($"Skipping {mod.Manifest.UniqueID}: {reason}");
                    continue;
                }

                if (alreadyLoaded)
                    continue;

                if (modOwnerLifecycle.RequiresRestart(mod.Manifest.UniqueID))
                {
                    RuntimeMonitor.LogOnce(
                        "owner-restart-required-" + mod.Manifest.UniqueID,
                        mod.Manifest.UniqueID + " 已在本进程停用；已加载程序集或未清零平台根使重新启用需要重启游戏。",
                        LogLevel.Warn);
                    continue;
                }

                if (IsReservedRuntimeOwnerId(mod.Manifest.UniqueID))
                {
                    Diagnostics.RecordError(mod.Manifest.UniqueID, "UniqueID 被 DTMAPI 运行时保留。", "Reserved owner id: " + mod.Manifest.UniqueID);
                    RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：该 UniqueID 被 DTMAPI 运行时保留。", LogLevel.Warn);
                    continue;
                }

                if (!CanLoadApiVersion(mod))
                    continue;

                if (!CanLoadGameVersion(mod))
                    continue;

                if (dependencyCycleBlockedIds.Contains(mod.Manifest.UniqueID))
                {
                    Diagnostics.RecordError(mod.Manifest.UniqueID, "依赖循环阻止加载。", "This mod is part of a dependency cycle and is blocked until the cycle is resolved.");
                    RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：此 Mod 处于依赖循环中，已标记为 blocked。", LogLevel.Warn);
                    continue;
                }

                if (!initialLoad && mod.Classification.IsLegacyNativeCompatibility)
                {
                    modOwnerLifecycle.RequireColdStart(mod.Manifest.UniqueID);
                    RuntimeMonitor.LogOnce(
                        "legacy-native-cold-start-required-" + mod.Manifest.UniqueID,
                        "Third-party native compatibility Mod " + mod.Manifest.UniqueID +
                        " was discovered or enabled after startup. Its author-managed DLL and Hooks will not be loaded in this process; restart Doloc Town to cold-load it.",
                        LogLevel.Warn);
                    continue;
                }

                if (!CanLoadDependencies(mod))
                    continue;

                if (mod.Classification.IsContentPack)
                {
                    bool published = false;
                    try
                    {
                        modOwnerLifecycle.BeginEntry(mod.Manifest.UniqueID);
                        ModRegistry.AddLoaded(mod.Manifest);
                        loadedMods.Add(mod);
                        modOwnerLifecycle.Activate(mod.Manifest.UniqueID);
                        PublishLoadedModsSnapshot();
                        published = true;
                        loadedNow++;
                    }
                    catch (Exception ex)
                    {
                        DeactivateOwner(mod.Manifest.UniqueID, ModOwnerCleanupReason.EntryFailed, shutdown: false, transactionId: string.Empty);
                        Diagnostics.RecordError(mod.Manifest.UniqueID, "Failed to publish non-code mod.", ex.ToString());
                    }
                    if (published)
                    {
                        try
                        {
                            RuntimeMonitor.Log($"Indexed non-code mod {mod.Manifest.UniqueID} ({mod.Manifest.Type}).");
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                Diagnostics.RecordWarning(mod.Manifest.UniqueID, "Non-code mod completion log failed after successful publication.", ex.GetType().Name + ": " + ex.Message);
                            }
                            catch
                            {
                            }
                        }
                    }
                    continue;
                }

                if (LoadCodeMod(mod))
                    loadedNow++;
            }
            PublishLoadedModsSnapshot();
            string refreshReason = (initialLoad ? "LoadMods initial" : "LoadMods hot") + " loadedNow=" + loadedNow;
            MarkSourceContentDirty(previousLoadedSnapshot, loadedModsSnapshot, refreshReason);
            Stopwatch contentPublication = Stopwatch.StartNew();
            RefreshContentQueryIfDirty(refreshReason);
            RuntimeMonitor.Log(
                "Authoritative active-owner content publication = " + Content.IndexedItemCount +
                " indexed item row(s) from " + Content.IndexedItemSourceCount +
                " source mod(s); activeOwners=" + loadedMods.Count +
                "; elapsedMs=" + contentPublication.ElapsedMilliseconds + ".");
            RefreshConfigPageLocks();
            RefreshShadowContentRegistry(refreshReason);
            RefreshContentManifestRegistry(refreshReason);
            ObserveResourceContentSignature(refreshReason);
            PublishModOwnerLifecycleStatus(refreshReason);
            return loadedNow;
        }

        private void PublishDiscoveredModsSnapshot()
        {
            discoveredModsSnapshot = discoveredMods.ToArray();
            Volatile.Write(ref discoveredModsView, Array.AsReadOnly(discoveredModsSnapshot));
        }

        private void PublishLoadedModsSnapshot()
        {
            if (loadedModsSnapshot.Length == loadedMods.Count)
            {
                bool unchanged = true;
                for (int index = 0; index < loadedModsSnapshot.Length; index++)
                {
                    if (!ReferenceEquals(loadedModsSnapshot[index], loadedMods[index]))
                    {
                        unchanged = false;
                        break;
                    }
                }
                if (unchanged)
                    return;
            }
            loadedModsSnapshot = loadedMods.ToArray();
            Volatile.Write(ref loadedModsView, Array.AsReadOnly(loadedModsSnapshot));
        }

        private void MarkSourceContentDirty(
            IEnumerable<DiscoveredMod> previousOwners,
            IEnumerable<DiscoveredMod> currentOwners,
            string reason)
        {
            IEnumerable<string> ownerIds = (previousOwners ?? Array.Empty<DiscoveredMod>())
                .Concat(currentOwners ?? Array.Empty<DiscoveredMod>())
                .Where(mod => mod?.Manifest != null && !string.IsNullOrWhiteSpace(mod.Manifest.UniqueID))
                .Select(mod => mod.Manifest.UniqueID);
            // ContentRefreshGenerationService consumes this once with fixed distinct
            // and raw-scan budgets, deduplicates case-insensitively, and
            // conservatively collapses larger/hostile sets to "all".
            // Keep this source projection lazy so a source refresh never sorts or
            // materializes an unbounded owner array for diagnostics.
            contentRefreshGenerations.MarkDirty(ContentRefreshDomains.SourceDriven, ownerIds, reason);
        }

        private void RefreshContentQueryIfDirty(string reason)
        {
            if (!contentRefreshGenerations.TryGetDirty(ContentRefreshDomains.ContentQuery, out ContentRefreshDirtyBatch batch))
                return;

            bool generationCompleted = false;
            try
            {
                ContentQueryRebuildResult result = Content.RebuildCandidate(loadedModsSnapshot, batch.Generation, reason);
                ContentRefreshCompletionStatus status = result.Success
                    ? ContentRefreshCompletionStatus.Committed
                    : ContentRefreshCompletionStatus.Rejected;
                Content.BeforeGenerationCompleteForTest?.Invoke(batch);
                generationCompleted = contentRefreshGenerations.CompleteWithAtomicCommit(
                    batch,
                    new[]
                    {
                        new ContentRefreshCompletion(
                            "all",
                            status,
                            result.PreviousGeneration,
                            result.CurrentGeneration,
                            result.CurrentGeneration,
                            result.Success
                                ? "assets=" + result.AssetCount + "; indexedItems=" + result.IndexedItemCount + "; skippedOfficialInputs=" + result.SkippedOfficialInputCount
                                : "retained=" + result.RetainedPreviousGeneration + "; failure=" + result.Failure)
                    },
                    result.Success
                        ? () =>
                        {
                            Content.PreCommitFaultForTest?.Invoke("before-visible-snapshot-swap");
                            result.PublishPreparedSnapshot?.Invoke();
                        }
                        : null);
                if (!generationCompleted)
                    throw new InvalidOperationException("ContentQuery generation completion was rejected as stale.");

                Content.PostCommitFaultForTest?.Invoke("terminal-receipt-and-visible-snapshot-committed");
                if (result.Success)
                {
                    if (result.SkippedOfficialInputCount > 0)
                    {
                        string inputDetails = "ContentQuery published valid enabled sources while skipping " +
                            result.SkippedOfficialInputCount.ToString(CultureInfo.InvariantCulture) +
                            " unreadable official input file(s); samples=" +
                            (result.OfficialInputDiagnostics.Count == 0 ? "none" : string.Join(" | ", result.OfficialInputDiagnostics)) + ".";
                        Diagnostics.RecordWarning(
                            "DTMAPI.ContentQuery",
                            "Enabled official content contained unreadable files; other valid content remains available.",
                            inputDetails);
                        RuntimeMonitor.Log(inputDetails, LogLevel.Warn);
                    }
                    ObserveResourceRefresh("ContentQuery", batch.ReasonSummary, ResourceLifecycleStatus.Rebuilt, result.AssetCount + result.IndexedItemCount);
                    ObserveLifecycleResourceEvent(
                        "ContentQuery",
                        "DomainGenerationCommitted",
                        "all",
                        result.CurrentGeneration.ToString(CultureInfo.InvariantCulture),
                        "dirtyGeneration=" + batch.Generation + "; reasons=" + batch.ReasonSummary + "; assets=" + result.AssetCount + "; indexedItems=" + result.IndexedItemCount);
                    return;
                }

                string details = "ContentQuery candidate rejected; last-good retained=" + result.RetainedPreviousGeneration +
                    "; generation=" + result.CurrentGeneration.ToString(CultureInfo.InvariantCulture) +
                    "; dirtyGeneration=" + batch.Generation.ToString(CultureInfo.InvariantCulture) +
                    "; reasons=" + batch.ReasonSummary +
                    "; failure=" + result.Failure + ".";
                Diagnostics.RecordWarning("DTMAPI.ContentQuery", "Content query generation was rejected; last-good publication retained.", details);
                RuntimeMonitor.Log(details, LogLevel.Warn);
                ObserveResourceRefresh("ContentQuery", batch.ReasonSummary, "rejected-last-good", result.AssetCount + result.IndexedItemCount);
                ObserveLifecycleResourceEvent(
                    "ContentQuery",
                    "DomainGenerationRejected",
                    "all",
                    result.CurrentGeneration.ToString(CultureInfo.InvariantCulture),
                    details);
            }
            catch (Exception ex)
            {
                if (!generationCompleted)
                {
                    contentRefreshGenerations.AbandonAndRequeue(
                        batch,
                        "ContentQuery consumer failed: " + ex.GetType().Name + ": " + ex.Message);
                }
                throw;
            }
        }

        private void ReconcileActiveOwners()
        {
            var deactivate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DiscoveredMod loaded in loadedMods)
            {
                if (!discoveredById.TryGetValue(loaded.Manifest.UniqueID, out DiscoveredMod current) || !current.OfficialEnabled)
                {
                    deactivate.Add(loaded.Manifest.UniqueID);
                    continue;
                }

                bool sourceIdentityChanged = !HasSameSourceIdentity(loaded, current);
                bool manifestVersionChanged = !string.Equals(
                    loaded.Manifest.Version,
                    current.Manifest.Version,
                    StringComparison.OrdinalIgnoreCase);
                if (sourceIdentityChanged || manifestVersionChanged)
                {
                    deactivate.Add(loaded.Manifest.UniqueID);
                    RuntimeMonitor.Log(
                        "Mod 来源或 manifest 版本已变化，旧 owner 进入 update-pending/source-handoff 停用；若已加载 Mono 程序集则要求重启。 owner=" + loaded.Manifest.UniqueID +
                        "; change=" + (sourceIdentityChanged
                            ? (manifestVersionChanged ? "source-and-version" : "source-identity")
                            : "manifest-version") +
                        "; loadedSource=" + loaded.Source +
                        "; loadedOfficialId=" + loaded.OfficialId +
                        "; loadedRoot=" + loaded.RootPath +
                        "; loadedVersion=" + loaded.Manifest.Version +
                        "; currentSource=" + current.Source +
                        "; currentOfficialId=" + current.OfficialId +
                        "; currentRoot=" + current.RootPath +
                        "; currentVersion=" + current.Manifest.Version + "。",
                        LogLevel.Warn);
                }
            }

            bool changed;
            do
            {
                changed = false;
                foreach (DiscoveredMod loaded in loadedMods)
                {
                    if (deactivate.Contains(loaded.Manifest.UniqueID))
                        continue;
                    foreach (IManifestDependency dependency in ((IManifest)loaded.Manifest).Dependencies)
                    {
                        bool available = TryGetAvailableDependencyVersion(dependency.UniqueID, deactivate, out string providerVersion) &&
                            IsVersionRequirementSatisfied(dependency.MinimumVersion, providerVersion, out _);
                        if (available)
                            continue;
                        if (!dependency.Required)
                        {
                            string optionalWarningKey = "optional-dependency-changed-" + loaded.Manifest.UniqueID + "-" + dependency.UniqueID;
                            RuntimeMonitor.LogOnce(
                                optionalWarningKey,
                                "可选依赖已变化：owner=" + loaded.Manifest.UniqueID + "; dependency=" + dependency.UniqueID + "。当前 Mod 保持活动。",
                                LogLevel.Warn);
                            RecordRefactorScaffoldWarningOnce(
                                optionalWarningKey,
                                "可选依赖已变化，Mod 保持活动。",
                                "owner=" + loaded.Manifest.UniqueID + "; dependency=" + dependency.UniqueID + "; restart may be required for provider services.");
                            continue;
                        }

                        deactivate.Add(loaded.Manifest.UniqueID);
                        changed = true;
                        RuntimeMonitor.Log(
                            "必需依赖已失效，停用 owner=" + loaded.Manifest.UniqueID + "; dependency=" + dependency.UniqueID + "。",
                            LogLevel.Warn);
                        break;
                    }
                }
            }
            while (changed);

            foreach (DiscoveredMod loaded in loadedMods.AsEnumerable().Reverse().ToArray())
            {
                if (deactivate.Contains(loaded.Manifest.UniqueID))
                    DeactivateOwner(loaded.Manifest.UniqueID, ModOwnerCleanupReason.Unload, shutdown: false, transactionId: string.Empty);
            }
        }

        private void RefreshConfigPageLocks()
        {
            if (configMenuRuntime == null)
                return;

            foreach (IConfigMenuPage page in configMenuRuntime.GetPages())
            {
                if (!discoveredById.TryGetValue(page.Manifest.UniqueID, out DiscoveredMod discovered))
                {
                    configMenuRuntime.SetPageLock(page.Manifest.UniqueID, true, "DTMAPI 当前不再发现此 Mod。");
                    continue;
                }

                bool loaded = IsLoadedMod(page.Manifest.UniqueID);
                if (!discovered.OfficialEnabled && loaded)
                    configMenuRuntime.SetPageLock(
                        page.Manifest.UniqueID,
                        true,
                        "此 Mod 已经加载；官方禁用会在重启游戏后完全停用。本次运行锁定配置编辑。");
                else if (!discovered.OfficialEnabled)
                    configMenuRuntime.SetPageLock(
                        page.Manifest.UniqueID,
                        true,
                        string.IsNullOrWhiteSpace(discovered.EnablementReason) ? "此 Mod 已在 Doloc Town 官方 Mod 界面或 Steam 创意工坊路径中禁用。" : discovered.EnablementReason);
                else if (!loaded)
                    configMenuRuntime.SetPageLock(page.Manifest.UniqueID, true, "此 Mod 尚未加载；请重启游戏或先检查依赖错误，再编辑配置。");
                else
                    configMenuRuntime.SetPageLock(page.Manifest.UniqueID, false, string.Empty);
            }
        }

        private bool LoadCodeMod(DiscoveredMod mod)
        {
            if (!TryResolveEntryDllPath(mod, out string dllPath))
                return false;

            string transactionId = string.Empty;
            IMonitor? monitor = null;
            try
            {
                modOwnerLifecycle.BeginEntry(mod.Manifest.UniqueID);
                transactionId = BeginModLoadTransaction(mod.Manifest.UniqueID);
                PublishModLoadTransactionDiagnosticBestEffort(mod.Manifest.UniqueID, transactionId, "BeginTransaction", modTransactionBeginDiagnosticForTests);
                RuntimeMonitor.Log(
                    "Code mod load-source owner=" + mod.Manifest.UniqueID +
                    "; identity=" + mod.Classification.IdentityName +
                    "; provenance=" + mod.Classification.DeclarationProvenance +
                    "; nativeRisk=" + mod.Classification.NativeRisk +
                    "; gameCompatibility=" + mod.Classification.GameCompatibility +
                    "; expectedHarmonyOwner=" + (mod.Classification.ExpectedHarmonyOwner.Length == 0 ? "none" : mod.Classification.ExpectedHarmonyOwner) +
                    "; source=" + mod.Source +
                    "; workshopId=" + (mod.WorkshopId.HasValue ? mod.WorkshopId.Value.ToString(CultureInfo.InvariantCulture) : "none") +
                    "; root=" + Path.GetFullPath(mod.RootPath) +
                    "; dll=" + dllPath + ".");
                if (mod.Classification.IsLegacyNativeCompatibility)
                {
                    ValidateLegacySourceIdentityBeforeLoad(mod, dllPath);
                    // Omitted-kind historical inputs may execute arbitrary native/Harmony
                    // initialization without the receipt-bound Advanced supervisor.
                    // They remain managed for discovery/Entry isolation only; unknown
                    // author-owned side effects make this a restart boundary before load.
                    MarkManagedAssemblyLoadAttempt(mod.Manifest.UniqueID);
                }
                else if (mod.Classification.IsAdvanced)
                {
                    ValidateAdvancedSourceIdentityBeforeLoad(mod, dllPath);
                    EnsureAdvancedHarmonyCleanupParticipantRegistered();
                    // Capture Harmony state before Assembly.LoadFrom: module initializers and
                    // other load-time side effects are part of the supervised entry window.
                    advancedHarmonySupervisor.BeginLoad(mod.Manifest.UniqueID, mod.Classification);
                    // Assembly.LoadFrom may run module initializers and then throw. From this
                    // point onward the process cannot prove that no assembly/static/native
                    // state escaped, even if canonical Harmony cleanup reaches zero.
                    MarkManagedAssemblyLoadAttempt(mod.Manifest.UniqueID);
                }
                Assembly assembly = Assembly.LoadFrom(dllPath);
                // A successfully loaded Mono assembly is process-lifetime even if any later
                // construction, Entry, publication, or commit checkpoint fails.
                MarkManagedAssemblyLoadAttempt(mod.Manifest.UniqueID);
                if (mod.Classification.IsAdvanced)
                {
                    ValidateAdvancedLoadedAssemblyIdentity(mod, dllPath, assembly);
                    advancedHarmonySupervisor.BindEntryAssembly(mod.Manifest.UniqueID, assembly);
                }
                else if (mod.Classification.IsLegacyNativeCompatibility)
                {
                    ValidateLegacyLoadedAssemblyIdentity(mod.Classification, dllPath, assembly);
                }
                RunModLoadCheckpoint(ModLoadCheckpoint.AssemblyLoaded);
                if (!TryResolveEntryType(mod, assembly, out Type? entryType))
                    throw new InvalidOperationException("No valid DtmMod entry type could be resolved for " + mod.Manifest.UniqueID + ".");

                var modInstance = (DtmMod)Activator.CreateInstance(entryType);
                modLifecycleInstances.Add(mod.Manifest.UniqueID, modInstance);
                RunModLoadCheckpoint(ModLoadCheckpoint.InstanceCreated);
                monitor = new FileMonitor(host, mod.Manifest.UniqueID, Diagnostics.LatestLogPath, Diagnostics.RecordError);
                modInstance.AttachContext(mod.Manifest, monitor);
                RunModLoadCheckpoint(ModLoadCheckpoint.ContextAttached);
                Action ensureRuntimeThread = () => EnsureModHelperRuntimeThread(mod.Manifest.UniqueID);
                Action ensureOwnerActive = () => EnsureModOwnerRegistrationAllowed(mod.Manifest.UniqueID);
                var helper = new DtmHelper(
                    mod.Manifest,
                    monitor,
                    Events.CreateOwnerBoundProxy(mod.Manifest.UniqueID, ensureOwnerActive),
                    Config.CreateOwnerBound(mod.Manifest, ensureOwnerActive),
                    ModRegistry.CreateOwnerBoundRegistry(mod.Manifest, ensureOwnerActive),
                    Workshop,
                    UI.CreateOwnerBound(mod.Manifest.UniqueID, ensureRuntimeThread, ensureOwnerActive),
                    Diagnostics.CreateOwnerBound(mod.Manifest.UniqueID, ensureRuntimeThread, ensureOwnerActive),
                    Content,
                    Input.CreateOwnerBound(mod.Manifest.UniqueID, ensureOwnerActive),
                    new TranslationService(mod.RootPath, TranslationService.DetectLanguage()));
                modInstance.Entry(helper);
                if (mod.Classification.IsAdvanced)
                    advancedHarmonySupervisor.CompleteEntry(mod.Manifest.UniqueID);
                RunModLoadCheckpoint(ModLoadCheckpoint.EntryReturned);

                ModRegistry.AddLoaded(mod.Manifest);
                RunModLoadCheckpoint(ModLoadCheckpoint.LoadedRegistryPublished);
                modInstances.Add(mod.Manifest.UniqueID, modInstance);
                RunModLoadCheckpoint(ModLoadCheckpoint.InstancePublished);
                loadedMods.Add(mod);
                PublishLoadedModsSnapshot();
                RunModLoadCheckpoint(ModLoadCheckpoint.LoadedListPublished);
                RecordModOwnerRegistration(mod.Manifest.UniqueID, "LoadedCodeMod", mod.Manifest.UniqueID, "Code mod loaded after Entry completed.");
                RunModLoadCheckpoint(ModLoadCheckpoint.BeforeCommit);
                modOwnerLifecycle.Activate(mod.Manifest.UniqueID);
                CommitModLoadTransaction(mod.Manifest.UniqueID, transactionId);
                if (mod.Classification.IsLegacyNativeCompatibility)
                {
                    RuntimeMonitor.Log(
                        "Third-party native compatibility Mod loaded owner=" + mod.Manifest.UniqueID +
                        "; native Hooks, static state, save side effects and cleanup remain author-managed; " +
                        "disable, unsubscribe and update take effect after restarting Doloc Town. " +
                        "DTMAPI does not claim that unknown third-party Hooks were unloaded.");
                }
            }
            catch (Exception ex)
            {
                string cleanupSummary = DeactivateOwner(mod.Manifest.UniqueID, ModOwnerCleanupReason.EntryFailed, shutdown: false, transactionId);
                bool assemblyLoaded = loadedAssemblyOwnerIds.Contains(mod.Manifest.UniqueID);
                bool restartRequired = modOwnerLifecycle.RequiresRestart(mod.Manifest.UniqueID);
                try
                {
                    Diagnostics.RecordError(
                        mod.Manifest.UniqueID,
                        "Failed to load code mod.",
                        ex + Environment.NewLine +
                        cleanupSummary + Environment.NewLine +
                        "AssemblyLoaded=" + assemblyLoaded.ToString(CultureInfo.InvariantCulture) +
                        "; RestartRequired=" + restartRequired.ToString(CultureInfo.InvariantCulture) +
                        "; unknown Harmony/static/native/Unity side effects are possible only after managed code was loaded or entered.");
                }
                catch
                {
                    // Error reporting must not undo or interrupt authoritative cleanup.
                }
                RuntimeMonitor.LogException(ex, $"Failed to load {mod.Manifest.UniqueID}.");
                RuntimeMonitor.Log("Failed code mod cleanup owner=" + mod.Manifest.UniqueID + " " + cleanupSummary + ".", LogLevel.Warn);
                return false;
            }

            try
            {
                if (monitor != null)
                {
                    if (modCompletionLogForTests != null)
                        modCompletionLogForTests(monitor);
                    else
                        monitor.Log("Mod Entry completed.");
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Diagnostics.RecordWarning(mod.Manifest.UniqueID, "Mod completion log failed after successful Entry.", ex.GetType().Name + ": " + ex.Message);
                }
                catch
                {
                }
            }
            return true;
        }

        private void EnsureAdvancedHarmonyCleanupParticipantRegistered()
        {
            if (advancedHarmonyParticipantRegistered)
                return;
            RegisterModOwnerCleanupParticipant(advancedHarmonySupervisor);
            advancedHarmonyParticipantRegistered = true;
        }

        private static void ValidateAdvancedSourceIdentityBeforeLoad(DiscoveredMod mod, string entryPath)
        {
            ManagedModClassification classification = mod.Classification;
            string currentFingerprint = ManagedModClassifier.ComputeSourceFingerprint(mod.RootPath);
            if (classification.SourceFingerprint.Length == 0 ||
                !classification.SourceFingerprint.Equals(currentFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "advanced-source-fingerprint-mismatch: Advanced package bytes changed after classification and before load.");
            }
            VerifyAdvancedEntryFileIdentity(classification, entryPath, "before-load");
        }

        private static void ValidateLegacySourceIdentityBeforeLoad(
            DiscoveredMod mod,
            string entryPath)
        {
            ManagedModClassification classification = mod.Classification;
            string currentFingerprint = ManagedModClassifier.ComputeLegacySourceFingerprint(
                mod.RootPath,
                mod.ManifestPath);
            if (classification.SourceFingerprint.Length == 0 ||
                !classification.SourceFingerprint.Equals(currentFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "legacy-native-source-fingerprint-mismatch: Legacy code closure changed after classification and before cold load.");
            }

            ValidateLegacyEntryIdentity(classification, entryPath, "before-load");
            DetectLegacyLoadedAssemblyIdentityCollision(classification, entryPath);
        }

        private static void ValidateLegacyEntryIdentity(
            ManagedModClassification classification,
            string entryPath,
            string phase)
        {
            string sha256;
            using (FileStream stream = new FileStream(entryPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (SHA256 hash = SHA256.Create())
                sha256 = BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
            PortableAssemblyMetadata metadata = PortableAssemblyReferenceInspector.Inspect(entryPath);
            if (classification.EntryDllSha256.Length == 0 ||
                classification.EntryModuleMvid.Length == 0 ||
                !classification.EntryDllSha256.Equals(sha256, StringComparison.OrdinalIgnoreCase) ||
                !classification.EntryModuleMvid.Equals(metadata.ModuleMvid, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    LegacyEntryIdentityFailureCode(classification) + ": Legacy native compatibility entry changed " + phase +
                    "; expectedSha256=" + classification.EntryDllSha256 + "; actualSha256=" + sha256 +
                    "; expectedMvid=" + classification.EntryModuleMvid + "; actualMvid=" + metadata.ModuleMvid + ".");
            }
        }

        private static void DetectLegacyLoadedAssemblyIdentityCollision(
            ManagedModClassification classification,
            string entryPath)
        {
            AssemblyName expectedName = AssemblyName.GetAssemblyName(entryPath);
            string expectedLocation = Path.GetFullPath(entryPath);
            foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
            {
                AssemblyName loadedName;
                string loadedLocation;
                try
                {
                    loadedName = loaded.GetName();
                    loadedLocation = loaded.IsDynamic || string.IsNullOrWhiteSpace(loaded.Location)
                        ? string.Empty
                        : Path.GetFullPath(loaded.Location);
                }
                catch
                {
                    continue;
                }
                if (!string.Equals(loadedName.FullName, expectedName.FullName, StringComparison.OrdinalIgnoreCase))
                    continue;

                string loadedMvid;
                try
                {
                    loadedMvid = loaded.ManifestModule.ModuleVersionId.ToString("D");
                }
                catch
                {
                    loadedMvid = string.Empty;
                }
                if (!string.Equals(
                        loadedLocation.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                        expectedLocation.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                        StringComparison.OrdinalIgnoreCase) ||
                    !loadedMvid.Equals(classification.EntryModuleMvid, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        "legacy-native-entry-assembly-identity-collision: Another assembly with the same CLR identity is already resident; expectedLocation=" +
                        expectedLocation + "; residentLocation=" + loadedLocation + "; expectedMvid=" +
                        classification.EntryModuleMvid + "; residentMvid=" + loadedMvid + ".");
                }
            }
        }

        private static void ValidateLegacyLoadedAssemblyIdentity(
            ManagedModClassification classification,
            string entryPath,
            Assembly assembly)
        {
            ValidateLegacyEntryIdentity(classification, entryPath, "after-load");
            string loadedLocation = Path.GetFullPath(assembly.Location ?? string.Empty);
            string loadedMvid = assembly.ManifestModule.ModuleVersionId.ToString("D", CultureInfo.InvariantCulture);
            string expectedLocation = Path.GetFullPath(entryPath);
            if (!string.Equals(
                    loadedLocation.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    expectedLocation.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase) ||
                !loadedMvid.Equals(classification.EntryModuleMvid, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    LegacyEntryIdentityFailureCode(classification) + ": Assembly.LoadFrom did not return the classified legacy entry; expectedLocation=" +
                    expectedLocation + "; actualLocation=" + loadedLocation + "; expectedMvid=" +
                    classification.EntryModuleMvid + "; actualMvid=" + loadedMvid + ".");
            }
        }

        private static string LegacyEntryIdentityFailureCode(ManagedModClassification classification) =>
            classification.IsLegacyExternalCompatibility
                ? "legacy-external-entry-identity-mismatch"
                : "legacy-native-entry-identity-mismatch";

        private static void ValidateAdvancedLoadedAssemblyIdentity(DiscoveredMod mod, string entryPath, Assembly assembly)
        {
            VerifyAdvancedEntryFileIdentity(mod.Classification, entryPath, "after-load");
            string loadedLocation;
            string loadedMvid;
            try
            {
                loadedLocation = Path.GetFullPath(assembly.Location ?? string.Empty);
                loadedMvid = assembly.ManifestModule.ModuleVersionId.ToString("D", CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(
                    "advanced-entry-identity-mismatch: Loaded Advanced assembly identity could not be read before type discovery: " +
                    ex.GetType().Name + ": " + ex.Message,
                    ex);
            }
            string expectedLocation = Path.GetFullPath(entryPath);
            if (!string.Equals(
                    loadedLocation.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    expectedLocation.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase) ||
                !loadedMvid.Equals(mod.Classification.EntryModuleMvid, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "advanced-entry-identity-mismatch: Assembly.LoadFrom did not return the receipt-bound Advanced entry; expectedLocation=" +
                    expectedLocation + "; actualLocation=" + loadedLocation + "; expectedMvid=" + mod.Classification.EntryModuleMvid +
                    "; actualMvid=" + loadedMvid + ".");
            }
        }

        private static void VerifyAdvancedEntryFileIdentity(ManagedModClassification classification, string entryPath, string phase)
        {
            string sha256;
            using (FileStream stream = new FileStream(entryPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (SHA256 hash = SHA256.Create())
                sha256 = BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
            PortableAssemblyMetadata metadata = PortableAssemblyReferenceInspector.Inspect(entryPath);
            if (classification.EntryDllSha256.Length == 0 || classification.EntryModuleMvid.Length == 0 ||
                !classification.EntryDllSha256.Equals(sha256, StringComparison.OrdinalIgnoreCase) ||
                !classification.EntryModuleMvid.Equals(metadata.ModuleMvid, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "advanced-entry-identity-mismatch: Receipt-bound Advanced entry changed " + phase +
                    "; expectedSha256=" + classification.EntryDllSha256 + "; actualSha256=" + sha256 +
                    "; expectedMvid=" + classification.EntryModuleMvid + "; actualMvid=" + metadata.ModuleMvid + ".");
            }
        }

        private void RunModLoadCheckpoint(ModLoadCheckpoint checkpoint) => modLoadCheckpointForTests?.Invoke(checkpoint);

        internal void MarkManagedAssemblyLoadAttempt(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("Owner id is required.", nameof(ownerId));
            loadedAssemblyOwnerIds.Add(ownerId);
        }

        internal void EnsureModOwnerRegistrationAllowed(string ownerId)
        {
            EnsureModHelperRuntimeThread(ownerId);
            modOwnerLifecycle.EnsureRegistrationAllowed(ownerId);
        }

        internal void EnsureModHelperRuntimeThread(string ownerId)
        {
            if (Thread.CurrentThread.ManagedThreadId != runtimeThreadId)
            {
                throw new InvalidOperationException(
                    "Mod owner '" + ownerId + "' can only register or mutate owner-bound platform resources on the DTMAPI runtime thread.");
            }
        }

        internal string DeactivateOwner(string uniqueId, ModOwnerCleanupReason reason, bool shutdown, string transactionId)
        {
            modOwnerLifecycle.TryGetState(uniqueId, out ModOwnerLifecycleState previousState);
            if (!modOwnerLifecycle.BeginDeactivation(uniqueId))
                return "OwnerBoundCleanup: owner=" + uniqueId + ", cleanupInProgressOrUnknown=true";

            bool effectiveShutdown = shutdown || previousState == ModOwnerLifecycleState.Shutdown;
            bool firstDeactivation = previousState == ModOwnerLifecycleState.Entering || previousState == ModOwnerLifecycleState.Active;
            bool restartRequired = !effectiveShutdown;
            if (!TryPrepareModInstanceDeactivation(
                    uniqueId,
                    reason,
                    out string preparationFailure))
            {
                modOwnerLifecycle.CancelDeactivation(
                    uniqueId,
                    previousState);
                int retainedRoots =
                    CountCoreOwnerRoots(uniqueId);
                string deferred =
                    "OwnerBoundCleanup: owner=" +
                    uniqueId +
                    ", deactivationDeferred=true" +
                    ", preparationFailure=" +
                    preparationFailure +
                    ", remaining=" +
                    retainedRoots +
                    ", restartRequired=False";
                try
                {
                    Diagnostics.RecordError(
                        uniqueId,
                        "Owner deactivation preparation failed; the active owner and all roots were retained for an atomic retry.",
                        deferred);
                }
                catch
                {
                }
                try
                {
                    PublishModOwnerLifecycleStatus(
                        "DeactivateOwnerDeferred");
                }
                catch
                {
                }
                return deferred;
            }
            try
            {
                int lifecycleInstancesRemoved = TryDeactivateModInstance(uniqueId, reason, out bool modDisposeFailed);
                int eventsRemoved = TryDeactivateStep(uniqueId, "Event", () => Events.RemoveOwner(uniqueId), out bool eventsFailed);
                int inputRemoved = TryDeactivateStep(uniqueId, "Input", () => Input.RemoveOwner(uniqueId), out bool inputFailed);
                int configPagesRemoved = TryDeactivateStep(uniqueId, "ConfigPage", () => configMenuRuntime?.RemoveOwner(uniqueId) ?? 0, out bool configPagesFailed);
                int migrationsRemoved = TryDeactivateStep(uniqueId, "ConfigMigration", () => Config.RemoveOwner(uniqueId), out bool migrationsFailed);
                int contentRemoved = TryDeactivateStep(uniqueId, "Content", () => Content.RemoveOwner(uniqueId), out bool contentFailed);
                // Remove API facade/cache roots before feature cleanup so facade-owned
                // callbacks and sessions can't observe teardown of their owner state.
                int registryRemoved = TryDeactivateStep(uniqueId, "Registry", () => ModRegistry.RemoveOwner(uniqueId), out bool registryFailed);
                ModOwnerParticipantCleanupSummary participants = CleanupModOwnerParticipants(uniqueId, reason);
                int customEntitiesRemoved = TryDeactivateStep(uniqueId, "CustomEntity", () => CustomEntities.RemoveOwner(uniqueId, "unified owner deactivation"), out bool customEntitiesFailed);
                int demandRemoved = TryDeactivateStep(
                    uniqueId,
                    "RuntimeDemand",
                    () => demandCoordinator.RemoveOwner(uniqueId, "unified owner deactivation reason=" + reason),
                    out bool demandFailed);
                int instancesRemoved = modInstances.Remove(uniqueId) ? 1 : 0;
                int loadedRemoved = loadedMods.RemoveAll(mod => mod.Manifest.UniqueID.Equals(uniqueId, StringComparison.OrdinalIgnoreCase));
                if (loadedRemoved > 0)
                    PublishLoadedModsSnapshot();
                if (!effectiveShutdown && firstDeactivation)
                {
                    contentRefreshGenerations.MarkDirty(
                        ContentRefreshDomains.SourceDriven,
                        new[] { uniqueId },
                        "owner deactivated reason=" + reason);
                }
                int coreCleanupFailures = new[] { modDisposeFailed, eventsFailed, inputFailed, configPagesFailed, migrationsFailed, contentFailed, customEntitiesFailed, registryFailed, demandFailed }.Count(failed => failed);

                int remaining = CountCoreOwnerRoots(uniqueId);
                bool cleanupProvedZero = remaining == 0 &&
                    coreCleanupFailures == 0 &&
                    participants.RemainingResources == 0 &&
                    participants.FailureCount == 0;
                bool assemblyLoaded = loadedAssemblyOwnerIds.Contains(uniqueId);
                restartRequired = !effectiveShutdown && (assemblyLoaded || !cleanupProvedZero);
                string summary = "OwnerBoundCleanup: owner=" + uniqueId +
                    ", eventsRemoved=" + eventsRemoved +
                    ", eventHandlersRemoved=" + eventsRemoved +
                    ", inputRemoved=" + inputRemoved +
                    ", inputButtonsRemoved=" + inputRemoved +
                    ", configPagesRemoved=" + configPagesRemoved +
                    ", migrationsRemoved=" + migrationsRemoved +
                    ", contentRemoved=" + contentRemoved +
                    ", customEntitiesRemoved=" + customEntitiesRemoved +
                    ", demandRemoved=" + demandRemoved +
                    ", registryRemoved=" + registryRemoved +
                    ", registryEntriesRemoved=" + registryRemoved +
                    ", lifecycleInstancesRemoved=" + lifecycleInstancesRemoved +
                    ", modDisposeFailures=" + (modDisposeFailed ? 1 : 0) +
                    ", instancesRemoved=" + instancesRemoved +
                    ", loadedRemoved=" + loadedRemoved +
                    ", coreCleanupFailures=" + coreCleanupFailures +
                    ", participantResourcesRemoved=" + participants.RemovedResources +
                    ", participantCleanupFailures=" + participants.FailureCount +
                    ", remaining=" + (remaining + participants.RemainingResources) +
                    ", assemblyLoaded=" + assemblyLoaded.ToString(CultureInfo.InvariantCulture) +
                    ", restartRequired=" + restartRequired.ToString(CultureInfo.InvariantCulture) +
                    ", thirdPartyHarmonyCleanupSupported=False";
                if (firstDeactivation && restartRequired)
                {
                    try
                    {
                        RecordModOwnerNeedsRestart(
                            uniqueId,
                            assemblyLoaded ? "LoadedAssembly" : "IncompleteOwnerCleanup",
                            assemblyLoaded
                                ? "Loaded assemblies and unknown Harmony/static/native/Unity side effects cannot be unloaded safely in-process."
                                : "Owner cleanup did not prove zero platform roots; same-process re-entry remains blocked.");
                    }
                    catch (Exception ex)
                    {
                        TryLogOwnerCleanupDiagnostic("Restart-required diagnostic failed owner=" + uniqueId + ": " + ex.GetType().Name + ": " + ex.Message);
                    }
                }
                try
                {
                    RollbackModLoadTransaction(uniqueId, transactionId, summary);
                }
                catch (Exception ex)
                {
                    TryLogOwnerCleanupDiagnostic("Owner transaction rollback reporting failed owner=" + uniqueId + ": " + ex.GetType().Name + ": " + ex.Message);
                }
                if (remaining > 0 || coreCleanupFailures > 0 || participants.RemainingResources > 0 || participants.FailureCount > 0)
                {
                    try
                    {
                        Diagnostics.RecordError(uniqueId, "Owner deactivation did not prove zero platform roots.", summary);
                    }
                    catch
                    {
                        // Cleanup completion and retryability must not depend on diagnostics.
                    }
                }
                return summary;
            }
            catch (Exception ex)
            {
                restartRequired = !effectiveShutdown;
                string summary = "OwnerBoundCleanup: owner=" + uniqueId + ", unexpectedCleanupFailure=" + ex.GetType().Name + ": " + ex.Message + ", restartRequired=" + restartRequired.ToString(CultureInfo.InvariantCulture);
                try
                {
                    RollbackModLoadTransaction(uniqueId, transactionId, summary);
                }
                catch
                {
                }
                try
                {
                    Diagnostics.RecordError(uniqueId, "Owner deactivation failed unexpectedly; a later cleanup pass may retry.", summary);
                }
                catch
                {
                }
                return summary;
            }
            finally
            {
                modOwnerLifecycle.CompleteDeactivation(uniqueId, effectiveShutdown, restartRequired);
                try
                {
                    PublishModOwnerLifecycleStatus("DeactivateOwner");
                }
                catch (Exception ex)
                {
                    TryLogOwnerCleanupDiagnostic("Owner lifecycle status publication failed owner=" + uniqueId + ": " + ex.GetType().Name + ": " + ex.Message);
                }
            }
        }

        private string CleanupFailedCodeModOwner(string uniqueId, string transactionId = "")
        {
            if (!modOwnerLifecycle.TryGetState(uniqueId, out _))
                modOwnerLifecycle.BeginEntry(uniqueId);
            return DeactivateOwner(uniqueId, ModOwnerCleanupReason.EntryFailed, shutdown: false, transactionId);
        }

        private int TryDeactivateStep(string ownerId, string kind, Func<int> cleanup, out bool failed)
        {
            failed = false;
            int removed;
            try
            {
                removed = cleanup();
            }
            catch (Exception ex)
            {
                failed = true;
                RecordModOwnerCleanupFailure(ownerId, kind, ex.GetType().Name + ": " + ex.Message);
                TryLogOwnerCleanupDiagnostic("Owner cleanup step failed owner=" + ownerId + " kind=" + kind + ": " + ex.GetType().Name + ": " + ex.Message);
                return 0;
            }

            if (removed > 0)
            {
                try
                {
                    RecordModOwnerCleanup(ownerId, kind, removed, "Unified owner deactivation removed resources.");
                }
                catch (Exception ex)
                {
                    // The authoritative registry mutation already succeeded. Ledger and
                    // log publication are diagnostics only and must not convert a zero-root
                    // cleanup into a failure or make the removed count disappear.
                    TryLogOwnerCleanupDiagnostic("Owner cleanup diagnostic failed after successful removal owner=" + ownerId + " kind=" + kind + ": " + ex.GetType().Name + ": " + ex.Message);
                }
            }
            return removed;
        }

        private int TryDeactivateModInstance(
            string ownerId,
            ModOwnerCleanupReason reason,
            out bool failed)
        {
            failed = false;
            if (!modLifecycleInstances.TryGetValue(ownerId, out DtmMod instance))
                return 0;

            MethodInfo? reasonAwareDeactivation =
                instance.GetType().GetMethod(
                    "DtmApiDeactivateOwner",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                    binder: null,
                    types: new[] { typeof(string) },
                    modifiers: null);
            if (reasonAwareDeactivation != null &&
                reasonAwareDeactivation.ReturnType != typeof(void))
            {
                reasonAwareDeactivation = null;
            }
            if (reasonAwareDeactivation != null ||
                instance is IDisposable)
            {
                try
                {
                    if (reasonAwareDeactivation != null)
                    {
                        reasonAwareDeactivation.Invoke(
                            instance,
                            new object[]
                            {
                                reason.ToString()
                            });
                    }
                    else
                    {
                        ((IDisposable)instance).Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Exception cleanupFailure =
                        ex is TargetInvocationException invocation &&
                        invocation.InnerException != null
                            ? invocation.InnerException
                            : ex;
                    failed = true;
                    string details =
                        cleanupFailure.GetType().Name +
                        ": " +
                        cleanupFailure.Message;
                    RecordModOwnerCleanupFailure(ownerId, "ModInstanceDispose", details);
                    TryLogOwnerCleanupDiagnostic("Mod instance Dispose failed owner=" + ownerId + ": " + details);
                    // Retain the instance as an authoritative remaining root so a later
                    // deactivation pass can retry idempotent product cleanup.
                    return 0;
                }
            }

            modLifecycleInstances.Remove(ownerId);
            try
            {
                RecordModOwnerCleanup(
                    ownerId,
                    "ModInstanceLifecycle",
                    1,
                    reasonAwareDeactivation != null
                        ? "Invoked the reason-aware managed Mod deactivation boundary before removing owner-bound platform roots."
                        : instance is IDisposable
                            ? "Disposed the constructed mod instance before removing owner-bound platform roots."
                            : "Released the constructed mod instance; no optional IDisposable cleanup was declared.");
            }
            catch (Exception ex)
            {
                TryLogOwnerCleanupDiagnostic("Mod instance cleanup diagnostic failed after successful release owner=" + ownerId + ": " + ex.GetType().Name + ": " + ex.Message);
            }
            return 1;
        }

        private bool TryPrepareModInstanceDeactivation(
            string ownerId,
            ModOwnerCleanupReason reason,
            out string failure)
        {
            failure = string.Empty;
            if (!modLifecycleInstances.TryGetValue(
                    ownerId,
                    out DtmMod instance))
            {
                return true;
            }

            MethodInfo? prepare =
                instance.GetType().GetMethod(
                    "DtmApiPrepareOwnerDeactivation",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                    binder: null,
                    types: new[] { typeof(string) },
                    modifiers: null);
            if (prepare == null)
                return true;
            if (prepare.ReturnType != typeof(void))
            {
                failure =
                    "InvalidOperationException: reason-aware preparation must return void.";
                return false;
            }

            try
            {
                prepare.Invoke(
                    instance,
                    new object[]
                    {
                        reason.ToString()
                    });
                return true;
            }
            catch (Exception ex)
            {
                Exception preparationFailure =
                    ex is TargetInvocationException invocation &&
                    invocation.InnerException != null
                        ? invocation.InnerException
                        : ex;
                failure =
                    preparationFailure.GetType().Name +
                    ": " +
                    preparationFailure.Message;
                RecordModOwnerCleanupFailure(
                    ownerId,
                    "ModInstancePrepareDeactivation",
                    failure);
                TryLogOwnerCleanupDiagnostic(
                    "Mod instance deactivation preparation failed owner=" +
                    ownerId +
                    ": " +
                    failure);
                return false;
            }
        }

        private void TryLogOwnerCleanupDiagnostic(string message)
        {
            try
            {
                RuntimeMonitor.Log(message, LogLevel.Warn);
            }
            catch
            {
            }
        }

        internal int CountCoreOwnerRoots(string ownerId)
        {
            int inputRoots = Input.CountOwnerResources(ownerId);
            int eventRoots = Events.CountOwnerResources(ownerId);
            int configPages = configMenuRuntime?.GetPages().Count(page => page.Manifest.UniqueID.Equals(ownerId, StringComparison.OrdinalIgnoreCase)) ?? 0;
            int customEntities = CustomEntities.GetAnimalSnapshot(ownerId).RegisteredDefinitionCount + CustomEntities.GetAnimalSnapshot(ownerId).ActiveRuntimeInstanceCount +
                CustomEntities.GetMonsterSnapshot(ownerId).RegisteredDefinitionCount + CustomEntities.GetMonsterSnapshot(ownerId).ActiveRuntimeInstanceCount +
                CustomEntities.GetAttackSnapshot(ownerId).RegisteredDefinitionCount + CustomEntities.GetAttackSnapshot(ownerId).ActiveRuntimeInstanceCount +
                CustomEntities.GetDroneSnapshot(ownerId).RegisteredDefinitionCount + CustomEntities.GetDroneSnapshot(ownerId).ActiveRuntimeInstanceCount;
            int demandRoots = demandCoordinator.GetOwnerDemandCount(ownerId);
            return inputRoots + eventRoots + configPages + demandRoots + Config.CountOwner(ownerId) + ModRegistry.CountOwner(ownerId) +
                Content.CountOwnerResources(ownerId) + customEntities + (modLifecycleInstances.ContainsKey(ownerId) ? 1 : 0) +
                (modInstances.ContainsKey(ownerId) ? 1 : 0) + loadedMods.Count(mod => mod.Manifest.UniqueID.Equals(ownerId, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsLoadedMod(string uniqueId)
        {
            return loadedMods.Any(m => m.Manifest.UniqueID.Equals(uniqueId, StringComparison.OrdinalIgnoreCase));
        }

        private bool CanLoadDependencies(DiscoveredMod mod)
        {
            foreach (IManifestDependency dependency in ((IManifest)mod.Manifest).Dependencies)
            {
                if (string.IsNullOrWhiteSpace(dependency.UniqueID))
                {
                    Diagnostics.RecordError(mod.Manifest.UniqueID, "依赖声明缺少 UniqueID。", "Dependency UniqueID is empty.");
                    RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：依赖声明缺少 UniqueID。", LogLevel.Warn);
                    return false;
                }

                if (!TryGetAvailableDependencyVersion(dependency.UniqueID, deactivatingOwners: null, out string dependencyVersion))
                {
                    if (!dependency.Required)
                        continue;
                    Diagnostics.RecordError(mod.Manifest.UniqueID, "缺少必需依赖。", dependency.UniqueID);
                    RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：缺少依赖 " + dependency.UniqueID + "。", LogLevel.Warn);
                    return false;
                }

                if (IsVersionRequirementSatisfied(dependency.MinimumVersion, dependencyVersion, out string dependencyVersionReason))
                    continue;

                string message = dependency.Required ? "必需依赖版本过低。" : "可选依赖版本过低。";
                string details = dependency.UniqueID + " requires >= " + dependency.MinimumVersion + ", loaded " + dependencyVersion + ". " + dependencyVersionReason;
                if (dependency.Required)
                    Diagnostics.RecordError(mod.Manifest.UniqueID, message, details);
                else
                    Diagnostics.RecordWarning(mod.Manifest.UniqueID, message, details);
                RuntimeMonitor.Log(
                    (dependency.Required ? "跳过 " + mod.Manifest.UniqueID + "：" : "诊断 " + mod.Manifest.UniqueID + "：") +
                    dependency.UniqueID + " 版本不满足要求 " + dependency.MinimumVersion + "，当前 " + dependencyVersion + "。",
                    LogLevel.Warn);
                if (dependency.Required)
                    return false;
            }
            return true;
        }

        private bool TryGetAvailableDependencyVersion(
            string dependencyId,
            ISet<string>? deactivatingOwners,
            out string version)
        {
            version = string.Empty;
            IManifest? loadedProvider = ModRegistry.Get(dependencyId);
            if (loadedProvider == null)
                return false;

            // Runtime APIs (Core, ConfigMenu, GameBridge, and Bootstrap hosts) are
            // process-lifetime providers registered directly in the authoritative
            // registry. They intentionally have no discoverable ordinary-Mod source,
            // so a Workshop refresh must not require a discoveredById row for them.
            if (processLifetimeProviderIds.Contains(dependencyId))
            {
                version = loadedProvider.Version;
                return true;
            }

            // Ordinary providers remain governed by their refreshed source enablement
            // and owner state, but their available API version is the canonical version
            // actually loaded in this process. A refreshed disk manifest must not make
            // an older resident DLL/API appear to satisfy a newer dependency contract.
            if (!discoveredById.TryGetValue(dependencyId, out DiscoveredMod provider) ||
                !provider.OfficialEnabled ||
                deactivatingOwners?.Contains(dependencyId) == true)
            {
                return false;
            }

            version = loadedProvider.Version;
            return true;
        }

        private static bool HasSameSourceIdentity(DiscoveredMod loaded, DiscoveredMod current)
        {
            bool sameLocation = string.Equals(loaded.Source, current.Source, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(loaded.OfficialId, current.OfficialId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    EnsureTrailingDirectorySeparator(loaded.RootPath),
                    EnsureTrailingDirectorySeparator(current.RootPath),
                    StringComparison.OrdinalIgnoreCase);
            if (!sameLocation || loaded.Classification.Identity != current.Classification.Identity)
                return false;
            if (!loaded.Classification.IsAdvanced &&
                !current.Classification.IsAdvanced &&
                !loaded.Classification.IsLegacyNativeCompatibility &&
                !current.Classification.IsLegacyNativeCompatibility)
                return true;
            return loaded.Classification.SourceFingerprint.Length > 0 &&
                current.Classification.SourceFingerprint.Length > 0 &&
                loaded.Classification.SourceFingerprint.Equals(current.Classification.SourceFingerprint, StringComparison.OrdinalIgnoreCase);
        }

        private bool CanLoadApiVersion(DiscoveredMod mod)
        {
            string minimum = mod.Manifest.MinimumDTMApiVersion;
            if (string.IsNullOrWhiteSpace(minimum))
                return true;

            if (IsVersionRequirementSatisfied(minimum, ApiVersion, out string reason))
                return true;

            Diagnostics.RecordError(
                mod.Manifest.UniqueID,
                "DTMAPI 前置版本过旧。",
                "This mod requires a newer DTMAPI runtime. MinimumDTMApiVersion=" + minimum + ", installed runtime=" + ApiVersion + ". Run 1_install_dtmapi.bat to update DTMAPI. " + reason);
            RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：DTMAPI 前置版本过旧；该 Mod 需要 DTMAPI >= " + minimum + "，当前 " + ApiVersion + "。请运行 1_install_dtmapi.bat 更新 DTMAPI。", LogLevel.Warn);
            return false;
        }

        private bool CanLoadGameVersion(DiscoveredMod mod)
        {
            string minimum = mod.Manifest.MinimumGameVersion;
            if (string.IsNullOrWhiteSpace(minimum))
                return true;

            Diagnostics.RecordWarning(
                mod.Manifest.UniqueID,
                "MinimumGameVersion cannot be verified.",
                "MinimumGameVersion=" + minimum + "; the current runtime host cannot detect the Doloc Town game version, so DTMAPI continues loading and records this as a structured warning.");
            RuntimeMonitor.Log(
                "MinimumGameVersion warning for " + mod.Manifest.UniqueID + ": manifest requires Doloc Town >= " + minimum + ", but the current runtime host cannot detect the game version; DTMAPI will continue loading and treat this as an explicit warning instead of silently ignoring it.",
                LogLevel.Warn);
            return true;
        }

        private bool TryResolveEntryDllPath(DiscoveredMod mod, out string dllPath)
        {
            dllPath = string.Empty;
            string entryDll = (mod.Manifest.EntryDll ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(entryDll))
            {
                Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryDll 缺失。", mod.RootPath);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryDll 缺失。", LogLevel.Warn);
                return false;
            }

            if (Path.IsPathRooted(entryDll))
            {
                Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryDll 必须是相对路径。", entryDll);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryDll 必须是相对路径，不能使用 " + entryDll + "。", LogLevel.Warn);
                return false;
            }

            string root = Path.GetFullPath(mod.RootPath);
            string resolved = Path.GetFullPath(Path.Combine(root, entryDll));
            string rootWithSeparator = EnsureTrailingDirectorySeparator(root);
            if (!resolved.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryDll 不能逃出 Mod 根目录。", "EntryDll=" + entryDll + ", resolved=" + resolved + ", root=" + root);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryDll 不能逃出 Mod 根目录。", LogLevel.Warn);
                return false;
            }

            if (!Path.GetExtension(resolved).Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryDll 必须指向 .dll 文件。", "EntryDll=" + entryDll + ", resolved=" + resolved);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryDll 必须指向 .dll 文件。", LogLevel.Warn);
                return false;
            }

            if (!File.Exists(resolved))
            {
                Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryDll 缺失。", resolved);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryDll 缺失，路径 " + resolved + "。", LogLevel.Warn);
                return false;
            }

            dllPath = resolved;
            return true;
        }

        private bool TryResolveEntryType(DiscoveredMod mod, Assembly assembly, out Type? entryType)
        {
            entryType = null;
            Type[] entryTypes = assembly.GetTypes()
                .Where(t => typeof(DtmMod).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(t => t.FullName, StringComparer.Ordinal)
                .ToArray();

            string requestedEntryType = (mod.Manifest.EntryType ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(requestedEntryType))
            {
                Type[] matches = entryTypes
                    .Where(t =>
                        string.Equals(t.FullName, requestedEntryType, StringComparison.Ordinal) ||
                        string.Equals(t.Name, requestedEntryType, StringComparison.Ordinal))
                    .ToArray();
                if (matches.Length == 1)
                {
                    entryType = matches[0];
                    return true;
                }

                string details = "EntryType=" + requestedEntryType + "; candidates=" + string.Join(", ", entryTypes.Select(t => t.FullName ?? t.Name).ToArray());
                Diagnostics.RecordError(
                    mod.Manifest.UniqueID,
                    matches.Length == 0 ? "EntryType 未找到。" : "EntryType 匹配多个 DtmMod 类型。",
                    details);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryType " + requestedEntryType + (matches.Length == 0 ? " 未在 DLL 中找到。" : " 匹配多个 DtmMod 类型。"), LogLevel.Warn);
                return false;
            }

            if (entryTypes.Length == 0)
            {
                Diagnostics.RecordError(mod.Manifest.UniqueID, "未找到 DtmMod 入口类型。", "No concrete DtmMod entry type with a parameterless constructor was found.");
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：DLL 内没有可构造的 DtmMod 入口类型。", LogLevel.Warn);
                return false;
            }

            if (entryTypes.Length > 1)
            {
                string details = "EntryType is required when a DLL contains multiple DtmMod subclasses: " + string.Join(", ", entryTypes.Select(t => t.FullName ?? t.Name).ToArray());
                Diagnostics.RecordError(mod.Manifest.UniqueID, "EntryType 缺失且 DLL 内包含多个 DtmMod 子类。", details);
                RuntimeMonitor.Log("跳过 " + mod.Manifest.UniqueID + "：EntryType 缺失且 DLL 内包含多个 DtmMod 子类，请在 manifest 中填写 EntryType。", LogLevel.Warn);
                return false;
            }

            entryType = entryTypes[0];
            return true;
        }

        private IReadOnlyList<DiscoveredMod> OrderMods(IReadOnlyList<DiscoveredMod> mods, out HashSet<string> dependencyCycleBlockedIds)
        {
            dependencyCycleBlockedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var byId = mods.Where(m => !string.IsNullOrWhiteSpace(m.Manifest.UniqueID)).ToDictionary(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase);
            var ordered = new List<DiscoveredMod>();
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var reportedCycles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (DiscoveredMod mod in mods.OrderBy(m => m.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase))
                Visit(mod, byId, ordered, visiting, visited, new List<string>(), reportedCycles, dependencyCycleBlockedIds);

            return ordered;
        }

        private void Visit(
            DiscoveredMod mod,
            Dictionary<string, DiscoveredMod> byId,
            List<DiscoveredMod> ordered,
            HashSet<string> visiting,
            HashSet<string> visited,
            List<string> stack,
            HashSet<string> reportedCycles,
            HashSet<string> dependencyCycleBlockedIds)
        {
            string id = mod.Manifest.UniqueID;
            if (visited.Contains(id))
                return;
            if (!visiting.Add(id))
            {
                int index = stack.FindIndex(value => value.Equals(id, StringComparison.OrdinalIgnoreCase));
                IEnumerable<string> cycle = index >= 0 ? stack.Skip(index).Concat(new[] { id }) : stack.Concat(new[] { id });
                foreach (string cycleId in cycle.Distinct(StringComparer.OrdinalIgnoreCase))
                    dependencyCycleBlockedIds.Add(cycleId);
                string cycleText = string.Join(" -> ", cycle.ToArray());
                if (reportedCycles.Add(cycleText))
                {
                    Diagnostics.RecordError("DTMAPI.ModLoader", "检测到 Mod 依赖循环。", cycleText);
                    RuntimeMonitor.Log("检测到 Mod 依赖循环：" + cycleText + "。", LogLevel.Warn);
                }
                return;
            }
            stack.Add(id);
            foreach (IManifestDependency dependency in ((IManifest)mod.Manifest).Dependencies)
            {
                if (byId.TryGetValue(dependency.UniqueID, out DiscoveredMod dependencyMod))
                    Visit(dependencyMod, byId, ordered, visiting, visited, stack, reportedCycles, dependencyCycleBlockedIds);
            }
            stack.RemoveAt(stack.Count - 1);
            visiting.Remove(id);
            visited.Add(id);
            ordered.Add(mod);
        }

        private static bool IsVersionRequirementSatisfied(string minimumVersion, string actualVersion, out string reason)
        {
            reason = string.Empty;
            if (string.IsNullOrWhiteSpace(minimumVersion))
                return true;

            if (!TryParseVersion(minimumVersion, out Version minimum))
            {
                reason = "Cannot parse required version.";
                return false;
            }

            if (!TryParseVersion(actualVersion, out Version actual))
            {
                reason = "Cannot parse actual version.";
                return false;
            }

            bool satisfied = CompareVersions(actual, minimum) >= 0;
            if (!satisfied)
                reason = "Actual version is older than the required minimum.";
            return satisfied;
        }

        private static bool TryParseVersion(string value, out Version version)
        {
            version = new Version(0, 0, 0, 0);
            string text = (value ?? string.Empty).Trim();
            int suffixIndex = text.IndexOfAny(new[] { '-', '+' });
            if (suffixIndex >= 0)
                text = text.Substring(0, suffixIndex);
            return Version.TryParse(text, out version);
        }

        private static int CompareVersions(Version actual, Version minimum)
        {
            int[] left = { actual.Major, actual.Minor, Math.Max(actual.Build, 0), Math.Max(actual.Revision, 0) };
            int[] right = { minimum.Major, minimum.Minor, Math.Max(minimum.Build, 0), Math.Max(minimum.Revision, 0) };
            for (int i = 0; i < left.Length; i++)
            {
                int comparison = left[i].CompareTo(right[i]);
                if (comparison != 0)
                    return comparison;
            }
            return 0;
        }

        private static string EnsureTrailingDirectorySeparator(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
                path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                return path;
            return path + Path.DirectorySeparatorChar;
        }

        private static IManifest CreateRuntimeManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI",
                Author = "DTMAPI",
                Version = ApiVersion,
                UniqueID = "DTMAPI",
                Type = "Runtime"
            };
        }

        private static IManifest CreateConfigMenuManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI Mod Config Menu",
                Author = "DTMAPI",
                Version = ApiVersion,
                UniqueID = "DTMAPI.ModConfigMenu",
                Type = "RuntimeApi"
            };
        }

        private Dictionary<string, OwnerRootIsolationCounts> CaptureOwnerRootCounts(IEnumerable<string> owners)
        {
            var result = new Dictionary<string, OwnerRootIsolationCounts>(StringComparer.OrdinalIgnoreCase);
            InputOwnerSnapshot inputSnapshot = Input.GetOwnerSnapshot();
            IConfigMenuPage[] configPages = configMenuRuntime?.GetPages().ToArray() ?? Array.Empty<IConfigMenuPage>();

            foreach (string owner in owners)
            {
                var counts = new OwnerRootIsolationCounts
                {
                    InputButton = inputSnapshot.ButtonsByOwner.TryGetValue(owner, out int inputCount) ? inputCount : 0,
                    EventHandler = Events.CountOwnerResources(owner),
                    ConfigPage = configPages.Count(page => page.Manifest.UniqueID.Equals(owner, StringComparison.OrdinalIgnoreCase)),
                    LoadedCodeMod = loadedMods.Any(mod => mod.Manifest.UniqueID.Equals(owner, StringComparison.OrdinalIgnoreCase)) ? 1 : 0
                };
                result[owner] = counts;
            }

            return result;
        }

        private static string FormatOwnerRootCounts(IReadOnlyDictionary<string, OwnerRootIsolationCounts> counts)
        {
            string value = string.Join("; ", counts
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => SanitizeMetricKey(pair.Key) +
                    "={InputButton=" + pair.Value.InputButton.ToString(CultureInfo.InvariantCulture) +
                    "; EventHandler=" + pair.Value.EventHandler.ToString(CultureInfo.InvariantCulture) +
                    "; ConfigPage=" + pair.Value.ConfigPage.ToString(CultureInfo.InvariantCulture) +
                    "; LoadedCodeMod=" + pair.Value.LoadedCodeMod.ToString(CultureInfo.InvariantCulture) + "}"));
            return string.IsNullOrWhiteSpace(value) ? "none" : value;
        }

        private static string FormatUnexpectedRemainingSuppressedRoots(
            IReadOnlyDictionary<string, OwnerRootIsolationCounts> after,
            IReadOnlyList<string> targetRootTypes)
        {
            var remaining = new List<string>();
            foreach (KeyValuePair<string, OwnerRootIsolationCounts> pair in after.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (targetRootTypes.Contains("InputButton", StringComparer.OrdinalIgnoreCase) && pair.Value.InputButton > 0)
                    remaining.Add(SanitizeMetricKey(pair.Key) + ".InputButton=" + pair.Value.InputButton.ToString(CultureInfo.InvariantCulture));
                if (targetRootTypes.Contains("EventHandler", StringComparer.OrdinalIgnoreCase) && pair.Value.EventHandler > 0)
                    remaining.Add(SanitizeMetricKey(pair.Key) + ".EventHandler=" + pair.Value.EventHandler.ToString(CultureInfo.InvariantCulture));
                if (targetRootTypes.Contains("ConfigPage", StringComparer.OrdinalIgnoreCase) && pair.Value.ConfigPage > 0)
                    remaining.Add(SanitizeMetricKey(pair.Key) + ".ConfigPage=" + pair.Value.ConfigPage.ToString(CultureInfo.InvariantCulture));
            }

            return remaining.Count == 0 ? "none" : string.Join("|", remaining);
        }

        private string FormatUnexpectedDisabledCodeOwners(IEnumerable<string> targetOwners)
        {
            string value = string.Join("|", targetOwners
                .Where(owner => !loadedMods.Any(mod => mod.Manifest.UniqueID.Equals(owner, StringComparison.OrdinalIgnoreCase)))
                .Select(SanitizeMetricKey));
            return string.IsNullOrWhiteSpace(value) ? "none" : value;
        }

        private static string SanitizeMetricValue(string value) => SanitizeMetricKey(value);

        private static SaveLoadObjectSnapshotMode ParseSaveLoadObjectSnapshotMode(string? mode)
        {
            string value = (mode ?? string.Empty).Trim();
            if (value.Equals(nameof(SaveLoadObjectSnapshotMode.Lite), StringComparison.OrdinalIgnoreCase))
                return SaveLoadObjectSnapshotMode.Lite;
            if (value.Equals(nameof(SaveLoadObjectSnapshotMode.Off), StringComparison.OrdinalIgnoreCase))
                return SaveLoadObjectSnapshotMode.Off;
            return SaveLoadObjectSnapshotMode.Full;
        }

        private enum SaveLoadObjectSnapshotMode
        {
            Full,
            Lite,
            Off
        }

        private sealed class OwnerRootIsolationCounts
        {
            public int InputButton { get; set; }
            public int EventHandler { get; set; }
            public int ConfigPage { get; set; }
            public int LoadedCodeMod { get; set; }
        }

    }

}
