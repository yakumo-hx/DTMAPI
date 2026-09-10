# 20260609-0011 CameraPlayable Smoke Case File

## Status

Verified.

## Source

- Goal: mechanically split the Camera playable smoke case into `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/CameraPlayableSmokeCase.cs`.
- Requirements:
  - Move Camera playable smoke code out of the old smoke case file path.
  - Do not change `result.json` fields.
  - Do not change screenshot or evidence file names.
  - Do not change `CameraViewService`.
  - Run build/test/Camera smoke.

## Known Facts And Rejected Directions

- The current refactor layout had already moved the CameraPlayable implementation out of `SmokeHarness.cs` into `Smoke/CameraSmoke.cs`; `SmokeHarness.cs` kept only scheduler calls and pending status setup for `AutoExerciseZoom`.
- This update moves that existing CameraPlayable smoke partial to the requested case namespace path: `Smoke/Cases/CameraPlayableSmokeCase.cs`.
- Content hash check proved the new file content is identical to the previous `Smoke/CameraSmoke.cs` content from `HEAD`.
- Rejected: changing CameraView behavior, smoke status strings, result mapping, screenshot names, dynamic telemetry schema, or evidence directory names.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Smoke/CameraSmoke.cs` removed from the current layout.
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/CameraPlayableSmokeCase.cs` added with identical content.
- `docs/hook-map/README.md`
- `docs/hook-map/focused/Camera.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0011-camera-playable-smoke-case-file.md`

## Summary

- Moved the CameraPlayable/AutoExerciseZoom smoke partial into `Smoke/Cases/CameraPlayableSmokeCase.cs`.
- Kept `SmokeHarness.cs` as the scheduler entry that calls `TryExerciseZoomForSmoke()`.
- Kept all CameraPlayable smoke method bodies, nested smoke classes, log strings, telemetry CSV header, and screenshot path strings unchanged.
- Confirmed `CameraViewService.cs` was not changed.
- Confirmed the smoke output still uses schema v2 result fields and the same screenshot/evidence filenames: `zoom-before.png`, `zoom-4x.png`, `zoom-reset.png`, `camera-playable-dynamic-telemetry.csv`, `dynamic-4x-start.png`, `dynamic-4x-mid.png`, `dynamic-4x-end.png`, `dynamic-2x-start.png`, `dynamic-2x-mid.png`, and `dynamic-2x-end.png`.

## Validation

- Passed: content identity check
  - `HEAD:src/DTMAPI.GameBridge.DolocTown/Smoke/CameraSmoke.cs` hash: `42af01264d82544034424d419c3b886d2dd9114a`
  - `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/CameraPlayableSmokeCase.cs` hash: `42af01264d82544034424d419c3b886d2dd9114a`
- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseZoom -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260609-110528`
  - `result.json`: `SchemaVersion=2`, `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `Zoom=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
  - Exit check: `process-check.txt` says no `DolocTown.exe` process was found.
  - Fatal popup check: `fatal-window-check.txt` says no fatal instance popup was found.
- Passed: `git diff -- src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraViewService.cs`
  - No diff.
- Passed: `git diff --check -- src/DTMAPI.GameBridge.DolocTown/Smoke docs/hook-map docs/debug/regressions/smoke-matrix.md docs/updates`
  - Only existing CRLF normalization warnings were reported.

## Evidence

- Startup log: `docs/debug/evidence/GAME-SMOKE/20260609-110528/DTMAPI-latest.log`
- HookProbe lines:
  - `HookProbe GameLaunched OK`
  - `HookProbe SaveLoaded OK slot=2 isNewGame=False`
- Hook/status lines:
  - `Camera.ViewEnvironmentLifecycle = experimental`
  - `Feature.Camera = ready`
  - `Camera.ViewApi = contract`
  - `Camera.ZoomApi = obsolete-compatibility`
  - `Smoke.CameraPlayable = verified`
  - `Smoke.Zoom = verified`
- Camera evidence: `docs/debug/evidence/GAME-SMOKE/20260609-110528/DTMAPI-evidence/CAMERA-PLAYABLE/20260609-110612`
  - `summary.txt`
  - `camera-playable-dynamic-telemetry.csv`
  - `zoom-before.png`
  - `zoom-4x.png`
  - `zoom-reset.png`
  - `dynamic-4x-start.png`, `dynamic-4x-mid.png`, `dynamic-4x-end.png`
  - `dynamic-2x-start.png`, `dynamic-2x-mid.png`, `dynamic-2x-end.png`
- Dynamic telemetry:
  - 4x dynamic: 30.247s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=67.5-67.5`, active owner `DTMAPI.ZoomMod`.
  - 2x fallback: 30.012s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=33.75-33.75`, active owner `DTMAPI.CameraViewCompetingSmoke`.
  - Reset restored vanilla `1x`.
  - Boundary fields remain `nativeRefresh=not-called-playable` and `uiScale=unchanged`.
- Report zip: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260609-110608.zip`

## Related Records

- `docs/hook-map/README.md` entries `Feature.Camera`, `Camera.ViewApi`, and `Camera.ZoomApi`
- `docs/hook-map/focused/Camera.md`
- `docs/debug/regressions/smoke-matrix.md` row `CAMERA-PLAYABLE-SMOKE-CASE-SPLIT-20260609`
- `docs/updates/2026/20260608-0025-gamebridge-smoke-case-split.md`
- `docs/updates/2026/20260608-0026-camera-playable-dynamic-smoke.md`
- `docs/updates/2026/20260609-0010-camera-setenvcamera-hook-owner.md`

## Rollback Notes

- If this case-folder layout complicates follow-up work, move `Smoke/Cases/CameraPlayableSmokeCase.cs` back to `Smoke/CameraSmoke.cs` without changing content.
- Re-run Release build/test and DirectExe third-save CameraPlayable smoke because the SDK compile glob and smoke evidence path must be revalidated after any file move.

## Follow-Up

- Keep future CameraPlayable smoke behavior changes separate from mechanical file moves so result/schema and evidence filename changes remain easy to audit.
