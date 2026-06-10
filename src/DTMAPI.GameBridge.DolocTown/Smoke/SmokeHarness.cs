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
        private void SmokeUpdate()
        {
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
                SmokeAttemptResult zoomResult = TryExerciseZoomForSmoke();
                if (zoomResult == SmokeAttemptResult.Pending)
                    return;

                autoExerciseZoomAttempted = true;
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
                    runtime.SetHookStatus("Smoke.CameraPlayable", "pending", "ICameraViewApi", "Waiting after save load to acquire competing CameraView leases, verify 4x/2x dynamic movement telemetry, verify arbitration fallback, and restore vanilla view.");
                if (smokeSettings.AutoExerciseChestLocatorEnhancer)
                    runtime.SetHookStatus("Smoke.ChestLocatorEnhancer", "pending", "IChestLocatorEnhancerApi + ArchiveDataHandle.GetAvailableInventories", "Waiting after save load to create a transient shared Case in a building room and verify CountItem/CostItem through the native shared-inventory array.");
                if (smokeSettings.AutoExerciseStrongPlantingGun)
                    runtime.SetHookStatus("Smoke.StrongPlantingGun", "pending", "IStrongPlantingGunApi + ItemFarmingGun", "Waiting after save load to generate an official farming gun, expose three slots, place seed/film/fertilizer, and apply them to a temporary plant basin.");
                if (smokeSettings.AutoExerciseCustomEntityApis)
                    runtime.SetHookStatus("Smoke.CustomEntityApis", "pending", "ICustomAnimalApi/ICustomMonsterApi/ICustomAttackApi/ICustomDroneApi", "Waiting after save load to register StableCandidate custom entity registry contracts, verify snapshots/status, confirm duplicate validation, confirm runtime-creation-blocked request results, and clean up the smoke owner.");
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

        private bool TryVerifyDiagnosticsSnapshotForSmoke(string scenario, params string[] expectedFeatureIds)
        {
            try
            {
                string reportPath = runtime.ExportLogs();
                IDtmDiagnosticsSnapshot snapshot = ((IDtmDiagnosticsApi)runtime).GetSnapshot();
                var missingFeatureStatuses = expectedFeatureIds
                    .Where(id => !snapshot.FeatureStatuses.Any(status => status.FeatureId.Equals(id, StringComparison.OrdinalIgnoreCase) && status.Status.Equals("ready", StringComparison.OrdinalIgnoreCase)))
                    .ToArray();
                var missingFeatureHooks = expectedFeatureIds
                    .Where(id => !snapshot.HookStatuses.Any(status => status.HookId.Equals("Feature." + id, StringComparison.OrdinalIgnoreCase) && status.Status.Equals("ready", StringComparison.OrdinalIgnoreCase)))
                    .ToArray();

                if (missingFeatureStatuses.Length > 0)
                    throw new InvalidOperationException("Missing feature statuses: " + string.Join(",", missingFeatureStatuses));
                if (missingFeatureHooks.Length > 0)
                    throw new InvalidOperationException("Missing feature hook statuses: " + string.Join(",", missingFeatureHooks));
                if (string.IsNullOrWhiteSpace(snapshot.LatestLogPath) || !File.Exists(snapshot.LatestLogPath))
                    throw new InvalidOperationException("LatestLogPath is missing or does not exist: " + snapshot.LatestLogPath);
                if (string.IsNullOrWhiteSpace(snapshot.LatestReportPath) || !File.Exists(snapshot.LatestReportPath))
                    throw new InvalidOperationException("LatestReportPath is missing or does not exist: " + snapshot.LatestReportPath);
                if (!snapshot.LatestReportPath.Equals(reportPath, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("LatestReportPath did not match exported report. snapshot=" + snapshot.LatestReportPath + ", exported=" + reportPath);
                if (snapshot.Mods.Count == 0)
                    throw new InvalidOperationException("Diagnostic mod status rows are missing.");
                string[] missingStatusCodeRows = snapshot.Mods
                    .Where(status => string.IsNullOrWhiteSpace(status.StatusCode))
                    .Select(status => status.UniqueID)
                    .ToArray();
                if (missingStatusCodeRows.Length > 0)
                    throw new InvalidOperationException("Diagnostic mod status rows are missing StatusCode: " + string.Join(",", missingStatusCodeRows));

                var missingLoadedModStatuses = snapshot.LoadedMods
                    .Where(loaded => !snapshot.Mods.Any(status =>
                        status.UniqueID.Equals(loaded.UniqueID, StringComparison.OrdinalIgnoreCase) &&
                        status.Loaded &&
                        status.StatusCode.Equals("loaded", StringComparison.OrdinalIgnoreCase)))
                    .Select(loaded => loaded.UniqueID)
                    .ToArray();
                if (missingLoadedModStatuses.Length > 0)
                    throw new InvalidOperationException("Missing loaded mod status rows: " + string.Join(",", missingLoadedModStatuses));
                string modStatusCodes = string.Join(
                    ",",
                    snapshot.Mods
                        .GroupBy(status => status.StatusCode, StringComparer.OrdinalIgnoreCase)
                        .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(group => group.Key + "=" + group.Count())
                        .ToArray());

                string summary =
                    "scenario=" + scenario +
                    ", expectedFeatures=" + string.Join(",", expectedFeatureIds) +
                    ", loadedMods=" + snapshot.LoadedMods.Count +
                    ", mods=" + snapshot.Mods.Count +
                    ", modStatusCodes=" + modStatusCodes +
                    ", errors=" + snapshot.Errors.Count +
                    ", warnings=" + snapshot.Warnings.Count +
                    ", hooks=" + snapshot.HookStatuses.Count +
                    ", features=" + snapshot.FeatureStatuses.Count +
                    ", latestLog=" + snapshot.LatestLogPath +
                    ", latestReport=" + snapshot.LatestReportPath;
                runtime.RuntimeMonitor.Log("Smoke diagnostics snapshot OK " + summary);
                runtime.SetHookStatus("Smoke.DiagnosticsSnapshot", "verified", "IDtmDiagnosticsApi.GetSnapshot", summary);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Smoke diagnostics snapshot exercise failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.DiagnosticsSnapshot", "failed", "IDtmDiagnosticsApi.GetSnapshot", scenario + ": " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
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

                IConfigMenuRuntime? configMenuRuntime = runtime.ConfigMenuRuntime;
                if (configMenuRuntime == null)
                    return false;

                configMenuRuntime.BeginEditing(page.Manifest.UniqueID);
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


        private static (int x, int y) ReadVector2IntForSmoke(object? vector)
        {
            if (vector == null)
                return (0, 0);
            return (ReadIntMember(vector, "x", 0), ReadIntMember(vector, "y", 0));
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

            ActionCompletionService? actionCompletion = ActionCompletionService;
            int before = actionCompletion?.OneActionApplicationCount ?? 0;
            onExit.Invoke(current, null);
            int after = actionCompletion?.OneActionApplicationCount ?? 0;
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

        private bool AllHookTargetsReady => IsSaveLoadedHookReady && loadRequestedPatched && saveSavingPatched && saveSavedPatched && returnHomePatched && (cameraFeature?.CameraViewSetEnvCameraPatched == true) && workshopReloadPatched && actionSpeedToolEnterPatched && actionSpeedToolExitPatched && actionSpeedInteractEnterPatched && actionSpeedInteractExitPatched && actionSpeedEatEnterPatched && actionSpeedUseItemContinuesPatched && actionSpeedBaseExitPatched && debugConsoleUseToolPatched && debugConsoleUseItemPatched && debugConsoleEnterUiCheckPatched && oneActionToolColliderPatched && fishingReadyEnterPatched && fishingCastEnterPatched && fishingWaitEnterPatched && fishingWaitPlayPatched && fishingMiniGameStartPatched && fishingMiniGameUpdatePatched && fishingMiniGameStopPatched && fishingPullEnterPatched && fishingPullExitPatched && fishRoeTitlePatched && fishRoeDescriptionPatched && fishRoeDetailPatched && animalFullInfoDataPatched && animalViewerShowPatched && animalPanelRefreshViewerPatched && motorKeyUsePatched && motorInteractPatched && motorGetOnPatched && motorGetOffPatched && motorFixedUpdatePrefixPatched && motorFixedUpdatePostfixPatched && motorUnlockPatched && motorSetPositionPatched && motorEnterRoomPatched && equipmentSlotsReloadParamsPatched && (equipmentSlotsAccessoriesInitPatched || equipmentSlotsAccessoriesStartShowPatched) && (strongPlantingGunFeature?.HookBridge.ToolPatched == true) && (strongPlantingGunFeature?.HookBridge.UiPlacePatched == true) && (strongPlantingGunFeature?.HookBridge.UiSwapOnePatched == true);

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

    }
}
