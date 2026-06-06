# 20260606-0012 - Goal files workflow and root task-ledger removal

## Status

Implemented as documentation/workflow change. No runtime code was changed.

## Source Request

- Stop using the unstable root task ledger as a feedback-review or implementation-goal ledger.
- Switch to one independent goal file per implementation handoff.
- Back up each short `/goal` prompt.
- Fully archive the old root task-ledger content after the transition.

## Summary

- Added `docs/goals/README.md` as the new goal-file workflow.
- Migrated the current root task ledger into `docs/goals/2026/20260606-0001-031-regression-new-content.md`.
- Added the short `/goal` backup at `docs/goals/2026/20260606-0001-031-regression-new-content.goal.txt`.
- Archived the previous root task-ledger snapshot at `docs/archive/task-ledgers/20260606-root-task-ledger-snapshot.md`.
- Removed the root task-ledger file entirely.
- Updated `AGENTS.md`, `docs/reviews/README.md`, and `docs/workflows/codex-feedback-to-goal.md` to prefer dedicated goal files and avoid mutable root task ledgers.
- Recorded that DTMAPI `0.3.1` was produced by a Codex automatic-versioning mistake and must not be used as a future versioning rule.
- Marked the migrated 0.3.1 goal and `.goal.txt` as archived audit material, not active implementation prompts.

## Changed Files

- `AGENTS.md`
- `docs/goals/README.md`
- `docs/goals/2026/20260606-0001-031-regression-new-content.md`
- `docs/goals/2026/20260606-0001-031-regression-new-content.goal.txt`
- `docs/archive/task-ledgers/20260606-root-task-ledger-snapshot.md`
- `docs/reviews/README.md`
- `docs/workflows/codex-feedback-to-goal.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260606-0012-goal-files-root-ledger-removed.md`

## Validation

- Documentation-only update.
- No build or game smoke was run.
- Checked that the current task ledger was copied before removing the root task-ledger file.

## Related Records

- `docs/updates/2026/20260606-0005-030-yconsole-newmods-goal.md`
- `docs/updates/2026/20260606-0007-031-regression-new-content-round.md`

## Rollback Notes

- Restore the archived root task-ledger snapshot only for historical inspection.
- Prefer not to roll back the workflow: the old mutable root-ledger pattern can corrupt multi-Codex goal context.

## Follow-Up

- Future feedback-to-goal turns should create `docs/goals/YYYY/YYYYMMDD-NNNN-short-slug.md` and a sibling `.goal.txt`.
- Short `/goal` prompts must reference the exact goal file and fixed target version.
