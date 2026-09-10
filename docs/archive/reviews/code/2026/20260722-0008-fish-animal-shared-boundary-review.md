# Fish / Animal Shared Boundary Review

**Review ID:** `20260722-0008`
**Date:** 2026-07-22
**Status:** decided — one minimal SharedNative adapter; all other compared seams remain Platform, ProductNative, Compatibility, ContentOwner or QA
**Scope:** post-migration comparison of `Yuuka.DTMAPI.FishBreedingAssistant` and `Yuuka.DTMAPI.AnimalHusbandryProgress`; no sixth product, G7 implementation, general Advanced authoring, Release, L0–L5, long test or 0.5.5 publication

## Question

After separately admitting both products, which code has two independent real consumers and the same native responsibility owner? Similar source shape, reflection, Harmony use, testability or centralized packaging is insufficient.

## Preconditions And Native Facts

- FishBreedingAssistant needs a localized parent-fish label for `ItemFishRoe.fishName` before its product-owned `Item.get_title` Postfix appends one marker.
- AnimalHusbandryProgress needs localized output-item labels while deriving read-only husbandry progress rows before its product-owned Animal viewer callbacks render clones.
- Both independently reach exactly `DolocAPI.QueryItemProto(itemId).Title` in build `23762374`. Neither owns the item proto table or localization data.
- The shareable fish-breeding lookup contains no rows. It is not a fallback authority and does not become platform data merely because the Fish product once referenced it.

## Decision Matrix

| Compared seam | Physical owner | Decision |
| --- | --- | --- |
| `itemId -> QueryItemProto(...).Title` | SharedNative GameBridge | Promote only `IItemDisplayNameApi.TryGetDisplayName`. It is owner-bound and read-only, exposes no native object or Hook API, and caches successful non-empty strings only after the shared EnvironmentReset Hook owner is ready. It clears on save/title/environment boundaries. |
| Empty fish incubation/growth lookup | no production owner; future ContentOwner/G7 | Do not ship or consult placeholder data. A future clean-room declarative table requires its own content authority and admission. |
| Fish title decoration | Fish ProductNative | Keep the single `Item.get_title` Postfix, `ItemFishRoe` test, localized format, duplicate-marker guard and enabled/config policy in Fish. |
| Animal progress rendering | Animal ProductNative | Keep four patches across three native methods, husbandry thresholds, derived rows, native mood-row cloning, throttle, colors and cleanup in Animal. |
| ConfigMenu registration/glue | existing Platform API plus product registration | No new shared product helper. The provider, ownership and cleanup contract are already Platform; page schema/defaults/callbacks remain product-owned. |
| Save/title/deactivation callbacks | Platform event roots plus ProductNative reset | Core continues to own event/config registration roots. Each product owns its local caches and native state restoration; matching callback shapes do not create one state holder. |
| Harmony owner and installation | separate ProductNative owners | Fish and Animal patch unrelated native targets and have different rollback inventories. Do not introduce a shared Harmony owner or dispatcher. |
| SDK, package, Doctor and Manager | Platform Catalog machinery | Both use the existing Catalog-driven Advanced registry, builder, package, install, Doctor, Manager and live zero-leftover projection. Keep only thin product wrappers and policy metadata. |
| Frozen old APIs | Compatibility | `IItemTooltipApi` and `IAnimalViewerApi` remain unchanged, demand-inactive and both-order fail-closed. They are not shared execution paths for the new products. |
| QA observation | optional QA | External reflection may observe each product callback and real Harmony owner. Product-specific row/title assertions remain QA policy, not a runtime public capability. |

## Accepted Shared Contract

`IItemDisplayNameApi` is intentionally smaller than a general item-query API:

- input is one public string ID;
- output is one display string or `false`;
- failure is fail-open to the product's vanilla/no-decoration path;
- no item proto DTO, metadata enumeration, mutation, Hook or guaranteed content completeness is exposed;
- each owner receives an owner-bound facade; cache lifetime belongs to GameBridge and cannot outlive save/title/environment resets.

The two products are real, independent consumers and the underlying native query owner is identical, satisfying the SharedNative admission rule. Fish-specific fallback wording is retired: GameBridge owns localized item-name resolution, while Fish owns how that name is displayed.

## Rejected Promotions

- A shared product base class would couple unrelated Hook inventories and cleanup state without a common native owner.
- A common Harmony owner would weaken exact-owner deactivation and collision diagnostics.
- A generic reflected-UI clone API is not justified by one Animal consumer.
- A fish-breeding public data table cannot be inferred from empty generated source or native name lookup.
- QA seams and package wrappers are test/tooling reuse, not player Runtime capabilities.

## Validation And Evidence

- Focused Fish and Animal source checks: PASS.
- GameBridge/Unit/QA Unit, Author SDK and InstallDoctor focused tests: PASS.
- Catalog, SDK validate/build/package and live zero-leftover checks: PASS.
- Author SDK ZIP SHA-256: `fe9d4f3cf230bf004e98f15709ade960f08622feaff0c8c25b86750e25febf15`.
- Fish package SHA-256 after shared-adapter migration: `F67D6209C8E824357C092D32D1CE81FD3961F015881B3FD20B47F61068BB8495`; entry SHA-256 `bc612490457edb800a73909ed8f1fcb39b19f0c607e3a1492c1e53adba6ca3ec`.
- Animal package SHA-256: `EF3A84E7C430FC42658E07DCEDF9458D8AA659241828DDBF58F62050C47DC7A2`; entry SHA-256 `101f8186d69f38dea9330f93ca83ee7ec7136d4d809a6ed66511f2a7bb8a2ff6`.
- Fish direct-fallback predecessor behavior passed `GAME-SMOKE/20260722-145154`; `153320`/`153845` remain Steam cloud-conflict infrastructure evidence. After that conflict was resolved, `GAME-SMOKE/20260722-161022` exercised the current shared adapter through both products: Fish rendered `鱼卵 (鱼)` with one patch/target, while Animal rendered a localized `羊毛脂 0/100` read-only row and native-style clone. Native close cleared Animal rows/clones, and real Loader deactivation reduced Fish `1` and Animal `4` patches across `3` targets plus callbacks/instances/Core roots to zero. Restoration and clean exit passed.
- Complete Release, L0–L5, long and GC tests were not run.

## Consequence

At this review checkpoint, the fifth product admission did not authorize a sixth and `GAME-SMOKE/20260722-161022` supplied the shared-adapter predecessor baseline. The successful run validated the minimal shared lookup and both ProductNative consumers without promoting their Hooks, UI, lifecycle, ConfigMenu glue or QA seams.

Post-review supersession: `GAME-SMOKE/20260722-180502` is the current corrected Fish/Animal product-behavior and owner-cleanup boundary. Review `20260722-0009` later found a missing ItemDisplayName fanout; follow-up review also found a Camera-only callback gate and an unprotected pre-Hook cache window. Update `20260722-0004` closes them with a non-disableable SharedNative physical Hook owner, Hook-ready fail-closed caching and focused production-static-callback Unit evidence; the SharedNative ownership decision itself remains unchanged.
