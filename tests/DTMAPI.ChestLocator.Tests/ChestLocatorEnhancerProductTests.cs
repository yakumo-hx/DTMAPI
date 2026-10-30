using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using DTMAPI.ChestLocatorEnhancer;
using DTMAPI.Testing;
using DolocTown;
using DolocTown.GameData;

namespace DTMAPI.UnitTests
{
    internal static class ChestLocatorEnhancerProductTests
    {
        internal static void RunAll()
        {
            OwnershipAndResidualState();
            IndexedGraphIncludesDerivedDeepRoomsAndPreservesNativeOrder();
            QueryReadsCurrentMembershipAndNativeSettings();
            ReentryFailureAndNextArchiveReleaseAllReferences();
            RepeatedObservationsOnlyLogChanges();
            PhysicalHarmonyFixtureExecutesOwnershipAndLifecycle();
        }

        private static void PhysicalHarmonyFixtureExecutesOwnershipAndLifecycle()
        {
            string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            string executable = Path.Combine(DtmApiTestSession.FindRepositoryRoot(), "tests", "DTMAPI.UnitTests",
                "Fixtures", "ChestLocatorHarmonyOwnerFixture", "bin", configuration, "net48", "ChestLocatorHarmonyOwnerFixture.exe");
            if (!File.Exists(executable))
                throw new FileNotFoundException("The product build did not produce the physical Harmony owner fixture.", executable);
            var result = BoundedTestProcess.Run(new ProcessStartInfo(executable));
            Assert(result.ExitCode == 0 && result.Output.Contains("ChestLocatorHarmonyOwnerFixture: OK", StringComparison.Ordinal),
                "Physical Harmony owner fixture failed. exit=" + result.ExitCode + ", output=" + result.Output + ", error=" + result.Error);
        }

        private static void OwnershipAndResidualState()
        {
            int installs = 0, unpatches = 0;
            ChestLocatorHookOwnership.ApplyEnabledState(true, () => installs++, () => unpatches++);
            ChestLocatorHookOwnership.ApplyEnabledState(false, () => installs++, () => unpatches++);
            Assert(installs == 1 && unpatches == 1, "Enable and disable each select exactly one ownership action.");
            Assert(ChestLocatorHookOwnership.DecideInstall(false, true) == ChestLocatorInstallDecision.RejectDuplicateProductOwner, "A duplicate product owner must fail closed.");
            Assert(ChestLocatorHookOwnership.DecideInstall(false, false) == ChestLocatorInstallDecision.Install, "An unowned target can install.");
            var residual = ChestLocatorHookOwnership.FromExactOwnerObservation(true);
            var clean = ChestLocatorHookOwnership.FromExactOwnerObservation(false);
            Assert(residual.IsInstalled && residual.InstalledPatchCount == 1 && !clean.IsInstalled && clean.InstalledPatchCount == 0, "Residual physical ownership cannot be reported as clean.");
        }

        private static void IndexedGraphIncludesDerivedDeepRoomsAndPreservesNativeOrder()
        {
            var f = new Fixture();
            var child = new Room();
            var deep = new Room();
            f.Room.DM_building.Buildings.Add(new Building { room = child });
            child.DM_building.Buildings.Add(new Building { room = deep });
            deep.DM_building.Buildings.Add(new Building { room = f.Room });
            var derived = new DerivedCase { IsShared = true };
            var derivedShelf = new DerivedShelf { IsShared = true };
            var box = new DerivedBox();
            derivedShelf.inventory.Items.Add(box);
            deep.DM_equipment.Entries.AddRange(new Equipment[] { derived, derivedShelf, new Equipment() });
            f.Room.DM_equipment.Entries.Add(new Case { IsShared = true, inventory = derived.inventory });
            // Preserve the native result verbatim, including native duplicates/null.
            var native = new[] { f.Native[0], f.Native[0], null! };
            var result = Query(f, native: native);
            EqualReferences(result.Inventories, native.Concat(new[] { f.Case.inventory, derived.inventory, f.Box.inventory, box.inventory }).ToArray());
            Assert(result.Counts.ScannedRootCount == 3, "Deep graph cycle and overlapping seeds must visit each room once.");
            Assert(result.Counts.AppendedInventoryCount == 4, "Every additional inventory appears once by reference.");
            Assert(f.Room.DM_equipment.IndexQueries == 2 && child.DM_equipment.IndexQueries == 2 && deep.DM_equipment.IndexQueries == 2, "One query per relevant native type, no AllEquipments enumeration.");
            EqualReferences(Query(f, native: native).Inventories, result.Inventories);
            ScratchIsEmpty();
        }

        private static void QueryReadsCurrentMembershipAndNativeSettings()
        {
            var f = new Fixture();
            foreach (bool useBox in new[] { false, true })
            foreach (bool auto in new[] { false, true })
            {
                var result = Query(f, useBox, auto);
                Assert(result.Inventories.Cast<LinearInventory>().Contains(f.Case.inventory), "Shared Case is independent of both box settings.");
                Assert(result.Inventories.Cast<LinearInventory>().Contains(f.Box.inventory) == (useBox && auto), "Shelf expansion needs both settings.");
                Assert(!result.Inventories.Cast<LinearInventory>().Contains(f.Hidden.inventory), "Remote non-shared Case must not enter.");
            }
            var replacement = new ItemBox();
            f.Shelf.inventory.Items.Clear();
            f.Shelf.inventory.Items.Add(replacement);
            var current = Query(f);
            Assert(!current.Inventories.Cast<LinearInventory>().Contains(f.Box.inventory) && current.Inventories.Cast<LinearInventory>().Contains(replacement.inventory), "Next query must use the shelf's current box.");
            f.Shelf.IsShared = false; f.Case.IsShared = false;
            Assert(ReferenceEquals(Query(f).Inventories, f.Native), "No additions returns the original native array.");
            f.Hidden.IsShared = true;
            Assert(Query(f).Inventories.Cast<LinearInventory>().Contains(f.Hidden.inventory), "Changing sharing is visible on the next query.");
            var added = new Case { IsShared = true };
            f.Room.DM_equipment.Entries.Add(added);
            Assert(Query(f).Inventories.Cast<LinearInventory>().Contains(added.inventory), "New indexed container is immediately visible.");
            var child = new Room(); f.Room.DM_building.Buildings.Add(new Building { room = child });
            f.Room.DM_equipment.Entries.Remove(added); child.DM_equipment.Entries.Add(added);
            Assert(Query(f).Inventories.Cast<LinearInventory>().Count(i => ReferenceEquals(i, added.inventory)) == 1, "Moving between reachable rooms preserves one inventory.");
            child.DM_equipment.Entries.Remove(added);
            Assert(!Query(f).Inventories.Cast<LinearInventory>().Contains(added.inventory), "Removed container disappears on next query.");
            ScratchIsEmpty();
        }

        private static void ReentryFailureAndNextArchiveReleaseAllReferences()
        {
            var outer = new Fixture(); var nested = new Fixture();
            outer.Room.DM_equipment.OnQuery = () => {
                outer.Room.DM_equipment.OnQuery = null;
                var inside = Query(nested);
                Assert(inside.Inventories.Cast<LinearInventory>().Contains(nested.Case.inventory) && !inside.Inventories.Cast<LinearInventory>().Contains(outer.Case.inventory), "Nested query must use separate scratch.");
            };
            var result = Query(outer);
            Assert(result.Inventories.Cast<LinearInventory>().Contains(outer.Case.inventory) && !result.Inventories.Cast<LinearInventory>().Contains(nested.Case.inventory), "Nested query must not alter its caller's result.");
            ScratchIsEmpty();
            outer.Room.DM_equipment.OnQuery = () => throw new InvalidOperationException("index failure");
            bool failed = false;
            try { Query(outer); } catch (TargetInvocationException e) { failed = e.InnerException?.Message == "index failure"; }
            Assert(failed, "Failure fixture must reach its intended boundary.");
            ScratchIsEmpty();
            var next = Query(nested);
            Assert(!next.Inventories.Cast<LinearInventory>().Contains(outer.Case.inventory), "Next archive cannot see a failed query's inventories.");
            Assert(typeof(ChestLocatorTraversalCounts).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).All(f => f.FieldType == typeof(int)), "Retained diagnostics contain only counts.");
        }

        private static void RepeatedObservationsOnlyLogChanges()
        {
            var gate = new ChestLocatorObservationLogGate();
            Assert(gate.ShouldLog(true, true, true), "First observation logs.");
            for (int i = 0; i < 32; i++) Assert(!gate.ShouldLog(true, true, true), "Stable queries do not amplify logs.");
            Assert(gate.ShouldLog(true, false, false), "Changed settings or widening logs.");
            gate.Reset();
            Assert(gate.ShouldLog(true, false, false), "A lifecycle reset permits its first observation.");
        }

        private static ChestLocatorTraversalResult Query(Fixture f, bool useBox = true, bool auto = true, LinearInventory[]? native = null) =>
            ChestLocatorInventoryTraversal.AppendSharedInventories(f.Archive, f.Room, useBox, auto, native ?? f.Native);

        private static void EqualReferences(Array actual, Array expected)
        {
            Assert(actual.Length == expected.Length, "Inventory sequence length differs.");
            for (int i = 0; i < actual.Length; i++) Assert(ReferenceEquals(actual.GetValue(i), expected.GetValue(i)), "Inventory order/reference differs at " + i);
        }

        private static void ScratchIsEmpty()
        {
            object scratch = typeof(ChestLocatorInventoryTraversal).GetField("cachedScratch", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            foreach (var field in scratch.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                if (field.GetValue(scratch) is IEnumerable entries)
                    Assert(!entries.Cast<object>().Any(), "Scratch retains references after query: " + field.Name);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class Fixture
        {
            internal readonly Room Room = new Room();
            internal readonly Case Case = new Case { IsShared = true };
            internal readonly Case Hidden = new Case();
            internal readonly StorageShelf Shelf = new StorageShelf { IsShared = true };
            internal readonly ItemBox Box = new ItemBox();
            internal readonly LinearInventory[] Native = { new LinearInventory() };
            internal readonly ArchiveDataHandle Archive;
            internal Fixture()
            {
                Room.RootRoom = Room;
                Shelf.inventory.Items.Add(Box);
                Shelf.inventory.Items.Add(new ItemBox { inventory = Case.inventory });
                Room.DM_equipment.Entries.AddRange(new Equipment[] { Case, Shelf, Hidden, new Equipment() });
                Archive = new ArchiveDataHandle { currentRoom = Room, MainFarm = Room, farmData = new FarmArchiveData { currentRoom = Room, MainFarm = Room } };
            }
        }
        private sealed class DerivedCase : Case { }
        private sealed class DerivedShelf : StorageShelf { }
        private sealed class DerivedBox : ItemBox { }
    }
}

// Contract doubles exercise traversal logic; native index maintenance and real
// count/max/cost are independently exercised by NativeProbe in the game.
namespace DolocTown
{
    internal class LinearInventory
    {
        internal readonly List<Item> Items = new List<Item>();
        // The host has both overloads; open generic reflection must never win.
        public IEnumerable<T> ReadAll<T>() where T : Item => throw new Exception("Open generic ReadAll must not be invoked.");
        public Item[] ReadAll() => Items.ToArray();
    }
    internal class Item { }
    internal class ItemBox : Item { public LinearInventory inventory = new LinearInventory(); }
    internal class Equipment { }
    internal class Case : Equipment { public bool IsShared; public LinearInventory inventory = new LinearInventory(); }
    internal class StorageShelf : Equipment { public bool IsShared; public LinearInventory inventory = new LinearInventory(); }
    internal class Room { public Room? RootRoom; public EquipmentManager DM_equipment = new EquipmentManager(); public BuildingManager DM_building = new BuildingManager(); }
    internal class Building { public Room? room; }
    internal class BuildingManager { public List<Building> Buildings = new List<Building>(); }
    internal class EquipmentManager
    {
        internal readonly List<Equipment> Entries = new List<Equipment>();
        internal int IndexQueries;
        internal Action? OnQuery;
        public IEnumerable<Equipment> AllEquipments => throw new Exception("AllEquipments must not be read by this product.");
        public T[] GetEquipments<T>() where T : Equipment { IndexQueries++; OnQuery?.Invoke(); return Entries.OfType<T>().ToArray(); }
    }
}
namespace DolocTown.GameData
{
    internal class ArchiveDataHandle { public DolocTown.Room? currentRoom; public DolocTown.Room? MainFarm; public FarmArchiveData? farmData; }
    internal class FarmArchiveData { public DolocTown.Room? currentRoom; public DolocTown.Room? MainFarm; }
}

internal static class DolocAPI { }
