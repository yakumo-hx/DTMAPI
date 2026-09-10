# 20260609-0008 GameBridge Feature Host Hardening

## Status

Verified.

## Source

- Goal: harden the GameBridge Feature Host without changing Camera behavior.
- Requirements: add `IGameBridgeFeature.Id`; use one safe wrapper for all feature dispatch; record DTMAPI diagnostics if `Update`, `SaveLoaded`, `ReturnedToTitle`, or `EnvironmentReset` throws; do not block other features; set `CameraFeature.Id = "Camera"`; add `Feature.Camera` hook/status output; do not modify `CameraViewService` zoom behavior; do not split a new feature; run build/test/Camera smoke.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/IGameBridgeFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `docs/hook-map/README.md`
- `docs/hook-map/focused/Camera.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0008-gamebridge-feature-host-hardening.md`

## Summary

- Added a stable internal `Id` to `IGameBridgeFeature`.
- Set `CameraFeature.Id` to `Camera`.
- Routed `RegisterApis`, `PublishHookStatuses`, `Update`, `SaveLoaded`, `ReturnedToTitle`, and `EnvironmentReset` through one `DolocTownGameBridge` safe-dispatch wrapper.
- Successful dispatches publish `Feature.<Id> = ready`; Camera now emits `Feature.Camera`.
- Dispatch failures record `DTMAPI.GameBridge.Feature.<Id>` diagnostics, log the exception type/message, mark `Feature.<Id>` failed, and continue dispatching later features.
- `CameraViewService` and Camera zoom behavior were not changed.
- No new feature was split.

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseZoom -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260609-100218`
  - `result.json`: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `Zoom=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
  - Exit check: `process-check.txt` says no `DolocTown.exe` process was found.

## Evidence

- Startup log: `docs/debug/evidence/GAME-SMOKE/20260609-100218/DTMAPI-latest.log`
- HookProbe lines:
  - `HookProbe GameLaunched OK`
  - `HookProbe SaveLoaded OK slot=2 isNewGame=False`
- Feature-host status:
  - `Feature.Camera = ready` for `PublishHookStatuses`, `Update`, `ReturnedToTitle`, `SaveLoaded`, and `EnvironmentReset`.
  - Report summary records `Errors: 0` and `HOOK Feature.Camera: ready`.
- Camera smoke:
  - `Camera.ViewApi = contract`
  - `Camera.ZoomApi = obsolete-compatibility`
  - `Smoke.CameraPlayable = verified`
  - `Smoke.Zoom = verified`
- Camera evidence: `docs/debug/evidence/GAME-SMOKE/20260609-100218/DTMAPI-evidence/CAMERA-PLAYABLE/20260609-100258`
  - 4x dynamic: 30.249s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=67.5-67.5`, active owner `DTMAPI.ZoomMod`.
  - 2x fallback: 30.241s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=33.75-33.75`, active owner `DTMAPI.CameraViewCompetingSmoke`.
  - Reset restored vanilla `1x`.
- Report zip: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260609-100255.zip`

## Related Records

- `docs/hook-map/README.md` entries `Feature.Camera`, `Camera.ViewApi`, and `Camera.ZoomApi`
- `docs/hook-map/focused/Camera.md`
- `docs/debug/regressions/smoke-matrix.md` row `GAMEBRIDGE-FEATURE-HOST-HARDENING-20260609`
- `docs/updates/2026/20260609-0006-gamebridge-feature-host-update.md`

## Rollback Notes

- If rolling back, remove `IGameBridgeFeature.Id` and the safe-dispatch wrapper, restore the previous direct feature dispatch calls, and remove `Feature.Camera` status docs.
- Re-run Release build/test and DirectExe third-save CameraPlayable smoke because rollback changes lifecycle/runtime dispatch.

## Follow-Up

- Future non-Camera GameBridge features should implement a stable `Id` and rely on the shared safe-dispatch wrapper instead of adding bespoke lifecycle calls.
