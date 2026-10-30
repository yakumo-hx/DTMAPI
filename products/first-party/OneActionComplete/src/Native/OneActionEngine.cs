using System;
using DolocTown;
using DolocTown.GameData;
using global::DTMAPI.Abstractions;
using UnityEngine;

namespace Yuuka.DTMAPI.OneActionComplete
{
    internal sealed class OneActionEngine
    {
        private OneActionConfig policy = new OneActionConfig();
        internal OneActionEngine(IMonitor monitor) => Monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        internal IMonitor Monitor { get; }

        // Config menu edits are a draft until its Save callback applies a copy.
        internal void Configure(OneActionConfig config) => policy = config.Copy();

        internal ToolHit? BeginToolHit(ItemTool? tool, Collider2D? collider)
        {
            if (tool == null || collider == null) return null;
            DungeonResource? resource = collider.GetComponent<DungeonResourceRenderer>()?.DungeonResource;
            return resource == null || resource.IsRemoved || resource.currentHealth <= 0 || !Matches(resource)
                ? null : new ToolHit(resource, tool);
        }

        internal void CompleteToolHit(ToolHit? hit)
        {
            if (hit == null) return;
            DungeonResource resource = hit.Resource;
            // Scope the evidence to this HandleTools invocation. A zero chop
            // counter on a second target cannot reuse the first accepted hit.
            if (resource.IsRemoved || resource.currentHealth <= 0 ||
                resource.currentHealth >= hit.Health || !Matches(resource)) return;

            int extraHits = 0;
            int remainingBound = resource.currentHealth;
            while (!resource.IsRemoved && resource.currentHealth > 0 && extraHits < remainingBound)
            {
                if (!DolocAPI.HasEnoughEnergyForUsingTool()) break;
                var data = new ResourceFellData(resource, hit.Tool, resource.PositionCenter);
                if (!data.Valid || !data.levelMatch || data.Damage <= 0 || !DolocAPI.CostToolEnergy()) break;
                int before = resource.currentHealth;
                extraHits++;
                // One native hit per payment preserves equipment bonuses,
                // tool-specific loot and the single terminal drop/removal call.
                resource._Fell(data);
                if (!resource.IsRemoved && resource.currentHealth >= before) break;
            }
        }

        internal FillAttempt? BeginEquipmentFill()
        {
            Equipment? equipment = DolocAPI.SelectedEquipment;
            Item? item = DolocAPI.SelectedItem;
            if (equipment == null || item == null || !Matches(equipment) || !Suitable(equipment, item)) return null;
            float progress = Progress(equipment);
            return progress < 0 || Full(equipment) || float.IsNaN(progress) ? null
                : new FillAttempt(equipment, item, Remaining(item), progress);
        }

        internal void CompleteEquipmentFill(FillAttempt? attempt)
        {
            if (attempt == null || !Matches(attempt.Equipment) ||
                !(Progress(attempt.Equipment) > attempt.Progress) || Remaining(attempt.Item) >= attempt.Count) return;

            Equipment equipment = attempt.Equipment;
            int consumed = 0;
            for (int guard = 0; guard < 99 && !Full(equipment); guard++)
            {
                Item? selected = DolocAPI.SelectedItem;
                if (selected == null || !Suitable(equipment, selected)) break;
                float before = Progress(equipment);
                if (!selected.CostSelf(out Item paidItem, false)) break;
                consumed++;
                try
                {
                    if (equipment is PowerGeneratorFuel fuel) fuel.AddFuel(paidItem.proto, true);
                    else ((Feeder)equipment).AddFeeds(paidItem.proto);
                }
                catch (Exception ex)
                {
                    // Native fill can mutate before a rendering exception.
                    // Inspect that state, stop, and never blindly refund a paid item.
                    Monitor.Log("One-action fill stopped after native exception; extraConsumed=" + consumed +
                        "; progressAdvanced=" + (Progress(equipment) > before) + "; error=" + ex.Message, LogLevel.Warn);
                    return;
                }
                if (!(Progress(equipment) > before)) break;
            }
        }

        private bool Matches(DungeonResource resource) =>
            (policy.CompleteTrees && (resource is DungeonResourceTree || resource is DungeonResourceTreeTrunk)) ||
            (policy.CompleteOres && resource is DungeonResourceOre) ||
            (policy.CompleteGarbage && (resource is DungeonResourceBuildingsGarbage || resource is DungeonResourceModelMechanicalGarbage)) ||
            (policy.CompleteWeeds && resource is DungeonResourceWeeds);

        private bool Matches(Equipment equipment) =>
            (policy.CompleteMachineFuel && equipment is PowerGeneratorFuel) || (policy.CompleteFeeder && equipment is Feeder);
        // FuelPercent is display-only and clamps before the native reserve is full.
        private static float Progress(Equipment equipment) => equipment is PowerGeneratorFuel
            ? ((ElectronicComponentGeneratorFuel)equipment.IElectronicComponent).Fuel : ((Feeder)equipment).progress;
        private static bool Full(Equipment equipment) => equipment is PowerGeneratorFuel
            ? ((ElectronicComponentGeneratorFuel)equipment.IElectronicComponent).IsFull : ((Feeder)equipment).IsFeederFull;
        private static bool Suitable(Equipment equipment, Item item) => equipment is PowerGeneratorFuel fuel
            ? fuel.IsSuitableFuel(item) : ((Feeder)equipment).IsAnimalFeeds(item.proto, out int _);
        private static int Remaining(Item item) => DolocAPI.archiveHandle.InventorySystem.inventory.IndexOf(item) < 0 ? 0 : item.count;

        internal sealed class ToolHit
        {
            internal ToolHit(DungeonResource resource, ItemTool tool) { Resource = resource; Tool = tool; Health = resource.currentHealth; }
            internal DungeonResource Resource { get; }
            internal ItemTool Tool { get; }
            internal int Health { get; }
        }

        internal sealed class FillAttempt
        {
            internal FillAttempt(Equipment equipment, Item item, int count, float progress) { Equipment = equipment; Item = item; Count = count; Progress = progress; }
            internal Equipment Equipment { get; }
            internal Item Item { get; }
            internal int Count { get; }
            internal float Progress { get; }
        }
    }
}
