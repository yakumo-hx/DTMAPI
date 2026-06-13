# Priority Roadmap

Status: docs-only roadmap
Date: 2026-06-13

Priority means research and planning order. It does not mean stable API readiness.

| Priority | Research target | Stable API readiness | Final status | Must happen before implementation goals |
| --- | --- | --- | --- | --- |
| P0 | Config, ModRegistry, Translation, GameLaunched, mod entry/helper | Highest | `Stable` / `StableCandidate` | Confirm real mod adoption, load-order behavior, failure isolation, API version/identity rules. |
| P0 | `UpdateTicked`, save/title lifecycle events | Medium-low | `Experimental` | Define event order across title, save load, return title, pause/menu states; document as DTMAPI pump, not Doloc simulation tick. |
| P0 | Input/keybind plus config menu registration shell | Medium | `Experimental`; config registration `StableCandidate` | Prove focus/menu/minigame behavior, key conflict rules, and whether suppress only affects DTMAPI hotkeys. |
| P1 | HUD, tooltip, menu events, world overlay, debug layers | Low-medium | `Experimental` / `Diagnostic` | Build UI host ownership model; screenshot/manual QA for scaling, focus, menu stack, render phases. |
| P1 | Map marker, map overlay, NPC/player location query | Medium | `Experimental` | Keep read-only; prove coordinate conversion, room/scene transitions, marker conflict policy. |
| P1 | Target inspection plus info query | Medium-low | `Proposed` / read-only `Experimental` | Split world target from menu target; return DTOs only; verify item/NPC/crop/machine/menu owners. |
| P1 | World scan, room/location events, object/resource/equipment snapshots | Low-medium | `Experimental` | Native-owner review for `Room`, `Dungeon`, `SceneManager`, resources, vegetation, placed equipment. |
| P2 | Content packs, read-only content index, JSON DTO patch pipeline | Low-medium | `Proposed` -> `Experimental` | Define official JSON/Workshop boundary, load order, cache invalidation, clean-room patch model. |
| P2 | Container query and inventory snapshot | Medium | `Experimental` | Start read-only; prove stable container handles, invalidation, cross-room consistency. |
| P2 | Inventory transaction | Low | high-risk `Experimental` | Native-owner review for `LinearInventory`, `InventorySystem`, `Item.TryCombine/TestCombine`; prove overflow, rollback, save behavior. |
| P2 | Machine query and production API | Low | `Experimental` | Query before automation; separate native machine lifecycle from DTMAPI sidecar loops. |
| P2 | Building query and building content definitions | Low | `Proposed` / `Experimental` | Query-only first; prove build menu, placement, archive/save owners before mutation. |
| P3 | Automation networks | Very low | later `Proposed` | Requires mature world events, container query, inventory transaction, and machine query. |
| P3 | Runtime world entities, custom vehicles, new buildings, map boundary expansion | Not ready | `Blocked` | Need proven lifecycle, render, collision, interaction, save/load owners. |
| P3 | Multiplayer messages | Unknown | `Future-reserved` | Only after Doloc networking model is confirmed. |
| P3 | Debug/cheat mutation: teleport, spawn, time/weather/state changes | Not stable gameplay | `Diagnostic-only` | Dev/test gate, third-save evidence, per-domain native-owner proof. |

## Dependency Chains

```text
Core mod ecosystem
  -> input/config/menu shell
  -> UI host and overlay proof
  -> read-only world/target/content/container/machine query
  -> inventory and machine mutation
  -> automation networks
```

```text
Map marker and info overlays
  -> character/player location query
  -> target inspection
  -> info query DTOs
  -> optional UI overlays
```

```text
Content ecosystem
  -> mod content loading
  -> content source index and boundary docs
  -> DTMAPI content pack shell
  -> JSON DTO patches
  -> asset edit and token/condition framework
```

## Implementation Guidance

- Start implementation goals only from P0 Core-owned surfaces or P1 read-only/overlay experiments.
- Treat P2 as native-owner research before runtime implementation.
- Keep P3 out of ordinary implementation goals except diagnostic prototypes.
- Do not update the public API matrix from this roadmap alone.
