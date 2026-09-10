# 20260714-0003 Third-Party Official JSON Authoring Tool Audit

Status: recorded
Date: 2026-07-14
Scope: read-only source, security-boundary, import/export, and current-official-schema audit of third-party Workshop item `3755367789`
Related Update: `docs/updates/2026/20260714-0006-third-party-official-json-authoring-tool-audit.md`
Authorized Local-Fork Follow-Up: `docs/updates/2026/20260715-0002-authorized-official-json-tool-fork.md`
Official-Documentation Regression: `docs/reviews/code/2026/20260715-0003-official-json-tool-doc-regression.md`

## Source Request And Boundary

The user asked for a quick review of a third-party helper for authoring official Doloc Town JSON Mods. The inspected local Workshop copy was:

`D:\Steam\steamapps\workshop\content\2285550\3755367789`

Its four files were inspected read-only:

| File | Bytes | SHA-256 |
| --- | ---: | --- |
| `Content/模组工具 v0.6.html` | 90,630 | `AAAA80D505C7414A7E8C7C6497D4BC0B15856C0DBDFAA5926C068E747A2FB07B` |
| `info.json` | 419 | `D88C456A0C1E883D9C1C4508E33934F6432356F07079CBCB16F538266E076E32` |
| `icon.png` | 479 | `F06EDA68AFB3A2E4BCAFFCD394C01A9A7852B640E1D6C7FEC4CF3780F06828B0` |
| `preview.png` | 111,598 | `27CF116670FF0CB996137E0346978E6F3BC76E836F579CE6B8F07A83F9990907` |

The HTML was not opened or executed as a page. No remote script was downloaded, no Mod ZIP was imported or generated, no game process was launched, and no Workshop/game file was modified.

## Screenshot Transcription

The supplied Workshop description says:

- this is a Mod creation tool and should not be loaded in the game;
- it provides a visual editor, project management, JSON/file assembly, and one-click ZIP export;
- version 0.6 supports new items, static hats, decorations/equipment, recipes, store items, and crops;
- the user should browse to Workshop item `3755367789`, open `Content/模组工具.html` in a browser, and use a browser to run it;
- the author considers it beta-quality and asks users to report bugs and suggestions.

The installed filename is currently `模组工具 v0.6.html`, not the unversioned filename shown in the description.

## Executive Verdict

This is a browser-side HTML tool, not a game-loaded code Mod. Merely subscribing to or enabling the Workshop item does not execute the HTML; the official game scans its `Content` for supported JSON/PNG resources and does not execute this `.html` file.

No obvious malicious or deliberately obfuscated behavior was found in the local inline source:

- no `fetch`, `XMLHttpRequest`, WebSocket, beacon, clipboard, shell/ActiveX, `eval`, or dynamic `Function` use was found;
- selected files are handled through browser file inputs and JSZip objects;
- the inline JavaScript passes a syntax-only parse.

However, the tool is not currently safe to treat as a production official-JSON editor, validator, or lossless round-trip utility. It has:

- an imported-data HTML-injection boundary;
- unverified remote JavaScript dependencies despite its local-processing claim;
- blocking export defects in four of the six advertised categories;
- destructive import/re-export behavior;
- stale or misspelled official IDs;
- almost no semantic or required-file validation.

The present practical boundary is: use it, at most, as a draft form for a simple plain item, a manually checked recipe, or a normal shop extension. Do not open an untrusted ZIP in it, and do not overwrite an existing Mod with its re-export.

## Supported-Category Verdict

| Advertised category | Static verdict | Main reason |
| --- | --- | --- |
| New item | partial | A plain `ItemFunction` record has the correct shape, but several subtype/source/function choices are invalid or lack required function fields. |
| Static hat | blocked | The generated item function writes `type` instead of the required `$type`, so current official validation rejects the item table. |
| Decoration/equipment | blocked | Both generated function objects write `type` instead of `$type`; the UI also permits item ID and equipment ID to diverge even though `ItemFunctionEquipment` has no separate equipment-ID field. |
| Recipe | partial | The basic record shape is correct, but two listed recipe-group IDs are misspelled and item/count/range references are not validated. |
| Normal shop item | mostly usable with review | The generated table shape is current, and the listed normal-store aliases are supported by the current handler; referenced item existence and numeric ranges remain unchecked. |
| Exchange shop item | blocked | Import and export use `mod_tbmodexchangeStoreextension.json`; the official/current basename is all-lowercase `mod_tbmodexchangestoreextension.json`. The official cache lookup is case-sensitive at this boundary, so the generated file is silently unused. |
| New crop | blocked/incomplete | The type declaration promises `item_tbitem.json` plus `plant_tbseed.json`, but export writes only the plant table. It creates neither the seed item nor harvest item and, by default, can emit one stage referencing a sprite that was never supplied. |

## Findings

### F1 — High: imported or entered text is inserted into `innerHTML` without escaping

The tool builds project cards and edit forms by interpolating project names, IDs, descriptions, imported JSON text, stage data, and file-derived values into HTML strings assigned to `innerHTML`.

A crafted imported ZIP can therefore place markup/event attributes in an item name or another imported field and execute JavaScript when the project list or edit form is rendered. The same class of bug can be triggered by manually entered text. The executed code would share the page context containing the current projects and selected file objects and could initiate network requests.

Required correction:

- render user/imported text with `textContent` and DOM property assignment;
- never interpolate untrusted values into markup or inline event handlers;
- if templating is retained, apply context-correct HTML and attribute escaping before every insertion.

Until then, do not import third-party/untrusted ZIP files.

### F2 — Medium: the page executes remote libraries without integrity controls

The HTML loads:

- JSZip `3.10.1` from `cdnjs.cloudflare.com`;
- FileSaver.js `2.0.5` from `cdnjs.cloudflare.com`.

There is no Subresource Integrity hash, Content Security Policy, or locally bundled copy. The local inline source contains no direct upload path, but the two remote scripts execute in the same page and can access selected files and form data. Therefore the footer claim that all data is local is broader than the static evidence supports. The tool also does not work fully offline unless those resources happen to be cached.

Preferred correction: bundle reviewed, pinned library files inside the Workshop item and add a restrictive CSP. At minimum, add correct SRI/cross-origin attributes and document the network dependency.

### F3 — Blocking: hat and equipment polymorphic discriminators are wrong

Official generated table classes select polymorphic functions exclusively through `function["$type"]`.

The tool emits:

- static-hat item function with `type: "ItemFunctionHat"`;
- equipment item function with `type: "ItemFunctionEquipment"`;
- equipment function with `type: "EquipmentFuncDecorator"`.

All three must use `$type`. Current official validation throws a serialization error and skips the affected merged config file.

The generic item form has a separate partial problem: it correctly writes `$type`, but exposes functions without collecting their required fields:

- `ItemFunctionHat` needs `hat_id`;
- `ItemFunctionCrop` needs `seed_item` and `eating_effect`;
- `ItemFunctionFood` needs `eating_effect`;
- `ItemFunctionWallpaper` needs `wallpaper_id`;
- `ItemFunctionBuildingExterior` needs `exterior_id`.

Only the generic plain `ItemFunction` path is unconditionally complete. `ItemFunctionEquipment` has no extra function field but still needs a matching equipment record; `ItemFunctionSeed` gets `seed_id` but still needs a matching plant record.

### F4 — Blocking: exchange-store table basename has incorrect case

The tool detects and emits `mod_tbmodexchangeStoreextension.json`. The official filename is `mod_tbmodexchangestoreextension.json`.

The current official Mod content cache stores the original basename and looks up the requested table name with a case-sensitive dictionary. This file is therefore not consumed, and the error can be silent. The exchange record body otherwise follows the current official example shape.

### F5 — Blocking: crop generation is not a complete “new crop” path

The exported crop project contains only `plant_tbseed.json`. The current official new-crop example also requires item records for:

- the seed item using `ItemFunctionSeed` and a matching `seed_id`;
- the harvest item using `ItemFunctionCrop`, `seed_item`, and `eating_effect`.

The UI asks for a separate “作物 ID” but uses it mainly as a folder name; it does not generate an item with that ID. A new crop therefore has no usable seed item and may reference a nonexistent harvest item.

There is also a default-stage defect. The form declares four default stages in a local variable but does not render them for a newly added crop. If the user does not manually add stages, export creates only one growth-0 stage referencing `sprite_seed_<id>_0`, while no corresponding image has been selected.

### F6 — High data-integrity risk: import/re-export is destructive

The “导入模组” function is not a lossless editor:

- only the first record of each JSON array is interpreted;
- one first-level `Content/<folder>/` directory is reduced to one inferred project type;
- composite folders containing item, equipment, recipe, and shop tables are re-exported as only the selected inferred type;
- unrecognized JSON and fields are discarded rather than preserved;
- files directly under `Content/` are ignored because the importer requires at least one subfolder;
- the correct official all-lowercase exchange-store filename is not recognized;
- root `icon.png` and `preview.png` are not imported, so re-export can drop them or accidentally retain files selected from an earlier session;
- prior project state and optional localization fields are not fully cleared before import;
- nested paths are flattened to leaf filenames inside the reconstructed project, allowing collisions and layout loss.

Do not use import → edit → export on the only copy of a Mod. Preserve the source tree and diff every generated archive.

### F7 — Current official ID drift and spelling defects

Static comparison with public build `23762374` found the following selectable values absent from their current official tables:

| Tool value | Current value/status |
| --- | --- |
| `equipment_cather` | `equipment_gather` |
| `equipment_industry_synthesize` | `equipment_industry_synthesizer` |
| source `gift` | source `npc` (“礼物”) |
| `assemble_molding_machir` | recipe group `assemble_molding_machine` |
| `resin_compacting_machin` | recipe group `resin_compacting_machine` |
| `ItemFunctionBuildingExteric` | `ItemFunctionBuildingExterior`, also requiring `exterior_id` |
| `appletree` | item `apple` |
| `faecs` | item `faeces` |
| `fridge_egg` | current game item is misspelled `fride_egg` |
| `mint` | item `mint_leaf` |
| `power_control_compartments` | item `power_control_compartment` |
| `rainbow_tout` | item `rainbow_trout` |
| `turnin` | item `turnip` |
| `leather`, `paper`, `patch` | no exact current item IDs; the intended concrete item must be selected instead |

`ITEM_ID_MAP` also declares `coffee` and `tea` twice. JavaScript keeps only the later beverage labels. Current crop-material IDs are `coffee_bean` and `tea_leaf`; current `coffee` and `tea` are drinks.

The normal-store values `kenenimuu_shop` and `seasonseed_villain_shop`, plus several exchange-store values that do not directly appear in the base tables, are not defects: the current official extension handlers explicitly map those compatibility aliases to live store IDs.

### F8 — Missing validation lets syntactically valid but unusable packages pass

The generator does not enforce:

- required Mod-level `icon.png` and `preview.png` even though the official authoring document requires both;
- nonempty, safe, unique IDs and folder names;
- duplicate record IDs across projects;
- item/equipment ID equality;
- required sprites for hats, equipment, and crop stages;
- referenced item, store, recipe-group, subtype, source, seed, crop, function, or gene existence;
- positive dimensions and sensible counts;
- `min_count <= max_count`;
- valid month values;
- a complete subtype-specific function payload.

This tool produces JSON text; it does not currently validate an official Mod.

## Confirmed Non-Issues And Useful Parts

- The Workshop package itself contains no DLL or executable and does not depend on DTMAPI/BepInEx.
- The game does not execute the HTML merely because the Workshop item is installed or enabled.
- `info.json` parses; the package icon is `32x32`; the preview is a valid `1168x1192` PNG.
- The HTML/inline JavaScript is readable rather than minified or obfuscated and passes a syntax-only parser check.
- Plain-item required fields and filename are aligned with the current official example.
- Basic recipe and normal-store object shapes are aligned with current official examples.
- All 12 recipe subtype IDs and all 22 crop-gene IDs listed by the tool exist in the current build.
- Normal and exchange store compatibility aliases listed by the tool match current explicit handler aliases.

## Safe Use Boundary For Version 0.6

If the tool must be used before its author updates it:

1. Do not import any ZIP you do not fully trust.
2. Do not use import/re-export as a preservation or migration operation.
3. Restrict draft generation to a plain item with `ItemFunction`, a recipe using a separately verified group/item set, or a normal shop extension.
4. Treat static hat, equipment, exchange shop, crop, food/crop/wallpaper/exterior function choices as blocked.
5. Manually inspect every generated basename, `$type`, required subtype field, ID reference, image reference, and root file.
6. Extract into a separate review directory, diff it against author source, and keep the original backup.

## Recommended Author Fix Order

1. Remove the `innerHTML` injection paths and bundle pinned dependencies locally with a restrictive CSP.
2. Add a generated-package validator based on exact current official table basenames, required fields, polymorphic `$type` payloads, reference IDs, ranges, and assets.
3. Fix hat/equipment `$type`, exchange basename casing, and crop item/stage generation.
4. Make import lossless by preserving unknown files/records/fields, or rename it explicitly to “limited first-record import” and refuse unsupported layouts.
5. Generate the selectable ID maps from current official tables instead of maintaining hand-copied lists.
6. Add golden tests that compare each advertised category with the current official example and reject a package containing an unknown table basename.

## Validation And Limits

Completed read-only checks:

- screenshot transcription;
- complete four-file inventory, hashes, JSON parse, and image decode/dimensions;
- HTML external-resource and browser-capability scan;
- inline JavaScript syntax-only parse with bundled Node.js;
- import, render, edit, and export source-path review;
- six-category comparison with the installed official example Mod `3705665433`;
- map comparison against current public-build item subtype/source/item, recipe subtype/group, store/exchange-store, crop-gene, and function-type owners;
- current official loader/cache and polymorphic constructor review.

The HTML page and remote libraries were not executed. This is a static review: it supports the listed source defects and safe-use boundary, but it does not attest to the live behavior of code later served by the external CDN.
