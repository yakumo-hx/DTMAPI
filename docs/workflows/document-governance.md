# DTMAPI Document Governance

Active policy for new work. Routine implementation uses the [Update template](../updates/README.md) and [product validation](product-change-validation.md); this file owns lifecycle/record policy, not another per-fix reading checklist.

## Canonical Ownership

| Fact | Owner |
| --- | --- |
| Mod identity, native ownership, native-save semantics | PROJECT.md |
| Scope, changed files, validation, rollback and completion | One implementation Update |
| Unresolved/changed root cause, architecture decision, acceptance reasoning | Task-specific Review when needed |
| Recurring symptom, attempts, rejected hypotheses and issue state | One Debug issue per symptom |
| Actual game/runtime run | Active smoke row and evidence path |
| Hook/API contract and its evidence | Focused Hook map / public API matrix |
| Product/release identities and observations | Catalog / subscription manifest / linked release Update |
| Working tree, version, lock and process | Git / version source / status script |

Other records link to the owner. The Update explains changes and acceptance; a release-candidate page keeps only candidate selection, remaining gates and evidence links; a roadmap/status page keeps only milestones and next actions. Detailed commands, run outcomes and hashes remain in their evidence owner. Do not copy these between Review, Issue, indexes and Update. Reuse existing Catalog/SDK/ABI/receipt/evidence authorities rather than creating milestone-specific assurance systems.

## Lifecycle

- Discussion: chat only unless a file is requested. Audit-only: at most one Review; no Update unless project files change.
- Review: required for unresolved or changed root cause/architecture, a repeated failed fix, explicit audit, or a named decision gate. A domain keyword (UI, Hook, save, Workshop) alone does not require a new Review; reuse valid analysis.
- Implementation: create/reuse one Update as `in-progress`; corrections and acceptance stay in that record.
- Acceptance: if independent audit is a required gate, use `implemented` until it passes; then `verified`. Never mark an unrun gate passed.
- Code/API/root-cause Reviews freeze substantive reasoning when implementation begins; allow a short resolution link. Manual QA remains append-only for new observations and gate outcomes, not implementation narrative.
- Update verification does not automatically close a broader Issue. Preserve valid failures and rejected hypotheses.

## Conditional Writes

Always update the owning Update and its monthly row for non-trivial changes. Other writes require changed facts: Issue state/evidence, actual smoke, Hook boundary, API contract, product metadata, or a moved navigation route. Source-only refactors need no Hook-map change. Small typo-only edits may skip an Update.

Use the [ledger sync](../../tools/scripts/sync-update-ledger.ps1) to project the four Update status fields into the monthly row. Keep that row's human summary/area; do not retype status copies or rewrite other rows. Root/year indexes remain navigation only.

Issue files own their `State` and `Current boundary` fields. Regenerate [the issue index](../debug/issues/README.md) with `tools/scripts/sync-issue-index.ps1`; `-Check` verifies it without writing. Do not edit generated index rows or maintain a second issue-state list.

## Validation and Finish

Use [product validation](product-change-validation.md#reuse-and-invalidation) for scope, evidence reuse and reruns. A local failure does not create a new full-suite gate; a required full suite still needs an actual complete PASS. Documentation edits alone do not invalidate previously tested code/package bytes.

Run `tools/scripts/check-doc-governance.ps1` at documentation closeout. Its deterministic checks remain required and fast; report failures or a concise PASS, not the assertion count as an accomplishment.

## Metadata and History

New Updates use separate fields:
- Lifecycle Status: `proposed, in-progress, implemented, verified, blocked, reverted, superseded`.
- Validation Level: comma-separated `not-run, docs, source, unit, runtime, player`.
- Runtime Validation: `not-required, not-run, passed, failed, blocked, partial`.
- Related Issue State: `none, open, monitoring, mitigated, verified, closed, deferred`.

Each Update belongs to exactly one monthly ledger. Keep root/year routing, allowed metadata, normalized monthly agreement and local links valid. A closed month gets one compact `Non-Authoritative Monthly Summary` with the checker marker; it is not another status owner.

Historical Goals, retired plans and cutoff snapshots are read-only evidence. Retire a live authority with one compact superseded/frozen handoff and a route to its successor; do not keep appending implementation progress. Frozen-text hashes require an explicit governance change. Preserve archival backlinks; change a current rule without rewriting old evidence.

The approved workspace construction may relocate retired records into `docs/archive` after preserving originals. Its migration manifest owns locations and full-reading coverage only. Archived bodies permit mechanical Markdown-link corrections, not rewritten observations or failures. Keep frozen originals and published path identities intact; resolve physical locations through the shared document-path helper. Update IDs and their single monthly rows survive migration. Restore from the batch journal only when the affected files still match that batch, preserving unrelated edits.

Extract reusable findings into existing current owners or `docs/knowledge`, with their actual source/build scope. A historical PASS, old requirement or guessed successor cannot become current authority through relocation. Ordinary fixes do not repeat this review or migration; closed months may be archived in a separate bounded batch.

A new receipt/schema family requires a new authority boundary. Ordinary versions, batches and product names do not justify one. Ownership consolidation must still prove zero forbidden leftovers, not merely a net-zero count.
