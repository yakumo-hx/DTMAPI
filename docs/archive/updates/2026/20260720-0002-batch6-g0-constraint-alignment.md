# 20260720-0002: Batch 6 G0 Constraint Alignment

## Metadata

- Update ID: `20260720-0002`
- Date: `2026-07-20`
- Lifecycle Status: `verified`
- Validation Level: `docs`
- Runtime Validation: `not-required`
- Related Issue State: `none`
- Area: architecture/batch6/g0/constraints/ownership/mod-identity
- Source: User request to make a small correction to active constraint documents based on Batch 6 prerequisite Review 0012.
- Related Reviews: [Batch 6 Boundary Correction Prerequisite](../../reviews/code/2026/20260719-0012-batch6-boundary-correction-prerequisite.md); [Batch 5 And Batch 6 Prerequisite Audit](../../reviews/code/2026/20260720-0001-batch5-and-batch6-prerequisite-audit.md)

## Scope

Align active project-direction and workflow documents with the corrected physical-ownership model without changing Runtime, Loader, Author SDK behavior, public API, packages, products, game files or Workshop state.

`PROJECT.md` becomes the single normative owner for the four managed/external identities and four physical-ownership categories. Other current documents reference or execute that classification instead of independently repeating the former universal rule that every Unity/Harmony/native implementation belongs in mandatory GameBridge.

This is only the documentation-authority slice of G0. It does not mark G0 complete: current `CodeMod` remains Strict, `SDK160` remains enforced, Advanced manifest/Loader/SDK/Doctor/Manager/package/Runtime support remains blocked for the dedicated G2 vertical slice, and the general optional Content Host declaration/loading split remains blocked under G7.

## Decisions Applied

- Preserve stable public API isolation: `DTMAPI.Abstractions` does not expose raw Unity, Harmony, BepInEx or decompiled game types.
- Classify native work before implementation: Platform stays in platform components; proven multi-consumer SharedNative enters GameBridge; single-product ProductNative targets a managed Advanced CodeMod; ContentOwner targets an optional Content Host.
- Keep every currently supported `CodeMod` in the Strict lane. Do not bypass `SDK160`, invent a manifest kind, hand-package an Advanced product or begin AutoFishing migration from this docs change.
- Keep DTMAPI-managed Strict/Advanced Mods under `Mods/`; only the DTMAPI Bootstrap is DTMAPI-owned under `BepInEx/plugins`. Third-party plugins there are External and outside DTMAPI ownership promises.
- Correct Review 0012's execution order: G0 precedes G2 Runtime; a minimal G2 fixture precedes the sole AutoFishing pilot; other products wait for G0-G7. AutoFishing native behavior and GC use the fifth save.
- Scope the Batch 5 five-DLL/fragile-GameBridge invariant to its current-0.5.5 acceptance tree rather than treating it as a permanent ProductNative rule.

## Changed Files

- `PROJECT.md`: canonical Strict/Advanced/ContentPack/External definitions, Platform/SharedNative/ProductNative/ContentOwner rules, current Advanced blocker and domain fixture exception.
- `AGENTS.md`: branched development model, ownership classification, current G2 blocker, managed-Mod installation boundary, Unity Mono target and save-fixture rule.
- `docs/workflows/codex-api-rebuild.md`: native-owner-first classification, physical-owner Phase 3, visibility-vs-ownership warning and authoritative fixture validation.
- `docs/planning/DolocTownModdingAPI.md` and `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`: dated correction of the original universal GameBridge wording, diagnostic ownership and unresolved G2 reference-supply mechanism.
- `docs/onboarding/current-state.md`, `README.md` and `src/README.md`: current entry-point routing and short architecture summaries.
- `author-sdk/README.md` and `author-docs/README.md`: current Strict-only SDK/content-author scope and explicit unimplemented Advanced warning.
- `docs/reviews/api/native-owner-domains/INDEX.md` and `docs/reviews/api/local-mods-native-owner/INDEX.md`: physical-owner classification before adapter/API work.
- `docs/updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md`: dated scope clarification without rewriting its historical Batch 5 implementation facts.
- `docs/reviews/code/2026/20260719-0012-batch6-boundary-correction-prerequisite.md`: audited G0/G2 order, G2/G3 staged gate, reproducible G1 baseline and fifth-save correction.
- This Update and `docs/updates/INDEX-2026-07.md`: implementation lifecycle and monthly navigation.

## Validation

- `tools/scripts/check-doc-governance.ps1` passed all 5,327 checks after final metadata synchronization.
- A local relative-link scan passed for all 14 changed documentation entry/authority files.
- Focused G0 assertions passed: `PROJECT.md` owns all eight canonical identity/ownership terms; the universal GameBridge rules are absent from AGENTS/API workflow; no undefined `Platform-native` category remains; G0 design-only, G2 staging, fifth-save AutoFishing, Advanced/G7 blockers and retained `SDK160` are all present.
- `git diff --check` exited `0`; the only output was existing LF-to-CRLF normalization warnings.
- Two independent read-only final reviews found no remaining P0/P1, broken link, metadata mismatch, capability overclaim or G0/G2 cycle.
- No build, unit, game or runtime validation is required because no executable source, manifest schema, SDK policy, package or runtime behavior changed.
- The shared Runtime lock is not required; no game directory, local official `MODS`, subscription content or Workshop upload folder is touched.

## Rollback

Revert only the constraint-document edits, remove this Update and its monthly row, and restore Review 0012's prior wording. Do not alter Runtime, SDK, packages or historical evidence. A rollback would restore the known conflicting universal-GameBridge guidance and therefore requires a replacement scalable physical-owner decision.

## Follow-Up

1. Finish G0 outside this docs slice by aligning the remaining manifest, Loader, SDK, Doctor, Manager, package and UI **design/vocabulary authority only**; do not implement their Advanced path under G0.
2. Build G1's reproducible ownership/assembly budget receipt.
3. After G0 passes, implement the atomic G2 minimal Advanced vertical fixture; do not begin with a real product.
4. Admit only AutoFishing after that fixture, use the fifth-save native/GC baseline, and require a zero ProductNative delta in mandatory Runtime.

## Result

Verified as a documentation-only G0 authority slice. Active constraint documents now use one physical-ownership model and the corrected staged gate. G0 is not fully passed, G2/G7 remain unimplemented and blocked, and no product migration is authorized.

## Resolution

This slice's remaining G0/G1/G2-design follow-up was completed later on 2026-07-20 by [20260720-0003 Batch 6 Phase 0 Closure](20260720-0003-batch6-phase0-closure.md). This historical slice did not itself implement Advanced Runtime; G2 Runtime and every product migration remain governed by that later contract.
