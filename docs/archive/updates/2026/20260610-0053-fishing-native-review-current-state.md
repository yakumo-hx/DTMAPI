# 20260610-0053 Fishing Native Review Current State

## Status

Verified.

## Source Request

User requested the mid/long-term follow-up plan from `Refactor` baseline `c7c8498`. This branch is `codex/docs-fishing-native-review-current-state`.

## Summary

- Updated the Fishing native responsibility review so the old `DolocTownExperimentalBridgeApi` route is explicitly historical rather than current implementation truth.
- Added the current post-feature-split implementation path: `FishingAutomationFeature`, `FishingAutomationService`, `FishingAutomationHookBridge`, `AutoFishingSmokeCase`, service failure throttle, runtime reset, and fresh report evidence.
- Kept the API conclusion unchanged: `IFishingAutomationApi` remains `Experimental`, observation and intervention paths stay distinct, and the feature split is not stability promotion.
- Did not change runtime code, public API members, hook/status IDs, smoke schema, game files, external package directories, evidence payloads, report zips, or reverse/decompiled source.

## Changed Files

- `docs/reviews/api/2026/20260610-fishing-native-responsibility.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0053-fishing-native-review-current-state.md`

## Validation

- Passed: `git diff --check`.
- Passed: `tools/scripts/build.ps1 -Configuration Release`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`.
- No game smoke is required for this docs-only correction; current runtime evidence remains `GAME-SMOKE/20260610-223354`.

## Evidence

- Current AutoFishing evidence referenced by the review: `docs/debug/evidence/GAME-SMOKE/20260610-223354`.
- Fresh report pointer referenced by the review: `docs/debug/evidence/GAME-SMOKE/20260610-223354/latest-report.txt`.
- Current API boundary: `docs/api/public-api-matrix.md`.
- Current hook/status boundary: `docs/hook-map/README.md`.

## Rollback

Restore the prior review wording if the project intentionally wants the original review to remain a pure historical document. No runtime rollback is needed.

## Follow-Up

- Add the independent `AutoFishingReportExport` result field in the smoke harness so report-export evidence is visible without conflating it with `AutoFishingMiniGameComplete`.
