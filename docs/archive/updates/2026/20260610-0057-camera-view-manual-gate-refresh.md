# 20260610-0057 CameraView Manual Gate Refresh

## Status

Verified.

## Source Request

User requested the mid/long-term follow-up plan from `Refactor` baseline `c7c8498`. This branch is `codex/qa-camera-view-manual-gate-refresh`.

## Summary

- Refreshed the CameraView manual QA gate record without marking any user-visible check as passed.
- Updated the latest supporting automated evidence from `GAME-SMOKE/20260610-022332` to `GAME-SMOKE/20260610-043619`.
- Kept `ICameraViewApi`, `ICameraViewLease`, and the obsolete `ICameraZoomApi` compatibility route at their existing statuses.
- Reaffirmed that CameraView remains `Experimental` until manual 2x/4x movement, flicker, boundary clamp, building transition, return-to-title reload, and ZoomMod hotkey/config checks are confirmed.

## Changed Files

- `docs/reviews/manual-qa/2026/20260610-0006-cameraview-manual-play-gate.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/hook-map/focused/Camera.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0057-camera-view-manual-gate-refresh.md`

## Validation

- Passed: `git diff --check`.
- Passed: `tools/scripts/build.ps1 -Configuration Release`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`.
- Not run: game smoke. This branch is documentation-only and does not change runtime, hooks, smoke harness, public API contracts, or installed-game behavior.
- Not run: manual play. The required CameraView checks remain pending user confirmation.

## Evidence

- Supporting automated Camera evidence: `docs/debug/evidence/GAME-SMOKE/20260610-043619`.
- That smoke records `Zoom=Passed`, `Feature.Camera = ready`, `Smoke.CameraPlayable = verified`, `Smoke.DiagnosticsSnapshot = verified`, `mods=14`, `modStatusCodes=loaded=14`, `features=5`, matching `LatestReportPath`, and clean process/fatal checks.
- Manual evidence has not been supplied in this branch.

## Rollback

Revert this update record and the CameraView manual gate evidence refresh if a successor manual/video review supersedes the gate. No runtime rollback is required.

## Follow-Up

- Collect user manual confirmation or video/screenshot review covering the seven checklist items before any CameraView promotion beyond `Experimental`.
- Keep automated Camera smoke evidence as support only, not as a replacement for the manual gate.
