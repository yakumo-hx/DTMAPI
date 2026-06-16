using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public static class DolocTownHookCallbacks
    {
        private const int HookCallbackShortLogLimit = 3;
        private static readonly TimeSpan HookCallbackSummaryInterval = TimeSpan.FromSeconds(30);
        private static readonly object hookCallbackFailureGate = new object();
        private static readonly Dictionary<string, HookCallbackFailureState> hookCallbackFailures = new Dictionary<string, HookCallbackFailureState>(StringComparer.Ordinal);
        private static DtmApiRuntime? runtime;

        public static DtmApiRuntime? Runtime
        {
            get => runtime;
            set
            {
                runtime = value;
                ResetHookCallbackFailureThrottle();
            }
        }

        public static DolocTownGameBridge? Bridge { get; set; }
        public static bool DebugConsoleModalOpen { get; set; }
        private static bool debugConsoleInputSuppressionLogged;

        public static void LoadGamePrefix(int index)
        {
            SafeCallback("LoadGame.NotifyLoadGameRequested", () => Runtime?.NotifyLoadGameRequested(index));
        }

        public static void AfterLoadArchiveDataPostfix(bool isNewGame)
        {
            SafeCallback("SaveLoaded.NotifyEquipmentSlots", () => Bridge?.ExperimentalApi?.NotifyEquipmentSlotsSaveLoaded(isNewGame));
            SafeCallback("SaveLoaded.NotifyGameBridgeFeatures", () => Bridge?.NotifyGameBridgeFeaturesSaveLoaded(isNewGame));
            SafeCallback("SaveLoaded.NotifyRuntime", () => Runtime?.NotifySaveLoaded(isNewGame));
            SafeCallback("SaveLoaded.MarkSmoke", () => Bridge?.MarkSaveLoadedForSmoke());
        }

        public static void SaveGamePrefix(int index)
        {
            SafeCallback("SaveGame.NotifySaveSaving", () => Runtime?.NotifySaveSaving(index));
        }

        public static void SaveGamePostfix(int index)
        {
            SafeCallback("SaveGame.NotifySaveSaved", () => Runtime?.NotifySaveSaved(index));
            SafeCallback("SaveGame.NotifyEquipmentSlotsSaved", () => Bridge?.ExperimentalApi?.NotifyEquipmentSlotsSaveSaved(index));
            SafeCallback("SaveGame.MarkSmoke", () => Bridge?.MarkSaveSavedForSmoke());
        }

        public static void ReturnHomePostfix()
        {
            SafeCallback("ReturnedToTitle.NotifyEquipmentSlots", () => Bridge?.ExperimentalApi?.NotifyEquipmentSlotsReturnedToTitle());
            SafeCallback("ReturnedToTitle.NotifyGameBridgeFeatures", () => Bridge?.NotifyGameBridgeFeaturesReturnedToTitle());
            SafeCallback("ReturnedToTitle.NotifyRuntime", () => Runtime?.NotifyReturnedToTitle());
        }

        public static void DolocApiSetEnvCameraPostfix()
        {
            SafeCallback("EnvironmentReset.NotifyGameBridgeFeatures", () => Bridge?.NotifyGameBridgeFeaturesEnvironmentReset("DolocAPI.SetEnvCamera"));
        }

        public static void GameDataUiStateShowPostfix(object __instance)
        {
            SafePostfix("SaveSlots.OfficialUi.ShowPaging", () => Bridge?.SaveSlotsService?.ApplyOfficialSavePanelPagingFromUiState(__instance));
        }

        public static void GameDataPanelSelectPrefix(object __instance, int __0)
        {
            SafePostfix("SaveSlots.OfficialUi.SelectPaging", () => Bridge?.SaveSlotsService?.EnsureOfficialSavePanelPageForSelection(__instance, __0));
        }

        public static void DolocGridUiResetLayoutSizePostfix(object __instance, int __0)
        {
            SafePostfix("UI.NativeLayoutDiagnostics.GridResetLayoutSize", () => Bridge?.NativeUiLayoutDiagnosticsService?.RecordGridResetLayoutSize(__instance, __0));
        }

        public static void DolocGridUiSetCapacityPostfix(object __instance, int __0, int __1)
        {
            SafePostfix("UI.NativeLayoutDiagnostics.GridSetCapacity", () => Bridge?.NativeUiLayoutDiagnosticsService?.RecordGridSetCapacity(__instance, __0, __1));
        }

        public static void GridLayoutGroupConstraintCountSetPrefix(object __instance, ref int __0)
        {
            try
            {
                NativeUiLayoutDiagnosticsService? service = Bridge?.NativeUiLayoutDiagnosticsService;
                if (service != null)
                    __0 = service.NormalizeGridLayoutConstraintCount(__instance, __0);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("UI.NativeLayoutDiagnostics.GridLayoutGroupConstraintCountSetPrefix", ex);
            }
        }

        public static void HomePageRenderTextMenuPostfix(object __instance)
        {
            SafePostfix("UI.NativeLayoutDiagnostics.HomePageRenderTextMenu", () => Bridge?.NativeUiLayoutDiagnosticsService?.RecordHomePageRenderTextMenu(__instance));
        }

        public static void HomePageTextMenuResetLayoutSizePostfix(object __instance, int __0)
        {
            SafePostfix("UI.NativeLayoutDiagnostics.HomePageTextMenuResetLayoutSize", () => Bridge?.NativeUiLayoutDiagnosticsService?.RecordHomePageTextMenuResetLayoutSize(__instance, __0));
        }

        public static void HomePageTextMenuResetLayoutSizePrefix(object __instance, ref int __0)
        {
            try
            {
                NativeUiLayoutDiagnosticsService? service = Bridge?.NativeUiLayoutDiagnosticsService;
                if (service != null)
                    __0 = service.NormalizeHomePageTextMenuResetLayoutSize(__instance, __0);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("UI.NativeLayoutDiagnostics.HomePageTextMenuResetLayoutSizePrefix", ex);
            }
        }

        public static void MenuUiSetCapacityPostfix(object __instance, int __0)
        {
            SafePostfix("UI.NativeLayoutDiagnostics.MenuUiSetCapacity", () => Bridge?.NativeUiLayoutDiagnosticsService?.RecordMenuUiSetCapacity(__instance, __0));
        }

        public static void MenuUiResetLayoutSizePostfix(object __instance, int __0)
        {
            SafePostfix("UI.NativeLayoutDiagnostics.MenuUiResetLayoutSize", () => Bridge?.NativeUiLayoutDiagnosticsService?.RecordMenuUiResetLayoutSize(__instance, __0));
        }

        public static void MenuUiResetLayoutSizePrefix(object __instance, ref int __0)
        {
            try
            {
                NativeUiLayoutDiagnosticsService? service = Bridge?.NativeUiLayoutDiagnosticsService;
                if (service != null)
                    __0 = service.NormalizeMenuUiResetLayoutSize(__instance, __0);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("UI.NativeLayoutDiagnostics.MenuUiResetLayoutSizePrefix", ex);
            }
        }

        public static void MainMenuPanelOnStartShowPostfix(object __instance)
        {
            SafePostfix("UI.NativeLayoutDiagnostics.MainMenuPanelOnStartShow", () => Bridge?.NativeUiLayoutDiagnosticsService?.RecordMainMenuPanelOnStartShow(__instance));
        }

        public static void GameDataPanelSetCapacityPostfix(object __instance, int __0)
        {
            SafePostfix("UI.NativeLayoutDiagnostics.GameDataPanelSetCapacity", () => Bridge?.NativeUiLayoutDiagnosticsService?.RecordGameDataPanelSetCapacity(__instance, __0));
        }

        public static void ReloadModsPostfix()
        {
            SafeCallback("Workshop.NotifyModListChanged", () => Runtime?.NotifyWorkshopModListChanged());
        }

        public static bool WwiseInternalPostSoundEventPrefix(string __0, object? __1, object? __2, bool __3, ref bool __result)
        {
            try
            {
                return Bridge?.AudioReplacementService?.HandleNativeSoundEvent(__0, __1, __2, __3, ref __result) ?? true;
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("AudioReplacement.WwiseSoundManager.InternalPostSoundEvent", ex);
                return true;
            }
        }

        public static void ModDataConstructorPostfix(object __instance, object __0)
        {
            SafePostfix("Workshop.LocalUploadPlan.Display", () => Bridge?.TryMarkDtmapiLocalUploadData(__instance, __0));
        }

        public static bool SteamWorkshopUploaderResolveUploadPlanPrefix(object __instance, object __0, object __1)
        {
            try
            {
                return Bridge?.TryResolveDtmapiUploadPlanIfNativeBusy(__instance, __0, __1) ?? true;
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Workshop.LocalUploadPlanBusyFallback.ResolveUploadPlanPrefix", ex);
                return true;
            }
        }

        public static void SteamWorkshopUploaderResolveUploadPlanPostfix(object __instance, object __0, object __1)
        {
            SafePostfix("Workshop.LocalUploadPlanKnownIdFallback.TrackResolveUploadPlan", () => Bridge?.TrackDtmapiUploadPlanFallbackAfterNativeQuery(__instance, __0, __1));
        }

        public static void ItemTitlePostfix(object __instance, ref string __result)
        {
            string original = __result;
            __result = SafeResult("Items.FishRoeTooltip.ItemTitle", original, () => Bridge?.FishRoeTooltipService?.DecorateFishRoeTitle(__instance, original) ?? original);
        }

        public static void ItemDescriptionPostfix(object __instance, ref string __result)
        {
            string original = __result;
            __result = SafeResult("Items.FishRoeTooltip.ItemDescription", original, () => Bridge?.FishRoeTooltipService?.DecorateFishRoeDetail(__instance, original) ?? original);
        }

        public static void ItemDetailInfoPostfix(object __instance, ref string __result)
        {
            string original = __result;
            __result = SafeResult("Items.FishRoeTooltip.ItemDetailInfo", original, () => Bridge?.FishRoeTooltipService?.DecorateFishRoeDetail(__instance, original) ?? original);
        }

        public static void AnimalFullInfoDataCtorPostfix(object __instance, object __0)
        {
            SafePostfix("Animals.ViewerRendering.FullInfoDataCtor", () => Bridge?.AnimalViewerService?.DecorateAnimalFullInfoData(__instance, __0));
        }

        public static void AnimalViewerShowPrefix(object __instance, object __0)
        {
            SafePostfix("Animals.ViewerRendering.ViewerShowPrefix", () => Bridge?.AnimalViewerService?.PrepareAnimalProgressOverlayBeforeShow(__instance, __0));
        }

        public static void AnimalViewerShowPostfix(object __instance, object __0)
        {
            SafePostfix("Animals.ViewerRendering.ViewerShowPostfix", () =>
            {
                Bridge?.AnimalViewerService?.RenderAnimalProgressOverlay(__instance, __0);
                if (Bridge?.AnimalViewerService?.RecordAnimalViewerUiEvidence(__instance, __0) == true)
                    Bridge.MarkAnimalViewerUiEvidenceForSmoke();
            });
        }

        public static void AnimalPanelRefreshViewerPostfix(object __instance, int __0)
        {
            SafePostfix("Animals.ViewerRendering.PanelRefreshViewer", () =>
            {
                if (Bridge?.AnimalViewerService?.RecordAnimalPanelUiEvidence(__instance, __0) == true)
                    Bridge.MarkAnimalViewerUiEvidenceForSmoke();
            });
        }

        public static void ToolColliderHandleToolsPrefix(object __instance, object other)
        {
            SafePostfix("ToolCollider.HandleTools.CaptureOilCoalDrop", () => Bridge?.OilCoalDropService?.CaptureOilCoalDropBeforeToolHit(__instance, other));
        }

        public static void ToolColliderHandleToolsPostfix(object __instance, object other)
        {
            RunToolColliderHandleToolsPostfixRoutes(
                () => Bridge?.ActionCompletionService?.ApplyOneActionToolHit(__instance, other) == true,
                () => Bridge?.OilCoalDropService?.ApplyOilCoalDropAfterToolHit(__instance, other),
                () => Bridge?.OilCoalDropService?.ClearCapturedOilCoalDrop(__instance, other));
        }

        private static void RunToolColliderHandleToolsPostfixRoutes(Func<bool> applyActionCompletion, Action applyOilDrop, Action clearCapturedOilDrop)
        {
            bool oneActionHandled = SafeResult("ToolCollider.HandleTools.ActionCompletion", false, applyActionCompletion);
            if (oneActionHandled)
                SafePostfix("ToolCollider.HandleTools.OilCoalDrop.ClearCaptured", clearCapturedOilDrop);
            else
                SafePostfix("ToolCollider.HandleTools.OilCoalDrop.ApplyAfterHit", applyOilDrop);
        }

        public static void AgentStateToolEnterPostfix(object __instance)
        {
            SafePostfix("AgentStateTool.OnEnter.ApplyActionSpeed", () => Bridge?.ActionSpeedService?.ApplyActionSpeedToolEnter(__instance));
        }

        public static void AgentStateToolExitPostfix()
        {
            SafeCallback("AgentStateTool.OnExit.RestoreActionSpeed", () => Bridge?.ActionSpeedService?.RestoreActionSpeed("AgentStateTool.OnExit"));
        }

        public static void AgentStateInteractEnterPostfix(object __instance)
        {
            SafePostfix("AgentStateInteract.OnEnter.ApplyActionSpeed", () => Bridge?.ActionSpeedService?.ApplyActionSpeedInteractEnter(__instance));
        }

        public static void AgentStateInteractExitPostfix()
        {
            SafeCallback("AgentStateInteract.OnExit.ApplyOneActionEquipmentFill", () => Bridge?.ActionCompletionService?.ApplyOneActionEquipmentFillAfterInteract());
            SafeCallback("AgentStateInteract.OnExit.RestoreActionSpeed", () => Bridge?.ActionSpeedService?.RestoreActionSpeed("AgentStateInteract.OnExit"));
        }

        public static void AgentStateEatEnterPostfix(object __instance)
        {
            SafePostfix("AgentStateEat.OnEnter.ApplyActionSpeed", () => Bridge?.ActionSpeedService?.ApplyActionSpeedEatEnter(__instance));
        }

        public static void AgentControllerStateUseItemContinuesPrefix(ref float __0)
        {
            float original = __0;
            try
            {
                Bridge?.ActionSpeedService?.AdjustActionSpeedUseItemContinuesDelta(ref __0);
            }
            catch (Exception ex)
            {
                __0 = original;
                RecordHookCallbackFailure("AgentControllerState.UseItemContinues.AdjustActionSpeedDelta", ex);
            }
        }

        public static void AgentControllerStateInteractContinuesPrefix(ref float __0)
        {
            float original = __0;
            try
            {
                Bridge?.ActionSpeedService?.AdjustActionSpeedInteractContinuesDelta(ref __0);
            }
            catch (Exception ex)
            {
                __0 = original;
                RecordHookCallbackFailure("AgentControllerState.InteractContinues.AdjustActionSpeedDelta", ex);
            }
        }

        public static void AnimalRendererOnInteractPrefix(object __instance)
        {
            SafePostfix("AnimalRenderer.OnInteract.MarkActionSpeedNativeOwner", () => Bridge?.ActionSpeedService?.MarkNativeAnimalInteract(__instance));
        }

        public static bool AgentControllerStateUseToolPrefix()
        {
            return SafePrefix("AgentControllerState.UseTool.InputIsolation", () => AllowNativeGameplayInput("UseTool"));
        }

        public static bool AgentControllerStateUseItemPrefix()
        {
            return SafePrefix("AgentControllerState.UseItem.InputIsolation", () => AllowNativeGameplayInput("UseItem"));
        }

        public static bool AgentControllerStateEnterUiCheckPrefix(ref bool __result)
        {
            try
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
            catch (Exception ex)
            {
                RecordHookCallbackFailure("AgentControllerState.EnterUICheck.InputIsolation", ex);
                return true;
            }
        }

        public static bool AdvancedCreativeBoolTruePrefix(ref bool __result)
        {
            try
            {
                if (Bridge?.ExperimentalApi?.ShouldBypassCreativeCostHooks() != true)
                    return true;

                __result = true;
                Bridge.ExperimentalApi.RecordCreativeCostBypassObserved("bool-cost-prefix");
                return false;
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Debug.CreativeMode.BoolCostPrefix", ex);
                return true;
            }
        }

        public static bool AdvancedCreativeVoidSkipPrefix()
        {
            return SafePrefix("Debug.CreativeMode.VoidCostPrefix", () =>
            {
                if (Bridge?.ExperimentalApi?.ShouldBypassCreativeCostHooks() != true)
                    return true;

                Bridge.ExperimentalApi.RecordCreativeCostBypassObserved("void-cost-prefix");
                return false;
            });
        }

        public static void AdvancedCreativeRecipeTimePostfix(ref int __result)
        {
            int original = __result;
            try
            {
                if (Bridge?.ExperimentalApi?.ShouldBypassCreativeTimeHooks() != true)
                    return;

                __result = 0;
                Bridge.ExperimentalApi.RecordCreativeNoTimeBypassObserved(original);
            }
            catch (Exception ex)
            {
                __result = original;
                RecordHookCallbackFailure("Debug.CreativeMode.RecipeTimePostfix", ex);
            }
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
            SafeCallback("AgentStateBase.OnExit.RestoreActionSpeed", () => Bridge?.ActionSpeedService?.RestoreActionSpeed("AgentStateBase.OnExit"));
            SafeCallback("AgentStateBase.OnExit.RestoreExperimentalAnimatorSpeeds", () => Bridge?.FishingAutomationService?.RestoreExperimentalAnimatorSpeeds("AgentStateBase.OnExit"));
        }

        public static void FishingReadyEnterPostfix(object __instance)
        {
            SafePostfix("Fishing.Ready.OnEnter.NotifyPhase", () => Bridge?.FishingAutomationService?.NotifyFishingPhase("Ready", __instance));
        }

        public static void FishingReadyPlayPostfix(object __instance)
        {
            SafePostfix("Fishing.Ready.OnPlay.Automation", () => Bridge?.FishingAutomationService?.ApplyFishingReadyAutomation(__instance));
        }

        public static void FishingCastEnterPostfix(object __instance)
        {
            SafePostfix("Fishing.Cast.OnEnter.NotifyPhase", () => Bridge?.FishingAutomationService?.NotifyFishingPhase("Cast", __instance));
        }

        public static void FishingWaitEnterPostfix(object __instance)
        {
            SafePostfix("Fishing.Wait.OnEnter.NotifyPhase", () => Bridge?.FishingAutomationService?.NotifyFishingPhase("Wait", __instance));
            SafePostfix("Fishing.Wait.OnEnter.ApplyAutomation", () => Bridge?.FishingAutomationService?.ApplyFishingWaitAutomation(__instance, "AgentStateFishingWait.OnEnter Postfix"));
        }

        public static void FishingWaitPlayPostfix(object __instance)
        {
            SafePostfix("Fishing.Wait.OnPlay.ApplyAutomation", () => Bridge?.FishingAutomationService?.ApplyFishingWaitAutomation(__instance, "AgentStateFishingWait.OnPlay Postfix"));
        }

        public static void FishingMiniGameStartPostfix(object __instance)
        {
            SafePostfix("Fishing.MiniGame.Start", () => Bridge?.FishingAutomationService?.NotifyFishingMiniGameStart(__instance));
        }

        public static void FishingMiniGameUpdatePrefix(object __instance)
        {
            SafeCallback("Fishing.MiniGame.Update.PrepareInput", () => Bridge?.FishingAutomationService?.PrepareFishingMiniGameAutomationInput(__instance));
        }

        public static void FishingMiniGameUpdatePostfix(object __instance)
        {
            SafePostfix("Fishing.MiniGame.Update", () => Bridge?.FishingAutomationService?.ApplyFishingMiniGameAutomationTick(__instance));
        }

        public static void FishingMiniGameStopPostfix(object __instance)
        {
            SafePostfix("Fishing.MiniGame.Stop", () => Bridge?.FishingAutomationService?.NotifyFishingMiniGameStop(__instance));
        }

        public static bool FishingInputNormalUseToolPrefix(ref bool __result)
        {
            return TryFishingMiniGameInputOverride("NormalUseTool", ref __result);
        }

        public static bool FishingInputNormalUseToolInProgressPrefix(ref bool __result)
        {
            try
            {
                if (Bridge?.FishingAutomationService?.TryOverrideFishingReadyChargeInput("NormalUseToolInProgress", out bool value) == true)
                {
                    __result = value;
                    return false;
                }
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.Ready.InputOverride.NormalUseToolInProgress", ex);
            }

            return TryFishingMiniGameInputOverride("NormalUseToolInProgress", ref __result);
        }

        public static bool FishingInputNormalUseItemPrefix(ref bool __result)
        {
            return TryFishingMiniGameInputOverride("NormalUseItem", ref __result);
        }

        public static bool FishingInputNormalUseItemInProgressPrefix(ref bool __result)
        {
            return TryFishingMiniGameInputOverride("NormalUseItemInProgress", ref __result);
        }

        public static bool FishingInputNormalFishingPrefix(ref bool __result)
        {
            return TryFishingMiniGameInputOverride("NormalFishing", ref __result);
        }

        public static bool FishingInputNormalFishingInProgressPrefix(ref bool __result)
        {
            return TryFishingMiniGameInputOverride("NormalFishingInProgress", ref __result);
        }

        private static bool TryFishingMiniGameInputOverride(string inputName, ref bool result)
        {
            try
            {
                if (Bridge?.FishingAutomationService?.TryOverrideFishingMiniGameInput(inputName, out bool value) == true)
                {
                    result = value;
                    return false;
                }
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.MiniGame.InputOverride." + inputName, ex);
            }
            return true;
        }

        public static void FishRodRendererCastHookPostfix(object __instance)
        {
            try
            {
                Bridge?.FishingAutomationService?.AdjustFishingCastHookPhysics(__instance);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.FishRodRenderer.CastHook.Physics", ex);
            }
        }

        public static void FishingPullEnterPostfix(object __instance)
        {
            SafePostfix("Fishing.Pull.OnEnter.NotifyPhase", () => Bridge?.FishingAutomationService?.NotifyFishingPhase("Pull", __instance));
        }

        public static void FishingPullExitPostfix()
        {
            try
            {
                SafeCallback("AgentStateFishingPull.OnExit.NotifyCooldown", () => Bridge?.FishingAutomationService?.NotifyFishingPhase("Cooldown", null));
            }
            finally
            {
                SafeCallback("AgentStateFishingPull.OnExit.RestoreExperimentalAnimatorSpeeds", () => Bridge?.FishingAutomationService?.RestoreExperimentalAnimatorSpeeds("AgentStateFishingPull.OnExit"));
            }
        }

        public static void FishRodRendererPullPostfix(ref float __result)
        {
            try
            {
                Bridge?.FishingAutomationService?.AdjustFishingPullDurationResult(ref __result, "FishRodRenderer.Pull");
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.FishRodRenderer.Pull.Duration", ex);
            }
        }

        public static void FishRodRendererPullCancelPostfix(ref float __result)
        {
            try
            {
                Bridge?.FishingAutomationService?.AdjustFishingPullDurationResult(ref __result, "FishRodRenderer.PullCancel");
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.FishRodRenderer.PullCancel.Duration", ex);
            }
        }

        public static void EquipmentRendererOnReusePostfix(object __instance)
        {
            SafePostfix("Equipment.Renderer.OnReuse", () => Bridge?.ExperimentalApi?.ResetEquipmentRendererScaleOnReuse(__instance));
        }

        public static void EquipmentBuilderCreateIndicatorPostfix(object __instance)
        {
            SafePostfix("Equipment.Builder.CreateIndicator", () => Bridge?.ExperimentalApi?.ApplyMineBuilderPreviewScale(__instance, "EquipmentBuilder.CreateIndicator"));
        }

        public static void EquipmentBuilderTurnIndicatorPostfix(object __instance)
        {
            SafePostfix("Equipment.Builder.TurnIndicator", () => Bridge?.ExperimentalApi?.ApplyMineBuilderPreviewScale(__instance, "EquipmentBuilder.TurnIndicator"));
        }

        public static void AgentEquipmentReloadParamsPostfix(object __instance)
        {
            SafePostfix("EquipmentSlots.AgentEquipment.ReloadParams", () => Bridge?.ExperimentalApi?.ApplyEquipmentSlotsAfterReloadParams(__instance));
        }

        public static void AccessoriesBarInitPostfix(object __instance)
        {
            SafePostfix("EquipmentSlots.AccessoriesBar.Init", () => Bridge?.ExperimentalApi?.RenderEquipmentSlotsUiForAccessoriesBar(__instance, "AccessoriesBar.__Init"));
        }

        public static bool BodyControllerOnAttackedPrefix(object __instance, float __0, bool __1, object __2, ref bool __result, ref bool __3)
        {
            try
            {
                return Bridge?.ExperimentalApi?.HandleBodyControllerOnAttackedPrefix(__instance, __0, __1, __2, ref __result, ref __3) ?? true;
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("EquipmentSlots.BodyController.OnAttacked", ex);
                return true;
            }
        }

        public static void AccessoriesBarOnStartShowPostfix(object __instance)
        {
            SafePostfix("EquipmentSlots.AccessoriesBar.OnStartShow", () => Bridge?.ExperimentalApi?.RenderEquipmentSlotsUiForAccessoriesBar(__instance, "AccessoriesBar.OnStartShow"));
        }

        public static Array ArchiveDataHandleGetAvailableInventoriesPostfix(object __instance, object __0, object __1, bool __2, Array __result)
        {
            return SafeResult("Inventory.ChestLocatorEnhancer.GetAvailableInventories", __result, () => Bridge?.ChestLocatorEnhancerService?.ExtendAvailableInventoriesForChestLocator(__instance, __0, __1, __2, __result) ?? __result);
        }

        public static void ItemFarmingGunCtorPostfix(object __instance)
        {
            SafePostfix("Farming.StrongPlantingGun.ItemFarmingGunCtor", () => Bridge?.StrongPlantingGunService?.ExpandFarmingGunInventoryIfNeeded(__instance, "ItemFarmingGun ctor"));
        }

        public static bool ItemFarmingGunOnUseAsToolPrefix(object __instance)
        {
            return SafePrefix("Farming.StrongPlantingGun.OnUseAsTool", () => Bridge?.StrongPlantingGunService?.HandleStrongPlantingGunToolUse(__instance) ?? true);
        }

        public static bool FarmingGunUiStateHandlePlaceToOtherSidePrefix(object __instance, int __0)
        {
            return SafePrefix("Farming.StrongPlantingGun.UiPlaceToOtherSide", () => Bridge?.StrongPlantingGunService?.HandleStrongPlantingGunUiPlaceToOtherSide(__instance, __0) ?? true);
        }

        public static bool FarmingGunUiStateHandleSwapOneItemPrefix(object __instance, int __0)
        {
            return SafePrefix("Farming.StrongPlantingGun.UiSwapOneItem", () => Bridge?.StrongPlantingGunService?.HandleStrongPlantingGunUiSwapOneItem(__instance, __0) ?? true);
        }

        private static void SafeCallback(string operation, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                RecordLifecycleCallbackFailure(operation, ex);
            }
        }

        private static void SafePostfix(string operation, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure(operation, ex);
            }
        }

        private static T SafeResult<T>(string operation, T fallback, Func<T> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure(operation, ex);
                return fallback;
            }
        }

        private static bool SafePrefix(string operation, Func<bool> action, bool fallback = true)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure(operation, ex);
                return fallback;
            }
        }

        private static void RecordLifecycleCallbackFailure(string operation, Exception ex)
        {
            try
            {
                Runtime?.Diagnostics.RecordError("DTMAPI.GameBridge.Lifecycle", "Lifecycle callback failed: " + operation + ".", ex.ToString());
                Runtime?.RuntimeMonitor.Log("Lifecycle callback failed operation=" + operation + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Error);
            }
            catch
            {
                // Harmony callbacks must never rethrow diagnostics failures into native gameplay.
            }
        }

        private static void RecordHookCallbackFailure(string operation, Exception ex)
        {
            try
            {
                HookCallbackFailurePublication publication = RecordHookCallbackFailureState(operation, ex);
                if (publication.RecordDiagnosticsError)
                    Runtime?.Diagnostics.RecordError("DTMAPI.GameBridge.HookCallback", "Hook callback failed: " + operation + ".", ex.ToString());

                if (publication.LogMode == HookCallbackFailureLogMode.Full)
                    Runtime?.RuntimeMonitor.Log("Hook callback failed operation=" + operation + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Error);
                else if (publication.LogMode == HookCallbackFailureLogMode.Short)
                    Runtime?.RuntimeMonitor.Log("Repeated hook callback failure operation=" + operation + " count=" + publication.Count + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
                else if (publication.LogMode == HookCallbackFailureLogMode.Summary)
                    Runtime?.RuntimeMonitor.Log("Throttled hook callback failures operation=" + operation + " count=" + publication.Count + " lastError=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            }
            catch
            {
                // Harmony callbacks must never rethrow diagnostics failures into native gameplay.
            }
        }

        private static HookCallbackFailurePublication RecordHookCallbackFailureState(string operation, Exception ex)
        {
            string key = string.IsNullOrWhiteSpace(operation) ? "<unknown>" : operation;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            lock (hookCallbackFailureGate)
            {
                if (!hookCallbackFailures.TryGetValue(key, out HookCallbackFailureState? state))
                {
                    state = new HookCallbackFailureState();
                    hookCallbackFailures[key] = state;
                }

                state.Count++;
                state.LastError = ex.GetType().Name + ": " + ex.Message;
                state.LastSeenAtUtc = now;

                if (state.Count == 1)
                {
                    state.LastPublishedAtUtc = now;
                    return new HookCallbackFailurePublication(true, HookCallbackFailureLogMode.Full, state.Count);
                }

                if (state.Count <= HookCallbackShortLogLimit)
                {
                    state.LastPublishedAtUtc = now;
                    return new HookCallbackFailurePublication(false, HookCallbackFailureLogMode.Short, state.Count);
                }

                if (now - state.LastPublishedAtUtc >= HookCallbackSummaryInterval)
                {
                    state.LastPublishedAtUtc = now;
                    return new HookCallbackFailurePublication(false, HookCallbackFailureLogMode.Summary, state.Count);
                }

                return new HookCallbackFailurePublication(false, HookCallbackFailureLogMode.None, state.Count);
            }
        }

        private static void ResetHookCallbackFailureThrottle()
        {
            lock (hookCallbackFailureGate)
                hookCallbackFailures.Clear();
        }

        private sealed class HookCallbackFailureState
        {
            public int Count { get; set; }
            public string LastError { get; set; } = string.Empty;
            public DateTimeOffset LastSeenAtUtc { get; set; }
            public DateTimeOffset LastPublishedAtUtc { get; set; }
        }

        private readonly struct HookCallbackFailurePublication
        {
            public HookCallbackFailurePublication(bool recordDiagnosticsError, HookCallbackFailureLogMode logMode, int count)
            {
                RecordDiagnosticsError = recordDiagnosticsError;
                LogMode = logMode;
                Count = count;
            }

            public bool RecordDiagnosticsError { get; }

            public HookCallbackFailureLogMode LogMode { get; }

            public int Count { get; }
        }

        private enum HookCallbackFailureLogMode
        {
            None,
            Full,
            Short,
            Summary
        }
    }
}
