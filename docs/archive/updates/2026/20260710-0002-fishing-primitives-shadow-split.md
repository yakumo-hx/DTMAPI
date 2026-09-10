# 20260710-0002 - Fishing Primitives Shadow Split

Status: source-and-unit-verified / internal-boundary-complete / runtime-counters-verified

## Source Request

Second phase of the requested split: extract native fishing responsibilities, add first-party-only scalar primitives and shadow-capable decisions, and preserve the legacy API and shared Harmony owner.

## Changed Files

- `src/DTMAPI.Abstractions/FirstPartyFishingPrimitives.cs`
- `src/DTMAPI.Abstractions/AssemblyInfo.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingNativeAdapter.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingRuntimeComponents.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingPrimitivesService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs`
- `first-party-mods/AutoFishingMod/FishingDecisionEngine.cs`
- `tests/DTMAPI.UnitTests/Program.cs`

## Summary

- Added a cached native adapter, active-session hook router, deterministic runtime session, due-time scheduler, and bounded transition diagnostics.
- Added one non-preemptive first-party session with scalar snapshots, sequence-checked cast/bite/reel operations, scoped synthetic-input and animation leases, and idempotent release.
- Native pool scanning now occurs only during an actual cast attempt with invalid cache; reflection handles are reused until environment invalidation.
- Added Legacy/Shadow/FirstParty decision modes. The final phase selects FirstParty; shadow counters remain available for targeted compatibility comparison.
- Harmony still uses the existing unified ID. Hook paths are structurally routed but never option-unpatched at runtime.

## Validation

- Release tests cover busy acquisition, stale/duplicate sequence rejection, WaitEntered/WaitPlayable, scheduler zero allocation before due time, cache invalidation, leases, title/owner cleanup, synthetic input decisions, and bounded counters.
- Runtime reports for `GAME-SMOKE/20260710-102651`, `103205`, `103350`, and `105201` show one pool scan per session fixture, bounded reflection misses, zero rejected operations in normal loops, and zero leases after close.

## Evidence Links

- Goal: `docs/goals/2026/20260710-0002-fishing-primitives-shadow-split.md`
- API matrix: `docs/api/public-api-matrix.md`
- Hook map: `docs/hook-map/README.md`
- Regression row: `AUTOFISHING-PRIMITIVES-CUTOVER-20260710`

## Rollback

Return compatibility execution to the old service facade while retaining phase-1 cleanup and input allocation fixes.

## Follow-Up

Only split physical Harmony IDs after close/title/failed-owner and long soak evidence is complete.
