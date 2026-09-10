# 20260706-0004 - Phase 8.13 Fresh Native Crash Capture

Date: 2026-07-06 +08:00
Status: source-and-runtime-evidence-captured / issue-010-open
Area: smoke/saveload/issue-010/crash-capture/no-hookprobe-lite/native-loadgame

## Trigger

After Phase 8.12 reproduced the no-HookProbe Lite fatal before SaveLoaded but did not produce a fresh Unity crash directory or stack, the external review requested one more no-HookProbe Lite baseline with stronger fatal-window crash capture before service/root isolation.

## Summary

Implemented smoke-only fatal-window crash capture hardening:

- `crash-baseline.txt` at smoke start.
- Unity crash freshness classification as `fresh`, `stale-only`, or `missing`.
- `-FatalWindowProcessDumpMode None|ComSvcsFull`, default `None`.
- `-FatalWindowPostCloseCrashDumpWaitSeconds`, default `20`.
- Result fields for crash baseline, process dump status, post-close wait, and Unity crash freshness.

Ran the full known-failing profile with:

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
- `-FatalWindowProcessDumpMode ComSvcsFull`
- `-FatalWindowPostCloseCrashDumpWaitSeconds 60`

Valid evidence `docs/debug/evidence/GAME-SMOKE/20260706-100234` reproduced Fatal GC after SaveLoaded completed and before native LoadGame returned.

## Changed Files

- `tools/scripts/run-game-smoke.ps1`
- `docs/reviews/code/2026/20260706-0003-phase813-fresh-native-crash-capture.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260706-0004-phase813-fresh-native-crash-capture.md`
- `docs/updates/INDEX.md`

No public API files, GameBridge service implementations, runtime owner cleanup, or Unity object destruction behavior changed.

## Evidence

Valid run:

- `docs/debug/evidence/GAME-SMOKE/20260706-100234`
  - `RunStatus=Aborted`
  - `HookProbe=Skipped`
  - `PreLoadForcedGCProbe=Skipped`
  - `SaveLoadObjectSnapshotMode=Lite`
  - `SaveLoaded=Passed`
  - `SaveLoadCycle=Failed`
  - `NoFatalInstanceWindow=Failed`
  - `ProcessExited=Passed`
  - `ForcedClose=Passed`
  - `OfficialModProfileRestored=True`

SaveLoad state:

- `requests=1`
- `nativeEnter=1`
- `nativeReturn=0`
- `saveLoaded=1`
- duplicate requests `0`
- last request state `SaveLoaded:SL-0001:slot=2:phase=SaveLoaded:source=DolocAPI.AfterLoadArchiveData`

Final DTMAPI line:

```text
SaveLoaded.Step=Hook.Exit elapsedMs=51 gc0=306 gc1=306 gc2=306 totalMemory=766808064 requestId=SL-0001 boundaryId=TR-0001 slot=2 phase=SaveLoaded.
```

Fatal popup:

- detected at `2026-07-06T11:02:48.0231517+08:00`
- live process `ProcessId=31624`
- `MainWindowTitle=Fatal error in GC`
- fatal check text included `Unexpected mark stack overflow`

Crash capture:

- `CrashBaseline=Passed`
- `UnityCrashFreshness=stale-only`
- `FatalWindowProcessDump=MissingDump`
- `ComSvcsFull` exited with `-2147024773` and created no dump
- copied Unity crash directories were older than this run, newest `Crash_2026-07-05_175308638`

Instrumentation-invalid attempts retained but not used for conclusions:

- `GAME-SMOKE/20260706-075600`: fatal window reproduced, but the first process-dump helper used PowerShell's read-only `$PID` variable and aborted.
- `GAME-SMOKE/20260706-085929`: fatal window reproduced, but the first freshness helper failed on null nullable handling after live collect.

## Validation

Pre-run:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; restricted-network `NU1900` warnings only.
- `git diff --check`: passed with line-ending normalization warnings only.
- No `DolocTown.exe` was running before the valid smoke.

Post-run:

- Runtime lock was released.
- `official-mod-profile-summary.json` was exact for the requested profile and AutoFishing was disabled.
- Profile was restored.
- `process-check.txt` reported no leftover `DolocTown.exe`.

## Decision

The no-HookProbe Lite baseline did not stay in the 8.12 pre-SaveLoaded window. It shifted to post-SaveLoaded/pre-native-return: SaveLoaded completed, DTMAPI logged `Hook.Exit`, then the fatal popup appeared before the native LoadGame postfix recorded `nativeReturn`.

Do not start service-level hard-disable directly from this sample. The shifted window and missing fresh native crash/dump mean the next phase should first analyze or improve fresh live dump capture for the post-SaveLoaded/native-return boundary.

Do not run `Off`, PreLoad GC, UI owner/pair splits, or unknown native Unity object destruction from this result.

## Rollback

The source changes are smoke-only diagnostics in `tools/scripts/run-game-smoke.ps1`. To roll back, remove the new fatal-window crash freshness, process dump, and post-close wait parameters and result fields. Player runtime behavior is unaffected.

## Follow-Up

- Preserve `GAME-SMOKE/20260706-100234` as the valid Phase 8.13 no-HookProbe Lite fresh-capture attempt.
- Preserve `075600` and `085929` only as instrumentation-invalid evidence of fixed smoke-capture bugs.
- Before service/root isolation, decide whether to replace `ComSvcsFull` with a more reliable live dump method or add a smaller targeted post-SaveLoaded/native-return breadcrumb.
