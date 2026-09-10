# Phase 8.7 UI Owner Split And PreLoad GC Probe

Date: 2026-07-05 +08:00
Status: source-and-runtime-smoke-verified / issue-010-open
Branch observed: `codex/dtmapi-overall-refactor-20260702`
Scope: ISSUE-010 Phase 8.7 single UI owner split, high-value UI pair split, and smoke-only PreLoad forced-GC probe.

## Summary

Phase 8.7 added one small diagnostic and then ran the requested continuous one-hour title-idle UI split matrix on the fixed base:

- Base B: `CoreCustomAnimals` plus action/utility mods plus Manbo audio.
- AutoFishing disabled by official profile.
- Save slot 3 / index 2.
- Each main matrix run used `1h title idle -> LoadGame -> ReturnHome -> LoadGame`, with two native loads maximum.

Result: no single UI owner and no requested high-value pair reproduced `Fatal error in GC / Unexpected mark stack overflow`. Each run had exactly two SaveLoad requests, two native enters, two native returns, two SaveLoaded closures, zero duplicate requests, and zero fatal windows.

This moves ISSUE-010 into the plan's situation C. The current evidence does not support a single DTMAPI UI owner leak or a high-value pair interaction as the direct trigger. The next useful step is native/root-set crash analysis on a full failing profile, keeping DTMAPI owner ledgers as the boundary map.

## Implementation

Added a smoke-only switch:

- `tools/scripts/run-game-smoke.ps1 -AutoExercisePreLoadGcProbe`

The switch is rejected unless `-AutoExerciseSaveLoadCycle` is also enabled. When enabled, the smoke harness records:

- `BeforePreLoadForcedGC` boundary event and object snapshot.
- `GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();`
- `PreLoadForcedGC` boundary event and object snapshot.

The probe runs only once, before the first post-idle LoadGame in a SaveLoadCycle run. It does not run before the second LoadGame after ReturnHome.

Existing per-owner keys remain the Phase 8.7 reading surface:

- `ModOwner.byOwner.*`
- `OwnerRoots.input.byOwner.*`, `OwnerRoots.events.byOwner.*`, `OwnerRoots.configPages.byOwner.*`
- `UI.byOwner.*`
- `BootstrapUi.titleSettings.rootAlive`
- `BootstrapUi.debugConsole.rootAlive`
- `GameBridge.featureById.*`

Key mapping note: the user-requested `Bootstrap.TitleSettings.rootAlive` and `Bootstrap.DebugConsole.rootAlive` are represented in current logs as `BootstrapUi.titleSettings.rootAlive` and `BootstrapUi.debugConsole.rootAlive`, with matching owner UI views under `UI.byOwner.DTMAPI.TitleSettings.rootAlive` and `UI.byOwner.DTMAPI.DebugConsole.rootAlive`.

## Matrix Evidence

All main matrix runs used the base IDs:

- `Workshop.3742763309`
- `Workshop.3742763843`
- `Workshop.3742763540`
- `Workshop.3742763706`
- `Local.Yuuka_DTMAPI_ManboCardboardAudio`

All `official-mod-profile-summary.json` files were checked. The target UI IDs were enabled and AutoFishing was not enabled in each run.

| Profile | Evidence | Result | Native owner counts at LoadGameNativeEnter | Target owner counts | Delta result |
| --- | --- | --- | --- | --- | --- |
| B + YConsole | `docs/debug/evidence/GAME-SMOKE/20260705-101737` | Passed | `ModOwner.records=98`, `EventHandler=24`, `InputButton=4`, `ConfigPage=7`, `LoadedCodeMod=10` | `DTMAPI.DebugConsoleMod={records=10, EventHandler=4, InputButton=2, ConfigPage=1, LoadedCodeMod=1}` | `LoadGameNativeEnter` and `AfterReturnedToTitleComplete` ownerPrevDelta had no root growth beyond SaveLoad request/lifecycle counters. |
| B + MoreEquipmentSlots | `docs/debug/evidence/GAME-SMOKE/20260705-111900` | Passed | `ModOwner.records=93`, `EventHandler=21`, `InputButton=2`, `ConfigPage=7`, `LoadedCodeMod=10` | `DTMAPI.MoreEquipmentSlotsMod={records=5, EventHandler=1, InputButton=0, ConfigPage=1, LoadedCodeMod=1}` | No owner root growth. In-save equipment entries returned to zero at title. |
| B + MoreSaves | `docs/debug/evidence/GAME-SMOKE/20260705-122003` | Passed | `ModOwner.records=92`, `EventHandler=20`, `InputButton=2`, `ConfigPage=7`, `LoadedCodeMod=10` | `DTMAPI.MoreSavesMod={records=4, EventHandler=0, InputButton=0, ConfigPage=1, LoadedCodeMod=1}` | No owner root growth. SaveSlots UI binders stayed zero at title. |
| B + Zoom | `docs/debug/evidence/GAME-SMOKE/20260705-132240` | Passed | `ModOwner.records=100`, `EventHandler=23`, `InputButton=7`, `ConfigPage=7`, `LoadedCodeMod=10` | `DTMAPI.ZoomMod={records=12, EventHandler=3, InputButton=5, ConfigPage=1, LoadedCodeMod=1}` | No owner root growth. |
| B + YConsole + MoreEquipmentSlots | `docs/debug/evidence/GAME-SMOKE/20260705-142438` | Passed | `ModOwner.records=103`, `EventHandler=25`, `InputButton=4`, `ConfigPage=8`, `LoadedCodeMod=11` | `DTMAPI.DebugConsoleMod=10/4/2/1/1`; `DTMAPI.MoreEquipmentSlotsMod=5/1/0/1/1` | No owner root growth. |
| B + YConsole + MoreSaves | `docs/debug/evidence/GAME-SMOKE/20260705-152600` | Passed | `ModOwner.records=102`, `EventHandler=24`, `InputButton=4`, `ConfigPage=8`, `LoadedCodeMod=11` | `DTMAPI.DebugConsoleMod=10/4/2/1/1`; `DTMAPI.MoreSavesMod=4/0/0/1/1` | No owner root growth. |
| B + MoreSaves + MoreEquipmentSlots | `docs/debug/evidence/GAME-SMOKE/20260705-162708` | Passed | `ModOwner.records=97`, `EventHandler=21`, `InputButton=2`, `ConfigPage=8`, `LoadedCodeMod=11` | `DTMAPI.MoreSavesMod=4/0/0/1/1`; `DTMAPI.MoreEquipmentSlotsMod=5/1/0/1/1` | No owner root growth. |

Every row ended with:

- `RunStatus=Passed`
- `SaveLoadCycle=Passed`
- `NoFatalInstanceWindow=Passed`
- `ProcessExited=Passed`
- `requests=2`
- `nativeEnter=2`
- `nativeReturn=2`
- `saveLoaded=2`
- `duplicateRequests=0`
- `fatalWindows=0`

## PreLoad GC Probe Evidence

Short probe validation:

- Evidence: `docs/debug/evidence/GAME-SMOKE/20260705-101446`
- Command shape: `CoreOnly`, `SaveLoadCycleCount=1`, `SaveLoadCycleInitialTitleIdleSeconds=5`, `-AutoExercisePreLoadGcProbe`.
- Result: `RunStatus=Passed`, `SaveLoadCycle=Passed`, `PreLoadForcedGCProbe=Passed`, `NoFatalInstanceWindow=Passed`, `ProcessExited=Passed`.
- Log confirmed `BeforePreLoadForcedGC` and `PreLoadForcedGC` snapshots before native LoadGame.

The main Phase 8.7 matrix intentionally did not use the probe, keeping it comparable to the known failing evidence. Because no single or pair profile reproduced, there was no same-profile fatal rerun where the probe could classify title-stable GC failure versus native LoadGame-triggered failure.

## Interpretation

The tested UI owners have stable roots, but those roots did not grow across the two-load one-hour-idle runs:

- YConsole/DebugConsole contributes event, input, and config roots.
- Zoom contributes the largest input-root count among single UI owners.
- MoreEquipmentSlots contributes one event root and one config page; its save-owned equipment state cleared on title return.
- MoreSaves contributes config ownership; SaveSlots UI binder roots remained zero at title.

The failing profile from `docs/debug/evidence/GAME-SMOKE/20260705-055655` had a larger stable graph (`ModOwner.records=119`, `EventHandler=28`, `InputButton=9`) and failed at first post-idle native LoadGame. The single/pair runs stayed below that stable root size and did not fail.

Current conclusion:

- Excluded in this matrix: single-owner YConsole, MoreEquipmentSlots, MoreSaves, Zoom as sufficient causes; the three requested high-value pairs as sufficient causes; duplicate LoadGame; AutoFishing participation.
- Still suspect: the full stable root-set threshold, all-UI/full-profile combination pressure, native Unity title scene/root graph after uninterrupted idle, and native LoadGame's additional root activation before `SaveLoaded`.
- Next validation: reproduce on the full failing profile, collect Unity crash dump and `Unity-Crashes/summary.txt`, then compare the last `BeforeNextLoadGame`, optional `PreLoadForcedGC`, and `LoadGameNativeEnter` snapshots.

## Source And Community Checks

- Unity managed memory documentation says native objects are unloaded only when no references point to them, and `Resources.UnloadUnusedAssets` also triggers managed collection. This supports count-first root attribution and argues against blindly destroying unknown native objects: [Unity managed memory introduction](https://docs.unity3d.com/6000.4/Documentation/Manual/performance-managed-memory-introduction.html).
- A Unity discussion notes `UnloadUnusedAssets` traverses GameObject hierarchy and static variables. This matches the Phase 8.7 focus on static owner roots, UI roots, and process-long Bootstrap/GameBridge roots: [Resources.UnloadUnusedAssets execution time discussion](https://discussions.unity.com/t/resources-unloadunusedassets-execution-time-slowly-increases-over-time/920692).
- Unity documents `UnityEvent.RemoveListener` for runtime listener cleanup. This supports treating Button/UnityEvent listeners and dynamic binders as owner roots that must be counted and removed when DTMAPI owns them: [UnityEvent.RemoveListener](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Events.UnityEvent.RemoveListener.html).
- Unity community reports for `Unexpected mark stack overflow` describe crashes from large or active managed object graphs rather than normal managed exceptions. This supports native/root-set escalation after owner deltas stay bounded: [GC crash Unexpected mark stack overflow](https://discussions.unity.com/t/gc-crash-unexpected-mark-stack-overflow/853176).
- BepInEx documents Unity runtime patching through HarmonyX, and Harmony documents unpatching by owner. This supports treating Harmony patches as process-long roots and cleaning DTMAPI-owned delegates/state instead of title/save-time unpatch churn: [BepInEx runtime patching](https://docs.bepinex.dev/articles/dev_guide/runtime_patching.html), [Harmony basics unpatching](https://harmony.pardeike.net/articles/basics.html).
- Unity `MonoBehaviour.OnDestroy` is tied to object/scene/application destruction. Bootstrap `DontDestroyOnLoad`-style roots therefore need explicit DTMAPI lifecycle reset; title return alone cannot be assumed to destroy them: [MonoBehaviour.OnDestroy](https://docs.unity3d.com/2021.1/Documentation/ScriptReference/MonoBehaviour.OnDestroy.html).

## Validation

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings.
- `git diff --check`: passed before runtime smokes with line-ending normalization warnings only.
- Runtime lock was acquired and released for every game smoke.
- No leftover `DolocTown.exe` after the matrix.

## Follow-Up

1. Stop UI single/pair guessing for Phase 8.7.
2. Run the full known-failing profile with crash-dump collection and the existing DTMAPI ledger.
3. If the full profile reproduces, rerun the same profile once with `-AutoExercisePreLoadGcProbe` to classify forced-GC-on-title versus native-LoadGame activation.
4. Inspect Unity crash dump, `Unity-Crashes/summary.txt`, `Player.log`, and the last DTMAPI `BeforeNextLoadGame` / `PreLoadForcedGC` / `LoadGameNativeEnter` snapshots.
5. Only fix concrete DTMAPI-owned stale roots or duplicate subscriptions. Keep CustomAnimals, AnimalVoice, AudioReplacement, AssetBundle/controller, and unknown native Unity objects on count-first diagnostics until ownership is proven.
