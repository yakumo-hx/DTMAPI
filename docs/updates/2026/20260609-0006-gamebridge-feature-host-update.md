# 20260609-0006 GameBridge Feature Host Update

## Metadata

- Update ID: 20260609-0006
- Date: 2026-06-09
- Status: verified
- Source: Active goal to establish a GameBridge feature host, move production `Update` out of `SmokeHarness`, and keep Camera behavior unchanged.
- Owner: Codex

## Summary

- Added internal `IGameBridgeFeature` as the common GameBridge feature contract.
- Made `CameraFeature` implement `IGameBridgeFeature`.
- Added a `DolocTownGameBridge` feature list and unified host dispatch for feature API registration, hook-status publication, runtime update, save-loaded, returned-to-title, and environment-reset boundaries.
- Moved production `public void Update()` into `DolocTownGameBridge.Update.cs`.
- Left `SmokeHarness.cs` with `SmokeUpdate()` and smoke-case scheduling only.
- Preserved CameraView behavior: lease arbitration, orthographic-size-only writes, obsolete CameraZoom redirect, `nativeRefresh=not-called-playable`, and `uiScale=unchanged`.
- Did not edit `CameraViewService`, FishingAutomation, ActionSpeed, EquipmentSlots, or SaveSlots behavior files.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Update.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/IGameBridgeFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/hook-map/focused/Camera.md`
- `docs/updates/2026/20260609-0006-gamebridge-feature-host-update.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseZoom -SaveSlot 3 -TimeoutSeconds 240`.
- Passed: code audit confirmed the production `DolocTownGameBridge.Update()` entry lives in `DolocTownGameBridge.Update.cs`, `SmokeHarness.cs` owns `SmokeUpdate()` only, and the old direct Camera lifecycle method names are gone.
- Passed: behavior-scope audit found no changes to `CameraViewService`, FishingAutomation, ActionSpeed, EquipmentSlots, or SaveSlots implementation files.

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260609-031302`
- `result.json`: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `Zoom=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`.
- Startup log: `DTMAPI-latest.log` collected in the smoke evidence folder.
- HookProbe log: `HookProbe GameLaunched OK`; `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
- Camera status evidence: logs show `Camera.ViewApi = vanilla/verified`, `Camera.ZoomApi = obsolete-compatibility`, `Smoke.CameraPlayable = verified`, and `Smoke.Zoom = verified`.
- Camera summary: `DTMAPI-evidence/CAMERA-PLAYABLE/20260609-031342/summary.txt`.
- 4x dynamic evidence: 30.002s, 28 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=67.5-67.5`, active owner `DTMAPI.ZoomMod`.
- 2x dynamic fallback evidence: 30.236s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=33.75-33.75`, active owner `DTMAPI.CameraViewCompetingSmoke`.
- Restore evidence: release of the lower-priority lease restored vanilla `1x`.
- Boundary evidence: summary and logs retain `nativeRefresh=not-called-playable` and `uiScale=unchanged` across runtime refresh, SaveLoaded, ReturnedToTitle, and `DolocAPI.SetEnvCamera` environment reset.
- Exit evidence: `process-check.txt` says no `DolocTown.exe` process was found.
- Complete report zip: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260609-031339.zip`.

## Related Records

- Camera feature split: `docs/updates/2026/20260609-0004-camera-feature-split.md`
- Camera merge to Refactor: `docs/updates/2026/20260609-0005-camera-feature-merge-refactor.md`
- Hook map: `docs/hook-map/README.md` and `docs/hook-map/focused/Camera.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `GAMEBRIDGE-FEATURE-HOST-20260609`

## Rollback Notes

- Remove `IGameBridgeFeature` and `DolocTownGameBridge.Update.cs` only if also restoring the former direct `CameraFeature` call sites and the former `SmokeHarness.Update()` production entry.
- If rolling back, re-run build/test and CameraPlayable smoke because the rollback changes runtime lifecycle dispatch.

## Follow-Up

- Future feature splits can implement `IGameBridgeFeature` and be added to `EnsureGameBridgeFeatures()` once their API registration, hook status, runtime update, and lifecycle boundaries are ready for host dispatch.
