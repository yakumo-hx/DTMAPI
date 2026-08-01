# Official JSON Tool Audit Documentation Regression

Status: recorded

Date: 2026-07-15

Scope: regress the findings in `20260714-0003-third-party-official-json-authoring-tool-audit.md` against the complete 2026-07-15 browser-rendered snapshot of the official Doloc Town Workshop authoring guide

Related Update: `docs/updates/2026/20260715-0004-official-json-tool-doc-regression.md`

Implementation Resolution: `docs/updates/2026/20260715-0005-official-json-tool-crop-month-fix-and-handoff.md`

## Source Request And Authority

The user asked to compare the newly refreshed official JSON documentation with the earlier third-party authoring-tool audit and confirm whether the audit itself was wrong.

The live Feishu Wiki remains canonical. This regression used the local dated snapshot routed by `references/doloc-town/official-workshop-docs/README.md`:

- snapshot: `references/doloc-town/official-workshop-docs/feishu-crawl-20260715`;
- manifest state: complete;
- pages: 55, with zero page failures and an empty remaining queue;
- code blocks: 86/86 complete;
- embedded sheets: 107/107 captured as TSV plus screenshot;
- rendered document images: 138/138 captured;
- comparison with 2026-05-17: no added or removed Wiki tokens, with 19 later visible source-modified labels.

The relevant official pages carry visible modified labels from April 14 through June 21, all before the original tool audit on July 14. The earlier May snapshot already contained the official `$type`, all-lowercase exchange filename, item/equipment ID-equality, and two-table crop requirements. The new snapshot makes those records easier to audit and, importantly, completely captures the previously truncated crop code.

## Verdict

No original finding is retracted. The blocking schema findings for the upstream v0.6 tool are confirmed by current official documentation, independently of the decompiled-build and installed-example evidence used in the first audit.

Two qualifications must be carried forward:

1. The user-approved fork design may keep seed and harvest items in separate “新增道具” projects, but the exported Mod as a whole must still contain both `item_tbitem.json` and `plant_tbseed.json`, with matching cross-references. This narrows how F5 should be explained; it does not make upstream v0.6 complete.
2. The authorized fork currently rejects an empty `growth_months`, while the official crop page explicitly says an empty array means no month restriction. This is a fork preflight defect, not an error in the original audit, whose F8 required valid month values but did not require a nonempty list.

## Finding-By-Finding Regression

| Original finding | Current official-document evidence | Result |
| --- | --- | --- |
| F1 imported-text `innerHTML` injection | The official JSON guide does not define browser-tool rendering security. The v0.6 source evidence remains controlling. | unchanged; not an official-schema question |
| F2 remote libraries without integrity/offline boundary | The official JSON guide does not define the helper's dependency boundary. The v0.6 HTML remains controlling. | unchanged; not an official-schema question |
| F3 wrong polymorphic discriminators and missing subtype payloads | The hat page uses `function.$type = ItemFunctionHat` plus `hat_id`; the equipment page uses `function.$type = ItemFunctionEquipment` and equipment `function.$type = EquipmentFuncDecorator`; crop, cooking, and wallpaper/exterior examples use `$type` plus their required subtype fields. | confirmed |
| F4 exchange-store basename case | The store page and composite hat case both name exactly `mod_tbmodexchangestoreextension.json`, all lowercase. No camel-case variant appears in the current structured snapshot. | confirmed |
| F5 incomplete crop path | The crop page says a crop Mod consists of `item_tbitem.json` and `plant_tbseed.json`, both required under `Content`; it defines separate seed and harvest item records and a plant record. | confirmed, with the separate-project qualification above |
| F6 destructive import/re-export | Official JSON documentation does not promise lossless behavior for this third-party browser tool. The v0.6 import/export source remains controlling. | unchanged; not an official-schema question |
| F7 stale/misspelled IDs | Current official TSVs contain `equipment_gather`, `equipment_industry_synthesizer`, source `npc` for 礼物, `assemble_molding_machine`, and `resin_compacting_machine`; none of the corresponding stale tool spellings appears. The exterior function example uses `ItemFunctionBuildingExterior`. | confirmed for documented tables; item-map corrections still rely on current native/sample data because the official item-ID sheet is intentionally partial |
| F8 missing package/semantic validation | “创建你的模组” requires `preview.png`, `icon.png`, `info.json`, and `Content`; the category pages define required paired records, fields, references, and assets. | confirmed in scope; the empty-month rule below corrects the fork, not F8 |

## Direct Official Evidence

### Polymorphic Functions And ID Equality

- `pages/PxaIwKgsPiua7okPqgdcoCOZnkF/code-blocks/01-code.jsonc` uses `"$type": "ItemFunctionHat"` and `"hat_id": "hat0"`.
- `pages/PxaIwKgsPiua7okPqgdcoCOZnkF/code-blocks/02-code.jsonc` says the hat ID is the same as the item ID.
- `pages/PSUjwr1GuiRixsk95HlcYTVQnPd/code-blocks/01-code.jsonc` uses `"$type": "ItemFunctionEquipment"`.
- `pages/PSUjwr1GuiRixsk95HlcYTVQnPd/code-blocks/02-code.jsonc` says the equipment ID is the same as the item ID and uses `"$type": "EquipmentFuncDecorator"`.
- `pages/LO6uwLfSXilCHLkAymLclh1jncd/code-blocks/01-code.jsonc` defines `ItemFunctionSeed.seed_id` and `ItemFunctionCrop.seed_item/eating_effect`.
- `pages/KaKyw74DnibQhmkjPlMcJgZXnBf/code-blocks/01-code.jsonc` defines `ItemFunctionFood.eating_effect`.
- `pages/YHhawESrpidQRikIjWJczOLLnrh/code-blocks/01-code.jsonc` defines `ItemFunctionBuildingExterior.exterior_id`.
- `pages/YHhawESrpidQRikIjWJczOLLnrh/code-blocks/04-code.jsonc` defines `ItemFunctionWallpaper.wallpaper_id`.

These records directly disprove upstream v0.6's `function: { type: ... }` output and its misspelled exterior function type.

### Exact Exchange Basename

`pages/CKj3wewhLiTuCkk2GrqceORunUg/content.md` identifies the exchange table twice as `mod_tbmodexchangestoreextension.json`. The same all-lowercase spelling appears in the composite hat example. The current snapshot contains eight structured-text occurrences of the lowercase name and zero occurrences of upstream v0.6's `mod_tbmodexchangeStoreextension` spelling.

### Crop Responsibility

`pages/LO6uwLfSXilCHLkAymLclh1jncd/content.md` says the crop Mod requires both:

- `item_tbitem.json`, containing separate seed and harvest item records;
- `plant_tbseed.json`, containing the plant definition and stage assets.

This confirms that a single upstream v0.6 crop project was not a complete one-click crop path. The authorized fork's explicit split is valid only when its separate item projects and plant project are exported into the same Mod and agree on `seed_id`, `seed_item`, crop output, and sprite references.

The same page says `growth_months` may be empty to mean unrestricted growth. The authorized fork currently emits the correct empty array on import/export but its validator requires at least one value. That validator must be relaxed to: empty is valid; otherwise every entry must be an integer from 1 through 4.

### Current Documented IDs

Exact TSV-cell comparison found:

| Upstream v0.6 value | Official current value | Official table |
| --- | --- | --- |
| `equipment_cather` | `equipment_gather` | 道具 / 子道具类型 |
| `equipment_industry_synthesize` | `equipment_industry_synthesizer` | 道具 / 子道具类型 |
| source `gift` | source `npc` | 道具 / 获取途径 |
| `assemble_molding_machir` | `assemble_molding_machine` | 配方 / 配方组 |
| `resin_compacting_machin` | `resin_compacting_machine` | 配方 / 配方组 |
| `ItemFunctionBuildingExteric` | `ItemFunctionBuildingExterior` | 道具 / 功能类型 and comprehensive example |

The current `04 ID对照表（道具）` item-ID subsection lists only tools and drone frames, so it cannot independently prove or disprove the original audit's corrections for crop, food, fish, animal-product, or other item IDs. Those corrections remain based on the current public-build tables and official sample. Absence from this partial official sheet is not evidence that upstream v0.6 was correct.

## Official-Documentation Inconsistencies Found

The official snapshot is authoritative guidance but is not internally perfect:

- the function-type TSV says `ItemFunctionWallpaper` has `exterior_id`, while the official comprehensive wallpaper JSON correctly uses `wallpaper_id`; the original audit selected the JSON/runtime-compatible field;
- the crop item's `sub_type` is correctly `farm_crop`, but its nearby comment says `farm_seed`;
- the item-ID sheet is partial and must not be treated as a complete item registry.

These inconsistencies explain why the original review cross-checked official examples, the installed official sample, and current native owners rather than accepting one prose cell in isolation.

## Validation And Limits

Completed read-only checks:

- verified the refreshed snapshot routing, report, manifest completeness, page inventory, and source-modified labels;
- compared the relevant current official code blocks, page text, and exact TSV cells with upstream v0.6 values and the original F1-F8 findings;
- checked the prior May snapshot for the key `$type`, exchange filename, ID-equality, and crop two-table statements;
- inspected the authorized fork validator and confirmed its current nonempty-month check.

No official snapshot, Workshop subscription, game directory, runtime package, or generated Mod was changed. No game run was needed: this regression establishes documentation/schema consistency, not player-visible behavior.

## Required Follow-Up

Before handing the authorized fork to the author as a release candidate:

1. allow an empty crop month list and retain integer/range validation only for supplied values;
2. add a browser regression proving empty `growth_months: []` passes preflight and survives export/import;
3. keep the UI wording explicit that seed and harvest items are separate “新增道具” projects but are jointly required in the exported Mod;
4. rerun the existing browser rendering and ZIP assertions after the validator change.
