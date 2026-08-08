# 20260715-0002 Authorized Official JSON Tool Fork

## Metadata

- Update ID: `20260715-0002`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs,source`
- Runtime Validation: `not-required`
- Related Issue State: `mitigated`
- Area: third-party/official-json/author-tool/security/import-export/schema/browser-ui
- Source: user confirmed author permission and requested a local fork of Workshop item `3755367789`, minimal UI change, browser rendering verification, and detailed update notes

## Scope

Created a separate authorized local fork of the audited third-party official-JSON authoring tool. The Steam subscription directory remains unchanged. The fork stays under the ignored third-party reference root and is not incorporated into DTMAPI Runtime, GameBridge, public APIs, player packages, or `BepInEx/plugins`.

The user explicitly resolved the crop design point: seed and harvest item records remain separate “新增道具” projects, so the crop project now promises and emits only `plant_tbseed.json` rather than duplicating item creation.

## Related Review

- `docs/reviews/code/2026/20260714-0003-third-party-official-json-authoring-tool-audit.md`
  - owns the version 0.6 findings, original artifact hashes, official-schema comparison, and remediation order.

## Changed Files

- Local ignored fork root: `references/third-party-mods/official-json-authoring-tool-fork/`
  - preserves an exact `upstream/v0.6` snapshot;
  - contains the authorized `fork/v0.7-authorized` package;
  - vendors JSZip `3.10.1` with its license and removes the remote FileSaver dependency;
  - includes `tests/verify-fork.cjs` and a detailed author-facing `README.md`.
- `docs/updates/2026/20260715-0002-authorized-official-json-tool-fork.md`
  - owns this implementation lifecycle and validation boundary.
- `docs/updates/INDEX-2026-07.md`
  - routes this Update from the July ledger.

The ignored third-party fork is intentionally absent from Git status. It must be handed to the authorized author separately; the tracked Update does not redistribute its source.

## Implementation

- Replaced CDN JSZip with a pinned local copy, removed FileSaver.js, added a restrictive Content Security Policy, and changed downloads to the native Blob URL path.
- Replaced project-card interpolation with DOM/text rendering and escaped every imported or entered value that still enters dynamic form markup.
- Removed the delayed form rebuild race that could erase fast user input.
- Corrected static-hat/equipment `$type` discriminators, equipment ID consistency, the all-lowercase exchange-store basename, audited subtype/source/function/recipe/item ID drift, and numeric zero handling.
- Added subtype-specific generic item-function fields and generation.
- Rendered four default crop stages and blocked missing stage assets while preserving the explicit item-via-“新增道具” workflow.
- Rebuilt import/export around an untouched imported archive: root images, unknown `info.json` fields, root `Content` files, nested paths, unknown files, extra records, and unknown record fields survive round trip. Correct lowercase exchange tables are recognized, imported state is reset, directory moves relocate preserved content, and explicit project deletion removes its imported paths.
- Added a preflight validator for package metadata, IDs, output collisions, required function fields/assets, reference warnings, numeric ranges, shops, recipes, and crops.

## Validation

Passed:

- inline JavaScript syntax parse with bundled Node.js;
- static scan confirming no HTTP(S) resource, fetch/XHR/WebSocket/beacon, dynamic code execution, FileSaver call, old exchange-store basename, stale `ItemFunctionBuildingExteric`, or `function: { type: ... }` generator remains;
- real Google Chrome page load with no fork console/page error;
- browser smoke covering all six project categories, root assets, form submission, validation, and ZIP generation;
- generated ZIP assertions: 24 files, all-lowercase exchange filename, correct hat/equipment `$type`, four crop stages, and no crop-folder `item_tbitem.json`;
- six generic item-function payload cases and their required subtype fields;
- DOM injection check using an event-bearing `<img>` project name; it remained text, created no image node, and executed no event;
- import/re-export fixture with correct lowercase exchange table, two item records, unknown record fields, unknown JSON, nested binary data, and a root `Content` file; all expected content survived and the legacy imported `type` discriminator became `$type`;
- initial-screen screenshot comparison at `1440×1000`: mean absolute RGB difference stayed below `1/255` per channel, and pixels differing by more than `10/255` were about `0.97%`, concentrated in version credit, the new validation button, and footer wording;
- visual inspection of the populated six-project page retained the original layout, palette, cards, form spacing, and action placement.

Local browser evidence:

- `output/playwright/json-tool-original.png`
- `output/playwright/json-tool-fork.png`
- `output/playwright/json-tool-six-projects.png`

The standard Playwright skill wrapper could not be used because this environment has no `npx` or managed Chromium. Equivalent validation used the bundled Playwright library with the installed Google Chrome executable. No browser state or signed-in profile was used.

No Doloc Town or DTMAPI runtime test was run or required: this is a standalone browser authoring tool and the request did not authorize installing generated fixtures into the game.

## Evidence And Identity

- Upstream HTML SHA-256: `AAAA80D505C7414A7E8C7C6497D4BC0B15856C0DBDFAA5926C068E747A2FB07B`.
- Authorized fork HTML SHA-256: `1764D3735988697F534F0F46CC0C9B3D455B7D03B2F2BA0A282471A389349FA8`.
- The original Workshop path was not modified.
- The fork README records permission provenance, source boundary, exact behavior changes, current limitations, validation coverage, and author handoff requirements.

## Rollback

- Delete the ignored `references/third-party-mods/official-json-authoring-tool-fork/` directory.
- Remove this Update and its single July ledger row.
- No runtime, game directory, Workshop subscription, DTMAPI API, package, save, or config rollback is required.

## Follow-Up

- The author should decide the public release version and license before publishing.
- A future hardening pass may move the remaining fixed inline event handlers to an external script and remove CSP `'unsafe-inline'`.
- A future game update should refresh the small selectable ID maps from a versioned review source without redistributing official full configuration data.
- True multi-record visual editing requires a new folder/table/record project model; the current fork deliberately keeps first-record form editing while making the rest lossless.
