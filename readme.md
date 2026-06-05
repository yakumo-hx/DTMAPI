# DTMAPI 0.2.9 Manual QA Follow-up Goal

This file is the current implementation ledger for the next DTMAPI round. It is based on the 0.2.8 user manual QA review, not on smoke results alone.

## Current Status

- Current implemented baseline: DTMAPI 0.2.8.
- Latest implementation record: `docs/updates/2026/20260606-0002-028-readme-implementation.md`.
- Latest review source: `docs/reviews/manual-qa/2026/20260606-0002-028-manual-qa-followup-review.md`.
- Local recovery checkpoint exists on `master`.
- Public GitHub preview branch was pushed to `https://github.com/yakumo-hx/DTMAPI.git` with only curated download/display content:
  - DTMAPI runtime DLLs.
  - Confirmed `ActionSpeed` and `OneActionComplete` official-local packages.
  - Branding and public API summary.
- Public branch intentionally excludes full source, reverse data, third-party samples, debug evidence, review records, unfinished experimental mods, and private research notes.
- User-confirmed packages for public sync: ActionSpeed and OneActionComplete. Do not rework them in this goal unless the user reports a new regression.

## Manual Feedback Header

- Time: 2026-06-06 +08:00.
- Source: user manual testing after 0.2.8 plus code-level review.
- Scope: AnimalHusbandryProgress flicker, Y-console layout/icon centering, Mine preview/real electricity, MoreEquipmentSlots save safety/config cleanup, and a new More Saves mod.
- Forbidden: do not copy old DLKsmapi code; do not reset/revert user or previous-Codex changes; do not publish or modify reverse/third-party reference files; do not count build/smoke as completion when user-visible QA points remain.
- Version requirement: implementation must bump DTMAPI from 0.2.8 to 0.2.9 and update controlled version sources/manifests/docs consistently.

## Per-Issue Manual QA Ledger

### Issue 1: AnimalHusbandryProgress still flashes the native mood row

Original feedback:

- Animal hidden-product UI still shows `心情` once when switching animals.
- Need detailed comparison with the older DLKsmapi animal bell implementation, which is currently reliable.

Review facts:

- DLKsmapi registers an animal viewer rendering event and applies progress bars through a runtime-managed prefix/postfix lifecycle.
- Current DTMAPI stores render rows from `AnimalFullInfoData`, then in `AnimalViewer.Show` postfix clones or overlays progress rows.
- This means the native mood row can be visible for at least one frame before DTMAPI replaces/adds the hidden-product row.
- The likely failure layer is DTMAPI GameBridge/AnimalViewer lifecycle, not the AnimalHusbandryProgress mod data lookup.

Required implementation:

- Review DLKsmapi behavior only as a reference; do not copy its code.
- Rework the DTMAPI AnimalViewer hook path so hidden-product rows are ready before the visible frame changes.
- Avoid writing hidden-product data into native mood fields.
- Keep hunger, mood, and hidden product as separate readable rows.
- Preserve native-sized text where possible; do not shrink the row so much that readability suffers.

Acceptance:

- Third save, open animal bell UI and switch across at least five animals.
- No first-frame `心情` flicker before hidden-product display.
- Hidden product row remains readable and independent from mood.
- Screenshot or short recording evidence captures the corrected behavior.

Blocker:

- If the native viewer cannot be safely intercepted before the first visible frame, report the exact hook path and leave the goal incomplete.

### Issue 2: Y-console expanded item grid did not expand side filters, and item icons are off-center

Original feedback:

- Y-console item rows increased, but the left `来源` and `子分类` filter columns did not increase rows.
- Item sprites are no longer visually centered in their grid cells.

Review facts:

- `ReflectedDebugConsoleUi` increased item `PageSize`, but source/category page-size constants remained smaller.
- Item icons are placed with fixed coordinates inside a fixed cell, which is fragile when sprite transparent bounds differ.

Required implementation:

- Increase source and category filter row counts to visually match the taller item grid.
- Keep filter pagination usable and aligned.
- Re-center item sprites with a stable icon viewport or bounds-aware centering, not a one-off offset.
- Preserve existing Y-console behavior: source/category filtering, search, hover, left/right give, weather, teleport, time, save, and input isolation.

Acceptance:

- Y-console screenshot shows the item grid and the two filter columns filling a coherent height.
- At least ten visually different item sprites are centered in their cells.
- No UI overlap with pagination or right-side controls.

Blocker:

- If sprite transparent bounds make generic centering impossible, document the fallback strategy and do not mark the item complete with only one tuned example.

### Issue 3: Mine preview is still wrong and Mine does not use real official electricity

Original feedback:

- Mine placement preview still does not scale correctly.
- Mine production default `120` minutes should mean every two in-game hours.
- Sleeping or passing time sometimes does not produce; behavior is inconsistent by room/time-skip path.
- Mine appears not to truly consume electricity: official power UI still shows no Mine load.

Review facts:

- MineMod passes electric configuration to DTMAPI Machine API.
- Current GameBridge records internal machine fields such as last electric cost, but code review did not find clear integration with the official electric grid/battery load.
- Current preview hook targets the equipment builder indicator path, but the user's held/placement preview uses a different visible object or timing.
- Previous time-skip review found production can differ depending on whether the active room is the Mine room, another room, sleep, or Y-console time jump.

Required implementation:

- Identify the official electricity path used by native powered equipment.
- Connect Mine to the real official load/consumption path, or explicitly block if unsafe.
- Official power UI must include Mine load when Mine is placed and active.
- No-power/insufficient-power states must stop production and show/log a reason.
- Fix the held placement preview so it is 2x before placement, not only after placement.
- Confirm the 120-minute default means every two in-game hours, including sleep/pass-time/cross-room paths.

Acceptance:

- Holding Mine before placement shows a 2x preview.
- Placed Mine remains 2x without randomly scaling other objects.
- Official power UI shows Mine electric load and/or battery consumption.
- In third save, Mine produces after each expected two-hour interval across active-room pass-time, other-room pass-time, and sleep.
- Logs identify production, skipped production, and power reason.

Blocker:

- If DTMAPI cannot safely register a custom machine with official electricity, leave Mine incomplete and report the exact official class/API boundary.

### Issue 4: MoreEquipmentSlots must be save-safe and config-minimal

Original feedback:

- Extra equipment effects are now confirmed, but safety is not.
- Config menu should remove everything except `启用`; safety recovery must be always on and not user-toggleable.
- Serious safety issue: putting a hat into an extra slot without saving still persisted DTMAPI data; re-entering the save produced two hats.
- Closing/disable behavior cannot rely on hot-disable; disabled mod recovery likely needs DTMAPI Core/GameBridge orphan recovery on next load.

Review facts:

- MoreEquipmentSlots currently exposes debug/action controls in config.
- GameBridge writes extra-slot storage immediately to a DTMAPI config file.
- Native backpack changes roll back if the game is not saved, but DTMAPI extra-slot storage does not roll back with the save transaction.
- This transaction mismatch can duplicate or pollute items.

Required implementation:

- Config UI for MoreEquipmentSlots must expose only `启用`.
- Force safe recovery on internally; remove the visible recovery toggle and debug buttons.
- Bind extra-slot persistence to real game save transactions.
- Do not persist slot storage if the player exits/reloads without saving.
- Add orphan recovery handled by DTMAPI Core/GameBridge when the mod is disabled: recover extra-slot items to backpack or ground on next safe load.
- Backpacks-full recovery must not delete items.
- Hat slots must apply special effects only and must not duplicate visible hat equipment.

Acceptance:

- Put a hat/accessory into an extra slot, exit without saving, reload: no duplicate item and no stale extra-slot data.
- Put a hat/accessory into an extra slot, save, reload: item remains in the extra slot and is not duplicated.
- Disable MoreEquipmentSlots, reload/save path: stored extra-slot items return to backpack or ground safely.
- Config page only shows `启用`.

Blocker:

- If extra-slot state cannot be made transaction-safe, disable extra-slot storage or leave the feature incomplete. Do not keep a duplication-prone implementation.

### Issue 5: Add a More Saves mod through the official save UI path

Original feedback:

- Add a new More Saves mod.
- Use the official path: only extend/paginate the official save UI and allow reading more local saves.
- Disabling the mod must not delete local save files.
- Saves must be runnable.

Review facts:

- Official decompiled code uses a fixed archive count, archive file naming, and a save UI panel that renders an array of archive info.
- It may be feasible to expand the official archive count or `GetAllArchiveInfos` result while reusing the official save panel.
- UI scrolling/paging for many slots still needs validation.

Required implementation:

- Create a DTMAPI official-local More Saves mod.
- Prefer official save UI and official archive file format.
- Add paging/scrolling only where needed; do not replace the whole save system.
- Disabling the mod must leave extra save files untouched.
- The first six vanilla slots must remain compatible.
- Extra slots must support create, load, save, copy/delete if the official UI supports these actions safely.

Acceptance:

- Official save UI shows more than six saves with clear paging/scrolling.
- Extra save slots can be created and loaded.
- Disabling the mod hides/ignores extra slots without deleting files or breaking the first six slots.
- Logs record slot count, file paths, and any unsupported action.

Blocker:

- If official save UI cannot safely handle more slots, report the exact UI/data path and stop before altering save files.

## Problem Grouping

- UI: AnimalViewer first-frame flicker, Y-console filter height/icon centering, MoreEquipmentSlots config simplification, More Saves official save UI.
- API/GameBridge: AnimalViewer lifecycle, Machine real electricity, EquipmentSlots save transaction/orphan recovery, Save UI/archive count.
- Config/content: MoreEquipmentSlots only `Enabled`; Mine electric semantics must match official systems.
- Testing/evidence: third-save screenshots/logs for all player-visible issues; save safety needs explicit no-save/reload and disable/reload checks.

## Boundary Constraints

Must do:

- Bump DTMAPI to 0.2.9.
- Keep this round scoped to the five issues above.
- Read the 20260606 follow-up review record before editing.
- Update `docs/updates`, `docs/debug`, smoke matrix, hook map/API matrix when relevant.
- Verify in the third local save unless testing the official save menu before save selection.

Must not do:

- Do not rework ActionSpeed or OneActionComplete unless a new regression is reported.
- Do not publish or copy reverse/decompiled/reference code.
- Do not treat internal Mine telemetry as real electricity.
- Do not keep MoreEquipmentSlots storage that can persist outside game saves.
- Do not delete extra save files when More Saves is disabled.
- Do not mark complete with only final-state screenshots when the bug is flicker/lifecycle.

## Implementation Tasks

### Task A: Baseline and 0.2.9 version bump

- Run `git status` first.
- Preserve user/other-Codex changes.
- Bump DTMAPI from 0.2.8 to 0.2.9 across controlled version sources and relevant official-local manifests.
- Treat 0.2.8 smoke as historical evidence only where user manual QA contradicts it.

### Task B: AnimalViewer lifecycle fix

- Compare DLKsmapi behavior as reference.
- Fix DTMAPI AnimalViewer rendering so hidden-product rows appear without `心情` flicker and without mood-row hijacking.

### Task C: Y-console layout correction

- Expand source/category filter rows with the item grid.
- Re-center item icons robustly.
- Preserve existing console functions and input isolation.

### Task D: Mine preview and real electricity

- Fix held placement preview scale.
- Connect Mine to the official electricity system or report blocker.
- Verify two-hour production across active room, other room, and sleep/pass-time paths.

### Task E: MoreEquipmentSlots save-safety rewrite

- Reduce config to `Enabled`.
- Make extra-slot persistence save-transaction-safe.
- Implement forced safe recovery and orphan recovery for disabled mod states.

### Task F: More Saves official-path mod

- Add a DTMAPI official-local More Saves mod using the official save UI/archive format.
- Ensure disabling the mod never deletes local save files.

### Task G: Validation and documentation closeout

- Release build and UnitTests pass with 0 errors.
- Third-save or official-save-menu smoke covers each player-visible issue.
- Capture screenshots/logs for AnimalViewer, Y-console, Mine preview/electricity/production, MoreEquipmentSlots save safety, and More Saves.
- Exit check shows no leftover `DolocTown.exe`.
- Update docs/updates, docs/debug, smoke matrix, hook map, and API matrix.

## Completion Standard

Only mark complete when Tasks A-G are player-visible and genuinely verified, with build, game smoke/save-menu smoke, logs/screenshots, exit cleanup, and documentation complete.

Do not count as complete:

- old 0.2.8 smoke evidence without fresh reproduction;
- Animal UI that eventually becomes correct but still flashes `心情`;
- Y-console item grid expansion that leaves side filters short or icons off-center;
- Mine internal telemetry without official electricity UI/load/consumption;
- MoreEquipmentSlots that can duplicate items after no-save reload;
- More Saves that works only by deleting, moving, or rewriting existing save files unsafely.
