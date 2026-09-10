# DTMAPI 0.4.2 CameraZoom API Rebuild Goal

Status: complete
Created: 2026-06-07
Target version: 0.4.2

## Source Request

The user asked to move from the completed existing-API native-owner audit into real API rebuild work. This goal intentionally starts with one narrow high-value boundary: CameraZoom / panorama rendering.

Detailed workflow source:

- `docs/workflows/codex-api-rebuild.md`
- `docs/reviews/api/2026/20260607-0008-native-owner-special-audits/02-camera-zoom.md`
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/07-all-api-risk-closure-table.md`
- `docs/reviews/manual-qa/2026/20260607-0001-panorama-background-zoom-note.md`

## Version And Active-Goal Guard

- This goal targets `0.4.2`.
- Before runtime edits, inventory controlled version sources.
- The earlier `docs/goals/2026/20260607-0001-041-ui-save-mine-animal-refactor.md` file is archived manual-QA context, not an active implementation blocker.
- The user explicitly allowed the API rebuild track to proceed after archiving that 0.4.1 manual-QA handoff. If the workspace controlled version is still `0.4.0`, this goal may move directly to the fixed target `0.4.2`.
- If the workspace is already `0.4.2`, do not bump again.
- Do not infer target versions from the previous accidental `0.3.1` bump.

## Current API Truth

- The 0008/0009/0010 reviews cover existing DTMAPI public APIs, not every possible future API.
- `ICameraZoomApi` is currently a `Gap`: it reaches camera `orthographicSize`, but not the native owners for background frame, depth fog, room bounds/render range, parallax, camera controller limits, or input isolation.
- The visible player problem is that large zoom can reveal the farm background as a small framed rectangle instead of a panorama-like continuous view.
- The desired rebuild is an API/GameBridge fix, not a new standalone camera mod.

## Non-Goals

- Do not rebuild MachineProduction, EquipmentSlots, Vehicle/SecondMotor, custom entity runtime verbs, or Input.Suppress in this goal.
- Do not create a new camera mod. Existing ZoomMod may be updated only as the smoke/test consumer for the rebuilt API.
- Do not copy DolocPlus / 小神增强 package code or decompiled source into DTMAPI.
- Do not mark complete from only changing `orthographicSize`.
- Do not call the API ordinary-mod stable until lifecycle, background/fog rendering, restore semantics, and third-save evidence are proven.

## Task A: Baseline, Version Guard, And API Status Correction

- Run `git status` before edits and preserve unrelated user/Codex changes.
- Read all required files from the short `/goal`.
- Inventory controlled version sources and apply the version guard above.
- Review `docs/api/public-api-matrix.md` and existing developer docs for `ICameraZoomApi`.
- If docs currently imply CameraZoom is ordinary-mod stable, downgrade wording to `experimental rebuild target` or equivalent until this goal proves the new behavior.
- Do not change runtime until the native-owner review in Task B is complete.

Acceptance:

- The implementation confirms the archived 0.4.1 handoff is not blocking and states whether it can proceed from the current controlled version to `0.4.2`.
- API matrix/docs no longer overclaim CameraZoom as complete if runtime work is blocked.

## Task B: Native Owner Method-Body Review For Camera/Panorama

Inspect the relevant native/decompiled method bodies and current DTMAPI code before implementation.

Must answer:

- Which native class/function owns main camera position and orthographic size?
- Which native class/function owns farm/outdoor background placement, size, parallax, and clipping?
- Which native class/function owns depth fog / darkness / edge masks visible during large zoom?
- Which native class/function decides room render range and connected-room visibility?
- Which native state resets on room transition, returned-to-title, save load, or map transition?
- Which UI/input layers must be isolated so zoom controls do not leak into gameplay or config menus?
- Which current DTMAPI code path only proves UI/smoke success instead of native-owner success?

Deliverable:

- Add or update an API review record under `docs/reviews/api/2026/` for this CameraZoom method-body review before runtime edits.

Blocker:

- If background/fog/room native owners cannot be identified, stop with evidence instead of patching only ZoomMod.

## Task C: Redesign The CameraZoom API Contract

Design a safer experimental contract before or during implementation:

- owner token or owner id for each zoom policy;
- requested scale versus applied scale;
- min/max/clamp details;
- background compensation status;
- fog/depth compensation status;
- lifecycle restore status;
- failure reasons;
- read-only snapshot method for current camera state;
- no raw Unity/decompiled types in public abstractions.

Acceptance:

- The public API communicates partial failure instead of returning a misleading success.
- Multiple future mods can request zoom without unowned global camera pollution.

## Task D: Rebuild GameBridge Camera/Panorama Runtime

Implement the minimum runtime needed for trustworthy experimental CameraZoom:

- apply zoom through native camera owner path where possible;
- compensate outdoor/farm background scaling/position so large zoom no longer reveals it as a small box;
- compensate depth fog / dark masks enough that the enlarged view is usable;
- restore vanilla camera/background/fog state on disable, returned-to-title, save load, map transition, and explicit reset;
- keep UI scale unchanged;
- ensure non-Zoom camera users are not permanently polluted;
- log before/after native owner state for smoke evidence.

Acceptance:

- Zoom 1x restores vanilla camera/background/fog.
- Zoom 4x shows a usable large farm view without the background becoming a small framed rectangle.
- Room/map transitions do not leave stale camera/background/fog state.

## Task E: Minimal ZoomMod Consumer Update

- Update the existing ZoomMod only as needed to call the rebuilt API.
- Keep `+`/`-` controls and existing config behavior unless the API contract requires a small adjustment.
- Do not add new gameplay features to ZoomMod in this goal.

Acceptance:

- Existing player workflow still works.
- The mod demonstrates the rebuilt API without becoming the place where native-owner fixes live.

## Task F: Validation And Documentation

Required validation:

- Release build and relevant tests.
- Third local save slot game smoke.
- Evidence for 1x, 2x/4x, restore, room/map transition, returned-to-title or clean exit.
- Screenshot or pixel/log evidence that the background/fog problem is fixed, not only that the camera got larger.
- Exit check: no leftover `DolocTown.exe`.

Required docs:

- Update `docs/updates/YYYY/...` and `docs/updates/INDEX.md`.
- Update `docs/debug/INDEX.md` or relevant debug records if lifecycle/input/runtime hooks changed.
- Update `docs/debug/regressions/smoke-matrix.md`.
- Update `docs/hook-map/README.md`.
- Update `docs/api/public-api-matrix.md`.
- Link the new CameraZoom API review record.

Completion standard:

- Mark complete only if the rebuilt API reaches the camera plus background/fog/restore owner paths and the third-save evidence proves the user-visible large-view background issue is gone.
- If native owners remain unknown, version conflict blocks the round, or the fix only changes `orthographicSize`, keep the goal incomplete and report blocker facts.

## Completion Evidence

Completed: 2026-06-07

- Version guard passed: the archived 0.4.1 manual-QA handoff was confirmed non-active, so the controlled version moved directly from `0.4.0` to fixed target `0.4.2`.
- Native owner review completed before runtime edits: `docs/reviews/api/2026/20260607-0011-camera-zoom-method-body-review.md`.
- Public API remained experimental and was expanded with owner/requested/clamped/applied scale, camera-controller/background/fog/scanner/lifecycle/UI-scale statuses, room snapshot telemetry, and `GetSnapshot(string uniqueId)` without raw Unity/decompiled game types.
- GameBridge owner path now reaches `DolocAPI.mainCamera`, `CameraController.RefreshResolution()` / `SetPosition(Vector2)`, `DolocAPI.envBackgroundEx` / `BackgroundRenderer`, `EnvCovariantController` depth-fog controllers, scanner refresh, and `DolocAPI.SetEnvCamera` lifecycle reapply/restore.
- ZoomMod was updated only as the API consumer/status surface; native owner compensation stayed in GameBridge.
- Release build/unit passed with 0 warnings and 0 errors: `tools/scripts/build.ps1 -Configuration Release`.
- Third-save DirectExe smoke passed: `docs/debug/evidence/GAME-SMOKE/20260607-072228`.
- Final smoke command: `tools/scripts/run-game-smoke.ps1 -DirectExe -SaveSlot 3 -IncludeHookProbe -AutoExerciseZoom -SkipBuild -TimeoutSeconds 300`.
- Required log evidence: `HookProbe GameLaunched OK`; `HookProbe SaveLoaded OK slot=2`; `Camera.ZoomEnvironmentLifecycle = experimental`; `Camera.ZoomApi = verified`; `Camera orthographic size 16.875->67.5 viewScale=4 owner=DTMAPI.ZoomMod cameraController=refreshed=True, positioned=True, positioned background=compensated-background=4 fog=compensated-depth-fog=4; compensated-building-depth-fog=4 scanner=refreshed lifecycle=not-needed uiScale=unchanged`; `room=farm_type1-平地`; `Smoke.Zoom = verified`.
- Screenshot evidence: `docs/debug/evidence/GAME-SMOKE/20260607-072228/DTMAPI-evidence/ZOOM-042/20260607-072308/zoom-before.png`, `zoom-4x.png`, `zoom-reset.png`, and `summary.txt`.
- Visual check: `zoom-4x.png` shows the farm background filling the 4x large view instead of appearing as a small framed rectangle.
- Exit evidence: `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup; result JSON has `ProcessExited=true`, `NoFatalInstanceWindow=true`, `ForcedClose=false`.
- Update record: `docs/updates/2026/20260607-0014-camerazoom-api-rebuild.md`.

Retained failed attempts:

- `docs/debug/evidence/GAME-SMOKE/20260607-070702`: caught the initial `CameraController.SetPosition` `Vector3`/`Vector2` owner-call mismatch and proved rollback behavior.
- `docs/debug/evidence/GAME-SMOKE/20260607-071602`: caught incomplete async screenshot evidence before the final screenshot state-machine fix.
