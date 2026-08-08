#pragma warning disable CS0618 // The GameBridge composition root registers frozen compatibility contracts.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private static readonly TimeSpan FeatureStatusPublishHeartbeat = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan FeatureFailureSummaryInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan LifecycleCounterPublishInterval = TimeSpan.FromSeconds(60);
        private static readonly string[] UiContextBlockingStateTypeNames =
        {
            "DolocTown.ModUiState, Assembly-CSharp",
            "DolocTown.SettingPanelUiState, Assembly-CSharp",
            "DolocTown.GameDataUiState, Assembly-CSharp",
            "DolocTown.ModChangeListUiState, Assembly-CSharp",
            "DolocTown.MainMenuUiState, Assembly-CSharp",
            "DolocTown.SystemMenuUiState, Assembly-CSharp",
            "DolocTown.SmallTextMenuUiState, Assembly-CSharp",
            "DolocTown.ConfirmUiState, Assembly-CSharp",
            "DolocTown.HomePageUiState, Assembly-CSharp"
        };
        private const int FeatureFailureShortLogLimit = 3;
        private const int FeatureFailureRecoverySuccessThreshold = 3;
        private readonly DtmApiRuntime runtime;
        private readonly IDebugConsoleApi? debugConsoleApi;
        private readonly DebugActionCompatibilityProxy debugActionApi;
        private DateTimeOffset initializedAt;
        private HarmonyReflectionPatcher? patcher;
        private Type? uiContextDolocApiType;
        private bool uiContextDolocApiResolutionAttempted;
        private Func<bool>? uiContextIsNormalStateGetter;
        private Func<object?>? uiContextUserInputGetter;
        private Func<object, object?>? uiContextCurrentStateGetter;
        private Type? uiContextUserInputType;
        private Type[] uiContextBlockingStateTypes = Array.Empty<Type>();
        private int environmentResetCount;
        private DateTimeOffset lastLifecycleCounterPublishedAtUtc = DateTimeOffset.MinValue;
        private Timer? hookRetryTimer;
        private readonly object hookGate = new object();
        private readonly HookInstallScheduler hookInstallScheduler = new HookInstallScheduler();
        private int runtimeAutomationUpdateInProgress;
        private long runtimeAutomationReentryBypassCount;
        private bool assemblyLoadSubscribed;
        private bool shutdownCleanupRan;
        private bool saveLoadedPatched;
        private bool saveLoadedEventSubscribed;
        private bool loadRequestedPatched;
        private bool loadReturnedPatched;
        private bool nativeGameFramePatched;
        private bool saveSavingPatched;
        private bool saveSavedPatched;
        private bool returnHomeRequestedPatched;
        private bool returnHomePatched;
        private bool workshopReloadPatched;
        private bool workshopModUiRegisterPatched;
        private bool workshopModUiHidePatched;
        private bool workshopModManagerSavePatched;
        private bool workshopModUiCloseTransactionPatched;
        private bool workshopLocalUploadDisplayPatched;
        private bool workshopUploadPlanBusyFallbackPatched;
        private bool workshopUploadPlanKnownIdFallbackPatched;
        private long optionalWorkshopFileStatusCallCount;
        private readonly List<PendingWorkshopUploadPlanResolution> pendingWorkshopUploadPlanResolutions = new List<PendingWorkshopUploadPlanResolution>();
        private bool actionSpeedToolEnterPatched => actionSpeedFeature?.HookBridge.ToolEnterPatched == true;
        private bool actionSpeedToolExitPatched => agentStateLifecycleHooks?.ToolExitPatched == true;
        private bool actionSpeedInteractEnterPatched => actionSpeedFeature?.HookBridge.InteractEnterPatched == true;
        private bool actionSpeedInteractExitPatched => agentStateLifecycleHooks?.InteractExitPatched == true;
        private bool actionSpeedEatEnterPatched => actionSpeedFeature?.HookBridge.EatEnterPatched == true;
        private bool actionSpeedUseItemContinuesPatched => actionSpeedFeature?.HookBridge.UseItemContinuesPatched == true;
        private bool actionSpeedInteractContinuesPatched => actionSpeedFeature?.HookBridge.InteractContinuesPatched == true;
        private bool actionSpeedAnimalRendererInteractPatched => actionSpeedFeature?.HookBridge.AnimalRendererInteractPatched == true;
        private bool actionSpeedBaseExitPatched => agentStateLifecycleHooks?.BaseExitPatched == true;
        private bool toolColliderHitPostfixPatched => toolColliderHitHooks?.PostfixPatched == true;
        private bool fishingCompatibilityHooksRequired => fishingCompatibilityFeature?.HooksRequired == true;
        private bool fishingCompatibilityHooksReady => fishingCompatibilityFeature?.HooksReady == true;
        private bool fishingCompatibilityReadyEnterPatched => fishingCompatibilityFeature?.ReadyEnterPatched == true;
        private bool fishingCompatibilityCastEnterPatched => fishingCompatibilityFeature?.CastEnterPatched == true;
        private bool fishingCompatibilityWaitEnterPatched => fishingCompatibilityFeature?.WaitEnterPatched == true;
        private bool fishingCompatibilityWaitPlayPatched => fishingCompatibilityFeature?.WaitPlayPatched == true;
        private bool fishingCompatibilityMiniGameStartPatched => fishingCompatibilityFeature?.MiniGameStartPatched == true;
        private bool fishingCompatibilityMiniGameUpdatePatched => fishingCompatibilityFeature?.MiniGameUpdatePatched == true;
        private bool fishingCompatibilityMiniGameStopPatched => fishingCompatibilityFeature?.MiniGameStopPatched == true;
        private bool fishingCompatibilityPullEnterPatched => fishingCompatibilityFeature?.PullEnterPatched == true;
        private bool fishingCompatibilityPullExitPatched => fishingCompatibilityFeature?.PullExitPatched == true;
        private bool fishingCompatibilityBaseExitPatched => fishingCompatibilityFeature?.BaseExitPatched == true;
        private bool fishRoeTitlePatched => fishRoeTooltipFeature?.HookBridge.TitlePatched == true;
        private bool fishRoeDescriptionPatched => fishRoeTooltipFeature?.HookBridge.DescriptionPatched == true;
        private bool fishRoeDetailPatched => fishRoeTooltipFeature?.HookBridge.DetailPatched == true;
        private bool animalFullInfoDataPatched => animalViewerFeature?.HookBridge.FullInfoDataPatched == true;
        private bool animalViewerShowPrefixPatched => animalViewerFeature?.HookBridge.ViewerShowPrefixPatched == true;
        private bool animalViewerShowPatched => animalViewerFeature?.HookBridge.ViewerShowPatched == true;
        private bool animalPanelUnregisterPatched => animalViewerFeature?.HookBridge.PanelUnregisterPatched == true;
        private bool hookResolutionDiagnosticLogged;
        private bool uiContextDiagnosticLogged;
        private Delegate? saveLoadedUnityEventDelegate;
        private readonly List<IGameBridgeFeature> features = new List<IGameBridgeFeature>();
        private readonly Dictionary<string, GameBridgeFeatureStatus> featureStatuses = new Dictionary<string, GameBridgeFeatureStatus>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GameBridgeFeatureFailureState> featureFailures = new Dictionary<string, GameBridgeFeatureFailureState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GameBridgeFeatureRuntimeState> featureRuntimeStates = new Dictionary<string, GameBridgeFeatureRuntimeState>(StringComparer.OrdinalIgnoreCase);
        private DateTimeOffset lastGameBridgeFinalHealthSnapshotAtUtc = DateTimeOffset.MinValue;
        private string latestGameBridgeFinalHealthSummary = "not-run";
        private bool experimentalApisRegistered;
        private DolocTownExperimentalBridgeApi? experimentalApi;
        private EnvironmentResetHookBridge? environmentResetHookBridge;
        private CameraFeature? cameraFeature;
        private FishingAutomationCompatibilityFeature? fishingCompatibilityFeature;
        private FishRoeTooltipFeature? fishRoeTooltipFeature;
        private ItemDisplayNameFeature? itemDisplayNameFeature;
        private ChestLocatorEnhancerFeature? chestLocatorEnhancerFeature;
        private EquipmentSlotsFeature? equipmentSlotsFeature;
        private SaveSlotsFeature? saveSlotsFeature;
        private NativeUiLayoutDiagnosticsFeature? nativeUiLayoutDiagnosticsFeature;
        private CropHarvestingFeature? cropHarvestingFeature;
        private AnimalViewerFeature? animalViewerFeature;
        private CustomAnimalAnimatorBridgeFeature? customAnimalAnimatorBridgeFeature;
        private AudioReplacementFeature? audioReplacementFeature;
        private AgentStateLifecycleHookBridge? agentStateLifecycleHooks;
        private ToolColliderHitHookBridge? toolColliderHitHooks;
        private ActionSpeedFeature? actionSpeedFeature;
        private ActionCompletionFeature? actionCompletionFeature;

        public DolocTownGameBridge(DtmApiRuntime runtime, IDebugConsoleApi? debugConsoleApi = null)
            : this(runtime, debugConsoleApi, null)
        {
        }

        internal DolocTownGameBridge(
            DtmApiRuntime runtime,
            IDebugConsoleApi? debugConsoleApi,
            PreparedQaHost? preparedQaHost)
        {
            this.runtime = runtime;
            this.debugConsoleApi = debugConsoleApi;
            debugActionApi = new DebugActionCompatibilityProxy(runtime);
            this.preparedQaHost = preparedQaHost;
            InitializeGameBridgeDemandRouting();
            RegisterExperimentalApis();
            AttachQaHostParticipant();
            runtime.ConfigureAuthorSessionHandler(HandleDemandedAuthorSessionRequest);
            runtime.RegisterModOwnerCleanupParticipant(new GameBridgeModOwnerCleanupParticipant(this));
            runtime.AddRuntimeReportContextProvider(BuildGameBridgeRuntimeReportContext);
            runtime.AddTitleReturnObjectGraphProvider(BuildGameBridgeTitleReturnObjectGraphSection);
            runtime.AddTitleReturnObjectGraphProvider(BuildGameBridgeUiOwnerObjectGraphSection);
            runtime.LogExportBoundary += () => PublishGameBridgeFinalHealthSnapshot("LogExport");
            runtime.RuntimeShutdownBoundary += reason => PublishGameBridgeFinalHealthSnapshot("Shutdown " + (reason ?? string.Empty));
        }

        internal DolocTownExperimentalBridgeApi? ExperimentalApi => experimentalApi;

        internal CameraFeature? CameraFeature => cameraFeature;

        internal LegacyFishingAutomationService? LegacyFishingAutomationService => fishingCompatibilityFeature?.Service;

        internal IFishingCompatibilityHookRuntime? FishingCompatibilityCallbackService => fishingCompatibilityFeature?.CallbackRuntime;

        internal FishRoeTooltipService? FishRoeTooltipService => fishRoeTooltipFeature?.Service;

        internal ChestLocatorEnhancerService? ChestLocatorEnhancerService => chestLocatorEnhancerFeature?.Service;

        internal EquipmentSlotsService? EquipmentSlotsService => equipmentSlotsFeature?.Service;

        internal NativeUiLayoutRepairService? NativeUiLayoutRepairService => nativeUiLayoutDiagnosticsFeature?.RepairService;

        internal CropHarvestingService? CropHarvestingService => cropHarvestingFeature?.Service;

        internal AnimalViewerService? AnimalViewerService => animalViewerFeature?.Service;

        internal CustomAnimalAnimatorBridgeService? CustomAnimalAnimatorBridgeService => customAnimalAnimatorBridgeFeature?.Service;

        internal ActionSpeedService? ActionSpeedService => actionSpeedFeature?.Service;

        internal ActionCompletionService? ActionCompletionService => actionCompletionFeature?.Service;

        internal AudioReplacementService? AudioReplacementService => audioReplacementFeature?.Service;

        internal DolocTownExperimentalBridgeApi? ExperimentalApiForQa => experimentalApi;
        internal DtmApiRuntime RuntimeForQa => runtime;
        internal IInventoryDebugApi InventoryDebugApiForQa => debugActionApi;
        internal IWeatherDebugApi WeatherDebugApiForQa => debugActionApi;
        internal ITeleportDebugApi TeleportDebugApiForQa => debugActionApi;
        internal IInstantSaveDebugApi InstantSaveDebugApiForQa => debugActionApi;
        internal ITimeDebugApi TimeDebugApiForQa => debugActionApi;
        internal IMovementDebugApi MovementDebugApiForQa => debugActionApi;
        internal IAdvancedDebugApi AdvancedDebugApiForQa => debugActionApi;
        internal bool DebugActionCompatibilityLoadedForQa =>
            debugActionApi.IsLoaded;
        internal CameraFeature? CameraFeatureForQa => cameraFeature;
        internal FishingAutomationCompatibilityFeature? FishingAutomationCompatibilityFeatureForQa => fishingCompatibilityFeature;
        internal AudioReplacementFeature? AudioReplacementFeatureForQa => audioReplacementFeature;
        internal NativeUiLayoutDiagnosticsFeature? NativeUiLayoutDiagnosticsFeatureForQa => nativeUiLayoutDiagnosticsFeature;
        internal IDebugConsoleApi? DebugConsoleApiForQa => debugConsoleApi;
        internal bool ActionSpeedInteractEnterPatchedForQa => actionSpeedInteractEnterPatched;
        internal bool ActionSpeedInteractExitPatchedForQa => actionSpeedInteractExitPatched;
        internal bool ActionSpeedEatEnterPatchedForQa => actionSpeedEatEnterPatched;
        internal bool ActionSpeedUseItemContinuesPatchedForQa => actionSpeedUseItemContinuesPatched;
        internal bool ActionSpeedInteractContinuesPatchedForQa => actionSpeedInteractContinuesPatched;

        public void Initialize()
        {
            initializedAt = DateTimeOffset.Now;
            ReconcileWorkshopAuthoringDemand(runtime.IsAuthorSessionActive, "GameBridge Initialize after Runtime start");
            PublishQaHostPreparedLifecycle();
            DolocTownHookCallbacks.Runtime = runtime;
            DolocTownHookCallbacks.Bridge = this;
            PublishGameBridgeFeatureHookStatuses();
            PublishGameBridgeFeatureContractDiagnostics("Initialize");
            PublishCustomEntityRegistryContractHookStatuses();
            runtime.SetHookStatus("GameLoop.UpdateTicked", "verified", "BepInEx MonoBehaviour.Update", "DTMAPI dispatches UpdateTicked from the bootstrap Update callback.");
            runtime.SetHookStatus("GameLoop.OneSecondUpdateTicked", "verified", "DTMAPI.Core timer", "DTMAPI dispatches a throttled one-second event from Update.");
            RequestHookInstall("Initialize");
            EnsureHookRetrySources("Initialize");
            PublishHookSchedulerStatus("Initialize");
            StartQaHostParticipant();
        }

        private void RegisterExperimentalApis()
        {
            if (experimentalApisRegistered)
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
            runtime.RegisterRuntimeApi<IInventoryDebugApi>(manifest, debugActionApi, OwnerBoundGameBridgeApis.ForInventoryDebug(debugActionApi));
            runtime.RegisterRuntimeApi<IMailDeliveryApi>(manifest, experimentalApi, OwnerBoundGameBridgeApis.ForMailDelivery(experimentalApi));
            runtime.RegisterRuntimeApi<IWeatherDebugApi>(manifest, debugActionApi, OwnerBoundGameBridgeApis.ForWeatherDebug(debugActionApi));
            runtime.RegisterRuntimeApi<ITeleportDebugApi>(manifest, debugActionApi, OwnerBoundGameBridgeApis.ForTeleportDebug(debugActionApi));
            runtime.RegisterRuntimeApi<IInstantSaveDebugApi>(manifest, debugActionApi, OwnerBoundGameBridgeApis.ForInstantSaveDebug(debugActionApi));
            runtime.RegisterRuntimeApi<ITimeDebugApi>(manifest, debugActionApi, OwnerBoundGameBridgeApis.ForTimeDebug(debugActionApi));
            runtime.RegisterRuntimeApi<IMovementDebugApi>(manifest, debugActionApi, OwnerBoundGameBridgeApis.ForMovementDebug(debugActionApi));
            runtime.RegisterRuntimeApi<ILampControlApi>(manifest, RetiredLampControlApi.Instance, OwnerBoundGameBridgeApis.ForLampControl(RetiredLampControlApi.Instance));
            RegisterGameBridgeFeatureApis(manifest);
            runtime.RegisterRuntimeApi<IAdvancedDebugApi>(manifest, debugActionApi, OwnerBoundGameBridgeApis.ForAdvancedDebug(debugActionApi));
            experimentalApisRegistered = true;
        }

        internal void UpdateRuntimeAutomation()
        {
            if (Interlocked.CompareExchange(ref runtimeAutomationUpdateInProgress, 1, 0) != 0)
            {
                Interlocked.Increment(ref runtimeAutomationReentryBypassCount);
                return;
            }

            try
            {
                CommitPendingDemandRoutesAtFrameBoundary();
                if (ProcessPendingHookInstallRequests("DemandFrame"))
                    RefreshDemandRoutePatchStates("HookInstallProcessed");
                DispatchActiveDemandUpdaters();
                debugActionApi.UpdateIfLoaded();
            }
            finally
            {
                Volatile.Write(ref runtimeAutomationUpdateInProgress, 0);
            }
        }

        internal void NotifyGameBridgeFeaturesSaveLoaded(bool isNewGame)
        {
            debugActionApi.ResetForSaveBoundaryIfLoaded();
            DispatchGameBridgeFeatures("SaveLoaded", feature => feature.SaveLoaded(isNewGame));
            PublishGameBridgeFinalHealthSnapshot("SaveLoaded");
        }

        internal void NotifyEquipmentSlotsSaveSaved(int slot)
        {
            if (OptionalHostDisablesEquipmentSlotsRuntime)
                return;
            equipmentSlotsFeature?.Service.SaveSaved(slot);
        }

        internal void NotifyEquipmentSlotsSaveSaving(int slot)
        {
            if (OptionalHostDisablesEquipmentSlotsRuntime)
                return;
            equipmentSlotsFeature?.Service.SaveSaving(slot);
        }

        internal void NotifyGameBridgeFeaturesReturnedToTitle()
        {
            ReconcileWorkshopAuthoringDemand(false, "ReturnedToTitle");
            debugActionApi.ResetForTitleBoundaryIfLoaded();
            DispatchGameBridgeFeatures("ReturnedToTitle", feature => feature.ReturnedToTitle());
            PublishGameBridgeFinalHealthSnapshot("ReturnedToTitle");
        }

        internal void NotifyGameBridgeFeaturesEnvironmentReset(string reason)
        {
            environmentResetCount++;
            RunEnvironmentResetStep("ItemDisplayName.EnvironmentReset", () => itemDisplayNameFeature?.EnvironmentReset(reason));
            RunEnvironmentResetStep("Runtime.LifecycleRetentionCounters", () => PublishLifecycleCountersIfNeeded(reason));
        }

        internal bool HasEnvironmentResetDemand =>
            HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ItemDisplayNameEnvironmentReset);

        internal void NotifyCameraCompatibilityEnvironmentReset(
            string reason)
        {
            if (cameraFeature?.HasEnvironmentResetDemand != true)
                return;
            RunEnvironmentResetStep(
                "Camera.CompatibilityEnvironmentReset",
                () => cameraFeature.EnvironmentReset(reason));
        }

        internal int EnvironmentResetCountForTests => environmentResetCount;

        private void RunEnvironmentResetStep(string operation, Action action)
        {
            try
            {
                action();
                if (RecordGameBridgeFeatureRecoverySuccess("Runtime.EnvironmentResetStep", operation))
                    runtime.SetHookStatus("Runtime.EnvironmentResetFanout", "recovered", operation, "EnvironmentReset step recovered after repeated successful runs.");
            }
            catch (Exception ex)
            {
                string summary = "operation=" + operation + ", error=" + ex.GetType().Name + ": " + ex.Message;
                GameBridgeFeatureFailurePublication publication = RecordGameBridgeFeatureFailurePublication("Runtime.EnvironmentResetStep", operation, ex);
                if (publication.RecordDiagnosticsError)
                    runtime.Diagnostics.RecordError("DTMAPI.GameBridge.EnvironmentReset", "EnvironmentReset step failed. " + summary, ex.ToString());
                if (publication.LogMode == GameBridgeFeatureFailureLogMode.Full)
                    runtime.RuntimeMonitor.Log("EnvironmentReset step failed. " + summary, LogLevel.Warn);
                else if (publication.LogMode == GameBridgeFeatureFailureLogMode.Short)
                    runtime.RuntimeMonitor.Log("Repeated EnvironmentReset step failure operation=" + operation + " count=" + publication.Count.ToString(CultureInfo.InvariantCulture) + " error=" + FormatGameBridgeExceptionSummary(ex), LogLevel.Warn);
                else if (publication.LogMode == GameBridgeFeatureFailureLogMode.Summary)
                    runtime.RuntimeMonitor.Log("Throttled EnvironmentReset step failures operation=" + operation + " count=" + publication.Count.ToString(CultureInfo.InvariantCulture) + " lastError=" + FormatGameBridgeExceptionSummary(ex), LogLevel.Warn);
                if (publication.ShouldPublishHookStatus)
                    runtime.SetHookStatus("Runtime.EnvironmentResetFanout", "degraded", operation, summary + ", count=" + publication.Count.ToString(CultureInfo.InvariantCulture));
            }
        }

        private void PublishLifecycleCountersIfNeeded(string reason)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (environmentResetCount > 3 &&
                now - lastLifecycleCounterPublishedAtUtc < LifecycleCounterPublishInterval)
                return;

            lastLifecycleCounterPublishedAtUtc = now;
            string summary = "environmentResetCount=" + environmentResetCount.ToString(CultureInfo.InvariantCulture) +
                ", reason=" + (reason ?? string.Empty) +
                ", featureFanout=" + features.Count.ToString(CultureInfo.InvariantCulture) +
                ", " + (saveSlotsFeature?.Service.GetOfficialSaveUiLifecycleSummary() ?? "saveUiStates=0, saveUiPagers=0, saveUiBinders=0") +
                ", " + (experimentalApi?.GetRuntimeAutomationLifecycleSummary() ?? "actionAnimators=0, actionAutoFillApplications=0, actionPendingAnimalInteract=false") +
                ", " + debugActionApi.GetLifecycleSummary() +
                ", " + (fishingCompatibilityFeature?.GetCompatibilityLifecycleSummary() ?? "compatibilityStatus=inactive/no-consumer, fishingStates=0, fishingOptions=0");
            runtime.SetHookStatus("Runtime.LifecycleRetentionCounters", "observed", "DTMAPI.GameBridge.DolocTown EnvironmentReset", summary);
        }

        private bool OptionalHostDisablesEquipmentSlotsRuntime =>
            preparedQaHost?.StartupOptions.DisableEquipmentSlotsRuntime == true;

        private bool IsFeatureDisabledByOptionalHost(string featureId) =>
            preparedQaHost?.StartupOptions.DisabledFeatureIds.Contains(featureId, StringComparer.OrdinalIgnoreCase) == true;

        private void EnsureGameBridgeFeatures()
        {
            environmentResetHookBridge ??= new EnvironmentResetHookBridge(runtime);

            if (!IsFeatureDisabledByOptionalHost("Camera"))
            {
                cameraFeature ??= new CameraFeature(runtime);
                if (!features.Contains(cameraFeature))
                    features.Add(cameraFeature);
            }

            fishingCompatibilityFeature ??= new FishingAutomationCompatibilityFeature(runtime);
            if (!features.Contains(fishingCompatibilityFeature))
                features.Add(fishingCompatibilityFeature);

            fishRoeTooltipFeature ??= new FishRoeTooltipFeature(runtime);
            if (!features.Contains(fishRoeTooltipFeature))
                features.Add(fishRoeTooltipFeature);

            itemDisplayNameFeature ??= new ItemDisplayNameFeature(runtime, () => environmentResetHookBridge?.SetEnvCameraPatched == true);
            if (!features.Contains(itemDisplayNameFeature))
                features.Add(itemDisplayNameFeature);

            chestLocatorEnhancerFeature ??= new ChestLocatorEnhancerFeature(runtime);
            if (!features.Contains(chestLocatorEnhancerFeature))
                features.Add(chestLocatorEnhancerFeature);

            if (!OptionalHostDisablesEquipmentSlotsRuntime)
            {
                equipmentSlotsFeature ??= new EquipmentSlotsFeature(runtime);
                if (!features.Contains(equipmentSlotsFeature))
                    features.Add(equipmentSlotsFeature);
            }

            if (!IsFeatureDisabledByOptionalHost("SaveSlots"))
            {
                saveSlotsFeature ??= new SaveSlotsFeature(runtime);
                if (!features.Contains(saveSlotsFeature))
                    features.Add(saveSlotsFeature);
            }

            nativeUiLayoutDiagnosticsFeature ??= new NativeUiLayoutDiagnosticsFeature(runtime);
            if (!features.Contains(nativeUiLayoutDiagnosticsFeature))
                features.Add(nativeUiLayoutDiagnosticsFeature);

            cropHarvestingFeature ??= new CropHarvestingFeature(runtime);
            if (!features.Contains(cropHarvestingFeature))
                features.Add(cropHarvestingFeature);

            animalViewerFeature ??= new AnimalViewerFeature(runtime);
            if (!features.Contains(animalViewerFeature))
                features.Add(animalViewerFeature);

            customAnimalAnimatorBridgeFeature ??= new CustomAnimalAnimatorBridgeFeature(runtime);
            if (!features.Contains(customAnimalAnimatorBridgeFeature))
                features.Add(customAnimalAnimatorBridgeFeature);

            audioReplacementFeature ??= new AudioReplacementFeature(runtime);
            if (!features.Contains(audioReplacementFeature))
                features.Add(audioReplacementFeature);

            agentStateLifecycleHooks ??= new AgentStateLifecycleHookBridge();
            toolColliderHitHooks ??= new ToolColliderHitHookBridge();

            actionSpeedFeature ??= new ActionSpeedFeature(runtime, agentStateLifecycleHooks);
            if (!features.Contains(actionSpeedFeature))
                features.Add(actionSpeedFeature);

            actionCompletionFeature ??= new ActionCompletionFeature(runtime, () => toolColliderHitPostfixPatched, () => agentStateLifecycleHooks?.InteractExitPatched == true);
            if (!features.Contains(actionCompletionFeature))
                features.Add(actionCompletionFeature);
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
                "Experimental/Frozen 0.4.0 custom entity registry compatibility contracts remain registered through the DTMAPI Core provider with owner-aware validation, duplicate-ID detection, snapshots, save-boundary cleanup, and provider error isolation.");
            runtime.SetHookStatus(
                "CustomAnimals.RegistryContract",
                "configured-blocked",
                "AnimalManager.CreateAnimal + Animal lifecycle research",
                "Experimental/Frozen registry compatibility contract; runtime creation remains blocked");
            runtime.SetHookStatus(
                "CustomMonsters.RegistryContract",
                "configured-blocked",
                "MonsterController + MonsterGroupManager + MonsterAttackBehaviour research",
                "Experimental/Frozen registry compatibility contract; runtime creation remains blocked");
            runtime.SetHookStatus(
                "CustomAttacks.RegistryContract",
                "configured-blocked",
                "BulletFactory + BulletManager + PhysicalDamageBox research",
                "Experimental/Frozen registry compatibility contract; runtime creation remains blocked");
            runtime.SetHookStatus(
                "CustomDrones.RegistryContract",
                "configured-blocked",
                "DroneController + DroneWeapon + DolocAPI.EquipDrone research",
                "Experimental/Frozen registry compatibility contract; runtime creation remains blocked");
        }

        private void RefreshUiContext()
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                if (!uiContextDolocApiResolutionAttempted)
                {
                    uiContextDolocApiType = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                    uiContextDolocApiResolutionAttempted = true;
                    if (uiContextDolocApiType != null)
                        BuildUiContextNativeCache(uiContextDolocApiType);
                }
                Type? dolocApi = uiContextDolocApiType;
                if (dolocApi == null)
                {
                    runtime.UI.SetUiContext("Unknown", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "DolocAPI not visible yet.");
                    return;
                }

                if (uiContextIsNormalStateGetter?.Invoke() == true)
                {
                    runtime.UI.SetUiContext("Gameplay", canDrawOverlay: true, gameplayHotkeysAllowed: true, reason: "DolocAPI.IsNormalState.");
                    return;
                }

                // HomePageUiState can remain in the state stack behind official title-page
                // panels. Prefer blocking panels first so the DTMAPI title button hides
                // when Doloc Town owns the visible UI.
                string context = FirstActiveUiState() ?? "Gameplay";

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

        private void BuildUiContextNativeCache(Type dolocApi)
        {
            uiContextIsNormalStateGetter = GameBridgeNativeAccessors.CreateStaticBoolGetter(
                GameBridgeNativeAccessors.FindMember(dolocApi, "IsNormalState", isStatic: true));
            uiContextUserInputGetter = GameBridgeNativeAccessors.CreateStaticObjectGetter(
                GameBridgeNativeAccessors.FindMember(dolocApi, "userInput", isStatic: true));
            uiContextBlockingStateTypes = UiContextBlockingStateTypeNames
                .Select(typeName => patcher?.ResolveType(typeName))
                .Where(type => type != null)
                .Cast<Type>()
                .ToArray();
            uiContextCurrentStateGetter = null;
            uiContextUserInputType = null;
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
                GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.WorkshopPendingUploadPlan, GameBridgeDemandRoutes.OperationOwner, RuntimeDemandSourceType.CapabilityOperation, RuntimeDemandLifetime.Operation, "pending-upload-plan", true, "Workshop upload plan fallback pending");
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
            {
                GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.WorkshopPendingUploadPlan, GameBridgeDemandRoutes.OperationOwner, RuntimeDemandSourceType.CapabilityOperation, RuntimeDemandLifetime.Operation, "pending-upload-plan", false, "Workshop upload plan queue empty");
                return;
            }

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

            if (pendingWorkshopUploadPlanResolutions.Count == 0)
                GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.WorkshopPendingUploadPlan, GameBridgeDemandRoutes.OperationOwner, RuntimeDemandSourceType.CapabilityOperation, RuntimeDemandLifetime.Operation, "pending-upload-plan", false, "Workshop upload plan queue drained");
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

        private bool TryReadDtmapiKnownLocalWorkshopRequest(object modInfo, out ulong workshopId, out string modId, out string rootPath)
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

        private bool IsDtmapiGeneratedLocalModRoot(string rootPath)
        {
            try
            {
                string runtimeMarkerPath = Path.Combine(rootPath, "Content", "DTMAPI", "release-manifest.json");
                optionalWorkshopFileStatusCallCount++;
                if (File.Exists(runtimeMarkerPath))
                    return true;

                string markerPath = Path.Combine(rootPath, "Content", "DTMAPI", "dtmapi-package.json");
                optionalWorkshopFileStatusCallCount++;
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

        private string? FirstActiveUiState()
        {
            object? userInput = uiContextUserInputGetter?.Invoke();
            if (userInput == null)
                return null;

            Type userInputType = userInput.GetType();
            if (!ReferenceEquals(uiContextUserInputType, userInputType))
            {
                uiContextUserInputType = userInputType;
                uiContextCurrentStateGetter = GameBridgeNativeAccessors.CreateObjectGetter(
                    GameBridgeNativeAccessors.FindMember(userInputType, "CurrentState"));
            }

            object? currentState = uiContextCurrentStateGetter?.Invoke(userInput);
            if (currentState == null)
                return null;
            for (int index = 0; index < uiContextBlockingStateTypes.Length; index++)
            {
                Type stateType = uiContextBlockingStateTypes[index];
                if (stateType.IsInstanceOfType(currentState))
                    return stateType.Name;
            }
            return null;
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

        private void TryUnsubscribeSaveLoadedUnityEvent(string reason)
        {
            if (saveLoadedUnityEventDelegate == null || patcher == null)
                return;

            Delegate listener = saveLoadedUnityEventDelegate;
            try
            {
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                FieldInfo? field = dolocApi?.GetField("OnAfterLoadArchiveData", BindingFlags.Public | BindingFlags.Static);
                object? unityEvent = field?.GetValue(null);
                if (unityEvent == null)
                    return;

                MethodInfo? removeListener = unityEvent.GetType().GetMethods()
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "RemoveListener")
                            return false;
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(listener.GetType());
                    });
                removeListener?.Invoke(unityEvent, new object[] { listener });
                runtime.RuntimeMonitor.Log("Unsubscribed DTMAPI SaveLoaded UnityEvent listener reason=" + (reason ?? string.Empty) + ".");
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordWarning("DTMAPI.GameBridge", "Failed to unsubscribe DolocAPI.OnAfterLoadArchiveData.", ex.ToString());
            }
            finally
            {
                saveLoadedUnityEventDelegate = null;
                saveLoadedEventSubscribed = false;
            }
        }

    }
}
