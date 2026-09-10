# 20260610-0006 - CameraView Manual Play Gate

## Manual QA Record Header

- Date: 2026-06-10
- Source: User requested the Refactor stability follow-up route, step `codex/qa-camera-view-manual-play`; refreshed by the mid/long follow-up branch `codex/qa-camera-view-manual-gate-refresh`; refreshed again by `codex/qa-camera-view-manual-play-handoff` to add a dedicated manual handoff goal.
- Scope: CameraView manual acceptance gate for the lease-based playable camera path.
- Status: first-party Zoom product manual acceptance passed with an accepted background/fog synchronization limitation; exact reload-reacquire coverage remains partial.
- Last refresh: 2026-07-12. The user confirmed Zoom operation, hotkey modification, enlarged-view gameplay, building transitions, map boundaries, and return-to-title minimum-size restoration. The supplied screenshot confirms that background/atmospheric coverage does not expand with the orthographic playable view; the user accepts this known non-goal as normal and reports no gameplay impact.
- Manual handoff goal: `docs/goals/2026/20260611-0001-cameraview-manual-play-handoff.md` and sibling prompt backup `docs/goals/2026/20260611-0001-cameraview-manual-play-handoff.goal.txt`.
- This record does not promote `ICameraViewApi`, `ICameraViewLease`, or `ICameraZoomApi`.

## Baseline

- `ICameraZoomApi` 0.4.2 remains `Failed / ObsoleteCompatibility` after the manual failure review in `docs/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md`.
- `ICameraViewApi` is the current ordinary playable camera route. It writes only `DolocAPI.mainCamera.orthographicSize`, keeps native camera follow/range ownership, and does not call `CameraController.RefreshResolution`, `CameraController.SetPosition`, `DolocAPI.RefreshScanner`, background compensation, fog compensation, or UI scaling.
- Automated CameraPlayable smoke evidence is supporting evidence only. It is not a substitute for this manual play gate.
- Latest supporting automated evidence: final `Refactor` smoke `GAME-SMOKE/20260611-031502`, with `Zoom=Passed`, `Feature.Camera = ready`, `Smoke.CameraPlayable = verified`, `Smoke.DiagnosticsSnapshot = verified`, `DiagnosticsReportExport=Passed`, report `dtmapi-report-20260611-031644.zip`, and clean process/fatal checks. Prior supporting diagnostics report-export evidence remains `GAME-SMOKE/20260611-024629`; prior diagnostics snapshot evidence remains `GAME-SMOKE/20260610-043619`.

## Required Manual Checks

All checks use the local third save slot unless a later user note says otherwise.

1. 2x true-input movement for at least 1 minute.
   - Expected: playable camera stays usable and player-centered enough for normal movement.
   - Status: passed by the cited 2026-06-11 manual QA; the 2026-07-12 first-party product sample reconfirmed general Zoom operation and enlarged-view play but did not restate the exact dwell time.

2. 4x true-input movement for at least 1 minute.
   - Expected: playable camera does not become fixed to a scene center and does not rely on segmented background refresh to catch up.
   - Status: passed by the cited 2026-06-11 manual QA; current first-party product feedback found no obvious gameplay problem while enlarged.

3. Background flicker review.
   - Expected: no user-visible background, fog, or layer flicker while moving vertically or through height-sensitive areas.
   - Status: accepted known limitation, not an implementation pass. The 2026-07-12 screenshot shows a sharp rectangular boundary around the smaller background illustration and gray uncovered space in the enlarged view. The user reports this is normal for the current product and has no gameplay impact; background/fog/panorama synchronization remains unimplemented.

4. Map-boundary native clamp review.
   - Expected: the native room/map clamp remains acceptable at edges and corners; no stuck view, inverted clamp, or detached camera.
   - Status: passed by 2026-07-12 user manual QA.

5. Enter and exit building.
   - Expected: room transition resets/reapplies the active playable-view lease without stale zoom, wrong room center, or background residue.
   - Status: passed by 2026-07-12 user manual QA.

6. Return to title and reload save.
   - Expected: title boundary restores vanilla camera state; reloading the third save can reacquire configured ZoomMod behavior without stale state.
   - Status: return-to-title minimum-size restoration passed by 2026-07-12 user manual QA; reload/reacquire was not separately stated in this feedback.

7. ZoomMod hotkey/config interaction.
   - Expected: ZoomMod hotkeys/config still acquire and update CameraView leases without conflicting with the compatibility `ICameraZoomApi` wrapper.
   - Status: passed for first-party ZoomMod hotkey modification in the 2026-07-12 user sample.

## 2026-07-12 First-Party Zoom Product Manual Follow-Up

Source: user manual QA after the Owner Lifetime refactor and first-party Zoom promotion. The initial feedback had no screenshot, video, exact duration, map route, or log bundle; the same user then supplied one enlarged-farm screenshot and the transition/boundary/title follow-up below.

### 1. Zoom in/out

- Original feedback: “手测zoom放大缩小无问题。”
- User-confirmed fact: the tested first-party Zoom product enlarged and reduced the playable view without a reported functional problem.
- Review boundary: this is a product-operation pass. It does not identify exact 2x/4x dwell times or independently prove every CameraView arbitration branch.

### 2. Hotkey modification

- Original feedback: “热键修改无问题。”
- User-confirmed fact: changing the Zoom hotkey worked in the tested configuration and the modified binding remained usable for Zoom operation.
- Review boundary: this closes the scoped ZoomMod hotkey/config interaction check for this sample; it does not reclassify unrelated Input APIs or other Mods' bindings.

### 3. Gameplay while enlarged

- Original feedback: “放大游玩未发现明显问题。”
- User-confirmed fact: ordinary gameplay in the enlarged view showed no obvious player-visible problem during the observed route.
- Review boundary: “未发现明显问题” is observation-bounded. The follow-up below separately covers map boundary, building transition, return-to-title restoration, and the accepted background limitation.

### 4. Building transitions

- Original feedback: “建筑切换通过。”
- User-confirmed fact: entering/exiting the tested building transition passed without a reported stale Zoom state or playability problem.
- Review boundary: the exact building and number of repetitions were not supplied; this is a scoped transition pass, not proof for every room type.

### 5. Map boundaries

- Original feedback: “地图边界通过。”
- User-confirmed fact: the tested map-boundary behavior remained acceptable without a reported stuck, inverted, or detached camera.
- Review boundary: the exact edges/corners and route were not supplied, so the result remains bounded to the observed map sample.

### 6. Background/fog synchronization and screenshot transcription

- Original feedback: “背景/雾同步 代码好像没有做，所以显示如图一，也是正常，对游玩没有影响。”
- Screenshot transcription: the image shows a very wide enlarged farm view. Gameplay objects and terrain occupy the lower/central screen while the illustrated city/sky background remains a smaller centered rectangle with a sharp boundary; a broad gray field fills the exposed area around it. HUD, inventory bar, time, currency, quest text, and farm label remain rendered at normal screen-space scale.
- User-confirmed fact: the visible coverage mismatch is accepted as normal for this Zoom product and did not affect gameplay in the tested route.
- Code/document fact: the current CameraView path changes only playable-camera `orthographicSize`; it does not synchronize `BackgroundRenderer`, `BackgroundLayerRenderer`, fog/weather, or panorama state.
- Review boundary: this screenshot is evidence of the known missing synchronization, not evidence that background/fog synchronization works. No new background compensation should be inferred or added without a separate native-owner/API task.

### 7. Return to title

- Original feedback: “回标题恢复最小大小无问题。”
- User-confirmed fact: returning to title restored the camera to the minimum/vanilla-size state without a reported residual enlarged view.
- Review boundary: this passes title restoration in the tested process; the feedback does not separately state that a subsequent save reload reacquired the configured Zoom value.

This manual follow-up validates the unchanged first-party Zoom product behavior. It does not change the public Camera API, Hook targets, owner-lifetime implementation, or `ICameraViewApi` Experimental status.

## Acceptance Boundary

- `ICameraViewApi` stays `Experimental`: playable Zoom acceptance is now strong, but background/fog/panorama synchronization is deliberately absent and reload/reacquire was not separately restated in the latest sample.
- Automated `Smoke.CameraPlayable` can continue to validate lease arbitration, orthographic-size-only writes, diagnostics snapshot presence, and clean process/fatal checks.
- Current manual evidence covers 2x/4x playable movement from the prior review plus first-party Zoom operation, hotkey modification, enlarged-view play, map boundary, building transition, and title restoration. Background coverage mismatch is an accepted limitation, not a passed synchronization feature; reload/reacquire remains unstated in the latest follow-up.
- Future completion claims for CameraView must cite this record or a successor manual/video review that covers the same user-visible failure modes.
- The 2026-06-11 handoff goal is a checklist transfer only. It does not authorize runtime edits or API promotion.

## Related Records

- `docs/reviews/manual-qa/2026/20260607-0003-camerazoom-042-manual-failure-review.md`
- `docs/debug/issues/ISSUE-009-20260608-camera-playable-dynamic-qa.md`
- `docs/debug/evidence/GAME-SMOKE/20260610-043619`
- `docs/debug/evidence/GAME-SMOKE/20260611-031502`
- `docs/goals/2026/20260611-0001-cameraview-manual-play-handoff.md`
- `docs/goals/2026/20260611-0001-cameraview-manual-play-handoff.goal.txt`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/focused/Camera.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260610-0057-camera-view-manual-gate-refresh.md`
- `docs/updates/2026/20260711-0013-first-party-zoom-owner-lifetime.md`
