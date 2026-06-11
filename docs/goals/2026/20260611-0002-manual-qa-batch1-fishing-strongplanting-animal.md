# Manual QA Batch 1 Fishing, StrongPlantingGun, AnimalViewer Goal

Status: completed
Created: 2026-06-11
Target branch: `codex/manual-qa-batch1-fishing-strongplanting-animal`
Target merge branch: `Refactor`
Target version: no version bump

## Source Request

The user supplied `/goal Manual QA Batch 1` and asked Codex to process the attached request. This goal preserves the implementation scope from that prompt so future Codex threads do not expand the task into unrelated manual QA findings.

## Required Reading

- `AGENTS.md`
- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/debug/INDEX.md`
- `docs/workflows/codex-feedback-to-goal.md`
- `docs/goals/README.md`
- `docs/reviews/README.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/reviews/manual-qa/2026/20260611-0001-refactor-manual-qa-code-review.md`
- `docs/reviews/manual-qa/2026/20260607-0002-ui-save-mine-animal-refactor-review.md`
- `docs/reviews/api/2026/20260610-fishing-native-responsibility.md`
- `docs/reviews/api/2026/20260610-fishing-options-contract-review.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`

## Scope

Implement only the low-risk Manual QA Batch 1 items:

1. Align Fishing/AutoFishing config semantics and visible UI/docs text with current `FishingAutomationService` behavior.
2. Align StrongPlantingGun slot, disabled-state, and range wording with the currently verified fixed three-slot contract.
3. Reduce AnimalViewer hidden-produce first-frame `心情` flicker risk and create a manual gate for visual confirmation.

## Non-Goals

- Camera background/fog synchronization.
- SaveSlots 18+ paging/layout work.
- Manager full-list paging.
- F9 fishing info UI.
- Equipment, MotorVehicle, MachineProduction, Workshop, installer, Content Pipeline, or API StableCandidate promotion.

## Acceptance Criteria

- AutoFishing config no longer claims an independent minigame skip or guaranteed animator acceleration.
- Fishing option XML/docs explain accepted-but-normalized values and delayed/minigame semantics.
- StrongPlantingGun migrated config no longer exposes unsupported slot count/range expansion.
- StrongPlantingGun service normalizes `SlotCount` to 3 while avoiding shrink/item-loss behavior on already-expanded tools.
- AnimalViewer clone localization is disabled before first activation; same-callback text guard is reported, but repeated visual switch confirmation stays pending until manually verified.
- Build/test and required smokes are run or explicitly recorded as not run.

## Required Validation

- `git diff --check`
- `tools/scripts/build.ps1 -Configuration Release`
- `tools/scripts/test.ps1 -Configuration Release`
- `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoPressAutoFishingHotkey -AutoExerciseAutoFishingPhase -AutoExerciseAutoFishingMiniGameComplete -SaveSlot 3 -TimeoutSeconds 360`
- `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseStrongPlantingGun -SaveSlot 3 -TimeoutSeconds 240`
- `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoOpenAnimalPanel -SaveSlot 3 -TimeoutSeconds 240`
- `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -SaveSlot 3 -TimeoutSeconds 240`

## Completion Notes

Final response must include:

- summary of changes;
- Fishing semantics table;
- StrongPlantingGun contract table;
- AnimalViewer flicker explanation;
- build/test/smoke results;
- unfixed list for Camera background sync, SaveSlots 18+ paging, Manager full list/paging, and F9 fishing info.
