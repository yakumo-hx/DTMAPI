# 20260608-0004 GameBridge Hooking Split

## Metadata

- Update ID: 20260608-0004
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Started the mechanical `DTMAPI.GameBridge.DolocTown` split by moving shared hook infrastructure into `src/DTMAPI.GameBridge.DolocTown/Hooking/`.
- Preserved namespaces, type names, hook callback names, hook IDs, status messages, and log text.
- Did not change hook logic, add APIs, or touch CameraZoom behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/HarmonyReflectionPatcher.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0004-gamebridge-hooking-split.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `git diff --check -- src/DTMAPI.GameBridge.DolocTown`
- Passed: `tools/scripts/build.ps1`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1`
  - Release build/test pass completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Initial smoke attempt failed before evidence creation because the current shell had no `DTMAPI_GAME_DIR` or `local.settings.json` game path. The follow-up run used the supported `DTMAPI_GAME_DIR` environment variable.
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseStrongPlantingGun -SkipBuild -TimeoutSeconds 240`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-063627`
- Startup evidence: `StartupLog=true` in `result.json`.
- HookProbe evidence: `HookProbe=true`, `SaveLoaded=true`, and log line `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
- Feature hook evidence: `StrongPlantingGun=true`; logs show `Farming.StrongPlantingGun = verified` and `Smoke.StrongPlantingGun = verified`.
- Exit evidence: `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`, `process-check.txt` says no `DolocTown.exe`, and `fatal-window-check.txt` says no fatal instance popup.

## Related Records

- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-HOOKING-001`
- Hook map: `docs/hook-map/README.md`
- API matrix: `docs/api/public-api-matrix.md`

## Rollback Notes

- Move `DolocTownHookCallbacks.cs` and `HarmonyReflectionPatcher.cs` back to `src/DTMAPI.GameBridge.DolocTown/`.
- No hook logic, API surface, or log/status text needs semantic rollback.

## Follow-Up

- Continue the same mechanical split one feature at a time.
- Next good candidate: move one narrow feature implementation, such as Strong Planting Gun, into `Features/StrongPlantingGun/` while keeping hook IDs, status output, and logs unchanged.
