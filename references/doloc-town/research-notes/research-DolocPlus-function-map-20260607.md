# DolocPlus / 小神增强包函数级重叠解析

Date: 2026-06-07

Scope: read-only function-level compatibility research. This is a function/patch map for the overlapping DolocPlus features. It is not source code to copy and not implementation evidence.

Deep-dive companion: `research-DolocPlus-deep-dive-20260607.md`.

Read method:

- Confirmed patch targets are from Harmony attributes in `DolocPlus.dll`.
- Function signatures and called game methods are from metadata/IL call references.
- CE/Lua entries are from `DolocTownEA_Lua_1.8.1.CT` function names and explicit `FindMonoMethod` / call strings.
- Where behavior is inferred from names/calls rather than full source, it is labeled as inferred.

## 0. Runtime / Feature Framework

DolocPlus entry and feature controllers:

- `DolocPlus.DolocPlusMod.Awake()`: plugin startup and patch/controller initialization entry.
- `DolocPlus.DolocPlusMod.Update()`: per-frame feature update dispatcher.
- `DolocPlus.DolocPlusMod.OnApplicationQuit()`: shutdown cleanup.
- `DolocPlus.Features.PatchController.Initialize()`, `OnInitialize()`, `OnFeatureToggled()`, `Dispose()`: shared Harmony patch lifecycle wrapper.
- `DolocPlus.Features.HotkeyController.Initialize()`, `Update()`: shared hotkey lifecycle wrapper.
- `DolocPlus.Features.FeatureUtils.DisableModUserInputs()` / `EnableModUserInputs()`: blocks normal mod/user inputs while custom UI is open.
- `DolocPlus.Features.FeatureUtils.CheckConflicts()` / `InitializeConflictMap()`: feature conflict bookkeeping.

DTMAPI implication:

- DTMAPI already separates bootstrap/Core/GameBridge/mod APIs. The useful lesson is not the framework shape, but the conflict/input lifecycle discipline: every UI/debug/automation API should declare input isolation, restore behavior, and conflict policy.

## 1. AutoFishing / Fishing AFK

Patch class: `DolocPlus.Patches.FishingAFK`

Confirmed Harmony targets:

- `Prefix_FishReadyOnExit(DolocTown.AgentStateFishingReady __instance)`
  - Target: `DolocTown.AgentStateFishingReady.OnExit`
  - Role: fishing-ready state cleanup boundary.
- `Postfix_FishCastNextState(AgentStateBase& __result)`
  - Target: `DolocTown.AgentStateFishingCast.NextState`
  - Role: observes cast-state transition and can stop/continue automation.
- `Prefix_FishWaitNextState(DolocTown.AgentStateFishingWait __instance)`
  - Target: `DolocTown.AgentStateFishingWait.NextState`
  - Role: before wait-state transition; touches private `waitForFishBite` via FieldRef.
- `Postfix_FishWaitNextState()`
  - Target: same as above.
- `Prefix_UpdateGame(DolocTown.FishingGameScrollBar __instance)`
  - Target: `DolocTown.FishingGameScrollBar.UpdateGame`
  - Role: minigame automation; FieldRefs include `_startTime`, `_currentNote`.
- `Postfix_UpdateGame()`
  - Target: same minigame update.
- `Prefix_NormalFishingKeyPress(bool& __result)`
  - Target: `DolocTown.DolocUserInput.get_NormalFishing`
  - Role: supplies synthetic fishing input.
- `Prefix_UseFishingRod(DolocTown.ItemFishingRod __instance)`
  - Target: `DolocTown.ItemFishingRod.OnUseAsTool`
  - Role: captures rod/use entry.

Other functions:

- `Stop(bool showMessage)`: resets `_isPressed`, `_isEnabled`, `_rod`, `_isSucceeded`; calls `DolocAPI.ShowMessageBoxNodeComplete`.
- `UpdatePerSec()`: checks `DolocAPI.agent.StateManager.CheckState<AgentStateIdle>()`; calls `BodyController.UseFishRod(_rod)` for re-cast.
- `get_IsEnabled()`: read-only current automation state.

DTMAPI overlap:

- Current DTMAPI surface: `IFishingAutomationApi` + `AutoFishingMod`.
- DTMAPI already touches the same conceptual chain: rod use, cast/wait states, scroll-bar/minigame, synthetic input, F6 enable/cancel.

DTMAPI risk lessons:

- AutoFishing should be verified across all state boundaries, not only "F6 toggles".
- Movement cancel and time-speed conflict should be explicit, because DolocPlus keeps fishing state in private static fields and uses state transitions to recover.
- DTMAPI should keep synthetic input confined to fishing APIs, never generic `DolocUserInput` spoofing outside active automation.

## 2. Fish Pool Analysis

Function class: `DolocPlus.Functions.FishAnalyzer`

Functions:

- `AnalyzeFishesInPreviousPool(float& expectedGain)`
  - Inferred role: collects candidate fish for the previously touched/current fishing pool and builds UI rows.
- `RunValidations(Func<string>[] validators)`
  - Runs reason strings for unavailable fish.
- `CheckFishRod(int fishRodLv, FishProto fishProto)`
  - Checks rod-level requirements.
- `CheckFishingLv(int fishingLv, FishProto fishProto)`
  - Checks player fishing skill requirement.
- `CheckWeather(WeatherType weather, FishProto fishProto)`
  - Reads fish weather constraints and current weather.
- `CheckMonth(DateInfo dateInfo, FishProto fishProto)`
  - Reads `FishProto.Month`.
- `CheckTime(DateInfo dateInfo, FishProto fishProto)`
  - Reads `FishProto.TimeRange` and `TimeRange.InRange`.
- `CheckUnlocked(FishProto fishProto)`
  - Calls `DolocAPI.CheckFishUnlocked`; also queries Yarn operands by keyword and `DolocAPI.QueryItemProto` for explanation data.

DTMAPI overlap:

- No direct DTMAPI API yet. Existing `FishBreedingAssistantMod` is fish roe tooltip, not pool analysis.

Future DTMAPI candidate:

- `IFishPoolAnalysisApi`: fish list, probability/weight, lock reason, rod/level/weather/month/time/unlock conditions, expected value.

## 3. Chest Locator / All-Chest Material Lookup

Patch classes:

- `DolocPlus.Patches.UseAllChests`
- `DolocPlus.Patches.UseAllChestsLocated`

Confirmed Harmony targets:

- `UseAllChests.Prefix_GetAvailableInventories(Vector2Int& anchor, Vector2Int& area)`
  - Target: `DolocTown.GameData.ArchiveDataHandle.GetAvailableInventories`
  - Calls: `DolocAPI.CurrentRoom`, `Room.RoomGridSize`.
  - Inferred role: widens material lookup area to entire current room.
- `UseAllChestsLocated.Prefix_GetInventoriesAroundEquipment()`
  - Target: `DolocAPI.GetInventoriesAroundEquipment`
  - Role: marks call path as equipment-driven.
- `UseAllChestsLocated.Transpiler_GetAvailableInventories(IEnumerable<CodeInstruction>)`
  - Target: `ArchiveDataHandle.GetAvailableInventories`
  - Role: swaps/extends equipment enumeration while in equipment call path.
- `UseAllChestsLocated.AllEquipmentsPatch(IEquipmentHost host)`
  - Calls: `IEquipmentHost.DM_equipment`, `EquipmentManager.AllEquipments`.
  - Inferred role: returns all equipment for host rather than native near-range equipment.

DTMAPI overlap:

- Current DTMAPI surface: `IChestLocatorEnhancerApi` + `ChestLocatorEnhancerMod`.
- DTMAPI currently appends shared inventories and verifies native `CountItem` / `CostItem`.

DTMAPI risk lessons:

- Keep the two modes distinct:
  - Current-room all chests.
  - Locator-marked/global equipment material lookup.
- Acceptance must cover `CountItem`, `TryCostItem`, `MaxCostItem`, and workbench/equipment recipes, because DolocPlus touches inventory discovery at a low level.

## 4. Strong Planting Gun / One-Click Planting

Patch class: `DolocPlus.Patches.OneClickPlanting`

Confirmed Harmony target:

- `Prefix_PlantSeed(ItemSeed __instance)`
  - Target: `DolocTown.ItemSeed.PlantSeed`
  - Returns `bool`, so it can skip/replace native logic.
  - Event helpers: `add_OnSeedPlanted`, `remove_OnSeedPlanted`.

Function class: `DolocPlus.Functions.PlantAutomator`

Functions:

- `AutomaticPlanting(ItemSeed seed, bool useFilm, bool useFertilizer)`
  - Calls: `DolocAPI.archiveHandle.currentRoom`, `DolocAPI.QuerySeedProto`, `ArchiveDataHandle.GetAvailableInventories`, `IEquipmentHost.GetEquipments<PlantBasin>()`, `PlantBasin.Plant`, `PlantBasin.Water`, `PlantBasin.Protect`, `DolocAPI.TryCostItem`, `DolocAPI.QueryItemProto`.
  - Role: whole-room basin automation triggered by seed planting.
- `TryCostItem(LinearInventory[] inventories, Item item, int count, bool shouldEqualAsItem)`
  - Calls: `DolocAPI.CountItem`, `DolocAPI.MaxCostItem`.
- `SubscribeToSeedPlant(Action<ItemSeed>)` / `UnsubscribeToSeedPlant(...)`
  - Hooks callbacks to the seed-plant event.

CE/Lua overlap:

- `GameData:PlantAutomation(cropNameAddr)` calls `DolocAPI:Command_PlantCrop`.
- `Trainer:PlantAutomation(status)`, `AutoUseMulch(status)`, `AutoUseFertilizer(status)` patch native planting flow and use `ArchiveDataHandle:GetAvailableInventories`, `DolocAPI:TryCostItem`, `DolocAPI:CountItem`, `DolocAPI:MaxCostItem`.

DTMAPI overlap:

- Current DTMAPI surface: `IStrongPlantingGunApi` + `StrongPlantingGunMod`.
- DTMAPI strengthens official farming gun slots/range; DolocPlus automates current-room planting after seed use.

DTMAPI risk lessons:

- Do not merge room automation into StrongPlantingGun by accident.
- If adding whole-room automation later, create separate `IFarmingAutomationApi`.
- Must preserve official basin legality: empty basin, seed type, film state, fertilizer state, material consumption order.

## 5. Crop / Resin / Animal / Fish Tank Automation

### 5.1 Automatic Crop Harvest

Patch class: `DolocPlus.Patches.AutomaticHarvest`

Confirmed Harmony target:

- `Prefix_CheckGrowthMonth(Crop __instance)`
  - Target: `DolocTown.Crop.CheckGrowthMonth`

Other function:

- `TryHarvest(Crop instance)`
  - Calls: `Crop.plantBasin`, `Equipment.Host.CurrentRoom`, `Room.IsInHouse`, `TemplateRoomInHouse.Building.proto.IsAnimalBuilding`, `Crop.isMature`, `PlantBasin.Harvest`.
  - Inferred role: auto-harvest mature crops, with husbandry-building mode gating.

DTMAPI overlap:

- Y Console advanced API can mature crops; StrongPlantingGun handles planting. There is no auto-harvest API yet.

### 5.2 Automatic Resin Collection

Patch class: `DolocPlus.Patches.AutomaticResinCollecting`

Confirmed Harmony target:

- `Prefix_UpdateNoRender(ResinCollector __instance)`
  - Target: `DolocTown.ResinCollector.UpdateNoRender`
  - FieldRefs: `_currentValue`, `_func`; calls reflected collect/update methods.

DTMAPI overlap:

- ActionSpeed accelerates some collection/interactions, but does not auto-collect resin.

### 5.3 Automatic Animal Collecting / Petting

Patch class: `DolocPlus.Patches.AutomaticAnimalCollecting`

Confirmed Harmony targets:

- `Prefix_IsHoneyCombFull(HoneyComb __instance)`
  - Target: `HoneyComb.get_IsHoneyCombFull`
- `Postfix_ProduceHoney(HoneyComb __instance)`
  - Target: `HoneyComb.ProduceHoney`
- `Prefix_IsChickenNestFull(ChickenNest __instance)`
  - Target: `ChickenNest.get_IsFull`
- `Postfix_Produce(ChickenNest __instance)`
  - Target: `ChickenNest.Produce`
- `Prefix_Update(Animal __instance)`
  - Target: `Animal.Update`
- `Prefix_UpdateNoRender(Animal __instance)`
  - Target: `Animal.UpdateNoRender`

Other functions:

- `TryCollectComb(HoneyComb honeyComb)`
  - FieldRef products list; calls `EquipmentPatch.CreateDropItem`.
- `TryCollectNest(ChickenNest chickenNest)`
  - FieldRef nest items; calls `EquipmentPatch.CreateDropItem`.
- `TryProduce(Animal animal)`
  - Checks `Animal.proto.ManualMetabolism`, `Animal.NeedMetabolism`, uses `GetInventoriesAroundAnimal`, `DolocAPI.TryCostItem`, `Animal.ProduceAsItems`, `DolocAPI.GenerateDropItems`.
- `GetInventoriesAroundAnimal(Animal animal)`
  - Enumerates equipment in the animal's current room; collects `Case` and `StorageShelf`, respecting `DolocAPI.userSettings.autoUseBox`.

Patch class: `DolocPlus.Patches.AutomaticAnimalFondle`

- `Prefix_Move(Animal __instance)`
  - Target: `DolocTown.Animal.Move`
  - Inferred role: auto-pet/fondle through animal movement/update flow.

DTMAPI overlap:

- Current DTMAPI: `IAnimalViewerApi` for hidden produce progress; `ICustomAnimalApi` stable contract with native creation blocked.
- No auto pet/collect API yet.

Future DTMAPI candidate:

- `IAnimalAutomationApi`: produce collection, metabolism feed cost, pet/fondle state, box lookup policy.

### 5.4 Automatic Fish Tank / Instant Fish Farming

Patch classes:

- `DolocPlus.Patches.AutomaticTankCollecting`
- `DolocPlus.Patches.InstantFishFarming`

Confirmed Harmony targets:

- `AutomaticTankCollecting.Prefix_Update(FishTank __instance)` -> `FishTank.Update`
- `AutomaticTankCollecting.Prefix_UpdateNoRender(FishTank __instance)` -> `FishTank.UpdateNoRender`
- `Prefix_EcoUpdate(FishTankEcological __instance)` -> `FishTankEcological.Update`
- `Prefix_EcoUpdateNoRender(...)` -> `FishTankEcological.UpdateNoRender`
- `Prefix_EelUpdate(FishTankElectricEel __instance)` -> `FishTankElectricEel.Update`
- `Prefix_EelUpdateNoRender(...)` -> `FishTankElectricEel.UpdateNoRender`
- `InstantFishFarming.Prefix_GrowFries(FarmFishTank __instance)` -> `FarmFishTank.GrowFries`
- `Prefix_FishFryGrow(ItemFishFry __instance)` -> `ItemFishFry.Grow`
- `Prefix_FishRoeIncubate(ItemFishRoe __instance)` -> `ItemFishRoe.Incubate`

Other function:

- `DoCollect(IFishTank fishTank)`
  - Calls: `IFishTank.tank`, `FarmFishTank.HasProduct`, reflected product getter, `EquipmentPatch.CreateDropItem`, releases product effect slots.

DTMAPI overlap:

- FishBreedingAssistant only shows roe info. There is no fish-tank automation/farming API.

Future DTMAPI candidate:

- `IFishTankAutomationApi` or `IFishFarmApi`.

## 6. Time, Movement, Jump/Dash, Motor Endurance

### 6.1 Time Speed

Patch class: `DolocPlus.Patches.TimeSpeed`

Confirmed Harmony target:

- `Prefix_FixedUpdate(GameLoop __instance)`
  - Target: `GameLoop.FixedUpdate`
  - FieldRef: `GameLoop` second timer (`RedSaw.RSTimer`).
  - Uses static `_multiplier` and `_isApplied`.

Controller:

- `TimeSpeedController.OnTimeSpeedChanged()`
- `TimeSpeedController.DelayedUnpatch()`

DTMAPI overlap:

- `IAdvancedDebugApi` time scale and `ITimeDebugApi` time jumps.

Risk lesson:

- Time speed is a low-level game loop manipulation. DTMAPI should keep restoration, conflict handling, and production/crop evidence mandatory.

### 6.2 Infinite Dash / Jump

Patch classes:

- `InfiniteDash.Prefix_SupportDash(bool& __result)` -> `AgentStateDash.get_SupportDash`
- `InfiniteDash.Prefix_Dash(BodyController __instance)` -> `BodyController.Dash`
- `InfiniteJump.Prefix_DoubleJumpTimes(int& __result)` -> `ArchiveOperationGlobal.DoubleJumpTimes`

DTMAPI overlap:

- Only partial overlap with `IMovementDebugApi` movement speed. Dash/jump capability is not currently modeled.

Future candidate:

- `IMovementAbilityDebugApi` for dash/jump/endurance overrides, separate from normal speed multiplier.

### 6.3 Tireless Motor

Patch class: `DolocPlus.Patches.TirelessMotor`

Confirmed Harmony target:

- `Prefix_CostEndurance(float& dt)`
  - Target: `DolocTown.MotorController.CostEndurance`
  - Role: reduces or cancels motor endurance drain.

DTMAPI overlap:

- `IMotorVehicleApi` handles original/second motor state, summon/ride/speed. It does not expose infinite endurance.

Risk lesson:

- Motor tuning should separate speed, energy/endurance, summon, room transition, and visual identity.

## 7. Crafting / Store / Cargo Drone

### 7.1 Max Craft Count

Patch class: `DolocPlus.Patches.MaxCraftCount`

Confirmed Harmony targets:

- `Prefix_ConversionRecipeMaxCraftCount(int& __result)`
  - Target: `ConversionRecipeUiState.get_maxCraftCount`
- `Prefix_SynthesizerMaxCraftCount2(Synthesizer __instance, IRecipe& recipe, int& __result)`
  - Target: `Synthesizer.GetMaxCraftCount`
  - Calls: `DolocAPI.GetInventoriesAroundEquipment`, `IRecipe.MaxAffordScale`.
- `Prefix_SynthesizerMaxCraftCount1(...)`
  - Same concept for overload.

DTMAPI overlap:

- DTMAPI has creative/no-time/no-cost debug surfaces but no batch-count API.

Future candidate:

- `IRecipeBatchApi` or `ICraftingDebugApi`.

### 7.2 Parking Apron Shopping / Cargo Drone

Patch class: `DolocPlus.Patches.ParkingApronShopping`

Confirmed Harmony targets:

- `SpawnStoreItems_Prefix(Store __instance, Dictionary<string,int>& __result)`
  - Target: `Store.SpawnStoreItems`
  - Reads `StoreProto.Id`, item tables, available item/type filters.
- `ItemCategoryGetter_Prefix(Store __instance)`
  - Target: `Store.get_ItemCategory`
- `GetItemUnitBuyingPrice_Postfix(Store __instance, int& __result)`
  - Target: `Store.GetItemUnitBuyingPrice`
- `BuyItemInternal_Prefix(StoreUiState __instance, StoreItemRef& cache, int& count, int& storeIndex)`
  - Target: `StoreUiState.BuyItemInternal`
  - Calls `Store.BuyItem`, `ArchiveDataHandle.CurrentMoney`, `StoreWidget.RaiseSpriteFadeUp`.
- `Show_Postfix(StoreUiState __instance)` / `Hide_Postfix(...)`
  - Store UI lifecycle.
- `SpawnMoneyData_Prefix(Equipment instance)`
  - Target: `EquipmentPatch.CreateDropItemMoney`
- `OnTouch_Postfix(ParkingApron __instance)` / `OnDisTouch_Postfix(...)`
  - Parking apron interaction.

Other function:

- `LaunchDelivery(ParkingApron parkingApron)`
  - FieldRefs: `couldLaunch`, `isTakeOff`, `pendingPayment`, `waitCounter`, equipment func.
  - Calls: parking apron launch animation, `EquipmentFuncParkingApron.GetGoodsLvDuration`, `DolocAPI.Broadcast(GameEventType)`.

DTMAPI overlap:

- No direct API. Inventory debug give is not the same thing.

Future candidate:

- `IStoreApi` / `ICargoDroneApi`.

## 8. NPC Info / Teleport / UI Injection

### 8.1 NPC Info

Function class: `DolocPlus.Functions.NPCInfoReader`

Functions:

- `GetNPCsInfo()`
  - Calls `DolocAPI.IsDataLoaded`, `archiveHandle.cityData.npcManager.AllNpcs`, `Npc.NpcName`, `Npc.GetCurrentTitle`, `Npc.TryGetCurrentRoom`, room/scene table lookup.
- `GetLocation(Npc npc, Room& npcRoom, Vector2& npcCoordinate)`
  - Calls `Npc.TryGetCurrentRoom`, `Npc.positionWS`.
- `GetLikingItemsSortedByNPCs()`
  - Reads `DolocConfig.Tables.TbNpcLikingProtos.DataList` and `NpcLikingProto.ItemMap`.
- `GetFavoritesBy(string npc, int levelCeiling)`
  - Uses cached liking map.

Controller:

- `NPCsInfoUiController.OnInitialize()`, `Update()`, `OnSlotClick()`, `PagerCount`.

DTMAPI overlap:

- Current Y console teleport does not expose NPC status or liking.

Future candidate:

- `INpcInfoApi`.

### 8.2 Teleport

Function classes:

- `TeleportPointManager.Load()`, `Save()`, `Delete()`, `EnsureDir()`, `WriteAll()`
- `Teleporter.OpenTeleportUI()`, `TeleportOrDelete(string alias)`, `DoTeleport(FastTravelInfo)`, `DoDelete(FastTravelInfo)`

Observed calls:

- `OpenTeleportUI` calls `DolocAPI.ShowSmallTextMenu`.
- `DoTeleport` calls `DolocAPI.EnterRoom(roomId, Vector2, Action)`.
- `TeleportOrDelete` patches input/UI temporarily and calls `FeatureUtils.DisableModUserInputs`.

CE/Lua overlap:

- `GameData:TeleportMarkPt(markPtAddr)` uses `DolocAPI:DoTransport`.
- `GameData:TeleportRoom(roomID, position)` uses `DolocAPI:EnterRoom`.
- `GameData:TeleportDungeon(sceneAddr, dungeon, position)` also enters room/scene.
- `GameData:GetAllMarkPtIDs()` enumerates native map/mark point IDs.

DTMAPI overlap:

- `ITeleportDebugApi` uses whitelisted native mark/station destinations and CSV export.

Risk lesson:

- DTMAPI should keep both transport routes separate:
  - Official transport/mark point route (`DoTransport`) for stations/ships.
  - Direct room-position route (`EnterRoom`) for debug/fallback destinations.

### 8.3 Input / Seed UI Data Injection

Patch classes:

- `InputNameUiDataInjector`: patches `InputNameBox.Render`, `InputNameUiState.Register`, `InputNameBox.OnConfirm`, `InputNameUiState.Unregister`.
- `SeedUnlockUiDataInjector`: patches `SeedUnLockUiState.DataGetter`, `get_totalCapacity`, `StaticTexts.SeedPanelTitle`, `StaticTexts.UiTipUnlockSeed`, `Register`, `OnDataClick`, `Unregister`.

DTMAPI implication:

- These are UI repurposing helpers. DTMAPI should avoid overloading unrelated native panels unless the API clearly marks it as debug/internal.

## 9. Panorama / Background / Camera

Function class: `DolocPlus.Functions.CameraRescaler`

Functions:

- `EnterPanoramaView()`
  - Checks `DolocAPI.IsDataLoaded`, `envBackgroundEx`, `EnvCovariantController`.
  - Hides agent, drone renderer, motor.
  - Clears scene operation tips, selected building touch, building scanner buffer.
  - Calls `FitCameraToRoom()` and `AdjustBackground()`.
- `FitCameraToRoom()`
  - Reads `DolocAPI.worldResolution`, `CurrentRoom.SceneSize`, `DolocAPI.mainCamera.orthographicSize`.
  - Writes camera orthographic size.
  - Writes private `CameraController.camSize` via FieldRef.
  - Uses camera controller room range/position logic.
- `AdjustBackground()`
  - Reads `EnvCovariantController._depthFogController`.
  - Backs up background and depth-fog scale.
  - Calls `ApplyBackgroundCompensation()`.
- `ApplyBackgroundCompensation()`
  - Ratio = current orthographic size / original orthographic size.
  - Multiplies background and depth-fog scale by ratio.
- `ExitPanoramaView()`
  - Restores agent/drone/motor visibility based on save/current room.
  - Calls `ResetCamera()`, `RestoreBackground()`, `DolocAPI.RefreshScanner()`.
- `ResetCamera()`
  - Restores camera orthographic size, calls `CameraController.RefreshResolution()`, recenters on `DolocAPI.AgentPosition`.
- `RestoreBackground()`
  - Restores backed-up background/depth-fog scales.

Controllers:

- `PanoramaViewController.Update()`: hotkey toggles full photo/panorama mode.
- `BackGroundViewController.Update()`: toggles background-only rendering by calling room render/clear paths.

DTMAPI overlap:

- `ICameraZoomApi` + `ZoomMod` covers view scale.

DTMAPI lesson:

- Zoom needs only `ApplyBackgroundCompensation`-style background/fog scaling.
- Full panorama needs separate API because it hides entities, changes room range, and interacts with photo state.

## 10. Official Console / CE Debug Entrypoints

CE/Lua file: `DolocTownEA_Lua_1.8.1.CT`

Important functions:

- `GameData:GetAllConsoleCommands()`
  - Reads `Console.ConsoleSystem.commandSystem.vm.callables`.
  - Extracts command name, description, and parameter info.
- `GameData:ExecuteConsoleCommand(command)`
  - Calls `DolocAPI:ExecuteCommand`.
- `Trainer:EnableGameConsole(status)`
  - Enables in-game F1 console path through a method patch.
- `Trainer:CommandsListView()`
  - Presents command/parameter list using `GetAllConsoleCommands`.
- `GameData:SetWeather(weatherType)`
  - Calls `ArchiveDataHandle:SetWeather` and weather patching.
- `GameData:ModifyMonsters(tagAddr, num)`
  - Calls `DolocAPI:Command_GenerateMonster` or `IMonsterHost:ClearMonsters`.
- `GameData:AddGameItem(itemTagAddr, itemNum)`
  - Calls `DolocAPI:TryPlaceInBackpack`.
- `GameData:PlantAutomation(cropNameAddr)`
  - Calls `DolocAPI:Command_PlantCrop`.
- `GameData:CreateAnimal(animalProto)`
  - Creates native animal through game data paths.
- `GameData:GetAllMarkPtIDs()`, `TeleportMarkPt`, `TeleportRoom`, `TeleportDungeon`
  - Mix official mark transport with direct room/dungeon entry.
- `Trainer:NoCostCrafting`, `InfinitePower`, `InstantCrafting`, `InstantFarming`, `OneShotMonster`, `DisableUI`
  - Trainer-only, high-risk debug/cheat behavior.

DTMAPI overlap:

- `IAdvancedDebugApi`, `IInventoryDebugApi`, `IWeatherDebugApi`, `ITeleportDebugApi`, custom entity contracts.

DTMAPI lesson:

- Add official console command metadata as a safe read-only API candidate.
- Keep raw `ExecuteCommand`, raw Lua-like method patches, player stats, no-cost/no-time, monster/entity generation, and direct save editing behind explicit debug/experimental gates.

## 11. Function-Level Candidate API Summary

Based on concrete functions, the strongest future DTMAPI candidates are:

- `IOfficialConsoleCommandApi`
  - Inspired by: `GameData:GetAllConsoleCommands`, `GameData:ExecuteConsoleCommand`.
  - Should expose metadata safely; execution must be whitelist-based.
- `IFishPoolAnalysisApi`
  - Inspired by: `FishAnalyzer.AnalyzeFishesInPreviousPool`, `CheckWeather`, `CheckMonth`, `CheckTime`, `CheckUnlocked`.
- `IInventoryTransferApi`
  - Inspired by: `ItemStacker.StackInventoryToStorage`.
- `IFarmingAutomationApi`
  - Inspired by: `PlantAutomator.AutomaticPlanting`, `OneClickPlanting.Prefix_PlantSeed`.
- `IAnimalAutomationApi`
  - Inspired by: `AutomaticAnimalCollecting.TryProduce`, `GetInventoriesAroundAnimal`, `AutomaticAnimalFondle.Prefix_Move`.
- `IFishTankAutomationApi`
  - Inspired by: `AutomaticTankCollecting.DoCollect`, `InstantFishFarming` patches.
- `INpcInfoApi`
  - Inspired by: `NPCInfoReader.GetNPCsInfo`, `GetLocation`, `GetLikingItemsSortedByNPCs`.
- `IStoreApi` / `ICargoDroneApi`
  - Inspired by: `ParkingApronShopping` store and delivery functions.
- `ICraftingDebugApi` / `IRecipeBatchApi`
  - Inspired by: `MaxCraftCount` patches and CE crafting functions.
- `IPanoramaCameraApi`
  - Inspired by: `CameraRescaler.EnterPanoramaView`, `FitCameraToRoom`, `AdjustBackground`, `ApplyBackgroundCompensation`.

## 12. What This Means For Current DTMAPI

Current DTMAPI already overlaps the core useful areas:

- Y console and safe advanced debug wrappers.
- AutoFishing.
- Chest Locator Enhancer.
- Strong Planting Gun.
- Camera Zoom.
- Motor/vehicle API.
- Custom entity definition APIs.

The major missing overlap areas are not small bug fixes; they are new API families:

- Fish pool analysis.
- NPC info/teleport by NPC.
- Inventory transfer/stacking.
- Whole-room farming automation.
- Animal/fish-tank automation.
- Store/cargo drone APIs.
- Official console metadata.
- Full panorama/photo camera API.
