# 20260715-0004 Official JSON Tool Documentation Regression

## Metadata

- Update ID: `20260715-0004`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs,source`
- Runtime Validation: `not-required`
- Related Issue State: `verified`
- Area: third-party/official-json/author-tool/audit/documentation/schema
- Source: user requested a regression of the earlier third-party JSON authoring-tool audit against the refreshed official Workshop JSON documentation

## Scope

Regressed every F1-F8 finding from the July 14 third-party tool audit against the complete July 15 browser-rendered official Feishu snapshot. This was a documentation and source review only. It did not modify the official snapshot, upstream Workshop subscription, authorized browser-tool fork, DTMAPI runtime, game directory, or user Mod data.

## Related Review

- `docs/reviews/code/2026/20260715-0003-official-json-tool-doc-regression.md`
  - owns the evidence matrix, official-document inconsistencies, current verdict, and fork follow-up requirement.
- `docs/reviews/code/2026/20260714-0003-third-party-official-json-authoring-tool-audit.md`
  - remains the owner of the upstream v0.6 source/security/import-export findings.
- `docs/updates/2026/20260715-0002-authorized-official-json-tool-fork.md`
  - owns the already-completed local fork implementation and browser validation.

## Changed Files

- `docs/reviews/code/2026/20260715-0003-official-json-tool-doc-regression.md`
  - records the new independent official-document regression.
- `docs/reviews/code/2026/20260714-0003-third-party-official-json-authoring-tool-audit.md`
  - adds one short follow-up link without rewriting the frozen original findings.
- `docs/updates/2026/20260715-0004-official-json-tool-doc-regression.md`
  - owns this audit lifecycle.
- `docs/updates/INDEX-2026-07.md`
  - routes this Update from the July ledger.

## Result

- No original finding was retracted.
- Official pages directly confirm `$type`, hat/equipment ID equality, subtype payload fields, the all-lowercase exchange-store filename, the two-table crop responsibility, and required root package resources.
- Official TSVs directly confirm the audited subtype, source, recipe-group, and exterior-function spelling corrections.
- Tool security and lossless-import findings remain source-proven and outside the official JSON schema's responsibility.
- The official item-ID sheet is partial, so full item-map corrections continue to rely on current native/sample evidence.
- The official documentation itself contains a wallpaper-field table mismatch and a crop comment typo; the original audit correctly followed the complete JSON examples and runtime-compatible fields.
- One new fork follow-up is open: official `growth_months: []` means unrestricted growth, but the authorized fork's preflight currently requires at least one month.

## Validation

Passed:

- official snapshot completeness/report inspection;
- exact official code-block and TSV-cell comparison for all schema and documented-ID findings;
- historical May-snapshot spot checks for the key pre-existing rules;
- source inspection of the authorized fork's crop-month validator;
- documentation-governance checks.

No game/runtime test was run or required because this update changes only review and lifecycle documentation.

## Evidence

- Official snapshot router: `references/doloc-town/official-workshop-docs/README.md`.
- Snapshot report: `references/doloc-town/official-workshop-docs/feishu-crawl-20260715/REPORT.md`.
- Snapshot manifest: `references/doloc-town/official-workshop-docs/feishu-crawl-20260715/manifest.json`.
- Detailed finding matrix: `docs/reviews/code/2026/20260715-0003-official-json-tool-doc-regression.md`.

## Rollback

Remove this Update, its July ledger row, the new regression review, and the single follow-up link in the original audit. No runtime, official reference, game, Workshop, or fork rollback is required.

## Follow-Up

Resolved by `docs/updates/2026/20260715-0005-official-json-tool-crop-month-fix-and-handoff.md`.

- Relax the fork validator so an empty crop month list is valid and add a browser regression for empty-list export/import.
- Preserve the user-approved split workflow while explaining that the complete Mod still needs both item and plant tables.
- Re-run the existing browser UI and ZIP regression suite when that source change is authorized.
