using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public static class DolocTownHookCallbacks
    {
        public static DtmApiRuntime? Runtime { get; set; }
        public static DolocTownGameBridge? Bridge { get; set; }

        public static void LoadGamePrefix(int index)
        {
            Runtime?.NotifyLoadGameRequested(index);
        }

        public static void AfterLoadArchiveDataPostfix(bool isNewGame)
        {
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
            Bridge?.MarkSaveSavedForSmoke();
        }

        public static void ReturnHomePostfix()
        {
            Runtime?.NotifyReturnedToTitle();
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

        public static void AnimalViewerShowPostfix(object __instance, object __0)
        {
            if (Bridge?.ExperimentalApi?.RecordAnimalViewerUiEvidence(__instance, __0) == true)
                Bridge.MarkAnimalViewerUiEvidenceForSmoke();
        }

        public static void AnimalPanelRefreshViewerPostfix(object __instance, int __0)
        {
            if (Bridge?.ExperimentalApi?.RecordAnimalPanelUiEvidence(__instance, __0) == true)
                Bridge.MarkAnimalViewerUiEvidenceForSmoke();
        }

        public static void ToolColliderHandleToolsPostfix(object __instance, object other)
        {
            Bridge?.ExperimentalApi?.ApplyOneActionToolHit(__instance, other);
        }

        public static void AgentStateToolEnterPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.ApplyActionSpeedToolEnter(__instance);
        }

        public static void AgentStateToolExitPostfix()
        {
            Bridge?.ExperimentalApi?.RestoreActionSpeed("AgentStateTool.OnExit");
        }

        public static void AgentStateInteractEnterPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.ApplyActionSpeedInteractEnter(__instance);
        }

        public static void AgentStateInteractExitPostfix()
        {
            Bridge?.ExperimentalApi?.ApplyOneActionEquipmentFillAfterInteract();
            Bridge?.ExperimentalApi?.RestoreActionSpeed("AgentStateInteract.OnExit");
        }

        public static void AgentStateEatEnterPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.ApplyActionSpeedEatEnter(__instance);
        }

        public static void AgentControllerStateUseItemContinuesPrefix(ref float __0)
        {
            Bridge?.ExperimentalApi?.AdjustActionSpeedUseItemContinuesDelta(ref __0);
        }

        public static void AgentStateBaseExitPostfix()
        {
            Bridge?.ExperimentalApi?.RestoreActionSpeed("AgentStateBase.OnExit");
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
            Bridge?.ExperimentalApi?.NotifyFishingPhase("MiniGame", __instance);
        }

        public static void FishingMiniGameStopPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.NotifyFishingPhase("MiniGameStop", __instance);
        }

        public static void FishingPullEnterPostfix(object __instance)
        {
            Bridge?.ExperimentalApi?.NotifyFishingPhase("Pull", __instance);
        }

        public static void FishingPullExitPostfix()
        {
            Bridge?.ExperimentalApi?.NotifyFishingPhase("Cooldown", null);
        }
    }
}
