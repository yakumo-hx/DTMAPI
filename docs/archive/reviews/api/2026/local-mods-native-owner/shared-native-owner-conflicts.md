# Shared Native Owner Conflict Matrix

Status: docs-only risk matrix
Date: 2026-06-13

The four-round review found that many local mods stress the same native owner. These owners must not be stabilized until ownership, priority, restore, and multi-mod behavior are specified.

| Shared owner or domain | Mods and samples | Final risk |
| --- | --- | --- |
| `AgentState*`, animation, action timers | `ActionSpeedMod`, `AutoFishingMod`, `OneActionCompleteMod`, DolocPlus automation | Experimental. Needs owner token, priority, stacking, and restore rules. |
| `ToolCollider.HandleTools` | `OneActionCompleteMod`, `OilMod`, auto drone/resource automation | High-risk shared route. Needs callback isolation and conflict policy. |
| `PlantBasin.*`, `Crop.*`, `ItemFarmingGun.*` | `AutoHarvestMod`, `CropHarvestingQaMod`, `StrongPlantingGunMod`, HoldToHarvest | Only narrow crop-container and fixed planting-gun paths are reviewed. |
| `LinearInventory`, `InventorySystem`, `ArchiveDataHandle.GetAvailableInventories` | `ChestLocatorEnhancerMod`, `MineMod`, auto drone sort, DolocPlus use-all-chests, FullTrainer stack/craft | Transaction risk: overflow, full inventory, cross-room access, partial placement, stale references. |
| `ArchiveDataHandle`, `DolocAPI.DoTransport/EnterRoom`, time/weather paths | `DebugConsoleMod`, DolocPlus, FullTrainer | Diagnostic/debug-only first. World refresh and date/weather mutation need separate evidence. |
| `MotorController`, `ItemMotorKey`, motor archive state | `SecondMotorMod`, Infinite Hover, DolocPlus motor features | Native motor clone/lease only. Generic vehicle creation blocked. |
| `AgentEquipmentManager`, `AgentEquipmentFunction`, `AccessoriesBar` | `MoreEquipmentSlotsMod`, equipment/hat/accessory domain | Sidecar attribute slots only. True native slot expansion remains high risk. |
| `DroneController`, `DroneStruct`, `DroneRenderer` | Auto drone samples, drone domain | Active drone owner likely exists. Multi active drone and story-lock bypass remain blocked/high risk. |
| `RoomGeometrySO`, `BuildingProto`, `RoomHandleUtils` | BuildingExpander, auto drone v1.1 | Runtime geometry mutation blocked. Read-only diagnostics may be possible. |
| `FishingState*`, `FishingGameScrollBar` | `AutoFishingMod`, DolocPlus, FullTrainer | Owner evidence strong but public API remains Experimental because policy and restore are DTMAPI-owned. |
| Native UI hosts | `MoreSavesMod`, `AnimalHusbandryProgressMod`, `ConfigMenuExample`, `DebugConsoleMod` | UI proof is not gameplay proof. Display-only reports must stay display-only. |
| Content JSON and runtime bridge split | `OilMod`, `MineMod`, ExpandedEncyclopedia | Content support can be found while runtime behavior remains partial or demand-only. Do not merge the evidence classes. |
