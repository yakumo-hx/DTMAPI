using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
namespace DTMAPI.Mine {
    internal sealed partial class MineNativeRuntime {
        internal string ObserveMineScheduleForFixture(object equipment) {
            if (!scheduler.TryGet(equipment, out MineScheduleEntry entry)) {
                return "present=False";
            }
            return "present=True, firstObservedTU=" + entry.FirstObservedTotalTus + ", lastObservedTU=" + entry.LastObservedTotalTus + ", nextDueTU=" + entry.NextDueTotalTus + ", cycles=" + entry.ProductionCycleCount + ", block=" + (string.IsNullOrWhiteSpace(entry.BlockedKind) ? "none" : entry.BlockedKind) + ", message=" + entry.LastMessage;
        }
        private void UpdateMachineProduction() {
            if ((DateTimeOffset.Now - lastMachineProductionPollAt) .TotalSeconds < 0.5) {
                return;
            }
            lastMachineProductionPollAt = DateTimeOffset.Now;
            Type? dolocApi = ResolveType( "DolocAPI, Assembly-CSharp");
            object? archive = ReadStaticMember( dolocApi, "archiveHandle");
            object? currentRoom = ReadStaticMember( dolocApi, "CurrentRoom");
            if (dolocApi == null || archive == null || currentRoom == null) {
                return;
            }
            int totalTus = GetCurrentTotalTus(archive);
            if (totalTus < 0) return;
            int cycleTus = GetMineCycleTus( mineDefinition.CycleMinutes, GetCurrentTuMinutes(archive));
            int placed = 0;
            MineScheduleEntry? latest = null;
            scheduler.BeginAuthoritativeScan();
            foreach (object equipment in EnumerateMachineCandidateEquipments( dolocApi, archive, currentRoom)) {
                if (!ReadStringMember( equipment, "Name").Equals( MineProductContract.MineItemId, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                placed++;
                scheduler.Observe( equipment, totalTus, cycleTus, out MineScheduleEntry entry);
                entry.LastObservedTotalTus = totalTus;
                latest = entry;
                string visual = TryApplyMineVisualScale(equipment);
                if (!string.IsNullOrWhiteSpace(visual)) entry.LastVisualScaleSummary = visual;
                int cyclesThisPoll = 0;
                while (totalTus >= entry.NextDueTotalTus && cyclesThisPoll++ < 4) {
                    int dueAt = entry.NextDueTotalTus;
                    if (!TryRunMineProductionCycle( equipment, entry, out string message)) {
                        entry.LastMessage = message + " Due retained at " + entry.NextDueTotalTus + ".";
                        break;
                    }
                    entry.NextDueTotalTus = checked(dueAt + cycleTus);
                    entry.ProductionCycleCount++;
                    state.ProductionCycleCount++;
                    entry.LastMessage = message;
                    runtime.RuntimeMonitor.Log( "Mine production cycle OK output=" + entry.LastOutputItemId + " count=" + entry.LastOutputCount + " dueAt=" + dueAt + " nextDue=" + entry.NextDueTotalTus + " totalTUs=" + totalTus + ".");
                }
                if (cyclesThisPoll >= 4 && totalTus >= entry.NextDueTotalTus) {
                    entry.LastMessage = "Mine catch-up reached the four-cycle per-poll cap; due retained at " + entry.NextDueTotalTus + ".";
                }
            }
            int removed = scheduler.EndAuthoritativeScan();
            state.PlacedMineCount = placed;
            state.LastObservedTotalTUs = totalTus;
            state.NextDueTotalTUs = -1;
            foreach (MineScheduleEntry entry in scheduler.Entries) {
                if (state.NextDueTotalTUs < 0 || entry.NextDueTotalTus < state.NextDueTotalTUs) {
                    state.NextDueTotalTUs = entry.NextDueTotalTus;
                }
            }
            if (latest != null) CopyEntryToState(latest);
            else if (placed == 0) state.LastMessage = "Runtime loop active; no placed Mine is present in the authoritative room/farm scan.";
            if (removed > 0) {
                runtime.RuntimeMonitor.Log( "Mine scheduler pruned " + removed + " absent equipment identity entr" + (removed == 1 ? "y." : "ies."));
            }
        }
        private void CopyEntryToState( MineScheduleEntry entry) {
            state.LastOutputItemId = entry.LastOutputItemId;
            state.LastOutputDisplayName = entry.LastOutputDisplayName;
            state.LastOutputCount = entry.LastOutputCount;
            state.LastStorageFilledSlots = entry.LastStorageFilledSlots;
            state.LastStorageCapacity = entry.LastStorageCapacity;
            state.LastStorageLineCapacity = entry.LastStorageLineCapacity;
            state.LastMessage = string.IsNullOrWhiteSpace( entry.LastVisualScaleSummary) ? entry.LastMessage : entry.LastMessage + " visual={" + entry.LastVisualScaleSummary + "}";
        }
        private bool TryRunMineProductionCycle( object equipment, MineScheduleEntry entry, out string message) {
            if (!TryPreflightMineCycle( equipment, entry, out MineCyclePreflight preflight, out message)) {
                return false;
            }
            if (IsSameBlock( entry, "output-rejected", preflight)) {
                message = entry.LastMessage;
                return false;
            }
            ClearBlock(entry);
            MineOutputRule? output = PickMineOutput();
            if (output == null) {
                message = "Mine has no positive output weight.";
                SetBlock( entry, "output-rejected", preflight, message);
                return false;
            }
            int count = outputRandom.Next( Math.Min(output.MinCount, output.MaxCount), Math.Max(output.MinCount, output.MaxCount) + 1);
            if (preflight.EmptySlots < count) {
                message = "Mine-owned storage lacks enough empty slots for the selected output. emptySlots=" + preflight.EmptySlots + ", requestedSlots=" + count + ".";
                SetBlock( entry, "capacity-for-count", preflight, message);
                return false;
            }
            var preparedItems = new object[count];
            for (int i = 0; i < count; i++) {
                if (!TryGenerateNativeItem( output.ItemId, 1, out object? item, out string reason, out string itemMessage) || item == null) {
                    message = "Mine output preparation failed: " + reason + " " + itemMessage;
                    SetBlock( entry, "output-rejected", preflight, message);
                    return false;
                }
                object? accepted = preflight.ContentFilter.Invoke( equipment, new[] {
                    item
                }
                );
                if (accepted is bool allowed && !allowed) {
                    message = "Mine-owned storage ContentFilter rejected " + output.ItemId + ".";
                    SetBlock( entry, "output-rejected", preflight, message);
                    return false;
                }
                preparedItems[i] = item;
            }
            MachineCycleSnapshot snapshot = CaptureMachineCycleSnapshot(equipment);
            try {
                object? launched = preflight.Launch.Invoke( preflight.Component, null);
                if (!(launched is bool launchOk) || !launchOk) {
                    RestoreMachineCycleSnapshot(snapshot);
                    message = "Official electric component Launch() rejected the cycle after preflight; exact power/inventory restored.";
                    SetBlock( entry, "low-power", preflight, message);
                    return false;
                }
                if (!TryPlacePreparedMineOutput( preflight, preparedItems, output.ItemId, out string placementMessage)) {
                    RestoreMachineCycleSnapshot(snapshot);
                    message = "Mine output placement failed after Launch(); exact power/inventory restored: " + placementMessage;
                    SetBlock( entry, "output-rejected", preflight, message);
                    return false;
                }
                ClearBlock(entry);
                entry.LastOutputItemId = output.ItemId;
                entry.LastOutputDisplayName = output.DisplayName;
                entry.LastOutputCount = count;
                entry.LastStorageFilledSlots = ReadIntMember( preflight.Inventory, "filledCount", preflight.FilledSlots);
                entry.LastStorageCapacity = ReadIntMember( preflight.Inventory, "capacity", preflight.Capacity);
                entry.LastStorageLineCapacity = ReadIntMember( equipment, "lineCapacity", 0);
                message = "Mine produced " + output.ItemId + " x" + count + " after full storage/power/output preflight; native Launch() consumed fixed threshold " + MineProductContract.FixedPowerCost + ", storage=" + entry.LastStorageFilledSlots + "/" + entry.LastStorageCapacity + ".";
                return true;
            }
            catch {
                RestoreMachineCycleSnapshot(snapshot);
                throw;
            }
        }
        private bool TryPreflightMineCycle( object equipment, MineScheduleEntry entry, out MineCyclePreflight preflight, out string message) {
            preflight = null!;
            object? inventory = ReadMember(equipment, "inventory");
            object? component = ReadMember( equipment, "IElectronicComponent");
            if (inventory == null || component == null) {
                message = "Mine requires its native Case inventory and official electric appliance.";
                return false;
            }
            int capacity = ReadIntMember(inventory, "capacity", 0);
            int filled = ReadIntMember(inventory, "filledCount", 0);
            int empty = ReadIntMember(inventory, "emptyCount", 0);
            double power = ReadDoubleMember( component, "power", double.NaN);
            MethodInfo? launch = FindMethodInHierarchy( component.GetType(), "Launch", 0);
            MethodInfo? filter = FindMethodInHierarchy( equipment.GetType(), "ContentFilter", 1);
            MethodInfo? place = FindMethodInHierarchy( inventory.GetType(), "PlaceItemAt", 2);
            preflight = new MineCyclePreflight( component, inventory, launch, filter, place, power, capacity, filled, empty);
            if (capacity <= 0 || launch == null || filter == null || place == null) {
                message = "Mine native storage/electric contract is incomplete.";
                SetBlock( entry, "native-contract", preflight, message);
                return false;
            }
            if (empty <= 0) {
                if (IsSameBlock( entry, "full-storage", preflight)) {
                    message = entry.LastMessage;
                    return false;
                }
                message = "Mine-owned storage is full; Launch() was not called. filled=" + filled + "/" + capacity + ".";
                SetBlock( entry, "full-storage", preflight, message);
                return false;
            }
            if (double.IsNaN(power) || power < MineProductContract.FixedPowerCost) {
                if (IsSameBlock( entry, "low-power", preflight)) {
                    message = entry.LastMessage;
                    return false;
                }
                message = "Mine has insufficient native power; no output was selected or constructed and inventory was not copied. power=" + (double.IsNaN(power) ? "unavailable" : power.ToString( "0.##", CultureInfo.InvariantCulture)) + "/" + MineProductContract.FixedPowerCost + ".";
                SetBlock( entry, "low-power", preflight, message);
                return false;
            }
            message = string.Empty;
            return true;
        }
        private static bool TryPlacePreparedMineOutput( MineCyclePreflight preflight, IReadOnlyList<object> preparedItems, string itemId, out string message) {
            foreach (object item in preparedItems) {
                int slot = ReadIntMember( preflight.Inventory, "FirstEmptyIndex", -1);
                if (slot < 0) {
                    message = "Native inventory no longer had an empty slot.";
                    return false;
                }
                object? leftover = preflight.PlaceItemAt.Invoke( preflight.Inventory, new[] {
                    (object)slot, item
                }
                );
                if (leftover != null && ReadIntMember( leftover, "count", 1) > 0) {
                    message = "LinearInventory.PlaceItemAt returned leftover " + itemId + " at slot " + slot + ".";
                    return false;
                }
            }
            message = "Stored prepared output in Mine-owned storage.";
            return true;
        }
        private MineOutputRule? PickMineOutput() {
            double total = 0d;
            foreach (MineOutputRule rule in mineDefinition.OutputRules) {
                if (rule.Weight > 0d && !string.IsNullOrWhiteSpace(rule.ItemId)) {
                    total += rule.Weight;
                }
            }
            if (total <= 0d) return null;
            double roll = outputRandom.NextDouble() * total;
            MineOutputRule? last = null;
            foreach (MineOutputRule rule in mineDefinition.OutputRules) {
                if (rule.Weight <= 0d || string.IsNullOrWhiteSpace(rule.ItemId)) {
                    continue;
                }
                last = rule;
                roll -= rule.Weight;
                if (roll <= 0d) return rule;
            }
            return last;
        }
        private static bool IsSameBlock( MineScheduleEntry entry, string kind, MineCyclePreflight preflight) => entry.BlockedKind.Equals( kind, StringComparison.Ordinal) && entry.BlockedPower.Equals(preflight.Power) && entry.BlockedEmptySlots == preflight.EmptySlots && entry.BlockedCapacity == preflight.Capacity;
        private static void SetBlock( MineScheduleEntry entry, string kind, MineCyclePreflight preflight, string message) {
            entry.BlockedKind = kind;
            entry.BlockedPower = preflight.Power;
            entry.BlockedEmptySlots = preflight.EmptySlots;
            entry.BlockedCapacity = preflight.Capacity;
            entry.LastMessage = message;
        }
        private static void ClearBlock( MineScheduleEntry entry) {
            entry.BlockedKind = string.Empty;
            entry.BlockedPower = double.NaN;
            entry.BlockedEmptySlots = -1;
            entry.BlockedCapacity = -1;
        }
        private string TryApplyMineVisualScale( object equipment) {
            try {
                object? renderer = ReadMember(equipment, "Renderer");
                object? transform = renderer == null ? null : ReadMember(renderer, "transform");
                if (transform == null) return "visualScale=2, renderer=pending";
                object? currentScale = ReadMember(transform, "localScale");
                double currentX = ReadVectorComponent(currentScale, "x");
                double currentY = ReadVectorComponent(currentScale, "y");
                double currentZ = ReadVectorComponent(currentScale, "z");
                double signedX = currentX < 0 ? -2d : 2d;
                double z = Math.Abs(currentZ) < 0.001 || double.IsNaN(currentZ) ? 1d : currentZ;
                bool alreadyApplied = IsNearScale(Math.Abs(currentX), 2d) && IsNearScale(Math.Abs(currentY), 2d);
                if (!alreadyApplied) {
                    CaptureScale(transform);
                    if (!TrySetTransformLocalScale( transform, signedX, 2d, z)) {
                        return "visualScale=2, rendererScale=failed";
                    }
                }
                return "visualScale=2, applied=" + (!alreadyApplied);
            }
            catch (Exception ex) {
                runtime.Diagnostics.RecordError( "DTMAPI.Mine", "Mine visual scale failed.", ex.ToString());
                return "visualScale=2, failed=" + ex.GetType().Name + ":" + ex.Message;
            }
        }
        internal void ApplyMineBuilderPreviewScale( object builder, string reason) {
            if (!active || disposed) return;
            try {
                string equipmentId = ResolveBuilderEquipmentId(builder);
                object? indicatorRenderer = ReadMember(builder, "indicatorRenderer");
                object? indicator = indicatorRenderer == null ? null : ReadMember( indicatorRenderer, "indicator");
                object? transform = ResolveBuilderIndicatorTransform( indicator);
                if (transform == null || string.IsNullOrWhiteSpace(equipmentId)) {
                    return;
                }
                if (!equipmentId.Equals( MineProductContract.MineItemId, StringComparison.OrdinalIgnoreCase)) {
                    RestoreScale(transform);
                    return;
                }
                object? currentScale = ReadMember(transform, "localScale");
                double currentX = ReadVectorComponent(currentScale, "x");
                double currentY = ReadVectorComponent(currentScale, "y");
                double currentZ = ReadVectorComponent(currentScale, "z");
                double signedX = currentX < 0 ? -2d : 2d;
                double z = Math.Abs(currentZ) < 0.001 || double.IsNaN(currentZ) ? 1d : currentZ;
                bool alreadyApplied = IsNearScale(Math.Abs(currentX), 2d) && IsNearScale(Math.Abs(currentY), 2d);
                if (!alreadyApplied) {
                    CaptureScale(transform);
                    if (!TrySetTransformLocalScale( transform, signedX, 2d, z)) {
                        return;
                    }
                }
                runtime.RuntimeMonitor.LogOnce( "mine-builder-preview-scale", "Mine placement preview scale applied. reason=" + reason + ", previous=" + FormatScale( currentX, currentY, currentZ) + ".");
            }
            catch (Exception ex) {
                runtime.Diagnostics.RecordError( "DTMAPI.Mine", "Mine builder preview scale failed.", ex.ToString());
            }
        }
        private static string ResolveBuilderEquipmentId( object? builder) {
            if (builder == null) return string.Empty;
            object? equipmentProto = ReadMember(builder, "equipmentProto");
            string id = ReadEquipmentProtoId(equipmentProto);
            if (!string.IsNullOrWhiteSpace(id)) return id;
            object? selectedItem = ReadMember(builder, "SelectedItem") ?? ReadMember(builder, "CurrentItem");
            id = selectedItem == null ? string.Empty : ReadEquipmentProtoId( ReadMember( selectedItem, "EquipmentProto"));
            if (!string.IsNullOrWhiteSpace(id)) return id;
            object? selectedContent = ReadMember(builder, "SelectedContent") ?? ReadMember(builder, "CurrentContent") ?? ReadMember(builder, "CheckedContent");
            return FirstText( selectedContent == null ? string.Empty : ReadStringMember( selectedContent, "Name"), selectedContent == null ? string.Empty : ReadEquipmentProtoId( ReadMember( selectedContent, "proto")), selectedContent == null ? string.Empty : ReadEquipmentProtoId( ReadMember( selectedContent, "Proto")));
        }
        private static string ReadEquipmentProtoId( object? proto) {
            if (proto == null) return string.Empty;
            return FirstText( ReadStringMember(proto, "Id"), ReadStringMember(proto, "id"), ReadStringMember(proto, "Name"), ReadStringMember(proto, "name"));
        }
        private static object? ResolveBuilderIndicatorTransform( object? indicator) {
            if (indicator == null) return null;
            object? transform = ReadMember(indicator, "transform");
            if (transform != null) return transform;
            object? gameObject = ReadMember(indicator, "gameObject");
            return gameObject == null ? null : ReadMember(gameObject, "transform");
        }
        private static bool TrySetTransformLocalScale( object transform, double x, double y, double z) {
            object? scale = CreateUnityVector3(x, y, z);
            return scale != null && SetMemberValue( transform, "localScale", scale);
        }
        private static bool IsNearScale( double value, double expected) => !double.IsNaN(value) && !double.IsInfinity(value) && Math.Abs(value - expected) <= 0.05;
        private static string FormatScale( double x, double y, double z) => x.ToString( "0.##", CultureInfo.InvariantCulture) + "x" + y.ToString( "0.##", CultureInfo.InvariantCulture) + "x" + z.ToString( "0.##", CultureInfo.InvariantCulture);
        private static int GetCurrentTotalTus( object archive) {
            object? dateNow = ReadMember(archive, "DateNow");
            object? timeData = ReadMember(archive, "timeData");
            dateNow ??= timeData == null ? null : ReadMember(timeData, "dateNow");
            return dateNow == null ? -1 : ReadIntMember( dateNow, "TotalTUs", -1);
        }
        private static int GetCurrentTuMinutes( object archive) {
            object? timeData = ReadMember(archive, "timeData");
            object? dateConfig = timeData == null ? null : ReadMember( timeData, "dateConfig");
            if (dateConfig != null) {
                return Math.Max( 1, ReadIntMember( dateConfig, "TU2Min", 10));
            }
            Type? dolocApi = ResolveType("DolocAPI, Assembly-CSharp");
            object? globalParameter = ReadStaticMember( dolocApi, "GlobalParameter");
            object? globalDateConfig = globalParameter == null ? null : ReadMember( globalParameter, "DateConfig");
            return globalDateConfig == null ? 10 : Math.Max( 1, ReadIntMember( globalDateConfig, "TU2Min", 10));
        }
        private static int GetMineCycleTus( int cycleMinutes, int tuMinutes) => Math.Max( 1, (int)Math.Ceiling( Math.Max(1, cycleMinutes) / (double)Math.Max(1, tuMinutes)));
        private static double ReadDoubleMember( object instance, string name, double fallback) {
            object? value = ReadMember(instance, name);
            if (value == null) return fallback;
            try {
                return Convert.ToDouble( value, CultureInfo.InvariantCulture);
            }
            catch {
                return fallback;
            }
        }
        private static MineDefinition NormalizeMineDefinition( MineDefinition? definition) {
            definition ??= new MineDefinition();
            var normalized = new MineDefinition {
                CycleMinutes = ClampInt( definition.CycleMinutes, 5, 720), NativeTechTreeId = definition.NativeTechTreeId ?? string.Empty, NativeTechNodeId = FirstText( definition.NativeTechNodeId, MineProductContract.MineItemId), NativeTechNodeTitle = FirstText( definition.NativeTechNodeTitle, "矿井"), NativeTechNodeParentId = FirstText( definition.NativeTechNodeParentId, "alloy_material"), NativeTechNodeAboveTitleContains = FirstText( definition .NativeTechNodeAboveTitleContains, "指挥官"), RecipeInputs = definition.RecipeInputs .Where(input => input != null && !string.IsNullOrWhiteSpace( input.ItemId) && input.Count > 0) .Select(input => new MineRecipeInput {
                    ItemId = input.ItemId.Trim(), Count = ClampInt( input.Count, 1, 9999)
                }
                ) .ToArray(), OutputRules = definition.OutputRules .Where(rule => rule != null && !string.IsNullOrWhiteSpace( rule.ItemId)) .Select(rule => new MineOutputRule {
                    ItemId = rule.ItemId.Trim(), DisplayName = rule.DisplayName ?? string.Empty, Weight = double.IsNaN( rule.Weight) || double.IsInfinity( rule.Weight) ? 0d : ClampDouble( rule.Weight, 0d, 100d), MinCount = ClampInt( rule.MinCount, 1, 2), MaxCount = ClampInt( Math.Max( rule.MinCount, rule.MaxCount), 1, 2)
                }
                ) .ToArray()
            };
            return normalized;
        }
        private sealed class MineCyclePreflight {
            internal MineCyclePreflight( object component, object inventory, MethodInfo? launch, MethodInfo? contentFilter, MethodInfo? placeItemAt, double power, int capacity, int filledSlots, int emptySlots) {
                Component = component;
                Inventory = inventory;
                Launch = launch!;
                ContentFilter = contentFilter!;
                PlaceItemAt = placeItemAt!;
                Power = power;
                Capacity = capacity;
                FilledSlots = filledSlots;
                EmptySlots = emptySlots;
            }
            internal object Component {
                get;
            }
            internal object Inventory {
                get;
            }
            internal MethodInfo Launch {
                get;
            }
            internal MethodInfo ContentFilter {
                get;
            }
            internal MethodInfo PlaceItemAt {
                get;
            }
            internal double Power {
                get;
            }
            internal int Capacity {
                get;
            }
            internal int FilledSlots {
                get;
            }
            internal int EmptySlots {
                get;
            }
        }
    }
}
