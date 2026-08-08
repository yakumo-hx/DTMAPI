using System;
using System.Collections.Generic;
using System.Linq;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private static readonly string[] GameBridgeOwnerDemandCapabilityIds = GameBridgeDemandRoutes.Catalog
            .Select(route => route.CapabilityId)
            .ToArray();

        private ModOwnerCleanupParticipantResult RemoveGameBridgeOwnerResources(string ownerId, ModOwnerCleanupReason reason)
        {
            string reasonText = reason.ToString();
            int removed = 0;
            var failures = new List<string>();
            int demandRemoved = runtime.DemandCoordinator.RemoveOwnerDemandsForCapabilities(
                ownerId,
                GameBridgeOwnerDemandCapabilityIds,
                "GameBridge owner cleanup " + reasonText);
            removed += TryRemoveGameBridgeOwnerStep("ActionSpeed", ownerId, () => actionSpeedFeature?.Service.RemoveOwner(ownerId, reasonText) ?? 0, failures);
            removed += TryRemoveGameBridgeOwnerStep("ActionCompletion", ownerId, () => actionCompletionFeature?.Service.RemoveOwner(ownerId, reasonText) ?? 0, failures);
            removed += TryRemoveGameBridgeOwnerStep("AnimalViewer", ownerId, () => animalViewerFeature?.Service.RemoveOwner(ownerId, reasonText) ?? 0, failures);
            removed += TryRemoveGameBridgeOwnerStep("AudioReplacement", ownerId, () => audioReplacementFeature?.Service.RemoveOwner(ownerId, reasonText) ?? 0, failures);
            removed += TryRemoveGameBridgeOwnerStep(
                "Camera",
                ownerId,
                () => cameraFeature?.RemoveOwner(ownerId, reasonText) ?? 0,
                failures,
                () => cameraFeature?.CountOwnerResources(ownerId) ?? 0);
            removed += TryRemoveGameBridgeOwnerStep("ChestLocatorEnhancer", ownerId, () => chestLocatorEnhancerFeature?.Service.RemoveOwner(ownerId, reasonText) ?? 0, failures);
            removed += TryRemoveGameBridgeOwnerStep("CropHarvesting", ownerId, () => cropHarvestingFeature?.Service.RemoveOwner(ownerId, reasonText) ?? 0, failures);
            removed += TryRemoveGameBridgeOwnerStep("CustomAnimalAnimator", ownerId, () => customAnimalAnimatorBridgeFeature?.Service.RemoveOwner(ownerId, reasonText) ?? 0, failures);
            removed += TryRemoveGameBridgeOwnerStep("FishRoeTooltip", ownerId, () => fishRoeTooltipFeature?.Service.RemoveOwner(ownerId, reasonText) ?? 0, failures);
            removed += TryRemoveGameBridgeOwnerStep("EquipmentSlots", ownerId, () => equipmentSlotsFeature?.Service.RemoveOwner(ownerId, reasonText) ?? 0, failures);
            removed += TryRemoveGameBridgeOwnerStep("SaveSlots", ownerId, () => saveSlotsFeature?.Service.RemoveOwner(ownerId, reasonText) ?? 0, failures);
            removed += TryRemoveGameBridgeOwnerStep("FishingAutomationCompatibility", ownerId, () => fishingCompatibilityFeature?.RemoveOwner(ownerId, reasonText) ?? 0, failures);
            removed += TryRemoveGameBridgeOwnerStep("DebugConsole", ownerId, () => (debugConsoleApi as IOwnerBoundApiHost)?.RemoveOwner(ownerId, reasonText) ?? 0, failures);
            removed += TryRemoveGameBridgeOwnerStep("DebugActions", ownerId, () => debugActionApi.RemoveOwner(ownerId, reasonText), failures);
            int remaining = CountGameBridgeOwnerResources(ownerId);
            string details = "Removed GameBridge owner policies, states, leases, callbacks, and native overlays; failedSteps=" +
                (failures.Count == 0 ? "none" : string.Join("|", failures)) +
                "; featureRootsRemoved=" + removed +
                "; demandRootsRemoved=" + demandRemoved +
                "; accounting=featureRoots+demandRoots because both are independently retained owner resources.";
            return new ModOwnerCleanupParticipantResult(removed + demandRemoved, remaining, failures.Count, details);
        }

        internal int CountGameBridgeOwnerResources(string ownerId)
        {
            ownerId ??= string.Empty;
            int featureRoots = (actionSpeedFeature?.Service.CountOwnerResources(ownerId) ?? 0) +
                (actionCompletionFeature?.Service.CountOwnerResources(ownerId) ?? 0) +
                (animalViewerFeature?.Service.CountOwnerResources(ownerId) ?? 0) +
                (audioReplacementFeature?.Service.CountOwnerResources(ownerId) ?? 0) +
                (cameraFeature?.CountOwnerResources(ownerId) ?? 0) +
                (chestLocatorEnhancerFeature?.Service.CountOwnerResources(ownerId) ?? 0) +
                (cropHarvestingFeature?.Service.CountOwnerResources(ownerId) ?? 0) +
                (customAnimalAnimatorBridgeFeature?.Service.CountOwnerResources(ownerId) ?? 0) +
                (fishRoeTooltipFeature?.Service.CountOwnerResources(ownerId) ?? 0) +
                (equipmentSlotsFeature?.Service.CountOwnerResources(ownerId) ?? 0) +
                (saveSlotsFeature?.Service.CountOwnerResources(ownerId) ?? 0) +
                (fishingCompatibilityFeature?.CountOwnerResources(ownerId) ?? 0) +
                ((debugConsoleApi as IOwnerBoundApiHost)?.CountOwnerResources(ownerId) ?? 0) +
                debugActionApi.CountOwnerResources(ownerId);
            int demandRoots = runtime.DemandCoordinator.GetOwnerDemandCountForCapabilities(
                ownerId,
                GameBridgeOwnerDemandCapabilityIds);
            return featureRoots + demandRoots;
        }

        internal ModOwnerCleanupParticipantResult RemoveGameBridgeOwnerResourcesForTests(string ownerId, ModOwnerCleanupReason reason) =>
            RemoveGameBridgeOwnerResources(ownerId, reason);

        private int TryRemoveGameBridgeOwnerStep(string step, string ownerId, Func<int> cleanup, List<string> failures, Func<int>? countRemaining = null)
        {
            try
            {
                int removed = cleanup();
                int remaining = countRemaining?.Invoke() ?? 0;
                if (remaining > 0)
                    failures.Add(step + ":remaining=" + remaining);
                return removed;
            }
            catch (Exception ex)
            {
                failures.Add(step + ":" + ex.GetType().Name);
                try
                {
                    runtime.RuntimeMonitor.Log(
                        "GameBridge owner cleanup step failed owner=" + ownerId + " step=" + step + " error=" + ex.GetType().Name + ": " + ex.Message,
                        LogLevel.Warn);
                }
                catch
                {
                }
                return 0;
            }
        }

        private sealed class GameBridgeModOwnerCleanupParticipant : IModOwnerCleanupParticipant
        {
            private readonly DolocTownGameBridge bridge;

            public GameBridgeModOwnerCleanupParticipant(DolocTownGameBridge bridge)
            {
                this.bridge = bridge;
            }

            public string ParticipantId => "DTMAPI.GameBridge.DolocTown.OwnerResources";

            public ModOwnerCleanupParticipantResult RemoveOwner(string ownerId, ModOwnerCleanupReason reason)
            {
                return bridge.RemoveGameBridgeOwnerResources(ownerId, reason);
            }
        }
    }
}
