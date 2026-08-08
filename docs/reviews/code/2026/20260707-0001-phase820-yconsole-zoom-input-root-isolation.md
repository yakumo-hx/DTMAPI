# Phase 8.20 YConsole + Zoom Input Root Isolation

Date: 2026-07-07 +08:00
Status: source-and-runtime-evidence-captured / NoInput passed twice under DirectExe fallback / issue-010-open
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: ISSUE-010 root-type isolation inside the already reproduced `YConsole + Zoom` pair.

## Summary

Phase 8.20 stopped mod-owner combination bisection and isolated root type inside the `YConsole + Zoom` pair. The route stayed aligned with the Phase 8.18/8.19 light diagnostic setup:

- no `-IncludeHookProbe`;
- no PreLoad GC;
- `SaveLoadObjectSnapshotMode=Lite`;
- `SmokeRootIsolationProfile=UiRuntime`;
- `SmokeNativeLoadContinuationProbe=VersionPatcher`;
- `SmokeOwnerRootIsolationProfile=YConsoleZoomNoInput`;
- continuous `3600s` title idle;
- max two LoadGames;
- AutoFishing, MoreSaves, and MoreEquipmentSlots disabled.

The valid evidence consists of two clean-process `DirectExe` fallback runs:

- `docs/debug/evidence/GAME-SMOKE/20260707-000526`
- `docs/debug/evidence/GAME-SMOKE/20260707-011257`

Both runs suppressed only the pair's `InputButton` roots and kept the code owners, event handlers, and config pages present:

```text
targetOwners=DTMAPI.DebugConsoleMod|DTMAPI.ZoomMod
targetRootTypes=InputButton
removed={InputButton=7; EventHandler=0; ConfigPage=0}
before={DTMAPI.DebugConsoleMod={InputButton=2; EventHandler=4; ConfigPage=1; LoadedCodeMod=1}; DTMAPI.ZoomMod={InputButton=5; EventHandler=3; ConfigPage=1; LoadedCodeMod=1}}
after={DTMAPI.DebugConsoleMod={InputButton=0; EventHandler=4; ConfigPage=1; LoadedCodeMod=1}; DTMAPI.ZoomMod={InputButton=0; EventHandler=3; ConfigPage=1; LoadedCodeMod=1}}
suppressionSucceeded=True
unexpectedRemainingSuppressedRoots=none
unexpectedDisabledCodeOwners=none
```

Both valid runs then passed two post-idle LoadGames with no fatal window. Classification: under this light diagnostic route, `YConsole + Zoom` `InputButton` roots are now a strong suspect / pressure amplifier. This is still not a player-runtime fix, because the suppression is smoke-only and the valid runs used `DirectExe` fallback after Steam launch attempts stalled.

## Implementation

Phase 8.20 added smoke-only owner root-type isolation:

- `tools/scripts/run-game-smoke.ps1`
  - added `-SmokeOwnerRootIsolationProfile`;
  - writes `SmokeOwnerRootIsolationProfile` into settings/result;
  - creates `owner-root-type-isolation-summary.txt`;
  - appends runtime evidence from `Smoke.OwnerRootIsolation` log lines.
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
  - added `SmokeOwnerRootIsolationProfile` settings support;
  - applies owner-root isolation only after the runtime has loaded enough owner roots for meaningful suppression;
  - gates automation if the requested isolation profile cannot be applied within the startup window.
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
  - added `ApplySmokeOwnerRootIsolationProfileForSmoke`;
  - removes only selected DTMAPI-owned runtime roots for smoke isolation:
    - `Input.RemoveOwner(owner)`;
    - `Events.RemoveOwner(owner)`;
    - `configMenuRuntime.RemoveOwner(owner)`;
  - preserves `LoadedCodeMod` / code-owner registrations for the target owners.
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
  - stores smoke-only owner-root isolation state.

Supported profiles at this phase:

```text
YConsoleZoomNoInput
YConsoleZoomNoEvents
YConsoleZoomNoInputEvents
YConsoleZoomNoConfig
ZoomNoInput
YConsoleNoInput
```

Only `YConsoleZoomNoInput` was run in Phase 8.20.

## Invalid / Control Attempts

Three earlier attempts are retained as setup/instrumentation evidence and excluded from ISSUE-010 root-cause conclusions:

| Evidence | Launch | Result | Reason excluded |
| --- | --- | --- | --- |
| `GAME-SMOKE/20260706-234314` | Steam | no `result.json` | Steam launch did not reach a usable game runtime. |
| `GAME-SMOKE/20260706-235527` | Steam | no `result.json` | Steam launch did not reach a usable game runtime. |
| `GAME-SMOKE/20260706-235733` | DirectExe | no final `result.json` | Pre-gate implementation applied suppression before owner roots were registered; `suppressionSucceeded=False` and `unexpectedDisabledCodeOwners=DTMAPI.DebugConsoleMod|DTMAPI.ZoomMod`. |

After `SmokeHarness` was changed to apply the profile during early smoke update after owner roots exist, both valid runs applied the profile correctly.

## Valid Runtime Evidence

Both valid runs used:

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
SmokeOwnerRootIsolationProfile=YConsoleZoomNoInput
FatalWindowProcessDumpMode=DbgHelpFull
FatalWindowPostCloseCrashDumpWaitSeconds=60
```

The enabled feature profile was the Phase 8.19 pair profile: `CoreCustomAnimals` plus the five non-UI base extras and only the `YConsole + Zoom` UI pair:

```text
Workshop.3742763309
Workshop.3742763843
Workshop.3742763540
Workshop.3742763706
Local.Yuuka_DTMAPI_ManboCardboardAudio
Workshop.3742714442
Workshop.3742717440
```

`Workshop.3744059735` / MoreEquipmentSlots, `Workshop.3742763050` / MoreSaves, and `Local.Yuuka_DTMAPI_AutoFishing` were disabled.

| Evidence | Launch | Suppression | Result |
| --- | --- | --- | --- |
| `20260707-000526` | DirectExe fallback | removed 7 input buttons; events/config/code owners retained | passed |
| `20260707-011257` | DirectExe fallback | removed 7 input buttons; events/config/code owners retained | passed |

SaveLoad result for both runs:

```text
RunStatus=Passed
SaveLoadCycle=Passed
NoFatalInstanceWindow=Passed
ProcessExited=Passed
DuplicateLoadRequests=Passed
requests=2
nativeEnter=2
nativeReturn=2
saveLoaded=2
duplicateRequests=0
fatalWindows=0
```

The final native continuation breadcrumb reached normal `DolocAPI.LoadGame.Exit` on the second load. Unity crash evidence was `stale-only`; no fresh fatal was produced.

## Comparison

| Profile | Fatal | Runtime input buttons for YConsole + Zoom | Event handlers retained | Config pages retained | Loaded code owners retained |
| --- | --- | ---: | ---: | ---: | ---: |
| Phase 8.19 `YConsole + Zoom` pair | yes | 7 | 7 | 2 | 2 |
| Phase 8.20 `YConsoleZoomNoInput` run 1 | no | 0 | 7 | 2 | 2 |
| Phase 8.20 `YConsoleZoomNoInput` run 2 | no | 0 | 7 | 2 | 2 |

The `ModOwner` ledger can still retain audit records for owners and historical registrations. The Phase 8.20 conclusion is about runtime owner roots (`InputButton`) being removed while the code owners, event handlers, and config pages remain.

## Classification

Current strongest conclusion:

```text
Suppressing only YConsole + Zoom InputButton roots made the previously reproducing pair profile pass twice under the same light diagnostic route.
```

This makes the pair's input-button root island a strong suspect / pressure amplifier. It does not prove a final fix because:

- the suppression is smoke-only and not player behavior;
- the valid evidence used `DirectExe` fallback because the Steam launch path was stale in this session;
- baseline fatal behavior is intermittent across related phases.

The next phase should split the input root suspect by owner:

```text
ZoomNoInput
YConsoleNoInput
```

If one single-owner input suppression passes twice while the other reproduces, inspect that owner's input registration and input callback path. If both singles pass, treat their combined input roots as threshold pressure and plan a player-safe input root lifetime change rather than service hard-disable. If either single fatals before SaveLoaded with the same stack, that owner's input roots are not necessary for that recurrence.

## Validation

Source validation before the valid runs:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings.
- `git diff --check`: passed with line-ending normalization warnings only.

Runtime validation:

- runtime lock was acquired and released;
- no leftover `DolocTown.exe` after each valid run;
- profiles were restored;
- `owner-root-type-isolation-summary.txt` recorded `SuppressionSucceeded=True`;
- `SmokeRootIsolationProfile=UiRuntime` and `SmokeNativeLoadContinuationProbe=VersionPatcher` were active;
- `SaveLoadObjectSnapshotMode=Lite` was active.

## Non-Changes

This phase did not:

- change public API;
- change ordinary player runtime behavior;
- run PreLoad GC;
- run `SaveLoadObjectSnapshotMode=Off`;
- run UI triples;
- run service hard-disable;
- run content/native-heavy isolation;
- destroy unknown native `GameObject`, `Component`, `AudioClip`, `AssetBundle`, controller, or Unity shell objects;
- mark ISSUE-010 solved.
