# Phase 8.14 Reliable Live Dump Capture And No-HookProbe Lite Baseline Repeat

Date: 2026-07-06 +08:00
Status: runtime-evidence-captured / live-dump-captured / issue-010-open
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: ISSUE-010 full known-failing profile, continuous one-hour title idle, no HookProbe, no PreLoad GC, `Lite` object snapshot mode, reliable fatal-window live dump capture.

## Summary

Phase 8.14 did not start service/root isolation. It first fixed the smoke-only live dump capture path requested after Phase 8.13, then repeated the same no-HookProbe Lite full known-failing profile.

Valid runtime evidence:

- `docs/debug/evidence/GAME-SMOKE/20260706-113849`

Result:

- Profile validation passed and the profile was restored.
- HookProbe was skipped.
- PreLoad GC was skipped.
- `SaveLoadObjectSnapshotMode=Lite` was active.
- The run reproduced Fatal GC before SaveLoaded:
  - `requests=1`
  - `nativeEnter=1`
  - `nativeReturn=0`
  - `saveLoaded=0`
  - `duplicateRequests=0`
- The final DTMAPI line was `LoadGame requested for slot/index 2. requestId=SL-0001.`
- The fatal popup appeared about 1.58 seconds later.
- Unity crash directory collection remained `stale-only`.
- The new `DbgHelpFull` path captured a full live dump:
  - dump size: `4,883,437,290` bytes
  - SHA256: `8F3DC0BD8E5C1CE1DED66EC67F9B3BBFBC889137F0972FBD4032DABC8C417B5F`

Classification: HookProbe and Full object snapshot/delta/logging are not necessary for this specific pre-SaveLoaded native `LoadGame` fatal recurrence. With a live dump now captured, the next phase may plan native root-set/service isolation, but no service hard-disable was run in this phase.

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
- `-FatalWindowProcessDumpMode Both`
- `-FatalWindowPostCloseCrashDumpWaitSeconds 60`

Not used:

- `-IncludeHookProbe`
- `-AutoExercisePreLoadGcProbe`
- `Off`
- service-level hard-disable
- UI owner/pair bisection
- unknown native Unity object destruction

The shared runtime lock was acquired for the runtime operation and released afterward.

## Implementation Changes

Smoke-only changes were made in `tools/scripts/run-game-smoke.ps1`:

- Expanded `-FatalWindowProcessDumpMode` to `None|ComSvcsFull|DbgHelpFull|Both`, default `None`.
- Added a two-path process dump helper:
  - `ComSvcsFull` invokes explicit `%WINDIR%\System32\rundll32.exe %WINDIR%\System32\comsvcs.dll, MiniDump <pid> <tempDumpPath> full`.
  - `DbgHelpFull` calls `MiniDumpWriteDump` with `MiniDumpWithFullMemory`.
  - `Both` tries ComSvcs first, then DbgHelp if no dump was captured.
- Dump capture uses the short temp directory `%TEMP%\DTMAPI-Dumps` and copies successful dumps back to `Process-Dumps/`.
- Dump diagnostics now write `process-dump-summary.txt`, command/stdout/stderr/error files, and a missing-dump README when applicable.

The 8.14 runtime sample hit a smoke-only empty stdout/stderr handling bug in the ComSvcs branch, so ComSvcs was reported as `Error` in that evidence. DbgHelp still captured a valid full dump. After the run, `Add-DumpText` was patched to accept null text so future ComSvcs attempts can report empty output without aborting that branch.

These changes do not affect ordinary smoke defaults, player runtime behavior, public APIs, GameBridge services, owner cleanup, or Unity object lifetime. `Full` remains the default object snapshot behavior unless a smoke command explicitly passes `Lite` or `Off`.

## Profile Validation

`official-mod-profile-summary.json` for `GAME-SMOKE/20260706-113849` was valid:

- `Profile=CoreCustomAnimals`
- `Applied=true`
- `ExtraEnabledIds` exactly matched the requested nine full-profile IDs.
- Enabled IDs included those nine IDs plus expected CoreCustomAnimals local animal packs: ShellCrab, HatchAssets, OilfloaterAssets, MoleAssets, and DreckoAssets.
- `Local.Yuuka_DTMAPI_AutoFishing` was disabled.
- No unexpected enabled feature mod was present in the smoke profile model.
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
- `SaveLoaded=Failed`
- `SaveLoadCycle=Failed`
- `NoFatalInstanceWindow=Failed`
- `ProcessExited=Passed`
- `ForcedClose=Passed`

SaveLoad summary:

```text
requests=1
active=SL-0001:slot=2
duplicateRequests=0
nativeEnter=1
nativeReturn=0
saveLoaded=0
last=NativeEnter:SL-0001:slot=2:phase=Update:source=Harmony LoadGame Prefix
```

Final DTMAPI position:

```text
2026-07-06 12:39:00.806 +08:00 [Info] [DTMAPI] LoadGame requested for slot/index 2. requestId=SL-0001.
```

No `SaveLoaded.Step` breadcrumb was written for this run.

Fatal popup:

- `FatalWindowDetectedAt=2026-07-06T12:39:02.3908174+08:00`
- `ProcessId=52784`
- `MainWindowTitle=Fatal error in GC`
- live check text included `Unexpected mark stack overflow`

This places the fatal after DTMAPI's `LoadGameNativeEnter` ledger/snapshot and request log, but before `DolocAPI.AfterLoadArchiveData`, DTMAPI SaveLoaded hooks, or native LoadGame return.

## Crash And Dump Capture

Crash baseline and freshness:

- `CrashBaseline=Passed`
- `UnityCrashFreshness=stale-only`
- `Unity-Crashes/summary.txt` copied six older directories.
- Newest copied directory was `Crash_2026-07-05_175308638`, older than this smoke run.
- No fresh Unity crash directory was created for the 12:39 fatal.

Live process dump:

- `FatalWindowProcessDumpMode=Both`
- `FatalWindowProcessDump=Captured:DbgHelpFull`
- `Process-Dumps/process-dump-summary.txt`:
  - `ProcessId=52784`
  - `MainWindowTitle=Fatal error in GC`
  - `ComSvcsFull.Status=Error`
  - `DbgHelpFull.Status=Captured`
  - `DbgHelpFull.DumpSize=4883437290`
  - `CapturedMode=DbgHelpFull`
- `Process-Dumps/TOO-LARGE-DUMP-README.txt` records the dump path, size, and SHA256 for review packages that intentionally exclude the 4.88 GB `.dmp`.

The ComSvcs error in this specific evidence was caused by the smoke harness rejecting null stdout/stderr text, not by the game fatal itself. That smoke-only null handling bug was patched after the run. The DbgHelp dump is the valid live dump evidence for Phase 8.14.

## Interpretation

Phase 8.14 strengthens the Phase 8.12 conclusion:

- HookProbe is not necessary for the pre-SaveLoaded fatal recurrence.
- Full title-return object snapshot/delta/logging is not necessary for this specific pre-SaveLoaded native LoadGame fatal, because the sample used `Lite` and the final Lite snapshots had no owner deltas.
- The fatal still occurs before SaveLoaded, so SaveLoaded-only Lite/Off classification is not the next useful step.
- Fresh Unity crash directories are unreliable for this fatal class on this machine, but DbgHelp live dump capture now provides a native/root-set artifact.

Next investigation should use this live dump and the existing DTMAPI boundary ledger to plan native LoadGame root-set or service-level isolation. It should not return to UI owner/pair bisection.

## Validation

Pre-runtime:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; restricted-network `NU1900` warnings only.
- `git diff --check`: passed with line-ending normalization warnings only.
- No `DolocTown.exe` was running before the smoke.

Post-runtime:

- Runtime lock released.
- Profile restored.
- `process-check.txt` reported no leftover `DolocTown.exe`.
- Fatal popup/process/log-position evidence was captured.
- DbgHelp full live dump was captured.
- Unity crash collection remained stale-only.

Post-run source check:

- PowerShell parser check was rerun after the ComSvcs null-output patch and passed.

## Decision

Do not continue immediately to:

- `Off`
- PreLoad GC
- UI owner/pair bisection
- blind service-level hard-disable without using the new dump evidence
- unknown native object destruction

The next phase can now plan native/root-set or service-level isolation using the live DbgHelp dump and existing boundary evidence. Service hard-disable should remain staged and targeted, not a broad UI-owner rerun.
