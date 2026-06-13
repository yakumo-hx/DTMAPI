# Native Owner Domain Library

Status: active, docs-only
Created: 2026-06-13
Primary reverse baseline: `references/doloc-town/reverse/builds/23465763_workshop_38581E`
Comparison baseline: `references/doloc-town/reverse/builds/23249387_workshop_247ACD`

This directory is the long-lived index for DTMAPI native-owner discovery by gameplay/content domain. It maps fuzzy author-facing goals to Doloc Town native responsibility functions and state holders before any GameBridge or public API rebuild starts.

This library is not a stability promotion record. It does not add public APIs, does not mark APIs stable, and does not prove runtime behavior without later implementation and game evidence.

Related coverage workbench: [Native Function Map](../native-function-map/README.md). Use it to see current reverse metadata, system-map coverage, native-owner report tags, and call relationships at a glance. It is a visualization aid, not API proof.

Related local mod demand index: [Local Mod Native Owner Review Library](../local-mods-native-owner/INDEX.md). Use it to see how current `testmods`, legacy local own-mod sources, and local third-party sample groups map back onto the same native-owner domains. It is demand and confidence evidence, not API proof.

Related ecosystem API research map: [SMAPI Ecosystem Semantic API Map](../smapi-ecosystem-map/INDEX.md). Use it to see clean-room ecosystem API concepts before narrowing a DTMAPI API rebuild to a Doloc native owner. It is architecture inspiration, not SMAPI compatibility or stability proof.

## Rules

- Start future API rebuilds from the relevant report here, then read the task-specific reverse maps and method bodies.
- Do not expose raw decompiled, Unity, Harmony, or BepInEx types in `DTMAPI.Abstractions`.
- Treat official Workshop docs as content/beauty support evidence only, not proof that DTMAPI can mutate runtime state safely.
- Mark missing native owners as blockers. Do not patch ordinary mods to imitate a stable API.
- Runtime work still needs GameBridge implementation, third-save validation, clean exit checks, and updates to the public API matrix.

## Source Index And Template

- [Source Index](SOURCE-INDEX.md)
- [Report Template](REPORT-TEMPLATE.md)
- [Three-Round Review Index](review-rounds/INDEX.md)

## Domain Reports

| ID | Domain | Primary verdict | Report |
| --- | --- | --- | --- |
| 01 | World time, weather, season, refresh lifecycle | Partial: query owners found; mutating date/weather remains debug/experimental | [World Time Weather Refresh](01-world-time-weather-refresh.md) |
| 02 | NPC body, behavior, animation, story, trade, location, new NPC | Partial: existing NPC owners found; complete new NPC remains blocked/proposed | [NPC Body Behavior](02-npc-body-behavior.md) |
| 03 | Livestock and animal behavior | Partial: animal lifecycle owners found; custom runtime animal creation remains blocked | [Animal Husbandry Behavior](03-animal-husbandry-behavior.md) |
| 04 | Wild birds, event drops, texture/new bird feasibility | Partial: bird event owner found; new bird behavior remains blocked/proposed | [Wild Birds Events Drops](04-wild-birds-events-drops.md) |
| 05 | Drones, components, slots, movement, multi-drone | Partial: active-drone and slot owners found; multi active drone blocked | [Drones Runtime Equipment](05-drones-runtime-equipment.md) |
| 06 | Flying motor and new vehicle types | Partial: singleton motor owner found; wholly new vehicle type blocked | [Flying Motor Vehicle Types](06-flying-motor-vehicle-types.md) |
| 07 | Maps, fixed scenes, dungeons, teleport, resources | Partial: room/dungeon/teleport/resource owners found; runtime map creation blocked | [Maps Dungeons Scenes Resources](07-maps-dungeons-scenes-resources.md) |
| 08 | Hats, accessories, equipment slots, extra equipment | Partial: native slots found; extra slot mutation remains experimental/high risk | [Hats Accessories Equipment Slots](08-hats-accessories-equipment-slots.md) |
| 09 | Food effects, equipment effects, buffs, reusable effects | Partial: existing effect owners found; brand-new behavior needs code owner | [Food Equipment Effects](09-food-equipment-effects.md) |
| 10 | Item stack and quantity limits | Found for native stack cap and inventory placement; mutation still needs transaction policy | [Item Stack Quantity Limits](10-item-stack-quantity-limits.md) |
| 11 | Original following pet | Blocked for stable native owner; DTMAPI-owned experimental runtime entity needed | [Original Follow Pet](11-original-follow-pet.md) |
| 12 | Held ranged weapons, projectiles, damage, attachments | Partial: projectile/damage owners found; handheld ranged weapon owner not found | [Held Ranged Weapons Projectiles](12-held-ranged-weapons-projectiles.md) |

## Review Rounds

| Round | Purpose | Report |
| --- | --- | --- |
| Round 1 | Parallel challenge review, overclaim questions, and initial confidence scoring. | [Round 1 Challenges](review-rounds/ROUND-1-challenges.md) |
| Round 2 | Parallel responses and supplemental native-owner findings. | [Round 2 Supplemental Report](review-rounds/ROUND-2-supplemental-report.md) |
| Round 3 | Final review, required downgrades, and confidence scores for the revised claims. | [Round 3 Final Confidence](review-rounds/ROUND-3-final-confidence.md) |

## Coverage Matrix

| User target | Covered reports |
| --- | --- |
| Time, weather, season, date jump, world refresh | 01, 07 |
| NPC body, behavior, animation, skin, encyclopedia, story, movement, trade, location, new NPC | 02 |
| Livestock feeding, defecation, breeding, products, buy/sell, movement | 03 |
| Birds: wild spawn/event behavior, scare/fly/drop, texture, new bird | 04 |
| Drones: new drone, texture, movement, components, slots, simultaneous drones | 05, 12 |
| Flying motor and wholly new vehicle types | 06 |
| New maps, teleport, boundaries, generation, fixed scenes, dungeons, resource refresh | 07, 01 |
| Accessories, hats, equipment slots, extra equipment | 08 |
| Food effects, equipment effects, unified multi-effects, new effects | 09 |
| Item stack and quantity limits | 10 |
| Original ground-only follower pet | 11 |
| Held items and ranged weapons with animation, projectile, damage, attachments | 12, 09 |

## Next-Use Pattern

When creating an implementation goal from this library:

1. Pick one domain report and one narrow API boundary.
2. Re-open the exact reverse maps and decompiled classes named by that report.
3. Decide whether the result is `stable open`, `experimental open`, `debug-only`, `registry-only`, `DTMAPI-internal`, or `blocked-rebuild`.
4. Create one `docs/goals/YYYY/...` handoff plus sibling `.goal.txt`.
5. Update `docs/api/public-api-matrix.md`, `docs/debug/regressions/smoke-matrix.md`, hook/debug records, and `docs/updates` only after implementation evidence exists.
