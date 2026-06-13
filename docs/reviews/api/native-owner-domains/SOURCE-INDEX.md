# Native Owner Domain Source Index

This file records the source set used by the initial native-owner domain library pass. It is a pointer index only. Do not copy decompiled method bodies or official DLL/source content into DTMAPI.

## Required Project Context

- `AGENTS.md`
- `PROJECT.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/reviews/README.md`
- `docs/api/public-api-matrix.md`
- `references/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`

## Reverse Baselines

- Primary: `references/doloc-town/reverse/builds/23465763_workshop_38581E`
- Comparison: `references/doloc-town/reverse/builds/23249387_workshop_247ACD`

Primary reverse maps used:

- `maps/GameLoop_Scene.md`
- `maps/Save_Load.md`
- `maps/Assets_Content.md`
- `maps/Action_Interaction.md`
- `maps/Resource_Gathering.md`
- `maps/Items_Inventory.md`
- `maps/Recipe_Crafting.md`
- `maps/Motor.md`
- `maps/NPC_Dialogue.md`
- `maps/Shop.md`
- `maps/UI.md`
- `maps/index/*-methods.csv`
- `maps/index/*-fields.csv`
- `maps/index/*-calls.csv`

## Official Workshop Documentation

Official docs are evidence for published content/beauty workflows, not proof of runtime mutation safety.

Relevant docs under `references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md` include:

- `004_*` Workshop mod type overview
- `006_*` player body, hats, tools
- `009_*` NPC
- `013_*` small animals
- `014_*` base content mods
- `015_*` new hats
- `016_*` resources
- `018_*` beauty texture replacement
- `019_*` buildings
- `022_*` vehicles
- `025_*` new platform case
- `026_*` drop cap and pity config
- `027_*` drop library ID table
- `028_*` platform
- `032_*` new resources
- `033_*` new cooking
- `035_*` new crops
- `036_*` new vegetation
- `040_*` NPC ID table
- `043_*` equipment ID table
- `044_*` small animal ID table
- `045_*` new store goods
- `046_*` new items
- `047_*` new equipment case
- `048_*` new decoration equipment
- `049_*` new recipes
- `050_*` resource and vegetation ID table
- `052_*` cooking ID table
- `053_*` platform ID table

## Existing Review Records To Cross-Check

- `docs/reviews/api/2026/20260607-0006-native-responsibility-method-audit-index.md`
- `docs/reviews/api/2026/20260607-0008-native-owner-special-audits-index.md`
- `docs/reviews/api/2026/20260607-0009-native-owner-special-audits-index.md`
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit-index.md`
- `docs/reviews/api/2026/20260610-fishing-native-responsibility.md`
- `docs/reviews/api/2026/20260610-saveslots-native-responsibility.md`
- `docs/reviews/api/2026/20260612-camera-background-native-owner-review.md`
- `docs/reviews/api/2026/20260612-crops-harvesting-native-responsibility.md`

## Initial Parallel Exploration Slices

The initial 2026-06-13 pass used read-only parallel exploration. These slices should be treated as seed findings, not final implementation proof:

- World and maps: time, weather, season, room, dungeon, teleport, resources, vegetation.
- Characters: NPCs, livestock, wild birds, stores, dialogue, movement.
- Drones and vehicles: drone item/config path, slots, active-drone singleton, motor singleton.
- Inventory and effects: stack limits, equipment slots, hats, food, buffs, equipment functions.
- Original content gaps: follower pet, handheld ranged weapons, projectile/damage reuse.

## Console Use Boundary

The game console/Y-console may be used in future follow-up reviews to collect evidence, but console success is not stable API proof. Any console-backed runtime evidence must record save slot, log output, native owner state, clean exit, and whether the route was debug-only.
