# 20260720-0001: Batch 5 And Batch 6 Prerequisite Audit

## Metadata

- Update ID: `20260720-0001`
- Date: `2026-07-20`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-run`
- Related Issue State: `open`
- Area: review/major-update/batch5/batch6/performance/evidence/provenance/architecture
- Source: User request to audit completed Batch 5 work and the newly added Batch 6 prerequisites.
- Related Review: [Batch 5 收口与 Batch 6 前置审计](../../reviews/code/2026/20260720-0001-batch5-and-batch6-prerequisite-audit.md)

## Summary

- Reaccepted all seven previously audited Batch 5 P1 implementation/evidence groups at their bounded dirty-worktree scope; no new P0/P1 Runtime regression was found.
- Separated that implementation result from release authorization: the final candidate is byte-bound to current Release outputs but its old `BuildCommit` cannot reproduce the uncommitted Batch 5 source tree.
- Found that Catalog/checker and evidence retention do not machine-bind or preserve the complete formal no-demand/ActionSpeed/AutoFishing/Candidate11/Published11 evidence set. The current ladders establish structural/lifecycle bounds and truthful trends, not a quantified memory/GC budget.
- Confirmed Batch 6 remains Phase-0-only. G0-G7 all remain blocked; G0/G2 ordering, the G2/G3 circular gate, AutoFishing's third-vs-fifth-save wording and the Advanced vertical-slice boundary require correction before implementation or product migration.
- Identified an adjacent current-build reverse-tool boundary: custom repo-local `BuildRoot` values can copy official game bytes outside the ignored reverse tree.

## User-Visible Impact

- No Runtime, product, save, game-directory, Workshop, SDK or package behavior changed.
- This audit does not authorize a 0.5.5 upload and does not start Batch 6 product migration.

## Changed Files

- `docs/reviews/code/2026/20260720-0001-batch5-and-batch6-prerequisite-audit.md`
- `docs/updates/2026/20260720-0001-batch5-and-batch6-prerequisite-audit.md`
- `docs/updates/INDEX-2026-07.md`
- `docs/debug/evidence-retention-allowlist.json` after regeneration for the new durable full-path references

## Validation

- Read the required project, feedback-review, documentation-governance, API-rebuild, debug, smoke, API-matrix, Batch 5 and Batch 6 authority records.
- Inspected the current Core/GameBridge/QA/product source, focused Batch 5 tests, Catalog/checker, GC/no-demand runners, release candidate, retained JSON receipts, allowlist generator/tests and reverse-baseline script.
- Independently ran PowerShell 7 `tools/scripts/test.ps1 -Configuration Release`: exit `0` in 971.1 seconds; all builds reported zero warnings/errors and the complete tracked source/unit/package/governance matrix passed. The suite's allowlist green result is itself within the audited incomplete exact-set contract and is not treated as proof that every formal Batch 5 root is retained.
- No game/runtime validation was run by this audit. Runtime lock was read-only checked as free and no `DolocTown.exe` remained.
- Regenerated the evidence allowlist from the new full-path references. `build-evidence-retention-allowlist.ps1 -Check` passed with 415 source files, 777 smoke runs, 62 runtime identities and nine durable roots; the supported final AutoFishing/Candidate11/Published11 references are now retained, while the intentionally recorded `BATCH5-NO-DEMAND` schema gap remains.
- `test-runtime-evidence-retention.ps1` passed against the regenerated allowlist, `check-doc-governance.ps1` passed 5,303 checks, and `git diff --check` exited `0` with existing line-ending normalization warnings only.

## Rollback

- Documentation-only change. Remove this Review/Update and its one monthly-ledger row, then regenerate the evidence-retention allowlist if the audit record must be withdrawn.
- Do not delete retained runtime evidence, reset the shared dirty worktree or use this rollback section to authorize a package upload.

## Follow-Up

1. Extend the evidence-retention schema/tests for the exact formal Batch 5 set, including `BATCH5-NO-DEMAND`, before cleanup.
2. Add structured Catalog performance receipts and accurate structural/trend-vs-budget wording.
3. Reconcile the API matrix, prior Review resolution, Batch 5 Update timeout/baseline wording, smoke matrix and roadmap QA status.
4. Rebuild the candidate from a reproducible source boundary before any 0.5.5 upload decision.
5. Complete Batch 6 G0 authority alignment, then G1 and the G2 minimal vertical slice; admit only the fifth-save AutoFishing pilot before other products.
6. Fail closed on unsafe repo-local reverse `BuildRoot` targets.

## Result

This audit is `verified` as a docs/source/unit review. Batch 5 remains accepted only as a bounded dirty-worktree implementation result; 0.5.5 release authorization and the stronger quantified performance claim are blocked. Batch 6 Phase 0 may continue after the recorded ordering corrections, while all product migration remains blocked.

## Subsequent Correction

An independent terminal-authority/fault-path review after the dirty checkpoint was committed found three adjacent source gaps that this audit did not cover: ContentQuery pre-receipt publication, CustomAnimals post-commit demand reconciliation, and pre-authority rejection observation. [Review 20260720-0002](../../reviews/code/2026/20260720-0002-batch5-terminal-receipt-observer-atomicity.md) owns those root causes and feeds the existing Batch 5 Update. The audit's performance-budget, retention, provenance, release-stop, and corrected-Phase-0 conclusions remain in force.
