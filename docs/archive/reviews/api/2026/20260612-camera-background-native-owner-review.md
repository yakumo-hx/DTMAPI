# 20260612 - Camera Background/Fog/Panorama Native Owner Review

Date: 2026-06-12
Scope: Task G for `docs/goals/2026/20260612-0003-bottom-layer-refactor-from-audit.md`.
Result: defer runtime background/fog/panorama changes; keep `ICameraViewApi` Experimental.

## Files Read

- `docs/workflows/codex-api-rebuild.md`
- `docs/reviews/api/2026/20260607-0011-camera-zoom-method-body-review.md`
- `docs/reviews/manual-qa/2026/20260611-0001-refactor-manual-qa-code-review.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraViewService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraDiagnosticsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/Camera/CameraFeature.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocAPI.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/CameraController.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/BackgroundRenderer.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/BackgroundLayerRenderer.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/DepthFogController.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/EnvCovariantController.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/EnvBackgroundSO.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/BackgroundLayer.cs`

## Native Owner Answers

Playable camera size:

- Current `ICameraViewApi` is intentionally orthographic-size-only. `CameraViewService` writes `DolocAPI.mainCamera.orthographicSize`, tracks owner leases, and restores/reapplies on save/title/environment boundaries.
- It does not call `CameraController.RefreshResolution()`, `CameraController.SetPosition(...)`, or `DolocAPI.RefreshScanner()`. That is a deliberate playable-camera boundary after the old 0.4.2 CameraZoom path mixed room-fit/panorama behavior into normal movement.

Camera room range and native transition boundary:

- `CameraController` owns `camSize`, movement/follow bounds, `SetRoomRange(...)`, `SetPosition(...)`, and `RefreshResolution()`.
- `DolocAPI.SetEnvCamera(Vector2 cameraPosition, Vector2 cameraSize, bool shouldShowBackground, bool shouldMaskBackground, bool shouldFollowPlayer = false)` is the native room/environment camera boundary. It sets camera range and optional follow-position behavior, then toggles environment background visibility and mask state.
- `DolocAPI.SetEnvCamera(...)` does not load a background preset and does not offer a safe public-like "sync background to current zoom scale" action.

Background/panorama ownership:

- `DolocAPI.LoadBackground(EnvBackgroundSO background)` owns loading the default or named background preset into `envBackgroundEx`.
- `BackgroundRenderer` owns environment background layers, visibility, mask state, offset, and y-origin. Its layer update path uses camera position, layer move speed, fixed offsets, and stored origins. This is not the same owner as playable orthographic size.
- `BackgroundLayerRenderer` owns each layer's renderer offset behavior. No reviewed native method proved that changing ordinary playable camera size should rescale or recompute all layers safely.

Fog/weather/environment ownership:

- `EnvCovariantController` owns the environment visual stack, including depth fog, building fog, cloud shadow, weather, and day-night rendering.
- `DepthFogController` owns outdoor fog renderer state. Fog behavior is entered through environment/room lifecycle methods, not through the current camera lease owner.

## Rejected Hypotheses

- Manual 2x/4x playable CameraView success is not background-sync proof. It proves movement, centering, native clamp, and orthographic restore in tested paths only.
- Old `ZOOM-042` background/fog compensation screenshots are not current `ICameraViewApi` completion proof. That path was superseded because it mixed `CameraController.RefreshResolution`, forced position refresh, scanner refresh, and background/fog scaling into ordinary playable zoom.
- There is no hidden native one-call owner in the reviewed files that makes background/fog/panorama synchronization safe to patch by trial and error this batch.

## Decision

Do not implement Camera background/fog/panorama runtime changes in the bottom-layer refactor batch.

Keep `ICameraViewApi` Experimental. The next safe step is a separate design/rebuild goal that decides whether background/panorama belongs in a new API or a separate mode, then starts from `DolocAPI.LoadBackground`, `BackgroundRenderer`, `BackgroundLayerRenderer`, `EnvCovariantController`, and `DepthFogController` ownership instead of extending the playable orthographic-size lease by side effect.

## Blocker Conditions

- Any future implementation must name which native method owns room background preset loading, layer offset/origin, fog state, and restoration.
- A future implementation must prove save load, returned-to-title, room transition, and 1x restore with screenshots or telemetry. Build success or CameraView movement smoke alone is not enough.
- Do not promote `ICameraViewApi` or claim background sync complete until those owners and lifecycle proofs are recorded.
