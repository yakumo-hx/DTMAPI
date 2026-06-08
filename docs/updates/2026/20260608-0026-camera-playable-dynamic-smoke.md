# 20260608-0026 CameraPlayable Dynamic Smoke

## Metadata

- Update ID: 20260608-0026
- Date: 2026-06-08
- Status: verified
- Source: Active goal: add CAMERA-PLAYABLE dynamic validation without changing CameraView implementation
- Owner: Codex

## Summary

- Extended the CAMERA-PLAYABLE smoke from static before/4x/reset evidence into sustained dynamic evidence.
- Added 30-second dynamic movement phases for both active 4x and fallback 2x camera leases.
- Recorded player position, camera position, orthographic size, active owner/lease, room, scale, arbitration, UI scale, and native-refresh status into `camera-playable-dynamic-telemetry.csv`.
- Captured start/mid/end screenshots for both 4x and 2x phases.
- Kept CameraView implementation unchanged: playable zoom remains lease arbitration plus orthographic-size-only writes.
- Retained `zoom-before.png`, `zoom-4x.png`, and `zoom-reset.png` as context only; the single `zoom-4x.png` is no longer accepted as passing evidence.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Smoke/CameraSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-009-20260608-camera-playable-dynamic-qa.md`
- `docs/debug/issues/README.md`
- `docs/debug/INDEX.md`
- `docs/updates/2026/20260608-0026-camera-playable-dynamic-smoke.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: script syntax check for `tools/scripts/run-game-smoke.ps1`.
- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -SkipBuild -IncludeHookProbe -AutoExerciseZoom -TimeoutSeconds 300`
  - `RunStatus=Passed`
  - `StartupLog=Passed`
  - `HookProbe=Passed`
  - `SaveLoaded=Passed`
  - `Zoom=Passed`
  - `ProcessExited=Passed`
  - `NoFatalInstanceWindow=Passed`
  - `ForcedClose=Passed`

## Evidence

- Final game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-222542`
- CAMERA-PLAYABLE evidence folder: `D:\Steam\steamapps\common\Doloc Town\DTMAPI\evidence\CAMERA-PLAYABLE\20260608-222622`
- Startup/HookProbe evidence: `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
- Smoke status evidence: logs show `Smoke.CameraPlayable = verified` and `Smoke.Zoom = verified`.
- Telemetry file: `camera-playable-dynamic-telemetry.csv`.
- Summary file: `summary.txt`.
- 4x dynamic screenshots:
  - `dynamic-4x-start.png`
  - `dynamic-4x-mid.png`
  - `dynamic-4x-end.png`
- 2x dynamic screenshots:
  - `dynamic-2x-start.png`
  - `dynamic-2x-mid.png`
  - `dynamic-2x-end.png`
- 4x dynamic result: 30.016s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=67.5-67.5`, active owner `DTMAPI.ZoomMod`, room `farm_type1-平地`.
- 2x dynamic result: 30.005s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=33.75-33.75`, active owner `DTMAPI.CameraViewCompetingSmoke`, room `farm_type1-平地`.
- Implementation boundary evidence: telemetry and summary keep `nativeRefresh=not-called-playable`, `uiScale=unchanged`, and `movement=smoke-agentposition-camera-setposition`.
- Exit evidence: `process-check.txt` says no `DolocTown.exe` process was found; `fatal-window-check.txt` says no fatal instance popup was found.

## Retained Failed Attempts

- `GAME-SMOKE/20260608-214807`: direct `DolocAPI.AgentPosition` movement produced player movement but `cameraDistance=0`, so it was rejected.
- `GAME-SMOKE/20260608-220107`: external WASD injection produced input noise and did not produce reliable player movement, so it was rejected.
- `GAME-SMOKE/20260608-221243`: official `CameraController.SetPosition(Vector2)` movement worked, but telemetry was still reading main camera transform and reported `cameraDistance=0`, so telemetry switched to native `CameraController.position2d`.

## Related Records

- CameraView lease rebuild: `docs/updates/2026/20260608-0020-camera-view-lease-rebuild.md`
- Manual QA failure review: `docs/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md`
- Manual QA checklist: `docs/debug/issues/ISSUE-009-20260608-camera-playable-dynamic-qa.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `CAMERA-PLAYABLE`
- Debug index: `docs/debug/INDEX.md`

## Rollback Notes

- Remove the dynamic 4x/2x phases, telemetry CSV, and dynamic screenshot requirements from `CameraSmoke.cs` if this smoke approach is replaced.
- Restore the old pending text in `SmokeHarness.cs` only if CAMERA-PLAYABLE returns to static evidence.
- Remove this update record, the `ISSUE-009` checklist, and the dynamic CAMERA-PLAYABLE smoke-matrix/debug-index entries when rolling back the dynamic validation.
- Do not roll back `ICameraViewApi` lease arbitration or CameraView implementation as part of this smoke-only rollback; those are covered by `20260608-0020`.

## Follow-Up

- A later human play pass can add room/building transition screenshots or notes, but the active 2x/4x 30-second dynamic telemetry requirement is now smoke-verified.
- If future validation needs real keyboard/gamepad input rather than smoke-owned movement, add it as a separate input-harness goal and keep it separate from CameraView implementation.
