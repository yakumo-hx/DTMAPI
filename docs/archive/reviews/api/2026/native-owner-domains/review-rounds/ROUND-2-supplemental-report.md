# Round 2 Supplemental Report

Status: recorded
Date: 2026-06-13
Input: Round 1 challenge questions
Output: supplemental owner findings and report corrections

Round 2 responded to the Round 1 challenges without repeating the initial broad scan. It refined the native responsibility map and clarified which targets remain `Found`, `Partial`, `Experimental`, or `Blocked`.

## 01 World Time Weather Refresh

Supplemental findings:

- Time snapshot owners are `TimeArchiveData.dateNow/dateConfig` and `ArchiveDataHandle.DateNow/DayProcess/CurrentWeekDay/CurrentDayPeriodType`.
- Weather snapshot owners are `TimeArchiveData.weather`, `ArchiveDataHandle.GlobalWeatherInfo/CurrentWeatherInfo`, and room effective weather via `Room.GetWeatherInfo`.
- Current-weather force is `ArchiveDataHandle.SetWeather(type, shouldRender)` through `WeatherSystem.SetCurrentWeather` and `_OnWeatherChanged`/no-render variants.
- Forecast/history patch is `ArchiveDataHandle.PatchWeather`, `TimeArchiveData.weatherPatch`, `GetWeatherInfoOfDay`, and `QueryWeatherHistory`.
- Controlled time advance is `PassTime`/`PassLongTime`; internal no-render advance is `PassTimeNoControl`, `UpdateNoRender`, `UpdateDateNoRender`, and no-render callbacks.
- Refresh observation must include `_OnHourChanged`, `_OnDayChanged`, `_OnDailyRefresh`, `_OnWeeklyRefresh`, `_OnMonthlyRefresh`, `_OnYearlyRefresh`, `_OnWeatherChanged`, and `_OnSeasonChanged`.
- `PassTimeNoControl` order includes before-time-pass, per-TU updates, date no-render updates, after-time-pass, then callback. The after-time-pass path touches room, NPC, player light, drone, weather/day-night render, and scene-box UI.
- Dungeon/custom independent weather state exists, but the active caller for independent dungeon weather was not proven.

## 07 Maps Dungeons Scenes Resources

Supplemental findings:

- Mark-point teleport is `DolocAPI.DoTransport` plus `TbMarkPoint`/`MarkPointInfo`.
- Direct room entry is `DolocAPI.EnterRoom`; city/farm routes use `EnterCity` and `EnterFarm`.
- Dungeon routes are `EnterDungeon`, `_TryEnterDungeon`, `_TryQuitDungeon`, `TryEnterDungeonSubRoom`, and `Dungeon._ExitCurrentRoom`.
- Portal/gate interactions are separate owners: `PortalInfo`, `AnimatedGate`, `SimpleGate`, `ScannerGate`, `ManagerGate`, `BuildingGate`, and `BuildingLinkGate`.
- Resource refresh owners are `IDungeonResourceHost`, `DungeonResourceManager`, `ResourceSpawnInfo`, and `DolocAPI.RefreshResourceByType`.
- Vegetation refresh owners are `IVegetationHost`, `VegetationManager`, `VegetationSpawnInfo`, and `RefreshWorldVegetation`.
- Runtime map/dungeon creation and boundary mutation remain blocked. `DolocBundleManager.LoadRooms`/`LoadDungeons` are load-time/private database paths, not authoring APIs.

## 02 NPC Body Behavior

Supplemental findings:

- Identity/config starts from `TbNpc`/`NpcInfo`; `Npc(NpcInfo)` constructs an instance.
- `NpcManager.AddNpc` registers by `NpcName`; `NpcManager` also owns save/load persistence and runtime lookup.
- `TbNpcDocument` is document/UI metadata, not runtime creation proof.
- Schedule support depends on `NpcInfo`, loaded schedule assets, mark points, scenes, and valid NodeCanvas graph state.
- Dialogue/story paths depend on `DialogueManager`, Yarn node existence, and interaction state.
- New NPC verdict is constrained experimental at the native-research level, but stable public API remains blocked until early `TbNpc` injection, assets, schedule graph, dialogue nodes, document data, liking/store integration, and save/load timing are proven.
- Store opening is store-id/dialogue-task driven, not NPC-owned.

## 03 Animal Husbandry Behavior

Supplemental findings:

- Existing animal creation/release path includes `ItemAnimalPackage.TryReleaseAnimal`, current `Room` as `IAnimalHost`, `AnimalManager.CreateAnimal`, and `IAnimalHost.CreateAnimal/AddAnimal`.
- Custom `AnimalInfo` species are plausible only as an experimental preload/content path. Stable API remains blocked until proto/assets/save/render/AI timing are proven.
- Room persistence uses room `DM_animal`, runtime `AnimalSystem`, home room, and current room state.
- Product ownership is split: animal state/eligibility through `AnimalInfo.ProduceSpawnEntry`, and collection through tool/machine owners such as milking, lint, and honeycomb interfaces.

## 04 Wild Birds Events Drops

Supplemental findings:

- Bird spawning is an env-object route: `RoomSpawnInfo.EnvObjectSpawnEntry`, `IEnvObjectHost.RefreshEnvObjects`, `TbEnvObject`, and `EnvObjectManager.CreateEnvObject`.
- Runtime behavior is `EnvObjectBird`; birds are `EnvObjectType.BIRD` environment events, not livestock.
- Bird drop lookup uses current season state and `SeasonInfo.BirdDropSpawnEntry` at touch/drop time.
- Built-in bird behavior can possibly be reused through content/config, but new behavior is blocked because dispatch is class/enum owned.

## 05 Drones Runtime Equipment

Supplemental findings:

- Native mod config loading likely can merge drone tables through `ModManager.LoadWithMods` and `ModInfo.LoadConfigs`, including `drone_tbdronestructure`, `drone_tbdroneweapon`, `drone_tbdronechip`, `drone_tbdroneengine`, `drone_tbdroneassist`, `drone_tbdroneskill`, and `drone_tbdroneslot`.
- `item_tbitem` plus `ItemFunctionDroneStructure` and matching drone tables can likely produce an `ItemDroneStructure` item, but this remains content-smoke pending.
- Active drone runtime is singleton-oriented: one `AgentEquipmentManager.droneItem`, one `DroneController.CurrentDrone`, one `DolocAPI.droneRenderer`, and `RunDrone` replaces/disposes current state.
- Install/remove transactions should be studied through `DronePanelUiState`, `DroneWidget`, `DroneItemSlot`, and inventory reconciliation.
- `DroneStruct` saves `protoName`, `power`, and component `items`; slots reconstruct from `DroneStructureInfo.Slots`.

## 06 Flying Motor Vehicle Types

Supplemental findings:

- No `TbVehicle`, `vehicle_tb`, `VehicleInfo`, general registry, prefab registry, save slot, or key-to-vehicle mapping was found.
- Existing motor is singleton-owned by `DolocAPI.Motor`, `MotorController`, and `MotorDataManager`.
- `ItemMotorKey.OnUse` places and summons the existing motor by `DolocAPI.SetMotorPosition` and `MotorController.AutoFlyTo`; it does not create a new vehicle type.
- Ride lifecycle crosses `MotorInteractable.OnInteract`, `AgentControllerState.GetOnMotor/GetOffMotor/OnUpdateRiding`, body hiding/rendering, motor riding state, driver hat, camera follow, drone follow target, scanner, and gate policies.

## 08 Hats Accessories Equipment Slots

Supplemental findings:

- Native player equipment fields are fixed: `hatItem`, `droneItem`, `activeItem`, `passiveItem1`, and `passiveItem2`.
- `ArchiveOperationGlobal.EquipPassiveItem` first fills passive1 and only routes to passive2 if `IsAdditionalPassiveSlotUnlocked()` is true and passive2 is empty.
- Direct `EquipPassiveItem2` does not enforce unlock and must be treated as internal/unsafe.
- Hat equip/takeoff refreshes player body and motor driver render paths through `DolocAPI.EquipHat`, `BodyController.SetHatInfo`, and `MotorDriverRenderer.SetHatInfo`.
- Equipment decal/socket placement includes `Equipment`, `IEquipmentHost.CreateEquipment`, `IDecalHost`, slot indexes, terrain fill, and render owners.

## 09 Food Equipment Effects

Supplemental findings:

- `ItemFood` implements `IEatable`; `IEatable.Eat` is the consume route while `DoEffects(scale)` applies configured/fetched effects without owning consumption.
- Eating consumes one item, emits use-item events, applies effects, and creates outputs.
- `AgentEquipmentFunctionFoodEffectsAddition` can reuse food effects by listening and calling `DoEffects`.
- `BuffManager` instant zero-duration effects are applied immediately and not stored; timed buffs are stored by id/UI icon. Duplicate timed add refreshes timer and does not reapply components or replace scale.
- `AgentEquipmentFunction*` lookup is native class/subtype based, but proto deserialization is closed over known `AgentEquipmentFuncProto` variants. Plugin subclasses are not stable extension points.

## 10 Item Stack Quantity Limits

Supplemental findings:

- Native cap remains `ItemInfo.Overlay` and item instance overlay/no-overlay state.
- Safe add/remove wrappers include `DolocAPI.GenerateItem`, `PlaceItem`, `TryPlaceInBackpack`, `PlaceItemAllForce`, `PlaceInBackpackOrGenerateDropItem`, `CostItem`, `CostSelectedItem`, and multi-inventory cost helpers.
- `ItemFactory.GenerateItem` clamps to overlay; `Item.TryCombine` enforces overlay.
- `TryPlaceInBackpack(...sendEmailOnOverflow:true)` may partially place and mail leftovers. With `false`, it can still mutate before incomplete/false result.
- No native over-cap split/clamp migration owner was found.

## 11 Original Follow Pet

Supplemental findings:

- No stable native ground-companion owner was found.
- `AnimalWatchDog` is not usable evidence for a pet system.
- `DroneRenderer.SetFollowTarget`, `DolocAPI.DroneFollowTarget`, `BodyController.DroneFollower`, `MotorController.DroneFollowPoint`, and auto-follow coroutines are drone/motor anchors, not ground-pet behavior.
- Livestock pathing and room transition methods can inform research but do not provide a companion lifecycle.
- Future pet work must be a DTMAPI-owned sidecar with save-slot persistence, room transition handling, ground navigation, feeding state, title-return cleanup, and mod-disable cleanup.

## 12 Held Ranged Weapons Projectiles

Supplemental findings:

- Projectile runtime ownership is solid: `BattleSystem` -> `BulletFactory` -> `BulletManager`.
- Player-held ranged weapon ownership remains not found.
- `NormalFire`, `NormalFireInProgress`, `NormalSwitchAutoFire`, `GunReloadTip`, `GunModeMenu`, `WeaponDebuggerSO`, `DroneWeaponInfo`, and `WeaponFunctionGun*` are drone/debug/specialized terms, not player handheld weapon owners.
- `ItemFarmingGun` is a farming/tool path, not proof of combat ranged weapon support.
- Custom `BulletProto`/`BulletMoverProto` injection is not stable without GameBridge-owned table injection proof and cleanup evidence.
