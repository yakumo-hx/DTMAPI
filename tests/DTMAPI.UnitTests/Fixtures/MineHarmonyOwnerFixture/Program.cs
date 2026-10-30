using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DTMAPI.Abstractions;
using DTMAPI.Mine;
using HarmonyLib;

namespace DTMAPI.MineHarmonyOwnerFixture
{
    internal static class Program
    {
        private const string ProductOwner =
            "dtmapi.mod.dtmapi.minemod";
        private const string UnrelatedOwner =
            "dtmapi.tests.mine.unrelated";

        private static readonly MethodBase[] Targets =
        {
            typeof(DolocTown.EquipmentRenderer).GetMethod(
                "OnReuse",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException(
                    "EquipmentRenderer.OnReuse"),
            typeof(DolocTown.EquipmentBuilder).GetMethod(
                "CreateIndicator",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException(
                    "EquipmentBuilder.CreateIndicator"),
            typeof(DolocTown.EquipmentBuilder).GetMethod(
                "TurnIndicator",
                BindingFlags.Public | BindingFlags.Instance)
                ?? throw new MissingMethodException(
                    "EquipmentBuilder.TurnIndicator")
        };

        private static readonly HarmonyMethod FixturePostfix =
            new HarmonyMethod(
                typeof(Program).GetMethod(
                    nameof(ExactTargetPostfix),
                    BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingMethodException(
                    typeof(Program).FullName,
                    nameof(ExactTargetPostfix)));

        private static int Main()
        {
            try
            {
                ProductContractIsFixed();
                ThreeTargetsInstallAndDeactivateToZero();
                AtomicInstallFailureRollsBackEveryTarget();
                ExactOwnerCleanupPreservesUnrelatedPatches();
                ExactNativeOriginalsAndCycleSnapshotRestore();
                FailedNativeRestoreRemainsQueuedUntilRetry();
                ActivationRollbackFailureBlocksReactivationUntilCleanup();
                SchedulerIdentityPrunesAndNeverTransfersDueState();
                PreflightOrderingAndRollback();
                ZeroOutputWeightRemainsDisabled();
                Console.WriteLine(
                    "MineHarmonyOwnerFixture: OK");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                Cleanup(ProductOwner);
                Cleanup(UnrelatedOwner);
                return 1;
            }
        }

        private static void ProductContractIsFixed()
        {
            Assert(
                MineProductContract.FixedPowerCost == 10,
                "Mine power must remain fixed at the native 10-point threshold.");
            Assert(
                MineProductContract.ExpectedHookCount == 3,
                "Mine must own exactly three visual Hooks.");
            Assert(
                MineProductContract.HarmonyOwner ==
                    ProductOwner,
                "Mine canonical Harmony owner drifted.");
        }

        private static void
            ThreeTargetsInstallAndDeactivateToZero()
        {
            ResetOwners();
            var installer =
                new MineHookInstaller(NullMonitor.Instance);
            var runtime =
                new MineNativeRuntime(
                    NullMonitor.Instance,
                    installer);
            installer.InstallAtomically(runtime);
            AssertOwnerCount(ProductOwner, 3);
            Assert(
                installer.InstalledPatchCount == 3 &&
                MineCallbacks.AttachedCount == 1,
                "Mine did not publish all three exact-owner hooks and its single callback root.");
            installer.UnpatchOwnedHooks(runtime);
            AssertOwnerCount(ProductOwner, 0);
            Assert(
                installer.InstalledPatchCount == 0 &&
                MineCallbacks.AttachedCount == 0,
                "Mine deactivation did not reach exact zero.");
        }

        private static void
            AtomicInstallFailureRollsBackEveryTarget()
        {
            ResetOwners();
            var installer =
                new MineHookInstaller(
                    NullMonitor.Instance,
                    index => index != 2);
            var runtime =
                new MineNativeRuntime(
                    NullMonitor.Instance,
                    installer);
            AssertThrows<InvalidOperationException>(
                () => installer.InstallAtomically(runtime),
                "Injected second-target failure did not fail Mine activation.");
            AssertOwnerCount(ProductOwner, 0);
            Assert(
                installer.InstalledPatchCount == 0 &&
                MineCallbacks.AttachedCount == 0,
                "Mine atomic install rollback retained a Hook or callback root.");
        }

        private static void
            ExactOwnerCleanupPreservesUnrelatedPatches()
        {
            ResetOwners();
            InstallOwner(UnrelatedOwner);
            var installer =
                new MineHookInstaller(NullMonitor.Instance);
            var runtime =
                new MineNativeRuntime(
                    NullMonitor.Instance,
                    installer);
            installer.InstallAtomically(runtime);
            AssertOwnerCount(ProductOwner, 3);
            AssertOwnerCount(UnrelatedOwner, 3);
            installer.UnpatchOwnedHooks(runtime);
            AssertOwnerCount(ProductOwner, 0);
            AssertOwnerCount(UnrelatedOwner, 3);
            Cleanup(UnrelatedOwner);
        }

        private static void
            ExactNativeOriginalsAndCycleSnapshotRestore()
        {
            var runtime =
                new MineNativeRuntime(NullMonitor.Instance);
            var native = new MutableNative
            {
                Value = 17
            };
            Invoke(
                runtime,
                "CaptureMemberRestore",
                "fixture-value",
                native,
                new[] { "Value" });
            native.Value = 99;

            var map = new Hashtable
            {
                ["existing"] = "original"
            };
            Invoke(
                runtime,
                "CaptureDictionaryEntryRestore",
                "fixture-map",
                map,
                "existing");
            map["existing"] = "changed";

            var list = new ArrayList();
            object inserted = new object();
            Invoke(
                runtime,
                "CaptureListAdditionRestore",
                "fixture-list",
                list,
                inserted);
            list.Add(inserted);
            Invoke(runtime, "RestoreNativeMutations");
            Assert(
                native.Value == 17 &&
                Equals(map["existing"], "original") &&
                !list.Contains(inserted),
                "Mine exact recipe/tech restoration did not restore the original member, map entry and injected list row.");

            var transform = new MutableTransform
            {
                localScale = new MutableScale(1d, 1d, 1d)
            };
            MutableScale originalScale = transform.localScale;
            Invoke(runtime, "CaptureScale", transform);
            transform.localScale =
                new MutableScale(2d, 2d, 1d);
            Invoke(runtime, "RestoreVisualScales");
            Assert(
                ReferenceEquals(
                    transform.localScale,
                    originalScale),
                "Mine visual cleanup did not restore the exact original scale object.");

            var equipment = new MutableEquipment
            {
                IElectronicComponent =
                    new MutablePowerComponent
                    {
                        power = 42
                    },
                inventory =
                    new MutableInventory(
                        "coal",
                        null)
            };
            object snapshot =
                InvokeStatic(
                    typeof(MineNativeRuntime),
                    "CaptureMachineCycleSnapshot",
                    equipment);
            equipment.IElectronicComponent.power = 7;
            equipment.inventory.Items[0] = "iron_ore";
            equipment.inventory.Items[1] = "copper_ore";
            InvokeStatic(
                typeof(MineNativeRuntime),
                "RestoreMachineCycleSnapshot",
                snapshot);
            Assert(
                equipment.IElectronicComponent.power == 42 &&
                Equals(
                    equipment.inventory.Items[0],
                    "coal") &&
                equipment.inventory.Items[1] == null,
                "Mine failed-output rollback did not restore exact native power and inventory.");
        }

        private static void
            FailedNativeRestoreRemainsQueuedUntilRetry()
        {
            var runtime =
                new MineNativeRuntime(NullMonitor.Instance);
            var stable = new MutableNative
            {
                Value = 17
            };
            var flaky = new FaultingNative(23);

            Invoke(
                runtime,
                "CaptureMemberRestore",
                "fixture-stable",
                stable,
                new[] { "Value" });
            Invoke(
                runtime,
                "CaptureMemberRestore",
                "fixture-flaky",
                flaky,
                new[] { "Value" });
            stable.Value = 91;
            flaky.Value = 92;
            flaky.FailNextWrite = true;
            SetField(runtime, "active", true);

            AssertThrows<AggregateException>(
                () => Invoke(
                    runtime,
                    "DeactivateSession",
                    "fault-injected first cleanup"),
                "Mine cleanup should fail when one exact native restoration is fault-injected.");
            Assert(
                stable.Value == 17 &&
                flaky.Value == 92 &&
                runtime.PendingNativeRestoreCount == 1 &&
                runtime.Status == "cleanup-failed",
                "Mine cleanup did not remove the successful restoration while retaining the failed original and truthful cleanup state.");

            stable.Value = 123;
            Invoke(
                runtime,
                "DeactivateSession",
                "fault-injected retry");
            Assert(
                stable.Value == 123 &&
                flaky.Value == 23 &&
                runtime.PendingNativeRestoreCount == 0,
                "Mine cleanup retry did not restore only the queued failure and reach an empty native restoration ledger.");
        }

        private static void
            ActivationRollbackFailureBlocksReactivationUntilCleanup()
        {
            ResetOwners();
            var flaky = new FaultingNative(23);
            MineNativeRuntime? runtime = null;
            bool captured = false;
            runtime = new MineNativeRuntime(
                NullMonitor.Instance,
                new MineHookInstaller(NullMonitor.Instance));
            var config = new MineConfig
            {
                Enabled = true,
                UseOilRecipeReplacement = false
            };
            runtime.InitializeAtEntry(
                config,
                new FixtureManifest(),
                () =>
                {
                    if (!captured)
                    {
                        Invoke(
                            runtime,
                            "CaptureMemberRestore",
                            "fixture-activation-flaky",
                            flaky,
                            new[] { "Value" });
                        flaky.Value = 92;
                        flaky.FailWrites = true;
                        captured = true;
                    }
                    return false;
                });

            AssertThrows<AggregateException>(
                () => runtime.SaveLoaded(3),
                "Mine activation should fail when native content is unavailable and exact rollback is fault-injected.");
            Assert(
                flaky.Value == 92 &&
                runtime.PendingNativeRestoreCount == 1 &&
                runtime.Status == "cleanup-failed" &&
                runtime.InstalledPatchCount == 0 &&
                !runtime.IsActive,
                "Mine activation failure did not retain the failed exact restore while rolling hooks and activity back to zero.");

            AssertThrows<AggregateException>(
                () => runtime.Configure(
                    config,
                    new FixtureManifest(),
                    () => false,
                    "fault-injected reactivation"),
                "Mine reactivation should be rejected while the original native cleanup is still failing.");
            Assert(
                runtime.PendingNativeRestoreCount == 1 &&
                runtime.Status == "cleanup-failed" &&
                runtime.InstalledPatchCount == 0 &&
                !runtime.IsActive,
                "Mine reactivation reached hooks or active state before the unresolved original was restored.");

            AssertThrows<AggregateException>(
                () => runtime.ReturnedToTitle(
                    "fault-injected title cleanup"),
                "Mine title cleanup should surface an unresolved exact native restoration.");
            runtime.Configure(
                config,
                new FixtureManifest(),
                () => false,
                "refresh after failed cleanup");
            Assert(
                runtime.PendingNativeRestoreCount == 1 &&
                runtime.Status == "cleanup-failed",
                "Mine configuration refresh overwrote cleanup-failed with waiting-for-save while an exact restore remained queued.");

            flaky.FailWrites = false;
            runtime.ReturnedToTitle("fault-injected cleanup retry");
            Assert(
                flaky.Value == 23 &&
                runtime.PendingNativeRestoreCount == 0 &&
                runtime.Status == "waiting-for-save" &&
                runtime.InstalledPatchCount == 0 &&
                !runtime.IsActive,
                "Mine cleanup retry did not restore the activation original exactly and return to a clean waiting state.");
        }

        private static void ZeroOutputWeightRemainsDisabled()
        {
            var definition = new DTMAPI.Mine.MineDefinition
            {
                OutputRules = new[]
                {
                    new DTMAPI.Mine.MineOutputRule
                    {
                        ItemId = "coal",
                        Weight = 0d
                    }
                }
            };
            var normalized =
                (DTMAPI.Mine.MineDefinition)InvokeStatic(
                    typeof(MineNativeRuntime),
                    "NormalizeMineDefinition",
                    definition);
            Assert(
                normalized.OutputRules.Count == 1 &&
                normalized.OutputRules[0].Weight == 0d,
                "Mine normalization revived a zero-weight output.");
        }

        private static void
            SchedulerIdentityPrunesAndNeverTransfersDueState()
        {
            var scheduler = new MineSessionScheduler();
            var first = new object();
            var second = new object();
            var replacementAtReusedIndex = new object();

            scheduler.BeginAuthoritativeScan();
            scheduler.Observe(
                first,
                100,
                12,
                out MineScheduleEntry firstEntry);
            scheduler.Observe(
                second,
                100,
                12,
                out MineScheduleEntry secondEntry);
            Assert(
                scheduler.EndAuthoritativeScan() == 0 &&
                scheduler.Count == 2 &&
                firstEntry.NextDueTotalTus == 112 &&
                secondEntry.NextDueTotalTus == 112,
                "Two observed Mines did not receive independent session due entries.");

            scheduler.BeginAuthoritativeScan();
            scheduler.Observe(
                first,
                105,
                12,
                out MineScheduleEntry movedSameIdentity);
            Assert(
                scheduler.EndAuthoritativeScan() == 1 &&
                scheduler.Count == 1 &&
                ReferenceEquals(
                    firstEntry,
                    movedSameIdentity) &&
                movedSameIdentity.NextDueTotalTus == 112,
                "A complete authoritative scan did not prune the removed Mine or retain the same moved object identity.");

            scheduler.BeginAuthoritativeScan();
            scheduler.Observe(
                first,
                106,
                12,
                out _);
            scheduler.Observe(
                replacementAtReusedIndex,
                106,
                12,
                out MineScheduleEntry replacement);
            Assert(
                scheduler.EndAuthoritativeScan() == 0 &&
                scheduler.Count == 2 &&
                replacement.NextDueTotalTus == 118 &&
                !ReferenceEquals(
                    replacement,
                    secondEntry),
                "A replacement Mine inherited stale due state from a removed/index-reused object.");

            scheduler.BeginAuthoritativeScan();
            scheduler.Observe(
                replacementAtReusedIndex,
                107,
                12,
                out _);
            Assert(
                scheduler.EndAuthoritativeScan() == 1 &&
                scheduler.Count == 1 &&
                !scheduler.Contains(first),
                "Dismantling the remaining original Mine did not remove its scheduler root.");
        }

        private static void PreflightOrderingAndRollback()
        {
            var runtime =
                new MineNativeRuntime(NullMonitor.Instance);
            SetField(
                runtime,
                "mineDefinition",
                new MineDefinition
                {
                    CycleMinutes = 120,
                    OutputRules = new[]
                    {
                        new MineOutputRule
                        {
                            ItemId = "coal",
                            DisplayName = "Coal",
                            Weight = 1d,
                            MinCount = 1,
                            MaxCount = 1
                        }
                    }
                });

            DolocTown.ItemFactory.Reset();
            var low = new CycleEquipment(
                power: 5f,
                slots: new object?[] { null, null });
            MineScheduleEntry lowEntry =
                NewEntry(low);
            Assert(
                !RunCycle(runtime, low, lowEntry) &&
                !RunCycle(runtime, low, lowEntry) &&
                low.IElectronicComponent.LaunchCount == 0 &&
                low.inventory.CopyCount == 0 &&
                low.inventory.OverwriteCount == 0 &&
                DolocTown.ItemFactory.GenerateCount == 0,
                "Low power selected/generated output, copied inventory or called Launch before preflight.");

            DolocTown.ItemFactory.Reset();
            var full = new CycleEquipment(
                power: 20f,
                slots: new object?[]
                {
                    new DolocTown.Item("coal", 1)
                });
            MineScheduleEntry fullEntry =
                NewEntry(full);
            Assert(
                !RunCycle(runtime, full, fullEntry) &&
                !RunCycle(runtime, full, fullEntry) &&
                full.IElectronicComponent.LaunchCount == 0 &&
                full.inventory.CopyCount == 0 &&
                DolocTown.ItemFactory.GenerateCount == 0,
                "Full storage generated output, copied inventory or called Launch before preflight.");

            DolocTown.ItemFactory.Reset();
            var rejected = new CycleEquipment(
                power: 20f,
                slots: new object?[] { null, null })
            {
                AllowContent = false
            };
            MineScheduleEntry rejectedEntry =
                NewEntry(rejected);
            Assert(
                !RunCycle(runtime, rejected, rejectedEntry) &&
                DolocTown.ItemFactory.GenerateCount == 1 &&
                !RunCycle(runtime, rejected, rejectedEntry) &&
                DolocTown.ItemFactory.GenerateCount == 1 &&
                rejected.IElectronicComponent.LaunchCount == 0 &&
                rejected.inventory.CopyCount == 0,
                "Unchanged rejected output was reconstructed on every poll or reached native mutation.");

            DolocTown.ItemFactory.Reset();
            var success = new CycleEquipment(
                power: 20f,
                slots: new object?[] { null, null });
            Assert(
                RunCycle(runtime, success, NewEntry(success)) &&
                success.IElectronicComponent.LaunchCount == 1 &&
                Math.Abs(
                    success.IElectronicComponent.power -
                    10f) < 0.001f &&
                success.inventory.CopyCount == 1 &&
                success.inventory.OverwriteCount == 0 &&
                success.inventory.filledCount == 1 &&
                DolocTown.ItemFactory.GenerateCount == 1,
                "Successful Mine cycle did not consume exactly one fixed-power transaction and place exactly one prepared item.");

            DolocTown.ItemFactory.Reset();
            var placementFailure = new CycleEquipment(
                power: 20f,
                slots: new object?[] { null, null });
            placementFailure.inventory.FailPlacement = true;
            Assert(
                !RunCycle(
                    runtime,
                    placementFailure,
                    NewEntry(placementFailure)) &&
                placementFailure.IElectronicComponent.LaunchCount == 1 &&
                Math.Abs(
                    placementFailure
                        .IElectronicComponent.power -
                    20f) < 0.001f &&
                placementFailure.inventory.CopyCount == 1 &&
                placementFailure.inventory.OverwriteCount == 1 &&
                placementFailure.inventory.filledCount == 0,
                "Post-Launch placement failure did not restore exact power and inventory.");
        }

        private static MineScheduleEntry NewEntry(
            object equipment) =>
            new MineScheduleEntry(
                equipment,
                0,
                0);

        private static bool RunCycle(
            MineNativeRuntime runtime,
            CycleEquipment equipment,
            MineScheduleEntry entry)
        {
            object[] args = { equipment, entry, string.Empty };
            MethodInfo method = FindMethod(
                typeof(MineNativeRuntime),
                "TryRunMineProductionCycle",
                3);
            try
            {
                return (bool)(method.Invoke(runtime, args) ??
                    false);
            }
            catch (TargetInvocationException ex)
                when (ex.InnerException != null)
            {
                throw ex.InnerException;
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

        private static object Invoke(
            object instance,
            string name,
            params object[] args)
        {
            MethodInfo method =
                FindMethod(
                    instance.GetType(),
                    name,
                    args.Length);
            try
            {
                return method.Invoke(instance, args)
                    ?? new object();
            }
            catch (TargetInvocationException ex)
                when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static object InvokeStatic(
            Type type,
            string name,
            params object[] args)
        {
            MethodInfo method =
                FindMethod(type, name, args.Length);
            try
            {
                return method.Invoke(null, args)
                    ?? new object();
            }
            catch (TargetInvocationException ex)
                when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static MethodInfo FindMethod(
            Type type,
            string name,
            int parameterCount) =>
            type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Static)
                .Single(method =>
                    method.Name == name &&
                    method.GetParameters().Length ==
                        parameterCount);

        private static void InstallOwner(string owner)
        {
            var harmony = new Harmony(owner);
            foreach (MethodBase target in Targets)
                harmony.Patch(target, postfix: FixturePostfix);
        }

        private static void ResetOwners()
        {
            Cleanup(ProductOwner);
            Cleanup(UnrelatedOwner);
        }

        private static void Cleanup(string owner) =>
            Harmony.UnpatchID(owner);

        private static void AssertOwnerCount(
            string owner,
            int expected)
        {
            int actual = Targets.Sum(target =>
            {
                Patches? patches =
                    Harmony.GetPatchInfo(target);
                if (patches == null)
                    return 0;
                return patches.Prefixes.Count(p =>
                           p.owner == owner) +
                       patches.Postfixes.Count(p =>
                           p.owner == owner) +
                       patches.Transpilers.Count(p =>
                           p.owner == owner) +
                       patches.Finalizers.Count(p =>
                           p.owner == owner);
            });
            Assert(
                actual == expected,
                "Harmony owner " + owner +
                " expected " + expected +
                " patches but observed " + actual + ".");
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

        private sealed class MutableNative
        {
            public int Value { get; set; }
        }

        private sealed class FaultingNative
        {
            private int value;

            internal FaultingNative(int value) =>
                this.value = value;

            internal bool FailNextWrite { get; set; }
            internal bool FailWrites { get; set; }

            public int Value
            {
                get => value;
                set
                {
                    if (FailWrites || FailNextWrite)
                    {
                        FailNextWrite = false;
                        throw new InvalidOperationException(
                            "Injected native restoration failure.");
                    }
                    this.value = value;
                }
            }
        }

        private sealed class MutableScale
        {
            internal MutableScale(
                double x,
                double y,
                double z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public double x;
            public double y;
            public double z;
        }

        private sealed class MutableTransform
        {
            public MutableScale localScale =
                new MutableScale(1d, 1d, 1d);
        }

        private sealed class MutablePowerComponent
        {
            public int power;
        }

        private sealed class MutableInventory
        {
            internal MutableInventory(params object?[] items) =>
                Items = items;

            public object?[] Items { get; private set; }

            public MutableInventory Copy() =>
                new MutableInventory(
                    (object?[])Items.Clone());

            public void Overwrite(
                MutableInventory source,
                bool notify) =>
                Items =
                    (object?[])source.Items.Clone();
        }

        private sealed class MutableEquipment
        {
            public MutablePowerComponent
                IElectronicComponent =
                    new MutablePowerComponent();

            public MutableInventory inventory =
                new MutableInventory();
        }

        private sealed class CyclePowerComponent
        {
            internal CyclePowerComponent(float initialPower) =>
                power = initialPower;

            public float power;
            public int LaunchCount { get; private set; }

            public bool Launch()
            {
                LaunchCount++;
                if (power <
                    MineProductContract.FixedPowerCost)
                {
                    return false;
                }
                power -= MineProductContract.FixedPowerCost;
                return true;
            }
        }

        private sealed class CycleInventory
        {
            internal CycleInventory(object?[] items) =>
                Items = (object?[])items.Clone();

            public object?[] Items { get; private set; }
            public int CopyCount { get; private set; }
            public int OverwriteCount { get; private set; }
            public bool FailPlacement { get; set; }
            public int capacity => Items.Length;
            public int filledCount =>
                Items.Count(item => item != null);
            public int emptyCount =>
                Items.Count(item => item == null);
            public int FirstEmptyIndex =>
                Array.FindIndex(
                    Items,
                    item => item == null);

            public CycleInventory Copy()
            {
                CopyCount++;
                return new CycleInventory(Items);
            }

            public void Overwrite(
                CycleInventory source,
                bool notify)
            {
                OverwriteCount++;
                Items =
                    (object?[])source.Items.Clone();
            }

            public object? PlaceItemAt(
                int index,
                object item)
            {
                if (FailPlacement)
                    return item;
                if (index < 0 ||
                    index >= Items.Length ||
                    Items[index] != null)
                {
                    return item;
                }
                Items[index] = item;
                return null;
            }
        }

        private sealed class CycleEquipment
        {
            internal CycleEquipment(
                float power,
                object?[] slots)
            {
                IElectronicComponent =
                    new CyclePowerComponent(power);
                inventory =
                    new CycleInventory(slots);
            }

            public CyclePowerComponent
                IElectronicComponent { get; }

            public CycleInventory inventory { get; }

            public bool AllowContent { get; set; } = true;

            public int lineCapacity => 4;

            public bool ContentFilter(object item) =>
                AllowContent;
        }

        private sealed class FixtureManifest : IManifest
        {
            public string Name => "Mine Fixture";
            public string Author => "DTMAPI";
            public string Version => "1.0.0";
            public string Description => string.Empty;
            public string UniqueID => "DTMAPI.MineMod";
            public string EntryDll => "DTMAPI.Mine.dll";
            public string EntryType => "DTMAPI.Mine.ModEntry";
            public string MinimumDTMApiVersion => "0.5.5";
            public string MinimumGameVersion => string.Empty;
            public string Type => "CodeMod";
            public IReadOnlyList<IManifestDependency> Dependencies =>
                Array.Empty<IManifestDependency>();
            public IReadOnlyList<string> UpdateKeys =>
                Array.Empty<string>();
        }
    }
}
