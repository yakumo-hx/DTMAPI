# Camera Hook Map

Last updated: 2026-06-09

## Scope

This focused map covers only the ordinary playable camera view path and the obsolete CameraZoom compatibility wrapper. It does not cover future panorama/photo camera work, background compensation, depth-fog compensation, scanner refresh, or room-range ownership.

## Implementation Owner

- `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraFeature.cs` owns camera API registration and lifecycle entry points.
- `CameraViewService` implements `ICameraViewApi`, owns camera-view lease state and writes only `DolocAPI.mainCamera.orthographicSize`.
- `CameraZoomCompatibilityService` implements obsolete `ICameraZoomApi` by redirecting callers to per-owner `ICameraViewApi` leases.
- `CameraDiagnosticsService` publishes `Camera.ViewApi` and `Camera.ZoomApi` hook statuses.
- `DolocTownExperimentalBridgeApi` no longer implements `ICameraViewApi` or `ICameraZoomApi`.

## Hook: Camera.ViewApi

- Status: experimental.
- Public surface: `ICameraViewApi`, `ICameraViewLease`, `CameraViewRequest`, `CameraViewResult`, `CameraViewState`, and `GetSnapshot(string uniqueId)`.
- Game method/type: `DolocAPI.mainCamera.orthographicSize`.
- Native lifecycle boundary: Harmony Postfix on `DolocAPI.SetEnvCamera(...)` notifies `CameraFeature.NotifyEnvironmentReset(...)` so active leases can reapply orthographic-size-only playable zoom after room/environment camera resets.
- Patch type: GameBridge runtime reflection plus Harmony Postfix on `DolocAPI.SetEnvCamera`; no raw Unity camera or decompiled game type is exposed through the public API.
- Behavior: highest priority active lease wins, with latest update order as tie-breaker. Releasing the active lease falls back to the next active lease or restores vanilla `1x`.
- Boundary: playable zoom must keep native camera follow/range semantics and must not call `CameraController.RefreshResolution`, `CameraController.SetPosition`, `DolocAPI.RefreshScanner`, background compensation, fog compensation, or UI scaling.
- Failure behavior: while the main camera is unavailable, the status is pending and runtime refresh retries the orthographic write.
- Mods/tests depending on it: `DTMAPI.ZoomMod`, smoke harness `-AutoExerciseZoom` / `Smoke.CameraPlayable`.

## Hook: Camera.ZoomApi

- Status: obsolete-compatibility.
- Public surface: `ICameraZoomApi`, `CameraZoomOptions`, `CameraZoomRegisterResult`, `CameraZoomResult`, `CameraZoomState`, and `GetSnapshot(string uniqueId)`.
- Implementation: compatibility wrapper over `ICameraViewApi` through `CameraZoomCompatibilityService`.
- Boundary: obsolete `CameraZoomOptions.RefreshCameraController`, `CompensateBackground`, `CompensateDepthFog`, and `RefreshScanners` are ignored for playable zoom.
- Failure behavior: missing/disabled owners return compatibility state from the underlying CameraView lease path; new code should use `ICameraViewApi`.

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseZoom -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260609-015803`.
  - `result.json`: `RunStatus=Passed`, `StartupLog=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `Zoom=Passed`, `GameLaunched=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`.
  - HookProbe log: `HookProbe GameLaunched OK` and `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
  - Camera summary: `DTMAPI-evidence/CAMERA-PLAYABLE/20260609-015844/summary.txt`.
  - 4x dynamic: 30s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=67.5-67.5`, active owner `DTMAPI.ZoomMod`.
  - 2x dynamic fallback: 30.003s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=33.75-33.75`, active owner `DTMAPI.CameraViewCompetingSmoke`.
  - Reset: lower-priority lease release restored vanilla `1x`.
  - Boundary evidence: summary and logs retain `nativeRefresh=not-called-playable` and `uiScale=unchanged`.
  - Report zip: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260609-015841.zip`.

## Related Records

- `docs/updates/2026/20260608-0020-camera-view-lease-rebuild.md`
- `docs/updates/2026/20260608-0026-camera-playable-dynamic-smoke.md`
- `docs/updates/2026/20260609-0004-camera-feature-split.md`
- `docs/debug/regressions/smoke-matrix.md` row `CAMERA-PLAYABLE`
- `docs/api/public-api-matrix.md` Camera rows
- `docs/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md`
