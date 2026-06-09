# 20260610-0001 Lifecycle Callback Isolation

## Metadata

- Update ID: 20260610-0001
- Date: 2026-06-10
- Status: verified
- Source: User Refactor stability follow-up plan, step `codex/fix-lifecycle-callback-isolation`.
- Owner: Codex

## Scope

- Isolate GameBridge/runtime lifecycle callback failures so one callback cannot block later cleanup, restore, smoke marking, or runtime event dispatch in the same native boundary.
- Cover save load, native save, returned-to-title, environment reset, Workshop reload, shared AgentState exit, and fishing Pull exit callbacks.
- Keep hook targets, hook IDs, hook status meanings, gameplay service behavior, and smoke result schema unchanged.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `docs/goals/2026/20260610-0001-refactor-stability-followup.md`
- `docs/goals/2026/20260610-0001-refactor-stability-followup.goal.txt`
- `docs/reviews/api/2026/20260610-0001-lifecycle-callback-isolation-review.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0001-lifecycle-callback-isolation.md`

## Known Facts

- `SaveLoaded`, `SaveSaved`, `ReturnedToTitle`, shared `AgentState*OnExit`, and fishing Pull exit callback bodies were direct call chains before this branch.
- The GameBridge feature host already isolates feature dispatch, but low-level Harmony/Unity callback chains could still be interrupted before reaching later callbacks.
- `AgentStateFishingPull.OnExit` must restore experimental fishing animator speeds even if fishing phase notification fails.

## Rejected Directions

- No public API change was needed.
- No hook target, hook ID, hook status text, smoke schema, mod config, or gameplay rule change was needed.
- A single outer try/catch around each postfix was rejected because it would still skip later callbacks after the first exception.

## Summary

- Added private `SafeCallback(operation, action)` and failure logging inside `DolocTownHookCallbacks`.
- Wrapped each lifecycle/save/agent-state callback step independently.
- Recorded failures under diagnostics owner `DTMAPI.GameBridge.Lifecycle` and runtime error logs when they occur.
- Kept normal successful execution quiet: passing runs do not add new hook-status rows or success noise.
- Kept `FishingPullExitPostfix` restore in a `finally`-equivalent path so cooldown notification cannot block animator-speed restore.
- Corrected local `latest-report.txt` pointers for the five new smoke evidence directories to point at their matching repo evidence zip files.

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Note: an earlier parallel build/test attempt hit only a transient `obj` file-lock while `test.ps1` succeeded; the sequential build above supersedes it.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoPressAutoFishingHotkey -AutoExerciseAutoFishingPhase -AutoExerciseAutoFishingMiniGameComplete -SaveSlot 3 -TimeoutSeconds 360`
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseActionSpeedTool -AutoExerciseActionSpeedConfigApply -AutoExerciseActionSpeedInteraction -SaveSlot 3 -TimeoutSeconds 360`
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseOneActionResourceHit -AutoExerciseOneActionWrongTool -AutoExerciseOneActionFuelFeed -AutoExerciseOneActionVegetation -SaveSlot 3 -TimeoutSeconds 360`
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseInstantSave -SaveSlot 3 -TimeoutSeconds 240`
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseTitleButtonLifecycle -SaveSlot 3 -TimeoutSeconds 300`

## Evidence

- AutoFishing smoke: `docs/debug/evidence/GAME-SMOKE/20260610-012052`
- AutoFishing evidence zip: `docs/debug/evidence/GAME-SMOKE/20260610-012052.zip`
- ActionSpeed smoke: `docs/debug/evidence/GAME-SMOKE/20260610-012242`
- ActionSpeed evidence zip: `docs/debug/evidence/GAME-SMOKE/20260610-012242.zip`
- OneAction smoke: `docs/debug/evidence/GAME-SMOKE/20260610-012400`
- OneAction evidence zip: `docs/debug/evidence/GAME-SMOKE/20260610-012400.zip`
- InstantSave smoke: `docs/debug/evidence/GAME-SMOKE/20260610-012750`
- InstantSave evidence zip: `docs/debug/evidence/GAME-SMOKE/20260610-012750.zip`
- Title lifecycle smoke: `docs/debug/evidence/GAME-SMOKE/20260610-012907`
- Title lifecycle evidence zip: `docs/debug/evidence/GAME-SMOKE/20260610-012907.zip`
- AutoFishing `result.json`: `RunStatus=Passed`, `SaveLoaded=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- ActionSpeed `result.json`: `RunStatus=Passed`, `SaveLoaded=Passed`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- OneAction `result.json`: `RunStatus=Passed`, `SaveLoaded=Passed`, `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- InstantSave log evidence: `SaveSaving hook dispatched. slot/index=2`, `SaveSaved hook dispatched. slot/index=2`, and `Smoke.InstantSave = verified`.
- Returned-to-title log evidence: `ReturnedToTitle hook dispatched.` and `Smoke.TitleButtonLifecycle = verified`.
- Failure isolation check: the five passing smoke logs contain no `Lifecycle callback failed` entries.
- Exit evidence: all five smokes report no leftover `DolocTown.exe` and no fatal instance popup.

## Related Records

- `docs/reviews/api/2026/20260610-0001-lifecycle-callback-isolation-review.md`
- `docs/debug/regressions/smoke-matrix.md` row `LIFECYCLE-CALLBACK-ISOLATION-20260610`
- `docs/hook-map/README.md` sections `Save.SaveLoaded`, `ActionSpeed.ToolAnimation`, `Actions.OneActionComplete`, `Fishing.Automation`, and `Fishing.MiniGameUpdate`
- `docs/api/public-api-matrix.md` rows `IFishingAutomationApi`, `IActionSpeedApi`, and `IActionCompletionApi`

## Rollback Notes

- Remove `SafeCallback(...)` and restore the direct callback chains in `DolocTownHookCallbacks`.
- Keep rollback scoped to callback isolation only; do not revert prior ActionSpeed, Fishing, ActionCompletion, or feature-host branches.

## Follow-Up

- Merge `codex/fix-lifecycle-callback-isolation` back to `Refactor` with `--no-ff`.
- Continue with `codex/chore-audit-package-markdown-cleanup`.

