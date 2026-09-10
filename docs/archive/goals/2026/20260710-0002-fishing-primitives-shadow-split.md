# 20260710-0002 Fishing Primitives Shadow Split

## Summary

Extract first-party Fishing Primitives and internal native responsibilities inside GameBridge while preserving the legacy `IFishingAutomationApi` contract and unified Harmony ID.

## Scope

- Separate cached native access, hook routing, runtime session state, auto-cast scheduling, and bounded diagnostics.
- Add a first-party-only primitives contract with a single non-preemptive session, scalar snapshots, stage sequence numbers, native cast/bite/reel transactions, scoped synthetic-input lease, scoped animation lease, and deterministic release.
- Reject stale or duplicate snapshot sequences before mutating native state.
- Distinguish `WaitEntered` from `WaitPlayable`; permit InstantBite preparation only after the playable boundary.
- Add `FishingDecisionEngine` modes `Legacy`, `Shadow`, and `FirstParty`; keep per-frame minigame mismatch reporting aggregated.
- Keep hooks installed through the existing shared Harmony owner and route them through active session/lease checks.

## Acceptance

- Unit tests cover adapter cache invalidation, scheduler pre-deadline zero allocation, one action per sequence, busy acquisition, leases, lifecycle release, Wait boundaries, and synthetic input decisions.
- Primitives expose no Unity, Harmony, or decompiled types and are inaccessible to ordinary mods at compile time.
- Legacy API behavior and DTO stay compatible.

## Outcome

Implemented. The decision engine retains Legacy/Shadow comparison support, while the completed third phase sets the product's execution mode to `FirstParty`. Runtime reports now expose bounded primitive transition, action, pool-scan, reflection-miss, lease, and mismatch counters.

## Rollback

Return the compatibility controller to the legacy service facade and remove the first-party assembly friendship. Keep owner cleanup and phase-1 input allocation fixes intact.
