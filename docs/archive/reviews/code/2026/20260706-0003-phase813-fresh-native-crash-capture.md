# Phase 8.13 Fresh Native Crash Capture For No-HookProbe Lite Baseline

Date: 2026-07-06 +08:00
Status: runtime-evidence-captured / shifted SaveLoaded window / fresh native crash still missing / issue-010-open
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: ISSUE-010 full known-failing profile, continuous one-hour title idle, no HookProbe, no PreLoad GC, `Lite` object snapshot mode, fatal-window crash freshness and live process dump capture.

## Summary

Phase 8.13 implemented smoke-only crash-capture hardening before another no-HookProbe Lite baseline run:

- crash-directory baseline at smoke start
- Unity crash evidence freshness classification: `fresh`, `stale-only`, or `missing`
- optional fatal-window live process dump mode: `-FatalWindowProcessDumpMode None|ComSvcsFull`
- configurable post-close crash-collection wait: `-FatalWindowPostCloseCrashDumpWaitSeconds`

The valid runtime evidence is:

- `docs/debug/evidence/GAME-SMOKE/20260706-100234`

Result:

- The profile was valid and profile state was restored.
- HookProbe was skipped and `SaveLoadObjectSnapshotMode=Lite` was active.
- The run did not reproduce the Phase 8.12 pre-SaveLoaded window. It shifted to a post-SaveLoaded, pre-native-return fatal:
  - `requests=1`
  - `nativeEnter=1`
  - `nativeReturn=0`
  - `saveLoaded=1`
  - `duplicateRequests=0`
  - final DTMAPI line: `SaveLoaded.Step=Hook.Exit ...`
- The fatal popup appeared about 124 ms after `Hook.Exit`.
- Unity crash collection still found only stale crash directories.
- `ComSvcsFull` live dump capture attempted but did not create a dump.

Per the Phase 8.13 decision rule, do not proceed directly to service-level hard-disable from this sample. The window shifted and needs analysis first.

## Runtime Conditions

Command intent:

- `-SaveSlot 3`
- no `-IncludeHookProbe`
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
- `-FatalWindowProcessDumpMode ComSvcsFull`
- `-FatalWindowPostCloseCrashDumpWaitSeconds 60`

Not used:

- `-IncludeHookProbe`
- `-AutoExercisePreLoadGcProbe`
- `Off`
- service-level hard-disable
- UI owner/pair bisection
- unknown native Unity object destruction

The runtime lock was acquired for each runtime operation and released afterward.

## Implementation Changes

Smoke-only changes were made in `tools/scripts/run-game-smoke.ps1`:

- Added `-FatalWindowProcessDumpMode None|ComSvcsFull`, default `None`.
- Added `-FatalWindowPostCloseCrashDumpWaitSeconds`, default `20`.
- Wrote `crash-baseline.txt` at smoke start with current Unity crash roots, directories, file lists, and latest preexisting crash id/time.
- Wrote `fatal-window-crash-freshness.txt` after live and post-close crash collection.
- Added `UnityCrashFreshness`, `UnityCrashFreshnessSummary`, `FatalWindowProcessDumpMode`, `FatalWindowProcessDump`, `FatalWindowPostCloseCrashDumpWaitSeconds`, and `CrashBaseline` fields to `result.json`.
- Added best-effort `ComSvcsFull` dump output under `Process-Dumps/`.

The changes do not affect ordinary smoke defaults, player runtime behavior, public APIs, GameBridge services, owner cleanup, or Unity object lifetime. `Full` remains the default object snapshot behavior unless a smoke command explicitly passes `Lite` or `Off`.

Two instrumentation-invalid attempts occurred before the valid sample:

- `GAME-SMOKE/20260706-075600`: fatal popup reproduced, but `Invoke-SmokeFatalProcessDump` used PowerShell's read-only `$PID` variable name and aborted before the scripted collection completed. A manual cleanup/dump attempt was made; do not use this run for ISSUE-010 conclusions.
- `GAME-SMOKE/20260706-085929`: fatal popup reproduced and live collection started, but the crash freshness helper treated a null nullable incorrectly and aborted before post-close completion. It was cleaned up manually; do not use this run for ISSUE-010 conclusions.

Both smoke-only bugs were fixed before `GAME-SMOKE/20260706-100234`.

## Profile Validation

`official-mod-profile-summary.json` for `GAME-SMOKE/20260706-100234` was valid:

- `Profile=CoreCustomAnimals`
- `Applied=true`
- `ExtraEnabledIds` exactly matched the requested nine full-profile IDs.
- Enabled IDs included those nine IDs plus expected CoreCustomAnimals local animal packs: ShellCrab, HatchAssets, OilfloaterAssets, MoleAssets, and DreckoAssets.
- `Local.Yuuka_DTMAPI_AutoFishing` was disabled.
- No unexpected enabled feature mod was present.
- `OfficialModProfileRestored=True` in `summary.txt` and `result.json`.

## Lite And HookProbe Evidence

HookProbe was absent:

- `summary.txt`: `IncludeHookProbe=False`
- `result.json`: `HookProbe=Skipped`
- DTMAPI owner summaries did not include `DTMAPI.HookProbeMod`.

Lite was verified beyond the command line:

- `summary.txt`: `SaveLoadObjectSnapshotMode=Lite`
- `result.json`: `"SaveLoadObjectSnapshotMode": "Lite"`
- `DTMAPI-latest.log`: `SaveLoad object snapshot mode configured mode=Lite source=smoke-settings.json smokeOnly=true.`
- `BeforeNextLoadGame` snapshot logged `saveLoad={mode=Lite...}` and `Runtime={snapshotMode=Lite...}`.
- `LoadGameNativeEnter` snapshot logged `saveLoad={mode=Lite, requestId=SL-0001}` and `Runtime={snapshotMode=Lite...}`.
- `SaveLoaded` snapshot also logged `saveLoad={mode=Lite, requestId=SL-0001}`.

Final owner deltas stayed empty in the Lite snapshots:

```text
ownerPrevDelta={none}; ownerSameBoundaryDelta={none}; ownerNonZero={none}
```

## Fatal Window Evidence

Run status:

- `RunStatus=Aborted`
- `HookProbe=Skipped`
- `PreLoadForcedGCProbe=Skipped`
- `SaveLoadObjectSnapshotMode=Lite`
- `SaveLoaded=Passed`
- `SaveLoadCycle=Failed`
- `NoFatalInstanceWindow=Failed`
- `ProcessExited=Passed`
- `ForcedClose=Passed`

SaveLoad summary:

```text
requests=1
active=none
duplicateRequests=0
nativeEnter=1
nativeReturn=0
saveLoaded=1
last=SaveLoaded:SL-0001:slot=2:phase=SaveLoaded:source=DolocAPI.AfterLoadArchiveData
```

Final DTMAPI position:

```text
SaveLoaded.Step=Hook.Exit elapsedMs=51 gc0=306 gc1=306 gc2=306 totalMemory=766808064 requestId=SL-0001 boundaryId=TR-0001 slot=2 phase=SaveLoaded.
```

Fatal popup:

- `FatalWindowDetectedAt=2026-07-06T11:02:48.0231517+08:00`
- `ProcessId=31624`
- `MainWindowTitle=Fatal error in GC`
- live check text included `Unexpected mark stack overflow`.

The fatal appeared after DTMAPI completed the SaveLoaded hook breadcrumb chain but before the native LoadGame postfix recorded a native return. This places the window after `DolocAPI.AfterLoadArchiveData` / DTMAPI SaveLoaded callback completion, but still inside the native LoadGame call.

## SaveLoaded Breadcrumb Order

The relevant final order in `DTMAPI-latest.log`:

```text
SaveLoaded.Step=Hook.Enter
SaveLoaded.Step=Hook.BeforeGameBridgeFeatureDispatch
SaveLoaded.Step=Hook.AfterGameBridgeFeatureDispatch
SaveLoaded.Step=Hook.BeforeRuntimeNotifySaveLoaded
SaveLoaded.Step=Runtime.Enter
SaveLoaded.Step=Runtime.BeforeSaveLoadRecord
SaveLoaded.Step=Runtime.AfterSaveLoadRecord
SaveLoaded.Step=Runtime.BeforeLifecycleObservation
SaveLoaded.Step=Runtime.AfterLifecycleObservation
SaveLoaded.Step=Runtime.BeforeSaveSessionLoaded
SaveLoaded.Step=Runtime.AfterSaveSessionLoaded
SaveLoaded.Step=Runtime.BeforeRuntimeEventDispatch
SaveLoaded.Step=Runtime.AfterQueueFlush
SaveLoaded.Step=Runtime.BeforeObjectSnapshot
TitleReturn object graph snapshot 13:SaveLoaded ... saveLoad={mode=Lite, requestId=SL-0001}
SaveLoad cycle object delta 13:SaveLoaded ...
SaveLoaded.Step=Runtime.AfterObjectSnapshot
SaveLoaded.Step=Runtime.Exit
SaveLoaded.Step=Hook.AfterRuntimeNotifySaveLoaded
SaveLoaded.Step=Hook.BeforeMarkSmoke
Smoke save/load cycle observed SaveLoaded for cycle 1/2.
SaveLoaded.Step=Hook.AfterMarkSmoke
SaveLoaded.Step=Hook.Exit
```

The last DTMAPI log write was `Hook.Exit` at `11:02:47.899`. The fatal popup was detected at `11:02:48.023`.

## Crash Capture Result

Crash baseline was written:

- `crash-baseline.txt`
- `CrashBaseline=Passed` in `result.json`

Unity crash freshness:

- `UnityCrashFreshness=stale-only`
- `fatal-window-crash-freshness.txt`: stale-only
- `Unity-Crashes/summary.txt` copied six older directories.
- Newest copied directory was `Crash_2026-07-05_175308638`, last written at `2026-07-06T01:53:14.7809740+08:00`.
- The valid 8.13 fatal was detected at `2026-07-06T11:02:48.0231517+08:00`.
- No fresh `Crash_2026-07-06_1102...` directory, fresh `Player.log`, or fresh `crash.dmp` was created.

Live process dump:

- `FatalWindowProcessDumpMode=ComSvcsFull`
- `FatalWindowProcessDump=MissingDump`
- `Process-Dumps/process-dump-summary.txt`:
  - `ProcessId=31624`
  - `MainWindowTitle=Fatal error in GC`
  - `ExitCode=-2147024773`
  - `Status=MissingDump`
- `process-dump-stderr.txt` and `process-dump-stdout.txt` were empty.

This means 8.13 improved evidence classification, but it did not yet obtain a fresh native crash stack or live dump.

## Interpretation

This valid sample does not support moving directly to service-level hard-disable:

- It is not the same pre-SaveLoaded window as `GAME-SMOKE/20260706-025334`.
- It is not a fresh-stack reproduction of the terrain/dungeon `DataPersistenceManager.LoadGame` window.
- It shows no-HookProbe + Lite can complete SaveLoaded and still fatal before native LoadGame returns.
- The immediate final DTMAPI position is after DTMAPI SaveLoaded work and smoke marking, not inside the Lite object snapshot itself.

Current classification:

- HookProbe is not required for the overall long-idle fatal class, based on 8.12 and this run.
- Full object snapshot/delta/logging is not required for the overall fatal class, because both 8.12 and 8.13 ran Lite.
- The exact immediate window is still unstable: 8.12 landed before SaveLoaded, 8.13 landed after SaveLoaded/Hook.Exit but before native return.
- Fresh native crash capture remains insufficient. The next phase should either harden dump capture further or use a different live dump mechanism before service/root isolation conclusions rely on native-stack evidence.

## Validation

Pre-runtime:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; restricted-network `NU1900` warnings only.
- `git diff --check`: passed with line-ending normalization warnings only.
- No `DolocTown.exe` was running before the valid 8.13 smoke.

Post-runtime:

- Runtime lock released.
- Profile restored.
- `process-check.txt` reported no leftover `DolocTown.exe`.
- Fatal popup/process/log-position evidence was captured.
- Fresh Unity crash stack/dump was missing for this specific 11:02 fatal.
- `ComSvcsFull` live process dump did not produce a dump file.

## Decision

Do not continue immediately to:

- `Off`
- PreLoad GC
- UI owner/pair bisection
- service-level hard-disable
- unknown native object destruction

The next phase should analyze this shifted post-SaveLoaded/pre-native-return window and fix or replace the failed live dump capture if native-stack evidence is required before service/root isolation.
