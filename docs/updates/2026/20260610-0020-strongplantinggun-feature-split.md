# 20260610-0020 StrongPlantingGun Feature Split

## Status

Verified.

## Source Request

User requested the post-midterm follow-up route and explicitly selected `StrongPlantingGunFeature` as one of the two additional feature splits.

## Summary

- Split `IStrongPlantingGunApi` ownership out of `DolocTownExperimentalBridgeApi` into `StrongPlantingGunFeature` and `StrongPlantingGunService`.
- Added `StrongPlantingGunHookBridge` to own the `ItemFarmingGun` constructor/use and `FarmingGunUiState` transfer hook installation.
- Changed StrongPlantingGun hook callbacks to call `Bridge.StrongPlantingGunService` rather than `Bridge.ExperimentalApi`.
- Changed smoke setup to register and expand through `StrongPlantingGunService`.
- Removed the experimental bridge's duplicate `Farming.StrongPlantingGun` contract publication.
- Kept `Farming.StrongPlantingGun`, `Farming.StrongPlantingGunUi`, `Smoke.StrongPlantingGun`, public DTOs, seed/film/fertilizer behavior, result schema, and testmod behavior unchanged.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Diagnostics/DolocTownExperimentalBridgeApi.Diagnostics.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/StrongPlantingGun/StrongPlantingGunFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/StrongPlantingGun/StrongPlantingGunHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/StrongPlantingGun/StrongPlantingGunService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/ContentSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- DirectExe third-save StrongPlantingGun smoke passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseStrongPlantingGun -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `GAME-SMOKE/20260610-101436`
  - Result: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `StrongPlantingGun=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
  - Logs: `Feature.StrongPlantingGun = ready`; `Farming.StrongPlantingGun = experimental`; `StrongPlantingGun API register success=True ... toolHook=True uiHook=True`; `StrongPlantingGun use owner=DTMAPI.StrongPlantingGunMod, slots=3, equipments=1, seedActions=1, filmActions=1, fertilizerActions=1, waterActions=0, consumed=3`; `Farming.StrongPlantingGun = verified`; `Smoke.StrongPlantingGun = verified`.
  - Report pointer: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-101512.zip`.
  - Exit check: `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup.

## Evidence Links

- Hook map: `docs/hook-map/README.md#hook-farmingstrongplantinggun`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folder: `docs/debug/evidence/GAME-SMOKE/20260610-101436`

## Rollback

- Re-register `IStrongPlantingGunApi` directly on `DolocTownExperimentalBridgeApi`.
- Move StrongPlantingGun hook installation back into `DolocTownGameBridge.InstallHarmonyHooks()`.
- Point callbacks and smoke setup back to `ExperimentalApi`.
- Re-run the third-save `-IncludeHookProbe -AutoExerciseStrongPlantingGun` smoke.

## Follow-Up

- Keep `IStrongPlantingGunApi` Experimental until broader UI transfer/manual coverage confirms all official farming-gun transfer paths beyond the current seed/film/fertilizer smoke.
