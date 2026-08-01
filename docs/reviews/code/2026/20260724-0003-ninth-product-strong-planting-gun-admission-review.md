# Ninth Product Strong Planting Gun Admission Review

**Review ID:** `20260724-0003`

**Date:** 2026-07-24

**Status:** recorded — GO for StrongPlantingGun as the only admitted ninth
product; Mine remains `PrototypeBlocked`; implementation and acceptance have
not started

**Scope:** independent admission-only comparison of StrongPlantingGun and Mine
across real consumer identity, native owner/state, default-loaded Runtime
removal, old-ABI cost, player-data risk and UI/Hook risk; no implementation,
Update, Catalog/policy/package mutation, new Host, receipt, public API, build,
game launch or Release run

## Source Request

After freezing the eight-product horizontal/runtime baseline, independently
compare StrongPlantingGun and Mine and admit at most one ninth ProductNative
product. The decision must not turn a single-product implementation into
SharedNative, a public API or a new Host.

## Verdict

**GO: admit exactly `DTMAPI.StrongPlantingGunMod` as the ninth Advanced
ProductNative product for one behavior-preserving extraction.**

**NO-GO: do not admit `DTMAPI.MineMod`.**

This decision authorizes one later StrongPlantingGun implementation Update. It
does not claim that an Advanced package or tracked policy exists, that the
native Hooks have moved, that mandatory Runtime has shrunk, that the old API
has been disposed, or that any focused/game acceptance has passed. The
implemented and verified baseline remains exactly eight products until that
Update closes.

StrongPlantingGun has one real product consumer and a concentrated native
boundary. Mine has a larger gross mandatory source opportunity, but its actual
state spans several native and product owners and its four recorded prototype
blockers remain open.

Safety clause:

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

The current `23762374_public_C416D4` method bodies were read for both
`ItemFarmingGun` and `FarmingGunUiState`; the owner/state result is below.

## Candidate Comparison

| Candidate | Gross default-Runtime opportunity | Native owner / state | ABI cost | Player-data and UI/Hook risk | Decision |
| --- | ---: | --- | --- | --- | --- |
| **StrongPlantingGun** | Three mandatory GameBridge files: **1,019 physical / 895 non-empty lines**. A thin or zero compatibility boundary must be decided from the real-consumer scan, so this is not a promised net delta. | Native `ItemFarmingGun` construction/use, its serialized `LinearInventory`, `ItemFunctionFarmingGun.Capacity`, and `FarmingGunUiState` transfer methods; product owns the fixed-three-slot seed/film/fertilizer policy. | Three methods plus three DTOs. No published/Workshop retained artifact is recorded; only the tracked legacy/no-Type source consumes it. | Medium: native per-item inventory is saved, reflected capacity can expose/hide occupied slots, and two UI transfer prefixes plus the tool prefix must conserve items. Five concrete Harmony patches are required. | **GO; sole ninth product.** |
| Mine | One mandatory GameBridge file: **1,468 physical / 1,315 non-empty lines**. | Not one native owner: placed equipment/container, native electric `Launch`, archive time, product scheduler/RNG/storage, global recipe/tech mutation and renderer lifetime. | Four methods plus the large Machine DTO family; no second real machine consumer exists. | High: economy and global tables, power, due-state catch-up, storage and visual lifecycle. Same-process restoration is not proven. | **NO-GO; keep `split-decided` / `PrototypeBlocked`.** |

These are current physical/non-empty source counts, not DLL, repository,
download, install or total-shipped reductions. A later implementation may
claim only the measured reduction of default-loaded mandatory Runtime.

## Frozen Product Identity

| Fact | Value |
| --- | --- |
| UniqueID | `DTMAPI.StrongPlantingGunMod` |
| Catalog ID | `strong-planting-gun` |
| Workshop / retained published version | none recorded |
| Current legacy/no-Type source version | `0.3.1-dtmapi` |
| Admitted target | `1.0.0`, minimum DTMAPI `0.5.5` |
| Official folder | `DTMAPI_StrongPlantingGun` |
| Package DLL | `DTMAPI.StrongPlantingGun.dll` |
| Canonical config | `DTMAPI/config/DTMAPI.StrongPlantingGunMod.json` |
| Canonical Advanced Harmony owner | `dtmapi.mod.dtmapi.strongplantinggunmod` |
| Product save sidecar | none |

The existing Catalog-driven Author SDK must generate the identity, policy,
receipt and package. This Review does not authorize a hand-written Advanced
manifest, receipt, native reference or package.

## Real Consumer And Compatibility Cost

The only independent real consumer is the tracked
`testmods/StrongPlantingGunMod/ModEntry.cs`. It calls
`IStrongPlantingGunApi.Register/GetState` and fixes the player product at three
slots with seed, film and fertilizer enabled and water disabled.

Current Catalog facts are materially different from the eight retained
published compatibility products:

- `publishedVersion` and Workshop ID are both null;
- `legacyReleaseLane` is `DeveloperInstallOnly`;
- the Phase 0 consumer authority records tracked source only and no retained
  tree hash;
- repository/API scans found no second product or retained published binary
  consumer.

Therefore this admission does **not** authorize a ninth Compatibility Host
executor or a fake dual-owner matrix. During implementation,
`IStrongPlantingGunApi`, `StrongPlantingGunOptions`,
`StrongPlantingGunRegisterResult` and `StrongPlantingGunState` must keep their
exact type/member shape and receive warning-bearing
Experimental/Deprecated/Frozen source metadata, but the migrated product must
not consume them and mandatory Runtime must not keep the heavy provider merely
for an unknown consumer.

A focused source/metadata check must freeze the current interface and DTO
members. If an exact real retained binary consumer is discovered before the
atomic switch, implementation stops and reopens compatibility disposition
against the existing single Host; it must not silently add an executor,
invent a receipt or claim dual-order proof without that evidence.

## Native Owner And Actual State Holders

The official method bodies establish these responsibilities:

| Responsibility | Native method/state | Ownership conclusion |
| --- | --- | --- |
| Saved gun contents | `ItemFarmingGun.inventory`, a `[JsonProperty] LinearInventory`; the JSON constructor accepts the saved inventory | Game owns item serialization and the per-gun item instances. Product may expand capacity but must never copy contents into a sidecar or truncate occupied slots. |
| Capacity display/creation | `ItemFunctionFarmingGun.Capacity`; both ordinary and JSON constructors create/retain the inventory; `totalCapacity` and `lineCapacity` read the function capacity | Product owns the fixed-three policy and an exact snapshot/restore of the reflected original function capacity. The function object may be shared by multiple guns, so this is global product state, not per-callback scratch state. |
| Tool behavior | `ItemFarmingGun.OnUseAsTool`, private native `GetEquipmentsFromArea`, `CheckCanInteract` and `DoInteract` | Product may iterate the three supported slots, but rejected paths must fall through and accepted actions must conserve/consume exactly the native item count expected by current behavior. |
| UI transfer | `FarmingGunUiState.HandlePlaceToOtherSide` and `HandleSwapOneItem`; native backpack/container inventory and buffer state | Product may intercept only the reviewed backpack-to-farming-gun cases. The native inventory objects and UI state remain game-owned. |

The product owns configuration, the fixed-three seed/film/fertilizer policy,
reflection metadata/cache, exact Harmony owner, diagnostics and restoration.
It does not own `PlantBasin`, backpack contents, native item serialization,
native UI objects or a general farming-tool API.

Water remains outside the first-party product contract even though the frozen
options DTO contains `IncludeWater`. The product must not silently broaden its
behavior while moving assemblies.

## Exact Harmony Boundary

The current route is often summarized as four Hooks, but the executable
boundary is **five concrete Harmony patches across four semantic paths**:

1. Postfix the ordinary `ItemFarmingGun(ItemInfo, int)` constructor;
2. Postfix the JSON `ItemFarmingGun(string, int, LinearInventory)` constructor;
3. Prefix `ItemFarmingGun.OnUseAsTool()`;
4. Prefix `FarmingGunUiState.HandlePlaceToOtherSide(int)`;
5. Prefix `FarmingGunUiState.HandleSwapOneItem(int)`.

The current bridge combines the two constructor attempts with a boolean OR.
That is insufficient for ProductNative admission: both overloads and all three
methods must pre-resolve, then install all-or-none under the canonical owner.
Any missing target, mixed residual owner, install exception or rollback
failure is fail-closed and must leave a truthful failed/restart-required state.

Config disable, title/re-entry and real Loader owner deactivation must:

- restore the exact original shared function capacity without truncating the
  saved `LinearInventory`;
- remove only the exact product owner from all five targets;
- clear product instance, callback, listener, cached Unity object and Core
  owner roots;
- allow one clean reinstall only when every prior root and patch is proven
  zero.

The implementation must not use a static callback fallback after the product
owner is gone.

## Player-Data And Behavior Gates

There is no product sidecar, but native save data still makes this a
player-data migration:

1. A farming gun may already contain items in slots two and three. Disable,
   title return or owner deactivation must not shrink or rewrite the native
   inventory in a way that drops, duplicates or silently moves those items.
2. The reflected `ItemFunctionFarmingGun.Capacity` is shared native prototype
   state. Snapshot it once before mutation and restore exactly on final owner
   cleanup, including exception paths.
3. Both ordinary creation and JSON reload must reach the same fixed-three
   product state. Covering only the ordinary constructor can strand saved guns
   at a mismatched capacity.
4. Seed, film and fertilizer remain the only first-party product categories.
   Rejected item kinds, full inventory, non-farming containers, non-empty UI
   buffer and native check failures must conserve item counts.
5. The product tool loop must preserve current official
   `CheckCanInteract`/`DoInteract`, season, basin, protection, fertilizer and
   area semantics. It must not turn the migration into a new planting engine.
6. Repeated use/transfer must cache reflection metadata, avoid unbounded logs
   and satisfy a short allocation gate; optimization may not replace native
   ownership or change item ordering.

## Mandatory Runtime Removal Opportunity

Current exact mandatory files are:

| File | Physical | Non-empty |
| --- | ---: | ---: |
| `StrongPlantingGunFeature.cs` | 66 | 53 |
| `StrongPlantingGunHookBridge.cs` | 85 | 71 |
| `StrongPlantingGunService.cs` | 868 | 771 |
| **Mandatory feature total** | **1,019** | **895** |

The current legacy/no-Type `ModEntry.cs` is 119 physical / 106 non-empty lines.
`1,019 / 895` is the gross removable feature directory, not a promised net
reduction. The later Update must also account for callback, demand,
registration and diagnostics code outside that directory and measure:

- mandatory five-project compiled physical/non-empty lines;
- mandatory GameBridge physical/non-empty lines and DLL bytes;
- the ninth product source/DLL and unchanged optional Host;
- a live zero-leftover scan for the old Service, HookBridge, callback and
  demand execution bodies.

The allowed conclusion remains smaller default-loaded Runtime only. The
ProductNative DLL is still distributed, so no repository/download/install/
total-package reduction may be claimed.

## SharedNative Decision

The extraction adds **zero SharedNative**.

There is one real product consumer and one product-specific fixed-three policy.
`ItemFarmingGun`, `LinearInventory` and `FarmingGunUiState` being native types
used elsewhere does not make this five-patch policy shared. Catalog, Advanced
SDK/package, Doctor/Manager, ConfigMenu registration, Loader owner lifecycle
and zero-leftover checks are existing Platform responsibilities.

Do not add a farming-gun DTO API, generic container-expansion service, shared
UI-transfer adapter, new Host, receipt or product-specific build/check family.
Only a future second independent product with the same exact native
owner/restoration invariant can reopen SharedNative.

## Why Mine Is Not Admitted

The G1 Oil/Mine Review and current Catalog already record Mine as
`split-decided` / `PrototypeBlocked`. Its static item/equipment/recipe/group
content remains official JSON, while any future runtime/economy product must
first close four defects:

1. the configurable 0–100 power value is not applied to the native component;
   actual native `Launch()` still uses fixed JSON power 10;
2. `config.Enabled` does not prevent registration;
3. `IncludeRuntimeModMinerals` has no production reader;
4. global recipe and tech mutation lacks an exact original snapshot/restore
   proof.

Mine also combines scheduler/RNG/due state, archive time, storage, power,
recipe/tech and renderer lifetime. That is not a single native owner and is not
made safer by its larger 1,468-line gross mandatory boundary. No Mine
assembly, machine Host/API or economy implementation is admitted here.

## Minimum Focused Implementation Checks

A later StrongPlantingGun implementation may reuse only existing focused
mechanisms:

1. Product/Runtime Release builds and source checks proving
   `netstandard2.0`, exact identity/policy/receipt/package and zero mandatory
   Strong executor/callback/demand body.
2. Executable Harmony tests for all five exact targets: pre-resolution,
   all-or-none install, mixed/residual owner rejection, injected failure
   rollback, exact-owner unpatch and real Loader deactivation.
3. Capacity/save tests for both constructors, native JSON inventory retention,
   original shared-function snapshot/restore, occupied slots two/three, config
   disable/re-enable, title/reload and no item loss/duplication.
4. Tool tests for seed/film/fertilizer filtering, official interaction
   checks, per-action consumption, native failure/fallthrough and repeated
   equipment/slot deduplication.
5. UI tests for place/swap, buffer/non-farming rejection, backpack/container
   conservation, full-container failure, title close and zero listener/object
   roots.
6. Frozen API/DTO metadata and MemberRef-shape checks plus a real consumer scan
   proving there is no retained published binary; no Compatibility Host
   executor may be added from a synthetic consumer.
7. Existing generic Catalog, Author SDK, package, Doctor/Manager,
   install/uninstall, release-contract and zero-leftover focused checks.
8. A short repeated-call allocation/log gate over cached reflection metadata.

No complete Release, L0–L5, GC gradient or long test is required or authorized
for this bounded migration.

## Minimum Third-Save Smoke

One protected third-save short smoke is required because five native patches
and native item inventory ownership move between assemblies:

- current Runtime/product DLLs and HookProbe start with the exact Advanced
  identity;
- both constructor overloads and the three methods have exactly one product
  owner each;
- a real farming gun exposes three slots and executes seed, film and
  fertilizer through the reviewed native checks;
- the real Farming Gun UI places and takes supported items without loss or
  duplication;
- native save, title return and re-entry retain the same three logical items
  and reinstall one exact owner set;
- config disable or real Loader owner deactivation restores the original
  shared function capacity, leaves native inventory contents intact, and
  reduces product instance/callback/listener/cache/Core roots and all five
  patches to zero;
- the runner restores the protected save, official profile, product
  source/deployment and QA staging, then exits with no `DolocTown.exe`.

If orchestration or a focused product defect prevents acceptance, repair that
fault and rerun only this bounded smoke. It is not permission to repeat the
complete Release suite.

## Admission Boundaries

This GO permits one StrongPlantingGun implementation Update only. It does not:

- claim a ninth implemented or verified product;
- admit Mine, a tenth product, G7 or general Advanced authoring;
- add a ninth Compatibility Host executor without a real retained consumer;
- stabilize or expand `IStrongPlantingGunApi`;
- add SharedNative, a farming DTO/API or a generic container/UI service;
- authorize 0.5.5 publication;
- claim repository, download, install or total-shipped-size reduction;
- authorize another complete Release run.

## Inspected Authorities And Code

- `PROJECT.md`
- `docs/workflows/codex-api-rebuild.md`
- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`
- `docs/reviews/api/2026/20260722-0002-batch6-g1-unresolved-native-owner-and-oil-mine-design-review.md`
- `docs/reviews/api/2026/20260607-0010-native-owner-remaining-api-audit/06-030-chest-strongplanting-apis.md`
- `docs/api/public-api-matrix.md`
- `tools/release/dtmapi-product-catalog.json`
- `tools/release/baselines/batch6-phase0-ownership-baseline-20260720.json`
- current Abstractions, StrongPlantingGun feature/callback/demand sources and
  tracked legacy/no-Type product input
- reverse build `23762374_public_C416D4`
  `DolocTown/ItemFarmingGun.cs` and `DolocTown/FarmingGunUiState.cs`

No build, Unit, package, Doctor, Release or game check ran for this
documentation-only admission Review.
