# 20260712-0004 — Document Governance Index Consistency

## Metadata

- Update ID: `20260712-0004`
- Date: 2026-07-12
- Lifecycle Status: `verified`
- Validation Level: `docs, source, unit`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Source: user approved a small correction after the first real Mod-refactor and player-log-research audit exposed index consistency and Review-lifecycle gaps.

## Scope

- validate normalized monthly Update metadata rather than validating only the owning record;
- require the monthly Lifecycle, Validation, Runtime, and Issue columns to match the owning Update exactly;
- correct the two rows that exposed the blind spot;
- distinguish frozen code/API/root-cause Reviews from append-only Manual QA evidence gates.

## Changed Files

- `docs/updates/INDEX-2026-07.md`
- `docs/workflows/document-governance.md`
- `docs/reviews/README.md`
- `tools/scripts/check-doc-governance.ps1`
- `tools/scripts/README.md`
- `docs/updates/2026/20260712-0004-document-governance-index-consistency.md`

## Validation

- PowerShell parser validation passed for `tools/scripts/check-doc-governance.ps1`.
- Negative governance gate: the upgraded checker rejected the six expected lifecycle/validation violations in `20260711-0012` and `20260711-0013` before those rows were corrected.
- Final standalone governance gate passed: `Document governance: OK (4093 checks)`.
- Full `tools/scripts/test.ps1 -Configuration Release` passed: all builds completed with 0 warnings and 0 errors, `DTMAPI.UnitTests: OK`, and the integrated governance gate returned success.
- `git diff --check` passed with line-ending normalization warnings only.
- No game install, runtime lock, game launch, or smoke run is required for this docs/source-only correction.

## Rollback Notes

- Revert this Update's changes to restore record-only metadata checks and the previous Review freeze wording.

## Follow-Up

- Keep domain-specific evidence in Review/Debug/Smoke owners; use normalized index columns only for routing and compact state projection.
