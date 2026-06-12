# 20260612-0003 Crops Harvesting API Hardening 1

Date: 2026-06-12

Status: verified

## Summary

Tightened the first `Experimental` `ICropHarvestingApi` slice after audit review. The API is now documented and named as a crop-container harvesting API rather than a generic tree/grass auto-harvest API. The only executable native owner remains the reviewed `PlantBasin.Harvest(bool,bool)` path.

## Source Request

User asked to apply the audit feedback and emphasized that this API must not be described as auto-harvesting grass or trees. `PlantBasinTree` is a tree-basin crop container for cocoa-style crops in the current game, while other categories include ordinary crops, vines, mushroom bags, and bushes.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CropHarvesting/CropHarvestingService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/CropHarvestingSmokeCase.cs`
- `testmods/AutoHarvestMod/*`
- `docs/reviews/api/2026/20260612-crops-harvesting-native-responsibility.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Implementation Notes

- Renamed misleading public target kinds from tree/grass wording to crop-container wording: `TreeBasinCrop`, `GrassForageBasin`, `MushroomBag`, `Bush`, `Vine`, and `OrdinaryCrop`.
- Renamed request filters to avoid implying wild-tree support: `IncludeMushroomBags`, `IncludeBushes`, and `IncludeTreeBasinCrops`.
- Documented `TargetId` as an opaque transient handle that is only valid for an immediate same-session follow-up harvest request.
- Changed success semantics so successful scans/harvest requests with no executable mature targets are no-ops, not API failures.
- Made `Pending` execution require basin-level `CouldHarvest` / `IsCropMature`; `Crop.isMature` and `TreeCrop.isMature` are diagnostic only.
- Validated the native harvest reflection target as `Harvest(bool,bool)` instead of only matching by name and parameter count.
- Stopped scanning arbitrary current rooms by default; the scope now prefers farm roots and farm building-room candidates.
- Kept `PlantBasinTree` / cocoa-style tree-basin crops scan-only/unsupported because their native owner is different from `PlantBasin.Harvest(bool,bool)`.
- Fixed AutoHarvestMod ConfigMenu-missing wording and stopped registering the `"None"` manual key.

## Validation

- `git diff --check` passed with line-ending warnings only.
- PowerShell AST parse for `tools/scripts/run-game-smoke.ps1` passed.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseCropHarvestingApi -SaveSlot 3 -TimeoutSeconds 240` passed.

## Evidence

- `docs/debug/evidence/GAME-SMOKE/20260612-072544`: `RunStatus=Passed`, `SaveLoaded=Passed`, `CropHarvestingApi=Passed`, `CropHarvestingApiEvidence=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Logs show `CropHarvesting API Scan ... mature=0 harvested=0 skipped=172 failed=0 targetFilter=1` followed by `noTargetScan={success=True ... mature=0 ... failed=0}`, proving no-target scan is a successful no-op.
- Logs also show the ordinary `PlantBasinSimple` transient target still scans and harvests through `PlantBasin.Harvest(bool,bool)`: `mature=1`, `harvested=1`, `failed=0`, `Crops.HarvestingApi=verified`, and `Smoke.CropHarvestingApi=verified`.
- Process/fatal checks report no leftover `DolocTown.exe` and no fatal instance popup.
- Prior first-slice evidence remains `docs/debug/evidence/GAME-SMOKE/20260612-062154`.

## Related Records

- `docs/reviews/api/2026/20260612-crops-harvesting-native-responsibility.md`
- `docs/updates/2026/20260612-0002-crops-harvesting-api.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Rollback Notes

Revert this update to return to the first-slice API naming and success semantics. Do not partially revert only the docs or only the DTO names; the docs and code are intentionally aligned so ordinary mod authors do not mistake this API for tree/grass automation.

## Follow-up

If tree-basin cocoa harvest execution is desired, do a separate native-owner review for `PlantBasinTree`, `TreeCrop`, `GenerateCropOutput`, and `TreeCrop.OnFell/OnCompleteFell`. Do not add it to the ordinary `PlantBasin.Harvest` slice.
