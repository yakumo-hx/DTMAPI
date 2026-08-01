# Phase 8.12 HookProbe / SaveLoaded Diagnostic Pressure Isolation

Date: 2026-07-06 +08:00
Status: runtime-evidence-captured / no-HookProbe Lite fatal reproduced before SaveLoaded / issue-010-open
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: ISSUE-010 full known-failing profile, continuous one-hour title idle, no PreLoad GC, `Lite` object snapshot mode, HookProbe removed.

## Summary

Phase 8.12 ran the next diagnostic-pressure control requested after the Phase 8.11 shifted window:

- full known-failing profile unchanged
- no `-IncludeHookProbe`
- no `-AutoExercisePreLoadGcProbe`
- `-SaveLoadObjectSnapshotMode Lite`
- `3600s` continuous title idle
- max two LoadGame attempts
- `-TimeoutSeconds 6000`
- `-FatalWindowCrashDumpGraceSeconds 30`

Evidence:

- `docs/debug/evidence/GAME-SMOKE/20260706-025334`

Result:

- The profile reproduced `Fatal error in GC / Unexpected mark stack overflow`.
- The fatal occurred before SaveLoaded: `saveLoaded=0`, `nativeReturn=0`, and no `SaveLoaded.Step` breadcrumbs were present.
- `HookProbe=Skipped` and `IncludeHookProbe=False`, so HookProbe is not a necessary condition for this specific pre-SaveLoaded fatal recurrence.
- `SaveLoadObjectSnapshotMode=Lite` was verified in both `BeforeNextLoadGame` and `LoadGameNativeEnter`, so full title-return object snapshot/delta/logging was not required for this run to reach the fatal popup.
- Unity crash collection copied only older crash directories; no fresh 03:54 Unity crash directory/stack was produced for this 8.12 fatal. Treat this as a lower-strength native-stack sample than `GAME-SMOKE/20260705-194759` and `GAME-SMOKE/20260705-231212`.

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

Not used:

- `-AutoExercisePreLoadGcProbe`
- `-IncludeHookProbe`
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

## HookProbe Removal Evidence

HookProbe was absent from the run:

- `summary.txt`: `IncludeHookProbe=False`
- `result.json`: `HookProbe=Skipped`
- `DTMAPI-latest.log`: owner summaries did not include `DTMAPI.HookProbeMod`.

The stable owner/root counters dropped compared with the Phase 8.11 HookProbe+Lite sample:

| Metric | Phase 8.11 HookProbe + Lite | Phase 8.12 no-HookProbe + Lite |
| --- | ---: | ---: |
| `ModOwner.records` | 119 | 100 |
| `EventHandler` | 28 | 19 |
| `InputButton` | 9 | 9 |
| `ConfigPage` / `ConfigMenuPage` | 10 | 9 |
| `LoadedCodeMod` | 13 | 10 |

Despite that reduction, the run still reproduced Fatal GC before SaveLoaded.

## Lite Mode Evidence

Lite was verified beyond the command line:

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

The final object delta stayed tiny and owner deltas remained empty:

```text
11:LoadGameNativeEnter ... metrics=2 ... prevDelta={none}; sameBoundaryDelta={Snapshot.sections=0->3(+3), Runtime.currentLoadingSlot=0->2(+2)}; ownerPrevDelta={none}; ownerSameBoundaryDelta={none}; ownerNonZero={none}
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
- `SaveLoadBoundary=Failed`
- `ProcessExited=Passed`
- `ForcedClose=Passed`

SaveLoad summary:

```text
requests=1
active=SL-0001:slot=2;owner=NativeGame;source=Harmony LoadGame Prefix;status=native-entered
nativeEnter=true
nativeReturn=false
saveLoaded=false
duplicateRequests=0
fatalWindows=0
last=NativeEnter:SL-0001:slot=2:phase=Update:source=Harmony LoadGame Prefix
```

Final DTMAPI position before the fatal window:

```text
LoadGame requested for slot/index 2. requestId=SL-0001.
```

Breadcrumb result:

- `SaveLoaded.Step=` count: `0`
- No hook-level or runtime-level SaveLoaded breadcrumb ran.
- `NotifySaveLoaded` did not enter.

Fatal window collection:

- Live fatal process: `ProcessId=46168`, `MainWindowTitle=Fatal error in GC`.
- `FatalWindowCrashDumpGraceSeconds=30`.
- The harness recorded live and post-close crash-evidence phases as present, but the copied Unity crash directories were stale relative to this run.

Crash evidence limitation:

- The 8.12 fatal window was detected at `2026-07-06T03:54:02.9599806+08:00`.
- `Unity-Crashes/summary.txt` copied the latest directory `Crash_2026-07-05_175308638`, whose `DirectoryLastWrite` was `2026-07-06T01:53:14.7809740+08:00`.
- No fresh `Crash_2026-07-06_0354...` directory, fresh `Player.log`, or fresh `crash.dmp` was created for this specific fatal.
- Root `Unity-Player.log` contains the DTMAPI load-request line but no `Fatal error in GC`, `Unexpected mark stack overflow`, `TerrainLayer`, `TextureUtils`, `MapManager.Init`, or `DataPersistenceManager.LoadGame` crash stack for the 03:54 fatal.

Therefore this run classifies the DTMAPI boundary and HookProbe/Lite conditions strongly, but it does not provide a fresh Unity native stack.

## Interpretation

This run answers the narrow Phase 8.12 control question:

- HookProbe is not necessary for the pre-SaveLoaded fatal class, because no-HookProbe + Lite still reproduced before SaveLoaded.
- Heavy `Full` title-return object snapshot/delta/logging is not necessary for this specific fatal popup to occur, because Lite was active at `BeforeNextLoadGame` and `LoadGameNativeEnter`.
- The run does not prove a new Terrain/Room/Dungeon stack, because Unity did not write a fresh crash report. Use the previous comparable no-probe full samples for stack-level evidence.

Current classification:

- The full stable profile remains under high GC/root-set pressure after continuous one-hour title idle.
- Removing HookProbe lowered stable owner/event/code-mod roots but did not prevent the pre-SaveLoaded fatal popup.
- The next useful step should move toward native LoadGame/root-set or service-level isolation planning, while improving fresh Unity crash capture if stack-level evidence is required.

High-value next cuts to discuss before running:

- Decide whether the next phase should first improve fatal crash capture for no-HookProbe + Lite, because this run lacked a fresh Unity crash dump.
- If service-level isolation begins, prefer one service-level hard-disable at a time under no-HookProbe + Lite, starting with non-player diagnostic/root-heavy services such as `NativeUiLayoutDiagnostics`, then SaveSlots/MoreSaves UI diagnostics, DebugConsole, AudioReplacement, and CustomAnimals.
- Do not go back to UI owner single/pair bisection.

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
- Fatal popup/process/log-position evidence was captured.
- Fresh Unity crash stack/dump was missing for this specific 03:54 fatal; copied crash directories were older.

## Decision

Do not continue immediately to:

- `Off`, because Lite did not pass and this run already reproduced before SaveLoaded without HookProbe.
- more UI owner/pair bisection.
- PreLoad GC.
- blind service-level hard-disable without deciding the next control and crash-capture target.
- unknown native object destruction.

The next planning question is whether Phase 8.13 should:

- rerun a fresh no-HookProbe + Lite capture with stronger fresh-crash evidence expectations, or
- begin service-level hard-disable isolation under no-HookProbe + Lite, with explicit recognition that `025334` lacks a fresh Unity stack.

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
