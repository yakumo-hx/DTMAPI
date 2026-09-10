# Lightning Chicken Native Store/Release Blocker

Date: 2026-06-18
Status: blocked-for-mvp-runtime-proof

## Summary

The Lightning Chicken MVP implementation reached static native table, package, shop-list, and AI-template proof, but it did not reach native phone-booth purchase/release or rendered animator-controller proof.

Stop rule triggered: the current non-Unity route cannot buy and release `dtmapi_lightning_chicken` through the native animal-shop pet-bag path. The native `animal_shop` config list contains `sack_dtmapi_lightning_chicken`, but the live `StoreUiState.storeItemCaches` used by the opened shop does not include it, even after an explicit native `DolocAPI.RefreshStore("animal_shop")`.

## Implemented Route Before Stop

- Developer-only official local package `DTMAPI.LightningChickenMod`.
- Unique species/item IDs:
  - `dtmapi_lightning_chicken`
  - `sack_dtmapi_lightning_chicken`
- Native content rows:
  - `animal_tbanimal.json` copies chicken-like data with `move_speed=3.6`, `run_speed=10.8`, `metabolism_increase=2.08334`, adult price `2000`, and `chicken_produce`.
  - `item_tbitem.json` creates `ItemFunctionAnimalPackage` with `preset_animal=dtmapi_lightning_chicken`, buy price `2000`, sell price `1000`.
  - `mod_tbmodstoreextension.json` appends the bag to `animal_shop`; no `store_tbstoreitemlist.json` override is used.
- Internal GameBridge adapter:
  - `AnimalAI.GetDefaultAnyState(string)` maps only `dtmapi_lightning_chicken` to nested `DolocTown.AnimalAI+Chicken_FreeTimeState`.
  - `Animal.OnRender` / `Animal.DEBUG_SetAdult` attempt to assign a distinct runtime `AnimatorOverrideController` and cloned clip path only after a real rendered lightning chicken exists.
  - `ICustomAnimalApi.RequestSpawn` remains blocked.

## Evidence

### Static/Build Evidence

- Release build passes with `0` warnings and `0` errors after the latest changes.
- Unit tests pass: `DTMAPI.UnitTests: OK`.
- PowerShell parser checks pass for `run-game-smoke.ps1`, `install-to-game.ps1`, `release-common.ps1`, and `build.ps1`.
- Lightning Chicken JSON parse passes.
- Static contamination scan passes:
  - no `testmods/LightningChickenMod/Content/store_tbstoreitemlist.json`
  - no override of `"id": "chicken"`
  - no override of `"id": "sack_chicken"`
  - no vanilla `sack_chicken` append inside Lightning Chicken content

### Runtime Evidence 1: `GAME-SMOKE/20260618-010235`

Command:

```powershell
tools/scripts/run-game-smoke.ps1 -AutoExerciseLightningChicken -SaveSlot 11 -TimeoutSeconds 150
```

Result:

- Shell timed out before `result.json`.
- Logs were collected manually; game was closed and runtime lock released.
- Slot 11/index 10 loaded.
- Static content and AI checks passed.
- Animator proof stayed pending because no rendered `dtmapi_lightning_chicken` existed.

Key log facts:

- `Lightning Chicken content pack loaded. Species=dtmapi_lightning_chicken package=sack_dtmapi_lightning_chicken.`
- `SaveLoaded hook dispatched. slot/index=10 isNewGame=False`
- `CustomAnimals AI template mapped species=dtmapi_lightning_chicken template=chicken state=DolocTown.AnimalAI+Chicken_FreeTimeState previous=DolocTown.AnimalAI+Normal_FreeTimeState.`
- `Smoke exercise LightningChicken static checks OK ... package=buy:2000/sell:1000 ... shop=animal_shop-append-preserved ... animator=pending-rendered-instance`

### Runtime Evidence 2: `GAME-SMOKE/20260618-012012`

Added an automatic native store/release attempt:

- `DolocAPI.OpenStore("animal_shop")`
- read `StoreUiState.storeItemCaches`
- find `sack_dtmapi_lightning_chicken`
- call native `StoreUiState.BuyItem(int,bool,bool)`
- find bought `ItemAnimalPackage`
- call native `ItemAnimalPackage.OnUseAsTool()`
- wait for rendered animator proof

Result:

- `result.json` exists.
- `LightningChicken=Failed`.
- `GameLaunched=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`.
- Runtime lock was released and no `DolocTown.exe` remained.

Failure:

```text
animal_shop store cache did not expose sack_dtmapi_lightning_chicken.
visibleItems=sack_chicken|sack_goat|sack_marsh_pangolin|seed_alfalfa|weeds|organic_fertilizer
```

### Runtime Evidence 3: `GAME-SMOKE/20260618-012554`

Added native store refresh before opening the shop:

- `DolocAPI.RefreshStore("animal_shop")`
- then the same `OpenStore` / `StoreUiState.storeItemCaches` route

Result:

- `result.json` exists.
- `LightningChicken=Failed`.
- `GameLaunched=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`.
- Runtime lock was released and no `DolocTown.exe` remained.

Failure remained identical:

```text
animal_shop store cache did not expose sack_dtmapi_lightning_chicken.
visibleItems=sack_chicken|sack_goat|sack_marsh_pangolin|seed_alfalfa|weeds|organic_fertilizer
```

## Rejected Or Deferred Paths

- Direct `DolocAPI.GenerateItem("sack_dtmapi_lightning_chicken", 1)` was not used as acceptance proof because it bypasses the requested phone-booth/shop purchase path.
- Direct animal instantiation was not used because the goal explicitly rejects pure-code animal creation as proof.
- Broad IL transpilers were not attempted.
- Unity Editor / AssetBundle authoring was not attempted; independent animator proof was not reached because no native lightning chicken instance was released/rendered.

## Current Failure Boundary

The DTMAPI content tables are loaded and the static `TbStoreItemList animal_shop` check sees the appended bag. The live native store UI path does not.

The likely boundary is between table-level `mod_tbmodstoreextension` merge and the save/runtime `Store.currentItems` / `StoreUiState.storeItemCaches` refresh path. The visible current shop cache remains the original animal-shop selection plus several non-animal fixed items, and does not include the new fixed extra item.

This blocks the MVP because the animal cannot be bought as a native pet bag, so the native `ItemAnimalPackage` release path and `Animal.OnRender` animator isolation cannot be proven.

## Questions For Deeper Research

1. Does `mod_tbmodstoreextension` update `TbStoreItemList` only, while existing save `Store.currentItems` remains based on pre-mod generated item names?
2. Does `Store.Refresh()` / `DolocAPI.RefreshStore("animal_shop")` ignore mod-store extension rows, or does it require a different unlock/store-level/season path?
3. Is the Lightning item row missing a required official-store field beyond the documented `item_name`, `spawn_weight`, count, discount, season, and date fields?
4. Does the phone booth animal shop use a store ID or current-item source different from `TbStoreItemList animal_shop` despite the static table name?
5. Would sleeping to the next day, changing season/date, or using a true phone-booth UI click regenerate `Store.currentItems` differently than `RefreshStore`?
6. Is a narrow hook around `Store.SpawnStoreItems` or `StoreUiState.HandleStartUpArgs` acceptable, or would that count as polluting the native shop path?
7. Once purchase/release works, can the runtime `AnimatorOverrideController` + cloned clip path satisfy the strict independent controller requirement without Unity Editor or AssetBundle authoring?

## Recommended Next Step

Do not continue adding speculative hooks. Deep-research the native store refresh/extension owner first, especially `Store.SpawnStoreItems`, `Store.RefreshItems`, `StoreUiState.HandleStartUpArgs`, and `Tables.HandleModStoreExtension`. If the store cache cannot include appended animal-package rows without a hook, decide whether a narrow store-cache hook is allowed by the MVP or whether this requires a Unity/official-content workflow.
