# 20260613-0004 AutoFishing Real Fixture Smoke Rewrite

- Date: 2026-06-13
- Status: implemented
- Branch: current dirty workspace
- Source: user requested only the approved AutoFishing smoke/test-program rewrite, with no API/runtime implementation changes outside smoke harness/test scripts.
- Version: stays `0.5.1-alpha` / `0.5.1.0`; no version bump.

## Changed Files

- `tools/scripts/run-game-smoke.ps1`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260613-0004-autofishing-real-fixture-smoke-rewrite.md`

## Summary

- `run-game-smoke.ps1` still defaults global `SaveSlot` to `3`, but `-AutoExerciseAutoFishingPhase` now blocks before install/launch unless `-SaveSlot 5` is explicitly supplied.
- AutoFishing scenario names are now `DefaultLoop`, `InstantBite`, `SkipMiniGame`, `FastAnimations`, `CombinedInstantSkip`, and `CombinedInstantComplete`.
- The AutoFishing smoke case no longer creates transient fishing pools, activates hidden pools, sets `FishingPoolOverrideForSmoke`, writes `FishingCache`, generates a smoke rod, or enters `AgentStateFishingWait` through `Overwrite<T>`.
- The smoke now observes the real fifth-save fixture loop through the normal AutoFishing hotkey path and native fishing phase hooks: `AutoCast -> Wait -> BiteReady -> Battle/Pull -> PullExit -> next AutoCast`.

## Validation

- PowerShell AST parse for `tools/scripts/run-game-smoke.ps1` passed.
- `tools/scripts/build.ps1 -Configuration Release -SkipTests` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `git diff --check -- tools/scripts/run-game-smoke.ps1 src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs` passed with line-ending warnings only.

## Game Evidence

- No game smoke was run by request.
- The new runtime gate expects future AutoFishing phase smokes to use `tools/scripts/run-game-smoke.ps1 -AutoExerciseAutoFishingPhase -SaveSlot 5 ...`.

## Rollback

- Revert this smoke-only rewrite to restore synthetic AutoFishing proof behavior. This is not recommended because the old proof path could pass using transient pools and direct state/cache setup instead of the real save fixture.
