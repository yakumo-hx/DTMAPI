# Update 20260718-0001: Batch 4 G9 Reacceptance Audit

- Lifecycle Status: `verified`
- Validation Level: `docs,source`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Date: 2026-07-18
- Source Request: independently re-audit whether Batch 4 is complete and whether Batch 5 may begin
- Related Review: [Batch 4 G9 Reacceptance Review](../../reviews/code/2026/20260718-0001-batch4-g9-reacceptance-review.md)
- Owning Implementation: [Batch 4 G9 Source Boundary Closure](20260717-0002-batch4-source-boundary-reopen.md)

## Scope And Outcome

This documentation/source audit independently rechecked final commit `d01c2ca7ee47461b3d876b7d192cb5e692d49c52` against the frozen Batch 4 route and the two P1 plus one P2 findings that had reopened G8. It does not change Runtime code, start Batch 5, install to the shared game, launch Doloc Town, alter Steam subscriptions, or upload a Workshop item.

The audit found no remaining Batch 4 admission blocker. Old UI evidence ownership and QA-only state mutation are absent from player source; the no-QA frame no longer traverses the optional participant; executable schema-5 and meta-negative gates pass; retained staged/no-QA/eleven-product receipts cover the required current-tree behavior; and a final-commit five-DLL/no-QA Runtime package passes the player-like Workshop audit. Batch 4 remains complete through G9 and Batch 5 may begin under a separate lifecycle Update.

## Changed Files

- `docs/reviews/code/2026/20260718-0001-batch4-g9-reacceptance-review.md`: durable independent findings, evidence, caveats, and Batch 5 admission boundary.
- `docs/api/public-api-matrix.md`: corrected the stale AnimalViewer Hook/lifecycle/evidence description without changing the public API or Experimental status.
- `docs/updates/2026/20260717-0002-batch4-source-boundary-reopen.md`: appended the exact final-commit package/provenance audit that its earlier pre-commit evidence required.
- `docs/debug/evidence-retention-allowlist.json`: regenerated after the new Review referenced existing G9 runs; the only delta is that Review as one new source file, with run/artifact/Runtime-evidence/dump counts unchanged at `758/225/62/6` and no `tmp` or absolute audit-candidate path.
- `docs/updates/INDEX-2026-07.md`: registered this Update once in the canonical monthly ledger.

The unrelated pre-existing edit in `docs/reviews/api/native-owner-domains/03-animal-husbandry-behavior.md` was neither read as Batch 4 evidence nor changed by this audit.

## Validation

- `tools/scripts/check-batch4-qa-semantic-boundary.ps1`: passed with `164` production source files, `4/597` neutral host files/lines, `25/25` contracts, `149/149` forbidden patterns, `20` lifecycle contracts, `49` negative samples, and `4` built boundary artifacts.
- `tools/scripts/test-batch4-qa-semantic-inventory.ps1`: passed all twelve named source mutations, one Catalog projection mutation, and six receipt-set rejection cases.
- `tools/scripts/check-product-catalog.ps1`: passed with `products=26`, `public=11`, `workshop-items=21`, and `api-rows=46`.
- `tools/scripts/build-evidence-retention-allowlist.ps1 -Check`: passed after deterministic regeneration; source files changed from `398` to `399` only for the new Review, while retained run/artifact/Runtime-evidence/dump sets did not change.
- Existing frozen exact-tree `tools/scripts/test.ps1 -Configuration Release` evidence remains owned by Update `20260717-0002`; no source changed after that tested G9 tree. This audit did not rerun the approximately fifteen-minute full suite solely to duplicate it.
- A fresh Runtime-only `-SkipBuild` package records `BuildCommit=d01c2ca7ee47`, exactly five receipt-bound Runtime DLLs, and zero forbidden QA payload. `scripts/test_subscription_package.ps1 -KeepTemp` passed Windows PowerShell 5.1 parsing and the eight-case matrix with `Blockers: 0` in paths containing spaces and Chinese text.
- No local game/runtime smoke was required or run because this Update changes documentation only and reuses the owning G9 Update's retained current-tree runtime receipts.

## Rollback

Revert this Review, the AnimalViewer API-matrix correction, the post-commit audit addendum, this Update, and its monthly row together. Runtime, Hook, Catalog, package scripts, game state, and Workshop state require no rollback because this audit changed none of them.

## Follow-Up

Open a new in-progress Batch 5 Update before changing recurring work or demand activation. Preserve the G9 source/IL/meta-negative gates and staged/no-QA/product matrices. ISSUE-010, ISSUE-011, and the independent AutoFishing/ActionSpeed GC ladders remain open later release gates; this admission audit does not reinterpret any short functional run as GC proof.
