# 20260719-0006 - Batch 5 AutoFishing Native Control No-Water Spawn

- Date: 2026-07-19
- Status: recorded historical failure; formal-fix conclusion corrected by [Review 20260719-0007](20260719-0007-batch5-autofishing-fifth-save-energy-budget.md)
- Severity: P1 acceptance-fixture blocker; no product crash, save corruption, source-tree drift, or GC terminal result observed
- Owning Update: [20260718-0003 Batch 5 Event, Demand, Content Invalidation, Lifecycle And Performance Boundary](../../../updates/2026/20260718-0003-batch5-event-demand-content-lifecycle-performance.md)
- Failed ladder: `docs/debug/evidence/BATCH5-GC-LADDER/20260719-101706-1f2dfe53`
- Failed stage: `docs/debug/evidence/BATCH5-GC-LADDER/20260719-101706-1f2dfe53/AutoFishing-L0/stage.json`
- Failed smoke: `docs/debug/evidence/GAME-SMOKE/20260719-101708`
- Preceding state-machine review: [20260719-0005 Batch 5 AutoFishing Native Session Normal-State Stall](20260719-0005-batch5-autofishing-native-session-normal-state-stall.md)
- Correction review: [20260719-0007 Batch 5 AutoFishing Fifth-Save And Energy-Budget Correction](20260719-0007-batch5-autofishing-fifth-save-energy-budget.md)

> Correction, 2026-07-19: this record remains authoritative for the observed third-save `no-water` failure in `101708`. Its later conclusion that formal AutoFishing should transport the third-save fixture to the wharf is superseded. The user reconfirmed that formal AutoFishing uses the existing fifth-save pond fixture. `GAME-SMOKE/20260719-105158` is retained as semantic destination-resolution and actual-transport evidence, not as formal GC evidence. Formal implementation and replay remain in progress.

## User-Confirmed Save Layout

The user deliberately moved the third-save player spawn to the right edge of the large barn, slightly to the right of the animal bell. One or two short presses of `A`, followed by `E`, exposes the bell interaction. The right side was cleared so the ordinary AnimalViewer and Zoom acceptance paths have a stable, unobstructed start. This spawn is authoritative for those ordinary no-QA checks and must not be permanently moved merely to make an AutoFishing fixture convenient.

The user also reported that a Zoom test waypoint previously placed the player below the visible ground, where the black map-boundary mask obstructed screenshot verification. That is a separate QA-position concern; it does not establish a gameplay or camera product defect.

## Observed Result

The attempted six-stage AutoFishing replay started with the same formal definition as the retained ActionSpeed ladder: 600-second measurement, 30-second samples, ten measured fish, five warmup fish, third save, and frozen AutoFishing OfficialLocal tree SHA-256 `FB2074989669485D899B0C1166B0F8FDABB2A04F6045BCFFEDA6A12E89937A97`.

`AutoFishing-L0` reached `SaveLoaded`, reported normal gameplay state, acquired the QA-owned native-control session, and then attempted 177 casts. Every attempt remained at phase `Idle`, sequence `1`, with `applied=False` and `status=no-water`. The 90-second no-progress watchdog added after review 0005 then failed the fixture diagnostically. The stage completed at the failed-host cleanup boundary with no measurement interval, zero measured fish, and all eleven GC metrics unavailable.

The runner restored the official profile and Author source state. The exact frozen product tree was restored, no `DolocTown.exe` remained, and the shared runtime lock was free. No fresh fatal window or crash was observed. This smoke did **not** request the player-save transaction (`PlayerSaveRestored=Skipped`). A separate read-only post-run comparison found the three third-save files still byte-identical to the retained `034102` baseline, so this attempt did not change them; that comparison is not a substitute for enabling the save transaction before the fixture starts using native transport.

This is not a GC-gradient result and does not invalidate the 24 retained ActionSpeed stages. The AutoFishing fixture never entered warmup or measurement.

## Root Cause

The QA native-control path invokes the native `BodyController.UseFishRod(ItemFishingRod)` responsibility and correctly preserves the native rejection. `FishingNativeAdapter` returns `no-water` before that invocation when its current-scene `UnityEngine.Object.FindObjectsOfType(DolocTown.FishingPool)` scan finds no active pool. The user's new barn/bell spawn is in the farm room, and the failure therefore means the current farm scene has no active `FishingPool`; moving a few coordinates inside the same room cannot satisfy the precondition. The fixture currently assumes that the third-save room already owns a fishing pool.

The inspected public build provides a deterministic native transport research route. `city_多洛可码头` contains three enabled `FishingPool` owners whose collider coverage spans approximately `x=72..189`, `y=-13.5..6.5`. The existing whitelisted destination `mark:下船点-左` enters that room at approximately `(117, 10.5)`, above the continuous pool region. DTMAPI's existing QA/debug transport bridge delegates this destination to `DolocAPI.DoTransport`; it does not create or override a pool. This establishes a useful transport hypothesis for the failed third-save experiment, but it is not the formal AutoFishing fixture route.

The previous normal-state defect is rejected for this failure: logs show `normalState=True`, repeated cast actions, and the bounded watchdog firing. The frozen product/source trees, GC sampler, Steam profile application, save restoration, selected rod, and native fishing-pool discovery are also rejected as primary causes of `101708` because the operation reached the native cast boundary and returned the specific `no-water` status on every attempt. That statement is limited to `101708`: later exploratory smoke `105158` initially reported `no-selected-rod` after transport, so third-save selected-rod readiness is not a stable formal-fixture fact.

## Ownership Boundary

This is a QA-fixture environment-selection defect, not permission to weaken the product's native `no-water` behavior. The original third-save attempt selected the wrong formal fixture. The correction belongs in the Batch 5 runner and `DTMAPI.GameBridge.DolocTown.QA`: formal AutoFishing must use the existing fifth-save pond fixture, while ActionSpeed and unrelated third-save acceptance retain their own route. The correction must not add a new public teleport surface, modify ordinary AutoFishing semantics, synthesize a successful cast, reinstate a fishing-pool fixture override, bypass `UseFishRod`, change a player's saved spawn, or make AnimalViewer/Zoom depend on a fishing room.

The fifth-save fixture must still read back a real pool, currently selected `ItemFishingRod`, native energy/spirit readiness, and accepted fishing progress. Historical fifth-save success and visual proximity to water do not replace current-stage readiness receipts.

## Required Correction

1. Bind formal AutoFishing L0-L5 to the fifth save and keep ActionSpeed on the third save. L5 must reload the fifth save.
2. Withdraw or disable the third-save wharf transport workaround from the formal AutoFishing GC route. Preserve `GAME-SMOKE/20260719-105158` as semantic destination-resolution and actual-transport evidence only.
3. Before session acquire or product enable, require current fifth-save room/pool, selected `ItemFishingRod`, and energy/spirit readiness receipts. The native `compose_energy`/`compose_spirit` and `get_energy_percent`/`get_spirit_percent` responsibilities and the exact budget are owned by [Review 0007](20260719-0007-batch5-autofishing-fifth-save-energy-budget.md).
4. Enable the runner's byte-exact fifth-save transaction for every AutoFishing stage. Any bounded in-memory QA adjustment must be read back and the protected fifth-save triple must reproduce byte-for-byte after process exit.
5. Preserve native `no-water`, insufficient-energy, `UseFishRod`, visible-reel and PullExited semantics. Do not count rejected casts or synthetic success.
6. Replay AutoFishing L0-L5 against the same frozen product tree and formal 600/30/10/5 definition only after the corrected source and unit gates pass. Aggregate the new evidence root with the retained 24-stage ActionSpeed root without rewriting either failed ladder.

## Acceptance Gate

The corrective replay is acceptable only when all six AutoFishing stages independently use the fifth save and pass, each has all eleven required metrics plus its level-specific behavior receipt and energy/spirit readiness receipt, L0 records real native cast plus visible-reel acceptance for every measured fish, L4 records a same-cycle applied/queued/consumed/native-accepted recovery transaction, L5 reloads the fifth save, and the runner verifies exact official/source/fifth-save restoration with no residual process or runtime lock.

`GAME-SMOKE/20260719-105158` remains useful evidence that `mark:下船点-左` resolves through the whitelist and that `DolocAPI.DoTransport` actually reaches the wharf room and its active pool. Its overall failed result, third-save source, incomplete formal duration/metrics, and initial `no-selected-rod` state exclude it from the formal GC pass set.

If the fifth-save fixture, selected rod, or bounded energy/spirit budget cannot be justified from native owners and verified without persistent player-state drift, Batch 5 remains blocked at the AutoFishing formal ladder rather than weakening the acceptance gate.
