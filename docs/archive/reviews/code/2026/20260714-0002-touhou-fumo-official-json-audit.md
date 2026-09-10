# 20260714-0002 Touhou Fumo Official JSON Mod Audit

Status: recorded
Date: 2026-07-14
Scope: read-only audit of a third-party pure-JSON Doloc Town Mod against the current local official example, official authoring documentation, and the current public-build configuration loader
Related Update: `docs/updates/2026/20260714-0005-touhou-fumo-official-json-audit.md`
Author-Tool Regression Follow-Up: `docs/reviews/code/2026/20260715-0004-touhou-fumo-author-tool-regression.md`

## Source Request And Boundary

The user supplied `D:\下载\touhou_fumo.zip` and asked for an official-JSON review without an existing failure report. The user explicitly classified it as a third-party pure-JSON Mod unrelated to DTMAPI.

Artifact identity:

- file: `D:\下载\touhou_fumo.zip`
- size: 30,051 bytes
- SHA-256: `BE7346EC8AEB00B3915CB637EEF3C0F55536FF8338E99F4AC6610DDDA6F0EC7D`

This review did not modify or copy the archive, install it, enable it, launch the game, or involve DTMAPI loading behavior. The authoritative comparison inputs were:

- the locally installed official example Mod `3705665433`, whose `info.json` identifies version `0.96.06`;
- [创建你的模组](../../../../../references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/003_%E5%88%9B%E5%BB%BA%E4%BD%A0%E7%9A%84%E6%A8%A1%E7%BB%84_Df5cwl6u2irIogkazI5crevfnWf.md);
- [新增装饰设备](../../../../../references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/048_03%E6%96%B0%E5%A2%9E%E8%A3%85%E9%A5%B0%E8%AE%BE%E5%A4%87_PSUjwr1GuiRixsk95HlcYTVQnPd.md);
- [新增商店道具](../../../../../references/doloc-town/official-workshop-docs/feishu-crawl-20260517/md/045_05%E6%96%B0%E5%A2%9E%E5%95%86%E5%BA%97%E9%81%93%E5%85%B7_CKj3wewhLiTuCkk2GrqceORunUg.md);
- the local reverse reference for public build `23762374`, used only to confirm current filename matching, table resolution, and store-extension behavior.

## Executive Verdict

The package structure and all four JSON texts are syntactically valid, but the Mod has one definite functional defect:

> `Content/Equipment/item_tbtiem.json` misspells the required table filename `item_tbitem.json`.

The current official loader indexes content by the JSON filename without its extension and only asks for known table names. `item_tbtiem` is not a known table name, so this file is collected into the Mod cache but is never merged into `item_tbitem`. This explains why the typo can produce no parse or loading error.

The downstream effect is larger than a harmless filename typo:

1. `equipment_tbequipment.json` can still contribute the equipment record.
2. The matching item record `reimufumo` is never registered.
3. The `mod_tbmodstoreextension.json` record resolves `item_name: reimufumo` to no item.
4. The current official store-extension handler skips an extra item whose resolved item reference is null.

Therefore the archive can be recognized and enabled without an obvious error while the intended item and shop acquisition route do not become functional.

After correcting that filename, the reviewed item, equipment, and shop records have the required fields and compatible JSON value types shown by the official example/current constructors. No second schema-blocking problem was found statically.

## Findings

### F1 — Definite functional defect: unknown table filename

Severity: blocking for the intended item

Current path:

`touhou_fumo/Content/Equipment/item_tbtiem.json`

Required path:

`touhou_fumo/Content/Equipment/item_tbitem.json`

This should be a filename-only correction. The JSON body itself matches the current `item_tbitem` record shape.

### F2 — Content semantics: the handbook source says processing, but no processing route exists

Severity: non-blocking metadata/content mismatch

The item declares `source: ["processing"]`, which the official table displays as “加工制造”. The package contains no `recipe_tbrecipe.json` or `mod_tbmodrecipegroupextension.json`; its actual acquisition route is the Hult shop extension.

The `source` field is descriptive handbook metadata and does not create a recipe. If this Mod is intentionally shop-only, use an empty source list, as the current official decoration example does, or deliberately use the existing `swap`/“交易” source if that wording is desired. If crafting is intended, add the official recipe and recipe-group records instead of relying on `source`.

### F3 — Publication metadata: template placeholder and short version form

Severity: non-blocking before local testing; should be cleaned before Workshop publication

- `tags` still contains the official template placeholder `"..."`. The official uploader forwards the array to Steam; the placeholder should be removed or replaced with intentional Workshop tags.
- `version: "0.1"` is accepted by the current official manifest because the field is only a string. It is not a syntax error. The official example convention is a three-part value such as `0.1.0` or `1.0.0`, so normalizing it would make future releases clearer.
- Empty Traditional Chinese and English localization values are allowed. The default Chinese name/description remain fallback text; missing translations are a reach/quality issue, not a load failure.

## Confirmed Valid Or Non-Issues

- The archive has one Mod root containing `info.json`, `icon.png`, `preview.png`, and `Content/`.
- All four JSON files parse successfully. Content-table roots are arrays with one record each; `info.json` is an object.
- `equipment_tbequipment.json` and `mod_tbmodstoreextension.json` are recognized current table filenames.
- Item ID, equipment ID, shop item reference, sprite references, and title/description key stems consistently use `reimufumo`.
- `sub_type: equipment_ornament`, `ItemFunctionEquipment`, and `EquipmentFuncDecorator` match the official decoration example.
- Required equipment fields are present. Omitting `electronic_component` is supported and means this decoration is not electrical.
- `hult_shop` is a valid store ID in the current public build. Although the official authoring page demonstrates `phone_booth_shop`, the current resolver/handler supports an extension targeting this existing ordinary store; this is not an invalid ID or JSON-rule violation.
- Arbitrary subdirectories under `Content/`, including `Equipment/Texture` and `Shop/Hult`, are allowed. Loading keys come from filenames, not these directory names.
- `storage: 0`, four seasonal entries, `min_count <= max_count`, and `spawn_weight: 0` are valid. As in the official documentation, store changes may require the next store refresh/day before becoming visible.
- All referenced PNG assets exist and decode: item icon `28x28`, both equipment sprites `26x24`, root icon `32x32`, and preview `453x393`. The root icon exactly matches the official recommended size; preview resolution is unrestricted.
- The two equipment sprites have identical dimensions and the `_flip` image is an exact horizontal pixel mirror of the normal image.
- A `2x2` placement footprint is not required to equal the full visible sprite bounds. The official example also allows sprite overhang, so the `26x24` scene image is not by itself a rule violation.
- A recipe is not required if the design is intentionally shop-only. No recipe-related JSON should be added merely to imitate the comprehensive official example.
- A local Mod does not need `workshop.json` before it has a Workshop identity.
- No DLL, BepInEx placement, DTMAPI manifest, or DTMAPI dependency exists in this package, which is correct for the stated pure official-JSON scope.

## Recommended Author Fix Order

1. Rename `item_tbtiem.json` to `item_tbitem.json`.
2. Decide whether acquisition is shop-only or shop plus crafting; align `source` and add recipe files only if crafting is intended.
3. Before publication, replace the `"..."` tag, preferably normalize `0.1` to `0.1.0`, and add translations if desired.

The Hult shop ID, subfolder layout, texture filenames, item/equipment record bodies, prices, stack limit, and footprint do not need to be changed to satisfy the reviewed official JSON rules.

## Acceptance Gates

Static gate after the author rebuilds the archive:

- every JSON parses;
- every Content JSON basename is a known current table name;
- `item_tbitem`, `equipment_tbequipment`, and `mod_tbmodstoreextension` all contain the same intended ID/reference;
- every referenced sprite basename exists exactly once.

Runtime gate, not run by this review:

- start a fresh game process with only the corrected Mod enabled;
- confirm no Mod config validation/load error;
- after the required shop refresh, confirm `reimufumo` appears in Hult's shop;
- buy it, place it, rotate/flip it, remove it, and confirm the icon/title/description and `2x2` collision footprint;
- restart once and confirm the placed decoration and store entry remain valid.

## Validation And Limits

Completed read-only checks:

- archive enumeration and SHA-256 identity;
- parse of all four JSON documents;
- table-basename comparison against current public-build content tables;
- field/value-type comparison with the current official example and generated table constructors;
- current `hult_shop` base-table and store-extension resolution review;
- referenced-asset existence, PNG decoding, dimensions, alpha bounds, and exact flip comparison.

No game/runtime test was run, so this review proves the static defect and static compatibility of the corrected record shape; it does not claim player-visible verification.
