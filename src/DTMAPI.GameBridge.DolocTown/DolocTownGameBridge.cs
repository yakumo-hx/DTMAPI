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
        private bool autoExerciseAutoFishingMovementCancelVerified;
        private bool autoExerciseAutoFishingPhaseVerified;
        private int autoFishingMiniGameCompleteBaseline;
        private bool autoExerciseTitleButtonLifecycleAttempted;
        private bool autoExerciseInstantSaveAttempted;
        private bool autoExerciseDebugConsoleAttempted;
        private bool autoExerciseDebugInventoryAttempted;
        private bool autoExerciseDebugWeatherAttempted;
        private bool autoExerciseDebugTeleportAttempted;
        private bool autoExerciseDebugTimeAttempted;
        private bool autoExerciseDebugMovementAttempted;
        private bool autoExerciseAdvancedDebugAttempted;
        private bool autoExerciseVehicleAttempted;
        private bool autoExerciseNewContentApisAttempted;
        private bool autoExerciseMineContentApisAttempted;
        private bool autoExerciseZoomAttempted;
        private ZoomSmokeRun? zoomSmokeRun;
        private bool autoExerciseChestLocatorEnhancerAttempted;
        private bool autoExerciseStrongPlantingGunAttempted;
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
        private DateTimeOffset vehicleOutdoorTeleportRequestedAt;
        private DateTimeOffset vehicleEdgeTransitionRequestedAt;
        private TeleportSnapshot? debugTeleportBeforeSnapshot;
        private TeleportDestination? debugTeleportDestination;
        private TeleportResult? debugTeleportRequestResult;
        private TeleportDestination? vehicleEdgeTransitionDestination;
        private TeleportResult? vehicleEdgeTransitionRequestResult;
        private MotorVehicleState? vehicleEdgeTransitionOriginalBefore;
        private MotorVehicleState? vehicleEdgeTransitionSecondBefore;
        private string vehicleEdgeTransitionBaseSummary = string.Empty;
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
        private bool cameraZoomSetEnvCameraPatched;
        private bool workshopReloadPatched;
        private bool actionSpeedToolEnterPatched;
        private bool actionSpeedToolExitPatched;
        private bool actionSpeedInteractEnterPatched;
        private bool actionSpeedInteractExitPatched;
        private bool actionSpeedEatEnterPatched;
        private bool actionSpeedUseItemContinuesPatched;
        private bool actionSpeedBaseExitPatched;
        private bool debugConsoleUseToolPatched;
        private bool debugConsoleUseItemPatched;
        private bool debugConsoleEnterUiCheckPatched;
        private bool oneActionToolColliderPatched;
        private bool oilCoalDropCapturePatched;
        private bool fishingReadyEnterPatched;
        private bool fishingCastEnterPatched;
        private bool fishingWaitEnterPatched;
        private bool fishingWaitPlayPatched;
        private bool fishingMiniGameStartPatched;
        private bool fishingMiniGameUpdatePatched;
        private bool fishingMiniGameStopPatched;
        private bool fishingPullEnterPatched;
        private bool fishingPullExitPatched;
        private bool fishRoeTitlePatched;
        private bool fishRoeDescriptionPatched;
        private bool fishRoeDetailPatched;
        private bool animalFullInfoDataPatched;
        private bool animalViewerShowPrefixPatched;
        private bool animalViewerShowPatched;
        private bool animalPanelRefreshViewerPatched;
        private bool motorKeyUsePatched;
        private bool motorInteractPatched;
        private bool motorGetOnPatched;
        private bool motorGetOffPatched;
        private bool motorFixedUpdatePrefixPatched;
        private bool motorFixedUpdatePostfixPatched;
        private bool motorUnlockPatched;
        private bool motorSetPositionPatched;
        private bool motorEnterRoomPatched;
        private bool equipmentRendererReusePatched;
        private bool equipmentBuilderCreateIndicatorPatched;
        private bool equipmentBuilderTurnIndicatorPatched;
        private bool equipmentSlotsReloadParamsPatched;
        private bool equipmentSlotsAccessoriesInitPatched;
        private bool equipmentSlotsAccessoriesStartShowPatched;
        private bool chestLocatorAvailableInventoriesPatched;
        private bool strongPlantingGunCtorPatched;
        private bool strongPlantingGunToolPatched;
        private bool strongPlantingGunUiPlacePatched;
        private bool strongPlantingGunUiSwapOnePatched;
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
        private DolocTownExperimentalBridgeApi? experimentalApi;
        private CameraFeature? cameraFeature;

        public DolocTownGameBridge(DtmApiRuntime runtime, Func<bool>? clickTitleSettingsButton = null, IDebugConsoleApi? debugConsoleApi = null)
        {
            this.runtime = runtime;
            this.clickTitleSettingsButton = clickTitleSettingsButton;
            this.debugConsoleApi = debugConsoleApi;
            RegisterExperimentalApis();
        }

        internal DolocTownExperimentalBridgeApi? ExperimentalApi => experimentalApi;

        internal CameraFeature? CameraFeature => cameraFeature;

        public void CleanupSecondMotorForLifecycleBoundary(string reason)
        {
            experimentalApi?.CleanupSecondMotorResidueForBoundary(reason);
        }

        public void Initialize()
        {
            initializedAt = DateTimeOffset.Now;
            DolocTownHookCallbacks.Runtime = runtime;
            DolocTownHookCallbacks.Bridge = this;
            experimentalApi?.PublishHookStatuses();
            cameraFeature?.PublishHookStatuses();
            PublishStableCustomEntityHookStatuses();
            runtime.SetHookStatus("GameLoop.UpdateTicked", "verified", "BepInEx MonoBehaviour.Update", "DTMAPI dispatches UpdateTicked from the bootstrap Update callback.");
            runtime.SetHookStatus("GameLoop.OneSecondUpdateTicked", "verified", "DTMAPI.Core timer", "DTMAPI dispatches a throttled one-second event from Update.");
            InstallHarmonyHooks();
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
            hookRetryTimer = new Timer(_ => InstallHarmonyHooks(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
            LoadSmokeSettings();
        }

        private void RegisterExperimentalApis()
        {
            if (experimentalApi != null && cameraFeature != null)
                return;
            experimentalApi ??= new DolocTownExperimentalBridgeApi(runtime);
            cameraFeature ??= new CameraFeature(runtime);
            var manifest = new ManifestModel
            {
                Name = "DTMAPI Doloc Town GameBridge",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.GameBridge.DolocTown",
                Type = "RuntimeApi"
            };
            runtime.RegisterRuntimeApi<IActionCompletionApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IFishingAutomationApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IActionSpeedApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IItemTooltipApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IAnimalViewerApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IInventoryDebugApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IMailDeliveryApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IWeatherDebugApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<ITeleportDebugApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IInstantSaveDebugApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<ITimeDebugApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IMovementDebugApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IMotorVehicleApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IMachineProductionApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IEquipmentSlotsApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<ISaveSlotsApi>(manifest, experimentalApi);
            cameraFeature.RegisterRuntimeApis(manifest);
            runtime.RegisterRuntimeApi<IChestLocatorEnhancerApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IStrongPlantingGunApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IAdvancedDebugApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<ICustomAnimalApi>(manifest, runtime.CustomEntities);
            runtime.RegisterRuntimeApi<ICustomMonsterApi>(manifest, runtime.CustomEntities);
            runtime.RegisterRuntimeApi<ICustomAttackApi>(manifest, runtime.CustomEntities);
            runtime.RegisterRuntimeApi<ICustomDroneApi>(manifest, runtime.CustomEntities);
        }

        internal void UpdateRuntimeAutomation(bool forceMachineProductionPoll = false)
        {
            experimentalApi?.UpdateRuntimeAutomation(forceMachineProductionPoll);
            cameraFeature?.RefreshForRuntime();
        }

        public void ResetCameraForLifecycleBoundary(string reason)
        {
            cameraFeature?.ResetForLifecycleBoundary(reason);
        }

        public void NotifyCameraEnvironmentReset(string reason)
        {
            cameraFeature?.NotifyEnvironmentReset(reason);
        }

        private void PublishStableCustomEntityHookStatuses()
        {
            runtime.SetHookStatus(
                "CustomEntities.CoreRegistry",
                "verified",
                "DTMAPI.Core.CustomEntityRegistryService",
                "Stable 0.4.0 custom animal, monster, attack/projectile, and drone APIs are registered with owner-aware validation, duplicate-ID detection, snapshots, save-boundary cleanup, and provider error isolation.");
            runtime.SetHookStatus(
                "CustomAnimals.StableApi",
                "configured-blocked",
                "AnimalManager.CreateAnimal + Animal lifecycle research",
                "Registry/status path is verified. Runtime creation remains blocked until AnimalInfo/proto, room/home, food, excrement, breeding, produce, and save adapters are verified without exposing raw Doloc Town types.");
            runtime.SetHookStatus(
                "CustomMonsters.StableApi",
                "configured-blocked",
                "MonsterController + MonsterGroupManager + MonsterAttackBehaviour research",
                "Registry/status path is verified. Runtime creation remains blocked until spawn groups, AI targeting, movement, attack, damage, drop, and despawn adapters are verified.");
            runtime.SetHookStatus(
                "CustomAttacks.ProjectileApi",
                "configured-blocked",
                "BulletFactory + BulletManager + PhysicalDamageBox research",
                "Registry/status path is verified. Runtime creation remains blocked until BulletManager/BulletFactory, hitbox/collision, damage ownership, and barrage tick adapters are verified.");
            runtime.SetHookStatus(
                "CustomDrones.StableApi",
                "configured-blocked",
                "DroneController + DroneWeapon + DolocAPI.EquipDrone research",
                "Registry/status path is verified. Runtime creation remains blocked until drone controller, equipment, weapon, movement, and save persistence adapters are verified.");
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

                if (!cameraZoomSetEnvCameraPatched)
                {
                    cameraZoomSetEnvCameraPatched = patcher.TryPatchPostfix("DolocAPI, Assembly-CSharp", "SetEnvCamera", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.DolocApiSetEnvCameraPostfix), BindingFlags.Public | BindingFlags.Static), 5);
                    runtime.SetHookStatus("Camera.ViewEnvironmentLifecycle", cameraZoomSetEnvCameraPatched ? "experimental" : "pending", "Harmony Postfix: DolocAPI.SetEnvCamera", cameraZoomSetEnvCameraPatched ? "Patched the native environment-camera reset boundary so active CameraView leases can reapply orthographic-size-only playable zoom after room transitions." : "Waiting for DolocAPI.SetEnvCamera to become patchable.");
                }

                if (!workshopReloadPatched)
                {
                    workshopReloadPatched = patcher.TryPatchPostfix("DolocTown.Config.ModManager, Assembly-CSharp", "ReloadMods", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ReloadModsPostfix), BindingFlags.Public | BindingFlags.Static));
                    runtime.SetHookStatus("Workshop.ReloadMods", workshopReloadPatched ? "experimental" : "pending", "Harmony Postfix: ModManager.ReloadMods", workshopReloadPatched ? "Patched to refresh DTMAPI diagnostics after official reload." : "Waiting for Assembly-CSharp/ModManager to become patchable.");
                }

                if (!chestLocatorAvailableInventoriesPatched)
                {
                    chestLocatorAvailableInventoriesPatched = patcher.TryPatchArrayResultPostfix(
                        "DolocTown.GameData.ArchiveDataHandle, Assembly-CSharp",
                        "GetAvailableInventories",
                        typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ArchiveDataHandleGetAvailableInventoriesPostfix), BindingFlags.Public | BindingFlags.Static),
                        3);
                    experimentalApi?.SetChestLocatorInventoryHookInstalled(chestLocatorAvailableInventoriesPatched);
                    runtime.SetHookStatus("Inventory.ChestLocatorEnhancer", chestLocatorAvailableInventoriesPatched ? "experimental" : "pending", "Harmony Postfix: ArchiveDataHandle.GetAvailableInventories", chestLocatorAvailableInventoriesPatched ? "Patched native inventory array enumeration so registered DTMAPI policies can append official shared container inventories without replacing CountItem/CostItem transaction logic." : "Waiting for ArchiveDataHandle.GetAvailableInventories to become patchable.");
                }

                if (!strongPlantingGunCtorPatched)
                {
                    MethodInfo? ctorPostfix = typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ItemFarmingGunCtorPostfix), BindingFlags.Public | BindingFlags.Static);
                    strongPlantingGunCtorPatched =
                        patcher.TryPatchConstructorPostfix("DolocTown.ItemFarmingGun, Assembly-CSharp", ctorPostfix, 2) |
                        patcher.TryPatchConstructorPostfix("DolocTown.ItemFarmingGun, Assembly-CSharp", ctorPostfix, 3);
                }

                if (!strongPlantingGunToolPatched)
                {
                    strongPlantingGunToolPatched = patcher.TryPatchPrefix("DolocTown.ItemFarmingGun, Assembly-CSharp", "OnUseAsTool", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ItemFarmingGunOnUseAsToolPrefix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!strongPlantingGunUiPlacePatched)
                {
                    strongPlantingGunUiPlacePatched = patcher.TryPatchPrefix("DolocTown.FarmingGunUiState, Assembly-CSharp", "HandlePlaceToOtherSide", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FarmingGunUiStateHandlePlaceToOtherSidePrefix), BindingFlags.Public | BindingFlags.Static), 1);
                }

                if (!strongPlantingGunUiSwapOnePatched)
                {
                    strongPlantingGunUiSwapOnePatched = patcher.TryPatchPrefix("DolocTown.FarmingGunUiState, Assembly-CSharp", "HandleSwapOneItem", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FarmingGunUiStateHandleSwapOneItemPrefix), BindingFlags.Public | BindingFlags.Static), 1);
                }

                bool strongPlantingGunToolHookReady = strongPlantingGunToolPatched;
                bool strongPlantingGunUiHooksReady = strongPlantingGunUiPlacePatched && strongPlantingGunUiSwapOnePatched;
                experimentalApi?.SetStrongPlantingGunHooksInstalled(strongPlantingGunToolHookReady, strongPlantingGunUiHooksReady, strongPlantingGunCtorPatched);
                runtime.SetHookStatus("Farming.StrongPlantingGun", (strongPlantingGunToolHookReady && strongPlantingGunUiHooksReady) ? "experimental" : "pending", "Harmony Prefix/Postfix: ItemFarmingGun + FarmingGunUiState", (strongPlantingGunToolHookReady && strongPlantingGunUiHooksReady) ? "Patched official farming gun construction, use, and UI transfer paths so registered DTMAPI policies can expose multi-slot seed/film/fertilizer behavior while delegating plant checks to official methods." : "Waiting for ItemFarmingGun/FarmingGunUiState targets to become patchable.");

                if (!actionSpeedToolEnterPatched)
                {
                    actionSpeedToolEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateTool, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentStateToolEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!actionSpeedToolExitPatched)
                {
                    actionSpeedToolExitPatched = patcher.TryPatchPostfix("DolocTown.AgentStateTool, Assembly-CSharp", "OnExit", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentStateToolExitPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!actionSpeedInteractEnterPatched)
                {
                    actionSpeedInteractEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateInteract, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentStateInteractEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!actionSpeedInteractExitPatched)
                {
                    actionSpeedInteractExitPatched = patcher.TryPatchPostfix("DolocTown.AgentStateInteract, Assembly-CSharp", "OnExit", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentStateInteractExitPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!actionSpeedEatEnterPatched)
                {
                    actionSpeedEatEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateEat, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentStateEatEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!actionSpeedUseItemContinuesPatched)
                {
                    actionSpeedUseItemContinuesPatched = patcher.TryPatchPrefix("DolocTown.AgentControllerState, Assembly-CSharp", "UseItemContinues", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentControllerStateUseItemContinuesPrefix), BindingFlags.Public | BindingFlags.Static), 1);
                }

                if (!actionSpeedBaseExitPatched)
                {
                    actionSpeedBaseExitPatched = patcher.TryPatchPostfix("AgentStateBase, Assembly-CSharp", "OnExit", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentStateBaseExitPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                bool actionSpeedToolHooksReady = actionSpeedToolEnterPatched && actionSpeedToolExitPatched;
                bool actionSpeedInteractionHooksReady = actionSpeedInteractEnterPatched && actionSpeedInteractExitPatched && actionSpeedEatEnterPatched && actionSpeedUseItemContinuesPatched && actionSpeedBaseExitPatched;
                experimentalApi?.SetActionSpeedToolHooksInstalled(actionSpeedToolHooksReady);
                experimentalApi?.SetActionSpeedInteractionHooksInstalled(actionSpeedInteractionHooksReady);
                runtime.SetHookStatus("ActionSpeed.ToolAnimation", actionSpeedToolHooksReady ? "verified" : "pending", "Harmony Postfix: AgentStateTool.OnEnter/OnExit", actionSpeedToolHooksReady ? "Patched tool animation speed and restore points; verified by ACTIONSPEED-001. Selected interaction slices are tracked separately in ACTIONSPEED-002." : "Waiting for AgentStateTool.OnEnter/OnExit to become patchable.");
                runtime.SetHookStatus("ActionSpeed.InteractionAnimation", actionSpeedInteractionHooksReady ? "experimental" : "pending", "Harmony Postfix/Prefix: AgentStateInteract/AgentStateEat/AgentControllerState.UseItemContinues", actionSpeedInteractionHooksReady ? "Patched shared interaction/eat animation speed points plus right-click continuous timer scaling. ACTIONSPEED-002 verifies fuel/feed add, eat/drink animation, bottled-water right-click continuous drink, IWaterContainer and in-water bottle fill, no-key auto-fill, planting, plant-basin crop harvest, resin collection, and wild vegetation harvest." : "Waiting for AgentStateInteract/AgentStateEat/UseItemContinues hooks to become patchable.");

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

                if (!oilCoalDropCapturePatched)
                {
                    oilCoalDropCapturePatched = patcher.TryPatchPrefix("DolocTown.ToolCollider, Assembly-CSharp", "HandleTools", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ToolColliderHandleToolsPrefix), BindingFlags.Public | BindingFlags.Static), 1);
                }

                if (!oneActionToolColliderPatched)
                {
                    oneActionToolColliderPatched = patcher.TryPatchPostfix("DolocTown.ToolCollider, Assembly-CSharp", "HandleTools", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ToolColliderHandleToolsPostfix), BindingFlags.Public | BindingFlags.Static), 1);
                }

                experimentalApi?.SetActionHooksInstalled(oneActionToolColliderPatched);
                runtime.SetHookStatus("Actions.OneActionComplete", oneActionToolColliderPatched ? "verified" : "pending", "Harmony Postfix: ToolCollider.HandleTools", oneActionToolColliderPatched ? "Patched resource/tool-hit path with native ResourceFellData validation; verified by ONEACTION-001/002. Fuel/feeder completion is tracked separately and verified by ONEACTION-002. Vegetation/dandelion uses native VegetationRenderer.OnFell and is verified as a non-DungeonResource exception in ONEACTION-003." : "Waiting for ToolCollider.HandleTools to become patchable.");
                runtime.SetHookStatus("Resources.OilCoalDrop", (oneActionToolColliderPatched && oilCoalDropCapturePatched) ? "experimental" : "pending", "Harmony Prefix/Postfix: ToolCollider.HandleTools", (oneActionToolColliderPatched && oilCoalDropCapturePatched) ? "Patched pre-hit coal resource capture plus post-hit oil placement; waiting for OilMod coal mining smoke evidence." : "Waiting for ToolCollider.HandleTools Prefix/Postfix to become patchable.");
                runtime.SetHookStatus("Actions.OneActionFuelFeed", actionSpeedInteractExitPatched ? "verified" : "pending", "Harmony Postfix: AgentStateInteract.OnExit", actionSpeedInteractExitPatched ? "Patched post-interact native CostSelf/AddFuel/AddFeeds path for fuel/feed targets; verified by ONEACTION-002 fuel/feed smoke." : "Waiting for AgentStateInteract.OnExit to become patchable.");

                if (!fishingReadyEnterPatched)
                {
                    fishingReadyEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingReady, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingReadyEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!fishingCastEnterPatched)
                {
                    fishingCastEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingCast, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingCastEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!fishingWaitEnterPatched)
                {
                    fishingWaitEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingWait, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingWaitEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!fishingWaitPlayPatched)
                {
                    fishingWaitPlayPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingWait, Assembly-CSharp", "OnPlay", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingWaitPlayPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!fishingMiniGameStartPatched)
                {
                    fishingMiniGameStartPatched = patcher.TryPatchPostfix("DolocTown.FishingGameScrollBar, Assembly-CSharp", "StartGame", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingMiniGameStartPostfix), BindingFlags.Public | BindingFlags.Static), 2);
                }

                if (!fishingMiniGameUpdatePatched)
                {
                    fishingMiniGameUpdatePatched = patcher.TryPatchPostfix("DolocTown.FishingGameScrollBar, Assembly-CSharp", "UpdateGame", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingMiniGameUpdatePostfix), BindingFlags.Public | BindingFlags.Static), 1);
                }

                if (!fishingMiniGameStopPatched)
                {
                    fishingMiniGameStopPatched = patcher.TryPatchPostfix("DolocTown.FishingGameScrollBar, Assembly-CSharp", "StopGame", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingMiniGameStopPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!fishingPullEnterPatched)
                {
                    fishingPullEnterPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingPull, Assembly-CSharp", "OnEnter", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingPullEnterPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!fishingPullExitPatched)
                {
                    fishingPullExitPatched = patcher.TryPatchPostfix("DolocTown.AgentStateFishingPull, Assembly-CSharp", "OnExit", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.FishingPullExitPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                bool fishingHooksReady = fishingReadyEnterPatched && fishingCastEnterPatched && fishingWaitEnterPatched && fishingWaitPlayPatched && fishingMiniGameStartPatched && fishingMiniGameUpdatePatched && fishingMiniGameStopPatched && fishingPullEnterPatched && fishingPullExitPatched;
                experimentalApi?.SetFishingHooksInstalled(fishingHooksReady);
                runtime.SetHookStatus("Fishing.Automation", fishingHooksReady ? "experimental" : "pending", "Harmony Postfix: fishing state/input phases", fishingHooksReady ? "Patched fishing phase observation hooks plus native BodyController.UseFishRod auto-cast, wait-phase InstantBite, and delayed FishingGameScrollBar.UpdateGame auto-complete; F6 auto-cast and wait phase verified by AUTOFISH-001." : "Waiting for fishing phase targets to become patchable.");

                if (!fishRoeTitlePatched)
                {
                    fishRoeTitlePatched = patcher.TryPatchPostfix("DolocTown.Item, Assembly-CSharp", "get_title", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ItemTitlePostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!fishRoeDescriptionPatched)
                {
                    fishRoeDescriptionPatched = patcher.TryPatchPostfix("DolocTown.Item, Assembly-CSharp", "get_description", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ItemDescriptionPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!fishRoeDetailPatched)
                {
                    fishRoeDetailPatched = patcher.TryPatchPostfix("DolocTown.Item, Assembly-CSharp", "GetDetailInfo", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ItemDetailInfoPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                bool fishRoeHooksReady = fishRoeTitlePatched && fishRoeDescriptionPatched && fishRoeDetailPatched;
                experimentalApi?.SetFishRoeHooksInstalled(fishRoeHooksReady);
                runtime.SetHookStatus("Items.FishRoeTooltip", fishRoeHooksReady ? "verified" : "pending", "Harmony Postfix: Item.title/description/GetDetailInfo", fishRoeHooksReady ? "Patched item display paths for fish roe providers; verified by FISHROE-001." : "Waiting for item display targets to become patchable.");

                if (!animalFullInfoDataPatched)
                {
                    animalFullInfoDataPatched = patcher.TryPatchConstructorPostfix("DolocTown.UI.AnimalFullInfoData, Assembly-CSharp", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalFullInfoDataCtorPostfix), BindingFlags.Public | BindingFlags.Static), 1);
                }

                if (!animalViewerShowPatched)
                {
                    animalViewerShowPrefixPatched = patcher.TryPatchPrefix("DolocTown.UI.AnimalViewer, Assembly-CSharp", "Show", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalViewerShowPrefix), BindingFlags.Public | BindingFlags.Static), 1);
                    animalViewerShowPatched = patcher.TryPatchPostfix("DolocTown.UI.AnimalViewer, Assembly-CSharp", "Show", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalViewerShowPostfix), BindingFlags.Public | BindingFlags.Static), 1);
                }

                if (!animalPanelRefreshViewerPatched)
                {
                    animalPanelRefreshViewerPatched = patcher.TryPatchPostfix("DolocTown.UI.AnimalPanel, Assembly-CSharp", "RefreshViewer", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AnimalPanelRefreshViewerPostfix), BindingFlags.Public | BindingFlags.Static), 1);
                }

                bool animalViewerHooksReady = animalFullInfoDataPatched && animalViewerShowPrefixPatched && animalViewerShowPatched && animalPanelRefreshViewerPatched;
                experimentalApi?.SetAnimalViewerHookInstalled(animalViewerHooksReady);
                runtime.SetHookStatus("Animals.ViewerRendering", animalViewerHooksReady ? "verified" : "pending", "Harmony Prefix/Postfix: AnimalFullInfoData(Animal) + AnimalViewer.Show + AnimalPanel.RefreshViewer", animalViewerHooksReady ? "Patched animal viewer data construction plus prefilled independent progress rows before native Show settles, with real UI refresh evidence in ANIMAL-001." : "Waiting for animal viewer data/UI targets to become patchable.");

                if (!motorKeyUsePatched)
                {
                    motorKeyUsePatched = patcher.TryPatchPrefix("DolocTown.ItemMotorKey, Assembly-CSharp", "OnUse", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ItemMotorKeyOnUsePrefix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!motorInteractPatched)
                {
                    motorInteractPatched = patcher.TryPatchPrefix("DolocTown.MotorInteractable, Assembly-CSharp", "OnInteract", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.MotorInteractableOnInteractPrefix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!motorGetOnPatched)
                {
                    motorGetOnPatched = patcher.TryPatchPostfix("DolocTown.AgentControllerState, Assembly-CSharp", "GetOnMotor", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentControllerStateGetOnMotorPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!motorGetOffPatched)
                {
                    motorGetOffPatched = patcher.TryPatchPostfix("DolocTown.AgentControllerState, Assembly-CSharp", "GetOffMotor", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.AgentControllerStateGetOffMotorPostfix), BindingFlags.Public | BindingFlags.Static), 0);
                }

                if (!motorFixedUpdatePrefixPatched)
                {
                    motorFixedUpdatePrefixPatched = patcher.TryPatchPrefix("DolocTown.MotorController, Assembly-CSharp", "OnFixedUpdate", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.MotorControllerOnFixedUpdatePrefix), BindingFlags.Public | BindingFlags.Static), 1);
                }

                if (!motorFixedUpdatePostfixPatched)
                {
                    motorFixedUpdatePostfixPatched = patcher.TryPatchPostfix("DolocTown.MotorController, Assembly-CSharp", "OnFixedUpdate", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.MotorControllerOnFixedUpdatePostfix), BindingFlags.Public | BindingFlags.Static), 1);
                }

                if (!motorUnlockPatched)
                {
                    motorUnlockPatched = patcher.TryPatchPostfix("DolocAPI, Assembly-CSharp", "UnlockMotor", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.UnlockMotorPostfix), BindingFlags.Public | BindingFlags.Static), 1);
                }

                if (!motorSetPositionPatched)
                {
                    motorSetPositionPatched = patcher.TryPatchPostfix("DolocAPI, Assembly-CSharp", "SetMotorPosition", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.SetMotorPositionPostfix), BindingFlags.Public | BindingFlags.Static), 2);
                }

                if (!motorEnterRoomPatched)
                {
                    motorEnterRoomPatched = patcher.TryPatchPostfix("DolocAPI, Assembly-CSharp", "EnterRoom", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.DolocApiEnterRoomPostfix), BindingFlags.Public | BindingFlags.Static), 3);
                }

                bool motorHooksReady = motorKeyUsePatched && motorInteractPatched && motorGetOnPatched && motorGetOffPatched && motorFixedUpdatePrefixPatched && motorFixedUpdatePostfixPatched && motorUnlockPatched && motorSetPositionPatched && motorEnterRoomPatched;
                experimentalApi?.SetMotorVehicleHooksInstalled(motorHooksReady);
                runtime.SetHookStatus("Vehicle.MotorApi", motorHooksReady ? "experimental" : "pending", "Harmony: ItemMotorKey/MotorInteractable/AgentControllerState/MotorController/DolocAPI", motorHooksReady ? "Patched native motor key, riding, tuning, unlock, position, and room-entry touchpoints. Second-motor routing remains experimental and must be smoke-verified." : "Waiting for native motor hook targets to become patchable.");

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

                if (!hookResolutionDiagnosticLogged && !AllHookTargetsReady && (DateTimeOffset.Now - initializedAt).TotalSeconds >= 4)
                {
                    hookResolutionDiagnosticLogged = true;
                    runtime.RuntimeMonitor.Log("Hook target resolution diagnostic:" + Environment.NewLine + patcher.BuildTypeResolutionReport(
                        "DolocAPI, Assembly-CSharp",
                        "DolocTown.HomePageUiState, Assembly-CSharp",
                        "DolocTown.GameData.DataPersistenceManager, Assembly-CSharp",
                        "DolocTown.Config.ModManager, Assembly-CSharp",
                        "DolocTown.AgentStateTool, Assembly-CSharp",
                        "DolocTown.AgentStateInteract, Assembly-CSharp",
                        "DolocTown.AgentStateEat, Assembly-CSharp",
                        "DolocTown.AgentControllerState, Assembly-CSharp",
                        "AgentStateBase, Assembly-CSharp",
                        "DolocTown.ToolCollider, Assembly-CSharp",
                        "DolocTown.DungeonResource, Assembly-CSharp",
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
                        "DolocTown.ItemMotorKey, Assembly-CSharp",
                        "DolocTown.MotorInteractable, Assembly-CSharp",
                        "DolocTown.MotorController, Assembly-CSharp",
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
