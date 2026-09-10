# 20260703-0009 GameBridge Feature Scheduler Health

## Summary

Implemented DTMAPI phase 7 as an internal-only GameBridge feature component layer. Each `IGameBridgeFeature` now has an explicit contract, low-risk `Update` bucket diagnostics/dispatch counts, and final health snapshots at save/title/export/shutdown boundaries.

This phase does not change content registry takeover, public APIs, manifest/content-pack JSON fields, CustomAnimals, AnimalVoice, AutoFishing gameplay, Hook targets, or `RegistryTakesOver=false`.

## Source Request / Goal

- User request: enter "DTMAPI 第 7 阶段：GameBridge Feature 组件调度与最终健康快照".
- Goal record: `docs/goals/2026/20260703-0009-gamebridge-feature-scheduler-health.md`.
- Short prompt backup: `docs/goals/2026/20260703-0009-gamebridge-feature-scheduler-health.goal.txt`.

## Changed Files

- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/RefactorScaffoldOptions.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Features.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/GameBridgeFeatureContract.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/IGameBridgeFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/*/*Feature.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/goals/2026/20260703-0009-gamebridge-feature-scheduler-health.md`
- `docs/goals/2026/20260703-0009-gamebridge-feature-scheduler-health.goal.txt`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/hook-map/README.md`

## Behavior

- Added internal scaffold flags, default on and locally configurable:
  - `GameBridgeFeatureContracts`
  - `GameBridgeFeatureUpdateBuckets`
  - `GameBridgeFinalHealthSnapshot`
- Added internal `GameBridgeFeatureContract` metadata for all 14 current GameBridge features:
  - `RequiresSave`
  - `AllowsTitleScreen`
  - `RequiresNativeScene`
  - `RequiresUi`
  - `UpdateBucket`
  - `EnvironmentResetSensitive`
  - `HasSaveLifetimeState`
  - `HasTitleLifetimeState`
  - `CanAutoPauseAfterFailure`
- Kept lifecycle/API/Hook/EnvironmentReset fanout order unchanged.
- Applied low-risk `Update` buckets only:
  - `EveryFrame`: `CustomAnimalAnimatorBridge`, `AudioReplacement`, `FishingAutomation`, `Camera`, `ActionSpeed`.
  - `Every250ms`: `SaveSlots`, `NativeUiLayoutDiagnostics`, `AnimalViewer`.
  - `LifecycleOnly`: `ActionCompletion`, `ChestLocatorEnhancer`, `CropHarvesting`, `FishRoeTooltip`, `OilCoalDrop`, `StrongPlantingGun`.
- Added per-feature dispatch/skip/failure counts and bucket summaries.
- Added final health snapshot report context with feature counts, bucket distribution, update dispatch counts, environment reset count, native/UI counters, owner/event/input counters, resource ledger summary, SaveLoad summary, and dispose graph status.
- Added smoke `result.json` fields:
  - `GameBridgeFeatureContracts`
  - `GameBridgeFeatureScheduler`
  - `GameBridgeFeatureBuckets`
  - `GameBridgeFeatureDispatchCounts`
  - `GameBridgeFinalHealthSnapshot`
  - `GameBridgeFinalHealthSummary`

## Validation

- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; restore/build emitted restricted-network `NU1900` package-vulnerability feed warnings only.
- `git diff --check`: passed; Git reported line-ending normalization warnings only.
- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- Slot 3 lifecycle smoke passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseTitleButtonLifecycle -TimeoutSeconds 320 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260703-224309`
  - Key fields: `RunStatus=Passed`, `SaveLoaded=Passed`, `TitleButtonLifecycle=Passed`, all phase-seven GameBridge fields passed, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
- Slot 7 Hatch AnimalVoice smoke passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -AutoExerciseHatchAnimalVoice -TimeoutSeconds 260 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260703-224422`
  - Key fields: `RunStatus=Passed`, `SaveLoaded=Passed`, `HatchAnimalVoice=Passed`, all phase-seven GameBridge fields passed, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
- Slot 5 AutoFishing short soak passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 5 -AutoExerciseAutoFishingPhase -AutoFishingSoakLoops 3 -TimeoutSeconds 420 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260703-224524`
  - Key fields: `RunStatus=Passed`, `AutoFishingPhase=Passed`, `AutoFishingSoak=Passed`, all phase-seven GameBridge fields passed, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
- Initial slot 7 AnimalPanel UI bucket regression did not pass, then was reclassified as a fixture mismatch:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -AutoOpenAnimalPanel -TimeoutSeconds 260 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260703-224713`
  - Result: `RunStatus=Failed`, `AnimalViewerUi=Failed`.
  - Important non-regression evidence: `GameBridgeFeatureContracts=Passed`, `GameBridgeFeatureScheduler=Passed`, `GameBridgeFeatureBuckets=Passed`, `GameBridgeFeatureDispatchCounts=Passed`, `GameBridgeFinalHealthSnapshot=Passed`, `Feature.AnimalViewer=ready`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, no Fatal GC, and final health had `failedFeatures=0`, `cleanupFailures=0`, `needsRestart=0`, `resourceErrors=0`.
  - Follow-up manual context from the user clarified that slot 7 is the custom-animal barn and does not contain the hidden-product animal expected by this smoke evidence; the hidden-product horned alpaca fixture is in slot 4. This makes the slot 7 failure a smoke fixture selection problem, not a GameBridge scheduler or AnimalViewer regression.
- Slot 4 AnimalPanel UI bucket regression passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 4 -AutoOpenAnimalPanel -TimeoutSeconds 260 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260703-231541`
  - Key fields: `RunStatus=Passed`, `SaveLoaded=Passed`, `AnimalViewerUi=Passed`, all phase-seven GameBridge fields passed, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
- Slot 3 SaveSlots bucket regression passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -AutoExerciseMoreSavesOfficialSaveUi -TimeoutSeconds 320 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260703-231700`
  - Key fields: `RunStatus=Passed`, `SaveLoaded=Passed`, `MoreSavesOfficialSaveUi=Passed`, all phase-seven GameBridge fields passed, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
- Stage-end long-title gate passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -TitleIdleBeforeSaveSeconds 3600 -TimeoutSeconds 3900 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260703-231929`
  - Key fields: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `LongTitleIdleBeforeSave=Passed`, `SaveLoadRequestCoordinator=Passed`, `SaveLoadBoundary=Passed`, `DuplicateLoadRequests=Passed`, `LifecycleBoundaryContract=Passed`, `ResourceLifecycleLedger=Passed`, `ResourceLifecycleCleanup=Passed`, `TitleIdleResourceGrowth=Passed`, `HookScheduler=Passed`, `CoreHookReadiness=Passed`, `FeatureHookReadiness=Passed`, `HookStatusQueue=Passed`, all phase-seven GameBridge fields passed, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
  - The current-run log showed exactly one active load request, `LoadGame requested for slot/index 2. requestId=SL-0001`, after the 3600-second title idle. `process-check.txt` reported no leftover `DolocTown.exe`, and `fatal-window-check.txt` reported no fatal popup.
- A follow-up attribution rerun attempted to use `DTMAPI_REFACTOR_GAMEBRIDGE_FEATURE_UPDATE_BUCKETS=false`, but the game was launched through Steam and the runtime log still reported `GameBridgeFeatureUpdateBuckets=true`. This shows the environment override is not reliable through an already-running Steam client; local `DTMAPI/config/refactor-scaffold.json` remains the reliable rollback route for Steam-launched smoke.

## Rollback Notes

- Set these flags in `DTMAPI/config/refactor-scaffold.json` to disable the phase-seven layers:
  - `GameBridgeFeatureContracts=false`
  - `GameBridgeFeatureUpdateBuckets=false`
  - `GameBridgeFinalHealthSnapshot=false`
- Disabling `GameBridgeFeatureUpdateBuckets` restores the legacy per-frame `Update` fanout while leaving lifecycle/API/Hook fanout unchanged.
- This phase does not remove old fallback systems or destroy/unload Unity/native/external objects.

## Follow-Up

- Keep the AnimalViewer UI bucket regression on slot 4 when hidden-product evidence is required; slot 7 is the custom-animal fixture and should not be used for that specific hidden-product row check.
- For Steam-launched rollback smoke, prefer writing the local runtime config flag instead of relying on environment variable propagation.
- ISSUE-010 remains open. Phase 7 improves final-state evidence and passed one stage-end 3600-second title-idle gate, but one local long-title pass is still a mitigation signal rather than proof that all independent long-run player crash risk is closed.
