# Goal: Title UI, MoreSaves 12, AutoFishing Fix

## Objective

Implement the user's 2026-06-12 plan for the current bottom-layer refactor branch:

- restore the title-page DTMAPI entry as a compact icon button;
- preserve DTMAPI config menu paging while forcing the official homepage menu back to single-column;
- make MoreSaves fixed at 12 total official save slots with no player-facing slot-count setting;
- make AutoFishing visible sub-features work independently and validate combinations.

## Target Version

- Current target version remains `0.5.1-alpha` / `0.5.1.0`.
- If the worktree is already at `0.5.1-alpha`, do not bump again.
- If a controlled version source is neither `0.5.0-alpha` nor `0.5.1-alpha`, stop and report blocker.

## Required Reading

- `AGENTS.md`
- `PROJECT.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/planning/Debug.md`
- `references/README.md`
- `docs/workflows/codex-feedback-to-goal.md`
- `docs/goals/README.md`
- `docs/reviews/README.md`
- `docs/reviews/manual-qa/2026/20260612-0004-title-saveslots-autofishing-regression-review.md`
- `docs/reviews/manual-qa/2026/20260611-0001-refactor-manual-qa-code-review.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260612-0012-bottom-layer-refactor-audit-implementation.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Safety Constraints

- Start with `git status --short --branch`; do not reset/revert user or prior Codex changes.
- Keep existing bottom-layer refactor work in the dirty worktree unless a changed file must be edited for this task.
- Ordinary DTMAPI mods must not be placed under `BepInEx/plugins`.
- Fragile Unity/Harmony/reflection logic belongs in GameBridge or bootstrap UI host.
- Public APIs must remain stable/experimental layered and must not expose raw decompiled types.
- Keep FishingAutomation and SaveSlots `Experimental`; do not promote from this fix.
- Do not use broad global UI layout resets. The title layout guard must target the official homepage menu only while `HomePageUiState` is active.

## Tasks

### Task A - Title UI icon and official title menu guard

- Restore `iconSprite = LoadIconSprite()` in `ReflectedTitleMenuSettingsUi`.
- Render `DTMAPI.TitleSettings.Button` as a compact top-left icon button using `assets/branding/dtmapi-icon.png`.
- Use compact `DTMAPI` text only as fallback when icon loading fails.
- Remove the large text-only `模组设置` title button shape.
- Add a title-homepage layout guard that only runs on `HomePageUiState` and forces the native `HomePageTextMenu`/`TextMenu` layout constraint back to single-column.
- Preserve DTMAPI config menu and Manager paging.

### Task B - MoreSaves fixed 12 total slots

- Remove the MoreSaves slot-count config option from the DTMAPI config menu.
- Keep stale `SlotCount` in the config type only as migration compatibility; ignore saved values by normalizing to the fixed total count.
- Normalize enabled SaveSlots requests to exactly 12 total official slots; disabled remains vanilla 6.
- Update status text and i18n to say the current contract is fixed 12 total slots.
- Update `SaveSlotsOptions.SlotCount` API docs and API matrix to say it is accepted for compatibility but currently normalized to 12.

### Task C - AutoFishing independent features

- Make `InstantBite` only prepare/own bite timing.
- Make `SkipMiniGame` independently route any bite-ready state directly to `AgentStateFishingPull`, whether the bite came from InstantBite or native wait state.
- Make `AutoCompleteMiniGame` independently route a bite-ready fish into the real minigame when needed, then force native minigame success after the visible delay.
- Define precedence: `SkipMiniGame=true` wins over auto-complete and should not open battle UI.
- Strengthen `FastAnimations` so reachable cast/pull animation paths are applied and unsupported paths publish pending/failed smoke status instead of silent success.
- Update AutoFishing config labels/tooltips/i18n/README from "not independent / may no-op" to the new actual semantics.

### Task D - Tests and smoke support

- Add unit coverage for SaveSlots normalization to fixed 12 and disabled vanilla 6.
- Add unit coverage for AutoFishing option precedence.
- Extend smoke support for explicit AutoFishing scenarios:
  - `AutoCastOnly`
  - `InstantBiteOnly`
  - `SkipOnly`
  - `AutoCompleteOnly`
  - `FastAnimationsOnly`
  - `CombinedSkip`
  - `CombinedComplete`
- Update MoreSaves smoke evidence so fixed 12 records `archiveFileCount=12`, rendered slots >= 12, no pager active, and extra slot selection/load path evidence.

### Task E - Validation

Run:

- `git diff --check`
- `tools/scripts/build.ps1 -Configuration Release`
- `tools/scripts/test.ps1 -Configuration Release`
- Title smoke with screenshot proving compact icon and official single-column homepage menu.
- MoreSaves third-save smoke proving fixed 12 official save slots and no pager active.
- AutoFishing third-save scenario smokes for each single feature and both combinations.
- Final HookProbe/exit smoke: no leftover `DolocTown.exe`, no fatal window, no Steam waiting-for-exit regression evidence.

## Docs And Records

Update:

- `docs/updates/INDEX.md`
- a new `docs/updates/2026/20260612-NNNN-*.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Completion Standard

Only mark the active goal complete when the player-visible requirements above are implemented and validated in the third local save with logs/screenshots, Release build/test, `git diff --check`, clean process exit, and docs updates.

If any mandatory player-visible requirement cannot be proven, keep the goal incomplete and report blocker facts, logs/evidence ids, verified facts, and the next split.
