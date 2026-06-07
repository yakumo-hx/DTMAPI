# DTMAPI Runtime Hardening And Branch Roadmap

Date: 2026-06-08
Status: archived roadmap
Source: user-provided architecture review and Git-scope cleanup follow-up

## Purpose

This record archives the next-stage direction before new feature branches start.
The current baseline is intentionally treated as a checkpoint: future changes should be done on narrow branches, verified, then merged back only after the branch goal is complete.

## Current Diagnosis

DTMAPI is a middle-stage experimental framework with many working features, but it is not yet a stable ModdingAPI product.

The main gap is not hook count. The main gap is product reliability:

- runtime guardrails are not hard enough;
- `DTMAPI.GameBridge.DolocTown` is too concentrated;
- public and experimental APIs are too close together;
- some APIs were promoted from test-mod or smoke success rather than native-owner evidence;
- tests need per-API real gameplay matrices, not only broad smoke checks.

The project direction remains:

```text
BepInEx bootstrap -> DTMAPI Core -> DolocTown GameBridge -> stable public API -> DTMAPI mods
```

Fragile Harmony/reflection/Unity/game-type logic belongs behind the GameBridge boundary. Public APIs should be smaller, steadier, and explicit about stable, experimental, diagnostic, proposed, failed, or blocked status.

## Branch Workflow

Use the cleaned `master` branch as the local baseline.

Recommended workflow:

```text
master
  -> codex/runtime-hardening
  -> codex/api-matrix-honesty
  -> codex/gamebridge-mechanical-split
  -> codex/camera-view-rebuild
  -> codex/content-pipeline
```

Rules:

- Create one branch per narrow objective.
- Do not mix runtime hardening, API redesign, feature mods, and bug fixes in one branch.
- Keep branch goals tied to one immutable `docs/goals/YYYY/...md` file when implementation is delegated.
- Merge back only after the branch has passing build/tests and required game/manual evidence.
- If a branch goes bad, discard the branch rather than repairing `master`.

## Priority 1: Runtime Hardening

Do not continue broad API expansion before the runtime is harder.

High-priority fixes:

- manifest dependency alias compatibility: `Required` and `IsRequired`;
- dependency version checks: required dependency version, DTMAPI version, game version;
- circular dependency diagnostics;
- duplicate `UniqueID` diagnostics;
- `EntryDll` must be relative and must not escape the mod folder;
- owner-bound API registration so mods cannot spoof another owner;
- high-frequency event failure circuit breaker for `UpdateTicked` and similar events;
- broken config JSON backup plus default restore;
- atomic config writes;
- clear `Input.Suppress` scope and release semantics;
- timer fallback must not dispatch ordinary mod update callbacks off the Unity main thread.

Goal shape:

```text
Core runtime hardening only.
Do not modify GameBridge hooks.
Do not repair CameraZoom here.
Add unit tests for manifest/dependency/path/config/event/input/runtime guardrails.
```

## Priority 2: Public API Matrix Honesty

The API matrix must describe what ordinary mods can safely depend on.

Use clear statuses:

```text
stable
stable-candidate
experimental
diagnostic-only
proposed
failed
blocked
```

Important current conclusions:

- CameraZoom 0.4.2 must not be treated as stable or complete after manual failure.
- Custom entity definitions can be contract-level stable candidates, but runtime spawn/summon/execute paths must remain experimental or blocked until native runtime ownership is proven.
- ConfigMenu is a strong product candidate, but author-facing registration APIs should be separated from internal runtime page-editing APIs.
- Debug console and advanced debug features should be diagnostic-only unless deliberately promoted.

## Priority 3: GameBridge Mechanical Split

`DTMAPI.GameBridge.DolocTown` should be split before more risky API work.

Do the first pass as behavior-preserving mechanical movement:

```text
DTMAPI.GameBridge.DolocTown/
  Hooking/
  Features/
    Save/
    Workshop/
    InputUi/
    Camera/
    Fishing/
    ActionSpeed/
    Equipment/
    Vehicles/
    Machines/
    StrongPlantingGun/
    DebugConsole/
    CustomEntities/
  Smoke/
  Diagnostics/
```

Rules:

- Do not change hook behavior during the mechanical split.
- Keep hook IDs, log text, and smoke evidence comparable where possible.
- Each feature bridge should eventually own its own state, hooks, diagnostics, and smoke matrix.

## Priority 4: CameraView Rebuild

The CameraZoom failure showed that playable zoom and panorama/room-fit camera control are different APIs.

Playable zoom should be rebuilt as a narrow lease-based API:

```text
ICameraViewApi
ICameraViewLease
CameraViewState
```

Playable zoom rules:

- keep the player naturally centered/followed;
- avoid taking over `SetPosition`;
- avoid mixing background/fog/room-fit ownership into ordinary zoom;
- support multiple mod requests through DTMAPI arbitration;
- restore vanilla camera state on release, scene change, mod unload, or failure.

Panorama/full-farm camera work should be separate:

```text
IPanoramaCameraApi
```

That API can research background, fog, room range, scanner refresh, UI hiding, and camera movement explicitly without destabilizing ordinary zoom.

## Priority 5: Content Pipeline

DTMAPI should evolve from a code mod loader plus utility hooks into a safer content pipeline.

Future direction:

- content pack support;
- structured data edits;
- content cache invalidation;
- official/Workshop enablement respect;
- conflict diagnostics when multiple mods edit the same target;
- no direct takeover of official content loading unless a GameBridge owner path is known.

## Testing Direction

Use five layers:

1. Open-source build tests: clone/build/test/package without local game binaries.
2. Core unit tests: manifest, dependency, path safety, registry, config, input, event guardrails.
3. Fake bridge or feature service tests: state machines without launching the game.
4. General game smoke: startup, HookProbe, third save, exit, no leftover process.
5. Feature-specific smoke/manual QA: camera movement, UI feel, flicker, save/load, input, lifecycle, visual regressions.

Build success and broad smoke are not enough for player-visible or visual APIs.

## Do Not Forget

- Do not infer stability from one screenshot, one smoke helper, or UI-only success.
- Do not stabilize an API unless native responsibility, state ownership, failure mode, and ordinary-mod usability are known.
- Do not keep adding APIs before the runtime can survive bad manifests, bad configs, bad dependencies, bad event handlers, and path abuse.
- Every branch should leave a traceable update record.
