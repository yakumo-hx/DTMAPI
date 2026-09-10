# 20260717-0006 Official JSON Monster Capture And Production Native-Owner Review

Status: recorded / read-only feasibility review / implementation not started
Date: 2026-07-17
Official-document baseline: `references/doloc-town/official-workshop-docs/feishu-crawl-20260715`
Reverse baseline: `references/doloc-town/reverse/builds/23762374_public_C416D4`
Related public matrix: `docs/api/public-api-matrix.md`
Related native-owner domain: `docs/reviews/api/native-owner-domains/12-held-ranged-weapons-projectiles.md`

## Source Request

Review whether an official-JSON-only mod can define a Pokeball-like tool, use it to capture a live wild monster by converting that monster into a species-specific item, and then put that item into a fish-tank-like device to generate products or perform operations. If official JSON cannot implement the exact loop, identify the smallest DLL bridge that should own the missing behavior.

Follow-up clarification: the production device is not intended to be an ordinary fish tank. It may reuse the native farm-fish scheduling/data mechanism, but it must be a separately registered equipment id that accepts only configured monster capsules and rejects ordinary fish. The capture ball should own a configurable success roll; a successful roll deterministically produces the mapped capsule, while a failed roll consumes the ball without damaging, alerting, or otherwise changing the monster.

Second follow-up correction: this project accepts community-discovered JSON fields that are present and verified in the current game even when the official Workshop documentation does not publish them. Therefore, `EquipmentFuncFishTank` / `EquipmentFuncFishTankEcological` being undocumented does not by itself justify a runtime version-check DLL. They are recorded as community-verified internal JSON formats for the reviewed build and require regression testing after game updates, not code merely to authorize their deserialization.

This is a read-only official-content and native-owner review. It does not patch DTMAPI or the game, create a mod package, launch the game, or promote a public API.

## Verdict

The exact requested loop is **not possible through the currently documented official JSON contract alone**.

Official JSON can implement the two ends of the loop, but not the live-capture transition in the middle:

| Requested part | Official JSON verdict | Exact boundary |
| --- | --- | --- |
| Define an item named and drawn as a capture ball | Supported as data | `item_tbitem.json` can define the item, but documented `ItemFunction` has no use effect. Setting `sub_type` to `tool` only classifies the item; it does not create a new tool runtime class or hit behavior. |
| Make a monster yield a custom specimen/capsule item when defeated | Supported | `mod_tbmoditemspawnextension.json` can append custom items to documented monster drop pools. The official drop-table document explicitly says these pools trigger when the monster is defeated. |
| Use the ball on a living monster, suppress death loot/experience, remove that monster, and choose an output by monster species | Not supported | No documented item function, conditional drop rule, live-monster interaction, target-selection rule, or monster-removal action expresses this transaction. |
| Process the captured item in an existing recipe machine | Supported | A normal captured item can be an input to `recipe_tbrecipe.json`, then the recipe can be appended to a documented existing recipe group. |
| Put the captured item in an ordinary existing fish tank | Technically possible but rejected for this design | Registering the captured item in `fishing_tbfarmfish.json` makes it acceptable to every native fish tank unless a bridge blocks it. The requested device must instead be isolated by equipment id. |
| Register a separate device that reuses fish-tank scheduling but rejects fish | Feasible in the reviewed build; JSON owns construction/production and a DLL owns strict content isolation | The current deserializer and equipment factory accept `EquipmentFuncFishTank` and instantiate a complete `FishTank`, including inventory, production, rendering, and save state. The undocumented status does not require a DLL version gate under this project's community-verified-JSON policy. The DLL is needed only when the custom device must reject ordinary fish and ordinary tanks must reject the custom rows. |
| Execute arbitrary operations from the tank/token | Not supported | Official recipes and farm-fish data can generate items and native tech/event side effects. They cannot call an arbitrary callback, change the world, release a monster, or run mod-defined code. |

There are therefore three materially different products:

1. **Pure-JSON defeat-drop version:** defeat a monster, receive a custom specimen item, and process it in an existing documented machine. This is fully supported, but it is not capture and the ball is only cosmetic.
2. **Community-verified fish-tank-type JSON without a bridge:** the reviewed build can deserialize a new `EquipmentFuncFishTank` or `EquipmentFuncFishTankEcological` row and run native save/UI/production. This is usable under the project's accepted extension policy, but the base filter accepts every registered farm-fish row and therefore cannot isolate custom capsules/mineral cores from fish by equipment id.
3. **Recommended exact version:** retain JSON for item, equipment, icon, electricity, timing, output, and farm-fish production definitions, then add a narrow DLL for live capture and device-id-specific filtering. The native fish-tank lifecycle remains the production owner; the DLL does not need to reimplement it.

## Official JSON Evidence

### Item behavior

`01 新增道具` defines a normal item with `"function": { "$type": "ItemFunction" }`. `04 ID对照表（道具）` lists the currently documented function families: default/no effect, hat, equipment placement, seed, crop, food, building exterior, and wallpaper. It does not list a generic targeted/throwable/capture callback.

The same table lists `tool` as a sub-item type. This is classification data, not a function type. The native `ItemFactory` selects the runtime item subclass from the function object's CLR type, not from `sub_type`. A documented `ItemFunction` therefore creates the base `Item`, whose `OnUseAsItem` and `OnUseAsTool` methods are empty.

### Monster drops

`06 综合案例一（新增道具）` documents adding a custom item to an existing drop pool through `mod_tbmoditemspawnextension.json`. `12 ID对照表（掉落库）`, updated June 12 in the reviewed snapshot, documents per-monster pools such as `amoeba_drop`, `chomper_drop`, `drone_ex_drop`, and `fungus_drop`, and states that the corresponding pool is necessarily triggered when the monster is defeated.

This supports species-specific defeat loot. It does not know which item/weapon caused the defeat and cannot replace the defeat with capture.

### Existing processing equipment

`04 新增配方` documents `recipe_tbrecipe.json` plus `mod_tbmodrecipegroupextension.json`. `08 ID对照表（配方）` lists the supported existing machine groups, including furnaces, grinders, compost machines, ovens, pots, teapots, drying boxes, brewing equipment, and the feed machine.

This is the cleanest official-JSON downstream route: each captured-specimen item can be an ordinary recipe input and can produce any defined item output supported by the recipe schema.

### New equipment and fish tanks

`03 新增装饰设备` documents new equipment with `EquipmentFuncDecorator`. The supported example supplies placement, art, cover size, and decoration behavior, not an inventory or production scheduler. An undocumented internal equipment function type is not an official JSON contract and must not be presented as one.

The current runtime nevertheless contains a complete internal data path for a separately registered fish-tank-type device:

```text
equipment_tbequipment.json function.$type = EquipmentFuncFishTank
  -> EquipmentFuncEquipment deserializer
  -> EquipmentManager.CreateEquipment
  -> EquipmentUtils type map: EquipmentFunc + FishTank
  -> FishTank instance
  -> serialized FarmFishTank inventory/metabolism/products
```

The required function fields are visible in the current base data: `total_capacity`, `line_capacity`, `metabolism_threshold`, `update_interval`, `product_capacity`, `alpha_mask`, and `energy_capacity`. This is strong static feasibility evidence for the reviewed build. The format is not in the official Workshop documentation, but that alone does not make a runtime version-check DLL necessary: under the accepted project policy it may be used as a community-verified internal JSON format. Its compatibility baseline and fields should be recorded, and the content should be revalidated after a game update. If the native constructor/deserializer later changes, that is a normal compatibility failure to detect through regression testing rather than a reason to put the current JSON behind code.

`07 综合案例二（新增鱼)` documents `fishing_tbfarmfish.json`, including energy cost, metabolism increase, incubation/growth duration, `produce_spawn_entry`, and roe item. The native `FarmFishTank.ContentFilter` accepts only an item whose id exists in `TbFarmFish`, or an `ItemFishFry`. `ResolveFishTank` adds every accepted non-fry item id to the fish shoal, and `_GenFarmFishProduct` rolls the configured `ProduceSpawnEntry`.

The fish disguise is therefore a real data path, not merely a UI guess. Its limitations are also real:

- the captured token is a farm-fish row and participates in fish metabolism/capacity behavior;
- tank formations inspect the contained item ids as fish ids;
- fish outputs that are themselves farm-fish ids are converted to their configured roe items;
- fish birth/collection and animal-tech side effects can be raised by the native tank path;
- fish-fry/roe growth remains the native typed `ItemFishRoe` / `ItemFishFry` lifecycle, not generic monster metadata;
- this route cannot preserve an arbitrary captured monster instance, health, variant, or other runtime state.

For a species-token design, one ordinary item id per monster species is sufficient and avoids custom item serialization. A single stackable item carrying arbitrary monster identity would require code and a reviewed save/load contract.

For the clarified separate device, the bridge can make this path suitably narrow without rebuilding production:

- capsule ids still receive `fishing_tbfarmfish.json` rows so `FarmFishShoal.AddFish` and `produce_spawn_entry` remain native;
- the new equipment row uses its own exact equipment id and the current internal `EquipmentFuncFishTank` function;
- `FishTank.ContentFilter`, which is the `IContainer` filter used by the inventory UI, is overridden only for configured habitat ids and accepts only the configured capsule-id allowlist;
- ordinary fish and every `ItemFishFry` are rejected by that habitat;
- because capsule ids are also farm-fish rows, ordinary native fish tanks must explicitly reject those capsule ids, otherwise the base `FarmFishTank.ContentFilter` would accept them;
- loaded or externally inserted invalid contents must be sanitized/ejected before `ResolveFishTank` rebuilds the shoal, since serialized inventory restoration does not prove that the current filter was applied.

The capsule item may keep a player-facing subtype such as `special_capsule`; `FarmFishShoal` resolves production by the `TbFarmFish` id, not by item subtype. That mixed classification is also outside the documented fish example and therefore requires runtime validation. Using `husbandry_fish` is more conservative internally but leaks fish classification into the item UI.

### Reuse for an electricity-driven mine

The same data path is a better fit for the shelved mine than the normal fed fish tank. The current base game contains `EquipmentFuncFishTankEcological`, whose runtime class is `FishTankEcological`. It keeps the same `FarmFishTank` inventory/production owner but uses `EComProtoAppliance`; each successful appliance work cycle consumes the configured electrical `threshold` and advances metabolism without fish feed. JSON can therefore define the equipment, power threshold, work/update intervals, capacities, accepted farm-fish rows, and production spawn tables without a DLL production implementation.

The native selection is content-driven, not an unrelated global mineral pool:

```text
items currently inside the device
  -> FarmFishShoal groups them by registered TbFarmFish id and count
  -> RollFish selects one contained id, weighted by its count
  -> that id's ProduceSpawnEntry runs
  -> GetProductItems materializes the resulting item(s)
```

Consequences for the mine design:

- if the device contains only one registered mineral-core id, every default production selection uses that core's own `produce_spawn_entry`;
- if it contains multiple core ids, one is selected per production event by count-weighted roulette; the native path does not independently produce once for every distinct input type;
- more copies of the same core also increase `TotalMetabolismIncrease`, so they can accelerate the native production clock as well as weight selection;
- the contained core is a persistent catalyst/specimen and is not consumed by production. A design that consumes ore as recipe input should use a recipe machine or add separate DLL behavior;
- a one-output spawn LUT with exact min/max makes the selected core's result deterministic; a multi-entry LUT deliberately adds a second output roll;
- avoid registering the same ordinary ore id as both the farm-fish-like input and the desired output. `FarmFishTank.GetProductItems` converts any produced id that is also registered in `TbFarmFish` into that row's roe item. The clean JSON-heavy model is a distinct input such as `copper_vein_core`, registered in `fishing_tbfarmfish.json`, whose production table emits the ordinary unregistered `copper_ore` item;
- if deterministic per-device selection is required without a filter DLL, configure the device so it can hold only one catalyst/core. If multiple slots are desired but every slot must contain the same id, a device-id-aware DLL filter must inspect existing contents and reject insertion of a different core id.

This supports the intended loop “insert a mineral/vein core, consume electricity, produce that core's minerals.” It does not support “consume each inserted ore and transform it one-for-one,” nor simultaneous independent production for several different core ids, without additional code.

### JSON cannot express per-device content isolation

Neither side of the current JSON relationship carries a local filter rule:

- `FarmFishInfo` contains only `id`, energy/metabolism, incubation/growth timing, `produce_spawn_entry`, tech point, and `roe_item`. It has no allowed-equipment or denied-equipment field.
- `EquipmentFuncFishTankBase` / `EquipmentFuncFishTankEcological` contains capacity, scheduling/production, rendering mask, work duration, and the separate appliance power component. It has no allowed-item, denied-item, subtype, or tag field.

The runtime `FarmFishTank.ContentFilter` is shared by normal and ecological fish-tank equipment and makes only this global decision:

```text
accept when content.name exists in global TbFarmFish
otherwise accept only when the runtime item is ItemFishFry
```

It does not inspect the equipment id, the farm-fish row's `roe_item`, the item's player-facing subtype, or a device JSON allowlist. Consequently:

- registering a mineral core in `fishing_tbfarmfish.json` makes that id acceptable to every unmodified native fish tank;
- registering a custom ecological fish-tank-type mine makes it accept every ordinary `TbFarmFish` fish and every `ItemFishFry`;
- an actual `ItemFishRoe` is already rejected by this particular filter unless its own item id is separately registered in `TbFarmFish`; the special unconditional branch is for fry, not roe;
- capacities can restrict quantity but cannot distinguish fish from mineral cores.

Strict two-way isolation therefore needs the small device-id-aware DLL filter: custom mine/habitat ids accept only their configured core/capsule ids, and ordinary fish-tank ids explicitly reject those custom ids. Loaded or externally injected contents still need a one-time sanitize/eject path because an insertion filter alone does not repair old save inventory. A pure-JSON release can omit the DLL only by accepting cross-placement as a known limitation or by choosing a non-fish-tank recipe-machine mechanism.

### Alternative native equipment screen for the mine

A follow-up screen covered every current `EquipmentWorker` completion path, every `IGatherableEquipment` implementation, and the other periodic `UpdateNoRender` producers in the reviewed build. No single non-fish class provides all four desired properties—persistent inserted core, electrical gating, bounded product storage, and item output—but several narrower native owners are useful.

| Native class / function | Input model | Power | Output/storage | Mine fit |
| --- | --- | --- | --- | --- |
| `FishTankEcological` / `EquipmentFuncFishTankEcological` | Persistent contained farm-fish-like rows; selected by contained id/count | Native `EComProtoAppliance` | Configured spawn entry into bounded `product_capacity` | Best current fit for “insert a persistent core, consume power, repeatedly produce that core's minerals”; retains fish semantics and needs a DLL only for strict two-way content isolation. |
| `SynthesizerGenerator` / `EquipmentFuncSynthesizerGenerator` | No input | Optional native appliance; with `EComProtoAppliance`, missing power pauses work and restored power resumes it | One fixed `output_item_name` is immediately created as a world drop after every interval; no buffer/capacity | Exact static fit for “place it, provide power, continuously produce one fixed mineral,” but unsafe as an unattended final design because output is unbounded ground entities. |
| `ResinCollector` / `EquipmentFuncResinCollector` | No input; original use is a decal on a mature tree | None; the class never calls its electronic component | Fixed output/range accumulates in serialized `currentValue` and stops at `capacity` | Strong bounded “place and passively produce” candidate. A standalone row with no fit slots should fall back to `default_output`, but this combination has no base-game exemplar and needs runtime proof. It cannot be honestly power-gated by JSON. |
| `GarbageShredder` / `EquipmentFuncGarbageShredder` | One input stack, filtered by its dismantle recipe group; one item consumed each cycle | Optional native appliance through `EquipmentWorker` | Spawn-table output is dropped into the world; continues until the submitted stack is exhausted | Good only if the supposed core is actually a consumable drill charge/sample. It cannot keep one persistent core and run forever. Its dismantle recipe/group tables are internal rather than documented Workshop authoring surfaces. |
| `Synthesizer` / `EquipmentFuncSynthesizer` | Normal recipe input, consumed; player starts a finite batch | Optional native appliance through `EquipmentWorker` | Normal recipe result is dropped after each task | Clean recipe-driven powered processor, but it is a manually started finite crafting machine rather than an autonomous mine. |
| `GeneReplicator`, `GeneIncubator`, `GeneSynthesizer` | Hard-coded seed/gene capsule runtime types | Optional worker power | Clone/mutate seed/gene objects, clear their buffers on completion | Rejected: JSON cannot redirect their hard-coded type tests and gene operations to ordinary mineral items. |
| `ChickenNest`, `HoneyComb`, `MilkingMachine`, `LintRoller`, `Toilet` | External animal callbacks | Milking machine charges once; others are not a mine power loop | Bounded product lists/counts | Rejected without a feature DLL that replaces the animal producer; their equipment JSON contains no timed output rule. |

#### Power-only generator path

`SynthesizerGenerator.OnCreated` starts `Work(interval)`. Both rendered and no-render completion create one `output_item_name` and immediately call `Work(interval)` again. `EquipmentWorker` serializes working/idle/counter state and, whenever `EquipmentInfo.isElectrical` is true, routes each work launch through `ElectronicComponentAppliance.Launch`; an `EComProtoAppliance` row therefore supplies native low-power pause/resume behavior.

This function is recognized by `EquipmentFuncEquipment.DeserializeEquipmentFuncEquipment`, its matching runtime class exists, and `EquipmentUtils` maps function names to equipment class names. However, none of the stored base `equipment_tbequipment.json` snapshots uses `EquipmentFuncSynthesizerGenerator`, and the official Workshop docs do not describe it. It is a dormant/community-discovered code path rather than a base-content-proven row and must be tested before use.

Current instance status (2026-07-17): no official equipment instance has been found in the stored public/Workshop baselines. This is an evidence gap, not proof that no formal-release build ever used the type. Recheck the official equipment, item, localization, recipe, and room-preset data when the mine mod is rebuilt or when a formal-release baseline is available; until then, describe `SynthesizerGenerator` only as an internal runtime capability with no currently located official exemplar.

Its bigger design problem is not deserialization but retention. Each completion calls `DropItemManager.CreateDropItem`, which creates a new serialized `DropItem` and does not merge it into an existing stack. A farm room does not run the dungeon-only disposable-drop cleanup path. An unattended fast generator can therefore grow room entities, save data, rendering work, and GC pressure without a bound. Given the project's existing long-session GC concern, this class is suitable for a minimal feasibility fixture or a deliberately slow/automated machine, not the default final mine unless a bounded output owner is added.

#### Bounded passive collector path

`ResinCollector` stores only a counter and `currentValue`. Its no-render update increments by the chosen `ResinCollectorOutputInfo.range`, clamps to `capacity`, and stops progressing when full. Collection materializes the accumulated count and resets it. This is materially safer for long sessions than unconditional ground drops and already implements `IGatherableEquipment`.

The original row is a decal because it has fit slots, while `EquipmentInfo.isDecal` is derived from whether `fit_slot_datas` is non-empty rather than from the function class. `ResinCollector.GetTargetOutputInfo` explicitly falls back to its configured `default_output` when there is no recognized tree host. Static code therefore supports the hypothesis that a row with empty `fit_slot_datas` can instantiate it as a standalone fixed-output collector. There is no current base exemplar for that combination, so placement, save/load, removal, automation gather, and preview rendering remain runtime gates.

Adding `electronic_component: EComProtoAppliance` to this row is not enough: `ResinCollector.UpdateNoRender` never calls `Launch`, so it would continue producing without consuming or requiring electricity. A powered bounded variant needs a small DLL gate or a new reviewed equipment runtime. It still has no input inventory, so different mineral outputs must be represented by different equipment ids/configured default-output rows rather than inserted cores.

#### Resulting mine choices

1. **Persistent selectable core:** keep `FishTankEcological`; use distinct core ids and the content-isolation DLL. This has bounded products and avoids ground-entity growth.
2. **No core, fixed output, power required:** `SynthesizerGenerator` is the narrowest JSON/code path, but only after a runtime fixture proves creation/load/power behavior and its unbounded drops are accepted or externally collected.
3. **No core, fixed output, bounded and passive:** test standalone `ResinCollector`; it is the safest pure-data retention model but has no power condition.
4. **Consumable charge or ore input:** use `GarbageShredder` for spawn-table output or normal `Synthesizer` for documented recipe-style output; neither represents a permanent mine core.

The placement-preview scale defect is orthogonal to these function choices. All of them still use the equipment/item sprite and builder preview path, so changing the production function does not itself prove that an enlarged well-derived preview will render at the intended scale.

## Native Responsibility And State Holders

### Use seam

`Item.UseAsItem` first runs the native condition check and then calls the virtual `OnUseAsItem`. The base `Item.OnUseAsItem` is empty. `ItemFactory.GetProtoInstanceName` derives the runtime item class from `proto.Function.GetType()`, and a documented `ItemFunction` resolves to the base `Item` class.

For a bridge-owned capture item that deliberately remains a documented `ItemFunction`, the narrowest candidate hook is the base `Item.OnUseAsItem`, filtered by exact registered item id. This preserves the native condition check and does not need to intercept every attack or monster damage call. Hooking `Item.UseAsItem` as a prefix and skipping the original would incorrectly bypass native use-state checks.

### Target owner

The current room's `IMonsterHost` owns `DM_monster` and exposes the live monster set through `MonsterEnv.AllMonsters` / `DM_monster.AllMonsters`. It also has native nearest-monster queries with controller validity, range, shield, and line-of-sight checks. A first slice can select a short-range nearest eligible monster and then apply an additional facing/front-cone rule. A visually thrown projectile and collision owner would be a separate, larger ranged-item feature.

### Removal owner

`Monster.Remove()` is the relevant replacement-removal seam:

```text
Monster.Remove
  -> reject missing controller or already-dead monster
  -> set isDead
  -> MonsterController.OnDead / MonsterDecorator.OnDead
  -> delayed IMonsterHost.RemoveMonster
  -> MonsterManager removal and entity recycle
```

`Monster.Kill()` calls `Remove()` and then additionally invokes death effects, slain-monster events, battle experience/collection recording, and the normal monster drop library. A capture that replaces defeat should not call `Kill()` and should not separately call `InvokeDropLib()`.

`Remove()` still calls the monster decorator's `OnDead`. This is harmless only for reviewed ordinary monsters. `MonsterDecoratorNian.OnDead`, for example, contains boss-stage and custom event behavior. The bridge must therefore use an exact allowlist and fail closed for bosses, multi-part monsters, mission targets, summons, and any unreviewed custom decorator.

### Output owner

The bridge can validate and create a configured ordinary item through the native item factory, then place it in the backpack or create a room drop through the native inventory/drop owner. The lowest-state version creates a normal species-specific item and needs no custom save serializer. The item then follows the game's ordinary inventory/save lifecycle and official JSON owns all subsequent recipes or farm-fish output.

## Recommended Small DLL Boundary

Do not add monster capture to DTMAPI Core, the BepInEx bootstrap, or the broad `ICustomMonsterApi`. The current public matrix marks only custom-monster definition/registry contracts as `StableCandidate`; native monster creation verbs and adapters remain blocked/Experimental/Proposed. Capture is a narrower native interaction and removal contract.

The recommended first implementation has two ownership layers while remaining a small feature:

| Layer | Responsibility |
| --- | --- |
| JSON content | Define capture-ball items, one capsule item per allowed species, icons/text, output items, a separate `EquipmentFuncFishTank` / `EquipmentFuncFishTankEcological` row, electricity/timing/capacity, and farm-fish production rows. These function rows are community-verified internal JSON for the reviewed build, not official Workshop-document guarantees. |
| First-party functional mod | Own the capture chances, monster-to-capsule mappings, habitat/mine-to-accepted-content mappings, enable/disable state, and player-facing messages. Its DLL executes capture attempts and feature policy. It must be an ordinary DTMAPI-managed mod, not a DLL installed directly under `BepInEx/plugins`. |
| `DTMAPI.GameBridge.DolocTown` internal adapter | Provide only the fragile native seams needed by the feature DLL: resolve item use, current `IMonsterHost`, eligible target, native removal and deterministic item placement; provide equipment-id-aware container filtering/sanitization without exposing raw game types. It does not own fish-tank production data or feature policy. |

There are two independent code seams in the minimum complete feature. The first is live capture: select and validate a monster, roll the configured ball chance, consume the ball on a valid attempt, leave the monster unchanged on failure, and on success call the reviewed removal path and create exactly the mapped capsule. The second is content isolation: narrow `FishTank.ContentFilter` by exact equipment id, keep ordinary fish/fry out of the custom device, keep custom farm-fish-like rows out of ordinary tanks, and sanitize invalid loaded contents. Both require DLL behavior for the strict design; electricity, scheduling, save state, and item production do not.

A sufficient first-slice companion file is intentionally small:

```json
{
  "schema_version": 1,
  "range": 3.0,
  "capture_items": [
    { "item_id": "monster_ball", "success_chance": 0.35 }
  ],
  "targets": [
    { "monster_id": "amoeba", "result_item_id": "captured_amoeba" },
    { "monster_id": "fungus", "result_item_id": "captured_fungus" }
  ],
  "habitats": [
    {
      "equipment_id": "monster_habitat",
      "accepted_item_ids": ["captured_amoeba", "captured_fungus"]
    }
  ]
}
```

`range` and `success_chance` are policy rather than native facts. `success_chance` should be validated as a finite value in `[0, 1]` and belongs to the capture-ball entry, as requested. A later design could add a per-monster modifier, but it is unnecessary for the first slice. A fixed chance also avoids touching monster health and attack state.

The first-slice transaction should be:

1. Run only after native item-use eligibility passed and only for an exact registered base `ItemFunction` ball id.
2. Require a current `IMonsterHost`, a live rendered target, an exact allowed monster id, a reviewed ordinary decorator, and range/front/line-of-sight validity.
3. Validate and pre-create the exact configured result item, and validate a room-drop host, before consuming the ball or changing the monster. There is no output drop-table roll.
4. Reserve one ball through the native selected-item inventory transaction. If the selected stack changed and reservation fails, abort without rolling or changing the monster.
5. Roll that ball's `success_chance` once. On failure, discard the reserved ball and optionally show a miss/failure effect; do not call damage, hurt, attack, remove, death, or drop methods on the monster.
6. On success, call `Monster.Remove()`. If it returns `false`, restore the reserved ball through the native placement/drop owner and create no capsule.
7. If removal succeeds, create exactly one mapped capsule as a native room drop at the target position. Do not call `Monster.Kill()`, `InvokeDeadEvents()`, or `InvokeDropLib()`.
8. Log the ball id, rolled chance/result, monster id, capsule id, room id, and outcome without retaining raw controller objects across frames or transitions.

Because `Monster.Remove()` schedules host removal one frame later and marks the monster dead immediately, the bridge must guard against repeated input in that frame. The item/drop portion should be implemented as a main-thread transaction with explicit failure logging. If output placement cannot be made provably total, treat that as an implementation blocker rather than silently deleting the monster.

The requested failure semantics are achievable. `Monster.HasBeenAttacked` changes through the native damage path; a failure branch that only selects the target, consumes the ball, and shows a detached visual/message does not call that path. The monster keeps its health, AI state, cooldowns, aggro state, drop state, and host membership. No eligible target or a technical/configuration failure should consume a ball; only a valid probabilistic attempt does.

## Deliberate V1 Semantics

The smallest coherent behavior is **capture is not a kill**:

- no normal monster loot;
- no battle experience;
- no slain-monster event or kill-mission increment;
- no automatic monster collection-book unlock;
- one capture ball consumed on every valid probability attempt, including failure;
- a failed roll leaves the monster entirely unchanged;
- a successful roll removes exactly one monster and deterministically produces exactly one mapped species capsule;
- regenerated dungeon/room populations continue to follow native room generation; capture does not make a wild species permanently extinct.

Making capture count as a kill is a separate policy decision. It would need explicit review of mission progress, collection records, boss/story events, guarantee-drop accounting, and double-reward behavior.

## What Is Not Small

The following requests should not be folded into the first bridge:

- a physical thrown ball with travel, collision, miss/recovery, and hit animation;
- a single serialized capsule item carrying arbitrary instance state;
- releasing the original monster back into the world;
- a new non-fish-derived habitat runtime with independently implemented inventory UI, power, TU/offline progression, production, save/load, construction removal, automation, and multiplayer ownership;
- arbitrary callbacks or world operations triggered by a specimen;
- capturing every native/custom monster through a general public API.

A truly independent habitat is its own machine-domain project. Reusing the native `FishTank` implementation under a separate registered equipment id, then isolating its accepted contents through the bridge, is the smallest viable habitat for this request. It still inherits native fish-tank scheduling, feed/metabolism, rendering, UI, animal-tech, and save semantics unless those are separately reviewed and changed.

## Rejected Shortcuts

- **Use `sub_type: "tool"` to create behavior:** subtype does not select the runtime item class.
- **Use a normal monster drop pool and call it capture:** the pool fires on defeat and cannot inspect the killing item.
- **Call `Monster.Kill()` from the bridge:** this produces normal kill events, experience, collection records, and loot in addition to the captured item.
- **Call `Monster.Remove()` for every monster:** decorator `OnDead` can own boss/story behavior; exact allowlisting is mandatory.
- **Describe the undocumented fish-tank `$type` as an official documented guarantee:** the reviewed build has the entity/UI/save path and the project permits using that community-verified JSON capability, but its provenance and tested game baseline must remain explicit. No DLL version gate is required merely because the official Workshop document omits it.
- **Expose this as stable `ICustomMonsterApi`:** current custom-monster runtime adapters are still blocked and capture does not prove spawn, AI, persistence, or general runtime creation.
- **Install the bridge directly under BepInEx:** that bypasses DTMAPI ownership, disable/re-enable cleanup, version constraints, and mod management.

## Validation Gate For A Later Implementation

Before an implementation can be called complete, validate at least:

- unmatched generic items retain native no-op behavior and all non-capture item subclasses remain untouched;
- no current room, no monster host, no target, out of range, wrong facing, blocked line of sight, unmapped monster, invalid result id, already-dead target, and feature disabled all consume nothing;
- chance `0` always consumes one ball and never changes the monster; chance `1` always consumes one ball and captures an otherwise valid target; seeded/injected boundary rolls prove the comparison convention;
- a failed valid roll changes no monster health, `HasBeenAttacked`, AI/aggro, cooldown, host membership, event, experience, loot, or collection state;
- one success removes exactly one target, creates exactly one mapped result with no second drop roll, and consumes exactly one ball, including a stacked ball and a full backpack;
- held/repeated use cannot capture or reward the same monster twice during delayed host removal;
- captured ordinary monsters do not grant normal loot, experience, collection unlock, guarantee-drop progress, or kill-mission progress;
- boss, Nian parts/stages, story/mission monsters, static targets, and unreviewed custom decorators are rejected;
- room exit/re-entry, dungeon regeneration, save/load, title/load, mod disable/re-enable, and game restart leave no stale controller references or duplicate hooks;
- ordinary captured items survive native inventory save/load and official recipe processing;
- the separate habitat accepts only its configured capsule ids and rejects ordinary fish and `ItemFishFry`; ordinary fish tanks reject capsule ids; UI clicks, tidy/all-transfer, buffer return, automation, save load, and config removal cannot bypass the policy;
- invalid legacy/edited contents are returned to the backpack or dropped without loss before shoal resolution; native fish tanks and unrelated containers remain unchanged;
- fish-tank-type equipment creation, feeding/metabolism, production collection, formation behavior, output-as-fish-to-roe conversion, UI terminology, rendering, and tech/event side effects are accepted and documented;
- any `SynthesizerGenerator` fixture proves fresh placement, no-power idle, powered production, power-loss pause, restored-power resume, offscreen work, save/load, removal, and restart; a long-duration test quantifies serialized ground-drop growth before this path is accepted for release;
- any standalone `ResinCollector` fixture proves placement with empty fit slots, null-host default-output fallback, bounded stop at capacity, manual and automated gathering, save/load, removal, and preview rendering; adding an appliance row must not be claimed to power-gate it unless a runtime owner actually calls `Launch`;
- clean game exit, no leftover process, startup/hook evidence, third-save evidence, and the relevant smoke/debug records are captured under the normal runtime-lock protocol.

## Evidence Checked

Official snapshot pages:

- `OFS1w4gFSiDkHRkXX1Pcu4ihnNh` — `01 新增道具`
- `QaKDwjPdvi2GtEk7GKPcQEoenyf` — `04 ID对照表（道具）`
- `KWqewbBLEiRw1Pkcn6lcZpQ7nXg` — `06 综合案例一（新增道具）`
- `TOb9w60BJihlQgkwv87cEtzFnVd` — `12 ID对照表（掉落库）`
- `VxeWw7ZDjiqFcTk0yLxcnB53n9b` — `04 新增配方`
- `Vm9YwaBVMiZtvbkf5kIcASronye` — `08 ID对照表（配方）`
- `PSUjwr1GuiRixsk95HlcYTVQnPd` — `03 新增装饰设备`
- `DSGAwje93icwXVkY8WvcpxCWnXg` — `05 ID对照表（设备）`
- `KkZ0wpgT1iLu4Vk2P32cNxnrnCd` — `07 综合案例二（新增鱼)`

Current decompiled baseline:

- `Item.cs`
- `ItemFactory.cs`
- `AgentControllerState.cs`
- `EquipmentManager.cs`
- `EquipmentUtils.cs`
- `Config/Equipment/EquipmentInfo.cs`
- `EquipmentWorker.cs`
- `IEquipmentWorker.cs`
- `ElectronicComponentAppliance.cs`
- `SynthesizerGenerator.cs`
- `Synthesizer.cs`
- `GarbageShredder.cs`
- `ResinCollector.cs`
- `DropItemManager.cs`
- `DropItemBase.cs`
- `DropItem.cs`
- `DungeonRoom.cs`
- `EquipmentPatch.cs`
- `IContainer.cs`
- `ContainerBaseUiState.cs`
- `IMonsterHost.cs`
- `Monster.cs`
- `MonsterController.cs`
- `MonsterDecorator.cs`
- `MonsterDecoratorNian.cs`
- `MonsterManager.cs`
- `DolocAPI.cs`
- `FishTank.cs`
- `FishTankEcological.cs`
- `FarmFishTank.cs`
- `FarmFishShoal.cs`
- `FarmFish.cs`
- `ItemFishRoe.cs`
- `Config/Equipment/EquipmentFuncEquipment.cs`
- `Config/Equipment/EquipmentFuncWorker.cs`
- `Config/Equipment/EquipmentFuncFishTank.cs`
- `Config/Equipment/EquipmentFuncFishTankBase.cs`
- `Config/Equipment/EquipmentFuncFishTankEcological.cs`
- `Config/Equipment/EquipmentFuncSynthesizerGenerator.cs`
- `Config/Equipment/EquipmentFuncSynthesizerBase.cs`
- `Config/Equipment/EquipmentFuncGarbageShredder.cs`
- `Config/Equipment/EquipmentFuncResinCollector.cs`
- `Config/Resource/ResinCollectorOutputInfo.cs`
- `Config/Recipe/DismantleRecipeInfo.cs`
- `Config/Recipe/DismantleRecipeGroupInfo.cs`
- current base `equipment_tbequipment.json` normal and ecological fish-tank rows
- current base `equipment_tbequipment.json` resin-collector and garbage-shredder rows; no base row uses `EquipmentFuncSynthesizerGenerator`

Project boundary evidence:

- `docs/api/public-api-matrix.md`
- `docs/reviews/api/native-owner-domains/12-held-ranged-weapons-projectiles.md`

No runtime launch, save mutation, official-content edit, or code implementation was performed.
