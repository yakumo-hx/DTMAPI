# 20260610-0046 AutoFishing Smoke Case File

## Status

Verified.

## Source Request

User requested the FishingAutomation hardening follow-up from the `85cd4af` Refactor baseline. This branch is `codex/refactor-smoke-autofishing-case`.

## Summary

- Moved `src/DTMAPI.GameBridge.DolocTown/Smoke/AutoFishingSmoke.cs` to `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`.
- Kept the `DolocTownGameBridge` partial methods, scheduler call sites, result fields, hook/status IDs, log key text, and smoke pass/fail conditions unchanged.
- Did not change runtime behavior, public APIs, FishingAutomation service logic, migrated AutoFishing mod behavior, or package layout.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0046-autofishing-smoke-case-file.md`

## Validation

- `git diff --cached --check` passed.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- DirectExe third-save AutoFishing smoke `GAME-SMOKE/20260610-205153` passed: `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Logs show unchanged `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, reset events for `ReturnedToTitle`, `SaveLoaded`, and `DolocAPI.SetEnvCamera`, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and animator restore `reason=AgentStateFishingPull.OnExit restored=2`.
- No `DolocTown.exe` remained and no fatal instance popup was found.
- The AutoFishing smoke did not export a fresh report zip; its `latest-report.txt` still points to `dtmapi-report-20260610-171030.zip`, so that stale report is not cited as this branch's report evidence.

## Evidence Links

- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folder: `docs/debug/evidence/GAME-SMOKE/20260610-205153`

## Rollback

- Move `Smoke/Cases/AutoFishingSmokeCase.cs` back to `Smoke/AutoFishingSmoke.cs`.
- Remove the smoke-matrix/hook-map/API/update references for this mechanical file move.

## Follow-Up

- Complete final `Refactor` validation, refresh full/web audit packages, and tag `refactor-fishing-hardening-followup-20260610`.
