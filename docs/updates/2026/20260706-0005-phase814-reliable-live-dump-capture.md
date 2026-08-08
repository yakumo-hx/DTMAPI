# 20260706-0005 - Phase 8.14 Reliable Live Dump Capture

Date: 2026-07-06 +08:00
Status: source-and-runtime-evidence-captured / live-dump-captured / issue-010-open
Area: smoke/saveload/issue-010/crash-capture/no-hookprobe-lite/native-loadgame

## Trigger

After Phase 8.13 reproduced the no-HookProbe Lite fatal class but failed to obtain a live dump or fresh Unity crash directory, the external review requested reliable fatal-window dump capture before service/root isolation.

## Summary

Implemented smoke-only live process dump hardening:

- Expanded `-FatalWindowProcessDumpMode` to `None|ComSvcsFull|DbgHelpFull|Both`, default `None`.
- Added an explicit ComSvcs command path through `%WINDIR%\System32\rundll32.exe`.
- Added a DbgHelp `MiniDumpWriteDump` full-memory fallback.
- Used `%TEMP%\DTMAPI-Dumps` as the short temporary dump path.
- Wrote process dump command/stdout/stderr/error/summary files.

Then reran the full known-failing profile with:

- `CoreCustomAnimals`
- action/utility Workshop IDs
- `Local.Yuuka_DTMAPI_ManboCardboardAudio`
- YConsole, MoreEquipmentSlots, MoreSaves, Zoom
- AutoFishing disabled
- no `-IncludeHookProbe`
- no `-AutoExercisePreLoadGcProbe`
- `-SaveLoadObjectSnapshotMode Lite`
- `3600s` continuous title idle
- max two LoadGame attempts
- `-TimeoutSeconds 6000`
- `-FatalWindowCrashDumpGraceSeconds 30`
- `-FatalWindowProcessDumpMode Both`
- `-FatalWindowPostCloseCrashDumpWaitSeconds 60`

Valid evidence `docs/debug/evidence/GAME-SMOKE/20260706-113849` reproduced Fatal GC before SaveLoaded and captured a DbgHelp full live dump.

## Changed Files

- `tools/scripts/run-game-smoke.ps1`
- `docs/reviews/code/2026/20260706-0004-phase814-reliable-live-dump-capture.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260706-0005-phase814-reliable-live-dump-capture.md`
- `docs/updates/INDEX.md`

No public API files, GameBridge service implementations, runtime owner cleanup, or Unity object destruction behavior changed.

## Evidence

Valid run:

- `docs/debug/evidence/GAME-SMOKE/20260706-113849`
  - `RunStatus=Aborted`
  - `HookProbe=Skipped`
  - `PreLoadForcedGCProbe=Skipped`
  - `SaveLoadObjectSnapshotMode=Lite`
  - `SaveLoaded=Failed`
  - `SaveLoadCycle=Failed`
  - `NoFatalInstanceWindow=Failed`
  - `ProcessExited=Passed`
  - `ForcedClose=Passed`
  - `OfficialModProfileRestored=True`

SaveLoad state:

- `requests=1`
- `nativeEnter=1`
- `nativeReturn=0`
- `saveLoaded=0`
- duplicate requests `0`
- last request state `NativeEnter:SL-0001:slot=2:phase=Update:source=Harmony LoadGame Prefix`

Final DTMAPI line:

```text
LoadGame requested for slot/index 2. requestId=SL-0001.
```

Fatal popup:

- detected at `2026-07-06T12:39:02.3908174+08:00`
- live process `ProcessId=52784`
- `MainWindowTitle=Fatal error in GC`
- fatal check text included `Unexpected mark stack overflow`

Crash and dump capture:

- `CrashBaseline=Passed`
- `UnityCrashFreshness=stale-only`
- `FatalWindowProcessDump=Captured:DbgHelpFull`
- DbgHelp dump size: `4,883,437,290` bytes
- DbgHelp dump SHA256: `8F3DC0BD8E5C1CE1DED66EC67F9B3BBFBC889137F0972FBD4032DABC8C417B5F`

The runtime evidence reports `ComSvcsFull.Status=Error` because the smoke helper rejected null stdout/stderr text for an empty-output ComSvcs attempt. DbgHelp still captured a valid dump. The null-output handling bug was patched immediately after the run.

## Validation

Pre-run:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; restricted-network `NU1900` warnings only.
- `git diff --check`: passed with line-ending normalization warnings only.
- No `DolocTown.exe` was running before the smoke.

Post-run:

- Runtime lock was released.
- `official-mod-profile-summary.json` was exact for the requested profile and AutoFishing was disabled.
- Profile was restored.
- `process-check.txt` reported no leftover `DolocTown.exe`.
- DbgHelp full dump was captured.
- Unity crash directory collection remained stale-only.

Post-run source check:

- PowerShell parser check passed again after the ComSvcs null-output patch.

## Decision

The valid 8.14 sample returned to the pre-SaveLoaded fatal window while HookProbe was absent and Lite snapshots were active. This means HookProbe and Full snapshot/delta/logging are not necessary for this specific native LoadGame fatal recurrence.

The next phase may plan native/root-set or service-level isolation using the live dump. Do not return to UI owner/pair bisection, do not run PreLoad GC for this classification, and do not destroy unknown native Unity objects.

## Rollback

The source changes are smoke-only diagnostics in `tools/scripts/run-game-smoke.ps1`. To roll back, remove the `DbgHelpFull|Both` dump modes and the associated process dump files. Player runtime behavior is unaffected.

## Follow-Up

- Preserve `GAME-SMOKE/20260706-113849` as the valid Phase 8.14 no-HookProbe Lite reliable live dump sample.
- Exclude the 4.88 GB dump from routine evidence zips; include `TOO-LARGE-DUMP-README.txt` instead.
- Use the captured dump and DTMAPI boundary ledger to decide the first targeted native/root-set or service-isolation cut.
