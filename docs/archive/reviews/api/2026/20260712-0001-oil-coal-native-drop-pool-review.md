# 20260712-0001 Oil And Coal Native Drop-Pool Review

Status: recorded
Date: 2026-07-12
Domain: Oil item acquisition from the native `coal_mine` resource
Related decision review: `docs/reviews/code/2026/20260712-0004-major-update-product-version-decision-docket.md`
Related Update: `docs/updates/2026/20260712-0009-major-update-product-version-decision-docket.md`

2026-07-13 selected refinement: `docs/reviews/code/2026/20260713-0001-major-update-second-decision-docket.md` chooses the lower-conflict first implementation: append only Oil weight 25 and do not replace coal or amber. This accepts amber changing from exactly 1% to about 0.976% per draw. The exact amber-preserving vector below remains analysis, not the selected development JSON.

2026-07-13 implementation resolution: `docs/updates/2026/20260713-0012-oil-official-json-oneaction-decoupling-p0.md` verifies the selected DLL-free official JSON package, retirement of the Oil GameBridge/OneAction callback route, exact current capped-amber fall-through probabilities, Oil-disabled absence, and an Oil-enabled natural native world drop with all four generic OneAction scenarios. The sufficiently-large distribution sample listed below is scoped only to later economy validation and product promotion; it is not an ownership-P0 completion gate. Deterministic exact-algorithm coverage plus the natural world-drop run close this P0. Final statistical economy tuning and product promotion remain deferred.

## Source Request

The user identified a local independent fish-expansion Mod which adds new fish rarity indices and rewrites fishing-pool rarity weights, and asked whether the same technique can keep Oil from taking probability away from the coal mine's existing rare product. Oil may be a rare native output or a genuinely additional output, must not reduce the coal mine's existing rare-product probability, and should not remain a fixed `x1` backpack grant which bypasses native collection bonuses.

The user confirmed that the GameBridge `crude_oil` hard-code and OneAction-to-Oil callback must be removed. Official JSON should own Oil's static item/drop content if the native semantics are sufficient.

This is a clean-room semantic/native-owner review. The local third-party Mod was inspected read-only for visible JSON behavior only. No third-party file, binary, source, or implementation was copied into DTMAPI.

## Current DTMAPI Boundary

There is no intended public Oil drop API. The current route is an internal product-specific sidecar:

- `OilCoalDropService.TryRollOilDropFromCoal` recognizes coal names, performs an independent `8%` managed roll, hard-codes `crude_oil`, and puts exactly one item directly in the backpack;
- the shared ToolCollider Prefix/Postfix route captures and applies that sidecar after a native tool hit;
- `ActionCompletionService` receives the Oil method as a delegate and invokes it after its synthetic final `_Fell` call;
- the route exists even when `DTMAPI.OilMod` has not registered demand.

Recommended status: **retire this internal route; do not promote it to a public API**. `IActionCompletionApi` must complete native resource actions without knowing that Oil exists.

## Fish-Pool Reference Semantics

Local evidence: Steam subscription `3727940804`, display name `鱼类拓展包`, version `1.0.1`.

The visible content does two coordinated things:

1. its fish extension assigns new fish to rarity indices `6` and `7`;
2. its full `fishing_tbfishingpool.json` definitions append weights for those indices and rebalance the existing weights.

For `marine_default`, the original six rarity weights sum to `145`. The Mod multiplies those six weights by `100` (sum `14,500`) and appends `165` and `18` for the two new indices. The new buckets therefore occupy `183 / 14,683 = 1.2463%`; every old bucket keeps its relative relationship to the other old buckets but its absolute probability is multiplied by `14,500 / 14,683 = 98.7537%`. The fish Mod does **not** preserve every old fish's absolute probability. This distinction is exactly what the user's “do not steal amber probability” requirement must improve for coal.

The native owner is `DolocAPI.RollFish`. It first filters eligible fish, finds the distinct rarity indices still present, selects one rarity using `FishingPoolInfo.RarityWeights[index]`, and then uniformly chooses a fish inside that selected rarity. The weight array is therefore a true first-stage rarity-bucket table with variable length.

That makes the Mod's technique valid for fishing: it creates new first-stage buckets instead of adding the new fish to an old bucket, while explicitly controlling the old and new bucket weights.

## Coal-Mine Native Owner And State

Authoritative native path for the current public build:

```text
resource_tbresource.json
  coal_mine / resource_type=ORE
  drop_spawn_entry = coal_mine_drop
  count_range = 3..4
    -> GuaranteedManager.SpawnResourceDropItems
       -> collectionAbility.GetCollectionStoneCount(native random count)
       -> ItemSpawnInfo.SpawnItems(totalCount, validity filter)
          -> ISpawnLut.SpawnInternal
       -> resource global-limit / guarantee handling
       -> native world drop creation
```

Native state holders:

- `ResourceInfo` / current resource level owns the `drop_spawn_entry` and its `3..4` count range;
- `ItemSpawnInfo` for `coal_mine_drop` owns one flat `SpawnDatas` list;
- the base list is coal weight `990`, unlimited, and amber ore weight `10`, maximum one;
- `GuaranteedManager` owns unlock, global-limit, and pity counters for resource outputs;
- `collectionAbility` owns the stone-collection count bonus applied before the LUT draws.

The coal pool has no rarity index and no nested category list. `ISpawnLut.SpawnInternal` allocates minimum counts first, then uses every remaining output slot for a cumulative-weight draw over the same flat item list. Consequently, adding Oil to `mod_tbmoditemspawnextension.json` makes Oil another candidate for the coal mine's existing output slots. It cannot create a separate independent probability channel.

## Can The Fish Technique Be Reused?

**The structure cannot be copied, but the probability-rebalancing principle can be reused.**

- Not reusable: coal JSON cannot append a new rarity index/bucket equivalent to fish rarity `6` or `7`.
- Reusable: Oil can extend the native flat LUT, and the existing amber weight can be deliberately adjusted so amber retains its original share of each pre-cap draw instead of being diluted by the larger denominator.
- Native consequence: Oil consumes a slot that would otherwise normally be coal. It need not consume amber's probability share. This is a native **rare output**, not a mathematically independent extra output.

Let `C`, `A`, and `O` be coal, amber, and Oil weights. To retain amber's original pre-cap per-draw share of `1%`:

```text
A / (C + A + O) = 0.01
```

With coal left at `C = 990`, the replacement amber weight is:

```text
A = (990 + O) / 99
```

This formula, not a fixed proposed Oil weight, is the current durable decision. The final Oil target belongs to Oil economy tuning and distribution tests. For illustration only, reproducing approximately an `8%` chance of at least one Oil across an equally likely 3-or-4-draw unbuffed node would require about a `2.356%` Oil share per draw; it must not be encoded without an explicit economy decision.

An integer-share illustration for that old `8% per unbuffed node` target is:

```text
coal      96,644 / 100,000 = 96.644%
amber      1,000 / 100,000 =  1.000%
crude_oil  2,356 / 100,000 =  2.356%
```

The corresponding official extension would redeclare all three entries in the array order `coal`, `crude_oil`, `amber_ore`. Because the native handler removes and appends each declared item in sequence, that produces the same final order and keeps maximum-one amber last, matching the base table's capped-roll behavior. This vector is a review example, not selected economy data. The real merged-table test must still prove the final order after every enabled Mod has been merged.

## Count And Bonus Semantics

The official pool route corrects the current fixed-`x1` problem if Oil uses `max_count: 0`:

- `0` means unlimited in native `SpawnData`, not zero allowed;
- an unbuffed coal node performs 3 or 4 draws, so a rare Oil candidate can yield zero, one, or more Oil;
- because `coal_mine` is `DungeonResourceType.ORE`, native `GetCollectionStoneCount` increases the total draw count when the collection-stone bonus applies;
- more native draws increase Oil occurrence and can increase Oil count, instead of granting a separate fixed item after the native transaction.

If Oil uses `max_count: 1`, it remains capped at one item per node. That does not match the user's requested non-fixed/count-bonus direction and is not recommended without a later economy reason.

## Official JSON Limits And Compatibility Risk

`mod_tbmoditemspawnextension.json` can add or replace entries by `item_name`, but the native handler removes a replaced entry and appends the replacement. Entry order matters after a maximum-one item has already been selected because a later capped roll may be discarded or fall through to a later entry. Therefore the final JSON must be tested as the complete merged list, not validated from weights alone.

Adjusting amber to preserve its probability also creates a content-pack composition risk:

- another Mod may extend or replace the same `coal_mine_drop` entries;
- load order can determine the last replacement for the same item id;
- the merged list and real distribution, not an isolated Oil JSON file, are authoritative.

The official `resource_tbglobalguaranteed.json` path can add unlock/global-limit/pity behavior, but it does not provide a normal independent extra random roll. It should be used only if Oil deliberately needs a pity threshold or global cap.

## Selected Product Direction

For the current Oil prototype:

1. use official content JSON for `crude_oil` and a native `coal_mine_drop` extension;
2. treat Oil as a rare candidate funded from normal coal probability, while preserving amber's pre-cap probability through explicit rebalance;
3. use unlimited Oil count unless economy testing selects a cap;
4. remove `OilCoalDropFeature`, its service/caches/Hook routes/statuses/smoke forcing, and the OneAction callback;
5. do not create a public resource-extra-drop API merely to preserve the old managed `8% x1` behavior.

If product design later requires Oil to be a **purely additional** output which reduces neither coal count nor amber probability, official flat-LUT JSON is insufficient. That requirement would reopen a separate generic native resource-output API review; it must not restore an Oil-specific GameBridge Hook.

## Acceptance Gates For Implementation

- Before implementation, re-check the current-build method bodies for `GuaranteedManager.SpawnResourceDropItems`, `ISpawnLut.SpawnInternal`, the Mod item-spawn extension handler, and `SpawnData.Unlimited`.
- Static package validation proves the Oil item and merged `coal_mine_drop` entry resolve through official JSON.
- A deterministic/source-level distribution test covers the final entry order, amber cap, Oil unlimited count, and 3/4 plus buffed draw counts.
- Runtime smoke breaks a real `coal_mine` through the native path, observes world drops rather than a direct backpack grant, and proves OneAction reaches the same native drop owner without an Oil callback.
- A sufficiently large distribution sample checks that amber remains within the agreed tolerance, Oil matches its selected economic target, Oil can exceed one when additional native draws permit it, and ordinary coal quantity changes only by the explicitly budgeted Oil share.
- Oil absent/disabled leaves the native base coal/amber table unchanged and installs no Oil Hook, cache, updater, or status route.
- Save load, return to title, and clean shutdown leave no former pending-Oil cache or ToolCollider Oil callback.

No economy-balance or product-promotion claim is complete until the large-sample gate passes. The ownership P0 is complete once the current native algorithm, official JSON package, Oil-off boundary, natural world-drop path, OneAction decoupling, lifecycle, and clean-exit gates pass.

## Validation Boundary

This review inspected current DTMAPI source, the local fish Mod's visible manifest/JSON semantics, official Workshop documentation, official extracted configs, and the current public-build native owner method bodies. It did not edit Mod JSON/source, launch the game, acquire the runtime lock, sample a live distribution, or mutate the Steam subscription.
