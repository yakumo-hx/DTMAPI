# Phase 8.9 SaveLoaded / Native LoadGame Activation Analysis

Date: 2026-07-05 +08:00
Status: source-and-existing-crash-evidence-reviewed / breadcrumb-diagnostic-added / issue-010-open
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: ISSUE-010 fatal-window ordering around native `LoadGame`, `DolocAPI.AfterLoadArchiveData`, `DtmApiRuntime.NotifySaveLoaded(bool)`, and title-return object graph snapshot publication.

## Summary

Phase 8.9 did not start a new long smoke first. It compared the two existing Phase 8.8 crash packages:

- `docs/debug/evidence/GAME-SMOKE/20260705-194759`
- `docs/debug/evidence/GAME-SMOKE/20260705-205236`

The ordering is not identical:

- `20260705-194759` is a no-PreLoad-GC full-profile fatal before `SaveLoaded`. The final DTMAPI line is the accepted `LoadGame` request, and Unity `Player.log` places the crash in native save-load terrain/dungeon activation.
- `20260705-205236` is the same full profile with `-AutoExercisePreLoadGcProbe`. The forced GC completed, native `LoadGame` reached `SaveLoaded`, public/runtime SaveLoaded handlers ran, and the fatal stack lands in DTMAPI title-return object graph snapshot publication/logging.

Current classification:

- The no-probe baseline still supports native `LoadGame` / save activation as a primary fatal window.
- The PreLoad probe sample shows the full SaveLoaded object snapshot/log path can be an immediate trigger or amplifier after native activation reaches `NotifySaveLoaded`.
- This does not prove the diagnostic snapshot is the original root cause; it proves the existing `Full` snapshot path is heavy enough to be inside the fatal window in one captured run.

## Evidence Comparison

| Evidence | Probe | Last DTMAPI position | Unity crash directory | SaveLoad state | Interpreted fatal window |
| --- | --- | --- | --- | --- | --- |
| `GAME-SMOKE/20260705-194759` | none | `LoadGame requested for slot/index 2. requestId=SL-0001.` | `Crash_2026-07-05_124847467` | `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, duplicate `0` | Native `LoadGame` save activation before DTMAPI `SaveLoaded` |
| `GAME-SMOKE/20260705-205236` | `PreLoadForcedGCProbe=Passed` | `TitleReturn object graph snapshot 17:SaveLoaded...` | `Crash_2026-07-05_135327389` | `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=1`, duplicate `0` | `NotifySaveLoaded` tail, full object snapshot publication/logging |

Both crash directories were copied from `%TEMP%/RedSawGames/DolocTown/Crashes` during live collection and contain `crash.dmp` plus `Player.log`.

Profile validation:

- Both profile summaries had `Profile=CoreCustomAnimals` and `Applied=true`.
- Both enabled the full requested extra set: action/utility Workshop IDs, Manbo audio, and YConsole/MoreEquipmentSlots/MoreSaves/Zoom Workshop IDs.
- Both kept `Local.Yuuka_DTMAPI_AutoFishing` disabled.
- `-IncludeHookProbe` was present in both, matching the known failing sample baseline from Phase 8.8.

## Stack Notes

No-probe `20260705-194759` `Unity-Player.log` stack:

- `mono_gc_register_root`
- `Dictionary<Vector2Int, TerrainSlot>.Resize`
- `DolocTown.TerrainLayer:.ctor`
- `DolocTown.Terrain:Create`
- `DolocTown.Room:ResetTerrain`
- `DolocTown.Room:AfterLoadData`
- `DolocTown.Dungeon:AfterLoadData`
- `DataPersistenceManager:LoadGame(int)`
- dynamic `DolocAPI::LoadGame(int)`

This is before DTMAPI `NotifySaveLoaded`.

PreLoad `20260705-205236` `Unity-Player.log` stack:

- `mono_gc_register_root`
- `mono_string_new_size`
- `string:Concat`
- `BepInEx.Logging.LogEventArgs:ToStringLine`
- `BepInEx.Logging.UnityLogListener:LogEvent`
- `DtmApiRuntime.PublishTitleReturnBoundaryLedgerUpdate`
- `DtmApiRuntime.CaptureTitleReturnObjectGraphSnapshot`
- `DtmApiRuntime.NotifySaveLoaded(bool)`
- `DolocTownHookCallbacks.AfterLoadArchiveDataPostfix`
- dynamic `DolocAPI::LoadGame(int)`

This places the crash after native `AfterLoadArchiveData` reached the DTMAPI SaveLoaded callback and during full snapshot log publication.

## Source Chain Review

Actual `DolocTownHookCallbacks.AfterLoadArchiveDataPostfix(bool)` order:

1. `Bridge.ExperimentalApi.NotifyEquipmentSlotsSaveLoaded(isNewGame)`
2. `Bridge.NotifyGameBridgeFeaturesSaveLoaded(isNewGame)`
3. `Runtime.NotifySaveLoaded(isNewGame)`
4. `Bridge.MarkSaveLoadedForSmoke()`

Actual `DtmApiRuntime.NotifySaveLoaded(bool)` order before this phase:

1. Set runtime phase and log `SaveLoaded hook dispatched`.
2. `SaveLoadRequestCoordinator.RecordSaveLoaded`.
3. Publish SaveLoad coordinator status and record the title-return `SaveLoaded` event.
4. `ObserveLifecycle`.
5. `CustomEntities.BeginSaveSession`.
6. `SaveSessionLoaded` internal event.
7. Public `Events.DispatchSaveLoaded`.
8. `FlushRuntimeQueues("SaveLoaded")`.
9. `CaptureTitleReturnObjectGraphSnapshot("SaveLoaded", ...)`.

The PreLoad fatal's final DTMAPI line and native stack are consistent with step 9, after steps 1-8 completed.

## Implementation Added

Added lightweight SaveLoaded activation breadcrumbs:

- Hook-level steps around EquipmentSlots, GameBridge feature dispatch, runtime `NotifySaveLoaded`, and smoke marking.
- Runtime-level steps around SaveLoad record, lifecycle observation, `SaveSessionLoaded`, public event dispatch, queue flush, and object snapshot.
- Each breadcrumb logs only:
  - `SaveLoaded.Step`
  - `elapsedMs`
  - `GC.CollectionCount(0/1/2)`
  - `GC.GetTotalMemory(false)`
  - current save-load request id
  - current title-return boundary id
  - slot and runtime phase

The breadcrumb intentionally does not enumerate objects, stringify dictionaries, run LINQ summaries, publish hook statuses, or format object deltas.

Added smoke-only `-SaveLoadObjectSnapshotMode Full|Lite|Off`:

- `Full` is the default and preserves current behavior.
- `Lite` records only tiny runtime/request/boundary sections for title-return object graph snapshots.
- `Off` skips title-return object graph snapshot capture and logs a skip line.
- The switch is written only through smoke settings and configured by `SmokeHarness`; ordinary player runtime and ordinary smoke default remain `Full`.

This phase did not run a Lite/Off long classification. Per user rule, Lite/Off should wait until a second comparable no-probe full fatal exists and the fatal window again points to SaveLoaded/snapshot.

## Changed Files

- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/SaveLoadRequestCoordinatorService.cs`
- `src/DTMAPI.Core/Runtime/TitleReturnBoundaryLedgerService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tools/scripts/run-game-smoke.ps1`

## Interpretation

Ruled out or weakened:

- A single UI owner or high-value UI pair as the next useful path; Phase 8.7 already passed those.
- Duplicate LoadGame as the observed cause in these two crash packages.
- Title-stable forced GC alone as a sufficient trigger in `20260705-205236`; the probe completed before LoadGame.

Still suspect:

- Native `LoadGame` / save activation over the full stable root-set.
- Full SaveLoaded snapshot/delta/log publication as a trigger or amplifier once native activation reaches SaveLoaded.
- Idle heap/allocation/GC-threshold pressure, because comparable full no-probe behavior remains intermittent.

Next shortest useful runtime step, if evidence is still insufficient after this code lands:

1. Run exactly one comparable full known-failing profile without PreLoad GC, `3600s` title idle, max two loads, `-TimeoutSeconds 6000`, `-FatalWindowCrashDumpGraceSeconds 30`, and `Full` snapshot mode.
2. If it fatals again before `SaveLoaded`, continue native/root-set analysis.
3. If it fatals again in SaveLoaded/snapshot, then use `Lite/Off` as a diagnostic classifier.
4. Only after two comparable no-probe full-profile fatals, consider service-level hard-disable experiments.

## Validation

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings from NuGet vulnerability metadata lookup.
- `git diff --check`: passed with line-ending normalization warnings only.
- No new runtime long smoke was launched in this phase before analyzing the existing crash packages.

## Guardrails

This phase did not:

- Run new long smoke before analyzing existing crash packages.
- Change public DTMAPI mod APIs.
- Change player runtime behavior.
- Continue UI owner/pair bisection.
- Run service-level hard-disable experiments.
- Destroy unknown native `GameObject`, `Component`, `AudioClip`, `AssetBundle`, `RuntimeAnimatorController`, or Unity shell objects.
