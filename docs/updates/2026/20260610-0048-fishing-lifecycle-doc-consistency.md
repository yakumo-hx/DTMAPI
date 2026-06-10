# 20260610-0048 Fishing Lifecycle Doc Consistency

## Status

Verified.

## Source Request

User requested the post-`e255191` Fishing follow-up plan. This branch is `codex/refactor-fishing-lifecycle-doc-consistency`.

## Summary

- Added a post-feature-split addendum to the Fishing native responsibility review so older `DolocTownExperimentalBridgeApi` ownership text is not mistaken for current layout.
- Removed direct save/title `FishingAutomationService.RestoreExperimentalAnimatorSpeeds(...)` calls from `DolocTownHookCallbacks`.
- Kept save/title Fishing cleanup under `FishingAutomationFeature.SaveLoaded` and `ReturnedToTitle`, which call `ResetFishingRuntimeState(...)`.
- Kept `AgentStateBase.OnExit` and `AgentStateFishingPull.OnExit` direct animator restore paths unchanged.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `docs/reviews/api/2026/20260610-fishing-native-responsibility.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0048-fishing-lifecycle-doc-consistency.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- DirectExe third-save AutoFishing smoke `GAME-SMOKE/20260610-220230` passed: `RunStatus=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Logs show `Feature.FishingAutomation = ready` through `ReturnedToTitle`, `SaveLoaded`, and `EnvironmentReset`, unchanged `Fishing.Automation = experimental`, unchanged AutoFishing smoke verified statuses, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2`.
- Logs no longer contain the old save/title callback keys `SaveLoaded.RestoreExperimentalAnimatorSpeeds` or `ReturnedToTitle.RestoreExperimentalAnimatorSpeeds`.
- No `DolocTown.exe` remained and no fatal instance popup was found.
- This smoke still points `latest-report.txt` at stale `dtmapi-report-20260610-171030.zip`; the report export fix is tracked in a later branch.

## Evidence Links

- Runtime evidence folder: `docs/debug/evidence/GAME-SMOKE/20260610-220230`
- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Public API matrix: `docs/api/public-api-matrix.md`

## Rollback

- Restore the two direct save/title `RestoreExperimentalAnimatorSpeeds` callbacks in `DolocTownHookCallbacks`.
- Remove the addendum/update/matrix references for this cleanup.

## Follow-Up

- Continue with the Fishing service failure recovery episode branch.
