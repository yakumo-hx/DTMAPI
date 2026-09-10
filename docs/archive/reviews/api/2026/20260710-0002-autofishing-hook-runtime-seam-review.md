# AutoFishing Hook Runtime Seam Review - 2026-07-10

## Decision

Run one real fifth-save Mono gate before moving first-party Hook behavior. Then introduce an internal `IFishingHookRuntime` seam, migrate first-party Hook behavior into `FishingPrimitiveHookRuntime`, and isolate the existing implementation as `LegacyFishingAutomationService` under Compatibility.

## Known Facts

- Source/unit verification proves the compiled accessor warm paths, but Unity Mono has not executed the new `Expression.Compile()` private-member delegates.
- A single fish with InstantBite, visible minigame, Fast x4, and charge zero reaches StateCache, Ready, Wait, minigame note/input, Cast/Pull animation, and ReturnedToTitle cleanup without creating a soak workload.
- `FishingRuntimeSession.Release()` currently invokes `Transitioned` before clearing references and before calling the primitive service release. A throwing subscriber can therefore retain the session, leases, provider, scheduler, and native references.
- `FishingNativeAdapter.InvalidateEnvironment()` clears the cached `UseFishRod` delegate even though the agent/rod method types are process-stable. Environment invalidation should clear scene/pool/native references, not a type-level call delegate.
- Accessor telemetry currently mixes first builds, type rebuilds, construction failures, and delegate invocation failures. Mono gate diagnostics need those dimensions separated.
- Static Hook callbacks currently reach `FishingAutomationService` directly. This couples first-party AutoFishing to construction of the 2937-line legacy compatibility implementation.
- First-party branches already exist inside the old service for phase routing, Ready control, animation leases, minigame input/status, and lifecycle cleanup; they can move without changing public APIs or Harmony targets.

## Rejected Directions

- Do not run 100/500 fish or any soak before proving one complete Mono path.
- Do not mechanically split the legacy service before first-party construction is cut; line-count reduction alone does not remove its object root from ordinary AutoFishing.
- Do not let HookBridge or static callbacks select between concrete legacy/primitive types.
- Do not allow primitive and compatibility runtime modes to coexist, even for the same owner ID.
- Do not publish Fishing Primitives or `IFishingHookRuntime` as public APIs.

## Target Boundary

```text
Fishing Harmony callbacks
  -> IFishingHookRuntime
       -> FishingPrimitiveHookRuntime (first-party only)
       -> LegacyFishingAutomationService (Experimental compatibility only)

FishingAutomationFeature
  -> one active runtime mode, never both
```

## Validation Order

1. Current source: one fifth-save Mono fish, no soak, InstantBite=true, SkipMiniGame=false, Fast=4, charge=0, then product release and ReturnedToTitle.
2. Require functional loop, accessor invocation failures zero, and title cleanup zero.
3. Apply lifecycle/counter/interface/runtime split.
4. Release build, full unit suite, allocation checks, script parse, and diff check.
5. Do not rerun soak in this task.

## Result

- Both pre-seam `docs/debug/evidence/GAME-SMOKE/20260710-233452` and final post-seam paths reached Ready, Cast, Wait, one visible native minigame, Pull, product release, and ReturnedToTitle. The pre-seam gate itself passed; only its then-missing common loop marker made the outer result fail.
- The final evidence is `docs/debug/evidence/GAME-SMOKE/20260710-235813`: all requested smoke fields passed, all accessor failure dimensions were zero, and title cleanup removed callback/runtime/session/lease/native state.
- Gate investigation rejected the assumption that reflected `AgentStateFishingWait.NextState()` switches the native state manager. The first-party path now queues the reviewed scoped native use-tool edge so the game performs its normal Battle transition.
- Current game `FishingNoteType` contains Delay, Stable, and Bonus; `Avoid` is not present. Treating Avoid as an optional capability removed a false accessor construction failure without changing input decisions for actual notes.
- Static callbacks now see only `IFishingHookRuntime`; first-party AutoFishing creates `FishingPrimitiveHookRuntime`, while compatibility enable creates `LegacyFishingAutomationService`. The modes cannot coexist even for the same owner ID.
- This closes the seam and single-fish Mono uncertainty only. It does not provide 100/500-fish allocation or long-gameplay evidence.
