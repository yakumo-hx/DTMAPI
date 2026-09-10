# 20260705-0007 Unity GC Boundary Correlation

Date: 2026-07-05
Status: recorded/docs-only
Area: docs/debug/issue-010/boundary

## Trigger

User asked to explore the current DTMAPI mainline and research the relationship between the current Unity/Mono GC fatal issue and unclear feature/API/content boundaries.

## Summary

Added a docs-only ISSUE-010 boundary-correlation research note. The note records that unclear boundaries are not proven as the direct GC root cause, but the current evidence strongly correlates the fatal profile with a larger stable process/title root graph created by combining CustomAnimals/AnimalVoice, action/utility features, Manbo audio, and UI feature owners. It also maps how current source already compensates with owner ledgers, lifecycle contracts, and object-delta snapshots, and recommends the next continuous one-hour idle UI-owner split.

## Changed Files

- `docs/reviews/code/2026/20260705-0002-issue010-boundary-correlation-research.md`
- `docs/updates/2026/20260705-0007-unity-gc-boundary-correlation.md`
- `docs/updates/INDEX.md`

## Evidence

- Reviewed current branch `codex/dtmapi-overall-refactor-20260702`.
- Reviewed ISSUE-010 and latest 2026-07-04/05 SaveLoad/title-return/lifecycle records.
- Checked current source around Bootstrap process roots, GameBridge feature host, owner-bound event/input/config/API cleanup, title-return object graph deltas, and smoke official mod profiles.
- Compared failure/pass profiles from retained evidence, including the minimal reproduced `CustomAnimals + action/utility + Manbo + UI` profile and the later 10-minute plus five-minute cadence pass.

## Validation

- Documentation-only research.
- No solution build and no game smoke were run because no runtime/source behavior was changed.
- Static check after writing: `git diff --check`.

## Rollback

Remove this update record, remove the linked research note, and remove the `20260705-0007` row from `docs/updates/INDEX.md`. No runtime rollback is needed.

## Follow-Up

- Run the UI-owner split under continuous one-hour title idle.
- Add exactly one of Zoom, MoreSaves, MoreEquipmentSlots, and YConsole on top of the known passing CustomAnimals/action/Manbo base.
- If no single UI owner reproduces, test UI pairs and then escalate to Unity crash dump/root-set analysis.
