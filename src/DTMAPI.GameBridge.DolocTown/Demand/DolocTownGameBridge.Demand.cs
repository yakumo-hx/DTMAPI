using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;

namespace DTMAPI.GameBridge.DolocTown
{
    [Flags]
    internal enum GameBridgeRetainedCallbackDemand
    {
        None = 0,
        FishingCompatibility = 1 << 0,
        FishRoeTooltip = 1 << 1,
        ChestLocatorEnhancer = 1 << 2,
        SaveSlots = 1 << 3,
        AnimalViewer = 1 << 5,
        CustomAnimalAnimatorBridge = 1 << 6,
        AudioReplacement = 1 << 7,
        ActionSpeed = 1 << 8,
        ActionCompletion = 1 << 9,
        AgentStateToolExitShared = 1 << 10,
        AgentStateInteractExitShared = 1 << 11,
        AgentStateBaseExitShared = 1 << 12,
        ToolColliderShared = 1 << 13,
        ActionSpeedToolStages = 1 << 14,
        ActionSpeedInteractionStages = 1 << 15,
        AudioReplacementAnimalVoiceContext = 1 << 16,
        WorkshopAuthoring = 1 << 20,
        ActionCompletionToolColliderOwned = 1 << 22,
        ActionCompletionInteractExitOwned = 1 << 23,
        ActionSpeedToolStagesOwned = 1 << 24,
        ActionSpeedToolExitOwned = 1 << 25,
        ActionSpeedInteractionStagesOwned = 1 << 26,
        ActionSpeedInteractExitOwned = 1 << 27,
        ActionSpeedBaseExitOwned = 1 << 28,
        ItemDisplayNameEnvironmentReset = 1 << 29
    }

    public sealed partial class DolocTownGameBridge
    {
        private readonly object demandRouteGate = new object();
        private readonly Dictionary<string, GameBridgeDemandRoute> demandRoutes = new Dictionary<string, GameBridgeDemandRoute>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> pendingDemandRouteIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private GameBridgeDemandRoute[] activeDemandUpdaterSnapshot = Array.Empty<GameBridgeDemandRoute>();
        private long demandUpdaterDispatchCount;
        private long demandUpdaterFailureCount;
        private long optionalDemandUpdaterDispatchCount;
        private long optionalHookInstallRequestCount;
        private long hookSchedulerIdleFrameFastPathCount;
        private long hookInstallProcessedBatchCount;
        private long hookReadinessPublishCount;
        private long routePatchStateRefreshPassCount;
        private long activeDemandUpdaterSnapshotRebuildCount;
        private int retainedCallbackDemandMask;
        private bool? coreHookTargetsReadyOverrideForTests;
        private bool demandRoutingInitialized;

        private void InitializeGameBridgeDemandRouting()
        {
            if (demandRoutingInitialized)
                return;

            foreach (RuntimeCapabilityDescriptor descriptor in GameBridgeDemandRoutes.Catalog)
            {
                runtime.DemandCoordinator.RegisterCapability(descriptor);
                demandRoutes[descriptor.CapabilityId] = new GameBridgeDemandRoute(descriptor.CapabilityId, descriptor.Outcome, descriptor.HookRouteId);
            }

            ConfigureDemandRoute(GameBridgeDemandRoutes.CoreLifecycle, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, IsCoreLifecycleHookRouteInstalled, IsAnyCoreLifecycleHookInstalled);
            ConfigureDemandRoute(GameBridgeDemandRoutes.CoreUiContext, TimeSpan.Zero, RefreshUiContext, requestsHookInstall: false, null);
            ConfigureDemandRoute(GameBridgeDemandRoutes.ContentRefreshDrain, TimeSpan.Zero, UpdateContentRefreshGenerationDrain, requestsHookInstall: false, null);
            ConfigureDemandRoute(GameBridgeDemandRoutes.NativeUiLayoutDiagnostics, TimeSpan.FromMilliseconds(250), () => nativeUiLayoutDiagnosticsFeature?.Update(), requestsHookInstall: true, IsNativeUiLayoutHookRouteInstalled, IsAnyNativeUiLayoutHookInstalled);
            ConfigureDemandRoute(GameBridgeDemandRoutes.Camera, TimeSpan.Zero, () => cameraFeature?.Update(), requestsHookInstall: true, () => cameraFeature?.HookBridge.SetEnvCameraPatched == true);
            ConfigureDemandRoute(GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, () => environmentResetHookBridge?.SetEnvCameraPatched == true);
            ConfigureDemandRoute(GameBridgeDemandRoutes.AnimalViewer, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, () => animalViewerFeature?.HookBridge.HooksReady == true, IsAnyAnimalViewerHookInstalled);
            ConfigureDemandRoute(GameBridgeDemandRoutes.AnimalViewerSession, TimeSpan.FromMilliseconds(80), () => animalViewerFeature?.Update(), requestsHookInstall: false, null);
            ConfigureDemandRoute(GameBridgeDemandRoutes.FishRoeTooltip, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, () => fishRoeTooltipFeature?.HookBridge.AllPatched == true, IsAnyFishRoeHookInstalled);
            ConfigureDemandRoute(GameBridgeDemandRoutes.ChestLocatorEnhancer, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, () => chestLocatorEnhancerFeature?.HookBridge.AvailableInventoriesPatched == true);
            ConfigureDemandRoute(GameBridgeDemandRoutes.ActionCompletion, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, IsActionCompletionDependencyClosureInstalled, IsAnyActionCompletionDependencyInstalled);
            ConfigureDemandRoute(GameBridgeDemandRoutes.AgentStateToolExitShared, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, () => agentStateLifecycleHooks?.ToolExitPatched == true);
            ConfigureDemandRoute(GameBridgeDemandRoutes.AgentStateInteractExitShared, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, () => agentStateLifecycleHooks?.InteractExitPatched == true);
            ConfigureDemandRoute(GameBridgeDemandRoutes.AgentStateBaseExitShared, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, () => agentStateLifecycleHooks?.BaseExitPatched == true);
            ConfigureDemandRoute(GameBridgeDemandRoutes.ToolColliderShared, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, () => toolColliderHitHooks?.PostfixPatched == true);

            ConfigureDemandRoute(GameBridgeDemandRoutes.FishingCompatibility, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, IsFishingCompatibilityHookRouteInstalled, IsAnyFishingCompatibilityHookInstalled);
            ConfigureDemandRoute(GameBridgeDemandRoutes.FishingCompatibilityUpdater, TimeSpan.Zero, () => fishingCompatibilityFeature?.Update(), requestsHookInstall: false, null);
            ConfigureDemandRoute(GameBridgeDemandRoutes.SaveSlots, TimeSpan.FromSeconds(3), () => saveSlotsFeature?.Update(), requestsHookInstall: false, null);
            ConfigureDemandRoute(GameBridgeDemandRoutes.SaveSlotsPendingRestore, TimeSpan.FromMilliseconds(750), () => saveSlotsFeature?.Update(), requestsHookInstall: false, null);
            ConfigureDemandRoute(GameBridgeDemandRoutes.ActionSpeed, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, IsActionSpeedHookRouteInstalled, IsAnyActionSpeedHookInstalled);
            ConfigureDemandRoute(GameBridgeDemandRoutes.ActionSpeedAutoFill, TimeSpan.Zero, () => actionSpeedFeature?.Update(), requestsHookInstall: false, null);
            ConfigureDemandRoute(GameBridgeDemandRoutes.ActionSpeedToolStages, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, () => actionSpeedFeature?.HookBridge.ToolEnterPatched == true);
            ConfigureDemandRoute(GameBridgeDemandRoutes.ActionSpeedInteractionStages, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, () => actionSpeedFeature?.HookBridge.InteractionStageHooksReady == true, IsAnyActionSpeedInteractionStageHookInstalled);
            ConfigureDemandRoute(GameBridgeDemandRoutes.CustomAnimalAnimatorBridge, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, () => customAnimalAnimatorBridgeFeature?.HookBridge.HooksReady == true, IsAnyCustomAnimalHookInstalled);
            ConfigureDemandRoute(GameBridgeDemandRoutes.AudioReplacement, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, () => audioReplacementFeature?.HookBridge.InternalPostSoundEventPatched == true, () => audioReplacementFeature?.HookBridge.InternalPostSoundEventPatched == true);
            ConfigureDemandRoute(GameBridgeDemandRoutes.AudioReplacementAnimalVoiceContext, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, IsAudioReplacementAnimalVoiceContextInstalled, IsAnyAudioReplacementAnimalVoiceContextInstalled);
            ConfigureDemandRoute(GameBridgeDemandRoutes.AudioReplacementPending, TimeSpan.Zero, UpdateAudioReplacementPendingWork, requestsHookInstall: false, null);
            ConfigureDemandRoute(GameBridgeDemandRoutes.WorkshopAuthoring, Timeout.InfiniteTimeSpan, null, requestsHookInstall: true, IsWorkshopAuthoringHookRouteInstalled, IsAnyWorkshopAuthoringHookInstalled);
            ConfigureDemandRoute(GameBridgeDemandRoutes.WorkshopPendingUploadPlan, TimeSpan.Zero, ProcessPendingDtmapiUploadPlanFallbacks, requestsHookInstall: false, null);
            ConfigureDemandRoute(GameBridgeDemandRoutes.QaHost, TimeSpan.Zero, () => qaHostFrameUpdate?.Invoke(), requestsHookInstall: false, null);

            MapRetainedCallbackDemand(GameBridgeDemandRoutes.FishingCompatibility, GameBridgeRetainedCallbackDemand.FishingCompatibility);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.FishRoeTooltip, GameBridgeRetainedCallbackDemand.FishRoeTooltip);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.ChestLocatorEnhancer, GameBridgeRetainedCallbackDemand.ChestLocatorEnhancer);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.AnimalViewer, GameBridgeRetainedCallbackDemand.AnimalViewer);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.CustomAnimalAnimatorBridge, GameBridgeRetainedCallbackDemand.CustomAnimalAnimatorBridge);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.AudioReplacement, GameBridgeRetainedCallbackDemand.AudioReplacement);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.ActionSpeed, GameBridgeRetainedCallbackDemand.ActionSpeed);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.ActionCompletion, GameBridgeRetainedCallbackDemand.ActionCompletion);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.AgentStateToolExitShared, GameBridgeRetainedCallbackDemand.AgentStateToolExitShared);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.AgentStateInteractExitShared, GameBridgeRetainedCallbackDemand.AgentStateInteractExitShared);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.AgentStateBaseExitShared, GameBridgeRetainedCallbackDemand.AgentStateBaseExitShared);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.ToolColliderShared, GameBridgeRetainedCallbackDemand.ToolColliderShared);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.ActionSpeedToolStages, GameBridgeRetainedCallbackDemand.ActionSpeedToolStages);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.ActionSpeedInteractionStages, GameBridgeRetainedCallbackDemand.ActionSpeedInteractionStages);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.AudioReplacementAnimalVoiceContext, GameBridgeRetainedCallbackDemand.AudioReplacementAnimalVoiceContext);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.WorkshopAuthoring, GameBridgeRetainedCallbackDemand.WorkshopAuthoring);
            MapRetainedCallbackDemand(GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset, GameBridgeRetainedCallbackDemand.ItemDisplayNameEnvironmentReset);

            runtime.DemandCoordinator.DemandStateChangedSynchronously += OnRuntimeDemandStateChangedSynchronously;
            runtime.DemandCoordinator.DemandTransitioned += OnRuntimeDemandTransitioned;
            RefreshRetainedCallbackDemandMaskSynchronously();
            demandRoutingInitialized = true;

            SetMandatoryFrameworkDemand(GameBridgeDemandRoutes.CoreLifecycle, "core-lifecycle", true, "GameBridge composition root");
            SetMandatoryFrameworkDemand(GameBridgeDemandRoutes.CoreUiContext, "ui-context", true, "GameBridge composition root");
            SetMandatoryFrameworkDemand(GameBridgeDemandRoutes.ContentRefreshDrain, "content-generation-drain", true, "GameBridge composition root");
            SetMandatoryFrameworkDemand(GameBridgeDemandRoutes.NativeUiLayoutDiagnostics, "native-title-layout", true, "GameBridge composition root");
            if (runtime.IsAuthorSessionActive)
                GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.WorkshopAuthoring, GameBridgeDemandRoutes.AuthorOwner, RuntimeDemandSourceType.CapabilitySession, RuntimeDemandLifetime.Process, "author-session", true, "authorized author session active");
        }

        private void ConfigureDemandRoute(
            string capabilityId,
            TimeSpan cadence,
            Action? updater,
            bool requestsHookInstall,
            Func<bool>? isPatchInstalled,
            Func<bool>? isAnyPatchInstalled = null)
        {
            if (!demandRoutes.TryGetValue(capabilityId, out GameBridgeDemandRoute route))
            {
                route = new GameBridgeDemandRoute(capabilityId, RuntimeCapabilityOutcome.Deferred, string.Empty);
                demandRoutes[capabilityId] = route;
            }

            route.Cadence = cadence;
            route.Updater = updater;
            route.RequestsHookInstall = requestsHookInstall;
            route.IsPatchInstalled = isPatchInstalled;
            route.IsAnyPatchInstalled = isAnyPatchInstalled ?? isPatchInstalled;
        }

        private void MapRetainedCallbackDemand(string capabilityId, GameBridgeRetainedCallbackDemand callbackDemand)
        {
            if (!demandRoutes.TryGetValue(capabilityId, out GameBridgeDemandRoute route))
                throw new InvalidOperationException("Unknown retained callback demand route " + (capabilityId ?? string.Empty) + ".");
            route.RetainedCallbackDemand = callbackDemand;
        }

        private void SetMandatoryFrameworkDemand(string capabilityId, string demandKey, bool active, string reason)
        {
            GameBridgeDemandRoutes.SetOwnerDemand(
                runtime,
                capabilityId,
                GameBridgeDemandRoutes.FrameworkOwner,
                RuntimeDemandSourceType.MandatoryFrameworkRepair,
                RuntimeDemandLifetime.Process,
                demandKey,
                active,
                reason);
        }

        private void ReconcileWorkshopAuthoringDemand(bool active, string reason)
        {
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.WorkshopAuthoring, GameBridgeDemandRoutes.AuthorOwner, RuntimeDemandSourceType.CapabilitySession, RuntimeDemandLifetime.Process, "author-session", active, reason);
        }

        private void OnRuntimeDemandTransitioned(RuntimeDemandTransition transition)
        {
            if (transition == null || !demandRoutes.TryGetValue(transition.CapabilityId, out GameBridgeDemandRoute route))
                return;

            lock (demandRouteGate)
                pendingDemandRouteIds.Add(transition.CapabilityId);
        }

        private void OnRuntimeDemandStateChangedSynchronously(string capabilityId)
        {
            if (!demandRoutes.TryGetValue(capabilityId, out GameBridgeDemandRoute route))
                return;

            int nextMask = Volatile.Read(ref retainedCallbackDemandMask);
            SetRetainedCallbackDemandBit(
                ref nextMask,
                route.RetainedCallbackDemand,
                runtime.DemandCoordinator.HasDemand(capabilityId));

            if (IsProductOwnedClosureRoute(capabilityId))
                ReconcileProductOwnedClosureBits(ref nextMask);

            Volatile.Write(ref retainedCallbackDemandMask, nextMask);
        }

        private void RefreshRetainedCallbackDemandMaskSynchronously()
        {
            int nextMask = 0;
            foreach (GameBridgeDemandRoute route in demandRoutes.Values)
            {
                if (runtime.DemandCoordinator.HasDemand(route.CapabilityId))
                    nextMask |= (int)route.RetainedCallbackDemand;
            }
            ReconcileProductOwnedClosureBits(ref nextMask);
            Volatile.Write(ref retainedCallbackDemandMask, nextMask);
        }

        private void ReconcileProductOwnedClosureBits(ref int mask)
        {
            RuntimeDemandCoordinator coordinator = runtime.DemandCoordinator;
            SetRetainedCallbackDemandBit(
                ref mask,
                GameBridgeRetainedCallbackDemand.ActionCompletionToolColliderOwned,
                coordinator.HasCoOwnedDemand(GameBridgeDemandRoutes.ActionCompletion, GameBridgeDemandRoutes.ToolColliderShared, "action-completion-tool"));
            SetRetainedCallbackDemandBit(
                ref mask,
                GameBridgeRetainedCallbackDemand.ActionCompletionInteractExitOwned,
                coordinator.HasCoOwnedDemand(GameBridgeDemandRoutes.ActionCompletion, GameBridgeDemandRoutes.AgentStateInteractExitShared, "action-completion-interact-exit"));
            SetRetainedCallbackDemandBit(
                ref mask,
                GameBridgeRetainedCallbackDemand.ActionSpeedToolStagesOwned,
                coordinator.HasCoOwnedDemand(GameBridgeDemandRoutes.ActionSpeed, GameBridgeDemandRoutes.ActionSpeedToolStages, "tool-stages"));
            SetRetainedCallbackDemandBit(
                ref mask,
                GameBridgeRetainedCallbackDemand.ActionSpeedToolExitOwned,
                coordinator.HasCoOwnedDemand(GameBridgeDemandRoutes.ActionSpeed, GameBridgeDemandRoutes.AgentStateToolExitShared, "action-speed-tool-exit"));
            SetRetainedCallbackDemandBit(
                ref mask,
                GameBridgeRetainedCallbackDemand.ActionSpeedInteractionStagesOwned,
                coordinator.HasCoOwnedDemand(GameBridgeDemandRoutes.ActionSpeed, GameBridgeDemandRoutes.ActionSpeedInteractionStages, "interaction-stages"));
            SetRetainedCallbackDemandBit(
                ref mask,
                GameBridgeRetainedCallbackDemand.ActionSpeedInteractExitOwned,
                coordinator.HasCoOwnedDemand(GameBridgeDemandRoutes.ActionSpeed, GameBridgeDemandRoutes.AgentStateInteractExitShared, "action-speed-interact-exit"));
            SetRetainedCallbackDemandBit(
                ref mask,
                GameBridgeRetainedCallbackDemand.ActionSpeedBaseExitOwned,
                coordinator.HasCoOwnedDemand(GameBridgeDemandRoutes.ActionSpeed, GameBridgeDemandRoutes.AgentStateBaseExitShared, "action-speed-base-exit"));
        }

        private static void SetRetainedCallbackDemandBit(
            ref int mask,
            GameBridgeRetainedCallbackDemand demand,
            bool active)
        {
            int bit = (int)demand;
            if (active)
                mask |= bit;
            else
                mask &= ~bit;
        }

        private static bool IsProductOwnedClosureRoute(string capabilityId)
        {
            return capabilityId.Equals(GameBridgeDemandRoutes.ActionCompletion, StringComparison.OrdinalIgnoreCase) ||
                capabilityId.Equals(GameBridgeDemandRoutes.ActionSpeed, StringComparison.OrdinalIgnoreCase) ||
                capabilityId.Equals(GameBridgeDemandRoutes.ToolColliderShared, StringComparison.OrdinalIgnoreCase) ||
                capabilityId.Equals(GameBridgeDemandRoutes.AgentStateToolExitShared, StringComparison.OrdinalIgnoreCase) ||
                capabilityId.Equals(GameBridgeDemandRoutes.AgentStateInteractExitShared, StringComparison.OrdinalIgnoreCase) ||
                capabilityId.Equals(GameBridgeDemandRoutes.AgentStateBaseExitShared, StringComparison.OrdinalIgnoreCase) ||
                capabilityId.Equals(GameBridgeDemandRoutes.ActionSpeedToolStages, StringComparison.OrdinalIgnoreCase) ||
                capabilityId.Equals(GameBridgeDemandRoutes.ActionSpeedInteractionStages, StringComparison.OrdinalIgnoreCase);
        }

        private void CommitPendingDemandRoutesAtFrameBoundary()
        {
            string[] pending;
            lock (demandRouteGate)
            {
                if (pendingDemandRouteIds.Count == 0)
                    return;
                pending = pendingDemandRouteIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
                pendingDemandRouteIds.Clear();
            }

            bool updaterMembershipChanged = false;
            var requestedPhysicalHookRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string capabilityId in pending)
            {
                if (!demandRoutes.TryGetValue(capabilityId, out GameBridgeDemandRoute route))
                    continue;

                bool demanded = runtime.DemandCoordinator.HasDemand(capabilityId);
                bool updaterDesired = demanded && route.Updater != null;
                if (route.UpdaterActive != updaterDesired)
                {
                    route.UpdaterActive = updaterDesired;
                    route.LastDispatchAtUtc = DateTimeOffset.MinValue;
                    updaterMembershipChanged = true;
                }

                route.DemandActive = demanded;
                if (demanded)
                {
                    if (route.Outcome == RuntimeCapabilityOutcome.RestartRequired)
                    {
                        runtime.DemandCoordinator.SetRouteState(
                            capabilityId,
                            RuntimeCapabilityLifecycleState.RestartRequired,
                            RuntimeCapabilityPatchState.NotInstalled,
                            RuntimeCapabilityRestartPolicy.RestartRequired,
                            updaterActive: false,
                            "named same-process transition cannot safely roll back or rebind");
                        continue;
                    }
                    bool installed = route.IsPatchInstalled?.Invoke() == true;
                    runtime.DemandCoordinator.SetRouteState(
                        capabilityId,
                        installed || !route.RequestsHookInstall ? RuntimeCapabilityLifecycleState.Active : RuntimeCapabilityLifecycleState.Activating,
                        installed ? RuntimeCapabilityPatchState.Installed : RuntimeCapabilityPatchState.NotInstalled,
                        RuntimeCapabilityRestartPolicy.None,
                        updaterDesired,
                        "frame-boundary demand activation");
                    string physicalHookRouteId = string.IsNullOrWhiteSpace(route.HookRouteId) ? route.CapabilityId : route.HookRouteId;
                    if (route.RequestsHookInstall && !installed && requestedPhysicalHookRoutes.Add(physicalHookRouteId))
                    {
                        if (route.Outcome != RuntimeCapabilityOutcome.Mandatory)
                            optionalHookInstallRequestCount++;
                        RequestHookInstall("DemandActivated:" + capabilityId);
                    }
                }
                else
                {
                    bool installed = route.IsAnyPatchInstalled?.Invoke() == true;
                    runtime.DemandCoordinator.SetRouteState(
                        capabilityId,
                        installed ? RuntimeCapabilityLifecycleState.ProcessPinnedDormant : RuntimeCapabilityLifecycleState.Stopped,
                        installed ? RuntimeCapabilityPatchState.ProcessPinned : RuntimeCapabilityPatchState.Removed,
                        RuntimeCapabilityRestartPolicy.None,
                        updaterActive: false,
                        installed ? "logical demand released; independent unpatch is not proven" : "logical demand released before physical patch installation");
                }
            }

            if (updaterMembershipChanged)
            {
                activeDemandUpdaterSnapshot = demandRoutes.Values
                    .Where(route => route.UpdaterActive && route.Updater != null)
                    .OrderBy(route => route.CapabilityId, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                activeDemandUpdaterSnapshotRebuildCount++;
            }

            StopHookRetrySources("DemandTransitionQuiescence");
        }

        private void RefreshDemandRoutePatchStates(string reason)
        {
            routePatchStateRefreshPassCount++;
            foreach (GameBridgeDemandRoute route in demandRoutes.Values)
            {
                if (!route.DemandActive || !route.RequestsHookInstall)
                    continue;

                bool installed = route.IsPatchInstalled?.Invoke() == true;
                runtime.DemandCoordinator.SetRouteState(
                    route.CapabilityId,
                    installed ? RuntimeCapabilityLifecycleState.Active : RuntimeCapabilityLifecycleState.Activating,
                    installed ? RuntimeCapabilityPatchState.Installed : RuntimeCapabilityPatchState.NotInstalled,
                    RuntimeCapabilityRestartPolicy.None,
                    route.UpdaterActive,
                    (reason ?? string.Empty) + (installed ? " hook route installed" : " hook target remains pending"));
            }
        }

        private void DispatchActiveDemandUpdaters()
        {
            GameBridgeDemandRoute[] snapshot = activeDemandUpdaterSnapshot;
            DateTimeOffset now = DateTimeOffset.UtcNow;
            for (int i = 0; i < snapshot.Length; i++)
            {
                GameBridgeDemandRoute route = snapshot[i];
                if (route.Cadence != TimeSpan.Zero &&
                    route.Cadence != Timeout.InfiniteTimeSpan &&
                    route.LastDispatchAtUtc != DateTimeOffset.MinValue &&
                    now - route.LastDispatchAtUtc < route.Cadence)
                    continue;

                route.LastDispatchAtUtc = now;
                try
                {
                    route.Updater?.Invoke();
                    route.DispatchCount++;
                    demandUpdaterDispatchCount++;
                    if (route.Outcome != RuntimeCapabilityOutcome.Mandatory)
                        optionalDemandUpdaterDispatchCount++;
                }
                catch (Exception ex)
                {
                    route.FailureCount++;
                    demandUpdaterFailureCount++;
                    runtime.Diagnostics.RecordError(
                        "DTMAPI.GameBridge.Demand",
                        "Demand-routed updater failed capability=" + route.CapabilityId + ".",
                        ex.ToString());
                }
            }
        }

        private bool HasGameBridgeDemand(string capabilityId) => runtime.DemandCoordinator.HasDemand(capabilityId);

        internal bool HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand demand)
        {
            int required = (int)demand;
            return required != 0 && (Volatile.Read(ref retainedCallbackDemandMask) & required) == required;
        }

        private void GetDemandedHookRouteCounts(out int ready, out int total)
        {
            var physicalRoutes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (GameBridgeDemandRoute route in demandRoutes.Values)
            {
                if (!route.RequestsHookInstall || !runtime.DemandCoordinator.HasDemand(route.CapabilityId))
                    continue;
                string physicalHookRouteId = string.IsNullOrWhiteSpace(route.HookRouteId) ? route.CapabilityId : route.HookRouteId;
                bool installed = route.IsPatchInstalled?.Invoke() == true;
                physicalRoutes[physicalHookRouteId] = physicalRoutes.TryGetValue(physicalHookRouteId, out bool alreadyInstalled)
                    ? alreadyInstalled && installed
                    : installed;
            }
            total = physicalRoutes.Count;
            ready = physicalRoutes.Values.Count(installed => installed);
        }

        private void GetCatalogPhysicalHookRouteCounts(out int ready, out int total)
        {
            var physicalRoutes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (GameBridgeDemandRoute route in demandRoutes.Values)
            {
                if (!route.RequestsHookInstall)
                    continue;
                string physicalHookRouteId = string.IsNullOrWhiteSpace(route.HookRouteId) ? route.CapabilityId : route.HookRouteId;
                bool installed = route.IsPatchInstalled?.Invoke() == true;
                physicalRoutes[physicalHookRouteId] = physicalRoutes.TryGetValue(physicalHookRouteId, out bool alreadyInstalled)
                    ? alreadyInstalled && installed
                    : installed;
            }
            total = physicalRoutes.Count;
            ready = physicalRoutes.Values.Count(installed => installed);
        }

        private void ReleaseGameBridgeDemandRouting(string reason)
        {
            if (!demandRoutingInitialized)
                return;
            runtime.DemandCoordinator.DemandTransitioned -= OnRuntimeDemandTransitioned;
            foreach (string capabilityId in demandRoutes.Keys)
                runtime.DemandCoordinator.RemoveCapabilityDemands(capabilityId, "GameBridge shutdown " + (reason ?? string.Empty));
            foreach (GameBridgeDemandRoute route in demandRoutes.Values)
            {
                bool installed = route.IsAnyPatchInstalled?.Invoke() == true;
                runtime.DemandCoordinator.SetRouteState(
                    route.CapabilityId,
                    installed ? RuntimeCapabilityLifecycleState.ProcessPinnedDormant : RuntimeCapabilityLifecycleState.Stopped,
                    installed ? RuntimeCapabilityPatchState.ProcessPinned : RuntimeCapabilityPatchState.Removed,
                    RuntimeCapabilityRestartPolicy.None,
                    updaterActive: false,
                    "GameBridge shutdown " + (reason ?? string.Empty));
                route.UpdaterActive = false;
                route.DemandActive = false;
            }
            activeDemandUpdaterSnapshot = Array.Empty<GameBridgeDemandRoute>();
            runtime.DemandCoordinator.DemandStateChangedSynchronously -= OnRuntimeDemandStateChangedSynchronously;
            Volatile.Write(ref retainedCallbackDemandMask, 0);
            lock (demandRouteGate)
                pendingDemandRouteIds.Clear();
            demandRoutingInitialized = false;
        }

        private void UpdateContentRefreshGenerationDrain()
        {
            // G1/G2 own generation dirtiness. IsDirty is a zero-allocation gate;
            // no optional feature updater is invoked in the steady no-demand case.
            if (runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.CustomAnimals))
                customAnimalAnimatorBridgeFeature?.Update();

            if (runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.AudioReplacement))
            {
                audioReplacementFeature?.Update();
                SetAudioReplacementPendingDemand(audioReplacementFeature?.Service.HasPendingRuntimeWork == true, "audio content generation consumed");
            }
        }

        private void UpdateAudioReplacementPendingWork()
        {
            AudioReplacementService? service = audioReplacementFeature?.Service;
            if (service == null)
            {
                SetAudioReplacementPendingDemand(false, "audio service unavailable");
                return;
            }

            service.Update();
            SetAudioReplacementPendingDemand(service.HasPendingRuntimeWork, "audio pending updater reconciliation");
        }

        private void SetAudioReplacementPendingDemand(bool active, string reason)
        {
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.AudioReplacementPending, GameBridgeDemandRoutes.OperationOwner, RuntimeDemandSourceType.CapabilityOperation, RuntimeDemandLifetime.Operation, "pending-load-or-context", active, reason);
        }

        private AuthorSessionOperationResult HandleDemandedAuthorSessionRequest(AuthorSessionRequest request)
        {
            AuthorSessionOperationResult result = HandleAuthorSessionRequest(request);
            if (result.Status.Equals("restart-required", StringComparison.Ordinal))
                RecordAuthorSessionRestartRequired(request.UniqueId, result.Code, result.Message);
            return result;
        }

        private void RecordAuthorSessionRestartRequired(string ownerId, string code, string reason)
        {
            ownerId = string.IsNullOrWhiteSpace(ownerId) ? GameBridgeDemandRoutes.AuthorOwner : ownerId;
            code = string.IsNullOrWhiteSpace(code) ? "unnamed-transition" : code;
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.AuthorSessionReload, ownerId, RuntimeDemandSourceType.CapabilityOperation, RuntimeDemandLifetime.Process, code, true, reason);
            runtime.DemandCoordinator.SetRouteState(
                GameBridgeDemandRoutes.AuthorSessionReload,
                RuntimeCapabilityLifecycleState.RestartRequired,
                RuntimeCapabilityPatchState.NotInstalled,
                RuntimeCapabilityRestartPolicy.RestartRequired,
                updaterActive: false,
                code + ": " + (reason ?? string.Empty));
        }

        private bool IsNativeUiLayoutHookRouteInstalled()
        {
            NativeUiLayoutDiagnosticsFeature? feature = nativeUiLayoutDiagnosticsFeature;
            return feature != null &&
                feature.HomePageRenderTextMenuPatched &&
                feature.MainMenuPanelStartShowPatched &&
                feature.MenuUiSetCapacityPatched;
        }

        private bool IsAnyNativeUiLayoutHookInstalled()
        {
            NativeUiLayoutDiagnosticsFeature? feature = nativeUiLayoutDiagnosticsFeature;
            return feature != null && (feature.HomePageRenderTextMenuPatched || feature.MainMenuPanelStartShowPatched || feature.MenuUiSetCapacityPatched);
        }

        private bool IsAnyAnimalViewerHookInstalled() => animalViewerFeature != null &&
            (animalViewerFeature.HookBridge.FullInfoDataPatched || animalViewerFeature.HookBridge.ViewerShowPrefixPatched || animalViewerFeature.HookBridge.ViewerShowPatched || animalViewerFeature.HookBridge.PanelUnregisterPatched);

        private bool IsCoreLifecycleHookRouteInstalled() => CoreHookTargetsReady;

        private bool IsAnyCoreLifecycleHookInstalled() =>
            IsSaveLoadedHookReady ||
            loadRequestedPatched ||
            loadReturnedPatched ||
            nativeGameFramePatched ||
            saveSavingPatched ||
            saveSavedPatched ||
            returnHomeRequestedPatched ||
            returnHomePatched ||
            workshopReloadPatched;

        private bool IsAnyFishRoeHookInstalled() => fishRoeTooltipFeature != null &&
            (fishRoeTooltipFeature.HookBridge.TitlePatched || fishRoeTooltipFeature.HookBridge.DescriptionPatched || fishRoeTooltipFeature.HookBridge.DetailPatched);

        private bool IsAnyActionCompletionDependencyInstalled() => toolColliderHitHooks?.PostfixPatched == true || agentStateLifecycleHooks?.InteractExitPatched == true;

        private bool IsAnyActionSpeedHookInstalled() => actionSpeedFeature != null &&
            (actionSpeedFeature.HookBridge.ToolEnterPatched || actionSpeedFeature.HookBridge.ToolExitPatched || actionSpeedFeature.HookBridge.InteractEnterPatched || actionSpeedFeature.HookBridge.InteractExitPatched || actionSpeedFeature.HookBridge.EatEnterPatched || actionSpeedFeature.HookBridge.UseItemContinuesPatched || actionSpeedFeature.HookBridge.InteractContinuesPatched || actionSpeedFeature.HookBridge.AnimalRendererInteractPatched || actionSpeedFeature.HookBridge.BaseExitPatched);

        private bool IsAnyActionSpeedInteractionStageHookInstalled() => actionSpeedFeature != null &&
            (actionSpeedFeature.HookBridge.InteractEnterPatched || actionSpeedFeature.HookBridge.EatEnterPatched || actionSpeedFeature.HookBridge.UseItemContinuesPatched || actionSpeedFeature.HookBridge.InteractContinuesPatched || actionSpeedFeature.HookBridge.AnimalRendererInteractPatched);

        private bool IsActionSpeedHookRouteInstalled()
        {
            ActionSpeedService? service = actionSpeedFeature?.Service;
            ActionSpeedHookBridge? hooks = actionSpeedFeature?.HookBridge;
            if (service == null || hooks == null || (!service.RequiresToolHooks && !service.RequiresInteractionHooks))
                return false;
            bool toolReady = !service.RequiresToolHooks || (hooks.ToolEnterPatched && hooks.ToolExitPatched);
            bool interactionReady = !service.RequiresInteractionHooks ||
                (hooks.InteractionStageHooksReady &&
                 (!service.RequiresInteractExit || hooks.InteractExitPatched) &&
                 (!service.RequiresBaseExit || hooks.BaseExitPatched));
            return toolReady && interactionReady;
        }

        private bool IsAudioReplacementAnimalVoiceContextInstalled() => audioReplacementFeature?.HookBridge.AnimalPlayAnimalSoundPrefixPatched == true &&
            audioReplacementFeature.HookBridge.AnimalPlayAnimalSoundPostfixPatched;

        private bool IsAnyAudioReplacementAnimalVoiceContextInstalled() => audioReplacementFeature != null &&
            (audioReplacementFeature.HookBridge.AnimalPlayAnimalSoundPrefixPatched || audioReplacementFeature.HookBridge.AnimalPlayAnimalSoundPostfixPatched);

        private bool IsAnyCustomAnimalHookInstalled() => customAnimalAnimatorBridgeFeature != null &&
            (customAnimalAnimatorBridgeFeature.HookBridge.AnimatorAssetTryLoadPatched || customAnimalAnimatorBridgeFeature.HookBridge.AnimalAIDefaultAnyStatePatched || customAnimalAnimatorBridgeFeature.HookBridge.AnimalOnRenderPatched || customAnimalAnimatorBridgeFeature.HookBridge.AnimalDebugSetAdultPatched || customAnimalAnimatorBridgeFeature.HookBridge.AnimalRendererOnRecyclePatched || customAnimalAnimatorBridgeFeature.HookBridge.SpriteOverrideTryGetModOverrideSpritePatched || customAnimalAnimatorBridgeFeature.HookBridge.AnimalSleepPatched || customAnimalAnimatorBridgeFeature.HookBridge.AnimalWakeUpPatched || customAnimalAnimatorBridgeFeature.HookBridge.AnimalCallToRoomPatched || customAnimalAnimatorBridgeFeature.HookBridge.AnimalRendererOnFellPrefixPatched || customAnimalAnimatorBridgeFeature.HookBridge.AnimalRendererOnFellPostfixPatched || customAnimalAnimatorBridgeFeature.HookBridge.AnimalRendererPlayAnimationPatched || customAnimalAnimatorBridgeFeature.HookBridge.AnimalRendererFixedUpdatePatched || customAnimalAnimatorBridgeFeature.HookBridge.AnimalAIMakeDecisionFreeTimePatched || customAnimalAnimatorBridgeFeature.HookBridge.AnimalControllerOnUpdatePatched);

        private bool IsWorkshopAuthoringHookRouteInstalled() => workshopLocalUploadDisplayPatched && workshopUploadPlanBusyFallbackPatched && workshopUploadPlanKnownIdFallbackPatched;

        private bool IsAnyWorkshopAuthoringHookInstalled() => workshopLocalUploadDisplayPatched || workshopUploadPlanBusyFallbackPatched || workshopUploadPlanKnownIdFallbackPatched;

        private bool IsActionCompletionDependencyClosureInstalled()
        {
            if (!HasGameBridgeDemand(GameBridgeDemandRoutes.ActionCompletion))
                return false;
            bool toolRequired = HasGameBridgeDemand(GameBridgeDemandRoutes.ToolColliderShared);
            bool lifecycleRequired = HasGameBridgeDemand(GameBridgeDemandRoutes.AgentStateInteractExitShared);
            return (!toolRequired || toolColliderHitHooks?.PostfixPatched == true) &&
                (!lifecycleRequired || agentStateLifecycleHooks?.InteractExitPatched == true);
        }

        private bool IsFishingCompatibilityHookRouteInstalled()
        {
            return !fishingCompatibilityHooksRequired || fishingCompatibilityHooksReady;
        }

        private bool IsAnyFishingCompatibilityHookInstalled() => fishingCompatibilityReadyEnterPatched || fishingCompatibilityCastEnterPatched || fishingCompatibilityWaitEnterPatched || fishingCompatibilityWaitPlayPatched || fishingCompatibilityMiniGameStartPatched || fishingCompatibilityMiniGameUpdatePatched || fishingCompatibilityMiniGameStopPatched || fishingCompatibilityPullEnterPatched || fishingCompatibilityPullExitPatched || fishingCompatibilityBaseExitPatched;

        internal Batch5NoDemandRuntimeSnapshot CaptureBatch5NoDemandRuntimeSnapshot()
        {
            RuntimeDemandSnapshot demand = runtime.DemandCoordinator.GetSnapshot();
            EventHandlerCleanupSnapshot events = runtime.Events.GetHandlerCleanupSnapshot();
            RuntimeCapabilityDemandSnapshot? qaDemand = demand.Capabilities.FirstOrDefault(item =>
                item.CapabilityId.Equals(GameBridgeDemandRoutes.QaHost, StringComparison.OrdinalIgnoreCase));
            string[] activeOptionalDemandIds = demand.Capabilities
                .Where(item => item.TotalDemand > 0 &&
                    !item.CapabilityId.Equals(GameBridgeDemandRoutes.QaHost, StringComparison.OrdinalIgnoreCase) &&
                    (!demandRoutes.TryGetValue(item.CapabilityId, out GameBridgeDemandRoute route) ||
                     route.Outcome != RuntimeCapabilityOutcome.Mandatory))
                .Select(item => item.CapabilityId)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string[] activeOptionalUpdaterIds = activeDemandUpdaterSnapshot
                .Where(route => route.Outcome != RuntimeCapabilityOutcome.Mandatory &&
                    !route.CapabilityId.Equals(GameBridgeDemandRoutes.QaHost, StringComparison.OrdinalIgnoreCase))
                .Select(route => route.CapabilityId)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string[] activeMandatoryUpdaterIds = activeDemandUpdaterSnapshot
                .Where(route => route.Outcome == RuntimeCapabilityOutcome.Mandatory)
                .Select(route => route.CapabilityId)
                .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Batch5NoDemandMandatoryUpdaterSnapshot[] mandatoryUpdaterDispatches = demandRoutes.Values
                .Where(route => route.Outcome == RuntimeCapabilityOutcome.Mandatory && route.Updater != null)
                .OrderBy(route => route.CapabilityId, StringComparer.OrdinalIgnoreCase)
                .Select(route => new Batch5NoDemandMandatoryUpdaterSnapshot
                {
                    CapabilityId = route.CapabilityId,
                    Active = route.UpdaterActive,
                    DispatchCount = route.DispatchCount
                })
                .ToArray();

            long customCallbackWork = customAnimalAnimatorBridgeFeature?.Service.RetainedCallbackWorkCountForTest ?? 0L;
            long audioCallbackWork = audioReplacementFeature?.Service.RetainedCallbackWorkCountForTest ?? 0L;
            return new Batch5NoDemandRuntimeSnapshot
            {
                RuntimeUpdateTicks = runtime.RuntimeUpdateTickCount,
                QaHostUpdateCount = qaHostUpdateCount,
                EventQueuePending = runtime.RuntimeEventQueueHasPending,
                EventQueueDiagnosticRevision = runtime.RuntimeEventQueueDiagnosticRevision,
                HookStatusQueuePending = runtime.RuntimeHookStatusQueueHasPending,
                HookStatusQueueDiagnosticRevision = runtime.RuntimeHookStatusQueueDiagnosticRevision,
                EventArgsCreated = events.EventArgsCreated,
                EventSnapshotRebuilds = events.SnapshotRebuilds,
                EventZeroListenerBypasses = events.ZeroListenerBypasses,
                ActiveUpdaterMembershipSnapshotRebuilds = activeDemandUpdaterSnapshotRebuildCount,
                OptionalPerFeatureProjectionBuilds =
                    (customAnimalAnimatorBridgeFeature?.Service.ContentProjectionBuildCountForTest ?? 0L) +
                    (audioReplacementFeature?.Service.ContentProjectionBuildCountForTest ?? 0L),
                OptionalNativeUpdaterInvocations = demandRoutes.Values
                    .Where(route => route.Outcome != RuntimeCapabilityOutcome.Mandatory &&
                        !route.CapabilityId.Equals(GameBridgeDemandRoutes.QaHost, StringComparison.OrdinalIgnoreCase))
                    .Sum(route => route.DispatchCount),
                QaObserverUpdaterInvocations = demandRoutes.TryGetValue(GameBridgeDemandRoutes.QaHost, out GameBridgeDemandRoute qaRoute) ? qaRoute.DispatchCount : 0L,
                MandatoryUpdaterInvocations = demandRoutes.Values.Where(route => route.Outcome == RuntimeCapabilityOutcome.Mandatory).Sum(route => route.DispatchCount),
                OptionalFeatureFileStatusCalls =
                    (customAnimalAnimatorBridgeFeature?.Service.OptionalFileStatusCallCount ?? 0L) +
                    (audioReplacementFeature?.Service.OptionalFileStatusCallCount ?? 0L) +
                    optionalAuthorFileStatusCallCount + optionalWorkshopFileStatusCallCount,
                OptionalDirectoryEnumerations =
                    optionalAuthorDirectoryEnumerationCount,
                OptionalReflectionObjectSearches = 0L,
                OptionalRetainedCallbackWork = customCallbackWork + audioCallbackWork,
                CustomAnimalsRetainedCallbackWork = customCallbackWork,
                AudioRetainedCallbackWork = audioCallbackWork,
                OptionalHookInstallRequests = optionalHookInstallRequestCount,
                CameraEnvironmentResets = environmentResetCount,
                CustomAnimalDefinitionCandidateBuilds = customAnimalAnimatorBridgeFeature?.Service.DefinitionCandidateBuildCountForTest ?? 0,
                ContentQueryCandidateBuilds = runtime.Content.CandidateBuildCountForTest,
                AudioPendingEntryVisits = audioReplacementFeature?.Service.PendingEntryVisitCountForTest ?? 0L,
                QaExplicitDemandActive = qaDemand != null &&
                    qaDemand.TotalDemand > 0 &&
                    qaDemand.BySource.TryGetValue(RuntimeDemandSourceType.ExplicitQa, out int explicitQaDemand) && explicitQaDemand > 0,
                QaUpdaterActive = activeDemandUpdaterSnapshot.Any(route => route.CapabilityId.Equals(GameBridgeDemandRoutes.QaHost, StringComparison.OrdinalIgnoreCase)),
                ActiveOptionalDemandIds = activeOptionalDemandIds,
                ActiveOptionalUpdaterIds = activeOptionalUpdaterIds,
                ActiveMandatoryUpdaterIds = activeMandatoryUpdaterIds,
                MandatoryUpdaterDispatches = mandatoryUpdaterDispatches
            };
        }

        internal string[] GetActiveDemandUpdaterIdsForTests() =>
            activeDemandUpdaterSnapshot.Select(route => route.CapabilityId).ToArray();

        private string FormatGameBridgeDemandRoutingSummary()
        {
            return "activeUpdaters=" + activeDemandUpdaterSnapshot.Length.ToString(CultureInfo.InvariantCulture) +
                "; dispatches=" + demandUpdaterDispatchCount.ToString(CultureInfo.InvariantCulture) +
                "; failures=" + demandUpdaterFailureCount.ToString(CultureInfo.InvariantCulture) +
                "; reentryBypasses=" + Interlocked.Read(ref runtimeAutomationReentryBypassCount).ToString(CultureInfo.InvariantCulture) +
                "; ids=" + string.Join("|", activeDemandUpdaterSnapshot.Select(route => route.CapabilityId).ToArray());
        }

        internal string FormatGameBridgeDemandRoutingSummaryForTests() => FormatGameBridgeDemandRoutingSummary();

        internal void CommitPendingDemandRoutesForTests() => CommitPendingDemandRoutesAtFrameBoundary();

        internal void RefreshDemandRoutePatchStatesForTests(string reason) => RefreshDemandRoutePatchStates(reason);

        internal void WarmAndRunNoOptionalDemandFramesForTests(int frameCount)
        {
            UpdateRuntimeAutomation();
            optionalDemandUpdaterDispatchCount = 0;
            optionalHookInstallRequestCount = 0;
            hookSchedulerIdleFrameFastPathCount = 0;
            hookInstallProcessedBatchCount = 0;
            hookReadinessPublishCount = 0;
            routePatchStateRefreshPassCount = 0;
            for (int i = 0; i < frameCount; i++)
                UpdateRuntimeAutomation();
        }

        internal long OptionalDemandUpdaterDispatchCountForTests => optionalDemandUpdaterDispatchCount;

        internal long OptionalHookInstallRequestCountForTests => optionalHookInstallRequestCount;

        internal long HookSchedulerIdleFrameFastPathCountForTests => hookSchedulerIdleFrameFastPathCount;

        internal long HookInstallProcessedBatchCountForTests => hookInstallProcessedBatchCount;

        internal long HookReadinessPublishCountForTests => hookReadinessPublishCount;

        internal long RoutePatchStateRefreshPassCountForTests => routePatchStateRefreshPassCount;

        internal long RuntimeAutomationReentryBypassCountForTests => Interlocked.Read(ref runtimeAutomationReentryBypassCount);

        internal void SetQaHostFrameUpdateForTests(Action? updater) => qaHostFrameUpdate = updater;

        internal void InvokeDemandUpdaterForTests(string capabilityId)
        {
            if (!demandRoutes.TryGetValue(capabilityId, out GameBridgeDemandRoute route) || route.Updater == null)
                throw new InvalidOperationException("Demand updater is unavailable for " + (capabilityId ?? string.Empty) + ".");
            route.Updater();
        }

        internal int RetainedCallbackDemandMaskForTests => Volatile.Read(ref retainedCallbackDemandMask);

        internal void OverrideDemandRoutePatchProbeForTests(string capabilityId, Func<bool> isInstalled)
        {
            if (!demandRoutes.TryGetValue(capabilityId, out GameBridgeDemandRoute route))
                throw new InvalidOperationException("Unknown demand route " + (capabilityId ?? string.Empty) + ".");
            route.IsPatchInstalled = isInstalled ?? throw new ArgumentNullException(nameof(isInstalled));
            route.IsAnyPatchInstalled = isInstalled;
        }

        internal void RecordAuthorSessionRestartRequiredForTests(string ownerId, string code) =>
            RecordAuthorSessionRestartRequired(ownerId, code, "unit-test named restart transition");

        private sealed class GameBridgeDemandRoute
        {
            public GameBridgeDemandRoute(string capabilityId, RuntimeCapabilityOutcome outcome, string hookRouteId)
            {
                CapabilityId = capabilityId ?? string.Empty;
                Outcome = outcome;
                HookRouteId = hookRouteId ?? string.Empty;
                Cadence = Timeout.InfiniteTimeSpan;
            }

            public string CapabilityId { get; }
            public RuntimeCapabilityOutcome Outcome { get; }
            public string HookRouteId { get; }
            public TimeSpan Cadence { get; set; }
            public Action? Updater { get; set; }
            public bool RequestsHookInstall { get; set; }
            public Func<bool>? IsPatchInstalled { get; set; }
            public Func<bool>? IsAnyPatchInstalled { get; set; }
            public bool DemandActive { get; set; }
            public bool UpdaterActive { get; set; }
            public GameBridgeRetainedCallbackDemand RetainedCallbackDemand { get; set; }
            public DateTimeOffset LastDispatchAtUtc { get; set; } = DateTimeOffset.MinValue;
            public long DispatchCount { get; set; }
            public long FailureCount { get; set; }
        }
    }
}
