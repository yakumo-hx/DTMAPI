# 20260607-0013 - Archive 0.4.1 Manual QA Handoff And Unblock API Rebuild

Date: 2026-06-07
Status: implemented
Scope: docs/goals/api

## Source Request

The user clarified that the `0.4.1` handoff was primarily a manual-testing record that should be archived and not implemented, because the project is moving directly into API rebuild work.

## Changed Files

- Updated `docs/goals/2026/20260607-0001-041-ui-save-mine-animal-refactor.md` to mark it as an archived manual-QA handoff, not an active implementation goal.
- Updated `docs/goals/2026/20260607-0001-041-ui-save-mine-animal-refactor.goal.txt` with an archived-prompt warning.
- Updated `docs/goals/2026/20260607-0002-042-camerazoom-api-rebuild.md` so the archived `0.4.1` handoff no longer blocks direct `0.4.0` -> `0.4.2` API rebuild work.
- Updated `docs/goals/2026/20260607-0002-042-camerazoom-api-rebuild.goal.txt` with the same unblocked version requirement.
- Added this update record and linked it from `docs/updates/INDEX.md`.

## Summary

- Resolved the active-goal/version blocker caused by the 0.4.1 handoff file being labeled active.
- Preserved the 0.4.1 manual QA content as historical context instead of deleting it.
- Kept CameraZoom as the next API rebuild handoff with fixed target `0.4.2`.

## Validation

- Documentation-only update.
- Ran `git diff --check`; no whitespace errors were reported. Git emitted existing LF/CRLF warnings for touched Markdown files.
- No build or game smoke was run because no runtime/API/mod/game files were changed.

## Rollback

Revert the status text in the 0.4.1 goal and prompt, restore the previous active-goal guard in the CameraZoom 0.4.2 goal/prompt, remove this update record, and remove the `20260607-0013` row from `docs/updates/INDEX.md`.
