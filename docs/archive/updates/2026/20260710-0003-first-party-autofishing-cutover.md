# 20260710-0003 - First-Party AutoFishing Cutover

Status: source-unit-and-core-runtime-scenarios-verified / extended-runtime-gates-pending / issue-010-open

## Source Request

Third phase of the requested split: first-party AutoFishing owns the complete player product loop while `IFishingAutomationApi` becomes a compatibility adapter.

## Changed Files

- `first-party-mods/AutoFishingMod/ModEntry.cs`
- `first-party-mods/AutoFishingMod/FishingDecisionEngine.cs`
- `first-party-mods/AutoFishingMod/README.md`
- `src/DTMAPI.GameBridge.DolocTown/Compatibility/FishingAutomation/FishingCompatibilityController.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingPrimitivesService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`
- `tools/scripts/run-game-smoke.ps1`
- first-party build/install/release wiring
- API, hook, debug, smoke, goal, and update records

## Summary

- AutoFishingMod now owns F6/`None`, enable/disable, 0.25-second recast, native movement cancel, sequence de-duplication, InstantBite, SkipMiniGame, visible minigame input decisions, and animation leases.
- Native movement uses `agent.Status.HorizontalMoveFactor` first at threshold `0.001`; the old key snapshot is a logged-once fallback only.
- FastAnimations applies velocity `×m`, gravity `×m²`, Pull duration `÷m`, and Pull animator `×m`, with full restoration and no Ready acceleration.
- Skip uses the native no-minigame result transaction and never acquires a synthetic-input lease. The three switches remain independent.
- `CastChargeRatio` is absent from product config but retained by the compatibility controller for existing clients.
- Compatibility API activation acquires the same single-owner primitives boundary and reports busy rather than preempting another owner.

## Validation

- Full Release build/unit tests passed with no warnings or errors and `DTMAPI.UnitTests: OK`.
- Fifth-save `GAME-SMOKE/20260710-102651` passed DefaultLoop and a three-loop short soak.
- `GAME-SMOKE/20260710-103205` passed independent FastAnimations with `instantBite=False`, velocity `×3`, gravity `×9`, Pull speed/duration scaling, and cleanup zero.
- `GAME-SMOKE/20260710-103350` passed independent InstantBite.
- `GAME-SMOKE/20260710-105201` passed independent SkipMiniGame with native skip reels, zero provider calls/input leases, next recast, report export, close cleanup, and no leftover process.
- Retained `20260710-104754` proves the same Skip behavior but failed only because an older deployed GameBridge DLL did not publish the new smoke result field; the full rebuild/deploy fixed the gate.
- Fresh custom-key/`None`, physical native movement cancel, and 100/500-loop current-build runs were not completed. ISSUE-010 therefore remains open.

## Evidence Links

- Goal: `docs/goals/2026/20260710-0003-first-party-autofishing-cutover.md`
- AutoFishing README: `first-party-mods/AutoFishingMod/README.md`
- Runtime evidence: `docs/debug/evidence/GAME-SMOKE/20260710-102651`, `103205`, `103350`, `105201`
- Regression row: `AUTOFISHING-PRIMITIVES-CUTOVER-20260710`

## Rollback

Set first-party execution back to compatibility mode and leave primitives dormant. Do not remove the stable compatibility entry point or unpatch the shared Harmony owner.

## Follow-Up

- Run current-build F7/`None` and physical native-movement cancellation.
- Run the requested 10/100/500-loop memory/GC matrix before considering ISSUE-010 closure.
- Defer physical Harmony ID splitting and option-specific install/uninstall.
