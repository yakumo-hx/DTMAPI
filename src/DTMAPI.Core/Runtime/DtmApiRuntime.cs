using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Services;
using DTMAPI.Internal.Authoring;

namespace DTMAPI.Core.Runtime
{
    public sealed partial class DtmApiRuntime : IDtmDiagnosticsApi
    {
        public const string ApiVersion = DtmApiBuildVersion.ReleaseVersion;
        public const string BinaryVersion = DtmApiBuildVersion.BinaryFileVersion;

        private readonly IRuntimeHost host;
        private readonly IDtmConfigMenuApi? configMenuApi;
        private readonly IConfigMenuRuntime? configMenuRuntime;
        private readonly List<DiscoveredMod> discoveredMods = new List<DiscoveredMod>();
        private readonly List<DiscoveredMod> loadedMods = new List<DiscoveredMod>();
        private DependencyAssemblyLoader? activeDependencyLoader;
        private readonly ModPackageLoader packageLoader;
        private DiscoveredMod[] discoveredModsSnapshot = Array.Empty<DiscoveredMod>();
        private DiscoveredMod[] loadedModsSnapshot = Array.Empty<DiscoveredMod>();
        private IReadOnlyList<DiscoveredMod> discoveredModsView = Array.AsReadOnly(Array.Empty<DiscoveredMod>());
        private IReadOnlyList<DiscoveredMod> loadedModsView = Array.AsReadOnly(Array.Empty<DiscoveredMod>());
        private readonly Dictionary<string, DiscoveredMod> discoveredById = new Dictionary<string, DiscoveredMod>(StringComparer.OrdinalIgnoreCase);
        private readonly ModOwnerInstanceStore ownerInstances = new ModOwnerInstanceStore();
        private readonly OwnerDependencyStore ownerDependencies = new OwnerDependencyStore();
        private readonly ContentManifestRegistry contentManifestRegistry = new ContentManifestRegistry();
        private readonly RuntimeBoundarySlot<Action<int?, bool>> saveSessionLoadedBoundary = new RuntimeBoundarySlot<Action<int?, bool>>();
        private readonly RuntimeBoundarySlot<Action> returnedToTitleBoundary = new RuntimeBoundarySlot<Action>();
        private readonly RuntimeBoundarySlot<Action<int, bool>> nativeLoadGameReturnedBoundary = new RuntimeBoundarySlot<Action<int, bool>>();
        private readonly RuntimeBoundarySlot<Action> nativeGameFrameBoundary = new RuntimeBoundarySlot<Action>();
        private readonly RuntimeBoundarySlot<Action> logExportBoundary = new RuntimeBoundarySlot<Action>();
        private readonly RuntimeBoundarySlot<Action<string>> runtimeShutdownBoundary = new RuntimeBoundarySlot<Action<string>>();
        private readonly OwnerServiceManager ownerServices;
        private readonly ContentHostManager contentHosts;
        private readonly PlatformRuntimeServices platformServices;
        private readonly ModOwnerLifecycleCoordinator modOwnerLifecycle = new ModOwnerLifecycleCoordinator();
        private readonly IModPatchCleanup? modPatchCleanup;
        private readonly List<Func<string>> runtimeReportContextProviders = new List<Func<string>>();
        private readonly List<IModOwnerCleanupParticipant> modOwnerCleanupParticipants = new List<IModOwnerCleanupParticipant>();

        private readonly DateTimeOffset startedAt = DateTimeOffset.Now;
        private DateTimeOffset lastSecondTick = DateTimeOffset.Now;
        private ContentManifestRegistrySnapshot? latestContentManifestRegistrySnapshot;
        private IReadOnlyList<string> latestManifestScannerErrors = Array.Empty<string>();
        private IReadOnlyList<string> latestManifestScannerWarnings = Array.Empty<string>();
        private NativeWorkshopSubscriptionSnapshot nativeWorkshopSubscriptions = NativeWorkshopSubscriptionSnapshot.Unavailable("GameBridge has not captured the native subscription owner yet.");
        private AuthorSourceSelectionState? latestAuthorSourceState;
        private IReadOnlyList<AuthorSourceSelectionDecision> latestAuthorSourceSelectionDecisions = Array.Empty<AuthorSourceSelectionDecision>();
        private string latestSourceSelectionSummary = "not-run";
        private string currentRuntimePhase = "Constructing";
        private ulong updateTick;
        private uint secondTick;
        private int? currentLoadingSlot;
        private int runtimeThreadId;
        private bool started;
        private readonly RuntimeLanguageSource languageSource = new RuntimeLanguageSource();

        internal DtmApiRuntime(IRuntimeHost host, IDtmConfigMenuApi? configMenuApi, IModPatchCleanup modPatchCleanup)
            : this(host, configMenuApi)
        {
            this.modPatchCleanup = modPatchCleanup;
        }

        public DtmApiRuntime(IRuntimeHost host, IDtmConfigMenuApi? configMenuApi = null)
        {
            this.host = host;
            packageLoader = new ModPackageLoader(this);
            this.configMenuApi = configMenuApi;
            configMenuRuntime = configMenuApi as IConfigMenuRuntime;
            runtimeThreadId = Thread.CurrentThread.ManagedThreadId;
            Paths = new RuntimePaths(host.GamePath, host.PluginPath);
            Diagnostics = new DiagnosticsService(Paths);
            platformServices = new PlatformRuntimeServices(() => Thread.CurrentThread.ManagedThreadId == runtimeThreadId,
                (owner, message, error) => Diagnostics.RecordError(owner, message, error.ToString()));
            platformServices.ContextPublished = snapshot => RuntimeMonitor?.Log("Platform.Context instance=" + snapshot.RuntimeInstanceId +
                "; revision=" + snapshot.Revision + "; phase=" + snapshot.Phase + "; saveEpoch=" + snapshot.SaveSessionEpoch +
                "; worldEpoch=" + snapshot.WorldEpoch + "; ready=" + snapshot.IsWorldReady + "; thread=" + Thread.CurrentThread.ManagedThreadId);
            contentHosts = new ContentHostManager(() => EnsureModHelperRuntimeThread("ContentHosts"),
                (owner, state) => RuntimeMonitor.Log("ContentHost owner=" + owner + "; " + state));
            ownerServices = new OwnerServiceManager(platformServices, contentHosts);
            RegisterModOwnerCleanupParticipant(contentHosts);
            RegisterModOwnerCleanupParticipant(platformServices);
            RegisterModOwnerCleanupParticipant(ownerServices);
            Events = new EventManager(Diagnostics);
            Config = new ConfigService(Paths, Diagnostics);
            ModRegistry = new ModRegistryService();
            Workshop = new WorkshopService();
            Content = new ContentQueryService(Diagnostics);
            Input = new InputService();
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
            ConfigDialogs = new ConfigMenuDialogService(UI, () => EnsureModHelperRuntimeThread("ConfigDialog"),
                (owner, ex) => Diagnostics.RecordError(owner, "Config dialog failed.", ex.ToString()));
            configMenuRuntime?.ConfigureDialogs(ConfigDialogs);
        }

        public RuntimePaths Paths { get; }
        public DiagnosticsService Diagnostics { get; }
        public UiRuntimeService UI { get; }
        internal ConfigMenuDialogService ConfigDialogs { get; }
        internal IConfigMenuRuntime? ConfigMenuRuntime => configMenuRuntime;
        internal EventManager Events { get; }
        internal ConfigService Config { get; }
        internal ModRegistryService ModRegistry { get; }
        internal WorkshopService Workshop { get; }
        internal ContentQueryService Content { get; }
        internal InputService Input { get; }
        public IMonitor RuntimeMonitor { get; private set; } = NullMonitor.Instance;
        public IReadOnlyList<DiscoveredMod> DiscoveredMods => discoveredModsView;
        public IReadOnlyList<DiscoveredMod> LoadedMods => loadedModsView;
        public DateTimeOffset StartedAt => startedAt;
        internal ContentManifestRegistrySnapshot? LatestContentManifestRegistrySnapshot => latestContentManifestRegistrySnapshot;

        internal ulong RuntimeUpdateTickCount => updateTick;

        internal void ConfigureLanguageProvider(Func<string?>? provider)
        {
            languageSource.SetNativeLanguageProvider(provider);
        }
        internal IReadOnlyList<AuthorSourceSelectionDecision> AuthorSourceSelectionDecisions => latestAuthorSourceSelectionDecisions;
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
        internal bool OwnerRequiresRestart(string ownerId) => modOwnerLifecycle.RequiresRestart(ownerId);
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

        internal void RegisterModOwnerCleanupParticipant(IModOwnerCleanupParticipant participant)
        {
            string participantId = string.IsNullOrWhiteSpace(participant.ParticipantId)
                ? participant.GetType().FullName ?? participant.GetType().Name
                : participant.ParticipantId.Trim();
            if (modOwnerCleanupParticipants.Any(existing => existing.ParticipantId.Equals(participantId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("A mod-owner cleanup participant is already registered as " + participantId + ".");
            modOwnerCleanupParticipants.Add(participant);
        }

        internal ModOwnerParticipantCleanupSummary CleanupModOwnerParticipants(string ownerId, ModOwnerCleanupReason reason)
        {
            var entries = new List<ModOwnerParticipantCleanupEntry>(modOwnerCleanupParticipants.Count);
            foreach (IModOwnerCleanupParticipant participant in modOwnerCleanupParticipants)
            {
                string participantId = participant.ParticipantId;
                try
                {
                    ModOwnerCleanupParticipantResult result = participant.RemoveOwner(ownerId, reason);
                    int failures = Math.Max(result.FailureCount, result.RemainingResources > 0 ? 1 : 0);
                    entries.Add(new ModOwnerParticipantCleanupEntry(participantId, result.RemovedResources, failures == 0, result.Details, result.RemainingResources, failures));
                }
                catch (Exception ex)
                {
                    entries.Add(new ModOwnerParticipantCleanupEntry(participantId, 0, false, ex.GetType().Name + ": " + ex.Message));
                    Diagnostics.RecordError(ownerId, "Mod cleanup participant failed: " + participantId, ex.ToString());
                }
            }
            if (reason == ModOwnerCleanupReason.EntryFailed && activeDependencyLoader != null && !runtimeModuleIds.Contains(ownerId, StringComparer.OrdinalIgnoreCase))
            {
                // Mod disposal has finished. Include dependencies it loaded while cleaning up.
                var bindings = activeDependencyLoader.GetBindings().Where(pair => pair.Value.Contains(ownerId, StringComparer.OrdinalIgnoreCase)).ToArray();
                var exclusive = bindings.Where(pair => pair.Value.Length == 1).Select(pair => pair.Key).ToArray();
                var shared = bindings.Where(pair => pair.Value.Length > 1).Select(pair => pair.Key).ToArray();
                string hint = discoveredById.TryGetValue(ownerId, out var mod) ? mod.Classification.ExpectedHarmonyOwner : "";
                const string participantId = "DTMAPI.ModPatches";
                try
                {
                    var result = modPatchCleanup?.RemovePatches(exclusive, shared, hint)
                        ?? (bindings.Length == 0
                            ? ModOwnerCleanupParticipantResult.None("No managed assembly was bound for this Mod.")
                            : new ModOwnerCleanupParticipantResult(0, 0, 1, "Mod patch cleanup is not available; restart required."));
                    entries.Add(new ModOwnerParticipantCleanupEntry(participantId, result.RemovedResources,
                        result.FailureCount == 0 && result.RemainingResources == 0, result.Details, result.RemainingResources,
                        Math.Max(result.FailureCount, result.RemainingResources > 0 ? 1 : 0)));
                }
                catch (Exception ex)
                {
                    entries.Add(new ModOwnerParticipantCleanupEntry(participantId, 0, false, ex.GetType().Name + ": " + ex.Message));
                    Diagnostics.RecordError(ownerId, "Mod patch cleanup failed; restart required.", ex.ToString());
                }
            }
            return new ModOwnerParticipantCleanupSummary(ownerId, reason, entries);
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

            IManifest runtimeManifest = CreateRuntimeManifest();
            RegisterRuntimeApi<IDtmDiagnosticsApi>(runtimeManifest, this);
            if (configMenuApi != null)
            {
                IManifest configMenuManifest = CreateConfigMenuManifest();
                RegisterRuntimeApi<IDtmConfigMenuApi>(configMenuManifest, configMenuApi);
            }

            InitializeRuntimeModules();
            using (var packageReads = new StartupPackageReadTransaction())
            {
                DiscoverMods(packageReads);
                long afterDiscovery = startup.ElapsedMilliseconds;
                LoadMods(packageReads);
                RuntimeMonitor.Log("Startup segment ModLoad elapsedMs=" + (startup.ElapsedMilliseconds - afterDiscovery) + " totalMs=" + startup.ElapsedMilliseconds + ".");
            }
            RefreshConfigPageLocks();
            SetHookStatus("GameLoop.GameLaunched", "verified", "DTMAPI.Core", "Dispatched after DTMAPI mod Entry completed.");
            platformServices.ReturnToTitle(completed: true);
            Events.DispatchGameLaunched();
            RuntimeMonitor.Log("GameLaunched dispatched.");
            RuntimeMonitor.Log("Startup segment Core.Start totalMs=" + startup.ElapsedMilliseconds + ".");
        }

        public void Update()
        {
            currentRuntimePhase = "Update";
            try
            {
                platformServices.BeginTick(updateTick + 1);
                updateTick++;
                platformServices.Drain();
                ownerDependencies.RetryPendingContributions();
                languageSource.PollLanguageChanges();
                Events.DispatchUpdateTicked(updateTick);
                DateTimeOffset now = DateTimeOffset.Now;
                if ((now - lastSecondTick).TotalSeconds >= 1)
                {
                    secondTick++;
                    lastSecondTick = now;
                    Events.DispatchOneSecondUpdateTicked(secondTick);
                    // Global patch auditing is an explicit diagnostic, never a timer gate.
                }
            }
            finally
            {
                Input.ClearFrame();
            }
        }

        internal IReadOnlyList<string> GetInputButtonsToSample() => Input.GetButtonsToSample(CreateInputAudienceSnapshot());

        internal InputFrameResult RecordInputFrame(IReadOnlyList<InputButtonSample> buttonSamples)
        {
            InputAudienceSnapshot audience = CreateInputAudienceSnapshot();
            return Input.RecordFrame(
                audience,
                buttonSamples,
                dispatch =>
                {
                    RuntimeMonitor.Log("Input " + dispatch.Button + " pressed dispatched to DTMAPI audience=" + audience.Mode + " owner=" + (dispatch.TargetOwnerId.Length == 0 ? "broadcast" : dispatch.TargetOwnerId) + ".");
                    return dispatch.IsBroadcast
                        ? Events.DispatchButtonPressedWithRecipients(dispatch.Button)
                        : Events.DispatchButtonPressedToOwnerWithRecipients(dispatch.TargetOwnerId, dispatch.Button);
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
                        ? Events.DispatchKeybindPressedWithRecipients(keybind.OwnerId, keybind.KeybindId, keybind.Keybinds, keybind.TriggerButton)
                        : Events.DispatchKeybindPressedToOwnerWithRecipients(targetOwnerId, keybind.OwnerId, keybind.KeybindId, keybind.Keybinds, keybind.TriggerButton);
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
            platformServices.ReturnToTitle(completed: false);
        }

        public void NotifyLoadGameRequested(int slot)
        {
            currentLoadingSlot = slot;
            platformServices.BeginSaveAttempt();

            RuntimeMonitor.Log("LoadGame requested for slot/index " + slot.ToString(CultureInfo.InvariantCulture) + ".");
        }

        public void NotifyLoadGameReturned(int slot, bool result)
        {
            RuntimeMonitor.Log("Native LoadGame returned for slot/index " + slot.ToString(CultureInfo.InvariantCulture) + "; result=" + (result ? "true" : "false") + ".");
            platformServices.CompleteSaveAttempt(result);
            InvokeRuntimeBoundary(nativeLoadGameReturnedBoundary, "Native LoadGame return", slot, result);
        }

        internal void BeginOwnerEntry(string ownerId)
        {
            modOwnerLifecycle.BeginEntry(ownerId);
        }

        internal void ActivateOwnerEntry(IManifest manifest)
        {
            ModRegistry.AddLoaded(manifest);
            modOwnerLifecycle.Activate(manifest.UniqueID);
        }

        internal bool HasOwnerInstance(string ownerId) => ownerInstances.HasPublished(ownerId);
        internal DtmMod? GetOwnerInstance(string ownerId) => ownerInstances.GetPublished(ownerId);
        internal bool HasOwnerAssemblyLoadAttempt(string ownerId) => ownerInstances.HasAssemblyLoadAttempt(ownerId);

        internal void NotifyNativeGameFrame()
        {
            InvokeRuntimeBoundary(nativeGameFrameBoundary, "Native game frame");
        }

        public void NotifySaveLoaded(bool isNewGame)
        {
            ConfigDialogs.Clear();
            platformServices.ObserveAfterLoad();
            currentRuntimePhase = "SaveLoaded";
            RuntimeMonitor.Log($"SaveLoaded hook dispatched. slot/index={currentLoadingSlot?.ToString() ?? "unknown"} isNewGame={isNewGame}");
            // Clear the previous save's input before notifying Mod callbacks.
            Input.ClearTransientState();
            InvokeRuntimeBoundary(saveSessionLoadedBoundary, "Save session", currentLoadingSlot, isNewGame);
            Events.DispatchSaveLoaded(currentLoadingSlot, isNewGame);
        }

        public void NotifySaveSaving(int? slot)
        {
            _ = TryNotifySaveSaving(slot);
        }

        public bool TryNotifySaveSaving(int? slot)
        {
            currentRuntimePhase = "SaveSaving";
            RuntimeMonitor.Log($"SaveSaving hook dispatched. slot/index={(slot ?? currentLoadingSlot)?.ToString() ?? "unknown"}");
            bool succeeded =
                Events.TryDispatchSaveSaving(
                    slot ?? currentLoadingSlot);
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
            currentRuntimePhase = "SaveSaved";
            RuntimeMonitor.Log($"SaveSaved hook dispatched. slot/index={(slot ?? currentLoadingSlot)?.ToString() ?? "unknown"}");
            Events.DispatchSaveSaved(slot ?? currentLoadingSlot);
        }

        public void NotifyReturnedToTitle()
        {
            ConfigDialogs.Clear();
            currentRuntimePhase = "ReturnedToTitle";
            platformServices.ReturnToTitle(completed: true);
            RuntimeMonitor.Log("ReturnedToTitle hook dispatched.");
            currentLoadingSlot = null;
            Input.ClearTransientState();
            InvokeRuntimeBoundary(returnedToTitleBoundary, "Returned-to-title");
            Events.DispatchReturnedToTitle();
        }

        public void NotifyWorkshopModListChanged()
        {
            currentRuntimePhase = "WorkshopModListChanged";
            using (var packageReads = new StartupPackageReadTransaction())
            {
                DiscoverMods(packageReads);
                LoadMods(packageReads, late: true);
            }
            Events.DispatchWorkshopModListChanged(discoveredMods.Count);
            ShowModListChanges();
        }

        // Keys already shown after an earlier page close, so each change pops up once.
        private HashSet<string> shownModChanges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private void ShowModListChanges()
        {
            var mods = CreateDiagnosticsSnapshot().Mods;
            var stopping = mods.Where(mod => mod.StatusCode == "restart-required" && mod.Loaded && !mod.OfficialEnabled).ToArray();
            var starting = mods.Where(mod => mod.StatusCode == "restart-required" && !(mod.Loaded && !mod.OfficialEnabled)).ToArray();
            var failed = mods.Where(mod => mod.OfficialEnabled && !mod.Loaded && mod.Status == "error").ToArray();
            var current = new HashSet<string>(
                stopping.Select(mod => "stop:" + mod.UniqueID)
                    .Concat(starting.Select(mod => "start:" + mod.UniqueID))
                    .Concat(failed.Select(mod => "failed:" + mod.UniqueID)),
                StringComparer.OrdinalIgnoreCase);
            var shown = shownModChanges;
            shownModChanges = current;
            stopping = stopping.Where(mod => !shown.Contains("stop:" + mod.UniqueID)).ToArray();
            starting = starting.Where(mod => !shown.Contains("start:" + mod.UniqueID)).ToArray();
            failed = failed.Where(mod => !shown.Contains("failed:" + mod.UniqueID)).ToArray();
            if (stopping.Length == 0 && starting.Length == 0 && failed.Length == 0) return;

            var lines = new List<string>();
            if (starting.Length != 0) lines.Add("重启后生效：" + string.Join("、", starting.Select(mod => mod.Name)));
            if (stopping.Length != 0) lines.Add("重启后停用：" + string.Join("、", stopping.Select(mod => mod.Name)));
            if (failed.Length != 0)
            {
                if (lines.Count != 0) { lines.Add(""); lines.Add("加载失败 Failed："); }
                var errors = Diagnostics.GetErrors();
                foreach (var mod in failed)
                    lines.Add(mod.Name + "：" + errors.Last(error => error.Owner.Equals(mod.UniqueID, StringComparison.OrdinalIgnoreCase)).Message);
            }
            string title = stopping.Length + starting.Length != 0 ? "需要重启 Restart" : "加载失败 Failed";
            string message = string.Join("\n", lines);
            ConfigDialogs.TryShow("DTMAPI", new DtmConfigDialog(title, message,
                new[] { new DtmConfigDialogButton("ok", "知道了") }, "ok",
                size: new DtmConfigDialogSize(480, DialogHeight(message))), _ => { }, out _);
        }

        // Fit the panel to the text: the dialog keeps 182 px for title, gaps and button, and 22 px
        // per line; a line holds about 48 width units (CJK counts 2), matching the dialog's wrapping.
        private static int DialogHeight(string message)
        {
            int lines = 0;
            foreach (string paragraph in message.Split('\n'))
            {
                int units = paragraph.Sum(c => c > 0x7f ? 2 : 1);
                lines += Math.Max(1, (units + 47) / 48);
            }
            return Math.Min(480, Math.Max(200, 182 + lines * 22));
        }

        public void SetHookStatus(string hookId, string status, string source, string details)
        {
            if (!Diagnostics.SetHookStatus(hookId, status, source, details))
                return;
            Events.DispatchHookStatusChanged(hookId, status);
            RuntimeMonitor.Log("Hook status: " + hookId + " = " + status + ". " + details);
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
            if (!ModRegistry.IsRuntimeProvider(owner.UniqueID) && ModRegistry.IsLoaded(owner.UniqueID))
            {
                throw new InvalidOperationException(
                    "Process-lifetime owner '" + owner.UniqueID + "' conflicts with an already loaded source-managed owner. " +
                    "A process provider can't share or replace an ordinary owner in the same process.");
            }
            ModRegistry.RegisterProcessLifetimeApiForOwner(owner, api, ownerBoundFactory);

            RuntimeMonitor.Log("Registered runtime API " + typeof(TApi).FullName + " from " + owner.UniqueID + ".");
        }

        public string ExportLogs()
        {
            currentRuntimePhase = "LogExport";
            InvokeRuntimeBoundary(logExportBoundary, "Log export");
            string report = Diagnostics.ExportLogs(BuildRuntimeReportContext());
            Events.DispatchLogExported(report);
            RuntimeMonitor.Log("Exported DTMAPI logs to " + report);
            return report;
        }

        public void NotifyRuntimeShutdown(string reason)
        {
            ConfigDialogs.Clear();
            platformServices.Shutdown();
            currentRuntimePhase = "Shutdown";
            RuntimeMonitor.Log("Runtime shutdown observed by DTMAPI. reason=" + (string.IsNullOrWhiteSpace(reason) ? "unknown" : reason) + ".");
            var shutdownOwners = loadedMods.Select(mod => mod.Manifest.UniqueID)
                .Concat(ownerInstances.PublishedOwners)
                .Concat(modOwnerLifecycle.GetOwners().Reverse())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Reverse()

                .ToArray();
            foreach (string ownerId in shutdownOwners)
            {
                try
                {
                    DeactivateOwner(ownerId, ModOwnerCleanupReason.RuntimeShutdown, shutdown: true);
                }
                catch (Exception ex)
                {
                    TryLogOwnerCleanupDiagnostic("Runtime shutdown owner cleanup failed owner=" + ownerId + ": " + ex.GetType().Name + ": " + ex.Message);
                }
            }
            InvokeRuntimeBoundary(runtimeShutdownBoundary, "Runtime shutdown", reason);
            runtimeComponents?.Dispose();
            activeDependencyLoader?.Dispose();
            activeDependencyLoader = null;
        }

        private void RefreshContentManifestRegistry(string reason)
        {
            latestContentManifestRegistrySnapshot = contentManifestRegistry.Build(
                discoveredModsSnapshot,
                loadedModsSnapshot,
                latestManifestScannerErrors,
                latestManifestScannerWarnings,
                reason,
                ModRegistry.GetAll());
        }

        private string BuildRuntimeReportContext()
        {
            IDtmDiagnosticsSnapshot snapshot = CreateDiagnosticsSnapshot();
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
                    bool assemblyLoaded = ownerInstances.HasAssemblyLoadAttempt(error.Owner);
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
            return ModRegistry.IsRuntimeProvider(uniqueId) ||

                uniqueId.Equals("DTMAPI", StringComparison.OrdinalIgnoreCase) ||
                uniqueId.Equals("DTMAPI.ModConfigMenu", StringComparison.OrdinalIgnoreCase) ||
                uniqueId.Equals("DTMAPI.GameBridge.DolocTown", StringComparison.OrdinalIgnoreCase);
        }

        private static string SingleLine(string? value)
        {
            string singleLine = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (string.IsNullOrWhiteSpace(singleLine))
                return "-";
            return singleLine;
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
                modOwnerLifecycle.GetRestartRequiredOwners(),
                mod => packageLoader.GetRestartReason(mod));
        }

        IDtmDiagnosticsSnapshot IDtmDiagnosticsApi.GetSnapshot() => CreateDiagnosticsSnapshot();

        private void DiscoverMods(StartupPackageReadTransaction? packageReads = null)
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
            var scanner = new ModScanner(
                Paths,
                nativeWorkshopSubscriptions,
                latestAuthorSourceState,
                packageReads);
            IReadOnlyList<DiscoveredMod> discovered = scanner.Discover();
            latestManifestScannerErrors = scanner.Errors;
            latestManifestScannerWarnings = scanner.Warnings;
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
            RefreshContentManifestRegistry("DiscoverMods");
            RuntimeMonitor.Log($"Discovered {discoveredMods.Count} DTMAPI-capable mod folder(s).");
            RuntimeMonitor.Log("Author source state " + latestAuthorSourceState.FormatSummary() + ".");
            RuntimeMonitor.Log("Source authority " + latestSourceSelectionSummary + "; native={" + nativeWorkshopSubscriptions.FormatSummary() + "}.");
            RuntimeMonitor.Log(
                "Official local MODS root = " + scanner.OfficialLocalModsRoot +
                "; exists=" + scanner.OfficialLocalModsRootExists +
                "; folders=" + scanner.OfficialLocalDirectoryCount +
                "; enablementFile=" + scanner.OfficialEnablementFilePath +
                "; enablementFileExists=" + scanner.OfficialEnablementFileExists +
                "; enablementEntries=" + scanner.OfficialEnablementEntryCount + ".");
            RuntimeMonitor.Log("Startup segment ManifestScan elapsedMs=" + scanner.ManifestScanElapsedMilliseconds + "; OfficialModsScan elapsedMs=" + scanner.OfficialModsScanElapsedMilliseconds + "; WorkshopScan elapsedMs=" + scanner.WorkshopScanElapsedMilliseconds + "; DiscoverMods totalMs=" + scan.ElapsedMilliseconds + ".");
        }

        private int LoadMods(StartupPackageReadTransaction? packageReads = null, bool late = false)
        {
            var residentIds = new HashSet<string>(loadedMods.Select(mod => mod.Manifest.UniqueID), StringComparer.OrdinalIgnoreCase);
            var available = loadedMods.Concat(discoveredMods.Where(mod => mod.OfficialEnabled && !residentIds.Contains(mod.Manifest.UniqueID))).ToArray();
            if (activeDependencyLoader == null)
                activeDependencyLoader = new DependencyAssemblyLoader(available, ModRegistry.RuntimeProviders,
                    message => RuntimeMonitor.Log(message), Paths.GamePath, packageReads);
            else
                activeDependencyLoader.UpdatePackages(available, ModRegistry.RuntimeProviders, packageReads);
            var byId = available.ToDictionary(mod => mod.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase);
            var ordered = activeDependencyLoader.Plan.LoadOrder.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
            ordered.AddRange(available.Where(mod => !ordered.Contains(mod)));
            var ready = ordered.Where(mod => !IsReservedRuntimeOwnerId(mod.Manifest.UniqueID) &&
                !activeDependencyLoader.Plan.Blocked.ContainsKey(mod.Manifest.UniqueID));
            if (frozenLoadPlan == null)
                frozenLoadPlan = new FrozenModLoadPlan(ready, message => RuntimeMonitor.Log(message));
            else
                frozenLoadPlan.Append(ready, message => RuntimeMonitor.Log(message));
            int loadedNow = 0;
            foreach (DiscoveredMod mod in ordered)
                if (packageLoader.Load(mod, activeDependencyLoader, late)) loadedNow++;
            RefreshConfigPageLocks();
            RefreshContentManifestRegistry(late ? "OfficialModPageClosed" : "Startup");
            return loadedNow;
        }

        private void PublishDiscoveredModsSnapshot()
        {
            discoveredModsSnapshot = discoveredMods.ToArray();
            discoveredModsView = Array.AsReadOnly(discoveredModsSnapshot);
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
            loadedModsView = Array.AsReadOnly(loadedModsSnapshot);
        }

        private readonly HashSet<string> runtimeConfigOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal void RegisterRuntimeConfigOwner(string ownerId) => runtimeConfigOwners.Add(ownerId);
        internal void RemoveRuntimeConfigOwner(string ownerId) => runtimeConfigOwners.Remove(ownerId);

        private void RefreshConfigPageLocks()
        {
            if (configMenuRuntime == null)
                return;

            foreach (IConfigMenuPage page in configMenuRuntime.GetPages())
            {
                if (runtimeConfigOwners.Contains(page.Manifest.UniqueID))
                {
                    configMenuRuntime.SetPageLock(page.Manifest.UniqueID, false, string.Empty);
                    continue;
                }
                bool loaded = IsLoadedMod(page.Manifest.UniqueID);
                configMenuRuntime.SetPageLock(page.Manifest.UniqueID, !loaded,
                    loaded ? string.Empty : "此 Mod 本次未加载；请重启游戏或先检查依赖错误，再编辑配置。");
            }
        }


        internal void MarkManagedAssemblyLoadAttempt(string ownerId)
        {
            ownerInstances.MarkAssemblyLoadAttempt(ownerId);
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

        internal string DeactivateOwner(string uniqueId, ModOwnerCleanupReason reason, bool shutdown)
        {
            if (!ownerDependencies.BeginCleanupRequest(uniqueId))
                return "OwnerBoundCleanup: owner=" + uniqueId + ", cleanupInProgressOrUnknown=true";
            try { return DeactivateOwnerCore(uniqueId, reason, shutdown); }
            finally { ownerDependencies.EndCleanupRequest(uniqueId); }
        }

        private string DeactivateOwnerCore(string uniqueId, ModOwnerCleanupReason reason, bool shutdown)
        {
            foreach (string dependent in ownerDependencies.RequiredDependents(uniqueId))
            {
                DeactivateOwner(dependent, reason, shutdown);
                if (!ownerDependencies.CleanupComplete(dependent))
                    return "OwnerBoundCleanup: owner=" + uniqueId + ", deactivationDeferred=true, requiredDependentCleanupPending=" + dependent;
            }
            modOwnerLifecycle.TryGetState(uniqueId, out ModOwnerLifecycleState previousState);
            if (!modOwnerLifecycle.BeginDeactivation(uniqueId))
                return "OwnerBoundCleanup: owner=" + uniqueId + ", cleanupInProgressOrUnknown=true";
            ownerDependencies.SetCleanupComplete(uniqueId, false);

            bool effectiveShutdown = shutdown || previousState == ModOwnerLifecycleState.Shutdown;
            bool firstDeactivation = previousState == ModOwnerLifecycleState.Entering || previousState == ModOwnerLifecycleState.Active;
            bool restartRequired = !effectiveShutdown;
            if (!TryPrepareModInstanceDeactivation(
                    uniqueId,
                    reason,
                    out string preparationFailure))
            {
                // Preparation may already have closed providers or released content.
                // Stop delivery of new work and retain that partial state for retry.
                TryDeactivateStep(uniqueId, "Event", () => Events.RemoveOwner(uniqueId), out _);
                TryDeactivateStep(uniqueId, "Input", () => Input.RemoveOwner(uniqueId), out _);
                TryDeactivateStep(uniqueId, "Modal", () => UI.RemoveOwner(uniqueId), out _);
                modOwnerLifecycle.CompleteDeactivation(uniqueId, effectiveShutdown, restartRequired: true);
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
                    ", restartRequired=True";
                Diagnostics.RecordError(
                    uniqueId,
                    "Owner deactivation is incomplete; new work is stopped and remaining resources await cleanup or restart.",
                    deferred);
                return deferred;
            }
            try
            {
                int lifecycleInstancesRemoved = TryDeactivateModInstance(uniqueId, reason, out bool modDisposeFailed);
                int eventsRemoved = TryDeactivateStep(uniqueId, "Event", () => Events.RemoveOwner(uniqueId), out bool eventsFailed);
                int modalRemoved = TryDeactivateStep(uniqueId, "Modal", () => UI.RemoveOwner(uniqueId), out bool modalFailed);
                int inputRemoved = TryDeactivateStep(uniqueId, "Input", () => Input.RemoveOwner(uniqueId), out bool inputFailed);
                int configPagesRemoved = TryDeactivateStep(uniqueId, "ConfigPage", () => configMenuRuntime?.RemoveOwner(uniqueId) ?? 0, out bool configPagesFailed);
                int migrationsRemoved = TryDeactivateStep(uniqueId, "ConfigMigration", () => Config.RemoveOwner(uniqueId), out bool migrationsFailed);
                int contentRemoved = TryDeactivateStep(uniqueId, "Content", () => Content.RemoveOwner(uniqueId), out bool contentFailed);
                // Remove API facade/cache roots before feature cleanup so facade-owned
                // callbacks and sessions can't observe teardown of their owner state.
                int registryRemoved = TryDeactivateStep(uniqueId, "Registry", () => ModRegistry.RemoveOwner(uniqueId), out bool registryFailed);
                ModOwnerParticipantCleanupSummary participants = CleanupModOwnerParticipants(uniqueId, reason);
                int instancesRemoved = ownerInstances.Unpublish(uniqueId) ? 1 : 0;
                int loadedRemoved = loadedMods.RemoveAll(mod => mod.Manifest.UniqueID.Equals(uniqueId, StringComparison.OrdinalIgnoreCase));
                if (loadedRemoved > 0)
                    PublishLoadedModsSnapshot();
                int coreCleanupFailures = new[] { modDisposeFailed, eventsFailed, modalFailed, inputFailed, configPagesFailed, migrationsFailed, contentFailed, registryFailed }.Count(failed => failed);

                int remaining = CountCoreOwnerRoots(uniqueId);
                bool cleanupProvedZero = remaining == 0 &&
                    coreCleanupFailures == 0 &&
                    participants.RemainingResources == 0 &&
                    participants.FailureCount == 0;
                bool assemblyLoaded = ownerInstances.HasAssemblyLoadAttempt(uniqueId);
                restartRequired = !effectiveShutdown && (assemblyLoaded || !cleanupProvedZero);
                string summary = "OwnerBoundCleanup: owner=" + uniqueId +
                    ", eventsRemoved=" + eventsRemoved +
                    ", modalRemoved=" + modalRemoved +
                    ", eventHandlersRemoved=" + eventsRemoved +
                    ", inputRemoved=" + inputRemoved +
                    ", inputButtonsRemoved=" + inputRemoved +
                    ", configPagesRemoved=" + configPagesRemoved +
                    ", migrationsRemoved=" + migrationsRemoved +
                    ", contentRemoved=" + contentRemoved +
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
                if (remaining > 0 || coreCleanupFailures > 0 || participants.RemainingResources > 0 || participants.FailureCount > 0)
                {
                    Diagnostics.RecordError(uniqueId, "Owner deactivation did not prove zero platform roots.", summary);
                }
                ownerDependencies.SetCleanupComplete(uniqueId, cleanupProvedZero);
                return summary;
            }
            catch (Exception ex)
            {
                restartRequired = !effectiveShutdown;
                string summary = "OwnerBoundCleanup: owner=" + uniqueId + ", unexpectedCleanupFailure=" + ex.GetType().Name + ": " + ex.Message + ", restartRequired=" + restartRequired.ToString(CultureInfo.InvariantCulture);
                Diagnostics.RecordError(uniqueId, "Owner deactivation failed unexpectedly; a later cleanup pass may retry.", summary);
                return summary;
            }
            finally
            {
                modOwnerLifecycle.CompleteDeactivation(uniqueId, effectiveShutdown, restartRequired);
            }
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
                TryLogOwnerCleanupDiagnostic("Owner cleanup step failed owner=" + ownerId + " kind=" + kind + ": " + ex.GetType().Name + ": " + ex.Message);
                return 0;
            }

            return removed;
        }

        private int TryDeactivateModInstance(
            string ownerId,
            ModOwnerCleanupReason reason,
            out bool failed)
        {
            failed = false;
            if (!ownerInstances.TryGetConstructed(ownerId, out DtmMod instance))
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
                    TryLogOwnerCleanupDiagnostic("Mod instance Dispose failed owner=" + ownerId + ": " + details);
                    // Retain the instance as an authoritative remaining root so a later
                    // deactivation pass can retry idempotent product cleanup.
                    return 0;
                }
            }

            ownerInstances.ReleaseConstructed(ownerId);
            return 1;
        }

        private bool TryPrepareModInstanceDeactivation(
            string ownerId,
            ModOwnerCleanupReason reason,
            out string failure)
        {
            failure = string.Empty;
            try { runtimeModules.BeginClose(ownerId); }
            catch (Exception ex) { failure = "OptionalProviderCloseAdmission: " + ex.GetBaseException().Message; return false; }
            if (!ownerDependencies.PrepareContributionsExit(ownerId, out failure)) return false;
            if (!ownerInstances.TryGetConstructed(
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
            RuntimeMonitor.Log(message, LogLevel.Warn);
        }

        internal int CountCoreOwnerRoots(string ownerId)
        {
            int inputRoots = Input.CountOwnerResources(ownerId);
            int eventRoots = Events.CountOwnerResources(ownerId);
            int configPages = configMenuRuntime?.GetPages().Count(page => page.Manifest.UniqueID.Equals(ownerId, StringComparison.OrdinalIgnoreCase)) ?? 0;
            return inputRoots + eventRoots + configPages + ConfigDialogs.CountOwnerResources(ownerId) + UI.CountOwnerResources(ownerId) + Config.CountOwner(ownerId) + ModRegistry.CountOwner(ownerId) +
                Content.CountOwnerResources(ownerId) + ownerDependencies.CountContributions(ownerId) + ownerInstances.CountRoots(ownerId) + loadedMods.Count(mod => mod.Manifest.UniqueID.Equals(ownerId, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsLoadedMod(string uniqueId)
        {
            return loadedMods.Any(m => m.Manifest.UniqueID.Equals(uniqueId, StringComparison.OrdinalIgnoreCase));
        }


        private static bool PackageRangeSatisfied(PackageModDependency dependency, string version)
        { if (dependency.LegacyOrderingOnly) return true; try { return dependency.Range.Contains(PackageSemanticVersion.Parse(version)); } catch (InvalidDataException) { return false; } }

        internal static bool HasSameSourceIdentity(DiscoveredMod loaded, DiscoveredMod current)
        {
            bool sameLocation = string.Equals(loaded.Source, current.Source, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(loaded.OfficialId, current.OfficialId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    EnsureTrailingDirectorySeparator(loaded.RootPath),
                    EnsureTrailingDirectorySeparator(current.RootPath),
                    StringComparison.OrdinalIgnoreCase);
            if (!sameLocation || loaded.Classification.Identity != current.Classification.Identity)
                return false;
            if (!loaded.Classification.IsCodeMod &&
                !current.Classification.IsCodeMod)
                return true;
            return loaded.Manifest.EntryDll.Equals(current.Manifest.EntryDll, StringComparison.OrdinalIgnoreCase) &&
                loaded.Manifest.EntryType == current.Manifest.EntryType && loaded.Manifest.Version == current.Manifest.Version &&
                loaded.Classification.ExpectedHarmonyOwner == current.Classification.ExpectedHarmonyOwner;
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

            // Keep legacy cycle admission (including optional cycles); replace DFS output only.
            HashSet<string> blockedIds = dependencyCycleBlockedIds;
            // Disabled sources remain available for skip diagnostics, but cannot delay ready owners.
            var nodes = byId.Values.Where(mod => mod.OfficialEnabled && !blockedIds.Contains(mod.Manifest.UniqueID))
                .ToDictionary(mod => mod.Manifest.UniqueID, mod => new PackagePlanInput
                { Id = mod.Manifest.UniqueID, OfficialPriority = FrozenModLoadPlan.Priority(mod) }, StringComparer.OrdinalIgnoreCase);
            var remaining = new HashSet<string>(nodes.Keys, StringComparer.OrdinalIgnoreCase);
            var selected = new List<DiscoveredMod>();
            while (remaining.Count > 0)
            {
                string next = PackageLoadOrder.Next(remaining.Select(id => nodes[id]), id =>
                    !((IManifest)byId[id].Manifest).Dependencies.Any(dependency => remaining.Contains(dependency.UniqueID)))
                    ?? throw new InvalidOperationException("Legacy dependency cycle escaped preflight.");
                remaining.Remove(next);
                selected.Add(byId[next]);
            }
            selected.AddRange(ordered.Where(mod => !mod.OfficialEnabled || blockedIds.Contains(mod.Manifest.UniqueID)));
            return selected;
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

    }

}
