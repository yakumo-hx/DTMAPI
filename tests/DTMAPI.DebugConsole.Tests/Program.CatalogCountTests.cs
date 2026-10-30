using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DTMAPI.Abstractions;
using DTMAPI.Core.Manifesting;
using DTMAPI.Core.Runtime;
using DTMAPI.DebugConsole;
using DTMAPI.ModConfigMenu;

namespace DTMAPI.UnitTests
{
    internal static partial class Program
    {
        private static void DebugConsoleCatalogCountDispatchAndPartialResults()
        {
            Assert(new DebugConsoleConfig().RightClickCount == 10 &&
                DebugConsoleConfig.NormalizeDispatchCount(0) == 1 &&
                DebugConsoleConfig.NormalizeDispatchCount(101) == 100 &&
                DebugConsoleConfig.NormalizeDispatchCount(double.NaN) == 10 &&
                DebugConsoleConfig.NormalizeDispatchCount(9.6) == 10,
                "Dispatch config must default to ten and support integer 1–100.");
            object? oldAssets = DolocAPI.assets, oldRoom = DolocAPI.CurrentRoom,
                oldPosition = DolocAPI.AgentPosition, oldTables = DolocTown.Config.DolocConfig.Tables;
            DebugConsoleInventoryFixture? oldInventory = DolocAPI.InventoryFixture;
            var assets = new DebugConsoleSpawnAssets();
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
            var adapter = new DebugConsoleTestRuntime(runtime);
            var actions = new DebugConsoleNativeActions(adapter);
            int count = 10;
            var ui = new DebugConsoleUi(adapter, rightClickCount: () => count);
            var owner = new ManifestModel { UniqueID = "DTMAPI.DebugConsoleMod", Name = "Y", Version = "1.1.6", Author = "DTMAPI" };
            SetPrivateField(ui, "ownerManifest", owner);
            SetPrivateField(ui, "inventoryApi", actions);
            SetPrivateField(ui, "advancedApi", actions);
            Type cellType = typeof(DebugConsoleUi).GetNestedType("CatalogCell", BindingFlags.NonPublic)!;
            object cell = RuntimeHelpers.GetUninitializedObject(cellType);
            cellType.GetField("owner", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(cell, ui);
            void CellCall(string name, params object?[] args) => cellType.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(cell, args);
            void Right() => CellCall("HandlePointerDown", new CatalogPointerData { button = 1 });
            string Status() => (string)typeof(DebugConsoleUi).GetField("statusMessage", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(ui)!;
            try
            {
                DolocAPI.assets = assets;
                DolocAPI.AgentPosition = new DebugConsoleAgentPositionFixture { x = 0, y = 0 };
                DolocTown.Config.DolocConfig.Tables = new DebugConsoleSpawnTables(assets.monsters.TotalProtos, assets.animals.TotalProtos);
                foreach (int requested in new[] { 1, 10, 100 })
                {
                    count = requested;
                    var inventory = new DebugConsoleInventoryFixture { Capacity = 1000 };
                    inventory.Protos.Add("CaseItem", new DebugConsoleInventoryProto("CaseItem", 20));
                    DolocAPI.InventoryFixture = inventory;
                    CellCall("BindItem", new InventoryDebugItem { Id = "CaseItem", DisplayName = "Case item", CanGive = true }, "", 0f, 0f);
                    Right();
                    Assert(inventory.Count("CaseItem") == requested && Status().Contains(requested.ToString()), "Item right-click must use current saved quantity and report actual count.");
                    CellCall("HandleClick");
                    Assert(inventory.Count("CaseItem") == requested + 1, "Left item click must remain one.");
                    Right();
                    Assert(inventory.Count("CaseItem") == 2 * requested + 1, "A separate immediate right press must not be swallowed by a 200ms window.");

                    var monsters = new DolocTown.DebugConsoleMonsterHost(assets.monsters.SpaceShipBastion);
                    DolocAPI.CurrentRoom = monsters;
                    CellCall("BindMonster", actions.GetMonsterCatalog().Single(option => option.Id == "slime"), "");
                    Right();
                    Assert(monsters.DM_monster.AllMonsters.Count == requested && Status().Contains(requested.ToString()), "Monster native generation must not silently clamp a requested 100 to ten.");
                    CellCall("HandleClick");
                    Assert(monsters.DM_monster.AllMonsters.Count == requested + 1, "Left monster click must remain one.");

                    var animals = new DolocTown.CatalogAnimalHost();
                    DolocAPI.CurrentRoom = animals;
                    CellCall("BindAnimal", actions.GetAnimalCatalog().Single(option => option.CardId == "slime.child"), "");
                    Right();
                    Assert(animals.DM_animal.AllAnimals.Count == requested && Status().Contains(requested.ToString()), "Animal native creation must preserve 1/10/100 requests.");
                    CellCall("HandleClick");
                    Assert(animals.DM_animal.AllAnimals.Count == requested + 1, "Left animal click must remain one.");
                }

                count = 100;
                var partialInventory = new DebugConsoleInventoryFixture { Capacity = 2 };
                partialInventory.Protos.Add("CaseItem", new DebugConsoleInventoryProto("CaseItem", 1));
                DolocAPI.InventoryFixture = partialInventory;
                CellCall("BindItem", new InventoryDebugItem { Id = "CaseItem", DisplayName = "Case item", CanGive = true }, "", 0f, 0f);
                Right();
                Assert(partialInventory.Count("CaseItem") == 2 && Status().Contains("100") && Status().Contains("2") && Status().Contains("partial-inventory-full"), "Partial inventory UI must include requested, actually given and stop reason.");
                partialInventory.Capacity = 10; count = 1; Right();
                Assert(partialInventory.Count("CaseItem") == 3, "Next click must dispatch only its own one, never replay the failed batch.");

                var guardians = new DolocTown.DebugConsoleMonsterHost(assets.monsters.SpaceShipBastion);
                DolocAPI.CurrentRoom = guardians;
                SpawnActionResult groups = actions.SpawnMonster(owner, "space_ship", 100);
                Assert(groups.Result.RequestedCount == 100 && groups.Result.SpawnedCount == 100 && groups.AddedEntityCount == 300, "Guardian request units remain root groups with separately counted native attachments.");
                var broken = new DolocTown.DebugConsoleMonsterHost(assets.monsters.SpaceShipBastion) { FailOnRootInvocation = 4 };
                DolocAPI.CurrentRoom = broken;
                CellCall("BindMonster", actions.GetMonsterCatalog().Single(option => option.Id == "slime"), "");
                count = 100; Right();
                Assert(broken.DM_monster.AllMonsters.Count == 4 && Status().Contains("100") && Status().Contains("3") && Status().Contains("4"), "Failed fourth generation must report three verified roots and four retained additions.");

                var capacity = new DolocTown.CatalogAnimalHost { Capacity = 2 };
                DolocAPI.CurrentRoom = capacity;
                SpawnActionResult limited = actions.SpawnAnimal(owner, "slime.child", 100);
                Assert(!limited.Result.Success && limited.Result.RequestedCount == 100 && limited.Result.SpawnedCount == 2 && limited.AddedEntityCount == 2 && capacity.CreateCalls == 2, "Animal capacity must stop without silently truncating the request or retrying.");
                var fault = new DolocTown.CatalogAnimalHost { FailOnCall = 4 };
                DolocAPI.CurrentRoom = fault;
                SpawnActionResult partial = actions.SpawnAnimal(owner, "slime.child", 100);
                Assert(!partial.Result.Success && partial.Result.SpawnedCount == 3 && partial.AddedEntityCount == 4 && fault.CreateCalls == 4, "Animal failure after insertion must retain and report every addition without compensation.");
                fault.FailOnCall = 0;
                Assert(actions.SpawnAnimal(owner, "slime.child", 1).AddedEntityCount == 1 && fault.CreateCalls == 5, "An independent next request must not replay prior animal work.");
            }
            finally
            {
                DolocAPI.assets = oldAssets; DolocAPI.CurrentRoom = oldRoom; DolocAPI.AgentPosition = oldPosition;
                DolocTown.Config.DolocConfig.Tables = oldTables; DolocAPI.InventoryFixture = oldInventory;
            }
        }

        private static void DebugConsoleCatalogFailureRetriesWithoutLostRows()
        {
            object? oldAssets = DolocAPI.assets, oldTables = DolocTown.Config.DolocConfig.Tables;
            var runtime = new DtmApiRuntime(new FakeHost(NewTempGameDir()), new ConfigMenuRegistry());
            var actions = new DebugConsoleNativeActions(new DebugConsoleTestRuntime(runtime));
            var assets = new DebugConsoleSpawnAssets();
            try
            {
                var monsters = new CatalogFailOnceRows(assets.monsters.TotalProtos);
                DolocAPI.assets = new { monsters = new { TotalProtos = monsters } };
                DolocTown.Config.DolocConfig.Tables = new DebugConsoleSpawnTables(assets.monsters.TotalProtos, assets.animals.TotalProtos);
                Assert(actions.GetMonsterCatalog().Count == 0, "Failed first catalog read should not publish its partial rows.");
                Assert(actions.GetMonsterCatalog().Count == 3, "Retry must include every row read before the failure.");
                var animals = new CatalogFailOnceRows(assets.animals.TotalProtos);
                DolocTown.Config.DolocConfig.Tables = new { TbAnimal = new { DataList = animals } };
                Assert(actions.GetAnimalCatalog().Count == 0, "Failed animal table read must not publish a partial state-card list.");
                Assert(actions.GetAnimalCatalog().Count == 6, "Animal retry must restore all three cards per merged-table row.");
                actions.InvalidateCatalogs(); DolocAPI.assets = null;
                Assert(actions.GetMonsterCatalog().Count == 0, "Missing native asset holder must remain unavailable.");
                DolocAPI.assets = assets;
                Assert(actions.GetMonsterCatalog().Count == 3, "A later ready holder must not be hidden by a cached empty catalog.");
            }
            finally { DolocAPI.assets = oldAssets; DolocTown.Config.DolocConfig.Tables = oldTables; }
        }

        private sealed class CatalogPointerData { public int button { get; set; } }
        private sealed class CatalogFailOnceRows : IEnumerable
        {
            private readonly IEnumerable rows; private bool fail = true;
            public CatalogFailOnceRows(IEnumerable rows) { this.rows = rows; }
            public IEnumerator GetEnumerator()
            {
                int index = 0;
                foreach (object row in rows)
                {
                    if (++index == 2 && fail) { fail = false; throw new InvalidOperationException("Catalog row read fault."); }
                    yield return row;
                }
            }
        }
    }
}

public static partial class DolocAPI
{
    public static UnityEngine.Vector2Int AgentRoomCellPosition => CropMaturityPlayerCell ?? new UnityEngine.Vector2Int(0, 0);
}
namespace UnityEngine
{
    public readonly struct Vector2Int
    {
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public readonly int x, y;
    }
}
namespace DolocTown
{
    // Authored native shapes; only signatures and observable membership/capacity.
    internal interface IAnimalHost
    {
        CatalogAnimal CreateAnimal(DTMAPI.UnitTests.DebugConsoleAnimalProto proto, UnityEngine.Vector2Int position, bool run);
        bool CheckAnimalSpace(int space);
    }
    internal sealed class CatalogAnimalHost : IAnimalHost
    {
        public CatalogAnimalSystem DM_animal { get; } = new CatalogAnimalSystem();
        public CatalogAnimalSystem animalSystem => DM_animal;
        public int Capacity = 1000, CreateCalls, FailOnCall;
        public bool CheckAnimalSpace(int space) => DM_animal.TotalCount < Capacity;
        public CatalogAnimal CreateAnimal(DTMAPI.UnitTests.DebugConsoleAnimalProto proto, UnityEngine.Vector2Int position, bool run)
        {
            CreateCalls++;
            var animal = new CatalogAnimal { proto = proto, CoverPositions = new[] { position } };
            DM_animal.AllAnimals.Add(animal);
            if (CreateCalls == FailOnCall) throw new InvalidOperationException("Animal notification failed after insertion.");
            return animal;
        }
    }
    internal sealed class CatalogAnimalSystem
    {
        public List<CatalogAnimal> AllAnimals { get; } = new List<CatalogAnimal>();
        public int TotalCount => AllAnimals.Count;
        public bool ContainsAnimal(CatalogAnimal animal) => AllAnimals.Contains(animal);
    }
    internal sealed class CatalogAnimal
    {
        public DTMAPI.UnitTests.DebugConsoleAnimalProto proto = null!;
        public UnityEngine.Vector2Int[] CoverPositions = Array.Empty<UnityEngine.Vector2Int>();
        public bool IsAdult { get; private set; }
        public void DEBUG_SetAdult(bool value) => IsAdult = value;
    }
    internal static class AnimalUtils
    {
        public static bool IsPositionWalkable(object room, UnityEngine.Vector2Int position, int width) => true;
    }
}
