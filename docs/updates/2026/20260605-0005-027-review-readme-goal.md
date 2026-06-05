# 20260605-0005 0.2.7 Review-to-Goal Conversion

Status: implemented
Area: docs/reviews/readme/goal

## Source Request

The user requested an extra code-level review of Mine production under Y-console time skip, then asked to convert this review together with the prior Mine/Animal/Y-console/ConfigMenuExample review into the next implementation handoff.

## Changed Files

- `docs/reviews/manual-qa/2026/20260605-0002-mine-time-skip-production-review.md`
- `readme.md`
- `docs/updates/2026/20260605-0005-027-review-readme-goal.md`
- `docs/updates/INDEX.md`

## Summary

- Added a durable review record for Y-console `下一时间段` and DTMAPI Mine production behavior.
- Confirmed from code and decompiled reference that Y-console time skip uses native `ArchiveDataHandle.PassTimeNoControl` plus `DolocAPI.OnWakeUp`.
- Recorded the likely root cause split: official time flow is native, while DTMAPI Mine production is a separate current-room-scanned runtime loop with no robust pass-time catch-up.
- Rewrote `readme.md` into the 0.2.7 manual-QA root-cause task ledger covering both review rounds.

## Validation

No build or game smoke was run. This is documentation/review/prompt-preparation work only.

## Evidence

- Review record: `docs/reviews/manual-qa/2026/20260605-0002-mine-time-skip-production-review.md`
- Prior review record: `docs/reviews/manual-qa/2026/20260605-0001-mine-animal-yconsole-code-review.md`
- Task ledger: `readme.md`

## Rollback Notes

Revert this update record, the new review record, the index row, and the `readme.md` rewrite to return to the previous 0.2.6 task ledger.

## Follow-up

Use the short `/goal` prompt produced from this ledger for a fresh implementation Codex. The implementation Codex must update runtime/debug/API evidence and must not mark complete on smoke-only success.
