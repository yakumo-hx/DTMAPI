# 20260607-0006 Native Responsibility Method Audit

- Date: 2026-06-07
- Status: implemented
- Area: docs/reviews/api
- Source request/goal: user requested the existing API-family audit be expanded into per-API, per-symbol, per-field review with bidirectional matrix coverage, symbol-first locators, ordinary-mod usability, stable/experimental reassessment, concrete evidence sources, Top Risks per volume, and no implementation goal creation.
- Scope: docs-only review; no runtime/API/code behavior changes.

## Changed Files

- `docs/reviews/api/2026/20260607-0006-native-responsibility-method-audit-index.md`
- `docs/reviews/api/2026/20260607-0006-native-responsibility-method-audit/01-framework-config-events.md`
- `docs/reviews/api/2026/20260607-0006-native-responsibility-method-audit/02-workshop-content-ui-diagnostics.md`
- `docs/reviews/api/2026/20260607-0006-native-responsibility-method-audit/03-migrated-gameplay-apis.md`
- `docs/reviews/api/2026/20260607-0006-native-responsibility-method-audit/04-debug-user-operations.md`
- `docs/reviews/api/2026/20260607-0006-native-responsibility-method-audit/05-machine-equipment-save-vehicle-camera-farming.md`
- `docs/reviews/api/2026/20260607-0006-native-responsibility-method-audit/06-custom-entities.md`
- `docs/updates/2026/20260607-0006-native-responsibility-method-audit.md`
- `docs/updates/INDEX.md`

## Summary

- Added the `20260607-0006-native-responsibility-method-audit` index and six domain volumes.
- Parsed 82 public-api-matrix rows and 1700 public Abstractions symbols.
- Each block uses `Symbol` as the primary locator and `Declaration path:line` as a secondary locator.
- Each block records current marker, review advice, ordinary mod usability, matrix coverage, implementation/native owner/evidence, result, and recommendation.
- Result taxonomy includes `OK`, `Watch`, `Gap`, `Blocked`, and `MatrixGap`; Gap/Blocked recommendations explain why ordinary mods cannot safely rely on the surface now.

## Validation

- Symbol inventory generated from `src/DTMAPI.Abstractions/*.cs`.
- Forward coverage: 82 matrix rows were parsed and summarized in the index.
- Reverse coverage: 1700 public symbols were classified as explicit/family-only/missing and listed in the final decision table.
- Passed: `git diff --check`.
- Game smoke was not run; this is a source/evidence documentation audit only.

## Evidence Links

- Review index: `docs/reviews/api/2026/20260607-0006-native-responsibility-method-audit-index.md`
- Prior family-level review: `docs/reviews/api/2026/20260607-0005-native-responsibility-api-audit.md`
- API matrix: `docs/api/public-api-matrix.md`
- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`

## Rollback

- Revert this update by removing the new `20260607-0006` review index, its volume directory, this update record, and the matching row in `docs/updates/INDEX.md`.
- Runtime behavior, public API assemblies, and game files are unaffected.

## Follow-Up

- No implementation goal was created by this audit.
- If a future fix is requested, choose one exact high-risk family from the final decision table, then create a dedicated `docs/goals/YYYY/...` handoff and sibling `.goal.txt` prompt.
