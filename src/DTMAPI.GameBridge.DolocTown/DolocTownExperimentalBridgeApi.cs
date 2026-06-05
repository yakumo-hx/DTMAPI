using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using DTMAPI.Abstractions;

namespace DTMAPI.GameBridge.DolocTown
{
    internal sealed class DolocTownExperimentalBridgeApi : IActionCompletionApi, IFishingAutomationApi, IActionSpeedApi, IItemTooltipApi, IAnimalViewerApi, IInventoryDebugApi, IMailDeliveryApi, IWeatherDebugApi, ITeleportDebugApi, IInstantSaveDebugApi, ITimeDebugApi, IMovementDebugApi, IMotorVehicleApi, IMachineProductionApi, IEquipmentSlotsApi
    {
        private const string SecondMotorScopedTintHex = "#8CE6FF";
        private const double SecondMotorScopedTintR = 0.55;
        private const double SecondMotorScopedTintG = 0.90;
        private const double SecondMotorScopedTintB = 1.00;

        private readonly DTMAPI.Core.Runtime.DtmApiRuntime runtime;
        private readonly Dictionary<string, ActionCompletionOptions> actionOptions = new Dictionary<string, ActionCompletionOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FishingAutomationOptions> fishingOptions = new Dictionary<string, FishingAutomationOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FishingAutomationState> fishingStates = new Dictionary<string, FishingAutomationState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ActionSpeedOptions> actionSpeedOptions = new Dictionary<string, ActionSpeedOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, FishRoeTooltipOptions> fishRoeOptions = new Dictionary<string, FishRoeTooltipOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Func<string, FishRoeDisplayInfo?>> fishRoeLookups = new Dictionary<string, Func<string, FishRoeDisplayInfo?>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AnimalHusbandryProgressOptions> animalOptions = new Dictionary<string, AnimalHusbandryProgressOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<object, IReadOnlyList<AnimalProgressRenderRow>> animalProgressRowsByData = new Dictionary<object, IReadOnlyList<AnimalProgressRenderRow>>();
        private readonly List<object> activeAnimalProgressOverlayObjects = new List<object>();
        private readonly List<AnimalProgressRenderRow> activeAnimalProgressOverlayRows = new List<AnimalProgressRenderRow>();
        private readonly Dictionary<string, int> husbandryThresholdCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> itemTitleCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedActionApplications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedFishingPhases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedActionSpeedApplications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedFishRoeApplications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loggedAnimalApplications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, PendingOilResourceHit> pendingOilResourceHits = new Dictionary<string, PendingOilResourceHit>(StringComparer.Ordinal);
        private readonly Dictionary<object, DateTimeOffset> fishingMiniGameStartedAt = new Dictionary<object, DateTimeOffset>();
        private readonly Dictionary<string, SecondMotorRuntime> secondMotors = new Dictionary<string, SecondMotorRuntime>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SecondMotorRuntime> secondMotorsByKeyItemId = new Dictionary<string, SecondMotorRuntime>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<MachineDefinition>> machineDefinitions = new Dictionary<string, List<MachineDefinition>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MachineProductionState> machineStates = new Dictionary<string, MachineProductionState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MachineRuntimeEntry> machineRuntimeEntries = new Dictionary<string, MachineRuntimeEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Random machineRandom = new Random();
        private readonly Dictionary<string, EquipmentSlotsOptions> equipmentSlotOptions = new Dictionary<string, EquipmentSlotsOptions>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, EquipmentSlotsState> equipmentSlotStates = new Dictionary<string, EquipmentSlotsState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<EquipmentSlotRuntimeEntry>> equipmentSlotEntries = new Dictionary<string, List<EquipmentSlotRuntimeEntry>>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> loadedEquipmentSlotStorageOwners = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<object> activeEquipmentSlotUiObjects = new List<object>();
        private readonly HashSet<object> secondMotorControllers = new HashSet<object>();
        private readonly HashSet<object> secondMotorInteractables = new HashSet<object>();
        private readonly Dictionary<object, double> originalAnimatorSpeeds = new Dictionary<object, double>();
        private bool actionHooksInstalled;
        private bool fishingHooksInstalled;
        private bool actionSpeedToolHooksInstalled;
        private bool actionSpeedInteractionHooksInstalled;
        private bool fishRoeHooksInstalled;
        private bool animalViewerHookInstalled;
        private bool motorVehicleHooksInstalled;
        private bool animalViewerUiEvidenceRecorded;
        private bool animalPanelUiProbeLogged;
        private bool animalViewerUiDelayedScreenshotRecorded;
        private string? latestAnimalViewerEvidenceDir;
        private string latestAnimalProgressOverlaySummary = string.Empty;
        private double movementSpeedMultiplier = 1;
        private DateTimeOffset lastActionSpeedAutoFillAt = DateTimeOffset.MinValue;
        private DateTimeOffset lastFishingAutoCastAt = DateTimeOffset.MinValue;
        private DateTimeOffset lastFishingFeedbackAt = DateTimeOffset.MinValue;
        private DateTimeOffset lastAnimalProgressOverlayRefreshAt = DateTimeOffset.MinValue;
        private int actionSpeedAutoFillApplications;
        private int fishingAutoCastApplications;
        private object? originalAgentMotorController;
        private OriginalMotorSnapshot? originalMotorSnapshotBeforeSecondRide;
        private SecondMotorRuntime? activeSecondMotor;
        private GlobalMotorTuningSnapshot? activeSecondMotorTuningSnapshot;
        private bool restoringOriginalMotorSnapshot;
        private DateTimeOffset lastMachineProductionPollAt = DateTimeOffset.MinValue;
        private int lastMachineProductionTotalTus = -1;
        private bool machineRuntimeLoopInstalled;
        private bool equipmentSlotsRuntimeHooksInstalled;
        private bool equipmentSlotsUiHooksInstalled;
        private bool equipmentSlotsUiRendered;
        private bool equipmentSlotsUiEvidenceRecorded;
        private bool equipmentSlotsApplyingFunctions;
        private bool equipmentSlotsOrphanRecoveryChecked;
        private DateTimeOffset lastEquipmentSlotsUiRefreshAt = DateTimeOffset.MinValue;
        private string equipmentSlotsUiLastSummary = string.Empty;

        public event EventHandler<MotorVehicleEventArgs>? VehicleChanged;

        public DolocTownExperimentalBridgeApi(DTMAPI.Core.Runtime.DtmApiRuntime runtime)
        {
            this.runtime = runtime;
        }

        internal int OneActionApplicationCount { get; private set; }

        internal string LastOneActionApplicationSummary { get; private set; } = string.Empty;

        internal int FishingAutomationApplicationCount { get; private set; }

        internal string LastFishingAutomationApplicationSummary { get; private set; } = string.Empty;

        internal string LastFishingMiniGameCompleteSummary { get; private set; } = string.Empty;

        internal int ActionSpeedApplicationCount { get; private set; }

        internal string LastActionSpeedApplicationSummary { get; private set; } = string.Empty;

        internal int ActionSpeedContinuousUseApplicationCount { get; private set; }

        internal string LastActionSpeedContinuousUseSummary { get; private set; } = string.Empty;

        internal string LastActionSpeedAutoFillSummary { get; private set; } = string.Empty;

        internal int ActionSpeedAutoFillApplicationCount => actionSpeedAutoFillApplications;

        internal bool SuppressActionSpeedAutoFillForSmoke { get; set; }

        internal int FishingAutoCastApplicationCount => fishingAutoCastApplications;

        internal string LastFishingAutoCastAttemptSummary { get; private set; } = string.Empty;

        internal int FishingMiniGameCompleteApplicationCount { get; private set; }

        internal bool SuppressFishingAutoCastForSmoke { get; set; }

        internal bool ForceFishingNoWaterForSmoke { get; set; }

        internal bool ForceFishingNoRodForSmoke { get; set; }

        internal object? FishingPoolOverrideForSmoke { get; set; }

        internal bool ForceFishingFishForSmoke { get; set; }

        internal bool ForceOilDropForSmoke { get; set; }

        internal int OilMiningDropCount { get; private set; }

        internal string LastOilMiningDropSummary { get; private set; } = string.Empty;

        internal bool ForceMachineProductionDueForSmoke { get; set; }

        internal void ResetFishingFeedbackCooldownForSmoke()
        {
            lastFishingFeedbackAt = DateTimeOffset.MinValue;
        }

        public void PublishHookStatuses()
        {
            runtime.SetHookStatus("Actions.OneActionComplete", "pending", "DTMAPI.GameBridge.DolocTown API", "Resource-hit, wrong-tool, and fuel/feed evidence exists in ONEACTION-001/002; vegetation/dandelion is recorded as the native VegetationRenderer.OnFell exception path in ONEACTION-003; waiting for ToolCollider/interact hooks in this run.");
            runtime.SetHookStatus("Actions.OneActionFuelFeed", "pending", "DTMAPI.GameBridge.DolocTown API", "Fuel/feeder fill evidence exists in ONEACTION-002; waiting for AgentStateInteract.OnExit to become patchable in this run.");
            runtime.SetHookStatus("Fishing.Automation", "pending", "DTMAPI.GameBridge.DolocTown API", "F6 auto-cast and wait-phase InstantBite evidence exists in AUTOFISH-001; waiting for fishing hook install in this run.");
            runtime.SetHookStatus("ActionSpeed.ToolAnimation", "pending", "DTMAPI.GameBridge.DolocTown API", "Tool-animation evidence exists in ACTIONSPEED-001; waiting for AgentStateTool hook install in this run. ACTIONSPEED-002 now covers fuel/feed add, eat/drink animation, bottled-water right-click continuous drink, IWaterContainer and in-water bottle fill, no-key auto-fill, planting, plant-basin crop harvest, resin collection, and wild vegetation harvest.");
            runtime.SetHookStatus("Items.FishRoeTooltip", "pending", "DTMAPI.GameBridge.DolocTown API", "Fish roe tooltip evidence exists in FISHROE-001; waiting for item display hook install in this run.");
            runtime.SetHookStatus("Animals.ViewerRendering", "pending", "DTMAPI.GameBridge.DolocTown API", "Animal bell UI evidence exists in ANIMAL-001; waiting for animal viewer hooks in this run.");
            runtime.SetHookStatus("Debug.InventoryApi", "experimental", "DTMAPI.GameBridge.DolocTown API", "Uses DolocConfig.Tables.TbItem and native DolocAPI item creation/backpack placement paths.");
            runtime.SetHookStatus("Debug.WeatherApi", "experimental", "DTMAPI.GameBridge.DolocTown API", "Uses native WeatherSystem via ArchiveDataHandle.SetWeather and PatchWeather; current-period patching is experimental.");
            runtime.SetHookStatus("Debug.TeleportApi", "experimental", "DTMAPI.GameBridge.DolocTown API", "Uses a whitelist of native mark points and DolocAPI.DoTransport; arbitrary coordinates are not exposed.");
            runtime.SetHookStatus("Debug.TimeApi", "experimental", "DTMAPI.GameBridge.DolocTown API", "Uses ArchiveDataHandle.PassTimeNoControl plus DolocAPI.OnWakeUp to jump to the next weather period; no raw save edit.");
            runtime.SetHookStatus("Debug.MovementApi", "experimental", "DTMAPI.GameBridge.DolocTown API", "Uses native MotionAbility.SetMoveScaler on the player body; reset restores scale 0.");
            runtime.SetHookStatus("Vehicle.MotorApi", "pending", "DTMAPI.GameBridge.DolocTown API", "Waiting for MotorController, ItemMotorKey, MotorInteractable, AgentControllerState, UnlockMotor, SetMotorPosition, and EnterRoom hooks.");
            runtime.SetHookStatus("Machine.ProductionApi", "contract", "DTMAPI.GameBridge.DolocTown API", "0.2.4 experimental machine contract accepts JSON-backed machine definitions; production/fuel/electric runtime hooks still require third-save implementation evidence.");
            runtime.SetHookStatus("Player.EquipmentSlotsApi", "contract", "DTMAPI.GameBridge.DolocTown API", "0.2.4 experimental equipment-slot contract records extra attribute slots and safe recovery policy; GameBridge owns DTMAPI slot storage, native stat-function application, read-only player equipment strip rendering, and recovery without exposing raw game types.");
        }

        internal void SetFishRoeHooksInstalled(bool installed)
        {
            fishRoeHooksInstalled = installed;
        }

        internal void SetActionHooksInstalled(bool installed)
        {
            actionHooksInstalled = installed;
        }

        internal void SetFishingHooksInstalled(bool installed)
        {
            fishingHooksInstalled = installed;
        }

        internal void SetActionSpeedToolHooksInstalled(bool installed)
        {
            actionSpeedToolHooksInstalled = installed;
        }

        internal void SetActionSpeedInteractionHooksInstalled(bool installed)
        {
            actionSpeedInteractionHooksInstalled = installed;
        }

        internal void SetAnimalViewerHookInstalled(bool installed)
        {
            animalViewerHookInstalled = installed;
        }

        internal void SetMotorVehicleHooksInstalled(bool installed)
        {
            motorVehicleHooksInstalled = installed;
        }

        internal void SetEquipmentSlotsRuntimeHooksInstalled(bool installed)
        {
            equipmentSlotsRuntimeHooksInstalled = installed;
            foreach (KeyValuePair<string, EquipmentSlotsState> entry in equipmentSlotStates.ToArray())
            {
                EquipmentSlotsState state = entry.Value;
                state.RuntimeStatsHookInstalled = installed;
                state.Status = installed && state.IsConfigured ? "configured-experimental-ui-storage-stats-hook" : state.Status;
                equipmentSlotStates[entry.Key] = state;
            }
        }

        internal void SetEquipmentSlotsUiHooksInstalled(bool installed)
        {
            equipmentSlotsUiHooksInstalled = installed;
            foreach (KeyValuePair<string, EquipmentSlotsState> entry in equipmentSlotStates.ToArray())
            {
                EquipmentSlotsState state = entry.Value;
                state.RuntimeUiHookInstalled = installed || equipmentSlotsUiRendered;
                state.Status = (installed || equipmentSlotsUiRendered) && state.IsConfigured && equipmentSlotsRuntimeHooksInstalled
                    ? "configured-experimental-player-ui-storage-stats-hook"
                    : state.Status;
                equipmentSlotStates[entry.Key] = state;
            }
        }

        public void Configure(IManifest owner, ActionCompletionOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            actionOptions[owner.UniqueID] = options ?? new ActionCompletionOptions();
            runtime.RuntimeMonitor.Log("Action completion bridge configured by " + owner.UniqueID + ".");
        }

        BridgeFeatureStatus IActionCompletionApi.GetStatus(string uniqueId)
        {
            return actionOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(actionHooksInstalled ? "configured-verified-resource-hook" : "configured-pending-hook", actionHooksInstalled ? "Policy accepted; resource/tool-hit, wrong-tool guard, and fuel/feeder native consume/fill paths have third-save smoke evidence. Vegetation/dandelion is a native VegetationRenderer.OnFell exception path, not a DungeonResource one-action path." : "Policy accepted; action-completion evidence exists, but this run has not installed the gameplay hook yet.")
                : new BridgeFeatureStatus("not-configured", "No action completion policy was registered for this mod.");
        }

        public void Configure(IManifest owner, ActionSpeedOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            actionSpeedOptions[owner.UniqueID] = NormalizeActionSpeedOptions(options);
            runtime.RuntimeMonitor.Log("Action speed bridge configured by " + owner.UniqueID + ".");
        }

        BridgeFeatureStatus IActionSpeedApi.GetStatus(string uniqueId)
        {
            return actionSpeedOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(actionSpeedToolHooksInstalled ? "configured-verified-runtime-hooks" : "configured-pending-tool-hook", actionSpeedToolHooksInstalled ? "Tool animation speed is verified. Fuel/feed add, eat/drink animation, bottled-water right-click continuous drink, IWaterContainer and in-water bottle fill, no-key auto-fill, planting, plant-basin crop harvest, resin collection, and wild vegetation harvest have third-save smoke evidence" + (actionSpeedInteractionHooksInstalled ? " and interaction hooks are installed in this run." : "; waiting for interaction hooks in this run.") : "Policy accepted; waiting for AgentStateTool hooks in this run.")
                : new BridgeFeatureStatus("not-configured", "No action-speed policy was registered for this mod.");
        }

        internal bool TryGetConfiguredActionSpeedOwner(out string ownerId)
        {
            return TryFindActionSpeedToolPolicy(out ownerId, out _);
        }

        internal bool TryGetConfiguredActionSpeedInteractionOwner(out string ownerId)
        {
            ownerId = string.Empty;
            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (!candidate.Enabled)
                    continue;
                if ((candidate.BottleFillSpeedEnabled && candidate.BottleFillMultiplier > 1) ||
                    (candidate.EatDrinkSpeedEnabled && candidate.EatDrinkMultiplier > 1) ||
                    (candidate.MachineAddSpeedEnabled && candidate.MachineAddMultiplier > 1) ||
                    (candidate.HarvestSpeedEnabled && candidate.HarvestMultiplier > 1) ||
                    (candidate.PlantSpeedEnabled && candidate.PlantMultiplier > 1) ||
                    (candidate.AutoFillBottle && candidate.BottleFillMultiplier > 1) ||
                    (candidate.ContinuousDrinkWithRightClick && candidate.EatDrinkMultiplier > 1))
                {
                    ownerId = entry.Key;
                    return true;
                }
            }
            return false;
        }

        public void Configure(IManifest owner, FishingAutomationOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            fishingOptions[owner.UniqueID] = NormalizeFishingAutomationOptions(options);
            if (!fishingStates.ContainsKey(owner.UniqueID))
                fishingStates[owner.UniqueID] = new FishingAutomationState();
            runtime.RuntimeMonitor.Log("Fishing automation bridge configured by " + owner.UniqueID + ".");
        }

        public void SetEnabled(IManifest owner, bool enabled, string reason)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (!fishingStates.TryGetValue(owner.UniqueID, out FishingAutomationState state))
            {
                state = new FishingAutomationState();
                fishingStates[owner.UniqueID] = state;
            }
            state.Enabled = enabled;
            state.Phase = enabled ? "Starting" : "Idle";
            state.LastReason = reason ?? string.Empty;
            runtime.RuntimeMonitor.Log("Fishing automation state " + owner.UniqueID + " enabled=" + enabled + " reason=" + state.LastReason);
            if (ShouldShowFishingToggleFeedback(enabled, state.LastReason))
                ShowNativeSmallMessage(FormatFishingToggleFeedback(enabled, state.LastReason), error: false);
        }

        public FishingAutomationState GetState(string uniqueId)
        {
            return fishingStates.TryGetValue(uniqueId ?? string.Empty, out FishingAutomationState state)
                ? state
                : new FishingAutomationState();
        }

        private static bool ShouldShowFishingToggleFeedback(bool enabled, string reason)
        {
            reason = reason ?? string.Empty;
            return reason.StartsWith("hotkey ", StringComparison.OrdinalIgnoreCase) ||
                (!enabled && reason.StartsWith("manual-move ", StringComparison.OrdinalIgnoreCase));
        }

        private static string FormatFishingToggleFeedback(bool enabled, string reason)
        {
            reason = reason ?? string.Empty;
            if (!enabled && reason.StartsWith("manual-move ", StringComparison.OrdinalIgnoreCase))
                return "自动钓鱼：移动取消 / Auto fishing canceled by movement";
            return enabled ? "自动钓鱼：开启 / Auto fishing on" : "自动钓鱼：关闭 / Auto fishing off";
        }

        BridgeFeatureStatus IFishingAutomationApi.GetStatus(string uniqueId)
        {
            return fishingOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(fishingHooksInstalled ? "configured-experimental-hook" : "configured-pending-hook", fishingHooksInstalled ? "Policy accepted and fishing phase hooks are installed; wait-phase InstantBite has smoke evidence, while broader automation remains experimental." : "Policy accepted; fishing phase and input hooks are not yet installed.")
                : new BridgeFeatureStatus("not-configured", "No fishing automation policy was registered for this mod.");
        }

        internal bool TryGetEnabledFishingAutomationOwner(out string ownerId)
        {
            ownerId = string.Empty;
            foreach (KeyValuePair<string, FishingAutomationState> entry in fishingStates)
            {
                if (entry.Value?.Enabled == true && fishingOptions.ContainsKey(entry.Key))
                {
                    ownerId = entry.Key;
                    return true;
                }
            }
            return false;
        }

        public void ConfigureFishRoeProvider(IManifest owner, FishRoeTooltipOptions options, Func<string, FishRoeDisplayInfo?> lookup)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            fishRoeOptions[owner.UniqueID] = options ?? new FishRoeTooltipOptions();
            if (lookup != null)
                fishRoeLookups[owner.UniqueID] = lookup;
            runtime.RuntimeMonitor.Log("Fish roe tooltip bridge configured by " + owner.UniqueID + ".");
        }

        BridgeFeatureStatus IItemTooltipApi.GetStatus(string uniqueId)
        {
            return fishRoeOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(fishRoeHooksInstalled ? "configured-verified-tooltip-hook" : "configured-pending-hook", fishRoeHooksInstalled ? "Lookup provider accepted and item display hooks are installed; fish roe tooltip evidence is recorded, but the API remains experimental." : "Lookup provider accepted; item tooltip hooks are not yet installed.")
                : new BridgeFeatureStatus("not-configured", "No fish roe tooltip provider was registered for this mod.");
        }

        public void ConfigureSpecialProduceProgress(IManifest owner, AnimalHusbandryProgressOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            animalOptions[owner.UniqueID] = options ?? new AnimalHusbandryProgressOptions();
            runtime.RuntimeMonitor.Log("Animal viewer bridge configured by " + owner.UniqueID + ".");
        }

        BridgeFeatureStatus IAnimalViewerApi.GetStatus(string uniqueId)
        {
            return animalOptions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(animalViewerHookInstalled ? "configured-verified-animal-viewer-hook" : "configured-pending-hook", animalViewerHookInstalled ? "Display options accepted and animal viewer data hooks are installed; real animal bell UI evidence is recorded, but the API remains experimental." : "Display options accepted; animal viewer hooks are not yet installed.")
                : new BridgeFeatureStatus("not-configured", "No animal viewer display policy was registered for this mod.");
        }

        public MachineRegisterResult RegisterMachine(IManifest owner, MachineDefinition definition)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            MachineDefinition normalized = NormalizeMachineDefinition(owner, definition);
            var result = new MachineRegisterResult
            {
                OwnerId = owner.UniqueID,
                MachineId = normalized.MachineId,
                Definition = normalized
            };

            if (string.IsNullOrWhiteSpace(normalized.MachineId) || string.IsNullOrWhiteSpace(normalized.EquipmentId))
            {
                result.FailureReason = "missing-id";
                result.Message = "MachineId and EquipmentId are required.";
                return result;
            }

            if (!machineDefinitions.TryGetValue(owner.UniqueID, out List<MachineDefinition>? definitions))
            {
                definitions = new List<MachineDefinition>();
                machineDefinitions[owner.UniqueID] = definitions;
            }

            definitions.RemoveAll(candidate => candidate.MachineId.Equals(normalized.MachineId, StringComparison.OrdinalIgnoreCase));
            definitions.Add(normalized);

            string nativeTechTreeSummary = EnsureNativeMachineTechRoute(normalized);
            machineStates[owner.UniqueID] = new MachineProductionState
            {
                OwnerId = owner.UniqueID,
                IsConfigured = true,
                RegisteredMachineCount = definitions.Count,
                RuntimeHookInstalled = machineRuntimeLoopInstalled,
                Status = machineRuntimeLoopInstalled ? "configured-experimental-runtime-loop" : "configured-pending-runtime-hook",
                NativeTechTreeSummary = nativeTechTreeSummary,
                LastMessage = "Registered machine definition " + normalized.MachineId + " with " + normalized.OutputRules.Count + " output rules." +
                    (string.IsNullOrWhiteSpace(nativeTechTreeSummary) ? string.Empty : " nativeTech={" + nativeTechTreeSummary + "}")
            };
            ApplyMachineDefinitionState(machineStates[owner.UniqueID], normalized, 0, null);
            runtime.RuntimeMonitor.Log("Machine production definition registered owner=" + owner.UniqueID + " machine=" + normalized.MachineId + " equipment=" + normalized.EquipmentId + " outputs=" + normalized.OutputRules.Count + ".");
            runtime.SetHookStatus("Machine.ProductionApi", machineRuntimeLoopInstalled ? "configured-experimental-runtime-loop" : "configured-pending-runtime-hook", "DTMAPI.GameBridge.DolocTown API", "Registered " + normalized.MachineId + " for " + owner.UniqueID + "; DTMAPI runtime loop handles cycle/output state while native fuel/electric UI remains experimental.");

            result.Success = true;
            result.Message = machineStates[owner.UniqueID].LastMessage;
            return result;
        }

        private string EnsureNativeMachineTechRoute(MachineDefinition definition)
        {
            string nodeId = FirstText(definition.NativeTechNodeId, definition.EquipmentId.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase) ? definition.EquipmentId : string.Empty);
            if (string.IsNullOrWhiteSpace(nodeId))
                return string.Empty;

            string parentId = FirstText(definition.NativeTechNodeParentId, "alloy_material");
            string aboveTitle = FirstText(definition.NativeTechNodeAboveTitleContains, "指挥官", "Commander");
            string equipmentId = FirstText(definition.EquipmentId, definition.ItemId, nodeId);
            string recipeId = FirstText(definition.RecipeId, equipmentId);
            try
            {
                string infoSummary = EnsureNativeTechNodeInfo(nodeId, FirstText(definition.NativeTechNodeTitle, definition.DisplayName, nodeId), FirstText(definition.NativeTechNodeDescription, "Unlocks " + definition.DisplayName + "."));
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? assets = ReadStaticMember(dolocApi, "assets");
                object? techTrees = assets == null ? null : ReadMember(assets, "techTrees");
                object? allTreeGraphs = techTrees == null ? null : ReadMember(techTrees, "AllTreeGraphs");
                if (!(allTreeGraphs is IEnumerable graphs))
                    return "pending:missing-native-techtrees, node=" + nodeId + ", " + infoSummary;

                object? targetGraph = null;
                object? parentNode = null;
                object? aboveNode = null;
                foreach (object graph in graphs)
                {
                    string treeId = ReadStringMember(graph, "id");
                    if (!string.IsNullOrWhiteSpace(definition.NativeTechTreeId) &&
                        !treeId.Equals(definition.NativeTechTreeId, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (TryFindTechGraphNode(graph, nodeId, out object? existingNode))
                    {
                        string existingSummary = BuildNativeTechRouteSummary(graph, existingNode!, parentId, aboveTitle, "verified-existing", infoSummary, equipmentId, recipeId);
                        PublishNativeTechRouteStatus(existingSummary);
                        return existingSummary;
                    }

                    if (TryFindTechGraphNode(graph, parentId, out object? candidateParent))
                    {
                        targetGraph = graph;
                        parentNode = candidateParent;
                        aboveNode = FindTechGraphNodeByTitle(graph, aboveTitle) ?? FindTechGraphNodeByTitle(graph, "Commander");
                        break;
                    }
                }

                if (targetGraph == null || parentNode == null)
                    return "pending:parent-not-found, node=" + nodeId + ", parent=" + parentId + ", " + infoSummary;

                string injectSummary = InjectNativeTechGraphNode(targetGraph, parentNode, aboveNode, definition, nodeId, parentId, aboveTitle, equipmentId, recipeId, infoSummary);
                PublishNativeTechRouteStatus(injectSummary);
                return injectSummary;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Native machine tech route injection failed for " + nodeId + ".", ex.ToString());
                return "failed:" + ex.GetType().Name + ", node=" + nodeId + ", message=" + ex.Message;
            }
        }

        private string InjectNativeTechGraphNode(object graph, object parentNode, object? aboveNode, MachineDefinition definition, string nodeId, string parentId, string aboveTitle, string equipmentId, string recipeId, string infoSummary)
        {
            object? nodesObject = ReadMember(graph, "nodes");
            if (!(nodesObject is Array nodes) || nodes.Length == 0)
                return "failed:graph-nodes-missing, node=" + nodeId + ", parent=" + parentId + ", " + infoSummary;

            object? parentPos = ReadMember(parentNode, "pos");
            object? abovePos = aboveNode == null ? null : ReadMember(aboveNode, "pos");
            int parentX = ReadIntMember(parentPos ?? new object(), "x", 0);
            int parentY = ReadIntMember(parentPos ?? new object(), "y", 0);
            int desiredX = parentX + 1;
            int desiredY = abovePos == null ? parentY - 1 : ReadIntMember(abovePos, "y", parentY) - 1;
            if (!TryFindFreeTechNodePosition(graph, desiredX, desiredY, parentX, abovePos, out int finalX, out int finalY, out string positionSummary))
                return "failed:no-free-position, node=" + nodeId + ", parent=" + parentId + ", desired=" + desiredX + "," + desiredY + ", " + positionSummary + ", " + infoSummary;

            Type? techNodeProtoType = ResolveType("DolocTown.GameData.TechNodeProto, Assembly-CSharp");
            Type? techNodeCostType = ResolveType("DolocTown.GameData.TechNodeCost, Assembly-CSharp");
            Type? spriteType = ResolveType("UnityEngine.Sprite, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Sprite, UnityEngine");
            if (techNodeProtoType == null || techNodeCostType == null)
                return "failed:missing-tech-types, node=" + nodeId + ", protoType=" + (techNodeProtoType != null) + ", costType=" + (techNodeCostType != null) + ", " + infoSummary;

            object costs = CreateNativeTechNodeCosts(techNodeCostType, graph, parentNode, ResolveNativeMachineTechCost(definition));
            ConstructorInfo? protoCtor = techNodeProtoType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(ctor => ctor.GetParameters().Length >= 6);
            if (protoCtor == null)
                return "failed:missing-TechNodeProto-ctor, node=" + nodeId + ", " + infoSummary;

            object? icon = TryResolveNativeMachineIcon(equipmentId, spriteType);
            object proto = protoCtor.Invoke(new object?[]
            {
                nodeId,
                new[] { equipmentId },
                Array.Empty<string>(),
                new[] { recipeId },
                costs,
                icon,
                true
            });

            Type? nodeType = nodes.GetType().GetElementType();
            Type? vector2IntType = ResolveType("UnityEngine.Vector2Int, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Vector2Int, UnityEngine");
            if (nodeType == null || vector2IntType == null)
                return "failed:missing-node-vector-types, node=" + nodeId + ", nodeType=" + (nodeType != null) + ", vector2IntType=" + (vector2IntType != null) + ", " + infoSummary;

            object? position = CreateUnityVector2Int(finalX, finalY);
            if (position == null)
                return "failed:create-position, node=" + nodeId + ", " + infoSummary;

            object newNode = Activator.CreateInstance(nodeType, new object[] { nodeId, position, new[] { parentId }, proto })!;
            Array newNodes = Array.CreateInstance(nodeType, nodes.Length + 1);
            Array.Copy(nodes, newNodes, nodes.Length);
            newNodes.SetValue(newNode, nodes.Length);

            object? size = ReadMember(graph, "size");
            int sizeX = size == null ? 0 : ReadIntMember(size, "x", 0);
            int sizeY = size == null ? 0 : ReadIntMember(size, "y", 0);
            object? newSize = CreateUnityVector2Int(Math.Max(sizeX, finalX + 1), Math.Max(sizeY, finalY + 1));
            if (newSize == null)
                return "failed:create-size, node=" + nodeId + ", " + infoSummary;

            string treeId = ReadStringMember(graph, "id");
            object? defaultNode = ReadMember(graph, "defaultNode");
            string defaultNodeName = FirstText(defaultNode == null ? string.Empty : ReadStringMember(defaultNode, "id"), ReadStringMember(nodes.GetValue(0)!, "id"));
            ConstructorInfo? graphCtor = graph.GetType().GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(ctor => ctor.GetParameters().Length == 4);
            if (graphCtor == null)
                return "failed:missing-TreeGraph-ctor, node=" + nodeId + ", tree=" + treeId + ", " + infoSummary;

            object newGraph = graphCtor.Invoke(new object[] { treeId, newNodes, newSize, defaultNodeName });
            object? assets = ReadStaticMember(ResolveType("DolocAPI, Assembly-CSharp"), "assets");
            object? techTrees = assets == null ? null : ReadMember(assets, "techTrees");
            object? treeMap = techTrees == null ? null : ReadMember(techTrees, "trees");
            if (treeMap is IDictionary dictionary)
            {
                dictionary[treeId] = newGraph;
            }
            else
            {
                return "failed:tech-tree-map-unavailable, node=" + nodeId + ", tree=" + treeId + ", " + infoSummary;
            }

            bool rightOfParent = finalX > parentX;
            bool aboveCommander = abovePos != null && finalY < ReadIntMember(abovePos, "y", finalY + 1);
            int costCount = CountArrayItems(costs);
            string summary = "injected, node=" + nodeId +
                ", tree=" + treeId +
                ", parent=" + parentId +
                ", pos=" + finalX + "," + finalY +
                ", parentPos=" + parentX + "," + parentY +
                ", aboveTitle=" + aboveTitle +
                ", rightOfParent=" + rightOfParent +
                ", aboveCommander=" + aboveCommander +
                ", equipment=" + equipmentId +
                ", recipe=" + recipeId +
                ", costs=" + costCount +
                ", icon=" + (icon == null ? "null" : icon.GetType().Name) +
                ", " + positionSummary +
                ", " + infoSummary;
            runtime.RuntimeMonitor.Log("Native machine tech route injected " + summary + ".");
            return summary;
        }

        private string EnsureNativeTechNodeInfo(string nodeId, string title, string description)
        {
            Type? dolocConfig = ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            object? tables = ReadStaticMember(dolocConfig, "Tables");
            object? techNodeTable = tables == null ? null : ReadMember(tables, "TbTechNode");
            object? dataMap = techNodeTable == null ? null : ReadMember(techNodeTable, "DataMap");
            object? dataList = techNodeTable == null ? null : ReadMember(techNodeTable, "DataList");
            if (!(dataMap is IDictionary map))
                return "techInfo=pending:missing-TbTechNode";

            if (map.Contains(nodeId))
                return "techInfo=existing";

            Type? infoType = ResolveType("DolocTown.Config.TechTree.TechNodeInfo, Assembly-CSharp");
            ConstructorInfo? ctor = infoType?.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string), typeof(string), typeof(string), typeof(string) }, null);
            if (ctor == null)
                return "techInfo=failed:missing-TechNodeInfo-ctor";

            object info = ctor.Invoke(new object[] { nodeId, title, description, string.Empty });
            map[nodeId] = info;
            if (dataList is IList list)
                list.Add(info);
            runtime.RuntimeMonitor.Log("Native TechNodeInfo injected node=" + nodeId + " title=" + title + ".");
            return "techInfo=injected";
        }

        private static bool TryFindTechGraphNode(object graph, string nodeId, out object? node)
        {
            node = null;
            object? nodesObject = ReadMember(graph, "nodes");
            if (!(nodesObject is IEnumerable nodes))
                return false;

            foreach (object candidate in nodes)
            {
                if (candidate != null && ReadStringMember(candidate, "id").Equals(nodeId, StringComparison.OrdinalIgnoreCase))
                {
                    node = candidate;
                    return true;
                }
            }
            return false;
        }

        private static object? FindTechGraphNodeByTitle(object graph, string titlePart)
        {
            if (string.IsNullOrWhiteSpace(titlePart))
                return null;

            object? nodesObject = ReadMember(graph, "nodes");
            if (!(nodesObject is IEnumerable nodes))
                return null;

            foreach (object node in nodes)
            {
                object? data = ReadMember(node, "data");
                string title = data == null ? string.Empty : ReadStringMember(data, "Title");
                string id = ReadStringMember(node, "id");
                if (title.IndexOf(titlePart, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    id.IndexOf(titlePart, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return node;
                }
            }
            return null;
        }

        private static bool TryFindFreeTechNodePosition(object graph, int desiredX, int desiredY, int parentX, object? abovePos, out int finalX, out int finalY, out string summary)
        {
            finalX = desiredX;
            finalY = desiredY;
            if (!IsTechGraphPositionOccupied(graph, finalX, finalY))
            {
                summary = "position=desired";
                return true;
            }

            int commanderY = abovePos == null ? desiredY + 1 : ReadIntMember(abovePos, "y", desiredY + 1);
            for (int yOffset = 0; yOffset <= 8; yOffset++)
            {
                for (int xOffset = 1; xOffset <= 8; xOffset++)
                {
                    int candidateX = parentX + xOffset;
                    int candidateY = abovePos == null ? desiredY - yOffset : commanderY - 1 - yOffset;
                    if (candidateX <= parentX)
                        continue;
                    if (abovePos != null && candidateY >= commanderY)
                        continue;
                    if (IsTechGraphPositionOccupied(graph, candidateX, candidateY))
                        continue;
                    finalX = candidateX;
                    finalY = candidateY;
                    summary = "position=adjusted, desired=" + desiredX + "," + desiredY;
                    return true;
                }
            }

            summary = "position=occupied, desired=" + desiredX + "," + desiredY;
            return false;
        }

        private static bool IsTechGraphPositionOccupied(object graph, int x, int y)
        {
            object? nodesObject = ReadMember(graph, "nodes");
            if (!(nodesObject is IEnumerable nodes))
                return false;

            foreach (object node in nodes)
            {
                object? pos = ReadMember(node, "pos");
                if (pos == null)
                    continue;
                if (ReadIntMember(pos, "x", int.MinValue) == x && ReadIntMember(pos, "y", int.MinValue) == y)
                    return true;
            }
            return false;
        }

        private static object CreateNativeTechNodeCosts(Type techNodeCostType, object graph, object parentNode, int count)
        {
            Array empty = Array.CreateInstance(techNodeCostType, 0);
            if (count <= 0)
                return empty;

            object? referenceCost = FindReferenceTechNodeCost(parentNode) ?? FindReferenceTechNodeCost(graph);
            object? costType = referenceCost == null ? null : ReadMember(referenceCost, "type");
            if (costType == null)
                return empty;

            ConstructorInfo? ctor = techNodeCostType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(candidate => candidate.GetParameters().Length == 2 && candidate.GetParameters()[1].ParameterType == typeof(int));
            if (ctor == null)
                return empty;

            Array costs = Array.CreateInstance(techNodeCostType, 1);
            costs.SetValue(ctor.Invoke(new object[] { costType, count }), 0);
            return costs;
        }

        private static object? FindReferenceTechNodeCost(object source)
        {
            object? nodesObject = ReadMember(source, "nodes");
            if (nodesObject is IEnumerable nodes)
            {
                foreach (object node in nodes)
                {
                    object? cost = FindReferenceTechNodeCost(node);
                    if (cost != null)
                        return cost;
                }
                return null;
            }

            object? data = ReadMember(source, "data") ?? source;
            object? costsObject = ReadMember(data, "costs");
            if (!(costsObject is IEnumerable costs))
                return null;

            foreach (object cost in costs)
            {
                if (cost != null)
                    return cost;
            }
            return null;
        }

        private static int ResolveNativeMachineTechCost(MachineDefinition definition)
        {
            if (definition.EquipmentId.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase) ||
                definition.MachineId.Equals("dtmapi.mine", StringComparison.OrdinalIgnoreCase))
            {
                return 30;
            }
            return 0;
        }

        private static object? TryResolveNativeMachineIcon(string itemId, Type? spriteType)
        {
            if (spriteType == null)
                return null;
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? getItemSprite = dolocApi?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(method => method.Name == "GetItemSprite" && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(string));
                object? sprite = getItemSprite?.Invoke(null, new object[] { itemId });
                return spriteType.IsInstanceOfType(sprite) ? sprite : null;
            }
            catch
            {
                return null;
            }
        }

        private string BuildNativeTechRouteSummary(object graph, object node, string parentId, string aboveTitle, string status, string infoSummary, string equipmentId, string recipeId)
        {
            object? pos = ReadMember(node, "pos");
            int x = pos == null ? 0 : ReadIntMember(pos, "x", 0);
            int y = pos == null ? 0 : ReadIntMember(pos, "y", 0);
            object? parentNode = null;
            TryFindTechGraphNode(graph, parentId, out parentNode);
            object? parentPos = parentNode == null ? null : ReadMember(parentNode, "pos");
            object? aboveNode = FindTechGraphNodeByTitle(graph, aboveTitle) ?? FindTechGraphNodeByTitle(graph, "Commander");
            object? abovePos = aboveNode == null ? null : ReadMember(aboveNode, "pos");
            bool rightOfParent = parentPos != null && x > ReadIntMember(parentPos, "x", x);
            bool aboveCommander = abovePos != null && y < ReadIntMember(abovePos, "y", y + 1);
            object? data = ReadMember(node, "data");
            int costCount = CountArrayItems(data == null ? null : ReadMember(data, "costs"));
            return status +
                ", node=" + ReadStringMember(node, "id") +
                ", tree=" + ReadStringMember(graph, "id") +
                ", parent=" + parentId +
                ", pos=" + x + "," + y +
                ", rightOfParent=" + rightOfParent +
                ", aboveCommander=" + aboveCommander +
                ", equipment=" + equipmentId +
                ", recipe=" + recipeId +
                ", costs=" + costCount +
                ", " + infoSummary;
        }

        private void PublishNativeTechRouteStatus(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
                return;

            string status = summary.StartsWith("failed", StringComparison.OrdinalIgnoreCase) || summary.StartsWith("pending", StringComparison.OrdinalIgnoreCase)
                ? "pending"
                : "verified";
            runtime.SetHookStatus("Machine.MineTechTreeRoute", status, "DolocAPI.assets.techTrees + DolocConfig.Tables.TbTechNode", summary);
        }

        private static int CountArrayItems(object? value)
        {
            if (value is Array array)
                return array.Length;
            if (value is ICollection collection)
                return collection.Count;
            if (value is IEnumerable enumerable)
            {
                int count = 0;
                foreach (object _ in enumerable)
                    count++;
                return count;
            }
            return 0;
        }

        public IReadOnlyList<MachineDefinition> GetMachines(string uniqueId)
        {
            return machineDefinitions.TryGetValue(uniqueId ?? string.Empty, out List<MachineDefinition>? definitions)
                ? definitions.ToArray()
                : Array.Empty<MachineDefinition>();
        }

        MachineProductionState IMachineProductionApi.GetState(string uniqueId)
        {
            if (machineStates.TryGetValue(uniqueId ?? string.Empty, out MachineProductionState state))
                return state;

            return new MachineProductionState
            {
                OwnerId = uniqueId ?? string.Empty,
                Status = "not-configured",
                LastMessage = "No machine definition registered."
            };
        }

        BridgeFeatureStatus IMachineProductionApi.GetStatus(string uniqueId)
        {
            return machineDefinitions.ContainsKey(uniqueId ?? string.Empty)
                ? new BridgeFeatureStatus(machineRuntimeLoopInstalled ? "configured-experimental-runtime-loop" : "configured-pending-runtime-hook", machineRuntimeLoopInstalled ? "Machine definitions are accepted and remain isolated from raw Doloc Town types; DTMAPI observes placed equipment and can deliver weighted outputs, while native fuel/electric UI remains experimental." : "Machine definitions are accepted and remain isolated from raw Doloc Town types; production, fuel, electric, placement-scale, and output-delivery hooks still need third-save evidence.")
                : new BridgeFeatureStatus("not-configured", "No machine definitions were registered for this mod.");
        }

        public EquipmentSlotsRegisterResult RegisterSlots(IManifest owner, EquipmentSlotsOptions options)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            EquipmentSlotsOptions normalized = NormalizeEquipmentSlotsOptions(options);
            equipmentSlotOptions[owner.UniqueID] = normalized;
            EnsureEquipmentSlotStorageLoaded(owner.UniqueID);
            EnsureEquipmentSlotEntries(owner.UniqueID, normalized);

            if (!normalized.Enabled && normalized.SafeUnequipOnDisable)
                RecoverEquipmentSlotEntries(owner.UniqueID, "disabled registration", saveAfterRecovery: true, out _);
            else if (normalized.Enabled)
                TryApplyStoredEquipmentSlotFunctions(owner.UniqueID, "register");
            RenderEquipmentSlotsUiForCurrentAccessoriesBar("register " + owner.UniqueID, force: true);

            EquipmentSlotsState state = BuildEquipmentSlotsState(owner.UniqueID, normalized);
            state.LastRecoveryMessage = FirstText(state.LastRecoveryMessage, "No runtime recovery has run in this session.");
            equipmentSlotStates[owner.UniqueID] = state;
            runtime.RuntimeMonitor.Log("Equipment slots definition registered owner=" + owner.UniqueID + " enabled=" + normalized.Enabled + " extraSlots=" + normalized.ExtraAttributeSlots + " preserveVanillaVisualSlots=" + normalized.PreserveVanillaVisualSlots + ".");
            runtime.SetHookStatus("Player.EquipmentSlotsApi", state.Status, "DTMAPI.GameBridge.DolocTown API", "Registered extra equipment-slot policy for " + owner.UniqueID + "; DTMAPI stores attribute-only slots, renders a read-only player equipment strip, preserves vanilla visual slots, and can recover stored items through native backpack placement.");

            return new EquipmentSlotsRegisterResult
            {
                Success = true,
                OwnerId = owner.UniqueID,
                ExtraAttributeSlots = normalized.Enabled ? normalized.ExtraAttributeSlots : 0,
                Message = state.Status
            };
        }

        public IReadOnlyList<EquipmentSlotInfo> GetSlots(string uniqueId)
        {
            string ownerId = uniqueId ?? string.Empty;
            EquipmentSlotsOptions options = GetEquipmentSlotsOptions(ownerId);
            EnsureEquipmentSlotStorageLoaded(ownerId);
            List<EquipmentSlotRuntimeEntry> entries = EnsureEquipmentSlotEntries(ownerId, options);
            return entries.Select(entry => ToEquipmentSlotInfo(ownerId, entry, options)).ToArray();
        }

        public EquipmentSlotEquipResult EquipExtraSlot(IManifest owner, string slotId, string itemId)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            string ownerId = owner.UniqueID;
            var result = new EquipmentSlotEquipResult
            {
                OwnerId = ownerId,
                SlotId = slotId ?? string.Empty,
                ItemId = itemId ?? string.Empty
            };

            EquipmentSlotsOptions options = GetEquipmentSlotsOptions(ownerId);
            if (!options.Enabled || options.ExtraAttributeSlots <= 0)
                return EquipmentSlotEquipFailed(result, "not-enabled", "Equipment slots are not enabled for " + ownerId + ".");

            EnsureEquipmentSlotStorageLoaded(ownerId);
            List<EquipmentSlotRuntimeEntry> entries = EnsureEquipmentSlotEntries(ownerId, options);
            EquipmentSlotRuntimeEntry? entry = FindEquipmentSlotEntry(entries, options, slotId ?? string.Empty);
            if (entry == null)
                return EquipmentSlotEquipFailed(result, "missing-slot", "Slot " + (slotId ?? string.Empty) + " is not available.");

            result.SlotId = entry.SlotId;
            string normalizedItemId = itemId?.Trim() ?? string.Empty;
            result.ItemId = normalizedItemId;
            if (string.IsNullOrWhiteSpace(normalizedItemId))
                return EquipmentSlotEquipFailed(result, "missing-item", "An item id is required.");

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (dolocApi == null || ReadStaticMember(dolocApi, "archiveHandle") == null)
                return EquipmentSlotEquipFailed(result, "missing-archive", "No loaded archive is available.");

            result.BeforeBackpackCount = CountNativeBackpackItem(dolocApi, normalizedItemId);
            if (result.BeforeBackpackCount <= 0)
                return EquipmentSlotEquipFailed(result, "missing-backpack-item", "Backpack does not contain " + normalizedItemId + ".");

            if (!TryGenerateNativeItem(normalizedItemId, 1, out object? item, out string generateReason, out string generateMessage))
                return EquipmentSlotEquipFailed(result, generateReason, generateMessage);

            if (!TryValidateExtraEquipmentSlotItem(item, out string displayName, out string skillId, out string validationReason, out string validationMessage))
                return EquipmentSlotEquipFailed(result, validationReason, validationMessage);
            result.DisplayName = displayName;

            if (!RecoverEquipmentSlotEntry(ownerId, entry, "replace before equip", saveAfterRecovery: false, out string recoveryMessage, out int recovered))
                return EquipmentSlotEquipFailed(result, "recover-existing-failed", recoveryMessage);
            result.RecoveredCount = recovered;

            if (!TryCostNativeBackpackItem(dolocApi, normalizedItemId, 1))
                return EquipmentSlotEquipFailed(result, "consume-failed", "DolocAPI.CostItem failed for " + normalizedItemId + " x1.");

            entry.ItemId = normalizedItemId;
            entry.DisplayName = displayName;
            entry.SkillId = skillId;
            entry.LastMessage = "Equipped " + displayName + " as attribute-only extra slot item.";
            entry.Applied = false;
            entry.NativeItem = null;
            entry.NativeFunction = null;

            string applyMessage = TryApplyStoredEquipmentSlotFunctions(ownerId, "equip " + entry.SlotId)
                ? "Applied native AgentEquipmentFunction for " + normalizedItemId + "."
                : "Stored item; native AgentEquipmentFunction will apply after equipment manager is available.";
            entry.LastMessage = entry.LastMessage + " " + applyMessage;
            SaveEquipmentSlotStorage(ownerId);
            RenderEquipmentSlotsUiForCurrentAccessoriesBar("equip " + entry.SlotId, force: true);

            EquipmentSlotsState state = BuildEquipmentSlotsState(ownerId, options);
            state.LastEquippedSlotId = entry.SlotId;
            state.LastEquippedItemId = normalizedItemId;
            state.LastRecoveryMessage = entry.LastMessage;
            equipmentSlotStates[ownerId] = state;

            result.AfterBackpackCount = CountNativeBackpackItem(dolocApi, normalizedItemId);
            result.Success = true;
            result.Message = entry.LastMessage + " backpack=" + result.BeforeBackpackCount + "->" + result.AfterBackpackCount + ".";
            runtime.RuntimeMonitor.Log("EquipmentSlots equip OK owner=" + ownerId + " slot=" + entry.SlotId + " item=" + normalizedItemId + " display=" + displayName + " backpack=" + result.BeforeBackpackCount + "->" + result.AfterBackpackCount + " applied=" + entry.Applied + ".");
            runtime.SetHookStatus("Player.EquipmentSlotsApi", state.Status, "IEquipmentSlotsApi.EquipExtraSlot -> AgentEquipmentFunction", result.Message);
            return result;
        }

        public EquipmentSlotEquipResult UnequipExtraSlot(IManifest owner, string slotId, string reason)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            string ownerId = owner.UniqueID;
            EquipmentSlotsOptions options = GetEquipmentSlotsOptions(ownerId);
            EnsureEquipmentSlotStorageLoaded(ownerId);
            List<EquipmentSlotRuntimeEntry> entries = EnsureEquipmentSlotEntries(ownerId, options);
            EquipmentSlotRuntimeEntry? entry = FindEquipmentSlotEntry(entries, options, slotId);
            var result = new EquipmentSlotEquipResult
            {
                OwnerId = ownerId,
                SlotId = slotId ?? string.Empty,
                ItemId = entry?.ItemId ?? string.Empty,
                DisplayName = entry?.DisplayName ?? string.Empty
            };
            if (entry == null)
                return EquipmentSlotEquipFailed(result, "missing-slot", "Slot " + (slotId ?? string.Empty) + " is not available.");

            result.SlotId = entry.SlotId;
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (!string.IsNullOrWhiteSpace(entry.ItemId))
                result.BeforeBackpackCount = CountNativeBackpackItem(dolocApi, entry.ItemId);

            if (!RecoverEquipmentSlotEntry(ownerId, entry, reason ?? "manual unequip", saveAfterRecovery: true, out string message, out int recoveredCount))
                return EquipmentSlotEquipFailed(result, "recover-failed", message);

            result.RecoveredCount = recoveredCount;
            if (!string.IsNullOrWhiteSpace(result.ItemId))
                result.AfterBackpackCount = CountNativeBackpackItem(dolocApi, result.ItemId);

            EquipmentSlotsState state = BuildEquipmentSlotsState(ownerId, options);
            state.LastRecoveryMessage = message;
            equipmentSlotStates[ownerId] = state;

            result.Success = true;
            result.Message = message;
            runtime.RuntimeMonitor.Log("EquipmentSlots unequip OK owner=" + ownerId + " slot=" + entry.SlotId + " item=" + result.ItemId + " recovered=" + recoveredCount + " reason=" + (reason ?? string.Empty) + ".");
            runtime.SetHookStatus("Player.EquipmentSlotsApi", state.Status, "IEquipmentSlotsApi.UnequipExtraSlot -> DolocAPI.TryPlaceInBackpack", message);
            RenderEquipmentSlotsUiForCurrentAccessoriesBar("unequip " + entry.SlotId, force: true);
            return result;
        }

        EquipmentSlotsState IEquipmentSlotsApi.GetState(string uniqueId)
        {
            string ownerId = uniqueId ?? string.Empty;
            if (equipmentSlotStates.TryGetValue(ownerId, out EquipmentSlotsState state))
            {
                EquipmentSlotsOptions options = GetEquipmentSlotsOptions(ownerId);
                EquipmentSlotsState fresh = BuildEquipmentSlotsState(ownerId, options);
                fresh.StatsRefreshCount = state.StatsRefreshCount;
                fresh.LastRecoveryMessage = FirstText(state.LastRecoveryMessage, fresh.LastRecoveryMessage);
                fresh.LastEquippedSlotId = state.LastEquippedSlotId;
                fresh.LastEquippedItemId = state.LastEquippedItemId;
                equipmentSlotStates[ownerId] = fresh;
                return fresh;
            }

            return new EquipmentSlotsState
            {
                OwnerId = ownerId,
                IsConfigured = false,
                Status = "not-configured",
                LastRecoveryMessage = "No equipment-slot policy registered."
            };
        }

        public EquipmentSlotsRecoveryResult RecoverExtraSlotItems(IManifest owner, string reason)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            EnsureEquipmentSlotStorageLoaded(owner.UniqueID);
            bool success = RecoverEquipmentSlotEntries(owner.UniqueID, reason ?? "manual recovery", saveAfterRecovery: true, out EquipmentSlotsRecoveryResult result);
            EquipmentSlotsState state = BuildEquipmentSlotsState(owner.UniqueID, GetEquipmentSlotsOptions(owner.UniqueID));
            state.LastRecoveryMessage = result.Message;
            equipmentSlotStates[owner.UniqueID] = state;
            runtime.RuntimeMonitor.Log("Equipment slots recovery requested owner=" + owner.UniqueID + " " + state.LastRecoveryMessage);
            RenderEquipmentSlotsUiForCurrentAccessoriesBar("recover " + owner.UniqueID, force: true);
            result.Success = success;
            return result;
        }

        BridgeFeatureStatus IEquipmentSlotsApi.GetStatus(string uniqueId)
        {
            string ownerId = uniqueId ?? string.Empty;
            return equipmentSlotOptions.ContainsKey(ownerId)
                ? new BridgeFeatureStatus(((IEquipmentSlotsApi)this).GetState(ownerId).Status, "Extra attribute-slot policy is registered while preserving vanilla visual slots; DTMAPI owns extra-slot storage, read-only player equipment strip rendering, native AgentEquipmentFunction application, and safe recovery through backpack/mail overflow.")
                : new BridgeFeatureStatus("not-configured", "No equipment-slot policy was registered for this mod.");
        }

        private EquipmentSlotsOptions GetEquipmentSlotsOptions(string ownerId)
        {
            return equipmentSlotOptions.TryGetValue(ownerId ?? string.Empty, out EquipmentSlotsOptions? options)
                ? options
                : new EquipmentSlotsOptions
                {
                    Enabled = false,
                    ExtraAttributeSlots = equipmentSlotEntries.TryGetValue(ownerId ?? string.Empty, out List<EquipmentSlotRuntimeEntry>? entries) ? entries.Count : 0,
                    SlotIdPrefix = "dtmapi.extra",
                    PreserveVanillaVisualSlots = true,
                    ExtraSlotsAffectVisuals = false,
                    SafeUnequipOnDisable = true,
                    AutoRecoverOnMissingMod = true
                };
        }

        private List<EquipmentSlotRuntimeEntry> EnsureEquipmentSlotEntries(string ownerId, EquipmentSlotsOptions options)
        {
            ownerId ??= string.Empty;
            if (!equipmentSlotEntries.TryGetValue(ownerId, out List<EquipmentSlotRuntimeEntry>? entries))
            {
                entries = new List<EquipmentSlotRuntimeEntry>();
                equipmentSlotEntries[ownerId] = entries;
            }

            int targetCount = Math.Max(0, options.Enabled ? options.ExtraAttributeSlots : Math.Max(options.ExtraAttributeSlots, entries.Count));
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].Index = i;
                if (string.IsNullOrWhiteSpace(entries[i].SlotId))
                    entries[i].SlotId = BuildEquipmentSlotId(options, i);
            }

            while (entries.Count < targetCount)
            {
                int index = entries.Count;
                entries.Add(new EquipmentSlotRuntimeEntry
                {
                    Index = index,
                    SlotId = BuildEquipmentSlotId(options, index),
                    LastMessage = "Empty DTMAPI extra equipment slot."
                });
            }

            if (options.Enabled && entries.Count > targetCount)
            {
                for (int i = targetCount; i < entries.Count; i++)
                    entries[i].LastMessage = "Slot is outside the current enabled range and will be recovered when safe recovery runs.";
            }

            return entries;
        }

        private static string BuildEquipmentSlotId(EquipmentSlotsOptions options, int index)
        {
            return FirstText(options.SlotIdPrefix, "dtmapi.extra") + "." + (index + 1).ToString(CultureInfo.InvariantCulture);
        }

        private static EquipmentSlotRuntimeEntry? FindEquipmentSlotEntry(List<EquipmentSlotRuntimeEntry> entries, EquipmentSlotsOptions options, string slotId)
        {
            slotId = (slotId ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(slotId))
            {
                EquipmentSlotRuntimeEntry? exact = entries.FirstOrDefault(entry => entry.SlotId.Equals(slotId, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                    return exact;
                if (int.TryParse(slotId, NumberStyles.Integer, CultureInfo.InvariantCulture, out int oneBased) && oneBased > 0)
                    return entries.FirstOrDefault(entry => entry.Index == oneBased - 1);
            }

            return entries
                .Where(entry => entry.Index >= 0 && entry.Index < Math.Max(0, options.ExtraAttributeSlots))
                .FirstOrDefault(entry => string.IsNullOrWhiteSpace(entry.ItemId)) ??
                entries.FirstOrDefault(entry => entry.Index >= 0 && entry.Index < Math.Max(0, options.ExtraAttributeSlots));
        }

        private EquipmentSlotsState BuildEquipmentSlotsState(string ownerId, EquipmentSlotsOptions options)
        {
            List<EquipmentSlotRuntimeEntry> entries = equipmentSlotEntries.TryGetValue(ownerId ?? string.Empty, out List<EquipmentSlotRuntimeEntry>? existing)
                ? existing
                : new List<EquipmentSlotRuntimeEntry>();
            int enabledCount = options.Enabled ? Math.Max(0, options.ExtraAttributeSlots) : 0;
            int storedCount = entries.Count(entry => !string.IsNullOrWhiteSpace(entry.ItemId));
            int appliedCount = entries.Count(entry => !string.IsNullOrWhiteSpace(entry.ItemId) && entry.Applied);
            int pendingRecovery = entries.Count(entry => !string.IsNullOrWhiteSpace(entry.ItemId) && (!options.Enabled || entry.Index >= enabledCount));
            string status = !options.Enabled
                ? (storedCount > 0 ? "disabled-recovery-pending" : "disabled")
                : equipmentSlotsRuntimeHooksInstalled
                    ? (equipmentSlotsUiHooksInstalled || equipmentSlotsUiRendered ? "configured-experimental-player-ui-storage-stats-hook" : "configured-experimental-storage-stats-hook")
                    : "configured-experimental-ui-storage";

            EquipmentSlotsState previous = equipmentSlotStates.TryGetValue(ownerId ?? string.Empty, out EquipmentSlotsState? oldState)
                ? oldState
                : new EquipmentSlotsState();
            return new EquipmentSlotsState
            {
                OwnerId = ownerId ?? string.Empty,
                IsConfigured = options.Enabled,
                RuntimeUiHookInstalled = options.Enabled && (equipmentSlotsUiHooksInstalled || equipmentSlotsUiRendered),
                RuntimeStatsHookInstalled = equipmentSlotsRuntimeHooksInstalled,
                ExtraAttributeSlots = enabledCount,
                StoredItemCount = storedCount,
                AppliedItemCount = appliedCount,
                PreserveVanillaVisualSlots = options.PreserveVanillaVisualSlots,
                ExtraSlotsAffectVisuals = false,
                SafeUnequipOnDisable = options.SafeUnequipOnDisable,
                StatsRefreshCount = previous.StatsRefreshCount,
                PendingRecoveryCount = pendingRecovery,
                LastEquippedSlotId = previous.LastEquippedSlotId,
                LastEquippedItemId = previous.LastEquippedItemId,
                Status = status,
                LastRecoveryMessage = FirstText(previous.LastRecoveryMessage, storedCount == 0 ? "No DTMAPI extra-slot items are stored." : "Stored extra-slot item count=" + storedCount + ", applied=" + appliedCount + ".")
            };
        }

        private static EquipmentSlotInfo ToEquipmentSlotInfo(string ownerId, EquipmentSlotRuntimeEntry entry, EquipmentSlotsOptions options)
        {
            return new EquipmentSlotInfo
            {
                OwnerId = ownerId ?? string.Empty,
                SlotId = entry.SlotId,
                Index = entry.Index,
                ItemId = entry.ItemId,
                DisplayName = entry.DisplayName,
                IsOccupied = !string.IsNullOrWhiteSpace(entry.ItemId),
                IsApplied = entry.Applied,
                IsRecoverable = true,
                AttributeOnly = true,
                AffectsVisuals = false,
                LastMessage = FirstText(entry.LastMessage, string.IsNullOrWhiteSpace(entry.ItemId) ? "Empty attribute-only slot." : "Stored attribute-only item.")
            };
        }

        private void EnsureEquipmentSlotStorageLoaded(string ownerId)
        {
            ownerId ??= string.Empty;
            if (string.IsNullOrWhiteSpace(ownerId) || loadedEquipmentSlotStorageOwners.Contains(ownerId))
                return;
            loadedEquipmentSlotStorageOwners.Add(ownerId);

            string path = GetEquipmentSlotStoragePath(ownerId);
            if (!File.Exists(path))
                return;

            try
            {
                EquipmentSlotStorageDocument? document = ReadJson<EquipmentSlotStorageDocument>(path);
                if (document?.Slots == null)
                    return;

                var entries = new List<EquipmentSlotRuntimeEntry>();
                for (int i = 0; i < document.Slots.Count; i++)
                {
                    EquipmentSlotStorageEntry slot = document.Slots[i] ?? new EquipmentSlotStorageEntry();
                    entries.Add(new EquipmentSlotRuntimeEntry
                    {
                        Index = slot.Index >= 0 ? slot.Index : i,
                        SlotId = FirstText(slot.SlotId, "dtmapi.extra." + (i + 1).ToString(CultureInfo.InvariantCulture)),
                        ItemId = slot.ItemId ?? string.Empty,
                        DisplayName = slot.DisplayName ?? string.Empty,
                        SkillId = slot.SkillId ?? string.Empty,
                        LastMessage = FirstText(slot.LastMessage, "Loaded stored DTMAPI extra-slot item.")
                    });
                }
                equipmentSlotEntries[ownerId] = entries.OrderBy(entry => entry.Index).ToList();
                runtime.RuntimeMonitor.Log("EquipmentSlots storage loaded owner=" + ownerId + " slots=" + entries.Count + " storedItems=" + entries.Count(entry => !string.IsNullOrWhiteSpace(entry.ItemId)) + ".");
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "EquipmentSlots storage load failed for " + ownerId + ".", ex.ToString());
                runtime.RuntimeMonitor.Log("EquipmentSlots storage load failed owner=" + ownerId + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            }
        }

        private void SaveEquipmentSlotStorage(string ownerId)
        {
            ownerId ??= string.Empty;
            if (string.IsNullOrWhiteSpace(ownerId))
                return;

            List<EquipmentSlotRuntimeEntry> entries = equipmentSlotEntries.TryGetValue(ownerId, out List<EquipmentSlotRuntimeEntry>? existing)
                ? existing
                : new List<EquipmentSlotRuntimeEntry>();
            var document = new EquipmentSlotStorageDocument
            {
                OwnerId = ownerId,
                SavedAt = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture),
                Slots = entries.Select(entry => new EquipmentSlotStorageEntry
                {
                    Index = entry.Index,
                    SlotId = entry.SlotId,
                    ItemId = entry.ItemId,
                    DisplayName = entry.DisplayName,
                    SkillId = entry.SkillId,
                    LastMessage = entry.LastMessage
                }).ToList()
            };

            string path = GetEquipmentSlotStoragePath(ownerId);
            try
            {
                WriteJson(path, document);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "EquipmentSlots storage save failed for " + ownerId + ".", ex.ToString());
                runtime.RuntimeMonitor.Log("EquipmentSlots storage save failed owner=" + ownerId + " error=" + ex.GetType().Name + ": " + ex.Message, LogLevel.Warn);
            }
        }

        private string GetEquipmentSlotStoragePath(string ownerId)
        {
            return Path.Combine(runtime.Paths.ConfigPath, "equipment-slots-" + MakeSafeFileName(ownerId ?? "unknown") + ".json");
        }

        private static string MakeSafeFileName(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return string.IsNullOrWhiteSpace(value) ? "unknown" : value;
        }

        private static T? ReadJson<T>(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                var serializer = new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true
                });
                object? value = serializer.ReadObject(stream);
                return value is T typed ? typed : default;
            }
        }

        private static void WriteJson<T>(string path, T value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            using (FileStream stream = File.Create(path))
            {
                var serializer = new DataContractJsonSerializer(typeof(T), new DataContractJsonSerializerSettings
                {
                    UseSimpleDictionaryFormat = true
                });
                serializer.WriteObject(stream, value);
            }
        }

        private bool TryApplyStoredEquipmentSlotFunctions(string ownerId, string reason)
        {
            if (equipmentSlotsApplyingFunctions)
                return false;
            if (!equipmentSlotEntries.TryGetValue(ownerId ?? string.Empty, out List<EquipmentSlotRuntimeEntry>? entries))
                return false;

            object? manager = GetNativeAgentEquipmentManager();
            if (manager == null)
                return false;

            bool changed = false;
            bool appliedAny = false;
            equipmentSlotsApplyingFunctions = true;
            try
            {
                foreach (EquipmentSlotRuntimeEntry entry in entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.ItemId))
                        continue;
                    if (entry.Applied && entry.NativeItem != null && NativeEquipmentFunctionsContains(manager, entry.NativeItem))
                    {
                        appliedAny = true;
                        continue;
                    }

                    RemoveEquipmentSlotFunction(manager, entry);
                    if (TryApplyEquipmentSlotFunction(manager, entry, out string message))
                    {
                        entry.LastMessage = message;
                        changed = true;
                        appliedAny = true;
                    }
                    else
                    {
                        entry.LastMessage = message;
                        runtime.RuntimeMonitor.Log("EquipmentSlots apply skipped owner=" + ownerId + " slot=" + entry.SlotId + " item=" + entry.ItemId + " reason=" + message, LogLevel.Warn);
                    }
                }

                if (changed)
                {
                    InvokeNativeReloadParams(manager);
                    SaveEquipmentSlotStorage(ownerId ?? string.Empty);
                    runtime.RuntimeMonitor.Log("EquipmentSlots applied stored functions owner=" + ownerId + " reason=" + (reason ?? string.Empty) + " applied=" + entries.Count(entry => entry.Applied) + "/" + entries.Count(entry => !string.IsNullOrWhiteSpace(entry.ItemId)) + ".");
                }
            }
            finally
            {
                equipmentSlotsApplyingFunctions = false;
            }

            if (equipmentSlotOptions.TryGetValue(ownerId ?? string.Empty, out EquipmentSlotsOptions? options))
                equipmentSlotStates[ownerId ?? string.Empty] = BuildEquipmentSlotsState(ownerId ?? string.Empty, options);
            RenderEquipmentSlotsUiForCurrentAccessoriesBar("apply " + (reason ?? string.Empty), force: changed);
            return appliedAny;
        }

        private void RecoverOrphanEquipmentSlotsIfNeeded()
        {
            if (equipmentSlotsOrphanRecoveryChecked)
                return;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (dolocApi == null || ReadStaticMember(dolocApi, "archiveHandle") == null)
                return;

            equipmentSlotsOrphanRecoveryChecked = true;
            if (!Directory.Exists(runtime.Paths.ConfigPath))
                return;

            foreach (string path in Directory.GetFiles(runtime.Paths.ConfigPath, "equipment-slots-*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    EquipmentSlotStorageDocument? document = ReadJson<EquipmentSlotStorageDocument>(path);
                    string ownerId = document?.OwnerId ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(ownerId) || equipmentSlotOptions.ContainsKey(ownerId))
                        continue;

                    loadedEquipmentSlotStorageOwners.Remove(ownerId);
                    EnsureEquipmentSlotStorageLoaded(ownerId);
                    if (RecoverEquipmentSlotEntries(ownerId, "orphan storage without registered mod", saveAfterRecovery: true, out EquipmentSlotsRecoveryResult result))
                    {
                        runtime.RuntimeMonitor.Log("EquipmentSlots orphan recovery OK owner=" + ownerId + " recovered=" + result.RecoveredCount + " message=" + result.Message + ".");
                        runtime.SetHookStatus("Player.EquipmentSlotsApi", "orphan-recovery-ok", "DTMAPI config/equipment-slots storage scan", "Recovered missing-mod extra-slot storage for " + ownerId + ": " + result.Message);
                    }
                    else
                    {
                        runtime.RuntimeMonitor.Log("EquipmentSlots orphan recovery failed owner=" + ownerId + " message=" + result.Message + ".", LogLevel.Warn);
                        runtime.SetHookStatus("Player.EquipmentSlotsApi", "orphan-recovery-failed", "DTMAPI config/equipment-slots storage scan", result.Message);
                    }
                }
                catch (Exception ex)
                {
                    runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "EquipmentSlots orphan recovery scan failed for " + path + ".", ex.ToString());
                }
            }
        }

        private static object? GetNativeAgentEquipmentManager()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? direct = ReadStaticMember(dolocApi, "AgentEquipmentManager");
            if (direct != null)
                return direct;

            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? farmData = archive == null ? null : ReadMember(archive, "farmData");
            object? agentData = farmData == null ? null : ReadMember(farmData, "agentData");
            return agentData == null ? null : ReadMember(agentData, "agentEquipment");
        }

        private bool TryApplyEquipmentSlotFunction(object manager, EquipmentSlotRuntimeEntry entry, out string message)
        {
            message = string.Empty;
            if (!TryGenerateNativeItem(entry.ItemId, 1, out object? item, out string reason, out string generateMessage))
            {
                message = reason + ": " + generateMessage;
                return false;
            }

            if (!TryValidateExtraEquipmentSlotItem(item, out string displayName, out string skillId, out string validationReason, out string validationMessage))
            {
                message = validationReason + ": " + validationMessage;
                return false;
            }

            Type? functionType = ResolveType("DolocTown.AgentEquipmentFunction, Assembly-CSharp");
            MethodInfo? create = functionType?.GetMethod("CreateAgentEquipmentFunction", BindingFlags.Public | BindingFlags.Static);
            if (create == null)
            {
                message = "missing-function-factory: AgentEquipmentFunction.CreateAgentEquipmentFunction was not found.";
                return false;
            }

            object?[] args = { item, manager, skillId, null };
            object? ok = create.Invoke(null, args);
            object? function = args[3];
            if (!(ok is bool success) || !success || function == null)
            {
                message = "function-create-failed: Could not create AgentEquipmentFunction for " + entry.ItemId + " skill=" + skillId + ".";
                return false;
            }

            IDictionary? functions = GetNativeEquipmentFunctions(manager);
            if (functions == null)
            {
                TryDisposeEquipmentFunction(function);
                message = "missing-functions-dictionary: AgentEquipmentManager.functions was not available.";
                return false;
            }

            functions[item] = function;
            entry.NativeItem = item;
            entry.NativeFunction = function;
            entry.DisplayName = displayName;
            entry.SkillId = skillId;
            entry.Applied = true;
            message = "Applied " + displayName + " (" + entry.ItemId + ") as attribute-only AgentEquipmentFunction skill=" + skillId + ".";
            return true;
        }

        private static bool NativeEquipmentFunctionsContains(object manager, object item)
        {
            IDictionary? functions = GetNativeEquipmentFunctions(manager);
            return functions != null && functions.Contains(item);
        }

        private static IDictionary? GetNativeEquipmentFunctions(object manager)
        {
            object? functions = ReadMember(manager, "functions");
            return functions as IDictionary;
        }

        private static void RemoveEquipmentSlotFunction(object manager, EquipmentSlotRuntimeEntry entry)
        {
            IDictionary? functions = GetNativeEquipmentFunctions(manager);
            if (functions != null && entry.NativeItem != null && functions.Contains(entry.NativeItem))
                functions.Remove(entry.NativeItem);
            if (entry.NativeFunction != null)
                TryDisposeEquipmentFunction(entry.NativeFunction);
            entry.NativeItem = null;
            entry.NativeFunction = null;
            entry.Applied = false;
        }

        private static void TryDisposeEquipmentFunction(object function)
        {
            try
            {
                FindMethodInHierarchy(function.GetType(), "Dispose", 0)?.Invoke(function, null);
            }
            catch
            {
            }
        }

        private static void InvokeNativeReloadParams(object manager)
        {
            try
            {
                FindMethodInHierarchy(manager.GetType(), "ReloadParams", 0)?.Invoke(manager, null);
            }
            catch
            {
            }
        }

        private static bool TryValidateExtraEquipmentSlotItem(object? item, out string displayName, out string skillId, out string reason, out string message)
        {
            displayName = string.Empty;
            skillId = string.Empty;
            reason = string.Empty;
            message = string.Empty;
            if (item == null)
            {
                reason = "missing-item";
                message = "Generated item is null.";
                return false;
            }

            Type itemType = item.GetType();
            if (!IsTypeOrBase(itemType, "DolocTown.ItemPassive"))
            {
                reason = "not-passive";
                message = "Only passive attribute equipment can be placed in DTMAPI extra slots; " + ReadStringMember(item, "name") + " is " + itemType.FullName + ".";
                return false;
            }

            object? proto = ReadMember(item, "proto");
            object? function = proto == null ? null : ReadMember(proto, "Function");
            string functionType = function == null ? string.Empty : function.GetType().FullName ?? function.GetType().Name;
            if (function == null || functionType.IndexOf("ItemFunctionPassive", StringComparison.OrdinalIgnoreCase) < 0 && functionType.IndexOf("ItemFunctionHerbPackage", StringComparison.OrdinalIgnoreCase) < 0)
            {
                reason = "not-attribute-passive";
                message = "Extra slots are attribute-only and require ItemFunctionPassive/ItemFunctionHerbPackage; item=" + ReadStringMember(item, "name") + ".";
                return false;
            }

            skillId = ReadStringMember(function, "Skill");
            if (string.IsNullOrWhiteSpace(skillId))
            {
                reason = "missing-skill";
                message = "Passive item " + ReadStringMember(item, "name") + " has no equipment skill.";
                return false;
            }

            displayName = FirstText(proto == null ? string.Empty : ReadStringMember(proto, "Title"), ReadStringMember(item, "name"));
            return true;
        }

        private bool RecoverEquipmentSlotEntries(string ownerId, string reason, bool saveAfterRecovery, out EquipmentSlotsRecoveryResult result)
        {
            result = new EquipmentSlotsRecoveryResult
            {
                OwnerId = ownerId ?? string.Empty
            };
            EnsureEquipmentSlotStorageLoaded(ownerId ?? string.Empty);
            if (!equipmentSlotEntries.TryGetValue(ownerId ?? string.Empty, out List<EquipmentSlotRuntimeEntry>? entries))
            {
                result.Success = true;
                result.Message = "No DTMAPI extra-slot storage exists for " + (ownerId ?? string.Empty) + ".";
                return true;
            }

            bool success = true;
            int recovered = 0;
            var messages = new List<string>();
            foreach (EquipmentSlotRuntimeEntry entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.ItemId))
                    continue;
                if (RecoverEquipmentSlotEntry(ownerId ?? string.Empty, entry, reason, saveAfterRecovery: false, out string message, out int entryRecovered))
                {
                    recovered += entryRecovered;
                    messages.Add(message);
                }
                else
                {
                    success = false;
                    messages.Add(message);
                }
            }

            if (saveAfterRecovery)
                SaveEquipmentSlotStorage(ownerId ?? string.Empty);
            object? manager = GetNativeAgentEquipmentManager();
            if (manager != null && recovered > 0)
                InvokeNativeReloadParams(manager);

            result.Success = success;
            result.RecoveredCount = recovered;
            result.FailureReason = success ? string.Empty : "recover-partial-failed";
            result.Message = messages.Count == 0
                ? "Recovery requested reason=" + (reason ?? string.Empty) + "; no DTMAPI extra-slot items were stored."
                : string.Join(" | ", messages);
            return success;
        }

        private bool RecoverEquipmentSlotEntry(string ownerId, EquipmentSlotRuntimeEntry entry, string reason, bool saveAfterRecovery, out string message, out int recoveredCount)
        {
            recoveredCount = 0;
            if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
            {
                message = "Slot is already empty.";
                return true;
            }

            string itemId = entry.ItemId;
            string display = FirstText(entry.DisplayName, itemId);
            object? manager = GetNativeAgentEquipmentManager();
            if (manager != null)
                RemoveEquipmentSlotFunction(manager, entry);

            if (!TryPlaceNativeBackpackItem(itemId, 1, sendEmailOnOverflow: true, out string placeMessage))
            {
                entry.LastMessage = "Recovery failed for " + display + ": " + placeMessage;
                message = entry.LastMessage;
                return false;
            }

            entry.ItemId = string.Empty;
            entry.DisplayName = string.Empty;
            entry.SkillId = string.Empty;
            entry.LastMessage = "Recovered " + display + " from DTMAPI extra slot reason=" + (reason ?? string.Empty) + ". " + placeMessage;
            entry.Applied = false;
            entry.NativeItem = null;
            entry.NativeFunction = null;
            recoveredCount = 1;
            message = entry.LastMessage;

            if (saveAfterRecovery)
                SaveEquipmentSlotStorage(ownerId ?? string.Empty);
            if (manager != null)
                InvokeNativeReloadParams(manager);
            return true;
        }

        private static EquipmentSlotEquipResult EquipmentSlotEquipFailed(EquipmentSlotEquipResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static int CountNativeBackpackItem(Type? dolocApi, string itemId)
        {
            if (dolocApi == null || string.IsNullOrWhiteSpace(itemId))
                return 0;
            return InvokeInt(dolocApi.GetMethod("CountItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(bool) }, null), null, new object?[] { itemId, false }, 0);
        }

        private static bool TryCostNativeBackpackItem(Type dolocApi, string itemId, int count)
        {
            MethodInfo? cost = dolocApi.GetMethod("CostItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int), typeof(bool) }, null);
            object? result = cost?.Invoke(null, new object?[] { itemId, Math.Max(1, count), false });
            return result is bool ok && ok;
        }

        private static bool TryPlaceNativeBackpackItem(string itemId, int count, bool sendEmailOnOverflow, out string message)
        {
            message = string.Empty;
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? tryPlaceInBackpack = dolocApi?.GetMethod("TryPlaceInBackpack", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int), typeof(bool) }, null);
            if (tryPlaceInBackpack == null)
            {
                message = "DolocAPI.TryPlaceInBackpack(string,int,bool) was not found.";
                return false;
            }

            object? placed = tryPlaceInBackpack.Invoke(null, new object?[] { itemId, Math.Max(1, count), sendEmailOnOverflow });
            if (!(placed is bool ok) || !ok)
            {
                message = "DolocAPI.TryPlaceInBackpack returned false for " + itemId + " x" + count + ".";
                return false;
            }

            message = "Returned " + itemId + " x" + count + " through native backpack placement" + (sendEmailOnOverflow ? " with email overflow enabled." : ".");
            return true;
        }

        public InventoryDebugPage GetItems(InventoryDebugQuery query)
        {
            query ??= new InventoryDebugQuery();
            int pageSize = Math.Max(1, Math.Min(50, query.PageSize <= 0 ? 12 : query.PageSize));
            int page = Math.Max(0, query.Page);
            string search = (query.SearchText ?? string.Empty).Trim();
            string category = (query.Category ?? string.Empty).Trim();
            string sourceId = (query.SourceId ?? string.Empty).Trim();

            try
            {
                List<InventoryDebugItem> all = EnumerateInventoryDebugItems().ToList();
                IEnumerable<InventoryDebugItem> filtered = all;
                if (!query.IncludeUnavailable)
                    filtered = filtered.Where(i => i.CanGive);
                if (!string.IsNullOrWhiteSpace(search))
                {
                    filtered = filtered.Where(i =>
                        i.Id.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.DisplayName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.EnglishName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.Category.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.SubCategory.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.IconAssetKey.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.SourceKind.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.SourceModTitle.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.SourceId.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (i.WorkshopId.HasValue && i.WorkshopId.Value.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        i.SearchText.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.Tags.Any(t => t.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
                }

                List<InventoryDebugItem> searchFiltered = filtered.ToList();
                InventoryDebugSourceGroup[] sources = BuildInventoryDebugSourceGroups(searchFiltered);

                IEnumerable<InventoryDebugItem> sourceFiltered = searchFiltered;
                if (!string.IsNullOrWhiteSpace(sourceId))
                    sourceFiltered = FilterInventoryBySource(sourceFiltered, sourceId);
                else if (query.ModItemsOnly)
                    sourceFiltered = sourceFiltered.Where(i => i.IsModItem);

                string[] categories = sourceFiltered
                    .Select(i => FirstText(i.SubCategory, i.Category))
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                IEnumerable<InventoryDebugItem> categoryFiltered = sourceFiltered;
                if (!string.IsNullOrWhiteSpace(category))
                {
                    categoryFiltered = categoryFiltered.Where(i =>
                        i.Category.Equals(category, StringComparison.OrdinalIgnoreCase) ||
                        i.SubCategory.Equals(category, StringComparison.OrdinalIgnoreCase));
                }

                List<InventoryDebugItem> list = categoryFiltered
                    .OrderBy(i => i.IsModItem ? 1 : 0)
                    .ThenBy(i => i.IsModItem ? i.RuntimeOrder : int.MaxValue)
                    .ThenBy(i => i.IsModItem ? Math.Max(0, i.LoadOrder) : 0)
                    .ThenBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                int totalPages = Math.Max(1, (int)Math.Ceiling(list.Count / (double)pageSize));
                page = Math.Min(page, totalPages - 1);
                return new InventoryDebugPage
                {
                    Items = list.Skip(page * pageSize).Take(pageSize).ToArray(),
                    Categories = categories,
                    Sources = sources,
                    Page = page,
                    PageSize = pageSize,
                    TotalItems = list.Count,
                    TotalPages = totalPages,
                    Status = "ok"
                };
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Inventory debug item enumeration failed.", ex.ToString());
                return new InventoryDebugPage
                {
                    Items = Array.Empty<InventoryDebugItem>(),
                    Categories = Array.Empty<string>(),
                    Sources = Array.Empty<InventoryDebugSourceGroup>(),
                    Page = 0,
                    PageSize = pageSize,
                    TotalItems = 0,
                    TotalPages = 1,
                    Status = ex.GetType().Name + ": " + ex.Message
                };
            }
        }

        public InventoryGiveResult GiveItem(IManifest owner, string itemId, int count)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            itemId = (itemId ?? string.Empty).Trim();
            count = Math.Max(0, count);
            var result = new InventoryGiveResult { ItemId = itemId, RequestedCount = count };
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
            {
                result.FailureReason = "invalid-request";
                result.Message = "Invalid item id or count.";
                return result;
            }

            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    return InventoryGiveFailed(result, "missing-dolocapi", "DolocAPI is not available.");

                MethodInfo? queryItemProto = dolocApi.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
                object?[] queryArgs = new object?[] { itemId, null };
                if (!(queryItemProto?.Invoke(null, queryArgs) is bool found) || !found || queryArgs[1] == null)
                    return InventoryGiveFailed(result, "unknown-item", "Item is not present in DolocConfig.Tables.TbItem.");

                object proto = queryArgs[1]!;
                result.DisplayName = FirstText(ReadStringMember(proto, "Title"), itemId);
                int maxStack = Math.Max(1, ReadIntMember(proto, "Overlay", 1));
                IContentItemInfo? sourceInfo = runtime.GetIndexedContentItem(itemId);
                if (sourceInfo != null && !sourceInfo.Enabled)
                    return InventoryGiveFailed(result, "source-disabled", "Item source is disabled by Doloc Town's official Mod UI or Steam Workshop enablement state.");
                int rawOverlay = ReadIntMember(proto, "Overlay", 1);
                if (rawOverlay <= 0)
                    return InventoryGiveFailed(result, "not-spawnable", "Item is present in the runtime table but is marked as not spawnable/stackable.");
                result.BeforeCount = InvokeInt(dolocApi.GetMethod("CountItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(bool) }, null), null, new object?[] { itemId, false }, 0);

                MethodInfo? canPlaceItem = dolocApi.GetMethod("CanPlaceItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int) }, null);
                MethodInfo? tryPlaceInBackpack = dolocApi.GetMethod("TryPlaceInBackpack", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int), typeof(bool) }, null);
                if (canPlaceItem == null || tryPlaceInBackpack == null)
                    return InventoryGiveFailed(result, "missing-native-method", "Native backpack placement methods are not available.");

                int remaining = count;
                int placed = 0;
                while (remaining > 0)
                {
                    int chunk = Math.Min(remaining, maxStack);
                    object? canPlace = canPlaceItem.Invoke(null, new object?[] { itemId, chunk });
                    if (!(canPlace is bool can && can))
                    {
                        result.FailureReason = placed > 0 ? "partial-inventory-full" : "inventory-full-or-unspawnable";
                        break;
                    }

                    object? placedResult = tryPlaceInBackpack.Invoke(null, new object?[] { itemId, chunk, false });
                    if (!(placedResult is bool ok && ok))
                    {
                        result.FailureReason = placed > 0 ? "partial-native-placement-failed" : "native-placement-failed";
                        break;
                    }

                    placed += chunk;
                    remaining -= chunk;
                }

                result.AfterCount = InvokeInt(dolocApi.GetMethod("CountItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(bool) }, null), null, new object?[] { itemId, false }, result.BeforeCount);
                result.GivenCount = Math.Max(placed, result.AfterCount - result.BeforeCount);
                result.Success = result.GivenCount > 0 && string.IsNullOrWhiteSpace(result.FailureReason);
                result.Message = result.Success
                    ? "Gave " + result.GivenCount + " " + result.ItemId + " through native backpack placement."
                    : "Gave " + result.GivenCount + " of " + result.RequestedCount + " " + result.ItemId + "; reason=" + result.FailureReason + ".";

                runtime.RuntimeMonitor.Log("Inventory debug give owner=" + ownerId + " item=" + itemId + " requested=" + count + " placed=" + placed + " before=" + result.BeforeCount + " after=" + result.AfterCount + " success=" + result.Success + " reason=" + result.FailureReason + ".");
                runtime.SetHookStatus(result.Success ? "Smoke.DebugInventoryGive" : "Debug.InventoryGive", result.Success ? "verified" : "failed", "DolocAPI.TryPlaceInBackpack", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Inventory debug give failed for " + itemId + ".", ex.ToString());
                return InventoryGiveFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        BridgeFeatureStatus IInventoryDebugApi.GetStatus()
        {
            return new BridgeFeatureStatus("experimental", "Merges the read-only official local/Workshop item source index with runtime DolocConfig.Tables.TbItem and gives only runtime-loaded, enabled, spawnable items through native DolocAPI.TryPlaceInBackpack.");
        }

        public MailItemDeliveryResult SendItemMail(IManifest owner, MailItemDeliveryRequest request)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            request ??= new MailItemDeliveryRequest();
            string itemId = (request.ItemId ?? string.Empty).Trim();
            int count = Math.Max(0, request.Count);
            string templateName = FirstText(request.TemplateName, "send_item_template");
            var result = new MailItemDeliveryResult
            {
                ItemId = itemId,
                RequestedCount = count,
                EmailName = request.EmailName ?? string.Empty,
                TemplateName = templateName
            };
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
                return MailItemDeliveryFailed(result, "invalid-request", "Invalid item id or count.");

            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    return MailItemDeliveryFailed(result, "missing-dolocapi", "DolocAPI is not available.");

                MethodInfo? queryItemProto = dolocApi.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
                object?[] queryArgs = new object?[] { itemId, null };
                if (!(queryItemProto?.Invoke(null, queryArgs) is bool found) || !found || queryArgs[1] == null)
                    return MailItemDeliveryFailed(result, "unknown-item", "Item is not present in DolocConfig.Tables.TbItem.");

                object proto = queryArgs[1]!;
                result.DisplayName = FirstText(ReadStringMember(proto, "Title"), itemId);
                IContentItemInfo? sourceInfo = runtime.GetIndexedContentItem(itemId);
                if (sourceInfo != null)
                {
                    result.SourceId = sourceInfo.SourceId;
                    result.SourceEnabled = sourceInfo.Enabled;
                    result.SourceEnablementKnown = sourceInfo.EnablementKnown;
                }
                else
                {
                    result.SourceEnablementKnown = false;
                }

                string requiredSourceId = (request.RequiredSourceId ?? string.Empty).Trim();
                if (request.RequireEnabledContentSource && sourceInfo == null)
                    return MailItemDeliveryFailed(result, "missing-content-source", "Mail item " + itemId + " has no indexed DTMAPI/Workshop content source.");
                if (!string.IsNullOrWhiteSpace(requiredSourceId) &&
                    (sourceInfo == null || !sourceInfo.SourceId.Equals(requiredSourceId, StringComparison.OrdinalIgnoreCase)))
                    return MailItemDeliveryFailed(result, "source-mismatch", "Mail item " + itemId + " source is " + (sourceInfo?.SourceId ?? "none") + ", expected " + requiredSourceId + ".");
                if (sourceInfo != null && !sourceInfo.Enabled)
                    return MailItemDeliveryFailed(result, "source-disabled", "Item source is disabled by Doloc Town's official Mod UI or Steam Workshop enablement state.");
                if (request.RequireEnabledContentSource && sourceInfo != null && !sourceInfo.EnablementKnown)
                    return MailItemDeliveryFailed(result, "source-enable-state-unknown", "Item source enablement state is unknown for " + sourceInfo.SourceId + ".");
                if (!TryGenerateNativeItem(itemId, count, out _, out string itemReason, out string itemMessage))
                    return MailItemDeliveryFailed(result, "attachment-" + itemReason, "Mail attachment cannot be generated before native delivery: " + itemMessage);

                MethodInfo? countItem = dolocApi.GetMethod("CountItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(bool) }, null);
                result.BackpackCount = InvokeInt(countItem, null, new object?[] { itemId, false }, 0);
                result.PendingMailCount = CountPendingUnacceptedItemMail(dolocApi, itemId);
                int pendingMailBeforeSend = result.PendingMailCount;

                if (request.SkipIfAlreadyOwned && result.BackpackCount >= count)
                {
                    result.Success = true;
                    result.Skipped = true;
                    result.Message = "Skipped mail delivery; backpack already contains " + result.BackpackCount + " " + itemId + ".";
                    LogMailDeliveryResult(ownerId, result);
                    runtime.SetHookStatus("Mail.ItemDelivery", "skipped", "DolocAPI.CountItem", result.Message);
                    return result;
                }

                if (request.PreventDuplicatePendingMail && result.PendingMailCount >= count)
                {
                    result.Success = true;
                    result.Skipped = true;
                    result.Message = "Skipped mail delivery; an unclaimed item mail already contains " + result.PendingMailCount + " " + itemId + ".";
                    LogMailDeliveryResult(ownerId, result);
                    runtime.SetHookStatus("Mail.ItemDelivery", "skipped", "EmailManager.emails", result.Message);
                    return result;
                }

                MethodInfo? sendItemAsEmail = dolocApi.GetMethod("SendItemAsEmail", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int), typeof(string), typeof(string), typeof(string), typeof(string) }, null);
                if (sendItemAsEmail == null)
                    return MailItemDeliveryFailed(result, "missing-native-method", "DolocAPI.SendItemAsEmail was not found.");

                object? sent = sendItemAsEmail.Invoke(null, new object?[]
                {
                    itemId,
                    count,
                    string.IsNullOrWhiteSpace(request.EmailName) ? null : request.EmailName,
                    string.IsNullOrWhiteSpace(request.Content) ? null : request.Content,
                    string.IsNullOrWhiteSpace(request.Sender) ? null : request.Sender,
                    templateName
                });
                if (!(sent is bool ok && ok))
                    return MailItemDeliveryFailed(result, "native-rejected", "DolocAPI.SendItemAsEmail returned false.");

                result.PendingMailCount = CountPendingUnacceptedItemMail(dolocApi, itemId);
                if (result.PendingMailCount < pendingMailBeforeSend + count)
                    return MailItemDeliveryFailed(result, "missing-attachment-after-send", "Native mail send returned true but no unclaimed " + itemId + " attachment was observed. before=" + pendingMailBeforeSend + ", after=" + result.PendingMailCount + ".");

                result.Sent = true;
                result.Success = true;
                result.Message = "Sent " + count + " " + itemId + " through native DolocAPI.SendItemAsEmail template=" + templateName + ".";
                LogMailDeliveryResult(ownerId, result);
                runtime.SetHookStatus("Mail.ItemDelivery", "experimental", "DolocAPI.SendItemAsEmail", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Mail item delivery failed for " + itemId + ".", ex.ToString());
                return MailItemDeliveryFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        BridgeFeatureStatus IMailDeliveryApi.GetStatus()
        {
            return new BridgeFeatureStatus("experimental", "Uses native DolocAPI.SendItemAsEmail with backpack-count and pending-unclaimed-mail duplicate guards. The current game build ignores custom email title/content/sender parameters for item mail, so DTMAPI treats this as a template-based delivery bridge.");
        }

        public WeatherDebugState GetState()
        {
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? dateNow = archive == null ? null : ReadMember(archive, "DateNow");
                object? timeData = archive == null ? null : ReadMember(archive, "timeData");
                object? season = timeData == null ? null : ReadMember(timeData, "SeasonProto");
                string currentId = ReadStaticMember(dolocApi, "archiveHandle") == null ? string.Empty : (ReadMember(archive!, "CurrentWeatherType")?.ToString() ?? string.Empty);
                string[] forecastIds = GetCurrentDayWeatherInfos(timeData).Select(GetWeatherId).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                WeatherDebugOption? current = GetAvailableWeathers().FirstOrDefault(w => w.Id.Equals(currentId, StringComparison.OrdinalIgnoreCase));
                return new WeatherDebugState
                {
                    CurrentWeatherId = currentId,
                    CurrentWeatherName = current?.DisplayName ?? currentId,
                    Year = dateNow == null ? 0 : ReadIntMember(dateNow, "Year", 0),
                    Month = dateNow == null ? 0 : ReadIntMember(dateNow, "Month", 0),
                    Day = dateNow == null ? 0 : ReadIntMember(dateNow, "Day", 0),
                    Hour = dateNow == null ? 0 : ReadIntMember(dateNow, "Hour", 0),
                    SeasonName = season == null ? string.Empty : FirstText(ReadStringMember(season, "Title"), ReadStringMember(season, "Id")),
                    CurrentDayForecastWeatherIds = forecastIds
                };
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Weather debug state failed.", ex.ToString());
                return new WeatherDebugState();
            }
        }

        public IReadOnlyList<WeatherDebugOption> GetAvailableWeathers()
        {
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? timeData = archive == null ? null : ReadMember(archive, "timeData");
                string current = archive == null ? string.Empty : (ReadMember(archive, "CurrentWeatherType")?.ToString() ?? string.Empty);
                HashSet<string> forecast = new HashSet<string>(GetCurrentDayWeatherInfos(timeData).Select(GetWeatherId), StringComparer.OrdinalIgnoreCase);
                object? table = GetDolocTable("TbWeather");
                object? dataList = table == null ? null : ReadMember(table, "DataList");
                var options = new List<WeatherDebugOption>();
                foreach (object weather in EnumerateObjects(dataList))
                {
                    string id = GetWeatherId(weather);
                    if (string.IsNullOrWhiteSpace(id) || id.Equals("NONE", StringComparison.OrdinalIgnoreCase))
                        continue;
                    options.Add(BuildWeatherOption(weather, current, forecast));
                }

                return options
                    .OrderByDescending(w => w.IsCurrent)
                    .ThenByDescending(w => w.IsCurrentDayForecast)
                    .ThenBy(w => w.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Weather debug list failed.", ex.ToString());
                return Array.Empty<WeatherDebugOption>();
            }
        }

        public WeatherSetResult SetWeather(IManifest owner, string weatherId, bool patchCurrentPeriod)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            weatherId = (weatherId ?? string.Empty).Trim();
            var result = new WeatherSetResult { WeatherId = weatherId, PatchedCurrentPeriod = patchCurrentPeriod };
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                Type? weatherType = ResolveType("DolocTown.Config.Weather.WeatherType, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                if (dolocApi == null || weatherType == null || archive == null)
                    return WeatherSetFailed(result, "missing-native-weather", "Native weather types are not available.");

                object parsed = Enum.Parse(weatherType, weatherId, ignoreCase: true);
                result.BeforeWeatherId = ReadMember(archive, "CurrentWeatherType")?.ToString() ?? string.Empty;
                MethodInfo? setWeather = archive.GetType().GetMethod("SetWeather", BindingFlags.Public | BindingFlags.Instance, null, new[] { weatherType, typeof(bool) }, null);
                if (setWeather == null)
                    return WeatherSetFailed(result, "missing-setweather", "ArchiveDataHandle.SetWeather was not found.");

                setWeather.Invoke(archive, new[] { parsed, (object)true });
                if (patchCurrentPeriod)
                {
                    MethodInfo? patchWeather = archive.GetType().GetMethod("PatchWeather", BindingFlags.Public | BindingFlags.Instance, null, new[] { weatherType }, null);
                    patchWeather?.Invoke(archive, new[] { parsed });
                }

                result.AfterWeatherId = ReadMember(archive, "CurrentWeatherType")?.ToString() ?? string.Empty;
                WeatherDebugOption? option = GetAvailableWeathers().FirstOrDefault(w => w.Id.Equals(result.AfterWeatherId, StringComparison.OrdinalIgnoreCase));
                result.DisplayName = option?.DisplayName ?? result.AfterWeatherId;
                result.Success = result.AfterWeatherId.Equals(weatherId, StringComparison.OrdinalIgnoreCase);
                result.Message = "Weather " + result.BeforeWeatherId + " -> " + result.AfterWeatherId + " patchCurrentPeriod=" + patchCurrentPeriod + ".";
                runtime.RuntimeMonitor.Log("Weather debug set owner=" + ownerId + " " + result.Message);
                runtime.SetHookStatus(result.Success ? "Smoke.DebugWeatherSet" : "Debug.WeatherSet", result.Success ? "verified" : "failed", "ArchiveDataHandle.SetWeather/PatchWeather", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Weather debug set failed for " + weatherId + ".", ex.ToString());
                return WeatherSetFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        BridgeFeatureStatus IWeatherDebugApi.GetStatus()
        {
            return new BridgeFeatureStatus("experimental", "Lists TbWeather and current-day generated weather candidates, then switches via ArchiveDataHandle.SetWeather and PatchWeather.");
        }

        public IReadOnlyList<TeleportDestination> GetDestinations()
        {
            try
            {
                return BuildTeleportDestinations().ToArray();
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Teleport debug destination enumeration failed.", ex.ToString());
                return Array.Empty<TeleportDestination>();
            }
        }

        public TeleportSnapshot GetCurrentSnapshot()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? room = ReadStaticMember(dolocApi, "CurrentRoom");
            object? position = ReadStaticMember(dolocApi, "AgentPosition");
            return BuildTeleportSnapshot(room, position);
        }

        public TeleportResult Teleport(IManifest owner, string destinationId)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            destinationId = (destinationId ?? string.Empty).Trim();
            var result = new TeleportResult { DestinationId = destinationId, Before = GetCurrentSnapshot() };
            try
            {
                TeleportDestination? destination = GetDestinations().FirstOrDefault(d => d.Id.Equals(destinationId, StringComparison.OrdinalIgnoreCase));
                if (destination == null || string.IsNullOrWhiteSpace(destination.MarkPointId))
                    return TeleportFailed(result, "not-whitelisted", "Destination is not in the DTMAPI debug teleport whitelist.");

                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    return TeleportFailed(result, "missing-dolocapi", "DolocAPI is not available.");

                MethodInfo? doTransport = dolocApi.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                    {
                        ParameterInfo[] p = m.GetParameters();
                        return m.Name == "DoTransport" &&
                            p.Length == 5 &&
                            p[0].ParameterType == typeof(string) &&
                            p[2].ParameterType == typeof(bool) &&
                            p[3].ParameterType == typeof(bool) &&
                            p[4].ParameterType == typeof(bool);
                    });
                if (doTransport == null)
                    return TeleportFailed(result, "missing-dotransport", "DolocAPI.DoTransport(markPointId, callback, ...) was not found.");

                result.DestinationName = destination.DisplayName;
                result.MarkPointId = destination.MarkPointId;
                object? accepted = doTransport.Invoke(null, new object?[] { destination.MarkPointId, null, true, false, false });
                result.Success = accepted is bool ok && ok;
                result.AfterRequest = GetCurrentSnapshot();
                if (!result.Success)
                    return TeleportFailed(result, "native-rejected", "DolocAPI.DoTransport rejected " + destination.MarkPointId + ".");

                result.Message = "Teleport request accepted destination=" + destination.DisplayName + " markPoint=" + destination.MarkPointId + " beforeRoom=" + result.Before.RoomId + " targetRoom=" + destination.RoomId + ".";
                runtime.RuntimeMonitor.Log("Teleport debug request owner=" + ownerId + " " + result.Message);
                runtime.SetHookStatus("Smoke.DebugTeleportRequest", "verified", "DolocAPI.DoTransport", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Teleport debug request failed for " + destinationId + ".", ex.ToString());
                return TeleportFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        BridgeFeatureStatus ITeleportDebugApi.GetStatus()
        {
            return new BridgeFeatureStatus("experimental", "Uses whitelisted stations/key mark points and native DolocAPI.DoTransport instead of arbitrary coordinate writes.");
        }

        public TeleportCsvExportResult ExportDestinationsCsv(IManifest owner)
        {
            var result = new TeleportCsvExportResult();
            try
            {
                TeleportDestination[] destinations = GetDestinations().ToArray();
                string evidenceDir = Path.Combine(runtime.Paths.EvidencePath, "TELEPORT-DESTINATIONS", DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
                Directory.CreateDirectory(evidenceDir);
                string path = Path.Combine(evidenceDir, "teleport-destinations.csv");
                var csv = new StringBuilder();
                csv.AppendLine("internal_id,map,x,y,current_display_name,suggested_name,source,mark_point_id,group,is_station");
                foreach (TeleportDestination destination in destinations)
                {
                    csv.Append(Csv(destination.Id)).Append(',')
                        .Append(Csv(destination.RoomId)).Append(',')
                        .Append(destination.X.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                        .Append(destination.Y.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                        .Append(Csv(destination.DisplayName)).Append(',')
                        .Append(Csv(FirstText(destination.SuggestedDisplayName, destination.DisplayName))).Append(',')
                        .Append(Csv(destination.Source)).Append(',')
                        .Append(Csv(destination.MarkPointId)).Append(',')
                        .Append(Csv(destination.Group)).Append(',')
                        .Append(destination.IsStation ? "true" : "false").AppendLine();
                }
                File.WriteAllText(path, csv.ToString(), Encoding.UTF8);

                result.Success = true;
                result.Path = path;
                result.RowCount = destinations.Length;
                result.Message = "Exported " + destinations.Length + " teleport destination row(s) for manual name review.";
                runtime.RuntimeMonitor.Log("Teleport debug CSV export owner=" + (owner?.UniqueID ?? "unknown") + " rows=" + result.RowCount + " path=" + path + ".");
                runtime.SetHookStatus("Debug.TeleportCsvExport", "verified", "ITeleportDebugApi.GetDestinations", result.Message + " path=" + path);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Teleport destination CSV export failed.", ex.ToString());
                runtime.SetHookStatus("Debug.TeleportCsvExport", "failed", "ITeleportDebugApi.GetDestinations", ex.GetType().Name + ": " + ex.Message);
                return TeleportCsvExportFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        InstantSaveDebugState IInstantSaveDebugApi.GetState()
        {
            return GetInstantSaveDebugState();
        }

        public InstantSaveDebugResult Save(IManifest owner, bool reloadAfterSave)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            var result = new InstantSaveDebugResult
            {
                ReloadAfterSave = reloadAfterSave,
                Before = GetInstantSaveDebugState()
            };
            result.SaveSlot = result.Before.SaveSlot;
            if (!result.Before.CanSave || result.Before.SaveSlot == null)
                return InstantSaveFailed(result, FirstText(result.Before.FailureReason, "not-saveable"), FirstText(result.Before.Message, "Current save slot is not available."));

            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? saveGame = FindMethod(dolocApi, "SaveGame", 1);
                if (dolocApi == null || saveGame == null)
                    return InstantSaveFailed(result, "missing-savegame", "DolocAPI.SaveGame(int) was not found.");

                int gameIndex = result.Before.SaveSlot.Value;
                runtime.RuntimeMonitor.Log("Instant save debug requested owner=" + ownerId + " slot/index=" + gameIndex + " location=" + FormatTeleportSnapshot(result.Before.CurrentLocation) + ".");
                runtime.SetHookStatus("Debug.InstantSave", "pending", "DolocAPI.SaveGame", "Requested save from current location " + FormatTeleportSnapshot(result.Before.CurrentLocation) + ".");
                object? saveResult = saveGame.Invoke(null, new object[] { gameIndex });
                if (saveResult is bool saved && !saved)
                    return InstantSaveFailed(result, "native-rejected", "DolocAPI.SaveGame returned false for slot/index " + gameIndex + ".");

                result.AfterSave = GetInstantSaveDebugState();
                result.Success = true;
                result.Message = "Saved slot/index=" + gameIndex + " at " + FormatTeleportSnapshot(result.Before.CurrentLocation) + ".";

                if (reloadAfterSave)
                {
                    MethodInfo? loadGame = FindMethod(dolocApi, "LoadGame", 1);
                    if (loadGame == null)
                        return InstantSaveFailed(result, "missing-loadgame", "DolocAPI.LoadGame(int) was not found.");
                    object? loadResult = loadGame.Invoke(null, new object[] { gameIndex });
                    result.ReloadRequested = !(loadResult is bool loaded) || loaded;
                    result.AfterReloadRequest = GetInstantSaveDebugState();
                    if (!result.ReloadRequested)
                        return InstantSaveFailed(result, "reload-rejected", "DolocAPI.LoadGame returned false for slot/index " + gameIndex + ".");
                    result.Message += " Reload requested for verification.";
                }

                runtime.RuntimeMonitor.Log("Instant save debug OK owner=" + ownerId + " slot/index=" + gameIndex + " reload=" + reloadAfterSave + " before=" + FormatTeleportSnapshot(result.Before.CurrentLocation) + " afterSave=" + FormatTeleportSnapshot(result.AfterSave.CurrentLocation) + ".");
                runtime.SetHookStatus("Debug.InstantSave", "verified", reloadAfterSave ? "DolocAPI.SaveGame -> LoadGame" : "DolocAPI.SaveGame", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Instant save debug failed.", ex.ToString());
                runtime.SetHookStatus("Debug.InstantSave", "failed", "DolocAPI.SaveGame", ex.GetType().Name + ": " + ex.Message);
                return InstantSaveFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        BridgeFeatureStatus IInstantSaveDebugApi.GetStatus()
        {
            return new BridgeFeatureStatus("experimental", "Saves the current loaded slot from the Y console through native DolocAPI.SaveGame and can optionally request LoadGame for verification.");
        }

        TimeDebugState ITimeDebugApi.GetState()
        {
            return GetTimeDebugState();
        }

        public TimeSkipResult SkipToNextWeatherPeriod(IManifest owner)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            var result = new TimeSkipResult { Before = GetTimeDebugState() };
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
                if (dolocApi == null || archive == null || globalParameter == null)
                    return TimeSkipFailed(result, "missing-native-time", "DolocAPI archive/global parameter objects are not available.");

                int hour2Min = Math.Max(1, ReadIntMember(globalParameter, "Hour2Min", 60));
                int day2Hour = Math.Max(1, ReadIntMember(globalParameter, "Day2Hour", 24));
                int currentHour = Math.Max(0, result.Before.Hour);
                int currentMinute = Math.Max(0, result.Before.Minute);
                int targetHour = GetNextDebugWeatherPeriodTarget(currentHour);
                int advancedMinutes = ((targetHour > currentHour ? targetHour - currentHour : targetHour + day2Hour - currentHour) * hour2Min) - currentMinute;
                if (advancedMinutes <= 0)
                    advancedMinutes = Math.Max(1, day2Hour * hour2Min - currentMinute);

                int seconds = Math.Max(1, GameMinutesToSeconds(globalParameter, advancedMinutes));
                MethodInfo? passTime = archive.GetType().GetMethod("PassTimeNoControl", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int), typeof(Action), typeof(bool) }, null)
                    ?? archive.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(m => m.Name == "PassTimeNoControl" && m.GetParameters().Length >= 1);
                if (passTime == null)
                    return TimeSkipFailed(result, "missing-pass-time", "ArchiveDataHandle.PassTimeNoControl was not found.");

                Action wake = () => InvokeWakeUp(dolocApi);
                ParameterInfo[] parameters = passTime.GetParameters();
                object?[] args = parameters.Length >= 3
                    ? new object?[] { seconds, wake, true }
                    : parameters.Length == 2
                        ? new object?[] { seconds, wake }
                        : new object?[] { seconds };
                passTime.Invoke(archive, args);

                result.After = GetTimeDebugState();
                result.AdvancedGameMinutes = advancedMinutes;
                result.AdvancedSeconds = seconds;
                result.TargetHour = targetHour;
                result.Success = true;
                result.Message = "Time advanced to next weather period owner=" + ownerId +
                    " targetHour=" + targetHour +
                    " minutes=" + advancedMinutes +
                    " seconds=" + seconds +
                    " before=" + FormatTimeDebugState(result.Before) +
                    " after=" + FormatTimeDebugState(result.After) + ".";
                runtime.RuntimeMonitor.Log("Time debug skip OK " + result.Message);
                runtime.SetHookStatus("Smoke.DebugTimeSkip", "verified", "ArchiveDataHandle.PassTimeNoControl + DolocAPI.OnWakeUp", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Time debug skip failed.", ex.ToString());
                return TimeSkipFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        BridgeFeatureStatus ITimeDebugApi.GetStatus()
        {
            return new BridgeFeatureStatus("experimental", "Skips to the next 06:00/18:00/24:00 weather period with ArchiveDataHandle.PassTimeNoControl and DolocAPI.OnWakeUp.");
        }

        MovementDebugState IMovementDebugApi.GetState()
        {
            return GetMovementDebugState("query");
        }

        public MovementSpeedResult SetSpeedMultiplier(IManifest owner, double multiplier)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            multiplier = ClampDebugSpeedMultiplier(multiplier);
            var result = new MovementSpeedResult
            {
                RequestedMultiplier = multiplier,
                Before = GetMovementDebugState("before")
            };

            try
            {
                object? motionAbility = ResolveMotionAbility();
                if (motionAbility == null)
                    return MovementSpeedFailed(result, "missing-motion-ability", "Player MotionAbility was not found.");

                MethodInfo? setScale = motionAbility.GetType().GetMethod("SetMoveScaler", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(float) }, null)
                    ?? motionAbility.GetType().GetMethod("SetMoveSpeedScale", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(float) }, null);
                if (setScale == null)
                    return MovementSpeedFailed(result, "missing-set-scale", "MotionAbility.SetMoveScaler(float) was not found.");

                setScale.Invoke(motionAbility, new object[] { (float)(multiplier - 1d) });
                movementSpeedMultiplier = multiplier;
                result.After = GetMovementDebugState("after");
                result.AppliedMultiplier = movementSpeedMultiplier;
                result.Success = true;
                result.Message = "Movement speed owner=" + ownerId + " multiplier=" + multiplier.ToString("0.###") + " beforeSpeed=" + FormatRatio(result.Before.MoveSpeed) + " afterSpeed=" + FormatRatio(result.After.MoveSpeed) + ".";
                runtime.RuntimeMonitor.Log("Movement debug speed OK " + result.Message);
                runtime.SetHookStatus("Smoke.DebugMovementSpeed", "verified", "MotionAbility.SetMoveScaler", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Movement debug speed failed.", ex.ToString());
                return MovementSpeedFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        public MovementSpeedResult ResetSpeed(IManifest owner, string reason)
        {
            MovementSpeedResult result = SetSpeedMultiplier(owner, 1);
            if (result.Success)
                result.Message += " resetReason=" + (reason ?? string.Empty);
            return result;
        }

        BridgeFeatureStatus IMovementDebugApi.GetStatus()
        {
            return new BridgeFeatureStatus("experimental", "Sets player movement through MotionAbility.SetMoveScaler and resets by applying multiplier 1x.");
        }

        public MotorVehicleState GetOriginalMotorState()
        {
            return BuildOriginalMotorState("query");
        }

        public MotorVehicleState GetVehicleState(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId) ||
                vehicleId.Equals("original", StringComparison.OrdinalIgnoreCase) ||
                vehicleId.Equals("doloc.original_motor", StringComparison.OrdinalIgnoreCase))
                return GetOriginalMotorState();

            return secondMotors.TryGetValue(vehicleId, out SecondMotorRuntime vehicle)
                ? BuildSecondMotorState(vehicle, "query")
                : new MotorVehicleState
                {
                    VehicleId = vehicleId ?? string.Empty,
                    LastFailureReason = "not-registered",
                    LastMessage = "No DTMAPI second motor is registered with this id."
                };
        }

        public IReadOnlyList<MotorVehicleState> GetVehicles()
        {
            var result = new List<MotorVehicleState> { GetOriginalMotorState() };
            foreach (SecondMotorRuntime vehicle in secondMotors.Values.OrderBy(v => v.Options.VehicleId, StringComparer.OrdinalIgnoreCase))
                result.Add(BuildSecondMotorState(vehicle, "query"));
            return result.ToArray();
        }

        public MotorVehicleRegisterResult RegisterSecondMotor(IManifest owner, SecondMotorOptions options)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            options ??= new SecondMotorOptions();
            options.VehicleId = FirstText(options.VehicleId, ownerId + ".second_motor");
            options.KeyItemId = FirstText(options.KeyItemId, "dtmapi_second_motor_key");
            options.DisplayName = FirstText(options.DisplayName, "Second Motor");
            options.SpeedMultiplier = Math.Max(0.1, Math.Min(8, options.SpeedMultiplier <= 0 ? 2 : options.SpeedMultiplier));

            var result = new MotorVehicleRegisterResult
            {
                VehicleId = options.VehicleId,
                KeyItemId = options.KeyItemId
            };

            if (owner == null)
                return MotorVehicleRegisterFailed(result, "missing-owner", "Vehicle registration requires a mod manifest owner.");
            if (string.IsNullOrWhiteSpace(options.VehicleId) || string.IsNullOrWhiteSpace(options.KeyItemId))
                return MotorVehicleRegisterFailed(result, "invalid-options", "VehicleId and KeyItemId are required.");
            if (!IsOwnerOfficiallyEnabled(owner.UniqueID, out string enablementMessage))
                return MotorVehicleRegisterFailed(result, "source-disabled", enablementMessage);
            if (secondMotorsByKeyItemId.TryGetValue(options.KeyItemId, out SecondMotorRuntime existingByKey) &&
                !existingByKey.Options.VehicleId.Equals(options.VehicleId, StringComparison.OrdinalIgnoreCase))
                return MotorVehicleRegisterFailed(result, "duplicate-key", "Key item " + options.KeyItemId + " is already registered by " + existingByKey.OwnerUniqueId + ".");

            if (!secondMotors.TryGetValue(options.VehicleId, out SecondMotorRuntime vehicle))
            {
                vehicle = new SecondMotorRuntime(ownerId, options);
                secondMotors[options.VehicleId] = vehicle;
            }
            else
            {
                vehicle.OwnerUniqueId = ownerId;
                vehicle.Options = options;
            }
            secondMotorsByKeyItemId[options.KeyItemId] = vehicle;

            vehicle.LastMessage = "Registered DTMAPI second motor key=" + options.KeyItemId + " speedMultiplier=" + options.SpeedMultiplier.ToString("0.###") + ".";
            result.Success = true;
            result.State = BuildSecondMotorState(vehicle, "registered");
            result.Message = vehicle.LastMessage;
            runtime.RuntimeMonitor.Log("Motor vehicle registration owner=" + ownerId + " vehicle=" + options.VehicleId + " key=" + options.KeyItemId + " speedMultiplier=" + options.SpeedMultiplier.ToString("0.###") + " visuals=" + (options.UseOriginalMotorVisuals ? "original-runtime-clone" : "custom") + " note=" + options.TextureSourceNote);
            runtime.SetHookStatus("Vehicle.SecondMotorRegistration", "experimental", "IMotorVehicleApi.RegisterSecondMotor", result.Message);
            RaiseVehicleEvent("registered", vehicle.Options.VehicleId, result.State, result.Message);
            return result;
        }

        public MotorVehicleSummonResult UnlockOriginalMotor(IManifest owner, double yOffset)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            var result = new MotorVehicleSummonResult
            {
                VehicleId = "doloc.original_motor",
                DisplayName = "Original Motor",
                Before = GetOriginalMotorState()
            };

            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? unlock = dolocApi?.GetMethod("UnlockMotor", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(float) }, null);
                if (unlock == null)
                    return MotorVehicleSummonFailed(result, "missing-unlock", "DolocAPI.UnlockMotor(float) was not found.");

                unlock.Invoke(null, new object[] { (float)yOffset });
                result.After = GetOriginalMotorState();
                result.Success = true;
                result.Message = "Original motor unlock requested owner=" + ownerId + " beforeUnlocked=" + result.Before.IsUnlocked + " afterUnlocked=" + result.After.IsUnlocked + ".";
                runtime.RuntimeMonitor.Log("Motor vehicle unlock OK " + result.Message);
                runtime.SetHookStatus("Vehicle.OriginalMotorUnlock", "experimental", "DolocAPI.UnlockMotor", result.Message);
                RaiseVehicleEvent("unlock", result.VehicleId, result.After, result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Original motor unlock failed.", ex.ToString());
                return MotorVehicleSummonFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        public MotorVehicleSummonResult SummonOriginalMotor(IManifest owner)
        {
            string ownerId = owner?.UniqueID ?? "unknown";
            var result = new MotorVehicleSummonResult
            {
                VehicleId = "doloc.original_motor",
                DisplayName = "Original Motor",
                Before = GetOriginalMotorState()
            };

            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    return MotorVehicleSummonFailed(result, "missing-dolocapi", "DolocAPI is not available.");

                if (!result.Before.IsUnlocked)
                {
                    MethodInfo? unlock = dolocApi.GetMethod("UnlockMotor", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(float) }, null);
                    unlock?.Invoke(null, new object[] { 0f });
                }

                if (!CanCallMotorInCurrentRoom(dolocApi, out string reason, out string message))
                    return MotorVehicleSummonFailed(result, reason, message);

                object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
                object? target = BuildMotorSummonPositionNearAgent(0.95f);
                if (currentRoom == null || target == null)
                    return MotorVehicleSummonFailed(result, "missing-position", "Current room or agent position was not available.");

                MethodInfo? setMotorPosition = dolocApi.GetMethod("SetMotorPosition", BindingFlags.Public | BindingFlags.Static);
                if (setMotorPosition == null)
                    return MotorVehicleSummonFailed(result, "missing-set-position", "DolocAPI.SetMotorPosition(Room, Vector2) was not found.");

                setMotorPosition.Invoke(null, new[] { currentRoom, target });
                object? motor = ReadStaticMember(dolocApi, "Motor");
                TryInvokeAutoFlyToAgent(motor);
                result.After = GetOriginalMotorState();
                result.Success = true;
                result.Message = "Original motor summon requested owner=" + ownerId + " room=" + result.After.RoomId + " x=" + FormatRatio(result.After.X) + " y=" + FormatRatio(result.After.Y) + ".";
                runtime.RuntimeMonitor.Log("Motor vehicle original summon OK " + result.Message);
                runtime.SetHookStatus("Vehicle.OriginalMotorSummon", "experimental", "DolocAPI.SetMotorPosition + MotorController.AutoFlyTo", result.Message);
                RaiseVehicleEvent("summon", result.VehicleId, result.After, result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Original motor summon failed.", ex.ToString());
                return MotorVehicleSummonFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        public MotorVehicleSummonResult SummonVehicle(IManifest owner, string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId) ||
                vehicleId.Equals("original", StringComparison.OrdinalIgnoreCase) ||
                vehicleId.Equals("doloc.original_motor", StringComparison.OrdinalIgnoreCase))
                return SummonOriginalMotor(owner);

            if (!secondMotors.TryGetValue(vehicleId, out SecondMotorRuntime vehicle))
            {
                return MotorVehicleSummonFailed(new MotorVehicleSummonResult
                {
                    VehicleId = vehicleId ?? string.Empty,
                    Before = new MotorVehicleState { VehicleId = vehicleId ?? string.Empty }
                }, "not-registered", "No DTMAPI second motor is registered with this id.");
            }

            return SummonSecondMotor(owner, vehicle, "api");
        }

        public MotorVehicleRideResult RideVehicle(IManifest owner, string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId) ||
                vehicleId.Equals("original", StringComparison.OrdinalIgnoreCase) ||
                vehicleId.Equals("doloc.original_motor", StringComparison.OrdinalIgnoreCase))
            {
                return MotorVehicleRideFailed(new MotorVehicleRideResult
                {
                    VehicleId = "doloc.original_motor",
                    DisplayName = "Original Motor",
                    Action = "ride",
                    Before = GetOriginalMotorState()
                }, "native-only", "Programmatic riding currently supports DTMAPI-managed second motors only; use the game's native interaction for the original motor.");
            }

            if (!secondMotors.TryGetValue(vehicleId, out SecondMotorRuntime vehicle))
            {
                return MotorVehicleRideFailed(new MotorVehicleRideResult
                {
                    VehicleId = vehicleId ?? string.Empty,
                    Action = "ride",
                    Before = new MotorVehicleState { VehicleId = vehicleId ?? string.Empty }
                }, "not-registered", "No DTMAPI second motor is registered with this id.");
            }

            var result = new MotorVehicleRideResult
            {
                VehicleId = vehicle.Options.VehicleId,
                DisplayName = vehicle.Options.DisplayName,
                Action = "ride",
                Before = BuildSecondMotorState(vehicle, "before-ride")
            };

            bool success = TryStartSecondMotorRide(vehicle, "api:" + (owner?.UniqueID ?? "unknown"));
            result.After = BuildSecondMotorState(vehicle, "after-ride");
            if (!success || !result.After.IsRiding)
                return MotorVehicleRideFailed(result, FirstText(vehicle.LastFailureReason, "ride-failed"), FirstText(vehicle.LastMessage, "Second motor ride request did not enter riding state."));

            result.Success = true;
            result.Message = vehicle.LastMessage;
            runtime.SetHookStatus("Vehicle.SecondMotorRide", "experimental", "IMotorVehicleApi.RideVehicle -> AgentControllerState.GetOnMotor", result.Message);
            return result;
        }

        public MotorVehicleRideResult DismountVehicle(IManifest owner, string reason)
        {
            SecondMotorRuntime? dismountedSecondMotor = activeSecondMotor;
            string vehicleId = dismountedSecondMotor?.Options.VehicleId ?? "doloc.original_motor";
            string displayName = dismountedSecondMotor?.Options.DisplayName ?? "Original Motor";
            MotorVehicleState before = dismountedSecondMotor == null ? GetOriginalMotorState() : BuildSecondMotorState(dismountedSecondMotor, "before-dismount");
            var result = new MotorVehicleRideResult
            {
                VehicleId = vehicleId,
                DisplayName = displayName,
                Action = "dismount",
                Before = before
            };

            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    return MotorVehicleRideFailed(result, "missing-dolocapi", "DolocAPI is not available.");

                if (!ReadStaticBoolMember(dolocApi, "IsAgentRiding", false) && activeSecondMotor == null)
                {
                    result.After = before;
                    result.Success = true;
                    result.Message = "No active motor ride was observed; dismount treated as a no-op owner=" + (owner?.UniqueID ?? "unknown") + ".";
                    return result;
                }

                object? agentController = GetAgentController(dolocApi);
                if (agentController == null)
                    return MotorVehicleRideFailed(result, "missing-agent-controller", "DolocAPI.AgentController was not available.");

                MethodInfo? getOff = FindMethodInHierarchy(agentController.GetType(), "GetOffMotor", 0);
                if (getOff == null)
                    return MotorVehicleRideFailed(result, "missing-get-off", "AgentControllerState.GetOffMotor was not found.");

                getOff.Invoke(agentController, null);
                result.After = dismountedSecondMotor == null ? GetOriginalMotorState() : BuildSecondMotorState(dismountedSecondMotor, "after-dismount");
                result.Success = true;
                result.Message = "Motor dismount requested owner=" + (owner?.UniqueID ?? "unknown") + " reason=" + (reason ?? string.Empty) + ".";
                runtime.SetHookStatus("Vehicle.MotorDismount", "experimental", "IMotorVehicleApi.DismountVehicle -> AgentControllerState.GetOffMotor", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Motor dismount failed.", ex.ToString());
                return MotorVehicleRideFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        BridgeFeatureStatus IMotorVehicleApi.GetStatus(string uniqueId)
        {
            if (string.IsNullOrWhiteSpace(uniqueId))
                return new BridgeFeatureStatus(motorVehicleHooksInstalled ? "experimental" : "pending-hook", "Motor API exposes original motor state plus DTMAPI-managed second motor registration, summon, ride, dismount, and instance-scoped clone appearance. Second motor riding uses private AgentControllerState.motorController routing and remains experimental.");
            bool registered = secondMotors.Values.Any(v => v.OwnerUniqueId.Equals(uniqueId, StringComparison.OrdinalIgnoreCase));
            if (!registered)
                return new BridgeFeatureStatus(motorVehicleHooksInstalled ? "available" : "pending-hook", "No second motor is registered for this owner.");
            return new BridgeFeatureStatus(motorVehicleHooksInstalled ? "configured-experimental" : "configured-pending-hook", motorVehicleHooksInstalled ? "Second motor is registered; key/use, riding, and scoped clone-appearance hooks are installed but require third-save smoke evidence." : "Second motor is registered; waiting for motor hook targets.");
        }

        internal MotorVehicleSummonResult UseRegisteredSecondMotorKeyForSmoke(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || !secondMotorsByKeyItemId.TryGetValue(itemId, out SecondMotorRuntime vehicle))
            {
                return MotorVehicleSummonFailed(new MotorVehicleSummonResult
                {
                    VehicleId = itemId ?? string.Empty,
                    DisplayName = "Second Motor",
                    Before = new MotorVehicleState { VehicleId = itemId ?? string.Empty }
                }, "not-registered", "No DTMAPI second motor key is registered with this item id.");
            }

            var result = new MotorVehicleSummonResult
            {
                VehicleId = vehicle.Options.VehicleId,
                DisplayName = vehicle.Options.DisplayName,
                Before = BuildSecondMotorState(vehicle, "before-smoke-key-use")
            };

            if (!TryGenerateNativeItem(itemId, 1, out object? item, out string itemReason, out string itemMessage))
                return MotorVehicleSummonFailed(result, itemReason, itemMessage);

            try
            {
                MethodInfo? onUse = FindMethodInHierarchy(item!.GetType(), "OnUse", 0);
                if (onUse == null)
                    return MotorVehicleSummonFailed(result, "missing-on-use", "Generated native item " + item.GetType().FullName + " does not expose OnUse().");

                onUse.Invoke(item, null);
                result.After = BuildSecondMotorState(vehicle, "after-smoke-key-use");
                if (!result.After.IsVisible)
                    return MotorVehicleSummonFailed(result, FirstText(vehicle.LastFailureReason, "key-hook-not-observed"), FirstText(vehicle.LastMessage, "Generated second motor key OnUse did not leave the DTMAPI second motor visible; ItemMotorKey.OnUse prefix may not have intercepted the key."));

                result.Success = true;
                result.Message = "Native ItemFactory generated " + item.GetType().FullName + " and OnUse triggered the registered second motor key path.";
                runtime.SetHookStatus("Smoke.VehicleSecondMotorKey", "verified", "ItemFactory.GenerateItem -> ItemMotorKey.OnUse prefix", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Second motor key smoke use failed.", ex.ToString());
                return MotorVehicleSummonFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        internal string ProbeSecondMotorAppearanceForSmoke(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId) || !secondMotors.TryGetValue(vehicleId, out SecondMotorRuntime vehicle))
                return "appearanceIsolated=False, reason=not-registered";

            if (!EnsureSecondMotorInstance(vehicle, out string reason, out string message))
                return "appearanceIsolated=False, reason=" + reason + ", message=" + message;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? originalMotor = ReadStaticMember(dolocApi, "Motor");
            CountSecondMotorScopedTintRenderers(originalMotor, out int originalTotal, out int originalTinted, out int originalSkippedDriver);
            CountSecondMotorScopedTintRenderers(vehicle.Controller, out int secondTotal, out int secondTinted, out int secondSkippedDriver);

            bool customExpected = !vehicle.Options.UseOriginalMotorVisuals;
            bool isolated = !customExpected || (originalTinted == 0 && secondTinted > 0);
            string summary = "appearanceIsolated=" + isolated +
                ", originalScopedTint=" + originalTinted + "/" + originalTotal +
                ", originalSkippedDriver=" + originalSkippedDriver +
                ", secondScopedTint=" + secondTinted + "/" + secondTotal +
                ", secondSkippedDriver=" + secondSkippedDriver +
                ", " + FirstText(vehicle.AppearanceSummary, "appearance=not-applied");
            runtime.RuntimeMonitor.Log("Smoke exercise VehicleSecondMotor appearance probe " + summary);
            runtime.SetHookStatus("Vehicle.SecondMotorAppearance", isolated ? "experimental" : "failed", "DTMAPI cloned MotorController SpriteRenderer.color", summary);
            return summary;
        }

        internal MotorVehicleSummonResult UseOriginalMotorKeyForSmoke(string itemId)
        {
            const string originalVehicleId = "doloc.original_motor";
            var result = new MotorVehicleSummonResult
            {
                VehicleId = originalVehicleId,
                DisplayName = "Original Motor",
                Before = BuildOriginalMotorState("before-smoke-original-key-use")
            };

            if (string.IsNullOrWhiteSpace(itemId))
                return MotorVehicleSummonFailed(result, "missing-item-id", "Original motor key item id is required.");
            if (secondMotorsByKeyItemId.ContainsKey(itemId))
                return MotorVehicleSummonFailed(result, "registered-second-key", "Item " + itemId + " is registered as a DTMAPI second motor key, not the original motor key.");
            if (!TryGenerateNativeItem(itemId, 1, out object? item, out string itemReason, out string itemMessage))
                return MotorVehicleSummonFailed(result, itemReason, itemMessage);

            try
            {
                MethodInfo? onUse = FindMethodInHierarchy(item!.GetType(), "OnUse", 0);
                if (onUse == null)
                    return MotorVehicleSummonFailed(result, "missing-on-use", "Generated native item " + item.GetType().FullName + " does not expose OnUse().");

                onUse.Invoke(item, null);
                result.After = BuildOriginalMotorState("after-smoke-original-key-use");
                if (!result.After.IsVisible)
                    return MotorVehicleSummonFailed(result, "original-key-not-visible", "Generated original motor key OnUse did not leave the original motor visible.");

                result.Success = true;
                result.Message = "Native ItemFactory generated " + item.GetType().FullName + " and OnUse followed the original motor key path.";
                runtime.SetHookStatus("Smoke.VehicleOriginalMotorKey", "verified", "ItemFactory.GenerateItem -> native ItemMotorKey.OnUse", result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Original motor key smoke use failed.", ex.ToString());
                return MotorVehicleSummonFailed(result, ex.GetType().Name, ex.Message);
            }
        }

        internal bool HandleMotorKeyUse(object item)
        {
            string itemId = ReadStringMember(item, "name");
            if (string.IsNullOrWhiteSpace(itemId) || !secondMotorsByKeyItemId.TryGetValue(itemId, out SecondMotorRuntime vehicle))
                return true;

            if (!IsOwnerOfficiallyEnabled(vehicle.OwnerUniqueId, out string enablementMessage))
            {
                string cleanup = CleanupSecondMotorRuntime(vehicle, "disabled key use", destroyGameObject: true);
                vehicle.LastFailureReason = "source-disabled";
                vehicle.LastMessage = enablementMessage + " cleanup={" + cleanup + "}";
                runtime.RuntimeMonitor.Log("Second motor key blocked because owner source is disabled item=" + itemId + " message=" + vehicle.LastMessage);
                runtime.SetHookStatus("Vehicle.SecondMotorKeyFailed", "failed", "ItemMotorKey.OnUse prefix + official enablement", vehicle.LastMessage);
                return false;
            }

            MotorVehicleSummonResult result = SummonSecondMotor(null!, vehicle, "key:" + itemId);
            runtime.RuntimeMonitor.Log("Second motor key intercepted item=" + itemId + " success=" + result.Success + " reason=" + result.FailureReason + " message=" + result.Message);
            runtime.SetHookStatus(result.Success ? "Vehicle.SecondMotorKey" : "Vehicle.SecondMotorKeyFailed", result.Success ? "experimental" : "failed", "ItemMotorKey.OnUse prefix", result.Message);
            return false;
        }

        internal bool HandleMotorInteract(object interactable)
        {
            if (!TryResolveSecondMotorFromInteractable(interactable, out SecondMotorRuntime vehicle))
                return true;

            bool success = TryStartSecondMotorRide(vehicle, "interact");
            runtime.SetHookStatus(success ? "Vehicle.SecondMotorRide" : "Vehicle.SecondMotorRideFailed", success ? "experimental" : "failed", "MotorInteractable.OnInteract prefix", vehicle.LastMessage);
            return false;
        }

        internal void NotifyMotorGetOn(object agentControllerState)
        {
            object? current = ReadMember(agentControllerState, "motorController");
            if (activeSecondMotor != null && current != null && ReferenceEquals(current, activeSecondMotor.Controller))
            {
                activeSecondMotor.IsRiding = true;
                MotorVehicleState state = BuildSecondMotorState(activeSecondMotor, "ride-on");
                runtime.RuntimeMonitor.Log("Second motor ride-on observed vehicle=" + activeSecondMotor.Options.VehicleId + " room=" + state.RoomId + ".");
                RaiseVehicleEvent("ride-on", activeSecondMotor.Options.VehicleId, state, "Second motor ride-on observed.");
                return;
            }

            MotorVehicleState original = BuildOriginalMotorState("ride-on");
            RaiseVehicleEvent("ride-on", original.VehicleId, original, "Original motor ride-on observed.");
        }

        internal void NotifyMotorGetOff(object agentControllerState)
        {
            if (activeSecondMotor == null)
            {
                MotorVehicleState original = BuildOriginalMotorState("ride-off");
                RaiseVehicleEvent("ride-off", original.VehicleId, original, "Original motor ride-off observed.");
                return;
            }

            SecondMotorRuntime vehicle = activeSecondMotor;
            vehicle.IsRiding = false;
            RestoreOriginalAgentMotorController(agentControllerState);
            RestoreOriginalMotorSnapshot(clearSnapshot: true);
            activeSecondMotor = null;
            MotorVehicleState state = BuildSecondMotorState(vehicle, "ride-off");
            string message = "Second motor ride-off restored original AgentControllerState.motorController and original motor snapshot.";
            vehicle.LastMessage = message;
            runtime.RuntimeMonitor.Log(message + " vehicle=" + vehicle.Options.VehicleId + ".");
            runtime.SetHookStatus("Vehicle.SecondMotorRideOff", "experimental", "AgentControllerState.GetOffMotor postfix", message);
            RaiseVehicleEvent("ride-off", vehicle.Options.VehicleId, state, message);
        }

        internal void ApplySecondMotorTuningForFixedUpdate(object motorController)
        {
            if (!secondMotorControllers.Contains(motorController))
                return;
            SecondMotorRuntime? vehicle = ResolveSecondMotorByController(motorController);
            if (vehicle == null || activeSecondMotorTuningSnapshot != null)
                return;

            object? globalParameter = ReadStaticMember(ResolveType("DolocAPI, Assembly-CSharp"), "GlobalParameter");
            if (globalParameter == null)
                return;

            double multiplier = Math.Max(0.1, vehicle.Options.SpeedMultiplier);
            activeSecondMotorTuningSnapshot = CaptureGlobalMotorTuning(globalParameter);
            WriteFloatMember(globalParameter, "MotorHorizontalAcceleration", (float)(activeSecondMotorTuningSnapshot.MotorHorizontalAcceleration * multiplier));
            WriteFloatMember(globalParameter, "MotorHorizontalRevertAcceleration", (float)(activeSecondMotorTuningSnapshot.MotorHorizontalRevertAcceleration * multiplier));
            WriteFloatMember(globalParameter, "MotorHorizontalDeceleration", (float)(activeSecondMotorTuningSnapshot.MotorHorizontalDeceleration * multiplier));
            WriteFloatMember(globalParameter, "MotorHorizontalMaxSpeed", (float)(activeSecondMotorTuningSnapshot.MotorHorizontalMaxSpeed * multiplier));
        }

        internal void RestoreSecondMotorTuningAfterFixedUpdate(object motorController)
        {
            if (activeSecondMotorTuningSnapshot == null)
                return;
            object? globalParameter = ReadStaticMember(ResolveType("DolocAPI, Assembly-CSharp"), "GlobalParameter");
            if (globalParameter != null)
                RestoreGlobalMotorTuning(globalParameter, activeSecondMotorTuningSnapshot);
            activeSecondMotorTuningSnapshot = null;
        }

        internal void NotifyOriginalMotorUnlocked()
        {
            MotorVehicleState state = BuildOriginalMotorState("unlock-hook");
            RaiseVehicleEvent("unlock", state.VehicleId, state, "DolocAPI.UnlockMotor completed.");
        }

        internal void NotifyOriginalMotorPositionChanged(object? room, object? position)
        {
            if (activeSecondMotor != null && !restoringOriginalMotorSnapshot)
            {
                SecondMotorRuntime vehicle = activeSecondMotor;
                vehicle.Room = room ?? ReadStaticMember(ResolveType("DolocAPI, Assembly-CSharp"), "CurrentRoom");
                vehicle.LastPosition = position;
                if (vehicle.Controller != null && position != null)
                    SetMotorControllerPosition(vehicle.Controller, position);
                RestoreOriginalMotorSnapshot(clearSnapshot: false);
                MotorVehicleState secondState = BuildSecondMotorState(vehicle, "position-hook-second-ride");
                string redirected = "Native SetMotorPosition observed while riding second motor; redirected position state to DTMAPI clone and restored original motor snapshot.";
                vehicle.LastMessage = redirected;
                runtime.RuntimeMonitor.Log(redirected + " vehicle=" + vehicle.Options.VehicleId + " room=" + secondState.RoomId + " x=" + FormatRatio(secondState.X) + " y=" + FormatRatio(secondState.Y) + ".");
                runtime.SetHookStatus("Vehicle.SecondMotorMapTransition", "experimental", "DolocAPI.SetMotorPosition postfix", redirected);
                RaiseVehicleEvent("position", vehicle.Options.VehicleId, secondState, redirected);
                return;
            }

            MotorVehicleState state = BuildOriginalMotorState("position-hook");
            RaiseVehicleEvent("position", state.VehicleId, state, "DolocAPI.SetMotorPosition completed.");
        }

        internal void NotifyEnterRoomForActiveSecondMotor(object? roomOrId, object? position, bool success)
        {
            if (!success || activeSecondMotor == null || position == null)
                return;

            object? targetRoom = ResolveRoomFromEnterRoomArgument(roomOrId);
            string targetRoomId = ReadStringMemberOrEmpty(targetRoom, "RoomId");
            if (string.IsNullOrWhiteSpace(targetRoomId))
                targetRoomId = roomOrId as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(targetRoomId))
                return;

            activeSecondMotor.PendingTransitionRoom = targetRoom;
            activeSecondMotor.PendingTransitionRoomId = targetRoomId;
            activeSecondMotor.PendingTransitionPosition = position;
            string message = "Second motor room transition target captured room=" + targetRoomId +
                " x=" + FormatRatio(ReadVectorComponent(position, "x")) +
                " y=" + FormatRatio(ReadVectorComponent(position, "y")) + ".";
            activeSecondMotor.LastMessage = message;
            runtime.SetHookStatus("Vehicle.SecondMotorMapTransition", "experimental", "DolocAPI.EnterRoom postfix", message);
        }

        private static object? ResolveRoomFromEnterRoomArgument(object? roomOrId)
        {
            if (roomOrId == null)
                return null;
            if (!string.IsNullOrWhiteSpace(ReadStringMemberOrEmpty(roomOrId, "RoomId")))
                return roomOrId;
            string roomId = roomOrId as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(roomId))
                return null;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? queryRoom = dolocApi?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    ParameterInfo[] parameters = m.GetParameters();
                    return m.Name == "QueryRoom" &&
                        parameters.Length == 2 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].IsOut;
                });
            if (queryRoom == null)
                return null;
            object?[] args = { roomId, null };
            object? ok = queryRoom.Invoke(null, args);
            return ok is bool success && success ? args[1] : null;
        }

        private void UpdateActiveSecondMotorRoomSnapshot()
        {
            if (activeSecondMotor == null)
                return;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
            string previousRoomId = ReadStringMemberOrEmpty(activeSecondMotor.Room, "RoomId");
            string currentRoomId = ReadStringMemberOrEmpty(currentRoom, "RoomId");
            bool roomChanged = !string.IsNullOrWhiteSpace(currentRoomId) &&
                !currentRoomId.Equals(previousRoomId, StringComparison.OrdinalIgnoreCase);
            bool appliedPendingTransition = false;
            if (activeSecondMotor.PendingTransitionPosition != null &&
                !string.IsNullOrWhiteSpace(activeSecondMotor.PendingTransitionRoomId) &&
                activeSecondMotor.PendingTransitionRoomId.Equals(currentRoomId, StringComparison.OrdinalIgnoreCase))
            {
                activeSecondMotor.Room = currentRoom ?? activeSecondMotor.PendingTransitionRoom;
                if (activeSecondMotor.Controller != null)
                {
                    SetMotorControllerPosition(activeSecondMotor.Controller, activeSecondMotor.PendingTransitionPosition);
                    activeSecondMotor.LastPosition = activeSecondMotor.PendingTransitionPosition;
                }
                object? agentController = GetAgentController(dolocApi);
                object? body = agentController == null ? null : ReadMember(agentController, "body");
                if (body != null)
                    SetMotorControllerPosition(body, activeSecondMotor.PendingTransitionPosition);
                string appliedMessage = "Second motor room transition applied room=" + currentRoomId +
                    " x=" + FormatRatio(ReadVectorComponent(activeSecondMotor.PendingTransitionPosition, "x")) +
                    " y=" + FormatRatio(ReadVectorComponent(activeSecondMotor.PendingTransitionPosition, "y")) + ".";
                activeSecondMotor.LastMessage = appliedMessage;
                runtime.SetHookStatus("Vehicle.SecondMotorMapTransition", "experimental", "DolocAPI.EnterRoom postfix + active ride sync", appliedMessage);
                activeSecondMotor.PendingTransitionRoom = null;
                activeSecondMotor.PendingTransitionRoomId = string.Empty;
                activeSecondMotor.PendingTransitionPosition = null;
                appliedPendingTransition = true;
            }
            if (!appliedPendingTransition && currentRoom != null)
                activeSecondMotor.Room = currentRoom;
            if (!appliedPendingTransition && activeSecondMotor.Controller != null && roomChanged)
            {
                object? agentPosition = ReadStaticMember(dolocApi, "AgentPosition");
                object? position2d = agentPosition == null ? null : CreateUnityVector2(ReadVectorComponent(agentPosition, "x"), ReadVectorComponent(agentPosition, "y"));
                if (position2d != null)
                {
                    SetMotorControllerPosition(activeSecondMotor.Controller, position2d);
                    activeSecondMotor.LastPosition = position2d;
                }
            }
            else if (!appliedPendingTransition && activeSecondMotor.Controller != null)
            {
                activeSecondMotor.LastPosition = ReadMember(activeSecondMotor.Controller, "position2d") ?? ReadMember(activeSecondMotor.Controller, "position") ?? activeSecondMotor.LastPosition;
            }
            RestoreOriginalMotorSnapshot(clearSnapshot: false);
            MirrorOriginalMotorTransformForSecondMotorAgentPosition(dolocApi);
        }

        private void MirrorOriginalMotorTransformForSecondMotorAgentPosition(Type? dolocApi)
        {
            if (activeSecondMotor?.Controller == null || originalMotorSnapshotBeforeSecondRide == null)
                return;

            string activeRoomId = ReadStringMemberOrEmpty(activeSecondMotor.Room, "RoomId");
            string originalRoomId = ReadStringMemberOrEmpty(originalMotorSnapshotBeforeSecondRide.Room, "RoomId");
            if (string.IsNullOrWhiteSpace(activeRoomId) ||
                activeRoomId.Equals(originalRoomId, StringComparison.OrdinalIgnoreCase))
                return;

            object? originalMotor = ReadStaticMember(dolocApi, "Motor");
            if (originalMotor == null || ReferenceEquals(originalMotor, activeSecondMotor.Controller))
                return;

            object? position = ReadMember(activeSecondMotor.Controller, "position2d") ?? ReadMember(activeSecondMotor.Controller, "position");
            if (position == null)
                return;

            SetMotorControllerPosition(originalMotor, position);
        }

        internal string DecorateFishRoeTitle(object item, string current)
        {
            if (!TryGetFishRoeId(item, out string fishId))
                return current ?? string.Empty;

            string result = current ?? string.Empty;
            foreach (KeyValuePair<string, FishRoeTooltipOptions> entry in fishRoeOptions)
            {
                FishRoeTooltipOptions options = entry.Value ?? new FishRoeTooltipOptions();
                if (!options.Enabled || !options.LabelFishRoeTitle)
                    continue;
                if (!TryLookupFishRoe(entry.Key, fishId, out FishRoeDisplayInfo info))
                    continue;

                string fishTitle = FirstText(info.FishTitle, info.FishId, fishId);
                string marker = " (" + fishTitle + ")";
                if (result.IndexOf(marker, StringComparison.Ordinal) >= 0)
                    continue;
                if (string.IsNullOrWhiteSpace(result))
                    result = FirstText(info.RoeTitle, "Fish roe");
                result += marker;
                LogOnce(loggedFishRoeApplications, entry.Key + ":title:" + fishId, "Fish roe title hook applied by " + entry.Key + " for " + fishId + ".");
            }
            return result;
        }

        internal string DecorateFishRoeDetail(object item, string current)
        {
            if (!TryGetFishRoeId(item, out string fishId))
                return current ?? string.Empty;

            string result = current ?? string.Empty;
            foreach (KeyValuePair<string, FishRoeTooltipOptions> entry in fishRoeOptions)
            {
                FishRoeTooltipOptions options = entry.Value ?? new FishRoeTooltipOptions();
                if (!options.Enabled || !options.LabelFishRoeDetails)
                    continue;
                if (!TryLookupFishRoe(entry.Key, fishId, out FishRoeDisplayInfo info))
                    continue;

                string fishTitle = FirstText(info.FishTitle, info.FishId, fishId);
                string marker = "Hatches: " + fishTitle;
                if (result.IndexOf(marker, StringComparison.Ordinal) >= 0)
                    continue;

                var parts = new List<string> { marker };
                if (!string.IsNullOrWhiteSpace(info.IncubateText))
                    parts.Add("Incubate: " + info.IncubateText);
                if (!string.IsNullOrWhiteSpace(info.GrowText))
                    parts.Add("Grow: " + info.GrowText);
                if (!string.IsNullOrWhiteSpace(info.ParentSummary))
                    parts.Add(info.ParentSummary);

                string extra = string.Join("; ", parts.ToArray());
                result = string.IsNullOrWhiteSpace(result) ? extra : result.TrimEnd() + Environment.NewLine + extra;
                LogOnce(loggedFishRoeApplications, entry.Key + ":detail:" + fishId, "Fish roe detail hook applied by " + entry.Key + " for " + fishId + ".");
            }
            return result;
        }

        internal void DecorateAnimalFullInfoData(object data, object animal)
        {
            if (data == null || animal == null)
                return;
            IReadOnlyList<AnimalProgressInfo> progressItems = BuildAnimalProgressList(animal);
            if (progressItems.Count == 0)
                return;

            var renderRows = new List<AnimalProgressRenderRow>();
            foreach (KeyValuePair<string, AnimalHusbandryProgressOptions> entry in animalOptions)
            {
                AnimalHusbandryProgressOptions options = entry.Value ?? new AnimalHusbandryProgressOptions();
                if (!options.Enabled)
                    continue;

                FieldInfo? stateDescription = data.GetType().GetField("stateDescription", BindingFlags.Public | BindingFlags.Instance);
                string current = stateDescription == null ? string.Empty : stateDescription.GetValue(data) as string ?? string.Empty;
                bool changed = false;
                foreach (AnimalProgressInfo progress in progressItems.OrderByDescending(p => p.Progress).ThenBy(p => p.OutputTitle, StringComparer.OrdinalIgnoreCase))
                {
                    string line = progress.OutputTitle + " " + BuildAnimalProgressBar(progress, options.ProgressColor) + " " + progress.Current + "/" + progress.Threshold;
                    if (stateDescription != null && current.IndexOf(line, StringComparison.Ordinal) < 0)
                    {
                        current = InsertAnimalProgressLine(current, line);
                        changed = true;
                    }
                    renderRows.Add(new AnimalProgressRenderRow
                    {
                        OwnerId = entry.Key,
                        OutputTitle = progress.OutputTitle,
                        Current = progress.Current,
                        Threshold = progress.Threshold,
                        Progress = progress.Progress,
                        Color = options.ProgressColor
                    });
                    LogOnce(loggedAnimalApplications, entry.Key + ":" + progress.AnimalId + ":" + progress.OutputId, "Animal viewer progress hook applied by " + entry.Key + " for " + progress.AnimalId + "/" + progress.OutputId + " current=" + progress.Current + " threshold=" + progress.Threshold + ".");
                }

                if (changed && stateDescription != null)
                    stateDescription.SetValue(data, current);
            }

            if (renderRows.Count > 0)
                animalProgressRowsByData[data] = renderRows;
        }

        internal bool RenderAnimalProgressOverlay(object viewer, object data)
        {
            latestAnimalProgressOverlaySummary = string.Empty;
            if (viewer == null || data == null)
                return false;
            if (!animalProgressRowsByData.TryGetValue(data, out IReadOnlyList<AnimalProgressRenderRow>? rows) || rows.Count == 0)
                return false;

            try
            {
                object? moodBar = ReadMember(viewer, "moodBar");
                object? moodGameObject = moodBar == null ? null : ReadMember(moodBar, "gameObject");
                object? moodTransform = moodGameObject == null ? null : ReadMember(moodGameObject, "transform");
                object? parent = moodTransform == null ? null : ReadMember(moodTransform, "parent");
                if (moodGameObject == null || moodTransform == null || parent == null)
                    return false;

                ClearAnimalProgressOverlay(parent);

                int rendered = 0;
                var textPatchSummaries = new List<string>();
                foreach (AnimalProgressRenderRow row in rows.Take(3))
                {
                    object? clone = CloneUnityObject(moodGameObject);
                    if (clone == null)
                        continue;

                    SetMemberValue(clone, "name", "DTMAPI.AnimalProduceProgress." + rendered);
                    SetActive(clone, false);
                    object? cloneTransform = ReadMember(clone, "transform");
                    if (cloneTransform == null)
                        continue;

                    SetParent(cloneTransform, parent, worldPositionStays: false);
                    PositionAnimalProgressRow(moodTransform, cloneTransform, rendered);
                    activeAnimalProgressOverlayObjects.Add(clone);

                    object? progressBar = GetComponent(clone, moodBar!.GetType());
                    if (progressBar == null)
                        continue;
                    string progressText = row.Current + "/" + row.Threshold;
                    string textPatchSummary = SetAnimalProgressTextsFromChildren(clone, row.OutputTitle, progressText);
                    MethodInfo? setTitle = progressBar.GetType().GetMethod("SetTitle", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
                    MethodInfo? setProgress = progressBar.GetType().GetMethod("SetProgress", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(float), typeof(string) }, null);
                    setTitle?.Invoke(progressBar, new object[] { row.OutputTitle });
                    setProgress?.Invoke(progressBar, new object[] { (float)Math.Max(0, Math.Min(1, row.Progress)), progressText });
                    SetUnityText(ReadMember(progressBar, "txtTitle"), row.OutputTitle);
                    SetUnityText(ReadMember(progressBar, "txtProgress"), progressText);
                    SetAnimalProgressTextsFromChildren(clone, row.OutputTitle, progressText);
                    textPatchSummaries.Add(textPatchSummary);

                    object? progressMask = ReadMember(progressBar, "progressMask");
                    object? color = CreateUnityColor(row.Color);
                    if (progressMask != null && color != null)
                        SetMemberValue(progressMask, "color", color);

                    SetActive(clone, true);
                    rendered++;
                }

                if (rendered <= 0)
                    return false;

                activeAnimalProgressOverlayRows.Clear();
                activeAnimalProgressOverlayRows.AddRange(rows.Take(rendered));
                lastAnimalProgressOverlayRefreshAt = DateTimeOffset.MinValue;
                RefreshAnimalProgressOverlayTexts(force: true);

                latestAnimalProgressOverlaySummary = "rows=" + rendered + ", " + string.Join("; ", rows.Take(rendered).Select(r => r.OutputTitle + " " + r.Current + "/" + r.Threshold).ToArray());
                runtime.RuntimeMonitor.Log("Animal viewer progress native-like UI overlay rendered " + latestAnimalProgressOverlaySummary + "; " + string.Join("; ", textPatchSummaries.ToArray()) + ".");
                runtime.SetHookStatus("Smoke.AnimalViewerProgressUi", "verified", "AnimalViewer.Show postfix + cloned native ProgressBar", latestAnimalProgressOverlaySummary);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Animal progress UI overlay render failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AnimalViewerProgressUi", "failed", "AnimalViewer.Show postfix + cloned native ProgressBar", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        internal bool RecordAnimalViewerUiEvidence(object viewer, object data)
        {
            if (viewer == null || data == null || animalViewerUiEvidenceRecorded)
                return false;

            string stateDescription = ReadStringMember(data, "stateDescription");
            if (string.IsNullOrWhiteSpace(stateDescription))
                return false;
            if (!ReadBoolMember(data, "notEmpty", true) || !ReadBoolMember(data, "visible", true))
                return false;
            if (!TryFindAnimalProgressMarker(stateDescription, out string ownerId, out string label))
                return false;

            animalViewerUiEvidenceRecorded = true;
            string title = ReadStringMember(data, "title");
            string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
            string evidenceDir = Path.Combine(runtime.Paths.EvidencePath, "ANIMAL-001", timestamp);
            Directory.CreateDirectory(evidenceDir);
            latestAnimalViewerEvidenceDir = evidenceDir;

            string screenshotPath = Path.Combine(evidenceDir, "animal-viewer-ui.png");
            bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
            string summaryPath = Path.Combine(evidenceDir, "summary.txt");
            File.WriteAllText(summaryPath,
                "Captured=" + DateTimeOffset.Now.ToString("o") + Environment.NewLine +
                "Owner=" + ownerId + Environment.NewLine +
                "Marker=" + label + Environment.NewLine +
                "Title=" + title + Environment.NewLine +
                "ViewerType=" + viewer.GetType().FullName + Environment.NewLine +
                "DataType=" + data.GetType().FullName + Environment.NewLine +
                "ScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                "Screenshot=" + screenshotPath + Environment.NewLine +
                "Overlay=" + latestAnimalProgressOverlaySummary + Environment.NewLine +
                "StateDescription=" + stateDescription.Replace(Environment.NewLine, " | ") + Environment.NewLine);

            runtime.RuntimeMonitor.Log("Animal viewer UI evidence OK owner=" + ownerId + " title=" + title + " marker=" + label + " overlay=" + latestAnimalProgressOverlaySummary + " screenshot=" + (screenshotRequested ? screenshotPath : "unavailable") + " stateDescription=" + stateDescription.Replace(Environment.NewLine, " | "));
            runtime.SetHookStatus("Animals.ViewerRendering", "verified", "Harmony Postfix: AnimalFullInfoData(Animal) + AnimalViewer.Show + AnimalPanel.RefreshViewer", "Real animal viewer UI showed visible progress text. Evidence=" + evidenceDir);
            runtime.SetHookStatus("Smoke.AnimalPanelUi", "verified", "DolocAPI.EnterUI(AnimalPanelUiState) + AnimalPanel.RefreshViewer", "Opened official AnimalPanel UI and observed progress text. Evidence=" + evidenceDir);
            runtime.SetHookStatus("Smoke.AnimalViewerUi", "verified", "AnimalViewer.Show", "Observed animal progress text in the real animal viewer UI. Evidence=" + evidenceDir);
            return true;
        }

        internal bool CaptureDelayedAnimalViewerUiEvidenceScreenshot()
        {
            if (animalViewerUiDelayedScreenshotRecorded || string.IsNullOrWhiteSpace(latestAnimalViewerEvidenceDir))
                return false;

            animalViewerUiDelayedScreenshotRecorded = true;
            string screenshotPath = Path.Combine(latestAnimalViewerEvidenceDir!, "animal-viewer-ui-delayed.png");
            bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
            string summaryPath = Path.Combine(latestAnimalViewerEvidenceDir!, "summary.txt");
            try
            {
                File.AppendAllText(summaryPath,
                    "DelayedCaptured=" + DateTimeOffset.Now.ToString("o") + Environment.NewLine +
                    "DelayedScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                    "DelayedScreenshot=" + screenshotPath + Environment.NewLine);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to append delayed animal UI screenshot summary.", ex.ToString());
            }

            runtime.RuntimeMonitor.Log("Animal viewer UI delayed screenshot " + (screenshotRequested ? "OK" : "unavailable") + " screenshot=" + screenshotPath + ".");
            runtime.SetHookStatus("Smoke.AnimalViewerUiScreenshot", screenshotRequested ? "verified" : "pending", "UnityEngine.ScreenCapture.CaptureScreenshot", "Delayed animal viewer screenshot=" + (screenshotRequested ? screenshotPath : "unavailable") + ".");
            return screenshotRequested;
        }

        internal bool RecordAnimalPanelUiEvidence(object panel, int index)
        {
            if (panel == null || animalViewerUiEvidenceRecorded)
                return false;

            object? currentDatas = ReadMember(panel, "currentDatas") ?? ReadMember(panel, "<currentDatas>k__BackingField");
            object? data = null;
            if (currentDatas is Array array && index >= 0 && index < array.Length)
                data = array.GetValue(index);
            else if (currentDatas is IEnumerable enumerable)
            {
                int currentIndex = 0;
                foreach (object item in enumerable)
                {
                    if (currentIndex == index)
                    {
                        data = item;
                        break;
                    }
                    currentIndex++;
                }
            }

            if (!animalPanelUiProbeLogged)
            {
                animalPanelUiProbeLogged = true;
                runtime.RuntimeMonitor.Log("Animal panel UI evidence probe index=" + index + " panel=" + panel.GetType().FullName + " currentDatas=" + (currentDatas?.GetType().FullName ?? "null") + " data=" + (data?.GetType().FullName ?? "null") + ".");
            }

            if (data != null && RecordAnimalViewerUiEvidence(panel, data))
                return true;

            if (currentDatas is Array scanArray)
            {
                foreach (object? item in scanArray)
                {
                    if (item != null && RecordAnimalViewerUiEvidence(panel, item))
                        return true;
                }
            }

            return false;
        }

        internal bool ApplyOneActionToolHit(object toolCollider, object collider)
        {
            if (toolCollider == null || collider == null || actionOptions.Count == 0)
                return false;

            object? resource = TryGetDungeonResourceFromCollider(collider);
            if (resource == null || IsResourceRemoved(resource))
                return false;

            int currentHealth = ReadIntMember(resource, "currentHealth", 0);
            if (currentHealth <= 0)
                return false;

            if (!TryFindActionPolicy(resource, out string ownerId, out ActionCompletionOptions options))
                return false;

            object? currentTool = ReadMember(toolCollider, "currentTool");
            if (currentTool == null)
                return false;

            object? hitPoint = resource.GetType().GetProperty("PositionCenter", BindingFlags.Public | BindingFlags.Instance)?.GetValue(resource);
            if (hitPoint == null)
                return false;

            Type? resourceFellDataType = ResolveType("DolocTown.ResourceFellData, Assembly-CSharp");
            if (resourceFellDataType == null)
                return false;

            if (!TryBuildValidatedResourceFellData(resourceFellDataType, resource, currentTool, hitPoint, out object? nativeFellData, out string rejectReason))
            {
                string skippedResource = GetResourceName(resource);
                string skippedTool = ReadStringMember(currentTool, "name");
                string skippedToolType = ReadMember(currentTool, "ToolType")?.ToString() ?? "unknown";
                LogOnce(loggedActionApplications, "skip:" + ownerId + ":" + skippedResource + ":" + skippedTool + ":" + rejectReason, "One-action tool hook skipped resource " + skippedResource + " for " + ownerId + " tool=" + skippedTool + " toolType=" + skippedToolType + " reason=" + rejectReason + ".");
                return false;
            }

            try
            {
                int toolLevel = ReadIntMember(nativeFellData!, "toolLevel", 0);
                int nativeDamage = Math.Max(1, ReadIntMember(nativeFellData!, "Damage", currentHealth));
                bool levelMatch = ReadBoolMember(nativeFellData!, "levelMatch", false);
                bool shouldCounterBack = ReadBoolMember(nativeFellData!, "shouldCounterBack", true);
                bool shouldRaiseToolTip = ReadBoolMember(nativeFellData!, "shouldRaiseToolTip", true);
                string overrideSpawnLut = ReadMember(nativeFellData!, "overrideSpawnLut") as string ?? string.Empty;
                object? extraItems = ReadMember(nativeFellData!, "extraItems");
                int requiredExtraHits = Math.Max(1, (int)Math.Ceiling(currentHealth / (double)nativeDamage));
                int paidExtraHits = CostOneActionExtraToolHits(requiredExtraHits, out string energyReason);
                if (paidExtraHits <= 0)
                {
                    string skippedResource = GetResourceName(resource);
                    LogOnce(loggedActionApplications, "energy-skip:" + ownerId + ":" + skippedResource, "One-action tool hook skipped resource " + skippedResource + " for " + ownerId + " reason=" + energyReason + ".");
                    runtime.SetHookStatus("Actions.OneActionComplete", "experimental", "DolocAPI.HasEnoughEnergyForUsingTool/CostToolEnergy", "Skipped extra one-action hits because " + energyReason + ".");
                    return false;
                }

                int damageToApply = Math.Min(currentHealth, paidExtraHits * nativeDamage);
                object? fellData = Activator.CreateInstance(resourceFellDataType, new object?[] { levelMatch, toolLevel, damageToApply, hitPoint, shouldCounterBack, shouldRaiseToolTip, overrideSpawnLut, extraItems });
                if (fellData == null)
                    return false;
                MethodInfo? fell = resource.GetType().GetMethod("_Fell", BindingFlags.Public | BindingFlags.Instance);
                if (fell == null)
                    return false;
                fell.Invoke(resource, new object?[] { fellData });
                string resourceName = GetResourceName(resource);
                string resourceClass = GetResourceClass(resource);
                string toolName = ReadStringMember(currentTool, "name");
                string toolType = ReadMember(currentTool, "ToolType")?.ToString() ?? "unknown";
                int afterHealth = ReadIntMember(resource, "currentHealth", 0);
                bool removed = IsResourceRemoved(resource);
                string oilDropSummary = TryRollOilDropFromCoal(resourceName, removed, "one-action-tool-hit");
                LastOneActionApplicationSummary = "owner=" + ownerId + ", resource=" + resourceName + ", class=" + resourceClass + ", tool=" + toolName + ", toolType=" + toolType + ", toolLevel=" + toolLevel + ", nativeDamage=" + nativeDamage + ", paidExtraHits=" + paidExtraHits + "/" + requiredExtraHits + ", damage=" + damageToApply + ", healthAfter=" + afterHealth + ", removed=" + removed;
                if (!string.IsNullOrWhiteSpace(oilDropSummary))
                    LastOneActionApplicationSummary += ", " + oilDropSummary;
                OneActionApplicationCount++;
                string key = ownerId + ":" + resourceName;
                if (options.VerboseLogging || !loggedActionApplications.Contains(key))
                    runtime.RuntimeMonitor.Log("One-action tool hook completed resource " + resourceName + " for " + ownerId + " tool=" + toolName + " toolType=" + toolType + " nativeDamage=" + nativeDamage + " paidExtraHits=" + paidExtraHits + "/" + requiredExtraHits + " damage=" + damageToApply + ".");
                loggedActionApplications.Add(key);
                runtime.SetHookStatus("Smoke.OneActionResourceHit", "verified", "ToolCollider.HandleTools Postfix -> DungeonResource._Fell", LastOneActionApplicationSummary);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "One-action tool hook failed.", ex.ToString());
                return false;
            }
        }

        internal void CaptureOilCoalDropBeforeToolHit(object toolCollider, object collider)
        {
            if (toolCollider == null || collider == null)
                return;

            object? resource = TryGetDungeonResourceFromCollider(collider);
            if (resource == null)
                return;

            string resourceName = GetResourceName(resource);
            if (!IsCoalResourceName(resourceName))
                return;

            string key = BuildOilResourceHitKey(toolCollider, collider);
            pendingOilResourceHits[key] = new PendingOilResourceHit(resource, resourceName, ReadIntMember(resource, "currentHealth", 0));
        }

        internal void ClearCapturedOilCoalDrop(object toolCollider, object collider)
        {
            if (toolCollider == null || collider == null)
                return;
            pendingOilResourceHits.Remove(BuildOilResourceHitKey(toolCollider, collider));
        }

        internal bool ApplyOilCoalDropAfterToolHit(object toolCollider, object collider)
        {
            if (toolCollider == null || collider == null)
                return false;

            string key = BuildOilResourceHitKey(toolCollider, collider);
            pendingOilResourceHits.TryGetValue(key, out PendingOilResourceHit? pending);
            pendingOilResourceHits.Remove(key);

            object? resource = TryGetDungeonResourceFromCollider(collider);
            if (resource == null)
                resource = pending?.Resource;
            if (resource == null)
            {
                if (ForceOilDropForSmoke)
                    runtime.RuntimeMonitor.Log("OilMod mining drop smoke probe source=native-tool-hit resourceFound=False collider=" + collider.GetType().FullName + ".");
                return false;
            }

            string resourceName = pending?.ResourceName ?? GetResourceName(resource);
            bool removed = IsResourceRemoved(resource);
            string summary = TryRollOilDropFromCoal(resourceName, removed, "native-tool-hit");
            if (string.IsNullOrWhiteSpace(summary))
            {
                if (ForceOilDropForSmoke)
                    runtime.RuntimeMonitor.Log("OilMod mining drop smoke probe source=native-tool-hit resource=" + resourceName + " removed=" + removed + " coal=" + IsCoalResourceName(resourceName) + ".");
                return false;
            }

            object? currentTool = ReadMember(toolCollider, "currentTool");
            string toolName = currentTool == null ? "unknown" : ReadStringMember(currentTool, "name", currentTool.GetType().Name);
            string toolType = currentTool == null ? "unknown" : (ReadMember(currentTool, "ToolType")?.ToString() ?? "unknown");
            LastOilMiningDropSummary += ", tool=" + toolName + ", toolType=" + toolType;
            if (pending != null)
                LastOilMiningDropSummary += ", healthBefore=" + pending.HealthBefore;
            return summary.IndexOf("oilDrop=dtmapi_oil", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal bool ApplyOneActionEquipmentFillAfterInteract()
        {
            if (actionOptions.Count == 0)
                return false;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? selectedEquipment = ReadStaticMember(dolocApi, "SelectedEquipment");
            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            if (selectedEquipment == null || selectedItem == null)
                return false;

            Type equipmentType = selectedEquipment.GetType();
            if (IsTypeOrBase(equipmentType, "DolocTown.PowerGeneratorFuel"))
                return TryApplyOneActionFuelFill(selectedEquipment, selectedItem, dolocApi);
            if (IsTypeOrBase(equipmentType, "DolocTown.Feeder"))
                return TryApplyOneActionFeederFill(selectedEquipment, selectedItem, dolocApi);
            return false;
        }

        private bool TryApplyOneActionFuelFill(object generator, object firstSelectedItem, Type? dolocApi)
        {
            if (!TryFindOneActionFuelPolicy(out string ownerId, out ActionCompletionOptions options))
                return false;

            MethodInfo? isSuitableFuel = FindMethodInHierarchy(generator.GetType(), "IsSuitableFuel", 1);
            MethodInfo? addFuel = FindMethodInHierarchy(generator.GetType(), "AddFuel", 2);
            if (isSuitableFuel == null || addFuel == null)
                return false;
            if (!InvokeBool(isSuitableFuel, generator, firstSelectedItem))
                return false;

            double before = ReadDoubleMember(generator, "FuelPercent", -1);
            int consumed = 0;
            int guard = 0;
            try
            {
                while (guard++ < 99 && !IsFuelGeneratorFull(generator))
                {
                    object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
                    if (selectedItem == null || !InvokeBool(isSuitableFuel, generator, selectedItem))
                        break;

                    object? proto = ReadMember(selectedItem, "proto");
                    if (proto == null)
                        break;

                    if (!TryCostSelf(selectedItem))
                        break;

                    addFuel.Invoke(generator, new object?[] { proto, true });
                    consumed++;
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "One-action fuel fill failed.", ex.ToString());
                return consumed > 0;
            }

            if (consumed <= 0)
                return false;

            double after = ReadDoubleMember(generator, "FuelPercent", -1);
            string itemName = FirstText(ReadStringMember(firstSelectedItem, "name"), firstSelectedItem.GetType().Name);
            LastOneActionApplicationSummary = "owner=" + ownerId + ", kind=FuelMachine, item=" + itemName + ", extraConsumed=" + consumed + ", fuelBefore=" + FormatRatio(before) + ", fuelAfter=" + FormatRatio(after);
            OneActionApplicationCount++;
            runtime.RuntimeMonitor.Log("One-action fuel fill completed by " + ownerId + " item=" + itemName + " extraConsumed=" + consumed + " fuel=" + FormatRatio(before) + "->" + FormatRatio(after) + ".");
            runtime.SetHookStatus("Smoke.OneActionFuelFeed", "verified", "AgentStateInteract.OnExit Postfix -> CostSelf/AddFuel", LastOneActionApplicationSummary);
            return true;
        }

        private bool TryApplyOneActionFeederFill(object feeder, object firstSelectedItem, Type? dolocApi)
        {
            if (!TryFindOneActionFeederPolicy(out string ownerId, out ActionCompletionOptions options))
                return false;

            MethodInfo? isAnimalFeeds = FindMethodInHierarchy(feeder.GetType(), "IsAnimalFeeds", 2);
            MethodInfo? addFeeds = FindMethodInHierarchy(feeder.GetType(), "AddFeeds", 1);
            if (isAnimalFeeds == null || addFeeds == null)
                return false;

            object? firstProto = ReadMember(firstSelectedItem, "proto");
            if (firstProto == null || !InvokeIsAnimalFeeds(isAnimalFeeds, feeder, firstProto, out _))
                return false;

            double before = ReadDoubleMember(feeder, "progress", -1);
            int consumed = 0;
            int guard = 0;
            try
            {
                while (guard++ < 99 && ReadDoubleMember(feeder, "progress", 0) < 0.999)
                {
                    object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
                    object? proto = selectedItem == null ? null : ReadMember(selectedItem, "proto");
                    if (selectedItem == null || proto == null || !InvokeIsAnimalFeeds(isAnimalFeeds, feeder, proto, out _))
                        break;

                    if (!TryCostSelf(selectedItem))
                        break;

                    addFeeds.Invoke(feeder, new object?[] { proto });
                    consumed++;
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "One-action feeder fill failed.", ex.ToString());
                return consumed > 0;
            }

            if (consumed <= 0)
                return false;

            double after = ReadDoubleMember(feeder, "progress", -1);
            string itemName = FirstText(ReadStringMember(firstSelectedItem, "name"), firstSelectedItem.GetType().Name);
            LastOneActionApplicationSummary = "owner=" + ownerId + ", kind=Feeder, item=" + itemName + ", extraConsumed=" + consumed + ", progressBefore=" + FormatRatio(before) + ", progressAfter=" + FormatRatio(after);
            OneActionApplicationCount++;
            runtime.RuntimeMonitor.Log("One-action feeder fill completed by " + ownerId + " item=" + itemName + " extraConsumed=" + consumed + " progress=" + FormatRatio(before) + "->" + FormatRatio(after) + ".");
            runtime.SetHookStatus("Smoke.OneActionFuelFeed", "verified", "AgentStateInteract.OnExit Postfix -> CostSelf/AddFeeds", LastOneActionApplicationSummary);
            return true;
        }

        private bool TryBuildValidatedResourceFellData(Type resourceFellDataType, object resource, object tool, object hitPoint, out object? fellData, out string rejectReason)
        {
            fellData = null;
            rejectReason = string.Empty;
            try
            {
                fellData = Activator.CreateInstance(resourceFellDataType, new object?[] { resource, tool, hitPoint });
                if (fellData == null)
                {
                    rejectReason = "native-fell-data-null";
                    return false;
                }
                if (!ReadBoolMember(fellData, "Valid", false))
                {
                    rejectReason = "tool-type-mismatch";
                    return false;
                }
                if (!ReadBoolMember(fellData, "levelMatch", false))
                {
                    int toolLevel = ReadIntMember(fellData, "toolLevel", -1);
                    rejectReason = "tool-level-mismatch level=" + toolLevel;
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                rejectReason = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static int CostOneActionExtraToolHits(int requiredHits, out string reason)
        {
            reason = string.Empty;
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            MethodInfo? hasEnergy = dolocApi?.GetMethod("HasEnoughEnergyForUsingTool", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            MethodInfo? costEnergy = dolocApi?.GetMethod("CostToolEnergy", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (hasEnergy == null || costEnergy == null)
            {
                reason = "native energy methods were not found";
                return 0;
            }

            int paid = 0;
            for (int i = 0; i < requiredHits; i++)
            {
                object? enough = hasEnergy.Invoke(null, null);
                if (!(enough is bool ok && ok))
                {
                    reason = paid == 0 ? "not enough energy for the first extra hit" : "not enough energy after " + paid + " extra hit(s)";
                    break;
                }

                object? cost = costEnergy.Invoke(null, null);
                if (!(cost is bool charged && charged))
                {
                    reason = paid == 0 ? "native CostToolEnergy rejected the first extra hit" : "native CostToolEnergy rejected after " + paid + " extra hit(s)";
                    break;
                }
                paid++;
            }

            if (string.IsNullOrWhiteSpace(reason))
                reason = "charged " + paid + " extra hit(s)";
            return paid;
        }

        internal bool TryFindOneActionPolicyForSmoke(object resource, out string ownerId)
        {
            return TryFindActionPolicy(resource, out ownerId, out _);
        }

        internal bool ApplyActionSpeedToolEnter(object state)
        {
            if (state == null || actionSpeedOptions.Count == 0)
                return false;

            if (!TryFindActionSpeedToolPolicy(out string ownerId, out ActionSpeedOptions options))
                return false;

            object? tool = ReadMember(state, "tool");
            if (!IsAcceleratedTool(tool))
                return false;

            object? body = ReadMember(state, "body");
            if (body == null)
                return false;

            double multiplier = ClampMultiplier(options.ToolMultiplier);
            int changed = 0;
            var samples = new List<string>();
            changed += ApplyAnimatorSpeed(ReadMember(body, "animator"), multiplier, "body", samples);

            object? toolRenderer = ReadMember(body, "ToolRenderer");
            if (toolRenderer != null)
            {
                changed += ApplyAnimatorSpeed(ReadMember(toolRenderer, "animator"), multiplier, "tool-renderer", samples);
                object? collider = ReadMember(toolRenderer, "_collider");
                if (collider != null)
                    changed += ApplyAnimatorSpeed(ReadMember(collider, "_animator"), multiplier, "tool-collider", samples);
            }

            if (changed <= 0)
                return false;

            string toolName = ReadStringMember(tool!, "name");
            if (string.IsNullOrWhiteSpace(toolName))
                toolName = tool!.GetType().Name;

            ActionSpeedApplicationCount++;
            LastActionSpeedApplicationSummary = "owner=" + ownerId + ", tool=" + toolName + ", multiplier=" + multiplier.ToString("0.###") + ", animators=" + changed + ", samples=" + string.Join(";", samples.ToArray());
            if (options.VerboseLogging || !loggedActionSpeedApplications.Contains(ownerId + ":" + toolName))
                runtime.RuntimeMonitor.Log("ActionSpeed tool animation speed applied by " + ownerId + " tool=" + toolName + " multiplier=" + multiplier.ToString("0.###") + " animators=" + changed + ".");
            loggedActionSpeedApplications.Add(ownerId + ":" + toolName);
            runtime.SetHookStatus("Smoke.ActionSpeedTool", "verified", "AgentStateTool.OnEnter Postfix", LastActionSpeedApplicationSummary);
            return true;
        }

        internal bool ApplyActionSpeedInteractEnter(object state)
        {
            if (state == null || actionSpeedOptions.Count == 0)
                return false;

            if (!TryClassifyActionSpeedInteraction(out string ownerId, out ActionSpeedOptions options, out string kind, out double multiplier, out string target))
                return false;

            object? body = ReadMember(state, "body");
            if (body == null)
                return false;

            return ApplyActionSpeedToBody(body, ownerId, options, kind, multiplier, target, "AgentStateInteract.OnEnter Postfix", "Smoke.ActionSpeedInteraction");
        }

        internal bool ApplyActionSpeedEatEnter(object state)
        {
            if (state == null || actionSpeedOptions.Count == 0)
                return false;

            if (!TryFindActionSpeedEatPolicy(out string ownerId, out ActionSpeedOptions options))
                return false;

            object? body = ReadMember(state, "body");
            if (body == null)
                return false;

            return ApplyActionSpeedToBody(body, ownerId, options, "EatDrink", options.EatDrinkMultiplier, "AgentStateEat", "AgentStateEat.OnEnter Postfix", "Smoke.ActionSpeedEatDrink");
        }

        internal bool AdjustActionSpeedUseItemContinuesDelta(ref float dt)
        {
            if (dt <= 0 || actionSpeedOptions.Count == 0)
                return false;

            if (!TryClassifyActionSpeedUseItem(out string ownerId, out ActionSpeedOptions options, out string kind, out double multiplier, out string target))
                return false;

            multiplier = ClampMultiplier(multiplier);
            if (multiplier <= 1)
                return false;

            float original = dt;
            dt = (float)Math.Min(1, original * multiplier);
            string logKey = ownerId + ":UseItemContinues:" + kind + ":" + target;
            if (options.VerboseLogging || !loggedActionSpeedApplications.Contains(logKey))
                runtime.RuntimeMonitor.Log("ActionSpeed right-click continuous use scaled by " + ownerId + " kind=" + kind + " target=" + target + " multiplier=" + multiplier.ToString("0.###") + " dt=" + original.ToString("0.###") + "->" + dt.ToString("0.###") + ".");
            loggedActionSpeedApplications.Add(logKey);
            ActionSpeedApplicationCount++;
            ActionSpeedContinuousUseApplicationCount++;
            LastActionSpeedContinuousUseSummary = "owner=" + ownerId + ", kind=" + kind + ", target=" + target + ", multiplier=" + multiplier.ToString("0.###") + ", dt=" + original.ToString("0.###") + "->" + dt.ToString("0.###");
            LastActionSpeedApplicationSummary = LastActionSpeedContinuousUseSummary;
            runtime.SetHookStatus("Smoke.ActionSpeedContinuousUse", "experimental", "AgentControllerState.UseItemContinues Prefix", LastActionSpeedApplicationSummary);
            return true;
        }

        internal void RestoreActionSpeed(string reason)
        {
            if (originalAnimatorSpeeds.Count == 0)
                return;

            int restored = 0;
            foreach (KeyValuePair<object, double> entry in new List<KeyValuePair<object, double>>(originalAnimatorSpeeds))
            {
                if (TryWriteAnimatorSpeed(entry.Key, entry.Value))
                    restored++;
            }
            originalAnimatorSpeeds.Clear();
            runtime.RuntimeMonitor.Log("ActionSpeed animator speeds restored reason=" + reason + " restored=" + restored + ".");
        }

        internal void UpdateRuntimeAutomation()
        {
            RecoverOrphanEquipmentSlotsIfNeeded();
            UpdateActiveSecondMotorRoomSnapshot();
            UpdateActionSpeedAutoFill();
            UpdateFishingAutoCast();
            UpdateMachineProduction();
            RefreshAnimalProgressOverlayTexts(force: false);
            RenderEquipmentSlotsUiForCurrentAccessoriesBar("runtime", force: false);
        }

        internal int GetMachineProductionCycleCountForSmoke(string ownerId)
        {
            return machineStates.TryGetValue(ownerId ?? string.Empty, out MachineProductionState state)
                ? state.ProductionCycleCount
                : 0;
        }

        internal string GetMachineProductionStateSummaryForSmoke(string ownerId)
        {
            if (!machineStates.TryGetValue(ownerId ?? string.Empty, out MachineProductionState state))
                return "not-configured";

            return "status=" + state.Status +
                ", machine=" + state.MachineId +
                ", equipment=" + state.EquipmentId +
                ", recipe=" + state.RecipeId +
                ", recipeGroup=" + state.RecipeGroupId +
                ", visualScale=" + state.VisualScale.ToString("0.##", CultureInfo.InvariantCulture) +
                ", fuel=" + state.RemainingFuel + "/" + state.FuelCapacity +
                ", cycleMinutes=" + state.CycleMinutes +
                ", cycleTUs=" + state.CycleTUs +
                ", nextDueTUs=" + state.NextDueTotalTUs +
                ", costs=fuel:" + state.FuelOnlyFuelCostPerCycle + "/electricFuel:" + state.ElectricModeFuelCostPerCycle + "/electricPower:" + state.ElectricModePowerCostPerCycle +
                ", placed=" + state.PlacedMachineCount +
                ", cycles=" + state.ProductionCycleCount +
                ", output=" + state.LastOutputItemId +
                ", count=" + state.LastOutputCount +
                ", outputTarget=" + state.LastOutputTarget +
                ", storage=" + state.LastStorageFilledSlots + "/" + state.LastStorageCapacity +
                ", storageLineCapacity=" + state.LastStorageLineCapacity +
                ", mode=" + state.LastMode +
                ", lastCost=" + state.LastFuelCost + "/" + state.LastElectricPowerCost +
                ", techTree={" + state.NativeTechTreeSummary + "}" +
                ", message=" + state.LastMessage;
        }

        internal string GetEquipmentSlotsStateSummaryForSmoke(string ownerId)
        {
            EquipmentSlotsState state = ((IEquipmentSlotsApi)this).GetState(ownerId ?? string.Empty);
            IReadOnlyList<EquipmentSlotInfo> slots = GetSlots(ownerId ?? string.Empty);
            return "status=" + state.Status +
                ", extra=" + state.ExtraAttributeSlots +
                ", stored=" + state.StoredItemCount +
                ", applied=" + state.AppliedItemCount +
                ", pendingRecovery=" + state.PendingRecoveryCount +
                ", uiRendered=" + equipmentSlotsUiRendered +
                ", ui={" + equipmentSlotsUiLastSummary + "}" +
                ", slots=" + string.Join(";", slots.Select(slot => slot.SlotId + "=" + (slot.IsOccupied ? slot.ItemId + (slot.IsApplied ? "[applied]" : "[stored]") : "empty")));
        }

        internal void ApplyEquipmentSlotsAfterReloadParams(object manager)
        {
            if (manager == null || equipmentSlotStates.Count == 0)
                return;

            foreach (KeyValuePair<string, EquipmentSlotsState> entry in equipmentSlotStates.ToArray())
            {
                EquipmentSlotsState state = entry.Value;
                if (!state.IsConfigured)
                    continue;
                state.RuntimeStatsHookInstalled = equipmentSlotsRuntimeHooksInstalled;
                state.StatsRefreshCount++;
                state.StoredItemCount = equipmentSlotEntries.TryGetValue(entry.Key, out List<EquipmentSlotRuntimeEntry>? entries)
                    ? entries.Count(slot => !string.IsNullOrWhiteSpace(slot.ItemId))
                    : 0;
                state.AppliedItemCount = equipmentSlotEntries.TryGetValue(entry.Key, out entries)
                    ? entries.Count(slot => !string.IsNullOrWhiteSpace(slot.ItemId) && slot.Applied)
                    : 0;
                state.Status = equipmentSlotsRuntimeHooksInstalled
                    ? (equipmentSlotsUiHooksInstalled || equipmentSlotsUiRendered ? "configured-experimental-player-ui-storage-stats-hook" : "configured-experimental-storage-stats-hook")
                    : state.Status;
                state.LastRecoveryMessage = "Native equipment params refreshed; extra slots are attribute-only and preserve vanilla visual slots. Stored extra-slot item count=" + state.StoredItemCount + ", applied=" + state.AppliedItemCount + ".";
                equipmentSlotStates[entry.Key] = state;
                if (state.StatsRefreshCount <= 1 || equipmentSlotOptions.TryGetValue(entry.Key, out EquipmentSlotsOptions? options) && options.VerboseLogging)
                    runtime.RuntimeMonitor.Log("EquipmentSlots stats refresh owner=" + entry.Key + " extraSlots=" + state.ExtraAttributeSlots + " stored=" + state.StoredItemCount + " applied=" + state.AppliedItemCount + " preserveVanillaVisualSlots=" + state.PreserveVanillaVisualSlots + " visualsFromExtraSlots=" + state.ExtraSlotsAffectVisuals + " refreshCount=" + state.StatsRefreshCount + ".");
            }

            runtime.SetHookStatus("Player.EquipmentSlotsApi", equipmentSlotsUiHooksInstalled || equipmentSlotsUiRendered ? "configured-experimental-player-ui-storage-stats-hook" : "configured-experimental-storage-stats-hook", "AgentEquipmentManager.ReloadParams postfix", "Native equipment params refresh observed for DTMAPI extra-slot policy; DTMAPI owns extra-slot storage and populated recovery.");
            RenderEquipmentSlotsUiForCurrentAccessoriesBar("AgentEquipmentManager.ReloadParams", force: true);
        }

        internal bool RenderEquipmentSlotsUiForAccessoriesBar(object accessoriesBar, string reason)
        {
            return RenderEquipmentSlotsUi(accessoriesBar, reason, force: true);
        }

        internal bool RenderEquipmentSlotsUiForCurrentAccessoriesBar(string reason, bool force)
        {
            if (!HasEnabledEquipmentSlots())
                return false;
            if (!force && (DateTimeOffset.Now - lastEquipmentSlotsUiRefreshAt).TotalSeconds < 0.5)
                return equipmentSlotsUiRendered;

            object? accessoriesBar = FindUnityObjectOfType("DolocTown.UI.AccessoriesBar, Assembly-CSharp");
            if (accessoriesBar == null)
                return false;
            return RenderEquipmentSlotsUi(accessoriesBar, reason, force);
        }

        internal string CaptureEquipmentSlotsUiEvidenceForSmoke(string reason)
        {
            bool rendered = RenderEquipmentSlotsUiForCurrentAccessoriesBar(reason ?? "smoke", force: true);
            if (!rendered && !equipmentSlotsUiRendered)
                throw new InvalidOperationException("DTMAPI extra equipment slot UI surface was not rendered before smoke evidence capture.");

            string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string evidenceDir = Path.Combine(runtime.Paths.EvidencePath, "EQUIPMENT-SLOTS-UI", timestamp);
            Directory.CreateDirectory(evidenceDir);
            string screenshotPath = Path.Combine(evidenceDir, "equipment-slots-ui.png");
            bool screenshotRequested = TryCaptureScreenshot(screenshotPath);
            string summary = "rendered=" + equipmentSlotsUiRendered +
                ", " + equipmentSlotsUiLastSummary +
                ", screenshot=" + (screenshotRequested ? screenshotPath : "unavailable");
            File.WriteAllText(Path.Combine(evidenceDir, "summary.txt"),
                "Captured=" + DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture) + Environment.NewLine +
                "Reason=" + (reason ?? string.Empty) + Environment.NewLine +
                "ScreenshotRequested=" + screenshotRequested + Environment.NewLine +
                "Screenshot=" + screenshotPath + Environment.NewLine +
                "Summary=" + summary + Environment.NewLine);
            equipmentSlotsUiEvidenceRecorded = true;
            runtime.RuntimeMonitor.Log("EquipmentSlots UI evidence " + (screenshotRequested ? "OK" : "unavailable") + " " + summary + ".");
            runtime.SetHookStatus("Smoke.NewContentEquipmentSlotsUi", screenshotRequested ? "verified" : "pending", "AccessoriesBar cloned DTMAPI extra slots + UnityEngine.ScreenCapture", summary);
            return summary;
        }

        private bool RenderEquipmentSlotsUi(object accessoriesBar, string reason, bool force)
        {
            if (accessoriesBar == null || !HasEnabledEquipmentSlots())
                return false;
            if (!force && (DateTimeOffset.Now - lastEquipmentSlotsUiRefreshAt).TotalSeconds < 0.5)
                return equipmentSlotsUiRendered;

            IReadOnlyList<EquipmentSlotRuntimeEntry> visibleSlots = BuildEquipmentSlotsForUi();
            if (visibleSlots.Count == 0)
                return false;

            try
            {
                object? sourceSlot = ReadMember(accessoriesBar, "passiveItem2") ?? ReadMember(accessoriesBar, "passiveItem1") ?? ReadMember(accessoriesBar, "positiveItem");
                object? sourceGameObject = sourceSlot == null ? null : ReadMember(sourceSlot, "gameObject");
                object? sourceTransform = sourceGameObject == null ? null : ReadMember(sourceGameObject, "transform");
                object? parent = sourceTransform == null ? null : ReadMember(sourceTransform, "parent");
                if (sourceSlot == null || sourceGameObject == null || sourceTransform == null || parent == null)
                    return false;

                ClearEquipmentSlotsUi(parent);

                int rendered = 0;
                int occupied = 0;
                var renderedNames = new List<string>();
                foreach (EquipmentSlotRuntimeEntry entry in visibleSlots.Take(6))
                {
                    object? clone = CloneUnityObject(sourceGameObject);
                    if (clone == null)
                        continue;

                    SetMemberValue(clone, "name", "DTMAPI.ExtraEquipmentSlot." + rendered);
                    SetActive(clone, false);
                    object? cloneTransform = ReadMember(clone, "transform");
                    if (cloneTransform == null)
                    {
                        DestroyUnityObject(clone);
                        continue;
                    }

                    SetParent(cloneTransform, parent, worldPositionStays: false);
                    PositionEquipmentSlotUiClone(sourceTransform, cloneTransform, rendered);
                    object? cloneSlot = GetComponent(clone, sourceSlot.GetType());
                    object? icon = ResolveEquipmentSlotIcon(entry);
                    if (icon != null)
                        occupied++;
                    MethodInfo? render = cloneSlot == null ? null : FindMethodInHierarchy(cloneSlot.GetType(), "Render", 1);
                    render?.Invoke(cloneSlot, new object?[] { icon });

                    object? button = cloneSlot == null ? null : ReadMember(cloneSlot, "button");
                    if (button != null)
                        SetMemberValue(button, "interactable", false);

                    activeEquipmentSlotUiObjects.Add(clone);
                    SetActive(clone, true);
                    renderedNames.Add(entry.SlotId + "=" + (string.IsNullOrWhiteSpace(entry.ItemId) ? "empty" : entry.ItemId));
                    rendered++;
                }

                if (rendered <= 0)
                    return false;

                equipmentSlotsUiRendered = true;
                lastEquipmentSlotsUiRefreshAt = DateTimeOffset.Now;
                equipmentSlotsUiLastSummary = "reason=" + (reason ?? string.Empty) +
                    ", hooks=" + equipmentSlotsUiHooksInstalled +
                    ", rendered=" + rendered +
                    ", occupied=" + occupied +
                    ", readOnly=true, attributeOnly=true, preserveVanillaVisualSlots=true" +
                    ", slots=" + string.Join(";", renderedNames.ToArray());
                RefreshEquipmentSlotsUiStateFlags();
                runtime.SetHookStatus("Player.EquipmentSlotsApi", "configured-experimental-player-ui-storage-stats-hook", "AccessoriesBar DTMAPI cloned extra-slot strip", equipmentSlotsUiLastSummary);
                if (!equipmentSlotsUiEvidenceRecorded)
                    runtime.SetHookStatus("Smoke.NewContentEquipmentSlotsUi", "pending", "AccessoriesBar cloned DTMAPI extra slots", equipmentSlotsUiLastSummary);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "EquipmentSlots UI render failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.NewContentEquipmentSlotsUi", "failed", "AccessoriesBar cloned DTMAPI extra slots", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private bool HasEnabledEquipmentSlots()
        {
            return equipmentSlotOptions.Any(entry => entry.Value.Enabled && entry.Value.ExtraAttributeSlots > 0);
        }

        private IReadOnlyList<EquipmentSlotRuntimeEntry> BuildEquipmentSlotsForUi()
        {
            var result = new List<EquipmentSlotRuntimeEntry>();
            foreach (KeyValuePair<string, EquipmentSlotsOptions> optionEntry in equipmentSlotOptions.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
            {
                EquipmentSlotsOptions options = optionEntry.Value;
                if (!options.Enabled || options.ExtraAttributeSlots <= 0)
                    continue;
                EnsureEquipmentSlotStorageLoaded(optionEntry.Key);
                foreach (EquipmentSlotRuntimeEntry entry in EnsureEquipmentSlotEntries(optionEntry.Key, options)
                    .Where(entry => entry.Index >= 0 && entry.Index < options.ExtraAttributeSlots)
                    .OrderBy(entry => entry.Index))
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        private object? ResolveEquipmentSlotIcon(EquipmentSlotRuntimeEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId))
                return null;
            if (!TryGenerateNativeItem(entry.ItemId, 1, out object? item, out _, out _))
                return null;
            return ReadMember(item!, "uiSprite");
        }

        private void ClearEquipmentSlotsUi(object parentTransform)
        {
            foreach (object instance in activeEquipmentSlotUiObjects.ToArray())
            {
                SetActive(instance, false);
                DestroyUnityObject(instance);
            }
            activeEquipmentSlotUiObjects.Clear();

            MethodInfo? find = parentTransform.GetType().GetMethod("Find", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            for (int i = 0; i < 12; i++)
            {
                object? child = find?.Invoke(parentTransform, new object[] { "DTMAPI.ExtraEquipmentSlot." + i });
                object? gameObject = child == null ? null : ReadMember(child, "gameObject");
                if (gameObject != null)
                    DestroyUnityObject(gameObject);
            }
        }

        private void RefreshEquipmentSlotsUiStateFlags()
        {
            foreach (KeyValuePair<string, EquipmentSlotsOptions> optionEntry in equipmentSlotOptions.ToArray())
            {
                EquipmentSlotsState state = BuildEquipmentSlotsState(optionEntry.Key, optionEntry.Value);
                state.RuntimeUiHookInstalled = equipmentSlotsUiHooksInstalled || equipmentSlotsUiRendered;
                state.RuntimeStatsHookInstalled = equipmentSlotsRuntimeHooksInstalled;
                if (state.IsConfigured && state.RuntimeUiHookInstalled && state.RuntimeStatsHookInstalled)
                    state.Status = "configured-experimental-player-ui-storage-stats-hook";
                state.LastRecoveryMessage = FirstText(equipmentSlotsUiLastSummary, state.LastRecoveryMessage);
                equipmentSlotStates[optionEntry.Key] = state;
            }
        }

        private static void PositionEquipmentSlotUiClone(object sourceTransform, object cloneTransform, int index)
        {
            object? localPosition = ReadMember(sourceTransform, "localPosition");
            double baseX = ReadVectorComponent(localPosition, "x");
            double baseY = ReadVectorComponent(localPosition, "y");
            double z = ReadVectorComponent(localPosition, "z");
            int column = index % 3;
            int row = index / 3;
            object? position = CreateUnityVector3(baseX + 44d * (column + 1), baseY - 44d * row, z);
            if (position != null)
                SetMemberValue(cloneTransform, "localPosition", position);

            object? localScale = ReadMember(sourceTransform, "localScale");
            if (localScale != null)
                SetMemberValue(cloneTransform, "localScale", localScale);
        }

        private static object? FindUnityObjectOfType(string assemblyQualifiedName)
        {
            Type? targetType = ResolveType(assemblyQualifiedName);
            if (targetType == null)
                return null;

            Type? objectType = ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Object, UnityEngine");
            MethodInfo? findObject = objectType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "FindObjectOfType" && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(Type));
            object? found = findObject?.Invoke(null, new object[] { targetType });
            if (found != null)
                return found;

            Type? resourcesType = ResolveType("UnityEngine.Resources, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Resources, UnityEngine");
            MethodInfo? findAll = resourcesType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method => method.Name == "FindObjectsOfTypeAll" && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(Type));
            object? all = findAll?.Invoke(null, new object[] { targetType });
            if (all is IEnumerable enumerable)
            {
                foreach (object candidate in enumerable)
                {
                    if (candidate != null)
                        return candidate;
                }
            }

            return null;
        }

        private void UpdateMachineProduction()
        {
            if (machineDefinitions.Count == 0)
                return;
            if ((DateTimeOffset.Now - lastMachineProductionPollAt).TotalSeconds < 0.5)
                return;

            lastMachineProductionPollAt = DateTimeOffset.Now;
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
            if (dolocApi == null || archive == null || currentRoom == null)
                return;

            int totalTus = GetCurrentTotalTus(archive);
            if (totalTus < 0)
                return;

            machineRuntimeLoopInstalled = true;
            int tuMinutes = Math.Max(1, GetCurrentTuMinutes(archive));
            bool forceDue = ForceMachineProductionDueForSmoke;
            bool newTu = forceDue || totalTus != lastMachineProductionTotalTus;
            if (newTu)
                lastMachineProductionTotalTus = totalTus;

            var allEquipments = EnumerateEquipments(currentRoom).ToArray();
            foreach (KeyValuePair<string, List<MachineDefinition>> ownerEntry in machineDefinitions.ToArray())
            {
                string ownerId = ownerEntry.Key;
                int placedCount = 0;
                int productionCount = machineStates.TryGetValue(ownerId, out MachineProductionState existingState) ? existingState.ProductionCycleCount : 0;
                string nativeTechTreeSummary = machineStates.TryGetValue(ownerId, out MachineProductionState existingTechState) ? existingTechState.NativeTechTreeSummary : string.Empty;

                foreach (MachineDefinition definition in ownerEntry.Value.ToArray())
                {
                    nativeTechTreeSummary = FirstText(EnsureNativeMachineTechRoute(definition), nativeTechTreeSummary);
                    object[] placed = allEquipments
                        .Where(equipment => string.Equals(ReadStringMember(equipment, "Name"), definition.EquipmentId, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    placedCount += placed.Length;

                    foreach (object equipment in placed)
                    {
                        string machineKey = BuildMachineRuntimeKey(ownerId, definition, equipment);
                        if (!machineRuntimeEntries.TryGetValue(machineKey, out MachineRuntimeEntry? entry))
                        {
                            int cycleTus = GetMachineCycleTus(definition, tuMinutes);
                            entry = new MachineRuntimeEntry
                            {
                                OwnerId = ownerId,
                                MachineId = definition.MachineId,
                                EquipmentId = definition.EquipmentId,
                                MachineKey = machineKey,
                                RemainingFuel = Math.Max(0, definition.FuelCapacity),
                                NextDueTotalTus = forceDue ? totalTus : totalTus + cycleTus,
                                LastObservedTotalTus = totalTus
                            };
                            machineRuntimeEntries[machineKey] = entry;
                        }

                        entry.LastObservedTotalTus = totalTus;
                        string visualScaleSummary = TryApplyMachineVisualScale(definition, equipment);
                        if (!string.IsNullOrWhiteSpace(visualScaleSummary))
                            entry.LastVisualScaleSummary = visualScaleSummary;
                        if (!newTu || totalTus < entry.NextDueTotalTus)
                            continue;

                        if (TryRunMachineProductionCycle(dolocApi, definition, equipment, entry, totalTus, tuMinutes, out string message))
                        {
                            productionCount++;
                            MachineOutputRule output = entry.LastOutputRule ?? new MachineOutputRule();
                            MachineProductionState state = GetMachineProductionStateForUpdate(ownerId, ownerEntry.Value.Count);
                            ApplyMachineDefinitionState(state, definition, tuMinutes, entry);
                            state.RuntimeHookInstalled = true;
                            state.PlacedMachineCount = placedCount;
                            state.ProductionCycleCount = productionCount;
                            state.LastOutputItemId = entry.LastOutputItemId;
                            state.LastOutputDisplayName = output.DisplayName ?? string.Empty;
                            state.LastOutputCount = entry.LastOutputCount;
                            state.LastMachineKey = machineKey;
                            state.LastMode = entry.LastMode;
                            state.LastObservedTotalTUs = totalTus;
                            state.LastOutputTarget = entry.LastOutputTarget;
                            state.LastStorageFilledSlots = entry.LastStorageFilledSlots;
                            state.LastStorageCapacity = entry.LastStorageCapacity;
                            state.LastStorageLineCapacity = entry.LastStorageLineCapacity;
                            state.NativeTechTreeSummary = nativeTechTreeSummary;
                            state.Status = "configured-experimental-runtime-loop";
                            state.LastMessage = string.IsNullOrWhiteSpace(entry.LastVisualScaleSummary) ? message : message + " visual={" + entry.LastVisualScaleSummary + "}";
                            machineStates[ownerId] = state;
                            runtime.RuntimeMonitor.Log("MachineProduction cycle OK owner=" + ownerId + " machine=" + definition.MachineId + " equipment=" + definition.EquipmentId + " output=" + entry.LastOutputItemId + " count=" + entry.LastOutputCount + " mode=" + entry.LastMode + " fuelRemaining=" + entry.RemainingFuel + " totalTUs=" + totalTus + ".");
                            runtime.SetHookStatus("Machine.ProductionApi", "configured-experimental-runtime-loop", "DTMAPI runtime update -> equipment IContainer/LinearInventory", message);
                        }
                        else if (!string.IsNullOrWhiteSpace(message))
                        {
                            MachineProductionState state = GetMachineProductionStateForUpdate(ownerId, ownerEntry.Value.Count);
                            ApplyMachineDefinitionState(state, definition, tuMinutes, entry);
                            state.RuntimeHookInstalled = true;
                            state.PlacedMachineCount = placedCount;
                            state.LastMachineKey = machineKey;
                            state.LastMode = entry.LastMode;
                            state.LastObservedTotalTUs = totalTus;
                            state.NativeTechTreeSummary = nativeTechTreeSummary;
                            state.Status = "configured-experimental-runtime-loop";
                            state.LastMessage = string.IsNullOrWhiteSpace(entry.LastVisualScaleSummary) ? message : message + " visual={" + entry.LastVisualScaleSummary + "}";
                            machineStates[ownerId] = state;
                        }
                    }
                }

                MachineProductionState finalState = GetMachineProductionStateForUpdate(ownerId, ownerEntry.Value.Count);
                MachineDefinition? finalDefinition = ownerEntry.Value.FirstOrDefault();
                MachineRuntimeEntry? finalEntry = finalDefinition == null
                    ? null
                    : machineRuntimeEntries.Values.FirstOrDefault(entry => entry.OwnerId.Equals(ownerId, StringComparison.OrdinalIgnoreCase) && entry.MachineId.Equals(finalDefinition.MachineId, StringComparison.OrdinalIgnoreCase));
                if (finalDefinition != null)
                    ApplyMachineDefinitionState(finalState, finalDefinition, tuMinutes, finalEntry);
                finalState.RuntimeHookInstalled = true;
                finalState.PlacedMachineCount = placedCount;
                finalState.ProductionCycleCount = productionCount;
                finalState.LastObservedTotalTUs = totalTus;
                finalState.NativeTechTreeSummary = nativeTechTreeSummary;
                if (placedCount == 0 && string.IsNullOrWhiteSpace(finalState.LastMessage))
                    finalState.LastMessage = "Runtime loop active; no placed registered machine found in current room/subrooms.";
                finalState.Status = "configured-experimental-runtime-loop";
                machineStates[ownerId] = finalState;
            }
        }

        private MachineProductionState GetMachineProductionStateForUpdate(string ownerId, int registeredMachineCount)
        {
            if (!machineStates.TryGetValue(ownerId, out MachineProductionState state))
            {
                state = new MachineProductionState
                {
                    OwnerId = ownerId,
                    IsConfigured = true
                };
            }

            state.RegisteredMachineCount = registeredMachineCount;
            return state;
        }

        private string TryApplyMachineVisualScale(MachineDefinition definition, object equipment)
        {
            if (definition.VisualScale <= 0 || Math.Abs(definition.VisualScale - 1d) < 0.01)
                return string.Empty;

            try
            {
                object? renderer = ReadMember(equipment, "Renderer");
                object? transform = renderer == null ? null : ReadMember(renderer, "transform");
                if (transform == null)
                    return "visualScale=" + definition.VisualScale.ToString("0.##", CultureInfo.InvariantCulture) + ", renderer=pending";

                object? currentScale = ReadMember(transform, "localScale");
                double currentX = currentScale == null ? 1 : ReadVectorComponent(currentScale, "x");
                double currentY = currentScale == null ? 1 : ReadVectorComponent(currentScale, "y");
                double currentZ = currentScale == null ? 1 : ReadVectorComponent(currentScale, "z");
                double signedX = currentX < 0 ? -definition.VisualScale : definition.VisualScale;
                double z = Math.Abs(currentZ) < 0.001 || double.IsNaN(currentZ) ? 1 : currentZ;
                bool alreadyApplied = Math.Abs(Math.Abs(currentX) - definition.VisualScale) < 0.01 && Math.Abs(Math.Abs(currentY) - definition.VisualScale) < 0.01;
                if (!alreadyApplied)
                {
                    object? scale = CreateUnityVector3(signedX, definition.VisualScale, z);
                    if (scale == null || !SetMemberValue(transform, "localScale", scale))
                        return "visualScale=" + definition.VisualScale.ToString("0.##", CultureInfo.InvariantCulture) + ", rendererScale=failed";
                }

                string summary = "visualScale=" + definition.VisualScale.ToString("0.##", CultureInfo.InvariantCulture) +
                    ", rendererScale=" + signedX.ToString("0.##", CultureInfo.InvariantCulture) + "x" + definition.VisualScale.ToString("0.##", CultureInfo.InvariantCulture) +
                    ", applied=" + (!alreadyApplied);
                runtime.SetHookStatus("Machine.VisualScale", "verified", "Equipment.Renderer.transform.localScale", definition.EquipmentId + " " + summary);
                return summary;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Machine visual scale failed for " + definition.EquipmentId + ".", ex.ToString());
                return "visualScale=" + definition.VisualScale.ToString("0.##", CultureInfo.InvariantCulture) + ", failed=" + ex.GetType().Name + ":" + ex.Message;
            }
        }

        private static void ApplyMachineDefinitionState(MachineProductionState state, MachineDefinition definition, int tuMinutes, MachineRuntimeEntry? entry)
        {
            state.MachineId = definition.MachineId;
            state.DisplayName = definition.DisplayName;
            state.ItemId = definition.ItemId;
            state.EquipmentId = definition.EquipmentId;
            state.RecipeId = definition.RecipeId;
            state.RecipeGroupId = definition.RecipeGroupId;
            state.VisualScale = definition.VisualScale;
            state.AllowFuelMode = definition.AllowFuelMode;
            state.AllowElectricMode = definition.AllowElectricMode;
            state.DefaultMode = definition.DefaultMode;
            state.FuelCapacity = definition.FuelCapacity;
            state.RemainingFuel = entry == null ? 0 : entry.RemainingFuel;
            state.FuelOnlyFuelCostPerCycle = definition.FuelOnlyFuelCostPerCycle;
            state.ElectricModeFuelCostPerCycle = definition.ElectricModeFuelCostPerCycle;
            state.ElectricModePowerCostPerCycle = definition.ElectricModePowerCostPerCycle;
            state.CycleMinutes = definition.CycleMinutes;
            state.CycleTUs = tuMinutes <= 0 ? 0 : GetMachineCycleTus(definition, tuMinutes);
            state.NextDueTotalTUs = entry == null ? -1 : entry.NextDueTotalTus;
            if (entry != null)
            {
                state.LastFuelCost = entry.LastFuelCost;
                state.LastElectricPowerCost = entry.LastElectricPowerCost;
                state.LastOutputTarget = entry.LastOutputTarget;
                state.LastStorageFilledSlots = entry.LastStorageFilledSlots;
                state.LastStorageCapacity = entry.LastStorageCapacity;
                state.LastStorageLineCapacity = entry.LastStorageLineCapacity;
            }
        }

        private bool TryRunMachineProductionCycle(Type dolocApi, MachineDefinition definition, object equipment, MachineRuntimeEntry entry, int totalTus, int tuMinutes, out string message)
        {
            message = string.Empty;
            int cycleTus = GetMachineCycleTus(definition, tuMinutes);
            entry.NextDueTotalTus = totalTus + cycleTus;
            entry.LastMode = definition.AllowElectricMode && definition.DefaultMode.Equals("electric", StringComparison.OrdinalIgnoreCase) ? "electric" : "fuel";
            int fuelCost = entry.LastMode.Equals("electric", StringComparison.OrdinalIgnoreCase)
                ? definition.ElectricModeFuelCostPerCycle
                : definition.FuelOnlyFuelCostPerCycle;
            entry.LastFuelCost = fuelCost;
            entry.LastElectricPowerCost = entry.LastMode.Equals("electric", StringComparison.OrdinalIgnoreCase) ? definition.ElectricModePowerCostPerCycle : 0;

            if (fuelCost > 0 && entry.RemainingFuel < fuelCost)
            {
                message = "Machine " + definition.MachineId + " skipped production because DTMAPI fuel state is empty. mode=" + entry.LastMode + ", fuelRemaining=" + entry.RemainingFuel + ", fuelCost=" + fuelCost + ".";
                return false;
            }

            MachineOutputRule? selected = PickMachineOutput(definition);
            if (selected == null)
            {
                message = "Machine " + definition.MachineId + " skipped production because it has no positive output weights.";
                return false;
            }

            int count = machineRandom.Next(Math.Min(selected.MinCount, selected.MaxCount), Math.Max(selected.MinCount, selected.MaxCount) + 1);
            if (!TryPlaceMachineOutput(dolocApi, definition, equipment, selected.ItemId, count, out string placementMessage, out string outputTarget, out int filledSlots, out int storageCapacity, out int storageLineCapacity))
            {
                message = "Machine " + definition.MachineId + " produced " + selected.ItemId + " x" + count + " but output placement failed: " + placementMessage;
                return false;
            }

            entry.RemainingFuel = Math.Max(0, entry.RemainingFuel - fuelCost);
            entry.LastOutputRule = selected;
            entry.LastOutputItemId = selected.ItemId;
            entry.LastOutputCount = count;
            entry.LastOutputTarget = outputTarget;
            entry.LastStorageFilledSlots = filledSlots;
            entry.LastStorageCapacity = storageCapacity;
            entry.LastStorageLineCapacity = storageLineCapacity;
            entry.ProductionCycleCount++;
            message = "Machine " + definition.MachineId + " produced " + selected.ItemId + " x" + count + " via " + entry.LastMode + " mode; fuelCost=" + fuelCost + ", electricPowerCost=" + entry.LastElectricPowerCost + ", " + placementMessage;
            return true;
        }

        private bool TryPlaceMachineOutput(Type dolocApi, MachineDefinition definition, object equipment, string itemId, int count, out string message, out string outputTarget, out int filledSlots, out int storageCapacity, out int storageLineCapacity)
        {
            message = string.Empty;
            outputTarget = "unknown";
            filledSlots = 0;
            storageCapacity = 0;
            storageLineCapacity = 0;

            object? inventory = ReadMember(equipment, "inventory");
            if (inventory != null)
            {
                storageCapacity = ReadIntMember(inventory, "capacity", 0);
                filledSlots = ReadIntMember(inventory, "filledCount", 0);
                int emptySlots = ReadIntMember(inventory, "emptyCount", 0);
                storageLineCapacity = ReadIntMember(equipment, "lineCapacity", 0);
                if (storageCapacity <= 0)
                {
                    message = "Equipment inventory exists but reports no capacity.";
                    return false;
                }
                if (emptySlots < count)
                {
                    message = "Machine-owned storage is full. emptySlots=" + emptySlots + ", requestedSlots=" + count + ", filled=" + filledSlots + "/" + storageCapacity + ".";
                    outputTarget = "equipment-storage-full";
                    return false;
                }

                MethodInfo? contentFilter = FindMethodInHierarchy(equipment.GetType(), "ContentFilter", 1);
                MethodInfo? placeItemAt = inventory.GetType().GetMethod("PlaceItemAt", BindingFlags.Public | BindingFlags.Instance);
                if (placeItemAt == null)
                {
                    message = "LinearInventory.PlaceItemAt was not available on machine-owned storage.";
                    return false;
                }

                for (int i = 0; i < count; i++)
                {
                    if (!TryGenerateNativeItem(itemId, 1, out object? item, out string itemReason, out string itemMessage))
                    {
                        message = "Could not generate output item for storage: " + itemReason + " " + itemMessage;
                        return false;
                    }
                    object? filterResult = contentFilter?.Invoke(equipment, new[] { item });
                    if (filterResult is bool allowed && !allowed)
                    {
                        message = "Machine-owned storage ContentFilter rejected " + itemId + ".";
                        return false;
                    }

                    int slot = ReadIntMember(inventory, "FirstEmptyIndex", -1);
                    if (slot < 0)
                    {
                        message = "Machine-owned storage had no empty slot during placement.";
                        return false;
                    }

                    object? leftover = placeItemAt.Invoke(inventory, new[] { slot, item });
                    if (leftover != null && ReadIntMember(leftover, "count", 1) > 0)
                    {
                        message = "LinearInventory.PlaceItemAt returned leftover item for " + itemId + " at slot " + slot + ".";
                        return false;
                    }
                }

                filledSlots = ReadIntMember(inventory, "filledCount", filledSlots);
                storageCapacity = ReadIntMember(inventory, "capacity", storageCapacity);
                storageLineCapacity = ReadIntMember(equipment, "lineCapacity", storageLineCapacity);
                outputTarget = "equipment-storage";
                message = "Stored " + itemId + " x" + count + " in machine-owned storage. filledSlots=" + filledSlots + "/" + storageCapacity + ", lineCapacity=" + storageLineCapacity + ".";
                return true;
            }

            if (definition.EquipmentId.Equals("dtmapi_mine", StringComparison.OrdinalIgnoreCase))
            {
                message = "dtmapi_mine is expected to be an IContainer/Case with machine-owned storage; backpack fallback is disabled for Mine.";
                outputTarget = "missing-equipment-storage";
                return false;
            }

            if (TryPlaceNativeItemInBackpack(dolocApi, itemId, count, out message))
            {
                outputTarget = "backpack";
                return true;
            }

            outputTarget = "backpack-failed";
            return false;
        }

        private MachineOutputRule? PickMachineOutput(MachineDefinition definition)
        {
            var weighted = new List<(MachineOutputRule Rule, double Weight)>();
            foreach (MachineOutputRule rule in definition.OutputRules ?? Array.Empty<MachineOutputRule>())
            {
                double weight = rule.Weight;
                if (rule.AllowProbabilityOverride &&
                    definition.ProbabilityOverrides != null &&
                    definition.ProbabilityOverrides.TryGetValue(rule.ItemId, out double overrideWeight))
                {
                    weight = overrideWeight;
                }

                if (weight > 0 && !string.IsNullOrWhiteSpace(rule.ItemId))
                    weighted.Add((rule, weight));
            }

            double total = weighted.Sum(entry => entry.Weight);
            if (total <= 0)
                return null;

            double roll = machineRandom.NextDouble() * total;
            double cursor = 0;
            foreach ((MachineOutputRule rule, double weight) in weighted)
            {
                cursor += weight;
                if (roll <= cursor)
                    return rule;
            }

            return weighted[weighted.Count - 1].Rule;
        }

        private string TryRollOilDropFromCoal(string resourceName, bool removed, string source)
        {
            if (!removed || !IsCoalResourceName(resourceName))
                return string.Empty;

            bool forced = ForceOilDropForSmoke;
            double roll = machineRandom.NextDouble();
            if (!forced && roll > 0.08)
                return string.Empty;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (dolocApi == null)
            {
                LastOilMiningDropSummary = "source=" + source + ", resource=" + resourceName + ", forced=" + forced + ", roll=" + roll.ToString("0.0000", CultureInfo.InvariantCulture) + ", oilDrop=failed:missing-dolocapi";
                return "oilDrop=failed:missing-dolocapi";
            }
            if (TryPlaceNativeItemInBackpack(dolocApi, "dtmapi_oil", 1, out string message))
            {
                OilMiningDropCount++;
                LastOilMiningDropSummary = "source=" + source + ", resource=" + resourceName + ", forced=" + forced + ", roll=" + roll.ToString("0.0000", CultureInfo.InvariantCulture) + ", oilDrop=dtmapi_oil, count=1, placement={" + message + "}";
                runtime.RuntimeMonitor.Log("OilMod mining drop OK " + LastOilMiningDropSummary);
                runtime.SetHookStatus("OilMod.MiningDrop", "experimental", "ToolCollider.HandleTools Postfix -> DolocAPI.TryPlaceInBackpack", "Coal resource rolled dtmapi_oil x1. " + LastOilMiningDropSummary);
                return "oilDrop=dtmapi_oil";
            }

            LastOilMiningDropSummary = "source=" + source + ", resource=" + resourceName + ", forced=" + forced + ", roll=" + roll.ToString("0.0000", CultureInfo.InvariantCulture) + ", oilDrop=failed:" + message;
            runtime.SetHookStatus("OilMod.MiningDrop", "failed", "ToolCollider.HandleTools Postfix -> DolocAPI.TryPlaceInBackpack", message);
            return "oilDrop=failed:" + message;
        }

        private static string BuildOilResourceHitKey(object toolCollider, object collider)
        {
            return RuntimeHelpers.GetHashCode(toolCollider).ToString(CultureInfo.InvariantCulture) + ":" +
                RuntimeHelpers.GetHashCode(collider).ToString(CultureInfo.InvariantCulture);
        }

        private static bool IsCoalResourceName(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
                return false;
            string normalized = resourceName.Trim().ToLowerInvariant();
            return normalized.Equals("coal", StringComparison.Ordinal) ||
                normalized.Equals("coal_ore", StringComparison.Ordinal) ||
                normalized.Contains("coal");
        }

        private static bool TryPlaceNativeItemInBackpack(Type dolocApi, string itemId, int count, out string message)
        {
            message = string.Empty;
            if (dolocApi == null)
            {
                message = "DolocAPI is not available.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(itemId) || count <= 0)
            {
                message = "Invalid item id or count.";
                return false;
            }

            MethodInfo? queryItemProto = dolocApi.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
            object?[] queryArgs = new object?[] { itemId, null };
            if (!(queryItemProto?.Invoke(null, queryArgs) is bool found) || !found || queryArgs[1] == null)
            {
                message = "Item " + itemId + " is not present in DolocConfig.Tables.TbItem.";
                return false;
            }

            MethodInfo? canPlaceItem = dolocApi.GetMethod("CanPlaceItem", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int) }, null);
            MethodInfo? tryPlaceInBackpack = dolocApi.GetMethod("TryPlaceInBackpack", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string), typeof(int), typeof(bool) }, null);
            if (canPlaceItem == null || tryPlaceInBackpack == null)
            {
                message = "Native backpack placement methods are not available.";
                return false;
            }

            object? canPlace = canPlaceItem.Invoke(null, new object?[] { itemId, count });
            if (!(canPlace is bool okToPlace) || !okToPlace)
            {
                message = "Backpack cannot place " + itemId + " x" + count + ".";
                return false;
            }

            object? placed = tryPlaceInBackpack.Invoke(null, new object?[] { itemId, count, false });
            if (!(placed is bool ok) || !ok)
            {
                message = "DolocAPI.TryPlaceInBackpack returned false for " + itemId + " x" + count + ".";
                return false;
            }

            message = "Placed " + itemId + " x" + count + " through native backpack placement.";
            return true;
        }

        private static int GetCurrentTotalTus(object archive)
        {
            object? dateNow = ReadMember(archive, "DateNow");
            object? timeData = ReadMember(archive, "timeData");
            dateNow ??= timeData == null ? null : ReadMember(timeData, "dateNow");
            return dateNow == null ? -1 : ReadIntMember(dateNow, "TotalTUs", -1);
        }

        private static int GetCurrentTuMinutes(object archive)
        {
            object? timeData = ReadMember(archive, "timeData");
            object? dateConfig = timeData == null ? null : ReadMember(timeData, "dateConfig");
            if (dateConfig != null)
                return Math.Max(1, ReadIntMember(dateConfig, "TU2Min", 10));

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
            object? globalDateConfig = globalParameter == null ? null : ReadMember(globalParameter, "DateConfig");
            return globalDateConfig == null ? 10 : Math.Max(1, ReadIntMember(globalDateConfig, "TU2Min", 10));
        }

        private static int GetMachineCycleTus(MachineDefinition definition, int tuMinutes)
        {
            tuMinutes = Math.Max(1, tuMinutes);
            return Math.Max(1, (int)Math.Ceiling(Math.Max(1, definition.CycleMinutes) / (double)tuMinutes));
        }

        private static string BuildMachineRuntimeKey(string ownerId, MachineDefinition definition, object equipment)
        {
            int index = ReadIntMember(equipment, "index", -1);
            if (index < 0)
                index = equipment.GetHashCode();
            object? room = ReadMember(equipment, "CurrentRoom") ?? ReadMember(equipment, "Host");
            string roomId = room == null ? "unknown-room" : FirstText(ReadStringMember(room, "RoomId"), ReadStringMember(room, "SceneRawName"), room.GetType().Name);
            return ownerId + "|" + definition.MachineId + "|" + definition.EquipmentId + "|" + roomId + "|" + index.ToString(CultureInfo.InvariantCulture);
        }

        private static IEnumerable<object> EnumerateEquipments(object room)
        {
            var visitedRooms = new HashSet<object>();
            foreach (object equipment in EnumerateEquipments(room, visitedRooms))
                yield return equipment;
        }

        private static IEnumerable<object> EnumerateEquipments(object room, HashSet<object> visitedRooms)
        {
            if (room == null || !visitedRooms.Add(room))
                yield break;

            object? equipmentManager = ReadMember(room, "DM_equipment");
            object? allEquipments = equipmentManager == null ? null : ReadMember(equipmentManager, "AllEquipments");
            if (allEquipments is IEnumerable enumerable)
            {
                foreach (object? equipment in enumerable)
                {
                    if (equipment != null)
                        yield return equipment;
                }
            }

            object? buildingManager = ReadMember(room, "DM_building");
            object? buildings = buildingManager == null ? null : ReadMember(buildingManager, "Buildings");
            if (!(buildings is IEnumerable buildingEnumerable))
                yield break;

            foreach (object? building in buildingEnumerable)
            {
                if (building == null)
                    continue;
                object? childRoom = ReadMember(building, "room");
                if (childRoom == null)
                    continue;
                foreach (object childEquipment in EnumerateEquipments(childRoom, visitedRooms))
                    yield return childEquipment;
            }
        }

        private void UpdateActionSpeedAutoFill()
        {
            if (SuppressActionSpeedAutoFillForSmoke)
                return;

            if (!TryFindActionSpeedAutoFillPolicy(out string ownerId, out ActionSpeedOptions options))
                return;

            double cooldownSeconds = GetActionSpeedAutoFillCooldownSeconds(options);
            if ((DateTimeOffset.Now - lastActionSpeedAutoFillAt).TotalSeconds < cooldownSeconds)
                return;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (!ReadStaticBoolMember(dolocApi, "IsNormalState", false) || !ReadStaticBoolMember(dolocApi, "IsAgentInWater", false))
                return;

            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            if (!IsEmptyBottleItem(selectedItem, dolocApi))
                return;

            object? agent = ReadStaticMember(dolocApi, "agent");
            if (agent != null && !ReadBoolMember(agent, "IsCurrentStateSupportInteract", true))
                return;

            MethodInfo? useAsItem = selectedItem?.GetType().GetMethod("UseAsItem", BindingFlags.Public | BindingFlags.Instance);
            if (useAsItem == null)
                return;

            lastActionSpeedAutoFillAt = DateTimeOffset.Now;
            try
            {
                useAsItem.Invoke(selectedItem, null);
                actionSpeedAutoFillApplications++;
                double normalCooldownSeconds = options.AutoFillCooldownSeconds;
                double strongCooldownSeconds = Math.Min(options.AutoFillCooldownSeconds, options.AutoFillStrongCooldownSeconds);
                LastActionSpeedAutoFillSummary = "owner=" + ownerId + ", behavior=AutoFillBottle, item=" + ReadStringMember(selectedItem!, "name") + ", inWater=true, strong=" + options.AutoFillStrong + ", cooldownSeconds=" + cooldownSeconds.ToString("0.###") + ", normalCooldownSeconds=" + normalCooldownSeconds.ToString("0.###") + ", strongCooldownSeconds=" + strongCooldownSeconds.ToString("0.###") + ", applications=" + actionSpeedAutoFillApplications;
                LastActionSpeedApplicationSummary = LastActionSpeedAutoFillSummary;
                if (options.VerboseLogging || actionSpeedAutoFillApplications == 1)
                    runtime.RuntimeMonitor.Log("ActionSpeed auto-fill invoked native ItemBottle.UseAsItem by " + ownerId + " summary=" + LastActionSpeedAutoFillSummary + ".");
                runtime.SetHookStatus("Smoke.ActionSpeedAutoFillBottle", "experimental", "ItemBottle.UseAsItem native path", LastActionSpeedAutoFillSummary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "ActionSpeed auto-fill bottle failed.", ex.ToString());
            }
        }

        private bool TryFindActionSpeedAutoFillPolicy(out string ownerId, out ActionSpeedOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (candidate.Enabled && candidate.AutoFillBottle)
                {
                    ownerId = entry.Key;
                    options = candidate;
                    return true;
                }
            }
            return false;
        }

        private void UpdateFishingAutoCast()
        {
            if ((DateTimeOffset.Now - lastFishingAutoCastAt).TotalSeconds < 0.5)
            {
                LastFishingAutoCastAttemptSummary = "skipped=throttle";
                return;
            }

            if (!TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
            {
                LastFishingAutoCastAttemptSummary = "skipped=no-enabled-policy";
                return;
            }

            if (SuppressFishingAutoCastForSmoke && !ForceFishingNoWaterForSmoke && !ForceFishingNoRodForSmoke)
            {
                LastFishingAutoCastAttemptSummary = "skipped=smoke-suppressed";
                return;
            }

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            if (!ReadStaticBoolMember(dolocApi, "IsNormalState", false))
            {
                state.Phase = "WaitingNormalState";
                state.LastReason = "auto:game-state";
                LastFishingAutoCastAttemptSummary = "skipped=not-normal-state owner=" + ownerId;
                return;
            }

            object? agent = ReadStaticMember(dolocApi, "agent");
            if (agent != null && !ReadBoolMember(agent, "IsCurrentStateSupportUseItem", true))
            {
                state.Phase = "WaitingAction";
                state.LastReason = "auto:busy";
                LastFishingAutoCastAttemptSummary = "skipped=busy owner=" + ownerId + ", state=" + DescribeAgentState(dolocApi);
                return;
            }

            bool forcedNoWater = ForceFishingNoWaterForSmoke;
            object? smokeFishingPool = FishingPoolOverrideForSmoke;
            if (forcedNoWater || (smokeFishingPool == null && !HasFishingPoolInScene()))
            {
                state.Phase = "NoWater";
                state.LastReason = forcedNoWater ? "smoke:no-fishing-pool" : "auto:no-fishing-pool";
                if (forcedNoWater)
                {
                    ForceFishingNoWaterForSmoke = false;
                    runtime.SetHookStatus("Smoke.AutoFishingNoWaterFeedback", "verified", "0.2.3 toast policy", "No fishable-water state reached; player toast intentionally suppressed.");
                }
                LastFishingAutoCastAttemptSummary = "skipped=no-water owner=" + ownerId + ", forced=" + forcedNoWater;
                return;
            }

            bool forcedNoRod = ForceFishingNoRodForSmoke;
            object? rod = forcedNoRod ? null : ResolveFishingRodForAutomation(dolocApi, options);
            if (rod == null)
            {
                state.Phase = "NoRod";
                state.LastReason = forcedNoRod ? "smoke:no-rod" : (options.RequireSelectedFishingRod ? "auto:no-selected-rod" : "auto:no-rod");
                if (forcedNoRod)
                {
                    ForceFishingNoRodForSmoke = false;
                    runtime.SetHookStatus("Smoke.AutoFishingNoRodFeedback", "verified", "0.2.3 toast policy", "No fishing-rod state reached; player toast intentionally suppressed.");
                }
                LastFishingAutoCastAttemptSummary = "skipped=no-rod owner=" + ownerId + ", forced=" + forcedNoRod + ", requireSelected=" + options.RequireSelectedFishingRod;
                return;
            }

            MethodInfo? useFishRod = agent?.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "UseFishRod" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType.IsAssignableFrom(rod.GetType()));
            if (useFishRod == null)
            {
                LastFishingAutoCastAttemptSummary = "skipped=missing-UseFishRod owner=" + ownerId + ", agent=" + (agent == null ? "null" : agent.GetType().FullName) + ", rod=" + rod.GetType().FullName;
                return;
            }

            lastFishingAutoCastAt = DateTimeOffset.Now;
            try
            {
                useFishRod.Invoke(agent, new[] { rod });
                if (smokeFishingPool != null)
                {
                    object? cache = agent == null ? null : ReadMember(agent, "FishingCache");
                    if (cache != null)
                        SetMemberValue(cache, "FishingPool", smokeFishingPool);
                }
                fishingAutoCastApplications++;
                state.Phase = "AutoCast";
                state.LastReason = "auto:UseFishRod";
                string smokePoolName = smokeFishingPool == null ? "none" : FirstText(ReadStringMember(smokeFishingPool, "PoolName"), smokeFishingPool.GetType().Name);
                LastFishingAutomationApplicationSummary = "owner=" + ownerId + ", behavior=AutoCast, rod=" + ReadStringMember(rod, "name") + ", smokePoolOverride=" + smokePoolName + ", applications=" + fishingAutoCastApplications;
                LastFishingAutoCastAttemptSummary = LastFishingAutomationApplicationSummary;
                runtime.RuntimeMonitor.Log("Fishing automation auto-cast invoked native BodyController.UseFishRod summary=" + LastFishingAutomationApplicationSummary + ".");
                runtime.SetHookStatus("Smoke.AutoFishingAutoCast", "experimental", "BodyController.UseFishRod", LastFishingAutomationApplicationSummary);
            }
            catch (Exception ex)
            {
                state.Phase = "AutoCastFailed";
                state.LastReason = ex.GetType().Name;
                LastFishingAutoCastAttemptSummary = "failed=" + ex.GetType().Name + ": " + ex.Message;
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Fishing auto-cast failed.", ex.ToString());
            }
        }

        internal void NotifyFishingPhase(string phase, object? source)
        {
            if (string.IsNullOrWhiteSpace(phase) || fishingStates.Count == 0)
                return;

            string sourceName = source?.GetType().Name ?? "FishingState";
            foreach (KeyValuePair<string, FishingAutomationState> entry in fishingStates)
            {
                FishingAutomationState state = entry.Value ?? new FishingAutomationState();
                state.Phase = phase;
                state.LastReason = "hook:" + sourceName;
            }

            if (phase.Equals("Cast", StringComparison.OrdinalIgnoreCase) || phase.Equals("Pull", StringComparison.OrdinalIgnoreCase))
                TryApplyFishingAnimationSpeed(phase, source);

            LogOnce(loggedFishingPhases, phase, "Fishing phase hook observed phase=" + phase + " source=" + sourceName + ".");
        }

        internal void NotifyFishingMiniGameStart(object? gameHandle)
        {
            if (gameHandle != null)
                fishingMiniGameStartedAt[gameHandle] = DateTimeOffset.Now;
            NotifyFishingPhase("MiniGame", gameHandle);
        }

        internal void ApplyFishingMiniGameAutomationTick(object? gameHandle)
        {
            if (gameHandle == null)
                return;
            if (!fishingMiniGameStartedAt.ContainsKey(gameHandle))
                fishingMiniGameStartedAt[gameHandle] = DateTimeOffset.Now;
            TryApplyFishingMiniGameAutomation(gameHandle);
        }

        internal void NotifyFishingMiniGameStop(object? gameHandle)
        {
            if (gameHandle != null)
                fishingMiniGameStartedAt.Remove(gameHandle);
            NotifyFishingPhase("MiniGameStop", gameHandle);
        }

        internal bool ApplyFishingWaitAutomation(object waitState)
        {
            if (waitState == null || fishingStates.Count == 0 || fishingOptions.Count == 0)
                return false;

            if (!ReadBoolMember(waitState, "_waitForFishBite", false))
                return false;

            if (!TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return false;

            if (!options.InstantBite)
                return false;

            try
            {
                MethodInfo? rollFish = FindMethodInHierarchy(waitState.GetType(), "RollFish", 0);
                if (rollFish == null)
                    return false;

                object? rolledValue = rollFish.Invoke(waitState, null);
                bool rolled = rolledValue is bool value && value;
                int rollAttempts = 1;
                if (ForceFishingFishForSmoke && options.AutoCompleteMiniGame && !options.SkipMiniGame)
                {
                    while ((!rolled || !TryReadFishingCacheFish(waitState, out _)) && rollAttempts < 100)
                    {
                        rolledValue = rollFish.Invoke(waitState, null);
                        rolled = rolledValue is bool retryValue && retryValue;
                        rollAttempts++;
                    }
                }
                if (!rolled)
                {
                    state.Phase = "Wait:AutoBiteFailed";
                    state.LastReason = "auto:InstantBite:no-fish";
                    runtime.SetHookStatus("Smoke.AutoFishingPhase", "failed", "AgentStateFishingWait.OnPlay Postfix", "InstantBite attempted but RollFish returned false.");
                    return false;
                }
                bool forceFishSatisfied = !ForceFishingFishForSmoke || !options.AutoCompleteMiniGame || options.SkipMiniGame || TryReadFishingCacheFish(waitState, out _);

                WriteBoolMember(waitState, "_waitForFishBite", false);
                WriteBoolMember(waitState, "_hasRolled", true);
                WriteFloatMember(waitState, "_hookProbability", 1f);
                WriteFloatMember(waitState, "_fishOnHookDuration", 100f);
                TryRefreshFishingRendererAfterBite(waitState);
                string autoHook = TryAdvanceFishingBite(waitState, options);

                state.Phase = string.IsNullOrWhiteSpace(autoHook) ? "Wait:AutoBite" : "Wait:AutoBite:" + autoHook;
                state.LastReason = string.IsNullOrWhiteSpace(autoHook) ? "auto:InstantBite" : "auto:InstantBite+" + autoHook;
                FishingAutomationApplicationCount++;

                object? body = ReadMember(waitState, "body");
                object? cache = body == null ? null : ReadMember(body, "FishingCache");
                object? fishProto = cache == null ? null : ReadMember(cache, "FishProto");
                object? pool = cache == null ? null : ReadMember(cache, "FishingPool");
                string fishId = fishProto == null ? "unknown" : ReadStringMember(fishProto, "Id");
                string poolName = pool == null ? "unknown" : ReadStringMember(pool, "PoolName");
                bool isFish = fishProto != null && ReadBoolMember(fishProto, "IsFish", false);
                LastFishingAutomationApplicationSummary = "owner=" + ownerId + ", behavior=InstantBite, phase=Wait, autoHook=" + FirstText(autoHook, "none") + ", fish=" + fishId + ", isFish=" + isFish + ", pool=" + poolName + ", autoCompleteMiniGame=" + options.AutoCompleteMiniGame + ", skipMiniGame=" + options.SkipMiniGame + ", forceFishForSmoke=" + ForceFishingFishForSmoke + ", forceFishSatisfied=" + forceFishSatisfied + ", rollAttempts=" + rollAttempts + ", applications=" + FishingAutomationApplicationCount;

                if (options.VerboseLogging || !loggedFishingPhases.Contains("AutoBite:" + ownerId))
                    runtime.RuntimeMonitor.Log("Fishing automation instant-bite applied by " + ownerId + " fish=" + fishId + " pool=" + poolName + ".");
                loggedFishingPhases.Add("AutoBite:" + ownerId);
                runtime.SetHookStatus("Smoke.AutoFishingPhase", "verified", "AgentStateFishingWait.OnPlay Postfix", LastFishingAutomationApplicationSummary);
                if (options.AutoCompleteMiniGame && options.SkipMiniGame && autoHook.Equals("AgentStateFishingPull", StringComparison.OrdinalIgnoreCase))
                    runtime.SetHookStatus("Smoke.AutoFishingMiniGameSkip", "verified", "AgentStateFishingWait.OnPlay -> AgentStateFishingPull", LastFishingAutomationApplicationSummary);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Fishing wait automation failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AutoFishingPhase", "failed", "AgentStateFishingWait.OnPlay Postfix", ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        private static bool TryReadFishingCacheFish(object waitState, out object? fishProto)
        {
            fishProto = null;
            object? body = ReadMember(waitState, "body");
            object? cache = body == null ? null : ReadMember(body, "FishingCache");
            fishProto = cache == null ? null : ReadMember(cache, "FishProto");
            return fishProto != null && ReadBoolMember(fishProto, "IsFish", false);
        }

        private bool TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state)
        {
            ownerId = string.Empty;
            options = null!;
            state = null!;
            foreach (KeyValuePair<string, FishingAutomationState> entry in fishingStates)
            {
                FishingAutomationState candidateState = entry.Value ?? new FishingAutomationState();
                if (!candidateState.Enabled)
                    continue;
                if (!fishingOptions.TryGetValue(entry.Key, out FishingAutomationOptions candidateOptions))
                    continue;
                ownerId = entry.Key;
                options = candidateOptions ?? new FishingAutomationOptions();
                state = candidateState;
                return true;
            }
            return false;
        }

        private static FishingAutomationOptions NormalizeFishingAutomationOptions(FishingAutomationOptions? options)
        {
            options ??= new FishingAutomationOptions();
            options.AutoRecast = true;
            options.RequireSelectedFishingRod = true;
            options.CastReleaseProgress = Math.Min(1, Math.Max(0, options.CastReleaseProgress));
            options.RecastDelaySeconds = ClampSeconds(options.RecastDelaySeconds, 0.05, 10);
            options.FastAnimationMultiplier = ClampMultiplier(options.FastAnimationMultiplier);
            if (!options.AutoCompleteMiniGame)
                options.SkipMiniGame = false;
            return options;
        }

        private static MachineDefinition NormalizeMachineDefinition(IManifest owner, MachineDefinition? definition)
        {
            definition ??= new MachineDefinition();
            var normalized = new MachineDefinition
            {
                MachineId = FirstText(definition.MachineId, definition.EquipmentId, owner.UniqueID + ".machine"),
                DisplayName = FirstText(definition.DisplayName, definition.MachineId, definition.EquipmentId, "DTMAPI Machine"),
                ItemId = FirstText(definition.ItemId, definition.EquipmentId, definition.MachineId),
                EquipmentId = FirstText(definition.EquipmentId, definition.ItemId, definition.MachineId),
                RecipeId = FirstText(definition.RecipeId, definition.ItemId, definition.EquipmentId, definition.MachineId),
                RecipeGroupId = FirstText(definition.RecipeGroupId, "equipment_workbench"),
                VisualScale = ClampDouble(definition.VisualScale <= 0 ? 1 : definition.VisualScale, 0.25, 4),
                AllowFuelMode = definition.AllowFuelMode,
                AllowElectricMode = definition.AllowElectricMode,
                DefaultMode = FirstText(definition.DefaultMode, "fuel"),
                NativeTechTreeId = definition.NativeTechTreeId ?? string.Empty,
                NativeTechNodeId = definition.NativeTechNodeId ?? string.Empty,
                NativeTechNodeTitle = definition.NativeTechNodeTitle ?? string.Empty,
                NativeTechNodeDescription = definition.NativeTechNodeDescription ?? string.Empty,
                NativeTechNodeParentId = definition.NativeTechNodeParentId ?? string.Empty,
                NativeTechNodeAboveTitleContains = definition.NativeTechNodeAboveTitleContains ?? string.Empty,
                FuelCapacity = ClampInt(definition.FuelCapacity, 1, 999999),
                FuelOnlyFuelCostPerCycle = ClampInt(definition.FuelOnlyFuelCostPerCycle, 0, 999999),
                ElectricModeFuelCostPerCycle = ClampInt(definition.ElectricModeFuelCostPerCycle, 0, 999999),
                ElectricModePowerCostPerCycle = ClampInt(definition.ElectricModePowerCostPerCycle, 0, 999999),
                CycleMinutes = ClampInt(definition.CycleMinutes, 5, 1440),
                IncludeRuntimeModMinerals = definition.IncludeRuntimeModMinerals,
                VerboseLogging = definition.VerboseLogging
            };
            if (!normalized.AllowFuelMode && !normalized.AllowElectricMode)
                normalized.AllowFuelMode = true;
            if (!normalized.DefaultMode.Equals("electric", StringComparison.OrdinalIgnoreCase))
                normalized.DefaultMode = "fuel";
            if (normalized.DefaultMode.Equals("electric", StringComparison.OrdinalIgnoreCase) && !normalized.AllowElectricMode)
                normalized.DefaultMode = "fuel";
            if (definition.OutputRules != null)
            {
                normalized.OutputRules = definition.OutputRules
                    .Where(rule => rule != null && !string.IsNullOrWhiteSpace(rule.ItemId))
                    .Select(rule => new MachineOutputRule
                    {
                        ItemId = rule.ItemId,
                        DisplayName = rule.DisplayName ?? string.Empty,
                        Weight = ClampDouble(rule.Weight <= 0 ? 1 : rule.Weight, 0.0001, 100000),
                        MinCount = ClampInt(rule.MinCount, 1, 9999),
                        MaxCount = ClampInt(Math.Max(rule.MaxCount, rule.MinCount), 1, 9999),
                        Source = FirstText(rule.Source, "default"),
                        AllowProbabilityOverride = rule.AllowProbabilityOverride
                    })
                    .ToArray();
            }
            normalized.ProbabilityOverrides = definition.ProbabilityOverrides ?? new Dictionary<string, double>();
            return normalized;
        }

        private static EquipmentSlotsOptions NormalizeEquipmentSlotsOptions(EquipmentSlotsOptions? options)
        {
            options ??= new EquipmentSlotsOptions();
            return new EquipmentSlotsOptions
            {
                Enabled = options.Enabled,
                ExtraAttributeSlots = ClampInt(options.ExtraAttributeSlots, 0, 24),
                SlotIdPrefix = FirstText(options.SlotIdPrefix, "dtmapi.extra"),
                PreserveVanillaVisualSlots = options.PreserveVanillaVisualSlots,
                ExtraSlotsAffectVisuals = false,
                SafeUnequipOnDisable = options.SafeUnequipOnDisable,
                AutoRecoverOnMissingMod = options.AutoRecoverOnMissingMod,
                VerboseLogging = options.VerboseLogging
            };
        }

        private static int ClampInt(int value, int min, int max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static double ClampDouble(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        private static void TryRefreshFishingRendererAfterBite(object waitState)
        {
            object? body = ReadMember(waitState, "body");
            object? renderer = body == null ? null : ReadMember(body, "fishRodRenderer");
            object? line = renderer == null ? null : ReadMember(renderer, "Line");
            line?.GetType().GetMethod("UseStraightLine", BindingFlags.Public | BindingFlags.Instance)?.Invoke(line, null);
            renderer?.GetType().GetMethod("EnableFishShadow", BindingFlags.Public | BindingFlags.Instance)?.Invoke(renderer, null);
        }

        private static string TryAdvanceFishingBite(object waitState, FishingAutomationOptions options)
        {
            object? body = ReadMember(waitState, "body");
            object? stateManager = body == null ? null : ReadMember(body, "StateManager");
            object? cache = body == null ? null : ReadMember(body, "FishingCache");
            object? fishProto = cache == null ? null : ReadMember(cache, "FishProto");
            bool isFish = fishProto != null && ReadBoolMember(fishProto, "IsFish", false);
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
            int energy = globalParameter == null ? 0 : ReadIntMember(globalParameter, "FishingEnergyCost", 0);
            MethodInfo? costEnergy = dolocApi?.GetMethod("CostEnergy", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
            if (energy > 0)
                costEnergy?.Invoke(null, new object[] { energy });

            string targetTypeName = isFish && !(options.AutoCompleteMiniGame && options.SkipMiniGame)
                ? "DolocTown.AgentStateFishingBattle, Assembly-CSharp"
                : "DolocTown.AgentStateFishingPull, Assembly-CSharp";
            Type? targetType = ResolveType(targetTypeName);
            if (stateManager == null || targetType == null)
                return string.Empty;

            MethodInfo? getStateOpen = stateManager.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "GetState" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            object? targetState = getStateOpen?.MakeGenericMethod(targetType).Invoke(stateManager, null);
            if (targetState == null)
                return string.Empty;

            if (targetType.FullName == "DolocTown.AgentStateFishingPull")
                WriteBoolMember(targetState, "IsFailed", false);

            MethodInfo? overwrite = stateManager.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "Overwrite" && !m.IsGenericMethodDefinition && m.GetParameters().Length >= 1);
            if (overwrite == null)
                return string.Empty;

            ParameterInfo[] parameters = overwrite.GetParameters();
            object?[] args = parameters.Length >= 2 ? new object?[] { targetState, true } : new object?[] { targetState };
            overwrite.Invoke(stateManager, args);
            return targetType.Name;
        }

        private void TryApplyFishingAnimationSpeed(string phase, object? stateSource)
        {
            if (stateSource == null || !TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return;
            if (!options.FastAnimations || options.FastAnimationMultiplier <= 1)
                return;

            object? body = ReadMember(stateSource, "body");
            if (body == null)
                return;
            double multiplier = ClampMultiplier(options.FastAnimationMultiplier);
            var samples = new List<string>();
            int changed = ApplyAnimatorSpeed(ReadMember(body, "animator"), multiplier, "body", samples);
            object? fishRodRenderer = ReadMember(body, "fishRodRenderer");
            if (fishRodRenderer != null)
                changed += ApplyAnimatorSpeed(ReadMember(fishRodRenderer, "_animator"), multiplier, "fishRodRenderer", samples);
            if (changed <= 0)
                return;

            FishingAutomationApplicationCount++;
            state.Phase = phase + ":FastAnimation";
            state.LastReason = "auto:FastAnimation";
            LastFishingAutomationApplicationSummary = "owner=" + ownerId + ", behavior=FastAnimation, phase=" + phase + ", multiplier=" + multiplier.ToString("0.###") + ", animators=" + changed + ", samples=" + string.Join(";", samples.ToArray()) + ", applications=" + FishingAutomationApplicationCount;
            runtime.RuntimeMonitor.Log("Fishing automation animation speed applied by " + ownerId + " phase=" + phase + " multiplier=" + multiplier.ToString("0.###") + " animators=" + changed + ".");
            runtime.SetHookStatus("Smoke.AutoFishingAnimationSpeed", "experimental", "AgentStateFishingCast/Pull.OnEnter", LastFishingAutomationApplicationSummary);
        }

        private void TryApplyFishingMiniGameAutomation(object? gameHandle)
        {
            if (gameHandle == null || !TryFindEnabledFishingOptions(out string ownerId, out FishingAutomationOptions options, out FishingAutomationState state))
                return;
            if (!options.AutoCompleteMiniGame || options.SkipMiniGame)
                return;

            try
            {
                FieldInfo? statusField = null;
                for (Type? type = gameHandle.GetType(); type != null && statusField == null; type = type.BaseType)
                    statusField = type.GetField("currentGameStatus", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (statusField == null || !statusField.FieldType.IsEnum)
                    return;

                object? currentStatus = statusField.GetValue(gameHandle);
                if (currentStatus != null && !currentStatus.ToString()!.Equals("Running", StringComparison.OrdinalIgnoreCase))
                {
                    fishingMiniGameStartedAt.Remove(gameHandle);
                    return;
                }

                if (!fishingMiniGameStartedAt.TryGetValue(gameHandle, out DateTimeOffset startedAt))
                {
                    fishingMiniGameStartedAt[gameHandle] = DateTimeOffset.Now;
                    return;
                }

                double visibleSeconds = (DateTimeOffset.Now - startedAt).TotalSeconds;
                if (visibleSeconds < 0.75)
                {
                    state.Phase = "MiniGame:Running";
                    state.LastReason = "auto:CompleteMiniGame:waiting";
                    return;
                }

                object success = Enum.Parse(statusField.FieldType, "Success");
                statusField.SetValue(gameHandle, success);
                fishingMiniGameStartedAt.Remove(gameHandle);
                FishingAutomationApplicationCount++;
                FishingMiniGameCompleteApplicationCount++;
                state.Phase = "MiniGame:AutoComplete";
                state.LastReason = "auto:CompleteMiniGame";
                LastFishingAutomationApplicationSummary = "owner=" + ownerId + ", behavior=AutoCompleteMiniGame, status=Success, skip=false, visibleSeconds=" + visibleSeconds.ToString("0.###", CultureInfo.InvariantCulture) + ", applications=" + FishingAutomationApplicationCount;
                LastFishingMiniGameCompleteSummary = LastFishingAutomationApplicationSummary;
                runtime.RuntimeMonitor.Log("Fishing automation completed minigame status by " + ownerId + " through delayed FishingGameScrollBar.UpdateGame currentGameStatus=Success visibleSeconds=" + visibleSeconds.ToString("0.###", CultureInfo.InvariantCulture) + ".");
                runtime.SetHookStatus("Smoke.AutoFishingMiniGameComplete", "verified", "FishingGameScrollBar.UpdateGame Postfix", LastFishingAutomationApplicationSummary);
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Fishing minigame completion failed.", ex.ToString());
                runtime.SetHookStatus("Smoke.AutoFishingMiniGameComplete", "failed", "FishingGameScrollBar.UpdateGame Postfix", ex.GetType().Name + ": " + ex.Message);
            }
        }

        private bool TryLookupFishRoe(string ownerId, string fishId, out FishRoeDisplayInfo info)
        {
            info = null!;
            if (!fishRoeLookups.TryGetValue(ownerId ?? string.Empty, out Func<string, FishRoeDisplayInfo?> lookup) || string.IsNullOrWhiteSpace(fishId))
                return false;
            try
            {
                FishRoeDisplayInfo? lookedUp = lookup(fishId);
                if (lookedUp == null)
                    return false;
                info = lookedUp;
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Fish roe provider failed for " + ownerId + "/" + fishId + ".", ex.ToString());
                return false;
            }
        }

        private bool TryFindActionPolicy(object resource, out string ownerId, out ActionCompletionOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            Type resourceType = resource.GetType();
            string typeName = resourceType.FullName ?? resourceType.Name;
            string resourceClass = GetResourceClass(resource);

            foreach (KeyValuePair<string, ActionCompletionOptions> entry in actionOptions)
            {
                ActionCompletionOptions candidate = entry.Value ?? new ActionCompletionOptions();
                if (!candidate.Enabled)
                    continue;

                bool matches =
                    (candidate.CompleteTrees && IsTreeResource(resourceType)) ||
                    (candidate.CompleteOres && (IsTypeOrBase(resourceType, "DolocTown.DungeonResourceOre") || resourceClass.Equals("Ore", StringComparison.OrdinalIgnoreCase))) ||
                    (candidate.CompleteGarbage && (typeName.IndexOf("Garbage", StringComparison.OrdinalIgnoreCase) >= 0 || resourceClass.Equals("Garbage", StringComparison.OrdinalIgnoreCase))) ||
                    (candidate.CompleteWeeds && IsTypeOrBase(resourceType, "DolocTown.DungeonResourceWeeds"));

                if (!matches)
                    continue;

                ownerId = entry.Key;
                options = candidate;
                return true;
            }

            return false;
        }

        private bool TryFindOneActionFuelPolicy(out string ownerId, out ActionCompletionOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            foreach (KeyValuePair<string, ActionCompletionOptions> entry in actionOptions)
            {
                ActionCompletionOptions candidate = entry.Value ?? new ActionCompletionOptions();
                if (!candidate.Enabled || !candidate.CompleteMachineFuel)
                    continue;

                ownerId = entry.Key;
                options = candidate;
                return true;
            }
            return false;
        }

        private bool TryFindOneActionFeederPolicy(out string ownerId, out ActionCompletionOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            foreach (KeyValuePair<string, ActionCompletionOptions> entry in actionOptions)
            {
                ActionCompletionOptions candidate = entry.Value ?? new ActionCompletionOptions();
                if (!candidate.Enabled || !candidate.CompleteFeeder)
                    continue;

                ownerId = entry.Key;
                options = candidate;
                return true;
            }
            return false;
        }

        private static bool IsTreeResource(Type type)
        {
            return IsTypeOrBase(type, "DolocTown.DungeonResourceTree") ||
                IsTypeOrBase(type, "DolocTown.DungeonResourceTreeTrunk");
        }

        private bool TryFindActionSpeedToolPolicy(out string ownerId, out ActionSpeedOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (!candidate.Enabled || !candidate.ToolSpeedEnabled || candidate.ToolMultiplier <= 1)
                    continue;

                ownerId = entry.Key;
                options = candidate;
                return true;
            }
            return false;
        }

        private bool TryFindActionSpeedEatPolicy(out string ownerId, out ActionSpeedOptions options)
        {
            ownerId = string.Empty;
            options = null!;
            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (!candidate.Enabled || !candidate.EatDrinkSpeedEnabled || candidate.EatDrinkMultiplier <= 1)
                    continue;

                ownerId = entry.Key;
                options = candidate;
                return true;
            }
            return false;
        }

        private bool TryClassifyActionSpeedInteraction(out string ownerId, out ActionSpeedOptions options, out string kind, out double multiplier, out string target)
        {
            ownerId = string.Empty;
            options = null!;
            kind = string.Empty;
            multiplier = 1;
            target = string.Empty;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            object? selectedEquipment = ReadStaticMember(dolocApi, "SelectedEquipment");
            object? currentInteractable = ReadCurrentActionSpeedInteractable(dolocApi);
            bool isInWater = ReadStaticBoolMember(dolocApi, "IsAgentInWater", false);

            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (!candidate.Enabled)
                    continue;

                if (candidate.BottleFillSpeedEnabled && candidate.BottleFillMultiplier > 1 && IsBottleFillInteraction(selectedItem, selectedEquipment, isInWater))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "BottleFill";
                    multiplier = candidate.BottleFillMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }

                if (candidate.PlantSpeedEnabled && candidate.PlantMultiplier > 1 && IsPlantInteraction(selectedItem, selectedEquipment))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "Plant";
                    multiplier = candidate.PlantMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }

                if (candidate.MachineAddSpeedEnabled && candidate.MachineAddMultiplier > 1 && IsMachineAddInteraction(selectedEquipment))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "MachineAdd";
                    multiplier = candidate.MachineAddMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }

                if (candidate.HarvestSpeedEnabled && candidate.HarvestMultiplier > 1 && IsHarvestInteraction(selectedEquipment, currentInteractable))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "Harvest";
                    multiplier = candidate.HarvestMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }
            }

            return false;
        }

        private bool TryClassifyActionSpeedUseItem(out string ownerId, out ActionSpeedOptions options, out string kind, out double multiplier, out string target)
        {
            ownerId = string.Empty;
            options = null!;
            kind = string.Empty;
            multiplier = 1;
            target = string.Empty;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            if (selectedItem == null)
                return false;

            object? selectedEquipment = ReadStaticMember(dolocApi, "SelectedEquipment");
            object? currentInteractable = ReadCurrentActionSpeedInteractable(dolocApi);
            bool isInWater = ReadStaticBoolMember(dolocApi, "IsAgentInWater", false);

            foreach (KeyValuePair<string, ActionSpeedOptions> entry in actionSpeedOptions)
            {
                ActionSpeedOptions candidate = entry.Value ?? new ActionSpeedOptions();
                if (!candidate.Enabled)
                    continue;

                if ((candidate.BottleFillSpeedEnabled || candidate.AutoFillBottle) && candidate.BottleFillMultiplier > 1 && IsBottleFillInteraction(selectedItem, selectedEquipment, isInWater))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "BottleFill";
                    multiplier = candidate.BottleFillMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }

                if (candidate.ContinuousDrinkWithRightClick && candidate.EatDrinkMultiplier > 1 && IsBottledWaterItem(selectedItem, dolocApi))
                {
                    ownerId = entry.Key;
                    options = candidate;
                    kind = "BottledWaterDrink";
                    multiplier = candidate.EatDrinkMultiplier;
                    target = DescribeActionSpeedTarget(selectedItem, selectedEquipment, currentInteractable);
                    return true;
                }
            }

            return false;
        }

        private static ActionSpeedOptions NormalizeActionSpeedOptions(ActionSpeedOptions? options)
        {
            options ??= new ActionSpeedOptions();
            options.ToolMultiplier = ClampMultiplier(options.ToolMultiplier);
            options.BottleFillMultiplier = ClampMultiplier(options.BottleFillMultiplier);
            options.EatDrinkMultiplier = ClampMultiplier(options.EatDrinkMultiplier);
            options.MachineAddMultiplier = ClampMultiplier(options.MachineAddMultiplier);
            options.HarvestMultiplier = ClampMultiplier(options.HarvestMultiplier);
            options.PlantMultiplier = ClampMultiplier(options.PlantMultiplier);
            options.AutoFillCooldownSeconds = ClampSeconds(options.AutoFillCooldownSeconds, 0.05, 5);
            options.AutoFillStrongCooldownSeconds = ClampSeconds(options.AutoFillStrongCooldownSeconds, 0.03, 5);
            if (!options.AutoFillBottle)
                options.AutoFillStrong = false;
            return options;
        }

        private static double GetActionSpeedAutoFillCooldownSeconds(ActionSpeedOptions options)
        {
            return options.AutoFillStrong
                ? Math.Min(options.AutoFillCooldownSeconds, options.AutoFillStrongCooldownSeconds)
                : options.AutoFillCooldownSeconds;
        }

        private static bool IsAcceleratedTool(object? tool)
        {
            if (tool == null)
                return false;

            object? toolType = ReadMember(tool, "ToolType");
            string name = toolType?.ToString() ?? string.Empty;
            return name.Equals("AXE", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("PICKAXE", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("SICKLE", StringComparison.OrdinalIgnoreCase);
        }

        private int ApplyAnimatorSpeed(object? animator, double multiplier, string label, List<string> samples)
        {
            if (animator == null)
                return 0;

            double original = ReadAnimatorSpeed(animator, 1);
            if (!originalAnimatorSpeeds.ContainsKey(animator))
                originalAnimatorSpeeds[animator] = original;
            else
                original = originalAnimatorSpeeds[animator];

            double target = original * multiplier;
            if (!TryWriteAnimatorSpeed(animator, target))
                return 0;

            if (samples.Count < 6)
                samples.Add(label + ":" + original.ToString("0.###") + "->" + target.ToString("0.###"));
            return 1;
        }

        private bool ApplyActionSpeedToBody(object body, string ownerId, ActionSpeedOptions options, string kind, double multiplier, string target, string source, string hookId)
        {
            multiplier = ClampMultiplier(multiplier);
            if (multiplier <= 1)
                return false;

            int changed = 0;
            var samples = new List<string>();
            changed += ApplyAnimatorSpeed(ReadMember(body, "animator"), multiplier, "body", samples);

            if (changed <= 0)
                return false;

            ActionSpeedApplicationCount++;
            LastActionSpeedApplicationSummary = "owner=" + ownerId + ", kind=" + kind + ", target=" + target + ", multiplier=" + multiplier.ToString("0.###") + ", animators=" + changed + ", samples=" + string.Join(";", samples.ToArray());
            string logKey = ownerId + ":" + kind + ":" + target;
            if (options.VerboseLogging || !loggedActionSpeedApplications.Contains(logKey))
                runtime.RuntimeMonitor.Log("ActionSpeed interaction animation speed applied by " + ownerId + " kind=" + kind + " target=" + target + " multiplier=" + multiplier.ToString("0.###") + " animators=" + changed + ".");
            loggedActionSpeedApplications.Add(logKey);
            runtime.SetHookStatus(hookId, "experimental", source, LastActionSpeedApplicationSummary);
            return true;
        }

        private static bool IsBottleFillInteraction(object? selectedItem, object? selectedEquipment, bool isInWater)
        {
            if (selectedItem == null || !IsTypeOrBase(selectedItem.GetType(), "DolocTown.ItemBottle"))
                return false;
            return isInWater || ImplementsInterface(selectedEquipment?.GetType(), "DolocTown.IWaterContainer");
        }

        private static bool IsEmptyBottleItem(object? selectedItem, Type? dolocApi)
        {
            if (selectedItem == null || !IsTypeOrBase(selectedItem.GetType(), "DolocTown.ItemBottle"))
                return false;
            string itemName = ReadStringMember(selectedItem, "name");
            object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
            string wasteBottle = globalParameter == null ? string.Empty : ReadStringMember(globalParameter, "ItemRefWastePlasticBottle");
            return string.IsNullOrWhiteSpace(wasteBottle) || itemName.Equals(wasteBottle, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEatDrinkItem(object? selectedItem)
        {
            if (selectedItem == null)
                return false;

            Type itemType = selectedItem.GetType();
            if (ImplementsInterface(itemType, "DolocTown.IEatable"))
                return true;

            string typeName = itemType.FullName ?? itemType.Name;
            return typeName.IndexOf("ItemFood", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("ItemDrink", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsBottledWaterItem(object? selectedItem, Type? dolocApi)
        {
            if (selectedItem == null)
                return false;
            string itemName = ReadStringMember(selectedItem, "name");
            object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
            string bottleOfWater = globalParameter == null ? string.Empty : ReadStringMember(globalParameter, "ItemRefBottleOfWater");
            return !string.IsNullOrWhiteSpace(itemName) &&
                !string.IsNullOrWhiteSpace(bottleOfWater) &&
                itemName.Equals(bottleOfWater, StringComparison.OrdinalIgnoreCase);
        }

        private static object? ResolveFishingRodForAutomation(Type? dolocApi, FishingAutomationOptions options)
        {
            object? selectedItem = ReadStaticMember(dolocApi, "SelectedItem");
            if (selectedItem != null && IsTypeOrBase(selectedItem.GetType(), "DolocTown.ItemFishingRod"))
                return selectedItem;
            if (options.RequireSelectedFishingRod)
                return null;

            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? inventorySystem = archive == null ? null : ReadMember(archive, "InventorySystem");
            object? inventory = inventorySystem == null ? null : ReadMember(inventorySystem, "inventory");
            MethodInfo? readAll = inventory?.GetType().GetMethod("ReadAll", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            object? all = readAll?.Invoke(inventory, null);
            foreach (object item in EnumerateObjects(all))
            {
                if (IsTypeOrBase(item.GetType(), "DolocTown.ItemFishingRod"))
                    return item;
            }
            return null;
        }

        private static string DescribeAgentState(Type? dolocApi)
        {
            object? agent = ReadStaticMember(dolocApi, "agent");
            object? stateManager = agent == null ? null : ReadMember(agent, "StateManager");
            object? current = stateManager == null ? null : ReadMember(stateManager, "current");
            return current == null ? "unknown" : current.GetType().Name;
        }

        private static bool HasFishingPoolInScene()
        {
            try
            {
                Type? poolType = ResolveType("DolocTown.FishingPool, Assembly-CSharp");
                Type? objectType = ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Object, UnityEngine");
                MethodInfo? findObjects = objectType?.GetMethod("FindObjectsOfType", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Type) }, null);
                object? result = poolType == null ? null : findObjects?.Invoke(null, new object[] { poolType });
                if (result is Array array)
                    return array.Length > 0;
                return EnumerateObjects(result).Any();
            }
            catch
            {
                return true;
            }
        }

        private void ShowFishingFeedback(string message, bool error)
        {
            if ((DateTimeOffset.Now - lastFishingFeedbackAt).TotalSeconds < 1.25)
                return;
            lastFishingFeedbackAt = DateTimeOffset.Now;
            ShowNativeSmallMessage(message, error);
        }

        private static void ShowNativeSmallMessage(string message, bool error)
        {
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                string methodName = error ? "ShowMessageBoxSmallErr" : "ShowMessageBoxSmall";
                MethodInfo? method = dolocApi?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length >= 1 && m.GetParameters()[0].ParameterType == typeof(string));
                if (method == null)
                    return;
                ParameterInfo[] parameters = method.GetParameters();
                object?[] args = parameters.Length == 1
                    ? new object?[] { message }
                    : parameters.Length == 2
                        ? new object?[] { message, 1.2f }
                        : new object?[] { message, 1.2f, false };
                method.Invoke(null, args);
            }
            catch
            {
            }
        }

        private static bool IsPlantInteraction(object? selectedItem, object? selectedEquipment)
        {
            if (selectedItem == null || selectedEquipment == null)
                return false;
            if (!IsTypeOrBase(selectedItem.GetType(), "DolocTown.ItemSeed"))
                return false;
            Type equipmentType = selectedEquipment.GetType();
            if (ReadBoolMember(selectedEquipment, "IsPlanted", false) || ReadBoolMember(selectedEquipment, "CouldHarvest", false))
                return false;
            return IsTypeOrBase(equipmentType, "DolocTown.PlantBasin") || IsTypeOrBase(equipmentType, "DolocTown.FlowerPot");
        }

        private static bool IsMachineAddInteraction(object? selectedEquipment)
        {
            if (selectedEquipment == null)
                return false;
            Type type = selectedEquipment.GetType();
            return IsTypeOrBase(type, "DolocTown.PowerGeneratorFuel") || IsTypeOrBase(type, "DolocTown.Feeder");
        }

        private static bool IsHarvestInteraction(object? selectedEquipment, object? currentInteractable)
        {
            if (selectedEquipment != null)
            {
                Type equipmentType = selectedEquipment.GetType();
                if (IsTypeOrBase(equipmentType, "DolocTown.ResinCollector") && ReadIntMember(selectedEquipment, "currentValue", 0) > 0)
                    return true;
                if (IsTypeOrBase(equipmentType, "DolocTown.PlantBasin") && ReadBoolMember(selectedEquipment, "CouldHarvest", false))
                    return true;
                if (ImplementsInterface(equipmentType, "DolocTown.IGatherableEquipment"))
                    return true;
            }

            if (currentInteractable == null)
                return false;
            Type interactableType = currentInteractable.GetType();
            string name = interactableType.FullName ?? interactableType.Name;
            return name.IndexOf("Vegetation", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ImplementsInterface(interactableType, "DolocTown.IGatherableEquipment");
        }

        private static object? ReadCurrentActionSpeedInteractable(Type? dolocApi)
        {
            object? currentInteractable = ReadStaticMember(dolocApi, "CurrentInteractableObject");
            if (currentInteractable != null)
                return currentInteractable;

            object? gameStateManager = ReadStaticMember(dolocApi, "gameStateManager");
            object? normalGameState = gameStateManager == null ? null : ReadMember(gameStateManager, "normalGameState");
            object? agentController = normalGameState == null ? null : ReadMember(normalGameState, "AgentController");
            object? interactableManager = agentController == null ? null : ReadMember(agentController, "interactableManager");
            if (interactableManager == null)
                return null;

            object? baseManager = ReadMember(interactableManager, "baseManager");
            object? current = baseManager == null ? null : ReadMember(baseManager, "Current");
            if (current != null)
                return UnwrapActionSpeedInteractable(current);

            object? subManagers = ReadMember(interactableManager, "subManagers");
            if (subManagers is IDictionary dictionary)
            {
                foreach (object? manager in dictionary.Values)
                {
                    current = manager == null ? null : ReadMember(manager, "Current");
                    if (current != null)
                        return UnwrapActionSpeedInteractable(current);
                }
            }

            return null;
        }

        private static object? UnwrapActionSpeedInteractable(object? interactable)
        {
            if (interactable == null)
                return null;

            object? vegetation = ReadMember(interactable, "Vegetation");
            if (vegetation != null)
                return vegetation;

            object? equipment = ReadMember(interactable, "equipment");
            return equipment ?? interactable;
        }

        private static string DescribeActionSpeedTarget(object? selectedItem, object? selectedEquipment, object? currentInteractable)
        {
            string item = selectedItem == null ? "none" : FirstText(ReadStringMember(selectedItem, "name"), selectedItem.GetType().Name);
            string equipment = selectedEquipment == null ? "none" : FirstText(ReadStringMember(selectedEquipment, "equipmentName"), selectedEquipment.GetType().Name);
            string interactable = currentInteractable == null ? "none" : FirstText(ReadStringMember(currentInteractable, "VegetationName"), currentInteractable.GetType().Name);
            return "item=" + item + ",equipment=" + equipment + ",interactable=" + interactable;
        }

        private static double ReadAnimatorSpeed(object animator, double fallback)
        {
            object? value = animator.GetType().GetProperty("speed", BindingFlags.Public | BindingFlags.Instance)?.GetValue(animator);
            return value == null ? fallback : Convert.ToDouble(value);
        }

        private static bool TryWriteAnimatorSpeed(object animator, double value)
        {
            try
            {
                PropertyInfo? speed = animator.GetType().GetProperty("speed", BindingFlags.Public | BindingFlags.Instance);
                if (speed == null || !speed.CanWrite)
                    return false;
                speed.SetValue(animator, Convert.ChangeType(value, speed.PropertyType));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static double ClampMultiplier(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 1;
            return Math.Min(4, Math.Max(1, value));
        }

        private static double ClampSeconds(double value, double min, double max)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return min;
            return Math.Min(max, Math.Max(min, value));
        }

        private static object? TryGetDungeonResourceFromCollider(object collider)
        {
            Type? rendererType = ResolveType("DolocTown.DungeonResourceRenderer, Assembly-CSharp");
            if (rendererType == null)
                return null;

            MethodInfo? getComponent = null;
            foreach (MethodInfo method in collider.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.Name == "GetComponent" && method.IsGenericMethodDefinition && method.GetParameters().Length == 0)
                {
                    getComponent = method.MakeGenericMethod(rendererType);
                    break;
                }
            }

            object? renderer = getComponent?.Invoke(collider, null);
            return renderer?.GetType().GetProperty("DungeonResource", BindingFlags.Public | BindingFlags.Instance)?.GetValue(renderer);
        }

        private static bool IsResourceRemoved(object resource)
        {
            object? value = resource.GetType().GetProperty("IsRemoved", BindingFlags.Public | BindingFlags.Instance)?.GetValue(resource);
            return value is bool removed && removed;
        }

        private static string GetResourceName(object resource)
        {
            object? value = resource.GetType().GetProperty("ResourceName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(resource);
            return value as string ?? resource.GetType().Name;
        }

        private static string GetResourceClass(object resource)
        {
            object? proto = resource.GetType().GetProperty("Proto", BindingFlags.Public | BindingFlags.Instance)?.GetValue(resource);
            object? resourceClass = proto?.GetType().GetProperty("ResourceClass", BindingFlags.Public | BindingFlags.Instance)?.GetValue(proto);
            return resourceClass?.ToString() ?? string.Empty;
        }

        private static bool IsFuelGeneratorFull(object generator)
        {
            object? generatorFuel = ReadMember(generator, "generatorFuel");
            if (generatorFuel != null && ReadBoolMember(generatorFuel, "IsFull", false))
                return true;

            double percent = ReadDoubleMember(generator, "FuelPercent", 0);
            return percent >= 0.999;
        }

        private static bool InvokeBool(MethodInfo method, object target, object arg)
        {
            try
            {
                object? result = method.Invoke(target, new object?[] { arg });
                return result is bool value && value;
            }
            catch
            {
                return false;
            }
        }

        private static bool InvokeIsAnimalFeeds(MethodInfo method, object target, object? itemInfo, out int energy)
        {
            energy = 0;
            try
            {
                object?[] args = new object?[] { itemInfo, energy };
                object? result = method.Invoke(target, args);
                if (!(result is bool ok) || !ok)
                    return false;
                if (args.Length > 1 && args[1] != null)
                    energy = Convert.ToInt32(args[1]);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryCostSelf(object item)
        {
            MethodInfo? costWithOut = FindMethodInHierarchy(item.GetType(), "CostSelf", 2);
            if (costWithOut != null)
            {
                ParameterInfo[] parameters = costWithOut.GetParameters();
                if (parameters.Length == 2 && parameters[0].ParameterType.IsByRef)
                {
                    object?[] args = new object?[] { null, false };
                    object? result = costWithOut.Invoke(item, args);
                    return result is bool ok && ok;
                }
            }

            MethodInfo? costSingle = FindMethodInHierarchy(item.GetType(), "CostSelf", 1);
            if (costSingle == null)
                return false;
            object? consumed = costSingle.Invoke(item, new object?[] { false });
            return consumed != null;
        }

        private static string FormatRatio(double value)
        {
            return value < 0 ? "unknown" : value.ToString("0.###");
        }

        private static int ReadIntMember(object instance, string name, int fallback)
        {
            Type type = instance.GetType();
            object? value = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance) ??
                type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(instance);
            return value == null ? fallback : Convert.ToInt32(value);
        }

        private static double ReadDoubleMember(object instance, string name, double fallback)
        {
            object? value = ReadMember(instance, name);
            return value == null ? fallback : Convert.ToDouble(value);
        }

        private MotorVehicleRegisterResult MotorVehicleRegisterFailed(MotorVehicleRegisterResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            runtime.SetHookStatus("Vehicle.SecondMotorRegistration", "failed", "IMotorVehicleApi.RegisterSecondMotor", result.Message);
            return result;
        }

        private MotorVehicleSummonResult MotorVehicleSummonFailed(MotorVehicleSummonResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            result.After = result.After.VehicleId.Length == 0 ? GetVehicleState(result.VehicleId) : result.After;
            runtime.RuntimeMonitor.Log("Motor vehicle request failed vehicle=" + result.VehicleId + " reason=" + result.FailureReason + " message=" + result.Message);
            return result;
        }

        private MotorVehicleSummonResult MotorVehicleSummonFailedWithCleanup(MotorVehicleSummonResult result, SecondMotorRuntime vehicle, string reason, string message, string cleanupReason)
        {
            string cleanup = CleanupSecondMotorRuntime(vehicle, cleanupReason + ": " + reason, destroyGameObject: true);
            result.After = BuildSecondMotorState(vehicle, "after-failed-summon-cleanup");
            string fullMessage = FirstText(message, "Second motor summon failed.") + " cleanup={" + cleanup + "}";
            return MotorVehicleSummonFailed(result, reason, fullMessage);
        }

        private MotorVehicleRideResult MotorVehicleRideFailed(MotorVehicleRideResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            result.After = result.After.VehicleId.Length == 0 ? GetVehicleState(result.VehicleId) : result.After;
            runtime.RuntimeMonitor.Log("Motor vehicle ride request failed action=" + result.Action + " vehicle=" + result.VehicleId + " reason=" + result.FailureReason + " message=" + result.Message);
            return result;
        }

        internal string CleanupSecondMotorResidueForBoundary(string reason)
        {
            int cleaned = 0;
            var summaries = new List<string>();
            foreach (SecondMotorRuntime vehicle in secondMotors.Values.ToArray())
            {
                string summary = CleanupSecondMotorRuntime(vehicle, reason, destroyGameObject: true);
                if (summary.IndexOf("cleaned=0", StringComparison.OrdinalIgnoreCase) < 0)
                    cleaned++;
                summaries.Add(vehicle.Options.VehicleId + "{" + summary + "}");
            }

            string message = "Second motor lifecycle cleanup reason=" + (reason ?? string.Empty) + " vehicles=" + secondMotors.Count + " cleanedVehicles=" + cleaned + " details=" + string.Join(";", summaries);
            runtime.RuntimeMonitor.Log(message);
            runtime.SetHookStatus("Vehicle.SecondMotorCleanup", "experimental", "SaveLoaded/ReturnedToTitle/failure cleanup", message);
            return message;
        }

        private string CleanupSecondMotorRuntime(SecondMotorRuntime vehicle, string reason, bool destroyGameObject)
        {
            if (vehicle == null)
                return "cleaned=0 reason=no-vehicle";

            int cleaned = 0;
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (activeSecondMotor != null && ReferenceEquals(activeSecondMotor, vehicle))
                {
                    RestoreSecondMotorTuningAfterFixedUpdate(vehicle.Controller ?? new object());
                    RestoreOriginalAgentMotorController(GetAgentController(dolocApi));
                    RestoreOriginalMotorSnapshot(clearSnapshot: true);
                    activeSecondMotor = null;
                    cleaned++;
                }

                if (vehicle.Controller != null)
                {
                    MethodInfo? setVisible = FindMethodInHierarchy(vehicle.Controller.GetType(), "SetVisible", 1);
                    setVisible?.Invoke(vehicle.Controller, new object[] { false });
                    secondMotorControllers.Remove(vehicle.Controller);
                    cleaned++;
                }

                if (vehicle.Interactable != null)
                {
                    secondMotorInteractables.Remove(vehicle.Interactable);
                    cleaned++;
                }

                if (destroyGameObject && vehicle.GameObject != null)
                {
                    DestroyUnityObject(vehicle.GameObject);
                    cleaned++;
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Second motor cleanup failed.", ex.ToString());
                return "cleaned=" + cleaned + ", error=" + ex.GetType().Name + ":" + ex.Message;
            }
            finally
            {
                vehicle.GameObject = null;
                vehicle.Controller = null;
                vehicle.Interactable = null;
                vehicle.Room = null;
                vehicle.LastPosition = null;
                vehicle.PendingTransitionRoom = null;
                vehicle.PendingTransitionRoomId = string.Empty;
                vehicle.PendingTransitionPosition = null;
                vehicle.IsRiding = false;
                vehicle.LastFailureReason = "cleaned";
                vehicle.LastMessage = "DTMAPI-owned second motor clone cleaned up. reason=" + (reason ?? string.Empty);
            }

            return "cleaned=" + cleaned + ", reason=" + (reason ?? string.Empty);
        }

        private bool IsOwnerOfficiallyEnabled(string ownerId, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(ownerId) ||
                ownerId.Equals("unknown", StringComparison.OrdinalIgnoreCase) ||
                ownerId.Equals("DTMAPI.GameBridge.DolocTown", StringComparison.OrdinalIgnoreCase))
                return true;

            DTMAPI.Core.Manifesting.DiscoveredMod? discovered = runtime.DiscoveredMods
                .FirstOrDefault(mod => mod.Manifest.UniqueID.Equals(ownerId, StringComparison.OrdinalIgnoreCase));
            if (discovered == null)
                return true;
            if (discovered.OfficialEnabled)
                return true;

            message = "Owner " + ownerId + " is disabled by official enablement path " + FirstText(discovered.OfficialId, discovered.Source) + ". " + FirstText(discovered.EnablementReason, "Restart is required for DLL unload; GameBridge blocks runtime vehicle behavior immediately.");
            return false;
        }

        private static bool TryGenerateNativeItem(string itemId, int count, out object? item, out string reason, out string message)
        {
            item = null;
            reason = string.Empty;
            message = string.Empty;
            Type? itemFactory = ResolveType("DolocTown.ItemFactory, Assembly-CSharp");
            MethodInfo? generateItem = itemFactory?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(method =>
                {
                    if (!method.Name.Equals("GenerateItem", StringComparison.Ordinal))
                        return false;
                    ParameterInfo[] parameters = method.GetParameters();
                    return parameters.Length == 3 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].ParameterType == typeof(int) &&
                        parameters[2].ParameterType.IsByRef;
                });
            if (generateItem == null)
            {
                reason = "missing-item-factory";
                message = "DolocTown.ItemFactory.GenerateItem(string,int,out Item) was not found.";
                return false;
            }

            object?[] args = { itemId, Math.Max(1, count), null };
            object? ok = generateItem.Invoke(null, args);
            item = args[2];
            if (!(ok is bool success) || !success || item == null)
            {
                reason = "item-generation-failed";
                message = "ItemFactory.GenerateItem failed for " + itemId + ".";
                return false;
            }

            return true;
        }

        private static object? GetAgentController(Type? dolocApi)
        {
            object? gameStateManager = ReadStaticMember(dolocApi, "gameStateManager");
            return ReadStaticMember(dolocApi, "AgentController") ??
                (gameStateManager == null ? null : ReadMember(gameStateManager, "agentController"));
        }

        private MotorVehicleSummonResult SummonSecondMotor(IManifest? owner, SecondMotorRuntime vehicle, string source)
        {
            string ownerId = owner?.UniqueID ?? vehicle.OwnerUniqueId;
            var result = new MotorVehicleSummonResult
            {
                VehicleId = vehicle.Options.VehicleId,
                DisplayName = vehicle.Options.DisplayName,
                Before = BuildSecondMotorState(vehicle, "before-summon")
            };

            try
            {
                if (!IsOwnerOfficiallyEnabled(vehicle.OwnerUniqueId, out string enablementMessage))
                    return MotorVehicleSummonFailedWithCleanup(result, vehicle, "source-disabled", enablementMessage, "disabled summon");

                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                    return MotorVehicleSummonFailedWithCleanup(result, vehicle, "missing-dolocapi", "DolocAPI is not available.", "missing DolocAPI");
                if (!CanCallMotorInCurrentRoom(dolocApi, out string reason, out string message))
                    return MotorVehicleSummonFailedWithCleanup(result, vehicle, reason, message, "room rejected summon");
                if (!EnsureSecondMotorInstance(vehicle, out string ensureReason, out string ensureMessage))
                    return MotorVehicleSummonFailedWithCleanup(result, vehicle, ensureReason, ensureMessage, "ensure instance failed");

                object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
                object? target = BuildMotorSummonPositionNearAgent(1.15f);
                if (currentRoom == null || target == null)
                    return MotorVehicleSummonFailedWithCleanup(result, vehicle, "missing-position", "Current room or agent position was not available.", "missing summon position");

                vehicle.Room = currentRoom;
                vehicle.LastPosition = target;
                vehicle.LastFailureReason = string.Empty;
                vehicle.LastMessage = "Second motor summon requested source=" + source + " owner=" + ownerId + ".";

                MethodInfo? reset = FindMethodInHierarchy(vehicle.Controller!.GetType(), "Reset", 0);
                reset?.Invoke(vehicle.Controller, null);
                MethodInfo? setVisible = FindMethodInHierarchy(vehicle.Controller.GetType(), "SetVisible", 1);
                setVisible?.Invoke(vehicle.Controller, new object[] { true });
                if (!TryInvokeAutoFlyToAgent(vehicle.Controller))
                    SetMotorControllerPosition(vehicle.Controller, target);

                result.After = BuildSecondMotorState(vehicle, "after-summon");
                result.Success = true;
                result.Message = vehicle.LastMessage + " room=" + result.After.RoomId + " speedMultiplier=" + vehicle.Options.SpeedMultiplier.ToString("0.###") + ".";
                runtime.RuntimeMonitor.Log("Second motor summon OK vehicle=" + vehicle.Options.VehicleId + " key=" + vehicle.Options.KeyItemId + " " + result.Message);
                runtime.SetHookStatus("Vehicle.SecondMotorSummon", "experimental", "DTMAPI cloned MotorController + MotorController.AutoFlyTo", result.Message);
                RaiseVehicleEvent("summon", vehicle.Options.VehicleId, result.After, result.Message);
                return result;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Second motor summon failed.", ex.ToString());
                vehicle.LastFailureReason = ex.GetType().Name;
                vehicle.LastMessage = ex.Message;
                return MotorVehicleSummonFailedWithCleanup(result, vehicle, ex.GetType().Name, ex.Message, "summon exception");
            }
        }

        private bool EnsureSecondMotorInstance(SecondMotorRuntime vehicle, out string reason, out string message)
        {
            reason = string.Empty;
            message = string.Empty;
            if (vehicle.Controller != null)
                return true;

            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? originalMotor = ReadStaticMember(dolocApi, "Motor");
            if (originalMotor == null)
            {
                reason = "missing-original-motor";
                message = "DolocAPI.Motor is not available to clone.";
                return false;
            }

            object? clone = CloneUnityObject(originalMotor);
            if (clone == null)
            {
                reason = "clone-failed";
                message = "UnityEngine.Object.Instantiate failed for the original MotorController.";
                return false;
            }

            object? controller = originalMotor.GetType().IsInstanceOfType(clone) ? clone : GetComponent(clone, originalMotor.GetType());
            if (controller == null)
            {
                reason = "missing-cloned-controller";
                message = "The cloned object does not contain a MotorController component.";
                DestroyUnityObject(clone);
                return false;
            }

            object? gameObject = ReadMember(controller, "gameObject") ?? clone;
            SetMemberValue(gameObject, "name", "DTMAPI.SecondMotor." + vehicle.Options.VehicleId);
            SetMemberValue(controller, "isInitialized", false);
            MethodInfo? init = FindMethodInHierarchy(controller.GetType(), "Init", 0);
            init?.Invoke(controller, null);
            MethodInfo? reset = FindMethodInHierarchy(controller.GetType(), "Reset", 0);
            reset?.Invoke(controller, null);
            MethodInfo? setVisible = FindMethodInHierarchy(controller.GetType(), "SetVisible", 1);
            setVisible?.Invoke(controller, new object[] { false });

            object? interactable = ReadMember(controller, "motorInteractable");
            if (interactable == null)
            {
                Type? interactableType = ResolveType("DolocTown.MotorInteractable, Assembly-CSharp");
                if (interactableType != null)
                    interactable = GetComponentInChildren(gameObject, interactableType, includeInactive: true);
            }

            vehicle.Controller = controller;
            vehicle.GameObject = gameObject;
            vehicle.Interactable = interactable;
            secondMotorControllers.Add(controller);
            if (interactable != null)
                secondMotorInteractables.Add(interactable);

            string appearanceSummary = ApplySecondMotorScopedAppearance(vehicle);
            vehicle.LastMessage = "Cloned original MotorController for DTMAPI second motor; " + appearanceSummary + ".";
            runtime.RuntimeMonitor.Log(vehicle.LastMessage + " vehicle=" + vehicle.Options.VehicleId + " textureNote=" + vehicle.Options.TextureSourceNote);
            return true;
        }

        private string ApplySecondMotorScopedAppearance(SecondMotorRuntime vehicle)
        {
            if (vehicle.Controller == null || vehicle.GameObject == null)
            {
                vehicle.AppearanceSummary = "appearance=missing-controller";
                return vehicle.AppearanceSummary;
            }

            if (vehicle.Options.UseOriginalMotorVisuals)
            {
                vehicle.AppearanceSummary = "appearance=original-runtime-clone";
                return vehicle.AppearanceSummary;
            }

            object? color = CreateUnityColor(new DtmColor(SecondMotorScopedTintR, SecondMotorScopedTintG, SecondMotorScopedTintB, 1));
            if (color == null)
            {
                vehicle.AppearanceSummary = "appearance=custom-tint-failed reason=missing-unity-color";
                return vehicle.AppearanceSummary;
            }

            object? driverRenderer = ReadMember(vehicle.Controller, "driverRenderer");
            int total = 0;
            int skippedDriver = 0;
            int changed = 0;
            foreach (object renderer in GetSpriteRenderers(vehicle.GameObject, includeInactive: true))
            {
                total++;
                if (IsComponentUnder(renderer, driverRenderer))
                {
                    skippedDriver++;
                    continue;
                }

                if (SetMemberValue(renderer, "color", color))
                    changed++;
            }

            vehicle.AppearanceSummary = "appearance=instance-scoped-tint hex=" + SecondMotorScopedTintHex + " renderers=" + changed + "/" + total + " skippedDriver=" + skippedDriver;
            runtime.SetHookStatus("Vehicle.SecondMotorAppearance", changed > 0 ? "experimental" : "failed", "DTMAPI cloned MotorController SpriteRenderer.color", vehicle.AppearanceSummary);
            return vehicle.AppearanceSummary;
        }

        private bool TryResolveSecondMotorFromInteractable(object interactable, out SecondMotorRuntime vehicle)
        {
            vehicle = null!;
            if (interactable == null)
                return false;
            foreach (SecondMotorRuntime candidate in secondMotors.Values)
            {
                if (candidate.Interactable != null && ReferenceEquals(candidate.Interactable, interactable))
                {
                    vehicle = candidate;
                    return true;
                }
            }

            object? controller = ReadMember(interactable, "_motorController");
            if (controller == null)
                return false;
            SecondMotorRuntime? resolved = ResolveSecondMotorByController(controller);
            if (resolved == null)
                return false;
            vehicle = resolved;
            return true;
        }

        private SecondMotorRuntime? ResolveSecondMotorByController(object controller)
        {
            foreach (SecondMotorRuntime candidate in secondMotors.Values)
            {
                if (candidate.Controller != null && ReferenceEquals(candidate.Controller, controller))
                    return candidate;
            }
            return null;
        }

        private bool TryStartSecondMotorRide(SecondMotorRuntime vehicle, string reason)
        {
            try
            {
                if (!EnsureSecondMotorInstance(vehicle, out string ensureReason, out string ensureMessage))
                {
                    vehicle.LastFailureReason = ensureReason;
                    vehicle.LastMessage = ensureMessage;
                    return false;
                }

                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                if (dolocApi == null)
                {
                    vehicle.LastFailureReason = "missing-dolocapi";
                    vehicle.LastMessage = "DolocAPI is not available.";
                    return false;
                }

                if (!CanCallMotorInCurrentRoom(dolocApi, out string callReason, out string callMessage))
                {
                    vehicle.LastFailureReason = callReason;
                    vehicle.LastMessage = callMessage;
                    return false;
                }

                object? agentController = GetAgentController(dolocApi);
                if (agentController == null)
                {
                    vehicle.LastFailureReason = "missing-agent-controller";
                    vehicle.LastMessage = "DolocAPI.AgentController was not available.";
                    return false;
                }

                object? currentController = ReadMember(agentController, "motorController");
                if (originalAgentMotorController == null && currentController != null && !secondMotorControllers.Contains(currentController))
                    originalAgentMotorController = currentController;

                originalMotorSnapshotBeforeSecondRide = CaptureOriginalMotorSnapshot();
                if (!SetMemberValue(agentController, "motorController", vehicle.Controller!))
                {
                    vehicle.LastFailureReason = "controller-route-failed";
                    vehicle.LastMessage = "Could not route AgentControllerState.motorController to the DTMAPI second motor clone.";
                    return false;
                }

                activeSecondMotor = vehicle;
                MethodInfo? getOn = FindMethodInHierarchy(agentController.GetType(), "GetOnMotor", 0);
                if (getOn == null)
                {
                    RestoreOriginalAgentMotorController(agentController);
                    activeSecondMotor = null;
                    vehicle.LastFailureReason = "missing-get-on";
                    vehicle.LastMessage = "AgentControllerState.GetOnMotor was not found.";
                    return false;
                }

                getOn.Invoke(agentController, null);
                vehicle.IsRiding = true;
                vehicle.LastFailureReason = string.Empty;
                vehicle.LastMessage = "Second motor ride requested reason=" + reason + ".";
                runtime.RuntimeMonitor.Log("Second motor ride requested vehicle=" + vehicle.Options.VehicleId + " reason=" + reason + ".");
                RaiseVehicleEvent("ride-request", vehicle.Options.VehicleId, BuildSecondMotorState(vehicle, "ride-request"), vehicle.LastMessage);
                return true;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Second motor ride failed.", ex.ToString());
                vehicle.LastFailureReason = ex.GetType().Name;
                vehicle.LastMessage = ex.Message;
                return false;
            }
        }

        private void RestoreOriginalAgentMotorController(object? agentController)
        {
            if (agentController == null || originalAgentMotorController == null)
                return;
            SetMemberValue(agentController, "motorController", originalAgentMotorController);
        }

        private OriginalMotorSnapshot? CaptureOriginalMotorSnapshot()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? motor = ReadStaticMember(dolocApi, "Motor");
            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? farmData = archive == null ? null : ReadMember(archive, "farmData");
            object? agentData = farmData == null ? null : ReadMember(farmData, "agentData");
            object? motorData = agentData == null ? null : ReadMember(agentData, "motorData");
            object? room = motorData == null ? null : ReadMember(motorData, "CurrentRoom");
            room ??= ReadStaticMember(dolocApi, "CurrentRoom");
            object? position = motorData == null ? null : ReadMember(motorData, "position");
            position ??= motor == null ? null : ReadMember(motor, "position2d");
            bool unlocked = motorData != null && ReadBoolMember(motorData, "isUnlocked", false);
            if (motor == null || position == null)
                return null;
            return new OriginalMotorSnapshot(room, position, unlocked);
        }

        private void RestoreOriginalMotorSnapshot(bool clearSnapshot)
        {
            if (originalMotorSnapshotBeforeSecondRide == null || !originalMotorSnapshotBeforeSecondRide.Unlocked)
                return;
            try
            {
                restoringOriginalMotorSnapshot = true;
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? setMotorPosition = dolocApi?.GetMethod("SetMotorPosition", BindingFlags.Public | BindingFlags.Static);
                if (setMotorPosition != null && originalMotorSnapshotBeforeSecondRide.Position != null)
                    setMotorPosition.Invoke(null, new[] { originalMotorSnapshotBeforeSecondRide.Room, originalMotorSnapshotBeforeSecondRide.Position });
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to restore original motor snapshot after second-motor ride.", ex.ToString());
            }
            finally
            {
                restoringOriginalMotorSnapshot = false;
                if (clearSnapshot)
                    originalMotorSnapshotBeforeSecondRide = null;
            }
        }

        private MotorVehicleState BuildOriginalMotorState(string source)
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? motor = ReadStaticMember(dolocApi, "Motor");
            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? farmData = archive == null ? null : ReadMember(archive, "farmData");
            object? agentData = farmData == null ? null : ReadMember(farmData, "agentData");
            object? motorData = agentData == null ? null : ReadMember(agentData, "motorData");
            object? room = motorData == null ? null : ReadMember(motorData, "CurrentRoom");
            room ??= ReadStaticMember(dolocApi, "CurrentRoom");
            object? position = motorData == null ? null : ReadMember(motorData, "position");
            position ??= motor == null ? null : ReadMember(motor, "position");
            object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
            double maxSpeed = globalParameter == null ? 0 : ReadDoubleMember(globalParameter, "MotorHorizontalMaxSpeed", 0);
            bool unlocked = motorData != null && ReadBoolMember(motorData, "isUnlocked", false);
            bool riding = activeSecondMotor == null && ReadStaticBoolMember(dolocApi, "IsAgentRiding", false);
            CanCallMotorInCurrentRoom(dolocApi, out string failureReason, out string message);
            return new MotorVehicleState
            {
                VehicleId = "doloc.original_motor",
                DisplayName = "Original Motor",
                IsOriginalMotor = true,
                IsRegistered = true,
                IsUnlocked = unlocked,
                IsVisible = motor != null && ReadBoolMember(motor, "isVisible", false),
                IsRiding = riding,
                IsAvailableInCurrentRoom = string.IsNullOrWhiteSpace(failureReason),
                RoomId = ReadStringMemberOrEmpty(room, "RoomId"),
                RoomTitle = BuildRoomTitle(room),
                X = ReadVectorComponent(position, "x"),
                Y = ReadVectorComponent(position, "y"),
                Z = ReadVectorComponent(position, "z"),
                EnduranceProgress = motor == null ? 0 : ReadDoubleMember(motor, "EnduranceProgress", 0),
                BaseMaxSpeed = maxSpeed,
                EffectiveMaxSpeed = maxSpeed,
                SpeedMultiplier = 1,
                KeyItemId = "motor_key",
                LastFailureReason = failureReason,
                LastMessage = FirstText(message, "Original motor state source=" + source + ".")
            };
        }

        private MotorVehicleState BuildSecondMotorState(SecondMotorRuntime vehicle, string source)
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? position = vehicle.Controller == null ? vehicle.LastPosition : ReadMember(vehicle.Controller, "position");
            object? globalParameter = ReadStaticMember(dolocApi, "GlobalParameter");
            double maxSpeed = globalParameter == null ? 0 : ReadDoubleMember(globalParameter, "MotorHorizontalMaxSpeed", 0);
            CanCallMotorInCurrentRoom(dolocApi, out string failureReason, out string message);
            return new MotorVehicleState
            {
                VehicleId = vehicle.Options.VehicleId,
                OwnerUniqueId = vehicle.OwnerUniqueId,
                DisplayName = vehicle.Options.DisplayName,
                IsOriginalMotor = false,
                IsRegistered = true,
                IsUnlocked = true,
                IsVisible = vehicle.Controller != null && ReadBoolMember(vehicle.Controller, "isVisible", false),
                IsRiding = vehicle.IsRiding,
                IsAvailableInCurrentRoom = string.IsNullOrWhiteSpace(failureReason),
                RoomId = ReadStringMemberOrEmpty(vehicle.Room, "RoomId"),
                RoomTitle = BuildRoomTitle(vehicle.Room),
                X = ReadVectorComponent(position, "x"),
                Y = ReadVectorComponent(position, "y"),
                Z = ReadVectorComponent(position, "z"),
                EnduranceProgress = vehicle.Controller == null ? 1 : ReadDoubleMember(vehicle.Controller, "EnduranceProgress", 1),
                BaseMaxSpeed = maxSpeed,
                EffectiveMaxSpeed = maxSpeed * vehicle.Options.SpeedMultiplier,
                SpeedMultiplier = vehicle.Options.SpeedMultiplier,
                KeyItemId = vehicle.Options.KeyItemId,
                LastFailureReason = FirstText(vehicle.LastFailureReason, failureReason),
                LastMessage = FirstText(vehicle.LastMessage, message, "Second motor state source=" + source + ".")
            };
        }

        private bool CanCallMotorInCurrentRoom(Type? dolocApi, out string reason, out string message)
        {
            reason = string.Empty;
            message = string.Empty;
            object? currentRoom = ReadStaticMember(dolocApi, "CurrentRoom");
            if (currentRoom == null)
            {
                reason = "missing-current-room";
                message = "Current room is not available.";
                return false;
            }

            if (ReadStaticBoolMember(dolocApi, "IsAgentRiding", false))
            {
                reason = "already-riding";
                message = "Cannot summon or switch motors while already riding.";
                return false;
            }

            object? baseProto = ReadMember(currentRoom, "baseProto");
            bool isInHouse = ReadBoolMember(currentRoom, "IsInHouse", false) || (baseProto != null && ReadBoolMember(baseProto, "isInHouse", false));
            bool disableMotor = ReadBoolMember(currentRoom, "DisableMotor", false);
            if (isInHouse || disableMotor)
            {
                reason = isInHouse ? "in-house" : "disabled-room";
                message = "The current room disallows motor summon.";
                return false;
            }

            return true;
        }

        private object? BuildMotorSummonPositionNearAgent(float xOffset)
        {
            object? center = ReadAgentPositionCenterObject();
            if (center == null)
                return null;
            return CreateUnityVector2(ReadVectorComponent(center, "x") + xOffset, ReadVectorComponent(center, "y"));
        }

        private bool TryInvokeAutoFlyToAgent(object? motor)
        {
            if (motor == null)
                return false;
            MethodInfo? autoFlyTo = FindMethodInHierarchy(motor.GetType(), "AutoFlyTo", 2);
            if (autoFlyTo == null)
                return false;
            Type delegateType = autoFlyTo.GetParameters()[0].ParameterType;
            Delegate? getter = CreateAgentPositionGetterDelegate(delegateType);
            if (getter == null)
                return false;
            autoFlyTo.Invoke(motor, new object?[] { getter, null });
            return true;
        }

        private static void SetMotorControllerPosition(object controller, object position2d)
        {
            double x = ReadVectorComponent(position2d, "x");
            double y = ReadVectorComponent(position2d, "y");
            double z = ReadVectorComponent(ReadMember(controller, "position"), "z");
            if (double.IsNaN(z))
                z = 0;
            object? position3d = CreateUnityVector3(x, y, z);
            if (position3d != null)
                SetMemberValue(controller, "position", position3d);
            SetMemberValue(controller, "position2d", position2d);
            object? rb = ReadMember(controller, "rb");
            if (rb != null)
            {
                SetMemberValue(rb, "position", position2d);
                object? zero = CreateUnityVector2(0, 0);
                if (zero != null)
                    SetMemberValue(rb, "velocity", zero);
            }
            MethodInfo? clearVelocity = FindMethodInHierarchy(controller.GetType(), "ClearVelocity", 0);
            clearVelocity?.Invoke(controller, null);
        }

        private static Delegate? CreateAgentPositionGetterDelegate(Type delegateType)
        {
            MethodInfo? method = typeof(DolocTownExperimentalBridgeApi).GetMethod(nameof(ReadAgentPositionCenterObject), BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo? invoke = delegateType.GetMethod("Invoke");
            Type? returnType = invoke?.ReturnType;
            if (method == null || returnType == null)
                return null;
            Expression body = Expression.Convert(Expression.Call(method), returnType);
            return Expression.Lambda(delegateType, body).Compile();
        }

        private static object? ReadAgentPositionCenterObject()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? agent = ReadStaticMember(dolocApi, "agent") ?? ReadStaticMember(dolocApi, "Agent");
            object? center = agent == null ? null : ReadMember(agent, "PositionCenter");
            if (center != null)
                return center;
            object? position = ReadStaticMember(dolocApi, "AgentPosition");
            if (position == null)
                return null;
            return CreateUnityVector2(ReadVectorComponent(position, "x"), ReadVectorComponent(position, "y"));
        }

        private static bool InvokeBoolNoArgs(object instance, string methodName, bool fallback)
        {
            try
            {
                MethodInfo? method = FindMethodInHierarchy(instance.GetType(), methodName, 0);
                object? value = method?.Invoke(instance, null);
                return value is bool result ? result : fallback;
            }
            catch
            {
                return fallback;
            }
        }

        private static string ReadStringMemberOrEmpty(object? instance, string name)
        {
            return instance == null ? string.Empty : ReadStringMember(instance, name);
        }

        private static string BuildRoomTitle(object? room)
        {
            if (room == null)
                return string.Empty;
            object? sceneConfig = ReadMember(room, "SceneConfig");
            return FirstText(ReadStringMemberOrEmpty(sceneConfig, "Title"), ReadStringMemberOrEmpty(room, "RoomId"), ReadStringMemberOrEmpty(room, "SceneRawName"));
        }

        private GlobalMotorTuningSnapshot CaptureGlobalMotorTuning(object globalParameter)
        {
            return new GlobalMotorTuningSnapshot(
                ReadDoubleMember(globalParameter, "MotorHorizontalAcceleration", 0),
                ReadDoubleMember(globalParameter, "MotorHorizontalRevertAcceleration", 0),
                ReadDoubleMember(globalParameter, "MotorHorizontalDeceleration", 0),
                ReadDoubleMember(globalParameter, "MotorHorizontalMaxSpeed", 0));
        }

        private static void RestoreGlobalMotorTuning(object globalParameter, GlobalMotorTuningSnapshot snapshot)
        {
            WriteFloatMember(globalParameter, "MotorHorizontalAcceleration", (float)snapshot.MotorHorizontalAcceleration);
            WriteFloatMember(globalParameter, "MotorHorizontalRevertAcceleration", (float)snapshot.MotorHorizontalRevertAcceleration);
            WriteFloatMember(globalParameter, "MotorHorizontalDeceleration", (float)snapshot.MotorHorizontalDeceleration);
            WriteFloatMember(globalParameter, "MotorHorizontalMaxSpeed", (float)snapshot.MotorHorizontalMaxSpeed);
        }

        private void RaiseVehicleEvent(string eventType, string vehicleId, MotorVehicleState state, string message)
        {
            VehicleChanged?.Invoke(this, new MotorVehicleEventArgs
            {
                EventType = eventType ?? string.Empty,
                VehicleId = vehicleId ?? string.Empty,
                State = state ?? new MotorVehicleState(),
                Message = message ?? string.Empty
            });
        }

        private static bool TryGetFishRoeId(object item, out string fishId)
        {
            fishId = string.Empty;
            if (item == null)
                return false;
            Type type = item.GetType();
            if (!IsTypeOrBase(type, "DolocTown.ItemFishRoe"))
                return false;
            object? value = type.GetProperty("fishName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(item);
            fishId = value as string ?? string.Empty;
            return !string.IsNullOrWhiteSpace(fishId);
        }

        private IReadOnlyList<AnimalProgressInfo> BuildAnimalProgressList(object animal)
        {
            Type type = animal.GetType();
            string animalId = type.GetProperty("protoName", BindingFlags.Public | BindingFlags.Instance)?.GetValue(animal) as string ?? string.Empty;
            if (string.IsNullOrWhiteSpace(animalId))
                return Array.Empty<AnimalProgressInfo>();

            object? valuesObject = type.GetField("husbandryValues", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(animal);
            var currentByOutput = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (valuesObject is IDictionary values)
            {
                foreach (DictionaryEntry entry in values)
                {
                    string outputId = entry.Key as string ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(outputId))
                        continue;
                    currentByOutput[outputId] = Convert.ToInt32(entry.Value);
                }
            }

            var progress = new List<AnimalProgressInfo>();
            foreach ((string outputId, int threshold) in EnumerateHusbandryOutputs(animalId))
            {
                if (string.IsNullOrWhiteSpace(outputId))
                    continue;
                if (threshold <= 0)
                    continue;
                currentByOutput.TryGetValue(outputId, out int current);
                double ratio = Math.Max(0, Math.Min(1, current / (double)threshold));
                progress.Add(new AnimalProgressInfo
                {
                    AnimalId = animalId,
                    OutputId = outputId,
                    OutputTitle = ResolveItemTitle(outputId),
                    Current = current,
                    Threshold = threshold,
                    Progress = ratio
                });
            }

            if (progress.Count == 0 && currentByOutput.Count > 0)
            {
                foreach (KeyValuePair<string, int> entry in currentByOutput)
                {
                    if (!TryGetHusbandryThreshold(animalId, entry.Key, out int threshold) || threshold <= 0)
                        continue;
                    double ratio = Math.Max(0, Math.Min(1, entry.Value / (double)threshold));
                    progress.Add(new AnimalProgressInfo
                    {
                        AnimalId = animalId,
                        OutputId = entry.Key,
                        OutputTitle = ResolveItemTitle(entry.Key),
                        Current = entry.Value,
                        Threshold = threshold,
                        Progress = ratio
                    });
                }
            }

            return progress.ToArray();
        }

        private IEnumerable<(string OutputId, int Threshold)> EnumerateHusbandryOutputs(string animalId)
        {
            object? tables = GetDolocTables();
            object? husbandry = tables == null ? null : ReadMember(tables, "TbHusbandry");
            MethodInfo? getOrDefault = husbandry?.GetType().GetMethod("GetOrDefault", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            object? info = getOrDefault?.Invoke(husbandry, new object?[] { animalId });
            object? datas = info == null ? null : ReadMember(info, "HusbandryDatas");
            foreach (object data in EnumerateObjects(datas))
            {
                string outputId = ReadStringMember(data, "Output");
                int threshold = ReadIntMember(data, "Threshold", 0);
                if (!string.IsNullOrWhiteSpace(outputId) && threshold > 0)
                    yield return (outputId, threshold);
            }
        }

        private bool TryGetHusbandryThreshold(string animalId, string outputId, out int threshold)
        {
            string cacheKey = animalId + "|" + outputId;
            if (husbandryThresholdCache.TryGetValue(cacheKey, out threshold))
                return threshold > 0;

            threshold = 0;
            try
            {
                Type? dolocConfig = ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
                object? tables = dolocConfig?.GetProperty("Tables", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                object? husbandry = tables?.GetType().GetProperty("TbHusbandry", BindingFlags.Public | BindingFlags.Instance)?.GetValue(tables);
                MethodInfo? method = husbandry?.GetType().GetMethod("TryGetThreshold", BindingFlags.Public | BindingFlags.Instance);
                if (method == null)
                    return false;

                object?[] args = new object?[] { animalId, outputId, threshold };
                object? result = method.Invoke(husbandry, args);
                if (!(result is bool ok) || !ok)
                    return false;

                threshold = Convert.ToInt32(args[2]);
                husbandryThresholdCache[cacheKey] = threshold;
                return threshold > 0;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to query husbandry threshold for " + cacheKey + ".", ex.ToString());
                return false;
            }
        }

        private string ResolveItemTitle(string itemId)
        {
            if (itemTitleCache.TryGetValue(itemId, out string title))
                return title;
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                MethodInfo? queryItemProto = dolocApi?.GetMethod("QueryItemProto", BindingFlags.Public | BindingFlags.Static);
                if (queryItemProto != null)
                {
                    object?[] args = new object?[] { itemId, null };
                    object? result = queryItemProto.Invoke(null, args);
                    if (result is bool ok && ok && args[1] != null)
                    {
                        title = args[1]!.GetType().GetProperty("Title", BindingFlags.Public | BindingFlags.Instance)?.GetValue(args[1]) as string ?? itemId;
                        itemTitleCache[itemId] = title;
                        return title;
                    }
                }
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Failed to query item title for " + itemId + ".", ex.ToString());
            }
            itemTitleCache[itemId] = itemId;
            return itemId;
        }

        private static Type? ResolveType(string assemblyQualifiedName)
        {
            Type? type = Type.GetType(assemblyQualifiedName);
            if (type != null)
                return type;
            int comma = assemblyQualifiedName.IndexOf(',');
            string typeName = comma >= 0 ? assemblyQualifiedName.Substring(0, comma).Trim() : assemblyQualifiedName;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(typeName, throwOnError: false);
                    if (type != null)
                        return type;
                }
                catch
                {
                }
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

        private static bool ImplementsInterface(Type? type, string fullName)
        {
            if (type == null)
                return false;
            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (string.Equals(interfaceType.FullName, fullName, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static object? ReadStaticMember(Type? type, string name)
        {
            if (type == null)
                return null;
            FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (field != null)
            {
                try
                {
                    object? value = field.GetValue(null);
                    if (value != null)
                        return value;
                }
                catch
                {
                }
            }

            PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (property != null)
            {
                try
                {
                    return property.GetValue(null);
                }
                catch
                {
                }
            }
            return null;
        }

        private static bool ReadStaticBoolMember(Type? type, string name, bool fallback)
        {
            object? value = ReadStaticMember(type, name);
            return value is bool result ? result : fallback;
        }

        private static string FirstText(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return string.Empty;
        }

        private static string InsertAnimalProgressLine(string current, string line)
        {
            current = current ?? string.Empty;
            if (string.IsNullOrWhiteSpace(current))
                return line;

            string[] lines = current.Replace("\r\n", "\n").Split('\n');
            if (lines.Length <= 1)
                return current.TrimEnd() + Environment.NewLine + line;

            return lines[0].TrimEnd() + Environment.NewLine + line + Environment.NewLine + string.Join(Environment.NewLine, lines.Skip(1).ToArray()).TrimStart();
        }

        private static string BuildAnimalProgressBar(AnimalProgressInfo progress, DtmColor color)
        {
            int width = 18;
            int filled = Math.Max(0, Math.Min(width, (int)Math.Round(progress.Progress * width)));
            string bar = new string('█', filled) + new string('░', width - filled);
            return "<color=#" + ToHex(color) + ">" + bar + "</color>";
        }

        private static string ToHex(DtmColor color)
        {
            int r = (int)Math.Round(Math.Max(0, Math.Min(1, color.R)) * 255);
            int g = (int)Math.Round(Math.Max(0, Math.Min(1, color.G)) * 255);
            int b = (int)Math.Round(Math.Max(0, Math.Min(1, color.B)) * 255);
            return r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
        }

        private bool TryFindAnimalProgressMarker(string stateDescription, out string ownerId, out string label)
        {
            ownerId = string.Empty;
            label = string.Empty;
            foreach (KeyValuePair<string, AnimalHusbandryProgressOptions> entry in animalOptions)
            {
                AnimalHusbandryProgressOptions options = entry.Value ?? new AnimalHusbandryProgressOptions();
                if (!options.Enabled)
                    continue;

                if (ContainsAnimalProgressBar(stateDescription))
                {
                    ownerId = entry.Key;
                    label = "produce-progress-bar";
                    return true;
                }
            }

            if (ContainsAnimalProgressBar(stateDescription))
            {
                ownerId = "unknown";
                label = "produce-progress-bar";
                return true;
            }

            return false;
        }

        private static bool ContainsAnimalProgressBar(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            return text.IndexOf("████", StringComparison.Ordinal) >= 0 ||
                (text.IndexOf("<color=#", StringComparison.OrdinalIgnoreCase) >= 0 && text.IndexOf("</color>", StringComparison.OrdinalIgnoreCase) >= 0 && text.IndexOf("/", StringComparison.Ordinal) >= 0);
        }

        private static string ReadStringMember(object instance, string name)
        {
            object? value = ReadMember(instance, name);
            return value as string ?? string.Empty;
        }

        private static string ReadStringMember(object instance, string name, string fallback)
        {
            string value = ReadStringMember(instance, name);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static bool ReadBoolMember(object instance, string name, bool fallback)
        {
            object? value = ReadMember(instance, name);
            return value is bool result ? result : fallback;
        }

        private static bool WriteBoolMember(object instance, string name, bool value)
        {
            return WriteMember(instance, name, typeof(bool), value);
        }

        private static bool WriteFloatMember(object instance, string name, float value)
        {
            return WriteMember(instance, name, typeof(float), value);
        }

        private static bool WriteMember(object instance, string name, Type expectedType, object value)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == expectedType)
                {
                    field.SetValue(instance, value);
                    return true;
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.CanWrite && property.PropertyType == expectedType)
                {
                    property.SetValue(instance, value);
                    return true;
                }
            }
            return false;
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

        private static object? ReadMember(object instance, string name)
        {
            for (Type? type = instance.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo? field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    try
                    {
                        object? value = field.GetValue(instance);
                        if (value != null)
                            return value;
                    }
                    catch
                    {
                    }
                }

                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null)
                {
                    try
                    {
                        object? value = property.GetValue(instance);
                        if (value != null)
                            return value;
                    }
                    catch
                    {
                    }
                }
            }
            return null;
        }

        private static object? CloneUnityObject(object original)
        {
            Type? objectType = ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Object, UnityEngine");
            MethodInfo? instantiate = objectType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "Instantiate" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == objectType);
            return instantiate?.Invoke(null, new[] { original });
        }

        private void ClearAnimalProgressOverlay(object parentTransform)
        {
            foreach (object instance in activeAnimalProgressOverlayObjects.ToArray())
            {
                SetActive(instance, false);
                DestroyUnityObject(instance);
            }
            activeAnimalProgressOverlayObjects.Clear();
            activeAnimalProgressOverlayRows.Clear();

            MethodInfo? find = parentTransform.GetType().GetMethod("Find", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            for (int i = 0; i < 10; i++)
            {
                object? child = find?.Invoke(parentTransform, new object[] { "DTMAPI.AnimalProduceProgress." + i });
                object? gameObject = child == null ? null : ReadMember(child, "gameObject");
                if (gameObject != null)
                    DestroyUnityObject(gameObject);
            }
        }

        private void RefreshAnimalProgressOverlayTexts(bool force)
        {
            if (activeAnimalProgressOverlayObjects.Count == 0 || activeAnimalProgressOverlayRows.Count == 0)
                return;
            if (!force && (DateTimeOffset.Now - lastAnimalProgressOverlayRefreshAt).TotalSeconds < 0.08)
                return;

            lastAnimalProgressOverlayRefreshAt = DateTimeOffset.Now;
            Type? progressBarType = ResolveType("DolocTown.UI.ProgressBar, Assembly-CSharp");
            int count = Math.Min(activeAnimalProgressOverlayObjects.Count, activeAnimalProgressOverlayRows.Count);
            for (int i = 0; i < count; i++)
            {
                object clone = activeAnimalProgressOverlayObjects[i];
                AnimalProgressRenderRow row = activeAnimalProgressOverlayRows[i];
                string progressText = row.Current + "/" + row.Threshold;
                object? progressBar = progressBarType == null ? null : GetComponent(clone, progressBarType);
                if (progressBar != null)
                {
                    MethodInfo? setTitle = progressBar.GetType().GetMethod("SetTitle", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
                    MethodInfo? setProgress = progressBar.GetType().GetMethod("SetProgress", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(float), typeof(string) }, null);
                    setTitle?.Invoke(progressBar, new object[] { row.OutputTitle });
                    setProgress?.Invoke(progressBar, new object[] { (float)Math.Max(0, Math.Min(1, row.Progress)), progressText });
                    SetUnityText(ReadMember(progressBar, "txtTitle"), row.OutputTitle);
                    SetUnityText(ReadMember(progressBar, "txtProgress"), progressText);
                }

                SetAnimalProgressTextsFromChildren(clone, row.OutputTitle, progressText);
            }
        }

        private static void DestroyUnityObject(object instance)
        {
            Type? objectType = ResolveType("UnityEngine.Object, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Object, UnityEngine");
            MethodInfo? destroy = objectType?.GetMethod("Destroy", BindingFlags.Public | BindingFlags.Static, null, new[] { objectType }, null);
            destroy?.Invoke(null, new[] { instance });
        }

        private static object? GetComponent(object gameObject, Type componentType)
        {
            MethodInfo? getComponent = gameObject.GetType().GetMethod("GetComponent", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type) }, null);
            return getComponent?.Invoke(gameObject, new object[] { componentType });
        }

        private static object? GetComponentInChildren(object gameObject, Type componentType, bool includeInactive)
        {
            MethodInfo? getComponent = gameObject.GetType().GetMethod("GetComponentInChildren", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type), typeof(bool) }, null);
            return getComponent?.Invoke(gameObject, new object[] { componentType, includeInactive });
        }

        private static List<object> GetSpriteRenderers(object gameObject, bool includeInactive)
        {
            var renderers = new List<object>();
            Type? spriteRendererType = ResolveType("UnityEngine.SpriteRenderer, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.SpriteRenderer, UnityEngine");
            if (spriteRendererType == null)
                return renderers;

            object? rootRenderer = GetComponent(gameObject, spriteRendererType);
            if (rootRenderer != null)
                renderers.Add(rootRenderer);

            MethodInfo? getComponents = gameObject.GetType().GetMethod("GetComponentsInChildren", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type), typeof(bool) }, null);
            object? result = getComponents?.Invoke(gameObject, new object[] { spriteRendererType, includeInactive });
            if (result is IEnumerable components)
            {
                foreach (object component in components)
                {
                    if (component == null || renderers.Any(existing => ReferenceEquals(existing, component)))
                        continue;
                    renderers.Add(component);
                }
            }

            return renderers;
        }

        private static bool IsComponentUnder(object component, object? parentComponent)
        {
            if (parentComponent == null)
                return false;

            object? current = ReadMember(component, "transform");
            object? parentTransform = ReadMember(parentComponent, "transform");
            int guard = 0;
            while (current != null && parentTransform != null && guard++ < 64)
            {
                if (ReferenceEquals(current, parentTransform))
                    return true;
                current = ReadMember(current, "parent");
            }

            return false;
        }

        private static void CountSecondMotorScopedTintRenderers(object? controller, out int total, out int tinted, out int skippedDriver)
        {
            total = 0;
            tinted = 0;
            skippedDriver = 0;
            if (controller == null)
                return;

            object? gameObject = ReadMember(controller, "gameObject");
            if (gameObject == null)
                return;

            object? driverRenderer = ReadMember(controller, "driverRenderer");
            foreach (object renderer in GetSpriteRenderers(gameObject, includeInactive: true))
            {
                total++;
                if (IsComponentUnder(renderer, driverRenderer))
                {
                    skippedDriver++;
                    continue;
                }

                if (IsSecondMotorScopedTintColor(ReadMember(renderer, "color")))
                    tinted++;
            }
        }

        private static bool IsSecondMotorScopedTintColor(object? color)
        {
            if (color == null)
                return false;

            return Math.Abs(ReadDoubleMember(color, "r", -1) - SecondMotorScopedTintR) <= 0.025 &&
                Math.Abs(ReadDoubleMember(color, "g", -1) - SecondMotorScopedTintG) <= 0.025 &&
                Math.Abs(ReadDoubleMember(color, "b", -1) - SecondMotorScopedTintB) <= 0.025;
        }

        private static string SetAnimalProgressTextsFromChildren(object gameObject, string title, string progress)
        {
            Type? textType = ResolveType("UnityEngine.UI.Text, UnityEngine.UI");
            if (textType == null)
                return "textType=missing";

            MethodInfo? getComponents = gameObject.GetType().GetMethod("GetComponentsInChildren", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(Type), typeof(bool) }, null);
            object? result = getComponents?.Invoke(gameObject, new object[] { textType, true });
            if (!(result is IEnumerable components))
                return "components=missing";

            int seen = 0;
            int titleWrites = 0;
            int progressWrites = 0;
            var samples = new List<string>();
            foreach (object component in components)
            {
                string current = ReadStringMember(component, "text");
                seen++;
                if (samples.Count < 4)
                    samples.Add(current);
                if (current.IndexOf("/", StringComparison.Ordinal) >= 0 || current.IndexOf("0/100", StringComparison.Ordinal) >= 0)
                {
                    SetUnityText(component, progress);
                    progressWrites++;
                }
                else if (!string.IsNullOrWhiteSpace(current))
                {
                    SetUnityText(component, title);
                    titleWrites++;
                }
            }

            return "textChildren=" + seen + ", titleWrites=" + titleWrites + ", progressWrites=" + progressWrites + ", samples=" + string.Join("|", samples.ToArray());
        }

        private static void SetUnityText(object? textComponent, string value)
        {
            if (textComponent == null)
                return;
            SetMemberValue(textComponent, "text", value ?? string.Empty);
            SetMemberValue(textComponent, "resizeTextForBestFit", false);
            SetMemberValue(textComponent, "fontSize", 18);
            SetMemberValue(textComponent, "resizeTextMinSize", 18);
            SetMemberValue(textComponent, "resizeTextMaxSize", 18);
        }

        private static void SetParent(object transform, object parent, bool worldPositionStays)
        {
            MethodInfo? setParent = transform.GetType().GetMethod("SetParent", BindingFlags.Public | BindingFlags.Instance, null, new[] { parent.GetType(), typeof(bool) }, null)
                ?? transform.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == "SetParent" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(bool));
            setParent?.Invoke(transform, new object[] { parent, worldPositionStays });
        }

        private static void SetActive(object gameObject, bool active)
        {
            MethodInfo? setActive = gameObject.GetType().GetMethod("SetActive", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(bool) }, null);
            setActive?.Invoke(gameObject, new object[] { active });
        }

        private static void PositionAnimalProgressRow(object sourceTransform, object rowTransform, int rowIndex)
        {
            object? localPosition = ReadMember(sourceTransform, "localPosition");
            double x = ReadVectorComponent(localPosition, "x");
            double y = ReadVectorComponent(localPosition, "y") - 42d * (rowIndex + 1);
            double z = ReadVectorComponent(localPosition, "z");
            object? position = CreateUnityVector3(x, y, z);
            if (position != null)
                SetMemberValue(rowTransform, "localPosition", position);

            object? localScale = ReadMember(sourceTransform, "localScale");
            if (localScale != null)
                SetMemberValue(rowTransform, "localScale", localScale);
        }

        private static object? CreateUnityVector3(double x, double y, double z)
        {
            Type? vector3 = ResolveType("UnityEngine.Vector3, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Vector3, UnityEngine");
            return vector3 == null ? null : Activator.CreateInstance(vector3, (float)x, (float)y, (float)z);
        }

        private static object? CreateUnityVector2(double x, double y)
        {
            Type? vector2 = ResolveType("UnityEngine.Vector2, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Vector2, UnityEngine");
            return vector2 == null ? null : Activator.CreateInstance(vector2, (float)x, (float)y);
        }

        private static object? CreateUnityVector2Int(int x, int y)
        {
            Type? vector2Int = ResolveType("UnityEngine.Vector2Int, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Vector2Int, UnityEngine");
            return vector2Int == null ? null : Activator.CreateInstance(vector2Int, x, y);
        }

        private static object? CreateUnityColor(DtmColor color)
        {
            Type? unityColor = ResolveType("UnityEngine.Color, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Color, UnityEngine");
            if (unityColor == null)
                return null;
            float r = (float)Math.Max(0, Math.Min(1, color.R));
            float g = (float)Math.Max(0, Math.Min(1, color.G));
            float b = (float)Math.Max(0, Math.Min(1, color.B));
            float a = (float)Math.Max(0, Math.Min(1, color.A));
            return Activator.CreateInstance(unityColor, r, g, b, a);
        }

        private static MethodInfo? FindMethodInHierarchy(Type? type, string name, int parameterCount)
        {
            for (Type? current = type; current != null; current = current.BaseType)
            {
                foreach (MethodInfo method in current.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    if (method.Name == name && method.GetParameters().Length == parameterCount)
                        return method;
                }
            }
            return null;
        }

        private static bool TryCaptureScreenshot(string path)
        {
            try
            {
                Type? screenCapture = ResolveType("UnityEngine.ScreenCapture, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.ScreenCapture, UnityEngine");
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
                        DestroyUnityObject(texture);
                    }
                }

                if (TryCaptureScreenshotWithReadPixels(path))
                    return true;

                MethodInfo? capture = screenCapture?.GetMethod("CaptureScreenshot", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(string) }, null);
                if (capture == null)
                    return false;
                capture.Invoke(null, new object[] { path });
                return File.Exists(path);
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
                Type? screenType = ResolveType("UnityEngine.Screen, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Screen, UnityEngine");
                Type? texture2DType = ResolveType("UnityEngine.Texture2D, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Texture2D, UnityEngine");
                Type? textureFormatType = ResolveType("UnityEngine.TextureFormat, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.TextureFormat, UnityEngine");
                Type? rectType = ResolveType("UnityEngine.Rect, UnityEngine.CoreModule") ?? ResolveType("UnityEngine.Rect, UnityEngine");
                if (screenType == null || texture2DType == null || textureFormatType == null || rectType == null)
                    return false;

                int width = Math.Max(1, Convert.ToInt32(screenType.GetProperty("width", BindingFlags.Public | BindingFlags.Static)?.GetValue(null), CultureInfo.InvariantCulture));
                int height = Math.Max(1, Convert.ToInt32(screenType.GetProperty("height", BindingFlags.Public | BindingFlags.Static)?.GetValue(null), CultureInfo.InvariantCulture));
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
                    DestroyUnityObject(texture);
            }
        }

        private static IEnumerable<InventoryDebugItem> FilterInventoryBySource(IEnumerable<InventoryDebugItem> items, string sourceId)
        {
            if (sourceId.Equals("__base", StringComparison.OrdinalIgnoreCase))
                return items.Where(i => !i.IsModItem);
            if (sourceId.Equals("__mods", StringComparison.OrdinalIgnoreCase))
                return items.Where(i => i.IsModItem);
            return items.Where(i => i.SourceId.Equals(sourceId, StringComparison.OrdinalIgnoreCase));
        }

        private static InventoryDebugSourceGroup[] BuildInventoryDebugSourceGroups(IEnumerable<InventoryDebugItem> items)
        {
            List<InventoryDebugItem> list = items.ToList();
            var groups = new List<InventoryDebugSourceGroup>();
            int baseCount = list.Count(i => !i.IsModItem);
            groups.Add(new InventoryDebugSourceGroup
            {
                Id = "__base",
                DisplayName = "本体",
                SourceKind = "Vanilla",
                IsModSource = false,
                Count = baseCount,
                Enabled = true,
                EnablementKnown = true
            });

            List<InventoryDebugItem> modItems = list.Where(i => i.IsModItem).ToList();
            groups.Add(new InventoryDebugSourceGroup
            {
                Id = "__mods",
                DisplayName = "模组",
                SourceKind = "Mod",
                IsModSource = true,
                Count = modItems.Count,
                Enabled = modItems.All(i => i.SourceEnabled),
                EnablementKnown = modItems.All(i => i.SourceEnablementKnown)
            });

            groups.AddRange(modItems
                .Where(i => !string.IsNullOrWhiteSpace(i.SourceId))
                .GroupBy(i => i.SourceId, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    InventoryDebugItem first = g
                        .OrderBy(i => i.LoadOrder < 0 ? int.MaxValue : i.LoadOrder)
                        .ThenBy(i => i.RuntimeOrder)
                        .ThenBy(i => i.Id, StringComparer.OrdinalIgnoreCase)
                        .First();
                    return new InventoryDebugSourceGroup
                    {
                        Id = g.Key,
                        DisplayName = FirstText(first.SourceModTitle, first.SourceId, first.SourceKind),
                        SourceKind = first.SourceKind,
                        IsModSource = true,
                        Count = g.Count(),
                        Enabled = g.Any(i => i.SourceEnabled),
                        EnablementKnown = g.Any(i => i.SourceEnablementKnown),
                        WorkshopId = first.WorkshopId
                    };
                })
                .OrderBy(g => g.SourceKind.Equals("Workshop", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .ThenBy(g => g.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Id, StringComparer.OrdinalIgnoreCase));

            return groups.ToArray();
        }

        private IEnumerable<InventoryDebugItem> EnumerateInventoryDebugItems()
        {
            Dictionary<string, IContentItemInfo> sources = runtime.GetIndexedContentItems()
                .GroupBy(i => i.ItemId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(i => i.Enabled)
                        .ThenBy(i => i.LoadOrder < 0 ? int.MaxValue : i.LoadOrder)
                        .ThenBy(i => i.SourceKind, StringComparer.OrdinalIgnoreCase)
                        .First(),
                    StringComparer.OrdinalIgnoreCase);

            var runtimeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int runtimeOrder = 0;
            foreach (InventoryDebugItem item in EnumerateRuntimeItems(runtimeOrderStart: 0, idSink: runtimeIds))
            {
                if (sources.TryGetValue(item.Id, out IContentItemInfo source))
                    ApplyContentSource(item, source);
                else
                {
                    item.SourceKind = "Vanilla";
                    item.SourceModTitle = "Doloc Town";
                    item.SourceId = "Vanilla";
                    item.SourceEnabled = true;
                    item.SourceEnablementKnown = true;
                    item.CanGive = item.CanSpawn;
                    item.CannotGiveReason = item.CanGive ? string.Empty : "not-spawnable";
                    item.SearchText = BuildInventorySearchText(item);
                }

                item.RuntimeOrder = runtimeOrder++;
                yield return item;
            }

            foreach (IContentItemInfo source in sources.Values
                .Where(s => !runtimeIds.Contains(s.ItemId))
                .OrderBy(s => s.LoadOrder < 0 ? int.MaxValue : s.LoadOrder)
                .ThenBy(s => s.ItemId, StringComparer.OrdinalIgnoreCase))
            {
                yield return CreateSourceOnlyInventoryItem(source);
            }
        }

        private IEnumerable<InventoryDebugItem> EnumerateRuntimeItems(int runtimeOrderStart, ISet<string> idSink)
        {
            object? table = GetDolocTable("TbItem");
            object? dataList = table == null ? null : ReadMember(table, "DataList");
            int runtimeOrder = runtimeOrderStart;
            foreach (object proto in EnumerateObjects(dataList))
            {
                string id = ReadStringMember(proto, "Id");
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                idSink.Add(id);

                object? mainType = ReadMember(proto, "MainType");
                object? subType = ReadMember(proto, "SubType_Ref");
                object? spriteAsset = ReadMember(proto, "UiSpriteAsset");
                string subCategory = FirstText(ReadStringMember(proto, "SubType"), subType == null ? string.Empty : ReadStringMember(subType, "Title"));
                string category = mainType == null ? subCategory : FirstText(ReadStringMember(mainType, "Title"), ReadStringMember(mainType, "Id"), subCategory);
                string englishName = FirstText(ReadStringMember(proto, "EnglishTitle"), ReadStringMember(proto, "TitleEn"), ReadStringMember(proto, "Name"), id);
                string[] tags = CollectInventoryTags(proto, mainType, subType).ToArray();
                string icon = spriteAsset?.ToString() ?? string.Empty;
                int overlay = ReadIntMember(proto, "Overlay", 1);
                var item = new InventoryDebugItem
                {
                    Id = id,
                    DisplayName = FirstText(ReadStringMember(proto, "Title"), id),
                    ChineseName = FirstText(ReadStringMember(proto, "Title"), id),
                    EnglishName = englishName,
                    Category = category,
                    SubCategory = subCategory,
                    Tags = tags,
                    MaxStack = Math.Max(1, overlay),
                    CanSpawn = overlay > 0,
                    CanGive = overlay > 0,
                    RuntimeLoaded = true,
                    IsModItem = false,
                    CannotGiveReason = overlay > 0 ? string.Empty : "not-spawnable",
                    HasIcon = spriteAsset != null,
                    IconAssetKey = icon,
                    RuntimeOrder = runtimeOrder++
                };
                item.SearchText = BuildInventorySearchText(item);
                yield return item;
            }
        }

        private static void ApplyContentSource(InventoryDebugItem item, IContentItemInfo source)
        {
            item.SourceKind = source.SourceKind;
            item.SourceModTitle = source.SourceModTitle;
            item.SourceId = source.SourceId;
            item.WorkshopId = source.WorkshopId;
            item.SourceEnabled = source.Enabled;
            item.SourceEnablementKnown = source.EnablementKnown;
            item.RootPath = source.RootPath;
            item.ContentPath = source.ContentPath;
            item.LoadOrder = source.LoadOrder;
            item.IconPath = source.IconPath;
            item.IconAssetKey = FirstText(item.IconAssetKey, source.IconAssetKey);
            item.ChineseName = FirstText(item.ChineseName, source.ChineseName);
            item.EnglishName = FirstText(item.EnglishName, source.EnglishName);
            item.IsModItem = !source.SourceKind.Equals("Vanilla", StringComparison.OrdinalIgnoreCase);
            item.Tags = item.Tags.Concat(source.Tags).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();
            item.CanGive = item.RuntimeLoaded && item.CanSpawn && source.Enabled;
            item.CannotGiveReason = item.CanGive
                ? string.Empty
                : (!source.Enabled ? "source-disabled" : (!item.RuntimeLoaded ? "not-runtime-loaded" : "not-spawnable"));
            item.SearchText = BuildInventorySearchText(item);
        }

        private static InventoryDebugItem CreateSourceOnlyInventoryItem(IContentItemInfo source)
        {
            var item = new InventoryDebugItem
            {
                Id = source.ItemId,
                DisplayName = FirstText(source.ChineseName, source.EnglishName, source.ItemId),
                ChineseName = FirstText(source.ChineseName, source.ItemId),
                EnglishName = source.EnglishName,
                Category = source.Category,
                SubCategory = source.Category,
                Tags = source.Tags,
                MaxStack = 0,
                CanSpawn = false,
                CanGive = false,
                RuntimeLoaded = false,
                IsModItem = true,
                CannotGiveReason = source.Enabled ? "not-runtime-loaded" : "source-disabled",
                HasIcon = !string.IsNullOrWhiteSpace(source.IconPath) || !string.IsNullOrWhiteSpace(source.IconAssetKey),
                IconAssetKey = source.IconAssetKey,
                IconPath = source.IconPath,
                SourceKind = source.SourceKind,
                SourceModTitle = source.SourceModTitle,
                SourceId = source.SourceId,
                WorkshopId = source.WorkshopId,
                SourceEnabled = source.Enabled,
                SourceEnablementKnown = source.EnablementKnown,
                RootPath = source.RootPath,
                ContentPath = source.ContentPath,
                LoadOrder = source.LoadOrder,
                RuntimeOrder = int.MaxValue
            };
            item.SearchText = BuildInventorySearchText(item);
            return item;
        }

        private static string BuildInventorySearchText(InventoryDebugItem item)
        {
            return string.Join(" ", new[]
                {
                    item.Id,
                    item.DisplayName,
                    item.ChineseName,
                    item.EnglishName,
                    item.Category,
                    item.SubCategory,
                    item.IconAssetKey,
                    item.SourceKind,
                    item.SourceModTitle,
                    item.SourceId,
                    item.WorkshopId?.ToString() ?? string.Empty,
                    item.ContentPath
                }
                .Concat(item.Tags ?? Array.Empty<string>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray());
        }

        private static IEnumerable<string> CollectInventoryTags(object proto, object? mainType, object? subType)
        {
            var tags = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            AddInventoryTag(tags, ReadStringMember(proto, "Id"));
            AddInventoryTag(tags, ReadStringMember(proto, "ItemType"));
            AddInventoryTag(tags, ReadStringMember(proto, "Type"));
            AddInventoryTag(tags, ReadMember(proto, "MainType")?.ToString() ?? string.Empty);
            AddInventoryTag(tags, ReadMember(proto, "SubType")?.ToString() ?? string.Empty);
            AddInventoryTag(tags, mainType == null ? string.Empty : ReadStringMember(mainType, "Id"));
            AddInventoryTag(tags, subType == null ? string.Empty : ReadStringMember(subType, "Id"));
            foreach (string memberName in new[] { "Tags", "Labels", "CollectionLabels", "ItemCollectionLabels", "FoodItemSubTypes" })
            {
                foreach (string value in ReadStringValues(ReadMember(proto, memberName)))
                    AddInventoryTag(tags, value);
            }
            return tags;
        }

        private static void AddInventoryTag(ISet<string> tags, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                tags.Add(value.Trim());
        }

        private static IEnumerable<string> ReadStringValues(object? value)
        {
            if (value == null)
                yield break;
            if (value is string text)
            {
                yield return text;
                yield break;
            }
            if (value is IEnumerable enumerable)
            {
                foreach (object? item in enumerable)
                {
                    if (item != null && !string.IsNullOrWhiteSpace(item.ToString()))
                        yield return item.ToString()!;
                }
            }
        }

        private static int CountPendingUnacceptedItemMail(Type dolocApi, string itemId)
        {
            if (dolocApi == null || string.IsNullOrWhiteSpace(itemId))
                return 0;

            object? archive = ReadStaticMember(dolocApi, "archiveHandle");
            object? farmData = archive == null ? null : ReadMember(archive, "farmData");
            object? emailManager = farmData == null ? null : ReadMember(farmData, "emailManager");
            object? emails = emailManager == null ? null : ReadMember(emailManager, "emails");
            int count = 0;
            foreach (object email in EnumerateObjects(emails))
            {
                object? attaches = ReadMember(email, "emailAttaches");
                foreach (object attach in EnumerateObjects(attaches))
                {
                    if (ReadBoolMember(attach, "IsAccept", false) || ReadBoolMember(attach, "isAccept", false))
                        continue;

                    object? reward = ReadMember(attach, "reward");
                    if (reward == null)
                        continue;

                    string rewardItemId = ReadStringMember(reward, "itemName");
                    if (!itemId.Equals(rewardItemId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    count += Math.Max(0, ReadIntMember(reward, "itemCount", 0));
                }
            }
            return count;
        }

        private void LogMailDeliveryResult(string ownerId, MailItemDeliveryResult result)
        {
            runtime.RuntimeMonitor.Log("Mail item delivery owner=" + ownerId +
                " item=" + result.ItemId +
                " requested=" + result.RequestedCount +
                " sent=" + result.Sent +
                " skipped=" + result.Skipped +
                " backpack=" + result.BackpackCount +
                " pendingMail=" + result.PendingMailCount +
                " template=" + result.TemplateName +
                " source=" + FirstText(result.SourceId, "none") +
                " sourceEnabled=" + result.SourceEnabled +
                " sourceKnown=" + result.SourceEnablementKnown +
                " success=" + result.Success +
                " reason=" + result.FailureReason + ".");
        }

        private static InventoryGiveResult InventoryGiveFailed(InventoryGiveResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static MailItemDeliveryResult MailItemDeliveryFailed(MailItemDeliveryResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static WeatherSetResult WeatherSetFailed(WeatherSetResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static TeleportResult TeleportFailed(TeleportResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static TeleportCsvExportResult TeleportCsvExportFailed(TeleportCsvExportResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static InstantSaveDebugResult InstantSaveFailed(InstantSaveDebugResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static TimeSkipResult TimeSkipFailed(TimeSkipResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private static MovementSpeedResult MovementSpeedFailed(MovementSpeedResult result, string reason, string message)
        {
            result.Success = false;
            result.FailureReason = reason ?? string.Empty;
            result.Message = message ?? string.Empty;
            return result;
        }

        private object? GetDolocTable(string tableName)
        {
            object? tables = GetDolocTables();
            return tables == null ? null : ReadMember(tables, tableName);
        }

        private static object? GetDolocTables()
        {
            Type? dolocConfig = ResolveType("DolocTown.Config.DolocConfig, Assembly-CSharp");
            return ReadStaticMember(dolocConfig, "Tables");
        }

        private static MethodInfo? FindMethod(Type? type, string name, int parameterCount)
        {
            if (type == null)
                return null;
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                if (method.Name.Equals(name, StringComparison.Ordinal) && method.GetParameters().Length == parameterCount)
                    return method;
            }
            return null;
        }

        private static IEnumerable<object> EnumerateObjects(object? value)
        {
            if (value == null)
                yield break;
            if (value is IEnumerable enumerable)
            {
                foreach (object? item in enumerable)
                {
                    if (item != null)
                        yield return item;
                }
            }
        }

        private static int InvokeInt(MethodInfo? method, object? target, object?[] args, int fallback)
        {
            try
            {
                object? value = method?.Invoke(target, args);
                return value == null ? fallback : Convert.ToInt32(value);
            }
            catch
            {
                return fallback;
            }
        }

        private static string GetWeatherId(object? weatherInfo)
        {
            if (weatherInfo == null)
                return string.Empty;
            return ReadMember(weatherInfo, "Id")?.ToString() ?? string.Empty;
        }

        private static IEnumerable<object> GetCurrentDayWeatherInfos(object? timeData)
        {
            MethodInfo? getWeatherInfoOfDay = timeData?.GetType().GetMethod("GetWeatherInfoOfDay", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(int) }, null);
            object? result = getWeatherInfoOfDay?.Invoke(timeData, new object?[] { 0 });
            return EnumerateObjects(result).ToArray();
        }

        private static WeatherDebugOption BuildWeatherOption(object weather, string currentWeatherId, HashSet<string> forecastIds)
        {
            string id = GetWeatherId(weather);
            return new WeatherDebugOption
            {
                Id = id,
                DisplayName = FirstText(ReadStringMember(weather, "Title"), id),
                Description = ReadStringMember(weather, "Description"),
                IsCurrent = id.Equals(currentWeatherId ?? string.Empty, StringComparison.OrdinalIgnoreCase),
                IsCurrentDayForecast = forecastIds.Contains(id),
                IsMalignant = ReadBoolMember(weather, "IsMalignantWeather", false),
                IsRainy = ReadBoolMember(weather, "IsRainy", false),
                IsWindy = ReadBoolMember(weather, "IsWindy", false),
                Sun = ReadDoubleMember(weather, "Sun", 0),
                Water = ReadDoubleMember(weather, "Water", 0),
                Wind = ReadDoubleMember(weather, "Wind", 0)
            };
        }

        private IEnumerable<TeleportDestination> BuildTeleportDestinations()
        {
            var destinations = new Dictionary<string, TeleportDestination>(StringComparer.OrdinalIgnoreCase);
            string initMarkPoint = GetInitMarkPointId();
            if (!string.IsNullOrWhiteSpace(initMarkPoint))
                AddTeleportDestination(destinations, "farm:" + initMarkPoint, "农场", "Farm", initMarkPoint, isStation: false, suggestedName: string.Empty, source: "GameInitConfig.initMarkPoint");

            object? stationTable = GetDolocTable("TbStation");
            object? stationList = stationTable == null ? null : ReadMember(stationTable, "DataList");
            foreach (object station in EnumerateObjects(stationList))
            {
                string stationId = ReadStringMember(station, "Id");
                string markPointId = ReadStringMember(station, "MarkPointId");
                if (string.IsNullOrWhiteSpace(stationId) || string.IsNullOrWhiteSpace(markPointId))
                    continue;
                object? stationMarkPoint = ResolveMarkPoint(markPointId);
                string stationRoomId = stationMarkPoint == null ? string.Empty : ReadStringMember(stationMarkPoint, "RoomId");
                string title = BuildStationDisplayName(stationId, ReadStringMember(station, "Title"), markPointId, stationRoomId);
                AddTeleportDestination(destinations, "station:" + stationId, title, "Station", markPointId, isStation: true, suggestedName: BuildMarkPointDisplayName(markPointId, stationRoomId), source: "TbStation.MarkPointId");
            }

            object? markPointTable = GetDolocTable("TbMarkPoint");
            object? markPointList = markPointTable == null ? null : ReadMember(markPointTable, "DataList");
            foreach (object markPoint in EnumerateObjects(markPointList))
            {
                string markPointId = ReadStringMember(markPoint, "Id");
                string roomId = ReadStringMember(markPoint, "RoomId");
                if (!IsWhitelistedMarkPoint(markPointId, roomId))
                    continue;
                AddTeleportDestination(destinations, "mark:" + markPointId, BuildMarkPointDisplayName(markPointId, roomId), "Key", markPointId, isStation: false, suggestedName: BuildMarkPointDisplayName(markPointId, roomId), source: "TbMarkPoint whitelist");
            }

            return destinations.Values
                .Where(d => !string.IsNullOrWhiteSpace(d.MarkPointId))
                .OrderBy(d => d.Group, StringComparer.OrdinalIgnoreCase)
                .ThenBy(d => d.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Take(80)
                .ToArray();
        }

        private void AddTeleportDestination(Dictionary<string, TeleportDestination> destinations, string id, string displayName, string group, string markPointId, bool isStation, string suggestedName, string source)
        {
            if (destinations.ContainsKey(id))
                return;
            object? markPoint = ResolveMarkPoint(markPointId);
            if (markPoint == null)
                return;
            object? position = ReadMember(markPoint, "Position");
            string roomId = ReadStringMember(markPoint, "RoomId");
            destinations[id] = new TeleportDestination
            {
                Id = id,
                DisplayName = FirstText(displayName, markPointId),
                SuggestedDisplayName = FirstText(suggestedName, BuildMarkPointDisplayName(markPointId, roomId), displayName, markPointId),
                Group = group,
                MarkPointId = markPointId,
                RoomId = roomId,
                X = ReadVectorComponent(position, "x"),
                Y = ReadVectorComponent(position, "y"),
                IsStation = isStation,
                IsUnlocked = true,
                Source = source ?? string.Empty
            };
        }

        private object? ResolveMarkPoint(string markPointId)
        {
            object? table = GetDolocTable("TbMarkPoint");
            MethodInfo? getOrDefault = table?.GetType().GetMethod("GetOrDefault", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
            return getOrDefault?.Invoke(table, new object?[] { markPointId });
        }

        private static string GetInitMarkPointId()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? gameManager = ReadStaticMember(dolocApi, "gameManager");
            object? gameInitConfig = gameManager == null ? null : ReadMember(gameManager, "gameInitConfig");
            return gameInitConfig == null ? string.Empty : ReadStringMember(gameInitConfig, "initMarkPoint");
        }

        private static bool IsWhitelistedMarkPoint(string markPointId, string roomId)
        {
            string text = ((markPointId ?? string.Empty) + " " + (roomId ?? string.Empty)).ToLowerInvariant();
            string[] keywords =
            {
                "farm",
                "station",
                "bus",
                "city",
                "town",
                "hall",
                "municip",
                "council",
                "government",
                "research",
                "laboratory",
                "lab",
                "bar",
                "pub",
                "tavern"
            };
            return keywords.Any(text.Contains);
        }

        private static string BuildStationDisplayName(string stationId, string title, string markPointId, string roomId)
        {
            string nativeTitle = FirstText(title, stationId);
            string detail = BuildMarkPointDisplayName(markPointId, roomId);
            if (string.IsNullOrWhiteSpace(detail) || nativeTitle.IndexOf(detail, StringComparison.OrdinalIgnoreCase) >= 0)
                return nativeTitle;
            if (detail.Equals("车站", StringComparison.OrdinalIgnoreCase))
                return nativeTitle;
            return nativeTitle + " / " + detail;
        }

        private static string BuildMarkPointDisplayName(string markPointId, string roomId)
        {
            string text = ((markPointId ?? string.Empty) + " " + (roomId ?? string.Empty)).ToLowerInvariant();
            string originalMarkPointId = (markPointId ?? string.Empty).Trim();
            string originalRoomId = (roomId ?? string.Empty).Trim();
            string cjkMarkPoint = ContainsCjk(originalMarkPointId) ? originalMarkPointId : string.Empty;
            if (!string.IsNullOrWhiteSpace(cjkMarkPoint))
            {
                if (cjkMarkPoint.Contains("农场") && (cjkMarkPoint.Contains("车站") || cjkMarkPoint.Contains("公交") || cjkMarkPoint.Contains("小径") || cjkMarkPoint.Contains("道路")))
                    return "农场小径/公交站";
                if (cjkMarkPoint.Contains("市政"))
                    return "市政厅";
                if (cjkMarkPoint.Contains("研究"))
                    return "研究所";
                if (cjkMarkPoint.Contains("酒吧"))
                    return "酒吧";
                return cjkMarkPoint;
            }

            if (text.Contains("farm") && (text.Contains("station") || text.Contains("bus") || text.Contains("path") || text.Contains("road")))
                return "农场小径/公交站";
            if (text.Contains("city") && (text.Contains("station") || text.Contains("bus") || text.Contains("path") || text.Contains("road")))
                return "城镇车站";
            if (text.Contains("farm"))
                return BuildDetailedLocationName("农场", originalMarkPointId, originalRoomId);
            if (text.Contains("hall") || text.Contains("municip") || text.Contains("council") || text.Contains("government"))
                return "市政厅";
            if (text.Contains("research") || text.Contains("laboratory") || text.Contains("lab"))
                return "研究所";
            if (text.Contains("bar") || text.Contains("pub") || text.Contains("tavern"))
                return "酒吧";
            if (text.Contains("station"))
                return "车站";
            if (text.Contains("city") || text.Contains("town"))
                return "城镇";
            return FirstText(markPointId ?? string.Empty, roomId ?? string.Empty, "Teleport");
        }

        private static string BuildDetailedLocationName(string baseName, string markPointId, string roomId)
        {
            string detail = ExtractReadableLocationDetail(markPointId);
            if (string.IsNullOrWhiteSpace(detail))
                detail = ExtractReadableLocationDetail(roomId);
            if (string.IsNullOrWhiteSpace(detail))
                return baseName;
            if (detail.Equals(baseName, StringComparison.OrdinalIgnoreCase) || detail.IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0)
                return detail;
            return baseName + "-" + detail;
        }

        private static string ExtractReadableLocationDetail(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            string trimmed = value.Trim();
            int dash = Math.Max(trimmed.LastIndexOf('-'), trimmed.LastIndexOf('_'));
            if (dash >= 0 && dash < trimmed.Length - 1)
            {
                string suffix = trimmed.Substring(dash + 1).Trim();
                if (ContainsCjk(suffix))
                    return suffix;
            }
            return ContainsCjk(trimmed) ? trimmed : string.Empty;
        }

        private static bool ContainsCjk(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            foreach (char c in value)
            {
                if (c >= 0x4E00 && c <= 0x9FFF)
                    return true;
            }
            return false;
        }

        private static TeleportSnapshot BuildTeleportSnapshot(object? room, object? position)
        {
            return new TeleportSnapshot
            {
                RoomId = room == null ? string.Empty : FirstText(ReadStringMember(room, "RoomId"), ReadStringMember(room, "roomId"), room.GetType().Name),
                RoomTitle = room == null ? string.Empty : ReadStringMember(room, "Title"),
                RoomType = room == null ? string.Empty : (ReadMember(room, "Type")?.ToString() ?? room.GetType().Name),
                X = ReadVectorComponent(position, "x"),
                Y = ReadVectorComponent(position, "y"),
                Z = ReadVectorComponent(position, "z")
            };
        }

        private InstantSaveDebugState GetInstantSaveDebugState()
        {
            var state = new InstantSaveDebugState();
            try
            {
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? room = ReadStaticMember(dolocApi, "CurrentRoom");
                object? position = ReadStaticMember(dolocApi, "AgentPosition");
                MethodInfo? saveGame = FindMethod(dolocApi, "SaveGame", 1);
                int archiveIndex = archive == null ? -1 : ReadIntMember(archive, "archiveIndex", -1);
                state.SaveSlot = archiveIndex >= 0 ? archiveIndex : (int?)null;
                state.CurrentLocation = BuildTeleportSnapshot(room, position);
                state.CanSave = dolocApi != null && archive != null && saveGame != null && state.SaveSlot != null;
                state.FailureReason = state.CanSave ? string.Empty : dolocApi == null ? "missing-dolocapi" : archive == null ? "missing-archive" : saveGame == null ? "missing-savegame" : "missing-archive-index";
                state.Message = state.CanSave
                    ? "Ready to save slot/index=" + archiveIndex + " at " + FormatTeleportSnapshot(state.CurrentLocation) + "."
                    : "Instant save is not ready: " + state.FailureReason + ".";
                return state;
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Instant save debug state failed.", ex.ToString());
                state.CanSave = false;
                state.FailureReason = ex.GetType().Name;
                state.Message = ex.Message;
                return state;
            }
        }

        private static string FormatTeleportSnapshot(TeleportSnapshot snapshot)
        {
            if (snapshot == null)
                return "unknown";
            return "room=" + FirstText(snapshot.RoomTitle, snapshot.RoomId, "unknown") +
                ", id=" + FirstText(snapshot.RoomId, "unknown") +
                ", pos=" + snapshot.X.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                snapshot.Y.ToString("0.###", CultureInfo.InvariantCulture) + "," +
                snapshot.Z.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Csv(string value)
        {
            value ??= string.Empty;
            bool quote = value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            value = value.Replace("\"", "\"\"");
            return quote ? "\"" + value + "\"" : value;
        }

        private TimeDebugState GetTimeDebugState()
        {
            try
            {
                WeatherDebugState weather = GetState();
                int minute = 0;
                Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
                object? archive = ReadStaticMember(dolocApi, "archiveHandle");
                object? dateNow = archive == null ? null : ReadMember(archive, "DateNow");
                if (dateNow != null)
                    minute = ReadIntMember(dateNow, "Minute", 0);

                int periodStart = GetDebugWeatherPeriodStart(weather.Hour);
                int periodEnd = GetDebugWeatherPeriodEnd(weather.Hour);
                return new TimeDebugState
                {
                    Year = weather.Year,
                    Month = weather.Month,
                    Day = weather.Day,
                    Hour = weather.Hour,
                    Minute = minute,
                    CurrentWeatherId = weather.CurrentWeatherId,
                    CurrentWeatherName = weather.CurrentWeatherName,
                    SeasonName = weather.SeasonName,
                    Period = periodStart.ToString("00") + ":00-" + periodEnd.ToString("00") + ":00"
                };
            }
            catch (Exception ex)
            {
                runtime.Diagnostics.RecordError("DTMAPI.GameBridge", "Time debug state failed.", ex.ToString());
                return new TimeDebugState();
            }
        }

        private static int GameMinutesToSeconds(object globalParameter, int gameMinutes)
        {
            MethodInfo? convert = globalParameter.GetType().GetMethod("GameMinutes2Secs", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(float) }, null);
            if (convert != null)
            {
                object? value = convert.Invoke(globalParameter, new object[] { (float)gameMinutes });
                if (value != null)
                    return Convert.ToInt32(value);
            }

            int tuLength = Math.Max(1, ReadIntMember(globalParameter, "TULength", 1));
            int tu2Min = Math.Max(1, ReadIntMember(globalParameter, "TU2Min", 1));
            return Math.Max(1, (int)Math.Round(gameMinutes * (double)tuLength / tu2Min));
        }

        private static int GetNextDebugWeatherPeriodTarget(int currentHour)
        {
            currentHour = ((currentHour % 24) + 24) % 24;
            if (currentHour < 6)
                return 6;
            if (currentHour < 18)
                return 18;
            return 24;
        }

        private static int GetDebugWeatherPeriodStart(int currentHour)
        {
            currentHour = ((currentHour % 24) + 24) % 24;
            if (currentHour < 6)
                return 0;
            if (currentHour < 18)
                return 6;
            return 18;
        }

        private static int GetDebugWeatherPeriodEnd(int currentHour)
        {
            currentHour = ((currentHour % 24) + 24) % 24;
            if (currentHour < 6)
                return 6;
            if (currentHour < 18)
                return 18;
            return 24;
        }

        private static void InvokeWakeUp(Type dolocApi)
        {
            MethodInfo? wake = dolocApi.GetMethod("OnWakeUp", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(bool), typeof(bool), typeof(bool) }, null);
            wake?.Invoke(null, new object?[] { false, true, false });
        }

        private MovementDebugState GetMovementDebugState(string source)
        {
            object? motionAbility = ResolveMotionAbility();
            double moveSpeed = motionAbility == null ? 0 : ReadDoubleMember(motionAbility, "MoveSpeed", 0);
            return new MovementDebugState
            {
                Multiplier = movementSpeedMultiplier,
                MoveSpeed = moveSpeed,
                IsDefault = Math.Abs(movementSpeedMultiplier - 1d) < 0.001,
                Source = source ?? string.Empty
            };
        }

        private static object? ResolveMotionAbility()
        {
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? agent = ReadStaticMember(dolocApi, "agent");
            object? motion = agent == null ? null : ReadMember(agent, "MotionAbility");
            if (motion != null)
                return motion;

            object? abilitySystem = ReadStaticMember(dolocApi, "AbilitySystem");
            return abilitySystem == null ? null : ReadMember(abilitySystem, "motionAbility");
        }

        private static double ClampDebugSpeedMultiplier(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 1;
            return Math.Min(4, Math.Max(0.5, value));
        }

        private static string FormatTimeDebugState(TimeDebugState state)
        {
            if (state == null)
                return "unknown";
            return state.Year + "-" + state.Month + "-" + state.Day + " " + state.Hour.ToString("00") + ":" + state.Minute.ToString("00") + " weather=" + FirstText(state.CurrentWeatherName, state.CurrentWeatherId);
        }

        private static double ReadVectorComponent(object? vector, string name)
        {
            if (vector == null)
                return double.NaN;
            object? value = ReadMember(vector, name);
            return value == null ? double.NaN : Convert.ToDouble(value);
        }

        private void LogOnce(HashSet<string> keys, string key, string message)
        {
            if (!keys.Add(key))
                return;
            runtime.RuntimeMonitor.Log(message);
        }

        private sealed class SecondMotorRuntime
        {
            public SecondMotorRuntime(string ownerUniqueId, SecondMotorOptions options)
            {
                OwnerUniqueId = ownerUniqueId;
                Options = options;
            }

            public string OwnerUniqueId { get; set; }
            public SecondMotorOptions Options { get; set; }
            public object? GameObject { get; set; }
            public object? Controller { get; set; }
            public object? Interactable { get; set; }
            public object? Room { get; set; }
            public object? LastPosition { get; set; }
            public object? PendingTransitionRoom { get; set; }
            public string PendingTransitionRoomId { get; set; } = string.Empty;
            public object? PendingTransitionPosition { get; set; }
            public bool IsRiding { get; set; }
            public string LastFailureReason { get; set; } = string.Empty;
            public string LastMessage { get; set; } = string.Empty;
            public string AppearanceSummary { get; set; } = string.Empty;
        }

        private sealed class MachineRuntimeEntry
        {
            public string OwnerId { get; set; } = string.Empty;
            public string MachineId { get; set; } = string.Empty;
            public string EquipmentId { get; set; } = string.Empty;
            public string MachineKey { get; set; } = string.Empty;
            public int RemainingFuel { get; set; }
            public int NextDueTotalTus { get; set; }
            public int LastObservedTotalTus { get; set; } = -1;
            public int ProductionCycleCount { get; set; }
            public string LastMode { get; set; } = string.Empty;
            public int LastFuelCost { get; set; }
            public int LastElectricPowerCost { get; set; }
            public string LastOutputItemId { get; set; } = string.Empty;
            public int LastOutputCount { get; set; }
            public string LastOutputTarget { get; set; } = string.Empty;
            public int LastStorageFilledSlots { get; set; }
            public int LastStorageCapacity { get; set; }
            public int LastStorageLineCapacity { get; set; }
            public string LastVisualScaleSummary { get; set; } = string.Empty;
            public MachineOutputRule? LastOutputRule { get; set; }
        }

        private sealed class PendingOilResourceHit
        {
            public PendingOilResourceHit(object resource, string resourceName, int healthBefore)
            {
                Resource = resource;
                ResourceName = resourceName;
                HealthBefore = healthBefore;
            }

            public object Resource { get; }

            public string ResourceName { get; }

            public int HealthBefore { get; }
        }

        private sealed class EquipmentSlotRuntimeEntry
        {
            public int Index { get; set; }
            public string SlotId { get; set; } = string.Empty;
            public string ItemId { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string SkillId { get; set; } = string.Empty;
            public bool Applied { get; set; }
            public string LastMessage { get; set; } = string.Empty;
            public object? NativeItem { get; set; }
            public object? NativeFunction { get; set; }
        }

        [DataContract]
        private sealed class EquipmentSlotStorageDocument
        {
            [DataMember(Name = "ownerId")]
            public string OwnerId { get; set; } = string.Empty;

            [DataMember(Name = "savedAt")]
            public string SavedAt { get; set; } = string.Empty;

            [DataMember(Name = "slots")]
            public List<EquipmentSlotStorageEntry> Slots { get; set; } = new List<EquipmentSlotStorageEntry>();
        }

        [DataContract]
        private sealed class EquipmentSlotStorageEntry
        {
            [DataMember(Name = "index")]
            public int Index { get; set; }

            [DataMember(Name = "slotId")]
            public string SlotId { get; set; } = string.Empty;

            [DataMember(Name = "itemId")]
            public string ItemId { get; set; } = string.Empty;

            [DataMember(Name = "displayName")]
            public string DisplayName { get; set; } = string.Empty;

            [DataMember(Name = "skillId")]
            public string SkillId { get; set; } = string.Empty;

            [DataMember(Name = "lastMessage")]
            public string LastMessage { get; set; } = string.Empty;
        }

        private sealed class OriginalMotorSnapshot
        {
            public OriginalMotorSnapshot(object? room, object position, bool unlocked)
            {
                Room = room;
                Position = position;
                Unlocked = unlocked;
            }

            public object? Room { get; }
            public object Position { get; }
            public bool Unlocked { get; }
        }

        private sealed class GlobalMotorTuningSnapshot
        {
            public GlobalMotorTuningSnapshot(double motorHorizontalAcceleration, double motorHorizontalRevertAcceleration, double motorHorizontalDeceleration, double motorHorizontalMaxSpeed)
            {
                MotorHorizontalAcceleration = motorHorizontalAcceleration;
                MotorHorizontalRevertAcceleration = motorHorizontalRevertAcceleration;
                MotorHorizontalDeceleration = motorHorizontalDeceleration;
                MotorHorizontalMaxSpeed = motorHorizontalMaxSpeed;
            }

            public double MotorHorizontalAcceleration { get; }
            public double MotorHorizontalRevertAcceleration { get; }
            public double MotorHorizontalDeceleration { get; }
            public double MotorHorizontalMaxSpeed { get; }
        }

        private sealed class AnimalProgressInfo
        {
            public string AnimalId { get; set; } = string.Empty;
            public string OutputId { get; set; } = string.Empty;
            public string OutputTitle { get; set; } = string.Empty;
            public int Current { get; set; }
            public int Threshold { get; set; }
            public double Progress { get; set; }
        }

        private sealed class AnimalProgressRenderRow
        {
            public string OwnerId { get; set; } = string.Empty;
            public string OutputTitle { get; set; } = string.Empty;
            public int Current { get; set; }
            public int Threshold { get; set; }
            public double Progress { get; set; }
            public DtmColor Color { get; set; }
        }
    }
}
