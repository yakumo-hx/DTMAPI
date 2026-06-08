# 20260608-0006 GameBridge Chest Locator Enhancer Feature Split

## Metadata

- Update ID: 20260608-0006
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Moved the Chest Locator Enhancer feature implementation from the large `DolocTownExperimentalBridgeApi.cs` file into `src/DTMAPI.GameBridge.DolocTown/Features/ChestLocatorEnhancer/`.
- Kept hook installation, hook callback methods, public API shape, hook IDs, status strings, and log text unchanged.
- Left shared helpers, Hooking files, and smoke entry points in their existing files for this slice.
- Did not modify CameraZoom behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ChestLocatorEnhancer/DolocTownExperimentalBridgeApi.ChestLocatorEnhancer.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0006-gamebridge-chestlocator-feature-split.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `git diff --check -- src/DTMAPI.GameBridge.DolocTown`
- Passed: `tools/scripts/build.ps1`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1`
  - Release build/test pass completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseChestLocatorEnhancer -SkipBuild -TimeoutSeconds 240`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-065107`
- Startup evidence: `StartupLog=true` in `result.json`.
- HookProbe evidence: `HookProbe=true`, `SaveLoaded=true`, and log line `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
- Feature hook evidence: `ChestLocatorEnhancer=true`; logs show `Inventory.ChestLocatorEnhancer = verified` and `Smoke.ChestLocatorEnhancer = verified`.
- Native inventory evidence: smoke log shows `base=1`, `appended=5`, `sharedCases=5`, and `Smoke exercise ChestLocatorEnhancer OK item=dtmapi_mine, baseline=0, afterPlace=3, afterCost=1`.
- Exit evidence: `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`, `process-check.txt` says no `DolocTown.exe`, and `fatal-window-check.txt` says no fatal instance popup.

## Related Records

- Previous split slice: `docs/updates/2026/20260608-0005-gamebridge-strongplant-feature-split.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-FEATURE-CHESTLOCATOR-001`
- Hook map: `docs/hook-map/README.md` row `Inventory.ChestLocatorEnhancer`

## Rollback Notes

- Move `DolocTownExperimentalBridgeApi.ChestLocatorEnhancer.cs` contents back into `DolocTownExperimentalBridgeApi.cs`.
- No hook IDs, status text, or API semantics require rollback because this was a mechanical feature split.

## Follow-Up

- Continue moving one feature at a time into `Features/`.
- Split Smoke and Diagnostics only after feature slices remain build- and smoke-clean.
