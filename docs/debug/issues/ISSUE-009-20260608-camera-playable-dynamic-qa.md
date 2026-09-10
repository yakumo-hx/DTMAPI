# ISSUE-009: CameraPlayable Dynamic QA Evidence

- State: `verified`
- Current boundary: Dynamic CameraView smoke evidence recorded.

## Current Status

- Opened: 2026-06-08 +08:00
- Target: DTMAPI 0.4.2 CameraView lease rebuild follow-up
- Source: active goal to add CAMERA-PLAYABLE dynamic validation without changing CameraView implementation
- Boundary: dynamic CAMERA-PLAYABLE validation only; no CameraView implementation changes

## Known Facts Before Changes

- `GAME-SMOKE/20260608-150914` proved CameraView lease acquisition, 4x winner, 2x fallback, reset, `nativeRefresh=not-called-playable`, `uiScale=unchanged`, and clean exit.
- That evidence only had before/4x/reset static screenshots, so it was not enough to prove playable camera behavior during sustained movement.
- `docs/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md` rejected the old CameraZoom native refresh/background/scanner/fog path for live playable zoom.
- CameraView implementation remains orthographic-size-only and must not be expanded with native refresh, scanner, background, fog, or panorama compensation paths for this goal.

## Rejected Hypotheses And Shortcuts

- Do not accept a single `zoom-4x.png` as passing evidence.
- Direct `DolocAPI.AgentPosition` movement alone is not enough: `GAME-SMOKE/20260608-214807` moved the player but reported `cameraDistance=0`.
- External WASD injection is not enough: `GAME-SMOKE/20260608-220107` produced input logs but did not produce usable player movement.
- Reading only Unity main-camera transform is insufficient for native camera-owner movement: `GAME-SMOKE/20260608-221243` still reported `cameraDistance=0` before telemetry switched to `CameraController.position2d`.
- Do not change `ICameraViewApi` / CameraView implementation to call `CameraController.RefreshResolution`, `DolocAPI.RefreshScanner`, background compensation, fog compensation, or panorama/range refresh paths.

## Manual QA Checklist

### 1. 4x dynamic playable evidence

- Required: at least 30 seconds of movement at 4x with player position, camera position, orthographic size, active lease, and room recorded.
- Passing evidence: `GAME-SMOKE/20260608-222542` plus `D:\Steam\steamapps\common\Doloc Town\DTMAPI\evidence\CAMERA-PLAYABLE\20260608-222622`.
- Result: 4x ran 30.016s with 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=67.5-67.5`, active owner `DTMAPI.ZoomMod`, and room `farm_type1-平地`.
- Screenshots: `dynamic-4x-start.png`, `dynamic-4x-mid.png`, and `dynamic-4x-end.png`.
- Telemetry: `camera-playable-dynamic-telemetry.csv`.

### 2. 2x fallback dynamic playable evidence

- Required: after releasing the high-priority 4x lease, prove the lower-priority 2x lease remains active during another 30 seconds of movement.
- Passing evidence: same final run and CAMERA-PLAYABLE evidence folder.
- Result: 2x ran 30.005s with 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=33.75-33.75`, active owner `DTMAPI.CameraViewCompetingSmoke`, and room `farm_type1-平地`.
- Screenshots: `dynamic-2x-start.png`, `dynamic-2x-mid.png`, and `dynamic-2x-end.png`.
- Telemetry: `camera-playable-dynamic-telemetry.csv`.

### 3. Non-static screenshot evidence

- Required: passing evidence must not be a single 4x still image.
- Passing evidence: six dynamic screenshots plus CSV telemetry and `summary.txt`.
- Note: `zoom-before.png`, `zoom-4x.png`, and `zoom-reset.png` remain in the evidence folder only as legacy context and reset/arbitration context.

### 4. Lease, reset, and exit cleanup

- Required: 4x owner, 2x fallback owner, reset to 1x, HookProbe save-load evidence, clean exit, and no fatal instance popup.
- Passing evidence: final result has `RunStatus=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `Zoom=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Logs show `Smoke.CameraPlayable = verified`, `Smoke.Zoom = verified`, and `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
- Cleanup files say no leftover `DolocTown.exe` and no fatal instance popup.

## Required Evidence

- Release build and unit tests.
- DirectExe third-save smoke with `-IncludeHookProbe -AutoExerciseZoom`.
- Telemetry fields for player position, camera position, orthographic size, current/applied scale, active owner, active lease, room ID/title, `nativeRefresh`, `uiScale`, and arbitration.
- Start/mid/end screenshots for both 4x and 2x dynamic phases.
- Clean exit process and fatal-window checks.

## Attempts

- `GAME-SMOKE/20260608-214807`: failed because direct `AgentPosition` movement did not move the measured camera position.
- `GAME-SMOKE/20260608-220107`: failed because external WASD input did not create reliable player movement and introduced window/input noise.
- `GAME-SMOKE/20260608-221243`: failed because telemetry read the wrong camera position source for the official camera controller movement.
- `GAME-SMOKE/20260608-222542`: passed after telemetry read `CameraController.position2d` and the smoke-only movement driver set both `DolocAPI.AgentPosition` and official `CameraController.SetPosition(Vector2)`.

## Result

Smoke-verified. The active goal's 2x/4x 30-second dynamic evidence is satisfied without changing CameraView implementation. A later human play pass can still add room/building transition acceptance evidence, but it is not a blocker for this dynamic smoke goal.

## 2026-07-19 Batch 5 Reentrant Fixture Regression And Recovery

- The user reported that the then-current Batch 5 Zoom QA route could place the player below visible farm ground, inside the map-boundary mask. The scoped correction changed only the QA target position; it did not add background/fog/panorama synchronization or change the Experimental `ICameraViewApi` contract.
- `GAME-SMOKE/20260719-024242` then exposed a separate deterministic optional-QA regression. The CameraPlayable G4 state machine acquired low/high leases and synchronously pumped runtime automation before committing its stage; re-entry repeated the same acquisitions, produced 585 alternating lease-acquired lines, and ended in a real `0xc0000005` process crash. Root-cause ownership and rejected hypotheses are frozen in [CameraPlayable Reentrant Lease Crash Review](../../archive/reviews/code/2026/20260719-0001-batch5-camera-playable-reentrant-lease-crash.md). This was P0 for the QA fixture, not proof of an ordinary CameraView/Zoom P0.
- The fixture now commits a non-reentrant stage before any lease or callback capable of re-entry, retains each handle once, and the GameBridge runtime-automation pump rejects synchronous nested updater entry while exposing a bounded bypass counter. Focused unit coverage re-enters from the automation callback and proves one low/high pair plus terminal release/root-zero cleanup.
- Final `GAME-SMOKE/20260719-031358` passed `Zoom`, `ZoomOwnerLifetime`, `GameBridgeZoomCleanupHealth`, `CameraPlayableEvidenceFiles`, QA lifecycle/cleanup, owner-lifetime close cleanup, official profile/Author state restoration, no-fatal and process-exit gates. The seven CameraPlayable screenshots cover before, 4x, movement start/mid/end, 2x fallback and reset; visual review keeps the player on visible ground beside the large barn. The black map-boundary mask remains visible at wide zoom but does not obscure this acceptance sequence.
- Current status remains `smoke-verified`. This bounded correction neither promotes CameraView from Experimental nor closes ISSUE-010/ISSUE-011.
