# 2026-06-03 Flow-Out Research: New Mods, Official JSON, and DTMAPI Gaps

This is a standalone research note outside the normal readme/goal/update/debug workflow. It records the user's new mod ideas, the official JSON routes that appear usable, and the DTMAPI APIs or hooks still needed.

## User Requirements

### Mod 1: Mine

- Reuse the water well model, but display it at 2x scale.
- Do not replace the original water well; add a new equipment named "矿井".
- Craft it at a second-tier crafting/equipment bench after research.
- Default operation consumes fuel, with large fuel capacity.
- Optional electric mode consumes 10 power.
- Electric mode should reduce fuel consumption; pure fuel mode should consume fuel faster.
- Produce minerals over in-game time periods.
- Support optional mod mineral compatibility:
  - compatibility is both item-pool compatibility and probability compatibility;
  - other mods should be able to set probabilities;
  - if no compatibility config exists, DTMAPI should provide local default probabilities;
  - check whether official content has rarity/probability concepts.
- Example base pool:
  - stone 10%
  - soil 10%
  - coal 10%
  - copper ore 30%
  - iron ore 25%
  - gold ore 10%
- If an oil mod is enabled and compatible, oil can be added with a medium-low default probability when the oil mod has no explicit config.

### Mod 2: Oil

- Add a new oil item.
- Mining coal has a small chance to drop oil.
- The new Mine can produce oil.
- Oil has only three core uses:
  - research the Mine;
  - craft the Mine;
  - high fuel value, higher than the current highest fuel item.
- Selling/buying/display item properties should be wired through normally.

### Mod 3: More Equipment Slots

- The base game currently has only one equipment/hat slot, e.g. miner hat and iron plate hat.
- Add more equipment slots from the game UI.
- The default slot keeps visible cosmetic appearance.
- Extra slots provide stats only and must not cause appearance conflicts.
- If the mod is disabled/uninstalled, all extra-slot equipment should be unequipped safely.

## Sources Checked

- Official Workshop docs:
  - `references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/046_01新增道具_OFS1w4gFSiDkHRkXX1Pcu4ihnNh.md`
  - `references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/048_03新增装饰设备_PSUjwr1GuiRixsk95HlcYTVQnPd.md`
  - `references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/047_08综合案例三（新增设备）_P57twsINsitUu6kWCDEc4vz6nNc.md`
  - `references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/049_04新增配方_VxeWw7ZDjiqFcTk0yLxcnB53n9b.md`
  - `references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/023_06综合案例一（新增道具）_KWqewbBLEiRw1Pkcn6lcZpQ7nXg.md`
  - `references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/032_04新增资源_DSwGwUzfki0gkPk1k6dcio6bnpb.md`
  - `references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/038_08综合案例三（新增资源）_U75Lwhq4qi4prxkti3zcffplnhf.md`
  - `references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/037_02新增鱼_O64fw47MJiC2qXk8A4fc4RsInyf.md`
- Reverse/config references:
  - `references/doloc-town/reverse/builds/23465763_workshop_38581E/content-configs/item_tbitem.json`
  - `references/doloc-town/reverse/builds/23465763_workshop_38581E/content-configs/equipment_tbequipment.json`
  - `references/doloc-town/reverse/builds/23465763_workshop_38581E/content-configs/recipe_tbrecipe.json`
  - `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/Config/Tables.cs`
  - `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/SimpleWell.cs`
  - `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/Synthesizer.cs`
  - `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/SynthesizerGenerator.cs`
  - `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/ElectronicComponentAppliance.cs`
  - `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/GameData/ElectronicComponentGeneratorFuel.cs`
  - `references/doloc-town/reverse/builds/23465763_workshop_38581E/decompiled/Assembly-CSharp/DolocTown/Config/Equipment/EquipmentFuncSynthesizerGenerator.cs`

## Overall Judgment

| Feature | Official JSON Only | DTMAPI Needed | Notes |
| --- | --- | --- | --- |
| New oil item | Yes | Optional | `item_tbitem.json` supports item title, icon, prices, sale, source, and `electric_energy`. |
| Oil as high fuel | Yes | No, unless changing fuel semantics | Current highest `electric_energy` found in base item table is pumpkin at 1200; oil can be set above that. |
| Oil small drop from coal mining | Partial | Likely for exact behavior | Official item spawn extension can add item to drop pools, but exact "coal resource only" should be verified. |
| New Mine equipment shell | Yes | Optional for shell | Official docs support new equipment, item, recipe, and recipe group extension. |
| Mine using water well model | Likely yes | Maybe for visual scale | A new equipment can reference the well sprite. No confirmed JSON scale field was found. |
| Mine displayed at 2x scale | Not confirmed | Likely | `cover_size` changes footprint, not necessarily visual scale. DTMAPI may need renderer scaling or a 2x sprite asset. |
| Mine passive fixed output | Possibly | Maybe | `EquipmentFuncSynthesizerGenerator` exists in code and outputs one fixed item at an interval, but no base content instance or official doc example was found. |
| Mine passive weighted random output pool | No | Yes | Official JSON does not show a weighted random machine generator. |
| Mine fuel + optional power mode | No | Yes | Appliance power and fuel generator are separate built-in concepts; a hybrid producer needs DTMAPI. |
| Mine mod mineral compatibility | No | Yes | Needs content index, mod source metadata, and probability registry/config. |
| New research node for Mine | Unclear/high risk | Likely | Tech tree tables exist, but UI uses `TechTreeGraph` assets; official docs reviewed do not explain adding tech tree nodes. |
| More equipment slots | No | Yes | Official JSON can add hats/equipment, but not add player equipment slots or alter equip UI behavior. |

## Official JSON Findings

### Items

Official `Content/item_tbitem.json` supports normal item definition:

- `id`
- `sub_type`
- `salable`
- `disposable`
- `consumable`
- `cookable`
- `electric_energy`
- `viewable`
- `source`
- `selling_price`
- `buying_price`
- `overlay`
- `ui_sprite_asset.url`
- localized title and description fields
- `function.$type`

This is sufficient for the Oil item as a normal fuel/salable material.

Base fuel comparison from `item_tbitem.json`:

| Item ID | Chinese title | Electric energy |
| --- | --- | --- |
| `pumpkin` | 南瓜 | 1200 |
| `turnip` | 萝卜 | 600 |
| `corn` | 玉米 | 400 |
| `coal` | 煤 | 300 |
| `thunder_grass` | 霹雳草 | 300 |

So "higher than the highest current fuel value" means oil should be higher than 1200 unless a future game build changes that table.

### Drop / Spawn Extension

Official `mod_tbmoditemspawnextension.json` can add items to existing drop pools with fields like:

- target drop pool id;
- extra item entries;
- spawn weight;
- min/max count;
- item name.

This should be tested for Oil by adding it to a coal-related drop pool if one exists. If the only available pool is broader than "mining coal", DTMAPI needs a hook for exact coal-resource mining behavior.

### Equipment

Official equipment creation uses at least:

- `item_tbitem.json` for the placeable equipment item;
- `equipment_tbequipment.json` for the equipment prototype;
- optional `recipe_tbrecipe.json`;
- optional `mod_tbmodrecipegroupextension.json`;
- optional `mod_tbmodstoreextension.json`.

Known equipment examples from base config:

- `well`
  - function: `EquipmentFuncSimpleWell`
  - capacity: `100`
  - adder: `10`
  - sprite asset: `sprite_equipment_well`
  - cover size: `4x3`
- `equipment_workbench`
  - function: `EquipmentFuncEquipmentWorkbench`
  - recipe group: `equipment_workbench`
- `manual_workbench`
  - function: `EquipmentFuncWorkbench`
  - recipe group: `manual_workbench`
- `large_smelter`
  - function: `EquipmentFuncSynthesizer`
  - recipe group: `large_smelter`
  - electronic component: `EComProtoAppliance`
  - threshold: `2`
- `coal_generator`
  - function: `EquipmentFuncPowerGeneratorFuel`
  - electronic component: `EComProtoGeneratorFuel`
  - fuel capacity: `1800`
- `modified_fuel_generator`
  - function: `EquipmentFuncPowerGeneratorFuel`
  - fuel capacity: `4800`

### Recipes

Official `recipe_tbrecipe.json` and `mod_tbmodrecipegroupextension.json` support adding a recipe to a recipe group. The official example uses `equipment_workbench` to add an equipment recipe.

For "二级合成台", the implementation Codex must verify the intended group. Current base config confirms both `equipment_workbench` and `manual_workbench` exist. Do not assume the Chinese phrase maps to one without checking the target in game or docs.

### Rarity / Probability

Official fish docs include a `rarity` field from 0 to 4. This confirms rarity exists for fish, but it does not prove a universal rarity system for ores/minerals.

Drop extension supports probability/weight-like behavior through item spawn tables. For the Mine's mineral output, a DTMAPI weighted pool is still the safer design because the desired output is machine-specific and must include other mods' minerals.

### Research / Tech Tree

Reverse data confirms:

- `techtree_tbtechtree`
- `techtree_tbtechnode`
- `techtree_tbtechpoint`
- runtime `TechTreeGraph`
- runtime `TechTreeDatabase`
- UI `TechTreeUiState`

The risk is that the visible research UI appears to depend on graph assets, not only JSON table rows. The reviewed official Workshop docs mention tech points from resources/recipes/fish, but do not clearly document adding a new tech tree node.

Therefore "research Mine" is not yet proven as a pure official JSON feature. It may require DTMAPI to inject a node, unlock recipe conditions, or provide a lighter custom research gate.

## Mod 1 Analysis: Mine

### What Official JSON Can Probably Do

Minimum official content shell:

1. Add an item such as `dtmapi_mine`.
2. Add equipment prototype such as `dtmapi_mine_equipment`.
3. Reuse `sprite_equipment_well` or provide a copied/derived local sprite reference only if allowed by content pipeline.
4. Add a recipe with `recipe_tbrecipe.json`.
5. Add that recipe to a workbench group with `mod_tbmodrecipegroupextension.json`.
6. Optionally add store availability with `mod_tbmodstoreextension.json`.

If a fixed-output test is acceptable, `EquipmentFuncSynthesizerGenerator` is a possible low-risk prototype because it has:

- `output_item_name`
- `interval`
- runtime behavior that creates drop items repeatedly after the interval.

But no official documentation/example for this function was found in the Workshop docs checked, and base config did not appear to contain an active `EquipmentFuncSynthesizerGenerator` entry. It should be treated as "engine-supported but not officially documented" until tested.

### What DTMAPI Needs To Provide

The full requested Mine needs DTMAPI:

- visual 2x scaling if no official scale field exists;
- a custom machine controller or hook bound to the Mine equipment id;
- fuel storage and fuel consumption for non-generator equipment;
- optional electric mode with threshold/consumption of 10;
- mode switching and config menu;
- time-period production using game time hooks;
- weighted mineral output pools;
- mod mineral discovery from official local MODS, Workshop, and DTMAPI packages;
- probability merging:
  - base defaults;
  - per-mod contribution;
  - local fallback for mods with no config;
  - optional disable for mod mineral compatibility;
- output delivery rules:
  - drop on ground,
  - internal buffer,
  - or send to container/backpack, depending on chosen design;
- clear logs for each production cycle and probability table.

### Suggested DTMAPI API Surface

Experimental API candidates:

- `IMachineApi`
  - register custom equipment behavior by equipment id;
  - listen to equipment created/loaded/destroyed;
  - attach a per-equipment state object;
  - tick by game time period or TU.
- `IWeightedOutputPoolApi`
  - register pool entries by item id, source mod, weight/probability, tags, rarity, and enabled state;
  - normalize probabilities;
  - expose final resolved pool for debug UI.
- `IFuelPowerHybridApi`
  - inspect/add/remove fuel energy;
  - consume electric power from an equipment component or local buffer;
  - report mode and consumption rate.
- `IOfficialContentIndexApi`
  - source-aware item metadata;
  - mod mineral tags;
  - Workshop/local source id.

### Recommended Implementation Order

1. Build Oil first as a normal official content item.
2. Build Mine official shell: item, equipment, recipe, icon/sprite, placement.
3. Test whether a new equipment can reuse `sprite_equipment_well` without replacing the original well.
4. Test whether `cover_size` only changes placement or also affects visual layout.
5. Prototype a fixed-output Mine using `EquipmentFuncSynthesizerGenerator` only as a research spike.
6. If fixed output works, still implement DTMAPI controller for weighted output and hybrid fuel/power.
7. Only after stable output, add compatibility with mod minerals and probability config.

## Mod 2 Analysis: Oil

### What Official JSON Can Do

Oil is the cleanest official-content mod:

- define item in `item_tbitem.json`;
- give it a material subtype, likely under ore/material category after checking existing item taxonomy;
- set `salable`, `selling_price`, `buying_price`;
- set `electric_energy` above 1200;
- provide icon and localized name/description;
- add source info.

Oil can also be an input item in the Mine recipe through `recipe_tbrecipe.json`.

### Drop Route

Use official `mod_tbmoditemspawnextension.json` first:

- find the coal resource/drop pool id in base tables;
- add Oil with a small weight/count;
- verify in game that mining coal can drop oil.

If the extension targets a broad drop pool and oil appears from unrelated sources, switch to a DTMAPI hook:

- hook resource broken / tool hit success path;
- confirm target resource id is coal;
- roll a small probability;
- create oil drop through native drop item APIs.

### Research Route

"Research Mine using oil" is not confirmed as official JSON. Recipe unlock via tech tree may need DTMAPI if official Workshop docs do not expose tech tree node creation. A safe staged design:

1. First release: recipe available by default or gated by existing official recipe unlock.
2. Later release: DTMAPI research node or custom unlock condition once tech tree injection is proven.

## Mod 3 Analysis: More Equipment Slots

### Official JSON Boundary

Official JSON can add hats/equipment items, but no reviewed official doc or config table indicates a way to add more player equipment slots through content JSON.

This mod should be treated as a DTMAPI gameplay/UI extension, not an official content-only mod.

### DTMAPI Work Needed

Likely areas:

- inspect and hook player equipment inventory data;
- inspect and hook the equipment UI/menu widget;
- add extra slot state per save;
- save/load extra slot state in a DTMAPI-owned save extension;
- apply stat effects from extra slots;
- ensure only the primary/default slot drives visible appearance;
- prevent duplicate equipment if the same item cannot be equipped twice;
- handle item validity when a source mod is disabled;
- on mod disable/uninstall:
  - move extra-slot items back to backpack if possible;
  - otherwise send by mail/recovery container/drop-safe path;
  - never delete silently.

### UI Direction

The player-facing behavior should be conservative:

- original slot remains visually equivalent to the base game;
- added slots are displayed in the same equipment panel if possible;
- extra slots clearly indicate "stats only" or use icon treatment to avoid cosmetic confusion;
- no forced visual stacking unless a later API explicitly supports cosmetic priority.

### API Direction

Experimental API candidates:

- `IEquipmentSlotApi`
  - enumerate slots;
  - add/remove mod-owned slots;
  - equip/unequip through native inventory paths;
  - get disabled-mod recovery actions.
- `IPlayerStatModifierApi`
  - apply modifiers from equipped items;
  - remove modifiers deterministically on unequip/load/uninstall.
- `IEquipmentAppearanceApi`
  - declare visible slot priority;
  - ensure extra slots are stats-only by default.

## Compatibility Strategy

### For Official/Workshop Content

Default rule: read-only indexing. Do not rewrite other mods' local metadata unless the user explicitly asks and a backup/restore path exists.

For Mine mineral compatibility:

- read loaded runtime item table first;
- use DTMAPI source metadata to identify mod items;
- classify likely minerals by subtype/tags/source/path/name;
- allow explicit compatibility JSON from the mod or from local DTMAPI config;
- if no explicit probability exists, use a DTMAPI fallback default;
- show final probability table in debug/config UI.

### Suggested Compatibility JSON

A DTMAPI-side optional file could look conceptually like:

```json
{
  "mineOutputs": [
    {
      "itemId": "example_oil",
      "weight": 6,
      "minCount": 1,
      "maxCount": 1,
      "tags": ["mineral", "fuel"],
      "enabledByDefault": true
    }
  ]
}
```

This should live in a DTMAPI compatibility layer, not by mutating Workshop mods.

## Open Questions For Next Codex

1. What is the exact recipe group for the user's "二级合成台" target?
2. Does official Workshop content accept `EquipmentFuncSynthesizerGenerator` in `equipment_tbequipment.json` even though it is not documented?
3. Can a new equipment safely reuse `sprite_equipment_well` without affecting the original water well?
4. Is there any official JSON field for visual scale, or must DTMAPI scale the renderer / provide a 2x sprite?
5. What exact drop pool/resource id corresponds to coal mining?
6. Does `mod_tbmoditemspawnextension.json` add oil only to coal mining, or to a broader coal-related drop context?
7. Is there an official way to add tech tree nodes, or does visible research require DTMAPI graph injection?
8. Which base item category should Oil use so Y console, stores, and item UI classify it naturally?
9. Which existing equipment UI/data classes own the single hat/equipment slot?
10. What is the safest extra-slot recovery path if the mod is disabled and backpack is full?

## Recommended Next Goal Shape

This research should become a future readme/goal only after the current running Codex finishes. Suggested split:

### Goal K: Oil + Mine Official Content Shell

- Implement Oil as an official-style content mod.
- Add Oil to coal mining through official drop extension if exact enough.
- Add Mine item/equipment/recipe using official JSON.
- Verify placement, recipe, sale, fuel value, and base UI classification.
- Research spike: test `EquipmentFuncSynthesizerGenerator` for fixed interval output.

### Goal L: DTMAPI Mine Machine API

- Add experimental machine API.
- Attach behavior to Mine equipment id.
- Implement hybrid fuel/power mode, weighted output, game-time production, and compatibility pool.
- Add debug/config UI for resolved output probabilities.

### Goal M: More Equipment Slots

- Add experimental equipment slot/stat API.
- Add extra equipment slot mod.
- Preserve one visible appearance slot.
- Save/load/recover extra-slot items safely.

## Final Assessment

- Oil is mostly official JSON and should be the first test target.
- Mine can have an official JSON shell, but the requested full behavior is DTMAPI-heavy.
- More equipment slots is almost entirely DTMAPI UI/state/stat work.
- The risky parts are research UI injection, exact coal-drop targeting, visual 2x scaling, and safe uninstall recovery.
