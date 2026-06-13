# Bottom-Layer Refactor From Refactor Branch Audit

Status: completed 2026-06-12
Created: 2026-06-12
Target branch: `codex/bottom-layer-refactor-audit-20260612`
Target merge branch: `Refactor`
Target version: from `0.5.0-alpha` to `0.5.1-alpha`

## Source Request

The user asked to convert the current branch-wide code audit into an implementation prompt and emphasized that the next implementation must work on a branch.

This handoff is based on:

- `docs/reviews/code/2026/20260612-0001-refactor-branch-wide-code-audit.md`
- `docs/reviews/manual-qa/2026/20260611-0001-refactor-manual-qa-code-review.md`
- `docs/updates/2026/20260612-0010-refactor-branch-wide-code-audit.md`
- current API/debug records as of 2026-06-12.

## Required Reading

- `AGENTS.md`
- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/workflows/codex-feedback-to-goal.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/goals/README.md`
- `docs/goals/2026/20260612-0003-bottom-layer-refactor-from-audit.md`
- `docs/goals/2026/20260612-0003-bottom-layer-refactor-from-audit.goal.txt`
- `docs/reviews/README.md`
- `docs/reviews/code/2026/20260612-0001-refactor-branch-wide-code-audit.md`
- `docs/reviews/manual-qa/2026/20260611-0001-refactor-manual-qa-code-review.md`
- `docs/reviews/manual-qa/2026/20260607-0002-ui-save-mine-animal-refactor-review.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260612-0010-refactor-branch-wide-code-audit.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- relevant current decompiled Doloc Town build under `references/doloc-town/reverse/builds` before hook/native-owner work.

## Branch Rule

Do not implement this goal directly on `Refactor`.

Required sequence before code edits:

1. Run `git status --short --branch`.
2. Confirm the starting branch is `Refactor` or explain why it is not.
3. Create or switch to `codex/bottom-layer-refactor-audit-20260612` from the current `Refactor` state.
4. Preserve all existing user/Codex changes; do not reset, revert, or discard dirty files.
5. Record the actual branch name in the update record.

If branch creation is blocked by local state, stop and report the blocker instead of implementing on `Refactor`.

## Scope

Implement the bottom-layer refactor items that the audit found still open:

1. Shared title settings UI scrolling/paging and official-style `模组设置` entry.
2. SaveSlots 18+/24-slot official save UI paging/scrolling.
3. Mine pure-electric runtime/config migration.
4. Mine visual scale lifecycle and held placement preview.
5. AnimalViewer repeated-switch first-frame flicker closure.
6. Camera background/fog/panorama native-owner review, without unsafe runtime changes unless the owner path is proven and recorded.

## Non-Goals

- Do not copy or imitate old DLKsmapi source. Old DLK material may be read-only behavior reference only.
- Do not move ordinary DTMAPI mods into `BepInEx/plugins`.
- Do not take over official save-file formats or replace official `LocalSave` ownership.
- Do not mark CameraView stable or claim background sync complete from orthographic-size-only evidence.
- Do not rework AutoFishing, StrongPlantingGun, OilCoalDrop, ChestLocator, CropHarvesting, custom entities, Workshop packaging, installer behavior, or release staging unless required by this goal's validation/docs.
- Do not hide mandatory player-visible requirements behind `Experimental`, `pending`, or smoke-only success.

## Task A: Branch, Baseline, And Version

- Start from `Refactor`, then create/switch to `codex/bottom-layer-refactor-audit-20260612` before edits.
- Inventory controlled version sources:
  - `Directory.Build.props`
  - `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
  - `tools/scripts/release-common.ps1`
  - install/package scripts and selected manifest normalization points that currently write `0.5.0-alpha`.
- Bump only from `0.5.0-alpha` / `0.5.0.0` to fixed target `0.5.1-alpha` / `0.5.1.0`.
- If the workspace is already at `0.5.1-alpha`, do not bump again.
- If the workspace is not at `0.5.0-alpha` or `0.5.1-alpha`, stop and report the version blocker.
- Establish pre-change facts for:
  - `ReflectedTitleMenuSettingsUi` fixed-row config/Manager rendering;
  - SaveSlots `archiveFileCount` path;
  - Mine/MachineProduction fuel/electric state and visual scale hooks;
  - AnimalViewer overlay lifecycle;
  - CameraView background limitation.

Acceptance:

- The branch name and version old/new values are recorded in the update record.
- No implementation happens on `Refactor`.
- No unrelated user/Codex dirty files are reverted.

## Task B: Title Settings UI, Manager Lists, And Config Scrolling

Requirement:

- The title-page DTMAPI entry should look like an official-style pixel button and be named `模组设置`.
- Manager pages and config pages must no longer hide rows only because of fixed first-N rendering.

Implementation expectations:

- Replace or restyle the title-page DTMAPI entry so it visually fits official controls like `开始旅程` / `简体中文`; an image is optional.
- Add shared scrolling or deterministic paging for:
  - left enabled-mod/config-page list;
  - selected config page item list;
  - Manager Mods, Hooks, Features, Errors, and Warnings lists where current fixed row limits hide entries.
- Preserve current Manager Status/Logs export behavior.
- Keep Chinese text inside bounds and avoid overlapping controls.
- Do not expose unsupported internal edit APIs as public mod API.

Acceptance:

- Title homepage shows `模组设置`, not only a small `DTMAPI` dark icon/button.
- All enabled mod pages are reachable when more than the visible row count exists.
- Mine's long config page is fully reachable without overflowing the right panel.
- Manager Mods/Hooks/Features can expose all rows without losing count summaries.
- Screenshot evidence covers the title entry, config list scrolling/paging, config item scrolling/paging, and Manager full-list access.

Blocker:

- If true Unity scrollbars are unsafe through reflection, implement clear paging controls and document the fallback.

## Task C: SaveSlots 18+/24 Slot Paging

Requirement:

- Preserve native 6 slots and the currently acceptable 12-slot layout.
- For 18+ and 24 slots, add scroll/page navigation so all slots stay inside the save UI and are reachable.

Implementation expectations:

- Keep official `LocalSave`, `DataPersistenceManager`, `GameDataUiState`, and `GameDataPanel` ownership for archive discovery, render data, load, save, delete, and copy.
- Add only the smallest GameBridge/bootstrap UI adaptation needed to contain expanded slot cards.
- Keep save/load targeting correct across page/scroll changes.
- Protect the user-confirmed extra-slot save/load behavior.

Acceptance:

- 6-slot UI still works.
- 12-slot UI still works without scroll regressions.
- 18-slot and 24-slot UI fit within the title save panel at 1920x1080.
- Slots 1-24 are reachable.
- Save/load, delete, and copy target the intended slot or any unsupported operation is documented as a blocker with evidence.
- Third-save evidence includes clean startup, save UI evidence, and clean exit.

Blocker:

- If official save panel internals cannot be adapted without breaking load/delete/copy targeting, stop and document the exact blocker instead of replacing the save system.

## Task D: Mine Pure-Electric Runtime And Config Migration

Requirement:

- Mine must become pure electric.
- Remove player-facing fuel mode, fuel capacity, fuel mode cost, and electric-mode fuel settings.
- Runtime production must not require or consume DTMAPI fuel for Mine.

Implementation expectations:

- Update `testmods/MineMod` config registration, default config, normalization, status text, translations, README, and machine definition.
- Set Mine definition to electric-only, default electric, with electric power cost remaining configurable if safe.
- Migrate or ignore stale persisted old fuel fields so old configs cannot re-enable fuel/hybrid behavior.
- Update MachineProduction normalization/runtime if it cannot represent an electric-only/no-fuel Mine without hidden fuel gates.
- Preserve Mine output rules, recipe behavior, storage behavior, and Oil recipe toggle unless they directly conflict with pure-electric behavior.

Acceptance:

- Mine config menu no longer displays default mode, fuel capacity, fuel mode cost, or electric-mode fuel cost.
- Mine status/logs show electric-only behavior and electric cost.
- A placed Mine can produce through official electric power only.
- Logs no longer show Mine skipped because DTMAPI fuel state is empty.
- Old config files do not resurrect fuel UI or fuel runtime.

Blocker:

- If the public Machine API cannot express electric-only/no-fuel machines safely, stop and perform a focused API/GameBridge contract review before hard-coding a Mine-only bypass.

## Task E: Mine Visual Scale Lifecycle And Placement Preview

Requirement:

- Placed Mine sprite and held placement preview must stay at the intended large visual scale.
- The fix must cover scene transitions, especially exiting rooms, and first visible frames.

Implementation expectations:

- Review official equipment renderer, builder indicator, renderer reuse, and room-transition paths before changing hooks.
- Apply Mine visual scale at relevant render/reuse/indicator lifecycle points, not only during production polling.
- Reset non-Mine equipment to native scale and avoid global contamination.
- Make held preview identification robust against prototype/id/name differences.

Acceptance:

- Held Mine preview is large before placement.
- Placed Mine is large on the first visible frame after room transition and remains large afterward.
- Nearby non-Mine equipment remains normal size.
- Hook map and smoke matrix record final hook paths and evidence.

Blocker:

- A final settled screenshot is not enough if the Mine visibly shrinks first. If first-frame proof cannot be automated, collect targeted manual/video-style evidence or report the evidence blocker.

## Task F: AnimalViewer Hidden-Produce First-Frame Closure

Requirement:

- AnimalHusbandryProgress hidden-produce row must never visibly show `心情` before changing to the correct hidden-produce text.

Implementation expectations:

- Use old DLK behavior only as read-only reference if needed:
  - `E:\Python_project\DLK\src\mods\AnimalHusbandryProgressMod\README.md`
  - `E:\Python_project\DLK\src\mods\AnimalHusbandryProgressMod\MAINTENANCE.md`
  - `E:\Python_project\DLK\docs\releases\DolocTownSMAPI-0.8.21-animals-api.md`
- Do not copy old DLK source or implementation.
- Keep fragile UI mapping, localization disabling, clone activation order, and lifecycle behavior in GameBridge/bootstrap UI host.
- Preserve the native mood row; hidden produce remains an independent row.
- Build on the current cloned progress-row path and the removed dead single-pass `moodInfo` helper from the audit.

Acceptance:

- Fresh launch, first animal selection, and repeated animal switches do not show `心情` on the hidden-produce row before correction.
- Native mood information remains visible and is not overwritten.
- Evidence checks the flicker timing, not only final correct text.
- Smoke matrix and hook map record final AnimalViewer lifecycle evidence.

Blocker:

- If the flicker cannot be reproduced locally, keep the goal incomplete unless another evidence path proves the exact first-visible-text condition.

## Task G: Camera Background Native-Owner Review

Requirement:

- Do not treat CameraView movement success as background sync completion.
- Perform native-owner review for background/fog/panorama synchronization before any runtime change.

Implementation expectations:

- Read `docs/workflows/codex-api-rebuild.md`.
- Inspect current CameraView code, old CameraZoom historical failure records, and relevant native/decompiled camera/background/fog paths.
- Produce either:
  - a narrow safe implementation with evidence, if native ownership is clear and risk is contained; or
  - a dedicated API/native-owner review and follow-up goal if implementation is not safe in this batch.

Acceptance:

- The final update explicitly states whether Camera background sync was implemented, deferred, or blocked.
- No Stable promotion is made for `ICameraViewApi`.
- If no implementation is made, the review still names the likely native owner/state holder or states what remains unknown.

Blocker:

- If native-owner responsibility is unclear, do not patch background/fog/panorama behavior by trial-and-error.

## Task H: Validation, Evidence, And Documentation

Required validation:

- `git diff --check`
- `tools/scripts/build.ps1 -Configuration Release`
- `tools/scripts/test.ps1 -Configuration Release`
- Game validation through local configured paths, not hard-coded Steam paths.
- Use the third local save slot unless the user gives a different instruction.
- Capture:
  - DTMAPI startup log;
  - relevant HookProbe/TestMod log lines;
  - third-save load evidence;
  - screenshots/logs for title UI/config/Manager scrolling;
  - save UI 6/12/18/24 evidence;
  - Mine pure-electric production evidence;
  - Mine preview and transition scale evidence;
  - AnimalViewer repeated-switch first-frame evidence;
  - clean exit, no leftover `DolocTown.exe`, and no Steam waiting-for-exit regression.

Required documentation:

- Add a new implementation update record under `docs/updates/2026/` and link it from `docs/updates/INDEX.md`.
- Update `docs/debug/INDEX.md`.
- Update `docs/debug/regressions/smoke-matrix.md`.
- Update `docs/hook-map/README.md` for changed hooks/UI/native routes.
- Update `docs/api/public-api-matrix.md` for any API status/boundary changes; do not promote APIs without meeting promotion gates.
- Update relevant mod README/i18n/official metadata only for DTMAPI-owned local/test mods touched by this goal.

Completion standard:

- Do not mark complete from build success alone.
- Do not mark complete if only a subset of mandatory player-visible items is fixed.
- If one mandatory area is blocked, keep the goal incomplete and report verified facts, blocker evidence, and the safest next split.

## Completion Evidence

- Branch: implemented on `codex/bottom-layer-refactor-audit-20260612`, not directly on `Refactor`.
- Version: controlled sources were bumped from `0.5.0-alpha` / `0.5.0.0` to `0.5.1-alpha` / `0.5.1.0`; no second bump was applied.
- Static validation: `git diff --check` passed with line-ending warnings only.
- Build/test: `tools/scripts/build.ps1 -Configuration Release` and `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings, 0 errors, and `DTMAPI.UnitTests: OK`.
- Title/config/Manager: `GAME-SMOKE/20260612-165456` verifies the `模组设置` title entry and config screenshots through Mine/MoreEquipmentSlots; `GAME-SMOKE/20260612-170119` verifies Manager Status/Mods/Errors/Hooks/Features/Logs paging, summary copy/fallback, export, screenshots, and clean exit.
- SaveSlots: third-save DirectExe smokes `GAME-SMOKE/20260612-170240`, `20260612-170331`, `20260612-170421`, and `20260612-170512` verify official UI behavior at 6/12/18/24 slots, with 18/24 selecting expanded targets `1|13` and `1|13|24`.
- Mine: third-save DirectExe smoke `GAME-SMOKE/20260612-170708` verifies electric-only/no-fuel runtime (`electricOnly=True`, `fuel=disabled`), official JSON/API, production/storage, tech-tree UI, placement screenshot, and visual containment.
- AnimalViewer: third-save DirectExe smoke `GAME-SMOKE/20260612-170819` verifies repeated official viewer refresh/select and first-frame guard with `moodTitleHits=0`.
- Camera: native-owner review `docs/reviews/api/2026/20260612-camera-background-native-owner-review.md` defers background/fog/panorama runtime changes; `ICameraViewApi` remains Experimental.
- Exit/regression: Steam third-save HookProbe `GAME-SMOKE/20260612-170928` passed SaveLoaded and clean exit; process checks report no leftover `DolocTown.exe`, and no Steam waiting-for-exit symptom was observed by the harness.
- Traceability: update record `docs/updates/2026/20260612-0012-bottom-layer-refactor-audit-implementation.md`, smoke matrix row `BOTTOM-LAYER-REFACTOR-20260612`, debug index, hook map, and API matrix were updated.
