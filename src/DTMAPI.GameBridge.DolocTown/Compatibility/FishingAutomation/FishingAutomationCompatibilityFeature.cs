#pragma warning disable CS0618 // Frozen IFishingAutomationApi compatibility implementation.
using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    /// <summary>
    /// Hosts only the frozen pre-0.6 fishing ABI. The first-party AutoFishing
    /// product owns its own state machine, native access and Harmony patches.
    /// </summary>
    internal sealed class FishingAutomationCompatibilityFeature : IGameBridgeFeature
    {
        private readonly DtmApiRuntime runtime;
        private readonly FishingAutomationCompatibilityAdapter api;
        private readonly Dictionary<string, FishingAutomationOptions> configuredOptions = new Dictionary<string, FishingAutomationOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FishingAutomationState> configuredStates = new Dictionary<string, FishingAutomationState>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> compatibilityWarningOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private LegacyFishingAutomationService? service;
        private FishingCompatibilityHookBridge? hookBridge;
        private HarmonyReflectionPatcher? patcher;
        private string lastActivationFailure = string.Empty;

        public FishingAutomationCompatibilityFeature(DtmApiRuntime runtime)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            api = new FishingAutomationCompatibilityAdapter(this);
        }

        public string Id => "FishingAutomationCompatibility";

        public GameBridgeFeatureContract Contract { get; } = new GameBridgeFeatureContract(
            "FishingAutomationCompatibility",
            requiresSave: true,
            allowsTitleScreen: false,
            requiresNativeScene: true,
            requiresUi: false,
            environmentResetSensitive: true,
            hasSaveLifetimeState: true,
            hasTitleLifetimeState: false,
            canAutoPauseAfterFailure: false);

        internal LegacyFishingAutomationService? Service => service;
        internal IFishingCompatibilityHookRuntime? CallbackRuntime => HooksReady && service?.HasActiveRuntimeConsumer == true ? service : null;
        internal bool HooksRequired => service?.HasEnabledOwner == true;
        internal bool HooksReady => hookBridge?.HooksReady == true;
        internal bool ReadyEnterPatched => hookBridge?.ReadyEnterPatched == true;
        internal bool CastEnterPatched => hookBridge?.CastEnterPatched == true;
        internal bool WaitEnterPatched => hookBridge?.WaitEnterPatched == true;
        internal bool WaitPlayPatched => hookBridge?.WaitPlayPatched == true;
        internal bool MiniGameStartPatched => hookBridge?.MiniGameStartPatched == true;
        internal bool MiniGameUpdatePatched => hookBridge?.MiniGameUpdatePatched == true;
        internal bool MiniGameStopPatched => hookBridge?.MiniGameStopPatched == true;
        internal bool PullEnterPatched => hookBridge?.PullEnterPatched == true;
        internal bool PullExitPatched => hookBridge?.PullExitPatched == true;
        internal bool BaseExitPatched => hookBridge?.BaseExitPatched == true;

        internal string GetCompatibilityLifecycleSummary()
        {
            return service?.GetFishingAutomationLifecycleSummary() ??
                "compatibilityStatus=inactive/no-consumer, fishingStates=0, fishingOptions=0, fishingMiniGameHandles=0, fishingInputHandles=0, fishingAnimators=0, fishingHookPhysics=0, fishingFailures=0, fishingPendingCast=false, fishingPendingCastAgeSeconds=0, fishingPendingCastStalls=0";
        }

        public void RegisterApis(IManifest manifest)
        {
            runtime.RegisterRuntimeApi<IFishingAutomationApi>(manifest, api, OwnerBoundFishingAutomationApi.For(api));
        }

        public void PublishHookStatuses()
        {
            if (hookBridge == null)
                PublishInactiveStatus("PublishHookStatuses");
            else
                hookBridge.PublishHookStatuses();
        }

        public void InstallHooks(HarmonyReflectionPatcher patcher)
        {
            BindPatcher(patcher);
            if (hookBridge == null)
            {
                PublishInactiveStatus("InstallHooks");
                return;
            }
            hookBridge.InstallHooks(this.patcher!);
        }

        internal void BindPatcher(HarmonyReflectionPatcher patcher)
        {
            if (patcher == null)
                throw new ArgumentNullException(nameof(patcher));
            this.patcher ??= new HarmonyReflectionPatcher(runtime, FishingCompatibilityHookBridge.HarmonyOwner);
        }

        public void Update()
        {
            if (service?.HasEnabledOwner == true)
                service.Update();
        }

        public void SaveLoaded(bool isNewGame)
        {
            service?.ResetFishingRuntimeState("SaveLoaded");
        }

        public void ReturnedToTitle()
        {
            service?.ResetFishingRuntimeState("ReturnedToTitle");
        }

        public void EnvironmentReset(string reason)
        {
            service?.NotifyFishingEnvironmentReset(reason);
        }

        internal int RemoveOwner(string ownerId, string reason)
        {
            ownerId ??= string.Empty;
            SetCompatibilityDemand(ownerId, active: false, "owner cleanup " + (reason ?? string.Empty));
            int removed = 0;
            if (configuredOptions.Remove(ownerId))
                removed++;
            if (configuredStates.Remove(ownerId))
                removed++;
            if (compatibilityWarningOwners.Remove(ownerId))
                removed++;
            removed += service?.RemoveOwner(ownerId, reason ?? string.Empty) ?? 0;
            if (service != null && !service.HasEnabledOwner)
                DeactivateRuntime("owner-cleanup:" + (reason ?? string.Empty));
            return removed;
        }

        internal int CountOwnerResources(string ownerId)
        {
            ownerId ??= string.Empty;
            return (configuredOptions.ContainsKey(ownerId) ? 1 : 0) +
                (configuredStates.ContainsKey(ownerId) ? 1 : 0) +
                (compatibilityWarningOwners.Contains(ownerId) ? 1 : 0) +
                (service?.CountOwnerResources(ownerId) ?? 0);
        }

        internal void ConfigureCompatibility(IManifest owner, FishingAutomationOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            WarnFrozenCompatibilityApi(owner.UniqueID);
            FishingAutomationOptions normalized = LegacyFishingAutomationService.NormalizeFishingAutomationOptions(options);
            configuredOptions[owner.UniqueID] = normalized;
            if (!configuredStates.ContainsKey(owner.UniqueID))
                configuredStates[owner.UniqueID] = new FishingAutomationState { Phase = "Configured" };
            service?.Configure(owner, normalized);
        }

        internal void SetCompatibilityEnabled(IManifest owner, bool enabled, string reason)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            WarnFrozenCompatibilityApi(owner.UniqueID);

            LegacyFishingAutomationService? runtimeService;
            try
            {
                runtimeService = enabled ? EnsureActivated("SetEnabled(true)") : service;
            }
            catch
            {
                if (enabled)
                    SetCompatibilityDemand(owner.UniqueID, active: false, "compatibility activation threw and was rolled back");
                DeactivateRuntime("activation-exception");
                throw;
            }
            if (runtimeService == null)
            {
                if (enabled)
                    SetCompatibilityDemand(owner.UniqueID, active: false, "compatibility activation rejected " + lastActivationFailure);
                FishingAutomationState configuredState = GetOrCreateConfiguredState(owner.UniqueID);
                configuredState.Enabled = false;
                configuredState.Phase = "Idle";
                configuredState.LastReason = FirstText(lastActivationFailure, reason ?? string.Empty);
                return;
            }

            if (!configuredOptions.TryGetValue(owner.UniqueID, out FishingAutomationOptions options))
            {
                options = LegacyFishingAutomationService.NormalizeFishingAutomationOptions(null);
                configuredOptions[owner.UniqueID] = options;
            }
            runtimeService.Configure(owner, options);
            runtimeService.SetEnabled(owner, enabled, reason ?? string.Empty);
            FishingAutomationState resultingState = CloneState(runtimeService.GetState(owner.UniqueID));
            configuredStates[owner.UniqueID] = resultingState;
            bool acceptedEnabledState = enabled && resultingState.Enabled;
            SetCompatibilityDemand(
                owner.UniqueID,
                acceptedEnabledState,
                acceptedEnabledState
                    ? "compatibility enable accepted " + (reason ?? string.Empty)
                    : enabled
                        ? "compatibility enable rejected " + resultingState.LastReason
                        : "compatibility disabled " + (reason ?? string.Empty));
            if (!runtimeService.HasEnabledOwner)
                DeactivateRuntime("compatibility-disabled:" + (reason ?? string.Empty));
        }

        internal FishingAutomationState GetCompatibilityState(string uniqueId)
        {
            if (service != null)
            {
                FishingAutomationState serviceState = service.GetState(uniqueId);
                if (serviceState.Enabled || !string.IsNullOrWhiteSpace(serviceState.Phase) || !string.IsNullOrWhiteSpace(serviceState.LastReason))
                    return CloneState(serviceState);
            }
            return configuredStates.TryGetValue(uniqueId ?? string.Empty, out FishingAutomationState state)
                ? CloneState(state)
                : new FishingAutomationState();
        }

        internal BridgeFeatureStatus GetCompatibilityStatus(string uniqueId)
        {
            string ownerId = uniqueId ?? string.Empty;
            if (!configuredOptions.ContainsKey(ownerId))
                return new BridgeFeatureStatus("inactive/no-consumer", "No fishing automation compatibility policy was registered for this mod.");
            if (service == null && configuredStates.TryGetValue(ownerId, out FishingAutomationState configuredState) &&
                (!string.IsNullOrWhiteSpace(configuredState.LastReason) || configuredState.Phase.Equals("Idle", StringComparison.OrdinalIgnoreCase)))
            {
                return new BridgeFeatureStatus("disabled/consumer-present", "Fishing automation compatibility policy is configured, but its legacy executor is inactive.");
            }
            return service == null
                ? new BridgeFeatureStatus("configured", "Fishing automation compatibility policy is stored without activating native hooks.")
                : service.GetStatus(ownerId);
        }

        private LegacyFishingAutomationService? EnsureActivated(string reason)
        {
            if (service == null)
            {
                service = new LegacyFishingAutomationService(runtime);
                hookBridge ??= new FishingCompatibilityHookBridge(runtime);
                hookBridge.AttachRuntime(service);
                runtime.SetHookStatus(
                    "Fishing.Compatibility",
                    "configured",
                    "FishingAutomationCompatibilityFeature." + FirstText(reason, "EnsureActivated"),
                    "The frozen IFishingAutomationApi executor was activated by a compatibility consumer.");
            }
            // A compatibility consumer can enable during the Runtime API phase,
            // before GameBridge.Initialize binds the Harmony patcher. Preserve
            // that owner state so the ordinary hook scheduler can install the
            // exact inventory later; callback publication remains closed until
            // HooksReady. Once a patcher is bound, an incomplete inventory is a
            // real activation failure and is rolled back below.
            if (patcher == null)
            {
                lastActivationFailure = string.Empty;
                service.SetFishingHooksInstalled(false);
                return service;
            }
            if (hookBridge != null && !hookBridge.HooksReady)
                hookBridge.InstallHooks(patcher);
            if (hookBridge?.HooksReady != true)
            {
                LegacyFishingAutomationService rejected = service!;
                lastActivationFailure = FirstText(
                    hookBridge?.LastInstallFailure ?? string.Empty,
                    "The frozen fishing compatibility patch inventory is unavailable.");
                rejected.ResetFishingRuntimeState("activation-rejected:" + lastActivationFailure);
                hookBridge?.DetachRuntime(rejected);
                rejected.SetFishingHooksInstalled(false);
                service = null;
                runtime.RuntimeMonitor.Log("Frozen fishing compatibility activation rejected: " + lastActivationFailure, LogLevel.Warn);
                return null;
            }
            lastActivationFailure = string.Empty;
            service.SetFishingHooksInstalled(hookBridge?.HooksReady == true);
            return service;
        }

        private void DeactivateRuntime(string reason)
        {
            LegacyFishingAutomationService? released = service;
            if (released == null)
                return;
            released.ResetFishingRuntimeState(reason);
            hookBridge?.DetachRuntime(released);
            released.SetFishingHooksInstalled(false);
            bool cleanupSucceeded = hookBridge?.UninstallHooks(patcher, reason) ?? true;
            if (!cleanupSucceeded)
            {
                lastActivationFailure = FirstText(
                    hookBridge?.LastInstallFailure ?? string.Empty,
                    "The frozen fishing compatibility Harmony owner could not be removed.");
                // Retain the service as a cleanup tombstone. A later owner-cleanup
                // pass can retry the exact-owner unpatch without allowing callbacks
                // or same-process activation to resume.
                throw new InvalidOperationException(lastActivationFailure);
            }
            try
            {
                released.RemoveAllOwners("runtime-deactivation:" + FirstText(reason, "unknown"));
                if (released.ConfiguredOwnerCount != 0 || released.OwnerStateCount != 0)
                    throw new InvalidOperationException("The process-resident frozen fishing executor retained owner policy or state after deactivation.");
            }
            catch (Exception ex)
            {
                lastActivationFailure = "The process-resident frozen fishing executor owner cleanup failed: " + ex.Message;
                // Keep the proxy as a retry tombstone. Callbacks and Hooks are
                // already detached, but the resident backend must not be hidden
                // until its owner dictionaries are actually empty.
                throw new InvalidOperationException(lastActivationFailure, ex);
            }
            service = null;
            lastActivationFailure = string.Empty;
        }

        private void SetCompatibilityDemand(string ownerId, bool active, string reason)
        {
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.FishingCompatibility, ownerId, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "compatibility-enabled", active, reason);
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.FishingCompatibilityUpdater, ownerId, RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "compatibility-enabled", active, reason);
        }

        private void PublishInactiveStatus(string reason)
        {
            runtime.SetHookStatus(
                "Fishing.Compatibility",
                "inactive/no-consumer",
                "FishingAutomationCompatibilityFeature." + FirstText(reason, "unknown"),
                "No frozen IFishingAutomationApi consumer is active; the compatibility executor and callback route remain dormant.");
        }

        private FishingAutomationState GetOrCreateConfiguredState(string ownerId)
        {
            ownerId ??= string.Empty;
            if (!configuredStates.TryGetValue(ownerId, out FishingAutomationState state))
            {
                state = new FishingAutomationState();
                configuredStates[ownerId] = state;
            }
            return state;
        }

        private static FishingAutomationState CloneState(FishingAutomationState state)
        {
            state ??= new FishingAutomationState();
            return new FishingAutomationState
            {
                Enabled = state.Enabled,
                Phase = state.Phase,
                LastReason = state.LastReason,
                NativeOwner = state.NativeOwner,
                LastNativeAction = state.LastNativeAction
            };
        }

        private void WarnFrozenCompatibilityApi(string ownerId)
        {
            ownerId ??= string.Empty;
            if (!compatibilityWarningOwners.Add(ownerId))
                return;
            runtime.RuntimeMonitor.Log(
                "Mod " + ownerId + " called deprecated IFishingAutomationApi. This 0.5.5 compatibility facade is frozen; migrate to the self-contained Yuuka.DTMAPI.AutoFishing product before the earliest 0.6.0 breaking cleanup.",
                LogLevel.Warn);
        }

        private static string FirstText(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
