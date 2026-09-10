# DTMAPI 0.4.1 UI Save Mine Animal Refactor Manual QA Archive

Status: archived manual-QA handoff; not an active implementation goal
Created: 2026-06-07
Target version: 0.4.1

Archived: 2026-06-07
Archive reason: The user clarified that this file primarily preserves one manual-testing feedback round. It should not block the later API rebuild track, and it should not be executed as the active 0.4.1 implementation goal unless the user explicitly reactivates it.

## Source Request

This file preserves a manual-QA implementation handoff that was later archived before implementation. The original handoff described bottom-layer refactoring after manual QA found four player-visible problems:

1. MoreSaves supports configurable counts, but 24 slots break the title save UI. Keep 12 slots usable and add a scrollable save menu for more slots.
2. The DTMAPI title-page entry should visually follow official pixel text/buttons and be named `模组设置`; the current dark `DTMAPI` icon button is not acceptable.
3. Mine still exposes fuel/hybrid settings instead of pure electric behavior; DTMAPI config UI overflows and likely truncates both the mod list and page items; Mine placed sprite shrinks during room transitions and the held placement preview remains original size.
4. AnimalHusbandryProgress still flickers from `心情` to hidden-produce text. The old DLK behavior must be used as read-only behavioral reference, not copied.

Detailed review source: `docs/reviews/manual-qa/2026/20260607-0002-ui-save-mine-animal-refactor-review.md`.

## Current Context

- Root `readme.md` is not part of the workflow and must not be used as a task ledger.
- This goal file is archived and is not the current detailed task source for implementation.
- The current controlled DTMAPI version is `0.4.0`.
- The fixed target originally written for this archived goal was `0.4.1`.
- Later API rebuild goals may proceed without treating this archived file as an active blocker.
- DTMAPI `0.3.1` was previously produced by a Codex version-bump mistake. Do not infer future target versions from that mistake.
- Existing 0.4.0 stable custom entity APIs must not be casually broken.
- Ordinary DTMAPI mods must not be moved under `BepInEx/plugins`.
- Fragile Unity/Harmony/reflection logic belongs in `DTMAPI.GameBridge.DolocTown` or the bootstrap UI host, not stable public API DTOs.

## Non-Goals

- Do not implement unrelated DolocPlus, zoom, chest, planting gun, monster, animal species, projectile, or drone features.
- Do not copy or imitate old DLKsmapi source code from `E:\Python_project\DLK\src`, `packages`, `archive`, or `tmp`. The old DLK animal records are behavior reference only.
- Do not modify third-party Workshop packages or official content files except read-only inspection or explicitly documented DTMAPI-owned generated/local mod artifacts.
- Do not hide required failures behind "experimental" or "pending"; the user-visible requirements in this file are mandatory.
- Do not mark complete from build success or final-state screenshots only. Lifecycle flicker and transition shrink must be tested for the moment they occur.

## Task A: Baseline, Safety, And 0.4.1 Version Target

- Run `git status` before editing and preserve unrelated user/Codex changes.
- Read all files named in the short `/goal` prompt plus this goal and the review record.
- Inventory controlled version sources and bump only from `0.4.0` to fixed target `0.4.1` if the workspace is still at `0.4.0`.
- Record old/new versions and touched version files in the update record.
- Establish a baseline of current code paths before changing hooks:
  - title/config UI path in `ReflectedTitleMenuSettingsUi`;
  - save slot API/UI hook path around `Save.MoreSlotsApi`;
  - machine definition/runtime/visual scale path;
  - animal viewer hook path.

Acceptance:

- The implementation can state exactly which version files changed.
- No unrelated feature work or unrelated mod edits are mixed into this goal.
- Existing user/previous-Codex dirty files were not reverted.

## Task B: MoreSaves Scrollable Save UI

Requirement:

- Preserve official 6-slot behavior and DTMAPI 12-slot behavior.
- For slot counts above 12, keep the save screen contained and add scroll/page navigation so all requested slots are reachable without off-screen cards.

Implementation expectations:

- Keep archive discovery/load/delete/copy behavior aligned with official `LocalSave` and save panel paths.
- Do not let DTMAPI take over save files with a separate incompatible storage model.
- Add a DTMAPI-owned UI adaptation only where needed to contain the official save cards.
- Keep requested slot count clamped to the existing safe range unless review finds a stronger limit is required.
- Capture operation evidence for selecting/loading/copying/deleting the intended visible slot or explain any blocked operation.

Acceptance:

- Vanilla 6-slot UI still works.
- DTMAPI 12-slot UI still works without scroll regressions.
- DTMAPI 24-slot UI displays within the title save panel, exposes a visible scroll/page affordance, and can reach slots 1-24.
- No save card is clipped by the screen edge at 1920x1080.
- Third-save smoke includes startup log, save UI evidence, slot count evidence, and clean exit.

Blocker:

- If official save panel internals cannot be safely adapted without breaking load/delete/copy targeting, stop and document the exact hook/UI blocker.

## Task C: DTMAPI Title And Config Menu UI Refactor

Requirement:

- Replace the current title-page `DTMAPI` icon button with an official-style pixel button named `模组设置`.
- Add scrolling/viewport behavior for DTMAPI config pages and long option lists.

Implementation expectations:

- Prefer official title-page font/sprite/button references when safely available through reflection.
- If official resources cannot be resolved reliably, create a close reflected-UI fallback that matches official pixel text/proportions better than the current dark icon rectangle.
- Icon/image is optional and should not force layout problems.
- Remove hard-coded truncation as the only access path for enabled mod config pages and config items.
- Provide scrollbars or clear page navigation for:
  - the left enabled-mod/config-page list;
  - the right selected config page item list;
  - any other DTMAPI settings page with a similar hard cap when the same helper can cover it safely.
- Ensure text and controls stay within panel bounds across Chinese labels.

Acceptance:

- Title homepage shows `模组设置`, not `DTMAPI`.
- The title button visually resembles official pixel UI enough to fit beside `开始旅程` / `简体中文` style controls.
- Opening the button still opens the same DTMAPI settings menu.
- With more enabled mods than the visible left list height, all entries are reachable.
- With Mine's long config page or another long page, all options are reachable without overflowing beyond the panel.
- Screenshot evidence covers title button, left-list scroll, and right-page scroll.

Blocker:

- If Unity UI reflection cannot support a real scrollbar, implement deterministic page navigation instead and document the fallback.

## Task D: Mine Pure-Electric Runtime And Config Cleanup

Requirement:

- Mine must be a pure-electric machine. Remove player-facing fuel mode, fuel capacity, fuel cost, and electric-mode fuel settings.
- Mine production must not require or consume DTMAPI fuel.

Implementation expectations:

- Update `testmods/MineMod` config registration, default config, normalization, status text, translations, and machine definition.
- Set Mine definition to electric-only: no fuel mode, default electric, power cost 10 per cycle unless user config changes an allowed electric-only power field.
- Cleanly ignore or migrate stale persisted Mine config values so old `DefaultMode=fuel`, fuel capacity, or electric-fuel cost cannot re-enable hybrid behavior.
- Update GameBridge `MachineDefinition` normalization/runtime if the current minimum `FuelCapacity=1` or fuel-gating semantics prevent a true no-fuel electric-only machine.
- Preserve existing Mine output rules, recipe behavior, Oil recipe option if still appropriate, and storage behavior unless they directly conflict with pure-electric operation.
- Adjust logs/status to avoid presenting fuel as a required Mine resource.

Acceptance:

- Mine config menu no longer displays `默认模式`, `燃料容量`, `燃料模式消耗`, or `耗电模式燃料`.
- Mine status/logs show electric-only mode and electric cost, with no "skipped because DTMAPI fuel state is empty" path.
- A placed Mine produces through official electric power consumption only.
- Existing old config files do not resurrect fuel UI or fuel runtime.

Blocker:

- If the public machine API cannot represent electric-only/no-fuel machines without breaking other machines, stop and refactor the API contract explicitly instead of hard-coding a Mine-only bypass.

## Task E: Mine Visual Scale Lifecycle And Placement Preview

Requirement:

- Mine placed sprite and held placement preview must stay at the intended 2x visual scale.
- The fix must cover scene transitions, especially exiting rooms, and first visible frames.

Implementation expectations:

- Review the official equipment renderer, builder indicator, and room-transition paths before changing hooks.
- Apply Mine visual scale at every relevant render/reuse/indicator lifecycle, not only during machine production polling.
- Ensure non-Mine equipment is reset to its original/native scale and is not globally enlarged.
- Make preview identification robust against builder prototype/id/name differences.
- Capture evidence that specifically covers:
  - held placement preview before placement;
  - placed Mine immediately after room transition/exit;
  - placed Mine after runtime has settled;
  - nearby non-Mine equipment remaining normal.

Acceptance:

- The held Mine preview is 2x before placement.
- The placed Mine is 2x on first visible frame after room transition and remains 2x afterward.
- No non-Mine equipment is accidentally scaled.
- Hook map and smoke matrix record the final hook path and evidence.

Blocker:

- A final settled screenshot is not sufficient if the Mine visibly shrinks first. If first-frame proof cannot be automated, collect manual/video-style evidence or report the evidence blocker.

## Task F: Animal Viewer Hidden-Produce Flicker Root Fix

Requirement:

- AnimalHusbandryProgress hidden-produce row must not visibly show `心情` before changing to the correct hidden-produce label/text.

Implementation expectations:

- Use old DLK records only as behavior reference:
  - `E:\Python_project\DLK\src\mods\AnimalHusbandryProgressMod\README.md`;
  - `E:\Python_project\DLK\src\mods\AnimalHusbandryProgressMod\MAINTENANCE.md`;
  - `E:\Python_project\DLK\docs\releases\DolocTownSMAPI-0.8.21-animals-api.md`.
- Do not copy old source or implementation.
- Keep the ordinary AnimalHusbandryProgress mod event/config usage narrow; Runtime/GameBridge should own fragile UI mapping, cloning, localization, and lifecycle.
- Ensure cloned extension progress bars are inactive while title/progress/color are assigned, then activated only after custom values are stable.
- Investigate and handle cloned localization components that can restore the native mood title on first activation.
- Remove or quarantine legacy single-pass mood override behavior if it can reintroduce the old `moodInfo`/`moodProgress` path.
- Preserve the native mood row; the hidden-produce row should be independent.

Acceptance:

- Fresh launch, first animal selection, and repeated animal switches never visibly show `心情` on the hidden-produce row before correction.
- Native mood information remains available and is not overwritten as the hidden-produce row.
- Evidence includes logs plus screenshot/video/manual timing proof that checks the flicker, not only final text.
- Hook map and smoke matrix reflect the final animal viewer lifecycle.

Blocker:

- If the flicker cannot be reproduced locally, keep the goal incomplete unless another evidence path proves the exact user-reported first-visible-text condition.

## Task G: Validation, Evidence, And Documentation

Required validation:

- Build the solution and run relevant tests.
- Launch local Doloc Town using configured paths, not hard-coded Steam paths.
- Use the local game's third save slot unless the user gives a different instruction.
- Capture DTMAPI startup log, relevant HookProbe/TestMod log lines, third-save load evidence, feature screenshots/logs for each task, and clean exit evidence.
- Confirm no leftover `DolocTown.exe` and no Steam waiting-for-exit regression.

Required docs after implementation:

- Add a new implementation update record under `docs/updates/2026/` and link it from `docs/updates/INDEX.md`.
- Update `docs/debug/INDEX.md` and relevant debug records if hooks/runtime lifecycle changed.
- Update `docs/debug/regressions/smoke-matrix.md` with the new smoke evidence.
- Update `docs/hook-map/README.md` for save UI, Mine visual/electric, and animal viewer hook changes.
- Update `docs/api/public-api-matrix.md` if `ISaveSlotsApi`, `IMachineProductionApi`, config menu behavior, or animal viewer API semantics change.
- Record any rollback notes and follow-up blockers.

Completion standard:

- Mark complete only when all four user-reported numbered issues are fixed in player-visible behavior and documented with evidence.
- If any mandatory item remains unresolved, keep the goal incomplete and report blockers, logs, validated facts, and next steps.
