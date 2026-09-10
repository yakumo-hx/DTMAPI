# 20260710-0008 AutoFishing Hook Runtime Seam

## Status

- Source, unit, parser, and one-fish Unity Mono gate verified.
- No soak and no 0/100/500 performance matrix were run.
- `ISSUE-010` remains open.

## Source Request

Implement `docs/goals/2026/20260710-0007-autofishing-hook-runtime-seam.md`: first prove one real visible-minigame fish under Mono, split accessor lifecycle counters, make primitive release unconditional, route static Fishing callbacks through an internal interface, move the first-party Hook path into its own runtime, and isolate the old implementation as compatibility-only.

## Changes

- Added internal `IFishingHookRuntime`. `FishingAutomationHookBridge`, `DolocTownGameBridge`, and static fishing callbacks now depend on that seam instead of a concrete service.
- Added `FishingPrimitiveHookRuntime` for first-party phase/Wait routing, Ready charge and animation lease behavior, minigame status/input override, Cast/Pull animation behavior, native exit cleanup, and primitive telemetry.
- Changed primitive activation so it creates only `FishingPrimitiveHookRuntime`; `FishingAutomationFeature.Service` remains null and legacy option/state maps remain empty. Only compatibility `IFishingAutomationApi.SetEnabled(true)` creates the old executor.
- Made primitive and legacy runtimes mutually exclusive even for the same owner ID. Final disable/release detaches the callback runtime and removes the concrete runtime root.
- Renamed and moved the old executor to `Compatibility/FishingAutomation/LegacyFishingAutomationService.cs` without attempting a risky mechanical decomposition.
- Preserved the typed `UseFishRod` delegate across environment invalidation and split accessor telemetry into Builds, Rebuilds, BuildFailures, and InvocationFailures for native state, minigame, native transactions, Ready access, and `UseFishRod`.
- Wrapped `FishingRuntimeSession.Release()` cleanup in `try/finally`; a throwing `Transitioned` subscriber is logged but cannot retain the session, provider, leases, scheduler, Hook runtime, or native references.
- Added the `-AutoFishingMonoGate` smoke route. It forces InstantBite=true, SkipMiniGame=false, Fast x4, charge 0, catches exactly one visible-minigame fish, disables the product, returns to title, and checks accessor and transient cleanup fields.
- Corrected two gate-discovered native assumptions: visible minigame entry now uses the scoped native use-tool edge instead of treating `Wait.NextState()` as a state-manager transition, and the optional nonexistent `FishingNoteType.Avoid` enum value no longer fails accessor construction.
- Updated the Mono smoke verifier so primitive mode does not require the legacy-only `Fishing.Automation.Lifecycle` publication.

## Main Files

- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/IFishingHookRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingPrimitiveHookRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingPrimitivesService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingNativeAdapter.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingNativeStateCache.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingMiniGameNativeCache.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingNativeTransactionCache.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAnimationController.cs`
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation/LegacyFishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`

## Validation

- `DTMAPI.sln` Release build: passed, 0 warnings and 0 errors.
- Full `DTMAPI.UnitTests`: passed with `DTMAPI.UnitTests: OK`.
- Unit coverage includes primitive-only construction, callback runtime selection, same-owner mode exclusion, throwing-release-subscriber cleanup, cross-environment `UseFishRod` delegate preservation, pure snapshot allocation, and split accessor failure classes.
- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `git diff --check`: passed; line-ending normalization warnings only.
- Post-seam fifth-save Steam gate: `docs/debug/evidence/GAME-SMOKE/20260710-235813`, `RunStatus=Passed`, `AutoFishingPhase=Passed`, `AutoFishingLifecycle=Passed`, `AutoFishingMonoGate=Passed`, `ProcessExited=Passed`.
- Gate evidence: `functionalFish=1`, `accessorBuilds=25`, `accessorRebuilds=0`, `accessorBuildFailures=0`, `accessorInvocationFailures=0`, `nativeTransient=0`, `sessions=0`, `inputLeases=0`, `animationLeases=0`, `schedulerPending=False`, `nativeReferences=False`, `callbackRuntime=False` after title return.
- No `DolocTown.exe` remained and the shared runtime lock was released.

## Retained Diagnostic Evidence

- `20260710-232054` exposed that calling `Wait.NextState()` did not enter the state manager's Battle path.
- `20260710-232653` completed the fish but exposed the optional enum-value accessor build failure.
- Pre-seam `20260710-233452` then verified the functional one-fish/accessor/title-cleanup gate; its outer run failed only because the new Mono route had not yet emitted the common AutoFishing loop marker.
- `20260710-235339` passed the primitive Mono gate but the outer script still expected the legacy lifecycle status; the verifier was corrected before the final passing run.

## Rollback

Rollback the interface/runtime seam, restore the legacy service at its old location/name, and restore concrete callback routing as one unit. Do not selectively keep concurrent primitive and legacy activation or reintroduce direct `NextState()` visible-reel routing.

## Follow-up

- Keep Fishing Primitives and `IFishingHookRuntime` internal.
- Keep the legacy service isolated; decide later whether to deprecate it or split it mechanically.
- Run the separately defined 0/100/500 allocation matrix only in a future runtime round. This single-fish gate is functional Mono proof, not long-run performance proof.
