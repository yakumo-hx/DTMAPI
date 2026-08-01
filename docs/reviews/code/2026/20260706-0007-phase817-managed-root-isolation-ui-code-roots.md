# Phase 8.17 Managed Root-Set Isolation: UI Code Roots

Date: 2026-07-06 +08:00
Status: runtime-evidence-captured / no-UI-code-roots passed twice / issue-010-open
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: ISSUE-010 UI code mod owner-root isolation before content/native-heavy service isolation.

## Summary

Phase 8.17 followed the external review request to stop chasing the `VersionPatcher` continuation path and instead isolate managed UI code roots.

The runtime profile kept the Phase 8.16 light diagnostics and native continuation probe shape, but removed the four UI code mods from the official enabled set:

- `Workshop.3742714442` / `DTMAPI.DebugConsoleMod`
- `Workshop.3744059735` / `DTMAPI.MoreEquipmentSlotsMod`
- `Workshop.3742763050` / `DTMAPI.MoreSavesMod`
- `Workshop.3742717440` / `DTMAPI.ZoomMod`

Two clean-process runs passed after a continuous `3600s` title idle:

- `docs/debug/evidence/GAME-SMOKE/20260706-163322`
- `docs/debug/evidence/GAME-SMOKE/20260706-174103`

Both ended with `requests=2`, `nativeEnter=2`, `nativeReturn=2`, `saveLoaded=2`, `fatalWindows=0`, `RunStatus=Passed`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`.

Classification: removing the UI code mod owners is the strongest pressure-reduction signal so far, but it is not proof of a fix because the full baseline is intermittent. Treat the four UI code roots as a strong suspect/amplifier and use the next phase to choose a narrower confirmation axis instead of declaring ISSUE-010 solved.

## Runtime Shape

Both runs used:

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
- extra enabled IDs: `Workshop.3742763309`, `Workshop.3742763843`, `Workshop.3742763540`, `Workshop.3742763706`, and `Local.Yuuka_DTMAPI_ManboCardboardAudio`;
- `-FatalWindowCrashDumpGraceSeconds 30`;
- `-FatalWindowProcessDumpMode DbgHelpFull`;
- `-FatalWindowPostCloseCrashDumpWaitSeconds 60`.

The runs intentionally did not use:

- HookProbe;
- PreLoad GC probe;
- `SaveLoadObjectSnapshotMode=Off`;
- UI owner/pair bisection;
- service hard-disable;
- unknown native Unity object destruction.

## Profile Validation

Both `official-mod-profile-summary.json` files were valid:

- `Profile=CoreCustomAnimals`
- `Applied=true`
- `ExtraEnabledIds` exactly matched the five non-UI IDs above.
- Enabled IDs included the expected CoreCustomAnimals local animal packs.
- `Local.Yuuka_DTMAPI_AutoFishing` was disabled.
- The four UI Workshop IDs were disabled.
- The profile was restored after each run.

The DTMAPI logs also showed the UI code mods being skipped:

```text
Skipping DTMAPI.DebugConsoleMod
Skipping DTMAPI.MoreEquipmentSlotsMod
Skipping DTMAPI.MoreSavesMod
Skipping DTMAPI.ZoomMod
```

`DTMAPI.DebugConsoleHost` remained as an unsupported host/API owner under `UiRuntime`; that is not the same as the disabled `DTMAPI.DebugConsoleMod` code owner.

## Owner / Root Counters

Both runs reported the same reduced managed owner/root shape:

| Metric | 20260706-163322 | 20260706-174103 |
| --- | ---: | ---: |
| `ModOwner.records` | 65 | 65 |
| `EventHandler` | 11 | 11 |
| `InputButton` | 2 | 2 |
| `ConfigPage` / `ConfigMenuPage` | 5 | 5 |
| `LoadedCodeMod` | 6 | 6 |
| Unexpected UI code owners remaining | none | none |

The retained non-UI owner roots were:

```text
OwnerRoots.input.byOwner={Yuuka.DTMAPI.ActionSpeed=1; Yuuka.DTMAPI.OneActionComplete=1}
OwnerRoots.events.byOwner={Yuuka.DTMAPI.ActionSpeed=4; Yuuka.DTMAPI.AnimalHusbandryProgress=2; Yuuka.DTMAPI.FishBreedingAssistant=2; Yuuka.DTMAPI.OneActionComplete=3}
OwnerRoots.configPages.byOwner={Yuuka.DTMAPI.ActionSpeed=1; Yuuka.DTMAPI.AnimalHusbandryProgress=1; Yuuka.DTMAPI.FishBreedingAssistant=1; Yuuka.DTMAPI.ManboCardboardAudio=1; Yuuka.DTMAPI.OneActionComplete=1}
```

For comparison, Phase 8.16 full-profile `UiRuntime + VersionPatcher` fatal still had the four UI code mods enabled and fataled before SaveLoaded. Phase 8.17 kept the same content/audio/action family and diagnostic mode but removed the four UI code roots; two clean-process samples passed.

## Snapshot / Continuation Evidence

Both runs verified `Lite` at the two pre-load boundaries:

```text
BeforeNextLoadGameLiteSeen=True
LoadGameNativeEnterLiteSeen=True
```

Both installed the requested continuation probe:

```text
SmokeNativeLoadContinuationProbe=VersionPatcher
AfterLoadArchiveDataHook=installed
VersionPatcherLoadAllHook=installed
VersionPatcherLoadBeyondHook=installed
MapManagerInitHook=installed
TextureUtilsDrawAreaHook=unsupported-skipped-high-frequency
```

Because both runs passed, the last continuation breadcrumb was the normal second-load exit:

```text
NativeContinuation.Step=DolocAPI.LoadGame.Exit ... requestId=SL-0002 ... nativeEnter=2 nativeReturn=1 saveLoaded=2
```

The final SaveLoad health snapshot closed the second load normally:

```text
requests=2
nativeEnter=2
nativeReturn=2
saveLoaded=2
duplicateRequests=0
fatalWindows=0
last=NativeReturn:SL-0002
```

## Validation

Before the rerun gate:

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings for nuget.org vulnerability metadata.
- `git diff --check`: passed with line-ending normalization warnings only.
- No `DolocTown.exe` was running.

After runtime:

- runtime lock was released;
- `tools/scripts/runtime-lock-status.ps1` reported free;
- no leftover `DolocTown.exe`;
- profile restore was recorded in each `summary.txt`.

## Classification

This phase does not prove the GC crash is fixed. It does prove that, under the Phase 8.16 diagnostic shape, removing the four UI code mod owners reduced the stable managed owner/root set from the prior UI-enabled profiles and produced two consecutive pass samples.

That makes the UI code roots a strong suspect/amplifier, especially compared with the earlier repeated fatal windows in the UI-enabled full profile. The remaining uncertainty is baseline intermittency: two passes are strong evidence, not a mathematical proof.

Recommended next step:

- do not return to UI owner/pair bisection;
- do not jump directly to broad service hard-disable as if this were already solved;
- run a narrow confirmation that preserves this no-UI-code-root profile and adds back one managed-root family, or choose the next external review's requested content/native-heavy axis.

## Non-Changes

This phase did not:

- change source code or public API;
- change ordinary player runtime behavior;
- run PreLoad GC;
- run `SaveLoadObjectSnapshotMode=Off`;
- destroy unknown native Unity objects;
- run a broad service-disable matrix;
- claim ISSUE-010 solved.
