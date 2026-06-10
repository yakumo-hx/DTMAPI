# 20260610-0045 Fishing Options Contract Review

## Status

Verified.

## Source Request

User requested the FishingAutomation hardening follow-up from the `85cd4af` Refactor baseline. This docs-only branch is `codex/review-fishing-options-contract`.

## Summary

- Added `docs/reviews/api/2026/20260610-fishing-options-contract-review.md`.
- Documented that `FishingAutomationService.NormalizeFishingAutomationOptions(...)` currently forces `AutoRecast=true` and `RequireSelectedFishingRod=true`.
- Kept runtime behavior unchanged because respecting those fields would broaden policy into unverified no-recast and non-selected/backpack-rod paths.
- Kept `IFishingAutomationApi`, `FishingAutomationOptions`, and `FishingAutomationState` Experimental; no public members were added, removed, renamed, or obsoleted.

## Changed Files

- `docs/reviews/api/2026/20260610-fishing-options-contract-review.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0045-fishing-options-contract-review.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- No game smoke was run for this docs-only branch because runtime, hooks, public API members, smoke harness, mods, package layout, and installed game behavior were unchanged.

## Evidence Links

- API review: `docs/reviews/api/2026/20260610-fishing-options-contract-review.md`
- Hook map: `docs/hook-map/README.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Retained runtime evidence:
  - `docs/debug/evidence/GAME-SMOKE/20260610-170839`
  - `docs/debug/evidence/GAME-SMOKE/20260610-202047`
  - `docs/debug/evidence/GAME-SMOKE/20260610-203301`
  - `docs/debug/evidence/GAME-SMOKE/20260610-204134`

## Rollback

- Remove the Fishing options contract review and matrix/hook-map/update references. No runtime rollback is needed because this branch does not change code.

## Follow-Up

- If a future branch changes `AutoRecast` or `RequireSelectedFishingRod` normalization, add dedicated unit tests and third-save smokes for disabled recast and non-selected/backpack rod discovery before changing the API matrix.
