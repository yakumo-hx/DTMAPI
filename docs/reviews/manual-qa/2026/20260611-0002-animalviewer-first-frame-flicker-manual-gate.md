# AnimalViewer First-Frame Flicker Manual Gate - 2026-06-11

## Scope

Manual QA Batch 1 addresses only the AnimalViewer hidden-produce first-frame flicker where the cloned progress row could briefly show the native mood title `心情` before the custom produce title was applied.

This gate does not cover Camera background synchronization, SaveSlots 18+ paging, Manager full-list paging, Equipment, MotorVehicle, MachineProduction, Workshop, installer, or API stability promotion.

## Current Automated Guard

The current implementation:

- clones the native progress bar for hidden produce progress;
- disables localization-style components on the cloned row before first activation;
- pre-fills the cloned row while inactive;
- activates the row only after text is filled;
- refreshes the cloned text again in the same callback;
- publishes `Smoke.AnimalViewerFirstFrameFlickerGuard` when the same-callback text check sees no mood-title hit on the cloned row.

This is useful automated evidence, but it is not equivalent to human visual confirmation across repeated animal switches. A one-frame UI flash can still be timing-dependent and may be missed by a final-state screenshot.

## Required Manual Steps

1. Start from local save slot 3 / index 2 with `Yuuka.DTMAPI.AnimalHusbandryProgress` enabled.
2. Open the official animal panel.
3. Select an animal with configured hidden-produce progress.
4. Switch between at least ten animals or repeat open/close/select cycles at least ten times.
5. Watch the DTMAPI hidden-produce progress row during the first frame after each switch.
6. Confirm that the hidden-produce row never displays the native mood title `心情` or English `Mood` before the custom title, while the native mood row remains intact elsewhere.
7. Record pass/fail, time, save slot, mod config, screenshots, and video if available.

## Expected Result

- Native mood/state UI remains unchanged.
- DTMAPI hidden-produce row displays the custom configured produce title and progress text immediately.
- No visible first-frame `心情` / `Mood` flicker appears on the cloned hidden-produce row during repeated switches.

## Status

Pending user confirmation.

Automated smoke `GAME-SMOKE/20260611-214845` verifies the hook path, final row text, same-callback text guard, clean exit, and no fatal popup. Its Animal summary records `localizationDisabled=1` and `firstFrameGuard=sameCallbackTextCheck ... moodTitleHits=0`. This evidence must not be used alone to claim this visual flicker is fully solved.

## Related Records

- `docs/reviews/manual-qa/2026/20260611-0001-refactor-manual-qa-code-review.md`
- `docs/reviews/manual-qa/2026/20260607-0002-ui-save-mine-animal-refactor-review.md`
- `docs/debug/regressions/smoke-matrix.md` row `MANUAL-QA-BATCH1-FISHING-STRONGPLANTING-ANIMAL-20260611`
- `docs/updates/2026/20260611-0018-manual-qa-batch1-fishing-strongplanting-animal.md`
