# 20260710-0007 AutoFishing Hook Runtime Seam

## Status

- Implementation: complete.
- Runtime target: one fifth-save fish passed; no soak was run.
- Parent review: `docs/reviews/api/2026/20260710-0002-autofishing-hook-runtime-seam-review.md`.

## Objective

Prove the compiled fishing accessors once under Unity Mono, harden release/type-cache lifecycle, route Fishing hooks through an internal runtime interface, move first-party Hook behavior into `FishingPrimitiveHookRuntime`, and prevent ordinary AutoFishing from instantiating the legacy compatibility service.

## Required Changes

1. Add a one-fish Mono gate: InstantBite=true, SkipMiniGame=false, Fast x4, charge 0, one complete visible-minigame Pull, product close, ReturnedToTitle, accessor failures zero, and all transient/session/lease/provider/native references zero.
2. Preserve the `UseFishRod` type delegate across environment invalidation.
3. Split accessor telemetry into Builds, Rebuilds, BuildFailures, and InvocationFailures.
4. Make `FishingRuntimeSession.Release()` cleanup unconditional with `try/finally`, including a throwing transition subscriber.
5. Add internal `IFishingHookRuntime`; HookBridge and static callbacks depend only on it.
6. Add `FishingPrimitiveHookRuntime` for first-party phase/Wait, Ready/animation, minigame/input/status, Cast/Pull/restore/native-exit, and lifecycle counts.
7. Primitive activation creates only the primitive Hook runtime. Compatibility `SetEnabled(true)` alone may create the legacy service.
8. Rename/move the old implementation to `Compatibility/FishingAutomation/LegacyFishingAutomationService.cs` without prioritizing mechanical line-count reduction.

## Acceptance

- While a primitive session is active, `FishingAutomationFeature.Service == null`.
- Hook callback runtime is non-null and is `FishingPrimitiveHookRuntime`.
- Legacy option/state counts are zero.
- Primitive release leaves callback runtime, session, leases, provider, scheduler, and native transients at zero.
- Compatibility enable still constructs and runs `LegacyFishingAutomationService`.
- Primitive and legacy modes cannot coexist, including when owner IDs match.
- Fishing Primitives, hook runtime, and native types remain internal.
- Release/unit/script/diff checks pass; no soak is run.

## Completion Evidence

- Release solution build passed with 0 warnings and 0 errors; full unit runner printed `DTMAPI.UnitTests: OK`.
- Fifth-save Steam evidence `docs/debug/evidence/GAME-SMOKE/20260710-235813` passed `RunStatus`, `AutoFishingPhase`, `AutoFishingLifecycle`, `AutoFishingMonoGate`, and `ProcessExited`.
- The one-fish gate recorded `accessorBuilds=25`, `accessorRebuilds=0`, `accessorBuildFailures=0`, and `accessorInvocationFailures=0`.
- ReturnedToTitle recorded zero native transient, sessions, input leases, animation leases, scheduler work, native references, and callback runtime.
- No soak or 0/100/500 matrix was run; `ISSUE-010` remains open.
