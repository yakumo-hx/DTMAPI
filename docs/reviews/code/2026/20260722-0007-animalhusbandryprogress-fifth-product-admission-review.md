# AnimalHusbandryProgress Fifth Product Admission Review

**Review ID:** `20260722-0007`
**Date:** 2026-07-22
**Status:** admitted and runtime-accepted — bounded fifth-product migration only
**Scope:** `Yuuka.DTMAPI.AnimalHusbandryProgress`; no general Advanced lane, G7, Release, L0–L5, long test, or 0.5.5 publication

## Prerequisite

FishBreedingAssistant passed its bounded third-save acceptance at `GAME-SMOKE/20260722-145154`: the product owned exactly one `Item.get_title` patch, rendered `鱼卵 (鱼)` through native item-title lookup, and Loader deactivation reduced its instance, callback, exact Harmony owner and Core roots to zero. Two earlier `144108`/`144534` attempts never launched the game; `145001` proved the Fish behavior but intentionally failed after ActionSpeed and OneActionComplete were not selected from their local Author sources. None of those rejected runs is product acceptance evidence.

## Native Owner Decision

Build `23762374` establishes the product boundary:

- `AnimalFullInfoData(Animal)` constructs the read-only animal detail DTO and exposes `visible`/`notEmpty` state.
- `AnimalViewer.Show(AnimalFullInfoData)` is the official detail render owner; a Prefix must clear stale product clones before its native render and a Postfix may append the current clones.
- `AnimalPanelUiState.Unregister()` is the real panel-close owner and must release all product clones and DTO-keyed rows.
- `Animal.husbandryValues`, `DolocConfig.Tables.TbHusbandry`, and `DolocAPI.QueryItemProto(...).Title` are read-only ProductNative inputs. The product must not mutate animal values, breeding, AI, host membership, save data, or native official rows.

Admit the product with four atomic patches across those three methods. The cloned rows, DTO-key map, refresh throttle, caches and cleanup belong to AnimalHusbandryProgress ProductNative. ConfigMenu, Loader/Manager, SDK/package/Doctor/Catalog, owner cleanup and event roots remain Platform.

## Compatibility Decision

Preserve the existing `IAnimalViewerApi` ABI and its four-Hook executor as frozen demand-inactive Compatibility for already-built consumers. Product-first compatibility requests fail before retaining policy/demand. Compatibility-first pending demand is synchronously removed before GameBridge can install Hooks. If the compatibility owner already physically owns any reviewed target, the product fails closed and requires restart.

## Shared Boundary Candidate

FishBreedingAssistant and AnimalHusbandryProgress independently require the same read-only native responsibility: resolve an item ID through `DolocAPI.QueryItemProto` and return its localized `Title`. That exact query is the only new two-consumer/common-native-owner candidate. ConfigMenu glue, product lifecycle callbacks, Harmony owner IDs, Hook targets, caches, UI clones and QA assertions are similar shapes but not a common native owner.

The shared-title candidate is not approved by this admission alone. The post-migration comparison must prove both real consumers and then either promote only a narrow read-only GameBridge adapter or retain both ProductNative lookups with an explicit reason. Review `20260722-0008` completed that comparison and approved only `IItemDisplayNameApi`; every Hook, state machine, row/clone cache and formatting rule remains ProductNative.

## Focused Acceptance

1. Product source owns exactly four atomic patches and no `IAnimalViewerApi` or old GameBridge executor dependency.
2. Catalog/SDK/package/Doctor/Manager and live zero-leftover checks recognize the fifth product through the generic Advanced path.
3. Unit tests prove both owner load orders fail closed without removing unrelated demand.
4. One bounded third-save smoke observes a real configured animal progress row, visible cloned UI, exact product Harmony owner, native close cleanup, Loader owner deactivation, restoration and clean exit.

## Implementation Link

Implementation lifecycle is owned by [AnimalHusbandryProgress Fifth Advanced Product](../../../updates/2026/20260722-0003-animalhusbandryprogress-fifth-advanced-product.md).

## Runtime Disposition

The focused source, unit, policy, Catalog, SDK, Doctor and package gates pass. The `153320`/`153845` windows and delayed title-only start remain Steam cloud-conflict infrastructure evidence; they are not product failures or acceptance. After the user resolved that conflict, `GAME-SMOKE/20260722-161022` loaded the third save and the current receipt-verified DLL with four patches across three targets. Optional QA observed 20 constructed animals, a visible product-owned `羊毛脂 0/100` read-only row, one native-style overlay row and a 434,758-byte screenshot, with `mutation=false`; native panel close reduced product rows/clones to zero. Final real Loader deactivation reduced the Animal instance, four actual patches/three targets, callback and Core roots to zero. Save/config/profile/QA restoration, no-fatal and process exit passed, so the fifth product is runtime-accepted.

Post-review supersession: `GAME-SMOKE/20260722-180502` is the current corrected-package Animal behavior, native-close and owner-cleanup boundary. Review `20260722-0009` later found shared ItemDisplayName EnvironmentReset fanout/callback-gate gaps; follow-up review found the pre-Hook cache window. Update `20260722-0004` closes them with focused Hook-ready fail-closed and production-static-callback Units and does not revoke this product admission.
