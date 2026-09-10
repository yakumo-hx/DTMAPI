# 20260714-0002 Major Update Progress, Route, And Regression Review

## Metadata

- Update ID: `20260714-0002`
- Date: 2026-07-14
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Area: review/major-update/route/git/version/installer/regression/gc/release
- Source: user requested a current progress, route-conformance, regression, and new-problem review while explicitly excluding a separate local analysis

## Scope

Added the durable review `docs/reviews/code/2026/20260714-0001-major-update-progress-route-regression-review.md` and linked it from the July Update ledger.

The review establishes the current two-axis handoff:

- implementation/evidence is through verified Batch 0 plus the player-uninstaller and Oil/OneAction P0s, at the Batch 2 entrance;
- Git durability remains at baseline commit `c826d261`, with all three implementation batches still uncommitted.

It confirms route conformance, ranks the next regression gates, and records two newly concrete Batch 2/3 risks without changing their implementation:

1. Oil's source manifest and native official info currently materialize `0.3.1-dtmapi` and `1.0.0` together;
2. developer package publication completes before enablement mutation, so malformed/unwritable `mod_infos.json` may leave a fail-closed but stranded package.

The review also bounds the latest runtime evidence to two focused DirectExe + HookProbe + CoreOnly Oil runs. It does not claim that 0.5.5, Steam/player startup, normal input, QA extraction, demand activation, or the AutoFishing/ActionSpeed GC gates are complete.

The independent 2026-07-14 JSON-derived-value/manual-QA analysis was explicitly excluded from the conclusions and remains separate worktree material.

## Changed Files

- `docs/reviews/code/2026/20260714-0001-major-update-progress-route-regression-review.md`
- `docs/updates/2026/20260714-0002-major-update-progress-route-regression-review.md`
- `docs/updates/INDEX-2026-07.md`

No runtime, public API, manifest, package, version, game, Workshop, debug issue, hook, smoke, or local installation file was changed by this review.

## Validation

- `tools/scripts/test.ps1 -Configuration Release`: passed in 169.1 seconds with zero build warnings/errors and `DTMAPI.UnitTests: OK`;
- player Runtime-only uninstall ownership matrix: passed;
- product Catalog checker: passed (`products=26`, `public=11`, `workshop-items=21`, `api-rows=45`);
- document governance: passed after adding this review, Update, and ledger entry, 4,534 checks;
- `git diff --check`: passed after adding this review, Update, and ledger entry;
- no game process was launched; preserved Oil runtime evidence was reviewed only.

## Evidence

- `docs/updates/2026/20260713-0010-batch0-boundary-catalog-baseline.md`
- `docs/updates/2026/20260713-0011-player-runtime-only-uninstall-ownership-p0.md`
- `docs/updates/2026/20260713-0012-oil-official-json-oneaction-decoupling-p0.md`
- `docs/debug/evidence/GAME-SMOKE/20260713-221443`
- `docs/debug/evidence/GAME-SMOKE/20260713-221610`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/issues/ISSUE-011-20260623-short-run-native-crash.md`

## Rollback

Remove this review, this Update, and its one July-ledger row together. No source or runtime rollback is applicable.

## Follow-Up

Separate and commit the verified Batch 0/P0 work from the explicitly excluded local analysis before starting Batch 2. At the Batch 2 entrance, add version-projection assertions and developer-install enablement failure/recovery coverage, then establish the single version/release authority and retained-binary compatibility gates.
