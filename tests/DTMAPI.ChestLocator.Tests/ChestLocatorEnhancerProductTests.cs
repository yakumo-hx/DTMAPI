using System;
using System.Collections.Generic;
using System.IO;
using DTMAPI.ChestLocatorEnhancer;

namespace DTMAPI.UnitTests
{
    internal static class ChestLocatorEnhancerProductTests
    {
        internal static void RunAll()
        {
            EnabledAndDisabledStatesChooseOneOwnershipAction();
            CompatibilityFirstActivationIsRejected();
            FailedUnpatchObservationCannotPublishZero();
            AdvancedIdentityAndExactHookOwnerStayFrozen();
            NativeGraphTraversalExecutesCaseShelfAutoUseBoxAndDedup();
            RepeatedTraversalCachesMetadataAndLimitsLogs();
        }

        private static void EnabledAndDisabledStatesChooseOneOwnershipAction()
        {
            int installs = 0;
            int unpatches = 0;
            ChestLocatorHookOwnership.ApplyEnabledState(
                enabled: true,
                () => installs++,
                () => unpatches++);
            ChestLocatorHookOwnership.ApplyEnabledState(
                enabled: false,
                () => installs++,
                () => unpatches++);

            Assert(installs == 1 && unpatches == 1,
                "Enabled must choose one install; disabled must choose one exact-owner unpatch.");
        }

        private static void CompatibilityFirstActivationIsRejected()
        {
            Assert(ChestLocatorHookOwnership.DecideInstall(
                       compatibilityOwnerPresent: true,
                       productOwnerPresent: false) ==
                   ChestLocatorInstallDecision.RejectCompatibilityOwner,
                "Compatibility-first startup must reject before ProductNative installation.");
            Assert(ChestLocatorHookOwnership.DecideInstall(
                       compatibilityOwnerPresent: false,
                       productOwnerPresent: true) ==
                   ChestLocatorInstallDecision.RejectDuplicateProductOwner,
                "A duplicate ProductNative owner must reject instead of stacking another Postfix.");
            Assert(ChestLocatorHookOwnership.DecideInstall(
                       compatibilityOwnerPresent: false,
                       productOwnerPresent: false) ==
                   ChestLocatorInstallDecision.Install,
                "An unowned target must admit exactly one ProductNative install.");
        }

        private static void FailedUnpatchObservationCannotPublishZero()
        {
            ChestLocatorObservedHookState retained =
                ChestLocatorHookOwnership.FromExactOwnerObservation(
                    productOwnerPresent: true);
            ChestLocatorObservedHookState cleared =
                ChestLocatorHookOwnership.FromExactOwnerObservation(
                    productOwnerPresent: false);

            Assert(retained.IsInstalled &&
                   retained.InstalledPatchCount == 1,
                "A failed unpatch with the exact owner still observed must retain installed/count=1.");
            Assert(!cleared.IsInstalled &&
                   cleared.InstalledPatchCount == 0,
                "Zero may be published only after the exact owner is observed absent.");
        }

        private static void AdvancedIdentityAndExactHookOwnerStayFrozen()
        {
            string root = ProductRoot();
            string manifest = File.ReadAllText(Path.Combine(root, "manifest.json"));
            string author = File.ReadAllText(Path.Combine(root, "dtmapi.author.json"));
            string hook = File.ReadAllText(Path.Combine(root, "src", "Native", "ChestLocatorEnhancerHookInstaller.cs"));
            string callback = File.ReadAllText(Path.Combine(root, "src", "Native", "ChestLocatorEnhancerCallbacks.cs"));
            string entry = File.ReadAllText(Path.Combine(root, "src", "ModEntry.cs"));

            Assert(manifest.Contains("\"UniqueID\": \"DTMAPI.ChestLocatorEnhancerMod\"", StringComparison.Ordinal) &&
                   manifest.Contains("\"EntryDll\": \"DTMAPI.ChestLocatorEnhancer.dll\"", StringComparison.Ordinal) &&
                   manifest.Contains("\"CodeModKind\": \"Advanced\"", StringComparison.Ordinal),
                "ChestLocatorEnhancer must retain the admitted Workshop identity and Advanced package DLL.");
            Assert(author.Contains("\"referencePolicyId\": \"doloctown-23762374-chestlocator-v1\"", StringComparison.Ordinal) &&
                   author.Contains("\"0Harmony\"", StringComparison.Ordinal) &&
                   author.Contains("\"Assembly-CSharp\"", StringComparison.Ordinal),
                "ChestLocatorEnhancer must use the one tracked 0.5.5 native reference policy.");
            Assert(hook.Contains("\"dtmapi.mod.dtmapi.chestlocatorenhancermod\"", StringComparison.Ordinal) &&
                   hook.Contains("\"DolocTown.GameData.ArchiveDataHandle\"", StringComparison.Ordinal) &&
                   hook.Contains("\"UnityEngine.Vector2Int\"", StringComparison.Ordinal) &&
                   Count(hook, "harmony.Patch(") == 1,
                "ChestLocatorEnhancer must own one Postfix on the exact three-argument inventory target.");
            Assert(!callback.Contains("TAnchor", StringComparison.Ordinal) &&
                   !callback.Contains("TArea", StringComparison.Ordinal) &&
                   !callback.Contains("__0", StringComparison.Ordinal) &&
                   !callback.Contains("__1", StringComparison.Ordinal),
                "The ProductNative Postfix must not bind and box unused Vector2Int arguments.");
            Assert(entry.IndexOf("ReadConfig<ChestLocatorEnhancerConfig>", StringComparison.Ordinal) <
                   entry.IndexOf("nativeRuntime.Configure(config, \"Entry\")", StringComparison.Ordinal),
                "Entry must read configuration before deciding whether native ownership is needed.");
        }

        private static void NativeGraphTraversalExecutesCaseShelfAutoUseBoxAndDedup()
        {
            TraversalFixture fixture = CreateTraversalFixture();
            ChestLocatorTraversalResult enabled =
                ChestLocatorInventoryTraversal.AppendSharedInventories(
                    fixture.Archive,
                    fixture.Root,
                    useBox: true,
                    includeSharedCases: true,
                    includeSharedStorageShelfBoxes: true,
                    nativeAutoUseBox: true,
                    fixture.Native);

            Assert(enabled.AppendedInventoryCount == 2 &&
                   enabled.SharedCaseCount == 1 &&
                   enabled.SharedStorageBoxCount == 1,
                "The executable graph must append one shared Case and one unique StorageShelf ItemBox inventory.");
            Assert(enabled.ScannedRootCount == 1 &&
                   enabled.ScannedEquipmentCount == 4,
                "Overlapping archive/root/farm seeds must scan one room graph once, while non-locator containers remain observed but excluded.");
            Assert(enabled.Inventories.Length == 3 &&
                   ReferenceEquals(enabled.Inventories.GetValue(0), fixture.Native[0]) &&
                   ReferenceEquals(enabled.Inventories.GetValue(1), fixture.CaseInventory) &&
                   ReferenceEquals(enabled.Inventories.GetValue(2), fixture.BoxInventory),
                "Widening must preserve native order and append inventories once by reference identity.");

            ChestLocatorTraversalResult nativeAutoUseBoxOff =
                ChestLocatorInventoryTraversal.AppendSharedInventories(
                    fixture.Archive,
                    fixture.Root,
                    useBox: true,
                    includeSharedCases: true,
                    includeSharedStorageShelfBoxes: true,
                    nativeAutoUseBox: false,
                    fixture.Native);
            Assert(nativeAutoUseBoxOff.AppendedInventoryCount == 1 &&
                   nativeAutoUseBoxOff.SharedCaseCount == 1 &&
                   nativeAutoUseBoxOff.SharedStorageBoxCount == 0,
                "Native autoUseBox=false must suppress StorageShelf ItemBox widening without suppressing shared Cases.");

            ChestLocatorTraversalResult shelfOptionOff =
                ChestLocatorInventoryTraversal.AppendSharedInventories(
                    fixture.Archive,
                    fixture.Root,
                    useBox: true,
                    includeSharedCases: true,
                    includeSharedStorageShelfBoxes: false,
                    nativeAutoUseBox: true,
                    fixture.Native);
            Assert(shelfOptionOff.AppendedInventoryCount == 1 &&
                   shelfOptionOff.SharedStorageBoxCount == 0,
                "The StorageShelf option must fail closed independently.");

            ChestLocatorTraversalResult callUseBoxOff =
                ChestLocatorInventoryTraversal.AppendSharedInventories(
                    fixture.Archive,
                    fixture.Root,
                    useBox: false,
                    includeSharedCases: true,
                    includeSharedStorageShelfBoxes: true,
                    nativeAutoUseBox: true,
                    fixture.Native);
            Assert(callUseBoxOff.AppendedInventoryCount == 1 &&
                   callUseBoxOff.SharedStorageBoxCount == 0,
                "The native call's useBox=false argument must suppress StorageShelf ItemBox widening.");

            string readme = File.ReadAllText(Path.Combine(ProductRoot(), "README.md"));
            Assert(readme.Contains("sole owner of inventory contents", StringComparison.Ordinal) &&
                   readme.Contains("CountItem/CostItem", StringComparison.Ordinal),
                "The product contract must leave contents and transactions with the game.");
        }

        private static void RepeatedTraversalCachesMetadataAndLimitsLogs()
        {
            TraversalFixture fixture = CreateTraversalFixture();
            for (int i = 0; i < 8; i++)
            {
                ChestLocatorInventoryTraversal.AppendSharedInventories(
                    fixture.Archive,
                    fixture.Root,
                    useBox: true,
                    includeSharedCases: true,
                    includeSharedStorageShelfBoxes: true,
                    nativeAutoUseBox: true,
                    fixture.Native);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 32; i++)
            {
                ChestLocatorInventoryTraversal.AppendSharedInventories(
                    fixture.Archive,
                    fixture.Root,
                    useBox: true,
                    includeSharedCases: true,
                    includeSharedStorageShelfBoxes: true,
                    nativeAutoUseBox: true,
                    fixture.Native);
            }
            long allocated =
                GC.GetAllocatedBytesForCurrentThread() -
                before;
            Assert(allocated <= 262144,
                "Thirty-two warmed repeated traversals allocated " +
                allocated +
                " bytes; reflection metadata or graph scratch reuse regressed.");

            var gate = new ChestLocatorObservationLogGate();
            int logs = 0;
            if (gate.ShouldLog(false, true, true, true))
                logs++;
            for (int i = 0; i < 32; i++)
                if (gate.ShouldLog(false, true, true, true))
                    logs++;
            if (gate.ShouldLog(false, true, false, false))
                logs++;
            if (gate.ShouldLog(true, true, false, false))
                logs++;
            Assert(logs == 3,
                "Repeated stable observations must log only once; state transitions and explicit verbose mode may log again.");
        }

        private static TraversalFixture CreateTraversalFixture()
        {
            var nativeInventory =
                new TraversalInventory("native");
            var caseInventory =
                new TraversalInventory("case");
            var boxInventory =
                new TraversalInventory("box");
            var sharedCase =
                new DolocTown.Case
                {
                    IsShared = true,
                    inventory = caseInventory
                };
            var shelfInventory =
                new TraversalShelfInventory(
                    new object[]
                    {
                        new DolocTown.ItemBox
                        {
                            inventory = boxInventory
                        },
                        new DolocTown.ItemBox
                        {
                            inventory = boxInventory
                        },
                        new DolocTown.ItemBox
                        {
                            inventory = caseInventory
                        }
                    });
            var shelf =
                new DolocTown.StorageShelf
                {
                    IsShared = true,
                    inventory = shelfInventory
                };
            var unsharedCase =
                new DolocTown.Case
                {
                    IsShared = false,
                    inventory =
                        new TraversalInventory(
                            "unshared-case")
                };
            var unsharedShelf =
                new DolocTown.StorageShelf
                {
                    IsShared = false,
                    inventory =
                        new TraversalShelfInventory(
                            new object[]
                            {
                                new DolocTown.ItemBox
                                {
                                    inventory =
                                        new TraversalInventory(
                                            "unshared-shelf-box")
                                }
                            })
                };
            var root =
                new TraversalRoom
                {
                    DM_equipment =
                        new TraversalEquipmentManager(
                            sharedCase,
                            shelf,
                            unsharedCase,
                            unsharedShelf),
                    DM_building =
                        new TraversalBuildingManager(),
                };
            root.RootRoom = root;
            var archive =
                new TraversalArchive
                {
                    currentRoom = root,
                    MainFarm = root,
                    farmData =
                        new TraversalFarmData
                        {
                            currentRoom = root,
                            MainFarm = root
                        }
                };
            return new TraversalFixture(
                archive,
                root,
                new[] { nativeInventory },
                caseInventory,
                boxInventory);
        }

        private static string ProductRoot() =>
            Path.Combine(FindRepositoryRoot(), "products", "first-party", "ChestLocatorEnhancer");

        private static string FindRepositoryRoot()
        {
            foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                DirectoryInfo? current = new DirectoryInfo(Path.GetFullPath(start));
                while (current != null)
                {
                    if (File.Exists(Path.Combine(current.FullName, "PROJECT.md")) &&
                        Directory.Exists(Path.Combine(current.FullName, "products", "first-party")))
                        return current.FullName;
                    current = current.Parent;
                }
            }
            throw new DirectoryNotFoundException("Could not locate the DTMAPI repository root.");
        }

        private static int Count(string source, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private sealed class TraversalFixture
        {
            internal TraversalFixture(
                TraversalArchive archive,
                TraversalRoom root,
                TraversalInventory[] native,
                TraversalInventory caseInventory,
                TraversalInventory boxInventory)
            {
                Archive = archive;
                Root = root;
                Native = native;
                CaseInventory = caseInventory;
                BoxInventory = boxInventory;
            }

            internal TraversalArchive Archive { get; }
            internal TraversalRoom Root { get; }
            internal TraversalInventory[] Native { get; }
            internal TraversalInventory CaseInventory { get; }
            internal TraversalInventory BoxInventory { get; }
        }

        private sealed class TraversalInventory
        {
            internal TraversalInventory(string id) =>
                Id = id;

            internal string Id { get; }
        }

        private sealed class TraversalArchive
        {
            public TraversalRoom? currentRoom;
            public TraversalRoom? MainFarm;
            public TraversalFarmData? farmData;
        }

        private sealed class TraversalFarmData
        {
            public TraversalRoom? currentRoom;
            public TraversalRoom? MainFarm;
        }

        private sealed class TraversalRoom
        {
            public TraversalRoom? RootRoom;
            public TraversalEquipmentManager? DM_equipment;
            public TraversalBuildingManager? DM_building;
        }

        private sealed class TraversalEquipmentManager
        {
            internal TraversalEquipmentManager(
                params object[] equipment) =>
                AllEquipments = equipment;

            public object[] AllEquipments { get; }
        }

        private sealed class TraversalBuildingManager
        {
            public object[] Buildings { get; } =
                Array.Empty<object>();
        }

        private sealed class TraversalShelfInventory
        {
            private readonly object[] items;

            internal TraversalShelfInventory(object[] items) =>
                this.items = items;

            public IEnumerable<object> ReadAll() =>
                items;
        }
    }
}

namespace DolocTown
{
    internal sealed class Case
    {
        public bool IsShared;
        public object? inventory;
    }

    internal sealed class StorageShelf
    {
        public bool IsShared;
        public object? inventory;
    }

    internal sealed class ItemBox
    {
        public object? inventory;
    }
}
