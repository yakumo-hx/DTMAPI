# 20260607-0011 - CameraZoom Method-Body Native Owner Review

Date: 2026-06-07
Scope: pre-implementation review for `docs/goals/2026/20260607-0002-042-camerazoom-api-rebuild.md`.
Result: proceed with a narrow 0.4.2 experimental rebuild; do not claim stable panorama support.

## Files Read

- `docs/goals/2026/20260607-0002-042-camerazoom-api-rebuild.md`
- `docs/reviews/api/2026/20260607-0008-native-owner-special-audits/02-camera-zoom.md`
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/07-all-api-risk-closure-table.md`
- `docs/reviews/manual-qa/2026/20260607-0001-panorama-background-zoom-note.md`
- `references/doloc-town/research-notes/research-DolocPlus-function-map-20260607.md`
- `references/doloc-town/research-notes/research-DolocPlus-deep-dive-20260607.md`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocAPI.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/CameraController.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/BackgroundRenderer.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/Room.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/RoomPatch.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/Dungeon.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/EnvCovariantController.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/DepthFogController.cs`
- `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/DepthFogControllerBuilding.cs`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/HarmonyReflectionPatcher.cs`
- `testmods/ZoomMod/ModEntry.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`

## Native Owner Answers

Main camera size and position:

- `DolocAPI.mainCamera` owns the Unity camera object used by gameplay. The current DTMAPI code reaches only its `orthographicSize` member.
- `DolocAPI.cameraController` owns gameplay camera range and position. `CameraController.RefreshResolution()` recomputes `camSize` from `DolocAPI.worldResolution` and the current `mainCamera.orthographicSize`, then refreshes the stored room range. `CameraController.SetPosition(DolocAPI.AgentPosition)` recenters/clamps the camera after the size change.
- `DolocAPI.SetEnvCamera(...)` is the native environment-camera boundary used on room/dungeon transitions. It calls `cameraController.SetRoomRange(...)`, optionally follows the player, then sets background visibility/masking.

Outdoor/farm background placement, size, parallax, and clipping:

- `DolocAPI.envBackgroundEx` is the native `BackgroundRenderer` singleton.
- `Room.OnEnterRoom()` calls `DolocAPI.SetEnvCamera(Room.CameraPosition, Room.CameraSize, Room.ShouldShowBackground, Room.ShouldMaskBackground)` and then `RoomPatch.HandleBackground(...)`.
- `RoomPatch.HandleBackground(...)` sets `envBackgroundEx` visible and loads `room.baseProto.background` through `DolocAPI.LoadBackground(...)`.
- `BackgroundRenderer` owns sky/layer renderers and parallax updates against the camera position. It does not automatically rescale when `orthographicSize` changes, so DTMAPI must backup/apply/restore its transform scale for large-view zoom.

Depth fog, darkness, and edge masks:

- `DolocAPI.EnvCovariantController` owns the environment visual stack.
- `EnvCovariantController.OnEnterRoom(...)` drives private `_depthFogController` and `_depthFogControllerBuilding` state from `room.IsInHouse` and `room.ShouldShowBackground`.
- `DepthFogController` owns outdoor fog renderers; `DepthFogControllerBuilding` owns the indoor/window-style sky and remote fog renderers. Both are native visual owners that can be reached from GameBridge by reflecting the controller fields and scaling/restoring their transforms.

Room render range and connected-room visibility:

- `Room.Render(...)` and `Room.ClearRender(...)` own normal room render visibility.
- `Dungeon._RenderNearRooms(Room)` renders the current dungeon room plus `proto.neighbourRooms`; it is not camera-size-aware.
- The 0.4.2 CameraZoom goal should record this as a boundary: it can refresh camera range/scanners and compensate background/fog, but it should not claim arbitrary cross-room panorama rendering. A full panorama/photo API needs a separate room-fit/render policy.

Lifecycle resets:

- Save load: native `DolocAPI.AfterLoadArchiveData(...)` calls `cameraController.RefreshResolution()` and then the DTMAPI `AfterLoadArchiveDataPostfix` currently resets CameraZoom.
- Returned to title: native `DolocAPI.ReturnHome(...)` clears `EnvCovariantController`, hides `envBackgroundEx`, disables/positions `cameraController`, unloads the save, and DTMAPI `ReturnHomePostfix` resets CameraZoom.
- Room/map transitions: `Room.OnEnterRoom()`, dungeon entry, and other transport paths call `DolocAPI.SetEnvCamera(...)` / `cameraController.SetPosition(...)` / `RoomPatch.HandleBackground(...)`, which can overwrite zoom-related camera/background/fog state. DTMAPI needs a hook or runtime refresh tied to this owner path, not only per-frame orthographic writes.
- Scanner state: `DolocAPI.RefreshScanner()` refreshes room and building scanners after camera/room state changes; DTMAPI should call it after zoom apply/restore when available.

UI/input isolation:

- The rebuilt CameraZoom API should leave UI scale unchanged and report that explicitly in state/status.
- ZoomMod input remains a normal DTMAPI hotkey consumer. Gameplay hotkeys are already gated by the runtime UI context, while the Y-console uses separate native input suppression hooks. CameraZoom should not add raw `DolocUserInput` spoofing or config-menu scale changes.

Current DTMAPI proof gap:

- `ICameraZoomApi` currently stores owner options/state and writes only `DolocAPI.mainCamera.orthographicSize` or `UnityEngine.Camera.main.orthographicSize`.
- The 0.3.0 smoke proves 4x/restore orthographic-size writes, not `CameraController.RefreshResolution`, background/fog compensation, room-transition restore, or background-small-frame prevention.
- `testmods/ZoomMod` is only an API consumer. It must not become the place where native camera/background/fog fixes live.

## Implementation Constraints For 0.4.2

- Keep fragile Unity/Harmony/reflection logic in `DTMAPI.GameBridge.DolocTown`.
- Keep public DTOs free of raw Unity or decompiled types.
- Add owner/report fields for requested, clamped, and applied view scales; camera-controller refresh; background compensation; fog compensation; lifecycle restore; UI scale isolation; and failure reasons.
- Apply zoom through the camera owner path: write `orthographicSize`, call `CameraController.RefreshResolution()`, call `CameraController.SetPosition(DolocAPI.AgentPosition)` when available, and refresh scanners when available.
- Backup and restore original `envBackgroundEx`, `_depthFogController`, and `_depthFogControllerBuilding` transform scales. Compensation scale is based on applied orthographic size divided by vanilla orthographic size.
- Reapply after environment-camera resets by patching or observing a native room/environment camera boundary, preferably `DolocAPI.SetEnvCamera(...)`, with the existing per-frame refresh as a fallback.
- Treat missing background/fog owner as partial failure in API results/state. Do not report full success if only the camera write succeeded for a zoom above 1x.

## Proceed / Block Decision

Proceed: native camera, background, fog, room, lifecycle, scanner, and UI/input owner boundaries are identified well enough for a narrow CameraZoom rebuild.

Do not complete the goal unless third-save evidence proves:

- 4x applies camera-controller refresh plus background/fog compensation.
- 1x/reset restores vanilla camera/background/fog scales.
- Save/title/room lifecycle does not leave stale compensated state.
- The farm/outdoor 4x view no longer shows the background as a small framed rectangle.
