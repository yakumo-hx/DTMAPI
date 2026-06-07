# 20260607-0004 - 0.4.1 UI Save Mine Animal Refactor Goal

Status: implemented
Date: 2026-06-07
Area: docs/reviews/goals/ui/save/mine/animal

## Source Request

The user requested review, durable recording, and conversion into a goal document before the next bottom-layer refactor. The feedback covered:

- MoreSaves 24-slot save UI overflow and the need for a scrollable menu above 12 slots.
- DTMAPI title-page entry style/name change to official-like `模组设置`.
- Mine pure-electric regression, ConfigMenu scrolling, enabled-mod list truncation, and Mine scale lifecycle/preview regressions.
- AnimalHusbandryProgress hidden-produce row flickering from `心情`, with old DLK behavior to be analyzed as read-only reference.

## Changed Files

- Added `docs/reviews/manual-qa/2026/20260607-0002-ui-save-mine-animal-refactor-review.md`.
- Added `docs/goals/2026/20260607-0001-041-ui-save-mine-animal-refactor.md`.
- Added `docs/goals/2026/20260607-0001-041-ui-save-mine-animal-refactor.goal.txt`.
- Added this update record.
- Linked this record from `docs/updates/INDEX.md`.

## Notes

- This is a documentation-only handoff. No runtime, hook, UI, mod, package, or version source was changed.
- The goal fixes the implementation target at `0.4.1`.
- The review preserves the user's four numbered issues in order and attaches code/path analysis directly under each issue.
- Old DLK AnimalHusbandryProgress files were inspected only for behavior and lifecycle lessons. No old DLK implementation was copied.
- The goal requires the implementation Codex to treat the reported lifecycle/flicker/transition failures as mandatory player-visible gates, not final-state-only screenshots.

## Validation

- Documentation files were created and the update index was amended.
- Build and game smoke were not run because this step did not change runtime code.
- No implementation completion is claimed by this record.

## Evidence

- Review record: `docs/reviews/manual-qa/2026/20260607-0002-ui-save-mine-animal-refactor-review.md`.
- Goal file: `docs/goals/2026/20260607-0001-041-ui-save-mine-animal-refactor.md`.
- Prompt backup: `docs/goals/2026/20260607-0001-041-ui-save-mine-animal-refactor.goal.txt`.

## Rollback

Remove the review file, goal file, prompt backup, this update record, and the index row if this handoff is superseded before use.

## Follow-Up

The implementation Codex should execute only the short `/goal` prompt stored in the sibling `.goal.txt` file and use the detailed `.md` goal as the single task source.
