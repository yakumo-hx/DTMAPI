# 20260705-0011 Debug Phase Summary

## Status

recorded/docs-only

## Source Request

User requested a handoff-style review of the debug documentation from the earliest records, with a staged summary that states each bug, how it was solved, whether it was newly introduced, and whether technical debt remains.

## Changed Files

- `docs/debug/phase-summary-20260705.md`
- `docs/debug/INDEX.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260705-0011-debug-phase-summary.md`

## Summary

Added a durable debug phase summary covering:

- formal issue ledgers `ISSUE-001` through `ISSUE-011`;
- index-only high-risk records such as AutoFishing follow-ups, MoreEquipmentSlots, Workshop upload, SecondMotor retirement, installer failures, Shell Crab animator/sleep fixes, and custom animal sleep diagnostics;
- stage-level conclusions and handoff rules for future debug/runtime work.

This is a documentation synthesis only. It does not claim any issue is solved and does not change runtime behavior.

## Validation

- Source documents were reviewed before writing the summary:
  - `PROJECT.md`
  - `docs/planning/DolocTownModdingAPI.md`
  - `docs/planning/Debug.md`
  - `references/README.md`
  - `docs/debug/INDEX.md`
  - `docs/debug/issues/*.md`
  - `docs/debug/lessons.md`
  - `docs/debug/regressions/smoke-matrix.md`
  - `docs/reviews/README.md`
- No build or game smoke was run because this update is docs-only and does not change source, scripts, packaging, or runtime behavior.

## Evidence Links

- Summary: `docs/debug/phase-summary-20260705.md`
- Debug index entry: `docs/debug/INDEX.md`

## Rollback

Remove `docs/debug/phase-summary-20260705.md`, remove its entry from `docs/debug/INDEX.md`, remove this update record, and remove the `20260705-0011` row from `docs/updates/INDEX.md`.

## Follow-up

Future debug summaries should either update this file with a dated appendix or add a new phase-summary file when the active issue set changes materially.
