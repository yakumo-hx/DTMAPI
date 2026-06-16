using System;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
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
                    var loadGameSignature = new HarmonyTargetSignature(returnType: typeof(bool), parameterTypes: new[] { typeof(int) });
                    loadRequestedPatched =
                        patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "LoadGame", loadGamePrefix, loadGameSignature) ||
                        patcher.TryPatchPrefix("DolocTown.GameData.DataPersistenceManager, Assembly-CSharp", "LoadGame", loadGamePrefix, loadGameSignature);
                    runtime.SetHookStatus("Save.LoadGameRequested", loadRequestedPatched ? "experimental" : "pending", "Harmony Prefix: LoadGame", loadRequestedPatched ? "Patched for save slot/index evidence." : "Waiting for Assembly-CSharp LoadGame target to become patchable.");
                }

                if (!saveSavingPatched)
                {
                    MethodInfo? saveGamePrefix = typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.SaveGamePrefix), BindingFlags.Public | BindingFlags.Static);
                    var saveGameSignature = new HarmonyTargetSignature(returnType: typeof(bool), parameterTypes: new[] { typeof(int) });
                    saveSavingPatched =
                        patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "SaveGame", saveGamePrefix, saveGameSignature) ||
                        patcher.TryPatchPrefix("DolocTown.GameData.DataPersistenceManager, Assembly-CSharp", "SaveGame", saveGamePrefix, saveGameSignature);
                    runtime.SetHookStatus("Save.SaveSaving", saveSavingPatched ? "experimental" : "pending", "Harmony Prefix: SaveGame", saveSavingPatched ? "Patched. Requires save evidence before verified." : "Waiting for Assembly-CSharp SaveGame target to become patchable.");
                }

                if (!saveSavedPatched)
                {
                    MethodInfo? saveGamePostfix = typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.SaveGamePostfix), BindingFlags.Public | BindingFlags.Static);
                    var saveGameSignature = new HarmonyTargetSignature(returnType: typeof(bool), parameterTypes: new[] { typeof(int) });
                    saveSavedPatched =
                        patcher.TryPatchPostfix("DolocAPI, Assembly-CSharp", "SaveGame", saveGamePostfix, saveGameSignature) ||
                        patcher.TryPatchPostfix("DolocTown.GameData.DataPersistenceManager, Assembly-CSharp", "SaveGame", saveGamePostfix, saveGameSignature);
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
    }
}
