# AnimalHusbandryProgress ProductNative Refresh Optimization

## Metadata

- Update ID: `20260723-0001`
- Date: `2026-07-23`
- Lifecycle Status: `verified`
- Validation Level: `docs,source,unit,runtime`
- Runtime Validation: `passed`
- Related Issue State: `closed`

## Source Request

Optimize only the admitted `AnimalHusbandryProgress` ProductNative viewer path:

- build, sort and cache stable progress rows once while the panel data is constructed;
- invalidate the active overlay when data/animal selection changes instead of repeating reflection, array copies, LINQ, strings, color construction and descendant scans every 80 ms;
- retain one next-frame guard for the native UI's first-frame rewrite;
- preserve the current exact Harmony owner, native close, save/title reset, owner deactivation and zero-residual semantics;
- validate with focused Unit/source checks and one bounded third-save repeated-animal-switch, close and title-recovery acceptance only.

No sixth product, public API expansion, SharedNative UI layer, complete Release,
L0-L5, GC ladder, long test or 0.5.5 publication is authorized by this Update.

## Owning Review

- [Production QA Seam And Animal Refresh Audit](../../reviews/code/2026/20260722-0011-production-qa-seam-and-animal-refresh-audit.md)
- [AnimalHusbandryProgress Fifth Product Admission Review](../../reviews/code/2026/20260722-0007-animalhusbandryprogress-fifth-product-admission-review.md)

The reviewed native responsibility remains `AnimalFullInfoData::.ctor`,
`AnimalViewer::Show/OnShow`, `ProgressBar::SetProgress` and
`AnimalPanelUiState::Unregister`. The native viewer writes its stable state once;
the former 80 ms reassertion loop is product-owned overhead, not a shared native
owner.

## Implementation

Changes are limited to the Animal product and focused tests:

- replace repeated sorting with one stable row plan per native data object;
- cache each clone's progress-bar methods, text/graphic references, formatted
  progress text and parsed color during construction;
- write while hidden, activate, then consume at most one next-frame guard;
- make later `UpdateTicked` callbacks take a zero-write fast path until the
  active overlay is invalidated;
- destroy tracked clones once in reverse order, retaining named-child discovery
  only as one fallback cleanup scan;
- keep exception-isolated state cleanup, callback detach and exact-owner unpatch.

The existing product Catalog's Schema-5 production-source metric advances from
211 to 212 because the product-local session primitive is a new production
source file. Both the structured metric and its existing `currentDebt`
projection were updated in place; no new Catalog authority or checker was
created.

`AnimalOverlaySessionState<T>` is the product-local render-session primitive.
It stores tracked rendered rows, owns the one guard, and clears tracked native
clones exactly once in reverse order. Active data/panel identity remains in
`AnimalHusbandryNativeRuntime`. `AnimalStableRowPlan` performs the single
descending top-three sort. The Runtime now captures the clone's progress
methods, text/graphic targets, parsed color and formatted text during the
initial hidden write; ordinary `UpdateTicked` calls return before reflection or
allocation after the guard is consumed.

The staged QA fixture performs its read-only product-row preflight once, then
selects three existing animals in sequence and requires a newer product receipt
plus `nextFrameGuard=completed` after every selection. It does not reconstruct
temporary animal data on each QA tick.

## Changed Files

- `products/first-party/AnimalHusbandryProgress/src/Native/AnimalHusbandryNativeRuntime.cs`
- `products/first-party/AnimalHusbandryProgress/src/Native/AnimalOverlaySessionState.cs`
- `products/first-party/AnimalHusbandryProgress/src/Native/NativeReflection.cs`
- `tests/DTMAPI.UnitTests/AnimalHusbandryProductTests.cs`
- focused Unit, QA fixture, product-source gate and Catalog projections
- Animal Hook/smoke authority, Batch 6 contract, lightweight roadmap, this
  Update and the July ledger

## Validation Plan

- focused Animal product source gate;
- focused Unit coverage for stable top-three ordering, one guard, zero writes
  across 100 clean updates and exactly-once reverse-order cleanup;
- product build/package checks required by the existing focused entry;
- one third-save short acceptance: switch continuously across multiple animals,
  verify the localized product row and native mood remain intact, close the
  native panel, return to title, deactivate the exact owner and exit cleanly.

## Evidence

Focused validation:

- `DTMAPI_UNIT_TEST_FOCUS=animalhusbandry-product` with
  `DTMAPI.UnitTests`: PASS. It covers stable top-three ordering, one initial
  write plus one guard, zero writes across 100 clean updates, guard cancellation
  on invalidation, and exactly-once reverse-order cleanup.
- `test-batch6-animalhusbandryprogress-advanced-product.ps1`: PASS
  (`source-files=7`, `patches=4`, `targets=3`, `policies=1`).
- `DTMAPI.QaUnitTests`: PASS after the repeated-switch fixture update.
- `check-product-catalog.ps1`: PASS
  (`products=27`, `public=11`, `workshop-items=21`, `api-rows=48`) after
  advancing the production-source projection from 211 to 212.
- Existing Catalog-driven Advanced builder with the already-built Author SDK:
  PASS. Package
  `DTMAPI-AnimalHusbandryProgress-advanced-pilot.zip` has SHA-256
  `ADC26019ADE5CDD44454E1728EE3B9B94CC595267FF384D39B6872FBAFCFD84E`.
  The thin product wrapper's attempted Author SDK rebuild remains subject to
  the already-known tracked Abstractions `0.1` hash mismatch, so the generic
  builder reused the frozen SDK root rather than creating another toolchain.

Runtime acceptance:

- [`GAME-SMOKE/20260723-115821`](../../../debug/evidence/GAME-SMOKE/20260723-115821)
  passed on save slot 3 with only the current Animal product selected.
- Startup, HookProbe, save load and the product's exact four-patch/three-target
  owner were observed.
- The QA-owned native panel switched animal indexes `0`, `1`, and `2`.
  Each switch observed at least one newer render receipt and completed one
  next-frame guard. Every `RenderAfterShow` performs its initial hidden write
  and rearms at most one guard; the index-0 `RefreshView` plus `Select` path can
  render twice. The final receipt therefore reports
  `writes=initial+next-frame-guard`, `repeatedSwitches=3`,
  `receiptSequence=6`, and the localized read-only `羊毛脂 0/100` row.
- Native close, bounded screenshot, title return and owner deactivation passed.
  Deactivation moved `actual4+targets3+callback1` to
  `instance0+actual0+callback0+roots0`; the title boundary reports
  `nativeData=0`, `overlayObjects=0`, `overlayRows=0`.
- Player save, official selection and local Author source state were restored;
  `DolocTown.exe` exited without a forced close. The shared runtime lock was
  released.

No complete Release, L0-L5, GC ladder, long test or publication was run.

## Rollback

Revert the Animal-only rendering/session changes and focused tests together.
Do not move the product renderer back into GameBridge, change the four-Hook
inventory or revive the frozen `IAnimalViewerApi` executor as the product path.

## Follow-up

Project the already-reviewed G1 Oil/Mine ownership conclusion into its existing
authorities. A sixth-product admission remains a separate Review and may not
implement a product unless that Review passes.
