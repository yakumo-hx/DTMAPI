# Phase 8.18 UI Code Root Necessity / Combination Triage

Date: 2026-07-06 +08:00
Status: runtime-evidence-captured / input-heavy UI pair reproduced / issue-010-open
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: ISSUE-010 managed UI code root add-back after Phase 8.17 no-UI-code-root passes.

## Summary

Phase 8.18 followed the external review request to keep the Phase 8.17 diagnostic shape and add back UI code owners by combination, starting with the input-heavy pair:

- `Workshop.3742714442` / `DTMAPI.DebugConsoleMod`
- `Workshop.3742717440` / `DTMAPI.ZoomMod`

Evidence:

- `docs/debug/evidence/GAME-SMOKE/20260706-185612`

This first 8.18A run reproduced the Fatal GC window after a continuous `3600s` title idle, before `SaveLoaded` and before native LoadGame returned:

```text
RunStatus=Aborted
SaveLoadCycle=Failed
NoFatalInstanceWindow=Failed
ProcessExited=Passed
requests=1
nativeEnter=1
nativeReturn=0
saveLoaded=0
duplicateRequests=0
LastNativeContinuationStep=DolocAPI.LoadGame.Enter
```

Because the first input-heavy pair already reproduced, Phase 8.18 stopped without running the all-but-one triples. The next phase should split `YConsole` versus `Zoom`, or inspect their shared input/static/config roots, rather than continuing broad UI pair/triple bisection.

## Runtime Shape

The run used:

- slot 3 / index 2;
- no `-IncludeHookProbe`;
- no `-AutoExercisePreLoadGcProbe`;
- `-AutoExerciseSaveLoadCycle`;
- `-SaveLoadCycleCount 2`;
- `-SaveLoadCycleInitialTitleIdleSeconds 3600`;
- `-SaveLoadCycleIntervalSeconds 5`;
- `-SaveLoadCycleInSaveSeconds 5`;
- `-SaveLoadObjectSnapshotMode Lite`;
- `-SmokeRootIsolationProfile UiRuntime`;
- `-SmokeNativeLoadContinuationProbe VersionPatcher`;
- `-TimeoutSeconds 6000`;
- `-OfficialModProfile CoreCustomAnimals`;
- base extra IDs `Workshop.3742763309`, `Workshop.3742763843`, `Workshop.3742763540`, `Workshop.3742763706`, `Local.Yuuka_DTMAPI_ManboCardboardAudio`;
- UI pair IDs `Workshop.3742714442` and `Workshop.3742717440`;
- `-FatalWindowCrashDumpGraceSeconds 30`;
- `-FatalWindowProcessDumpMode DbgHelpFull`;
- `-FatalWindowPostCloseCrashDumpWaitSeconds 60`.

The run intentionally did not use HookProbe, PreLoad GC, `SaveLoadObjectSnapshotMode=Off`, service hard-disable, or unknown native Unity object destruction.

## Profile Validation

`official-mod-profile-summary.json` was valid:

- `Profile=CoreCustomAnimals`
- `Applied=true`
- `ExtraEnabledIds` exactly matched the five base IDs plus YConsole and Zoom.
- Enabled IDs included the expected CoreCustomAnimals local animal packs.
- `Local.Yuuka_DTMAPI_AutoFishing` was disabled.
- MoreEquipmentSlots `Workshop.3744059735` and MoreSaves `Workshop.3742763050` were disabled.
- The profile was restored after the run.

The local fallback UI IDs remained disabled, while the requested Workshop IDs were enabled. `DTMAPI.DebugConsoleHost` remained as an unsupported host/API owner under `UiRuntime`; that is not the same as disabling or enabling the `DTMAPI.DebugConsoleMod` code owner.

## Owner / Root Counters

The input-heavy pair increased managed root pressure from the Phase 8.17 no-UI-code-root profile and reproduced the fatal:

| Metric | Phase 8.17 no UI code roots | Phase 8.18A YConsole + Zoom |
| --- | ---: | ---: |
| `ModOwner.records` | 65 | 87 |
| `EventHandler` | 11 | 18 |
| `InputButton` | 2 | 9 |
| `ConfigPage` / `ConfigMenuPage` | 5 | 7 |
| `LoadedCodeMod` | 6 | 8 |
| Unexpected remaining owners | none | none |

Observed UI owners:

```text
ExpectedEnabledUiOwners=DTMAPI.DebugConsoleMod|DTMAPI.ZoomMod
ExpectedDisabledUiOwners=DTMAPI.MoreEquipmentSlots|DTMAPI.MoreSaves
UnsupportedRemainingOwners=DTMAPI.DebugConsoleHost
UnexpectedRemainingOwners=none
```

The YConsole + Zoom pair contributed all of the UI-code-root delta over Phase 8.17:

```text
DTMAPI.DebugConsoleMod: ConfigMenuPage=1, EventHandler=4, InputButton=2, LoadedCodeMod=1
DTMAPI.ZoomMod: ConfigMenuPage=1, EventHandler=3, InputButton=5, LoadedCodeMod=1
```

## Snapshot / Continuation Evidence

`Lite` was verified at both pre-load boundaries:

```text
TitleReturn object graph snapshot 9:BeforeNextLoadGame ... saveLoad={mode=Lite, requestId=none}; resourceLifecycle={mode=Lite}; modOwner={mode=Lite}; Runtime={snapshotMode=Lite ...}
TitleReturn object graph snapshot 11:LoadGameNativeEnter ... saveLoad={mode=Lite, requestId=SL-0001}; resourceLifecycle={mode=Lite}; modOwner={mode=Lite}; Runtime={snapshotMode=Lite ...}
```

The final DTMAPI line was:

```text
NativeContinuation.Step=DolocAPI.LoadGame.Enter method=DolocAPI.LoadGame phase=Enter elapsedMs=8 gc0=284 gc1=284 gc2=284 totalMemory=254267392 requestId=SL-0001 boundaryId=TR-0001 slot=2 runtimePhase=Update threadId=1 threadName=none nativeEnter=1 nativeReturn=0 saveLoaded=0 exceptionType=none.
```

No `AfterLoadArchiveData`, `VersionPatcher`, or `MapManager.Init` continuation breadcrumb was reached.

The fresh Unity crash stack again pointed into the pre-SaveLoaded terrain/dungeon native load activation path:

```text
System.Collections.Generic.Dictionary`2<UnityEngine.Vector2Int, DolocTown.TerrainSlot>:Resize
DolocTown.TerrainLayer:.ctor
DolocTown.Terrain:CreateLayers
DolocTown.Room:AfterLoadData
DolocTown.DungeonManager:AfterLoadData
DolocTown.GameData.DungeonArchiveData:AfterLoadData
DolocTown.GameData.DataPersistenceManager:LoadGame
```

## Validation

Before runtime:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings for nuget.org vulnerability metadata.
- `git diff --check`: passed with line-ending normalization warnings only.
- No `DolocTown.exe` was running.

After runtime:

- runtime lock was released;
- no leftover `DolocTown.exe`;
- profile restore was recorded;
- DbgHelp full dump capture reported `Captured:DbgHelpFull`;
- Unity crash collection reported a fresh post-close crash directory and final source from live collect/post-close evidence.

## Classification

Phase 8.18A shows that removing all UI code roots in Phase 8.17 was not merely random pass noise: adding back just `YConsole + Zoom` restored enough managed root pressure to reproduce the recurring pre-SaveLoaded terrain/dungeon fatal under the same light diagnostic route.

This does not prove whether YConsole alone, Zoom alone, or their combined input/config/event root shape is the necessary root. It also does not prove the issue is a conventional leak, because the fatal still happens in native `DataPersistenceManager.LoadGame` terrain/dungeon activation after a high-pressure title-stable managed graph.

Recommended next step:

- split `YConsole` versus `Zoom` under the same exact 8.18 route, or inspect their shared input/static/config roots first;
- do not run all-but-one triples from this phase because the first pair already reproduced;
- do not start broad service hard-disable until the YConsole/Zoom axis is resolved.

## Non-Changes

This phase did not:

- change source code or public API;
- change ordinary player runtime behavior;
- run HookProbe;
- run PreLoad GC;
- run `SaveLoadObjectSnapshotMode=Off`;
- run service hard-disable;
- destroy unknown native Unity objects;
- claim ISSUE-010 solved.
