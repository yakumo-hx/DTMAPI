# 20260712-0009 Major Update Product And Version Decision Docket

## Metadata

- Update ID: `20260712-0009`
- Date: 2026-07-12
- Lifecycle Status: `verified`
- Validation Level: `docs, source`
- Runtime Validation: `not-required`
- Related Issue State: `open`
- Source: user clarified and then confirmed Oil/Mine ownership, the 0.5.5 plus formal-Mod 1.0.0 version epoch, compatibility and pre-public naming requirements, and the lower crop API/no-first-party-AutoHarvest boundary

## Summary

Recorded and resolved the first major-update product decision round.

The record corrects Oil/Mine from shipped-product language to unpublished DeveloperOnly prototypes while retaining the finding that product-specific Oil behavior leaks into base GameBridge. It proves official JSON can add Oil to the native coal-mine drop LUT, distinguishes that weighted native behavior from the current independent 8% grant, recommends a two-layer Mine, and keeps the real held-preview defect open.

It fixes public DTMAPI at `0.5.5` with generated file version `0.5.5.0`, public naming as `DTMAPI`/`DTMAPI 前置`, one product-version authority, and non-rewritten minimum requirements. It records the user's explicit one-time formal-product epoch in which every formal functional Mod starts at plain `1.0.0`, while compatibility with already subscribed DTMAPI-dependent Mods remains a separate requirement. The 0.5.5 framework assembly compatibility identity remains `0.5.3.0` pending real old-DLL Unity Mono proof.

It closes AutoHarvest as a non-published compatibility/API demand sample, rules out a first-party AutoHarvest product, and selects a lower crop-container DTO/transient-handle/revalidation/single-target `TryHarvest` bridge instead of the current whole-farm operation.

The follow-up Oil native-owner review proves that the fish Mod's rarity-bucket structure cannot be copied to the coal mine's flat LUT. Its rebalancing principle can be used: Oil becomes a native rare output funded from ordinary coal probability while amber's original share is explicitly preserved. Unlimited native Oil count participates in the coal resource's native 3-4 draws and stone-collection count bonus. A truly independent extra roll remains outside official flat-LUT JSON and is not selected.

The local Workshop compatibility audit found 12 subscribed Code Mod DLLs referencing `DTMAPI.Abstractions` 0.5.1.0 or 0.5.2.0 and one confirmed current public-member deletion: Workshop AutoFishing calls `FishingAutomationOptions.set_StopOnManualMove`. Restoring and validating this member, freezing the old provider/public identities, separating the 0.5.5 public/file version from the 0.5.3.0 assembly compatibility identity, and running an old-DLL Unity Mono matrix are now explicit 0.5.5 release gates.

No source, manifest, API, version, product identity, JSON, Hook, package, game, or Workshop implementation changed.

## Source Review

- `docs/reviews/code/2026/20260712-0004-major-update-product-version-decision-docket.md`
- `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
- `docs/reviews/api/2026/20260610-dtmapi-version-compatibility-policy.md`
- `docs/reviews/api/2026/20260612-crops-harvesting-native-responsibility.md`
- `docs/reviews/api/2026/20260607-0008-native-owner-special-audits/03-machine-production.md`
- `docs/reviews/api/2026/20260712-0001-oil-coal-native-drop-pool-review.md`
- `docs/reviews/api/2026/20260712-0002-workshop-055-binary-compatibility-review.md`

## Decisions Recorded Versus Open

Confirmed user decisions:

- Oil/Mine are unfinished, unpublished paired prototypes and require boundary splitting.
- official JSON should lead content/static definitions where supported.
- delete the GameBridge Oil hard-code/service/Hook route and OneAction coupling; use official native drop JSON with amber-preserving probability rebalance;
- continue the two-layer Mine, with unverified electricity retained only as a later rewrite/validation target and with temporary real-preview repair followed by dedicated art/no 2x runtime scale;
- use plain DTMAPI `0.5.5`, generated file projection `0.5.5.0`, and a separately frozen `0.5.3.0` assembly compatibility identity for this release;
- explicitly reset all formal functional Mods to plain `1.0.0` as a new product epoch while preserving compatibility for existing DTMAPI-dependent Workshop Mods;
- use public `DTMAPI` / `DTMAPI 前置` wording and complete a compatibility-aware full naming/readability audit before broad public release;
- expose only lower crop query/transient-handle/revalidation/single-target harvesting primitives and ship no first-party AutoHarvest.

No first-round product-visible choice remains open. Oil's exact economic weight/tolerance, the concrete legacy compatibility inventory/window, the formal-product roster, and the naming-audit schedule remain implementation inputs.

## Changed Files

- `docs/api/public-api-matrix.md`
- `docs/reviews/code/2026/20260712-0003-dtmapi-full-boundary-audit.md`
- `docs/reviews/code/2026/20260712-0004-major-update-product-version-decision-docket.md`
- `docs/reviews/api/2026/20260712-0001-oil-coal-native-drop-pool-review.md`
- `docs/reviews/api/2026/20260712-0002-workshop-055-binary-compatibility-review.md`
- this Update
- `docs/updates/INDEX-2026-07.md`

## Validation

- focused source/manifests/official JSON/release/native-owner/manual-QA/version-policy citations were cross-checked;
- local fish-extension visible JSON semantics were compared with the current public-build `DolocAPI.RollFish`, `GuaranteedManager.SpawnResourceDropItems`, `ISpawnLut.SpawnInternal`, `SpawnData.Unlimited`, and Mod item-spawn extension method bodies;
- local subscribed manifests/managed references and the retained 0.5.2-to-current Abstractions public surface were audited; one deleted legacy fishing option remains an implementation/runtime release blocker;
- `tools/scripts/check-doc-governance.ps1`: passed, 4,211 checks;
- `git diff --check`: passed; only the existing Windows line-ending notice was emitted;
- no build/runtime validation was required because no source or package behavior changed.

## Runtime Evidence

Not required. Existing Mine placement-preview and production evidence remains historical scope evidence; no new player/game claim is made.

## Rollback

Remove this Update row/record, the decision Review, the two focused API reviews, and the short clarification link added to the full audit. No source/runtime rollback is required.

## Follow-Up

Create implementation Updates in the established Batch 0/1/2 order. Do not combine version projection, Oil/Mine behavior, crop API replacement, naming audit, and UI work in one implementation batch. Oil implementation must first select/test its economy target. Version implementation must first restore the confirmed old-DLL API deletion and preserve the compatibility identities recorded by the compatibility review; 0.5.5 remains blocked from release, not from implementation, until the old-binary Unity Mono matrix passes.
