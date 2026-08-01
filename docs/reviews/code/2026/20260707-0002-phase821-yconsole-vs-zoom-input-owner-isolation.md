# Phase 8.21 YConsole vs Zoom Input Owner Isolation

Date: 2026-07-07 +08:00
Status: runtime-evidence-captured / ZoomNoInput passed twice / YConsoleNoInput passed twice / issue-010-open
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: ISSUE-010 single-owner input-root split inside the already reproduced `YConsole + Zoom` pair.

## Summary

Phase 8.21 split the Phase 8.20 `YConsole + Zoom` input-root suspect by owner. The feature profile stayed unchanged from the light diagnostic route:

- no `-IncludeHookProbe`;
- no PreLoad GC;
- `SaveLoadObjectSnapshotMode=Lite`;
- `SmokeRootIsolationProfile=UiRuntime`;
- `SmokeNativeLoadContinuationProbe=VersionPatcher`;
- continuous `3600s` title idle;
- max two LoadGames;
- AutoFishing, MoreSaves, and MoreEquipmentSlots disabled;
- YConsole and Zoom both enabled.

The only changed control variable was `SmokeOwnerRootIsolationProfile`:

| Profile | Evidence | Target owner | Removed roots | Result |
| --- | --- | --- | --- | --- |
| `ZoomNoInput` | `GAME-SMOKE/20260707-054818` | `DTMAPI.ZoomMod` | `InputButton=5` | passed |
| `ZoomNoInput` repeat | `GAME-SMOKE/20260707-065046` | `DTMAPI.ZoomMod` | `InputButton=5` | passed |
| `YConsoleNoInput` | `GAME-SMOKE/20260707-075645` | `DTMAPI.DebugConsoleMod` | `InputButton=2` | passed |
| `YConsoleNoInput` repeat | `GAME-SMOKE/20260707-085818` | `DTMAPI.DebugConsoleMod` | `InputButton=2` | passed |

All four valid runs completed:

```text
RunStatus=Passed
SaveLoadCycle=Passed
NoFatalInstanceWindow=Passed
ProcessExited=Passed
requests=2
nativeEnter=2
nativeReturn=2
saveLoaded=2
duplicateRequests=0
fatalWindows=0
```

Classification: under the Phase 8.18-8.20 light diagnostic route, removing either side of the `YConsole + Zoom` input-root island was enough for two clean-process passes. This keeps runtime `InputButton` roots as the strongest current pressure suspect, but does not prove a player-runtime fix. The suppression is smoke-only, both owner services still exist, and the valid runs used `DirectExe` fallback after the Steam launch route was stale in this session.

## Runtime Shape

Every valid Phase 8.21 run used:

```text
SaveSlot=3
IncludeHookProbe=False
AutoExerciseSaveLoadCycle=True
SaveLoadCycleCount=2
SaveLoadCycleInitialTitleIdleSeconds=3600
SaveLoadCycleIntervalSeconds=5
SaveLoadCycleInSaveSeconds=5
AutoExercisePreLoadGcProbe=False
SaveLoadObjectSnapshotMode=Lite
SmokeRootIsolationProfile=UiRuntime
SmokeNativeLoadContinuationProbe=VersionPatcher
FatalWindowProcessDumpMode=DbgHelpFull
FatalWindowPostCloseCrashDumpWaitSeconds=60
LaunchMode=DirectExe
```

The enabled profile was `CoreCustomAnimals` plus:

```text
Workshop.3742763309
Workshop.3742763843
Workshop.3742763540
Workshop.3742763706
Local.Yuuka_DTMAPI_ManboCardboardAudio
Workshop.3742714442
Workshop.3742717440
```

`Workshop.3744059735` / MoreEquipmentSlots, `Workshop.3742763050` / MoreSaves, and `Local.Yuuka_DTMAPI_AutoFishing` were disabled in every `official-mod-profile-summary.json`.

## Owner Suppression Evidence

`ZoomNoInput` removed only the Zoom runtime input roots:

```text
profile=ZoomNoInput
targetOwners=DTMAPI.ZoomMod
targetRootTypes=InputButton
removed={InputButton=5; EventHandler=0; ConfigPage=0}
before={DTMAPI.ZoomMod={InputButton=5; EventHandler=3; ConfigPage=1; LoadedCodeMod=1}}
after={DTMAPI.ZoomMod={InputButton=0; EventHandler=3; ConfigPage=1; LoadedCodeMod=1}}
suppressionSucceeded=True
unexpectedRemainingSuppressedRoots=none
unexpectedDisabledCodeOwners=none
```

`YConsoleNoInput` removed only the DebugConsole runtime input roots:

```text
profile=YConsoleNoInput
targetOwners=DTMAPI.DebugConsoleMod
targetRootTypes=InputButton
removed={InputButton=2; EventHandler=0; ConfigPage=0}
before={DTMAPI.DebugConsoleMod={InputButton=2; EventHandler=4; ConfigPage=1; LoadedCodeMod=1}}
after={DTMAPI.DebugConsoleMod={InputButton=0; EventHandler=4; ConfigPage=1; LoadedCodeMod=1}}
suppressionSucceeded=True
unexpectedRemainingSuppressedRoots=none
unexpectedDisabledCodeOwners=none
```

This means the smoke-only control did not remove `LoadedCodeMod`, config pages, or event-handler roots for the target owner. It isolated input roots without turning the owner into a disabled-code profile.

## Interpretation

The Phase 8.19 pair profile reproduced twice before SaveLoaded with YConsole and Zoom together. Phase 8.20 removed all seven input roots for the pair and passed twice. Phase 8.21 now shows both single-owner removals also passed twice:

| Profile | Runtime YConsole input roots | Runtime Zoom input roots | Result |
| --- | ---: | ---: | --- |
| 8.19 `YConsole + Zoom` pair | 2 | 5 | reproduced |
| 8.20 `YConsoleZoomNoInput` | 0 | 0 | passed twice |
| 8.21 `ZoomNoInput` | 2 | 0 | passed twice |
| 8.21 `YConsoleNoInput` | 0 | 5 | passed twice |

The best current reading is threshold pressure rather than a single owner proven in isolation:

```text
Both sides' input roots contribute to the pair's pressure island. Removing either side drops the tested route below the observed failure threshold.
```

This is not strong enough to claim that either owner is individually defective, because the full baseline remains intermittent and the pair fatal is a native LoadGame terrain/dungeon activation crash before SaveLoaded. The next useful step should inspect the actual input registration/callback lifetime for both owners and design a player-safe input-root lifetime reduction, not hard-disable services.

## Validation

Validation for the current 8.20/8.21 source state:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed before the Phase 8.20/8.21 run set.
- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed again after the Phase 8.21 docs/evidence updates.
- `tools/scripts/test.ps1 -Configuration Release`: initial direct test execution built successfully but failed to launch the `net8.0` unit-test exe because the host has .NET 6 and .NET 9 runtimes but not `Microsoft.NETCore.App 8.0.0`; rerunning with `DOTNET_ROLL_FORWARD=Major` passed with `DTMAPI.UnitTests: OK`.
- `git diff --check`: passed with line-ending normalization warnings only.

Runtime validation:

- runtime lock was acquired and released for each runtime operation;
- no leftover `DolocTown.exe` after the run set;
- every run restored the official profile;
- `owner-root-type-isolation-summary.txt` recorded `SuppressionSucceeded=True`;
- `SmokeRootIsolationProfile=UiRuntime` and `SmokeNativeLoadContinuationProbe=VersionPatcher` were active;
- `SaveLoadObjectSnapshotMode=Lite` was active;
- final native continuation reached `DolocAPI.LoadGame.Exit` on the second load in every run;
- `managed-root-isolation-summary.txt` and `ui-pair-decomposition-summary.txt` are not expected for this phase because the control is owner-root-type isolation, not code-root removal or pair decomposition.

## Non-Changes

This phase did not:

- change source code after the Phase 8.20 owner-root isolation implementation;
- change public API;
- change ordinary player runtime behavior;
- run PreLoad GC;
- run `SaveLoadObjectSnapshotMode=Off`;
- run HookProbe;
- run service hard-disable;
- run UI triples;
- run content/native-heavy isolation;
- destroy unknown native `GameObject`, `Component`, `AudioClip`, `AssetBundle`, controller, or Unity shell objects;
- mark ISSUE-010 solved.

## Next Step

Do not return to UI pair bisection. The next phase should inspect or instrument the input-root lifetime itself:

```text
DTMAPI.DebugConsoleMod input registrations/callbacks
DTMAPI.ZoomMod input registrations/callbacks
input root lifetime across title idle and SaveLoad
whether title-idle input callbacks can be made lazy, title-bounded, or weakly rebound without changing user-visible controls
```

Any player-facing fix should preserve YConsole and Zoom behavior. A safe candidate would reduce long-lived input roots or stale callback captures, then rerun the original reproducing `YConsole + Zoom` pair profile without smoke suppression.
