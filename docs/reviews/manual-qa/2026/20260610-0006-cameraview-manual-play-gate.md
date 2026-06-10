# 20260610-0006 - CameraView Manual Play Gate

## Manual QA Record Header

- Date: 2026-06-10
- Source: User requested the Refactor stability follow-up route, step `codex/qa-camera-view-manual-play`; refreshed by the mid/long follow-up branch `codex/qa-camera-view-manual-gate-refresh`; refreshed again by `codex/qa-camera-view-manual-play-handoff` to add a dedicated manual handoff goal.
- Scope: CameraView manual acceptance gate for the lease-based playable camera path.
- Status: pending user confirmation.
- Last refresh: 2026-06-11. No new manual confirmation was supplied, so every required user-visible check below remains pending.
- Manual handoff goal: `docs/goals/2026/20260611-0001-cameraview-manual-play-handoff.md` and sibling prompt backup `docs/goals/2026/20260611-0001-cameraview-manual-play-handoff.goal.txt`.
- This record does not promote `ICameraViewApi`, `ICameraViewLease`, or `ICameraZoomApi`.

## Baseline

- `ICameraZoomApi` 0.4.2 remains `Failed / ObsoleteCompatibility` after the manual failure review in `docs/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md`.
- `ICameraViewApi` is the current ordinary playable camera route. It writes only `DolocAPI.mainCamera.orthographicSize`, keeps native camera follow/range ownership, and does not call `CameraController.RefreshResolution`, `CameraController.SetPosition`, `DolocAPI.RefreshScanner`, background compensation, fog compensation, or UI scaling.
- Automated CameraPlayable smoke evidence is supporting evidence only. It is not a substitute for this manual play gate.
- Latest supporting automated evidence: `GAME-SMOKE/20260611-024629`, with `Zoom=Passed`, `Feature.Camera = ready`, `Smoke.CameraPlayable = verified`, `Smoke.DiagnosticsSnapshot = verified`, `DiagnosticsReportExport=Passed`, report `dtmapi-report-20260611-024813.zip`, and clean process/fatal checks. Prior supporting diagnostics snapshot evidence remains `GAME-SMOKE/20260610-043619`.

## Required Manual Checks

All checks use the local third save slot unless a later user note says otherwise.

1. 2x true-input movement for at least 1 minute.
   - Expected: playable camera stays usable and player-centered enough for normal movement.
   - Status: pending user confirmation.

2. 4x true-input movement for at least 1 minute.
   - Expected: playable camera does not become fixed to a scene center and does not rely on segmented background refresh to catch up.
   - Status: pending user confirmation.

3. Background flicker review.
   - Expected: no user-visible background, fog, or layer flicker while moving vertically or through height-sensitive areas.
   - Status: pending user confirmation.

4. Map-boundary native clamp review.
   - Expected: the native room/map clamp remains acceptable at edges and corners; no stuck view, inverted clamp, or detached camera.
   - Status: pending user confirmation.

5. Enter and exit building.
   - Expected: room transition resets/reapplies the active playable-view lease without stale zoom, wrong room center, or background residue.
   - Status: pending user confirmation.

6. Return to title and reload save.
   - Expected: title boundary restores vanilla camera state; reloading the third save can reacquire configured ZoomMod behavior without stale state.
   - Status: pending user confirmation.

7. ZoomMod hotkey/config interaction.
   - Expected: ZoomMod hotkeys/config still acquire and update CameraView leases without conflicting with the compatibility `ICameraZoomApi` wrapper.
   - Status: pending user confirmation.

## Acceptance Boundary

- Until the checks above are manually confirmed, `ICameraViewApi` stays `Experimental`.
- Automated `Smoke.CameraPlayable` can continue to validate lease arbitration, orthographic-size-only writes, diagnostics snapshot presence, and clean process/fatal checks.
- This refresh does not claim pass/fail results for the 2x/4x movement, flicker, boundary clamp, building transition, title reload, or ZoomMod interaction checklist.
- Future completion claims for CameraView must cite this record or a successor manual/video review that covers the same user-visible failure modes.
- The 2026-06-11 handoff goal is a checklist transfer only. It does not authorize runtime edits or API promotion.

## Related Records

- `docs/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md`
- `docs/debug/issues/ISSUE-009-20260608-camera-playable-dynamic-qa.md`
- `docs/debug/evidence/GAME-SMOKE/20260610-043619`
- `docs/debug/evidence/GAME-SMOKE/20260611-024629`
- `docs/goals/2026/20260611-0001-cameraview-manual-play-handoff.md`
- `docs/goals/2026/20260611-0001-cameraview-manual-play-handoff.goal.txt`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/focused/Camera.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260610-0057-camera-view-manual-gate-refresh.md`
