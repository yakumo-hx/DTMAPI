# 20260710-0006 AutoFishing Hot Path and Native Cache

## Status

- Implementation: source/unit complete; runtime 0/100/500 matrix pending.
- Fixed target version: `0.5.2-alpha`; no version bump.
- Review: `docs/reviews/api/2026/20260710-0001-autofishing-hot-path-native-cache-review.md`.

## Objective

Make first-party fishing snapshots pure and Hook-fed, eliminate reflection/boxing/captured delegates from the active fishing frame path, defer successful diagnostics until a boundary/change/interval, and split the legacy executor into bounded internal components without changing player behavior or exposing Fishing Primitives publicly.

## Required Changes

1. Add cached typed native accessors and `FishingNativeStateCache`; refresh it before native-frame Mod dispatch and from fishing phase hooks.
2. Make `GetSnapshot()` a side-effect-free struct read. Move native-exit reconciliation to cache refresh.
3. Replace high-frequency Fishing callback lambdas with direct fail-open `try/catch` calls. Cache minigame fields, methods, time getter, and enum values.
4. Gate diagnostic formatting before strings/snapshots are built. Repeated success increments counters only; boundaries, failures, state changes, and one 30-second aggregate may publish.
5. Extract Hook routing, input override, animation control, native state, native transactions, and the legacy compatibility adapter. Keep process-long shared Harmony installation.
6. Preserve the single Gameplay toggle registration, local movement snapshot fallback, charge and option independence, and internal-only primitives.

## Validation

- Release build, full unit suite, allocation micro-tests, simulated acquire/phase/release/save/title/owner cleanup, and `git diff --check`.
- Add disabled-by-default performance telemetry for later strict 0/100/500 fish evidence.
- Do not launch the game or classify runtime complete in this goal execution. Keep ISSUE-010 open.

## Acceptance

- warmed `GetSnapshot()` performs no native reads, mutations, diagnostics, or allocations;
- warmed native/minigame frame access uses compiled cached delegates with no reflective fallback;
- repeated success does not linearly publish Hook/lifecycle/object-graph status;
- all leases/native references restore to zero in simulated lifecycle tests;
- public API surface and AutoFishing behavior remain unchanged.

## Implemented Result

- Hook-fed `FishingNativeStateCache` is refreshed before native-frame dispatch; `GetSnapshot()` is now a pure struct read.
- Warmed snapshot, minigame frame, native transaction, and scheduler micro-tests report zero current-thread allocation with stable accessor counts.
- First-party cast/bite/reel no longer executes through legacy `FishingAutomationService`; compatibility options/state remain zero for a primitive-only session.
- High-frequency fishing hooks use direct fail-open exception isolation, and repeated lifecycle success is gated before full snapshot/string construction.
- The disabled performance route and JSON schema are present, including exact allocation-counter blocking behavior and returned-title cleanup fields.
- Release build, full unit suite, script parse, and diff checks are the completion gate for this source/unit round. No game matrix was run; ISSUE-010 remains open.

Implementation uses cached fishing-local `Expression.Compile()` typed delegates because the current `netstandard2.0` reference graph does not expose a direct `DynamicMethod` surface without adding another runtime dependency. It preserves the required no-boxing/no-reflective-fallback active path and latches failed capabilities unavailable.
