# 20260612-0002 Crops Harvesting API

Date: 2026-06-12

Status: verified

Area: api/gamebridge/crops/smoke/testmod/docs

## Summary

Added the first `Experimental` Crops / Harvesting API over the reviewed native `PlantBasin.CouldHarvest` / `PlantBasin.Harvest(bool,bool)` responsibility path. The slice adds public semantic DTOs, a GameBridge `CropHarvestingFeature` and service, a transient mature-crop smoke path, and a default-off `AutoHarvestMod` sample that consumes only the DTMAPI API. It does not copy old DLKsmapi/DolocSMAPI AutoHarvest code and does not change existing gameplay features.

## Source Request

User asked to complete a Crops / Harvesting API based on native responsibility functions, after reading agent rules and writing update/debug/review content. The supplied screenshots of an external old AutoHarvest mod were reference material only.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CropHarvesting/CropHarvestingFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CropHarvesting/CropHarvestingService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/CropHarvestingSmokeCase.cs`
- `testmods/AutoHarvestMod/*`
- `tools/scripts/build.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `DTMAPI.sln`
- `docs/reviews/api/2026/20260612-crops-harvesting-native-responsibility.md`
- `docs/goals/2026/20260612-0001-crops-harvesting-api.md`
- `docs/goals/2026/20260612-0001-crops-harvesting-api.goal.txt`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `testmods/README.md`

## Details

- Added `ICropHarvestingApi` with scan and harvest operations, `CropHarvestRequest`, result DTOs, target kind/status enums, and explicit `Experimental` documentation.
- Added `CropHarvestingFeature` to register the API through the feature host and publish `Feature.CropHarvesting` / `Crops.HarvestingApi` status.
- Added `CropHarvestingService` with a batch lock, normalized request limits, current-farm/farm-room scan, opaque target ids, family filters, dry-run support, revalidation before harvest, and native `PlantBasin.Harvest` execution.
- Materialized each room's equipment list before native harvest, because `PlantBasin.Harvest` can mutate native equipment/crop collections while the API is processing targets.
- Classifies unsupported native crop families instead of force-harvesting them. Trees and grass/forage-style basins remain unsupported in the first API version.
- Added `run-game-smoke.ps1 -AutoExerciseCropHarvestingApi` and smoke fields `CropHarvestingApi` / `CropHarvestingApiEvidence`.
- Added smoke-only transient mature `PlantBasin` setup using existing GameBridge smoke helpers: place seed in quick slot, point the native agent cell tip at the transient basin, call native `ItemSeed.PlantSeed`, run the native interact-exit path, force mature for smoke only, then scan/harvest through the public API.
- Added the new CropHarvesting result fields to the global smoke failure calculation so future failures cannot be reported as an overall passed run.
- Added `testmods/AutoHarvestMod`, a default-off sample mod that uses only `ICropHarvestingApi`, has no Harmony dependency, and does not reference `Assembly-CSharp`.

## Validation

Passed:

- `git diff --check` (line-ending warnings only).
- PowerShell AST parse for `tools/scripts/run-game-smoke.ps1`.
- `tools/scripts/build.ps1 -Configuration Release`.
- `tools/scripts/test.ps1 -Configuration Release`.
- `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseCropHarvestingApi -SaveSlot 3 -TimeoutSeconds 240`.

Intermediate failures fixed before final validation:

- `GAME-SMOKE/20260612-060648` reached save load but failed `Smoke.CropHarvestingApi` because direct `ItemSeed.PlantSeed` setup left the transient basin without a crop. The smoke setup was changed to use the existing native-interaction helper path.
- `GAME-SMOKE/20260612-061539` proved setup and native harvest changed the target state, but the API returned failure because native harvest modified a collection during delayed equipment enumeration. The service now snapshots each room's equipment list before harvest.

## Evidence

- Native responsibility review: `docs/reviews/api/2026/20260612-crops-harvesting-native-responsibility.md`.
- Goal handoff: `docs/goals/2026/20260612-0001-crops-harvesting-api.md`.
- Smoke matrix case: `CROPS-HARVESTING-API-20260612`.
- `GAME-SMOKE/20260612-062154`: DirectExe third-save crop harvesting smoke passed with `RunStatus=Passed`, `SaveLoaded=Passed`, `CropHarvestingApi=Passed`, `CropHarvestingApiEvidence=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`. Logs show `Feature.CropHarvesting=ready`, `Crops.HarvestingApi=scan-verified`, `Crops.HarvestingApi=verified`, `Smoke.CropHarvestingApi=verified`, `rooms=13`, `basins=172`, `mature=1`, `harvested=1`, `skipped=171`, `failed=0`, and `targetFilter=1`.

## Rollback Notes

Rollback by reverting this update. The rollback removes the new public experimental crop harvesting DTOs/API, GameBridge feature/service, smoke flag/results, AutoHarvest test mod, and documentation updates. It does not affect existing Camera, Fishing, SaveSlots, StrongPlantingGun, AnimalViewer, release, or Manager UI behavior.

## Follow-Up

- Add native-owner reviews for tree crops, forage/grass harvest, and any mushroom/vine special cases before supporting those target families.
- Add longer manual QA with real mature crop fields across seasons and building rooms.
- Decide multi-owner scheduling/lease policy before treating auto-harvest as stable ordinary-mod behavior.
- Consider player-facing Workshop release only after the API has more crop-family evidence.
