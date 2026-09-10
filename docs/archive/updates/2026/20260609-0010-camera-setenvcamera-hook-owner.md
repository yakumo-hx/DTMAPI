# 20260609-0010 Camera SetEnvCamera Hook Owner

## Status

Verified.

## Source

- Goal: move Camera SetEnvCamera hook ownership into `CameraFeature`.
- Requirements:
  - Do not change the patch target, callback, or hook status text core meaning.
  - `CameraFeature` owns `cameraViewSetEnvCameraPatched`.
  - `DolocTownGameBridge` no longer stores `cameraZoomSetEnvCameraPatched`.
  - The Camera portion of `AllHookTargetsReady` remains correct.
  - `run-game-smoke.ps1 -AutoExerciseZoom` passes.

## Known Facts And Rejected Directions

- Native owner review identifies `DolocAPI.SetEnvCamera(Vector2, Vector2, bool, bool, bool)` as the room/environment camera reset boundary.
- The callback remains `DolocTownHookCallbacks.DolocApiSetEnvCameraPostfix`, which dispatches `EnvironmentReset` to GameBridge features.
- This change does not modify `CameraViewService`, lease arbitration, orthographic-size writes, zoom compatibility, native camera refresh, scanner, background, fog, or UI scale behavior.
- Rejected: moving the callback target, changing CameraView zoom semantics, or resurrecting the old CameraZoom background/fog owner path.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/IGameBridgeFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraDiagnosticsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `docs/hook-map/README.md`
- `docs/hook-map/focused/Camera.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0010-camera-setenvcamera-hook-owner.md`

## Summary

- Added feature-owned `InstallHooks(HarmonyReflectionPatcher patcher)` dispatch to `IGameBridgeFeature`.
- Added `CameraFeature.cameraViewSetEnvCameraPatched` and made `CameraFeature.InstallHooks(...)` install the existing `DolocAPI.SetEnvCamera` postfix.
- Kept the patch target `DolocAPI, Assembly-CSharp::SetEnvCamera`, callback `DolocTownHookCallbacks.DolocApiSetEnvCameraPostfix`, parameter count `5`, and `Camera.ViewEnvironmentLifecycle` status meaning unchanged.
- Removed `cameraZoomSetEnvCameraPatched` from `DolocTownGameBridge`.
- Updated `AllHookTargetsReady` so the Camera readiness condition reads `cameraFeature.CameraViewSetEnvCameraPatched`.
- `CameraDiagnosticsService` now publishes the environment-lifecycle hook status for the Camera feature.

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseZoom -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260609-102529`
  - `result.json`: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `Zoom=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
  - Exit check: `process-check.txt` says no `DolocTown.exe` process was found.
  - Fatal popup check: `fatal-window-check.txt` says no fatal instance popup was found.

## Evidence

- Startup log: `docs/debug/evidence/GAME-SMOKE/20260609-102529/DTMAPI-latest.log`
- Hook installation status:
  - `Camera.ViewEnvironmentLifecycle = experimental`
  - `Feature.Camera = ready. Safe feature host dispatch completed InstallHooks for this GameBridge feature.`
- HookProbe lines:
  - `HookProbe GameLaunched OK`
  - `HookProbe SaveLoaded OK slot=2 isNewGame=False`
- Feature lifecycle status:
  - `Feature.Camera = ready` for `InstallHooks`, `Update`, `ReturnedToTitle`, `SaveLoaded`, and `EnvironmentReset`.
- Camera smoke:
  - `Camera.ViewApi = contract`
  - `Camera.ZoomApi = obsolete-compatibility`
  - `Smoke.CameraPlayable = verified`
  - `Smoke.Zoom = verified`
- Camera evidence: `docs/debug/evidence/GAME-SMOKE/20260609-102529/DTMAPI-evidence/CAMERA-PLAYABLE/20260609-102609`
  - 4x dynamic: 30.015s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=67.5-67.5`, active owner `DTMAPI.ZoomMod`.
  - 2x fallback: 30.004s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=33.75-33.75`, active owner `DTMAPI.CameraViewCompetingSmoke`.
  - Reset restored vanilla `1x`.
  - Boundary fields remain `nativeRefresh=not-called-playable` and `uiScale=unchanged`.
- Report zip: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260609-102606.zip`

## Related Records

- `docs/hook-map/README.md` entries `Feature.Camera`, `Camera.ViewApi`, and `Camera.ZoomApi`
- `docs/hook-map/focused/Camera.md`
- `docs/debug/regressions/smoke-matrix.md` row `CAMERA-HOOK-OWNER-FEATURE-20260609`
- `docs/updates/2026/20260609-0008-gamebridge-feature-host-hardening.md`
- `docs/api/public-api-matrix.md` Camera rows: no status change; `ICameraViewApi` remains Experimental and `ICameraZoomApi` remains Failed / ObsoleteCompatibility.

## Rollback Notes

- If rolling back, restore the old `cameraZoomSetEnvCameraPatched` field and SetEnvCamera patch block in `DolocTownGameBridge`, remove `IGameBridgeFeature.InstallHooks`, and make `AllHookTargetsReady` read the GameBridge field again.
- Re-run Release build/test and DirectExe third-save CameraPlayable smoke because rollback changes Harmony hook installation ownership.

## Follow-Up

- Future GameBridge features can move their hook installation into feature-owned `InstallHooks` methods, but only after preserving hook target/callback/status semantics and adding focused smoke evidence.
