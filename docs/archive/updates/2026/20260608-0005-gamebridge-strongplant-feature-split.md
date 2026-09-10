# 20260608-0005 GameBridge Strong Planting Gun Feature Split

## Metadata

- Update ID: 20260608-0005
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Moved the Strong Planting Gun feature implementation from the large `DolocTownExperimentalBridgeApi.cs` file into `src/DTMAPI.GameBridge.DolocTown/Features/StrongPlantingGun/`.
- Changed `DolocTownExperimentalBridgeApi` to a partial class so the feature can live in its own file.
- Kept hook installation, hook callback methods, public API shape, hook IDs, status strings, and log text unchanged.
- Did not modify CameraZoom behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/StrongPlantingGun/DolocTownExperimentalBridgeApi.StrongPlantingGun.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0005-gamebridge-strongplant-feature-split.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `git diff --check -- src/DTMAPI.GameBridge.DolocTown`
- Passed: `tools/scripts/build.ps1`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1`
  - Release build/test pass completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseStrongPlantingGun -SkipBuild -TimeoutSeconds 240`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-064314`
- Startup evidence: `StartupLog=true` in `result.json`.
- HookProbe evidence: `HookProbe=true`, `SaveLoaded=true`, and log line `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
- Feature hook evidence: `StrongPlantingGun=true`; logs show `Farming.StrongPlantingGun = verified` and `Smoke.StrongPlantingGun = verified`.
- Exit evidence: `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`, `process-check.txt` says no `DolocTown.exe`, and `fatal-window-check.txt` says no fatal instance popup.

## Related Records

- Previous split slice: `docs/updates/2026/20260608-0004-gamebridge-hooking-split.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-FEATURE-STRONGPLANT-001`
- Hook map: `docs/hook-map/README.md` row `Farming.StrongPlantingGun`

## Rollback Notes

- Move `DolocTownExperimentalBridgeApi.StrongPlantingGun.cs` contents back into `DolocTownExperimentalBridgeApi.cs`.
- Remove `partial` from the main `DolocTownExperimentalBridgeApi` declaration.
- No hook IDs, status text, or API semantics require rollback because this was a mechanical feature split.

## Follow-Up

- Continue moving one feature at a time into `Features/`.
- Keep smoke slices feature-specific and do not use this refactor path to fix CameraZoom or change hook logic.
