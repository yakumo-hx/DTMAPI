# Phase 8.16 Post-SaveLoaded Native Continuation Probe

Date: 2026-07-06 +08:00
Status: source-and-runtime-evidence-captured / pre-SaveLoaded terrain-dungeon fatal / live-dump-captured / issue-010-open
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: ISSUE-010 native LoadGame continuation breadcrumbs, smoke-only VersionPatcher probe, UiRuntime isolation repeat.

## Summary

Phase 8.16 followed the external review request to avoid another broad service-disable run and instead add a narrow smoke-only probe around the native continuation that had been suspected by Phase 8.15.

The probe was installed, but the runtime did not reach any of the continuation targets. The final DTMAPI line was the first `DolocAPI.LoadGame.Enter` breadcrumb:

```text
NativeContinuation.Step=DolocAPI.LoadGame.Enter method=DolocAPI.LoadGame phase=Enter elapsedMs=6 gc0=225 gc1=225 gc2=225 totalMemory=205991936 requestId=SL-0001 boundaryId=TR-0001 slot=2 runtimePhase=Update threadId=1 threadName=none nativeEnter=1 nativeReturn=0 saveLoaded=0 exceptionType=none.
```

Fresh Unity crash evidence then showed the same pre-SaveLoaded native terrain/dungeon activation family as the comparable Phase 8.10 and 8.14 failures:

```text
Dictionary<Vector2Int,TerrainSlot>.Resize
TerrainLayer..ctor
Terrain.CreateLayers
Terrain.Create
Room.ResetTerrain
Room.AfterLoadData
Dungeon.AfterLoadData
DungeonManager.AfterLoadData
DungeonArchiveData.AfterLoadData
DataPersistenceManager.LoadGame
DolocAPI:DMD<DolocAPI::LoadGame>
```

Classification: Phase 8.16 does not implicate `VersionPatcher`, `AfterLoadArchiveData`, `MapManager.Init`, or `TextureUtils.DrawArea` for this recurrence because none of those continuation breadcrumbs fired. The current sample again points to native `LoadGame` terrain/dungeon graph allocation before SaveLoaded.

## Implementation Changes

Added smoke-only native continuation probe support:

- `tools/scripts/run-game-smoke.ps1`
  - new `-SmokeNativeLoadContinuationProbe None|VersionPatcher`, default `None`;
  - result/summary fields for probe mode, runtime hook evidence, and last continuation breadcrumb;
  - `native-continuation-probe-summary.txt` in smoke evidence.
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
  - loads `SmokeNativeLoadContinuationProbe`.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
  - stores the smoke-only probe mode and hook state.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs`
  - installs probe hooks only when requested.
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
  - records lightweight native-continuation breadcrumbs.
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
  - records `NativeContinuation.Step` lines with method, phase, elapsed time, GC counts, total memory, request id, boundary id, thread id/name, and save-load counters.
- `src/DTMAPI.Core/Runtime/SaveLoadRequestCoordinatorService.cs`
  - exposes lightweight diagnostic counts for the breadcrumb.

The probe targets were:

- `DolocAPI.LoadGame` prefix/postfix as the baseline enter/exit marker.
- `DolocAPI.AfterLoadArchiveData(bool)`.
- `DolocTown.VersionPatcher.LoadAllVersionPatches()`.
- `DolocTown.VersionPatcher.LoadAllVersionPatchesBeyond(string)`.
- `DolocTown.MapManager.Init(bool)`.

`DolocTown.TextureUtils.DrawArea` was intentionally reported as `unsupported-skipped-high-frequency` to avoid adding heavy logging pressure to a potentially hot texture path.

Breadcrumbs do not enumerate objects, stringify dictionaries, run LINQ summaries, use reflection helpers, or publish object-delta text.

## Runtime Command

Evidence:

- `docs/debug/evidence/GAME-SMOKE/20260706-150952`

Command shape:

- slot 3 / index 2
- no `-IncludeHookProbe`
- no `-AutoExercisePreLoadGcProbe`
- `-AutoExerciseSaveLoadCycle`
- `-SaveLoadCycleCount 2`
- `-SaveLoadCycleInitialTitleIdleSeconds 3600`
- `-SaveLoadCycleIntervalSeconds 5`
- `-SaveLoadCycleInSaveSeconds 5`
- `-SaveLoadObjectSnapshotMode Lite`
- `-SmokeRootIsolationProfile UiRuntime`
- `-SmokeNativeLoadContinuationProbe VersionPatcher`
- `-TimeoutSeconds 6000`
- `-OfficialModProfile CoreCustomAnimals`
- full nine extra IDs for action/utility, Manbo audio, YConsole, MoreEquipmentSlots, MoreSaves, and Zoom
- `-FatalWindowCrashDumpGraceSeconds 30`
- `-FatalWindowProcessDumpMode DbgHelpFull`
- `-FatalWindowPostCloseCrashDumpWaitSeconds 60`

Not used:

- HookProbe
- PreLoad GC probe
- `SaveLoadObjectSnapshotMode=Off`
- UI owner/pair split
- new service hard-disable
- unknown native Unity object destruction

## Profile Validation

`official-mod-profile-summary.json` was valid:

- `Profile=CoreCustomAnimals`
- `Applied=true`
- `ExtraEnabledIds` exactly matched the requested nine full-profile IDs.
- Enabled IDs included the nine extra IDs plus expected CoreCustomAnimals local animal packs: ShellCrab, HatchAssets, OilfloaterAssets, MoleAssets, and DreckoAssets.
- `Local.Yuuka_DTMAPI_AutoFishing` was disabled.
- No unexpected Workshop/feature mod was enabled.
- `OfficialModProfileRestored=True` was recorded in `summary.txt`.

## Probe Evidence

Runtime installed the requested probe hooks:

```text
SmokeNativeLoadContinuationProbe=VersionPatcher
AfterLoadArchiveDataHook=installed
VersionPatcherLoadAllHook=installed
VersionPatcherLoadBeyondHook=installed
MapManagerInitHook=installed
TextureUtilsDrawAreaHook=unsupported-skipped-high-frequency
```

The run also kept the Phase 8.15 UiRuntime isolation:

```text
SmokeRootIsolationProfile=UiRuntime
SmokeRootIsolation.Disabled=NativeUiLayoutDiagnostics|SaveSlots|EquipmentSlots|Camera
SmokeRootIsolation.Unsupported=DebugConsoleHost
```

`Lite` was verified at the two pre-load boundaries:

```text
TitleReturn object graph snapshot 9:BeforeNextLoadGame ... saveLoad={mode=Lite, requestId=none} ... Runtime={snapshotMode=Lite ...}
TitleReturn object graph snapshot 11:LoadGameNativeEnter ... saveLoad={mode=Lite, requestId=SL-0001} ... Runtime={snapshotMode=Lite ...}
```

The only native-continuation breadcrumb emitted was:

```text
NativeContinuation.Step=DolocAPI.LoadGame.Enter
```

No `DolocAPI.AfterLoadArchiveData`, `VersionPatcher`, `MapManager.Init`, `SaveLoaded.Step`, or `DolocAPI.LoadGame.Exit` breadcrumb was emitted.

## Fatal Window

Result summary:

```text
RunStatus=Aborted
SaveLoadObjectSnapshotMode=Lite
SmokeRootIsolationProfile=UiRuntime
SmokeNativeLoadContinuationProbe=VersionPatcher
SaveLoaded=Failed
SaveLoadCycle=Failed
NoFatalInstanceWindow=Failed
ProcessExited=Passed
FatalWindowProcessDump=Captured:DbgHelpFull
UnityCrashFreshness=fresh
```

Save-load request summary:

```text
requests=1
nativeEnter=1
nativeReturn=0
saveLoaded=0
duplicateRequests=0
last=NativeEnter:SL-0001:slot=2:phase=Update:source=Harmony LoadGame Prefix
```

Fatal popup live evidence:

```text
FatalWindowDetectedAt=2026-07-06T16:10:21.0513846+08:00
ProcessId=11580
MainWindowTitle=Fatal error in GC
```

Last DTMAPI log position:

```text
LastLine=2026-07-06 16:10:19.121 +08:00 [Info] [DTMAPI] NativeContinuation.Step=DolocAPI.LoadGame.Enter ...
```

The fatal occurred after the native `LoadGame` enter marker and before the next probe target.

## Crash Evidence

Fresh Unity crash evidence was captured:

```text
Unity-Crashes/Crash_2026-07-06_081133308-54af863107cf
Player.log bytes=787870
crash.dmp bytes=1727998
```

`fatal-window-crash-freshness.txt` reports:

```text
Status=fresh
FreshDirectories=C:\Users\Administrator\AppData\Local\Temp\RedSawGames\DolocTown\Crashes\Crash_2026-07-06_081133308
Reason=At least one copied crash directory was created or modified after this smoke run started.
```

DbgHelp full live process dump was captured:

```text
Process-Dumps/DolocTown-11580-fatal-live-dbghelp.dmp
DumpSize=4891602600
DumpSha256=B99DAC880F82234DEB7B43B4E095CA3EDD9D6560A370C7DCD1AA022BEC3EEC78
CapturedMode=DbgHelpFull
```

Routine packages should include `Process-Dumps/TOO-LARGE-DUMP-README.txt`, not the 4.9 GB dump.

## Interpretation

This run narrows the shifted 8.15 suspicion:

- `VersionPatcher` remains relevant to the Phase 8.15 shifted stack, but it was not reached in this Phase 8.16 recurrence.
- `AfterLoadArchiveData`, `MapManager.Init`, and `TextureUtils.DrawArea` were also not reached by breadcrumbs in this recurrence.
- The fresh stack points at `TerrainLayer` dictionary growth during terrain/dungeon restore inside `DataPersistenceManager.LoadGame`.
- `Lite`, no HookProbe, and UiRuntime isolation did not remove the pre-SaveLoaded native fatal.

Current best classification:

```text
pre-SaveLoaded native LoadGame terrain/dungeon allocation over a high-pressure stable root set
```

The next investigation should avoid returning to UI owner/pair splits and avoid assuming `VersionPatcher` is the only continuation root. The strongest next evidence would be either external dump decoding or a narrow native/root-set isolation plan centered on the terrain/dungeon load graph and content/native-heavy roots that remain active under no-HookProbe + Lite.

## Validation

Before runtime:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings.
- `git diff --check`: passed with line-ending normalization warnings only.
- No existing `DolocTown.exe`.

After runtime:

- Runtime lock released.
- No leftover `DolocTown.exe`.
- Profile restored.
- Fresh Unity crash evidence captured post-close.
- DbgHelp full live dump captured.

## Non-Changes

This phase did not:

- change public API;
- do a GameBridge large refactor;
- run PreLoad GC;
- run `SaveLoadObjectSnapshotMode=Off`;
- resume UI owner/pair split;
- run a new broad service-disable matrix;
- destroy unknown native `GameObject`, `Component`, `AudioClip`, `AssetBundle`, `RuntimeAnimatorController`, or Unity shell objects;
- change ordinary player runtime behavior outside smoke-only diagnostics.

## Next Step

Recommended next phase:

```text
Phase 8.17 Native LoadGame Terrain/Dungeon Root-Set Analysis
```

Start from the fresh Phase 8.16 crash stack plus the DbgHelp dump:

- decode the live dump if a debugger is available;
- compare Phase 8.10, 8.14, and 8.16 pre-SaveLoaded terrain/dungeon stacks;
- inspect native terrain/dungeon allocation pressure around `TerrainLayer`, `Room.ResetTerrain`, and `DungeonArchiveData.AfterLoadData`;
- only then choose the first narrow isolation axis among content/native-heavy roots such as CustomAnimals/AnimalVoice, AudioReplacement platform players, action/utility hooks, or smoke diagnostics.
