# Seven-Product Horizontal Comparison And Eighth-Candidate Decision

**Review ID:** `20260723-0008`

**Date:** 2026-07-23

**Status:** recorded — seven-product baseline frozen; MoreEquipmentSlots
selected for one independent eighth-product admission Review only

**Scope:** compare the seven verified Advanced products after the corrected
ChestLocatorEnhancer acceptance, decide whether another shared Runtime boundary
exists, and select exactly one eighth candidate; no eighth admission,
implementation, Catalog/package identity change, Host, receipt, public API,
Release run or game launch

## Source Request

After correcting the seventh-product commit-range findings, perform a short
cross-product comparison. Promote only a boundary with two independent real
product consumers and a common native owner. Then choose one eighth candidate
and keep its admission decision separate.

## Baseline Result

The seven-product baseline is `verified/closed`.
`GAME-SMOKE/20260723-224200` binds the corrected candidate to Runtime
`BuildCommit=7ace68260cb1` and product entry SHA-256
`29C2FD2EDA5618C6814C851913F64E77245CE9A31703FF6FD70C1B802D0DA9C1`.
Current startup, HookProbe, third-save Chest behavior, title restoration,
exact-owner deactivation and clean process exit pass. Update `20260723-0007`
owns the implementation and validation facts.

## Seven-Product Comparison

| Product | ProductNative owner | Reused Platform surface | SharedNative result |
| --- | --- | --- | --- |
| AutoFishing | fishing state machine, 22 exact Hooks and deep native restoration | SDK/package, Catalog, Loader lifecycle, owner-scoped ConfigMenu | No second product shares the fishing owner or restore invariant. |
| OneActionComplete | two completion Hooks and action-specific policy | same Platform build, package and lifecycle conventions | No second product owns the same completion targets. |
| ActionSpeed | nine action/input/animation Hooks and exact state restoration | same Platform build, package and lifecycle conventions | Its superficially similar action state does not share OneActionComplete's native targets or restoration state. |
| FishBreedingAssistant | fish-title Hook and product formatting | same Platform lifecycle plus `IItemDisplayNameApi` | Shares one read-only native item-display-name owner with Animal only. |
| AnimalHusbandryProgress | four animal-view/UI Hooks, cached rows and clone lifecycle | same Platform lifecycle plus `IItemDisplayNameApi` | Shares only the existing item-display-name query; animal UI remains ProductNative. |
| MoreSaves | `archiveFileCount` six/twelve-slot policy, zero Harmony | same Platform build, package and lifecycle conventions | No other product owns save-slot policy or state. |
| ChestLocatorEnhancer | one inventory-query Postfix and traversal policy | same Platform build/package/lifecycle and existing Compatibility Host | No second product shares the widening policy or inventory-query native owner. |

The common Catalog-driven Advanced SDK, package builder, Doctor/Manager,
installation, zero-leftover classification and owner cleanup are already
Platform capabilities. Owner-scoped ConfigMenu registration and Loader
lifecycle have small repeated product glue, but they are conventions over
different product states and native owners; extracting another runtime layer
would add indirection without removing a common native responsibility.

The only proven two-product SharedNative boundary remains the Experimental,
read-only `IItemDisplayNameApi` used by FishBreedingAssistant and
AnimalHusbandryProgress. No new SharedNative capability is justified.

## Eighth-Candidate Decision

**Select `DTMAPI.MoreEquipmentSlotsMod` for one independent eighth-product
admission Review. This is selection for review, not admission.**

| Candidate | Default-Runtime removal opportunity | Ownership and compatibility | Risk | Decision |
| --- | --- | --- | --- | --- |
| MoreEquipmentSlots | Earlier inventory measured about 2,814 GameBridge lines against a 92-line Strict consumer, the largest remaining credible ProductNative extraction. Exact movable/net counts must be remeasured in its own Review. | One real product currently consumes the sidecar equipment policy. The frozen `IEquipmentSlotsApi` executor could reuse the existing Compatibility Host if admitted. | High: per-save protected sidecars, inventory/mail overflow, stat functions, shield hats, cloned AccessoriesBar UI and disable recovery. | **Selected for independent admission Review because it now offers the largest credible default-loaded Runtime reduction and is being evaluated alone.** |
| Zoom | Earlier inventory counted 1,513 GameBridge lines, but the playable-camera service, arbitration, native camera lifecycle and `ICameraViewApi` are genuinely shared. The product-removable subset remains unproven and likely small. | It is already a Strict consumer of a real SharedNative Camera owner. | Low player-data risk but global visual/lifecycle risk; migration could steal shared ownership merely to improve a line count. | Keep Strict; do not use as the eighth product without a separately proven ProductNative slice. |

MoreEquipmentSlots must not be bundled with MoreSaves. Save-slot count and
equipment sidecar/UI recovery have different native owners, player-data
boundaries and failure modes. The Review may return GO or NO-GO; until it
records a bounded verdict, the exact admitted and implemented set remains the
current seven.

## Admission Questions Handed Forward

The independent Review must answer:

1. Which native functions own stat application, hit/shield handling, native
   inventory transfer and UI lifecycle, and which state is instead owned by
   the product sidecar?
2. Is MoreEquipmentSlots still the only real product consumer, distinct from
   frozen compatibility callers?
3. How many physical/non-empty mandatory Runtime lines and DLL bytes can
   actually leave default loading after retaining thin provider/lifecycle
   coordination?
4. How will `IEquipmentSlotsApi` and the retained published consumer stay
   frozen without a new Host, API, receipt or product-specific toolchain?
5. Can all equipment policy, cloned UI and sidecar recovery remain
   ProductNative with zero new SharedNative?
6. What focused transaction, exact-owner, sidecar isolation, overflow,
   disable/restart, UI cleanup and Hook tests are mandatory, and what is the
   smallest truthful third-save smoke?

Safety clause:

> 先做本轮 API/domain 的 native owner 方法体审查；未找到 native owner 或状态持有者前，不得通过 mod 层补丁冒充 API 重做完成。

## Inspected Authorities

- `docs/architecture/batch6-managed-mod-identity-contract.md`
- `docs/planning/20260712-dtmapi-lightweight-functional-mod-roadmap.md`
- `docs/reviews/code/2026/20260723-0005-seventh-product-chestlocator-admission-review.md`
- `docs/reviews/code/2026/20260723-0007-seven-product-commit-range-audit.md`
- `docs/updates/2026/20260723-0007-chestlocator-seventh-advanced-product.md`
- `docs/api/public-api-matrix.md`
- `docs/reviews/api/2026/20260607-0009-native-owner-special-audits/05-equipment-slots-api.md`
- `docs/reviews/api/local-mods-native-owner/INDEX.md`
- `tools/release/dtmapi-product-catalog.json`

No build, Unit, package, Doctor, Release or game check ran for this
documentation-only comparison and candidate selection.
