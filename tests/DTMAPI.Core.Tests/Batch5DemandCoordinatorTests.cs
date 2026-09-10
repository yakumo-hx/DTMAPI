using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Runtime;

namespace DTMAPI.UnitTests
{
    internal static class Batch5DemandCoordinatorTests
    {
        internal static void RunAll()
        {
            PassiveCatalogRegistrationDoesNotActivateCapability();
            DemandSourcesAndRouteStateRemainIndependent();
            FirstLastTransitionsAndOwnerCleanupAreDeterministic();
            CapabilityScopedOwnerCleanupDoesNotCrossSubsystemBoundaries();
            ConcurrentFirstLastTransitionsDrainInMutationOrder();
            AuthoritativeDemandIsNeverDroppedAndDiagnosticDetailsStayBounded();
            LongOwnerDiagnosticsAreStableAndSummariesStayBoundedAcrossRoutes();
        }

        private static void PassiveCatalogRegistrationDoesNotActivateCapability()
        {
            var coordinator = new RuntimeDemandCoordinator();
            coordinator.RegisterCapability(new RuntimeCapabilityDescriptor(
                "Camera",
                RuntimeCapabilityOutcome.DemandActivated,
                "EveryFrameWhileLeased",
                "Camera.SetEnvCamera",
                "Provider publication and facade lookup are passive."));

            RuntimeDemandSnapshot snapshot = coordinator.GetSnapshot();
            RuntimeCapabilityDemandSnapshot camera = snapshot.Capabilities.Single(item => item.CapabilityId == "Camera");
            Assert(snapshot.TotalDemand == 0 && snapshot.ActiveUpdaterCount == 0, "Registering a capability descriptor must not activate demand or an updater.");
            Assert(camera.LifecycleState == RuntimeCapabilityLifecycleState.Cold && camera.PatchState == RuntimeCapabilityPatchState.NotInstalled, "Passive registration must preserve cold lifecycle and absent patch state.");
        }

        private static void DemandSourcesAndRouteStateRemainIndependent()
        {
            var coordinator = new RuntimeDemandCoordinator();
            coordinator.RegisterCapability(new RuntimeCapabilityDescriptor(
                "AnimalViewer",
                RuntimeCapabilityOutcome.ProcessPinnedDormant,
                "Every250msWhileSessionOpen",
                "AnimalViewer.SessionDetection",
                "Configured-owner and viewer-session demand are distinct."));

            coordinator.SetDemand("AnimalViewer", "Example.Owner", RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "configured", 1, "configured owner");
            coordinator.SetDemand("AnimalViewer", "Example.Owner", RuntimeDemandSourceType.CapabilitySession, RuntimeDemandLifetime.Session, "viewer-1", 1, "native viewer opened");
            coordinator.SetRouteState(
                "AnimalViewer",
                RuntimeCapabilityLifecycleState.Active,
                RuntimeCapabilityPatchState.ProcessPinned,
                RuntimeCapabilityRestartPolicy.None,
                updaterActive: true,
                reason: "session active");

            RuntimeCapabilityDemandSnapshot state = coordinator.GetSnapshot().Capabilities.Single(item => item.CapabilityId == "AnimalViewer");
            Assert(state.TotalDemand == 2 && state.BySource[RuntimeDemandSourceType.CapabilityRegistration] == 1 && state.BySource[RuntimeDemandSourceType.CapabilitySession] == 1, "Registration and session demand must be counted independently.");
            Assert(state.LifecycleState == RuntimeCapabilityLifecycleState.Active && state.PatchState == RuntimeCapabilityPatchState.ProcessPinned && state.RestartPolicy == RuntimeCapabilityRestartPolicy.None && state.UpdaterActive, "Lifecycle, physical patch, restart policy, and updater state must remain independently reportable.");
        }

        private static void FirstLastTransitionsAndOwnerCleanupAreDeterministic()
        {
            var coordinator = new RuntimeDemandCoordinator();
            int first = 0;
            int last = 0;
            coordinator.DemandTransitioned += transition =>
            {
                if (transition.IsFirstDemand)
                    first++;
                if (transition.IsLastRelease)
                    last++;
            };

            coordinator.SetDemand("FishRoeTooltip", "Owner.A", RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "config", 1, "register");
            coordinator.SetDemand("FishRoeTooltip", "Owner.B", RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "config", 1, "register");
            coordinator.SetDemand("FishRoeTooltip", "Owner.A", RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "config", 0, "release");
            Assert(first == 1 && last == 0 && coordinator.GetDemandCount("FishRoeTooltip") == 1, "Only the aggregate zero-to-one edge should publish first demand.");

            int removed = coordinator.RemoveOwner("Owner.B", "owner disabled");
            Assert(removed == 1 && first == 1 && last == 1 && !coordinator.HasDemand("FishRoeTooltip"), "Owner cleanup must release the final demand and publish one last-release transition.");
        }

        private static void CapabilityScopedOwnerCleanupDoesNotCrossSubsystemBoundaries()
        {
            var coordinator = new RuntimeDemandCoordinator();
            const string owner = "DTMAPI.Tests.Batch5.ScopedOwner";
            const string eventCapability = "Event.GameLoop.UpdateTicked";
            string[] gameBridgeCapabilities = { "Camera", "FishRoeTooltip" };

            coordinator.SetDemand("Camera", owner, RuntimeDemandSourceType.CapabilityLease, RuntimeDemandLifetime.Owner, "lease", 1, "camera lease");
            coordinator.SetDemand("Camera", owner, RuntimeDemandSourceType.CapabilityOperation, RuntimeDemandLifetime.Operation, "restore", 1, "camera restore");
            coordinator.SetDemand(eventCapability, owner, RuntimeDemandSourceType.EventSubscription, RuntimeDemandLifetime.Owner, "listener", 1, "event listener");

            Assert(coordinator.GetOwnerDemandCount(owner) == 3, "The complete owner total must include GameBridge and Event roots.");
            Assert(coordinator.GetOwnerDemandCountForCapabilities(owner, gameBridgeCapabilities) == 2, "A capability-scoped count must include only the selected GameBridge roots.");
            Assert(coordinator.RemoveOwnerDemandsForCapabilities(owner, gameBridgeCapabilities, "GameBridge participant cleanup") == 2, "A capability-scoped cleanup must remove exactly the selected GameBridge roots.");
            Assert(coordinator.GetDemandCount("Camera") == 0 && coordinator.GetDemandCount(eventCapability) == 1, "Scoped cleanup must leave the same owner's Event demand untouched.");
            Assert(coordinator.GetOwnerDemandCountForCapabilities(owner, gameBridgeCapabilities) == 0 && coordinator.GetOwnerDemandCount(owner) == 1, "Scoped remaining counts must reach zero without hiding the Core-owned Event root.");
            Assert(coordinator.RemoveOwner(owner, "Core owner cleanup") == 1 && coordinator.GetOwnerDemandCount(owner) == 0, "Core must be able to remove the preserved Event root independently later.");
        }

        private static void AuthoritativeDemandIsNeverDroppedAndDiagnosticDetailsStayBounded()
        {
            var coordinator = new RuntimeDemandCoordinator(receiptCapacity: 3, diagnosticOwnerCapacity: 2);
            Assert(coordinator.SetDemand("Audio", "Owner.A", RuntimeDemandSourceType.ContentDefinition, RuntimeDemandLifetime.Owner, "a", 1, "a"), "First bounded demand should be accepted.");
            Assert(coordinator.SetDemand("Audio", "Owner.A", RuntimeDemandSourceType.CapabilitySession, RuntimeDemandLifetime.Session, "session", 2, "a-session"), "Multiple roots for one owner must retain an exact owner aggregate.");
            Assert(coordinator.SetDemand("Audio", "Owner.B", RuntimeDemandSourceType.ContentDefinition, RuntimeDemandLifetime.Owner, "b", 1, "b"), "Second bounded demand should be accepted.");
            Assert(coordinator.SetDemand("Audio", "Owner.C", RuntimeDemandSourceType.ContentDefinition, RuntimeDemandLifetime.Owner, "c", 1, "c"), "A live capability root must never be rejected merely to bound diagnostics.");
            coordinator.SetRouteState("Audio", RuntimeCapabilityLifecycleState.Active, RuntimeCapabilityPatchState.Installed, RuntimeCapabilityRestartPolicy.None, updaterActive: false, reason: "ready entries need no polling");

            RuntimeDemandSnapshot snapshot = coordinator.GetSnapshot();
            RuntimeCapabilityDemandSnapshot audio = snapshot.Capabilities.Single(item => item.CapabilityId == "Audio");
            Assert(snapshot.DemandEntryCount == 4 && snapshot.TotalDemand == 5, "The authoritative demand map must retain every live root and its count.");
            Assert(audio.ByOwner["Owner.A"] == 3, "The bounded projection must aggregate multiple source/lifetime roots for a named owner without sorting the full demand map.");
            Assert(audio.ByOwner.Count == 2 && audio.TrimmedOwnerCount == 1 && audio.TrimmedOwnerDemand == 1, "Named owner diagnostics should stay bounded while reporting the omitted aggregate.");
            Assert(snapshot.Receipts.Count == 3 && snapshot.Receipts.Count <= snapshot.ReceiptCapacity && snapshot.TrimmedReceipts >= 1, "Recent demand receipts must be trimmed to the configured capacity.");
            string runtimeSummary = snapshot.FormatSummary();
            Assert(runtimeSummary.Contains("ownerSample={Owner.A=3", StringComparison.Ordinal), "Runtime Diagnostics must identify a bounded sample of who demands each capability.");
            Assert(runtimeSummary.Contains("receiptSample={seq=", StringComparison.Ordinal), "Runtime Diagnostics must expose bounded recent demand receipts, not only internal snapshot objects.");

            Assert(coordinator.RemoveCapabilityDemands("Audio", "shutdown") == 5, "Capability shutdown must clear every authoritative root, including owners omitted from diagnostics.");
            Assert(coordinator.DemandEntryCount == 0 && !coordinator.HasDemand("Audio"), "Capability shutdown must leave no live demand entry.");
        }

        private static void ConcurrentFirstLastTransitionsDrainInMutationOrder()
        {
            var coordinator = new RuntimeDemandCoordinator();
            using var firstCallbackEntered = new ManualResetEvent(false);
            using var releaseFirstCallback = new ManualResetEvent(false);
            var observed = new List<int>();
            int synchronouslyObservedCount = -1;
            coordinator.DemandStateChangedSynchronously += capabilityId =>
            {
                if (capabilityId.Equals("Concurrent", StringComparison.OrdinalIgnoreCase))
                    Volatile.Write(ref synchronouslyObservedCount, coordinator.GetDemandCount(capabilityId));
            };
            coordinator.DemandTransitioned += transition =>
            {
                if (observed.Count == 0)
                {
                    firstCallbackEntered.Set();
                    if (!releaseFirstCallback.WaitOne(TimeSpan.FromSeconds(5)))
                        throw new TimeoutException("Demand transition test did not release the first callback.");
                }
                observed.Add(transition.CurrentCount);
            };

            Exception? workerFailure = null;
            var worker = new Thread(() =>
            {
                try
                {
                    coordinator.SetDemand("Concurrent", "Owner.A", RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "root", 1, "acquire");
                }
                catch (Exception ex)
                {
                    workerFailure = ex;
                }
            });
            worker.Start();
            Assert(firstCallbackEntered.WaitOne(TimeSpan.FromSeconds(5)), "First-demand callback should enter the serialized transition drain.");
            coordinator.SetDemand("Concurrent", "Owner.A", RuntimeDemandSourceType.CapabilityRegistration, RuntimeDemandLifetime.Owner, "root", 0, "release");
            Assert(Volatile.Read(ref synchronouslyObservedCount) == 0 && observed.Count == 0,
                "The internal scalar observer must publish last-owner zero before Release returns even while ordered public transition publication is blocked.");
            releaseFirstCallback.Set();
            worker.Join();
            if (workerFailure != null)
                throw new InvalidOperationException("Concurrent demand worker failed.", workerFailure);

            Assert(observed.SequenceEqual(new[] { 1, 0 }), "Demand transitions must preserve authoritative mutation order even when the first subscriber call is delayed.");
            Assert(!coordinator.HasDemand("Concurrent"), "A delayed first-demand callback must not resurrect a released authoritative root.");
        }

        private static void LongOwnerDiagnosticsAreStableAndSummariesStayBoundedAcrossRoutes()
        {
            const int routeCount = 256;
            string ownerId = "DTMAPI.Tests." + new string('X', 8_000);
            string expectedDisplayOwner = BoundedDiagnosticScalar.StableIdentity(ownerId);
            var coordinator = new RuntimeDemandCoordinator();

            for (int index = 0; index < routeCount; index++)
            {
                string capabilityId = "LongOwner.Route." + index.ToString("000");
                Assert(
                    coordinator.SetDemand(capabilityId, ownerId, RuntimeDemandSourceType.ContentDefinition, RuntimeDemandLifetime.Owner, "definition", 1, "8k owner multi-route diagnostic bound"),
                    "Every long-owner route should retain its authoritative demand.");
            }

            Assert(coordinator.GetOwnerDemandCount(ownerId) == routeCount, "Display bounding must not change authoritative owner lookup or demand totals.");
            RuntimeDemandSnapshot snapshot = coordinator.GetSnapshot();
            Assert(snapshot.Capabilities.Count == routeCount && snapshot.TotalDemand == routeCount, "Every long-owner route must remain present in the authoritative snapshot totals.");
            Assert(
                snapshot.Capabilities.All(capability => capability.ByOwner.Count == 1 && capability.ByOwner.Single().Key == expectedDisplayOwner && capability.ByOwner.Single().Value == 1),
                "Every diagnostic owner projection must use the same stable bounded hash for the same 8k authoritative owner.");
            Assert(snapshot.Receipts.All(receipt => receipt.OwnerId == expectedDisplayOwner && receipt.OwnerId.Length <= BoundedDiagnosticScalar.OwnerIdChars), "Demand receipts must use the stable bounded display owner instead of prefix truncation.");
            Assert(
                snapshot.Capabilities.All(capability => capability.FormatSummary().Length <= RuntimeCapabilityDemandSnapshot.MaxFormatSummaryChars && !capability.FormatSummary().Contains(ownerId, StringComparison.Ordinal)),
                "Every capability summary must have a fixed total bound and must not embed the raw 8k owner.");

            string runtimeSummary = snapshot.FormatSummary();
            Assert(runtimeSummary.Length == RuntimeDemandSnapshot.MaxFormatSummaryChars && runtimeSummary.EndsWith("]", StringComparison.Ordinal), "The multi-route runtime summary must hit its fixed total bound while retaining the stable hash suffix.");
            Assert(!runtimeSummary.Contains(ownerId, StringComparison.Ordinal) && runtimeSummary.Contains("routeSampleOmitted=128", StringComparison.Ordinal), "The bounded runtime summary must omit unsampled routes explicitly and never embed the raw 8k owner.");

            Assert(coordinator.RemoveOwner(ownerId, "long owner cleanup") == routeCount, "Authoritative cleanup must still match the complete long owner after diagnostic hashing.");
            Assert(coordinator.DemandEntryCount == 0 && coordinator.GetOwnerDemandCount(ownerId) == 0, "Long-owner cleanup must clear all authoritative routes.");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("Batch5 demand coordinator test failed: " + message);
        }
    }
}
