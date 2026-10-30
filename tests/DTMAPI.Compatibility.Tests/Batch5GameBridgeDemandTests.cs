#pragma warning disable CS0618 // These tests intentionally exercise frozen compatibility facades.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;
using DTMAPI.Testing;

namespace DTMAPI.UnitTests
{
    internal static class Batch5GameBridgeDemandTests
    {
        private static int fixtureSequence;

        internal static void RunAll()
        {
            CatalogIsFixedUniqueAndClassified();
            CoreLifecycleRouteTracksItsPhysicalHookClosure();
            IdleSchedulerFastPathAllocatesNothing();
            WarmTenThousandFramesRemainOptionallySilent();
            StructuredNoDemandBoundaryTracksCoreAndMandatoryCadence();
            RuntimeAutomationPumpSuppressesSynchronousUpdaterReentry();
            OptionalHookRetryLivesUntilSuccessOrRelease();
            NamedAuthorReloadPublishesRestartRequiredState();
            ShutdownAuthoritativelyClearsEveryGameBridgeRoute();
        }

        private static void CatalogIsFixedUniqueAndClassified()
        {
            string[] mainFeatures =
            {
                GameBridgeDemandRoutes.NativeUiLayoutDiagnostics,
                GameBridgeDemandRoutes.CropHarvesting,
            };

            RuntimeCapabilityDescriptor[] catalog = GameBridgeDemandRoutes.Catalog.ToArray();
            Assert(catalog.Select(item => item.CapabilityId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == catalog.Length, "Capability ids must be unique.");
            Assert(mainFeatures.All(id => catalog.Any(item => item.CapabilityId.Equals(id, StringComparison.OrdinalIgnoreCase))), "The fixed catalog must contain every retained Runtime feature contract.");
            Assert(!catalog.Any(item => item.CapabilityId.Contains("Equipment", StringComparison.OrdinalIgnoreCase)), "Frozen EquipmentSlots compatibility must not retain a mandatory or periodic demand route.");
            Assert(catalog.Any(item => item.CapabilityId == GameBridgeDemandRoutes.NativeUiLayoutDiagnostics && item.Outcome == RuntimeCapabilityOutcome.Mandatory), "Native title layout repair must stay mandatory.");
            RuntimeCapabilityDescriptor[] restart = catalog.Where(item => item.Outcome == RuntimeCapabilityOutcome.RestartRequired).ToArray();
            Assert(restart.Length == 1 && restart[0].CapabilityId == GameBridgeDemandRoutes.AuthorSessionReload, "Only the named AuthorSession same-process replacement route is RestartRequired; optional installed hooks are ProcessPinnedDormant.");
        }

        private static void IdleSchedulerFastPathAllocatesNothing()
        {
            var scheduler = new HookInstallScheduler();
            for (int i = 0; i < 16; i++)
            {
                Assert(!scheduler.HasPending, "A new scheduler must stay empty.");
                Assert(!scheduler.TryConsumePending(out HookInstallRequest[] warm) && warm.Length == 0, "Empty TryConsume must return the shared empty array.");
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++)
            {
                if (scheduler.HasPending)
                    throw new InvalidOperationException("Idle HookInstallScheduler unexpectedly reported pending work.");
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert(allocated == 0, "10,000 idle HasPending checks must allocate zero bytes on the current thread; actual=" + allocated + ".");
        }

        private static void CoreLifecycleRouteTracksItsPhysicalHookClosure()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            bool shutdown = false;
            bool installed = false;
            try
            {
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.CoreLifecycle, () => installed);
                bridge.CommitPendingDemandRoutesForTests();
                RuntimeCapabilityDemandSnapshot activating = GetRoute(runtime, GameBridgeDemandRoutes.CoreLifecycle);
                Assert(activating.LifecycleState == RuntimeCapabilityLifecycleState.Activating && activating.PatchState == RuntimeCapabilityPatchState.NotInstalled, "CoreLifecycle must not claim Active before its exact base-hook closure is ready.");

                installed = true;
                bridge.RefreshDemandRoutePatchStatesForTests("unit core closure ready");
                RuntimeCapabilityDemandSnapshot active = GetRoute(runtime, GameBridgeDemandRoutes.CoreLifecycle);
                Assert(active.LifecycleState == RuntimeCapabilityLifecycleState.Active && active.PatchState == RuntimeCapabilityPatchState.Installed, "CoreLifecycle must become Active/Installed only when the physical closure probe is true.");

                bridge.Shutdown("Batch5 core lifecycle route test");
                shutdown = true;
                RuntimeCapabilityDemandSnapshot stopped = GetRoute(runtime, GameBridgeDemandRoutes.CoreLifecycle);
                Assert(stopped.LifecycleState == RuntimeCapabilityLifecycleState.ProcessPinnedDormant && stopped.PatchState == RuntimeCapabilityPatchState.ProcessPinned, "A retained base Harmony closure must remain authoritatively ProcessPinnedDormant at shutdown.");
            }
            finally
            {
                if (!shutdown)
                    bridge.Shutdown("Batch5 core lifecycle route test cleanup");
            }
        }

        private static void WarmTenThousandFramesRemainOptionallySilent()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            try
            {
                bridge.SetCoreHookReadinessOverrideForTests(true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.NativeUiLayoutDiagnostics, () => true);
                bridge.WarmAndRunNoOptionalDemandFramesForTests(10000);
                Assert(bridge.OptionalDemandUpdaterDispatchCountForTests == 0, "A warmed 10,000-frame no-consumer run must dispatch zero optional updaters.");
                Assert(bridge.OptionalHookInstallRequestCountForTests == 0, "A warmed 10,000-frame no-consumer run must request zero optional hook installs.");
                Assert(bridge.HookSchedulerIdleFrameFastPathCountForTests == 10000, "Every warmed frame must take the idle scheduler fast path.");
                Assert(bridge.HookInstallProcessedBatchCountForTests == 0, "No idle frame may consume a hook-install batch.");
                Assert(bridge.HookReadinessPublishCountForTests == 0, "No idle frame may rebuild or publish hook-readiness status.");
                Assert(bridge.RoutePatchStateRefreshPassCountForTests == 0, "No idle frame may rescan route patch state.");

                RuntimeDemandSnapshot snapshot = runtime.DemandCoordinator.GetSnapshot();
                Assert(snapshot.Capabilities.Where(item => item.TotalDemand > 0).All(item => item.Descriptor?.Outcome == RuntimeCapabilityOutcome.Mandatory), "No optional route may retain demand after warmup.");
                string[] activeUpdaterIds = bridge.GetActiveDemandUpdaterIdsForTests();
                Assert(activeUpdaterIds.All(id => GameBridgeDemandRoutes.Catalog.Single(item => item.CapabilityId == id).Outcome == RuntimeCapabilityOutcome.Mandatory), "The dense updater snapshot may contain only mandatory routes without consumers.");
                Assert(!activeUpdaterIds.Any(id => id.Contains("EquipmentSlots", StringComparison.OrdinalIgnoreCase)), "Dormant EquipmentSlots compatibility must not retain an updater without a loaded Host.");
            }
            finally
            {
                bridge.Shutdown("Batch5 no-demand test");
            }
        }

        private static void StructuredNoDemandBoundaryTracksCoreAndMandatoryCadence()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            try
            {
                bridge.SetCoreHookReadinessOverrideForTests(true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.NativeUiLayoutDiagnostics, () => true);
                bridge.SetQaHostFrameUpdateForTests(() => { });
                GameBridgeDemandRoutes.SetOwnerDemand(
                    runtime,
                    GameBridgeDemandRoutes.QaHost,
                    GameBridgeDemandRoutes.QaOwner,
                    RuntimeDemandSourceType.ExplicitQa,
                    RuntimeDemandLifetime.Session,
                    "no-demand-boundary-unit",
                    true,
                    "unit full Update-path observer");
                bridge.CommitPendingDemandRoutesForTests();

                const string unexpectedEventOwner = "DTMAPI.Tests.Batch5.NoDemand.EventOwner";
                const string unexpectedEventCapability = "Event.GameLoop.UpdateTicked";
                IEventsHelper unexpectedEvents = runtime.Events.CreateProxy(unexpectedEventOwner);
                EventHandler<UpdateTickedEventArgs> unexpectedHandler = (_, _) => { };
                unexpectedEvents.GameLoop.UpdateTicked += unexpectedHandler;
                Batch5NoDemandRuntimeSnapshot pollutedDemand = bridge.CaptureBatch5NoDemandRuntimeSnapshot();
                Assert(pollutedDemand.ActiveOptionalDemandIds.Contains(unexpectedEventCapability, StringComparer.OrdinalIgnoreCase), "The whole-runtime receipt must expose optional Core Event demand even when it has no GameBridge route.");
                unexpectedEvents.GameLoop.UpdateTicked -= unexpectedHandler;
                Assert(!bridge.CaptureBatch5NoDemandRuntimeSnapshot().ActiveOptionalDemandIds.Contains(unexpectedEventCapability, StringComparer.OrdinalIgnoreCase), "Removing the final Core Event listener must clear it before the no-demand baseline.");

                bridge.Update();
                runtime.Update();
                long coreUiAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                for (int frame = 0; frame < 10000; frame++)
                    bridge.InvokeDemandUpdaterForTests(GameBridgeDemandRoutes.CoreUiContext);
                long coreUiAllocated = GC.GetAllocatedBytesForCurrentThread() - coreUiAllocatedBefore;
                long contentAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                for (int frame = 0; frame < 10000; frame++)
                    bridge.InvokeDemandUpdaterForTests(GameBridgeDemandRoutes.ContentRefreshDrain);
                long contentAllocated = GC.GetAllocatedBytesForCurrentThread() - contentAllocatedBefore;
                long qaAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                for (int frame = 0; frame < 10000; frame++)
                    bridge.InvokeDemandUpdaterForTests(GameBridgeDemandRoutes.QaHost);
                long qaAllocated = GC.GetAllocatedBytesForCurrentThread() - qaAllocatedBefore;
                Assert(coreUiAllocated == 0 && contentAllocated == 0 && qaAllocated == 0,
                    "Every warmed mandatory/observer updater must allocate zero current-thread bytes across 10,000 direct invocations; coreUi=" + coreUiAllocated + "; content=" + contentAllocated + "; qa=" + qaAllocated + ".");
                long bridgeAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                for (int frame = 0; frame < 10000; frame++)
                    bridge.Update();
                long bridgeAllocated = GC.GetAllocatedBytesForCurrentThread() - bridgeAllocatedBefore;
                long runtimeAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                for (int frame = 0; frame < 10000; frame++)
                    runtime.Update();
                long runtimeAllocated = GC.GetAllocatedBytesForCurrentThread() - runtimeAllocatedBefore;
                Assert(bridgeAllocated == 0, "The warmed GameBridge no-demand Update path must allocate zero current-thread bytes across 10,000 frames; actual=" + bridgeAllocated + ".");
                Assert(runtimeAllocated == 0, "The warmed Core no-demand Update path must allocate zero current-thread bytes across 10,000 frames; actual=" + runtimeAllocated + ".");
                Batch5NoDemandRuntimeSnapshot start = bridge.CaptureBatch5NoDemandRuntimeSnapshot();
                const int measuredFrames = 10000;
                long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                for (int frame = 0; frame < measuredFrames; frame++)
                {
                    bridge.Update();
                    runtime.Update();
                }
                long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
                Batch5NoDemandRuntimeSnapshot end = bridge.CaptureBatch5NoDemandRuntimeSnapshot();

                Assert(allocated == 0, "The combined warmed GameBridge plus Core Update path must allocate zero current-thread bytes across 10,000 no-demand frames; actual=" + allocated + ".");
                Assert(end.RuntimeUpdateTicks - start.RuntimeUpdateTicks == measuredFrames, "The full no-demand boundary must observe exactly one Core DtmApiRuntime.Update per measured frame.");
                Assert(end.QaObserverUpdaterInvocations - start.QaObserverUpdaterInvocations == measuredFrames, "The explicit QA observer route must dispatch exactly once per measured frame.");
                Assert(start.QaExplicitDemandActive && end.QaExplicitDemandActive && start.QaUpdaterActive && end.QaUpdaterActive, "The observer must remain explicitly QA-demanded without becoming an optional product route.");
                Assert(start.ActiveOptionalDemandIds.Length == 0 && end.ActiveOptionalDemandIds.Length == 0 && start.ActiveOptionalUpdaterIds.Length == 0 && end.ActiveOptionalUpdaterIds.Length == 0, "No optional product demand/updater may enter the structured boundary.");
                Assert(end.ActiveUpdaterMembershipSnapshotRebuilds == start.ActiveUpdaterMembershipSnapshotRebuilds, "Stable demand must not rebuild the active updater membership snapshot.");
                Assert(end.OptionalPerFeatureProjectionBuilds == start.OptionalPerFeatureProjectionBuilds, "No-demand frames must build zero CustomAnimals/Audio feature projections.");
                Assert(end.ContentQueryCandidateBuilds == start.ContentQueryCandidateBuilds, "No-demand frames must build zero ContentQuery candidates.");
                Assert(end.OptionalFeatureFileStatusCalls == start.OptionalFeatureFileStatusCalls && end.OptionalDirectoryEnumerations == start.OptionalDirectoryEnumerations, "No-demand frames must perform zero optional file-status or directory-enumeration work.");
                Assert(end.OptionalReflectionObjectSearches == start.OptionalReflectionObjectSearches && end.OptionalNativeUpdaterInvocations == start.OptionalNativeUpdaterInvocations, "No-demand frames must perform zero optional reflection/object search or native updater work.");
                Assert(end.OptionalRetainedCallbackWork == start.OptionalRetainedCallbackWork, "Retained optional callback work remains zero without demand.");
                Assert(!start.EventQueuePending && !end.EventQueuePending && !start.HookStatusQueuePending && !end.HookStatusQueuePending, "Core event and Hook-status queues must be empty at both structured boundaries.");

                Batch5NoDemandMandatoryUpdaterSnapshot coreUiStart = start.MandatoryUpdaterDispatches.Single(item => item.CapabilityId == GameBridgeDemandRoutes.CoreUiContext);
                Batch5NoDemandMandatoryUpdaterSnapshot coreUiEnd = end.MandatoryUpdaterDispatches.Single(item => item.CapabilityId == GameBridgeDemandRoutes.CoreUiContext);
                Batch5NoDemandMandatoryUpdaterSnapshot contentStart = start.MandatoryUpdaterDispatches.Single(item => item.CapabilityId == GameBridgeDemandRoutes.ContentRefreshDrain);
                Batch5NoDemandMandatoryUpdaterSnapshot contentEnd = end.MandatoryUpdaterDispatches.Single(item => item.CapabilityId == GameBridgeDemandRoutes.ContentRefreshDrain);
                Batch5NoDemandMandatoryUpdaterSnapshot nativeLayoutStart = start.MandatoryUpdaterDispatches.Single(item => item.CapabilityId == GameBridgeDemandRoutes.NativeUiLayoutDiagnostics);
                Batch5NoDemandMandatoryUpdaterSnapshot nativeLayoutEnd = end.MandatoryUpdaterDispatches.Single(item => item.CapabilityId == GameBridgeDemandRoutes.NativeUiLayoutDiagnostics);
                Assert(coreUiStart.Active && coreUiEnd.Active && coreUiEnd.DispatchCount - coreUiStart.DispatchCount == measuredFrames, "Core UI context must report an exact per-frame mandatory cadence.");
                Assert(contentStart.Active && contentEnd.Active && contentEnd.DispatchCount - contentStart.DispatchCount == measuredFrames, "Content generation drain must report an exact per-frame mandatory cadence.");
                Assert(nativeLayoutStart.Active && nativeLayoutEnd.Active && nativeLayoutEnd.DispatchCount >= nativeLayoutStart.DispatchCount, "Native UI layout repair must expose its independent bounded mandatory cadence.");
            }
            finally
            {
                bridge.SetQaHostFrameUpdateForTests(null);
                bridge.Shutdown("Batch5 structured no-demand boundary test");
            }
        }

        private static void RuntimeAutomationPumpSuppressesSynchronousUpdaterReentry()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            int updaterInvocations = 0;
            try
            {
                bridge.SetQaHostFrameUpdateForTests(() =>
                {
                    updaterInvocations++;
                    bridge.UpdateRuntimeAutomation();
                });
                GameBridgeDemandRoutes.SetOwnerDemand(
                    runtime,
                    GameBridgeDemandRoutes.QaHost,
                    GameBridgeDemandRoutes.QaOwner,
                    RuntimeDemandSourceType.ExplicitQa,
                    RuntimeDemandLifetime.Session,
                    "reentry-unit",
                    true,
                    "unit synchronous updater re-entry");
                bridge.CommitPendingDemandRoutesForTests();

                bridge.UpdateRuntimeAutomation();
                Assert(updaterInvocations == 1 && bridge.RuntimeAutomationReentryBypassCountForTests == 1,
                    "A demand updater that synchronously pumps the bridge must execute once while its nested pump is bypassed once.");

                bridge.UpdateRuntimeAutomation();
                Assert(updaterInvocations == 2 && bridge.RuntimeAutomationReentryBypassCountForTests == 2,
                    "The pump guard must release in finally so the next real frame runs once and suppresses only its own nested call.");
                Assert(bridge.FormatGameBridgeDemandRoutingSummaryForTests().Contains("reentryBypasses=2", StringComparison.Ordinal),
                    "Demand diagnostics must expose the bounded re-entry bypass count without logging on the hot path.");
            }
            finally
            {
                bridge.SetQaHostFrameUpdateForTests(null);
                bridge.Shutdown("Batch5 runtime-automation re-entry test");
            }
        }

        private static void OptionalHookRetryLivesUntilSuccessOrRelease()
        {
            foreach (bool scheduled in new[] { true, false })
                OptionalHookRetryLivesUntilSuccessOrRelease(scheduled);
        }

        private static void OptionalHookRetryLivesUntilSuccessOrRelease(bool scheduled)
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            runtime.RefactorOptions.HookInstallSchedulerValue = scheduled;
            double now = 10;
            var scheduler = new HookInstallScheduler(() => now);
            typeof(DolocTownGameBridge).GetField("hookInstallScheduler", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(bridge, scheduler);
            IManifest owner = Manifest("DTMAPI.Tests.Batch5.Retry.Success");
            bool installed = false;
            try
            {
                bridge.SetCoreHookReadinessOverrideForTests(true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.NativeUiLayoutDiagnostics, () => true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset, () => installed);
                GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset, owner.UniqueID, RuntimeDemandSourceType.CapabilityLease, RuntimeDemandLifetime.Owner, "retry", true, "unit demand");
                bridge.CommitPendingDemandRoutesForTests();
                Assert(bridge.HookRetrySourcesAliveForTests, "A failed demanded optional hook must keep the bounded retry sources alive even when Core is ready.");
                Assert(scheduler.HasPending == scheduled, "The option must preserve queued versus immediate main-thread installation.");

                bridge.UpdateRuntimeAutomation();
                RuntimeCapabilityDemandSnapshot activating = GetRoute(runtime, GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset);
                Assert(activating.LifecycleState == RuntimeCapabilityLifecycleState.Activating, "The failed optional route must remain Activating.");

                long Processed() => scheduler.GetSnapshot(false, "", "", "", "", "", false, false, false).TotalProcessed;
                long before = Processed();
                now = 12;
                bridge.UpdateRuntimeAutomation();
                Assert(Processed() == before + 1 && bridge.HookRetrySourcesAliveForTests,
                    "Both configuration values must process a due retry and retain it while the target is unavailable.");
                installed = true;
                now = 14;
                bridge.UpdateRuntimeAutomation();
                RuntimeCapabilityDemandSnapshot active = GetRoute(runtime, GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset);
                Assert(active.LifecycleState == RuntimeCapabilityLifecycleState.Active && active.PatchState == RuntimeCapabilityPatchState.Installed, "The next demanded retry must publish Active/Installed when the target becomes available.");
                Assert(!bridge.HookRetrySourcesAliveForTests, "Retry sources must stop after Core and every demanded optional hook are ready.");

                runtime.RefactorOptions.HookInstallSchedulerValue = !scheduled;
                before = Processed();
                var worker = new Thread(() => bridge.RequestHookInstallForTests("AssemblyLoad:worker"));
                worker.Start();
                worker.Join();
                Assert(scheduler.HasPending && Processed() == before,
                    "Off-thread requests must wait for the runtime frame even after toggling direct installation.");
                bridge.UpdateRuntimeAutomation();
                Assert(!scheduler.HasPending && Processed() == before + 1, "The runtime frame must consume the worker request exactly once.");

                bridge.Shutdown("retry configuration close");
                bridge.RequestHookInstallForTests("DemandActivated:after-shutdown");
                now = 30;
                bridge.UpdateRuntimeAutomation();
                Assert(!scheduler.HasPending && !bridge.HookRetrySourcesAliveForTests && Processed() == before + 1,
                    "Shutdown must reject late requests, retry activation and frame work for either configuration.");
            }
            finally
            {
                bridge.Shutdown("Batch5 optional retry success test");
            }

            CreateFixture(out runtime, out bridge);
            owner = Manifest("DTMAPI.Tests.Batch5.Retry.Release");
            runtime.RefactorOptions.HookInstallSchedulerValue = scheduled;
            try
            {
                bridge.SetCoreHookReadinessOverrideForTests(true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.NativeUiLayoutDiagnostics, () => true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset, () => false);
                GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset, owner.UniqueID, RuntimeDemandSourceType.CapabilityLease, RuntimeDemandLifetime.Owner, "retry", true, "unit demand");
                bridge.CommitPendingDemandRoutesForTests();
                Assert(bridge.HookRetrySourcesAliveForTests, "An unavailable demanded route must start retry sources.");
                bridge.UpdateRuntimeAutomation();

                bridge.RemoveGameBridgeOwnerResourcesForTests(owner.UniqueID, ModOwnerCleanupReason.Unload);
                bridge.CommitPendingDemandRoutesForTests();
                Assert(!bridge.HookRetrySourcesAliveForTests, "Releasing the final unavailable consumer must stop retry sources without waiting for the target.");
            }
            finally
            {
                bridge.Shutdown("Batch5 optional retry release test");
            }
        }

        private static void NamedAuthorReloadPublishesRestartRequiredState()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            const string ownerId = "DTMAPI.Tests.Batch5.AuthorReload";
            try
            {
                bridge.RecordAuthorSessionRestartRequiredForTests(ownerId, "codemod-restart-required");
                bridge.CommitPendingDemandRoutesForTests();
                RuntimeCapabilityDemandSnapshot route = GetRoute(runtime, GameBridgeDemandRoutes.AuthorSessionReload);
                Assert(route.TotalDemand == 1 && route.LifecycleState == RuntimeCapabilityLifecycleState.RestartRequired && route.PatchState == RuntimeCapabilityPatchState.NotInstalled && route.Descriptor?.Outcome == RuntimeCapabilityOutcome.RestartRequired, "Named code/format replacement must publish an actual RestartRequired route state.");
            }
            finally
            {
                bridge.Shutdown("Batch5 named author restart route test");
            }
        }

        private static void ShutdownAuthoritativelyClearsEveryGameBridgeRoute()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            IManifest first = Manifest("DTMAPI.Tests.Batch5.Shutdown.A");
            IManifest second = Manifest("DTMAPI.Tests.Batch5.Shutdown.B");
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset, first.UniqueID, RuntimeDemandSourceType.CapabilityLease, RuntimeDemandLifetime.Owner, "retry", true, "unit demand");
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.CropHarvesting, second.UniqueID, RuntimeDemandSourceType.CapabilityLease, RuntimeDemandLifetime.Owner, "shutdown", true, "unit demand");
            Assert(runtime.DemandCoordinator.GetOwnerDemandCount(first.UniqueID) > 0 && runtime.DemandCoordinator.GetOwnerDemandCount(second.UniqueID) > 0, "Shutdown fixture must begin with ordinary owner demand.");

            bridge.Shutdown("Batch5 authoritative route release");
            RuntimeDemandSnapshot snapshot = runtime.DemandCoordinator.GetSnapshot();
            Assert(snapshot.Capabilities.Where(item => GameBridgeDemandRoutes.Catalog.Any(route => route.CapabilityId.Equals(item.CapabilityId, StringComparison.OrdinalIgnoreCase))).All(item => item.TotalDemand == 0 && !item.UpdaterActive), "Shutdown must clear every GameBridge capability directly, independent of bounded diagnostic owner projections.");

            CreateFixture(out runtime, out bridge);
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset, first.UniqueID, RuntimeDemandSourceType.CapabilityLease, RuntimeDemandLifetime.Owner, "retry", true, "unit demand");
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.CropHarvesting, second.UniqueID, RuntimeDemandSourceType.CapabilityLease, RuntimeDemandLifetime.Owner, "shutdown", true, "unit demand");
            bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset, () => throw new InvalidOperationException("shutdown patch probe failed"));
            bridge.Shutdown("failure isolation");
            snapshot = runtime.DemandCoordinator.GetSnapshot();
            Assert(snapshot.Capabilities.All(item => item.TotalDemand == 0 && !item.UpdaterActive), "One failing physical-patch probe must not retain other demand roots or frame updaters.");
            Assert(GetRoute(runtime, GameBridgeDemandRoutes.ItemDisplayNameEnvironmentReset).PatchState == RuntimeCapabilityPatchState.Failed &&
                runtime.Diagnostics.GetHookStatuses().Any(item => item.HookId == "GameBridge.ShutdownCleanup" && item.Status == "failed"),
                "Cleanup must preserve a failed probe as failed evidence instead of claiming physical removal.");
            bridge.RequestHookInstallForTests("AssemblyLoad:after-shutdown");
            bridge.UpdateRuntimeAutomation();
            Assert(!bridge.HookRetrySourcesAliveForTests, "Post-shutdown requests cannot recreate Hook retry sources.");
        }

        private static RuntimeCapabilityDemandSnapshot GetRoute(DtmApiRuntime runtime, string capabilityId) =>
            runtime.DemandCoordinator.GetSnapshot().Capabilities.Single(item => item.CapabilityId.Equals(capabilityId, StringComparison.OrdinalIgnoreCase));

        private static int CountTextOccurrences(string text, string value)
        {
            int count = 0;
            int offset = 0;
            while (!string.IsNullOrEmpty(value) && (offset = (text ?? string.Empty).IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
            {
                count++;
                offset += value.Length;
            }
            return count;
        }

        private static void CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge)
        {
            string gamePath = Path.Combine(DtmApiTestSession.Current.RootPath, "batch5-gamebridge-" + (++fixtureSequence));
            Directory.CreateDirectory(Path.Combine(gamePath, "BepInEx", "plugins"));
            runtime = new DtmApiRuntime(new FakeHost(gamePath), new ConfigMenuRegistry());
            bridge = new DolocTownGameBridge(runtime);
        }

        private static IManifest Manifest(string id) => new ManifestModel
        {
            Name = id,
            Author = "DTMAPI",
            Version = "1.0.0",
            UniqueID = id,
            Type = "CodeMod"
        };

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("Batch5 GameBridge demand test failed: " + message);
        }

        private sealed class FakeHost : IRuntimeHost, ILegacyDevelopmentModSourceTestHost
        {
            public FakeHost(string gamePath)
            {
                GamePath = gamePath;
                PluginPath = Path.Combine(gamePath, "BepInEx", "plugins");
            }

            public string GamePath { get; }
            public string PluginPath { get; }
            public string HostName => "Batch5GameBridgeUnitTest";
            public bool IncludeLegacyDevelopmentModSourceForTests => true;
            public List<string> Logs { get; } = new List<string>();
            public void Log(string message) => Logs.Add(message ?? string.Empty);
            public void LogWarning(string message) => Logs.Add(message ?? string.Empty);
            public void LogError(string message, Exception? exception = null) => Logs.Add((message ?? string.Empty) + (exception == null ? string.Empty : ": " + exception.Message));
        }
    }
}
