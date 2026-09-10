# 20260713-0002 SaveSlots Fixed-12 Product Boundary Review

Status: recorded / revised K1 and L1 selected / implementation open
Date: 2026-07-13
Scope: MoreSaves 1.0.0 scope, current fixed-12 semantics, future naming/scrolling, and `ISaveSlotsApi` compatibility
Related decision docket: `docs/reviews/code/2026/20260713-0008-major-update-fourth-decision-docket.md`
Related Update: `docs/updates/2026/20260713-0003-third-round-closure-fourth-decision-docket.md`
Refines: `docs/reviews/api/2026/20260610-saveslots-native-responsibility.md`

## Source Request

The user stated that MoreSaves should be split more cleanly and should later add save naming and a scrolling UI. The fourth decision round needs to separate the already published product's 1.0.0 rebuild from a speculative general save-system API.

This is a source/API review. It does not change slot count, save files, sidecars, UI, public members, or the installed MoreSaves product.

## Current Product Is Fixed At Twelve

MoreSaves is an established product (`DTMAPI.MoreSavesMod`, Workshop `3742763050`). Its current Mod body owns little beyond `Enabled`:

- `FixedSlotCount=12` is passed on every registration;
- config `SlotCount` is a migration residue and is always normalized back to 12;
- the public `SaveSlotsOptions.SlotCount` appears arbitrary, but GameBridge normalizes every enabled request to 12 and disabled requests to vanilla 6;
- the effective global count is therefore only 6 or 12;
- source tests deliberately assert that other requested counts normalize to 12.

The public shape is broader than the implemented product contract.

## Native Ownership And Evidence

GameBridge writes the global `GameManager.archiveFileCount`. Official `LocalSave`, `DataPersistenceManager`, `GameDataUiState`, and `GameDataPanel` continue to own discovery, file naming/path, creation, save, load, copy, delete, and UI rendering/navigation.

Earlier automated/runtime evidence proved count expansion and official 12-slot rendering but did not itself close the complete product matrix for slot 7+. The user's durable long-term manual feedback now confirms the current product behavior for:

- slots 7-12 create/save/title-return/reload and fresh restart recognition;
- copy and delete;
- non-destructive disable to six and re-enable recovery;
- ordinary use at 12 and 16 slots.

The same feedback reports that 18 slots stack on one page and overflow the screen. This narrows the known failure to the current UI layout/navigation boundary; it does not prove every error/recovery path or arbitrary-count API semantics. See `docs/reviews/manual-qa/2026/20260713-0001-moresaves-long-term-player-baseline-review.md`.

`SaveSlotsService` retains historical >12 paging/layout code, but no current ordinary/product entry can request >12. That code is QA/future residue, not proof of a scrolling product.

Save naming is a separate native-owner problem. A future DTMAPI sidecar must bind names to a durable archive identity/fingerprint rather than a drifting list index and must define copy/delete/new-game/migration behavior. Do not write unreviewed display names into official save data.

## Desired Ownership

| Layer | Responsibility |
| --- | --- |
| MoreSaves product | fixed/selected product count, enablement, future count policy, naming/scroll UX, player messages |
| GameBridge | narrow archive-count adapter, reviewed official lifecycle observation, minimum official-panel adapter |
| native game | save files, archive data, create/load/save/copy/delete and authoritative UI state |
| Core/Bootstrap | no MoreSaves product policy |

SaveSlots construction, Hook installation, and periodic participation should become consumer/demand activated. That is a hot-path engineering requirement, not evidence that arbitrary counts are supported.

## K - MoreSaves 1.0.0 Scope

### K1 - fixed 12 first

MoreSaves 1.0.0 preserves a fixed twelve-slot player contract and completes the full slot 7-12 lifecycle matrix. Save naming, configurable higher counts, and a real scrolling UI become later 1.x projects with their own native/UI reviews.

Selected in revised form. It protects the existing default-twelve behavior, turns long-term manual success into formal regression coverage, and does not make naming/scrolling part of the 1.0.0 gate.

### K2 - naming, variable count, and scrolling are 1.0.0 gates

Deliver the complete desired feature set in the rewrite epoch.

This avoids a second product milestone, but simultaneously expands save-file identity, sidecar migration, copy/delete/restart, official UI layout, and product configuration risk.

### K3 - permanently fixed 12

Declare naming/scrolling out of scope forever.

This is smallest but conflicts with the stated future direction.

## L - Public SaveSlots API

### L1 - fixed-12 compatibility facade

DTMAPI 0.5.5 retains the old `ISaveSlotsApi` binary/provider surface as a fixed-12 compatibility facade. `SlotCount` is frozen/obsolete and no longer documented as arbitrary. New MoreSaves 1.0.0 consumes a first-party internal capability. A new ordinary public save API is designed only when a second real consumer creates demand.

Selected under the full I1 lifecycle. It preserves old binaries in 0.5.5 without making one first-party product's misleading DTO a permanent platform promise, then permits conditional removal only after the warning/consumer/migration/breaking-version gates.

### L2 - public 6/12 mode API

Keep the public Experimental API and explicitly redefine/document it as requesting the official vanilla/expanded mode, never an arbitrary count.

This is truthful but retains a public single-product abstraction with little external benefit.

### L3 - implement arbitrary slot counts now

Make the current DTO promise real, including multi-owner policy, safe bounds, every expanded-slot lifecycle, scrolling/navigation, and disable/recovery.

This is a large save-platform project and is not recommended for 0.5.5/MoreSaves 1.0.0.

## Acceptance Boundary

Selecting revised K1/L1 does not delete public members in 0.5.5 or erase the 16/18-slot manual evidence. It changes the future product/API promise. Implementation still needs compatibility warnings, old Workshop DLL loading, demand activation, conversion of the protected behavior into current-build regression evidence, and a later dedicated save-name identity review.
