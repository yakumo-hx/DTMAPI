# DolocPlus / 小神增强包重叠功能学习记录

Date: 2026-06-07

Scope: read-only compatibility research. This note compares the user's DTMAPI feature set with the third-party DolocPlus / 小神增强包 features that overlap existing DTMAPI APIs or mods. It is not an implementation goal, not fix evidence, and not permission to copy third-party code.

Reference inputs:

- Workspace package: `references/third-party-mods/小神增强包`.
- BepInEx DLL extracted only to temp for inspection: `DolocPlus.dll`, version text says `1.3.2`.
- CE/Lua trainer extracted only to temp for inspection: `DolocTownEA_Lua_1.8.1.CT`.
- DTMAPI status docs: `docs/api/public-api-matrix.md`, `docs/hook-map/README.md`, `testmods/README.md`.
- Function-level companion: `research-DolocPlus-function-map-20260607.md`.
- Deep-dive companion: `research-DolocPlus-deep-dive-20260607.md`.

Important boundary: DolocPlus binaries and CE scripts are third-party references only. DTMAPI should learn concepts, game entry points, risk patterns, and acceptance targets, but should not copy code or bundle those binaries.

## High-Level Shape

DolocPlus uses two different styles:

1. DLL / BepInEx mod: Harmony patches and feature controllers such as `FishingAFK`, `UseAllChests`, `UseAllChestsLocated`, `OneClickPlanting`, `TimeSpeed`, `CameraRescaler`, `PanoramaViewController`, `GameConsoleController`, `NPCsInfoUiController`, `FishAnalyzerUiController`, and automation controllers.
2. CE / Lua trainer: Mono method lookup/invocation, official console discovery, direct `DolocAPI` calls, and high-power trainer actions such as monster generation, weather set, item edits, command execution, player stat edits, no-cost/no-time crafting, and hidden UI.

This explains why DolocPlus can ship many features quickly: it patches concrete game behavior directly. DTMAPI should usually absorb the same discoveries through GameBridge-owned hooks and stable DTO APIs, so ordinary mods do not need to touch raw Unity or decompiled Doloc Town types.

For the function-level mapping of each overlapping feature, see `research-DolocPlus-function-map-20260607.md`.

## Overlap Matrix

### 1. Settings / Config UI

DolocPlus:

- Uses BepInEx plus ConfigurationManager.
- Readme flow is external/plugin-style; user opens settings by hotkey.

DTMAPI:

- Uses in-game DTMAPI config menu and official/local mod packages.
- Existing config API includes localized names, bool/number/text/keybind/color options, conditional visibility, and inline controls.

Learning:

- DTMAPI is stronger for player-facing ecosystem consistency.
- DolocPlus is still useful as a reminder that every feature should expose simple toggles and conflict notes, especially automation/time-speed features.
- Future polish can improve descriptions/tooltips/grouping, but not by copying ConfigurationManager UI.

### 2. Y Console / Official Console / Advanced Debug

DolocPlus:

- DLL includes `GameConsoleController`.
- CE script has `GameData:GetAllConsoleCommands()` reading `Console.ConsoleSystem.commandSystem.vm.callables`.
- CE script executes commands through `DolocAPI:ExecuteCommand`.
- CE exposes/uses debug actions including weather, world teleport, monsters, item add/remove, crop maturity, and official console command listing.

DTMAPI:

- `DebugConsoleMod` + `IDebugConsoleApi` provide the Y-key console.
- `IAdvancedDebugApi` provides safe wrappers for time advance, time scale, money, tech points, tech-tree unlock, crop maturity, creative mode, creative generator, monster spawn, and resource spawn.
- `IWeatherDebugApi`, `ITeleportDebugApi`, `IInventoryDebugApi`, `IInstantSaveDebugApi`, `ITimeDebugApi`, and `IMovementDebugApi` cover much of the player-visible debug-console surface.

Learning / gap:

- DTMAPI should consider an experimental official console metadata API: list command names, parameter shapes, descriptions, and safe execution status.
- The Y console should prefer official command metadata where appropriate, but still keep dangerous raw commands behind strict whitelists.
- DolocPlus CE proves official command discovery is possible; DTMAPI should wrap it rather than exposing raw `ExecuteCommand` freely.

### 3. Time Speed / Time Jump

DolocPlus:

- DLL includes `TimeSpeed` and `TimeSpeedController`.
- CE includes time-flow speed controls.
- DolocPlus marks some features as incompatible with AFK fishing, which suggests conflict management matters.

DTMAPI:

- `ITimeDebugApi` skips to native weather-period boundaries.
- `IAdvancedDebugApi` exposes next-day/week/season style time advance and time scale.
- Current design tries to use native time flow rather than directly editing saved time.

Learning / gap:

- Time changes must have conflict policy with auto fishing, production machines, crop growth, and current-room machine catch-up.
- Evidence should distinguish simple value edits from real native time flow.
- A future time API should publish restore-on-exit/returned-to-title behavior and feature-conflict events.

### 4. Zoom / Panorama / Background Rendering

DolocPlus:

- `CameraRescaler.EnterPanoramaView` fits the camera to the whole current room.
- It adjusts `mainCamera.orthographicSize`, reflected `CameraController.camSize`, and room camera range.
- It scales `DolocAPI.envBackgroundEx` and `EnvCovariantController._depthFogController` by the orthographic-size ratio.
- It can use `PhotoState` for panorama/photo behavior, hide player/drone/motor, and restore state on exit.

DTMAPI:

- `ICameraZoomApi` + `ZoomMod` change camera view scale and restore it.
- A separate note already records that Zoom needs background/depth-fog compensation, not full photo-mode behavior.

Learning / gap:

- For Zoom, the missing overlap is background/fog scaling so the farm background does not remain a small rectangle.
- Full panorama/photo mode should be a separate future API (`IPanoramaCameraApi` or `IPhotoCameraApi`) and should not be mixed into Zoom.

### 5. Chest Locator / All Chests

DolocPlus:

- DLL includes `UseAllChests` and `UseAllChestsLocated`.
- Inspected method names include `Prefix_GetAvailableInventories`, `Transpiler_GetAvailableInventories`, and `Prefix_GetInventoriesAroundEquipment`.
- CE has "consume materials from current-room all chests" and "all farm chest-locator storage devices" style features.

DTMAPI:

- `IChestLocatorEnhancerApi` + `ChestLocatorEnhancerMod` support shared-inventory lookup across farm/building rooms for equipment material consumption.
- Smoke evidence verifies native `CountItem` / `CostItem` can see appended shared case inventories.

Learning / gap:

- DTMAPI should keep validating both material-count and material-cost paths, not only item discovery.
- DolocPlus suggests two separable modes: all chests in current room, and all locator-marked storage across the farm/building network.
- The DTMAPI version should preserve official locator semantics and avoid silently making every chest global unless the mod explicitly opts in.

### 6. Strong Planting Gun / One-Click Planting

DolocPlus:

- DLL includes `OneClickPlanting` and `PlantAutomator`.
- Inspected methods include `Prefix_PlantSeed`, `ConfirmAndAutomatePlanting`, `AutomaticPlanting`, and `SubscribeToSeedPlant`.
- CE has one-room planting automation for seed + watering + film + fertilizer.

DTMAPI:

- `IStrongPlantingGunApi` + `StrongPlantingGunMod` extend the official farming gun with seed/film/fertilizer slots and range behavior.
- The DTMAPI route stays closer to official tool interaction.

Learning / gap:

- DolocPlus is closer to whole-room automation; DTMAPI is closer to an upgraded official tool.
- If DTMAPI adds one-room automation later, it should probably be a separate `IFarmingAutomationApi`, not mixed into `IStrongPlantingGunApi`.
- Acceptance should require official basin rules: empty basin only for seed, damaged/missing film only for film, fertilizer only where legal, and native item consumption.

### 7. Auto Fishing / Fish Analysis

DolocPlus:

- DLL includes `FishingAFK` and `FishAnalyzer`.
- Inspected methods include `Prefix_UseFishingRod`, `Postfix_FishCastNextState`, `Prefix_FishWaitNextState`, `Postfix_FishWaitNextState`, `Prefix_UpdateGame`, `Postfix_UpdateGame`, `Prefix_NormalFishingKeyPress`, and `AnalyzeFishesInPreviousPool`.
- CE has fish pool fish/probability parsing and fishing instant-kill.

DTMAPI:

- `IFishingAutomationApi` + `AutoFishingMod` handle F6 enablement, auto cast, movement cancel, auto-complete and skip-minigame separation, and animation speed policy.
- `FishBreedingAssistantMod` is fish roe tooltip/info, not fish pool analysis.

Learning / gap:

- DolocPlus has a second fish-related capability DTMAPI does not yet have: fishing-pool analysis with fish species/probabilities/conditions.
- Future API candidate: `IFishingEnvironmentApi` or `IFishPoolAnalysisApi`.
- AutoFishing should retain conflict handling with time speed and input state, because DolocPlus treats fishing automation as state-sensitive.

### 8. Animal / Fish Tank Automation

DolocPlus:

- DLL includes `AutomaticAnimalCollecting`, `AutomaticAnimalFondle`, `AutomaticTankCollecting`, and `InstantFishFarming`.
- Inspected methods include honeycomb/chicken nest produce hooks, fish fry growth, fish roe incubation, tank ecological/electric-eel updates, and animal inventory lookup.

DTMAPI:

- `IAnimalViewerApi` + `AnimalHusbandryProgressMod` show hidden produce progress in the animal viewer.
- `ICustomAnimalApi` defines stable custom animal contracts but native runtime creation is blocked until adapters are verified.
- There is no current DTMAPI auto-pet/auto-collect/fish-tank automation API.

Learning / gap:

- DolocPlus demonstrates a broader husbandry automation category, while DTMAPI currently focuses on viewer information and future entity contracts.
- If requested, add `IAnimalAutomationApi` or `IFishTankAutomationApi` separately from hidden-produce UI.
- Hidden-produce display should keep native UI stability as its own problem; automation does not solve the animal viewer flicker/layout issues.

### 9. Movement / Action / Motor QoL

DolocPlus:

- Includes infinite jump, infinite dash, and tireless motor.
- Inspected methods include `Prefix_SupportDash`, `Prefix_Dash`, `Prefix_DoubleJumpTimes`, and `Prefix_CostEndurance`.

DTMAPI:

- `IActionSpeedApi` accelerates tool/shared-use/eat/fill interactions.
- `IMovementDebugApi` changes player movement speed for the Y console.
- `IMotorVehicleApi` supports original/second motor state and second motor example.

Learning / gap:

- DolocPlus movement features are more cheat-like and not the same as ActionSpeed.
- If DTMAPI supports jump/dash/motor endurance later, it should be a debug or movement-ability API with clear save/restore behavior.
- Motor endurance is separate from second-motor creation.

### 10. Crafting / Batch / No-Cost / No-Time

DolocPlus:

- DLL includes `MaxCraftCount` and `InstantFarming`.
- CE includes no-material crafting, no-power equipment, all item crafting no time, skill exp multiplier, player stat edits, and recipe/database pages.

DTMAPI:

- `IAdvancedDebugApi` includes creative no-cost/no-time state.
- `OneActionCompleteMod` covers resource/tool completion, not crafting-batch caps.
- `IMachineProductionApi` covers custom machine production definitions.

Learning / gap:

- Recipe batch cap removal is a distinct QoL API candidate, separate from creative mode.
- Creative/no-cost/no-time should remain debug-style or explicitly marked as cheat functionality.
- Recipe/database analysis could become content-query tooling, but should not be folded into unrelated machine APIs.

### 11. NPC Info / Teleport / Map Transition

DolocPlus:

- DLL includes `NPCInfoReader`, `NPCsInfoUiController`, `TeleportPointManager`, `Teleporter`, and `CustomTransitionMenu`.
- Inspected methods include `GetNPCsInfo`, `GetLikingItemsSortedByNPCs`, `OpenTeleportUI`, `TeleportOrDelete`, and `DoTeleport`.
- CE includes NPC status query and teleport.

DTMAPI:

- `ITeleportDebugApi` exposes whitelisted native mark/station destinations and CSV export.
- No NPC status/favorability/location API exists yet.

Learning / gap:

- Future API candidate: `INpcDebugApi` or `INpcInfoApi` for status, room, position, schedule/state, liking/favorability, and safe teleport-to-NPC.
- Teleport destination review should compare against DolocPlus-style saved teleport points and native station/ship logic.

### 12. Inventory Stack / Store / Cargo Drone

DolocPlus:

- DLL includes `ItemStacker` and `ParkingApronShopping`.
- CE includes backpack stacking into room chests and store/cargo-drone delivery features.

DTMAPI:

- No direct equivalent yet, except inventory give/debug and chest locator enhancement.

Learning / gap:

- Future API candidates: `IInventoryTransferApi`, `IStoreApi`, and cargo-drone delivery helpers.
- These should remain separate from `IInventoryDebugApi`, because debug item giving and player QoL inventory transfer have different safety constraints.

## Current DTMAPI-Unique Areas

The following current DTMAPI areas do not have a clear DolocPlus overlap in this pass:

- Official-local content indexing for Workshop/local source metadata.
- `OilMod`, `MineMod`, machine-production content API.
- `MoreEquipmentSlotsMod` and `IEquipmentSlotsApi`.
- `MoreSavesMod` and `ISaveSlotsApi`.
- `SecondMotorMod` and independent second-motor instance routing.
- Stable custom animal/monster/attack/drone definition APIs in 0.4.0.

## Risk Lessons From The Overlap

1. Direct patching makes features fast, but DTMAPI should route fragile logic through GameBridge and expose stable DTOs.
2. Debug/cheat features need strict whitelists and restore behavior, especially official console execution, time scaling, monster/resource spawning, no-cost/no-time crafting, and player stat edits.
3. Automation features need conflict matrices, especially fishing + time speed, chest lookup + crafting/equipment, and planting automation + official basin rules.
4. UI features need lifecycle tests. DolocPlus has many hotkey panels; DTMAPI's Y console/config pages need input isolation, stale-state reset, hover behavior, and language switching evidence.
5. Full panorama/photo behavior is not the same as zoom. Zoom needs background/depth-fog compensation only; panorama needs a separate API and stricter state restoration.

## Best Next API Candidates From Overlap

These are candidates, not goals:

- `IOfficialConsoleCommandApi`: read official command metadata and execute only whitelisted safe commands.
- `IFishPoolAnalysisApi`: current pool fish list, probability, unlock/condition explanation.
- `INpcInfoApi`: NPC room/position/status/liking/safe teleport.
- `IInventoryTransferApi`: stack backpack items into matching storage and query eligible target inventories.
- `IFarmingAutomationApi`: whole-room planting/watering/film/fertilizer automation independent from StrongPlantingGun.
- `IAnimalAutomationApi` / `IFishTankAutomationApi`: optional auto pet/collect/incubation helpers separate from viewer UI.
- `IRecipeBatchApi` / `ICraftingDebugApi`: batch limit and no-time/no-cost debug policies, separate from normal machine APIs.
- `IPanoramaCameraApi`: full-room photo/panorama mode, separate from `ICameraZoomApi`.

## Recommended Use In Future Goals

When a future goal references DolocPlus:

1. State that DolocPlus is behavior reference only.
2. Prefer "same player-visible outcome" over "same implementation".
3. Require DTMAPI GameBridge ownership for Harmony/reflection/native calls.
4. Require public APIs to avoid raw Unity/decompiled types.
5. Require third-save smoke evidence for every overlapping feature that touches runtime state.
