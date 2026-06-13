# Round 1 / Agent E: Local Third-Party Mod Semantics

Status: Round 1 complete, docs-only
Date: 2026-06-13
Agent role: read-only sample inventory and semantic extraction
Source root: `references/third-party-mods`

## Scope And Method

This pass reviewed local third-party sample archives and folders without modifying them and without extracting anything into the repository. Archive listings used PowerShell, `tar`, and a temporary Python `py7zr`/`dnfile` install under `%TEMP%`; the temporary directory was removed after inspection.

Evidence included visible archive paths, file names, README/config text, screenshot text, .NET assembly/type/member metadata, BepInEx plugin metadata strings, and Cheat Engine table descriptions. It did not copy third-party code and did not inspect or reproduce method bodies.

Confidence scores below mean semantic/API-demand confidence, not native-owner stability. Round 2-4 review later tightened all third-party wording to `demand-only`, `clean-room only`, and `candidate native-owner domain to verify`.

## Sample Inventory

| Sample | Visible package contents | Readable metadata | Initial classification | Confidence |
| --- | --- | --- | --- | --- |
| `ExpandedEncyclopedia.zip` | `BepInEx/plugins/ExpandedEncyclopedia/ExpandedEncyclopedia.dll` | Assembly `com.coelaphage.plugins.ExpandedEncyclopedia`; type `ExpandedEncyclopedia.Patches.ExpandedEncyclopedia`; method names include `DolocConfig_Loader_Postfix`, `Patch_settings_globalparameters`, `Patch_item_tbitemprotos` | BepInEx content/UI metadata patch for encyclopedia/collection visibility | 82 |
| `HoldToHarvest.zip` | `BepInEx/plugins/HoldToHarvest/HoldToHarvest.dll` | Assembly `com.coelaphage.plugins.HoldToHarvest`; type `HoldToHarvest.Patches.HoldToHarvestAndPlant`; strings include `PlantBasin`, `enableHoldToHarvest`, `DolocAPI`, `get_SelectedItem` | BepInEx crop interaction convenience mod | 74 |
| `Infinite Hover.7z` | `Infinite-Hover.dll`, `Infinite-Hover.pdb` | Assembly `Infinite-Hover`; type `Infinite_Hover.Patches.InfiniteVehicleThrust`; strings include `MotorController`, `HarmonyPrefix` | BepInEx flying motor endurance/thrust patch | 88 |
| `Genesis.ContentLoader.7z` | `Genesis.ContentLoader.dll` | Assembly `Genesis.ContentLoader`; type `Genesis.ContentLoader.Patch`; strings include `HarmonyMethod`, `IPluginBase`, `DolocTown.Config`, `item` | Genesis framework content/config loader extension | 60 |
| `Genesis.Core.7z` | `Standalone/Genesis.Core.dll`, Doorstop config, Harmony/MonoMod/Cecil libraries, `winhttp.dll` | Doorstop target `Genesis.Core.dll`; assembly `Genesis.Core`; types include `Genesis.Config`, `Genesis.IPluginBase`; methods include `AddConfig`, `GetConfig` | Alternative standalone mod loader/framework | 80 for framework shape, 45 for gameplay API demand |
| `自动采集无人机_v1.0.zip` | Full BepInEx payload plus `BepInEx/plugins/BackpackSortMod.dll`; `使用说明.txt` says F1 toggles auto collection | Assembly `BackpackSortMod`; types include `AutoChopSystem`, `DroneUtils`, `SortKeyListener`; strings include `GetCurrentDrone`, `TryCollectResin`, `DungeonResourceRenderer`, `VegetationRenderer`, `DroneRenderer`, `TryCostPower`, `LinearInventory` | Auto drone resource collection plus backpack sort | 92 |
| `自动采集无人机_v1.1.zip` | Full BepInEx payload plus `BackpackSortMod.dll`; same F1 usage text | Same as v1.0 plus `BuildingExpanderPatches`, `RoomGeometrySO`, `BuildingProto`, `RoomHandleUtils`, `AutoExpandRoomColliders`, `ExpandCurrentRoomColliders` | Auto drone collection plus room/building expansion hooks | 94 |
| `【小型、中型、温室】更大的建造空间_v1.0.zip` | Full BepInEx payload plus `BuildingExpander.dll`; `操作说明.txt` links Steam guide `3619980568` | Assembly `BuildingExpander`; types include `BuildingExpanderListener`, `BuildingExpanderPatches`; methods include `TryExpandCurrentRoom`, `IsExpandableRoom`, `SetRoomMultiplier` | Buildable-room/greenhouse/small-medium room expansion | 93 |
| `屏幕截图 2026-05-21 192651.png` | Screenshot for auto drone sample | Text says F1 toggles auto collection; consumes in-game robot battery; ignores tool level; may collect story-locked resources and reduce difficulty | Supports auto-drone semantics and risk flags | 95 |
| `小神增强包/链接和密码.txt` | Steam guide URL `3477767443`, password `qiuzy` | Author/source pointer only | Package source metadata | 90 |
| `DolocTownEA_BepInEx_DolocPlusMod.7z` | Encrypted archive; password worked. Contains `BepInEx_win_x64_5.4.23.3.zip`, `Plugins.zip`, one-click installer, manual-install GIF, README, version marker `1.3.2` | README says F2 opens mod settings panel. `Plugins.zip` contains Configuration Manager and `DolocPlus.dll`. Assembly `DolocPlus`; many feature/controller names visible | Large BepInEx QoL/cheat feature bundle | 96 for visible feature inventory, 70 for exact native-owner claims |
| `DolocTownEA_Lua_FullTrainer_1.8.1.7z` | Encrypted archive; password worked. Contains Cheat Engine 7.5 runtime, `DolocTownEA_Lua_1.8.1.CT`, trainer batch/GIF | CT descriptions list many game edits, console, database, inventory, world/weather/map/NPC/animal/crafting/fishing functions | Cheat Engine trainer, not a DTMAPI-style mod | 90 for semantic list, 35 for native-owner stability |

## Semantic Targets And API Demand

| Semantic target | Sample evidence | Candidate native-owner domain to verify | DTMAPI API demand | Stability risk |
| --- | --- | --- | --- | --- |
| Encyclopedia/collection visibility expansion | `ExpandedEncyclopedia.dll` patches `DolocConfig` item/global parameter names | `09-food-equipment-effects`, `10-item-stack-quantity-limits`, content/index query surface; also native UI/content config owners outside the initial 12-domain split | Read-only content index, item taxonomy DTOs, encyclopedia visibility adapter, config-load event | Medium: config patch may be build-sensitive; UI display is not runtime owner proof |
| Hold-to-harvest and plant | `HoldToHarvest.dll` references `PlantBasin`, selected item, DolocAPI | Crop harvesting native review, map/resource domain `07`, inventory/items `10` | Continuous-action input API, crop maturity query, crop harvest/plant adapter with native `PlantBasin` owner | Medium/high: input repeat and crop transaction state need save/load and item-cost proof |
| Infinite flying motor thrust/endurance | `Infinite-Hover.dll` references `MotorController` | `06-flying-motor-vehicle-types` | Vehicle endurance/thrust modifier lease, diagnostic motor state query | High: native motor is singleton-oriented; should remain Experimental unless owner/restore is proven |
| Alternative mod loader and content loader | `Genesis.Core.dll`, Doorstop target, `Genesis.ContentLoader.dll`, `IPluginBase`, config helpers | Framework/core, Workshop/content pipeline | DTMAPI manifest/loader compatibility, content pack lifecycle, config registration, mod API registry | Medium for framework discovery; not direct gameplay API proof |
| Auto drone collection | Auto drone sample references `DroneStruct`, `DroneRenderer`, `TryCostPower`, resource renderers, resin collector | `05-drones-runtime-equipment`, `07-maps-dungeons-scenes-resources`, `10-item-stack-quantity-limits` | Active drone query, drone battery spend, resource eligibility scan, native resource collect adapter, result reporting, story-lock guard | High: screenshot explicitly says it ignores current tool level and may affect story progression |
| Backpack sort / stack to storage | Auto drone sample strings include `DoSort`, `LinearInventory`; FullTrainer includes stack-to-room-storage feature | `10-item-stack-quantity-limits`, inventory/chest APIs | Inventory sort command, room chest search, stack-to-existing-storage transaction, overflow result DTO | Medium/high: must respect containers, cartons, partial place, and save transaction boundaries |
| Larger buildable rooms/greenhouse | BuildingExpander and auto drone v1.1 reference `RoomGeometrySO`, `BuildingProto`, `RoomHandleUtils`, room size/grid/colliders | `07-maps-dungeons-scenes-resources` | Build-area query API, room geometry diagnostics, experimental room-boundary adapter | Very high: current native-owner domain report marks runtime map/boundary mutation blocked |
| DolocPlus auto harvest/resin/tank/animal collection | DolocPlus types `AutomaticHarvest`, `AutomaticResinCollecting`, `AutomaticTankCollecting`, `AutomaticAnimalCollecting`, `AutomaticAnimalFondle` | `03-animal-husbandry-behavior`, `07-maps-dungeons-scenes-resources`, crop/resource native reviews | Animal product collect/fondle adapter, fish tank output collect adapter, resin/resource collect adapter, automation scheduler | High: crosses product ownership, inventory placement, story/season/resources |
| Fishing AFK, instant fish farming, fish analysis | DolocPlus `FishingAFK`, `InstantFishFarming`, `FishAnalyzer`; FullTrainer fishing AFK/pool/probability/seconds-kill | `09-food-equipment-effects`, existing fishing native review, `12-held-ranged-weapons-projectiles` only for projectile-adjacent combat not fishing | Fishing automation API, fishing-pool query, fish eligibility/probability DTO, visible minigame/result policy | Medium/high: current `IFishingAutomationApi` is Experimental with fifth-save evidence only |
| One-click/current-room planting automation | DolocPlus `OneClickPlanting`, `PlantAutomator`; FullTrainer current-room planting | Crop harvesting native review, `07`, `10` | Planting transaction API, seed/film/fertilizer selection, room scan, material cost policy | High: large-room batch mutation and material costs must be native-owner proven |
| Time speed and world/weather edits | DolocPlus `TimeSpeed`; FullTrainer time flow, weather change, crop wet/mature | `01-world-time-weather-refresh` | Debug-only/experimental time/weather controls, world refresh diagnostics | High: date/weather mutation remains debug/experimental in native-owner report |
| World teleport, doors, map devices, NPC location | DolocPlus `Teleporter`, `TeleportPointManager`, `UnlockAllDoors`, NPC info UI; FullTrainer world teleport, NPC query/teleport | `07-maps-dungeons-scenes-resources`, `02-npc-body-behavior` | Teleport point query, diagnostic transport API, NPC location query DTO, door/access-rule diagnostics | High: direct transport and access-rule bypass must stay diagnostic unless lifecycle and quest locks are proven |
| NPC liking/gift/status database | DolocPlus `NPCInfoReader`, `GetLikingItemsSortedByNPCs`; FullTrainer NPC state query | `02-npc-body-behavior` | NPC identity, location, relation/gift preference read-only DTOs | Medium: read-only query is plausible; NPC behavior/story mutation remains blocked/proposed |
| Craft count/cost bypass and recipe database | DolocPlus `MaxCraftCount`, recipe-related strings; FullTrainer max craft limit removal, no-material workbench, recipe encyclopedia | `09-food-equipment-effects`, `10-item-stack-quantity-limits`, crafting API reviews needed | Recipe query DTO, max craft calculation hook, diagnostic cost bypass, craft transaction result | High: bypass/no-cost is cheat/debug-only; stable mods need native craft transaction owner |
| Use all room chests for material cost | DolocPlus `UseAllChests`, `UseAllChestsLocated`; FullTrainer room chest material recognition | Inventory/chest APIs, `10` | Room inventory discovery, cost preview, native `CountItem/CostItem` adapter, ownership policy | Medium/high: existing ChestLocator API is Experimental |
| Player status and movement cheats | FullTrainer infinite dash/jump, HP/energy/stamina, speed/time | Framework/gameplay state not fully in native-owner domain reports | Diagnostic player-state query/mutate APIs only | High: cheat style, save corruption/balance risk |
| Electric no-power, instant crafting, building invulnerability | FullTrainer electric no-power, all items no time, building not damaged | Machine/equipment/resource owners; `07`, `09`, machine API matrix | Machine production lease/diagnostic override, building damage diagnostics | High: should not become stable gameplay API without owner review |
| Spawn/clear monsters, one-hit kill | FullTrainer map spawn/clear monsters, monster one-hit kill | Combat/monster domain not yet in 12-domain library except projectile/damage hints | Future custom monster/combat native-owner review; diagnostic console only now | Very high: no stable native owner report in current library |
| Spawn arbitrary animal | FullTrainer spawn animal description | `03-animal-husbandry-behavior` | Animal creation request remains blocked/experimental; registry-only definitions possible | Very high: custom runtime animal creation is blocked in current native-owner report |
| Camera/background/panorama view | DolocPlus `CameraRescaler`, `BackGroundViewController`, `PanoramaViewController` | Camera API reviews, background native-owner review | CameraView lease, panorama/background future API | High: existing camera view is Experimental; background sync deferred |
| Config/menu/hotkeys | DolocPlus F2 Configuration Manager; FullTrainer CE UI; samples use F1/F2/F7 | Framework/config UI/input | DTMAPI config menu, keybind conflict query, safe input boundary | Medium: DTMAPI has StableCandidate config registration but UI/manual QA gates remain |

## Per-Sample Notes

### ExpandedEncyclopedia

- Evidence: archive contains only a BepInEx plugin DLL under `BepInEx/plugins/ExpandedEncyclopedia`.
- Visible metadata points to item proto/global parameter config patches, not a full new UI framework.
- Likely DTMAPI need: a supported way to make item/content entries visible in collection/encyclopedia-like views without raw `DolocConfig` patching.
- Gaps: no README/license/source; exact UI owner and save/load behavior unknown.
- License risk: high for redistribution or migration without permission.

### HoldToHarvest

- Evidence: BepInEx plugin DLL plus metadata strings for `PlantBasin`, selected item, and `enableHoldToHarvest`.
- Likely DTMAPI need: a crop/action repeat API that respects native mature/plant/harvest checks and input suppression rules.
- Gaps: no README/license/source; exact target native methods not proven from metadata alone.
- License risk: high for redistribution or migration without permission.

### Infinite Hover

- Evidence: DLL and PDB; metadata names `InfiniteVehicleThrust` and `MotorController`.
- Likely DTMAPI need: experimental motor endurance/thrust modifier owned by GameBridge, with restore on disable/save/title.
- Gaps: no README/license/source; native motor singleton risk remains.
- License risk: high for redistribution or migration without permission.

### Genesis Core And ContentLoader

- Evidence: `Genesis.Core.7z` is a Doorstop standalone loader with Harmony/MonoMod/Cecil dependencies; `Genesis.ContentLoader.7z` is a small Genesis plugin/content loader.
- Likely DTMAPI need: compatibility detector for non-BepInEx loaders, and DTMAPI manifest/content lifecycle that can absorb the useful content-loader role without mixing runtimes.
- Gaps: no README/license/source; no gameplay feature list in visible metadata.
- License risk: high for bundling; also runtime conflict risk if Genesis and DTMAPI both install Doorstop.

### Auto Drone v1.0 / v1.1

- Evidence: both packages are full BepInEx overwrite payloads with `BackpackSortMod.dll`; usage says F1 toggles auto collection. Screenshot states it consumes robot battery, ignores current tool level, may mine advanced nodes, and may affect story progression.
- v1.0 metadata points to drone/resource/resin/vegetation/dungeon resource collection and backpack sorting.
- v1.1 adds room/building expansion symbols, overlapping the separate BuildingExpander package.
- Likely DTMAPI need: separate stable read/query APIs from experimental automation:
  - drone active state query and battery cost adapter;
  - resource eligibility/collect adapters that respect native tool/story gates;
  - backpack sort and stack-to-storage transactions;
  - explicit warning/result reasons for story-locked resources.
- Gaps: no source/license; exact native collection owner and story-lock detection need method-body review and game evidence.
- License risk: very high for redistribution because the packages include BepInEx/runtime dependencies and closed plugin DLL.

### Building Expander

- Evidence: full BepInEx overwrite payload with `BuildingExpander.dll`; metadata targets `RoomGeometrySO`, `BuildingProto`, `RoomHandleUtils`, room grid/size/collider methods.
- Likely DTMAPI need: diagnostic room geometry query and a blocked/experimental build-space adapter.
- Gaps: runtime room boundary mutation is already marked blocked/high risk in native-owner domains; needs exact native owner and save/load/build placement evidence before any API.
- License risk: very high for redistribution; closed DLL and bundled runtime files.

### DolocPlus

- Evidence: encrypted package with password from local text; README says F2 opens a settings panel; `Plugins.zip` includes Configuration Manager and `DolocPlus.dll`; visible type/method names enumerate many feature modules.
- Key visible feature clusters:
  - automatic animal collect/fondle;
  - automatic harvest/resin/fish tank collection;
  - one-click planting and instant farming/fish farming;
  - fishing AFK and fish analyzer;
  - time speed, infinite jump/dash, tireless motor;
  - use all chests / located chests;
  - max craft count;
  - NPC info and liking items;
  - custom transition menu, world teleport, unlock doors;
  - item stacker, backpack sort, game console;
  - camera/background/panorama view.
- Likely DTMAPI need: this is the strongest local demand map for a broad QoL API set, but most entries should remain Experimental/Diagnostic until native owners are proven.
- Gaps: no source/license; exact native hooks are only symbol-level evidence. Many features are balance/cheat-like and need policy separation from ordinary stable mod APIs.
- License risk: very high; includes installer, BepInEx runtime zip, Configuration Manager, and closed plugin DLL.

### FullTrainer 1.8.1

- Evidence: Cheat Engine trainer package and CT table descriptions. This is not a BepInEx/DTMAPI mod, but it is useful as a semantic demand list.
- Key CT feature descriptions include:
  - infinite double jump, infinite sprint, motor endurance;
  - time speed and weather edits;
  - fishing pool probability analysis, fishing AFK, fishing instant kill;
  - NPC status query and teleport;
  - auto crop harvest, resin/fish-roe/animal product collection, animal fondle;
  - room chest material recognition, craft max count removal, stack-to-storage;
  - no-material workbenches, no-power machines, instant crafting;
  - world teleport, door/access bypass, map monster spawn/clear;
  - seed/recipe/food/buff/craft encyclopedias and tech-tree data;
  - backpack capacity, stack count, selected item quantity, item add/remove;
  - crop wet/mature changes, hide UI, spawn arbitrary animal.
- Likely DTMAPI need: diagnostic console and API backlog. It should not be used as stable runtime implementation proof.
- Gaps: CE memory scripts are high-risk and not suitable as DTMAPI code source; native owners must be found independently.
- License risk: very high; trainer bundles Cheat Engine runtime and scripts.

## License And Redistribution Risk Summary

| Risk | Samples | Notes |
| --- | --- | --- |
| Closed DLL with no license | ExpandedEncyclopedia, HoldToHarvest, Infinite Hover, Auto Drone, BuildingExpander, DolocPlus | Use only as compatibility/API-demand evidence unless permission is obtained. |
| Bundled BepInEx/runtime dependencies | Auto Drone, BuildingExpander, DolocPlus | DTMAPI should not merge or redistribute these payloads; use installer/runtime boundary instead. |
| Alternative loader conflict | Genesis Core | Doorstop/bootstrap ownership can conflict with DTMAPI/BepInEx install chain. |
| Cheat Engine trainer package | FullTrainer | High security/licensing/distribution risk; use only as local semantic evidence. |
| Steam guide / author authorization gap | 小神增强包 link/password, BuildingExpander guide link | Public guide existence is not migration permission. |

## Round 2 Questions For Review Agents

1. Does `ExpandedEncyclopedia` require a distinct encyclopedia/UI domain report, or can it be covered by content query plus item config owners?
2. Are `HoldToHarvest` and current `ICropHarvestingApi` aligned, or does holding-to-repeat require a separate input/action-repeat owner?
3. For Auto Drone, which native path should be treated as authoritative: drone battery, resource renderer harvest, tool-level check, or inventory placement?
4. Should v1.1 Auto Drone and BuildingExpander be considered separate samples or one author iteration with overlapping building-expansion code?
5. Which DolocPlus features are ordinary QoL candidates, and which must be classified as debug/cheat-only even if native owners are found?
6. Does `Genesis.ContentLoader` expose a compatibility requirement for DTMAPI Workshop/content packs, or is it only a competing loader runtime?
7. Which FullTrainer descriptions should seed new native-owner domains not covered by the existing 12 reports, especially monsters/combat/player-status/tech-tree databases?
8. What minimum permission/source evidence would be required before migrating any third-party sample rather than writing a clean-room DTMAPI equivalent?

## Initial Priority Backlog

| Priority | API demand | Reason |
| --- | --- | --- |
| P0 | Third-party mod compatibility detector and report | Many samples are full runtime overwrite packages; DTMAPI needs to warn users and avoid mixed loaders. |
| P0 | Permission/license ledger for local samples | Several packages are closed DLLs/trainers with no visible license. |
| P1 | Drone/resource automation native-owner deep dive | Auto Drone is a strong repeated demand and overlaps resources, drone power, inventory, and story locks. |
| P1 | Room/building expansion blocker review | BuildingExpander demand is clear, but current native-owner report marks boundary mutation high risk/blocked. |
| P1 | DolocPlus feature taxonomy | DolocPlus is broad enough to drive a QoL API roadmap, but needs QoL vs cheat/debug separation. |
| P2 | Encyclopedia/content visibility review | ExpandedEncyclopedia suggests content-query/UI API demand with lower gameplay risk. |
| P2 | Hold-to-repeat crop interaction review | Could become a safe input/action wrapper if native crop owners are respected. |
| P2 | Genesis runtime compatibility note | Useful to detect/avoid competing Doorstop/loader installs. |
