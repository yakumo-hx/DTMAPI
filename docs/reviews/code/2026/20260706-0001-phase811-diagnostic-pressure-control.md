# Phase 8.11 Diagnostic Pressure Control Before Service-Disable

Date: 2026-07-06 +08:00
Status: runtime-evidence-captured / snapshot-lite shifted fatal window / issue-010-open
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: ISSUE-010 full known-failing profile, continuous one-hour title idle, no PreLoad GC, `Lite` object snapshot mode.

## Summary

Phase 8.11 ran the requested diagnostic-pressure control before any service-level hard-disable:

- full known-failing profile unchanged
- `-IncludeHookProbe`
- no `-AutoExercisePreLoadGcProbe`
- `-SaveLoadObjectSnapshotMode Lite`
- `3600s` continuous title idle
- max two LoadGame attempts
- `-TimeoutSeconds 6000`
- `-FatalWindowCrashDumpGraceSeconds 30`

Evidence:

- `docs/debug/evidence/GAME-SMOKE/20260706-005212`

Result:

- The profile reproduced `Fatal error in GC / Unexpected mark stack overflow`.
- The fatal did not match the two comparable no-probe `Full` pre-SaveLoaded windows from `GAME-SMOKE/20260705-194759` and `GAME-SMOKE/20260705-231212`.
- Lite reached `SaveLoaded`, completed every new SaveLoaded breadcrumb through `Hook.Exit`, and then the Unity crash stack landed in `TextureUtils.DrawArea` / `MapManager.Init` under `DolocAPI.AfterLoadArchiveData` while the original `LoadGame` call still had not returned.
- Per the 8.11 decision rules, this is a shifted fatal window. Stop Lite/Off classification here and analyze the shifted LoadGame-after-SaveLoaded/native-return window before any service hard-disable.

## Runtime Conditions

Command intent:

- `-SaveSlot 3`
- `-IncludeHookProbe`
- `-AutoExerciseSaveLoadCycle`
- `-SaveLoadCycleCount 2`
- `-SaveLoadCycleInitialTitleIdleSeconds 3600`
- `-SaveLoadCycleIntervalSeconds 5`
- `-SaveLoadCycleInSaveSeconds 5`
- `-SaveLoadObjectSnapshotMode Lite`
- `-TimeoutSeconds 6000`
- `-OfficialModProfile CoreCustomAnimals`
- full extra IDs for action/utility, Manbo audio, YConsole, MoreEquipmentSlots, MoreSaves, and Zoom
- `-FatalWindowCrashDumpGraceSeconds 30`

Not used:

- `-AutoExercisePreLoadGcProbe`
- `Off`
- service-level hard-disable
- UI owner/pair bisection

The runtime lock was acquired before the smoke and released in `finally`.

## Profile Validation

`official-mod-profile-summary.json` was valid for ISSUE-010 conclusions:

- `Profile=CoreCustomAnimals`
- `Applied=true`
- `ExtraEnabledIds` exactly matched the requested nine full-profile IDs.
- Enabled IDs included those nine IDs plus the expected CoreCustomAnimals local animal packs: ShellCrab, HatchAssets, OilfloaterAssets, MoleAssets, and DreckoAssets.
- `Local.Yuuka_DTMAPI_AutoFishing` was disabled.
- No unexpected enabled feature mod was present.
- `OfficialModProfileRestored=True` in `summary.txt` and `result.json`.

## Lite Mode Evidence

Lite was verified in both command/result evidence and runtime DTMAPI evidence:

- `summary.txt`: `SaveLoadObjectSnapshotMode=Lite`
- `result.json`: `"SaveLoadObjectSnapshotMode": "Lite"`
- `DTMAPI-latest.log`: `SaveLoad object snapshot mode configured mode=Lite source=smoke-settings.json smokeOnly=true.`
- `BeforeNextLoadGame` snapshot:

```text
TitleReturn object graph snapshot 9:BeforeNextLoadGame:TR-0001:sections=3...
saveLoad={mode=Lite, requestId=none}
Runtime={snapshotMode=Lite; inputContext=ModChangeListUiState; phase=Update; currentLoadingSlot=2}
```

- `LoadGameNativeEnter` snapshot:

```text
TitleReturn object graph snapshot 11:LoadGameNativeEnter:TR-0001:sections=3...
saveLoad={mode=Lite, requestId=SL-0001}
Runtime={snapshotMode=Lite; inputContext=ModChangeListUiState; phase=Update; currentLoadingSlot=2}
```

The Lite deltas only retained tiny runtime/request/boundary sections:

```text
9:BeforeNextLoadGame ... metrics=4 ... ownerPrevDelta={none}; ownerSameBoundaryDelta={none}
11:LoadGameNativeEnter ... metrics=2 ... ownerPrevDelta={none}; ownerSameBoundaryDelta={none}
13:SaveLoaded ... metrics=2 ... ownerPrevDelta={none}; ownerSameBoundaryDelta={none}
```

## Fatal Window Evidence

Run status:

- `RunStatus=Aborted`
- `SaveLoaded=Passed`
- `SaveLoadCycle=Failed`
- `NoFatalInstanceWindow=Failed`
- `ProcessExited=Passed`
- `HookProbe=Passed`
- `PreLoadForcedGCProbe=Skipped`

SaveLoad summary:

```text
requests=1
active=none
duplicateRequests=0
nativeEnter=1
nativeReturn=0
saveLoaded=1
fatalWindows=0
last=SaveLoaded:SL-0001:slot=2:phase=SaveLoaded:source=DolocAPI.AfterLoadArchiveData
```

The native `LoadGame` postfix still had not returned, but DTMAPI's SaveLoaded callback chain had completed.

Final DTMAPI position before the fatal window:

```text
SaveLoaded.Step=Hook.Exit elapsedMs=637 gc0=306 gc1=306 gc2=306 totalMemory=783945728 requestId=SL-0001 boundaryId=TR-0001 slot=2 phase=LogExport.
```

Breadcrumb order reached:

```text
Hook.Enter
Hook.BeforeEquipmentSlots
Hook.AfterEquipmentSlots
Hook.BeforeGameBridgeFeatureDispatch
Hook.AfterGameBridgeFeatureDispatch
Hook.BeforeRuntimeNotifySaveLoaded
Runtime.Enter
Runtime.BeforeSaveLoadRecord
Runtime.AfterSaveLoadRecord
Runtime.BeforeLifecycleObservation
Runtime.AfterLifecycleObservation
Runtime.BeforeSaveSessionLoaded
Runtime.AfterSaveSessionLoaded
Runtime.BeforeRuntimeEventDispatch
Runtime.AfterRuntimeEventDispatch
Runtime.BeforeQueueFlush
Runtime.AfterQueueFlush
Runtime.BeforeObjectSnapshot
Runtime.AfterObjectSnapshot
Runtime.Exit
Hook.AfterRuntimeNotifySaveLoaded
Hook.BeforeMarkSmoke
Hook.AfterMarkSmoke
Hook.Exit
```

Fatal window collection:

- Live fatal process: `ProcessId=59144`, `MainWindowTitle=Fatal error in GC`.
- `FatalWindowCrashDumpGraceSeconds=30`.
- Crash evidence was present during live collect and again post-close; final source was `live-collect`.
- Latest Unity crash directory copied: `Crash_2026-07-05_175308638`.

Unity `Player.log` crash stack:

```text
mono_gc_register_root
mono_array_new_specific
DolocTown.TextureUtils:DrawArea
DolocTown.TextureUtils:FillColor
DolocTown.TextureUtils:CreateTexture
DolocTown.MapManager:Init(bool)
UnityEngine.Events.UnityEvent`1<bool>:Invoke(bool)
DolocAPI:AfterLoadArchiveData(bool)
DolocAPI:DMD<DolocAPI::LoadGame>(int)
DolocTown.GameDataUiState:Load(bool,int)
```

This is not the earlier `TerrainLayer` / `Room.ResetTerrain` / `Dungeon.AfterLoadData` / `DataPersistenceManager.LoadGame` pre-SaveLoaded stack. It is also not a crash inside DTMAPI full object snapshot publication: the Lite SaveLoaded object snapshot and `Hook.Exit` breadcrumb both completed before the fatal popup was observed.

## Interpretation

This run does not support claiming that `Lite` fixed or proved anything by passing; it did not pass.

This run also does not support the narrower Phase 8.10 statement that `Lite` is irrelevant to the no-probe baseline. With Lite, the fatal window moved from pre-SaveLoaded terrain/dungeon activation to post-SaveLoaded `AfterLoadArchiveData` / `MapManager.Init` texture allocation while the native `LoadGame` call was still active.

Current classification:

- The full stable profile remains under high GC/root-set pressure after continuous one-hour title idle.
- Reducing DTMAPI object-snapshot/delta/logging pressure to Lite changed the timing/window enough for native LoadGame to pass the earlier pre-SaveLoaded terrain/dungeon activation point.
- The process still hit Mono mark-stack overflow before native `LoadGame` returned.
- Therefore Phase 8.11 should stop here and analyze the shifted window before any `Off` run or service-level hard-disable.

## Validation

Pre-run:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; restricted-network `NU1900` warnings only.
- `git diff --check`: passed with line-ending normalization warnings only.
- No `DolocTown.exe` process was running before the smoke.

Post-run:

- Runtime lock released.
- Profile restored.
- No leftover `DolocTown.exe` process found after collection/cleanup.
- Fatal-window two-stage collection captured live and post-close Unity crash evidence.

## Decision

Do not continue immediately to:

- a second Lite rerun, because the first Lite did not pass and instead shifted the fatal window;
- `Off`, because the next classification question is no longer "does Lite pass twice?";
- service-level hard-disable, because the shifted window needs its own analysis first;
- UI owner/pair bisection;
- PreLoad GC;
- unknown native object destruction.

Next investigation should compare the three now-distinct no/diagnostic-pressure windows:

- `Full` no-probe pre-SaveLoaded terrain/dungeon activation: `GAME-SMOKE/20260705-194759`, `GAME-SMOKE/20260705-231212`.
- `PreLoad GC + Full` SaveLoaded full snapshot/logging trigger/amplifier: `GAME-SMOKE/20260705-205236`.
- `Lite` post-SaveLoaded native LoadGame continuation in `AfterLoadArchiveData` / `MapManager.Init` texture allocation: `GAME-SMOKE/20260706-005212`.

The next cut should be framed as native LoadGame activation/root-set timing analysis, with HookProbe/no-HookProbe and diagnostic-pressure controls considered before broad service hard-disable.

## Guardrails

This phase did not:

- Change source code.
- Change public DTMAPI mod APIs.
- Change player runtime behavior.
- Run `Off`.
- Run PreLoad GC.
- Run UI owner/pair bisection.
- Run service-level hard-disable.
- Destroy unknown native `GameObject`, `Component`, `AudioClip`, `AssetBundle`, `RuntimeAnimatorController`, or Unity shell objects.
