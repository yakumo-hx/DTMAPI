using System;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public static class DolocTownHookCallbacks
    {
        public static DtmApiRuntime? Runtime { get; set; }
        public static DolocTownGameBridge? Bridge { get; set; }
        public static bool DebugConsoleModalOpen { get; set; }
        private static bool debugConsoleInputSuppressionLogged;

        public static void LoadGamePrefix(int index)
        {
            Runtime?.NotifyLoadGameRequested(index);
        }

        public static void AfterLoadArchiveDataPostfix(bool isNewGame)
        {
            Bridge?.CleanupSecondMotorForLifecycleBoundary("SaveLoaded");
            Bridge?.ExperimentalApi?.NotifyEquipmentSlotsSaveLoaded(isNewGame);
            Bridge?.NotifyGameBridgeFeaturesSaveLoaded(isNewGame);
            Runtime?.NotifySaveLoaded(isNewGame);
            Bridge?.MarkSaveLoadedForSmoke();
        }

        public static void SaveGamePrefix(int index)
        {
            Runtime?.NotifySaveSaving(index);
        }

        public static void SaveGamePostfix(int index)
        {
            Runtime?.NotifySaveSaved(index);
            Bridge?.ExperimentalApi?.NotifyEquipmentSlotsSaveSaved(index);
            Bridge?.MarkSaveSavedForSmoke();
        }

        public static void ReturnHomePostfix()
        {
            Bridge?.CleanupSecondMotorForLifecycleBoundary("ReturnedToTitle");
            Bridge?.ExperimentalApi?.NotifyEquipmentSlotsReturnedToTitle();
            Bridge?.NotifyGameBridgeFeaturesReturnedToTitle();
            Runtime?.NotifyReturnedToTitle();
        }

        public static void DolocApiSetEnvCameraPostfix()
        {
            Bridge?.NotifyGameBridgeFeaturesEnvironmentReset("DolocAPI.SetEnvCamera");
        }

        public static void ReloadModsPostfix()
        {
            Runtime?.NotifyWorkshopModListChanged();
        }

        public static void ItemTitlePostfix(object __instance, ref string __result)
        {
            if (Bridge?.ExperimentalApi != null)
                __result = Bridge.ExperimentalApi.DecorateFishRoeTitle(__instance, __result);
        }

        public static void ItemDescriptionPostfix(object __instance, ref string __result)
        {
            if (Bridge?.ExperimentalApi != null)
                __result = Bridge.ExperimentalApi.DecorateFishRoeDetail(__instance, __result);
        }

        public static void ItemDetailInfoPostfix(object __instance, ref string __result)
        {
            if (Bridge?.ExperimentalApi != null)
                __result = Bridge.ExperimentalApi.DecorateFishRoeDetail(__instance, __result);
        }

        public static void AnimalFullInfoDataCtorPostfix(object __instance, object __0)
        {
            Bridge?.ExperimentalApi?.DecorateAnimalFullInfoData(__instance, __0);
        }

        public static void AnimalViewerShowPrefix(object __instance, object __0)
        {
            Bridge?.ExperimentalApi?.PrepareAnimalProgressOverlayBeforeShow(__instance, __0);
        }

        public static void AnimalViewerShowPostfix(object __instance, object __0)
        {
            Bridge?.ExperimentalApi?.RenderAnimalProgressOverlay(__instance, __0);
            if (Bridge?.ExperimentalApi?.RecordAnimalViewerUiEvidence(__instance, __0) == true)
                Bridge.MarkAnimalViewerUiEvidenceForSmoke();
        }

        public static void AnimalPanelRefreshViewerPostfix(object __instance, int __0)
        {
            if (Bridge?.ExperimentalApi?.RecordAnimalPanelUiEvidence(__instance, __0) == true)
                Bridge.MarkAnimalViewerUiEvidenceForSmoke();
        }

        public static void ToolColliderHandleToolsPrefix(object __instance, object other)
        {
            Bridge?.ExperimentalApi?.CaptureOilCoalDropBeforeToolHit(__instance, other);
        }

        public static void ToolColliderHandleToolsPostfix(object __instance, object other)
        {
            bool oneActionHandled = Bridge?.ExperimentalApi?.ApplyOneActionToolHit(__instance, other) == true;
            if (!oneActionHandled)
                Bridge?.ExperimentalApi?.ApplyOilCoalDropAfterToolHit(__instance, other);
            else
                Bridge?.ExperimentalApi?.ClearCapturedOilCoalDrop(__instance, other);
        }

        public static void AgentStateToolEnterPostfix(object __instance)
        {
            Bridge?.ActionSpeedService?.ApplyActionSpeedToolEnter(__instance);
        }

        public static void AgentStateToolExitPostfix()
        {
            Bridge?.ActionSpeedService?.RestoreActionSpeed("AgentStateTool.OnExit");
        }

        public static void AgentStateInteractEnterPostfix(object __instance)
        {
            Bridge?.ActionSpeedService?.ApplyActionSpeedInteractEnter(__instance);
        }

        public static void AgentStateInteractExitPostfix()
        {
            Bridge?.ExperimentalApi?.ApplyOneActionEquipmentFillAfterInteract();
            Bridge?.ActionSpeedService?.RestoreActionSpeed("AgentStateInteract.OnExit");
        }

        public static void AgentStateEatEnterPostfix(object __instance)
        {
            Bridge?.ActionSpeedService?.ApplyActionSpeedEatEnter(__instance);
        }

        public static void AgentControllerStateUseItemContinuesPrefix(ref float __0)
        {
            Bridge?.ActionSpeedService?.AdjustActionSpeedUseItemContinuesDelta(ref __0);
        }

        public static bool AgentControllerStateUseToolPrefix()
        {
            return AllowNativeGameplayInput("UseTool");
        }

        public static bool AgentControllerStateUseItemPrefix()
        {
            return AllowNativeGameplayInput("UseItem");
        }

        public static bool AgentControllerStateEnterUiCheckPrefix(ref bool __result)
        {
            if (!DebugConsoleModalOpen)
                return true;

            __result = true;
            if (!debugConsoleInputSuppressionLogged)
            {
                debugConsoleInputSuppressionLogged = true;
                Runtime?.RuntimeMonitor.Log("Debug console native input isolation active: AgentControllerState.EnterUICheck suppressed while DTMAPI console is open.");
                Runtime?.SetHookStatus("UI.DebugConsoleInputIsolation", "verified", "Harmony Prefix: AgentControllerState.EnterUICheck/UseTool/UseItem", "Native backpack/menu/tool/item input is swallowed while the DTMAPI Y console is open.");
            }
            return false;
        }

        public static bool AdvancedCreativeBoolTruePrefix(ref bool __result)
        {
            if (Bridge?.ExperimentalApi?.ShouldBypassCreativeCostHooks() != true)
                return true;

            __result = true;
            Bridge.ExperimentalApi.RecordCreativeCostBypassObserved("bool-cost-prefix");
            return false;
        }

        public static bool AdvancedCreativeVoidSkipPrefix()
        {
            if (Bridge?.ExperimentalApi?.ShouldBypassCreativeCostHooks() != true)
                return true;

            Bridge.ExperimentalApi.RecordCreativeCostBypassObserved("void-cost-prefix");
            return false;
        }

        public static void AdvancedCreativeRecipeTimePostfix(ref int __result)
        {
            if (Bridge?.ExperimentalApi?.ShouldBypassCreativeTimeHooks() != true)
                return;

            int original = __result;
            __result = 0;
            Bridge.ExperimentalApi.RecordCreativeNoTimeBypassObserved(original);
        }

        private static bool AllowNativeGameplayInput(string source)
        {
            if (!DebugConsoleModalOpen)
                return true;

            if (!debugConsoleInputSuppressionLogged)
            {
                debugConsoleInputSuppressionLogged = true;
                Runtime?.RuntimeMonitor.Log("Debug console native input isolation active: AgentControllerState." + source + " suppressed while DTMAPI console is open.");
                Runtime?.SetHookStatus("UI.DebugConsoleInputIsolation", "verified", "Harmony Prefix: AgentControllerState.EnterUICheck/UseTool/UseItem", "Native backpack/menu/tool/item input is swallowed while the DTMAPI Y console is open.");
            }
            return false;
        }

        public static void AgentStateBaseExitPostfix()
        {
            Bridge?.ActionSpeedService?.RestoreActionSpeed("AgentStateBase.OnExit");
        }

        public static void FishingReadyEnterPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.NotifyFishingPhase("Ready", __instance);
        }

        public static void FishingCastEnterPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.NotifyFishingPhase("Cast", __instance);
        }

        public static void FishingWaitEnterPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.NotifyFishingPhase("Wait", __instance);
        }

        public static void FishingWaitPlayPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.ApplyFishingWaitAutomation(__instance);
        }

        public static void FishingMiniGameStartPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.NotifyFishingMiniGameStart(__instance);
        }

        public static void FishingMiniGameUpdatePostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.ApplyFishingMiniGameAutomationTick(__instance);
        }

        public static void FishingMiniGameStopPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.NotifyFishingMiniGameStop(__instance);
        }

        public static void FishingPullEnterPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.NotifyFishingPhase("Pull", __instance);
        }

        public static void FishingPullExitPostfix()
        {
            Bridge?.ExperimentalApi?.NotifyFishingPhase("Cooldown", null);
        }

        public static bool ItemMotorKeyOnUsePrefix(object __instance)
        {
            return Bridge?.ExperimentalApi?.HandleMotorKeyUse(__instance) ?? true;
        }

        public static bool MotorInteractableOnInteractPrefix(object __instance)
        {
            return Bridge?.ExperimentalApi?.HandleMotorInteract(__instance) ?? true;
        }

        public static void AgentControllerStateGetOnMotorPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.NotifyMotorGetOn(__instance);
        }

        public static void AgentControllerStateGetOffMotorPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.NotifyMotorGetOff(__instance);
        }

        public static void MotorControllerOnFixedUpdatePrefix(object __instance)
        {
            Bridge?.ExperimentalApi?.ApplySecondMotorTuningForFixedUpdate(__instance);
        }

        public static void MotorControllerOnFixedUpdatePostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.RestoreSecondMotorTuningAfterFixedUpdate(__instance);
        }

        public static void UnlockMotorPostfix()
        {
            Bridge?.ExperimentalApi?.NotifyOriginalMotorUnlocked();
        }

        public static void SetMotorPositionPostfix(object __0, object __1)
        {
            Bridge?.ExperimentalApi?.NotifyOriginalMotorPositionChanged(__0, __1);
        }

        public static void DolocApiEnterRoomPostfix(object __0, object __1, bool __result)
        {
            Bridge?.ExperimentalApi?.NotifyEnterRoomForActiveSecondMotor(__0, __1, __result);
        }

        public static void EquipmentRendererOnReusePostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.ResetEquipmentRendererScaleOnReuse(__instance);
        }

        public static void EquipmentBuilderCreateIndicatorPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.ApplyMineBuilderPreviewScale(__instance, "EquipmentBuilder.CreateIndicator");
        }

        public static void EquipmentBuilderTurnIndicatorPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.ApplyMineBuilderPreviewScale(__instance, "EquipmentBuilder.TurnIndicator");
        }

        public static void AgentEquipmentReloadParamsPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.ApplyEquipmentSlotsAfterReloadParams(__instance);
        }

        public static void AccessoriesBarInitPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.RenderEquipmentSlotsUiForAccessoriesBar(__instance, "AccessoriesBar.__Init");
        }

        public static void AccessoriesBarOnStartShowPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.RenderEquipmentSlotsUiForAccessoriesBar(__instance, "AccessoriesBar.OnStartShow");
        }

        public static Array ArchiveDataHandleGetAvailableInventoriesPostfix(object __instance, object __0, object __1, bool __2, Array __result)
        {
            return Bridge?.ExperimentalApi?.ExtendAvailableInventoriesForChestLocator(__instance, __0, __1, __2, __result) ?? __result;
        }

        public static void ItemFarmingGunCtorPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.ExpandFarmingGunInventoryIfNeeded(__instance, "ItemFarmingGun ctor");
        }

        public static bool ItemFarmingGunOnUseAsToolPrefix(object __instance)
        {
            return Bridge?.ExperimentalApi?.HandleStrongPlantingGunToolUse(__instance) ?? true;
        }

        public static bool FarmingGunUiStateHandlePlaceToOtherSidePrefix(object __instance, int __0)
        {
            return Bridge?.ExperimentalApi?.HandleStrongPlantingGunUiPlaceToOtherSide(__instance, __0) ?? true;
        }

        public static bool FarmingGunUiStateHandleSwapOneItemPrefix(object __instance, int __0)
        {
            return Bridge?.ExperimentalApi?.HandleStrongPlantingGunUiSwapOneItem(__instance, __0) ?? true;
        }
    }
}
