# 20260603-0007 ActionSpeed AutoFishing Smoke

## Source Request

Continue the DTMAPI 0.2.3 manual-QA productization goal from `readme.md`, specifically task C's AutoFishing movement/minigame proof and task D's ActionSpeed strong auto-fill frequency proof.

## Summary

- Extended ActionSpeed smoke config to enable `AutoFillStrong`.
- Added normal/strong cooldown fields to the ActionSpeed auto-fill bridge summary so one native fill evidence line records the effective cooldown and the ordinary-vs-strong comparison.
- Extended AutoFishing smoke config to enable `AutoCompleteMiniGame`, `SkipMiniGame`, and fast animations.
- Added hard smoke evidence for AutoFishing movement cancel: the bridge sends `W`, verifies automation is disabled through the mod's input handler, then re-enables with `F6`.
- Added hard smoke evidence for skip-minigame routing: wait-phase `InstantBite` must route to `AgentStateFishingPull` and publish `Smoke.AutoFishingMiniGameSkip=verified`.
- Updated the smoke harness result/failure gates so movement cancel and minigame skip cannot silently pass as old wait-phase-only evidence.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `tools/scripts/run-game-smoke.ps1`
- `readme.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\scripts\build.ps1 -Configuration Release`
- Result: all projects and `DTMAPI.UnitTests` built with `0` warnings and `0` errors.
- Passed: `tools/scripts/run-game-smoke.ps1 -UseSteam -SaveSlot 3 -SkipBuild -AutoExerciseActionSpeedInteraction -AutoExitAfterSecondsOverride 95 -TimeoutSeconds 260`
- Result: third-save Steam smoke passed with `ActionSpeedInteraction=true`, clean exit, and no fatal popup.
- Passed: `tools/scripts/run-game-smoke.ps1 -UseSteam -SaveSlot 3 -SkipBuild -AutoPressAutoFishingHotkey -AutoExerciseAutoFishingPhase -AutoExitAfterSecondsOverride 95 -TimeoutSeconds 260`
- Result: third-save Steam smoke passed with `AutoFishingInputLog=true`, `AutoFishingMovementCancel=true`, `AutoFishingPhase=true`, `AutoFishingMiniGameSkip=true`, clean exit, and no fatal popup.

## Evidence Links

- ActionSpeed smoke: `docs/debug/evidence/GAME-SMOKE/20260603-041950`.
- ActionSpeed logs: `DTMAPI-latest.log` records `ActionSpeed auto-fill invoked native ItemBottle.UseAsItem ... strong=True, cooldownSeconds=0.08, normalCooldownSeconds=0.1, strongCooldownSeconds=0.08`.
- ActionSpeed result: `result.json` has `ActionSpeedInteraction=true`, `ProcessExited=true`, and `NoFatalInstanceWindow=true`.
- AutoFishing smoke: `docs/debug/evidence/GAME-SMOKE/20260603-042437`.
- AutoFishing logs: `DTMAPI-latest.log` records external `Input F6`, `manual-move W`, `Smoke.AutoFishingMovementCancel = verified`, native `BodyController.UseFishRod` auto-cast, Pull animation speed `multiplier=3`, and `Smoke.AutoFishingMiniGameSkip = verified ... autoHook=AgentStateFishingPull`.
- AutoFishing result: `result.json` has `AutoFishingInputLog=true`, `AutoFishingMovementCancel=true`, `AutoFishingPhase=true`, `AutoFishingMiniGameSkip=true`, `ProcessExited=true`, and `NoFatalInstanceWindow=true`.
- Exit evidence: both `process-check.txt` files say no `DolocTown.exe`; fatal-window checks say no fatal instance popup.

## Known Facts And Rejected Hypotheses

- `GAME-SMOKE/20260603-030142` remains useful partial evidence for the 0.2.3 toast policy, but it did not prove movement cancel or minigame behavior.
- The ActionSpeed strong proof is a native no-key fill path with a shorter configured cooldown, not a direct save edit or fake inventory mutation.
- The AutoFishing minigame proof here verifies the high-impact `SkipMiniGame` route by jumping from wait-phase bite handling to native Pull. The non-skip `FishingGameScrollBar.currentGameStatus=Success` path was later directly smoke-verified in `docs/updates/2026/20260603-0016-autofishing-skipfalse-minigame-smoke.md`.

## Rollback

- Revert this record and the changed files listed above.
- If rolling back only smoke strictness, remove `AutoFishingMovementCancel` and `AutoFishingMiniGameSkip` from the harness result/failure gates and restore the previous AutoFishing smoke config.

## Follow-up

- Continue remaining 0.2.3 blocker: second-motor dual-key/dual-instance manual proof and mail/key-delivery research.
- A human manual pass should still confirm player-facing AutoFishing toasts are limited to F6 on/off and movement cancel.
