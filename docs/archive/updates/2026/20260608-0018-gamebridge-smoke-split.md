# 20260608-0018 GameBridge Smoke Split

## Metadata

- Update ID: 20260608-0018
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Moved the smoke harness implementation from the large `DolocTownGameBridge.cs` file into `src/DTMAPI.GameBridge.DolocTown/Smoke/`.
- Kept hook installation, hook callback methods, hook IDs, status strings, log text, smoke settings schema, and smoke behavior unchanged.
- Moved the `Update` smoke dispatcher, smoke settings load path, `Mark*ForSmoke` entry points, auto-load/auto-save/auto-exit helpers, screenshot/evidence helpers, all `TryExercise*ForSmoke` helpers, smoke manifest builders, smoke-only native probes, and smoke-only nested runtime classes.
- Left hook installation, UI context detection, save-loaded hook subscription, and shared fields in the main partial.
- Did not change hook logic, feature behavior, public API shape, smoke command flags, status output, or CameraZoom behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/DolocTownGameBridge.Smoke.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0018-gamebridge-smoke-split.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `tools/scripts/test.ps1`
  - Release build/test pass completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseStrongPlantingGun -SkipBuild -TimeoutSeconds 300 -AutoExitAfterSecondsOverride 240`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-092055`
- Startup evidence: `DTMAPI-latest.log` starts with `DTMAPI runtime starting.`, and `result.json` records `StartupLog=true`.
- Smoke settings evidence: `summary.txt` records `IncludeHookProbe=True` and `AutoExerciseStrongPlantingGun=True`.
- Auto-load evidence: logs show `Smoke.AutoLoadSave = pending`, official mod-change prompt confirmation, and third-save load.
- Third-save evidence: logs show `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`.
- HookProbe evidence: logs show `HookProbe Entry OK`, `HookProbe GameLaunched OK`, `HookProbe SaveLoaded OK slot=2 isNewGame=False`, and DTMAPI UI status/mod/config/error/hook page checks.
- StrongPlantingGun evidence: logs show `Farming.StrongPlantingGun = experimental` after hook install, `StrongPlantingGun API register success=True reason=SaveLoaded slot=2 enabled=True slots=3 toolHook=True uiHook=True`, `Farming.StrongPlantingGun = verified`, and `Smoke.StrongPlantingGun = verified`.
- Exit evidence: `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`, `process-check.txt` says no `DolocTown.exe`, and `fatal-window-check.txt` says no fatal instance popup.

## Related Records

- Previous split slice: `docs/updates/2026/20260608-0017-gamebridge-motorvehicle-feature-split.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-SMOKE-001`
- Existing HookProbe/input smoke records: `docs/debug/regressions/smoke-matrix.md` rows `INPUT-001`, `INPUT-002`, and `EXIT-002`

## Rollback Notes

- Move `DolocTownGameBridge.Smoke.cs` contents back into `DolocTownGameBridge.cs`.
- No hook IDs, callback paths, status text, log text, smoke settings schema, public API semantics, feature behavior, or CameraZoom behavior require rollback because this was a mechanical smoke split.

## Follow-Up

- Split Diagnostics after the smoke harness remains build- and smoke-clean.
- Keep smoke harness behavior changes, smoke flag changes, and evidence parser changes separate from this mechanical split.
