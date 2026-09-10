# 20260609-0024 ActionCompletion Smoke Case File

## Metadata

- Update ID: 20260609-0024
- Date: 2026-06-09
- Status: verified
- Source: User follow-up plan, step `codex/refactor-smoke-actioncompletion-case`.
- Owner: Codex

## Scope

- Move ActionCompletion/OneAction smoke case implementation out of `SmokeHarness.cs`.
- Keep result schema, result fields, hook status keys, log text meanings, and evidence semantics unchanged.
- Do not change `ActionCompletionFeature`, `ActionCompletionService`, `ActionCompletionHookBridge`, or gameplay behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/ActionCompletionSmokeCase.cs`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0024-actioncompletion-smoke-case-file.md`

## Summary

- Moved the OneAction resource-hit, wrong-tool, fuel/feed, and vegetation smoke methods into `Smoke/Cases/ActionCompletionSmokeCase.cs`.
- Left `SmokeHarness.cs` responsible for scheduling attempts, result-field ownership, smoke settings, and shared helper methods.
- Preserved the moved method block byte-for-byte except for the new file wrapper/usings.
- Fixed the generated `latest-report.txt` pointer for the smoke evidence to point at `docs/debug/evidence/GAME-SMOKE/20260609-171936.zip`.

## Mechanical Check

- Moved method block SHA-256 before move: `9bdd9ed007fbb05061025e3209002ac9c542fafedcf1e2d6e97e31c78f3628d3`
- Moved method block SHA-256 after move: `9bdd9ed007fbb05061025e3209002ac9c542fafedcf1e2d6e97e31c78f3628d3`

## Validation

- Passed: `git diff --check`
- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseOneActionResourceHit -AutoExerciseOneActionWrongTool -AutoExerciseOneActionFuelFeed -AutoExerciseOneActionVegetation -SaveSlot 3 -TimeoutSeconds 360`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260609-171936`
- Report zip: `docs/debug/evidence/GAME-SMOKE/20260609-171936.zip`
- `result.json`: `SchemaVersion=2`, `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Log evidence: `Feature.ActionCompletion = ready` with structured status details.
- Hook status evidence: `Smoke.OneActionResourceHit = verified`, `Smoke.OneActionWrongTool = verified`, `Smoke.OneActionFuelFeed = verified`, and `Smoke.OneActionVegetation = verified` keep their existing meanings.
- Gameplay evidence: one-action resource completion, wrong-tool matrix, fuel-machine/feeder fill, and vegetation/dandelion exception classification all passed.
- Exit evidence: `process-check.txt` says no `DolocTown.exe` process was found; `fatal-window-check.txt` says no fatal instance popup was found.

## Related Records

- `docs/debug/regressions/smoke-matrix.md` row `ACTIONCOMPLETION-SMOKE-CASE-SPLIT-20260609`
- `docs/hook-map/README.md` section `Actions.OneActionComplete`
- `docs/updates/2026/20260609-0021-actioncompletion-feature-split.md`

## Rollback Notes

- Move the methods from `Smoke/Cases/ActionCompletionSmokeCase.cs` back into `SmokeHarness.cs`.
- Delete `Smoke/Cases/ActionCompletionSmokeCase.cs`.
- Keep this rollback mechanical; do not change ActionCompletion service, hooks, or public API behavior.

## Follow-Up

- Merge `codex/refactor-smoke-actioncompletion-case` back to `Refactor`.
- Continue the shared AgentState lifecycle hook owner branch.
