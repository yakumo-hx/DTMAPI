# 20260721-0001: Assurance Governance Proportionality

## Metadata

- Update ID: `20260721-0001`
- Date: `2026-07-21`
- Lifecycle Status: `verified`
- Validation Level: `docs`
- Runtime Validation: `not-required`
- Related Issue State: `mitigated`
- Area: governance/assurance/review/validation/receipts/batch6
- Source: User request to make small constraint-document corrections after the read-only Batch 5 closeout through Batch 6 Phase 0/G2 process-cost audit.
- Related Update: [Batch 6 AutoFishing Advanced Pilot](20260720-0008-batch6-autofishing-advanced-pilot.md)
- Related Review: [Batch 6 Boundary Correction Prerequisite](../../reviews/code/2026/20260719-0012-batch6-boundary-correction-prerequisite.md)

## Scope

This docs-only correction prevents the valid Batch 6 ownership boundary from growing a separate product-specific assurance framework. It does not change Runtime, SDK, product source, package contents, game files, Workshop state, or any historical receipt.

## Decisions

- A task that requires independent acceptance remains `implemented` until that acceptance passes; it is not marked `verified` first and reopened afterward.
- Audit-only work owns one Review and no Update unless project files change. Implementation owns one Update; a new root-cause Review is conditional, not automatic.
- Atomicity applies to final admission or visibility, not to the size of the implementation task. Hidden/blocked work may use small reversible commits.
- Adjacent G4/G5/G6 concerns for one product use one consolidated migration evidence set and reuse Catalog, SDK package, ABI, Doctor and release authorities.
- Consolidation retains a compact live zero-leftover scan for known product-owned owners/symbols in mandatory Runtime; aggregate net-zero is not accepted as ownership proof.
- Historical all-path inventories and milestone receipts remain audit evidence; only a small current invariant belongs in the default full suite.
- Validation is risk-proportional. Documentation changes do not require the complete Release suite; the exact final integrated package receives one full suite and the authoritative runtime/long-run validation.
- Dynamic commits, hashes and phase narratives stay with their owning Update/receipt instead of being copied into onboarding constraints.

## Changed files

- `AGENTS.md`
- `docs/workflows/document-governance.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/reviews/code/2026/20260719-0012-batch6-boundary-correction-prerequisite.md`
- `docs/updates/2026/20260720-0008-batch6-autofishing-advanced-pilot.md`
- this Update and `docs/updates/INDEX-2026-07.md`

## Validation

- `pwsh -NoProfile -File tools/scripts/check-doc-governance.ps1`: passed, `Document governance: OK (5491 checks)`.
- `git diff --check` on the seven owned constraint/record paths: passed; only existing Git line-ending conversion notices were emitted.
- Independent acceptance first found one P1: a net-zero-only ownership projection could leave unchanged AutoFishing ProductNative code in mandatory Runtime. The same Update was corrected to require a compact live zero-leftover owner/symbol check; independent re-review returned `PASS`.
- Complete Release suite and game smoke: intentionally not run because no executable, package, Hook, Loader, lifecycle or game behavior changed.

## Rollback

Revert only the constraint and record changes listed above. Do not modify or delete the parallel AutoFishing source migration, its local evidence, historical Phase 0/G2 receipts, game files, saves, or Workshop content.

## Follow-up

Apply these rules to the in-progress AutoFishing pilot before accepting any new product-specific receipt family into the default suite. Existing uncommitted implementation files are not completion evidence and remain owned by their active task.
