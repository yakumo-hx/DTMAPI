# Seventh Product ChestLocatorEnhancer Admission Review

**Review ID:** `20260723-0005`

**Date:** 2026-07-23

**Status:** recorded — GO for ChestLocatorEnhancer as the only admitted seventh
product; implementation and acceptance have not started

**Scope:** independent admission-only comparison of ChestLocatorEnhancer, Zoom
and MoreEquipmentSlots across native ownership, mandatory-Runtime removal,
frozen-ABI cost, player-data risk and UI/Hook risk; no implementation, Update,
policy, Catalog, package, receipt, gate, game launch or release change

## Source Request

After the Phase 1 Core and Phase 4 public-API tails closed, perform one
independent seventh-product admission decision. Evaluate one product at a time,
prefer ChestLocatorEnhancer, keep Zoom and MoreEquipmentSlots separate, and do
not create a new Host, receipt family or public API.

## Independent Verdict

**GO: admit only `DTMAPI.ChestLocatorEnhancerMod` as the seventh Advanced
product for a bounded ProductNative extraction.**

The decision admits an implementation task; it does not claim that the product
has moved, that its Advanced package exists, that the old Runtime executor is
dormant, or that any focused or game acceptance has passed. The exact
six-product verified baseline remains the current implemented baseline until a
separate Update owns and verifies this extraction.

ChestLocatorEnhancer has the best current combination of a single patch target,
one real product consumer, measurable mandatory GameBridge product logic, no
save sidecar and no UI clone. Zoom retains a genuinely shared Camera owner, so
its apparently large bridge count is not a credible movable-product count
without another boundary review. MoreEquipmentSlots has greater theoretical
removal value but materially higher save, equipment-restoration and UI risk and
therefore remains last.

## Candidate Comparison

| Candidate | Mandatory Runtime / removal confidence | Native owner and ABI cost | Player-data and UI/Hook risk | Disposition |
| --- | --- | --- | --- | --- |
| **ChestLocatorEnhancer** | Current mandatory GameBridge boundary is **542 physical / 475 non-empty lines**; its service alone is **426 / 378**. A thin proxy and coordination will remain, so the net default-loaded reduction must be measured after implementation. | One `ArchiveDataHandle.GetAvailableInventories(Vector2Int, Vector2Int, bool)` Postfix. The frozen `IChestLocatorEnhancerApi` executor can use the existing Compatibility Host and existing ABI authority. | No sidecar, no native inventory mutation, no cloned UI. Medium Hook/conflict risk because the result changes the inventories visible to official Count/Cost consumers. | **GO; sole seventh product.** |
| Zoom | Earlier inventory counted 1,513 GameBridge lines and 221 product lines, but Camera lifecycle, arbitration and view ownership are already genuinely shared. The product-removable subset is therefore unproven and may be small. | Shared Camera owner and Experimental `ICameraViewApi`; deleting the adapter to improve a line count would violate ownership. | Low player-data risk, but global camera/fog/background lifecycle and arbitration remain coupled. | Not admitted; revisit only after a product-specific movable boundary is proven. |
| MoreEquipmentSlots | Earlier inventory counted 2,814 GameBridge lines and 92 product lines, giving the largest theoretical reduction. | Equipment-slot native state and frozen `IEquipmentSlotsApi` require their own high-risk migration. They do not share Chest's inventory-query owner. | Highest risk: per-save sidecars, equipment/overflow recovery, hats/shields, cloned UI and hot-disable cleanup. | Not admitted; keep as a separate final candidate. |

These figures are source-line inventory, not DLL, total-repository, download or
Workshop-package reduction. Chest implementation may claim only the measured
default-loaded Runtime reduction after the final mandatory/optional boundary is
known.

## Unique Native Owner And Actual State Holder

The unique native responsibility function is:

```text
DolocTown.GameData.ArchiveDataHandle.GetAvailableInventories(
    Vector2Int anchor,
    Vector2Int area,
    bool useBox)
```

That method constructs the `LinearInventory[]` consumed by the game's native
item Count/Cost paths. It begins with the backpack, scans the current
`IEquipmentHost`, selects nearby or shared `Case` and `StorageShelf` instances,
and includes `ItemBox.inventory` according to native `useBox` and
`DolocAPI.userSettings.autoUseBox` policy.

The authoritative item state is not held by DTMAPI. It remains in the loaded
room/building equipment graph (`IEquipmentHost.AllEquipments`) and in the
native `LinearInventory` instances owned by `Case.inventory`,
`StorageShelf.inventory` and `ItemBox.inventory`. The product owns only the
policy that widens which already-native inventories are appended, plus its
configuration, diagnostics, patch lifecycle and exact owner.

The implementation must continue to return only native `LinearInventory`
instances. It must not take ownership of inventory contents, item DTOs,
CountItem/CostItem transactions, persistence or UI.

## Real Consumer

The only current real product consumer is
`DTMAPI.ChestLocatorEnhancerMod`, currently sourced from
`testmods/ChestLocatorEnhancerMod`. Repository source, Catalog and local
native-owner inventories contain no second independent product that consumes
this widening policy.

Frozen third-party ABI consumers may still call the public interface, but they
are compatibility obligations, not a second real product and do not create a
SharedNative ownership case.

Identity preserved by a later implementation:

| Fact | Frozen value |
| --- | --- |
| UniqueID | `DTMAPI.ChestLocatorEnhancerMod` |
| Workshop item | `3742765514` |
| Official folder | `DTMAPI_ChestLocatorEnhancer` |
| Package DLL | `DTMAPI.ChestLocatorEnhancer.dll` |
| Canonical config | `DTMAPI/config/DTMAPI.ChestLocatorEnhancerMod.json` |
| Current published version | `0.3.1-dtmapi` |
| Admitted source target | `1.0.0`, minimum DTMAPI `0.5.5` |
| Save sidecars | none |

The existing Author SDK and tracked policy registry must generate the Advanced
identity. This Review does not authorize a hand-written Advanced manifest or
package.

## Mandatory Runtime Removal Boundary

The current mandatory files measure:

| File | Physical | Non-empty |
| --- | ---: | ---: |
| `ChestLocatorEnhancerFeature.cs` | 66 | 53 |
| `ChestLocatorEnhancerHookBridge.cs` | 50 | 44 |
| `ChestLocatorEnhancerService.cs` | 426 | 378 |
| **Mandatory GameBridge total** | **542** | **475** |

The current product `ModEntry.cs` is 101 physical / 89 non-empty lines. The
426/378-line service is the clear heavy executor candidate, but the extraction
must retain a small mandatory provider/demand proxy and exact owner
coordination. Therefore **426/378 is not the promised net reduction**. The
implementing Update must report before/after physical and non-empty counts of
the mandatory boundary and a live zero-leftover result.

Moving the old executor to the optional component reduces default-loaded
Runtime code only. It does not reduce the total repository, installed download
or shipped package because frozen compatibility remains distributed.

## Frozen `IChestLocatorEnhancerApi`

`IChestLocatorEnhancerApi`, `ChestLocatorEnhancerOptions`,
`ChestLocatorEnhancerRegisterResult` and `ChestLocatorEnhancerState` remain
Experimental frozen compatibility contracts. Preserve their provider identity,
metadata, signatures, DTO members and retained-consumer MemberRefs; do not
stabilize or expand them.

The old executor must move into the **existing single dormant-shipped
Compatibility Host**. Mandatory GameBridge may retain only the thin provider,
demand and lifecycle proxy needed to activate that Host for a real old-ABI
request. Do not add another Host, component, receipt/schema/builder/checker
family or public API.

Because the ProductNative DLL and frozen executor target the same method, their
coordination is a hard admission condition:

- **product first, old ABI second:** the compatibility request must observe the
  exact product Harmony owner and fail closed or use the existing coordination
  path without installing a second Postfix;
- **old ABI first, product second:** product startup must observe the exact
  compatibility patch on the target and fail closed before installing its own
  Postfix, or complete one atomic ownership handoff; there must never be one
  frame with two widening executors;
- failed install and failed restoration must still attempt exact-owner unpatch
  and leave an explicit failed state rather than an ambiguous dual owner;
- removing one owner must not tear down the other owner or leave callback,
  demand, owner-state or patch residue.

Both orders require focused executable tests. Testing only the product-first
order is insufficient.

## SharedNative Decision

The extraction can and must add **zero SharedNative capability**. There is one
real product consumer and one product-specific widening policy. Existing
Platform responsibilities already cover Loader lifecycle, Harmony ownership
observation, Author SDK, Catalog, Doctor/Manager, package construction and the
single Compatibility Host.

Reflection reuse, a common provider facade, frozen consumers or future
possibilities do not establish a second native owner or consumer. The new
ProductNative implementation may directly own the one Postfix and native graph
scan; no inventory-query API, DTO, shared scan service or new GameBridge adapter
is admitted.

## Minimum Focused Implementation Checks

A later implementation needs the existing focused mechanisms only:

1. Product build and Unit checks for exact
   `GetAvailableInventories(Vector2Int, Vector2Int, bool)` targeting, atomic
   one-Postfix installation, native-inventory deduplication, current/root/farm
   room traversal, shared Case and optional StorageShelf/ItemBox policy, config
   reload, title/owner cleanup and exception-safe exact-owner unpatch.
2. Dual-order Product/Compatibility tests that inspect real Harmony patch
   owners on the exact target and prove fail-closed behavior without one-frame
   duplicate execution.
3. Frozen API metadata, retained artifact hash/MemberRefs, provider identity,
   Host-on-demand and old observable state/merge-policy tests.
4. Existing generic Author SDK, tracked policy, Catalog, package,
   Doctor/Manager, Compatibility Host and release-contract focused checks,
   including a live zero-leftover assertion for the old heavy executor in
   mandatory Runtime.
5. Source/diff checks that forbid a new SharedNative adapter, Host, receipt
   family or product-specific build/check entry when the Catalog-driven generic
   path already expresses the product.

No complete Release, L0-L5, GC ladder or long test is required for this bounded
implementation.

## Minimum Third-Save Smoke

One short third-save smoke is required after implementation because Harmony
ownership moves between assemblies:

- current Runtime/product DLLs start and HookProbe identifies the admitted
  Advanced product;
- the exact native method has one and only one effective ProductNative
  Postfix;
- loading the third save exercises a real shared Case and proves the widened
  inventory reaches official CountItem/CostItem behavior without mutating or
  replacing the native inventories;
- product config disable or actual Loader owner deactivation removes only the
  exact product patch and returns to native behavior;
- return to title and re-entry preserve clean restoration and allow one clean
  reinstallation;
- final exit leaves no product Harmony owner, compatibility demand, retained
  callback, product state, UI root or `DolocTown.exe` process.

The smoke does not need broad recipe/UI coverage, a complete Release, L0-L5,
GC or long-session testing. If the first launch is not an acceptance run, the
smallest affected focused checks and the same bounded smoke may be rerun under
the normal repair rule.

## Admission Boundaries

This GO permits one ChestLocatorEnhancer implementation Update only. It does
not:

- admit Zoom, MoreEquipmentSlots or any eighth product;
- claim a seventh Advanced product currently exists or has passed runtime
  acceptance;
- open general Advanced authoring;
- add or stabilize an inventory public API;
- add a shared inventory owner, native DTO or UI layer;
- authorize 0.5.5 publication;
- claim download-package or total-shipped-size reduction.

After implementation, a short seven-product comparison may inspect actual
SDK, ConfigMenu and lifecycle duplication. It may promote nothing to
SharedNative unless two independent real consumers prove a common native owner.

## Inspected Authorities

- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`
- `docs/reviews/code/2026/20260722-0004-actionspeed-weight-and-next-product-audit.md`
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/06-030-chest-strongplanting-apis.md`
- `docs/api/public-api-matrix.md`
- `tools/release/dtmapi-product-catalog.json`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ChestLocatorEnhancer`
- `testmods/ChestLocatorEnhancerMod`
- reverse build `23762374_public_C416D4`, especially
  `ArchiveDataHandle.GetAvailableInventories`, `Case`, `StorageShelf`,
  `ItemBox` and `EquipmentManager`

No build, Unit, package, Doctor, Release or game check ran for this
documentation-only admission Review.
