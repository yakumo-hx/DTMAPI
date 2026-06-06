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
    public sealed class DolocTownGameBridge
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

        public DolocTownGameBridge(DtmApiRuntime runtime, Func<bool>? clickTitleSettingsButton = null, IDebugConsoleApi? debugConsoleApi = null)
        {
            this.runtime = runtime;
            this.clickTitleSettingsButton = clickTitleSettingsButton;
            this.debugConsoleApi = debugConsoleApi;
            RegisterExperimentalApis();
        }

        internal DolocTownExperimentalBridgeApi? ExperimentalApi => experimentalApi;

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
            if (experimentalApi != null)
                return;
            experimentalApi = new DolocTownExperimentalBridgeApi(runtime);
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
            runtime.RegisterRuntimeApi<ICameraZoomApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IChestLocatorEnhancerApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IStrongPlantingGunApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<IAdvancedDebugApi>(manifest, experimentalApi);
            runtime.RegisterRuntimeApi<ICustomAnimalApi>(manifest, runtime.CustomEntities);
            runtime.RegisterRuntimeApi<ICustomMonsterApi>(manifest, runtime.CustomEntities);
            runtime.RegisterRuntimeApi<ICustomAttackApi>(manifest, runtime.CustomEntities);
            runtime.RegisterRuntimeApi<ICustomDroneApi>(manifest, runtime.CustomEntities);
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

        public void Update()
        {
            RefreshUiContext();
            experimentalApi?.UpdateRuntimeAutomation();
            if (smokeSettings == null)
                LoadSmokeSettings();
            if (smokeSettings == null || !smokeSettings.Enabled)
                return;

            double seconds = (DateTimeOffset.Now - initializedAt).TotalSeconds;
            if (!autoExerciseTitleButtonLifecycleAttempted && smokeSettings.AutoExerciseTitleButtonLifecycle && seconds >= Math.Max(1, smokeSettings.AutoOpenTitleSettingsDelaySeconds))
            {
                SmokeAttemptResult lifecycleResult = TryExerciseTitleButtonLifecycleForSmoke();
                if (lifecycleResult == SmokeAttemptResult.Pending)
                    return;

                autoExerciseTitleButtonLifecycleAttempted = true;
                if (!autoExitAttempted)
                {
                    autoExitAttempted = true;
                    TryQuitApplication(lifecycleResult == SmokeAttemptResult.Succeeded ? "smoke title button lifecycle evidence captured" : "smoke title button lifecycle failed");
                }
            }
            if (!autoOpenTitleSettingsAttempted && smokeSettings.AutoOpenTitleSettingsMenu && seconds >= Math.Max(1, smokeSettings.AutoOpenTitleSettingsDelaySeconds))
            {
                autoOpenTitleSettingsAttempted = TryAutoOpenTitleSettingsMenu();
            }
            if (!autoOpenOfficialModUiAttempted && smokeSettings.AutoOpenOfficialModUi && seconds >= Math.Max(1, smokeSettings.AutoOpenOfficialModUiDelaySeconds))
            {
                SmokeAttemptResult officialModUiResult = TryAutoOpenOfficialModUiForSmoke();
                if (officialModUiResult == SmokeAttemptResult.Pending)
                    return;

                autoOpenOfficialModUiAttempted = true;
                if (officialModUiResult == SmokeAttemptResult.Succeeded && !autoExitAttempted)
                {
                    autoExitAttempted = true;
                    TryQuitApplication("official Mod UI evidence captured");
                }
            }
            if (!autoLoadAttempted && !smokeSettings.AutoExerciseTitleButtonLifecycle && smokeSettings.AutoLoadSaveSlot > 0 && seconds >= Math.Max(1, smokeSettings.AutoLoadDelaySeconds))
            {
                autoLoadAttempted = TryAutoLoadSave(smokeSettings.AutoLoadSaveSlot);
            }
            if (!autoReloadModsAttempted && smokeSettings.AutoReloadMods && seconds >= Math.Max(1, smokeSettings.AutoReloadModsDelaySeconds))
            {
                autoReloadModsAttempted = true;
                TryAutoReloadMods();
            }
            if (autoLoadOfficialPathRequested && !modChangePromptConfirmed)
                TryConfirmModChangePrompt();
            if (!autoSaveAttempted && pendingAutoSaveIndex.HasValue && seconds >= Math.Max(1, smokeSettings.AutoLoadDelaySeconds) &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoSaveDelaySeconds))
            {
                autoSaveAttempted = true;
                TryAutoSave(pendingAutoSaveIndex.Value);
            }
            if (!autoExerciseInstantSaveAttempted && smokeSettings.AutoExerciseInstantSave && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseInstantSaveDelaySeconds))
            {
                autoExerciseInstantSaveAttempted = true;
                TryExerciseInstantSaveForSmoke();
            }
            if (smokeSettings.AutoExerciseDebugTeleport && debugTeleportRequestedAt != default && !debugTeleportVerificationCompleted &&
                (DateTimeOffset.Now - debugTeleportRequestedAt).TotalSeconds >= 4)
            {
                debugTeleportVerificationCompleted = true;
                CompleteDebugTeleportForSmoke();
                TryQuitAfterDebugSmoke();
            }
            if (!autoExerciseDebugConsoleAttempted && smokeSettings.AutoExerciseDebugConsole && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseDebugConsoleDelaySeconds))
            {
                autoExerciseDebugConsoleAttempted = true;
                TryExerciseDebugConsoleHotkeyForSmoke();
                TryQuitAfterDebugSmoke();
            }
            if (!autoExerciseDebugInventoryAttempted && smokeSettings.AutoExerciseDebugInventory && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseDebugInventoryDelaySeconds))
            {
                autoExerciseDebugInventoryAttempted = true;
                TryExerciseDebugInventoryForSmoke();
                TryQuitAfterDebugSmoke();
            }
            if (!autoExerciseDebugWeatherAttempted && smokeSettings.AutoExerciseDebugWeather && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseDebugWeatherDelaySeconds))
            {
                autoExerciseDebugWeatherAttempted = true;
                TryExerciseDebugWeatherForSmoke();
                TryQuitAfterDebugSmoke();
            }
            if (!autoExerciseDebugTimeAttempted && smokeSettings.AutoExerciseDebugTime && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseDebugTimeDelaySeconds))
            {
                autoExerciseDebugTimeAttempted = true;
                TryExerciseDebugTimeForSmoke();
                TryQuitAfterDebugSmoke();
            }
            if (!autoExerciseDebugMovementAttempted && smokeSettings.AutoExerciseDebugMovement && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseDebugMovementDelaySeconds))
            {
                autoExerciseDebugMovementAttempted = true;
                TryExerciseDebugMovementForSmoke();
                TryQuitAfterDebugSmoke();
            }
            if (!autoExerciseAdvancedDebugAttempted && smokeSettings.AutoExerciseAdvancedDebug && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseAdvancedDebugDelaySeconds))
            {
                autoExerciseAdvancedDebugAttempted = true;
                TryExerciseAdvancedDebugForSmoke();
                TryQuitAfterDebugSmoke();
            }
            if (!autoExerciseVehicleAttempted && smokeSettings.AutoExerciseVehicle && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseVehicleDelaySeconds))
            {
                SmokeAttemptResult vehicleResult = TryExerciseVehicleForSmoke();
                if (vehicleResult == SmokeAttemptResult.Pending)
                    return;

                autoExerciseVehicleAttempted = true;
                TryQuitAfterDebugSmoke();
            }
            if (!autoExerciseNewContentApisAttempted && smokeSettings.AutoExerciseNewContentApis && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseNewContentApisDelaySeconds))
            {
                SmokeAttemptResult newContentResult = TryExerciseNewContentApisForSmoke(mineOnly: false);
                if (newContentResult == SmokeAttemptResult.Pending)
                    return;

                autoExerciseNewContentApisAttempted = true;
                TryQuitAfterDebugSmoke();
            }
            if (!autoExerciseMineContentApisAttempted && smokeSettings.AutoExerciseMineContentApis && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseMineContentApisDelaySeconds))
            {
                SmokeAttemptResult mineContentResult = TryExerciseNewContentApisForSmoke(mineOnly: true);
                if (mineContentResult == SmokeAttemptResult.Pending)
                    return;

                autoExerciseMineContentApisAttempted = true;
                TryQuitAfterDebugSmoke();
            }
            if (!autoExerciseZoomAttempted && smokeSettings.AutoExerciseZoom && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseZoomDelaySeconds))
            {
                autoExerciseZoomAttempted = true;
                TryExerciseZoomForSmoke();
                TryQuitAfterDebugSmoke();
            }
            if (!autoExerciseChestLocatorEnhancerAttempted && smokeSettings.AutoExerciseChestLocatorEnhancer && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseChestLocatorEnhancerDelaySeconds))
            {
                autoExerciseChestLocatorEnhancerAttempted = true;
                TryExerciseChestLocatorEnhancerForSmoke();
                TryQuitAfterDebugSmoke();
            }
            if (!autoExerciseStrongPlantingGunAttempted && smokeSettings.AutoExerciseStrongPlantingGun && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseStrongPlantingGunDelaySeconds))
            {
                autoExerciseStrongPlantingGunAttempted = true;
                TryExerciseStrongPlantingGunForSmoke();
                TryQuitAfterDebugSmoke();
            }
            if (!autoExerciseCustomEntityApisAttempted && smokeSettings.AutoExerciseCustomEntityApis && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseCustomEntityApisDelaySeconds))
            {
                autoExerciseCustomEntityApisAttempted = true;
                TryExerciseCustomEntityApisForSmoke();
                TryQuitAfterDebugSmoke();
            }
            if (!autoExerciseDebugTeleportAttempted && smokeSettings.AutoExerciseDebugTeleport && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseDebugTeleportDelaySeconds))
            {
                autoExerciseDebugTeleportAttempted = true;
                SmokeAttemptResult teleportResult = TryExerciseDebugTeleportForSmoke();
                if (teleportResult != SmokeAttemptResult.Pending)
                {
                    debugTeleportVerificationCompleted = true;
                    TryQuitAfterDebugSmoke();
                }
                else
                {
                    return;
                }
            }
            if (!autoOpenAnimalPanelAttempted && smokeSettings.AutoOpenAnimalPanel && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoOpenAnimalPanelDelaySeconds))
            {
                autoOpenAnimalPanelAttempted = true;
                TryAutoOpenAnimalPanel();
            }
            if (!autoExerciseActionSpeedToolAttempted && smokeSettings.AutoExerciseActionSpeedTool && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseActionSpeedToolDelaySeconds))
            {
                SmokeAttemptResult actionSpeedResult = TryExerciseActionSpeedToolForSmoke();
                if (actionSpeedResult == SmokeAttemptResult.Pending)
                    return;

                autoExerciseActionSpeedToolAttempted = true;
                if (actionSpeedResult == SmokeAttemptResult.Succeeded && smokeSettings.AutoExitAfterSaveLoaded && !autoExitAttempted)
                {
                    autoExitAttempted = true;
                    TryQuitApplication("smoke action-speed tool evidence captured");
                }
            }
            if (!autoExerciseActionSpeedConfigApplyAttempted && smokeSettings.AutoExerciseActionSpeedConfigApply && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseActionSpeedConfigApplyDelaySeconds))
            {
                SmokeAttemptResult actionSpeedConfigResult = TryExerciseActionSpeedConfigApplyForSmoke();
                if (actionSpeedConfigResult == SmokeAttemptResult.Pending)
                    return;

                autoExerciseActionSpeedConfigApplyAttempted = true;
                if (actionSpeedConfigResult == SmokeAttemptResult.Succeeded && smokeSettings.AutoExitAfterSaveLoaded && !autoExitAttempted)
                {
                    autoExitAttempted = true;
                    TryQuitApplication("smoke action-speed config-apply evidence captured");
                }
            }
            if (!autoExerciseActionSpeedInteractionAttempted && smokeSettings.AutoExerciseActionSpeedInteraction && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseActionSpeedInteractionDelaySeconds))
            {
                SmokeAttemptResult actionSpeedInteractionResult = TryExerciseActionSpeedInteractionForSmoke();
                if (actionSpeedInteractionResult == SmokeAttemptResult.Pending)
                    return;

                autoExerciseActionSpeedInteractionAttempted = true;
                if (actionSpeedInteractionResult == SmokeAttemptResult.Succeeded && smokeSettings.AutoExitAfterSaveLoaded && !autoExitAttempted)
                {
                    autoExitAttempted = true;
                    TryQuitApplication("smoke action-speed interaction evidence captured");
                }
            }
            if (!autoExerciseOneActionResourceHitAttempted && smokeSettings.AutoExerciseOneActionResourceHit && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseOneActionResourceHitDelaySeconds))
            {
                SmokeAttemptResult oneActionResult = TryExerciseOneActionResourceHitForSmoke();
                if (oneActionResult == SmokeAttemptResult.Pending)
                    return;

                autoExerciseOneActionResourceHitAttempted = true;
                if (oneActionResult == SmokeAttemptResult.Succeeded && smokeSettings.AutoExitAfterSaveLoaded && !autoExitAttempted)
                {
                    autoExitAttempted = true;
                    TryQuitApplication("smoke one-action resource-hit evidence captured");
                }
            }
            if (!autoExerciseOneActionWrongToolAttempted && smokeSettings.AutoExerciseOneActionWrongTool && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseOneActionWrongToolDelaySeconds))
            {
                SmokeAttemptResult oneActionWrongToolResult = TryExerciseOneActionWrongToolForSmoke();
                if (oneActionWrongToolResult == SmokeAttemptResult.Pending)
                    return;

                autoExerciseOneActionWrongToolAttempted = true;
                if (oneActionWrongToolResult == SmokeAttemptResult.Succeeded && smokeSettings.AutoExitAfterSaveLoaded && !autoExitAttempted)
                {
                    autoExitAttempted = true;
                    TryQuitApplication("smoke one-action wrong-tool evidence captured");
                }
            }
            if (!autoExerciseOneActionFuelFeedAttempted && smokeSettings.AutoExerciseOneActionFuelFeed && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseOneActionFuelFeedDelaySeconds))
            {
                SmokeAttemptResult oneActionFuelFeedResult = TryExerciseOneActionFuelFeedForSmoke();
                if (oneActionFuelFeedResult == SmokeAttemptResult.Pending)
                    return;

                autoExerciseOneActionFuelFeedAttempted = true;
                if (oneActionFuelFeedResult == SmokeAttemptResult.Succeeded && smokeSettings.AutoExitAfterSaveLoaded && !autoExitAttempted)
                {
                    autoExitAttempted = true;
                    TryQuitApplication("smoke one-action fuel/feed evidence captured");
                }
            }
            if (!autoExerciseOneActionVegetationAttempted && smokeSettings.AutoExerciseOneActionVegetation && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseOneActionVegetationDelaySeconds))
            {
                SmokeAttemptResult oneActionVegetationResult = TryExerciseOneActionVegetationForSmoke();
                if (oneActionVegetationResult == SmokeAttemptResult.Pending)
                    return;

                autoExerciseOneActionVegetationAttempted = true;
                if (oneActionVegetationResult == SmokeAttemptResult.Succeeded && smokeSettings.AutoExitAfterSaveLoaded && !autoExitAttempted)
                {
                    autoExitAttempted = true;
                    TryQuitApplication("smoke one-action vegetation evidence captured");
                }
            }
            if (!autoExerciseAutoFishingPhaseAttempted && smokeSettings.AutoExerciseAutoFishingPhase && saveLoadedAt != default &&
                (DateTimeOffset.Now - saveLoadedAt).TotalSeconds >= Math.Max(1, smokeSettings.AutoExerciseAutoFishingPhaseDelaySeconds))
            {
                SmokeAttemptResult autoFishingResult = TryExerciseAutoFishingPhaseForSmoke();
                if (autoFishingResult == SmokeAttemptResult.Pending)
                    return;

                autoExerciseAutoFishingPhaseAttempted = true;
                if (autoFishingResult == SmokeAttemptResult.Succeeded && smokeSettings.AutoExitAfterSaveLoaded && !autoExitAttempted)
                {
                    autoExitAttempted = true;
                    TryQuitApplication("smoke auto-fishing phase evidence captured");
                }
            }
            if (!animalViewerUiDelayedScreenshotRequested && animalViewerUiEvidenceAt != default &&
                (DateTimeOffset.Now - animalViewerUiEvidenceAt).TotalSeconds >= 1.5)
            {
                animalViewerUiDelayedScreenshotRequested = true;
                experimentalApi?.CaptureDelayedAnimalViewerUiEvidenceScreenshot();
            }
            if (!titleSettingsMenuScreenshotRequested && titleSettingsMenuEvidenceAt != default &&
                (DateTimeOffset.Now - titleSettingsMenuEvidenceAt).TotalSeconds >= 1.5)
            {
                titleSettingsMenuScreenshotRequested = true;
                CaptureTitleSettingsMenuEvidenceScreenshot();
            }
            if (smokeSettings.AutoOpenTitleSettingsMenu && titleSettingsMenuScreenshotRequested && titleSettingsConfigScreenshotStage < 9)
                UpdateTitleSettingsConfigEvidenceScreenshots();
            if (!autoExitAttempted && animalViewerUiEvidenceAt != default &&
                (DateTimeOffset.Now - animalViewerUiEvidenceAt).TotalSeconds >= 5)
            {
                autoExitAttempted = true;
                TryQuitApplication("smoke animal viewer UI evidence captured");
            }

            if (!autoExitAttempted && smokeSettings.AutoExitAfterSeconds > 0 && seconds >= smokeSettings.AutoExitAfterSeconds)
            {
                autoExitAttempted = true;
                TryQuitApplication("smoke auto-exit timer elapsed");
            }
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

        public void MarkSaveLoadedForSmoke()
        {
            if (smokeSettings == null || !smokeSettings.Enabled || !smokeSettings.AutoExitAfterSaveLoaded || autoExitAttempted)
                return;
            saveLoadedAt = DateTimeOffset.Now;
            if (smokeSettings.AutoExerciseExperimentalHooks && !autoExerciseAttempted)
            {
                autoExerciseAttempted = true;
                TryExerciseExperimentalHooksForSmoke();
            }
            if (smokeSettings.AutoExerciseDebugConsole || smokeSettings.AutoExerciseDebugInventory || smokeSettings.AutoExerciseDebugWeather || smokeSettings.AutoExerciseDebugTeleport || smokeSettings.AutoExerciseDebugTime || smokeSettings.AutoExerciseDebugMovement || smokeSettings.AutoExerciseAdvancedDebug || smokeSettings.AutoExerciseVehicle || smokeSettings.AutoExerciseZoom || smokeSettings.AutoExerciseChestLocatorEnhancer || smokeSettings.AutoExerciseStrongPlantingGun || smokeSettings.AutoExerciseCustomEntityApis)
            {
                if (smokeSettings.AutoExerciseDebugConsole)
                    runtime.SetHookStatus("Smoke.DebugConsoleHotkey", "pending", "DTMAPI.DebugConsoleMod + Unity UI Canvas", "Save loaded; in-game smoke will exercise Y/Escape/Y/Y, ten short taps, and held-Y no-flicker.");
                if (smokeSettings.AutoExerciseDebugInventory)
                    runtime.SetHookStatus("Smoke.DebugInventory", "pending", "IInventoryDebugApi", "Waiting after save load to give one official item through native backpack placement.");
                if (smokeSettings.AutoExerciseDebugWeather)
                    runtime.SetHookStatus("Smoke.DebugWeather", "pending", "IWeatherDebugApi", "Waiting after save load to list and switch a native weather option.");
                if (smokeSettings.AutoExerciseDebugTeleport)
                    runtime.SetHookStatus("Smoke.DebugTeleport", "pending", "ITeleportDebugApi", "Waiting after save load to request a whitelisted native mark-point transport.");
                if (smokeSettings.AutoExerciseDebugTime)
                    runtime.SetHookStatus("Smoke.DebugTime", "pending", "ITimeDebugApi", "Waiting after save load to skip to the next weather period through native time pass.");
                if (smokeSettings.AutoExerciseDebugMovement)
                    runtime.SetHookStatus("Smoke.DebugMovement", "pending", "IMovementDebugApi", "Waiting after save load to cycle 1x/2x/3x/4x and restore 1x.");
                if (smokeSettings.AutoExerciseAdvancedDebug)
                    runtime.SetHookStatus("Smoke.AdvancedDebug", "pending", "IAdvancedDebugApi whitelist", "Waiting after save load to exercise time advance, time scale, value grants, creative toggle, and current-room spawn probes.");
                if (smokeSettings.AutoExerciseVehicle)
                    runtime.SetHookStatus("Smoke.VehicleSecondMotor", "pending", "IMotorVehicleApi + ItemMotorKey.OnUse", "Waiting after save load to verify SecondMotor registration, official mod item key, key summon, ride, dismount, and original motor summon.");
                if (smokeSettings.AutoExerciseZoom)
                    runtime.SetHookStatus("Smoke.Zoom", "pending", "ICameraZoomApi", "Waiting after save load to register the Zoom policy, apply a 4x camera view, and restore vanilla view.");
                if (smokeSettings.AutoExerciseChestLocatorEnhancer)
                    runtime.SetHookStatus("Smoke.ChestLocatorEnhancer", "pending", "IChestLocatorEnhancerApi + ArchiveDataHandle.GetAvailableInventories", "Waiting after save load to create a transient shared Case in a building room and verify CountItem/CostItem through the native shared-inventory array.");
                if (smokeSettings.AutoExerciseStrongPlantingGun)
                    runtime.SetHookStatus("Smoke.StrongPlantingGun", "pending", "IStrongPlantingGunApi + ItemFarmingGun", "Waiting after save load to generate an official farming gun, expose three slots, place seed/film/fertilizer, and apply them to a temporary plant basin.");
                if (smokeSettings.AutoExerciseCustomEntityApis)
                    runtime.SetHookStatus("Smoke.CustomEntityApis", "pending", "ICustomAnimalApi/ICustomMonsterApi/ICustomAttackApi/ICustomDroneApi", "Waiting after save load to register stable 0.4.0 custom entity definitions, verify snapshots/status, confirm duplicate validation, confirm runtime-creation-blocked request results, and clean up the smoke owner.");
                return;
            }
            if (smokeSettings.AutoOpenAnimalPanel)
            {
                runtime.SetHookStatus("Smoke.AnimalPanelUi", "pending", "AnimalPanelUiState", "Waiting after save load to open the official animal panel UI.");
                return;
            }
            if (smokeSettings.AutoExerciseActionSpeedTool)
            {
                runtime.SetHookStatus("Smoke.ActionSpeedTool", "pending", "AgentStateTool.OnEnter", "Waiting after save load to exercise a real AgentStateTool tool animation speed hook.");
                return;
            }
            if (smokeSettings.AutoExerciseActionSpeedConfigApply)
            {
                runtime.SetHookStatus("Smoke.ActionSpeedConfigApply", "pending", "DTMAPI ConfigMenu Save", "Waiting after save load to verify that saved ActionSpeed config changes update the active tool-animation policy without restart.");
                return;
            }
            if (smokeSettings.AutoExerciseActionSpeedInteraction)
            {
                runtime.SetHookStatus("Smoke.ActionSpeedInteraction", "pending", "AgentStateInteract/AgentStateEat/UseItemContinues", "Waiting after save load to exercise real ActionSpeed interaction, eat, and continuous-use paths.");
                return;
            }
            if (smokeSettings.AutoExerciseOneActionResourceHit)
            {
                runtime.SetHookStatus("Smoke.OneActionResourceHit", "pending", "ToolCollider.HandleTools", "Waiting after save load to exercise a real rendered resource hit through the ToolCollider path.");
                return;
            }
            if (smokeSettings.AutoExerciseOneActionWrongTool)
            {
                runtime.SetHookStatus("Smoke.OneActionWrongTool", "pending", "ToolCollider.HandleTools", "Waiting after save load to verify a mismatched tool does not trigger DTMAPI one-action completion.");
                return;
            }
            if (smokeSettings.AutoExerciseOneActionFuelFeed)
            {
                runtime.SetHookStatus("Smoke.OneActionFuelFeed", "pending", "AgentStateInteract.OnExit", "Waiting after save load to exercise native fuel/feed item consumption through the interact completion path.");
                return;
            }
            if (smokeSettings.AutoExerciseOneActionVegetation)
            {
                runtime.SetHookStatus("Smoke.OneActionVegetation", "pending", "ToolCollider.HandleTools -> VegetationRenderer.OnFell", "Waiting after save load to verify vegetation/dandelion native tool constraints and DTMAPI non-interference.");
                return;
            }
            if (smokeSettings.AutoExerciseAutoFishingPhase)
            {
                runtime.SetHookStatus("Smoke.AutoFishingPhase", "pending", "AgentStateFishingWait.OnPlay", "Waiting after save load to toggle AutoFishing and exercise the instant-bite wait phase.");
                if (smokeSettings.AutoExerciseAutoFishingMiniGameComplete)
                    runtime.SetHookStatus("Smoke.AutoFishingMiniGameComplete", "pending", "FishingGameScrollBar.UpdateGame", "SkipMiniGame=false; waiting for visible minigame auto-complete evidence.");
                return;
            }
            if (smokeSettings.AutoExerciseTitleButtonLifecycle)
            {
                runtime.SetHookStatus("Smoke.TitleButtonLifecycle", "pending", "DolocAPI.ReturnHome", "Save loaded; waiting briefly before returning to the title homepage.");
                return;
            }
            if (smokeSettings.AutoExerciseInstantSave)
            {
                runtime.SetHookStatus("Smoke.InstantSave", "pending", "DolocAPI.SaveGame", "Save loaded; waiting briefly before invoking the save-only instant-save debug path. Native immediate reload is disabled.");
                return;
            }
            if (smokeSettings.AutoExerciseNewContentApis)
            {
                runtime.SetHookStatus("Smoke.NewContentOilCoalDrop", "pending", "ToolCollider.HandleTools + OilMod crude_oil", "Save loaded; waiting briefly before creating or finding a coal resource, breaking it with a native pickaxe hit, and forcing the smoke-only oil roll.");
                runtime.SetHookStatus("Smoke.NewContentEquipmentSlots", "pending", "IEquipmentSlotsApi + passive/hat equipment", "Save loaded; waiting briefly before giving passive and hat items, equipping them into DTMAPI extra slots, capturing interactive UI evidence, and recovering them.");
                runtime.SetHookStatus("Smoke.NewContentMineProduction", "pending", "IMachineProductionApi + transient dtmapi_mine equipment", "Save loaded; waiting briefly before creating a temporary mine and forcing one runtime production cycle for evidence.");
                return;
            }
            if (smokeSettings.AutoExerciseMineContentApis)
            {
                runtime.SetHookStatus("Smoke.NewContentMineOfficialJson", "pending", "official JSON + native tech table", "Save loaded; waiting briefly before verifying Mine tech cost/unlock payload.");
                runtime.SetHookStatus("Smoke.NewContentMineProduction", "pending", "IMachineProductionApi + transient dtmapi_mine equipment", "Save loaded; waiting briefly before creating a temporary Mine, checking visual containment, and forcing one runtime production cycle.");
                return;
            }
            if (smokeSettings.AutoSaveAfterLoad && pendingAutoLoadGameIndex.HasValue && !autoSaveAttempted)
            {
                pendingAutoSaveIndex = pendingAutoLoadGameIndex.Value;
                runtime.SetHookStatus("Smoke.AutoSave", "pending", "DolocAPI.SaveGame", "Waiting to save slot/index " + pendingAutoSaveIndex.Value + " after load.");
                return;
            }
            autoExitAttempted = true;
            TryQuitApplication("smoke save-loaded evidence captured");
        }

        public void MarkAnimalViewerUiEvidenceForSmoke()
        {
            if (smokeSettings == null || !smokeSettings.Enabled || !smokeSettings.AutoOpenAnimalPanel || !smokeSettings.AutoExitAfterSaveLoaded || autoExitAttempted)
                return;
            animalViewerUiEvidenceAt = DateTimeOffset.Now;
            runtime.SetHookStatus("Smoke.AutoExit", "pending", "AnimalViewer.Show", "Waiting briefly for screenshot capture before quitting.");
        }

        public void MarkSaveSavedForSmoke()
        {
            if (smokeSettings == null || !smokeSettings.Enabled || !smokeSettings.AutoSaveAfterLoad || !smokeSettings.AutoExitAfterSaveLoaded || autoExitAttempted)
                return;
            autoExitAttempted = true;
            TryQuitApplication("smoke save-saved evidence captured");
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

        private void LoadSmokeSettings()
        {
            try
            {
                if (!File.Exists(runtime.Paths.SmokeSettingsPath))
                    return;
                using (FileStream stream = File.OpenRead(runtime.Paths.SmokeSettingsPath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(SmokeSettings));
                    smokeSettings = (SmokeSettings?)serializer.ReadObject(stream);
                }
                if (smokeSettings != null && smokeSettings.Enabled)
                    runtime.RuntimeMonitor.Log($"Smoke settings loaded: saveSlot={smokeSettings.AutoLoadSaveSlot}, autoExit={smokeSettings.AutoExitAfterSeconds}s.");
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to read smoke settings.", ex.ToString());
            }
        }

        private bool TryAutoLoadSave(int humanSlot)
        {
            int gameIndex = Math.Max(0, humanSlot - 1);
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                if (TryAutoLoadSaveViaOfficialUi(patcher, humanSlot, gameIndex, out bool waitForOfficialUi))
                    return true;
                if (waitForOfficialUi)
                    return false;

                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? loadGame = FindMethod(dolocApi, "LoadGame", 1);
                if (loadGame == null)
                    throw new MissingMethodException("DolocAPI.LoadGame(int) was not found.");
                runtime.RuntimeMonitor.Log($"Smoke automation loading save slot {humanSlot} using direct DolocAPI.LoadGame fallback, game index {gameIndex}.");
                loadGame.Invoke(null, new object[] { gameIndex });
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-load failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AutoLoadSave", "failed", "DolocAPI.LoadGame", ex.GetType().Name + ": " + ex.Message);
                runtime.RuntimeMonitor.Log("Smoke auto-load target diagnostic:" + Environment.NewLine + (patcher?.BuildTypeResolutionReport(
                    "DolocAPI, Assembly-CSharp",
                    "DolocTown.GameData.DataPersistenceManager, Assembly-CSharp") ?? "Hook patcher was unavailable."));
                return true;
            }
        }

        private bool TryAutoOpenTitleSettingsMenu()
        {
            if (!runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase))
                return false;
            runtime.RuntimeMonitor.LogOnce("title-settings-button-visible-smoke", "DTMAPI title settings button visible on HomePageUiState.");
            runtime.SetHookStatus("UI.TitleSettingsEntry", "verified", "Unity UI Canvas", "DTMAPI Settings button is available on the Doloc Town title homepage.");
            if (!titleSettingsButtonScreenshotRequested)
            {
                titleSettingsButtonScreenshotRequested = true;
                titleSettingsButtonScreenshotAt = DateTimeOffset.Now;
                CaptureTitleSettingsButtonEvidenceScreenshot();
                return false;
            }
            if ((DateTimeOffset.Now - titleSettingsButtonScreenshotAt).TotalSeconds < 1.5)
                return false;

            bool opened = clickTitleSettingsButton?.Invoke() ?? false;
            if (!opened)
                runtime.UI.OpenConfigPage();
            runtime.RuntimeMonitor.Log("Smoke automation opened DTMAPI title settings menu.");
            runtime.SetHookStatus("Smoke.TitleSettingsMenu", "verified", "DTMAPI title settings button/menu", "Opened the title settings menu while HomePageUiState was active.");
            titleSettingsMenuEvidenceAt = DateTimeOffset.Now;
            return true;
        }

        private SmokeAttemptResult TryExerciseTitleButtonLifecycleForSmoke()
        {
            try
            {
                DateTimeOffset now = DateTimeOffset.Now;
                if (titleLifecycleStage == 0)
                {
                    if (!runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase))
                    {
                        LogTitleLifecyclePending("Waiting for startup HomePageUiState before first DTMAPI button click. context=" + runtime.UI.InputContext + ".");
                        return SmokeAttemptResult.Pending;
                    }

                    bool opened = clickTitleSettingsButton?.Invoke() ?? false;
                    if (!opened)
                        throw new InvalidOperationException("DTMAPI title settings button click returned false on startup HomePageUiState.");

                    titleLifecycleStage = 1;
                    titleLifecycleStageAt = now;
                    runtime.RuntimeMonitor.Log("Smoke title lifecycle initial settings open OK.");
                    runtime.SetHookStatus("Smoke.TitleButtonLifecycle", "pending", "DTMAPI title button", "Initial title button click opened the DTMAPI settings menu.");
                    return SmokeAttemptResult.Pending;
                }

                if (titleLifecycleStage == 1)
                {
                    if ((now - titleLifecycleStageAt).TotalSeconds < 1)
                        return SmokeAttemptResult.Pending;

                    runtime.UI.Close();
                    titleLifecycleStage = 2;
                    titleLifecycleStageAt = now;
                    runtime.RuntimeMonitor.Log("Smoke title lifecycle settings close OK.");
                    runtime.SetHookStatus("Smoke.TitleButtonLifecycle", "pending", "DTMAPI title button", "Closed the DTMAPI settings menu; waiting to load save.");
                    return SmokeAttemptResult.Pending;
                }

                if (titleLifecycleStage == 2)
                {
                    if ((now - titleLifecycleStageAt).TotalSeconds < 1)
                        return SmokeAttemptResult.Pending;
                    if (smokeSettings == null || smokeSettings.AutoLoadSaveSlot <= 0)
                        throw new InvalidOperationException("Title lifecycle smoke requires AutoLoadSaveSlot > 0.");
                    if (!runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase))
                    {
                        LogTitleLifecyclePending("Waiting for HomePageUiState before loading save in lifecycle smoke. context=" + runtime.UI.InputContext + ".");
                        return SmokeAttemptResult.Pending;
                    }

                    bool requested = TryAutoLoadSave(smokeSettings.AutoLoadSaveSlot);
                    if (!requested)
                        return SmokeAttemptResult.Pending;

                    autoLoadAttempted = true;
                    titleLifecycleStage = 3;
                    titleLifecycleStageAt = now;
                    runtime.RuntimeMonitor.Log("Smoke title lifecycle requested save slot " + smokeSettings.AutoLoadSaveSlot + " after open/close.");
                    runtime.SetHookStatus("Smoke.TitleButtonLifecycle", "pending", "GameDataUiState.OnConfirm", "Save load requested after title menu open/close; ReturnHome will wait for stable Gameplay to avoid queued loading work.");
                    return SmokeAttemptResult.Pending;
                }

                if (titleLifecycleStage == 3)
                {
                    if (saveLoadedAt == default)
                    {
                        LogTitleLifecyclePending("Waiting for SaveLoaded before returning to title.");
                        return SmokeAttemptResult.Pending;
                    }
                    if (!runtime.UI.InputContext.Equals("Gameplay", StringComparison.OrdinalIgnoreCase))
                    {
                        if ((now - saveLoadedAt).TotalSeconds > 120)
                            throw new TimeoutException("SaveLoaded did not settle into Gameplay within 120 seconds; current context=" + runtime.UI.InputContext + ".");
                        LogTitleLifecyclePending("Waiting for Gameplay after SaveLoaded before ReturnHome. context=" + runtime.UI.InputContext + ".");
                        return SmokeAttemptResult.Pending;
                    }
                    if ((now - saveLoadedAt).TotalSeconds < 5)
                        return SmokeAttemptResult.Pending;
                    if (!TryGetStaticBoolProperty(patcher?.ResolveType("DolocAPI, Assembly-CSharp")!, "IsNormalState"))
                    {
                        LogTitleLifecyclePending("Waiting for NormalGameState before ReturnHome. context=" + runtime.UI.InputContext + ".");
                        return SmokeAttemptResult.Pending;
                    }

                    TryReturnHomeForSmoke();
                    titleLifecycleStage = 4;
                    titleLifecycleStageAt = now;
                    return SmokeAttemptResult.Pending;
                }

                if (titleLifecycleStage == 4)
                {
                    if (!runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase))
                    {
                        if ((now - titleLifecycleStageAt).TotalSeconds > 45)
                            throw new TimeoutException("DolocAPI.ReturnHome did not settle on HomePageUiState within 45 seconds; current context=" + runtime.UI.InputContext + ".");
                        LogTitleLifecyclePending("Waiting for HomePageUiState after ReturnHome. context=" + runtime.UI.InputContext + ".");
                        return SmokeAttemptResult.Pending;
                    }
                    if ((now - titleLifecycleStageAt).TotalSeconds < 1.5)
                        return SmokeAttemptResult.Pending;

                    bool reopened = clickTitleSettingsButton?.Invoke() ?? false;
                    if (!reopened)
                        throw new InvalidOperationException("DTMAPI title settings button did not reopen after ReturnHome.");

                    titleLifecycleStage = 5;
                    titleLifecycleStageAt = now;
                    runtime.RuntimeMonitor.Log("Smoke title lifecycle reopened settings after return to title OK.");
                    runtime.SetHookStatus("Smoke.TitleButtonLifecycle", "pending", "DTMAPI title button", "Button remounted and reopened after save load -> ReturnHome.");
                    return SmokeAttemptResult.Pending;
                }

                if (titleLifecycleStage == 5)
                {
                    if ((now - titleLifecycleStageAt).TotalSeconds < 1.5)
                        return SmokeAttemptResult.Pending;

                    string evidenceDir = EnsureTitleLifecycleEvidenceDir();
                    string screenshotPath = Path.Combine(evidenceDir, "title-settings-after-return.png");
                    bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
                    File.AppendAllText(Path.Combine(evidenceDir, "summary.txt"),
                        "Captured=" + DateTimeOffset.Now.ToString("o") + Environment.NewLine +
                        "InputContext=" + runtime.UI.InputContext + Environment.NewLine +
                        "SaveSlot=" + (smokeSettings?.AutoLoadSaveSlot.ToString() ?? "unknown") + Environment.NewLine +
                        "ScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                        "Screenshot=" + screenshotPath + Environment.NewLine);

                    string summary = "startupOpen=true, closed=true, saveLoaded=" + (saveLoadedAt != default) + ", returnedContext=" + runtime.UI.InputContext + ", reopened=true, screenshot=" + screenshotPath;
                    runtime.RuntimeMonitor.Log("Smoke exercise TitleButtonLifecycle OK " + summary);
                    runtime.SetHookStatus("Smoke.TitleButtonLifecycle", "verified", "HomePageUiState -> SaveLoaded -> DolocAPI.ReturnHome -> DTMAPI title button", summary);
                    return SmokeAttemptResult.Succeeded;
                }

                return SmokeAttemptResult.Pending;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke title button lifecycle failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.TitleButtonLifecycle", "failed", "DTMAPI title button lifecycle", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private bool TryDirectLoadSaveForTitleLifecycleSmoke(int humanSlot)
        {
            int gameIndex = Math.Max(0, humanSlot - 1);
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? loadGame = FindMethod(dolocApi, "LoadGame", 1);
                if (loadGame == null)
                    throw new MissingMethodException("DolocAPI.LoadGame(int) was not found.");

                runtime.RuntimeMonitor.Log("Smoke title lifecycle loading save slot " + humanSlot + " through direct DolocAPI.LoadGame, game index " + gameIndex + ". Rejected official GameDataUiState path for this smoke because it can leave queued UI/load work during ReturnHome.");
                runtime.SetHookStatus("Smoke.AutoLoadSave", "pending", "DolocAPI.LoadGame", "Requested direct save load for title button lifecycle smoke.");
                loadGame.Invoke(null, new object[] { gameIndex });
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke title lifecycle direct load failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.TitleButtonLifecycle", "failed", "DolocAPI.LoadGame", ex.GetType().Name + ": " + ex.Message);
                throw;
            }
        }

        private void TryReturnHomeForSmoke()
        {
            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? returnHome = FindMethod(dolocApi, "ReturnHome", 1);
            if (returnHome == null)
                throw new MissingMethodException("DolocAPI.ReturnHome(bool) was not found.");

            runtime.RuntimeMonitor.Log("Smoke title lifecycle requesting DolocAPI.ReturnHome after save load.");
            runtime.SetHookStatus("Smoke.TitleButtonLifecycle", "pending", "DolocAPI.ReturnHome", "Requested return to title after save load.");
            returnHome.Invoke(null, new object[] { false });
        }

        private void LogTitleLifecyclePending(string message)
        {
            if ((DateTimeOffset.Now - lastTitleLifecycleReadinessLog).TotalSeconds < 5)
                return;
            lastTitleLifecycleReadinessLog = DateTimeOffset.Now;
            runtime.RuntimeMonitor.Log("Smoke title lifecycle waiting: " + message);
            runtime.SetHookStatus("Smoke.TitleButtonLifecycle", "pending", "DTMAPI title button lifecycle", message);
        }

        private string EnsureTitleLifecycleEvidenceDir()
        {
            if (!string.IsNullOrWhiteSpace(titleLifecycleEvidenceDir))
                return titleLifecycleEvidenceDir!;

            string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            titleLifecycleEvidenceDir = Path.Combine(runtime.Paths.EvidencePath, "UI-006", timestamp);
            Directory.CreateDirectory(titleLifecycleEvidenceDir);
            return titleLifecycleEvidenceDir;
        }

        private SmokeAttemptResult TryAutoOpenOfficialModUiForSmoke()
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? modUiStateType = patcher.ResolveType("DolocTown.ModUiState, Assembly-CSharp");
                if (dolocApi == null || modUiStateType == null)
                    throw new MissingMemberException("DolocAPI or ModUiState was not visible.");

                if (!runtime.UI.InputContext.Equals("HomePageUiState", StringComparison.OrdinalIgnoreCase) && GetExistingUiState(dolocApi, modUiStateType) == null)
                {
                    runtime.SetHookStatus("Smoke.OfficialModUi", "pending", "HomePageUiState", "Waiting for official title homepage before opening the official Mod UI.");
                    return SmokeAttemptResult.Pending;
                }

                object? state = GetExistingUiState(dolocApi, modUiStateType);
                if (state == null)
                {
                    MethodInfo? enterUi = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m => m.Name == "EnterUI" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
                    if (enterUi == null)
                        throw new MissingMethodException("DolocAPI.EnterUI<ModUiState>() was not found.");

                    state = enterUi.MakeGenericMethod(modUiStateType).Invoke(null, null);
                    officialModUiEvidenceAt = DateTimeOffset.Now;
                    runtime.RuntimeMonitor.Log("Smoke automation opened official Mod UI.");
                    runtime.SetHookStatus("Smoke.OfficialModUi", "pending", "DolocAPI.EnterUI<ModUiState>", "Opened official Mod UI; waiting for list render and screenshot.");
                    return SmokeAttemptResult.Pending;
                }

                if (officialModUiEvidenceAt == default)
                    officialModUiEvidenceAt = DateTimeOffset.Now;

                object? panel = ReadMember(state, "panel");
                if (panel == null)
                    throw new MissingMemberException("ModUiState.panel was not available.");

                MethodInfo? onMenuIconSelect = FindMethod(state.GetType(), "OnMenuIconSelect", 1);
                if (onMenuIconSelect != null && (DateTimeOffset.Now - officialModUiEvidenceAt).TotalSeconds < 0.5)
                {
                    onMenuIconSelect.Invoke(state, new object[] { 2 });
                    runtime.SetHookStatus("Smoke.OfficialModUi", "pending", "ModUiState.OnMenuIconSelect", "Selected official Mod UI all-mods tab for DTMAPI package evidence.");
                    return SmokeAttemptResult.Pending;
                }

                MethodInfo? refreshView = panel.GetType().GetMethod("RefreshView", BindingFlags.Public | BindingFlags.Instance);
                refreshView?.Invoke(panel, null);

                object? currentList = ReadMember(state, "currentModList");
                if (!(currentList is IEnumerable enumerable))
                    throw new MissingMemberException("ModUiState.currentModList was not enumerable.");

                var dtmapiMods = new List<object>();
                foreach (object? item in enumerable)
                {
                    if (item == null)
                        continue;
                    if (IsOfficialDtmApiPackageInfo(item))
                        dtmapiMods.Add(item);
                }
                if (dtmapiMods.Count == 0)
                    throw new InvalidOperationException("Official Mod UI list did not contain Yuuka_DTMAPI_* packages.");

                object selected = dtmapiMods[0];
                int selectedIndex = FindIndexInEnumerable(enumerable, selected);
                MethodInfo? select = panel.GetType().GetMethod("Select", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
                if (selectedIndex >= 0)
                    select?.Invoke(panel, new object[] { selectedIndex });

                if ((DateTimeOffset.Now - officialModUiEvidenceAt).TotalSeconds < 1.5)
                    return SmokeAttemptResult.Pending;

                object? currentDatas = ReadMember(panel, "currentDatas") ?? ReadMember(panel, "<currentDatas>k__BackingField");
                int visibleDtmapiRows = CountVisibleDtmApiModRows(currentDatas);
                string selectedId = ReadStringMember(selected, "id", string.Empty);
                string title = ReadStringMember(selected, "title", selectedId);
                string description = ReadStringMember(selected, "description", string.Empty);
                string iconPath = ReadStringMember(selected, "iconPath", string.Empty);
                string previewPath = ReadStringMember(selected, "previewPath", string.Empty);
                bool iconLoaded = ReadMember(selected, "icon") != null;
                bool iconFile = !string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath);
                bool previewFile = !string.IsNullOrWhiteSpace(previewPath) && File.Exists(previewPath);
                if (!iconLoaded || !iconFile || !previewFile)
                    throw new InvalidOperationException("Official Mod UI asset check failed for " + selectedId + ": iconLoaded=" + iconLoaded + ", iconFile=" + iconFile + ", previewFile=" + previewFile + ".");

                string evidenceDir = EnsureOfficialModUiEvidenceDir();
                string screenshotPath = Path.Combine(evidenceDir, "official-mod-ui.png");
                bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
                string summary = "dtmapiMods=" + dtmapiMods.Count +
                    ", visibleRows=" + visibleDtmapiRows +
                    ", selected=" + selectedId +
                    ", title=" + title +
                    ", description=" + description +
                    ", iconLoaded=" + iconLoaded +
                    ", iconFile=" + iconFile +
                    ", previewFile=" + previewFile +
                    ", screenshot=" + screenshotPath;
                File.AppendAllText(Path.Combine(evidenceDir, "summary.txt"),
                    "Captured=" + DateTimeOffset.Now.ToString("o") + Environment.NewLine +
                    summary + Environment.NewLine +
                    "ScreenshotRequested=" + screenshotRequested + Environment.NewLine);

                runtime.RuntimeMonitor.Log("Official Mod UI evidence OK " + summary);
                runtime.SetHookStatus("Smoke.OfficialModUi", "verified", "DolocAPI.EnterUI<ModUiState> + ModPanel", summary);
                runtime.SetHookStatus("Smoke.OfficialModUiScreenshot", screenshotRequested ? "verified" : "pending", "UnityEngine.ScreenCapture.CaptureScreenshot", "Official Mod UI screenshot=" + (screenshotRequested ? screenshotPath : "unavailable") + ".");
                return SmokeAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke official Mod UI evidence failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.OfficialModUi", "failed", "DolocTown.ModUiState", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private string EnsureOfficialModUiEvidenceDir()
        {
            if (!string.IsNullOrWhiteSpace(officialModUiEvidenceDir))
                return officialModUiEvidenceDir!;

            string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            officialModUiEvidenceDir = Path.Combine(runtime.Paths.EvidencePath, "OFFICIAL-001", timestamp);
            Directory.CreateDirectory(officialModUiEvidenceDir);
            return officialModUiEvidenceDir;
        }

        private static int FindIndexInEnumerable(IEnumerable enumerable, object target)
        {
            int index = 0;
            foreach (object? item in enumerable)
            {
                if (ReferenceEquals(item, target))
                    return index;
                index++;
            }
            return -1;
        }

        private static bool IsOfficialDtmApiPackageInfo(object item)
        {
            string id = ReadStringMember(item, "id", string.Empty);
            string title = ReadStringMember(item, "title", string.Empty);
            string description = ReadStringMember(item, "description", string.Empty);
            string rootPath = ReadStringMember(item, "rootPath", string.Empty);
            if (id.IndexOf("Yuuka_DTMAPI_", StringComparison.OrdinalIgnoreCase) >= 0 ||
                title.IndexOf("DTMAPI", StringComparison.OrdinalIgnoreCase) >= 0 ||
                description.IndexOf("DTMAPI", StringComparison.OrdinalIgnoreCase) >= 0 ||
                rootPath.IndexOf("Yuuka_DTMAPI_", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            object? tags = ReadMember(item, "tags");
            if (tags is IEnumerable tagValues)
            {
                foreach (object? tag in tagValues)
                {
                    if ((tag?.ToString() ?? string.Empty).IndexOf("DTMAPI", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
            return false;
        }

        private static int CountVisibleDtmApiModRows(object? currentDatas)
        {
            if (!(currentDatas is Array array))
                return 0;

            int count = 0;
            for (int i = 0; i < array.Length; i++)
            {
                object? data = array.GetValue(i);
                if (data == null)
                    continue;
                string name = ReadStringMember(data, "name", string.Empty);
                if (name.IndexOf("DTMAPI", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("动作加速", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("自动钓鱼", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("一键完成", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("繁殖助手", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("牧畜进度", StringComparison.OrdinalIgnoreCase) >= 0)
                    count++;
            }
            return count;
        }

        private bool CaptureTitleSettingsButtonEvidenceScreenshot()
        {
            try
            {
                string evidenceDir = EnsureTitleSettingsEvidenceDir();
                string screenshotPath = Path.Combine(evidenceDir, "title-settings-button.png");
                bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
                string summaryPath = Path.Combine(evidenceDir, "summary.txt");
                File.AppendAllText(summaryPath,
                    "ButtonCaptured=" + DateTimeOffset.Now.ToString("o") + Environment.NewLine +
                    "ButtonInputContext=" + runtime.UI.InputContext + Environment.NewLine +
                    "ButtonScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                    "ButtonScreenshot=" + screenshotPath + Environment.NewLine);

                runtime.RuntimeMonitor.Log("Title settings button screenshot " + (screenshotRequested ? "OK" : "unavailable") + " screenshot=" + screenshotPath + ".");
                runtime.SetHookStatus("Smoke.TitleSettingsButtonScreenshot", screenshotRequested ? "verified" : "pending", "UnityEngine.ScreenCapture.CaptureScreenshot", "Title settings button screenshot=" + (screenshotRequested ? screenshotPath : "unavailable") + ".");
                return screenshotRequested;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to capture title settings button screenshot.", ex.ToString());
                runtime.SetHookStatus("Smoke.TitleSettingsButtonScreenshot", "failed", "UnityEngine.ScreenCapture.CaptureScreenshot", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private bool CaptureTitleSettingsMenuEvidenceScreenshot()
        {
            try
            {
                string evidenceDir = EnsureTitleSettingsEvidenceDir();

                string screenshotPath = Path.Combine(evidenceDir, "title-settings-menu.png");
                bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
                string summaryPath = Path.Combine(evidenceDir, "summary.txt");
                File.AppendAllText(summaryPath,
                    "MenuCaptured=" + DateTimeOffset.Now.ToString("o") + Environment.NewLine +
                    "MenuInputContext=" + runtime.UI.InputContext + Environment.NewLine +
                    "MenuCurrentPage=" + runtime.UI.CurrentPage + Environment.NewLine +
                    "MenuScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                    "MenuScreenshot=" + screenshotPath + Environment.NewLine);

                runtime.RuntimeMonitor.Log("Title settings menu screenshot " + (screenshotRequested ? "OK" : "unavailable") + " screenshot=" + screenshotPath + ".");
                runtime.SetHookStatus("Smoke.TitleSettingsMenuScreenshot", screenshotRequested ? "verified" : "pending", "UnityEngine.ScreenCapture.CaptureScreenshot", "Title settings menu screenshot=" + (screenshotRequested ? screenshotPath : "unavailable") + ".");
                return screenshotRequested;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to capture title settings menu screenshot.", ex.ToString());
                runtime.SetHookStatus("Smoke.TitleSettingsMenuScreenshot", "failed", "UnityEngine.ScreenCapture.CaptureScreenshot", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private void UpdateTitleSettingsConfigEvidenceScreenshots()
        {
            DateTimeOffset now = DateTimeOffset.Now;
            if (titleSettingsConfigScreenshotStage == 0)
            {
                runtime.UI.OpenConfigPage("Yuuka.DTMAPI.ActionSpeed");
                titleSettingsConfigScreenshotStage = 1;
                titleSettingsConfigScreenshotAt = now;
                return;
            }

            if (titleSettingsConfigScreenshotStage == 1)
            {
                if ((now - titleSettingsConfigScreenshotAt).TotalSeconds < 0.75)
                    return;
                CaptureTitleSettingsConfigPageEvidenceScreenshot("Yuuka.DTMAPI.ActionSpeed", "action-speed");
                titleSettingsConfigScreenshotStage = 2;
                titleSettingsConfigScreenshotAt = now;
                return;
            }

            if (titleSettingsConfigScreenshotStage == 2)
            {
                if ((now - titleSettingsConfigScreenshotAt).TotalSeconds < 0.75)
                    return;
                runtime.UI.OpenConfigPage("Yuuka.DTMAPI.AutoFishing");
                titleSettingsConfigScreenshotStage = 3;
                titleSettingsConfigScreenshotAt = now;
                return;
            }

            if (titleSettingsConfigScreenshotStage == 3)
            {
                if ((now - titleSettingsConfigScreenshotAt).TotalSeconds < 0.75)
                    return;
                CaptureTitleSettingsConfigPageEvidenceScreenshot("Yuuka.DTMAPI.AutoFishing", "auto-fishing");
                titleSettingsConfigScreenshotStage = 4;
                titleSettingsConfigScreenshotAt = now;
                return;
            }

            if (titleSettingsConfigScreenshotStage == 4)
            {
                if ((now - titleSettingsConfigScreenshotAt).TotalSeconds < 0.75)
                    return;
                runtime.UI.OpenConfigPage("Yuuka.DTMAPI.AnimalHusbandryProgress");
                titleSettingsConfigScreenshotStage = 5;
                titleSettingsConfigScreenshotAt = now;
                return;
            }

            if (titleSettingsConfigScreenshotStage == 5)
            {
                if ((now - titleSettingsConfigScreenshotAt).TotalSeconds < 0.75)
                    return;
                CaptureTitleSettingsConfigPageEvidenceScreenshot("Yuuka.DTMAPI.AnimalHusbandryProgress", "animal-husbandry-progress");
                if (TryStageAnimalCustomColorForTitleSmoke())
                    runtime.UI.OpenConfigPage("DTMAPI.SecondMotorMod");
                titleSettingsConfigScreenshotStage = 6;
                titleSettingsConfigScreenshotAt = now;
                return;
            }

            if (titleSettingsConfigScreenshotStage == 6)
            {
                if ((now - titleSettingsConfigScreenshotAt).TotalSeconds < 0.25)
                    return;
                runtime.UI.OpenConfigPage("Yuuka.DTMAPI.AnimalHusbandryProgress");
                titleSettingsConfigScreenshotStage = 7;
                titleSettingsConfigScreenshotAt = now;
                return;
            }

            if (titleSettingsConfigScreenshotStage == 7)
            {
                if ((now - titleSettingsConfigScreenshotAt).TotalSeconds < 0.75)
                    return;
                CaptureTitleSettingsConfigPageEvidenceScreenshot("Yuuka.DTMAPI.AnimalHusbandryProgress", "animal-husbandry-progress-custom");
                titleSettingsConfigScreenshotStage = 8;
                titleSettingsConfigScreenshotAt = now;
                return;
            }

            if (titleSettingsConfigScreenshotStage == 8)
            {
                if ((now - titleSettingsConfigScreenshotAt).TotalSeconds < 0.75)
                    return;
                runtime.UI.OpenConfigPage("DTMAPI.SecondMotorMod");
                titleSettingsConfigScreenshotStage = 9;
                titleSettingsConfigScreenshotAt = now;
                return;
            }

            if (titleSettingsConfigScreenshotStage == 9)
            {
                if ((now - titleSettingsConfigScreenshotAt).TotalSeconds < 0.75)
                    return;
                CaptureTitleSettingsConfigPageEvidenceScreenshot("DTMAPI.SecondMotorMod", "second-motor");
                titleSettingsConfigScreenshotStage = 10;
                titleSettingsConfigScreenshotAt = now;
                return;
            }

            if (titleSettingsConfigScreenshotStage == 10)
            {
                if ((now - titleSettingsConfigScreenshotAt).TotalSeconds < 0.75)
                    return;
                runtime.UI.OpenConfigPage("DTMAPI.MineMod");
                titleSettingsConfigScreenshotStage = 11;
                titleSettingsConfigScreenshotAt = now;
                return;
            }

            if (titleSettingsConfigScreenshotStage == 11)
            {
                if ((now - titleSettingsConfigScreenshotAt).TotalSeconds < 0.75)
                    return;
                CaptureTitleSettingsConfigPageEvidenceScreenshot("DTMAPI.MineMod", "mine");
                titleSettingsConfigScreenshotStage = 12;
                titleSettingsConfigScreenshotAt = now;
                return;
            }

            if (titleSettingsConfigScreenshotStage == 12)
            {
                if ((now - titleSettingsConfigScreenshotAt).TotalSeconds < 0.75)
                    return;
                runtime.UI.OpenConfigPage("DTMAPI.MoreEquipmentSlotsMod");
                titleSettingsConfigScreenshotStage = 13;
                titleSettingsConfigScreenshotAt = now;
                return;
            }

            if (titleSettingsConfigScreenshotStage == 13)
            {
                if ((now - titleSettingsConfigScreenshotAt).TotalSeconds < 0.75)
                    return;
                CaptureTitleSettingsConfigPageEvidenceScreenshot("DTMAPI.MoreEquipmentSlotsMod", "more-equipment-slots");
                titleSettingsConfigScreenshotStage = 14;
                titleSettingsConfigScreenshotAt = now;
            }
        }

        private bool TryStageAnimalCustomColorForTitleSmoke()
        {
            try
            {
                IConfigMenuPage? page = runtime.CreateSnapshot().ConfigPages.FirstOrDefault(p => p.Manifest.UniqueID.Equals("Yuuka.DTMAPI.AnimalHusbandryProgress", StringComparison.OrdinalIgnoreCase));
                if (page == null || page.IsLocked)
                    return false;

                page.BeginEditing();
                SetConfigPendingValue(page, "ColorPreset", "Custom", "颜色预设", "Color preset");
                runtime.RuntimeMonitor.Log("Smoke staged AnimalHusbandryProgress Custom color preset for pending-preview screenshot.");
                runtime.SetHookStatus("Smoke.TitleSettingsConfigPageScreenshot.animal-husbandry-progress-custom", "pending", "DTMAPI ConfigMenu pending preview", "Staged Custom color preset without saving so the custom hex input should be visible in the next screenshot.");
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to stage AnimalHusbandryProgress Custom color screenshot state.", ex.ToString());
                runtime.SetHookStatus("Smoke.TitleSettingsConfigPageScreenshot.animal-husbandry-progress-custom", "failed", "DTMAPI ConfigMenu pending preview", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private bool CaptureTitleSettingsConfigPageEvidenceScreenshot(string uniqueId, string slug)
        {
            try
            {
                string evidenceDir = EnsureTitleSettingsEvidenceDir();
                string screenshotPath = Path.Combine(evidenceDir, "title-settings-config-" + slug + ".png");
                bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
                string summaryPath = Path.Combine(evidenceDir, "summary.txt");
                File.AppendAllText(summaryPath,
                    "ConfigPageCaptured=" + DateTimeOffset.Now.ToString("o") + Environment.NewLine +
                    "ConfigPageUniqueId=" + uniqueId + Environment.NewLine +
                    "ConfigPageScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                    "ConfigPageScreenshot=" + screenshotPath + Environment.NewLine);

                runtime.RuntimeMonitor.Log("Title settings config page screenshot " + (screenshotRequested ? "OK" : "unavailable") + " uniqueId=" + uniqueId + " screenshot=" + screenshotPath + ".");
                runtime.SetHookStatus("Smoke.TitleSettingsConfigPageScreenshot." + slug, screenshotRequested ? "verified" : "pending", "UnityEngine.ScreenCapture.CaptureScreenshot", uniqueId + " screenshot=" + (screenshotRequested ? screenshotPath : "unavailable") + ".");
                return screenshotRequested;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to capture title settings config page screenshot.", ex.ToString());
                runtime.SetHookStatus("Smoke.TitleSettingsConfigPageScreenshot." + slug, "failed", "UnityEngine.ScreenCapture.CaptureScreenshot", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private string EnsureTitleSettingsEvidenceDir()
        {
            if (!string.IsNullOrWhiteSpace(titleSettingsEvidenceDir))
                return titleSettingsEvidenceDir!;

            string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            titleSettingsEvidenceDir = Path.Combine(runtime.Paths.EvidencePath, "UI-004", timestamp);
            Directory.CreateDirectory(titleSettingsEvidenceDir);
            return titleSettingsEvidenceDir;
        }

        private static bool TryCaptureScreenshot(string path)
        {
            try
            {
                Type? screenCapture = Type.GetType("UnityEngine.ScreenCapture, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.ScreenCapture, UnityEngine");
                MethodInfo? captureTexture = screenCapture?.GetMethod("CaptureScreenshotAsTexture", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                object? texture = captureTexture?.Invoke(null, null);
                if (texture != null)
                {
                    try
                    {
                        MethodInfo? encodeToPng = texture.GetType().GetMethod("EncodeToPNG", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                        if (encodeToPng?.Invoke(texture, null) is byte[] bytes && bytes.Length > 0)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                            File.WriteAllBytes(path, bytes);
                            return true;
                        }
                    }
                    finally
                    {
                        TryDestroyUnityObject(texture);
                    }
                }

                if (TryCaptureScreenshotWithReadPixels(path))
                    return true;

                MethodInfo? capture = screenCapture?.GetMethod("CaptureScreenshot", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (capture == null)
                    return false;
                capture.Invoke(null, new object[] { path });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryCaptureScreenshotWithReadPixels(string path)
        {
            object? texture = null;
            try
            {
                Type? screenType = Type.GetType("UnityEngine.Screen, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Screen, UnityEngine");
                Type? texture2DType = Type.GetType("UnityEngine.Texture2D, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Texture2D, UnityEngine");
                Type? textureFormatType = Type.GetType("UnityEngine.TextureFormat, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.TextureFormat, UnityEngine");
                Type? rectType = Type.GetType("UnityEngine.Rect, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Rect, UnityEngine");
                if (screenType == null || texture2DType == null || textureFormatType == null || rectType == null)
                    return false;

                int width = Math.Max(1, Convert.ToInt32(screenType.GetProperty("width", BindingFlags.Public | BindingFlags.Static)?.GetValue(null), System.Globalization.CultureInfo.InvariantCulture));
                int height = Math.Max(1, Convert.ToInt32(screenType.GetProperty("height", BindingFlags.Public | BindingFlags.Static)?.GetValue(null), System.Globalization.CultureInfo.InvariantCulture));
                object format = Enum.Parse(textureFormatType, "RGB24");
                texture = Activator.CreateInstance(texture2DType, width, height, format, false);
                if (texture == null)
                    return false;

                object rect = Activator.CreateInstance(rectType, 0f, 0f, (float)width, (float)height)!;
                MethodInfo? readPixels = texture2DType.GetMethod("ReadPixels", BindingFlags.Public | BindingFlags.Instance, null, new[] { rectType, typeof(int), typeof(int) }, null);
                MethodInfo? apply = texture2DType.GetMethod("Apply", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                MethodInfo? encodeToPng = texture2DType.GetMethod("EncodeToPNG", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (readPixels == null || apply == null || encodeToPng == null)
                    return false;

                readPixels.Invoke(texture, new object[] { rect, 0, 0 });
                apply.Invoke(texture, null);
                if (encodeToPng.Invoke(texture, null) is byte[] bytes && bytes.Length > 0)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                    File.WriteAllBytes(path, bytes);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (texture != null)
                    TryDestroyUnityObject(texture);
            }
        }

        private static void TryDestroyUnityObject(object instance)
        {
            try
            {
                Type? objectType = Type.GetType("UnityEngine.Object, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Object, UnityEngine");
                MethodInfo? destroy = objectType?.GetMethod("Destroy", BindingFlags.Public | BindingFlags.Static, null, new[] { objectType }, null);
                destroy?.Invoke(null, new[] { instance });
            }
            catch
            {
            }
        }

        private string EnsureNewContentEvidenceDir()
        {
            if (!string.IsNullOrWhiteSpace(newContentEvidenceDir))
                return newContentEvidenceDir!;

            string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            newContentEvidenceDir = Path.Combine(runtime.Paths.EvidencePath, "NEWCONTENT-025", timestamp);
            Directory.CreateDirectory(newContentEvidenceDir);
            return newContentEvidenceDir;
        }

        private string CaptureMinePlacementEvidenceForSmoke(object mine, string createSummary)
        {
            try
            {
                string evidenceDir = EnsureNewContentEvidenceDir();
                string screenshotPath = Path.Combine(evidenceDir, "mine-placed-dtmapi-mine.png");
                bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
                string equipmentSummary = DescribeEquipmentForSmoke(mine);
                string summary = "playerItem=dtmapi_mine ItemEquipment, placed=" + equipmentSummary + ", create={" + createSummary + "}, screenshot=" + (screenshotRequested ? screenshotPath : "unavailable");
                File.WriteAllText(Path.Combine(evidenceDir, "mine-placement-summary.txt"),
                    "Captured=" + DateTimeOffset.Now.ToString("o", System.Globalization.CultureInfo.InvariantCulture) + Environment.NewLine +
                    "ScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                    "Screenshot=" + screenshotPath + Environment.NewLine +
                    "Create=" + createSummary + Environment.NewLine +
                    "Equipment=" + equipmentSummary + Environment.NewLine);
                runtime.RuntimeMonitor.Log("Mine placement evidence " + (screenshotRequested ? "OK" : "unavailable") + " " + summary + ".");
                runtime.SetHookStatus("Smoke.NewContentMinePlacement", screenshotRequested ? "verified" : "pending", "ItemEquipment dtmapi_mine + IEquipmentHost.CreateEquipment + UnityEngine.ScreenCapture", summary);
                return summary;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Mine placement evidence capture failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.NewContentMinePlacement", "failed", "ItemEquipment dtmapi_mine + IEquipmentHost.CreateEquipment", ex.GetType().Name + ": " + ex.Message);
                return "failed:" + ex.GetType().Name + ":" + ex.Message;
            }
        }

        private void TryQuitApplication(string reason)
        {
            try
            {
                runtime.RuntimeMonitor.Log("Smoke automation requesting game quit: " + reason);
                Type? application = Type.GetType("UnityEngine.Application, UnityEngine.CoreModule") ?? Type.GetType("UnityEngine.Application, UnityEngine");
                MethodInfo? quit = application?.GetMethod("Quit", Type.EmptyTypes);
                if (quit == null)
                    throw new MissingMethodException("UnityEngine.Application.Quit() was not found.");
                quit.Invoke(null, null);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-exit failed.", ex.ToString());
            }
        }

        private void TryAutoSave(int gameIndex)
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? saveGame = FindMethod(dolocApi, "SaveGame", 1);
                if (saveGame == null)
                    throw new MissingMethodException("DolocAPI.SaveGame(int) was not found.");

                runtime.RuntimeMonitor.Log("Smoke automation saving game slot/index " + gameIndex + " through DolocAPI.SaveGame.");
                runtime.SetHookStatus("Smoke.AutoSave", "pending", "DolocAPI.SaveGame", "Requested save for slot/index " + gameIndex + ".");
                object? result = saveGame.Invoke(null, new object[] { gameIndex });
                if (result is bool saved && !saved)
                    runtime.SetHookStatus("Smoke.AutoSave", "failed", "DolocAPI.SaveGame", "SaveGame returned false for slot/index " + gameIndex + ".");
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-save failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AutoSave", "failed", "DolocAPI.SaveGame", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void TryExerciseInstantSaveForSmoke()
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? saveGame = FindMethod(dolocApi, "SaveGame", 1);
                if (dolocApi == null || saveGame == null)
                    throw new MissingMethodException("DolocAPI.SaveGame(int) was not found.");

                InstantSaveSnapshot before = CaptureInstantSaveSnapshot(dolocApi);
                int gameIndex = before.ArchiveIndex ?? pendingAutoLoadGameIndex ?? 0;

                runtime.RuntimeMonitor.Log("DTMAPI debug instant save requested slot/index=" + gameIndex + " before=" + before.ToLogString() + ".");
                runtime.SetHookStatus("Smoke.InstantSave", "pending", "DolocAPI.SaveGame", "Requested native save from current scene: " + before.ToLogString());
                object? saveResult = saveGame.Invoke(null, new object[] { gameIndex });
                if (saveResult is bool saved && !saved)
                {
                    runtime.SetHookStatus("Smoke.InstantSave", "failed", "DolocAPI.SaveGame", "SaveGame returned false for slot/index " + gameIndex + ".");
                    return;
                }

                InstantSaveSnapshot afterSave = CaptureInstantSaveSnapshot(dolocApi);
                bool sameRoom = before.RoomId.Equals(afterSave.RoomId, StringComparison.OrdinalIgnoreCase);
                double distance = before.DistanceTo(afterSave);
                string summary =
                    "slot/index=" + gameIndex +
                    ", before={" + before.ToLogString() + "}" +
                    ", afterSave={" + afterSave.ToLogString() + "}" +
                    ", sameRoom=" + sameRoom +
                    ", distance=" + (double.IsNaN(distance) ? "unknown" : distance.ToString("0.###", CultureInfo.InvariantCulture)) +
                    ", reloadDisabled=True";

                runtime.RuntimeMonitor.Log("Smoke exercise InstantSave OK " + summary);
                runtime.SetHookStatus("Smoke.InstantSave", "verified", "DolocAPI.SaveGame", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke instant-save exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.InstantSave", "failed", "DolocAPI.SaveGame", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                if (smokeSettings?.AutoExitAfterSaveLoaded == true && !autoExitAttempted)
                {
                    if (IsDebugSmokeRequested())
                    {
                        TryQuitAfterDebugSmoke();
                    }
                    else
                    {
                        autoExitAttempted = true;
                        TryQuitApplication("smoke instant-save evidence captured");
                    }
                }
            }
        }

        private void TryExerciseDebugInventoryForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                InventoryDebugItem? item = SelectInventorySmokeItem(experimentalApi);
                if (item == null)
                    throw new InvalidOperationException("No spawnable official item was found in TbItem.");

                InventoryGiveResult result = experimentalApi.GiveItem(CreateSmokeManifest(), item.Id, 1);
                if (!result.Success)
                    throw new InvalidOperationException(result.FailureReason + ": " + result.Message);

                string modSummary = "modItem=not-found";
                InventoryDebugItem? modItem = SelectModInventorySmokeItem(experimentalApi);
                if (modItem != null)
                {
                    InventoryGiveResult modResult = experimentalApi.GiveItem(CreateSmokeManifest(), modItem.Id, 1);
                    if (!modResult.Success)
                        throw new InvalidOperationException("Mod item give failed " + modItem.Id + ": " + modResult.FailureReason + ": " + modResult.Message);

                    modSummary = "modItem=" + modResult.ItemId +
                        ", modDisplay=" + FirstNonEmpty(modResult.DisplayName, modItem.DisplayName, modItem.Id) +
                        ", sourceKind=" + modItem.SourceKind +
                        ", sourceTitle=" + modItem.SourceModTitle +
                        ", sourceId=" + modItem.SourceId +
                        ", workshopId=" + (modItem.WorkshopId.HasValue ? modItem.WorkshopId.Value.ToString() : "none") +
                        ", runtimeLoaded=" + modItem.RuntimeLoaded +
                        ", before=" + modResult.BeforeCount +
                        ", after=" + modResult.AfterCount +
                        ", given=" + modResult.GivenCount +
                        (modItem.SourceKind.Equals("Workshop", StringComparison.OrdinalIgnoreCase) ? ", workshopRuntimeItem=verified" : ", workshopRuntimeItem=not-found");
                }

                string summary = "item=" + result.ItemId +
                    ", display=" + FirstNonEmpty(result.DisplayName, item.DisplayName, item.Id) +
                    ", before=" + result.BeforeCount +
                    ", after=" + result.AfterCount +
                    ", given=" + result.GivenCount +
                    ", " + modSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise DebugInventory OK " + summary);
                runtime.SetHookStatus("Smoke.DebugInventory", "verified", "IInventoryDebugApi -> DolocAPI.TryPlaceInBackpack", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug inventory exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugInventory", "failed", "IInventoryDebugApi", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static InventoryDebugItem? SelectInventorySmokeItem(IInventoryDebugApi api)
        {
            string[] preferredIds = { "wood", "stone", "roughage_feed", "seed_endyam" };
            foreach (string preferredId in preferredIds)
            {
                InventoryDebugPage page = api.GetItems(new InventoryDebugQuery { SearchText = preferredId, PageSize = 50 });
                InventoryDebugItem? exact = page.Items.FirstOrDefault(i => i.CanSpawn && i.Id.Equals(preferredId, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                    return exact;

                InventoryDebugItem? partial = page.Items.FirstOrDefault(i => i.CanSpawn && i.Id.IndexOf(preferredId, StringComparison.OrdinalIgnoreCase) >= 0);
                if (partial != null)
                    return partial;
            }

            return api.GetItems(new InventoryDebugQuery { PageSize = 50 }).Items.FirstOrDefault(i => i.CanSpawn);
        }

        private static InventoryDebugItem? SelectModInventorySmokeItem(IInventoryDebugApi api)
        {
            InventoryDebugPage page = api.GetItems(new InventoryDebugQuery { ModItemsOnly = true, IncludeUnavailable = true, PageSize = 200 });
            InventoryDebugItem? preferredButter = page.Items
                .Where(i => i.CanGive && i.RuntimeLoaded && i.IsModItem)
                .Where(i => i.SourceKind.Equals("Workshop", StringComparison.OrdinalIgnoreCase))
                .Where(i => i.WorkshopId == 3722791728UL || i.SourceId.Equals("Workshop.3722791728", StringComparison.OrdinalIgnoreCase))
                .Where(i => i.Id.Equals("mod_butter", StringComparison.OrdinalIgnoreCase) || ContainsIgnoreCase(i.DisplayName, "黄油") || ContainsIgnoreCase(i.SearchText, "butter"))
                .OrderBy(i => i.Id.Equals("mod_butter", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(i => i.RuntimeOrder)
                .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (preferredButter != null)
                return preferredButter;

            InventoryDebugItem? workshop = page.Items
                .Where(i => i.CanGive && i.RuntimeLoaded && i.IsModItem)
                .Where(i => i.SourceKind.Equals("Workshop", StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => i.RuntimeOrder)
                .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (workshop != null)
                return workshop;

            return page.Items
                .Where(i => i.CanGive && i.RuntimeLoaded && i.IsModItem)
                .OrderBy(i => i.RuntimeOrder)
                .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        private void TryExerciseDebugWeatherForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                WeatherDebugState state = experimentalApi.GetState();
                IReadOnlyList<WeatherDebugOption> options = experimentalApi.GetAvailableWeathers();
                WeatherDebugOption? option = options.FirstOrDefault(w => !w.IsCurrent)
                    ?? options.FirstOrDefault();
                if (option == null)
                    throw new InvalidOperationException("No native weather options were available.");

                WeatherSetResult result = experimentalApi.SetWeather(CreateSmokeManifest(), option.Id, patchCurrentPeriod: true);
                if (!result.Success)
                    throw new InvalidOperationException(result.FailureReason + ": " + result.Message);

                string summary = "options=" + options.Count +
                    ", season=" + state.SeasonName +
                    ", date=" + state.Year + "-" + state.Month + "-" + state.Day + " " + state.Hour +
                    ", before=" + result.BeforeWeatherId +
                    ", after=" + result.AfterWeatherId +
                    ", display=" + FirstNonEmpty(result.DisplayName, option.DisplayName, option.Id) +
                    ", currentDayForecast=" + option.IsCurrentDayForecast;
                runtime.RuntimeMonitor.Log("Smoke exercise DebugWeather OK " + summary);
                runtime.SetHookStatus("Smoke.DebugWeather", "verified", "IWeatherDebugApi -> ArchiveDataHandle.SetWeather/PatchWeather", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug weather exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugWeather", "failed", "IWeatherDebugApi", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void TryExerciseDebugTimeForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                var steps = new List<string>();
                for (int i = 0; i < 3; i++)
                {
                    TimeSkipResult result = experimentalApi.SkipToNextWeatherPeriod(CreateSmokeManifest());
                    if (!result.Success)
                        throw new InvalidOperationException(result.FailureReason + ": " + result.Message);

                    steps.Add("step" + (i + 1) +
                        "{targetHour=" + result.TargetHour +
                        ", advancedMinutes=" + result.AdvancedGameMinutes +
                        ", advancedSeconds=" + result.AdvancedSeconds +
                        ", before=" + FormatTimeSnapshot(result.Before) +
                        ", after=" + FormatTimeSnapshot(result.After) +
                        "}");
                    runtime.RuntimeMonitor.Log("Smoke exercise DebugTime step OK " + steps[steps.Count - 1]);
                }

                string summary = "transitions=" + string.Join(" -> ", steps.ToArray());
                runtime.RuntimeMonitor.Log("Smoke exercise DebugTime OK " + summary);
                runtime.SetHookStatus("Smoke.DebugTime", "verified", "ITimeDebugApi -> ArchiveDataHandle.PassTimeNoControl", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug time exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugTime", "failed", "ITimeDebugApi", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void TryExerciseDebugMovementForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                double[] levels = { 1, 2, 3, 4 };
                var samples = new List<string>();
                foreach (double level in levels)
                {
                    MovementSpeedResult result = experimentalApi.SetSpeedMultiplier(CreateSmokeManifest(), level);
                    if (!result.Success)
                        throw new InvalidOperationException(result.FailureReason + ": " + result.Message);
                    samples.Add(level.ToString("0.#") + "x:" + FormatSmokeDouble(result.After.MoveSpeed));
                }
                MovementSpeedResult reset = experimentalApi.ResetSpeed(CreateSmokeManifest(), "smoke-restore");
                if (!reset.Success || !reset.After.IsDefault)
                    throw new InvalidOperationException("Reset failed: " + reset.FailureReason + " " + reset.Message);

                string summary = "levels=" + string.Join(",", samples.ToArray()) + ", restored=" + reset.After.IsDefault + ", finalSpeed=" + FormatSmokeDouble(reset.After.MoveSpeed);
                runtime.RuntimeMonitor.Log("Smoke exercise DebugMovement OK " + summary);
                runtime.SetHookStatus("Smoke.DebugMovement", "verified", "IMovementDebugApi -> MotionAbility.SetMoveScaler", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug movement exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugMovement", "failed", "IMovementDebugApi", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void TryExerciseAdvancedDebugForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                ManifestModel owner = CreateDebugConsoleSmokeManifest();
                var required = new List<string>();
                var optional = new List<string>();

                TimeSkipResult day = experimentalApi.AdvanceTime(owner, AdvancedTimeAdvanceKind.Day, 1);
                if (!day.Success)
                    throw new InvalidOperationException("Advance day failed: " + day.FailureReason + ": " + day.Message);
                required.Add("day{seconds=" + day.AdvancedSeconds + ", before=" + FormatTimeSnapshot(day.Before) + ", after=" + FormatTimeSnapshot(day.After) + "}");

                TimeScaleDebugResult scale = experimentalApi.SetTimeScale(owner, 4);
                if (!scale.Success)
                    throw new InvalidOperationException("Set time scale failed: " + scale.FailureReason + ": " + scale.Message);
                TimeScaleDebugResult resetScale = experimentalApi.ResetTimeScale(owner, "advanced-smoke");
                if (!resetScale.Success)
                    throw new InvalidOperationException("Reset time scale failed: " + resetScale.FailureReason + ": " + resetScale.Message);
                required.Add("scale{set=" + FormatSmokeDouble(scale.AfterMultiplier) + ", reset=" + FormatSmokeDouble(resetScale.AfterMultiplier) + "}");

                DebugValueResult money = experimentalApi.AddMoney(owner, 1);
                if (!money.Success)
                    throw new InvalidOperationException("Add money failed: " + money.FailureReason + ": " + money.Message);
                required.Add("money{" + money.BeforeValue + "->" + money.AfterValue + "}");

                TechPointDebugOption? techOption = experimentalApi.GetTechPointOptions().FirstOrDefault();
                if (techOption == null)
                    throw new InvalidOperationException("No tech point options were available.");
                DebugValueResult tech = experimentalApi.AddTechPoint(owner, techOption.Id, 1);
                if (!tech.Success)
                    throw new InvalidOperationException("Add tech point failed: " + tech.FailureReason + ": " + tech.Message);
                required.Add("tech{" + techOption.Id + ":" + tech.BeforeValue + "->" + tech.AfterValue + "}");

                CreativeModeResult creativeOn = experimentalApi.SetCreativeMode(owner, true);
                CreativeModeResult? creativeOff = null;
                string creativeSmoke;
                try
                {
                    if (!creativeOn.Success || !creativeOn.After.RuntimeHooksInstalled)
                        throw new InvalidOperationException("Creative enable failed: " + FirstNonEmpty(creativeOn.FailureReason, creativeOn.Message));
                    creativeSmoke = experimentalApi.VerifyAdvancedCreativeHooksForSmoke();
                }
                finally
                {
                    creativeOff = experimentalApi.SetCreativeMode(owner, false);
                }
                if (creativeOff == null || !creativeOff.Success)
                    throw new InvalidOperationException("Creative disable failed: " + (creativeOff == null ? "no result" : FirstNonEmpty(creativeOff.FailureReason, creativeOff.Message)));
                required.Add("creative{hooks=" + creativeOn.After.RuntimeHooksInstalled + ", generatorAvailable=" + creativeOn.After.GeneratorRuntimeAvailable + ", smoke=" + creativeSmoke + "}");

                DebugCommandResult unlock = experimentalApi.UnlockAllTechTrees(owner);
                optional.Add("unlockTech{success=" + unlock.Success + ", affected=" + unlock.AffectedCount + ", reason=" + FirstNonEmpty(unlock.FailureReason, "none") + "}");

                CropMaturityResult crops = experimentalApi.MatureAllCrops(owner);
                optional.Add("crops{success=" + crops.Success + ", matured=" + crops.CropsMatured + "/" + crops.PlantBasinsVisited + ", reason=" + FirstNonEmpty(crops.FailureReason, "none") + "}");

                InventoryGiveResult generator = experimentalApi.GiveCreativeGenerator(owner);
                if (!generator.Success)
                    throw new InvalidOperationException("Creative generator give failed: " + generator.FailureReason + ": " + generator.Message);
                required.Add("generator{success=True, id=" + FirstNonEmpty(generator.ItemId, "dtmapi_creative_generator") + ", before=" + generator.BeforeCount + ", after=" + generator.AfterCount + "}");

                SpawnDebugOption? monsterOption = experimentalApi.GetMonsterOptions().FirstOrDefault(o => o.IsAvailableInCurrentRoom);
                if (monsterOption == null)
                    throw new InvalidOperationException("No monster option was available in the current room.");
                else
                {
                    SpawnDebugResult monster = experimentalApi.SpawnMonster(owner, monsterOption.Id, 1);
                    if (!monster.Success)
                        throw new InvalidOperationException("Monster spawn failed: " + monster.FailureReason + ": " + monster.Message);
                    required.Add("monster{success=True, id=" + monsterOption.Id + ", count=" + monster.SpawnedCount + "}");
                }

                SpawnDebugOption? resourceOption = experimentalApi.GetResourceOptions().FirstOrDefault(o => o.IsAvailableInCurrentRoom);
                if (resourceOption == null)
                    throw new InvalidOperationException("No resource option was available in the current room.");
                else
                {
                    SpawnDebugResult resource = experimentalApi.SpawnResource(owner, resourceOption.Id, 1);
                    if (!resource.Success)
                        throw new InvalidOperationException("Resource spawn failed: " + resource.FailureReason + ": " + resource.Message);
                    required.Add("resource{success=True, id=" + resourceOption.Id + ", count=" + resource.SpawnedCount + "}");
                }

                string summary = "required=" + string.Join("; ", required.ToArray()) + "; optional=" + string.Join("; ", optional.ToArray());
                runtime.RuntimeMonitor.Log("Smoke exercise AdvancedDebug OK " + summary);
                runtime.SetHookStatus("Smoke.AdvancedDebug", "verified", "IAdvancedDebugApi whitelist", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke advanced debug exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AdvancedDebug", "failed", "IAdvancedDebugApi whitelist", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void TryExerciseZoomForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                ICameraZoomApi zoomApi = experimentalApi;
                ManifestModel owner = CreateZoomSmokeManifest();
                CameraZoomRegisterResult register = zoomApi.Register(owner, new CameraZoomOptions
                {
                    Enabled = true,
                    MinViewScale = 1,
                    MaxViewScale = 4,
                    Step = 1,
                    VerboseLogging = true
                });
                if (!register.Success)
                    throw new InvalidOperationException(register.FailureReason + ": " + register.Message);

                CameraZoomState before = zoomApi.GetState(owner.UniqueID);
                CameraZoomResult max = zoomApi.SetViewScale(owner, 4d, "smoke max-view");
                experimentalApi.UpdateRuntimeAutomation();
                CameraZoomState maxState = zoomApi.GetState(owner.UniqueID);
                CameraZoomResult reset = zoomApi.ResetViewScale(owner, "smoke restore-vanilla");
                experimentalApi.UpdateRuntimeAutomation();
                CameraZoomState after = zoomApi.GetState(owner.UniqueID);

                if (!max.Success)
                    throw new InvalidOperationException("4x apply failed: " + max.FailureReason + ": " + max.Message);
                if (!maxState.CameraAvailable)
                    throw new InvalidOperationException("Camera was not available after 4x apply. state=" + FormatZoomState(maxState));
                if (maxState.CurrentViewScale < 3.95d)
                    throw new InvalidOperationException("Expected 4x view scale after apply. state=" + FormatZoomState(maxState));
                if (maxState.AppliedOrthographicSize <= maxState.VanillaOrthographicSize)
                    throw new InvalidOperationException("Expected applied orthographic size to exceed vanilla size. state=" + FormatZoomState(maxState));
                if (!reset.Success)
                    throw new InvalidOperationException("Reset failed: " + reset.FailureReason + ": " + reset.Message);
                if (after.CurrentViewScale > 1.05d)
                    throw new InvalidOperationException("Expected vanilla view scale after reset. state=" + FormatZoomState(after));

                string summary = "before={" + FormatZoomState(before) + "}, max={" + FormatZoomState(maxState) + "}, reset={" + FormatZoomState(after) + "}, apply={" + max.Message + "}, restore={" + reset.Message + "}";
                runtime.RuntimeMonitor.Log("Smoke exercise Zoom OK " + summary);
                runtime.SetHookStatus("Smoke.Zoom", "verified", "ICameraZoomApi -> DolocAPI.mainCamera.orthographicSize", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke zoom exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.Zoom", "failed", "ICameraZoomApi", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void TryExerciseChestLocatorEnhancerForSmoke()
        {
            object? transientCase = null;
            object? targetRoom = null;
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new InvalidOperationException("DolocAPI was not available.");

                IChestLocatorEnhancerApi api = experimentalApi;
                ManifestModel owner = CreateChestLocatorSmokeManifest();
                ChestLocatorEnhancerRegisterResult register = api.Register(owner, new ChestLocatorEnhancerOptions
                {
                    Enabled = true,
                    IncludeSharedCases = true,
                    IncludeSharedStorageShelfBoxes = true,
                    RespectNativeAutoUseBoxSetting = true,
                    VerboseLogging = true
                });
                if (!register.Success)
                    throw new InvalidOperationException("Register failed: " + register.FailureReason + ": " + register.Message);
                if (!register.HookInstalled)
                    throw new InvalidOperationException("ArchiveDataHandle.GetAvailableInventories hook was not installed. message=" + register.Message);

                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                if (archive == null)
                    throw new InvalidOperationException("archiveHandle was not available.");
                targetRoom = FindChestLocatorSmokeBuildingRoom(dolocApi, archive, out string roomSummary);
                if (targetRoom == null)
                    throw new InvalidOperationException("No building room was available for cross-room chest locator smoke. " + roomSummary);

                string itemId = SelectZeroBaselineSmokeItemId(dolocApi, new[] { "dtmapi_mine", "crude_oil", "case_locator", "recipe_case_locator", "sunmao_showcase", "mountain_showcase" }, out int baseline, out string itemSummary);
                if (string.IsNullOrWhiteSpace(itemId))
                    throw new InvalidOperationException("No zero-baseline generated item was available for safe consume smoke. " + itemSummary);

                transientCase = TryCreateTransientEquipmentNoRenderForSmoke(dolocApi, targetRoom, "DolocTown.Case", new[] { "wooden_case", "large_wooden_case" }, out string caseSummary);
                if (transientCase == null)
                    throw new InvalidOperationException("Failed to create transient shared Case. " + caseSummary);
                if (!SetMemberValue(transientCase, "IsShared", true))
                    throw new InvalidOperationException("Failed to set transient Case IsShared=true. " + DescribeEquipmentForSmoke(transientCase));

                object? inventory = ReadMember(transientCase, "inventory");
                MethodInfo? placeItem = inventory == null ? null : FindMethod(inventory.GetType(), "PlaceItem", 1);
                object? item = GenerateItemForSmoke(dolocApi, itemId, 3);
                if (inventory == null || placeItem == null || item == null)
                    throw new InvalidOperationException("Failed to prepare transient inventory item. inventory=" + (inventory != null) + ", placeItem=" + (placeItem != null) + ", item=" + (item != null));
                object? leftover = placeItem.Invoke(inventory, new[] { item });
                if (leftover != null)
                    throw new InvalidOperationException("Transient Case inventory did not accept " + itemId + "; leftover=" + leftover.GetType().FullName);

                int afterPlace = CountNativeItemForSmoke(dolocApi, itemId, checkBox: true);
                if (afterPlace < baseline + 3)
                    throw new InvalidOperationException("CountItem did not include transient shared Case. item=" + itemId + ", baseline=" + baseline + ", afterPlace=" + afterPlace + ", bridge={" + experimentalApi.LastChestLocatorEnhancerSummary + "}");

                bool cost = CostNativeItemForSmoke(dolocApi, itemId, 2, checkBox: true);
                int afterCost = CountNativeItemForSmoke(dolocApi, itemId, checkBox: true);
                if (!cost || afterCost < baseline + 1 || afterCost > baseline + 1)
                    throw new InvalidOperationException("CostItem did not consume through shared inventory array. item=" + itemId + ", cost=" + cost + ", baseline=" + baseline + ", afterPlace=" + afterPlace + ", afterCost=" + afterCost + ", bridge={" + experimentalApi.LastChestLocatorEnhancerSummary + "}");

                ChestLocatorEnhancerState state = api.GetState(owner.UniqueID);
                if (state.LastAppendedInventoryCount <= 0 || state.LastSharedCaseCount <= 0)
                    throw new InvalidOperationException("Bridge state did not record appended shared Case inventory. state=" + FormatChestLocatorState(state));

                string summary = "item=" + itemId +
                    ", baseline=" + baseline +
                    ", afterPlace=" + afterPlace +
                    ", afterCost=" + afterCost +
                    ", room={" + roomSummary + "}" +
                    ", case={" + caseSummary + "}" +
                    ", bridge={" + experimentalApi.LastChestLocatorEnhancerSummary + "}" +
                    ", state={" + FormatChestLocatorState(state) + "}";
                runtime.RuntimeMonitor.Log("Smoke exercise ChestLocatorEnhancer OK " + summary);
                runtime.SetHookStatus("Smoke.ChestLocatorEnhancer", "verified", "IChestLocatorEnhancerApi -> ArchiveDataHandle.GetAvailableInventories -> CountItem/CostItem", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke chest locator enhancer exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.ChestLocatorEnhancer", "failed", "IChestLocatorEnhancerApi", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                if (transientCase != null)
                    TryRemoveTransientEquipmentForSmoke(targetRoom, transientCase);
            }
        }

        private void TryExerciseStrongPlantingGunForSmoke()
        {
            object? transientBasin = null;
            object? inventory = null;
            object? originalSlotItem = null;
            int quickSlot = 0;
            Type? dolocApiForCleanup = null;

            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new InvalidOperationException("DolocAPI was not available.");
                dolocApiForCleanup = dolocApi;

                IStrongPlantingGunApi api = experimentalApi;
                ManifestModel owner = CreateStrongPlantingGunSmokeManifest();
                StrongPlantingGunRegisterResult register = api.Register(owner, new StrongPlantingGunOptions
                {
                    Enabled = true,
                    SlotCount = 3,
                    IncludeSeeds = true,
                    IncludeFilms = true,
                    IncludeFertilizers = true,
                    IncludeWater = false,
                    VerboseLogging = true
                });
                if (!register.Success)
                    throw new InvalidOperationException("Register failed: " + register.FailureReason + ": " + register.Message);
                if (!register.ToolHookInstalled || !register.UiHookInstalled)
                    throw new InvalidOperationException("StrongPlantingGun hooks were not installed. toolHook=" + register.ToolHookInstalled + ", uiHook=" + register.UiHookInstalled + ", message=" + register.Message);

                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                    throw new InvalidOperationException("CurrentRoom was not available.");

                transientBasin = TryCreateTransientEquipmentForSmoke(dolocApi, room, "DolocTown.PlantBasin", new[] { "plantbasin_basic" }, out string basinSource);
                if (transientBasin == null)
                    throw new InvalidOperationException("No PlantBasin target available. source=" + basinSource);

                object? gun = GenerateItemForSmoke(dolocApi, "farming_gun", 1);
                if (gun == null || !IsTypeOrBase(gun.GetType(), "DolocTown.ItemFarmingGun"))
                    throw new InvalidOperationException("Could not generate official farming_gun item.");

                experimentalApi.ExpandFarmingGunInventoryIfNeeded(gun, "StrongPlantingGun smoke");
                object? gunInventory = ReadMember(gun, "inventory");
                if (gunInventory == null)
                    throw new InvalidOperationException("Generated farming gun did not expose inventory.");

                int inventoryCapacity = ReadIntMember(gunInventory, "capacity", 0);
                int totalCapacity = ReadIntMember(gun, "totalCapacity", 0);
                int lineCapacity = ReadIntMember(gun, "lineCapacity", 0);
                if (inventoryCapacity < 3 || totalCapacity < 3 || lineCapacity < 3)
                    throw new InvalidOperationException("Farming gun capacity was not expanded to three visible slots. inventory=" + inventoryCapacity + ", total=" + totalCapacity + ", line=" + lineCapacity + ".");

                object? seed = FindStrongPlantingGunSeedForSmoke(dolocApi, transientBasin, out string seedSummary);
                object? film = GenerateItemForSmoke(dolocApi, "plastic_film", 1);
                object? fertilizer = GenerateItemForSmoke(dolocApi, "fertilizer", 1);
                if (seed == null || film == null || fertilizer == null)
                    throw new InvalidOperationException("Could not generate seed/film/fertilizer. seed={" + seedSummary + "}, film=" + (film != null) + ", fertilizer=" + (fertilizer != null));

                bool seedPlaced = SwapInventoryItemAtForSmoke(gunInventory, 0, seed, out string seedPlace);
                bool filmPlaced = SwapInventoryItemAtForSmoke(gunInventory, 1, film, out string filmPlace);
                bool fertilizerPlaced = SwapInventoryItemAtForSmoke(gunInventory, 2, fertilizer, out string fertilizerPlace);
                if (!seedPlaced || !filmPlaced || !fertilizerPlaced)
                    throw new InvalidOperationException("Could not place strong planting gun contents. seed={" + seedPlace + "}, film={" + filmPlace + "}, fertilizer={" + fertilizerPlace + "}");

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, gun, quickSlot, out inventory, out originalSlotItem, out string quickSlotSummary))
                    throw new InvalidOperationException("Could not quick-slot generated farming gun. " + quickSlotSummary);

                object? selectedGun = ReadStaticMember(dolocApi, "SelectedItem");
                if (selectedGun == null || !IsTypeOrBase(selectedGun.GetType(), "DolocTown.ItemFarmingGun"))
                    throw new InvalidOperationException("SelectedItem was not the generated farming gun after quick-slot placement. selected=" + (selectedGun == null ? "null" : selectedGun.GetType().FullName));

                if (!TryPointAgentCellTipAtEquipmentForSmoke(dolocApi, transientBasin, out string tipSummary))
                    throw new InvalidOperationException("Could not point farming gun cell tip at transient basin. " + tipSummary);

                MethodInfo? onUseAsTool = FindMethod(selectedGun.GetType(), "OnUseAsTool", 0);
                if (onUseAsTool == null)
                    throw new MissingMethodException("ItemFarmingGun.OnUseAsTool was not found.");

                int beforeSeedCount = ReadInventoryItemCount(gunInventory, 0);
                int beforeFilmCount = ReadInventoryItemCount(gunInventory, 1);
                int beforeFertilizerCount = ReadInventoryItemCount(gunInventory, 2);
                onUseAsTool.Invoke(selectedGun, null);

                bool planted = ReadBoolMember(transientBasin, "IsPlanted", false);
                bool protectedByFilm = ReadBoolMember(transientBasin, "IsProtected", false);
                bool fertilized = ReadBoolMember(transientBasin, "IsFertilizerd", false);
                int afterSeedCount = ReadInventoryItemCount(gunInventory, 0);
                int afterFilmCount = ReadInventoryItemCount(gunInventory, 1);
                int afterFertilizerCount = ReadInventoryItemCount(gunInventory, 2);
                StrongPlantingGunState state = api.GetState(owner.UniqueID);

                if (!planted || !protectedByFilm || !fertilized || state.LastSeedActions <= 0 || state.LastFilmActions <= 0 || state.LastFertilizerActions <= 0)
                    throw new InvalidOperationException("Strong planting gun did not apply all three visible actions. planted=" + planted + ", protected=" + protectedByFilm + ", fertilized=" + fertilized + ", state={" + FormatStrongPlantingGunState(state) + "}");

                string summary = "seed={" + seedSummary + "}" +
                    ", basin={" + DescribeEquipmentForSmoke(transientBasin) + " source=" + basinSource + "}" +
                    ", capacities=inventory:" + inventoryCapacity + "/total:" + totalCapacity + "/line:" + lineCapacity +
                    ", counts=seed:" + beforeSeedCount + "->" + afterSeedCount +
                    ", film:" + beforeFilmCount + "->" + afterFilmCount +
                    ", fertilizer:" + beforeFertilizerCount + "->" + afterFertilizerCount +
                    ", basinState=planted:" + planted + ",protected:" + protectedByFilm + ",fertilized:" + fertilized +
                    ", quickSlot={" + quickSlotSummary + "}" +
                    ", tip={" + tipSummary + "}" +
                    ", state={" + FormatStrongPlantingGunState(state) + "}";
                runtime.RuntimeMonitor.Log("Smoke exercise StrongPlantingGun OK " + summary);
                runtime.SetHookStatus("Smoke.StrongPlantingGun", "verified", "IStrongPlantingGunApi -> ItemFarmingGun.OnUseAsTool", summary);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke strong planting gun exercise failed.", ex.InnerException.ToString());
                runtime.SetHookStatus("Smoke.StrongPlantingGun", "failed", "IStrongPlantingGunApi", ex.InnerException.GetType().Name + ": " + ex.InnerException.Message);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke strong planting gun exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.StrongPlantingGun", "failed", "IStrongPlantingGunApi", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                if (dolocApiForCleanup != null)
                    RestoreSmokeQuickSlot(dolocApiForCleanup, inventory, quickSlot, originalSlotItem);
                if (transientBasin != null && dolocApiForCleanup != null)
                    TryRemoveTransientEquipmentForSmoke(ReadStaticMember(dolocApiForCleanup, "CurrentRoom"), transientBasin);
            }
        }

        private static string FormatZoomState(CameraZoomState state)
        {
            if (state == null)
                return "unknown";

            return "owner=" + state.OwnerId +
                ", status=" + state.Status +
                ", enabled=" + state.Enabled +
                ", current=" + FormatSmokeDouble(state.CurrentViewScale) +
                ", range=" + FormatSmokeDouble(state.MinViewScale) + "-" + FormatSmokeDouble(state.MaxViewScale) +
                ", camera=" + state.CameraAvailable +
                ", vanillaSize=" + FormatSmokeDouble(state.VanillaOrthographicSize) +
                ", appliedSize=" + FormatSmokeDouble(state.AppliedOrthographicSize) +
                ", message=" + state.LastMessage;
        }

        private SmokeAttemptResult TryExerciseVehicleForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                const string vehicleId = "dtmapi.second_motor";
                const string keyItemId = "dtmapi_second_motor_key";
                ManifestModel smokeManifest = CreateSmokeManifest();
                MotorVehicleState registered = experimentalApi.GetVehicleState(vehicleId);
                if (!registered.IsRegistered)
                    throw new InvalidOperationException("SecondMotorMod did not register " + vehicleId + ".");

                if (!registered.OwnerUniqueId.Equals("DTMAPI.SecondMotorMod", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Second motor owner was " + registered.OwnerUniqueId + " instead of DTMAPI.SecondMotorMod.");

                if (Math.Abs(registered.SpeedMultiplier - 2) > 0.05)
                    throw new InvalidOperationException("Second motor speed multiplier was " + FormatSmokeDouble(registered.SpeedMultiplier) + " instead of 2x.");

                if (vehicleEdgeTransitionRequestedAt != default)
                {
                    if ((DateTimeOffset.Now - vehicleEdgeTransitionRequestedAt).TotalSeconds < 4)
                        return SmokeAttemptResult.Pending;

                    return CompleteVehicleEdgeTransitionForSmoke(smokeManifest, vehicleId, keyItemId, registered);
                }

                if (vehicleOutdoorTeleportRequestedAt != default &&
                    (DateTimeOffset.Now - vehicleOutdoorTeleportRequestedAt).TotalSeconds < 4)
                {
                    return SmokeAttemptResult.Pending;
                }

                string outdoorRecovery = vehicleOutdoorTeleportRequestedAt == default ? "not-needed" : "verified";
                if (!registered.IsAvailableInCurrentRoom)
                {
                    if (vehicleOutdoorTeleportRequestedAt == default)
                    {
                        TeleportDestination? farm = SelectVehicleOutdoorSmokeDestination(experimentalApi);
                        if (farm == null)
                            throw new InvalidOperationException("Current room disallows motor summon and no farm/outdoor teleport destination was available. reason=" + registered.LastFailureReason + " message=" + registered.LastMessage);

                        TeleportResult transport = experimentalApi.Teleport(smokeManifest, farm.Id);
                        if (!transport.Success)
                            throw new InvalidOperationException("Current room disallows motor summon and outdoor recovery teleport failed: " + transport.FailureReason + ": " + transport.Message);

                        vehicleOutdoorTeleportRequestedAt = DateTimeOffset.Now;
                        runtime.SetHookStatus("Smoke.VehicleSecondMotor", "pending", "ITeleportDebugApi -> DolocAPI.DoTransport", "Current room disallows motor summon (" + registered.LastFailureReason + "); requested outdoor recovery teleport destination=" + farm.Id + " markPoint=" + farm.MarkPointId + ".");
                        return SmokeAttemptResult.Pending;
                    }

                    if ((DateTimeOffset.Now - vehicleOutdoorTeleportRequestedAt).TotalSeconds < 4)
                        return SmokeAttemptResult.Pending;

                    registered = experimentalApi.GetVehicleState(vehicleId);
                    outdoorRecovery = "requested";
                    if (!registered.IsAvailableInCurrentRoom)
                        throw new InvalidOperationException("Outdoor recovery teleport did not reach a motor-enabled room. reason=" + registered.LastFailureReason + " message=" + registered.LastMessage);

                    outdoorRecovery = "verified";
                }

                InventoryGiveResult giveKey = experimentalApi.GiveItem(smokeManifest, keyItemId, 1);
                if (!giveKey.Success)
                    throw new InvalidOperationException("Failed to give second motor key: " + giveKey.FailureReason + ": " + giveKey.Message);

                MotorVehicleState originalBeforeKey = experimentalApi.GetOriginalMotorState();
                if (!originalBeforeKey.IsUnlocked)
                {
                    MotorVehicleSummonResult unlock = experimentalApi.UnlockOriginalMotor(smokeManifest, 0);
                    if (!unlock.Success)
                        throw new InvalidOperationException("Original motor unlock before key smoke failed: " + unlock.FailureReason + ": " + unlock.Message);
                }

                MotorVehicleSummonResult originalKeySummon = experimentalApi.UseOriginalMotorKeyForSmoke("motor_key");
                if (!originalKeySummon.Success)
                    throw new InvalidOperationException("Original motor key summon failed: " + originalKeySummon.FailureReason + ": " + originalKeySummon.Message);

                MotorVehicleSummonResult keySummon = experimentalApi.UseRegisteredSecondMotorKeyForSmoke(keyItemId);
                if (!keySummon.Success)
                    throw new InvalidOperationException("Second motor key summon failed: " + keySummon.FailureReason + ": " + keySummon.Message);

                MotorVehicleState originalAfterKeySummon = experimentalApi.GetOriginalMotorState();
                MotorVehicleState secondAfterKeySummon = experimentalApi.GetVehicleState(vehicleId);
                bool dualVisibleAfterKeySummon = originalAfterKeySummon.IsVisible && secondAfterKeySummon.IsVisible;
                runtime.RuntimeMonitor.Log("Smoke exercise VehicleSecondMotor dual-visible probe originalVisible=" + originalAfterKeySummon.IsVisible +
                    " originalRoom=" + originalAfterKeySummon.RoomId +
                    " secondVisible=" + secondAfterKeySummon.IsVisible +
                    " secondRoom=" + secondAfterKeySummon.RoomId +
                    " dualVisible=" + dualVisibleAfterKeySummon + ".");
                if (!dualVisibleAfterKeySummon)
                    throw new InvalidOperationException("Original and DTMAPI second motor were not simultaneously visible after original-key then second-key summon. originalRoom=" + originalAfterKeySummon.RoomId + " secondRoom=" + secondAfterKeySummon.RoomId + ".");

                string appearanceProbe = experimentalApi.ProbeSecondMotorAppearanceForSmoke(vehicleId);
                if (!appearanceProbe.Contains("appearanceIsolated=True"))
                    throw new InvalidOperationException("Second motor appearance was not isolated from the original motor: " + appearanceProbe);

                MotorVehicleRideResult ride = experimentalApi.RideVehicle(smokeManifest, vehicleId);
                if (!ride.Success || !ride.After.IsRiding)
                    throw new InvalidOperationException("Second motor ride failed: " + ride.FailureReason + ": " + ride.Message);

                vehicleEdgeTransitionBaseSummary = "vehicle=" + vehicleId +
                    ", owner=" + registered.OwnerUniqueId +
                    ", keyItem=" + keyItemId +
                    ", keyBefore=" + giveKey.BeforeCount +
                    ", keyAfter=" + giveKey.AfterCount +
                    ", originalKeySummonRoom=" + originalKeySummon.After.RoomId +
                    ", keySummonRoom=" + keySummon.After.RoomId +
                    ", originalVisibleAfterKey=" + originalAfterKeySummon.IsVisible +
                    ", secondVisibleAfterKey=" + secondAfterKeySummon.IsVisible +
                    ", dualVisibleAfterKey=" + dualVisibleAfterKeySummon +
                    ", appearanceProbe=" + appearanceProbe.Replace(", ", "|") +
                    ", ride=" + ride.After.IsRiding +
                    ", speedMultiplier=" + FormatSmokeDouble(registered.SpeedMultiplier) +
                    ", baseMaxSpeed=" + FormatSmokeDouble(registered.BaseMaxSpeed) +
                    ", effectiveMaxSpeed=" + FormatSmokeDouble(registered.EffectiveMaxSpeed) +
                    ", enduranceSummon=" + FormatSmokeDouble(keySummon.After.EnduranceProgress) +
                    ", enduranceRide=" + FormatSmokeDouble(ride.After.EnduranceProgress) +
                    ", outdoorRecovery=" + outdoorRecovery;
                if (StartVehicleEdgeTransitionForSmoke(smokeManifest, vehicleId, originalAfterKeySummon, ride.After))
                    return SmokeAttemptResult.Pending;

                MotorVehicleRideResult dismount = experimentalApi.DismountVehicle(smokeManifest, "smoke-restore");
                if (!dismount.Success || dismount.After.IsRiding)
                    throw new InvalidOperationException("Second motor dismount failed: " + dismount.FailureReason + ": " + dismount.Message);

                MotorVehicleSummonResult original = experimentalApi.SummonOriginalMotor(smokeManifest);
                if (!original.Success)
                    throw new InvalidOperationException("Original motor summon after second-motor dismount failed: " + original.FailureReason + ": " + original.Message);

                string disabledProbe = TryProbeVehicleDisabledLocationForSmoke(vehicleId, keyItemId);
                string summary = "vehicle=" + vehicleId +
                    ", owner=" + registered.OwnerUniqueId +
                    ", keyItem=" + keyItemId +
                    ", keyBefore=" + giveKey.BeforeCount +
                    ", keyAfter=" + giveKey.AfterCount +
                    ", originalKeySummonRoom=" + originalKeySummon.After.RoomId +
                    ", keySummonRoom=" + keySummon.After.RoomId +
                    ", originalVisibleAfterKey=" + originalAfterKeySummon.IsVisible +
                    ", secondVisibleAfterKey=" + secondAfterKeySummon.IsVisible +
                    ", dualVisibleAfterKey=" + dualVisibleAfterKeySummon +
                    ", appearanceProbe=" + appearanceProbe.Replace(", ", "|") +
                    ", ride=" + ride.After.IsRiding +
                    ", dismount=" + (!dismount.After.IsRiding) +
                    ", speedMultiplier=" + FormatSmokeDouble(registered.SpeedMultiplier) +
                    ", baseMaxSpeed=" + FormatSmokeDouble(registered.BaseMaxSpeed) +
                    ", effectiveMaxSpeed=" + FormatSmokeDouble(registered.EffectiveMaxSpeed) +
                    ", enduranceSummon=" + FormatSmokeDouble(keySummon.After.EnduranceProgress) +
                    ", enduranceRide=" + FormatSmokeDouble(ride.After.EnduranceProgress) +
                    ", enduranceDismount=" + FormatSmokeDouble(dismount.After.EnduranceProgress) +
                    ", enduranceTuning=unchanged" +
                    ", originalRoom=" + original.After.RoomId +
                    ", outdoorRecovery=" + outdoorRecovery +
                    ", disabledProbe=" + disabledProbe;
                runtime.RuntimeMonitor.Log("Smoke exercise VehicleSecondMotor OK " + summary);
                runtime.SetHookStatus("Smoke.VehicleSecondMotor", "verified", "IMotorVehicleApi + ItemMotorKey.OnUse prefix", summary);
                return SmokeAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke vehicle exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.VehicleSecondMotor", "failed", "IMotorVehicleApi", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private static TeleportDestination? SelectVehicleOutdoorSmokeDestination(ITeleportDebugApi api)
        {
            IReadOnlyList<TeleportDestination> destinations = api.GetDestinations();
            return destinations
                .Where(d => !string.IsNullOrWhiteSpace(d.MarkPointId))
                .FirstOrDefault(d => d.Id.StartsWith("farm:", StringComparison.OrdinalIgnoreCase) || ContainsIgnoreCase(d.DisplayName, "农场") || ContainsIgnoreCase(d.DisplayName, "Farm"))
                ?? destinations
                    .Where(d => !string.IsNullOrWhiteSpace(d.MarkPointId))
                    .OrderByDescending(d => d.IsStation)
                    .ThenBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
        }

        private bool StartVehicleEdgeTransitionForSmoke(ManifestModel smokeManifest, string vehicleId, MotorVehicleState originalBefore, MotorVehicleState secondBefore)
        {
            if (experimentalApi == null)
                throw new InvalidOperationException("Experimental bridge API is not available.");

            TeleportSnapshot before = experimentalApi.GetCurrentSnapshot();
            TeleportDestination? destination = SelectVehicleEdgeTransitionDestination(experimentalApi, before.RoomId);
            if (destination == null)
                throw new InvalidOperationException("No outdoor/motor-allowed transition destination was available for second-motor edge smoke. before=" + FormatTeleportSnapshot(before));

            TeleportResult transport = experimentalApi.Teleport(smokeManifest, destination.Id);
            if (!transport.Success)
                throw new InvalidOperationException("Second-motor edge transition request failed: " + transport.FailureReason + ": " + transport.Message);

            vehicleEdgeTransitionRequestedAt = DateTimeOffset.Now;
            vehicleEdgeTransitionDestination = destination;
            vehicleEdgeTransitionRequestResult = transport;
            vehicleEdgeTransitionOriginalBefore = originalBefore;
            vehicleEdgeTransitionSecondBefore = secondBefore;

            string summary = "vehicle=" + vehicleId +
                ", destination=" + FirstNonEmpty(destination.DisplayName, destination.Id) +
                ", markPoint=" + destination.MarkPointId +
                ", before={" + FormatTeleportSnapshot(before) + "}" +
                ", originalBeforeRoom=" + originalBefore.RoomId +
                ", secondBeforeRoom=" + secondBefore.RoomId +
                ", secondRidingBefore=" + secondBefore.IsRiding +
                ", requestAfter={" + FormatTeleportSnapshot(transport.AfterRequest) + "}";
            runtime.RuntimeMonitor.Log("Smoke exercise VehicleSecondMotor edge-transition request OK " + summary);
            runtime.SetHookStatus("Smoke.VehicleSecondMotorEdgeTransition", "pending", "ITeleportDebugApi -> DolocAPI.DoTransport while riding second motor", summary);
            return true;
        }

        private SmokeAttemptResult CompleteVehicleEdgeTransitionForSmoke(ManifestModel smokeManifest, string vehicleId, string keyItemId, MotorVehicleState registered)
        {
            if (experimentalApi == null)
                throw new InvalidOperationException("Experimental bridge API is not available.");

            TeleportSnapshot before = vehicleEdgeTransitionRequestResult?.Before ?? new TeleportSnapshot();
            TeleportSnapshot after = experimentalApi.GetCurrentSnapshot();
            MotorVehicleState originalAfterTransition = experimentalApi.GetOriginalMotorState();
            MotorVehicleState secondAfterTransition = experimentalApi.GetVehicleState(vehicleId);
            bool changedRoom = !string.IsNullOrWhiteSpace(before.RoomId) && !before.RoomId.Equals(after.RoomId, StringComparison.OrdinalIgnoreCase);
            bool secondStillRiding = secondAfterTransition.IsRiding;
            bool secondInCurrentRoom = !string.IsNullOrWhiteSpace(after.RoomId) && secondAfterTransition.RoomId.Equals(after.RoomId, StringComparison.OrdinalIgnoreCase);
            double distanceToDestination = vehicleEdgeTransitionDestination == null
                ? double.NaN
                : DistanceBetween(after.X, after.Y, vehicleEdgeTransitionDestination.X, vehicleEdgeTransitionDestination.Y);
            bool nearDestination = !double.IsNaN(distanceToDestination) && distanceToDestination < 8;
            double originalDistanceToEntry = DistanceBetween(originalAfterTransition.X, originalAfterTransition.Y, after.X, after.Y);
            bool originalAtNewEntry = originalAfterTransition.IsVisible &&
                originalAfterTransition.RoomId.Equals(after.RoomId, StringComparison.OrdinalIgnoreCase) &&
                !double.IsNaN(originalDistanceToEntry) &&
                originalDistanceToEntry < 6;
            bool noStuck = changedRoom && secondStillRiding && secondInCurrentRoom && nearDestination;

            string transitionSummary = "destination=" + FirstNonEmpty(vehicleEdgeTransitionDestination?.DisplayName ?? string.Empty, vehicleEdgeTransitionDestination?.Id ?? string.Empty) +
                ", markPoint=" + (vehicleEdgeTransitionDestination?.MarkPointId ?? string.Empty) +
                ", before={" + FormatTeleportSnapshot(before) + "}" +
                ", after={" + FormatTeleportSnapshot(after) + "}" +
                ", changedRoom=" + changedRoom +
                ", secondStillRiding=" + secondStillRiding +
                ", secondRoom=" + secondAfterTransition.RoomId +
                ", secondInCurrentRoom=" + secondInCurrentRoom +
                ", distanceToDestination=" + FormatSmokeDouble(distanceToDestination) +
                ", nearDestination=" + nearDestination +
                ", originalRoomAfterTransition=" + originalAfterTransition.RoomId +
                ", originalVisibleAfterTransition=" + originalAfterTransition.IsVisible +
                ", originalAtNewEntry=" + originalAtNewEntry +
                ", originalDistanceToEntry=" + FormatSmokeDouble(originalDistanceToEntry) +
                ", noStuck=" + noStuck;

            if (!noStuck || originalAtNewEntry)
                throw new InvalidOperationException("Second-motor edge transition failed. " + transitionSummary);

            runtime.RuntimeMonitor.Log("Smoke exercise VehicleSecondMotor edge-transition OK " + transitionSummary);
            runtime.SetHookStatus("Smoke.VehicleSecondMotorEdgeTransition", "verified", "ITeleportDebugApi -> DolocAPI.DoTransport while riding second motor", transitionSummary);

            MotorVehicleRideResult dismount = experimentalApi.DismountVehicle(smokeManifest, "smoke-edge-transition-restore");
            if (!dismount.Success || dismount.After.IsRiding)
                throw new InvalidOperationException("Second motor dismount after edge transition failed: " + dismount.FailureReason + ": " + dismount.Message);

            MotorVehicleSummonResult original = experimentalApi.SummonOriginalMotor(smokeManifest);
            if (!original.Success)
                throw new InvalidOperationException("Original motor summon after second-motor edge transition failed: " + original.FailureReason + ": " + original.Message);

            string disabledProbe = TryProbeVehicleDisabledLocationForSmoke(vehicleId, keyItemId);
            string summary = FirstNonEmpty(vehicleEdgeTransitionBaseSummary, "vehicle=" + vehicleId + ", owner=" + registered.OwnerUniqueId) +
                ", edgeTransition={" + transitionSummary + "}" +
                ", dismount=" + (!dismount.After.IsRiding) +
                ", enduranceDismount=" + FormatSmokeDouble(dismount.After.EnduranceProgress) +
                ", originalRoom=" + original.After.RoomId +
                ", originalVisibleAfterRestore=" + original.After.IsVisible +
                ", disabledProbe=" + disabledProbe;
            runtime.RuntimeMonitor.Log("Smoke exercise VehicleSecondMotor OK " + summary);
            runtime.SetHookStatus("Smoke.VehicleSecondMotor", "verified", "IMotorVehicleApi + ItemMotorKey.OnUse prefix + DoTransport edge transition", summary);
            return SmokeAttemptResult.Succeeded;
        }

        private static TeleportDestination? SelectVehicleEdgeTransitionDestination(ITeleportDebugApi api, string currentRoomId)
        {
            IReadOnlyList<TeleportDestination> destinations = api.GetDestinations();
            return destinations
                .Where(d => !string.IsNullOrWhiteSpace(d.MarkPointId))
                .Where(d => string.IsNullOrWhiteSpace(currentRoomId) || string.IsNullOrWhiteSpace(d.RoomId) || !d.RoomId.Equals(currentRoomId, StringComparison.OrdinalIgnoreCase))
                .Where(d => !LooksLikeIndoorVehicleDestination(d))
                .OrderByDescending(d => d.RoomId.StartsWith("city_", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(d => ContainsIgnoreCase(d.DisplayName, "丘陵") || ContainsIgnoreCase(d.DisplayName, "郊区") || ContainsIgnoreCase(d.DisplayName, "Farm") || ContainsIgnoreCase(d.DisplayName, "农场"))
                .ThenByDescending(d => d.IsStation)
                .ThenBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
        }

        private static bool LooksLikeIndoorVehicleDestination(TeleportDestination destination)
        {
            string text = (destination.Id ?? string.Empty) + " " + (destination.DisplayName ?? string.Empty) + " " + (destination.RoomId ?? string.Empty);
            return ContainsIgnoreCase(text, "hall") ||
                ContainsIgnoreCase(text, "research") ||
                ContainsIgnoreCase(text, "laboratory") ||
                ContainsIgnoreCase(text, "lab") ||
                ContainsIgnoreCase(text, "bar") ||
                ContainsIgnoreCase(text, "市政") ||
                ContainsIgnoreCase(text, "研究") ||
                ContainsIgnoreCase(text, "酒吧") ||
                ContainsIgnoreCase(text, "屋") ||
                ContainsIgnoreCase(text, "室内");
        }

        private static double DistanceBetween(double x1, double y1, double x2, double y2)
        {
            if (double.IsNaN(x1) || double.IsNaN(y1) || double.IsNaN(x2) || double.IsNaN(y2))
                return double.NaN;
            double dx = x1 - x2;
            double dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private string TryProbeVehicleDisabledLocationForSmoke(string vehicleId, string keyItemId)
        {
            try
            {
                if (experimentalApi == null)
                    return "not-verified:missing-api";

                TeleportSnapshot before = experimentalApi.GetCurrentSnapshot();
                TeleportDestination? indoor = experimentalApi.GetDestinations()
                    .Where(d => !string.IsNullOrWhiteSpace(d.MarkPointId))
                    .Where(d => !d.RoomId.Equals(before.RoomId, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault(d => ContainsIgnoreCase(d.Id, "hall") || ContainsIgnoreCase(d.Id, "research") || ContainsIgnoreCase(d.Id, "laboratory") || ContainsIgnoreCase(d.Id, "lab") || ContainsIgnoreCase(d.Id, "bar") || ContainsIgnoreCase(d.DisplayName, "市政") || ContainsIgnoreCase(d.DisplayName, "研究") || ContainsIgnoreCase(d.DisplayName, "酒吧"));
                if (indoor == null)
                    return "not-verified:no-indoor-whitelist-destination";

                TeleportResult transport = experimentalApi.Teleport(CreateSmokeManifest(), indoor.Id);
                if (!transport.Success)
                    return "not-verified:teleport-" + transport.FailureReason;

                TeleportSnapshot after = experimentalApi.GetCurrentSnapshot();
                bool changed = !string.IsNullOrWhiteSpace(before.RoomId) && !before.RoomId.Equals(after.RoomId, StringComparison.OrdinalIgnoreCase);
                if (!changed)
                    return "not-verified:teleport-not-settled:" + indoor.Id;

                MotorVehicleSummonResult disabledKey = experimentalApi.UseRegisteredSecondMotorKeyForSmoke(keyItemId);
                if (!disabledKey.Success && (disabledKey.FailureReason.Equals("in-house", StringComparison.OrdinalIgnoreCase) || disabledKey.FailureReason.Equals("disabled-room", StringComparison.OrdinalIgnoreCase)))
                    return "verified:" + disabledKey.FailureReason + ":" + FirstNonEmpty(after.RoomTitle, after.RoomId, indoor.Id);

                return "not-verified:unexpected-" + (disabledKey.Success ? "success" : disabledKey.FailureReason);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke vehicle disabled-location probe failed.", ex.ToString());
                return "not-verified:" + ex.GetType().Name;
            }
        }

        private static bool ContainsIgnoreCase(string value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(search) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsAny(IEnumerable<string> values, params string[] searches)
        {
            foreach (string value in values)
            {
                foreach (string search in searches)
                {
                    if (ContainsIgnoreCase(value, search))
                        return true;
                }
            }

            return false;
        }

        private SmokeAttemptResult TryExerciseDebugTeleportForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                TeleportSnapshot before = experimentalApi.GetCurrentSnapshot();
                IReadOnlyList<TeleportDestination> destinations = experimentalApi.GetDestinations();
                ManifestModel smokeManifest = CreateSmokeManifest();
                TeleportCsvExportResult csvExport = experimentalApi.ExportDestinationsCsv(smokeManifest);
                if (!csvExport.Success)
                    throw new InvalidOperationException("Teleport CSV export failed: " + csvExport.FailureReason + ": " + csvExport.Message);
                runtime.RuntimeMonitor.Log("Smoke exercise DebugTeleportCsv OK rows=" + csvExport.RowCount + " path=" + csvExport.Path);
                runtime.SetHookStatus("Smoke.DebugTeleportCsv", "verified", "ITeleportDebugApi.ExportDestinationsCsv", "rows=" + csvExport.RowCount + " path=" + csvExport.Path);

                TeleportDestination? destination = destinations
                    .Where(d => !string.IsNullOrWhiteSpace(d.MarkPointId))
                    .Where(d => string.IsNullOrWhiteSpace(before.RoomId) || !d.RoomId.Equals(before.RoomId, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(d => d.IsStation)
                    .ThenBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault()
                    ?? destinations.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d.MarkPointId));
                if (destination == null)
                    throw new InvalidOperationException("No whitelisted teleport destination was available.");

                debugTeleportBeforeSnapshot = before;
                debugTeleportDestination = destination;
                TeleportResult result = experimentalApi.Teleport(smokeManifest, destination.Id);
                debugTeleportRequestResult = result;
                if (!result.Success)
                    throw new InvalidOperationException(result.FailureReason + ": " + result.Message);

                debugTeleportRequestedAt = DateTimeOffset.Now;
                string summary = "destination=" + FirstNonEmpty(destination.DisplayName, destination.Id) +
                    ", markPoint=" + destination.MarkPointId +
                    ", from={" + FormatTeleportSnapshot(before) + "}" +
                    ", requestAfter={" + FormatTeleportSnapshot(result.AfterRequest) + "}";
                runtime.RuntimeMonitor.Log("Smoke exercise DebugTeleport request OK " + summary);
                runtime.SetHookStatus("Smoke.DebugTeleport", "pending", "ITeleportDebugApi -> DolocAPI.DoTransport", summary);
                return SmokeAttemptResult.Pending;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug teleport exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugTeleport", "failed", "ITeleportDebugApi", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private void CompleteDebugTeleportForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental bridge API is not available.");

                TeleportSnapshot before = debugTeleportBeforeSnapshot ?? debugTeleportRequestResult?.Before ?? new TeleportSnapshot();
                TeleportSnapshot after = experimentalApi.GetCurrentSnapshot();
                bool changedRoom = !string.IsNullOrWhiteSpace(before.RoomId) && !before.RoomId.Equals(after.RoomId, StringComparison.OrdinalIgnoreCase);
                double distance = DistanceBetween(before, after);
                bool moved = changedRoom || (!double.IsNaN(distance) && distance > 1);
                string summary = "destination=" + FirstNonEmpty(debugTeleportDestination?.DisplayName ?? string.Empty, debugTeleportDestination?.Id ?? string.Empty, debugTeleportRequestResult?.DestinationId ?? string.Empty) +
                    ", markPoint=" + FirstNonEmpty(debugTeleportDestination?.MarkPointId ?? string.Empty, debugTeleportRequestResult?.MarkPointId ?? string.Empty) +
                    ", before={" + FormatTeleportSnapshot(before) + "}" +
                    ", after={" + FormatTeleportSnapshot(after) + "}" +
                    ", changedRoom=" + changedRoom +
                    ", distance=" + FormatSmokeDouble(distance);

                if (!moved)
                    throw new InvalidOperationException("Teleport request did not change the observed room/position within the smoke verification window. " + summary);

                runtime.RuntimeMonitor.Log("Smoke exercise DebugTeleport OK " + summary);
                runtime.SetHookStatus("Smoke.DebugTeleport", "verified", "ITeleportDebugApi -> DolocAPI.DoTransport", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug teleport completion failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugTeleport", "failed", "ITeleportDebugApi -> DolocAPI.DoTransport", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void TryQuitAfterDebugSmoke()
        {
            if (smokeSettings == null || !smokeSettings.Enabled || !smokeSettings.AutoExitAfterSaveLoaded || autoExitAttempted)
                return;
            if (!IsDebugSmokeRequested())
                return;
            if (smokeSettings.AutoExerciseDebugConsole && !autoExerciseDebugConsoleAttempted)
                return;
            if (smokeSettings.AutoExerciseDebugInventory && !autoExerciseDebugInventoryAttempted)
                return;
            if (smokeSettings.AutoExerciseDebugWeather && !autoExerciseDebugWeatherAttempted)
                return;
            if (smokeSettings.AutoExerciseDebugTime && !autoExerciseDebugTimeAttempted)
                return;
            if (smokeSettings.AutoExerciseDebugMovement && !autoExerciseDebugMovementAttempted)
                return;
            if (smokeSettings.AutoExerciseAdvancedDebug && !autoExerciseAdvancedDebugAttempted)
                return;
            if (smokeSettings.AutoExerciseInstantSave && !autoExerciseInstantSaveAttempted)
                return;
            if (smokeSettings.AutoExerciseVehicle && !autoExerciseVehicleAttempted)
                return;
            if (smokeSettings.AutoExerciseNewContentApis && !autoExerciseNewContentApisAttempted)
                return;
            if (smokeSettings.AutoExerciseMineContentApis && !autoExerciseMineContentApisAttempted)
                return;
            if (smokeSettings.AutoExerciseZoom && !autoExerciseZoomAttempted)
                return;
            if (smokeSettings.AutoExerciseChestLocatorEnhancer && !autoExerciseChestLocatorEnhancerAttempted)
                return;
            if (smokeSettings.AutoExerciseStrongPlantingGun && !autoExerciseStrongPlantingGunAttempted)
                return;
            if (smokeSettings.AutoExerciseCustomEntityApis && !autoExerciseCustomEntityApisAttempted)
                return;
            if (smokeSettings.AutoExerciseDebugTeleport && !debugTeleportVerificationCompleted)
                return;

            autoExitAttempted = true;
            TryQuitApplication("smoke debug console/API evidence captured");
        }

        private void TryExerciseDebugConsoleHotkeyForSmoke()
        {
            try
            {
                if (debugConsoleApi == null)
                    throw new InvalidOperationException("Debug console host API was not available to the GameBridge smoke runner.");

                ManifestModel owner = CreateDebugConsoleSmokeManifest();
                if (debugConsoleApi.IsOpen)
                    debugConsoleApi.Close(owner, "smoke-reset");

                int openCount = 0;
                int escapeCloseCount = 0;
                int yCloseCount = 0;

                DispatchSmokeInput("Y");
                if (!debugConsoleApi.IsOpen)
                    throw new InvalidOperationException("Y did not open the debug console through the DTMAPI input event path.");
                openCount++;

                debugConsoleApi.Close(owner, "Escape");
                if (debugConsoleApi.IsOpen)
                    throw new InvalidOperationException("Escape did not close the debug console host state.");
                escapeCloseCount++;

                DispatchSmokeInput("Y");
                if (!debugConsoleApi.IsOpen)
                    throw new InvalidOperationException("Second Y did not reopen the debug console through the DTMAPI input event path.");
                openCount++;

                debugConsoleApi.Close(owner, "Y");
                if (debugConsoleApi.IsOpen)
                    throw new InvalidOperationException("Y close did not close the debug console host state.");
                yCloseCount++;

                for (int tap = 1; tap <= 10; tap++)
                {
                    if (debugConsoleApi.IsOpen)
                    {
                        debugConsoleApi.Close(owner, "Y");
                        if (debugConsoleApi.IsOpen)
                            throw new InvalidOperationException("Short Y tap " + tap + " did not close the console.");
                        yCloseCount++;
                    }
                    else
                    {
                        DispatchSmokeInput("Y");
                        if (!debugConsoleApi.IsOpen)
                            throw new InvalidOperationException("Short Y tap " + tap + " did not open the console.");
                        openCount++;
                    }
                }

                int openBeforeHold = openCount;
                int yCloseBeforeHold = yCloseCount;
                DispatchSmokeInput("Y");
                if (!debugConsoleApi.IsOpen)
                    throw new InvalidOperationException("Held-Y smoke did not open the console before the no-flicker check.");
                openCount++;

                runtime.RecordInputPressed("Y");
                runtime.RecordInputReleased("Y");
                bool holdNoFlicker = debugConsoleApi.IsOpen && openCount == openBeforeHold + 1 && yCloseCount == yCloseBeforeHold;
                if (!holdNoFlicker)
                    throw new InvalidOperationException("Held-Y smoke changed the expected open/close counts.");

                bool keepOpenForMouseGive = smokeSettings?.AutoExerciseDebugConsoleMouseGive == true;
                if (!keepOpenForMouseGive && debugConsoleApi.IsOpen)
                    debugConsoleApi.Close(owner, "smoke-cleanup");

                string summary = "openCount=" + openCount +
                    ", escapeCloseCount=" + escapeCloseCount +
                    ", yCloseCount=" + yCloseCount +
                    ", shortTaps=10" +
                    ", holdNoFlicker=" + holdNoFlicker +
                    ", keepOpenForMouseGive=" + keepOpenForMouseGive;
                runtime.RuntimeMonitor.Log("Smoke exercise DebugConsoleHotkey OK " + summary);
                runtime.SetHookStatus("Smoke.DebugConsoleHotkey", "verified", "DtmApiRuntime.RecordInputPressed + IDebugConsoleApi", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke debug console hotkey exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DebugConsoleHotkey", "failed", "DtmApiRuntime.RecordInputPressed + IDebugConsoleApi", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void DispatchSmokeInput(string button)
        {
            runtime.RecordInputPressed(button);
            runtime.RecordInputReleased(button);
        }

        private void TryExerciseCustomEntityApisForSmoke()
        {
            ManifestModel owner = CreateCustomEntitySmokeManifest();
            string animalId = owner.UniqueID + ".Animal";
            string monsterId = owner.UniqueID + ".Monster";
            string attackId = owner.UniqueID + ".Attack";
            string droneId = owner.UniqueID + ".Drone";
            try
            {
                runtime.CustomEntities.RemoveOwner(owner.UniqueID, "pre-smoke cleanup");

                int animalEvents = 0;
                int monsterEvents = 0;
                int attackEvents = 0;
                int droneEvents = 0;
                EventHandler<CustomAnimalLifecycleEventArgs> animalListener = (_, __) => animalEvents++;
                EventHandler<CustomMonsterLifecycleEventArgs> monsterListener = (_, __) => monsterEvents++;
                EventHandler<CustomAttackLifecycleEventArgs> attackListener = (_, __) => attackEvents++;
                EventHandler<CustomDroneLifecycleEventArgs> droneListener = (_, __) => droneEvents++;
                runtime.CustomEntities.AnimalLifecycleChanged += animalListener;
                runtime.CustomEntities.MonsterLifecycleChanged += monsterListener;
                runtime.CustomEntities.AttackLifecycleChanged += attackListener;
                runtime.CustomEntities.DroneLifecycleChanged += droneListener;
                try
                {
                    CustomAnimalRegistrationResult invalidAnimal = runtime.CustomEntities.RegisterSpecies(owner, new CustomAnimalSpeciesDefinition { SpeciesId = "SmokeAnimal" });
                    EnsureCustomEntitySmoke(!invalidAnimal.Succeeded && invalidAnimal.FailureReason == "invalid-definition", "Invalid animal definition should fail namespaced-ID validation.");

                    CustomAnimalRegistrationResult animal = runtime.CustomEntities.RegisterSpecies(owner, new CustomAnimalSpeciesDefinition
                    {
                        SpeciesId = animalId,
                        VariantIds = new[] { "default" },
                        DisplayName = SmokeText("Custom Entity Smoke Animal"),
                        Description = SmokeText("Contract-only animal used by the internal DTMAPI smoke harness."),
                        Diet = new CustomAnimalDietPolicy { AcceptedItemIds = new[] { "hay" }, UnitsPerFeeding = 1, CanGraze = true },
                        Consumption = new CustomAnimalConsumptionPolicy { HungerIntervalHours = 12, MaxFeedCapacity = 2 },
                        Excrement = new CustomAnimalExcrementPolicy { Enabled = true, IntervalHours = 24, MaxPendingCount = 2, Outputs = new[] { new CustomAnimalItemOutput { ItemId = "dtmapi_smoke_excrement", MinStack = 1, MaxStack = 1 } } },
                        Breeding = new CustomAnimalBreedingPolicy { Enabled = true, CompatibleSpeciesIds = new[] { animalId }, CooldownHours = 48, PregnancyOrIncubationHours = 72, OffspringCount = 1, PopulationLimitPerOwner = 8 },
                        HiddenProducts = new[] { new CustomAnimalProductRule { ProductId = "dtmapi_smoke_hidden_product", DisplayName = SmokeText("Smoke Hidden Product"), HiddenUntilReady = true, ProgressPerGameHour = 5, RequiredProgress = 100, Outputs = new[] { new CustomAnimalItemOutput { ItemId = "dtmapi_smoke_hidden_product", MinStack = 1, MaxStack = 1 } } } },
                        ProduceRules = new[] { new CustomAnimalProductRule { ProductId = "dtmapi_smoke_product", DisplayName = SmokeText("Smoke Product"), ProgressPerGameHour = 10, RequiredProgress = 100, Outputs = new[] { new CustomAnimalItemOutput { ItemId = "dtmapi_smoke_product", MinStack = 1, MaxStack = 2 } } } },
                        Persistence = SmokePersistence(),
                        TickPolicy = SmokeTickPolicy()
                    });
                    EnsureCustomEntitySmoke(animal.Succeeded, "Animal registration failed: " + animal.FailureReason);
                    CustomAnimalRegistrationResult duplicateAnimal = runtime.CustomEntities.RegisterSpecies(owner, new CustomAnimalSpeciesDefinition { SpeciesId = animalId });
                    EnsureCustomEntitySmoke(!duplicateAnimal.Succeeded && duplicateAnimal.FailureReason == "duplicate-definition-id", "Duplicate animal registration should fail.");

                    CustomAttackRegistrationResult attack = runtime.CustomEntities.RegisterAttack(owner, new CustomAttackDefinition
                    {
                        AttackId = attackId,
                        FactionId = "DTMAPI.Smoke",
                        RelationToPlayer = CustomEntityRelationKind.OwnerAlly,
                        Damage = new CustomDamagePayload { Amount = 1, DamageType = "smoke" },
                        EffectTags = new[] { "contract", "smoke" },
                        Hitbox = new CustomHitboxDefinition { Shape = CustomHitboxShapeKind.Circle, Radius = 0.5 },
                        Trajectory = new CustomTrajectoryDefinition { Kind = CustomEntityMovementKind.FollowTarget, Speed = 3 },
                        Pattern = new CustomBarragePatternDefinition { Kind = CustomAttackPatternKind.Projectile, ProjectileCount = 1, DeterministicRandomSeed = true },
                        Persistence = new CustomEntityPersistencePolicy { Kind = CustomEntityPersistenceKind.RuntimeOnly, SchemaVersion = 1 },
                        TickPolicy = SmokeTickPolicy()
                    });
                    EnsureCustomEntitySmoke(attack.Succeeded, "Attack registration failed: " + attack.FailureReason);

                    CustomMonsterRegistrationResult monster = runtime.CustomEntities.RegisterMonster(owner, new CustomMonsterDefinition
                    {
                        MonsterId = monsterId,
                        VariantIds = new[] { "default" },
                        DisplayName = SmokeText("Custom Entity Smoke Monster"),
                        Description = SmokeText("Contract-only monster used by the internal DTMAPI smoke harness."),
                        SpawnRules = new[] { new CustomMonsterSpawnRule { RuleId = owner.UniqueID + ".SpawnRule", RoomTags = new[] { "smoke" }, Probability = 1, MinGroupSize = 1, MaxGroupSize = 1 } },
                        MaxCountPerRoom = 1,
                        FactionId = "DTMAPI.Smoke",
                        Stats = new CustomMonsterStats { MaxHealth = 5, Armor = 0, ContactDamage = 1, MoveSpeed = 1 },
                        Targeting = new CustomMonsterTargetPolicy { AggroRange = 5, RetargetWhenDamaged = true },
                        Movement = new CustomMonsterMovementPolicy { Kind = CustomEntityMovementKind.Wander, PatrolRadius = 3 },
                        AttackSlots = new[] { new CustomMonsterAttackSlot { SlotId = "primary", AttackId = attackId, CooldownSeconds = 1, Range = 4 } },
                        Loot = new[] { new CustomMonsterLootRule { ItemId = "dtmapi_smoke_loot", MinStack = 1, MaxStack = 1, Chance = 1 } },
                        Persistence = SmokePersistence(),
                        TickPolicy = SmokeTickPolicy()
                    });
                    EnsureCustomEntitySmoke(monster.Succeeded, "Monster registration failed: " + monster.FailureReason);
                    CustomMonsterRegistrationResult spawnTable = runtime.CustomEntities.RegisterSpawnTable(owner, new CustomMonsterSpawnTableDefinition { SpawnTableId = owner.UniqueID + ".SpawnTable", MonsterIds = new[] { monsterId } });
                    EnsureCustomEntitySmoke(spawnTable.Succeeded, "Monster spawn-table registration failed: " + spawnTable.FailureReason);

                    CustomDroneRegistrationResult drone = runtime.CustomEntities.RegisterDrone(owner, new CustomDroneDefinition
                    {
                        DroneId = droneId,
                        VariantIds = new[] { "default" },
                        DisplayName = SmokeText("Custom Entity Smoke Drone"),
                        Description = SmokeText("Contract-only drone used by the internal DTMAPI smoke harness."),
                        OwnerBinding = new CustomDroneOwnerBindingPolicy { BindToPlayer = true, BindToOwnerMod = true },
                        SupportedModes = new[] { CustomDroneBehaviorMode.Follow, CustomDroneBehaviorMode.Guard, CustomDroneBehaviorMode.Attack },
                        EquipmentSlots = new[] { new CustomDroneEquipmentSlotDefinition { SlotId = "weapon", DisplayName = SmokeText("Weapon"), AllowedItemTags = new[] { "smoke-weapon" } } },
                        ModuleSlots = new[] { new CustomDroneEquipmentSlotDefinition { SlotId = "module", DisplayName = SmokeText("Module"), AllowedItemTags = new[] { "smoke-module" } } },
                        AttackIds = new[] { attackId },
                        Stats = new CustomDroneStats { MaxHealth = 10, MaxShield = 5, Armor = 1, ContactDamage = 1, MoveSpeed = 3 },
                        Energy = new CustomDroneEnergyPolicy { MaxEnergy = 100, EnergyPerSecond = 1, AttackEnergyCost = 5 },
                        Movement = new CustomDroneMovementPolicy { Kind = CustomEntityMovementKind.FollowTarget, FollowDistance = 2, ProviderCanOverride = true },
                        Repair = new CustomDroneRepairPolicy { CanRepair = true, RepairAmountPerItem = 5, RepairItemIds = new[] { "dtmapi_smoke_repair" } },
                        Summon = new CustomDroneSummonPolicy { CanSummonAnywhere = false, CooldownSeconds = 1, MaxActiveInstances = 1 },
                        Persistence = SmokePersistence(),
                        TickPolicy = SmokeTickPolicy()
                    });
                    EnsureCustomEntitySmoke(drone.Succeeded, "Drone registration failed: " + drone.FailureReason);

                    CustomAnimalSpawnResult animalSpawn = runtime.CustomEntities.RequestSpawn(owner, new CustomAnimalSpawnRequest { SpeciesId = animalId, Position = SmokePosition() });
                    CustomMonsterSpawnResult monsterSpawn = runtime.CustomEntities.RequestSpawn(owner, new CustomMonsterSpawnRequest { MonsterId = monsterId, Position = SmokePosition() });
                    CustomAttackSpawnResult attackSpawn = runtime.CustomEntities.SpawnProjectile(owner, new CustomAttackSpawnRequest { AttackId = attackId, Origin = SmokePosition() });
                    CustomDroneSummonResult droneSummon = runtime.CustomEntities.RequestSummon(owner, new CustomDroneSummonRequest { DroneId = droneId, Position = SmokePosition() });
                    EnsureCustomEntitySmoke(IsRuntimeCreationBlocked(animalSpawn), "Animal spawn should return runtime-creation-blocked.");
                    EnsureCustomEntitySmoke(IsRuntimeCreationBlocked(monsterSpawn), "Monster spawn should return runtime-creation-blocked.");
                    EnsureCustomEntitySmoke(IsRuntimeCreationBlocked(attackSpawn), "Attack spawn should return runtime-creation-blocked.");
                    EnsureCustomEntitySmoke(IsRuntimeCreationBlocked(droneSummon), "Drone summon should return runtime-creation-blocked.");

                    CustomDroneEquipmentResult equip = runtime.CustomEntities.Equip(owner, new CustomEntityHandle { Family = CustomEntityFamily.Drone, OwnerUniqueId = owner.UniqueID, DefinitionId = droneId, RuntimeId = "smoke-missing" }, new CustomDroneEquipmentRequest { SlotId = "weapon", ItemId = "dtmapi_smoke_weapon" });
                    CustomDroneCommandResult command = runtime.CustomEntities.SetMode(owner, new CustomEntityHandle { Family = CustomEntityFamily.Drone, OwnerUniqueId = owner.UniqueID, DefinitionId = droneId, RuntimeId = "smoke-missing" }, new CustomDroneCommandRequest { Mode = CustomDroneBehaviorMode.Guard, Reason = "smoke" });
                    EnsureCustomEntitySmoke(IsRuntimeCreationBlocked(equip), "Drone equipment request should return runtime-creation-blocked.");
                    EnsureCustomEntitySmoke(IsRuntimeCreationBlocked(command), "Drone mode request should return runtime-creation-blocked.");

                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetAnimalSnapshot(owner.UniqueID).RegisteredDefinitionCount == 1, "Animal snapshot should include one definition.");
                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetMonsterSnapshot(owner.UniqueID).RegisteredDefinitionCount == 1, "Monster snapshot should include one definition.");
                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetAttackSnapshot(owner.UniqueID).RegisteredDefinitionCount == 1, "Attack snapshot should include one definition.");
                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetDroneSnapshot(owner.UniqueID).RegisteredDefinitionCount == 1, "Drone snapshot should include one definition.");
                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetAnimalStatus(owner.UniqueID).Status == "configured-no-runtime-instance", "Animal status should report configured-no-runtime-instance.");
                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetMonsterStatus(owner.UniqueID).Status == "configured-no-runtime-instance", "Monster status should report configured-no-runtime-instance.");
                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetAttackStatus(owner.UniqueID).Status == "configured-no-runtime-instance", "Attack status should report configured-no-runtime-instance.");
                    EnsureCustomEntitySmoke(runtime.CustomEntities.GetDroneStatus(owner.UniqueID).Status == "configured-no-runtime-instance", "Drone status should report configured-no-runtime-instance.");
                }
                finally
                {
                    runtime.CustomEntities.AnimalLifecycleChanged -= animalListener;
                    runtime.CustomEntities.MonsterLifecycleChanged -= monsterListener;
                    runtime.CustomEntities.AttackLifecycleChanged -= attackListener;
                    runtime.CustomEntities.DroneLifecycleChanged -= droneListener;
                }

                int removed = runtime.CustomEntities.RemoveOwner(owner.UniqueID, "smoke cleanup");
                EnsureCustomEntitySmoke(removed >= 5, "Smoke cleanup should remove all four definitions and the monster spawn table. removed=" + removed);
                EnsureCustomEntitySmoke(runtime.CustomEntities.GetAnimalSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Animal cleanup snapshot should be empty.");
                EnsureCustomEntitySmoke(runtime.CustomEntities.GetMonsterSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Monster cleanup snapshot should be empty.");
                EnsureCustomEntitySmoke(runtime.CustomEntities.GetAttackSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Attack cleanup snapshot should be empty.");
                EnsureCustomEntitySmoke(runtime.CustomEntities.GetDroneSnapshot(owner.UniqueID).RegisteredDefinitionCount == 0, "Drone cleanup snapshot should be empty.");

                string summary = "registered=animal,monster,attack,drone; invalidAnimal=invalid-definition; duplicateAnimal=duplicate-definition-id; requests=runtime-creation-blocked; cleanupRemoved=" + removed + "; lifecycleEvents=" + animalEvents + "/" + monsterEvents + "/" + attackEvents + "/" + droneEvents + ".";
                runtime.RuntimeMonitor.Log("Smoke exercise CustomEntityApis OK " + summary);
                runtime.SetHookStatus("Smoke.CustomEntityApis", "verified", "DTMAPI.Core.CustomEntityRegistryService", summary);
            }
            catch (Exception ex)
            {
                runtime.CustomEntities.RemoveOwner(owner.UniqueID, "smoke failure cleanup");
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke custom entity API exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.CustomEntityApis", "failed", "DTMAPI.Core.CustomEntityRegistryService", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static CustomEntityLocalizedText SmokeText(string value)
        {
            return new CustomEntityLocalizedText
            {
                Default = value,
                English = value,
                SimplifiedChinese = value
            };
        }

        private static CustomEntityPersistencePolicy SmokePersistence()
        {
            return new CustomEntityPersistencePolicy
            {
                Kind = CustomEntityPersistenceKind.SaveScoped,
                SchemaVersion = 1,
                RemoveInstancesWhenOwnerMissing = true,
                RestoreRuntimeInstancesOnSaveLoad = false,
                SaveKeys = new[] { new CustomEntitySaveDataKey { Key = "smoke-state", Version = 1, Description = "Smoke-only stable custom entity state key." } }
            };
        }

        private static CustomEntityTickPolicy SmokeTickPolicy()
        {
            return new CustomEntityTickPolicy
            {
                Kind = CustomEntityTickPolicyKind.OneSecond,
                IntervalSeconds = 1,
                DeterministicOrder = true,
                Order = 0
            };
        }

        private static CustomEntityGridPosition SmokePosition()
        {
            return new CustomEntityGridPosition
            {
                RoomId = "smoke-current-room",
                X = 0,
                Y = 0,
                Layer = 0
            };
        }

        private static bool IsRuntimeCreationBlocked(CustomEntityRequestResult result)
        {
            return result != null &&
                !result.Succeeded &&
                result.FailureReason == "runtime-creation-blocked" &&
                result.RuntimeStatus == CustomEntityRuntimeStatus.RuntimeCreationBlocked;
        }

        private static void EnsureCustomEntitySmoke(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private bool IsDebugSmokeRequested()
        {
            return smokeSettings != null && (smokeSettings.AutoExerciseDebugConsole || smokeSettings.AutoExerciseDebugInventory || smokeSettings.AutoExerciseDebugWeather || smokeSettings.AutoExerciseDebugTeleport || smokeSettings.AutoExerciseDebugTime || smokeSettings.AutoExerciseDebugMovement || smokeSettings.AutoExerciseAdvancedDebug || smokeSettings.AutoExerciseVehicle || smokeSettings.AutoExerciseNewContentApis || smokeSettings.AutoExerciseMineContentApis || smokeSettings.AutoExerciseZoom || smokeSettings.AutoExerciseChestLocatorEnhancer || smokeSettings.AutoExerciseStrongPlantingGun || smokeSettings.AutoExerciseCustomEntityApis);
        }

        private static ManifestModel CreateSmokeManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI Smoke",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.Smoke",
                Type = "Smoke"
            };
        }

        private static ManifestModel CreateZoomSmokeManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI Zoom Smoke",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.ZoomMod",
                Type = "Smoke"
            };
        }

        private static ManifestModel CreateChestLocatorSmokeManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI Chest Locator Enhancer Smoke",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.ChestLocatorEnhancerMod",
                Type = "Smoke"
            };
        }

        private static ManifestModel CreateStrongPlantingGunSmokeManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI Strong Planting Gun Smoke",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.StrongPlantingGunMod",
                Type = "Smoke"
            };
        }

        private static ManifestModel CreateCustomEntitySmokeManifest()
        {
            return new ManifestModel
            {
                Name = "DTMAPI Custom Entity API Smoke",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.CustomEntityApiSmokeHarness",
                Type = "Smoke"
            };
        }

        private static ManifestModel CreateDebugConsoleSmokeManifest()
        {
            return new ManifestModel
            {
                Name = "Y-Key Console",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.DebugConsoleMod",
                Type = "Smoke"
            };
        }

        private static double DistanceBetween(TeleportSnapshot before, TeleportSnapshot after)
        {
            if (before == null || after == null || double.IsNaN(before.X) || double.IsNaN(before.Y) || double.IsNaN(after.X) || double.IsNaN(after.Y))
                return double.NaN;
            double dx = before.X - after.X;
            double dy = before.Y - after.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static string FormatTeleportSnapshot(TeleportSnapshot snapshot)
        {
            if (snapshot == null)
                return "unknown";
            return "roomId=" + snapshot.RoomId +
                ", roomTitle=" + snapshot.RoomTitle +
                ", roomType=" + snapshot.RoomType +
                ", position=" + FormatSmokeDouble(snapshot.X) + "," + FormatSmokeDouble(snapshot.Y) + "," + FormatSmokeDouble(snapshot.Z);
        }

        private static string FormatTimeSnapshot(TimeDebugState snapshot)
        {
            if (snapshot == null)
                return "unknown";
            return snapshot.Year + "-" + snapshot.Month + "-" + snapshot.Day + " " + snapshot.Hour.ToString("00") + ":" + snapshot.Minute.ToString("00") +
                ", weather=" + FirstNonEmpty(snapshot.CurrentWeatherName, snapshot.CurrentWeatherId) +
                ", period=" + snapshot.Period;
        }

        private static string FormatSmokeDouble(double value)
        {
            return double.IsNaN(value) ? "unknown" : value.ToString("0.###");
        }

        private void TryAutoReloadMods()
        {
            if (!TryReloadOfficialModsAndConfig("Smoke.AutoReloadMods", "smoke auto-reload mods", out string detail))
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-reload mods failed.", detail);
            }
        }

        private bool TryReloadOfficialModsAndConfig(string statusKey, string reason, out string detail)
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                object? modManager = dolocApi?.GetProperty("modManager", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                MethodInfo? reloadMods = modManager?.GetType().GetMethod("ReloadMods", BindingFlags.Public | BindingFlags.Instance);
                Type? dolocConfig = patcher.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
                MethodInfo? reloadConfig = dolocConfig?.GetMethod("Reload", BindingFlags.Public | BindingFlags.Static);
                if (modManager == null || reloadMods == null)
                    throw new MissingMethodException("DolocAPI.modManager.ReloadMods() was not found.");
                if (reloadConfig == null)
                    throw new MissingMethodException("DolocConfig.Reload() was not found.");

                detail = "official ModManager.ReloadMods + DolocConfig.Reload requested for " + reason + ".";
                runtime.RuntimeMonitor.Log(detail);
                runtime.SetHookStatus(statusKey, "pending", "DolocAPI.modManager.ReloadMods + DolocConfig.Reload", detail);
                reloadMods.Invoke(modManager, null);
                reloadConfig.Invoke(null, null);
                return true;
            }
            catch (Exception ex)
            {
                Exception root = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                detail = root.GetType().Name + ": " + root.Message;
                runtime.SetHookStatus(statusKey, "failed", "DolocAPI.modManager.ReloadMods + DolocConfig.Reload", detail);
                return false;
            }
        }

        private void TryExerciseExperimentalHooksForSmoke()
        {
            bool fishRoe = TryExerciseFishRoeTooltipForSmoke();
            bool animalViewer = TryExerciseAnimalViewerForSmoke();
            runtime.SetHookStatus("Smoke.ExperimentalHookExercise", (fishRoe && animalViewer) ? "verified" : "pending", "DTMAPI smoke exercise", "FishRoeTooltip=" + fishRoe + ", AnimalViewerRendering=" + animalViewer + ".");
        }

        private SmokeAttemptResult TryExerciseOneActionResourceHitForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental GameBridge API was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? toolColliderType = patcher.ResolveType("DolocTown.ToolCollider, Assembly-CSharp");
                Type? resourceRendererType = patcher.ResolveType("DolocTown.DungeonResourceRenderer, Assembly-CSharp");
                if (dolocApi == null || toolColliderType == null || resourceRendererType == null)
                    throw new MissingMemberException("DolocAPI, ToolCollider, or DungeonResourceRenderer was not visible.");

                if (!TryPrepareOneActionResourceHitWorldForSmoke(dolocApi, resourceRendererType, out string pendingReason))
                {
                    if ((DateTimeOffset.Now - lastOneActionReadinessLog).TotalSeconds >= 5)
                    {
                        lastOneActionReadinessLog = DateTimeOffset.Now;
                        runtime.RuntimeMonitor.Log("Smoke one-action resource-hit waiting: " + pendingReason);
                        runtime.SetHookStatus("Smoke.OneActionResourceHit", "pending", "ToolCollider.HandleTools", pendingReason);
                    }
                    return SmokeAttemptResult.Pending;
                }

                MethodInfo? handleTools = FindMethod(toolColliderType, "HandleTools", 1);
                MethodInfo? resetTool = FindMethod(toolColliderType, "ResetTool", 1);
                MethodInfo? resetChopCounter = FindMethod(toolColliderType, "ResetChopCounter", 1);
                if (handleTools == null || resetTool == null)
                    throw new MissingMethodException("ToolCollider.HandleTools/ResetTool path was not found.");

                object? toolCollider = FindToolColliderForSmoke(dolocApi, toolColliderType);
                if (toolCollider == null)
                    throw new InvalidOperationException("No ToolCollider instance was available after save load.");

                int rendererCount = 0;
                int resourceCount = 0;
                int policyMatchCount = 0;
                int colliderCount = 0;
                int toolCount = 0;
                int invokedCount = 0;
                List<string> samples = new List<string>();

                foreach (object renderer in FindUnityObjects(resourceRendererType))
                {
                    rendererCount++;
                    object? resource = ReadMember(renderer, "DungeonResource");
                    if (resource == null || IsRemoved(resource))
                        continue;
                    resourceCount++;
                    int healthBefore = ReadIntMember(resource, "currentHealth", 0);
                    if (healthBefore <= 0)
                        continue;
                    bool policyMatch = experimentalApi.TryFindOneActionPolicyForSmoke(resource, out string ownerId);
                    string resourceName = ReadStringMember(resource, "ResourceName", resource.GetType().Name);
                    string resourceClass = ReadResourceClass(resource);
                    if (samples.Count < 8)
                        samples.Add((resource.GetType().FullName ?? resource.GetType().Name) + "/" + resourceName + "/class=" + resourceClass + "/health=" + healthBefore + "/policy=" + policyMatch);
                    if (!policyMatch)
                        continue;
                    policyMatchCount++;
                    if (!TryChooseToolIdForResource(resource, out string toolId))
                        continue;

                    object? collider = ReadMember(renderer, "PolygonCollider");
                    if (collider == null)
                        continue;
                    colliderCount++;
                    object? tool = GenerateItemForSmoke(dolocApi, toolId);
                    if (tool == null)
                        continue;
                    toolCount++;

                    int toolDamage = ReadIntMember(tool, "ChopNumber", 0);
                    int seededHealth = healthBefore;
                    if (toolDamage > 0 && healthBefore <= toolDamage)
                    {
                        seededHealth = toolDamage + Math.Max(1, healthBefore);
                        WriteIntMember(resource, "currentHealth", seededHealth);
                    }

                    int appliedBefore = experimentalApi.OneActionApplicationCount;
                    resetTool.Invoke(toolCollider, new object[] { tool });
                    resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                    handleTools.Invoke(toolCollider, new object[] { collider });
                    invokedCount++;

                    int healthAfter = ReadIntMember(resource, "currentHealth", 0);
                    bool removedAfter = IsRemoved(resource);
                    if (experimentalApi.OneActionApplicationCount > appliedBefore)
                    {
                        string summary = "owner=" + ownerId + ", resource=" + resourceName + ", tool=" + toolId + ", healthBefore=" + healthBefore + ", seededHealth=" + seededHealth + ", toolDamage=" + toolDamage + ", healthAfter=" + healthAfter + ", removed=" + removedAfter + ", bridge=" + experimentalApi.LastOneActionApplicationSummary;
                        runtime.RuntimeMonitor.Log("Smoke exercise OneActionResourceHit OK " + summary);
                        runtime.SetHookStatus("Smoke.OneActionResourceHit", "verified", "ToolCollider.HandleTools private path on real DungeonResourceRenderer", summary);
                        return SmokeAttemptResult.Succeeded;
                    }
                }

                throw new InvalidOperationException("No rendered resource matched an enabled OneAction policy and survived the normal ToolCollider hit long enough for the Postfix completion path. renderers=" + rendererCount + ", resources=" + resourceCount + ", policyMatches=" + policyMatchCount + ", colliders=" + colliderCount + ", tools=" + toolCount + ", invoked=" + invokedCount + ", samples=" + string.Join(" ; ", samples));
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action resource-hit exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.OneActionResourceHit", "failed", "ToolCollider.HandleTools", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private SmokeAttemptResult TryExerciseOneActionWrongToolForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental GameBridge API was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? toolColliderType = patcher.ResolveType("DolocTown.ToolCollider, Assembly-CSharp");
                Type? resourceRendererType = patcher.ResolveType("DolocTown.DungeonResourceRenderer, Assembly-CSharp");
                if (dolocApi == null || toolColliderType == null || resourceRendererType == null)
                    throw new MissingMemberException("DolocAPI, ToolCollider, or DungeonResourceRenderer was not visible.");

                if (!TryPrepareOneActionResourceHitWorldForSmoke(dolocApi, resourceRendererType, out string pendingReason))
                {
                    if ((DateTimeOffset.Now - lastOneActionReadinessLog).TotalSeconds >= 5)
                    {
                        lastOneActionReadinessLog = DateTimeOffset.Now;
                        runtime.RuntimeMonitor.Log("Smoke one-action wrong-tool waiting: " + pendingReason);
                        runtime.SetHookStatus("Smoke.OneActionWrongTool", "pending", "ToolCollider.HandleTools", pendingReason);
                    }
                    return SmokeAttemptResult.Pending;
                }

                MethodInfo? handleTools = FindMethod(toolColliderType, "HandleTools", 1);
                MethodInfo? resetTool = FindMethod(toolColliderType, "ResetTool", 1);
                MethodInfo? resetChopCounter = FindMethod(toolColliderType, "ResetChopCounter", 1);
                if (handleTools == null || resetTool == null)
                    throw new MissingMethodException("ToolCollider.HandleTools/ResetTool path was not found.");

                object? toolCollider = FindToolColliderForSmoke(dolocApi, toolColliderType);
                if (toolCollider == null)
                    throw new InvalidOperationException("No ToolCollider instance was available after save load.");

                TryEnsureOneActionWrongToolMatrixResourcesForSmoke(dolocApi, resourceRendererType, out string matrixResourceSummary);

                int rendererCount = 0;
                int resourceCount = 0;
                int policyMatchCount = 0;
                int colliderCount = 0;
                int toolCount = 0;
                int invokedCount = 0;
                List<string> samples = new List<string>();
                Dictionary<string, string> covered = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, string> failed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> attemptedKinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (object renderer in FindUnityObjects(resourceRendererType))
                {
                    rendererCount++;
                    object? resource = ReadMember(renderer, "DungeonResource");
                    if (resource == null || IsRemoved(resource))
                        continue;
                    resourceCount++;

                    int healthBefore = ReadIntMember(resource, "currentHealth", 0);
                    if (healthBefore <= 0)
                        continue;

                    bool policyMatch = experimentalApi.TryFindOneActionPolicyForSmoke(resource, out string ownerId);
                    string resourceName = ReadStringMember(resource, "ResourceName", resource.GetType().Name);
                    string resourceClass = ReadResourceClass(resource);
                    if (samples.Count < 8)
                        samples.Add((resource.GetType().FullName ?? resource.GetType().Name) + "/" + resourceName + "/class=" + resourceClass + "/health=" + healthBefore + "/policy=" + policyMatch);
                    if (!policyMatch)
                        continue;
                    policyMatchCount++;

                    if (!TryChooseMismatchedToolIdForResource(resource, out string wrongToolId, out string expectedToolId))
                        continue;

                    string kind = GetOneActionResourceKind(resource);
                    if (string.IsNullOrWhiteSpace(kind) || covered.ContainsKey(kind) || attemptedKinds.Contains(kind))
                        continue;
                    attemptedKinds.Add(kind);

                    object? collider = ReadMember(renderer, "PolygonCollider");
                    if (collider == null)
                        continue;
                    colliderCount++;

                    object? wrongTool = GenerateItemForSmoke(dolocApi, wrongToolId);
                    if (wrongTool == null)
                        continue;
                    toolCount++;

                    int appliedBefore = experimentalApi.OneActionApplicationCount;
                    resetTool.Invoke(toolCollider, new object[] { wrongTool });
                    resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                    handleTools.Invoke(toolCollider, new object[] { collider });
                    invokedCount++;

                    int appliedAfter = experimentalApi.OneActionApplicationCount;
                    int healthAfter = ReadIntMember(resource, "currentHealth", 0);
                    bool removedAfter = IsRemoved(resource);
                    if (appliedAfter == appliedBefore && healthAfter == healthBefore && !removedAfter)
                    {
                        string wrongToolName = ReadStringMember(wrongTool, "name", wrongToolId);
                        string wrongToolType = ReadMember(wrongTool, "ToolType")?.ToString() ?? "unknown";
                        string result = "kind=" + kind + ", owner=" + ownerId + ", resource=" + resourceName + ", class=" + resourceClass + ", wrongTool=" + wrongToolName + ", wrongToolType=" + wrongToolType + ", expectedTool=" + expectedToolId + ", healthBefore=" + healthBefore + ", healthAfter=" + healthAfter + ", removed=" + removedAfter + ", oneActionDelta=0";
                        covered[kind] = result;
                        runtime.RuntimeMonitor.Log("Smoke exercise OneActionWrongTool sample OK " + result);
                        continue;
                    }

                    string failedResult = "resource=" + resourceName + "/kind=" + kind + "/wrongTool=" + wrongToolId + "/health=" + healthBefore + "->" + healthAfter + "/removed=" + removedAfter + "/oneActionDelta=" + (appliedAfter - appliedBefore);
                    failed[kind] = failedResult;
                    if (samples.Count < 8)
                        samples.Add("failedNegative/" + failedResult);
                }

                if (covered.Count > 0)
                {
                    string coveredKinds = string.Join(",", OneActionWrongToolTargetKinds.Where(k => covered.ContainsKey(k)));
                    string missingKinds = string.Join(",", OneActionWrongToolTargetKinds.Where(k => !covered.ContainsKey(k)));
                    string failedKinds = string.Join(",", failed.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
                    string summary = "coveredKinds=" + coveredKinds + ", missingKinds=" + (string.IsNullOrWhiteSpace(missingKinds) ? "none" : missingKinds) + ", failedKinds=" + (string.IsNullOrWhiteSpace(failedKinds) ? "none" : failedKinds) + ", " + string.Join(" | ", covered.Values) + (string.IsNullOrWhiteSpace(matrixResourceSummary) ? string.Empty : ", resourceSetup={" + matrixResourceSummary + "}");
                    string status = string.IsNullOrWhiteSpace(missingKinds) ? "verified" : "experimental";
                    runtime.RuntimeMonitor.Log("Smoke exercise OneActionWrongTool OK " + summary);
                    runtime.SetHookStatus("Smoke.OneActionWrongTool", status, "ToolCollider.HandleTools private path on real DungeonResourceRenderer", summary);
                    return SmokeAttemptResult.Succeeded;
                }

                throw new InvalidOperationException("No rendered resource produced a clean wrong-tool negative result. renderers=" + rendererCount + ", resources=" + resourceCount + ", policyMatches=" + policyMatchCount + ", colliders=" + colliderCount + ", tools=" + toolCount + ", invoked=" + invokedCount + ", samples=" + string.Join(" ; ", samples) + ", resourceSetup=" + matrixResourceSummary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action wrong-tool exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.OneActionWrongTool", "failed", "ToolCollider.HandleTools", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private SmokeAttemptResult TryExerciseOneActionFuelFeedForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental GameBridge API was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new MissingMemberException("DolocAPI was not visible.");

                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    LogOneActionFuelFeedPending("Waiting for NormalGameState before one-action fuel/feed smoke. context=" + runtime.UI.InputContext + ".");
                    return SmokeAttemptResult.Pending;
                }

                if (!actionSpeedInteractExitPatched)
                {
                    LogOneActionFuelFeedPending("Waiting for AgentStateInteract.OnExit Postfix before one-action fuel/feed smoke.");
                    return SmokeAttemptResult.Pending;
                }

                if (!TryExerciseOneActionEquipmentFillKindForSmoke(
                    dolocApi,
                    "FuelMachine",
                    "DolocTown.PowerGeneratorFuel",
                    new[] { "wood_generator", "coal_generator", "modified_fuel_generator" },
                    new[] { "wood", "coal", "weeds" },
                    "FuelPercent",
                    8,
                    out string fuelSummary))
                {
                    throw new InvalidOperationException("Fuel-machine native fill path did not produce verified extra consumption. " + fuelSummary);
                }

                if (!TryExerciseOneActionEquipmentFillKindForSmoke(
                    dolocApi,
                    "Feeder",
                    "DolocTown.Feeder",
                    new[] { "feeder", "large_feeder" },
                    new[] { "roughage_feed", "green_feed", "weeds", "thunder_grass" },
                    "progress",
                    8,
                    out string feederSummary))
                {
                    throw new InvalidOperationException("Feeder native fill path did not produce verified extra consumption. " + feederSummary);
                }

                string summary = "fuel={" + fuelSummary + "}; feeder={" + feederSummary + "}";
                runtime.RuntimeMonitor.Log("Smoke exercise OneActionFuelFeed OK " + summary);
                runtime.SetHookStatus("Smoke.OneActionFuelFeed", "verified", "Equipment.DecoratedInteract -> AgentStateInteract.OnExit Postfix -> native CostSelf/AddFuel/AddFeeds", summary);
                runtime.SetHookStatus("Actions.OneActionFuelFeed", "verified", "Harmony Postfix: AgentStateInteract.OnExit", "Fuel-machine and animal-feeder native CostSelf/AddFuel/AddFeeds paths verified by smoke. " + summary);
                return SmokeAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action fuel/feed exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.OneActionFuelFeed", "failed", "AgentStateInteract.OnExit", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private SmokeAttemptResult TryExerciseOneActionVegetationForSmoke()
        {
            object? vegetation = null;
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental GameBridge API was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? toolColliderType = patcher.ResolveType("DolocTown.ToolCollider, Assembly-CSharp");
                if (dolocApi == null || toolColliderType == null)
                    throw new MissingMemberException("DolocAPI or ToolCollider was not visible.");

                object? currentRoom = dolocApi.GetProperty("CurrentRoom", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (currentRoom == null)
                    throw new InvalidOperationException("CurrentRoom was unavailable.");

                bool isMainFarm = IsMainFarmRoomForSmoke(dolocApi, currentRoom);
                if (!isMainFarm && !autoExerciseOneActionMainFarmRequested && TryEnterMainFarmForOneActionSmoke(dolocApi, currentRoom, out string transitionSummary))
                {
                    LogOneActionVegetationPending(transitionSummary);
                    return SmokeAttemptResult.Pending;
                }
                if (!isMainFarm && autoExerciseOneActionMainFarmRequested)
                {
                    LogOneActionVegetationPending("Waiting for main farm transition before vegetation smoke. room=" + DescribeRoomForSmoke(currentRoom));
                    return SmokeAttemptResult.Pending;
                }

                MethodInfo? handleTools = FindMethod(toolColliderType, "HandleTools", 1);
                MethodInfo? resetTool = FindMethod(toolColliderType, "ResetTool", 1);
                MethodInfo? resetChopCounter = FindMethod(toolColliderType, "ResetChopCounter", 1);
                if (handleTools == null || resetTool == null)
                    throw new MissingMethodException("ToolCollider.HandleTools/ResetTool path was not found.");

                object? toolCollider = FindToolColliderForSmoke(dolocApi, toolColliderType);
                if (toolCollider == null)
                    throw new InvalidOperationException("No ToolCollider instance was available after save load.");

                currentRoom = dolocApi.GetProperty("CurrentRoom", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (currentRoom == null)
                    throw new InvalidOperationException("CurrentRoom was unavailable after transition.");

                if (!TryCreateTransientDandelionVegetationForSmoke(dolocApi, currentRoom, out vegetation, out object? renderer, out string sourceSummary) ||
                    vegetation == null ||
                    renderer == null)
                {
                    throw new InvalidOperationException("Could not create a transient dandelion vegetation sample. " + sourceSummary);
                }

                if (!TryChooseToolIdsForVegetation(vegetation, out string expectedToolId, out string expectedToolType, out int expectedMinLevel, out string wrongToolId, out string toolSummary))
                    throw new InvalidOperationException("Could not resolve vegetation tool constraints. " + toolSummary);

                object? collider = ReadMember(renderer, "Collider2d");
                if (collider == null)
                    throw new InvalidOperationException("VegetationRenderer.Collider2d was unavailable. target=" + DescribeVegetationForSmoke(vegetation));

                object? wrongTool = GenerateItemForSmoke(dolocApi, wrongToolId);
                object? expectedTool = GenerateItemForSmoke(dolocApi, expectedToolId);
                if (wrongTool == null || expectedTool == null)
                    throw new InvalidOperationException("Could not generate vegetation smoke tools. expected=" + expectedToolId + ", wrong=" + wrongToolId);

                int appBeforeWrong = experimentalApi.OneActionApplicationCount;
                resetTool.Invoke(toolCollider, new object[] { wrongTool });
                resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                handleTools.Invoke(toolCollider, new object[] { collider });
                int appAfterWrong = experimentalApi.OneActionApplicationCount;
                bool removedAfterWrong = ReadMember(renderer, "Vegetation") == null;
                if (removedAfterWrong || appAfterWrong != appBeforeWrong)
                    throw new InvalidOperationException("Wrong-tool vegetation hit was not a clean negative. removed=" + removedAfterWrong + ", oneActionDelta=" + (appAfterWrong - appBeforeWrong));

                int appBeforeCorrect = experimentalApi.OneActionApplicationCount;
                resetTool.Invoke(toolCollider, new object[] { expectedTool });
                resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                handleTools.Invoke(toolCollider, new object[] { collider });
                int appAfterCorrect = experimentalApi.OneActionApplicationCount;
                bool removedAfterCorrect = ReadMember(renderer, "Vegetation") == null;
                if (!removedAfterCorrect || appAfterCorrect != appBeforeCorrect)
                    throw new InvalidOperationException("Correct-tool vegetation hit did not stay on the native one-fell path. removed=" + removedAfterCorrect + ", oneActionDelta=" + (appAfterCorrect - appBeforeCorrect));

                string wrongToolName = ReadStringMember(wrongTool, "name", wrongToolId);
                string wrongToolType = ReadMember(wrongTool, "ToolType")?.ToString() ?? "unknown";
                string expectedToolName = ReadStringMember(expectedTool, "name", expectedToolId);
                string summary = "target=" + DescribeVegetationForSmoke(vegetation) +
                    ", source={" + sourceSummary + "}" +
                    ", path=ToolCollider.HandleTools->VegetationRenderer.OnFell->VegetationDandelion.OnFell->Vegetation.CheckToolConstraints" +
                    ", expectedTool=" + expectedToolName +
                    ", expectedToolType=" + expectedToolType +
                    ", expectedMinLevel=" + expectedMinLevel +
                    ", wrongTool=" + wrongToolName +
                    ", wrongToolType=" + wrongToolType +
                    ", wrongRemoved=" + removedAfterWrong +
                    ", correctRemoved=" + removedAfterCorrect +
                    ", oneActionDeltaWrong=" + (appAfterWrong - appBeforeWrong) +
                    ", oneActionDeltaCorrect=" + (appAfterCorrect - appBeforeCorrect) +
                    ", resourcePath=DungeonResourceRenderer:none";
                runtime.RuntimeMonitor.Log("Smoke exercise OneActionVegetation OK " + summary);
                runtime.SetHookStatus("Smoke.OneActionVegetation", "verified", "ToolCollider.HandleTools -> VegetationRenderer.OnFell", summary);
                runtime.SetHookStatus("Actions.OneActionVegetation", "verified", "native Vegetation.CheckToolConstraints", "Dandelion/vegetation is not a DungeonResource one-action path; wrong tools are rejected by the game and correct tools fell through native Vegetation.OnFell. " + summary);
                return SmokeAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action vegetation exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.OneActionVegetation", "failed", "ToolCollider.HandleTools -> VegetationRenderer.OnFell", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
            finally
            {
                if (vegetation != null)
                    TryRemoveTransientVegetationForSmoke(ReadStaticMember(patcher?.ResolveType("DolocAPI, Assembly-CSharp"), "CurrentRoom"), vegetation);
            }
        }

        private bool TryExerciseOneActionEquipmentFillKindForSmoke(Type dolocApi, string kind, string targetTypeName, string[] equipmentIds, string[] itemIds, string ratioMember, int stackCount, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            object? transientEquipment = null;
            int quickSlot = 0;

            try
            {
                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                {
                    summary = "CurrentRoom unavailable.";
                    return false;
                }

                object? equipment = TryCreateTransientEquipmentForSmoke(dolocApi, room, targetTypeName, equipmentIds, out string targetSource);
                if (equipment == null)
                {
                    equipment = FindExistingEquipmentForSmoke(room, targetTypeName, ratioMember, out targetSource);
                    if (equipment == null)
                    {
                        summary = "No target equipment available. targetType=" + targetTypeName + ", source=" + targetSource;
                        return false;
                    }
                }
                else
                {
                    transientEquipment = equipment;
                }

                string? itemId = SelectFillItemIdForSmoke(dolocApi, equipment, kind, itemIds, out string itemSelectSummary);
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    summary = "No valid fill item. target=" + DescribeEquipmentForSmoke(equipment) + ", " + itemSelectSummary;
                    return false;
                }

                object? item = GenerateItemForSmoke(dolocApi, itemId!, stackCount);
                if (item == null)
                {
                    summary = "Could not generate smoke item " + itemId + ".";
                    return false;
                }

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, item, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                {
                    summary = placeSummary;
                    return false;
                }

                object? anchor = ReadMember(equipment, "Anchor");
                string selectSummary = anchor == null ? "equipment anchor unavailable" : string.Empty;
                if (anchor == null || !TrySelectEquipmentForSmoke(dolocApi, equipment, anchor, out selectSummary))
                {
                    summary = "Could not select target equipment. target=" + DescribeEquipmentForSmoke(equipment) + ", " + selectSummary;
                    return false;
                }

                int beforeApplications = experimentalApi?.OneActionApplicationCount ?? 0;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                double beforeRatio = ReadDoubleMember(equipment, ratioMember, -1);
                MethodInfo? decoratedInteract = FindMethod(equipment.GetType(), "DecoratedInteract", 0);
                if (decoratedInteract == null)
                {
                    summary = "Equipment.DecoratedInteract was not found. target=" + DescribeEquipmentForSmoke(equipment);
                    return false;
                }

                decoratedInteract.Invoke(equipment, null);
                if (!TryRunCurrentAgentInteractExitForSmoke(dolocApi, out string interactSummary))
                {
                    summary = "Native interaction did not reach AgentStateInteract.OnExit. target=" + DescribeEquipmentForSmoke(equipment) + ", " + interactSummary;
                    return false;
                }

                int afterApplications = experimentalApi?.OneActionApplicationCount ?? 0;
                int afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                double afterRatio = ReadDoubleMember(equipment, ratioMember, -1);
                int totalConsumed = beforeCount - afterCount;
                bool applied = afterApplications > beforeApplications;
                bool changed = afterRatio > beforeRatio || (beforeRatio < 0 && totalConsumed > 0);
                if (!applied || totalConsumed < 2 || !changed)
                {
                    summary = "target=" + DescribeEquipmentForSmoke(equipment) +
                        ", item=" + itemId +
                        ", source=" + targetSource +
                        ", count=" + beforeCount + "->" + afterCount +
                        ", ratio=" + FormatRatio(beforeRatio) + "->" + FormatRatio(afterRatio) +
                        ", oneActionDelta=" + (afterApplications - beforeApplications) +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", bridge=" + (experimentalApi?.LastOneActionApplicationSummary ?? "none");
                    return false;
                }

                summary = "kind=" + kind +
                    ", target=" + DescribeEquipmentForSmoke(equipment) +
                    ", item=" + itemId +
                    ", source=" + targetSource +
                    ", count=" + beforeCount + "->" + afterCount +
                    ", totalConsumed=" + totalConsumed +
                    ", ratio=" + FormatRatio(beforeRatio) + "->" + FormatRatio(afterRatio) +
                    ", oneActionDelta=" + (afterApplications - beforeApplications) +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", bridge=" + (experimentalApi?.LastOneActionApplicationSummary ?? "none");
                runtime.RuntimeMonitor.Log("Smoke exercise OneActionFuelFeed sample OK " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action " + kind + " fill target failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action " + kind + " fill target failed.", ex.ToString());
                return false;
            }
            finally
            {
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
            }
        }

        private bool TryExerciseAutoFishingAutoCastForSmoke(Type dolocApi, Type fishingPoolType, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            int quickSlot = 0;

            try
            {
                if (experimentalApi == null)
                {
                    summary = "Experimental API unavailable.";
                    return false;
                }

                experimentalApi.SuppressFishingAutoCastForSmoke = false;

                experimentalApi.ResetFishingFeedbackCooldownForSmoke();
                experimentalApi.ForceFishingNoWaterForSmoke = true;
                experimentalApi.UpdateRuntimeAutomation();

                object? fishingPool = FindOrCreateFishingPoolForSmoke(fishingPoolType, dolocApi, out string poolSource);
                if (fishingPool == null)
                {
                    summary = "No fishing pool was available for auto-cast smoke. " + poolSource;
                    return false;
                }
                experimentalApi.FishingPoolOverrideForSmoke = fishingPool;

                experimentalApi.ResetFishingFeedbackCooldownForSmoke();
                experimentalApi.ForceFishingNoRodForSmoke = true;
                experimentalApi.UpdateRuntimeAutomation();
                experimentalApi.ForceFishingNoWaterForSmoke = false;
                experimentalApi.ForceFishingNoRodForSmoke = false;

                object? fishingRod = GenerateFishingRodForSmoke(dolocApi);
                if (fishingRod == null)
                {
                    summary = "Generated fishing rod was not available.";
                    return false;
                }

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, fishingRod, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                {
                    summary = placeSummary;
                    return false;
                }

                TryEnterIdleStateForSmoke(dolocApi);
                int beforeAutoCast = experimentalApi.FishingAutoCastApplicationCount;
                experimentalApi.ResetFishingFeedbackCooldownForSmoke();
                int afterAutoCast = beforeAutoCast;
                for (int attempt = 1; attempt <= 24; attempt++)
                {
                    experimentalApi.UpdateRuntimeAutomation();
                    afterAutoCast = experimentalApi.FishingAutoCastApplicationCount;
                    if (afterAutoCast > beforeAutoCast)
                        break;
                    Thread.Sleep(125);
                }
                string bridgeSummary = experimentalApi.LastFishingAutomationApplicationSummary;
                string attemptSummary = experimentalApi.LastFishingAutoCastAttemptSummary;
                bool invoked = afterAutoCast > beforeAutoCast && bridgeSummary.IndexOf("AutoCast", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!invoked)
                {
                    summary = "pool={" + poolSource + "}, rod=" + ReadStringMember(fishingRod, "name", fishingRod.GetType().Name) +
                        ", autoCastDelta=" + (afterAutoCast - beforeAutoCast) +
                        ", place={" + placeSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary) +
                        ", lastAttempt=" + (string.IsNullOrWhiteSpace(attemptSummary) ? "none" : attemptSummary);
                    return false;
                }

                summary = "pool={" + poolSource + "}, rod=" + ReadStringMember(fishingRod, "name", fishingRod.GetType().Name) +
                    ", autoCastDelta=" + (afterAutoCast - beforeAutoCast) +
                    ", place={" + placeSummary + "}" +
                    ", toastPolicy=0.2.3-suppressed-no-water-no-rod-cast" +
                    ", bridge=" + bridgeSummary;
                experimentalApi.SuppressFishingAutoCastForSmoke = true;
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-fishing auto-cast exercise failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-fishing auto-cast exercise failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (experimentalApi != null)
                {
                    experimentalApi.ForceFishingNoWaterForSmoke = false;
                    experimentalApi.ForceFishingNoRodForSmoke = false;
                    experimentalApi.FishingPoolOverrideForSmoke = null;
                }
            }
        }

        private SmokeAttemptResult TryExerciseAutoFishingPhaseForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental GameBridge API was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? fishingWaitType = patcher.ResolveType("DolocTown.AgentStateFishingWait, Assembly-CSharp");
                Type? fishingPoolType = patcher.ResolveType("DolocTown.FishingPool, Assembly-CSharp");
                if (dolocApi == null || fishingWaitType == null || fishingPoolType == null)
                    throw new MissingMemberException("DolocAPI, AgentStateFishingWait, or FishingPool was not visible.");

                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    LogAutoFishingPending("Waiting for NormalGameState before auto-fishing smoke. context=" + runtime.UI.InputContext + ".");
                    return SmokeAttemptResult.Pending;
                }

                if (!autoFishingHotkeyInjected)
                {
                    if (experimentalApi.TryGetEnabledFishingAutomationOwner(out string externallyEnabledOwner))
                    {
                        autoFishingHotkeyInjected = true;
                        runtime.RuntimeMonitor.Log("Smoke automation observed AutoFishing already enabled before synthetic input; treating F6 input chain as external/real path. owner=" + externallyEnabledOwner + ".");
                        runtime.SetHookStatus("Smoke.AutoFishingHotkey", "verified", "Unity input polling -> DTMAPI input event", "AutoFishing enabled by registered hotkey before bridge fallback; owner=" + externallyEnabledOwner + ".");
                    }
                    else if (smokeSettings?.AutoFishingExternalHotkeyRequired == true)
                    {
                        LogAutoFishingPending("Waiting for external F6 keypress to enable AutoFishing. context=" + runtime.UI.InputContext + ", menuOpen=" + runtime.UI.IsOpen + ".");
                        return SmokeAttemptResult.Pending;
                    }
                    else if (runtime.UI.BlocksGameplayHotkeys)
                    {
                        LogAutoFishingPending("Waiting for gameplay hotkeys before synthetic F6 toggle. context=" + runtime.UI.InputContext + ", menuOpen=" + runtime.UI.IsOpen + ".");
                        return SmokeAttemptResult.Pending;
                    }
                    else
                    {
                        runtime.RecordInputPressed("F6");
                        runtime.RecordInputReleased("F6");
                        autoFishingHotkeyInjected = true;
                        runtime.RuntimeMonitor.Log("Smoke automation dispatched AutoFishing toggle key F6 through DTMAPI input service.");
                        runtime.SetHookStatus("Smoke.AutoFishingHotkey", "verified", "DtmApiRuntime.RecordInputPressed", "Dispatched F6 through the same registered input event path used by gameplay hotkeys.");
                    }
                }

                if (!experimentalApi.TryGetEnabledFishingAutomationOwner(out string ownerId))
                    throw new InvalidOperationException("AutoFishing policy was not enabled after the F6 input dispatch. Is the official AutoFishing package enabled?");

                if (!autoExerciseAutoFishingMovementCancelVerified)
                {
                    runtime.RecordInputPressed("W");
                    runtime.RecordInputReleased("W");
                    if (experimentalApi.TryGetEnabledFishingAutomationOwner(out string stillEnabledOwner))
                        throw new InvalidOperationException("AutoFishing movement cancel did not disable automation. owner=" + stillEnabledOwner);

                    autoExerciseAutoFishingMovementCancelVerified = true;
                    runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingMovementCancel OK key=W owner=" + ownerId + ".");
                    runtime.SetHookStatus("Smoke.AutoFishingMovementCancel", "verified", "DTMAPI input W -> AutoFishingMod manual cancel", "Movement key W disabled automation after F6 enable; owner=" + ownerId + ".");

                    runtime.RecordInputPressed("F6");
                    runtime.RecordInputReleased("F6");
                    if (!experimentalApi.TryGetEnabledFishingAutomationOwner(out ownerId))
                        throw new InvalidOperationException("AutoFishing policy did not re-enable after movement-cancel re-toggle.");
                    runtime.RuntimeMonitor.Log("Smoke automation re-enabled AutoFishing after movement-cancel proof through F6 input. owner=" + ownerId + ".");
                    runtime.SetHookStatus("Smoke.AutoFishingHotkey", "verified", "DtmApiRuntime.RecordInputPressed", "F6 enabled automation and re-enabled it after movement-cancel proof.");
                }

                if (!autoExerciseAutoFishingAutoCastVerified)
                {
                    if (!TryExerciseAutoFishingAutoCastForSmoke(dolocApi, fishingPoolType, out string autoCastSummary))
                        throw new InvalidOperationException("AutoFishing auto-cast path failed. " + autoCastSummary);
                    autoExerciseAutoFishingAutoCastVerified = true;
                    runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingAutoCast OK " + autoCastSummary);
                    runtime.SetHookStatus("Smoke.AutoFishingAutoCast", "verified", "BodyController.UseFishRod", autoCastSummary);
                }

                if (!autoExerciseAutoFishingPhaseStarted)
                {
                    object? agent = dolocApi.GetProperty("agent", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                    object? fishingRod = GenerateFishingRodForSmoke(dolocApi);
                    if (agent == null || fishingRod == null)
                        throw new MissingMemberException("DolocAPI.agent or a generated fishing rod was not available.");

                    object? fishingPool = FindOrCreateFishingPoolForSmoke(fishingPoolType, dolocApi, out string poolSource);
                    if (fishingPool == null)
                        throw new InvalidOperationException("No fishing pool was available for auto-fishing smoke. " + poolSource);

                    object? cache = ReadMember(agent, "FishingCache");
                    object? stateManager = ReadMember(agent, "StateManager");
                    if (cache == null || stateManager == null)
                        throw new MissingMemberException("Agent FishingCache or StateManager was not available.");

                    if (!WriteObjectMember(cache, "FishingRod", fishingRod) || !WriteObjectMember(cache, "FishingPool", fishingPool))
                        throw new MissingMemberException("Could not seed FishingCache with a rod and pool for smoke.");

                    MethodInfo? overwrite = stateManager.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(m =>
                        {
                            if (m.Name != "Overwrite" || !m.IsGenericMethodDefinition)
                                return false;
                            ParameterInfo[] parameters = m.GetParameters();
                            return parameters.Length == 1 && parameters[0].ParameterType == typeof(bool);
                        });
                    if (overwrite == null)
                        throw new MissingMethodException("AgentStateManager.Overwrite<T>(bool) was not found.");

                    autoFishingApplicationBaseline = experimentalApi.FishingAutomationApplicationCount;
                    autoFishingMiniGameCompleteBaseline = experimentalApi.FishingMiniGameCompleteApplicationCount;
                    autoFishingPhaseStartedAt = DateTimeOffset.Now;
                    autoExerciseAutoFishingPhaseStarted = true;
                    experimentalApi.ForceFishingFishForSmoke = smokeSettings?.AutoExerciseAutoFishingMiniGameComplete == true;
                    runtime.RuntimeMonitor.Log("Smoke automation entering AgentStateFishingWait for AutoFishing phase evidence. owner=" + ownerId + ", rod=" + fishingRod.GetType().Name + ", poolSource=" + poolSource + ".");
                    runtime.SetHookStatus("Smoke.AutoFishingPhase", "pending", "AgentStateManager.Overwrite<AgentStateFishingWait>", "Entered real AgentStateFishingWait; waiting for OnPlay instant-bite automation.");
                    overwrite.MakeGenericMethod(fishingWaitType).Invoke(stateManager, new object[] { true });
                    return SmokeAttemptResult.Pending;
                }

                if (experimentalApi.FishingAutomationApplicationCount > autoFishingApplicationBaseline)
                {
                    string summary = experimentalApi.LastFishingAutomationApplicationSummary;
                    if (!autoExerciseAutoFishingPhaseVerified)
                    {
                        autoExerciseAutoFishingPhaseVerified = true;
                        runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingPhase OK " + summary);
                        runtime.SetHookStatus("Smoke.AutoFishingPhase", "verified", "AgentStateFishingWait.OnPlay Postfix", summary);
                    }

                    if (smokeSettings?.AutoExerciseAutoFishingMiniGameComplete == true)
                    {
                        if (experimentalApi.FishingMiniGameCompleteApplicationCount > autoFishingMiniGameCompleteBaseline)
                        {
                            experimentalApi.ForceFishingFishForSmoke = false;
                            string completeSummary = string.IsNullOrWhiteSpace(experimentalApi.LastFishingMiniGameCompleteSummary)
                                ? experimentalApi.LastFishingAutomationApplicationSummary
                                : experimentalApi.LastFishingMiniGameCompleteSummary;
                            runtime.RuntimeMonitor.Log("Smoke exercise AutoFishingMiniGameComplete OK " + completeSummary);
                            runtime.SetHookStatus("Smoke.AutoFishingMiniGameComplete", "verified", "FishingGameScrollBar.UpdateGame Postfix", completeSummary);
                            return SmokeAttemptResult.Succeeded;
                        }

                        if ((DateTimeOffset.Now - autoFishingPhaseStartedAt).TotalSeconds > 30)
                            throw new TimeoutException("AutoFishing skip=false minigame completion did not apply within 30 seconds after entering AgentStateFishingWait.");

                        LogAutoFishingPending("Waiting for FishingGameScrollBar.UpdateGame auto-complete skip=false. miniGameApplications=" + experimentalApi.FishingMiniGameCompleteApplicationCount + ".");
                        return SmokeAttemptResult.Pending;
                    }

                    experimentalApi.ForceFishingFishForSmoke = false;
                    return SmokeAttemptResult.Succeeded;
                }

                if ((DateTimeOffset.Now - autoFishingPhaseStartedAt).TotalSeconds > 20)
                    throw new TimeoutException("AutoFishing wait-phase automation did not apply within 20 seconds after entering AgentStateFishingWait.");

                LogAutoFishingPending("Waiting for AgentStateFishingWait.OnPlay instant-bite application. applications=" + experimentalApi.FishingAutomationApplicationCount + ".");
                return SmokeAttemptResult.Pending;
            }
            catch (Exception ex)
            {
                if (experimentalApi != null)
                    experimentalApi.ForceFishingFishForSmoke = false;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke auto-fishing phase exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AutoFishingPhase", "failed", "AgentStateFishingWait.OnPlay", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private SmokeAttemptResult TryExerciseNewContentApisForSmoke(bool mineOnly)
        {
            object? transientMine = null;
            object? room = null;
            string newContentStage = mineOnly ? "MineOfficialJson" : "OilItemMetadata";
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental GameBridge API was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new MissingMemberException("DolocAPI was not visible.");

                if (mineOnly && mineOfficialTechTreeUiOpenRequested && !mineOfficialTechTreeUiEvidenceCaptured)
                    return CaptureMineOfficialTechTreeUiEvidenceForSmoke(dolocApi);

                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    string recovery = mineOnly && mineOfficialTechTreeUiEvidenceCaptured
                        ? TryRecoverNormalStateAfterMineTechTreeUiForSmoke(dolocApi)
                        : "not-applicable";
                    runtime.SetHookStatus("Smoke.NewContentMineProduction", "pending", "NormalGameState", "Waiting for NormalGameState before creating a temporary dtmapi_mine. recovery={" + recovery + "}");
                    return SmokeAttemptResult.Pending;
                }

                room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                    throw new InvalidOperationException("CurrentRoom was not available for new-content smoke.");

                if (!mineOnly && ReadBoolMember(room, "IsInHouse", false))
                {
                    if (!autoExerciseOneActionMainFarmRequested && TryEnterMainFarmForOneActionSmoke(dolocApi, room, out string transitionSummary))
                    {
                        runtime.SetHookStatus("Smoke.NewContentOilCoalDrop", "pending", "DolocAPI.EnterFarm", transitionSummary);
                        return SmokeAttemptResult.Pending;
                    }

                    runtime.SetHookStatus("Smoke.NewContentOilCoalDrop", "pending", "DolocAPI.EnterFarm", "Waiting for outdoor farm transition before OilMod coal-resource smoke. room=" + DescribeRoomForSmoke(room));
                    return SmokeAttemptResult.Pending;
                }

                string oilItemMetadataSummary = mineOnly ? "skipped-mine-only" : TryExerciseOilItemMetadataForSmoke(dolocApi);
                newContentStage = mineOnly ? "MineOfficialJson" : "OilCoalDrop";
                string oilCoalDropSummary = mineOnly ? "skipped-mine-only" : TryExerciseOilCoalDropForSmoke(dolocApi, room);
                newContentStage = "MineOfficialJson";
                string mineOfficialJsonSummary = TryExerciseMineOfficialJsonForSmoke(dolocApi);
                if (mineOnly && !mineOfficialTechTreeUiEvidenceCaptured)
                    return OpenMineOfficialTechTreeUiForSmoke(dolocApi, mineOfficialJsonSummary);

                newContentStage = mineOnly ? "MineProduction" : "EquipmentSlots";
                string equipmentSlotsSummary = mineOnly ? "skipped-mine-only" : TryExerciseEquipmentSlotsForSmoke();
                newContentStage = "MineProduction";

                transientMine = TryCreateTransientEquipmentForSmoke(dolocApi, room, "DolocTown.Equipment", new[] { "dtmapi_mine" }, out string createSummary);
                if (transientMine == null)
                    throw new InvalidOperationException("Could not create transient dtmapi_mine. " + createSummary);
                if (!ReadStringMember(transientMine, "Name", string.Empty).Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Transient equipment was not dtmapi_mine. target=" + DescribeEquipmentForSmoke(transientMine) + ", create={" + createSummary + "}");
                experimentalApi.UpdateRuntimeAutomation(forceMachineProductionPoll: true);
                string scaleContainmentSummary = experimentalApi.ProbeMachineVisualScaleContainmentForSmoke("dtmapi_mine");
                if (ContainsIgnoreCase(scaleContainmentSummary, "contamination=True") ||
                    ContainsIgnoreCase(scaleContainmentSummary, "containment=failed"))
                {
                    throw new InvalidOperationException("Mine visual scale containment failed. " + scaleContainmentSummary);
                }

                int beforeCycles = experimentalApi.GetMachineProductionCycleCountForSmoke("DTMAPI.MineMod");
                TimeSkipResult timeSkip = experimentalApi.SkipToNextWeatherPeriod(CreateSmokeManifest());
                if (!timeSkip.Success)
                    throw new InvalidOperationException("Native pass-time smoke path failed before Mine catch-up. reason=" + timeSkip.FailureReason + ", message=" + timeSkip.Message);

                for (int attempt = 1; attempt <= 20; attempt++)
                {
                    experimentalApi.UpdateRuntimeAutomation(forceMachineProductionPoll: true);
                    int afterCycles = experimentalApi.GetMachineProductionCycleCountForSmoke("DTMAPI.MineMod");
                    if (afterCycles > beforeCycles)
                    {
                        string state = experimentalApi.GetMachineProductionStateSummaryForSmoke("DTMAPI.MineMod");
                        if (!ContainsIgnoreCase(state, "outputTarget=equipment-storage") ||
                            !ContainsIgnoreCase(state, "storageLineCapacity=4"))
                        {
                            throw new InvalidOperationException("Machine runtime produced but did not place output into Mine-owned 16-slot storage. state={" + state + "}");
                        }

                        string minePlacementEvidenceSummary = CaptureMinePlacementEvidenceForSmoke(transientMine, createSummary);
                        string summary = "mode=" + (mineOnly ? "mine-only" : "all-new-content") + ", oilItemMetadata={" + oilItemMetadataSummary + "}, oilCoalDrop={" + oilCoalDropSummary + "}, mineOfficialJson={" + mineOfficialJsonSummary + "}, equipmentSlots={" + equipmentSlotsSummary + "}, timeSkip={" + timeSkip.Message + "}, minePlacement={" + minePlacementEvidenceSummary + "}, scaleContainment={" + scaleContainmentSummary + "}, mine={" + DescribeEquipmentForSmoke(transientMine) + "}, create={" + createSummary + "}, beforeCycles=" + beforeCycles + ", afterCycles=" + afterCycles + ", state={" + state + "}";
                        runtime.RuntimeMonitor.Log("Smoke exercise NewContentMineProduction OK " + summary);
                        runtime.SetHookStatus("Smoke.NewContentMineProduction", "verified", "ArchiveDataHandle.PassTimeNoControl -> IMachineProductionApi catch-up + equipment IContainer/LinearInventory", summary);
                        return SmokeAttemptResult.Succeeded;
                    }

                    Thread.Sleep(125);
                }

                throw new InvalidOperationException("Machine runtime loop did not catch up after native pass-time for transient dtmapi_mine. beforeCycles=" + beforeCycles + ", timeSkip={" + timeSkip.Message + "}, state={" + experimentalApi.GetMachineProductionStateSummaryForSmoke("DTMAPI.MineMod") + "}");
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke new-content mine production failed.", ex.InnerException.ToString());
                runtime.SetHookStatus("Smoke.NewContent" + newContentStage, "failed", "NewContent smoke", ex.InnerException.GetType().Name + ": " + ex.InnerException.Message);
                return SmokeAttemptResult.Failed;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke new-content mine production failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.NewContent" + newContentStage, "failed", "NewContent smoke", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
            finally
            {
                if (experimentalApi != null)
                {
                    experimentalApi.ForceOilDropForSmoke = false;
                    experimentalApi.ForceMachineProductionDueForSmoke = false;
                }
                if (room != null && transientMine != null)
                    TryRemoveTransientEquipmentForSmoke(room, transientMine);
            }
        }

        private SmokeAttemptResult OpenMineOfficialTechTreeUiForSmoke(Type dolocApi, string mineOfficialJsonSummary)
        {
            string treeId = ExtractMineTechTreeId(mineOfficialJsonSummary);
            string nodeId = "dtmapi_mine";
            MethodInfo? jumpTechTreeNode = FindMethod(dolocApi, "JumpTechTreeNode", 2);
            if (jumpTechTreeNode == null)
                throw new MissingMethodException("DolocAPI.JumpTechTreeNode(string,string) was not found.");

            mineOfficialTechTreeUiTreeId = treeId;
            mineOfficialTechTreeUiNodeId = nodeId;
            mineOfficialTechTreeUiOpenRequested = true;
            mineOfficialTechTreeUiOpenAt = DateTimeOffset.Now;
            runtime.SetHookStatus("Smoke.NewContentMineOfficialTechTreeUi", "pending", "DolocAPI.JumpTechTreeNode + TechTreeUiState", "Opening official tech tree node tree=" + treeId + ", node=" + nodeId + " for visual evidence.");
            runtime.RuntimeMonitor.Log("Mine official tech tree UI opening tree=" + treeId + " node=" + nodeId + " via DolocAPI.JumpTechTreeNode.");
            jumpTechTreeNode.Invoke(null, new object[] { treeId, nodeId });
            return SmokeAttemptResult.Pending;
        }

        private SmokeAttemptResult CaptureMineOfficialTechTreeUiEvidenceForSmoke(Type dolocApi)
        {
            if ((DateTimeOffset.Now - mineOfficialTechTreeUiOpenAt).TotalSeconds < 1.75)
            {
                runtime.SetHookStatus("Smoke.NewContentMineOfficialTechTreeUi", "pending", "DolocAPI.JumpTechTreeNode + TechTreeUiState", "Waiting for official tech tree panel render before screenshot. tree=" + mineOfficialTechTreeUiTreeId + ", node=" + mineOfficialTechTreeUiNodeId + ".");
                return SmokeAttemptResult.Pending;
            }

            try
            {
                string evidenceDir = EnsureNewContentEvidenceDir();
                string screenshotPath = Path.Combine(evidenceDir, "mine-official-tech-tree-dtmapi-mine.png");
                bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
                string closeSummary = CloseMineOfficialTechTreeUiForSmoke(dolocApi);
                mineOfficialTechTreeUiEvidenceCaptured = true;
                mineOfficialTechTreeUiClosedAt = DateTimeOffset.Now;

                string summary = "tree=" + mineOfficialTechTreeUiTreeId +
                    ", node=" + mineOfficialTechTreeUiNodeId +
                    ", screenshot=" + (screenshotRequested ? screenshotPath : "unavailable") +
                    ", close={" + closeSummary + "}";
                File.AppendAllText(Path.Combine(evidenceDir, "mine-official-tech-tree-summary.txt"),
                    "Captured=" + DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture) + Environment.NewLine +
                    "Tree=" + mineOfficialTechTreeUiTreeId + Environment.NewLine +
                    "Node=" + mineOfficialTechTreeUiNodeId + Environment.NewLine +
                    "ScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                    "Screenshot=" + screenshotPath + Environment.NewLine +
                    "Close=" + closeSummary + Environment.NewLine);
                runtime.RuntimeMonitor.Log("Mine official tech tree UI evidence " + (screenshotRequested ? "OK" : "unavailable") + " " + summary + ".");
                runtime.SetHookStatus("Smoke.NewContentMineOfficialTechTreeUi", screenshotRequested ? "verified" : "pending", "DolocAPI.JumpTechTreeNode + TechTreeUiState + UnityEngine.ScreenCapture", summary);
                return SmokeAttemptResult.Pending;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Mine official tech tree UI evidence capture failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.NewContentMineOfficialTechTreeUi", "failed", "DolocAPI.JumpTechTreeNode + TechTreeUiState", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private string CloseMineOfficialTechTreeUiForSmoke(Type dolocApi)
        {
            Type? techTreeUiState = patcher?.ResolveType("DolocTown.TechTreeUiState, Assembly-CSharp");
            if (techTreeUiState == null)
                return "skipped:missing-TechTreeUiState";

            object? userInput = ReadStaticMember(dolocApi, "userInput");
            object? currentState = userInput == null ? null : ReadMember(userInput, "CurrentState");
            if (currentState != null && techTreeUiState.IsAssignableFrom(currentState.GetType()))
            {
                MethodInfo? popState = FindMethod(userInput!.GetType(), "PopState", 0);
                if (popState != null && popState.Invoke(userInput, null) is bool popped && popped)
                    return "popped-current-TechTreeUiState";
            }

            MethodInfo? removeUiState = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(method => method.Name.Equals("RemoveUiState", StringComparison.Ordinal) && method.IsGenericMethodDefinition && method.GetParameters().Length == 0);
            if (removeUiState == null)
                return "skipped:missing-DolocAPI.RemoveUiState";

            removeUiState.MakeGenericMethod(techTreeUiState).Invoke(null, null);
            return "removed-TechTreeUiState";
        }

        private string TryRecoverNormalStateAfterMineTechTreeUiForSmoke(Type dolocApi)
        {
            object? userInput = ReadStaticMember(dolocApi, "userInput");
            object? currentState = userInput == null ? null : ReadMember(userInput, "CurrentState");
            string currentStateName = currentState?.GetType().FullName ?? "null";
            double secondsSinceClose = mineOfficialTechTreeUiClosedAt == default ? -1 : (DateTimeOffset.Now - mineOfficialTechTreeUiClosedAt).TotalSeconds;
            Type? techTreeUiState = patcher?.ResolveType("DolocTown.TechTreeUiState, Assembly-CSharp");
            if (userInput != null && currentState != null && techTreeUiState != null && techTreeUiState.IsAssignableFrom(currentState.GetType()))
            {
                MethodInfo? popState = FindMethod(userInput.GetType(), "PopState", 0);
                if (popState != null && popState.Invoke(userInput, null) is bool popped)
                    return "retry-pop-TechTreeUiState popped=" + popped + ", secondsSinceClose=" + FormatRatio(secondsSinceClose);
            }

            if ((DateTimeOffset.Now - lastMineOfficialTechTreeUiRecoveryLogAt).TotalSeconds >= 5)
            {
                lastMineOfficialTechTreeUiRecoveryLogAt = DateTimeOffset.Now;
                runtime.RuntimeMonitor.Log("Mine official tech tree UI recovery waiting currentState=" + currentStateName + " secondsSinceClose=" + FormatRatio(secondsSinceClose) + ".");
            }

            return "currentState=" + currentStateName + ", secondsSinceClose=" + FormatRatio(secondsSinceClose);
        }

        private static string ExtractMineTechTreeId(string summary)
        {
            const string marker = "tree=";
            int start = summary.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return "industrial_techtree";

            start += marker.Length;
            int end = start;
            while (end < summary.Length && summary[end] != ',' && summary[end] != '}' && !char.IsWhiteSpace(summary[end]))
                end++;

            string tree = summary.Substring(start, end - start).Trim();
            return string.IsNullOrWhiteSpace(tree) ? "industrial_techtree" : tree;
        }

        private string TryExerciseEquipmentSlotsForSmoke()
        {
            if (experimentalApi == null)
                throw new InvalidOperationException("Experimental GameBridge API was not registered.");

            var owner = new ManifestModel
            {
                Name = "DTMAPI More Equipment Slots",
                Author = "DTMAPI",
                Version = DtmApiRuntime.ApiVersion,
                UniqueID = "DTMAPI.MoreEquipmentSlotsMod",
                Type = "CodeMod"
            };

            InventoryGiveResult passiveGive = experimentalApi.GiveItem(CreateSmokeManifest(), "grandmas_button", 1);
            if (!passiveGive.Success)
                throw new InvalidOperationException("Could not give grandmas_button for equipment-slot smoke. " + passiveGive.Message);

            IReadOnlyList<EquipmentSlotInfo> beforeSlots = experimentalApi.GetSlots(owner.UniqueID);
            EquipmentSlotEquipResult passiveEquip = experimentalApi.EquipExtraSlot(owner, string.Empty, "grandmas_button");
            if (!passiveEquip.Success)
                throw new InvalidOperationException("Could not equip DTMAPI extra slot with passive item. " + passiveEquip.Message);

            IReadOnlyList<EquipmentSlotInfo> equippedSlots = experimentalApi.GetSlots(owner.UniqueID);
            string passiveUiSummary = experimentalApi.CaptureEquipmentSlotsUiEvidenceForSmoke("new-content smoke after passive equip");
            EquipmentSlotEquipResult passiveRecover = experimentalApi.UnequipExtraSlot(owner, passiveEquip.SlotId, "new-content smoke passive recovery");
            if (!passiveRecover.Success)
                throw new InvalidOperationException("Could not recover DTMAPI passive extra slot. " + passiveRecover.Message);

            string nativeHatBefore = GetNativeAgentEquipmentItemId("hatItem");
            InventoryGiveResult hatGive = experimentalApi.GiveItem(CreateSmokeManifest(), "straw_hat", 1);
            if (!hatGive.Success)
                throw new InvalidOperationException("Could not give straw_hat for equipment-slot hat smoke. " + hatGive.Message);

            EquipmentSlotEquipResult hatEquip = experimentalApi.EquipExtraSlot(owner, string.Empty, "straw_hat");
            if (!hatEquip.Success)
                throw new InvalidOperationException("Could not equip DTMAPI extra slot with hat item. " + hatEquip.Message);
            string nativeHatAfterEquip = GetNativeAgentEquipmentItemId("hatItem");
            if (!string.Equals(nativeHatBefore, nativeHatAfterEquip, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("DTMAPI extra-slot hat changed the native hat visual slot. before=" + nativeHatBefore + ", after=" + nativeHatAfterEquip);

            IReadOnlyList<EquipmentSlotInfo> hatEquippedSlots = experimentalApi.GetSlots(owner.UniqueID);
            string hatUiSummary = experimentalApi.CaptureEquipmentSlotsUiEvidenceForSmoke("new-content smoke after hat equip");
            EquipmentSlotEquipResult hatRecover = experimentalApi.UnequipExtraSlot(owner, hatEquip.SlotId, "new-content smoke hat recovery");
            if (!hatRecover.Success)
                throw new InvalidOperationException("Could not recover DTMAPI hat extra slot. " + hatRecover.Message);
            string nativeHatAfterRecover = GetNativeAgentEquipmentItemId("hatItem");

            IReadOnlyList<EquipmentSlotInfo> recoveredSlots = experimentalApi.GetSlots(owner.UniqueID);
            string summary = "passiveGive=" + passiveGive.ItemId + " " + passiveGive.BeforeCount + "->" + passiveGive.AfterCount +
                ", beforeSlots=" + beforeSlots.Count +
                ", passiveSlot=" + passiveEquip.SlotId +
                ", passiveBackpack=" + passiveEquip.BeforeBackpackCount + "->" + passiveEquip.AfterBackpackCount +
                ", equippedStored=" + equippedSlots.Count(slot => slot.IsOccupied) +
                ", passiveUi={" + passiveUiSummary + "}" +
                ", passiveRecover=" + passiveRecover.RecoveredCount +
                ", passiveRecoverBackpack=" + passiveRecover.BeforeBackpackCount + "->" + passiveRecover.AfterBackpackCount +
                ", hatGive=" + hatGive.ItemId + " " + hatGive.BeforeCount + "->" + hatGive.AfterCount +
                ", hatSlot=" + hatEquip.SlotId +
                ", hatBackpack=" + hatEquip.BeforeBackpackCount + "->" + hatEquip.AfterBackpackCount +
                ", hatEquippedStored=" + hatEquippedSlots.Count(slot => slot.IsOccupied) +
                ", nativeHat=" + nativeHatBefore + "->" + nativeHatAfterEquip + "->" + nativeHatAfterRecover +
                ", hatUi={" + hatUiSummary + "}" +
                ", hatRecover=" + hatRecover.RecoveredCount +
                ", hatRecoverBackpack=" + hatRecover.BeforeBackpackCount + "->" + hatRecover.AfterBackpackCount +
                ", recoveredStored=" + recoveredSlots.Count(slot => slot.IsOccupied) +
                ", state={" + experimentalApi.GetEquipmentSlotsStateSummaryForSmoke(owner.UniqueID) + "}";
            runtime.RuntimeMonitor.Log("Smoke exercise NewContentEquipmentSlots OK " + summary);
            runtime.SetHookStatus("Smoke.NewContentEquipmentSlots", "verified", "IEquipmentSlotsApi passive+hat EquipExtraSlot/UnequipExtraSlot", summary);
            return summary;
        }

        private string GetNativeAgentEquipmentItemId(string memberName)
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? farmData = archive == null ? null : ReadMember(archive, "farmData");
                object? agentData = farmData == null ? null : ReadMember(farmData, "agentData");
                object? equipment = agentData == null ? null : ReadMember(agentData, "agentEquipment");
                object? item = equipment == null ? null : ReadMember(equipment, memberName);
                return item == null ? "none" : ReadStringMember(item, "name", item.GetType().Name);
            }
            catch
            {
                return "unknown";
            }
        }

        private string TryExerciseMineOfficialJsonForSmoke(Type dolocApi)
        {
            if (experimentalApi == null)
                throw new InvalidOperationException("Experimental GameBridge API was not registered.");

            if (!TryGetNativeItemProto(dolocApi, "dtmapi_mine", out object? itemProto, out string itemProbe))
            {
                string reloadDetail;
                if (!TryReloadOfficialModsAndConfig("Smoke.NewContentMineOfficialJson", "missing dtmapi_mine before Mine metadata smoke", out reloadDetail))
                    throw new InvalidOperationException("dtmapi_mine is not present in the native item table before Mine metadata smoke, and official reload failed. probe={" + itemProbe + "}, reload={" + reloadDetail + "}");

                if (!TryGetNativeItemProto(dolocApi, "dtmapi_mine", out itemProto, out itemProbe))
                    throw new InvalidOperationException("dtmapi_mine is not present in the native item table after official reload. probe={" + itemProbe + "}, reload={" + reloadDetail + "}");
            }

            object? equipmentProto = QueryEquipmentProtoForSmoke(dolocApi, "dtmapi_mine");
            object? wellProto = QueryEquipmentProtoForSmoke(dolocApi, "well");
            if (!TryGetNativeRecipeProto(dolocApi, "dtmapi_mine", out object? recipeProto, out string recipeProbe))
                throw new InvalidOperationException("dtmapi_mine recipe is not present in the native recipe table. probe={" + recipeProbe + "}");
            object? generatedMineItem = GenerateItemForSmoke(dolocApi, "dtmapi_mine");
            string generatedMineItemType = generatedMineItem == null ? "null" : generatedMineItem.GetType().FullName ?? generatedMineItem.GetType().Name;
            bool generatedMineIsItemEquipment = generatedMineItem != null && IsTypeOrBase(generatedMineItem.GetType(), "DolocTown.ItemEquipment");

            object? recipeGroup = GetDolocConfigDataMapValueForSmoke("TbRecipeGroup", "equipment_workbench");
            IContentItemInfo? sourceInfo = runtime.GetIndexedContentItem("dtmapi_mine");
            experimentalApi.UpdateRuntimeAutomation();
            MachineProductionState state = ((IMachineProductionApi)experimentalApi).GetState("DTMAPI.MineMod");
            object equipmentObject = equipmentProto ?? new object();
            object wellObject = wellProto ?? new object();
            object recipeObject = recipeProto ?? new object();

            string title = FirstNonEmpty(ReadAnyStringMember(itemProto!, string.Empty, "Title", "title"), "dtmapi_mine");
            string description = ReadAnyStringMember(itemProto!, string.Empty, "DescriptionBasic", "description_basic");
            string subType = ReadAnyStringMember(itemProto!, string.Empty, "SubType", "sub_type");
            string itemFunction = ReadAnyMember(itemProto!, "Function", "function")?.GetType().Name ?? "none";
            int overlay = ReadAnyIntMember(itemProto!, -1, "Overlay", "overlay");
            int buyingPrice = ReadAnyIntMember(itemProto!, -1, "BuyingPrice", "buying_price");
            bool viewable = ReadAnyBoolMember(itemProto!, false, "Viewable", "viewable");
            string indexedIcon = sourceInfo?.IconAssetKey ?? string.Empty;

            (int mineWidth, int mineHeight) = ReadVector2IntForSmoke(ReadAnyMember(equipmentObject, "CoverSize", "cover_size"));
            (int wellWidth, int wellHeight) = ReadVector2IntForSmoke(ReadAnyMember(wellObject, "CoverSize", "cover_size"));
            string sceneAsset = ReadAnyStringMember(ReadAnyMember(equipmentObject, "SceneAsset", "scene_asset") ?? new object(), string.Empty, "AssetUrl", "url");
            object? equipmentFunctionObject = ReadAnyMember(equipmentObject, "Function", "function");
            string equipmentFunction = equipmentFunctionObject?.GetType().Name ?? "none";
            int caseTotalCapacity = ReadAnyIntMember(equipmentFunctionObject ?? new object(), -1, "TotalCapacity", "total_capacity");
            int caseLineCapacity = ReadAnyIntMember(equipmentFunctionObject ?? new object(), -1, "LineCapacity", "line_capacity");
            object? electronicComponentObject = ReadAnyMember(equipmentObject, "ElectronicComponent", "electronic_component");
            string electronicComponent = electronicComponentObject?.GetType().Name ?? "none";
            int electronicThreshold = ReadAnyIntMember(electronicComponentObject ?? new object(), -1, "Threshold", "threshold");
            string outputItem = ReadAnyStringMember(ReadAnyMember(recipeObject, "OutputItem", "output_item") ?? new object(), string.Empty, "itemName", "ItemName", "item_name");
            int techPoint = ReadAnyIntMember(recipeObject, -1, "TechPoint", "tech_point");
            bool defaultUnlock = ReadAnyBoolMember(recipeObject, false, "DefaultUnlock", "default_unlock");
            bool groupIncludesRecipe = recipeGroup != null && ReadStringValues(ReadAnyMember(recipeGroup, "RecipeIds", "recipe_ids")).Any(v => v.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase));
            string inputs = DescribeCountItemsForSmoke(ReadAnyMember(recipeObject, "InputItems", "input_items"));
            string outputRules = string.Join("|", experimentalApi.GetMachines("DTMAPI.MineMod")
                .SelectMany(machine => machine.OutputRules ?? Array.Empty<MachineOutputRule>())
                .Select(rule => rule.ItemId + ":" + rule.Weight.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + ":" + rule.Source));
            string nativeTechTreeSummary = state.NativeTechTreeSummary ?? string.Empty;

            var failures = new List<string>();
            if (equipmentProto == null)
                failures.Add("missing-equipment-proto");
            if (wellProto == null)
                failures.Add("missing-well-proto");
            if (sourceInfo == null || !sourceInfo.SourceKind.Equals("DTMAPI", StringComparison.OrdinalIgnoreCase))
                failures.Add("missing-dtmapi-source:" + (sourceInfo == null ? "none" : sourceInfo.SourceKind));
            if (!ContainsIgnoreCase(title, "矿井") && !ContainsIgnoreCase(title, "Mine"))
                failures.Add("missing-title:" + title);
            if (!ContainsIgnoreCase(description, "水井") && !ContainsIgnoreCase(description, "well"))
                failures.Add("missing-well-description");
            if (!subType.Equals("equipment_ornament", StringComparison.OrdinalIgnoreCase))
                failures.Add("unexpected-subtype:" + subType);
            if (!itemFunction.Equals("ItemFunctionEquipment", StringComparison.OrdinalIgnoreCase))
                failures.Add("unexpected-item-function:" + itemFunction);
            if (!generatedMineIsItemEquipment)
                failures.Add("generated-item-not-ItemEquipment:" + generatedMineItemType);
            if (overlay <= 0 || buyingPrice <= 0 || !viewable)
                failures.Add("item-not-viewable-buyable-stackable:overlay=" + overlay + ", buy=" + buyingPrice + ", viewable=" + viewable);
            if (!ContainsIgnoreCase(indexedIcon, "icon_item_well"))
                failures.Add("missing-indexed-icon:" + indexedIcon);
            if (mineWidth != 8 || mineHeight != 6)
                failures.Add("mine-cover-size=" + mineWidth + "x" + mineHeight);
            if (wellWidth <= 0 || wellHeight <= 0 || mineWidth != wellWidth * 2 || mineHeight != wellHeight * 2)
                failures.Add("not-double-well-cover:mine=" + mineWidth + "x" + mineHeight + ", well=" + wellWidth + "x" + wellHeight);
            if (!ContainsIgnoreCase(sceneAsset, "sprite_equipment_well"))
                failures.Add("scene-asset=" + sceneAsset);
            if (!equipmentFunction.Equals("EquipmentFuncCase", StringComparison.OrdinalIgnoreCase))
                failures.Add("unexpected-equipment-function:" + equipmentFunction);
            if (caseTotalCapacity != 16 || caseLineCapacity != 4)
                failures.Add("unexpected-case-storage:" + caseTotalCapacity + "/" + caseLineCapacity);
            if (!electronicComponent.Equals("EComProtoAppliance", StringComparison.OrdinalIgnoreCase) || electronicThreshold != 10)
                failures.Add("unexpected-electronic-component:" + electronicComponent + "/" + electronicThreshold);
            if (!outputItem.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase))
                failures.Add("recipe-output=" + outputItem);
            bool hasOilRecipe = CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "crude_oil", 10) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "metal_framework", 10) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "engine_core", 5) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "steel_ingot", 20);
            bool hasFallbackRecipe = CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "metal_framework", 15) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "engine_core", 10) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "steel_ingot", 20) &&
                CountItemsContain(ReadAnyMember(recipeObject, "InputItems", "input_items"), "coal", 100);
            if (!hasOilRecipe && !hasFallbackRecipe)
                failures.Add("recipe-inputs=" + inputs);
            if (techPoint != 1 || defaultUnlock)
                failures.Add("recipe-tech-default:tech=" + techPoint + ", defaultUnlock=" + defaultUnlock);
            if (!groupIncludesRecipe)
                failures.Add("equipment_workbench-missing-dtmapi_mine");
            if (!ContainsIgnoreCase(nativeTechTreeSummary, "node=dtmapi_mine") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "parent=alloy_material") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "rightOfParent=True") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "aboveCommander=True") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "unlockEntries=recipe-only") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "equipmentEntries=0") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "recipeEntries=1") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, "costValues=") ||
                !ContainsIgnoreCase(nativeTechTreeSummary, ":1") ||
                ContainsIgnoreCase(nativeTechTreeSummary, "pending") ||
                ContainsIgnoreCase(nativeTechTreeSummary, "failed"))
            {
                failures.Add("native-tech-route=" + nativeTechTreeSummary);
            }
            if (!state.MachineId.Equals("dtmapi.mine", StringComparison.OrdinalIgnoreCase) ||
                !state.EquipmentId.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase) ||
                !state.RecipeGroupId.Equals("equipment_workbench", StringComparison.OrdinalIgnoreCase) ||
                state.VisualScale < 1.99 ||
                !state.AllowFuelMode ||
                !state.AllowElectricMode ||
                !state.DefaultMode.Equals("electric", StringComparison.OrdinalIgnoreCase) ||
                state.FuelCapacity <= 0 ||
                state.FuelOnlyFuelCostPerCycle <= state.ElectricModeFuelCostPerCycle ||
                state.ElectricModeFuelCostPerCycle <= 0 ||
                state.ElectricModePowerCostPerCycle != 10 ||
                (ContainsIgnoreCase(outputRules, "DTMAPI.OilMod") && !ContainsIgnoreCase(outputRules, "crude_oil")))
                failures.Add("machine-api-state=machine:" + state.MachineId + ", equipment:" + state.EquipmentId + ", group:" + state.RecipeGroupId + ", visualScale:" + state.VisualScale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + ", hybrid=" + (state.AllowFuelMode && state.AllowElectricMode) + ", defaultMode=" + state.DefaultMode + ", fuelCapacity=" + state.FuelCapacity + ", fuelOnlyCost=" + state.FuelOnlyFuelCostPerCycle + ", electricFuelCost=" + state.ElectricModeFuelCostPerCycle + ", powerCost=" + state.ElectricModePowerCostPerCycle + ", outputs=" + outputRules);

            string summary = "item=" + title +
                ", source=" + (sourceInfo == null ? "none" : sourceInfo.SourceKind + "/" + sourceInfo.SourceId + "/" + sourceInfo.SourceModTitle) +
                ", icon=" + indexedIcon +
                ", itemFunction=" + itemFunction +
                ", subtype=" + subType +
                ", cover=" + mineWidth + "x" + mineHeight +
                ", baseWellCover=" + wellWidth + "x" + wellHeight +
                ", sceneAsset=" + sceneAsset +
                ", equipmentFunction=" + equipmentFunction +
                ", caseStorage=" + caseTotalCapacity + "/" + caseLineCapacity +
                ", electronicComponent=" + electronicComponent + "/" + electronicThreshold +
                ", generatedItemType=" + generatedMineItemType +
                ", recipeOutput=" + outputItem +
                ", recipeInputs=" + inputs +
                ", techPoint=" + techPoint +
                ", defaultUnlock=" + defaultUnlock +
                ", recipeGroup=equipment_workbench includes=" + groupIncludesRecipe +
                ", nativeTech={" + nativeTechTreeSummary + "}" +
                ", machineApi=machine:" + state.MachineId + "/item:" + state.ItemId + "/equipment:" + state.EquipmentId + "/recipe:" + state.RecipeId + "/group:" + state.RecipeGroupId + "/visualScale:" + state.VisualScale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "/hybrid:" + (state.AllowFuelMode && state.AllowElectricMode) + "/defaultMode:" + state.DefaultMode + "/fuelCapacity:" + state.FuelCapacity + "/fuelOnlyCost:" + state.FuelOnlyFuelCostPerCycle + "/electricFuelCost:" + state.ElectricModeFuelCostPerCycle + "/cycleMinutes:" + state.CycleMinutes + "/powerCost:" + state.ElectricModePowerCostPerCycle +
                ", outputRules=" + outputRules +
                ", probes=item{" + itemProbe + "}, recipe{" + recipeProbe + "}";
            if (failures.Count > 0)
                throw new InvalidOperationException("Mine official JSON/API smoke failed: " + string.Join(", ", failures) + ". " + summary);

            runtime.RuntimeMonitor.Log("Smoke exercise NewContentMineOfficialJson OK " + summary);
            runtime.SetHookStatus("Smoke.NewContentMineOfficialJson", "verified", "DolocConfig item/equipment/recipe/group + IMachineProductionApi state", summary);
            return summary;
        }

        private string TryExerciseOilItemMetadataForSmoke(Type dolocApi)
        {
            if (experimentalApi == null)
                throw new InvalidOperationException("Experimental GameBridge API was not registered.");

            if (!TryGetNativeItemProto(dolocApi, "crude_oil", out object? proto, out string nativeProbe))
            {
                string reloadDetail;
                if (!TryReloadOfficialModsAndConfig("Smoke.NewContentOilItemMetadata", "missing crude_oil before Oil metadata smoke", out reloadDetail))
                    throw new InvalidOperationException("crude_oil is not present in the native item table before Oil metadata smoke, and official reload failed. probe={" + nativeProbe + "}, reload={" + reloadDetail + "}");

                if (!TryGetNativeItemProto(dolocApi, "crude_oil", out proto, out nativeProbe))
                    throw new InvalidOperationException("crude_oil is not present in the native item table after official reload. probe={" + nativeProbe + "}, reload={" + reloadDetail + "}");
            }

            if (proto == null)
                throw new InvalidOperationException("crude_oil native item query returned no proto. probe={" + nativeProbe + "}");

            InventoryDebugPage page = experimentalApi.GetItems(new InventoryDebugQuery
            {
                SearchText = "crude_oil",
                IncludeUnavailable = true,
                PageSize = 50
            });
            InventoryDebugItem? item = page.Items.FirstOrDefault(i => i.Id.Equals("crude_oil", StringComparison.OrdinalIgnoreCase));
            if (item == null)
                throw new InvalidOperationException("IInventoryDebugApi did not return crude_oil. status=" + page.Status + ", total=" + page.TotalItems + ".");

            InventoryDebugPage sourcePage = experimentalApi.GetItems(new InventoryDebugQuery
            {
                SourceId = item.SourceId,
                IncludeUnavailable = true,
                PageSize = 50
            });
            IContentItemInfo? sourceInfo = runtime.GetIndexedContentItem("crude_oil");
            bool sourceFilterIncludesOil = sourcePage.Items.Any(i => i.Id.Equals("crude_oil", StringComparison.OrdinalIgnoreCase));
            InventoryDebugSourceGroup? sourceGroup = page.Sources.FirstOrDefault(g => g.Id.Equals(item.SourceId, StringComparison.OrdinalIgnoreCase));
            string effectiveCategory = FirstNonEmpty(item.SubCategory, item.Category);
            bool categoryListed = !string.IsNullOrWhiteSpace(effectiveCategory) &&
                page.Categories.Any(c => c.Equals(effectiveCategory, StringComparison.OrdinalIgnoreCase));
            string[] tags = item.Tags?.ToArray() ?? Array.Empty<string>();
            string[] nativeSources = ReadStringValues(ReadAnyMember(proto, "Source", "source")).ToArray();

            string title = FirstNonEmpty(ReadAnyStringMember(proto, string.Empty, "Title", "title"), item.DisplayName, item.ChineseName, item.Id);
            string description = ReadAnyStringMember(proto, string.Empty, "DescriptionBasic", "description_basic");
            bool salable = ReadAnyBoolMember(proto, false, "Salable", "salable");
            bool viewable = ReadAnyBoolMember(proto, false, "Viewable", "viewable");
            int sellingPrice = ReadAnyIntMember(proto, -1, "SellingPrice", "selling_price");
            int buyingPrice = ReadAnyIntMember(proto, -1, "BuyingPrice", "buying_price");
            int electricEnergy = ReadAnyIntMember(proto, -1, "ElectricEnergy", "electric_energy");
            int overlay = ReadAnyIntMember(proto, -1, "Overlay", "overlay");
            string nativeIcon = ReadAnyMember(proto, "UiSpriteAsset", "ui_sprite_asset")?.ToString() ?? string.Empty;
            string indexedIcon = sourceInfo?.IconAssetKey ?? string.Empty;
            string nativeSubType = ReadAnyStringMember(proto, string.Empty, "SubType", "sub_type");
            string functionType = ReadAnyMember(proto, "Function", "function")?.GetType().Name ?? "none";
            int highestBaseFuel = FindHighestNativeFuelEnergyExcept("crude_oil", out string highestBaseFuelItem);

            var failures = new List<string>();
            if (!item.RuntimeLoaded)
                failures.Add("not-runtime-loaded");
            if (!item.CanSpawn || !item.CanGive)
                failures.Add("not-giveable:" + item.CannotGiveReason);
            if (!item.IsModItem)
                failures.Add("not-marked-mod-item");
            if (!item.SourceKind.Equals("DTMAPI", StringComparison.OrdinalIgnoreCase))
                failures.Add("source-kind=" + item.SourceKind);
            if (!item.SourceId.Equals("Local.DTMAPI_Oil", StringComparison.OrdinalIgnoreCase))
                failures.Add("source-id=" + item.SourceId);
            if (sourceGroup == null || sourceGroup.Count <= 0)
                failures.Add("missing-source-group");
            if (!sourceFilterIncludesOil)
                failures.Add("source-filter-misses-oil");
            if (string.IsNullOrWhiteSpace(effectiveCategory))
                failures.Add("missing-category");
            if (!categoryListed)
                failures.Add("category-not-listed:" + effectiveCategory);
            if (!ContainsAny(tags, "material_ore", "mining", "processing") &&
                !ContainsAny(nativeSources, "material_ore", "mining", "processing") &&
                !ContainsIgnoreCase(item.SearchText, "material_ore"))
                failures.Add("missing-mining-category-tags");
            if (!item.HasIcon || string.IsNullOrWhiteSpace(nativeIcon) || !ContainsIgnoreCase(indexedIcon, "icon_item_coal"))
                failures.Add("missing-icon:hasIcon=" + item.HasIcon + ", item=" + item.IconAssetKey + ", native=" + nativeIcon + ", indexed=" + indexedIcon);
            if (!ContainsIgnoreCase(title, "原油") && !ContainsIgnoreCase(title, "石油") && !ContainsIgnoreCase(title, "Oil") && !ContainsIgnoreCase(item.DisplayName, "原油") && !ContainsIgnoreCase(item.DisplayName, "Oil"))
                failures.Add("missing-localized-title:" + title + "/" + item.DisplayName);
            if (!ContainsIgnoreCase(description, "燃料") && !ContainsIgnoreCase(description, "fuel"))
                failures.Add("missing-fuel-description");
            if (!salable || sellingPrice <= 0)
                failures.Add("not-salable:sale=" + salable + ", price=" + sellingPrice);
            if (buyingPrice <= 0)
                failures.Add("missing-buy-price:" + buyingPrice);
            if (!viewable)
                failures.Add("not-viewable");
            if (overlay <= 0)
                failures.Add("not-stackable-overlay:" + overlay);
            if (electricEnergy <= highestBaseFuel)
                failures.Add("fuel-not-above-base-highest:" + electricEnergy + "<=" + highestBaseFuel + "(" + highestBaseFuelItem + ")");

            string summary = "id=" + item.Id +
                ", display=" + FirstNonEmpty(item.DisplayName, title, item.Id) +
                ", sourceKind=" + item.SourceKind +
                ", sourceId=" + item.SourceId +
                ", sourceTitle=" + item.SourceModTitle +
                ", sourceGroup=" + (sourceGroup == null ? "missing" : sourceGroup.DisplayName + "/" + sourceGroup.Count) +
                ", sourceFilterIncludesOil=" + sourceFilterIncludesOil +
                ", category=" + effectiveCategory +
                ", categoryListed=" + categoryListed +
                ", tags=" + (tags.Length == 0 ? "none" : string.Join("|", tags)) +
                ", nativeSources=" + (nativeSources.Length == 0 ? "none" : string.Join("|", nativeSources)) +
                ", salable=" + salable +
                ", sellingPrice=" + sellingPrice +
                ", buyingPrice=" + buyingPrice +
                ", fuelEnergy=" + electricEnergy +
                ", baseHighestFuel=" + highestBaseFuelItem + ":" + highestBaseFuel +
                ", icon=" + FirstNonEmpty(item.IconAssetKey, nativeIcon) +
                ", indexedIcon=" + indexedIcon +
                ", title=" + title +
                ", subType=" + nativeSubType +
                ", function=" + functionType +
                ", nativeProbe={" + nativeProbe + "}";
            if (failures.Count > 0)
                throw new InvalidOperationException("Oil item metadata smoke failed: " + string.Join(", ", failures) + ". " + summary);

            runtime.RuntimeMonitor.Log("Smoke exercise NewContentOilItemMetadata OK " + summary);
            runtime.SetHookStatus("Smoke.NewContentOilItemMetadata", "verified", "IInventoryDebugApi.GetItems + DolocAPI.QueryItemProto", summary);
            return summary;
        }

        private string TryExerciseOilCoalDropForSmoke(Type dolocApi, object room)
        {
            if (experimentalApi == null)
                throw new InvalidOperationException("Experimental GameBridge API was not registered.");

            if (!TryQueryNativeItemProto(dolocApi, "crude_oil", out string oilProbe))
            {
                string reloadDetail;
                if (!TryReloadOfficialModsAndConfig("Smoke.NewContentOilCoalDrop", "missing crude_oil before OilMod smoke", out reloadDetail))
                    throw new InvalidOperationException("crude_oil is not present in the native item table before OilMod smoke, and official reload failed. probe={" + oilProbe + "}, reload={" + reloadDetail + "}");

                if (!TryQueryNativeItemProto(dolocApi, "crude_oil", out oilProbe))
                    throw new InvalidOperationException("crude_oil is not present in the native item table after official reload. probe={" + oilProbe + "}, reload={" + reloadDetail + "}");
            }

            Type? toolColliderType = patcher?.ResolveType("DolocTown.ToolCollider, Assembly-CSharp");
            Type? resourceRendererType = patcher?.ResolveType("DolocTown.DungeonResourceRenderer, Assembly-CSharp");
            if (toolColliderType == null || resourceRendererType == null)
                throw new MissingMemberException("ToolCollider or DungeonResourceRenderer was not visible.");

            if (!TryEnsureCoalResourceForOilSmoke(room, resourceRendererType, out string setupSummary))
                throw new InvalidOperationException("Could not prepare a coal resource for OilMod smoke. " + setupSummary);

            MethodInfo? handleTools = FindMethod(toolColliderType, "HandleTools", 1);
            MethodInfo? resetTool = FindMethod(toolColliderType, "ResetTool", 1);
            MethodInfo? resetChopCounter = FindMethod(toolColliderType, "ResetChopCounter", 1);
            if (handleTools == null || resetTool == null)
                throw new MissingMethodException("ToolCollider.HandleTools/ResetTool path was not found for OilMod smoke.");

            object? toolCollider = FindToolColliderForSmoke(dolocApi, toolColliderType);
            if (toolCollider == null)
                throw new InvalidOperationException("No ToolCollider instance was available for OilMod smoke.");

            object? tool = GenerateItemForSmoke(dolocApi, "steel_pickaxe") ?? GenerateItemForSmoke(dolocApi, "iron_pickaxe") ?? GenerateItemForSmoke(dolocApi, "old_pickaxe");
            if (tool == null)
                throw new InvalidOperationException("Could not generate a pickaxe for OilMod smoke.");

            string toolName = ReadStringMember(tool, "name", tool.GetType().Name);
            int toolDamage = ReadIntMember(tool, "ChopNumber", 0);
            int beforeDrops = experimentalApi.OilMiningDropCount;
            int rendererCount = 0;
            int coalCount = 0;
            int invokedCount = 0;
            List<string> samples = new List<string>();
            List<string> attempts = new List<string>();

            try
            {
                experimentalApi.ForceOilDropForSmoke = true;
                foreach (object renderer in FindUnityObjects(resourceRendererType))
                {
                    rendererCount++;
                    object? resource = ReadMember(renderer, "DungeonResource");
                    if (resource == null || IsRemoved(resource))
                        continue;

                    string resourceName = ReadStringMember(resource, "ResourceName", resource.GetType().Name);
                    string resourceClass = ReadResourceClass(resource);
                    int healthBefore = ReadIntMember(resource, "currentHealth", 0);
                    if (samples.Count < 8)
                        samples.Add(resourceName + "/class=" + resourceClass + "/health=" + healthBefore);
                    if (!IsCoalResourceNameForSmoke(resourceName) || healthBefore <= 0)
                        continue;

                    coalCount++;
                    object? collider = ReadMember(renderer, "PolygonCollider");
                    if (collider == null)
                        continue;

                    int seededHealth = Math.Max(1, toolDamage > 0 ? Math.Min(healthBefore, toolDamage) : 1);
                    WriteIntMember(resource, "currentHealth", seededHealth);
                    resetTool.Invoke(toolCollider, new object[] { tool });
                    resetChopCounter?.Invoke(toolCollider, new object[] { 999 });
                    handleTools.Invoke(toolCollider, new object[] { collider });
                    invokedCount++;

                    int healthAfter = ReadIntMember(resource, "currentHealth", 0);
                    bool removedAfter = IsRemoved(resource);
                    if (attempts.Count < 8)
                        attempts.Add(resourceName + "/tool=" + toolName + "/toolDamage=" + toolDamage + "/health=" + healthBefore + "->" + healthAfter + "/seeded=" + seededHealth + "/removed=" + removedAfter + "/drops=" + beforeDrops + "->" + experimentalApi.OilMiningDropCount + "/bridge={" + experimentalApi.LastOilMiningDropSummary + "}");
                    if (experimentalApi.OilMiningDropCount > beforeDrops)
                    {
                        string summary = "setup={" + setupSummary + "}, resource=" + resourceName + ", class=" + resourceClass + ", tool=" + toolName + ", toolDamage=" + toolDamage + ", healthBefore=" + healthBefore + ", seededHealth=" + seededHealth + ", healthAfter=" + healthAfter + ", removed=" + removedAfter + ", beforeDrops=" + beforeDrops + ", afterDrops=" + experimentalApi.OilMiningDropCount + ", bridge={" + experimentalApi.LastOilMiningDropSummary + "}";
                        runtime.RuntimeMonitor.Log("Smoke exercise NewContentOilCoalDrop OK " + summary);
                        runtime.SetHookStatus("Smoke.NewContentOilCoalDrop", "verified", "ToolCollider.HandleTools private path + OilMod.MiningDrop", summary);
                        return summary;
                    }
                }
            }
            finally
            {
                experimentalApi.ForceOilDropForSmoke = false;
            }

            throw new InvalidOperationException("No coal resource produced crude_oil during OilMod smoke. renderers=" + rendererCount + ", coalResources=" + coalCount + ", invoked=" + invokedCount + ", beforeDrops=" + beforeDrops + ", afterDrops=" + experimentalApi.OilMiningDropCount + ", setup={" + setupSummary + "}, attempts=" + (attempts.Count == 0 ? "none" : string.Join(" ; ", attempts)) + ", samples=" + string.Join(" ; ", samples));
        }

        private static bool TryQueryNativeItemProto(Type dolocApi, string itemId, out string detail)
        {
            return TryGetNativeItemProto(dolocApi, itemId, out _, out detail);
        }

        private static bool TryGetNativeRecipeProto(Type dolocApi, string recipeId, out object? proto, out string detail)
        {
            detail = string.Empty;
            proto = null;
            try
            {
                MethodInfo? queryRecipeProto = dolocApi.GetMethod("QueryRecipeProto", BindingFlags.Public | BindingFlags.Static);
                if (queryRecipeProto == null)
                {
                    detail = "DolocAPI.QueryRecipeProto was not found.";
                    return false;
                }

                object?[] args = new object?[] { recipeId, null };
                bool found = queryRecipeProto.Invoke(null, args) is bool ok && ok && args[1] != null;
                proto = found ? args[1] : null;
                detail = found ? "found " + recipeId + " in DolocConfig.Tables.TbRecipe." : "missing " + recipeId + " in DolocConfig.Tables.TbRecipe.";
                return found;
            }
            catch (Exception ex)
            {
                Exception root = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                detail = root.GetType().Name + ": " + root.Message;
                return false;
            }
        }

        private static bool TryGetNativeItemProto(Type dolocApi, string itemId, out object? proto, out string detail)
        {
            detail = string.Empty;
            proto = null;
            try
            {
                MethodInfo? queryItemProto = dolocApi.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
                if (queryItemProto == null)
                {
                    detail = "DolocAPI.QueryItemProto was not found.";
                    return false;
                }

                object?[] args = new object?[] { itemId, null };
                bool found = queryItemProto.Invoke(null, args) is bool ok && ok && args[1] != null;
                proto = found ? args[1] : null;
                detail = found ? "found " + itemId + " in DolocConfig.Tables.TbItem." : "missing " + itemId + " in DolocConfig.Tables.TbItem.";
                return found;
            }
            catch (Exception ex)
            {
                Exception root = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                detail = root.GetType().Name + ": " + root.Message;
                return false;
            }
        }

        private int FindHighestNativeFuelEnergyExcept(string excludedItemId, out string itemId)
        {
            itemId = string.Empty;
            int highest = 0;
            HashSet<string> indexedContentIds = new HashSet<string>(
                runtime.GetIndexedContentItems()
                    .Select(i => i.ItemId)
                    .Where(id => !string.IsNullOrWhiteSpace(id)),
                StringComparer.OrdinalIgnoreCase);
            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocConfig = patcher.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? tbItem = tables == null ? null : ReadMember(tables, "TbItem");
            object? dataList = tbItem == null ? null : ReadMember(tbItem, "DataList");
            if (!(dataList is IEnumerable enumerable))
                return highest;

            foreach (object proto in enumerable)
            {
                string id = ReadAnyStringMember(proto, string.Empty, "Id", "id");
                if (string.IsNullOrWhiteSpace(id) || id.Equals(excludedItemId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (indexedContentIds.Contains(id))
                    continue;

                int energy = ReadAnyIntMember(proto, 0, "ElectricEnergy", "electric_energy");
                if (energy > highest)
                {
                    highest = energy;
                    itemId = id;
                }
            }

            return highest;
        }

        private object? GetDolocConfigDataMapValueForSmoke(string tableName, string id)
        {
            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocConfig = patcher.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig == null ? null : ReadStaticMember(dolocConfig, "Tables");
            object? table = tables == null ? null : ReadMember(tables, tableName);
            object? dataMap = table == null ? null : ReadMember(table, "DataMap");
            foreach (object entry in EnumerateObjects(dataMap))
            {
                object? key = ReadMember(entry, "Key");
                if (key == null || !id.Equals(key.ToString(), StringComparison.OrdinalIgnoreCase))
                    continue;
                return ReadMember(entry, "Value");
            }

            return null;
        }

        private static (int x, int y) ReadVector2IntForSmoke(object? vector)
        {
            if (vector == null)
                return (0, 0);
            return (ReadIntMember(vector, "x", 0), ReadIntMember(vector, "y", 0));
        }

        private static string DescribeCountItemsForSmoke(object? items)
        {
            List<string> parts = new List<string>();
            foreach (object item in EnumerateObjects(items))
            {
                string itemId = ReadAnyStringMember(item, string.Empty, "itemName", "ItemName", "item_name");
                int count = ReadAnyIntMember(item, 0, "itemCount", "ItemCount", "item_count");
                if (!string.IsNullOrWhiteSpace(itemId))
                    parts.Add(itemId + "x" + count);
            }

            return parts.Count == 0 ? "none" : string.Join("|", parts);
        }

        private static bool CountItemsContain(object? items, string itemId, int minimumCount)
        {
            foreach (object item in EnumerateObjects(items))
            {
                string currentId = ReadAnyStringMember(item, string.Empty, "itemName", "ItemName", "item_name");
                int count = ReadAnyIntMember(item, 0, "itemCount", "ItemCount", "item_count");
                if (currentId.Equals(itemId, StringComparison.OrdinalIgnoreCase) && count >= minimumCount)
                    return true;
            }

            return false;
        }

        private static IEnumerable<object> EnumerateObjects(object? value)
        {
            if (value is IEnumerable enumerable && !(value is string))
            {
                foreach (object? item in enumerable)
                {
                    if (item != null)
                        yield return item;
                }
            }
        }

        private bool TryEnsureCoalResourceForOilSmoke(object room, Type resourceRendererType, out string summary)
        {
            TryRenderAllResourcesForSmoke(room);
            int existing = CountRenderedCoalResources(FindUnityObjects(resourceRendererType), out string existingSamples);
            if (existing > 0)
            {
                summary = "existingCoalResources=" + existing + ", samples=" + existingSamples;
                return true;
            }

            if (TryCreateTransientOneActionResourceForSmoke(room, "Ore", "coal", out string createSummary))
            {
                TryRenderAllResourcesForSmoke(room);
                int created = CountRenderedCoalResources(FindUnityObjects(resourceRendererType), out string createdSamples);
                summary = "createdCoalResources=" + created + ", create={" + createSummary + "}, samples=" + createdSamples;
                return created > 0;
            }

            summary = "existingCoalResources=0, samples=" + existingSamples + ", create={" + createSummary + "}";
            return false;
        }

        private static int CountRenderedCoalResources(IEnumerable<object> renderers, out string samplesText)
        {
            int count = 0;
            List<string> samples = new List<string>();
            foreach (object renderer in renderers)
            {
                object? resource = ReadMember(renderer, "DungeonResource");
                if (resource == null || IsRemoved(resource))
                    continue;
                string resourceName = ReadStringMember(resource, "ResourceName", resource.GetType().Name);
                string resourceClass = ReadResourceClass(resource);
                int health = ReadIntMember(resource, "currentHealth", 0);
                if (samples.Count < 8)
                    samples.Add(resourceName + "/class=" + resourceClass + "/health=" + health);
                if (IsCoalResourceNameForSmoke(resourceName))
                    count++;
            }

            samplesText = samples.Count == 0 ? "none" : string.Join(" ; ", samples);
            return count;
        }

        private void LogAutoFishingPending(string message)
        {
            if ((DateTimeOffset.Now - lastAutoFishingReadinessLog).TotalSeconds < 5)
                return;
            lastAutoFishingReadinessLog = DateTimeOffset.Now;
            runtime.RuntimeMonitor.Log("Smoke auto-fishing waiting: " + message);
            runtime.SetHookStatus("Smoke.AutoFishingPhase", "pending", "AgentStateFishingWait.OnPlay", message);
        }

        private SmokeAttemptResult TryExerciseActionSpeedToolForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental GameBridge API was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? agentStateToolType = patcher.ResolveType("DolocTown.AgentStateTool, Assembly-CSharp");
                if (dolocApi == null || agentStateToolType == null)
                    throw new MissingMemberException("DolocAPI or AgentStateTool was not visible.");

                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    LogActionSpeedPending("Waiting for NormalGameState before action-speed tool smoke. context=" + runtime.UI.InputContext + ".");
                    return SmokeAttemptResult.Pending;
                }

                if (!experimentalApi.TryGetConfiguredActionSpeedOwner(out string ownerId))
                    throw new InvalidOperationException("ActionSpeed policy was not configured/enabled for tool animation. Is the official ActionSpeed package enabled and smoke config written?");

                object? agent = dolocApi.GetProperty("agent", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                object? stateManager = agent == null ? null : ReadMember(agent, "StateManager");
                object? tool = GenerateActionSpeedToolForSmoke(dolocApi);
                if (agent == null || stateManager == null || tool == null)
                    throw new MissingMemberException("DolocAPI.agent, AgentStateManager, or generated tool was not available.");

                MethodInfo? getState = stateManager.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "GetState" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
                MethodInfo? onEnter = FindMethod(agentStateToolType, "OnEnter", 0);
                MethodInfo? onExit = FindMethod(agentStateToolType, "OnExit", 0);
                if (getState == null || onEnter == null || onExit == null)
                    throw new MissingMethodException("AgentStateManager.GetState<T>() or AgentStateTool.OnEnter/OnExit was not found.");

                object? state = getState.MakeGenericMethod(agentStateToolType).Invoke(stateManager, null);
                if (state == null || !WriteObjectMember(state, "tool", tool))
                    throw new MissingMemberException("Could not seed AgentStateTool.tool for smoke.");

                int before = experimentalApi.ActionSpeedApplicationCount;
                onEnter.Invoke(state, null);
                int after = experimentalApi.ActionSpeedApplicationCount;
                string summary = experimentalApi.LastActionSpeedApplicationSummary;
                onExit.Invoke(state, null);
                experimentalApi.RestoreActionSpeed("smoke action-speed tool cleanup");

                if (after <= before)
                    throw new InvalidOperationException("AgentStateTool.OnEnter ran but ActionSpeed did not apply. owner=" + ownerId + ", tool=" + (ReadStringMember(tool, "name", tool.GetType().Name)));

                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedTool OK " + summary);
                runtime.SetHookStatus("Smoke.ActionSpeedTool", "verified", "AgentStateTool.OnEnter/OnExit private smoke path", summary);
                return SmokeAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed tool exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.ActionSpeedTool", "failed", "AgentStateTool.OnEnter", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private SmokeAttemptResult TryExerciseActionSpeedConfigApplyForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental GameBridge API was not registered.");

                SmokeAttemptResult beforeResult = TryExerciseActionSpeedToolForSmoke();
                if (beforeResult == SmokeAttemptResult.Pending)
                    return SmokeAttemptResult.Pending;
                if (beforeResult == SmokeAttemptResult.Failed)
                    throw new InvalidOperationException("Initial ActionSpeed tool exercise failed before config save.");

                string beforeSummary = experimentalApi.LastActionSpeedApplicationSummary;
                if (!SummaryContainsMultiplier(beforeSummary, 2))
                    throw new InvalidOperationException("Expected initial ActionSpeed multiplier=2 before config save; actual summary=" + beforeSummary);

                IConfigMenuPage? page = runtime.CreateSnapshot().ConfigPages.FirstOrDefault(p => p.Manifest.UniqueID.Equals("Yuuka.DTMAPI.ActionSpeed", StringComparison.OrdinalIgnoreCase));
                if (page == null)
                    throw new InvalidOperationException("ActionSpeed config page was not registered.");
                if (page.IsLocked)
                    throw new InvalidOperationException("ActionSpeed config page was locked: " + page.LockReason);

                page.BeginEditing();
                SetConfigPendingValue(page, "Bool", "true", "启用", "Enabled");
                SetConfigPendingValue(page, "InlineBoolNumber", "true|4", "工具动画加速", "Tool animation speed");
                page.Save();
                runtime.RuntimeMonitor.Log("Smoke ActionSpeed config saved through DTMAPI ConfigMenu page multiplier=4.");

                SmokeAttemptResult afterResult = TryExerciseActionSpeedToolForSmoke();
                if (afterResult == SmokeAttemptResult.Pending)
                    return SmokeAttemptResult.Pending;
                if (afterResult == SmokeAttemptResult.Failed)
                    throw new InvalidOperationException("ActionSpeed tool exercise failed after config save.");

                string afterSummary = experimentalApi.LastActionSpeedApplicationSummary;
                if (!SummaryContainsMultiplier(afterSummary, 4))
                    throw new InvalidOperationException("Expected ActionSpeed multiplier=4 after config save; actual summary=" + afterSummary);

                string summary = "before=" + beforeSummary + "; after=" + afterSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedConfigApply OK " + summary);
                runtime.SetHookStatus("Smoke.ActionSpeedConfigApply", "verified", "DTMAPI ConfigMenu Save -> AgentStateTool.OnEnter", summary);
                return SmokeAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed config-apply exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.ActionSpeedConfigApply", "failed", "DTMAPI ConfigMenu Save", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private SmokeAttemptResult TryExerciseActionSpeedInteractionForSmoke()
        {
            try
            {
                if (experimentalApi == null)
                    throw new InvalidOperationException("Experimental GameBridge API was not registered.");

                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new MissingMemberException("DolocAPI was not visible.");

                if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
                {
                    LogActionSpeedPending("Waiting for NormalGameState before action-speed interaction smoke. context=" + runtime.UI.InputContext + ".", "Smoke.ActionSpeedInteraction");
                    return SmokeAttemptResult.Pending;
                }

                if (!actionSpeedInteractEnterPatched || !actionSpeedInteractExitPatched || !actionSpeedEatEnterPatched || !actionSpeedUseItemContinuesPatched)
                {
                    LogActionSpeedPending("Waiting for AgentStateInteract/AgentStateEat/UseItemContinues patches before action-speed interaction smoke.", "Smoke.ActionSpeedInteraction");
                    return SmokeAttemptResult.Pending;
                }

                if (!experimentalApi.TryGetConfiguredActionSpeedInteractionOwner(out string ownerId))
                    throw new InvalidOperationException("ActionSpeed policy was not configured/enabled for interaction paths. Is the official ActionSpeed package enabled and smoke config written?");

                object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
                if (currentRoom != null && ReadBoolMember(currentRoom, "IsInHouse", false))
                {
                    if (!autoExerciseActionSpeedInteractionMainFarmRequested && TryEnterMainFarmForActionSpeedInteractionSmoke(dolocApi, currentRoom, out string transitionSummary))
                    {
                        LogActionSpeedPending(transitionSummary, "Smoke.ActionSpeedInteraction");
                        return SmokeAttemptResult.Pending;
                    }

                    if (autoExerciseActionSpeedInteractionMainFarmRequested)
                    {
                        LogActionSpeedPending("Waiting for main farm transition before action-speed interaction smoke. room=" + DescribeRoomForSmoke(currentRoom), "Smoke.ActionSpeedInteraction");
                        return SmokeAttemptResult.Pending;
                    }
                }

                var samples = new List<string>();
                if (!TryExerciseActionSpeedMachineAddKindForSmoke(
                    dolocApi,
                    "FuelMachine",
                    "DolocTown.PowerGeneratorFuel",
                    new[] { "wood_generator", "coal_generator", "modified_fuel_generator" },
                    new[] { "wood", "coal", "weeds" },
                    "FuelPercent",
                    out string fuelSummary))
                {
                    throw new InvalidOperationException("Fuel-machine ActionSpeed interaction path failed. " + fuelSummary);
                }
                samples.Add("fuel={" + fuelSummary + "}");

                if (!TryExerciseActionSpeedMachineAddKindForSmoke(
                    dolocApi,
                    "Feeder",
                    "DolocTown.Feeder",
                    new[] { "feeder", "large_feeder" },
                    new[] { "roughage_feed", "green_feed", "weeds", "thunder_grass" },
                    "progress",
                    out string feederSummary))
                {
                    throw new InvalidOperationException("Feeder ActionSpeed interaction path failed. " + feederSummary);
                }
                samples.Add("feeder={" + feederSummary + "}");

                if (!TryExerciseActionSpeedEatDrinkForSmoke(dolocApi, out string eatSummary))
                    throw new InvalidOperationException("Eat/drink ActionSpeed animation path failed. " + eatSummary);
                samples.Add("eatDrink={" + eatSummary + "}");

                if (!TryExerciseActionSpeedBottledWaterContinuousForSmoke(dolocApi, out string bottledWaterSummary))
                    throw new InvalidOperationException("Bottled-water right-click continuous-use path failed. " + bottledWaterSummary);
                samples.Add("bottledWaterRightClick={" + bottledWaterSummary + "}");

                if (!TryExerciseActionSpeedBottleFillForSmoke(dolocApi, out string bottleSummary))
                    throw new InvalidOperationException("Bottle-fill ActionSpeed continuous-use path failed. " + bottleSummary);
                samples.Add("bottleFill={" + bottleSummary + "}");

                if (!TryExerciseActionSpeedBottleFillInWaterForSmoke(dolocApi, out string bottleWaterSummary))
                    throw new InvalidOperationException("In-water bottle-fill ActionSpeed continuous-use path failed. " + bottleWaterSummary);
                samples.Add("bottleFillInWater={" + bottleWaterSummary + "}");

                if (!TryExerciseActionSpeedAutoFillBottleForSmoke(dolocApi, out string autoFillBottleSummary))
                    throw new InvalidOperationException("No-key in-water bottle auto-fill path failed. " + autoFillBottleSummary);
                samples.Add("autoFillBottle={" + autoFillBottleSummary + "}");

                if (!TryExerciseActionSpeedPlantForSmoke(dolocApi, out string plantSummary))
                    throw new InvalidOperationException("Planting ActionSpeed interaction path failed. " + plantSummary);
                samples.Add("plant={" + plantSummary + "}");

                if (!TryExerciseActionSpeedCropHarvestForSmoke(dolocApi, out string cropHarvestSummary))
                    throw new InvalidOperationException("Plant-basin crop harvest ActionSpeed interaction path failed. " + cropHarvestSummary);
                samples.Add("cropHarvest={" + cropHarvestSummary + "}");

                if (!TryExerciseActionSpeedResinHarvestForSmoke(dolocApi, out string resinSummary))
                    throw new InvalidOperationException("Resin harvest ActionSpeed interaction path failed. " + resinSummary);
                samples.Add("resin={" + resinSummary + "}");

                if (!TryExerciseActionSpeedVegetationHarvestForSmoke(dolocApi, out string vegetationSummary))
                    throw new InvalidOperationException("Wild vegetation harvest ActionSpeed interaction path failed. " + vegetationSummary);
                samples.Add("vegetationHarvest={" + vegetationSummary + "}");

                string summary = "owner=" + ownerId + "; " + string.Join("; ", samples.ToArray()) + "; pending=none";
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction OK " + summary);
                runtime.SetHookStatus("Smoke.ActionSpeedInteraction", "verified", "Native item/equipment paths -> AgentStateInteract/AgentStateEat/UseItemContinues", summary);
                runtime.SetHookStatus("ActionSpeed.InteractionAnimation", "experimental", "Harmony Postfix/Prefix: AgentStateInteract.OnEnter, AgentStateEat.OnEnter, AgentControllerState.UseItemContinues", "Verified fuel/feed add, eat/drink animation, bottled-water right-click continuous drink, bottle fill from IWaterContainer and in-water branch, no-key ItemBottle.UseAsItem auto-fill, planting, plant-basin crop harvest, resin collection, and wild vegetation harvest in third-save smoke.");
                return SmokeAttemptResult.Succeeded;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed interaction exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.ActionSpeedInteraction", "failed", "AgentStateInteract/AgentStateEat/UseItemContinues", ex.GetType().Name + ": " + ex.Message);
                return SmokeAttemptResult.Failed;
            }
        }

        private bool TryExerciseActionSpeedMachineAddKindForSmoke(Type dolocApi, string kind, string targetTypeName, string[] equipmentIds, string[] itemIds, string ratioMember, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            object? transientEquipment = null;
            int quickSlot = 0;

            try
            {
                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                {
                    summary = "CurrentRoom unavailable.";
                    return false;
                }

                object? equipment = TryCreateTransientEquipmentForSmoke(dolocApi, room, targetTypeName, equipmentIds, out string targetSource);
                if (equipment == null)
                {
                    equipment = FindExistingEquipmentForSmoke(room, targetTypeName, ratioMember, out targetSource);
                    if (equipment == null)
                    {
                        summary = "No target equipment available. targetType=" + targetTypeName + ", source=" + targetSource;
                        return false;
                    }
                }
                else
                {
                    transientEquipment = equipment;
                }

                string? itemId = SelectFillItemIdForSmoke(dolocApi, equipment, kind, itemIds, out string itemSelectSummary);
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    summary = "No valid fill item. target=" + DescribeEquipmentForSmoke(equipment) + ", " + itemSelectSummary;
                    return false;
                }

                object? item = GenerateItemForSmoke(dolocApi, itemId!, 3);
                if (item == null)
                {
                    summary = "Could not generate smoke item " + itemId + ".";
                    return false;
                }

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, item, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                {
                    summary = placeSummary;
                    return false;
                }

                object? anchor = ReadMember(equipment, "Anchor");
                string selectSummary = anchor == null ? "equipment anchor unavailable." : string.Empty;
                if (anchor == null || !TrySelectEquipmentForSmoke(dolocApi, equipment, anchor, out selectSummary))
                {
                    summary = "Could not select target equipment. target=" + DescribeEquipmentForSmoke(equipment) + ", " + selectSummary;
                    return false;
                }

                int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                double beforeRatio = ReadDoubleMember(equipment, ratioMember, -1);
                MethodInfo? decoratedInteract = FindMethod(equipment.GetType(), "DecoratedInteract", 0);
                if (decoratedInteract == null)
                {
                    summary = "Equipment.DecoratedInteract was not found. target=" + DescribeEquipmentForSmoke(equipment);
                    return false;
                }

                decoratedInteract.Invoke(equipment, null);
                if (!TryRunCurrentAgentInteractExitForSmoke(dolocApi, out string interactSummary))
                {
                    summary = "Native interaction did not reach AgentStateInteract.OnExit. target=" + DescribeEquipmentForSmoke(equipment) + ", " + interactSummary;
                    return false;
                }

                int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                double afterRatio = ReadDoubleMember(equipment, ratioMember, -1);
                int totalConsumed = beforeCount - afterCount;
                string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "MachineAdd");
                bool changed = afterRatio > beforeRatio || (beforeRatio < 0 && totalConsumed > 0);
                if (!speedApplied || totalConsumed <= 0 || !changed)
                {
                    summary = "target=" + DescribeEquipmentForSmoke(equipment) +
                        ", item=" + itemId +
                        ", source=" + targetSource +
                        ", count=" + beforeCount + "->" + afterCount +
                        ", ratio=" + FormatRatio(beforeRatio) + "->" + FormatRatio(afterRatio) +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "kind=" + kind +
                    ", target=" + DescribeEquipmentForSmoke(equipment) +
                    ", item=" + itemId +
                    ", source=" + targetSource +
                    ", count=" + beforeCount + "->" + afterCount +
                    ", ratio=" + FormatRatio(beforeRatio) + "->" + FormatRatio(afterRatio) +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed " + kind + " target failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed " + kind + " target failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
            }
        }

        private bool TryExerciseActionSpeedEatDrinkForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            int quickSlot = 0;

            try
            {
                object? item = null;
                string itemId = string.Empty;
                foreach (string candidate in new[] { "can", "bread", "berry", "milk" })
                {
                    item = GenerateItemForSmoke(dolocApi, candidate, 3);
                    if (item != null && IsTypeOrBase(item.GetType(), "DolocTown.ItemFood"))
                    {
                        itemId = candidate;
                        break;
                    }
                }
                if (item == null)
                {
                    summary = "No ItemFood candidate could be generated.";
                    return false;
                }

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, item, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                {
                    summary = placeSummary;
                    return false;
                }

                int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int beforeContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                int afterCount = beforeCount;
                string invokeSummary = string.Empty;
                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    if (!TryInvokeUseItemContinuesForSmoke(dolocApi, 0.2f, out invokeSummary))
                    {
                        summary = invokeSummary;
                        return false;
                    }
                    afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                    if (afterCount < beforeCount)
                        break;
                }

                int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int afterContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                string continuousSummary = experimentalApi?.LastActionSpeedContinuousUseSummary ?? string.Empty;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "EatDrink");
                if (!speedApplied || afterCount >= beforeCount)
                {
                    summary = "item=" + itemId +
                        ", count=" + beforeCount + "->" + afterCount +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                        ", invoke={" + invokeSummary + "}" +
                        ", continuous=" + (string.IsNullOrWhiteSpace(continuousSummary) ? "none" : continuousSummary) +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "item=" + itemId +
                    ", count=" + beforeCount + "->" + afterCount +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                    ", invoke={" + invokeSummary + "}" +
                    ", continuous=" + (string.IsNullOrWhiteSpace(continuousSummary) ? "none" : continuousSummary) +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK EatDrinkAnimation " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed eat/drink failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed eat/drink failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
            }
        }

        private bool TryExerciseActionSpeedBottledWaterContinuousForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            int quickSlot = 0;

            try
            {
                object? item = GenerateItemForSmoke(dolocApi, "bottle_of_water", 3);
                if (item == null)
                {
                    summary = "Could not generate bottle_of_water.";
                    return false;
                }

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, item, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                {
                    summary = placeSummary;
                    return false;
                }

                int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int beforeContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                int afterCount = beforeCount;
                string invokeSummary = string.Empty;
                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    if (!TryInvokeUseItemContinuesForSmoke(dolocApi, 0.2f, out invokeSummary))
                    {
                        summary = invokeSummary;
                        return false;
                    }
                    afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                    if (afterCount < beforeCount)
                        break;
                }

                int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int afterContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                string continuousSummary = experimentalApi?.LastActionSpeedContinuousUseSummary ?? string.Empty;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "EatDrink");
                bool continuousApplied = afterContinuous > beforeContinuous && SummaryContainsKind(continuousSummary, "BottledWaterDrink");
                if (!speedApplied || !continuousApplied || afterCount >= beforeCount)
                {
                    summary = "item=bottle_of_water" +
                        ", count=" + beforeCount + "->" + afterCount +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                        ", place={" + placeSummary + "}" +
                        ", invoke={" + invokeSummary + "}" +
                        ", continuous=" + (string.IsNullOrWhiteSpace(continuousSummary) ? "none" : continuousSummary) +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "item=bottle_of_water" +
                    ", count=" + beforeCount + "->" + afterCount +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                    ", place={" + placeSummary + "}" +
                    ", invoke={" + invokeSummary + "}" +
                    ", continuous=" + continuousSummary +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK BottledWaterRightClick " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed bottled-water right-click failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed bottled-water right-click failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
            }
        }

        private bool TryExerciseActionSpeedBottleFillForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            object? transientEquipment = null;
            int quickSlot = 0;

            try
            {
                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                {
                    summary = "CurrentRoom unavailable.";
                    return false;
                }

                object? well = TryCreateTransientEquipmentForSmoke(dolocApi, room, "DolocTown.SimpleWell", new[] { "well" }, out string wellSource);
                if (well == null)
                {
                    summary = "No SimpleWell target available. source=" + wellSource;
                    return false;
                }
                transientEquipment = well;
                MethodInfo? drawMax = FindMethod(well.GetType(), "DrawMax", 0);
                drawMax?.Invoke(well, null);

                object? item = GenerateItemForSmoke(dolocApi, "waste_plastic_bottle", 3);
                if (item == null || !IsTypeOrBase(item.GetType(), "DolocTown.ItemBottle"))
                {
                    summary = "Could not generate ItemBottle waste_plastic_bottle.";
                    return false;
                }

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, item, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                {
                    summary = placeSummary;
                    return false;
                }

                object? anchor = ReadMember(well, "Anchor");
                string selectSummary = anchor == null ? "equipment anchor unavailable." : string.Empty;
                if (anchor == null || !TrySelectEquipmentForSmoke(dolocApi, well, anchor, out selectSummary))
                {
                    summary = "Could not select well. target=" + DescribeEquipmentForSmoke(well) + ", " + selectSummary;
                    return false;
                }
                if (!TryPointAgentCellTipAtEquipmentForSmoke(dolocApi, well, out string tipSummary))
                {
                    summary = "Could not point item cell tip at well. target=" + DescribeEquipmentForSmoke(well) + ", " + tipSummary;
                    return false;
                }

                int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int beforeContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                int beforeWater = ReadIntMember(well, "Water", -1);
                int afterCount = beforeCount;
                int afterWater = beforeWater;
                string invokeSummary = string.Empty;
                string interactSummary = string.Empty;
                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    if (!TryInvokeUseItemContinuesForSmoke(dolocApi, 0.2f, out invokeSummary))
                    {
                        summary = invokeSummary;
                        return false;
                    }
                    if (TryRunCurrentAgentInteractExitForSmoke(dolocApi, out interactSummary))
                    {
                        afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                        afterWater = ReadIntMember(well, "Water", -1);
                        if (afterCount < beforeCount)
                            break;
                    }
                }

                int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int afterContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                string continuousSummary = experimentalApi?.LastActionSpeedContinuousUseSummary ?? string.Empty;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "BottleFill");
                bool continuousApplied = afterContinuous > beforeContinuous && SummaryContainsKind(continuousSummary, "BottleFill");
                bool waterChanged = beforeWater < 0 || afterWater < beforeWater;
                if (!speedApplied || !continuousApplied || afterCount >= beforeCount || !waterChanged)
                {
                    summary = "target=" + DescribeEquipmentForSmoke(well) +
                        ", count=" + beforeCount + "->" + afterCount +
                        ", water=" + beforeWater + "->" + afterWater +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                        ", tip={" + tipSummary + "}" +
                        ", invoke={" + invokeSummary + "}" +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", continuous=" + (string.IsNullOrWhiteSpace(continuousSummary) ? "none" : continuousSummary) +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "target=" + DescribeEquipmentForSmoke(well) +
                    ", count=" + beforeCount + "->" + afterCount +
                    ", water=" + beforeWater + "->" + afterWater +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                    ", tip={" + tipSummary + "}" +
                    ", invoke={" + invokeSummary + "}" +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", continuous=" + continuousSummary +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK BottleFill " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed bottle fill failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed bottle fill failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
            }
        }

        private bool TryExerciseActionSpeedBottleFillInWaterForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            int quickSlot = 0;
            bool originalIsInWater = false;
            Type? interactiveWaterType = null;

            try
            {
                if (experimentalApi != null)
                    experimentalApi.SuppressActionSpeedAutoFillForSmoke = true;

                interactiveWaterType = patcher?.ResolveType("DolocTown.InteractiveWater, Assembly-CSharp");
                if (interactiveWaterType == null)
                {
                    summary = "InteractiveWater type unavailable.";
                    return false;
                }

                object? original = ReadStaticMember(interactiveWaterType, "IsInWater");
                originalIsInWater = original is bool value && value;
                if (!WriteStaticBoolMember(interactiveWaterType, "IsInWater", true))
                {
                    summary = "Could not force InteractiveWater.IsInWater for water-pit branch.";
                    return false;
                }
                TryClearRoomScannerSelectionForSmoke(dolocApi);

                object? item = GenerateItemForSmoke(dolocApi, "waste_plastic_bottle", 3);
                if (item == null || !IsTypeOrBase(item.GetType(), "DolocTown.ItemBottle"))
                {
                    summary = "Could not generate ItemBottle waste_plastic_bottle.";
                    return false;
                }

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, item, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                {
                    summary = placeSummary;
                    return false;
                }

                int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int beforeContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                int afterCount = beforeCount;
                string invokeSummary = string.Empty;
                string interactSummary = string.Empty;
                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    if (!TryInvokeUseItemContinuesForSmoke(dolocApi, 0.2f, out invokeSummary))
                    {
                        summary = invokeSummary;
                        return false;
                    }
                    if (TryRunCurrentAgentInteractExitForSmoke(dolocApi, out interactSummary))
                    {
                        afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                        if (afterCount < beforeCount)
                            break;
                    }
                }

                int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int afterContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
                string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                string continuousSummary = experimentalApi?.LastActionSpeedContinuousUseSummary ?? string.Empty;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "BottleFill");
                bool continuousApplied = afterContinuous > beforeContinuous && SummaryContainsKind(continuousSummary, "BottleFill");
                if (!speedApplied || !continuousApplied || afterCount >= beforeCount)
                {
                    summary = "branch=InteractiveWater.IsInWater" +
                        ", count=" + beforeCount + "->" + afterCount +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                        ", place={" + placeSummary + "}" +
                        ", invoke={" + invokeSummary + "}" +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", continuous=" + (string.IsNullOrWhiteSpace(continuousSummary) ? "none" : continuousSummary) +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "branch=InteractiveWater.IsInWater" +
                    ", count=" + beforeCount + "->" + afterCount +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                    ", place={" + placeSummary + "}" +
                    ", invoke={" + invokeSummary + "}" +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", continuous=" + continuousSummary +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK BottleFillInWater " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed in-water bottle fill failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed in-water bottle fill failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (experimentalApi != null)
                    experimentalApi.SuppressActionSpeedAutoFillForSmoke = false;
                if (interactiveWaterType != null)
                    WriteStaticBoolMember(interactiveWaterType, "IsInWater", originalIsInWater);
            }
        }

        private bool TryExerciseActionSpeedAutoFillBottleForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            int quickSlot = 0;
            bool originalIsInWater = false;
            Type? interactiveWaterType = null;

            try
            {
                if (experimentalApi == null)
                {
                    summary = "Experimental API unavailable.";
                    return false;
                }

                interactiveWaterType = patcher?.ResolveType("DolocTown.InteractiveWater, Assembly-CSharp");
                if (interactiveWaterType == null)
                {
                    summary = "InteractiveWater type unavailable.";
                    return false;
                }

                object? original = ReadStaticMember(interactiveWaterType, "IsInWater");
                originalIsInWater = original is bool value && value;
                if (!WriteStaticBoolMember(interactiveWaterType, "IsInWater", true))
                {
                    summary = "Could not force InteractiveWater.IsInWater for no-key auto-fill branch.";
                    return false;
                }
                TryClearRoomScannerSelectionForSmoke(dolocApi);

                object? item = GenerateItemForSmoke(dolocApi, "waste_plastic_bottle", 3);
                if (item == null || !IsTypeOrBase(item.GetType(), "DolocTown.ItemBottle"))
                {
                    summary = "Could not generate ItemBottle waste_plastic_bottle.";
                    return false;
                }

                if (!TryPlaceSmokeItemInQuickSlot(dolocApi, item, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                {
                    summary = placeSummary;
                    return false;
                }

                int beforeAutoFill = experimentalApi.ActionSpeedAutoFillApplicationCount;
                int beforeApplications = experimentalApi.ActionSpeedApplicationCount;
                int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                string beforeItem = ReadQuickSlotItemName(inventory, quickSlot);
                string interactSummary = string.Empty;
                for (int attempt = 1; attempt <= 8; attempt++)
                {
                    experimentalApi.UpdateRuntimeAutomation();
                    if (experimentalApi.ActionSpeedAutoFillApplicationCount > beforeAutoFill)
                    {
                        WriteStaticBoolMember(interactiveWaterType, "IsInWater", false);
                        break;
                    }
                    Thread.Sleep(80);
                }
                TryRunCurrentAgentInteractExitForSmoke(dolocApi, out interactSummary);

                int afterAutoFill = experimentalApi.ActionSpeedAutoFillApplicationCount;
                int afterApplications = experimentalApi.ActionSpeedApplicationCount;
                int afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                string afterItem = ReadQuickSlotItemName(inventory, quickSlot);
                string bridgeSummary = experimentalApi.LastActionSpeedAutoFillSummary;
                bool invoked = afterAutoFill > beforeAutoFill && bridgeSummary.IndexOf("AutoFillBottle", StringComparison.OrdinalIgnoreCase) >= 0;
                bool inventoryChanged = afterCount != beforeCount || !afterItem.Equals(beforeItem, StringComparison.OrdinalIgnoreCase);
                if (!invoked)
                {
                    summary = "branch=InteractiveWater.IsInWater" +
                        ", item=" + beforeItem + "->" + afterItem +
                        ", count=" + beforeCount + "->" + afterCount +
                        ", autoFillDelta=" + (afterAutoFill - beforeAutoFill) +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", place={" + placeSummary + "}" +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "branch=InteractiveWater.IsInWater" +
                    ", item=" + beforeItem + "->" + afterItem +
                    ", count=" + beforeCount + "->" + afterCount +
                    ", inventoryChanged=" + inventoryChanged +
                    ", autoFillDelta=" + (afterAutoFill - beforeAutoFill) +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", place={" + placeSummary + "}" +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK AutoFillBottle " + summary);
                runtime.SetHookStatus("Smoke.ActionSpeedAutoFillBottle", "verified", "ItemBottle.UseAsItem native path", summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed no-key auto-fill bottle failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed no-key auto-fill bottle failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (interactiveWaterType != null)
                    WriteStaticBoolMember(interactiveWaterType, "IsInWater", originalIsInWater);
            }
        }

        private bool TryExerciseActionSpeedPlantForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            object? transientEquipment = null;
            int quickSlot = 0;

            try
            {
                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                {
                    summary = "CurrentRoom unavailable.";
                    return false;
                }

                object? basin = TryCreateTransientEquipmentForSmoke(dolocApi, room, "DolocTown.PlantBasin", new[] { "plantbasin_basic" }, out string basinSource);
                if (basin == null)
                {
                    summary = "No PlantBasin target available. source=" + basinSource;
                    return false;
                }
                transientEquipment = basin;

                object? anchor = ReadMember(basin, "Anchor");
                string selectSummary = anchor == null ? "equipment anchor unavailable." : string.Empty;
                if (anchor == null || !TrySelectEquipmentForSmoke(dolocApi, basin, anchor, out selectSummary))
                {
                    summary = "Could not select plant basin. target=" + DescribeEquipmentForSmoke(basin) + ", " + selectSummary;
                    return false;
                }

                foreach (string seedId in new[] { "seed_endyam", "seed_thunder_grass", "seed_chinese_cabbage", "seed_scallion", "seed_pumpkin" })
                {
                    object? seed = GenerateItemForSmoke(dolocApi, seedId, 3);
                    if (seed == null || !IsTypeOrBase(seed.GetType(), "DolocTown.ItemSeed"))
                        continue;

                    if (!TryPlaceSmokeItemInQuickSlot(dolocApi, seed, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                    {
                        summary = placeSummary;
                        return false;
                    }
                    if (!TryPointAgentCellTipAtEquipmentForSmoke(dolocApi, basin, out string tipSummary))
                    {
                        summary = "Could not point item cell tip at plant basin. target=" + DescribeEquipmentForSmoke(basin) + ", " + tipSummary;
                        return false;
                    }

                    object? selectedSeed = ReadStaticMember(dolocApi, "SelectedItem");
                    if (selectedSeed == null || !IsTypeOrBase(selectedSeed.GetType(), "DolocTown.ItemSeed"))
                    {
                        summary = "SelectedItem was not an ItemSeed after quick-slot placement. place={" + placeSummary + "}, selected=" + (selectedSeed == null ? "null" : selectedSeed.GetType().FullName);
                        return false;
                    }

                    string seedType = ReadSeedTypeForSmoke(selectedSeed);
                    string basinSeedType = ReadPlantBasinSeedTypeForSmoke(basin);
                    WriteBoolMember(selectedSeed, "enablePlantInInvalidSeason", true);
                    MethodInfo? plantSeed = selectedSeed.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(m => m.Name == "PlantSeed" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsInstanceOfType(basin));
                    if (plantSeed == null)
                    {
                        summary = "ItemSeed.PlantSeed(PlantBasin) was not found.";
                        return false;
                    }

                    int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                    int beforeCount = ReadQuickSlotItemCount(inventory, quickSlot);
                    plantSeed.Invoke(selectedSeed, new[] { basin });
                    if (!TryRunCurrentAgentInteractExitForSmoke(dolocApi, out string interactSummary))
                    {
                        summary = "Native planting interaction did not reach AgentStateInteract.OnExit. " + interactSummary;
                        return false;
                    }

                    int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                    int afterCount = ReadQuickSlotItemCount(inventory, quickSlot);
                    bool isPlanted = ReadBoolMember(basin, "IsPlanted", false);
                    string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                    bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "Plant");
                    if (speedApplied && isPlanted && afterCount < beforeCount)
                    {
                        summary = "seed=" + seedId +
                            ", seedType=" + seedType +
                            ", basinSeedType=" + basinSeedType +
                            ", target=" + DescribeEquipmentForSmoke(basin) +
                            ", count=" + beforeCount + "->" + afterCount +
                            ", isPlanted=" + isPlanted +
                            ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                            ", place={" + placeSummary + "}" +
                            ", tip={" + tipSummary + "}" +
                            ", nativeInteract={" + interactSummary + "}" +
                            ", bridge=" + bridgeSummary;
                        runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK Plant " + summary);
                        return true;
                    }

                    RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                    inventory = null;
                    originalSlotItem = null;
                    TryEnterIdleStateForSmoke(dolocApi);
                }

                summary = "No seed candidate produced a planted PlantBasin through the native ItemSeed.PlantSeed path. target=" + DescribeEquipmentForSmoke(basin) +
                    ", basinSeedType=" + ReadPlantBasinSeedTypeForSmoke(basin) +
                    ", isRemoved=" + ReadBoolMember(basin, "IsRemoved", false);
                return false;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed planting failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed planting failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
            }
        }

        private bool TryExerciseActionSpeedCropHarvestForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? inventory = null;
            object? originalSlotItem = null;
            object? transientEquipment = null;
            int quickSlot = 0;

            try
            {
                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                {
                    summary = "CurrentRoom unavailable.";
                    return false;
                }

                object? basin = TryCreateTransientEquipmentForSmoke(dolocApi, room, "DolocTown.PlantBasin", new[] { "plantbasin_basic" }, out string basinSource);
                if (basin == null)
                {
                    summary = "No PlantBasin target available. source=" + basinSource;
                    return false;
                }
                transientEquipment = basin;

                object? anchor = ReadMember(basin, "Anchor");
                string selectSummary = anchor == null ? "equipment anchor unavailable." : string.Empty;
                if (anchor == null || !TrySelectEquipmentForSmoke(dolocApi, basin, anchor, out selectSummary))
                {
                    summary = "Could not select plant basin. target=" + DescribeEquipmentForSmoke(basin) + ", " + selectSummary;
                    return false;
                }

                foreach (string seedId in new[] { "seed_endyam", "seed_thunder_grass", "seed_chinese_cabbage", "seed_scallion", "seed_pumpkin" })
                {
                    object? seed = GenerateItemForSmoke(dolocApi, seedId, 3);
                    if (seed == null || !IsTypeOrBase(seed.GetType(), "DolocTown.ItemSeed"))
                        continue;

                    if (!TryPlaceSmokeItemInQuickSlot(dolocApi, seed, quickSlot, out inventory, out originalSlotItem, out string placeSummary))
                    {
                        summary = placeSummary;
                        return false;
                    }
                    if (!TryPointAgentCellTipAtEquipmentForSmoke(dolocApi, basin, out string tipSummary))
                    {
                        summary = "Could not point item cell tip at plant basin. target=" + DescribeEquipmentForSmoke(basin) + ", " + tipSummary;
                        return false;
                    }

                    object? selectedSeed = ReadStaticMember(dolocApi, "SelectedItem");
                    if (selectedSeed == null || !IsTypeOrBase(selectedSeed.GetType(), "DolocTown.ItemSeed"))
                    {
                        summary = "SelectedItem was not an ItemSeed after quick-slot placement. place={" + placeSummary + "}, selected=" + (selectedSeed == null ? "null" : selectedSeed.GetType().FullName);
                        return false;
                    }

                    WriteBoolMember(selectedSeed, "enablePlantInInvalidSeason", true);
                    MethodInfo? plantSeed = selectedSeed.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(m => m.Name == "PlantSeed" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsInstanceOfType(basin));
                    if (plantSeed == null)
                    {
                        summary = "ItemSeed.PlantSeed(PlantBasin) was not found.";
                        return false;
                    }

                    plantSeed.Invoke(selectedSeed, new[] { basin });
                    if (!TryRunCurrentAgentInteractExitForSmoke(dolocApi, out string plantInteractSummary))
                    {
                        summary = "Native planting setup did not reach AgentStateInteract.OnExit. " + plantInteractSummary;
                        return false;
                    }

                    object? crop = ReadMember(basin, "crop");
                    if (crop == null || !TrySetCropMatureForSmoke(crop, out string matureSummary))
                    {
                        RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                        inventory = null;
                        originalSlotItem = null;
                        TryEnterIdleStateForSmoke(dolocApi);
                        continue;
                    }

                    RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                    inventory = null;
                    originalSlotItem = null;
                    TryEnterIdleStateForSmoke(dolocApi);

                    if (!TrySelectEquipmentForSmoke(dolocApi, basin, anchor, out string harvestSelectSummary))
                    {
                        summary = "Could not re-select mature plant basin. target=" + DescribeEquipmentForSmoke(basin) + ", " + harvestSelectSummary;
                        return false;
                    }

                    bool beforeCouldHarvest = ReadBoolMember(basin, "CouldHarvest", false);
                    int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                    MethodInfo? decoratedInteract = FindMethod(basin.GetType(), "DecoratedInteract", 0);
                    if (decoratedInteract == null)
                    {
                        summary = "Equipment.DecoratedInteract was not found. target=" + DescribeEquipmentForSmoke(basin);
                        return false;
                    }

                    decoratedInteract.Invoke(basin, null);
                    if (!TryRunCurrentAgentInteractExitForSmoke(dolocApi, out string harvestInteractSummary))
                    {
                        summary = "Native crop harvest interaction did not reach AgentStateInteract.OnExit. target=" + DescribeEquipmentForSmoke(basin) + ", " + harvestInteractSummary;
                        return false;
                    }

                    int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                    object? afterCrop = ReadMember(basin, "crop");
                    bool afterCouldHarvest = ReadBoolMember(basin, "CouldHarvest", false);
                    bool afterMature = afterCrop != null && ReadBoolMember(afterCrop, "isMature", false);
                    string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                    bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "Harvest");
                    bool changed = beforeCouldHarvest && (!afterCouldHarvest || afterCrop == null || !afterMature);
                    if (speedApplied && changed)
                    {
                        summary = "seed=" + seedId +
                            ", target=" + DescribeEquipmentForSmoke(basin) +
                            ", source=" + basinSource +
                            ", couldHarvest=" + beforeCouldHarvest + "->" + afterCouldHarvest +
                            ", afterCrop=" + (afterCrop == null ? "null" : afterCrop.GetType().Name) +
                            ", afterMature=" + afterMature +
                            ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                            ", setup={" + plantInteractSummary + "; " + matureSummary + "}" +
                            ", place={" + placeSummary + "}" +
                            ", tip={" + tipSummary + "}" +
                            ", nativeInteract={" + harvestInteractSummary + "}" +
                            ", bridge=" + bridgeSummary;
                        runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK CropHarvest " + summary);
                        return true;
                    }

                    summary = "seed=" + seedId +
                        ", target=" + DescribeEquipmentForSmoke(basin) +
                        ", couldHarvest=" + beforeCouldHarvest + "->" + afterCouldHarvest +
                        ", afterCrop=" + (afterCrop == null ? "null" : afterCrop.GetType().Name) +
                        ", afterMature=" + afterMature +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", nativeInteract={" + harvestInteractSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "No seed candidate could be planted and matured for crop-harvest smoke. target=" + DescribeEquipmentForSmoke(basin);
                return false;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed crop harvest failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed crop harvest failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                RestoreSmokeQuickSlot(dolocApi, inventory, quickSlot, originalSlotItem);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
            }
        }

        private bool TryExerciseActionSpeedResinHarvestForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? transientEquipment = null;
            object? transientResource = null;

            try
            {
                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                {
                    summary = "CurrentRoom unavailable.";
                    return false;
                }

                object? collector = FindExistingResinCollectorForSmoke(room, out string collectorSource);
                if (collector == null)
                {
                    collector = TryCreateTransientResinCollectorForSmoke(dolocApi, room, out transientResource, out collectorSource);
                    if (collector == null)
                    {
                        summary = "No ResinCollector target available. source=" + collectorSource;
                        return false;
                    }
                    transientEquipment = collector;
                }

                MethodInfo? updateCurrentValue = FindMethod(collector.GetType(), "UpdateCurrentValue", 1);
                if (updateCurrentValue != null)
                    updateCurrentValue.Invoke(collector, new object[] { 3 });
                else
                    WriteIntMember(collector, "currentValue", 3);

                object? anchor = ReadMember(collector, "Anchor");
                string selectSummary = anchor == null ? "equipment anchor unavailable." : string.Empty;
                if (anchor == null || !TrySelectEquipmentForSmoke(dolocApi, collector, anchor, out selectSummary))
                {
                    summary = "Could not select resin collector. target=" + DescribeEquipmentForSmoke(collector) + ", " + selectSummary;
                    return false;
                }

                int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int beforeValue = ReadIntMember(collector, "currentValue", -1);
                MethodInfo? decoratedInteract = FindMethod(collector.GetType(), "DecoratedInteract", 0);
                if (decoratedInteract == null)
                {
                    summary = "Equipment.DecoratedInteract was not found. target=" + DescribeEquipmentForSmoke(collector);
                    return false;
                }

                decoratedInteract.Invoke(collector, null);
                if (!TryRunCurrentAgentInteractExitForSmoke(dolocApi, out string interactSummary))
                {
                    summary = "Native resin interaction did not reach AgentStateInteract.OnExit. target=" + DescribeEquipmentForSmoke(collector) + ", " + interactSummary;
                    return false;
                }

                int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int afterValue = ReadIntMember(collector, "currentValue", -1);
                string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "Harvest");
                if (!speedApplied || beforeValue <= 0 || afterValue != 0)
                {
                    summary = "target=" + DescribeEquipmentForSmoke(collector) +
                        ", source=" + collectorSource +
                        ", currentValue=" + beforeValue + "->" + afterValue +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "target=" + DescribeEquipmentForSmoke(collector) +
                    ", source=" + collectorSource +
                    ", currentValue=" + beforeValue + "->" + afterValue +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK ResinHarvest " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed resin harvest failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed resin harvest failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                if (transientEquipment != null)
                    TryRemoveTransientEquipmentForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientEquipment);
                if (transientResource != null)
                    TryRemoveTransientDungeonResourceForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientResource);
            }
        }

        private bool TryExerciseActionSpeedVegetationHarvestForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? transientVegetation = null;

            try
            {
                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                if (room == null)
                {
                    summary = "CurrentRoom unavailable.";
                    return false;
                }

                if (!TryCreateTransientMatureVegetationForSmoke(dolocApi, room, out object? vegetation, out object? renderer, out string vegetationSource) ||
                    vegetation == null ||
                    renderer == null)
                {
                    summary = vegetationSource;
                    return false;
                }
                transientVegetation = vegetation;

                int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int beforeLevel = ReadIntMember(vegetation, "currentLevel", -1);
                int beforeMaxLevel = ReadIntMember(vegetation, "maxLevel", -1);
                TryClearRoomScannerSelectionForSmoke(dolocApi);
                if (!TryInteractWithCurrentInteractableForSmoke(dolocApi, renderer, out string interactStartSummary))
                {
                    summary = "Could not start vegetation interact. target=" + DescribeVegetationForSmoke(vegetation) + ", " + interactStartSummary;
                    return false;
                }

                if (!TryRunCurrentAgentInteractExitForSmoke(dolocApi, out string interactSummary))
                {
                    summary = "Native vegetation harvest interaction did not reach AgentStateInteract.OnExit. target=" + DescribeVegetationForSmoke(vegetation) + ", " + interactSummary;
                    return false;
                }

                int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
                int afterLevel = ReadIntMember(vegetation, "currentLevel", -1);
                object? afterRenderer = ReadMember(vegetation, "Renderer");
                string bridgeSummary = experimentalApi?.LastActionSpeedApplicationSummary ?? string.Empty;
                bool speedApplied = afterApplications > beforeApplications && SummaryContainsKind(bridgeSummary, "Harvest");
                bool changed = afterLevel >= 0 && beforeLevel >= 0
                    ? afterLevel < beforeLevel || afterRenderer == null
                    : afterRenderer == null;
                if (!speedApplied || !changed)
                {
                    summary = "target=" + DescribeVegetationForSmoke(vegetation) +
                        ", source=" + vegetationSource +
                        ", level=" + beforeLevel + "/" + beforeMaxLevel + "->" + afterLevel +
                        ", rendererAfter=" + (afterRenderer == null ? "null" : afterRenderer.GetType().Name) +
                        ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                        ", start={" + interactStartSummary + "}" +
                        ", nativeInteract={" + interactSummary + "}" +
                        ", bridge=" + (string.IsNullOrWhiteSpace(bridgeSummary) ? "none" : bridgeSummary);
                    return false;
                }

                summary = "target=" + DescribeVegetationForSmoke(vegetation) +
                    ", source=" + vegetationSource +
                    ", level=" + beforeLevel + "/" + beforeMaxLevel + "->" + afterLevel +
                    ", rendererAfter=" + (afterRenderer == null ? "null" : afterRenderer.GetType().Name) +
                    ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                    ", start={" + interactStartSummary + "}" +
                    ", nativeInteract={" + interactSummary + "}" +
                    ", bridge=" + bridgeSummary;
                runtime.RuntimeMonitor.Log("Smoke exercise ActionSpeedInteraction sample OK VegetationHarvest " + summary);
                return true;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed vegetation harvest failed.", ex.InnerException.ToString());
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed vegetation harvest failed.", ex.ToString());
                return false;
            }
            finally
            {
                TryEnterIdleStateForSmoke(dolocApi);
                if (transientVegetation != null)
                    TryRemoveTransientVegetationForSmoke(ReadStaticMember(dolocApi, "CurrentRoom"), transientVegetation);
            }
        }

        private object? FindExistingResinCollectorForSmoke(object room, out string summary)
        {
            int scanned = 0;
            object? manager = ReadMember(room, "DM_equipment");
            object? allEquipments = manager == null ? null : ReadMember(manager, "AllEquipments");
            if (allEquipments is IEnumerable enumerable)
            {
                foreach (object equipment in enumerable)
                {
                    if (equipment == null)
                        continue;
                    scanned++;
                    if (!IsTypeOrBase(equipment.GetType(), "DolocTown.ResinCollector") || IsRemoved(equipment))
                        continue;

                    object? decalHost = ReadMember(equipment, "DecalHost");
                    summary = "existing:" + DescribeEquipmentForSmoke(equipment) +
                        ", host=" + (decalHost == null ? "none" : DescribeDecalHostForSmoke(decalHost)) +
                        ", scanned=" + scanned;
                    return equipment;
                }
            }

            summary = "no existing ResinCollector. scanned=" + scanned;
            return null;
        }

        private object? TryCreateTransientResinCollectorForSmoke(Type dolocApi, object room, out object? transientResource, out string summary)
        {
            transientResource = null;
            summary = string.Empty;

            Type? hostType = patcher?.ResolveType("DolocTown.IEquipmentHost, Assembly-CSharp");
            if (hostType == null || !hostType.IsInstanceOfType(room))
            {
                summary = "current room is not an IEquipmentHost. room=" + DescribeRoomForSmoke(room);
                return null;
            }

            object? proto = QueryEquipmentProtoForSmoke(dolocApi, "resin_collector");
            if (proto == null)
            {
                summary = "resin_collector proto missing.";
                return null;
            }

            if (!TryFindResinCollectorPlacementForSmoke(room, proto, out object? decalHost, out int decalSlotIndex, out object? worldPosition, out object? anchor, out string placementSummary))
            {
                transientResource = TryCreateTransientResinTreeHostForSmoke(room, proto, out string resourceSummary);
                if (transientResource == null ||
                    !TryFindResinCollectorPlacementForSmoke(room, proto, out decalHost, out decalSlotIndex, out worldPosition, out anchor, out placementSummary))
                {
                    summary = "decal placement unavailable. initial={" + placementSummary + "}, resource={" + resourceSummary + "}";
                    return null;
                }

                placementSummary = placementSummary + ", transientResource={" + resourceSummary + "}";
            }

            MethodInfo? createEquipment = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "CreateEquipment" && m.GetParameters().Length == 6);
            if (createEquipment == null)
            {
                summary = "IEquipmentHost.CreateEquipment was not found.";
                return null;
            }

            try
            {
                object? collector = createEquipment.Invoke(room, new object?[] { worldPosition, anchor, proto, false, decalHost, decalSlotIndex });
                if (collector == null)
                {
                    summary = "CreateEquipment returned null. placement={" + placementSummary + "}";
                    return null;
                }

                if (!IsTypeOrBase(collector.GetType(), "DolocTown.ResinCollector"))
                {
                    summary = "CreateEquipment returned wrong type " + collector.GetType().FullName + ". placement={" + placementSummary + "}";
                    TryRemoveTransientEquipmentForSmoke(room, collector);
                    return null;
                }

                summary = "transient-decal:resin_collector, target=" + DescribeEquipmentForSmoke(collector) +
                    ", placement={" + placementSummary + "}";
                return collector;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = "CreateEquipment threw " + ex.InnerException.GetType().Name + ": " + ex.InnerException.Message + ". placement={" + placementSummary + "}";
                return null;
            }
            catch (Exception ex)
            {
                summary = "CreateEquipment threw " + ex.GetType().Name + ": " + ex.Message + ". placement={" + placementSummary + "}";
                return null;
            }
        }

        private object? TryCreateTransientResinTreeHostForSmoke(object room, object resinCollectorProto, out string summary)
        {
            summary = string.Empty;
            Type? resourceHostType = patcher?.ResolveType("DolocTown.IDungeonResourceHost, Assembly-CSharp");
            MethodInfo? createResource = FindMethod(resourceHostType, "CreateDungeonResource", 1);
            if (resourceHostType == null || createResource == null || !resourceHostType.IsInstanceOfType(room))
            {
                summary = "room is not an IDungeonResourceHost. room=" + DescribeRoomForSmoke(room);
                return null;
            }

            int attempted = 0;
            List<string> notes = new List<string>();
            foreach (object proto in FindOneActionResourceProtosForSmoke("Tree"))
            {
                attempted++;
                object? resource = null;
                try
                {
                    resource = createResource.Invoke(room, new object[] { proto });
                    if (resource == null)
                    {
                        notes.Add(ReadStringMember(proto, "Id", "tree") + ":null");
                        continue;
                    }

                    FindMethod(resource.GetType(), "SetMaxGrowthLevel", 1)?.Invoke(resource, new object[] { false });
                    FindMethod(resource.GetType(), "SetMaxHealth", 0)?.Invoke(resource, null);

                    if (AcceptsResinCollectorHostForSmoke(room, resinCollectorProto, resource, out string hostFilterSummary) &&
                        TryFindResinCollectorPlacementForSmoke(room, resinCollectorProto, out object? host, out int slotIndex, out _, out _, out string placementSummary) &&
                        ReferenceEquals(host, resource))
                    {
                        summary = "created transient tree host " + DescribeDecalHostForSmoke(resource) +
                            ", slot=" + slotIndex +
                            ", attempted=" + attempted +
                            ", hostFilter={" + hostFilterSummary + "}, placement={" + placementSummary + "}";
                        return resource;
                    }

                    notes.Add(ReadStringMember(resource, "ResourceName", resource.GetType().Name) + ":not-usable");
                    TryRemoveTransientDungeonResourceForSmoke(room, resource);
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    notes.Add(ReadStringMember(proto, "Id", "tree") + ":" + ex.InnerException.GetType().Name);
                    if (resource != null)
                        TryRemoveTransientDungeonResourceForSmoke(room, resource);
                }
                catch (Exception ex)
                {
                    notes.Add(ReadStringMember(proto, "Id", "tree") + ":" + ex.GetType().Name);
                    if (resource != null)
                        TryRemoveTransientDungeonResourceForSmoke(room, resource);
                }
            }

            summary = "no usable transient tree host. attempted=" + attempted + ", notes=" + string.Join("|", notes.Take(8));
            return null;
        }

        private bool TryFindResinCollectorPlacementForSmoke(
            object room,
            object proto,
            out object? decalHost,
            out int decalSlotIndex,
            out object? worldPosition,
            out object? anchor,
            out string summary)
        {
            decalHost = null;
            decalSlotIndex = -1;
            worldPosition = null;
            anchor = null;
            summary = string.Empty;

            object? terrain = ReadMember(room, "DM_terrain");
            MethodInfo? getCachedDecalHosts = terrain == null ? null : FindMethod(terrain.GetType(), "GetCachedDecalHosts", 1);
            if (terrain == null || getCachedDecalHosts == null)
            {
                summary = "DM_terrain.GetCachedDecalHosts unavailable.";
                return false;
            }

            object? fitSlots = ReadMember(proto, "FitSlots");
            if (!(fitSlots is IEnumerable fitSlotEnumerable))
            {
                summary = "resin_collector FitSlots unavailable.";
                return false;
            }

            int hostCount = 0;
            int occupiedCount = 0;
            List<string> notes = new List<string>();
            foreach (object fitSlot in fitSlotEnumerable)
            {
                object? slotType = ReadMember(fitSlot, "SlotType");
                if (slotType == null)
                    continue;

                object? hosts = getCachedDecalHosts.Invoke(terrain, new[] { slotType });
                if (!(hosts is IEnumerable hostEnumerable))
                    continue;

                foreach (object host in hostEnumerable)
                {
                    if (host == null || IsRemoved(host))
                        continue;
                    hostCount++;
                    if (!AcceptsResinCollectorHostForSmoke(room, proto, host, out string hostFilterSummary))
                    {
                        notes.Add(DescribeDecalHostForSmoke(host) + ":host-filter=" + hostFilterSummary);
                        continue;
                    }

                    foreach ((int slotIndex, object slotWorldPosition) in GetDecalSlotPositionsForSmoke(host, slotType))
                    {
                        if (IsDecalSlotOccupiedForSmoke(host, slotIndex))
                        {
                            occupiedCount++;
                            continue;
                        }

                        object? slotAnchor = CreateDecalAnchorForSmoke(room, proto, slotWorldPosition) ?? ReadMember(host, "Anchor");
                        if (slotAnchor == null)
                        {
                            notes.Add(DescribeDecalHostForSmoke(host) + ":anchor-unavailable");
                            continue;
                        }

                        decalHost = host;
                        decalSlotIndex = slotIndex;
                        worldPosition = slotWorldPosition;
                        anchor = slotAnchor;
                        summary = "host=" + DescribeDecalHostForSmoke(host) +
                            ", slotType=" + slotType +
                            ", slot=" + slotIndex +
                            ", anchor=" + ReadIntMember(slotAnchor, "x", 0) + "," + ReadIntMember(slotAnchor, "y", 0) +
                            ", world=" + FormatVectorForSmoke(slotWorldPosition) +
                            ", hostFilter={" + hostFilterSummary + "}" +
                            ", scannedHosts=" + hostCount +
                            ", occupiedSlots=" + occupiedCount;
                        return true;
                    }
                }
            }

            summary = "no free accepted decal slot. scannedHosts=" + hostCount + ", occupiedSlots=" + occupiedCount + ", notes=" + string.Join("|", notes.Take(6));
            return false;
        }

        private IEnumerable<(int slotIndex, object worldPosition)> GetDecalSlotPositionsForSmoke(object decalHost, object slotType)
        {
            Type? extensionType = patcher?.ResolveType("DolocTown.DecalHostExtension, Assembly-CSharp");
            MethodInfo? getSlotWorldPos = extensionType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "GetSlotWorldPos" && m.GetParameters().Length == 2);
            object? positions = getSlotWorldPos?.Invoke(null, new[] { decalHost, slotType });
            if (!(positions is IEnumerable enumerable))
                yield break;

            foreach (object tuple in enumerable)
            {
                object? slotIndexValue = ReadMember(tuple, "Item1");
                object? worldPosition = ReadMember(tuple, "Item2");
                if (slotIndexValue == null || worldPosition == null)
                    continue;
                yield return (Convert.ToInt32(slotIndexValue), worldPosition);
            }
        }

        private bool AcceptsResinCollectorHostForSmoke(object room, object proto, object decalHost, out string summary)
        {
            summary = string.Empty;
            Type? equipmentManager = patcher?.ResolveType("DolocTown.EquipmentManager, Assembly-CSharp");
            MethodInfo? createDisposeEquipment = equipmentManager?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "CreateDisposeEquipment" && m.GetParameters().Length == 3);
            if (createDisposeEquipment == null)
            {
                summary = "CreateDisposeEquipment unavailable; using host type fallback.";
                return IsTypeOrBase(decalHost.GetType(), "DolocTown.DungeonResourceTree") || IsTypeOrBase(decalHost.GetType(), "DolocTown.PlantBasinTree");
            }

            try
            {
                object? disposable = createDisposeEquipment.Invoke(null, new object[] { room, proto, false });
                MethodInfo? hostFilter = disposable == null ? null : FindMethod(disposable.GetType(), "HostFilter", 1);
                object? result = hostFilter?.Invoke(disposable, new[] { decalHost });
                bool accepted = result is bool ok && ok;
                summary = accepted ? "accepted" : "rejected";
                return accepted;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                summary = ex.InnerException.GetType().Name + ": " + ex.InnerException.Message;
                return false;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private bool IsDecalSlotOccupiedForSmoke(object decalHost, int slotIndex)
        {
            object? attachedDecals = ReadMember(decalHost, "AttachedDecals");
            if (attachedDecals == null)
                return false;

            MethodInfo? containsKey = attachedDecals.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "ContainsKey" && m.GetParameters().Length == 1);
            if (containsKey != null)
            {
                object? result = containsKey.Invoke(attachedDecals, new object[] { slotIndex });
                if (result is bool occupied)
                    return occupied;
            }

            if (attachedDecals is IDictionary dictionary)
                return dictionary.Contains(slotIndex);

            return false;
        }

        private object? CreateDecalAnchorForSmoke(object room, object proto, object worldPosition)
        {
            object? roomPosition = ReadMember(room, "RoomPosition");
            object? decalPosition = CreateVector2ForSmoke(
                ReadDoubleMember(worldPosition, "x", 0),
                ReadDoubleMember(worldPosition, "y", 0));
            if (roomPosition == null || decalPosition == null)
                return null;

            MethodInfo? getAnchorByPosition = proto.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "GetAnchorTile" && m.GetParameters().Length == 2);
            if (getAnchorByPosition != null)
                return getAnchorByPosition.Invoke(proto, new[] { roomPosition, decalPosition });

            MethodInfo? getCoveredTile = FindMethod(proto.GetType(), "GetCoveredTileLBRT", 2);
            MethodInfo? getAnchorByLbrt = FindMethod(proto.GetType(), "GetAnchorTile", 1);
            object? lbrt = getCoveredTile?.Invoke(proto, new[] { roomPosition, decalPosition });
            return lbrt == null ? null : getAnchorByLbrt?.Invoke(proto, new[] { lbrt });
        }

        private static string DescribeDecalHostForSmoke(object decalHost)
        {
            return ReadStringMember(decalHost, "Name", ReadStringMember(decalHost, "ResourceName", decalHost.GetType().Name)) +
                "/" + (decalHost.GetType().FullName ?? decalHost.GetType().Name) +
                "/index=" + ReadIntMember(decalHost, "index", -1) +
                "/level=" + ReadIntMember(decalHost, "currentLevel", -1);
        }

        private static string FormatVectorForSmoke(object vector)
        {
            return ReadDoubleMember(vector, "x", 0).ToString("0.###") + "," +
                ReadDoubleMember(vector, "y", 0).ToString("0.###") + "," +
                ReadDoubleMember(vector, "z", 0).ToString("0.###");
        }

        private bool TryCreateTransientDandelionVegetationForSmoke(Type dolocApi, object room, out object? vegetation, out object? renderer, out string summary)
        {
            vegetation = null;
            renderer = null;
            summary = string.Empty;

            Type? hostType = patcher?.ResolveType("DolocTown.IVegetationHost, Assembly-CSharp");
            if (hostType == null || !hostType.IsInstanceOfType(room))
            {
                summary = "current room is not an IVegetationHost. room=" + DescribeRoomForSmoke(room);
                return false;
            }

            MethodInfo? createVegetation = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "CreateVegetationNoRender" && m.GetParameters().Length == 2);
            if (createVegetation == null)
            {
                summary = "IVegetationHost.CreateVegetationNoRender was not found.";
                return false;
            }

            IReadOnlyList<object> protos = FindDandelionVegetationProtosForSmoke(out string protoSummary);
            List<string> notes = new List<string>();
            foreach (object proto in protos)
            {
                string protoId = ReadStringMember(proto, "Id", proto.GetType().Name);
                object? candidate = null;
                try
                {
                    candidate = createVegetation.Invoke(room, new object[] { proto, false });
                    if (candidate == null)
                    {
                        candidate = TryCreateDirectTransientVegetationForSmoke(room, proto, out string directSummary);
                        if (candidate == null)
                        {
                            notes.Add(protoId + ":create-null,direct={" + directSummary + "}");
                            continue;
                        }

                        notes.Add(protoId + ":direct-create(" + directSummary + ")");
                    }

                    if (!TrySetVegetationMaxGrowthForSmoke(candidate, out string growthSummary))
                    {
                        notes.Add(protoId + ":growth-failed(" + growthSummary + ")");
                        TryRemoveTransientVegetationForSmoke(room, candidate);
                        continue;
                    }

                    if (!TryRenderVegetationForSmoke(dolocApi, room, candidate, out renderer, out string renderSummary) || renderer == null)
                    {
                        notes.Add(protoId + ":render-failed(" + renderSummary + ")");
                        TryRemoveTransientVegetationForSmoke(room, candidate);
                        continue;
                    }

                    vegetation = candidate;
                    summary = "transient:" + DescribeVegetationForSmoke(candidate) + ", proto={" + protoSummary + "}, growth={" + growthSummary + "}, render={" + renderSummary + "}, notes=" + string.Join("|", notes.Take(8));
                    return true;
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    notes.Add(protoId + ":" + ex.InnerException.GetType().Name);
                    if (candidate != null)
                        TryRemoveTransientVegetationForSmoke(room, candidate);
                }
                catch (Exception ex)
                {
                    notes.Add(protoId + ":" + ex.GetType().Name);
                    if (candidate != null)
                        TryRemoveTransientVegetationForSmoke(room, candidate);
                }
            }

            summary = "No dandelion vegetation target available. proto={" + protoSummary + "}, transientNotes=" + string.Join("|", notes.Take(8));
            return false;
        }

        private bool TryCreateTransientMatureVegetationForSmoke(Type dolocApi, object room, out object? vegetation, out object? renderer, out string summary)
        {
            vegetation = null;
            renderer = null;
            summary = string.Empty;

            Type? hostType = patcher?.ResolveType("DolocTown.IVegetationHost, Assembly-CSharp");
            if (hostType == null || !hostType.IsInstanceOfType(room))
            {
                summary = "current room is not an IVegetationHost. room=" + DescribeRoomForSmoke(room);
                return false;
            }

            MethodInfo? createVegetation = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "CreateVegetationNoRender" && m.GetParameters().Length == 2);
            if (createVegetation == null)
            {
                summary = "IVegetationHost.CreateVegetationNoRender was not found.";
                return false;
            }

            IReadOnlyList<object> protos = FindHarvestableVegetationProtosForSmoke(out string protoSummary);
            List<string> notes = new List<string>();
            foreach (object proto in protos)
            {
                string protoId = ReadStringMember(proto, "Id", proto.GetType().Name);
                object? candidate = null;
                try
                {
                    candidate = createVegetation.Invoke(room, new object[] { proto, false });
                    if (candidate == null)
                    {
                        candidate = TryCreateDirectTransientVegetationForSmoke(room, proto, out string directSummary);
                        if (candidate == null)
                        {
                            notes.Add(protoId + ":create-null,direct={" + directSummary + "}");
                            continue;
                        }

                        notes.Add(protoId + ":direct-create(" + directSummary + ")");
                    }

                    if (!TrySetVegetationMaxGrowthForSmoke(candidate, out string growthSummary))
                    {
                        notes.Add(protoId + ":growth-failed(" + growthSummary + ")");
                        TryRemoveTransientVegetationForSmoke(room, candidate);
                        continue;
                    }

                    if (!TryRenderVegetationForSmoke(dolocApi, room, candidate, out renderer, out string renderSummary) || renderer == null)
                    {
                        notes.Add(protoId + ":render-failed(" + renderSummary + ")");
                        TryRemoveTransientVegetationForSmoke(room, candidate);
                        continue;
                    }

                    vegetation = candidate;
                    summary = "transient:" + DescribeVegetationForSmoke(candidate) + ", proto={" + protoSummary + "}, growth={" + growthSummary + "}, render={" + renderSummary + "}";
                    return true;
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    notes.Add(protoId + ":" + ex.InnerException.GetType().Name);
                    if (candidate != null)
                        TryRemoveTransientVegetationForSmoke(room, candidate);
                }
                catch (Exception ex)
                {
                    notes.Add(protoId + ":" + ex.GetType().Name);
                    if (candidate != null)
                        TryRemoveTransientVegetationForSmoke(room, candidate);
                }
            }

            if (TryFindExistingMatureVegetationForSmoke(dolocApi, room, out vegetation, out renderer, out string existingSummary))
            {
                summary = "existing:" + existingSummary + ", proto={" + protoSummary + "}, transientNotes=" + string.Join("|", notes.Take(8));
                return true;
            }

            summary = "No mature vegetation target available. proto={" + protoSummary + "}, transientNotes=" + string.Join("|", notes.Take(8)) + ", existing={" + existingSummary + "}";
            return false;
        }

        private object? TryCreateDirectTransientVegetationForSmoke(object room, object proto, out string summary)
        {
            summary = string.Empty;
            object? manager = ReadMember(room, "DM_vegetation");
            MethodInfo? createVegetation = manager?.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "CreateVegetation" && m.GetParameters().Length == 3);
            if (manager == null || createVegetation == null)
            {
                summary = "DM_vegetation.CreateVegetation unavailable.";
                return null;
            }

            object? agentCell = ReadStaticMember(patcher?.ResolveType("DolocAPI, Assembly-CSharp"), "AgentRoomCellPosition");
            int anchorX = agentCell == null ? 4 : Math.Max(1, ReadIntMember(agentCell, "x", 4) + 3);
            int anchorY = agentCell == null ? 4 : Math.Max(1, ReadIntMember(agentCell, "y", 4));
            object? gridSize = ReadMember(room, "RoomGridSize");
            int width = Math.Max(1, ReadIntMember(proto, "Width", 1));
            if (gridSize != null)
            {
                anchorX = Math.Min(Math.Max(1, ReadIntMember(gridSize, "x", anchorX + width + 2) - width - 1), anchorX);
                anchorY = Math.Min(Math.Max(1, ReadIntMember(gridSize, "y", anchorY + 2) - 2), anchorY);
            }

            object? anchor = CreateVector2IntForSmoke(anchorX, anchorY);
            object? position = CreateEquipmentWorldPositionForSmoke(room, anchorX, anchorY, width);
            if (anchor == null || position == null)
            {
                summary = "Could not create anchor/position for direct vegetation.";
                return null;
            }

            object? vegetation = createVegetation.Invoke(manager, new[] { proto, anchor, position });
            if (vegetation == null)
            {
                summary = "CreateVegetation returned null.";
                return null;
            }

            WriteObjectMember(vegetation, "Host", room);
            object? terrain = ReadMember(room, "DM_terrain");
            MethodInfo? fillContent = terrain == null ? null : FindMethod(terrain.GetType(), "FillContent", 1);
            string terrainSummary = "terrain-fill-skipped";
            if (fillContent != null)
            {
                try
                {
                    fillContent.Invoke(terrain, new[] { vegetation });
                    terrainSummary = "terrain-fill-ok";
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    terrainSummary = "terrain-fill-" + ex.InnerException.GetType().Name;
                }
                catch (Exception ex)
                {
                    terrainSummary = "terrain-fill-" + ex.GetType().Name;
                }
            }

            summary = "anchor=" + anchorX + "," + anchorY + ", " + terrainSummary;
            return vegetation;
        }

        private IReadOnlyList<object> FindHarvestableVegetationProtosForSmoke(out string summary)
        {
            List<object> preferred = new List<object>();
            List<object> fallback = new List<object>();
            Type? dolocConfig = patcher?.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? tbVegetation = tables == null ? null : ReadMember(tables, "TbVegetation");
            object? dataList = tbVegetation == null ? null : ReadMember(tbVegetation, "DataList");
            if (!(dataList is IEnumerable enumerable))
            {
                summary = "TbVegetation.DataList unavailable.";
                return preferred;
            }

            int scanned = 0;
            foreach (object proto in enumerable)
            {
                scanned++;
                object? function = ReadMember(proto, "Function");
                string functionName = function == null ? string.Empty : function.GetType().Name;
                if (functionName.IndexOf("VegetationFuncCrop", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    preferred.Add(proto);
                    continue;
                }
                if (functionName.IndexOf("VegetationFuncGrowLuminous", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    functionName.IndexOf("VegetationFuncBerryThicket", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    fallback.Add(proto);
                }
            }

            preferred.AddRange(fallback);
            summary = "scanned=" + scanned + ", harvestable=" + preferred.Count + ", sample=" + string.Join("|", preferred.Take(5).Select(proto => ReadStringMember(proto, "Id", proto.GetType().Name)));
            return preferred;
        }

        private IReadOnlyList<object> FindDandelionVegetationProtosForSmoke(out string summary)
        {
            List<object> preferred = new List<object>();
            List<object> fallback = new List<object>();
            Type? dolocConfig = patcher?.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? tbVegetation = tables == null ? null : ReadMember(tables, "TbVegetation");
            object? dataList = tbVegetation == null ? null : ReadMember(tbVegetation, "DataList");
            if (!(dataList is IEnumerable enumerable))
            {
                summary = "TbVegetation.DataList unavailable.";
                return preferred;
            }

            int scanned = 0;
            foreach (object proto in enumerable)
            {
                scanned++;
                string id = ReadStringMember(proto, "Id", proto.GetType().Name);
                object? function = ReadMember(proto, "Function");
                string functionName = function == null ? string.Empty : function.GetType().Name;
                if (!TryGetFirstVegetationToolConstraint(proto, out string toolType, out _, out _))
                    continue;

                if (functionName.IndexOf("VegetationFuncDandelion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    id.IndexOf("dandelion", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    preferred.Add(proto);
                    continue;
                }

                if (functionName.IndexOf("VegetationFuncGrow", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    toolType.Equals("SICKLE", StringComparison.OrdinalIgnoreCase))
                {
                    fallback.Add(proto);
                }
            }

            summary = "scanned=" + scanned +
                ", dandelion=" + preferred.Count +
                ", fallback=" + fallback.Count +
                ", sample=" + string.Join("|", preferred.Concat(fallback).Take(4).Select(proto => ReadStringMember(proto, "Id", proto.GetType().Name)));
            return preferred.Count > 0 ? preferred : fallback;
        }

        private bool TryFindExistingMatureVegetationForSmoke(Type dolocApi, object room, out object? vegetation, out object? renderer, out string summary)
        {
            vegetation = null;
            renderer = null;
            object? manager = ReadMember(room, "DM_vegetation");
            object? allDatas = manager == null ? null : ReadMember(manager, "AllDatas");
            int scanned = 0;
            if (allDatas is IEnumerable enumerable)
            {
                foreach (object candidate in enumerable)
                {
                    scanned++;
                    string typeName = candidate.GetType().FullName ?? candidate.GetType().Name;
                    if (typeName.IndexOf("VegetationCrop", StringComparison.OrdinalIgnoreCase) < 0 &&
                        typeName.IndexOf("VegetationGrowLuminous", StringComparison.OrdinalIgnoreCase) < 0 &&
                        typeName.IndexOf("VegetationBerryThicket", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    if (!TrySetVegetationMaxGrowthForSmoke(candidate, out string growthSummary))
                        continue;

                    if (!TryRenderVegetationForSmoke(dolocApi, room, candidate, out renderer, out string renderSummary) || renderer == null)
                        continue;

                    vegetation = candidate;
                    summary = DescribeVegetationForSmoke(candidate) + ", scanned=" + scanned + ", growth={" + growthSummary + "}, render={" + renderSummary + "}";
                    return true;
                }
            }

            summary = "no existing harvestable vegetation. scanned=" + scanned;
            return false;
        }

        private bool TrySetVegetationMaxGrowthForSmoke(object vegetation, out string summary)
        {
            int maxLevel = ReadIntMember(vegetation, "maxLevel", -1);
            if (maxLevel < 0)
            {
                summary = "maxLevel unavailable. target=" + DescribeVegetationForSmoke(vegetation);
                return false;
            }

            MethodInfo? setGrowthLevel = FindMethod(vegetation.GetType(), "SetGrowthLevel", 2);
            if (setGrowthLevel == null)
            {
                summary = "SetGrowthLevel(int,bool) unavailable. target=" + DescribeVegetationForSmoke(vegetation);
                return false;
            }

            int before = ReadIntMember(vegetation, "currentLevel", -1);
            setGrowthLevel.Invoke(vegetation, new object[] { maxLevel, false });
            int after = ReadIntMember(vegetation, "currentLevel", -1);
            bool ok = after >= maxLevel && maxLevel > 0;
            summary = "level=" + before + "->" + after + "/" + maxLevel;
            return ok;
        }

        private bool TryRenderVegetationForSmoke(Type dolocApi, object room, object vegetation, out object? renderer, out string summary)
        {
            renderer = ReadMember(vegetation, "Renderer");
            if (renderer != null)
            {
                summary = "already-rendered:" + renderer.GetType().Name;
                return true;
            }

            Type? hostType = patcher?.ResolveType("DolocTown.IVegetationHost, Assembly-CSharp");
            Type? rendererType = patcher?.ResolveType("DolocTown.VegetationRenderer, Assembly-CSharp");
            object? entitySystem = ReadStaticMember(dolocApi, "EntitySystem");
            MethodInfo? next = entitySystem?.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "Next" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            MethodInfo? renderVegetation = hostType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "RenderVegetation" && m.GetParameters().Length == 2);
            if (hostType == null || rendererType == null || entitySystem == null || next == null || renderVegetation == null)
            {
                summary = "render dependencies unavailable. hostType=" + (hostType != null) + ", rendererType=" + (rendererType != null) + ", entitySystem=" + (entitySystem != null) + ", next=" + (next != null) + ", render=" + (renderVegetation != null);
                return false;
            }

            renderer = next.MakeGenericMethod(rendererType).Invoke(entitySystem, null);
            if (renderer == null)
            {
                summary = "EntitySystem.Next<VegetationRenderer>() returned null.";
                return false;
            }

            renderVegetation.Invoke(room, new[] { renderer, vegetation });
            object? attached = ReadMember(vegetation, "Renderer");
            bool ok = ReferenceEquals(attached, renderer);
            summary = "rendered=" + ok + ", renderer=" + renderer.GetType().FullName;
            return ok;
        }

        private bool TryInteractWithCurrentInteractableForSmoke(Type dolocApi, object interactable, out string summary)
        {
            summary = string.Empty;
            object? gameStateManager = ReadStaticMember(dolocApi, "gameStateManager");
            object? normalGameState = gameStateManager == null ? null : ReadMember(gameStateManager, "normalGameState");
            object? agentController = normalGameState == null ? null : ReadMember(normalGameState, "AgentController");
            object? interactableManager = agentController == null ? null : ReadMember(agentController, "interactableManager");
            MethodInfo? touch = interactableManager == null ? null : FindMethod(interactableManager.GetType(), "Touch", 1);
            MethodInfo? tryInteract = interactableManager == null ? null : FindMethod(interactableManager.GetType(), "TryInteract", 1);
            if (interactableManager == null || touch == null || tryInteract == null)
            {
                summary = "InteractableManagerEx Touch/TryInteract unavailable.";
                return false;
            }

            touch.Invoke(interactableManager, new[] { interactable });
            object? result = tryInteract.Invoke(interactableManager, new object[] { false });
            summary = "manager=" + interactableManager.GetType().FullName + ", target=" + interactable.GetType().FullName + ", result=" + (result is bool ok && ok);
            return result is bool interacted && interacted;
        }

        private void TryRemoveTransientVegetationForSmoke(object? room, object vegetation)
        {
            try
            {
                Type? hostType = patcher?.ResolveType("DolocTown.IVegetationHost, Assembly-CSharp");
                MethodInfo? removeVegetation = hostType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "RemoveVegetation" && m.GetParameters().Length == 1);
                if (room != null && hostType != null && hostType.IsInstanceOfType(room) && removeVegetation != null)
                    removeVegetation.Invoke(room, new[] { vegetation });
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke transient vegetation removal failed.", ex.ToString());
            }
        }

        private static string DescribeVegetationForSmoke(object vegetation)
        {
            object? anchor = ReadMember(vegetation, "Anchor");
            string name = ReadStringMember(vegetation, "VegetationName", vegetation.GetType().Name);
            int index = ReadIntMember(vegetation, "index", -1);
            int level = ReadIntMember(vegetation, "currentLevel", -1);
            int maxLevel = ReadIntMember(vegetation, "maxLevel", -1);
            string anchorText = anchor == null ? "unknown" : ReadIntMember(anchor, "x", 0) + "," + ReadIntMember(anchor, "y", 0);
            return name + "/" + (vegetation.GetType().FullName ?? vegetation.GetType().Name) + "/index=" + index + "/anchor=" + anchorText + "/level=" + level + "/" + maxLevel;
        }

        private static bool SummaryContainsMultiplier(string summary, double multiplier)
        {
            return summary.IndexOf("multiplier=" + multiplier.ToString("0.###"), StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool SummaryContainsKind(string summary, string kind)
        {
            return summary.IndexOf("kind=" + kind, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void SetConfigPendingValue(IConfigMenuPage page, string kind, string value, params string[] nameFragments)
        {
            IConfigMenuItem? item = page.Items.FirstOrDefault(candidate =>
                candidate.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase) &&
                nameFragments.Any(fragment => candidate.Name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0));
            if (item == null)
                throw new InvalidOperationException("Could not find config item kind=" + kind + " names=" + string.Join("/", nameFragments) + ".");
            if (!item.TrySetPendingValue(value, out string error))
                throw new InvalidOperationException("Could not stage config value for " + item.Name + ": " + error);
        }

        private void LogActionSpeedPending(string message, string statusKey = "Smoke.ActionSpeedTool")
        {
            if ((DateTimeOffset.Now - lastActionSpeedReadinessLog).TotalSeconds < 5)
                return;
            lastActionSpeedReadinessLog = DateTimeOffset.Now;
            runtime.RuntimeMonitor.Log("Smoke action-speed waiting: " + message);
            runtime.SetHookStatus(statusKey, "pending", "ActionSpeed smoke readiness", message);
        }

        private object? GenerateActionSpeedToolForSmoke(Type dolocApi)
        {
            foreach (string itemId in new[] { "old_pickaxe", "old_axe", "old_sickle" })
            {
                object? item = GenerateItemForSmoke(dolocApi, itemId);
                if (item != null && IsTypeOrBase(item.GetType(), "DolocTown.ItemTool"))
                    return item;
            }
            return null;
        }

        private object? GenerateFishingRodForSmoke(Type dolocApi)
        {
            foreach (string itemId in new[] { "carbon_fishrod", "bamboo_fishrod", "simple_fishrod", "old_fishrod" })
            {
                object? item = GenerateItemForSmoke(dolocApi, itemId);
                if (item != null && IsTypeOrBase(item.GetType(), "DolocTown.ItemFishingRod"))
                    return item;
            }
            return null;
        }

        private void LogOneActionFuelFeedPending(string message)
        {
            if ((DateTimeOffset.Now - lastOneActionReadinessLog).TotalSeconds < 5)
                return;
            lastOneActionReadinessLog = DateTimeOffset.Now;
            runtime.RuntimeMonitor.Log("Smoke one-action fuel/feed waiting: " + message);
            runtime.SetHookStatus("Smoke.OneActionFuelFeed", "pending", "AgentStateInteract.OnExit", message);
        }

        private void LogOneActionVegetationPending(string message)
        {
            if ((DateTimeOffset.Now - lastOneActionReadinessLog).TotalSeconds < 5)
                return;
            lastOneActionReadinessLog = DateTimeOffset.Now;
            runtime.RuntimeMonitor.Log("Smoke one-action vegetation waiting: " + message);
            runtime.SetHookStatus("Smoke.OneActionVegetation", "pending", "ToolCollider.HandleTools -> VegetationRenderer.OnFell", message);
        }

        private object? TryCreateTransientEquipmentForSmoke(Type dolocApi, object room, string targetTypeName, IReadOnlyList<string> equipmentIds, out string summary)
        {
            summary = string.Empty;
            Type? hostType = patcher?.ResolveType("DolocTown.IEquipmentHost, Assembly-CSharp");
            if (hostType == null || !hostType.IsInstanceOfType(room))
            {
                summary = "current room is not an IEquipmentHost. room=" + DescribeRoomForSmoke(room);
                return null;
            }

            MethodInfo? createEquipment = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "CreateEquipment" && m.GetParameters().Length == 6);
            if (createEquipment == null)
            {
                summary = "IEquipmentHost.CreateEquipment was not found.";
                return null;
            }

            List<string> notes = new List<string>();
            int attempted = 0;
            foreach (string equipmentId in equipmentIds)
            {
                object? proto = QueryEquipmentProtoForSmoke(dolocApi, equipmentId);
                if (proto == null)
                {
                    notes.Add(equipmentId + ":missing-proto");
                    continue;
                }

                object? coverSize = ReadMember(proto, "CoverSize");
                int width = coverSize == null ? 1 : Math.Max(1, ReadIntMember(coverSize, "x", 1));
                int height = coverSize == null ? 1 : Math.Max(1, ReadIntMember(coverSize, "y", 1));
                foreach ((int x, int y) in EnumerateSmokeEquipmentAnchors(dolocApi, room, width, height))
                {
                    attempted++;
                    object? anchor = CreateVector2IntForSmoke(x, y);
                    if (anchor == null)
                        continue;
                    if (!AreEquipmentCellsEmptyForSmoke(hostType, room, x, y, width, height))
                        continue;

                    object? worldPosition = CreateEquipmentWorldPositionForSmoke(room, x, y, width);
                    if (worldPosition == null)
                        continue;

                    try
                    {
                        object? equipment = createEquipment.Invoke(room, new object?[] { worldPosition, anchor, proto, false, null, -1 });
                        if (equipment == null)
                        {
                            notes.Add(equipmentId + "@" + x + "," + y + ":null");
                            continue;
                        }

                        if (IsTypeOrBase(equipment.GetType(), targetTypeName))
                        {
                            summary = "transient:" + equipmentId + "@" + x + "," + y + ", attempted=" + attempted + ", room=" + DescribeRoomForSmoke(room);
                            return equipment;
                        }

                        notes.Add(equipmentId + "@" + x + "," + y + ":wrong-type=" + equipment.GetType().FullName);
                        TryRemoveTransientEquipmentForSmoke(room, equipment);
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException != null)
                    {
                        notes.Add(equipmentId + "@" + x + "," + y + ":" + ex.InnerException.GetType().Name);
                    }
                    catch (Exception ex)
                    {
                        notes.Add(equipmentId + "@" + x + "," + y + ":" + ex.GetType().Name);
                    }
                }
            }

            summary = "transient-create-failed targetType=" + targetTypeName + ", attempted=" + attempted + ", notes=" + string.Join("|", notes.Take(8));
            return null;
        }

        private object? TryCreateTransientEquipmentNoRenderForSmoke(Type dolocApi, object room, string targetTypeName, IReadOnlyList<string> equipmentIds, out string summary)
        {
            summary = string.Empty;
            Type? hostType = patcher?.ResolveType("DolocTown.IEquipmentHost, Assembly-CSharp");
            if (hostType == null || !hostType.IsInstanceOfType(room))
            {
                summary = "room is not an IEquipmentHost. room=" + DescribeRoomForSmoke(room);
                return null;
            }

            MethodInfo? createEquipmentNoRender = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "CreateEquipmentNoRender" && m.GetParameters().Length == 4);
            if (createEquipmentNoRender == null)
            {
                summary = "IEquipmentHost.CreateEquipmentNoRender was not found.";
                return null;
            }

            List<string> notes = new List<string>();
            int attempted = 0;
            foreach (string equipmentId in equipmentIds)
            {
                object? proto = QueryEquipmentProtoForSmoke(dolocApi, equipmentId);
                if (proto == null)
                {
                    notes.Add(equipmentId + ":missing-proto");
                    continue;
                }

                object? coverSize = ReadMember(proto, "CoverSize");
                int width = coverSize == null ? 1 : Math.Max(1, ReadIntMember(coverSize, "x", 1));
                int height = coverSize == null ? 1 : Math.Max(1, ReadIntMember(coverSize, "y", 1));
                foreach ((int x, int y) in EnumerateSmokeEquipmentAnchors(dolocApi, room, width, height))
                {
                    attempted++;
                    object? anchor = CreateVector2IntForSmoke(x, y);
                    object? worldPosition = CreateEquipmentWorldPositionForSmoke(room, x, y, width);
                    if (anchor == null || worldPosition == null)
                        continue;
                    if (!AreEquipmentCellsEmptyForSmoke(hostType, room, x, y, width, height))
                        continue;

                    try
                    {
                        object? equipment = createEquipmentNoRender.Invoke(room, new object?[] { worldPosition, anchor, proto, false });
                        if (equipment == null)
                        {
                            notes.Add(equipmentId + "@" + x + "," + y + ":null");
                            continue;
                        }

                        if (IsTypeOrBase(equipment.GetType(), targetTypeName))
                        {
                            summary = "transient-no-render:" + equipmentId + "@" + x + "," + y + ", attempted=" + attempted + ", room=" + DescribeRoomForSmoke(room);
                            return equipment;
                        }

                        notes.Add(equipmentId + "@" + x + "," + y + ":wrong-type=" + equipment.GetType().FullName);
                        TryRemoveTransientEquipmentForSmoke(room, equipment);
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException != null)
                    {
                        notes.Add(equipmentId + "@" + x + "," + y + ":" + ex.InnerException.GetType().Name);
                    }
                    catch (Exception ex)
                    {
                        notes.Add(equipmentId + "@" + x + "," + y + ":" + ex.GetType().Name);
                    }
                }
            }

            summary = "transient-no-render-create-failed targetType=" + targetTypeName + ", attempted=" + attempted + ", notes=" + string.Join("|", notes.Take(8));
            return null;
        }

        private object? FindChestLocatorSmokeBuildingRoom(Type dolocApi, object archive, out string summary)
        {
            object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
            object? mainFarm = ReadMember(archive, "MainFarm");
            object? buildingManager = mainFarm == null ? null : ReadMember(mainFarm, "DM_building");
            object? buildings = buildingManager == null ? null : ReadMember(buildingManager, "Buildings");
            int scanned = 0;
            var samples = new List<string>();

            if (buildings is IEnumerable enumerable)
            {
                foreach (object? building in enumerable)
                {
                    if (building == null)
                        continue;
                    scanned++;
                    object? room = ReadMember(building, "room");
                    if (room == null)
                    {
                        samples.Add("building" + scanned + ":no-room");
                        continue;
                    }
                    samples.Add(DescribeRoomForSmoke(room));
                    if (ReferenceEquals(room, currentRoom))
                        continue;
                    if (ReadMember(room, "DM_equipment") == null)
                        continue;

                    summary = "selected=" + DescribeRoomForSmoke(room) + ", scanned=" + scanned + ", current=" + (currentRoom == null ? "none" : DescribeRoomForSmoke(currentRoom));
                    return room;
                }
            }

            summary = "scanned=" + scanned + ", current=" + (currentRoom == null ? "none" : DescribeRoomForSmoke(currentRoom)) + ", samples=" + string.Join(" | ", samples.Take(5));
            return null;
        }

        private string SelectZeroBaselineSmokeItemId(Type dolocApi, IReadOnlyList<string> candidates, out int baseline, out string summary)
        {
            baseline = 0;
            var notes = new List<string>();
            foreach (string candidate in candidates)
            {
                object? item = GenerateItemForSmoke(dolocApi, candidate, 1);
                if (item == null)
                {
                    notes.Add(candidate + ":missing");
                    continue;
                }

                int count = CountNativeItemForSmoke(dolocApi, candidate, checkBox: true);
                notes.Add(candidate + ":baseline=" + count);
                if (count == 0)
                {
                    baseline = count;
                    summary = string.Join("|", notes);
                    return candidate;
                }
            }

            summary = string.Join("|", notes);
            return string.Empty;
        }

        private static int CountNativeItemForSmoke(Type dolocApi, string itemId, bool checkBox)
        {
            MethodInfo? countItem = dolocApi.GetMethod("CountItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(bool) }, null);
            object? result = countItem == null ? null : countItem.Invoke(null, new object[] { itemId, checkBox });
            return result is int value ? value : -1;
        }

        private static bool CostNativeItemForSmoke(Type dolocApi, string itemId, int count, bool checkBox)
        {
            MethodInfo? costItem = dolocApi.GetMethod("CostItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int), typeof(bool) }, null);
            object? result = costItem == null ? null : costItem.Invoke(null, new object[] { itemId, count, checkBox });
            return result is bool value && value;
        }

        private static string FormatChestLocatorState(ChestLocatorEnhancerState state)
        {
            if (state == null)
                return "unknown";
            return "owner=" + state.OwnerId +
                ", status=" + state.Status +
                ", enabled=" + state.Enabled +
                ", hook=" + state.HookInstalled +
                ", applications=" + state.ExtensionApplications +
                ", base=" + state.LastBaseInventoryCount +
                ", appended=" + state.LastAppendedInventoryCount +
                ", roots=" + state.LastScannedRootCount +
                ", equipment=" + state.LastScannedEquipmentCount +
                ", cases=" + state.LastSharedCaseCount +
                ", storageBoxes=" + state.LastSharedStorageBoxCount +
                ", message=" + state.LastMessage;
        }

        private object? FindStrongPlantingGunSeedForSmoke(Type dolocApi, object basin, out string summary)
        {
            string basinSeedType = ReadPlantBasinSeedTypeForSmoke(basin);
            var notes = new List<string>();
            foreach (string seedId in new[] { "seed_endyam", "seed_thunder_grass", "seed_chinese_cabbage", "seed_scallion", "seed_pumpkin", "seed_wheat" })
            {
                object? seed = GenerateItemForSmoke(dolocApi, seedId, 1);
                if (seed == null || !IsTypeOrBase(seed.GetType(), "DolocTown.ItemSeed"))
                {
                    notes.Add(seedId + ":missing");
                    continue;
                }

                WriteBoolMember(seed, "enablePlantInInvalidSeason", true);
                string seedType = ReadSeedTypeForSmoke(seed);
                notes.Add(seedId + ":" + seedType);
                if (seedType.Equals(basinSeedType, StringComparison.OrdinalIgnoreCase))
                {
                    summary = "selected=" + seedId + ", seedType=" + seedType + ", basinSeedType=" + basinSeedType + ", candidates=" + string.Join("|", notes);
                    return seed;
                }
            }

            summary = "basinSeedType=" + basinSeedType + ", candidates=" + string.Join("|", notes);
            return null;
        }

        private static bool SwapInventoryItemAtForSmoke(object inventory, int slot, object item, out string summary)
        {
            try
            {
                MethodInfo? swapItem = FindMethod(inventory.GetType(), "SwapItem", 2);
                if (swapItem == null)
                {
                    summary = "LinearInventory.SwapItem was not available.";
                    return false;
                }

                object? leftover = swapItem.Invoke(inventory, new object[] { slot, item });
                if (leftover != null)
                {
                    summary = "slot=" + slot + ", leftover=" + ReadStringMember(leftover, "name", leftover.GetType().Name) + ", count=" + ReadIntMember(leftover, "count", 0);
                    return false;
                }

                object? placed = FindMethod(inventory.GetType(), "Read", 1)?.Invoke(inventory, new object[] { slot });
                summary = "slot=" + slot + ", item=" + (placed == null ? "null" : ReadStringMember(placed, "name", placed.GetType().Name)) + ", count=" + (placed == null ? 0 : ReadIntMember(placed, "count", 0));
                return placed != null;
            }
            catch (Exception ex)
            {
                summary = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static string FormatStrongPlantingGunState(StrongPlantingGunState state)
        {
            if (state == null)
                return "unknown";
            return "owner=" + state.OwnerId +
                ", status=" + state.Status +
                ", enabled=" + state.Enabled +
                ", slots=" + state.SlotCount +
                ", toolHook=" + state.ToolHookInstalled +
                ", uiHook=" + state.UiHookInstalled +
                ", expanded=" + state.ExpandedGunCount +
                ", visited=" + state.LastVisitedEquipmentCount +
                ", seed=" + state.LastSeedActions +
                ", film=" + state.LastFilmActions +
                ", fertilizer=" + state.LastFertilizerActions +
                ", water=" + state.LastWaterActions +
                ", consumed=" + state.LastConsumedItemCount +
                ", message=" + state.LastMessage;
        }

        private object? FindExistingEquipmentForSmoke(object room, string targetTypeName, string ratioMember, out string summary)
        {
            int scanned = 0;
            object? manager = ReadMember(room, "DM_equipment");
            object? allEquipments = manager == null ? null : ReadMember(manager, "AllEquipments");
            if (allEquipments is IEnumerable enumerable)
            {
                foreach (object equipment in enumerable)
                {
                    if (equipment == null)
                        continue;
                    scanned++;
                    if (!IsTypeOrBase(equipment.GetType(), targetTypeName))
                        continue;
                    double ratio = ReadDoubleMember(equipment, ratioMember, -1);
                    if (ratio < 0 || ratio < 0.999)
                    {
                        summary = "existing:" + DescribeEquipmentForSmoke(equipment) + ", ratio=" + FormatRatio(ratio) + ", scanned=" + scanned;
                        return equipment;
                    }
                }
            }

            summary = "no existing low target. targetType=" + targetTypeName + ", scanned=" + scanned;
            return null;
        }

        private string? SelectFillItemIdForSmoke(Type dolocApi, object equipment, string kind, IReadOnlyList<string> itemIds, out string summary)
        {
            List<string> notes = new List<string>();
            MethodInfo? isSuitableFuel = kind.Equals("FuelMachine", StringComparison.OrdinalIgnoreCase)
                ? FindMethod(equipment.GetType(), "IsSuitableFuel", 1)
                : null;
            MethodInfo? isAnimalFeeds = kind.Equals("Feeder", StringComparison.OrdinalIgnoreCase)
                ? FindMethod(equipment.GetType(), "IsAnimalFeeds", 2)
                : null;

            foreach (string itemId in itemIds)
            {
                object? item = GenerateItemForSmoke(dolocApi, itemId, 1);
                if (item == null)
                {
                    notes.Add(itemId + ":missing-item");
                    continue;
                }

                if (isSuitableFuel != null)
                {
                    object? result = isSuitableFuel.Invoke(equipment, new object[] { item });
                    if (result is bool suitable && suitable)
                    {
                        summary = "selected=" + itemId + ", validator=IsSuitableFuel";
                        return itemId;
                    }
                    notes.Add(itemId + ":not-fuel");
                    continue;
                }

                if (isAnimalFeeds != null)
                {
                    object? proto = ReadMember(item, "proto");
                    object?[] args = { proto, 0 };
                    object? result = proto == null ? null : isAnimalFeeds.Invoke(equipment, args);
                    if (result is bool isFeed && isFeed)
                    {
                        summary = "selected=" + itemId + ", validator=IsAnimalFeeds, energy=" + args[1];
                        return itemId;
                    }
                    notes.Add(itemId + ":not-feed");
                }
            }

            summary = "no candidate accepted. kind=" + kind + ", notes=" + string.Join("|", notes);
            return null;
        }

        private bool TryPlaceSmokeItemInQuickSlot(Type dolocApi, object item, int slot, out object? inventory, out object? originalSlotItem, out string summary)
        {
            inventory = null;
            originalSlotItem = null;
            summary = string.Empty;

            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? inventorySystem = archive == null ? null : ReadMember(archive, "InventorySystem");
            inventory = inventorySystem == null ? null : ReadMember(inventorySystem, "inventory");
            if (inventory == null)
            {
                summary = "InventorySystem.inventory was not available.";
                return false;
            }

            MethodInfo? read = FindMethod(inventory.GetType(), "Read", 1);
            MethodInfo? take = FindMethod(inventory.GetType(), "Take", 1);
            MethodInfo? placeItemAt = FindMethod(inventory.GetType(), "PlaceItemAt", 2);
            if (read == null || take == null || placeItemAt == null)
            {
                summary = "LinearInventory Read/Take/PlaceItemAt methods were not available.";
                return false;
            }

            originalSlotItem = read.Invoke(inventory, new object[] { slot });
            if (originalSlotItem != null)
                take.Invoke(inventory, new object[] { slot });

            object? leftover = placeItemAt.Invoke(inventory, new object[] { slot, item });
            if (leftover != null && ReadIntMember(leftover, "count", 0) > 0)
            {
                summary = "Could not place full smoke stack in quick slot " + slot + "; leftover=" + ReadIntMember(leftover, "count", 0) + ".";
                return false;
            }

            object? uiSystem = ReadStaticMember(dolocApi, "uiSystem");
            object? quickInventory = uiSystem == null ? null : ReadMember(uiSystem, "inventoryQuick");
            if (quickInventory == null || !WriteIntMember(quickInventory, "selectedIndex", slot))
            {
                summary = "Could not select quick inventory slot " + slot + ".";
                return false;
            }

            FindMethod(dolocApi, "QuickSelectCurrentItem", 0)?.Invoke(null, null);
            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            int count = selectedItem == null ? 0 : ReadIntMember(selectedItem, "count", 0);
            if (selectedItem == null || count <= 0)
            {
                summary = "SelectedItem was not available after quick slot placement.";
                return false;
            }

            summary = "quickSlot=" + slot + ", selected=" + ReadStringMember(selectedItem, "name", selectedItem.GetType().Name) + ", count=" + count;
            return true;
        }

        private void RestoreSmokeQuickSlot(Type dolocApi, object? inventory, int slot, object? originalSlotItem)
        {
            try
            {
                FindMethod(dolocApi, "QuickDeselectCurrentItem", 0)?.Invoke(null, null);
                if (inventory == null)
                    return;
                MethodInfo? take = FindMethod(inventory.GetType(), "Take", 1);
                MethodInfo? swapItem = FindMethod(inventory.GetType(), "SwapItem", 2);
                take?.Invoke(inventory, new object[] { slot });
                if (originalSlotItem != null && swapItem != null)
                    swapItem.Invoke(inventory, new object[] { slot, originalSlotItem });
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke quick-slot restore failed.", ex.ToString());
            }
        }

        private bool TrySelectEquipmentForSmoke(Type dolocApi, object equipment, object anchor, out string summary)
        {
            summary = string.Empty;
            object? gameStateManager = ReadStaticMember(dolocApi, "gameStateManager");
            object? normalGameState = gameStateManager == null ? null : ReadMember(gameStateManager, "normalGameState");
            object? agentController = normalGameState == null ? null : ReadMember(normalGameState, "AgentController");
            object? scanner = agentController == null ? null : ReadMember(agentController, "RoomScanner");
            MethodInfo? onPosChanged = scanner == null ? null : FindMethod(scanner.GetType(), "OnPosChanged", 1);
            if (scanner == null || onPosChanged == null)
            {
                summary = "RoomScanner.OnPosChanged unavailable.";
                return false;
            }

            onPosChanged.Invoke(scanner, new[] { anchor });
            object? selected = ReadStaticMember(dolocApi, "SelectedEquipment");
            if (ReferenceEquals(selected, equipment))
            {
                summary = "scanner-selected";
                return true;
            }

            if (WriteObjectMember(scanner, "_currentEquipment", equipment) || WriteObjectMember(scanner, "CurrentEquipment", equipment))
            {
                selected = ReadStaticMember(dolocApi, "SelectedEquipment");
                if (ReferenceEquals(selected, equipment))
                {
                    summary = "scanner-forced-current-equipment";
                    return true;
                }
            }

            summary = "selected=" + (selected == null ? "null" : DescribeEquipmentForSmoke(selected)) + ", expected=" + DescribeEquipmentForSmoke(equipment);
            return false;
        }

        private void TryClearRoomScannerSelectionForSmoke(Type dolocApi)
        {
            try
            {
                object? gameStateManager = ReadStaticMember(dolocApi, "gameStateManager");
                object? normalGameState = gameStateManager == null ? null : ReadMember(gameStateManager, "normalGameState");
                object? agentController = normalGameState == null ? null : ReadMember(normalGameState, "AgentController");
                object? scanner = agentController == null ? null : ReadMember(agentController, "RoomScanner");
                FindMethod(scanner?.GetType(), "ClearBuffer", 0)?.Invoke(scanner, null);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke room scanner clear failed.", ex.ToString());
            }
        }

        private bool TryPointAgentCellTipAtEquipmentForSmoke(Type dolocApi, object equipment, out string summary)
        {
            summary = string.Empty;
            object? anchor = ReadMember(equipment, "Anchor");
            object? agentCell = ReadStaticMember(dolocApi, "AgentRoomCellPosition");
            object? uiSystem = ReadStaticMember(dolocApi, "uiSystem");
            object? basicTip = uiSystem == null ? null : ReadMember(uiSystem, "basicTip");
            object? cellTip = basicTip == null ? null : ReadMember(basicTip, "AgentCellTip");
            if (anchor == null || agentCell == null || cellTip == null)
            {
                summary = "anchor/AgentRoomCellPosition/AgentCellTip unavailable.";
                return false;
            }

            int dx = ReadIntMember(anchor, "x", 0) - ReadIntMember(agentCell, "x", 0);
            int dy = ReadIntMember(anchor, "y", 0) - ReadIntMember(agentCell, "y", 0);
            object? offset = CreateVector2IntForSmoke(dx, dy);
            if (offset == null || !WriteObjectMember(cellTip, "oringinOffset", offset))
            {
                summary = "Could not write AgentCellTip offset dx=" + dx + ",dy=" + dy + ".";
                return false;
            }
            WriteBoolMember(cellTip, "flipWhenFaceLeft", false);

            object? room = ReadStaticMember(dolocApi, "CurrentRoom");
            Type? hostType = patcher?.ResolveType("DolocTown.IEquipmentHost, Assembly-CSharp");
            MethodInfo? getEquipment = hostType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "GetEquipment")
                        return false;
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType.FullName == "UnityEngine.Vector2Int";
                });
            object? cellAnchor = ReadMember(cellTip, "CellAnchor");
            object? selected = room == null || hostType == null || getEquipment == null || cellAnchor == null ? null : getEquipment.Invoke(room, new[] { cellAnchor });
            if (!ReferenceEquals(selected, equipment))
            {
                summary = "cellTip anchor selected=" + (selected == null ? "null" : DescribeEquipmentForSmoke(selected)) + ", expected=" + DescribeEquipmentForSmoke(equipment) + ", offset=" + dx + "," + dy + ".";
                return false;
            }

            summary = "offset=" + dx + "," + dy + ", anchor=" + ReadIntMember(anchor, "x", 0) + "," + ReadIntMember(anchor, "y", 0);
            return true;
        }

        private bool TryInvokeUseItemContinuesForSmoke(Type dolocApi, float dt, out string summary)
        {
            summary = string.Empty;
            object? gameStateManager = ReadStaticMember(dolocApi, "gameStateManager");
            object? normalGameState = gameStateManager == null ? null : ReadMember(gameStateManager, "normalGameState");
            object? agentController = normalGameState == null ? null : ReadMember(normalGameState, "AgentController");
            MethodInfo? useItemContinues = agentController == null ? null : FindMethod(agentController.GetType(), "UseItemContinues", 1);
            if (agentController == null || useItemContinues == null)
            {
                summary = "AgentController.UseItemContinues(float) unavailable.";
                return false;
            }

            int beforeApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
            int beforeContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
            useItemContinues.Invoke(agentController, new object[] { dt });
            int afterApplications = experimentalApi?.ActionSpeedApplicationCount ?? 0;
            int afterContinuous = experimentalApi?.ActionSpeedContinuousUseApplicationCount ?? 0;
            summary = "dt=" + dt.ToString("0.###") +
                ", currentState=" + ReadCurrentAgentStateForSmoke(dolocApi) +
                ", actionSpeedDelta=" + (afterApplications - beforeApplications) +
                ", continuousDelta=" + (afterContinuous - beforeContinuous) +
                ", continuous=" + (experimentalApi?.LastActionSpeedContinuousUseSummary ?? "none");
            return true;
        }

        private bool TryRunCurrentAgentInteractExitForSmoke(Type dolocApi, out string summary)
        {
            summary = string.Empty;
            object? agent = ReadStaticMember(dolocApi, "agent");
            object? stateManager = agent == null ? null : ReadMember(agent, "StateManager");
            object? current = stateManager == null ? null : ReadMember(stateManager, "current");
            if (current == null)
            {
                summary = "AgentStateManager.current unavailable.";
                return false;
            }
            if (!IsTypeOrBase(current.GetType(), "DolocTown.AgentStateInteract"))
            {
                summary = "current state is " + (current.GetType().FullName ?? current.GetType().Name) + ", expected DolocTown.AgentStateInteract.";
                return false;
            }

            MethodInfo? onExit = FindMethod(current.GetType(), "OnExit", 0);
            if (onExit == null)
            {
                summary = "AgentStateInteract.OnExit unavailable.";
                return false;
            }

            int before = experimentalApi?.OneActionApplicationCount ?? 0;
            onExit.Invoke(current, null);
            int after = experimentalApi?.OneActionApplicationCount ?? 0;
            summary = "state=" + current.GetType().Name + ", oneActionDelta=" + (after - before);
            return true;
        }

        private void TryEnterIdleStateForSmoke(Type dolocApi)
        {
            try
            {
                Type? idleType = patcher?.ResolveType("DolocTown.AgentStateIdle, Assembly-CSharp");
                object? agent = ReadStaticMember(dolocApi, "agent");
                object? stateManager = agent == null ? null : ReadMember(agent, "StateManager");
                MethodInfo? overwrite = stateManager?.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "Overwrite" || !m.IsGenericMethodDefinition)
                            return false;
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType == typeof(bool);
                    });
                if (idleType != null && overwrite != null)
                    overwrite.MakeGenericMethod(idleType).Invoke(stateManager, new object[] { false });
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke idle-state restore failed.", ex.ToString());
            }
        }

        private static string ReadCurrentAgentStateForSmoke(Type dolocApi)
        {
            object? agent = ReadStaticMember(dolocApi, "agent");
            object? stateManager = agent == null ? null : ReadMember(agent, "StateManager");
            object? current = stateManager == null ? null : ReadMember(stateManager, "current");
            return current == null ? "unknown" : current.GetType().Name;
        }

        private int ReadQuickSlotItemCount(object? inventory, int slot)
        {
            if (inventory == null)
                return 0;
            MethodInfo? read = FindMethod(inventory.GetType(), "Read", 1);
            object? item = read?.Invoke(inventory, new object[] { slot });
            return item == null ? 0 : ReadIntMember(item, "count", 0);
        }

        private static int ReadInventoryItemCount(object? inventory, int slot)
        {
            if (inventory == null)
                return 0;
            MethodInfo? read = FindMethod(inventory.GetType(), "Read", 1);
            object? item = read?.Invoke(inventory, new object[] { slot });
            return item == null ? 0 : ReadIntMember(item, "count", 0);
        }

        private string ReadQuickSlotItemName(object? inventory, int slot)
        {
            if (inventory == null)
                return string.Empty;
            MethodInfo? read = FindMethod(inventory.GetType(), "Read", 1);
            object? item = read?.Invoke(inventory, new object[] { slot });
            return item == null ? string.Empty : ReadStringMember(item, "name", item.GetType().Name);
        }

        private static string ReadSeedTypeForSmoke(object seed)
        {
            object? seedProto = ReadMember(seed, "seedProto");
            if (seedProto == null)
                return "unknown";
            string seedType = ReadStringMember(seedProto, "SeedType", string.Empty);
            if (!string.IsNullOrWhiteSpace(seedType))
                return seedType;
            object? seedTypeRef = ReadMember(seedProto, "SeedType_Ref");
            return seedTypeRef == null ? "unknown" : ReadStringMember(seedTypeRef, "Id", "unknown");
        }

        private static string ReadPlantBasinSeedTypeForSmoke(object basin)
        {
            object? seedTypeInfo = ReadMember(basin, "SeedTypeInfo");
            return seedTypeInfo == null ? "unknown" : ReadStringMember(seedTypeInfo, "Id", "unknown");
        }

        private bool TrySetCropMatureForSmoke(object crop, out string summary)
        {
            object? seedProto = ReadMember(crop, "seedProto");
            int matureLevel = seedProto == null ? -1 : ReadIntMember(seedProto, "MatureLevel", -1);
            if (matureLevel < 0)
            {
                summary = "Crop seedProto.MatureLevel unavailable.";
                return false;
            }

            MethodInfo? debugSetLevel = FindMethod(crop.GetType(), "DEBUG_SetLevel", 2);
            if (debugSetLevel == null)
            {
                summary = "Crop.DEBUG_SetLevel(bool,int) unavailable.";
                return false;
            }

            int beforeLevel = ReadIntMember(crop, "CurrentLevel", -1);
            bool beforeMature = ReadBoolMember(crop, "isMature", false);
            debugSetLevel.Invoke(crop, new object[] { false, matureLevel });
            int afterLevel = ReadIntMember(crop, "CurrentLevel", -1);
            bool afterMature = ReadBoolMember(crop, "isMature", false);
            summary = "level=" + beforeLevel + "->" + afterLevel + "/" + matureLevel + ", mature=" + beforeMature + "->" + afterMature;
            return afterMature && afterLevel >= matureLevel;
        }

        private object? QueryEquipmentProtoForSmoke(Type dolocApi, string equipmentId)
        {
            MethodInfo? queryEquipment = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "QueryEquipment")
                        return false;
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length == 2 && parameters[0].ParameterType == typeof(string) && parameters[1].IsOut;
                });
            if (queryEquipment == null)
                return null;
            object?[] args = { equipmentId, null };
            object? result = queryEquipment.Invoke(null, args);
            return result is bool ok && ok ? args[1] : null;
        }

        private IEnumerable<(int x, int y)> EnumerateSmokeEquipmentAnchors(Type dolocApi, object room, int width, int height)
        {
            object? agentCell = ReadStaticMember(dolocApi, "AgentRoomCellPosition");
            int baseX = agentCell == null ? 4 : ReadIntMember(agentCell, "x", 4);
            int baseY = agentCell == null ? 4 : ReadIntMember(agentCell, "y", 4);
            object? gridSize = ReadMember(room, "RoomGridSize");
            int maxX = gridSize == null ? baseX + 24 : Math.Max(1, ReadIntMember(gridSize, "x", baseX + 24) - width - 1);
            int maxY = gridSize == null ? baseY + 12 : Math.Max(1, ReadIntMember(gridSize, "y", baseY + 12) - height - 1);

            (int dx, int dy)[] offsets =
            {
                (2, 0), (4, 0), (6, 0), (-4, 0), (-6, 0),
                (0, 2), (2, 2), (4, 2), (-4, 2), (0, -2),
                (8, 2), (-8, 2), (2, 4), (-2, 4), (6, 4)
            };

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach ((int dx, int dy) in offsets)
            {
                int x = Math.Max(1, Math.Min(maxX, baseX + dx));
                int y = Math.Max(1, Math.Min(maxY, baseY + dy));
                string key = x + "," + y;
                if (seen.Add(key))
                    yield return (x, y);
            }
        }

        private bool AreEquipmentCellsEmptyForSmoke(Type hostType, object room, int anchorX, int anchorY, int width, int height)
        {
            MethodInfo? getEquipment = hostType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "GetEquipment")
                        return false;
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType.FullName == "UnityEngine.Vector2Int";
                });
            if (getEquipment == null)
                return false;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    object? cell = CreateVector2IntForSmoke(anchorX + x, anchorY + y);
                    if (cell == null)
                        return false;
                    object? existing = getEquipment.Invoke(room, new[] { cell });
                    if (existing != null)
                        return false;
                }
            }
            return true;
        }

        private object? CreateEquipmentWorldPositionForSmoke(object room, int anchorX, int anchorY, int width)
        {
            object? roomPosition = ReadMember(room, "RoomPosition");
            double roomX = ReadDoubleMember(roomPosition, "x", 0);
            double roomY = ReadDoubleMember(roomPosition, "y", 0);
            double worldX = roomX + (anchorX + width * 0.5) * 1.5;
            double worldY = roomY + anchorY * 1.5;
            return CreateVector3ForSmoke(worldX, worldY, 0);
        }

        private object? CreateVector2IntForSmoke(int x, int y)
        {
            Type? vector2Int = patcher?.ResolveType("UnityEngine.Vector2Int, UnityEngine.CoreModule") ??
                patcher?.ResolveType("UnityEngine.Vector2Int, UnityEngine");
            return vector2Int == null ? null : Activator.CreateInstance(vector2Int, new object[] { x, y });
        }

        private object? CreateVector2ForSmoke(double x, double y)
        {
            Type? vector2 = patcher?.ResolveType("UnityEngine.Vector2, UnityEngine.CoreModule") ??
                patcher?.ResolveType("UnityEngine.Vector2, UnityEngine");
            return vector2 == null ? null : Activator.CreateInstance(vector2, new object[] { (float)x, (float)y });
        }

        private object? CreateVector3ForSmoke(double x, double y, double z)
        {
            Type? vector3 = patcher?.ResolveType("UnityEngine.Vector3, UnityEngine.CoreModule") ??
                patcher?.ResolveType("UnityEngine.Vector3, UnityEngine");
            return vector3 == null ? null : Activator.CreateInstance(vector3, new object[] { (float)x, (float)y, (float)z });
        }

        private void TryRemoveTransientEquipmentForSmoke(object? room, object equipment)
        {
            try
            {
                Type? hostType = patcher?.ResolveType("DolocTown.IEquipmentHost, Assembly-CSharp");
                MethodInfo? removeEquipment = hostType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "RemoveEquipment" && m.GetParameters().Length == 4);
                if (room != null && hostType != null && hostType.IsInstanceOfType(room) && removeEquipment != null)
                    removeEquipment.Invoke(room, new object?[] { equipment, false, false, true });
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke transient equipment removal failed.", ex.ToString());
            }
        }

        private void TryRemoveTransientDungeonResourceForSmoke(object? room, object resource)
        {
            try
            {
                Type? hostType = patcher?.ResolveType("DolocTown.IDungeonResourceHost, Assembly-CSharp");
                MethodInfo? removeResource = hostType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "RemoveDungeonResource" && m.GetParameters().Length == 1);
                if (room != null && hostType != null && hostType.IsInstanceOfType(room) && removeResource != null)
                    removeResource.Invoke(room, new object[] { resource });
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke transient dungeon resource removal failed.", ex.ToString());
            }
        }

        private static string DescribeEquipmentForSmoke(object equipment)
        {
            object? anchor = ReadMember(equipment, "Anchor");
            object? proto = ReadMember(equipment, "proto") ?? ReadMember(equipment, "Proto");
            object? coverSize = proto == null ? null : ReadMember(proto, "CoverSize");
            object? sceneAsset = proto == null ? null : ReadMember(proto, "SceneAsset");
            object? function = proto == null ? null : ReadMember(proto, "Function");
            object? renderer = ReadMember(equipment, "Renderer");
            object? transform = renderer == null ? null : ReadMember(renderer, "transform");
            object? localScale = transform == null ? null : ReadMember(transform, "localScale");
            object? inventory = ReadMember(equipment, "inventory");
            string name = ReadStringMember(equipment, "Name", ReadStringMember(equipment, "Title", equipment.GetType().Name));
            int index = ReadIntMember(equipment, "index", -1);
            string anchorText = anchor == null ? "unknown" : ReadIntMember(anchor, "x", 0) + "," + ReadIntMember(anchor, "y", 0);
            string coverText = coverSize == null ? "unknown" : ReadIntMember(coverSize, "x", 0) + "x" + ReadIntMember(coverSize, "y", 0);
            string sceneText = sceneAsset == null ? "unknown" : FirstNonEmpty(ReadStringMember(sceneAsset, "AssetUrl", string.Empty), sceneAsset.ToString() ?? string.Empty);
            string scaleText = localScale == null ? "unknown" : ReadDoubleMember(localScale, "x", 0).ToString("0.##") + "x" + ReadDoubleMember(localScale, "y", 0).ToString("0.##");
            string storageText = inventory == null
                ? "none"
                : ReadIntMember(inventory, "filledCount", 0) + "/" + ReadIntMember(inventory, "capacity", 0) + "/line=" + ReadIntMember(equipment, "lineCapacity", 0);
            return name + "/" + (equipment.GetType().FullName ?? equipment.GetType().Name) + "/index=" + index + "/anchor=" + anchorText + "/cover=" + coverText + "/scene=" + sceneText + "/function=" + (function == null ? "unknown" : function.GetType().Name) + "/rendererScale=" + scaleText + "/storage=" + storageText;
        }

        private object? FindOrCreateFishingPoolForSmoke(Type fishingPoolType, Type dolocApi, out string source)
        {
            List<string> details = new List<string>();
            object[] activePools = FindUnityObjects(fishingPoolType);
            details.Add("activeScenePools=" + activePools.Length);
            foreach (object existing in activePools)
            {
                string existingName = ReadStringMember(existing, "PoolName", string.Empty);
                if (string.IsNullOrWhiteSpace(existingName))
                    continue;

                if (TryRollFishForSmoke(dolocApi, existingName, out string sceneFishId, out string sceneRollDetails))
                {
                    source = "scene:" + existingName + ", fish=" + sceneFishId + ", " + string.Join(", ", details);
                    return existing;
                }

                details.Add("scenePoolNoRoll=" + existingName + "(" + sceneRollDetails + ")");
            }

            object[] allPools = FindUnityObjects(fishingPoolType, includeInactive: true);
            if (allPools.Length != activePools.Length)
                details.Add("inactiveOrHiddenScenePools=" + Math.Max(0, allPools.Length - activePools.Length));
            foreach (object existing in allPools)
            {
                string existingName = ReadStringMember(existing, "PoolName", string.Empty);
                if (string.IsNullOrWhiteSpace(existingName) || !TryRollFishForSmoke(dolocApi, existingName, out string hiddenFishId, out string _))
                    continue;

                if (activePools.Contains(existing))
                    continue;

                if (TrySetUnityComponentActiveForSmoke(existing, true, out string activationDetails))
                {
                    details.Add("activatedHiddenScenePool=" + existingName + "(" + hiddenFishId + ", " + activationDetails + ")");
                    source = "activated-scene:" + existingName + ", fish=" + hiddenFishId + ", " + string.Join(", ", details);
                    return existing;
                }

                details.Add("hiddenScenePool=" + existingName + "(" + hiddenFishId + ", activation=" + activationDetails + ")");
                source = "hidden-scene:" + existingName + ", fish=" + hiddenFishId + ", " + string.Join(", ", details);
                return existing;
            }

            IReadOnlyList<string> poolNames = FindFishingPoolNamesForSmoke(out string configDetails);
            details.Add(configDetails);
            foreach (string candidatePoolName in poolNames)
            {
                if (TryRollFishForSmoke(dolocApi, candidatePoolName, out string fishId, out string rollDetails))
                {
                    object? pool = CreateTransientFishingPoolForSmoke(fishingPoolType, candidatePoolName, out string createDetails);
                    details.Add("selected=" + candidatePoolName + ", fish=" + fishId + ", " + createDetails);
                    if (pool != null)
                    {
                        source = "transient:" + candidatePoolName + ", fish=" + fishId + ", " + string.Join(", ", details);
                        return pool;
                    }
                }
                else
                {
                    details.Add("candidateNoRoll=" + candidatePoolName + "(" + rollDetails + ")");
                }
            }

            source = string.Join(", ", details);
            return null;
        }

        private static bool TrySetUnityComponentActiveForSmoke(object component, bool active, out string details)
        {
            details = string.Empty;
            try
            {
                object? gameObject = ReadMember(component, "gameObject");
                MethodInfo? setActive = gameObject?.GetType().GetMethod("SetActive", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(bool) }, null);
                if (gameObject == null || setActive == null)
                {
                    details = "missing-gameObject";
                    return false;
                }

                int activatedParents = 0;
                object? transform = ReadMember(gameObject, "transform");
                for (object? parent = transform == null ? null : ReadMember(transform, "parent"); parent != null; parent = ReadMember(parent, "parent"))
                {
                    object? parentGameObject = ReadMember(parent, "gameObject");
                    MethodInfo? parentSetActive = parentGameObject?.GetType().GetMethod("SetActive", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(bool) }, null);
                    if (parentGameObject == null || parentSetActive == null)
                        break;
                    parentSetActive.Invoke(parentGameObject, new object[] { active });
                    activatedParents++;
                    if (activatedParents >= 16)
                        break;
                }

                setActive.Invoke(gameObject, new object[] { active });
                bool activeSelf = ReadBoolMember(gameObject, "activeSelf", false);
                bool activeInHierarchy = ReadBoolMember(gameObject, "activeInHierarchy", false);
                details = "activeSelf=" + activeSelf + ", activeInHierarchy=" + activeInHierarchy + ", activatedParents=" + activatedParents;
                return activeInHierarchy;
            }
            catch (Exception ex)
            {
                details = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private object? CreateTransientFishingPoolForSmoke(Type fishingPoolType, string poolName, out string details)
        {
            Type? gameObjectType = patcher?.ResolveType("UnityEngine.GameObject, UnityEngine.CoreModule") ?? patcher?.ResolveType("UnityEngine.GameObject, UnityEngine");
            MethodInfo? addComponent = gameObjectType?.GetMethod("AddComponent", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type) }, null);
            if (gameObjectType == null || addComponent == null)
            {
                details = "create=missing-gameobject";
                return null;
            }

            object? gameObject = Activator.CreateInstance(gameObjectType, new object[] { "DTMAPI.SmokeFishingPool" });
            object? pool = gameObject == null ? null : addComponent.Invoke(gameObject, new object[] { fishingPoolType });
            if (pool == null || !WriteObjectMember(pool, "poolName", poolName))
            {
                details = "create=failed";
                return null;
            }

            details = "create=ok";
            return pool;
        }

        private IReadOnlyList<string> FindFishingPoolNamesForSmoke(out string details)
        {
            List<string> names = new List<string>();
            List<string> notes = new List<string>();
            Type? dolocConfig = patcher?.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            if (dolocConfig == null)
            {
                details = "config=missing-dolocconfig";
                return names;
            }

            object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (tables == null)
            {
                details = "config=missing-tables";
                return names;
            }

            object? tbFishingPool = tables == null ? null : ReadMember(tables, "TbFishingPool");
            if (tbFishingPool == null)
            {
                details = "config=missing-tbfishingpool";
                return names;
            }

            object? dataList = tbFishingPool == null ? null : ReadMember(tbFishingPool, "DataList");
            if (dataList is IEnumerable enumerable)
            {
                int count = 0;
                foreach (object info in enumerable)
                {
                    count++;
                    string id = ReadStringMember(info, "Id", string.Empty);
                    object? fishes = ReadMember(info, "Fishes_Ref") ?? ReadMember(info, "Fishes");
                    if (!string.IsNullOrWhiteSpace(id) && HasAnyEnumerableItem(fishes))
                    {
                        AddUnique(names, id);
                        if (notes.Count < 5)
                            notes.Add(id);
                    }
                }
                details = "config=dataList:" + count + ", candidates=" + names.Count + (notes.Count == 0 ? string.Empty : ", sample=" + string.Join("|", notes));
                return names;
            }

            object? dataMap = ReadMember(tbFishingPool!, "DataMap");
            if (dataMap is IDictionary dictionary)
            {
                foreach (object key in dictionary.Keys)
                {
                    if (key is string id && !string.IsNullOrWhiteSpace(id))
                        AddUnique(names, id);
                }
                details = "config=dataMap:" + dictionary.Count + ", candidates=" + names.Count;
                return names;
            }

            if (dataMap is IEnumerable mapEnumerable)
            {
                int count = 0;
                foreach (object entry in mapEnumerable)
                {
                    count++;
                    if (entry == null)
                        continue;
                    string id = ReadMember(entry, "Key") as string ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(id))
                        AddUnique(names, id);
                }
                details = "config=dataMapEnumerable:" + count + ", candidates=" + names.Count;
                return names;
            }

            details = "config=missing-datalist-and-datamap";
            return names;
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (values.Any(existing => existing.Equals(value, StringComparison.Ordinal)))
                return;
            values.Add(value);
        }

        private static bool TryRollFishForSmoke(Type dolocApi, string poolName, out string fishId, out string details)
        {
            fishId = string.Empty;
            MethodInfo? rollFish = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "RollFish")
                        return false;
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length == 2 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].ParameterType == typeof(int);
                });
            if (rollFish == null)
            {
                details = "missing-rollfish";
                return false;
            }

            int attempts = 0;
            foreach (int toolLevel in new[] { 5, 4, 3, 2, 1, 0 })
            {
                attempts++;
                object? fish = rollFish.Invoke(null, new object[] { poolName, toolLevel });
                if (fish == null)
                    continue;

                fishId = ReadStringMember(fish, "Id", fish.GetType().Name);
                details = "toolLevel=" + toolLevel + ", attempts=" + attempts;
                return true;
            }
            details = "attempts=" + attempts + ", no-fish";
            return false;
        }

        private static bool HasAnyEnumerableItem(object? value)
        {
            if (!(value is IEnumerable enumerable))
                return false;
            foreach (object _ in enumerable)
                return true;
            return false;
        }

        private bool TryAutoOpenAnimalPanel()
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                List<object> animals = FindAnimalsForSmoke();
                if (animals.Count == 0)
                    throw new InvalidOperationException("No save animal was available for animal panel UI smoke.");

                EnsureAnimalProgressForSmoke(animals);
                if (!TryFindAnimalPanelRoomForSmoke(animals, out object? room, out string source) || room == null)
                    throw new InvalidOperationException("No animal room was available for animal panel UI smoke.");

                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? animalPanelUiState = patcher.ResolveType("DolocTown.AnimalPanelUiState, Assembly-CSharp");
                if (dolocApi == null || animalPanelUiState == null)
                    throw new MissingMemberException("DolocAPI or AnimalPanelUiState was not visible.");

                MethodInfo? enterUi = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "EnterUI" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
                MethodInfo? handleStartUpArgs = animalPanelUiState.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "HandleStartUpArgs")
                            return false;
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(room);
                    });
                if (enterUi == null || handleStartUpArgs == null)
                    throw new MissingMethodException("AnimalPanelUiState EnterUI/HandleStartUpArgs path was not found.");

                Type delegateType = typeof(Func<,>).MakeGenericType(animalPanelUiState, typeof(bool));
                ParameterExpression stateParameter = Expression.Parameter(animalPanelUiState, "state");
                Expression roomConstant = Expression.Constant(room, handleStartUpArgs.GetParameters()[0].ParameterType);
                MethodCallExpression call = Expression.Call(stateParameter, handleStartUpArgs, roomConstant);
                Delegate startUpDelegate = Expression.Lambda(delegateType, call, stateParameter).Compile();

                runtime.SetHookStatus("Smoke.AnimalPanelUi", "pending", "DolocAPI.EnterUI(AnimalPanelUiState)", "Opening official AnimalPanel UI; waiting for AnimalViewer.Show evidence.");
                object? state = enterUi.MakeGenericMethod(animalPanelUiState).Invoke(null, new object[] { startUpDelegate });
                if (state == null)
                    throw new InvalidOperationException("AnimalPanelUiState could not be entered.");
                TryRefreshAnimalPanelForSmoke(state);

                runtime.RuntimeMonitor.Log("Smoke automation opened animal panel UI source=" + source + " animals=" + animals.Count + ".");
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke animal panel UI open failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AnimalPanelUi", "failed", "AnimalPanelUiState", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private void TryRefreshAnimalPanelForSmoke(object state)
        {
            try
            {
                object? panel = ReadMember(state, "panel");
                MethodInfo? refreshView = panel?.GetType().GetMethod("RefreshView", BindingFlags.Public | BindingFlags.Instance);
                MethodInfo? refreshViewer = panel?.GetType().GetMethod("RefreshViewer", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo? select = panel?.GetType().GetMethod("Select", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
                refreshView?.Invoke(panel, null);
                int evidenceIndex = panel == null ? 0 : FindAnimalProgressDataIndex(panel);
                refreshViewer?.Invoke(panel, new object[] { evidenceIndex });
                select?.Invoke(panel, new object[] { evidenceIndex });
                refreshViewer?.Invoke(panel, new object[] { evidenceIndex });
                if (panel != null)
                    runtime.RuntimeMonitor.Log("Smoke automation refreshed animal panel viewer selection panel=" + panel.GetType().FullName + " evidenceIndex=" + evidenceIndex + " refreshView=" + (refreshView != null) + " refreshViewer=" + (refreshViewer != null) + " select=" + (select != null) + ".");
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke animal panel refresh failed.", ex.ToString());
            }
        }

        private int FindAnimalProgressDataIndex(object panel)
        {
            object? currentDatas = ReadMember(panel, "currentDatas") ?? ReadMember(panel, "<currentDatas>k__BackingField");
            if (currentDatas is Array array)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    object? data = array.GetValue(i);
                    if (experimentalApi != null && experimentalApi.HasAnimalProgressRowsForSmoke(data, out _))
                        return i;
                }
            }
            return 0;
        }

        private bool TryExerciseFishRoeTooltipForSmoke()
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    throw new MissingMemberException("DolocAPI was not visible.");

                string? roeItemId = FindFishRoeItemId();
                if (string.IsNullOrWhiteSpace(roeItemId))
                    throw new MissingMemberException("Could not find an ItemFunctionFishRoe item id.");
                string nonNullRoeItemId = roeItemId!;

                MethodInfo? generateItem = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] p = m.GetParameters();
                        return m.Name == "GenerateItem" &&
                            p.Length >= 1 &&
                            p.Length <= 2 &&
                            p[0].ParameterType == typeof(string) &&
                            (p.Length == 1 || p[1].ParameterType == typeof(int));
                    });
                if (generateItem == null)
                    throw new MissingMethodException("DolocAPI.GenerateItem(string, int) was not found.");

                object? item = generateItem.GetParameters().Length == 1
                    ? generateItem.Invoke(null, new object[] { nonNullRoeItemId })
                    : generateItem.Invoke(null, new object[] { nonNullRoeItemId, 1 });
                if (item == null)
                    throw new InvalidOperationException("DolocAPI.GenerateItem returned null for " + nonNullRoeItemId + ".");

                item.GetType().GetMethod("SetFishName", BindingFlags.Public | BindingFlags.Instance)?.Invoke(item, new object[] { "fish" });
                string title = item.GetType().GetProperty("title", BindingFlags.Public | BindingFlags.Instance)?.GetValue(item) as string ?? string.Empty;
                string description = item.GetType().GetProperty("description", BindingFlags.Public | BindingFlags.Instance)?.GetValue(item) as string ?? string.Empty;
                string detail = item.GetType().GetMethod("GetDetailInfo", BindingFlags.Public | BindingFlags.Instance)?.Invoke(item, null) as string ?? string.Empty;
                bool ok = title.IndexOf("(鱼)", StringComparison.Ordinal) >= 0 || description.IndexOf("Hatches:", StringComparison.Ordinal) >= 0 || detail.IndexOf("Hatches:", StringComparison.Ordinal) >= 0;
                if (!ok)
                    throw new InvalidOperationException("Fish roe display hooks did not append expected text. title=" + title + " detail=" + detail);

                runtime.RuntimeMonitor.Log("Smoke exercise FishRoeTooltip OK item=" + nonNullRoeItemId + " title=" + title + " detail=" + detail.Replace(Environment.NewLine, " | "));
                runtime.SetHookStatus("Smoke.FishRoeTooltip", "verified", "ItemFishRoe title/description/detail", "Generated fish roe item and observed decorated tooltip text.");
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke fish roe tooltip exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.FishRoeTooltip", "failed", "ItemFishRoe title/description/detail", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private bool TryExerciseAnimalViewerForSmoke()
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                List<object> animals = FindAnimalsForSmoke();
                if (animals.Count == 0)
                    throw new InvalidOperationException("No save animal was available in this save/location.");

                Type? dataType = patcher.ResolveType("DolocTown.UI.AnimalFullInfoData, Assembly-CSharp");
                if (dataType == null)
                    throw new MissingMemberException("AnimalFullInfoData was not visible.");

                string lastDescription = string.Empty;
                foreach (object animal in animals)
                {
                    if (!TryConstructAnimalViewerData(dataType, animal, out object? data, out string stateDescription))
                        continue;
                    lastDescription = stateDescription;
                    if (experimentalApi != null && experimentalApi.HasAnimalProgressRowsForSmoke(data, out string rowSummary))
                    {
                        runtime.RuntimeMonitor.Log("Smoke exercise AnimalViewerRendering OK animal=" + GetAnimalId(animal) + " rows=" + rowSummary + " stateDescription=" + stateDescription.Replace(Environment.NewLine, " | "));
                        runtime.SetHookStatus("Smoke.AnimalViewerRendering", "verified", "AnimalFullInfoData(Animal)", "Constructed animal viewer data and observed independent hidden-produce progress row.");
                        return true;
                    }
                }

                foreach (object animal in animals)
                {
                    if (!TrySeedAnimalHusbandryForSmoke(animal, out string seedSummary))
                        continue;
                    runtime.RuntimeMonitor.Log("Smoke exercise seeded transient animal husbandry progress for " + seedSummary + ".");

                    if (!TryConstructAnimalViewerData(dataType, animal, out object? data, out string stateDescription))
                        continue;
                    lastDescription = stateDescription;
                    if (experimentalApi != null && experimentalApi.HasAnimalProgressRowsForSmoke(data, out string rowSummary))
                    {
                        runtime.RuntimeMonitor.Log("Smoke exercise AnimalViewerRendering OK animal=" + GetAnimalId(animal) + " rows=" + rowSummary + " stateDescription=" + stateDescription.Replace(Environment.NewLine, " | "));
                        runtime.SetHookStatus("Smoke.AnimalViewerRendering", "verified", "AnimalFullInfoData(Animal)", "Constructed animal viewer data and observed independent hidden-produce progress row after transient in-memory husbandry progress seed.");
                        return true;
                    }
                }

                throw new InvalidOperationException("Animal viewer data did not include independent special produce progress rows across " + animals.Count + " animal(s). lastStateDescription=" + lastDescription);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke animal viewer exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AnimalViewerRendering", "failed", "AnimalFullInfoData(Animal)", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private void EnsureAnimalProgressForSmoke(List<object> animals)
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dataType = patcher.ResolveType("DolocTown.UI.AnimalFullInfoData, Assembly-CSharp");
                if (dataType == null)
                    return;

                foreach (object animal in animals)
                {
                    if (TryConstructAnimalViewerData(dataType, animal, out object? data, out _) && experimentalApi != null && experimentalApi.HasAnimalProgressRowsForSmoke(data, out _))
                        return;
                }

                foreach (object animal in animals)
                {
                    if (!TrySeedAnimalHusbandryForSmoke(animal, out string seedSummary))
                        continue;
                    runtime.RuntimeMonitor.Log("Smoke exercise seeded transient animal husbandry progress for real UI path " + seedSummary + ".");
                    if (TryConstructAnimalViewerData(dataType, animal, out object? data, out _) && experimentalApi != null && experimentalApi.HasAnimalProgressRowsForSmoke(data, out _))
                        return;
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke animal progress preparation failed.", ex.ToString());
            }
        }

        private static bool HasAnimalProgressMarker(string stateDescription)
        {
            return !string.IsNullOrWhiteSpace(stateDescription) &&
                (stateDescription.IndexOf("Special produce:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 stateDescription.IndexOf("隐藏产物:", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 stateDescription.IndexOf("隐藏产物：", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 (stateDescription.IndexOf("<color=#", StringComparison.OrdinalIgnoreCase) >= 0 &&
                  stateDescription.IndexOf("█", StringComparison.OrdinalIgnoreCase) >= 0 &&
                  stateDescription.IndexOf("/", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private string? FindFishRoeItemId()
        {
            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocConfig = patcher.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? tbItem = tables?.GetType().GetProperty("TbItem", BindingFlags.Public | BindingFlags.Instance)?.GetValue(tables);
            object? dataMap = tbItem?.GetType().GetProperty("DataMap", BindingFlags.Public | BindingFlags.Instance)?.GetValue(tbItem);
            if (!(dataMap is IEnumerable entries))
                return null;

            foreach (object entry in entries)
            {
                Type entryType = entry.GetType();
                object? key = entryType.GetProperty("Key")?.GetValue(entry);
                object? itemInfo = entryType.GetProperty("Value")?.GetValue(entry);
                object? function = itemInfo?.GetType().GetProperty("Function", BindingFlags.Public | BindingFlags.Instance)?.GetValue(itemInfo);
                if (function?.GetType().FullName != "DolocTown.Config.Item.ItemFunctionFishRoe")
                    continue;
                object? id = itemInfo?.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)?.GetValue(itemInfo);
                return id as string ?? key?.ToString();
            }
            return null;
        }

        private bool TryConstructAnimalViewerData(Type dataType, object animal, out object? data, out string stateDescription)
        {
            data = null;
            stateDescription = string.Empty;
            ConstructorInfo? constructor = dataType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(c =>
                {
                    ParameterInfo[] p = c.GetParameters();
                    return p.Length == 1 && p[0].ParameterType.IsInstanceOfType(animal);
                });
            if (constructor == null)
                throw new MissingMethodException("AnimalFullInfoData(Animal) constructor was not found.");

            data = constructor.Invoke(new[] { animal });
            stateDescription = dataType.GetField("stateDescription", BindingFlags.Public | BindingFlags.Instance)?.GetValue(data) as string ?? string.Empty;
            return data != null;
        }

        private bool TrySeedAnimalHusbandryForSmoke(object animal, out string summary)
        {
            summary = string.Empty;
            string animalId = GetAnimalId(animal);
            if (string.IsNullOrWhiteSpace(animalId))
                return false;

            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocConfig = patcher.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? tbHusbandry = tables?.GetType().GetProperty("TbHusbandry", BindingFlags.Public | BindingFlags.Instance)?.GetValue(tables);
            object? info = tbHusbandry?.GetType().GetMethod("GetOrDefault", BindingFlags.Public | BindingFlags.Instance)?.Invoke(tbHusbandry, new object[] { animalId });
            object? dataList = info?.GetType().GetProperty("HusbandryDatas", BindingFlags.Public | BindingFlags.Instance)?.GetValue(info);
            object? first = FirstFromEnumerable(dataList);
            if (first == null)
                return false;

            string outputId = first.GetType().GetProperty("Output", BindingFlags.Public | BindingFlags.Instance)?.GetValue(first) as string ?? string.Empty;
            object? thresholdValue = first.GetType().GetProperty("Threshold", BindingFlags.Public | BindingFlags.Instance)?.GetValue(first);
            if (string.IsNullOrWhiteSpace(outputId) || thresholdValue == null)
                return false;

            int threshold = Convert.ToInt32(thresholdValue);
            if (threshold <= 0)
                return false;

            MethodInfo? setHusbandryValue = animal.GetType().GetMethod("DEBUG_SetHusbandryValue", BindingFlags.Public | BindingFlags.Instance);
            if (setHusbandryValue == null)
                return false;

            int value = Math.Max(1, threshold - 1);
            setHusbandryValue.Invoke(animal, new object[] { outputId, value });
            summary = animalId + "/" + outputId + "=" + value + "/" + threshold;
            return true;
        }

        private static string GetAnimalId(object animal)
        {
            return animal.GetType().GetProperty("protoName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(animal) as string ?? animal.GetType().Name;
        }

        private List<object> FindAnimalsForSmoke()
        {
            patcher ??= new HarmonyReflectionPatcher(runtime);
            var animals = new List<object>();
            Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
            object? room = dolocApi?.GetProperty("CurrentRoom", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            AddAnimalsFromRoom(animals, room);
            AddAnimalsFromArchive(animals, dolocApi);
            return animals;
        }

        private bool TryFindAnimalPanelRoomForSmoke(List<object> animals, out object? room, out string source)
        {
            foreach (object animal in animals)
            {
                object? animalRoom = ReadMember(animal, "currentRoom");
                if (HasAnimalsInRoom(animalRoom))
                {
                    room = animalRoom;
                    source = "animal.currentRoom";
                    return true;
                }
            }

            patcher ??= new HarmonyReflectionPatcher(runtime);
            Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
            object? currentRoom = dolocApi?.GetProperty("CurrentRoom", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (HasAnimalsInRoom(currentRoom))
            {
                room = currentRoom;
                source = "DolocAPI.CurrentRoom";
                return true;
            }

            object? archive = dolocApi?.GetProperty("archiveHandle", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? mainFarm = archive?.GetType().GetProperty("MainFarm", BindingFlags.Public | BindingFlags.Instance)?.GetValue(archive);
            if (HasAnimalsInRoom(mainFarm))
            {
                room = mainFarm;
                source = "archiveHandle.MainFarm";
                return true;
            }

            object? farmData = archive?.GetType().GetProperty("farmData", BindingFlags.Public | BindingFlags.Instance)?.GetValue(archive);
            object? fallbackFarm = farmData?.GetType().GetProperty("MainFarm", BindingFlags.Public | BindingFlags.Instance)?.GetValue(farmData);
            if (HasAnimalsInRoom(fallbackFarm))
            {
                room = fallbackFarm;
                source = "archiveHandle.farmData.MainFarm";
                return true;
            }

            room = null;
            source = string.Empty;
            return false;
        }

        private bool HasAnimalsInRoom(object? room)
        {
            if (room == null)
                return false;
            var animals = new List<object>();
            AddAnimalsFromRoom(animals, room);
            return animals.Count > 0;
        }

        private void AddAnimalsFromArchive(List<object> animals, Type? dolocApi)
        {
            object? archive = dolocApi?.GetProperty("archiveHandle", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? mainFarm = archive?.GetType().GetProperty("MainFarm", BindingFlags.Public | BindingFlags.Instance)?.GetValue(archive);
            AddAnimalsFromRoom(animals, mainFarm);

            object? farmData = archive?.GetType().GetProperty("farmData", BindingFlags.Public | BindingFlags.Instance)?.GetValue(archive);
            object? fallbackFarm = farmData?.GetType().GetProperty("MainFarm", BindingFlags.Public | BindingFlags.Instance)?.GetValue(farmData);
            AddAnimalsFromRoom(animals, fallbackFarm);
        }

        private void AddAnimalsFromRoom(List<object> animals, object? room)
        {
            if (room == null)
                return;

            AddAnimalsFromEnumerable(animals, room.GetType().GetProperty("AllAnimals", BindingFlags.Public | BindingFlags.Instance)?.GetValue(room));

            object? animalSystem = room.GetType().GetProperty("animalSystem", BindingFlags.Public | BindingFlags.Instance)?.GetValue(room);
            AddAnimalsFromEnumerable(animals, animalSystem?.GetType().GetProperty("Animals", BindingFlags.Public | BindingFlags.Instance)?.GetValue(animalSystem));

            object? manager = room.GetType().GetProperty("DM_animal", BindingFlags.Public | BindingFlags.Instance)?.GetValue(room);
            AddAnimalsFromEnumerable(animals, manager?.GetType().GetProperty("AllAnimals", BindingFlags.Public | BindingFlags.Instance)?.GetValue(manager));
        }

        private static void AddAnimalsFromEnumerable(List<object> animals, object? value)
        {
            if (!(value is IEnumerable enumerable))
                return;
            foreach (object item in enumerable)
            {
                if (item != null && !animals.Contains(item))
                    animals.Add(item);
            }
        }

        private static object? FirstFromEnumerable(object? value)
        {
            if (!(value is IEnumerable enumerable))
                return null;
            foreach (object item in enumerable)
                return item;
            return null;
        }

        private bool TryPrepareOneActionResourceHitWorldForSmoke(Type dolocApi, Type resourceRendererType, out string pendingReason)
        {
            pendingReason = string.Empty;
            object[] renderers = FindUnityObjects(resourceRendererType);
            if (CountRenderedResources(renderers) > 0)
                return true;

            object? currentRoom = dolocApi.GetProperty("CurrentRoom", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (currentRoom == null)
            {
                pendingReason = "Waiting for DolocAPI.CurrentRoom after save load.";
                return false;
            }

            if (!TryGetStaticBoolProperty(dolocApi, "IsNormalState"))
            {
                pendingReason = "Waiting for NormalGameState before tool-hit smoke. room=" + DescribeRoomForSmoke(currentRoom);
                return false;
            }

            if (!ReadBoolMember(currentRoom, "IsRenderNow", false) && !ReadBoolMember(currentRoom, "isRenderNow", false))
            {
                TryRenderRoomForSmoke(currentRoom);
                if (!ReadBoolMember(currentRoom, "IsRenderNow", false) && !ReadBoolMember(currentRoom, "isRenderNow", false))
                {
                    pendingReason = "Waiting for current room render before tool-hit smoke. room=" + DescribeRoomForSmoke(currentRoom);
                    return false;
                }
            }

            TryRenderAllResourcesForSmoke(currentRoom);
            renderers = FindUnityObjects(resourceRendererType);
            if (CountRenderedResources(renderers) > 0)
                return true;

            bool isMainFarm = IsMainFarmRoomForSmoke(dolocApi, currentRoom);
            if (!isMainFarm && !autoExerciseOneActionMainFarmRequested && TryEnterMainFarmForOneActionSmoke(dolocApi, currentRoom, out string transitionSummary))
            {
                pendingReason = transitionSummary;
                return false;
            }

            if (!isMainFarm && autoExerciseOneActionMainFarmRequested)
            {
                pendingReason = "Waiting for main farm transition before tool-hit smoke. room=" + DescribeRoomForSmoke(currentRoom);
                return false;
            }

            if (TryCreateTransientOneActionResourceForSmoke(currentRoom, out string createdSummary))
            {
                runtime.RuntimeMonitor.Log(createdSummary);
                renderers = FindUnityObjects(resourceRendererType);
                if (CountRenderedResources(renderers) > 0)
                    return true;

                pendingReason = "Waiting one frame for transient resource renderer. " + createdSummary;
                return false;
            }

            throw new InvalidOperationException("No rendered or creatable one-action resource was available for smoke. room=" + DescribeRoomForSmoke(currentRoom));
        }

        private static int CountRenderedResources(IEnumerable<object> renderers)
        {
            int count = 0;
            foreach (object renderer in renderers)
            {
                object? resource = ReadMember(renderer, "DungeonResource");
                if (resource != null && !IsRemoved(resource))
                    count++;
            }
            return count;
        }

        private bool TryRenderRoomForSmoke(object room)
        {
            try
            {
                MethodInfo? render = FindMethod(room.GetType(), "Render", 1);
                if (render == null)
                    return false;
                render.Invoke(room, new object[] { false });
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action room render probe failed.", ex.ToString());
                return false;
            }
        }

        private bool TryRenderAllResourcesForSmoke(object room)
        {
            try
            {
                Type? hostType = patcher?.ResolveType("DolocTown.IDungeonResourceHost, Assembly-CSharp");
                MethodInfo? renderAllResources = FindMethod(hostType, "RenderAllResources", 0);
                if (renderAllResources == null || hostType == null || !hostType.IsInstanceOfType(room))
                    return false;
                renderAllResources.Invoke(room, null);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action resource render probe failed.", ex.ToString());
                return false;
            }
        }

        private bool TryEnterMainFarmForOneActionSmoke(Type dolocApi, object currentRoom, out string summary)
        {
            summary = string.Empty;
            try
            {
                object? archiveHandle = dolocApi.GetProperty("archiveHandle", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                object? mainFarm = archiveHandle == null ? null : ReadMember(archiveHandle, "MainFarm");
                object? geometry = mainFarm == null ? null : ReadMember(mainFarm, "Geometry");
                object? entryPosition = geometry == null ? null : ReadMember(geometry, "DefaultEntryPosition");
                if (mainFarm == null || entryPosition == null)
                    return false;

                MethodInfo? enterFarm = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] parameters = m.GetParameters();
                        return m.Name == "EnterFarm" &&
                            parameters.Length == 3 &&
                            parameters[0].ParameterType == typeof(string);
                    });
                if (enterFarm == null)
                    return false;

                object? result = enterFarm.Invoke(null, new object?[] { string.Empty, entryPosition, null });
                if (result is bool entered && !entered)
                    return false;

                autoExerciseOneActionMainFarmRequested = true;
                summary = "Current room has no rendered one-action resource; requested official main farm transition for smoke. from=" + DescribeRoomForSmoke(currentRoom) + ", to=" + DescribeRoomForSmoke(mainFarm);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke one-action main farm transition failed.", ex.ToString());
                return false;
            }
        }

        private bool TryEnterMainFarmForActionSpeedInteractionSmoke(Type dolocApi, object currentRoom, out string summary)
        {
            summary = string.Empty;
            try
            {
                object? archiveHandle = dolocApi.GetProperty("archiveHandle", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                object? mainFarm = archiveHandle == null ? null : ReadMember(archiveHandle, "MainFarm");
                object? geometry = mainFarm == null ? null : ReadMember(mainFarm, "Geometry");
                object? entryPosition = geometry == null ? null : ReadMember(geometry, "DefaultEntryPosition");
                if (mainFarm == null || entryPosition == null)
                    return false;

                MethodInfo? enterFarm = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] parameters = m.GetParameters();
                        return m.Name == "EnterFarm" &&
                            parameters.Length == 3 &&
                            parameters[0].ParameterType == typeof(string);
                    });
                if (enterFarm == null)
                    return false;

                object? result = enterFarm.Invoke(null, new object?[] { string.Empty, entryPosition, null });
                if (result is bool entered && !entered)
                    return false;

                autoExerciseActionSpeedInteractionMainFarmRequested = true;
                summary = "Current room is indoor; requested official main farm transition for action-speed interaction smoke. from=" + DescribeRoomForSmoke(currentRoom) + ", to=" + DescribeRoomForSmoke(mainFarm);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke action-speed interaction main farm transition failed.", ex.ToString());
                return false;
            }
        }

        private bool TryCreateTransientOneActionResourceForSmoke(object room, out string summary)
        {
            return TryCreateTransientOneActionResourceForSmoke(room, null, out summary);
        }

        private bool TryCreateTransientOneActionResourceForSmoke(object room, string? targetKind, out string summary)
        {
            return TryCreateTransientOneActionResourceForSmoke(room, targetKind, null, out summary);
        }

        private bool TryCreateTransientOneActionResourceForSmoke(object room, string? targetKind, string? resourceNameContains, out string summary)
        {
            summary = string.Empty;
            Type? hostType = patcher?.ResolveType("DolocTown.IDungeonResourceHost, Assembly-CSharp");
            MethodInfo? createResource = FindMethod(hostType, "CreateDungeonResource", 1);
            if (hostType == null || createResource == null || !hostType.IsInstanceOfType(room))
                return false;

            int attempted = 0;
            foreach (object proto in FindOneActionResourceProtosForSmoke(targetKind, resourceNameContains))
            {
                attempted++;
                try
                {
                    object? resource = createResource.Invoke(room, new object[] { proto });
                    if (resource == null)
                        continue;
                    string resourceName = ReadStringMember(resource, "ResourceName", resource.GetType().Name);
                    string resourceClass = ReadResourceClass(resource);
                    string kind = GetOneActionResourceKind(resource);
                    summary = "Smoke created transient one-action resource through IDungeonResourceHost.CreateDungeonResource. room=" + DescribeRoomForSmoke(room) + ", targetKind=" + (targetKind ?? "any") + ", resourceNameContains=" + (resourceNameContains ?? "any") + ", resource=" + resourceName + ", kind=" + kind + ", class=" + resourceClass + ", attemptedProtos=" + attempted;
                    return true;
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Transient one-action resource create failed for a candidate.", ex.InnerException.ToString());
                }
                catch (Exception ex)
                {
                    runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Transient one-action resource create failed for a candidate.", ex.ToString());
                }
            }

            summary = "No transient one-action resource candidate could be created. room=" + DescribeRoomForSmoke(room) + ", targetKind=" + (targetKind ?? "any") + ", resourceNameContains=" + (resourceNameContains ?? "any") + ", attemptedProtos=" + attempted;
            return false;
        }

        private bool TryEnsureOneActionWrongToolMatrixResourcesForSmoke(Type dolocApi, Type resourceRendererType, out string summary)
        {
            summary = string.Empty;
            object? currentRoom = dolocApi.GetProperty("CurrentRoom", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            if (currentRoom == null)
            {
                summary = "CurrentRoom unavailable; could not create missing matrix resources.";
                return false;
            }

            HashSet<string> observedKinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (object renderer in FindUnityObjects(resourceRendererType))
            {
                object? resource = ReadMember(renderer, "DungeonResource");
                if (resource == null || IsRemoved(resource))
                    continue;
                string kind = GetOneActionResourceKind(resource);
                if (!string.IsNullOrWhiteSpace(kind))
                    observedKinds.Add(kind);
            }

            List<string> setupNotes = new List<string>();
            foreach (string kind in OneActionWrongToolTargetKinds)
            {
                if (observedKinds.Contains(kind) || autoExerciseOneActionWrongToolCreateAttempts.Contains(kind))
                    continue;

                autoExerciseOneActionWrongToolCreateAttempts.Add(kind);
                if (TryCreateTransientOneActionResourceForSmoke(currentRoom, kind, out string createSummary))
                {
                    setupNotes.Add("created:" + kind);
                    runtime.RuntimeMonitor.Log(createSummary);
                }
                else
                {
                    setupNotes.Add("missing:" + kind + ":" + createSummary);
                }
            }

            summary = "observedBefore=" + (observedKinds.Count == 0 ? "none" : string.Join(",", observedKinds.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))) + ", setup=" + (setupNotes.Count == 0 ? "none" : string.Join(" ; ", setupNotes));
            return setupNotes.Count > 0;
        }

        private IEnumerable<object> FindOneActionResourceProtosForSmoke(string? targetKind = null, string? resourceNameContains = null)
        {
            Type? dolocConfig = patcher?.ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? tbResource = tables == null ? null : ReadMember(tables, "TbResource");
            object? dataList = tbResource == null ? null : ReadMember(tbResource, "DataList");
            if (!(dataList is IEnumerable enumerable))
                yield break;

            foreach (object proto in enumerable)
            {
                string id = ReadStringMember(proto, "Id", string.Empty);
                if (string.IsNullOrWhiteSpace(id))
                    id = ReadStringMember(proto, "id", string.Empty);
                if (!string.IsNullOrWhiteSpace(resourceNameContains) &&
                    id.IndexOf(resourceNameContains, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                string resourceClass = ReadMember(proto, "ResourceClass")?.ToString() ?? string.Empty;
                object? resourceTypeRef = ReadMember(proto, "ResourceType_Ref");
                string classTypeName = resourceTypeRef == null ? string.Empty : ReadStringMember(resourceTypeRef, "ClassTypeName", string.Empty);
                string kind = GetOneActionProtoKind(resourceClass, classTypeName);
                if (!string.IsNullOrWhiteSpace(kind) && (string.IsNullOrWhiteSpace(targetKind) || kind.Equals(targetKind, StringComparison.OrdinalIgnoreCase)))
                    yield return proto;
            }
        }

        private static bool IsMainFarmRoomForSmoke(Type dolocApi, object room)
        {
            object? archiveHandle = dolocApi.GetProperty("archiveHandle", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? mainFarm = archiveHandle == null ? null : ReadMember(archiveHandle, "MainFarm");
            return mainFarm != null && ReferenceEquals(mainFarm, room);
        }

        private static string DescribeRoomForSmoke(object room)
        {
            return "type=" + room.GetType().Name +
                ", roomId=" + ReadStringMember(room, "RoomId", string.Empty) +
                ", title=" + ReadStringMember(room, "Title", string.Empty) +
                ", inHouse=" + ReadBoolMember(room, "IsInHouse", false) +
                ", renderNow=" + (ReadBoolMember(room, "IsRenderNow", false) || ReadBoolMember(room, "isRenderNow", false)) +
                ", resources=" + CountRoomResourcesForSmoke(room);
        }

        private static int CountRoomResourcesForSmoke(object room)
        {
            object? manager = ReadMember(room, "DM_dungeonResource");
            object? total = manager == null ? null : ReadMember(manager, "ResourceTotalCount");
            if (total != null)
                return Convert.ToInt32(total);
            object? resources = manager == null ? null : ReadMember(manager, "AllDungeonResources");
            if (!(resources is IEnumerable enumerable))
                return -1;
            int count = 0;
            foreach (object _ in enumerable)
                count++;
            return count;
        }

        private object[] FindUnityObjects(Type type, bool includeInactive = false)
        {
            Type? unityObjectType = patcher?.ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ??
                patcher?.ResolveType("UnityEngine.Object, UnityEngine");
            if (unityObjectType == null)
                return Array.Empty<object>();

            if (includeInactive)
            {
                MethodInfo? includeInactiveFindObjectsOfType = unityObjectType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "FindObjectsOfType" || m.IsGenericMethodDefinition)
                            return false;
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 2 &&
                            parameters[0].ParameterType == typeof(Type) &&
                            parameters[1].ParameterType == typeof(bool);
                    });
                object[] foundWithInactive = ToObjectArray(includeInactiveFindObjectsOfType?.Invoke(null, new object[] { type, true }));
                if (foundWithInactive.Length > 0)
                    return foundWithInactive;

                MethodInfo? findObjectsByType = unityObjectType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        if (m.Name != "FindObjectsByType" || m.IsGenericMethodDefinition)
                            return false;
                        ParameterInfo[] parameters = m.GetParameters();
                        return parameters.Length == 3 &&
                            parameters[0].ParameterType == typeof(Type) &&
                            parameters[1].ParameterType.IsEnum &&
                            parameters[2].ParameterType.IsEnum;
                    });
                if (findObjectsByType != null)
                {
                    ParameterInfo[] parameters = findObjectsByType.GetParameters();
                    object include = Enum.Parse(parameters[1].ParameterType, "Include");
                    object none = Enum.Parse(parameters[2].ParameterType, "None");
                    object[] foundByType = ToObjectArray(findObjectsByType.Invoke(null, new[] { type, include, none }));
                    if (foundByType.Length > 0)
                        return foundByType;
                }

                Type? resourcesType = patcher?.ResolveType("UnityEngine.Resources, UnityEngine.CoreModule") ??
                    patcher?.ResolveType("UnityEngine.Resources, UnityEngine");
                MethodInfo? findObjectsOfTypeAll = resourcesType?.GetMethod("FindObjectsOfTypeAll", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type) }, null);
                object[] allObjects = ToObjectArray(findObjectsOfTypeAll?.Invoke(null, new object[] { type }));
                if (allObjects.Length > 0)
                    return allObjects;
            }

            MethodInfo? findObjectsOfType = unityObjectType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "FindObjectsOfType" || m.IsGenericMethodDefinition)
                        return false;
                    ParameterInfo[] parameters = m.GetParameters();
                    return parameters.Length == 1 && parameters[0].ParameterType == typeof(Type);
                });
            return ToObjectArray(findObjectsOfType?.Invoke(null, new object[] { type }));
        }

        private static object[] ToObjectArray(object? found)
        {
            if (!(found is Array array))
                return Array.Empty<object>();
            return array.Cast<object>().Where(o => o != null).ToArray();
        }

        private object? FindFirstUnityObject(Type type)
        {
            return FindUnityObjects(type).FirstOrDefault();
        }

        private object? FindToolColliderForSmoke(Type dolocApi, Type toolColliderType)
        {
            object? activeCollider = FindFirstUnityObject(toolColliderType);
            if (activeCollider != null)
                return activeCollider;

            object? agent = dolocApi.GetProperty("agent", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            object? toolRenderer = agent == null ? null : ReadMember(agent, "ToolRenderer");
            if (toolRenderer == null)
                return null;

            toolRenderer.GetType().GetMethod("Init", BindingFlags.Public | BindingFlags.Instance)?.Invoke(toolRenderer, null);
            return ReadMember(toolRenderer, "_collider");
        }

        private static object? GenerateItemForSmoke(Type dolocApi, string itemId)
        {
            return GenerateItemForSmoke(dolocApi, itemId, 1);
        }

        private static object? GenerateItemForSmoke(Type dolocApi, string itemId, int count)
        {
            MethodInfo? generateItem = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    ParameterInfo[] p = m.GetParameters();
                    return m.Name == "GenerateItem" &&
                        p.Length >= 1 &&
                        p.Length <= 2 &&
                        p[0].ParameterType == typeof(string) &&
                        (p.Length == 1 || p[1].ParameterType == typeof(int));
                });
            if (generateItem == null)
                return null;
            return generateItem.GetParameters().Length == 1
                ? generateItem.Invoke(null, new object[] { itemId })
                : generateItem.Invoke(null, new object[] { itemId, Math.Max(1, count) });
        }

        private static bool TryChooseToolIdForResource(object resource, out string toolId)
        {
            toolId = string.Empty;
            string kind = GetOneActionResourceKind(resource);
            if (kind.Equals("Tree", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_axe";
                return true;
            }
            if (kind.Equals("Ore", StringComparison.OrdinalIgnoreCase) ||
                kind.Equals("Garbage", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_pickaxe";
                return true;
            }
            if (kind.Equals("Weeds", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_sickle";
                return true;
            }
            return false;
        }

        private static bool TryChooseToolIdsForVegetation(object vegetation, out string expectedToolId, out string expectedToolType, out int expectedMinLevel, out string wrongToolId, out string summary)
        {
            expectedToolId = string.Empty;
            expectedToolType = string.Empty;
            expectedMinLevel = -1;
            wrongToolId = string.Empty;
            summary = string.Empty;

            object? proto = ReadMember(vegetation, "proto");
            if (proto == null)
            {
                summary = "vegetation proto unavailable. target=" + DescribeVegetationForSmoke(vegetation);
                return false;
            }

            if (!TryGetFirstVegetationToolConstraint(proto, out expectedToolType, out expectedMinLevel, out summary))
                return false;

            if (!TryGetToolIdForToolType(expectedToolType, out expectedToolId) ||
                !TryGetWrongToolIdForToolType(expectedToolType, out wrongToolId))
            {
                summary = "No smoke tool mapping for vegetation toolType=" + expectedToolType + ".";
                return false;
            }

            summary = "toolType=" + expectedToolType + ", minLevel=" + expectedMinLevel + ", expectedTool=" + expectedToolId + ", wrongTool=" + wrongToolId;
            return true;
        }

        private static bool TryGetFirstVegetationToolConstraint(object proto, out string toolType, out int minLevel, out string summary)
        {
            toolType = string.Empty;
            minLevel = -1;
            summary = string.Empty;

            object? constraints = ReadMember(proto, "ToolConstraints");
            if (!(constraints is IEnumerable enumerable))
            {
                summary = "ToolConstraints unavailable for vegetation proto " + ReadStringMember(proto, "Id", proto.GetType().Name) + ".";
                return false;
            }

            foreach (object constraint in enumerable)
            {
                toolType = ReadMember(constraint, "ToolType")?.ToString() ?? string.Empty;
                minLevel = ReadIntMember(constraint, "ToolLevel", -1);
                if (!string.IsNullOrWhiteSpace(toolType))
                {
                    summary = "proto=" + ReadStringMember(proto, "Id", proto.GetType().Name) + ", toolType=" + toolType + ", minLevel=" + minLevel;
                    return true;
                }
            }

            summary = "No tool constraint entries for vegetation proto " + ReadStringMember(proto, "Id", proto.GetType().Name) + ".";
            return false;
        }

        private static bool TryGetToolIdForToolType(string toolType, out string toolId)
        {
            toolId = string.Empty;
            if (toolType.Equals("AXE", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_axe";
                return true;
            }
            if (toolType.Equals("PICKAXE", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_pickaxe";
                return true;
            }
            if (toolType.Equals("SICKLE", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_sickle";
                return true;
            }
            return false;
        }

        private static bool TryGetWrongToolIdForToolType(string toolType, out string toolId)
        {
            toolId = string.Empty;
            if (toolType.Equals("AXE", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_pickaxe";
                return true;
            }
            if (toolType.Equals("PICKAXE", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_axe";
                return true;
            }
            if (toolType.Equals("SICKLE", StringComparison.OrdinalIgnoreCase))
            {
                toolId = "old_pickaxe";
                return true;
            }
            return false;
        }

        private static bool TryChooseMismatchedToolIdForResource(object resource, out string toolId, out string expectedToolId)
        {
            toolId = string.Empty;
            expectedToolId = string.Empty;
            string kind = GetOneActionResourceKind(resource);
            if (kind.Equals("Tree", StringComparison.OrdinalIgnoreCase))
            {
                expectedToolId = "old_axe";
                toolId = "old_pickaxe";
                return true;
            }
            if (kind.Equals("Ore", StringComparison.OrdinalIgnoreCase) ||
                kind.Equals("Garbage", StringComparison.OrdinalIgnoreCase))
            {
                expectedToolId = "old_pickaxe";
                toolId = "old_axe";
                return true;
            }
            if (kind.Equals("Weeds", StringComparison.OrdinalIgnoreCase))
            {
                expectedToolId = "old_sickle";
                toolId = "old_pickaxe";
                return true;
            }
            return false;
        }

        private static string GetOneActionResourceKind(object resource)
        {
            Type resourceType = resource.GetType();
            string typeName = resourceType.FullName ?? resourceType.Name;
            string resourceClass = ReadResourceClass(resource);
            if (IsOneActionTreeResource(resourceType))
                return "Tree";
            if (IsTypeOrBase(resourceType, "DolocTown.DungeonResourceOre") || resourceClass.Equals("Ore", StringComparison.OrdinalIgnoreCase))
                return "Ore";
            if (typeName.IndexOf("Garbage", StringComparison.OrdinalIgnoreCase) >= 0 || resourceClass.Equals("Garbage", StringComparison.OrdinalIgnoreCase))
                return "Garbage";
            if (IsTypeOrBase(resourceType, "DolocTown.DungeonResourceWeeds"))
                return "Weeds";
            return string.Empty;
        }

        private static string GetOneActionProtoKind(string resourceClass, string classTypeName)
        {
            if (ClassNameMatches(classTypeName, "DolocTown.DungeonResourceTree") ||
                ClassNameMatches(classTypeName, "DolocTown.DungeonResourceTreeTrunk"))
                return "Tree";
            if (ClassNameMatches(classTypeName, "DolocTown.DungeonResourceOre") ||
                resourceClass.Equals("Ore", StringComparison.OrdinalIgnoreCase))
                return "Ore";
            if (classTypeName.IndexOf("Garbage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                resourceClass.Equals("Garbage", StringComparison.OrdinalIgnoreCase))
                return "Garbage";
            if (ClassNameMatches(classTypeName, "DolocTown.DungeonResourceWeeds") ||
                ClassNameMatches(classTypeName, "DolocTown.DungeonResourceWeedsSmall"))
                return "Weeds";
            return string.Empty;
        }

        private static bool IsOneActionTreeResource(Type type)
        {
            return IsTypeOrBase(type, "DolocTown.DungeonResourceTree") ||
                IsTypeOrBase(type, "DolocTown.DungeonResourceTreeTrunk");
        }

        private static bool ClassNameMatches(string classTypeName, string fullName)
        {
            string simpleName = fullName.Substring(fullName.LastIndexOf('.') + 1);
            return classTypeName.Equals(fullName, StringComparison.Ordinal) ||
                classTypeName.Equals(simpleName, StringComparison.Ordinal) ||
                classTypeName.EndsWith("." + simpleName, StringComparison.Ordinal);
        }

        private static bool IsRemoved(object instance)
        {
            object? value = ReadMember(instance, "IsRemoved");
            return value is bool removed && removed;
        }

        private static bool IsCoalResourceNameForSmoke(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                return false;
            string normalized = resourceName.Trim().ToLowerInvariant();
            return normalized.Equals("coal", StringComparison.Ordinal) ||
                normalized.Equals("coal_ore", StringComparison.Ordinal) ||
                normalized.Contains("coal");
        }

        private static int ReadIntMember(object instance, string name, int fallback)
        {
            object? value = ReadMember(instance, name);
            return value == null ? fallback : Convert.ToInt32(value);
        }

        private static int ReadAnyIntMember(object instance, int fallback, params string[] names)
        {
            object? value = ReadAnyMember(instance, names);
            if (value == null)
                return fallback;

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return fallback;
            }
        }

        private static bool ReadBoolMember(object instance, string name, bool fallback)
        {
            object? value = ReadMember(instance, name);
            return value == null ? fallback : Convert.ToBoolean(value);
        }

        private static bool ReadAnyBoolMember(object instance, bool fallback, params string[] names)
        {
            object? value = ReadAnyMember(instance, names);
            if (value == null)
                return fallback;

            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return fallback;
            }
        }

        private static bool WriteIntMember(object instance, string name, int value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(int))
                {
                    field.SetValue(instance, value);
                    return true;
                }
                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && property.PropertyType == typeof(int))
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
        }

        private static bool WriteBoolMember(object instance, string name, bool value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(bool))
                {
                    field.SetValue(instance, value);
                    return true;
                }
                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
        }

        private static bool WriteStaticBoolMember(Type type, string name, bool value)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                FieldInfo? field = current.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) ??
                    current.GetField("<" + name + ">k__BackingField", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null && field.FieldType == typeof(bool))
                {
                    field.SetValue(null, value);
                    return true;
                }

                PropertyInfo? property = current.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (property != null && property.CanWrite && property.PropertyType == typeof(bool))
                {
                    property.SetValue(null, value);
                    return true;
                }
            }
            return false;
        }

        private static bool WriteObjectMember(object instance, string name, object value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && IsAssignableToMember(field.FieldType, value))
                {
                    field.SetValue(instance, value);
                    return true;
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && IsAssignableToMember(property.PropertyType, value))
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
        }

        private static bool IsAssignableToMember(Type memberType, object value)
        {
            if (value == null)
                return !memberType.IsValueType;
            return memberType.IsInstanceOfType(value) || memberType == value.GetType();
        }

        private static string ReadStringMember(object instance, string name, string fallback)
        {
            object? value = ReadMember(instance, name);
            return value as string ?? fallback;
        }

        private static string ReadAnyStringMember(object instance, string fallback, params string[] names)
        {
            object? value = ReadAnyMember(instance, names);
            return value as string ?? value?.ToString() ?? fallback;
        }

        private static IEnumerable<string> ReadStringValues(object? value)
        {
            if (value == null)
                yield break;
            if (value is string text)
            {
                if (!string.IsNullOrWhiteSpace(text))
                    yield return text;
                yield break;
            }
            if (value is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    string? itemText = item?.ToString();
                    if (!string.IsNullOrWhiteSpace(itemText))
                        yield return itemText!;
                }
                yield break;
            }

            string? fallback = value.ToString();
            if (!string.IsNullOrWhiteSpace(fallback))
                yield return fallback!;
        }

        private static int CountEnumerableForSmoke(object? value)
        {
            if (value == null)
                return -1;
            if (value is string)
                return -1;
            if (value is IEnumerable enumerable)
            {
                int count = 0;
                foreach (object _ in enumerable)
                    count++;
                return count;
            }
            return -1;
        }

        private static string ReadResourceClass(object resource)
        {
            object? proto = ReadMember(resource, "Proto");
            object? resourceClass = proto == null ? null : ReadMember(proto, "ResourceClass");
            return resourceClass?.ToString() ?? string.Empty;
        }

        private static object? ReadStaticMember(Type? type, string name)
        {
            if (type == null)
                return null;
            object? value = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
            if (value != null)
                return value;
            return type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
        }

        private static object? ReadMember(object instance, string name)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                object? value = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance) ??
                    type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);
                if (value != null)
                    return value;
            }
            return null;
        }

        private static bool SetMemberValue(object instance, string name, object value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && (value == null || field.FieldType.IsInstanceOfType(value)))
                {
                    field.SetValue(instance, value);
                    return true;
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && (value == null || property.PropertyType.IsInstanceOfType(value)))
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
        }

        private static object? ReadAnyMember(object instance, params string[] names)
        {
            foreach (string name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                object? value = ReadMember(instance, name);
                if (value != null)
                    return value;
            }

            return null;
        }

        private static bool IsTypeOrBase(Type type, string fullName)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                if (string.Equals(current.FullName, fullName, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static InstantSaveSnapshot CaptureInstantSaveSnapshot(Type dolocApi)
        {
            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? room = ReadStaticMember(dolocApi, "CurrentRoom");
            object? position = ReadStaticMember(dolocApi, "AgentPosition");
            object? timeData = archive == null ? null : ReadMember(archive, "timeData");
            object? dateNow = timeData == null ? null : ReadMember(timeData, "dateNow");
            int archiveIndex = archive == null ? -1 : ReadIntMember(archive, "archiveIndex", -1);

            return new InstantSaveSnapshot
            {
                ArchiveIndex = archiveIndex >= 0 ? archiveIndex : null,
                RoomId = room == null ? "unknown" : FirstNonEmpty(ReadStringMember(room, "RoomId", string.Empty), ReadStringMember(room, "roomId", string.Empty), room.GetType().Name),
                RoomTitle = room == null ? string.Empty : ReadStringMember(room, "Title", string.Empty),
                RoomType = room == null ? "unknown" : (ReadMember(room, "Type")?.ToString() ?? room.GetType().Name),
                X = ReadDoubleMember(position, "x", double.NaN),
                Y = ReadDoubleMember(position, "y", double.NaN),
                Z = ReadDoubleMember(position, "z", double.NaN),
                TimeText = dateNow?.ToString() ?? string.Empty
            };
        }

        private static double ReadDoubleMember(object? instance, string name, double fallback)
        {
            if (instance == null)
                return fallback;
            object? value = ReadMember(instance, name);
            return value == null ? fallback : Convert.ToDouble(value);
        }

        private static string FormatRatio(double value)
        {
            if (double.IsNaN(value) || value < 0)
                return "unknown";
            return value.ToString("0.###");
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return string.Empty;
        }

        private static MethodInfo? FindMethod(Type? type, string name, int parameterCount)
        {
            if (type == null)
                return null;
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (!method.Name.Equals(name, StringComparison.Ordinal) || method.GetParameters().Length != parameterCount)
                    continue;
                return method;
            }
            return null;
        }

        private bool IsSaveLoadedHookReady => saveLoadedPatched || saveLoadedEventSubscribed;

        private bool AllHookTargetsReady => IsSaveLoadedHookReady && loadRequestedPatched && saveSavingPatched && saveSavedPatched && returnHomePatched && workshopReloadPatched && actionSpeedToolEnterPatched && actionSpeedToolExitPatched && actionSpeedInteractEnterPatched && actionSpeedInteractExitPatched && actionSpeedEatEnterPatched && actionSpeedUseItemContinuesPatched && actionSpeedBaseExitPatched && debugConsoleUseToolPatched && debugConsoleUseItemPatched && debugConsoleEnterUiCheckPatched && oneActionToolColliderPatched && fishingReadyEnterPatched && fishingCastEnterPatched && fishingWaitEnterPatched && fishingWaitPlayPatched && fishingMiniGameStartPatched && fishingMiniGameUpdatePatched && fishingMiniGameStopPatched && fishingPullEnterPatched && fishingPullExitPatched && fishRoeTitlePatched && fishRoeDescriptionPatched && fishRoeDetailPatched && animalFullInfoDataPatched && animalViewerShowPatched && animalPanelRefreshViewerPatched && motorKeyUsePatched && motorInteractPatched && motorGetOnPatched && motorGetOffPatched && motorFixedUpdatePrefixPatched && motorFixedUpdatePostfixPatched && motorUnlockPatched && motorSetPositionPatched && motorEnterRoomPatched && equipmentSlotsReloadParamsPatched && (equipmentSlotsAccessoriesInitPatched || equipmentSlotsAccessoriesStartShowPatched) && strongPlantingGunToolPatched && strongPlantingGunUiPlacePatched && strongPlantingGunUiSwapOnePatched;

        private bool TryAutoLoadSaveViaOfficialUi(HarmonyReflectionPatcher patcher, int humanSlot, int gameIndex, out bool waitForOfficialUi)
        {
            waitForOfficialUi = false;
            Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
            Type? homePageUiState = patcher.ResolveType("DolocTown.HomePageUiState, Assembly-CSharp");
            Type? gameDataUiState = patcher.ResolveType("DolocTown.GameDataUiState, Assembly-CSharp");
            if (dolocApi == null || homePageUiState == null || gameDataUiState == null)
                return false;

            if (!IsUiStateActive(dolocApi, homePageUiState))
            {
                double secondsSinceInit = (DateTimeOffset.Now - initializedAt).TotalSeconds;
                if (smokeSettings?.AutoExerciseZoom == true && secondsSinceInit >= Math.Max(30, smokeSettings.AutoLoadDelaySeconds + 25))
                {
                    runtime.RuntimeMonitor.Log("Smoke automation did not observe HomePageUiState before Zoom smoke load; falling back to direct DolocAPI.LoadGame for save slot " + humanSlot + ". context=" + runtime.UI.InputContext + ".");
                    runtime.SetHookStatus("Smoke.AutoLoadSave", "pending", "DolocAPI.LoadGame", "HomePageUiState was not observed for Zoom smoke; using direct save-load fallback for slot " + humanSlot + ".");
                    waitForOfficialUi = false;
                    return false;
                }

                waitForOfficialUi = true;
                if ((DateTimeOffset.Now - lastAutoLoadReadinessLog).TotalSeconds >= 5)
                {
                    lastAutoLoadReadinessLog = DateTimeOffset.Now;
                    runtime.RuntimeMonitor.Log("Smoke automation waiting for HomePageUiState before loading save slot " + humanSlot + ". context=" + runtime.UI.InputContext + ".");
                    runtime.SetHookStatus("Smoke.AutoLoadSave", "pending", "HomePageUiState", "Waiting for official home/save UI to become active. context=" + runtime.UI.InputContext + ".");
                }
                return false;
            }

            MethodInfo? enterUi = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "EnterUI" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            if (enterUi == null)
                return false;

            runtime.RuntimeMonitor.Log($"Smoke automation selecting save slot {humanSlot} through GameDataUiState official path.");
            object? state = enterUi.MakeGenericMethod(gameDataUiState).Invoke(null, null);
            if (state == null)
                throw new InvalidOperationException("GameDataUiState could not be entered.");
            pendingAutoLoadGameDataState = state;
            RecordMoreSavesOfficialSaveUiEvidence(dolocApi, state);

            MethodInfo? select = gameDataUiState.GetMethod("OnDataSlotSelect", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo? confirm = gameDataUiState.GetMethod("OnConfirm", BindingFlags.Instance | BindingFlags.NonPublic);
            if (select == null || confirm == null)
                throw new MissingMethodException("GameDataUiState select/confirm methods were not found.");

            select.Invoke(state, new object[] { gameIndex });
            runtime.NotifyLoadGameRequested(gameIndex);
            confirm.Invoke(state, null);
            autoLoadOfficialPathRequested = true;
            modChangePromptConfirmed = false;
            pendingAutoLoadGameIndex = gameIndex;
            runtime.SetHookStatus("Smoke.AutoLoadSave", "pending", "GameDataUiState.OnConfirm", "Requested official save UI load path for slot " + humanSlot + ".");
            return true;
        }

        private void RecordMoreSavesOfficialSaveUiEvidence(Type dolocApi, object gameDataUiState)
        {
            try
            {
                object? gameManager = ReadStaticMember(dolocApi, "gameManager");
                int archiveFileCount = gameManager == null ? -1 : ReadIntMember(gameManager, "archiveFileCount", -1);
                object? panel = ReadMember(gameDataUiState, "panel");
                int panelSlotCount = panel == null ? -1 : ReadIntMember(panel, "slotCount", -1);
                object? slots = panel == null ? null : ReadMember(panel, "slots");
                int renderedSlots = CountEnumerableForSmoke(slots);
                string status = archiveFileCount >= 12 && (panelSlotCount >= 12 || renderedSlots >= 12)
                    ? "verified"
                    : "pending";
                string summary = "archiveFileCount=" + archiveFileCount + ", panelSlotCount=" + panelSlotCount + ", renderedSlots=" + renderedSlots + ", path=DolocAPI.gameManager.archiveFileCount -> GameDataUiState.Show -> GameDataPanel.Render.";
                runtime.RuntimeMonitor.Log("MoreSaves official save UI evidence " + summary);
                runtime.SetHookStatus("Smoke.MoreSavesOfficialSaveUi", status, "GameDataUiState official save UI path", summary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "MoreSaves official save UI evidence failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.MoreSavesOfficialSaveUi", "failed", "GameDataUiState official save UI path", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void TryConfirmModChangePrompt()
        {
            try
            {
                patcher ??= new HarmonyReflectionPatcher(runtime);
                Type? dolocApi = patcher.ResolveType("DolocAPI, Assembly-CSharp");
                Type? modChangeListUiState = patcher.ResolveType("DolocTown.ModChangeListUiState, Assembly-CSharp");
                if (dolocApi == null || modChangeListUiState == null)
                    return;

                object? state = GetExistingUiState(dolocApi, modChangeListUiState);
                if (state == null)
                    return;

                if (pendingAutoLoadGameIndex.HasValue)
                {
                    RestoreGameDataSelection(dolocApi, pendingAutoLoadGameIndex.Value);
                    WrapModChangeConfirmForSmoke(dolocApi, modChangeListUiState, state, pendingAutoLoadGameIndex.Value);
                }

                MethodInfo? confirm = modChangeListUiState.GetMethod("OnConfirm", BindingFlags.Instance | BindingFlags.NonPublic);
                if (confirm == null)
                    return;

                runtime.RuntimeMonitor.Log("Smoke automation confirming ModChangeListUiState before save load.");
                confirm.Invoke(state, null);
                modChangePromptConfirmed = true;
                runtime.SetHookStatus("Smoke.AutoLoadSave", "pending", "ModChangeListUiState.OnConfirm", "Confirmed official mod-change prompt.");
            }
            catch (Exception ex)
            {
                modChangePromptConfirmed = true;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke mod-change confirmation failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AutoLoadSave", "failed", "ModChangeListUiState.OnConfirm", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private void WrapModChangeConfirmForSmoke(Type dolocApi, Type modChangeListUiState, object modChangeState, int gameIndex)
        {
            FieldInfo? onConfirm = modChangeListUiState.GetField("onConfirm", BindingFlags.Instance | BindingFlags.NonPublic);
            if (!(onConfirm?.GetValue(modChangeState) is Action originalConfirm))
                return;

            onConfirm.SetValue(modChangeState, new Action(() =>
            {
                RestoreGameDataSelection(dolocApi, gameIndex);
                runtime.RuntimeMonitor.Log("Smoke automation restored save slot " + (gameIndex + 1) + " immediately before official load confirm.");
                originalConfirm();
            }));
            runtime.RuntimeMonitor.Log("Smoke automation wrapped ModChangeListUiState confirmation for save slot " + (gameIndex + 1) + ".");
        }

        private void RestoreGameDataSelection(Type dolocApi, int gameIndex)
        {
            Type? gameDataUiState = patcher?.ResolveType("DolocTown.GameDataUiState, Assembly-CSharp");
            if (gameDataUiState == null)
                return;

            object? gameDataState = pendingAutoLoadGameDataState;
            if (gameDataState == null || !gameDataUiState.IsInstanceOfType(gameDataState))
                gameDataState = GetExistingUiState(dolocApi, gameDataUiState);
            if (gameDataState == null)
            {
                runtime.RuntimeMonitor.Log("Smoke automation could not restore GameDataUiState selection; state was not cached.");
                return;
            }

            FieldInfo? currentIndex = gameDataUiState.GetField("currentIndex", BindingFlags.Instance | BindingFlags.NonPublic);
            currentIndex?.SetValue(gameDataState, gameIndex);

            MethodInfo? select = gameDataUiState.GetMethod("OnDataSlotSelect", BindingFlags.Instance | BindingFlags.NonPublic);
            select?.Invoke(gameDataState, new object[] { gameIndex });
            runtime.NotifyLoadGameRequested(gameIndex);
            runtime.RuntimeMonitor.Log("Smoke automation restored GameDataUiState selection to save slot " + (gameIndex + 1) + ".");
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

        private enum SmokeAttemptResult
        {
            Pending,
            Failed,
            Succeeded
        }

        [DataContract]
        private sealed class SmokeSettings
        {
            [DataMember] public bool Enabled { get; set; }
            [DataMember] public int AutoLoadSaveSlot { get; set; }
            [DataMember] public int AutoLoadDelaySeconds { get; set; } = 8;
            [DataMember] public int AutoExitAfterSeconds { get; set; }
            [DataMember] public bool AutoExitAfterSaveLoaded { get; set; }
            [DataMember] public bool AutoSaveAfterLoad { get; set; }
            [DataMember] public int AutoSaveDelaySeconds { get; set; } = 2;
            [DataMember] public bool AutoReloadMods { get; set; }
            [DataMember] public int AutoReloadModsDelaySeconds { get; set; } = 12;
            [DataMember] public bool AutoExerciseExperimentalHooks { get; set; }
            [DataMember] public bool AutoExerciseActionSpeedTool { get; set; }
            [DataMember] public int AutoExerciseActionSpeedToolDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoExerciseActionSpeedConfigApply { get; set; }
            [DataMember] public int AutoExerciseActionSpeedConfigApplyDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoExerciseActionSpeedInteraction { get; set; }
            [DataMember] public int AutoExerciseActionSpeedInteractionDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoExerciseOneActionResourceHit { get; set; }
            [DataMember] public int AutoExerciseOneActionResourceHitDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoExerciseOneActionWrongTool { get; set; }
            [DataMember] public int AutoExerciseOneActionWrongToolDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoExerciseOneActionFuelFeed { get; set; }
            [DataMember] public int AutoExerciseOneActionFuelFeedDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoExerciseOneActionVegetation { get; set; }
            [DataMember] public int AutoExerciseOneActionVegetationDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoExerciseAutoFishingPhase { get; set; }
            [DataMember] public int AutoExerciseAutoFishingPhaseDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoExerciseAutoFishingMiniGameComplete { get; set; }
            [DataMember] public bool AutoExerciseTitleButtonLifecycle { get; set; }
            [DataMember] public bool AutoExerciseInstantSave { get; set; }
            [DataMember] public int AutoExerciseInstantSaveDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoExerciseDebugConsole { get; set; }
            [DataMember] public bool AutoExerciseDebugConsoleMouseGive { get; set; }
            [DataMember] public int AutoExerciseDebugConsoleDelaySeconds { get; set; } = 60;
            [DataMember] public bool AutoExerciseDebugInventory { get; set; }
            [DataMember] public int AutoExerciseDebugInventoryDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoExerciseDebugWeather { get; set; }
            [DataMember] public int AutoExerciseDebugWeatherDelaySeconds { get; set; } = 4;
            [DataMember] public bool AutoExerciseDebugTeleport { get; set; }
            [DataMember] public int AutoExerciseDebugTeleportDelaySeconds { get; set; } = 5;
            [DataMember] public bool AutoExerciseDebugTime { get; set; }
            [DataMember] public int AutoExerciseDebugTimeDelaySeconds { get; set; } = 6;
            [DataMember] public bool AutoExerciseDebugMovement { get; set; }
            [DataMember] public int AutoExerciseDebugMovementDelaySeconds { get; set; } = 7;
            [DataMember] public bool AutoExerciseAdvancedDebug { get; set; }
            [DataMember] public int AutoExerciseAdvancedDebugDelaySeconds { get; set; } = 8;
            [DataMember] public bool AutoExerciseVehicle { get; set; }
            [DataMember] public int AutoExerciseVehicleDelaySeconds { get; set; } = 8;
            [DataMember] public bool AutoExerciseNewContentApis { get; set; }
            [DataMember] public int AutoExerciseNewContentApisDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoExerciseMineContentApis { get; set; }
            [DataMember] public int AutoExerciseMineContentApisDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoExerciseZoom { get; set; }
            [DataMember] public int AutoExerciseZoomDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoExerciseChestLocatorEnhancer { get; set; }
            [DataMember] public int AutoExerciseChestLocatorEnhancerDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoExerciseStrongPlantingGun { get; set; }
            [DataMember] public int AutoExerciseStrongPlantingGunDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoExerciseCustomEntityApis { get; set; }
            [DataMember] public int AutoExerciseCustomEntityApisDelaySeconds { get; set; } = 3;
            [DataMember] public bool AutoFishingExternalHotkeyRequired { get; set; }
            [DataMember] public bool AutoOpenTitleSettingsMenu { get; set; }
            [DataMember] public int AutoOpenTitleSettingsDelaySeconds { get; set; } = 12;
            [DataMember] public bool AutoOpenOfficialModUi { get; set; }
            [DataMember] public int AutoOpenOfficialModUiDelaySeconds { get; set; } = 12;
            [DataMember] public bool AutoOpenAnimalPanel { get; set; }
            [DataMember] public int AutoOpenAnimalPanelDelaySeconds { get; set; } = 2;
        }

        private sealed class InstantSaveSnapshot
        {
            public int? ArchiveIndex { get; set; }
            public string RoomId { get; set; } = "unknown";
            public string RoomTitle { get; set; } = string.Empty;
            public string RoomType { get; set; } = "unknown";
            public double X { get; set; } = double.NaN;
            public double Y { get; set; } = double.NaN;
            public double Z { get; set; } = double.NaN;
            public string TimeText { get; set; } = string.Empty;

            public double DistanceTo(InstantSaveSnapshot other)
            {
                if (other == null || double.IsNaN(X) || double.IsNaN(Y) || double.IsNaN(other.X) || double.IsNaN(other.Y))
                    return double.NaN;
                double dx = X - other.X;
                double dy = Y - other.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            public string ToLogString()
            {
                return "archiveIndex=" + (ArchiveIndex?.ToString() ?? "unknown") +
                    ", roomId=" + RoomId +
                    ", roomTitle=" + RoomTitle +
                    ", roomType=" + RoomType +
                    ", position=" + FormatDouble(X) + "," + FormatDouble(Y) + "," + FormatDouble(Z) +
                    ", time=" + TimeText;
            }

            private static string FormatDouble(double value)
            {
                return double.IsNaN(value) ? "unknown" : value.ToString("0.###");
            }
        }
    }
}
