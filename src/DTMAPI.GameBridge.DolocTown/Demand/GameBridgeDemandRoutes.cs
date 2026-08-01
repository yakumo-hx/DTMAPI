using System;
using System.Collections.Generic;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal static class GameBridgeDemandRoutes
    {
        internal const string Camera = "Camera";
        internal const string ItemDisplayNameEnvironmentReset = "ItemDisplayName.EnvironmentReset";
        internal const string FishingCompatibility = "FishingAutomation.Compatibility";
        internal const string FishRoeTooltip = "FishRoeTooltip";
        internal const string ChestLocatorEnhancer = "ChestLocatorEnhancer";
        internal const string SaveSlots = "SaveSlots";
        internal const string NativeUiLayoutDiagnostics = "NativeUiLayoutDiagnostics";
        internal const string CropHarvesting = "CropHarvesting";
        internal const string AnimalViewer = "AnimalViewer";
        internal const string CustomAnimalAnimatorBridge = "CustomAnimalAnimatorBridge";
        internal const string AudioReplacement = "AudioReplacement";
        internal const string ActionSpeed = "ActionSpeed";
        internal const string ActionCompletion = "ActionCompletion";

        internal const string CoreLifecycle = "GameBridge.CoreLifecycle";
        internal const string CoreUiContext = "GameBridge.CoreUiContext";
        internal const string ContentRefreshDrain = "GameBridge.ContentRefreshDrain";
        internal const string AgentStateLifecycleShared = "GameBridge.Shared.AgentStateLifecycle";
        internal const string AgentStateToolExitShared = "GameBridge.Shared.AgentStateToolExit";
        internal const string AgentStateInteractExitShared = "GameBridge.Shared.AgentStateInteractExit";
        internal const string AgentStateBaseExitShared = "GameBridge.Shared.AgentStateBaseExit";
        internal const string ToolColliderShared = "GameBridge.Shared.ToolCollider";
        internal const string AnimalViewerSession = "AnimalViewer.Session";
        internal const string FishingCompatibilityUpdater = "FishingAutomation.Compatibility.Updater";
        internal const string ActionSpeedAutoFill = "ActionSpeed.AutoFill";
        internal const string ActionSpeedToolStages = "ActionSpeed.ToolStages";
        internal const string ActionSpeedInteractionStages = "ActionSpeed.InteractionStages";
        internal const string AudioReplacementPending = "AudioReplacement.Pending";
        internal const string AudioReplacementAnimalVoiceContext = "AudioReplacement.AnimalVoiceContext";
        internal const string CustomAnimalRuntimeSession = "CustomAnimalAnimatorBridge.RuntimeSession";
        internal const string SaveSlotsPendingRestore = "SaveSlots.PendingRestore";
        internal const string WorkshopAuthoring = "GameBridge.WorkshopAuthoring";
        internal const string WorkshopPendingUploadPlan = "GameBridge.WorkshopPendingUploadPlan";
        internal const string TitleSettingsUi = "GameBridge.TitleSettingsUi";
        internal const string GlobalInputSampling = "GameBridge.GlobalInputSampling";
        internal const string QaHost = "GameBridge.QaHost";
        internal const string AuthorSessionReload = "GameBridge.AuthorSessionReload";

        internal const string FrameworkOwner = "DTMAPI.Framework";
        internal const string OperationOwner = "DTMAPI.GameBridge.Operations";
        internal const string QaOwner = "DTMAPI.QA";
        internal const string AuthorOwner = "DTMAPI.AuthorSession";

        internal static IReadOnlyList<RuntimeCapabilityDescriptor> Catalog { get; } = new[]
        {
            Descriptor(Camera, RuntimeCapabilityOutcome.StoppedRemovable, "frame", "camera.compatibility.set-env", "Frozen camera ABI leases activate an exact compatibility owner; ProductNative Zoom owns the same target independently and both orders fail closed."),
            Descriptor(ItemDisplayNameEnvironmentReset, RuntimeCapabilityOutcome.ProcessPinnedDormant, "none", "camera.set-env", "A successful shared item-title lookup arms the common environment-reset Hook; caching remains disabled until that physical Hook is ready."),
            Descriptor(FishingCompatibility, RuntimeCapabilityOutcome.ProcessPinnedDormant, "frame-compatibility-only", "fishing.compatibility", "An enabled frozen fishing-ABI consumer activates the compatibility-only hook executor."),
            Descriptor(FishRoeTooltip, RuntimeCapabilityOutcome.ProcessPinnedDormant, "none", "item.fish-roe-tooltip", "Enabled provider registration activates item display hooks."),
            Descriptor(ChestLocatorEnhancer, RuntimeCapabilityOutcome.ProcessPinnedDormant, "none", "inventory.chest-locator", "Enabled policy activates the available-inventories postfix."),
            Descriptor(SaveSlots, RuntimeCapabilityOutcome.ProcessPinnedDormant, "750ms-or-3s", "save-slots.compatibility-host", "Frozen ABI demand activates only the fixed-six/twelve archive-count compatibility executor; save UI Hooks remain absent."),
            Descriptor(NativeUiLayoutDiagnostics, RuntimeCapabilityOutcome.Mandatory, "250ms", "native-ui.layout-repair", "Mandatory framework repair for official title and menu layouts."),
            Descriptor(CropHarvesting, RuntimeCapabilityOutcome.StoppedRemovable, "operation-only", string.Empty, "Synchronous explicit crop scan/harvest operation; no persistent hook or updater."),
            Descriptor(AnimalViewer, RuntimeCapabilityOutcome.ProcessPinnedDormant, "session-child", "animal-viewer.display", "Enabled display registration activates native viewer lifecycle hooks."),
            Descriptor(CustomAnimalAnimatorBridge, RuntimeCapabilityOutcome.ProcessPinnedDormant, "generation-or-session", "custom-animal.behavior", "Content-generation behavior route; loaded native bundles may remain process pinned."),
            Descriptor(AudioReplacement, RuntimeCapabilityOutcome.ProcessPinnedDormant, "pending-only", "audio.wwise", "Enabled definitions activate Wwise/animal-context hooks; updater is pending-load only."),
            Descriptor(ActionSpeed, RuntimeCapabilityOutcome.ProcessPinnedDormant, "autofill-child", "action-speed.native-stages", "Enabled native-stage policy activates action hooks and shared lifecycle exits."),
            Descriptor(ActionCompletion, RuntimeCapabilityOutcome.ProcessPinnedDormant, "none", "action-completion.closure", "Enabled one-action policy activates only its ToolCollider/InteractExit dependency closure."),

            Descriptor(CoreLifecycle, RuntimeCapabilityOutcome.Mandatory, "lifecycle", "gamebridge.core", "Base save/frame/title lifecycle route."),
            Descriptor(CoreUiContext, RuntimeCapabilityOutcome.Mandatory, "frame", string.Empty, "Single shared producer for the native UI input context."),
            Descriptor(ContentRefreshDrain, RuntimeCapabilityOutcome.Mandatory, "frame-cheap-dirty-check", string.Empty, "Base generation drain; dirty checks perform no optional file or directory I/O."),
            Descriptor(AgentStateLifecycleShared, RuntimeCapabilityOutcome.Deferred, "none", string.Empty, "Legacy aggregate identifier retained for diagnostics; consumers use the exact ToolExit, InteractExit, or BaseExit routes."),
            Descriptor(AgentStateToolExitShared, RuntimeCapabilityOutcome.ProcessPinnedDormant, "none", "shared.agent-state-tool-exit", "Reference-counted AgentStateTool.OnExit postfix used by ActionSpeed."),
            Descriptor(AgentStateInteractExitShared, RuntimeCapabilityOutcome.ProcessPinnedDormant, "none", "shared.agent-state-interact-exit", "Reference-counted AgentStateInteract.OnExit postfix used by ActionCompletion and ActionSpeed."),
            Descriptor(AgentStateBaseExitShared, RuntimeCapabilityOutcome.ProcessPinnedDormant, "none", "shared.agent-state-base-exit", "Reference-counted AgentStateBase.OnExit postfix used by ActionSpeed."),
            Descriptor(ToolColliderShared, RuntimeCapabilityOutcome.ProcessPinnedDormant, "none", "shared.tool-collider", "Reference-counted ToolCollider.HandleTools postfix route."),
            Descriptor(AnimalViewerSession, RuntimeCapabilityOutcome.StoppedRemovable, "80ms", string.Empty, "Derived overlay session updater; separate from configured hook demand."),
            Descriptor(FishingCompatibilityUpdater, RuntimeCapabilityOutcome.StoppedRemovable, "frame", string.Empty, "Updater for an enabled frozen fishing-ABI compatibility consumer."),
            Descriptor(ActionSpeedAutoFill, RuntimeCapabilityOutcome.StoppedRemovable, "frame-policy-cooldown", string.Empty, "Auto-fill polling only while an enabled auto-fill policy exists."),
            Descriptor(ActionSpeedToolStages, RuntimeCapabilityOutcome.ProcessPinnedDormant, "none", "action-speed.tool-stages", "Derived tool-stage hook group; does not install interaction-stage hooks."),
            Descriptor(ActionSpeedInteractionStages, RuntimeCapabilityOutcome.ProcessPinnedDormant, "none", "action-speed.interaction-stages", "Derived interaction/eat/continuous hook group; does not install ToolEnter."),
            Descriptor(AudioReplacementPending, RuntimeCapabilityOutcome.StoppedRemovable, "frame-pending", string.Empty, "Async audio request/retry operation updater."),
            Descriptor(AudioReplacementAnimalVoiceContext, RuntimeCapabilityOutcome.ProcessPinnedDormant, "none", "audio.animal-voice-context", "Derived only from committed enabled AnimalVoice definitions; requires both Animal.PlayAnimalSound context hooks."),
            Descriptor(CustomAnimalRuntimeSession, RuntimeCapabilityOutcome.Removed, "none", string.Empty, "No standalone polling route remains; native callbacks own live custom-animal sessions."),
            Descriptor(SaveSlotsPendingRestore, RuntimeCapabilityOutcome.StoppedRemovable, "750ms", string.Empty, "Official archive-count restoration operation when the native manager is temporarily unavailable."),
            Descriptor(WorkshopAuthoring, RuntimeCapabilityOutcome.ProcessPinnedDormant, "event-driven", "workshop.authoring", "Local author/upload workflow hooks, separate from ordinary subscription refresh."),
            Descriptor(WorkshopPendingUploadPlan, RuntimeCapabilityOutcome.StoppedRemovable, "frame-until-4s", string.Empty, "Bounded delayed native upload-plan callback operation."),
            Descriptor(TitleSettingsUi, RuntimeCapabilityOutcome.Deferred, "title-session", string.Empty, "Bootstrap-owned title settings UI route; catalogued for cross-host wiring."),
            Descriptor(GlobalInputSampling, RuntimeCapabilityOutcome.Deferred, "subscription-driven", string.Empty, "Bootstrap-owned global input sampling route; catalogued for event-demand wiring."),
            Descriptor(QaHost, RuntimeCapabilityOutcome.StoppedRemovable, "frame-explicit-qa", string.Empty, "Optional QA participant; only ExplicitQa demand may activate it."),
            Descriptor(AuthorSessionReload, RuntimeCapabilityOutcome.RestartRequired, "operation-only", string.Empty, "Named same-process restart boundary for code assembly, manifest/source identity, custom-animal, native-content or content-format replacement transitions.")
        };

        internal static void SetOwnerDemand(
            DtmApiRuntime runtime,
            string capabilityId,
            string ownerId,
            RuntimeDemandSourceType sourceType,
            RuntimeDemandLifetime lifetime,
            string demandKey,
            bool active,
            string reason)
        {
            if (runtime == null || string.IsNullOrWhiteSpace(ownerId))
                return;
            runtime.DemandCoordinator.SetDemand(
                capabilityId,
                ownerId,
                sourceType,
                lifetime,
                demandKey,
                active ? 1 : 0,
                reason ?? string.Empty);
        }

        private static RuntimeCapabilityDescriptor Descriptor(
            string capabilityId,
            RuntimeCapabilityOutcome outcome,
            string cadence,
            string hookRoute,
            string details)
        {
            return new RuntimeCapabilityDescriptor(capabilityId, outcome, cadence, hookRoute, details);
        }
    }
}
