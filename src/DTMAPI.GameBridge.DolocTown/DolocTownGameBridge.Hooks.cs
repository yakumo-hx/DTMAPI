using System;
using System.Linq;
using System.Reflection;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private void InstallHarmonyHooks()
        {
            lock (hookGate)
            {
                if (shutdownCleanupRan) return;
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
                    runtime.SetHookStatus("GameLoop.ReturnHomeRequested", returnHomeRequestedPatched ? "experimental" : "pending", "Harmony Prefix: DolocAPI.ReturnHome", returnHomeRequestedPatched ? "Patched ReturnHome request boundary for title-return object graph diagnostics." : "Waiting for DolocAPI.ReturnHome to become patchable.");
                }

                if (!returnHomePatched)
                {
                    returnHomePatched = patcher.TryPatchPostfix("DolocAPI, Assembly-CSharp", "ReturnHome", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ReturnHomePostfix), BindingFlags.Public | BindingFlags.Static), 1);
                    runtime.SetHookStatus("GameLoop.ReturnedToTitle", returnHomePatched ? "experimental" : "pending", "Harmony Postfix: DolocAPI.ReturnHome", returnHomePatched ? "Patched ReturnHome; title lifecycle smoke verifies the button remount." : "Waiting for DolocAPI.ReturnHome to become patchable.");
                }

                InstallPlatformContextHooks(patcher);
                InstallGameBridgeFeatureHooks(patcher);

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

                if (HasGameBridgeDemand(GameBridgeDemandRoutes.WorkshopAuthoring) && !workshopLocalUploadDisplayPatched)
                {
                    workshopLocalUploadDisplayPatched = patcher.TryPatchConstructorPostfix("DolocTown.UI.ModData, Assembly-CSharp", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.ModDataConstructorPostfix), BindingFlags.Public | BindingFlags.Static), 2);
                    runtime.SetHookStatus("Workshop.LocalUploadPlan", workshopLocalUploadDisplayPatched ? "experimental" : "pending", "Harmony Postfix: ModData..ctor", workshopLocalUploadDisplayPatched ? "Display-only patch keeps DTMAPI-generated local packages on Update when workshop.json is present; native Steam ResolveLocalModUploadPlan still owns upload execution." : "Waiting for Assembly-CSharp/ModData to become patchable.");
                }

                if (HasGameBridgeDemand(GameBridgeDemandRoutes.WorkshopAuthoring) && !workshopUploadPlanBusyFallbackPatched)
                {
                    workshopUploadPlanBusyFallbackPatched = patcher.TryPatchPrefix("DolocTown.Config.SteamWorkshopUploader, Assembly-CSharp", "ResolveUploadPlan", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.SteamWorkshopUploaderResolveUploadPlanPrefix), BindingFlags.Public | BindingFlags.Static), 2);
                    runtime.SetHookStatus("Workshop.LocalUploadPlanBusyFallback", workshopUploadPlanBusyFallbackPatched ? "experimental" : "pending", "Harmony Prefix: SteamWorkshopUploader.ResolveUploadPlan", workshopUploadPlanBusyFallbackPatched ? "Prevents DTMAPI-generated local package upload-plan resolve requests from stalling ModManager when the native uploader is already busy; upload execution remains native-owned." : "Waiting for Assembly-CSharp/SteamWorkshopUploader.ResolveUploadPlan to become patchable.");
                }

                if (HasGameBridgeDemand(GameBridgeDemandRoutes.WorkshopAuthoring) && !workshopUploadPlanKnownIdFallbackPatched)
                {
                    workshopUploadPlanKnownIdFallbackPatched = patcher.TryPatchPostfix("DolocTown.Config.SteamWorkshopUploader, Assembly-CSharp", "ResolveUploadPlan", typeof(DolocTownHookCallbacks).GetMethod(nameof(DolocTownHookCallbacks.SteamWorkshopUploaderResolveUploadPlanPostfix), BindingFlags.Public | BindingFlags.Static), 2);
                    runtime.SetHookStatus("Workshop.LocalUploadPlanKnownIdFallback", workshopUploadPlanKnownIdFallbackPatched ? "experimental" : "pending", "Harmony Postfix + Update watchdog: SteamWorkshopUploader.ResolveUploadPlan", workshopUploadPlanKnownIdFallbackPatched ? "Allows native Steam details resolution first, then releases DTMAPI-generated local package upload-plan requests with the known workshop.json id only if the same callback remains unresolved after a short delay." : "Waiting for Assembly-CSharp/SteamWorkshopUploader.ResolveUploadPlan to become patchable.");
                }

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

                if (AllHookTargetsReady)
                {
                    if (!runtime.RefactorOptions.HookReadinessLayers)
                        StopHookRetrySources("LegacyAllHookTargetsReady");
                }
            }
        }

        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (shutdownCleanupRan) return;
            string? assemblyName = args.LoadedAssembly.GetName().Name;
            if (assemblyName == "Assembly-CSharp")
            {
                uiContextDolocApiType = null;
                uiContextDolocApiResolutionAttempted = false;
                uiContextIsNormalStateGetter = null;
                uiContextUserInputGetter = null;
                uiContextCurrentStateGetter = null;
                uiContextUserInputType = null;
                uiContextBlockingStateTypes = Array.Empty<Type>();
            }
            if (assemblyName == "Assembly-CSharp" || assemblyName == "0Harmony")
                RequestHookInstall("AssemblyLoad:" + assemblyName);
        }

        private void RequestHookInstall(string reason)
        {
            if (shutdownCleanupRan) return;
            if ((reason ?? string.Empty).StartsWith("DemandActivated", StringComparison.OrdinalIgnoreCase))
                EnsureHookRetrySources(reason ?? string.Empty);

            // The option controls immediate main-thread requests, not retry lifetime.
            // AssemblyLoad can arrive on a worker and must not install native hooks there.
            if (!runtime.RefactorOptions.HookInstallScheduler && runtime.IsRuntimeThread)
            {
                InstallHarmonyHooks();
                PublishHookReadinessStatuses("LegacyDirectInstall:" + (reason ?? string.Empty));
                StopHookRetrySources("LegacyDirectInstallComplete");
                return;
            }

            hookInstallScheduler.Request(
                reason ?? string.Empty,
                runtime.CurrentRuntimePhase,
                System.Threading.Thread.CurrentThread.ManagedThreadId,
                runtime.RuntimeThreadId);
            PublishHookSchedulerStatus("Request:" + (reason ?? string.Empty));
        }

        private void EnsureHookRetrySources(string reason)
        {
            if (shutdownCleanupRan)
                return;
            if (!assemblyLoadSubscribed)
            {
                AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
                assemblyLoadSubscribed = true;
            }
            hookInstallScheduler.StartRetry();
        }

        private bool ProcessPendingHookInstallRequests(string reason)
        {
            if (shutdownCleanupRan || !runtime.IsRuntimeThread)
                return false;
            hookInstallScheduler.PollRetry(runtime.CurrentRuntimePhase, runtime.RuntimeThreadId);
            if (!hookInstallScheduler.HasPending)
            {
                hookSchedulerIdleFrameFastPathCount++;
                return false;
            }

            if (!hookInstallScheduler.TryConsumePending(out HookInstallRequest[] requests))
                return false;

            hookInstallProcessedBatchCount++;
            InstallHarmonyHooks();
            PublishHookSchedulerStatus("Process:" + (reason ?? string.Empty) + " requests=" + requests.Length);
            PublishHookReadinessStatuses(reason ?? string.Empty);
            if (CoreHookTargetsReady)
                StopHookRetrySources("CoreHookReadinessReady");
            return true;
        }

        private void StopHookRetrySources(string reason, bool force = false)
        {
            if (!force && (!CoreHookTargetsReady || HasUnreadyDemandedHookRoutes()))
                return;
            hookInstallScheduler.StopRetry();
            if (assemblyLoadSubscribed)
            {
                AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
                assemblyLoadSubscribed = false;
            }

            PublishHookSchedulerStatus(reason ?? string.Empty);
        }

        private bool HasUnreadyDemandedHookRoutes()
        {
            GetDemandedHookRouteCounts(out int ready, out int total);
            return ready < total;
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
            Release("QA participant", () => CloseQaHostParticipant("shutdown:" + (reason ?? string.Empty)));
            Release("audio startup policies", () => { audioStartupPolicies?.Dispose(); audioStartupPolicies = null; });

            lock (hookGate)
            {
                Release("hook retry sources", () => StopHookRetrySources("Shutdown:" + (reason ?? string.Empty), force: true));
                Release("save-loaded listener", () => TryUnsubscribeSaveLoadedUnityEvent(reason ?? string.Empty));
            }

            // Classify the route only after removable UnityEvent listeners have gone.
            // Harmony patches have no proven independent rollback and therefore remain
            // authoritative ProcessPinnedDormant physical roots at shutdown.
            Release("demand routing", () => ReleaseGameBridgeDemandRouting("shutdown:" + (reason ?? string.Empty)));

            if (ReferenceEquals(DolocTownHookCallbacks.Bridge, this))
                DolocTownHookCallbacks.Bridge = null;
            if (ReferenceEquals(DolocTownHookCallbacks.Runtime, runtime))
                DolocTownHookCallbacks.Runtime = null;
            HarmonyReflectionPatcher.ClearStaticCallbacks();
            runtime.SetHookStatus(
                "GameBridge.ShutdownCleanup",
                failures == 0 ? "verified" : "failed",
                "Unity OnApplicationQuit",
                "Shutdown steps completed; failures=" + failures + ". QA scenario cleanup is owned by the optional participant. reason=" + (reason ?? string.Empty));
        }

        private void PublishHookSchedulerStatus(string operation)
        {
            HookInstallSchedulerSnapshot snapshot = BuildHookInstallSchedulerSnapshot();
            runtime.Diagnostics.SetFeatureStatus(
                "Refactor.HookInstallScheduler",
                runtime.RefactorOptions.HookInstallScheduler ? "scheduled-main-thread" : "disabled",
                operation ?? string.Empty,
                success: true,
                failureCount: 0,
                lastError: string.Empty,
                details: runtime.RefactorOptions.HookInstallScheduler ? snapshot.FormatSummary() : "Hook install scheduler disabled; legacy direct install path is active.");
            runtime.SetHookStatus(
                "Refactor.HookInstallScheduler",
                runtime.RefactorOptions.HookInstallScheduler ? "observed" : "disabled",
                "DTMAPI.GameBridge.DolocTown HookInstallScheduler",
                runtime.RefactorOptions.HookInstallScheduler ? snapshot.FormatSummary() : "Hook install scheduler disabled; main-thread direct install and frame retries are active. " + snapshot.FormatSummary());
            runtime.SetHookStatus(
                "Refactor.OffThreadHookRequests",
                snapshot.OffThreadRequests == 0 ? "ok" : "observed",
                "DTMAPI.GameBridge.DolocTown HookInstallScheduler",
                snapshot.FormatOffThreadSummary());
            runtime.SetHookStatus(
                "Refactor.AssemblyLoadSubscription",
                assemblyLoadSubscribed ? "active" : "released",
                "AppDomain.AssemblyLoad",
                snapshot.AssemblySummary);
            runtime.SetHookStatus(
                "Refactor.RetryTimerAlive",
                "released",
                "GameBridge frame retry (no background timer)",
                snapshot.RetrySummary);
        }

        private void PublishHookReadinessStatuses(string operation)
        {
            if (!runtime.RefactorOptions.HookReadinessLayers)
                return;

            hookReadinessPublishCount++;
            HookInstallSchedulerSnapshot snapshot = BuildHookInstallSchedulerSnapshot();
            runtime.Diagnostics.SetFeatureStatus(
                "Refactor.HookReadinessLayers",
                snapshot.CoreReady ? "core-ready" : "core-pending",
                operation ?? string.Empty,
                success: snapshot.CoreReady,
                failureCount: snapshot.CoreReady ? 0 : 1,
                lastError: snapshot.CoreReady ? string.Empty : "Core hook targets are not all ready.",
                details: "core={" + snapshot.CoreSummary + "}; feature={" + snapshot.FeatureSummary + "}; smokeDiagnostics={" + snapshot.SmokeDiagnosticsSummary + "}; legacyAllReady=" + (snapshot.LegacyAllReady ? "true" : "false"));
            runtime.SetHookStatus(
                "Refactor.CoreHookReadiness",
                snapshot.CoreReady ? "ready" : "pending",
                "DTMAPI.GameBridge.DolocTown layered hook readiness",
                snapshot.CoreSummary);
            runtime.SetHookStatus(
                "Refactor.FeatureHookReadiness",
                FeatureHookTargetsReady ? "ready" : "partial",
                "DTMAPI.GameBridge.DolocTown legacy 25/34 physical hook subset",
                snapshot.FeatureSummary);
            GetCatalogPhysicalHookRouteCounts(out int catalogPhysicalReady, out int catalogPhysicalTotal);
            runtime.SetHookStatus(
                "Refactor.CatalogPhysicalHookReadiness",
                catalogPhysicalReady == catalogPhysicalTotal ? "ready" : "partial",
                "DTMAPI.GameBridge.DolocTown fixed demand catalog physical routes",
                "ready=" + (catalogPhysicalReady == catalogPhysicalTotal ? "true" : "false") + "; readyCount=" + catalogPhysicalReady + "; total=" + catalogPhysicalTotal + "; policy=all-catalog-hook-routes-independent-of-demand");
            runtime.SetHookStatus(
                "Refactor.DemandedFeatureHookReadiness",
                DemandedFeatureHookTargetsReady ? "ready" : "partial",
                "DTMAPI.GameBridge.DolocTown demand-routed hook readiness",
                BuildDemandedFeatureHookReadinessSummary());
            runtime.SetHookStatus(
                "Refactor.SmokeDiagnosticsHookReadiness",
                SmokeDiagnosticsHookTargetsReady ? "ready" : "partial",
                "DTMAPI.GameBridge.DolocTown layered hook readiness",
                snapshot.SmokeDiagnosticsSummary);
            runtime.SetHookStatus(
                "Refactor.LegacyAllHookTargetsReady",
                snapshot.LegacyAllReady ? "ready" : "mixed",
                "DTMAPI.GameBridge.DolocTown legacy compatibility diagnostic",
                "Legacy mixed readiness remains diagnostic-only in stage 4; it no longer controls global ready. legacyAllReady=" + (snapshot.LegacyAllReady ? "true" : "false"));
        }

        private HookInstallSchedulerSnapshot BuildHookInstallSchedulerSnapshot()
        {
            return hookInstallScheduler.GetSnapshot(
                CoreHookTargetsReady,
                BuildCoreHookReadinessSummary(),
                BuildFeatureHookReadinessSummary(),
                BuildSmokeDiagnosticsHookReadinessSummary(),
                "assemblyLoadSubscribed=" + (assemblyLoadSubscribed ? "true" : "false") + "; policy=" + (CoreHookTargetsReady ? "released-after-core-ready" : "active-until-core-ready"),
                "retryTimerAlive=false; frameRetryActive=" + hookInstallScheduler.RetryActive.ToString().ToLowerInvariant() + "; policy=until-core-and-demanded-routes-ready",
                AllHookTargetsReady,
                assemblyLoadSubscribed,
                false);
        }

        private bool CoreHookTargetsReady => coreHookTargetsReadyOverrideForTests ??
            (IsSaveLoadedHookReady &&
             loadRequestedPatched &&
             loadReturnedPatched &&
             nativeGameFramePatched &&
             saveSavingPatched &&
             saveSavedPatched &&
             returnHomeRequestedPatched &&
             returnHomePatched &&
             IsWorkshopCommitHookReady);

        private bool IsSaveLoadedHookReady => saveLoadedPatched || saveLoadedEventSubscribed;

        private bool IsWorkshopCommitHookReady =>
            workshopReloadPatched &&
            workshopModUiRegisterPatched &&
            workshopModUiHidePatched &&
            workshopModManagerSavePatched &&
            workshopModUiCloseTransactionPatched;

        private bool AllHookTargetsReady => CoreHookTargetsReady && FeatureHookTargetsReady && SmokeDiagnosticsHookTargetsReady;

        internal bool AllHookTargetsReadyForQa => AllHookTargetsReady;

        internal void SetCoreHookReadinessOverrideForTests(bool? value) => coreHookTargetsReadyOverrideForTests = value;

        internal bool HookRetrySourcesAliveForTests => hookInstallScheduler.RetryActive || assemblyLoadSubscribed;

        internal void RequestHookInstallForTests(string reason) => RequestHookInstall(reason);

        private bool FeatureHookTargetsReady => DemandedFeatureHookTargetsReady;

        private bool DemandedFeatureHookTargetsReady
        {
            get
            {
                GetDemandedHookRouteCounts(out int ready, out int total);
                return ready == total;
            }
        }

        private bool SmokeDiagnosticsHookTargetsReady => true;

        private string BuildCoreHookReadinessSummary()
        {
            return "ready=" + (CoreHookTargetsReady ? "true" : "false") +
                "; SaveLoaded=" + (IsSaveLoadedHookReady ? "ready" : "pending") +
                "; LoadGameRequested=" + (loadRequestedPatched ? "ready" : "pending") +
                "; LoadGameReturned=" + (loadReturnedPatched ? "ready" : "pending") +
                "; NativeGameFrame=" + (nativeGameFramePatched ? "ready" : "pending") +
                "; SaveSaving=" + (saveSavingPatched ? "ready" : "pending") +
                "; SaveSaved=" + (saveSavedPatched ? "ready" : "pending") +
                "; ReturnHomeRequested=" + (returnHomeRequestedPatched ? "ready" : "pending") +
                "; ReturnedToTitle=" + (returnHomePatched ? "ready" : "pending") +
                "; WorkshopModUiCommit=" + (IsWorkshopCommitHookReady ? "ready" : "pending");
        }

        private string BuildFeatureHookReadinessSummary() => BuildDemandedFeatureHookReadinessSummary();

        private string BuildDemandedFeatureHookReadinessSummary()
        {
            GetDemandedHookRouteCounts(out int ready, out int total);
            return "ready=" + (ready == total ? "true" : "false") +
                "; readyCount=" + ready +
                "; total=" + total +
                "; policy=demanded-routes-only";
        }

        private string BuildSmokeDiagnosticsHookReadinessSummary() => "ready=true; readyCount=0; total=0; owner=ProductNative; mandatoryGameBridgeTargets=none";
    }
}
