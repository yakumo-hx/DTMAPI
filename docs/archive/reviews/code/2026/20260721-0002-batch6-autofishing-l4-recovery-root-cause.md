# Batch 6 AutoFishing L4 recovery root-cause review

Status: recorded

Source request: continue G3-G6 from the second formal ladder failure without replaying L0-L3 or running the full Release suite.

Owning lifecycle record: [Batch 6 AutoFishing Advanced pilot](../../../updates/2026/20260720-0008-batch6-autofishing-advanced-pilot.md)

## Observed failure

The second formal ladder completed the L4 product measurement, disabled the real `ModEntry` updater/session, and acquired a distinct QA recovery session, but the following 300 seconds produced no recovery unit. The terminal receipt reported `recoveryDriverNativeProgress=false` and `recoveryDriverUnits=0` while cleanup and shared-runtime restoration still passed:

- [second formal ladder plan](../../../../debug/evidence/BATCH6-AUTOFISHING-GC-LADDER/20260721-132436-b7e23d5f/ladder-plan.json)
- [second formal L4 runtime receipt](../../../../debug/evidence/GAME-SMOKE/20260721-140825/DTMAPI-evidence/AUTO-FISHING-PERF/6fc38ed6e66f4f21b57ae4516d32dfdc/auto-fishing-performance.json)

## Code and diagnostic facts

- The recovery driver acquired ProductNative primitives immediately after F6-off. It did not wait for `DolocAPI.IsNormalState`, although the retired Batch 5 recovery fixture had an explicit `NormalGameState` entry gate.
- Its initial cast branch required the composite primitive snapshot `CanCast`. That composite is false while `HasFishingPool` is false.
- Product disable invalidates the pool cache. The native adapter refreshes the pool cache inside `TryCast`, so requiring the composite value before invoking `TryCast` creates a circular QA precondition.
- A short non-authoritative L4-only diagnostic showed a second unsafe path: the recovery session entered while the product's last native cast was still in `AgentStateFishingCast`. The QA driver never applied `TryCast`; it consumed the inherited `WaitPlayable -> BiteReady -> PullExited` transitions and would otherwise count that product-originated loop as QA recovery. See [diagnostic runtime log](../../../../debug/evidence/GAME-SMOKE/20260721-150848/DTMAPI-latest.log).
- The same diagnostic proved that the real product, package, and ProductNative hooks can complete the native transition chain; the defect is in the QA recovery admission/accounting boundary, not evidence that the product DLL must change.

## Root cause and ownership

The L4 QA harness conflated three states: product inactive, native fishing idle, and QA-originated recovery progress. It entered before native idle, used a composite post-scan value as a pre-scan cast gate, and accepted any later `PullExited` delta without proving that the QA owner initiated the corresponding cast.

Ownership is the existing Batch 6 AutoFishing QA harness and ladder runner. No new public API, ProductNative behavior, receipt family, or release gate is justified.

## Rejected interpretations

- Increasing the 300-second timeout cannot resolve a cast branch that never becomes eligible.
- Relaxing the native-progress assertion would turn the recovery gate into a cleanup-only check.
- Treating an inherited product cast as independent QA recovery does not prove post-disable re-entry.
- Rebuilding or changing the Advanced product package is not supported by the diagnostic evidence and would unnecessarily invalidate the already accepted L0-L3 hashes.

## Acceptance gates

1. After F6-off, wait for `Gameplay`, `DolocAPI.IsNormalState`, the selected fifth-save fishing rod, and at least one fishing pool before acquiring the QA recovery session.
2. Permit the QA cast attempt from a native-normal, selected-rod state; let the existing native adapter perform its own pool scan and fail closed on a real no-water result.
3. Count L4 recovery only after this QA driver has an applied `TryCast` and the resulting recovery window advances through at least one new `PullExited`.
4. Prove product updater/session inactivity throughout the recovery window and exact zero QA resources, leases, scheduler work, and product transients after cleanup.
5. Because only QA/runtime harness code changes, formally rerun L4 alone, run L5 alone for the first time, and reuse the unchanged-package second-round L0-L3 evidence. A full L0-L5 replay is required only if product/Runtime/package hashes change.

## Resolution checkpoint

The harness-only corrections through `aca65a15` satisfy the acceptance gates without changing the product DLL, mandatory Runtime, manifest, policy or package hashes:

- formal L4-only parent [`20260721-152146-c761b08d-l4-only`](../../../../debug/evidence/BATCH6-AUTOFISHING-GC-LADDER/20260721-152146-c761b08d-l4-only) and smoke [`GAME-SMOKE/20260721-152148`](../../../../debug/evidence/GAME-SMOKE/20260721-152148) pass after an independently QA-originated applied cast, one resulting native recovery unit, continuous product inactivity, zero residual sessions/resources/transients, clean restoration and lock release;
- formal L5-only parent [`20260721-155340-d24150aa-l5-only`](../../../../debug/evidence/BATCH6-AUTOFISHING-GC-LADDER/20260721-155340-d24150aa-l5-only) and smoke [`GAME-SMOKE/20260721-155342`](../../../../debug/evidence/GAME-SMOKE/20260721-155342) pass title return/re-entry, six handshakes, four input events, provenance, cleanup and restoration;
- the unchanged exact product/package hashes allow the second formal ladder's passing L0-L3 evidence to remain authoritative. No third L0-L3 replay was run.

This closes the L4 harness root cause. Lifecycle completion and Batch 6 status remain owned by the linked Update and its final Release/independent-acceptance gates; this Review does not independently mark either lifecycle `verified`.
