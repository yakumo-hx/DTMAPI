# 06 - 0.3.0 Chest Locator Enhancer And Strong Planting Gun APIs

## Scope

Audited the two remaining 0.3.0 feature APIs not covered by 0008:

- `IChestLocatorEnhancerApi.Register/GetState/GetStatus`
- `IStrongPlantingGunApi.Register/GetState/GetStatus`
- DTO semantics for `ChestLocatorEnhancerOptions/RegisterResult/State`
- DTO semantics for `StrongPlantingGunOptions/RegisterResult/State`

Both APIs are real native-hook adapters, not pure registry stubs. The question is whether they are stable ordinary-mod APIs or restricted DTMAPI official-local feature contracts.

## Files read

- `docs/api/public-api-matrix.md`: lines 89-90.
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`: lines 172-186 and 832-908.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`: lines 883-920.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`: lines 331-354.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`: lines 1332-1418 and 1420-2198.
- `testmods/ChestLocatorEnhancerMod/ModEntry.cs`: lines 15-99.
- `testmods/StrongPlantingGunMod/ModEntry.cs`: lines 15-107.
- `docs/hook-map/README.md`: Chest Locator / Strong Planting Gun entries found through hook-map search.
- `docs/debug/regressions/smoke-matrix.md`: 0.3.0 feature evidence rows found through search.
- `docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md`: lines 33-74.
- `docs/updates/2026/20260606-0010-030-strong-planting-gun.md`: lines 33-78.
- `references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md`: `UseAllChests` / `ArchiveDataHandle.GetAvailableInventories` and farming automation native-owner notes.
- `references/doloc-town/research-notes/research-DolocPlus-deep-dive-20260607.md`: chest locator and farming gun native-owner notes.
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/maps/Items_Inventory.md`: `ItemFarmingGun`, `FarmingGunUiState`, and inventory candidate map.

## Functions read

- `DTMAPI.Abstractions.IChestLocatorEnhancerApi.Register`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:175`.
- `DTMAPI.Abstractions.IChestLocatorEnhancerApi.GetState`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:176`.
- `DTMAPI.Abstractions.IChestLocatorEnhancerApi.GetStatus`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:177`.
- `DTMAPI.Abstractions.IStrongPlantingGunApi.Register`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:183`.
- `DTMAPI.Abstractions.IStrongPlantingGunApi.GetState`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:184`.
- `DTMAPI.Abstractions.IStrongPlantingGunApi.GetStatus`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:185`.
- `DolocTownGameBridge.InstallHarmonyHooks` chest/strong hook slice: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:883`.
- `DolocTownHookCallbacks.ArchiveDataHandleGetAvailableInventoriesPostfix`: `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:331`.
- `DolocTownHookCallbacks.ItemFarmingGunCtorPostfix`: `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:336`.
- `DolocTownHookCallbacks.ItemFarmingGunOnUseAsToolPrefix`: `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:341`.
- `DolocTownHookCallbacks.FarmingGunUiStateHandlePlaceToOtherSidePrefix`: `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:346`.
- `DolocTownHookCallbacks.FarmingGunUiStateHandleSwapOneItemPrefix`: `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs:351`.
- `DolocTownExperimentalBridgeApi.IChestLocatorEnhancerApi.Register`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1332`.
- `DolocTownExperimentalBridgeApi.IChestLocatorEnhancerApi.GetState`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1362`.
- `DolocTownExperimentalBridgeApi.IChestLocatorEnhancerApi.GetStatus`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1367`.
- `DolocTownExperimentalBridgeApi.IStrongPlantingGunApi.Register`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1373`.
- `DolocTownExperimentalBridgeApi.IStrongPlantingGunApi.GetState`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1409`.
- `DolocTownExperimentalBridgeApi.IStrongPlantingGunApi.GetStatus`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1414`.
- `DolocTownExperimentalBridgeApi.ExtendAvailableInventoriesForChestLocator`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1621`.
- `DolocTownExperimentalBridgeApi.TryGetChestLocatorPolicy`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1712`.
- `DolocTownExperimentalBridgeApi.ExpandFarmingGunInventoryIfNeeded`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1420`.
- `DolocTownExperimentalBridgeApi.HandleStrongPlantingGunToolUse`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1471`.
- `DolocTownExperimentalBridgeApi.HandleStrongPlantingGunUiPlaceToOtherSide`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1557`.
- `DolocTownExperimentalBridgeApi.HandleStrongPlantingGunUiSwapOneItem`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1587`.
- `DolocTownExperimentalBridgeApi.TryPrepareStrongPlantingGunUiTransfer`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2153`.
- `ChestLocatorEnhancerMod.ModEntry.BindChestLocatorApi`: `testmods/ChestLocatorEnhancerMod/ModEntry.cs:41`.
- `StrongPlantingGunMod.ModEntry.BindStrongPlantingGunApi`: `testmods/StrongPlantingGunMod/ModEntry.cs:43`.

## Call graph

```text
Chest Locator Enhancer
  ChestLocatorEnhancerMod.Entry/SaveLoaded/config saved
    IChestLocatorEnhancerApi.Register
      stores first enabled owner policy
      state = configured-experimental-inventory-hook or pending
  Harmony Postfix: ArchiveDataHandle.GetAvailableInventories
    DolocTownHookCallbacks.ArchiveDataHandleGetAvailableInventoriesPostfix
      ExtendAvailableInventoriesForChestLocator
        start with native LinearInventory[] result
        scan candidate rooms/equipment
        append shared Case inventories
        append ItemBox inventories inside shared StorageShelf when useBox/native setting allow
        return expanded native inventory array
  native CountItem/CostItem keeps using official transaction logic over returned inventory array

Strong Planting Gun
  StrongPlantingGunMod.Entry/SaveLoaded/config saved
    IStrongPlantingGunApi.Register
      stores first enabled owner policy
      state = configured-experimental-tool-ui-hooks or pending
  Harmony Postfix: ItemFarmingGun constructors
    ExpandFarmingGunInventoryIfNeeded
      LinearInventory.ValidateCapacity
      reflected Capacity backing field on gun func
  Harmony Prefix: ItemFarmingGun.OnUseAsTool
    HandleStrongPlantingGunToolUse
      enumerate gun-area equipments
      read allowed seed/film/fertilizer/water slot items
      official CheckCanInteract
      cost one item from gun inventory
      official DoInteract
      skip original method only after consumed actions
  Harmony Prefixes: FarmingGunUiState.HandlePlaceToOtherSide / HandleSwapOneItem
    custom transfer into expanded gun inventory
```

## Function body findings

- `IChestLocatorEnhancerApi.Register` normalizes options, stores one owner policy by unique id, marks hook installation status, and returns success even if the native hook is not installed yet (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1332-1359`).
- GameBridge patches `ArchiveDataHandle.GetAvailableInventories` with an array-result postfix and records that DTMAPI appends official shared-container inventories without replacing native `CountItem/CostItem` logic (`src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:883-892`).
- `ExtendAvailableInventoriesForChestLocator` starts from the native inventory array, deduplicates by object identity, scans candidate rooms/equipment, checks `IsShared`, appends `Case.inventory`, and optionally appends `ItemBox.inventory` found inside shared `StorageShelf` inventories when `useBox` and native auto-use box settings allow it (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1621-1709`).
- `TryGetChestLocatorPolicy` picks the first enabled policy in dictionary enumeration order. There is no multi-owner merge or priority contract (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1712-1727`).
- `IStrongPlantingGunApi.Register` normalizes options, stores one owner policy by unique id, reports tool/UI hook status, and returns success even if a hook is pending (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1373-1406`).
- GameBridge patches official `ItemFarmingGun` constructors, `OnUseAsTool`, and `FarmingGunUiState` transfer methods. This is a fragile reflected native path, not a native official extension point (`src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:894-920`).
- `ExpandFarmingGunInventoryIfNeeded` uses `LinearInventory.ValidateCapacity` and a reflected function capacity field/property to expand the official gun storage to the requested slot count (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1420-1469`).
- `HandleStrongPlantingGunToolUse` reads current gun inventory slots, filters allowed item kinds, enumerates equipment in range, calls official `CheckCanInteract`, costs one item from the gun inventory, then calls official `DoInteract`. It returns `false` only after at least one DTMAPI-applied action, thereby replacing the original method for that use (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1471-1554`).
- `HandleStrongPlantingGunUiPlaceToOtherSide` and `HandleStrongPlantingGunUiSwapOneItem` implement DTMAPI transfer behavior for expanded gun storage by taking/costing from backpack and placing into the container inventory (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1557-1618`).
- `TryPrepareStrongPlantingGunUiTransfer` only handles backpack-to-farming-gun transfers when the UI buffer is empty, selected item is allowed, and the container is an official farming gun. Other UI paths fall back to native code (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2153-2192`).
- The test mods are intentionally thin: they read config, register policy on Entry/SaveLoaded/config save, and show status text; they do not own Harmony, reflection, or native types (`testmods/ChestLocatorEnhancerMod/ModEntry.cs:41-58`, `testmods/StrongPlantingGunMod/ModEntry.cs:43-62`).

## Native owner verdict

| API | Verdict | Native owner reached |
| --- | --- | --- |
| `IChestLocatorEnhancerApi` | Partial/Reaches native inventory enumeration | Native `ArchiveDataHandle.GetAvailableInventories` result is extended, and native `CountItem/CostItem` can then see appended inventories. DTMAPI owns room/equipment traversal and policy arbitration. |
| `IStrongPlantingGunApi` | Partial/Reaches native farming gun owners | Official `ItemFarmingGun`, `LinearInventory`, `CheckCanInteract`, `DoInteract`, and `FarmingGunUiState` paths are reached. DTMAPI owns capacity expansion, transfer interception, and multi-slot iteration. |

## Ordinary mod usability

- `IChestLocatorEnhancerApi`: 仅 DTMAPI 自家 mod 可用 / restricted experimental. Do not present as a stable ordinary-mod shared-inventory API.
- `IStrongPlantingGunApi`: 仅 DTMAPI 自家 mod 可用 / restricted experimental. Do not present as a stable ordinary-mod farming-tool extension API.

## Concrete failure modes

1. Chest Locator policy arbitration is first-enabled-owner wins. Multiple ordinary mods can register conflicting shared-container policies with nondeterministic dictionary-order results.
2. Chest Locator scans candidate rooms/equipment outside the native method's original visible scope. A wrong room candidate or stale equipment reference can make material lookup include inventories the player should not reach.
3. Chest Locator DTO fields such as `LastAppendedInventoryCount` and `LastSharedCaseCount` can be misread as stable native inventory ownership; they are DTMAPI telemetry for a postfix traversal.
4. Strong Planting Gun expands official gun capacity via reflected `ValidateCapacity` and capacity backing fields. A native update can break storage or leave UI/storage size mismatch.
5. Strong Planting Gun consumes from gun inventory before `DoInteract` success is fully under native transaction ownership; exceptions or partial paths can produce item loss or duplicated leftovers if reflection assumptions change.
6. Strong Planting Gun UI prefixes only cover selected backpack transfer paths. Ordinary mods expecting a general expanded-container UI can hit unhandled swap/buffer paths.
7. Strong Planting Gun `IncludeWater` exists in the public options, but the official-local mod sets it false and smoke evidence focuses seed/film/fertilizer; water behavior should not be advertised as stable.

## Minimal rebuild direction

- Keep both APIs experimental and restricted to DTMAPI official-local features.
- For Chest Locator, design an owner-token policy merge contract, explicit room-scope rules, and tests for multiple registered owners before ordinary-mod documentation.
- For Chest Locator DTOs, document telemetry fields as DTMAPI hook observations, not proof of native ownership.
- For Strong Planting Gun, split stable config/policy DTOs from runtime adapter internals: capacity expansion, UI transfer, tool-use loop, and save/load persistence should each have separate owner tests.
- Add negative tests for multi-owner registration, disabled policy, hook pending state, and native method-signature drift before considering public stabilization.

## Evidence gaps

- No multi-mod arbitration test was found for either API.
- No long-term save/load audit was found for expanded farming-gun inventory contents across reloads in this pass.
- Strong Planting Gun water-slot behavior has no direct evidence in the read smoke/update records.
- Reverse maps/research notes confirm the reviewed native candidates, especially `ArchiveDataHandle.GetAvailableInventories` and `ItemFarmingGun`, but they also reinforce that DTMAPI is patching native flows rather than using an official extension registry.
- No game smoke was run in this pass. Existing evidence comes from public matrix lines 89-90, update records `20260606-0009` / `20260606-0010`, and hook/status code paths cited above.
