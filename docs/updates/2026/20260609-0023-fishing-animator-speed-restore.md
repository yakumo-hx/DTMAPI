# 20260609-0023 Fishing Animator Speed Restore

## Metadata

- Update ID: 20260609-0023
- Date: 2026-06-09
- Status: verified
- Source: User follow-up plan, step `codex/fix-fishing-animator-speed-restore`.
- Owner: Codex

## Scope

- Restore the experimental fishing automation animator-speed snapshots at state and lifecycle boundaries.
- Keep `IFishingAutomationApi` public contract unchanged.
- Do not split `FishingFeature` in this step.
- Do not change `ActionSpeedService` behavior or its independent animator-speed snapshot state.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0023-fishing-animator-speed-restore.md`

## Known Facts

- Fishing automation fast animation used the experimental bridge's `originalAnimatorSpeeds` cache through `ApplyAnimatorSpeed(...)`.
- `ActionSpeedService` already had an independent restore path, but that cache is separate from the fishing automation cache.
- `AgentStateFishingPull.OnExit`, `AgentStateBase.OnExit`, `SaveLoaded`, and `ReturnedToTitle` are the useful recovery boundaries for stale fishing animation state.

## Rejected Directions

- No ActionSpeed restore changes were needed.
- No new public API/status contract was needed.
- No hook target, smoke result field, or mod config change was needed.

## Summary

- Added internal `RestoreExperimentalAnimatorSpeeds(reason)` to the experimental bridge.
- Restored cached original animator speeds from a snapshot of the existing dictionary, then cleared the cache.
- Logged the restore reason/count and published `Smoke.AutoFishingAnimationSpeedRestore` for smoke evidence.
- Called the restore path from save load, return-to-title, `AgentStateBase.OnExit`, and `AgentStateFishingPull.OnExit`.
- Fixed the generated `latest-report.txt` pointer for the smoke evidence to point at `docs/debug/evidence/GAME-SMOKE/20260609-170646.zip`.

## Validation

- Passed: `git diff --check`
- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoPressAutoFishingHotkey -AutoExerciseAutoFishingPhase -AutoExerciseAutoFishingMiniGameComplete -SaveSlot 3 -TimeoutSeconds 360`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260609-170646`
- Report zip: `docs/debug/evidence/GAME-SMOKE/20260609-170646.zip`
- `result.json`: `SchemaVersion=2`, `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Log evidence: `Fishing automation animation speed applied by Yuuka.DTMAPI.AutoFishing phase=Pull multiplier=3 animators=2.`
- Restore evidence: `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2.`
- Hook status evidence: `Smoke.AutoFishingAnimationSpeedRestore = experimental. reason=AgentStateFishingPull.OnExit, restored=2`
- Exit evidence: `process-check.txt` says no `DolocTown.exe` process was found; `fatal-window-check.txt` says no fatal instance popup was found.

## Related Records

- `docs/debug/regressions/smoke-matrix.md` row `FISHING-ANIMATOR-RESTORE-20260609`
- `docs/hook-map/README.md` sections `Fishing.Automation` and `Fishing.MiniGameUpdate`
- `docs/api/public-api-matrix.md` row `IFishingAutomationApi`

## Rollback Notes

- Remove `RestoreExperimentalAnimatorSpeeds(reason)` and the four lifecycle calls in `DolocTownHookCallbacks`.
- Keep this rollback scoped to Fishing automation; do not revert ActionSpeed lifecycle restore or feature-host code.

## Follow-Up

- Merge `codex/fix-fishing-animator-speed-restore` back to `Refactor` with `--no-ff`.
- Continue the planned independent follow-up branches from the clean `Refactor` baseline.
