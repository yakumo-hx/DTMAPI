# 20260715-0006 Touhou Fumo Author-Tool Regression

## Metadata

- Update ID: `20260715-0006`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-required`
- Related Issue State: `verified`
- Area: third-party/official-json/author-tool/import/validation/round-trip/touhou-fumo
- Source: user requested testing the final authorized handoff tool with the known typo-bearing Touhou Fumo archive

## Scope

Ran the exact handoff tool against the original third-party archive, isolated the enclosing-directory boundary from the known filename typo with a byte-identical inner-content fixture, exercised preflight and browser re-export, and recorded newly confirmed import/validation/data-integrity defects.

This update records a test and root-cause review only. It does not implement a fix, mutate the third-party source archive, rebuild the handoff package, or change DTMAPI/game/runtime files.

## Owning Review

- `docs/reviews/code/2026/20260715-0004-touhou-fumo-author-tool-regression.md`
  - owns the exact browser messages, archive identities, generated-path comparison, root causes, and required fix order.
- `docs/reviews/code/2026/20260714-0002-touhou-fumo-official-json-audit.md`
  - remains the owner of the Mod's official-loader/schema finding that `item_tbtiem.json` blocks the intended item and shop route.

## Changed Files

Tracked records:

- `docs/reviews/code/2026/20260714-0002-touhou-fumo-official-json-audit.md`
  - adds one short author-tool regression link.
- `docs/reviews/code/2026/20260715-0004-touhou-fumo-author-tool-regression.md`
  - records this regression and root-cause analysis.
- `docs/updates/2026/20260715-0005-official-json-tool-crop-month-fix-and-handoff.md`
  - marks the current handoff ZIP as a retained test artifact rather than a final release candidate.
- `docs/updates/2026/20260715-0006-touhou-fumo-author-tool-regression.md`
  - owns this test lifecycle.
- `docs/updates/INDEX-2026-07.md`
  - routes this Update.

Generated ignored evidence:

- `output/playwright/touhou-fumo-tool-test-original-wrapper.png`;
- `output/playwright/touhou-fumo-tool-test-normalized-import.png`;
- `output/touhou-fumo-tool-test/touhou_fumo-root-normalized-still-typo.zip`;
- `output/touhou-fumo-tool-test/touhou_fumo-reexported-by-tool.zip`.

## Result

- The original one-wrapper-directory ZIP imports as zero projects and is reported missing root `info.json`.
- Removing only that wrapper with all nine file hashes unchanged lets the tool import one shop project.
- The equipment directory receives only a generic unmapped warning; `item_tbtiem.json` is not named or suggested as `item_tbitem.json`.
- Preflight initially catches the tool's three-part-version rule and falsely treats the valid nested `Shop` directory as a generated ID.
- After controlled UI normalization of those unrelated blockers, preflight allows generation with warnings.
- Re-export keeps the misspelled item table byte-identical and creates no correctly named item table.
- Re-export relocates the original nested shop table and adds a second generated shop table; their seasonal counts disagree (`99` versus `1`).

Attribution: the misspelled table belongs to the original Touhou Fumo Mod; wrapper handling, exact-only type inference, first-segment path grouping, and seasonal-value loss are inherited v0.6 limitations; the imported-directory false rejection and duplicate relocated/generated shop tables are fork-introduced regressions.

The current handoff package therefore fails this real third-party fixture and remains open for an importer/validator correction pass.

## Validation

Passed as evidence collection:

- original and normalized archive identity/inventory;
- zero differences across all nine inner file hashes after wrapper-only normalization;
- real headed Chrome import, snapshot, preflight, edit, warning confirmation, and download;
- generated ZIP entry/hash/path/content comparison;
- source-level root-cause inspection;
- documentation governance checks.

No Doloc Town or DTMAPI runtime test was run or required for this browser-tool regression.

## Rollback

- Remove the generated `output/` fixtures and screenshots.
- Remove this Update, its July ledger row, the new regression review, and the two short cross-links.
- No source, package, game, Workshop, save, or runtime rollback is required because no implementation changed.

## Follow-Up

- Implement the seven-step fix order in the owning review using synthetic fixtures rather than redistributing the third-party Mod.
- Re-run all earlier v0.7 browser/security/import/export tests plus wrapper-root, near-match basename, nested-shop, and seasonal-value regressions.
- Replace the `20260715-r2` handoff ZIP with a newly hashed package only after all gates pass.

## Resolution

Resolved and reverified by `docs/updates/2026/20260715-0007-official-json-tool-v06-repair-clone.md`. The new repair clone passes the original archive blocker test, a corrected-name in-memory comparison, exact nested relocation, seasonal preservation, source Chrome, and final extracted-package Chrome gates. The r2 artifact remains superseded evidence only.
