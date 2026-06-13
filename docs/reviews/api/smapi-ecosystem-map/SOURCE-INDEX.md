# SMAPI Ecosystem Source Index

Status: docs-only source map
Date: 2026-06-13

This source index records the input used for the SMAPI ecosystem semantic API map.

## Primary Input

The source was the user's pasted summary of ten SMAPI C# mods and their ecosystem capabilities. The review used that user-provided summary only. It did not browse upstream repositories during this four-round workflow.

The summary covered these representative mods:

| Mod | Ecosystem signal used in this review |
| --- | --- |
| Automate | Event bus, machine automation, containers, data/config, console commands, mod API, multiplayer messages, world object changes. |
| Chests Anywhere | Remote containers, inventory UI, HUD/menu overlays, input suppression, mod integration, multiplayer notifications. |
| Lookup Anything | Target inspection, info query, HUD/menu rendering, translations, data files, reflection risk. |
| Data Layers | Visual overlay layers, input suppression, console commands, config, translation, mod API, event priority. |
| Tractor Mod | Custom building/entity/vehicle semantics, world rendering, input, multiplayer routing, location scanning. |
| NPC Map Locations | Map markers, minimap/map UI, NPC/player locations, custom locations, multiplayer marker sync. |
| UI Info Suite 2 | HUD icons, hover info, range overlays, per-save data, config menu integration. |
| Content Patcher | Content-pack discovery, asset edit semantics, token/condition/action framework, locale/cache concerns. |
| Generic Mod Config Menu | Unified config menu protocol, mod API exchange, menu UI, input/render integration. |
| CJB Cheats Menu | Debug/cheat menu semantics, console/debug ecosystem, teleport/spawn/time/weather mutation risk. |

## Clean-Room Boundary

This review extracts semantic needs only:

- event bus needs;
- content pipeline needs;
- config/data/translation needs;
- input/UI/overlay needs;
- mod integration needs;
- world/container/machine/map/entity needs;
- debug and diagnostic needs.

It does not copy SMAPI APIs, SMAPI code, Content Patcher behavior, Stardew Valley implementation details, or third-party mod implementation logic.

## What This Source Can Prove

- Mature mod ecosystems need common surfaces for events, content, config, UI, data, mod integration, and diagnostics.
- DTMAPI should split ecosystem APIs by owner layer: Core, UI host, GameBridge read-only, GameBridge mutation, Diagnostic, Blocked, and Future-reserved.
- DTMAPI should design lower-risk read-only/query/overlay APIs before high-risk runtime mutation or creation APIs.

## What This Source Cannot Prove

- That DTMAPI should be SMAPI-compatible.
- That DTMAPI can stabilize any API listed here without Doloc native-owner review.
- That Content Patcher semantics should be copied.
- That Doloc Town has equivalent native owners for Stardew Valley concepts.
- That public API matrix statuses should change.

## Related DTMAPI Research

- `docs/reviews/api/native-owner-domains/INDEX.md`
- `docs/reviews/api/local-mods-native-owner/INDEX.md`
- `docs/reviews/api/native-function-map/README.md`
- `docs/api/public-api-matrix.md`
