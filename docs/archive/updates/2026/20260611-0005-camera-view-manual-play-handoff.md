# 20260611-0005 CameraView Manual Play Handoff

## Summary

Prepared a dedicated CameraView manual-play handoff goal and refreshed the pending manual QA gate without claiming manual pass or promoting `ICameraViewApi`.

## Source Request

User requested the mid/long next-round plan, including a CameraView manual QA gate refresh and handoff for later human confirmation.

## Changed Files

- `docs/goals/2026/20260611-0001-cameraview-manual-play-handoff.md`
- `docs/goals/2026/20260611-0001-cameraview-manual-play-handoff.goal.txt`
- `docs/reviews/manual-qa/2026/20260610-0006-cameraview-manual-play-gate.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/hook-map/focused/Camera.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Details

- Added a dedicated handoff goal for CameraView manual play checks.
- Saved the exact short `/goal` prompt beside the goal file.
- Kept all manual checks pending user confirmation: 2x and 4x true-input movement, background flicker, map-boundary native clamp, building transition, return-to-title reload, and ZoomMod hotkey/config interaction.
- Updated Camera docs to cite latest supporting automated evidence `GAME-SMOKE/20260611-024629`, including `DiagnosticsReportExport=Passed`, while stating that automated smoke is support evidence only.
- Kept `ICameraViewApi` and `ICameraViewLease` `Experimental`; no Stable/StableCandidate promotion was made.
- No runtime code, hook IDs, smoke schema, public API, package layout, or installed game behavior changed.

## Validation

- Passed:
  - `git diff --check`
  - `tools/scripts/build.ps1 -Configuration Release`
  - `tools/scripts/test.ps1 -Configuration Release`

No game smoke was required for this docs-only handoff. The latest supporting automated Camera evidence remains `GAME-SMOKE/20260611-024629`; it does not close the manual gate.

## Evidence

- Manual gate: `docs/reviews/manual-qa/2026/20260610-0006-cameraview-manual-play-gate.md`
- Handoff goal: `docs/goals/2026/20260611-0001-cameraview-manual-play-handoff.md`
- Supporting automated evidence: `docs/debug/evidence/GAME-SMOKE/20260611-024629`

## Rollback Notes

Remove the handoff goal and revert Camera documentation references to the prior 2026-06-10 gate refresh. Runtime code and public API are unaffected.
