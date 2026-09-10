# Round 1: Semantic API Extraction

Status: complete
Date: 2026-06-13
Mode: parallel read-only extraction

Round 1 split the user-provided ten-mod SMAPI summary into five independent extraction lanes.

## Lane A: Lifecycle, Events, Content, Debug

Core findings:

- `IGameLoopEvents` is a P0 ecosystem need.
- `UpdateTicked` must be treated as a DTMAPI pump event, not a Doloc native tick.
- Content pipeline needs `IModContent`, `IGameContent`, `AssetRequested`, and cache invalidation concepts.
- Console commands and mod API exchange are ecosystem infrastructure.
- Debug/cheat menus must become Diagnostic-only.

## Lane B: Input, HUD, Menu, Overlay, Target Inspection

Core findings:

- Input/keybind APIs are P0 research targets but not stable by default.
- HUD, menu, tooltip, and overlay APIs must be split by render phase and owner.
- `TargetInspectionApi` and `InfoQueryApi` look like UI APIs but actually require multiple GameBridge native owners.
- Data Layers style overlays are high-value developer diagnostics.

## Lane C: Containers, Inventory, Machines, Automation

Core findings:

- Automate-like behavior requires three primitives first: world changes, inventory/container transaction, and machine query.
- `IContainerApi.GetContainers` can start read-only.
- `IInventoryTransactionApi` is high risk.
- `IMachineAutomationApi` and automation networks must wait for read-only query and transaction layers.

## Lane D: Maps, NPC/Player Locations, Buildings, Entities, Vehicles

Core findings:

- `IMapMarkerApi`, `IMapOverlayApi`, `ITargetInspectionApi`, and `IInfoQueryApi` are high-value read-only/overlay candidates.
- NPC/player location query is plausible, but NPC mutation is not.
- Custom buildings, runtime entities, and custom vehicles must remain Proposed or Blocked until Doloc owners are proven.

## Lane E: Ecosystem Infrastructure

Core findings:

- `IModRegistry`, config/data/translation, config menu protocol, console commands, and content-pack discovery are the strongest ecosystem candidates.
- Content Patcher maps to two layers: lower-risk content pack/config/token infrastructure and higher-risk asset edit actions.
- Multiplayer should remain Future-reserved unless Doloc networking exists and is reviewed.

## Round 1 Initial Shape

```text
P0: Core ecosystem surfaces
P1: UI host, overlay, query, content boundary research
P2: containers, inventory transaction, machines, content patching
P3: runtime entities, custom vehicles, multiplayer, debug mutation
```
