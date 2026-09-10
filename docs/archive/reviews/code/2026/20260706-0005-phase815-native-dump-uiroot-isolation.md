# Phase 8.15 Native Dump Triage And First UiRuntime Root Isolation

Date: 2026-07-06 +08:00
Status: source-and-runtime-evidence-captured / shifted-window / live-dump-captured / issue-010-open
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: ISSUE-010 native dump triage, smoke-only UiRuntime root isolation, one-hour title idle full profile, no HookProbe, no PreLoad GC, `Lite` snapshot mode.

## Summary

Phase 8.15 followed the external review request in two stages:

1. Consume the Phase 8.14 DbgHelp full live dump before more isolation.
2. If the dump cannot be decoded locally, run exactly one first smoke-only UiRuntime root isolation profile.

Dump analysis could not decode the native stack because no Windows debugger was available on this machine. The analysis still recorded the dump path, size, SHA256, and a missing-debugger README:

- `docs/debug/evidence/GAME-SMOKE/20260706-113849/Dump-Analysis/dump-analysis-summary.txt`
- `docs/debug/evidence/GAME-SMOKE/20260706-113849/Dump-Analysis/MISSING-WINDBG-README.txt`

Because the dump was not decoded, service isolation proceeded with boundary evidence plus live dump hash, without a decoded native stack.

The first UiRuntime root isolation run used evidence:

- `docs/debug/evidence/GAME-SMOKE/20260706-131450`

Classification:

- UiRuntime isolation did not eliminate the fatal.
- The fatal window shifted compared with the Phase 8.14 pre-SaveLoaded sample.
- This run reached `SaveLoaded`, completed the Lite SaveLoaded object snapshot, completed runtime notify, and reached `SaveLoaded.Step=Hook.Exit`.
- The final DTMAPI log position was `SaveLoaded.Step=Hook.Exit ... totalMemory=778035200 ...`.
- The fatal popup was detected about 0.94 seconds later.
- A new DbgHelp full live dump was captured:
  - dump size: `4,972,791,628` bytes
  - SHA256: `9E9780BF1258C670E307BFA6A36BBD59E72752CF554090ECFAD54CAA3CBF0ADE`

Decision: stop this phase after the shifted window. Do not continue into a broad service-disable matrix from this sample. The next phase should analyze the post-SaveLoaded, pre-native-return LoadGame continuation and the fresh dump/crash evidence before choosing the next isolation axis.

## Implementation Changes

### Dump Analysis Script

Added `tools/scripts/analyze-process-dump.ps1`.

The script:

- accepts `-DumpPath` and `-OutputDir`;
- records dump path, size, SHA256, debugger discovery, timing, exit code, and stack-family summary fields;
- searches for `cdb.exe` / `windbg.exe` through PATH, common Windows Kits locations, and Visual Studio/vswhere discovery;
- writes `MISSING-WINDBG-README.txt` instead of failing when no debugger is available.

Local result for the 8.14 dump:

```text
DumpPath=E:\Python_project\DTMAPI\docs\debug\evidence\GAME-SMOKE\20260706-113849\Process-Dumps\DolocTown-52784-fatal-live-dbghelp.dmp
DumpSize=4883437290
DumpSha256=8F3DC0BD8E5C1CE1DED66EC67F9B3BBFBC889137F0972FBD4032DABC8C417B5F
DebuggerFound=False
CrashThreadGuess=unknown
HasMonoFrames=unknown
HasTerrainFrames=unknown
HasLoadGameFrames=unknown
HasDtmapiFrames=unknown
HasBepInExFrames=unknown
HasHarmonyFrames=unknown
TopStackSummary=missing-debugger
```

### Smoke Root Isolation Profile

Added smoke-only `-SmokeRootIsolationProfile None|UiRuntime`, default `None`.

`UiRuntime` disables DTMAPI-owned UI/runtime helper roots by skipping registration, update, hook/status publication, or render paths where safe:

- `NativeUiLayoutDiagnostics`
- `SaveSlots`
- `EquipmentSlots`
- `Camera`

`DebugConsoleHost` is not safely disableable in the current boundary and is reported as unsupported:

```text
SmokeRootIsolation.Unsupported=DebugConsoleHost
```

Evidence lines from runtime:

```text
SmokeRootIsolationProfile=UiRuntime
SmokeRootIsolation.Disabled=NativeUiLayoutDiagnostics|SaveSlots|EquipmentSlots|Camera
SmokeRootIsolation.Unsupported=DebugConsoleHost
SmokeRootIsolation.Active=ActionCompletion|ActionSpeed|AnimalViewer|AudioReplacement|ChestLocatorEnhancer|CropHarvesting|CustomAnimalAnimatorBridge|FishingAutomation|FishRoeTooltip|OilCoalDrop|StrongPlantingGun|DebugConsoleHost(unsupported-not-disabled)
```

EquipmentSlots hook/status evidence:

```text
Player.EquipmentSlotsApi = smoke-isolated
Player.EquipmentSlotsShield = smoke-isolated
```

### Dump README Hash Fallback

During the runtime sample, the smoke harness captured the DbgHelp full dump but then failed while writing `TOO-LARGE-DUMP-README.txt` because `Get-FileHash` was unavailable in that child PowerShell context. This left no generated `result.json` and skipped automatic profile restore.

After the run, `tools/scripts/run-game-smoke.ps1` was patched so `Write-TooLargeDumpReadme` falls back to .NET SHA256 hashing when `Get-FileHash` is missing or fails. This is smoke-only and does not change player runtime behavior.

Manual recovery evidence for this run:

- `RESULT-MISSING-README.txt`
- `manual-result-reconstruction.json`
- `manual-fatal-cleanup-note.txt`
- `manual-official-mod-profile-restore.txt`
- reconstructed `Process-Dumps/process-dump-summary.txt`
- reconstructed `Process-Dumps/TOO-LARGE-DUMP-README.txt`

## Runtime Command

The run used the requested first-isolation command shape:

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
- broad service-disable matrix
- unknown native Unity object destruction

## Profile Validation

`official-mod-profile-summary.json` for `GAME-SMOKE/20260706-131450` was valid:

- `Profile=CoreCustomAnimals`
- `Applied=true`
- `ExtraEnabledIds` exactly matched the requested nine full-profile IDs.
- Enabled IDs included the nine extra IDs plus expected CoreCustomAnimals local animal packs: ShellCrab, HatchAssets, OilfloaterAssets, MoleAssets, and DreckoAssets.
- `Local.Yuuka_DTMAPI_AutoFishing` was disabled.

Because the smoke harness exited while writing the dump README, automatic profile restore did not run. The profile was manually restored from:

```text
docs/debug/evidence/GAME-SMOKE/20260706-131450/official-mod-profile.mod_infos.before.json
```

Manual restore evidence:

```text
manual-official-mod-profile-restore.txt
ManualOfficialModProfileRestored=True
```

After cleanup, `DolocTown.exe` was not running and the runtime lock was free.

## Root Isolation Evidence

At title after the first ReturnedToTitle boundary, UiRuntime reduced the stable service/root surface:

```text
featureCount=11
activeFeatures=11
NativeUiLayoutDiagnostics absent
SaveSlots absent
Camera absent
Equipment rendered=False
equipmentClones=0
equipmentBinders=0
saveUiBinders=0
ModOwner.records=96
EventHandler=19
InputButton=9
LoadedCodeMod=10
ConfigMenuPage=9
```

The remaining active feature list was:

```text
ActionCompletion
ActionSpeed
AnimalViewer
AudioReplacement
ChestLocatorEnhancer
CropHarvesting
CustomAnimalAnimatorBridge
FishingAutomation
FishRoeTooltip
OilCoalDrop
StrongPlantingGun
```

This confirms the run was not merely a mod-manifest owner split. It disabled the intended GameBridge/UI runtime service roots while leaving the full feature/content profile otherwise intact.

## Fatal Window

The run held title for one hour, started the first post-idle LoadGame, reached SaveLoaded, then fataled before native LoadGame returned.

Key DTMAPI order:

```text
14:15:18.036 BeforeNextLoadGame snapshot 9, mode=Lite
14:15:18.040 LoadGameNativeEnter snapshot 11, mode=Lite, requestId=SL-0001
14:15:18.040 LoadGame requested for slot/index 2. requestId=SL-0001.
14:15:20.044 SaveLoaded.Step=Hook.Enter
14:15:20.048 SaveLoaded.Step=Hook.AfterGameBridgeFeatureDispatch
14:15:20.049 SaveLoaded.Step=Hook.BeforeRuntimeNotifySaveLoaded
14:15:20.075 SaveLoaded.Step=Runtime.BeforeObjectSnapshot
14:15:20.077 SaveLoaded.Step=Runtime.AfterObjectSnapshot
14:15:20.077 SaveLoaded.Step=Runtime.Exit
14:15:20.077 SaveLoaded.Step=Hook.AfterRuntimeNotifySaveLoaded
14:15:20.079 SaveLoaded.Step=Hook.AfterMarkSmoke
14:15:20.079 SaveLoaded.Step=Hook.Exit
```

Fatal window live evidence:

```text
FatalWindowDetectedAt=2026-07-06T14:15:21.0204961+08:00
ProcessId=34588
MainWindowTitle=Fatal error in GC
Text=Unexpected mark stack overflow
```

Last DTMAPI line:

```text
SaveLoaded.Step=Hook.Exit elapsedMs=35 gc0=232 gc1=232 gc2=232 totalMemory=778035200 requestId=SL-0001 boundaryId=TR-0001 slot=2 phase=SaveLoaded.
```

SaveLoad state inferred from DTMAPI evidence:

```text
requests=1
nativeEnter=1
nativeReturn=0
saveLoaded=1
duplicateRequests=0
```

Because `result.json` was not generated, this run must be read through `manual-result-reconstruction.json` plus the DTMAPI log and fatal-window evidence.

## Crash Evidence

Fresh Unity crash evidence was captured after manual close/post-close collection:

```text
Unity-Crashes/Crash_2026-07-06_061918817
Player.log bytes=849750
crash.dmp bytes=1774101
```

DbgHelp full live process dump:

```text
Process-Dumps/DolocTown-34588-fatal-live-dbghelp.dmp
DumpSize=4972791628
DumpSha256=9E9780BF1258C670E307BFA6A36BBD59E72752CF554090ECFAD54CAA3CBF0ADE
CapturedMode=DbgHelpFull
```

Routine packages should include `Process-Dumps/TOO-LARGE-DUMP-README.txt` instead of the `.dmp`.

## Interpretation

This sample is not a pass, and it is not the same pre-SaveLoaded terrain/dungeon fatal class as Phase 8.14.

It shows:

- `UiRuntime` service/root isolation is not sufficient to make the profile stable.
- Disabling UiRuntime changed the fatal timing to post-SaveLoaded / pre-native-return.
- The fatal still occurs before native LoadGame return.
- The remaining suspect surface is now the native LoadGame continuation after SaveLoaded, plus content/native-heavy roots that remained active: AudioReplacement platform players, CustomAnimals/AnimalVoice registrations, AnimalViewer, action/utility hooks, and the native `AfterLoadArchiveData` / map/texture path.

Do not use this run to claim a specific UiRuntime owner is root cause. Also do not use it to launch an immediate broad service-disable matrix. The correct next move is to analyze the shifted window and the fresh dump/crash evidence, then choose one narrow isolation axis.

## Validation

Before runtime:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings.
- `git diff --check`: passed with line-ending normalization warnings only.
- No existing `DolocTown.exe` before launch.

After runtime and manual cleanup:

- No leftover `DolocTown.exe`.
- Runtime lock free.
- Profile restored manually from the smoke backup.
- `tools/scripts/run-game-smoke.ps1` parser check passed again after the hash fallback patch.
- `tools/scripts/test.ps1 -Configuration Release` passed again after the patch.
- `git diff --check` passed again with line-ending normalization warnings only.

## Non-Changes

This phase did not:

- change public API;
- do a GameBridge large refactor;
- run PreLoad GC;
- run `SaveLoadObjectSnapshotMode=Off`;
- resume UI owner/pair split;
- run a broad service-disable matrix;
- destroy unknown native `GameObject`, `Component`, `AudioClip`, `AssetBundle`, `RuntimeAnimatorController`, or Unity shell objects;
- change ordinary player runtime behavior.

## Next Step

Recommended next phase:

```text
Phase 8.16 Post-SaveLoaded Native LoadGame Continuation Analysis
```

Start from the new shifted-window evidence:

- Decode or externally analyze the 8.15 DbgHelp dump if a debugger is available.
- Compare 8.11 / 8.13 / 8.15 post-SaveLoaded windows.
- Inspect Unity crash `Crash_2026-07-06_061918817` and `Player.log`.
- Decide whether the first narrow isolation should target AudioReplacement, CustomAnimals/AnimalVoice, AnimalViewer, or action/utility hooks.
- Keep `UiRuntime` as a demonstrated timing amplifier/change, not a proven root cause.
