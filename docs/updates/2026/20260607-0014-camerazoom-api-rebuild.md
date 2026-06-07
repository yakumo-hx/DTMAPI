# Update 20260607-0014: 0.4.2 CameraZoom API Rebuild

Date: 2026-06-07
Status: implemented

## Source Request

Goal file: `docs/goals/2026/20260607-0002-042-camerazoom-api-rebuild.md`

Short prompt backup: `docs/goals/2026/20260607-0002-042-camerazoom-api-rebuild.goal.txt`

The user requested a fixed `0.4.2` CameraZoom rebuild using the new native-owner API workflow. The goal required starting from method-body review, then rebuilding the experimental public API through GameBridge camera/background/fog/lifecycle owner paths, using the existing ZoomMod only as an API consumer.

## Version Change

- Old controlled version: `0.4.0`
- New controlled version: `0.4.2`
- Version guard result: the earlier `0.4.1` manual-QA handoff is archived and non-active, so the goal was allowed to proceed directly from `0.4.0` to fixed target `0.4.2`.
- Controlled version files changed:
  - `Directory.Build.props`: assembly/file/package version `0.4.0` -> `0.4.2`
  - `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`: `ApiVersion` `0.4.0` -> `0.4.2`
  - `tools/scripts/install-to-game.ps1`: installed-package `MinimumDTMApiVersion` and DTMAPI dependency normalization `0.4.0` -> `0.4.2`

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `Directory.Build.props`
- `tools/scripts/install-to-game.ps1`
- `testmods/ZoomMod/ModEntry.cs`
- `testmods/ZoomMod/manifest.json`
- `testmods/ZoomMod/official-info.json`
- `testmods/ZoomMod/README.md`
- `docs/reviews/api/2026/20260607-0011-camera-zoom-method-body-review.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260607-0014-camerazoom-api-rebuild.md`
- `docs/goals/2026/20260607-0002-042-camerazoom-api-rebuild.md`

## Implementation Notes

- Added the required CameraZoom method-body native-owner review before runtime edits.
- Expanded the experimental `ICameraZoomApi` contract with owner-aware result/state telemetry, requested/clamped/applied scale, camera-controller/background/fog/scanner/lifecycle/UI-scale statuses, current-room snapshot fields, and `GetSnapshot(string uniqueId)`.
- Kept public abstractions free of raw Unity and decompiled Doloc Town types.
- Rebuilt GameBridge zoom application as a transactional owner-path operation:
  - updates `DolocAPI.mainCamera.orthographicSize`;
  - refreshes and repositions `DolocAPI.cameraController`;
  - compensates `DolocAPI.envBackgroundEx` / `BackgroundRenderer` transforms;
  - compensates `DolocAPI.EnvCovariantController` depth-fog controller transforms;
  - refreshes scanners;
  - rolls back when a scale above 1x cannot complete the compensation path;
  - restores vanilla camera/background/fog state on reset, save load, returned-to-title, and lifecycle boundaries.
- Added a Harmony postfix for `DolocAPI.SetEnvCamera` so room/environment-camera transitions can trigger CameraZoom reapply/restore through GameBridge.
- Updated the smoke harness to verify compensation statuses and to wait for before/4x/reset screenshot files instead of treating async `ScreenCapture.CaptureScreenshot` as immediate proof.
- Updated ZoomMod as an ordinary API consumer to display the new applied scale and owner-path statuses; no native-owner fixes live in ZoomMod.

## Known Facts And Rejected Hypotheses

- Native camera size is owned by `DolocAPI.mainCamera`, while camera range/position must be refreshed through `DolocAPI.cameraController.RefreshResolution()` and `SetPosition(Vector2)`.
- Native outdoor background state is owned by `DolocAPI.envBackgroundEx` / `BackgroundRenderer`, entered through `Room.OnEnterRoom()` and `RoomPatch.HandleBackground(...)`.
- Native depth fog is owned by private controllers under `DolocAPI.EnvCovariantController`.
- `DolocAPI.SetEnvCamera(...)` is the lifecycle boundary that can overwrite camera/background/fog state on room or map transitions.
- Rejected direction: marking the rebuild complete by only changing `orthographicSize`.
- Rejected direction: moving native compensation into ZoomMod. ZoomMod remains only the smoke/test consumer of the GameBridge API.
- Rejected scope: arbitrary cross-room panorama rendering. The 0.4.2 API compensates camera/background/fog/lifecycle owner paths and refreshes scanners, but does not claim a room-fit or connected-room render policy.

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Result: Release build and unit tests passed with 0 warnings and 0 errors.
  - Unit tests: `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -SaveSlot 3 -IncludeHookProbe -AutoExerciseZoom -SkipBuild -TimeoutSeconds 300`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260607-072228`
  - Result JSON: `StartupLog=true`, `GameLaunched=true`, `HookProbe=true`, `SaveLoaded=true`, `Zoom=true`, `ProcessExited=true`, `NoFatalInstanceWindow=true`, `ForcedClose=false`
  - Startup/log evidence: `HookProbe GameLaunched OK`; `HookProbe SaveLoaded OK slot=2 isNewGame=False`; `Camera.ZoomEnvironmentLifecycle = experimental`; `Camera.ZoomApi = verified`.
  - Zoom apply evidence: `Camera orthographic size 16.875->67.5 viewScale=4 owner=DTMAPI.ZoomMod cameraController=refreshed=True, positioned=True, positioned background=compensated-background=4 fog=compensated-depth-fog=4; compensated-building-depth-fog=4 scanner=refreshed lifecycle=not-needed uiScale=unchanged reason=smoke max-view`.
  - Room evidence: `room=farm_type1-平地`, `roomBackground=True`, and `status=applied-compensated`.
  - Screenshot evidence: `DTMAPI-evidence/ZOOM-042/20260607-072308/zoom-before.png`, `zoom-4x.png`, `zoom-reset.png`, and `summary.txt`.
  - Visual inspection: `zoom-4x.png` shows the farm background spanning the large 4x view instead of appearing as a small framed rectangle.
  - Report: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260607-072305.zip`
  - Exit checks: `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup.

## Retained Failed Attempts

- `docs/debug/evidence/GAME-SMOKE/20260607-070702`: startup, HookProbe, third-save load, and clean exit succeeded, but CameraZoom failed on the native owner call because `CameraController.SetPosition` expected `UnityEngine.Vector2` and the first implementation passed a `Vector3` value. The runtime rolled back instead of claiming success.
- `docs/debug/evidence/GAME-SMOKE/20260607-071602`: zoom apply/restore passed, but screenshot evidence was incomplete because async `ScreenCapture.CaptureScreenshot` only wrote the reset image before the smoke summary was finalized.

## Evidence Links

- Goal: `docs/goals/2026/20260607-0002-042-camerazoom-api-rebuild.md`
- Method-body review: `docs/reviews/api/2026/20260607-0011-camera-zoom-method-body-review.md`
- Native-owner prior audit: `docs/reviews/api/2026/20260607-0008-native-owner-special-audits/02-camera-zoom.md`
- Closure table: `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/07-all-api-risk-closure-table.md`
- Manual QA note: `docs/reviews/manual-qa/2026/20260607-0001-panorama-background-zoom-note.md`
- API matrix: `docs/api/public-api-matrix.md`
- Hook map: `docs/hook-map/README.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Final third-save smoke: `docs/debug/evidence/GAME-SMOKE/20260607-072228`

## Rollback Notes

- Revert `Directory.Build.props`, `DtmApiRuntime.ApiVersion`, and installer dependency normalization only if rolling back the entire `0.4.2` CameraZoom goal.
- Reverting the API contract also requires removing the GameBridge result/state fields, ZoomMod status display updates, smoke-harness compensation checks, hook-map/API-matrix/smoke-matrix/debug-index updates, and this update record.
- If the `DolocAPI.SetEnvCamera` postfix is removed, CameraZoom should be treated as lifecycle-incomplete again until another environment-camera boundary is verified.

## Follow-Up

- Keep CameraZoom experimental until a second real mod or a future camera/photo API consumes the owner-path contract.
- Full connected-room panorama or room-fit rendering remains a separate API design problem because native `Room.Render/ClearRender` and `Dungeon._RenderNearRooms` are not camera-size-aware.
- Future smoke can add a deliberate room-transition apply/reapply screenshot, but the required 0.4.2 farm 4x background/fog/lifecycle owner-path proof is complete.
