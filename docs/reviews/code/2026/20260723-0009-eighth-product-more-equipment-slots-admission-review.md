# Eighth Product MoreEquipmentSlots Admission Review

**Review ID:** `20260723-0009`

**Date:** 2026-07-23

**Status:** recorded — GO for MoreEquipmentSlots as the only admitted eighth
product; implementation and acceptance have not started

**Pre-implementation correction:** the independent recheck
[`20260723-0010`](20260723-0010-eighth-product-admission-runtime-weight-and-api-reuse-audit.md)
retains the ProductNative owner admission but holds implementation until the
seven-product Doctor baseline, native backpack/mail three-state result,
durable cross-save/sidecar transaction and exact frozen-ABI `0..24` semantics
are closed. This link refines the implementation gate; it does not rewrite the
historical admission analysis below.

**Scope:** independent admission-only review of MoreEquipmentSlots identity,
native owners, DTMAPI sidecar state, mandatory-Runtime removal, frozen ABI,
four-Hook coordination, player-data/UI risk and minimum validation; no
implementation, Update, Catalog/package mutation, new Host, receipt, public
API, build, game launch or Release run

## Source Request And Prior Selection

The seven-product comparison in Review
[`20260723-0008`](20260723-0008-seven-product-horizontal-comparison-and-eighth-candidate-decision.md)
selected only MoreEquipmentSlots for an independent eighth-product decision.
It did not admit the product.

This Review answers that bounded decision. It does not bundle MoreEquipmentSlots
with MoreSaves or Zoom, and it does not revisit the seven verified products.

## Verdict

**GO: admit exactly `DTMAPI.MoreEquipmentSlotsMod` as the eighth Advanced
ProductNative product for a high-risk, behavior-preserving extraction.**

The GO permits one implementation Update. It does not claim that an Advanced
package exists, the four Hooks have moved, protected items have been migrated,
the old ABI is frozen in source metadata, the Compatibility Host contains an
eighth executor, default-loaded Runtime has shrunk, or any focused/game
acceptance has passed. The current implemented baseline remains exactly seven
verified products until that Update closes.

The product has one real consumer and a large, clearly product-shaped mandatory
feature directory. Its extra slots are not a native generic slot facility:
they are a MoreEquipmentSlots-owned protected sidecar, reflected UI clones and
native effect adapters. This makes the boundary ProductNative, not
SharedNative. The migration is admitted only with the data-safety and
owner-coordination gates below.

Safety clause:

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

The method-body review below identifies every current native responsibility
used by the product; it does not claim that the game has a native extra-slot
model.

## Frozen Product Identity

| Fact | Value |
| --- | --- |
| UniqueID | `DTMAPI.MoreEquipmentSlotsMod` |
| Workshop item | `3744059735` |
| Current source/published version | `0.3.1-dtmapi` |
| Admitted target | `1.0.0`, minimum DTMAPI `0.5.5` |
| Official folder | `DTMAPI_MoreEquipmentSlots` |
| Package DLL | `DTMAPI.MoreEquipmentSlots.dll` |
| Canonical config | `DTMAPI/config/DTMAPI.MoreEquipmentSlotsMod.json` |
| Protected sidecar | `DTMAPI/config/protected-items/equipment-slots/slot-<archiveIndex>/equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json` |
| Legacy sidecar inputs | global `equipment-slots-DTMAPI.MoreEquipmentSlotsMod.json` plus `.migrated-<timestamp>` archive |
| Retained published DLL evidence | SHA-256 `092807CC5C5DB359B40D5325EDD6EBC6860238E51F7F65014F5B39FBC0CF71ED` |

The existing Author SDK and tracked policy registry must generate the Advanced
identity. Do not hand-author `CodeModKind=Advanced`, a reference receipt,
manifest or package. The three version axes remain independent: current
source, retained published version and admitted target.

## Real Consumer

The only current real product consumer is
`DTMAPI.MoreEquipmentSlotsMod`. The tracked source calls
`IEquipmentSlotsApi`, and the Phase 0 ownership baseline binds the same
Catalog product to the retained public artifact tree. No second independent
product implements or consumes this sidecar slot policy.

Frozen old-ABI callers remain compatibility obligations. They are not a
second real ProductNative consumer and do not satisfy the SharedNative gate.
The new Advanced product must not consume its own frozen API.

## Native Owner And Actual State Holders

There is no official variable-length equipment-slot list to take over. The
reviewed build `23762374_public_C416D4` has four relevant native
responsibilities:

| Responsibility | Native function/state | Ownership conclusion |
| --- | --- | --- |
| Native skill/passive effects and parameter recomputation | `AgentEquipmentManager.functions`, `AgentEquipmentFunction.CreateAgentEquipmentFunction`, `AgentEquipmentManager.ReloadParams()` and `EquipmentAbility` | Game owns native functions and committed player parameters. Product may insert/remove only functions corresponding to its protected sidecar entries, then request native recomputation. |
| Official item movement | `DolocAPI.CountItem`, `CostItem` and `TryPlaceInBackpack(..., sendEmailOnOverflow:true)` | Game owns backpack/mail item transactions. Product owns when its sidecar transfers exactly one item into or out of those native containers. |
| Shield-hit ordering and health tail | `BodyController.OnAttacked(float,bool,Vector2,out bool)` first asks `AgentEquipmentManager.TryGetShieldItem`, then applies damage/health/hit state | Vanilla shield remains first owner. Product may handle a sidecar shield only when the native manager reports no shield; it must not clear or replace native `hatItem`. |
| Equipment panel lifecycle | `AccessoriesBar.__Init()`, `AccessoriesBar.OnStartShow()` and native `AccessorySlot` objects | Game owns the panel and official slots. Product owns only its cloned extra-slot objects, listeners, layout and destruction. |

The authoritative extra-slot inventory is the DTMAPI protected sidecar plus
the product's loaded runtime entries. The game does not serialize these extra
slots. This is deliberate ProductNative state, not evidence for a public or
shared native equipment API.

The existing current-tree Hook set is therefore exactly:

1. Postfix `AgentEquipmentManager.ReloadParams()`;
2. Prefix `BodyController.OnAttacked(float,bool,Vector2,out bool)`;
3. Postfix `AccessoriesBar.__Init()`;
4. Postfix `AccessoriesBar.OnStartShow()`.

Native SaveGame/SaveLoaded/ReturnedToTitle events remain Platform lifecycle
events. The product must subscribe through existing owner-scoped events; it
must not install a fifth product SaveGame Hook.

## Current Product Call Graph

```text
Strict MoreEquipmentSlots Entry / SaveLoaded
  -> IEquipmentSlotsApi.RegisterSlots(owner, fixed three-slot options)
     -> read current-save protected sidecar
     -> apply stored native functions / defense / shield state
     -> render reflected AccessorySlot clones

UI/API equip
  -> native backpack CountItem / CostItem exactly once
  -> product sidecar entry
  -> native AgentEquipmentFunction or defense/shield adapter
  -> mark sidecar dirty
  -> persist only after native SaveSaved

disable / missing product
  -> tail-first recovery
  -> native TryPlaceInBackpack with mail overflow
  -> clear sidecar entry only after successful placement

native Hooks
  -> ReloadParams merges product effects
  -> OnAttacked preserves native shield priority
  -> AccessoriesBar init/show rebuilds ProductNative clones
```

This execution graph moves into the Advanced product. An equivalent frozen
executor moves into the existing Compatibility Host only for old API callers
and cold missing-product recovery.

## Mandatory Runtime Removal Opportunity

Current exact named production boundaries:

| Source | Physical | Non-empty |
| --- | ---: | ---: |
| `DolocTownExperimentalBridgeApi.EquipmentSlots.cs` | 2,650 | 2,341 |
| `EquipmentSlotProtectedStoragePolicy.cs` | 99 | 87 |
| `EquipmentSlotShieldPolicy.cs` | 65 | 56 |
| **Mandatory named EquipmentSlots feature** | **2,814** | **2,484** |
| Current Strict product `ModEntry.cs` | 92 | 81 |

The current Release product DLL is 9,728 bytes; the current mandatory
GameBridge DLL is 937,984 bytes. These are pre-implementation measurements,
not promised final deltas.

The heavy 2,814-line feature is a credible extraction candidate, but the
implementation must retain small mandatory boundaries for:

- the frozen provider and on-demand Host proxy;
- exact Product/Compatibility owner observation;
- a cheap cold sidecar-presence route that can demand-load the existing Host
  when the product is disabled, removed or never calls the API;
- Platform lifecycle dispatch that is not product policy.

Therefore 2,814 lines is the gross product-shaped boundary, not a promised net
reduction. The implementing Update must report before/after physical and
non-empty mandatory source plus GameBridge DLL bytes and run a live
zero-leftover check for the old heavy executor. It may claim only reduced
default-loaded Runtime. The Host executor and Advanced product remain shipped,
so download, installed, combined binary, total source and repository size
claims are prohibited.

## Data-Safety Gates

The extraction may not merely copy current code. Three current behaviors are
explicit hard gates:

1. `PersistEquipmentSlotStorage` writes through `File.Create` and returns no
   success value, while the SaveSaved caller removes the dirty owner after the
   call. A failed or interrupted write can therefore lose the retry signal or
   leave a truncated sidecar. The product implementation must use a
   recoverable temp/replace write and retain an explicit dirty/failed state
   until persistence succeeds.
2. Current owner cleanup recovers items into native inventory and then writes
   the sidecar outside the native SaveGame transaction. If the game save and
   sidecar commit diverge, this can duplicate or lose an item. Real Loader
   deactivation must first remove native effects, UI roots, listeners and
   exact Hooks, while retaining protected entries and an explicit
   recovery-pending state unless a native SaveSaved-bound transaction can
   complete. It must not claim hot recovery merely because a direct file write
   returned.
3. Same-process official disable historically leaves inert extra-slot UI until
   restart. The Advanced product must destroy its clones/listeners and clear
   product runtime roots on config disable and real Loader deactivation.

Cold disabled/unsubscribed recovery remains required. A tiny mandatory
sidecar-presence route may load the existing Compatibility Host at SaveLoaded
when a protected or legacy sidecar needs recovery. It must not parse or execute
the full product policy in mandatory GameBridge, and it must remain dormant
when no sidecar or old ABI request exists.

Recovery keeps current semantics:

- current save scope and archive/player/save-clock guards;
- legacy global adoption followed by archived migration;
- tail-first ordering;
- native backpack placement with mail overflow;
- a failed placement retains the sidecar entry and reports partial failure;
- no item is cleared from protected storage before native placement succeeds;
- native visual `hatItem` is never replaced by an extra-slot hat.

## Frozen `IEquipmentSlotsApi`

`IEquipmentSlotsApi`, `EquipmentSlotsOptions`,
`EquipmentSlotsRegisterResult`, `EquipmentSlotsState`, `EquipmentSlotInfo`,
`EquipmentSlotEquipResult` and `EquipmentSlotsRecoveryResult` currently remain
Experimental public ABI. During implementation they must receive the same
warning-bearing `Experimental + Deprecated/Frozen` source disposition used by
the prior seven compatibility surfaces, without changing binary signatures,
provider identity, DTO members or observable old-caller behavior.

Mandatory GameBridge retains only the owner-bound facade, thin on-demand proxy
and cold recovery trigger. The heavy old executor becomes the eighth domain in
the **existing single** dormant-shipped Compatibility Host. Do not add a Host,
provider, API, schema, receipt family, product-specific builder or checker.

The implementation must scan and bind the exact retained published DLL, prove
its TypeRef/MemberRef compatibility, and preserve arbitrary old owner IDs. The
new Advanced product is not routed through this ABI.

## Four-Hook Owner Coordination

The product owner is
`dtmapi.mod.dtmapi.moreequipmentslotsmod`. All four targets must pre-resolve
and install atomically; partial stat/UI/shield ownership is fail-closed.

Focused executable tests must prove both physical orders:

- **product first, old ABI second:** the Host observes all exact product owners
  and does not install the compatibility executor;
- **old ABI first, product second:** product Entry observes compatibility
  ownership on every target and installs none;
- a mixed or residual subset is failure, not degraded success;
- cleanup of one owner never removes the other owner;
- failure during state restoration still reaches exact-owner unpatch for all
  product targets and leaves a truthful pending-recovery/restart state;
- config disable/re-enable, title/re-entry and actual Loader owner deactivation
  leave no clone, listener, native function, callback or product owner residue.

The native vanilla shield path always wins. Compatibility and ProductNative
must never both replay the `BodyController.OnAttacked` tail in one hit.

## SharedNative Decision

The admitted extraction adds **zero SharedNative**.

No other real product owns the MoreEquipmentSlots sidecar, variable extra-slot
policy, reflected `AccessoriesBar` clones or shield fallback. Native
`AgentEquipmentManager`, backpack/mail and UI types are shared game objects,
but a shared game type is not a shared product responsibility. Catalog,
Author SDK, package/Doctor/Manager, Loader lifecycle, owner events and the one
Compatibility Host are already Platform.

Do not create an equipment DTO API, generic protected-container API, shared UI
clone service, shared reflection cache or shield adapter in this migration.
Future reuse must be reviewed from a second real consumer and common native
owner, not inferred from this product.

## Minimum Focused Checks

A later implementation needs the existing focused mechanisms only:

1. Release product/Runtime/Host builds and product Units for fixed three-slot
   normalization, config reload, source/published/target identity, exact
   `netstandard2.0` package and zero mandatory heavy executor.
2. Executable real-Harmony four-target tests for atomic install, both
   Product/Compatibility orders, mixed residual rejection, exact-owner
   cleanup, config disable/re-enable and actual owner deactivation.
3. Sidecar transaction Units for current-save isolation, archive/player/clock
   rejection, legacy adoption/archive, tail-first recovery, atomic replace,
   failed-write dirty retention, corrupt/truncated-file fallback, and no
   cross-save adoption.
4. Inventory/effect tests for exactly-once CostItem, replacement rollback,
   backpack placement, mail overflow, failed placement retaining the entry,
   native function removal, hat defense merge, vanilla shield priority,
   tail-first multi-shield consumption and native visual-hat preservation.
5. UI lifecycle tests for one clone/listener set, no duplicate binding,
   disable/title/re-entry cleanup and stale/destroyed Unity-object handling.
6. Frozen ABI metadata, exact retained artifact MemberRefs, provider state,
   cold sidecar Host activation, arbitrary legacy owner behavior and
   Host/product zero-dual-owner checks.
7. Existing generic Catalog, Author SDK, tracked policy, package,
   Doctor/Manager, transaction, Compatibility Host and release-contract
   focused checks. No new assurance family is authorized.

No complete Release, L0-L5, GC gradient or long test is required for the
bounded implementation.

## Minimum Third-Save Acceptance

Because player-owned items cross the native save and product sidecar boundary,
one process cannot prove the cold disabled/unsubscribed path. The minimum is
**one bounded third-save acceptance transaction with two short launches under
one runtime lock and one protected-save backup/restore**:

1. Enabled current Advanced product: current DLL startup and HookProbe; exactly
   four product-owned targets; open/hover/close the extra-slot UI; equip one
   passive, one defense-only hat and one shield hat; verify native effects,
   native shield priority and unchanged visual hat; native save, title/reload,
   no duplication and exact sidecar scope.
2. Cold product-disabled launch: no product Entry or product Harmony owner;
   the existing Host activates only because protected recovery is demanded;
   recover tail-first through real backpack/mail overflow, complete one native
   SaveSaved-bound sidecar commit, restore title, and finish with zero product
   or compatibility transient owner/UI/callback roots and no
   `DolocTown.exe`.

The runner must restore the exact third-save files, official profile, product
source/deployment state and any mail/backpack mutation. If the first launch is
non-acceptance, repair the focused fault and rerun only this bounded
transaction. This is not a complete Release, L0-L5, GC or long-session test.

## Admission Boundaries

This GO permits one MoreEquipmentSlots implementation Update only. It does
not:

- claim an eighth implemented or verified product;
- admit Zoom, a ninth product, G7 or general Advanced authoring;
- combine MoreEquipmentSlots with MoreSaves;
- stabilize or expand `IEquipmentSlotsApi`;
- add a native equipment-slot promise, SharedNative adapter or new Host;
- change the accepted missing yellow shield-bar visual difference;
- authorize 0.5.5 publication;
- claim download or total-shipped-size reduction.

## Inspected Authorities And Code

- `PROJECT.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`
- `docs/reviews/code/2026/20260723-0008-seven-product-horizontal-comparison-and-eighth-candidate-decision.md`
- `docs/reviews/api/2026/20260607-0009-native-owner-special-audits/05-equipment-slots-api.md`
- `docs/reviews/api/local-mods-native-owner/INDEX.md`
- `docs/reviews/api/local-mods-native-owner/shared-native-owner-conflicts.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/2026/20260614-0001-equipment-slots-protected-storage.md`
- `docs/updates/2026/20260614-0004-equipment-slots-hat-defense.md`
- `docs/updates/2026/20260614-0006-equipment-slots-shield-hat-protection.md`
- current Abstractions, GameBridge EquipmentSlots, owner cleanup, Hook
  installer/callbacks, Strict product and Catalog sources
- reverse build `23762374_public_C416D4`, especially
  `AgentEquipmentManager`, `AgentEquipmentFunction`, `BodyController.OnAttacked`,
  `AccessoriesBar` and `AccessorySlot`

No build, Unit, package, Doctor, Release or game check ran for this
documentation-only admission Review.
