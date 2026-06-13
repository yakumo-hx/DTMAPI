# 20260612-0016 - Title Icon, Fixed MoreSaves 12, AutoFishing Independence

Status: verified
Date: 2026-06-12
Branch: `codex/bottom-layer-refactor-audit-20260612`
Source request: user follow-up plan "Title UI, MoreSaves 12, AutoFishing Fix Plan" plus manual QA regression review `docs/reviews/manual-qa/2026/20260612-0004-title-saveslots-autofishing-regression-review.md`.
Version: stays `0.5.1-alpha` / `0.5.1.0`; no additional version bump.

## Changed Files

- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`
- `testmods/MoreSavesMod/*`
- `testmods/AutoFishingMod/*`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/goals/2026/20260612-0004-title-saveslots-autofishing-fix.md`
- `docs/goals/2026/20260612-0004-title-saveslots-autofishing-fix.goal.txt`
- `docs/reviews/manual-qa/2026/20260612-0004-title-saveslots-autofishing-regression-review.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260612-0016-title-saveslots-autofishing-fix.md`

## Summary

- Restored the title homepage DTMAPI entry to a compact top-left icon button using `assets/branding/dtmapi-icon.png`, with a compact `DTMAPI` text fallback only when the icon cannot load.
- Added a title-homepage guard that calls the official `HomePageTextMenu.ResetLayoutSize(1)` path and constrains `slotLayoutGroup` back to one column while `HomePageUiState` is active. DTMAPI config menu paging remains unchanged.
- Fixed MoreSaves to exactly 12 total official save slots when enabled. The old `SlotCount` config field is migration-only and hidden from the player-facing config menu; disabled state remains the vanilla six slots.
- Updated `SaveSlotsOptions.SlotCount` docs to say the experimental compatibility field is normalized to 12 in enabled state.
- Reworked FishingAutomation wait-phase routing so `InstantBite` owns only bite timing, `SkipMiniGame` independently routes any bite-ready state to native pull/result, and `AutoCompleteMiniGame` independently enters or uses the real minigame before delayed native success.
- Defined combo precedence in code and unit tests: `SkipMiniGame=true` wins over `AutoCompleteMiniGame`.
- Strengthened FastAnimations by trying field/property/component animator paths, applying the same reachable-path logic immediately after native `UseFishRod`, and publishing `Smoke.AutoFishingAnimationSpeed=pending` instead of silent success when no path is writable.
- Extended AutoFishing smoke support with `-AutoFishingScenario` for `AutoCastOnly`, `InstantBiteOnly`, `SkipOnly`, `AutoCompleteOnly`, `FastAnimationsOnly`, `CombinedSkip`, and `CombinedComplete`.

## Known Facts And Rejected Hypotheses

- The DTMAPI config menu paging itself is accepted; only the official title homepage menu needed a single-column guard.
- "Extra save slots fixed to 12" is implemented as 12 total official slots, matching prior 12-slot evidence and the player's requested fixed count.
- MoreSaves does not need a player-facing slot-count control; keeping the DTO/config field is only for compatibility and migration.
- AutoFishing single-feature failures came from coupled wait-phase routing: `SkipMiniGame` previously depended on `InstantBite`, and `AutoCompleteMiniGame` was skipped when `SkipMiniGame` was also true.
- Camera background/fog/panorama native-owner behavior is not changed by this update.

## Validation

- `git diff --check` passed with line-ending warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors; script output ended with `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; script output ended with `DTMAPI.UnitTests: OK`.
- Title homepage smoke `GAME-SMOKE/20260612-204350` passed with `TitleSettingsButton=Passed`, `TitleSettingsButtonScreenshotFile=Passed`, `TitleSettingsMenu=Passed`, `TitleSettingsMenuScreenshotFile=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`. Logs include `UI.TitleHomeMenuLayout = verified` and screenshots under `DTMAPI-evidence/UI-004/20260612-204434/`.
- MoreSaves third-save smoke `GAME-SMOKE/20260612-203558` passed with `SaveLoaded=Passed`, `MoreSavesOfficialSaveUi=Passed`, `MoreSavesOfficialSaveUiEvidence=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`; logs report `archiveFileCount=12`, `panelSlotCount=12`, `renderedSlots=12`, and `visibleSlotsAfterPaging=12`.
- AutoFishing third-save scenario smokes passed:
  - `AutoCastOnly`: `GAME-SMOKE/20260612-204934`
  - `InstantBiteOnly`: `GAME-SMOKE/20260612-205040`
  - `SkipOnly`: `GAME-SMOKE/20260612-205147`
  - `AutoCompleteOnly`: `GAME-SMOKE/20260612-205248`
  - `FastAnimationsOnly`: first failed as intended with missing animation summary in retained attempt `GAME-SMOKE/20260612-205353`, then passed after the reachable auto-cast animator-path fix in `GAME-SMOKE/20260612-210544` with `AutoFishingAnimationSpeed=Passed`.
  - `CombinedSkip`: `GAME-SMOKE/20260612-210646`
  - `CombinedComplete`: `GAME-SMOKE/20260612-210744`
- Final Steam HookProbe/exit smoke `GAME-SMOKE/20260612-210900` passed with `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.

## Evidence

- Manual QA review: `docs/reviews/manual-qa/2026/20260612-0004-title-saveslots-autofishing-regression-review.md`.
- Implementation goal: `docs/goals/2026/20260612-0004-title-saveslots-autofishing-fix.md`.
- Regression row: `TITLE-SAVESLOTS-AUTOFISHING-FIX-20260612`.
- Runtime evidence: `GAME-SMOKE/20260612-204350`, `GAME-SMOKE/20260612-203558`, `GAME-SMOKE/20260612-204934`, `GAME-SMOKE/20260612-205040`, `GAME-SMOKE/20260612-205147`, `GAME-SMOKE/20260612-205248`, `GAME-SMOKE/20260612-210544`, `GAME-SMOKE/20260612-210646`, `GAME-SMOKE/20260612-210744`, and `GAME-SMOKE/20260612-210900`.

## Rollback

- Title UI rollback is limited to `ReflectedTitleMenuSettingsUi`; keep config/Manager paging unless a separate regression proves paging itself is at fault.
- MoreSaves rollback would re-expose the slot-count setting and variable `archiveFileCount`; do not do that without a new manual QA review because this update intentionally fixes the count at 12.
- AutoFishing rollback should not restore the coupled `InstantBite` -> skip/complete behavior; if one scenario regresses, isolate the scenario branch.

## Follow-Up

- Keep `IFishingAutomationApi` and `ISaveSlotsApi` Experimental.
- The seven AutoFishing scenario smokes are now verified on the third save.
- If a future native change removes the writable auto-cast/cast/pull animator path, keep `FastAnimationsOnly` failed/pending and split a native-owner follow-up instead of claiming success.
- Superseded for title homepage layout guard only: `docs/updates/2026/20260612-0017-title-homepage-layout-guard-removal.md` removes the reflected `HomePageTextMenu` single-column mutation after user feedback that it affected pause-menu horizontal icons.
