# Camera Hook Map

Last updated: 2026-06-09

## Scope

This focused map covers only the ordinary playable camera view path and the obsolete CameraZoom compatibility wrapper. It does not cover future panorama/photo camera work, background compensation, depth-fog compensation, scanner refresh, or room-range ownership.

## Implementation Owner

- `src/DTMAPI.GameBridge.DolocTown/Features/IGameBridgeFeature.cs` defines the internal feature contract, including stable feature `Id` and feature-owned hook installation.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs` owns the GameBridge feature list and safe-dispatches `RegisterApis`, `PublishHookStatuses`, `InstallHooks`, `Update`, `SaveLoaded`, `ReturnedToTitle`, and `EnvironmentReset`. A feature exception records `DTMAPI.GameBridge.Feature.<Id>` diagnostics, marks `Feature.<Id>` failed, logs the exception type/message, and does not block the next feature.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Update.cs` owns the production `public void Update()` entry and calls `RefreshUiContext()`, `UpdateRuntimeAutomation()`, and `SmokeUpdate()`.
- `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraFeature.cs` owns camera API registration, lifecycle entry points, and `cameraViewSetEnvCameraPatched`; its feature id is `Camera`.
- `CameraViewService` implements `ICameraViewApi`, owns camera-view lease state and writes only `DolocAPI.mainCamera.orthographicSize`.
- `CameraZoomCompatibilityService` implements obsolete `ICameraZoomApi` by redirecting callers to per-owner `ICameraViewApi` leases.
- `CameraDiagnosticsService` publishes `Camera.ViewApi`, `Camera.ZoomApi`, and `Camera.ViewEnvironmentLifecycle` hook statuses.
- `DolocTownExperimentalBridgeApi` no longer implements `ICameraViewApi` or `ICameraZoomApi`.
- `SmokeHarness.cs` owns `SmokeUpdate()` scheduling only; it no longer owns the production GameBridge `Update` method.
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/CameraPlayableSmokeCase.cs` owns the `AutoExerciseZoom` / `Smoke.CameraPlayable` case implementation.

## Hook: Feature.Camera

- Status: ready.
- Public surface: internal DTMAPI diagnostics/status only.
- Implementation: `DolocTownGameBridge` publishes `Feature.Camera` around safe feature-host dispatch; `CameraFeature.Id` is `Camera`, and `InstallHooks` is now part of the feature dispatch path.
- Failure behavior: feature dispatch exceptions are isolated per feature and recorded under `DTMAPI.GameBridge.Feature.Camera`, with `Feature.Camera` marked failed.
- Evidence: `GAME-SMOKE/20260609-110528` logs `Feature.Camera = ready` for `PublishHookStatuses`, `InstallHooks`, `Update`, `ReturnedToTitle`, `SaveLoaded`, and `EnvironmentReset`; `Camera.ViewEnvironmentLifecycle = experimental` is emitted during `InstallHooks`.

## Hook: Camera.ViewApi

- Status: experimental.
- Public surface: `ICameraViewApi`, `ICameraViewLease`, `CameraViewRequest`, `CameraViewResult`, `CameraViewState`, and `GetSnapshot(string uniqueId)`.
- Game method/type: `DolocAPI.mainCamera.orthographicSize`.
- Native lifecycle boundary: `CameraFeature.InstallHooks(...)` installs the Harmony Postfix on `DolocAPI.SetEnvCamera(...)` and records `cameraViewSetEnvCameraPatched`; the callback still notifies `DolocTownGameBridge.NotifyGameBridgeFeaturesEnvironmentReset(...)`, which dispatches to `CameraFeature.EnvironmentReset(...)` so active leases can reapply orthographic-size-only playable zoom after room/environment camera resets.
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
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260609-110528` on `Refactor` after the CameraPlayable smoke case-file move.
  - `result.json`: `RunStatus=Passed`, `StartupLog=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `Zoom=Passed`, `GameLaunched=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`.
  - HookProbe/status log: `HookProbe GameLaunched OK`, `HookProbe SaveLoaded OK slot=2 isNewGame=False`, `Camera.ViewEnvironmentLifecycle = experimental`, and `Feature.Camera = ready`.
  - Camera summary: `DTMAPI-evidence/CAMERA-PLAYABLE/20260609-110612/summary.txt`.
  - 4x dynamic: 30.247s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=67.5-67.5`, active owner `DTMAPI.ZoomMod`.
  - 2x dynamic fallback: 30.012s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=33.75-33.75`, active owner `DTMAPI.CameraViewCompetingSmoke`.
  - Reset: lower-priority lease release restored vanilla `1x`.
  - Boundary evidence: summary and logs retain `nativeRefresh=not-called-playable` and `uiScale=unchanged`.
  - Feature-host evidence: `InstallHooks`, `SaveLoaded`, `ReturnedToTitle`, runtime refresh, and `DolocAPI.SetEnvCamera` route through the GameBridge safe-dispatch feature host into `CameraFeature` while preserving CameraView messages.
  - Case-file evidence: `Smoke/Cases/CameraPlayableSmokeCase.cs` is content-identical to the previous `Smoke/CameraSmoke.cs`; `CameraViewService.cs` has no diff; result schema and screenshot/evidence names are unchanged.
  - Report zip: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260609-110608.zip`.

## Related Records

- `docs/updates/2026/20260608-0020-camera-view-lease-rebuild.md`
- `docs/updates/2026/20260608-0026-camera-playable-dynamic-smoke.md`
- `docs/updates/2026/20260609-0004-camera-feature-split.md`
- `docs/updates/2026/20260609-0005-camera-feature-merge-refactor.md`
- `docs/updates/2026/20260609-0006-gamebridge-feature-host-update.md`
- `docs/updates/2026/20260609-0008-gamebridge-feature-host-hardening.md`
- `docs/updates/2026/20260609-0010-camera-setenvcamera-hook-owner.md`
- `docs/updates/2026/20260609-0011-camera-playable-smoke-case-file.md`
- `docs/debug/regressions/smoke-matrix.md` row `CAMERA-PLAYABLE`
- `docs/api/public-api-matrix.md` Camera rows
- `docs/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md`
