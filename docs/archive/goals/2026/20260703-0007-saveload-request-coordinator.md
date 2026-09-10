# 20260703-0007 SaveLoad Request Coordinator

## Goal

Implement DTMAPI phase 5: SaveLoad request coordination and save-load boundary diagnostics. This phase focuses on the long-title-idle failure boundary where a 3600-second title idle followed by slot 3 load showed repeated `LoadGame requested for slot/index 2` lines before `Fatal error in GC / Unexpected mark stack overflow`.

## Guardrails

- Do not rewrite content registry.
- Do not change CustomAnimals, AnimalVoice, AutoFishing gameplay, content-pack JSON semantics, or public APIs.
- Keep `RegistryTakesOver=false`.
- Do not intercept native player UI clicks.
- Only deduplicate or suppress duplicate DTMAPI/smoke-originated `LoadGame` requests for the same slot/index while a load is already active.
- Keep old behavior as fallback through local internal feature flag support.

## Implementation Scope

- Add an internal-only SaveLoad request ledger/coordinator.
- Record request id, slot/index, owner/source, thread id, runtime phase, timestamp, native `LoadGame` enter/return, SaveLoaded dispatch, timeout/fatal-window diagnostics where observable.
- Add smoke result fields:
  - `SaveLoadRequestCoordinator`
  - `SaveLoadRequestSummary`
  - `DuplicateLoadRequests`
  - `SaveLoadBoundary`
- Fix smoke fallback paths so DTMAPI/smoke does not call direct `DolocAPI.LoadGame` repeatedly for the same slot/index while the original load is active.

## Validation Plan

- Source checks:
  - `tools/scripts/test.ps1 -Configuration Release`
  - `git diff --check`
  - PowerShell parser check for changed smoke scripts.
- Short smoke gate:
  - slot 3: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseTitleButtonLifecycle -TimeoutSeconds 320 -SkipBuild`
  - slot 7: `tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -AutoExerciseHatchAnimalVoice -TimeoutSeconds 260 -SkipBuild`
- Stage-end long gate:
  - `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -TitleIdleBeforeSaveSeconds 3600 -TimeoutSeconds 3900 -SkipBuild`

## Expected Evidence

- Normal save load has one active native load request.
- DTMAPI/smoke direct fallback is suppressed if the same slot/index is already loading.
- SaveLoaded closes a request id.
- If Fatal GC still appears, smoke result and logs identify the last SaveLoad phase and whether duplicate requests existed.
