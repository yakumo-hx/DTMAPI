#pragma warning disable CS0618 // Frozen compatibility contracts are intentionally exercised.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using DTMAPI.Abstractions;
using DTMAPI.Core.Diagnostics;
using DTMAPI.Core.Logging;
using DTMAPI.Core.Manager;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.Core.Services;
using DTMAPI.GameBridge.DolocTown;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {

        private static void InactiveOwnerCleanupCanBeRetriedUntilZero()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                const string ownerId = "DTMAPI.Tests.CleanupRetry";
                var participant = new RetryingOwnerCleanupParticipant("DTMAPI.Tests.CleanupRetry.Participant", failFirstCleanup: true);
                runtime.RegisterModOwnerCleanupParticipant(participant);
                runtime.Start();

                string first = InvokeFailedOwnerCleanup(runtime, ownerId);
                Assert(participant.CallCount == 1 && participant.RemainingResources == 1, "The first owner cleanup attempt should expose the participant's retained root.");
                Assert(first.Contains("participantCleanupFailures=1", StringComparison.Ordinal) && first.Contains("remaining=1", StringComparison.Ordinal), "The first owner cleanup summary should fail closed while a participant root remains.");
                Assert(runtime.OwnerRequiresRestart(ownerId), "A failed deactivation attempt must leave the owner inactive and restart-required.");
                int restartDiagnostics = runtime.ModOwnerLedgerSnapshot.NeedsRestart;

                string second = InvokeOwnerDeactivation(runtime, ownerId, ModOwnerCleanupReason.EntryFailed, shutdown: false);
                Assert(participant.CallCount == 2 && participant.RemainingResources == 0, "An inactive owner must permit a cleanup retry that removes the retained root.");
                Assert(!second.Contains("alreadyInactive=true", StringComparison.Ordinal), "A terminal owner state must not short-circuit a required cleanup retry.");
                Assert(second.Contains("participantCleanupFailures=0", StringComparison.Ordinal) && second.Contains("remaining=0", StringComparison.Ordinal), "The retry summary should prove zero remaining roots without another Entry.");
                Assert(!runtime.OwnerRequiresRestart(ownerId), "An owner that never loaded an assembly should become eligible for same-process Entry once a retry proves every platform root is zero.");
                Assert(runtime.ModOwnerLedgerSnapshot.NeedsRestart == restartDiagnostics, "A cleanup retry must not duplicate the owner's restart-required diagnostic.");

                string modDir = WriteAtomicProbePackage(dir, ownerId, string.Empty);
                AtomicOwnerProbeMod.EntryCount = 0;
                runtime.NotifyWorkshopModListChanged();
                Assert(runtime.LoadedMods.Count(mod => mod.Manifest.UniqueID == ownerId) == 1 && AtomicOwnerProbeMod.EntryCount == 1, "The fully cleaned no-assembly owner should be able to enter and load normally in the same process.");
                Assert(!runtime.OwnerRequiresRestart(ownerId) && CountCoreOwnerRootsForTest(runtime, ownerId) > 0, "Successful same-process re-entry should publish ordinary owner roots without a stale restart gate.");

                File.WriteAllText(Path.Combine(modDir, "dtmapi.disabled"), "disable after successful Assembly.LoadFrom");
                runtime.NotifyWorkshopModListChanged();
                Assert(!runtime.LoadedMods.Any(mod => mod.Manifest.UniqueID == ownerId) && CountCoreOwnerRootsForTest(runtime, ownerId) == 0, "Disabling the successfully loaded retry owner should still clean all platform roots.");
                Assert(runtime.OwnerRequiresRestart(ownerId), "Once Assembly.LoadFrom succeeds, the code owner must remain restart-required after deactivation even when cleanup proves zero roots.");
                Assert(AtomicOwnerProbeMod.EntryCount == 1, "A loaded-assembly deactivation must not re-run Entry.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void RetiredLampCompatibilityShellIsOwnerBoundAndSideEffectFree()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                var owner = new ManifestModel
                {
                    Name = "Retained Lamp Consumer",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.RetainedLampConsumer",
                    Type = "CodeMod"
                };
                var spoof = new ManifestModel
                {
                    Name = "Spoofed Lamp Consumer",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.SpoofedLampConsumer",
                    Type = "CodeMod"
                };
                ILampControlApi raw = runtime.ModRegistry.GetApi<ILampControlApi>("DTMAPI.GameBridge.DolocTown")
                    ?? throw new InvalidOperationException("The historical GameBridge provider must publish a non-null retained Lamp contract.");
                bool ownerActive = true;
                IModRegistry registry = runtime.ModRegistry.CreateOwnerBoundRegistry(owner, () =>
                {
                    if (!ownerActive)
                        throw new InvalidOperationException("owner inactive");
                });
                ILampControlApi api = registry.GetApi<ILampControlApi>("DTMAPI.GameBridge.DolocTown")
                    ?? throw new InvalidOperationException("A retained Lamp consumer must receive a non-null owner-bound facade.");
                Assert(!ReferenceEquals(raw, api) && ReferenceEquals(api, registry.GetApi<ILampControlApi>("DTMAPI.GameBridge.DolocTown")), "The Lamp provider must cache a stable consumer facade instead of exposing its raw provider.");

                var options = new LampManualToggleOptions
                {
                    Enabled = true,
                    EquipmentIds = new[] { "floor_lamp", "wall_lamp" },
                    VerboseLogging = true
                };
                LampManualToggleRegisterResult result = api.RegisterManualToggle(owner, options);
                Assert(!result.Success && !result.Enabled && !result.HookInstalled && result.OwnerId == owner.UniqueID, "Retired Lamp registration must deterministically fail closed for the canonical owner.");
                Assert(result.FailureReason == RetiredLampControlApi.StatusCode && result.Message == RetiredLampControlApi.StatusMessage && result.RegisteredEquipmentIds.Count == 0, "Retired Lamp registration must report the stable retired-disabled reason without registering equipment.");
                Assert(result.SessionOverrideCount == 0 && result.LastTouchedEquipmentId.Length == 0 && result.LastToggledEquipmentId.Length == 0, "Retired Lamp registration must create no session, touch, or toggle state.");

                LampManualToggleState state = api.GetState(owner.UniqueID);
                BridgeFeatureStatus status = api.GetStatus(owner.UniqueID);
                Assert(!state.IsConfigured && !state.Enabled && !state.HookInstalled && state.Status == RetiredLampControlApi.StatusCode && state.FailureReason == RetiredLampControlApi.StatusCode, "Retired Lamp state must remain disabled and unconfigured after registration attempts.");
                Assert(state.RegisteredEquipmentIds.Count == 0 && state.SessionOverrideCount == 0 && state.LastTouchedEquipmentId.Length == 0 && state.LastToggledEquipmentId.Length == 0 && !state.LastToggledValue, "Retired Lamp state must retain no equipment, override, or toggle state.");
                Assert(status.Status == RetiredLampControlApi.StatusCode && status.Details == RetiredLampControlApi.StatusMessage, "Retired Lamp status must use the stable retired-disabled result.");
                Assert(CountGameBridgeOwnerResourcesForTest(bridge, owner.UniqueID) == 0, "Retired Lamp compatibility calls must create no GameBridge owner resources.");
                Assert(!runtime.ResourceLifecycleSnapshot.Records.Any(record => record.OwnerId.Equals(owner.UniqueID, StringComparison.OrdinalIgnoreCase)), "Retired Lamp compatibility calls must create no lifecycle records.");
                Assert(!runtime.Diagnostics.GetHookStatuses().Any(item => item.HookId.Contains("Lamp", StringComparison.OrdinalIgnoreCase)), "Retired Lamp compatibility must publish or install no Lamp hook.");

                AssertThrows(() => api.RegisterManualToggle(spoof, options), "The owner-bound Lamp facade must reject a forged manifest.");
                AssertThrows(() => api.GetState(spoof.UniqueID), "The owner-bound Lamp facade must reject a cross-owner state query.");
                AssertThrows(() => api.GetStatus(spoof.UniqueID), "The owner-bound Lamp facade must reject a cross-owner status query.");
                ownerActive = false;
                AssertThrows(() => api.GetStatus(owner.UniqueID), "A stale owner-bound Lamp facade must reject use after consumer deactivation.");
                Assert(CountGameBridgeOwnerResourcesForTest(bridge, owner.UniqueID) == 0, "Rejected stale Lamp calls must not create GameBridge owner resources.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void GameBridgeOwnerCleanupCountsAndReleasesCameraLease()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string gameDir = NewTempGameDir();
                StageCompatibilityHostFixture(gameDir);
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                bridge.CameraFeatureForQa!.HookBridge
                    .SetCompatibilityOwnerClaimOverrideForTests(() => true);
                var owner = new ManifestModel
                {
                    Name = "Camera Lease Consumer",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.CameraLeaseConsumer",
                    Type = "CodeMod"
                };
                var spoof = new ManifestModel
                {
                    Name = "Spoofed Camera Owner",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.CameraLeaseSpoof",
                    Type = "CodeMod"
                };
                bool ownerActive = true;
                IModRegistry registry = runtime.ModRegistry.CreateOwnerBoundRegistry(owner, () =>
                {
                    if (!ownerActive)
                        throw new InvalidOperationException("owner inactive");
                });
                ICameraViewApi api = registry.GetApi<ICameraViewApi>("DTMAPI.GameBridge.DolocTown")
                    ?? throw new InvalidOperationException("Camera View API should be registered by GameBridge.");
                var request = new CameraViewRequest
                {
                    Enabled = true,
                    ViewScale = 1.5,
                    MinViewScale = 1,
                    MaxViewScale = 4,
                    Step = 0.25,
                    LeaseName = "owner-cleanup-unit"
                };
                AssertThrows(() => api.AcquireLease(spoof, request), "The owner-bound Camera facade must reject a forged manifest before creating a lease.");
                ICameraViewLease lease = api.AcquireLease(owner, request);
                Assert(!lease.IsReleased && CountGameBridgeOwnerResourcesForTest(bridge, owner.UniqueID) == 2, "A live Camera lease and its independently retained coordinator demand must be counted as two authoritative GameBridge owner roots.");

                ownerActive = false;
                string summary = InvokeFailedOwnerCleanup(runtime, owner.UniqueID);

                Assert(lease.IsReleased, "Unified owner deactivation should release the Camera lease handle.");
                Assert(CountGameBridgeOwnerResourcesForTest(bridge, owner.UniqueID) == 0, "GameBridge owner root counting should reach zero after Camera lease cleanup.");
                Assert(runtime.ModRegistry.GetApi<ICameraViewApi>("DTMAPI.GameBridge.DolocTown") != null, "Consumer deactivation must not remove the process-lifetime GameBridge Camera provider.");
                Assert(summary.Contains("participantResourcesRemoved=2", StringComparison.Ordinal) &&
                    summary.Contains("participantCleanupFailures=0", StringComparison.Ordinal) &&
                    summary.Contains("remaining=0", StringComparison.Ordinal), "Camera lease deactivation should report the removed lease plus coordinator demand root and zero remaining roots.");
                AssertThrows(() => api.AcquireLease(owner, request), "A stale owner-bound Camera facade must reject lease acquisition after owner deactivation.");
                Assert(CountGameBridgeOwnerResourcesForTest(bridge, owner.UniqueID) == 0, "A stale Camera facade must not recreate a GameBridge owner root.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void CameraOwnerCleanupRetainsFailedNativeRestoreForRetry()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string gameDir = NewTempGameDir();
                StageCompatibilityHostFixture(gameDir);
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                CameraFeature feature = bridge.CameraFeature
                    ?? throw new InvalidOperationException("Camera feature should be registered for native-restore cleanup tests.");
                feature.HookBridge
                    .SetCompatibilityOwnerClaimOverrideForTests(() => true);
                CameraViewService service = GetPrivateField<CameraViewService>(feature, "viewService");
                double nativeSize = 10d;
                bool failWrites = false;
                service.ConfigureNativeAccessForTests(
                    () => nativeSize,
                    size =>
                    {
                        if (failWrites)
                            return false;
                        nativeSize = size;
                        return true;
                    });
                var owner = new ManifestModel
                {
                    Name = "Camera Restore Consumer",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.CameraRestoreConsumer",
                    Type = "CodeMod"
                };
                IModRegistry registry = runtime.ModRegistry.CreateOwnerBoundRegistry(owner, () =>
                {
                    if (runtime.OwnerRequiresRestart(owner.UniqueID))
                        throw new InvalidOperationException("owner inactive");
                });
                ICameraViewApi api = registry.GetApi<ICameraViewApi>("DTMAPI.GameBridge.DolocTown")
                    ?? throw new InvalidOperationException("Camera View API should be registered by GameBridge.");
                ICameraViewLease lease = api.AcquireLease(owner, new CameraViewRequest
                {
                    Enabled = true,
                    ViewScale = 2d,
                    MinViewScale = 1d,
                    MaxViewScale = 4d,
                    Step = 0.25d,
                    LeaseName = "native-restore-retry"
                });
                Assert(Math.Abs(nativeSize - 20d) < 0.001d && !lease.IsReleased, "The fixture should apply a live 2x native Camera lease before cleanup.");

                failWrites = true;
                string failed = InvokeFailedOwnerCleanup(runtime, owner.UniqueID);
                Assert(lease.IsReleased, "A failed restore should still make the consumer lease handle immutable/released.");
                Assert(Math.Abs(nativeSize - 20d) < 0.001d && CountGameBridgeOwnerResourcesForTest(bridge, owner.UniqueID) == 1, "A failed native Camera write must retain one owner cleanup tombstone and the applied native size for retry.");
                Assert(failed.Contains("participantCleanupFailures=1", StringComparison.Ordinal) && failed.Contains("remaining=1", StringComparison.Ordinal), "Camera restore failure must be surfaced as a participant failure with one authoritative remaining root. summary=" + failed);

                failWrites = false;
                string retried = InvokeOwnerDeactivation(runtime, owner.UniqueID, ModOwnerCleanupReason.EntryFailed, shutdown: false);
                Assert(Math.Abs(nativeSize - 10d) < 0.001d && CountGameBridgeOwnerResourcesForTest(bridge, owner.UniqueID) == 0, "A later owner cleanup pass should restore vanilla Camera size and release the tombstone without another Entry.");
                Assert(retried.Contains("participantCleanupFailures=0", StringComparison.Ordinal) && retried.Contains("remaining=0", StringComparison.Ordinal), "Successful Camera cleanup retry must prove zero remaining roots. summary=" + retried);
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void CameraLeaseRequestSurvivesSaveAndTitleUntilOwnerDeactivation()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string gameDir = NewTempGameDir();
                StageCompatibilityHostFixture(gameDir);
                var runtime = new DtmApiRuntime(new FakeHost(gameDir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                CameraFeature feature = bridge.CameraFeature
                    ?? throw new InvalidOperationException("Camera feature should be registered for lifecycle-preservation tests.");
                feature.HookBridge
                    .SetCompatibilityOwnerClaimOverrideForTests(() => true);
                CameraViewService service = GetPrivateField<CameraViewService>(feature, "viewService");
                double nativeSize = 10d;
                service.ConfigureNativeAccessForTests(
                    () => nativeSize,
                    size =>
                    {
                        nativeSize = size;
                        return true;
                    });
                var owner = new ManifestModel
                {
                    Name = "Camera Lifecycle Consumer",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.CameraLifecycleConsumer",
                    Type = "CodeMod"
                };
                IModRegistry registry = runtime.ModRegistry.CreateOwnerBoundRegistry(owner, () =>
                {
                    if (runtime.OwnerRequiresRestart(owner.UniqueID))
                        throw new InvalidOperationException("owner inactive");
                });
                ICameraViewApi api = registry.GetApi<ICameraViewApi>("DTMAPI.GameBridge.DolocTown")
                    ?? throw new InvalidOperationException("Camera View API should be registered by GameBridge.");
                ICameraViewLease lease = api.AcquireLease(owner, new CameraViewRequest
                {
                    Enabled = true,
                    ViewScale = 2d,
                    MinViewScale = 1d,
                    MaxViewScale = 4d,
                    Step = 0.25d,
                    LeaseName = "process-lifetime-camera"
                });
                Assert(Math.Abs(nativeSize - 20d) < 0.001d, "The fixture should apply the owner's 2x Camera lease.");

                bridge.NotifyGameBridgeFeaturesSaveLoaded(isNewGame: false);
                CameraViewState saveState = lease.GetState();
                Assert(!lease.IsReleased &&
                    Math.Abs(saveState.RequestedViewScale - 2d) < 0.001d &&
                    Math.Abs(saveState.AppliedViewScale - 2d) < 0.001d &&
                    Math.Abs(nativeSize - 20d) < 0.001d,
                    "SaveLoaded should clear/rebuild only the native application and reapply the unchanged process-lifetime 2x lease request.");

                bridge.NotifyGameBridgeFeaturesReturnedToTitle();
                CameraViewState titleState = lease.GetState();
                Assert(!lease.IsReleased &&
                    Math.Abs(titleState.RequestedViewScale - 2d) < 0.001d &&
                    Math.Abs(titleState.AppliedViewScale - 1d) < 0.001d &&
                    Math.Abs(nativeSize - 10d) < 0.001d &&
                    CountGameBridgeOwnerResourcesForTest(bridge, owner.UniqueID) == 2,
                    "ReturnedToTitle should restore the transient native camera while retaining the owner's process-lifetime lease and coordinator demand roots.");
                service.RefreshForRuntime();
                Assert(Math.Abs(nativeSize - 10d) < 0.001d, "Per-frame refresh must not apply a retained gameplay lease to the title camera.");

                bridge.NotifyGameBridgeFeaturesSaveLoaded(isNewGame: false);
                CameraViewState reloadedState = lease.GetState();
                Assert(Math.Abs(reloadedState.RequestedViewScale - 2d) < 0.001d &&
                    Math.Abs(reloadedState.AppliedViewScale - 2d) < 0.001d &&
                    Math.Abs(nativeSize - 20d) < 0.001d,
                    "The next SaveLoaded should reapply the retained 2x lease without another Entry or acquisition.");

                string cleanup = InvokeFailedOwnerCleanup(runtime, owner.UniqueID);
                Assert(lease.IsReleased &&
                    Math.Abs(nativeSize - 10d) < 0.001d &&
                    CountGameBridgeOwnerResourcesForTest(bridge, owner.UniqueID) == 0,
                    "Owner deactivation should remain the boundary that releases the Camera lease and restores vanilla native size.");
                Assert(cleanup.Contains("participantCleanupFailures=0", StringComparison.Ordinal) && cleanup.Contains("remaining=0", StringComparison.Ordinal), "Camera lifecycle cleanup should prove zero remaining roots. summary=" + cleanup);
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void ChestLocatorPoliciesMergeEnabledOwners()
        {
            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                _ = new DolocTownGameBridge(runtime);
                IChestLocatorEnhancerApi api = GetModRegistry(runtime).GetApi<IChestLocatorEnhancerApi>("DTMAPI.GameBridge.DolocTown")
                    ?? throw new InvalidOperationException("ChestLocatorEnhancer API should be registered by the GameBridge runtime owner.");
                IManifest ownerA = new ManifestModel
                {
                    Name = "Chest Policy A",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.ChestPolicyA"
                };
                IManifest ownerB = new ManifestModel
                {
                    Name = "Chest Policy B",
                    Author = "DTMAPI",
                    Version = "1.0.0",
                    UniqueID = "DTMAPI.Tests.ChestPolicyB"
                };

                ChestLocatorEnhancerRegisterResult first = api.Register(ownerA, new ChestLocatorEnhancerOptions
                {
                    Enabled = true,
                    IncludeSharedCases = false,
                    IncludeSharedStorageShelfBoxes = false,
                    RespectNativeAutoUseBoxSetting = false,
                    VerboseLogging = false
                });
                ChestLocatorEnhancerRegisterResult second = api.Register(ownerB, new ChestLocatorEnhancerOptions
                {
                    Enabled = true,
                    IncludeSharedCases = true,
                    IncludeSharedStorageShelfBoxes = true,
                    RespectNativeAutoUseBoxSetting = true,
                    VerboseLogging = true
                });

                ChestLocatorEnhancerState stateA = api.GetState(ownerA.UniqueID);
                ChestLocatorEnhancerState stateB = api.GetState(ownerB.UniqueID);
                string messageA = stateA.LastMessage ?? string.Empty;
                string messageB = stateB.LastMessage ?? string.Empty;
                Assert(first.Success && second.Success, "ChestLocatorEnhancer owner registration should continue to succeed.");
                Assert(messageA.Contains("effectiveOwners=DTMAPI.Tests.ChestPolicyA|DTMAPI.Tests.ChestPolicyB", StringComparison.Ordinal), "Effective owner summary should include every enabled owner in deterministic order.");
                Assert(messageB.Contains("effectiveOwners=DTMAPI.Tests.ChestPolicyA|DTMAPI.Tests.ChestPolicyB", StringComparison.Ordinal), "Every owner state should expose the same effective owner summary.");
                Assert(messageA.Contains("includeSharedCases=True", StringComparison.Ordinal), "Merged policy should enable shared Case scanning when any enabled owner requests it.");
                Assert(messageA.Contains("includeSharedStorageShelfBoxes=True", StringComparison.Ordinal), "Merged policy should enable shared StorageShelf box scanning when any enabled owner requests it.");
                Assert(messageA.Contains("respectNativeAutoUseBox=False", StringComparison.Ordinal), "Merged policy should allow forced box scans when any enabled owner opts out of native auto-use-box.");
                Assert(messageA.Contains("verboseLogging=True", StringComparison.Ordinal), "Merged policy should enable verbose logging when any enabled owner requests it.");
                Assert(stateA.Status == "configured-pending-hook" && stateB.Status == "configured-pending-hook", "Merged policy should keep the existing pending-hook state before Harmony installation.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void SaveSlotsNormalizeToFixedTwelveContract()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.SaveSlotsService")
                ?? throw new InvalidOperationException("SaveSlotsService type should exist.");
            MethodInfo normalize = serviceType.GetMethod("NormalizeSaveSlotsOptions", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("SaveSlotsService should keep an internal normalize helper.");

            var oversized = new SaveSlotsOptions { Enabled = true, SlotCount = 24, VerboseLogging = true };
            var undersized = new SaveSlotsOptions { Enabled = true, SlotCount = 6 };
            var disabled = new SaveSlotsOptions { Enabled = false, SlotCount = 24 };

            SaveSlotsOptions normalizedOversized = (SaveSlotsOptions)(normalize.Invoke(null, new object?[] { oversized }) ?? throw new InvalidOperationException("Normalize should return options."));
            SaveSlotsOptions normalizedUndersized = (SaveSlotsOptions)(normalize.Invoke(null, new object?[] { undersized }) ?? throw new InvalidOperationException("Normalize should return options."));
            SaveSlotsOptions normalizedDisabled = (SaveSlotsOptions)(normalize.Invoke(null, new object?[] { disabled }) ?? throw new InvalidOperationException("Normalize should return options."));

            Assert(normalizedOversized.Enabled && normalizedOversized.SlotCount == 12, "SaveSlots enabled oversized requests should normalize to 12 total official slots.");
            Assert(normalizedUndersized.Enabled && normalizedUndersized.SlotCount == 12, "SaveSlots enabled undersized requests should normalize to 12 total official slots.");
            Assert(!normalizedDisabled.Enabled && normalizedDisabled.SlotCount == 6, "SaveSlots disabled requests should normalize to the vanilla six-slot contract.");
            Assert(normalizedOversized.VerboseLogging, "SaveSlots normalize should preserve unrelated logging flags.");
        }

        private static void ActionSpeedOptionsNormalizeNativeStageDefaults()
        {
            ActionSpeedOptions defaults = InvokeActionSpeedCompatibilityStatic<ActionSpeedOptions>("NormalizeActionSpeedOptionsForTest", new object?[] { null });
            var invalid = new ActionSpeedOptions
            {
                Enabled = true,
                ToolSpeedEnabled = true,
                ToolMultiplier = double.NaN,
                BottleFillSpeedEnabled = true,
                BottleFillMultiplier = 100,
                EatDrinkSpeedEnabled = true,
                EatDrinkMultiplier = -5,
                MachineAddSpeedEnabled = true,
                MachineAddMultiplier = double.PositiveInfinity,
                HarvestSpeedEnabled = true,
                HarvestMultiplier = 2.5,
                PlantSpeedEnabled = true,
                PlantMultiplier = 0,
                AutoFillBottle = false,
                AutoFillStrong = true,
                AutoFillCooldownSeconds = -1,
                AutoFillStrongCooldownSeconds = 99,
                ContinuousDrinkWithRightClick = true,
                VerboseLogging = true
            };

            ActionSpeedOptions normalizedInvalid = InvokeActionSpeedCompatibilityStatic<ActionSpeedOptions>("NormalizeActionSpeedOptionsForTest", new object?[] { invalid });

            Assert(Math.Abs(defaults.ToolMultiplier - 3) < 0.0001, "ActionSpeed defaults should keep the existing tool multiplier.");
            Assert(Math.Abs(defaults.BottleFillMultiplier - 3) < 0.0001, "ActionSpeed defaults should keep the existing bottle-fill multiplier.");
            Assert(Math.Abs(defaults.AutoFillCooldownSeconds - 0.25) < 0.0001, "ActionSpeed defaults should keep the existing auto-fill cooldown.");
            Assert(Math.Abs(defaults.AutoFillStrongCooldownSeconds - 0.1) < 0.0001, "ActionSpeed defaults should keep the existing strong auto-fill cooldown.");
            Assert(Math.Abs(normalizedInvalid.ToolMultiplier - 1) < 0.0001, "ActionSpeed invalid tool multiplier should normalize to one.");
            Assert(Math.Abs(normalizedInvalid.BottleFillMultiplier - 4) < 0.0001, "ActionSpeed bottle-fill multiplier should clamp to four.");
            Assert(Math.Abs(normalizedInvalid.EatDrinkMultiplier - 1) < 0.0001, "ActionSpeed eat/drink multiplier should clamp to one.");
            Assert(Math.Abs(normalizedInvalid.MachineAddMultiplier - 1) < 0.0001, "ActionSpeed infinite machine multiplier should normalize to one.");
            Assert(Math.Abs(normalizedInvalid.HarvestMultiplier - 2.5) < 0.0001, "ActionSpeed valid harvest multiplier should be preserved.");
            Assert(Math.Abs(normalizedInvalid.PlantMultiplier - 1) < 0.0001, "ActionSpeed plant multiplier should clamp to one.");
            Assert(Math.Abs(normalizedInvalid.AutoFillCooldownSeconds - 0.05) < 0.0001, "ActionSpeed auto-fill cooldown should clamp to supported minimum.");
            Assert(Math.Abs(normalizedInvalid.AutoFillStrongCooldownSeconds - 5) < 0.0001, "ActionSpeed strong auto-fill cooldown should clamp to supported maximum.");
            Assert(!normalizedInvalid.AutoFillStrong, "ActionSpeed strong auto-fill should be disabled when auto-fill itself is disabled.");
            Assert(normalizedInvalid.VerboseLogging, "ActionSpeed normalize should preserve unrelated logging flags.");
        }

        private static void ActionSpeedProviderPrecedenceIsDeterministic()
        {
            var policies = new Dictionary<string, ActionSpeedOptions>(StringComparer.OrdinalIgnoreCase)
            {
                ["Yuuka.DTMAPI.Slower"] = new ActionSpeedOptions { Enabled = true, PlantSpeedEnabled = true, PlantMultiplier = 2 },
                ["Yuuka.DTMAPI.DisabledFast"] = new ActionSpeedOptions { Enabled = false, PlantSpeedEnabled = true, PlantMultiplier = 4 },
                ["Yuuka.DTMAPI.FastB"] = new ActionSpeedOptions { Enabled = true, PlantSpeedEnabled = true, PlantMultiplier = 4 },
                ["Yuuka.DTMAPI.FastA"] = new ActionSpeedOptions { Enabled = true, PlantSpeedEnabled = true, PlantMultiplier = 4 },
                ["Yuuka.DTMAPI.Unit"] = new ActionSpeedOptions { Enabled = true, PlantSpeedEnabled = true, PlantMultiplier = 1 }
            };

            object?[] arguments =
            {
                policies,
                (Func<ActionSpeedOptions, bool>)(candidate => candidate.PlantSpeedEnabled),
                (Func<ActionSpeedOptions, double>)(candidate => candidate.PlantMultiplier),
                null,
                0d
            };
            bool selected = InvokeActionSpeedCompatibilityStatic<bool>("TrySelectPolicyForTest", arguments);
            string ownerId = arguments[3] as string ?? string.Empty;
            double multiplier = arguments[4] is double selectedMultiplier ? selectedMultiplier : 0d;

            Assert(selected, "ActionSpeed provider precedence should select an enabled provider.");
            Assert(ownerId == "Yuuka.DTMAPI.FastA", "ActionSpeed provider precedence should choose the highest multiplier and stable owner-id tie-break.");
            Assert(Math.Abs(multiplier - 4) < 0.0001, "ActionSpeed provider precedence should expose the selected multiplier.");
        }

        private static void ActionSpeedNativeClassificationCoversAttempts()
        {
            var fertilizedBasin = new DolocTown.PlantBasin { IsPlanted = true, IsFertilizerd = true };
            bool fertilizerAttempt = InvokeActionSpeedCompatibilityClassification("IsPlantInteractionForTest", out string fertilizerOwner, new DolocTown.ItemFertilizer(), fertilizedBasin, null);
            Assert(fertilizerAttempt && fertilizerOwner.Contains("PlantBasin.Fertilizer") && fertilizerOwner.Contains("already-fertilized"), "ActionSpeed should classify repeated fertilizer attempts because native still plays AgentStateInteract before the failure message.");

            var protectedBasin = new DolocTown.PlantBasin { IsPlanted = true, IsProtected = true };
            protectedBasin.supply.IsProtectedFull = true;
            bool filmAttempt = InvokeActionSpeedCompatibilityClassification("IsPlantInteractionForTest", out string filmOwner, new DolocTown.ItemFilm(), protectedBasin, null);
            Assert(filmAttempt && filmOwner.Contains("PlantBasin.Protect") && filmOwner.Contains("already-full"), "ActionSpeed should classify repeated crop-film attempts because native still plays AgentStateInteract before the failure message.");

            bool plantedSeedAttempt = InvokeActionSpeedCompatibilityClassification("IsPlantInteractionForTest", out _, new DolocTown.ItemSeed(), new DolocTown.PlantBasin { IsPlanted = true }, null);
            Assert(!plantedSeedAttempt, "ActionSpeed should not classify ordinary seed attempts on already-planted PlantBasin because native returns before _Interact.");

            bool flowerPotSeedAttempt = InvokeActionSpeedCompatibilityClassification("IsPlantInteractionForTest", out string potOwner, new DolocTown.ItemSeed(), new DolocTown.FlowerPot { IsPlanted = true }, null);
            Assert(flowerPotSeedAttempt && potOwner.Contains("FlowerPot.Plant"), "ActionSpeed should classify FlowerPot seed attempts because native still enters _Interact before the no-op callback.");

            var treeFertilizer = new DolocTown.ItemFertilizer();
            treeFertilizer.func.IsTree = true;
            var fertilizedTree = new DolocTown.PlantBasinTree { Crop = new DolocTown.TreeCrop { IsFertilizered = true } };
            bool treeFertilizerAttempt = InvokeActionSpeedCompatibilityClassification("IsPlantInteractionForTest", out string treeOwner, treeFertilizer, fertilizedTree, null);
            Assert(treeFertilizerAttempt && treeOwner.Contains("PlantBasinTree.Fertilizer") && treeOwner.Contains("already-fertilized"), "ActionSpeed should classify repeated tree fertilizer attempts through PlantBasinTree.");

            bool machineSwitch = InvokeActionSpeedCompatibilityClassification("IsMachineInteractionForTest", out string switchOwner, new DolocTown.Sprinkler(), null);
            Assert(machineSwitch && switchOwner.Contains("AffectorElectric.OnInteract"), "ActionSpeed machine interaction should include electric sprinkler switch actions.");
            bool growLightSwitch = InvokeActionSpeedCompatibilityClassification("IsMachineInteractionForTest", out string growLightOwner, new DolocTown.FarmLight(), null);
            Assert(growLightSwitch && growLightOwner.Contains("AffectorElectric.OnInteract"), "ActionSpeed machine interaction should include agricultural grow-light switch actions.");
            Assert(!InvokeActionSpeedCompatibilityClassification("IsMachineInteractionForTest", out _, new DolocTown.Sound(), null), "ActionSpeed should not broaden machine switch acceleration to every AffectorElectric subclass.");
            Assert(InvokeActionSpeedCompatibilityStatic<bool>("IsAnimalFondleInteractionForTest", new object?[] { new DolocTown.AnimalRenderer() }), "ActionSpeed should classify native animal fondle interaction separately from harvest.");
            Assert(InvokeActionSpeedCompatibilityStatic<bool>("IsAnimalFondleInteractionForTest", new object?[] { new DolocTown.RoomConnector(), new DolocTown.AnimalRenderer() }), "ActionSpeed should prefer the native AnimalRenderer.OnInteract owner marker when scanner state is overlapped by a room connector.");
            Assert(!InvokeActionSpeedCompatibilityStatic<bool>("ShouldClearPendingNativeAnimalInteractForTest", new object?[] { "AgentStateBase.OnExit" }), "ActionSpeed should not clear the native animal owner marker while BodyController._Interact is replacing the previous state.");
            Assert(!InvokeActionSpeedCompatibilityStatic<bool>("ShouldClearPendingNativeAnimalInteractForTest", new object?[] { "AgentStateTool.OnExit" }), "ActionSpeed should not clear the native animal owner marker if an action transition exits a tool state before AgentStateInteract.OnEnter.");
            Assert(InvokeActionSpeedCompatibilityStatic<bool>("ShouldClearPendingNativeAnimalInteractForTest", new object?[] { "AgentStateInteract.OnExit" }), "ActionSpeed should clear the native animal owner marker after the native interact state exits.");

            var resinCollector = new DolocTown.ResinCollector { currentValue = 1 };
            bool resinSelected = InvokeActionSpeedCompatibilityClassification("IsHarvestInteractionForTest", out string resinSelectedOwner, resinCollector, null);
            Assert(resinSelected && resinSelectedOwner.Contains("ResinCollector.OnInteract"), "ActionSpeed should classify ready resin collectors from selected equipment.");
            bool resinCurrent = InvokeActionSpeedCompatibilityClassification("IsHarvestInteractionForTest", out string resinCurrentOwner, null, resinCollector);
            Assert(resinCurrent && resinCurrentOwner.Contains("ResinCollector.OnInteract"), "ActionSpeed should classify ready resin collectors from current interactable.");
            Assert(!InvokeActionSpeedCompatibilityClassification("IsHarvestInteractionForTest", out _, new DolocTown.ResinCollector { currentValue = 0 }, null), "ActionSpeed should not classify empty resin collectors through generic IGatherableEquipment fallback.");
        }

        private static void EquipmentSlotProtectedStoragePolicyUsesPerSaveTailRecovery()
        {
            Assert(EquipmentSlotProtectedStoragePolicy.BuildSaveScopeKey(3) == "slot-3", "Equipment-slot protected storage should key sidecars by archive slot.");
            Assert(EquipmentSlotProtectedStoragePolicy.MakeSafePathSegment("A:B/C") == "A_B_C", "Equipment-slot protected storage should sanitize path segments.");
            Assert(EquipmentSlotProtectedStoragePolicy.GetTailIndexFromEnd(0, 4) == 3, "Equipment-slot storage should record head slots as furthest from the tail.");
            Assert(EquipmentSlotProtectedStoragePolicy.GetTailIndexFromEnd(3, 4) == 0, "Equipment-slot storage should record the final slot as the tail.");

            int[] ordered = EquipmentSlotProtectedStoragePolicy.OrderTailFirst(new[] { 0, 1, 2, 3 }, value => value).ToArray();
            Assert(ordered.SequenceEqual(new[] { 3, 2, 1, 0 }), "Equipment-slot recovery should process tail slots before head slots.");

            Assert(EquipmentSlotProtectedStoragePolicy.IsStorageCompatible(3, "Yuuka", string.Empty, 100, 3, "Yuuka", string.Empty, 120, out _), "Matching save identity should allow protected storage.");
            Assert(!EquipmentSlotProtectedStoragePolicy.IsStorageCompatible(2, "Yuuka", string.Empty, 100, 3, "Yuuka", string.Empty, 120, out string archiveReason) && archiveReason.Contains("archive-index-mismatch"), "Protected storage should reject mismatched archive slots.");
            Assert(!EquipmentSlotProtectedStoragePolicy.IsStorageCompatible(3, "Other", string.Empty, 100, 3, "Yuuka", string.Empty, 120, out string playerReason) && playerReason.Contains("player-name-mismatch"), "Protected storage should reject clearly mismatched player identities.");
            Assert(!EquipmentSlotProtectedStoragePolicy.IsStorageCompatible(3, "Yuuka", string.Empty, 1000, 3, "Yuuka", string.Empty, 120, out string clockReason) && clockReason.Contains("total-game-seconds-regressed"), "Protected storage should reject strong save-clock regressions.");
        }

        private static void EquipmentSlotShieldPolicyMirrorsNativeShieldHat()
        {
            Assert(EquipmentSlotShieldPolicy.IsShieldSkill("shield", "DolocTown.ItemFunctionHatShield", "DolocTown.AgentEquipmentFuncProtoShield"), "Equipment-slot shield policy should recognize native shield hats by skill/function.");

            EquipmentSlotShieldBlockResult defended = EquipmentSlotShieldPolicy.Block(incomingDamage: 2, shieldValue: 10, shieldDefend: 3);
            Assert(defended.FullyBlocked && !defended.Broken && defended.BlockedDamage == 0 && defended.RemainingShieldValue == 10, "Shield Defend should fully block small hits without consuming shield value.");

            EquipmentSlotShieldBlockResult blocked = EquipmentSlotShieldPolicy.Block(incomingDamage: 10, shieldValue: 20, shieldDefend: 2);
            Assert(blocked.FullyBlocked && !blocked.Broken && blocked.BlockedDamage == 8 && blocked.RemainingShieldValue == 12, "Shield should consume damage after native Defend when it fully blocks the hit.");

            EquipmentSlotShieldBlockResult broken = EquipmentSlotShieldPolicy.Block(incomingDamage: 10, shieldValue: 5, shieldDefend: 2);
            Assert(!broken.FullyBlocked && broken.Broken && broken.BlockedDamage == 5 && broken.RemainingShieldValue == 0, "Broken shields should report only the shield value as blocked damage, matching native residual damage semantics.");
        }

        private static void AnimalViewerLocalizationGuardRecognizesUiLocalizationComponents()
        {
            Type serviceType = LoadCompatibilityBackendType("AnimalViewer");
            MethodInfo looksLikeLocalization = serviceType.GetMethod("LooksLikeLocalizationComponent", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("AnimalViewerService should keep a localization guard helper.");

            Assert((bool)(looksLikeLocalization.Invoke(null, new object[] { "DolocTown.UI.UILocalization" }) ?? false), "AnimalViewer localization guard should recognize Doloc UI localization components.");
            Assert((bool)(looksLikeLocalization.Invoke(null, new object[] { "Some.Namespace.LocalizeText" }) ?? false), "AnimalViewer localization guard should recognize Localize-style components.");
            Assert(!(bool)(looksLikeLocalization.Invoke(null, new object[] { "UnityEngine.UI.Text" }) ?? true), "AnimalViewer localization guard must not disable plain text components.");
            Assert(!(bool)(looksLikeLocalization.Invoke(null, new object[] { "DolocTown.UI.ProgressBar" }) ?? true), "AnimalViewer localization guard must not disable the ProgressBar component itself.");
        }

        private static void AnimalViewerNativeCloseBoundaryPublishesNeutralLifecycle()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type serviceType = LoadCompatibilityBackendType("AnimalViewer");
            Type hookBridgeType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.AnimalViewerHookBridge")
                ?? throw new InvalidOperationException("AnimalViewerHookBridge type should exist.");
            MethodInfo close = serviceType.GetMethod("NotifyAnimalPanelUnregistered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("AnimalViewerService must expose the native panel close boundary internally.");
            FieldInfo nativeRows = serviceType.GetField("animalProgressRowsByData", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("AnimalViewerService should retain derived native rows only for the current panel session.");
            Type renderRowType = serviceType.GetNestedType("AnimalProgressRenderRow", BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("AnimalViewerService progress render row type should exist.");
            MethodInfo callback = typeof(DolocTownHookCallbacks).GetMethod("AnimalPanelUiStateUnregisterPostfix", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException("The native AnimalPanelUiState.Unregister postfix callback should exist.");
            PropertyInfo closePatched = hookBridgeType.GetProperty("PanelUnregisterPatched", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("AnimalViewerHookBridge should track its native panel close patch.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                var bridge = new DolocTownGameBridge(runtime);
                FieldInfo featureField = typeof(DolocTownGameBridge).GetField("animalViewerFeature", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("DolocTownGameBridge should retain its AnimalViewer feature for hook readiness projection.");
                object feature = featureField.GetValue(bridge)
                    ?? throw new InvalidOperationException("DolocTownGameBridge should initialize the AnimalViewer feature during API registration.");
                PropertyInfo hookBridgeProperty = feature.GetType().GetProperty("HookBridge", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("AnimalViewerFeature should expose its hook bridge internally.");
                object hookBridge = hookBridgeProperty.GetValue(feature)
                    ?? throw new InvalidOperationException("AnimalViewerFeature hook bridge should exist.");
                PropertyInfo fullInfoPatched = hookBridgeType.GetProperty("FullInfoDataPatched", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("AnimalViewerHookBridge should track its FullInfoData patch.");
                PropertyInfo showPrefixPatched = hookBridgeType.GetProperty("ViewerShowPrefixPatched", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("AnimalViewerHookBridge should track its Viewer.Show prefix patch.");
                PropertyInfo showPatched = hookBridgeType.GetProperty("ViewerShowPatched", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("AnimalViewerHookBridge should track its Viewer.Show postfix patch.");
                PropertyInfo hooksReady = hookBridgeType.GetProperty("HooksReady", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("AnimalViewerHookBridge should expose aggregate readiness internally.");
                PropertyInfo globalPanelUnregisterPatched = typeof(DolocTownGameBridge).GetProperty("animalPanelUnregisterPatched", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("DolocTownGameBridge must project the AnimalPanelUiState.Unregister hook into global readiness.");
                MethodInfo featureSummary = typeof(DolocTownGameBridge).GetMethod("BuildFeatureHookReadinessSummary", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("DolocTownGameBridge should expose its feature hook readiness summary internally.");

                int ReadSummaryCount(string name)
                {
                    string summary = (string)(featureSummary.Invoke(bridge, Array.Empty<object>())
                        ?? throw new InvalidOperationException("Feature hook readiness summary should be available."));
                    string prefix = name + "=";
                    string token = summary.Split(';').Select(part => part.Trim()).Single(part => part.StartsWith(prefix, StringComparison.Ordinal));
                    return int.Parse(token.Substring(prefix.Length), CultureInfo.InvariantCulture);
                }

                int ReadyCount() => ReadSummaryCount("readyCount");

                int initialReady = ReadyCount();
                int initialTotal = ReadSummaryCount("total");
                fullInfoPatched.SetValue(hookBridge, true);
                showPatched.SetValue(hookBridge, true);
                Assert(!(bool)(hooksReady.GetValue(hookBridge) ?? true) && ReadyCount() == initialReady + 2,
                    "Global AnimalViewer readiness must not become ready when only the former FullInfoData and Viewer.Show postfix pair is installed.");
                showPrefixPatched.SetValue(hookBridge, true);
                Assert(!(bool)(hooksReady.GetValue(hookBridge) ?? true) && ReadyCount() == initialReady + 3,
                    "Global AnimalViewer readiness must count the Viewer.Show prefix while still requiring the native close hook.");
                closePatched.SetValue(hookBridge, true);
                Assert((bool)(hooksReady.GetValue(hookBridge) ?? false) &&
                    (bool)(globalPanelUnregisterPatched.GetValue(bridge) ?? false) &&
                    ReadyCount() == initialReady + 4 &&
                    ReadSummaryCount("total") == initialTotal &&
                    initialTotal >= 4,
                    "Global readiness must project and count all four AnimalViewer hooks without hard-coding unrelated product Hook totals.");

                object service = CompatibilityHostBroker.For(runtime).GetService("AnimalViewer");

                var retainedRows = (IDictionary)(nativeRows.GetValue(service)
                    ?? throw new InvalidOperationException("AnimalViewerService native row map should exist."));
                retainedRows.Add(new object(), Array.CreateInstance(renderRowType, 0));
                Assert(retainedRows.Count == 1, "The unit close test should begin with one retained native AnimalFullInfoData key.");

                close.Invoke(service, Array.Empty<object>());
                IHookStatusInfo status = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Animals.ViewerNativeLifecycle");
                Assert(status.Status == "closed" && status.Details.Contains("Unregister", StringComparison.Ordinal),
                    "The native AnimalPanelUiState.Unregister boundary should publish a neutral closed receipt without QA evidence policy.");
                Assert(callback.GetParameters().Length == 0 && closePatched.PropertyType == typeof(bool),
                    "The close callback must match the zero-parameter native Unregister signature and be tracked as a required hook.");
                Assert(retainedRows.Count == 0,
                    "The native panel close boundary must release derived AnimalFullInfoData keys instead of retaining them across repeated UI sessions.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static void AnimalViewerFeatureResetsCloneLifecycleOnBoundaries()
        {
            Assembly bridgeAssembly = typeof(DolocTownGameBridge).Assembly;
            Type featureType = bridgeAssembly.GetType("DTMAPI.GameBridge.DolocTown.AnimalViewerFeature")
                ?? throw new InvalidOperationException("AnimalViewerFeature type should exist.");

            string? previousRoot = UseTempPersistentRoot();
            try
            {
                string dir = NewTempGameDir();
                StageCompatibilityHostFixture(dir);
                var runtime = new DtmApiRuntime(new FakeHost(dir), new ConfigMenuRegistry());
                CompatibilityHostBroker.For(runtime).GetService("AnimalViewer");
                object feature = Activator.CreateInstance(featureType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { runtime }, null)
                    ?? throw new InvalidOperationException("AnimalViewerFeature should be constructable for unit tests.");

                MethodInfo saveLoaded = featureType.GetMethod("SaveLoaded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("AnimalViewerFeature.SaveLoaded should exist.");
                MethodInfo returnedToTitle = featureType.GetMethod("ReturnedToTitle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("AnimalViewerFeature.ReturnedToTitle should exist.");
                MethodInfo environmentReset = featureType.GetMethod("EnvironmentReset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("AnimalViewerFeature.EnvironmentReset should exist.");

                saveLoaded.Invoke(feature, new object[] { false });
                IHookStatusInfo saveStatus = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Animals.ViewerRenderingLifecycle");
                Assert(saveStatus.Status == "reset" && saveStatus.Details.Contains("SaveLoaded", StringComparison.Ordinal), "AnimalViewer save-load boundary should reset cloned progress-bar runtime state.");

                returnedToTitle.Invoke(feature, Array.Empty<object>());
                IHookStatusInfo titleStatus = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Animals.ViewerRenderingLifecycle");
                Assert(titleStatus.Status == "reset" && titleStatus.Details.Contains("ReturnedToTitle", StringComparison.Ordinal), "AnimalViewer title boundary should reset cloned progress-bar runtime state.");

                environmentReset.Invoke(feature, new object[] { "unit-test" });
                IHookStatusInfo resetStatus = runtime.Diagnostics.GetHookStatuses().Single(h => h.HookId == "Animals.ViewerRenderingLifecycle");
                Assert(resetStatus.Status == "observed" && resetStatus.Details.Contains("EnvironmentReset unit-test", StringComparison.Ordinal), "AnimalViewer environment reset should validate cloned progress-bar runtime state without destructively clearing a live session.");
            }
            finally
            {
                RestorePersistentRoot(previousRoot);
            }
        }

        private static Type LoadCompatibilityBackendType(string serviceId)
        {
            string gamePath = NewTempGameDir();
            StageCompatibilityHostFixture(gamePath);
            var runtime = new DtmApiRuntime(new FakeHost(gamePath), new ConfigMenuRegistry());
            return CompatibilityHostBroker.For(runtime).GetService(serviceId).GetType();
        }

        private static Type GetCompatibilityExecutorType(string typeName)
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(candidate =>
                    string.Equals(candidate.GetName().Name, "DTMAPI.GameBridge.DolocTown.Compatibility", StringComparison.Ordinal))
                ?? throw new InvalidOperationException("The staged Compatibility Host must be activated through its broker before executor tests run.");
            return assembly.GetType("DTMAPI.GameBridge.DolocTown." + typeName, throwOnError: true, ignoreCase: false)!;
        }

        private static T InvokeActionSpeedCompatibilityStatic<T>(string methodName, object?[] arguments)
        {
            MethodInfo method = GetCompatibilityExecutorType("ActionSpeedService")
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(candidate => candidate.Name == methodName && candidate.GetParameters().Length == arguments.Length);
            object? result = method.Invoke(null, arguments);
            return result is T typed ? typed : throw new InvalidOperationException("Compatibility ActionSpeed method '" + methodName + "' returned an unexpected value.");
        }

        private static bool InvokeActionSpeedCompatibilityClassification(string methodName, out string nativeOwner, params object?[] inputs)
        {
            object?[] arguments = inputs.Concat(new object?[] { null }).ToArray();
            bool result = InvokeActionSpeedCompatibilityStatic<bool>(methodName, arguments);
            nativeOwner = arguments[arguments.Length - 1] as string ?? string.Empty;
            return result;
        }

        private sealed class FakeHarmonyUnpatcher
        {
            internal bool ThrowOnUnpatch { get; set; }
            internal int UnpatchCalls { get; private set; }

            public void UnpatchAll(string ownerId)
            {
                UnpatchCalls++;
                if (ThrowOnUnpatch)
                    throw new InvalidOperationException("compatibility-unpatch-failure:" + ownerId);
            }
        }

        private struct FakeVector2
        {
            public FakeVector2(float x, float y)
            {
                this.x = x;
                this.y = y;
            }

            public float x;
            public float y;
        }

        private sealed class FakeDolocGameManager
        {
            public FakeDolocGameInitConfig gameInitConfig { get; set; } = new FakeDolocGameInitConfig();
        }

        private sealed class FakeDolocGameInitConfig
        {
            public bool skipFishingGame { get; set; }

            public bool ignoreMaterialCost { get; set; }

            public bool skipMoneyVerifyInShop { get; set; }

            public bool ignoreSpiritCost { get; set; }
        }

        private sealed class FakeGlobalParameter
        {
            public int FishingEnergyCost { get; set; }
        }
    }
}
