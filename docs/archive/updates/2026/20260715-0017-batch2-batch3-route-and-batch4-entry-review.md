# 20260715-0017 Batch 2/3 Route Compliance And Batch 4 Entry Review

## Metadata

- Update ID: `20260715-0017`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-run`
- Related Issue State: `open`
- Area: review/major-update/batch2/batch3/batch4/doctor/qa/route
- Source: user requested verification that completed Batch 2/3 work followed the roadmap and asked whether Batch 4 could begin

## Scope

- independently re-audit Batch 2 and Batch 3 commit, Update, source, package and retained runtime evidence;
- rerun the complete non-game Release entry on current HEAD;
- distinguish the verified Batch 3 Author SDK preview from the full frozen Batch 3 product milestone;
- identify the missing player-facing read-only Doctor/misinstallation path required by D1 and the third-round closed inputs;
- authorize only read-only Batch 4 Checkpoint B dependency mapping while holding physical QA extraction until the narrow Batch 3 closure passes;
- record post-Batch-3 regression and final-release carry-forward gates without reclassifying them as Batch 4 entry blockers.

This is a review/documentation update. It does not modify Runtime/API/Doctor/QA behavior, create a Batch 4 implementation lifecycle, acquire the Runtime lock, launch Doloc Town, or write a game/Workshop/subscription/player/package tree.

## Changed Files

- `docs/reviews/code/2026/20260715-0009-batch2-batch3-route-and-batch4-entry-review.md`: owns the route verdict, Batch 2/3 evidence, player Doctor gap, Batch 4 entry boundary and carry-forward regression gates.
- `docs/updates/2026/20260715-0017-batch2-batch3-route-and-batch4-entry-review.md`: owns this documentation lifecycle.
- `docs/updates/INDEX-2026-07.md`: routes this Update from the monthly ledger.

## Validation

- Git branch/HEAD/status and commit ancestry were inspected; the worktree was clean and Batch 2/3 are distinct ordered commits.
- Batch 2 and Batch 3 Reviews, Updates, decision dockets, Catalog, projects, source paths, tests and retained runtime/smoke records were cross-checked.
- `tools/scripts/test.ps1 -Configuration Release` passed on current HEAD in 732.3 seconds with zero reported build warnings/errors and all Unit, Author SDK, Doctor, deterministic package, portable SDK, ABI/Catalog/release-contract, dual-host installer transaction, evidence and governance gates green.
- Focused `DTMAPI.InstallDoctor.Tests` and `DTMAPI.AuthorSdk.Tests` Release runs passed with zero warnings/errors.
- Current Author SDK ZIP SHA-256 equals the recorded `12C246EFF17557A1536357677B50B87AA83455AA47712F1930F7D72749A8929C`.
- The existing final Batch 2 candidate package audit was reviewed under the Workshop release-audit workflow and has zero blockers; no new matrix was run because that package predates Batch 3 and is not a current-HEAD RC.
- The evidence-retention allowlist was regenerated and its check passed after adding this record.
- `tools/scripts/check-doc-governance.ps1` and `git diff --check` passed after adding this record.
- No game/runtime validation was run; Runtime conclusions use already-owned smoke evidence only.

## Rollback

Remove Review 0009, this Update and its July ledger row, then regenerate the evidence-retention allowlist. Rollback changes no Batch 2/3 source, package, Catalog or runtime evidence.

## Follow-Up

- Open one focused Batch 3 player-Doctor closure Review/Update; do not fold it into Batch 4 or Author SDK repair authority.
- Batch 4 may prepare its read-only Checkpoint B dependency map in parallel, but physical QA host/code extraction waits for the Doctor closure.
- Coordinate ownership of `run-game-smoke.ps1`, UnitTests and the active Smoke matrix with continuing Update `20260715-0003` before Batch 4 edits them.
- After the closure, create one `in-progress` Batch 4 Update and preserve the staged extraction/rollback order in Review 0009.
