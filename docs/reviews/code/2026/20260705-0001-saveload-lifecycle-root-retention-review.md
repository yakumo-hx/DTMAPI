# 20260705-0001 SaveLoad Lifecycle Root-Retention Review

## Scope

This review follows the user request to apply a targeted SaveLoad lifecycle code review and not launch the game. The review checks what DTMAPI creates, registers, loads, or roots across `LoadGame` / `ReturnHome`, then classifies whether the matching cleanup happens on `SaveLoaded`, `ReturnedToTitle`, or shutdown.

Hard boundaries:

- No Doloc Town launch.
- No game smoke.
- No runtime lock.
- No runtime code edits.
- This is a review conclusion only; it is not proof that ISSUE-010 is fixed.

Input reviewed:

- User-provided review method in the Codex attachment: registration roots, static caches, UI roots, coroutine/task/timer roots, Harmony/native callbacks, and lifecycle pairing.
- Required project context: `PROJECT.md`, `docs/planning/DolocTownModdingAPI.md`, `docs/planning/Debug.md`, `references/README.md`, `docs/debug/INDEX.md`, `docs/reviews/README.md`.
- Recent debug/update context: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`, `docs/debug/regressions/smoke-matrix.md`, `docs/reviews/code/2026/20260704-0002-project-object-lifecycle-audit.md`, and recent 2026-07-04 update records.

The working tree already contained uncommitted runtime/diagnostic changes before this review. Those files were treated as the current reviewed source, but this review does not modify them.

## Baseline Facts

- ISSUE-010 remains open. The latest documented long cycle after the 2026-07-04 lifecycle sweep reached the fifth native `LoadGame` enter after four `ReturnHome` closures, then reproduced `Fatal error in GC / Unexpected mark stack overflow`.
- That long cycle did not show duplicate save-load requests: `requests=5`, `nativeEnter=5`, `nativeReturn=4`, `saveLoaded=4`, `duplicateRequests=0`, `suppressedDuplicates=0`, `timeouts=0`.
- The latest title-return ledger before abort was at `LoadGameNativeEnter` for boundary `TR-0005`, with no runtime-recorded fatal event. That keeps the suspect window at title-stable / next-native-LoadGame, not at an obvious duplicated DTMAPI `LoadGame` dispatch.
- Native reference metadata keeps the expected lifecycle points stable across recent builds: `DolocAPI.LoadGame(int)`, `DolocAPI.AfterLoadArchiveData(bool)`, `DolocAPI.ReturnHome(bool)`, `DolocTown.GameData.DataPersistenceManager.LoadGame(int)`, and related native `AfterLoadArchiveData` receivers.

## Findings

### P2 - Runtime fatal correlation can miss the final fatal boundary

`DtmApiRuntime.NotifySaveLoadFatalWindowObserved` only records a `FatalWindowObserved` event and object graph snapshot when that runtime method is called (`src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:370`). The latest long failure was caught by the external smoke/fatal-window check with `fatalEvents=0` and coordinator `fatalWindows=0`, so the runtime ledger did not mark the final object graph as fatal.

This is not a cleanup bug by itself, but it is the largest evidence gap for the current investigation. The new `SaveLoad cycle object delta` log is useful (`src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:1634`), but if the process aborts before runtime can publish `FatalWindowObserved`, the final conclusion must be made by correlating the last emitted delta line with the external fatal package.

Conclusion: keep ISSUE-010 open. Do not read `fatalEvents=0` as proof that the final boundary was clean.

### P2 - Harmony static callback roots are not actually cleared on shutdown

`HarmonyReflectionPatcher` stores several static delegates:

- `arrayResultPostfixCallback`
- `animatorAssetTryLoadAssetCallback`
- `animalAIDefaultAnyStateCallback`
- `animalAIMakeDecisionFreeTimeCallback`
- `spriteOverrideTryGetModOverrideSpriteCallback`

They are declared at `src/DTMAPI.GameBridge.DolocTown/Hooking/HarmonyReflectionPatcher.cs:16` and assigned from feature services at `:134`, `:179`, `:229`, `:282`, and `:508`. A source search found no clear/reset/unpatch method for these fields.

`DolocTownGameBridge.Shutdown` stops retry sources, unsubscribes the DTMAPI-owned `SaveLoaded` UnityEvent listener, and clears `DolocTownHookCallbacks.Runtime` / `Bridge` (`src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs:314`). The hook status text says shutdown "released DTMAPI static hook roots" (`:331`), but the `HarmonyReflectionPatcher` static delegate roots remain live until process exit.

This does not explain per-`LoadGame` accumulation because the feature hook bridges are guarded by `*Patched` flags and install once. It is still a real shutdown-contract mismatch and a possible root-retention issue for plugin reload, shutdown hang analysis, or final log-export interpretation.

### P2 - Third and later `SaveLoaded` phases collapse back to `SaveLoaded`

`NotifySaveLoaded` classifies the phase as:

- `SecondSaveLoaded` only when `observedSaveLoadedCount == 1`
- `SaveLoaded` for the first and all third-plus loads

See `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:385` and the matching `ObserveLifecycle` call at `:402`.

That means long-cycle diagnostics cannot distinguish first save load from the third, fourth, or fifth `SaveLoaded` using `currentRuntimePhase` alone. Existing debug records already mention repeated SaveLoaded classification warnings. This is a telemetry bug, not a proven cleanup bug, but it weakens root-cause review exactly where ISSUE-010 now lives.

### P3 - Object-delta telemetry is useful, but still text-derived and partial

The reviewed source adds `SaveLoadCycleObjectDeltaLedger` output through the title-return object graph snapshots. This is the right direction because it compares feature/root counts across `BeforeNextLoadGame`, `LoadGameNativeEnter`, `SaveLoaded`, and `AfterReturnedToTitleComplete`.

Limitations:

- The extractor parses scalar `key=value` text summaries. Complex list-like or nested values can be missed or flattened.
- The "same boundary" comparison is based on boundary name; repeated snapshot names inside one boundary can become intra-boundary comparisons rather than cross-cycle comparisons.
- The smoke parser looks for either hook status or `SaveLoad cycle object delta` log lines; if the fatal is external, the final runtime status may lag behind the last direct log line.

Conclusion: use the delta ledger as a narrowing tool, not as final proof. The final long-run package must preserve the last object-delta log before the fatal popup/process exit.

## Lifecycle Pairing Matrix

| Area | Created / rooted on load or runtime | Cleanup or boundary observed | Review conclusion |
| --- | --- | --- | --- |
| Bootstrap fallback pump | Process-long `Timer` and runtime coroutine (`BootstrapPlugin.cs:158`, `:219`). | Disposed on `OnApplicationQuit` (`:397`). | Not a per-load leak candidate. |
| Bootstrap event lambdas | `SaveSessionLoaded` and `ReturnedToTitleBoundary` lambdas are added once during `Awake` (`BootstrapPlugin.cs:61`). | Same `DtmApiRuntime` lifetime; no per-load registration loop. | Acceptable process-lifetime root. |
| Title settings UI | `DontDestroyOnLoad` root plus dynamic panel children/listeners (`ReflectedTitleMenuSettingsUi.cs:201`, `:223`). | `ResetBoundaryState` clears panel, binders, input values, fallback EventSystem on save/title boundaries (`:1285`); shutdown destroys root/sprite (`:1727`). | Pairing looks correct after the 2026-07-04 cleanup. |
| Debug console UI | `DontDestroyOnLoad` root and dynamic panel/listeners (`ReflectedDebugConsoleUi.cs:356`, `:373`). | Close/title/shutdown paths destroy owned EventSystem and clear binders/input fields (`:137`, `:146`, `:232`, `:605`). | Pairing looks correct; persistent root is process-level. |
| GameBridge hook scheduler | `AssemblyLoad` subscription and retry timer on initialize (`DolocTownGameBridge.cs:307`). | `StopHookRetrySources` disposes timer and removes `AssemblyLoad` (`DolocTownGameBridge.Hooks.cs:301`). | Cycle-safe; shutdown static callback gap remains separate. |
| SaveLoaded UnityEvent listener | Adds listener to `DolocAPI.OnAfterLoadArchiveData` (`DolocTownGameBridge.cs:931`). | Removes listener on shutdown (`:960`). | Not repeated per LoadGame due `saveLoadedEventSubscribed` guard. |
| Harmony patches | Installed through guarded `*Patched` fields (`DolocTownGameBridge.Hooks.cs:9`; feature hook bridges). | Not unpatched during title/save boundaries, by design. | No duplicate patch loop found. Static callback clear is missing on shutdown. |
| SaveSlots | Official save UI binders/pagers tracked. | `SaveLoaded` and `ReturnedToTitle` clear UI states (`SaveSlotsFeature.cs:68`, `:74`; `SaveSlotsService.cs:90`). | Pairing looks correct. |
| EquipmentSlots | Cloned slots, binders, storage session state. | Save/title clear UI lifecycle and reset session state (`DolocTownExperimentalBridgeApi.EquipmentSlots.cs:16`, `:37`, `:48`, `:76`). | Pairing looks correct. |
| AnimalViewer | Native data rows and overlay objects. | `ResetAnimalViewerRuntimeState` clears overlays/native data on save/title (`AnimalViewerFeature.cs:55`, `:60`; `AnimalViewerService.cs:204`). | Pairing looks correct. |
| FishingAutomation | Native object keyed states, mini-game handles, input/animator/physics snapshots. | `ResetFishingRuntimeState` clears save/title state (`FishingAutomationFeature.cs:55`, `:60`; `FishingAutomationService.cs:2035`). | Pairing looks correct. |
| CustomAnimals save state | Per-save `ConditionalWeakTable` contexts and diagnostic sets. | `ClearSaveLifetimeState` rebuilds weak tables and clears per-save diagnostics (`CustomAnimalAnimatorBridgeService.cs:207`). | Save-lifetime pairing looks correct. |
| CustomAnimals title/process caches | AssetBundle and animator controller caches retained by registration key/path (`CustomAnimalAnimatorBridgeService.cs:1274`, `:1347`). | Stale cache entries are removed when definitions disappear (`:392`, `:399`), but live registration caches persist across title return. | Intentional title-lifetime cache; remains a high-value delta suspect, not a proven leak. |
| AudioReplacement save state | Animal sound context stack. | `ClearSaveLifetimeState` clears contexts on save/title (`AudioReplacementService.cs:209`). | Save-lifetime pairing looks correct. |
| AudioReplacement title/process state | Entries, clips, requests, callback owners, platform players (`AudioReplacementService.cs:63`). | Requests/platform players are disposed when entries are replaced/removed (`:1926`, `:1942`); regular save/title keeps content definitions alive. | Intentional title-lifetime content state; platform-player count should be checked in long deltas. |
| Resource lifecycle ledger | Tracks owner/content/save lifetime records. | Current dirty source prunes older closed save-lifetime records during save-generation close. | Positive bounded-ledger change; it does not prove Unity/native objects were collected. |

## Rejected Or Not-Proven Hypotheses

- Duplicate DTMAPI `LoadGame` requests are not supported by the latest evidence: duplicate counters were zero in the reproduced long cycle.
- A repeated UI `AddListener` loop is not supported by the current title settings/debug console code. Dynamic binders are tied to dynamic UI objects and cleared on panel rebuild/title/save/shutdown.
- A repeated Harmony patch install loop is not supported by the guarded `*Patched` fields in the bridge and feature hook bridges.
- DTMAPI-owned feature save-lifetime state has obvious cleanup boundaries in the reviewed source. The remaining risk is title/process-lifetime caches or native/Unity-owned graphs that code review cannot prove.

## Review Conclusion

The targeted code review does not find a direct "LoadGame creates/registers X every cycle and ReturnHome never releases X" smoking gun in the DTMAPI-owned lifecycle paths.

The strongest source-level conclusions are:

1. The current failure window should stay narrowed to title-return/title-stable-to-next-native-`LoadGame`, with external fatal correlation required.
2. DTMAPI cleanup pairing for UI, SaveSlots, EquipmentSlots, AnimalViewer, FishingAutomation, and save-lifetime CustomAnimals/AudioReplacement state now looks mostly correct at source level.
3. Two implementation follow-ups are worth doing before the next long game run: clear `HarmonyReflectionPatcher` static callback delegates on shutdown, and fix multi-cycle `SaveLoaded` phase naming.
4. The remaining runtime suspects are title/process-lifetime object families, especially CustomAnimals AssetBundle/controller caches, AudioReplacement platform players/clips/requests, and native Doloc Town graphs around the next `LoadGame` enter. These require object-delta evidence, not another broad code-only pass.

## Validation

- Game was not launched.
- Runtime lock was not acquired.
- No build, unit test, or smoke test was run.
- Review was source/docs only.
