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
            "DTMAPI.StrongPlantingGunMod.Native";
        private const string CompatibilityOwner =
            "dtmapi.gamebridge.doloctown";
        private const string UnrelatedOwner =
            "dtmapi.tests.strongplantinggun.unrelated";

        private const int PatchCount = 15;
        private static readonly MethodBase[] Targets = BuildTargets();
        private static MethodBase[] BuildTargets()
        {
            var list = new List<MethodBase> {
                typeof(DolocTown.ItemFarmingGun).GetConstructor(new[] { typeof(DolocTown.Config.Item.ItemInfo), typeof(int) })!,
                typeof(DolocTown.ItemFarmingGun).GetConstructor(new[] { typeof(string), typeof(int), typeof(DolocTown.LinearInventory) })!,
                typeof(DolocTown.ItemFarmingGun).GetMethod("OnUseAsTool")!,
                typeof(DolocTown.ItemFarmingGun).GetMethod("RefreshCellTip")!,
                typeof(DolocTown.ItemFarmingGun).GetMethod("OpenContainerUI")!
            };
            foreach (string name in new[] { "HandlePlaceToOtherSide", "HandleSwapOneItem", "OnBackpackItemClick",
                "OnContainerItemClick", "HandleSwapHalfItems", "PutAll", "PutMax", "GetTipInBackpack", "GetTipInContainer" })
                list.Add(typeof(DolocTown.FarmingGunUiState).GetMethod(name)!);
            list.Add(typeof(DolocTown.ItemFarmingGun).GetMethod("InitCellTip")!);
            return list.ToArray();
        }

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
                NativeForwardingAndAllSlotPreview();
                CostRequiresCurrentThreeParameterContract();
                RangePreservesNativeAreaAndCancelsPendingBatch();
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



        private static void FiveTargetsResolveAndProductionCallbacksExecute()
        {
            ResetOwners();
            var runtime = new StrongPlantingGunNativeRuntime(NullMonitor.Instance);
            var older = new DolocTown.ItemFarmingGun("native-before-enable",1,new DolocTown.LinearInventory(1));
            runtime.Configure("constructor fixture");
            older.OpenContainerUI();
            Assert(older.inventory.capacity == 4,"Direct UI entry did not prepare a previously constructed one-slot gun.");
            AssertOwnerCount(ProductOwner, PatchCount);
            var ordinary = new DolocTown.ItemFarmingGun(new DolocTown.Config.Item.ItemInfo(), 1);
            var wide = new DolocTown.ItemFarmingGun("wide", 1, new DolocTown.LinearInventory(7));
            var held = new DolocTown.ItemSeed { name = "held", count = 9 };
            wide.inventory.SetItem(6, held);
            Assert(ordinary.inventory.capacity == 4 && wide.inventory.capacity == 7 && wide.func.Capacity == 4,
                "Native constructors did not expand four slots and preserve wider capacity.");
            Assert(runtime.CapacitySnapshotCount == 1 && ReferenceEquals(ordinary.func, wide.func), "Shared function snapshot duplicated.");
            runtime.DeactivateOwner("constructor cleanup");
            Assert(ordinary.func.Capacity == 1 && wide.inventory.capacity == 7 && ReferenceEquals(wide.inventory.Read(6), held),
                "Cleanup failed capacity restore or shrank held inventory.");
            AssertOwnerCount(ProductOwner, 0); AssertLifecycleZero();
        }

        private static void CompatibilityFirstFailsClosed()
        {
            foreach (string owner in new[] { CompatibilityOwner, StrongPlantingGunProductContract.LegacyProductOwner })
            {
                ResetOwners(); InstallOwner(owner, Targets);
                var runtime = new StrongPlantingGunNativeRuntime(NullMonitor.Instance);
                AssertThrows<InvalidOperationException>(() => runtime.Configure("conflict"), "Old owner overlap accepted.");
                AssertOwnerCount(ProductOwner, 0); AssertOwnerCount(owner, PatchCount); Cleanup(owner);
            }
        }

        private static void AtomicRollbackRemovesEveryInstalledTarget()
        {
            for (int failAt = 1; failAt <= PatchCount; failAt++)
            {
                ResetOwners(); int boundary = failAt;
                var runtime = new StrongPlantingGunNativeRuntime(NullMonitor.Instance, ordinal => ordinal != boundary);
                AssertThrows<InvalidOperationException>(() => runtime.Configure("rollback " + boundary), "Partial install did not surface failure.");
                AssertOwnerCount(ProductOwner, 0);
                Assert(runtime.InstalledPatchCount == 0, "Partial install left an exact owner patch.");
                AssertLifecycleZero();
            }
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
                () => runtime.Configure("failed rollback"),
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
                        "roots=1;pendingBatches=0;pendingCallbacks=0",
                        StringComparison.Ordinal),
                "Failed rollback published a false zero lifecycle summary: " +
                StrongPlantingGunCallbacks.LastLifecycleSummary);
            AssertThrows<InvalidOperationException>(
                () => runtime.Configure("partial residue retry"),
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
            runtime.Configure("unrelated co-owner");
            AssertOwnerCount(ProductOwner, PatchCount);
            AssertOwnerCount(UnrelatedOwner, PatchCount);

            runtime.DeactivateOwner("exact owner");
            AssertOwnerCount(ProductOwner, 0);
            AssertOwnerCount(UnrelatedOwner, PatchCount);
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
            runtime.Configure("failed deactivation");
            AssertOwnerCount(ProductOwner, PatchCount);
            AssertThrows<AggregateException>(
                () => runtime.DeactivateOwner(
                    "failed deactivation"),
                "A real deactivation unpatch failure was not surfaced.");
            AssertOwnerCount(ProductOwner, PatchCount);
            Assert(
                runtime.InstalledPatchCount == PatchCount &&
                runtime.Status == "cleanup-failed-owned" &&
                StrongPlantingGunCallbacks.LastLifecycleSummary
                    .IndexOf(
                        "callbacks=0;hooks=15",
                        StringComparison.Ordinal) >= 0 &&
                StrongPlantingGunCallbacks.LastLifecycleSummary
                    .EndsWith(
                        "roots=1;pendingBatches=0;pendingCallbacks=0",
                        StringComparison.Ordinal),
                "Failed deactivation published a false state instead of fifteen real residual owners: status=" +
                runtime.Status +
                "; summary=" +
                StrongPlantingGunCallbacks.LastLifecycleSummary);
            Cleanup(ProductOwner);
        }





        private static void
            ConfigTitleAndDisposeLifecycleReachZero()
        {
            ResetOwners();
            var runtime =
                new StrongPlantingGunNativeRuntime(
                    NullMonitor.Instance);
            runtime.Configure("config enabled");
            AssertOwnerCount(ProductOwner, PatchCount);
            runtime.DeactivateOwner("official owner disabled");
            AssertOwnerCount(ProductOwner, 0);
            AssertLifecycleZero();

            runtime.Configure("same process restart");
            AssertOwnerCount(ProductOwner, PatchCount);
            runtime.SuspendForTitle("fixture title");
            AssertOwnerCount(ProductOwner, 0);
            AssertLifecycleZero();

            runtime.Configure("post-title restart");
            AssertOwnerCount(ProductOwner, PatchCount);
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
            runtime.Configure("pre-title");
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
                runtime.ConfigureLoadedSaveBoundary("SaveLoaded");
            Assert(
                prepared == 1 &&
                jsonGun.inventory.capacity == 4 &&
                jsonGun.func.Capacity == 4 &&
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
            AssertOwnerCount(ProductOwner, PatchCount);

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
                runtime.ConfigureLoadedSaveBoundary("SaveLoaded failing scan");
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

        private static void NativeForwardingAndAllSlotPreview()
        {
            ResetOwners();
            var runtime = new StrongPlantingGunNativeRuntime(NullMonitor.Instance);
            runtime.Configure("native forwarding");
            var ui = new DolocTown.FarmingGunUiState();
            foreach (MethodBase target in Targets.Skip(5).Take(9))
            {
                object? result = target.Invoke(ui, target.GetParameters().Length == 0 ? null : new object[] { 2 });
                Assert(ui.LastNativeOperation == target.Name, "Native base operation not reached: " + target.Name);
                if (result is string[] tips) Assert(tips.SequenceEqual(new[] { "native-tip" }), "Native operation tips lost.");
                var baseMethod = typeof(DolocTown.ContainerBaseUiState).GetMethod(target.Name)!;
                Assert(Harmony.GetPatchInfo(baseMethod)?.Owners.Contains(ProductOwner) != true, "Shared base was patched.");
            }
            var gun = new DolocTown.ItemFarmingGun(new DolocTown.Config.Item.ItemInfo(), 1);
            var context = new PreviewContext();
            runtime.SetBatchContext(context);
            DolocAPI.CurrentRoom = DolocAPI.archiveHandle = DolocAPI.agent = new object();
            DolocAPI.SelectedItem = gun;
            gun.inventory.SetItem(3, new DolocTown.ItemFilm { name = "film", count = 2 });
            gun.RefreshCellTip(); Assert(gun.cellTip.CellTipValid, "Empty first slot hid a qualified later material.");
            gun.inventory.SetItem(0, new DolocTown.ItemSeed { name = "seed", count = 2 });
            gun.RefreshCellTip(); Assert(gun.cellTip.CellTipValid, "Rejected first material hid another material.");
            gun.CheckCanInteractResult = false;
            gun.RefreshCellTip(); Assert(!gun.cellTip.CellTipValid, "Rejected native qualification left preview valid.");
            int queries = gun.EquipmentQueryCount;
            gun.cellTip.CellTipValid = true;
            DolocAPI.SelectedItem = new object();
            gun.RefreshCellTip();
            Assert(gun.EquipmentQueryCount == queries && gun.cellTip.CellTipValid, "A stale gun callback queried terrain or overwrote the shared preview.");
            DolocAPI.SelectedItem = gun;
            context.Snapshot = new DtmRuntimeContextSnapshot(Guid.Empty, 2, DtmRuntimePhase.LoadingWorld, 1, null);
            gun.RefreshCellTip();
            Assert(gun.EquipmentQueryCount == queries && gun.cellTip.CellTipValid, "Loading-world preview queried terrain or changed the tip.");
            context.Snapshot = new DtmRuntimeContextSnapshot(Guid.Empty, 3, DtmRuntimePhase.WorldReady, 1, 2);
            DolocAPI.CurrentRoom = null;
            gun.RefreshCellTip();
            Assert(gun.EquipmentQueryCount == queries, "Preview queried missing native room.");
            DolocAPI.CurrentRoom = new object(); gun.CheckCanInteractResult = true;
            gun.RefreshCellTip();
            Assert(gun.EquipmentQueryCount == queries + 1 && gun.cellTip.CellTipValid, "Ready selected gun failed to resume material preview.");
            context.Snapshot = new DtmRuntimeContextSnapshot(Guid.Empty, 4, DtmRuntimePhase.LoadingWorld, 1, null);
            int count = gun.inventory.TotalCount();
            gun.OnUseAsTool();
            Assert(gun.OriginalToolUseCount == 0 && gun.inventory.TotalCount() == count, "Missing world context performed action or cost.");
            runtime.DeactivateOwner("native forwarding cleanup");
            ui.PutAll(); Assert(ui.LastNativeOperation == "legacy-PutAll", "Owner cleanup did not restore original override.");
            Assert(gun.inventory.capacity == 4 && gun.func.Capacity == 1, "Cleanup shrank held inventory.");
            AssertLifecycleZero();
        }

        private sealed class PreviewContext : IDtmRuntimeContext, IDisposable
        {
            public DtmRuntimeContextSnapshot Snapshot { get; set; } = new DtmRuntimeContextSnapshot(Guid.Empty, 1, DtmRuntimePhase.WorldReady, 1, 1);
            public bool IsMainThread => true;
            public IDisposable Subscribe(Action<DtmRuntimeContextSnapshot> changed) => this;
            public void Dispose() { }
        }

        private static void CostRequiresCurrentThreeParameterContract()
        {
            var runtime = new StrongPlantingGunNativeRuntime(NullMonitor.Instance);
            MethodInfo cost = typeof(StrongPlantingGunNativeRuntime).GetMethod("TryCostAtIndex", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var inventory = new DolocTown.LinearInventory(1);
            inventory.SetItem(0, new DolocTown.ItemSeed { name = "seed", count = 1 });
            Assert(cost.Invoke(runtime, new object[] { inventory, 0, 1, true }) is true && inventory.LastCostExactItem && inventory.TotalCount() == 0,
                "The current cost contract did not receive exactItem or consume exactly one item.");
            Assert(cost.Invoke(runtime, new object[] { inventory, 0, 1, true }) is false && inventory.TotalCount() == 0,
                "A rejected native cost did not remain false with unchanged inventory.");
            var unsupported = new TwoParameterCostInventory();
            try
            {
                cost.Invoke(runtime, new object[] { unsupported, 0, 1, true });
                throw new InvalidOperationException("Missing current cost signature was accepted.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is MissingMethodException missing && missing.Message.Contains("TryCostAtIndex(int,int,bool)")) { }
            Assert(unsupported.Calls == 0, "The unsupported two-parameter cost method was invoked.");
        }

        private sealed class TwoParameterCostInventory
        {
            internal int Calls;
            public bool TryCostAtIndex(int index, int count) { Calls++; return true; }
        }

        private static void RangePreservesNativeAreaAndCancelsPendingBatch()
        {
            ResetOwners();
            var runtime = new StrongPlantingGunNativeRuntime(NullMonitor.Instance);
            var context = new PreviewContext(); runtime.SetBatchContext(context);
            runtime.Configure("range fixture");
            var gun = new DolocTown.ItemFarmingGun(new DolocTown.Config.Item.ItemInfo(), 1);
            DolocAPI.CurrentRoom = DolocAPI.agent = DolocAPI.archiveHandle = new object(); DolocAPI.SelectedItem = gun;
            for (int n = 7; n <= 15; n++) {
                runtime.ApplyRange(n); gun.InitCellTip();
                Assert(gun.cellTip.CellSize.x == n && gun.cellTip.CellSize.y == n, "Native area read did not use integer range " + n);
                Assert(gun.func.Area.x == 7 && gun.func.Area.y == 7 && gun.func.Offset.x == -3 && gun.func.Offset.y == 0, "Range mutated shared native Area/Offset.");
            }
            var stale = new DolocTown.ItemFarmingGun(new DolocTown.Config.Item.ItemInfo(), 1) { cellTip = gun.cellTip };
            stale.InitCellTip(); Assert(gun.cellTip.CellSize.x == 15, "Old Init overwrote current shared size.");
            context.Snapshot = new DtmRuntimeContextSnapshot(Guid.Empty, 2, DtmRuntimePhase.LoadingWorld, 1, null);
            runtime.ApplyRange(8); gun.InitCellTip();
            Assert(gun.cellTip.CellSize.x == 15, "Loading Init changed the shared tip.");
            context.Snapshot = new DtmRuntimeContextSnapshot(Guid.Empty, 3, DtmRuntimePhase.WorldReady, 1, 2);
            gun.InitCellTip(); Assert(gun.cellTip.CellSize.x == 8, "Ready Init did not apply latest range.");
            Type batchType = typeof(StrongPlantingGunNativeRuntime).GetNestedType("ToolBatch", BindingFlags.NonPublic)!;
            object pending = Activator.CreateInstance(batchType, BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { gun, gun.inventory, DolocAPI.CurrentRoom, DolocAPI.archiveHandle, DolocAPI.agent, context.Snapshot, new object(), new object(), new object() }, null)!;
            int effects = 0; var callback = new StrongPlantingGunToolCallback(() => effects++);
            ((System.Collections.IList)batchType.GetField("Callbacks", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(pending)!).Add(callback);
            SetField(runtime, "batch", pending);
            runtime.ApplyRange(8); Assert(runtime.PendingBatchCount == 1 && callback.IsPending, "Unchanged effective range cancelled a batch.");
            runtime.ApplyRange(15); callback.Invoke(); callback.Invoke();
            Assert(runtime.PendingBatchCount == 0 && !callback.IsPending && effects == 0 && gun.cellTip.CellSize.x == 15, "Range change did not cancel old tickets before new preview.");
            runtime.DeactivateOwner("range cleanup");
            Assert(gun.func.Area.x == 7 && gun.func.Offset.x == -3 && gun.cellTip.CellSize.x == 7, "Owner cleanup did not restore native selected-gun preview.");
            AssertOwnerCount(ProductOwner, 0); AssertLifecycleZero();
        }

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
                "listeners=0;callbacks=0;hooks=0;cachedObjects=0;cachedMembers=0;capacitySnapshots=0;roots=0;pendingBatches=0;pendingCallbacks=0",
                "Lifecycle summary was not exact zero: " +
                StrongPlantingGunCallbacks.LastLifecycleSummary);

        private static void Cleanup(string owner) =>
            Harmony.UnpatchID(owner);

        private static void ResetOwners()
        {
            DolocAPI.archiveHandle = null;
            DolocAPI.CurrentRoom = DolocAPI.agent = DolocAPI.SelectedItem = null;
            Cleanup(ProductOwner);
            Cleanup(StrongPlantingGunProductContract.LegacyProductOwner);
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

    }
}
