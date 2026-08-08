# 20260713-0010 Major Update Fifth Decision Docket

Status: recorded / fifth-round decisions closed
Date: 2026-07-13
Scope: unified animal content product, animal economy, short-SFX author route, Manbo/C# API migration, playback maturity, and BGM project boundary
Related Update: `docs/updates/2026/20260713-0004-fourth-round-closure-fifth-decision-docket.md`
Closure Update: `docs/updates/2026/20260713-0005-fifth-round-closure-sixth-decision-docket.md`
Follows: `docs/reviews/code/2026/20260713-0008-major-update-fourth-decision-docket.md`

## Source Request

The user supplied `D:/下载/第四轮.md`, selected J1/revised-K1/L1/M1/N1, corrected the MoreSaves protected behavior baseline, and asked to continue a fifth round.

The user then supplied `D:/下载/第五轮.md` and closed O/P/Q/R/S/T. O1 was refined by permanently retiring Lightning Chicken as research rather than retaining a later product promise. P was replaced with a user-defined P2 custom-product/official-JSON processing chain rather than selecting the proposal's old P2 wording. R1 was refined so Manbo first serves as a real Workshop migration Canary, after which the product may remain or retire. T was labeled T0 to emphasize that 0.5.5 makes no BGM promise; its substantive boundary matches the proposed independent-future-project direction.

This round covers content/economy/audio rather than UI ownership. It is docs/source analysis and does not implement any option.

## Decision-Label Resolution

The final user labels are authoritative. Because the response introduced new meanings for P2 and T0, they must not be read as selecting the identically numbered proposal text:

| Final label | Durable meaning |
| --- | --- |
| O1 revised | Hatch/Mole/Drecko/Oilfloater in one content product; Shell Crab unpublished advanced prototype; Lightning Chicken retired research with no future product promise. |
| P2 revised | Custom primary/hidden animal products, processed through official JSON into fixed native resources and recipes; product owns all probabilities, quantities and economy. |
| Q1 | Declarative `SoundKey -> WAV` content route with a small reviewed catalog. |
| R1 revised | Manbo data migration Canary; C# registration API enters I1 retirement; keep-versus-retire the content product is decided only after migration evidence. |
| S1 | Content contract, SoundKey/category and playback backend mature independently. |
| T0 | No current BGM schema, API, SoundKey or product; future work is a separately chartered native/Wwise project. |

## Fourth-Round Closure Incorporated

- J1 is one `Mods / Settings / Hotkeys / Help` player center with advanced diagnostics and no second keybind store/input owner.
- revised K1 protects the existing default-twelve MoreSaves behavior, records 16-slot manual success and 18-slot UI overflow, and treats naming/count/scroll as new later features.
- L1 freezes/warns `ISaveSlotsApi` in 0.5.5 and permits conditional removal only through I1.
- M1 keeps Y console permanently optional; N1 extracts equivalently before a separate full rewrite.

The durable MoreSaves player feedback is `docs/reviews/manual-qa/2026/20260713-0001-moresaves-long-term-player-baseline-review.md`.

## Closed Correctness Inputs, Not Fifth-Round Choices

- public animal assets require original work or explicit redistribution permission and a per-asset provenance inventory;
- the unified pack uses a new product identity and cannot impersonate an old package;
- existing animal/species/item/LUT/animator/sound ids are preserved or receive an explicit pre-public prototype-save migration;
- new/old animal packs cannot be enabled together, and player Runtime does not auto-delete old packages;
- AudioReplacement content discovery becomes B0 lifecycle/content-generation driven, with no ordinary per-frame file/timestamp/signature scans;
- audio suppression is fail-open and duplicate suppressing scopes receive deterministic visible errors;
- short SFX work cannot be called a BGM proof or ISSUE-010 GC fix.

## O - Unified Animal Pack Scope

Focused review: `docs/reviews/code/2026/20260713-0009-first-party-animal-pack-product-boundary-review.md`.

| Option | Meaning | Trade-off |
| --- | --- | --- |
| O1 - four proven routes first | New pure ContentPack includes Hatch/Mole/Drecko/Oilfloater; Shell Crab stays independent; Lightning Chicken does not enter the product. | Delivers one healthy architecture without waiting for legacy code. |
| O2 - wait and release five together | Convert Lightning Chicken first, then release all non-Shell-Crab animals. | One complete first roster, but old CodeMod/asset work blocks four routes. |
| O3 - keep four packages separate | No consolidation migration. | Lowest immediate risk, continued identity/update/validation/support duplication. |

Final decision: **O1 revised**. The package prefers official JSON and uses `Content/DTMAPI` declarations only for PNG/WAV/template-animation/native-adaptation gaps. Its physical layout separates official JSON, DTMAPI declarations, per-species files and audio. Species, item, spawn, produce, sprite and sound identities do not depend on an asset filename so later art replacement does not alter content/save identity.

Shell Crab remains `Prototype / Unpublished / AdvancedAssetBundle`. Lightning Chicken becomes `RetiredResearch / NeverPublish`; only its audit, failed route and native-owner evidence remain. Public staging remains blocked until asset provenance/replacement passes, and shipped assets remain separate from Author SDK/templates/tutorials with a source-status/hash inventory.

## P - Animal 1.0.0 Economy

| Option | Meaning | Trade-off |
| --- | --- | --- |
| P1 - native-template comparable | Tune bag/sale/growth/space/feed/production/breeding/capacity around each native template while preserving thematic official-item outputs; roughly 80%-120% event-value is a starting band, not the final formula. | Defensible vanilla-scale 1.0.0; unique industry moves later. |
| old P2 - adjust purchase/output only | Keep all prototype lifecycle values and rebalance only visible prices/counts. | Smaller migration, mixed hard-to-explain mechanics. This is not the user's final P2. |
| P3 - unique product/processing/hidden economy now | Build custom items, recipes and rare products before 1.0.0. | Strong identity, much larger product project. This is the nearest proposal ancestor of the user's revised P2. |
| P4 - preserve prototype values | Keep current common-800 and current outputs. | Minimal changes, no coherent shared economy. |

Final decision: **P2 revised**, superseding the proposal's recommendation and old P2 definition.

```text
animal
-> custom primary or hidden product
-> official-JSON processing/crafting recipe
-> fixed native resources
-> native economy and downstream recipes
```

All primary and hidden outputs are custom items. Official JSON should own item definitions, production and probability LUTs, hidden outputs, processing/crafting recipes, stores, technology and text wherever the game can express them. GameBridge must not own species tables, probabilities or processing quantities.

Each species 1.0.0 design includes a primary pool, hidden product, processing results, direct recipe uses and an economy target. Hatch begins with `hatch_egg`, `mutated_hatch_egg` and a rare `giant_hatch_egg`; exact weights and yields wait for the economy rebuild. Before implementation, a native/schema review must prove ordinary candidate LUTs, an independent hidden-product path, and multi-result/batch processing. If an independent hidden route is absent, the giant egg becomes a low-weight official LUT candidate rather than a species-specific GameBridge Hook.

The tuning target is intentionally above vanilla husbandry: stable return around 2-3 times the vanilla comparison and long-run return including hidden products around 3-5 times, while remaining a meaningful alternative to crops and fish. The product-owned model must include purchase, lifecycle, feed/energy/mood, capacity, occupied-space daily return and payback. Custom-item sale value, processing value and processing cost must prevent direct-sale-plus-processing arbitrage.

## Q - Formal Short-SFX Author Route

Focused review: `docs/reviews/api/2026/20260713-0003-short-sfx-content-and-api-boundary-review.md`.

| Option | Meaning | Trade-off |
| --- | --- | --- |
| Q1 - JSON + reviewed event catalog | `audio-replacements.json` is the author route for narrow allowlisted `SimpleSfx` and scoped `AnimalVoice`; Author SDK validates it. | Working data route with a deliberately small contract. |
| Q2 - JSON and C# equally formal | Expand/maintain dynamic public registration beside JSON. | Larger permanent API and arbitration/path/lifecycle promise. |
| Q3 - wait for Wwise | Withdraw author audio until a native bank backend exists. | Highest mixer fidelity bar, blocks protected current behavior. |

Final decision: **Q1**. The first public catalog contains only roughly 5-15 verified short one-shot targets rather than exposing the complete native event list. Authors select a stable `SoundKey`, provide a WAV and declare it in JSON; DTMAPI owns native-event mapping, suppression and backend differences. Author SDK owns search, maturity display, format/path/context validation and conflict diagnostics.

## R - Manbo And C# API Lifecycle

| Option | Meaning | Trade-off |
| --- | --- | --- |
| R1 - data-only Manbo + I1 API retirement | Preserve Workshop/UniqueID; publish 1.0.0 as JSON+WAV ContentPack; 0.5.5 freezes/warns C# API and conditionally removes it later. | Removes a no-policy DLL while keeping product/update identity. |
| R2 - keep CodeMod/API indefinitely | Retain DLL for static registration/status. | Avoids migration, preserves unnecessary code/public surface. |
| R3 - new Manbo product identity | Leave old product and publish a replacement item. | Breaks factual identity/update continuity without benefit. |

Final decision: **R1 revised**. Preserve Manbo's current Workshop/UniqueID and migrate it to a data-only 1.0.0 package as the real-player Canary for JSON/WAV loading, suppression/fail-open behavior, disable recovery, long-run behavior, old-Runtime blocking and Workshop update continuity. Only after that evidence is collected is Manbo retained as a small content product or retired with a release notice. `IAudioReplacementApi` still follows the complete I1 freeze/warning/consumer-scan/breaking-version retirement lifecycle; new author guidance and templates recommend JSON only.

## S - Short-SFX Contract Versus Playback Backend

| Option | Meaning | Trade-off |
| --- | --- | --- |
| S1 - layered maturity | Reviewed schema/validator is formal/StableCandidate; verified product categories are ProtectedCurrent; current Windows WAV backend remains narrow Experimental; arbitrary audio unsupported. | Truthful author route without false mixer claims. |
| S2 - nothing formal until Wwise | Keep even schema/product behavior Experimental until native mixer playback works. | Stronger fidelity gate, understates existing products. |
| S3 - current backend is stable audio replacement | Treat successful SoundPlayer WAV as broad completion. | Incorrect: no game mixer/volume/pause/3D/callback semantics. |

Final decision: **S1**. `volume` is not a formal v1 semantic while the current backend ignores it. A stable content declaration does not imply that every category or the playback backend is stable, and an Experimental backend does not prevent a reviewed narrow declaration from becoming the recommended author route.

## T - BGM Boundary

Focused review: `docs/reviews/api/2026/20260713-0004-bgm-native-lifecycle-project-boundary-review.md`.

| Option | Meaning | Trade-off |
| --- | --- | --- |
| T0 - no current commitment; independent future project | No 0.5.5 BGM schema/API/product; run discovery, custom-bank and music-state probes only under a separately chartered project. | No premature promise; future research does not create a product commitment. |
| T2 - add BGM category now | Add `BgmMusic` to current JSON before callback/STOP/bank/transition contract. | Schema freezes ahead of backend. |
| T3 - arbitrary event/audio API | Let authors replace any Wwise/local event now. | Unsafe native/state/resource exposure and cleanup. |

Final decision: **T0**. Do not add BGM SoundKeys, JSON fields, C# APIs or first-party products, and do not generalize short-SFX evidence to music. Any future project begins again from discovery, custom-bank proof, loop/STOP/callback/pause/scene/volume/owner cleanup and lifecycle evidence.

## Recommended Answer Set

```text
O = O1  four content routes; Shell Crab unpublished; Lightning retired research
P = P2  custom products processed by official JSON into native resources
Q = Q1  declarative reviewed short-SFX author route
R = R1  data-only Manbo migration Canary; C# API follows I1 retirement
S = S1  formal narrow contract, protected products, Experimental backend
T = T0  no current BGM promise; any future work is separately chartered
```

## Work-Order Effect

```text
Batch 0  freeze Catalog product identity and existing animal/audio/API compatibility identities;
         defer new AnimalPack item/recipe/equipment/economy ids to its rebuild
Batch 3A Author SDK schemas/validators, asset provenance, migration and B1 reload
Batch 4  lifecycle/demand-driven CustomAnimals and AudioReplacement hot paths
Batch 5  data-only Manbo Canary; animal official-schema proof and product/economy work
Batch 6  C# audio API warning/consumer/removal governance
Future   Shell Crab advanced-route proof; separately chartered BGM research if approved
```

No public animal/Manbo/BGM package can ship from this docket alone. Each needs its own implementation Update, validation, asset/license gate, compatibility/migration evidence, and player/runtime release approval.

## Validation Boundary

This docket cross-checked the fourth-round feedback, current local animal packages and documented prototype sources, current Manbo/audio implementation, public API matrix, native animal save/content owners, current Wwise/BGM reverse-build owners, and related audio/animal reviews. It did not write local MODS/save state, copy external assets, launch the game, load a Wwise bank, or change/publicize any package.
