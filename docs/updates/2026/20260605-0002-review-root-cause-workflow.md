# 20260605-0002 Review Root-Cause Workflow

## Status

Implemented as documentation/workflow update. No runtime code was changed.

## Source Request

The user asked to improve the review workflow and file structure after observing that some bugs are fixed well while others repeat unless a code-level review exposes a deeper lifecycle/path issue.

## Changed Files

- `AGENTS.md`
- `docs/workflows/codex-feedback-to-goal.md`
- `docs/reviews/README.md`
- `docs/reviews/manual-qa/README.md`
- `docs/reviews/templates/manual-qa-review.md`

## Summary

- Added `docs/reviews` as a dedicated pre-implementation review layer for manual QA, root-cause, and code-path analysis.
- Clarified that `readme.md` is the current implementation ledger, not the archive for every review-only discussion.
- Added a review-first gate for repeated, previously "fixed", lifecycle, UI flicker, input, save/load, hook, vehicle, machine, official-content, and broad state-pollution issues.
- Made task numbering flexible; `任务 A-G` is now an example rather than a fixed requirement.
- Added a durable manual-QA review template with per-issue original feedback, screenshot/log transcription, code/doc facts, hypotheses, acceptance checks, and blocker conditions.
- Updated `AGENTS.md` so fresh Codex review agents read the review docs and create durable review records when needed.

## Validation

Documentation inspection only. No build, unit tests, game smoke, hook smoke, or runtime validation were run because this update only changes workflow documents.

## Evidence

- `docs/reviews/README.md` explains the new review layer and its relationship to `readme.md`, `docs/debug`, and `docs/updates`.
- `docs/reviews/templates/manual-qa-review.md` provides the reusable per-issue review template.
- `docs/workflows/codex-feedback-to-goal.md` now requires code-path review before generating new implementation goals for repeated or lifecycle-style bugs.

## Rollback

Revert the changed documentation files and remove this index entry. No runtime state or game files are affected.

## Follow-Up

When the next manual QA review is requested, create a concrete record under `docs/reviews/manual-qa/YYYY/` before updating `readme.md` if the issues are repeated, ambiguous, or lifecycle-related.

