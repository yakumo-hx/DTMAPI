# 20260610-0044 Fishing Native Helper Dependency

## Status

Verified.

## Source Request

User requested the FishingAutomation hardening follow-up from the `85cd4af` Refactor baseline. This branch is `codex/refactor-fishing-native-helper-dependency`.

## Summary

- Moved the FishingAutomationService reflection/native helper dependency from `DolocTownExperimentalBridgeApi` to `GameBridgeNativeHelpers`.
- Added Fishing-required helper implementations to `src/DTMAPI.GameBridge.DolocTown/Native/GameBridgeNativeHelpers.cs`: native small-message display, static bool reads, member writes, animator speed read/write, bounded multipliers/seconds, generic member assignment, and enumerable object iteration.
- Kept the `DolocTownExperimentalBridgeApi` compatibility wrappers in place for other not-yet-migrated code.
- Kept `IFishingAutomationApi`, options/state DTOs, hook/status IDs, and AutoFishing behavior unchanged.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Native/GameBridgeNativeHelpers.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0044-fishing-native-helper-dependency.md`

## Validation

- `rg -n "DolocTownExperimentalBridgeApi|using static DTMAPI.GameBridge.DolocTown.DolocTownExperimentalBridgeApi" src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs` returned no matches.
- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- DirectExe third-save AutoFishing smoke `GAME-SMOKE/20260610-204134` passed: `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Logs show `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, reset events for `ReturnedToTitle`, `SaveLoaded`, and `DolocAPI.SetEnvCamera`, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and animator restore `reason=AgentStateFishingPull.OnExit restored=2`.
- No `DolocTown.exe` remained and no fatal instance popup was found.
- The AutoFishing smoke did not export a fresh report zip; its `latest-report.txt` still points to `dtmapi-report-20260610-171030.zip`, so that stale report is not cited as this branch's report evidence.

## Evidence Links

- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folder: `docs/debug/evidence/GAME-SMOKE/20260610-204134`

## Rollback

- Change `FishingAutomationService` back to the `DolocTownExperimentalBridgeApi` static helper import.
- Remove the newly added Fishing helper methods from `GameBridgeNativeHelpers` if no other code has started consuming them.

## Follow-Up

- Continue with `codex/review-fishing-options-contract` to document the current forced `AutoRecast` and `RequireSelectedFishingRod` normalization contract without changing runtime behavior.
