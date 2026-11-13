using System;
using System.Linq;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private void InstallHarmonyHooks()
        {
            if (shutdownCleanupRan) return;
            patcher ??= new HarmonyPatcher(runtime);
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

            if (loadRequestedPatched && !loadReturnedPatched)
            {
                MethodInfo? loadGamePostfix = typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.LoadGamePostfix), BindingFlags.Public | BindingFlags.Static);
                var loadGameSignature = new HarmonyTargetSignature(returnType: typeof(bool), parameterTypes: new[] { typeof(int) });
                loadReturnedPatched =
                    patcher.TryPatchPostfix("DolocAPI, Assembly-CSharp", "LoadGame", loadGamePostfix, loadGameSignature) ||
                    patcher.TryPatchPostfix("DolocTown.GameData.DataPersistenceManager, Assembly-CSharp", "LoadGame", loadGamePostfix, loadGameSignature);
                runtime.SetHookStatus("Save.LoadGameReturned", loadReturnedPatched ? "experimental" : "pending", "Harmony Postfix: LoadGame", loadReturnedPatched ? "Patched for save-load request return diagnostics." : "Waiting for Assembly-CSharp LoadGame target to become patchable.");
            }

            if (!nativeGameFramePatched)
            {
                MethodInfo? nativeFramePostfix = typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.NormalGameFramePostfix), BindingFlags.Public | BindingFlags.Static);
                nativeGameFramePatched = patcher.TryPatchPostfix(
                    "DolocTown.NormalGameState, Assembly-CSharp",
                    "OnUpdate",
                    nativeFramePostfix,
                    new HarmonyTargetSignature(returnType: typeof(void), parameterTypes: new[] { typeof(float) }));
                runtime.SetHookStatus("GameLoop.NativeFrameDrain", nativeGameFramePatched ? "experimental" : "pending", "Harmony Postfix: NormalGameState.OnUpdate(float)", nativeGameFramePatched ? "Gameplay frame drain installed; Bootstrap keeps title PlayerLoop and owns shared input/Core/UI dispatch." : "Waiting for NormalGameState.OnUpdate(float) to become patchable.");
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

            if (!returnHomeRequestedPatched)
            {
                returnHomeRequestedPatched = patcher.TryPatchPrefix("DolocAPI, Assembly-CSharp", "ReturnHome", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ReturnHomePrefix), BindingFlags.Public | BindingFlags.Static), 1);
                runtime.SetHookStatus("GameLoop.ReturnHomeRequested", returnHomeRequestedPatched ? "experimental" : "pending", "Harmony Prefix: DolocAPI.ReturnHome", returnHomeRequestedPatched ? "Patched ReturnHome request boundary for title lifecycle handling." : "Waiting for DolocAPI.ReturnHome to become patchable.");
            }

            if (!returnHomePatched)
            {
                returnHomePatched = patcher.TryPatchPostfix("DolocAPI, Assembly-CSharp", "ReturnHome", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ReturnHomePostfix), BindingFlags.Public | BindingFlags.Static), 1);
                runtime.SetHookStatus("GameLoop.ReturnedToTitle", returnHomePatched ? "experimental" : "pending", "Harmony Postfix: DolocAPI.ReturnHome", returnHomePatched ? "Patched ReturnHome; title lifecycle smoke verifies the button remount." : "Waiting for DolocAPI.ReturnHome to become patchable.");
            }

            InstallPlatformContextHooks(patcher);

            if (!workshopReloadPatched)
            {
                workshopReloadPatched = patcher.TryPatchPostfix(
                    "DolocTown.Config.ModManager, Assembly-CSharp",
                    "ReloadMods",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ReloadModsPostfix), BindingFlags.Public | BindingFlags.Static),
                    HarmonyTargetSignature.Exact("DolocTown.Config.ModManager", "System.Void"));
            }
            if (!workshopModUiRegisterPatched)
            {
                workshopModUiRegisterPatched = patcher.TryPatchPrefix(
                    "DolocTown.ModUiState, Assembly-CSharp",
                    "Register",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ModUiStateRegisterPrefix), BindingFlags.Public | BindingFlags.Static),
                    HarmonyTargetSignature.Exact("DolocTown.ModUiState", "System.Void"));
            }
            if (!workshopModUiHidePatched)
            {
                workshopModUiHidePatched = patcher.TryPatchPrefix(
                    "DolocTown.ModUiState, Assembly-CSharp",
                    "Hide",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ModUiStateHidePrefix), BindingFlags.Public | BindingFlags.Static),
                    HarmonyTargetSignature.Exact("DolocTown.ModUiState", "System.Void"));
            }
            if (!workshopModManagerSavePatched)
            {
                workshopModManagerSavePatched = patcher.TryPatchPostfix(
                    "DolocTown.GameData.DataPersistenceManager, Assembly-CSharp",
                    "SaveModManager",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.SaveModManagerPostfix), BindingFlags.Public | BindingFlags.Static),
                    HarmonyTargetSignature.Exact(
                        "DolocTown.GameData.DataPersistenceManager",
                        "System.Boolean",
                        "DolocTown.Config.ModManager"));
            }
            if (!workshopModUiCloseTransactionPatched)
            {
                workshopModUiCloseTransactionPatched = patcher.TryPatchPostfix(
                    "DolocTown.ModUiState, Assembly-CSharp",
                    "<Hide>b__27_1",
                    typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ModUiStateCloseTransactionPostfix), BindingFlags.Public | BindingFlags.Static),
                    HarmonyTargetSignature.Exact("DolocTown.ModUiState", "System.Void"));
            }
            bool workshopCommitReady = IsWorkshopCommitHookReady;
            runtime.SetHookStatus(
                "Workshop.ModUiCommit",
                workshopCommitReady ? "experimental" : "pending",
                "Harmony: ModUiState Register/Hide/close, ModManager.ReloadMods, DataPersistenceManager.SaveModManager",
                workshopCommitReady
                    ? "Opening reload is preview-only; a successful native close save commits one deferred DTMAPI refresh."
                    : "Waiting for the complete official Mod UI source transaction boundary to become patchable.");

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
                    "HarmonyLib.Harmony, 0Harmony"));
            }

        }

        private void ProcessPendingHookInstallRequests()
        {
            hookInstallScheduler.PollRetry();
            if (!hookInstallScheduler.TryConsumePending())
                return;

            InstallHarmonyHooks();
            if (CoreHookTargetsReady)
                hookInstallScheduler.StopRetry();
        }

        public void Shutdown(string reason)
        {
            if (shutdownCleanupRan)
                return;
            shutdownCleanupRan = true;
            hookInstallScheduler.Close();

            int failures = 0;
            void Release(string operation, Action action)
            {
                try { action(); }
                catch (Exception ex)
                {
                    failures++;
                    runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Shutdown step failed: " + operation, ex.ToString());
                }
            }
            Release("audio startup policies", () => { audioStartupPolicies?.Dispose(); audioStartupPolicies = null; });

            Release("save-loaded listener", () => TryUnsubscribeSaveLoadedUnityEvent(reason));

            if (ReferenceEquals(DolocTownHookCallbacks.Bridge, this))
                DolocTownHookCallbacks.Bridge = null;
            if (ReferenceEquals(DolocTownHookCallbacks.Runtime, runtime))
                DolocTownHookCallbacks.Runtime = null;
            HarmonyPatcher.ClearStaticCallbacks();
            runtime.SetHookStatus(
                "GameBridge.ShutdownCleanup",
                failures == 0 ? "verified" : "failed",
                "Unity OnApplicationQuit",
                "Shutdown steps completed; failures=" + failures + ". reason=" + (reason));
        }

        private bool CoreHookTargetsReady =>
            IsSaveLoadedHookReady &&
             loadRequestedPatched &&
             loadReturnedPatched &&
             nativeGameFramePatched &&
             saveSavingPatched &&
             saveSavedPatched &&
             returnHomeRequestedPatched &&
             returnHomePatched &&
             IsWorkshopCommitHookReady &&
             PlatformContextHooksReady;

        private bool IsSaveLoadedHookReady => saveLoadedPatched || saveLoadedEventSubscribed;

        private bool IsWorkshopCommitHookReady =>
            workshopReloadPatched &&
            workshopModUiRegisterPatched &&
            workshopModUiHidePatched &&
            workshopModManagerSavePatched &&
            workshopModUiCloseTransactionPatched;

        private bool AllHookTargetsReady => CoreHookTargetsReady;
    }
}
