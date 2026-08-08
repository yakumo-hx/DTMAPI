# 20260706-0003 - Phase 8.12 HookProbe / SaveLoaded Diagnostic Pressure Isolation

Date: 2026-07-06 +08:00
Status: runtime-evidence-captured / issue-010-open
Area: smoke/saveload/issue-010/diagnostic-pressure/hookprobe/native-loadgame

## Trigger

After Phase 8.11 showed `SaveLoadObjectSnapshotMode=Lite` did not pass but shifted the fatal window to post-SaveLoaded native LoadGame continuation, the external review requested a no-HookProbe Lite run before any broader service-level hard-disable. The goal was to determine whether HookProbe was a necessary diagnostic-pressure root for the current fatal class.

## Summary

Ran the full known-failing profile:

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

Evidence `docs/debug/evidence/GAME-SMOKE/20260706-025334` reproduced Fatal GC before SaveLoaded. HookProbe was skipped and stable owner/event/code-mod roots dropped, but the fatal popup still appeared during the first post-idle native LoadGame before `NotifySaveLoaded`.

## Changed Files

- `docs/reviews/code/2026/20260706-0002-phase812-hookprobe-diagnostic-pressure-isolation.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260706-0003-phase812-hookprobe-diagnostic-pressure-isolation.md`
- `docs/updates/INDEX.md`

No source code or public API files were changed in this phase.

## Evidence

- `docs/debug/evidence/GAME-SMOKE/20260706-025334`
  - `RunStatus=Aborted`
  - `HookProbe=Skipped`
  - `PreLoadForcedGCProbe=Skipped`
  - `SaveLoadObjectSnapshotMode=Lite`
  - `SaveLoaded=Failed`
  - `SaveLoadCycle=Failed`
  - `NoFatalInstanceWindow=Failed`
  - `SaveLoadBoundary=Failed`
  - `ProcessExited=Passed`
  - `ForcedClose=Passed`
  - `OfficialModProfileRestored=True`
- SaveLoad state:
  - `requests=1`
  - `nativeEnter=1`
  - `nativeReturn=0`
  - `saveLoaded=0`
  - duplicate requests `0`
- Final DTMAPI line:
  - `LoadGame requested for slot/index 2. requestId=SL-0001.`
- Fatal popup:
  - fatal process `ProcessId=46168`, `MainWindowTitle=Fatal error in GC`
  - `fatal-window-live-check.txt` recorded `Fatal error in GC / Unexpected mark stack overflow`
- Fresh Unity crash stack:
  - missing for this specific 03:54 fatal
  - `Unity-Crashes/summary.txt` copied older crash directories, newest `Crash_2026-07-05_175308638` had `DirectoryLastWrite=2026-07-06T01:53:14.7809740+08:00`
  - root `Unity-Player.log` did not contain a fresh fatal/native stack

## Lite And HookProbe Verification

Lite was verified beyond the command line:

- `summary.txt`: `SaveLoadObjectSnapshotMode=Lite`
- `result.json`: `"SaveLoadObjectSnapshotMode": "Lite"`
- `DTMAPI-latest.log`: `SaveLoad object snapshot mode configured mode=Lite source=smoke-settings.json smokeOnly=true.`
- `BeforeNextLoadGame`: snapshot `9` logged `saveLoad={mode=Lite, requestId=none}` and `Runtime={snapshotMode=Lite...}`.
- `LoadGameNativeEnter`: snapshot `11` logged `saveLoad={mode=Lite, requestId=SL-0001}` and `Runtime={snapshotMode=Lite...}`.

HookProbe was removed:

- `summary.txt`: `IncludeHookProbe=False`
- `result.json`: `HookProbe=Skipped`
- `DTMAPI-latest.log`: owner summaries did not include `DTMAPI.HookProbeMod`.

Stable owner counts dropped from the Phase 8.11 HookProbe+Lite sample:

- `ModOwner.records`: `119 -> 100`
- `EventHandler`: `28 -> 19`
- `InputButton`: `9 -> 9`
- `ConfigPage` / `ConfigMenuPage`: `10 -> 9`
- `LoadedCodeMod`: `13 -> 10`

## Validation

Pre-run:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; restricted-network `NU1900` warnings only.
- `git diff --check`: passed with line-ending normalization warnings only.
- No `DolocTown.exe` process was running before the smoke.

Post-run:

- Runtime lock was released.
- `official-mod-profile-summary.json` was exact for the requested profile and AutoFishing was disabled.
- Profile was restored.
- No leftover `DolocTown.exe` process was found after collection/cleanup.

## Decision

HookProbe is not a necessary condition for this pre-SaveLoaded fatal recurrence.

Full object snapshot/delta/logging is not necessary for this specific fatal popup either, because Lite was active at the final `BeforeNextLoadGame` and `LoadGameNativeEnter` snapshots. However, this sample lacks a fresh Unity native crash stack, so stack-level comparisons should still rely on `GAME-SMOKE/20260705-194759` and `GAME-SMOKE/20260705-231212` unless a new fresh dump is captured.

Do not resume UI owner/pair splits, do not run PreLoad GC, do not run `Off` immediately, and do not destroy unknown native Unity objects.

## Rollback

Docs-only update. No source rollback is needed.

## Follow-Up

- Keep `GAME-SMOKE/20260706-025334` as the Phase 8.12 no-HookProbe Lite control.
- Ask the next external review whether Phase 8.13 should first improve fresh crash capture for no-HookProbe+Lite, or begin service-level hard-disable under no-HookProbe+Lite.
- If service-level isolation starts, prefer one service/root at a time and start with diagnostic/root-heavy services before gameplay/content roots.
