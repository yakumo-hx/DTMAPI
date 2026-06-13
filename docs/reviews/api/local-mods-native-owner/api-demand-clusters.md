# API Demand Clusters

Status: docs-only clustering
Date: 2026-06-13

This file groups current local mods by API demand. It intentionally mixes current DTMAPI testmods, legacy local sources, and third-party sample groups when they stress the same native owner.

## Framework, Config, Diagnostics

| Inputs | Native/Core owner | API conclusion |
| --- | --- | --- |
| `HelloDtmMod` | DTMAPI Core mod entry, monitor, `GameLaunched` | Stable for framework entry/log sample only. |
| `ConfigMenuExample` | `IDtmConfigMenuApi`, config registry, title/pause UI host | StableCandidate, pending broader manual QA. |
| `BrokenManifestMod` | manifest reader, loader diagnostics | Core Diagnostic. |
| `HookProbeMod` | GameLoop, Save, Workshop, Diagnostics evidence paths | Diagnostic/Internal evidence fixture. |
| `DebugConsoleMod`, FullTrainer-style controls | `DolocAPI`, `ArchiveDataHandle`, `MotionAbility`, debug adapters | Diagnostic only. Ordinary gameplay APIs must not inherit these powers. |

## Action And Input Automation

| Inputs | Candidate native owner | API conclusion |
| --- | --- | --- |
| `ActionSpeedMod` | `AgentStateTool`, `AgentStateInteract`, `AgentStateEat`, `AgentControllerState.UseItemContinues`, `ItemBottle.UseAsItem` | Experimental. No native global action-speed policy found. |
| `OneActionCompleteMod` | `ToolCollider.HandleTools`, `DungeonResource`, `PowerGeneratorFuel`, `Feeder`, `AgentStateInteract` | Experimental. Narrow slices only. |
| `HoldToHarvest`, DolocPlus automation | `PlantBasin` plus native input-repeat owners to verify | Demand-only until input suppression/repeat owner is reviewed. |

## Crops, Planting, Harvest

| Inputs | Candidate native owner | API conclusion |
| --- | --- | --- |
| `AutoHarvestMod`, `CropHarvestingQaMod` | `PlantBasin.CouldHarvest`, `PlantBasin.Harvest`, `Crop.GenCropOutput`, `Crop.AfterHarvest` | Crop-container only. Experimental/Diagnostic. |
| `StrongPlantingGunMod` | `ItemFarmingGun`, `FarmingGunUiState`, `LinearInventory`, `PlantBasin.Plant/Fertilizer/Protect` | Restricted Experimental fixed three-slot adapter. |
| DolocPlus one-click planting, FullTrainer room planting | same crop owners plus batch inventory transaction owners | Demand-only until native transaction boundaries are proven. |

## Fishing And Fish Roe

| Inputs | Candidate native owner | API conclusion |
| --- | --- | --- |
| `AutoFishingMod` | `BodyController.UseFishRod`, `AgentStateFishingReady/Cast/Wait/Battle/Pull`, `FishingGameScrollBar` | Strongest gameplay owner evidence, still Experimental. |
| `FishBreedingAssistantMod` | `ItemFishRoe`, `Item.get_title`, `Item.get_description`, `Item.GetDetailInfo` | Display-only partial. Real roe/fish breeding lookup not found. |
| DolocPlus fishing AFK, FullTrainer fishing edits | fishing state owners plus pool/probability owners to verify | Demand-only or Diagnostic depending on mutation. |

## Animal And Livestock

| Inputs | Candidate native owner | API conclusion |
| --- | --- | --- |
| `AnimalHusbandryProgressMod` | `AnimalFullInfoData`, `AnimalViewer.Show`, `AnimalPanel.RefreshViewer`, `Animal.husbandryValues` | Display-only Experimental. |
| DolocPlus animal collect/fondle, FullTrainer animal spawn | `Animal`, animal manager/controller/AI, feeders, toilets, product machines to verify | Demand-only. Complete new animals and runtime spawn remain blocked. |

## Inventory, Chests, Stack, Crafting

| Inputs | Candidate native owner | API conclusion |
| --- | --- | --- |
| `ChestLocatorEnhancerMod` | `ArchiveDataHandle.GetAvailableInventories`, `DolocAPI.CountItem`, `DolocAPI.TryCostItem`, `LinearInventory`, `Case`, `StorageShelf` | Experimental inventory-array extension. |
| Auto drone backpack sort, DolocPlus use-all-chests, FullTrainer stack/craft | `LinearInventory`, `InventorySystem`, craft transaction owners to verify | Demand-only until transaction and overflow rules are reviewed. |
| stack limits | `ItemInfo.Overlay`, `Item.TryCombine/TestCombine`, inventory placement paths | Existing domain owner found, mutation still transaction-gated. |

## Content Index And Encyclopedia

| Inputs | Candidate native owner | API conclusion |
| --- | --- | --- |
| `OilMod` item content | official content JSON, `ItemInfo` | Content Found. |
| `MineMod` item/recipe/tech content | `ItemFunctionEquipment`, `EquipmentFuncCase`, recipe and mod extension JSON | Content Found, runtime production separate. |
| ExpandedEncyclopedia, DolocPlus/FullTrainer database views | content/config and encyclopedia UI owners to verify | Demand-only. Needs new content index/encyclopedia native-owner review. |

## Machines And Production

| Inputs | Candidate native owner | API conclusion |
| --- | --- | --- |
| `MineMod` | `Equipment.IElectronicComponent`, `ElectronicComponentAppliance.Launch`, `Case.inventory`, `EquipmentRenderer.OnReuse` | Experimental/Gap. DTMAPI sidecar scheduler, not native machine scheduler. |
| FullTrainer no-power/instant crafting | machine and craft owners to verify | Diagnostic or blocked until owner review. |

## Vehicles, Drones, Camera

| Inputs | Candidate native owner | API conclusion |
| --- | --- | --- |
| `SecondMotorMod`, Infinite Hover | `ItemMotorKey`, `MotorController`, `MotorDataManager`, `AgentControllerState`, `DolocAPI` motor paths | Experimental native motor clone/lease. New vehicle type blocked. |
| Auto drone samples | `DroneStruct`, `DroneSlot`, `DroneController`, `DroneRenderer`, resource and inventory owners | Strong demand, native owner partial. Multi active drone blocked. |
| `ZoomMod`, DolocPlus camera/panorama | playable camera `orthographicSize`, background/fog/panorama owners to verify | `OrthographicOnly` Experimental. Background/fog/panorama separate deep dive. |

## Save UI, World, Map, Geometry

| Inputs | Candidate native owner | API conclusion |
| --- | --- | --- |
| `MoreSavesMod` | `DolocAPI.gameManager.archiveFileCount`, `LocalSave`, `DataPersistenceManager`, `GameDataPanel` | Experimental save UI adapter, not save format stability. |
| DebugConsole, DolocPlus/FullTrainer teleport/time/weather | `ArchiveDataHandle`, `DolocAPI.DoTransport/EnterRoom`, time/weather owners | Diagnostic or Experimental debug-only. |
| BuildingExpander | `RoomGeometrySO`, `BuildingProto`, `RoomHandleUtils` candidates | Runtime boundary mutation blocked. Query diagnostics only. |

## NPC, Player, Combat Backlog

| Inputs | Candidate native owner | API conclusion |
| --- | --- | --- |
| DolocPlus NPC info, FullTrainer NPC query | `Npc`, `NpcController`, `NpcScheduleAsset`, dialogue/store owners | Read-only query demand. New NPC creation remains blocked/proposed. |
| FullTrainer player status and combat/monster edits | player status, battle, monster spawn/drop owners to verify | Diagnostic or future native-owner domain. |
