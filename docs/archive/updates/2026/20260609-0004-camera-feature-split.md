# 20260609-0004 Camera Feature Split

## Metadata

- Update ID: 20260609-0004
- Date: 2026-06-09
- Status: verified
- Source: Active goal: split Camera GameBridge code out of `DolocTownExperimentalBridgeApi` into an independent `CameraFeature` without changing behavior.
- Owner: Codex

## Summary

- Added `Features/Camera/CameraFeature.cs` as the camera feature owner.
- Split CameraView lease behavior into `CameraViewService`.
- Split obsolete CameraZoom compatibility behavior into `CameraZoomCompatibilityService`.
- Split Camera hook-status publication into `CameraDiagnosticsService`.
- Registered `ICameraViewApi` and obsolete `ICameraZoomApi` through `CameraFeature` instead of `DolocTownExperimentalBridgeApi`.
- Kept playable camera behavior unchanged: lease arbitration, obsolete wrapper redirect, orthographic-size-only writes, `nativeRefresh=not-called-playable`, and `uiScale=unchanged`.
- Did not edit Fishing, ActionSpeed, EquipmentSlots, or SaveSlots feature implementation files.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraViewService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraZoomCompatibilityService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraDiagnosticsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CameraZoom/DolocTownExperimentalBridgeApi.CameraZoom.cs` removed
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/CameraSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/AutoFishingSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/ContentSmoke.cs`
- `docs/hook-map/README.md`
- `docs/hook-map/focused/Camera.md`
- `docs/updates/2026/20260609-0004-camera-feature-split.md`
- `docs/updates/INDEX.md`

## Package Artifacts

- Source package: `E:\Python_project\DTMAPI-open-source-codex-refactor-camera-feature`
- Source package zip: `E:\Python_project\DTMAPI-open-source-codex-refactor-camera-feature.zip`
- Audit package: `E:\Python_project\DTMAPI-audit-package-codex-refactor-camera-feature`
- Audit package zip: `E:\Python_project\DTMAPI-audit-package-codex-refactor-camera-feature.zip`

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseZoom -SaveSlot 3 -TimeoutSeconds 240`
- Passed: source package build from `E:\Python_project\DTMAPI-open-source-codex-refactor-camera-feature` using `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: source and audit package exclusion checks.
  - No `.git`, `.tools`, `bin`, `obj`, `decompiled`, or `input` directories in package directories or zips.
  - No DLL/EXE/PDB/RAR/7Z/NuGet artifacts in package directories or zips.
  - Audit package allows only the required complete report zip under `audit/report/`.
  - Source/share-area local path scan passed; audit evidence logs intentionally retain runtime game paths as evidence text.

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260609-015803`
- `result.json`: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `Zoom=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`.
- Startup log: `DTMAPI-latest.log` collected in the smoke evidence folder.
- HookProbe log: `HookProbe GameLaunched OK`; `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
- Camera status evidence: logs show `Camera.ViewApi = contract`, `Camera.ZoomApi = obsolete-compatibility`, `Smoke.CameraPlayable = verified`, and `Smoke.Zoom = verified`.
- Camera summary: `DTMAPI-evidence/CAMERA-PLAYABLE/20260609-015844/summary.txt`.
- 4x dynamic evidence: 30s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=67.5-67.5`, active owner `DTMAPI.ZoomMod`.
- 2x dynamic fallback evidence: 30.003s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=33.75-33.75`, active owner `DTMAPI.CameraViewCompetingSmoke`.
- Restore evidence: release of the lower-priority lease restored vanilla `1x`.
- Boundary evidence: summary and logs retain `nativeRefresh=not-called-playable` and `uiScale=unchanged`.
- Exit evidence: `process-check.txt` says no `DolocTown.exe` process was found.
- Complete report zip: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260609-015841.zip`.
- Packaged report zip: `E:\Python_project\DTMAPI-audit-package-codex-refactor-camera-feature\audit\report\DTMAPI-CameraFeature-20260609-015841-report.zip`.
  - Contains `DTMAPI-latest.log`, `HookProbe-lines.txt`, `Unity-Player.log`, `BepInEx-LogOutput.log`, `process-check.txt`, `result.json`, startup analysis, and CameraPlayable summary/telemetry.
- Packaged smoke evidence: `E:\Python_project\DTMAPI-audit-package-codex-refactor-camera-feature\audit\evidence\GAME-SMOKE\20260609-015803`.
- Source package file count: 236 files.
- Audit package file count: 319 files.

## Related Records

- CameraView lease rebuild: `docs/updates/2026/20260608-0020-camera-view-lease-rebuild.md`
- CameraPlayable dynamic smoke: `docs/updates/2026/20260608-0026-camera-playable-dynamic-smoke.md`
- Hook map: `docs/hook-map/README.md` and `docs/hook-map/focused/Camera.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `CAMERA-PLAYABLE`
- API matrix: `docs/api/public-api-matrix.md` Camera rows
- Manual QA failure review: `docs/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md`

## Rollback Notes

- Move `CameraViewService` and `CameraZoomCompatibilityService` behavior back into the former `DolocTownExperimentalBridgeApi.CameraZoom.cs` partial only if also restoring `DolocTownExperimentalBridgeApi` as the direct `ICameraViewApi`/`ICameraZoomApi` implementation.
- Restore Camera API registration in `DolocTownGameBridge.RegisterExperimentalApis()` to use `experimentalApi`.
- Restore Camera lifecycle callbacks in `DolocTownHookCallbacks` to call the experimental API.
- Remove `docs/hook-map/focused/Camera.md`, this update record, and the index row if rolling the split back.

## Follow-Up

- None required for the CameraFeature split package. Future package refreshes should use the same source/share-area path scan and audit-report-zip exception.
