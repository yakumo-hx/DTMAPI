# 20260611-0017 Refactor Manual QA Code Review

Date: 2026-06-11

Status: recorded

Area: docs/manual-qa/review

## Summary

Recorded the user's Refactor-branch manual QA groups A-F and attached code-path review for Manager UI/export, CameraView, AutoFishing, ActionSpeed/OneAction/OilCoalDrop, SaveSlots, ChestLocator, StrongPlantingGun, and AnimalViewer. This is a review-only artifact and does not implement runtime changes or create an implementation handoff goal.

## Source Request

User asked to switch to the `refactor` branch and perform manual-test recording plus code review for the supplied QA groups.

## Changed Files

- `docs/reviews/manual-qa/2026/20260611-0001-refactor-manual-qa-code-review.md`
- `docs/updates/2026/20260611-0017-refactor-manual-qa-code-review.md`
- `docs/updates/INDEX.md`

## Details

- Preserved the user's A-F manual QA group order and recorded the reported pass/fail facts.
- Confirmed Manager UI row caps from code: Mods show first 16 rows; Hooks and Features show first 17 rows.
- Confirmed Manager report export UI displays export status, exported path, snapshot report, and path-match state.
- Confirmed the reported report zip path exists locally.
- Recorded CameraView manual pass evidence while noting the current orthographic-only contract does not synchronize background/fog.
- Recorded AutoFishing option-semantics mismatches from code: delayed minigame status success, skip behavior tied to instant-bite transition, and conditional fast-animation writes.
- Recorded SaveSlots 18+ UI overflow as a layout/paging issue while preserving the user's extra-slot save/load pass.
- Recorded StrongPlantingGun one-way inventory expansion, slot-contract mismatch, and range/use mismatch risks.
- Recorded AnimalViewer first-frame `心情` flicker as a lifecycle/evidence-gap issue.

## Validation

- Branch checked: `Refactor`.
- Initial worktree status before edits: clean.
- Read-only report path check: `D:\Steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260611-195533.zip` exists, length `93629`, last write time `2026-06-11 19:55:33`.
- `git diff --check`: passed. Git reported only the existing line-ending normalization warning for `docs/updates/INDEX.md`.
- Build/test/game smoke: not run because this was docs-only review work.

## Evidence

- Review record: `docs/reviews/manual-qa/2026/20260611-0001-refactor-manual-qa-code-review.md`.
- Code paths inspected include Manager UI, CameraView, FishingAutomation, SaveSlots, StrongPlantingGun, AnimalViewer, ActionSpeed, OilCoalDrop, and ChestLocatorEnhancer paths named in the review header.
- User manual QA reports are preserved verbatim or near-verbatim under the relevant A-F sections in the review record.

## Rollback Notes

Rollback by removing the new manual QA review record, removing this update record, and deleting the `20260611-0017` row from `docs/updates/INDEX.md`. No runtime or package artifacts were changed.

## Follow-Up

- If the user asks for an implementation handoff, create one dedicated `docs/goals/2026/...md` plus sibling `.goal.txt` from the review record.
- Keep review-only evidence separate from automated smoke evidence and future user verification.
