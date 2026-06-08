# 20260608-0020 CameraView Lease Rebuild

## Metadata

- Update ID: 20260608-0020
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Rebuild playable camera zoom as lease-based ICameraViewApi"
- Owner: Codex

## Summary

- Added lease-based `ICameraViewApi` and `ICameraViewLease` as the ordinary playable camera view API.
- Added DTMAPI-side lease arbitration for multiple camera-view requests: highest priority wins, then latest update order.
- Marked `ICameraZoomApi` obsolete and redirected compatibility callers through per-owner CameraView leases.
- Kept `IPanoramaCameraApi` as an interface draft only; panorama/background/fog/range compensation is not mixed into playable zoom.
- Changed ZoomMod and the smoke harness to consume `ICameraViewApi`.
- Rebuilt playable zoom to write only the gameplay camera orthographic size. It intentionally does not call `CameraController.RefreshResolution`, `CameraController.SetPosition`, `DolocAPI.RefreshScanner`, background compensation, or fog compensation.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Diagnostics/DolocTownExperimentalBridgeApi.Diagnostics.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CameraZoom/DolocTownExperimentalBridgeApi.CameraZoom.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/DolocTownGameBridge.Smoke.cs`
- `testmods/ZoomMod/ModEntry.cs`
- `testmods/ZoomMod/README.md`
- `testmods/README.md`
- `tools/scripts/run-game-smoke.ps1`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/2026/20260608-0020-camera-view-lease-rebuild.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1`
  - Release build/test pass completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -SaveSlot 3 -IncludeHookProbe -AutoExerciseZoom -SkipBuild -TimeoutSeconds 300`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-150914`
- Startup evidence: `result.json` records `StartupLog=true`.
- HookProbe evidence: `HookProbe=true`, `SaveLoaded=true`, and log line `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
- Contract evidence: logs show `Camera.ViewApi = contract` and `Camera.ZoomApi = obsolete-compatibility`.
- CameraView max evidence: logs show high-priority `DTMAPI.ZoomMod` lease at 4x, `appliedSize=67.5`, `arbitration=active-highest-priority-latest`, `nativeRefresh=not-called-playable`, and `uiScale=unchanged`.
- CameraView arbitration evidence: logs and `CAMERA-PLAYABLE/20260608-150952/summary.txt` show fallback from 4x to the lower-priority `DTMAPI.CameraViewCompetingSmoke` 2x lease after releasing the high-priority lease.
- CameraView restore evidence: logs and summary show reset from 2x to 1x after releasing the lower-priority lease.
- Smoke status evidence: logs show `Smoke.CameraPlayable = verified` and `Smoke.Zoom = verified`; `result.json` records `Zoom=true`.
- Screenshot evidence: `D:\Steam\steamapps\common\Doloc Town\DTMAPI\evidence\CAMERA-PLAYABLE\20260608-150952` contains `zoom-before.png`, `zoom-4x.png`, `zoom-reset.png`, and `summary.txt`.
- Exit evidence: `result.json` records `ProcessExited=true`, `ForcedClose=false`, and `NoFatalInstanceWindow=true`; `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup.

## Related Records

- Manual QA failure review: `docs/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md`
- Method-body review: `docs/reviews/api/2026/20260607-0011-camera-zoom-method-body-review.md`
- Prior failed CameraZoom rebuild evidence: `docs/updates/2026/20260607-0014-camerazoom-api-rebuild.md`
- Prior mechanical CameraZoom split: `docs/updates/2026/20260608-0007-gamebridge-camerazoom-feature-split.md`
- Hook map: `docs/hook-map/README.md` rows `Camera.ViewApi` and `Camera.ZoomApi`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `CAMERA-PLAYABLE`
- API matrix: `docs/api/public-api-matrix.md` Camera rows

## Rollback Notes

- Remove `ICameraViewApi`, `ICameraViewLease`, `CameraViewRequest`, `CameraViewResult`, and `CameraViewState` from `ExperimentalGameBridge.cs`.
- Revert the CameraZoom feature file to the previous `ICameraZoomApi` implementation only if also reverting the obsolete compatibility redirect and smoke harness.
- Revert ZoomMod to the old `ICameraZoomApi` consumer only if the public matrix is restored to the old failed CameraZoom state.
- Remove `CAMERA-PLAYABLE` smoke/manual row and this update record if rolling back the rebuild.

## Follow-Up

- Add manual QA notes after a real play pass covers sustained 4x movement, room transitions, player-follow behavior, and absence of fixed-center/segmented background behavior.
- Keep any future panorama/photo camera work on `IPanoramaCameraApi` instead of extending the playable `ICameraViewApi` path with background/fog/range compensation.
