# 20260609-0025 Shared AgentState Lifecycle Hooks

## Metadata

- Update ID: 20260609-0025
- Date: 2026-06-09
- Status: verified
- Source: User follow-up plan, step `codex/refactor-shared-agentstate-lifecycle-hooks`.
- Owner: Codex

## Scope

- Move shared `AgentState*OnExit` hook ownership out of the ActionSpeed hook bridge.
- Keep existing callbacks, hook IDs, hook status text, smoke result fields, and gameplay behavior unchanged.
- Make ActionCompletion fuel/feed depend on the shared `AgentStateInteract.OnExit` owner instead of ActionSpeed hook installation state.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/AgentStateLifecycleHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/ActionSpeedFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/ActionSpeedHookBridge.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0025-shared-agentstate-lifecycle-hooks.md`

## Summary

- Added `AgentStateLifecycleHookBridge` as the owner for `AgentStateTool.OnExit`, `AgentStateInteract.OnExit`, and `AgentStateBase.OnExit`.
- Left `DolocTownHookCallbacks` unchanged, so ActionSpeed restore and ActionCompletion fuel/feed still run through the same callback bodies.
- Made `ActionSpeedHookBridge` own only ActionSpeed-specific enter/continuous-use hooks while reading shared lifecycle readiness for tool/interaction readiness.
- Made `ActionCompletionFeature` read `AgentStateInteract.OnExit` readiness from the shared lifecycle bridge.
- Kept `ActionSpeed.ToolAnimation`, `ActionSpeed.InteractionAnimation`, `Actions.OneActionComplete`, and `Actions.OneActionFuelFeed` status keys/meanings unchanged.
- Fixed generated `latest-report.txt` pointers for `GAME-SMOKE/20260609-172748` and `GAME-SMOKE/20260609-172903`.

## Validation

- Passed: `git diff --check`
- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseOneActionResourceHit -AutoExerciseOneActionWrongTool -AutoExerciseOneActionFuelFeed -AutoExerciseOneActionVegetation -SaveSlot 3 -TimeoutSeconds 360`
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseActionSpeedTool -AutoExerciseActionSpeedConfigApply -AutoExerciseActionSpeedInteraction -SaveSlot 3 -TimeoutSeconds 360`

## Evidence

- OneAction game smoke: `docs/debug/evidence/GAME-SMOKE/20260609-172748`
- OneAction report zip: `docs/debug/evidence/GAME-SMOKE/20260609-172748.zip`
- ActionSpeed game smoke: `docs/debug/evidence/GAME-SMOKE/20260609-172903`
- ActionSpeed report zip: `docs/debug/evidence/GAME-SMOKE/20260609-172903.zip`
- OneAction `result.json`: `SchemaVersion=2`, `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- OneAction log evidence: `Feature.ActionCompletion = ready`, `Actions.OneActionFuelFeed = verified`, resource completion, wrong-tool matrix, fuel/feed completion, and vegetation exception verification all keep existing meanings.
- ActionSpeed `result.json`: `SchemaVersion=2`, `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- ActionSpeed log evidence: `Feature.ActionSpeed = ready`, `ActionSpeed.ToolAnimation = verified`, `ActionSpeed.InteractionAnimation = experimental`, and restore logs for `AgentStateTool.OnExit`, `AgentStateInteract.OnExit`, and `AgentStateBase.OnExit`.
- Exit evidence: both smokes report no leftover `DolocTown.exe` and no fatal instance popup.

## Related Records

- `docs/debug/regressions/smoke-matrix.md` row `SHARED-AGENTSTATE-LIFECYCLE-20260609`
- `docs/hook-map/README.md` sections `ActionSpeed.ToolAnimation` and `Actions.OneActionComplete`
- `docs/api/public-api-matrix.md` rows `IActionSpeedApi` and `IActionCompletionApi`
- `docs/updates/2026/20260609-0021-actioncompletion-feature-split.md`
- `docs/updates/2026/20260609-0013-actionspeed-feature-host-split.md`

## Rollback Notes

- Move `AgentStateTool.OnExit`, `AgentStateInteract.OnExit`, and `AgentStateBase.OnExit` patch installation back into `ActionSpeedHookBridge`.
- Repoint ActionCompletion's interact-exit readiness function to the ActionSpeed hook bridge.
- Keep rollback scoped to hook ownership; do not change callback bodies or smoke schemas.

## Follow-Up

- Merge `codex/refactor-shared-agentstate-lifecycle-hooks` back to `Refactor`.
- Continue the GameBridge native helper extraction branch.
