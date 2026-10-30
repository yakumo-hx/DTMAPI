using System;
using System.Linq;
using DTMAPI.Abstractions;
using DolocTown;
using UnityEngine;
using Yuuka.DTMAPI.OneActionComplete;

namespace DTMAPI.UnitTests
{
    internal static class OneActionProductTests
    {
        internal static void RunAll()
        {
            var runtime = new OneActionNativeRuntime(NullMonitor.Instance);
            var engine = runtime.Engine;
            var config = new OneActionConfig();
            Check(typeof(OneActionConfig).GetProperties().Length == 6 &&
                typeof(OneActionConfig).GetProperties().All(p => !(bool)p.GetValue(config)!), "Exactly six defaults must be false.");
            var tool = new ItemTool { Damage = 2 };
            var resource = new DungeonResourceTree { currentHealth = 8 };
            var collider = Collider(resource);
            Check(engine.BeginToolHit(tool, collider) == null, "Defaults must not complete resources.");
            config.CompleteTrees = true;
            engine.Configure(config);
            config.CompleteTrees = false;
            Check(engine.BeginToolHit(tool, collider) != null, "Draft menu edits must not change applied policy.");

            DolocAPI.Energy = 20;
            var rejected = engine.BeginToolHit(tool, collider);
            engine.CompleteToolHit(rejected);
            Check(resource.currentHealth == 8 && DolocAPI.Energy == 20, "Rejected first hit cannot append damage or cost.");
            tool.LevelMatch = false;
            engine.CompleteToolHit(engine.BeginToolHit(tool, collider));
            Check(DolocAPI.Energy == 20, "Low-level rejection cannot cost extra energy.");
            tool.LevelMatch = true;

            // Native first hit uses the final permitted collision in this swing.
            var lastLegal = engine.BeginToolHit(tool, collider);
            NativeHit(resource, tool);
            engine.CompleteToolHit(lastLegal);
            Check(resource.IsRemoved && resource.Drops == 1 && resource.Hits == 4 && DolocAPI.Energy == 16, "Final legal hit must complete with per-hit charges and one native drop.");
            var second = new DungeonResourceTree { currentHealth = 8 };
            var secondAttempt = engine.BeginToolHit(tool, Collider(second));
            engine.CompleteToolHit(secondAttempt); // native refuses: swing counter is zero
            Check(second.currentHealth == 8 && DolocAPI.Energy == 16, "The next rejected target cannot inherit an accepted hit.");

            resource = new DungeonResourceTree { currentHealth = 8 };
            DolocAPI.Energy = 2;
            var partial = engine.BeginToolHit(tool, Collider(resource));
            NativeHit(resource, tool);
            engine.CompleteToolHit(partial);
            Check(!resource.IsRemoved && resource.currentHealth == 4 && DolocAPI.Energy == 0, "Partial energy must produce only paid native damage.");
            config.CompleteOres = true;
            engine.Configure(config);
            var ore = new DungeonResourceOre { currentHealth = 12, Bonus = 1 };
            DolocAPI.Energy = 10;
            var oreHit = engine.BeginToolHit(tool, Collider(ore));
            NativeHit(ore, tool);
            engine.CompleteToolHit(oreHit);
            Check(ore.Hits == 4 && ore.Drops == 1 && DolocAPI.Energy == 6, "Native per-hit equipment bonuses must not be aggregated incorrectly.");

            config.CompleteMachineFuel = config.CompleteFeeder = true;
            engine.Configure(config);
            foreach (bool feeder in new[] { false, true })
            {
                Equipment equipment = feeder ? new Feeder() : new PowerGeneratorFuel();
                Item stack = Select(equipment, 10);
                var attempt = engine.BeginEquipmentFill();
                engine.CompleteEquipmentFill(attempt);
                Check(stack.count == 10 && equipment.Amount == 0, "An unrelated interaction cannot initiate a fill.");
                FirstFill(equipment, stack);
                engine.CompleteEquipmentFill(attempt);
                Check(stack.count == 5 && equipment.Amount == 5, "Fill must stop at exact native capacity.");
                Check(engine.BeginEquipmentFill() == null, "Full containers must not consume anything.");

                equipment.Amount = 0;
                stack = Select(equipment, 2);
                attempt = engine.BeginEquipmentFill();
                FirstFill(equipment, stack);
                engine.CompleteEquipmentFill(attempt);
                Check(equipment.Amount == 2 && DolocAPI.SelectedItem == null, "Limited inventory must stop without fabricating materials.");
                stack = Select(equipment, 10); stack.proto.Suitable = false;
                Check(engine.BeginEquipmentFill() == null && stack.count == 10, "Wrong materials cannot be consumed.");
            }
            var reserve = new PowerGeneratorFuel { Capacity = 3, ExceedCapacity = 2, Amount = 3 };
            var reserveStack = Select(reserve, 10);
            var reserveAttempt = engine.BeginEquipmentFill();
            Check(reserve.FuelPercent == 1 && reserveAttempt != null, "Display 100% must not hide native fuel reserve capacity.");
            FirstFill(reserve, reserveStack);
            engine.CompleteEquipmentFill(reserveAttempt);
            Check(reserve.Amount == 5 && reserveStack.count == 8 && engine.BeginEquipmentFill() == null,
                "Native fuel progress beyond display 100% must fill to real capacity, then stop.");

            var fuel = new PowerGeneratorFuel { Capacity = 1000 };
            var boundedStack = Select(fuel, 200);
            var bounded = engine.BeginEquipmentFill();
            FirstFill(fuel, boundedStack);
            engine.CompleteEquipmentFill(bounded);
            Check(fuel.Amount == 100 && boundedStack.count == 100, "Only 99 extra transfers are allowed per interaction.");

            foreach (bool mutateBeforeThrow in new[] { false, true })
            {
                fuel = new PowerGeneratorFuel();
                var stack = Select(fuel, 10);
                var attempt = engine.BeginEquipmentFill();
                FirstFill(fuel, stack);
                fuel.ThrowBefore = !mutateBeforeThrow; fuel.ThrowAfter = mutateBeforeThrow;
                engine.CompleteEquipmentFill(attempt);
                Check(stack.count == 8 && fuel.Amount == (mutateBeforeThrow ? 2 : 1), "An ambiguous native failure must stop without duplicating a paid item.");
            }

            runtime.InstallHooksAtomically();
            OneActionHookInstaller.FailCleanup = true;
            try { runtime.DeactivateOwner(); throw new Exception("Unpatch failure was hidden."); } catch (InvalidOperationException) { }
            OneActionCallbacks.ToolColliderHandleToolsPrefix(tool, Collider(second), out var detached);
            Check(detached == null, "Callback must detach even when unpatch fails.");
            OneActionHookInstaller.FailCleanup = false;
            runtime.DeactivateOwner();
            Check(runtime.InstalledPatchCount == 0, "Unpatch can be retried.");
        }
        private static Collider2D Collider(DungeonResource resource) => new Collider2D { Renderer = new DungeonResourceRenderer { DungeonResource = resource } };
        private static void NativeHit(DungeonResource resource, ItemTool tool) { resource._Fell(new ResourceFellData(resource, tool, default)); DolocAPI.CostToolEnergy(); }
        private static Item Select(Equipment equipment, int count) { var item = new Item { count = count }; DolocAPI.SelectedEquipment = equipment; DolocAPI.SelectedItem = item; DolocAPI.archiveHandle.InventorySystem.inventory.Current = item; return item; }
        private static void FirstFill(Equipment equipment, Item item) { Check(item.CostSelf(out var paid, false), "Fixture consumption failed."); equipment.Add(paid.proto); }
        private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
