# 20260610-0055 Fishing API Contract Clarity

## Status

Verified.

## Source Request

User requested the mid/long-term follow-up plan from `Refactor` baseline `c7c8498`. This branch is `codex/docs-fishing-api-contract-clarity`.

## Summary

- Added XML documentation to `FishingAutomationOptions` clarifying that `AutoRecast` and `RequireSelectedFishingRod` are accepted but normalized to conservative safe values in the 0.5.0-alpha GameBridge.
- Updated public API and hook-map docs to record the current single-effective-owner policy for `IFishingAutomationApi`.
- Kept `NormalizeFishingAutomationOptions(...)` behavior unchanged.
- Did not add, remove, rename, or obsolete public API members; did not change runtime behavior, hook/status IDs, smoke schema, game files, external package directories, evidence payloads, report zips, or reverse/decompiled source.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0055-fishing-api-contract-clarity.md`

## Validation

- Passed: `git diff --check`.
- Passed: `tools/scripts/build.ps1 -Configuration Release`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`.
- No game smoke is required because this branch changes docs/XML comments only and does not change GameBridge behavior.

## Evidence

- Existing options review: `docs/reviews/api/2026/20260610-fishing-options-contract-review.md`.
- Latest report-export result-field smoke remains `docs/debug/evidence/GAME-SMOKE/20260611-000932`.

## Rollback

Remove the XML comments and restore the previous public API/hook-map wording. No runtime rollback is needed.

## Follow-Up

- If a future branch honors `AutoRecast=false` or `RequireSelectedFishingRod=false`, add dedicated tests and third-save smoke/manual evidence before changing the matrix wording.
