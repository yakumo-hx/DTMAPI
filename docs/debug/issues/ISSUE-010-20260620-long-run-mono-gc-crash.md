# ISSUE-010: Long-Run Unity/Mono GC Crash

## Current Status

- Status: open / main-menu long-idle input-pressure path mitigated 2026-07-08 / FullKnown Steam one-hour-title plus 10 one-minute save-load cycles passed 2026-07-08 / long-term gameplay GC class still open / post-one-hour-title-idle Fatal reproduced 2026-07-05/06 / duplicate LoadGame ruled out in latest samples / per-owner deltas added / interrupted 10-minute-plus-five-minute cadence passed / Phase 8.7 UI split passed / Phase 8.10 captured a second comparable no-probe full fatal before SaveLoaded / Phase 8.11 Lite shifted the fatal window to post-SaveLoaded, pre-native-return LoadGame continuation / Phase 8.12 no-HookProbe+Lite still reproduced before SaveLoaded / Phase 8.14 no-HookProbe+Lite reproduced before SaveLoaded again and captured a DbgHelp full live dump / Phase 8.15 UiRuntime isolation shifted the fatal to post-SaveLoaded Hook.Exit / Phase 8.16 native-continuation probe reproduced before SaveLoaded and before VersionPatcher/AfterLoadArchiveData/MapManager breadcrumbs / Phase 8.17 removed four UI code mods and passed twice / Phase 8.18A added back YConsole+Zoom and reproduced before SaveLoaded / Phase 8.19 Zoom-only and YConsole-only passed once, while pair confirmation reproduced before SaveLoaded / Phase 8.20 YConsole+Zoom NoInput passed twice under DirectExe fallback / Phase 8.21 ZoomNoInput and YConsoleNoInput each passed twice, making input roots a threshold-pressure suspect rather than a single-owner proof / Phase 8.22 proved old per-registered-string polling with 50 virtual keys is a 20-minute crash amplifier / hotkey rebuild and edge follow-up removed ordinary title-idle polling, restored native-feel short taps, passed 50-key pressure, original YConsole+Zoom, no-virtual long routes, and a FullKnown Steam one-hour-plus-ten-cycle route
- 2026-07-11 scope note: inactive AutoFishing sustained retention is no longer an active suspect; active fishing-loop retention and the broader long-term gameplay/native GC class remain open.
- Opened: 2026-06-20 +08:00
- Symptom: long play can end in Unity native crash or `Fatal error in GC / Unexpected mark stack overflow`.
- Scope: DTMAPI runtime lifecycle, GameBridge feature roots, UI clone/binder cleanup, AutoFishing state progression, and log export coverage.

## Known Facts

- Player logs from `D:\下载\DTMAPI-logs\20260619-131746` show a normal DTMAPI startup followed by a long play session and Unity/Mono native crash evidence.
- The crash is not currently explained by a managed DTMAPI exception.
- Older exported reports did not include Unity native crash dump directories under `%TEMP%\RedSawGames\DolocTown\Crashes`, which blocks precise native-object/root-set analysis.
- Code review found concrete DTMAPI-owned object-retention risks:
  - SaveSlots pager UI state can hold native panels and UnityEvent callbacks past title/environment boundaries.
  - EquipmentSlots can hold cloned UI objects and event binders unless render-time cleanup runs.
  - `SetEnvCamera -> EnvironmentReset` is high-frequency and fanouts all GameBridge features, including forced machine polling.
  - AutoFishing can log `auto-cast invoked` without proving native fishing-state progression.

## Rejected Hypotheses

- This is not proven to be a DTMAPI startup bug: the reviewed log starts normally.
- This is not proven to be Qiuzy/"enhanced features" or audio replacement: the reviewed 2026-06-19 package did not load Qiuzy, and audio replacement activity was too small to explain the GC crash.
- This is not proven to be a direct C# infinite recursion: parallel code review did not find a direct recursive stack-overflow path.

## Current Attempt

2026-07-08 AutoFishing lifecycle diagnostics pressure reduction:

- Implemented source-level ResourceLifecycleLedger pressure controls for the AutoFishing boundary plan. This is diagnostics lifecycle reduction only; it does not mark ISSUE-010 solved and does not claim a long-term gameplay GC fix.
- Ledger snapshots now expose/report `recordCount`, `releasedCurrentSaveRecords`, `snapshotBuilds`, `publishCountByArea`, `skippedRefreshCalls`, repeated skipped-refresh fast-path count, and AutoFishing record count.
- Repeated skipped refreshes with unchanged `phase + area + result + resourceCount + contentGeneration` now take a no-publish/no-snapshot fast path.
- AutoFishing `BorrowedNative + SaveLifetime` resources are aggregated by high-churn kind and lifecycle phase, such as `AutoFishing.MiniGameHandle`, `AutoFishing.ReadyChargeState`, `AutoFishing.AnimatorSnapshot`, and `AutoFishing.HookPhysicsSnapshot`, with observed/released counts plus first/last sample ids instead of one full long-lived ledger record per native handle.
- High-churn release publication is sampled, while failures, warnings, cleanup, and generation-boundary updates still publish immediately.
- FishingAutomation now registers an inactive facade at startup and creates service/runtime state only after a consumer calls `Configure` or `SetEnabled(true)`. Inactive status is `inactive/no-consumer`; configured/enabled/disabled consumer states are `configured`, `active`, and `disabled/consumer-present`.
- `FishingAutomationOptions.StopOnManualMove` was retired from the experimental GameBridge DTO. F6 and manual movement cancel remain AutoFishingMod-owned product behavior.
- Source validation passed with `$env:DOTNET_ROLL_FORWARD='Major'; tools/scripts/test.ps1 -Configuration Release` because the local host has .NET 6 and 9 but no .NET 8 runtime.
- Runtime inactive/no-consumer evidence `docs/debug/evidence/GAME-SMOKE/20260708-132338` passed with AutoFishing disabled. `Fishing.Automation` reported `inactive/no-consumer`, hook install signals did not require `Fishing.Automation`, and the final resource ledger had `autoFishingRecords=0`.
- Fifth-save AutoFishing product smokes passed after the no-charge product change: `DefaultLoop` `GAME-SMOKE/20260708-143733`, `InstantBite` `20260708-143905`, `SkipMiniGame` `20260708-144029`, and final `FastAnimations` `20260708-144556`. Retained failed `FastAnimations` run `20260708-144151` caught two fixed issues: smoke false success from accepting `WaitingAction` as native Wait, and Ready charge speed still ticking `_castTimer` even when product charge was disabled.
- Longer AutoFishing soak `GAME-SMOKE/20260708-144724` passed with a normal loop (`soakLoops=8/8`, `autoCast=0->10`, `miniGameComplete=0->9`) and bounded diagnostics (`recordCount=6`, `autoFishingRecords=4`, `snapshotBuilds=51`, `skippedRefreshCalls=380`, `skippedRefreshFastPath=374` in the final health snapshot). This is evidence that the diagnostics ledger is no longer adding one full AutoFishing native-handle record per fish in this route.
- Product `AutoFishingCastCharge` is now intentionally skipped because first-party AutoFishing forces `CastChargeRatio=0` to avoid the observed over-charge/out-of-water cast. Positive `CastChargeRatio` remains an experimental GameBridge API path covered by unit tests and historical `GAME-SMOKE/20260613-171405`, not a current product smoke requirement.

2026-07-05 phase 8.6 SaveLoad cycle accumulation and feature bisection:

- Added a bounded per-cycle object delta ledger on top of `TitleReturnBoundaryLedger`. It now records same-boundary and previous-snapshot metric deltas at SaveLoaded, ReturnedToTitle, next LoadGame, and native LoadGame enter boundaries.
- Added smoke/report field `SaveLoadCycleObjectDeltaLedger` plus `SaveLoadCycleObjectDeltaSummary`, and added Bootstrap/GameBridge lifecycle sections to object graph snapshots.
- Added smoke `OfficialModProfile` support for `CoreOnly`, `CoreUi`, `CoreCustomAnimals`, `CoreAutoFishing`, and explicit extra enabled IDs, with `SAVE/mod_infos.json` backup/restore evidence.
- Fixed one definite DTMAPI-owned residual: stale string-only `ResourceLifecycle` `SaveLifetime` diagnostic records from older save generations are now pruned when later save generations close. Evidence `docs/debug/evidence/GAME-SMOKE/20260705-013446` passed and showed older save diagnostics being pruned; a later current/default fatal repro proves this cleanup is not the crash root cause.
- Source/community checks are recorded in `docs/reviews/code/2026/20260705-0001-phase86-saveload-cycle-accumulation-audit.md`: Unity managed-shell and `Object.Destroy` docs support count-first/ownership-first cleanup; Unity mark-stack overflow reports support object-graph pressure triage; Unity event discussions support event/input owner counters; Harmony and BepInEx docs support process-long patch/plugin roots.
- No-idle current/default 20-cycle evidence `docs/debug/evidence/GAME-SMOKE/20260704-184517` passed with `requests=20`, `nativeEnter=20`, `nativeReturn=20`, `saveLoaded=20`, and `duplicateRequests=0`. This rules out SaveLoad cycle count alone as sufficient in the tested window.
- Current/default one-hour-idle evidence `docs/debug/evidence/GAME-SMOKE/20260704-185211` failed on the third native LoadGame after idle. Current/default evidence after the ResourceLifecycle prune, `docs/debug/evidence/GAME-SMOKE/20260705-013648`, failed on the first post-idle load after `SaveLoaded` but before native return. Both had duplicate requests ruled out.
- `CoreOnly` one-hour-idle evidence `docs/debug/evidence/GAME-SMOKE/20260704-201341` passed 6 cycles, proving core runtime plus one-hour idle was not enough.
- Individual feature-family bisection passed:
  - `CoreCustomAnimals` one-hour-idle evidence `docs/debug/evidence/GAME-SMOKE/20260704-214043` passed.
  - `CoreAutoFishing` one-hour-idle evidence `docs/debug/evidence/GAME-SMOKE/20260704-225729` passed.
  - `CoreUi` one-hour-idle evidence `docs/debug/evidence/GAME-SMOKE/20260705-001613` passed.
  - Action/utility-only evidence `docs/debug/evidence/GAME-SMOKE/20260705-023843` passed.
  - CustomAnimals plus action/utility evidence `docs/debug/evidence/GAME-SMOKE/20260705-034457` passed.
  - CustomAnimals plus action/utility plus Manbo audio evidence `docs/debug/evidence/GAME-SMOKE/20260705-045059` passed.
- Minimal reproduced profile in this pass: `docs/debug/evidence/GAME-SMOKE/20260705-055655` enabled CustomAnimals/AnimalVoice packs, action/utility mods, Manbo audio, and UI mods, while AutoFishing remained disabled. It reproduced `Fatal error in GC / Unexpected mark stack overflow` on first post-idle native LoadGame before SaveLoaded: `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, `duplicateRequests=0`.
- Latest object-delta conclusion: DTMAPI-owned ReturnedToTitle transients are bounded. SaveSlots, EquipmentSlots, AnimalViewer, AudioReplacement runtime requests/clips/callbacks, CustomAnimals controller/bundle caches, and AutoFishing native transients all return to zero in the latest profiles. The failing profile instead has a larger stable process/title graph: `ModOwner.records=119`, `EventHandler=28`, `InputButton=9`, AudioReplacement `entries/platformPlayers=11`, CustomAnimals `registrations=10`, and GameBridge feature states/statuses/features `14`.
- Classification: ISSUE-010 is most consistent with one-hour-title-idle plus a larger stable process/title object graph, not duplicate `LoadGame` and not cycle count alone. The next target is the UI group inside the failing profile: split Zoom, MoreSaves, MoreEquipmentSlots, and YConsole one at a time on top of the passing CustomAnimals+action+Manbo profile.

2026-07-05 per-owner delta and idle-cadence follow-up:

- Added per-owner root diagnostics before the requested reruns. Object graph snapshots and deltas now expose:
  - `ModOwner.byOwner.*` for records, `EventHandler`, `InputButton`, `ConfigPage`, and `LoadedCodeMod`.
  - `OwnerRoots.input.byOwner.*`, `OwnerRoots.events.byOwner.*`, and `OwnerRoots.configPages.byOwner.*`.
  - `UI.byOwner.*` and Bootstrap `uiByOwner` for `Canvas`, `EventSystem`, `Button`, `InputField`, `ScrollRect`, `UnityEventListeners`, `DynamicBinders`, and `rootAlive`.
  - `GameBridge.featureById.*` for registered/status/runtime-state/failure/update/lifecycle counters.
  - `ownerPrevDelta`, `ownerSameBoundaryDelta`, and `ownerNonZero` in `SaveLoadCycleObjectDeltaSummary`, so owner fields are visible at `BeforeNextLoadGame` and `LoadGameNativeEnter`.
- Source/community checks align with this diagnostic approach: Unity documents runtime `UnityEvent` listener removal and managed-shell object retention; Unity mark-stack overflow reports support object-graph/root-pressure triage; Harmony patch edge-case docs support treating hooks as process-long roots rather than unpatching during title/save. See `docs/updates/2026/20260705-0002-saveload-per-owner-delta-cadence.md`.
- Per-owner no-idle rerun `docs/debug/evidence/GAME-SMOKE/20260705-075428` passed the minimal reproduced profile for 20 cycles with `requests=20`, `nativeEnter=20`, `nativeReturn=20`, `saveLoaded=20`, `duplicateRequests=0`, `fatalWindows=0`, and owner delta summaries present.
- Interrupted-idle cadence run `docs/debug/evidence/GAME-SMOKE/20260705-080223` passed the same minimal profile with `initialIdleSeconds=600`, `intervalSeconds=300`, `cycles=20`, and `elapsedSeconds=6535`. It ended with `requests=20`, `nativeEnter=20`, `nativeReturn=20`, `saveLoaded=20`, `duplicateRequests=0`, `fatalWindows=0`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`.
- New classification: cumulative wall-clock time and SaveLoad loop count are still insufficient when title idle is interrupted every five minutes. The next repro should use the continuous one-hour title-idle route for UI-owner split tests, because the 10-minute/five-minute cadence did not reproduce even after roughly 109 minutes.
- Current per-owner observations in the minimal profile:
  - `DTMAPI.DebugConsoleMod`: `EventHandler=4`, `InputButton=2`, `ConfigPage=1`, `LoadedCodeMod=1`.
  - `DTMAPI.ZoomMod`: `EventHandler=3`, `InputButton=5`, `ConfigPage=1`, `LoadedCodeMod=1`.
  - `DTMAPI.MoreEquipmentSlotsMod`: `EventHandler=1`, `ConfigPage=1`, `LoadedCodeMod=1`; EquipmentSlots UI entries appear in-save and return to zero at title.
  - `DTMAPI.MoreSavesMod`: `ConfigPage=1`, `LoadedCodeMod=1`; SaveSlots UI pager/binder counts remain zero at title in the latest runs.

2026-07-05 phase 8.7 UI owner split and PreLoad GC probe:

- Added smoke-only `-AutoExercisePreLoadGcProbe`. The switch requires `-AutoExerciseSaveLoadCycle`; when enabled, the harness records `BeforePreLoadForcedGC`, runs `GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();`, records `PreLoadForcedGC`, and then continues into the first native `LoadGame`. It only runs before the first post-idle LoadGame, not the second ReturnHome follow-up load.
- Key mapping for the requested Bootstrap fields: current logs expose `Bootstrap.TitleSettings.rootAlive` / `Bootstrap.DebugConsole.rootAlive` as `BootstrapUi.titleSettings.rootAlive` / `BootstrapUi.debugConsole.rootAlive`, and also as `UI.byOwner.DTMAPI.TitleSettings.rootAlive` / `UI.byOwner.DTMAPI.DebugConsole.rootAlive`.
- Short probe validation `docs/debug/evidence/GAME-SMOKE/20260705-101446` passed with `PreLoadForcedGCProbe=Passed`, `BeforePreLoadForcedGC` and `PreLoadForcedGC` snapshots present, `SaveLoadCycle=Passed`, `NoFatalInstanceWindow=Passed`, and clean process exit.
- Phase 8.7 fixed base B was `CoreCustomAnimals` plus `Workshop.3742763309`, `Workshop.3742763843`, `Workshop.3742763540`, `Workshop.3742763706`, and `Local.Yuuka_DTMAPI_ManboCardboardAudio`; AutoFishing remained disabled. All profile summaries were checked for target IDs and AutoFishing absence.
- Single UI owner results, all using one-hour title idle and at most two LoadGames:
  - B + YConsole `docs/debug/evidence/GAME-SMOKE/20260705-101737`: passed. At `LoadGameNativeEnter`: `ModOwner.records=98`, `EventHandler=24`, `InputButton=4`, `ConfigPage=7`, `LoadedCodeMod=10`; `DTMAPI.DebugConsoleMod={records=10, EventHandler=4, InputButton=2, ConfigPage=1, LoadedCodeMod=1}`.
  - B + MoreEquipmentSlots `docs/debug/evidence/GAME-SMOKE/20260705-111900`: passed. At `LoadGameNativeEnter`: `ModOwner.records=93`, `EventHandler=21`, `InputButton=2`, `ConfigPage=7`, `LoadedCodeMod=10`; `DTMAPI.MoreEquipmentSlotsMod={records=5, EventHandler=1, InputButton=0, ConfigPage=1, LoadedCodeMod=1}`.
  - B + MoreSaves `docs/debug/evidence/GAME-SMOKE/20260705-122003`: passed. At `LoadGameNativeEnter`: `ModOwner.records=92`, `EventHandler=20`, `InputButton=2`, `ConfigPage=7`, `LoadedCodeMod=10`; `DTMAPI.MoreSavesMod={records=4, EventHandler=0, InputButton=0, ConfigPage=1, LoadedCodeMod=1}`.
  - B + Zoom `docs/debug/evidence/GAME-SMOKE/20260705-132240`: passed. At `LoadGameNativeEnter`: `ModOwner.records=100`, `EventHandler=23`, `InputButton=7`, `ConfigPage=7`, `LoadedCodeMod=10`; `DTMAPI.ZoomMod={records=12, EventHandler=3, InputButton=5, ConfigPage=1, LoadedCodeMod=1}`.
- Because all four singles passed, ran the requested high-value pairs:
  - B + YConsole + MoreEquipmentSlots `docs/debug/evidence/GAME-SMOKE/20260705-142438`: passed. At `LoadGameNativeEnter`: `ModOwner.records=103`, `EventHandler=25`, `InputButton=4`, `ConfigPage=8`, `LoadedCodeMod=11`.
  - B + YConsole + MoreSaves `docs/debug/evidence/GAME-SMOKE/20260705-152600`: passed. At `LoadGameNativeEnter`: `ModOwner.records=102`, `EventHandler=24`, `InputButton=4`, `ConfigPage=8`, `LoadedCodeMod=11`.
  - B + MoreSaves + MoreEquipmentSlots `docs/debug/evidence/GAME-SMOKE/20260705-162708`: passed. At `LoadGameNativeEnter`: `ModOwner.records=97`, `EventHandler=21`, `InputButton=2`, `ConfigPage=8`, `LoadedCodeMod=11`.
- Every single and pair ended with `requests=2`, `nativeEnter=2`, `nativeReturn=2`, `saveLoaded=2`, `duplicateRequests=0`, `fatalWindows=0`, `RunStatus=Passed`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`.
- Delta reading: `LoadGameNativeEnter` and `AfterReturnedToTitleComplete` `ownerPrevDelta` did not show `ModOwner`, `OwnerRoots`, or `UI.byOwner` root growth in the single/pair runs. The remaining changes were SaveLoad request counters and expected GameBridge dispatch/lifecycle counters, which are not alone treated as leaks.
- Classification: Phase 8.7 enters situation C. No tested single UI owner or requested high-value pair is a sufficient reproducer, and no definite DTMAPI-owned stale UI/event/input/config root was found to fix. Stop UI guessing and escalate to full-profile native/root-set analysis while preserving the DTMAPI ledger as the boundary map.
- Source/community checks recorded in `docs/reviews/code/2026/20260705-0003-phase87-ui-owner-split-preload-gc.md` support this approach: Unity managed memory and `UnloadUnusedAssets` docs support root/reference counting before native destruction; UnityEvent docs support listener cleanup when DTMAPI owns listeners; BepInEx/Harmony docs support treating patches as process-long roots; Unity mark-stack overflow reports support native/root-set escalation once owner deltas stay bounded.

2026-07-05 phase 8.8 full profile fatal-window/root-set capture:

- Stopped UI single/pair bisection completely. Phase 8.8 used only the full known-failing profile: `CoreCustomAnimals`, action/utility owners, Manbo audio, and all four UI owners; AutoFishing remained disabled.
- Original known-failing evidence `docs/debug/evidence/GAME-SMOKE/20260705-055655` used `IncludeHookProbe=True`, so Phase 8.8 retained `-IncludeHookProbe` for baseline parity.
- Added smoke-only `-FatalWindowCrashDumpGraceSeconds`, default `0`, and used `-FatalWindowCrashDumpGraceSeconds 30` plus `-TimeoutSeconds 6000` for Phase 8.8. On fatal window detection, the harness now immediately records live fatal-window/process/log-position evidence, waits grace, collects logs while the process is alive, closes/kills the process, waits 20 seconds, collects crash directories again, and records whether Unity crash evidence came from live collect, post-close collect, or was missing.
- Profile validation was mandatory for every run. `docs/debug/evidence/GAME-SMOKE/20260705-184626`, `docs/debug/evidence/GAME-SMOKE/20260705-194759`, and `docs/debug/evidence/GAME-SMOKE/20260705-205236` all had exactly the full extra IDs plus the expected `CoreCustomAnimals` local animal packs enabled, AutoFishing disabled, and no unexpected enabled IDs. Each profile summary is valid for ISSUE-010 conclusions.
- Full no-probe attempt 1 `docs/debug/evidence/GAME-SMOKE/20260705-184626` passed: `requests=2`, `nativeEnter=2`, `nativeReturn=2`, `saveLoaded=2`, `duplicateRequests=0`, `fatalWindows=0`, `RunStatus=Passed`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`.
- Same no-probe full profile rerun `docs/debug/evidence/GAME-SMOKE/20260705-194759` reproduced `Fatal error in GC / Unexpected mark stack overflow` before `SaveLoaded`: `requests=1`, active `SL-0001`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, `duplicateRequests=0`. Live fatal capture recorded `ProcessId=50648`, `MainWindowTitle=Fatal error in GC`, last DTMAPI line `LoadGame requested for slot/index 2. requestId=SL-0001.`, Unity crash directory `Crash_2026-07-05_124847467`, `crash.dmp`, and final crash source `live-collect`.
- Same full profile with `-AutoExercisePreLoadGcProbe` `docs/debug/evidence/GAME-SMOKE/20260705-205236` reproduced again, but classification changed: `PreLoadForcedGCProbe=Passed`, then native LoadGame reached `SaveLoaded` before fatal. Summary: `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=1`, `duplicateRequests=0`. Live fatal capture recorded `ProcessId=35116`, `MainWindowTitle=Fatal error in GC`, last DTMAPI line `TitleReturn object graph snapshot 17:SaveLoaded...`, Unity crash directory `Crash_2026-07-05_135327389`, `crash.dmp`, and final crash source `live-collect`.
- Threshold comparison across 8.7 and 8.8:
  - 8.7 singles/pairs passed with `ModOwner.records=92-103`, `EventHandler=20-25`, `InputButton=2-7`, `ConfigPage=7-8`, `LoadedCodeMod=10-11`.
  - 8.8 full pass and full fatal both reported `ModOwner.records=119`, `EventHandler=28`, `InputButton=9`, `ConfigPage=10`, `LoadedCodeMod=13`, `GameBridge.featureById=14`, `AudioReplacement.platformPlayers=11`, `CustomAnimals.registrations=10`, Bootstrap roots `1/1`, and `UI.byOwner` root total `2`.
- Classification update: PreLoad forced GC did not crash at title, so the current PreLoad evidence does not support "title stable root-set alone is sufficient." The same profile still crashed after LoadGame/SaveLoaded, so the leading suspect is native LoadGame/save activation over the full stable root-set. This is not a proven single DTMAPI owner leak because full pass and full fatal had the same stable owner counts.
- Do not start service-level hard-disable experiments yet. The no-probe full profile reproduced once after one comparable pass; the PreLoad fatal is intentionally a different condition. Require at least two comparable no-probe full-profile fatals before service-level hard-disable.
- No unknown native `GameObject`, `Component`, `AudioClip`, `AssetBundle`, `RuntimeAnimatorController`, or Unity shell object was destroyed in this phase.
- Detailed review: `docs/reviews/code/2026/20260705-0004-phase88-full-profile-fatal-window-capture.md`. Regression row: `SAVELOAD-FULL-PROFILE-FATAL-WINDOW-20260705`. Update record: `docs/updates/2026/20260705-0009-phase88-full-profile-fatal-window-capture.md`.

2026-07-05 phase 8.9 SaveLoaded / native LoadGame activation analysis:

- Analyzed the existing crash packages first and did not start a new long smoke. The two reviewed packages were `docs/debug/evidence/GAME-SMOKE/20260705-194759` and `docs/debug/evidence/GAME-SMOKE/20260705-205236`.
- No-probe full-profile fatal `20260705-194759`: `fatal-window-dtmapi-log-position.txt` ended at `LoadGame requested for slot/index 2. requestId=SL-0001.`; SaveLoad summary was `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, duplicate `0`; Unity crash `Crash_2026-07-05_124847467` `Player.log` stack points through `TerrainLayer`, `Terrain.Create`, `Room.ResetTerrain`, `Dungeon.AfterLoadData`, `DataPersistenceManager.LoadGame(int)`, and dynamic `DolocAPI::LoadGame(int)`. This is native LoadGame/save activation before DTMAPI `SaveLoaded`.
- PreLoad full-profile fatal `20260705-205236`: `PreLoadForcedGCProbe=Passed`, then the final DTMAPI position was `TitleReturn object graph snapshot 17:SaveLoaded...`; SaveLoad summary was `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=1`, duplicate `0`; Unity crash `Crash_2026-07-05_135327389` stack points through `string.Concat`, `BepInEx.Logging`, `DtmApiRuntime.PublishTitleReturnBoundaryLedgerUpdate`, `CaptureTitleReturnObjectGraphSnapshot`, `NotifySaveLoaded(bool)`, `DolocTownHookCallbacks.AfterLoadArchiveDataPostfix`, and dynamic `DolocAPI::LoadGame(int)`. This places the fatal after native activation reached SaveLoaded and during full object snapshot publication/logging.
- Source audit confirmed actual `AfterLoadArchiveDataPostfix` order: EquipmentSlots SaveLoaded notification, GameBridge feature SaveLoaded dispatch, runtime `NotifySaveLoaded`, then smoke marker. Runtime `NotifySaveLoaded` order is SaveLoad record/title event, lifecycle observation, `SaveSessionLoaded`, public `Events.DispatchSaveLoaded`, queue flush, then `CaptureTitleReturnObjectGraphSnapshot`.
- Added lightweight SaveLoaded activation breadcrumbs around the real hook/runtime order. Each line records only `SaveLoaded.Step`, elapsed milliseconds, GC collection counts, `GC.GetTotalMemory(false)`, current save-load request id, current title-return boundary id, slot, and phase. It does not enumerate objects, stringify large dictionaries, run LINQ summaries, publish hook statuses, or format deltas.
- Added smoke-only `-SaveLoadObjectSnapshotMode Full|Lite|Off`. `Full` is the current default behavior; ordinary smoke and player runtime remain unchanged unless a smoke explicitly writes `Lite` or `Off`. `Lite` records only tiny runtime/request/boundary sections; `Off` skips title-return object graph snapshot capture and logs a skip line. Phase 8.9 did not run Lite/Off long classification.
- Classification update: there are now two concrete fatal windows. The no-probe window is native LoadGame/save activation before SaveLoaded; the PreLoad window reached SaveLoaded and died inside full object snapshot publication/logging. The full snapshot path is therefore a trigger/amplifier candidate, not yet proven as the original root cause.
- Next shortest runtime step, if more evidence is needed, is one comparable full known-failing no-probe run with `3600s` continuous title idle, max two loads, `-TimeoutSeconds 6000`, `-FatalWindowCrashDumpGraceSeconds 30`, and default `Full` snapshot mode. Only if it fatals again near SaveLoaded/snapshot should Lite/Off be used as a classifier.
- No public API was changed, no GameBridge service hard-disable was run, no UI owner/pair bisection resumed, and no unknown native `GameObject`, `Component`, `AudioClip`, `AssetBundle`, `RuntimeAnimatorController`, or Unity shell object was destroyed.
- Detailed review: `docs/reviews/code/2026/20260705-0005-phase89-saveload-activation-analysis.md`. Regression row: `SAVELOAD-SAVELOADED-ACTIVATION-BREADCRUMB-20260705`. Update record: `docs/updates/2026/20260705-0010-phase89-saveload-activation-analysis.md`.

2026-07-05/06 phase 8.10 comparable full no-probe breadcrumb run:

- Ran exactly one comparable full known-failing no-probe baseline with the Phase 8.9 breadcrumbs installed: `docs/debug/evidence/GAME-SMOKE/20260705-231212`.
- Command shape matched the requested baseline: `-IncludeHookProbe`, `-AutoExerciseSaveLoadCycle`, two maximum loads, `3600s` continuous title idle, `-TimeoutSeconds 6000`, `-FatalWindowCrashDumpGraceSeconds 30`, `-SaveLoadObjectSnapshotMode Full`, `CoreCustomAnimals` plus the full extra IDs, and no `-AutoExercisePreLoadGcProbe`.
- Profile validation passed. `official-mod-profile-summary.json` had the expected full extra IDs plus the CoreCustomAnimals local animal packs, `Local.Yuuka_DTMAPI_AutoFishing` disabled, and no unexpected enabled feature mod. The profile was restored after the run.
- The run reproduced Fatal GC on the first post-idle native LoadGame: `RunStatus=Aborted`, `HookProbe=Passed`, `PreLoadForcedGCProbe=Skipped`, `SaveLoaded=Failed`, `SaveLoadCycle=Failed`, `NoFatalInstanceWindow=Failed`, `ProcessExited=Passed`.
- SaveLoad summary was `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, duplicate `0`; final DTMAPI line was `LoadGame requested for slot/index 2. requestId=SL-0001.`.
- `SaveLoaded.Step` breadcrumb count was `0`, so the fatal did not reach hook-level EquipmentSlots/GameBridge/runtime SaveLoaded dispatch or the SaveLoaded object snapshot path.
- Unity fatal evidence was captured live and post-close; latest crash directory `Crash_2026-07-05_161323045` contains `crash.dmp` and `Player.log`, final source `live-collect`. Crash stack points through `mono_gc_register_root`, `TerrainLayer`, `Terrain.Create`, `Room.ResetTerrain`, `Room.AfterLoadData`, `Dungeon.AfterLoadData`, `DataPersistenceManager.LoadGame(int)`, and dynamic `DolocAPI::LoadGame(int)`.
- Classification update: this is the second comparable full no-probe fatal before SaveLoaded, matching `GAME-SMOKE/20260705-194759`. The PreLoad-GC sample `GAME-SMOKE/20260705-205236` remains valuable as proof that SaveLoaded full snapshot/logging can trigger or amplify a later fatal, but it is not the no-probe baseline window.
- Decision: do not run `SaveLoadObjectSnapshotMode Lite|Off` for this window. Continue native LoadGame/save activation root-set analysis. Do not resume UI owner/pair bisection, do not do service-level hard-disable in this phase, and do not destroy unknown native Unity objects.
- Detailed review: `docs/reviews/code/2026/20260705-0006-phase810-comparable-full-no-probe-breadcrumb.md`. Regression row: `SAVELOAD-COMPARABLE-FULL-NO-PROBE-BREADCRUMB-20260705`. Update record: `docs/updates/2026/20260705-0012-phase810-comparable-full-no-probe.md`.

2026-07-06 phase 8.11 diagnostic pressure control before service-disable:

- Ran exactly one full known-failing profile with `SaveLoadObjectSnapshotMode=Lite`: `docs/debug/evidence/GAME-SMOKE/20260706-005212`.
- Command shape preserved the full 8.10 profile and HookProbe comparability, but disabled PreLoad GC and used Lite: `-IncludeHookProbe`, `-AutoExerciseSaveLoadCycle`, two maximum loads, `3600s` continuous title idle, `-TimeoutSeconds 6000`, `-FatalWindowCrashDumpGraceSeconds 30`, `CoreCustomAnimals` plus the full extra IDs, and no `-AutoExercisePreLoadGcProbe`.
- Profile validation passed. `official-mod-profile-summary.json` had the expected full extra IDs plus the CoreCustomAnimals local animal packs, `Local.Yuuka_DTMAPI_AutoFishing` disabled, and no unexpected enabled feature mod. The profile was restored after the run.
- Lite was verified in `summary.txt`, `result.json`, and runtime DTMAPI evidence. `BeforeNextLoadGame` snapshot `9` and `LoadGameNativeEnter` snapshot `11` both logged `saveLoad={mode=Lite...}` and `Runtime={snapshotMode=Lite...}`; deltas retained only tiny runtime/request/boundary sections and no owner root deltas.
- The run reproduced Fatal GC, but not in the previous comparable no-probe `Full` window. Result fields: `RunStatus=Aborted`, `HookProbe=Passed`, `PreLoadForcedGCProbe=Skipped`, `SaveLoaded=Passed`, `SaveLoadCycle=Failed`, `NoFatalInstanceWindow=Failed`, `ProcessExited=Passed`.
- SaveLoad summary was `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=1`, duplicate `0`; final DTMAPI line was `SaveLoaded.Step=Hook.Exit elapsedMs=637 gc0=306 gc1=306 gc2=306 totalMemory=783945728 requestId=SL-0001 boundaryId=TR-0001 slot=2 phase=LogExport.`
- Breadcrumbs reached every hook/runtime SaveLoaded step through `Runtime.AfterObjectSnapshot`, `Runtime.Exit`, `Hook.AfterRuntimeNotifySaveLoaded`, `Hook.AfterMarkSmoke`, and `Hook.Exit`. This means the Lite SaveLoaded snapshot and smoke marker both completed before the fatal popup was observed.
- Unity fatal evidence was captured live and post-close; latest crash directory `Crash_2026-07-05_175308638` contains `crash.dmp` and `Player.log`, final source `live-collect`. Crash stack points through `mono_gc_register_root`, `mono_array_new_specific`, `TextureUtils.DrawArea`, `TextureUtils.CreateTexture`, `MapManager.Init(bool)`, `DolocAPI.AfterLoadArchiveData(bool)`, and dynamic `DolocAPI::LoadGame(int)`.
- Classification update: this is a shifted fatal window, not a Lite pass and not the same pre-SaveLoaded terrain/dungeon stack as `GAME-SMOKE/20260705-194759` / `GAME-SMOKE/20260705-231212`. Stop Lite/Off classification here; analyze the post-SaveLoaded, pre-native-return LoadGame continuation before any service-level hard-disable.
- Detailed review: `docs/reviews/code/2026/20260706-0001-phase811-diagnostic-pressure-control.md`. Regression row: `SAVELOAD-DIAGNOSTIC-PRESSURE-LITE-20260706`. Update record: `docs/updates/2026/20260706-0002-phase811-diagnostic-pressure-control.md`.

2026-07-06 phase 8.14 reliable live dump capture:

- Implemented smoke-only reliable live process dump capture in `tools/scripts/run-game-smoke.ps1`: `-FatalWindowProcessDumpMode` now supports `None|ComSvcsFull|DbgHelpFull|Both`, default `None`. The ComSvcs path uses explicit `%WINDIR%\System32\rundll32.exe` plus `%WINDIR%\System32\comsvcs.dll`, and the DbgHelp path calls `MiniDumpWriteDump` with full memory. Dumps are first written to `%TEMP%\DTMAPI-Dumps` and copied into the evidence folder.
- Valid evidence: `docs/debug/evidence/GAME-SMOKE/20260706-113849`.
- Command shape: full known-failing profile, `CoreCustomAnimals` plus action/utility Workshop IDs, `Local.Yuuka_DTMAPI_ManboCardboardAudio`, YConsole, MoreEquipmentSlots, MoreSaves, Zoom, AutoFishing disabled, no `-IncludeHookProbe`, no `-AutoExercisePreLoadGcProbe`, `SaveLoadObjectSnapshotMode=Lite`, `3600s` continuous title idle, max two LoadGames, `TimeoutSeconds=6000`, `FatalWindowCrashDumpGraceSeconds=30`, `FatalWindowProcessDumpMode=Both`, and `FatalWindowPostCloseCrashDumpWaitSeconds=60`.
- Profile validation passed. `official-mod-profile-summary.json` had the requested nine full extra IDs plus expected CoreCustomAnimals local animal packs, `Local.Yuuka_DTMAPI_AutoFishing` disabled, no unexpected enabled feature mod, and profile restoration succeeded.
- Runtime result:
  - `RunStatus=Aborted`
  - `HookProbe=Skipped`
  - `PreLoadForcedGCProbe=Skipped`
  - `SaveLoadObjectSnapshotMode=Lite`
  - `SaveLoaded=Failed`
  - `SaveLoadCycle=Failed`
  - `NoFatalInstanceWindow=Failed`
  - `ProcessExited=Passed`
  - `ForcedClose=Passed`
  - `CrashBaseline=Passed`
  - `UnityCrashFreshness=stale-only`
  - `FatalWindowProcessDump=Captured:DbgHelpFull`
- SaveLoad summary was `requests=1`, active `SL-0001`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, duplicate requests `0`, and `last=NativeEnter:SL-0001:slot=2:phase=Update:source=Harmony LoadGame Prefix`.
- Lite was verified in `summary.txt`, `result.json`, and DTMAPI snapshots at `BeforeNextLoadGame` and `LoadGameNativeEnter`; final owner deltas were empty.
- Final DTMAPI line before fatal:
  - `LoadGame requested for slot/index 2. requestId=SL-0001.`
- Fatal popup was detected at `2026-07-06T12:39:02.3908174+08:00`, about 1.58 seconds after the final DTMAPI line, with live process `ProcessId=52784`, `MainWindowTitle=Fatal error in GC`, and `Unexpected mark stack overflow` text.
- Fresh Unity crash directory collection was still insufficient: `UnityCrashFreshness=stale-only`; the newest copied Unity crash directory was older than the 12:39 fatal.
- DbgHelp captured a full live dump:
  - `Process-Dumps/DolocTown-52784-fatal-live-dbghelp.dmp`
  - size `4,883,437,290` bytes
  - SHA256 `8F3DC0BD8E5C1CE1DED66EC67F9B3BBFBC889137F0972FBD4032DABC8C417B5F`
  - routine review packages should include `Process-Dumps/TOO-LARGE-DUMP-README.txt` instead of the `.dmp` unless the dump is explicitly requested.
- The ComSvcs branch in this runtime sample reported `Error` because the smoke helper rejected null stdout/stderr text for an empty-output ComSvcs attempt. DbgHelp still captured a valid dump, and the smoke-only null-output handling bug was patched after the run.
- Classification update:
  - HookProbe is not required for this pre-SaveLoaded fatal recurrence.
  - Full object snapshot/delta/logging is not required for this specific pre-SaveLoaded native LoadGame fatal, because the run used `Lite` and fataled before SaveLoaded.
  - The next phase may use the live DbgHelp dump plus DTMAPI boundary evidence for native/root-set or service-level isolation planning.
  - Do not return to UI owner/pair bisection, do not run PreLoad GC for this classification, and do not destroy unknown native Unity objects.
- Detailed review: `docs/reviews/code/2026/20260706-0004-phase814-reliable-live-dump-capture.md`. Regression row: `SAVELOAD-RELIABLE-LIVE-DUMP-NO-HOOKPROBE-LITE-20260706`. Update record: `docs/updates/2026/20260706-0005-phase814-reliable-live-dump-capture.md`.

2026-07-06 phase 8.15 native dump triage and first UiRuntime root isolation:

- Added `tools/scripts/analyze-process-dump.ps1` and analyzed the Phase 8.14 DbgHelp dump metadata. No `cdb.exe` / `windbg.exe` was found, so the dump was not decoded. Evidence `docs/debug/evidence/GAME-SMOKE/20260706-113849/Dump-Analysis` records `DebuggerFound=False`, dump size `4,883,437,290`, SHA256 `8F3DC0BD8E5C1CE1DED66EC67F9B3BBFBC889137F0972FBD4032DABC8C417B5F`, and `TopStackSummary=missing-debugger`. Service isolation therefore proceeded with boundary evidence plus dump hash, without decoded native stack.
- Added smoke-only `-SmokeRootIsolationProfile None|UiRuntime`, default `None`. `UiRuntime` disables DTMAPI-owned `NativeUiLayoutDiagnostics`, `SaveSlots`, `EquipmentSlots`, and `Camera` service/root paths where safe. `DebugConsoleHost` is explicitly logged as unsupported rather than silently disabled.
- Runtime evidence `docs/debug/evidence/GAME-SMOKE/20260706-131450` used the requested first-isolation command: no `-IncludeHookProbe`, no PreLoad GC, `SaveLoadObjectSnapshotMode=Lite`, `SmokeRootIsolationProfile=UiRuntime`, full known-failing profile, AutoFishing disabled, `3600s` continuous title idle, two maximum loads, `FatalWindowProcessDumpMode=DbgHelpFull`, and `FatalWindowPostCloseCrashDumpWaitSeconds=60`.
- Profile validation passed: the nine full extra IDs plus expected CoreCustomAnimals local animal packs were enabled, and `Local.Yuuka_DTMAPI_AutoFishing` was disabled.
- Root isolation status was present in DTMAPI logs: `SmokeRootIsolation.Disabled=NativeUiLayoutDiagnostics|SaveSlots|EquipmentSlots|Camera`, `SmokeRootIsolation.Unsupported=DebugConsoleHost`, and EquipmentSlots hook statuses were `smoke-isolated`. Title/service counters dropped to `featureCount=11`, `ModOwner.records=96`, `EventHandler=19`, `InputButton=9`, and `LoadedCodeMod=10`.
- The run did not eliminate Fatal GC. It shifted the window: SaveLoaded completed through `Runtime.AfterObjectSnapshot`, `Runtime.Exit`, `Hook.AfterRuntimeNotifySaveLoaded`, `Hook.AfterMarkSmoke`, and final `SaveLoaded.Step=Hook.Exit elapsedMs=35 gc0=232 gc1=232 gc2=232 totalMemory=778035200 requestId=SL-0001 boundaryId=TR-0001 slot=2 phase=SaveLoaded.` Fatal popup was detected at `2026-07-06T14:15:21.0204961+08:00`.
- SaveLoad state is reconstructed from DTMAPI evidence because `result.json` was not generated: `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=1`, duplicate requests `0`.
- DbgHelp captured another full live dump: `Process-Dumps/DolocTown-34588-fatal-live-dbghelp.dmp`, size `4,972,791,628`, SHA256 `9E9780BF1258C670E307BFA6A36BBD59E72752CF554090ECFAD54CAA3CBF0ADE`. Routine packages should include `Process-Dumps/TOO-LARGE-DUMP-README.txt`, not the `.dmp`.
- Unity crash collection after manual close/post-close captured fresh `Unity-Crashes/Crash_2026-07-06_061918817` with `crash.dmp` and `Player.log`.
- The smoke harness exposed a smoke-only packaging bug: after DbgHelp captured the dump, `Write-TooLargeDumpReadme` failed because `Get-FileHash` was unavailable in that child PowerShell context. This aborted `result.json` generation and automatic profile restore. Evidence includes `RESULT-MISSING-README.txt`, `manual-result-reconstruction.json`, `manual-fatal-cleanup-note.txt`, and `manual-official-mod-profile-restore.txt`.
- Fixed the smoke-only hash path after the run by adding a .NET SHA256 fallback. The profile was manually restored from `official-mod-profile.mod_infos.before.json`, no `DolocTown.exe` remained, and the runtime lock was free.
- Classification update: UiRuntime is not a simple sufficient fix/root removal. It changed timing but did not remove the fatal. Stop broad service-disable expansion from this sample and analyze the post-SaveLoaded, pre-native-return LoadGame continuation and fresh dump/crash evidence before choosing the next narrow content/native-heavy isolation axis.
- Detailed review: `docs/reviews/code/2026/20260706-0005-phase815-native-dump-uiroot-isolation.md`. Regression row: `SAVELOAD-UIRUNTIME-ROOT-ISOLATION-20260706`. Update record: `docs/updates/2026/20260706-0006-phase815-native-dump-uiroot-isolation.md`.

2026-07-06 phase 8.16 post-SaveLoaded native continuation / VersionPatcher probe:

- Added smoke-only `-SmokeNativeLoadContinuationProbe None|VersionPatcher`, default `None`. It records lightweight `NativeContinuation.Step` breadcrumbs only: method, phase, elapsed time, GC collection counts, `GC.GetTotalMemory(false)`, request id, boundary id, thread id/name, and save-load counters.
- Probe targets were `DolocAPI.LoadGame` enter/exit, `DolocAPI.AfterLoadArchiveData(bool)`, `DolocTown.VersionPatcher.LoadAllVersionPatches()`, `DolocTown.VersionPatcher.LoadAllVersionPatchesBeyond(string)`, and `DolocTown.MapManager.Init(bool)`. `TextureUtils.DrawArea` was reported as `unsupported-skipped-high-frequency` and was not patched.
- Runtime evidence `docs/debug/evidence/GAME-SMOKE/20260706-150952` used no HookProbe, no PreLoad GC, `SaveLoadObjectSnapshotMode=Lite`, `SmokeRootIsolationProfile=UiRuntime`, `SmokeNativeLoadContinuationProbe=VersionPatcher`, full known-failing profile, AutoFishing disabled, `3600s` continuous title idle, max two LoadGames, `FatalWindowProcessDumpMode=DbgHelpFull`, and `FatalWindowPostCloseCrashDumpWaitSeconds=60`.
- Profile validation passed. The nine full extra IDs plus expected CoreCustomAnimals local animal packs were enabled, `Local.Yuuka_DTMAPI_AutoFishing` was disabled, no unexpected Workshop/feature mod was enabled, and `OfficialModProfileRestored=True` was recorded.
- Probe runtime evidence passed: `AfterLoadArchiveDataHook=installed`, `VersionPatcherLoadAllHook=installed`, `VersionPatcherLoadBeyondHook=installed`, `MapManagerInitHook=installed`, and `TextureUtilsDrawAreaHook=unsupported-skipped-high-frequency`.
- The run fataled before SaveLoaded and before any continuation target. The only native continuation breadcrumb was `NativeContinuation.Step=DolocAPI.LoadGame.Enter ... nativeEnter=1 nativeReturn=0 saveLoaded=0`.
- SaveLoad state: `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, duplicate requests `0`. `SaveLoaded.Step` count was `0`.
- Fresh Unity crash `Unity-Crashes/Crash_2026-07-06_081133308-54af863107cf/Player.log` stack points through `Dictionary<Vector2Int,TerrainSlot>.Resize`, `TerrainLayer`, `Terrain.Create`, `Room.ResetTerrain`, `Dungeon.AfterLoadData`, `DungeonArchiveData.AfterLoadData`, `DataPersistenceManager.LoadGame`, and dynamic `DolocAPI::LoadGame`.
- DbgHelp captured another full live dump: `Process-Dumps/DolocTown-11580-fatal-live-dbghelp.dmp`, size `4,891,602,600`, SHA256 `B99DAC880F82234DEB7B43B4E095CA3EDD9D6560A370C7DCD1AA022BEC3EEC78`. Routine packages should include `Process-Dumps/TOO-LARGE-DUMP-README.txt`, not the `.dmp`.
- Classification update: this recurrence does not implicate `VersionPatcher`, `AfterLoadArchiveData`, `MapManager.Init`, or `TextureUtils.DrawArea`, because the fatal happened before those breadcrumbs. Continue native LoadGame terrain/dungeon root-set analysis around `TerrainLayer`, `Room.ResetTerrain`, and `DungeonArchiveData.AfterLoadData`.
- Detailed review: `docs/reviews/code/2026/20260706-0006-phase816-post-saveload-native-continuation-probe.md`. Regression row: `SAVELOAD-NATIVE-CONTINUATION-PROBE-20260706`. Update record: `docs/updates/2026/20260706-0007-phase816-native-continuation-probe.md`.

2026-07-06 phase 8.17 managed root-set isolation: UI code roots:

- Ran the Phase 8.16 light diagnostic shape but removed the four UI code mods from the official enabled profile: YConsole `Workshop.3742714442`, MoreEquipmentSlots `Workshop.3744059735`, MoreSaves `Workshop.3742763050`, and Zoom `Workshop.3742717440`.
- First no-UI-code-root evidence `docs/debug/evidence/GAME-SMOKE/20260706-163322` passed. Per the 8.17 decision rules, a clean-restart rerun was required before attribution.
- Clean-restart rerun `docs/debug/evidence/GAME-SMOKE/20260706-174103` also passed. Both runs used no HookProbe, no PreLoad GC, `SaveLoadObjectSnapshotMode=Lite`, `SmokeRootIsolationProfile=UiRuntime`, `SmokeNativeLoadContinuationProbe=VersionPatcher`, `3600s` continuous title idle, and at most two LoadGames.
- Profile validation passed in both runs. `official-mod-profile-summary.json` enabled only the five non-UI extras (`Workshop.3742763309`, `Workshop.3742763843`, `Workshop.3742763540`, `Workshop.3742763706`, `Local.Yuuka_DTMAPI_ManboCardboardAudio`) plus expected CoreCustomAnimals packs. `Local.Yuuka_DTMAPI_AutoFishing` and all four UI Workshop IDs were disabled, and profile restoration was recorded.
- DTMAPI logs showed `Skipping DTMAPI.DebugConsoleMod`, `Skipping DTMAPI.MoreEquipmentSlotsMod`, `Skipping DTMAPI.MoreSavesMod`, and `Skipping DTMAPI.ZoomMod`. `managed-root-isolation-summary.txt` for both runs records `UnexpectedRemainingOwners=none`. `DTMAPI.DebugConsoleHost` remains only as an unsupported host/API owner under `UiRuntime`, not as the disabled `DTMAPI.DebugConsoleMod` code owner.
- Managed owner counters dropped to `ModOwner.records=65`, `EventHandler=11`, `InputButton=2`, `ConfigMenuPage=5`, and `LoadedCodeMod=6` in both runs. The remaining owner roots were only the non-UI action/audio/content owners.
- Both runs completed two native loads and one return-home cycle with `RunStatus=Passed`, `SaveLoadCycle=Passed`, `NoFatalInstanceWindow=Passed`, `ProcessExited=Passed`, `requests=2`, `nativeEnter=2`, `nativeReturn=2`, `saveLoaded=2`, `duplicateRequests=0`, and `fatalWindows=0`.
- Classification update: removing the four UI code mod roots is the strongest pressure-reduction signal so far, but it is not proof of a fix because the full baseline is intermittent. Treat UI code roots as a strong suspect/amplifier and require a narrower confirmation axis before claiming root cause or solution.
- Detailed review: `docs/reviews/code/2026/20260706-0007-phase817-managed-root-isolation-ui-code-roots.md`. Regression row: `SAVELOAD-MANAGED-ROOT-UI-CODE-ISOLATION-20260706`. Update record: `docs/updates/2026/20260706-0008-phase817-managed-root-isolation-ui-code-roots.md`.

2026-07-06 phase 8.18 UI code root combination triage:

- Ran the Phase 8.17 light diagnostic route and added back only the input-heavy UI pair: YConsole `Workshop.3742714442` and Zoom `Workshop.3742717440`. MoreEquipmentSlots `Workshop.3744059735` and MoreSaves `Workshop.3742763050` remained disabled.
- Evidence `docs/debug/evidence/GAME-SMOKE/20260706-185612` used no HookProbe, no PreLoad GC, `SaveLoadObjectSnapshotMode=Lite`, `SmokeRootIsolationProfile=UiRuntime`, `SmokeNativeLoadContinuationProbe=VersionPatcher`, `3600s` continuous title idle, and at most two LoadGames.
- Profile validation passed. Enabled IDs were the five non-UI base extras plus YConsole and Zoom plus expected CoreCustomAnimals packs. AutoFishing, MoreEquipmentSlots, MoreSaves, and local fallback UI IDs were disabled, and profile restoration was recorded.
- The run reproduced Fatal GC on the first post-idle LoadGame before SaveLoaded: `RunStatus=Aborted`, `SaveLoaded=Failed`, `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, `duplicateRequests=0`, final DTMAPI line `NativeContinuation.Step=DolocAPI.LoadGame.Enter`.
- `Lite` was verified at both `BeforeNextLoadGame` and `LoadGameNativeEnter`; owner deltas at those Lite snapshots were empty. The native continuation probe did not reach `AfterLoadArchiveData`, `VersionPatcher`, or `MapManager.Init`.
- Fresh Unity crash evidence again pointed through `Dictionary<Vector2Int,TerrainSlot>.Resize`, `TerrainLayer`, `Terrain.CreateLayers`, `Room.AfterLoadData`, `DungeonManager.AfterLoadData`, `DungeonArchiveData.AfterLoadData`, and `DataPersistenceManager.LoadGame`.
- Managed counters rose from the Phase 8.17 no-UI-code-root shape (`records=65`, `EventHandler=11`, `InputButton=2`, `ConfigMenuPage=5`, `LoadedCodeMod=6`) to `records=87`, `EventHandler=18`, `InputButton=9`, `ConfigMenuPage=7`, `LoadedCodeMod=8`.
- Classification update: `YConsole + Zoom` is sufficient to reproduce the pre-SaveLoaded terrain/dungeon fatal under the Phase 8.17 route. This narrows the UI suspect axis to the input-heavy pair but does not yet prove whether YConsole alone, Zoom alone, or their shared input/static/config roots are necessary.
- Detailed review: `docs/reviews/code/2026/20260706-0008-phase818-ui-code-root-combination-triage.md`. Regression row: `SAVELOAD-MANAGED-ROOT-UI-PAIR-TRIAGE-20260706`. Update record: `docs/updates/2026/20260706-0009-phase818-ui-code-root-combination-triage.md`.

2026-07-06 phase 8.19 YConsole / Zoom pair decomposition:

- Ran three Phase 8.19 samples under the same light diagnostic route as 8.18: no HookProbe, no PreLoad GC, `SaveLoadObjectSnapshotMode=Lite`, `SmokeRootIsolationProfile=UiRuntime`, `SmokeNativeLoadContinuationProbe=VersionPatcher`, `3600s` continuous title idle, max two LoadGames, AutoFishing disabled, MoreSaves disabled, and MoreEquipmentSlots disabled.
- Zoom-only evidence `docs/debug/evidence/GAME-SMOKE/20260706-201209` passed with `requests=2`, `nativeEnter=2`, `nativeReturn=2`, `saveLoaded=2`, `duplicateRequests=0`, `fatalWindows=0`. Owner counters: `records=77`, `EventHandler=14`, `InputButton=7`, `ConfigMenuPage=6`, `LoadedCodeMod=7`.
- YConsole-only evidence `docs/debug/evidence/GAME-SMOKE/20260706-211430` passed with `requests=2`, `nativeEnter=2`, `nativeReturn=2`, `saveLoaded=2`, `duplicateRequests=0`, `fatalWindows=0`. Owner counters: `records=75`, `EventHandler=15`, `InputButton=4`, `ConfigMenuPage=6`, `LoadedCodeMod=7`.
- Pair confirmation evidence `docs/debug/evidence/GAME-SMOKE/20260706-221651` reproduced Fatal GC before SaveLoaded with `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, `duplicateRequests=0`, final line `NativeContinuation.Step=DolocAPI.LoadGame.Enter`, and fresh Unity crash evidence.
- The pair confirmation had the same owner-root shape as 8.18A: `records=87`, `EventHandler=18`, `InputButton=9`, `ConfigMenuPage=7`, `LoadedCodeMod=8`, with `DTMAPI.DebugConsoleMod` and `DTMAPI.ZoomMod` enabled and MoreSaves/MoreEquipmentSlots absent.
- Unity stack for `20260706-221651` pointed through `RoomProtoPatch.CreateGameMaps`, `MonsterEnv`, `Room.AfterLoadData`, `DungeonArchiveData.AfterLoadData`, and `DataPersistenceManager.LoadGame`. This differs from the earlier pair fatal's exact terrain allocation site but stays in the same pre-SaveLoaded room/dungeon LoadGame activation window.
- Classification update: a single pass does not prove Zoom or YConsole safe, but singles passing once and pair reproducing twice makes combined `YConsole + Zoom` input/event/config/code-owner pressure the current strongest suspect. Next work should isolate root type inside this pair rather than run UI triples, service hard-disable, or content/native-heavy isolation.
- Detailed review: `docs/reviews/code/2026/20260706-0009-phase819-yconsole-zoom-pair-decomposition.md`. Regression row: `SAVELOAD-YCONSOLE-ZOOM-PAIR-DECOMPOSITION-20260706`. Update record: `docs/updates/2026/20260706-0010-phase819-yconsole-zoom-pair-decomposition.md`.

2026-07-07 phase 8.20 YConsole + Zoom input root isolation:

- Added smoke-only `SmokeOwnerRootIsolationProfile` support and ran only the root-type split requested for the already reproduced `YConsole + Zoom` pair. No public API or ordinary player runtime behavior changed.
- Excluded setup/control attempts:
  - `docs/debug/evidence/GAME-SMOKE/20260706-234314`: Steam launch did not reach a usable game runtime.
  - `docs/debug/evidence/GAME-SMOKE/20260706-235527`: Steam launch did not reach a usable game runtime.
  - `docs/debug/evidence/GAME-SMOKE/20260706-235733`: instrumentation-invalid, suppression was attempted before owner roots were registered and reported `unexpectedDisabledCodeOwners=DTMAPI.DebugConsoleMod|DTMAPI.ZoomMod`.
- Valid evidence:
  - `docs/debug/evidence/GAME-SMOKE/20260707-000526`: DirectExe fallback, `SmokeOwnerRootIsolationProfile=YConsoleZoomNoInput`, passed.
  - `docs/debug/evidence/GAME-SMOKE/20260707-011257`: clean-process DirectExe fallback rerun, same profile, passed.
- Both valid runs used no HookProbe, no PreLoad GC, `SaveLoadObjectSnapshotMode=Lite`, `SmokeRootIsolationProfile=UiRuntime`, `SmokeNativeLoadContinuationProbe=VersionPatcher`, continuous `3600s` title idle, max two LoadGames, AutoFishing disabled, MoreSaves disabled, and MoreEquipmentSlots disabled.
- Suppression evidence was identical in both valid runs:
  - Target owners: `DTMAPI.DebugConsoleMod|DTMAPI.ZoomMod`.
  - Target root type: `InputButton`.
  - Removed roots: `InputButton=7`, `EventHandler=0`, `ConfigPage=0`.
  - Before/after: `DTMAPI.DebugConsoleMod` input buttons `2->0`, `DTMAPI.ZoomMod` input buttons `5->0`; both retained their event handlers, config pages, and `LoadedCodeMod` registrations.
  - `suppressionSucceeded=True`, `unexpectedRemainingSuppressedRoots=none`, `unexpectedDisabledCodeOwners=none`.
- Both valid runs completed the two-load route with `requests=2`, `nativeEnter=2`, `nativeReturn=2`, `saveLoaded=2`, `duplicateRequests=0`, and `fatalWindows=0`.
- Classification update: suppressing only the pair's runtime `InputButton` roots made the previously reproducing pair profile pass twice under the same light diagnostic route. This makes `YConsole + Zoom` input roots a strong suspect / pressure amplifier. It is not yet a player-runtime fix, and the DirectExe launch-mode caveat remains because Steam launch was stale in this session.
- Next step should split input roots by owner with `ZoomNoInput` and `YConsoleNoInput`, not return to UI triples, service hard-disable, PreLoad GC, or `Off`.
- Detailed review: `docs/reviews/code/2026/20260707-0001-phase820-yconsole-zoom-input-root-isolation.md`. Regression row: `SAVELOAD-YCONSOLE-ZOOM-ROOT-TYPE-ISOLATION-20260707`. Update record: `docs/updates/2026/20260707-0001-phase820-yconsole-zoom-input-root-isolation.md`.

2026-07-07 phase 8.21 YConsole vs Zoom input owner isolation:

- Ran the requested single-owner input split under the same light diagnostic route as Phase 8.20: no HookProbe, no PreLoad GC, `SaveLoadObjectSnapshotMode=Lite`, `SmokeRootIsolationProfile=UiRuntime`, `SmokeNativeLoadContinuationProbe=VersionPatcher`, continuous `3600s` title idle, max two LoadGames, AutoFishing disabled, MoreSaves disabled, and MoreEquipmentSlots disabled.
- Valid evidence:
  - `docs/debug/evidence/GAME-SMOKE/20260707-054818`: `ZoomNoInput`, passed.
  - `docs/debug/evidence/GAME-SMOKE/20260707-065046`: `ZoomNoInput` clean-process repeat, passed.
  - `docs/debug/evidence/GAME-SMOKE/20260707-075645`: `YConsoleNoInput`, passed.
  - `docs/debug/evidence/GAME-SMOKE/20260707-085818`: `YConsoleNoInput` clean-process repeat, passed.
- Every run used DirectExe fallback and a valid `CoreCustomAnimals + base extras + YConsole + Zoom` profile. `Local.Yuuka_DTMAPI_AutoFishing`, MoreSaves `Workshop.3742763050`, and MoreEquipmentSlots `Workshop.3744059735` were disabled, and each profile was restored.
- `ZoomNoInput` removed only the Zoom input roots: `DTMAPI.ZoomMod` `InputButton 5->0`, while `EventHandler=3`, `ConfigPage=1`, and `LoadedCodeMod=1` stayed present. Both `ZoomNoInput` runs had `suppressionSucceeded=True`, `unexpectedRemainingSuppressedRoots=none`, and `unexpectedDisabledCodeOwners=none`.
- `YConsoleNoInput` removed only the DebugConsole input roots: `DTMAPI.DebugConsoleMod` `InputButton 2->0`, while `EventHandler=4`, `ConfigPage=1`, and `LoadedCodeMod=1` stayed present. Both `YConsoleNoInput` runs had `suppressionSucceeded=True`, `unexpectedRemainingSuppressedRoots=none`, and `unexpectedDisabledCodeOwners=none`.
- All four runs completed `requests=2`, `nativeEnter=2`, `nativeReturn=2`, `saveLoaded=2`, `duplicateRequests=0`, and `fatalWindows=0`; the final native continuation reached `DolocAPI.LoadGame.Exit` on the second load.
- Classification update: both single-owner input suppressions passing twice suggests the pair's input roots act as a threshold-pressure island. Removing either side lowers the tested route below the observed fatal threshold. This is not a player-runtime fix, not proof that a single owner is individually defective, and still carries the DirectExe fallback/baseline-intermittency caveat.
- Next step should inspect or instrument the input-root lifetime and callback captures for `DTMAPI.DebugConsoleMod` and `DTMAPI.ZoomMod`, then design a player-safe reduction in long-lived input roots before rerunning the original `YConsole + Zoom` pair without smoke suppression.
- Detailed review: `docs/reviews/code/2026/20260707-0002-phase821-yconsole-vs-zoom-input-owner-isolation.md`. Regression row: `SAVELOAD-YCONSOLE-ZOOM-INPUT-OWNER-ISOLATION-20260707`. Update record: `docs/updates/2026/20260707-0002-phase821-yconsole-vs-zoom-input-owner-isolation.md`.

2026-07-06 phase 8.13 fresh crash capture hardening:

- Implemented smoke-only fatal-window crash capture hardening in `tools/scripts/run-game-smoke.ps1`:
  - `crash-baseline.txt` at smoke start.
  - Unity crash freshness classification as `fresh`, `stale-only`, or `missing`.
  - `-FatalWindowProcessDumpMode None|ComSvcsFull`, default `None`.
  - `-FatalWindowPostCloseCrashDumpWaitSeconds`, default `20`.
  - Result fields for `CrashBaseline`, `UnityCrashFreshness`, `UnityCrashFreshnessSummary`, `FatalWindowProcessDumpMode`, `FatalWindowProcessDump`, and `FatalWindowPostCloseCrashDumpWaitSeconds`.
- These changes are smoke-only. No public API changed, no GameBridge service was disabled or refactored, ordinary player runtime behavior was unchanged, `Full` remains the default snapshot mode, no PreLoad GC or `Off` classification was run, and no unknown native `GameObject`, `Component`, `AudioClip`, `AssetBundle`, controller, or Unity shell object was destroyed.
- Two instrumentation-invalid attempts are retained but excluded from ISSUE-010 conclusions:
  - `docs/debug/evidence/GAME-SMOKE/20260706-075600`: fatal popup reproduced, but the new process-dump helper used PowerShell's read-only `$PID` variable and aborted before scripted collection completed.
  - `docs/debug/evidence/GAME-SMOKE/20260706-085929`: fatal popup reproduced, but the new crash-freshness helper failed on null nullable handling after live collect and before post-close completion.
- Valid evidence: `docs/debug/evidence/GAME-SMOKE/20260706-100234`.
- Command shape: full known-failing profile, `CoreCustomAnimals` plus action/utility Workshop IDs, `Local.Yuuka_DTMAPI_ManboCardboardAudio`, YConsole, MoreEquipmentSlots, MoreSaves, Zoom, AutoFishing disabled, no `-IncludeHookProbe`, no `-AutoExercisePreLoadGcProbe`, `SaveLoadObjectSnapshotMode=Lite`, `3600s` continuous title idle, max two LoadGames, `TimeoutSeconds=6000`, `FatalWindowCrashDumpGraceSeconds=30`, `FatalWindowProcessDumpMode=ComSvcsFull`, and `FatalWindowPostCloseCrashDumpWaitSeconds=60`.
- Profile validation passed. `official-mod-profile-summary.json` had the requested nine full extra IDs plus expected CoreCustomAnimals local animal packs, `Local.Yuuka_DTMAPI_AutoFishing` disabled, no unexpected enabled feature mod, and profile restoration succeeded.
- Runtime result:
  - `RunStatus=Aborted`
  - `HookProbe=Skipped`
  - `PreLoadForcedGCProbe=Skipped`
  - `SaveLoadObjectSnapshotMode=Lite`
  - `SaveLoaded=Passed`
  - `SaveLoadCycle=Failed`
  - `NoFatalInstanceWindow=Failed`
  - `ProcessExited=Passed`
  - `ForcedClose=Passed`
  - `CrashBaseline=Passed`
  - `UnityCrashFreshness=stale-only`
  - `FatalWindowProcessDump=MissingDump`
- SaveLoad summary was `requests=1`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=1`, duplicate requests `0`, and `last=SaveLoaded:SL-0001:slot=2:phase=SaveLoaded:source=DolocAPI.AfterLoadArchiveData`.
- Lite was verified in `summary.txt`, `result.json`, and DTMAPI snapshots at `BeforeNextLoadGame`, `LoadGameNativeEnter`, and `SaveLoaded`; the Lite snapshots had empty owner deltas.
- Final DTMAPI line before fatal:
  - `SaveLoaded.Step=Hook.Exit elapsedMs=51 gc0=306 gc1=306 gc2=306 totalMemory=766808064 requestId=SL-0001 boundaryId=TR-0001 slot=2 phase=SaveLoaded.`
- Fatal popup was detected at `2026-07-06T11:02:48.0231517+08:00`, about 124 ms after the `Hook.Exit` log, with live process `ProcessId=31624`, `MainWindowTitle=Fatal error in GC`, and `Unexpected mark stack overflow` text.
- The valid 8.13 window is therefore post-SaveLoaded and pre-native-return: DTMAPI's SaveLoaded hook breadcrumb chain completed, but the native LoadGame postfix did not record `nativeReturn`.
- Fresh native crash evidence is still insufficient:
  - `Unity-Crashes/summary.txt` copied only stale directories, newest `Crash_2026-07-05_175308638`, last written at `2026-07-06T01:53:14`, before the 8.13 fatal at `11:02:48`.
  - `ComSvcsFull` attempted a live dump and exited `-2147024773`, creating no `DolocTown-31624-fatal-live.dmp`; stderr/stdout were empty.
- Classification update:
  - HookProbe is not required for the overall long-idle fatal class.
  - `Full` snapshot/delta/logging is not required for the overall fatal class because the no-HookProbe profile can still fatal under `Lite`.
  - The immediate fatal window remains unstable: Phase 8.12 no-HookProbe Lite was pre-SaveLoaded, while Phase 8.13 no-HookProbe Lite reached `SaveLoaded.Step=Hook.Exit` and then fataled before native return.
  - Do not start service-level hard-disable directly from this sample. First analyze this shifted post-SaveLoaded/pre-native-return window or improve live dump capture with a more reliable mechanism.
- Detailed review: `docs/reviews/code/2026/20260706-0003-phase813-fresh-native-crash-capture.md`. Regression row: `SAVELOAD-FRESH-CRASH-CAPTURE-NO-HOOKPROBE-LITE-20260706`. Update record: `docs/updates/2026/20260706-0004-phase813-fresh-native-crash-capture.md`.

2026-07-06 phase 8.12 HookProbe / SaveLoaded diagnostic pressure isolation:

- Ran exactly one full known-failing profile with HookProbe removed and `SaveLoadObjectSnapshotMode=Lite`: `docs/debug/evidence/GAME-SMOKE/20260706-025334`.
- Command shape preserved the 8.11 feature profile and continuous title-idle route but omitted `-IncludeHookProbe`: two maximum loads, `3600s` continuous title idle, `-TimeoutSeconds 6000`, `-FatalWindowCrashDumpGraceSeconds 30`, `CoreCustomAnimals` plus the full extra IDs, and no `-AutoExercisePreLoadGcProbe`.
- Profile validation passed. `official-mod-profile-summary.json` had the expected full extra IDs plus the CoreCustomAnimals local animal packs, `Local.Yuuka_DTMAPI_AutoFishing` disabled, and no unexpected enabled feature mod. The profile was restored after the run.
- HookProbe was absent: `summary.txt` had `IncludeHookProbe=False`, `result.json` had `HookProbe=Skipped`, and DTMAPI owner summaries did not include `DTMAPI.HookProbeMod`.
- Stable owner counters dropped from the 8.11 HookProbe+Lite sample: `ModOwner.records 119->100`, `EventHandler 28->19`, `LoadedCodeMod 13->10`, `ConfigPage/ConfigMenuPage 10->9`, while `InputButton` stayed `9`.
- Lite was verified in `summary.txt`, `result.json`, and runtime DTMAPI evidence. `BeforeNextLoadGame` snapshot `9` and `LoadGameNativeEnter` snapshot `11` both logged `saveLoad={mode=Lite...}` and `Runtime={snapshotMode=Lite...}`; final owner deltas were still empty.
- The run reproduced Fatal GC before SaveLoaded. Result fields: `RunStatus=Aborted`, `HookProbe=Skipped`, `PreLoadForcedGCProbe=Skipped`, `SaveLoaded=Failed`, `SaveLoadCycle=Failed`, `NoFatalInstanceWindow=Failed`, `SaveLoadBoundary=Failed`, `ProcessExited=Passed`, and `ForcedClose=Passed`.
- SaveLoad summary was `requests=1`, active `SL-0001`, `nativeEnter=1`, `nativeReturn=0`, `saveLoaded=0`, duplicate `0`; final DTMAPI line was `LoadGame requested for slot/index 2. requestId=SL-0001.`; `SaveLoaded.Step` count was `0`.
- Fatal popup/process evidence was captured: `ProcessId=46168`, `MainWindowTitle=Fatal error in GC`, and live check text `Fatal error in GC / Unexpected mark stack overflow`.
- Evidence limitation: this run did not produce a fresh Unity crash directory/stack. `Unity-Crashes/summary.txt` copied older crash directories, with newest `Crash_2026-07-05_175308638` last written at `2026-07-06T01:53:14`, while the 8.12 fatal was detected at `03:54:02`. Root `Unity-Player.log` had the DTMAPI load-request line but no fresh fatal/native stack.
- Classification update: HookProbe is not a necessary condition for this pre-SaveLoaded fatal recurrence, and `Full` object snapshot/delta/logging is not necessary for this specific fatal popup because Lite was active at the final boundary snapshots. Stack-level native classification should still rely on the fresh-stack comparable full samples `GAME-SMOKE/20260705-194759` and `GAME-SMOKE/20260705-231212` until another fresh no-HookProbe stack is captured.
- Detailed review: `docs/reviews/code/2026/20260706-0002-phase812-hookprobe-diagnostic-pressure-isolation.md`. Regression row: `SAVELOAD-NO-HOOKPROBE-LITE-20260706`. Update record: `docs/updates/2026/20260706-0003-phase812-hookprobe-diagnostic-pressure-isolation.md`.

2026-07-04 phase 8.6 project object lifecycle sweep:

- Ran a full-project code-level lifecycle audit across static roots, events/delegates, timers/tasks, Unity UI roots, `Texture2D`/`AudioClip`/`AssetBundle`/`RuntimeAnimatorController` families, Bootstrap, GameBridge, registry/content-pack state, CustomAnimals, AnimalVoice, AutoFishing, SaveSlots, EquipmentSlots, and AnimalViewer.
- Added audit record `docs/reviews/code/2026/20260704-0002-project-object-lifecycle-audit.md` classifying object owners as process-long, ReturnedToTitle transient, SaveLoaded rebind/clear, Shutdown/native/Unity cleanup, or suspect.
- Community/source checks in that audit support the current approach: Unity static roots and GameObject hierarchies affect reachability; `DontDestroyOnLoad` roots survive scene changes; runtime UnityEvent listeners should be removed when lifetimes differ; AssetBundle/controller caches should not be unloaded blindly; UnityWebRequest audio objects remain a separate count family; Harmony patches are process-long, so shutdown should release DTMAPI static roots rather than unpatching during title/save.
- Small fixes applied to definite DTMAPI-owned state only:
  - Title Settings UI now resets at SaveLoaded, ReturnedToTitle, title-hidden, and Shutdown boundaries, clearing rendered objects, dynamic binders, input/captured key state, fallback EventSystem, pending status/page markers, and pending config edits through the existing config transaction path.
  - Title Settings UI destroys its DTMAPI-owned icon Sprite/Texture2D on UI reset/shutdown.
  - Debug Console UI now destroys its DTMAPI-owned root/fallback EventSystem and clears binders/input fields/modal state at Shutdown.
  - GameBridge Shutdown now stops retry sources, releases `AppDomain.AssemblyLoad`, removes the DTMAPI-owned SaveLoaded UnityEvent listener, and clears static `DolocTownHookCallbacks` roots.
  - Win32 fallback input edge state clears at SaveLoaded, ReturnedToTitle, and Shutdown.
- Source validation passed: `tools/scripts/test.ps1 -Configuration Release` reported `DTMAPI.UnitTests: OK` with restricted-network `NU1900` warnings only; `git diff --check` passed with line-ending normalization warnings only.
- Short smoke passed under the shared runtime lock, and the lock was released afterward. Final process check found no leftover `DolocTown.exe`.
- Evidence `docs/debug/evidence/GAME-SMOKE/20260704-151818` passed slot 3 title-button lifecycle with one closed SaveLoad request, `duplicateRequests=0`, `fatalWindows=0`, `TitleReturnBoundaryLedger=Passed`, clean process exit, and no fatal window.
- Evidence `docs/debug/evidence/GAME-SMOKE/20260704-151937` passed slot 3 load/title/load with `requests=2`, `nativeEnter=2`, `nativeReturn=2`, `saveLoaded=2`, `duplicateRequests=0`, `fatalWindows=0`, title-return `events=24`, `objectSnapshots=10`, and `fatalEvents=0`.
- Evidence `docs/debug/evidence/GAME-SMOKE/20260704-152059` passed slot 3 three-cycle title return with `requests=3`, `nativeEnter=3`, `nativeReturn=3`, `saveLoaded=3`, `duplicateRequests=0`, `fatalWindows=0`, title-return `events=34`, `objectSnapshots=14`, and `fatalEvents=0`.
- Evidence `docs/debug/evidence/GAME-SMOKE/20260704-152238` passed slot 7 Hatch AnimalVoice; `docs/debug/evidence/GAME-SMOKE/20260704-152339` passed slot 5 AutoFishing phase/soak with `nativeTransientHandles=0`, `miniGameHandles=0`, `readyChargeStates=0`, `animators=0`, `hookPhysics=0`, and `pendingCast=False`; `docs/debug/evidence/GAME-SMOKE/20260704-152523` passed slot 4 AnimalViewer; `docs/debug/evidence/GAME-SMOKE/20260704-152629` passed slot 3 SaveSlots UI.
- All seven short smokes reported `RunStatus=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Classification: this pass fixed definite DTMAPI-owned lifecycle roots and ruled out short-window regressions in title UI, SaveLoad cycles, Hatch AnimalVoice, AutoFishing, AnimalViewer, and SaveSlots. It does not close ISSUE-010 because the known Fatal GC class remains long-window/intermittent. Remaining suspects are documented in the code audit, and the next closure step is a long title-return gate or native crash/root-set analysis if DTMAPI-owned counts stay bounded.
- Long follow-up requested on 2026-07-04: ran the existing slot 3 SaveLoadCycle long route under the runtime lock with `-AutoExerciseSaveLoadCycle -SaveLoadCycleCount 24 -SaveLoadCycleInitialTitleIdleSeconds 3600 -SaveLoadCycleIntervalSeconds 300 -SaveLoadCycleInSaveSeconds 5 -TimeoutSeconds 11250 -SkipBuild`. Evidence `docs/debug/evidence/GAME-SMOKE/20260704-154234` reproduced `Fatal error in GC / Unexpected mark stack overflow`.
- The long follow-up did not fail at the first post-one-hour load. It completed four native LoadGame/ReturnHome closures, then failed at the fifth native LoadGame enter: `requests=5`, active `SL-0005`, `nativeEnter=5`, `nativeReturn=4`, `saveLoaded=4`, `duplicateRequests=0`, `suppressedDuplicates=0`, `timeouts=0`, and coordinator `fatalWindows=0` before the external fatal-window check caught the popup.
- The latest title-return ledger line before abort was `currentBoundary=TR-0005`, `events=47`, `objectSnapshots=20`, `fatalEvents=0`, latest event/snapshot `LoadGameNativeEnter`. This keeps the suspect window at title-return/title-stable-to-next-native-LoadGame, but after the phase 8.6 cleanup it can survive multiple cycles before the native GC fatal recurs.
- Process cleanup for the long follow-up succeeded after forced close: `ProcessExited=Passed`, `ForcedClose=Passed`, and runtime lock was released.

2026-07-04 phase 8.5 TitleReturnBoundaryLedger and object graph snapshots:

- Added diagnostic-only `TitleReturnBoundaryLedger` without changing Registry takeover, public APIs, JSON fields, content pack paths, CustomAnimals behavior, AnimalVoice behavior, AutoFishing behavior, or native/unknown Unity object ownership.
- The ledger records ReturnHome request, ReturnHome native/postfix, Runtime ReturnedToTitle start/end, title-stable observation, next LoadGame prefix/native enter, next LoadGame return, SaveLoaded closure, and fatal-window observation.
- The required boundary snapshots now cover `BeforeReturnHome`, `AfterReturnedToTitleComplete`, `BeforeNextLoadGame`, and `LoadGameNativeEnter`. Snapshot sections include Bootstrap TitleSettings and DebugConsole lifecycle summaries, fallback EventSystem state, SaveSlots/EquipmentSlots UI roots and binders, AnimalViewer overlay/native-data counts, CustomAnimals controller/bundle caches, AudioReplacement entries/clips/requests/callback owners/players, AutoFishing native transients, resource/owner ledgers, dispose graph, and SaveLoad request state.
- `Refactor.SaveLoadBoundary` was corrected to classify active/latest request state instead of using historical `SaveLoadedDispatchCount > 0`. This keeps an active native-enter/no-return request in `loading`, reports fatal during an active/latest request as `fatal`, reports a completed latest request as `closed`, and reports no request as `idle`.
- Community checks are recorded in `docs/updates/2026/20260704-0003-title-return-boundary-ledger.md`: Unity mark-stack overflow reports support object-graph/root-pressure triage; Unity `UnloadUnusedAssets` docs support counting static roots and GameObject hierarchy; `DontDestroyOnLoad` and EventSystem docs support Bootstrap UI/EventSystem snapshots; UnityEvent listener discussions support UI binder counts; AssetBundle and AudioClip docs support count-first/no-unload diagnostics for CustomAnimals and AudioReplacement.
- Source validation passed: PowerShell parser check for `tools/scripts/run-game-smoke.ps1` passed; `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK` and restricted-network `NU1900` feed warnings only; `git diff --check` passed with line-ending normalization warnings only.
- Short slot 3 three-cycle evidence `docs/debug/evidence/GAME-SMOKE/20260704-142213` passed `RunStatus`, `SaveLoadCycle`, `SaveLoaded`, `SaveLoadRequestCoordinator`, `DuplicateLoadRequests`, `SaveLoadBoundary`, `TitleReturnBoundaryLedger`, `ProcessExited`, and `NoFatalInstanceWindow`. Summary: `requests=3; active=none; duplicateRequests=0; nativeEnter=3; nativeReturn=3; saveLoaded=3; fatalWindows=0`; title ledger summary: `currentBoundary=TR-0004; events=34; objectSnapshots=14; fatalEvents=0; latestSnapshot=AfterReturnedToTitleComplete`.
- Short slot 3 title-stable-wait evidence `docs/debug/evidence/GAME-SMOKE/20260704-142402` passed the same gates after a 60-second title interval. Summary: `requests=2; active=none; duplicateRequests=0; nativeEnter=2; nativeReturn=2; saveLoaded=2; fatalWindows=0`; title ledger summary: `currentBoundary=TR-0003; events=24; objectSnapshots=10; fatalEvents=0`.
- Short-test object-family conclusion: no specific uncleared DTMAPI-owned family was found, so no third targeted short test was warranted. After title return, SaveSlots states/pagers/binders were `0`, EquipmentSlots clones/binders/storage owners were `0`, AnimalViewer native data/overlay rows/objects were `0`, CustomAnimals controller/bundle caches were `0`, AudioReplacement pending requests/async operations/AudioClips/callback owners/animal contexts were `0`, AutoFishing native transient handles were `0`, and Bootstrap fallback EventSystem state was absent. AudioReplacement retained stable title/process-lifetime definitions and platform players, which are visible but not growing in these short tests.
- Classification: phase 8.5 provides the missing title-return boundary map and proves the short slot 3 title-return/next-load path can converge. It does not close ISSUE-010 because the reproduced Fatal GC class was long-window/intermittent and no long gate was run after this diagnostic layer. Keep 8B Registry takeover paused until a long gate with this telemetry passes or a fatal recurrence identifies one concrete DTMAPI-owned object family.

2026-07-03 phase 7 GameBridge feature scheduler and final health snapshot:

- Added internal-only GameBridge feature contracts, low-risk `Update` buckets, dispatch counters, and final health snapshots without changing public APIs, content registry takeover, manifest/content-pack JSON semantics, CustomAnimals, AnimalVoice, AutoFishing gameplay, or Hook targets.
- New diagnostics for future long-run classification: `GameBridgeFeatureContracts`, `GameBridgeFeatureScheduler`, `GameBridgeFeatureBuckets`, `GameBridgeFeatureDispatchCounts`, `GameBridgeFinalHealthSnapshot`, and `GameBridgeFinalHealthSummary`.
- Source validation passed: `tools/scripts/test.ps1 -Configuration Release` reported `DTMAPI.UnitTests: OK`; PowerShell parser check for `run-game-smoke.ps1` passed; `git diff --check` passed with line-ending warnings only.
- Short runtime smoke evidence passed: slot 3 `docs/debug/evidence/GAME-SMOKE/20260703-224309`, slot 7 Hatch AnimalVoice `docs/debug/evidence/GAME-SMOKE/20260703-224422`, slot 5 AutoFishing short soak `docs/debug/evidence/GAME-SMOKE/20260703-224524`, slot 4 AnimalPanel UI `docs/debug/evidence/GAME-SMOKE/20260703-231541`, and slot 3 SaveSlots UI `docs/debug/evidence/GAME-SMOKE/20260703-231700` all reported the phase-seven GameBridge fields passed, clean process exit, and no fatal window.
- Initial slot 7 AnimalPanel UI evidence `docs/debug/evidence/GAME-SMOKE/20260703-224713` failed `AnimalViewerUi`, but the user clarified slot 7 is the custom-animal fixture and lacks the hidden-product row expected by this smoke. The same run still reported `Feature.AnimalViewer=ready`, all phase-seven GameBridge scheduler/final-health fields passed, clean process exit, no fatal window, `failedFeatures=0`, `cleanupFailures=0`, `needsRestart=0`, and `resourceErrors=0`. Correct slot 4 evidence then passed `AnimalViewerUi`.
- Stage-end long title-idle evidence `docs/debug/evidence/GAME-SMOKE/20260703-231929` held title for 3600 seconds, then loaded slot 3 and exited cleanly. `result.json` reports `RunStatus=Passed`, `LongTitleIdleBeforeSave=Passed`, `SaveLoaded=Passed`, `TitleIdleResourceGrowth=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, `SaveLoadBoundary=Passed`, `LifecycleBoundaryContract=Passed`, `ResourceLifecycleLedger=Passed`, `ResourceLifecycleCleanup=Passed`, `HookScheduler=Passed`, `CoreHookReadiness=Passed`, `FeatureHookReadiness=Passed`, `HookStatusQueue=Passed`, all phase-seven GameBridge fields passed, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- The phase-seven long-title log has exactly one current-run `LoadGame requested for slot/index 2. requestId=SL-0001` line at 2026-07-04 00:19:38. `SaveLoaded` followed at 00:19:40. Top-level current-run DTMAPI/BepInEx/Unity logs for `GAME-SMOKE/20260703-231929` did not contain `Fatal error in GC` or `Unexpected mark stack overflow`.
- Classification: this phase improves final-state evidence and passed one 3600-second title-idle gate after the scheduler changes. There is still no direct proof that GameBridge feature scheduling was the original Fatal GC cause; treat the pass as a mitigation/containment signal, not enough by itself to close ISSUE-010.
- Follow-up note: a temporary environment-variable flag attempt did not override the Steam-launched runtime; the game log still reported `GameBridgeFeatureUpdateBuckets=true`. For Steam-launched rollback smokes, use local `DTMAPI/config/refactor-scaffold.json` rather than relying on environment propagation through an already-running Steam client.

2026-07-03 phase 5 SaveLoad request coordinator:

- Added an internal-only SaveLoad request coordinator without changing content registry, CustomAnimals, AnimalVoice, AutoFishing, public APIs, manifest/content-pack JSON semantics, or `RegistryTakesOver=false`.
- The new ledger records `requestId`, slot/index, owner/source, thread id, runtime phase, timestamp, native `LoadGame` enter/return, `SaveLoaded` dispatch, timeout, and fatal-window observations.
- The coordinator suppresses only DTMAPI/smoke-originated duplicate direct `LoadGame` calls for the same active slot. It does not intercept native player UI clicks; native duplicates remain diagnostic-only.
- Smoke direct-load fallback paths now ask the runtime coordinator before invoking native `LoadGame`. Smoke-only fake request notifications were removed from selection/restore paths that did not actually call native `LoadGame`.
- Native boundary reference: local reverse metadata for build `23465763_workshop_38581E` confirms `DolocAPI.LoadGame(int index) -> bool` / `DolocTown.GameData.DataPersistenceManager.LoadGame(int index) -> bool`, and `DolocAPI.LoadGame` calls `DolocAPI.AfterLoadArchiveData(bool)`. The phase records signatures/timing only and does not copy decompiled method bodies.
- Important intermediate finding: the first implementation modeled the native `LoadGame` postfix as a second request because native return can happen after `SaveLoaded`. The final implementation attaches delayed native return to the completed same-slot request, with unit coverage for `SaveLoaded`-before-return ordering.
- New smoke fields: `SaveLoadRequestCoordinator`, `SaveLoadRequestSummary`, `DuplicateLoadRequests`, and `SaveLoadBoundary`.
- Source validation passed: `tools/scripts/test.ps1 -Configuration Release` reported `DTMAPI.UnitTests: OK`; PowerShell parser check for `run-game-smoke.ps1` passed; `git diff --check` passed with line-ending warnings only.
- Final short slot 3 evidence `docs/debug/evidence/GAME-SMOKE/20260703-201031` passed startup/save/title-button/process/fatal checks plus `SaveLoadRequestCoordinator`, `DuplicateLoadRequests`, and `SaveLoadBoundary`. Summary: `requests=1; active=none; duplicateRequests=0; suppressedDuplicates=0; nativeEnter=1; nativeReturn=1; saveLoaded=1`.
- Final slot 7 Hatch AnimalVoice evidence `docs/debug/evidence/GAME-SMOKE/20260703-201129` passed Hatch AnimalVoice and the same SaveLoad fields with one closed request and no duplicates.
- Stage-end long title-idle evidence `docs/debug/evidence/GAME-SMOKE/20260703-201302` held title for 3600 seconds, then loaded slot 3 and exited cleanly. `result.json` reports `RunStatus=Passed`, `LongTitleIdleBeforeSave=Passed`, `SaveLoaded=Passed`, `TitleIdleResourceGrowth=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, `SaveLoadBoundary=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- The long-title log has exactly one current-run `LoadGame requested for slot/index 2. requestId=SL-0001` line at 2026-07-03 21:13:10. `SaveLoaded` closed `SL-0001` at 21:13:12, and delayed native return attached to the same request at 21:13:12. Summary: `requests=1; active=none; duplicateRequests=0; suppressedDuplicates=0; nativeEnter=1; nativeReturn=1; saveLoaded=1; timeouts=0; fatalWindows=0`.
- Top-level current-run DTMAPI/BepInEx/Unity logs for `GAME-SMOKE/20260703-201302` did not contain `Fatal error in GC` or `Unexpected mark stack overflow`.
- Classification: this is the first local 3600-second title-idle gate to pass after the reproducible phase-four Fatal GC sample. The repeated load-request boundary is now controlled and observable. Do not mark ISSUE-010 solved yet: one local long-title pass is a strong mitigation signal, not enough to close all independent long-run player crash risk. Future player or release-blocker packages should preserve `SaveLoadRequestSummary` so any recurrence reports the last SaveLoad phase and duplicate count.

2026-07-03 phase 4.5 AutoFishing lifecycle attribution:

- Added an AutoFishing-specific lifecycle attribution layer without changing F6 behavior, `IFishingAutomationApi`, Hook targets, config fields, JSON semantics, Registry takeover, CustomAnimals, or AnimalVoice behavior.
- `FishingAutomationService` now exposes an internal lifecycle snapshot that separates long-lived owner/config state (`fishingOptions`, `fishingStates`, Hook installed state) from native-object-keyed transient state (`fishingMiniGameStartedAt`, mini-game input dictionaries, Ready charge state sets, current Ready state, mini-game input override, animator speed snapshots, hook physics snapshots, and pending cast markers).
- New report-only boundary assertions publish `Fishing.Automation.Lifecycle` and `Refactor.AutoFishingLifecycle` diagnostics for `SetEnabled(false)`, `SaveLoaded`, `ReturnedToTitle`, `FishingGameScrollBar.StopGame`, `AgentStateFishingPull.OnExit`, and pending-cast watchdog release. They do not block gameplay or change cleanup order. `FishingGameScrollBar.StopGame` owns the concrete mini-game handle/input cleanup; full transient-clear assertions remain on destructive or native-exit boundaries.
- Resource lifecycle ledger entries now record AutoFishing owner policies/states plus borrowed native handles held as DTMAPI dictionary/set keys. Releases only clear DTMAPI-held references and restored values; DTMAPI does not destroy native Unity objects.
- `EnvironmentReset` remains non-destructive and low-frequency observed only, because it can be driven by high-frequency environment/camera refresh and should not cancel active fishing.
- Smoke `result.json` now has `AutoFishingLifecycle`, `AutoFishingLifecycleSummary`, and opt-in `AutoFishingSoak`; the new parameter `-AutoFishingSoakLoops` defaults to `0`.
- Charge-ratio audit: source does not show a hard-coded nonzero charge ratio. `AutoFishingMod` passes `config.CastChargeRatio`, smoke defaults `AutoFishingCastChargeRatio=0`, and the ready input override releases immediately for target `0`. Local runtime config `D:\Steam\steamapps\common\Doloc Town\DTMAPI\config\Yuuka.DTMAPI.AutoFishing.json` had `CastChargeRatio=0`, and no matching config was found in the local Steam Workshop subscription cache for app `2285550`. Remaining suspicion is either stale player-side config outside the checked paths or fast-animation Ready timer tick making a tiny visible charge before release. The lifecycle summary now prints `charge=...`, `fast=...`, and multiplier to make this evidence explicit.
- Follow-up note from user manual observation: before the phase 4.5 smoke, manual play still appeared to charge even when the setting was "no charge". The successful short soak does not disprove that report because the smoke temporarily writes a controlled `DefaultLoop` config with `CastChargeRatio=0` and `FastAnimations=false`, while the restored local runtime config has `CastChargeRatio=0` and `FastAnimations=true`. Treat this as a separate AutoFishing behavior follow-up: run a fifth-save targeted case with `FastAnimations=true` and `CastChargeRatio=0`, then check whether Ready `_castTimer` progress advances above zero before release. This is not current evidence for ISSUE-010 Fatal GC and should be managed outside the resource/lifecycle refactor thread unless it starts retaining native state.
- Classification: AutoFishing remains a reasonable ISSUE-010 suspect only in the narrow architectural sense that it owns native-object-keyed transient state and long-loop automation. There is still no evidence that AutoFishing caused the reproduced title-idle Fatal GC: the latest failures occur during title idle or post-idle save load before active fishing, stage 3/4 already showed no title-idle Hook/Event/resource growth loop, and the fifth-save AutoFishing soak passed with no retained native transient state.
- Validation passed: `tools/scripts/test.ps1 -Configuration Release` reported `DTMAPI.UnitTests: OK` with restricted-network `NU1900` feed warnings only; `git diff --check` passed with line-ending warnings only; PowerShell parser check for `run-game-smoke.ps1` passed; fifth-save short soak `GAME-SMOKE/20260703-194001` passed `RunStatus`, `AutoFishingPhase`, `AutoFishingMiniGameComplete`, `AutoFishingLifecycle`, `AutoFishingSoak`, `ProcessExited`, and `NoFatalInstanceWindow`. The soak summary included `charge=0;fast=False;mult=3`, `nativeTransientHandles=0`, `boundaryClearCount=0`, `miniGameHandles=0`, `readyChargeStates=0`, `animators=0`, `hookPhysics=0`, and `pendingCast=False`.

2026-07-02:

- Added the fourth-stage Hook/Event main-thread scheduling layer. This is not a gameplay fix and does not claim to solve the Fatal GC yet; it separates external Hook install triggers from runtime-thread patch execution and makes Hook/event growth visible for the next long-idle gate.
- `Initialize`, `AssemblyLoad`, and the hook retry timer now request Hook installation through an internal scheduler when `HookInstallScheduler=true`; actual Harmony installation is processed from `DolocTownGameBridge.Update()` on the runtime thread.
- Hook readiness is split into core, feature, and smoke/diagnostics layers. Core readiness controls retry timer and AssemblyLoad subscription release; feature readiness is allowed to remain partial without blocking global readiness.
- `SetHookStatus` now updates diagnostics snapshots immediately and queues `HookStatusChanged` event/log publication until runtime-thread queue flush when `HookStatusQueue=true`.
- EventManager now queues safe off-thread lifecycle/save/workshop/log/HookStatus events and rejects off-thread Update/Input events when `EventMainThreadBoundary=true`.
- New smoke/result fields for the stage-four gate: `HookScheduler`, `CoreHookReadiness`, `FeatureHookReadiness`, `HookStatusQueue`, `OffThreadHookRequests`, `AssemblyLoadSubscription`, and `RetryTimerAlive`.
- Stage-four source validation passed: `tools/scripts/test.ps1 -Configuration Release` reported `DTMAPI.UnitTests: OK`; `git diff --check` and PowerShell parser checks passed with only line-ending/feed warnings.
- Slot 3 short lifecycle smoke `docs/debug/evidence/GAME-SMOKE/20260702-204727` passed startup, save load, title-button lifecycle, lifecycle/scaffold/resource gates, all fourth-stage Hook/Event fields, process exit, and fatal-window checks.
- Slot 7 Hatch AnimalVoice smoke `docs/debug/evidence/GAME-SMOKE/20260702-204851` passed Hatch AnimalVoice, lifecycle/scaffold/resource gates, all fourth-stage Hook/Event fields, process exit, and fatal-window checks.
- Slot 8 paper-box behavior remains accepted from user manual QA plus `docs/debug/evidence/GAME-SMOKE/20260702-204954/DTMAPI-latest.log` evidence showing paper-box native owner, `AudioReplacement paper-box OnInteract`, and replacement playback `played=True suppressed=True`. The slot 8 automation can still miss the fixture interaction, so it remains smoke-harness reliability work, not a stage-four runtime regression.
- Long title-idle run `docs/debug/evidence/GAME-SMOKE/20260702-210050` held title for 3600 seconds, then attempted to load slot 3 and reproduced native `Fatal error in GC / Unexpected mark stack overflow`; `result.json` reports `RunStatus=Aborted`, `LongTitleIdleBeforeSave=Failed`, `SaveLoaded=Failed`, and `NoFatalInstanceWindow=Failed`.
- The same long-idle evidence passed `HookScheduler`, `CoreHookReadiness`, `FeatureHookReadiness`, `HookStatusQueue`, `OffThreadHookRequests`, `AssemblyLoadSubscription`, `RetryTimerAlive`, `ResourceLifecycleLedger`, `ResourceLifecycleCleanup`, and `TitleIdleResourceGrowth`. During title idle, observed counts stayed bounded: `AudioReplacement local WAV ready=11`, `Refactor shadow content registry=4`, and `CustomAnimals.AnimatorBridge refreshed=1`.
- The failure boundary is now narrower: the log has four `LoadGame requested for slot/index 2` lines at 22:00:59 and zero `SaveLoaded hook dispatched` lines. The fatal window appeared before save load completed, so the next single-axis isolation should focus on the post-title-idle load request/save-load boundary, including request debouncing and native load-state ownership, rather than another broad Hook/resource matrix.

2026-07-02 third-stage resource lifecycle:

- Added the third-stage resource lifecycle ledger and low-risk DTMAPI-owned `SaveLifetime` cleanup. The ledger records resource lifetime, ownership, process/content/save generation, acquisition/release phase, refresh status, and title-idle growth warnings without changing public API, JSON semantics, Hook installation, CustomAnimals gameplay, AnimalVoice behavior, content-pack paths, or `RegistryTakesOver=false`.
- AudioReplacement now records content definitions, WAV requests, ready clips, platform players, and `AnimalSoundContext` stack entries. It clears only save-lifetime animal sound context state at `SaveLoaded`/`ReturnedToTitle`; existing `CleanupEntry` remains the AudioClip/player release path for content-generation replacement/removal.
- CustomAnimals now records content definitions, animator registrations, AI template registrations, bundle/controller managed-cache refs, PNG sprite override contexts, and sleep follow-up contexts. It clears only save-lifetime PNG/sleep/diagnostic context state at `SaveLoaded`/`ReturnedToTitle`; title-level Unity assets, native sprites, and borrowed template controllers are not unloaded or destroyed.
- Short third-stage smokes passed. Slot 3 evidence `docs/debug/evidence/GAME-SMOKE/20260702-183915` passed lifecycle/title-button/resource ledger/resource cleanup/title-idle-growth gates with resource summary `status=ok`, `records=56`, `titleIdleGrowthWarnings=0`, `warnings=0`, `errors=0`. Slot 7 evidence `docs/debug/evidence/GAME-SMOKE/20260702-184039` passed Hatch AnimalVoice plus resource gates with Hatch child/adult replacement `played=True suppressed=True` and resource summary `status=ok`, `records=89`, `byOwnership=BorrowedNative:24,DtmapiOwned:65`, `warnings=0`, `errors=0`.
- Long title-idle run `docs/debug/evidence/GAME-SMOKE/20260702-184205` held title for 3600 seconds, then loaded slot 3 and logged `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`.
- The long-idle run reproduced the native failure with the hardened harness: `result.json` reports `RunStatus=Aborted`, `LongTitleIdleBeforeSave=Failed`, and `NoFatalInstanceWindow=Failed`; `fatal-window-check.txt` captured `Fatal error in GC` and `Unexpected mark stack overflow`.
- The same long-idle run still had `ResourceLifecycleLedger=Passed`, `ResourceLifecycleCleanup=Passed`, and `TitleIdleResourceGrowth=Passed`. The resource summary at save load reported `status=ok`, `contentGeneration=1`, `saveGeneration=1`, `records=54`, `titleIdleGrowthWarnings=0`, `warnings=0`, and `errors=0`.
- Idle-window counts were bounded in that evidence: `CustomAnimalsRefresh=1`, `AudioRefresh=1`, `LocalWavReady=11`, `HookInstallRepeated=0`, and `ResourceDiagnostics=0`.
- Classification after stage 3: not solved, but narrowed. The reproduced failure is not currently explained by repeated shadow registry refresh, repeated Hook install, repeated WAV ready growth, repeated CustomAnimals binding growth, save-lifetime DTMAPI animal/audio context accumulation, or resource-generation growth during title idle. The next pass should isolate AudioReplacement, CustomAnimals, or Core/lifecycle/native save-load boundary one at a time.

2026-07-02 second-stage lifecycle boundary:

- Added the second-stage lifecycle boundary contract as diagnostic-only scaffolding. It records `Startup`, `TitleObserved`, `SaveLoaded`, `ReturnedToTitle`, `SecondSaveLoaded`, `LogExport`, and `Shutdown` phase counts plus shadow registry refresh counts, Hook install/status counts, and known CustomAnimals/AudioReplacement resource event counts.
- Short second-stage smokes passed for slot 3 lifecycle/title-button and slot 7 Hatch AnimalVoice with lifecycle contract `status=ok`, `warnings=0`, and `errors=0`.
- Slot 10 paper-box automation still failed to trigger the fixture input, but lifecycle contract, startup, save load, process, and fatal checks passed. The user separately confirmed slot 8 paper-box behavior normal, so the remaining paper-box item is automation reliability rather than runtime behavior evidence.
- Long title-idle run `docs/debug/evidence/GAME-SMOKE/20260702-152855` held title for 3600 seconds, then loaded slot 3 and logged `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`.
- During the title-idle window, DTMAPI logs did not show continuous shadow registry scans, Hook reinstall loops, or repeated audio/resource load growth after the initial burst. The lifecycle boundary contract still reported `status=ok`, `warnings=0`, and `errors=0` at save load and log export.
- The live `DolocTown.exe` process then showed `MainWindowTitle : Fatal error in GC` at 2026-07-02 16:41 +08:00. Manual evidence is saved at `docs/debug/evidence/GAME-SMOKE/20260702-152855/long-title-idle-fatal-gc-observation.txt`.
- The generated smoke `result.json` for that run incorrectly passed fatal-window checks because the old harness checked after the process was killed. `tools/scripts/common.ps1` and `tools/scripts/run-game-smoke.ps1` were hardened afterward to detect `Fatal error in GC` and `Unexpected mark stack overflow` while `DolocTown.exe` is still alive, including process `MainWindowTitle`.
- Classification: not solved. The second-stage evidence suggests the 50-60 minute title-idle crash is not explained by an obvious DTMAPI shadow registry loop, Hook reinstall loop, or steady animal/audio resource reload loop. The next investigation should focus on lifecycle/resource ownership across title return and save load, Unity/native object retention, and native crash-report contents.

2026-06-28:

- Reviewed a longer local run with frequent Y-console usage and AutoFishing catches. The run did not show managed DTMAPI fatal errors, but AutoFishing success diagnostics dominated log volume through repeated Ready charge, animation speed, auto-cast, minigame, and restore evidence.
- Added AutoFishing success-diagnostic throttling: repeated success/experimental hook statuses and routine runtime logs publish the first three samples and then 30-second interval samples. Failures and `Fishing.Automation.AutoCastWatchdog=needs-review` remain immediate.
- This is a log-volume and evidence-quality improvement only. It does not prove or disprove the Unity/Mono GC crash root cause.

2026-06-20:

- Added a code-level audit: `docs/reviews/code/2026/20260620-0001-long-run-gc-crash-lifecycle-audit.md`.
- Updated game-internal report export and offline `collect-logs.ps1` to include recent Unity crash report directories and `Player-prev.log`. The follow-up safety review added bounded crash collection budgets, preferred crash evidence ordering, skipped-file summaries, stopped offline collection from copying complete historical `dtmapi-report-*.zip` payloads by default, and made root-bat desktop output fall back to `%TEMP%\DTMAPI-logs` when the desktop path is unavailable. The second safety pass gives the first `crash.dmp` a larger explicit budget and writes `Unity-Crashes/MISSING-CRASH-DUMP-README.txt` if a dump is too large or fails to copy.
- Added lifecycle cleanup for DTMAPI-owned SaveSlots pager roots/event binders at save-load and title-return boundaries. Follow-up review removed destructive cleanup from high-frequency `EnvironmentReset`; cleanup now tries to restore active official save-panel slots before releasing pager/binder state.
- Added parent-independent EquipmentSlots UI cleanup for tracked cloned slot objects, event binders, rendered/evidence flags, and stale summaries at save-load and title-return boundaries. Follow-up review removed clone/binder destruction from high-frequency `EnvironmentReset`; that path only refreshes state flags. The second safety pass also requires a loaded archive plus an active `AccessoriesBar` host before rendering, so stale inactive UI objects cannot be rediscovered after title return.
- Added AutoFishing pending-cast watchdog: DTMAPI now marks pending before native `BodyController.UseFishRod`, clears on real fishing phase/minigame progress, and reports `Fishing.Automation.AutoCastWatchdog=needs-review` if no native progression arrives before the short timeout. The review follow-up releases the pending marker and enters short backoff instead of permanently blocking auto-cast. The second safety pass preserves synchronous native phase confirmation instead of overwriting it with a later `pending` status.
- Added low-frequency lifecycle counters under `Runtime.LifecycleRetentionCounters` for EnvironmentReset count, feature fanout, SaveSlots state/pager/binder counts, EquipmentSlots clone/binder/storage counts, AutoFishing caches/pending cast/backoff, ActionSpeed animator cache, and machine runtime entries. Review follow-up removed every-100-reset publication and keeps the first three samples plus interval-based samples. Repeated `EnvironmentReset` failures are throttled through first/short/interval summary publication.
- Isolated `EnvironmentReset` pre-steps so EquipmentSlots refresh, runtime automation refresh, feature fanout, and lifecycle counter publication cannot block each other if one throws. Feature fanout now uses an allocation-free index loop instead of per-dispatch snapshots on high-frequency paths.
- Removed forced machine-production polling from high-frequency `EnvironmentReset`; normal throttling applies.
- Stopped treating AutoFishing `EnvironmentReset` as a full runtime reset. SaveLoaded/ReturnedToTitle remain destructive reset boundaries; disabling AutoFishing clears transient minigame/ready-charge/pending/animation state.
- Validation so far:
  - Windows PowerShell 5.1 parser check passed for `collect-logs.ps1`.
  - Fake Unity crash directory collection test copied `error.log` and `crash.dmp` into `Unity-Crashes`.
  - Package-shaped root `4_collect_dtmapi_logs.bat` test passed from a path containing spaces and Chinese text; a desktop-write failure fell back to fake `%TEMP%\DTMAPI-logs` and still collected `Unity-Crashes/summary.txt`.
  - `git diff --check` passed with line-ending warnings only.
  - Release build/test passed with `DTMAPI.UnitTests: OK`.
  - New source-only lifecycle cleanup/review unit coverage passed in this branch, including non-destructive SaveSlots/EquipmentSlots EnvironmentReset, AutoFishing hooks-not-installed, disable cleanup, watchdog backoff, EnvironmentReset pre-step isolation, and too-large `crash.dmp` README export.
  - A no-game offline collector run completed with the game folder unresolved and still wrote `Unity-Crashes/summary.txt` with the crash collection budget fields.

2026-07-03 phase 6 mod owner/input/event cleanup:

- Added an internal owner ledger, mod-load transaction, owner-bound input, event-handler quarantine, and config-preview audit. This is a platform isolation layer, not a direct gameplay rewrite or a GC root-cause claim.
- Source validation passed with `tools/scripts/test.ps1 -Configuration Release` and `DTMAPI.UnitTests: OK`. Unit coverage now checks failed partial Entry rollback for API/event/input/config/custom entity registrations, owner-bound input cleanup, event quarantine leaving the active dispatch path, config preview audit, and all new flags disabled.
- New diagnostics for future long-run classification: `ModOwnerLifecycle`, `ModLoadTransaction`, `OwnerBoundInput`, `EventHandlerCleanup`, `ConfigPreviewAudit`, and `FailedModRollback`.
- If ISSUE-010 recurs after phase 6, inspect owner ledger and event/input growth alongside `SaveLoadRequestSummary`, resource ledger, and Hook queue. Raw Harmony/static/native side effects remain `NeedsRestart`; do not treat them as safely rolled back.
- Short runtime smoke evidence for this phase passed: slot 3 `docs/debug/evidence/GAME-SMOKE/20260703-220819`, slot 7 Hatch AnimalVoice `docs/debug/evidence/GAME-SMOKE/20260703-220920`, and slot 5 AutoFishing short soak `docs/debug/evidence/GAME-SMOKE/20260703-221013` all reported `ModOwnerLifecycle=Passed`, `ModLoadTransaction=Passed`, `OwnerBoundInput=Passed`, `EventHandlerCleanup=Passed`, `ConfigPreviewAudit=Passed`, `FailedModRollback=Passed`, clean process exit, and no fatal window.

2026-07-04 phase 8A content/manifest registry index:

- Added an internal content/manifest registry authoritative diagnostic index without enabling `RegistryTakesOver` and without changing content application, CustomAnimals, AnimalVoice, AnimalViewer, AutoFishing, public APIs, Hook targets, JSON semantics, or content-pack paths.
- The index records discovered rows, loaded rows, source/enablement/owner, capabilities, dependency compatibility, manifest diagnostics, and registry diffs. It resolves dependencies against runtime `ModRegistry.GetAll()` so runtime-owned API manifests such as `DTMAPI.GameBridge.DolocTown` are not treated as missing content packs.
- New smoke/result fields: `ContentRegistry`, `ManifestRegistry`, `DependencyCompatibility`, `ContentPackOwnership`, and `RegistryDiffs`.
- Initial slot 3 phase-8A evidence `docs/debug/evidence/GAME-SMOKE/20260704-004551` failed only the new manifest/dependency diagnostic gate. Runtime behavior was clean and `RegistryDiffs=Passed`; the failure exposed a false-positive dependency resolver gap for runtime-owned manifests. The same phase fixed that by passing registered manifests into the resolver and by treating manifest/dependency warnings as diagnostics rather than smoke blockers.
- Final short smoke evidence passed: slot 3 `docs/debug/evidence/GAME-SMOKE/20260704-005016`, slot 7 Hatch AnimalVoice `docs/debug/evidence/GAME-SMOKE/20260704-005121`, slot 4 AnimalViewer UI `docs/debug/evidence/GAME-SMOKE/20260704-005217`, and slot 5 AutoFishing short soak `docs/debug/evidence/GAME-SMOKE/20260704-005317` all reported `ContentRegistry=Passed`, `ManifestRegistry=Passed`, `DependencyCompatibility=Passed`, `ContentPackOwnership=Passed`, `RegistryDiffs=Passed`, `RegistryTakesOver=false`, clean process exit, and no fatal window.
- Final slot 3 registry summary recorded `rows=28`, `loadedRows=19`, `contentPacks=7`, `loadedContentPacks=5`, `customAnimalPacks=6`, `animalVoicePacks=5`, and `diffs=0`. Remaining warnings are explainable local diagnostics: duplicate UniqueIDs between enabled Workshop and disabled OfficialLocal sources, plus locally present `0.5.3-alpha` mods while this runtime baseline remains `0.5.2-alpha`.
- Classification: phase 8A adds better content ownership and manifest/dependency evidence for future long-run crash packages, but it is not a long-title gate and does not close ISSUE-010. If Fatal GC recurs, inspect `RegistryDiffs`, content ownership, dependency warnings, and whether a new unexplained content/manifest mismatch appeared alongside SaveLoad/resource/Hook evidence.

2026-07-04 long title and save/load cycle validation:

- Added opt-in smoke-only `SaveLoadCycle` support to exercise same-process save enter, return-to-title, and repeated save entry. This is test harness support only and does not change player runtime behavior, public APIs, content registry takeover, CustomAnimals, AnimalVoice, AutoFishing, Hook targets, JSON semantics, or content-pack paths.
- Two-hour title-idle single-load evidence `docs/debug/evidence/GAME-SMOKE/20260704-010303` passed. The run held title for 7200 seconds, loaded slot 3, and reported `RunStatus=Passed`, `LongTitleIdleBeforeSave=Passed`, `SaveLoaded=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, `SaveLoadBoundary=Passed`, `LifecycleObservation=Passed`, `ResourceLifecycleLedger=Passed`, `TitleIdleResourceGrowth=Passed`, `ContentRegistry=Passed`, `ManifestRegistry=Passed`, `RegistryDiffs=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`. The current-run log had one `LoadGame requested for slot/index 2. requestId=SL-0001`, followed by `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`.
- Seventh-save repeated enter/return evidence `docs/debug/evidence/GAME-SMOKE/20260704-033112` passed after the smoke-only cycle path was changed to reuse the existing official auto-load route instead of direct native `LoadGame`. It completed three cycles on slot 7 with `SaveLoadCycle=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, `SaveLoadBoundary=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`; summary: `Smoke exercise SaveLoadCycle OK cycles=3, slot=7, initialIdleSeconds=5, intervalSeconds=5, inSaveSeconds=5, elapsedSeconds=74`.
- Retained intermediate evidence `docs/debug/evidence/GAME-SMOKE/20260704-031102` and `docs/debug/evidence/GAME-SMOKE/20260704-032123` are smoke-harness reliability failures from the earlier direct-LoadGame cycle path. They did not show Fatal GC and are superseded by `GAME-SMOKE/20260704-033112`.
- Three-hour periodic cycle attempt `docs/debug/evidence/GAME-SMOKE/20260704-033314` waited one hour on title, then attempted cycle 1/24 and reproduced native `Fatal error in GC / Unexpected mark stack overflow` at the first post-idle load. `result.json` reports `RunStatus=Aborted`, `NoFatalInstanceWindow=Failed`, `SaveLoadCycle=Failed`, `SaveLoaded=Failed`, and `SaveLoadBoundary=Failed`; process/fatal evidence was captured and the script then forced the process closed cleanly.
- Last SaveLoad boundary before the fatal window: `LoadGame requested for slot/index 2. requestId=SL-0001` at 2026-07-04 04:33:24.601 +08:00. `SaveLoadRequestSummary` reports `requests=1; active=SL-0001:slot=2; duplicateRequests=0; suppressedDuplicates=0; nativeEnter=1; nativeReturn=0; saveLoaded=0`. No `SaveLoaded hook dispatched` appeared after the request.
- Classification: the latest reproduction rules out duplicate DTMAPI/smoke LoadGame requests for this sample and narrows the failure to the native post-title-idle `LoadGame` transition before `SaveLoaded`. The two-hour pass plus one-hour failure show this is not deterministic by title-idle duration alone. Current evidence still does not implicate registry diffs, title-idle resource generation growth, Hook reinstall loops, or SaveLoad duplicate requests.
- Follow-up evidence-quality note: the periodic-cycle smoke published repeated `Smoke.SaveLoadCycle=pending` statuses during the one-hour idle window (`statusCount=706`). This is smoke-harness noise and should be throttled before rerunning a multi-hour periodic test, so the next long evidence cannot be muddied by avoidable diagnostic/log pressure.
- Follow-up throttled rerun: `docs/debug/evidence/GAME-SMOKE/20260704-060648` reduced the one-hour initial-idle `Smoke.SaveLoadCycle=pending` publication to five-minute heartbeats and only 19 total pending status publications by failure. The first post-idle load succeeded (`SL-0001` reached `SaveLoaded`, returned to title, and completed cycle 1/24), but the second cycle reproduced the native Fatal GC before `SaveLoaded`: `LoadGame requested for slot/index 2. requestId=SL-0002` at 2026-07-04 07:12:10.037 +08:00, then `fatal-window-check.txt` captured `Fatal error in GC / Unexpected mark stack overflow` for process id `29044`. `SaveLoadRequestSummary` reported `requests=2; active=SL-0002:slot=2; duplicateRequests=0; suppressedDuplicates=0; nativeEnter=2; nativeReturn=1; saveLoaded=1`.
- Classification update after throttling: dense smoke pending publication is not a necessary condition for the crash. The most useful boundary is now title-return-to-next-load: after one successful post-idle save load and `DolocAPI.ReturnHome`, the next native `LoadGame` can still hit Mono GC mark-stack overflow before `SaveLoaded`. This keeps the investigation on native/Unity object graph state across return-to-title and next load, not on duplicate SaveLoad requests.
- Follow-up check for the `Smoke.SaveLoadCycle=pending` `statusCount=706` concern: that count was not present before or immediately after the phase-five duplicate-LoadGame fix. Evidence `docs/debug/evidence/GAME-SMOKE/20260702-210050` had pending count `0` before the fix, and `docs/debug/evidence/GAME-SMOKE/20260703-201302` had pending count `0` after the fix. Later long-title passes `docs/debug/evidence/GAME-SMOKE/20260703-231929` and `docs/debug/evidence/GAME-SMOKE/20260704-010303` also had pending count `0`. The `706` count first appears in the later periodic save/load cycle smoke `docs/debug/evidence/GAME-SMOKE/20260704-033314`.
- Pending-pressure isolation: added opt-in smoke-only `SaveLoadCyclePendingPressure` support and ran the formal command `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseSaveLoadCyclePendingPressure -SaveLoadCyclePendingPressureSeconds 1200 -SaveLoadCyclePendingPressureIntervalSeconds 2 -TimeoutSeconds 1600 -SkipBuild`. Evidence `docs/debug/evidence/GAME-SMOKE/20260704-081844` passed with `RunStatus=Passed`, `SaveLoaded=Passed`, `SaveLoadCyclePendingPressure=Passed`, `NoFatalInstanceWindow=Passed`, `ProcessExited=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, and `SaveLoadBoundary=Passed`.
- The pressure run published exactly 600 current-run `Smoke.SaveLoadCycle = pending` lines (`published=600, expected=600`), then produced exactly one `LoadGame requested for slot/index 2. requestId=SL-0001`, one `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`, and a closed SaveLoad summary with `requests=1`, `duplicateRequests=0`, `nativeEnter=1`, `nativeReturn=1`, `saveLoaded=1`, `timeouts=0`, and `fatalWindows=0`.
- Classification update after pressure isolation: the earlier `706` pending count is not evidence that the phase-five duplicate-LoadGame fix accidentally disabled the thing that made the long gate pass. Dense `Smoke.SaveLoadCycle=pending` publication by itself did not reproduce Fatal GC. Keep ISSUE-010 focused on native title-return-to-next-load object graph/lifecycle state unless future evidence shows duplicate requests or another bounded owner count growing.

2026-07-07 phase 8.22 input polling diagnostics and virtual pressure:

- Added smoke-only input polling diagnostics and a smoke-only virtual input owner pressure route. The diagnostics count registered input-button union size, frames, total per-button polls, `GetKeyDown` calls, `GetKey` calls, context buckets, pressed/released events, and reflected backend attempts/misses/exceptions. The pressure route registers synthetic owner-bound buttons through the same input owner path as normal mods.
- Count-only control evidence `docs/debug/evidence/GAME-SMOKE/20260707-152226` validated the counters. It used a title-only `SaveSlot=0` route and therefore failed the existing always-requested SaveLoad coordinator gate, but `SmokeInputPollingDiagnostics=Passed`. Final summary: `elapsedSeconds=324.9`, `frames=1169`, `registeredButtonsLast=9`, `registeredButtonsMax=9`, `totalButtonsPolled=10521`, `getKeyDownCalls=10521`, `getKeyCalls=10521`, `titleButtonsPolled=10368`, `gameplayButtonsPolled=153`, and reflected backend attempts `legacyAttempts=21042`, `inputSystemAttempts=21042`, `win32Attempts=21042`.
- This proves the current idle poll loop asks both `GetKeyDown` and `GetKey` once per registered button per frame. The baseline union was 9 under this profile, matching the active registered hotkey/input button graph rather than only the previously discussed 7-key YConsole+Zoom pair.
- Virtual pressure evidence `docs/debug/evidence/GAME-SMOKE/20260707-153023` registered `DTMAPI.Smoke.VirtualInput` with `requestedKeys=50`, `registeredKeys=50`, `ownerButtonCount=50`, `registeredButtonUnion=57`, and `pressureSucceeded=True`. Final input summary showed `totalButtonsPolled=18924`, `getKeyDownCalls=18924`, `getKeyCalls=18924`, and reflected backend attempts `37848` per backend family. Existing save-load result gates mismatched, but the pressure path itself was verified and no fatal occurred in the short confirmation.
- The 20-minute pressure route `docs/debug/evidence/GAME-SMOKE/20260707-153322` reproduced `Fatal error in GC / Unexpected mark stack overflow` at the first post-idle native save entry with 50 virtual keys. `result.json` reported `RunStatus=Aborted`, `NoFatalInstanceWindow=Failed`, `SaveLoadCycle=Failed`, `SaveLoaded=Failed`, `SaveLoadBoundary=Failed`, `FatalWindowProcessDump=Captured:DbgHelpFull`, `UnityCrashFreshness=fresh`, and `ProcessExited=Passed`.
- The fatal was detected at `2026-07-07T15:53:31.1011441+08:00`. The last DTMAPI native breadcrumb was `NativeContinuation.Step=DolocAPI.LoadGame.Enter ... requestId=SL-0001 ... slot=2 ... nativeEnter=1 nativeReturn=0 saveLoaded=0`, so this sample remains a pre-`SaveLoaded` native `LoadGame` mark-stack overflow.
- Final input summary before the fatal load: `elapsedSeconds=1194.2`, `frames=4657`, `registeredButtonsLast=57`, `registeredButtonsMax=57`, `totalButtonsPolled=265449`, `getKeyDownCalls=265449`, `getKeyCalls=265449`, `titleButtonsPolled=264480`, `gameplayButtonsPolled=969`, reflected `legacyAttempts=530898`, `inputSystemAttempts=530898`, `win32Attempts=530898`, `legacyExceptions=530898`, and `win32ForegroundMisses=530898`.
- Live DbgHelp dump metadata: `docs/debug/evidence/GAME-SMOKE/20260707-153322/Process-Dumps/DolocTown-17360-fatal-live-dbghelp.dmp`, size `4946066230`, SHA256 `B9D5BEF2648843E0E5AFC9945AEAFA8CEEF0219B29379795435CE931D81F28F0`. The evidence package includes `TOO-LARGE-DUMP-README.txt`; the dump remains local and should not be distributed.
- Classification: registered input polling/root count is now a verified pressure amplifier and shorter reproducer. The synthetic 50-key owner follows the same owner-bound input-button path as normal hotkeys and can pull the known native `LoadGame` GC fatal forward to about 20 minutes. This is not sole-root-cause proof for YConsole/Zoom; it shows the input root/polling graph can move the native/managed root set over the Mono mark-stack threshold earlier.
- The planned long run with 50 virtual keys was skipped because the 20-minute gate already reproduced. Next fix work should reduce the real runtime input polling/root graph before another marathon route: avoid title polling for hotkeys that do not need title input, context-gate disabled/non-current hotkeys, and reorder release checks so idle keys do not call `GetKey` unless the runtime believes the button is currently down.
- Regression row: `SAVELOAD-INPUT-POLLING-VIRTUAL-PRESSURE-20260707`. Update record: `docs/updates/2026/20260707-0003-input-polling-virtual-pressure.md`.

2026-07-07 hotkey rebuild and 50-key virtual pressure retest:

- Implemented the runtime mitigation requested after Phase 8.22: `DTMAPI.Abstractions` now has typed physical buttons, keybinds, keybind lists, input scopes, owner-bound keybind registrations, and typed keybind events. Core/Bootstrap now build one scoped cached input frame per update and ordinary hotkeys no longer use the old per-frame registered-string union/sort/reflection polling path. `RegisterButton` remains as a compatibility wrapper over the new registry. Reflected Unity input remains available for title key capture, diagnostics, and explicit fallback paths.
- Migrated the first consumers without splitting product mod package boundaries: DebugConsole registers Y/Escape in save-loaded scope, Zoom registers canonical keybind lists without duplicate `Plus`/`Equals` physical aliases, AutoFishing registers F6 through the typed keybind path and reads manual-cancel keys from the current input snapshot only while automation is enabled, while ActionSpeed and OneActionComplete continue through the compatibility wrapper backed by the new registry.
- Five-minute count evidence `docs/debug/evidence/GAME-SMOKE/20260707-172738` confirmed the ordinary hotkey path no longer publishes old registered-string polling. The script result still had the existing long-title/save-load gate mismatch, but `SmokeInputPollingDiagnostics=Passed`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`. Final input summary: `elapsedSeconds=292.9`, `frames=1052`, `registeredButtonsMax=8`, `totalButtonsPolled=128`, `getKeyDownCalls=0`, `getKeyCalls=0`, `cachedStateSamples=128`, `titleFrames=1036`, `titleButtonsPolled=0`, `gameplayFrames=16`, and `gameplayButtonsPolled=128`.
- Two-minute 50-key confirmation evidence `docs/debug/evidence/GAME-SMOKE/20260707-173334` verified the synthetic owner still registers 50 virtual keybinds, but uses the new cached input frame route rather than restoring the old poll loop. The script result had the same existing gate mismatch, while `SmokeVirtualInputPressure=Passed`, `SmokeInputPollingDiagnostics=Passed`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`. Virtual summary: `requestedKeys=50`, `registeredKeys=50`, `ownerButtonCount=50`, `registeredButtonUnion=56`, `pressureSucceeded=True`. Input summary: `registeredButtonsMax=56`, `totalButtonsPolled=896`, `getKeyDownCalls=0`, `getKeyCalls=0`, `cachedStateSamples=896`, `titleFrames=316`, `titleButtonsPolled=0`, and `gameplayButtonsPolled=896`.
- Short functional smoke evidence `docs/debug/evidence/GAME-SMOKE/20260707-173636` passed with `RunStatus=Passed`, `SaveLoaded=Passed`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`. It verified `DebugConsoleOpenY1=Passed`, `DebugConsoleCloseEscape=Passed`, `DebugConsoleOpenY2=Passed`, `DebugConsoleCloseY=Passed`, and `Zoom=Passed`. The `AutoFishingHotkey` result field was skipped by the existing harness gate, but logs show `Input F6 pressed dispatched` and `Fishing automation state Yuuka.DTMAPI.AutoFishing enabled=True reason=hotkey F6`.
- The comparable 20-minute 50-key pressure route `docs/debug/evidence/GAME-SMOKE/20260707-173940` passed after 1200 seconds of title idle plus save entry. Result fields included `RunStatus=Passed`, `SaveLoadCycle=Passed`, `SmokeInputPollingDiagnostics=Passed`, `SmokeVirtualInputPressure=Passed`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`; no Fatal GC window or live dump was captured. Input summary: `elapsedSeconds=1193.2`, `frames=4656`, `registeredButtonsMax=56`, `totalButtonsPolled=1008`, `getKeyDownCalls=0`, `getKeyCalls=0`, `cachedStateSamples=1008`, `titleFrames=4638`, `titleButtonsPolled=0`, `gameplayFrames=18`, and `gameplayButtonsPolled=1008`.
- Classification update: the old registered-string polling path was a verified amplifier, and the new scoped cached keybind path removes that amplifier from ordinary title idle. The 50 virtual keybinds still exist as registered owner roots, but they no longer generate title-idle reflected input calls. This is a mitigation/evidence improvement, not ISSUE-010 closure: the native `LoadGame` mark-stack crash class remains open until the original unsuppressed YConsole+Zoom route and at least one no-virtual long route pass under the rebuilt input layer.
- Regression row: `INPUT-HOTKEY-REBUILD-50-VIRTUAL-PRESSURE-20260707`. Update record: `docs/updates/2026/20260707-0004-hotkey-rebuild.md`.

2026-07-07 hotkey edge sampling follow-up:

- User manual testing of the first hotkey rebuild was reclassified as half-pass. AutoFishing, Y console, Y console internal input, and Zoom were usable, but short/soft taps and rapid repeated taps did not feel native enough. Code review found the rebuild sampled only current down-state and made Core derive edges, so a `down -> up` between DTMAPI frames could be missed and a later tap could be swallowed if release was missed.
- Implemented the edge-sampling follow-up: Bootstrap/Core now pass internal `InputButtonSample` frames with `IsDownNow`, `PressedEdge`, and `ReleasedEdge`; Bootstrap reuses a sample list instead of allocating a dictionary for ordinary hotkey frames; `ReflectedUnityInput.SampleButtonCached` samples Input System `wasPressedThisFrame` / `isPressed` / `wasReleasedThisFrame`, Win32 transition/current state with local release repair, and legacy `GetKeyDown` / `GetKey` / `GetKeyUp` fallback.
- Core now dispatches button press from `PressedEdge || IsDownNow && !wasDown` and release from `ReleasedEdge || !IsDownNow && wasDown`. Keybind press uses all-buttons-down plus any member pressed this frame, while single-key binds can dispatch from `PressedEdge` even when the key is already up in the same received sample. This preserves short taps and lets a new press edge dispatch even if the previous release was missed.
- `Control`, `Shift`, and `Alt` remain logical generic modifiers and match either physical side; `LeftControl` / `RightControl` and other side-specific bindings remain distinct. Config conflict detection expands generic modifier chords to physical alternatives so generic conflicts with either side, while left/right specific chords do not conflict with each other.
- Five-minute edge-count evidence `docs/debug/evidence/GAME-SMOKE/20260707-185110` confirmed the ordinary hotkey path still does not restore old registered-string polling. The script result retained the existing long-title/save-load gate mismatch, but `SmokeInputPollingDiagnostics=Passed`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`. Final input summary: `elapsedSeconds=288.7`, `frames=1054`, `registeredButtonsMax=8`, `totalButtonsPolled=144`, `getKeyDownCalls=0`, `getKeyCalls=0`, `cachedStateSamples=144`, `cachedPressedEdges=0`, `cachedReleasedEdges=0`, `titleFrames=1036`, `titleButtonsPolled=0`, and `gameplayButtonsPolled=144`. Earlier evidence `docs/debug/evidence/GAME-SMOKE/20260707-184940` is invalid because `AutoExitAfterSeconds=20` preempted the intended five-minute title idle.
- Two-minute 50-key confirmation evidence `docs/debug/evidence/GAME-SMOKE/20260707-185717` verified 50 synthetic keybinds on the edge-sampling path. The script result retained the existing gate mismatch, but `SmokeVirtualInputPressure=Passed`, `SmokeInputPollingDiagnostics=Passed`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`. Virtual summary: `requestedKeys=50`, `registeredKeys=50`, `ownerButtonCount=50`, `registeredButtonUnion=56`, `pressureSucceeded=True`. Input summary: `elapsedSeconds=110.7`, `frames=334`, `registeredButtonsMax=56`, `totalButtonsPolled=952`, `getKeyDownCalls=0`, `getKeyCalls=0`, `cachedStateSamples=952`, and `titleButtonsPolled=0`.
- Third-save functional evidence `docs/debug/evidence/GAME-SMOKE/20260707-190231` passed the hotkey dispatch paths while `RunStatus` failed only because `DiagnosticsReportExport=Failed`. Result fields included `SaveLoaded=Passed`, `DebugConsoleOpenY1=Passed`, `DebugConsoleCloseEscape=Passed`, `DebugConsoleOpenY2=Passed`, `DebugConsoleCloseY=Passed`, `DebugConsoleTenYShortTaps=Passed`, `DebugConsoleHoldYNoFlicker=Passed`, `Zoom=Passed`, `AutoFishingInputLog=Passed`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`. Logs show `Input F6 pressed dispatched`, `Keybind Yuuka.DTMAPI.AutoFishing/auto-fishing.toggle pressed trigger=F6`, and `AutoFishing automation enabled reason=hotkey F6`. Repeated F6 log lines in this evidence are attributed to the smoke harness repeatedly sending external F6 while waiting for its result gate.
- The comparable 20-minute 50-key pressure route `docs/debug/evidence/GAME-SMOKE/20260707-191553` passed after 1200 seconds of title idle plus save entry. Result fields included `RunStatus=Passed`, `SaveLoaded=Passed`, `SaveLoadCycle=Passed`, `SmokeInputPollingDiagnostics=Passed`, `SmokeVirtualInputPressure=Passed`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`; no Fatal GC window or live dump was captured. Final input summary: `elapsedSeconds=1193.5`, `frames=4652`, `registeredButtonsMax=56`, `totalButtonsPolled=840`, `getKeyDownCalls=0`, `getKeyCalls=0`, `cachedStateSamples=840`, `cachedPressedEdges=0`, `cachedReleasedEdges=0`, `titleFrames=4637`, `titleButtonsPolled=0`, `gameplayFrames=15`, and `gameplayButtonsPolled=840`.
- Classification update: edge sampling fixes the first rebuild's native-feel regression while preserving the scoped active-button pressure reduction. This is still a mitigation/evidence improvement, not ISSUE-010 closure. Require user manual retest for short/soft taps and rapid repeated taps, plus original unsuppressed YConsole+Zoom long route and one no-virtual long route, before changing ISSUE-010 status.
- Regression row: `INPUT-HOTKEY-EDGE-SAMPLING-50-VIRTUAL-PRESSURE-20260707`. Update record: `docs/updates/2026/20260707-0005-hotkey-edge-sampling.md`.

2026-07-07 AutoFishing hotkey regression follow-up:

- User manual testing after edge sampling passed YConsole and Zoom, but AutoFishing regressed: F6 repeatedly toggled automation and movement no longer cancelled it.
- Reclassified `docs/debug/evidence/GAME-SMOKE/20260707-190231`: the repeated `auto-fishing.toggle` / enable-disable lines were confirmed product regression evidence, not only smoke harness retry noise.
- Fixed Win32 cached sampling so `GetAsyncKeyState` transition low-bit produces `PressedEdge` only when local DTMAPI state was not already down. This preserves short-tap recovery but prevents held-state repeated edges.
- Added an AutoFishing toggle-release guard so one physical F6 press cannot flip automation more than once before release.
- Changed AutoFishing movement cancel from temporary typed keybind events to temporary `RegisterButton` snapshot tracking while automation is enabled. Disabled idle owner input now stays at one registration for AutoFishing in local smoke evidence instead of keeping movement cancel buttons registered.
- Source validation passed: Release build, Release unit tests after rebuild, console unit runner, and `git diff --check`.
- Runtime attempts `docs/debug/evidence/GAME-SMOKE/20260707-195714`, `docs/debug/evidence/GAME-SMOKE/20260707-195938`, and `docs/debug/evidence/GAME-SMOKE/20260707-200227` are retained as blocked F6-smoke evidence: all had clean process/fatal checks, but external F6 sends failed at the window-focus/input-target layer and therefore cannot prove AutoFishing F6 behavior. `20260707-195938` confirms local AutoFishing idle input registration count is one after the manual-cancel registration change.
- Classification: this fixed an AutoFishing consumer/backend-edge regression on top of the edge-sampling hotkey rebuild. The blocked F6 smoke evidence is superseded by the 2026-07-08 manual and typed-frame smoke pass; broader ISSUE-010 classification remains governed by the later runtime closure section.
- Regression row: `INPUT-HOTKEY-AUTOFISHING-REGRESSION-20260707`. Update record: `docs/updates/2026/20260707-0006-autofishing-hotkey-regression.md`.

2026-07-07 hotkey edge follow-up:

- A second user/code review accepted the scoped edge-sampling direction but found remaining native-feel gaps: chord short taps still required every member to be currently down, typed `KeybindReleased` did not consume `ReleasedEdge`, Win32 fallback could not guarantee every rapid-retap edge, hot-path allocations remained in some loops, and owner-bound helper queries could read same-id state from another owner.
- Updated `DtmKeybind` / `DtmKeybindList` so pressed checks accept `wasReleased` as a same-frame participating state and release checks consume `ReleasedEdge` directly. This lets `Ctrl+F6` dispatch when `Ctrl` is held and `F6` has already gone down/up inside the sampled frame, then dispatches `KeybindReleased` without leaving aggregate keybind state stuck.
- Core now records release-edge frame state separately from legacy `ButtonReleased` dispatch. Same-frame short taps can feed typed keybind release events while preserving the previous conservative compatibility behavior that does not synthesize `ButtonReleased` for a key DTMAPI never tracked as down.
- Owner-bound helper `IsKeybindDown` / `WasKeybindPressed` now resolve exact owner registration keys, while the global helper keeps compatibility behavior.
- Replaced ordinary-frame LINQ/temporary arrays in active registration iteration, inactive keybind cleanup, keybind list state checks, suppression checks, and trigger lookup with cached buffers and manual loops. Dirty-path cached button array rebuilds remain expected.
- Source validation passed: Release build and the console unit runner with `DOTNET_ROLL_FORWARD=Major`. New tests cover chord short tap pressed+released ordering, release-edge cleanup, owner same-id isolation, rapid retap behavior, and legacy tracked-button compatibility.
- No runtime smoke was rerun for this internal input-frame semantics follow-up at the time. The 2026-07-08 runtime closure section later supplied the original-route and no-virtual long evidence.
- Regression row: `INPUT-HOTKEY-EDGE-FOLLOWUP-20260707`. Update record: `docs/updates/2026/20260707-0007-hotkey-edge-followup.md`.

2026-07-08 hotkey edge follow-up runtime closure:

- User manual retest passed after the edge follow-up and AutoFishing guard fixes: AutoFishing F6 on/off, AutoFishing movement cancel, Y console, Zoom, and return-to-title behavior all felt correct. AutoFishing manual cancel keys were tightened to the fishing-state movement keys A/D/Space/Shift; W/S are not treated as current fishing movement-cancel inputs.
- Added a smoke-only typed-frame input helper so runtime smoke can dispatch the same three-state keybind frame used by the rebuilt hotkey layer. This fixed the old smoke gap where `RecordInputPressed` only exercised legacy `ButtonPressed` and did not prove typed `KeybindPressed` consumers. AutoFishing movement-cancel smoke still validates the snapshot model: it dispatches F6 through a typed frame, holds A down for one runtime update, and lets AutoFishing read `helper.Input.IsDown(A)` from `UpdateTicked`.
- Short functional smoke `docs/debug/evidence/GAME-SMOKE/20260708-021606` passed with `RunStatus=Passed`, `SaveLoaded=Passed`, `DebugConsoleOpenY1/CloseEscape/OpenY2/CloseY/TenYShortTaps/HoldYNoFlicker=Passed`, `Zoom=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`. Its input diagnostics kept `legacyRegisteredStringPath=false`, `getKeyDownCalls=0`, and `getKeyCalls=0`.
- A comparable 50-key 20-minute pressure run `docs/debug/evidence/GAME-SMOKE/20260708-030543` passed under the CoreCustomAnimals + local YConsole + local Zoom light diagnostic route after 1200 seconds title idle. `SmokeVirtualInputPressure=Passed`, `requestedKeys=50`, `registeredKeys=50`, `registeredButtonUnion=54`, `SaveLoadCycle=Passed`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`. Input diagnostics stayed on the new path: `legacyRegisteredStringPath=false`, `getKeyDownCalls=0`, `getKeyCalls=0`, `titleFrames=4636`, `titleButtonsPolled=0`, and `gameplayButtonsPolled=918`. Excluded attempt `docs/debug/evidence/GAME-SMOKE/20260708-021859` is launch/profile invalid: Steam did not reach fresh DTMAPI startup and only CoreCustomAnimals assets were enabled.
- The original unsuppressed YConsole+Zoom long route `docs/debug/evidence/GAME-SMOKE/20260708-032723` passed with no virtual keys, no input-root suppression, `SaveLoadCycleCount=2`, `SaveLoadCycleInitialTitleIdleSeconds=3600`, `SaveLoadObjectSnapshotMode=Lite`, `SmokeRootIsolationProfile=UiRuntime`, and `SmokeNativeLoadContinuationProbe=VersionPatcher`. It used local YConsole/Zoom plus the previous base non-UI extras and reached `NativeContinuation.Step=DolocAPI.LoadGame.Exit` for `SL-0002` with `SaveLoaded=2`; `NoFatalInstanceWindow=Passed` and `ProcessExited=Passed`.
- The no-virtual long route with input diagnostics `docs/debug/evidence/GAME-SMOKE/20260708-042909` also passed after 3600 seconds title idle and two post-idle save loads. It reported `RunStatus=Passed`, `SaveLoaded=Passed`, `SaveLoadCycle=Passed`, `SmokeInputPollingDiagnostics=Passed`, `SmokeVirtualInputKeyCount=0`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`; final diagnostics showed `legacyRegisteredStringPath=false`, `elapsedSeconds=3621.3`, `registeredButtonsMax=6`, `totalButtonsPolled=256`, `getKeyDownCalls=0`, `getKeyCalls=0`, `titleFrames=14315`, `titleButtonsPolled=0`, and `NativeContinuation.Step=DolocAPI.LoadGame.Exit` for `SL-0002`.
- Classification update: the old main-menu/title-idle hotkey input-pressure path is mitigated for the tested YConsole+Zoom pressure island. The evidence supports marking the main-menu long-idle GC issue as pressure mitigated, not solved. ISSUE-010 remains open for the broader long-term gameplay/native GC crash class and future crash packages should continue to preserve native crash evidence, lifecycle counters, and title-return/load breadcrumbs.

2026-07-08 FullKnown one-hour title plus 10-cycle validation:

- User requested a broader validation after the hotkey/input-pressure mitigation: Steam launch, every DTMAPI-known local/Workshop mod profile, one continuous hour on the title screen, then 10 save-load cycles with one-minute title intervals instead of the older five-minute cadence.
- Runtime evidence `docs/debug/evidence/GAME-SMOKE/20260708-075829` passed under the shared runtime lock. Command shape: `-UseSteam`, `-IncludeHookProbe`, `-OfficialModProfile FullKnown`, `-AutoExerciseSaveLoadCycle`, `-SaveLoadCycleCount 10`, `-SaveLoadCycleInitialTitleIdleSeconds 3600`, `-SaveLoadCycleIntervalSeconds 60`, `-SaveLoadCycleInSaveSeconds 5`, `-TimeoutSeconds 5400`, `-FatalWindowCrashDumpGraceSeconds 30`, `-FatalWindowProcessDumpMode DbgHelpFull`, and `-SmokeInputPollingDiagnostics`.
- Result fields: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `SaveLoadCycle=Passed`, `SmokeInputPollingDiagnostics=Passed`, `NoFatalInstanceWindow=Passed`, and `ProcessExited=Passed`. Startup analysis classified the run as `NormalDtmapiStartup`, not Steam launch blocking. `OfficialModProfileRestored=True` and runtime lock cleanup succeeded.
- Input diagnostics remained on the rebuilt path: `legacyRegisteredStringPath=false`, `getKeyDownCalls=0`, `getKeyCalls=0`, `titleButtonsPolled=0`, `registeredButtonsMax=7`, `totalButtonsPolled=1462`, and no virtual input pressure was used.
- Classification update: this is the strongest post-fix main-menu validation so far because it combines Steam launch, `FullKnown`, one uninterrupted title hour, and repeated post-idle save entries. Mark the main-menu/title-idle pressure path as successfully mitigated for this tested profile. Keep ISSUE-010 open for long-term gameplay/native GC crashes because this test still does not cover arbitrary long play, room churn, extended fishing loops, equipment/UI usage, or future player crash packages.

2026-07-10 input-watch and first-party AutoFishing primitives follow-up:

- Replaced local snapshot's rotating per-owner `HashSet` copies with stable generation-based owner/button watches and a cached active-button union. Unit coverage warms the query/sample/record/clear route and asserts `GC.GetAllocatedBytesForCurrentThread` does not increase for the steady-state frame.
- Added generic isolated owner cleanup from Core into GameBridge product services. Failed Entry cleanup has source/unit coverage for removal of policy, state, callback, lease, session, and lifecycle state; cleanup participant failures are retained in a bounded summary rather than aborting other owners.
- Moved first-party AutoFishing onto scalar Fishing Primitives with a single session, sequence-checked native transactions, scoped input/animation leases, a due-time scheduler, cached reflection/native handles, and pool scanning only on an actual invalid-cache cast attempt.
- Added runtime counters for primitive sessions/leases, transitions, cast actions, rejected actions, provider calls/failures, pool scans, reflection misses, Wait/Bite/MiniGame/Pull stages, native bite preparation, visible reels, skip reels, and shadow mismatches.
- Fifth-save evidence passed DefaultLoop/short soak (`GAME-SMOKE/20260710-102651`), independent FastAnimations (`103205`), independent InstantBite (`103350`), and independent SkipMiniGame (`105201`). Close summaries return owner state, sessions, leases, native transients, animators, and hook physics to zero. Normal fixtures used one fish-pool scan per session and bounded reflection misses.
- Classification: this removes a proven DTMAPI Gen0 allocation source and makes AutoFishing native/session state bounded in the tested short routes. It is not ISSUE-010 closure. No 100/500-loop current-build soak, broad no-water/no-rod matrix, or new long arbitrary gameplay route was completed.
- The first-party toggle migration then exposed a second measured Core-only hot path: registered-keybind evaluation rebuilt `owner + separator + id` three times per active frame, measuring 312 bytes/frame (3.12 MB over 10,000 warmed frames) in the focused test. `InputRegistrationState` now caches that stable key and the evaluator avoids per-frame delegates; the same warmed 10,000-frame registered route reports zero Core bytes on its test thread. This is not an end-to-end Bootstrap, Unity backend, or Mono allocation claim.
- Targeted current-build fifth-save validation `GAME-SMOKE/20260710-135136` passed F7 Gameplay input, DefaultLoop, four applied casts, three native Wait/Bite/MiniGame/Pull cycles, one extra soak loop, report export, F7 close cleanup to zero sessions/leases/native transients, clean process exit, and no fatal window. This is short functional/lifecycle evidence only; it does not close the 100/500-loop or arbitrary long-gameplay GC gates.

2026-07-10 per-frame input, bounded config audit, and Ready lease follow-up:

- The latest manual feedback kept AutoFishing's loop/config behavior passing but reported rapid F6/F7 taps still felt worse than native input. Runtime review found the intended Unity Update/Coroutine callbacks stopped after startup/load, leaving the 250ms fallback as the effective Gameplay sampler. InputSystem-only and PlayerLoop-only probes were not scene-stable across native LoadGame.
- Current implementation latches watched press/release edges by stable generation until Core consumes them once, uses compiled InputSystem bool getters, and drains normal Gameplay through a GameBridge-owned `DolocTown.NormalGameState.OnUpdate(float)` Postfix. Bootstrap retains one PlayerLoop title/non-normal fallback and suppresses its sampling while the native drain is current; the Timer performs health checks only.
- Config preview success no longer appends permanent owner-ledger rows. It aggregates by stable owner/item/kind/operation; the focused 200-scope test records 400 observations in two aggregates with no retained success detail. A matching 200-failure test preserves counts while retaining only 64 recent failure rows. The general ledger is capped at 2048.
- AutoFishing now retains phase as an enum, builds movement text only after real movement, avoids per-Ready-tick list/join/summary creation, and publishes Fast success once per flow. Focused warm-tick coverage verifies repeated Ready updates keep the same summary string reference.
- The internal first-party animation lease now carries independent Ready/Cast/Pull multipliers. Ready body/rod animators restore from exact snapshots, and its native charge timer scales without changing the `0..1` target. Unit evidence verifies `1.25 -> 5 -> 1.25` at multiplier 4.
- Retained runtime evidence `GAME-SMOKE/20260710-171352` passed current-build external input/hotkey, target `0.5`, Ready/Cast speed, cleanup, process exit, and fatal-window gates; its scripted `HorizontalMoveFactor=-1` correctly cancelled before the complete loop. Probes `172128`, `172756`, and `173218` retain the rejected driver lifecycle failures.
- Final cadence evidence `GAME-SMOKE/20260710-173812` installed `GameLoop.NativeFrameDrain` and recorded 2394 Gameplay frames/11968 button samples in 6.8 seconds, `nativeGameFrameCallbackSeen=True`, clean process exit, and no fatal window. Three external F6 sends failed before input reached the game, so this evidence neither proves nor disproves physical rapid taps.
- Classification: this removes one retained-growth path and several measured/source-proven Gen0 hot paths, and replaces 4Hz Gameplay sampling with a verified native per-frame drain. It does not close ISSUE-010: physical 40ms F6/F7 and visual Ready still need manual retest, and no current-build 100/500-loop or broad long-gameplay soak was run.
- Regression row: `INPUT-FRAME-GC-READY-20260710`. Update: `docs/updates/2026/20260710-0005-input-frame-gc-ready-animation.md`.
- The first player retest then found a functional regression hidden by the cadence-only sample: one F6 held cycle emitted press twice 17ms apart, so AutoFishing enabled and immediately disabled before release. The package, primitive session, Ready animator `1->3`, and charge Hook had all run; “Mod not enabled” was the correct player-visible effect but not the loading root cause.
- Fixed the platform latch to accept only one source per Unity frame and suppress repeated backend pressed flags while a key remains down without release. Restored AutoFishing's consumer-level release guard. Unit coverage verifies source duplicate rejection, held duplicate rejection, release, and next legal press.
- Fifth-save `GAME-SMOKE/20260710-185323` passed one press/one release, complete DefaultLoop, three visible minigame/result cycles, one extra soak loop, second-F6 cleanup, report export, clean process exit, and no fatal window. Source correction from the player: both F6 actions were manually pressed during smoke, not injected by the script, so classify it as manual-assisted runtime proof and not sender proof. `185537` partially verifies target 1 plus Ready/Cast Fast but overshoots the fixture water; `185857` behavior-verifies InstantBite + Skip + Fast and next cast at charge 0 while retaining an external-sender result-gate failure.
- The final player retest passed AutoFishing functionality, individual configuration effects, charge adjustment, and the overall requested manual matrix. This closes the immediate enable-then-disable and scoped manual behavior regressions. It does not verify the smoke sender and does not close ISSUE-010 long-gameplay/100–500-loop gates.

2026-07-10 AutoFishing hot-path/native-cache source follow-up:

- `20260710-0006` moves first-party snapshot availability/movement into a Hook-fed `FishingNativeStateCache`. Warmed `GetSnapshot()` is a pure struct read: 10,000 unit-test calls keep sequence/native-read counts unchanged and report zero current-thread allocation.
- Minigame status/note/time and first-party `RollFish`/bite fields/`InvokeFishOnHookTip`/`NextState`/skip flag/`Overwrite` use cached typed delegates. Warmed 10,000 minigame and native-transaction reads report zero current-thread allocation with no accessor rebuild or reflective fallback.
- Failed fast-access capabilities latch unavailable and log once; high-frequency callbacks fail open. First-party sessions no longer populate legacy fishing option/state or minigame handle dictionaries.
- Lifecycle publication now gates before full snapshot/detail construction. A 1,000-clean-success unit loop publishes once, suppresses 999, then permits the 30-second aggregate; exceptions/status changes/boundaries remain immediate.
- A disabled probe can later write exact current-thread allocations, Gen0, log bytes, snapshot builds, native transients, session/lease/scheduler state, and ReturnedToTitle cleanup for isolated 0/100/500 processes. Missing allocation-counter support marks the run blocked and never substitutes `GC.GetTotalMemory`.
- Classification remains `open`: this is source/unit hot-path evidence, not Unity Mono proof and not a current-build 0/100/500 or arbitrary long-gameplay run.
- Regression row: `AUTOFISHING-HOT-PATH-NATIVE-CACHE-20260710`. Update: `docs/updates/2026/20260710-0006-autofishing-hot-path-native-cache.md`.

2026-07-10 AutoFishing Hook-runtime single-fish Mono follow-up:

- Static Fishing callbacks now depend on internal `IFishingHookRuntime`. Ordinary first-party AutoFishing creates `FishingPrimitiveHookRuntime` without constructing the isolated `LegacyFishingAutomationService` or populating compatibility option/state maps.
- `UseFishRod` keeps its typed delegate across environment invalidation. Accessor counters now separate Builds, Rebuilds, BuildFailures, and InvocationFailures.
- Primitive release uses unconditional cleanup; a throwing transition subscriber cannot retain its provider, leases, scheduler, callback runtime, or cached native handles.
- Fifth-save `GAME-SMOKE/20260710-235813` completed one visible-minigame fish with InstantBite=true, SkipMiniGame=false, Fast x4, and charge 0, then disabled and returned to title. The gate passed with `accessorBuilds=25`, all rebuild/build/invocation failure counts zero, and all session/lease/scheduler/native/callback transient fields zero.
- Classification remains `open`: one fish proves the Unity Mono accessor and lifecycle path did not regress, but it is not 0/100/500 allocation evidence or a long-gameplay soak.
- Regression row: `AUTOFISHING-HOOK-RUNTIME-MONO-GATE-20260710`. Update: `docs/updates/2026/20260710-0008-autofishing-hook-runtime-seam.md`.

2026-07-11 AutoFishing reliability/0.5.3 bounded-runtime follow-up:

- Visible-reel input now confirms consumption, retries missed 250 ms edges, waits 500 ms for native progress after consumption, and interrupts after five seconds without forcing the native state machine. Primitive acquire fails closed on incomplete Hooks and transactionally rolls back every attachment before `activeSession` commit.
- Ready/Cast/Pull animation and Cast Hook physics now use typed cached access after warm-up. On the .NET 8 unit-test runtime, the 10,000-stage microtest reports zero current-thread allocation, one-time accessor construction, no reflection fallback/rebuild/failure, and exact animator/velocity/gravity/duration restoration; this does not establish Unity Mono allocation bytes.
- Three independent fifth-save one-fish runs passed: natural wait plus visible minigame `GAME-SMOKE/20260711-011246`, InstantBite plus Skip `GAME-SMOKE/20260711-011805`, and full-charge Fast x4 `GAME-SMOKE/20260711-012735`.
- `InactiveNoConsumer` completed a 60-second warm-up plus 600-second interval under `GAME-SMOKE/20260711-012851`: 0 cast/fish, 9 Gen0, 0 Fishing hot-log lines, accessor `0->0`, and all title cleanup fields zero. Its outer wrapper result was invalidated only by a now-fixed diagnostics-export expectation.
- `EnabledNoRod` final run `GAME-SMOKE/20260711-024427` completed the same bounded interval with 0 cast/fish, 9 Gen0, 0 Fishing hot-log lines, accessor `4->4`, native movement cancellation `manual-move HorizontalMoveFactor=-1`, and complete title cleanup.
- Allocation evidence correction: `GAME-SMOKE/20260711-070116` proves Unity Mono's present allocation method is nonfunctional: a kept-alive 4096-byte same-thread array leaves the counter at `0 -> 0`. Therefore both historical `AllocatedBytes=0` fields are invalid, and the nine Gen0 values do not identify thread bytes. Their non-allocation invariants above remain accepted.
- Classification remains `open`: these bounded samples remove deterministic reliability/allocation defects and establish two quiet baselines, but they do not cover the deferred 100/500-fish matrix, arbitrary long gameplay, room churn, or independent player crash recurrence. `IFishingAutomationApi` is frozen/obsolete but retained through a warning-bearing preview boundary.
- Regression row: `AUTOFISHING-RELIABILITY-053-PREVIEW-20260711`. Update: `docs/updates/2026/20260711-0001-autofishing-reliability-053-preview.md`.

2026-07-11 AutoFishing allocation-counter and visible-reel telemetry correction:

- The performance probe now requires a same-thread 4096-byte behavioral calibration before establishing allocation/Gen0 baselines. Missing, nonfunctional, or throwing counters block; allocation and Gen0 interval fields serialize as null.
- `FishingPrimitivesService` archives visible-reel queued/consumed/retry/timeout values exactly once when the primitive runtime detaches and exposes archived plus live totals. Diagnostics no longer emit blank released-runtime counter values, and performance JSON has start/end/delta fields.
- Fifth-save natural one-fish evidence `GAME-SMOKE/20260711-065929` records queued=1, consumed=1, retries=0, timeouts=0 after release and all title roots zero.
- Fifth-save short calibration evidence `GAME-SMOKE/20260711-070116` records outer `RunStatus=Blocked`, counter available but nonfunctional, calibration payload=4096, before/after/delta `0/0/0`, null allocation/Gen0 interval fields, visible-reel fields zero, title cleanup true, and clean process exit.
- Native `AgentStateFishingWait.NextState()` deducts energy when one of the three reviewed input getters is true. At this historical checkpoint the 500 ms post-consumption rearm was unchanged and input-getter consumption was not transaction confirmation; the immediately following `20260711-0003` section supersedes that risk with a native NextState acceptance Hook and delayed-phase fault injection.
- Classification remains `open`: the historical byte-allocation conclusion is withdrawn, the Mono allocation matrix is blocked, and no ten-minute replacement, 100/500 fish, soak, or arbitrary gameplay run occurred.
- Regression row: `AUTOFISHING-PERFORMANCE-COUNTER-REEL-TELEMETRY-20260711`. Update: `docs/updates/2026/20260711-0002-autofishing-performance-counter-reel-telemetry.md`.

2026-07-11 allocation-independent runtime trend and native reel acceptance follow-up:

- Allocation calibration is now a separate subprobe. Unity Mono still reports the same-thread byte counter as nonfunctional, but the performance scenario continues for its full duration and process Gen0/1/2 remain independent metrics.
- A general bounded Core probe samples Mono used/heap, Unity allocated/reserved, process private/working set, process GC generations, resource record count, owner roots, and caller accessor/reel/transient counters every 30 seconds without forced GC.
- First full reruns `GAME-SMOKE/20260711-073623` and `074859` exposed another Unity Mono stub: managed Process private/working values were zero. Windows `GetProcessMemoryInfo` fallback replaced the false zero; these runs are discovery evidence, not final process-memory baselines.
- Final `InactiveNoConsumer` `GAME-SMOKE/20260711-080312` completed 600.016 seconds/21 samples: allocation subprobe Blocked, outer Passed, process private delta `+29,982,720`, working delta `+29,851,648`, Gen0/1/2 `9/9/9`, and record/root/accessor/reel/transient deltas zero with clean title/process exit.
- Final `EnabledNoRod` `GAME-SMOKE/20260711-081532` completed 600.015 seconds/21 samples: process private delta `+28,221,440`, working delta `+29,614,080`, Gen0/1/2 `10/10/10`, 0 cast/fish, zero domain deltas, native HorizontalMoveFactor cancellation, and clean title/process exit.
- These roughly 28–30 MB process deltas are bounded baselines, not a leak threshold or closure claim. No forced GC, 100/500 fish, arbitrary gameplay soak, room churn, or independent player recurrence test ran.
- `AgentStateFishingWait.NextState` now confirms a consumed visible-reel edge only after native state selection returns a different state. Delayed-phase fault injection proves the accepted logical reel cannot rearm at 500 ms and its simulated energy/reel effect remains exactly once.
- Classification remains `open`: deterministic duplicate-reel risk is removed and quiet bounded trends are now truthful, but the broader long-gameplay/native GC crash class is not closed.
- Regression row: `AUTOFISHING-RUNTIME-MEMORY-NATIVE-REEL-20260711`. Update: `docs/updates/2026/20260711-0003-runtime-memory-trend-native-reel-acceptance.md`.

2026-07-11 thirty-minute inactive stable-window follow-up:

- `RuntimeMemoryTrendProbe` now reports startup `0..180s`, stable `180s..end`, and trailing final-600-second windows with endpoint trends and least-squares slopes. It also records sampled post-Gen2 MonoUsed low-water epochs. No forced GC is used.
- Fifth-save `InactiveNoConsumer` `GAME-SMOKE/20260711-115939` completed 1800.002 seconds with 61 untrimmed samples, allocation subprobe Blocked, outer Passed, Process Gen0/1/2 `30/30/30`, 0 cast/fish, zero DTMAPI record/root/accessor/reel/transient deltas, and clean title/process exit.
- Trailing ten minutes: MonoUsed delta `-139,452,416`, MonoHeap delta `0`, UnityReserved delta `0`, UnityAllocated delta `+1,069,349`, process working delta `+2,400,256` with OLS `+329,239 B/min`.
- Process private had a temporary non-linear wave around 1560–1680 seconds (`~4.97 GB`) and therefore a misleading positive trailing OLS slope, but returned to `4,530,388,992`, which is `42,098,688` below the trailing-window start. It is not a continuously rising baseline.
- Trailing post-Gen2 MonoUsed low waters descend overall from `795,127,808` at 1230 seconds to `725,606,400` at 1800 seconds. MonoHeap, trailing UnityReserved, and all DTMAPI structural metrics remain fixed.
- Classification: the inactive curve is common game/Unity/platform warming, collection sawtooth, and cache waves followed by a flat tail. Inactive AutoFishing GC suspicion is temporarily cleared; no platform-layer baseline is warranted by this result.
- For the AutoFishing branch of ISSUE-010, the remaining unverified item is an active long fishing loop. This 0-fish run does not claim that active-loop validation.
- Regression row: `AUTOFISHING-INACTIVE-30M-STABLE-WINDOW-20260711`. Update: `docs/updates/2026/20260711-0004-runtime-memory-stable-window.md`.

## Next Required Fixes

Do not mark solved until these are implemented and verified:

1. Preserve `SAVELOAD-REQUEST-COORDINATOR-20260703`, `SAVELOAD-CYCLE-LONG-RUN-20260704`, and `TITLE-RETURN-BOUNDARY-LEDGER-20260704` as release-blocker evidence for this crash class.
2. Preserve `SAVELOAD-CYCLE-DELTA-LEDGER-20260705` and `SAVELOAD-CYCLE-FEATURE-BISECTION-20260705` as the current phase 8.6 evidence set.
3. Keep `TitleReturnBoundaryLedger`, `TitleReturnObjectGraphSnapshots`, `TitleReturnBoundarySummary`, `SaveLoadCycleObjectDeltaSummary`, and `SaveLoadRequestSummary` in future crash packages and long smokes.
4. Preserve `SAVELOAD-UI-OWNER-SPLIT-PRELOAD-GC-20260705` as the Phase 8.7 evidence that YConsole, MoreEquipmentSlots, MoreSaves, Zoom, and the three high-value pairs did not isolate a DTMAPI-owned growing UI root under the one-hour-title-idle two-load route.
5. Preserve `SAVELOAD-FULL-PROFILE-FATAL-WINDOW-20260705` as the Phase 8.8 evidence that the full profile can still reproduce and that PreLoad forced GC passed before a later LoadGame/SaveLoaded fatal.
6. Preserve `SAVELOAD-SAVELOADED-ACTIVATION-BREADCRUMB-20260705` as the Phase 8.9 evidence that no-probe and PreLoad fatals had different immediate windows: native LoadGame activation before SaveLoaded versus full SaveLoaded snapshot publication/logging after SaveLoaded.
7. Preserve `SAVELOAD-COMPARABLE-FULL-NO-PROBE-BREADCRUMB-20260705` as the Phase 8.10 evidence that a second comparable no-probe full fatal still lands before SaveLoaded with zero `SaveLoaded.Step` breadcrumbs.
8. Preserve `SAVELOAD-DIAGNOSTIC-PRESSURE-LITE-20260706` as the Phase 8.11 evidence that reducing object snapshot pressure to Lite shifted the fatal window from pre-SaveLoaded terrain/dungeon activation to post-SaveLoaded, pre-native-return `AfterLoadArchiveData` / `MapManager.Init` texture allocation.
9. Preserve `SAVELOAD-NO-HOOKPROBE-LITE-20260706` as the Phase 8.12 evidence that HookProbe is not a necessary condition for the pre-SaveLoaded fatal recurrence, and that this specific fatal popup can occur with Lite snapshots active at `BeforeNextLoadGame` and `LoadGameNativeEnter`.
10. Do not rely on `GAME-SMOKE/20260706-025334` for stack-level native classification because it did not produce a fresh Unity crash directory/stack; use it for HookProbe/Lite boundary classification unless another fresh stack is captured.
11. Continue native LoadGame/save activation root-set analysis around terrain/dungeon activation, `AfterLoadArchiveData`, `MapManager.Init`, texture allocation, and the full stable title/process root-set before any broad service-level hard-disable experiment.
12. If service-level isolation begins, prefer one hard-disable control at a time under the no-HookProbe + Lite route, and start with diagnostic/root-heavy services before gameplay/content roots.
13. If native/root-set analysis identifies a definite DTMAPI-owned stale root, duplicate subscription, or stale binder, fix only that owner and rerun the shortest reproducer.
14. Do not destroy unknown native `GameObject`, `Component`, `AudioClip`, `AssetBundle`, or controller objects without ownership proof. Keep CustomAnimals and AnimalVoice count-first diagnostics intact.
15. If Fatal GC recurs with unsuppressed duplicate requests, treat SaveLoad coordination as the active suspect and inspect whether the duplicate came from native UI, DTMAPI/smoke fallback, or another owner/source.
16. Do not resume UI owner single/pair splits.
17. Preserve `SAVELOAD-FRESH-CRASH-CAPTURE-NO-HOOKPROBE-LITE-20260706` as the Phase 8.13 evidence that the no-HookProbe Lite profile can shift to a post-SaveLoaded/pre-native-return fatal window, with `SaveLoaded.Step=Hook.Exit` as the last DTMAPI line.
18. Do not begin service-level hard-disable solely from `GAME-SMOKE/20260706-100234`. First analyze the shifted post-SaveLoaded/native-return boundary or replace the failed `ComSvcsFull` dump capture with a more reliable fresh native dump route.
19. Preserve `SAVELOAD-NATIVE-CONTINUATION-PROBE-20260706` as the Phase 8.16 evidence that the UiRuntime + VersionPatcher-probe route can still fatal before SaveLoaded and before `AfterLoadArchiveData` / `VersionPatcher` / `MapManager.Init` breadcrumbs.
20. Next native/root-set work should center the recurring pre-SaveLoaded terrain/dungeon allocation path: `TerrainLayer`, `Room.ResetTerrain`, `DungeonArchiveData.AfterLoadData`, and `DataPersistenceManager.LoadGame`. Do not treat `VersionPatcher` as sufficient for all recurrences.
21. Preserve `SAVELOAD-MANAGED-ROOT-UI-CODE-ISOLATION-20260706` as the Phase 8.17 evidence that removing the four UI code mod owners (`DTMAPI.DebugConsoleMod`, `DTMAPI.MoreEquipmentSlotsMod`, `DTMAPI.MoreSavesMod`, `DTMAPI.ZoomMod`) reduced managed root pressure and passed twice under the Phase 8.16 light diagnostic route.
22. Do not mark ISSUE-010 solved from Phase 8.17 alone. Treat UI code roots as a strong suspect/amplifier and require a narrower confirmation axis, add-back, or root-owned fix before claiming root cause.
23. Preserve `SAVELOAD-MANAGED-ROOT-UI-PAIR-TRIAGE-20260706` as the Phase 8.18 evidence that adding back only `DTMAPI.DebugConsoleMod` and `DTMAPI.ZoomMod` was sufficient to reproduce the pre-SaveLoaded terrain/dungeon fatal under the same light diagnostic route.
24. Do not infer that YConsole alone or Zoom alone is proven. Next work should split that pair or inspect their shared input/static/config roots before broad service-level hard-disable.
25. Preserve `SAVELOAD-YCONSOLE-ZOOM-PAIR-DECOMPOSITION-20260706` as the Phase 8.19 evidence that Zoom-only and YConsole-only each passed once, while the pair reproduced again under the same route.
26. Next work should isolate root type inside the pair: Zoom input registrations, YConsole input registrations, both input registrations, and then event handlers if needed. Do not jump to UI triples, content/native-heavy isolation, or service hard-disable from this state.
27. Preserve `SAVELOAD-INPUT-POLLING-VIRTUAL-PRESSURE-20260707` as the Phase 8.22 evidence that the old owner-bound registered-string polling path was a verified amplifier and 20-minute reproducer.
28. Preserve `INPUT-HOTKEY-REBUILD-50-VIRTUAL-PRESSURE-20260707` as the first mitigation evidence that the ordinary hotkey path no longer polls registered strings during title idle and that the comparable 50-key 20-minute route passed.
29. Preserve `INPUT-HOTKEY-EDGE-SAMPLING-50-VIRTUAL-PRESSURE-20260707` as the native-feel follow-up to the first hotkey rebuild.
30. Preserve `INPUT-HOTKEY-AUTOFISHING-REGRESSION-20260707` as the AutoFishing-specific regression/fix evidence after edge sampling. The blocked F6 smoke attempts remain invalid as functional proof and are superseded by 2026-07-08 manual/runtime evidence.
31. Preserve `INPUT-HOTKEY-EDGE-FOLLOWUP-20260707` as the source/unit verified correction for chord short taps, typed keybind release edges, owner-bound query isolation, and residual hot-path allocations.
32. Preserve `INPUT-HOTKEY-EDGE-FOLLOWUP-RUNTIME-20260708` as the runtime closure evidence that manual retest, short functional smoke, 50-key pressure, original YConsole+Zoom long route, and no-virtual long route all passed under the edge-sampled input layer. Treat this as main-menu long-idle input-pressure mitigation, not closure of the broader long-term gameplay/native GC crash class.
33. Preserve `SAVELOAD-FULLKNOWN-ONE-HOUR-TEN-CYCLE-20260708` as the successful Steam `FullKnown` validation after the hotkey/input-pressure mitigation. Treat it as success for the tested main-menu one-hour plus repeated save-entry route, not proof that the broader long-term gameplay/native GC crash class is solved.

## 2026-07-11 inactive AutoFishing stop boundary and smoke isolation

- `GAME-SMOKE/20260711-115939` measured 1800.002 seconds of `InactiveNoConsumer` without enabling AutoFishing or fishing. It retained 61 samples, zero DTMAPI record/root/accessor/reel/transient deltas, flat MonoHeap/UnityReserved, a nearly flat trailing working set, descending MonoUsed/Gen2 low waters, and clean title/process cleanup.
- The private-memory wave peaking near 4.97 GB recovered below its trailing-window start and is classified as a common game/Unity/platform transient phenomenon. It is not evidence of an AutoFishing leak.
- Stop further inactive/no-consumer memory tests, platform-layer baselines, and extensions of the 30-minute process. This closes only the inactive AutoFishing sustained-retention suspicion; ISSUE-010 remains open.
- Active AutoFishing trend validation is deferred to formal-release readiness and should be one approximately 10-minute or 10–20-fish run. Do not run 100/500 for the current preview.
- Source follow-up `20260711-0005` separates `AutoFishingPrimitiveSmokeCase` from `LegacyFishingAutomationCompatibilitySmokeCase`. The primitive case cannot reference the frozen API, legacy service, or `FishingAutomationFeature.Service`; source gates also prevent product/Core/native/reflection/internal-API boundary regressions.
- AutoFishing retains one owner-bound Gameplay toggle as the bounded reliable-first-edge exception. Ordinary mods continue local `DtmKeybindList.JustPressed(helper.Input)` queries. This source-only follow-up did not acquire the runtime lock or run the game.

## Acceptance Criteria

- A new player or local crash package contains `Unity-Crashes/summary.txt` and recent Unity crash report files when Unity created them.
- Long-running lifecycle counters remain bounded across repeated room changes, title returns, equipment UI opens, FarmCase/RecipePanel use, and fishing loops.
- If a crash still occurs, the report contains enough native crash evidence to classify the failing object graph or root set.

## 2026-07-11 source/runtime owner-root hardening

- Update `20260711-0010` replaces ordinary-Mod successful registration history with bounded counters and authoritative registry root counts, releases quarantined delegates, caps `LogOnce` and diagnostic graphs, and unifies failure/disable/removal/shutdown owner cleanup.
- `SaveLoaded` and `ReturnedToTitle` continue to preserve active process-lifetime Mod services; only transient input/save state is cleared there.
- Final third-save `GAME-SMOKE/20260711-230125` passed the cross-domain OwnerLifetime control: eight Core + real Camera roots survived SaveLoaded, the Camera lease remained live until explicit owner deactivation, the cleanup owner and control owner both reached authoritative `remaining=0`, and HookProbe/final-health/fatal/process-exit gates passed with no remaining `DolocTown.exe`. Earlier Core-only `151205` and false-diagnostic `162335` evidence remain historical/superseded.
- This narrows DTMAPI-owned retention risk and adds an `OwnerLifetime` smoke assertion, but it does not identify or close the broader native/Unity Mono Fatal GC class. ISSUE-010 remains `open`.

## 2026-07-11 real first-party Zoom owner boundary

- Update `20260711-0013` moves the unchanged Zoom product source to `first-party-mods/ZoomMod` while retaining the ordinary Abstractions-only `netstandard2.0` boundary and demand-local input path.
- Final third-save `GAME-SMOKE/20260711-233720` loaded the actual `Local.DTMAPI_Zoom` official-local package exactly once, passed 4x/2x Camera lease arbitration, then deactivated the real owner at Core roots `19 -> 0` and Camera leases `1 -> 0` with Event/Input/ConfigPage/Content/API-facade/instance/loaded roots removed, provider retained, cleanup failures zero, and authoritative remaining zero.
- Reconciliation while the source remained enabled did not rerun Entry or republish roots; it retained the terminal restart-required state. This replaces the former synthetic-only Zoom owner evidence with a real ordinary-product boundary, but it is a bounded lifecycle test rather than a long gameplay/native allocation result. ISSUE-010 remains `open`.

## 2026-07-13 two-track scope and active-gameplay release priority

- The formal sixth-round product decision makes active AutoFishing and ActionSpeed the parallel highest-priority gameplay GC gates before DTMAPI 0.5.5. This is a release-risk ordering based on severity, long-session field correlation and animation/action throughput, not prevalence or a newly proven Fatal GC root cause. The user characterizes reports as affecting a minority of players and generally appearing during long-running play; that is field context, not a measured incidence statistic.
- Preserve two parallel evidence tracks:
  1. the title-idle/native LoadGame track, which has repeatedly produced Fatal GC while AutoFishing was disabled and currently remains associated with stable-root pressure plus native room/terrain/dungeon loading;
  2. the active-gameplay track, which must run separate but comparable AutoFishing and ActionSpeed speed ladders.
- The 30-minute `InactiveNoConsumer` result continues to clear only the current inactive-AutoFishing sustained-retention suspicion. It does not clear an active loop and should not be repeated as a substitute for active evidence.
- Current source routes AutoFishing through Fishing Ready/Cast/Pull and ActionSpeed through Tool/Interact/Eat/Continuous-use. Their common risk is independent acceleration of animation time and action-state progression, which may amplify the same class of Unity/Mono pressure; current evidence does not show same-Animator ownership.
- Each domain is compared at native 1x, enabled without acceleration, common acceleration, high multiplier, disable recovery and title-cycle levels. Results use per-real-minute plus per-fish/per-ActionSpeed-action denominators and examine throughput/short-lived allocation, animation-event/completion-callback allocation, small per-action residual and high-speed native/Mono state-transition pressure.
- Same-Animator arbitration is out of the default plan. It is added only if instrumentation observes a real overlapping owner/state.
- The controlled ladders and release criteria are owned by `docs/reviews/code/2026/20260713-0013-autofishing-actionspeed-active-gc-release-gate.md`. Preserve unavailable allocation metrics as `Blocked`/`null`, capture crash/profile/lifecycle evidence, and avoid forced GC as a product workaround.
- Passing this gate can establish bounded DTMAPI-owned state and no Fatal in the tested window. It must not close the broader Unity/Mono crash class or state that all GC problems are solved. ISSUE-010 remains `open`.

## 2026-07-13 release-baseline clarification

- The user accepts the current title-long-idle behavior as basically resolved for planning purposes. The evidence-safe interpretation is the existing one-hour-title plus ten-one-minute-save-load `FullKnown` pass after the hotkey/input-pressure mitigation.
- Treat that route as the current tested forward baseline. Do not repeat the historical title wording as though the same route is still known to fail on the current build, but retain the old Fatal evidence for regression analysis.
- This does not close active AutoFishing/ActionSpeed, other long gameplay routes or the global Unity/Mono Fatal class. Public communication should say “the tested title-long-idle route was mitigated and passed; a minority of long-session reports remain under investigation,” not “all GC issues are fixed.”
- The subscription/current/prerelease baseline boundary is owned by `docs/reviews/code/2026/20260713-0014-workshop-subscription-and-prerelease-baseline-review.md`.

## 2026-07-19 Batch 5 GC-ladder readiness checkpoint

- Batch 5 has corrected source-proven inactive-pressure defects around empty queue diagnostics, retained CustomAnimals/Audio/Camera/shared callbacks, evidence-only product ticks, content-generation commits, event membership, and physical route/cleanup reporting. Focused tests include stable 10,000-publication/event and 10,000-pass all-family zero-demand callback checks. These results establish bounded source/unit behavior only; they do not prove a Unity Mono GC fix.
- The executable GC path now distinguishes AutoFishing L4 disable/native-control recovery from L5 title/third-save reload/re-enable/one-loop/disable/final-cleanup behavior, and ActionSpeed retains separate L0-L5 receipts for Tool, Interact, Eat and ContinuousUse. The wrapper fails closed on missing duration, unit, behavior, provenance, cleanup or metric receipts; it does not force GC.
- No formal Batch 5 ActionSpeed or AutoFishing runtime ladder stage had completed at this checkpoint. Plan/source tests and short Local11/Zoom smokes are not GC evidence, and unavailable Unity Mono allocation counters must remain `Blocked`/`null` rather than being replaced with `GC.GetTotalMemory` claims.
- `GAME-SMOKE/20260719-031358` and `20260719-034102` passed their scoped Zoom and ordinary Local11/no-QA acceptance gates without a Fatal window and with clean process exit. Their minute-scale scope does not close the active-gameplay ladder or the broader long-session/native GC class.
- ISSUE-010 therefore remains `open`. The owning Batch 5 Update must record the independent runtime ladder results before Batch 5 can close, while any successful bounded ladder must still be described as tested-window allocation/root evidence rather than proof that all Unity/Mono GC crashes are solved.

## 2026-07-19 Batch 5 bounded runtime ladder result

- `BATCH5-NO-DEMAND/formal-final-qa-vitals-20260719-2000` passed a 300-frame warmup plus 10,000 measured real Unity frames on the exact Batch 5 candidate. Optional demand/updater membership was empty and optional file, directory, projection, reflection, native-updater, retained-callback, event-snapshot and event/Hook-diagnostic revision deltas were all zero. The real-Unity claim is call/cadence silence, not whole-game zero allocation; the warmed combined GameBridge/Core 10,000-call zero-allocation assertion remains an offline gate.
- `BATCH5-GC-LADDER/20260719-044157-70ce39e6` retains 24 completed third-save ActionSpeed stages: Tool, Interact, Eat and ContinuousUse at L0-L5, each with a 600-second window, 21 samples, no forced GC and passing behavior/source/save/exit evidence. The later obsolete parent AutoFishing continuation does not invalidate those completed child receipts.
- The initial fifth-save replay `formal-autofishing-final-candidate-20260719-1915` completed L0-L2 but stopped L3 because the official energy-maintenance fixture sampled every 1000 ms. That fixture could miss a 4x energy drop between samples; the stop was QA-authored and had no Fatal window, leftover process or failed save/source recovery. It is a fixture failure, not an AutoFishing product/GC failure.
- After reducing only the official-command maintenance check to 250 ms, `BATCH5-GC-LADDER/formal-autofishing-vitals250-final-20260719-2014` replayed L0-L5 from the beginning on the fifth save. All six 600-second stages completed with 21 samples, `ForcedGc=false`, all eleven metric classes available, official `Command_ComposeEnergy` / `Command_ComposeSpirit` provenance, zero native insufficient-energy deltas, exact save/source recovery and clean process exit. L4 independently verified disable/native recovery; L5 independently verified title reload/re-enable/loop/disable cleanup.
- `GAME-SMOKE/20260719-105158` remains a separate third-save semantic-transport receipt: `mark:下船点-左` resolved, transport reached `city_多洛可码头`, and native readback reported `fishingPool=true`. The later unselected-rod stop excludes it from the GC ladder; it is retained only as semantic lookup and actual transport evidence.
- These bounded passes close the Batch 5 release requirement for independent no-demand, ActionSpeed and AutoFishing evidence. They do not identify every historical Fatal-GC root and do not prove long-session native/Mono safety outside the tested routes. ISSUE-010 remains `open`.

## 2026-07-23 MoreSaves frame-demand source correction

- MoreSaves no longer keeps `GameLoop.UpdateTicked` subscribed throughout a
  healthy owner lifetime. A missing native manager demand-activates the
  subscription only while the owner remains active, and the first successful
  bounded retry removes it.
- Focused ProductNative Unit now proves that healthy configure and title
  reconciliation publish zero retry demand, active missing-manager work
  publishes then clears one demand, and failed final Loader cleanup retains
  pending-work evidence without claiming a surviving event scheduler.
- This is source/unit evidence for removing one avoidable per-frame event
  allocation source. No game run, GC ladder or long-duration test was run for
  this correction, and it does not close or reclassify the broader Unity/Mono
  crash investigation. ISSUE-010 remains `open`.

## 2026-07-28 retired GameBridge bucket scheduler truth

- The 2026-07-03 bucketed feature scheduler had no remaining production caller:
  the active frame path already used demand routing and feature-local updater
  membership. Its bucket enum, dispatch filter, option, runtime status,
  diagnostics, smoke gates and active Unit expectations are now removed.
- Existing `GameBridgeFeatureUpdateBuckets` configuration is recognized only
  as a migration input and rewritten without that key. It no longer enables a
  runtime mode or publishes a health result.
- Demand-routing diagnostics report active updaters, route dispatch totals and
  IDs. The separate direct-fanout diagnostic counts lifecycle, API and
  on-demand Hook distribution; it is not a second frame scheduler. This is
  source/unit truth cleanup, not new runtime or GC evidence. Historical
  2026-07-03 smoke receipts remain valid for the code tested then, but their
  scheduler fields do not describe the current model.
- No game, GC ladder, long-duration route or complete Release ran for this
  correction. ISSUE-010 remains `open`.

## 2026-07-28 0.5.5 current-candidate focused consistency gate

- The frozen `0.5.5` Runtime candidate passed the real-Unity no-demand route at
  `BATCH5-NO-DEMAND/prerelease-055-candidate-20260728-r3` /
  `GAME-SMOKE/20260728-051433`: after 300 warm-up frames, 10,000 measured
  frames retained no optional demand or optional updater and added zero
  optional Hook installs, directory enumerations, reflection searches,
  retained callbacks or native-updater invocations. The live Unity allocation
  counter remained unavailable/non-functional, so this proves optional-work
  call/cadence silence rather than whole-game zero allocation.
- The exact current ActionSpeed and AutoFishing product candidates passed
  `PRERELEASE-ACTIVE-GC/prerelease-055-candidate-20260728-r14`. ActionSpeed
  completed the selected L0, L1, four L3 workloads, L4 recovery and L5 title
  cycle; every stage retained `ResourceSnapshotBuilds=35 -> 35`.
  AutoFishing L1/L3/L4/L5 each completed the real five-warm-up/ten-measured
  LongRun contract. Across every AutoFishing stage, owner roots, input owners,
  event handlers, API roots, total demand and Runtime records were stable
  (`52`, `1`, `5`, `35`, `10`, `2` respectively).
- All twelve game runs were `NoNativeSave`, proved the player archive and
  committed sidecars unchanged before cleanup, performed no player archive
  writeback and exited. The enclosing lease restored the two original product
  trees, deployment journals and Author source-state exactly, left no
  run-created disabled marker, and preserved the pre-existing ActionSpeed 42 /
  AutoFishing 25 recovery-artifact ledgers.
- This was a short current-candidate focus with no forced GC, full historical
  ladder, long soak or complete Release. Process private/working-set values are
  diagnostic only and no quantified memory-risk budget is established. The
  result is consistent with the selected 0.5.5 wording that GC pressure was
  significantly reduced and crash probability lowered to some extent; it does
  not prove that every Unity/Mono growth path or crash is solved. ISSUE-010
  remains `open`.
- The first independent Step 6 review accepted those runtime observations but
  rejected two surrounding proof claims. The outer product/journal lease was
  same-process exception safe rather than process-interruption recoverable, and
  r14's four original AutoFishing `stage.json` files still recorded the earlier
  `MaximumMeasurementSeconds=600`.
- The existing lease file now records a pending phase before each original,
  candidate and restore move, binds the frozen ZIP hash plus exact entry
  ledger, and has an independent stale-lock `-RecoverOnly` path. Focused tests
  terminate separate Windows PowerShell processes at seven mutation boundaries
  and then recover the exact original product/journal and pre-existing
  recovery-artifact ledger. This is source/fixture evidence; the successful r14
  cleanup itself remains the runtime evidence.
- No game rerun was needed for the bound correction.
  `PRERELEASE-ACTIVE-GC/prerelease-055-candidate-20260728-r14/auto-fishing/final-validator-reevaluation.json`
  hashes the unchanged raw/stage inputs and replays the final target-driven
  validator read-only. It records original maximum `600`, current maximum
  `190`, and passing last samples of about `22/10/13/13` seconds for
  L1/L3/L4/L5. The receipt is interpretation-only, reports no
  Runtime/product/SDK mutation and does not rewrite the original stage
  receipts.

## 2026-07-31 frozen 0.5.5 all-functional one-hour route

- `GAME-SMOKE/20260731-013049` loaded the frozen `0.5.5` Runtime with nine
  exact current Advanced products, Workshop MoreEquipmentSlots `0.3.1`, and
  current OfficialLocal Manbo. All eleven code mods committed exactly once;
  the title remained open for 3,600 seconds, slot 3 then loaded once, stayed
  in-save for 15 seconds, returned to title and exited.
- The run passed `NoNativeSave`: current/prev/bak and committed equipment
  sidecars were unchanged before any cleanup, no routine backup/writeback was
  used, and no fatal window, attributable Unity crash or leftover process was
  found.
- An independent 30-second OS sampler recorded 120 samples with zero
  non-responding samples. The last 30 minutes changed by only `+0.016 MiB`
  working set and `+0.008 MiB` private bytes; least-squares slopes were
  `+0.000257 MiB/min` and `-0.000032 MiB/min`. Handles ended at `1,503`
  within `1,490-1,513`; threads ended at `128` within `127-136`.
- Runtime health at the final title boundary reported zero feature, demand,
  event-handler, cleanup and resource-ledger failures. One startup
  frame-driver stall/missing-frame warning recovered in about four seconds
  and did not recur during the hour.
- This is a bounded pass for the historically important continuous-title-idle
  then first-load route on the exact candidate. It does not establish a
  managed/native memory budget, cover long gameplay, or prove the wider
  Unity/Mono crash class solved. ISSUE-010 remains `open`.
- Detailed input, source, active-focus and restoration facts are recorded in
  `docs/reviews/code/2026/20260731-0001-dtmapi-055-upload-functional-pressure-audit.md`.

## 2026-07-31 obsolete coroutine-driver retirement

- Source review of the later 0.5.5 manual run confirmed that Bootstrap
  installed the PlayerLoop driver, then still unconditionally attempted the
  older reflected `StartCoroutine(IEnumerator)` loop. When Unity rejected that
  reflection path it produced a player-visible Error even though a successful
  coroutine would immediately stand down behind the active PlayerLoop.
- The unused coroutine startup, iterator and `"Coroutine"` frame source were
  removed. PlayerLoop, InputSystem, MonoBehaviour `Update`, native Gameplay
  frame drain and the 250 ms health/reinstall pump remain.
- Independent review also found that health-pump construction failure was
  still player-Error level and that a health check already posted to the Unity
  context could resubscribe InputSystem after quit cleanup. Pump construction
  failure now records a Warning, and an atomic shutdown guard closes Timer,
  posted-health, PlayerLoop-reinstall and InputSystem-resubscribe paths before
  driver cleanup.
- Release build plus Unit/QA source gates passed. No game was launched for
  this correction, so restart/load/title continuity and the reduced
  player-visible error count remain runtime evidence gaps. This change does
  not establish a GC fix; ISSUE-010 remains `open`.
- Exact active and retired UI hooks are recorded in
  `docs/hook-map/focused/NativeUiLayout.md`; lifecycle ownership and rollback
  remain with
  `docs/updates/2026/20260731-0002-autofishing-legacy-native-and-runtime-fallback-closeout.md`.

## 2026-07-31 relative frame health and Lite player diagnostics

- The later AutoFishing manual window contained two same-timestamp startup
  frame warnings even though PlayerLoop/native callbacks subsequently advanced
  with zero Update failures. Source review found the Timer queued only after all
  callbacks were stale, then attributed the old wall-clock timestamp to
  InputSystem alone after Unity resumed.
- Bootstrap now captures callback counts before posting the health check. A
  global unchanged gap keeps all subscriptions and emits no Warning; only
  sibling progress with an unchanged stale InputSystem count can request its
  recovery. Successful recovery is Info and only failed recovery is Warning.
- Ordinary Runtime object-graph evidence now defaults to Lite. Full remains an
  explicit QA mode. Lifecycle contract summaries no longer repeat for every
  Hook observation when status and diagnostics are unchanged.
- Unit coverage separates global pause, isolated stall, recovered input and
  missing-subscription retry. It also proves Lite does not hide an Error's
  owner/operation/request context, outer and inner exception or captured stack.
- Release build and complete Unit pass. No corrected binary has run in game yet,
  so this is source/unit pressure reduction and warning-classification evidence,
  not a new GC/crash acceptance. ISSUE-010 remains `open`.
