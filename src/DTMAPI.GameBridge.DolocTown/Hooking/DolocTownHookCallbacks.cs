using System;
using System.Collections.Generic;
using System.Diagnostics;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public static class DolocTownHookCallbacks
    {
        private const int HookCallbackShortLogLimit = 3;
        private static readonly TimeSpan HookCallbackSummaryInterval = TimeSpan.FromSeconds(30);
        private static readonly object hookCallbackFailureGate = new object();
        private static readonly object nativeContinuationProbeGate = new object();
        private static readonly Dictionary<string, HookCallbackFailureState> hookCallbackFailures = new Dictionary<string, HookCallbackFailureState>(StringComparer.Ordinal);
        private static DtmApiRuntime? runtime;
        private static Stopwatch? nativeContinuationProbeStopwatch;

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
        public static void LoadGamePrefix(int index)
        {
            SafeCallback("Platform.ResetLoadObservation", () => Bridge?.ResetPlatformWorldObservation());
            StartNativeContinuationProbeStopwatch();
            SafeCallback("LoadGame.NotifyLoadGameRequested", () => Runtime?.NotifyLoadGameRequested(index));
            RecordNativeContinuationProbe("DolocAPI.LoadGame", "Enter");
        }

        public static void LoadGamePostfix(int index, bool __result)
        {
            RecordNativeContinuationProbe("DolocAPI.LoadGame", "Exit");
            SafeCallback("LoadGame.NotifyLoadGameReturned", () => Runtime?.NotifyLoadGameReturned(index, __result));
        }

        public static void NormalGameFramePostfix()
        {
            try
            {
                SafeCallback("Lifecycle.ObserveNativeFrame", () => Bridge?.ObserveFirstNativeWorldFrame());
                SafeCallback("Platform.ObserveNativeFrame", () => Bridge?.ObservePlatformNativeFrame());
                Runtime?.NotifyNativeGameFrame();
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("NormalGameState.OnUpdate.NativeFrameDrain", ex);
            }
        }

        public static void NativeContinuationAfterLoadArchiveDataPrefix()
        {
            RecordNativeContinuationProbe("DolocAPI.AfterLoadArchiveData", "Enter");
        }

        public static void NativeContinuationAfterLoadArchiveDataPostfix()
        {
            RecordNativeContinuationProbe("DolocAPI.AfterLoadArchiveData", "Exit");
        }

        public static void NativeContinuationVersionPatcherLoadAllPrefix()
        {
            RecordNativeContinuationProbe("DolocTown.VersionPatcher.LoadAllVersionPatches", "Enter");
        }

        public static void NativeContinuationVersionPatcherLoadAllPostfix()
        {
            RecordNativeContinuationProbe("DolocTown.VersionPatcher.LoadAllVersionPatches", "Exit");
        }

        public static void NativeContinuationVersionPatcherLoadBeyondPrefix()
        {
            RecordNativeContinuationProbe("DolocTown.VersionPatcher.LoadAllVersionPatchesBeyond", "Enter");
        }

        public static void NativeContinuationVersionPatcherLoadBeyondPostfix()
        {
            RecordNativeContinuationProbe("DolocTown.VersionPatcher.LoadAllVersionPatchesBeyond", "Exit");
        }

        public static void NativeContinuationMapManagerInitPrefix()
        {
            RecordNativeContinuationProbe("DolocTown.MapManager.Init", "Enter");
        }

        public static void NativeContinuationMapManagerInitPostfix()
        {
            RecordNativeContinuationProbe("DolocTown.MapManager.Init", "Exit");
        }

        public static void AfterLoadArchiveDataPostfix(bool isNewGame)
        {
            SafeCallback("Lifecycle.ObserveSaveLoaded", () => Bridge?.ObserveSaveLoadedNativeState());
            Stopwatch breadcrumb = Stopwatch.StartNew();
            Runtime?.RecordSaveLoadedActivationBreadcrumb("Hook.Enter", breadcrumb);
            Runtime?.RecordSaveLoadedActivationBreadcrumb("Hook.BeforeGameBridgeFeatureDispatch", breadcrumb);
            SafeCallback("SaveLoaded.NotifyGameBridgeFeatures", () => Bridge?.NotifyGameBridgeFeaturesSaveLoaded(isNewGame));
            Runtime?.RecordSaveLoadedActivationBreadcrumb("Hook.AfterGameBridgeFeatureDispatch", breadcrumb);
            Runtime?.RecordSaveLoadedActivationBreadcrumb("Hook.BeforeRuntimeNotifySaveLoaded", breadcrumb);
            SafeCallback("SaveLoaded.NotifyRuntime", () => Runtime?.NotifySaveLoaded(isNewGame));
            Runtime?.RecordSaveLoadedActivationBreadcrumb("Hook.AfterRuntimeNotifySaveLoaded", breadcrumb);
            Runtime?.RecordSaveLoadedActivationBreadcrumb("Hook.Exit", breadcrumb);
        }

        public static bool SaveGamePrefix(
            int index,
            ref bool __result)
        {
            bool equipmentReady =
                TryLifecycleCallback(
                    "SaveGame.NotifyEquipmentSlotsSaveSaving",
                    () => Bridge?
                        .NotifyEquipmentSlotsSaveSaving(index));
            bool runtimeReady =
                Runtime?.TryNotifySaveSaving(index) ?? true;
            if (equipmentReady && runtimeReady)
                return true;

            __result = false;
            RecordLifecycleCallbackFailure(
                "SaveGame.SaveSavingFailClosed",
                new InvalidOperationException(
                    "A SaveSaving participant failed before the native save boundary; SaveGame was canceled."));
            return false;
        }

        public static void SaveGamePostfix(int index, bool __result)
        {
            RunSaveSavedIfNativeSucceeded(
                __result,
                () => (Runtime ?? throw new InvalidOperationException("SaveSaved Runtime callback is unavailable.")).NotifySaveSaved(index),
                () => (Bridge ?? throw new InvalidOperationException("SaveSaved GameBridge callback is unavailable.")).NotifyEquipmentSlotsSaveSaved(index),
                () => Bridge?.QaHostSaveSavedNotification?.Invoke(index));
        }

        public static void ReturnHomePrefix()
        {
            SafeCallback("Platform.ResetTitleObservation", () => Bridge?.ResetPlatformWorldObservation());
            SafeCallback("ReturnHome.NotifyRequested", () => Runtime?.NotifyReturnHomeRequested("Harmony Prefix: DolocAPI.ReturnHome"));
        }

        public static void ReturnHomePostfix()
        {
            SafeCallback("ReturnHome.NotifyNativePostfix", () => Runtime?.NotifyReturnHomeNativePostfix("Harmony Postfix: DolocAPI.ReturnHome"));
            SafeCallback("ReturnedToTitle.NotifyGameBridgeFeatures", () => Bridge?.NotifyGameBridgeFeaturesReturnedToTitle());
            SafeCallback("ReturnedToTitle.NotifyRuntime", () => Runtime?.NotifyReturnedToTitle());
        }

        public static void PlatformNewGamePrefix(int index)
        {
            SafeCallback("Platform.NewGame.Start", () => { Bridge?.ResetPlatformWorldObservation(); Runtime?.NotifyPlatformNewGameStarting(index); });
        }
        public static void PlatformNewGamePostfix() => SafeCallback("Platform.NewGame.End", () => Runtime?.NotifyPlatformNewGameCompleted());
        // Void finalizer observes failure without swallowing or replacing the native exception.
        public static void PlatformLoadFinalizer(Exception? __exception)
        {
            if (__exception != null) SafeCallback("Platform.Load.Exception", () => { Bridge?.ResetPlatformWorldObservation(); Runtime?.NotifyPlatformLoadFailure(); });
        }
        public static void PlatformWorldTransitionPrefix() => SafeCallback("Platform.World.Start", () => Bridge?.ObservePlatformWorldTransition());
        public static void PlatformRoomEnteredPostfix(object __instance) => SafeCallback("Platform.Room.Entered", () => Bridge?.ObservePlatformRoomEntered(__instance));

        public static void DolocApiSetEnvCameraPostfix()
        {
            DolocTownGameBridge? bridge = Bridge;
            if (bridge?.HasEnvironmentResetDemand != true)
                return;
            try
            {
                bridge.NotifyGameBridgeFeaturesEnvironmentReset("DolocAPI.SetEnvCamera");
            }
            catch (Exception ex)
            {
                RecordLifecycleCallbackFailure("EnvironmentReset.NotifyFeatures", ex);
            }
        }

        public static void CameraCompatibilitySetEnvCameraPostfix()
        {
            SafeCallback(
                "CameraCompatibility.EnvironmentReset",
                () => Bridge?
                    .NotifyCameraCompatibilityEnvironmentReset(
                        "DolocAPI.SetEnvCamera"));
        }

        public static void HomePageRenderTextMenuPostfix(object __instance)
        {
            SafePostfix("UI.NativeLayoutRepair.HomePageRenderTextMenu", () =>
            {
                Bridge?.NativeUiLayoutRepairService?.RepairHomePageTextMenu(GameBridgeNativeHelpers.ReadMember(__instance, "textMenu"), "HomePageUiState.RenderTextMenu");
                Bridge?.QaHostUiObservationNotification?.Invoke("HomePageUiState.RenderTextMenu");
            });
        }

        public static void MenuUiSetCapacityPostfix(object __instance, int __0)
        {
            SafePostfix("UI.NativeLayoutRepair.MenuUiSetCapacity", () =>
            {
                Bridge?.NativeUiLayoutRepairService?.RepairMainMenu(__instance, "MenuUI.SetCapacity");
                Bridge?.QaHostUiObservationNotification?.Invoke("MenuUI.SetCapacity");
            });
        }

        public static void MainMenuPanelOnStartShowPostfix(object __instance)
        {
            SafePostfix("UI.NativeLayoutRepair.MainMenuPanelOnStartShow", () =>
            {
                Bridge?.NativeUiLayoutRepairService?.UpdateActiveMenuLayout();
                Bridge?.QaHostUiObservationNotification?.Invoke("MainMenuPanel.OnStartShow");
            });
        }

        public static void GameDataPanelSetCapacityPostfix(object __instance, int __0)
        {
            SafePostfix("UI.NativeLayoutRepair.GameDataPanelSetCapacity", () => Bridge?.QaHostUiObservationNotification?.Invoke("GameDataPanel.SetCapacity"));
        }

        public static void ReloadModsPostfix(object __instance)
        {
            SafePostfix(
                "Workshop.ModManager.ReloadMods",
                () => (Bridge ?? throw new InvalidOperationException(
                        "Workshop GameBridge callback is unavailable."))
                    .HandleNativeModManagerReloaded(__instance));
        }

        public static void ModUiStateRegisterPrefix(object __instance)
        {
            SafePostfix(
                "Workshop.ModUiState.Register",
                () => (Bridge ?? throw new InvalidOperationException(
                        "Workshop GameBridge callback is unavailable."))
                    .BeginOfficialModUiTransaction(__instance));
        }

        public static void ModUiStateHidePrefix(object __instance)
        {
            SafePostfix(
                "Workshop.ModUiState.Hide",
                () => (Bridge ?? throw new InvalidOperationException(
                        "Workshop GameBridge callback is unavailable."))
                    .BeginOfficialModUiClose(__instance));
        }

        public static void SaveModManagerPostfix(
            object __0,
            bool __result)
        {
            SafePostfix(
                "Workshop.DataPersistenceManager.SaveModManager",
                () => (Bridge ?? throw new InvalidOperationException(
                        "Workshop GameBridge callback is unavailable."))
                    .ObserveOfficialModManagerSave(__0, __result));
        }

        public static void ModUiStateCloseTransactionPostfix(
            object __instance)
        {
            SafePostfix(
                "Workshop.ModUiState.Hide.CloseTransaction",
                () => (Bridge ?? throw new InvalidOperationException(
                        "Workshop GameBridge callback is unavailable."))
                    .CompleteOfficialModUiClose(__instance));
        }

        public static bool WwiseInternalPostSoundEventPrefix(string __0, object? __1, object? __2, bool __3, ref bool __result)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.AudioReplacement))
                return true;
            AudioReplacementService? service = Bridge?.AudioReplacementService;
            if (service?.HasEnabledDefinitions != true)
                return true;
            try
            {
                return service.HandleNativeSoundEvent(__0, __1, __2, __3, ref __result);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("AudioReplacement.WwiseSoundManager.InternalPostSoundEvent", ex);
                return true;
            }
        }

        public static void DungeonResourceModelPaperBoxOnInteractPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.AudioReplacement))
                return;
            AudioReplacementService? service = Bridge?.AudioReplacementService;
            if (service?.HasEnabledPaperBoxDiagnosticDefinitions == true)
            {
                try { service.RecordPaperBoxInteract(__instance); }
                catch (Exception ex) { RecordHookCallbackFailure("AudioReplacement.DungeonResourceModelPaperBox.OnInteract", ex); }
            }
        }

        public static void AnimalPlayAnimalSoundPrefix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.AudioReplacementAnimalVoiceContext))
                return;
            AudioReplacementService? service = Bridge?.AudioReplacementService;
            if (service?.HasEnabledAnimalVoiceDefinitions == true)
            {
                try { service.BeginAnimalSoundContext(__instance); }
                catch (Exception ex) { RecordLifecycleCallbackFailure("AudioReplacement.Animal.PlayAnimalSound.Prefix", ex); }
            }
        }

        public static void AnimalPlayAnimalSoundPostfix()
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.AudioReplacementAnimalVoiceContext))
                return;
            AudioReplacementService? service = Bridge?.AudioReplacementService;
            if (service?.HasEnabledAnimalVoiceDefinitions == true)
            {
                try { service.EndAnimalSoundContext(); }
                catch (Exception ex) { RecordLifecycleCallbackFailure("AudioReplacement.Animal.PlayAnimalSound.Postfix", ex); }
            }
        }

        public static void ModDataConstructorPostfix(object __instance, object __0)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.WorkshopAuthoring))
                return;
            try
            {
                Bridge?.TryMarkDtmapiLocalUploadData(__instance, __0);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Workshop.LocalUploadPlan.Display", ex);
            }
        }

        public static bool SteamWorkshopUploaderResolveUploadPlanPrefix(object __instance, object __0, object __1)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.WorkshopAuthoring))
                return true;
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
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.WorkshopAuthoring))
                return;
            try
            {
                Bridge?.TrackDtmapiUploadPlanFallbackAfterNativeQuery(__instance, __0, __1);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Workshop.LocalUploadPlanKnownIdFallback.TrackResolveUploadPlan", ex);
            }
        }

        public static void ItemTitlePostfix(object __instance, ref string __result)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishRoeTooltip))
                return;
            string original = __result;
            try
            {
                __result = Bridge?.FishRoeTooltipService?.DecorateFishRoeTitle(__instance, original) ?? original;
            }
            catch (Exception ex)
            {
                __result = original;
                RecordHookCallbackFailure("Items.FishRoeTooltip.ItemTitle", ex);
            }
        }

        public static void ItemDescriptionPostfix(object __instance, ref string __result)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishRoeTooltip))
                return;
            string original = __result;
            try
            {
                __result = Bridge?.FishRoeTooltipService?.DecorateFishRoeDetail(__instance, original) ?? original;
            }
            catch (Exception ex)
            {
                __result = original;
                RecordHookCallbackFailure("Items.FishRoeTooltip.ItemDescription", ex);
            }
        }

        public static void ItemDetailInfoPostfix(object __instance, ref string __result)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishRoeTooltip))
                return;
            string original = __result;
            try
            {
                __result = Bridge?.FishRoeTooltipService?.DecorateFishRoeDetail(__instance, original) ?? original;
            }
            catch (Exception ex)
            {
                __result = original;
                RecordHookCallbackFailure("Items.FishRoeTooltip.ItemDetailInfo", ex);
            }
        }

        public static void AnimalFullInfoDataCtorPostfix(object __instance, object __0)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.AnimalViewer))
                return;
            try
            {
                Bridge?.AnimalViewerService?.DecorateAnimalFullInfoData(__instance, __0);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Animals.ViewerRendering.FullInfoDataCtor", ex);
            }
        }

        public static void AnimalViewerShowPrefix(object __instance, object __0)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.AnimalViewer))
                return;
            try
            {
                Bridge?.AnimalViewerService?.PrepareAnimalProgressOverlayBeforeShow(__instance, __0);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Animals.ViewerRendering.ViewerShowPrefix", ex);
            }
        }

        public static void AnimalViewerShowPostfix(object __instance, object __0)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.AnimalViewer))
                return;
            try
            {
                Bridge?.AnimalViewerService?.RenderAnimalProgressOverlay(__instance, __0);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Animals.ViewerRendering.ViewerShowPostfix", ex);
            }
        }

        public static void AnimalPanelUiStateUnregisterPostfix()
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.AnimalViewer))
                return;
            SafePostfix("Animals.ViewerRendering.PanelUnregisterPostfix", () => Bridge?.AnimalViewerService?.NotifyAnimalPanelUnregistered());
        }

        public static void AnimalOnRenderPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.CustomAnimalAnimatorBridge))
                return;
            CustomAnimalAnimatorBridgeService? service = Bridge?.CustomAnimalAnimatorBridgeService;
            if (service?.HasPngSpriteCallbackDemand == true)
            {
                try { service.ApplyPngSpriteOverrideContext(__instance); }
                catch (Exception ex) { RecordHookCallbackFailure("CustomAnimals.PngSpriteBridge.AnimalOnRender", ex); }
            }
            if (service?.HasDiagnosticCallbackDemand == true)
            {
                try { service.RecordAnimalOnRenderDiagnostic(__instance); }
                catch (Exception ex) { RecordHookCallbackFailure("CustomAnimals.SleepWakeDiagnostics.AnimalOnRender", ex); }
            }
        }

        public static void AnimalDebugSetAdultPostfix(object __instance, bool __0)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.CustomAnimalAnimatorBridge))
                return;
            CustomAnimalAnimatorBridgeService? service = Bridge?.CustomAnimalAnimatorBridgeService;
            if (service?.HasPngSpriteCallbackDemand == true)
            {
                try { service.ApplyPngSpriteOverrideContext(__instance); }
                catch (Exception ex) { RecordHookCallbackFailure("CustomAnimals.PngSpriteBridge.AnimalDebugSetAdult", ex); }
            }
            if (service?.HasDiagnosticCallbackDemand == true)
            {
                try { service.RecordAnimalDebugSetAdultDiagnostic(__instance, __0); }
                catch (Exception ex) { RecordHookCallbackFailure("CustomAnimals.SleepWakeDiagnostics.AnimalDebugSetAdult", ex); }
            }
        }

        public static void AnimalRendererOnRecyclePostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.CustomAnimalAnimatorBridge))
                return;
            CustomAnimalAnimatorBridgeService? service = Bridge?.CustomAnimalAnimatorBridgeService;
            if (service?.HasPngSpriteCallbackDemand == true)
            {
                try { service.ClearPngSpriteOverrideContext(__instance); }
                catch (Exception ex) { RecordHookCallbackFailure("CustomAnimals.PngSpriteBridge.AnimalRendererOnRecycle", ex); }
            }
        }

        public static void AnimalSleepPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.CustomAnimalAnimatorBridge))
                return;
            CustomAnimalAnimatorBridgeService? service = Bridge?.CustomAnimalAnimatorBridgeService;
            if (service?.HasDiagnosticCallbackDemand == true)
            {
                try { service.RecordAnimalSleepDiagnostic(__instance); }
                catch (Exception ex) { RecordHookCallbackFailure("CustomAnimals.SleepWakeDiagnostics.AnimalSleep", ex); }
            }
        }

        public static void AnimalWakeUpPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.CustomAnimalAnimatorBridge))
                return;
            CustomAnimalAnimatorBridgeService? service = Bridge?.CustomAnimalAnimatorBridgeService;
            if (service?.HasDiagnosticCallbackDemand == true)
            {
                try { service.RecordAnimalWakeUpDiagnostic(__instance); }
                catch (Exception ex) { RecordHookCallbackFailure("CustomAnimals.SleepWakeDiagnostics.AnimalWakeUp", ex); }
            }
        }

        public static void AnimalControllerOnUpdatePostfix(object __instance, float __0)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.CustomAnimalAnimatorBridge))
                return;
            CustomAnimalAnimatorBridgeService? service = Bridge?.CustomAnimalAnimatorBridgeService;
            if (service?.HasSleepTaskCallbackDemand == true)
            {
                try { service.EnforceSleepTaskBoundaryAfterAnimalControllerUpdate(__instance); }
                catch (Exception ex) { RecordHookCallbackFailure("CustomAnimals.SleepTaskBoundary.AnimalControllerOnUpdate", ex); }
            }
        }

        public static void AnimalCallToRoomPostfix(object __instance, object __0, object __1)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.CustomAnimalAnimatorBridge))
                return;
            CustomAnimalAnimatorBridgeService? service = Bridge?.CustomAnimalAnimatorBridgeService;
            if (service?.HasDiagnosticCallbackDemand == true)
            {
                try { service.RecordAnimalCallToRoomDiagnostic(__instance, __0, __1); }
                catch (Exception ex) { RecordHookCallbackFailure("CustomAnimals.SleepWakeDiagnostics.AnimalCallToRoom", ex); }
            }
        }

        public static void AnimalRendererOnFellPrefix(object __instance, object __0)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.CustomAnimalAnimatorBridge))
                return;
            CustomAnimalAnimatorBridgeService? service = Bridge?.CustomAnimalAnimatorBridgeService;
            if (service?.HasDiagnosticCallbackDemand == true)
            {
                try { service.RecordAnimalRendererOnFellPrefixDiagnostic(__instance, __0); }
                catch (Exception ex) { RecordHookCallbackFailure("CustomAnimals.SleepWakeDiagnostics.AnimalRendererOnFellPrefix", ex); }
            }
        }

        public static void AnimalRendererOnFellPostfix(object __instance, object __0, bool __result)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.CustomAnimalAnimatorBridge))
                return;
            CustomAnimalAnimatorBridgeService? service = Bridge?.CustomAnimalAnimatorBridgeService;
            if (service?.HasDiagnosticCallbackDemand == true)
            {
                try { service.RecordAnimalRendererOnFellPostfixDiagnostic(__instance, __result); }
                catch (Exception ex) { RecordHookCallbackFailure("CustomAnimals.SleepWakeDiagnostics.AnimalRendererOnFellPostfix", ex); }
            }
        }

        public static void AnimalRendererPlayAnimationPostfix(object __instance, string __0, bool __1, float __2)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.CustomAnimalAnimatorBridge))
                return;
            CustomAnimalAnimatorBridgeService? service = Bridge?.CustomAnimalAnimatorBridgeService;
            if (service?.HasDiagnosticCallbackDemand == true)
            {
                try { service.RecordAnimalRendererPlayAnimationDiagnostic(__instance, __0, __1, __2); }
                catch (Exception ex) { RecordHookCallbackFailure("CustomAnimals.SleepWakeDiagnostics.AnimalRendererPlayAnimation", ex); }
            }
        }

        public static void AnimalRendererFixedUpdatePostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.CustomAnimalAnimatorBridge))
                return;
            CustomAnimalAnimatorBridgeService? service = Bridge?.CustomAnimalAnimatorBridgeService;
            if (service?.HasDiagnosticCallbackDemand == true)
            {
                try { service.RecordAnimalRendererFixedUpdateDiagnostic(__instance); }
                catch (Exception ex) { RecordHookCallbackFailure("CustomAnimals.SleepWakeDiagnostics.AnimalRendererFixedUpdate", ex); }
            }
        }

        public static void ToolColliderHandleToolsPostfix(object __instance, object other)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ActionCompletionToolColliderOwned))
                return;
            try
            {
                Bridge?.ActionCompletionService?.ApplyOneActionToolHit(__instance, other);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("ToolCollider.HandleTools.ActionCompletion", ex);
            }
        }

        public static void AgentStateToolEnterPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ActionSpeedToolStagesOwned))
                return;
            try
            {
                Bridge?.ActionSpeedService?.ApplyActionSpeedToolEnter(__instance);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("AgentStateTool.OnEnter.ApplyActionSpeed", ex);
            }
        }

        public static void AgentStateToolExitPostfix()
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ActionSpeedToolExitOwned))
                return;
            SafeCallback("AgentStateTool.OnExit.RestoreActionSpeed", () => Bridge?.ActionSpeedService?.RestoreActionSpeed("AgentStateTool.OnExit"));
        }

        public static void AgentStateInteractEnterPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ActionSpeedInteractionStagesOwned))
                return;
            try
            {
                Bridge?.ActionSpeedService?.ApplyActionSpeedInteractEnter(__instance);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("AgentStateInteract.OnEnter.ApplyActionSpeed", ex);
            }
        }

        public static void AgentStateInteractExitPostfix()
        {
            if (HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ActionCompletionInteractExitOwned))
                SafeCallback("AgentStateInteract.OnExit.ApplyOneActionEquipmentFill", () => Bridge?.ActionCompletionService?.ApplyOneActionEquipmentFillAfterInteract());
            if (HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ActionSpeedInteractExitOwned))
                SafeCallback("AgentStateInteract.OnExit.RestoreActionSpeed", () => Bridge?.ActionSpeedService?.RestoreActionSpeed("AgentStateInteract.OnExit"));
        }

        public static void AgentStateEatEnterPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ActionSpeedInteractionStagesOwned))
                return;
            try
            {
                Bridge?.ActionSpeedService?.ApplyActionSpeedEatEnter(__instance);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("AgentStateEat.OnEnter.ApplyActionSpeed", ex);
            }
        }

        public static void AgentControllerStateUseItemContinuesPrefix(ref float __0)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ActionSpeedInteractionStagesOwned))
                return;
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
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ActionSpeedInteractionStagesOwned))
                return;
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
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ActionSpeedInteractionStagesOwned))
                return;
            try
            {
                Bridge?.ActionSpeedService?.MarkNativeAnimalInteract(__instance);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("AnimalRenderer.OnInteract.MarkActionSpeedNativeOwner", ex);
            }
        }

        public static void AgentStateBaseExitPostfix()
        {
            if (HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ActionSpeedBaseExitOwned))
                SafeCallback("AgentStateBase.OnExit.RestoreActionSpeed", () => Bridge?.ActionSpeedService?.RestoreActionSpeed("AgentStateBase.OnExit"));
        }

        public static void FishingCompatibilityBaseExitPostfix()
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            SafeCallback(
                "AgentStateBase.OnExit.RestoreFishingCompatibilityAnimatorSpeeds",
                () => Bridge?.FishingCompatibilityCallbackService?.RestoreExperimentalAnimatorSpeeds("AgentStateBase.OnExit"));
        }

        public static void FishingCompatibilityReadyEnterPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.NotifyFishingPhase("Ready", __instance);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.Ready.OnEnter.NotifyPhase", ex);
            }
        }

        public static void FishingCompatibilityReadyPlayPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.ApplyFishingReadyAutomation(__instance);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.Ready.OnPlay.Automation", ex);
            }
        }

        public static void FishingCompatibilityCastEnterPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.NotifyFishingPhase("Cast", __instance);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.Cast.OnEnter.NotifyPhase", ex);
            }
        }

        public static void FishingCompatibilityWaitEnterPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.NotifyFishingPhase("Wait", __instance);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.Wait.OnEnter.NotifyPhase", ex);
            }
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.ApplyFishingWaitAutomation(__instance, "AgentStateFishingWait.OnEnter Postfix");
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.Wait.OnEnter.ApplyAutomation", ex);
            }
        }

        public static void FishingCompatibilityWaitPlayPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.ApplyFishingWaitAutomation(__instance, "AgentStateFishingWait.OnPlay Postfix");
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.Wait.OnPlay.ApplyAutomation", ex);
            }
        }

        public static void FishingCompatibilityMiniGameStartPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.NotifyFishingMiniGameStart(__instance);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.MiniGame.Start", ex);
            }
        }

        public static void FishingCompatibilityMiniGameUpdatePrefix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.PrepareFishingMiniGameAutomationInput(__instance);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.MiniGame.Update.PrepareInput", ex);
            }
        }

        public static void FishingCompatibilityMiniGameUpdatePostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.ApplyFishingMiniGameAutomationTick(__instance);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.MiniGame.Update", ex);
            }
        }

        public static void FishingCompatibilityMiniGameStopPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.NotifyFishingMiniGameStop(__instance);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.MiniGame.Stop", ex);
            }
        }

        public static bool FishingCompatibilityInputNormalUseToolPrefix(ref bool __result)
        {
            return TryFishingMiniGameInputOverride("NormalUseTool", ref __result);
        }

        public static bool FishingCompatibilityInputNormalUseToolInProgressPrefix(ref bool __result)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return true;
            try
            {
                if (Bridge?.FishingCompatibilityCallbackService?.TryOverrideFishingReadyChargeInput("NormalUseToolInProgress", out bool value) == true)
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

        public static bool FishingCompatibilityInputNormalUseItemPrefix(ref bool __result)
        {
            return TryFishingMiniGameInputOverride("NormalUseItem", ref __result);
        }

        public static bool FishingCompatibilityInputNormalUseItemInProgressPrefix(ref bool __result)
        {
            return TryFishingMiniGameInputOverride("NormalUseItemInProgress", ref __result);
        }

        public static bool FishingCompatibilityInputNormalFishingPrefix(ref bool __result)
        {
            return TryFishingMiniGameInputOverride("NormalFishing", ref __result);
        }

        public static bool FishingCompatibilityInputNormalFishingInProgressPrefix(ref bool __result)
        {
            return TryFishingMiniGameInputOverride("NormalFishingInProgress", ref __result);
        }

        private static bool TryFishingMiniGameInputOverride(string inputName, ref bool result)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return true;
            try
            {
                if (Bridge?.FishingCompatibilityCallbackService?.TryOverrideFishingMiniGameInput(inputName, out bool value) == true)
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

        private static void StartNativeContinuationProbeStopwatch()
        {
            lock (nativeContinuationProbeGate)
                nativeContinuationProbeStopwatch = Stopwatch.StartNew();
        }

        private static long GetNativeContinuationProbeElapsedMs()
        {
            lock (nativeContinuationProbeGate)
                return nativeContinuationProbeStopwatch?.ElapsedMilliseconds ?? 0;
        }

        private static void RecordNativeContinuationProbe(string method, string phase, Exception? exception = null)
        {
            try
            {
                Runtime?.RecordNativeLoadContinuationBreadcrumb(method, phase, GetNativeContinuationProbeElapsedMs(), exception?.GetType().Name);
            }
            catch
            {
                // Probe breadcrumbs must not affect native save-load behavior.
            }
        }

        public static void FishingCompatibilityWaitNextStatePostfix(object __instance, object? __result)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.ConfirmFishingWaitNativeReelAccepted(__instance, __result);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.Wait.NextState.NativeReelAccepted", ex);
            }
        }

        public static void FishingCompatibilityRodCastHookPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.AdjustFishingCastHookPhysics(__instance);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.FishRodRenderer.CastHook.Physics", ex);
            }
        }

        public static void FishingCompatibilityPullEnterPostfix(object __instance)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.NotifyFishingPhase("Pull", __instance);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.Pull.OnEnter.NotifyPhase", ex);
            }
        }

        public static void FishingCompatibilityPullExitPostfix()
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.NotifyFishingPhase("Cooldown", null);
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("AgentStateFishingPull.OnExit.NotifyCooldown", ex);
            }
            finally
            {
                try
                {
                    Bridge?.FishingCompatibilityCallbackService?.RestoreExperimentalAnimatorSpeeds("AgentStateFishingPull.OnExit");
                }
                catch (Exception ex)
                {
                    RecordHookCallbackFailure("AgentStateFishingPull.OnExit.RestoreExperimentalAnimatorSpeeds", ex);
                }
                try
                {
                    Bridge?.FishingCompatibilityCallbackService?.NotifyFishingNativeExit("AgentStateFishingPull.OnExit");
                }
                catch (Exception ex)
                {
                    RecordHookCallbackFailure("AgentStateFishingPull.OnExit.LifecycleBoundary", ex);
                }
            }
        }

        public static void FishingCompatibilityRodPullPostfix(ref float __result)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.AdjustFishingPullDurationResult(ref __result, "FishRodRenderer.Pull");
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.FishRodRenderer.Pull.Duration", ex);
            }
        }

        public static void FishingCompatibilityRodPullCancelPostfix(ref float __result)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.FishingCompatibility))
                return;
            try
            {
                Bridge?.FishingCompatibilityCallbackService?.AdjustFishingPullDurationResult(ref __result, "FishRodRenderer.PullCancel");
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Fishing.FishRodRenderer.PullCancel.Duration", ex);
            }
        }

        public static Array ArchiveDataHandleGetAvailableInventoriesPostfix(object __instance, object __0, object __1, bool __2, Array __result)
        {
            if (!HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ChestLocatorEnhancer))
                return __result;
            try
            {
                return Bridge?.ChestLocatorEnhancerService?.ExtendAvailableInventoriesForChestLocator(__instance, __0, __1, __2, __result) ?? __result;
            }
            catch (Exception ex)
            {
                RecordHookCallbackFailure("Inventory.ChestLocatorEnhancer.GetAvailableInventories", ex);
                return __result;
            }
        }

        internal static bool HasChestLocatorEnhancerRetainedCallbackDemand() =>
            HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ChestLocatorEnhancer);

        private static bool HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand demand) =>
            Bridge?.HasRetainedCallbackDemand(demand) == true;

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

        private static bool TryLifecycleCallback(string operation, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                RecordLifecycleCallbackFailure(operation, ex);
                return false;
            }
        }

        private static bool TryLifecycleReceipt(string operation, Func<bool> action)
        {
            try
            {
                // A false receipt is an expected degraded result whose production
                // owner has already published its warning/status. It gates only the
                // optional QA completion and must not be promoted to a second Error.
                return action();
            }
            catch (Exception ex)
            {
                RecordLifecycleCallbackFailure(operation, ex);
                return false;
            }
        }

        private static void RunSaveSavedQaLifecycle(
            Action runtimeNotification,
            Action productionPersistence,
            Action qaNotification)
        {
            bool runtimeSucceeded = TryLifecycleCallback("SaveGame.NotifySaveSaved", runtimeNotification);
            bool productionSucceeded = TryLifecycleCallback("SaveGame.NotifyEquipmentSlotsSaved", productionPersistence);
            if (runtimeSucceeded && productionSucceeded)
                SafeCallback("SaveGame.NotifyQaHostSaved", qaNotification);
        }

        private static void RunSaveSavedIfNativeSucceeded(
            bool nativeSaveSucceeded,
            Action runtimeNotification,
            Action productionPersistence,
            Action qaNotification)
        {
            if (!nativeSaveSucceeded)
                return;
            RunSaveSavedQaLifecycle(runtimeNotification, productionPersistence, qaNotification);
        }

        private static void RunWorkshopQaLifecycle(Func<bool> captureReceipt, Action runtimeNotification, Action qaNotification)
        {
            bool captureSucceeded = TryLifecycleReceipt("Workshop.CaptureNativeSubscriptions", captureReceipt);
            bool runtimeSucceeded = TryLifecycleCallback("Workshop.NotifyModListChanged", runtimeNotification);
            if (captureSucceeded && runtimeSucceeded)
                SafeCallback("Workshop.NotifyQaHostReloadCompleted", qaNotification);
        }

        internal static void RunSaveSavedQaLifecycleForTests(Action runtimeNotification, Action productionPersistence, Action qaNotification)
        {
            RunSaveSavedQaLifecycle(
                runtimeNotification ?? throw new ArgumentNullException(nameof(runtimeNotification)),
                productionPersistence ?? throw new ArgumentNullException(nameof(productionPersistence)),
                qaNotification ?? throw new ArgumentNullException(nameof(qaNotification)));
        }

        internal static void RunSaveSavedIfNativeSucceededForTests(
            bool nativeSaveSucceeded,
            Action runtimeNotification,
            Action productionPersistence,
            Action qaNotification)
        {
            RunSaveSavedIfNativeSucceeded(
                nativeSaveSucceeded,
                runtimeNotification ?? throw new ArgumentNullException(nameof(runtimeNotification)),
                productionPersistence ?? throw new ArgumentNullException(nameof(productionPersistence)),
                qaNotification ?? throw new ArgumentNullException(nameof(qaNotification)));
        }

        internal static void RunWorkshopQaLifecycleForTests(Func<bool> captureReceipt, Action runtimeNotification, Action qaNotification)
        {
            RunWorkshopQaLifecycle(
                captureReceipt ?? throw new ArgumentNullException(nameof(captureReceipt)),
                runtimeNotification ?? throw new ArgumentNullException(nameof(runtimeNotification)),
                qaNotification ?? throw new ArgumentNullException(nameof(qaNotification)));
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
