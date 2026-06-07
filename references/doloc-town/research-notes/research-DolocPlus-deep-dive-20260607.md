# DolocPlus / 小神增强包逐功能深度拆解

Date: 2026-06-07

Scope: read-only third-party compatibility research. This file records how the DolocPlus DLL and CE/Lua trainer appear to implement each major feature, which native Doloc Town call paths are involved, and what DTMAPI can learn without copying third-party code.

Boundary:

- DolocPlus binaries and CE scripts are reference material only.
- Do not copy DolocPlus code, embedded resources, or patch bodies into DTMAPI.
- Use this document to identify stable native seams, acceptance targets, and GameBridge-owned hook candidates.
- Public DTMAPI APIs should expose DTOs and intent-level operations, not raw decompiled game types.

Evidence labels:

- `DLL confirmed`: found in `DolocPlus.dll` Harmony patch metadata or method call references.
- `CE confirmed`: found in `DolocTownEA_Lua_1.8.1.CT` Mono method lookup/invocation or method patch logic.
- `Game confirmed`: verified by reading the local decompiled Doloc Town build under `references/doloc-town/reverse/builds/23465763_workshop_38581E`.
- `Inferred`: behavior follows from method names/call paths, but needs runtime validation before becoming DTMAPI contract.

Companion docs:

- Function map: `research-DolocPlus-function-map-20260607.md`
- Overlap study: `research-DolocPlus-overlap-study-20260607.md`
- Panorama/Zoom note: `docs/reviews/manual-qa/2026/20260607-0001-panorama-background-zoom-note.md`

## 0. Architecture Pattern

DolocPlus is two tools sharing a feature set:

- BepInEx DLL: runtime Harmony patches, feature controllers, hotkeys, custom UI, camera changes, automation patches.
- CE/Lua trainer: Mono method discovery, direct method invocation, and byte-level method edits for high-power trainer features.

DTMAPI should not mirror the CE style. The useful lesson is the target map: DolocPlus identifies which game functions carry real behavior. DTMAPI should turn those into GameBridge-owned hooks and stable public APIs.

Important structural lesson:

- DolocPlus can patch concrete game methods directly because it is a single trainer/mod.
- DTMAPI is an ecosystem API, so every fragile hook must stay in `DTMAPI.GameBridge.DolocTown`.
- Ordinary DTMAPI mods should request behaviors like "advance time", "collect eligible animal products", or "query fish pool" through typed APIs.

## 1. Runtime, Hotkeys, Input Isolation

Observed implementation:

- `DLL confirmed`: `DolocPlusMod.Awake`, `Update`, and `OnApplicationQuit` form the plugin lifecycle.
- `DLL confirmed`: `PatchController.Initialize/Dispose/OnFeatureToggled` centralizes Harmony patch enablement.
- `DLL confirmed`: `HotkeyController.Initialize/Update` centralizes hotkey polling.
- `DLL confirmed`: `FeatureUtils.DisableModUserInputs`, `EnableModUserInputs`, and `CheckConflicts` exist as shared lifecycle utilities.
- `CE confirmed`: the trainer stores enabled feature flags and restores patched methods on close.

Native path:

- No single Doloc Town function is the feature. This is plugin hygiene around user input, feature conflicts, and cleanup.

DTMAPI lesson:

- Every UI-heavy mod must explicitly isolate input from the game while its overlay is focused.
- Every automation/time-speed/fishing feature needs a conflict table and restore path.
- For DTMAPI, this belongs in Core/UI host services: active overlay capture, key/button routing, and "restore on save exit / main menu / quit".

Risk if not done:

- Y-console typing leaks into gameplay.
- Left-click UI actions also use tools.
- Automation state survives save unload.
- A compacted context may incorrectly treat smoke success as user-facing stability.

## 2. Auto Fishing / AFK Fishing

Observed implementation:

- `DLL confirmed`: patches `AgentStateFishingReady.OnExit`.
- `DLL confirmed`: patches `AgentStateFishingCast.NextState`.
- `DLL confirmed`: patches `AgentStateFishingWait.NextState`.
- `DLL confirmed`: patches `FishingGameScrollBar.UpdateGame`.
- `DLL confirmed`: patches `DolocUserInput.get_NormalFishing`.
- `DLL confirmed`: patches `ItemFishingRod.OnUseAsTool`.
- `CE confirmed`: trainer AFK fishing modifies fishing-ready cast timer data, fishing state transitions, `ItemFishingRod.OnUseAsTool`, and `FishingGameScrollBar` timing/current-note checks.
- `CE confirmed`: trainer refuses AFK fishing when another fish helper or time-speed indicator is active.

Native path:

- `ItemFishingRod.Use` calls `DolocAPI.agent.UseFishRod(this)`.
- `AgentStateFishingReady.NextState` holds cast progress until tool input is released/animation completes, then moves to `AgentStateFishingCast`.
- `AgentStateFishingCast.OnTouchPool` binds the touched pool and marks fishing UI control.
- `AgentStateFishingWait.NextState` decides bite wait, movement cancel, failed pull, battle start, or skipped minigame pull.
- `FishingGameScrollBar.UpdateGame` evaluates active fishing input and note timing for minigame success/failure.

Stable behavior to learn:

- A reliable auto-fishing implementation is a state machine, not only a hotkey.
- "F6 enabled" must immediately enter the same native path as using a rod, if a valid rod and fishing context exist.
- "Auto complete minigame" and "skip minigame" are distinct:
  - auto complete should play the minigame path and drive it to success;
  - skip should bypass/finish without playing the UI.
- Cast/reel animation speed must target fishing ready/cast/pull states, not generic tool animation only.

DTMAPI API candidate:

- `IFishingAutomationApi`
  - `CanStartAutoFishing`
  - `StartAutoFishing`
  - `StopAutoFishing(reason)`
  - `SetMiniGamePolicy(autoComplete, skip)`
  - `SetAnimationSpeedMultiplier`
  - events: `CastStarted`, `PoolTouched`, `BiteReady`, `BattleStarted`, `CatchCompleted`, `Stopped`

Validation targets:

- Short F6 press starts rod use.
- Movement/jump/sprint cancels.
- No toast spam on save exit or every cast.
- Auto-complete-without-skip visibly goes through the minigame success path.
- Time speed conflict is either blocked or explicitly handled.

## 3. Fish Pool Analysis

Observed implementation:

- `DLL confirmed`: `FishAnalyzer.AnalyzeFishesInPreviousPool`, `RunValidations`, `CheckFishRod`, `CheckFishingLv`, `CheckWeather`, `CheckMonth`, `CheckTime`, and `CheckUnlocked`.
- `CE confirmed`: `GameData:AnalyzeFishesInPool` reads current month/hour/weather, current fishing rod level, touched fishing pool, fishing pool table, fish table, and tech level.
- `CE confirmed`: the trainer computes eligible fish and garbage probabilities by rarity weights and pool garbage probability.

Native path:

- Current fishing context provides `FishingCache.FishingRod` and `FishingCache.FishingPool`.
- Static config tables provide pool fish lists, rarity weights, fish unlocks, weather/time/month/fishing-level constraints, and prices.
- Tech levels influence eligibility.

Stable behavior to learn:

- Fish pool display should not guess from item descriptions.
- It should calculate from the same table constraints used by native fishing.
- Probability should split by rarity weight and then by matching fish count in that rarity.

DTMAPI API candidate:

- `IFishingInfoApi`
  - `GetCurrentPoolAnalysis`
  - `GetPoolAnalysis(poolId, date, weather, rodLevel, techLevels)`
  - DTO fields: fish id, localized name, unlocked, eligible, probability, rarity, expected value, failed constraints.

Validation targets:

- Same pool/date/weather changes eligible fish.
- Unlocked and locked fish are separated.
- Garbage probability is visible and not mixed into fish probability incorrectly.

## 4. Infinite Jump, Infinite Dash, Tireless Motor

Observed implementation:

- `DLL confirmed`: `InfiniteJump` patches `ArchiveOperationGlobal.DoubleJumpTimes`.
- `DLL confirmed`: `InfiniteDash` patches `AgentStateDash.get_SupportDash` and `BodyController.Dash`.
- `DLL confirmed`: `TirelessMotor` patches `MotorController.CostEndurance`.
- `CE confirmed`: infinite jump returns a very high jump count; infinite dash redirects support checks; tireless motor zeros the endurance cost.

Native path:

- `ArchiveOperationGlobal.DoubleJumpTimes` gates double-jump count based on unlock/config.
- `AgentStateDash.SupportDash` and `BodyController.Dash` gate dash support/cooldown/state transition.
- `MotorController.CostEndurance(dt)` reduces motor endurance while flying/riding.

Stable behavior to learn:

- These are debug/cheat movement toggles, not action-speed features.
- They should be implemented as explicit debug/player movement APIs, not hidden side effects of other mods.

DTMAPI API candidate:

- `IMovementDebugApi`
  - `SetJumpOverride(count | unlimited)`
  - `SetDashPolicy(normal | always | disabled)`
  - `SetMotorEndurancePolicy(normal | noCost)`

Validation targets:

- Toggle off restores native movement.
- Motor no-cost does not change speed or summon logic.
- Jump/dash changes do not remain after save unload.

## 5. Time Speed / Time Flow

Observed implementation:

- `DLL confirmed`: `TimeSpeed` patches `GameLoop.FixedUpdate`.
- `CE confirmed`: time-speed edits `secondTimer.currentTime` and `secondTimer.interval`; values above 1 reduce the interval, and a special indicator resets current time.

Native path:

- `GameLoop.FixedUpdate` advances user input and calls archive update when the second timer ticks.
- Native archive update is the path that should advance crops, machines, weather-time segments, and other simulation.

Stable behavior to learn:

- Correct time speed changes timer cadence so the normal archive update still runs.
- It should not directly edit date/time without calling the game's update pipeline unless the operation is explicitly "teleport time".
- Time speed can conflict with fishing automation, machine production tests, and save/load evidence.

DTMAPI API candidate:

- `ITimeDebugApi`
  - `SetTimeScale(1, 2, 4, 8, 16)`
  - `AdvanceToNextPeriod`
  - `AdvanceDays`
  - `RunSimulationUntil(targetDateTime, mode)`

Validation targets:

- Machines in current room and unloaded rooms both advance.
- Crops grow according to normal simulation rules.
- Time scale resets on save exit and game quit.

## 6. Use All Chests / Chest Locator / Stack Backpack

Observed implementation:

- `DLL confirmed`: `UseAllChests` patches `ArchiveDataHandle.GetAvailableInventories`.
- `DLL confirmed`: `UseAllChestsLocated` patches `DolocAPI.GetInventoriesAroundEquipment` and uses a transpiler on `ArchiveDataHandle.GetAvailableInventories`.
- `CE confirmed`: `UseAllBoxes` changes the inventory query area to a large rectangle and enables box use.
- `CE confirmed`: `EZSortBackpack` reads player inventory and room inventories, then moves stackable backpack items into available storage.

Native path:

- `ArchiveDataHandle.GetAvailableInventories(anchor, area, useBox)` starts with player inventory, scans current room equipment, adds shared/nearby cases and storage shelves, sorts by distance, then optionally includes item boxes.
- `DolocAPI.GetInventoriesAroundEquipment(equipment)` computes an area around an equipment and delegates to `GetAvailableInventories`.
- Workbenches and synthesizers use the returned inventories for `CountItem`, `MaxCost`, and `CostItem`.

Stable behavior to learn:

- The strongest extension point is the inventory query, not every individual workbench.
- Whole-farm support must decide whether to include all rooms, only farm rooms, only rooms with locator devices, or current building groups.
- Stack-to-storage must respect slot locks, max stack overlay, existing item matching, and failed placement.

DTMAPI API candidate:

- `IInventoryScopeApi`
  - `RegisterInventoryProvider`
  - `GetCraftingInventoriesFor(equipmentId)`
  - `GetStorageTargets(scope)`
  - `StackBackpackToKnownStorage(policy)`

Validation targets:

- Workbench, equipment workbench, synthesizer, cooking, and device-input paths use the same inventory scope.
- No storage duplication.
- Locked slots remain untouched.
- Cross-room locator support is opt-in and visible.

## 7. Auto Harvest Crops

Observed implementation:

- `DLL confirmed`: `AutomaticHarvest` patches `Crop.CheckGrowthMonth` and calls harvest logic when mature.
- `CE confirmed`: `AutomaticHarvest` patches crop growth/check path and calls `PlantBasin.Harvest` when `Crop.data.isMature` and render/host conditions allow it.
- `CE confirmed`: optional husbandry-harvest flag prevents harvesting animal-building plants incorrectly.

Native path:

- `PlantBasin.Update/UpdateNoRender` runs growth via crop checks.
- `PlantBasin.Harvest` handles mature/dead crop output, tech exp, broadcast, and post-harvest state.
- `Crop.data.isMature`, `Crop.Renderer`, and `Crop.plantBasin` determine whether a crop can be harvested.

Stable behavior to learn:

- Auto harvest should call native `PlantBasin.Harvest`, not synthesize output.
- Animal-building or husbandry plants may need special exclusion rules.
- Rendered and non-rendered rooms both matter.

DTMAPI API candidate:

- `IFarmingAutomationApi`
  - `CollectMatureCrops(scope, policy)`
  - `PreviewHarvestableCrops(scope)`
  - event: `CropAutoHarvested`

Validation targets:

- Mature crop output matches manual harvest.
- Dead crop behavior is unchanged.
- Offscreen/unloaded room behavior is consistent.

## 8. One-Click / Batch Planting

Observed implementation:

- `DLL confirmed`: `OneClickPlanting` patches `ItemSeed.PlantSeed` and has `PlantAutomator.AutomaticPlanting`, `TryCostItem`, and event subscription helpers.
- `CE confirmed`: `PlantAutomation` can invoke `DolocAPI.Command_PlantCrop` and optional mulch/fertilizer toggles use a shared `plantAutomationStack`.
- `Game confirmed`: `ItemSeed.Use` and `ItemSeed.PlantSeed` enforce basin seed type, season, empty basin, and item cost.
- `Game confirmed`: `ItemFarmingGun.OnUseAsTool` scans a selected area and calls native interactions for seed, film, fertilizer, or water.

Native path:

- Seed path: `ItemSeed.Use` -> selected `PlantBasin` / `PlantSeed` -> `PlantBasin.Plant`.
- Farming gun path: area scan -> `CheckCanInteract` -> `CostSelf` -> `DoInteract` -> `PlantBasin.Plant`, `Protect`, `Fertilizer`, or `Water`.

Stable behavior to learn:

- Strong planting should extend the official farming gun because it already handles area targeting and legal basin checks.
- One-click whole-room planting is more intrusive and should remain separate from normal tool enhancement.
- Seed/film/fertilizer/water order must be deterministic and inventory-cost aware.

DTMAPI API candidate:

- `IFarmingGunApi`
  - `SetSlot(seed|film|fertilizer)`
  - `SetAreaSize`
  - `ApplyToArea(policy)`
  - DTO: per-basin result with skipped reason.

Validation targets:

- Empty basins only receive seeds.
- Film only replaces missing/broken protection.
- Fertilizer obeys native acceptance.
- Partial inventory shortage behaves like native tool use.

## 9. Auto Resin Collection

Observed implementation:

- `DLL confirmed`: `AutomaticResinCollecting` patches `ResinCollector.UpdateNoRender`.
- `CE confirmed`: trainer checks `ResinCollector.currentValue` against `func.Capacity`, then calls `ResinCollector.Collect(false)` and `UpdateCurrentValue(0)`.

Native path:

- `ResinCollector.UpdateNoRender` advances resin value while offscreen.
- `ResinCollector.Collect` converts stored value into output items.
- Output depends on the connected tree/plant decal host or default output rules.

Stable behavior to learn:

- Collection should happen at full capacity or by explicit policy.
- Native collect should be used because it knows output type.
- Resetting counters manually is dangerous unless the native method expects it.

DTMAPI API candidate:

- `IResinAutomationApi`
  - `CollectReadyResin(scope)`
  - `GetResinCollectorStatus`

Validation targets:

- Tree-specific resin remains correct.
- No duplicate output after render/no-render transitions.

## 10. Auto Fish Tank Collection / Instant Fish Farming

Observed implementation:

- `DLL confirmed`: `AutomaticTankCollecting` patches `FishTank.Update`, `FishTank.UpdateNoRender`, `FishTankEcological.Update`, `FishTankEcological.UpdateNoRender`, `FishTankElectricEel.Update`, and `FishTankElectricEel.UpdateNoRender`.
- `DLL confirmed`: `InstantFishFarming` patches `FarmFishTank.GrowFries`, `ItemFishFry.Grow`, and `ItemFishRoe.Incubate`.
- `CE confirmed`: auto tank collection calls `FarmFishTank.CollectProductItems` when `FarmFishTank.HasProduct`.

Native path:

- `FishTank.Update/UpdateNoRender` advances fry growth and metabolism.
- `FarmFishTank.AddMetabolism` reaches thresholds and fills product queues.
- `FarmFishTank.GetProductItems` converts product strings into items or fish roe, adds tech exp, and clears product storage.
- `ItemFishRoe.Incubate` and `ItemFishFry.Grow` advance hatch/growth counters.

Stable behavior to learn:

- Tank products are queued state, not one item floating in the world.
- Fry/roe instant growth should be a separate debug feature because it bypasses normal time.
- Collection should not skip energy/metabolism unless explicitly configured.

DTMAPI API candidate:

- `IFishTankApi`
  - `GetTankStatus`
  - `CollectProducts`
  - `SetGrowthPolicy(normal|accelerated|instant)`

Validation targets:

- Energy use remains native unless debug mode says otherwise.
- Product queue capacity is respected.
- Rendered/offscreen tanks behave the same.

## 11. Animal Product Collection / Auto Fondle

Observed implementation:

- `DLL confirmed`: `AutomaticAnimalCollecting` patches `HoneyComb.IsHoneyCombFull`, `HoneyComb.ProduceHoney`, `ChickenNest.IsFull`, `ChickenNest.Produce`, `Animal.Update`, and `Animal.UpdateNoRender`.
- `DLL confirmed`: `AutomaticAnimalFondle` patches `Animal.Move`.
- `CE confirmed`: animal automation reads `Animal.currentRoom`, sleep/escaped/fondled/mood/proto fields, calls `Animal.ProduceAsItems`, `DolocAPI.GenerateDropItems`, `Animal.Fondle`, and `DolocAPI.AddTechExp`.
- `CE confirmed`: non-rendered fondle manually updates mood/fondle flag and adds animal tech exp.

Native path:

- `Animal.ProduceAsItems` checks metabolism and mood, broadcasts produce, adds animal tech exp, consumes metabolism, and creates base plus hidden products.
- `AnimalAI` moves animals to nest/honey/milking equipment and calls equipment `Produce` using `animal.ProduceAsItems()`.
- `HoneyComb.ProduceHoney` and `ChickenNest.Produce` append product strings to equipment storage.
- `Animal.Fondle` updates mood/fondled status when rendered/interactable.

Stable behavior to learn:

- Hidden products are part of `Animal.ProduceAsItems`; duplicating that call can consume hidden progress or duplicate output.
- Rendered and non-rendered animal logic differs.
- Fondle automation must account for sleep/escaped/hasFondled and native mood cap behavior.

DTMAPI API candidate:

- `IAnimalAutomationApi`
  - `GetAnimalStatus`
  - `CollectEligibleProducts`
  - `FondleEligibleAnimals`
  - DTO includes base product, hidden product progress, mood, hunger/metabolism, sleep/escaped/fondled.

Validation targets:

- Hidden progress and base products match manual bell UI.
- Animals with zero hidden progress still show stable status if UI requests it.
- Auto-collect does not double-consume metabolism.

## 12. Max Craft Count / No-Cost Crafting / Instant Crafting

Observed implementation:

- `DLL confirmed`: `MaxCraftCount` patches `ConversionRecipeUiState.get_maxCraftCount` and `Synthesizer.GetMaxCraftCount` overloads.
- `CE confirmed`: `ProMaxCraftCount` forces a high max count and patches multiple count paths.
- `CE confirmed`: `NoCostCrafting` patches crafting count/cost functions for workbench/equipment/cooking modes.
- `CE confirmed`: `InstantCrafting` patches machine/crafting time path.

Native path:

- Recipe UI uses max craft count to decide batch limits.
- `Synthesizer.GetMaxCraftCount` counts inputs from available inventories.
- Crafting cost paths use `DolocAPI.CountItem`, `CostItem`, `MaxCostItem`, and inventory providers.
- Production equipment consumes time and inputs through machine-specific logic.

Stable behavior to learn:

- Removing UI count limits is not the same as no-cost crafting.
- Creative mode should be explicit and reversible.
- No-cost/no-time should be a scoped policy so normal mods cannot accidentally make all recipes free.

DTMAPI API candidate:

- `ICraftingPolicyApi`
  - `SetBatchLimitPolicy`
  - `SetCreativeCraftingPolicy`
  - `SetMachineTimePolicy`
  - events: `RecipeCostRequested`, `MachineProductionRequested`

Validation targets:

- Turning creative off restores cost and time.
- Workbench, device workbench, cooking, and machines are covered separately.
- No stack overflow/long UI freeze from huge craft counts.

## 13. Cargo Drone Shopping / Parking Apron Shop

Observed implementation:

- `DLL confirmed`: `ParkingApronShopping` patches `Store.SpawnStoreItems`, `Store.get_ItemCategory`, `Store.GetItemUnitBuyingPrice`, `StoreUiState.BuyItemInternal`, `StoreUiState.Show`, `StoreUiState.Hide`, `EquipmentPatch.CreateDropItemMoney`, `ParkingApron.OnTouch`, and `ParkingApron.OnDisTouch`.
- `DLL confirmed`: `LaunchDelivery` manipulates parking apron launch and delivery state.

Native path:

- `StoreUiState.BuyItemInternal` buys through `Store.BuyItem`, subtracts current money, places item through native inventory/drop paths, and records bought items.
- `ParkingApron` owns launch availability, pending payment, item return queue, wait counter, inventory/store association, and launch animation/events.

Stable behavior to learn:

- Cargo shopping is a hybrid of store UI and drone/parking apron state.
- It should not directly place items without matching payment and return flow.
- It needs strong UI lifecycle cleanup because the store UI is borrowed outside a normal shop.

DTMAPI API candidate:

- `ICargoDroneApi`
  - `OpenCargoShop`
  - `GetCargoShopItems`
  - `LaunchDelivery`
  - events: `CargoShopOpened`, `DeliveryLaunched`, `DeliveryArrived`

Validation targets:

- Buying subtracts money once.
- Leaving UI returns pending items safely.
- Delivery duration and pending payment match native parking apron rules.

## 14. Enhanced Traffic Light

Observed implementation:

- `DLL confirmed`: `EnhancedTrafficLight` patches `CropTrafficLight.RenderTrafficLights`.
- `DLL confirmed`: it reads the owning `Building.room`, iterates `IEquipmentHost.AllEquipments`, checks `IDropItemHost.DM_dropitem`, and uses `MaterialPropertyBlock` / SpriteRenderer color.

Native path:

- `CropTrafficLight` renders official crop/equipment status lights.
- Indoor building room equipment and drop-item hosts can indicate pending work or products.

Stable behavior to learn:

- The valuable concept is a room-level "action needed" aggregate API, not just a traffic-light patch.
- DTMAPI can support mods that report status from machines, storage, drops, crops, and animals.

DTMAPI API candidate:

- `IRoomStatusIndicatorApi`
  - `RegisterStatusProvider`
  - `GetRoomStatus(roomId)`
  - `SetTrafficLightOverlay`

Validation targets:

- Indoor generated rooms are scanned.
- Machines with pending output, missing input, or blocked output are distinguishable.
- Rendering changes do not permanently tint sprites.

## 15. NPC Status / Gift Info / NPC Teleport

Observed implementation:

- `DLL confirmed`: `NPCInfoReader` provides `GetNPCsInfo`, `GetLocation`, `GetLikingItemsSortedByNPCs`, and `GetFavoritesBy`.
- `CE confirmed`: `GameData:GetNPCsInfoDict`, `GetNPCsLikingVals`, `GetNPCsGiftCfgDict`, and `AddNpcLikingValue`.
- `CE confirmed`: NPC teleport uses room/dungeon teleport methods depending on location and asks confirmation.

Native path:

- NPC manager exposes all NPCs, current room, reachable state, and position.
- Item/gift config tables expose liking values and favorite categories.
- Teleport uses `DolocAPI.EnterRoom` for normal rooms or dungeon-specific room lookup for dungeon positions.

Stable behavior to learn:

- NPC info is read-only utility plus optional debug teleport.
- Teleporting to NPC needs dungeon awareness and should not be confused with station travel.
- Liking edits are trainer/debug only.

DTMAPI API candidate:

- `INpcInfoApi`
  - `ListNpcs`
  - `GetNpcLocation`
  - `GetNpcGiftPreferences`
  - optional debug `TeleportToNpc`

Validation targets:

- Unreachable/off-schedule NPCs show a clear reason.
- Dungeon teleport does not land in invalid rooms.
- Gift data localizes item names.

## 16. Teleport Menu / Official Transport / Mark Points

Observed implementation:

- `DLL confirmed`: `CustomTransitionMenu` patches `GameData.GameInitConfig.EnterTransitionMenu` and opens a custom `Teleporter` UI.
- `DLL confirmed`: `TeleportPointManager` loads/saves/deletes teleport points; `Teleporter.OpenTeleportUI`, `TeleportOrDelete`, and `DoTeleport` execute actions.
- `CE confirmed`: trainer can call `DolocAPI.DoTransport(markPointId)`, `DolocAPI.EnterRoom(roomId, position)`, and dungeon-room teleport.

Native path:

- `DolocAPI.DoTransport` reads mark point table and performs official transport-style room transition.
- `DolocAPI.EnterRoom` performs direct room transition, with dungeon handling and scene transition callbacks.
- Official station UI uses mark points, ticket/money rules, and transport callbacks.

Stable behavior to learn:

- Mark-point transport and direct room teleport are separate capabilities.
- Official-station-compatible teleports should prefer mark points.
- Debug/direct teleports should expose failure reasons for invalid room, dungeon, or position.

DTMAPI API candidate:

- `ITeleportDebugApi`
  - `ListMarkPoints`
  - `TransportToMarkPoint`
  - `EnterRoomAtPosition`
  - `ExportTeleportCsv`

Validation targets:

- Cross-region station/boat points are covered.
- Current room/position export can be curated by user.
- Teleport does not leave scene tips or stale UI behind.

## 17. Official Console Discovery / Command Execution

Observed implementation:

- `CE confirmed`: `GameData:GetAllConsoleCommands` reads `Console.ConsoleSystem.commandSystem.vm.callables` for command name, description, and parameter info.
- `CE confirmed`: `GameData:ExecuteConsoleCommand` calls `DolocAPI.ExecuteCommand`.
- `CE confirmed`: trainer can enable the in-game console by patching a method.

Native path:

- `DolocAPI.GetCommandFunction`, `GetAllCommandFunctions`, and `ExecuteCommand` expose official console command machinery.
- Example commands include item, monster, crop, weather, teleport, and other debug operations.

Stable behavior to learn:

- DTMAPI should expose command metadata safely, but raw command execution is dangerous.
- Y-console can avoid reimplementing known official actions by wrapping safe commands.

DTMAPI API candidate:

- `IOfficialConsoleApi`
  - `ListCommands`
  - `TryExecuteWhitelistedCommand`
  - `GetCommandHelp`

Validation targets:

- Dangerous commands are not exposed by default.
- Parameter names/descriptions are localized or clearly marked raw.
- Command execution logs exact command/result.

## 18. Weather / Monsters / Animals / Item Give

Observed implementation:

- `CE confirmed`: item give uses `DolocAPI.TryPlaceInBackpack` for positive quantities and `LinearInventory.MaxCost` for removal.
- `CE confirmed`: weather uses `ArchiveDataHandle.SetWeather` and `PatchWeather`.
- `CE confirmed`: monsters use `DolocAPI.Command_GenerateMonster` and `IMonsterHost.ClearMonsters`.
- `CE confirmed`: animals use `IAnimalHost.CreateAnimal` at the player position/cell.

Native path:

- `TryPlaceInBackpack` is the safe item-give path.
- Weather needs both archive data update and weather patch/refresh.
- Monster generation requires current room monster host.
- Animal creation requires current room animal host and valid animal proto.

Stable behavior to learn:

- Debug spawn/give operations should use official native entry points where possible.
- UI should show why an action is unavailable in the current room.

DTMAPI API candidate:

- Existing `IInventoryDebugApi`, `IWeatherDebugApi`, and advanced Y-console APIs cover much of this.
- Future APIs can add `IAnimalDebugApi` and safer `IMonsterSpawnDebugApi`.

Validation targets:

- Give item uses backpack placement and respects full inventory.
- Monster spawn fails safely outside monster rooms.
- Weather UI shows current/target weather and date segment.

## 19. Backpack Capacity / Infinite Items / Player Stat Cheats

Observed implementation:

- `CE confirmed`: `SetBackpackCapacity` calls `InventorySystem.SetBackpackCapacity`.
- `CE confirmed`: `InfiniteItems` iterates backpack items and writes count to each item proto overlay value.
- `CE confirmed`: trainer lists or modifies player stat values such as stamina, health, and physical power.
- `CE confirmed`: god-mode related entries include `OneShotMonster` and `Indestructible`.

Native path:

- Player inventory is under archive farm data inventory system.
- Item stack limit comes from proto overlay.
- Combat damage and hit processing are separate from item inventory.

Stable behavior to learn:

- Backpack capacity is potentially persistent and save-affecting, so it needs a stronger safety model than one-shot debug item give.
- Infinite items is a destructive direct-edit cheat and should not be normal mod API.
- Health/damage cheats belong to debug-only APIs.

DTMAPI API candidate:

- Keep as debug-only:
  - `IPlayerDebugApi`
  - `IInventoryDebugApi.FillStacks`
  - no stable public normal-mod API for arbitrary save mutation.

Validation targets:

- Capacity changes survive/revert intentionally.
- Inventory edits are logged.
- No item duplication from stack overflows.

## 20. Instant Farming / Crop Maturity

Observed implementation:

- `CE confirmed`: `InstantFarming` patches crop grow/check methods and writes crop current growth/current level to mature-level values.
- `CE confirmed`: it reads `Crop.seedProto.MatureLevel`, `Crop.seedProto.GrooveDepth`, `Crop.data.currentGrowthValue`, and `Crop.data.currentLevel`.

Native path:

- Crop growth data stores level and current growth amount.
- Mature level/depth is defined on seed proto.
- Native crop update/weather decorators continue to process crop after the state is changed.

Stable behavior to learn:

- Instant maturity is debug action, not a normal farming mod primitive.
- Safe implementation should call or emulate the minimum official state change and then refresh visuals.

DTMAPI API candidate:

- `IAdvancedDebugApi.MatureAllCrops(scope)`
- Future `IFarmingDebugApi.SetCropGrowth`

Validation targets:

- Mature crops are harvestable.
- Visual state refreshes immediately.
- Protected/dead/invalid crops are not corrupted.

## 21. Full-Scene / Panorama Camera

Observed implementation:

- `DLL confirmed`: `CameraRescaler.EnterPanoramaView`, `ExitPanoramaView`, `FitCameraToRoom`, `ResetCamera`, `AdjustBackground`, `RestoreBackground`, and `ApplyBackgroundCompensation`.
- `Game confirmed`: `CameraController` owns camera size, target resolution, room range, and camera position.
- `Game confirmed`: `DolocAPI.envBackgroundEx` is a background renderer used by the game environment.

Native path:

- Camera orthographic size alone expands visible gameplay geometry.
- Background rendering and depth fog/parallax need separate compensation; otherwise the background becomes a small centered rectangle.
- Room range and camera bounds control whether nearby room/large farm geometry is visible.

Stable behavior to learn:

- DTMAPI Zoom must not be just "camera size".
- It needs background scaling and depth-fog compensation APIs.
- Full panorama/photo mode is a separate API from player zoom.

DTMAPI API candidate:

- `ICameraApi`
  - `SetZoomScale`
  - `SetBackgroundCompensation`
  - `SetDepthFogCompensation`
  - `FitCurrentRoom`
  - `EnterPanoramaMode`

Validation targets:

- At 4x zoom, farm background is not a small box.
- Indoor rooms do not expose broken black/gray gaps more than native.
- Restoring zoom returns camera/background/fog to native values.

## 22. Strong Door / Equipment Unlocks

Observed implementation:

- `CE confirmed`: `UnlockAllDoors` patches a method indexed by the trainer table.
- `CE confirmed`: `UnlockAllEquips` redirects an equipment-use restriction method to a permissive branch.
- `Inferred`: based on feature names and surrounding trainer entries, these bypass world gate and equipment-use checks.

Native path:

- Exact method names require additional mapping from the trainer's method index table.
- Likely targets include door/gate interactability and equipment unlock/state checks.

Stable behavior to learn:

- Unlock bypasses are debug cheats, not stable normal mod APIs.
- If DTMAPI exposes them, they should be whitelisted debug actions with visible logs and restore policy.

DTMAPI API candidate:

- `IWorldDebugApi`
  - `SetDoorBypass`
  - `SetEquipmentUnlockBypass`

Validation targets:

- Toggle off restores native locks.
- It does not permanently unlock save data unless explicitly requested.

## 23. Dialogue / UI Data Injection

Observed implementation:

- `DLL confirmed`: `DialogueNodeReader` reads Yarn program nodes, instruction operands, line provider, and localization.
- `DLL confirmed`: `InputNameUiDataInjector` patches `InputNameBox.Render`, `InputNameUiState.Register`, `InputNameBox.OnConfirm`, and `InputNameUiState.Unregister`.
- `DLL confirmed`: `SeedUnlockUiDataInjector` patches `SeedUnLockUiState.DataGetter`, capacity, static texts, register/click/unregister.
- `DLL confirmed`: `QuestionBoxPatch` patches `QuestionUiState.OnUiUpdate` to handle cancel/restore.

Native path:

- Yarn dialogue data is available through the dialogue manager and project localization.
- Name input and seed unlock UI states can be repurposed as quick trainer UI surfaces.
- Question UI cancel can trap input if not restored correctly.

Stable behavior to learn:

- DTMAPI should prefer its own UI system for mods.
- Reading dialogue/localization data can become a useful diagnostic/content API.
- Borrowing native UI states is risky unless input restoration is explicit.

DTMAPI API candidate:

- `IDialogueQueryApi`
  - list nodes, localized lines, operands, referenced commands.
- `IUiInputIsolationApi`
  - scoped capture and guaranteed restore.

Validation targets:

- Custom UI close/cancel always restores gameplay input.
- Dialogue dumps do not write into runtime game directories unexpectedly.

## 24. Instant / Infinite Power

Observed implementation:

- `CE confirmed`: `InfinitePower` patches a power/electricity method.
- `CE confirmed`: trainer also has creative/no-cost/no-time features that can affect equipment operation.

Native path:

- The exact target method needs method-index mapping.
- Related game paths include equipment electronic components, generator output, battery storage, and equipment power consumption.

Stable behavior to learn:

- Power cheats are high-risk because they can mask whether machines consume electricity correctly.
- A creative generator item is safer than globally patching electricity rules for normal play.

DTMAPI API candidate:

- `IPowerDebugApi`
  - `SetInfinitePower`
  - `CreateDebugGenerator`
  - `GetPowerNetworkStatus`

Validation targets:

- Normal machines still consume power when debug off.
- Creative generator can be removed without corrupting the network.

## 25. Feature-to-DTMAPI Absorption Priority

High priority, because DTMAPI already has similar features and user-facing regressions:

- Fishing automation state API.
- Camera/background/fog zoom API.
- Inventory scope/chest locator API.
- Farming gun / batch planting API.
- Time scale and native simulation API.
- Official console metadata API.

Medium priority, good ecosystem APIs:

- Fish pool analysis API.
- NPC location/gift API.
- Animal automation/status API.
- Fish tank/status API.
- Room status indicator API.
- Cargo drone/store API.

Debug-only, avoid stable normal-mod exposure:

- Infinite jump/dash/motor endurance.
- No-cost/no-time crafting.
- Infinite items/backpack capacity mutation.
- Door/equipment bypass.
- One-shot monster / invulnerability.
- Raw official console execution.

## 26. Cross-Feature Stability Lessons

Common pattern in stable-looking DolocPlus features:

- It usually patches the native method that already owns the real rule.
- It often calls native methods for final effects, such as `TryPlaceInBackpack`, `PlantBasin.Harvest`, `FarmFishTank.CollectProductItems`, `Animal.ProduceAsItems`, `DolocAPI.DoTransport`, or `ArchiveDataHandle.SetWeather`.
- It separates conflict-prone features, especially fishing and time speed.
- It restores method patches on disable/close.

Common risks if DTMAPI implements only surface behavior:

- UI appears correct but gameplay state does not change.
- Smoke tests pass but user hand tests fail in a different room/save state.
- Rendered and offscreen update paths diverge.
- Debug cheats accidentally become persistent save mutations.
- Input leaks from overlays into gameplay.

## 27. Acceptance Checklist for Future Goals

When a future DTMAPI goal uses this document, it should require:

- One API family per goal unless the features share the same native call path.
- GameBridge hook map entry for each fragile native method.
- Public API matrix entry with stable/experimental status.
- Manual QA or third-save smoke that proves the real native result, not only UI state.
- Explicit conflict rules for time-speed, fishing, creative, and UI overlays.
- Exit/save-unload cleanup evidence.
- Rollback notes for any save-affecting debug operation.
