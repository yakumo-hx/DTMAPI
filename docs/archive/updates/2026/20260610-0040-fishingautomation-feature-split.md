# 20260610-0040 FishingAutomation Feature Split

## Status

Verified.

## Source Request

User requested the e904d04 post-review stabilization route, third branch `codex/refactor-fishingautomation-feature-mechanical-split`, to mechanically move FishingAutomation API, state, hook ownership, and runtime update dispatch into the GameBridge feature-host route without rewriting Fishing native responsibility.

## Summary

- Added `FishingAutomationFeature`, `FishingAutomationService`, and `FishingAutomationHookBridge`.
- Moved `IFishingAutomationApi` registration from `DolocTownExperimentalBridgeApi` to the feature/service route.
- Moved FishingAutomation state, options, smoke-only flags, auto-cast, wait-phase InstantBite, minigame completion, phase observation, and fast animator restore ownership into `FishingAutomationService`.
- Moved `Fishing.Automation` hook installation/status publishing into `FishingAutomationHookBridge` while preserving the same Harmony targets, priorities, status key, and details text.
- Updated Fishing callbacks and AutoFishing smoke helpers to call `FishingAutomationService` directly.
- Added unit coverage proving `DolocTownExperimentalBridgeApi` no longer implements `IFishingAutomationApi` and `FishingAutomationFeature` owns an `IFishingAutomationApi` service.
- Did not change `IFishingAutomationApi` public contract, DTO/config semantics, hook/status IDs, smoke result schema, Fishing native state-machine behavior, or ActionSpeed restore behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- FishingAutomation implementation migrated out of the old experimental-bridge partial route and into `FishingAutomationService.cs`; no separate `DolocTownExperimentalBridgeApi.FishingAutomation.cs` file is present in this final source snapshot.
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/AutoFishingSmoke.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0040-fishingautomation-feature-split.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Unit coverage verifies:
  - `DolocTownExperimentalBridgeApi` no longer implements `IFishingAutomationApi`;
  - `FishingAutomationService` implements `IFishingAutomationApi`;
  - `FishingAutomationFeature` publishes id `FishingAutomation` and owns an `IFishingAutomationApi` service.
- DirectExe third-save AutoFishing smoke `GAME-SMOKE/20260610-170839` passed: `AutoFishingHotkey=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- AutoFishing logs show `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2`.
- DirectExe third-save ActionSpeed regression smoke `GAME-SMOKE/20260610-170950` passed: `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Process checks for both smokes report no `DolocTown.exe`; fatal-window checks report no fatal instance popup.
- The AutoFishing smoke did not export a fresh report zip; its `latest-report.txt` still points to `dtmapi-report-20260610-165123.zip`, so that stale report is not cited as this branch's report evidence. The ActionSpeed regression smoke points to `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-171030.zip`.

## Evidence Links

- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folders:
  - `docs/debug/evidence/GAME-SMOKE/20260610-170839`
  - `docs/debug/evidence/GAME-SMOKE/20260610-170950`
- Runtime report zip:
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-171030.zip`

## Rollback

- Restore `IFishingAutomationApi` implementation and FishingAutomation state/method ownership to `DolocTownExperimentalBridgeApi`.
- Move Fishing hook installation/status publishing back into `DolocTownGameBridge.InstallHarmonyHooks`.
- Point Fishing callbacks and AutoFishing smoke helpers back to `ExperimentalApi`.
- Remove `FishingAutomationFeature`, `FishingAutomationHookBridge`, `FishingAutomationService`, and the feature-ownership unit coverage.

## Follow-Up

- Continue with final Refactor audit package refresh after this branch is merged back to `Refactor`.
