# 20260630-0005 Custom Animal Sleep Task Boundary

## Summary

Added a bottom-layer GameBridge stability boundary for registered custom livestock that reuse native template AI. When a custom animal is already `isSleep=true` during `Night`, FreeTime decisions no longer create movement work, and stale movement tasks created before the sleep state settles are stopped before execution.

## Source Request

User asked to commit the current Hatch/Shell Crab diagnostic work first, then implement the stable fix that was identified from code-level analysis. The requested direction was not a narrow visual patch, but a lower-level stability build around the native AI/task boundary.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/HarmonyReflectionPatcher.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Implementation Notes

- Added `CustomAnimals.SleepTaskBoundary` hook status.
- Added a Harmony prefix bridge for `AnimalAI/AnimalAIState.MakeDecision_FreeTime()`. For registered custom species only, `isSleep=true`, and `CurrentDayPeriodType=Night`, it returns native `LinearTask.WaitFrames(5)`.
- Added an `AnimalController.OnUpdate(float)` postfix. After native AI update and before the current task executes, it stops stale `AnimalMove`, `AnimalJump`, or `AnimalEnterRoom` tasks if the same sleep boundary is true.
- Kept vanilla species, unregistered species, animals still walking to bed, morning wake, and tool wakeups outside the boundary.
- Made reflection member reads walk base types so derived native AI states can expose the base `AnimalAIState.animal` field reliably.
- Further reduced diagnostic log volume by throttling `AnimalRenderer.PlayAnimation` sleep diagnostics to one line per animal/reason/animation name.

## Root-Cause Notes

The current confirmed path is not a renderer-only artifact:

- Manual logs captured Hatch at 00:05 with `sleep=true`, `period=Night`, `aiState=Normal_SleepState`, and `task=DolocTown.AnimalMove`.
- The same window had no `Animal.WakeUp`, no `AnimalRenderer.OnFell`, and no `Animal.CallToRoom`.
- Native `MakeDecision_FreeTime()` can return wander/movement work without checking `animal.isSleep`.
- Native `DecisionMaker` does not automatically stop an already-created movement task when the state machine later reaches `Normal_SleepState`.

The fix therefore treats custom template animals as needing a bridge-owned sleep/task boundary, not a forced renderer state.

## Validation

- Passed: `git diff --check`; only CRLF normalization warnings were reported.
- Passed: `tools/scripts/test.ps1 -Configuration Release` with `DTMAPI.UnitTests: OK`.
- Warnings: restricted-network `NU1900` package vulnerability index warnings while querying NuGet metadata.
- Passed: installed current Release runtime to the local Doloc Town directory under runtime lock, then ran `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -SaveSlot 7 -TimeoutSeconds 240 -SkipBuild`.
- Passed: slot 7 smoke evidence `docs/debug/evidence/GAME-SMOKE/20260630-222709` reports `StartupLog`, `GameLaunched`, `SaveLoaded`, `HookProbe`, `ProcessExited`, `NoFatalInstanceWindow`, and `ForcedClose`.
- Passed: `docs/debug/evidence/GAME-SMOKE/20260630-222709/process-check.txt` reports no `DolocTown.exe`.
- Verified: `DTMAPI-latest.log` / `Unity-Player.log` show `CustomAnimals.SleepTaskBoundary = verified`, `animalAIMakeDecisionFreeTime=True`, `animalControllerOnUpdate=True`, and `customSpecies=hatch,shell_crab`.
- Later verified: after this change, the user completed a five-night manual retest on slot 7 with debug time skips and post-midnight barn entry. Hatch/Shell Crab stayed asleep. The follow-up log review and diagnostic log trim are recorded in `docs/updates/2026/20260630-0006-custom-animal-diagnostic-followup-log-trim.md`.

## Evidence

- Source/reverse evidence is recorded in `docs/hook-map/README.md` under `CustomAnimals.SleepTaskBoundary`.
- Regression gate is recorded in `docs/debug/regressions/smoke-matrix.md` as `CUSTOM-ANIMAL-SLEEP-TASK-BOUNDARY-20260630`.
- Runtime smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260630-222709`.

## Rollback

Disable the `AnimalAI.MakeDecision_FreeTime` prefix and `AnimalController.OnUpdate` postfix installation from `CustomAnimalAnimatorBridgeHookBridge`. Keep the diagnostic-only sleep/wake hooks if further manual evidence is needed.

## Follow-Up

Manual no-tool midnight barn-entry retest has since passed in slot 7. Future follow-up should only reopen this path if logs again show persistent custom-animal `sleep=true period=Night task=DolocTown.AnimalMove` or visible Hatch/Shell Crab stand/move after the boundary has had a tick to clean stale tasks.
