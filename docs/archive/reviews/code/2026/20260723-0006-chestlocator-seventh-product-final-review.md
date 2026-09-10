# ChestLocatorEnhancer Seventh Product Final Review

**Review ID:** `20260723-0006`

**Date:** 2026-07-23

**Status:** `recorded — independent PASS; no remaining P0/P1/P2 finding`

**Scope:** independent closeout review of the already implemented Phase 1/4
baseline and the uncommitted ChestLocatorEnhancer seventh-product extraction.
This Review records the acceptance decision only; Update `20260723-0007`
remains the implementation lifecycle owner.

## Verdict

**PASS.** Phase 1 Core legacy seams and Phase 4 public-API contract tails remain
closed. ChestLocatorEnhancer satisfies its sole-product admission boundary,
preserves the frozen ABI through the existing dormant Compatibility Host, and
reduces default-loaded Runtime volume without adding SharedNative capability,
a second Host, a public API, a receipt family or a product-specific gate.

No P0, P1 or P2 finding remains after the corrections described below. The
seventh-product Update may advance to `verified/closed`; this decision admits
no eighth product and does not change the G7 or 0.5.5 release blocks.

## Phase 1 And Phase 4 Recheck

Focused Unit groups passed independently:

- `phase1-core-cleanup`;
- `phase4-api-cleanup`;
- `api-metadata`.

The removed internal string-input methods
`RecordInputPressed`, `RecordInputReleased` and
`GetRegisteredInputButtons` were not reintroduced. The unowned
`NotifySaveLoadFatalWindowObserved` chain and its obsolete tests remain absent;
public input ABI, typed input and external fatal-window detection remain the
intentional frozen/current boundaries.

CustomEntity declarations remain Experimental/Frozen rather than
StableCandidate. Provider registration retains one authoritative owner and
rejects conflicting duplicates. AutoHarvest does not depend on
`IInstantSaveDebugApi`; the remaining use belongs to DebugConsole. Helper
owner, main-thread and stale-owner behavior remains covered without expanding
C# CustomEntity or implementing a first-party AutoHarvest.

## ChestLocatorEnhancer Ownership And Compatibility

The unique native responsibility remains:

```text
ArchiveDataHandle.GetAvailableInventories(
    Vector2Int anchor,
    Vector2Int area,
    bool useBox)
```

Native `LinearInventory` instances, CountItem/CostItem transactions,
persistence and UI remain game-owned. The sole real product consumer is
`DTMAPI.ChestLocatorEnhancerMod`; ProductNative owns only its widening policy,
configuration, diagnostics and one exact Postfix under
`dtmapi.mod.dtmapi.chestlocatorenhancermod`.

The old `IChestLocatorEnhancerApi` surface remains
Experimental/Deprecated/Frozen. Mandatory GameBridge contains a thin
on-demand provider/demand proxy, while the heavy executor is a seventh domain
inside the existing dormant-shipped Compatibility Host. The Host is still
outside BepInEx scan paths and mandatory GameBridge has no static AssemblyRef
to it.

Focused Unit groups `chestlocator-product`, `batch5-gamebridge-demand` and
`compatibility-host` passed independently. They cover both product-first and
compatibility-first activation, pending-demand reconciliation, atomic
pre-resolution and rollback, exact-owner residual rejection, disable/re-enable
and save/title observation reset. Both activation orders fail closed before a
second physical Postfix can exist; cleanup does not unpatch the other owner.

The product Hook is intentionally process-lifetime while enabled. SaveLoaded
and ReturnedToTitle clear observation state but do not repeatedly unpatch and
reinstall it. Disable, failed Entry and real Loader owner deactivation perform
exception-safe exact-owner unpatch and report residue as failure.

## Authority Correction Reviewed

The Phase 0 contract now separates live topology from its historical receipt:

- `product-mirror-apis.knownTrackedConsumers` contains only the six current
  source consumers still using product-shaped APIs;
- `baselineKnownTrackedConsumers` preserves the twelve historical
  `2026-07-20` source roots;
- the focused Phase 0 test scans `first-party-mods`,
  `products/first-party` and `testmods`, and exact-matches both current files
  and current roots without rewriting the frozen receipt.

`tools/scripts/test-batch6-phase0-contract.ps1` passed independently. This
closes the earlier false-current-consumer finding and keeps topology and
dormant-shipped authority honest.

## Runtime Evidence

`GAME-SMOKE/20260723-203127` is the product-behavior acceptance:

- current Runtime, HookProbe and the Advanced product loaded the third save;
- the reviewed native target had one exact product Postfix;
- native inventory behavior was
  `case_locator 0 -> 3 -> 1` through official CountItem/CostItem;
- title restoration passed;
- real Loader deactivation observed
  `actual1+callback1 -> instance0+actual0+callback0+roots0`;
- the three player-save files, official profile, Author source state and QA
  tree were restored, followed by a clean process exit.

`GAME-SMOKE/20260723-210424` closes the same-process re-entry requirement:

- `RunStatus`, startup, HookProbe, SaveLoaded, SaveLoadCycle, title-boundary,
  no-fatal-window, player-save restoration, QA cleanup and process exit all
  passed;
- the coordinator observed
  `requests=2, nativeEnter=2, nativeReturn=2, saveLoaded=2` for native slot
  index `2`;
- the product installed its exact owner once at startup with `count=1`,
  survived both title boundaries without a duplicate install, and received
  both SaveLoaded resets with no event-handler failure;
- `Smoke exercise SaveLoadCycle OK` reported two completed cycles.

`GAME-SMOKE/20260723-205205` and `20260723-210101` returned to title before any
save-load request and are non-acceptance orchestration conflicts, not product
failures. Each runner still restored its scoped state. They are not used as
positive evidence.

## Reduction Claim And Cross-Product Boundary

The mandatory Chest boundary changes from 542 physical / 475 non-empty lines
to 243 / 206, reducing it by 299 physical / 269 non-empty lines. Mandatory
GameBridge changes from 966,656 to 937,984 bytes, reducing it by 28,672 bytes
(about 2.97%).

The 540-line frozen executor remains shipped in the Compatibility Host, and
the ProductNative DLL is separately shipped. The valid claim is therefore only
a reduction in default-loaded Runtime volume. It is not a download,
installed-footprint, combined-binary, total-source or total-repository
reduction.

All seven products share the Catalog-driven Advanced SDK/package path,
Doctor/Manager projections, owner-scoped ConfigMenu and Loader lifecycle
conventions. No other real product shares ChestLocatorEnhancer's native
inventory-query owner or widening invariant. These are reusable Platform
mechanisms, not proof for a new SharedNative inventory capability.

## Validation Boundary

The independent review used focused source inspection, the focused Unit groups
listed above, the focused Phase 0 contract test, and the two bounded game
evidence roots. It did not run a complete Release, L0-L5, a GC ladder, a long
test or a 0.5.5 publication flow.

The portable reverse-capture Update and tool directory were outside this
review and were not modified.
