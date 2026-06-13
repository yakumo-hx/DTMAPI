# Round 3: Revised API Map

Status: complete
Date: 2026-06-13
Mode: parallel read-only revision

Round 3 produced six revised report blocks.

## Ecosystem Foundation

| API / capability | Owner layer | Revised status | Key note |
| --- | --- | --- | --- |
| Mod entry / manifest / helper | Core-owned | `Stable` / `StableCandidate` | Does not need Doloc native owner. |
| `GameLaunched` | Core-owned | `StableCandidate` | Use as all-mods-loaded integration point, pending load-order proof. |
| `UpdateTicked` | Core pump | `Experimental` | DTMAPI pump event, not native simulation tick. |
| Save/title lifecycle | Core plus save boundary | `Experimental` | Sidecar cleanup/data write only first. |
| Config read/write | Core-owned | `Stable` | DTMAPI-owned file config. |
| Data files | Core-owned and save-bound variants | `Proposed` | Split `ModData`, `SaveData`, `TempSessionData`. |
| Console commands | Core plus Diagnostic split | `Proposed` / `Diagnostic` | Ordinary author commands and debug commands are separate. |
| Mod registry/API exchange | Core-owned | `StableCandidate` | Registry-only truth, not official Workshop truth. |
| Local mod messages | Core-owned | `Proposed` | Same-process only, not multiplayer. |

## Input, UI, Overlay

| API / capability | Owner layer | Revised status | Key note |
| --- | --- | --- | --- |
| Input/keybind | Core event facade plus UI state | `Experimental` | Needs focus/menu/minigame proof. |
| Input suppression | Core helper plus UI host | `Experimental`; native isolation unproven | Clarify DTMAPI keybind suppression only. |
| Config menu protocol | Core protocol plus DTMAPI UI host | `StableCandidate` for registration | Runtime UI remains separate. |
| HUD/menu/tooltip/overlay | UI host plus adapters | `Proposed` / `Experimental` | Split each surface and prove render/focus lifecycle. |
| Target inspection | UI host plus GameBridge adapters | `Proposed` / `Experimental` | First version should be read-only world targets. |
| Info query | GameBridge read-only DTO | `Experimental read-only` | DTO-only and no mutation. |
| Debug/data layers | Diagnostic UI host | `Diagnostic` / `Experimental` | Developer visualization first. |
| Debug/cheat tools | Diagnostic plus GameBridge adapters | `Diagnostic` | Dev/test gate required. |

## Content Pipeline

| Capability | Owner layer | Revised status | Key note |
| --- | --- | --- | --- |
| Read-only content query | Core content index | `Experimental` | Seeing a source is not proof the game loaded it. |
| Content-pack discovery | Core-owned | `Proposed` | DTMAPI content packs only. |
| Official JSON/Workshop boundary | Docs first | `Docs-only` / `Proposed` | Define load order, overwrite rules, and read-only boundaries. |
| Mod content loading | Core-owned | `StableCandidate` | Low-risk own-mod file loading. |
| Game content read | GameBridge/content adapter | `Experimental` | DTO-only read. |
| Asset requested/edit pipeline | Content pipeline plus GameBridge adapters | `Proposed` | Start with JSON/data DTOs, images/maps later. |
| Token/condition/action framework | Core content framework plus optional GameBridge query | `Proposed / P2` | Do not commit to Content Patcher compatibility. |
| Cache invalidation | Content pipeline | `Proposed` | Requires load order, rollback, and safe trigger rules. |

## World, Container, Machine

| API / capability | Layer | Revised status | Key note |
| --- | --- | --- | --- |
| `IWorldScanApi` | GameBridge read-only query | `Experimental` | Snapshot DTOs first. |
| `IRoomEvents`, `ILocationEvents` | Core facade plus GameBridge lifecycle | `Experimental` | Event order matrix required. |
| `IWorldObjectEvents` | GameBridge native-owner needed | `Proposed` / `Experimental` | Do not fake stable events with polling. |
| `IContainerApi.GetContainers` | GameBridge query | `Experimental` | Not a global stable container system. |
| `IInventorySnapshotApi` | GameBridge query | `Experimental` | DTO item stacks only. |
| `IInventoryTransactionApi` | GameBridge mutation | high-risk `Experimental` | Needs overflow, rollback, cursor/buffer, save proof. |
| `IContainerUiApi.OpenContainer` | UI host plus GameBridge | `Deep Experimental` / `Blocked` | UI, input, and inventory cross-domain risk. |
| `IMachineQueryApi` | GameBridge query | `Experimental` | Read-only first. |
| `IMachineAutomationApi` / network | DTMAPI scheduler plus primitives | later `Proposed` | Wait for query, transaction, and world events. |

## Map, Inspection, Entity

| API / capability | Layer | Revised status | Key note |
| --- | --- | --- | --- |
| `IMapMarkerApi`, `IMapOverlayApi` | UI host plus read-only GameBridge | `Experimental` | No NPC state mutation. |
| `ICharacterLocationApi` | GameBridge read-only | `Experimental` | Query only. |
| `ITargetInspectionApi` | UI host plus GameBridge adapters | `Proposed` / `Experimental` | Split world targets from menu targets. |
| `IInfoQueryApi` | Content query plus GameBridge read-only | `Experimental read-only` | Trace DTO fields to native owners. |
| `IBuildingQueryApi` | GameBridge read-only | `Proposed` / `Experimental` | Query before mutation. |
| `IBuildingContentApi` | Content pipeline plus native owner needed | `Proposed` | Definition concepts only. |
| `IWorldEntityApi` registry | Core registry concept | `Proposed` | Split from runtime. |
| `IWorldEntityApi` runtime | GameBridge runtime | `Blocked` | No proven lifecycle owner. |
| `IMotorVehicleApi` | GameBridge native motor adapter | `Experimental` | Native flying motor only. |
| `IVehicleRegistryApi` | GameBridge runtime | `Blocked` | No custom vehicle owner. |
| `IMultiplayerApi` | future | `Future-reserved` | No implementation promise. |

## Revised Roadmap

The final roadmap is stored in [Priority Roadmap](../priority-roadmap.md).
