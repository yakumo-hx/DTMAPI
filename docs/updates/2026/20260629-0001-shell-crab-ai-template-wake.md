# 20260629-0001 Shell Crab AI Template Wake Bridge

## Summary

Manual slot 7 feedback reported that a young Shell Crab stayed in `sleep` after overnight time passed. Root-cause review against reverse build `23762374_public_C416D4` found this is not an AnimatorController issue: native `Animal.Sleep()` and `Animal.WakeUp()` directly play `sleep` / `idle`, and the Shell Crab controllers contain those states.

The bug is a custom-species AI template boundary. Shell Crab keeps `schedule_id: goat`, so native `AnimalAI.Create` builds a goat state machine, but `AnimalAIState` chooses its default any-state by `animal.protoName`. For `shell_crab`, vanilla `GetDefaultAnyState("shell_crab")` returns `Normal_FreeTimeState`, which is not in the goat state machine. When sleep should exit, `Normal_SleepState.GetNextState()` returns null, `OnExit()` does not run, and `Animal.WakeUp()` is never called.

This update adds a narrow internal `CustomAnimals.AiTemplateBridge` that maps enabled DTMAPI custom species to their declared `aiTemplate` only for `AnimalAI.GetDefaultAnyState(string)`.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/HarmonyReflectionPatcher.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/hook-map/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/reviews/manual-qa/2026/20260629-0001-shell-crab-sleep-ai-template-review.md`
- `docs/updates/INDEX.md`

## Source Request

User feedback: "第七档，小螃蟹过夜，sleep没有变成正常的其他状态。检查是动画机、AI还是DTMAPI问题。"

## Implementation

- Added internal parsing for `aiTemplate` in `Content/DTMAPI/custom-animals.json`.
- Added enabled ContentPack mapping from custom `speciesId` to native AI template species, e.g. `shell_crab -> goat`.
- Added Harmony postfix support for the private static native method `DolocTown.AnimalAI.GetDefaultAnyState(string)`.
- Added `CustomAnimals.AiTemplateBridge` status publication and per-species verified/degraded status.
- The postfix only changes the returned state type for registered custom species. Unknown vanilla species and unregistered keys keep the native result.
- Kept Shell Crab `schedule_id: goat` and did not mutate goat/chicken controllers, template `AnimalInfo`, renderer sprites, or runtime entities.

## Validation

- Passed: `tools/scripts/test.ps1 -Configuration Release`.
- Passed: `DTMAPI.UnitTests: OK`.
- Passed: runtime-locked slot 7 smoke `tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -IncludeHookProbe -AutoExitAfterSecondsOverride 120 -TimeoutSeconds 180`.
- Unit coverage now verifies:
  - `aiTemplate` metadata parses to Shell Crab `shell_crab|goat`.
  - `templateSpeciesId` is used as a fallback when `aiTemplate` is omitted.
  - identity native mappings are not registered.
  - unknown species keep the original `AnimalAI.GetDefaultAnyState` result.
  - registered custom species degrade and preserve the original result when native game `AnimalAI` state types are unavailable in unit tests.

Build output still shows restricted-network `NU1900` package-vulnerability-index warnings, matching prior local test behavior.

## Evidence

- Native review:
  - `AnimalController` creates AI by `animal.proto.ScheduleId`.
  - `AnimalAIState` queries default any-state by `animal.protoName`.
  - `Normal_SleepState.GetNextState()` returns `defaultAnyState` after sleep time.
  - StateMachine stays in the current state if `GetNextState()` returns null.
  - `Normal_SleepState.OnExit()` owns `Animal.WakeUp()`.
- Runtime smoke `GAME-SMOKE/20260629-164028`:
  - Result: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
  - `process-check.txt`: `No DolocTown.exe process found.`
  - `fatal-window-check.txt`: `No fatal instance popup found.`
  - Log: `CustomAnimals.AiTemplateBridge = verified. AnimalAI.GetDefaultAnyState hook is patched; registered custom animal AI templates=shell_crab->goat.`
  - Log: `CustomAnimals.AiTemplateBridge verified species=shell_crab aiTemplate=goat stateType=DolocTown.AnimalAI+Goat_FreeTimeState.`
  - Log: `SaveLoaded hook dispatched. slot/index=6 isNewGame=False` and `HookProbe SaveLoaded OK slot=6 isNewGame=False`.
- Manual overnight wake verification remains pending.

## Rollback

Revert the `TryPatchAnimalAIDefaultAnyStatePostfix` route and `aiTemplate` registry additions. Shell Crab visuals would still work through `CustomAnimals.AnimatorBridge`, but custom species using a template schedule could again sleep-stall when their `speciesId` differs from the template recognized by vanilla `GetDefaultAnyState`.

## Follow-Up

- Manually retest slot 7 young Shell Crab overnight in the empty barn; pass requires it to leave sleep and resume normal goat-template AI behavior while retaining Shell Crab visuals.
