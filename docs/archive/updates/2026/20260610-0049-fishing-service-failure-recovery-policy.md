# 20260610-0049 Fishing Service Failure Recovery Policy

## Status

Verified.

## Source Request

User requested the post-`e255191` Fishing follow-up plan. This branch is `codex/fix-fishing-service-failure-recovery-policy`.

## Summary

- Added internal success recovery for high-frequency `FishingAutomationService` failure episodes.
- `FishingAutomation.AutoCast`, `FishingAutomation.Wait.OnPlay`, and `FishingAutomation.MiniGame.Update` now record operation success on their successful intervention paths.
- A repeated failure episode clears after three stable successes, so a later failure starts a fresh diagnostics episode.
- Public API members, hook targets, hook/status IDs, smoke schema, and normal AutoFishing behavior are unchanged.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0049-fishing-service-failure-recovery-policy.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Unit coverage simulates six repeated `FishingAutomation.MiniGame.Update` failures, three stable successes, and a later failure; diagnostics record one full error for the first episode and a second full error after recovery, with fresh `failureCount=1` hook details on the post-recovery failure.
- DirectExe third-save AutoFishing smoke `GAME-SMOKE/20260610-220835` passed: `RunStatus=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Passing logs show unchanged `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, AutoFishing smoke verified statuses, and no `Fishing automation service failed`, `Repeated FishingAutomation`, or `Throttled FishingAutomation` entries.
- No `DolocTown.exe` remained and no fatal instance popup was found.
- This smoke still points `latest-report.txt` at stale `dtmapi-report-20260610-171030.zip`; the report export fix is tracked in the next branch.

## Evidence Links

- Runtime evidence folder: `docs/debug/evidence/GAME-SMOKE/20260610-220835`
- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Public API matrix: `docs/api/public-api-matrix.md`

## Rollback

- Remove `RecordFishingAutomationSuccess(...)`, the success calls, and the unit test.
- Keep the earlier first-failure/short/summary throttling behavior from `20260610-0042`.

## Follow-Up

- Continue with the AutoFishing smoke fresh report export branch.
