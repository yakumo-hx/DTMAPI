# 20260708-0001 AutoFishing Long-Play GC Research

## Scope

User asked to start with AutoFishing-related code research for a long-play GC problem, especially after enabling AutoFishing. This is a source/document review only: no game launch, no runtime lock, no implementation change, and no claim that the GC issue is fixed.

Required context checked: project/planning/debug/reference indexes, recent ISSUE-010 records, AutoFishing lifecycle/log-throttle/history updates, hook map, smoke matrix, `FishingAutomationService`, `FishingAutomationFeature`, `FishingAutomationHookBridge`, `DolocTownHookCallbacks`, `AutoFishingMod`, and the resource lifecycle ledger path used by AutoFishing diagnostics. This revision adds the requested five iterative review rounds: each round questions the previous round's finding, then answers from current source/update/debug evidence.

## Current Classification

This review should not reopen the already narrowed main-menu/title-idle route as an AutoFishing root cause. The 2026-07-05 to 2026-07-08 ISSUE-010 evidence repeatedly disabled AutoFishing in the reproduced full/profile failures, while `CoreAutoFishing` and short fifth-save AutoFishing lifecycle/soak routes reported bounded native transients. The current question is narrower and different: during long active gameplay, especially long AutoFishing loops, can AutoFishing create enough managed allocation/diagnostic pressure or retain enough native-object keys to contribute to a GC failure class?

Current answer: plausible, with the strongest source-level suspect being diagnostic/resource-ledger pressure plus per-frame reflection/allocation, not an immediately proven unbounded native-object dictionary leak.

## Answer To Current Lifecycle Question

Yes, this belongs in the lifecycle family, but there are two different lifecycles that should not be collapsed:

1. Gameplay native-object lifecycle: AutoFishing borrows native objects such as minigame handles, Ready charge states, animators, and hook rigidbodies. The source code has cleanup for the nominal loop: `StopGame`, non-Ready phase transitions, Pull exit, disable, save load, and title return. Short previous soak evidence also reported transient native handles returning to zero. This is not currently proven to be a normal-loop strong-reference leak.
2. Diagnostic/evidence lifecycle: AutoFishing records those short-lived native handles into `ResourceLifecycleLedger` as `SaveLifetime` borrowed-native resources. The ledger stores string resource ids, so it should not keep Unity objects alive directly. But released save-lifetime records remain in the open save generation, and every later observe/release builds a full copied/sorted snapshot. For long AutoFishing sessions, this can turn many correctly released fishing objects into accumulated diagnostic records and repeated allocation/log work.

So if the question is "is there something that should be cleared after each fish but is not?", the clearest answer is: the AutoFishing gameplay dictionaries mostly do clear per fish on the expected path, but the resource lifecycle ledger intentionally does not clear released `SaveLifetime` records at fish end. That policy is acceptable for low-churn evidence, but is risky for high-churn AutoFishing native handles. I would classify it as a diagnostic lifecycle mismatch rather than a direct native object lifecycle leak.

The other class is not long retention, but sustained hot-path allocation. Ready/minigame/update paths can allocate every frame or every catch even when nothing accumulates permanently. That can still produce GC pressure during AFK fishing.

## Five Iterative Review Rounds

### Round 1 - If "things accumulate", do AutoFishing native-key collections actually have cleanup?

Question raised from the first suspicion: are `FishingAutomationService` dictionaries and sets just growing during every fish, or is there code-level cleanup after each native stage?

Research:

- `FishingAutomationService` stores native/transient state in object-keyed maps and sets: minigame start times, bonus taps, input stats, completed handles, Ready charge states, animator snapshots, and hook-physics snapshots (`FishingAutomationService.cs:40-48`).
- `NotifyFishingMiniGameStart` creates a minigame record and observes the native handle (`FishingAutomationService.cs:401-409`).
- `NotifyFishingMiniGameStop` removes that handle from `fishingMiniGameStartedAt`, `fishingMiniGameBonusTaps`, `fishingMiniGameInputStats`, `fishingMiniGameCompletedHandles`, clears the current override, and releases the lifecycle record (`FishingAutomationService.cs:436-445`).
- `DolocTownHookCallbacks` wires `FishingGameScrollBar.StartGame`, `UpdateGame`, and `StopGame` directly to those service methods (`DolocTownHookCallbacks.cs:595-612`).
- `FishingPullExitPostfix` runs restore and `NotifyFishingNativeExit` in a `finally` block (`DolocTownHookCallbacks.cs:719-729`).
- `ResetFishingRuntimeState` and `ClearFishingAutomationTransientState` clear all main native/transient AutoFishing maps (`FishingAutomationService.cs:2035-2124`).

Answer: normal AutoFishing gameplay state is not obviously an always-growing strong-reference leak. There is concrete cleanup after minigame stop, disable, save load, and title return. The weak spot is abnormal native flow: `NotifyFishingNativeExit` publishes an expected-clean lifecycle boundary but does not itself clear leftover minigame/input/completed handles (`FishingAutomationService.cs:2030-2032`). If `StopGame` or a phase transition is skipped, the retained native-object keys can survive until disable/save/title. That is a real lifecycle risk, but it is not yet evidence that the nominal loop leaks every fish.

### Round 2 - If those maps clear, where can long-session accumulation still live?

Question raised from Round 1: if the gameplay dictionaries mostly clear, why does "long AutoFishing" still fit a cumulative GC story?

Research:

- AutoFishing observes native handles through `ObserveFishingNativeHandleResource`, using resource kind `AutoFishing.<kind>`, a formatted native object id, `SaveLifetime`, and `BorrowedNative` ownership (`FishingAutomationService.cs:2200-2212`).
- AutoFishing releases those handles through `ReleaseFishingNativeHandleResource`, again as `SaveLifetime` borrowed-native records (`FishingAutomationService.cs:2215-2227`).
- The release policy explicitly says "clear DTMAPI key/reference only; do not destroy native object" (`FishingAutomationService.cs:2212`, `FishingAutomationService.cs:2226`), which means this ledger is evidence/accounting, not native ownership.
- `ResourceLifecycleLedgerService.RecordResource` creates or updates a record and adds a timeline entry on every observe (`ResourceLifecycleLedgerService.cs:154-226`).
- `ResourceLifecycleLedgerService.ReleaseResource` creates a record if missing, marks release status, and adds a timeline entry on every release (`ResourceLifecycleLedgerService.cs:230-282`).
- `PruneClosedSaveLifetimeRecordsNoLock` only removes save-lifetime records whose generation is older than the current save generation (`ResourceLifecycleLedgerService.cs:406-428`).

Answer: the clearest accumulation candidate is not the native object dictionary but the diagnostic ledger record set. The ledger does not retain the Unity object directly; AutoFishing passes a formatted id string. But during one long open save, released `SaveLifetime` records for high-churn AutoFishing handles remain present until a later save generation closes. A 1000-fish AFK session can therefore accumulate many released string records even if the gameplay maps correctly return to zero after each fish.

### Round 3 - Are ledger records harmless strings, or can they still create GC pressure?

Question raised from Round 2: if the ledger only stores strings and bounded status keys, is it just cosmetic noise, or can it materially increase allocation pressure?

Research:

- Every AutoFishing observe calls `DtmApiRuntime.ObserveResourceLifecycle`, which immediately publishes a resource lifecycle ledger update (`DtmApiRuntime.cs:1014-1041`).
- Every AutoFishing release calls `DtmApiRuntime.ReleaseResourceLifecycle`, which also immediately publishes a ledger update (`DtmApiRuntime.cs:1057-1084`).
- `ResourceLifecycleLedgerService.BuildSnapshotNoLock` copies diagnostics, sorts all records, materializes `records.Values.Select(...).OrderBy(...).ThenBy(...).ToArray()`, creates a dictionary from refresh counts, and copies the timeline (`ResourceLifecycleLedgerService.cs:347-366`).
- `PublishResourceLifecycleLedgerUpdate` formats the whole snapshot summary on every update (`DtmApiRuntime.cs:1777-1780`).
- The same publisher logs any operation containing `Release` (`DtmApiRuntime.cs:1782-1789`), so AutoFishing's routine handle releases take the runtime logging branch.
- `DiagnosticsService.SetFeatureStatus` replaces fixed feature keys (`DiagnosticsService.cs:128-134`), so this is not a feature-status key leak.

Answer: this is allocation/log pressure, not a status-key leak. The dangerous shape is repeated full-snapshot work over a record set that can grow during one open save. Even when feature status keys are fixed, each observe/release can allocate snapshot arrays, sorted records, summaries, log strings, and replacement status objects. This explains why the 2026-06-28 AutoFishing success-diagnostic throttle was useful but incomplete: it reduced AutoFishing-specific hook/log spam, while resource lifecycle snapshotting and release logging remain outside that throttle.

### Round 4 - If ledger pressure is fixed or sampled, what AutoFishing hot paths remain?

Question raised from Round 3: after the ledger, are there other long-play GC risks that do not require long-term retention?

Research:

- `UpdateFishingAutoCast` is reached by an every-frame feature. It has early returns, but eligible attempts still resolve `DolocAPI`, read static/native members, scan fishing pools, find `UseFishRod` through `GetMethods(...).FirstOrDefault(...)`, invoke with `new[] { rod }`, and update summary strings (`FishingAutomationService.cs:208-362`).
- `HasFishingPoolInScene` resolves native types and calls reflected `UnityEngine.Object.FindObjectsOfType(Type)` (`FishingAutomationService.cs:1881-1897`).
- `GameBridgeNativeHelpers.ResolveType` scans loaded assemblies after `Type.GetType` fails (`GameBridgeNativeHelpers.cs:137-156`).
- `ReadStaticMember`, `ReadMember`, and `FindMethodInHierarchy` perform reflection lookups on demand (`GameBridgeNativeHelpers.cs:169-199`, `GameBridgeNativeHelpers.cs:287-332`).
- `ApplyFishingReadyChargeTarget` reads progress and calls `RefreshFishingReadyChargeBar(readyState, progress, new List<string>())` before the already-tracked guard returns (`FishingAutomationService.cs:1108-1119`).
- `TryPrepareFishingMiniGameAutomationInput` runs during minigame update, reads native status/note state, creates/uses bonus tap sets, creates a new `FishingMiniGameInputOverride`, updates stats, and writes a long summary string (`FishingAutomationService.cs:1435-1501`).
- `AutoFishingMod.OnUpdateTicked` polls fixed movement-cancel keys while enabled and calls `DtmButton.Parse(key)` inside the update loop (`AutoFishingMod/ModEntry.cs:106-125`).

Answer: yes. Even with perfect cleanup, AutoFishing can still be a sustained allocation source during active AFK loops. These are not "forgot to clear" bugs; they are hot-path cost bugs. The most concrete low-risk findings are Ready charge's avoidable sample-list/UI-refresh work before the already-tracked return, repeated reflection/native scene discovery in auto-cast, per-frame minigame override/status strings, and per-update manual-cancel key parsing.

### Round 5 - What should change without changing the native-owner semantics?

Question raised from Round 4: how can stability improve without undoing the reasons AutoFishing was written this way?

Research:

- The 2026-06-13 native loop rewrite intentionally moved AutoFishing to the game's real native stages: `BodyController.UseFishRod`, native wait/reel, visible minigame input, native collect, and recast.
- The 2026-06-13 minigame/animation work explicitly rejected direct minigame success writes in favor of scoped native input and native Ready/Cast/Pull owners.
- The 2026-06-20 lifecycle safety follow-up says `EnvironmentReset` is too frequent for destructive cleanup and that save/title/native exits own cleanup; it also moved pending-cast tracking before native `UseFishRod` and made stalls a short-backoff diagnostic path.
- The 2026-06-28 diagnostic throttle was created because a longer run with many AutoFishing catches produced thousands of success diagnostics, proving log volume can become a runtime concern.
- The 2026-07-03 AutoFishing lifecycle attribution audit found short fifth-save soak cleanup with `nativeTransientHandles=0`, which supports preserving the native loop while improving longer-session evidence.
- The 2026-07-07/08 hotkey records restored AutoFishing F6 and movement-cancel behavior while removing the old title-idle registered-string polling amplifier.
- The 2026-07-08 FullKnown validation keeps the main-menu/title-idle path separated from the still-open long-term gameplay GC class.

Answer: hardening should target evidence volume and hot-path allocations, not gameplay semantics. Keep native ownership and keep `EnvironmentReset` non-destructive. The strongest non-semantic changes are: aggregate/sample AutoFishing borrowed-native resource records; avoid full resource ledger snapshot/log publication for every transient release; cache reflected native types/members/methods; cache or invalidate fishable-water discovery; move Ready charge's already-tracked check ahead of avoidable allocation where visual behavior is unchanged; cache parsed manual-cancel buttons; and add per-N-catch counters that separate native-transient counts from ledger-record counts and hot-path call counts.

## Code Path Map

- `testmods/AutoFishingMod/ModEntry.cs` owns F6 policy, config, and movement cancel. After the 2026-07-07/08 hotkey work, F6 is a typed gameplay keybind and manual cancel buttons are registered only while automation is enabled.
- `FishingAutomationFeature` is an every-frame GameBridge feature. Save/title boundaries call `ResetFishingRuntimeState`; `EnvironmentReset` is intentionally non-destructive.
- `FishingAutomationHookBridge` patches Ready/Cast/Wait/Pull, `FishingGameScrollBar`, selected `DolocUserInput` getters, and `FishRodRenderer` CastHook/Pull/PullCancel.
- `FishingAutomationService` owns auto-cast, wait/reel routing, visible minigame input automation, fast animation/charge, native handle lifecycle summaries, and diagnostic publication.
- AutoFishing native handle lifecycle records flow into `DtmApiRuntime.ObserveResourceLifecycle` / `ReleaseResourceLifecycle`, which use `ResourceLifecycleLedgerService`.

## Findings

1. AutoFishing's native-object-keyed collections are mostly bounded on the nominal fishing loop, but the cleanup depends on native hook boundaries firing.

   `SetEnabled(false)`, `SaveLoaded`, and `ReturnedToTitle` clear transient state. Leaving `Ready` clears ready-charge handles. `FishingGameScrollBar.StopGame` clears the concrete minigame handle/input state. `AgentStateFishingPull.OnExit` restores animator/hook physics and publishes a native-exit lifecycle boundary.

   The risk is the abnormal path: `NotifyFishingNativeExit` asserts `expectTransientClear=true`, but it does not itself clear leftover minigame/completed/input handles. If `StopGame` or phase transitions are skipped during an odd native path, AutoFishing should warn through `Fishing.Automation.Lifecycle`, but it can retain those native object keys until disable/save/title. That is a real watch item for long-play packages.

2. The strongest source-level GC pressure suspect is `ResourceLifecycleLedger`, not the AutoFishing dictionaries themselves.

   AutoFishing calls `ObserveFishingNativeHandleResource` / `ReleaseFishingNativeHandleResource` for minigame handles, ready-charge states, animator snapshots, and hook-physics snapshots. These are formatted as strings, so the ledger does not retain native Unity objects directly.

   However, each observe/release calls `ResourceLifecycleLedgerService.BuildSnapshotNoLock()`. That snapshot copies and sorts all lifecycle records with `Select(...).OrderBy(...).ThenBy(...).ToArray()`. Released `SaveLifetime` records are only pruned when a later save generation closes, not continuously during one long open save. A 1000-fish session can therefore grow many released AutoFishing native-handle records inside the same save generation, and every later observe/release pays a larger snapshot allocation cost. Releases also satisfy the runtime `shouldLog` branch, so routine AutoFishing handle releases can create repeated log/status work outside the 2026-06-28 AutoFishing-specific diagnostic throttle.

   This is the highest-priority next hypothesis because it matches "long play plus AutoFishing" better than a title-idle root and explains cumulative managed allocation pressure without requiring a native-object strong-reference leak.

3. Auto-cast idle/probe work still uses expensive reflection and scene scans.

   `UpdateFishingAutoCast` runs from an every-frame feature. It throttles recast attempts, but on eligible attempts it resolves `DolocAPI`, reads static members, reflects `UseFishRod`, and calls `HasFishingPoolInScene()`. `HasFishingPoolInScene()` uses `UnityEngine.Object.FindObjectsOfType(Type)`, which can allocate arrays and scan the scene. In a no-water, wrong-tool, busy, or repeatedly blocked state, this becomes a periodic allocation/reflection path while AutoFishing remains enabled.

4. Ready/minigame/animation hot paths allocate temporary managed objects during active fishing.

   `ApplyFishingReadyAutomation` calls into Ready charge handling every Ready `OnPlay`; `ApplyFishingReadyChargeTarget` creates a fresh `List<string>` and refreshes reflected native UI fields before the "already tracked" early return. Fast animation paths create `List<string>`, `List<KeyValuePair<object,string>>`, and `HashSet<object>` samples. Minigame input automation creates/updates per-handle stats and summary strings during `FishingGameScrollBar.UpdateGame`. These allocations are bounded per frame/catch, but in an AFK loop they become sustained GC pressure.

5. The 2026-06-28 success-diagnostic throttle is still valid, but it does not cover all pressure paths.

   `fishingAutomationDiagnostics` is keyed by fixed diagnostic keys and is cleared on reset, so it is not an obvious unbounded map. The throttle reduces repeated AutoFishing hook/status/log spam. It does not remove allocations created before the publish decision, resource-ledger snapshots, release logs, reflection lookup arrays, or lifecycle snapshot formatting.

6. AutoFishingMod's current input ownership is not the main long-play GC suspect.

   The mod now keeps one F6 gameplay keybind while disabled and temporarily registers A/D/Space/Shift snapshot buttons only while enabled. This is consistent with the hotkey rebuild method. It should still be counted during long-play input diagnostics, but it is no longer the old title-idle registered-string polling amplifier.

## Function-Level Review

### Abstractions

`IFishingAutomationApi` is a stable public control surface: `Configure`, `SetEnabled`, `GetState`, and `GetStatus`. It does not own native handles and should not itself accumulate long-play data. The API shape is correct for keeping fragile native logic inside GameBridge.

`FishingAutomationOptions` is config/state transfer only. `NormalizeOptions` clamps values and keeps behavior policy explicit: native wait or instant bite, visible minigame auto-complete or native skip, normal or fast animation, manual movement cancel, recast delay, cast charge ratio, animation multiplier, and verbose logging. It is not a GC suspect except through the runtime behavior those options enable.

`FishingAutomationState` keeps one status per automation owner. The owner-state dictionary is process-lifetime and bounded by mod owners. It replaces strings rather than appending historical state.

### `FishingAutomationFeature`

Constructor: creates one `FishingAutomationService` and one hook bridge for the process. No loop accumulation.

`Contract`: marks the feature as save-required, non-title, native-scene-required, every-frame, environment-reset-sensitive, and save-lifetime-state. The important GC implication is that `Update` is called every frame, so expensive work must be gated inside the service.

`RegisterApis`: registers one API instance. No long-run accumulation.

`PublishHookStatuses` and `InstallHooks`: delegate hook setup/status. These are install-time/status operations, not per-frame gameplay paths.

`Update`: calls `Service.Update()` every frame. This is a frequency amplifier for any service path that misses an early return.

`SaveLoaded` and `ReturnedToTitle`: call `ResetFishingRuntimeState`, which is the destructive cleanup boundary. This is the strongest code-level evidence that native transient maps are intended to be save/title scoped, not process scoped.

`EnvironmentReset`: calls non-destructive `NotifyFishingEnvironmentReset`. This is intentional because Doloc Town environment reset events can be high frequency during normal play. Making it destructive would likely create false cancellations.

### `FishingAutomationHookBridge`

Fields/properties: one boolean per hook target and a fixed `installAttempted` flag. The hook readiness state is bounded.

`HooksReady`: requires every expected fishing hook. This is a correctness gate; AutoFishing should not start native automation unless its cleanup/control hooks are all present.

`InstallHooks`: patches Ready, Cast, Wait, `FishingGameScrollBar`, `DolocUserInput` getters, `FishRodRenderer`, and Pull state methods. The hook list shows why the implementation is native-owner oriented: it follows the game's actual state machine and input/result surfaces. Hook installation itself is not a GC suspect, but each hook target determines where cleanup can or cannot run.

`PublishStatuses`: publishes a fixed `Fishing.Automation` hook status key. This should replace status, not grow a key set.

### `DolocTownHookCallbacks` Fishing Hooks

`AgentStateBaseExitPostfix`: restores action speed and AutoFishing animator speeds on all state exits. Purpose is broad safety cleanup. It can allocate restore lists when snapshots exist, but that is bounded by active snapshots.

`AgentStateFishingReadyEnterPostfix`: records phase `Ready`. This starts the Ready lifecycle and permits cast-charge/animation tracking.

`AgentStateFishingReadyPlayPostfix`: applies Ready automation every Ready `OnPlay`. This is one of the highest-frequency paths while holding charge. Review focus: make sure the service exits before reflection/list work when the charge state is already tracked.

`AgentStateFishingCastEnterPostfix`: records phase `Cast`. This is phase bookkeeping and also triggers animation-speed logic.

`AgentStateFishingWaitEnterPostfix` and `AgentStateFishingWaitPlayPostfix`: split preparation from native advancement. The split exists because earlier instant-bite/energy work found that advancing too early could double-charge or desync the native state. GC risk is lower than Ready/minigame because the work is tied to wait/bite progression, not arbitrary title idle.

`FishingGameScrollBar.StartGamePostfix`: observes the minigame handle and creates per-minigame timing/stats state. This should be paired with `StopGame`.

`FishingGameScrollBar.UpdateGamePrefix` and `UpdateGamePostfix`: run on minigame frames. These are hot paths: input override creation, reflected status/note reads, bonus tap sets, stats, and status strings live here.

`FishingGameScrollBar.StopGamePostfix`: removes minigame state and releases the lifecycle record. This is the expected per-fish cleanup boundary. If this hook is skipped, the service retains the handle until a broader cleanup.

`DolocUserInput` getter prefixes: expose scoped virtual input for Ready/minigame. Current service keeps only one override object, but the getter frequency means override preparation must remain cheap.

`FishRodRenderer.CastHookPostfix`, `PullPostfix`, and `PullCancelPostfix`: adjust physics/result duration for fast animation. They snapshot original physics and restore later. The risk is not unbounded on the normal path, but missed restore boundaries could retain rigidbody keys until reset.

`AgentStateFishingPullEnterPostfix` and `PullExitPostfix`: Pull exit restores animation/physics and publishes a native-exit lifecycle boundary. Important nuance: the boundary currently asserts/logs expected cleanliness; it is not a catch-all clear of minigame dictionaries.

### `FishingAutomationService`

Fields: the map can be divided by lifetime. Owner config/state is process-lifetime and bounded by owner ids. Phase logs, failure throttles, and diagnostics are fixed-key maps reset on save/title. Native transient maps are keyed by borrowed game objects and should clear on StopGame, non-Ready phase, Pull exit restore, disable, save, or title. Pending auto-cast is a single marker.

Constructor: simple dependency capture. No accumulation.

`Update`: calls pending-cast watchdog and auto-cast. Because this is every frame, the service's real safety depends on the gates inside `CheckFishingAutoCastWatchdog` and `UpdateFishingAutoCast`.

`Configure`: normalizes options, stores one state, observes owner/state resources, and logs. This is bounded by owner count. It is not a per-catch leak.

`SetEnabled`: toggles state and on disable calls `ClearFishingAutomationTransientState`. This is a real cleanup path. It intentionally leaves fixed diagnostic/failure maps and owner config/state in place.

`GetState` and `GetStatus`: return current state/status. `GetState` can allocate a default state for an unknown owner, but normal mod polling uses the configured owner.

`TryGetEnabledFishingAutomationOwner`: scans bounded owner states.

`UpdateFishingAutoCast`: eligible-attempt path reflects `DolocAPI`, reads native static members, checks selected rod, scans fishing pools, finds `UseFishRod`, and invokes the native cast owner. Purpose is correct: use the game's own `BodyController.UseFishRod` rather than fake a cast. Risks: reflection and `FindObjectsOfType` on repeated recast attempts, and per-frame pending summary string updates while a cast is pending.

`NotifyFishingPhase`: updates phase and clears Ready charge handles when leaving Ready. This is good lifecycle behavior. It also calls animation-speed logic on Ready/Cast/Pull, which can allocate snapshot helper collections.

`NotifyFishingMiniGameStart`: creates per-minigame timing/stats and observes the minigame native handle. Expected lifetime is one visible minigame.

`PrepareFishingMiniGameAutomationInput`: clears previous override and prepares a new per-frame minigame decision. This is allocation pressure, not long retention.

`ApplyFishingMiniGameAutomationTick`: ensures StartGame was observed, observes result, and clears expired override. This protects against missed StartGame but can also cause extra lifecycle ledger entries.

`NotifyFishingMiniGameStop`: removes minigame timing, bonus taps, stats, completed handles, and override for that handle, then releases lifecycle. This is the per-fish cleanup point. It also publishes a lifecycle boundary, which can allocate diagnostics and feed the resource ledger.

`ApplyFishingWaitAutomation`: handles native wait/reel state. The code is deliberately conservative: OnEnter prepares and OnPlay advances after native readiness. Main GC costs are reflected reads/writes and summary strings during transitions.

`TryForceFishingBite`: instant-bite path can call native roll multiple times in one bite attempt. This is intentional behavior, but should be part of future option-split validation because it increases work per catch.

`TryAdvanceFishingBite` and `TryMirrorNativeFishingWaitNextState`: temporarily write native fields and restore them, or mirror native transition while respecting energy cost. These are correctness-driven reflection paths, not retention paths.

`TryApplyFishingAnimationSpeed`: creates lists/sets of animator candidates and snapshots original speeds. Purpose is reversible fast animation. Normal cleanup is `RestoreExperimentalAnimatorSpeeds`; risk is missed restore boundary or excessive temporary allocation on frequent phase changes.

`ApplyFishingReadyAutomation`: runs every Ready play tick and delegates charge target/speed work. It is a hot path.

`ApplyFishingReadyChargeSpeed`: reads native charge timer/tick, computes desired speed, refreshes UI, and publishes throttled status. It does repeated reflection while charging.

`ApplyFishingReadyChargeTarget`: sets release target and observes the Ready state once, but currently does avoidable work before the already-tracked guard: it refreshes the power bar using a fresh sample list before `fishingReadyChargeTargetStates.Add` can reject repeats. This is a concrete non-semantic optimization candidate.

`TryOverrideFishingReadyChargeInput`: answers native input getter calls while the charge state is current. The tracked state is one handle at a time, so retention is bounded.

`RefreshFishingReadyChargeBar`: reflected UI update. It should stay off paths where no visible refresh is needed.

`AdjustFishingCastHookPhysics` and `TryScaleHookGravity`: snapshot and scale hook velocity/gravity for fast casts. Restore path exists. Native-object-key retention risk depends on restore firing.

`TryPrepareFishingMiniGameAutomationInput`: per-minigame-frame decision logic. It reads native status/note, tracks bonus taps, creates a new override object with an expiry, updates stats, and writes status strings. This is the strongest sustained gameplay-loop allocation site inside AutoFishing itself.

`ObserveFishingMiniGameAutomationResult`: records terminal result and keeps stats until StopGame so final logging has context. This is semantically reasonable, but again StopGame is the cleanup owner.

`TryOverrideFishingMiniGameInput`: uses one scoped override with expiry. It does not accumulate, but it is called from multiple input getter prefixes.

`MarkFishingAutoCastPending`, `ConfirmPendingFishingAutoCast`, `ClearPendingFishingAutoCastCore`, `CheckFishingAutoCastWatchdog`, and `ReleasePendingFishingAutoCastAfterStall`: together prove pending-cast state is designed as one marker, not an accumulating list. The watchdog is a safety boundary after stalls.

`GetFishingAutomationLifecycleSnapshot` and `GetFishingAutomationLifecycleSummary`: build arrays/strings for diagnostics. Useful for review, but potentially expensive when called per boundary/catch.

`NotifyFishingEnvironmentReset`: throttles non-destructive environment-reset lifecycle status to about once per minute. This matches earlier lifecycle review.

`HasFishingPoolInScene`: reflected scene scan using `FindObjectsOfType`. This is not retention, but it is a periodic allocation source while enabled in unsuitable locations or no-water conditions.

`RestoreExperimentalAnimatorSpeeds`: copies snapshot dictionaries to lists, restores native fields, clears maps, and logs. Good cleanup behavior, with bounded temporary allocation.

`NotifyFishingNativeExit`: publishes an expected-clean boundary. It does not itself clear minigame/completed/ready maps. This is fine as an assertion tool, but it means missed earlier hooks can leave state until disable/save/title.

`ResetFishingRuntimeState`: destructive reset. Clears transient dictionaries, diagnostics/failures, phase logs, counters, pending state, smoke overrides, and restores snapshots. This is the broad cleanup boundary.

`ClearFishingAutomationTransientState`: disable cleanup. Clears active native/transient maps and snapshots, but intentionally preserves fixed diagnostic state and owner configuration.

`PublishFishingLifecycleBoundary` and `PublishFishingPendingCastWatchdogBoundary`: diagnostic publication. They build snapshots/details and can amplify allocation pressure if called on every fish or repeated stall.

`ObserveFishingOwnerResource` and `ObserveFishingStateResource`: process-lifetime resource evidence for owner/state. Bounded by owner ids.

`ObserveFishingNativeHandleResource` and `ReleaseFishingNativeHandleResource`: the critical bridge into `ResourceLifecycleLedger`. IDs are strings generated from type/hash, so native objects are not retained directly. But high-churn records accumulate inside the open save generation.

`ReleaseFishingReadyChargeStateResources`: releases Ready charge handles when leaving Ready. Creates a copy set, releases each, then removes handles. Good cleanup; allocation is per phase transition.

`PublishFishingAutomationHookStatus`, `LogFishingAutomation`, `ShouldPublishFishingAutomationDiagnostic`, `RecordFishingAutomationFailure`, and `RecordFishingAutomationSuccess`: fixed-key throttle state. They reduce spam but still allocate before/around the publish decision in several callers.

Nested classes: `FishingMiniGameInputOverride` is a one-frame scoped override; `FishingMiniGameInputStats` is per minigame until StopGame; `FishingAutomationLifecycleSnapshot` is diagnostic payload; failure/diagnostic state classes are fixed-key throttle records.

### `GameBridgeNativeHelpers`

`ResolveType`: scans loaded assemblies when `Type.GetType` fails. Called from AutoFishing hot paths, so lack of caching matters.

`ReadStaticMember`, `ReadMember`, `SetMemberValue`, `WriteMember`, and `FindMethodInHierarchy`: walk reflection metadata on demand. This is acceptable for rare bridge calls but expensive for Ready/minigame/frame loops. Caching reflected members by native type/member name would not change semantics.

### `ResourceLifecycleLedgerService`

`RecordResource` and `ReleaseResource`: normalize resource ids, update or create a ledger record, add timeline entries, increment counts, then build a full snapshot.

`BuildSnapshotNoLock`: copies diagnostics, records, counts, and timeline; sorts all records; and materializes arrays/dictionaries. This is the expensive accumulation point.

`PruneClosedSaveLifetimeRecordsNoLock`: removes save-lifetime records only after the save generation closes. It does not continuously prune released records from the currently open save.

Conclusion: this service is not holding native objects directly, but it can accumulate released AutoFishing evidence records and make every later observe/release more expensive. For high-churn borrowed native handles, the ledger needs a sampling, aggregation, or pruning policy.

### `DtmApiRuntime` And Diagnostics

`ObserveResourceLifecycle` and `ReleaseResourceLifecycle`: call the ledger for every AutoFishing native-handle observe/release.

`PublishResourceLifecycleLedgerUpdate`: formats a summary and publishes feature status on every update. Release operations hit the logging branch. This bypasses the AutoFishing-specific success throttle.

`DiagnosticsService.SetFeatureStatus`: replaces fixed feature keys, so it is not a key leak. The cost is repeated payload creation and string formatting.

### `AutoFishingMod`

`Entry`: reads config, registers input/config/menu, configures bridge, and subscribes events. This is process setup, not a long-loop leak.

`RegisterConfigMenu`: allocates closures/options for config UI. Not a gameplay-loop suspect.

`ConfigureBridge`: creates one options object and calls the bridge. Only config/toggle path.

`OnKeybindPressed`: F6 typed keybind path with release guard. Current design prevents repeated hold toggles.

`OnUpdateTicked`: while enabled, checks manual cancel keys every update. It calls `DtmButton.Parse(key)` for fixed strings on each tick, which is a small but real per-frame allocation/reflection-like parsing risk. Cache parsed buttons after normalization.

`SetAutomation`: enables/disables bridge and registers/disposes manual cancel buttons. Disable clears guards/buttons. Bounded.

`RegisterInputKeys`, `RegisterManualCancelKeys`, `DisposeManualCancelButtons`, and `RefreshToggleReleaseGuard`: input ownership is scoped and bounded after the hotkey fix. The remaining review item is per-frame parsing.

`NormalizeConfig` and `NormalizeKey`: config-only.

### `AutoFishingSmokeCase`

Smoke code is validation workload, not normal player runtime. It polls lifecycle summaries and can generate strings/counters while testing. Do not attribute player GC to smoke-only loops unless smoke mode is enabled.

## GC Risk Ledger

| Risk | Type | Evidence | Normal cleanup | Remaining concern |
| --- | --- | --- | --- | --- |
| `ResourceLifecycleLedger` AutoFishing native records | Diagnostic lifecycle accumulation | SaveLifetime released records remain in the open save generation; full snapshot per observe/release | Save-generation close/title/save reset | Long AFK fishing can create many released string records and increasingly expensive snapshots |
| Minigame handle dictionaries | Native-key retention if hook missed | Start/Update observe handle; Stop removes handle state | `StopGame`, disable, save/title reset | Abnormal native path could skip StopGame; NativeExit only asserts |
| Ready charge state tracking | Native-key retention plus per-frame work | Ready OnPlay observes charge state and refreshes UI | Leaving Ready, disable, reset | Some avoidable list/UI work happens before already-tracked guard |
| Animator/hook physics snapshots | Native-key retention until restore | Fast animation snapshots animator/rigidbody originals | state exit, Pull exit, disable, reset | Missed restore boundary can retain keys until broader cleanup |
| Minigame input override/stats | Sustained allocation | UpdateGame prefix/postfix creates override/status/stats strings | StopGame and override expiry | AFK loop repeats every minigame frame |
| Auto-cast discovery | Periodic allocation | Reflection and `FindObjectsOfType` on eligible attempts | throttles/backoff | Enabled in unsuitable context can keep probing |
| AutoFishingMod manual cancel polling | Small per-frame allocation risk | `DtmButton.Parse` on fixed keys during update | disable stops polling | Cache parsed buttons |
| Diagnostics/status strings | Allocation pressure, not retention | lifecycle/status summaries formatted often | fixed keys replaced | Resource release logs and lifecycle boundaries can be frequent |

## Non-Semantic Stability Options

1. Treat AutoFishing borrowed-native resource records as high-churn evidence. Aggregate by type/phase, sample, or prune released records within the open save instead of keeping every released `SaveLifetime` handle until save generation close.
2. Do not build/publish a full lifecycle ledger snapshot for every short-lived native handle release. Keep counters and publish sampled summaries.
3. Cache reflected native members/types used by AutoFishing hot paths, especially `UseFishRod`, Ready charge fields, minigame status/note fields, and Unity time/fishing pool lookups.
4. Cache or invalidate fishable-water discovery instead of scanning with `FindObjectsOfType` on repeated recast attempts.
5. Move Ready charge's already-tracked guard before UI refresh/sample-list allocation where the visible semantics are unchanged.
6. Cache `DtmButton` values for AutoFishing manual cancel keys after config normalization.
7. Keep `EnvironmentReset` non-destructive. Use explicit native boundaries, disable, save/title, and watchdogs for cleanup instead.

## Rejected Or Unproven Hypotheses

- Rejected for this active-gameplay question: duplicate `LoadGame`, old title-idle registered-string polling, and the YConsole+Zoom title input-pressure island. Those explain the recent main-menu route, not active AutoFishing loops.
- Not recommended: making `EnvironmentReset` destructive for AutoFishing. Prior review correctly rejected that because `DolocAPI.SetEnvCamera` is high frequency and can interrupt valid fishing state.
- Unproven: `FishingAutomationService` native-object dictionaries alone grow without bound on the normal loop. Existing short soak evidence says they return to zero. The open risk is missed native boundaries or diagnostic-ledger growth during a much longer open save.
- Unproven: FastAnimations or CastChargeRatio alone causes GC failure. They do add Ready/Cast/Pull hot-path work and native snapshots, so they should be included as toggles in a future matrix.

## Next Review / Validation Direction

Before implementing gameplay behavior changes, add or inspect low-impact counters that can separate leak from allocation pressure:

- AutoFishing per-N-catch summary: `nativeTransientHandles`, miniGame/ready/animator/hookPhysics counts, pending cast/backoff, confirmed auto-casts, completed minigames.
- ResourceLifecycle summary deltas during one open save: total records, `AutoFishing.*` records, released SaveLifetime records, and release-log count.
- Hot-path counters: `FindObjectsOfType` calls from AutoFishing, reflected `GetMethods/GetField/GetProperty` calls if measurable, and Ready/minigame update counts.
- Scenario split: AutoFishing off baseline, AutoFishing default, default plus FastAnimations, default plus InstantBite, default plus SkipMiniGame, and a no-water/no-rod enabled-idle case.

Likely implementation directions, if this review becomes a goal:

- Collapse or sample AutoFishing native-handle lifecycle ledger records instead of recording every transient handle as a full ResourceLifecycle record during long open saves.
- Avoid publishing full resource lifecycle snapshots/logs on every release-like operation.
- Cache native reflection handles in AutoFishing hot paths where the native owner is stable.
- Move fishable-water discovery to a throttled/cache-invalidated path rather than repeated `FindObjectsOfType` attempts.
- Move Ready charge target's "already tracked" return before avoidable sample-list/UI-refresh work, while preserving current native-owner semantics.

## Methodology Notes

The useful method here is:

1. Separate the reproduced crash class from the new suspicion. AutoFishing was not required for the recent title-idle fatal route, but can still be a long-gameplay suspect.
2. Classify owner and lifetime first: process owner/config, save/native transient, per-frame temporary, diagnostic-only.
3. Distinguish strong-reference retention from allocation/log pressure. Both can hurt GC, but they require different fixes.
4. Trace frequency, not just code existence: every-frame, every Ready frame, every minigame frame, every catch, every save/title boundary.
5. Treat diagnostics as runtime workload. A diagnostic ledger that snapshots, sorts, formats, and logs can become part of the bug it is measuring.
6. Do not fix from smoke success alone. Short soak can prove nominal cleanup, but long-play GC needs cumulative counters over an open save.
