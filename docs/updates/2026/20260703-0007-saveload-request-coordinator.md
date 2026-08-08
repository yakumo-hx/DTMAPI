# 20260703-0007 SaveLoad Request Coordinator

## Summary

Implemented DTMAPI phase 5 as an internal-only SaveLoad request coordinator and boundary diagnostic layer. The change focuses on the long-title-idle failure boundary where a previous 3600-second title idle showed four `LoadGame requested for slot/index 2` lines before `Fatal error in GC / Unexpected mark stack overflow`.

This phase does not rewrite content registry, does not change CustomAnimals, AnimalVoice, AutoFishing, public APIs, manifest/content-pack JSON semantics, or `RegistryTakesOver=false`.

## Source Request / Goal

- User request: enter "DTMAPI 第 5 阶段：SaveLoad 请求协调与进档边界收口".
- Goal record: `docs/goals/2026/20260703-0007-saveload-request-coordinator.md`.
- Previous narrowing evidence: stage-four long-idle evidence `docs/debug/evidence/GAME-SMOKE/20260702-210050` reproduced Fatal GC after title idle and before `SaveLoaded`, while Hook/Event/resource/AutoFishing diagnostics did not show title-idle growth.

## Changed Files

- `src/DTMAPI.Core/Runtime/SaveLoadRequestCoordinatorService.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/RefactorScaffoldOptions.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/goals/2026/20260703-0007-saveload-request-coordinator.md`
- `docs/goals/2026/20260703-0007-saveload-request-coordinator.goal.txt`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/hook-map/README.md`

## Behavior

- Added internal `SaveLoadRequestCoordinatorService` with request ids such as `SL-0001`.
- Records each observed load boundary with:
  - slot/index,
  - owner/source,
  - thread id,
  - runtime phase,
  - timestamp,
  - native `LoadGame` enter/return,
  - `SaveLoaded` dispatch,
  - timeout/fatal-window diagnostics when reported.
- Added local scaffold flag `SaveLoadRequestCoordinator`, default `true`, with config/env override support.
- `DtmApiRuntime.NotifyLoadGameRequested` now records native enter and publishes internal diagnostics instead of only storing `currentLoadingSlot`.
- Added a diagnostic `LoadGame` postfix so native return can close the same request id. Because native `LoadGame` can return after `AfterLoadArchiveData`/`SaveLoaded`, the ledger attaches delayed native return to the most recent same-slot request even when `SaveLoaded` already closed it.
- Smoke direct `LoadGame` fallbacks now call `ShouldSuppressDtmapiLoadGameRequest` first. Only DTMAPI/smoke-originated duplicate requests for the same slot while a load is active are suppressed. Native player UI clicks are not intercepted.
- Removed smoke-only fake `NotifyLoadGameRequested` calls from selection/restore paths that did not actually invoke native `LoadGame`.
- Added smoke `result.json` fields:
  - `SaveLoadRequestCoordinator`
  - `SaveLoadRequestSummary`
  - `DuplicateLoadRequests`
  - `SaveLoadBoundary`

## Native Boundary Notes

Reference metadata for build `23465763_workshop_38581E` confirms the reviewed boundary is `DolocAPI.LoadGame(int index) -> bool` / `DolocTown.GameData.DataPersistenceManager.LoadGame(int index) -> bool`, and `DolocAPI.LoadGame` calls `DolocAPI.AfterLoadArchiveData(bool)`. This phase records prefix/postfix timing only; it does not copy or depend on decompiled method bodies.

## Validation

- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`.
- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `git diff --check`: passed; Git reported line-ending normalization warnings only.
- Slot 3 short lifecycle smoke passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseTitleButtonLifecycle -TimeoutSeconds 320 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260703-201031`
  - Key fields: `RunStatus=Passed`, `SaveLoaded=Passed`, `TitleButtonLifecycle=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, `SaveLoadBoundary=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
  - Summary: `requests=1; active=none; duplicateRequests=0; suppressedDuplicates=0; nativeEnter=1; nativeReturn=1; saveLoaded=1`.
- Slot 7 Hatch AnimalVoice smoke passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -AutoExerciseHatchAnimalVoice -TimeoutSeconds 260 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260703-201129`
  - Key fields: `RunStatus=Passed`, `SaveLoaded=Passed`, `HatchAnimalVoice=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, `SaveLoadBoundary=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
  - Summary: `requests=1; active=none; duplicateRequests=0; suppressedDuplicates=0; nativeEnter=1; nativeReturn=1; saveLoaded=1`.
- Stage-end long title-idle gate passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -TitleIdleBeforeSaveSeconds 3600 -TimeoutSeconds 3900 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260703-201302`
  - Key fields: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `LongTitleIdleBeforeSave=Passed`, `TitleIdleResourceGrowth=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, `SaveLoadBoundary=Passed`, `HookScheduler=Passed`, `CoreHookReadiness=Passed`, `FeatureHookReadiness=Passed`, `RetryTimerAlive=Passed`, `AssemblyLoadSubscription=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
  - SaveLoad summary: `requests=1; active=none; duplicateRequests=0; suppressedDuplicates=0; nativeEnter=1; nativeReturn=1; saveLoaded=1; timeouts=0; fatalWindows=0`.
  - Log evidence: one `LoadGame requested for slot/index 2. requestId=SL-0001` at 2026-07-03 21:13:10, `SaveLoaded` closed the same request at 21:13:12, and delayed native return attached to `SL-0001` at 21:13:12.
  - No `Fatal error in GC` or `Unexpected mark stack overflow` was found in the current-run top-level DTMAPI/BepInEx/Unity logs.

## Retained Intermediate Finding

The first post-implementation short smokes passed but exposed a diagnostic modeling bug: native `LoadGame` postfix can return after `SaveLoaded`, so the initial ledger created a second `native-returned` request. The follow-up fix attaches delayed native return to the completed same-slot request and adds unit coverage. Final short and long evidence above use the corrected one-request closure.

## Rollback Notes

- Set `SaveLoadRequestCoordinator=false` in `DTMAPI/config/refactor-scaffold.json` or the matching environment override to disable the new ledger and suppression layer.
- The old `currentLoadingSlot` behavior remains as fallback.
- The smoke field additions are diagnostics-only and do not affect ordinary mod APIs.

## Follow-Up

- Do not mark ISSUE-010 fully solved from one local long-title pass. This is strong evidence that the repeated DTMAPI/smoke/native load-request boundary was a real contributor or at least a necessary guardrail, but player long-run crash packages should still be classified through fresh logs if they recur.
- Keep the next release-blocker long-idle gate focused: `SaveLoadRequestSummary` must show one request, no unsuppressed duplicates, `SaveLoadBoundary=Passed`, and no Fatal GC.
- If Fatal GC recurs with `duplicateRequests=0`, use the coordinator's last phase/request id to choose the next single-axis investigation instead of reopening a broad resource/Hook/AutoFishing matrix.
