using System;
using System.Collections.Generic;
using DTMAPI.Abstractions;
using DTMAPI.Core.Runtime;

namespace DTMAPI.GameBridge.DolocTown
{
    public sealed partial class DolocTownGameBridge
    {
        private ModOwnerCleanupParticipantResult RemoveGameBridgeOwnerResources(string ownerId, ModOwnerCleanupReason reason)
        {
            string reasonText = reason.ToString();
            int removed = 0;
            var failures = new List<string>();
            removed += TryRemoveGameBridgeOwnerStep("CropHarvesting", ownerId, () => cropHarvestingFeature?.Service.RemoveOwner(ownerId, reasonText) ?? 0, failures);
            int remaining = CountGameBridgeOwnerResources(ownerId);
            string details = "Removed GameBridge owner policies, states, leases, callbacks, and native overlays; failedSteps=" +
                (failures.Count == 0 ? "none" : string.Join("|", failures)) +
                "; resourcesRemoved=" + removed + ".";
            return new ModOwnerCleanupParticipantResult(removed, remaining, failures.Count, details);
        }

        internal int CountGameBridgeOwnerResources(string ownerId)
        {
            int featureRoots = cropHarvestingFeature?.Service.CountOwnerResources(ownerId) ?? 0;
            return featureRoots;
        }

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
                runtime.RuntimeMonitor.Log(
                    "GameBridge owner cleanup step failed owner=" + ownerId + " step=" + step + " error=" + ex.GetType().Name + ": " + ex.Message,
                    LogLevel.Warn);
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

