# 20260721-0003: AutoFishing Product Slimming Route

## Metadata

- Update ID: `20260721-0003`
- Date: `2026-07-21`
- Lifecycle Status: `verified`
- Validation Level: `docs`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Area: planning/autofishing/product-native/lightweight/config-menu/validation/release
- Source: User request to quantify AutoFishing and DolocPlus AFK fishing, explain the remaining player-side weight, confirm shared settings registration, pause 0.5.5 publication, and reduce long/full validation.
- Related Review: [AutoFishing Product Weight And Config Reuse Review](../../reviews/code/2026/20260721-0004-autofishing-product-weight-and-config-reuse-review.md)

## Summary

This documentation-only update inserts Checkpoint D.5 between the accepted AutoFishing Advanced migration and any second real-product admission. D.5 keeps the existing product identity and verified behavior while removing dead migration scaffolding, making QA-only observation optional, and folding redundant single-owner native layers.

It also distinguishes the 6,266-line player product from its excluded 8,348-line QA tree and the separate 4,164-line frozen Runtime compatibility path. The existing DTMAPI ConfigMenu remains the one reusable settings route. The user has paused 0.5.5 publication, full Release runs, L0-L5 gradients and long soaks unless changed risk later justifies them.

## Decisions

- AutoFishing remains one product; no `Lite` package, alternate Workshop identity, new Runtime API, receipt family or assurance system is created.
- D.5 starts with high-confidence dead code and QA-only diagnostics, then folds duplicate session/router/lease/transaction/cache layers one native responsibility at a time.
- Behavior, configuration, Workshop identity, old ABI boundary, fail-closed hook installation, duplicate-settlement protection and native/input/animation restoration are preservation gates.
- `IDtmConfigMenuApi` remains the unified settings registration. AutoFishing already uses it, and the next product should use the same owner-bound route.
- Per-slice validation is focused. One short fifth-save enable/disable/title/recovery run is reserved for the final changed product candidate. Full Release, L0-L5 and long loops remain out of scope by default.
- AutoFishing acceptance does not admit a second product. After D.5, a separate bounded decision may admit only one second pilot, with `OneActionComplete` as the current recommendation.
- Only after two real products exist will the project compare their settings glue, lifecycle, Harmony owner, SDK/package, Doctor/Manager and QA seams for genuine shared ownership.
- AutoHarvest remains an external consumer/API research input, not a planned first-party publication step.

## Changed Files

- `docs/reviews/code/2026/20260721-0004-autofishing-product-weight-and-config-reuse-review.md`
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`
- `docs/planning/DolocTownModdingAPI.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/architecture/batch6-managed-mod-identity-contract.md`
- this Update and `docs/updates/INDEX-2026-07.md`

No source, product asset, package, Runtime, game directory, save, local official `MODS`, subscription folder or Workshop state changed.

## Validation

- `tools/scripts/check-doc-governance.ps1`: passed (`Document governance: OK`, 5,537 checks).
- Scoped `git diff --check` for the seven owned documentation paths: passed; only existing Git line-ending notices were emitted.
- Product build, unit tests, complete Release suite, game smoke, L0-L5 and long soak: intentionally not run because this update changes documentation and route only.

## Evidence

- Current Author SDK input compiles only `products/first-party/AutoFishing/src`: 23 C# files, 6,266 physical lines; the separate `qa` tree is 23 files and 8,348 lines.
- Git rename detection across the accepted migration identifies 14 Native files retained at 94%-99% similarity, totaling 4,481 current lines (71.5% of player product source).
- The local DolocPlus v1.3.2 package contains no source. Temporary ILSpy output measures two direct AFK fishing files at 227 physical lines; the full 28-feature DLL decompiles to about 5,876 lines. The comparison is directional because its features and lifecycle guarantees are narrower.
- AutoFishing obtains `IDtmConfigMenuApi` from `DTMAPI.ModConfigMenu` and registers its options through the shared owner-bound registry; no private settings UI is present.
- The accepted AutoFishing implementation and runtime evidence remain owned by [20260720-0008](20260720-0008-batch6-autofishing-advanced-pilot.md); this route change does not replay or replace them.

## Related Records

- Debug: none; no runtime symptom or evidence changed.
- Hook map: none; no hook target, owner or lifecycle changed.
- Smoke matrix: none; the game did not run.
- API matrix: unchanged; the ConfigMenu stability wording gap is a later author-documentation task, not a new API decision here.

## Rollback Notes

Revert only the seven documentation paths listed above. Do not revert the accepted Batch 6 product implementation, Runtime, packages or evidence. Rollback restores the prior route wording but does not make its stale AutoFishing status true.

## Follow-Up

Execute D.5 through small reversible source slices under one implementation Update. When its final short fifth-save regression passes, decide whether to admit exactly one second real-product pilot. If admitted, migrate `OneActionComplete`, then compare the two products before proposing any new shared Runtime/API layer.
