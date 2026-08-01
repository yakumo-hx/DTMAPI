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
using DTMAPI.DebugConsole;
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
            CompatibilityHostRejectsNonCanonicalReceiptPath();
            CompatibilityHostRejectsWrongIdentityBeforeLoad();
            CompatibilityHostRejectsWrongVersionBeforeLoad();
            CompatibilityHostRejectsWrongFrameworkBeforeLoad();
            DebugConsoleCompatibilityProxyRemainsDormantUntilOldAbiCall();
            FrozenCameraHostFailsClosedInBothOrdersBeforeNativeWrite();
            FrozenSaveSlotsOwnerFailsClosedInBothOrdersAndRetriesRestore();
            CoreLifecycleRouteTracksItsPhysicalHookClosure();
            IdleSchedulerFastPathAllocatesNothing();
            WarmTenThousandFramesRemainOptionallySilent();
            StructuredNoDemandBoundaryTracksCoreAndMandatoryCadence();
            RuntimeAutomationPumpSuppressesSynchronousUpdaterReentry();
            RetainedNativeCallbacksSleepAtZeroDemand();
            GenericArrayPostfixSleepsBeforeValueTypeBoxing();
            SharedRetainedCallbackClosuresAreProductAndOwnerScoped();
            ConcurrentLastReleaseClearsRetainedMaskBeforeReturning();
            AutoFillOnlyUsesItsUpdaterWithoutNativeStageHooks();
            ActionSpeedNativeStageGroupsUseExactExitClosures();
            InteractionOnlyActionCompletionUsesOnlyInteractExit();
            ManagedOneActionOwnerFailsClosedInBothLoadOrders();
            ManagedActionSpeedOwnerFailsClosedInBothLoadOrders();
            ManagedFishBreedingOwnerFailsClosedInBothLoadOrders();
            ManagedAnimalHusbandryOwnerFailsClosedInBothLoadOrders();
            OneHookConsumerActivatesAndOwnerCleanupReleasesOnlyItsRoute();
            OwnerCleanupScopesDemandAndCountsIndependentRootsAuthoritatively();
            OptionalHookRetryLivesUntilSuccessOrRelease();
            DynamicAudioRegistrationPublishesAndReleasesDemand();
            AudioDefinitionClosureDistinguishesSimpleAndAnimalVoice();
            AudioContentDemandsAreOwnerAttributedAndRetainLastGoodGeneration();
            CustomAnimalPostCommitFaultReconcilesAddedDefinitionDemand();
            CustomAnimalPostCommitFaultReconcilesRemovedDefinitionDemand();
            CustomAnimalInvalidRejectionWaitsForStaleAuthority();
            CustomAnimalMissingSchemaRejectionWaitsForPreCommitAuthority();
            NamedAuthorReloadPublishesRestartRequiredState();
            ShutdownAuthoritativelyClearsEveryGameBridgeRoute();
        }

        private static void CatalogIsFixedUniqueAndClassified()
        {
            string[] mainFeatures =
            {
                GameBridgeDemandRoutes.Camera,
                GameBridgeDemandRoutes.FishingCompatibility,
                GameBridgeDemandRoutes.FishRoeTooltip,
                GameBridgeDemandRoutes.ChestLocatorEnhancer,
                GameBridgeDemandRoutes.SaveSlots,
                GameBridgeDemandRoutes.NativeUiLayoutDiagnostics,
                GameBridgeDemandRoutes.CropHarvesting,
                GameBridgeDemandRoutes.AnimalViewer,
                GameBridgeDemandRoutes.CustomAnimalAnimatorBridge,
                GameBridgeDemandRoutes.AudioReplacement,
                GameBridgeDemandRoutes.ActionSpeed,
                GameBridgeDemandRoutes.ActionCompletion
            };

            RuntimeCapabilityDescriptor[] catalog = GameBridgeDemandRoutes.Catalog.ToArray();
            Assert(catalog.Select(item => item.CapabilityId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == catalog.Length, "Capability ids must be unique.");
            Assert(mainFeatures.All(id => catalog.Any(item => item.CapabilityId.Equals(id, StringComparison.OrdinalIgnoreCase))), "The fixed catalog must contain every retained Runtime feature contract.");
            Assert(!catalog.Any(item => item.CapabilityId.Contains("Equipment", StringComparison.OrdinalIgnoreCase)), "Frozen EquipmentSlots compatibility must not retain a mandatory or periodic demand route.");
            Assert(catalog.Any(item => item.CapabilityId == GameBridgeDemandRoutes.NativeUiLayoutDiagnostics && item.Outcome == RuntimeCapabilityOutcome.Mandatory), "Native title layout repair must stay mandatory.");
            RuntimeCapabilityDescriptor[] restart = catalog.Where(item => item.Outcome == RuntimeCapabilityOutcome.RestartRequired).ToArray();
            Assert(restart.Length == 1 && restart[0].CapabilityId == GameBridgeDemandRoutes.AuthorSessionReload, "Only the named AuthorSession same-process replacement route is RestartRequired; optional installed hooks are ProcessPinnedDormant.");
        }

        private static void CompatibilityHostRejectsNonCanonicalReceiptPath()
        {
            string gamePath = Path.Combine(DtmApiTestSession.Current.RootPath, "compatibility-host-path-drift-" + (++fixtureSequence).ToString());
            Directory.CreateDirectory(Path.Combine(gamePath, "BepInEx", "plugins"));
            StageCompatibilityHostFixture(gamePath);

            const string fileName = "DTMAPI.GameBridge.DolocTown.Compatibility.dll";
            string canonical = Path.Combine(gamePath, "DTMAPI", "components", "compatibility", fileName);
            string misplaced = Path.Combine(gamePath, "BepInEx", "plugins", fileName);
            File.Move(canonical, misplaced);
            string manifestPath = Path.Combine(gamePath, "DTMAPI", "release-manifest.json");
            string manifest = File.ReadAllText(manifestPath, Encoding.UTF8)
                .Replace(
                    "DTMAPI/components/compatibility/" + fileName,
                    "BepInEx/plugins/" + fileName,
                    StringComparison.Ordinal);
            File.WriteAllText(manifestPath, manifest, new UTF8Encoding(false));

            var runtime = new DtmApiRuntime(new FakeHost(gamePath), new ConfigMenuRegistry());
            bool rejected = false;
            try
            {
                CompatibilityHostBroker.For(runtime).GetService("NonCanonicalPathProbe", Array.Empty<object>());
            }
            catch (InvalidOperationException ex)
            {
                rejected = ex.ToString().Contains("frozen dormant-shipped topology", StringComparison.Ordinal);
            }

            IHookStatusInfo status = runtime.Diagnostics.GetHookStatuses().Single(item => item.HookId == "Compatibility.Host");
            Assert(rejected && status.Status == "failed-closed" && runtime.DemandCoordinator.GetSnapshot().TotalDemand == 0,
                "A valid Host moved into the BepInEx scan tree must fail closed before service, demand, callback, or Hook activation.");
        }

        private static void CompatibilityHostRejectsWrongIdentityBeforeLoad() =>
            CompatibilityHostRejectsCandidateBeforeLoad(
                "CompatibilityHostWrongNameFixture",
                "DTMAPI.GameBridge.DolocTown.Compatibility.Substitute.dll",
                "wrong-name",
                "DTMAPI.GameBridge.DolocTown.Compatibility.Substitute");

        private static void CompatibilityHostRejectsWrongVersionBeforeLoad() =>
            CompatibilityHostRejectsCandidateBeforeLoad(
                "CompatibilityHostWrongVersionFixture",
                "DTMAPI.GameBridge.DolocTown.Compatibility.dll",
                "wrong-version",
                CompatibilityHostBroker.AssemblySimpleName);

        private static void CompatibilityHostRejectsWrongFrameworkBeforeLoad() =>
            CompatibilityHostRejectsCandidateBeforeLoad(
                "CompatibilityHostWrongFrameworkFixture",
                "DTMAPI.GameBridge.DolocTown.Compatibility.dll",
                "wrong-framework",
                CompatibilityHostBroker.AssemblySimpleName);

        private static void DebugConsoleCompatibilityProxyRemainsDormantUntilOldAbiCall()
        {
            string gamePath = Path.Combine(
                DtmApiTestSession.Current.RootPath,
                "debug-console-compatibility-dormant-" +
                (++fixtureSequence).ToString());
            Directory.CreateDirectory(
                Path.Combine(gamePath, "BepInEx", "plugins"));
            StageCompatibilityHostFixture(gamePath);
            var runtime = new DtmApiRuntime(
                new FakeHost(gamePath),
                new ConfigMenuRegistry());
            var proxy = new DebugConsoleCompatibilityProxy(runtime);
            CompatibilityHostBroker broker =
                CompatibilityHostBroker.For(runtime);

            Assert(
                !proxy.IsOpen &&
                proxy.CountOwnerResources("DTMAPI.Tests.LegacyConsole") == 0 &&
                !broker.TryGetService("DebugConsole", out _),
                "Constructing and observing the mandatory DebugConsole proxy must not load the optional Compatibility component.");

            BridgeFeatureStatus status =
                proxy.GetStatus("DTMAPI.Tests.LegacyConsole");
            Assert(
                broker.TryGetService("DebugConsole", out object? service) &&
                service != null &&
                status != null &&
                CompatibilityHostBroker.ReadProperty(
                    service,
                    "ConsumedInputThisFrame",
                    true) == false,
                "The first explicit frozen ABI call may load the optional service, which must remain input- and Hook-dormant.");
            proxy.ShutdownIfLoaded("unit dormant cleanup");
            Assert(
                proxy.CountOwnerResources("DTMAPI.Tests.LegacyConsole") == 0 &&
                proxy.GetLifecycleSummary().Contains(
                    "compatibilityPatches=0",
                    StringComparison.Ordinal),
                "A resident Compatibility service with no old owner must report zero UI roots and input patches.");
        }

        private static void CompatibilityHostRejectsCandidateBeforeLoad(string fixtureDirectory, string fixtureFileName, string label, string actualAssemblyName)
        {
            Assert(!AppDomain.CurrentDomain.GetAssemblies().Any(candidate =>
                    string.Equals(candidate.GetName().Name, actualAssemblyName, StringComparison.OrdinalIgnoreCase)),
                "The " + label + " pre-load fixture assembly must not already be resident.");

            string gamePath = Path.Combine(DtmApiTestSession.Current.RootPath, "compatibility-host-" + label + "-" + (++fixtureSequence).ToString());
            Directory.CreateDirectory(Path.Combine(gamePath, "BepInEx", "plugins"));
            StageCompatibilityHostFixture(gamePath, fixtureDirectory, fixtureFileName);
            var runtime = new DtmApiRuntime(new FakeHost(gamePath), new ConfigMenuRegistry());
            bool rejected = false;
            try
            {
                CompatibilityHostBroker.For(runtime).GetService("PreLoadIdentityProbe", Array.Empty<object>());
            }
            catch (InvalidOperationException ex)
            {
                rejected = ex.ToString().Contains("pre-load validation", StringComparison.Ordinal);
            }

            Assert(rejected, "The " + label + " compatibility candidate must fail its metadata identity check.");
            Assert(!AppDomain.CurrentDomain.GetAssemblies().Any(candidate =>
                    string.Equals(candidate.GetName().Name, actualAssemblyName, StringComparison.OrdinalIgnoreCase)),
                "The " + label + " compatibility candidate must be rejected before Assembly.Load.");
            Assert(runtime.DemandCoordinator.GetSnapshot().TotalDemand == 0,
                "A rejected " + label + " compatibility candidate must retain no demand.");
        }

        private static void FrozenCameraHostFailsClosedInBothOrdersBeforeNativeWrite()
        {
            CreateFixture(
                out DtmApiRuntime zoomFirstRuntime,
                out DolocTownGameBridge zoomFirstBridge);
            IManifest zoomFirstOwner = Manifest(
                "DTMAPI.Tests.Zoom.ObsoleteApiFirstCall");
            try
            {
                CameraFeature zoomFirstFeature =
                    zoomFirstBridge.CameraFeatureForQa ??
                    throw new InvalidOperationException(
                        "Camera feature should be registered.");
                zoomFirstFeature.HookBridge
                    .SetCompatibilityOwnerClaimOverrideForTests(
                        () => true);
                CameraZoomRegisterResult zoomFirstResult =
                    zoomFirstFeature.ZoomApi.Register(
                        zoomFirstOwner,
                        new CameraZoomOptions
                        {
                            Enabled = true
                        });
                Assert(
                    zoomFirstBridge.CountGameBridgeOwnerResources(
                        zoomFirstOwner.UniqueID) > 0 &&
                    zoomFirstRuntime.DemandCoordinator
                        .GetOwnerDemandCount(
                            zoomFirstOwner.UniqueID) > 0,
                    "When frozen ICameraZoomApi is the first Camera Host call, it must receive the same synchronous exact-owner claim as ICameraViewApi before retaining its compatibility lease. result=" +
                    zoomFirstResult.Message);
            }
            finally
            {
                zoomFirstBridge.RemoveGameBridgeOwnerResourcesForTests(
                    zoomFirstOwner.UniqueID,
                    ModOwnerCleanupReason.Unload);
                zoomFirstBridge.Shutdown(
                    "Frozen Camera Zoom-first Host test");
            }

            string productFirstPath = Path.Combine(
                DtmApiTestSession.Current.RootPath,
                "camera-product-first-" +
                (++fixtureSequence).ToString());
            Directory.CreateDirectory(
                Path.Combine(productFirstPath, "BepInEx", "plugins"));
            StageCompatibilityHostFixture(productFirstPath);
            var productFirstRuntime = new DtmApiRuntime(
                new FakeHost(productFirstPath),
                new ConfigMenuRegistry());
            bool productPresent = true;
            double nativeSize = 10d;
            int nativeWrites = 0;
            object productFirst = CompatibilityHostBroker
                .For(productFirstRuntime)
                .GetService(
                    "Camera",
                    new object[]
                    {
                        new Func<bool>(() => productPresent),
                        new Func<bool>(() => false)
                    });
            CompatibilityHostBroker.Invoke(
                productFirst,
                "ConfigureNativeAccessForTests",
                new Func<double?>(() => nativeSize),
                new Func<double, bool>(value =>
                {
                    nativeWrites++;
                    nativeSize = value;
                    return true;
                }));
            IManifest productFirstOwner = Manifest(
                "DTMAPI.Tests.Zoom.ProductFirst");
            bool productFirstRejected = false;
            try
            {
                CompatibilityHostBroker.Invoke<ICameraViewLease>(
                    productFirst,
                    "ViewAcquireLease",
                    productFirstOwner,
                    new CameraViewRequest
                    {
                        Enabled = true,
                        ViewScale = 2d
                    });
            }
            catch (InvalidOperationException ex)
            {
                productFirstRejected =
                    ex.Message.Contains(
                        "managed product",
                        StringComparison.OrdinalIgnoreCase);
            }
            Assert(
                productFirstRejected &&
                nativeWrites == 0 &&
                CompatibilityHostBroker.Invoke<int>(
                    productFirst,
                    "CountOwnerResources",
                    productFirstOwner.UniqueID) == 0 &&
                productFirstRuntime.DemandCoordinator
                    .GetOwnerDemandCount(
                        productFirstOwner.UniqueID) == 0,
                "Product-first then frozen Camera acquire must fail before state, demand, or native mutation.");

            string compatibilityFirstPath = Path.Combine(
                DtmApiTestSession.Current.RootPath,
                "camera-compatibility-first-" +
                (++fixtureSequence).ToString());
            Directory.CreateDirectory(
                Path.Combine(
                    compatibilityFirstPath,
                    "BepInEx",
                    "plugins"));
            StageCompatibilityHostFixture(
                compatibilityFirstPath);
            var compatibilityRuntime = new DtmApiRuntime(
                new FakeHost(compatibilityFirstPath),
                new ConfigMenuRegistry());
            productPresent = false;
            nativeSize = 10d;
            nativeWrites = 0;
            object compatibilityFirst = CompatibilityHostBroker
                .For(compatibilityRuntime)
                .GetService(
                    "Camera",
                    new object[]
                    {
                        new Func<bool>(() => productPresent),
                        new Func<bool>(() => false)
                    });
            CompatibilityHostBroker.Invoke(
                compatibilityFirst,
                "ConfigureNativeAccessForTests",
                new Func<double?>(() => nativeSize),
                new Func<double, bool>(value =>
                {
                    nativeWrites++;
                    nativeSize = value;
                    return true;
                }));
            IManifest compatibilityOwner = Manifest(
                "DTMAPI.Tests.Zoom.CompatibilityFirst");
            bool noClaimRejected = false;
            try
            {
                CompatibilityHostBroker.Invoke<ICameraViewLease>(
                    compatibilityFirst,
                    "ViewAcquireLease",
                    compatibilityOwner,
                    new CameraViewRequest
                    {
                        Enabled = true,
                        ViewScale = 2d
                    });
            }
            catch (InvalidOperationException ex)
            {
                noClaimRejected = ex.Message.Contains(
                    "atomically claim",
                    StringComparison.OrdinalIgnoreCase);
            }
            Assert(
                noClaimRejected &&
                nativeWrites == 0 &&
                compatibilityRuntime.DemandCoordinator
                    .GetOwnerDemandCount(
                        compatibilityOwner.UniqueID) == 0 &&
                CompatibilityHostBroker.Invoke<int>(
                    compatibilityFirst,
                    "CountOwnerResources",
                    compatibilityOwner.UniqueID) == 0,
                "A frozen Camera request must fail before demand/state retention or native write when its exact Compatibility Hook owner cannot be synchronously claimed.");
            IManifest obsoleteZoomOwner = Manifest(
                "DTMAPI.Tests.Zoom.ObsoleteApiNoClaim");
            bool obsoleteZoomRejected = false;
            try
            {
                CompatibilityHostBroker
                    .Invoke<CameraZoomRegisterResult>(
                        compatibilityFirst,
                        "ZoomRegister",
                        obsoleteZoomOwner,
                        new CameraZoomOptions
                        {
                            Enabled = true
                        });
            }
            catch (InvalidOperationException ex)
            {
                obsoleteZoomRejected =
                    ex.Message.Contains(
                        "atomically claim",
                        StringComparison.OrdinalIgnoreCase);
            }
            Assert(
                obsoleteZoomRejected &&
                CompatibilityHostBroker.Invoke<int>(
                    compatibilityFirst,
                    "CountOwnerResources",
                    obsoleteZoomOwner.UniqueID) == 0 &&
                compatibilityRuntime.DemandCoordinator
                    .GetOwnerDemandCount(
                        obsoleteZoomOwner.UniqueID) == 0 &&
                nativeWrites == 0,
                "A frozen ICameraZoomApi Register must not retain options, state, lease, demand, or native writes when the exact owner claim fails.");

            productPresent = true;
            int removed = CompatibilityHostBroker.Invoke<int>(
                compatibilityFirst,
                "ReconcileManagedProductOwnerBeforeHookInstall");
            Assert(
                removed == 0 &&
                nativeWrites == 0 &&
                CompatibilityHostBroker.Invoke<int>(
                    compatibilityFirst,
                    "CountOwnerResources",
                    compatibilityOwner.UniqueID) == 0 &&
                compatibilityRuntime.DemandCoordinator
                    .GetOwnerDemandCount(
                        compatibilityOwner.UniqueID) == 0,
                "Compatibility-request-before-Hook then product load must leave no hidden compatibility demand for the product to overtake.");

            string hookReadyPath = Path.Combine(
                DtmApiTestSession.Current.RootPath,
                "camera-hook-ready-owner-switch-" +
                (++fixtureSequence).ToString());
            Directory.CreateDirectory(
                Path.Combine(hookReadyPath, "BepInEx", "plugins"));
            StageCompatibilityHostFixture(hookReadyPath);
            var hookReadyRuntime = new DtmApiRuntime(
                new FakeHost(hookReadyPath),
                new ConfigMenuRegistry());
            productPresent = false;
            nativeSize = 10d;
            nativeWrites = 0;
            object hookReady = CompatibilityHostBroker
                .For(hookReadyRuntime)
                .GetService(
                    "Camera",
                    new object[]
                    {
                        new Func<bool>(() => productPresent),
                        new Func<bool>(() => true)
                    });
            CompatibilityHostBroker.Invoke(
                hookReady,
                "ConfigureNativeAccessForTests",
                new Func<double?>(() => nativeSize),
                new Func<double, bool>(value =>
                {
                    nativeWrites++;
                    nativeSize = value;
                    return true;
                }));
            IManifest activeOwner = Manifest(
                "DTMAPI.Tests.Zoom.ActiveCompatibility");
            ICameraViewLease active =
                CompatibilityHostBroker.Invoke<ICameraViewLease>(
                    hookReady,
                    "ViewAcquireLease",
                    activeOwner,
                    new CameraViewRequest
                    {
                        Enabled = true,
                        ViewScale = 2d
                    });
            Assert(
                active.LastResult.Success &&
                nativeWrites == 1 &&
                Math.Abs(nativeSize - 20d) < 0.001d,
                "An exact-owner-ready frozen Camera lease may perform its one compatibility native write.");
            productPresent = true;
            bool activeUpdateRejected = false;
            try
            {
                active.SetViewScale(
                    3d,
                    "managed product arrived");
            }
            catch (InvalidOperationException)
            {
                activeUpdateRejected = true;
            }
            Assert(
                activeUpdateRejected &&
                nativeWrites == 1 &&
                active.IsReleased &&
                hookReadyRuntime.DemandCoordinator
                    .GetOwnerDemandCount(
                        activeOwner.UniqueID) == 0,
                "Every mutable old lease entry must re-check the managed product owner before another native write and clear stale compatibility demand.");
        }

        private static void FrozenSaveSlotsOwnerFailsClosedInBothOrdersAndRetriesRestore()
        {
            MoreSavesNativeOwnerCoordinator.ResetForTests();
            string productFirstPath = Path.Combine(DtmApiTestSession.Current.RootPath, "save-slots-product-first-" + (++fixtureSequence).ToString());
            Directory.CreateDirectory(Path.Combine(productFirstPath, "BepInEx", "plugins"));
            StageCompatibilityHostFixture(productFirstPath);
            var productFirstRuntime = new DtmApiRuntime(new FakeHost(productFirstPath), new ConfigMenuRegistry());
            var productFirstManager = new SaveSlotsManagerFixture();
            object productFirstService = CompatibilityHostBroker.For(productFirstRuntime).GetService(
                "SaveSlots",
                new Func<object?>(() => productFirstManager),
                new Func<object, int>(value => ((SaveSlotsManagerFixture)value).archiveFileCount),
                new Func<object, int, bool>((value, count) =>
                {
                    ((SaveSlotsManagerFixture)value).archiveFileCount = count;
                    return true;
                }));
            productFirstManager.archiveFileCount = 12;
            SaveSlotsState unregistered = CompatibilityHostBroker.Invoke<SaveSlotsState>(
                productFirstService,
                "GetState",
                "DTMAPI.Tests.MoreSaves.Unregistered");
            Assert(!unregistered.IsConfigured && !unregistered.Enabled &&
                   unregistered.NativeSlotCount == 12 && unregistered.RequestedSlotCount == 6 &&
                   unregistered.AppliedSlotCount == 12 && unregistered.Status == "not-configured",
                "An unregistered frozen ABI owner must report the observed native count without becoming configured.");

            IManifest coldDisabled = Manifest("DTMAPI.Tests.MoreSaves.ColdDisabled");
            SaveSlotsRegisterResult coldDisabledResult = CompatibilityHostBroker.Invoke<SaveSlotsRegisterResult>(
                productFirstService,
                "RegisterSlots",
                coldDisabled,
                new SaveSlotsOptions { Enabled = false, SlotCount = 24 });
            SaveSlotsState coldDisabledState = CompatibilityHostBroker.Invoke<SaveSlotsState>(
                productFirstService,
                "GetState",
                coldDisabled.UniqueID);
            Assert(coldDisabledResult.Success && coldDisabledResult.RequestedSlotCount == 6 &&
                   productFirstManager.archiveFileCount == 6 && coldDisabledState.IsConfigured &&
                   !coldDisabledState.Enabled && coldDisabledState.Status == "disabled-vanilla-slot-count" &&
                   MoreSavesNativeOwnerCoordinator.ActiveOwner.Length == 0,
                "A cold disabled registration must normalize native state to six before releasing its temporary compatibility lease.");
            CompatibilityHostBroker.Invoke<int>(productFirstService, "RemoveOwner", coldDisabled.UniqueID, "unit cold-disabled cleanup");

            IManifest mixedEnabled = Manifest("DTMAPI.Tests.MoreSaves.MixedEnabled");
            IManifest mixedDisabled = Manifest("DTMAPI.Tests.MoreSaves.MixedDisabled");
            CompatibilityHostBroker.Invoke<SaveSlotsRegisterResult>(
                productFirstService,
                "RegisterSlots",
                mixedEnabled,
                new SaveSlotsOptions { Enabled = true });
            SaveSlotsRegisterResult mixedDisabledResult = CompatibilityHostBroker.Invoke<SaveSlotsRegisterResult>(
                productFirstService,
                "RegisterSlots",
                mixedDisabled,
                new SaveSlotsOptions { Enabled = false });
            SaveSlotsState mixedEnabledState = CompatibilityHostBroker.Invoke<SaveSlotsState>(
                productFirstService,
                "GetState",
                mixedEnabled.UniqueID);
            SaveSlotsState mixedDisabledState = CompatibilityHostBroker.Invoke<SaveSlotsState>(
                productFirstService,
                "GetState",
                mixedDisabled.UniqueID);
            Assert(productFirstManager.archiveFileCount == 12 &&
                   mixedDisabledResult.RequestedSlotCount == 6 && mixedDisabledResult.AppliedSlotCount == 12 &&
                   mixedEnabledState.Enabled && mixedEnabledState.Status == "configured-official-archive-count" &&
                   !mixedDisabledState.Enabled && mixedDisabledState.RequestedSlotCount == 6 &&
                   mixedDisabledState.AppliedSlotCount == 12 && mixedDisabledState.Status == "disabled-vanilla-slot-count",
                "Mixed frozen owners must preserve each owner's configured state while projecting the shared effective native count.");
            CompatibilityHostBroker.Invoke<int>(productFirstService, "RemoveOwner", mixedDisabled.UniqueID, "unit mixed-disabled cleanup");
            CompatibilityHostBroker.Invoke<int>(productFirstService, "RemoveOwner", mixedEnabled.UniqueID, "unit mixed-enabled cleanup");
            Assert(productFirstManager.archiveFileCount == 6 && MoreSavesNativeOwnerCoordinator.ActiveOwner.Length == 0,
                "Mixed-owner cleanup must restore native six and release the compatibility writer.");

            Assert(MoreSavesNativeOwnerCoordinator.TryAcquire(MoreSavesNativeOwnerCoordinator.ProductOwner, out _),
                "Product-first fixture must acquire the one archive-count writer.");
            bool productFirstRejected = false;
            try
            {
                CompatibilityHostBroker.Invoke(
                    productFirstService,
                    "RegisterSlots",
                    Manifest("DTMAPI.Tests.MoreSaves.ProductFirst"),
                    new SaveSlotsOptions { Enabled = true, SlotCount = 12 });
            }
            catch (InvalidOperationException ex)
            {
                productFirstRejected = ex.Message.Contains("failed closed", StringComparison.Ordinal);
            }
            Assert(productFirstRejected && productFirstManager.archiveFileCount == 6 &&
                   CompatibilityHostBroker.Invoke<int>(productFirstService, "CountOwnerResources", "DTMAPI.Tests.MoreSaves.ProductFirst") == 0,
                "Product-first then frozen ISaveSlotsApi must reject before state retention or a native write.");
            Assert(MoreSavesNativeOwnerCoordinator.ReleaseAfterNativeRestore(MoreSavesNativeOwnerCoordinator.ProductOwner, true),
                "Product-first fixture cleanup must release its restored native lease.");

            string compatibilityFirstPath = Path.Combine(DtmApiTestSession.Current.RootPath, "save-slots-compatibility-first-" + (++fixtureSequence).ToString());
            Directory.CreateDirectory(Path.Combine(compatibilityFirstPath, "BepInEx", "plugins"));
            StageCompatibilityHostFixture(compatibilityFirstPath);
            var compatibilityRuntime = new DtmApiRuntime(new FakeHost(compatibilityFirstPath), new ConfigMenuRegistry());
            var manager = new SaveSlotsManagerFixture();
            bool managerAvailable = false;
            bool failRestore = false;
            object service = CompatibilityHostBroker.For(compatibilityRuntime).GetService(
                "SaveSlots",
                new Func<object?>(() => managerAvailable ? manager : null),
                new Func<object, int>(value => ((SaveSlotsManagerFixture)value).archiveFileCount),
                new Func<object, int, bool>((value, count) =>
                {
                    if (count == 6 && failRestore)
                        throw new InvalidOperationException("synthetic restore failure");
                    ((SaveSlotsManagerFixture)value).archiveFileCount = count;
                    return true;
                }));
            IManifest first = Manifest("DTMAPI.Tests.MoreSaves.CompatibilityFirstA");
            IManifest second = Manifest("DTMAPI.Tests.MoreSaves.CompatibilityFirstB");
            SaveSlotsRegisterResult pending = CompatibilityHostBroker.Invoke<SaveSlotsRegisterResult>(
                service,
                "RegisterSlots",
                first,
                new SaveSlotsOptions { Enabled = true, SlotCount = 24 });
            Assert(!pending.Success && pending.FailureReason == "missing-game-manager" &&
                   MoreSavesNativeOwnerCoordinator.IsOwnedBy(MoreSavesNativeOwnerCoordinator.CompatibilityOwner),
                "A missing manager must retain one compatibility writer lease and a retryable failure instead of releasing ownership.");

            managerAvailable = true;
            CompatibilityHostBroker.Invoke(service, "RefreshSaveSlotExpansionForRuntime", true, "unit manager ready");
            Assert(manager.archiveFileCount == 12, "The bounded manager retry must normalize the frozen ABI request to twelve.");
            Assert(!MoreSavesNativeOwnerCoordinator.TryAcquire(MoreSavesNativeOwnerCoordinator.ProductOwner, out _),
                "Compatibility-first then product must fail closed while the frozen executor owns the native writer.");

            SaveSlotsRegisterResult secondResult = CompatibilityHostBroker.Invoke<SaveSlotsRegisterResult>(
                service,
                "RegisterSlots",
                second,
                new SaveSlotsOptions { Enabled = true });
            Assert(secondResult.Success && manager.archiveFileCount == 12, "A second old ABI consumer must share the one compatibility lease.");
            CompatibilityHostBroker.Invoke<int>(service, "RemoveOwner", first.UniqueID, "unit first owner removal");
            Assert(manager.archiveFileCount == 12 &&
                   MoreSavesNativeOwnerCoordinator.IsOwnedBy(MoreSavesNativeOwnerCoordinator.CompatibilityOwner),
                "Removing one old consumer must not restore six while another enabled consumer remains.");

            failRestore = true;
            CompatibilityHostBroker.Invoke<int>(service, "RemoveOwner", second.UniqueID, "unit final owner restoration failure");
            Assert(manager.archiveFileCount == 12 &&
                   MoreSavesNativeOwnerCoordinator.IsOwnedBy(MoreSavesNativeOwnerCoordinator.CompatibilityOwner) &&
                   compatibilityRuntime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.SaveSlotsPendingRestore) == 1,
                "A restoration exception must retain the exact compatibility lease and one pending retry demand.");

            failRestore = false;
            CompatibilityHostBroker.Invoke(service, "RefreshSaveSlotExpansionForRuntime", true, "unit restore retry");
            Assert(manager.archiveFileCount == 6 &&
                   MoreSavesNativeOwnerCoordinator.ActiveOwner.Length == 0 &&
                   compatibilityRuntime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.SaveSlotsPendingRestore) == 0,
                "A successful retry must restore native six, release the exact writer, and clear pending demand.");
            MoreSavesNativeOwnerCoordinator.ResetForTests();
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
                Assert(end.ContentQueryCandidateBuilds == start.ContentQueryCandidateBuilds && end.CustomAnimalDefinitionCandidateBuilds == start.CustomAnimalDefinitionCandidateBuilds, "No-demand frames must build zero ContentQuery or CustomAnimals candidates.");
                Assert(end.OptionalFeatureFileStatusCalls == start.OptionalFeatureFileStatusCalls && end.OptionalDirectoryEnumerations == start.OptionalDirectoryEnumerations, "No-demand frames must perform zero optional file-status or directory-enumeration work.");
                Assert(end.OptionalReflectionObjectSearches == start.OptionalReflectionObjectSearches && end.OptionalNativeUpdaterInvocations == start.OptionalNativeUpdaterInvocations, "No-demand frames must perform zero optional reflection/object search or native updater work.");
                Assert(end.CustomAnimalsRetainedCallbackWork == start.CustomAnimalsRetainedCallbackWork && end.AudioRetainedCallbackWork == start.AudioRetainedCallbackWork && end.OptionalRetainedCallbackWork == start.OptionalRetainedCallbackWork, "CustomAnimals and Audio retained callback work must remain individually and compositionally zero.");
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

        private static void RetainedNativeCallbacksSleepAtZeroDemand()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            HookCallbackBoundaryScope callbackBoundary = BindHookCallbackBoundary(runtime, bridge);
            try
            {
                Assert(ReferenceEquals(DolocTownHookCallbacks.Runtime, runtime) && ReferenceEquals(DolocTownHookCallbacks.Bridge, bridge), "The retained-callback allocation test must exercise the live fixture through the production static callback boundary.");
                Assert(bridge.RetainedCallbackDemandMaskForTests == 0, "The live retained-callback fixture must begin with an exact zero demand mask.");
                object native = new object();
                Array inventories = Array.Empty<object>();
                InvokeZeroDemandRetainedNativeCallbacks(native, inventories);

                long customBefore = bridge.CustomAnimalAnimatorBridgeService!.RetainedCallbackWorkCountForTest;
                long audioBefore = bridge.AudioReplacementService!.RetainedCallbackWorkCountForTest;
                int cameraBefore = bridge.EnvironmentResetCountForTests;
                Assert(bridge.RetainedCallbackDemandMaskForTests == 0, "No optional retained callback route may publish a scalar demand bit without a consumer.");
                long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                for (int index = 0; index < 10000; index++)
                    InvokeZeroDemandRetainedNativeCallbacks(native, inventories);
                long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

                Assert(bridge.CustomAnimalAnimatorBridgeService.RetainedCallbackWorkCountForTest == customBefore, "Retained CustomAnimals patches must do zero service/reflection work after definition demand reaches zero.");
                Assert(bridge.AudioReplacementService.RetainedCallbackWorkCountForTest == audioBefore, "Retained Audio patches must do zero context/filter work after definition demand reaches zero.");
                Assert(bridge.EnvironmentResetCountForTests == cameraBefore, "Retained SetEnvCamera must not enter Camera or broad feature fanout with zero lease/restore demand.");
                Assert(bridge.RetainedCallbackDemandMaskForTests == 0, "Retained callback invocation must not create optional demand.");
                Assert(allocated == 0, "10,000 all-family zero-demand retained callback passes must allocate zero bytes; actual=" + allocated + ".");
            }
            finally
            {
                try
                {
                    bridge.Shutdown("Batch5 retained callback zero-demand test");
                }
                finally
                {
                    callbackBoundary.Dispose();
                }
            }
        }

        private static void GenericArrayPostfixSleepsBeforeValueTypeBoxing()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            HookCallbackBoundaryScope callbackBoundary = BindHookCallbackBoundary(runtime, bridge);
            try
            {
                Assert(ReferenceEquals(DolocTownHookCallbacks.Runtime, runtime) && ReferenceEquals(DolocTownHookCallbacks.Bridge, bridge), "The generic array-result allocation test must exercise the live fixture through the production static callback boundary.");
                Assert(bridge.RetainedCallbackDemandMaskForTests == 0, "The live generic array-result fixture must begin with an exact zero demand mask.");
                int callbackInvocations = 0;
                object[] replacementResult = { new object() };
                Array Callback(object instance, object anchor, object area, bool useBox, Array result)
                {
                    callbackInvocations++;
                    return replacementResult;
                }

                HarmonyReflectionPatcher.ConfigureArrayResultPostfixForTests(
                    Callback,
                    DolocTownHookCallbacks.HasChestLocatorEnhancerRetainedCallbackDemand);

                object native = new object();
                var anchor = new ValueTypeGridPoint(17, 31);
                var area = new ValueTypeGridPoint(47, 53);
                object[] nativeResult = { native };
                object[] result = nativeResult;
                IManifest demandOwner = Manifest("DTMAPI.Tests.Batch5.ChestLocator.GenericWrapperPositiveControl");
                ChestLocatorEnhancerRegisterResult registration = bridge.ChestLocatorEnhancerService!.Register(
                    demandOwner,
                    new ChestLocatorEnhancerOptions { Enabled = true });
                int chestLocatorDemandBit = (int)GameBridgeRetainedCallbackDemand.ChestLocatorEnhancer;

                Assert(registration.Success, "The generic array-result positive control must publish one real ChestLocator owner registration.");
                Assert(DolocTownHookCallbacks.HasChestLocatorEnhancerRetainedCallbackDemand(), "The live generic array-result guard must become true when ChestLocator has one owner.");
                Assert(bridge.RetainedCallbackDemandMaskForTests == chestLocatorDemandBit, "The live generic array-result fixture must publish exactly the ChestLocator retained-callback bit for its positive control.");

                HarmonyReflectionPatcher.ArrayResultPostfixGeneric<ValueTypeGridPoint, ValueTypeGridPoint, object>(
                    native,
                    anchor,
                    area,
                    false,
                    ref result);
                Assert(callbackInvocations == 1 && ReferenceEquals(result, replacementResult), "The generic array-result positive control must dispatch the object callback once and accept its replacement result while ChestLocator demand is active.");

                Assert(bridge.ChestLocatorEnhancerService.RemoveOwner(demandOwner.UniqueID, "generic wrapper zero-demand allocation measurement") > 0, "The generic array-result positive-control owner must be removable before the zero-demand measurement.");
                Assert(!DolocTownHookCallbacks.HasChestLocatorEnhancerRetainedCallbackDemand(), "The live generic array-result guard must become false immediately after the final ChestLocator owner is released.");
                Assert(bridge.RetainedCallbackDemandMaskForTests == 0, "The ChestLocator retained-callback bit must clear synchronously before the zero-demand measurement.");

                result = nativeResult;
                HarmonyReflectionPatcher.ArrayResultPostfixGeneric<ValueTypeGridPoint, ValueTypeGridPoint, object>(
                    native,
                    anchor,
                    area,
                    false,
                    ref result);
                Assert(callbackInvocations == 1 && ReferenceEquals(result, nativeResult), "The released generic array-result wrapper must warm its zero-demand path without dispatching the callback or replacing the reset native result.");

                long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
                for (int index = 0; index < 10000; index++)
                {
                    HarmonyReflectionPatcher.ArrayResultPostfixGeneric<ValueTypeGridPoint, ValueTypeGridPoint, object>(
                        native,
                        anchor,
                        area,
                        false,
                        ref result);
                }
                long allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

                Assert(callbackInvocations == 1, "The generic array-result wrapper must remain at the one positive-control dispatch after 10,000 zero-demand calls.");
                Assert(ReferenceEquals(result, nativeResult) && ReferenceEquals(result[0], native), "The zero-demand generic array-result wrapper must not replace or mutate the native result.");
                Assert(anchor.X == 17 && anchor.Y == 31 && area.X == 47 && area.Y == 53, "The zero-demand generic array-result wrapper must not mutate its value-type native arguments.");
                Assert(bridge.RetainedCallbackDemandMaskForTests == 0, "The generic array-result wrapper must not create ChestLocator demand.");
                Assert(allocated == 0, "10,000 zero-demand generic array-result wrapper calls with two value-type arguments must allocate zero bytes before object boxing; actual=" + allocated + ".");
            }
            finally
            {
                try
                {
                    bridge.Shutdown("Batch5 generic array-result wrapper zero-demand test");
                }
                finally
                {
                    HarmonyReflectionPatcher.ClearStaticCallbacks();
                    callbackBoundary.Dispose();
                }
            }
        }

        private static void InvokeZeroDemandRetainedNativeCallbacks(object native, Array inventories)
        {
            bool nativeResult = false;
            bool nativeState = false;
            float nativeDelta = 0.25f;
            float pullDuration = 0.5f;
            int recipeTime = 12;
            string tooltip = "native-tooltip";

            DolocTownHookCallbacks.DolocApiSetEnvCameraPostfix();
            if (!DolocTownHookCallbacks.WwiseInternalPostSoundEventPrefix("PLAY_RESOURCE_PAPER_BOX", null, null, false, ref nativeResult))
                throw new InvalidOperationException("Zero-demand Audio callback suppressed native sound.");
            DolocTownHookCallbacks.DungeonResourceModelPaperBoxOnInteractPostfix(native);
            DolocTownHookCallbacks.AnimalPlayAnimalSoundPrefix(native);
            DolocTownHookCallbacks.AnimalPlayAnimalSoundPostfix();

            DolocTownHookCallbacks.ModDataConstructorPostfix(native, native);
            if (!DolocTownHookCallbacks.SteamWorkshopUploaderResolveUploadPlanPrefix(native, native, native))
                throw new InvalidOperationException("Zero-demand Workshop callback suppressed the native upload plan.");
            DolocTownHookCallbacks.SteamWorkshopUploaderResolveUploadPlanPostfix(native, native, native);

            DolocTownHookCallbacks.ItemTitlePostfix(native, ref tooltip);
            DolocTownHookCallbacks.ItemDescriptionPostfix(native, ref tooltip);
            DolocTownHookCallbacks.ItemDetailInfoPostfix(native, ref tooltip);
            DolocTownHookCallbacks.AnimalFullInfoDataCtorPostfix(native, native);
            DolocTownHookCallbacks.AnimalViewerShowPrefix(native, native);
            DolocTownHookCallbacks.AnimalViewerShowPostfix(native, native);
            DolocTownHookCallbacks.AnimalPanelUiStateUnregisterPostfix();

            DolocTownHookCallbacks.AnimalOnRenderPostfix(native);
            DolocTownHookCallbacks.AnimalDebugSetAdultPostfix(native, false);
            DolocTownHookCallbacks.AnimalRendererOnRecyclePostfix(native);
            DolocTownHookCallbacks.AnimalSleepPostfix(native);
            DolocTownHookCallbacks.AnimalWakeUpPostfix(native);
            DolocTownHookCallbacks.AnimalControllerOnUpdatePostfix(native, 0f);
            DolocTownHookCallbacks.AnimalCallToRoomPostfix(native, native, native);
            DolocTownHookCallbacks.AnimalRendererOnFellPrefix(native, native);
            DolocTownHookCallbacks.AnimalRendererOnFellPostfix(native, native, false);
            DolocTownHookCallbacks.AnimalRendererPlayAnimationPostfix(native, "idle", false, 1f);
            DolocTownHookCallbacks.AnimalRendererFixedUpdatePostfix(native);

            DolocTownHookCallbacks.ToolColliderHandleToolsPostfix(native, native);
            DolocTownHookCallbacks.AgentStateToolEnterPostfix(native);
            DolocTownHookCallbacks.AgentStateToolExitPostfix();
            DolocTownHookCallbacks.AgentStateInteractEnterPostfix(native);
            DolocTownHookCallbacks.AgentStateInteractExitPostfix();
            DolocTownHookCallbacks.AgentStateEatEnterPostfix(native);
            DolocTownHookCallbacks.AgentControllerStateUseItemContinuesPrefix(ref nativeDelta);
            DolocTownHookCallbacks.AgentControllerStateInteractContinuesPrefix(ref nativeDelta);
            DolocTownHookCallbacks.AnimalRendererOnInteractPrefix(native);
            DolocTownHookCallbacks.AgentStateBaseExitPostfix();

            if (!CompatibilityDebugConsoleInputHooks.UseToolPrefix() ||
                !CompatibilityDebugConsoleInputHooks.UseItemPrefix() ||
                !CompatibilityDebugConsoleInputHooks.EnterUiCheckPrefix(ref nativeResult))
                throw new InvalidOperationException("Dormant Compatibility DebugConsole callback suppressed native input.");
            if (!DebugConsoleCreativeHooks.BoolTruePrefix(ref nativeResult) ||
                !DebugConsoleCreativeHooks.VoidSkipPrefix())
                throw new InvalidOperationException("Zero-demand Creative callback suppressed a native cost operation.");
            DebugConsoleCreativeHooks.RecipeTimePostfix(ref recipeTime);

            DolocTownHookCallbacks.FishingCompatibilityReadyEnterPostfix(native);
            DolocTownHookCallbacks.FishingCompatibilityReadyPlayPostfix(native);
            DolocTownHookCallbacks.FishingCompatibilityCastEnterPostfix(native);
            DolocTownHookCallbacks.FishingCompatibilityWaitEnterPostfix(native);
            DolocTownHookCallbacks.FishingCompatibilityWaitPlayPostfix(native);
            DolocTownHookCallbacks.FishingCompatibilityWaitNextStatePostfix(native, native);
            DolocTownHookCallbacks.FishingCompatibilityMiniGameStartPostfix(native);
            DolocTownHookCallbacks.FishingCompatibilityMiniGameUpdatePrefix(native);
            DolocTownHookCallbacks.FishingCompatibilityMiniGameUpdatePostfix(native);
            DolocTownHookCallbacks.FishingCompatibilityMiniGameStopPostfix(native);
            if (!DolocTownHookCallbacks.FishingCompatibilityInputNormalUseToolPrefix(ref nativeResult) ||
                !DolocTownHookCallbacks.FishingCompatibilityInputNormalUseToolInProgressPrefix(ref nativeResult) ||
                !DolocTownHookCallbacks.FishingCompatibilityInputNormalUseItemPrefix(ref nativeResult) ||
                !DolocTownHookCallbacks.FishingCompatibilityInputNormalUseItemInProgressPrefix(ref nativeResult) ||
                !DolocTownHookCallbacks.FishingCompatibilityInputNormalFishingPrefix(ref nativeResult) ||
                !DolocTownHookCallbacks.FishingCompatibilityInputNormalFishingInProgressPrefix(ref nativeResult))
                throw new InvalidOperationException("Zero-demand Fishing callback suppressed native input.");
            DolocTownHookCallbacks.FishingCompatibilityRodCastHookPostfix(native);
            DolocTownHookCallbacks.FishingCompatibilityPullEnterPostfix(native);
            DolocTownHookCallbacks.FishingCompatibilityPullExitPostfix();
            DolocTownHookCallbacks.FishingCompatibilityRodPullPostfix(ref pullDuration);
            DolocTownHookCallbacks.FishingCompatibilityRodPullCancelPostfix(ref pullDuration);

            if (!ReferenceEquals(inventories, DolocTownHookCallbacks.ArchiveDataHandleGetAvailableInventoriesPostfix(native, native, native, false, inventories)))
                throw new InvalidOperationException("Zero-demand ChestLocator callback replaced the native inventory result.");

            if (!tooltip.Equals("native-tooltip", StringComparison.Ordinal) ||
                nativeDelta != 0.25f ||
                pullDuration != 0.5f ||
                recipeTime != 12 ||
                nativeResult ||
                nativeState)
                throw new InvalidOperationException("A zero-demand retained callback mutated native state.");
        }

        private static void SharedRetainedCallbackClosuresAreProductAndOwnerScoped()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            const string parentOwner = "DTMAPI.Tests.Batch5.ProductClosure.Parent";
            const string childOwner = "DTMAPI.Tests.Batch5.ProductClosure.Child";
            ProductClosureCase[] cases =
            {
                new ProductClosureCase(GameBridgeDemandRoutes.ActionCompletion, GameBridgeDemandRoutes.ToolColliderShared, "action-completion-tool", "foreign-tool-family", GameBridgeRetainedCallbackDemand.ActionCompletion, GameBridgeRetainedCallbackDemand.ToolColliderShared, GameBridgeRetainedCallbackDemand.ActionCompletionToolColliderOwned),
                new ProductClosureCase(GameBridgeDemandRoutes.ActionCompletion, GameBridgeDemandRoutes.AgentStateInteractExitShared, "action-completion-interact-exit", "action-speed-interact-exit", GameBridgeRetainedCallbackDemand.ActionCompletion, GameBridgeRetainedCallbackDemand.AgentStateInteractExitShared, GameBridgeRetainedCallbackDemand.ActionCompletionInteractExitOwned),
                new ProductClosureCase(GameBridgeDemandRoutes.ActionSpeed, GameBridgeDemandRoutes.ActionSpeedToolStages, "tool-stages", "foreign-tool-stages", GameBridgeRetainedCallbackDemand.ActionSpeed, GameBridgeRetainedCallbackDemand.ActionSpeedToolStages, GameBridgeRetainedCallbackDemand.ActionSpeedToolStagesOwned),
                new ProductClosureCase(GameBridgeDemandRoutes.ActionSpeed, GameBridgeDemandRoutes.AgentStateToolExitShared, "action-speed-tool-exit", "foreign-tool-exit", GameBridgeRetainedCallbackDemand.ActionSpeed, GameBridgeRetainedCallbackDemand.AgentStateToolExitShared, GameBridgeRetainedCallbackDemand.ActionSpeedToolExitOwned),
                new ProductClosureCase(GameBridgeDemandRoutes.ActionSpeed, GameBridgeDemandRoutes.ActionSpeedInteractionStages, "interaction-stages", "foreign-interaction-stages", GameBridgeRetainedCallbackDemand.ActionSpeed, GameBridgeRetainedCallbackDemand.ActionSpeedInteractionStages, GameBridgeRetainedCallbackDemand.ActionSpeedInteractionStagesOwned),
                new ProductClosureCase(GameBridgeDemandRoutes.ActionSpeed, GameBridgeDemandRoutes.AgentStateInteractExitShared, "action-speed-interact-exit", "action-completion-interact-exit", GameBridgeRetainedCallbackDemand.ActionSpeed, GameBridgeRetainedCallbackDemand.AgentStateInteractExitShared, GameBridgeRetainedCallbackDemand.ActionSpeedInteractExitOwned),
                new ProductClosureCase(GameBridgeDemandRoutes.ActionSpeed, GameBridgeDemandRoutes.AgentStateBaseExitShared, "action-speed-base-exit", "foreign-base-exit", GameBridgeRetainedCallbackDemand.ActionSpeed, GameBridgeRetainedCallbackDemand.AgentStateBaseExitShared, GameBridgeRetainedCallbackDemand.ActionSpeedBaseExitOwned)
            };

            try
            {
                foreach (ProductClosureCase item in cases)
                {
                    SetTestDemand(runtime, item.ParentCapabilityId, parentOwner, "parent", true);
                    SetTestDemand(runtime, item.ChildCapabilityId, childOwner, item.ChildDemandKey, true);
                    Assert(bridge.HasRetainedCallbackDemand(item.ParentBit | item.ChildBit),
                        "The raw parent/child bits should expose the cross-owner adversarial setup for " + item.ClosureBit + ".");
                    Assert(!bridge.HasRetainedCallbackDemand(item.ClosureBit),
                        "A shared child owned by another owner must not activate " + item.ClosureBit + ".");

                    SetTestDemand(runtime, item.ChildCapabilityId, childOwner, item.ChildDemandKey, false);
                    SetTestDemand(runtime, item.ChildCapabilityId, parentOwner, item.ForeignDemandKey, true);
                    Assert(!bridge.HasRetainedCallbackDemand(item.ClosureBit),
                        "Another product family on the same owner must not activate " + item.ClosureBit + ".");

                    SetTestDemand(runtime, item.ChildCapabilityId, parentOwner, item.ForeignDemandKey, false);
                    SetTestDemand(runtime, item.ChildCapabilityId, parentOwner, item.ChildDemandKey, true);
                    Assert(bridge.HasRetainedCallbackDemand(item.ClosureBit),
                        "The exact product parent and child held by one owner must activate " + item.ClosureBit + ".");

                    SetTestDemand(runtime, item.ChildCapabilityId, parentOwner, item.ChildDemandKey, false);
                    SetTestDemand(runtime, item.ParentCapabilityId, parentOwner, "parent", false);
                    Assert(!bridge.HasRetainedCallbackDemand(item.ClosureBit),
                        "Releasing the exact product closure must clear " + item.ClosureBit + " synchronously.");
                }
            }
            finally
            {
                bridge.Shutdown("Batch5 product-owned retained callback closure test");
            }
        }

        private static void ConcurrentLastReleaseClearsRetainedMaskBeforeReturning()
        {
            string gamePath = Path.Combine(DtmApiTestSession.Current.RootPath, "batch5-gamebridge-" + (++fixtureSequence).ToString());
            Directory.CreateDirectory(Path.Combine(gamePath, "BepInEx", "plugins"));
            var runtime = new DtmApiRuntime(new FakeHost(gamePath), new ConfigMenuRegistry());
            using var blockingTransitionEntered = new ManualResetEvent(false);
            using var releaseBlockingTransition = new ManualResetEvent(false);
            const string blockerCapability = "DTMAPI.Tests.Batch5.BlockedTransition";
            runtime.DemandCoordinator.DemandTransitioned += transition =>
            {
                if (!transition.CapabilityId.Equals(blockerCapability, StringComparison.OrdinalIgnoreCase) || !transition.IsFirstDemand)
                    return;
                blockingTransitionEntered.Set();
                if (!releaseBlockingTransition.WaitOne(TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("The retained-mask concurrency test did not release its blocking transition.");
            };

            var bridge = new DolocTownGameBridge(runtime);
            const string owner = "DTMAPI.Tests.Batch5.ConcurrentClosure";
            Exception? blockerFailure = null;
            Exception? releaseFailure = null;
            var blocker = new Thread(() =>
            {
                try
                {
                    SetTestDemand(runtime, blockerCapability, owner, "blocker", true);
                }
                catch (Exception ex)
                {
                    blockerFailure = ex;
                }
            });
            var release = new Thread(() =>
            {
                try
                {
                    SetTestDemand(runtime, GameBridgeDemandRoutes.ToolColliderShared, owner, "action-completion-tool", false);
                }
                catch (Exception ex)
                {
                    releaseFailure = ex;
                }
            });

            try
            {
                SetTestDemand(runtime, GameBridgeDemandRoutes.ActionCompletion, owner, "parent", true);
                SetTestDemand(runtime, GameBridgeDemandRoutes.ToolColliderShared, owner, "action-completion-tool", true);
                Assert(bridge.HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ActionCompletionToolColliderOwned),
                    "The concurrency fixture must begin with one exact co-owned closure.");

                blocker.Start();
                Assert(blockingTransitionEntered.WaitOne(TimeSpan.FromSeconds(5)),
                    "The concurrency fixture must hold the ordered public transition drain open.");
                release.Start();
                Assert(release.Join(TimeSpan.FromSeconds(5)),
                    "A slow ordered transition listener must not turn the internal scalar last-release projection into a blocking public API boundary.");
                if (releaseFailure != null)
                    throw new InvalidOperationException("Concurrent retained-mask release failed.", releaseFailure);
                Assert(!bridge.HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ActionCompletionToolColliderOwned),
                    "The last shared-child Release must clear its product-owned retained bit before Release returns even while the public transition drain is busy.");
            }
            finally
            {
                releaseBlockingTransition.Set();
                if (blocker.ThreadState != ThreadState.Unstarted)
                    blocker.Join(TimeSpan.FromSeconds(5));
                if (release.ThreadState != ThreadState.Unstarted)
                    release.Join(TimeSpan.FromSeconds(5));
                bridge.Shutdown("Batch5 concurrent retained callback release test");
            }

            if (blockerFailure != null)
                throw new InvalidOperationException("Blocked transition worker failed.", blockerFailure);
        }

        private static void SetTestDemand(DtmApiRuntime runtime, string capabilityId, string ownerId, string demandKey, bool active)
        {
            runtime.DemandCoordinator.SetDemand(
                capabilityId,
                ownerId,
                RuntimeDemandSourceType.CapabilityRegistration,
                RuntimeDemandLifetime.Owner,
                demandKey,
                active ? 1 : 0,
                "Batch5 retained callback ownership test");
        }

        private readonly struct ProductClosureCase
        {
            internal ProductClosureCase(
                string parentCapabilityId,
                string childCapabilityId,
                string childDemandKey,
                string foreignDemandKey,
                GameBridgeRetainedCallbackDemand parentBit,
                GameBridgeRetainedCallbackDemand childBit,
                GameBridgeRetainedCallbackDemand closureBit)
            {
                ParentCapabilityId = parentCapabilityId;
                ChildCapabilityId = childCapabilityId;
                ChildDemandKey = childDemandKey;
                ForeignDemandKey = foreignDemandKey;
                ParentBit = parentBit;
                ChildBit = childBit;
                ClosureBit = closureBit;
            }

            internal string ParentCapabilityId { get; }
            internal string ChildCapabilityId { get; }
            internal string ChildDemandKey { get; }
            internal string ForeignDemandKey { get; }
            internal GameBridgeRetainedCallbackDemand ParentBit { get; }
            internal GameBridgeRetainedCallbackDemand ChildBit { get; }
            internal GameBridgeRetainedCallbackDemand ClosureBit { get; }
        }

        private readonly struct ValueTypeGridPoint
        {
            internal ValueTypeGridPoint(int x, int y)
            {
                X = x;
                Y = y;
            }

            internal int X { get; }
            internal int Y { get; }
        }

        private static void AutoFillOnlyUsesItsUpdaterWithoutNativeStageHooks()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            IManifest owner = Manifest("DTMAPI.Tests.Batch5.ActionSpeed.AutoFillOnly");
            try
            {
                bridge.ActionSpeedService!.Configure(owner, new ActionSpeedOptions
                {
                    Enabled = true,
                    AutoFillBottle = true
                });
                bridge.CommitPendingDemandRoutesForTests();

                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeed) == 0, "Auto-fill-only policy must not activate native ActionSpeed stage hooks.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeedAutoFill) == 1, "Auto-fill-only policy must activate its child updater.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateToolExitShared) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateInteractExitShared) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateBaseExitShared) == 0,
                    "Auto-fill-only policy must not acquire any AgentState exit hook.");
                Assert(bridge.GetActiveDemandUpdaterIdsForTests().Contains(GameBridgeDemandRoutes.ActionSpeedAutoFill, StringComparer.OrdinalIgnoreCase) &&
                    !bridge.GetActiveDemandUpdaterIdsForTests().Contains(GameBridgeDemandRoutes.ActionSpeed, StringComparer.OrdinalIgnoreCase),
                    "The dense updater snapshot must contain only the auto-fill child route.");

                bridge.RemoveGameBridgeOwnerResourcesForTests(owner.UniqueID, ModOwnerCleanupReason.Unload);
                bridge.CommitPendingDemandRoutesForTests();
                Assert(runtime.DemandCoordinator.GetOwnerDemandCount(owner.UniqueID) == 0, "Auto-fill owner cleanup must release its child updater demand.");
            }
            finally
            {
                bridge.Shutdown("Batch5 auto-fill-only closure test");
            }
        }

        private static void InteractionOnlyActionCompletionUsesOnlyInteractExit()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            IManifest owner = Manifest("DTMAPI.Tests.Batch5.ActionCompletion.InteractOnly");
            IManifest inert = Manifest("DTMAPI.Tests.Batch5.ActionCompletion.EnabledWithoutAction");
            try
            {
                bridge.ActionCompletionService!.Configure(inert, new ActionCompletionOptions
                {
                    Enabled = true,
                    CompleteTrees = false,
                    CompleteOres = false,
                    CompleteGarbage = false,
                    CompleteWeeds = false,
                    CompleteMachineFuel = false,
                    CompleteFeeder = false
                });
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionCompletion) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ToolColliderShared) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateInteractExitShared) == 0,
                    "Enabled ActionCompletion with no selected action family must publish zero parent and child callback demand.");
                bridge.RemoveGameBridgeOwnerResourcesForTests(inert.UniqueID, ModOwnerCleanupReason.Unload);

                bridge.ActionCompletionService!.Configure(owner, new ActionCompletionOptions
                {
                    Enabled = true,
                    CompleteMachineFuel = true
                });

                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionCompletion) == 1, "Interaction-only ActionCompletion must activate its main closure route.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateInteractExitShared) == 1, "Interaction-only ActionCompletion must acquire InteractExit.");
                Assert(bridge.HasRetainedCallbackDemand(
                    GameBridgeRetainedCallbackDemand.ActionCompletion |
                    GameBridgeRetainedCallbackDemand.AgentStateInteractExitShared),
                    "Retained callback scalar demand must publish the exact ActionCompletion + InteractExit closure synchronously.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateToolExitShared) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateBaseExitShared) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ToolColliderShared) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateLifecycleShared) == 0,
                    "Interaction-only ActionCompletion must not acquire ToolExit, BaseExit, ToolCollider, or the legacy aggregate route.");

                bridge.RemoveGameBridgeOwnerResourcesForTests(owner.UniqueID, ModOwnerCleanupReason.Unload);
                Assert(runtime.DemandCoordinator.GetOwnerDemandCount(owner.UniqueID) == 0, "Interaction-only ActionCompletion cleanup must release the exact dependency root.");
                Assert(!bridge.HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.ActionCompletion) &&
                    !bridge.HasRetainedCallbackDemand(GameBridgeRetainedCallbackDemand.AgentStateInteractExitShared),
                    "The last ActionCompletion owner release must clear retained callback scalar demand before the next frame boundary.");
            }
            finally
            {
                bridge.Shutdown("Batch5 ActionCompletion exact closure test");
            }
        }

        private static void ManagedOneActionOwnerFailsClosedInBothLoadOrders()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            bool managedProductPresent = true;
            var compatibility = new ActionCompletionService(runtime, () => managedProductPresent);
            IManifest productFirstOwner = Manifest("DTMAPI.Tests.Batch6.OneAction.ProductFirst");
            IManifest compatibilityFirstOwner = Manifest("DTMAPI.Tests.Batch6.OneAction.CompatibilityFirst");
            IManifest unrelatedSharedOwner = Manifest("DTMAPI.Tests.Batch6.ActionSpeed.SharedInteract");
            var options = new ActionCompletionOptions
            {
                Enabled = true,
                CompleteTrees = true,
                CompleteMachineFuel = true
            };

            try
            {
                bool rejected = false;
                try
                {
                    compatibility.Configure(productFirstOwner, options);
                }
                catch (InvalidOperationException)
                {
                    rejected = true;
                }

                Assert(rejected, "Product-first then Compatibility-request must fail closed.");
                Assert(compatibility.CountOwnerResources(productFirstOwner.UniqueID) == 0 &&
                    runtime.DemandCoordinator.GetOwnerDemandCount(productFirstOwner.UniqueID) == 0,
                    "Rejected product-first Compatibility demand must store neither policy nor parent/child demand.");

                managedProductPresent = false;
                compatibility.Configure(compatibilityFirstOwner, options);
                SetTestDemand(runtime, GameBridgeDemandRoutes.AgentStateInteractExitShared, unrelatedSharedOwner.UniqueID, "action-speed-interact-exit", true);
                Assert(compatibility.CountOwnerResources(compatibilityFirstOwner.UniqueID) == 1 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionCompletion) == 1 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ToolColliderShared) == 1 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateInteractExitShared) == 2,
                    "Compatibility-first fixture must begin with one complete pending ActionCompletion closure plus one unrelated shared consumer.");

                managedProductPresent = true;
                Assert(compatibility.ReconcileManagedProductOwnerBeforeHookInstall() == 1,
                    "Install-boundary reconciliation must remove the pending Compatibility owner when the product appears second.");
                Assert(compatibility.CountOwnerResources(compatibilityFirstOwner.UniqueID) == 0 &&
                    runtime.DemandCoordinator.GetOwnerDemandCount(compatibilityFirstOwner.UniqueID) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionCompletion) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ToolColliderShared) == 0,
                    "Compatibility-first then product must synchronously clear its policy, parent demand and ToolCollider child before Hook installation.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateInteractExitShared) == 1 &&
                    runtime.DemandCoordinator.GetOwnerDemandCount(unrelatedSharedOwner.UniqueID) == 1,
                    "ActionCompletion reconciliation must preserve an unrelated ActionSpeed-style shared InteractExit demand.");
            }
            finally
            {
                SetTestDemand(runtime, GameBridgeDemandRoutes.AgentStateInteractExitShared, unrelatedSharedOwner.UniqueID, "action-speed-interact-exit", false);
                bridge.Shutdown("Batch6 OneActionComplete bidirectional owner-order test");
            }
        }

        private static void ManagedActionSpeedOwnerFailsClosedInBothLoadOrders()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            bool managedProductPresent = true;
            var compatibility = new ActionSpeedService(runtime, () => managedProductPresent);
            IManifest productFirstOwner = Manifest("DTMAPI.Tests.Batch6.ActionSpeed.ProductFirst");
            IManifest compatibilityFirstOwner = Manifest("DTMAPI.Tests.Batch6.ActionSpeed.CompatibilityFirst");
            IManifest productFirstAutoFillOwner = Manifest("DTMAPI.Tests.Batch6.ActionSpeed.ProductFirstAutoFill");
            IManifest compatibilityFirstAutoFillOwner = Manifest("DTMAPI.Tests.Batch6.ActionSpeed.CompatibilityFirstAutoFill");
            IManifest unrelatedSharedOwner = Manifest("DTMAPI.Tests.Batch6.OneAction.SharedInteract");
            var options = new ActionSpeedOptions
            {
                Enabled = true,
                ToolSpeedEnabled = true,
                ToolMultiplier = 3,
                BottleFillSpeedEnabled = true,
                BottleFillMultiplier = 3,
                EatDrinkSpeedEnabled = true,
                EatDrinkMultiplier = 3,
                AutoFillBottle = true
            };

            try
            {
                bool rejected = false;
                try
                {
                    compatibility.Configure(productFirstOwner, options);
                }
                catch (InvalidOperationException)
                {
                    rejected = true;
                }

                Assert(rejected, "ActionSpeed product-first then Compatibility-request must fail closed.");
                Assert(compatibility.CountOwnerResources(productFirstOwner.UniqueID) == 0 &&
                    runtime.DemandCoordinator.GetOwnerDemandCount(productFirstOwner.UniqueID) == 0,
                    "Rejected ActionSpeed product-first Compatibility demand must store neither policy nor parent/child demand.");

                managedProductPresent = false;
                compatibility.Configure(compatibilityFirstOwner, options);
                SetTestDemand(runtime, GameBridgeDemandRoutes.AgentStateInteractExitShared, unrelatedSharedOwner.UniqueID, "one-action-interact-exit", true);
                Assert(compatibility.CountOwnerResources(compatibilityFirstOwner.UniqueID) == 1 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeed) == 1 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeedToolStages) == 1 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeedInteractionStages) == 1 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeedAutoFill) == 1 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateToolExitShared) == 1 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateInteractExitShared) == 2 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateBaseExitShared) == 1,
                    "ActionSpeed Compatibility-first fixture must begin with one complete parent/child closure plus one unrelated shared consumer.");

                managedProductPresent = true;
                Assert(compatibility.ReconcileManagedProductOwnerBeforeHookInstall() == 1,
                    "ActionSpeed install-boundary reconciliation must remove the pending Compatibility owner when the product appears second.");
                Assert(compatibility.CountOwnerResources(compatibilityFirstOwner.UniqueID) == 0 &&
                    runtime.DemandCoordinator.GetOwnerDemandCount(compatibilityFirstOwner.UniqueID) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeed) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeedToolStages) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeedInteractionStages) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeedAutoFill) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateToolExitShared) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateBaseExitShared) == 0,
                    "ActionSpeed reconciliation must synchronously clear its policy and every product-specific/shared child demand before Hook installation.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateInteractExitShared) == 1 &&
                    runtime.DemandCoordinator.GetOwnerDemandCount(unrelatedSharedOwner.UniqueID) == 1,
                    "ActionSpeed reconciliation must preserve an unrelated OneActionComplete-style shared InteractExit demand.");

                var autoFillOnly = new ActionSpeedOptions
                {
                    Enabled = true,
                    ToolSpeedEnabled = false,
                    BottleFillSpeedEnabled = false,
                    EatDrinkSpeedEnabled = false,
                    MachineAddSpeedEnabled = false,
                    HarvestSpeedEnabled = false,
                    PlantSpeedEnabled = false,
                    ContinuousDrinkWithRightClick = false,
                    AutoFillBottle = true
                };
                bool autoFillRejected = false;
                try
                {
                    compatibility.Configure(productFirstAutoFillOwner, autoFillOnly);
                }
                catch (InvalidOperationException)
                {
                    autoFillRejected = true;
                }
                Assert(autoFillRejected && compatibility.CountOwnerResources(productFirstAutoFillOwner.UniqueID) == 0 &&
                    runtime.DemandCoordinator.GetOwnerDemandCount(productFirstAutoFillOwner.UniqueID) == 0,
                    "ActionSpeed product-first then AutoFill-only Compatibility request must fail closed without retaining policy or updater demand.");

                managedProductPresent = false;
                compatibility.Configure(compatibilityFirstAutoFillOwner, autoFillOnly);
                Assert(compatibility.CountOwnerResources(compatibilityFirstAutoFillOwner.UniqueID) == 1 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeed) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeedAutoFill) == 1 &&
                    runtime.DemandCoordinator.GetOwnerDemandCount(compatibilityFirstAutoFillOwner.UniqueID) == 1,
                    "AutoFill-only Compatibility-first demand must activate only its updater and retain no Hook route.");

                managedProductPresent = true;
                compatibility.Update();
                Assert(compatibility.CountOwnerResources(compatibilityFirstAutoFillOwner.UniqueID) == 0 &&
                    runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeedAutoFill) == 0 &&
                    runtime.DemandCoordinator.GetOwnerDemandCount(compatibilityFirstAutoFillOwner.UniqueID) == 0,
                    "ActionSpeed Compatibility-first AutoFill-only demand must reconcile at the updater boundary before processing the product-owned frame.");
            }
            finally
            {
                SetTestDemand(runtime, GameBridgeDemandRoutes.AgentStateInteractExitShared, unrelatedSharedOwner.UniqueID, "one-action-interact-exit", false);
                bridge.Shutdown("Batch6 ActionSpeed bidirectional owner-order test");
            }
        }

        private static void ActionSpeedNativeStageGroupsUseExactExitClosures()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            IManifest tool = Manifest("DTMAPI.Tests.Batch5.ActionSpeed.ToolOnly");
            IManifest interaction = Manifest("DTMAPI.Tests.Batch5.ActionSpeed.InteractionOnly");
            IManifest eat = Manifest("DTMAPI.Tests.Batch5.ActionSpeed.EatOnly");
            try
            {
                bridge.SetCoreHookReadinessOverrideForTests(true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.NativeUiLayoutDiagnostics, () => true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.ActionSpeed, () => true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.ActionSpeedToolStages, () => true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.AgentStateToolExitShared, () => true);
                bridge.ActionSpeedService!.Configure(tool, new ActionSpeedOptions { Enabled = true, ToolSpeedEnabled = true });
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeedToolStages) == 1 && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateToolExitShared) == 1, "Tool-only ActionSpeed must acquire ToolEnter/ToolExit.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeedInteractionStages) == 0 && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateInteractExitShared) == 0 && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateBaseExitShared) == 0, "Tool-only ActionSpeed must not acquire interaction hooks, InteractExit, or BaseExit.");
                bridge.CommitPendingDemandRoutesForTests();
                bridge.RemoveGameBridgeOwnerResourcesForTests(tool.UniqueID, ModOwnerCleanupReason.Unload);
                bridge.CommitPendingDemandRoutesForTests();
                Assert(GetRoute(runtime, GameBridgeDemandRoutes.ActionSpeedToolStages).LifecycleState == RuntimeCapabilityLifecycleState.ProcessPinnedDormant && GetRoute(runtime, GameBridgeDemandRoutes.AgentStateToolExitShared).LifecycleState == RuntimeCapabilityLifecycleState.ProcessPinnedDormant, "Releasing an installed tool-only group must report ProcessPinnedDormant for exactly the installed group.");

                bridge.ActionSpeedService.Configure(interaction, new ActionSpeedOptions { Enabled = true, BottleFillSpeedEnabled = true });
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeedInteractionStages) == 1 && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateInteractExitShared) == 1, "Bottle interaction-only ActionSpeed must acquire the interaction group and InteractExit.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeedToolStages) == 0 && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateToolExitShared) == 0 && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateBaseExitShared) == 0, "Bottle interaction-only ActionSpeed must not acquire ToolEnter, ToolExit, or BaseExit.");
                bridge.RemoveGameBridgeOwnerResourcesForTests(interaction.UniqueID, ModOwnerCleanupReason.Unload);

                bridge.ActionSpeedService.Configure(eat, new ActionSpeedOptions { Enabled = true, EatDrinkSpeedEnabled = true });
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.ActionSpeedInteractionStages) == 1 && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateBaseExitShared) == 1, "EatDrink-only ActionSpeed must acquire the interaction group and BaseExit restore fallback.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateToolExitShared) == 0 && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AgentStateInteractExitShared) == 0, "EatDrink-only ActionSpeed must not acquire ToolExit or InteractExit.");
                bridge.RemoveGameBridgeOwnerResourcesForTests(eat.UniqueID, ModOwnerCleanupReason.Unload);
            }
            finally
            {
                bridge.Shutdown("Batch5 ActionSpeed exact stage closure test");
            }
        }

        private static void OneHookConsumerActivatesAndOwnerCleanupReleasesOnlyItsRoute()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            IManifest owner = Manifest("DTMAPI.Tests.Batch5.FishRoe");
            try
            {
                bridge.FishRoeTooltipService!.ConfigureFishRoeProvider(owner, new FishRoeTooltipOptions { Enabled = true }, _ => null);
                bridge.CommitPendingDemandRoutesForTests();
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.FishRoeTooltip) == 1, "One enabled provider must create one FishRoe demand.");
                Assert(bridge.OptionalHookInstallRequestCountForTests == 1, "The first FishRoe consumer must request exactly one optional hook-install pass.");
                Assert(!bridge.GetActiveDemandUpdaterIdsForTests().Contains(GameBridgeDemandRoutes.FishRoeTooltip, StringComparer.OrdinalIgnoreCase), "Hook-only FishRoe demand must not enter the updater snapshot.");
                Assert(GameBridgeDemandRoutes.Catalog.Where(item => item.Outcome != RuntimeCapabilityOutcome.Mandatory).All(item => item.CapabilityId == GameBridgeDemandRoutes.FishRoeTooltip || runtime.DemandCoordinator.GetDemandCount(item.CapabilityId) == 0), "One FishRoe consumer must not activate unrelated optional routes.");

                ModOwnerCleanupParticipantResult cleanup = bridge.RemoveGameBridgeOwnerResourcesForTests(owner.UniqueID, ModOwnerCleanupReason.Unload);
                bridge.CommitPendingDemandRoutesForTests();
                Assert(cleanup.FailureCount == 0 && cleanup.RemainingResources == 0, "Unified GameBridge owner cleanup must leave no FishRoe roots.");
                Assert(runtime.DemandCoordinator.GetOwnerDemandCount(owner.UniqueID) == 0 && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.FishRoeTooltip) == 0, "Owner cleanup must release the final logical demand.");
                RuntimeCapabilityDemandSnapshot route = runtime.DemandCoordinator.GetSnapshot().Capabilities.Single(item => item.CapabilityId == GameBridgeDemandRoutes.FishRoeTooltip);
                Assert(route.LifecycleState == RuntimeCapabilityLifecycleState.Stopped && route.PatchState == RuntimeCapabilityPatchState.Removed, "A route released before physical installation must report Stopped/Removed, not a fake restart requirement.");
            }
            finally
            {
                bridge.Shutdown("Batch5 one-consumer test");
            }
        }

        private static void ManagedFishBreedingOwnerFailsClosedInBothLoadOrders()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            IManifest oldConsumer = Manifest("DTMAPI.Tests.Batch6.FishRoe.OldConsumer");
            IManifest unrelated = Manifest("DTMAPI.Tests.Batch6.FishRoe.Unrelated");
            bool managedProductPresent = true;
            var service = new FishRoeTooltipService(runtime, () => managedProductPresent);
            try
            {
                bool rejected = false;
                try
                {
                    service.ConfigureFishRoeProvider(oldConsumer, new FishRoeTooltipOptions { Enabled = true, LabelFishRoeTitle = true }, _ => null);
                }
                catch (InvalidOperationException ex)
                {
                    rejected = ex.Message.Contains("Yuuka.DTMAPI.FishBreedingAssistant", StringComparison.Ordinal);
                }
                Assert(rejected, "Product-first then IItemTooltipApi compatibility request must fail closed before retaining provider state.");
                Assert(service.CountOwnerResources(oldConsumer.UniqueID) == 0 && runtime.DemandCoordinator.GetOwnerDemandCount(oldConsumer.UniqueID) == 0, "Rejected product-first FishRoe compatibility must retain no provider or demand root.");

                managedProductPresent = false;
                service.ConfigureFishRoeProvider(oldConsumer, new FishRoeTooltipOptions { Enabled = true, LabelFishRoeTitle = true }, _ => null);
                SetTestDemand(runtime, GameBridgeDemandRoutes.Camera, unrelated.UniqueID, "unrelated-fish-owner-order", true);
                Assert(service.CountOwnerResources(oldConsumer.UniqueID) == 2 && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.FishRoeTooltip) == 1, "Compatibility-first setup must retain its options, lookup, and one route demand before product ownership appears.");

                managedProductPresent = true;
                int removed = service.ReconcileManagedProductOwnerBeforeHookInstall();
                Assert(removed == 2 && service.CountOwnerResources(oldConsumer.UniqueID) == 0, "Compatibility-first then product must synchronously clear pending FishRoe provider state before Hook installation.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.FishRoeTooltip) == 0 && runtime.DemandCoordinator.GetOwnerDemandCount(oldConsumer.UniqueID) == 0, "Compatibility-first reconciliation must release all old-consumer FishRoe demand.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.Camera) == 1 && runtime.DemandCoordinator.GetOwnerDemandCount(unrelated.UniqueID) == 1, "FishRoe reconciliation must preserve unrelated owner demand.");
            }
            finally
            {
                SetTestDemand(runtime, GameBridgeDemandRoutes.Camera, unrelated.UniqueID, "unrelated-fish-owner-order", false);
                bridge.Shutdown("Batch6 FishBreeding bidirectional owner-order test");
            }
        }

        private static void ManagedAnimalHusbandryOwnerFailsClosedInBothLoadOrders()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            IManifest oldConsumer = Manifest("DTMAPI.Tests.Batch6.AnimalViewer.OldConsumer");
            IManifest unrelated = Manifest("DTMAPI.Tests.Batch6.AnimalViewer.Unrelated");
            bool managedProductPresent = true;
            var service = new AnimalViewerService(runtime, () => managedProductPresent);
            var options = new AnimalHusbandryProgressOptions { Enabled = true };
            try
            {
                bool rejected = false;
                try
                {
                    service.ConfigureSpecialProduceProgress(oldConsumer, options);
                }
                catch (InvalidOperationException ex)
                {
                    rejected = ex.Message.Contains("Yuuka.DTMAPI.AnimalHusbandryProgress", StringComparison.Ordinal);
                }
                Assert(rejected, "Product-first then IAnimalViewerApi compatibility request must fail closed before retaining policy state.");
                Assert(service.CountOwnerResources(oldConsumer.UniqueID) == 0 && runtime.DemandCoordinator.GetOwnerDemandCount(oldConsumer.UniqueID) == 0,
                    "Rejected product-first AnimalViewer compatibility must retain no policy or demand root.");

                managedProductPresent = false;
                service.ConfigureSpecialProduceProgress(oldConsumer, options);
                SetTestDemand(runtime, GameBridgeDemandRoutes.Camera, unrelated.UniqueID, "unrelated-animal-owner-order", true);
                Assert(service.CountOwnerResources(oldConsumer.UniqueID) == 1 && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AnimalViewer) == 1,
                    "Compatibility-first setup must retain one old AnimalViewer policy and route demand before product ownership appears.");

                managedProductPresent = true;
                int removed = service.ReconcileManagedProductOwnerBeforeHookInstall();
                Assert(removed == 1 && service.CountOwnerResources(oldConsumer.UniqueID) == 0,
                    "Compatibility-first then product must synchronously clear pending AnimalViewer policy state before Hook installation.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AnimalViewer) == 0 && runtime.DemandCoordinator.GetOwnerDemandCount(oldConsumer.UniqueID) == 0,
                    "Compatibility-first reconciliation must release all old-consumer AnimalViewer demand.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.Camera) == 1 && runtime.DemandCoordinator.GetOwnerDemandCount(unrelated.UniqueID) == 1,
                    "AnimalViewer reconciliation must preserve unrelated owner demand.");
            }
            finally
            {
                SetTestDemand(runtime, GameBridgeDemandRoutes.Camera, unrelated.UniqueID, "unrelated-animal-owner-order", false);
                bridge.Shutdown("Batch6 AnimalHusbandry bidirectional owner-order test");
            }
        }

        private static void OwnerCleanupScopesDemandAndCountsIndependentRootsAuthoritatively()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            IManifest owner = Manifest("DTMAPI.Tests.Batch5.ScopedOwnerCleanup");
            const string eventCapability = "Event.GameLoop.UpdateTicked";
            try
            {
                bridge.CameraFeatureForQa!.HookBridge
                    .SetCompatibilityOwnerClaimOverrideForTests(() => true);
                IModRegistry registry = runtime.ModRegistry.CreateOwnerBoundRegistry(owner, () => { });
                ICameraViewApi camera = registry.GetApi<ICameraViewApi>("DTMAPI.GameBridge.DolocTown")
                    ?? throw new InvalidOperationException("Camera View API should be registered by GameBridge.");
                ICameraViewLease lease = camera.AcquireLease(owner, new CameraViewRequest
                {
                    Enabled = true,
                    ViewScale = 1.5,
                    MinViewScale = 1,
                    MaxViewScale = 4,
                    Step = 0.25,
                    LeaseName = "Batch5 scoped owner cleanup"
                });
                IEventsHelper events = runtime.Events.CreateProxy(owner.UniqueID);
                EventHandler<UpdateTickedEventArgs> handler = (_, _) => { };
                events.GameLoop.UpdateTicked += handler;

                Assert(runtime.DemandCoordinator.GetOwnerDemandCount(owner.UniqueID) == 2, "The complete coordinator total must include Camera and Event demand roots.");
                Assert(bridge.CountGameBridgeOwnerResources(owner.UniqueID) == 2, "GameBridge accounting must count the Camera lease plus its demand as two independent roots while excluding Event demand.");

                ModOwnerCleanupParticipantResult result = bridge.RemoveGameBridgeOwnerResourcesForTests(owner.UniqueID, ModOwnerCleanupReason.Unload);
                Assert(result.RemovedResources == 2 && result.RemainingResources == 0 && result.FailureCount == 0, "GameBridge cleanup must authoritatively report the removed Camera lease plus Camera demand and zero GameBridge roots remaining. removed=" + result.RemovedResources + " remaining=" + result.RemainingResources + " failures=" + result.FailureCount + " details=" + result.Details + ".");
                Assert(lease.IsReleased && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.Camera) == 0, "GameBridge cleanup must physically release the Camera lease and its logical demand.");
                Assert(runtime.DemandCoordinator.GetDemandCount(eventCapability) == 1 && runtime.DemandCoordinator.GetOwnerDemandCount(owner.UniqueID) == 1, "GameBridge cleanup must not remove or count the same owner's Event demand.");

                Assert(runtime.Events.RemoveOwner(owner.UniqueID) == 1, "Core Event cleanup must independently remove the preserved event handler later.");
                Assert(runtime.DemandCoordinator.GetOwnerDemandCount(owner.UniqueID) == 0 && !runtime.DemandCoordinator.HasDemand(eventCapability), "Core Event cleanup must publish the final Event demand release.");
            }
            finally
            {
                bridge.Shutdown("Batch5 scoped owner cleanup accounting test");
            }
        }

        private static void OptionalHookRetryLivesUntilSuccessOrRelease()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            IManifest owner = Manifest("DTMAPI.Tests.Batch5.Retry.Success");
            bool installed = false;
            try
            {
                bridge.SetCoreHookReadinessOverrideForTests(true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.NativeUiLayoutDiagnostics, () => true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.FishRoeTooltip, () => installed);
                bridge.FishRoeTooltipService!.ConfigureFishRoeProvider(owner, new FishRoeTooltipOptions { Enabled = true }, _ => null);
                bridge.CommitPendingDemandRoutesForTests();
                Assert(bridge.HookRetrySourcesAliveForTests, "A failed demanded optional hook must keep the bounded retry sources alive even when Core is ready.");

                bridge.UpdateRuntimeAutomation();
                RuntimeCapabilityDemandSnapshot activating = GetRoute(runtime, GameBridgeDemandRoutes.FishRoeTooltip);
                Assert(activating.LifecycleState == RuntimeCapabilityLifecycleState.Activating, "The failed optional route must remain Activating.");

                installed = true;
                bridge.RequestHookInstallForTests("RetryTimer:unit-success");
                bridge.UpdateRuntimeAutomation();
                RuntimeCapabilityDemandSnapshot active = GetRoute(runtime, GameBridgeDemandRoutes.FishRoeTooltip);
                Assert(active.LifecycleState == RuntimeCapabilityLifecycleState.Active && active.PatchState == RuntimeCapabilityPatchState.Installed, "The next demanded retry must publish Active/Installed when the target becomes available.");
                Assert(!bridge.HookRetrySourcesAliveForTests, "Retry sources must stop after Core and every demanded optional hook are ready.");
            }
            finally
            {
                bridge.Shutdown("Batch5 optional retry success test");
            }

            CreateFixture(out runtime, out bridge);
            owner = Manifest("DTMAPI.Tests.Batch5.Retry.Release");
            try
            {
                bridge.SetCoreHookReadinessOverrideForTests(true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.NativeUiLayoutDiagnostics, () => true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.FishRoeTooltip, () => false);
                bridge.FishRoeTooltipService!.ConfigureFishRoeProvider(owner, new FishRoeTooltipOptions { Enabled = true }, _ => null);
                bridge.CommitPendingDemandRoutesForTests();
                Assert(bridge.HookRetrySourcesAliveForTests, "An unavailable demanded route must start retry sources.");

                bridge.RemoveGameBridgeOwnerResourcesForTests(owner.UniqueID, ModOwnerCleanupReason.Unload);
                bridge.CommitPendingDemandRoutesForTests();
                Assert(!bridge.HookRetrySourcesAliveForTests, "Releasing the final unavailable consumer must stop retry sources without waiting for the target.");
            }
            finally
            {
                bridge.Shutdown("Batch5 optional retry release test");
            }
        }

        private static void DynamicAudioRegistrationPublishesAndReleasesDemand()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            IManifest owner = Manifest("DTMAPI.Tests.Batch5.Audio");
            IManifest second = Manifest("DTMAPI.Tests.Batch5.Audio.Second");
            try
            {
                bridge.AudioReplacementService!.RegisterReplacement(owner, new AudioReplacementOptions
                {
                    Enabled = true,
                    ReplacementId = "paper-box",
                    NativeSoundEvent = "PLAY_RESOURCE_PAPER_BOX",
                    AudioPath = Path.Combine(DtmApiTestSession.Current.RootPath, "missing-batch5-audio.wav"),
                    SuppressNativeWhenReady = false
                });
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AudioReplacement) == 1, "Dynamic code-Mod audio registration must publish primary hook demand without waiting for a content generation.");

                bridge.AudioReplacementService.RegisterReplacement(second, new AudioReplacementOptions
                {
                    Enabled = true,
                    ReplacementId = "paper-box-second",
                    NativeSoundEvent = "PLAY_RESOURCE_PAPER_BOX",
                    AudioPath = Path.Combine(DtmApiTestSession.Current.RootPath, "missing-batch5-audio-second.wav"),
                    SuppressNativeWhenReady = false
                });
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AudioReplacement) == 2, "Two dynamic owners must publish two owner-attributed roots, without a duplicate Framework aggregate root.");

                ModOwnerCleanupParticipantResult cleanup = bridge.RemoveGameBridgeOwnerResourcesForTests(owner.UniqueID, ModOwnerCleanupReason.Unload);
                Assert(cleanup.FailureCount == 0 && runtime.DemandCoordinator.GetOwnerDemandCount(owner.UniqueID) == 0 && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AudioReplacement) == 1, "Removing one Audio owner must preserve the other owner's root.");
                cleanup = bridge.RemoveGameBridgeOwnerResourcesForTests(second.UniqueID, ModOwnerCleanupReason.Unload);
                Assert(cleanup.FailureCount == 0 && runtime.DemandCoordinator.GetOwnerDemandCount(second.UniqueID) == 0 && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AudioReplacement) == 0, "Removing the final Audio owner must release the route.");
            }
            finally
            {
                bridge.Shutdown("Batch5 dynamic audio demand test");
            }
        }

        private static void AudioDefinitionClosureDistinguishesSimpleAndAnimalVoice()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            const string simpleOwner = "DTMAPI.Tests.Batch5.Audio.SimpleClosure";
            const string animalOwner = "DTMAPI.Tests.Batch5.Audio.AnimalClosure";
            bool wwiseInstalled = false;
            bool animalContextInstalled = false;
            try
            {
                bridge.SetCoreHookReadinessOverrideForTests(true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.NativeUiLayoutDiagnostics, () => true);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.AudioReplacement, () => wwiseInstalled);
                bridge.OverrideDemandRoutePatchProbeForTests(GameBridgeDemandRoutes.AudioReplacementAnimalVoiceContext, () => animalContextInstalled);

                SetAudioDefinitionDemand(runtime, simpleOwner, active: true, animalVoice: false);
                bridge.CommitPendingDemandRoutesForTests();
                Assert(GetRoute(runtime, GameBridgeDemandRoutes.AudioReplacement).LifecycleState == RuntimeCapabilityLifecycleState.Activating, "SimpleSfx must wait only for Wwise on its first failed pass.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AudioReplacementAnimalVoiceContext) == 0, "SimpleSfx must not activate Animal.PlayAnimalSound context hooks.");

                wwiseInstalled = true;
                bridge.RequestHookInstallForTests("RetryTimer:simple-wwise-ready");
                bridge.UpdateRuntimeAutomation();
                Assert(GetRoute(runtime, GameBridgeDemandRoutes.AudioReplacement).LifecycleState == RuntimeCapabilityLifecycleState.Active, "SimpleSfx must recover as soon as Wwise becomes available.");

                SetAudioDefinitionDemand(runtime, animalOwner, active: true, animalVoice: true);
                bridge.CommitPendingDemandRoutesForTests();
                Assert(GetRoute(runtime, GameBridgeDemandRoutes.AudioReplacement).LifecycleState == RuntimeCapabilityLifecycleState.Active, "Adding AnimalVoice must not invalidate the already-ready Wwise route.");
                Assert(GetRoute(runtime, GameBridgeDemandRoutes.AudioReplacementAnimalVoiceContext).LifecycleState == RuntimeCapabilityLifecycleState.Activating, "AnimalVoice must remain Activating until both context hooks are available.");

                animalContextInstalled = true;
                bridge.RequestHookInstallForTests("RetryTimer:animal-context-ready");
                bridge.UpdateRuntimeAutomation();
                Assert(GetRoute(runtime, GameBridgeDemandRoutes.AudioReplacementAnimalVoiceContext).LifecycleState == RuntimeCapabilityLifecycleState.Active, "AnimalVoice must recover after its prefix/postfix context closure becomes ready.");

                SetAudioDefinitionDemand(runtime, simpleOwner, active: false, animalVoice: false);
                SetAudioDefinitionDemand(runtime, animalOwner, active: false, animalVoice: true);
                bridge.CommitPendingDemandRoutesForTests();
            }
            finally
            {
                bridge.Shutdown("Batch5 Audio dynamic closure test");
            }
        }

        private static void AudioContentDemandsAreOwnerAttributedAndRetainLastGoodGeneration()
        {
            CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge);
            const string simpleOwner = "DTMAPI.Tests.Batch5.Audio.Content.Simple";
            const string animalOwner = "DTMAPI.Tests.Batch5.Audio.Content.Animal";
            var service = new AudioReplacementService(runtime, (_, __) => new object());
            string root = Path.Combine(DtmApiTestSession.Current.RootPath, "batch5-audio-generations-" + (++fixtureSequence).ToString());
            string simpleRoot = CreateAudioPack(root, "simple", "[{ \"id\": \"simple\", \"category\": \"SimpleSfx\", \"nativeSoundEvent\": \"PLAY_RESOURCE_PAPER_BOX\", \"file\": \"Content/Audio/test.wav\" }]");
            string animalRoot = CreateAudioPack(root, "animal", "[{ \"id\": \"animal\", \"category\": \"AnimalVoice\", \"speciesId\": \"chicken\", \"stage\": \"adult\", \"nativeSoundEvent\": \"PLAY_ANIMAL_PET_CHICKEN\", \"file\": \"Content/Audio/test.wav\" }]");
            try
            {
                Assert(service.ReloadContentPackOwner(simpleOwner, simpleRoot, "unit simple").Success, "Simple content generation must commit.");
                Assert(service.ReloadContentPackOwner(animalOwner, animalRoot, "unit animal").Success, "AnimalVoice content generation must commit.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AudioReplacement) == 2, "Committed content definitions must be attributed to their two owners.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AudioReplacementAnimalVoiceContext) == 1, "Only the AnimalVoice owner must acquire the context closure.");

                File.WriteAllText(Path.Combine(animalRoot, "Content", "DTMAPI", "audio-replacements.json"), "[{ invalid-json ]");
                AudioReplacementService.AudioReplacementOwnerReloadResult rejected = service.ReloadContentPackOwner(animalOwner, animalRoot, "unit reject");
                Assert(!rejected.Success && rejected.RetainedPreviousGeneration, "A rejected AnimalVoice generation must retain its last-good generation.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AudioReplacement) == 2 && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AudioReplacementAnimalVoiceContext) == 1, "Rejected replacement must retain both owner demand and its AnimalVoice dependency closure.");

                service.RemoveOwner(simpleOwner, "unit remove first");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AudioReplacement) == 1, "Removing the first content owner must preserve the last owner.");
                service.RemoveOwner(animalOwner, "unit remove last");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AudioReplacement) == 0 && runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.AudioReplacementAnimalVoiceContext) == 0, "Removing the last content owner must release both Audio routes.");
            }
            finally
            {
                bridge.Shutdown("Batch5 Audio owner generation demand test");
            }
        }

        private static void CustomAnimalPostCommitFaultReconcilesAddedDefinitionDemand()
        {
            CreateCustomAnimalDemandFixture(
                "AddedDemandFault",
                "DTMAPI.Tests.Batch5.CustomAnimals.AddedDemandFault",
                "species_added_fault",
                "anim_added_fault",
                out DtmApiRuntime runtime,
                out CustomAnimalAnimatorBridgeService service,
                out string ownerId,
                out _);
            try
            {
                service.PostCommitFaultForTest = _ => throw new InvalidOperationException("injected CustomAnimals post-commit demand fault");
                bool threw = false;
                try
                {
                    service.RefreshDefinitions("unit-custom-animal-added-demand-fault", force: false);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("post-commit demand fault", StringComparison.Ordinal))
                {
                    threw = true;
                }
                finally
                {
                    service.PostCommitFaultForTest = null;
                }

                ContentRefreshReceipt receipt = runtime.ContentRefreshGenerations.GetReceipts().Last(item => item.Domain == ContentRefreshDomains.CustomAnimals && item.OwnerId == ownerId);
                Assert(threw && receipt.Status == ContentRefreshCompletionStatus.Committed && service.RegisteredKeySummary.Contains("anim_added_fault", StringComparison.Ordinal), "The injected post-commit fault must propagate only after the definition snapshot and terminal receipt commit.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.CustomAnimalAnimatorBridge) == 1 && runtime.DemandCoordinator.GetOwnerDemandCount(ownerId) == 1, "A zero-to-definition commit must reconcile exactly one owner-attributed CustomAnimals demand even when post-commit hygiene throws.");
                Assert(!runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.CustomAnimals), "A committed CustomAnimals generation must not be requeued after the post-commit fault.");
            }
            finally
            {
                runtime.NotifyRuntimeShutdown("Batch5 CustomAnimals added demand post-commit fault test");
            }
        }

        private static void CustomAnimalPostCommitFaultReconcilesRemovedDefinitionDemand()
        {
            CreateCustomAnimalDemandFixture(
                "RemovedDemandFault",
                "DTMAPI.Tests.Batch5.CustomAnimals.RemovedDemandFault",
                "species_removed_fault",
                "anim_removed_fault",
                out DtmApiRuntime runtime,
                out CustomAnimalAnimatorBridgeService service,
                out string ownerId,
                out _);
            try
            {
                Assert(service.RefreshDefinitions("unit-custom-animal-removed-demand-initial", force: false), "The initial CustomAnimals definition generation must commit before removal.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.CustomAnimalAnimatorBridge) == 1 && runtime.DemandCoordinator.GetOwnerDemandCount(ownerId) == 1, "The initial definition must own one CustomAnimals demand root.");

                DiscoveredMod loadedOwner = runtime.LoadedMods.Single(mod => mod.Manifest.UniqueID.Equals(ownerId, StringComparison.OrdinalIgnoreCase));
                loadedOwner.Manifest.Type = "CodeMod";
                runtime.ContentRefreshGenerations.MarkDirty(ContentRefreshDomains.CustomAnimals, new[] { ownerId }, "unit-custom-animal-removed-demand-fault");
                service.PostCommitFaultForTest = _ => throw new InvalidOperationException("injected CustomAnimals post-commit removal fault");
                bool threw = false;
                try
                {
                    service.RefreshDefinitions("unit-custom-animal-removed-demand-fault", force: false);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("post-commit removal fault", StringComparison.Ordinal))
                {
                    threw = true;
                }
                finally
                {
                    service.PostCommitFaultForTest = null;
                }

                ContentRefreshReceipt receipt = runtime.ContentRefreshGenerations.GetReceipts().Last(item => item.Domain == ContentRefreshDomains.CustomAnimals && item.OwnerId == ownerId);
                Assert(threw && receipt.Status == ContentRefreshCompletionStatus.Removed && !service.RegisteredKeySummary.Contains("anim_removed_fault", StringComparison.Ordinal), "The injected post-commit fault must leave the removed definition snapshot and terminal receipt authoritative.");
                Assert(runtime.DemandCoordinator.GetDemandCount(GameBridgeDemandRoutes.CustomAnimalAnimatorBridge) == 0 && runtime.DemandCoordinator.GetOwnerDemandCount(ownerId) == 0, "A definition-to-removed commit must release the final owner demand even when post-commit hygiene throws.");
                Assert(!runtime.ContentRefreshGenerations.IsDirty(ContentRefreshDomains.CustomAnimals), "The published removal generation must remain terminal after the post-commit fault.");
            }
            finally
            {
                runtime.NotifyRuntimeShutdown("Batch5 CustomAnimals removed demand post-commit fault test");
            }
        }

        private static void CustomAnimalInvalidRejectionWaitsForStaleAuthority()
        {
            CreateCustomAnimalDemandFixture(
                "InvalidStale",
                "DTMAPI.Tests.Batch5.CustomAnimals.InvalidStale",
                "species_invalid_stale",
                "anim_invalid_stale",
                out DtmApiRuntime runtime,
                out CustomAnimalAnimatorBridgeService service,
                out string ownerId,
                out string schemaPath);
            try
            {
                Assert(service.RefreshDefinitions("unit-custom-animal-invalid-stale-initial", force: false), "The last-good CustomAnimals generation must commit before stale rejection testing.");
                File.WriteAllText(schemaPath, "[{ invalid-json ]", new UTF8Encoding(false));
                runtime.ContentRefreshGenerations.MarkDirty(ContentRefreshDomains.CustomAnimals, new[] { ownerId }, "unit-custom-animal-invalid-stale");
                int[] before = CaptureCustomAnimalRejectionObservations(runtime, ownerId);
                service.BeforeGenerationCompleteForTest = batch =>
                {
                    AssertCustomAnimalRejectionObservations(before, CaptureCustomAnimalRejectionObservations(runtime, ownerId), "An invalid candidate must publish no rejection observation before stale authority validation.");
                    runtime.ContentRefreshGenerations.AbandonAndRequeue(batch, "injected invalid CustomAnimals stale completion");
                };
                bool threw = false;
                try
                {
                    service.RefreshDefinitions("unit-custom-animal-invalid-stale", force: false);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("rejected as stale", StringComparison.Ordinal))
                {
                    threw = true;
                }
                finally
                {
                    service.BeforeGenerationCompleteForTest = null;
                }

                Assert(threw, "The injected stale invalid generation must be rejected by the authority edge.");
                AssertCustomAnimalRejectionObservations(before, CaptureCustomAnimalRejectionObservations(runtime, ownerId), "A stale invalid candidate must leave no diagnostics, log, or lifecycle rejection side effect.");

                Assert(service.RefreshDefinitions("unit-custom-animal-invalid-stale-successor", force: false), "The invalid stale generation's successor must remain consumable.");
                int[] after = CaptureCustomAnimalRejectionObservations(runtime, ownerId);
                Assert(after[0] == before[0] + 1 && after[1] == before[1] + 1 && after[2] == before[2] + 1, "The fresh invalid successor must publish its diagnostics, log, and lifecycle rejection exactly once after authority commit.");
                Assert(runtime.ContentRefreshGenerations.GetReceipts().Count(item => item.Domain == ContentRefreshDomains.CustomAnimals && item.OwnerId == ownerId && item.Status == ContentRefreshCompletionStatus.Rejected) == 1, "Only the fresh successor may publish the owner-scoped rejected terminal receipt.");
            }
            finally
            {
                runtime.NotifyRuntimeShutdown("Batch5 CustomAnimals invalid stale authority test");
            }
        }

        private static void CustomAnimalMissingSchemaRejectionWaitsForPreCommitAuthority()
        {
            CreateCustomAnimalDemandFixture(
                "MissingSchemaPreCommit",
                "DTMAPI.Tests.Batch5.CustomAnimals.MissingSchemaPreCommit",
                "species_missing_schema_precommit",
                "anim_missing_schema_precommit",
                out DtmApiRuntime runtime,
                out CustomAnimalAnimatorBridgeService service,
                out string ownerId,
                out string schemaPath);
            try
            {
                Assert(service.RefreshDefinitions("unit-custom-animal-missing-schema-precommit-initial", force: false), "The last-good CustomAnimals generation must commit before missing-schema pre-commit rejection testing.");
                File.Delete(schemaPath);
                runtime.ContentRefreshGenerations.MarkDirty(ContentRefreshDomains.CustomAnimals, new[] { ownerId }, "unit-custom-animal-missing-schema-precommit");
                int[] before = CaptureCustomAnimalRejectionObservations(runtime, ownerId);
                service.PreCommitFaultForTest = _ =>
                {
                    AssertCustomAnimalRejectionObservations(before, CaptureCustomAnimalRejectionObservations(runtime, ownerId), "A missing-schema candidate must publish no rejection observation before its visible-snapshot authority callback.");
                    throw new InvalidOperationException("injected missing-schema CustomAnimals pre-commit fault");
                };
                bool threw = false;
                try
                {
                    service.RefreshDefinitions("unit-custom-animal-missing-schema-precommit", force: false);
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("missing-schema CustomAnimals pre-commit fault", StringComparison.Ordinal))
                {
                    threw = true;
                }
                finally
                {
                    service.PreCommitFaultForTest = null;
                }

                Assert(threw, "The injected missing-schema pre-commit failure must cross the real authority callback.");
                AssertCustomAnimalRejectionObservations(before, CaptureCustomAnimalRejectionObservations(runtime, ownerId), "A failed missing-schema pre-commit candidate must leave no diagnostics, log, or lifecycle rejection side effect.");

                Assert(service.RefreshDefinitions("unit-custom-animal-missing-schema-precommit-successor", force: false), "The missing-schema pre-commit generation's requeued successor must remain consumable.");
                int[] after = CaptureCustomAnimalRejectionObservations(runtime, ownerId);
                Assert(after[0] == before[0] + 1 && after[1] == before[1] + 1 && after[2] == before[2] + 1, "The fresh missing-schema successor must publish its diagnostics, log, and lifecycle rejection exactly once after authority commit.");
                Assert(runtime.ContentRefreshGenerations.GetReceipts().Count(item => item.Domain == ContentRefreshDomains.CustomAnimals && item.OwnerId == ownerId && item.Status == ContentRefreshCompletionStatus.Rejected) == 1, "Only the fresh requeued successor may publish the owner-scoped rejected terminal receipt.");
            }
            finally
            {
                runtime.NotifyRuntimeShutdown("Batch5 CustomAnimals missing-schema pre-commit authority test");
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
            bridge.FishRoeTooltipService!.ConfigureFishRoeProvider(first, new FishRoeTooltipOptions(), _ => null);
            bridge.ChestLocatorEnhancerService!.Register(second, new ChestLocatorEnhancerOptions { Enabled = true });
            Assert(runtime.DemandCoordinator.GetOwnerDemandCount(first.UniqueID) > 0 && runtime.DemandCoordinator.GetOwnerDemandCount(second.UniqueID) > 0, "Shutdown fixture must begin with ordinary owner demand.");

            bridge.Shutdown("Batch5 authoritative route release");
            RuntimeDemandSnapshot snapshot = runtime.DemandCoordinator.GetSnapshot();
            Assert(snapshot.Capabilities.Where(item => GameBridgeDemandRoutes.Catalog.Any(route => route.CapabilityId.Equals(item.CapabilityId, StringComparison.OrdinalIgnoreCase))).All(item => item.TotalDemand == 0 && !item.UpdaterActive), "Shutdown must clear every GameBridge capability directly, independent of bounded diagnostic owner projections.");
        }

        private static RuntimeCapabilityDemandSnapshot GetRoute(DtmApiRuntime runtime, string capabilityId) =>
            runtime.DemandCoordinator.GetSnapshot().Capabilities.Single(item => item.CapabilityId.Equals(capabilityId, StringComparison.OrdinalIgnoreCase));

        private static void SetAudioDefinitionDemand(DtmApiRuntime runtime, string ownerId, bool active, bool animalVoice)
        {
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.AudioReplacement, ownerId, RuntimeDemandSourceType.ContentDefinition, RuntimeDemandLifetime.Owner, "enabled-definitions", active, "unit audio definition closure");
            GameBridgeDemandRoutes.SetOwnerDemand(runtime, GameBridgeDemandRoutes.AudioReplacementAnimalVoiceContext, ownerId, RuntimeDemandSourceType.ContentDefinition, RuntimeDemandLifetime.Owner, "animal-voice-definitions", active && animalVoice, "unit audio AnimalVoice closure");
        }

        private static void CreateCustomAnimalDemandFixture(
            string fixtureName,
            string ownerId,
            string speciesId,
            string animatorKey,
            out DtmApiRuntime runtime,
            out CustomAnimalAnimatorBridgeService service,
            out string normalizedOwnerId,
            out string schemaPath)
        {
            string gamePath = Path.Combine(DtmApiTestSession.Current.RootPath, "batch5-custom-animal-demand-" + (++fixtureSequence).ToString() + "-" + fixtureName);
            string ownerRoot = Path.Combine(gamePath, "Mods", fixtureName);
            string dtmapiRoot = Path.Combine(ownerRoot, "Content", "DTMAPI");
            Directory.CreateDirectory(Path.Combine(gamePath, "BepInEx", "plugins"));
            Directory.CreateDirectory(dtmapiRoot);
            File.WriteAllText(
                Path.Combine(ownerRoot, "manifest.json"),
                "{ \"Name\": \"Batch 5 CustomAnimals Demand\", \"Author\": \"DTMAPI\", \"Version\": \"1.0.0\", \"UniqueID\": \"" + ownerId + "\", \"Type\": \"ContentPack\" }",
                new UTF8Encoding(false));
            schemaPath = Path.Combine(dtmapiRoot, "custom-animals.json");
            WriteCustomAnimalDemandSchema(schemaPath, speciesId, animatorKey);
            runtime = new DtmApiRuntime(new FakeHost(gamePath), new ConfigMenuRegistry());
            runtime.Start();
            service = new CustomAnimalAnimatorBridgeService(runtime);
            normalizedOwnerId = ownerId;
        }

        private static void WriteCustomAnimalDemandSchema(string schemaPath, string speciesId, string animatorKey)
        {
            File.WriteAllText(
                schemaPath,
                "[{ \"speciesId\": \"" + speciesId + "\", \"templateSpeciesId\": \"goat\", \"aiTemplate\": \"goat\", \"animatorMode\": \"assetBundle\", \"adultAnimatorKey\": \"" + animatorKey + "\", \"animatorBundle\": \"Content/Bundles/animals\", \"adultAnimatorAsset\": \"controller\" }]",
                new UTF8Encoding(false));
        }

        private static int[] CaptureCustomAnimalRejectionObservations(DtmApiRuntime runtime, string ownerId)
        {
            int warnings = runtime.Diagnostics.GetWarnings().Count(warning =>
                warning.Owner.Equals(ownerId, StringComparison.OrdinalIgnoreCase) &&
                warning.Message.Contains("Custom animal generation rejected", StringComparison.Ordinal));
            string logPath = runtime.Diagnostics.GetLatestLogPath();
            string logText = File.Exists(logPath) ? File.ReadAllText(logPath, Encoding.UTF8) : string.Empty;
            int logEntries = CountTextOccurrences(logText, "Custom animal owner generation rejected owner=" + ownerId);
            int lifecycleEvents = runtime.LifecycleBoundaryContractSnapshot.ResourceEventCounts
                .Where(pair => pair.Key.EndsWith("::CustomAnimals::OwnerGenerationRejected", StringComparison.OrdinalIgnoreCase))
                .Sum(pair => pair.Value);
            return new[] { warnings, logEntries, lifecycleEvents };
        }

        private static void AssertCustomAnimalRejectionObservations(int[] expected, int[] actual, string message)
        {
            Assert(
                expected.Length == 3 && actual.Length == 3 &&
                expected[0] == actual[0] && expected[1] == actual[1] && expected[2] == actual[2],
                message + " expected=" + string.Join(",", expected) + "; actual=" + string.Join(",", actual) + ".");
        }

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

        private static string CreateAudioPack(string parent, string name, string schema)
        {
            string root = Path.Combine(parent, name);
            string dtmapi = Path.Combine(root, "Content", "DTMAPI");
            string audio = Path.Combine(root, "Content", "Audio");
            Directory.CreateDirectory(dtmapi);
            Directory.CreateDirectory(audio);
            File.WriteAllText(Path.Combine(dtmapi, "audio-replacements.json"), schema ?? "[]", new UTF8Encoding(false));
            WritePcm16MonoWav(Path.Combine(audio, "test.wav"));
            return root;
        }

        private static void WritePcm16MonoWav(string path)
        {
            const int sampleRate = 8000;
            const short channels = 1;
            const short bitsPerSample = 16;
            const int sampleCount = 80;
            int dataLength = sampleCount * sizeof(short);
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: false);
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataLength);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * bitsPerSample / 8);
            writer.Write((short)(channels * bitsPerSample / 8));
            writer.Write(bitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataLength);
            for (int i = 0; i < sampleCount; i++)
                writer.Write((short)1000);
        }

        private static void CreateFixture(out DtmApiRuntime runtime, out DolocTownGameBridge bridge)
        {
            string gamePath = Path.Combine(DtmApiTestSession.Current.RootPath, "batch5-gamebridge-" + (++fixtureSequence).ToString());
            Directory.CreateDirectory(Path.Combine(gamePath, "BepInEx", "plugins"));
            StageCompatibilityHostFixture(gamePath);
            runtime = new DtmApiRuntime(new FakeHost(gamePath), new ConfigMenuRegistry());
            bridge = new DolocTownGameBridge(runtime);
        }

        private static void StageCompatibilityHostFixture(string gamePath)
        {
            StageCompatibilityHostFixture(
                gamePath,
                "CompatibilityHostFixture",
                "DTMAPI.GameBridge.DolocTown.Compatibility.dll");
        }

        private static void StageCompatibilityHostFixture(string gamePath, string fixtureDirectory, string fixtureFileName)
        {
            const string fileName = "DTMAPI.GameBridge.DolocTown.Compatibility.dll";
            string source = Path.Combine(AppContext.BaseDirectory, fixtureDirectory, fixtureFileName);
            if (!File.Exists(source))
                throw new FileNotFoundException("The Unit build did not stage the Compatibility Host fixture.", source);

            string componentDirectory = Path.Combine(gamePath, "DTMAPI", "components", "compatibility");
            string manifestDirectory = Path.Combine(gamePath, "DTMAPI");
            Directory.CreateDirectory(componentDirectory);
            Directory.CreateDirectory(manifestDirectory);
            string target = Path.Combine(componentDirectory, fileName);
            File.Copy(source, target, overwrite: true);

            var info = new FileInfo(target);
            string sha256;
            using (FileStream stream = File.OpenRead(target))
            using (SHA256 hash = SHA256.Create())
                sha256 = BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty);
            string fileVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(target).FileVersion ?? string.Empty;
            string manifest =
                "{\"OptionalComponents\":[{" +
                "\"ComponentId\":\"gamebridge-compatibility-host\"," +
                "\"Distribution\":\"dormant-shipped\"," +
                "\"LoadPolicy\":\"first-frozen-abi-call\"," +
                "\"RelativePath\":\"DTMAPI/components/compatibility/" + fileName + "\"," +
                "\"Length\":" + info.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "\"Sha256\":\"" + sha256 + "\"," +
                "\"AssemblyName\":\"DTMAPI.GameBridge.DolocTown.Compatibility\"," +
                "\"AssemblyVersion\":\"" + CompatibilityHostBroker.ExpectedAssemblyVersion + "\"," +
                "\"FileVersion\":\"" + fileVersion + "\"," +
                "\"TargetFramework\":\"netstandard2.0\"," +
                "\"DefaultLoadState\":\"dormant\"," +
                "\"IncludedInDownloadPackage\":true}]}";
            File.WriteAllText(Path.Combine(manifestDirectory, "release-manifest.json"), manifest, new UTF8Encoding(false));
        }

        private static HookCallbackBoundaryScope BindHookCallbackBoundary(DtmApiRuntime runtime, DolocTownGameBridge bridge) =>
            new HookCallbackBoundaryScope(runtime, bridge);

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

        private readonly struct HookCallbackBoundaryScope : IDisposable
        {
            private readonly DtmApiRuntime? previousRuntime;
            private readonly DolocTownGameBridge? previousBridge;

            internal HookCallbackBoundaryScope(DtmApiRuntime runtime, DolocTownGameBridge bridge)
            {
                previousRuntime = DolocTownHookCallbacks.Runtime;
                previousBridge = DolocTownHookCallbacks.Bridge;
                DolocTownHookCallbacks.Runtime = runtime;
                DolocTownHookCallbacks.Bridge = bridge;
            }

            public void Dispose()
            {
                DolocTownHookCallbacks.Runtime = previousRuntime;
                DolocTownHookCallbacks.Bridge = previousBridge;
            }
        }

        private sealed class SaveSlotsManagerFixture
        {
            internal int archiveFileCount = 6;
        }

        private sealed class FakeHost : IRuntimeHost
        {
            public FakeHost(string gamePath)
            {
                GamePath = gamePath;
                PluginPath = Path.Combine(gamePath, "BepInEx", "plugins");
            }

            public string GamePath { get; }
            public string PluginPath { get; }
            public string HostName => "Batch5GameBridgeUnitTest";
            public List<string> Logs { get; } = new List<string>();
            public void Log(string message) => Logs.Add(message ?? string.Empty);
            public void LogWarning(string message) => Logs.Add(message ?? string.Empty);
            public void LogError(string message, Exception? exception = null) => Logs.Add((message ?? string.Empty) + (exception == null ? string.Empty : ": " + exception.Message));
        }
    }
}
