# 20260612-0017 - Title Homepage Layout Guard Removal

Status: implemented
Date: 2026-06-12
Branch: `codex/bottom-layer-refactor-audit-20260612`
Source request: user manual feedback that the official homepage single-column guard affected horizontal pause-menu icons.
Version: stays `0.5.1-alpha` / `0.5.1.0`; no version bump.

## Changed Files

- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260612-0017-title-homepage-layout-guard-removal.md`

## Summary

- Removed the reflected `HomePageTextMenu.ResetLayoutSize(1)` / `slotLayoutGroup.constraintCount=1` guard from the title UI.
- Kept the compact DTMAPI title icon button and DTMAPI config/Manager paging changes.
- Removed unused guard-only reflection helpers from `ReflectedTitleMenuSettingsUi`.
- Stopped claiming `UI.TitleHomeMenuLayout` as a current hook/status path; the prior `GAME-SMOKE/20260612-204350` remains historical evidence for the removed guard only.

## Known Facts And Rejected Hypotheses

- User feedback indicates the guard can affect an official horizontal icon layout outside the intended title homepage menu.
- The DTMAPI config menu paging itself is still accepted and was not reverted.
- The title icon restoration is independent of the removed official-layout guard.

## Validation

- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors; script output ended with `DTMAPI.UnitTests: OK`.
- Game smoke/manual QA not rerun in this change because the purpose is to let the user retest the affected official UI without the guard.

## Evidence

- Supersedes the current-behavior claim from `docs/updates/2026/20260612-0016-title-saveslots-autofishing-fix.md` for `UI.TitleHomeMenuLayout` only.
- Manual QA pending: verify pause-menu horizontal icons and title homepage layout after removing the guard.

## Rollback

- Reintroduce a title-only layout fix only after proving it cannot touch reused official menu/icon layout components. Prefer a narrower native-owner review or a screenshot-only diagnostic before any new layout mutation.

