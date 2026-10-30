using System;
using System.Collections.Generic;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    internal static class GameBridgeDemandRoutes
    {
        internal const string ItemDisplayNameEnvironmentReset = "ItemDisplayName.EnvironmentReset";
        internal const string NativeUiLayoutDiagnostics = "NativeUiLayoutDiagnostics";
        internal const string CropHarvesting = "CropHarvesting";

        internal const string CoreLifecycle = "GameBridge.CoreLifecycle";
        internal const string CoreUiContext = "GameBridge.CoreUiContext";
        internal const string ContentRefreshDrain = "GameBridge.ContentRefreshDrain";
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
            Descriptor(ItemDisplayNameEnvironmentReset, RuntimeCapabilityOutcome.ProcessPinnedDormant, "none", "camera.set-env", "A successful shared item-title lookup arms the common environment-reset Hook; caching remains disabled until that physical Hook is ready."),
            Descriptor(NativeUiLayoutDiagnostics, RuntimeCapabilityOutcome.Mandatory, "250ms", "native-ui.layout-repair", "Mandatory framework repair for official title and menu layouts."),
            Descriptor(CropHarvesting, RuntimeCapabilityOutcome.StoppedRemovable, "operation-only", string.Empty, "Synchronous explicit crop scan/harvest operation; no persistent hook or updater."),

            Descriptor(CoreLifecycle, RuntimeCapabilityOutcome.Mandatory, "lifecycle", "gamebridge.core", "Base save/frame/title lifecycle route."),
            Descriptor(CoreUiContext, RuntimeCapabilityOutcome.Mandatory, "frame", string.Empty, "Single shared producer for the native UI input context."),
            Descriptor(ContentRefreshDrain, RuntimeCapabilityOutcome.Mandatory, "frame-cheap-dirty-check", string.Empty, "Base generation drain; dirty checks perform no optional file or directory I/O."),
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
