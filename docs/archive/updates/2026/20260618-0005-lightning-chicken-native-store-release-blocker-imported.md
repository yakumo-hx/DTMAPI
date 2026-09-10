# Update 20260618-0005: Lightning Chicken Native Store Release Blocker (Imported)

> Imported archival note (2026-07-08): this record was originally `20260618-0002` in `E:\Python_project\DTMAPI-animal`. It was renumbered on import because the latest DTMAPI branch already uses `20260618-0002` for installer work. It is historical branch evidence, not current mainline runtime support.

Date: 2026-06-18
Status: blocked-runtime-proof

## Source Request

The active goal is the DTMAPI `0.5.3-alpha` Lightning Chicken MVP: add a developer-only `DTMAPI.LightningChickenMod`, buy `sack_dtmapi_lightning_chicken` through the phone-booth animal shop, release a native animal, prove chicken-template AI and independent animator-controller/clip binding, and verify ordinary chickens are not polluted.

This update records the stop-rule result after two native store/release attempts on save slot 11.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalsNativeTemplateAdapter.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/LightningChickenSmokeCase.cs`
- `docs/reviews/api/2026/20260618-0001-lightning-chicken-native-store-release-blocker.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/2026/20260618-0001-lightning-chicken-runtime-precheck.md`
- `docs/updates/2026/20260618-0002-lightning-chicken-native-store-release-blocker.md`
- `docs/updates/INDEX.md`

## What Changed

- Added a narrow automatic Lightning Chicken smoke attempt that uses native store/package methods instead of direct animal generation:
  - refresh `animal_shop` with `DolocAPI.RefreshStore("animal_shop")`
  - open `animal_shop` with `DolocAPI.OpenStore("animal_shop")`
  - inspect native `StoreUiState.storeItemCaches`
  - buy through private native `StoreUiState.BuyItem(int,bool,bool)` if `sack_dtmapi_lightning_chicken` is visible
  - find the resulting native `ItemAnimalPackage`
  - close the store UI
  - call native `ItemAnimalPackage.OnUseAsTool()`
  - wait for rendered `dtmapi_lightning_chicken` animator isolation evidence
- Added one-shot runtime state fields so the smoke records a single native purchase/release attempt and a useful failure summary.
- Kept `ICustomAnimalApi.RequestSpawn` blocked; no direct animal instantiation was accepted as MVP proof.
- Recorded an API blocker report because the native store cache does not expose the appended package item.

## Runtime Evidence

### `GAME-SMOKE/20260618-012012`

Command:

```powershell
tools/scripts/run-game-smoke.ps1 -AutoExerciseLightningChicken -SaveSlot 11 -TimeoutSeconds 180
```

Result:

- `LightningChicken=Failed`
- `GameLaunched=Passed`
- `SaveLoaded=Passed`
- `ProcessExited=Passed`
- `NoFatalInstanceWindow=Passed`
- `ForcedClose=Passed`

Failure:

```text
animal_shop store cache did not expose sack_dtmapi_lightning_chicken.
visibleItems=sack_chicken|sack_goat|sack_marsh_pangolin|seed_alfalfa|weeds|organic_fertilizer
```

### `GAME-SMOKE/20260618-012554`

Command:

```powershell
tools/scripts/run-game-smoke.ps1 -AutoExerciseLightningChicken -SaveSlot 11 -TimeoutSeconds 180
```

This run added an explicit native `DolocAPI.RefreshStore("animal_shop")` before opening the shop.

Result:

- `LightningChicken=Failed`
- `GameLaunched=Passed`
- `SaveLoaded=Passed`
- `ProcessExited=Passed`
- `NoFatalInstanceWindow=Passed`
- `ForcedClose=Passed`

Failure remained identical:

```text
animal_shop store cache did not expose sack_dtmapi_lightning_chicken.
visibleItems=sack_chicken|sack_goat|sack_marsh_pangolin|seed_alfalfa|weeds|organic_fertilizer
```

## Validation

- Passed: `E:\Python_project\DTMAPI\.tools\dotnet\dotnet.exe build DTMAPI.sln -c Release --nologo -nr:false` with `0` warnings and `0` errors after the latest code changes.
- Passed: `E:\Python_project\DTMAPI\.tools\dotnet\dotnet.exe run --project tests\DTMAPI.UnitTests\DTMAPI.UnitTests.csproj -c Release --no-build` -> `DTMAPI.UnitTests: OK`.
- Passed: PowerShell parser checks for `run-game-smoke.ps1`, `install-to-game.ps1`, `release-common.ps1`, and `build.ps1`.
- Passed: all `testmods/LightningChickenMod/**/*.json` parse as JSON.
- Passed: static contamination scan found no Lightning Chicken override of vanilla `store_tbstoreitemlist.json`, `chicken`, `sack_chicken`, or vanilla `sack_chicken` shop append.
- Passed: controlled runtime/package/script version scan confirms `0.5.3-alpha` / `0.5.3.0` and no remaining `0.5.2-alpha` / `0.5.2.0`.
- Passed with line-ending warnings only: `git diff --check`.
- Cleanup verified after smoke: shared runtime lock released and no `DolocTown.exe` process remained.

## Stop-Rule Result

The MVP cannot be accepted in the current route. Static native content loading, package data, shop-list data, and AI template mapping passed, but native phone-booth animal-shop purchase/release did not.

The likely boundary is between table-level `mod_tbmodstoreextension` merge and the runtime/live `Store` or `StoreUiState` item-cache refresh path. The static `animal_shop` list sees `sack_dtmapi_lightning_chicken`; the opened store cache does not.

Because no native `ItemAnimalPackage` release happened, the smoke also could not reach the rendered `Animal.OnRender` path and could not prove an independent native `RuntimeAnimatorController` / cloned clip path.

## Rollback Notes

The runtime store/release smoke attempt is isolated to `LightningChickenSmokeCase` and the Lightning Chicken runtime tracking fields. Reverting this update should leave the earlier static content, AI adapter, and developer package work reviewable, but it will remove the evidence-producing native store failure path.

Do not publish `0.5.3-alpha` or include `DTMAPI.LightningChickenMod` in public Workshop packages from this branch state.

## Follow-Up

- Deep-research native owners before adding hooks: `Tables.HandleModStoreExtension`, `Store.SpawnStoreItems`, `Store.RefreshItems`, `Store.Refresh`, `DolocAPI.RefreshStore`, and `StoreUiState.HandleStartUpArgs`.
- Decide whether a narrow store-cache hook that admits appended package rows is acceptable for the MVP, or whether it violates the native phone-booth path requirement.
- After native purchase/release works, rerun save-slot 11 smoke to prove rendered animator isolation, movement speed, production cadence, vanilla chicken non-pollution, interaction, reload, and clean exit.
