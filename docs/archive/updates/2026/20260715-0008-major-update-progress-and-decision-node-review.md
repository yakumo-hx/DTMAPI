# 20260715-0008 Major Update Progress And Decision-Node Review

## Metadata

- Update ID: `20260715-0008`
- Date: 2026-07-15
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Area: review/major-update/route/batch2/regression/validation/decision-nodes/animalpack
- Source: user requested a current major-mainline progress review, exclusion of unrelated local/third-party non-DLL work, regression and new-problem classification, and printed future decision nodes with AnimalPack detail

## Scope

Added the durable review `docs/reviews/code/2026/20260715-0005-major-update-progress-and-decision-node-review.md`.

The review records that the committed mainline has reached the first independently reviewable Batch 2 slice while Batch 2 remains `in-progress`; Batch 0, both ownership P0s and the pre-Batch-2 recovery/regression checkpoint are complete and independently committed. It confirms that Batch 3–8 have not started and the agreed order remains intact.

It ranks immediate and release regressions, records a non-hermetic private-reference dependency in the default Release test path, corrects the `20260714-0003` frame-driver evidence statement, and prints the focused product-decision nodes which reopen only when scheduled. AnimalPack receives a dedicated trigger, frozen boundary, required facts, `Animal-U` through `Animal-Y` choices, and evidence-first implementation sequence.

The user-designated official-document/third-party JSON-tool/non-DLL Mod work was excluded from all mainline conclusions. No excluded implementation or result is approved by this Update.

## Changed Files

- `docs/reviews/code/2026/20260715-0005-major-update-progress-and-decision-node-review.md`
- `docs/updates/2026/20260715-0008-major-update-progress-and-decision-node-review.md`
- `docs/updates/2026/20260714-0003-pre-batch2-install-transaction-and-runtime-regressions.md` (dated evidence correction only)
- `docs/updates/2026/20260714-0004-batch2-version-release-authority.md` (exact-HEAD replay evidence and prerequisite qualification only)
- `docs/updates/INDEX-2026-07.md`

No Runtime, API, manifest, package, game, Workshop, debug issue, hook or product implementation changed.

## Validation

- exact detached HEAD `866021b5` compiled with zero warnings/errors;
- a Git-only detached worktree failed UnitTests at the direct read of ignored private reverse data, proving an implicit validation prerequisite;
- the exact same HEAD with the configured workspace reverse-reference root mounted passed `tools/scripts/test.ps1 -Configuration Release` in 220.4 seconds;
- passing gates: `DTMAPI.UnitTests: OK`, runtime-evidence retention, player Runtime-only uninstall ownership, dual-host developer package/enablement transaction tests, and Catalog checks (`26/11/21/45`);
- no game process was launched; existing two mainline runtime evidence sets were reviewed;
- document governance passed 4,810 checks after adding this Review/Update/index entry and the two dated evidence qualifications;
- `git diff --check` passed after the documentation changes.

## Evidence

- `docs/updates/2026/20260713-0010-batch0-boundary-catalog-baseline.md`
- `docs/updates/2026/20260713-0011-player-runtime-only-uninstall-ownership-p0.md`
- `docs/updates/2026/20260713-0012-oil-official-json-oneaction-decoupling-p0.md`
- `docs/updates/2026/20260714-0003-pre-batch2-install-transaction-and-runtime-regressions.md`
- `docs/updates/2026/20260714-0004-batch2-version-release-authority.md`
- `docs/debug/evidence/GAME-SMOKE/20260714-034644`
- `docs/debug/evidence/GAME-SMOKE/20260714-034949`
- `docs/reviews/code/2026/20260713-0009-first-party-animal-pack-product-boundary-review.md`
- `docs/reviews/code/2026/20260713-0011-major-update-sixth-decision-docket.md`

## Rollback

Remove this review and Update, revert the dated factual qualifications in Updates `20260714-0003` and `20260714-0004`, and remove this Update's single July-ledger row together. No implementation or runtime rollback applies.

## Follow-Up

Continue the existing Batch 2 Update. Make the version authority and release contract executable, separate normal Release tests from optional private-reference conformance or add an explicit preflight, close minimum-version and retained-ABI gates, then run the real stale-Runtime/update/recovery and all-public-product matrices. Open AnimalPack only at its printed evidence-ready node.
