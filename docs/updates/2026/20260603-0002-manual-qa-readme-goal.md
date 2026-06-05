# 20260603-0002 - Manual QA Readme And Goal Prompt

## Metadata

- Update ID: 20260603-0002
- Date: 2026-06-03
- Status: implemented
- Source: User requested execution of the upgraded feedback-to-goal workflow: update `readme.md` with manual QA details and output a short `/goal` prompt.
- Owner: Codex

## Summary

- Replaced the previous next-goal `readme.md` content with a detailed DTMAPI 0.2.3 manual-QA productization ledger.
- Preserved the user's five numbered feedback groups in order.
- Converted screenshot-only observations into written task context.
- Attached per-item review records with confirmed facts, screenshot/log observations, Codex inferences, ownership classification, update needs, acceptance checks, and blocker criteria.
- Condensed the implementation work into tasks A-G for a short `/goal` prompt.

## User-Visible Impact

- A fresh implementation Codex can read `readme.md` and understand the exact manual test failures without relying on chat context or images.
- The next `/goal` can remain short while still carrying the full task requirements through `readme.md`.
- The next implementation goal explicitly requires a DTMAPI version bump, recommended 0.2.2 -> 0.2.3.

## Changed Files

- `readme.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260603-0002-manual-qa-readme-goal.md`

## Validation

- Documentation/task-ledger-only change.
- Build and game smoke were not run because no runtime, hook, API, script, package, installer, or game behavior changed.
- Manual document check: `readme.md` contains the required manual-test header, per-item review blocks, task A-G details, and completion standard.

## Evidence

- N/A for runtime evidence.

## Related Records

- Workflow rule: `docs/workflows/codex-feedback-to-goal.md`
- Previous workflow update: `docs/updates/2026/20260603-0001-feedback-review-workflow.md`
- Latest implementation update: `docs/updates/2026/20260602-0003-content-index-second-motor.md`

## Rollback Notes

- Restore the previous `readme.md` task list if this manual-QA productization goal is superseded before implementation starts.

## Follow-Up

- The implementation Codex should update `docs/debug`, smoke matrix, hook map, API matrix, and a new runtime update record after making code changes and validating in game.
