using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.StrongPlantingGun;
using HarmonyLib;

namespace DTMAPI.StrongPlantingGunHarmonyOwnerFixture
{
    internal static class Program
    {
        private const string ProductOwner =
            "dtmapi.mod.dtmapi.strongplantinggunmod";
        private const string CompatibilityOwner =
            "dtmapi.gamebridge.doloctown";
        private const string UnrelatedOwner =
            "dtmapi.tests.strongplantinggun.unrelated";

        private static readonly MethodBase[] Targets =
        {
            typeof(DolocTown.ItemFarmingGun).GetConstructor(
                new[]
                {
                    typeof(DolocTown.Config.Item.ItemInfo),
                    typeof(int)
                }) ?? throw new MissingMethodException(
                    "ItemFarmingGun.ctor(ItemInfo,int)"),
            typeof(DolocTown.ItemFarmingGun).GetConstructor(
                new[]
                {
                    typeof(string),
                    typeof(int),
                    typeof(DolocTown.LinearInventory)
                }) ?? throw new MissingMethodException(
                    "ItemFarmingGun.ctor(string,int,LinearInventory)"),
            typeof(DolocTown.ItemFarmingGun).GetMethod(
                "OnUseAsTool",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException(
                    "ItemFarmingGun.OnUseAsTool"),
            typeof(DolocTown.FarmingGunUiState).GetMethod(
                "HandlePlaceToOtherSide",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException(
                    "FarmingGunUiState.HandlePlaceToOtherSide"),
            typeof(DolocTown.FarmingGunUiState).GetMethod(
                "HandleSwapOneItem",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException(
                    "FarmingGunUiState.HandleSwapOneItem")
        };

        private static readonly HarmonyMethod FixturePostfix =
            new HarmonyMethod(
                typeof(Program).GetMethod(
                    nameof(ExactTargetPostfix),
                    BindingFlags.NonPublic |
                    BindingFlags.Static)
                ?? throw new MissingMethodException(
                    typeof(Program).FullName,
                    nameof(ExactTargetPostfix)));

        private static int Main()
        {
            try
            {
                FiveTargetsResolveAndProductionCallbacksExecute();
                CompatibilityFirstFailsClosed();
                AtomicRollbackRemovesEveryInstalledTarget();
                FailedRollbackPublishesRealResidue();
                ExactOwnerUnpatchPreservesUnrelatedOwners();
                FailedDeactivationPublishesRealResidue();
                ToolAndUiFailuresRollBackAndSuppressOriginals();
                ReceiverRollbackRestoresItemLockAndObservation();
                ConfigTitleAndDisposeLifecycleReachZero();
                SaveLoadedPreparesJsonGunConstructedWhileSuspended();
                SaveLoadedScanFailureRollsBackExactOwner();
                Console.WriteLine(
                    "StrongPlantingGunHarmonyOwnerFixture: OK");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Cleanup(ProductOwner);
                Cleanup(CompatibilityOwner);
                Cleanup(UnrelatedOwner);
                return 1;
            }
        }

        private static void
            FiveTargetsResolveAndProductionCallbacksExecute()
        {
            ResetOwners();
            var runtime =
                new StrongPlantingGunNativeRuntime(
                    NullMonitor.Instance);
            runtime.Configure(
                EnabledConfig(),
                "fixture pre-resolution");
            AssertOwnerCount(ProductOwner, 5);
            Assert(
                runtime.InstalledPatchCount == 5,
                "Production HookInstaller did not publish all five exact targets.");

            var ordinary =
                new DolocTown.ItemFarmingGun(
                    new DolocTown.Config.Item.ItemInfo(),
                    1);
            Assert(
                ordinary.inventory.capacity == 3 &&
                ordinary.func.Capacity == 3 &&
                runtime.CapacitySnapshotCount == 1,
                "The real constructor Postfix did not execute ProductNative fixed-three preparation.");

            var nativeJsonInventory =
                new DolocTown.LinearInventory(7);
            var json =
                new DolocTown.ItemFarmingGun(
                    "fixture-json",
                    1,
                    nativeJsonInventory);
            Assert(
                json.inventory.capacity == 7 &&
                json.func.Capacity == 3 &&
                runtime.CapacitySnapshotCount == 1,
                "The JSON constructor Postfix did not retain the native inventory while applying fixed-three function policy. inventory=" +
                json.inventory.capacity +
                "; function=" +
                json.func.Capacity +
                "; snapshots=" +
                runtime.CapacitySnapshotCount +
                "; last=" +
                runtime.LastMessage +
                ".");
            var ordinaryAfterJson =
                new DolocTown.ItemFarmingGun(
                    new DolocTown.Config.Item.ItemInfo(),
                    1);
            Assert(
                ordinaryAfterJson.inventory.capacity == 3 &&
                ordinaryAfterJson.func.Capacity == 3 &&
                runtime.CapacitySnapshotCount == 1 &&
                ReferenceEquals(ordinary.func, json.func) &&
                ReferenceEquals(
                    ordinary.func,
                    ordinaryAfterJson.func),
                "A subsequent ordinary gun did not retain the fixed-three ProductNative contract after a seven-slot JSON gun.");

            runtime.DeactivateOwner("fixture constructor cleanup");
            AssertOwnerCount(ProductOwner, 0);
            Assert(
                ordinary.func.Capacity == 1 &&
                json.func.Capacity == 1 &&
                ordinaryAfterJson.func.Capacity == 1,
                "Owner cleanup did not restore the single shared proto function capacity to one.");
            AssertLifecycleZero();
        }

        private static void
            ReceiverRollbackRestoresItemLockAndObservation()
        {
            ResetOwners();
            var runtime =
                new StrongPlantingGunNativeRuntime(
                    NullMonitor.Instance);
            runtime.Configure(
                EnabledConfig(),
                "receiver rollback fixture");

            var uiGun =
                new DolocTown.ItemFarmingGun(
                    new DolocTown.Config.Item.ItemInfo(),
                    1);
            var backpack =
                new DolocTown.LinearInventory(3);
            var seed =
                new DolocTown.ItemSeed
                {
                    name = "locked-seed",
                    count = 2
                };
            backpack.SetItem(0, seed);
            backpack.SetSlotLocked(0, true);

            DolocTown.InventoryReceiver throwing =
                (index, item, locked) =>
                    throw new InvalidOperationException(
                        "fixture receiver failure");
            bool throwingReceiverRegistered = false;
            try
            {
                backpack.AddReceiver(throwing);
            }
            catch (InvalidOperationException)
            {
                throwingReceiverRegistered = true;
            }
            Assert(
                throwingReceiverRegistered,
                "The throwing receiver did not enter the native receiver list.");

            var observer = new InventoryObserver(3);
            backpack.AddReceiver(observer.Receive);
            int baselineObservations =
                observer.ObservationCount;
            var ui =
                new DolocTown.FarmingGunUiState(
                    uiGun,
                    backpack,
                    seed,
                    0);

            ui.HandlePlaceToOtherSide(0);
            Assert(
                backpack.TotalCount() == 2 &&
                ReferenceEquals(backpack.Read(0), seed) &&
                backpack.GetSlotLocked(0) &&
                uiGun.inventory.TotalCount() == 0 &&
                ui.OriginalPlaceCount == 0,
                "Receiver-triggered Take failure did not restore the exact item, count, slot lock, destination, and original-method suppression.");
            Assert(
                observer.ObservationCount >
                    baselineObservations &&
                ReferenceEquals(
                    observer.LastItems[0],
                    seed) &&
                observer.LastCounts[0] == 2 &&
                observer.LastLocks[0],
                "A throwing native receiver prevented the later observer from receiving the rollback-synchronized item/count/lock state.");

            backpack.RemoveReceiver(throwing);
            backpack.RemoveReceiver(observer.Receive);
            runtime.DeactivateOwner(
                "receiver rollback fixture cleanup");
            AssertOwnerCount(ProductOwner, 0);
            AssertLifecycleZero();
        }

        private static void CompatibilityFirstFailsClosed()
        {
            ResetOwners();
            InstallOwner(CompatibilityOwner, Targets);
            var runtime =
                new StrongPlantingGunNativeRuntime(
                    NullMonitor.Instance);
            AssertThrows<InvalidOperationException>(
                () => runtime.Configure(
                    EnabledConfig(),
                    "compatibility first"),
                "Compatibility-first ownership did not reject ProductNative before patching.");
            AssertOwnerCount(ProductOwner, 0);
            AssertOwnerCount(CompatibilityOwner, 5);
            Cleanup(CompatibilityOwner);
        }

        private static void
            AtomicRollbackRemovesEveryInstalledTarget()
        {
            ResetOwners();
            var runtime =
                new StrongPlantingGunNativeRuntime(
                    NullMonitor.Instance,
                    ordinal => ordinal != 3);
            AssertThrows<InvalidOperationException>(
                () => runtime.Configure(
                    EnabledConfig(),
                    "rollback"),
                "The injected third-target installation failure was not surfaced.");
            AssertOwnerCount(ProductOwner, 0);
            Assert(
                runtime.InstalledPatchCount == 0,
                "A successful atomic rollback published a non-zero owner count.");
            AssertLifecycleZero();
        }

        private static void FailedRollbackPublishesRealResidue()
        {
            ResetOwners();
            var runtime =
                new StrongPlantingGunNativeRuntime(
                    NullMonitor.Instance,
                    ordinal => ordinal != 3,
                    () => throw new InvalidOperationException(
                        "fixture unpatch failure"));
            AssertThrows<AggregateException>(
                () => runtime.Configure(
                    EnabledConfig(),
                    "failed rollback"),
                "Install plus unpatch failure did not surface as an aggregate.");
            AssertOwnerCount(ProductOwner, 2);
            Assert(
                runtime.InstalledPatchCount == 2 &&
                runtime.Status == "enable-failed-owned",
                "Failed rollback did not re-observe and classify the two real constructor owners. status=" +
                runtime.Status +
                ".");
            Assert(
                StrongPlantingGunCallbacks.LastLifecycleSummary
                    .IndexOf(
                        "callbacks=0;hooks=2",
                        StringComparison.Ordinal) >= 0 &&
                StrongPlantingGunCallbacks.LastLifecycleSummary
                    .EndsWith(
                        "roots=1",
                        StringComparison.Ordinal),
                "Failed rollback published a false zero lifecycle summary: " +
                StrongPlantingGunCallbacks.LastLifecycleSummary);
            AssertThrows<InvalidOperationException>(
                () => runtime.Configure(
                    EnabledConfig(),
                    "partial residue retry"),
                "A detached runtime with two residual owners did not block same-process re-enable.");
            AssertOwnerCount(ProductOwner, 2);
            Cleanup(ProductOwner);
        }

        private static void
            ExactOwnerUnpatchPreservesUnrelatedOwners()
        {
            ResetOwners();
            InstallOwner(UnrelatedOwner, Targets);
            var runtime =
                new StrongPlantingGunNativeRuntime(
                    NullMonitor.Instance);
            runtime.Configure(
                EnabledConfig(),
                "unrelated co-owner");
            AssertOwnerCount(ProductOwner, 5);
            AssertOwnerCount(UnrelatedOwner, 5);

            runtime.DeactivateOwner("exact owner");
            AssertOwnerCount(ProductOwner, 0);
            AssertOwnerCount(UnrelatedOwner, 5);
            AssertLifecycleZero();
            Cleanup(UnrelatedOwner);
        }

        private static void
            FailedDeactivationPublishesRealResidue()
        {
            ResetOwners();
            var runtime =
                new StrongPlantingGunNativeRuntime(
                    NullMonitor.Instance,
                    installGate: null,
                    unpatchOverride:
                        () => throw new InvalidOperationException(
                            "fixture deactivation unpatch failure"));
            runtime.Configure(
                EnabledConfig(),
                "failed deactivation");
            AssertOwnerCount(ProductOwner, 5);
            AssertThrows<AggregateException>(
                () => runtime.DeactivateOwner(
                    "failed deactivation"),
                "A real deactivation unpatch failure was not surfaced.");
            AssertOwnerCount(ProductOwner, 5);
            Assert(
                runtime.InstalledPatchCount == 5 &&
                runtime.Status == "cleanup-failed-owned" &&
                StrongPlantingGunCallbacks.LastLifecycleSummary
                    .IndexOf(
                        "callbacks=0;hooks=5",
                        StringComparison.Ordinal) >= 0 &&
                StrongPlantingGunCallbacks.LastLifecycleSummary
                    .EndsWith(
                        "roots=1",
                        StringComparison.Ordinal),
                "Failed deactivation published a false state instead of five real residual owners: status=" +
                runtime.Status +
                "; summary=" +
                StrongPlantingGunCallbacks.LastLifecycleSummary);
            Cleanup(ProductOwner);
        }

        private static void
            ToolAndUiFailuresRollBackAndSuppressOriginals()
        {
            ResetOwners();
            var monitor = new ThrowUiTransferMonitor();
            var runtime =
                new StrongPlantingGunNativeRuntime(monitor);
            runtime.Configure(
                EnabledConfig(verbose: true),
                "transaction fixture");

            var gun =
                new DolocTown.ItemFarmingGun(
                    new DolocTown.Config.Item.ItemInfo(),
                    1);
            var toolSeed =
                new DolocTown.ItemSeed
                {
                    name = "tool-seed",
                    count = 2
                };
            gun.inventory.SetItem(0, toolSeed);
            gun.DoInteractResult = false;
            gun.OnUseAsTool();
            Assert(
                gun.inventory.TotalCount() == 2 &&
                gun.OriginalToolUseCount == 0,
                "DoInteract=false after native cost did not restore the exact seed count or suppress the original tool method.");

            gun.DoInteractResult = true;
            gun.ThrowOnDoInteract = true;
            gun.OnUseAsTool();
            Assert(
                gun.inventory.TotalCount() == 2 &&
                gun.OriginalToolUseCount == 0,
                "DoInteract throw after native cost did not restore the exact seed count or suppress the original tool method.");
            gun.ThrowOnDoInteract = false;
            gun.inventory.ThrowOnCostAfterMutation = true;
            gun.OnUseAsTool();
            Assert(
                gun.inventory.TotalCount() == 2 &&
                gun.OriginalToolUseCount == 0,
                "Tool TryCostAtIndex throw after count mutation did not restore the exact seed count or suppress the original tool method.");
            gun.inventory.ThrowOnCostAfterMutation = false;

            var uiGun =
                new DolocTown.ItemFarmingGun(
                    new DolocTown.Config.Item.ItemInfo(),
                    1);
            var backpack =
                new DolocTown.LinearInventory(3);
            var uiSeed =
                new DolocTown.ItemSeed
                {
                    name = "ui-seed",
                    count = 2
                };
            backpack.SetItem(0, uiSeed);
            var ui =
                new DolocTown.FarmingGunUiState(
                    uiGun,
                    backpack,
                    uiSeed,
                    0);

            backpack.ThrowOnTakeAfterMutation = true;
            ui.HandlePlaceToOtherSide(0);
            AssertUiRolledBack(
                ui,
                backpack,
                uiGun.inventory,
                expectedPlaceOriginals: 0,
                expectedSwapOriginals: 0,
                "Take throw after mutation");

            backpack.ThrowOnTakeAfterMutation = false;
            uiGun.inventory.ThrowOnPlaceAfterMutation = true;
            ui.HandlePlaceToOtherSide(0);
            AssertUiRolledBack(
                ui,
                backpack,
                uiGun.inventory,
                expectedPlaceOriginals: 0,
                expectedSwapOriginals: 0,
                "Place throw after mutation");

            uiGun.inventory.ThrowOnPlaceAfterMutation = false;
            backpack.ThrowOnCostAfterMutation = true;
            ui.HandleSwapOneItem(0);
            AssertUiRolledBack(
                ui,
                backpack,
                uiGun.inventory,
                expectedPlaceOriginals: 0,
                expectedSwapOriginals: 0,
                "UI TryCostAtIndex throw after count mutation");

            backpack.ThrowOnCostAfterMutation = false;
            ui.HandleSwapOneItem(0);
            AssertUiRolledBack(
                ui,
                backpack,
                uiGun.inventory,
                expectedPlaceOriginals: 0,
                expectedSwapOriginals: 0,
                "UI receipt/log emit throw after mutation");

            runtime.DeactivateOwner(
                "transaction fixture cleanup");
            AssertOwnerCount(ProductOwner, 0);
            AssertLifecycleZero();
        }

        private static void AssertUiRolledBack(
            DolocTown.FarmingGunUiState ui,
            DolocTown.LinearInventory backpack,
            DolocTown.LinearInventory gunInventory,
            int expectedPlaceOriginals,
            int expectedSwapOriginals,
            string reason)
        {
            Assert(
                backpack.TotalCount() == 2 &&
                gunInventory.TotalCount() == 0 &&
                ui.OriginalPlaceCount ==
                    expectedPlaceOriginals &&
                ui.OriginalSwapCount ==
                    expectedSwapOriginals,
                reason +
                " did not preserve exactly two logical items, clear the destination, and suppress the original UI method. backpack=" +
                backpack.TotalCount() +
                "; gun=" +
                gunInventory.TotalCount() +
                "; placeOriginal=" +
                ui.OriginalPlaceCount +
                "; swapOriginal=" +
                ui.OriginalSwapCount +
                ".");
        }

        private static void
            ConfigTitleAndDisposeLifecycleReachZero()
        {
            ResetOwners();
            var runtime =
                new StrongPlantingGunNativeRuntime(
                    NullMonitor.Instance);
            runtime.Configure(
                EnabledConfig(),
                "config enabled");
            AssertOwnerCount(ProductOwner, 5);
            runtime.Configure(
                new StrongPlantingGunConfig
                {
                    Enabled = false
                },
                "config disabled");
            AssertOwnerCount(ProductOwner, 0);
            AssertLifecycleZero();

            runtime.Configure(
                EnabledConfig(),
                "same process restart");
            AssertOwnerCount(ProductOwner, 5);
            runtime.SuspendForTitle("fixture title");
            AssertOwnerCount(ProductOwner, 0);
            AssertLifecycleZero();

            runtime.Configure(
                EnabledConfig(),
                "post-title restart");
            AssertOwnerCount(ProductOwner, 5);
            var entry = new ModEntry();
            SetField(entry, "nativeRuntime", runtime);
            SetField(entry, "runtimeCreated", true);
            entry.Dispose();
            entry.Dispose();
            AssertOwnerCount(ProductOwner, 0);
            AssertLifecycleZero();
        }

        private static void
            SaveLoadedPreparesJsonGunConstructedWhileSuspended()
        {
            ResetOwners();
            var runtime =
                new StrongPlantingGunNativeRuntime(
                    NullMonitor.Instance);
            runtime.Configure(
                EnabledConfig(),
                "pre-title");
            runtime.SuspendForTitle("fixture title");
            AssertOwnerCount(ProductOwner, 0);

            var backpack =
                new DolocTown.LinearInventory(4);
            var jsonGun =
                new DolocTown.ItemFarmingGun(
                    "fixture-json-after-title",
                    1,
                    new DolocTown.LinearInventory(3));
            backpack.SetItem(0, jsonGun);
            Assert(
                jsonGun.func.Capacity == 1,
                "The suspended JSON constructor unexpectedly retained ProductNative capacity.");

            DolocAPI.archiveHandle =
                new DolocTown.ArchiveHandle(backpack);
            int prepared =
                runtime.ConfigureLoadedSaveBoundary(
                    EnabledConfig(),
                    "SaveLoaded");
            Assert(
                prepared == 1 &&
                jsonGun.inventory.capacity == 3 &&
                jsonGun.func.Capacity == 3 &&
                runtime.CapacitySnapshotCount == 1,
                "SaveLoaded did not prepare the already-deserialized backpack gun. prepared=" +
                prepared +
                "; inventory=" +
                jsonGun.inventory.capacity +
                "; function=" +
                jsonGun.func.Capacity +
                "; snapshots=" +
                runtime.CapacitySnapshotCount +
                ".");
            AssertOwnerCount(ProductOwner, 5);

            runtime.DeactivateOwner(
                "SaveLoaded fixture cleanup");
            Assert(
                jsonGun.func.Capacity == 1,
                "SaveLoaded cleanup did not restore the shared native function capacity.");
            AssertOwnerCount(ProductOwner, 0);
            AssertLifecycleZero();
            DolocAPI.archiveHandle = null;
        }

        private static void
            SaveLoadedScanFailureRollsBackExactOwner()
        {
            ResetOwners();
            var backpack =
                new DolocTown.LinearInventory(2);
            var jsonGun =
                new DolocTown.ItemFarmingGun(
                    "fixture-json-failing-scan",
                    1,
                    new DolocTown.LinearInventory(3));
            backpack.SetItem(0, jsonGun);
            backpack.ThrowOnReadIndex = 1;
            DolocAPI.archiveHandle =
                new DolocTown.ArchiveHandle(backpack);

            var runtime =
                new StrongPlantingGunNativeRuntime(
                    NullMonitor.Instance);
            bool failed = false;
            try
            {
                runtime.ConfigureLoadedSaveBoundary(
                    EnabledConfig(),
                    "SaveLoaded failing scan");
            }
            catch (TargetInvocationException ex)
                when (ex.InnerException is InvalidOperationException)
            {
                failed = true;
            }

            Assert(
                failed,
                "SaveLoaded inventory scan failure was not surfaced.");
            Assert(
                jsonGun.func.Capacity == 1,
                "SaveLoaded failure rollback did not restore the original native function capacity.");
            Assert(
                runtime.InstalledPatchCount == 0 &&
                runtime.CapacitySnapshotCount == 0,
                "SaveLoaded failure rollback retained ProductNative hooks or capacity snapshots.");
            AssertOwnerCount(ProductOwner, 0);
            AssertLifecycleZero();
            DolocAPI.archiveHandle = null;
        }

        private static StrongPlantingGunConfig EnabledConfig(
            bool verbose = false) =>
            new StrongPlantingGunConfig
            {
                Enabled = true,
                IncludeSeeds = true,
                IncludeFilms = true,
                IncludeFertilizers = true,
                VerboseLogging = verbose
            };

        private static void InstallOwner(
            string owner,
            IEnumerable<MethodBase> targets)
        {
            var harmony = new Harmony(owner);
            foreach (MethodBase target in targets)
            {
                harmony.Patch(
                    target,
                    postfix: FixturePostfix);
            }
        }

        private static void SetField(
            object instance,
            string name,
            object value)
        {
            FieldInfo field =
                instance.GetType().GetField(
                    name,
                    BindingFlags.NonPublic |
                    BindingFlags.Instance)
                ?? throw new MissingFieldException(
                    instance.GetType().FullName,
                    name);
            field.SetValue(instance, value);
        }

        private static void AssertLifecycleZero() =>
            Assert(
                StrongPlantingGunCallbacks.LastLifecycleSummary ==
                "listeners=0;callbacks=0;hooks=0;cachedObjects=0;cachedMembers=0;capacitySnapshots=0;roots=0",
                "Lifecycle summary was not exact zero: " +
                StrongPlantingGunCallbacks.LastLifecycleSummary);

        private static void Cleanup(string owner) =>
            Harmony.UnpatchID(owner);

        private static void ResetOwners()
        {
            DolocAPI.archiveHandle = null;
            Cleanup(ProductOwner);
            Cleanup(CompatibilityOwner);
            Cleanup(UnrelatedOwner);
            AssertOwnerCount(ProductOwner, 0);
            AssertOwnerCount(CompatibilityOwner, 0);
            AssertOwnerCount(UnrelatedOwner, 0);
        }

        private static int OwnerCount(string owner) =>
            Targets.Count(target =>
            {
                Patches? info =
                    Harmony.GetPatchInfo(target);
                return info != null &&
                    info.Owners.Any(
                        current =>
                            current.Equals(
                                owner,
                                StringComparison.Ordinal));
            });

        private static void AssertOwnerCount(
            string owner,
            int expected)
        {
            int actual = OwnerCount(owner);
            Assert(
                actual == expected,
                "Unexpected exact-target owner count for " +
                owner +
                ": expected=" +
                expected +
                ", actual=" +
                actual +
                ".");
        }

        private static void ExactTargetPostfix()
        {
        }

        private static void Assert(
            bool condition,
            string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertThrows<TException>(
            Action action,
            string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }

        private sealed class ThrowUiTransferMonitor : IMonitor
        {
            public void Log(
                string message,
                LogLevel level = LogLevel.Info)
            {
                if ((message ?? string.Empty).IndexOf(
                    "StrongPlantingGun UI transfer",
                    StringComparison.Ordinal) >= 0)
                {
                    throw new InvalidOperationException(
                        "fixture UI receipt emission failure");
                }
            }

            public void LogOnce(
                string key,
                string message,
                LogLevel level = LogLevel.Info)
            {
            }

            public void LogException(
                Exception exception,
                string message)
            {
            }
        }

        private sealed class InventoryObserver
        {
            internal InventoryObserver(int capacity)
            {
                LastItems =
                    new DolocTown.Item?[capacity];
                LastCounts = new int[capacity];
                LastLocks = new bool[capacity];
            }

            internal int ObservationCount { get; private set; }

            internal DolocTown.Item?[] LastItems { get; }

            internal int[] LastCounts { get; }

            internal bool[] LastLocks { get; }

            internal void Receive(
                int index,
                DolocTown.Item? item,
                bool locked)
            {
                ObservationCount++;
                LastItems[index] = item;
                LastCounts[index] =
                    item?.count ?? 0;
                LastLocks[index] = locked;
            }
        }
    }
}
