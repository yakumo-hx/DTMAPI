# 20260704-0003 Title Return Boundary Ledger

## Summary

Added the phase 8.5 diagnostic-only title-return boundary ledger for ISSUE-010. The new telemetry connects ReturnHome request/postfix, Core ReturnedToTitle start/end, title-stable observation, next native LoadGame enter/return, SaveLoaded closure, and fatal-window observation. It also captures bounded object graph summaries at the four required boundary snapshots:

- Before ReturnHome.
- After ReturnedToTitle completes.
- Before the next LoadGame.
- After LoadGame prefix/native enter.

Two short slot 3 title-return/next-load smokes passed. The new snapshots did not find an uncleared DTMAPI-owned UI/native transient family in the short window: SaveSlots, EquipmentSlots, AnimalViewer, CustomAnimals controller/bundle caches, AutoFishing transients, AudioReplacement request/clip/callback state, and fallback EventSystem state were bounded or zero after title return. AudioReplacement process-lifetime definitions and platform players remain visible and stable; they are not a growth signal in this evidence.

ISSUE-010 remains open because the long-run Fatal GC class was not mitigated or re-run with long duration in this phase. 8B Registry takeover remains paused until a long gate with this telemetry passes or a fatal recurrence identifies a concrete object family to mitigate.

## Source Request / Goal

- Goal: enter ISSUE-010 phase 8.5, focused on `Title Return -> Next Load` root-cause investigation.
- Required scope: diagnostic-only ledger, snapshots, summaries, and short tests first.
- Hard constraints preserved:
  - Registry takeover stayed paused.
  - No CustomAnimals, AnimalVoice, or AutoFishing behavior changed.
  - No public API, JSON field, or content pack path changed.
  - No unknown or native-owned Unity object was destroyed or unloaded.
  - No long test was run before short diagnostics had evidence.

## Known Facts Before This Change

- `GAME-SMOKE/20260704-033314` reproduced `Fatal error in GC / Unexpected mark stack overflow` at the first post-idle native LoadGame, before SaveLoaded, with `requests=1`, `duplicateRequests=0`, `nativeEnter=1`, `nativeReturn=0`, and `saveLoaded=0`.
- `GAME-SMOKE/20260704-060648` completed cycle 1, returned to title, then reproduced Fatal GC on cycle 2 before SaveLoaded, with `requests=2`, `duplicateRequests=0`, `nativeEnter=2`, `nativeReturn=1`, and `saveLoaded=1`.
- `GAME-SMOKE/20260704-081844` published exactly 600 `Smoke.SaveLoadCycle=pending` statuses and then loaded slot 3 once successfully, so dense smoke pending publication alone is unlikely to be the trigger.
- The active suspect class was title-return-to-next-native-LoadGame object graph/lifecycle state, not duplicate SaveLoad requests.

## Rejected Hypotheses Before This Change

- Duplicate DTMAPI/smoke LoadGame requests are not present in the latest fatal samples.
- Dense `Smoke.SaveLoadCycle=pending` publication is not a necessary condition for the latest Fatal GC class.
- Registry diffs, title-idle resource generation growth, Hook reinstall loops, and content-manifest diagnostic mismatches were not implicated by the latest evidence.
- It would be premature to destroy/unload AssetBundles, RuntimeAnimatorControllers, AudioClips, EventSystems, or native UI objects without first proving a DTMAPI-owned retained family.

## Changed Files

- `src/DTMAPI.Core/Runtime/TitleReturnBoundaryLedgerService.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/SaveLoadRequestCoordinatorService.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Features.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/AnimalViewerService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260704-0003-title-return-boundary-ledger.md`

## Behavior

- Added bounded `TitleReturnBoundaryLedgerService` diagnostics with event and snapshot limits.
- Added runtime report lines:
  - `TitleReturnBoundaryLedger`
  - `TitleReturnObjectGraphSnapshots`
- Added Hook status and lifecycle diagnostics under `Refactor.TitleReturnBoundaryLedger`.
- Added ReturnHome prefix observation under `GameLoop.ReturnHomeRequested`; the existing ReturnHome postfix cleanup order is unchanged.
- Added Bootstrap object graph summaries for Title Settings UI and Debug Console UI, including persistent roots, panel/root state, event binders, input fields, fallback EventSystem root/component, static binders, and icon sprite state.
- Added GameBridge object graph summaries for SaveSlots, EquipmentSlots, AnimalViewer, CustomAnimals, AudioReplacement, AutoFishing, owner counters, resource ledger, SaveLoad request state, and dispose graph state.
- Added per-feature summary methods for AnimalViewer, CustomAnimals animator/controller/bundle state, and AudioReplacement entry/request/clip/callback/player state.
- Corrected `Refactor.SaveLoadBoundary` to use active/latest request state instead of historical `SaveLoadedDispatchCount > 0`.
  - No request: `idle`.
  - Active native enter without return/SaveLoaded: `loading`.
  - Latest closed request: `closed`.
  - Fatal during active/latest request: `fatal`.
- Added smoke result parsing for `TitleReturnBoundaryLedger` and `TitleReturnBoundarySummary`.

## Community Checks

| Finding or suspect | Search terms and location | Links | Conclusion | Effect on testing |
| --- | --- | --- | --- | --- |
| `Unexpected mark stack overflow` can be associated with large reachable Unity object graphs or GC handle/root pressure. | `Unity "Unexpected mark stack overflow" GC GameObject hierarchy root set`; Unity Issue Tracker and Unity Discussions. | <https://issuetracker.unity.com/issues/5585>, <https://discussions.unity.com/t/il2cpp-crash-at-malloc/849108?page=3>, <https://discussions.unity.com/t/fatal-error-in-gc-unexpected-mark-stack-overflow/760479> | Supports treating the crash as object-graph/root-pressure evidence until local snapshots prove otherwise. It does not identify DTMAPI as the cause by itself. | Added boundary snapshots and did not close as "native Unity" without DTMAPI-owned object counts. |
| Static roots and GameObject hierarchy can keep Unity assets reachable. | `Unity Resources.UnloadUnusedAssets static variables GameObject hierarchy unused assets docs`; Unity docs. | <https://docs.unity3d.com/2023.2/Documentation/ScriptReference/Resources.UnloadUnusedAssets.html> | Unity's unused-asset scan considers GameObject hierarchy and static variables, so process-long Bootstrap/GameBridge references are relevant evidence. | Included Bootstrap UI roots, fallback EventSystem state, GameBridge feature roots, resource ledger, and owner ledger counts in snapshots. |
| `DontDestroyOnLoad` preserves roots and their children across scene loads. | `Unity DontDestroyOnLoad object will not be destroyed when loading new scene docs`; Unity docs and community. | <https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Object.DontDestroyOnLoad.html>, <https://stackoverflow.com/questions/36163574/duplicates-because-of-dontdestroyonload> | Persistent Bootstrap UI roots are valid but must be counted, because they survive scene transitions with child transforms. | Added Title Settings and Debug Console lifecycle summaries instead of assuming GameBridge final health covers Bootstrap roots. |
| EventSystem should be singular and retained/destroyed deliberately. | `Unity DontDestroyOnLoad EventSystem duplicate EventSystem UI retained references`; Unity docs and Unity Discussions. | <https://docs.unity3d.com/530/Documentation/ScriptReference/EventSystems.EventSystem.html>, <https://discussions.unity.com/t/event-system-issue/910015> | Fallback EventSystem state is a plausible UI-boundary risk if duplicated or retained under DTMAPI roots. | Snapshots include fallback EventSystem root/component for Bootstrap UI; short tests saw no fallback EventSystem retained after title return. |
| UnityEvent/listener lifetime can keep subscribers alive when owner/subscriber lifetimes differ. | `UnityEvent persistent listener delegate memory leak destroyed GameObject`; Unity Discussions and StackOverflow. | <https://discussions.unity.com/t/do-i-need-to-unsubscribe-my-event-listeners/836703>, <https://discussions.unity.com/t/do-i-need-to-unsubscribe-if-an-object-containing-event-handler-is-destroyed/829956>, <https://stackoverflow.com/questions/52412904/is-listener-of-onclick-destroyed-when-button-is-destroyed> | UI event binders are a valid snapshot target when managed owners survive longer than cloned/destroyed UI. | Snapshots count SaveSlots, EquipmentSlots, Bootstrap UI, and Debug Console binders; short tests showed SaveSlots/EquipmentSlots binder cleanup reached zero after title return. |
| AssetBundle unload/reload policy can duplicate or break live Unity objects. | `Unity AssetBundle.Unload loaded objects duplicate memory docs`; Unity docs. | <https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetBundle.Unload.html> | Destroying/unloading bundle-owned objects without a proven stale DTMAPI-owned family is risky. | Kept CustomAnimals behavior unchanged; added controller/bundle cache counts and non-null counts only. |
| UnityWebRequest audio handlers create and hold AudioClip-backed native/Unity objects. | `UnityWebRequestMultimedia GetAudioClip Dispose AudioClip Unity docs`; Unity docs. | <https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Networking.UnityWebRequestMultimedia.GetAudioClip.html>, <https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Networking.DownloadHandlerAudioClip.html> | Pending requests, async operations, clips, callback owners, and player objects are a valid object-family count for title-return snapshots. | Added AudioReplacement lifecycle summary. Short tests showed ready entries and platform players stable, with zero pending requests, async operations, AudioClips, callback owners, or animal contexts after title return. |
| Mono/Boehm root processing makes registered roots and mark/trace stacks relevant to crash triage. | `Mono Boehm GC mark stack overflow GC_push_all roots handle stack`; GitHub Mono and Boehm GC references. | <https://github.com/mono/mono/blob/main/mono/metadata/boehm-gc.c>, <https://www.mono-project.com/docs/advanced/garbage-collector/sgen/>, <https://www.hboehm.info/gc/gcdescr.html> | This supports root-set/object-graph instrumentation but does not prove a DTMAPI leak. | The phase records object-family counts before and after title-return boundaries instead of adding speculative cleanup. |

## Validation

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; restore/build emitted restricted-network `NU1900` package-vulnerability feed warnings only.
- `git diff --check`: passed; Git reported line-ending normalization warnings only.
- The shared runtime lock was acquired before installing/running local runtime smoke tests and released afterward.

### Short Test 1: Slot 3 Load, Return Title, Load Again, Repeat 3x

- Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseSaveLoadCycle -SaveLoadCycleCount 3 -SaveLoadCycleInitialTitleIdleSeconds 5 -SaveLoadCycleIntervalSeconds 5 -SaveLoadCycleInSaveSeconds 5 -TimeoutSeconds 520 -SkipBuild`
- Evidence: `docs/debug/evidence/GAME-SMOKE/20260704-142213`
- Result: passed.
- Key fields: `RunStatus=Passed`, `SaveLoadCycle=Passed`, `SaveLoaded=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, `SaveLoadBoundary=Passed`, `TitleReturnBoundaryLedger=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- SaveLoad summary: `requests=3; active=none; duplicateRequests=0; suppressedDuplicates=0; nativeEnter=3; nativeReturn=3; saveLoaded=3; timeouts=0; fatalWindows=0`.
- Title-return summary: `currentBoundary=TR-0004; events=34; objectSnapshots=14; fatalEvents=0; latestEvent=TitleStable; latestSnapshot=AfterReturnedToTitleComplete`.
- Object-family evidence after title return: SaveSlots states/pagers/binders `0`; EquipmentSlots clones/binders/storage owners `0`; AnimalViewer native data/overlay rows/objects `0`; CustomAnimals controller/bundle caches `0`; AudioReplacement pending requests/async operations/clips/callback owners/animal contexts `0`; AutoFishing native transient handles `0`; Bootstrap fallback EventSystem absent.
- Note: lifecycle contract summary retained a known diagnostic warning for repeated SaveLoaded classification in a multi-cycle smoke. The run still passed the SaveLoad request coordinator and title-return ledger gates.

### Short Test 2: Slot 3 Load, Return Title, Wait 60s, Load Again

- Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseSaveLoadCycle -SaveLoadCycleCount 2 -SaveLoadCycleInitialTitleIdleSeconds 5 -SaveLoadCycleIntervalSeconds 60 -SaveLoadCycleInSaveSeconds 5 -TimeoutSeconds 760 -SkipBuild`
- Evidence: `docs/debug/evidence/GAME-SMOKE/20260704-142402`
- Result: passed.
- Key fields: `RunStatus=Passed`, `SaveLoadCycle=Passed`, `SaveLoaded=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, `SaveLoadBoundary=Passed`, `TitleReturnBoundaryLedger=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Cycle summary: `cycles=2, slot=3, initialIdleSeconds=5, intervalSeconds=60, inSaveSeconds=5, elapsedSeconds=113`.
- SaveLoad summary: `requests=2; active=none; duplicateRequests=0; suppressedDuplicates=0; nativeEnter=2; nativeReturn=2; saveLoaded=2; timeouts=0; fatalWindows=0`.
- Title-return summary: `currentBoundary=TR-0003; events=24; objectSnapshots=10; fatalEvents=0; latestEvent=TitleStable; latestSnapshot=AfterReturnedToTitleComplete`.
- Object-family evidence after title return matched the first short test: DTMAPI-owned UI clone/binder/transient families were zero or bounded, and AudioReplacement retained only stable title/process lifetime definitions and platform players.

### Targeted Short Test Decision

No extra targeted short test was run because the required short tests did not identify a specific uncleared object family. The visible retained families were intentional process/title-lifetime state:

- Bootstrap Title Settings and Debug Console roots are persistent but inactive at title, with no fallback EventSystem retained.
- AudioReplacement retained `entries=11`, `readyEntries=11`, and `platformPlayers=11`, while pending requests, async operations, AudioClips, callback owners, and animal contexts were zero.
- Owner and resource ledgers stayed bounded and reported no cleanup failures, resource errors, or restart-required native-owner releases.

## Interpretation

- The new telemetry proves the short title-return/next-load boundary can converge cleanly for slot 3.
- The latest short evidence does not implicate SaveSlots, EquipmentSlots, AnimalViewer, CustomAnimals controller/bundle caches, AudioReplacement pending/request/clip/callback state, AutoFishing transients, Bootstrap fallback EventSystem, duplicate LoadGame requests, or smoke pending pressure.
- The evidence does not yet clear the long-run Fatal GC class, because the original failures occurred only after long idle/cycle windows and this phase intentionally prioritized short tests.
- The best next evidence is a long gate using the new `TitleReturnBoundaryLedger` snapshots, not a speculative cleanup pass.

## 8B Registry Status

8B Registry takeover should not continue yet. Phase 8.5 added the necessary title-return boundary telemetry and passed short tests, but it did not prove the long-run Fatal GC fixed or identify a concrete object family to mitigate. Continue 8B only after one of these is true:

- A long title-return/periodic gate passes with `TitleReturnBoundaryLedger=Passed`, no Fatal GC, and bounded object snapshots.
- A long gate reproduces Fatal GC and the ledger identifies a concrete DTMAPI-owned object family to fix before registry takeover resumes.

## Rollback Notes

- The change is diagnostic-only. To roll it back, remove the `TitleReturnBoundaryLedgerService`, its runtime report/hook-status publication, the ReturnHome prefix diagnostic hook, and the smoke parser/result fields.
- Do not roll back the corrected `SaveLoadRequestCoordinatorService.BoundaryStatus` unless a replacement continues to model active/latest request state; the old `SaveLoadedDispatchCount > 0` heuristic misclassified active failed loads after a previous successful load.
- Do not add asset unload/destroy behavior as rollback. This phase deliberately avoided native-owned or unknown Unity object release.

## Follow-Up

- Keep ISSUE-010 open.
- Preserve `TitleReturnBoundaryLedger`, `TitleReturnObjectGraphSnapshots`, and `TitleReturnBoundarySummary` in all future crash/smoke packages.
- The next long evidence should inspect snapshot deltas at `AfterReturnedToTitleComplete`, `BeforeNextLoadGame`, and `LoadGameNativeEnter` before adding mitigation.
- If Fatal GC recurs with the new telemetry, prioritize the object family whose count changes or remains unexpectedly live across title return. If no DTMAPI-owned family changes, escalate to native crash dump/root-set analysis with the ledger as the boundary map.
