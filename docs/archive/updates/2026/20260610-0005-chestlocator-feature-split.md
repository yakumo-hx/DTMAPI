# 20260610-0005 ChestLocatorEnhancer Feature Split

## Metadata

- Update ID: 20260610-0005
- Date: 2026-06-10
- Status: verified
- Source: User requested the Refactor stability follow-up route, step `codex/refactor-chestlocator-feature`.
- Owner: Codex

## Scope

- Split low-risk `ChestLocatorEnhancer` into the GameBridge feature-host route.
- Move `IChestLocatorEnhancerApi` registration from `DolocTownExperimentalBridgeApi` to a feature/service pair.
- Move `ArchiveDataHandle.GetAvailableInventories` hook ownership to a hook bridge.
- Preserve public API semantics, `Inventory.ChestLocatorEnhancer` hook status, `Smoke.ChestLocatorEnhancer`, result fields, and `ChestLocatorEnhancerMod` behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/ChestLocatorEnhancer/ChestLocatorEnhancerFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ChestLocatorEnhancer/ChestLocatorEnhancerHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ChestLocatorEnhancer/ChestLocatorEnhancerService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ChestLocatorEnhancer/DolocTownExperimentalBridgeApi.ChestLocatorEnhancer.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Diagnostics/DolocTownExperimentalBridgeApi.Diagnostics.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/ContentSmoke.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0005-chestlocator-feature-split.md`

## Summary

- Renamed the old ChestLocator experimental-bridge partial into `ChestLocatorEnhancerService`.
- Added `ChestLocatorEnhancerFeature` to register `IChestLocatorEnhancerApi` through the feature host.
- Added `ChestLocatorEnhancerHookBridge` to publish/install the `Inventory.ChestLocatorEnhancer` hook status and `ArchiveDataHandle.GetAvailableInventories` postfix.
- Updated `DolocTownHookCallbacks.ArchiveDataHandleGetAvailableInventoriesPostfix` to call `ChestLocatorEnhancerService` instead of `DolocTownExperimentalBridgeApi`.
- Removed `IChestLocatorEnhancerApi` from `DolocTownExperimentalBridgeApi`; other experimental APIs remain unchanged.
- Updated the ChestLocator smoke path to use the feature service API while preserving result/schema/status meanings.

## Validation

- Passed: `git diff --check` with CRLF warnings only.
- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - 0 warnings, 0 errors, `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - 0 warnings, 0 errors, `DTMAPI.UnitTests: OK`.
- Passed third-save smoke:
  - `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseChestLocatorEnhancer -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260610-024237`
  - Report/evidence zip: `docs/debug/evidence/GAME-SMOKE/20260610-024237.zip`

## Evidence

- `result.json` records `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `ChestLocatorEnhancer=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Logs record:
  - `Feature.ChestLocatorEnhancer = ready`
  - `ChestLocatorEnhancer API register success=True`
  - `Inventory.ChestLocatorEnhancer = verified`
  - `ChestLocatorEnhancer inventories owner=DTMAPI.ChestLocatorEnhancerMod, useBox=True, nativeAutoUseBox=True, base=1, appended=5, roots=1, equipments=283, sharedCases=5, sharedStorageBoxes=0`
  - `Smoke.ChestLocatorEnhancer = verified. item=dtmapi_mine, baseline=0, afterPlace=3, afterCost=1`
- Exit checks record no leftover `DolocTown.exe` and no fatal instance popup.
- `latest-report.txt` was corrected to point at `docs/debug/evidence/GAME-SMOKE/20260610-024237.zip`.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- Earlier ChestLocator evidence: `docs/debug/evidence/GAME-SMOKE/20260606-163226`

## Rollback Notes

- Move `IChestLocatorEnhancerApi` implementation and the `ArchiveDataHandle.GetAvailableInventories` hook installation back to `DolocTownExperimentalBridgeApi` if the feature-host split is discarded.
- Restore `DolocTownHookCallbacks` to call `ExperimentalApi.ExtendAvailableInventoriesForChestLocator(...)`.
- No public API DTO rollback is required because this branch does not change the public contract.

## Follow-Up

- Continue with the CameraView manual QA gate record after this branch merges back to `Refactor`.
