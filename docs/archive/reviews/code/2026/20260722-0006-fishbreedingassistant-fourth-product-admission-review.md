# FishBreedingAssistant Fourth Product Admission Review

**Review ID:** `20260722-0006`
**Date:** 2026-07-22
**Status:** admitted and runtime-accepted — bounded fourth-product migration only
**Scope:** `Yuuka.DTMAPI.FishBreedingAssistant`; no AnimalHusbandryProgress decision, general Advanced authoring, G7 implementation, Release, L0-L5, long test, or 0.5.5 publication

## Prerequisite

The corrected ActionSpeed package passed the bounded third-save acceptance at `GAME-SMOKE/20260722-141220`. Its three product cases and title lifecycle passed; ActionSpeed and OneActionComplete real Loader deactivation both reduced lifecycle instances, actual Harmony targets/patches, callbacks, and platform roots to zero. The same run closed OneActionComplete's partial-energy, configuration save/reload, and real owner-deactivation evidence gaps. The process, save, configuration, source, profile, and deployment restoration gates passed.

This result closes the preceding product gate without running the complete Release suite, L0-L5, or a long test.

## Native Authority Findings

Build `23762374` establishes the exact display path:

- `DolocTown.ItemFishRoe.fishName` stores the parent fish item ID and validates it against the native farm-fish table.
- `DolocTown.Item.title` returns the base item proto title.
- `DolocAPI.QueryItemProto(string, out ItemInfo)` resolves the parent fish proto, whose `Title` is the localized fish name.

The shareable `Generated/FishBreedingLookup.g.cs` contains no rows and always returns false. It is therefore a placeholder, not a data authority. Current visible behavior succeeds only because GameBridge falls back to the native parent-fish proto title.

## Ownership Decision

| Boundary | Owner | Decision |
| --- | --- | --- |
| Empty public fish-breeding lookup | no production owner | Retire it from the game-loaded product. It cannot support a runtime or API claim. A future clean-room incubation/growth dataset is declarative ContentOwner/G7 work and requires its own authority. |
| `ItemFishRoe.fishName -> QueryItemProto(...).Title` fallback and cache | provisional FishBreedingAssistant ProductNative | At admission this had one consumer and was not promoted. The later Animal migration established a second independent consumer of the exact same native title owner; Review `20260722-0008` supersedes only this lookup placement with the minimal read-only `IItemDisplayNameApi` SharedNative adapter. |
| `Item.get_title` Postfix and duplicate marker guard | FishBreedingAssistant ProductNative | Install one product-owner Hook atomically; preserve vanilla text and append one localized native fish title marker only for `ItemFishRoe`. |
| `Item.get_description` and `Item.GetDetailInfo` Hooks | frozen `IItemTooltipApi` Compatibility | The admitted product forces details off and does not consume these paths. Retain them only for already-built ABI consumers and install them only on real compatibility demand. |
| ConfigMenu, Loader/Manager, SDK/package/Doctor/Catalog, owner cleanup | Platform | Reuse the existing generic Advanced-product mechanisms; do not add a Fish-only builder or receipt family. |

## Compatibility And Lifecycle Gates

- Preserve `IItemTooltipApi`, `FishRoeTooltipOptions`, and `FishRoeDisplayInfo` source/binary boundaries for 0.5.5, but mark the executor Deprecated/Frozen and keep it demand-inactive.
- Product-first compatibility requests must fail before retaining provider state or demand.
- Compatibility-demand-first must synchronously remove pending provider state before GameBridge installs its title/detail Hooks. If compatibility already physically owns `Item.get_title`, the product must fail closed and require restart.
- The product owns its native cache, callback attachment, exact Harmony owner, and local lifecycle flags. Core owns platform event/config roots during Manager deactivation.
- Hook installation is all-target-pre-resolved and exact-owner rollback/unpatch; a failed fallback returns vanilla title text.

## Admission Decision

Admit only `Yuuka.DTMAPI.FishBreedingAssistant` as the fourth real managed Advanced ProductNative CodeMod. Preserve UniqueID `Yuuka.DTMAPI.FishBreedingAssistant`, Workshop ID `3742763706`, version `1.1.3-dtmapi`, canonical config path, current enabled/cache settings, and title-only player behavior. Build and package it only through the tracked build-`23762374` policy and the shared Catalog-driven Advanced builder.

This admission does not promote fish data, the native proto query, or item-title decoration to SharedNative. There is one real consumer and one product-specific display rule.

## Focused Acceptance

1. Product source contains no `IItemTooltipApi`, compatibility executor dependency, direct native proto query, or placeholder lookup execution; its only GameBridge dependency is the owner-bound `IItemDisplayNameApi` admitted after the two-product comparison.
2. Catalog/SDK/package/Doctor/Manager and live zero-leftover checks recognize the fourth product through generic metadata.
3. Unit coverage proves both compatibility/product load orders fail closed and no unrelated demand is removed.
4. A bounded third-save smoke loads the Author SDK Advanced DLL, observes exactly one product owner Hook, renders `鱼卵 (鱼)` through the native fallback, then proves title/owner deactivation/restoration/exit.

## Implementation Link

Implementation lifecycle and validation are owned by [FishBreedingAssistant Fourth Advanced Product](../../../updates/2026/20260722-0002-fishbreedingassistant-fourth-advanced-product.md).

## Post-migration Disposition

The product passed its original direct-fallback implementation in `GAME-SMOKE/20260722-145154`. After AnimalHusbandryProgress became the second real consumer, the native lookup moved to the shared adapter without moving Fish formatting, its title Hook, the empty data table, cache policy beyond display names, or any QA assertion. Focused checks and the rebuilt package pass. The `153320`/`153845` runner windows and delayed title start remain Steam cloud-conflict infrastructure evidence only. After the user resolved that conflict, `GAME-SMOKE/20260722-161022` loaded the current package, exercised the shared lookup, rendered `鱼卵 (鱼)` with exactly one owner patch/target, and reduced the product instance, actual patch, callback and Core roots to zero through real Loader deactivation. Save/config/profile/QA restoration and clean exit passed.

Post-review supersession: `GAME-SMOKE/20260722-180502` is the current corrected-package Fish behavior and owner-cleanup boundary. Review `20260722-0009` later found shared ItemDisplayName EnvironmentReset fanout/callback-gate gaps; follow-up review found the pre-Hook cache window. Update `20260722-0004` closes them with focused Hook-ready fail-closed and production-static-callback Units and does not revoke this product admission.
