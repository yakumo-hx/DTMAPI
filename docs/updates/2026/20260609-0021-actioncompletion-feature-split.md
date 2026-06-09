# 20260609-0021 ActionCompletion Feature Split

## Metadata

- Update ID: 20260609-0021
- Date: 2026-06-09
- Status: verified
- Source: User route step to split the third low-risk feature as `ActionCompletionFeature` after the feature-status model.
- Owner: Codex

## Scope

- Move `IActionCompletionApi` registration and implementation out of `DolocTownExperimentalBridgeApi`.
- Add an ActionCompletion feature/service/hook bridge under the GameBridge feature-host route.
- Keep `Actions.OneActionComplete`, `Actions.OneActionFuelFeed`, and OneAction smoke result meanings unchanged.
- Do not split Fishing, Vehicle, Equipment, or FishRoeTooltip in this step.
- Do not promote `IActionCompletionApi` beyond `Experimental`.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Diagnostics/DolocTownExperimentalBridgeApi.Diagnostics.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionCompletion/ActionCompletionFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionCompletion/ActionCompletionHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionCompletion/ActionCompletionService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionCompletion/DolocTownExperimentalBridgeApi.ActionCompletion.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/OilCoalDrop/DolocTownExperimentalBridgeApi.OilCoalDrop.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/evidence/GAME-SMOKE/20260609-143612/latest-report.txt`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0021-actioncompletion-feature-split.md`

## Summary

- Added `ActionCompletionFeature`, which registers `IActionCompletionApi` through `ActionCompletionService`.
- Moved one-action policy, counters, status DTO, resource-hit completion, fuel/feed completion, and guard helpers into `ActionCompletionService`.
- Added `ActionCompletionHookBridge` for the `ToolCollider.HandleTools` hook and unchanged OneAction hook statuses.
- Kept the fuel/feed callback on the existing `AgentStateInteract.OnExit` feature-host route so hook IDs and status text remain stable.
- Routed `DolocTownHookCallbacks` through `Bridge.ActionCompletionService`.
- Left OilCoalDrop ownership in the experimental bridge, with only the existing coal-drop helper exposed internally to the ActionCompletion service.
- Fixed the generated `latest-report.txt` pointer for this smoke evidence to point at `docs/debug/evidence/GAME-SMOKE/20260609-143612.zip`.

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseOneActionResourceHit -AutoExerciseOneActionWrongTool -AutoExerciseOneActionFuelFeed -AutoExerciseOneActionVegetation -SaveSlot 3 -TimeoutSeconds 360`.

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260609-143612`
- Report zip: `docs/debug/evidence/GAME-SMOKE/20260609-143612.zip`
- `result.json`: `SchemaVersion=2`, `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Log evidence: `Feature.ActionCompletion = ready` with `Feature status: id=ActionCompletion, lastOperation=PublishHookStatuses/InstallHooks/Update/ReturnedToTitle/SaveLoaded/EnvironmentReset, success=True, failureCount=0, lastError=none`.
- Hook status evidence: `Actions.OneActionComplete = verified` and `Actions.OneActionFuelFeed = verified` keep their existing meanings.
- Gameplay evidence: one-action resource completion, wrong-tool skips, fuel-machine fill, feeder fill, and vegetation/dandelion exception classification all passed.
- Exit evidence: `process-check.txt` says no `DolocTown.exe` process was found; `fatal-window-check.txt` says no fatal instance popup was found.

## Related Records

- `docs/debug/regressions/smoke-matrix.md` row `ACTIONCOMPLETION-FEATURE-SPLIT-20260609`
- `docs/hook-map/README.md` section `Actions.OneActionComplete`
- `docs/api/public-api-matrix.md` row `IActionCompletionApi`
- `docs/updates/2026/20260609-0020-feature-status-model.md`

## Rollback Notes

- Move `IActionCompletionApi` implementation and registration back into `DolocTownExperimentalBridgeApi`.
- Restore `DolocTownHookCallbacks` to call the experimental bridge directly.
- Remove `ActionCompletionFeature`, `ActionCompletionService`, and `ActionCompletionHookBridge`.
- Keep this rollback scoped to ActionCompletion; do not touch Fishing, Vehicle, Equipment, ActionSpeed, or Camera feature ownership.

## Follow-Up

- Merge `codex/refactor-actioncompletion-feature` back to `Refactor`.
- Generate final full and web audit packages from the merged `Refactor` worktree.
