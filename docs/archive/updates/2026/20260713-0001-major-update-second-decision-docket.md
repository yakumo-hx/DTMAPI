# 20260713-0001 Major Update Second Decision Docket

## Metadata

- Update ID: `20260713-0001`
- Date: 2026-07-13
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Source: user supplied `D:/下载/小讨论.txt`, then closed A/B/C/D through `D:/下载/第二轮小讨论.md`; refined Oil/version compatibility direction, requested the remaining boundary options, and asked why DTMAPI can manage its Mods while direct BepInEx plugins remain outside its control

## Summary

Recorded the second major-update decision docket and two focused source reviews.

The Oil baseline is refined to append only `crude_oil` weight 25 through official JSON, accepting a small amber denominator dilution to avoid replacing base entries. Mine remains a two-layer JSON-first prototype. Compatibility now emphasizes automatically updated Workshop functional Mods meeting a stale manually installed Runtime, with truthful per-Mod minimums and pre-assembly-load rejection.

The management review separates Steam file ownership, official enablement, DTMAPI CodeMod owner transactions, and BepInEx Chainloader ownership. It records that DTMAPI deactivation removes known owner resources but cannot unload a Mono assembly, Steam—not DTMAPI—deletes unsubscribed subscription folders, and external BepInEx plugins are diagnostic-only. It also identifies OfficialLocal shadowing of newer Workshop packages, a possible Reload-before-Save stale enablement refresh, and raw Workshop directory enumeration that is weaker than the native subscription list.

The installer P0 review proves `dtmapi-package.json` currently conflates five meanings and cannot grant destructive authority. Local read-only evidence found 22 existence-recognized packages but only 14 current bundled-state entries. The recommended contract separates author manifest, build provenance, and a transaction-matched installer receipt plus external state.

The follow-up closes the second round through a layered answer rather than one player-side choice: the player uninstaller removes Runtime only; verified receipts belong to Author SDK/internal deployment cleanup; player content loading is startup/lifecycle-driven with zero polling; the SDK provides explicit content reload first and optional development watching later; Catalog-first gradual migration targets a fully classified tree; and one unified versioned Author SDK owns the complete author workflow while the player keeps read-only diagnostics.

No runtime, JSON, manifest, version, package, installer, enablement, Workshop, BepInEx, QA, hot-path, product, or API implementation changed.

## Source Reviews

- `docs/reviews/code/2026/20260713-0001-major-update-second-decision-docket.md`
- `docs/reviews/code/2026/20260713-0002-installer-package-ownership-receipt-p0.md`
- `docs/reviews/code/2026/20260713-0003-mod-management-source-boundary.md`
- `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
- `docs/reviews/code/2026/20260712-0004-major-update-product-version-decision-docket.md`
- `docs/reviews/api/2026/20260712-0001-oil-coal-native-drop-pool-review.md`
- `docs/reviews/api/2026/20260712-0002-workshop-055-binary-compatibility-review.md`

## Decisions Recorded Versus Open Implementation

Recorded refinements:

- Oil uses append-only official JSON weight 25 as its first development baseline;
- Mine remains official-static-content plus Mine-owned product policy and demand-only native adapters;
- new Workshop Mod plus stale installed Runtime is the primary deployment skew;
- old first-party DLLs remain compatibility regression samples, not permanent architecture owners;
- external BepInEx plugins stay outside DTMAPI management;
- QA exclusion, hot-path pre-work throttling/generation invalidation, protected JSON animals, and non-destructive unknown-plugin handling are engineering constraints.

Closed second-round choices:

- player uninstall is Runtime-only; transaction receipts/cleanup belong to Author SDK/internal deployment tools;
- player content loading is B0 lifecycle-driven; Author SDK implements B1 explicit reload first and may later add B2 development watching; B3 player polling is rejected;
- C2 Catalog-first gradual migration is selected, with a fully classified physical tree as the endpoint;
- D1 establishes one independent versioned Author SDK; the player package keeps read-only Doctor and error guidance.

Implementation remains open. Later decision rounds own CustomEntity scope, the formal product roster, player source precedence/release sequencing, warning windows, and product-specific UX.

## Changed Files

- `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
- `docs/reviews/code/2026/20260712-0004-major-update-product-version-decision-docket.md`
- `docs/reviews/api/2026/20260712-0001-oil-coal-native-drop-pool-review.md`
- `docs/reviews/api/2026/20260712-0002-workshop-055-binary-compatibility-review.md`
- the three 2026-07-13 Reviews above
- this Update
- `docs/updates/INDEX-2026-07.md`

## Validation

- source/docs/native-owner/local-state findings were cross-checked;
- `tools/scripts/check-doc-governance.ps1`: passed after the decision-closure/third-round record changes (`4257` checks);
- `git diff --check`: passed for tracked changes (only the repository's existing LF-to-CRLF working-copy warnings were reported);
- the ten new Review/Update records were checked separately for trailing whitespace: passed;
- no build/runtime validation is required because no implementation or package behavior changed.

## Runtime Evidence

Not required. Existing logs were inspected read-only as source-state evidence; no new game/runtime claim or smoke row was created.

## Rollback

Remove the 2026-07-13 Review/Update records, their monthly row, and the short refinement links added to the earlier Reviews. No source/runtime rollback is required.

## Follow-Up

Treat the A/B/C/D product choices as closed. Build the third-round decision docket, then implement Batch 0 and the two P0 boundary changes as separate Updates. Do not migrate or destructively clean local packages until the player uninstaller is Runtime-only and the Author SDK receipt path has passed its fail-closed temporary-root matrix.
