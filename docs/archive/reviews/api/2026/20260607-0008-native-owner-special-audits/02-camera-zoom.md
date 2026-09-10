# 02 - ICameraZoomApi Special Audit

## 1. Scope

API under review: `DTMAPI.Abstractions.ICameraZoomApi`, especially `Register`, `SetViewScale`, `StepViewScale`, `ResetViewScale`, `GetState`, and `GetStatus`.

Questions answered:

- Which camera fields the current implementation writes.
- Whether DolocPlus panorama research owners are connected: `CameraController`, background, depth fog, room render range, parallax, scanner/room bounds.
- Why current zoom can create the "background small frame" failure.
- Which owners must be split for a stable panorama/large-view API.

Result: `Partial`. The API reaches camera orthographic size only. It does not own the broader visual and room-state stack needed for stable large view.

## 2. Files read

- `docs/api/public-api-matrix.md:88`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review-index.md:69`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review/01-high-risk-runtime-bridges.md:473`
- `docs/reviews/api/2026/20260607-0007-native-responsibility-code-review/07-completion-audit.md:82`
- `docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md:52`
- `docs/updates/2026/20260607-0007-native-responsibility-code-review.md:71`
- `docs/hook-map/README.md:729`
- `docs/debug/INDEX.md:28`
- `docs/debug/regressions/smoke-matrix.md:10`
- `docs/reviews/manual-qa/2026/20260607-0001-panorama-background-zoom-note.md`
- `references/doloc-town/research-notes/research-DolocPlus-overlap-study-20260607.md`
- `references/doloc-town/research-notes/research-DolocPlus-deep-dive-20260607.md`
- `references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- Reverse candidate search under `references/doloc-town/reverse/builds/` for `CameraController.cs`, `CameraUtils.cs`, `RoomPatch.cs`, `DepthFogController.cs`, `EnvCovariantController.cs`, `Room.cs`, `ScreenManager.cs`, and `DolocAPI.cs`.

## 3. Functions read

- `DTMAPI.Abstractions.ICameraZoomApi.Register(...)`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:162`
- `DTMAPI.Abstractions.ICameraZoomApi.SetViewScale(...)`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:164`
- `DTMAPI.Abstractions.ICameraZoomApi.StepViewScale(...)`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:165`
- `DTMAPI.Abstractions.ICameraZoomApi.ResetViewScale(...)`: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs:166`
- `DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi.ResetCameraZoomForLifecycleBoundary(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:159`
- `DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi.Register(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1242`
- `DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi.SetViewScale(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1274`
- `DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi.StepViewScale(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1304`
- `DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi.ResetViewScale(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1316`
- `DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi.RefreshCameraZoomForRuntime()`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2329`
- `DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi.ApplyCameraZoomTarget(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2337`
- `DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi.ComputeCameraZoomTargetScale()`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2382`
- `DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi.UpdateCameraZoomStates(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2395`
- `DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi.NormalizeCameraZoomOptions(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2469`
- `DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi.TryGetMainCameraObject()`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2485`
- `DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi.TryReadCameraOrthographicSize(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2496`
- `DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi.TryWriteCameraOrthographicSize(...)`: `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2526`
- Zoom smoke exercise: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:2530`

## 4. Call graph

Current implementation:

`mod -> ICameraZoomApi.Register/SetViewScale -> DolocTownExperimentalBridgeApi cameraZoomOptions/cameraZoomStates -> ApplyCameraZoomTarget -> TryReadCameraOrthographicSize -> TryWriteCameraOrthographicSize -> DolocAPI.mainCamera.orthographicSize or UnityEngine.Camera.main.orthographicSize`

Runtime refresh:

`DolocTownGameBridge.Update -> DolocTownExperimentalBridgeApi.UpdateRuntimeAutomation -> RefreshCameraZoomForRuntime -> ApplyCameraZoomTarget`

Lifecycle reset:

`SaveLoaded/ReturnedToTitle boundary -> ResetCameraZoomForLifecycleBoundary -> ApplyCameraZoomTarget(scale=1)`

Missing DolocPlus-style large-view path:

`CameraController.camSize / RefreshResolution`, `DolocAPI.worldResolution`, `CurrentRoom.SceneSize`, room camera range, `DolocAPI.envBackgroundEx`, `EnvCovariantController._depthFogController`, `DolocAPI.RefreshScanner`, parallax/depth fog restoration, and UI/input coordinate scaling are not called by current DTMAPI zoom code.

## 5. Function body findings

- `Register` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1242` stores normalized options per owner, initializes state, and immediately calls `ApplyCameraZoomTarget`.
- `SetViewScale` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1274` clamps the requested scale to the owner's min/max, updates DTMAPI state, and calls `ApplyCameraZoomTarget`.
- `StepViewScale` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1304` is a convenience wrapper over `SetViewScale`.
- `ResetViewScale` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:1316` sets scale back to `1`.
- `ApplyCameraZoomTarget` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2337` reads the current camera size, computes `vanillaSize * targetScale`, and writes that value as `orthographicSize`.
- `TryGetMainCameraObject` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2485` tries `DolocAPI.mainCamera` first, then falls back to `UnityEngine.Camera.main`.
- `TryReadCameraOrthographicSize` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2496` reads a property/field named `orthographicSize`.
- `TryWriteCameraOrthographicSize` at `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:2526` writes a property/field named `orthographicSize`.
- Source search in `src/DTMAPI.GameBridge.DolocTown`, `src/DTMAPI.Core`, and `src/DTMAPI.BepInExBootstrap` found no current calls to `CameraController`, `envBackgroundEx`, `EnvCovariantController`, `DepthFogController`, `RefreshResolution`, `RefreshScanner`, or room range/scanner owners from CameraZoom.
- DolocPlus notes identify the missing background/fog pieces: `research-DolocPlus-function-map-20260607.md:493` checks `envBackgroundEx` and `EnvCovariantController`; `:498` reads `worldResolution`, `CurrentRoom.SceneSize`, and `mainCamera.orthographicSize`; `:500` writes `CameraController.camSize`; `:503` reads `_depthFogController`; `:511` calls scanner/camera reset paths.
- The focused panorama note at `docs/reviews/manual-qa/2026/20260607-0001-panorama-background-zoom-note.md:26` explicitly records the future minimum fix as scaling `DolocAPI.envBackgroundEx` and `_depthFogController`.

## 6. Native owner verdict

Verdict: `Partial`.

Reached owner:

- `DolocAPI.mainCamera.orthographicSize` / `UnityEngine.Camera.main.orthographicSize`.

Missing owner map:

- `CameraController.camSize` and camera-controller resolution refresh.
- Room camera range/position and scanner refresh around `CurrentRoom.SceneSize`, `DolocAPI.worldResolution`, and `DolocAPI.RefreshScanner`.
- `DolocAPI.envBackgroundEx` background transform scale.
- `EnvCovariantController._depthFogController` and depth fog scale.
- Parallax/depth fog restoration across save/title/room transitions.
- UI and input coordinate behavior at non-vanilla world scale.

Excluded candidates:

- Current DTMAPI code does not reference the DolocPlus candidate owners above, so the 4x smoke proves only camera size writes and restoration, not full visual ownership.
- Full DolocPlus panorama/photo behavior is excluded from "current Zoom" because research notes distinguish large-view zoom compensation from photo-mode room fitting and object hiding.

## 7. Ordinary mod usability

Ordinary mod usability: `仅 DTMAPI 自家 mod 可用`.

Ordinary mods should not treat `ICameraZoomApi` as a stable large-view or panorama API. It can be used by DTMAPI's own ZoomMod under controlled expectations, but external mods risk visual and input/camera-bound mismatches.

## 8. Concrete failure modes

1. Background-small-frame: increasing `orthographicSize` shows more world area while `envBackgroundEx` and depth-fog transforms remain vanilla-sized, leaving a small centered background rectangle.
2. Fog/parallax mismatch: depth fog and background layers do not scale with the camera ratio, so visual layers drift relative to foreground geometry.
3. Room range/scanner mismatch: the camera sees beyond the range that room render/scanner systems prepared, causing missing content, stale scanner buffers, or edge artifacts.
4. UI/input mismatch: world camera scale changes without any explicit UI/input coordinate owner, so cursor hit tests or placement previews can diverge from the visual area in future mod uses.
5. Lifecycle drift: resetting only orthographic size may restore camera size while leaving future background/fog compensation state stale unless those owners are split and restored together.

## 9. Minimal rebuild direction

- Keep `ViewScale` as a small stable contract only after it states which owner set it controls.
- Split implementation layers:
  - `CameraSizeOwner`: `mainCamera.orthographicSize`, `CameraController.camSize`, `RefreshResolution`.
  - `BackgroundFogOwner`: `envBackgroundEx` and `_depthFogController` backup/apply/restore.
  - `RoomBoundsOwner`: room render range, scanner refresh, camera position/room size calculations.
  - `UiInputScaleOwner`: explicit decision to preserve, map, or forbid UI/input interaction during large-view modes.
  - `PanoramaOwner`: separate API for full room fit/photo behavior, because that includes hiding agent/drone/motor and stricter scene restoration.
- Add smoke that verifies not only orthographic size but also no background-small-frame, restored background/fog, and room-edge rendering after reset/save/title.

## 10. Evidence gaps

- Missing DTMAPI code evidence for `CameraController.camSize`, `envBackgroundEx`, `_depthFogController`, `RefreshResolution`, and `RefreshScanner` because these owners are not currently called.
- Missing reverse-level method signature mapping for a safe public adapter; this review located candidate files but did not copy decompiled source.
- Missing automated screenshot/pixel evidence for the background-small-frame failure in current Zoom; only existing notes and DolocPlus research identify the failure mode.
- Missing room transition and UI/input smoke for large-view modes; the existing `GAME-SMOKE/20260606-134042` proves 4x/restore camera writes only.
