# Phase 8.10 Comparable Full No-Probe Breadcrumb Run

Date: 2026-07-05/06 +08:00
Status: runtime-evidence-captured / no-probe fatal reproduced before SaveLoaded / issue-010-open
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: ISSUE-010 full known-failing profile, continuous one-hour title idle, no PreLoad GC, default `Full` object snapshot mode.

## Summary

Phase 8.10 ran exactly one comparable full no-probe baseline after Phase 8.9 added SaveLoaded breadcrumbs.

Evidence:

- `docs/debug/evidence/GAME-SMOKE/20260705-231212`

Result:

- The profile reproduced `Fatal error in GC / Unexpected mark stack overflow`.
- The fatal happened before DTMAPI SaveLoaded breadcrumbs could run.
- SaveLoad state was `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, duplicate requests `0`.
- The final DTMAPI line was `LoadGame requested for slot/index 2. requestId=SL-0001.`
- Unity `Player.log` places the crash inside native save-load terrain/dungeon activation, matching the earlier comparable no-probe fatal `GAME-SMOKE/20260705-194759`.

Classification:

- This is the second comparable full no-probe fatal before `SaveLoaded`.
- It supports continuing native LoadGame/save activation root-set analysis.
- It does not justify running `Lite`/`Off`, because the snapshot path did not execute.
- It does not justify UI owner/pair bisection or immediate service-level hard-disable in this phase.

## Runtime Conditions

Command intent:

- `-SaveSlot 3`
- `-IncludeHookProbe`
- `-AutoExerciseSaveLoadCycle`
- `-SaveLoadCycleCount 2`
- `-SaveLoadCycleInitialTitleIdleSeconds 3600`
- `-SaveLoadCycleIntervalSeconds 5`
- `-SaveLoadCycleInSaveSeconds 5`
- `-SaveLoadObjectSnapshotMode Full`
- `-TimeoutSeconds 6000`
- `-OfficialModProfile CoreCustomAnimals`
- full extra IDs for action/utility, Manbo audio, YConsole, MoreEquipmentSlots, MoreSaves, and Zoom
- `-FatalWindowCrashDumpGraceSeconds 30`

Not used:

- `-AutoExercisePreLoadGcProbe`
- `-SkipBuild`
- `Lite` or `Off` snapshot mode
- service-level hard-disable

The runtime lock was acquired before the smoke and released in `finally`.

## Profile Validation

`official-mod-profile-summary.json` was valid for ISSUE-010 conclusions:

- `Profile=CoreCustomAnimals`
- `Applied=true`
- `ExtraEnabledIds` exactly matched the requested nine full-profile IDs.
- Enabled IDs included those nine IDs plus the expected CoreCustomAnimals local animal packs: ShellCrab, HatchAssets, OilfloaterAssets, MoleAssets, and DreckoAssets.
- `Local.Yuuka_DTMAPI_AutoFishing` was disabled.
- No unexpected enabled feature mod was present.

## Fatal Window Evidence

Final DTMAPI position:

```text
LoadGame requested for slot/index 2. requestId=SL-0001.
```

SaveLoad summary:

```text
requests=1
active=SL-0001:slot=2
nativeEnter=1
nativeReturn=0
saveLoaded=0
duplicateRequests=0
```

Breadcrumb result:

- `SaveLoaded.Step=` count in `DTMAPI-latest.log`: `0`
- No hook-level or runtime-level SaveLoaded breadcrumb ran.

Unity crash evidence:

- Fatal window process: `ProcessId=58872`, `MainWindowTitle=Fatal error in GC`.
- Unity crash source: present during live collect, and still present during post-close collect; final source was live collect.
- Latest Unity crash directory copied: `Crash_2026-07-05_161323045`.
- `process-check.txt` after cleanup: no `DolocTown.exe` process found.

`Unity-Player.log` crash stack:

```text
mono_gc_register_root
mono_object_new_specific
DolocTown.TerrainLayer:.ctor
DolocTown.Terrain:CreateLayers
DolocTown.Terrain:.ctor
DolocTown.Terrain:Create
DolocTown.Room:ResetTerrain
DolocTown.Room:AfterLoadData
DolocTown.Room:__AfterLoadData
DolocTown.Dungeon:AfterLoadData
DolocTown.DungeonManager:AfterLoadData
DolocTown.GameData.DungeonArchiveData:AfterLoadData
DolocTown.GameData.DataPersistenceManager:LoadGame(int)
DolocAPI:DMD<DolocAPI::LoadGame>(int)
DolocTown.GameDataUiState:Load(bool,int)
```

This stack matches native LoadGame/save activation before DTMAPI `NotifySaveLoaded`.

## Object Snapshot Notes

The last heavy DTMAPI snapshot before fatal was `LoadGameNativeEnter`:

- `ModOwner.records=119`
- `OwnerRoots.events.activeHandlers=28`
- `OwnerRoots.input.buttons=9`
- `GameBridge.bridgeLifecycle.features=14`
- `AudioReplacement.platformPlayers=11`
- `CustomAnimals.registrations=10`
- SaveSlots, EquipmentSlots, AnimalViewer, AudioReplacement transient requests/clips/callbacks, AutoFishing native transient handles, and CustomAnimals controller/bundle caches stayed zero or bounded at title.

The dominant deltas were long-idle dispatch counters and stable owner/root-set counts, not a new per-owner root growth after return-to-title.

## Validation

Pre-run:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; restricted-network `NU1900` warnings only.
- `git diff --check`: passed with line-ending normalization warnings only.
- No `DolocTown.exe` process was running before the smoke.

Post-run:

- Runtime lock released.
- Profile restored.
- `process-check.txt` reported no leftover `DolocTown.exe`.
- Fatal-window two-stage collection captured live and post-close Unity crash evidence.

## Decision

Phase 8.10 resolves the immediate 8.9 question:

- The comparable no-probe baseline did not move to SaveLoaded/snapshot.
- It reproduced the same pre-SaveLoaded native LoadGame/save activation window as `GAME-SMOKE/20260705-194759`.

Next step:

- Continue native/root-set analysis around `DataPersistenceManager.LoadGame`, terrain/dungeon activation, and the stable full-profile title/process root-set.
- Do not run `SaveLoadObjectSnapshotMode Lite|Off` for this window, because the fatal occurs before the title-return `SaveLoaded` object snapshot path.
- Do not return to UI owner/pair bisection.
- Do not destroy unknown native Unity objects.
- Do not do service-level hard-disable in this phase; if planned later, it must be framed as native/root-set isolation after this second comparable no-probe fatal.

## Guardrails

This phase did not:

- Change source code.
- Change public DTMAPI mod APIs.
- Change player runtime behavior.
- Run `Lite`/`Off`.
- Run PreLoad GC.
- Run UI owner/pair bisection.
- Run service-level hard-disable.
- Destroy unknown native `GameObject`, `Component`, `AudioClip`, `AssetBundle`, `RuntimeAnimatorController`, or Unity shell objects.
