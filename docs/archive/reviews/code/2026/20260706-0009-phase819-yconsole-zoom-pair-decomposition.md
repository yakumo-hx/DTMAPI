# Phase 8.19 YConsole / Zoom Pair Decomposition

Date: 2026-07-06 +08:00
Status: runtime-evidence-captured / singles passed / pair reproduced / issue-010-open
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: ISSUE-010 UI code root combination triage after Phase 8.18A showed `YConsole + Zoom` was sufficient to reproduce.

## Summary

Phase 8.19 kept the Phase 8.18 diagnostic route and changed only the UI code owner combination:

- no `-IncludeHookProbe`;
- no PreLoad GC;
- `SaveLoadObjectSnapshotMode=Lite`;
- `SmokeRootIsolationProfile=UiRuntime`;
- `SmokeNativeLoadContinuationProbe=VersionPatcher`;
- AutoFishing disabled;
- MoreEquipmentSlots and MoreSaves disabled;
- continuous `3600s` title idle;
- max two LoadGames.

Runs:

- Zoom-only: `docs/debug/evidence/GAME-SMOKE/20260706-201209` passed.
- YConsole-only: `docs/debug/evidence/GAME-SMOKE/20260706-211430` passed.
- YConsole + Zoom pair confirmation: `docs/debug/evidence/GAME-SMOKE/20260706-221651` reproduced Fatal GC before SaveLoaded.

Classification: a single Zoom or YConsole sample passing does not prove either owner safe, but the pair reproduced twice under comparable conditions (`20260706-185612` and `20260706-221651`) while both singles passed once. The current strongest suspect is the combined input/event owner-root pressure of `DTMAPI.DebugConsoleMod + DTMAPI.ZoomMod`, not MoreSaves, MoreEquipmentSlots, HookProbe, Full snapshots, or service hard-disable.

## Runtime Matrix

All runs used slot 3 / index 2 and the same base enabled IDs:

```text
Workshop.3742763309
Workshop.3742763843
Workshop.3742763540
Workshop.3742763706
Local.Yuuka_DTMAPI_ManboCardboardAudio
```

UI combinations:

| Run | Combination | Extra UI IDs | Result |
| --- | --- | --- | --- |
| `20260706-201209` | Zoom-only | `Workshop.3742717440` | passed |
| `20260706-211430` | YConsole-only | `Workshop.3742714442` | passed |
| `20260706-221651` | YConsole + Zoom pair | `Workshop.3742714442`, `Workshop.3742717440` | fatal before SaveLoaded |

All three profile summaries were valid:

- `Profile=CoreCustomAnimals`
- `Applied=true`
- expected CoreCustomAnimals local animal packs enabled;
- `Local.Yuuka_DTMAPI_AutoFishing` disabled;
- MoreEquipmentSlots `Workshop.3744059735` disabled;
- MoreSaves `Workshop.3742763050` disabled;
- profile restoration recorded.

## Owner / Root Counters

| Profile | Fatal | `ModOwner.records` | `EventHandler` | `InputButton` | `ConfigMenuPage` | `LoadedCodeMod` |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Phase 8.17 no UI code roots | no, twice | 65 | 11 | 2 | 5 | 6 |
| 8.19 Zoom-only | no | 77 | 14 | 7 | 6 | 7 |
| 8.19 YConsole-only | no | 75 | 15 | 4 | 6 | 7 |
| 8.18A YConsole + Zoom | yes | 87 | 18 | 9 | 7 | 8 |
| 8.19 pair confirmation | yes | 87 | 18 | 9 | 7 | 8 |

Per-owner roots:

```text
Zoom-only:
DTMAPI.ZoomMod={EventHandler=3; InputButton=5; ConfigMenuPage=1; LoadedCodeMod=1}

YConsole-only:
DTMAPI.DebugConsoleMod={EventHandler=4; InputButton=2; ConfigMenuPage=1; LoadedCodeMod=1}

YConsole + Zoom:
DTMAPI.DebugConsoleMod={EventHandler=4; InputButton=2; ConfigMenuPage=1; LoadedCodeMod=1}
DTMAPI.ZoomMod={EventHandler=3; InputButton=5; ConfigMenuPage=1; LoadedCodeMod=1}
```

`DTMAPI.DebugConsoleHost` remained as an unsupported host/API owner under `UiRuntime`; it is not the same as the enabled or disabled `DTMAPI.DebugConsoleMod` code owner.

## SaveLoad Results

Zoom-only:

```text
RunStatus=Passed
requests=2
nativeEnter=2
nativeReturn=2
saveLoaded=2
duplicateRequests=0
fatalWindows=0
LastNativeContinuationStep=DolocAPI.LoadGame.Exit
```

YConsole-only:

```text
RunStatus=Passed
requests=2
nativeEnter=2
nativeReturn=2
saveLoaded=2
duplicateRequests=0
fatalWindows=0
LastNativeContinuationStep=DolocAPI.LoadGame.Exit
```

YConsole + Zoom pair:

```text
RunStatus=Aborted
SaveLoaded=Failed
requests=1
nativeEnter=1
nativeReturn=0
saveLoaded=0
duplicateRequests=0
LastNativeContinuationStep=DolocAPI.LoadGame.Enter
```

The pair fataled before `AfterLoadArchiveData`, `VersionPatcher`, or `MapManager.Init` continuation breadcrumbs.

## Fatal Window

The pair confirmation fatal was again pre-SaveLoaded native LoadGame activation:

```text
NativeContinuation.Step=DolocAPI.LoadGame.Enter method=DolocAPI.LoadGame phase=Enter elapsedMs=11 gc0=283 gc1=283 gc2=283 totalMemory=253448192 requestId=SL-0001 boundaryId=TR-0001 slot=2 runtimePhase=Update threadId=1 threadName=none nativeEnter=1 nativeReturn=0 saveLoaded=0 exceptionType=none.
```

`Lite` was verified at both pre-load snapshots:

```text
BeforeNextLoadGame ... saveLoad={mode=Lite}; resourceLifecycle={mode=Lite}; modOwner={mode=Lite}; Runtime={snapshotMode=Lite ...}
LoadGameNativeEnter ... saveLoad={mode=Lite, requestId=SL-0001}; resourceLifecycle={mode=Lite}; modOwner={mode=Lite}; Runtime={snapshotMode=Lite ...}
```

Unity stack for `20260706-221651` pointed through the native room/dungeon load activation path:

```text
DolocTown.GameData.RoomProtoPatch:CreateGameMaps
DolocTown.MonsterEnv:.ctor
DolocTown.Room:AfterLoadData
DolocTown.DungeonManager:AfterLoadData
DolocTown.GameData.DungeonArchiveData:AfterLoadData
DolocTown.GameData.DataPersistenceManager:LoadGame
```

This differs in exact allocation site from `20260706-185612` (`TerrainLayer` / terrain slot dictionary resize) but remains the same broader pre-SaveLoaded `DataPersistenceManager.LoadGame` room/dungeon activation window.

## Validation

Before runtime:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings.
- `git diff --check`: passed with line-ending normalization warnings only.
- No `DolocTown.exe` was running.

After runtime:

- runtime lock was released after each run;
- no leftover `DolocTown.exe`;
- all profiles were restored;
- `SaveLoadObjectSnapshotMode=Lite` was verified in result and DTMAPI snapshot lines;
- `SmokeRootIsolationProfile=UiRuntime` was active;
- `SmokeNativeLoadContinuationProbe=VersionPatcher` was active.

The pair fatal captured `FatalWindowProcessDump=Captured:DbgHelpFull` and fresh Unity crash evidence. The DbgHelp `.dmp` itself is too large for routine handoff packages; `Process-Dumps/TOO-LARGE-DUMP-README.txt` and `process-dump-summary.txt` preserve the metadata.

## Classification

Current strongest conclusion:

```text
Singles pass once, pair fatals twice.
The immediate suspect is YConsole + Zoom combined input/event/config/code-owner pressure.
```

This is not yet a root-cause fix and does not prove either single owner is harmless. The next phase should stop mod-combination bisection and split root type for the pair:

- suppress Zoom input registrations while keeping Zoom code/config owner;
- suppress YConsole input registrations while keeping YConsole code/config owner;
- suppress both input registrations while keeping code/config owners;
- only then consider event-handler suppression as a separate axis.

## Non-Changes

This phase did not:

- change source code or public API;
- change ordinary player runtime behavior;
- run HookProbe;
- run PreLoad GC;
- run `SaveLoadObjectSnapshotMode=Off`;
- run UI triples;
- run service hard-disable;
- run CustomAnimals / AudioReplacement / Manbo / action utility isolation;
- destroy unknown native Unity objects;
- claim ISSUE-010 solved.
