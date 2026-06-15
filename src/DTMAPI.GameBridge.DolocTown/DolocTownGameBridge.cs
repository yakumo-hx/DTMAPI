using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private static readonly string[] OneActionWrongToolTargetKinds = { "Tree", "Ore", "Garbage", "Weeds" };
        private static readonly TimeSpan FeatureStatusPublishHeartbeat = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan FeatureFailureSummaryInterval = TimeSpan.FromSeconds(30);
        private const int FeatureFailureShortLogLimit = 3;
        private const int FeatureFailureRecoverySuccessThreshold = 3;
        private readonly DtmApiRuntime runtime;
        private readonly Func<bool>? clickTitleSettingsButton;
        private readonly IDebugConsoleApi? debugConsoleApi;
        private SmokeSettings? smokeSettings;
        private DateTimeOffset initializedAt;
        private HarmonyReflectionPatcher? patcher;
        private bool autoLoadAttempted;
        private bool autoSaveAttempted;
        private bool autoReloadModsAttempted;
        private bool autoExerciseAttempted;
        private bool autoExerciseActionSpeedToolAttempted;
        private bool autoExerciseActionSpeedConfigApplyAttempted;
        private bool autoExerciseActionSpeedInteractionAttempted;
        private bool autoExerciseActionSpeedInteractionMainFarmRequested;
        private bool autoExerciseOneActionResourceHitAttempted;
        private bool autoExerciseOneActionWrongToolAttempted;
        private bool autoExerciseOneActionFuelFeedAttempted;
        private bool autoExerciseOneActionVegetationAttempted;
        private bool autoExerciseOneActionMainFarmRequested;
        private readonly HashSet<string> autoExerciseOneActionWrongToolCreateAttempts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool autoExerciseAutoFishingPhaseAttempted;
        private bool autoExerciseAutoFishingPhaseStarted;
        private bool autoExerciseAutoFishingAutoCastVerified;
        private bool autoExerciseAutoFishingPhaseVerified;
        private bool autoFishingReportExported;
        private int autoFishingMiniGameCompleteBaseline;
        private bool autoExercisePauseMenuLayoutAttempted;
        private int pauseMenuLayoutStage;
        private DateTimeOffset pauseMenuLayoutStageAt;
        private DateTimeOffset pauseMenuLayoutLastSampleAt;
        private readonly List<string> pauseMenuLayoutSamples = new List<string>();
        private string? pauseMenuLayoutEvidenceDir;
        private bool autoExerciseTitleButtonLifecycleAttempted;
        private bool autoExerciseInstantSaveAttempted;
        private bool autoExerciseDebugConsoleAttempted;
        private bool autoExerciseDebugInventoryAttempted;
        private bool autoExerciseDebugWeatherAttempted;
        private bool autoExerciseDebugTeleportAttempted;
        private bool autoExerciseDebugTimeAttempted;
        private bool autoExerciseDebugMovementAttempted;
        private bool autoExerciseAdvancedDebugAttempted;
        private bool autoExerciseNewContentApisAttempted;
        private bool autoExerciseMineContentApisAttempted;
        private bool autoExerciseZoomAttempted;
        private ZoomSmokeRun? zoomSmokeRun;
        private bool autoExerciseChestLocatorEnhancerAttempted;
        private bool autoExerciseStrongPlantingGunAttempted;
        private bool autoExerciseCropHarvestingApiAttempted;
        private bool autoExerciseCustomEntityApisAttempted;
        private bool debugTeleportVerificationCompleted;
        private bool autoFishingHotkeyInjected;
        private bool autoOpenTitleSettingsAttempted;
        private bool autoOpenOfficialModUiAttempted;
        private bool autoOpenAnimalPanelAttempted;
        private bool autoExitAttempted;
        private bool autoLoadOfficialPathRequested;
        private bool modChangePromptConfirmed;
        private int? pendingAutoLoadGameIndex;
        private int? pendingAutoSaveIndex;
        private object? pendingAutoLoadGameDataState;
        private DateTimeOffset saveLoadedAt;
        private DateTimeOffset titleSettingsButtonScreenshotAt;
        private bool titleSettingsButtonScreenshotRequested;
        private DateTimeOffset titleSettingsMenuEvidenceAt;
        private bool titleSettingsMenuScreenshotRequested;
        private DateTimeOffset titleSettingsStatusPageEvidenceAt;
        private bool titleSettingsStatusPageScreenshotRequested;
        private bool titleSettingsStatusSummaryTextRecorded;
        private bool titleSettingsStatusSummaryCopyRecorded;
        private int titleSettingsManagerMvpStage;
        private DateTimeOffset titleSettingsManagerMvpStageAt;
        private int titleSettingsConfigScreenshotStage;
        private DateTimeOffset titleSettingsConfigScreenshotAt;
        private string? titleSettingsEvidenceDir;
        private DateTimeOffset officialModUiEvidenceAt;
        private string? officialModUiEvidenceDir;
        private DateTimeOffset animalViewerUiEvidenceAt;
        private bool animalViewerUiDelayedScreenshotRequested;
        private DateTimeOffset lastAutoLoadReadinessLog = DateTimeOffset.MinValue;
        private DateTimeOffset lastTitleLifecycleReadinessLog = DateTimeOffset.MinValue;
        private DateTimeOffset lastActionSpeedReadinessLog = DateTimeOffset.MinValue;
        private DateTimeOffset lastOneActionReadinessLog = DateTimeOffset.MinValue;
        private DateTimeOffset lastAutoFishingReadinessLog = DateTimeOffset.MinValue;
        private DateTimeOffset autoFishingPhaseStartedAt;
        private int autoFishingApplicationBaseline;
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
        private int titleLifecycleStage;
        private DateTimeOffset titleLifecycleStageAt;
        private string? titleLifecycleEvidenceDir;
        private string? newContentEvidenceDir;
        private Timer? hookRetryTimer;
        private readonly object hookGate = new object();
        private bool saveLoadedPatched;
        private bool saveLoadedEventSubscribed;
        private bool loadRequestedPatched;
        private bool saveSavingPatched;
        private bool saveSavedPatched;
        private bool returnHomePatched;
        private bool workshopReloadPatched;
        private bool workshopLocalUploadDisplayPatched;
        private bool workshopUploadPlanBusyFallbackPatched;
        private bool workshopUploadPlanKnownIdFallbackPatched;
        private readonly List<PendingWorkshopUploadPlanResolution> pendingWorkshopUploadPlanResolutions = new List<PendingWorkshopUploadPlanResolution>();
        private bool actionSpeedToolEnterPatched => actionSpeedFeature?.HookBridge.ToolEnterPatched == true;
        private bool actionSpeedToolExitPatched => agentStateLifecycleHooks?.ToolExitPatched == true;
        private bool actionSpeedInteractEnterPatched => actionSpeedFeature?.HookBridge.InteractEnterPatched == true;
        private bool actionSpeedInteractExitPatched => agentStateLifecycleHooks?.InteractExitPatched == true;
        private bool actionSpeedEatEnterPatched => actionSpeedFeature?.HookBridge.EatEnterPatched == true;
        private bool actionSpeedUseItemContinuesPatched => actionSpeedFeature?.HookBridge.UseItemContinuesPatched == true;
        private bool actionSpeedBaseExitPatched => agentStateLifecycleHooks?.BaseExitPatched == true;
        private bool debugConsoleUseToolPatched;
        private bool debugConsoleUseItemPatched;
        private bool debugConsoleEnterUiCheckPatched;
        private bool toolColliderHitPostfixPatched => toolColliderHitHooks?.PostfixPatched == true;
        private bool oilCoalDropRoutePatched => toolColliderHitHooks?.RoutePatched == true;
        private bool fishingReadyEnterPatched => fishingAutomationFeature?.HookBridge.ReadyEnterPatched == true;
        private bool fishingCastEnterPatched => fishingAutomationFeature?.HookBridge.CastEnterPatched == true;
        private bool fishingWaitEnterPatched => fishingAutomationFeature?.HookBridge.WaitEnterPatched == true;
        private bool fishingWaitPlayPatched => fishingAutomationFeature?.HookBridge.WaitPlayPatched == true;
        private bool fishingMiniGameStartPatched => fishingAutomationFeature?.HookBridge.MiniGameStartPatched == true;
        private bool fishingMiniGameUpdatePatched => fishingAutomationFeature?.HookBridge.MiniGameUpdatePatched == true;
        private bool fishingMiniGameStopPatched => fishingAutomationFeature?.HookBridge.MiniGameStopPatched == true;
        private bool fishingPullEnterPatched => fishingAutomationFeature?.HookBridge.PullEnterPatched == true;
        private bool fishingPullExitPatched => fishingAutomationFeature?.HookBridge.PullExitPatched == true;
        private bool fishRoeTitlePatched => fishRoeTooltipFeature?.HookBridge.TitlePatched == true;
        private bool fishRoeDescriptionPatched => fishRoeTooltipFeature?.HookBridge.DescriptionPatched == true;
        private bool fishRoeDetailPatched => fishRoeTooltipFeature?.HookBridge.DetailPatched == true;
        private bool animalFullInfoDataPatched => animalViewerFeature?.HookBridge.FullInfoDataPatched == true;
        private bool animalViewerShowPrefixPatched => animalViewerFeature?.HookBridge.ViewerShowPrefixPatched == true;
        private bool animalViewerShowPatched => animalViewerFeature?.HookBridge.ViewerShowPatched == true;
        private bool animalPanelRefreshViewerPatched => animalViewerFeature?.HookBridge.PanelRefreshViewerPatched == true;
        private bool equipmentRendererReusePatched;
        private bool equipmentBuilderCreateIndicatorPatched;
        private bool equipmentBuilderTurnIndicatorPatched;
        private bool equipmentSlotsReloadParamsPatched;
        private bool equipmentSlotsShieldAttackPatched;
        private bool equipmentSlotsAccessoriesInitPatched;
        private bool equipmentSlotsAccessoriesStartShowPatched;
        private bool advancedCreativeCostEnergyPatched;
        private bool advancedCreativeCostToolEnergyPatched;
        private bool advancedCreativeHasEnoughEnergyPatched;
        private bool advancedCreativeHasEnoughToolEnergyPatched;
        private bool advancedCreativeCostItemStringPatched;
        private bool advancedCreativeCostItemObjectPatched;
        private bool advancedCreativeCostItemNoCheckListPatched;
        private bool advancedCreativeCostItemNoCheckStringPatched;
        private bool advancedCreativeCostSelectedItemDefaultPatched;
        private bool advancedCreativeCostSelectedItemAtPatched;
        private bool advancedCreativeCostItemAtPatched;
        private bool advancedCreativeCanAffordDefaultPatched;
        private bool advancedCreativeCanAffordScaledPatched;
        private bool advancedCreativeCanAffordMoneyPatched;
        private bool advancedCreativeRecipeTimePatched;
        private bool hookResolutionDiagnosticLogged;
        private bool uiContextDiagnosticLogged;
        private Delegate? saveLoadedUnityEventDelegate;
        private readonly List<IGameBridgeFeature> features = new List<IGameBridgeFeature>();
        private readonly Dictionary<string, GameBridgeFeatureStatus> featureStatuses = new Dictionary<string, GameBridgeFeatureStatus>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GameBridgeFeatureFailureState> featureFailures = new Dictionary<string, GameBridgeFeatureFailureState>(StringComparer.OrdinalIgnoreCase);
        private DolocTownExperimentalBridgeApi? experimentalApi;
        private CameraFeature? cameraFeature;
        private FishingAutomationFeature? fishingAutomationFeature;
        private FishRoeTooltipFeature? fishRoeTooltipFeature;
        private ChestLocatorEnhancerFeature? chestLocatorEnhancerFeature;
        private SaveSlotsFeature? saveSlotsFeature;
        private NativeUiLayoutDiagnosticsFeature? nativeUiLayoutDiagnosticsFeature;
        private StrongPlantingGunFeature? strongPlantingGunFeature;
        private CropHarvestingFeature? cropHarvestingFeature;
        private AnimalViewerFeature? animalViewerFeature;
        private OilCoalDropFeature? oilCoalDropFeature;
        private AgentStateLifecycleHookBridge? agentStateLifecycleHooks;
        private ToolColliderHitHookBridge? toolColliderHitHooks;
        private ActionSpeedFeature? actionSpeedFeature;
        private ActionCompletionFeature? actionCompletionFeature;

        public DolocTownGameBridge(DtmApiRuntime runtime, Func<bool>? clickTitleSettingsButton = null, IDebugConsoleApi? debugConsoleApi = null)
        {
            this.runtime = runtime;
            this.clickTitleSettingsButton = clickTitleSettingsButton;
            this.debugConsoleApi = debugConsoleApi;
            RegisterExperimentalApis();
        }

        internal DolocTownExperimentalBridgeApi? ExperimentalApi => experimentalApi;

        internal CameraFeature? CameraFeature => cameraFeature;

        internal FishingAutomationService? FishingAutomationService => fishingAutomationFeature?.Service;

        internal FishRoeTooltipService? FishRoeTooltipService => fishRoeTooltipFeature?.Service;

        internal ChestLocatorEnhancerService? ChestLocatorEnhancerService => chestLocatorEnhancerFeature?.Service;

        internal SaveSlotsService? SaveSlotsService => saveSlotsFeature?.Service;

        internal NativeUiLayoutDiagnosticsService? NativeUiLayoutDiagnosticsService => nativeUiLayoutDiagnosticsFeature?.Service;

        internal StrongPlantingGunService? StrongPlantingGunService => strongPlantingGunFeature?.Service;

        internal CropHarvestingService? CropHarvestingService => cropHarvestingFeature?.Service;

        internal AnimalViewerService? AnimalViewerService => animalViewerFeature?.Service;

        internal OilCoalDropService? OilCoalDropService => oilCoalDropFeature?.Service;

        internal ActionSpeedService? ActionSpeedService => actionSpeedFeature?.Service;

        internal ActionCompletionService? ActionCompletionService => actionCompletionFeature?.Service;

        public void Initialize()
        {
            initializedAt = DateTimeOffset.Now;
            DolocTownHookCallbacks.Runtime = runtime;
            DolocTownHookCallbacks.Bridge = this;
            experimentalApi?.PublishHookStatuses();
            PublishGameBridgeFeatureHookStatuses();
            PublishCustomEntityRegistryContractHookStatuses();
            runtime.SetHookStatus("GameLoop.UpdateTicked", "verified", "BepInEx MonoBehaviour.Update", "DTMAPI dispatches UpdateTicked from the bootstrap Update callback.");
            runtime.SetHookStatus("GameLoop.OneSecondUpdateTicked", "verified", "DTMAPI.Core timer", "DTMAPI dispatches a throttled one-second event from Update.");
            InstallHarmonyHooks();
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            hookRetryTimer = new Timer(_ => InstallHarmonyHooks(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
            LoadSmokeSettings();
        }

        private void RegisterExperimentalApis()
        {
            if (experimentalApi != null && cameraFeature != null && fishingAutomationFeature != null && fishRoeTooltipFeature != null && chestLocatorEnhancerFeature != null && saveSlotsFeature != null && nativeUiLayoutDiagnosticsFeature != null && strongPlantingGunFeature != null && cropHarvestingFeature != null && animalViewerFeature != null && oilCoalDropFeature != null && actionSpeedFeature != null && actionCompletionFeature != null)
                return;
            experimentalApi ??= new DolocTownExperimentalBridgeApi(runtime);
            EnsureGameBridgeFeatures();
            experimentalApi.AttachActionSpeedService(actionSpeedFeature!.Service);
            var manifest = new ManifestModel
            {
                Name = "DTMAPI Doloc Town GameBridge",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.GameBridge.DolocTown",
                Type = "RuntimeApi"
            };
            runtime.RegisterRuntimeApi<IInventoryDebugApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IMailDeliveryApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IWeatherDebugApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<ITeleportDebugApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IInstantSaveDebugApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<ITimeDebugApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IMovementDebugApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IMachineProductionApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IEquipmentSlotsApi>(manifest, experimentalApi);
            RegisterGameBridgeFeatureApis(manifest);
            runtime.RegisterRuntimeApi<IAdvancedDebugApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<ICustomAnimalApi>(manifest, runtime.CustomEntities);
            runtime.RegisterRuntimeApi<ICustomMonsterApi>(manifest, runtime.CustomEntities);
            runtime.RegisterRuntimeApi<ICustomAttackApi>(manifest, runtime.CustomEntities);
            runtime.RegisterRuntimeApi<ICustomDroneApi>(manifest, runtime.CustomEntities);
        }

        internal void UpdateRuntimeAutomation(bool forceMachineProductionPoll = false)
        {
            experimentalApi?.UpdateRuntimeAutomation(forceMachineProductionPoll);
            ProcessPendingDtmapiUploadPlanFallbacks();
            UpdateGameBridgeFeatures();
        }

        internal void NotifyGameBridgeFeaturesSaveLoaded(bool isNewGame)
        {
            DispatchGameBridgeFeatures("SaveLoaded", feature => feature.SaveLoaded(isNewGame));
        }

        internal void NotifyGameBridgeFeaturesReturnedToTitle()
        {
            DispatchGameBridgeFeatures("ReturnedToTitle", feature => feature.ReturnedToTitle());
        }

        internal void NotifyGameBridgeFeaturesEnvironmentReset(string reason)
        {
            experimentalApi?.UpdateRuntimeAutomation(forceMachineProductionPoll: true);
            DispatchGameBridgeFeatures("EnvironmentReset", feature => feature.EnvironmentReset(reason));
        }

        private void EnsureGameBridgeFeatures()
        {
            cameraFeature ??= new CameraFeature(runtime);
            if (!features.Contains(cameraFeature))
                features.Add(cameraFeature);

            fishingAutomationFeature ??= new FishingAutomationFeature(runtime);
            if (!features.Contains(fishingAutomationFeature))
                features.Add(fishingAutomationFeature);

            fishRoeTooltipFeature ??= new FishRoeTooltipFeature(runtime);
            if (!features.Contains(fishRoeTooltipFeature))
                features.Add(fishRoeTooltipFeature);

            chestLocatorEnhancerFeature ??= new ChestLocatorEnhancerFeature(runtime);
            if (!features.Contains(chestLocatorEnhancerFeature))
                features.Add(chestLocatorEnhancerFeature);

            saveSlotsFeature ??= new SaveSlotsFeature(runtime);
            if (!features.Contains(saveSlotsFeature))
                features.Add(saveSlotsFeature);

            nativeUiLayoutDiagnosticsFeature ??= new NativeUiLayoutDiagnosticsFeature(runtime);
            if (!features.Contains(nativeUiLayoutDiagnosticsFeature))
                features.Add(nativeUiLayoutDiagnosticsFeature);

            strongPlantingGunFeature ??= new StrongPlantingGunFeature(runtime);
            if (!features.Contains(strongPlantingGunFeature))
                features.Add(strongPlantingGunFeature);

            cropHarvestingFeature ??= new CropHarvestingFeature(runtime);
            if (!features.Contains(cropHarvestingFeature))
                features.Add(cropHarvestingFeature);

            animalViewerFeature ??= new AnimalViewerFeature(runtime);
            if (!features.Contains(animalViewerFeature))
                features.Add(animalViewerFeature);

            agentStateLifecycleHooks ??= new AgentStateLifecycleHookBridge();
            toolColliderHitHooks ??= new ToolColliderHitHookBridge();

            oilCoalDropFeature ??= new OilCoalDropFeature(runtime, () => oilCoalDropRoutePatched);

            actionSpeedFeature ??= new ActionSpeedFeature(runtime, agentStateLifecycleHooks);
            if (!features.Contains(actionSpeedFeature))
                features.Add(actionSpeedFeature);

            actionCompletionFeature ??= new ActionCompletionFeature(runtime, oilCoalDropFeature.Service.TryRollOilDropFromCoal, () => toolColliderHitPostfixPatched, () => agentStateLifecycleHooks?.InteractExitPatched == true);
            if (!features.Contains(actionCompletionFeature))
                features.Add(actionCompletionFeature);

            if (!features.Contains(oilCoalDropFeature))
                features.Add(oilCoalDropFeature);
        }

        private void RegisterGameBridgeFeatureApis(IManifest manifest)
        {
            DispatchGameBridgeFeatures("RegisterApis", feature => feature.RegisterApis(manifest));
        }

        private void PublishGameBridgeFeatureHookStatuses()
        {
            DispatchGameBridgeFeatures("PublishHookStatuses", feature => feature.PublishHookStatuses());
        }

        private void InstallGameBridgeFeatureHooks(HarmonyReflectionPatcher patcher)
        {
            agentStateLifecycleHooks?.InstallHooks(patcher);
            toolColliderHitHooks?.InstallHooks(patcher);
            DispatchGameBridgeFeatures("InstallHooks", feature => feature.InstallHooks(patcher));
        }

        private void UpdateGameBridgeFeatures()
        {
            DispatchGameBridgeFeatures("Update", feature => feature.Update());
        }

        private void DispatchGameBridgeFeatures(string operation, Action<IGameBridgeFeature> action)
        {
            foreach (IGameBridgeFeature feature in features)
                DispatchGameBridgeFeature(feature, operation, action);
        }

        private void DispatchGameBridgeFeature(IGameBridgeFeature feature, string operation, Action<IGameBridgeFeature> action)
        {
            string id = GetGameBridgeFeatureId(feature);
            try
            {
                action(feature);
                GameBridgeFeatureStatus status = RecordGameBridgeFeatureSuccess(id, operation);
                string details = FormatGameBridgeFeatureStatus(status);
                PublishGameBridgeFeatureStatusIfNeeded(
                    status,
                    "ready",
                    operation,
                    "Safe feature host dispatch completed " + operation + " for this GameBridge feature. " + details);
            }
            catch (Exception ex)
            {
                GameBridgeFeatureStatus status = RecordGameBridgeFeatureDispatchFailure(id, operation, ex, out GameBridgeFeatureFailurePublication publication);
                string details = FormatGameBridgeFeatureStatus(status);
                PublishGameBridgeFeatureStatusIfNeeded(
                    status,
                    "failed",
                    operation,
                    operation + " failed: " + ex.GetType().Name + ": " + ex.Message + ". " + details,
                    publication.ShouldPublishHookStatus);
            }
        }

        private void PublishGameBridgeFeatureStatusIfNeeded(GameBridgeFeatureStatus status, string hookStatus, string operation, string details)
        {
            PublishGameBridgeFeatureStatusIfNeeded(status, hookStatus, operation, details, forceHookStatusPublication: false);
        }

        private void PublishGameBridgeFeatureStatusIfNeeded(GameBridgeFeatureStatus status, string hookStatus, string operation, string details, bool forceHookStatusPublication)
        {
            DateTimeOffset now = DateTimeOffset.Now;
            runtime.Diagnostics.SetFeatureStatus(status.Id, hookStatus, status.LastOperation, status.LastSucceeded, status.FailureCount, status.LastError, FormatGameBridgeFeatureStatus(status));
            if (!ShouldPublishGameBridgeFeatureStatus(status, hookStatus, operation, now, forceHookStatusPublication))
                return;

            runtime.SetHookStatus(
                "Feature." + status.Id,
                hookStatus,
                "DTMAPI.GameBridge.DolocTown feature host",
                details);
            status.MarkPublished(hookStatus, now);
        }

        private static bool ShouldPublishGameBridgeFeatureStatus(GameBridgeFeatureStatus status, string hookStatus, string operation, DateTimeOffset now, bool forceHookStatusPublication)
        {
            if (!status.HasPublished)
                return true;

            if (forceHookStatusPublication)
                return true;

            if (!string.Equals(status.PublishedHookStatus, hookStatus, StringComparison.OrdinalIgnoreCase))
                return true;

            if (status.PublishedSucceeded != status.LastSucceeded)
                return true;

            if (!string.Equals(operation, "Update", StringComparison.OrdinalIgnoreCase))
                return true;

            return now - status.LastPublishedAt >= FeatureStatusPublishHeartbeat;
        }

        private GameBridgeFeatureStatus RecordGameBridgeFeatureSuccess(string id, string operation)
        {
            GameBridgeFeatureStatus status = GetGameBridgeFeatureStatus(id);
            bool recovered = status.ConsecutiveFailureCount > 0 || (!status.LastSucceeded && status.FailureCount > 0);
            status.LastOperation = operation;
            status.LastSucceeded = true;
            status.ConsecutiveFailureCount = 0;
            status.LastError = string.Empty;
            if (recovered)
                status.LastRecoveredAtUtc = DateTimeOffset.UtcNow;
            RecordGameBridgeFeatureRecoverySuccess(id, operation);
            return status;
        }

        private GameBridgeFeatureStatus RecordGameBridgeFeatureFailure(string id, string operation, Exception ex)
        {
            GameBridgeFeatureStatus status = GetGameBridgeFeatureStatus(id);
            status.LastOperation = operation;
            status.LastSucceeded = false;
            status.FailureCount++;
            status.ConsecutiveFailureCount++;
            status.LastError = ex.GetType().Name + ": " + ex.Message;
            return status;
        }

        private GameBridgeFeatureStatus RecordGameBridgeFeatureDispatchFailure(string id, string operation, Exception ex, out GameBridgeFeatureFailurePublication publication)
        {
            GameBridgeFeatureStatus status = RecordGameBridgeFeatureFailure(id, operation, ex);
            publication = RecordGameBridgeFeatureFailurePublication(id, operation, ex);
            string message = "GameBridge feature '" + id + "' failed during " + operation + ".";

            if (publication.RecordDiagnosticsError)
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge.Feature." + id, message, ex.ToString());

            if (publication.LogMode == GameBridgeFeatureFailureLogMode.Full)
                runtime.RuntimeMonitor.Log(message + " " + ex.GetType().Name + ": " + ex.Message, LogLevel.Error);
            else if (publication.LogMode == GameBridgeFeatureFailureLogMode.Short)
                runtime.RuntimeMonitor.Log("Repeated GameBridge feature failure feature=" + id + " operation=" + operation + " count=" + publication.Count.ToString(CultureInfo.InvariantCulture) + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            else if (publication.LogMode == GameBridgeFeatureFailureLogMode.Summary)
                runtime.RuntimeMonitor.Log("Throttled GameBridge feature failures feature=" + id + " operation=" + operation + " count=" + publication.Count.ToString(CultureInfo.InvariantCulture) + " lastError=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);

            return status;
        }

        private GameBridgeFeatureFailurePublication RecordGameBridgeFeatureFailurePublication(string id, string operation, Exception ex)
        {
            string key = GetGameBridgeFeatureFailureKey(id, operation);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (!featureFailures.TryGetValue(key, out GameBridgeFeatureFailureState state))
            {
                state = new GameBridgeFeatureFailureState();
                featureFailures[key] = state;
            }

            state.Count++;
            state.ConsecutiveSuccessCount = 0;
            state.LastError = ex.GetType().Name + ": " + ex.Message;
            state.LastSeenAtUtc = now;

            if (state.Count == 1)
            {
                state.LastPublishedAtUtc = now;
                return new GameBridgeFeatureFailurePublication(true, GameBridgeFeatureFailureLogMode.Full, state.Count);
            }

            if (state.Count <= FeatureFailureShortLogLimit)
            {
                state.LastPublishedAtUtc = now;
                return new GameBridgeFeatureFailurePublication(false, GameBridgeFeatureFailureLogMode.Short, state.Count);
            }

            if (now - state.LastPublishedAtUtc >= FeatureFailureSummaryInterval)
            {
                state.LastPublishedAtUtc = now;
                return new GameBridgeFeatureFailurePublication(false, GameBridgeFeatureFailureLogMode.Summary, state.Count);
            }

            return new GameBridgeFeatureFailurePublication(false, GameBridgeFeatureFailureLogMode.None, state.Count);
        }

        private void RecordGameBridgeFeatureRecoverySuccess(string id, string operation)
        {
            string key = GetGameBridgeFeatureFailureKey(id, operation);
            if (!featureFailures.TryGetValue(key, out GameBridgeFeatureFailureState state))
                return;

            state.ConsecutiveSuccessCount++;
            if (state.ConsecutiveSuccessCount >= FeatureFailureRecoverySuccessThreshold)
                featureFailures.Remove(key);
        }

        private static string GetGameBridgeFeatureFailureKey(string id, string operation)
        {
            return (string.IsNullOrWhiteSpace(id) ? "<unknown>" : id.Trim()) + "::" + (string.IsNullOrWhiteSpace(operation) ? "<unknown>" : operation.Trim());
        }

        private GameBridgeFeatureStatus GetGameBridgeFeatureStatus(string id)
        {
            if (!featureStatuses.TryGetValue(id, out GameBridgeFeatureStatus status))
            {
                status = new GameBridgeFeatureStatus(id);
                featureStatuses[id] = status;
            }

            return status;
        }

        private static string FormatGameBridgeFeatureStatus(GameBridgeFeatureStatus status)
        {
            string lastError = string.IsNullOrWhiteSpace(status.LastError) ? "none" : status.LastError;
            string recoveredAt = status.LastRecoveredAtUtc.HasValue ? status.LastRecoveredAtUtc.Value.ToString("O", CultureInfo.InvariantCulture) : "none";
            return "Feature status: id=" + status.Id + ", lastOperation=" + status.LastOperation + ", success=" + status.LastSucceeded.ToString(CultureInfo.InvariantCulture) + ", failureCount=" + status.FailureCount.ToString(CultureInfo.InvariantCulture) + ", consecutiveFailureCount=" + status.ConsecutiveFailureCount.ToString(CultureInfo.InvariantCulture) + ", lastRecoveredAt=" + recoveredAt + ", lastError=" + lastError + ".";
        }

        private static string GetGameBridgeFeatureId(IGameBridgeFeature feature)
        {
            string id = feature.Id;
            if (!string.IsNullOrWhiteSpace(id))
                return id.Trim();

            return feature.GetType().Name;
        }

        private sealed class GameBridgeFeatureStatus
        {
            internal GameBridgeFeatureStatus(string id)
            {
                Id = id;
            }

            internal string Id { get; }

            internal string LastOperation { get; set; } = string.Empty;

            internal bool LastSucceeded { get; set; }

            internal int FailureCount { get; set; }

            internal int ConsecutiveFailureCount { get; set; }

            internal string LastError { get; set; } = string.Empty;

            internal DateTimeOffset? LastRecoveredAtUtc { get; set; }

            internal bool HasPublished { get; private set; }

            internal string PublishedHookStatus { get; private set; } = string.Empty;

            internal bool PublishedSucceeded { get; private set; }

            internal DateTimeOffset LastPublishedAt { get; private set; } = DateTimeOffset.MinValue;

            internal void MarkPublished(string hookStatus, DateTimeOffset publishedAt)
            {
                HasPublished = true;
                PublishedHookStatus = hookStatus;
                PublishedSucceeded = LastSucceeded;
                LastPublishedAt = publishedAt;
            }
        }

        private sealed class GameBridgeFeatureFailureState
        {
            internal int Count { get; set; }
            internal int ConsecutiveSuccessCount { get; set; }
            internal string LastError { get; set; } = string.Empty;
            internal DateTimeOffset LastSeenAtUtc { get; set; }
            internal DateTimeOffset LastPublishedAtUtc { get; set; }
        }

        private readonly struct GameBridgeFeatureFailurePublication
        {
            internal GameBridgeFeatureFailurePublication(bool recordDiagnosticsError, GameBridgeFeatureFailureLogMode logMode, int count)
            {
                RecordDiagnosticsError = recordDiagnosticsError;
                LogMode = logMode;
                Count = count;
            }

            internal bool RecordDiagnosticsError { get; }

            internal GameBridgeFeatureFailureLogMode LogMode { get; }

            internal int Count { get; }

            internal bool ShouldPublishHookStatus => LogMode != GameBridgeFeatureFailureLogMode.None;
        }

        private enum GameBridgeFeatureFailureLogMode
        {
            None,
            Full,
            Short,
            Summary
        }

        private sealed class PendingWorkshopUploadPlanResolution
        {
            public PendingWorkshopUploadPlanResolution(object uploader, object callback, ulong workshopId, string modId, DateTimeOffset startedAtUtc)
            {
                Uploader = uploader;
                Callback = callback;
                WorkshopId = workshopId;
                ModId = modId;
                StartedAtUtc = startedAtUtc;
            }

            public object Uploader { get; }

            public object Callback { get; }

            public ulong WorkshopId { get; }

            public string ModId { get; }

            public DateTimeOffset StartedAtUtc { get; }
        }

        private void PublishCustomEntityRegistryContractHookStatuses()
        {
            runtime.SetHookStatus(
                "CustomEntities.CoreRegistry",
                "verified",
                "DTMAPI.Core.CustomEntityRegistryService",
                "StableCandidate 0.4.0 custom entity registry contracts are registered with owner-aware validation, duplicate-ID detection, snapshots, save-boundary cleanup, and provider error isolation.");
            runtime.SetHookStatus(
                "CustomAnimals.RegistryContract",
                "configured-blocked",
                "AnimalManager.CreateAnimal + Animal lifecycle research",
                "StableCandidate registry contract; runtime creation remains blocked");
            runtime.SetHookStatus(
                "CustomMonsters.RegistryContract",
                "configured-blocked",
                "MonsterController + MonsterGroupManager + MonsterAttackBehaviour research",
                "StableCandidate registry contract; runtime creation remains blocked");
            runtime.SetHookStatus(
                "CustomAttacks.RegistryContract",
                "configured-blocked",
                "BulletFactory + BulletManager + PhysicalDamageBox research",
                "StableCandidate registry contract; runtime creation remains blocked");
            runtime.SetHookStatus(
                "CustomDrones.RegistryContract",
                "configured-blocked",
                "DroneController + DroneWeapon + DolocAPI.EquipDrone research",
                "StableCandidate registry contract; runtime creation remains blocked");
        }

        private void RefreshUiContext()
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                {
                    runtime.UI.SetUiContext("Unknown", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "DolocAPI not visible yet.");
                    return;
                }

                if (TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    runtime.UI.SetUiContext("Gameplay", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "DolocAPI.IsNormalState.");
                    return;
                }

                // HomePageUiState can remain in the state stack behind official title-page
                // panels. Prefer blocking panels first so the DTMAPI title button hides
                // when Doloc Town owns the visible UI.
                string context =
                    FirstActiveUiState(dolocApi,
                        "DolocTown.ModUiState, Assembly-CSharp",
                        "DolocTown.SettingPanelUiState, Assembly-CSharp",
                        "DolocTown.GameDataUiState, Assembly-CSharp",
                        "DolocTown.ModChangeListUiState, Assembly-CSharp",
                        "DolocTown.MainMenuUiState, Assembly-CSharp",
                        "DolocTown.SystemMenuUiState, Assembly-CSharp",
                        "DolocTown.SmallTextMenuUiState, Assembly-CSharp",
                        "DolocTown.ConfirmUiState, Assembly-CSharp",
                        "DolocTown.HomePageUiState, Assembly-CSharp") ?? "Gameplay";

                bool gameplayHotkeysAllowed = context.Equals("Gameplay", StringComparison.OrdinalIgnoreCase);
                runtime.UI.SetUiContext(context, canDrawOverlay: true, gameplayHotkeysAllowed: gameplayHotkeysAllowed, reason: "Active Doloc Town UI state.");
            }
            catch (Exception ex)
            {
                if (!uiContextDiagnosticLogged)
                {
                    uiContextDiagnosticLogged = true;
                    runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to refresh UI input context.", ex.ToString());
                }
                runtime.UI.SetUiContext("Unknown", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "UI context query failed once; see diagnostics.");
            }
        }

        private static bool TryGetStaticBoolProperty(Type type, string propertyName)
        {
            try
            {
                PropertyInfo? property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
                object? value = property?.GetValue(null, null);
                return value is bool result && result;
            }
            catch
            {
                return false;
            }
        }

        internal void TryMarkDtmapiLocalUploadData(object? modData, object? modInfo)
        {
            try
            {
                if (modData == null || modInfo == null)
                    return;

                object? source = ReadInstanceMember(modInfo, "source");
                if (!IsOfficialLocalModSource(source))
                    return;

                ulong workshopId = ConvertToUInt64(ReadInstanceMember(modInfo, "workshopId"));
                if (workshopId == 0)
                    return;

                string? rootPath = Convert.ToString(ReadInstanceMember(modInfo, "rootPath"), CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(rootPath) || !IsDtmapiGeneratedLocalModRoot(rootPath))
                    return;

                SetInstanceMember(modData, "canUpdateWorkshopItem", true);
                string modId = Convert.ToString(ReadInstanceMember(modInfo, "id"), CultureInfo.InvariantCulture) ?? "<unknown>";
                runtime.SetHookStatus(
                    "Workshop.LocalUploadPlan",
                    "verified",
                    "Harmony Postfix: ModData..ctor",
                    "DTMAPI-generated local package " + modId + " displays Update from workshop.json workshopId=" + workshopId.ToString(CultureInfo.InvariantCulture) + "; native Steam ResolveLocalModUploadPlan still owns upload execution.");
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to mark DTMAPI local Workshop upload display; native upload resolver is unchanged.", ex.ToString());
            }
        }

        internal bool TryResolveDtmapiUploadPlanIfNativeBusy(object? uploader, object? modInfo, object? onResolved)
        {
            try
            {
                if (uploader == null || modInfo == null || onResolved == null)
                    return true;

                if (!TryReadDtmapiKnownLocalWorkshopRequest(modInfo, out ulong workshopId, out string modId, out _))
                    return true;

                bool isBusy = ConvertToBoolean(ReadInstanceMember(uploader, "IsBusy")) ||
                    ConvertToBoolean(ReadInstanceMember(uploader, "IsUploading")) ||
                    ConvertToBoolean(ReadInstanceMember(uploader, "IsResolvingUploadPlan"));
                if (!isBusy)
                    return true;

                object? plan = CreateNativeWorkshopUploadPlan("Update", workshopId);
                if (plan == null)
                    return true;

                InvokeWorkshopUploadPlanCallback(onResolved, plan);

                runtime.SetHookStatus(
                    "Workshop.LocalUploadPlanBusyFallback",
                    "busy-fallback",
                    "Harmony Prefix: SteamWorkshopUploader.ResolveUploadPlan",
                    "Native uploader was busy while resolving " + modId + "; returned an Update plan for workshopId=" + workshopId.ToString(CultureInfo.InvariantCulture) +
                    ". Upload execution still uses native SteamWorkshopUploader.");
                return false;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to complete DTMAPI local Workshop upload-plan busy fallback; native resolver will continue.", ex.ToString());
                return true;
            }
        }

        internal void TrackDtmapiUploadPlanFallbackAfterNativeQuery(object? uploader, object? modInfo, object? onResolved)
        {
            try
            {
                if (uploader == null || modInfo == null || onResolved == null)
                    return;

                if (!TryReadDtmapiKnownLocalWorkshopRequest(modInfo, out ulong workshopId, out string modId, out _))
                    return;

                if (!ConvertToBoolean(ReadInstanceMember(uploader, "IsResolvingUploadPlan")))
                    return;

                object? currentCallback = ReadInstanceMember(uploader, "resolveUploadPlanCallback");
                if (currentCallback == null || !ReferenceEquals(currentCallback, onResolved))
                    return;

                ulong pendingWorkshopId = ConvertToUInt64(ReadInstanceMember(uploader, "pendingWorkshopId"));
                if (pendingWorkshopId != workshopId)
                    return;

                for (int i = pendingWorkshopUploadPlanResolutions.Count - 1; i >= 0; i--)
                {
                    PendingWorkshopUploadPlanResolution existing = pendingWorkshopUploadPlanResolutions[i];
                    if (ReferenceEquals(existing.Uploader, uploader) && ReferenceEquals(existing.Callback, onResolved))
                    {
                        pendingWorkshopUploadPlanResolutions[i] = new PendingWorkshopUploadPlanResolution(uploader, onResolved, workshopId, modId, DateTimeOffset.UtcNow);
                        return;
                    }
                }

                pendingWorkshopUploadPlanResolutions.Add(new PendingWorkshopUploadPlanResolution(uploader, onResolved, workshopId, modId, DateTimeOffset.UtcNow));
                runtime.SetHookStatus(
                    "Workshop.LocalUploadPlanKnownIdFallback",
                    "watching",
                    "Harmony Postfix: SteamWorkshopUploader.ResolveUploadPlan",
                    "Native Steam details query started for DTMAPI-generated local package " + modId + "; fallback will only use workshop.json id=" + workshopId.ToString(CultureInfo.InvariantCulture) + " if the same callback remains unresolved after a short delay.");
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to track DTMAPI local Workshop upload-plan delayed fallback.", ex.ToString());
            }
        }

        private void ProcessPendingDtmapiUploadPlanFallbacks()
        {
            if (pendingWorkshopUploadPlanResolutions.Count == 0)
                return;

            DateTimeOffset now = DateTimeOffset.UtcNow;
            for (int i = pendingWorkshopUploadPlanResolutions.Count - 1; i >= 0; i--)
            {
                PendingWorkshopUploadPlanResolution pending = pendingWorkshopUploadPlanResolutions[i];
                if ((now - pending.StartedAtUtc).TotalSeconds < 4)
                    continue;

                if (!IsSamePendingWorkshopUploadPlanResolution(pending))
                {
                    pendingWorkshopUploadPlanResolutions.RemoveAt(i);
                    continue;
                }

                object? plan = CreateNativeWorkshopUploadPlan("Update", pending.WorkshopId);
                if (plan == null)
                {
                    pendingWorkshopUploadPlanResolutions.RemoveAt(i);
                    runtime.SetHookStatus(
                        "Workshop.LocalUploadPlanKnownIdFallback",
                        "pending",
                        "Harmony Postfix: SteamWorkshopUploader.ResolveUploadPlan",
                        "Native Steam details query remained unresolved for " + pending.ModId + ", but DTMAPI could not construct a native WorkshopUploadPlan; native resolver remains owner.");
                    continue;
                }

                SetInstanceMember(pending.Uploader, "resolveUploadPlanCallback", null);
                SetInstanceMember(pending.Uploader, "pendingWorkshopId", 0uL);
                InvokeWorkshopUploadPlanCallback(pending.Callback, plan);
                pendingWorkshopUploadPlanResolutions.RemoveAt(i);

                runtime.SetHookStatus(
                    "Workshop.LocalUploadPlanKnownIdFallback",
                    "delayed-fallback",
                    "DTMAPI.GameBridge.DolocTown.Update",
                    "Native Steam details query did not resolve for DTMAPI-generated local package " + pending.ModId + " within 4 seconds; used known workshop.json id=" + pending.WorkshopId.ToString(CultureInfo.InvariantCulture) + " to release the official ModManager queue. Upload execution remains native-owned.");
            }
        }

        private bool IsSamePendingWorkshopUploadPlanResolution(PendingWorkshopUploadPlanResolution pending)
        {
            try
            {
                object? currentCallback = ReadInstanceMember(pending.Uploader, "resolveUploadPlanCallback");
                if (currentCallback == null || !ReferenceEquals(currentCallback, pending.Callback))
                    return false;

                if (!ConvertToBoolean(ReadInstanceMember(pending.Uploader, "IsResolvingUploadPlan")))
                    return false;

                ulong currentWorkshopId = ConvertToUInt64(ReadInstanceMember(pending.Uploader, "pendingWorkshopId"));
                return currentWorkshopId == pending.WorkshopId;
            }
            catch
            {
                return false;
            }
        }

        private static void InvokeWorkshopUploadPlanCallback(object onResolved, object plan)
        {
            if (onResolved is Delegate callback)
            {
                callback.DynamicInvoke(plan);
                return;
            }

            onResolved.GetType().GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance)?.Invoke(onResolved, new[] { plan });
        }

        private static bool TryReadDtmapiKnownLocalWorkshopRequest(object modInfo, out ulong workshopId, out string modId, out string rootPath)
        {
            workshopId = 0;
            modId = Convert.ToString(ReadInstanceMember(modInfo, "id"), CultureInfo.InvariantCulture) ?? "<unknown>";
            rootPath = string.Empty;

            object? source = ReadInstanceMember(modInfo, "source");
            if (!IsOfficialLocalModSource(source))
                return false;

            workshopId = ConvertToUInt64(ReadInstanceMember(modInfo, "workshopId"));
            if (workshopId == 0)
                return false;

            rootPath = Convert.ToString(ReadInstanceMember(modInfo, "rootPath"), CultureInfo.InvariantCulture) ?? string.Empty;
            return !string.IsNullOrWhiteSpace(rootPath) && IsDtmapiGeneratedLocalModRoot(rootPath);
        }

        private object? CreateNativeWorkshopUploadPlan(string modeName, ulong workshopId)
        {
            try
            {
                Type? planType = patcher?.ResolveType("DolocTown.Config.WorkshopUploadPlan, Assembly-CSharp");
                Type? modeType = patcher?.ResolveType("DolocTown.Config.WorkshopUploadMode, Assembly-CSharp");
                if (planType == null || modeType == null)
                    return null;

                object mode = Enum.Parse(modeType, modeName);
                return Activator.CreateInstance(planType, mode, workshopId);
            }
            catch
            {
                return null;
            }
        }

        private static bool IsOfficialLocalModSource(object? source)
        {
            return string.Equals(Convert.ToString(source, CultureInfo.InvariantCulture), "Local", StringComparison.Ordinal);
        }

        private static bool IsDtmapiGeneratedLocalModRoot(string rootPath)
        {
            try
            {
                string runtimeMarkerPath = Path.Combine(rootPath, "Content", "DTMAPI", "release-manifest.json");
                if (File.Exists(runtimeMarkerPath))
                    return true;

                string markerPath = Path.Combine(rootPath, "Content", "DTMAPI", "dtmapi-package.json");
                if (!File.Exists(markerPath))
                    return false;
                string markerJson = File.ReadAllText(markerPath);
                return markerJson.IndexOf("\"owner\"", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    markerJson.IndexOf("DTMAPI", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    markerJson.IndexOf("\"uniqueId\"", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private static object? ReadInstanceMember(object instance, string name)
        {
            Type type = instance.GetType();
            PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null)
                return property.GetValue(instance, null);

            FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(instance);
        }

        private static void SetInstanceMember(object instance, string name, object? value)
        {
            Type type = instance.GetType();
            PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                property.SetValue(instance, value, null);
                return;
            }

            FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(instance, value);
        }

        private static ulong ConvertToUInt64(object? value)
        {
            if (value == null)
                return 0;
            if (value is ulong ulongValue)
                return ulongValue;
            if (value is long longValue && longValue >= 0)
                return (ulong)longValue;
            return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
        }

        private static bool ConvertToBoolean(object? value)
        {
            if (value == null)
                return false;
            if (value is bool boolValue)
                return boolValue;
            return bool.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out bool result) && result;
        }

        private string? FirstActiveUiState(Type dolocApi, params string[] stateTypes)
        {
            foreach (string typeName in stateTypes)
            {
                Type? type = patcher?.ResolveType(typeName);
                if (type == null)
                    continue;
                if (IsUiStateActive(dolocApi, type))
                    return type.Name;
            }
            return null;
        }

        private void InstallHarmonyHooks()
        {
            lock (hookGate)
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                if (!saveLoadedPatched && !saveLoadedEventSubscribed)
                {
                    saveLoadedEventSubscribed = TrySubscribeSaveLoadedUnityEvent(patcher);
                    if (!saveLoadedEventSubscribed)
                        saveLoadedPatched = patcher.TryPatchPostfix("DolocAPI, Assembly-CSharp", "AfterLoadArchiveData", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AfterLoadArchiveDataPostfix), BindingFlags.Public | BindingFlags.Static));

                    string source = saveLoadedEventSubscribed ? "UnityEvent: DolocAPI.OnAfterLoadArchiveData" : "Harmony Postfix: DolocAPI.AfterLoadArchiveData";
                    runtime.SetHookStatus("Save.SaveLoaded", IsSaveLoadedHookReady ? "experimental" : "pending", source, IsSaveLoadedHookReady ? "Hook installed. Requires in-game save evidence before verified." : "Waiting for Assembly-CSharp/DolocAPI to become patchable.");
                }

                if (!loadRequestedPatched)
                {
                    MethodInfo? loadGamePrefix = typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.LoadGamePrefix), BindingFlags.Public | BindingFlags.Static);
                    loadRequestedPatched =
                        patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "LoadGame", loadGamePrefix, 1) ||
                        patcher.TryPatchPrefix("DolocTown.GameData.DataPersistenceManager, Assembly-CSharp", "LoadGame", loadGamePrefix, 1);
                    runtime.SetHookStatus("Save.LoadGameRequested", loadRequestedPatched ? "experimental" : "pending", "Harmony Prefix: LoadGame", loadRequestedPatched ? "Patched for save slot/index evidence." : "Waiting for Assembly-CSharp LoadGame target to become patchable.");
                }

                if (!saveSavingPatched)
                {
                    MethodInfo? saveGamePrefix = typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.SaveGamePrefix), BindingFlags.Public | BindingFlags.Static);
                    saveSavingPatched =
                        patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "SaveGame", saveGamePrefix, 1) ||
                        patcher.TryPatchPrefix("DolocTown.GameData.DataPersistenceManager, Assembly-CSharp", "SaveGame", saveGamePrefix, 1);
                    runtime.SetHookStatus("Save.SaveSaving", saveSavingPatched ? "experimental" : "pending", "Harmony Prefix: SaveGame", saveSavingPatched ? "Patched. Requires save evidence before verified." : "Waiting for Assembly-CSharp SaveGame target to become patchable.");
                }

                if (!saveSavedPatched)
                {
                    MethodInfo? saveGamePostfix = typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.SaveGamePostfix), BindingFlags.Public | BindingFlags.Static);
                    saveSavedPatched =
                        patcher.TryPatchPostfix("DolocAPI, Assembly-CSharp", "SaveGame", saveGamePostfix, 1) ||
                        patcher.TryPatchPostfix("DolocTown.GameData.DataPersistenceManager, Assembly-CSharp", "SaveGame", saveGamePostfix, 1);
                    runtime.SetHookStatus("Save.SaveSaved", saveSavedPatched ? "experimental" : "pending", "Harmony Postfix: SaveGame", saveSavedPatched ? "Patched. Requires save evidence before verified." : "Waiting for Assembly-CSharp SaveGame target to become patchable.");
                }

                if (!returnHomePatched)
                {
                    returnHomePatched = patcher.TryPatchPostfix("DolocAPI, Assembly-CSharp", "ReturnHome", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ReturnHomePostfix), BindingFlags.Public | BindingFlags.Static), 1);
                    runtime.SetHookStatus("GameLoop.ReturnedToTitle", returnHomePatched ? "experimental" : "pending", "Harmony Postfix: DolocAPI.ReturnHome", returnHomePatched ? "Patched ReturnHome; title lifecycle smoke verifies the button remount." : "Waiting for DolocAPI.ReturnHome to become patchable.");
                }

                InstallGameBridgeFeatureHooks(patcher);

                if (!workshopReloadPatched)
                {
                    workshopReloadPatched = patcher.TryPatchPostfix("DolocTown.Config.ModManager, Assembly-CSharp", "ReloadMods", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ReloadModsPostfix), BindingFlags.Public | BindingFlags.Static));
                    runtime.SetHookStatus("Workshop.ReloadMods", workshopReloadPatched ? "experimental" : "pending", "Harmony Postfix: ModManager.ReloadMods", workshopReloadPatched ? "Patched to refresh DTMAPI diagnostics after official reload." : "Waiting for Assembly-CSharp/ModManager to become patchable.");
                }

                if (!workshopLocalUploadDisplayPatched)
                {
                    workshopLocalUploadDisplayPatched = patcher.TryPatchConstructorPostfix("DolocTown.UI.ModData, Assembly-CSharp", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ModDataConstructorPostfix), BindingFlags.Public | BindingFlags.Static), 2);
                    runtime.SetHookStatus("Workshop.LocalUploadPlan", workshopLocalUploadDisplayPatched ? "experimental" : "pending", "Harmony Postfix: ModData..ctor", workshopLocalUploadDisplayPatched ? "Display-only patch keeps DTMAPI-generated local packages on Update when workshop.json is present; native Steam ResolveLocalModUploadPlan still owns upload execution." : "Waiting for Assembly-CSharp/ModData to become patchable.");
                }

                if (!workshopUploadPlanBusyFallbackPatched)
                {
                    workshopUploadPlanBusyFallbackPatched = patcher.TryPatchPrefix("DolocTown.Config.SteamWorkshopUploader, Assembly-CSharp", "ResolveUploadPlan", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.SteamWorkshopUploaderResolveUploadPlanPrefix), BindingFlags.Public | BindingFlags.Static), 2);
                    runtime.SetHookStatus("Workshop.LocalUploadPlanBusyFallback", workshopUploadPlanBusyFallbackPatched ? "experimental" : "pending", "Harmony Prefix: SteamWorkshopUploader.ResolveUploadPlan", workshopUploadPlanBusyFallbackPatched ? "Prevents DTMAPI-generated local package upload-plan resolve requests from stalling ModManager when the native uploader is already busy; upload execution remains native-owned." : "Waiting for Assembly-CSharp/SteamWorkshopUploader.ResolveUploadPlan to become patchable.");
                }

                if (!workshopUploadPlanKnownIdFallbackPatched)
                {
                    workshopUploadPlanKnownIdFallbackPatched = patcher.TryPatchPostfix("DolocTown.Config.SteamWorkshopUploader, Assembly-CSharp", "ResolveUploadPlan", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.SteamWorkshopUploaderResolveUploadPlanPostfix), BindingFlags.Public | BindingFlags.Static), 2);
                    runtime.SetHookStatus("Workshop.LocalUploadPlanKnownIdFallback", workshopUploadPlanKnownIdFallbackPatched ? "experimental" : "pending", "Harmony Postfix + Update watchdog: SteamWorkshopUploader.ResolveUploadPlan", workshopUploadPlanKnownIdFallbackPatched ? "Allows native Steam details resolution first, then releases DTMAPI-generated local package upload-plan requests with the known workshop.json id only if the same callback remains unresolved after a short delay." : "Waiting for Assembly-CSharp/SteamWorkshopUploader.ResolveUploadPlan to become patchable.");
                }

                if (!debugConsoleUseToolPatched)
                {
                    debugConsoleUseToolPatched = patcher.TryPatchPrefix("DolocTown.AgentControllerState, Assembly-CSharp", "UseTool", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentControllerStateUseToolPrefix), BindingFlags.Public | BindingFlags.Static), 1);
                }

                if (!debugConsoleUseItemPatched)
                {
                    debugConsoleUseItemPatched = patcher.TryPatchPrefix("DolocTown.AgentControllerState, Assembly-CSharp", "UseItem", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentControllerStateUseItemPrefix), BindingFlags.Public | BindingFlags.Static), 1);
                }

                if (!debugConsoleEnterUiCheckPatched)
                {
                    debugConsoleEnterUiCheckPatched = patcher.TryPatchPrefix("DolocTown.AgentControllerState, Assembly-CSharp", "EnterUICheck", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentControllerStateEnterUiCheckPrefix), BindingFlags.Public | BindingFlags.Static), 2);
                }

                bool debugConsoleInputHooksReady = debugConsoleUseToolPatched && debugConsoleUseItemPatched && debugConsoleEnterUiCheckPatched;
                runtime.SetHookStatus("UI.DebugConsoleInputIsolation", debugConsoleInputHooksReady ? "experimental" : "pending", "Harmony Prefix: AgentControllerState.EnterUICheck/UseTool/UseItem", debugConsoleInputHooksReady ? "Patched native UI toggles and tool/item entry points; active only while the DTMAPI Y console is open." : "Waiting for AgentControllerState input methods to become patchable.");

                MethodInfo? creativeBoolTruePrefix = typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AdvancedCreativeBoolTruePrefix), BindingFlags.Public | BindingFlags.Static);
                MethodInfo? creativeVoidSkipPrefix = typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AdvancedCreativeVoidSkipPrefix), BindingFlags.Public | BindingFlags.Static);
                MethodInfo? creativeRecipeTimePostfix = typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AdvancedCreativeRecipeTimePostfix), BindingFlags.Public | BindingFlags.Static);
                if (!advancedCreativeCostEnergyPatched)
                    advancedCreativeCostEnergyPatched = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "CostEnergy", creativeBoolTruePrefix, 1);
                if (!advancedCreativeCostToolEnergyPatched)
                    advancedCreativeCostToolEnergyPatched = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "CostToolEnergy", creativeBoolTruePrefix, 0);
                if (!advancedCreativeHasEnoughEnergyPatched)
                    advancedCreativeHasEnoughEnergyPatched = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "HasEnoughEnergy", creativeBoolTruePrefix, 1);
                if (!advancedCreativeHasEnoughToolEnergyPatched)
                    advancedCreativeHasEnoughToolEnergyPatched = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "HasEnoughEnergyForUsingTool", creativeBoolTruePrefix, 0);
                if (!advancedCreativeCostItemStringPatched)
                    advancedCreativeCostItemStringPatched = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "CostItem", creativeBoolTruePrefix, 3);
                if (!advancedCreativeCostItemObjectPatched)
                    advancedCreativeCostItemObjectPatched = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "CostItem", creativeBoolTruePrefix, 4);
                if (!advancedCreativeCostItemNoCheckListPatched)
                    advancedCreativeCostItemNoCheckListPatched = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "CostItemNoCheck", creativeVoidSkipPrefix, 2);
                if (!advancedCreativeCostItemNoCheckStringPatched)
                    advancedCreativeCostItemNoCheckStringPatched = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "CostItemNoCheck", creativeVoidSkipPrefix, 3);
                if (!advancedCreativeCostSelectedItemDefaultPatched)
                    advancedCreativeCostSelectedItemDefaultPatched = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "CostSelectedItem", creativeBoolTruePrefix, 2);
                if (!advancedCreativeCostSelectedItemAtPatched)
                    advancedCreativeCostSelectedItemAtPatched = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "CostSelectedItem", creativeBoolTruePrefix, 3);
                if (!advancedCreativeCostItemAtPatched)
                    advancedCreativeCostItemAtPatched = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "CostItemAt", creativeBoolTruePrefix, 2);
                if (!advancedCreativeCanAffordDefaultPatched)
                    advancedCreativeCanAffordDefaultPatched = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "CanAfford", creativeBoolTruePrefix, 2);
                if (!advancedCreativeCanAffordScaledPatched)
                    advancedCreativeCanAffordScaledPatched = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "CanAfford", creativeBoolTruePrefix, 3);
                if (!advancedCreativeCanAffordMoneyPatched)
                    advancedCreativeCanAffordMoneyPatched = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "CanAffordMoney", creativeBoolTruePrefix, 1);
                if (!advancedCreativeRecipeTimePatched)
                    advancedCreativeRecipeTimePatched = patcher.TryPatchPostfix("DolocTown.Synthesizer, Assembly-CSharp", "GetRecipeTime", creativeRecipeTimePostfix, 2);

                bool advancedCreativeCostHooksReady =
                    advancedCreativeCostEnergyPatched &&
                    advancedCreativeCostToolEnergyPatched &&
                    advancedCreativeHasEnoughEnergyPatched &&
                    advancedCreativeHasEnoughToolEnergyPatched &&
                    advancedCreativeCostItemStringPatched &&
                    advancedCreativeCostItemObjectPatched &&
                    advancedCreativeCostItemNoCheckListPatched &&
                    advancedCreativeCostItemNoCheckStringPatched &&
                    advancedCreativeCostSelectedItemDefaultPatched &&
                    advancedCreativeCostSelectedItemAtPatched &&
                    advancedCreativeCostItemAtPatched &&
                    advancedCreativeCanAffordDefaultPatched &&
                    advancedCreativeCanAffordScaledPatched &&
                    advancedCreativeCanAffordMoneyPatched;
                experimentalApi?.SetAdvancedCreativeHooksInstalled(advancedCreativeCostHooksReady, advancedCreativeRecipeTimePatched);
                runtime.SetHookStatus("Debug.CreativeModeHooks", (advancedCreativeCostHooksReady && advancedCreativeRecipeTimePatched) ? "experimental" : "pending", "Harmony Prefix/Postfix: DolocAPI cost/afford APIs + Synthesizer.GetRecipeTime", (advancedCreativeCostHooksReady && advancedCreativeRecipeTimePatched) ? "Patched no-cost/no-energy checks and synthesizer recipe time for the Y-console creative toggle; GameInitConfig material/shop/spirit flags are applied only while creative mode is enabled." : "Waiting for all advanced creative cost/time targets to become patchable.");

                if (!equipmentRendererReusePatched)
                {
                    equipmentRendererReusePatched = patcher.TryPatchPostfix("DolocTown.EquipmentRenderer, Assembly-CSharp", "OnReuse", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.EquipmentRendererOnReusePostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!equipmentBuilderCreateIndicatorPatched)
                {
                    equipmentBuilderCreateIndicatorPatched = patcher.TryPatchPostfix("DolocTown.EquipmentBuilder, Assembly-CSharp", "CreateIndicator", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.EquipmentBuilderCreateIndicatorPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!equipmentBuilderTurnIndicatorPatched)
                {
                    equipmentBuilderTurnIndicatorPatched = patcher.TryPatchPostfix("DolocTown.EquipmentBuilder, Assembly-CSharp", "TurnIndicator", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.EquipmentBuilderTurnIndicatorPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                bool mineVisualHooksReady = equipmentRendererReusePatched && equipmentBuilderCreateIndicatorPatched && equipmentBuilderTurnIndicatorPatched;
                runtime.SetHookStatus("Machine.MineVisualContainment", mineVisualHooksReady ? "experimental" : "pending", "Harmony: EquipmentRenderer.OnReuse + EquipmentBuilder.CreateIndicator/TurnIndicator", mineVisualHooksReady ? "Equipment renderer pool scale is reset on reuse and Mine placement preview receives Mine-only visual scale." : "Waiting for equipment renderer/builder hook targets to become patchable.");

                if (!equipmentSlotsReloadParamsPatched)
                {
                    equipmentSlotsReloadParamsPatched = patcher.TryPatchPostfix("DolocTown.GameData.AgentEquipmentManager, Assembly-CSharp", "ReloadParams", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentEquipmentReloadParamsPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!equipmentSlotsShieldAttackPatched)
                {
                    equipmentSlotsShieldAttackPatched = patcher.TryPatchPrefix("DolocTown.BodyController, Assembly-CSharp", "OnAttacked", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.BodyControllerOnAttackedPrefix), BindingFlags.Public | BindingFlags.Static), 4);
                }

                if (!equipmentSlotsAccessoriesInitPatched)
                {
                    equipmentSlotsAccessoriesInitPatched = patcher.TryPatchPostfix("DolocTown.UI.AccessoriesBar, Assembly-CSharp", "__Init", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AccessoriesBarInitPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!equipmentSlotsAccessoriesStartShowPatched)
                {
                    equipmentSlotsAccessoriesStartShowPatched = patcher.TryPatchPostfix("DolocTown.UI.AccessoriesBar, Assembly-CSharp", "OnStartShow", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AccessoriesBarOnStartShowPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                experimentalApi?.SetEquipmentSlotsRuntimeHooksInstalled(equipmentSlotsReloadParamsPatched);
                experimentalApi?.SetEquipmentSlotsUiHooksInstalled(equipmentSlotsAccessoriesInitPatched || equipmentSlotsAccessoriesStartShowPatched);
                runtime.SetHookStatus("Player.EquipmentSlotsApi", equipmentSlotsReloadParamsPatched ? "experimental" : "pending", "Harmony Postfix: AgentEquipmentManager.ReloadParams + AccessoriesBar", equipmentSlotsReloadParamsPatched ? "Patched native equipment stat refresh and AccessoriesBar lifecycle so DTMAPI extra-slot state can participate as attribute-only stats and render an interactive player equipment strip without exposing raw game types." : "Waiting for AgentEquipmentManager.ReloadParams and AccessoriesBar UI hooks to become patchable.");
                runtime.SetHookStatus("Player.EquipmentSlotsShield", equipmentSlotsShieldAttackPatched ? "experimental" : "pending", "Harmony Prefix: BodyController.OnAttacked", equipmentSlotsShieldAttackPatched ? "Patched native player hit path so DTMAPI managed extra-slot shield hats participate only when vanilla hat shields are absent; vanilla visual hat slot stays native-owned." : "Waiting for BodyController.OnAttacked to become patchable.");

                if (!hookResolutionDiagnosticLogged && !AllHookTargetsReady && (DateTimeOffset.Now - initializedAt).TotalSeconds >= 4)
                {
                    hookResolutionDiagnosticLogged = true;
                    runtime.RuntimeMonitor.Log("Hook target resolution diagnostic:" + Environment.NewLine + patcher.BuildTypeResolutionReport(
                        "DolocAPI, Assembly-CSharp",
                        "DolocTown.HomePageUiState, Assembly-CSharp",
                        "DolocTown.GameData.DataPersistenceManager, Assembly-CSharp",
                        "DolocTown.Config.ModManager, Assembly-CSharp",
                        "DolocTown.BodyController, Assembly-CSharp",
                        "DolocTown.AgentStateTool, Assembly-CSharp",
                        "DolocTown.AgentStateInteract, Assembly-CSharp",
                        "DolocTown.AgentStateEat, Assembly-CSharp",
                        "DolocTown.AgentControllerState, Assembly-CSharp",
                        "AgentStateBase, Assembly-CSharp",
                        "DolocTown.ToolCollider, Assembly-CSharp",
                        "DolocTown.DungeonResource, Assembly-CSharp",
                        "DolocTown.GameData.AgentEquipmentManager, Assembly-CSharp",
                        "DolocTown.UI.AccessoriesBar, Assembly-CSharp",
                        "DolocTown.AgentStateFishingReady, Assembly-CSharp",
                        "DolocTown.AgentStateFishingCast, Assembly-CSharp",
                        "DolocTown.AgentStateFishingWait, Assembly-CSharp",
                        "DolocTown.FishingGameScrollBar, Assembly-CSharp",
                        "DolocTown.AgentStateFishingPull, Assembly-CSharp",
                        "DolocTown.Item, Assembly-CSharp",
                        "DolocTown.ItemFishRoe, Assembly-CSharp",
                        "DolocTown.UI.AnimalFullInfoData, Assembly-CSharp",
                        "DolocTown.UI.AnimalViewer, Assembly-CSharp",
                        "DolocTown.UI.AnimalPanel, Assembly-CSharp",
                        "DolocTown.Animal, Assembly-CSharp",
                        "DolocTown.ItemFarmingGun, Assembly-CSharp",
                        "DolocTown.FarmingGunUiState, Assembly-CSharp",
                        "HarmonyLib.Harmony, 0Harmony"));
                }

                if (AllHookTargetsReady)
                {
                    hookRetryTimer?.Dispose();
                    hookRetryTimer = null;
                    AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
                }
            }
        }

        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (args.LoadedAssembly.GetName().Name == "Assembly-CSharp" || args.LoadedAssembly.GetName().Name == "0Harmony")
                InstallHarmonyHooks();
        }

        private static bool IsUiStateActive(Type dolocApi, Type stateType)
        {
            PropertyInfo? userInputProperty = dolocApi.GetProperty("userInput", BindingFlags.Public | BindingFlags.Static);
            object? userInput = userInputProperty?.GetValue(null, null);
            object? currentState = userInput?.GetType().GetProperty("CurrentState", BindingFlags.Public | BindingFlags.Instance)?.GetValue(userInput, null);
            return currentState != null && stateType.IsInstanceOfType(currentState);
        }

        private static object? GetExistingUiState(Type dolocApi, Type stateType)
        {
            PropertyInfo? gameUiStatesProperty = dolocApi.GetProperty("gameUiStates", BindingFlags.Public | BindingFlags.Static);
            object? gameUiStates = gameUiStatesProperty?.GetValue(null);
            if (gameUiStates == null)
                return null;

            MethodInfo? hasState = gameUiStates.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "HasState" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            if (hasState == null)
                return null;

            object? result = hasState.MakeGenericMethod(stateType).Invoke(gameUiStates, null);
            if (!(result is bool active) || !active)
                return null;

            MethodInfo? getState = gameUiStates.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "GetState" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            return getState?.MakeGenericMethod(stateType).Invoke(gameUiStates, null);
        }

        private bool TrySubscribeSaveLoadedUnityEvent(HarmonyReflectionPatcher patcher)
        {
            try
            {
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                FieldInfo? field = dolocApi?.GetField("OnAfterLoadArchiveData", BindingFlags.Public | BindingFlags.Static);
                object? unityEvent = field?.GetValue(null);
                if (unityEvent == null)
                    return false;

                MethodInfo? addListener = unityEvent.GetType().GetMethods()
                    .FirstOrDefault(m => m.Name == "AddListener" && m.GetParameters().Length == 1);
                MethodInfo? callback = typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AfterLoadArchiveDataPostfix), BindingFlags.Public | BindingFlags.Static);
                Type? delegateType = addListener?.GetParameters()[0].ParameterType;
                if (addListener == null || callback == null || delegateType == null)
                    return false;

                saveLoadedUnityEventDelegate = Delegate.CreateDelegate(delegateType, callback);
                addListener.Invoke(unityEvent, new object[] { saveLoadedUnityEventDelegate });
                runtime.RuntimeMonitor.Log("Subscribed to DolocAPI.OnAfterLoadArchiveData for SaveLoaded evidence.");
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to subscribe DolocAPI.OnAfterLoadArchiveData.", ex.ToString());
                return false;
            }
        }

    }
}
