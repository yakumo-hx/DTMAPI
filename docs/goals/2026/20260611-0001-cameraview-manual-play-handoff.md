# CameraView Manual Play Handoff Goal

Status: pending manual/user confirmation
Created: 2026-06-11
Target branch: `Refactor`
Target version: no version bump

## Source Request

The user asked for the mid/long next-round plan to include a CameraView manual QA handoff. This file preserves the checklist and acceptance gates for a future manual tester or fresh Codex thread without claiming that CameraView has passed.

## Required Reading

- `AGENTS.md`
- `PROJECT.md`
- `docs/goals/README.md`
- `docs/reviews/README.md`
- `docs/workflows/codex-feedback-to-goal.md`
- `docs/reviews/manual-qa/2026/20260610-0006-cameraview-manual-play-gate.md`
- `docs/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/focused/Camera.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- latest Camera smoke evidence referenced by the manual gate

## Scope

This is a manual QA handoff, not a runtime implementation goal. Do not change CameraView code, API status, hook IDs, smoke result schema, or package layout while executing this handoff unless the user explicitly opens a separate implementation goal.

## Current Truth

- `ICameraZoomApi` 0.4.2 remains `Failed / ObsoleteCompatibility`.
- `ICameraViewApi` and `ICameraViewLease` remain `Experimental`.
- Current CameraView implementation writes only playable camera `orthographicSize`.
- Native camera follow/range ownership should stay native-owned.
- DTMAPI must not use `CameraController.RefreshResolution`, `CameraController.SetPosition`, `DolocAPI.RefreshScanner`, background compensation, fog compensation, or UI scaling for ordinary playable zoom.
- Automated `Smoke.CameraPlayable`, `Feature.Camera=ready`, and `DiagnosticsReportExport=Passed` are supporting evidence only.
- Manual pass requires real user-visible play confirmation or an equivalent targeted video/manual review.

## Supporting Automated Evidence

Latest supporting automated evidence at handoff time:

- `GAME-SMOKE/20260611-024629`
- `Zoom=Passed`
- `Smoke.CameraPlayable=verified`
- `Smoke.DiagnosticsSnapshot=verified`
- `DiagnosticsReportExport=Passed`
- `Feature.Camera=ready`
- report: `dtmapi-report-20260611-024813.zip`
- clean process/fatal checks

This evidence does not close the manual gate by itself.

## Manual Checklist

Use local save slot 3 unless the user explicitly asks for another save.

1. 2x true-input movement for at least 1 minute.
   - Acceptance: playable camera remains usable and player-centered enough for normal movement.
   - Must record: route/location, whether movement used real keyboard/controller input, and whether any camera detachment or jitter appeared.

2. 4x true-input movement for at least 1 minute.
   - Acceptance: playable camera does not become fixed to a scene center and does not rely on segmented background refresh to catch up.
   - Must record: route/location, whether the camera follows during sustained movement, and whether zoom remains active.

3. Background flicker review.
   - Acceptance: no user-visible background, fog, or layer flicker while moving vertically or through height-sensitive areas.
   - Must record: area tested and whether any flicker appears before the final frame settles.

4. Map-boundary native clamp review.
   - Acceptance: native room/map clamp remains acceptable at edges and corners; no stuck view, inverted clamp, or detached camera.
   - Must record: boundaries tested and whether movement/camera recovers normally.

5. Enter and exit building.
   - Acceptance: room transition resets or reapplies the active playable-view lease without stale zoom, wrong room center, or background residue.
   - Must record: building pair and whether zoom state after transition matches expectation.

6. Return to title and reload save.
   - Acceptance: title boundary restores vanilla camera state; reloading third save can reacquire configured ZoomMod behavior without stale state.
   - Must record: before-title zoom, post-title state, reload result, and any stale state.

7. ZoomMod hotkey/config interaction.
   - Acceptance: ZoomMod hotkeys/config acquire and update CameraView leases without conflicting with the obsolete `ICameraZoomApi` wrapper.
   - Must record: hotkeys used, config values changed, and observed zoom state.

## Required Output After Manual Testing

Update the existing gate record or create a successor review if the result is complex:

- `docs/reviews/manual-qa/2026/20260610-0006-cameraview-manual-play-gate.md`

If all checks pass:

- Mark each checklist item as user/manual confirmed with date and evidence.
- Keep `ICameraViewApi` Experimental unless a separate API promotion review is explicitly performed.
- Update `docs/api/public-api-matrix.md`, `docs/hook-map/focused/Camera.md`, `docs/hook-map/README.md`, and `docs/debug/regressions/smoke-matrix.md`.
- Add a `docs/updates/2026/...` record.

If any check fails or remains untested:

- Keep the gate pending or failed for that item.
- Preserve user-visible reproduction details.
- Do not claim CameraView completion.
- If implementation is needed, create a new implementation goal file rather than editing this handoff into code work.

## Blockers

- No real manual/user confirmation.
- Missing route/location or input details for movement checks.
- Any visible flicker, clamp issue, stale zoom, wrong room center, title reload leak, or ZoomMod interaction failure.
- Evidence only from automated smoke without manual/video confirmation.

## Completion Standard

This handoff can be considered complete only when every checklist item has explicit manual/user confirmation or a successor review records the remaining blockers. Automated smoke evidence alone is insufficient.
