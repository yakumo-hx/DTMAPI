# 20260608-0007 GameBridge CameraZoom Feature Split

## Metadata

- Update ID: 20260608-0007
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Moved the CameraZoom feature implementation from the large `DolocTownExperimentalBridgeApi.cs` file into `src/DTMAPI.GameBridge.DolocTown/Features/CameraZoom/`.
- Kept hook installation, hook callback methods, public API shape, hook IDs, status strings, and log text unchanged.
- Moved only CameraZoom-owned lifecycle entry points, API methods, apply/compensation helpers, and CameraZoom-only nested runtime DTOs.
- Left shared reflection/vector helpers, smoke entry points, and Hooking files in their existing files for this slice.
- Did not fix or otherwise change CameraZoom behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CameraZoom/DolocTownExperimentalBridgeApi.CameraZoom.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0007-gamebridge-camerazoom-feature-split.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `git diff --check -- src/DTMAPI.GameBridge.DolocTown`
- Passed: `tools/scripts/build.ps1`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1`
  - Release build/test pass completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseZoom -SkipBuild -TimeoutSeconds 260`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-065908`
- Startup evidence: `StartupLog=true` in `result.json`.
- HookProbe evidence: `HookProbe=true`, `SaveLoaded=true`, and log line `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
- Feature hook evidence: `Zoom=true`; logs show `Camera.ZoomEnvironmentLifecycle = experimental`, `Camera.ZoomApi = verified`, and `Smoke.Zoom = verified`.
- Camera behavior evidence: logs show `Camera orthographic size 16.875->67.5 viewScale=4 owner=DTMAPI.ZoomMod`, `background=compensated-background=4`, `fog=compensated-depth-fog=4; compensated-building-depth-fog=4`, `scanner=refreshed`, and reset `Camera orthographic size 67.5->16.875 viewScale=1`.
- Screenshot evidence exists under `D:\Steam\steamapps\common\Doloc Town\DTMAPI\evidence\ZOOM-042\20260608-065945`: `zoom-before.png`, `zoom-4x.png`, and `zoom-reset.png`.
- Exit evidence: `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`, `process-check.txt` says no `DolocTown.exe`, and `fatal-window-check.txt` says no fatal instance popup.

## Related Records

- Previous split slice: `docs/updates/2026/20260608-0006-gamebridge-chestlocator-feature-split.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-FEATURE-CAMERAZOOM-001`
- Hook map: `docs/hook-map/README.md` rows `Camera.ZoomApi` and `Camera.ZoomEnvironmentLifecycle`

## Rollback Notes

- Move `DolocTownExperimentalBridgeApi.CameraZoom.cs` contents back into `DolocTownExperimentalBridgeApi.cs`.
- No hook IDs, status text, log text, or API semantics require rollback because this was a mechanical feature split.

## Follow-Up

- Continue moving one feature at a time into `Features/`.
- Keep this refactor path separate from CameraZoom behavior fixes.
- Split Smoke and Diagnostics only after feature slices remain build- and smoke-clean.
