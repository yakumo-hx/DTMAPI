using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DTMAPI.GameBridge.DolocTown.QA
{
    internal sealed class QaHostParticipant : IQaHostParticipant, IQaHostNativeDriver, IQaHostPreRuntimeParticipant
    {
        private readonly GameBridgeFixtureAccess access;
        private readonly QaHostSettings settings;
        private readonly Batch6AutoFishingPilotCoordinator? batch6AutoFishingPilot;
        private readonly Batch6AutoFishingManagerLifecycleCoordinator? batch6AutoFishingManagerLifecycle;
        private readonly Batch5GcLadderOrchestrator batch5GcLadder;
        private readonly Batch5NoDemandProfileFixture? batch5NoDemand;
        private readonly QaScenarioController scenarios;
        private readonly CustomEntityRegistrationContractProbe customEntityProbe;
        private readonly FishRoeTooltipObservationProbe fishRoeProbe;
        private readonly bool itemDisplayNameLifecycleRequested;
        private readonly DiagnosticsSnapshotProbe diagnosticsProbe;
        private readonly NativeLoadContinuationProbe? nativeLoadContinuationProbe;
        private readonly DebugConsoleBehaviorObservation debugConsoleBehavior = new DebugConsoleBehaviorObservation();
        private readonly ExternalPlayerInputObservation? externalPlayerInput;
        private readonly QaSaveFixtureIsolation?
            saveFixtureIsolation;
        private readonly Dictionary<string, ContinuousHomePageObservation> homePageTerminals = new Dictionary<string, ContinuousHomePageObservation>(StringComparer.Ordinal);
        private bool started;
        private bool preRuntimePrepared;
        private bool closed;
        private int updates;
        private bool customEntityCompleted;
        private bool fishRoeCompleted;
        private bool diagnosticsCompleted;
        private bool saveLoadedObserved;
        private bool saveSavedObserved;
        private bool workshopReloadObserved;
        private bool titleSettingsCompleted;
        private bool managerStatusCompleted;
        private bool managerMvpCompleted;
        private bool officialModUiCompleted;
        private bool pauseMenuCompleted;
        private bool debugConsoleBehaviorCompleted;
        private string debugConsoleBehaviorDetails = string.Empty;
        private bool debugConsoleCompleted;
        private bool saveSlotsCompleted;
        private bool moreSavesPostTitlePanelCompleted;
        private bool animalCompleted;
        private bool equipmentSlotsCompleted;
        private bool audioCompleted;
        private bool audioReadinessCompleted;
        private string audioReadinessDetails = string.Empty;
        private bool hatchActionCompleted;
        private string hatchActionDetails = string.Empty;
        private bool hatchCompleted;
        private bool cameraCompleted;
        private bool contentMetadataCompleted;
        private bool nativeUiObservationCompleted;
        private bool postTitleBoundaryObserved;
        private bool continuousHomePageCompleted;
        private bool returnedToTitleG4CleanupCompleted;
        private bool inSaveG4ExecutionBlocked;
        private string returnedToTitleG4Failure = string.Empty;
        private bool returnedToTitleG4FailureRaised;
        private bool failureCloseRequested;
        private int g5WorldMutationIndex;
        private bool g5WorldMutationCompleted;
        private DateTimeOffset g5WorldMutationReadyObservedAt;
        private int g6LifecycleIndex;
        private bool g6LifecycleCompleted;
        private DateTimeOffset startedAt;
        private bool returnHomeRequested;
        private bool batch6AutoFishingReentryLoadRequested;
        private bool productOwnerRefreshRequested;
        private bool productOwnerRefreshCompleted;
        private bool productOwnerRefreshCleanupCompleted;
        private bool titleLifecycleCompleted;
        private bool advancedProductOwnerDeactivationCompleted;
        private DateTimeOffset returnHomeQuiescenceObservedAt;

        internal QaHostParticipant(GameBridgeFixtureAccess access, QaHostSettings settings)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            scenarios = new QaScenarioController(access);
            scenarios.ConfigureDebugConsoleSaveAcceptance(
                settings.DebugConsoleSaveAcceptancePhase,
                settings.ExpectedDebugConsoleMoney);
            scenarios.ConfigureMoreEquipmentSlotsNoNativeSaveColdObserver(
                settings.ExpectedMoreEquipmentSlotsBackpackBaseline,
                settings.ExpectedMoreEquipmentSlotsCommittedGeneration,
                settings.ExpectedMoreEquipmentSlotsCommittedOccupied,
                settings.ExpectedMoreEquipmentSlotsCommittedSlots);
            scenarios.ConfigureMoreEquipmentSlotsInterruptedCandidateObserver(
                settings
                    .MoreEquipmentSlotsInterruptedCandidateObservationEnabled,
                settings
                    .ExpectedMoreEquipmentSlotsCandidatePreGeneration,
                settings.ExpectedMoreEquipmentSlotsCommittedOccupied,
                settings.ExpectedMoreEquipmentSlotsCommittedSlots);
            scenarios.ConfigureMoreEquipmentSlotsTransition(
                settings.MoreEquipmentSlotsTransitionPhase);
            scenarios.ConfigureMoreEquipmentSlots100UiAcceptance(
                settings.EquipmentSlotsObservationEnabled &&
                settings.G5WorldMutationCases.Contains(
                    "MoreEquipmentSlotsNoNativeSave",
                    StringComparer.Ordinal));
            if (settings.Batch6AutoFishingPilot.Enabled)
                batch6AutoFishingPilot = new Batch6AutoFishingPilotCoordinator(access, settings.Batch6AutoFishingPilot);
            if (settings.Batch6AutoFishingManagerLifecycle.Enabled)
            {
                batch6AutoFishingManagerLifecycle = new Batch6AutoFishingManagerLifecycleCoordinator(
                    access,
                    settings.Batch6AutoFishingManagerLifecycle,
                    scenarios.RequestProductOwnerRefresh);
            }
            batch5GcLadder = new Batch5GcLadderOrchestrator(settings, access);
            if (settings.Batch5NoDemandEnabled)
            {
                batch5NoDemand = new Batch5NoDemandProfileFixture(
                    access,
                    settings.SaveSlot,
                    settings.Batch5NoDemandWarmupFrames,
                    settings.Batch5NoDemandTargetFrames);
            }
            scenarios.ConfigureBatch5GcLadder(new Batch5GcLadderFixtureOptions
            {
                Enabled = settings.Batch5GcLadderEnabled,
                Domain = settings.Batch5GcLadderDomain,
                Level = settings.Batch5GcLadderLevel,
                Workload = settings.Batch5GcLadderWorkload,
                Multiplier = settings.Batch5GcLadderMultiplier,
                TargetUnits = settings.Batch5GcLadderTargetUnits,
                MeasureSeconds = settings.Batch5GcLadderMeasureSeconds,
                SaveSlot = settings.SaveSlot
            });
            customEntityProbe = new CustomEntityRegistrationContractProbe(access);
            fishRoeProbe = new FishRoeTooltipObservationProbe(access);
            itemDisplayNameLifecycleRequested = settings.FishRoeTooltipObservationEnabled &&
                settings.G6LifecycleCases.Any(item => item.Equals("SaveLoadCycle", StringComparison.Ordinal)) &&
                settings.G6SaveLoadCycleCount >= 2;
            diagnosticsProbe = new DiagnosticsSnapshotProbe(access, settings.DiagnosticsScenario, settings.DiagnosticsExpectedFeatureIds);
            if (string.Equals(settings.G6NativeLoadContinuationProbe, "VersionPatcher", StringComparison.OrdinalIgnoreCase))
                nativeLoadContinuationProbe = new NativeLoadContinuationProbe(access);
            if (settings.ContinuousHomePageTerminalEnabled)
                homePageTerminals["terminal"] = new ContinuousHomePageObservation(settings.ContinuousHomePageSeconds);
            if (settings.ExternalPlayerInputObservationEnabled)
            {
                ValidateExternalMarkerBoundary(settings.ExternalPlayerInputEvidenceRoot, settings.ExternalPlayerInputMarkerPath);
                externalPlayerInput = new ExternalPlayerInputObservation(settings.ExternalPlayerInputToken, settings.ExternalPlayerInputMarkerPath, 1.5d);
                homePageTerminals["external-player-input"] = new ContinuousHomePageObservation(settings.ContinuousHomePageSeconds);
            }
            if (settings.RequireDisposableSaveRedirect)
            {
                saveFixtureIsolation =
                    new QaSaveFixtureIsolation(
                        access,
                        settings.SaveTestMode,
                        settings
                            .DisposableSaveFixtureSaveRoot);
            }
        }

        public string Id => "DTMAPI.QA.Protocol7.Participant";

        public void PrepareBeforeRuntimeStart()
        {
            if (closed || started || preRuntimePrepared)
                throw new InvalidOperationException("The optional QA pre-Runtime boundary may prepare only once before participant Start.");
            saveFixtureIsolation?.Install();
            preRuntimePrepared = true;
        }

        public void Start()
        {
            if (closed || started)
                throw new InvalidOperationException("The optional QA participant may start only once.");
            started = true;
            startedAt = DateTimeOffset.UtcNow;
            if (settings.RequireDisposableSaveRedirect && !preRuntimePrepared)
                throw new InvalidOperationException("The required disposable save guard was not installed before Runtime Mod loading.");
            PublishRootIsolationStatus();
            RetainedHelperAbiProbe.RunIfRequested(message => access.Log(message));
            nativeLoadContinuationProbe?.Update("Start");
            if (settings.G6LifecycleCases.Length > 0)
            {
                scenarios.PrepareG6Lifecycle(new G6FixtureOptions
                {
                    Cases = settings.G6LifecycleCases,
                    SaveSlot = settings.SaveSlot,
                    InitialDelaySeconds = settings.G6InitialDelaySeconds,
                    SaveLoadCycleCount = settings.G6SaveLoadCycleCount,
                    SaveLoadCycleInitialTitleIdleSeconds = settings.G6SaveLoadCycleInitialTitleIdleSeconds,
                    SaveLoadCycleIntervalSeconds = settings.G6SaveLoadCycleIntervalSeconds,
                    SaveLoadCycleInSaveSeconds = settings.G6SaveLoadCycleInSaveSeconds,
                    PreLoadGcProbe = settings.G6PreLoadGcProbe,
                    SaveLoadObjectSnapshotMode = settings.G6SaveLoadObjectSnapshotMode,
                    OwnerRootIsolationProfile = settings.G6OwnerRootIsolationProfile,
                    PendingPressureSeconds = settings.G6PendingPressureSeconds,
                    PendingPressureIntervalSeconds = settings.G6PendingPressureIntervalSeconds,
                    LongTitleIdleSeconds = settings.G6LongTitleIdleSeconds
                });
            }
            batch6AutoFishingPilot?.Start();
            batch6AutoFishingManagerLifecycle?.Start();
            access.Log("Optional QA host participant started runId=" + access.RunId + ".");
            PublishPendingStatuses();
        }

        public void Update()
        {
            if (!started || closed)
                throw new InvalidOperationException("The optional QA participant cannot update outside its active lifetime.");
            updates++;
            nativeLoadContinuationProbe?.Update("Update");
            batch5GcLadder.Update(scenarios.CaptureActionSpeedGcLadderProgress(DateTimeOffset.UtcNow));
            if (batch5NoDemand != null && saveLoadedObserved && !batch5NoDemand.MeasurementCompleted)
            {
                batch5NoDemand.Update();
                return;
            }
            if (itemDisplayNameLifecycleRequested && !fishRoeProbe.LifecycleComplete)
                fishRoeProbe.UpdateLifecycle();

            if (settings.ProductOwnerRefreshEnabled && !productOwnerRefreshRequested)
            {
                if (DateTimeOffset.UtcNow - startedAt < TimeSpan.FromSeconds(Math.Max(1, settings.G6InitialDelaySeconds)))
                    return;
                G4FixtureStepResult refresh = scenarios.RequestProductOwnerRefresh();
                productOwnerRefreshRequested = true;
                if (!refresh.Completed || !refresh.Succeeded)
                {
                    access.PublishG4Status("Smoke.ProductOwnerRefresh", "failed", "optional QA official Mod refresh", refresh.Details + "; owner=qa; fallback=false");
                    throw new InvalidOperationException(refresh.Details);
                }
                if (workshopReloadObserved)
                {
                    productOwnerRefreshCompleted = true;
                    access.PublishG4Status("Smoke.ProductOwnerRefresh", "verified", "official ModManager.ReloadMods -> Runtime dispatch -> optional participant", refresh.Details + "; owner=qa; fallback=false");
                }
                else
                {
                    access.PublishG4Status("Smoke.ProductOwnerRefresh", "pending", "optional QA official Mod refresh", refresh.Details + "; waitingForWorkshopReceipt=true; owner=qa; fallback=false");
                }
                return;
            }
            if (settings.ProductOwnerRefreshEnabled && !productOwnerRefreshCompleted)
            {
                access.PublishG4Status("Smoke.ProductOwnerRefresh", "pending", "optional QA official Mod refresh", "Waiting for the ordered Workshop capture and Runtime dispatch receipt; owner=qa; fallback=false");
                return;
            }

            if (batch6AutoFishingPilot?.ShouldRequestReentryLoad == true)
            {
                G4FixtureStepResult wait = batch6AutoFishingPilot.Advance();
                if (wait.Completed && !wait.Succeeded)
                    throw new InvalidOperationException(wait.Details);
                if (!batch6AutoFishingReentryLoadRequested)
                {
                    batch6AutoFishingReentryLoadRequested = scenarios.TryRequestInitialSaveLoad(settings.SaveSlot);
                    if (batch6AutoFishingReentryLoadRequested)
                        access.Log("Batch6 AutoFishing L5 QA lifecycle requested the second authoritative save-slot load through the existing official save-load coordinator.");
                }
                if (batch6AutoFishingReentryLoadRequested && batch6AutoFishingPilot.ShouldRequestReentryLoad)
                    scenarios.ContinueInitialSaveLoad();
                return;
            }

            // The bounded product lifecycle acceptances combine in-save observers
            // with the existing SaveLoadCycle. Let SaveLoadCycle drive the initial
            // load, then pause it while G5 performs its in-save ProductNative work
            // so it cannot request ReturnHome first.
            if (itemDisplayNameLifecycleRequested && settings.AnimalObservationEnabled &&
                saveLoadedObserved && !animalCompleted)
            {
                RunG4Step("Smoke.AnimalViewerRendering", "optional QA read-only animal observer", scenarios.ObserveAnimals, ref animalCompleted);
                return;
            }

            bool saveLoadCycleMayDriveInitialLoad =
                !saveLoadedObserved &&
                !g6LifecycleCompleted &&
                g6LifecycleIndex < settings.G6LifecycleCases.Length &&
                string.Equals(settings.G6LifecycleCases[g6LifecycleIndex], "SaveLoadCycle", StringComparison.Ordinal);
            if (!g6LifecycleCompleted && settings.G6LifecycleCases.Length > 0 &&
                (settings.G5WorldMutationCases.Length == 0 || g5WorldMutationCompleted || saveLoadCycleMayDriveInitialLoad))
            {
                string caseId = settings.G6LifecycleCases[g6LifecycleIndex];
                G4FixtureStepResult result;
                if (string.Equals(caseId, Batch6AutoFishingPilotSettings.CaseId, StringComparison.Ordinal))
                    result = batch6AutoFishingPilot?.Advance() ?? G4FixtureStepResult.Failed("Batch6AutoFishingPilot case has no product QA coordinator.");
                else if (string.Equals(caseId, Batch6AutoFishingManagerLifecycleSettings.CaseId, StringComparison.Ordinal))
                    result = batch6AutoFishingManagerLifecycle?.Advance() ?? G4FixtureStepResult.Failed("Batch6AutoFishingManagerLifecycle case has no product QA coordinator.");
                else
                    result = scenarios.AdvanceG6Lifecycle(caseId);
                if (!result.Completed)
                {
                    access.PublishG6Status("pending", result.Details + "; completed=" + g6LifecycleIndex + "/" + settings.G6LifecycleCases.Length + "; owner=qa");
                    return;
                }
                if (!result.Succeeded)
                {
                    access.PublishG6Status("failed", result.Details + "; failedCase=" + caseId + "; owner=qa");
                    throw new InvalidOperationException(result.Details);
                }
                g6LifecycleIndex++;
                g6LifecycleCompleted = g6LifecycleIndex >= settings.G6LifecycleCases.Length;
                access.PublishG6Status(
                    g6LifecycleCompleted ? "verified" : "pending",
                    result.Details + "; completed=" + g6LifecycleIndex + "/" + settings.G6LifecycleCases.Length + "; cases=" + string.Join("|", settings.G6LifecycleCases) + "; owner=qa");
                access.Log("G6 lifecycle case terminal " + result.Details + ".");
                return;
            }

            if (settings.TitleSettingsUiEnabled && !titleSettingsCompleted)
            {
                RunG4Step("Smoke.TitleSettingsMenu", "optional QA title settings UI driver", scenarios.ObserveTitleSettingsUi, ref titleSettingsCompleted);
                return;
            }
            if (settings.ManagerStatusUiEnabled && !managerStatusCompleted)
            {
                RunG4Step("Smoke.TitleSettingsMenu", "optional QA Manager Status title UI driver", scenarios.ObserveManagerStatusUi, ref managerStatusCompleted);
                return;
            }
            if (settings.ManagerMvpUiEnabled && !managerMvpCompleted)
            {
                RunG4Step("Smoke.TitleSettingsMenu", "optional QA Manager MVP title UI driver", scenarios.ObserveManagerMvpUi, ref managerMvpCompleted);
                return;
            }
            if (settings.OfficialModUiEnabled && !officialModUiCompleted)
            {
                RunG4Step("Smoke.OfficialModUi", "optional QA official Mod UI driver", scenarios.ObserveOfficialModUi, ref officialModUiCompleted);
                return;
            }
            if (!postTitleBoundaryObserved && settings.SaveSlotsPagingEnabled && !saveSlotsCompleted && !saveLoadedObserved)
            {
                G4FixtureStepResult saveResult = scenarios.AdvanceSaveSlotScenario(settings.SaveSlot);
                if (saveResult.Completed && !saveResult.Succeeded)
                    throw new InvalidOperationException(saveResult.Details);
                access.PublishG4Status("Smoke.MoreSavesOfficialSaveUi", "pending", "optional QA save-slot coordinator", saveResult.Details + "; owner=qa; fallback=false");
                return;
            }

            if (saveLoadedObserved && !postTitleBoundaryObserved)
            {
                if (settings.ExternalPlayerInputObservationEnabled && externalPlayerInput != null && !externalPlayerInput.Ready)
                {
                    if (externalPlayerInput.ObserveReady(scenarios.IsNormalGameplay(), DateTimeOffset.UtcNow))
                    {
                        access.PublishG4Status("Smoke.ExternalPlayerInput", "ready", "existing Runtime input-context sampler", "READY token=" + settings.ExternalPlayerInputToken + "; owner=qa; newHook=false; newSampler=false");
                        access.Log("Smoke external player input READY token=" + settings.ExternalPlayerInputToken + ".");
                    }
                    else
                        access.PublishG4Status("Smoke.ExternalPlayerInput", "pending", "existing Runtime input-context sampler", "Waiting for continuous normal Gameplay; interruptions reset the observation.");
                    return;
                }
                if (settings.PauseMenuLayoutEnabled && !pauseMenuCompleted)
                {
                    RunG4Step("Smoke.PauseMenuLayout", "optional QA pause-menu observer", scenarios.ObservePauseMenuLayout, ref pauseMenuCompleted);
                    return;
                }
                if (settings.DebugConsoleEnabled && !debugConsoleCompleted)
                {
                    RunDebugConsoleObservation();
                    return;
                }
                if (settings.AnimalObservationEnabled && !animalCompleted)
                {
                    RunG4Step("Smoke.AnimalViewerRendering", "optional QA read-only animal observer", scenarios.ObserveAnimals, ref animalCompleted);
                    return;
                }
                if (settings.EquipmentSlotsObservationEnabled && !equipmentSlotsCompleted)
                {
                    RunG4Step("Smoke.EquipmentSlotsUiObservation", "real player B input + product-neutral state + QA-owned evidence", scenarios.ObserveEquipmentSlotsUi, ref equipmentSlotsCompleted);
                    return;
                }
                if (settings.AudioObservationEnabled && !audioCompleted)
                {
                    RunAudioReplacementObservation();
                    return;
                }
                if (settings.HatchVoiceEnabled && !hatchCompleted)
                {
                    RunHatchVoiceObservation();
                    return;
                }
                if (settings.CameraPlayableEnabled && !cameraCompleted)
                {
                    RunG4Step("Smoke.CameraPlayable", "optional QA receipt-bound CameraView arbitration", scenarios.ExerciseCameraPlayable, ref cameraCompleted);
                    if (cameraCompleted)
                        access.PublishG4Status("Smoke.Zoom", "verified", "G4 CameraPlayable compatibility status", "CameraPlayable is a Frozen-ABI compatibility fixture and is not current Zoom ProductNative acceptance evidence.");
                    return;
                }
                if (settings.ContentMetadataObservationEnabled && !contentMetadataCompleted)
                {
                    RunG4Step("Smoke.ContentMetadataObservation", "optional QA read-only content metadata observer", () => scenarios.ObserveContentMetadata(settings.ContentMetadataExpectOilAbsent), ref contentMetadataCompleted);
                    return;
                }
                if (!g5WorldMutationCompleted && settings.G5WorldMutationCases.Length > 0)
                {
                    if (!QaScenarioController.HasContinuousStableObservationForFixture(
                            scenarios.IsG5WorldMutationReady(),
                            DateTimeOffset.UtcNow,
                            1d,
                            ref g5WorldMutationReadyObservedAt))
                    {
                        access.PublishG5Status(
                            "pending",
                            "Waiting for continuous native world readiness: Gameplay + archive/currentRoom/RoomInfo/agent; completed=" +
                            g5WorldMutationIndex + "/" +
                            settings.G5WorldMutationCases.Length +
                            "; owner=qa; fallback=false");
                        return;
                    }
                    string caseId = settings.G5WorldMutationCases[g5WorldMutationIndex];
                    G4FixtureStepResult result = scenarios.AdvanceG5WorldMutation(caseId);
                    if (!result.Completed)
                    {
                        access.PublishG5Status("pending", result.Details + "; completed=" + g5WorldMutationIndex + "/" + settings.G5WorldMutationCases.Length + "; owner=qa");
                        return;
                    }
                    if (!result.Succeeded)
                    {
                        access.PublishG5Status("failed", result.Details + "; failedCase=" + caseId + "; owner=qa");
                        throw new InvalidOperationException(result.Details);
                    }
                    g5WorldMutationIndex++;
                    g5WorldMutationCompleted = g5WorldMutationIndex >= settings.G5WorldMutationCases.Length;
                    access.PublishG5Status(
                        g5WorldMutationCompleted ? "verified" : "pending",
                        result.Details + "; completed=" + g5WorldMutationIndex + "/" + settings.G5WorldMutationCases.Length + "; cases=" + string.Join("|", settings.G5WorldMutationCases) + "; owner=qa");
                    access.Log("G5 world-mutation case terminal " + result.Details + ".");
                    return;
                }
                // The runner writes the external terminal marker only after its real
                // input matrix has completed. Observe it after all QA-owned in-save
                // behavior so marker waiting cannot deadlock DebugConsole evidence.
                if (settings.ExternalPlayerInputObservationEnabled && externalPlayerInput != null && !externalPlayerInput.MarkerObserved)
                {
                    if (externalPlayerInput.ObserveMarker())
                    {
                        if (!externalPlayerInput.MarkerPassed)
                        {
                            string failed = "Runner terminal marker reported Passed=False for token=" + settings.ExternalPlayerInputToken + "; owner=qa; fallback=false";
                            access.PublishG4Status("Smoke.ExternalPlayerInputMarker", "failed", "runner terminal marker read-only observation", failed);
                            throw new InvalidOperationException(failed);
                        }
                        access.PublishG4Status("Smoke.ExternalPlayerInputMarker", "verified", "runner terminal marker read-only observation", "token=" + settings.ExternalPlayerInputToken + "; passed=" + externalPlayerInput.MarkerPassed.ToString().ToLowerInvariant() + "; owner=qa");
                        access.Log("Smoke external player input marker token=" + settings.ExternalPlayerInputToken + "; passed=" + externalPlayerInput.MarkerPassed.ToString().ToLowerInvariant() + ".");
                    }
                    else
                        access.PublishG4Status("Smoke.ExternalPlayerInputMarker", "pending", "runner terminal marker read-only observation", "Waiting for matching terminal marker after QA-owned in-save behavior completed.");
                    return;
                }
            }

            bool needsNativeUiObservation = HasTitleUiRoute() || settings.OfficialModUiEnabled || settings.PauseMenuLayoutEnabled;
            if (needsNativeUiObservation && !nativeUiObservationCompleted &&
                (!settings.PauseMenuLayoutEnabled || pauseMenuCompleted))
            {
                RunG4Step("Smoke.NativeUiLayoutObservation", "optional QA observer over production-owned repair", scenarios.ObserveNativeUiLayout, ref nativeUiObservationCompleted);
                return;
            }

            if (postTitleBoundaryObserved && homePageTerminals.Count > 0 && !continuousHomePageCompleted)
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                bool all = true;
                foreach (ContinuousHomePageObservation observation in homePageTerminals.Values)
                    all &= observation.Observe(access.InputContext, now);
                if (all)
                {
                    continuousHomePageCompleted = true;
                    access.PublishG4Status("Smoke.QaContinuousHomePage", "verified", "continuous HomePage terminal arbitration", "cases=" + string.Join("|", homePageTerminals.Keys) + "; durationSeconds=" + settings.ContinuousHomePageSeconds + "; interruptedObservationsReset=true");
                    if (settings.ExternalPlayerInputObservationEnabled && externalPlayerInput != null && externalPlayerInput.MarkerPassed)
                    {
                        access.PublishG4Status("Smoke.ExternalPlayerInputCleanup", "verified", "QA marker -> participant ReturnHome disposition -> QA HomePage observation", "token=" + settings.ExternalPlayerInputToken + "; terminalPassed=true; continuousHomePage=true; owner=qa-observation");
                        access.Log("Smoke exercise ExternalPlayerInputCleanup OK token=" + settings.ExternalPlayerInputToken + "; continuousHomePage=true; owner=qa-observation.");
                    }
                    if (settings.ProductOwnerRefreshEnabled && !productOwnerRefreshCleanupCompleted)
                    {
                        productOwnerRefreshCleanupCompleted = true;
                        access.PublishG4Status("Smoke.ProductOwnerRefreshCleanup", "verified", "Workshop refresh -> SaveLoaded -> ReturnedToTitle -> continuous HomePage", "refreshObserved=true; titleCleanup=true; owner=qa; fallback=false");
                        access.Log("Smoke exercise ProductOwnerRefreshCleanup OK refreshObserved=true; titleCleanup=true; owner=qa; fallback=false.");
                    }
                    if (settings.TitleLifecycleEnabled && !titleLifecycleCompleted)
                    {
                        titleLifecycleCompleted = true;
                        access.PublishG4Status("Smoke.TitleButtonLifecycle", "verified", "SaveLoaded -> ReturnedToTitle -> continuous HomePage", "owner=qa; fallback=false");
                        access.Log("Smoke exercise TitleButtonLifecycle OK owner=qa; fallback=false.");
                    }
                }
            }

            if (RequiresMoreSavesPostTitlePanelReceipt() && postTitleBoundaryObserved &&
                continuousHomePageCompleted && !moreSavesPostTitlePanelCompleted)
            {
                RunG4Step(
                    "Smoke.MoreSavesPostTitleOfficialSaveUi",
                    "optional QA post-title official save-panel observer",
                    scenarios.ObservePostTitleMoreSavesOfficialSaveUi,
                    ref moreSavesPostTitlePanelCompleted);
                return;
            }

            if (postTitleBoundaryObserved && !string.IsNullOrWhiteSpace(returnedToTitleG4Failure) &&
                (homePageTerminals.Count == 0 || continuousHomePageCompleted))
            {
                RaiseReturnedToTitleG4Failure();
            }

            if (RequiresStrongPlantingGunReentry() && postTitleBoundaryObserved &&
                continuousHomePageCompleted && !advancedProductOwnerDeactivationCompleted)
            {
                G4FixtureStepResult reentry =
                    scenarios.AdvanceStrongPlantingGunReentryLoad(
                        settings.SaveSlot);
                if (!reentry.Completed)
                {
                    access.PublishG4Status("Smoke.AdvancedProductOwnerDeactivation", "pending", "StrongPlantingGun native-save re-entry before Core Loader owner deactivation", reentry.Details + "; owner=qa; fallback=false");
                    return;
                }
                if (!reentry.Succeeded)
                {
                    access.PublishG4Status("Smoke.AdvancedProductOwnerDeactivation", "failed", "StrongPlantingGun native-save re-entry before Core Loader owner deactivation", reentry.Details + "; owner=qa; fallback=false");
                    throw new InvalidOperationException(reentry.Details);
                }
            }

            if (settings.AdvancedProductOwnerDeactivationEnabled && postTitleBoundaryObserved &&
                continuousHomePageCompleted && !advancedProductOwnerDeactivationCompleted)
            {
                G4FixtureStepResult deactivation = scenarios.DeactivateAdvancedProductOwnersForFixture(
                    settings.AdvancedProductOwnerDeactivationOwnerIds);
                if (!deactivation.Completed)
                {
                    access.PublishG4Status("Smoke.AdvancedProductOwnerDeactivation", "pending", "Core Loader final owner-deactivation path", deactivation.Details + "; owner=qa; fallback=false");
                    return;
                }
                if (!deactivation.Succeeded)
                {
                    access.PublishG4Status("Smoke.AdvancedProductOwnerDeactivation", "failed", "Core Loader final owner-deactivation path", deactivation.Details + "; owner=qa; fallback=false");
                    throw new InvalidOperationException(deactivation.Details);
                }
                advancedProductOwnerDeactivationCompleted = true;
                access.PublishG4Status("Smoke.AdvancedProductOwnerDeactivation", "verified", "Core Loader owner deactivation -> product IDisposable -> exact Harmony owner cleanup", deactivation.Details + "; owner=qa; fallback=false");
                return;
            }
        }

        public void OnSaveLoaded(int? slot, bool isNewGame)
        {
            if (!started || closed)
                return;
            access.Log("Optional QA host participant observed SaveLoaded slot=" + (slot?.ToString() ?? "unknown") + "; isNewGame=" + isNewGame.ToString().ToLowerInvariant() + ".");
            if (postTitleBoundaryObserved && inSaveG4ExecutionBlocked)
            {
                access.Log("Optional QA host participant ignored late SaveLoaded after the terminal title boundary; no in-save G4 case was resumed.");
                return;
            }
            if (settings.SaveSlotsPagingEnabled)
            {
                int expectedSlot = settings.SaveSlot - 1;
                if (!slot.HasValue || slot.Value != expectedSlot)
                {
                    string details = "Wrong SaveLoaded slot after QA-owned save coordination. expectedIndex=" + expectedSlot + "; actualIndex=" + (slot?.ToString() ?? "unknown") + "; owner=qa; fallback=false";
                    access.PublishG4Status("Smoke.MoreSavesOfficialSaveUi", "failed", "optional QA save-slot coordinator", details);
                    throw new InvalidOperationException(details);
                }
            }
            saveLoadedObserved = true;
            batch5GcLadder.OnSaveLoaded();
            batch6AutoFishingPilot?.OnSaveLoaded(slot, isNewGame);
            batch6AutoFishingManagerLifecycle?.OnSaveLoaded(slot, isNewGame);
            if (settings.G6LifecycleCases.Length > 0)
                scenarios.NotifyG6SaveLoaded(slot, isNewGame);
            if (settings.SaveSlotsPagingEnabled && !saveSlotsCompleted)
            {
                saveSlotsCompleted = true;
                access.PublishG4Status("Smoke.MoreSavesOfficialSaveUi", "verified", "optional QA save-slot coordinator", "SaveLoaded observed for expectedIndex=" + (settings.SaveSlot - 1) + " after ordered official panel paging and requested-slot continuation; owner=qa; fallback=false");
            }
            if (settings.ObserveSaveLoaded)
            {
                access.PublishG3Status(
                    "Smoke.QaLifecycle.SaveLoaded",
                    "verified",
                    "DtmApiRuntime.SaveSessionLoaded -> optional QA participant",
                    "slot=" + (slot?.ToString() ?? "unknown") + "; isNewGame=" + isNewGame.ToString().ToLowerInvariant() + "; owner=qa; fallback=false");
            }

            if (settings.CustomEntityContractEnabled && !customEntityCompleted)
                customEntityCompleted = RunG3Probe("Smoke.CustomEntityApis", "owner-bound in-memory registration session", customEntityProbe.Run);
            if (settings.FishRoeTooltipObservationEnabled &&
                (!fishRoeCompleted || (itemDisplayNameLifecycleRequested && fishRoeProbe.ShouldRunAfterSaveLoaded)))
            {
                bool completed = RunG3Probe("Smoke.FishRoeTooltip", "transient ItemFishRoe tooltip observation", fishRoeProbe.Run);
                fishRoeCompleted = fishRoeCompleted || completed;
            }
            if (settings.DiagnosticsSnapshotEnabled && !diagnosticsCompleted)
                diagnosticsCompleted = RunG3Probe("Smoke.DiagnosticsSnapshot", "IDtmDiagnosticsApi.GetSnapshot + export", diagnosticsProbe.Run);
        }

        public bool TryRequestInitialSaveLoad(int saveSlot) => scenarios.TryRequestInitialSaveLoad(saveSlot);

        public void ContinueInitialSaveLoad() => scenarios.ContinueInitialSaveLoad();

        public void OnUiObservation(string source) => scenarios.NotifyUiObservation(source);

        public QaHostRunDisposition GetRunDisposition()
        {
            if (!started || closed)
                return QaHostRunDisposition.Continue;

            if (ShouldRequestInitialSaveLoad())
                return QaHostRunDisposition.RequestInitialSaveLoad;

            if (!returnHomeRequested && ShouldRequestReturnHome())
            {
                returnHomeRequested = true;
                return QaHostRunDisposition.RequestReturnHome;
            }

            return AllRequirementsComplete() && !settings.WaitForManualExit
                ? QaHostRunDisposition.RequestQuit
                : QaHostRunDisposition.Continue;
        }

        public void OnSaveSaved(int? slot)
        {
            if (!started || closed)
                return;
            saveSavedObserved = true;
            scenarios.NotifyMoreEquipmentSlotsTransitionSaveSaved();
            scenarios.NotifyG6SaveSaved(slot);
            if (settings.ObserveSaveSaved)
            {
                access.PublishG3Status(
                    "Smoke.QaLifecycle.SaveSaved",
                    "verified",
                    "SaveGame Postfix after Runtime notification and production persistence",
                    "slot=" + (slot?.ToString() ?? "unknown") + "; owner=qa; fallback=false");
            }
        }

        public void OnWorkshopReloadCompleted()
        {
            if (!started || closed)
                return;
            workshopReloadObserved = true;
            batch6AutoFishingManagerLifecycle?.OnWorkshopReloadCompleted();
            if (settings.ProductOwnerRefreshEnabled)
            {
                productOwnerRefreshCompleted = true;
                access.PublishG4Status(
                    "Smoke.ProductOwnerRefresh",
                    "verified",
                    "ModManager.ReloadMods Postfix after Runtime notification",
                    "orderedWorkshopReceipt=true; owner=qa; fallback=false");
                access.Log("Smoke auto-reload completion observed after Workshop ModListChanged dispatch.");
            }
            if (settings.AdvancedProductOwnerDeactivationEnabled)
                access.PublishG4Status("Smoke.AdvancedProductOwnerDeactivation", "pending", "Core Loader final owner-deactivation path", "Waiting for all in-save product behavior, ReturnedToTitle, and continuous HomePage before disabling ActionSpeed and OneActionComplete.");
            if (settings.ObserveWorkshopReloadCompleted)
            {
                access.PublishG3Status(
                    "Smoke.QaLifecycle.WorkshopReloadCompleted",
                    "verified",
                    "ModManager.ReloadMods Postfix after Runtime notification",
                    "owner=qa; fallback=false");
            }
        }

        public QaHostBoundaryDisposition OnReturnedToTitle(bool hasObservedSaveLoaded)
        {
            // Runtime can publish an initial title boundary before the requested
            // save has ever loaded. It is not a return from an in-save fixture and
            // must not arm cleanup, post-title observation, or permanent blocking.
            if (!hasObservedSaveLoaded && RequiresSaveLoadedBoundary())
                return QaHostBoundaryDisposition.Continue;
            scenarios.OnReturnedToTitle();
            batch5GcLadder.OnReturnedToTitle();
            if (batch5NoDemand != null && !batch5NoDemand.CompleteTitleCleanup())
                throw new InvalidOperationException("Batch 5 no-demand profile did not reach a successful terminal title-cleanup state.");
            batch6AutoFishingPilot?.OnReturnedToTitle();
            if (itemDisplayNameLifecycleRequested)
                fishRoeProbe.OnReturnedToTitle();
            postTitleBoundaryObserved = true;
            if (!returnedToTitleG4CleanupCompleted && HasG4Requirements())
            {
                string[] incompleteInSave = GetIncompleteInSaveG4Requirements();
                string cleanup = scenarios.CloseG4Fixtures();
                returnedToTitleG4CleanupCompleted = true;
                access.Log("G4 ReturnedToTitle cleanup receipt " + cleanup + ".");
                if (incompleteInSave.Length > 0)
                {
                    inSaveG4ExecutionBlocked = true;
                    returnedToTitleG4Failure = "ReturnedToTitle interrupted incomplete requires-save G4 cases after verified receipt cleanup: " + string.Join(",", incompleteInSave) + ". In-save execution is permanently blocked for this participant.";
                    PublishReturnedToTitleG4Failures(incompleteInSave, returnedToTitleG4Failure);
                }
            }
            string[] incompleteG5 = GetIncompleteG5Requirements();
            if (incompleteG5.Length > 0)
            {
                string details = "ReturnedToTitle interrupted incomplete G5 world-mutation cases: " + string.Join(",", incompleteG5) + ". The participant is failed closed; runner restoration remains blocked until process exit.";
                access.PublishG5Status("failed", details + "; owner=qa; fallback=false");
                throw new InvalidOperationException(details);
            }
            if (GetIncompleteG6Requirements().Length > 0)
                return QaHostBoundaryDisposition.Continue;
            if (!string.IsNullOrWhiteSpace(returnedToTitleG4Failure))
            {
                if (homePageTerminals.Count > 0 && !continuousHomePageCompleted)
                    return QaHostBoundaryDisposition.Continue;
                RaiseReturnedToTitleG4Failure();
            }
            if (GetIncompleteG3Requirements().Length > 0)
                return QaHostBoundaryDisposition.Continue;
            if (GetIncompleteG4Requirements().Length > 0)
                return QaHostBoundaryDisposition.Continue;
            if (settings.G5WorldMutationCases.Length > 0 && !g5WorldMutationCompleted)
                return QaHostBoundaryDisposition.Continue;
            if (settings.G6LifecycleCases.Length > 0 && !g6LifecycleCompleted)
                return QaHostBoundaryDisposition.Continue;
            if (batch5GcLadder.Enabled && !batch5GcLadder.TerminalSucceeded)
                return QaHostBoundaryDisposition.Continue;
            return QaHostBoundaryDisposition.Close;
        }

        public void Close(string reason)
        {
            if (closed)
                return;
            string closeReason = reason ?? string.Empty;
            if (closeReason.StartsWith("failure:", StringComparison.Ordinal))
                failureCloseRequested = true;
            bool failureClose = failureCloseRequested;
            var closeFailures = new List<Exception>();
            bool nativeProbeClosed = nativeLoadContinuationProbe?.Close(closeReason) ?? true;
            if (!nativeProbeClosed)
                closeFailures.Add(new InvalidOperationException("The optional QA native continuation probe did not release its Harmony owner."));
            if (saveFixtureIsolation != null)
            {
                RunCloseStep(
                    closeFailures,
                    "QaSaveFixtureIsolation.Close",
                    () => saveFixtureIsolation.Close(
                        closeReason));
            }
            RunCloseStep(closeFailures, "QaScenarioController.Close", () => scenarios.Close(closeReason));
            RunCloseStep(closeFailures, "Batch5GcLadder.Close", () => batch5GcLadder.Close(closeReason));
            if (batch5NoDemand != null)
                RunCloseStep(closeFailures, "Batch5NoDemand.Close", () => batch5NoDemand.Close(closeReason));
            if (batch6AutoFishingPilot != null)
                RunCloseStep(closeFailures, "Batch6AutoFishingPilot.Close", () => batch6AutoFishingPilot.Close(closeReason));
            if (batch6AutoFishingManagerLifecycle != null)
                RunCloseStep(closeFailures, "Batch6AutoFishingManagerLifecycle.Close", () => batch6AutoFishingManagerLifecycle.Close(closeReason));
            if (HasG4Requirements())
                RunCloseStep(closeFailures, "G4Fixtures.Close", () => access.Log("G4 fixture close receipt " + scenarios.CloseG4Fixtures() + "."));
            if (!string.IsNullOrWhiteSpace(returnedToTitleG4Failure) && !returnedToTitleG4FailureRaised)
            {
                returnedToTitleG4FailureRaised = true;
                if (!failureClose)
                    closeFailures.Add(new InvalidOperationException(returnedToTitleG4Failure));
            }
            string[] incomplete = GetIncompleteG3Requirements();
            if (incomplete.Length > 0 && !failureClose)
                closeFailures.Add(new InvalidOperationException("The G3 QA participant closed before completing: " + string.Join(",", incomplete) + "."));
            string[] incompleteG4 = GetIncompleteG4RequirementsForClose();
            if (incompleteG4.Length > 0 && !failureClose)
                closeFailures.Add(new InvalidOperationException("The G4 QA participant closed before completing: " + string.Join(",", incompleteG4) + "."));
            string[] incompleteG5 = GetIncompleteG5Requirements();
            if (incompleteG5.Length > 0 && !failureClose)
                closeFailures.Add(new InvalidOperationException("The G5 QA participant closed before completing: " + string.Join(",", incompleteG5) + "."));
            string[] incompleteG6 = GetIncompleteG6Requirements();
            if (incompleteG6.Length > 0 && !failureClose)
                closeFailures.Add(new InvalidOperationException("The G6 QA participant closed before completing: " + string.Join(",", incompleteG6) + "."));
            if (closeFailures.Count == 1)
                throw closeFailures[0];
            if (closeFailures.Count > 1)
                throw new AggregateException("Optional QA host participant close did not complete; the host may retry cleanup.", closeFailures);
            closed = true;
            if (failureClose && (incomplete.Length > 0 || incompleteG4.Length > 0 || incompleteG5.Length > 0 || incompleteG6.Length > 0 || !string.IsNullOrWhiteSpace(returnedToTitleG4Failure)))
                access.Log("Optional QA host failure cleanup completed without reclassifying incomplete scenario terminals as cleanup failure. g3=" + string.Join(",", incomplete) + "; g4=" + string.Join(",", incompleteG4) + "; g5=" + string.Join(",", incompleteG5) + "; g6=" + string.Join(",", incompleteG6) + ".");
            access.Log("Optional QA host participant closed runId=" + access.RunId + "; updates=" + updates + "; reason=" + closeReason + ".");
        }

        private static void RunCloseStep(List<Exception> failures, string operation, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failures.Add(new InvalidOperationException(operation + " failed.", ex));
            }
        }

        private void PublishRootIsolationStatus()
        {
            if (!string.Equals(settings.G6RootIsolationProfile, "UiRuntime", StringComparison.OrdinalIgnoreCase))
                return;

            const string details =
                "SmokeRootIsolationProfile=UiRuntime; " +
                "SmokeRootIsolation.Disabled=SaveSlots|EquipmentSlots|Camera; " +
                "SmokeRootIsolation.Unsupported=DebugConsoleHost; " +
                "SmokeRootIsolation.Active=AllExceptUiRuntimeDisabledAndUnsupported; " +
                "operation=QaHostParticipant.Start; owner=qa";
            access.Log(details + ".");
            access.SetHookStatus("Smoke.RootIsolation", "active", "optional QA G6 activation", details);
            access.SetHookStatus("Player.EquipmentSlotsApi", "smoke-isolated", "optional QA UiRuntime profile", "EquipmentSlots registration, native hooks, and UI hooks were suppressed by the explicit optional-host startup receipt; owner=qa.");
            access.SetHookStatus("Player.EquipmentSlotsShield", "smoke-isolated", "optional QA UiRuntime profile", "EquipmentSlots shield hook was suppressed by the explicit optional-host startup receipt; owner=qa.");
        }

        private void PublishPendingStatuses()
        {
            if (settings.CustomEntityContractEnabled)
                access.PublishG3Status("Smoke.CustomEntityApis", "pending", "optional QA owner-bound registration session", "Waiting for SaveLoaded; runtime verbs are unavailable through the QA facade.");
            if (settings.FishRoeTooltipObservationEnabled)
                access.PublishG3Status("Smoke.FishRoeTooltip", "pending", "optional QA transient tooltip observation", "Waiting for SaveLoaded; no world object will be created or retained.");
            if (itemDisplayNameLifecycleRequested)
                access.PublishG3Status("Smoke.ItemDisplayNameLifecycle", "pending", "existing Fish/Animal product and SaveLoadCycle observers", "Waiting for two product queries and two real DolocAPI.SetEnvCamera cache-clear boundaries; owner=qa; fallback=false.");
            if (settings.DiagnosticsSnapshotEnabled)
                access.PublishG3Status("Smoke.DiagnosticsSnapshot", "pending", "optional QA diagnostics snapshot/export", "Waiting for SaveLoaded.");
            if (settings.ObserveSaveLoaded)
                access.PublishG3Status("Smoke.QaLifecycle.SaveLoaded", "pending", "optional QA lifecycle observation", "Waiting for Runtime SaveSessionLoaded.");
            if (settings.ObserveSaveSaved)
                access.PublishG3Status("Smoke.QaLifecycle.SaveSaved", "pending", "optional QA lifecycle observation", "Waiting for SaveGame Postfix after production persistence.");
            if (settings.ObserveWorkshopReloadCompleted)
                access.PublishG3Status("Smoke.QaLifecycle.WorkshopReloadCompleted", "pending", "optional QA lifecycle observation", "Waiting for Workshop reload completion after Runtime dispatch.");
            if (settings.TitleSettingsUiEnabled)
                access.PublishG4Status("Smoke.TitleSettingsMenu", "pending", "optional QA title settings UI driver", "Waiting for HomePage; owner=qa; fallback=false.");
            if (settings.ManagerStatusUiEnabled)
                access.PublishG4Status("Smoke.TitleSettingsMenu", "pending", "optional QA Manager Status title UI driver", "Waiting for HomePage; owner=qa; fallback=false.");
            if (settings.ManagerMvpUiEnabled)
                access.PublishG4Status("Smoke.TitleSettingsMenu", "pending", "optional QA Manager MVP title UI driver", "Waiting for HomePage; owner=qa; fallback=false.");
            if (settings.OfficialModUiEnabled)
                access.PublishG4Status("Smoke.OfficialModUi", "pending", "optional QA official Mod UI driver", "Waiting for HomePage; owner=qa; fallback=false.");
            if (settings.PauseMenuLayoutEnabled)
                access.PublishG4Status("Smoke.PauseMenuLayout", "pending", "optional QA pause-menu observer", "Waiting for SaveLoaded; production repair remains active.");
            if (settings.DebugConsoleEnabled)
            {
                access.PublishG4Status("Smoke.DebugConsoleHotkey", "pending", "optional QA real-input observer", "Waiting for SaveLoaded and the runner Y/Escape/short-tap/held-Y matrix; synthetic input and mouse give are excluded.");
                access.PublishG4Status("Smoke.DebugConsoleUiEvidence", "pending", "optional QA read-only screenshot observer", "The QA host only observes the real product-owned Y-key console and never opens, closes, or claims it.");
            }
            if (settings.SaveSlotsPagingEnabled)
                access.PublishG4Status("Smoke.MoreSavesOfficialSaveUi", "pending", "optional QA panel paging observer", "Waiting for the participant-owned save-load coordinator to expose the official panel.");
            if (RequiresMoreSavesPostTitlePanelReceipt())
                access.PublishG4Status("Smoke.MoreSavesPostTitleOfficialSaveUi", "pending", "optional QA post-title official save-panel observer", "Waiting for SaveLoaded, ReturnedToTitle and continuous HomePage before reopening the official panel.");
            if (settings.AnimalObservationEnabled)
                access.PublishG4Status("Smoke.AnimalViewerRendering", "pending", "optional QA read-only animal observer", "Waiting for SaveLoaded; no husbandry seed is permitted.");
            if (settings.EquipmentSlotsObservationEnabled)
                access.PublishG4Status("Smoke.EquipmentSlotsUiObservation", "pending", "optional QA read-only equipment UI observer", "Waiting for SaveLoaded and runner real B input; QA does not open, close, render, or mutate the product UI.");
            if (settings.AudioObservationEnabled)
                access.PublishG4Status("Smoke.AudioReplacement", "pending", "optional QA real-input audio observer", "Waiting for definition readiness and runner real E input; no definition refresh is permitted.");
            if (settings.HatchVoiceEnabled)
                access.PublishG4Status("Smoke.HatchAnimalVoice", "pending", "optional QA receipt-bound Hatch voice driver", "Waiting for SaveLoaded and already-preloaded definitions.");
            if (settings.CameraPlayableEnabled)
                access.PublishG4Status("Smoke.CameraPlayable", "pending", "optional QA receipt-bound CameraView arbitration", "Waiting for SaveLoaded; this route exercises Frozen CameraView compatibility only.");
            if (settings.ContentMetadataObservationEnabled)
                access.PublishG4Status("Smoke.ContentMetadataObservation", "pending", "optional QA read-only content metadata observer", "Waiting for SaveLoaded; world and inventory verbs are unavailable.");
            if (settings.ContinuousHomePageTerminalEnabled)
            {
                string generation = settings.ContinuousHomePageRequiresSaveLoaded
                    ? "Timer arms only after this participant observes SaveLoaded followed by ReturnedToTitle."
                    : "Timer may arm on a title-only ReturnedToTitle boundary without SaveLoaded.";
                access.PublishG4Status("Smoke.QaContinuousHomePage", "pending", "continuous HomePage terminal arbitration", generation + " It resets on every non-HomePage observation.");
            }
            if (settings.TitleLifecycleEnabled)
                access.PublishG4Status("Smoke.TitleButtonLifecycle", "pending", "optional QA title lifecycle", "Waiting for SaveLoaded, ReturnedToTitle, and continuous HomePage.");
            if (settings.ProductOwnerRefreshEnabled)
            {
                access.PublishG4Status("Smoke.ProductOwnerRefresh", "pending", "optional QA official Mod refresh", "Waiting for the bounded title delay before requesting one official reload.");
                access.PublishG4Status("Smoke.ProductOwnerRefreshCleanup", "pending", "optional QA product-owner cleanup", "Waiting for refresh, SaveLoaded, ReturnedToTitle, and continuous HomePage.");
            }
            if (settings.ExternalPlayerInputObservationEnabled)
            {
                access.PublishG4Status("Smoke.ExternalPlayerInput", "pending", "existing Runtime input-context sampler", "Waiting for continuous normal Gameplay before publishing the READY token.");
                access.PublishG4Status("Smoke.ExternalPlayerInputMarker", "pending", "runner terminal marker read-only observation", "Waiting for READY then the matching marker.");
                access.PublishG4Status("Smoke.ExternalPlayerInputCleanup", "pending", "QA observation around participant ReturnHome disposition", "Waiting for marker and continuous HomePage terminal.");
            }
            if (settings.G5WorldMutationCases.Length > 0)
                access.PublishG5Status("pending", "cases=" + string.Join("|", settings.G5WorldMutationCases) + "; setup=explicit-g5-whitelist; commit=pending; cleanup=runner-post-exit-transaction; owner=qa; fallback=false");
            if (settings.G6LifecycleCases.Length > 0)
                access.PublishG6Status("pending", "cases=" + string.Join("|", settings.G6LifecycleCases) + "; setup=explicit-g6-route; commit=pending; cleanup=title+shutdown+runner-post-exit-transaction; owner=qa; fallback=false");
        }

        private bool RunG3Probe(string hookId, string source, Func<string> probe)
        {
            try
            {
                string details = probe();
                string logPrefix = hookId.Equals("Smoke.CustomEntityApis", StringComparison.Ordinal)
                    ? "Smoke exercise CustomEntityApis OK "
                    : hookId + " QA probe OK ";
                access.Log(logPrefix + details);
                access.PublishG3Status(hookId, "verified", source, details);
                return true;
            }
            catch (Exception ex)
            {
                access.PublishG3Status(hookId, "failed", source, ex.GetType().Name + ": " + ex.Message + "; owner=qa; fallback=false");
                throw;
            }
        }

        private string[] GetIncompleteG3Requirements()
        {
            var incomplete = new System.Collections.Generic.List<string>();
            if (settings.CustomEntityContractEnabled && !customEntityCompleted)
                incomplete.Add("CustomEntityContract");
            if (settings.FishRoeTooltipObservationEnabled && !fishRoeCompleted)
                incomplete.Add("FishRoeTooltipObservation");
            if (itemDisplayNameLifecycleRequested && !fishRoeProbe.LifecycleComplete)
                incomplete.Add("ItemDisplayNameLifecycle");
            if (settings.DiagnosticsSnapshotEnabled && !diagnosticsCompleted)
                incomplete.Add("DiagnosticsSnapshot");
            if (settings.ObserveSaveLoaded && !saveLoadedObserved)
                incomplete.Add("SaveLoaded");
            if (settings.ObserveSaveSaved && !saveSavedObserved)
                incomplete.Add("SaveSaved");
            if (settings.ObserveWorkshopReloadCompleted && !workshopReloadObserved)
                incomplete.Add("WorkshopReloadCompleted");
            return incomplete.ToArray();
        }

        private void RunG4Step(string hookId, string source, Func<G4FixtureStepResult> action, ref bool completed)
        {
            try
            {
                G4FixtureStepResult result = action();
                if (!result.Completed)
                {
                    access.PublishG4Status(hookId, "pending", source, result.Details + "; owner=qa; fallback=false");
                    return;
                }
                if (!result.Succeeded)
                    throw new InvalidOperationException(result.Details);
                completed = true;
                access.PublishG4Status(hookId, "verified", source, result.Details + "; owner=qa; fallback=false");
            }
            catch (Exception ex)
            {
                access.PublishG4Status(hookId, "failed", source, ex.GetType().Name + ": " + ex.Message + "; owner=qa; fallback=false");
                throw;
            }
        }

        private void RunDebugConsoleObservation()
        {
            const string behaviorSource = "runner real Y/Escape input -> existing typed input sampler";
            if (!debugConsoleBehaviorCompleted)
            {
                G4FixtureStepResult behavior = debugConsoleBehavior.Observe(access.ReadLatestLogText(), DateTimeOffset.UtcNow);
                if (!behavior.Completed)
                {
                    access.PublishG4Status("Smoke.DebugConsoleHotkey", "pending", behaviorSource, behavior.Details + "; owner=qa; fallback=false");
                    return;
                }
                if (!behavior.Succeeded)
                    throw new InvalidOperationException(behavior.Details);
                debugConsoleBehaviorCompleted = true;
                debugConsoleBehaviorDetails = behavior.Details;
            }

            G4FixtureStepResult ui = scenarios.ObserveDebugConsoleUi();
            if (!ui.Completed)
            {
                access.PublishG4Status("Smoke.DebugConsoleUiEvidence", "pending", "real-owner UI + QA-owned screenshot", ui.Details + "; uiOwner=product; screenshotOwner=qa; fallback=false");
                access.PublishG4Status("Smoke.DebugConsoleHotkey", "pending", behaviorSource, debugConsoleBehaviorDetails + "; waitingForQaScreenshot=true; owner=qa; fallback=false");
                return;
            }
            if (!ui.Succeeded)
            {
                access.PublishG4Status("Smoke.DebugConsoleUiEvidence", "failed", "real-owner UI + QA-owned screenshot", ui.Details + "; uiOwner=product; screenshotOwner=qa; fallback=false");
                throw new InvalidOperationException(ui.Details);
            }

            debugConsoleCompleted = true;
            access.PublishG4Status("Smoke.DebugConsoleUiEvidence", "verified", "real-owner UI + QA-owned screenshot", ui.Details + "; uiOwner=product; screenshotOwner=qa; fallback=false");
            string details = debugConsoleBehaviorDetails + "; uiReceipt={" + ui.Details + "}; owner=qa; fallback=false";
            access.PublishG4Status("Smoke.DebugConsoleHotkey", "verified", behaviorSource, details);
            access.Log("Smoke exercise DebugConsoleHotkey OK " + details);
        }

        private void RunAudioReplacementObservation()
        {
            const string source = "runner real E -> paper-box OnInteract -> production AudioReplacement breadcrumbs";
            if (!audioReadinessCompleted)
            {
                G4FixtureStepResult readiness = scenarios.ObserveAudioDefinitions();
                if (!readiness.Completed)
                {
                    access.PublishG4Status("Smoke.AudioReplacement", "pending", source, readiness.Details + "; owner=qa; fallback=false");
                    return;
                }
                if (!readiness.Succeeded)
                    throw new InvalidOperationException(readiness.Details);
                audioReadinessCompleted = true;
                audioReadinessDetails = readiness.Details;
            }

            G4FixtureStepResult behavior = AudioReplacementBehaviorObservation.Observe(access.ReadLatestLogText(), audioReadinessDetails);
            if (!behavior.Completed)
            {
                access.PublishG4Status("Smoke.AudioReplacement", "pending", source, behavior.Details + "; owner=qa; fallback=false");
                return;
            }
            if (!behavior.Succeeded)
                throw new InvalidOperationException(behavior.Details);
            audioCompleted = true;
            access.PublishG4Status("Smoke.AudioReplacement", "verified", source, behavior.Details + "; owner=qa; fallback=false");
            access.Log("Smoke exercise AudioReplacement OK " + behavior.Details + "; owner=qa.");
        }

        private void RunHatchVoiceObservation()
        {
            const string source = "QA receipt-bound native Hatch stage/sound calls -> production AudioReplacement breadcrumbs";
            if (!hatchActionCompleted)
            {
                G4FixtureStepResult action = scenarios.ExerciseHatchVoice();
                if (!action.Completed)
                {
                    access.PublishG4Status("Smoke.HatchAnimalVoice", "pending", source, action.Details + "; owner=qa; fallback=false");
                    return;
                }
                if (!action.Succeeded)
                    throw new InvalidOperationException(action.Details);
                hatchActionCompleted = true;
                hatchActionDetails = action.Details;
            }

            string log = access.ReadLatestLogText();
            bool child = log.IndexOf("AudioReplacement event owner=DTMAPI.HatchAssets replacement=hatch-pet-child event=PLAY_ANIMAL_PET_CHICKEN_CHILD played=True suppressed=True", StringComparison.Ordinal) >= 0;
            bool adult = log.IndexOf("AudioReplacement event owner=DTMAPI.HatchAssets replacement=hatch-pet-adult event=PLAY_ANIMAL_PET_CHICKEN played=True suppressed=True", StringComparison.Ordinal) >= 0;
            if (!child || !adult)
            {
                access.PublishG4Status("Smoke.HatchAnimalVoice", "pending", source, hatchActionDetails + "; childPlayedSuppressed=" + child.ToString().ToLowerInvariant() + "; adultPlayedSuppressed=" + adult.ToString().ToLowerInvariant() + "; owner=qa; fallback=false");
                return;
            }
            hatchCompleted = true;
            string details = hatchActionDetails + "; childPlayedSuppressed=true; adultPlayedSuppressed=true; owner=qa; fallback=false";
            access.PublishG4Status("Smoke.HatchAnimalVoice", "verified", source, details);
            access.Log("Smoke exercise HatchAnimalVoice OK " + details);
        }

        private static void ValidateExternalMarkerBoundary(string evidenceRoot, string markerPath)
        {
            string root = Path.GetFullPath(evidenceRoot ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string marker = Path.GetFullPath(markerPath ?? string.Empty);
            string? markerDirectory = Path.GetDirectoryName(marker);
            if (!string.Equals(markerDirectory, root, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetFileName(marker), "external-player-input-complete.signal", StringComparison.Ordinal))
            {
                throw new InvalidDataException("QA external player-input marker must be the fixed direct child of the authorized runner evidence root.");
            }
        }

        private void RaiseReturnedToTitleG4Failure()
        {
            returnedToTitleG4FailureRaised = true;
            throw new InvalidOperationException(returnedToTitleG4Failure);
        }

        private void PublishReturnedToTitleG4Failures(IEnumerable<string> incomplete, string details)
        {
            var published = new HashSet<string>(StringComparer.Ordinal);
            foreach (string requirement in incomplete)
            {
                string hookId;
                switch (requirement)
                {
                    case "PauseMenuLayout": hookId = "Smoke.PauseMenuLayout"; break;
                    case "DebugConsole": hookId = "Smoke.DebugConsoleHotkey"; break;
                    case "SaveSlotsPaging": hookId = "Smoke.MoreSavesOfficialSaveUi"; break;
                    case "AnimalObservation": hookId = "Smoke.AnimalViewerRendering"; break;
                    case "EquipmentSlotsObservation": hookId = "Smoke.EquipmentSlotsUiObservation"; break;
                    case "AudioObservation": hookId = "Smoke.AudioReplacement"; break;
                    case "HatchVoice": hookId = "Smoke.HatchAnimalVoice"; break;
                    case "CameraPlayable": hookId = "Smoke.CameraPlayable"; break;
                    case "ContentMetadata": hookId = "Smoke.ContentMetadataObservation"; break;
                    case "ExternalInputReady": hookId = "Smoke.ExternalPlayerInput"; break;
                    case "ExternalInputMarker":
                    case "ExternalInputMarkerPassed": hookId = "Smoke.ExternalPlayerInputMarker"; break;
                    case "NativeUiObservation": hookId = "Smoke.NativeUiLayoutObservation"; break;
                    default: continue;
                }
                if (published.Add(hookId))
                    access.PublishG4Status(hookId, "failed", "ReturnedToTitle fail-closed boundary", details + "; cleanup=verified; postTitleOnly=true; owner=qa; fallback=false");
            }
        }

        private bool HasG4Requirements() =>
            HasTitleUiRoute() || settings.OfficialModUiEnabled || settings.PauseMenuLayoutEnabled ||
            settings.DebugConsoleEnabled || settings.SaveSlotsPagingEnabled || settings.AnimalObservationEnabled ||
            settings.EquipmentSlotsObservationEnabled ||
            settings.AudioObservationEnabled || settings.HatchVoiceEnabled || settings.CameraPlayableEnabled ||
            settings.ContentMetadataObservationEnabled || settings.ContinuousHomePageTerminalEnabled || settings.ExternalPlayerInputObservationEnabled ||
            settings.TitleLifecycleEnabled || settings.ProductOwnerRefreshEnabled || settings.AdvancedProductOwnerDeactivationEnabled;

        private bool RequiresSaveLoadedForG4() =>
            settings.PauseMenuLayoutEnabled || settings.DebugConsoleEnabled || settings.SaveSlotsPagingEnabled ||
            settings.AnimalObservationEnabled || settings.EquipmentSlotsObservationEnabled || settings.AudioObservationEnabled || settings.HatchVoiceEnabled ||
            settings.CameraPlayableEnabled || settings.ContentMetadataObservationEnabled || settings.ExternalPlayerInputObservationEnabled ||
            settings.TitleLifecycleEnabled || settings.ProductOwnerRefreshEnabled || settings.AdvancedProductOwnerDeactivationEnabled;

        private bool RequiresSaveLoadedBoundary() =>
            RequiresSaveLoadedForG4() ||
            settings.ContinuousHomePageRequiresSaveLoaded ||
            settings.CustomEntityContractEnabled || settings.FishRoeTooltipObservationEnabled ||
            settings.DiagnosticsSnapshotEnabled || settings.ObserveSaveLoaded || settings.ObserveSaveSaved ||
            settings.Batch5NoDemandEnabled ||
            settings.G5WorldMutationCases.Length > 0 || settings.G6LifecycleCases.Length > 0;

        private bool ShouldRequestInitialSaveLoad()
        {
            if (saveLoadedObserved || postTitleBoundaryObserved || settings.SaveSlot <= 0 || !RequiresSaveLoadedBoundary())
                return false;
            if (settings.ProductOwnerRefreshEnabled && !productOwnerRefreshCompleted)
                return false;
            if (DateTimeOffset.UtcNow - startedAt < TimeSpan.FromSeconds(Math.Max(1, settings.G6InitialDelaySeconds)))
                return false;
            if (settings.SaveSlotsPagingEnabled)
                return false;
            if (settings.G6LifecycleCases.Length > 0)
            {
                string first = settings.G6LifecycleCases[0];
                if (first.StartsWith(
                        "MoreSavesFixed12",
                        StringComparison.Ordinal))
                {
                    return false;
                }
                if (string.Equals(first, "SaveLoadCycle", StringComparison.Ordinal) ||
                    string.Equals(first, "SaveLoadPendingPressure", StringComparison.Ordinal) ||
                    string.Equals(first, "LongTitleLoad", StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }

        private bool ShouldRequestReturnHome()
        {
            if (returnHomeRequested || postTitleBoundaryObserved || !saveLoadedObserved)
                return false;
            if (batch6AutoFishingPilot?.ShouldRequestReturnHome == true)
                return true;
            if (GetIncompleteG3Requirements().Length > 0 || GetIncompleteG5Requirements().Length > 0 || GetIncompleteG6Requirements().Length > 0)
                return false;
            if (batch5GcLadder.ShouldRequestReturnHome)
                return true;
            if (batch5NoDemand?.ShouldRequestReturnHome == true)
                return true;
            if (homePageTerminals.Count == 0)
                return false;
            bool onlyPostTitleRequirementsRemain = GetIncompleteG4Requirements().All(item =>
                string.Equals(item, "ContinuousHomePage", StringComparison.Ordinal) ||
                string.Equals(item, "MoreSavesPostTitlePanel", StringComparison.Ordinal) ||
                string.Equals(item, "AdvancedProductOwnerDeactivation", StringComparison.Ordinal));
            if (!onlyPostTitleRequirementsRemain)
            {
                returnHomeQuiescenceObservedAt = default;
                return false;
            }

            // A native UI fixture can publish its terminal receipt in the same frame
            // that it pops its exact state. Give the game a continuous normal-Gameplay
            // window to settle that native exit before ReturnHome tears the scene down.
            return QaScenarioController.HasContinuousStableObservationForFixture(
                scenarios.IsNormalGameplay(),
                DateTimeOffset.UtcNow,
                1d,
                ref returnHomeQuiescenceObservedAt);
        }

        private bool AllRequirementsComplete()
        {
            if (GetIncompleteG3Requirements().Length > 0 || GetIncompleteG4Requirements().Length > 0 ||
                GetIncompleteG5Requirements().Length > 0 || GetIncompleteG6Requirements().Length > 0)
            {
                return false;
            }
            return (!batch5GcLadder.Enabled || batch5GcLadder.TerminalSucceeded) &&
                (batch5NoDemand == null || batch5NoDemand.TerminalSucceeded);
        }

        private string[] GetIncompleteG5Requirements()
        {
            if (settings.G5WorldMutationCases.Length == 0 || g5WorldMutationCompleted)
                return Array.Empty<string>();
            return settings.G5WorldMutationCases.Skip(g5WorldMutationIndex).ToArray();
        }

        private string[] GetIncompleteG6Requirements()
        {
            if (settings.G6LifecycleCases.Length == 0 || g6LifecycleCompleted)
                return Array.Empty<string>();
            return settings.G6LifecycleCases.Skip(g6LifecycleIndex).ToArray();
        }

        private bool HasTitleUiRoute() =>
            settings.TitleSettingsUiEnabled || settings.ManagerStatusUiEnabled || settings.ManagerMvpUiEnabled;

        private bool RequiresMoreSavesPostTitlePanelReceipt() =>
            settings.SaveSlotsPagingEnabled &&
            settings.AdvancedProductOwnerDeactivationEnabled &&
            settings.AdvancedProductOwnerDeactivationOwnerIds.Any(ownerId =>
                ownerId.Equals("DTMAPI.MoreSavesMod", StringComparison.OrdinalIgnoreCase));

        private bool RequiresStrongPlantingGunReentry() =>
            settings.AdvancedProductOwnerDeactivationEnabled &&
            settings.AdvancedProductOwnerDeactivationOwnerIds.Any(ownerId =>
                ownerId.Equals(
                    "DTMAPI.StrongPlantingGunMod",
                    StringComparison.OrdinalIgnoreCase));

        private string[] GetIncompleteInSaveG4Requirements()
        {
            var incomplete = new List<string>();
            if (settings.PauseMenuLayoutEnabled && !pauseMenuCompleted) incomplete.Add("PauseMenuLayout");
            if (settings.DebugConsoleEnabled && !debugConsoleCompleted) incomplete.Add("DebugConsole");
            if (settings.SaveSlotsPagingEnabled && !saveSlotsCompleted) incomplete.Add("SaveSlotsPaging");
            if (settings.AnimalObservationEnabled && !animalCompleted) incomplete.Add("AnimalObservation");
            if (settings.EquipmentSlotsObservationEnabled && !equipmentSlotsCompleted) incomplete.Add("EquipmentSlotsObservation");
            if (settings.AudioObservationEnabled && !audioCompleted) incomplete.Add("AudioObservation");
            if (settings.HatchVoiceEnabled && !hatchCompleted) incomplete.Add("HatchVoice");
            if (settings.CameraPlayableEnabled && !cameraCompleted) incomplete.Add("CameraPlayable");
            if (settings.ContentMetadataObservationEnabled && !contentMetadataCompleted) incomplete.Add("ContentMetadata");
            if (settings.ExternalPlayerInputObservationEnabled && (externalPlayerInput == null || !externalPlayerInput.Ready)) incomplete.Add("ExternalInputReady");
            if (settings.ExternalPlayerInputObservationEnabled && (externalPlayerInput == null || !externalPlayerInput.MarkerObserved)) incomplete.Add("ExternalInputMarker");
            if (settings.ExternalPlayerInputObservationEnabled && externalPlayerInput != null && externalPlayerInput.MarkerObserved && !externalPlayerInput.MarkerPassed) incomplete.Add("ExternalInputMarkerPassed");
            if (settings.PauseMenuLayoutEnabled && !nativeUiObservationCompleted) incomplete.Add("NativeUiObservation");
            return incomplete.ToArray();
        }

        private string[] GetIncompleteG4RequirementsForClose()
        {
            string[] incomplete = GetIncompleteG4Requirements();
            if (!inSaveG4ExecutionBlocked || string.IsNullOrWhiteSpace(returnedToTitleG4Failure))
                return incomplete;
            var aborted = new HashSet<string>(GetIncompleteInSaveG4Requirements(), StringComparer.Ordinal);
            return incomplete.Where(item => !aborted.Contains(item)).ToArray();
        }

        private string[] GetIncompleteG4Requirements()
        {
            var incomplete = new List<string>();
            if (settings.TitleSettingsUiEnabled && !titleSettingsCompleted) incomplete.Add("TitleSettingsUi");
            if (settings.ManagerStatusUiEnabled && !managerStatusCompleted) incomplete.Add("ManagerStatusUi");
            if (settings.ManagerMvpUiEnabled && !managerMvpCompleted) incomplete.Add("ManagerMvpUi");
            if (settings.OfficialModUiEnabled && !officialModUiCompleted) incomplete.Add("OfficialModUi");
            if (settings.PauseMenuLayoutEnabled && !pauseMenuCompleted) incomplete.Add("PauseMenuLayout");
            if (settings.DebugConsoleEnabled && !debugConsoleCompleted) incomplete.Add("DebugConsole");
            if (settings.SaveSlotsPagingEnabled && !saveSlotsCompleted) incomplete.Add("SaveSlotsPaging");
            if (RequiresMoreSavesPostTitlePanelReceipt() && !moreSavesPostTitlePanelCompleted) incomplete.Add("MoreSavesPostTitlePanel");
            if (settings.AnimalObservationEnabled && !animalCompleted) incomplete.Add("AnimalObservation");
            if (settings.EquipmentSlotsObservationEnabled && !equipmentSlotsCompleted) incomplete.Add("EquipmentSlotsObservation");
            if (settings.AudioObservationEnabled && !audioCompleted) incomplete.Add("AudioObservation");
            if (settings.HatchVoiceEnabled && !hatchCompleted) incomplete.Add("HatchVoice");
            if (settings.CameraPlayableEnabled && !cameraCompleted) incomplete.Add("CameraPlayable");
            if (settings.ContentMetadataObservationEnabled && !contentMetadataCompleted) incomplete.Add("ContentMetadata");
            if (settings.ExternalPlayerInputObservationEnabled && (externalPlayerInput == null || !externalPlayerInput.Ready)) incomplete.Add("ExternalInputReady");
            if (settings.ExternalPlayerInputObservationEnabled && (externalPlayerInput == null || !externalPlayerInput.MarkerObserved)) incomplete.Add("ExternalInputMarker");
            if (settings.ExternalPlayerInputObservationEnabled && externalPlayerInput != null && externalPlayerInput.MarkerObserved && !externalPlayerInput.MarkerPassed) incomplete.Add("ExternalInputMarkerPassed");
            if ((HasTitleUiRoute() || settings.OfficialModUiEnabled || settings.PauseMenuLayoutEnabled) && !nativeUiObservationCompleted) incomplete.Add("NativeUiObservation");
            if (settings.ProductOwnerRefreshEnabled && !productOwnerRefreshCompleted) incomplete.Add("ProductOwnerRefresh");
            if (homePageTerminals.Count > 0 && !continuousHomePageCompleted) incomplete.Add("ContinuousHomePage");
            if (postTitleBoundaryObserved && settings.ProductOwnerRefreshEnabled && !productOwnerRefreshCleanupCompleted) incomplete.Add("ProductOwnerRefreshCleanup");
            if (postTitleBoundaryObserved && settings.TitleLifecycleEnabled && !titleLifecycleCompleted) incomplete.Add("TitleLifecycle");
            if (settings.AdvancedProductOwnerDeactivationEnabled && !advancedProductOwnerDeactivationCompleted) incomplete.Add("AdvancedProductOwnerDeactivation");
            return incomplete.ToArray();
        }
    }
}
