# 20260610-0042 Fishing Service Failure Throttle

## Status

Verified.

## Source Request

User requested the FishingAutomation hardening follow-up from the `85cd4af` Refactor baseline. This branch is `codex/fix-fishing-service-failure-throttle`.

## Summary

- Added service-level operation throttling inside `FishingAutomationService` for `FishingAutomation.AutoCast`, `FishingAutomation.Wait.OnPlay`, and `FishingAutomation.MiniGame.Update`.
- Repeated high-frequency internal failures now record the first full diagnostics error, two short warning publications, and later 30-second summaries instead of writing diagnostics and failed hook statuses every frame.
- Kept `IFishingAutomationApi`, `FishingAutomationOptions`, `FishingAutomationState`, hook targets, hook/status IDs, and AutoFishing smoke result schema unchanged.
- Fixed the `20260610-0040` update record changed-files list so it no longer names a non-existent `DolocTownExperimentalBridgeApi.FishingAutomation.cs` file.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0040-fishingautomation-feature-split.md`
- `docs/updates/2026/20260610-0042-fishing-service-failure-throttle.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Unit coverage verifies repeated `FishingAutomation.MiniGame.Update` service failures record one diagnostics error and stop repeated failed hook-status publication at `failureCount=3`.
- DirectExe third-save AutoFishing smoke `GAME-SMOKE/20260610-202047` passed: `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Logs show `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2`, and no `FishingAutomation service failed` / `Repeated FishingAutomation` / `Throttled FishingAutomation` entries on the passing path.
- The AutoFishing smoke did not export a fresh report zip; its `latest-report.txt` still points to `dtmapi-report-20260610-171030.zip`, so that stale report is not cited as this branch's report evidence.

## Evidence Links

- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folder: `docs/debug/evidence/GAME-SMOKE/20260610-202047`

## Rollback

- Remove the `FishingAutomationService` service-level failure throttle state/helper.
- Restore the internal catch blocks to direct diagnostics/hook-status writes.
- Remove the unit test added for FishingAutomation service failure throttling.

## Follow-Up

- Continue with `codex/fix-fishing-runtime-state-reset` to clear FishingAutomation runtime state at save/title/environment boundaries.
