# 20260711-0002 Zoom First-Party Owner Lifetime Audit

## Metadata

- Date: 2026-07-11
- Status: `recorded`
- Scope: promote ZoomMod from a test fixture directory to a first-party product Mod without changing its feature or public API behavior, then verify ordinary-Mod, Camera lease, and same-process owner-disable boundaries.
- Source request: user requested ZoomMod productization after the common Owner Lifetime refactor was audited and committed.

## Facts Inspected

- `testmods/ZoomMod` is already an ordinary `DtmMod` project targeting `netstandard2.0` and references only `DTMAPI.Abstractions`.
- Product identity is already established: `DTMAPI.ZoomMod`, product version `0.4.2-dtmapi`, source assembly/manifest entry `ZoomMod.dll`, release package DLL `DTMAPI.Zoom.dll`, official folder `DTMAPI_Zoom`, and Workshop item `3742717440`.
- The Mod owns config, translations, `+/-` keybind interpretation, config-menu callbacks, three lifecycle event subscriptions, and one `ICameraViewApi` lease. It does not own Unity reflection, Harmony, native Camera objects, background, fog, panorama, or room-range state.
- The current input path uses local `DtmKeybindList.JustPressed(helper.Input)` queries rather than persistent product keybind registrations. Alias canonicalization remains a Core input concern.
- Owner Lifetime Update `20260711-0010` makes ordinary-Mod Entry publication atomic and routes failure, disable, removal, dependency invalidation, shutdown, API/config/event/input/content, and GameBridge owner roots through one deactivation coordinator.
- `ICameraViewApi` remains Experimental and orthographic-size-only. Save/title preservation of the process service and lease request is a GameBridge contract; ZoomMod may still explicitly set its own lease scale to `1x` from its existing callbacks.
- User manual evidence in `docs/reviews/manual-qa/2026/20260611-0001-refactor-manual-qa-code-review.md` already passes real 2x/4x movement, centering, native clamp, no jump/flicker, title reset, and no residual 4x for the scoped playable path. It also keeps background synchronization as a known non-goal, so productization neither reopens nor closes that separate visual boundary.

## Rejected Hypotheses

- Moving ZoomMod to `first-party-mods` does not justify moving its feature into Core, Bootstrap, or GameBridge.
- Old CameraZoom background/fog compensation and screenshots are not current CameraView completion proof and must not be restored in the product Mod.
- Build success, official package installation, or `Smoke.CameraPlayable` alone does not prove actual ordinary-Mod owner disable.
- Same-process deactivation must not unload the Mono assembly, rerun `Entry`, reverse unknown third-party state, or remove the process-lifetime GameBridge Camera provider.

## Required Invariants

- Preserve public API signatures and `0.5.3-alpha` runtime version.
- Preserve ZoomMod source behavior, config schema/defaults, i18n, product version, minimum dependency versions, IDs, assembly/package names, official folder, and Workshop ID.
- Keep the product project `netstandard2.0`, Abstractions-only, and free of Harmony/reflection/native types/internal APIs.
- Build, installer, release, publish metadata, solution, and source tests must use `first-party-mods/ZoomMod`; historical records may retain their original path.
- Owner deactivation must remove the real Zoom owner Event/Input/API-facade/ConfigPage/Content/loaded/instance and Camera lease roots, keep the Camera provider alive, mark the loaded assembly restart-required, reject stale facade use, and refuse same-process re-entry.

## Acceptance Gates

- Full Release build/unit and document governance pass with no public API/version drift.
- Release packaging still emits official folder `DTMAPI_Zoom`, package DLL `DTMAPI.Zoom.dll`, unchanged manifest/config/i18n/assets, and expected Workshop metadata.
- Unit/source gates prove first-party ordinary-Mod architecture and real owner-disable semantics.
- Third-save game smoke loads the real Zoom product, proves a real Camera lease root, deactivates that owner in-process, reaches zero authoritative Core/GameBridge roots, retains the process Camera provider, reports restart-required/no re-entry, passes HookProbe/SaveLoaded/fatal/process-exit, and leaves no `DolocTown.exe`. The long CameraPlayable sequence must not be combined with the title-button orchestrator which exits first; real-product unit coverage plus `20260711-0010` runtime evidence own the independent SaveLoaded/ReturnedToTitle preservation gate.
- No Camera API stability promotion, Hook Map change, or background/fog/panorama claim is made.

## Resolution Link

Implementation and validation are owned by [Update 20260711-0013](../../../updates/2026/20260711-0013-first-party-zoom-owner-lifetime.md).
