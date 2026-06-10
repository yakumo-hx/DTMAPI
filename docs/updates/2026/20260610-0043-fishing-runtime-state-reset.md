# 20260610-0043 Fishing Runtime State Reset

## Status

Verified.

## Source Request

User requested the FishingAutomation hardening follow-up from the `85cd4af` Refactor baseline. This branch is `codex/fix-fishing-runtime-state-reset`.

## Summary

- Added `FishingAutomationService.ResetFishingRuntimeState(reason)` to clear mini-game handles, phase log cooldowns, service failure throttle episodes, smoke-only transient overrides, and recent AutoFishing feedback/cast cooldown state at lifecycle boundaries.
- Routed `FishingAutomationFeature.SaveLoaded`, `ReturnedToTitle`, and `EnvironmentReset` through the reset helper while preserving animator-speed restoration.
- Kept `IFishingAutomationApi`, `FishingAutomationOptions`, `FishingAutomationState`, hook/status IDs, hook targets, and AutoFishing smoke result schema unchanged.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0043-fishing-runtime-state-reset.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Unit coverage verifies `ResetFishingRuntimeState` clears mini-game handle state, phase log cooldown state, service failure throttle state, smoke-only AutoFishing overrides, and restores/clears saved animator speeds.
- DirectExe third-save AutoFishing smoke `GAME-SMOKE/20260610-203301` passed: `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Logs show `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, reset events for `ReturnedToTitle`, `SaveLoaded`, and `DolocAPI.SetEnvCamera`, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and animator restore `reason=AgentStateFishingPull.OnExit restored=2`.
- No `DolocTown.exe` remained and no fatal instance popup was found.
- The AutoFishing smoke did not export a fresh report zip; its `latest-report.txt` still points to `dtmapi-report-20260610-171030.zip`, so that stale report is not cited as this branch's report evidence.

## Evidence Links

- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folder: `docs/debug/evidence/GAME-SMOKE/20260610-203301`

## Rollback

- Restore `FishingAutomationFeature` lifecycle methods to call only `RestoreExperimentalAnimatorSpeeds(...)`.
- Remove `ResetFishingRuntimeState(...)` and the unit test added for lifecycle reset cleanup.

## Follow-Up

- Continue with `codex/refactor-fishing-native-helper-dependency` so FishingAutomationService no longer statically depends on `DolocTownExperimentalBridgeApi` helper methods.
