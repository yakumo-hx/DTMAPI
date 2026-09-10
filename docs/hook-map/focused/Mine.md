# Mine ProductNative Hook Map

## Current Boundary

- Product: `DTMAPI.MineMod`
- Assembly: `DTMAPI.Mine.dll`
- Harmony owner: `dtmapi.mod.dtmapi.minemod`
- Physical owner: ProductNative Advanced CodeMod
- Current status: `implemented/open`; the bounded third-save behavior matrix
  remains valid, while an independently found recipe/tech restore-failure
  retry invariant has a focused source/Unit correction pending acceptance.
- Public compatibility: `IMachineProductionApi` and its five DTOs remain
  Experimental / Deprecated / Frozen warning shells. No Runtime or
  Compatibility Host provider remains.

Official JSON remains ContentOwner for `dtmapi_mine`, its native 16-slot
`Case`, workbench recipe/group and appliance threshold `10`. The product owns
only the session-derived production rule, optional recipe/tech mutation and
Mine-only visual lifecycle.

## Atomic Target Set

All three Postfixes pre-resolve and install or roll back as one exact-owner
set:

1. `DolocTown.EquipmentRenderer.OnReuse()` — restores the exact captured
   renderer scale before a pooled renderer can serve another equipment;
2. `DolocTown.EquipmentBuilder.CreateIndicator()` — applies the Mine-only 2x
   placement preview;
3. `DolocTown.EquipmentBuilder.TurnIndicator()` — reapplies the Mine-only 2x
   preview after indicator rotation/rebuild.

An unresolved target, duplicate Mine owner or partial install fails
activation. Other Harmony owners on the same targets are preserved.

## Lifecycle And State

Cold `Enabled=false` installs no Hook, callback, updater or scheduler and
performs no recipe, tech or scale mutation. Cold-enabled Entry installs the
inert exact Hook set; an enabled `SaveLoaded` activates the session, captures
exact recipe/tech originals, and arms each observed Mine at
`current TotalTUs + cycle`. Returning to title, disabling the configuration,
Entry rollback, Loader owner deactivation and process shutdown remove the Mine
owner, detach the single callback root, restore exact native originals and
clear scheduler/RNG/cache state. A successful reverse step removes only its
own restoration closure. A failed step remains queued, keeps cleanup
truthfully failed, and blocks clean reactivation until a later retry succeeds
or the process restarts.

The scheduler is deliberately session-derived. It has no sidecar, journal,
Working/Committed generations or cross-exit continuity promise. Native
`Case.inventory` and native appliance power remain the only saved Mine state.

Full storage and low power return before output construction, inventory copy
or native `Launch()`. Only a cycle that passes preflight captures power and
inventory; a later launch, placement or exception failure restores that exact
snapshot and retains the due cycle.

## Evidence

- Admission Review:
  [20260726-0002](../../archive/reviews/code/2026/20260726-0002-eleventh-product-mine-admission-review.md)
- Owning Update:
  [20260726-0004](../../archive/updates/2026/20260726-0004-mine-eleventh-advanced-product.md)
- Focused Unit fixture proves three-target install, injected step-two rollback,
  unrelated-owner preservation, exact recipe/tech/scale restoration,
  retained failed-restore retry, power/inventory rollback, identity pruning,
  preflight ordering and zero-weight disabling.
- Cold-disabled `GAME-SMOKE/20260726-223105` and restored-enabled
  `GAME-SMOKE/20260726-223236` prove the current SDK-generated DLL, two-Mine
  independence, low power, full storage, hot disable/re-enable, move,
  dismantle, index reuse, title and Loader cleanup, protected-save
  preservation and clean exit.

## Rollback

Revert the Mine admission as one atomic unit: product source/package identity,
Catalog/policy, QA route and the removal of the mandatory MachineProduction
executor. Never leave both the old provider and ProductNative implementation
active, and never delete the frozen public member shape as part of rollback.
