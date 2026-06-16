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
            experimentalApi?.ResetMovementDebugLease("SaveLoaded");
            DispatchGameBridgeFeatures("SaveLoaded", feature => feature.SaveLoaded(isNewGame));
        }

        internal void NotifyGameBridgeFeaturesReturnedToTitle()
        {
            experimentalApi?.ResetMovementDebugLease("ReturnedToTitle");
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
