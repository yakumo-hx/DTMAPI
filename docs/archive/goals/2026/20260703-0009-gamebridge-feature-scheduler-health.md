# DTMAPI Goal 20260703-0009: GameBridge Feature Scheduler And Final Health Snapshot

## Summary

Implement DTMAPI phase 7 as an internal-only GameBridge feature component scaffold. Do not perform content registry takeover. Do not change CustomAnimals, AnimalVoice, AutoFishing gameplay, public APIs, manifest fields, content-pack JSON fields, Hook targets, or fallback behavior.

This phase has three guarded slices:

- 7A: feature scheduling contracts, diagnostic only.
- 7B: low-risk `Update` bucket scheduling, feature-flagged and reversible.
- 7C: final health snapshot / dispose graph diagnostics at save/load, title return, log export, and shutdown.

## Guardrails

- `RegistryTakesOver=false` remains the required runtime state.
- `CustomAnimalAnimatorBridge`, `AudioReplacement`, `FishingAutomation`, `Camera`, and `ActionSpeed` remain `EveryFrame`.
- `SaveLoaded`, `ReturnedToTitle`, `EnvironmentReset`, Hook install, and API registration continue to use the old ordered fanout.
- No Unity asset destroy/unload, no native-owned object disposal, no third-party object disposal.
- No stable public API additions.

## Internal Feature Flags

Add these local runtime config flags to `DTMAPI/config/refactor-scaffold.json`, with environment overrides for smoke/CI:

- `GameBridgeFeatureContracts=true`
- `GameBridgeFeatureUpdateBuckets=true`
- `GameBridgeFinalHealthSnapshot=true`

If a flag is disabled, the corresponding new diagnostics or bucket scheduling must not execute. `GameBridgeFeatureUpdateBuckets=false` must restore legacy per-frame `Update` broadcast.

## Tasks

### 7A - Feature Contracts

- Add internal `GameBridgeFeatureContract`.
- Add internal `GameBridgeFeatureUpdateBucket` values: `EveryFrame`, `Every250ms`, `Every1s`, `LifecycleOnly`, `SaveOnly`, `TitleOnly`, `Disabled`.
- Add a `Contract` property to internal `IGameBridgeFeature`.
- Each existing feature explicitly declares:
  - `RequiresSave`
  - `AllowsTitleScreen`
  - `RequiresNativeScene`
  - `RequiresUi`
  - `UpdateBucket`
  - `EnvironmentResetSensitive`
  - `HasSaveLifetimeState`
  - `HasTitleLifetimeState`
  - `CanAutoPauseAfterFailure`
- Validate/format contract diagnostics only. Missing or contradictory contracts produce warnings and `Refactor.GameBridgeFeatureContracts=warning`, not behavior changes.

### 7B - Low-Risk Update Buckets

- Leave these `EveryFrame`: `CustomAnimalAnimatorBridge`, `AudioReplacement`, `FishingAutomation`, `Camera`, `ActionSpeed`.
- Move these to `Every250ms`: `SaveSlots`, `NativeUiLayoutDiagnostics`, `AnimalViewer`.
- Move these to `LifecycleOnly`: `ActionCompletion`, `ChestLocatorEnhancer`, `CropHarvesting`, `FishRoeTooltip`, `OilCoalDrop`, `StrongPlantingGun`.
- Record per-feature `updateDispatchCount`, `updateSkippedCount`, `failureCount`, `lastDispatch`, and `lastSkip`.
- Do not throttle lifecycle fanout, Hook install, API registration, or `EnvironmentReset` in this phase.

### 7C - Final Health Snapshot

Publish `GameBridgeFinalHealthSnapshot` at:

- `SaveLoaded`
- `ReturnedToTitle`
- `LogExport`
- `Shutdown`

Snapshot must include:

- feature count
- active/failed/auto-disabled count
- bucket distribution
- per-feature dispatch/skip/failure counts
- environment reset count
- native handle/UI clone/binder counters from existing summaries
- event/input owner counters from existing Core summaries
- resource ledger summary
- SaveLoad summary
- dispose graph summary: existing cleanup counters plus `NeedsRestart` for unknown/native/external owners

## Smoke Result Fields

Extend `tools/scripts/run-game-smoke.ps1` result schema v2 with:

- `GameBridgeFeatureContracts`
- `GameBridgeFeatureScheduler`
- `GameBridgeFeatureBuckets`
- `GameBridgeFeatureDispatchCounts`
- `GameBridgeFinalHealthSnapshot`
- `GameBridgeFinalHealthSummary`

## Validation

Run:

```powershell
tools/scripts/test.ps1 -Configuration Release
git diff --check
[System.Management.Automation.PSParser]::Tokenize((Get-Content -Raw tools/scripts/run-game-smoke.ps1), [ref]$null) | Out-Null
```

Short smoke gate under runtime lock:

```powershell
tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseTitleButtonLifecycle -TimeoutSeconds 320 -SkipBuild
tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -AutoExerciseHatchAnimalVoice -TimeoutSeconds 260 -SkipBuild
tools/scripts/run-game-smoke.ps1 -SaveSlot 5 -AutoExerciseAutoFishingPhase -AutoFishingSoakLoops 3 -TimeoutSeconds 420 -SkipBuild
tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -AutoOpenAnimalPanel -TimeoutSeconds 260 -SkipBuild
tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -AutoExerciseMoreSavesOfficialSaveUi -TimeoutSeconds 320 -SkipBuild
```

Stage-end gate only after short smokes pass:

```powershell
tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -TitleIdleBeforeSaveSeconds 3600 -TimeoutSeconds 3900 -SkipBuild
```

## Documentation

- Add/update an update record under `docs/updates/2026/`.
- Update `docs/updates/INDEX.md`.
- Update `docs/hook-map/README.md`.
- Update `docs/debug/regressions/smoke-matrix.md`.
- Leave `ISSUE-010` open; this phase improves final-state evidence but does not prove the long-run GC crash fully solved.
