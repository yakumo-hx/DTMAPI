# 20260610-0006 CameraView Manual QA Gate

## Metadata

- Update ID: 20260610-0006
- Date: 2026-06-10
- Status: implemented
- Source: User requested the Refactor stability follow-up route, step `codex/qa-camera-view-manual-play`.
- Owner: Codex

## Scope

- Add a durable CameraView manual-play acceptance gate.
- Keep CameraView `Experimental` and leave CameraZoom 0.4.2 as `Failed / ObsoleteCompatibility`.
- Record required manual checks for 2x/4x true-input movement, background flicker, native map-boundary clamp, building transitions, return-to-title reload, and ZoomMod hotkey/config interaction.
- Do not change runtime code, hook IDs/statuses, public API contracts, smoke result schema, or testmod behavior.

## Changed Files

- `docs/reviews/manual-qa/2026/20260610-0006-cameraview-manual-play-gate.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/hook-map/focused/Camera.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0006-camera-view-manual-qa-gate.md`

## Summary

- Added a manual QA gate record with status `pending user confirmation`.
- Marked automated CameraPlayable smoke evidence as supporting proof only, not manual acceptance.
- Updated Camera API, hook-map, and smoke-matrix records so future reviewers see the gate before promoting CameraView.

## Validation

- Passed: `git diff --check` with CRLF warnings only.
- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - 0 warnings, 0 errors, `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - 0 warnings, 0 errors, `DTMAPI.UnitTests: OK`.
- Not run: game smoke. This branch is documentation-only and does not change runtime, hooks, smoke harness, or installed game behavior.
- Not run: manual play. The required manual checks remain `pending user confirmation`.

## Evidence

- Supporting automated evidence remains `docs/debug/evidence/GAME-SMOKE/20260610-022332`.
- Manual evidence has not been supplied in this branch.

## Related Records

- `docs/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md`
- `docs/debug/issues/ISSUE-009-20260608-camera-playable-dynamic-qa.md`
- `docs/updates/2026/20260610-0004-diagnostics-snapshot-mod-status.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Rollback Notes

- Remove the manual gate record and revert the matrix/hook-map references if the acceptance process changes.
- No code rollback is required because this update does not change runtime behavior.

## Follow-Up

- Collect user manual play confirmation or video/screenshot review covering the seven checklist items before any CameraView promotion beyond `Experimental`.
- Generate the final Refactor full/web audit packages after this branch merges back to `Refactor`.
