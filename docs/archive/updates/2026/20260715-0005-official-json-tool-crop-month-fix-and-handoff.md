# 20260715-0005 Official JSON Tool Crop-Month Fix And Handoff

## Metadata

- Update ID: `20260715-0005`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-required`
- Related Issue State: `verified`
- Area: third-party/official-json/author-tool/crop/validation/browser/package
- Source: user requested correction of the authorized fork and an author-return handoff package after the official-document regression found that empty `growth_months` is valid

## Scope

Corrected the authorized local browser-tool fork so an empty crop month list means unrestricted growth, retained strict validation for supplied values, added browser regression coverage, visually checked the affected crop form, and produced a self-contained author handoff ZIP.

The Steam subscription, upstream v0.6 snapshot, official documentation snapshot, game directory, DTMAPI runtime, BepInEx layout, and user Mod data were not changed.

## Source Review

- `docs/reviews/code/2026/20260715-0003-official-json-tool-doc-regression.md`
  - established that the official crop page explicitly allows an empty `growth_months` and that the fork's nonempty requirement was an over-strict validator defect.
- `docs/updates/2026/20260715-0002-authorized-official-json-tool-fork.md`
  - remains the owner of the main v0.6-to-v0.7 fork implementation and earlier browser/security/import-export validation.

## Changed Files

Ignored authorized-fork sources:

- `references/third-party-mods/official-json-authoring-tool-fork/fork/v0.7-authorized/Content/模组工具 v0.7-authorized.html`
  - accepts an empty month array;
  - validates only supplied month entries as integers in `1–4`;
  - explains the empty-list meaning in the crop form and error message.
- `references/third-party-mods/official-json-authoring-tool-fork/tests/verify-fork.cjs`
  - exports an empty-month crop and asserts exact `growth_months: []` preservation;
  - changes the same crop to `0,5,2.5` and asserts preflight rejection.
- `references/third-party-mods/official-json-authoring-tool-fork/README.md`
  - records the corrected semantics, new test coverage, and current HTML hash.
- `references/third-party-mods/official-json-authoring-tool-fork/HANDOFF-20260715.md`
  - gives the author a standalone explanation, validation result, package layout, and publication checklist.

Tracked lifecycle records:

- `docs/reviews/code/2026/20260715-0003-official-json-tool-doc-regression.md`
  - adds a short implementation-resolution link.
- `docs/updates/2026/20260715-0004-official-json-tool-doc-regression.md`
  - marks the discovered fork issue verified and links this implementation.
- `docs/updates/2026/20260715-0005-official-json-tool-crop-month-fix-and-handoff.md`
  - owns this implementation and package lifecycle.
- `docs/updates/INDEX-2026-07.md`
  - routes this Update and synchronizes the resolved state of `20260715-0004`.

Generated ignored artifacts:

- `output/handoff/doloc-town-json-authoring-tool-v0.7-authorized-20260715-r2.zip`
- `output/handoff/doloc-town-json-authoring-tool-v0.7-authorized-20260715-r2/`
- `output/playwright/json-tool-fork-20260715-regression/`
- `output/playwright/json-tool-handoff-r2/`

## Implementation

The prior predicate rejected both an empty list and out-of-range values. It now rejects only supplied entries that are non-integers or outside `1–4`. The generated JSON behavior did not need a transformation: the tool already emitted `data.growMonths`, so a valid empty form value naturally remains `growth_months: []`.

The affected UI text now says:

- label: `生长月份（留空表示不限制）`;
- placeholder: `2,3（可留空；填写时用逗号分隔 1-4）`;
- error: `生长月份留空表示不限制；填写时只能使用 1–4 的整数。`.

No project model, JSON basename, crop stage, item split, import preservation, CSP, dependency, or main-layout behavior changed.

## Validation

Passed on the working fork:

- test-script syntax check;
- extraction and syntax compilation of the single inline HTML script;
- real Google Chrome browser suite for all six project categories;
- generated ZIP assertions: 24 files, exact lowercase exchange basename, correct hat/equipment `$type`, four crop stages, no crop-local `item_tbitem.json`, and exact empty `growth_months: []`;
- invalid `0,5,2.5` crop months rejected with the new official-compatible message;
- six generic item-function payloads;
- DOM injection regression;
- lowercase exchange import and loss-preserving multi-record/unknown/nested/root-content round trip;
- zero page-script or console errors in the automated file-page suite.

Passed with Playwright CLI and installed Google Chrome:

- opened the same tool over a temporary `127.0.0.1`-only static server because the CLI blocks `file://`;
- snapshot confirmed the new crop label, empty-capable placeholder, split item/plant guidance, and four stage rows;
- visual inspection found no clipped label, overlapping field, width drift, or broken modal scrolling;
- the temporary server and browser session were closed after capture;
- the sole CLI console entry was an expected HTTP 404 for an absent optional `favicon.ico`, not a tool script error.

Visual comparison at a 1440×1000 viewport against the pre-fix authorized-fork initial screenshot:

- mean absolute RGB difference: approximately `0.0007904` normalized;
- pixels differing by more than `10/255`: `0.12%`;
- the initial/main screen has no material layout change; the intended text change appears only after opening the crop form.

Passed on a fresh extraction of the final handoff ZIP:

- 13 archive files total, including `SHA256SUMS.txt`;
- all 12 manifest-listed file hashes matched;
- all required root, HTML, vendored JSZip, MIT-license, author-note, test, and evidence files were present;
- no `upstream/` snapshot was included;
- the full real-Chrome suite passed again from the extracted package with the same 24-file result.

Documentation governance passed with 4741 checks. No Doloc Town or DTMAPI runtime test was run or required because this is a standalone browser authoring tool.

## Evidence And Identity

- Corrected HTML SHA-256: `BA56B0DEA224BD72660A9FBCD1DBF728BD9A177E8A67E5F6E5943FF7D70B5D3E`.
- Updated browser test SHA-256: `D2848E0A99B5F2A4AD0A59D54858DAA550281AEEA5DD10A10C2584D681F4B47A`.
- Handoff ZIP size: `417599` bytes.
- Handoff ZIP SHA-256: `BC860FDA743AB4891419D2D4D7B4B236EA119B267B83C751307E6AE9BEC951E9`.
- Crop-form screenshot: `output/playwright/json-tool-fork-20260715-regression/json-tool-crop-empty-month-ui.png`.
- Six-project screenshot: `output/playwright/json-tool-fork-20260715-regression/json-tool-six-projects.png`.
- Extracted-package browser evidence: `output/playwright/json-tool-handoff-r2/json-tool-six-projects.png`.

The handoff archive contains only the corrected fork working package, author notes/tests/evidence, vendored JSZip and its license. It does not contain the upstream v0.6 snapshot, official game configuration, decompiled source, DTMAPI binaries, or runtime files.

## Rollback

- Restore the two-line crop label/validator behavior and remove the two new test assertions only if intentionally returning to the incorrect nonempty-month rule.
- Delete `HANDOFF-20260715.md` and the generated handoff/evidence artifacts.
- Revert the four tracked documentation changes owned by this Update.
- No game, runtime, Workshop subscription, save, configuration, or DTMAPI rollback is required.

## Follow-Up

Superseded on 2026-07-15 by `docs/updates/2026/20260715-0007-official-json-tool-v06-repair-clone.md`. The r2 ZIP and its then-current internal naming remain audit evidence, not the current handoff candidate.

- Post-handoff Touhou Fumo regression `docs/updates/2026/20260715-0006-touhou-fumo-author-tool-regression.md` found unsupported single-wrapper archives, no near-match table-name detection, and destructive nested-shop re-export. Treat the current ZIP as a retained test handoff, not a final author release candidate, until that issue is fixed and the package is rebuilt.

- The author still owns the public version, license, 署名, and Workshop publication decision; the internal package remains labeled `v0.7-authorized` with `info.json` version `0.7.0`.
- After publication, test the actual downloaded Workshop copy offline and generate one unrestricted-month crop plus one restricted-month crop.
- Keep `Content/vendor/` and `Content/vendor/JSZIP-LICENSE.md` together with the HTML.
