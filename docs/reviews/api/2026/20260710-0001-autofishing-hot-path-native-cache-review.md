# AutoFishing Hot-Path and Native Cache Review - 2026-07-10

## Decision

Keep the first-party Fishing Primitives internal. Rebuild the active AutoFishing read path around a Hook-fed native state cache, keep native cast/bite/reel mutations in `FishingNativeAdapter`, and retain the public Experimental `IFishingAutomationApi` only as a compatibility adapter.

## Confirmed Current Problems

- `FishingRuntimeSession.GetSnapshot()` calls `FishingNativeAdapter.ReadAvailability()`, performs native reflection reads, may scan the scene for a fishing pool, and can advance the session to `Interrupted`. Snapshot reads are therefore neither pure nor allocation-safe.
- high-frequency Ready/Wait/MiniGame callbacks use captured lambdas through the general safe-callback helpers;
- minigame frames repeatedly discover fields/methods, box enum values, call `ToString()`, and invoke `GetNote` through reflection;
- successful fishing diagnostics decide whether to publish only after the caller has already formatted summaries;
- the 2928-line `FishingAutomationService` remains the native executor for the first-party product, despite the product policy and internal primitive session having moved out.

## Native Responsibility

The current reverse baseline is `references/doloc-town/reverse/builds/23762374_public_C416D4`. The authoritative native flow remains:

- `BodyController.UseFishRod` starts the native Ready state;
- Ready/Cast/Wait/Battle/Pull own native fishing phase state;
- `AgentStateFishingWait.OnPlay/NextState` own bite and result routing;
- `FishingGameScrollBar.UpdateGame` owns visible-minigame input/result state;
- `FishRodRenderer.CastHook/Pull/PullCancel` own hook physics and pull duration;
- `Agent.Status.HorizontalMoveFactor` is the native movement-cancel signal.

## Target Boundary

- `FishingNativeStateCache`: Hook-fed scalar/reference cache; no mutations.
- `FishingNativeAdapter`: cached cast/bite/reel transactions.
- `FishingHookRouter`: callback orchestration and primitive transitions.
- `FishingInputOverride`: Ready and minigame scoped input overrides.
- `FishingAnimationController`: Ready/Cast/Pull speed, hook physics, duration, and restore.
- `FishingAutomationCompatibilityAdapter`: legacy Experimental API policy/state only.
- `FishingPrimitivesService`: internal session, sequence, scheduler, and leases.

No public primitive contract is added or promoted. Hook IDs and legacy DTO behavior stay compatible.

## Validation Boundary

This implementation round is source/unit only: Release build, full unit tests, allocation micro-tests, simulated lifecycle cleanup, and diff checks. It must be recorded as runtime-matrix pending. The later matrix is strict zero-catch for ten measured minutes after a sixty-second warm-up, then isolated 100/500 successful-fish runs using InstantBite, SkipMiniGame, Fast x4, and charge zero.

## Implementation Review Result

- The first-party read path now follows `Hook/native frame -> FishingNativeStateCache -> FishingRuntimeSession -> pure snapshot`.
- First-party `RollFish`, bite fields, hook-tip callback, `NextState`, skip flag, and `Overwrite` are compiled/cached in `FishingNativeTransactionCache`; the legacy service retains its reflective executor only for Experimental compatibility consumers.
- A primitive-only session creates no legacy fishing option/state owner. Ready charge and animation callbacks read the primitive session and animation lease directly.
- Repeated clean lifecycle observations are counted before detail construction and publish only on the first state, change/warning, boundary, or 30-second aggregate.
- The current `netstandard2.0` dependency surface did not expose direct `DynamicMethod` construction. The implementation therefore uses fishing-local cached `Expression.Compile()` typed delegates, which preserve private-member access in source/unit coverage and avoid boxing/reflection on the warmed path without adding a new package. Runtime Mono compatibility remains part of the pending game matrix.
- The old service is no longer the first-party native transaction or public API implementation, but remains a callback/diagnostic facade plus legacy compatibility executor. Further mechanical reduction is allowed only after the runtime matrix; it is not needed to reopen the first-party boundary.
