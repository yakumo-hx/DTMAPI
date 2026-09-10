# Phase 8.6 SaveLoad Cycle Accumulation Audit

Date: 2026-07-05 +08:00

Status: source verified, per-owner deltas added, short/no-idle and 10-minute-cadence cycles verified, one-hour-continuous-idle feature bisection reproduced the Fatal GC class

Related issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`

## Scope

This pass continues the ISSUE-010 investigation after duplicate `LoadGame` requests were ruled out. The focus is the object graph that exists after title idle, repeated save entry, `ReturnHome`, and the next native `LoadGame`.

The pass does not copy SMAPI code, names, protocols, or resources. SMAPI remains background architecture reference only.

## Instrumentation Added

- Added a bounded per-cycle object delta ledger to `TitleReturnBoundaryLedgerService`.
- Recorded deltas at `BeforeReturnHome`, `AfterReturnedToTitleComplete`, `BeforeNextLoadGame`, `LoadGameNativeEnter`, and `SaveLoaded`.
- Added `SaveLoadCycleObjectDeltaLedger` and `SaveLoadCycleObjectDeltaSummary` smoke/report fields.
- Extended object graph snapshots with Bootstrap lifecycle and GameBridge lifecycle sections.
- Added smoke feature profiles: `CoreOnly`, `CoreUi`, `CoreCustomAnimals`, `CoreAnimalVoice`, `CoreAutoFishing`, and `FullKnown`-style extras.
- Added a small DTMAPI-owned cleanup for stale diagnostic `SaveLifetime` records in the resource lifecycle ledger.
- Added per-owner object-root diagnostics for ModOwner records/kinds, owner-bound input, event handlers, config pages, Bootstrap UI, SaveSlots UI, EquipmentSlots UI, and GameBridge feature status/runtime states.
- Added `ownerPrevDelta`, `ownerSameBoundaryDelta`, and `ownerNonZero` to object-delta formatting so owner metrics are visible at `BeforeNextLoadGame` and `LoadGameNativeEnter`.

## Lifecycle Classification

| Family | Classification | Evidence / Reasoning |
| --- | --- | --- |
| `DtmApiRuntime`, Bootstrap plugin, GameBridge service objects | process-long owner/service | BepInEx plugin objects are expected to survive scene changes; they are the owners that publish diagnostics and bridge APIs. |
| Harmony patches and patch metadata | process-long hook roots | Harmony patches are static patch methods and Harmony-owned method metadata. Do not unpatch broadly during title/save transitions. |
| Content pack metadata, mod registry rows, owner ledger definitions | process/content lifetime config | These should remain stable after startup/title refresh and are not per-save transients. |
| Custom animal registrations, AI template registrations, PNG sprite definitions | process/content lifetime config | They implement the official JSON + player PNG/WAV + DTMAPI JSON path. Do not destroy native templates blindly. |
| AudioReplacement content entries and platform WAV players | process/content lifetime service state | Stable replacement definitions are expected. Runtime `AudioClip`, pending request, async operation, callback owner, and animal context counts must return to zero. |
| SaveSlots UI states/pagers/binders | ReturnedToTitle transient | Must be zero after title return. Latest smokes show zero in pass and fail profiles. |
| EquipmentSlots entries/storage/clones/binders | ReturnedToTitle transient and SaveLoaded rebind | Must be released on title return and rebound on save load. Latest smokes show zero at title. |
| AnimalViewer native rows/overlay objects | ReturnedToTitle transient | Native data and overlay rows must be zero outside active panel use. Latest smokes show zero. |
| AutoFishing native handles, minigame handles, ready charge, animator, hook physics, pending cast | ReturnedToTitle transient | Latest solo and combined passes show zero; AutoFishing is excluded from the minimal failing profile. |
| Debug Console active slot/session, Title Settings UI roots, fallback EventSystem | SaveLoaded rebind / Shutdown native UI cleanup | Active save slot appears in-save and clears at title. UI roots are DTMAPI-owned and are cleanup targets at shutdown/title boundaries. |
| ResourceLifecycle `SaveLifetime` diagnostics | save-owned diagnostic state | Stale older save generations are DTMAPI-owned diagnostics and now pruned after later save-generation close. |
| Unknown Doloc Town native GameObjects and native save-load objects | suspect/native-owned | Count first. Do not destroy without ownership proof. |

## Smoke And Bisection Evidence

| Evidence | Profile / Route | Result | Key finding |
| --- | --- | --- | --- |
| `docs/debug/evidence/GAME-SMOKE/20260704-184517` | current/default, no one-hour idle, 20 SaveLoad cycles | passed | Loop count alone did not reproduce; `requests=20`, `duplicateRequests=0`. |
| `docs/debug/evidence/GAME-SMOKE/20260704-185211` | current/default, one-hour idle, then cycle route | failed | Reproduced on the third native load after idle; duplicate requests still zero. |
| `docs/debug/evidence/GAME-SMOKE/20260704-201341` | `CoreOnly`, one-hour idle, 6 cycles | passed | Core runtime alone did not reproduce after idle. |
| `docs/debug/evidence/GAME-SMOKE/20260704-214043` | `CoreCustomAnimals`, one-hour idle, 4 cycles | passed | CustomAnimals plus bundled AnimalVoice did not reproduce alone. |
| `docs/debug/evidence/GAME-SMOKE/20260704-225729` | `CoreAutoFishing`, one-hour idle, 4 cycles | passed | AutoFishing alone did not reproduce. |
| `docs/debug/evidence/GAME-SMOKE/20260705-001613` | `CoreUi`, one-hour idle, 4 cycles | passed | UI group alone did not reproduce. |
| `docs/debug/evidence/GAME-SMOKE/20260705-013446` | `CoreOnly`, short 3-cycle after ResourceLifecycle prune | passed | Stale `SaveLifetime` diagnostics prune works; records no longer accumulate across save generations. |
| `docs/debug/evidence/GAME-SMOKE/20260705-013648` | current/default after ResourceLifecycle prune, one-hour idle | failed | Pruning diagnostic records is not the root cause; full/current still reproduced. |
| `docs/debug/evidence/GAME-SMOKE/20260705-023843` | `CoreOnly` plus action/utility group, one-hour idle | passed | Action/utility group alone did not reproduce. |
| `docs/debug/evidence/GAME-SMOKE/20260705-034457` | CustomAnimals plus action/utility, one-hour idle | passed | CustomAnimals/AnimalVoice plus action/utility did not reproduce. |
| `docs/debug/evidence/GAME-SMOKE/20260705-045059` | CustomAnimals plus action/utility plus Manbo audio, one-hour idle | passed | The eleventh audio replacement / Manbo audio was not sufficient. |
| `docs/debug/evidence/GAME-SMOKE/20260705-055655` | CustomAnimals plus action/utility plus Manbo plus UI, one-hour idle | failed | Minimal confirmed repro group for this pass. AutoFishing was disabled. |
| `docs/debug/evidence/GAME-SMOKE/20260705-075428` | Minimal reproduced profile, no-idle 20-cycle rerun after per-owner deltas | passed | `requests=20`, `nativeEnter=20`, `nativeReturn=20`, `saveLoaded=20`, `duplicateRequests=0`; owner delta fields are present. |
| `docs/debug/evidence/GAME-SMOKE/20260705-080223` | Minimal reproduced profile, 10-minute title idle then 5-minute cadence for 20 cycles | passed | `initialIdleSeconds=600`, `intervalSeconds=300`, `elapsedSeconds=6535`, `requests=20`, `nativeEnter=20`, `nativeReturn=20`, `saveLoaded=20`, `duplicateRequests=0`; interrupted title idle did not reproduce. |

## Object Delta Findings

Clear transients at title in pass and fail profiles:

- SaveSlots: `saveUiStates=0`, `saveUiPagers=0`, `saveUiBinders=0`.
- EquipmentSlots: `equipmentEntries=0`, `equipmentClones=0`, `equipmentBinders=0`, `equipmentStorageOwners=0`.
- AnimalViewer: `nativeData=0`, `nativeRows=0`, `overlayObjects=0`, `overlayRows=0`.
- AudioReplacement runtime transients: `audioClips=0`, `pendingRequests=0`, `asyncOperations=0`, `callbackOwners=0`, `animalContexts=0`.
- CustomAnimals native cache transients: `controllerCache=0`, `nonNullControllers=0`, `bundleCache=0`, `nonNullBundles=0`.
- AutoFishing native transients: `nativeTransientHandles=0`, `miniGameHandles=0`, `readyChargeStates=0`, `animators=0`, `hookPhysics=0`, `pendingCast=False`.

Stable but large object families in the minimal failing profile:

- `ModOwner.records=119`, including `EventHandler=28`, `InputButton=9`, `LoadedCodeMod=13`.
- `ResourceLifecycle.records=54`, mostly title lifetime diagnostics plus 2 save lifetime release records.
- GameBridge features/status/runtime states: 14 each.
- AudioReplacement: `entries=11`, `contentPackEntries=10`, `loadStarted=11`, `readyEntries=11`, `platformPlayers=11`, `states=6`.
- CustomAnimals: `registrations=10`, `aiTemplates=5`, `pngSprites=4`.

Per-owner roots visible after this follow-up:

- `DTMAPI.DebugConsoleMod`: `EventHandler=4`, `InputButton=2`, `ConfigPage=1`, `LoadedCodeMod=1`.
- `DTMAPI.ZoomMod`: `EventHandler=3`, `InputButton=5`, `ConfigPage=1`, `LoadedCodeMod=1`.
- `DTMAPI.MoreEquipmentSlotsMod`: `EventHandler=1`, `ConfigPage=1`, `LoadedCodeMod=1`; EquipmentSlots UI entries appear in-save and return to zero at title.
- `DTMAPI.MoreSavesMod`: `ConfigPage=1`, `LoadedCodeMod=1`; SaveSlots UI pager/binder counts remain zero at title in latest runs.
- `Yuuka.DTMAPI.ActionSpeed`: `EventHandler=4`, `InputButton=1`, `ConfigPage=1`, `LoadedCodeMod=1`.

The failing `20260705-055655` run crashed on first post-idle native `LoadGame` before `SaveLoaded`: `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, `duplicateRequests=0`. A separate current/default run after the resource-ledger prune (`20260705-013648`) reached `SaveLoaded` but not native return, so the fatal window can land either just before or just after the managed save-loaded boundary.

## Conclusion

The crash is not explained by SaveLoad cycle count alone: 20 no-idle cycles passed. It is also not explained by core runtime idle alone: `CoreOnly` passed after one-hour idle and repeated loads.

The strongest current classification is: one-hour title idle plus a larger process/title object graph is needed, and the smallest reproduced feature set in this pass is `CustomAnimals/AnimalVoice + action/utility + Manbo audio + UI`. AutoFishing is not required for this reproduction.

No DTMAPI-owned per-save/title transient family was found growing without bound in the latest ledgers. The remaining suspect is interaction between stable title/process roots and Unity/Mono GC root pressure, especially the UI group added on top of custom animals/audio/action utilities.

The 10-minute initial idle plus five-minute cadence pass changes the timing classification: cumulative wall-clock time with repeated SaveLoad interruptions is not sufficient in the tested window. The next UI split should use the continuous one-hour title-idle route, because interrupting title idle every five minutes avoided the crash for 20 cycles.

## Fixed In This Pass

- Per-cycle object delta ledger added and exported through smoke/report context.
- Feature-profile bisection support added to `run-game-smoke.ps1`, with backup/restore of `SAVE/mod_infos.json`.
- Stale DTMAPI-owned `ResourceLifecycle` `SaveLifetime` diagnostic records from older save generations are pruned.
- Per-owner root/delta summaries added for ModOwner, input, events, config pages, Bootstrap UI, SaveSlots, EquipmentSlots, and GameBridge features.

## Source And Community Checks

- Unity Memory Profiler managed shell docs: <https://docs.unity3d.com/Packages/com.unity.memoryprofiler@1.1/manual/managed-shell-objects.html>. Supports clearing managed references and counting Unity object shells before destroying native objects.
- Unity `UnityEvent` docs: <https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Events.UnityEvent.html>. Supports tracking runtime listener roots and removing only DTMAPI-owned dynamic listeners.
- Unity `Object.Destroy` docs: <https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Object.Destroy.html>. Supports avoiding blind native destruction because actual destruction is deferred and ownership matters.
- Unity Discussions mark stack overflow thread: <https://discussions.unity.com/t/fatal-error-in-gc-unexpected-mark-stack-overflow/760479>. Supports treating fatal mark-stack overflow as object-graph/root-pressure evidence, not a normal managed exception.
- Unity Issue Tracker large GameObject crash report: <https://issuetracker.unity3d.com/issues/the-player-crashes-on-unexpected-mark-stack-overflow-without-a-stacktrace-when-creating-a-large-amount-of-game-objects-and-the-il2cpp-scripting-backend-is-selected>. Supports counting GameObject/UI graph families when the fatal lacks a useful stack trace.
- Unity event leak discussions: <https://discussions.unity.com/t/c-question-about-events/557817> and <https://discussions.unity.com/t/question-regarding-classes-and-memory-leaks/875871>. Supports event/input owner counters as a first-class retention signal.
- Harmony basics and patching docs: <https://harmony.pardeike.net/articles/basics.html> and <https://harmony.pardeike.net/articles/patching.html>. Supports treating patches as process-long roots and scoping any future unpatching by Harmony ID.
- BepInEx `BaseUnityPlugin` API and plugin tutorial: <https://docs.bepinex.dev/v6.0.0-pre.1/api/BepInEx.BaseUnityPlugin.html> and <https://docs.bepinex.dev/master/articles/dev_guide/plugin_tutorial/2_plugin_start.html>. Supports Bootstrap/plugin lifecycle being a Unity `MonoBehaviour` root.
- BepInEx plugin manager lifetime issue: <https://github.com/BepInEx/BepInEx/issues/420>. Supports classifying plugin manager/bootstrap objects as scene-independent long-lived roots.
- HarmonyX unpatch issue: <https://github.com/BepInEx/HarmonyX/issues/5>. Supports using Harmony ID-scoped cleanup only if shutdown unpatching is ever required.
- Harmony patch edge-case docs: <https://harmony.pardeike.net/articles/patching-edgecases.html>. Supports keeping patch ownership explicit and avoiding broad title/save unpatching.

## Suspect List

| Suspect | Evidence | Community/source alignment | Next validation |
| --- | --- | --- | --- |
| Continuous title idle | One-hour continuous title-idle routes reproduce; 10-minute initial idle plus five-minute SaveLoad cadence passed for 20 cycles and `elapsedSeconds=6535`. | Unity mark-stack overflow reports support root pressure from long-lived object graphs; the cadence pass suggests the uninterrupted title scene state is important. | Run UI-owner split with the continuous one-hour title-idle route, not the interrupted cadence route. |
| UI group on top of CustomAnimals/AnimalVoice/action/Manbo | Custom+action+Manbo passed, adding Zoom/MoreSaves/MoreEquipment/YConsole reproduced before `SaveLoaded`. | Unity event/UI root guidance and mark-stack overflow reports support UI graph pressure as plausible. | Add Zoom, MoreSaves, MoreEquipment, and DebugConsole/YConsole one at a time to the passing Custom+action+Manbo profile under continuous one-hour idle. |
| Stable owner/event/input graph | Failing profile has `ModOwner.records=119`, `EventHandler=28`, `InputButton=9`; per-owner deltas now show DebugConsole, Zoom, MoreEquipmentSlots, MoreSaves, and ActionSpeed roots separately. | Unity event leak discussions support event/delegate roots retaining object graphs. | Watch `ownerPrevDelta`, `ownerSameBoundaryDelta`, and `ownerNonZero` at `BeforeNextLoadGame` and `LoadGameNativeEnter` during each UI split. |
| Stable AudioReplacement + platform WAV players with UI group | AudioReplacement stable counts are large but not growing; Manbo did not fail without UI. | Unity object-shell docs support keeping counts visible while not destroying unknown/native assets blindly. | Keep count-first diagnostics; only clear DTMAPI-owned request/callback/transient objects. |
| Native Doloc Town save-load object graph | Fatal occurs inside native/Mono GC and sometimes before DTMAPI `SaveLoaded`. DTMAPI transients are bounded. | Unity mark-stack overflow reports show fatal can occur without managed stack trace from large object graphs. | If UI split does not isolate one DTMAPI owner, collect crash dump/root-set or Unity Memory Profiler-style object counts outside DTMAPI ownership. |

## Rollback Notes

- The ledger and smoke profile changes are diagnostic-only and can be disabled by reverting this pass.
- The resource-ledger prune only removes old DTMAPI-owned string diagnostic records after a later save generation closes; it does not touch native game objects, content packs, PNG/WAV assets, `AudioClip`, `AssetBundle`, or `RuntimeAnimatorController` objects.
