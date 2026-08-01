using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>Owns optional, receipt-bound QA scenario policy outside the player GameBridge.</summary>
    internal sealed partial class QaScenarioController
    {
        private readonly GameBridgeFixtureAccess access;
        private readonly DtmApiRuntime runtime;
        private DolocTownGameBridge bridge => access.Bridge;
        private static readonly string[] OneActionWrongToolTargetKinds = { "Tree", "Ore", "Garbage", "Weeds" };
        private static readonly TimeSpan SaveLoadCyclePendingLogInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan SaveLoadCycleLongIdlePendingLogInterval = TimeSpan.FromMinutes(5);
        private readonly DateTimeOffset initializedAt = DateTimeOffset.Now;
        private HarmonyReflectionPatcher? patcher;
        private G6FixtureState? g6FixtureState;
        private bool autoExerciseActionSpeedInteractionMainFarmRequested;
        private bool autoExerciseOneActionMainFarmRequested;
        private readonly HashSet<string> autoExerciseOneActionWrongToolCreateAttempts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> pauseMenuLayoutSamples = new List<string>();
        private bool autoLoadOfficialPathRequested;
        private bool modChangePromptConfirmed;
        private bool autoLoadDirectFallbackAfterModChangeAttempted;
        private int? pendingAutoLoadGameIndex;
        private object? pendingAutoLoadGameDataState;
        private DateTimeOffset modChangePromptConfirmedAt = DateTimeOffset.MinValue;
        private DateTimeOffset saveLoadedAt;
        private DateTimeOffset lastAutoLoadReadinessLog = DateTimeOffset.MinValue;
        private DateTimeOffset lastTitleLifecycleReadinessLog = DateTimeOffset.MinValue;
        private DateTimeOffset lastSaveLoadCycleReadinessLog = DateTimeOffset.MinValue;
        private DateTimeOffset lastSaveLoadCyclePendingPressureReadinessLog = DateTimeOffset.MinValue;
        private DateTimeOffset lastActionSpeedReadinessLog = DateTimeOffset.MinValue;
        private DateTimeOffset lastOneActionReadinessLog = DateTimeOffset.MinValue;
        private DateTimeOffset debugTeleportRequestedAt;
        private TeleportSnapshot? debugTeleportBeforeSnapshot;
        private TeleportDestination? debugTeleportDestination;
        private TeleportResult? debugTeleportRequestResult;
        private bool mineOfficialTechTreeUiOpenRequested;
        private bool mineOfficialTechTreeUiEvidenceCaptured;
        private DateTimeOffset mineOfficialTechTreeUiClosedAt;
        private DateTimeOffset lastMineOfficialTechTreeUiRecoveryLogAt;
        private DateTimeOffset mineOfficialTechTreeUiOpenAt;
        private string mineOfficialTechTreeUiTreeId = string.Empty;
        private string mineOfficialTechTreeUiNodeId = string.Empty;
        private int saveLoadCycleStage;
        private int saveLoadCycleCompletedCount;
        private DateTimeOffset saveLoadCycleStageAt;
        private DateTimeOffset saveLoadCycleLoadRequestedAt;
        private DateTimeOffset saveLoadCycleSaveLoadedAt;
        private DateTimeOffset saveLoadCycleStartedAt;
        private DateTimeOffset saveLoadCycleHomePageObservedAt;
        private bool saveLoadCyclePreLoadGcProbeCompleted;
        private int saveLoadCyclePendingPressureStage;
        private int saveLoadCyclePendingPressurePublishedCount;
        private DateTimeOffset saveLoadCyclePendingPressureStartedAt;
        private DateTimeOffset saveLoadCyclePendingPressureLastPublishedAt;
        private DateTimeOffset saveLoadCyclePendingPressureHomePageObservedAt;
        private DateTimeOffset saveLoadCyclePendingPressureLoadRequestedAt;
        private DateTimeOffset saveLoadCyclePendingPressureSaveLoadedAt;
        private string? newContentEvidenceDir;
        private bool qaHostObservedSaveLoaded;
        private Batch5GcLadderFixtureOptions batch5GcLadder = new Batch5GcLadderFixtureOptions();
        private readonly Batch6ActionSpeedReflectionObserver actionSpeedProductObserver = new Batch6ActionSpeedReflectionObserver();
        private DebugConsoleActionFixtureAdapter? debugConsoleActions;
        private string debugConsoleSaveAcceptancePhase = "None";
        private int expectedDebugConsoleMoney = -1;

        private DolocTownExperimentalBridgeApi? experimentalApi => bridge.ExperimentalApiForQa;
        private DebugConsoleActionFixtureAdapter DebugConsoleActions =>
            debugConsoleActions ??=
                new DebugConsoleActionFixtureAdapter(runtime, bridge);
        private IInventoryDebugApi inventoryDebugApi => DebugConsoleActions;
        private IWeatherDebugApi weatherDebugApi => DebugConsoleActions;
        private ITeleportDebugApi teleportDebugApi => DebugConsoleActions;
        private IInstantSaveDebugApi instantSaveDebugApi => DebugConsoleActions;
        private ITimeDebugApi timeDebugApi => DebugConsoleActions;
        private IMovementDebugApi movementDebugApi => DebugConsoleActions;
        private IAdvancedDebugApi advancedDebugApi => DebugConsoleActions;
        private CameraFeature? cameraFeature => bridge.CameraFeatureForQa;
        private FishingAutomationCompatibilityFeature? fishingCompatibilityFeature => bridge.FishingAutomationCompatibilityFeatureForQa;
        private AudioReplacementFeature? audioReplacementFeature => bridge.AudioReplacementFeatureForQa;
        private NativeUiLayoutDiagnosticsFeature? nativeUiLayoutDiagnosticsFeature => bridge.NativeUiLayoutDiagnosticsFeatureForQa;
        private NativeUiLayoutRepairService? NativeUiLayoutRepairService => bridge.NativeUiLayoutRepairService;
        private ChestLocatorEnhancerService? ChestLocatorEnhancerService => bridge.ChestLocatorEnhancerService;
        private CropHarvestingService? CropHarvestingService => bridge.CropHarvestingService;
        private ActionCompletionService? ActionCompletionService => bridge.ActionCompletionService;
        private bool AllHookTargetsReady => bridge.AllHookTargetsReadyForQa;
        private bool actionSpeedInteractEnterPatched => ObserveActionSpeedProduct().ActualHarmonyOwnerReady;
        private bool actionSpeedInteractExitPatched => ObserveActionSpeedProduct().ActualHarmonyOwnerReady;
        private bool actionSpeedEatEnterPatched => ObserveActionSpeedProduct().ActualHarmonyOwnerReady;
        private bool actionSpeedUseItemContinuesPatched => ObserveActionSpeedProduct().ActualHarmonyOwnerReady;
        private bool actionSpeedInteractContinuesPatched => ObserveActionSpeedProduct().ActualHarmonyOwnerReady;

        private Batch6ActionSpeedObservation ObserveActionSpeedProduct() => actionSpeedProductObserver.Observe(runtime);

        internal QaScenarioController(GameBridgeFixtureAccess access)
        {
            this.access = access ?? throw new ArgumentNullException(nameof(access));
            runtime = access.Runtime;
        }

        internal bool TryRequestInitialSaveLoad(int saveSlot) => TryAutoLoadSave(saveSlot);

        internal void ContinueInitialSaveLoad()
        {
            if (!autoLoadOfficialPathRequested)
                return;
            if (!modChangePromptConfirmed)
                TryConfirmModChangePrompt();
            if (modChangePromptConfirmed)
                TryAutoLoadSaveDirectFallbackAfterModChange();
        }

        internal void NotifyUiObservation(string source) => g4UiObservationCount++;

        internal G4FixtureStepResult ObserveTitleSettingsUi() => ObserveTitleSettingsUiForFixture(GetEvidencePath("g4/ui", "title-settings.png"));
        internal G4FixtureStepResult ObserveManagerStatusUi() => ObserveManagerStatusUiForFixture(GetEvidencePath("g4/ui", "manager-status-page.png"), access.SetHookStatus);
        internal G4FixtureStepResult ObserveManagerMvpUi() => ObserveManagerMvpUiForFixture(
            GetEvidencePath("g4/ui", "manager-status-page.png"),
            GetEvidencePath("g4/ui", "manager-mods-page.png"),
            GetEvidencePath("g4/ui", "manager-advanced-page.png"),
            GetEvidencePath("g4/ui", "manager-logs-page.png"),
            access.SetHookStatus);
        internal G4FixtureStepResult ObserveOfficialModUi() => ObserveOfficialModUiForFixture(GetEvidencePath("g4/ui", "official-mod-ui.png"));
        internal G4FixtureStepResult ObservePauseMenuLayout() => ObservePauseMenuLayoutForFixture(GetEvidencePath("g4/ui", "pause-menu-layout.png"));
        internal G4FixtureStepResult ObserveDebugConsoleUi() => ObserveDebugConsoleUiForFixture(GetEvidencePath("g4/ui", "debug-console.png"));
        internal G4FixtureStepResult AdvanceSaveSlotScenario(int saveSlot) => AdvanceSaveSlotScenarioForFixture(saveSlot, GetEvidencePath("g4/ui", "official-save-ui.png"));
        internal G4FixtureStepResult ObservePostTitleMoreSavesOfficialSaveUi() => ObservePostTitleMoreSavesOfficialSaveUiForFixture(GetEvidencePath("g4/ui", "official-save-ui-post-title.png"));
        internal G4FixtureStepResult ObserveAnimals() => ObserveAnimalsForFixture(GetEvidencePath("g4/ui", "animal-panel.png"));
        internal G4FixtureStepResult ObserveEquipmentSlotsUi() => ObserveEquipmentSlotsUiForFixture(GetEvidencePath("g4/ui", "equipment-slots.png"), GetEvidencePath("g4/ui", "equipment-slots-summary.txt"));
        internal G4FixtureStepResult ObserveAudioDefinitions() => ObserveAudioDefinitionsForFixture();
        internal G4FixtureStepResult ExerciseHatchVoice() => ExerciseHatchVoiceForFixture();
        internal G4FixtureStepResult ExerciseCameraPlayable() => ExerciseCameraPlayableForFixture(access.RunId, GetEvidenceDirectory("g4/camera-playable"));
        internal G4FixtureStepResult ObserveContentMetadata(bool expectOilAbsent) => ObserveContentMetadataForFixture(expectOilAbsent);
        internal G4FixtureStepResult ObserveNativeUiLayout() => ObserveNativeUiLayoutForFixture();
        internal G4FixtureStepResult RequestProductOwnerRefresh() => TryReloadOfficialModsAndConfig("Smoke.ProductOwnerRefresh", "optional QA product-owner refresh before save load", out string details) ? G4FixtureStepResult.Verified(details) : G4FixtureStepResult.Failed(details);
        internal G4FixtureStepResult AdvanceG5WorldMutation(string caseId) => AdvanceG5WorldMutationForFixture(caseId);
        internal void ConfigureDebugConsoleSaveAcceptance(
            string phase,
            int expectedMoney)
        {
            debugConsoleSaveAcceptancePhase =
                (phase ?? "None").Trim();
            expectedDebugConsoleMoney = expectedMoney;
        }
        internal void ConfigureMoreEquipmentSlotsNoNativeSaveColdObserver(
            int backpackBaseline,
            long committedGeneration,
            int committedOccupied,
            string committedSlots) =>
            ConfigureMoreEquipmentSlotsNoNativeSaveColdObserverForFixture(
                backpackBaseline,
                committedGeneration,
                committedOccupied,
                committedSlots);
        internal void ConfigureMoreEquipmentSlotsInterruptedCandidateObserver(
            bool enabled,
            long candidatePreGeneration,
            int committedOccupied,
            string committedSlots) =>
            ConfigureMoreEquipmentSlotsInterruptedCandidateObserverForFixture(
                enabled,
                candidatePreGeneration,
                committedOccupied,
                committedSlots);
        internal void ConfigureMoreEquipmentSlotsTransition(string phase) =>
            ConfigureMoreEquipmentSlotsTransitionForFixture(phase);
        internal void NotifyMoreEquipmentSlotsTransitionSaveSaved() =>
            NotifyMoreEquipmentSlotsTransitionSaveSavedForFixture();
        internal void ConfigureBatch5GcLadder(Batch5GcLadderFixtureOptions options) =>
            batch5GcLadder = options ?? throw new ArgumentNullException(nameof(options));
        internal void PrepareG6Lifecycle(G6FixtureOptions options) => PrepareG6LifecycleForFixture(options);
        internal G4FixtureStepResult AdvanceG6Lifecycle(string caseId) => AdvanceG6LifecycleForFixture(caseId);
        internal void NotifyG6SaveLoaded()
        {
            qaHostObservedSaveLoaded = true;
            NotifyG6SaveLoadedForFixture();
        }
        internal bool IsNormalGameplay() => IsNormalGameplayForFixture();

        internal void OnReturnedToTitle()
        {
            FinalizeOwnerLifetimeAfterTitle();
        }

        internal void Close(string reason)
        {
            var failures = new List<Exception>();
            RunScenarioCloseStep(failures, "ActionSpeedAutoFillFixtureCleanup", () => CleanupActionSpeedAutoFillFixtureState("QA host close: " + (reason ?? string.Empty)));
            RunScenarioCloseStep(failures, "PendingMineProductionCleanup", () => CleanupPendingMineProductionForFixture("QA host close: " + (reason ?? string.Empty)));
            RunScenarioCloseStep(failures, "OwnerLifetimeCleanup", () => CleanupOwnerLifetimeOnClose(reason ?? string.Empty));
            if (failures.Count > 0)
                throw new AggregateException("QA scenario close did not complete; cleanup may be retried.", failures);
        }

        private static void RunScenarioCloseStep(List<Exception> failures, string operation, Action action)
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

        private void UpdateRuntimeAutomation() => bridge.UpdateRuntimeAutomation();

        private string GetEvidenceDirectory(string relativePath)
        {
            string qaRoot = Path.GetFullPath(Path.Combine(access.EvidenceRoot, "qa-host"));
            string directory = Path.GetFullPath(Path.Combine(qaRoot, relativePath ?? string.Empty));
            string requiredPrefix = qaRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!directory.Equals(qaRoot, StringComparison.OrdinalIgnoreCase) &&
                !directory.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("QA evidence directory escaped the authorized run root.");
            }

            Directory.CreateDirectory(directory);
            return directory;
        }

        private string GetEvidencePath(string relativeDirectory, string fileName)
        {
            string directory = GetEvidenceDirectory(relativeDirectory);
            string path = Path.GetFullPath(Path.Combine(directory, fileName ?? string.Empty));
            string requiredPrefix = directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("QA evidence file escaped its authorized directory.");
            return path;
        }

        private void EditAndSaveConfigPage(string uniqueId, Action<IConfigMenuPage> edit)
        {
            IConfigMenuPage? page = runtime.CreateSnapshot().ConfigPages.FirstOrDefault(candidate =>
                candidate.Manifest.UniqueID.Equals(uniqueId ?? string.Empty, StringComparison.OrdinalIgnoreCase));
            if (page == null)
                throw new InvalidOperationException("Config page was not registered: " + (uniqueId ?? string.Empty) + ".");
            if (page.IsLocked)
                throw new InvalidOperationException("Config page was locked: " + page.LockReason);

            object? configMenuRuntime = runtime.GetType()
                .GetProperty("ConfigMenuRuntime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(runtime);
            if (configMenuRuntime == null)
                throw new InvalidOperationException("Config menu runtime was not available.");

            Type? runtimeContract = configMenuRuntime.GetType().GetInterfaces().FirstOrDefault(candidate =>
                string.Equals(candidate.FullName, "DTMAPI.Abstractions.IConfigMenuRuntime", StringComparison.Ordinal));
            System.Reflection.MethodInfo? beginEditing = runtimeContract?.GetMethod("BeginEditing");
            System.Reflection.MethodInfo? save = runtimeContract?.GetMethod("Save");
            System.Reflection.MethodInfo? cancel = runtimeContract?.GetMethod("Cancel");
            if (beginEditing == null || save == null || cancel == null)
                throw new MissingMethodException("The internal config-menu runtime contract was not available to the QA assembly.");

            bool editingBegan = false;
            bool committed = false;
            Exception? failure = null;
            try
            {
                beginEditing.Invoke(configMenuRuntime, new object[] { page.Manifest.UniqueID });
                editingBegan = true;
                edit?.Invoke(page);
                save.Invoke(configMenuRuntime, new object[] { page.Manifest.UniqueID });
                committed = true;
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            if (editingBegan && !committed)
            {
                try
                {
                    cancel.Invoke(configMenuRuntime, new object[] { page.Manifest.UniqueID });
                }
                catch (Exception cancelFailure)
                {
                    if (failure != null)
                        throw new AggregateException("QA config mutation failed and its editing transaction could not be cancelled.", failure, cancelFailure);
                    throw;
                }
            }

            if (failure != null)
                throw failure;
        }

        private static bool TryGetStaticBoolProperty(Type type, string propertyName)
        {
            if (type == null)
                return false;
            object? value = type.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.GetValue(null);
            return value is bool result && result;
        }

        private static bool IsUiStateActive(Type dolocApi, Type stateType) => GetExistingUiState(dolocApi, stateType) != null;

        private static object? GetExistingUiState(Type dolocApi, Type stateType)
        {
            object? userInput = GameBridgeNativeHelpers.ReadStaticMember(dolocApi, "userInput");
            object? state = userInput == null ? null : GameBridgeNativeHelpers.ReadMember(userInput, "CurrentState");
            return state != null && stateType.IsInstanceOfType(state) ? state : null;
        }

        private bool TryVerifyDiagnosticsSnapshotForFixture(string scenario, params string[] expectedFeatureIds)
        {
            try
            {
                string reportPath = access.ExportDiagnostics();
                IDtmDiagnosticsSnapshot snapshot = access.GetDiagnosticsSnapshot();
                foreach (string expected in expectedFeatureIds)
                {
                    if (!System.Linq.Enumerable.Any(snapshot.FeatureStatuses, status => status.FeatureId.Equals(expected, StringComparison.OrdinalIgnoreCase) && status.Status.Equals("ready", StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException("Missing ready feature status: " + expected + ".");
                }
                if (string.IsNullOrWhiteSpace(snapshot.LatestLogPath) || !System.IO.File.Exists(snapshot.LatestLogPath) || string.IsNullOrWhiteSpace(reportPath) || !System.IO.File.Exists(reportPath))
                    throw new InvalidOperationException("Diagnostics log/report evidence is missing.");
                access.SetHookStatus("Smoke.DiagnosticsSnapshot", "verified", "optional fixture diagnostics export", "scenario=" + scenario + "; expectedFeatures=" + string.Join(",", expectedFeatureIds) + "; mods=" + snapshot.Mods.Count + "; owner=qa");
                return true;
            }
            catch (Exception ex)
            {
                access.SetHookStatus("Smoke.DiagnosticsSnapshot", "failed", "optional fixture diagnostics export", scenario + ": " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }
    }
}
