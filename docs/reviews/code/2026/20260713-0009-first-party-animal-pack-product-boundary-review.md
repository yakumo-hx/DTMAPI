# 20260713-0009 First-Party Animal Pack Product Boundary Review

Status: recorded / fifth-round decisions closed / public-assets blocked
Date: 2026-07-13
Scope: unified first-party animal content product, package/save migration, prototype coexistence, economy baseline, and public asset provenance
Related decision docket: `docs/reviews/code/2026/20260713-0010-major-update-fifth-decision-docket.md`
Related Update: `docs/updates/2026/20260713-0004-fourth-round-closure-fifth-decision-docket.md`
Closure Update: `docs/updates/2026/20260713-0005-fifth-round-closure-sixth-decision-docket.md`
Deferred design draft: `docs/reviews/code/2026/20260713-0011-major-update-sixth-decision-docket.md`
Scope reclassification: `docs/updates/2026/20260713-0006-sixth-round-scope-reclassification.md`
Related API review: `docs/reviews/api/2026/20260705-0003-custom-animal-content-boundary-review.md`

## Source Request

The user wants the DTMAPI animals other than Shell Crab merged into one product, with a deliberate economy/produce system and improved author documentation. E1 already selected official JSON plus PNG/WAV husbandry animals as the formal route. This review determines what the first unified product can truthfully contain.

This is a read-only product/source/local-state review. It does not copy assets, edit prototype/local packages, change save data, install/enable/disable a Mod, or publish a Workshop item.

## Current Technical Routes

| Package | Current DTMAPI identity | Runtime shape | Evidence/boundary |
| --- | --- | --- | --- |
| Hatch | `DTMAPI.HatchAssets` | pure ContentPack; `pngSpriteOverride`; 44 PNG, 2 WAV | proven main route |
| Mole | `DTMAPI.MoleAssets` | pure ContentPack; `pngSpriteOverride`; 45 PNG, 2 WAV | proven main route |
| Drecko | `DTMAPI.DreckoAssets` | pure ContentPack; `pngSpriteOverride`; 36 PNG, 2 WAV | proven main route |
| Oilfloater | `DTMAPI.OilfloaterAssets` | pure ContentPack; `pngSpriteOverride`; 48 PNG, 2 WAV | main route, but document JSON retains an older field shape |
| Shell Crab | `DTMAPI.ShellCrabMod` | ContentPack with AssetBundle animator route | advanced separate product; AssetBundle unload is not proven |
| Lightning Chicken | historical `DTMAPI.LightningChickenMod` | retired CodeMod/`runtimeOverrideController` research; worktree product removed per user feedback | never-publish failure/native-owner evidence only |

An additional external `PuftAssets` prototype has no installed product or formal evidence record and cannot silently enter the roster.

The first shared technical product uses one content architecture. Combining the four pure JSON+PNG+WAV packages does not justify importing the Shell Crab AssetBundle lifecycle. Lightning Chicken is no longer a delayed product input.

## O - First Unified Product Scope

Final decision: **O1 revised**.

Create one new first-party ContentPack product containing Hatch, Mole, Drecko, and Oilfloater. Prefer official JSON and its composition/extension forms. Use `Content/DTMAPI` declarations only for PNG/WAV/template-animation/native-adaptation gaps the official content system cannot express.

```text
UnifiedAnimalPack/
|- Content/
|  |- official-json/
|  `- DTMAPI/
|- Animals/
|  |- Hatch/
|  |- Mole/
|  |- Drecko/
|  `- Oilfloater/
`- Audio/
```

Analysis immediately following O: physical species isolation makes product validation, economy ownership and asset replacement auditable without turning each species back into a separately versioned Mod. Species, item, spawn, produce, sprite and sound identities do not depend on concrete PNG/WAV filenames, so an art replacement does not silently become a save/content migration.

Shell Crab remains `Prototype / Unpublished / AdvancedAssetBundle` and does not enter this product. Lightning Chicken is `RetiredResearch / NeverPublish`; retain audit, failed route, store/purchase/release evidence and native-owner research only, with no future product commitment.

PNG/WAV assets remain physically and procedurally separate from JSON/code. The product records source status and hashes, excludes those assets from Author SDK/templates/general tutorials, and supports a whole-asset-set replacement without changing frozen content ids.

## Product Identity And Save Compatibility

The unified product needs a new Catalog/UniqueID and must not impersonate one of its four inputs. Provisional shape:

```text
Version: 1.0.0
Type: ContentPack
EntryDll: none
UniqueID: new first-party AnimalPack identity, frozen by Batch 0 Catalog
```

Existing native saves identify animals through `protoName` and resolve that value against the registered `TbAnimal` row. When migration is intended to preserve existing prototype saves, the new product must preserve or explicitly migrate all content identities, including:

- species `hatch`, `mole`, `drecko`, `oilfloater`;
- animal bags `sack_hatch`, `sack_mole`, `sack_drecko`, `sack_oilfloater`;
- produce LUTs `hatch_produce`, `mole_produce`, `drecko_produce`, `oilfloater_produce`;
- animator keys, document ids, sound replacement ids, sprite prefixes, and referenced store/item ids.

There can be no migration window in which all old definitions are disabled, the new definitions are absent, and the player loads/saves a world containing those animals. If asset/character provenance work requires new visible names or ids before public release, that becomes an explicit local-prototype save migration rather than silent renaming.

## Old-Package Coexistence

| State | Required result |
| --- | --- |
| new pack enabled; old four absent | normal |
| new pack enabled; old copies present but disabled | `[MIGRATION]` informational state |
| new pack plus any old input enabled | `[BLOCKED] Duplicate animal content identity` before safe play |
| only old inputs enabled | Legacy state with migration guidance |

“New pack wins” is not safe. Official animal/item/store/produce JSON may already have collided before DTMAPI bridge duplicate warnings run, and directory enumeration order is not a compatibility contract.

Author SDK/Doctor must preflight the state. A local development migration is a backed-up, game-exited atomic switch: validate the new pack, enable it while disabling all old inputs, test the third save and save/reload, then archive verified old directories. Player Runtime never auto-deletes old packages; receipt cleanup remains Author SDK/internal tooling.

## Public Asset Provenance Gate

Current local evidence says:

- Mole, Drecko, and Oilfloater frame metadata identifies ONI-derived frames;
- Shell Crab documentation identifies development-only ONI art/audio sources;
- Hatch lacks a complete per-asset provenance and redistribution record.

The current files are therefore private QA/technical prototypes, not public 1.0.0 release inputs, until every shipped PNG/WAV/name/design is original or has explicit cross-project redistribution permission. Attribution alone is not treated as permission.

Klei's own UGC guideline says third-party work requires credited permission, copyrighted material requires ownership/permission, missing explicit permission is treated as no permission, and assets from other games/media are not allowed in Klei Mods. That policy does not directly govern Doloc Town Workshop, but it also cannot be used as a grant to redistribute Klei assets in another game: [Klei General Mod and UGC Guidelines](https://support.klei.com/hc/en-us/articles/360029556052-General-Mod-and-UGC-Guidelines).

The public package needs a generated asset inventory containing path, author, source, license/permission, modification and cross-project redistribution scope, evidence link, and SHA-256. The release gate may pass through original replacement assets or documented permission; “credit only” is rejected.

## Current Economy Is Prototype Data

| Animal/template | Current bag price | Template bag price | Current output sale value | Template output value |
| --- | ---: | ---: | ---: | ---: |
| Hatch / chicken | 800 | 800 | meat x1 = 60 | egg x1 = 75 |
| Mole / marsh pangolin | 800 | 5000 | meat + soil = 65 | milk = 250 |
| Drecko / goat | 800 | 3000 | wool + meat = 560 | wool = 500 |
| Oilfloater / slime | 800 | 1200 | coal x1 = 10 | honey = 100 |

The common 800 price and very different output ratios are useful test fixtures, not one coherent economy. Exact value also depends on growth, space, feeding/energy, mood, production interval, breeding/sale, and equipment capacity.

## P - Animal 1.0.0 Economy

Final decision: **P2 revised**, a new user-defined meaning that supersedes the proposal's old P2.

Every primary and hidden animal output is a custom item. Official JSON then processes or crafts those products into fixed quantities of native resources, which re-enter the native economy and recipes:

```text
animal
-> custom primary or hidden product
-> official-JSON processing/crafting
-> fixed native items
-> native economy and downstream recipes
```

Analysis immediately following P: this avoids forcing raw native items to express animal-specific identity while preventing GameBridge from becoming the owner of a species economy. Official JSON should own custom item definitions, production tables, probability LUTs, hidden products, processing/crafting, stores, technology and text wherever possible. GameBridge owns only reviewed native adaptation that the official content system cannot express; it stores no species probability, yield or recipe table.

Each species design must define:

```text
primary product pool
+ hidden product
+ processing result
+ direct recipe purpose
+ economy target
```

Hatch begins with `hatch_egg` and `mutated_hatch_egg` in its primary pool plus `giant_hatch_egg` as the intended hidden product. Ordinary, mutated and giant eggs receive distinct fixed processing results during the economy rebuild. Before implementation, review the native JSON owners and schemas for candidate-weight LUTs, an independent hidden-product path, and batch/multiple-result processing. If the independent hidden route is unavailable, put the giant egg into the ordinary official LUT at a low weight rather than adding a Hatch-specific GameBridge Hook.

Mole, Drecko and Oilfloater product identities, hidden-product roles, processing results and direct recipe uses remain AnimalPack rebuild decisions, not DTMAPI mainline sixth-round questions. Exact quantities and weights follow the later product-owned economy table, simulation and manual regression.

The selected tuning goal is deliberately above vanilla husbandry: stable return around 2-3 times the comparison and long-run return including hidden products around 3-5 times, while forming a meaningful choice against crops and fish. The economy table must include bag cost, growth/breeding, feed/energy/mood, interval/probability, fixed processed native value, direct recipes, equipment capacity, occupied-space daily return and payback.

Custom product sale price, processed value and processing cost must be designed together so players cannot obtain a free `sell custom item + process the same value` arbitrage. A later product decision may make an item processing-only, directly usable, sellable, or some controlled combination; GameBridge does not decide that policy.

## Pre-Implementation Gaps

- establish a repository-owned canonical source; the external Hatch source and LocalLow installed package already drift in five metadata/content files, and LocalLow cannot be the source of truth;
- update Oilfloater's older document fields to the current official document schema and validate it;
- verify the native owners and official schema for weighted primary candidates, an independent hidden product, and fixed multi-result/batch processing before freezing a content contract;
- close the four-species product-identity/use decisions during the AnimalPack rebuild before freezing new item/recipe ids; do not freeze exact probabilities or yields before an economy model and test band exist;
- create the asset/provenance inventory before any public staging;
- freeze new pack/product identity and explicit prototype-save migration in the Catalog;
- validate old/new duplicate detection before native content ingestion where possible, and fail visibly when that cannot be guaranteed.
