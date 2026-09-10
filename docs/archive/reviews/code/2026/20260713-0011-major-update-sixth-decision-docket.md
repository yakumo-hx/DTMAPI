# 20260713-0011 AnimalPack Deferred Product Design Draft

Status: recorded / reclassified / decisions deferred to AnimalPack 1.0.0 rebuild
Date: 2026-07-13
Scope: non-binding four-species AnimalPack product roles, hidden-product purposes, Oil/Mine integration, and direct-sale versus processing design inputs
Original classification: proposed mainline sixth decision round
Original Update: `docs/updates/2026/20260713-0005-fifth-round-closure-sixth-decision-docket.md`
Reclassification Update: `docs/updates/2026/20260713-0006-sixth-round-scope-reclassification.md`
Mainline successor: `docs/reviews/code/2026/20260713-0012-major-update-sixth-decision-docket.md`
Follows for product history: `docs/reviews/code/2026/20260713-0010-major-update-fifth-decision-docket.md`
Focused review: `docs/reviews/code/2026/20260713-0009-first-party-animal-pack-product-boundary-review.md`

## Source Request

The user supplied `D:/下载/第五轮.md`, closed O/P/Q/R/S/T, and identified the remaining principal product decisions as the four animals' primary pools, hidden products, processing quantities and recipe purposes. The user then asked to continue a sixth round.

The user subsequently supplied four screenshots and corrected the scope against the full audit and first-through-fifth-round problem scale. Animal species roles, exact outputs, equipment naming/unlock, probability, quantity, price and Oil integration belong to the later AnimalPack rebuild, not the DTMAPI mainline sixth round. This record is therefore retained as an auditable deferred product-design draft; no U/V/W/X/Y answer is currently requested. The true mainline sixth round continues in `docs/reviews/code/2026/20260713-0012-major-update-sixth-decision-docket.md`.

The original draft attempted to decide product identity and use before numeric balance. Those questions remain useful, but they now reopen only inside the AnimalPack 1.0.0 product Review/Update.

This is docs/source/native-config analysis. It does not add items/recipes, freeze public names or ids, copy/replace assets, change animal JSON, launch the game, or modify packages/save/Workshop state.

## Closed Fifth-Round Inputs, Not Sixth-Round Choices

- the unified product contains Hatch, Mole, Drecko and Oilfloater only;
- Shell Crab remains unpublished AdvancedAssetBundle prototype; Lightning Chicken is NeverPublish retired research;
- all ordinary/hidden animal outputs are custom products;
- official JSON owns items, production/LUTs, recipes, stores, technology and text wherever supported;
- processed results re-enter the native economy; GameBridge owns no species probability/yield/recipe table;
- stable return targets roughly 2-3 times a vanilla comparison and long-run hidden-product return roughly 3-5 times, subject to a full occupied-space/payback model;
- asset provenance, product/content ids and package migration remain release gates;
- current decisions do not block Batch 0 or either P0 implementation.

## Brief Technical Resolution From The Sixth-Round Scope Correction

### 1. Official content has two distinct processing routes

Ordinary `recipe_tbrecipe.json` remains a multiple-input-to-one-`output_item` route. That does not describe every native processing machine.

The current public build separately contains:

```text
EquipmentFuncGarbageShredder
-> recipe_tbdismantlerecipegroup
-> recipe_tbdismantlerecipe
-> output_item_spawn_entry
-> item_tbitemspawn
```

`DismantleRecipeInfo` owns an `output_item_spawn_entry` rather than one `output_item`. `GarbageShredder.SpawnItems` passes its fixed/ranged draw count to the referenced item-spawn LUT. The native LUT first allocates configured minimum counts, then makes remaining weighted draws, so one processing job can return several different item types. Existing official shredder pools already contain multiple candidate items.

Analysis immediately following issue 1: this corrects the earlier inference that ordinary recipe shape ruled out official multi-result processing. A fixed `egg + meat + eggshell` result is source-plausible through three bounded LUT rows and a matching fixed draw count, but it is not a completed Mod capability until exact `min_count/max_count`, `count_range`, capacity and runtime behavior pass focused tests.

### 2. An AnimalPack-owned processing machine is a reasonable candidate

The later product may define a distinct animal-product processor by reusing the native shredder function shape, with its own equipment/item, dismantle recipe group, per-product dismantle recipes and per-recipe item-spawn LUTs.

Analysis immediately following issue 2: the current public-build loader discovers same-named Mod JSON tables dynamically, appends new ids, resolves the four equipment/dismantle/group/spawn tables, and maps `EquipmentFuncGarbageShredder` to the native `GarbageShredder`; source therefore proves that a complete official-content injection path exists in this build. The official author documentation does not promise these dismantle tables as a stable public surface, and no real content-package run has yet proved the cross-table references, crafting/placement, UI, power, batch, save/reload, disable/update or art/animation paths. Those gates belong to one focused AnimalPack native-JSON smoke.

Equipment policy, recipes, output probabilities/counts, UI, batch size, power, unlock and economy remain owned by AnimalPack. This direction does not justify modifying the official garbage shredder, adding Hatch/Mole-specific Hooks, storing yield tables in GameBridge, or creating a public machine API.

Hidden husbandry output, special-feed progression, ordinary multi-candidate animal LUT/count interaction, equipment capacity/mixed species and real technology/store ids remain later product validation gates.

## Deferred Hatch Product Role - historical U label

The user has already proposed the working identities `hatch_egg`, `mutated_hatch_egg` and `giant_hatch_egg`. Exact public names/ids remain provisional until the content/provenance freeze.

| Option | Product role | Trade-off |
| --- | --- | --- |
| U1 - food ladder | Normal egg is the reliable food/native-egg route; mutated egg unlocks stronger cooking/processing recipes; giant egg is the rare high-value bulk product. Meat and eggshell functions remain part of the processing/direct-recipe design. | Clear early/mid/rare progression and uses the game's existing egg/meat food economy; multi-result mechanics still need proof. |
| U2 - breeding ladder | Normal egg is food; mutated/giant eggs primarily create animal bags, incubation or breeding advantages. | Strong husbandry identity, but official JSON-only incubation/animal creation is not proven and would widen 1.0.0. |
| U3 - research/industry ladder | Normal egg is food while mutated/giant eggs primarily feed technology or industrial recipes. | Distinctive, but makes the first animal depend on unproved technology/equipment content. |

Historical working direction: **U1**. No current decision is requested; reconsider it during the AnimalPack rebuild.

## Deferred Mole Product Role - historical V label

Current native facts: `soil` is a low-value foundation material used widely by planter/equipment recipes; `fertilizer` exists as a farm product. The prototype currently returns meat plus soil, but that is not the selected custom-product architecture.

| Option | Product role | Trade-off |
| --- | --- | --- |
| V1 - soil and agriculture specialist | Primary pool is custom soil/root clods processed into native soil or used directly by planter/agriculture recipes; hidden enriched-earth core enters fertilizer/advanced soil uses. | Gives the pack a clear agriculture lane with many native sinks and little overlap with Drecko/Oilfloater. |
| V2 - soil plus underground prospecting | Stable product remains soil-oriented; hidden mineralized nodule processes into one reviewed native mineral. | Adds excitement, but overlaps Mine and requires a deliberate mineral/economy choice. |
| V3 - compost/feed closed loop | Products primarily become fertilizer/feed inputs so livestock sustains farming and more livestock. | Coherent simulation loop, but ordinary output-by-feed semantics are not proved and may create a self-amplifying economy. |

Historical working direction: **V1**, with V2's mineralized nodule only as a later hidden-product candidate. No name or direction is frozen.

## Deferred Drecko Product Role - historical W label

Current native facts: `wool` processes to `woolen_cloth`; both have equipment/decor sinks, while meat has established cooking sinks. The prototype returns wool plus meat.

| Option | Product role | Trade-off |
| --- | --- | --- |
| W1 - textile-led, meat as secondary yield | Primary custom fleece/fiber product is processed toward wool and, if multi-result processing is proved, a controlled meat by-product; the hidden glossy product feeds high-grade textile/equipment recipes. | Preserves the requested textile/meat destination while keeping a clear textile identity. |
| W2 - two independent primary candidates | Ordinary LUT separately yields a custom textile bundle or custom meat product; each has a simple single-result processing route; hidden product remains premium textile. | Fits the currently proved single-output recipe shape, but adds item/LUT/capacity variance and makes daily output less legible. |
| W3 - industrial insulation specialist | Products are mostly non-food equipment/defence/insulation materials. | Stronger industrial niche, but actual equipment recipes/material owners are not yet proved and meat disappears from the selected destination. |

Historical working direction: **W1 as product semantics**. No current decision is requested.

## Deferred Oilfloater And Oil/Mine Relationship - historical X label

Current native facts: coal is a low-unit-value but widely consumed fuel/industry input. The separate Oil prototype owns the intended official-JSON `crude_oil` item and coal-mine LUT extension; `crude_oil` is not a neutral Runtime item today. Making Oilfloater produce it creates a product dependency/identity decision.

| Option | Product role | Trade-off |
| --- | --- | --- |
| X1 - self-contained fuel animal | Stable and hidden custom fuel products process to native coal/other reviewed native fuel outputs; the custom products may also be direct Mine/industry recipe inputs. AnimalPack does not require the Oil product. | Clean disable/update ownership and independent 1.0.0; weaker Oil ecosystem connection. |
| X2 - coal stable, crude-oil hidden integration | Stable product processes to coal; hidden oil sac processes to the Oil product's `crude_oil`. AnimalPack declares an explicit Oil dependency or the integration lives in an Oil-owned extension. | Strong thematic ecosystem and avoids putting coal/oil in one ordinary animal pool, but dependency/absence/load-order/identity behavior must be designed. |
| X3 - move crude-oil identity to a shared industrial content owner | AnimalPack and Oil both depend on a new shared static-content package. | Clean theoretical ownership for a shared item, but creates another required product solely to share one prototype identity. |

Historical working direction: **X1 for an independent first rebuild**, with any later Oil integration owned by Oil. No current decision is requested. Under no option does GameBridge own `crude_oil` or Oilfloater yield tables.

## Deferred Direct Sale Versus Processing Rule - historical Y label

This is a cross-species rule. Selling consumes an item, so the real balance risk is a zero-cost processing route that dominates direct sale or a custom product whose direct price already includes value later recovered again through a non-consuming path.

| Option | Rule | Trade-off |
| --- | --- | --- |
| Y1 - processing-only | Primary/hidden custom products are not directly salable or have only nominal salvage value; almost all value requires processing/direct recipes. | Strong industry loop, but punishes early players without the right station/recipe. |
| Y2 - safe sale floor plus processing premium | Every ordinary product has a modest direct-sale exit; processing/direct recipes give higher net value after time/energy/equipment. Hidden items may use the same rule or be a documented jackpot sale, species by species. | Player-friendly fallback with meaningful industry; requires one consistent net-value model. |
| Y3 - no pack-wide rule | Each item independently chooses sale-only, processing-only or dual-use. | Maximum flavor, weakest predictability and highest economy-test burden. |

Historical working direction: **Y2**. Nothing is frozen now; economy work later decides the rule and values together.

## Historical Working Recommendation - no current answer requested

```text
U = U1  Hatch food ladder: normal, mutated and giant eggs have distinct tiers
V = V1  Mole owns soil/agriculture; mineralized hidden output remains optional
W = W1  Drecko is textile-led with controlled meat secondary value
X = X1  Oilfloater 1.0.0 is self-contained; Oil integration is later and Oil-owned
Y = Y2  direct-sale safety floor plus a tested processing premium
```

This draft would produce four clear lanes, but the lanes are not part of the mainline sixth-round decision set:

```text
Hatch       food
Mole        soil/agriculture
Drecko      textile/equipment
Oilfloater  fuel/industry
```

## Decisions Deliberately Deferred

- exact public product/item names, English ids, icons and descriptions;
- probabilities, quantities, sale prices, energy/time, bag cost and payback;
- whether a proved multi-result device/recipe exists and the selected fallback if it does not;
- exact hidden-feed trigger and progression values;
- specific mineral, fertilizer, textile and industrial recipe ids until native/schema verification;
- Manbo's post-Canary keep/retire decision;
- MoreEquipment and ActionSpeed later product-scope decisions;
- BGM, pets, vehicles and multiplayer.

These animal decisions do not delay Batch 0, the player-uninstaller P0 or the Oil/OneAction P0. After fifth-round facts are recorded, those streams can start under independent Updates while the AnimalPack remains a later product project.

## Validation Boundary

This docket cross-checked the fifth-round feedback, current four animal prototypes and selected content review, the current official public-build animal/item/spawn/recipe configs, existing custom-animal production reviews, the Oil/coal native-drop review, and the full boundary audit. It did not perform a build, runtime smoke, official-content mutation, external-asset copy, package publication or save/Workshop change.
