# DTMAPI Gap Map

Status: docs-only gap map
Date: 2026-06-13

This map groups the candidate APIs by the layer that would need to own them.

## Core-Owned Or Mostly Core-Owned

| Candidate | Final status | Gap |
| --- | --- | --- |
| `DtmMod`, manifest, `IDtmHelper` | `Stable` / `StableCandidate` | Keep adoption and manifest dependency validation current. |
| `ReadConfig<T>`, `WriteConfig<T>` | `Stable` | Keep migration/default behavior documented. |
| `GameLaunched` | `StableCandidate` | Load order, failure isolation, and real mod regression evidence. |
| `IModRegistry.GetApi<T>`, `RegisterApi<T>` | `StableCandidate` | Provider identity, API versioning, spoof/conflict policy. |
| Translation helper | `StableCandidate` | Do not claim official game language ownership. |
| `IDataApi` | `Proposed` | Split `ModData`, `SaveData`, and `TempSessionData`. |
| Console author API | `Proposed` / `Diagnostic` | Split ordinary author commands from debug/cheat commands. |
| Local mod message bus | `Proposed` | Keep separate from future multiplayer. |

## UI-Host-Owned

| Candidate | Final status | Gap |
| --- | --- | --- |
| Config menu registration UI | `StableCandidate` for registration only | Runtime UI, save/cancel, paging, live preview, and visual QA. |
| HUD draw | `Proposed` / `Experimental` | Render phase, scaling, focus, menu state, screenshot QA. |
| Menu events and overlays | `Proposed` / `Experimental` | Official menu adapters, no raw native menu exposure. |
| Tooltip/info panel host | `Experimental` | Provider merge order, hover target stability, DTO source tracking. |
| World overlay and debug layers | `Diagnostic` / `Experimental` | Coordinate conversion, layer order, map transition cleanup. |
| Input suppression | `Experimental` | Clarify DTMAPI hotkey suppression versus native input isolation. |

## GameBridge Read-Only Or Query-First

| Candidate | Final status | Gap |
| --- | --- | --- |
| `IWorldScanApi` | `Experimental` | DTO snapshots, throttling, no raw `Room` or Unity objects. |
| `IRoomEvents`, `ILocationEvents` | `Experimental` | Event order across title, save load, dungeon, room, and return-to-title. |
| `IWorldObjectEvents` | `Proposed` / `Experimental` | Reliable dirty signal for room objects, resources, equipment, vegetation, and buildings. |
| `IMapMarkerApi`, `IMapOverlayApi` | `Experimental` | Coordinate conversion, marker conflict policy, map/minimap lifecycle. |
| `ICharacterLocationApi` | `Experimental` | NPC/player position holders and transition consistency. |
| `ITargetInspectionApi` | `Proposed` / `Experimental` | Split world targets and menu targets; avoid reflection-only menu scraping. |
| `IInfoQueryApi` | `Experimental read-only` | DTO fields traced to item, NPC, crop, building, or machine owners. |
| `IContainerApi.GetContainers` | `Experimental` | Stable handles, invalidation, cross-room consistency, hidden/invalid container filtering. |
| `IInventorySnapshotApi` | `Experimental` | DTO item identity, stack caps, player/chest/machine/cursor distinctions. |
| `IMachineQueryApi` | `Experimental` | Machine type coverage, storage/renderer/recipe holders, room equipment enumeration. |
| `IBuildingQueryApi` | `Proposed` / `Experimental` | Building registry/state holder, footprint, entrance, owning room. |

## GameBridge Mutation Or High-Risk Automation

| Candidate | Final status | Gap |
| --- | --- | --- |
| `IInventoryTransactionApi` | high-risk `Experimental` | Partial placement, overflow, rollback, cursor/buffer, save consistency. |
| `IContainerUiApi.OpenContainer` | `Deep Experimental` / `Blocked` | Container UI owner, menu stack, input suppression, close/save behavior. |
| `IMachineProductionApi` mutation | high-risk `Experimental` | Machine lifecycle, save/load, offline/cross-room behavior, storage transaction. |
| `IMachineAutomationApi` | later `Proposed` | Requires machine query, inventory transaction, and world events first. |
| `IAutomationNetworkApi` | P2/P3 `Proposed` | Requires container, machine, inventory, and world primitives mature enough. |
| Content asset patch/edit pipeline | `Proposed` | Official JSON/Workshop load order, cache invalidation, rollback, conflict order. |

## Diagnostic-Only

| Candidate | Final status | Gap |
| --- | --- | --- |
| `IDebugToolsApi` | `Diagnostic-only` | Dev/test gate, third-save evidence, no ordinary gameplay dependency. |
| Teleport/spawn/time/weather mutation | `Diagnostic-only` | Per-domain native-owner proof and clean exit evidence. |
| Data layer/collision debug overlays | `Diagnostic` / `Experimental` | Overlay lifecycle and query owner proof. |
| Content conflict diagnostics | `Diagnostic` / `Proposed` | Useful before patch pipeline stability. |

## Blocked Or Future-Reserved

| Candidate | Final status | Gap |
| --- | --- | --- |
| Runtime `IWorldEntityApi` spawn/update/render/interact | `Blocked` | No stable lifecycle, save/load, render, collision, or interaction owner. |
| `IVehicleRegistryApi` for custom vehicles | `Blocked` | Native multi-vehicle registry not proven. |
| Runtime building creation/deletion/space expansion | `Blocked` | Building save/archive, placement, collider/grid mutation owners not proven. |
| Map boundary expansion | `Blocked` | Runtime map boundary mutation remains high risk. |
| `IMultiplayerApi` | `Future-reserved` | Doloc networking model is unknown. |
