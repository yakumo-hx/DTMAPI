# 20260713-0002 Second-Round Closure And Third Decision Docket

## Metadata

- Update ID: `20260713-0002`
- Date: 2026-07-13
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Source: user supplied `D:/下载/第二轮小讨论.md`, then resolved E/F/G/H/I through `D:/下载/第三轮.md` and asked to continue with a fourth decision round

## Summary

Closed the second decision docket with separate player and author-tool boundaries. Player uninstall is Runtime-only and player content loading is startup/lifecycle-driven with zero file polling. Transaction receipts/verified cleanup, explicit content reload, and any later development watcher belong to the unified independent Author SDK. Catalog-first gradual migration remains selected and targets a fully classified physical tree.

Created the third decision docket plus focused CustomEntity and product-catalog fact reviews. The C# CustomEntity umbrella has 97 public types but no native runtime creation; the working JSON animal route is separate. Current release scripts label only eight products Published, while local distribution evidence establishes eleven Workshop product identities and shows AutoFishing, MoreEquipmentSlots, and Manbo are incorrectly treated as DeveloperOnly.

The user closed the third round with E1/F1/G1/H1/I1. G1 adds explicit Author-SDK modes for player/Workshop, per-Mod local development, Workshop validation, and player reproduction. H1 adds a low-risk AutoFishing-or-OneAction Canary between the 0.5.5 Runtime release and other one-by-one 1.0.0 updates. Version naming, real minimums, pre-load rejection, three-way version status, offline/in-game read-only Doctor, external-plugin safety, and post-save official enablement remain existing decisions or engineering constraints.

No runtime, public API, product classification source, Catalog, Mod, package, installer, enablement, Workshop, game, or BepInEx behavior changed.

## Reviews

- `docs/reviews/code/2026/20260713-0001-major-update-second-decision-docket.md`
- `docs/reviews/code/2026/20260713-0002-installer-package-ownership-receipt-p0.md`
- `docs/reviews/code/2026/20260713-0003-mod-management-source-boundary.md`
- `docs/reviews/code/2026/20260713-0004-first-party-product-catalog-fact-review.md`
- `docs/reviews/code/2026/20260713-0005-major-update-third-decision-docket.md`
- `docs/reviews/api/2026/20260713-0001-customentity-public-promise-review.md`

## Changed Files

- new CustomEntity, product-catalog, and third-docket Reviews;
- closure edits to `docs/reviews/code/2026/20260713-0001-major-update-second-decision-docket.md` and `docs/reviews/code/2026/20260713-0002-installer-package-ownership-receipt-p0.md`;
- the existing Mod-management Review is referenced but unchanged;
- `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`;
- `docs/updates/2026/20260713-0001-major-update-second-decision-docket.md`;
- this Update;
- `docs/updates/INDEX-2026-07.md`.

## Validation

- source/API/release/local-state findings were cross-checked;
- `tools/scripts/check-doc-governance.ps1`: passed again after third-round closure and fourth-round records (`4280` checks);
- `git diff --check`: passed for tracked changes (only the repository's existing LF-to-CRLF working-copy warnings were reported);
- the four new third-round Review/Update records were checked separately for trailing whitespace: passed;
- no build/runtime validation is required because no implementation or package behavior changed.

## Runtime Evidence

Not required. No game process was started, no runtime lock was needed, and no smoke/debug/hook/API completion claim was made.

## Rollback

Remove the new focused Reviews, third docket, this Update/monthly row, and revert the second-round closure edits. No source/runtime rollback is required.

## Follow-Up

Treat E/F/G/H/I as closed. Continue the fourth decision round, then implement Batch 0, player-uninstaller ownership removal, and Oil boundary removal as separate task Updates. G1/H1 implementation still requires source, installer, compatibility, QA, and release evidence; the decision record alone does not authorize a Workshop upload.

## Subsequent Refinement

The formal sixth round in `docs/updates/2026/20260713-0007-sixth-round-closure-and-active-gc-gate.md` retains Runtime-first H1 but replaces the low-risk AutoFishing-or-OneAction choice with V1 revised: AutoFishing is the fixed compatibility/update Canary, OneAction is the structural split template, ActionSpeed follows its focused GC gate, low-user products may share a window with independent rollback, and MoreEquipment releases alone. This Update remains the historical owner of the third-round selection.
