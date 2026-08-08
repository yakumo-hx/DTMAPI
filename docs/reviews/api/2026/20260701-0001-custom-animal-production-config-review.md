# Custom Animal Production Config Review - 2026-07-01

## Scope

User asked whether the current Hatch/Chicken custom-animal route can be reused by authors for new animals, and specifically whether production, breeding, room capacity, feeding, multi-output production, hidden products, native equipment capacity, and mixed husbandry can be controlled by content JSON.

This review records the code-level findings used to update the standalone author guide. It is not a public API promotion. The stable DTMAPI-owned custom-animal files remain:

- `Content/DTMAPI/custom-animals.json`
- `Content/DTMAPI/audio-replacements.json`

The official-style files under `Content/*.json`, such as `animal_tbanimal.json` and `item_tbitemspawn.json`, are consumed through the game's official mod/config reload path. DTMAPI currently relies on that native content pipeline for those tables.

## Sources Read

- `author-docs/content-packs/custom-animal-json-png-wav.md`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/OilCoalDropSmokeCase.cs`
- Reverse build references under `references/doloc-town/reverse/builds`, covering `Animal`, `AnimalAI`, animal config classes, item-spawn config classes, husbandry config classes, and the four native livestock production equipment classes.
- Local extracted official config bundles for animal rows, animal hidden husbandry rows, item spawn rows, global time parameters, and equipment capacity rows.
- Oilfloater content package at `E:/DolocTownUnity/DolocTownMeta/prototypes/oilfloater/DTMAPI_OilfloaterAssets/Content`.

No official decompiled source or game binary content is copied into this report.

## Native Content Boundary

DTMAPI scans enabled `ContentPack` mods for its own bridge schemas:

- `Content/DTMAPI/custom-animals.json` registers custom species animator routing, PNG sprite override routing, and AI template mapping.
- `Content/DTMAPI/audio-replacements.json` registers reviewed short-SFX replacements such as `AnimalVoice`.

The official-style content tables are separate. Runtime smoke helpers reload them through:

- `DolocAPI.modManager.ReloadMods()`
- `DolocTown.Config.DolocConfig.Reload()`

That means authors can use official-like JSON rows for new animal data, item data, spawn data, and shop data, but DTMAPI does not yet wrap every one of those native fields as a stable DTMAPI public API.

## Author-Controlled Animal Parameters

The following values are content-author controlled when the official-style row is loaded into `animal_tbanimal.json`:

| Area | Fields | Code-level behavior |
| --- | --- | --- |
| Child growth | `grow_interval`, `grow_increase`, `grow_cost` | Child animals advance growth every `grow_interval` TU if energy can be spent. At 100 progress they become pre-adult, then adult on the next day-change. |
| Adult fertility | `fertility_interval`, `fertility_increase`, `fertility_cost` | Adult fertility progress advances to 100 over time. Breeding also requires energy, at least one same-proto adult partner in the room, and enough home-room capacity for the child. |
| Breed device duration | `breed_duration` | The nursery/breeding equipment uses this as its occupied duration. |
| Breed energy cost | `breed_energy_cost` | The parent spends this when breeding starts. |
| Production cycle | `metabolism_interval`, `metabolism_increase`, `metabolism_cost` | Adult metabolism advances to 100 over time. Production also requires mood and a suitable production device. |
| Base products | `produce_spawn_entry.spawn_lut`, `produce_spawn_entry.count_range`, `item_tbitemspawn.json` | Production resolves the spawn LUT into one or more item units. |
| Room capacity | `space` | Barn/room capacity and breeding capacity checks use this value. |
| Ground body size | `size` | Affects the native animal's ground/path footprint. It is separate from PNG canvas and stage `sprite_size`. |
| Interaction body size | `levels[*].sprite_size` | Native animal info derives renderer collider size and emotion offset from this field. DTMAPI's PNG bridge does not use it for frame selection. |
| Feeding cadence and amount | `eat_interval`, `eat_count` | Hungry animals periodically search for food and take feed energy from the selected feeder path. |
| Production mood requirement | `produce_require_mood` | Ordinary production is blocked if mood is below this threshold. |
| Production reset style | `manual_metabolism` | `true` resets metabolism to 0 after production; `false` subtracts 100 and preserves overflow. |

Global time parameters observed in the current build put one in-game day at roughly 288 TU. The author guide now describes timing as estimates because energy, mood, sleep, and device availability also gate behavior.

## Product Spawn Semantics

Native production returns a list of item units, not a single opaque production record.

`produce_spawn_entry.count_range` chooses how many item units the base product roll should produce. `item_tbitemspawn.json` then resolves weighted rows with min/max counts. Official rows can use `0/0` to represent an unlimited weighted row, but that is easy for new authors to misread. The author guide now recommends a simpler first-pass pattern:

```text
count_range: 1..1
spawn_datas: one row, min_count=1, max_count=1
```

Multiple products are supported at the native data level. Authors can use a larger `count_range`, weighted rows, or row minimums to produce more than one item unit. They must account for the production equipment capacity, because equipment stores item units.

## Hidden Product Semantics

Hidden products are native-supported through two table families and maintain progress separately from the ordinary `produce_spawn_entry` product roll:

- `animal_tbhusbandry.json` defines animal-specific hidden outputs, progress thresholds, output ranges, and optional limited contribution sources.
- `animal_tbhusbandryenergy.json` defines feed item energy and per-food contribution values.

Therefore the hidden-product progress bar is content-configurable: the threshold defines the bar length/full point, contribution values define how much each accepted food increments the bar, and limited contribution sources define which foods can increment that hidden progress.

At production time, the animal first generates its base `produce_spawn_entry` items, then appends special husbandry outputs whose progress thresholds have been reached.

Author/user clarification on 2026-07-01 aligns with the reviewed code path: the default feeder contributes feed energy only and does not grow hidden-product progress. Hidden-product progress grows through feed paths that pass a concrete food item ID to the animal, such as crop eating, pasture/plant-basin feeds, and wild or room grass/resource eating. Therefore hidden products are a real content-author configurable native feature, but should be documented as an advanced, separately tested workflow rather than a first-animal requirement.

## Native Equipment Capacities

The reviewed official production equipment capacities are:

| Template route | Native production equipment | Capacity |
| --- | --- | --- |
| `chicken` | chicken nest | 10 item units |
| `slime` | honey comb | 10 item units |
| `goat` | lint roller | 20 item units |
| `marsh_pangolin` | milking machine | 20 item units |

Capacity is item-unit capacity. If one production action tries to add many units, some equipment paths may fill and stop accepting additional units. Chicken nests select only non-full nests before production but the add path itself is less defensive, so high-count production should be treated carefully.

Milking machines have an additional availability/charge condition before animals can use them.

## Mixed Husbandry Behavior

Native equipment lookup is by equipment type and reachability, not by species-specific ownership. If two custom species share the same template route, they share the same production equipment pool:

- two `slime`-template species both use honey combs;
- two `goat`-template species both use lint rollers;
- two `marsh_pangolin`-template species both use milking machines;
- two `chicken`-template species both use chicken nests.

The equipment stores item IDs, not animal source metadata. Mixed custom animals can therefore place different item IDs into the same device. One device can contain multiple product item IDs at the same time; this is not visual or audio pollution, but it is a player-facing mixing/capacity concern.

Breeding is more isolated: the reviewed `NeedBreed` path requires another adult with the same `protoName`, and the child spawned by breeding uses the parent's proto. Cross-species breeding was not observed in this route.

## Oilfloater Content Update

The Oilfloater prototype package now uses a simple daily coal production target:

- `E:/DolocTownUnity/DolocTownMeta/prototypes/oilfloater/DTMAPI_OilfloaterAssets/Content/item_tbitemspawn.json`
- spawn LUT `oilfloater_produce`
- item `coal`
- `min_count=1`
- `max_count=1`

The package's `animal_tbanimal.json` already points `produce_spawn_entry.spawn_lut` at `oilfloater_produce` and uses `count_range 1..1`. Its metabolism values (`metabolism_interval=3`, `metabolism_increase=1.04167`, `manual_metabolism=false`) are expected to reach roughly one production per in-game day once the adult animal has energy, mood, and equipment access.

The unused prototype custom item `oilfloater_meat` was removed from the Oilfloater package's `item_tbitem.json`; the animal bag remains `sack_oilfloater`.

## Author Guidance Outcome

The author guide should present three tiers:

1. Minimum reliable production: one ordinary product through `produce_spawn_entry` and a single `item_tbitemspawn` row.
2. Advanced production count/randomness: larger `count_range`, weighted rows, and capacity-aware multi-output behavior.
3. Hidden husbandry products: `animal_tbhusbandry` and `animal_tbhusbandryenergy`, with an explicit rule that default feeder storage does not grow hidden progress, while crop/pasture/wild-grass-style paths can because they pass item identity.

It should also warn that AI template choice selects the native production equipment family. JSON alone cannot define a new fifth equipment type or a species-only filter for shared equipment.

## Validation

Completed:

- Static code review of DTMAPI content-pack scanners and native reload helper path.
- Static reverse-code review of animal growth, fertility, production, hidden product, equipment capacity, and equipment lookup behavior.
- Static review of current Oilfloater content JSON before and after the coal product change.

Not completed:

- No build/test run for this review.
- No runtime smoke or slot-7 game validation for Oilfloater coal production in this pass.
- Hidden product behavior was not runtime-tested with a custom content pack; it remains an advanced documented route requiring separate manual verification.
