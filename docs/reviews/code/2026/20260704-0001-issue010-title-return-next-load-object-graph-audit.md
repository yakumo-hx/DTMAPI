# ISSUE-010 Title-Return / Next-Load Object Graph Audit

Date: 2026-07-04 +08:00  
Scope: code-level/root-cause review only. No runtime install, no game launch, and no long smoke was run for this review.  
Status: root cause not fully proven; highest-confidence narrowed class is a title-return-to-next-native-LoadGame Unity/native object graph boundary failure.

## Executive Conclusion

ISSUE-010 should no longer be treated as a broad "long idle" or "duplicate LoadGame" problem. The latest 2026-07-04 failure evidence narrows the current leftover to this chain:

1. DTMAPI reaches title, or reaches title after one successful save/load cycle.
2. A new native `LoadGame` starts.
3. The process hits Unity/Mono `Fatal error in GC / Unexpected mark stack overflow`.
4. The run never reaches the native `LoadGame` postfix or DTMAPI `SaveLoaded` dispatch for that request.

In code terms: `DolocTownHookCallbacks.LoadGamePrefix` records native enter, but the failure happens before `DolocTownHookCallbacks.LoadGamePostfix` and before `AfterLoadArchiveDataPostfix` can close the request through `DtmApiRuntime.NotifySaveLoaded` (`src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs:30`, `:35`, `:40`; `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:262`, `:281`, `:349`).

The most defensible conclusion is therefore:

> Some Unity/native/managed object graph state remains reachable across title return or title idle, and the next native `LoadGame` can trigger Mono/Boehm GC mark-stack overflow while the native load path scans or allocates. DTMAPI has good cleanup for several known owned feature objects, but it does not yet prove that the full title-return boundary has converged before the next load.

This does not prove a single leaking class yet. It does prove the next useful work should be bounded boundary instrumentation and DTMAPI-owned object snapshots, not another full-project sweep.

## External Semantics Used

These public references explain why "Unity resources blew up" can mean "too much reachable object graph/root pressure", not only raw memory size:

- Unity managed memory docs: Unity's managed heap is traced by a GC; references keep objects alive, and fragmentation/allocation can force GC work. <https://docs.unity3d.com/2022.3/Documentation/Manual/performance-managed-memory.html>
- `Resources.UnloadUnusedAssets`: Unity decides unused assets by walking the whole GameObject hierarchy and also examining static variables; the script stack is not examined. <https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Resources.UnloadUnusedAssets.html>
- `AssetBundle.Unload`: unloading policy is delicate; keeping loaded objects alive or reloading bundles can duplicate memory/object state. <https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetBundle.Unload.html>
- Mono Boehm GC root plumbing pushes registered roots and handle stacks into `GC_push_all`; a large/deep reachable graph can fail in this area. <https://github.com/mono/mono/blob/main/mono/metadata/boehm-gc.c>
- Unity issue/discussion references show the exact `Unexpected mark stack overflow` class appears with very large GameObject/object graphs or GC root pressure. <https://issuetracker.unity.com/issues/5585>, <https://discussions.unity.com/t/gc-crash-unexpected-mark-stack-overflow/853176>

## Latest Evidence Map

Primary project evidence:

- `docs/updates/2026/20260704-0002-long-title-and-saveload-cycle-validation.md:83`: three-hour periodic run failed at the first post-idle load.
- `docs/updates/2026/20260704-0002-long-title-and-saveload-cycle-validation.md:89`: failure summary was `requests=1; active=SL-0001; duplicateRequests=0; nativeEnter=1; nativeReturn=0; saveLoaded=0`.
- `docs/updates/2026/20260704-0002-long-title-and-saveload-cycle-validation.md:104`: throttled rerun failed during cycle 2 after cycle 1 completed.
- `docs/updates/2026/20260704-0002-long-title-and-saveload-cycle-validation.md:111`: cycle-2 failure summary was `requests=2; active=SL-0002; duplicateRequests=0; nativeEnter=2; nativeReturn=1; saveLoaded=1`.
- `docs/updates/2026/20260704-0002-long-title-and-saveload-cycle-validation.md:124`: pending-pressure isolation published 600 `Smoke.SaveLoadCycle=pending` states and then loaded once successfully.
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md:5`: current status already says duplicate LoadGame is not present in latest samples.

Interpretation:

- `duplicateRequests=0` in both latest fatal samples weakens the old duplicate-load hypothesis.
- `nativeEnter > nativeReturn` and no second `SaveLoaded` put the crash inside native `LoadGame`, before DTMAPI can run most managed save-loaded cleanup or diagnostics.
- The 600-pending pressure pass weakens HookStatus/log-volume pressure as a standalone trigger.

## Logic Chain A: Native Load Boundary

Evidence A:

- The failing requests are active native load requests with no native return or SaveLoaded closure (`20260704-033314`, `20260704-060648`).

Code A:

- `DolocTownHookCallbacks.LoadGamePrefix` calls `Runtime?.NotifyLoadGameRequested(index)` (`src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs:30`).
- `DolocTownHookCallbacks.LoadGamePostfix` calls `Runtime?.NotifyLoadGameReturned(index, __result)` (`src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs:35`).
- `DolocTownHookCallbacks.AfterLoadArchiveDataPostfix` runs feature SaveLoaded hooks and then `Runtime?.NotifySaveLoaded(isNewGame)` (`src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs:40`).
- `DtmApiRuntime.NotifyLoadGameRequested` records coordinator native enter (`src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:262`).
- `DtmApiRuntime.NotifyLoadGameReturned` records native return (`src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:281`).
- `DtmApiRuntime.NotifySaveLoaded` records SaveLoaded and closes the save generation (`src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:349`).
- `SaveLoadRequestCoordinatorService.RecordNativeEnter` increments native enter and creates/attaches the request (`src/DTMAPI.Core/Runtime/SaveLoadRequestCoordinatorService.cs:51`).
- `SaveLoadRequestCoordinatorService.RecordSaveLoaded` increments SaveLoaded dispatch and completes the request (`src/DTMAPI.Core/Runtime/SaveLoadRequestCoordinatorService.cs:102`).

Resulting conclusion:

The current fatal happens after managed prefix instrumentation and before managed postfix/SaveLoaded instrumentation. Any explanation that requires DTMAPI `SaveLoaded` cleanup to run during the failing request is too late for this crash. The root cause must exist before or during native `LoadGame` entry.

## Logic Chain B: ReturnHome Boundary Is After-Native and Event-Like

Evidence B:

- One latest fatal sample succeeds through cycle 1, returns to title, waits, then dies on cycle 2 before SaveLoaded. That makes the `SaveLoaded -> ReturnHome -> title stable -> next LoadGame` boundary the most valuable current axis.

Code B:

- The smoke cycle waits for `Gameplay`, waits for native normal state, then calls ReturnHome (`src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs:1180`, `:1196`, `:1202`).
- After ReturnHome, the smoke waits for `HomePageUiState` and a short settle interval (`src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs:1208`, `:1217`).
- `ReturnHomePostfix` then runs EquipmentSlots cleanup, GameBridge feature cleanup, and only afterward runtime title-boundary notification (`src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs:60`).
- `DolocTownGameBridge.NotifyGameBridgeFeaturesReturnedToTitle` dispatches feature `ReturnedToTitle` and publishes a GameBridge final health snapshot (`src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:357`).
- `DtmApiRuntime.NotifyReturnedToTitle` records lifecycle, clears `currentLoadingSlot`, clears input transients, clears custom runtime instances, invokes `ReturnedToTitleBoundary`, dispatches public event, and flushes queues (`src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:396`).

Resulting conclusion:

The current `ReturnedToTitle` signal is useful but not sufficient proof that native/Unity title state is fully converged. It is a managed postfix/event boundary after native ReturnHome, not a pre/post object graph ledger. It also publishes GameBridge feature health before Core invokes `ReturnedToTitleBoundary`, so Bootstrap UI cleanup/reporting is not included in that GameBridge snapshot.

## Logic Chain C: Process-Long Roots Can Keep Unity Objects Reachable

Evidence C:

- Unity `Resources.UnloadUnusedAssets` examines static variables and walks the GameObject hierarchy. Therefore a process-long managed static or singleton that references Unity objects can keep them alive and make them part of later GC/resource scans.

Code C:

- `DolocTownHookCallbacks` is static and holds static `Runtime` and `Bridge` references (`src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs:16`, `:26`).
- `BootstrapPlugin` holds process-long `runtime`, `bridge`, `titleSettingsUi`, and `debugConsoleUi` fields (`src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs:19`, `:21`).
- Title settings UI stores rendered Unity objects, event binders, static event binders, root objects, fallback EventSystem roots, and an icon sprite (`src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs:27`, `:29`, `:58`, `:63`).
- Title settings UI explicitly calls `DontDestroyOnLoad(root)` (`src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs:193`).
- Debug console UI also has event binders and fallback EventSystem state, and calls `DontDestroyOnLoad(root)` (`src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:21`, `:61`, `:328`).

Resulting conclusion:

DTMAPI intentionally has process-long managed roots. That is normal for a bootstrap, but it means DTMAPI must either avoid holding Unity objects under those roots or report them clearly. Today the GameBridge feature health counters do not cover every Bootstrap UI root.

## Most Suspicious Technical Debts

### Ranked Held-Object Chains To Snapshot

These are not proven root causes. They are the highest-value object families to count at the next title-return/next-load boundary because they are reachable from long-lived DTMAPI roots and can contain Unity/native objects:

1. Static hook root -> GameBridge -> CustomAnimals asset caches. `DolocTownHookCallbacks.Runtime` / `Bridge` are static (`src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs:16`, `:26`), `DolocTownGameBridge` holds process-long feature instances (`src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs:225`), and CustomAnimals keeps `controllerCache` / `bundleCache` (`src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs:34`, `:35`). This is the strongest technical-debt chain because the cached objects can be Unity `RuntimeAnimatorController` / `AssetBundle` instances.
2. AudioReplacement entries -> request/async/AudioClip/platform player. `AudioReplacementService.entries` is process-long for loaded definitions (`src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementService.cs:44`), while each entry can hold `Request`, `AsyncOperation`, `AudioClip`, `AudioCallbackOwner`, and `PlatformAudioPlayer` (`AudioReplacementService.cs:2178`, `:2180`, `:2182`). SaveLoaded/ReturnedToTitle clears animal sound contexts and refreshes definitions, but clip cleanup runs mainly through content-generation replacement / `CleanupEntry` (`AudioReplacementFeature.cs:55`, `:61`; `AudioReplacementService.cs:192`, `:1925`, `:1954`). This is a medium-high snapshot target.
3. EquipmentSlots UI clone chain. `activeEquipmentSlotUiObjects` and `equipmentSlotUiEventBinders` are process-held lists (`src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs:32`, `:33`); clone UnityEvent delegates can capture runtime entries and native item/function state. SaveLoaded/ReturnedToTitle cleanup is present, so this is mainly risky if ReturnHome cleanup is skipped, delayed, or runs after native objects have become awkward to destroy (`src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs:48`, `:2328`).
4. SaveSlots official UI panel chain. `officialSaveUiStates` keys are native panel objects and values hold pager roots/binders (`src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsService.cs:90`, `:683`). Cleanup is present on SaveLoaded/ReturnedToTitle, but this still belongs in boundary snapshots because it ties a managed dictionary to official UI panel objects.
5. AnimalViewer native data/UI overlay chain. `animalProgressRowsByData` keys are native data objects and the service tracks active overlay objects/parent (`src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerService.cs:18`, `:19`, `:33`). SaveLoaded/ReturnedToTitle cleanup is present; EnvironmentReset only validates (`AnimalViewerService.cs:190`, `:199`).
6. FishingAutomation native-object keyed dictionaries/hashsets. These are less likely for the current failure because latest fatal timing is not active fishing, but `fishingMiniGameStartedAt`, input stats, completed handles, ready-charge states, animator speeds, and hook physics snapshots are still good counters to include (`src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs:40`, `:47`, `:48`, `:2037`, `:2051`).
7. Managed diagnostic ledgers. `ModOwnerLedgerService.entries` and `ResourceLifecycleLedgerService.records` / `refreshCounts` can grow as managed diagnostic data (`src/DTMAPI.Core/Runtime/ModOwnerLedgerService.cs:11`; `src/DTMAPI.Core/Runtime/ResourceLifecycleLedgerService.cs:80`, `:81`). They do not directly hold Unity/native objects, so they are lower-confidence for Mono mark-stack overflow, but unbounded diagnostic metadata should be kept visible.

### 1. Missing TitleReturnBoundaryLedger

Current state:

- SaveLoad has a request coordinator.
- Resource lifecycle has save/content-generation diagnostics.
- GameBridge has feature final health snapshots.
- There is no ledger that connects ReturnHome request, ReturnHome postfix, Core ReturnedToTitle start/end, title-stable frame, next native LoadGame enter, next native LoadGame return, SaveLoaded, and fatal-window observation.

Why it matters:

The latest crash window is exactly between title stability and next native `LoadGame` closure. Existing diagnostics say "feature cleanup ran" or "a native load entered", but do not show what DTMAPI-owned Unity objects existed at these four moments:

1. before ReturnHome,
2. after ReturnedToTitle handling,
3. immediately before next LoadGame,
4. after native LoadGame enter if still recordable.

Risk:

High diagnostic risk. This gap is why the root cause cannot be proven from current code and logs.

Recommended next patch:

- Add `TitleReturnBoundaryLedger` under Core or GameBridge diagnostics.
- Record boundary id, last save-load request id, phase, `InputContext`, frame/time, thread id, and fatal-window state.
- Keep it diagnostic-only.
- Publish a report line and smoke `result.json` summary.

### 2. Bootstrap UI Persistent Roots Are Not In GameBridge Health

Current state:

- `BootstrapPlugin` creates both UI hosts at startup (`src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs:56`, `:57`).
- Only debug console subscribes to `ReturnedToTitleBoundary` (`src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs:59`).
- Title settings UI does not have a ReturnedToTitle boundary reset. It updates every frame and hides itself when `runtime.UI.InputContext` is not `HomePageUiState` (`src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs:113`, `:120`, `:136`).
- Title settings fallback EventSystem is destroyed when title is hidden or a native EventSystem appears (`src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs:141`, `:302`, `:483`).
- Debug console closes on title boundary if open and destroys its fallback EventSystem (`src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs:117`, `:187`, `:200`, `:560`).
- Both UI roots are persistent `DontDestroyOnLoad` roots (`ReflectedTitleMenuSettingsUi.cs:193`, `ReflectedDebugConsoleUi.cs:328`).

Why it matters:

The persistent roots may be benign. The problem is that current final health snapshots can report clean GameBridge feature state while Bootstrap still has persistent Unity roots, event binders, icon sprite, panel roots, or fallback EventSystem state not summarized in the same boundary evidence.

Risk:

Medium-high diagnostic risk; medium root-cause risk. It is not proven that these roots grow, but they are exactly the kind of reachable Unity object graph Unity/Mono will see.

Recommended next patch:

- Add `GetLifecycleSummary()` to `ReflectedTitleMenuSettingsUi` and `ReflectedDebugConsoleUi`.
- Include counts: initialized, root alive, active root, title button root, panel root, renderedObjects, eventBinders, staticEventBinders, inputFields, hoverTooltipRoot, eventSystemRoot, eventSystemComponent, iconSprite.
- Register a Bootstrap runtime report context provider.
- Add `ReflectedTitleMenuSettingsUi.ResetForTitleBoundary()` that closes `runtime.UI`, clears rendered panel objects, clears transient binders/input values/capturing state, destroys owned EventSystem, and leaves only the intended title button host if title is visible.

Do not destroy unknown/native EventSystems or game-owned UI. Only release DTMAPI-owned roots/binders.

### 3. Title-Lifetime Asset Caches Are Report-Only

Current state:

- Custom animals keep `controllerCache` and `bundleCache` dictionaries (`src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs:34`, `:35`).
- Save-lifetime contexts are cleared on SaveLoaded/ReturnedToTitle, but title-lifetime controller/bundle caches are intentionally retained (`CustomAnimalAnimatorBridgeService.cs:183`, `:257`).
- Stale controller cache refs are released as managed refs; stale bundle refs are `drop-managed-ref-only-no-unload` (`CustomAnimalAnimatorBridgeService.cs:369`, `:376`, `:421`, `:432`).
- Loaded `RuntimeAnimatorController` and `AssetBundle` resources are recorded as title lifetime with no destroy/unload (`CustomAnimalAnimatorBridgeService.cs:1255`, `:1292`, `:1323`, `:1344`).
- The scaffold explicitly keeps title asset release disabled/blocked (`src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:926`, `:932`; `src/DTMAPI.Core/Runtime/RefactorScaffoldOptions.cs:87`, `:89`).

Why it matters:

Unity asset caches across title are a reasonable compatibility choice, but they must be bounded and visible. Unity's `AssetBundle.Unload` rules make asset release risky, so the safer next step is not to start destroying bundles. The safer step is to snapshot counts/ids and prove they are stable across title-return-next-load.

Risk:

High technical-debt chain; medium diagnostic risk; low-to-medium current root-cause proof. Latest evidence says `TitleIdleResourceGrowth`, registry diffs, and feature final health passed, so this should be counted before being blamed. It becomes a lead suspect only if bundle/controller counts grow or stale cache entries survive content/title boundaries unexpectedly.

Recommended next patch:

- Expose `controllerCache.Count`, non-null controller count, `bundleCache.Count`, non-null bundle count, current content signature, and stale-release counters in CustomAnimals lifecycle summary.
- Keep no-unload policy until a specific DTMAPI-owned stale bundle/controller pattern is proven.

### 4. AudioReplacement Title-Lifetime Audio Objects Need Counts

Current state:

- AudioReplacement stores loaded definitions in `entries` and per-owner `states` (`src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementService.cs:44`, `:45`).
- SaveLoaded/ReturnedToTitle clear only save-lifetime animal sound context and refresh content pack definitions (`src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementFeature.cs:55`, `:61`; `AudioReplacementService.cs:192`).
- `CleanupEntry` disposes pending requests and destroys `AudioClip`, but it runs when entries are replaced/removed, not merely because a title boundary happened (`AudioReplacementService.cs:1925`, `:1940`, `:1954`).
- `AudioReplacementEntry` can hold `Request`, `AsyncOperation`, `AudioClip`, `AudioCallbackOwner`, and `PlatformAudioPlayer` (`AudioReplacementService.cs:2178`, `:2180`, `:2182`).

Why it matters:

Audio clips are intended title-lifetime resources, similar to CustomAnimals assets. That can be valid and avoids expensive reloads, but the next audit needs to prove entry/clip/request/player counts are stable and no pending UnityWebRequest or callback owner remains across title-return-next-load.

Risk:

Medium-high snapshot target; low-to-medium current root-cause proof. The latest fatal is not specifically tied to audio playback, but WAV clips and callback owners are Unity/native object references held under a process-long feature service.

Recommended next patch:

- Add an AudioReplacement lifecycle summary with entries, ready clips, pending requests, async operations, platform players, callback owners, content-pack keys, and animal context count.
- Include it in GameBridge final health and the proposed title-return snapshot.

### 5. SaveLoadBoundary Hook Status Can Mislead After Cycle 1

Current state:

- `DtmApiRuntime.PublishSaveLoadRequestCoordinatorUpdate` sets `Refactor.SaveLoadBoundary` to `closed` if `snapshot.SaveLoadedDispatchCount > 0`; only if zero does it use active request state (`src/DTMAPI.Core/Runtime/DtmApiRuntime.cs:1499`, `:1516`, `:1517`).

Why it matters:

After cycle 1 succeeds, `SaveLoadedDispatchCount` remains greater than zero. During cycle 2, an active request can exist but the hook status may still read `closed`, while the detailed summary correctly shows `active=SL-0002`.

Risk:

Low root-cause risk; high evidence-reading risk. It can cause future reports to overtrust `SaveLoadBoundary=Passed`.

Recommended next patch:

- Derive `Refactor.SaveLoadBoundary` from the active/latest request, not the global historical SaveLoaded count.
- For example: active request with no SaveLoaded => `loading`; latest request completed => `closed`; no request => `idle`.

## Paths Now Weakened Or Not Root

### Duplicate LoadGame

Current latest evidence has `duplicateRequests=0` in both fatal samples. The coordinator suppresses only DTMAPI/smoke duplicate direct requests for the same active slot (`src/DTMAPI.Core/Runtime/SaveLoadRequestCoordinatorService.cs:24`, `:51`). This remains a useful guardrail, but it is not the latest root cause.

### HookStatus / Pending Log Pressure

The formal pending-pressure run published exactly 600 pending statuses and then completed one native load with one SaveLoaded and no fatal (`docs/updates/2026/20260704-0002-long-title-and-saveload-cycle-validation.md:124`, `:130`). Log/status density alone is unlikely to be necessary.

### Managed Callback Exception Propagation

Hook callbacks and feature dispatch catch exceptions and record diagnostics. A plain managed callback throw is not a good match for a native Mono GC fatal before LoadGame postfix/SaveLoaded.

### SaveSlots, EquipmentSlots, AnimalViewer UI Clone Lists

These remain important snapshot targets but are less suspicious than before:

- SaveSlots clears official pager roots and binders on SaveLoaded/ReturnedToTitle and reports lifecycle summary (`src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsFeature.cs:68`, `:74`; `SaveSlotsService.cs:90`, `:133`).
- EquipmentSlots clears active clone objects and binders on SaveLoaded/ReturnedToTitle; EnvironmentReset is explicitly non-destructive (`src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs:16`, `:37`, `:43`, `:48`, `:2328`, `:2346`).
- AnimalViewer clears overlay session and rows on SaveLoaded/ReturnedToTitle, with EnvironmentReset validation (`src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerFeature.cs:55`, `:60`, `:65`; `AnimalViewerService.cs:190`, `:199`, `:564`, `:605`).

### AutoFishing Active Loop

Latest fatal timing is title/next-load before SaveLoaded, not active fishing. AutoFishing should remain in lifecycle summaries, but it is not the lead suspect for the current leftover.

### Managed Diagnostic Ledgers

`ModOwnerLedgerService.entries` and `ResourceLifecycleLedgerService.records` / `refreshCounts` can grow over time, but they are ordinary managed diagnostic data and do not directly hold Unity/native objects (`src/DTMAPI.Core/Runtime/ModOwnerLedgerService.cs:11`; `src/DTMAPI.Core/Runtime/ResourceLifecycleLedgerService.cs:80`, `:81`). They are worth bounding or summarizing later, but they do not explain the current native object graph hypothesis as well as the Unity object holders above.

## Proposed Short Next Step

Do not run a long validation for this review task. The next implementation goal should be short and surgical:

1. Add `TitleReturnBoundaryLedger`.
2. Add DTMAPI-owned object snapshots at four boundaries:
   - before ReturnHome request,
   - after Runtime ReturnedToTitle completes,
   - before next LoadGame prefix,
   - after LoadGame prefix/native enter if still alive enough to record.
3. Add Bootstrap UI lifecycle summaries to runtime report context.
4. Fix `Refactor.SaveLoadBoundary` so it is per-active/latest request.
5. Run a short gate only: save slot 3, load -> ReturnHome -> title stable -> load, three cycles maximum. Capture startup log, SaveLoad summary, title-return ledger, Bootstrap/GameBridge lifecycle summaries, fatal-window check, and no leftover `DolocTown.exe`.

If the short snapshot shows DTMAPI-owned counts clean at every boundary but fatal still occurs in a later long run, reclassify toward native/game-owned title/load boundary and keep Unity crash dumps. If a specific DTMAPI-owned count remains nonzero or grows, target only that class in the next patch.

## Confidence

- High: latest failure is not duplicate LoadGame.
- High: latest failure occurs after native LoadGame prefix and before native return/SaveLoaded.
- High: dense pending HookStatus/log pressure is not necessary by itself.
- Medium-high: the remaining issue class is title-return/title-stable/next-load Unity/native object graph state.
- Medium: Bootstrap persistent UI roots are the most important unreported DTMAPI-owned object family.
- Medium-high technical-debt concern but low-to-medium root-cause proof: CustomAnimals title-lifetime AssetBundle/controller caches and AudioReplacement title-lifetime AudioClip/request/player entries. They are plausible reachable Unity/native object holders, but current evidence does not prove growth.
- Not proven: a single leaking object/class. The available evidence supports a narrowed object-graph boundary audit, not a final root-cause conviction.

## Handoff Summary

Use this as the next `/goal` shape:

```text
ISSUE-010 next step: implement diagnostic-only TitleReturnBoundaryLedger and DTMAPI-owned object snapshots for ReturnHome -> title stable -> next LoadGame. Include Bootstrap UI lifecycle summaries and fix Refactor.SaveLoadBoundary to be per-request. Do not destroy unknown/native objects. Run only a short 3-cycle slot-3 gate, no long test.
```
