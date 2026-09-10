# Hook Map

Every DTMAPI hook must be registered here before it becomes a stable API event/helper.

Phase summary: [2026-07-06 Hook Map Phase Summary](phase-summary-20260706.md).

Use this template:

```md
## Hook: PublicApi.EventOrHelperName

- Status: proposed / experimental / verified / stable / disabled
- Public surface:
- Game build:
- Game method/type:
- Patch type: Postfix / Prefix / Finalizer / Transpiler / Unity callback / reflection
- Why this point:
- Failure behavior:
- Mods/tests depending on it:
- Evidence:
  - Build:
  - Save:
  - Log line:
  - Screenshot/report:
- Regression cases:
```

Default preference: Postfix or read-only reflection first, Prefix only when needed, Transpiler only with explicit review and regression evidence.

## Diagnostic: Runtime.GameBridgeShutdownCleanup

- Status: experimental / source-and-short-smoke-verified
- Public surface: none; internal shutdown cleanup and Hook status only.
- Game build: 23465763 workshop
- Game method/type: `BootstrapPlugin.OnApplicationQuit`, `DolocTownGameBridge`, `AppDomain.AssemblyLoad`, existing `DolocAPI.OnAfterLoadArchiveData` UnityEvent subscription, and static `DolocTownHookCallbacks`.
- Patch type: Unity lifecycle callback plus reflection-based removal of DTMAPI-owned UnityEvent listener. No new Harmony target, public API, manifest field, content-pack field, or gameplay behavior change.
- Why this point: the project lifecycle sweep found definite DTMAPI-owned roots that should not survive Unity quit: hook retry timer, AssemblyLoad callback, SaveLoaded UnityEvent listener, and static Hook callback references to runtime/bridge. Normal title/save boundaries should not unpatch Harmony hooks, but shutdown should release DTMAPI object roots.
- Failure behavior: if UnityEvent removal fails, DTMAPI records a diagnostics warning and clears its local listener reference so shutdown can proceed. If the bridge already released retry sources after core hook readiness, shutdown is idempotent. No native or unknown Unity objects are destroyed here.
- Mods/tests depending on it: DTMAPI diagnostics and smoke process-exit checks only; ordinary mods should not depend on this as public API.
- Evidence:
  - Build: 2026-07-04 `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK` and restricted-network `NU1900` warnings only; `git diff --check` passed with line-ending normalization warnings only.
  - Smoke: `PROJECT-OBJECT-LIFECYCLE-SWEEP-20260704` passed. Slot 3 title-button lifecycle `GAME-SMOKE/20260704-151818`, slot 3 two-cycle `GAME-SMOKE/20260704-151937`, slot 3 three-cycle `GAME-SMOKE/20260704-152059`, slot 7 Hatch AnimalVoice `GAME-SMOKE/20260704-152238`, slot 5 AutoFishing `GAME-SMOKE/20260704-152339`, slot 4 AnimalViewer `GAME-SMOKE/20260704-152523`, and slot 3 SaveSlots `GAME-SMOKE/20260704-152629` all passed process-exit and fatal-window checks.
  - Log line: expected `Hook status: GameBridge.ShutdownCleanup = verified` during Unity quit.
  - Screenshot/report: not applicable; evidence is log/result based.
- Regression cases: PROJECT-OBJECT-LIFECYCLE-SWEEP-20260704, ISSUE-010

## Diagnostic: Runtime.TitleReturnBoundaryLedger

- Status: experimental / source-and-short-smoke-verified
- Public surface: none; internal runtime ledger, Hook status, report context, and smoke result fields only.
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.ReturnHome`, `DolocAPI.LoadGame(int index) -> bool`, existing `DolocAPI.AfterLoadArchiveData(bool isNewGame)`, Core `ReturnedToTitleBoundary`, Bootstrap UI diagnostics, and GameBridge feature lifecycle summaries.
- Patch type: Harmony Prefix on `DolocAPI.ReturnHome` for diagnostic request observation, existing ReturnHome Postfix for native-return boundary observation, existing LoadGame Prefix/Postfix and SaveLoaded hooks for next-load closure; no public API, JSON field, content-pack path, or gameplay behavior change.
- Why this point: ISSUE-010 latest evidence narrowed the Fatal GC class to title-return or title-stable state followed by the next native `LoadGame`, before SaveLoaded and with duplicate LoadGame ruled out. The ledger connects ReturnHome request/postfix, Runtime ReturnedToTitle start/end, title-stable observation, next LoadGame enter/return, SaveLoaded, and fatal-window observation to object graph snapshots.
- Failure behavior: diagnostics are report-only. If the ReturnHome prefix is unavailable, the existing ReturnHome postfix, ReturnedToTitle runtime boundary, LoadGame, SaveLoaded, and fatal-window observations still report. If no SaveLoad request exists, `SaveLoadBoundary` reports `idle`; if an active native-enter request has not returned or reached SaveLoaded, it reports `loading`; fatal during an active/latest request reports `fatal`. The ledger never destroys or unloads native/unknown Unity objects.
- Mods/tests depending on it: DTMAPI diagnostics, HookProbe smoke, `run-game-smoke.ps1` fields `TitleReturnBoundaryLedger` and `TitleReturnBoundarySummary`, ISSUE-010 phase 8.5 evidence; ordinary mods should not depend on this as public API.
- Evidence:
  - Build: 2026-07-04 source validation passed: PowerShell parser check for `tools/scripts/run-game-smoke.ps1`, `tools/scripts/test.ps1 -Configuration Release` with `DTMAPI.UnitTests: OK` and restricted-network `NU1900` warnings only, and `git diff --check` with line-ending warnings only.
  - Save: slot 3 / index 2 evidence `GAME-SMOKE/20260704-142213` passed three load/return/load cycles with `TitleReturnBoundaryLedger=Passed`, `SaveLoadBoundary=Passed`, `DuplicateLoadRequests=Passed`, clean process exit, no fatal popup, `requests=3`, `nativeEnter=3`, `nativeReturn=3`, `saveLoaded=3`, and title ledger `events=34`, `objectSnapshots=14`, `fatalEvents=0`.
  - Save: slot 3 / index 2 evidence `GAME-SMOKE/20260704-142402` passed a 60-second title-stable wait between cycles with the same gates, `requests=2`, `nativeEnter=2`, `nativeReturn=2`, `saveLoaded=2`, and title ledger `events=24`, `objectSnapshots=10`, `fatalEvents=0`.
  - Object graph: short snapshots did not identify an uncleared DTMAPI-owned family after title return. SaveSlots, EquipmentSlots, AnimalViewer, CustomAnimals controller/bundle caches, AudioReplacement pending request/async/clip/callback state, AutoFishing native transients, and Bootstrap fallback EventSystem state were zero or bounded.
  - Log line: expected `Hook status: Refactor.TitleReturnBoundaryLedger = observing` and `TitleReturn boundary ledger operation=...`.
  - Screenshot/report: runtime report context includes `TitleReturnBoundaryLedger` and `TitleReturnObjectGraphSnapshots`.
- Regression cases: TITLE-RETURN-BOUNDARY-LEDGER-20260704, ISSUE-010

## Diagnostic: Runtime.NativeLoadContinuationProbe

- Status: experimental / smoke-only / issue-010-evidence-captured
- Public surface: none; smoke-only `-SmokeNativeLoadContinuationProbe None|VersionPatcher`, result fields, and diagnostic log lines only.
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.LoadGame(int)`, `DolocAPI.AfterLoadArchiveData(bool)`, `DolocTown.VersionPatcher.LoadAllVersionPatches()`, `DolocTown.VersionPatcher.LoadAllVersionPatchesBeyond(string)`, and `DolocTown.MapManager.Init(bool)`. `DolocTown.TextureUtils.DrawArea` is explicitly skipped as high-frequency.
- Patch type: Harmony Prefix/Postfix breadcrumbs installed only when the smoke setting is `VersionPatcher`.
- Why this point: ISSUE-010 Phase 8.15 shifted one fatal window to post-SaveLoaded and before native LoadGame return, with a stack around `AfterLoadArchiveData` / `VersionPatcher`. The probe adds minimal breadcrumbs to determine whether later samples reach that continuation path before Fatal GC.
- Failure behavior: if a target cannot be patched, the smoke hook status reports the missing target and the run remains diagnostic. Breadcrumbs record only method, phase, elapsed time, GC counts, memory, request id, boundary id, thread, and save-load counters. They do not enumerate objects, stringify dictionaries, run LINQ summaries, or destroy/unload Unity objects.
- Mods/tests depending on it: ISSUE-010 smoke diagnostics only; no ordinary mod should depend on this as public API.
- Evidence:
  - Build: 2026-07-06 PowerShell parser check passed, `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK` and restricted-network `NU1900` warnings only, and `git diff --check` passed with line-ending warnings only.
  - Save: slot 3 / index 2 evidence `GAME-SMOKE/20260706-150952` installed the probe hooks and then fataled before SaveLoaded; the last DTMAPI breadcrumb was `NativeContinuation.Step=DolocAPI.LoadGame.Enter ... nativeEnter=1 nativeReturn=0 saveLoaded=0`.
  - Log line: `Hook status: Smoke.NativeLoadContinuationProbe = active. SmokeNativeLoadContinuationProbe=VersionPatcher; AfterLoadArchiveDataHook=installed; VersionPatcherLoadAllHook=installed; VersionPatcherLoadBeyondHook=installed; MapManagerInitHook=installed; TextureUtilsDrawAreaHook=unsupported-skipped-high-frequency`.
  - Screenshot/report: not applicable; evidence is log/result/crash based.
- Regression cases: SAVELOAD-NATIVE-CONTINUATION-PROBE-20260706, ISSUE-010

## Diagnostic: Runtime.GameBridgeFeatureSchedulerHealth

- Status: experimental / source-and-runtime-smoke-verified / long-idle-gate-passed-once
- Public surface: none; internal scaffold flags, feature statuses, Hook statuses, report context, and smoke result fields only.
- Game build: 23465763 workshop
- Game method/type: `DolocTownGameBridge` internal feature fanout, `IGameBridgeFeature` implementations, runtime report context, and existing smoke/result diagnostics.
- Patch type: internal feature contract declarations, low-risk `Update` bucket scheduling, dispatch counters, and final health snapshot reporting; no new Harmony target, public helper member, public event args, manifest field, content-pack field, or JSON semantic change.
- Why this point: GameBridge feature execution had become a broad per-frame broadcast. Phase 7 makes each feature declare runtime requirements, update frequency, lifetime state, and failure behavior so future lifecycle/resource/hook work can reason about individual components instead of a single fanout blob.
- Failure behavior: when `GameBridgeFeatureContracts=true`, missing or conflicting declarations publish diagnostics only. When `GameBridgeFeatureUpdateBuckets=true`, only `Update` dispatch uses the internal buckets; lifecycle/API/Hook/EnvironmentReset fanout order remains unchanged. When `GameBridgeFinalHealthSnapshot=true`, save/title/export/shutdown boundaries write a summary of feature counts, bucket distribution, dispatch counts, owner/event/input counters, resource ledger, SaveLoad, and dispose graph. Disabling the flags restores legacy update broadcast or suppresses the new diagnostics.
- Mods/tests depending on it: DTMAPI diagnostics, smoke harness fields `GameBridgeFeatureContracts`, `GameBridgeFeatureScheduler`, `GameBridgeFeatureBuckets`, `GameBridgeFeatureDispatchCounts`, `GameBridgeFinalHealthSnapshot`, and `GameBridgeFinalHealthSummary`; ordinary mods should not depend on this as public API.
- Evidence:
  - Build: 2026-07-03 source validation added unit coverage for all existing feature contracts/buckets, flag defaults/config/env overrides, all-flags-off behavior, and final health summary aggregation; Release tests passed with `DTMAPI.UnitTests: OK`. PowerShell parser check passed and `git diff --check` passed with line-ending warnings only.
  - Save: slot 3 / index 2 evidence `GAME-SMOKE/20260703-224309` passed title-button lifecycle and all phase-seven GameBridge fields; slot 7 / index 6 evidence `GAME-SMOKE/20260703-224422` passed Hatch AnimalVoice and all phase-seven fields; slot 5 / index 4 evidence `GAME-SMOKE/20260703-224524` passed AutoFishing phase, AutoFishing soak, and all phase-seven fields.
  - UI bucket: initial slot 7 AnimalPanel evidence `GAME-SMOKE/20260703-224713` failed `AnimalViewerUi`, but the user clarified slot 7 is the custom-animal fixture and lacks the hidden-product row expected by this smoke. The same run passed `GameBridgeFeatureContracts`, `GameBridgeFeatureScheduler`, `GameBridgeFeatureBuckets`, `GameBridgeFeatureDispatchCounts`, `GameBridgeFinalHealthSnapshot`, process exit, and fatal-window checks; `Feature.AnimalViewer=ready`, `failedFeatures=0`, `cleanupFailures=0`, `needsRestart=0`, and `resourceErrors=0`. Correct slot 4 AnimalPanel evidence `GAME-SMOKE/20260703-231541` passed `AnimalViewerUi` and all phase-seven fields.
  - SaveSlots bucket and long-title gate: slot 3 evidence `GAME-SMOKE/20260703-231700` passed `MoreSavesOfficialSaveUi` and all phase-seven fields. Long-title evidence `GAME-SMOKE/20260703-231929` held title for 3600 seconds, loaded slot 3 with one `LoadGame requested for slot/index 2. requestId=SL-0001`, and passed `LongTitleIdleBeforeSave`, SaveLoad duplicate/boundary fields, lifecycle/resource/Hook fields, all phase-seven GameBridge fields, process exit, and fatal-window checks.
  - Log line: expected `Hook status: Refactor.GameBridgeFeatureContracts = ok`, `Hook status: Refactor.GameBridgeFeatureScheduler = bucketed-update`, `Hook status: Refactor.GameBridgeFeatureBuckets = ok`, `Hook status: Refactor.GameBridgeFeatureDispatchCounts = ok`, and `Hook status: Refactor.GameBridgeFinalHealthSnapshot = ok`.
  - Screenshot/report: runtime report context includes `GameBridgeFeatureContracts`, `GameBridgeFeatureScheduler`, `GameBridgeFeatureBuckets`, `GameBridgeFeatureDispatchCounts`, `GameBridgeFinalHealthSnapshot`, and `GameBridgeFinalHealthSnapshotAt`.
- Regression cases: GAMEBRIDGE-FEATURE-SCHEDULER-HEALTH-20260703, ISSUE-010

## Diagnostic: Runtime.InputFrameDrain

- Status: experimental / source-unit-runtime-and-player-manual-verified / external-key-sender-unverified
- Public surface: existing Experimental typed input DTOs/events only; the frame driver, latch, and native notification are internal.
- Game build: 23465763 workshop
- Game method/type: Input System `onAfterUpdate` edge capture, Unity `PlayerLoop.Update` title/non-normal fallback, and `DolocTown.NormalGameState.OnUpdate(float)` Gameplay Postfix.
- Patch type: one GameBridge-owned Harmony Postfix publishes an internal `NativeGameFrame`; Bootstrap owns the single input/Core/UI drain. No ordinary mod hotkey is injected into the game's ActionMap.
- Why this point: the Bootstrap MonoBehaviour/Coroutine and captured `SynchronizationContext` were observed to stop across native loading, while `NormalGameState.OnUpdate` is the stable native Gameplay responsibility function. PlayerLoop remains useful before/after normal gameplay but stands down while the native callback is current.
- Failure behavior: InputSystem edges are held by generation until Core consumes them exactly once. Only one source may latch registered controls in a Unity frame; a repeated backend press while the same key is still down and no release was observed is suppressed. A release+press sample remains a valid rapid retap. PlayerLoop directly samples as a fallback, and the 250ms timer performs subscription/driver health checks only; it does not poll Gameplay keys or dispatch ordinary Mod Update. Title scope continues to exclude AutoFishing Gameplay controls.
- Mods/tests depending on it: all typed DTMAPI hotkeys; first-party AutoFishing is the current F6/F7 consumer.
- Evidence:
  - Build/unit: 2026-07-10 Release build and `DTMAPI.UnitTests: OK`; unit coverage retains a complete press+release until one consumption and rejects a second consumption.
  - Save: `GAME-SMOKE/20260710-173812` installed `GameLoop.NativeFrameDrain`, observed 2394 Gameplay frames/11968 samples in 6.8 seconds, `nativeGameFrameCallbackSeen=True`, clean exit, and no fatal window. The external sender failed before delivering F6, so rapid-tap behavior remains manual pending.
  - Retained probes `172128`, `172756`, and `173218` document why InputSystem-only, install-once PlayerLoop, and in-load PlayerLoop reinstall were rejected as the sole Gameplay drain.
  - Post-fix `GAME-SMOKE/20260710-185323` is manual-assisted: player F6 produced one press/release, then the automated loop/cleanup/report gates passed. Final player retest confirms AutoFishing, individual settings, charge, and the requested manual behavior pass. This is not automated sender proof.
- Regression cases: INPUT-FRAME-GC-READY-20260710, ISSUE-010

## Diagnostic: Runtime.ModOwnerTransactionInputEventCleanup

- Status: experimental / source-and-short-smoke-verified
- Public surface: none; internal scaffold flags, feature statuses, Hook statuses, report context, and smoke result fields only.
- Game build: 23465763 workshop
- Game method/type: `DtmApiRuntime.LoadCodeMod`, internal `DtmHelper` construction, `InputService`, `EventManager`, `ModRegistryService`, `CustomEntityRegistryService`, and first-party `ConfigMenuRegistry`.
- Patch type: internal owner attribution, transaction ledger, rollback diagnostics, input owner map, event handler quarantine, and config preview audit; no new Harmony target, public helper member, public event args, manifest field, content-pack field, or JSON semantic change.
- Why this point: DTMAPI is becoming a platform, so registrations produced during a mod `Entry()` need owner and rollback boundaries. A failed, disabled, or partially loaded mod should not leave DTMAPI-owned input, event, API, config, or custom entity state in hot paths during long play sessions.
- Failure behavior: when `ModLoadTransaction=true`, code-mod Entry begins an internal transaction and commits only after Entry returns. If Entry throws, DTMAPI removes owner-bound event handlers, API registrations, input buttons, config pages, and custom entity definitions where DTMAPI owns the registration. Raw Harmony, static state, external native objects, and unknown third-party side effects are reported as `NeedsRestart`. High-frequency event handlers that hit the existing failure threshold are quarantined outside active dispatch when `EventHandlerQuarantine=true`. Successful config preview apply/restore observations aggregate by stable owner/item/kind/operation instead of appending permanent ledger rows; recent failure details are capped at 64 and the general owner ledger at 2048.
- Mods/tests depending on it: DTMAPI diagnostics, smoke harness fields `ModOwnerLifecycle`, `ModOwnerLifecycleSummary`, `ModLoadTransaction`, `OwnerBoundInput`, `EventHandlerCleanup`, `ConfigPreviewAudit`, and `FailedModRollback`; ordinary mods should not depend on this as public API.
- Evidence:
  - Build: 2026-07-03 source validation added unit coverage for flag default/config/env overrides, owner-bound input multi-owner cleanup, failed partial Entry rollback, event quarantine removal from active dispatch, config preview audit, and all-flags-off behavior; Release tests passed with `DTMAPI.UnitTests: OK`. PowerShell parser check passed and `git diff --check` passed with line-ending warnings only.
  - Save: slot 3 / index 2 evidence `GAME-SMOKE/20260703-220819` passed title-button lifecycle and all phase-six fields; slot 7 / index 6 evidence `GAME-SMOKE/20260703-220920` passed Hatch AnimalVoice and all phase-six fields; slot 5 / index 4 evidence `GAME-SMOKE/20260703-221013` passed AutoFishing phase, AutoFishing soak, and all phase-six fields.
  - Log line: expected `Hook status: Refactor.ModOwnerLifecycle = ok`, `Hook status: Refactor.ModLoadTransaction = closed`, `Hook status: Refactor.OwnerBoundInput = ok`, `Hook status: Refactor.EventHandlerCleanup = ok`, `Hook status: Refactor.ConfigPreviewAudit = observing`, and `Hook status: Refactor.FailedModRollback = idle|rolled-back`.
  - Screenshot/report: runtime report context includes `ModOwnerLifecycle`, `ModOwnerLifecycleSummary`, `ModLoadTransaction`, `OwnerBoundInput`, `EventHandlerCleanup`, `ConfigPreviewAudit`, and `FailedModRollback`.
- Regression cases: MOD-OWNER-TRANSACTION-INPUT-EVENT-CLEANUP-20260703, ISSUE-010

## Diagnostic: Runtime.HookEventMainThreadScheduler

- Status: experimental / source-and-short-smoke-verified
- Public surface: none; internal scaffold flags, feature statuses, Hook statuses, report context, and smoke result fields only.
- Game build: 23465763 workshop
- Game method/type: `DolocTownGameBridge.Initialize`, `AppDomain.AssemblyLoad`, hook retry `Timer`, `DolocTownGameBridge.Update`, `DtmApiRuntime.SetHookStatus`, and internal `EventManager` dispatch.
- Patch type: internal scheduling/diagnostics around existing Harmony patch installation and event dispatch; no new Harmony target, public event args, public helper, manifest field, content-pack field, or JSON semantic change.
- Why this point: Hook installation and diagnostics can be triggered by startup, assembly-load callbacks, retry timers, and hook callbacks. Stage 4 makes these sources register intent, then lets the runtime thread perform patching, publish Hook status events/logs, and flush safe event queues at phase boundaries.
- Failure behavior: when `HookInstallScheduler=true`, external hook triggers call `RequestHookInstall` and duplicate keys are coalesced. Actual `InstallHarmonyHooks()` runs from the runtime-thread update path. Core Hook readiness controls AssemblyLoad/timer release; feature Hook pending remains diagnostic-only. `SetHookStatus` updates diagnostics snapshots immediately and queues `HookStatusChanged` plus `Hook status:` logs until runtime flush. Safe off-thread lifecycle/save/workshop/log/HookStatus events queue; off-thread Update/Input events are rejected and diagnosed. Disabling the related scaffold flags restores legacy synchronous behavior or disables the new diagnostic layer.
- Mods/tests depending on it: DTMAPI diagnostics, smoke harness fields `HookScheduler`, `CoreHookReadiness`, `FeatureHookReadiness`, `HookStatusQueue`, `OffThreadHookRequests`, `AssemblyLoadSubscription`, and `RetryTimerAlive`; no ordinary mod should depend on this as public API.
- Evidence:
  - Build: 2026-07-02 source validation added unit coverage for scaffold flag defaults/overrides, Hook status queue flush semantics, off-thread event boundary queue/reject behavior, and scheduler request coalescing; Release tests passed with `DTMAPI.UnitTests: OK`.
  - Save: slot 3 / index 2 evidence `GAME-SMOKE/20260702-204727` passed title-button lifecycle, HookProbe, and all stage-four fields; slot 7 / index 6 evidence `GAME-SMOKE/20260702-204851` passed Hatch AnimalVoice and all stage-four fields.
  - Log line: expected `Hook status: Refactor.CoreHookReadiness = ready`, `Hook status: Refactor.FeatureHookReadiness = ready|partial`, `Hook status: Refactor.AssemblyLoadSubscription = released`, and `Hook status: Refactor.RetryTimerAlive = released`.
  - Long-idle: `GAME-SMOKE/20260702-210050` still reproduced `Fatal error in GC` after 3600 seconds when loading slot 3, but the Hook scheduler/readiness/status fields stayed passed and did not show title-idle growth; ISSUE-010 remains open.
  - Screenshot/report: not applicable; evidence is log/result based.
- Regression cases: HOOK-EVENT-MAIN-THREAD-SCHEDULER-20260702, ISSUE-010

## Diagnostic: Runtime.SaveLoadRequestCoordinator

- Status: experimental / runtime-smoke-verified
- Public surface: none; internal scaffold flag, diagnostics feature/hook statuses, report context, and smoke `result.json` fields only.
- Game build: 23465763 workshop; signature rechecked against local reverse metadata before implementation.
- Game method/type: `DolocAPI.LoadGame(int index) -> bool`, fallback `DolocTown.GameData.DataPersistenceManager.LoadGame(int index) -> bool`, and existing `DolocAPI.AfterLoadArchiveData(bool isNewGame)` / `Save.SaveLoaded` boundary.
- Patch type: Harmony Prefix records native `LoadGame` enter; Harmony Postfix records native return; existing SaveLoaded callback closes the request. Smoke direct-load fallback asks the runtime coordinator before invoking native `LoadGame`.
- Why this point: stage-four long-title evidence `GAME-SMOKE/20260702-210050` reproduced Fatal GC after a 3600-second title idle and before SaveLoaded, with four `LoadGame requested for slot/index 2` lines. The coordinator makes this transition attributable by request id and suppresses only duplicate DTMAPI/smoke-originated fallback loads for the same active slot.
- Failure behavior: if disabled through `SaveLoadRequestCoordinator=false`, legacy `currentLoadingSlot` behavior remains. If native load enters but SaveLoaded does not arrive, diagnostics retain the active request and last phase. DTMAPI/smoke duplicates are suppressed and counted as suppressed duplicates; native player UI clicks are never intercepted and any native duplicates remain diagnostic-only.
- Mods/tests depending on it: DTMAPI diagnostics, HookProbe smoke, `run-game-smoke.ps1` fields `SaveLoadRequestCoordinator`, `SaveLoadRequestSummary`, `DuplicateLoadRequests`, and `SaveLoadBoundary`; ordinary mods should not depend on it as public API.
- Evidence:
  - Build: 2026-07-03 Release tests passed with `DTMAPI.UnitTests: OK`; parser check for `run-game-smoke.ps1` passed; `git diff --check` passed with line-ending warnings only.
  - Save: slot 3 / index 2 evidence `GAME-SMOKE/20260703-201031` passed title-button lifecycle with `requests=1`, `nativeEnter=1`, `nativeReturn=1`, `saveLoaded=1`, and `duplicateRequests=0`; slot 7 / index 6 evidence `GAME-SMOKE/20260703-201129` passed Hatch AnimalVoice with the same one-request closure.
  - Long-idle: `GAME-SMOKE/20260703-201302` held title for 3600 seconds, loaded slot 3, exited cleanly, and passed `LongTitleIdleBeforeSave`, `SaveLoadRequestCoordinator`, `DuplicateLoadRequests`, `SaveLoadBoundary`, `ProcessExited`, and `NoFatalInstanceWindow`. Logs show one `LoadGame requested for slot/index 2. requestId=SL-0001`, SaveLoaded closed `SL-0001`, and delayed native return attached to `SL-0001`.
  - Retained intermediate finding: earlier same-day short smokes passed but showed native return as a separate request because `LoadGame` returns after SaveLoaded; the final implementation attaches delayed return to the completed same-slot request.
- Regression cases: SAVELOAD-REQUEST-COORDINATOR-20260703, ISSUE-010

## Diagnostic: Smoke.DiagnosticsReportExport

- Status: verified
- Public surface: none; `run-game-smoke.ps1` result-field evidence only.
- Game build: 23465763 workshop
- Game method/type: smoke harness log/result validation around `Smoke.DiagnosticsSnapshot = verified` entries produced by `IDtmDiagnosticsApi.GetSnapshot` after `runtime.ExportLogs()`.
- Patch type: smoke script result aggregation; no Harmony hook, public API, hook/status ID, or gameplay behavior change.
- Why this point: report export proof should be comparable across Camera, ActionSpeed, AutoFishing, and future diagnostics snapshot smokes instead of being locked to a per-feature result field.
- Failure behavior: if any requested diagnostics-export scenario does not log `Smoke.DiagnosticsSnapshot = verified`, the new `DiagnosticsReportExport` result field is `Failed` and the overall smoke run fails; existing behavior fields and `AutoFishingReportExport` stay separate for compatibility.
- Mods/tests depending on it: Camera/ActionSpeed/AutoFishing smoke harness routes and final web audit package evidence selection.
- Evidence:
  - Build: 2026-06-11 `git diff --check`, PowerShell script syntax parsing, Release build, and Release unit tests passed.
  - Save: local slot 3 / index 2.
  - Log line: final `Refactor` evidence `GAME-SMOKE/20260611-031502` logs `Smoke.DiagnosticsSnapshot = verified. scenario=Camera` and result `DiagnosticsReportExport=Passed`; `GAME-SMOKE/20260611-031721` logs `scenario=ActionSpeed` and result `DiagnosticsReportExport=Passed`; `GAME-SMOKE/20260611-031838` logs `scenario=AutoFishing AutoFishingMiniGameComplete report export` with result `DiagnosticsReportExport=Passed` and compatibility `AutoFishingReportExport=Passed`.
  - Screenshot/report: smoke evidence under `docs/debug/evidence/GAME-SMOKE/20260611-031502`, `20260611-031721`, and `20260611-031838`; report zips `dtmapi-report-20260611-031644.zip`, `dtmapi-report-20260611-031801.zip`, and `dtmapi-report-20260611-031919.zip`.
- Regression cases: DIAGNOSTICS-REPORT-EXPORT-FIELD-20260611

## Diagnostic: Runtime.LifecycleRetentionCounters

- Status: experimental / pending long-run validation
- Public surface: none; internal hook-status diagnostics only.
- Game build: 23465763 workshop
- Game method/type: `DolocTownGameBridge.NotifyGameBridgeFeaturesEnvironmentReset`, with summaries from SaveSlots, EquipmentSlots, AutoFishing, ActionSpeed, and machine runtime state.
- Patch type: internal status publication after existing `DolocAPI.SetEnvCamera -> EnvironmentReset` fanout; no new Harmony target and no public API change.
- Why this point: long-run Unity/Mono GC crash packages need low-frequency counts for DTMAPI-owned lifecycle roots so future logs can distinguish bounded cleanup from growing retained native/UI references.
- Failure behavior: environment-reset pre-steps are isolated from each other. If EquipmentSlots refresh, runtime automation refresh, feature fanout, or counter publication throws, DTMAPI records `Runtime.EnvironmentResetFanout=degraded` for that step and continues the remaining steps. Repeated failures are throttled with first/short/interval summary publication to avoid per-reset log growth. Counter publication is status-only and runs on the first three environment resets, then by time interval.
- Mods/tests depending on it: DTMAPI diagnostics only; ISSUE-010 long-run GC crash triage.
- Evidence:
  - Build: 2026-06-20 source-only validation passed for the review follow-up: `git diff --check`, Release unit tests, Windows PowerShell 5.1 parse of `tools/scripts/collect-logs.ps1`, and a no-game offline collector run that still wrote `Unity-Crashes/summary.txt`. Second safety pass fixed per-dispatch feature fanout allocation, EquipmentSlots stale inactive UI rediscovery, AutoFishing synchronous confirmation overwrite, and too-large `crash.dmp` README export.
  - Save: no game save; game smoke intentionally not run for this source-only update.
  - Log line: expected future line `Runtime.LifecycleRetentionCounters = observed` with `environmentResetCount`, `featureFanout`, `saveUiStates`, `equipmentClones`, `fishingPendingCast`, `fishingAutoCastBackoff`, `actionAnimators`, and `machineRuntimeEntries`; degraded pre-step failures appear as `Runtime.EnvironmentResetFanout = degraded`.
  - Screenshot/report: no new game report yet; future player/local long-run packages should include this hook status in exported diagnostics.
- Regression cases: RUNTIME-LIFECYCLE-CLEANUP-WATCHDOG-20260620

## Diagnostic: Runtime.EnvironmentResetFanout

- Status: experimental / source-verified
- Public surface: none; internal hook-status diagnostics only.
- Game build: 23465763 workshop
- Game method/type: `DolocTownGameBridge.NotifyGameBridgeFeaturesEnvironmentReset` pre-step isolation around EquipmentSlots refresh, runtime automation refresh, feature fanout, and lifecycle counter publication.
- Patch type: internal callback isolation around existing `DolocAPI.SetEnvCamera -> EnvironmentReset` fanout; no new Harmony target and no public API change.
- Why this point: `EnvironmentReset` can be triggered by high-frequency camera/environment refreshes, so one DTMAPI cleanup or diagnostics failure must not prevent other GameBridge features from receiving their lifecycle notification.
- Failure behavior: first failure per operation records diagnostics under `DTMAPI.GameBridge.EnvironmentReset`; first/short/interval summary failures publish `Runtime.EnvironmentResetFanout=degraded` with the operation and exception summary, then the remaining steps continue. Suppressed repeated failures keep aggregate diagnostics without per-reset log/status spam.
- Mods/tests depending on it: DTMAPI diagnostics only; ISSUE-010 long-run GC crash triage.
- Evidence:
  - Build: 2026-06-20 source-only unit coverage `GameBridgeEnvironmentResetStepFailuresAreIsolated`; high-frequency feature dispatch uses an allocation-free index loop and still isolates each feature callback.
  - Save: no game save; game smoke intentionally not run for this source-only update.
  - Log line: expected future degraded line `Runtime.EnvironmentResetFanout = degraded`.
  - Screenshot/report: no game report yet.
- Regression cases: RUNTIME-LIFECYCLE-CLEANUP-WATCHDOG-20260620

## Hook: Audio.SoundEventReplacement

- Status: experimental
- Public surface: `IAudioReplacementApi` for the reviewed code-mod paper-box slice; internal ContentPack JSON for `AnimalVoice`.
- Game build: 23465763 workshop
- Game method/type: exact `DolocTown.WwiseSoundManager.InternalPostSoundEvent(string, UnityEngine.GameObject, EventCallback, bool) -> bool`; for animal voice scoping, exact `DolocTown.Animal.PlayAnimalSound()` prefix/postfix context.
- Patch type: Harmony Prefix on Wwise event post; Harmony Prefix/Postfix on `Animal.PlayAnimalSound` for context capture/cleanup.
- Why this point: wild paper boxes keep native gameplay ownership in `DungeonResourceModelPaperBox.OnInteract`, which posts `SoundEvents.PLAY_RESOURCE_PAPER_BOX`; the shared Wwise internal event owner is the narrowest reviewed point that can replace the sound without copying paper-box drop/removal/save logic. Hatch reuses the vanilla chicken animal template, so a global event-name replacement would pollute native chickens; `Animal.PlayAnimalSound()` supplies the native animal owner context before the shared Wwise event is intercepted.
- Failure behavior: native audio is allowed if the target event has no registered replacement, the event is not in the reviewed allowlist, the local WAV is missing/pending/failed, the native event has unsupported emitter/callback semantics, local playback cannot be verified, playback throws, or the feature/hook is unavailable. `AnimalVoice` additionally requires fresh matching animal context (`speciesId + stage + nativeSoundEvent`); missing, stale, mismatched, or vanilla-chicken context fails open. If a replacement is ready and `suppressNativeWhenReady=true`, native audio is suppressed after DTMAPI local WAV playback starts successfully, and also when replacement playback is skipped only because the same ready replacement is inside its cooldown window. Cooldown is therefore a duplicate replacement playback guard, not a native-audio fail-open path. The current verified backend tries Unity local WAV decode first and falls back to `System.Media.SoundPlayer` when Unity returns zero-metadata clips. Duplicate suppressing replacements for the same reviewed scope are rejected until a broader arbitration policy exists.
- Mods/tests depending on it: developer test mod `Yuuka.DTMAPI.ManboCardboardAudio`; Hatch content pack `DTMAPI_HatchAssets`; `run-game-smoke.ps1 -SaveSlot 10 -AutoExerciseAudioReplacement`; `run-game-smoke.ps1 -SaveSlot 7 -AutoExerciseHatchAnimalVoice`.
- Evidence:
  - Build: Release build/test and `git diff --check` passed after adding the exact Wwise signature, reviewed-event allowlist, duplicate suppressing registration rejection, local WAV load cascade, platform fallback, and fail-open playback validation. 2026-07-01 Release tests pass with `DTMAPI.UnitTests: OK` for content-pack `AnimalVoice` parsing/scope tests, ready-cooldown native suppression, and paper-box regression coverage; `git diff --check` passes with CRLF warnings only.
  - Save: local slot 10 / index 9 fixture required for the paper-box smoke; local slot 7 / index 6 required for Hatch AnimalVoice smoke.
  - Log line: verified in `GAME-SMOKE/20260617-065447`: `AudioReplacement paper-box OnInteract owner=DungeonResourceModelPaperBox event=PLAY_RESOURCE_PAPER_BOX` and `AudioReplacement event owner=Yuuka.DTMAPI.ManboCardboardAudio replacement=manbo-paper-box event=PLAY_RESOURCE_PAPER_BOX played=True suppressed=True`. Hatch verified in `GAME-SMOKE/20260701-073005`: `AudioReplacement event owner=DTMAPI.HatchAssets replacement=hatch-pet-child event=PLAY_ANIMAL_PET_CHICKEN_CHILD played=True suppressed=True`, `replacement=hatch-pet-adult event=PLAY_ANIMAL_PET_CHICKEN played=True suppressed=True`, and `Smoke exercise HatchAnimalVoice OK ... vanillaChickenCheck=not-applicable`. Cooldown semantics revalidated in `GAME-SMOKE/20260701-234850`: Hatch child/adult replacement events stayed `played=True suppressed=True`, the non-stacked smoke had no `replacement cooldown active; native sound allowed` lines, and unit coverage verifies ready suppressing cooldown uses `played=False suppressed=True`. User manual QA on 2026-07-01 confirms adult/child Hatch pet and hit paths pass and vanilla chicken is not polluted.
  - Screenshot/report: earlier failures `GAME-SMOKE/20260617-052549`, `20260617-053452`, `20260617-060219`, and `20260617-061723` document Unity clip failure modes; `GAME-SMOKE/20260617-062938` passed a pre-hardening synthetic-event smoke; `GAME-SMOKE/20260617-065447` proves post-key native paper-box interaction plus replacement playback and exits cleanly with no `DolocTown.exe`. Hatch retained failed run `GAME-SMOKE/20260701-071336` documents the fixed smoke force-refresh loop; `GAME-SMOKE/20260701-073005` passed slot 7 Hatch child/adult replacement and clean process/fatal checks; `GAME-SMOKE/20260701-234850` passed `HatchAnimalVoice`, `HookProbe`, `ProcessExited`, `NoFatalInstanceWindow`, and `ForcedClose` after the cooldown suppression change.
- Regression cases: AUDIO-REPLACEMENT-CARDBOARD-20260617, HATCH-ANIMALVOICE-AUDIO-20260701, ANIMALVOICE-COOLDOWN-SUPPRESSION-20260701

## Diagnostic: HookCallbackSafeFallbacks

- Status: verified
- Public surface: none; internal GameBridge/Harmony callback safety policy.
- Game build: 23465763 workshop
- Game method/type: non-lifecycle Harmony callback paths owned by `DolocTownHookCallbacks`, including ChestLocator, FishRoe, ActionSpeed enter/continuous-use, ActionCompletion/Oil tool hit, Fishing phase/minigame, Motor, Equipment, StrongPlantingGun, input isolation, creative/debug, and animal viewer callbacks.
- Patch type: internal callback wrapper around existing Prefix/Postfix bodies; no hook target, hook ID, public API, or smoke schema change.
- Why this point: ordinary hook callbacks should fail toward native behavior instead of leaking exceptions through Harmony into Doloc Town control flow. Lifecycle cleanup/restore ordering is tracked separately under `LIFECYCLE-CALLBACK-ISOLATION-20260610`.
- Failure behavior: `SafeResult<T>` returns the original result/fallback on failure, `SafePrefix` returns `true` by default so native logic continues, and `SafePostfix` records diagnostics without throwing back into native code. Failures are recorded under `DTMAPI.GameBridge.HookCallback` with runtime-monitor log details.
- Mods/tests depending on it: `DTMAPI.UnitTests`, `DTMAPI.ChestLocatorEnhancerMod`, `Yuuka.DTMAPI.FishBreedingAssistant`, `Yuuka.DTMAPI.ActionSpeed`, `Yuuka.DTMAPI.OneActionComplete`, `Yuuka.DTMAPI.AutoFishing`, plus the affected feature smoke harnesses.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors; unit helper coverage verified fallback values and diagnostics recording.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260610-035216`, `GAME-SMOKE/20260610-035326`, `GAME-SMOKE/20260610-035434`, `GAME-SMOKE/20260610-035639`, and `GAME-SMOKE/20260610-035752` all record their focused smoke cases as passed with clean process/fatal checks after non-lifecycle callbacks were wrapped.
  - Screenshot/report: smoke evidence under `docs/debug/evidence/GAME-SMOKE/20260610-035216`, `20260610-035326`, `20260610-035434`, `20260610-035639`, and `20260610-035752`; latest runtime report generated during the branch was `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-035514.zip`.
- Regression cases: HOOK-CALLBACK-SAFE-FALLBACKS-20260610

## Diagnostic: HookCallbackFailureThrottle

- Status: verified
- Public surface: none; internal GameBridge/Harmony callback failure diagnostics policy.
- Game build: 23465763 workshop
- Game method/type: `DolocTownHookCallbacks.RecordHookCallbackFailure` for non-lifecycle callback failures recorded by `SafeResult<T>`, `SafePrefix`, `SafePostfix`, and direct guarded callback bodies.
- Patch type: internal diagnostics throttling around existing callback wrappers; no hook target, hook ID, public API, or smoke schema change.
- Why this point: a repeated per-frame hook failure should not flood diagnostics and runtime logs after the first actionable error, but the callback must still fail toward native behavior.
- Failure behavior: first failure per operation records a full `DTMAPI.GameBridge.HookCallback` diagnostics error and error log; the next two repeats write short warning logs; later repeats are suppressed until a 30-second summary window. Prefix callbacks still return the native-pass fallback, result callbacks still return fallback/original values, and void callbacks still do not throw into Harmony/native code.
- Mods/tests depending on it: `DTMAPI.UnitTests`, plus ChestLocator, FishRoe, ActionSpeed, OneAction, and AutoFishing smoke paths that use the affected callback wrappers.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors; unit coverage verifies fallback values and one diagnostics error per repeated operation.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260610-092620`, `GAME-SMOKE/20260610-092819`, `GAME-SMOKE/20260610-092928`, `GAME-SMOKE/20260610-093037`, and `GAME-SMOKE/20260610-093148` all passed their focused cases with clean process/fatal checks and no hook-callback failure entries.
  - Screenshot/report: smoke evidence under `docs/debug/evidence/GAME-SMOKE/20260610-092620`, `20260610-092819`, `20260610-092928`, `20260610-093037`, and `20260610-093148`.
- Regression cases: HOOK-CALLBACK-FAILURE-THROTTLE-20260610

## Diagnostic: ToolColliderCallbackIsolation

- Status: verified
- Public surface: none; internal GameBridge/Harmony callback safety policy for the shared `ToolCollider.HandleTools` postfix route.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.ToolCollider.HandleTools` shared Prefix/Postfix installed by `ToolColliderHitHookBridge`; callback routing lives in `DolocTownHookCallbacks.ToolColliderHandleToolsPostfix`.
- Patch type: internal callback wrapper split around the existing shared Postfix route; no hook target, hook ID, public API, or smoke schema change.
- Why this point: ActionCompletion and OilCoalDrop share the same native `ToolCollider.HandleTools` route, but ActionCompletion failure should not prevent OilCoalDrop from applying or clearing captured coal-resource state.
- Failure behavior: `SafeResult("ToolCollider.HandleTools.ActionCompletion", false, ...)` falls back to the native-pass/Oil route on ActionCompletion exceptions; `SafePostfix("ToolCollider.HandleTools.OilCoalDrop.ApplyAfterHit", ...)` and `SafePostfix("ToolCollider.HandleTools.OilCoalDrop.ClearCaptured", ...)` record independent diagnostics failures without throwing into Harmony/native code.
- Mods/tests depending on it: `DTMAPI.UnitTests`, `DTMAPI.OneActionCompleteMod`, `DTMAPI.OilMod`, and NewContent/Oil smoke paths.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors; unit coverage verifies ActionCompletion failure does not block OilCoalDrop apply and that ActionCompletion/OilCoalDrop apply/clear failures use distinct diagnostics keys.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260610-163813` records `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`; `GAME-SMOKE/20260610-163928` records `NewContentOilItemMetadata=Passed`, `NewContentOilCoalDrop=Passed`, `NewContentEquipmentSlots=Passed`, `NewContentMineOfficialJson=Passed`, `NewContentMineProduction=Passed`, `NewContentApis=Passed`, and clean process/fatal checks.
  - Screenshot/report: smoke evidence under `docs/debug/evidence/GAME-SMOKE/20260610-163813` and `docs/debug/evidence/GAME-SMOKE/20260610-163928`; these smoke modes did not export fresh report zips, and their stale `latest-report.txt` files are not cited as report evidence.
- Regression cases: TOOLCOLLIDER-CALLBACK-ISOLATION-20260610

## Diagnostic: GameBridgeFeatureFailureThrottle

- Status: verified
- Public surface: none; internal GameBridge feature-host dispatch diagnostics policy.
- Game build: 23465763 workshop
- Game method/type: `DolocTownGameBridge.DispatchGameBridgeFeature(...)` catch path for feature-host operations such as `RegisterApis`, `PublishHookStatuses`, `InstallHooks`, `Update`, `SaveLoaded`, `ReturnedToTitle`, and environment reset.
- Patch type: internal diagnostics throttling around existing feature-host dispatch; no hook target, hook ID, public API, feature service behavior, or smoke schema change.
- Why this point: repeated high-frequency feature-host failures, especially `Update()`, should not flood diagnostics and runtime logs after the first actionable error, while structured `Feature.<Id>` state still needs current failure counts and latest error text.
- Failure behavior: first failure per `featureId + operation` records a full diagnostics error and error log; the next two repeats write short warning logs; later repeats are suppressed until a 30-second summary window. Internal `GameBridgeFeatureStatus` and diagnostics feature snapshots still increment cumulative `FailureCount` and update `LastError` on every failure, while `Feature.<Id>` hook-status publication follows the throttled publication decision instead of rewriting on every failure-count change. Feature status details also include `consecutiveFailureCount` and `lastRecoveredAt`; after three successful dispatches for the same feature operation, the failure episode is cleared so a later failure records a fresh diagnostics error.
- Mods/tests depending on it: `DTMAPI.UnitTests`, diagnostics snapshot/status UI, and all feature-hosted GameBridge features.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors; unit coverage verifies one diagnostics error for six repeated `UnitFeature/Update` failures while failure count reaches 6.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260610-113540`, `GAME-SMOKE/20260610-113752`, `GAME-SMOKE/20260610-113902`, and `GAME-SMOKE/20260610-114008` all record focused feature-host smokes as passed with clean process/fatal checks and no GameBridge feature failure entries.
  - Hook-status publication split: 2026-06-10 unit coverage verifies the diagnostics feature snapshot reaches `FailureCount=6` and latest error text while `Feature.UnitFeature` hook status stops at the third allowed failure publication; focused smokes `GAME-SMOKE/20260610-133933`, `20260610-134145`, `20260610-134259`, and `20260610-134406` pass Camera, ActionSpeed, SaveSlots/HookProbe, and AnimalViewer with no GameBridge feature failure entries.
  - Recovery episode policy: 2026-06-10 unit coverage verifies three stable successes after a repeated `UnitFeature/Update` failure episode reset `consecutiveFailureCount` to 0, preserve cumulative `FailureCount=6`, record `lastRecoveredAt`, and allow a later post-recovery failure to record a second diagnostics error with `consecutiveFailureCount=1`. Camera smoke `GAME-SMOKE/20260610-164830` and ActionSpeed smoke `GAME-SMOKE/20260610-165043` verify the existing ready/smoke routes still pass with fresh report zips `dtmapi-report-20260610-165010.zip` and `dtmapi-report-20260610-165123.zip`.
  - Screenshot/report: smoke evidence under `docs/debug/evidence/GAME-SMOKE/20260610-113540`, `20260610-113752`, `20260610-113902`, and `20260610-114008`; report zips `dtmapi-report-20260610-113722.zip`, `dtmapi-report-20260610-113832.zip`, `dtmapi-report-20260610-113939.zip`, and `dtmapi-report-20260610-114044.zip`.
- Regression cases: FEATURE-HOST-FAILURE-THROTTLE-20260610, FEATURE-FAILURE-STATUS-THROTTLE-20260610, FEATURE-FAILURE-RECOVERY-POLICY-20260610

## Diagnostic: UI.NativeLayoutDiagnostics

- Status: experimental/diagnostic
- Public surface: none; internal GameBridge hook status and runtime log diagnostics only.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.HomePageUiState.RenderTextMenu`, concrete `DolocTown.UI.MenuUI.SetCapacity`, `DolocTown.UI.MainMenuPanel.OnStartShow`, `DolocTown.UI.GameDataPanel.SetCapacity`, best-effort concrete `DolocTown.UI.HomePageTextMenu.ResetLayoutSize` / `DolocTown.UI.MenuUI.ResetLayoutSize`, and attempted generic `DolocTown.UI.DolocGridUI<T>.ResetLayoutSize/SetCapacity`.
- Patch type: Harmony Postfix diagnostics only. `HomePageUiState.RenderTextMenu`, `MenuUI.SetCapacity`, `MainMenuPanel.OnStartShow`, and `GameDataPanel.SetCapacity` record owner-event evidence; active `HomePageUiState.Update` and `MainMenuUiState.Update` sample only and do not repair. The broad 500 ms polling repair, later Update repair loop, and global `GridLayoutGroup.constraintCount` setter normalization were removed after root-cause proof showed SaveSlots was misapplying `ResetLayoutSize(2)` through an inherited Select hook.
- Why this point: manual QA showed the title homepage menu and in-save pause menu could switch to a two-column/vertical layout after DTMAPI install. Stack diagnostics in `GAME-SMOKE/20260613-142631` proved the delayed homepage write came from `SaveSlotsService.TryResetLayoutSize -> RestoreOfficialSavePanel -> EnsureOfficialSavePanelPageForSelection -> GameDataPanelSelectPrefix`, where the inherited `DolocGridUI<T>.Select` hook reached `HomePageTextMenu`.
- Failure behavior: native UI continues unchanged if diagnostics hooks are unavailable. When installed, the service logs source, target type, current UI state, DTMAPI input context, requested total/line counts, slot counts, visible slot counts, layout group constraint/count, and a truncated stack for interesting title/pause targets or observed two-column values. Update sampling logs `twoColumnObserved=True` without trying to repair it.
- Mods/tests depending on it: DTMAPI support/manual QA only.
- Evidence:
  - Build: 2026-06-13 `20260613-0001` package/icon build-test-install passed. Follow-up `20260613-0002` `git diff --check`, Release build/test, package staging/install, and status check passed; installed GameBridge DLL length is `970752`. Follow-up `20260613-0003` `git diff --check`, Release build/test, package staging/install, and status check passed after removing the polling repair and adding concrete writer diagnostics; installed GameBridge DLL length is `975360`. Revalidation `20260613-0006` passed `git diff --check`, Release build/test, install, and title smoke. Intermediate setter normalization `20260613-0008` passed Release tests and smokes but was superseded by SaveSlots root fix `20260613-0013`, which passed Release build, `git diff --check`, title, pause, and MoreSaves smokes without installing a setter normalization hook.
  - Save: title homepage smoke plus local slot 3 / index 2 pause-menu layout smoke.
  - Log line: root-cause diagnostic `GAME-SMOKE/20260613-142631` logged the inherited Select stack through `SaveSlotsService.TryResetLayoutSize`, `RestoreOfficialSavePanel`, and `GameDataPanelSelectPrefix`. Root-fix title smoke `GAME-SMOKE/20260613-143434` passed title button/menu screenshot checks and logged `Save.MoreSlotsUiPaging = not-required` with no `requestedCount=2` or `Native UI layout normalized`. Pause smoke `GAME-SMOKE/20260613-143621` observed 14 samples over 12 seconds with `layoutConstraintCount=8`, `rowCount=1`, and `twoColumnObserved=False`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260613-143434` has title button/menu screenshot evidence and clean process/fatal checks. `docs/debug/evidence/GAME-SMOKE/20260613-143621/DTMAPI-evidence/UI-007/20260613-143712` has pause-menu initial/final screenshots and samples.
- Regression cases: TITLE-PAUSE-AUTOFISHING-ROOTFIX-20260613, TITLE-PAUSE-AUTOFISHING-FOLLOWUP-20260613, INSTALL-AUTOFISHING-UI-DIAGNOSTICS-20260613, TITLE-HOMEPAGE-GUARD-REMOVAL-20260612

## UI: TitleSettingsPaging

- Status: experimental
- Public surface: none; internal first-party title Settings UI over `IConfigMenuRuntime`, `DtmManagerViewModel`, and diagnostics snapshots.
- Game build: 23465763 workshop
- Game method/type: reflected Unity title canvas hosted by `ReflectedTitleMenuSettingsUi`; no raw public mod API or native gameplay hook. Official title/pause layout evidence belongs separately to `UI.NativeLayoutDiagnostics`; the title settings entry itself does not mutate official menu layout groups.
- Patch type: Unity UI reflection and DTMAPI runtime UI state; deterministic pager buttons for title config and Manager pages. The earlier title-homepage `HomePageTextMenu` single-column guard was removed in `20260612-0017` after manual feedback that it affected pause-menu horizontal icons. Update `20260613-0002` restores only native-owner layout invariants outside the title settings UI host.
- Why this point: the compact title-page DTMAPI icon entry, config page list, long config item pages, and Manager Mods/Errors/Warnings/Hooks/Features lists must remain reachable without exposing internal config edit verbs to ordinary mods or disturbing the official homepage menu layout.
- Failure behavior: if Unity UI types are unavailable, the title UI records `title-ui-types-missing` and no gameplay hook is affected. Paging state is in-memory and rebuilt with the reflected panel. Title Settings itself does not mutate official title/pause menu layout groups; official menu repair evidence belongs to `UI.NativeLayoutDiagnostics`.
- Mods/tests depending on it: DTMAPI title Manager smoke, ConfigMenu smoke screenshots, user manual QA for Manager/config visibility.
- Evidence:
  - Build: 2026-06-12 bottom-layer Release build/test passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`; `git diff --check` passed with line-ending warnings only.
  - Save: title homepage for UI entry/config/Manager evidence; third local save slot / index 2 for runtime follow-up smokes.
  - Log line: `GAME-SMOKE/20260612-165456` verifies the bottom-layer title/config route before the icon restoration follow-up; `GAME-SMOKE/20260612-204350` verifies the compact icon button and title settings menu screenshots, but its `UI.TitleHomeMenuLayout` guard evidence is superseded by `20260612-0017` because that guard was removed. `GAME-SMOKE/20260612-170119` verifies Manager Status, Mods, Errors, Hooks, Features, Logs, summary copy/fallback, and logs export.
  - 2026-06-13 packaged install evidence: package staging/install passed and `check-dtmapi-status.ps1` reported `[OK] DTMAPI title icon asset` for `D:\steam\steamapps\common\Doloc Town\BepInEx\plugins\DTMAPI\assets\branding\dtmapi-icon.png` with length `8018`. Title revalidation `GAME-SMOKE/20260613-114113` passed after `UI.NativeLayoutDiagnostics` owner-state homepage repair.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260613-114113` has latest title button/menu screenshot evidence; `docs/debug/evidence/GAME-SMOKE/20260612-204350` has earlier `DTMAPI-evidence/UI-004/20260612-204434/title-settings-button.png` and `title-settings-menu.png`; `docs/debug/evidence/GAME-SMOKE/20260612-170119` has Manager `UI-004/20260612-170153`; all cited runs have clean process/fatal checks.
- Regression cases: INSTALL-AUTOFISHING-UI-DIAGNOSTICS-20260613, BOTTOM-LAYER-REFACTOR-20260612, UI-001, CONFIG-006

## Hook: CustomEntities.CoreRegistry

- Status: verified
- Public surface: `ICustomAnimalApi`, `ICustomMonsterApi`, `ICustomAttackApi`, `ICustomDroneApi`
- Game build: 23465763 workshop
- Game method/type: DTMAPI Core runtime registry and save-boundary lifecycle
- Patch type: runtime dispatch
- Why this point: custom entity definitions, snapshots, lifecycle events, owner cleanup, and failure reasons are DTMAPI-owned registry state and do not require raw game object access; the public registry contracts remain `StableCandidate`.
- Failure behavior: invalid IDs and duplicates return result DTO errors; provider/listener exceptions are recorded under the owner; native spawn/summon/execute requests return `runtime-creation-blocked` until GameBridge adapters are verified.
- Mods/tests depending on it: no player-facing mod in 0.4.0; `DTMAPI.UnitTests` and internal `DTMAPI.CustomEntityApiSmokeHarness`.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors; unit test `CustomEntityRegistriesValidateRegistrationDuplicateCleanupAndSnapshots` verifies invalid ID, duplicate ID, four-family registration, snapshots/status, blocked requests, save-boundary cleanup, and owner cleanup.
  - Save: local slot 3 / index 2 in `GAME-SMOKE/20260610-045414`.
  - Log line: `GAME-SMOKE/20260610-045414` logs `CustomEntities.CoreRegistry = verified. StableCandidate 0.4.0 custom entity registry contracts are registered...`, `Smoke.CustomEntityApis = verified`, and blocked request summary `requests=runtime-creation-blocked`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-045414`; result has `CustomEntityApis=Passed`, `SaveLoaded=Passed`, `HookProbe=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Regression cases: CUSTOMENTITY-STATUS-NAMING-20260610, CUSTOM-ENTITY-040-STABLE-API

## Hook: CustomAnimals.RegistryContract

- Status: configured-blocked
- Public surface: `ICustomAnimalApi`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.AnimalManager.CreateAnimal`, `DolocTown.Animal`, animal work classes, `AnimalViewer`, and animal save data paths.
- Patch type: reflection research/status path; no native creation patch installed in 0.4.0.
- Why this point: native animals depend on `AnimalInfo` proto data, home/current rooms, feed/excrement/breeding work, produce rules, UI viewer rows, and save data. StableCandidate registry DTOs must stay separated from these fragile runtime details.
- Failure behavior: registration/query/snapshot works; `RequestSpawn` returns `runtime-creation-blocked` with adapter details instead of creating an unsafe animal.
- Mods/tests depending on it: internal custom entity smoke harness only.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2 in `GAME-SMOKE/20260610-045414`.
  - Latest naming: `GAME-SMOKE/20260610-045414` logs `CustomAnimals.RegistryContract = configured-blocked` with details `StableCandidate registry contract; runtime creation remains blocked`.
  - Historical smoke summary: `Smoke exercise CustomEntityApis OK registered=animal,monster,attack,drone; invalidAnimal=invalid-definition; duplicateAnimal=duplicate-definition-id; requests=runtime-creation-blocked; cleanupRemoved=5; lifecycleEvents=2/3/2/2.`
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-045414`.
- Regression cases: CUSTOMENTITY-STATUS-NAMING-20260610, CUSTOM-ENTITY-040-STABLE-API, ANIMAL-001

## Hook: CustomAnimals.AnimatorBridge

- Status: verified for AssetBundle controllers; Hatch `pngSpriteOverride` startup/hook-smoke verified / manual release pending
- Public surface: none; this is an internal ContentPack bridge schema, not `ICustomAnimalApi.RequestSpawn`.
- Game build: 23762374 public C416D4 reference
- Game method/type: `DolocTown.AnimatorAsset.TryLoadAsset(string,out RuntimeAnimatorController)`, `Animal.OnRender`, `RuntimeAnimatorController`, Unity `AssetBundle`, `DolocTown.SpriteOverrideHandler`, and downstream vanilla `DolocAPI.GetAsset<RuntimeAnimatorController>`.
- Patch type: GameBridge-owned Harmony prefix on the non-generic `AnimatorAsset.TryLoadAsset` override. Unknown addresses return unhandled and continue through the original Doloc Town animator asset cache. `animatorMode: assetBundle` loads an independent controller from a mod bundle; `animatorMode: pngSpriteOverride` returns the registered template controller and leaves PNG frame replacement to `CustomAnimals.PngSpriteBridge`.
- Why this point: native animal rendering already resolves table `animator.url` through `AnimatorAsset -> DolocAPI.GetAsset<RuntimeAnimatorController>`. Intercepting only registered DTMAPI animator keys lets custom animal rows point at independent package controllers or a template controller without mutating goat/chicken controllers or replacing live animal entities.
- Failure behavior: if a registered AssetBundle key cannot load its bundle/controller asset, the bridge records `CustomAnimals.AnimatorBridge.<key> = degraded`, logs a warning under the owner, and returns the template animator key fallback. If a registered `pngSpriteOverride` key cannot load its template controller, it degrades and returns handled-null so the failure is visible. Unknown vanilla keys, including goat/chicken, are never intercepted.
- Mods/tests depending on it: Shell Crab prototype package `DTMAPI_ShellCrab`; Hatch PNG prototype package `DTMAPI_HatchAssets`; `DTMAPI.UnitTests` cover metadata parse, key registration, unknown-key passthrough, missing-bundle degraded status, `pngSpriteOverride` parse, unknown mode ignore, and sprite-name mapping.
- Evidence:
  - Build: 2026-06-28 `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK` and `NU1900` package-vulnerability index warnings only.
  - Asset evidence: Unity 2021.3.45f2 batch built `E:\DolocTownUnity\DolocTownMeta\prototypes\shell_crab\DTMAPI_ShellCrab\Content\DTMAPI\assets\shell-crab\shell_crab_animators.bundle`; post-build validation loaded `shell_crab_adult` and `shell_crab_young` from the bundle.
  - Rejected hook evidence: `GAME-SMOKE/20260628-201917`, `20260628-202858`, `20260628-203345`, and `20260628-203832` showed closed `DolocAssetCache.GetAsset<RuntimeAnimatorController>` / `CheckAsset<RuntimeAnimatorController>` prefixes polluted Unity/Mono shared generic cache lookups and broke unrelated weather/UI prefab assets. The current hook avoids that path.
  - Runtime smoke: `docs/debug/evidence/GAME-SMOKE/20260628-205615` passed with slot/index `6`, HookProbe OK, Shell Crab ContentPack indexed, `CustomAnimals.AnimatorBridge` verified, registered keys `dtmapi_anim_animal_shell_crab` and `dtmapi_anim_animal_shell_crab_child`, no fatal popup, and no leftover `DolocTown.exe`.
  - Manual slot 7 barn release: Y debug console gave `sack_shell_crab`, the native animal bag flow opened the naming dialog, and the spawned animal rendered as Shell Crab rather than goat. Runtime log line: `CustomAnimals.AnimatorBridge verified key=dtmapi_anim_animal_shell_crab_child species=shell_crab stage=child ... asset=shell_crab_young`.
  - 2026-06-30 Hatch PNG slice: `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK`; `GAME-SMOKE/20260630-184600` passed slot 7 HookProbe/save-load/clean-exit and logs `DTMAPI.HatchAssets` indexed, registered keys `dtmapi_anim_animal_hatch` / `_child`, `hatch->chicken` AI template, and `pngSpriteOverrides=hatch`. This stable smoke includes the root official-local `info.json` fix so Doloc Town no longer removes `Local.DTMAPI_HatchAssets` from `mod_infos.json`. Manual Hatch release is still required to verify per-frame PNG replacement on a live renderer.
- Regression cases: SHELL-CRAB-ANIMATOR-BRIDGE-20260628, HATCH-PNG-CUSTOM-ANIMAL-20260630, CUSTOM-ENTITY-040-STABLE-API

## Hook: CustomAnimals.PngSpriteBridge

- Status: runtime-smoke verified / manual Hatch eat-frame verified / success logs throttled
- Public surface: none; this is an internal ContentPack bridge schema used by `animatorMode: pngSpriteOverride`.
- Game build: 23762374 public C416D4 reference
- Game method/type: private `DolocTown.Animal.OnRender()`, `DolocTown.Animal.DEBUG_SetAdult(bool)`, `DolocTown.AnimalRenderer.OnRecycle()`, protected `DolocTown.SpriteOverrideHandler.TryGetModOverrideSprite(string,out Sprite)`, and native `DolocAPI.modManager.LoadSpriteFromFile(string,out Sprite)`.
- Patch type: GameBridge-owned Harmony postfixes. `Animal.OnRender` and `DEBUG_SetAdult` attach or refresh a native `SpriteOverrideHandler` only on renderers whose animal `protoName` is a registered PNG custom species. `TryGetModOverrideSprite` maps template frame names to custom PNG frame names for the registered handler only. `AnimalRenderer.OnRecycle` removes the handler context to protect pooled renderers.
- Why this point: Hatch should reuse the chicken AnimatorController, AI, movement, sleep, and production route while replacing only frame sprites loaded from loose PNG files. This keeps Unity Editor/AssetBundle generation out of the path and avoids patching `DolocAssetCache<T>` or mutating chicken controllers.
- Failure behavior: unknown handlers, non-Hatch renderers, and non-template sprite names return unhandled. Missing mapped Hatch PNGs are not replaced, mark `CustomAnimals.PngSpriteBridge.<species>` degraded, and leave the template sprite visible for diagnosis. Successful mappings publish `CustomAnimals.PngSpriteBridge.<species>` only on the first mapped sprite per species to avoid per-frame HookProbe/log-export noise. `jump_ready` maps to `jump_0` because Hatch has no dedicated `jump_ready` PNG.
- Mods/tests depending on it: Hatch prototype package `DTMAPI_HatchAssets`; `DTMAPI.UnitTests` cover metadata parse, unknown mode ignore, template sprite mapping, `jump_ready -> jump_0`, child-to-young frame tolerance, and unregistered handler passthrough.
- Evidence:
  - Static: 2026-06-30 Hatch content JSON under `E:\DolocTownUnity\DolocTownMeta\prototypes\hatch\DTMAPI_HatchAssets\Content` passes PowerShell `ConvertFrom-Json`; Release unit tests cover metadata parsing and sprite-name mapping.
  - Runtime startup/hook smoke: `GAME-SMOKE/20260630-184600` passed with `CustomAnimals.PngSpriteBridge=verified`, `Animal.OnRender`, `Animal.DEBUG_SetAdult`, `AnimalRenderer.OnRecycle`, and `SpriteOverrideHandler.TryGetModOverrideSprite` all patched, and registered species `hatch:anim_animal_chicken->anim_animal_hatch`.
  - 2026-06-30 asset sync: after manual QA found the runtime package still had only 38 Hatch PNG files and missed `eat_4..6`, the local `MODS\DTMAPI_HatchAssets` package was resynced from the Hatch prototype source. The runtime package now has 44 Hatch PNG files, young `sprite_size` `32x26`, and adult `36x34`.
  - Runtime diagnostic smoke: `GAME-SMOKE/20260630-195052` passed slot 7 HookProbe/save-load/clean-exit with no `missing mapped sprite` and no `CustomAnimals.PngSpriteBridge.hatch = degraded` log lines.
  - Manual follow-up: Hatch direction and eating are fixed after the updated PNG/frame-count package; adult `eat_4`, `eat_5`, and `eat_6` map to Hatch sprites. 2026-06-30 source follow-up throttles verified sprite status publication to the first successful mapped sprite per species while preserving immediate degraded logs for missing mapped sprites.
- Regression cases: HATCH-PNG-CUSTOM-ANIMAL-20260630, CUSTOM-ANIMAL-SLEEPWAKE-DIAGNOSTICS-20260630, SHELL-CRAB-ANIMATOR-BRIDGE-20260628

## Hook: CustomAnimals.SleepWakeDiagnostics

- Status: runtime-smoke-verified / suspicious-render diagnostics only / log-trimmed
- Public surface: none; this is an internal diagnostic-only hook set for registered custom livestock species.
- Game build: 23762374 public C416D4 reference
- Game method/type: private `DolocTown.Animal.OnRender()`, public/internal `DolocTown.Animal.Sleep()`, `DolocTown.Animal.WakeUp()`, `DolocTown.Animal.CallToRoom(...)`, `DolocTown.AnimalRenderer.OnFell(...)`, `DolocTown.AnimalRenderer.PlayAnimation(...)`, and private `DolocTown.AnimalRenderer.FixedUpdate()`.
- Patch type: GameBridge-owned Harmony postfixes on `Sleep`, `WakeUp`, `CallToRoom`, `PlayAnimation`, and `FixedUpdate`, a suspicious-snapshot postfix on `OnRender`, plus prefix/postfix diagnostics around `AnimalRenderer.OnFell`.
- Why this point: manual QA showed Hatch and Shell Crab can sometimes render asleep when entering the barn at night, then stand up after a short delay, while vanilla animals were not observed doing so. Existing logs could not identify whether the source was AI sleep exit, renderer/tool collision, room call-to-room, or render state drift.
- Failure behavior: diagnostics are scoped to registered custom species only (`hatch`, `shell_crab` in the current local packages). Unknown species and native animals are ignored. `Sleep`, `WakeUp`, `CallToRoom`, and `AnimalRenderer.OnFell` remain low-frequency source logs. `Animal.OnRender` logs only suspicious snapshots, currently `sleep=true` with a non-sleep AI state, includes runtime animal identity/position labels plus renderer animator-state labels when available, and suppresses repeated identical signatures per animal instance including renderer state. `AnimalRenderer.PlayAnimation` logs only custom-animal sleep-related calls and is throttled to one line per animal/reason/animation name so changing normalized time does not flood exported logs. `AnimalRenderer.FixedUpdate` logs only bounded follow-up samples for renderers previously marked by a suspicious sleeping `OnRender`; after 2026-06-30 it clears immediately once the animal is still asleep, AI is settled/no-state, task is native wait/none, and renderer is already playing `sleep`, and its repeated-signature check ignores the `animState=sleep@normalizedTime` value. Remaining unresolved follow-ups are capped at 60 fixed frames. Callback failures are wrapped by the existing safe hook callback path and do not suppress native sleep/wake/collision behavior.
- Mods/tests depending on it: Hatch prototype package `DTMAPI_HatchAssets`, Shell Crab prototype package `DTMAPI_ShellCrab`; `DTMAPI.UnitTests` verifies diagnostic species are scoped to registered custom animals, exclude native identity mappings, and trim stable sleep renderer follow-up diagnostics without hiding movement-task cases.
- Evidence:
  - Review/root-cause note: `docs/reviews/manual-qa/2026/20260630-0002-hatch-shellcrab-eat-sleep-review.md` preserves the user issue order and records rejected hypotheses before adding hooks.
  - Build: 2026-06-30 `tools/scripts/test.ps1 -Configuration Release` and `tools/scripts/build.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings were emitted.
  - Runtime smoke: `GAME-SMOKE/20260630-195052` passed with `CustomAnimals.SleepWakeDiagnostics=verified`, hook install flags `animalSleep=True`, `animalWakeUp=True`, `animalCallToRoom=True`, `animalRendererOnFellPrefix=True`, `animalRendererOnFellPostfix=True`, diagnostic species `hatch,shell_crab`, slot 7 save load, clean exit, and no leftover `DolocTown.exe`.
  - Runtime smoke: `GAME-SMOKE/20260630-205912` passed after adding the renderer follow-up diagnostics, with `animalRendererPlayAnimation=True`, `animalRendererFixedUpdate=True`, `diagnosticSpecies=hatch,shell_crab`, HookProbe, slot 7 save load, clean exit, and no leftover `DolocTown.exe`.
  - Runtime smoke: `GAME-SMOKE/20260630-230749` passed after the log trim, with `animalRendererFixedUpdate=True`, `CustomAnimals.SleepTaskBoundary=verified`, clean exit, no leftover `DolocTown.exe`, and zero `AnimalRenderer.FixedUpdateFollowUp` lines in `Unity-Player.log`.
  - Manual follow-up log: tool-caused wakeups are native `AnimalRenderer.OnFell.Prefix ... tool=ItemTool:steel_sickle` followed by `Animal.WakeUp`. The first-entry Shell Crab stand-up did not log `WakeUp`, `OnFell`, or `CallToRoom`; it logged `sleep=true` with `aiState=Goat_FreeTimeState` at night, narrowing the remaining issue to a sleep flag vs AI state/render lifecycle mismatch.
  - Code-level audit: `docs/reviews/code/2026/20260630-0003-custom-animal-sleep-state-log-audit.md` reviewed native metadata for sleep/free-time transitions and rejected a behavior patch until a concrete transition owner is proven. 2026-06-30 source follow-up keeps the low-frequency wake-source events, suppresses ordinary `OnRender` noise, and adds instance-level suspicious snapshot labels plus renderer-state labels so one Shell Crab can be followed across later logs and checked for `idle` vs `sleep` animation. Update `20260630-0004` first added diagnostic-only immediate `PlayAnimation("sleep")` and next-`FixedUpdate` renderer sampling, then extended it after user clarification that the wake can persist/move. After the sleep/task boundary passed a five-night manual retest, update `20260630-0006` trims this follow-up: stable sleep clears without logging per frame, repeated signatures ignore animator normalized time, and the unresolved follow-up cap is 60 fixed frames.
- Regression cases: CUSTOM-ANIMAL-SLEEPWAKE-DIAGNOSTICS-20260630, HATCH-PNG-CUSTOM-ANIMAL-20260630, SHELL-CRAB-AI-TEMPLATE-WAKE-20260629, SHELL-CRAB-ANIMATOR-BRIDGE-20260628

## Hook: CustomAnimals.SleepTaskBoundary

- Status: runtime-smoke-verified / manual five-night midnight retest passed
- Public surface: none; this is an internal GameBridge stability boundary for registered custom livestock species.
- Game build: 23762374 public C416D4 reference
- Game method/type: protected `DolocTown.AnimalAI/AnimalAIState.MakeDecision_FreeTime() -> RedSaw.AI.LinearTask.LinearTask`, `DolocTown.AnimalController.OnUpdate(float)`, `RedSaw.AI.LinearTask.LinearTask.WaitFrames(int)`, and `RedSaw.AI.LinearTask.DecisionMaker.StopTask()`.
- Patch type: GameBridge-owned Harmony prefix on the shared native FreeTime decision helper plus postfix on `AnimalController.OnUpdate`. The prefix returns the native `LinearTask.WaitFrames(5)` only when the animal is a registered custom species, `isSleep=true`, and `CurrentDayPeriodType=Night`. The controller postfix breaks stale movement-like tasks after AI update and before task execution when the same boundary is true.
- Why this point: manual slot 7 logs proved a sustained visible stand/move case can be `sleep=true`, `period=Night`, `aiState=Normal_SleepState`, and `task=DolocTown.AnimalMove` without `WakeUp`, `OnFell`, or `CallToRoom`. Native FreeTime decision lacks an `isSleep` guard and `DecisionMaker` does not automatically stop an already-created movement task when the state machine later reaches sleep. The bridge exposed that native timing gap for custom template animals after midnight/day-change/room-entry.
- Scope guard: vanilla animals and unregistered species are ignored. Walking to bed remains untouched because `isSleep=false`; morning wake remains untouched because the period is not `Night`; tool wake remains untouched because native `WakeUp()` clears `isSleep`.
- Failure behavior: if `LinearTask.WaitFrames` cannot be resolved, the FreeTime prefix falls through and `CustomAnimals.SleepTaskBoundary` is marked degraded. If no stale movement task exists, the controller postfix does nothing. The bridge does not modify animator controllers, sprite overrides, template `AnimalInfo`, original animal tables, or renderer state.
- Mods/tests depending on it: Shell Crab prototype package `DTMAPI_ShellCrab`; Hatch PNG prototype package `DTMAPI_HatchAssets`; `DTMAPI.UnitTests` cover custom species scoping, Night/asleep guard conditions, and the movement-task allowlist (`AnimalMove`, `AnimalJump`, `AnimalEnterRoom`) versus wait/eat tasks.
- Evidence:
  - Reverse evidence: native metadata shows chicken/goat/free-time states call `AnimalAIState.MakeDecision_FreeTime`; `MakeDecision_Sleep` returns `LinearTask.WaitFrames(5)` when `animal.isSleep`; `DecisionMaker.StopTask()` and `LinearTask.WaitFrames(int)` are public native methods.
  - Source validation: 2026-06-30 `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` warnings were emitted.
  - Static validation: 2026-06-30 `git diff --check` passed with CRLF normalization warnings only.
  - Runtime smoke: `GAME-SMOKE/20260630-222709` passed `StartupLog`, `GameLaunched`, `SaveLoaded`, `HookProbe`, `ProcessExited`, `NoFatalInstanceWindow`, and `ForcedClose`; `process-check.txt` reports no `DolocTown.exe`.
  - Runtime smoke: `GAME-SMOKE/20260630-230749` passed after the diagnostic follow-up log trim, still verifying `CustomAnimals.SleepTaskBoundary=verified`, `animalAIMakeDecisionFreeTime=True`, `animalControllerOnUpdate=True`, and clean process exit.
  - Log line: `CustomAnimals.SleepTaskBoundary = verified. Custom animal sleep task boundary hooks are patched for species=hatch,shell_crab.`
  - Log line: `animalAIMakeDecisionFreeTime=True animalControllerOnUpdate=True ... diagnosticSpecies=hatch,shell_crab customSpecies=hatch,shell_crab.`
  - Manual follow-up: 2026-06-30 slot 7 five-night manual retest with debug time skips and post-midnight barn entry kept Hatch/Shell Crab sleeping. Latest `Player.log` has no residual custom-animal `sleep=true period=Night task=DolocTown.AnimalMove`; it contains expected `FreeTimeDecisionGuard ... replacement=WaitFrames(5)` lines for Shell Crab and Hatch at 00:00, plus ordinary tool/morning `WakeUp` events.
- Regression cases: CUSTOM-ANIMAL-SLEEPWAKE-DIAGNOSTICS-20260630, HATCH-PNG-CUSTOM-ANIMAL-20260630, SHELL-CRAB-AI-TEMPLATE-WAKE-20260629

## Hook: CustomAnimals.AiTemplateBridge

- Status: runtime-smoke-verified / manual-overnight-pending
- Public surface: none; this is an internal ContentPack bridge schema, not `ICustomAnimalApi.RequestSpawn`.
- Game build: 23762374 public C416D4 reference
- Game method/type: private static `DolocTown.AnimalAI.GetDefaultAnyState(string) -> System.Type`, called from `AnimalAIState` construction; downstream `Normal_SleepState.GetNextState()`, `StateMachine.Update`, and `Animal.WakeUp()`.
- Patch type: GameBridge-owned Harmony postfix. The postfix only replaces `__result` when the queried animal id is a registered enabled DTMAPI custom species.
- Why this point: custom animals may keep a native template `schedule_id` such as `goat` to reuse eating, sleeping, breeding, movement, excrement, and production-device AI. Native AI creation uses `schedule_id` for the state machine but uses `animal.protoName` for the default any-state lookup. For `shell_crab` with `schedule_id: goat`, vanilla returns `Normal_FreeTimeState`, which is absent from the goat state machine; sleep exit then returns null and `Normal_SleepState.OnExit()` never calls `Animal.WakeUp()`. Mapping only `shell_crab -> goat` at `GetDefaultAnyState` preserves native state-machine ownership while fixing the custom species/template name boundary.
- Failure behavior: unknown species and unregistered custom species keep the vanilla return value. If a registered template state type cannot be resolved, DTMAPI records `CustomAnimals.AiTemplateBridge.<species> = degraded` and preserves the original result. The bridge does not mutate goat/chicken controllers, template `AnimalInfo`, `schedule_id`, renderer sprites, or live entities.
- Mods/tests depending on it: Shell Crab prototype package `DTMAPI_ShellCrab`; `DTMAPI.UnitTests` cover metadata parse, identity mapping skip, unknown species passthrough, and missing native type degraded fallback.
- Evidence:
  - Build: 2026-06-29 `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK` and restricted-network `NU1900` warnings only.
  - Reverse evidence: `AnimalController` creates AI with `animal.proto.ScheduleId`; `AnimalAIState` initializes `defaultAnyState` from `GetDefaultAnyState(animal.protoName)`; `Normal_SleepState.GetNextState()` returns `defaultAnyState` after sleep time; `Normal_SleepState.OnExit()` owns `Animal.WakeUp()`.
  - Runtime smoke: `GAME-SMOKE/20260629-164028` passed with `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`; process check found no leftover `DolocTown.exe`.
  - Log line: `CustomAnimals.AiTemplateBridge = verified. AnimalAI.GetDefaultAnyState hook is patched; registered custom animal AI templates=shell_crab->goat.`
  - Log line: `CustomAnimals.AiTemplateBridge.shell_crab = verified. Mapped custom animal species=shell_crab to aiTemplate=goat stateType=DolocTown.AnimalAI+Goat_FreeTimeState.`
  - Save: `SaveLoaded hook dispatched. slot/index=6 isNewGame=False` and `HookProbe SaveLoaded OK slot=6 isNewGame=False`.
  - Manual slot 7 overnight wake retest: pending.
- Regression cases: SHELL-CRAB-AI-TEMPLATE-WAKE-20260629, SHELL-CRAB-ANIMATOR-BRIDGE-20260628

## Hook: CustomMonsters.RegistryContract

- Status: configured-blocked
- Public surface: `ICustomMonsterApi`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.MonsterController`, `MonsterGroupManager`, `MonsterAI_Target`, `MonsterStateManager`, `MonsterAttackBehaviour`, and `MonsterAttackBehaviourManager`.
- Patch type: reflection research/status path; no native creation patch installed in 0.4.0.
- Why this point: native monsters require verified spawn group, AI, movement, attack, damage, death/drop, and despawn adapters. StableCandidate registry DTOs define the author contract without exposing raw update loops.
- Failure behavior: registration/query/spawn-table/snapshot works; `RequestSpawn` returns `runtime-creation-blocked` until native adapters are verified.
- Mods/tests depending on it: internal custom entity smoke harness only.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2 in `GAME-SMOKE/20260610-045414`.
  - Latest naming: `GAME-SMOKE/20260610-045414` logs `CustomMonsters.RegistryContract = configured-blocked` with details `StableCandidate registry contract; runtime creation remains blocked`.
  - Historical smoke summary: `Smoke exercise CustomEntityApis OK ... cleanupRemoved=5`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-045414`.
- Regression cases: CUSTOMENTITY-STATUS-NAMING-20260610, CUSTOM-ENTITY-040-STABLE-API

## Hook: CustomAttacks.RegistryContract

- Status: configured-blocked
- Public surface: `ICustomAttackApi`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.BulletFactory`, `BulletManager`, `Bullet`, `BulletEntity`, `PhysicalDamageBox`, `AttackInfo`, and `AttackHitInfo`.
- Patch type: reflection research/status path; no native projectile/damage patch installed in 0.4.0.
- Why this point: projectile creation and barrage behavior must preserve native collision and damage ownership without exposing physics/collider/decompiled types to public API consumers.
- Failure behavior: registration/query/snapshot works; `SpawnProjectile` and `ExecuteAttack` return `runtime-creation-blocked` until BulletManager/collision/damage adapters are verified.
- Mods/tests depending on it: internal custom entity smoke harness only; future monster and drone APIs reference attack IDs.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2 in `GAME-SMOKE/20260610-045414`.
  - Latest naming: `GAME-SMOKE/20260610-045414` logs `CustomAttacks.RegistryContract = configured-blocked` with details `StableCandidate registry contract; runtime creation remains blocked`.
  - Historical smoke summary: `Smoke exercise CustomEntityApis OK ... requests=runtime-creation-blocked`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-045414`.
- Regression cases: CUSTOMENTITY-STATUS-NAMING-20260610, CUSTOM-ENTITY-040-STABLE-API

## Hook: CustomDrones.RegistryContract

- Status: configured-blocked
- Public surface: `ICustomDroneApi`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.Drone`, `DroneController`, `DroneWeapon`, `DroneWeaponGun`, `DroneWeaponSword`, `DronePanel`, and `DolocAPI.EquipDrone`.
- Patch type: reflection research/status path; no native drone creation/equipment patch installed in 0.4.0.
- Why this point: native drones combine controller, weapon, equipment, movement, owner binding, UI panel, and save/persistence behavior. StableCandidate registry definitions keep future mods away from fragile raw types.
- Failure behavior: registration/query/snapshot works; `RequestSummon`, `Equip`, and `SetMode` return `runtime-creation-blocked` until drone controller/weapon/equipment adapters are verified.
- Mods/tests depending on it: internal custom entity smoke harness only.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2 in `GAME-SMOKE/20260610-045414`.
  - Latest naming: `GAME-SMOKE/20260610-045414` logs `CustomDrones.RegistryContract = configured-blocked` with details `StableCandidate registry contract; runtime creation remains blocked`.
  - Historical smoke summary: `Smoke exercise CustomEntityApis OK ... requests=runtime-creation-blocked`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-045414`.
- Regression cases: CUSTOMENTITY-STATUS-NAMING-20260610, CUSTOM-ENTITY-040-STABLE-API

## Diagnostic: Startup.SegmentTiming

- Status: experimental
- Public surface: DTMAPI startup logs and smoke evidence only.
- Game build: 23465763 workshop
- Game method/type: Steam launch wall-clock checkpoints plus BepInEx `Awake`, DTMAPI runtime start, manifest/official MODS/workshop scans, content query index, Harmony initialization, icon loading, and mod loading.
- Patch type: timing instrumentation around existing bootstrap/runtime paths plus smoke-harness timeline capture.
- Why this point: the occasional 30s launch must be compared through segment logs instead of guessed optimizations.
- Failure behavior: if Steam never creates `DolocTown.exe`, no DTMAPI segment log exists; that is tracked as a launch-blocking smoke issue rather than DTMAPI startup slowness.
- Mods/tests depending on it: smoke harness, repeated startup sampler, startup regression matrix.
- Evidence:
  - Build: DTMAPI 0.2.1 local build passed 2026-06-01 with 0 errors; earlier 0.1.13 build/unit passed 2026-05-31
  - Save: n/a
  - Log line: startup segment logs for `Bootstrap.Awake`, `Bootstrap.HarmonyInitialize`, `Core.Start`, `ManifestScan`, `OfficialModsScan`, `WorkshopScan`, `ContentQueryIndex`, `ModLoad`, and `IconLoad`; smoke timeline fields `LaunchToProcessMs`, `LaunchToDtmapiLogFileMs`, and `LaunchToStartupPatternMs`.
  - Screenshot/report: normal collected logs `docs/debug/evidence/GAME-SMOKE/20260531-114255`, `docs/debug/evidence/GAME-SMOKE/20260531-115235`, `docs/debug/evidence/GAME-SMOKE/20260531-124429`, `docs/debug/evidence/GAME-SMOKE/20260531-125453`, `docs/debug/evidence/GAME-SMOKE/20260531-125613`, `docs/debug/evidence/GAME-SMOKE/20260531-160943`, and `docs/debug/evidence/GAME-SMOKE/20260531-161545`; launch-blocked evidence `docs/debug/evidence/GAME-SMOKE/20260531-120507` and `docs/debug/evidence/GAME-SMOKE/20260531-123258`; analyzer report `docs/debug/evidence/STARTUP-COMPARE/20260531-162725/startup-analysis.md`; integrated title smoke evidence `docs/debug/evidence/GAME-SMOKE/20260531-163330/startup-analysis.md`; integrated timeline smoke evidence `docs/debug/evidence/GAME-SMOKE/20260531-164214/startup-timeline.json` and `docs/debug/evidence/GAME-SMOKE/20260531-164214/startup-analysis.md`; repeated startup aggregate `docs/debug/evidence/STARTUP-SAMPLES/20260531-165252/startup-analysis.md` with primary timelines `docs/debug/evidence/GAME-SMOKE/20260531-165253/startup-timeline.json` and `docs/debug/evidence/GAME-SMOKE/20260531-165447/startup-timeline.json`; extended baseline aggregate `docs/debug/evidence/STARTUP-SAMPLES/20260531-170717/startup-analysis.md` with five normal primary samples from `docs/debug/evidence/GAME-SMOKE/20260531-170718` through `docs/debug/evidence/GAME-SMOKE/20260531-171105`; fast pure-startup aggregate `docs/debug/evidence/STARTUP-SAMPLES/20260531-171630/startup-analysis.md` with five normal primary samples from `docs/debug/evidence/GAME-SMOKE/20260531-171631` through `docs/debug/evidence/GAME-SMOKE/20260531-171859`; threshold-summary aggregate `docs/debug/evidence/STARTUP-SAMPLES/20260531-172605/startup-sample-summary.md` with two normal primary samples `docs/debug/evidence/GAME-SMOKE/20260531-172606` and `docs/debug/evidence/GAME-SMOKE/20260531-172640`; stop-on-slow aggregate `docs/debug/evidence/STARTUP-SAMPLES/20260531-173318/startup-samples.md` proved `-StopOnSlowSample` using artificial `SlowLaunchThresholdMs=1`, stopped after primary sample `docs/debug/evidence/GAME-SMOKE/20260531-173318`, and did not indicate DTMAPI runtime slowness; real-threshold stop-on-slow baseline `docs/debug/evidence/STARTUP-SAMPLES/20260531-174136/startup-sample-summary.md` captured six normal samples from `docs/debug/evidence/GAME-SMOKE/20260531-174136` through `docs/debug/evidence/GAME-SMOKE/20260531-174425` with `SlowLaunchCount=0`, `SlowRuntimeCount=0`, `LaunchToStartupPatternMs=6136-6165`, and `Bootstrap.Awake totalMs=587-615`; monitor normal validation `docs/debug/evidence/STARTUP-MONITOR/20260531-175403/startup-monitor.md` captured two normal samples and `Triggered=False`; monitor artificial trigger validation `docs/debug/evidence/STARTUP-MONITOR/20260531-175538/startup-monitor.md` stopped on `slow-launch-threshold` with artificial `SlowLaunchThresholdMs=1`; real-threshold monitor run `docs/debug/evidence/STARTUP-MONITOR/20260531-180222/startup-monitor.md` captured two batches / six normal samples in `docs/debug/evidence/STARTUP-SAMPLES/20260531-180222` and `docs/debug/evidence/STARTUP-SAMPLES/20260531-180406`, with `Triggered=False`, `LaunchToStartupPatternMs=6142-7172`, and `Bootstrap.Awake totalMs=589-608`; follow-up real-threshold monitor run `docs/debug/evidence/STARTUP-MONITOR/20260531-181048/startup-monitor.md` captured three batches / nine normal samples in `docs/debug/evidence/STARTUP-SAMPLES/20260531-181048`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-181230`, and `docs/debug/evidence/STARTUP-SAMPLES/20260531-181413`, with `Triggered=False`, `LaunchToStartupPatternMs=6139-7170`, and `Bootstrap.Awake totalMs=586-612`.
  - Failure-capture validation: title-settings monitor `docs/debug/evidence/STARTUP-MONITOR/20260531-182957/startup-monitor.md` stopped with `sample-failure-count-1`, while linked `docs/debug/evidence/STARTUP-SAMPLES/20260531-182958/startup-analysis.md` classified the primary smoke `docs/debug/evidence/GAME-SMOKE/20260531-182958` as `NormalDtmapiStartup` with `LaunchToStartupPatternMs=6153` and `Bootstrap.Awake totalMs=657`; normal no-title recheck `docs/debug/evidence/STARTUP-SAMPLES/20260531-183210/startup-sample-summary.md` recorded `LaunchToStartupPatternMs=6159`, `Bootstrap.Awake totalMs=610`, and no leftover process.
  - Longer real-threshold monitor: `docs/debug/evidence/STARTUP-MONITOR/20260531-183958/startup-monitor.md` captured four batches / twelve normal startup samples in `docs/debug/evidence/STARTUP-SAMPLES/20260531-183958`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-184141`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-184325`, and `docs/debug/evidence/STARTUP-SAMPLES/20260531-184508`, with `Triggered=False`, `LaunchToStartupPatternMs=6132-6172`, `Bootstrap.Awake totalMs=584-638`, and no leftover process.
  - Follow-up longer real-threshold monitor: `docs/debug/evidence/STARTUP-MONITOR/20260531-185341/startup-monitor.md` captured five batches / twenty normal startup samples in `docs/debug/evidence/STARTUP-SAMPLES/20260531-185341`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-185551`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-185759`, `docs/debug/evidence/STARTUP-SAMPLES/20260531-190010`, and `docs/debug/evidence/STARTUP-SAMPLES/20260531-190217`, with `Triggered=False`, `LaunchToStartupPatternMs=6133-7181`, `Bootstrap.Awake totalMs=586-617`, and no leftover process.
  - External launch observer: `docs/debug/evidence/STARTUP-OBSERVE/20260531-191241/startup-analysis.md` verified that no-launch timeout does not reuse stale logs; `docs/debug/evidence/STARTUP-OBSERVE/20260531-191352/startup-analysis.md` captured an external Steam URL launch as `NormalDtmapiStartup` with `LaunchToStartupPatternMs=7236`, `Bootstrap.Awake totalMs=230`, and cleanup evidence `process-check-after-cleanup.txt` showing no leftover process.
  - Startup comparison gate: `docs/debug/evidence/STARTUP-COMPARE/20260531-192226/startup-comparison.md` compared normal external-launch evidence with blocked/no-fresh-log evidence and reported `OnlyPreRuntimeOrBlockedAbnormalEvidence`, so the true 30-second DTMAPI runtime comparison remains pending.
- Regression cases: STARTUP-001, SMOKE-002

## Hook: GameLoop.GameLaunched

- Status: verified
- Public surface: `helper.Events.GameLoop.GameLaunched`
- Game build: 23465763 workshop
- Game method/type: DTMAPI runtime lifecycle after mod `Entry`
- Patch type: runtime dispatch
- Why this point: lets mods subscribe during `Entry` and receive a first ready signal.
- Failure behavior: if a mod handler throws, DTMAPI records the owning mod error.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`, `DTMAPI.HelloDtmMod`
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: n/a
  - Log line: `HookProbe GameLaunched OK`
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260530-071017`
- Regression cases: HOOK-001

## Hook: GameLoop.UpdateTicked

- Status: verified
- Public surface: `helper.Events.GameLoop.UpdateTicked`
- Game build: 23465763 workshop
- Game method/type: BepInEx plugin frame callback with fallback pump
- Patch type: Unity callback / SynchronizationContext fallback / coroutine fallback
- Why this point: no game internals exposed; enough for first QoL mods and smoke probes.
- Failure behavior: if Unity `Update` does not fire, DTMAPI uses the fallback pump and logs the source.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`, official-local `Yuuka.DTMAPI.ActionSpeed` hotload smoke
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: local slot 3 / index 2
  - Log line: `HookProbe UpdateTicked OK tick=1`
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-080759`
- Regression cases: LOOP-001

## Hook: GameLoop.OneSecondUpdateTicked

- Status: verified
- Public surface: `helper.Events.GameLoop.OneSecondUpdateTicked`
- Game build: 23465763 workshop
- Game method/type: DTMAPI timer derived from frame loop
- Patch type: runtime dispatch
- Why this point: throttled periodic work without mod-side timers.
- Failure behavior: no event if frame loop stops.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.11 local
  - Save: local slot 3 / index 2
  - Log line: `HookProbe OneSecondUpdateTicked OK second=1`
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-202404`
- Regression cases: LOOP-002

## Hook: Save.SaveLoaded

- Status: verified
- Public surface: `helper.Events.Save.SaveLoaded`
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.OnAfterLoadArchiveData` event, fallback candidate `DolocAPI.AfterLoadArchiveData(bool isNewGame)`
- Patch type: UnityEvent subscription, Harmony Postfix fallback
- Why this point: Save_Load map identifies it as a medium/risky post-load candidate with no public raw game type exposure.
- Failure behavior: status remains pending; no save event is exposed as verified.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: local slot 3 / index 2
  - Log line: `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`
  - 2026-06-10 lifecycle isolation log line: `GAME-SMOKE/20260610-012052`, `GAME-SMOKE/20260610-012242`, `GAME-SMOKE/20260610-012400`, `GAME-SMOKE/20260610-012750`, and `GAME-SMOKE/20260610-012907` all record `SaveLoaded=Passed` after `DolocTownHookCallbacks` wrapped each SaveLoaded cleanup/restore/runtime notify/smoke-mark callback in `SafeCallback`; the same logs contain no `Lifecycle callback failed` entries.
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-080759`
- Regression cases: SAVE-001

## Hook: Save.LoadGameRequested

- Status: verified
- Public surface: diagnostics/internal save-load evidence
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.LoadGame(int index)` / fallback `DolocTown.GameData.DataPersistenceManager.LoadGame`
- Patch type: Harmony Prefix; phase 5 also pairs this with the internal-only `Runtime.SaveLoadRequestCoordinator` and a diagnostic postfix for native return.
- Why this point: records the requested save slot before `SaveLoaded` so public save events can carry a stable slot/index without raw game types. Phase 5 additionally gives the request a request id so long-title-idle load transitions can be closed or diagnosed.
- Failure behavior: `SaveLoaded` still dispatches with `SaveSlot=null` if the load request cannot be observed. If the coordinator is enabled, missing SaveLoaded leaves an active request with last phase; DTMAPI/smoke-originated duplicate direct loads are suppressed while native UI clicks are not intercepted.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: local slot 3 / index 2
  - Log line: `LoadGame requested for slot/index 2.`
  - Phase 5: `GAME-SMOKE/20260703-201302` logs `LoadGame requested for slot/index 2. requestId=SL-0001`, then closes the same request through SaveLoaded and native return with no duplicate requests after 3600 seconds title idle.
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-080759`
- Regression cases: SAVE-001, SAVELOAD-REQUEST-COORDINATOR-20260703

## Hook: Save.SaveSaving

- Status: verified
- Public surface: `helper.Events.Save.SaveSaving`
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.SaveGame` / fallback `DolocTown.GameData.DataPersistenceManager.SaveGame`
- Patch type: Harmony Prefix
- Why this point: exposes a pre-save event without raw save handles.
- Failure behavior: no save-saving event; diagnostics status remains pending/experimental.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: local slot 5 / index 4
  - Log line: `HookProbe SaveSaving OK slot=4`
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-081411`
- Regression cases: SAVE-002

## Hook: Save.SaveSaved

- Status: verified
- Public surface: `helper.Events.Save.SaveSaved`
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.SaveGame` / fallback `DolocTown.GameData.DataPersistenceManager.SaveGame`
- Patch type: Harmony Postfix
- Why this point: exposes a post-save event without raw save handles.
- Failure behavior: no save-saved event; diagnostics status remains pending/experimental.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.0 local
  - Save: local slot 5 / index 4
  - Log line: `HookProbe SaveSaved OK slot=4`
  - 2026-06-10 lifecycle isolation log line: `GAME-SMOKE/20260610-012750` records `InstantSave=Passed`, `SaveSaving hook dispatched. slot/index=2`, `SaveSaved hook dispatched. slot/index=2`, clean exit, no fatal popup, and no `Lifecycle callback failed` entries after save prefix/postfix callbacks were wrapped independently.
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-081411`
- Regression cases: SAVE-002

## Diagnostic: Debug.InstantSave

- Status: experimental
- Public surface: smoke/debug testing feature only; not a stable public API.
- Game build: 23465763 workshop
- Game method/type: native `DolocAPI.SaveGame(int index)` with reflected room/position/time snapshots before and after the save call. Historical debug-only reload evidence used `DolocAPI.LoadGame(int index)`, but the 0.2.6 player-facing Y console path is save-only.
- Patch type: smoke/debug reflection call over the native save method; public APIs do not expose raw save data.
- Why this point: lets hook/API validation create a checkpoint in field, fishing, or machine-test scenes while using the game's own save path instead of writing save files directly.
- Failure behavior: `reloadAfterSave=true` now returns `reload-disabled`; the player-facing console does not perform save-then-immediate-load because manual QA showed active-scene residue.
- Mods/tests depending on it: smoke harness only.
- Evidence:
  - Build: DTMAPI 0.1.13 local build/unit passed 2026-05-31
  - Save: local slot 3 / index 2
  - Log line: 0.2.6 focused smoke `GAME-SMOKE/20260605-181224` logs `Smoke exercise InstantSave OK ... sameRoom=True, distance=0, reloadDisabled=True`; historical 0.1.13 debug-only evidence logged `LoadGame requested for slot/index 2` and `limitation=none`.
  - Screenshot/report: smoke result `docs/debug/evidence/GAME-SMOKE/20260531-115149`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-115235`; first early-sample limitation attempt `docs/debug/evidence/GAME-SMOKE/20260531-114928`.
- Regression cases: SAVE-003

## Hook: Workshop.ReloadMods

- Status: verified
- Public surface: `helper.Events.Workshop.ModListChanged`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.Config.ModManager.ReloadMods`
- Patch type: Harmony Postfix
- Why this point: respects official ModManager and uses DTMAPI UI only for status/diagnostics. As of 2026-05-31, the notification also lets DTMAPI hot-load newly enabled, not-yet-loaded code mods without taking over official enable/disable ownership.
- Failure behavior: DTMAPI startup scan still works; if a mod is already loaded and later officially disabled, DTMAPI does not attempt DLL unload and marks config as restart-required.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`
- Evidence:
  - Build: DTMAPI 0.1.12 local build/unit passed 2026-05-31
  - Save: title homepage smoke
  - Log line: `Workshop ModListChanged hook dispatched. discoveredMods=5 hotLoaded=1`, followed by later `hotLoaded=0`
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260531-011745`, state backup/restored in `docs/debug/evidence/OFFICIAL-HOTLOAD/20260531-011650`
- Regression cases: WORKSHOP-001, WORKSHOP-002

## Hook: Workshop.LocalUploadPlan

- Status: verified-display; live upload pending
- Public surface: none; internal official-local Workshop update-button display guard.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.UI.ModData..ctor`
- Patch type: Harmony constructor Postfix
- Why this point: `ModViewer.Render` chooses `上传模组` vs `更新模组` from `ModData.canUpdateWorkshopItem`. DTMAPI only corrects that display flag for generated official-local packages with nonzero `workshopId` and a generated DTMAPI marker: `Content/DTMAPI/dtmapi-package.json` for ordinary functional packages or `Content/DTMAPI/release-manifest.json` for the runtime package. Native `ModManager.ResolveLocalModUploadPlan` and `SteamWorkshopUploader.UploadMod` still own Steam item-detail resolution and actual upload execution.
- Failure behavior: non-DTMAPI local mods, missing markers, zero ids, and reflection failures keep native display behavior. If the hook does not install, DTMAPI-generated local upload packages can again transiently show `上传模组` when native plan resolution is unavailable or stale. Upload execution should still use the official native path either way.
- Mods/tests depending on it: official-local upload packages generated by DTMAPI release tooling, including `DTMAPI` and `DTMAPI 更多装备栏位`.
- Evidence:
  - 2026-06-14 local diagnosis: `DTMAPI_MoreEquipmentSlots/workshop.json` still contained `workshop_id=3744059735`, while the official UI showed Upload after restart. Native review showed `ModUiState` displays Upload until `ResolveLocalModUploadPlan` returns an Update plan.
  - Superseded attempt: `20260614-0009` patched `ModManager.ResolveLocalModUploadPlan` directly; manual upload logs then showed `k_EResultTimeout` and Steam `workshop_log.txt` lacked a fresh `GetDetails request` before `SubmitItemUpdate`.
  - Current implementation: `20260614-0010` replaced the plan prefix with a display-only `ModData` constructor postfix so native Steam details resolution is preserved.
  - Build/test: `git diff --check`, Release build, and Release tests passed 2026-06-14.
  - Steam title official Mod UI smoke: `GAME-SMOKE/20260614-232339` logged `Workshop.LocalUploadPlan = verified. DTMAPI-generated local package Local.DTMAPI displays Update from workshop.json workshopId=3743016467; native Steam ResolveLocalModUploadPlan still owns upload execution`, `OfficialModUi=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`. The run aggregate is retained as failed only because `OfficialModUiScreenshotFile` reported false despite the referenced screenshot existing.
  - Live owner-account update click remains pending; validation should require a fresh Steam `GetDetails request` followed by upload `OK`, not `Timeout`.
- Regression cases: WORKSHOP-003

## Hook: Workshop.LocalUploadPlanBusyFallback

- Status: experimental; live upload pending
- Public surface: none; internal native official-local Workshop resolve queue guard.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.Config.SteamWorkshopUploader.ResolveUploadPlan(ModInfo, Action<WorkshopUploadPlan>)`
- Patch type: Harmony Prefix
- Why this point: native `ModManager.ResolveLocalModUploadPlan` marks one local package as resolving and relies on the uploader callback to clear the queue. Native `SteamWorkshopUploader.ResolveUploadPlan` can return without invoking that callback when the uploader is already busy, leaving later local Update clicks apparently inert. DTMAPI only completes the callback for DTMAPI-generated official-local packages with a nonzero `workshop.json` id and DTMAPI package markers.
- Failure behavior: non-DTMAPI local mods, zero ids, missing package markers, and normal non-busy resolution keep native behavior. Actual upload execution remains native `SteamWorkshopUploader.UploadMod`; this hook does not claim to solve Steam `k_EResultTimeout`.
- Evidence:
  - Release build/test passed with `DTMAPI.UnitTests: OK`; `git diff --check` passed with line-ending warnings only.
  - Steam title official Mod UI smoke `GAME-SMOKE/20260614-235540` logged `Workshop.LocalUploadPlanBusyFallback = experimental`, passed `OfficialModUi`, `ProcessExited`, and `NoFatalInstanceWindow`, and left no `DolocTown.exe` running. The aggregate is failed only because `OfficialModUiScreenshotFile` reported false while `Smoke.OfficialModUiScreenshot = verified` exists in the log.
  - 2026-06-15 follow-up keeps this prefix as busy-only. Non-busy Steam details-query stalls are tracked separately by `Workshop.LocalUploadPlanKnownIdFallback`.
- Regression cases: WORKSHOP-LOCAL-UPLOAD-BUSY-FALLBACK-20260614

## Hook: Workshop.LocalUploadPlanKnownIdFallback

- Status: verified
- Public surface: none; internal native official-local Workshop resolve watchdog.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.Config.SteamWorkshopUploader.ResolveUploadPlan(ModInfo, Action<WorkshopUploadPlan>)` plus `DTMAPI.GameBridge.DolocTown.Update`.
- Patch type: Harmony Postfix plus DTMAPI update watchdog.
- Why this point: latest manual evidence showed the official button entered `PendingUiState` and Steam emitted `GetDetails request 0xc/0xd`, but no `QuestionUiState`, `Update local mod`, `Update workshop mod`, or `Upload starting` appeared. That means the button click was received, but native `ResolveUploadPlan` did not invoke the callback that `ModManager` needs before it can show the confirm dialog or upload.
- Failure behavior: native Steam details resolution still runs first. DTMAPI only tracks DTMAPI-generated official-local packages with a nonzero `workshop.json` id and package markers. If the same native uploader callback is still active for the same workshop id after 4 seconds, DTMAPI clears the native pending callback/id and invokes the callback with a native `WorkshopUploadPlan(Update, workshopId)` so the official `ModManager` queue can continue. Non-DTMAPI packages, zero ids, missing markers, native callbacks that return in time, and cached native plans keep official behavior. Actual upload execution remains native `SteamWorkshopUploader.UploadMod`; this does not claim to solve `k_EResultTimeout`.
- Evidence:
  - Build/test: `tools/scripts/build.ps1 -Configuration Release` and `tools/scripts/test.ps1 -Configuration Release` passed 2026-06-15 with 0 warnings/errors and `DTMAPI.UnitTests: OK`.
  - `git diff --check` passed with line-ending warnings only.
  - Package/install: release Workshop packages rebuilt, installed to the game, and the DTMAPI official local upload package was synced from `dist\workshop-packages\DTMAPI` while preserving `workshop.json`.
  - Hash: `DTMAPI.GameBridge.DolocTown.dll` SHA256 `00AE49CB0F88D13B44207541E81DC6A4E87DDBE4BBB5C3FD64C6400A24C678CA` matches source, installed plugin, dist runtime package, and official local DTMAPI upload package.
  - Title official Mod UI smoke `GAME-SMOKE/20260615-010503` passed `OfficialModUi`, `ProcessExited`, and `NoFatalInstanceWindow`; aggregate failed only because `OfficialModUiScreenshotFile` reported false. Logs loaded `Workshop.LocalUploadPlanKnownIdFallback = experimental` and observed `watching` for `Local.DTMAPI` / `workshopId=3743016467`.
  - Pre-fix log line: `Player.log` at `2026-06-15 00:25` entered `PendingUiState` and returned to `HomePageUiState` with no `QuestionUiState` or upload log lines; Steam `workshop_log.txt` had `GetDetails request 0xc/0xd` and no matching `Upload starting`.
  - Manual owner-account update retest passed after reboot: Steam `workshop_log.txt` records `Upload finished ... : OK` for DTMAPI item `3743016467` at `2026-06-15 07:02:54-07:02:56` and MoreEquipmentSlots item `3744059735` at `2026-06-15 07:03:29-07:03:31`; later subscribe/download lines detected the updated cached manifests.
- Regression cases: WORKSHOP-LOCAL-UPLOAD-KNOWN-ID-WATCHDOG-20260615

## Hook: UI.TitleSettingsEntry

- Status: verified
- Public surface: title-page DTMAPI Settings button; config pages reached through `IUiHelper.OpenConfigPage`.
- Game build: 23465763 workshop
- Game method/type: `HomePageUiState` active-context detection with a reflected Unity UI Canvas. Blocking title-page states such as `ModUiState`, `GameDataUiState`, and confirmation/menu panels are detected first so the compact DTMAPI icon button only appears on the unobstructed title homepage.
- Patch type: reflection-created Unity UI Canvas plus EventSystem fallback, no official ModManager enable/disable override.
- Why this point: gives players a compact visible DTMAPI entry on the title homepage while leaving official mod enable/disable/order controls in the official path.
- Failure behavior: if Unity UI creation fails or the active page is not the unobstructed `HomePageUiState`, DTMAPI hides/recreates the canvas and keeps runtime ticks isolated from UI failures. As of 2026-06-23, the fallback EventSystem path does not force-create `InputSystemUIInputModule` while Unity Input System reports not initialized; it reuses any native EventSystem, falls back to `StandaloneInputModule` when available, or publishes `UI.TitleSettingsEventSystem=pending` for later retry instead of throwing during startup.
- Mods/tests depending on it: migrated config pages for `Yuuka.DTMAPI.ActionSpeed`, `Yuuka.DTMAPI.AutoFishing`, `Yuuka.DTMAPI.OneActionComplete`.
- Evidence:
  - Build: DTMAPI 0.1.13 local build/unit passed 2026-05-31
  - Save: title homepage and local slot 3 / index 2 return-to-title lifecycle
  - Log line: `DTMAPI title settings button visible on HomePageUiState.`, `Startup segment IconLoad ... result=loaded`, `Title settings button screenshot OK`, and `Smoke exercise TitleButtonLifecycle OK startupOpen=true, closed=true, saveLoaded=True, returnedContext=HomePageUiState, reopened=true`. The former `UI.TitleHomeMenuLayout` guard log is historical only and no longer emitted after `20260612-0017`.
  - 2026-06-10 lifecycle isolation log line: `GAME-SMOKE/20260610-012907` records `TitleButtonLifecycle=Passed`, `ReturnedToTitle hook dispatched.`, clean exit, no fatal popup, and no `Lifecycle callback failed` entries after returned-to-title cleanup/restore/runtime notify callbacks were wrapped independently.
  - 2026-06-23 source follow-up: player logs from old `0.5.2-alpha` showed startup `Input System not yet initialized` exceptions in the fallback path. Unit coverage verifies the title settings UI does not choose `InputSystemUIInputModule` while the Input System readiness probe is false; code review then fixed owned fallback EventSystem owner tracking so the fallback component is not mistaken for a separate native EventSystem. Game/title real-click smoke remains pending before this path can be promoted beyond source validation.
  - Screenshot/report: current compact icon and menu smoke `docs/debug/evidence/GAME-SMOKE/20260612-204350/DTMAPI-evidence/UI-004/20260612-204434/title-settings-button.png` and `title-settings-menu.png`; older position/localization screenshot `docs/debug/evidence/GAME-SMOKE/20260531-042239/DTMAPI-evidence/UI-004/20260531-042057/title-settings-button.png`; lifecycle smoke result `docs/debug/evidence/GAME-SMOKE/20260531-111831`; returned-title screenshot `docs/debug/evidence/GAME-SMOKE/20260531-111940/DTMAPI-evidence/UI-006/20260531-111940/title-settings-after-return.png`.
- Regression cases: UI-003, UI-004, UI-006

## Hook: UI.TitleSettingsMenu

- Status: verified
- Public surface: title-page DTMAPI Settings menu with Config, Mods, Status, Errors, Hooks, and Logs pages.
- Game build: 23465763 workshop
- Game method/type: DTMAPI reflected Unity UI Canvas opened from the title settings entry.
- Patch type: reflection-created Unity UI Canvas plus EventSystem fallback.
- Why this point: replaces the temporary F8/F10 overlay route with a title-screen menu that can edit DTMAPI mod config without taking over official mod management.
- Failure behavior: menu is closed when leaving `HomePageUiState`; ordinary mod updates continue to tick if UI rendering has a recoverable failure.
- Mods/tests depending on it: `DTMAPI.HookProbeMod`, `DTMAPI.ConfigMenuExample`, `Yuuka.DTMAPI.ActionSpeed`, `Yuuka.DTMAPI.AutoFishing`, `Yuuka.DTMAPI.OneActionComplete`.
- Evidence:
  - Build: DTMAPI 0.1.13 local build/unit passed 2026-05-31
  - Save: title homepage and local slot 3 / index 2 return-to-title lifecycle
  - Log line: `DTMAPI title settings button clicked.`, `Smoke automation opened DTMAPI title settings menu.`, `DTMAPI title settings menu opened.`, `Title settings menu screenshot OK`, and lifecycle reopen after returning to `HomePageUiState`.
  - Screenshot/report: title UI visual evidence `docs/debug/evidence/GAME-SMOKE/20260531-042239/DTMAPI-evidence/UI-004/20260531-042057/title-settings-menu.png`; lifecycle smoke result `docs/debug/evidence/GAME-SMOKE/20260531-111831`; logs/screenshots `docs/debug/evidence/GAME-SMOKE/20260531-111940`.
- Regression cases: UI-003, UI-004, UI-006, CONFIG-003

## Diagnostic: Smoke.ManagerStatusPage

- Status: verified
- Public surface: none; `run-game-smoke.ps1` Status page result-field evidence only.
- Game build: 23465763 workshop
- Game method/type: DTMAPI title settings UI opened from `HomePageUiState`, then `UiRuntimeService.OpenDtmApiStatusPage()` refreshes the internal Manager view model.
- Patch type: smoke harness automation and reflected Unity screenshot capture; no Harmony hook, public API, or ConfigMenu contract change.
- Why this point: validates that the first real Manager UI consumer is visible on the title page and that support-facing summary text can be captured independently from the older Config screenshot rotation.
- Failure behavior: if the Status page cannot open, the Manager model is unavailable, summary text is missing, or screenshot capture does not produce a file, `ManagerStatusPage`, `ManagerStatusSummaryText`, `ManagerStatusPageScreenshot`, or `ManagerStatusPageScreenshotFile` fails and the smoke run fails.
- Mods/tests depending on it: compact web audit package Manager Status evidence and future Manager UI page work.
- Evidence:
  - Build: 2026-06-11 `git diff --check`, PowerShell AST parse, Release build, and Release unit tests passed.
  - Save: local slot 3 / index 2.
  - Log line: DirectExe smoke `GAME-SMOKE/20260611-100827` logs `Smoke automation opened DTMAPI Manager Status page.`, `Manager Status summary text OK overall=ready; mods=loaded:16,blocked:0,disabled:0; diagnostics=errors:0,warnings:0; hooks=failed:0,missing:0; features=failed:0,degraded:0; report=ready; ...`, and `Manager Status page screenshot OK screenshot=...manager-status-page.png`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260611-100827`; copied title/menu/status screenshots exist under the smoke evidence package, while the source screenshot folder contains `title-settings-button.png`, `title-settings-menu.png`, `manager-status-page.png`, and `summary.txt`.
  - Severity model: branch `codex/refactor-manager-status-severity-model` keeps the same smoke status IDs while moving missing hook and degraded feature counts into first-class internal `ManagerSummary` fields; DirectExe Status smoke `GAME-SMOKE/20260611-101818` verified `ManagerStatusPage`, `ManagerStatusSummaryText`, `ManagerStatusPageScreenshot`, screenshot file existence, clean exit, and summary text with `hooks=failed:0,missing:0` and `features=failed:0,degraded:0`.
  - Final hardening: `GAME-SMOKE/20260611-103601` on final `Refactor` verifies `ManagerStatusPage=Passed`, `ManagerStatusSummaryText=Passed`, `ManagerStatusPageScreenshot=Passed`, `ManagerStatusPageScreenshotFile=Passed`, title/menu screenshot checks, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed` after export/refresh safety, dedicated Status smoke, severity model, and Logs report-state branches merged.
- Regression cases: MANAGER-STATUS-PAGE-SMOKE-20260611, MANAGER-STATUS-SEVERITY-MODEL-20260611, MANAGER-STATUS-HARDENING-FINAL-20260611, UI-003, UI-004

## Diagnostic: Smoke.ManagerMvpPages

- Status: verified
- Public surface: none; `run-game-smoke.ps1 -AutoOpenTitleSettingsManagerMvp` title Settings result-field evidence only.
- Game build: 23465763 workshop
- Game method/type: DTMAPI title settings UI opened from `HomePageUiState`, then `UiRuntimeService` refreshes the internal Manager model while smoke automation visits Status, Mods, Errors, Hooks, Features, and Logs.
- Patch type: smoke harness automation, internal Manager view-model UI rendering, report export, and reflected Unity screenshot capture; no Harmony hook, public API, gameplay feature, or ConfigMenu contract change.
- Why this point: validates the Manager MVP support loop as a real UI consumer instead of a design-only view model: summary, row pages, and Logs export can be checked from the title page without raw log parsing.
- Failure behavior: if any page cannot open, Status summary copy/fallback cannot produce text, Logs export does not reach `exported`, the exported report path does not match the refreshed snapshot, or Status/Logs screenshots are missing, the corresponding `Manager*` result field fails and the smoke run fails.
- Mods/tests depending on it: compact web audit package Manager MVP evidence and future Manager UI page slices.
- Evidence:
  - Build: 2026-06-11 Release build/test and PowerShell AST parse passed.
  - Save: title homepage for Manager MVP; local slot 3 / index 2 for HookProbe regression.
  - Log line: DirectExe smoke `GAME-SMOKE/20260611-112148` logs `Smoke automation opened DTMAPI Manager Mods page.`, `Errors page.`, `Hooks page.`, `Features page.`, `Logs page.`, `Manager Logs export button OK status=exported pathMatch=matched path=D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260611-112232.zip`, `Manager Logs export state OK export=exported; pathMatch=matched; snapshotReport=ready`, and `Manager Logs page screenshot OK screenshot=...manager-logs-page.png`.
  - Result fields: `GAME-SMOKE/20260611-112148` records `ManagerStatusPage=Passed`, `ManagerStatusSummaryText=Passed`, `ManagerStatusPageScreenshotFile=Passed`, `ManagerModsPage=Passed`, `ManagerErrorsPage=Passed`, `ManagerHooksPage=Passed`, `ManagerFeaturesPage=Passed`, `ManagerLogsPage=Passed`, `ManagerLogsExportButton=Passed`, `ManagerLogsExportStateText=Passed`, `ManagerLogsPageScreenshotFile=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
  - Runtime regression: `GAME-SMOKE/20260611-112402` records `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
  - Final `Refactor` evidence: `GAME-SMOKE/20260611-113018` records all Manager MVP page/export/screenshot result fields as `Passed`, including `ManagerLogsExportButton=Passed` and `ManagerLogsExportStateText=Passed`; logs show `Manager Logs export button OK status=exported pathMatch=matched path=D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260611-113103.zip`.
  - Final runtime regression: `GAME-SMOKE/20260611-113156` records `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
  - Developer Preview polish: `GAME-SMOKE/20260611-123852` records `ManagerStatusSummaryCopy=Passed`, all Manager Status/Mods/Errors/Hooks/Features/Logs page fields as `Passed`, Logs export/path-match fields as `Passed`, and clean process/fatal checks. The DTMAPI log records `Manager Status summary copy OK status=copy-unavailable`, proving the runtime-log fallback path without depending on OS clipboard availability. HookProbe regression `GAME-SMOKE/20260611-124008` records `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Regression cases: MANAGER-UI-MVP-PHASE1-20260611, MANAGER-UI-MVP-PHASE1-FINAL-20260611, MANAGER-UI-DEV-PREVIEW-POLISH-20260611, UI-003, UI-004

## Hook: UI.ConfigMenuAdvancedControls

- Status: experimental
- Public surface: `IDtmConfigMenuApi.AddInlineBoolNumberOption`, `AddInlineBoolBoolOption`, `AddColorPresetOption`, bool/text-option `isVisible`/`canEdit`, `IConfigMenuItem.IsVisible`, and `DtmColorPreset`.
- Game build: 23465763 workshop
- Game method/type: DTMAPI title settings reflected Unity UI menu only; no raw game type exposure.
- Patch type: config registry plus reflected Unity UI rendering.
- Why this point: migrated mods need native-feeling compact controls without hard-coding mod-specific UI in each mod.
- Failure behavior: unsupported controls stay inside the config menu page and do not affect game runtime hooks; save/cancel/reset still use the normal config transaction model.
- Mods/tests depending on it: `Yuuka.DTMAPI.ActionSpeed`, `Yuuka.DTMAPI.AutoFishing`, `Yuuka.DTMAPI.AnimalHusbandryProgress`. Historical archived sample: `DTMAPI.SecondMotorMod`.
- Evidence:
  - Build: DTMAPI 0.2.3 local build passed 2026-06-03 with 0 errors.
  - Save: title homepage.
  - Log line: `GAME-SMOKE/20260603-052444/DTMAPI-latest.log` records `Smoke.TitleSettingsConfigPageScreenshot.action-speed = verified`, `auto-fishing = verified`, `animal-husbandry-progress = verified`, and historical `second-motor = verified`; the 2026-06-15 cleanup removes archived SecondMotor from the current title config screenshot rotation.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260603-052444/DTMAPI-evidence/UI-004/20260603-052523/title-settings-config-action-speed.png` shows ActionSpeed inline bool+number rows plus same-row `自动装水`/`强化自动装水`; `title-settings-config-auto-fishing.png` shows same-row `自动完成小游戏`/`跳过小游戏`; `title-settings-config-animal-husbandry-progress.png` shows swatches without the right-side `Orange` label or non-Custom hex input; `title-settings-config-second-motor.png` is historical archived sample evidence only.
- Regression cases: CONFIG-008, CONFIG-009

## Hook: UI.DebugConsoleHost

- Status: experimental
- Public surface: `IDebugConsoleApi`, ordinary mod `DTMAPI.DebugConsoleMod`, and the in-save Unity Canvas debug console with source/category item browser plus time, movement, weather, and teleport controls.
- Game build: 23465763 workshop
- Game method/type: DTMAPI bootstrap Unity `Update` plus reflected Unity UI `Canvas`, `Button`, `Text`, `InputField`, and `EventTrigger`. Ordinary Y/Escape binding is registered by `DTMAPI.DebugConsoleMod`; the host consumes Y/Escape while open so gameplay hotkeys do not receive duplicate toggles. As of 2026-06-16, focused reflected `InputField` instances block Y-close so search text can contain `Y`, and item give uses Unity `Button` for left-click plus actual item-cell Unity `PointerDown` for right-click. As of 2026-06-17, a console opened by Y suppresses the closing Y edge until that physical key is released, because player logs showed the same Y press can otherwise open and immediately close before Canvas visibility. The old global `Mouse1` screen-rectangle hit-test route and the residual hover-based `Mouse1` fallback were removed because stale layout or hover state could target the wrong item. As of 2026-06-23, DebugConsole defers fallback `InputSystemUIInputModule` creation until Unity Input System is ready, records `UI.DebugConsoleEventSystem=pending` while waiting, scopes DTMAPI-owned fallback EventSystems to the open console lifecycle, and only creates/retries the reflected EventSystem when the console is open. Give-item actions publish bounded `DebugConsole.LastGive` breadcrumbs and `debug-console-last-give.txt` with item id, requested count, given count, right-click flag, and failure reason so crash packages preserve recent give context without per-frame logging. 0.2.9 keeps movement at `1x/2x/3x/4x`, expands the item page to 35 cells, extends source/category lists, and centers icons in the item cells. 0.3.1 adds an in-game smoke path that dispatches Y through the DTMAPI input event service and drives Escape/Y close through the same DebugConsole host state machine, avoiding flaky external key injection.
- Patch type: reflection-created native Unity UI host; no IMGUI/F8/F10 overlay route.
- Why this point: keeps the debug console out of the title screen and out of ordinary `BepInEx/plugins` mod placement, while letting a normal DTMAPI mod own the player-facing hotkey.
- Failure behavior: if UI construction fails, the host logs a runtime error and the ordinary mod keeps the game playable; if the Unity Input System is not initialized, EventSystem creation is marked pending for retry instead of throwing during startup. When the menu is open, DTMAPI blocks normal mod updates/hotkeys through the UI boundary and publishes modal state for native input-isolation prefixes.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseDebugConsole` and `-AutoExerciseDebugConsoleMouseGive`.
- Evidence:
  - Build: DTMAPI 0.3.1 Release build/unit passed 2026-06-06 with 0 errors; only restricted-network NU1900 vulnerability-index warnings occurred.
  - Save: local slot 3 / index 2.
  - Log line: 0.3.1 Y-console smoke `GAME-SMOKE/20260606-150210` logs `Smoke exercise DebugConsoleHotkey OK openCount=8, escapeCloseCount=1, yCloseCount=6, shortTaps=10, holdNoFlicker=True`, `InstantSave=true`, `DebugTeleportCsv=true` with 80 rows, `DebugTeleport=true`, `DebugInventory=true`, `DebugWeather=true`, `DebugTime=true`, and `DebugMovement=true`. 0.2.9 `GAME-SMOKE/20260606-051958` remains the 35-cell layout proof. 2026-06-16 unit coverage verifies the stale global `Mouse1` hit-test method/nested target type and hover fallback field are removed, and focused input fields gate Y-close while unfocused fields do not. Steam third-save smoke `GAME-SMOKE/20260616-124555` passed `DebugConsoleOpenY1`, `DebugConsoleCloseEscape`, `DebugConsoleOpenY2`, `DebugConsoleCloseY`, `DebugConsoleTenYShortTaps`, `DebugConsoleHoldYNoFlicker`, `DebugConsoleMouseGive`, and `DebugMovement`; logs show `Inventory debug give ... requested=1` and `requested=10` for the current clicked item cell, with right-click source `pointer-down`. 2026-06-17 player log `D:\下载\DTMAPI-logs\20260616-212130` is the negative proof for same-press Y open/close: Y dispatch and `opened` logs occurred 25 times, but each was followed by `closed reason=Y` before release.
  - Official UI: `GAME-SMOKE/20260601-135332` shows `selected=Local.DTMAPI_YKeyConsole, title=Y键控制台`; disabled-state evidence `GAME-SMOKE/20260601-135531` logs `Skipping DTMAPI.DebugConsoleMod` when `Local.DTMAPI_YKeyConsole.enabled=false`, with state restored from `OFFICIAL-ENABLE/20260601-135520`.
  - Screenshot/report: current 0.3.1 hotkey/save/teleport evidence `docs/debug/evidence/GAME-SMOKE/20260606-150210`; title/config UI evidence `docs/debug/evidence/GAME-SMOKE/20260606-150928`; 0.2.9 screenshot `D:\steam\steamapps\common\Doloc Town\DTMAPI\evidence\DEBUG-CONSOLE-UI\20260606-052037\debug-console.png` remains the 35-cell item page, extended filters, and centered-icons visual proof.
- Regression cases: DEBUGCONSOLE-001, OFFICIAL-001, OFFICIAL-004, INPUT-001, MANUALQA-025-Y-CONSOLE, MANUALQA-026-MINE-Y-CONSOLE, MANUALQA-028-README, MANUALQA-031-REGRESSION-NEWCONTENT

## Hook: Debug.AdvancedYConsoleApis

- Status: verified
- Public surface: `IDebugConsoleApi.BindAdvanced`, `IAdvancedDebugApi`, `AdvancedTimeAdvanceKind`, `TimeScaleDebugResult`, `DebugValueResult`, `DebugCommandResult`, `CropMaturityResult`, `CreativeModeState`, `CreativeModeResult`, `TechPointDebugOption`, `SpawnDebugOption`, and `SpawnDebugResult`.
- Game build: 23465763 workshop
- Game method/type: GameBridge reflection over native safe wrappers and tables, including `ArchiveDataHandle.PassTimeNoControl`, `DolocAPI.OnWakeUp`, `DolocAPI.SetTimeScale`, `DolocAPI.RevertTimeScale`, `DolocAPI.AddTechPoint`, the official money command/fallback current-money path, archive tech-tree collections, crop `DEBUG_SetLevel`, runtime `DolocConfig.Tables.TbItem`, official-local `dtmapi_creative_generator` JSON content, official `DolocAPI.Command_GenerateMonster`, and current-room resource/monster host probes.
- Patch type: GameBridge-owned reflection, Harmony Prefix/Postfix for creative cost/time hooks, official command wrapper for monster spawn, and reflected Unity UI buttons; no raw Unity objects or decompiled Doloc Town types are exposed through the public API.
- Why this point: the official console contains powerful commands, so DTMAPI exposes only explicit whitelisted actions with typed DTO results and keeps fragile native access inside `DTMAPI.GameBridge.DolocTown` / bootstrap UI host.
- Failure behavior: missing native methods, unavailable spawn hosts, unavailable generator item, or incomplete creative hooks return failed result DTOs and hook-status lines instead of executing arbitrary console/Lua commands. Creative mode applies/restores only bounded GameInitConfig flags and Harmony hooks while enabled.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, reflected Y-console advanced panel, smoke harness `-AutoExerciseAdvancedDebug`.
- Evidence:
  - Build: DTMAPI 0.3.1 Release build/unit passed 2026-06-06 with 0 errors; only restricted-network NU1900 vulnerability-index warnings occurred.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260606-172855` logs `Debug.CreativeMode = verified` with `ignoreMaterialCost=True`, `skipMoneyVerifyInShop=True`, `ignoreSpiritCost=True`, `canAffordMoneyIntMax=True`, `costEnergyNoChange=True`, `noTimeHookInstalled=True`, and `generatorAvailable=True`; `Debug.CreativeGeneratorGive = verified`; `Debug.SpawnMonster = verified` through official `Command_GenerateMonster`; `Debug.SpawnResource = verified`; `Smoke exercise AdvancedDebug OK`; and `Smoke.AdvancedDebug = verified`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260606-172855`; result has `AdvancedDebug=true`, `SaveLoaded=true`, `ProcessExited=true`, `NoFatalInstanceWindow=true`, and `ForcedClose=false`. Historical blocker baseline: `docs/debug/evidence/GAME-SMOKE/20260606-155802`.
- Regression cases: YCONSOLE-030-ADVANCED
- Pending related paths: none for the active 0.3.0 advanced Y-console closure. Chest Locator Enhancer and Strong Planting Gun are covered by their own hook records.

## Hook: UI.DebugConsoleInputIsolation

- Status: experimental
- Public surface: no new public API; this is GameBridge-owned native input isolation for the modal Y-console host.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.AgentControllerState.EnterUICheck`, `DolocTown.AgentControllerState.UseTool`, and `DolocTown.AgentControllerState.UseItem`, gated by `DolocTownHookCallbacks.DebugConsoleModalOpen`.
- Patch type: Harmony Prefix.
- Why this point: the debug console's reflected UI can consume DTMAPI hotkeys, but native gameplay input still reaches backpack/menu/tool/item state unless the bridge swallows these native entry points while the modal is open.
- Failure behavior: prefixes return normal native behavior when the console is closed; if a target is missing, hook status remains pending/failed and the goal cannot claim native input isolation.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseDebugConsoleMouseGive`.
- Evidence:
  - Build: DTMAPI 0.2.8 Release build/unit passed 2026-06-06 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260605-230442` logs `Hook status: UI.DebugConsoleInputIsolation = experimental. Patched native UI toggles and tool/item entry points`, then `Debug console native input isolation active: AgentControllerState.EnterUICheck suppressed while DTMAPI console is open.` and `UI.DebugConsoleInputIsolation = verified. Native backpack/menu/tool/item input is swallowed while the DTMAPI Y console is open.` 0.2.8 smoke `GAME-SMOKE/20260606-031919` rechecks the same console/mouse/debug API flow with clean exit.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260606-031919` and retained isolation baseline `docs/debug/evidence/GAME-SMOKE/20260605-230442`; both have `DebugConsoleMouseGive=true`, `ProcessExited=true`, and no fatal popup.
- Regression cases: MANUALQA-027-ROOT-CAUSE, MANUALQA-028-README, DEBUGCONSOLE-001, DEBUGITEMS-001

## Hook: Debug.InventoryWeatherTeleportApis

- Status: experimental
- Public surface: `IInventoryDebugApi`, `IWeatherDebugApi`, `ITeleportDebugApi`, and their DTOs in `DTMAPI.Abstractions`, including 0.2.3 `InventoryDebugQuery.SourceId` and `InventoryDebugPage.Sources`.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.Config.DolocConfig.Tables.TbItem`, `DolocAPI.QueryItemProto`, `DolocAPI.CanPlaceItem`, `DolocAPI.TryPlaceInBackpack`, `DolocAPI.CountItem`, `TbWeather`, `TimeArchiveData.GetWeatherInfoOfDay`, `ArchiveDataHandle.SetWeather`, `ArchiveDataHandle.PatchWeather`, `TbStation`, `StationInfo.MarkPointId/Title`, `TbMarkPoint`, `MarkPointInfo.RoomId/Position`, and `DolocAPI.DoTransport`.
- Patch type: GameBridge-owned reflection over native tables/methods. Public APIs expose stable DTOs, not raw decompiled game types.
- Why this point: debug menus need powerful game actions, but fragile Unity/Harmony/reflection details must stay inside `DTMAPI.GameBridge.DolocTown`.
- Failure behavior: inventory full, illegal items, missing native methods, weather parse failures, and rejected transport requests return result DTOs with failure reasons and write hook-status/log evidence; teleport exposes only a whitelist and records before/after snapshots.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseDebugInventory`, `-AutoExerciseDebugWeather`, `-AutoExerciseDebugTeleport`, and `-AutoExerciseDebugConsoleMouseGive`.
- Evidence:
  - Build: DTMAPI 0.2.8 Release build/unit passed 2026-06-06 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: 0.2.8 Y-console smoke `GAME-SMOKE/20260606-031919` logs `Smoke exercise DebugInventory OK` for base `wood` and official-local `crude_oil`, `Smoke exercise DebugWeather OK options=7 ... before=THUNDERSTORM, after=CLOUDY`, `Smoke exercise DebugTeleport OK destination=上游丘陵1-下端 ... changedRoom=True`, left mouse give OK, and right mouse give OK.
  - Screenshot/report: 0.2.8 evidence `docs/debug/evidence/GAME-SMOKE/20260606-031919`; 0.2.6 Y-console screenshot `docs/debug/evidence/GAME-SMOKE/20260605-181224/DTMAPI-evidence/DEBUG-CONSOLE-UI/20260605-181315/debug-console.png` shows localized/compact weather buttons and teleport rows.
- Regression cases: DEBUGITEMS-001, DEBUGWEATHER-001, DEBUGTELEPORT-001, MANUALQA-026-MINE-Y-CONSOLE, MANUALQA-028-README

## Hook: Debug.TimeMovementApis

- Status: experimental
- Public surface: `ITimeDebugApi`, `IMovementDebugApi`, `TimeDebugState`, `TimeSkipResult`, `MovementDebugState`, and `MovementSpeedResult`.
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.GlobalParameter.Hour2Min/Day2Hour/GameMinutes2Secs`, `ArchiveDataHandle.PassTimeNoControl`, `DolocAPI.OnWakeUp(false,true,false)`, `DolocAPI.agent.MotionAbility`, and native `MotionAbility.SetMoveScaler(float)`.
- Patch type: GameBridge-owned reflection over native time and player motion APIs. Public APIs expose DTOs, not raw game types.
- Why this point: debug time skipping and movement speed are powerful save-state/gameplay changes, so the ordinary mod only asks GameBridge for explicit experimental actions.
- Failure behavior: missing native time/motion members return failed result DTOs and hook-status messages. As of 2026-06-16, non-default movement speed is treated as a lightweight debug lease over `MotionAbility.SetMoveScaler`, reapplied at low frequency while active, and cleared on explicit reset, `SaveLoaded`, and `ReturnedToTitle`; reset clears future reapply state even if native `MotionAbility` is unavailable at that moment.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseDebugTime`, `-AutoExerciseDebugMovement`.
- Evidence:
  - Build: DTMAPI 0.2.8 Release build/unit passed 2026-06-06 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: 0.2.8 Y-console smoke `GAME-SMOKE/20260606-031919` logs debug time period transitions and `Smoke.DebugMovement = verified. levels=1x:12,2x:24,3x:36,4x:48, restored=True, finalSpeed=12`. 2026-06-16 unit coverage verifies `Debug.MovementLease` clears at SaveLoaded and missing-motion reset boundaries. Steam third-save smoke `GAME-SMOKE/20260616-124555` revalidates `DebugMovement=Passed`, `Smoke.DebugMovement = verified. levels=1x:12,2x:24,3x:36,4x:48, restored=True, finalSpeed=12`, `Debug.MovementLease=active` for non-default multipliers, and `Debug.MovementLease=disabled` after reset.
  - Screenshot/report: current time/movement smoke `docs/debug/evidence/GAME-SMOKE/20260616-124555`; process check says no `DolocTown.exe`. Earlier `GAME-SMOKE/20260601-140915` remains historical native-method proof, including the now-removed `0.5x` UI option.
- Regression cases: DEBUGTIME-001, DEBUGMOVE-001, MANUALQA-028-README

## Hook: ActionSpeed.ToolAnimation

- Status: verified for tool animation, core interaction/eat animation, bottled-water continuous drink, bottle fill, no-key auto-fill including 0.2.3 strong cooldown evidence, planting, harvest, resin, and vegetation slices; 2026-06-16 native-stage rebuild and interaction follow-up are build/unit verified and pending third-save manual smoke.
- Public surface: `IActionSpeedApi.Configure`, `IActionSpeedApi.GetStatus`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.AgentStateTool.OnEnter`, `DolocTown.AgentStateTool.OnExit`, `DolocTown.AgentStateInteract.OnEnter/OnExit`, `DolocTown.AgentStateEat.OnEnter`, `DolocTown.AgentControllerState.UseItemContinues(float dt)`, `DolocTown.AgentControllerState.InteractContinues(float dt)`, `AgentStateBase.OnExit`, native `ItemBottle.DrawWater*`, `ItemSeed.PlantSeed`, `ItemFertilizer.Fertilizer`, `ItemFilm.Protect`, `ResinCollector.OnInteract`, `AnimalRenderer.OnInteract`, `AffectorElectric.OnInteract` for `Sprinkler` / `FarmLight`, and GameBridge reflection over the body/tool/tool-collider/shared interaction animators owned by active states. `AnimalRenderer.OnInteract` is patched as a native-owner marker so overlapping scanner state, such as room connectors, does not hide the animal fondle owner from `AgentStateInteract.OnEnter` / `InteractContinues`; the marker survives generic state replacement exits and clears on interact exit/save/title/environment reset or expiry.
- Patch type: Harmony Postfix/Prefix plus scoped reflection writes; public API does not expose raw decompiled game types.
- Feature host owner: `ActionSpeedFeature` registers `IActionSpeedApi` through `ActionSpeedService`; `ActionSpeedHookBridge` owns ActionSpeed enter/continuous timer hook installation while `AgentStateLifecycleHookBridge` owns the shared `AgentStateTool.OnExit`, `AgentStateInteract.OnExit`, and `AgentStateBase.OnExit` patch points consumed by ActionSpeed restore and ActionCompletion fuel/feed. The original `ActionSpeed.ToolAnimation` / `ActionSpeed.InteractionAnimation` hook IDs are preserved; `InteractContinues` is included in the interaction hook readiness/status surface. `DolocTownGameBridge` owns the internal feature-status model for `Feature.ActionSpeed`, recording feature id, last operation, success/failure, failure count, and last error without adding public-like members to `IGameBridgeFeature`. `Smoke/Cases/ActionSpeedSmokeCase.cs` owns the `AutoExerciseActionSpeedTool`, `AutoExerciseActionSpeedConfigApply`, and `AutoExerciseActionSpeedInteraction` smoke case implementation; `SmokeHarness.cs` keeps scheduling and result-field ownership.
- Why this point: keeps fragile animation-speed writes inside `DTMAPI.GameBridge.DolocTown` while the migrated ActionSpeed mod supplies only a policy and player config.
- Failure behavior: if the hook is not installed or no enabled policy exists, ActionSpeed remains configured/pending and no animator speed or timer delta is changed. Speeds captured during `OnEnter` are restored on `OnExit`, smoke cleanup, `SaveLoaded`, `ReturnedToTitle`, and `EnvironmentReset`; continuous scaling only changes the `dt` passed to the game's own `UseItemContinues` / `InteractContinues` timers and does not call item logic directly. Fertilizer and crop-film repeated-attempt failure branches still accelerate only the native player action; native `PlantBasin.Fertilizer` / `PlantBasin.Protect` remains responsible for returning false and showing the failure message. Ready resin collectors accelerate through the one-shot `AgentStateInteract.OnEnter` animation for `ResinCollector.OnInteract -> BodyController._Interact -> Collect`; empty resin collectors are intentionally excluded from the generic `IGatherableEquipment` fallback. Animal fondle uses a short-lived `AnimalRenderer.OnInteract` marker that survives `AgentStateBase.OnExit` / `AgentStateTool.OnExit` during native state replacement, can be read by both `AgentStateInteract.OnEnter` and `InteractContinues`, and clears on `AgentStateInteract.OnExit`, save/title/environment reset, or expiry. Auto-fill continues to call native `ItemBottle.UseAsItem`; 0.2.3 normal/strong modes differ only by bridge cooldown and log `strong/cooldownSeconds` for smoke comparison.
- Mods/tests depending on it: `Yuuka.DTMAPI.ActionSpeed`.
- Evidence:
  - Build: DTMAPI 0.1.13 local build/unit passed 2026-05-31
  - Save: local slot 3 / index 2
  - Log line: `ActionSpeed tool animation speed applied by Yuuka.DTMAPI.ActionSpeed tool=old_pickaxe multiplier=3 animators=3.`, `Hook status: Smoke.ActionSpeedTool = verified. owner=Yuuka.DTMAPI.ActionSpeed, tool=old_pickaxe, multiplier=3, animators=3, samples=body:1->3;tool-renderer:1->3;tool-collider:1->3`, `ActionSpeed animator speeds restored reason=AgentStateTool.OnExit restored=3.`
  - Config apply log line: `Smoke exercise ActionSpeedConfigApply OK before=... multiplier=2 ... samples=body:1->2;tool-renderer:1->2;tool-collider:1->2; after=... multiplier=4 ... samples=body:1->4;tool-renderer:1->4;tool-collider:1->4`
  - Interaction hook log line: `Hook status: ActionSpeed.InteractionAnimation = experimental. ... bottled-water right-click continuous drink ... no-key ItemBottle.UseAsItem auto-fill ...`
  - Interaction gameplay log line: `Smoke exercise ActionSpeedInteraction OK ... eatDrink={item=can ... continuous=none}; bottledWaterRightClick={item=bottle_of_water ... continuousDelta=1}; bottleFillInWater={branch=InteractiveWater.IsInWater ...}; autoFillBottle={... behavior=AutoFillBottle ... applications=1}; ... pending=none`.
  - 0.2.3 strong auto-fill log line: `GAME-SMOKE/20260603-041950` records native `ItemBottle.UseAsItem` with `strong=True`, `cooldownSeconds=0.08`, `normalCooldownSeconds=0.1`, `strongCooldownSeconds=0.08`, and `inventoryChanged=True`.
  - 2026-06-09 post-merge `Refactor` log line: `GAME-SMOKE/20260609-134246` records `Feature.ActionSpeed = ready`, `ActionSpeed.ToolAnimation = verified`, `ActionSpeed.InteractionAnimation = experimental`, `Smoke.ActionSpeedTool = verified`, `Smoke.ActionSpeedConfigApply = verified`, `Smoke.ActionSpeedAutoFillBottle = verified`, and `Smoke.ActionSpeedInteraction = verified`; earlier branch-package split evidence remains `GAME-SMOKE/20260609-120153`.
  - 2026-06-09 lifecycle restore log line: `GAME-SMOKE/20260609-135903` records `Feature.ActionSpeed = ready` dispatch through `ReturnedToTitle` and `SaveLoaded`, `ActionSpeed SaveLoaded restore boundary OK slot=2`, unchanged `AgentStateTool.OnExit` / `AgentStateInteract.OnExit` / `AgentStateBase.OnExit` restore logs, and unchanged `Smoke.ActionSpeedTool`, `Smoke.ActionSpeedConfigApply`, `Smoke.ActionSpeedAutoFillBottle`, and `Smoke.ActionSpeedInteraction` status meanings.
  - 2026-06-09 smoke case-file split log line: `GAME-SMOKE/20260609-140738` records unchanged `SchemaVersion=2`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `Feature.ActionSpeed = ready`, `Smoke.ActionSpeedTool = verified`, `Smoke.ActionSpeedConfigApply = verified`, `Smoke.ActionSpeedAutoFillBottle = verified`, and `Smoke.ActionSpeedInteraction = verified`; moved ActionSpeed smoke method hash stayed `492089ca05ff0faabff397f074a3d92a4af433c448042937409d69a14b31215e`.
  - 2026-06-09 feature-status model log line: `GAME-SMOKE/20260609-141440` records `Feature.ActionSpeed = ready` with `Feature status: id=ActionSpeed, lastOperation=PublishHookStatuses/InstallHooks/Update/ReturnedToTitle/SaveLoaded/EnvironmentReset, success=True, failureCount=0, lastError=none`, while `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, and all existing ActionSpeed smoke status meanings remain unchanged.
  - 2026-06-09 shared lifecycle owner log line: `GAME-SMOKE/20260609-172903` records `ActionSpeed.ToolAnimation = verified`, `ActionSpeed.InteractionAnimation = experimental`, `Feature.ActionSpeed = ready`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, and unchanged restore logs through `AgentStateTool.OnExit`, `AgentStateInteract.OnExit`, and `AgentStateBase.OnExit`; report zip `docs/debug/evidence/GAME-SMOKE/20260609-172903.zip`.
  - 2026-06-10 diagnostics mod-status log line: `GAME-SMOKE/20260610-022543` records `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `Feature.ActionSpeed = ready`, `Smoke.DiagnosticsSnapshot = verified`, and diagnostics summary `loadedMods=14, mods=14, errors=0, warnings=0, hooks=63, features=4`; report zip `docs/debug/evidence/GAME-SMOKE/20260610-022543.zip`.
  - 2026-06-10 lifecycle isolation log line: `GAME-SMOKE/20260610-012242` records `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, unchanged `ActionSpeed.ToolAnimation` / `ActionSpeed.InteractionAnimation` meanings, clean exit, no fatal popup, and no `Lifecycle callback failed` entries after `AgentStateTool.OnExit`, `AgentStateInteract.OnExit`, and `AgentStateBase.OnExit` callbacks were wrapped independently.
  - 2026-06-10 feature-status publish throttle log line: `GAME-SMOKE/20260610-041156` records `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `Feature.ActionSpeed = ready`, `Smoke.DiagnosticsSnapshot = verified`, diagnostics summary `loadedMods=14, mods=14, errors=0, warnings=0, hooks=64, features=5`, and only five successful `Feature.<Id>` `Update` status lines across five features during the short smoke, proving heartbeat publication instead of every-frame success publication.
  - 2026-06-10 diagnostics status-code log line: `GAME-SMOKE/20260610-043830` records `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `Feature.ActionSpeed = ready`, `Smoke.DiagnosticsSnapshot = verified`, diagnostics summary `loadedMods=14, mods=14, modStatusCodes=loaded=14, errors=0, warnings=0, hooks=64, features=5`, and report zip pointer `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-043910.zip`.
  - 2026-06-16 native-stage rebuild build/unit line: update `20260616-0007` adds `AgentControllerState.InteractContinues(float dt)` for water-container/interact-held native timer scaling, logs native owners for bottle-fill and plant stages, and adds deterministic highest-multiplier provider selection. `tools/scripts/build.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK`; third-save game smoke/manual QA remains pending.
  - 2026-06-16 interaction follow-up build/unit line: update `20260616-0008` aligns plant-item target lookup with native `Item.SelectedEquipment`, classifies fertilizer/crop-film attempts including already-fertilized/fully covered failure branches, adds `AnimalRenderer.OnInteract` owner marking for room-connector scanner overlap with marker lifetime guarded across generic state-exit restore, `Sprinkler` / `FarmLight` switch, and ready `ResinCollector.OnInteract -> BodyController._Interact -> Collect` classifications, and keeps `IActionSpeedApi` unchanged. `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK`, including empty-resin fallback exclusion and animal marker lifetime coverage; third-save game smoke/manual QA remains pending.
  - Config screenshot: `docs/debug/evidence/GAME-SMOKE/20260603-052444/DTMAPI-evidence/UI-004/20260603-052523/title-settings-config-action-speed.png` shows six inline bool+number rows plus same-row `自动装水` and `强化自动装水`.
  - Screenshot/report: tool smoke result `docs/debug/evidence/GAME-SMOKE/20260531-044611`; config-apply smoke result `docs/debug/evidence/GAME-SMOKE/20260531-045330`; 0.2.1 interaction smoke result `docs/debug/evidence/GAME-SMOKE/20260602-004823`; 0.2.3 strong interaction smoke result `docs/debug/evidence/GAME-SMOKE/20260603-041950`; final title config screenshot smoke result `docs/debug/evidence/GAME-SMOKE/20260603-052444`; branch feature-host ActionSpeed smoke result `docs/debug/evidence/GAME-SMOKE/20260609-120153`; post-merge ActionSpeed smoke result `docs/debug/evidence/GAME-SMOKE/20260609-134246`; lifecycle restore ActionSpeed smoke result `docs/debug/evidence/GAME-SMOKE/20260609-135903`; smoke case-file split ActionSpeed smoke result `docs/debug/evidence/GAME-SMOKE/20260609-140738`; feature-status ActionSpeed smoke result `docs/debug/evidence/GAME-SMOKE/20260609-141440`; report zip `docs/debug/evidence/GAME-SMOKE/20260609-141440.zip`.
- Regression cases: ACTIONSPEED-001, ACTIONSPEED-002, CONFIG-007, CONFIG-008
- Pending related paths: Recast/minigame fishing remains tracked separately under `Fishing.Automation`.

## Hook: Actions.OneActionComplete

- Status: verified for resource-hit path, tree/ore/garbage/weeds wrong-tool matrix, fuel/feeder native consume/fill path, and vegetation/dandelion exception classification
- Public surface: `IActionCompletionApi.Configure`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.ToolCollider.HandleTools(Collider2D)` for resource hits, with native `ResourceFellData(resource,currentTool,hitPoint)` validation before applying final damage and native `DolocAPI.HasEnoughEnergyForUsingTool` / `DolocAPI.CostToolEnergy` charged for each extra hit; `DolocTown.AgentStateInteract.OnExit` for post-native fuel/feed completion using the selected `PowerGeneratorFuel` or `Feeder`; `DolocTown.VegetationRenderer.OnFell(ItemTool,Vector2)` / `Vegetation.CheckToolConstraints(ItemTool)` recorded as a native exception path which DTMAPI must not force-complete through `DungeonResourceRenderer`.
- Patch type: Harmony Postfix plus GameBridge reflection, no public raw game type exposure.
- Feature host owner: `ActionCompletionFeature` registers `IActionCompletionApi` through `ActionCompletionService`; shared `ToolColliderHitHookBridge` owns the `ToolCollider.HandleTools` Prefix/Postfix route, while `ActionCompletionHookBridge` consumes the shared postfix ready state and publishes the unchanged `Actions.OneActionComplete` / `Actions.OneActionFuelFeed` statuses. The fuel/feed completion callback observes the `AgentStateInteract.OnExit` patch owned by `AgentStateLifecycleHookBridge`, so ActionCompletion no longer depends on ActionSpeed's hook installation state while status text and hook IDs stay unchanged. `Smoke/Cases/ActionCompletionSmokeCase.cs` owns the OneAction resource-hit, wrong-tool, fuel/feed, and vegetation smoke case implementation; `SmokeHarness.cs` keeps scheduling and result-field ownership.
- Why this point: migrates one-action resource completion into GameBridge instead of ordinary mods owning broad Harmony patches, while preserving the game's resource/tool matching rules.
- Failure behavior: if the ToolCollider hook is not installed, policy can still be registered but gameplay resource completion remains disabled; if native validation reports a tool-type/tool-level mismatch or native energy checks reject an extra hit, DTMAPI logs the skip/partial completion and does not apply unpaid extra damage. Fuel/feed completion runs only after the game's native interaction callback and consumes extra items through `CostSelf` before calling native fill helpers. Vegetation/dandelion hits are not `DungeonResourceRenderer` resources, so DTMAPI records the native path and leaves wrong/correct tool behavior to `Vegetation.CheckToolConstraints`.
- Mods/tests depending on it: `Yuuka.DTMAPI.OneActionComplete`
- Evidence:
  - Build: DTMAPI 0.2.1 local build passed 2026-06-01 with 0 errors; earlier 0.1.13 build/unit passed 2026-05-31
  - Save: local slot 3 / index 2
  - Log line: `OneActionComplete SaveLoaded restore boundary OK slot=2`, `Smoke one-action resource-hit waiting: Current room has no rendered one-action resource; requested official main farm transition for smoke`, `One-action tool hook completed resource stone for Yuuka.DTMAPI.OneActionComplete damage=11.`, `Smoke exercise OneActionResourceHit OK owner=Yuuka.DTMAPI.OneActionComplete, resource=stone, tool=old_pickaxe`
  - Guard behavior: 0.1.13 logs native-validation skip reasons such as `tool-type-mismatch` and `tool-level-mismatch`; wrong-tool matrix smoke verified `Tree`, `Ore`, `Garbage`, and `Weeds` in the third save with unchanged health, `removed=False`, and `oneActionDelta=0`. Fuel/feed smoke verified `PowerGeneratorFuel` with `wood` and `Feeder` with `roughage_feed` through `Equipment.DecoratedInteract -> AgentStateInteract.OnExit`, native `CostSelf`, and native `AddFuel`/`AddFeeds`. Vegetation smoke records dandelion as `VegetationDandelion` using `Vegetation.CheckToolConstraints(ItemTool)`, not the `DungeonResourceRenderer` one-action path: wrong `old_pickaxe` is rejected, expected `old_sickle` removes through native `OnFell`, and both sides keep `oneActionDelta=0`. 0.2.1 recheck `GAME-SMOKE/20260601-135239` records energy accounting for resource completion: `nativeDamage=4, paidExtraHits=2/2, damage=6`.
  - 2026-06-09 post-merge `Refactor` feature split log line: `GAME-SMOKE/20260609-144458` records `Feature.ActionCompletion = ready` with `Feature status: id=ActionCompletion, lastOperation=PublishHookStatuses/InstallHooks/Update/ReturnedToTitle/SaveLoaded/EnvironmentReset, success=True, failureCount=0, lastError=none`, unchanged `Actions.OneActionComplete = verified` and `Actions.OneActionFuelFeed = verified` status meanings, plus `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`; report zip `docs/debug/evidence/GAME-SMOKE/20260609-144458.zip`.
  - 2026-06-09 smoke case-file split log line: `GAME-SMOKE/20260609-171936` records unchanged `SchemaVersion=2`, `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `Feature.ActionCompletion = ready`, `Smoke.OneActionResourceHit = verified`, `Smoke.OneActionWrongTool = verified`, `Smoke.OneActionFuelFeed = verified`, and `Smoke.OneActionVegetation = verified`; moved ActionCompletion smoke method hash stayed `9bdd9ed007fbb05061025e3209002ac9c542fafedcf1e2d6e97e31c78f3628d3`.
  - 2026-06-09 shared lifecycle owner log line: `GAME-SMOKE/20260609-172748` records `Feature.ActionCompletion = ready`, `Actions.OneActionFuelFeed = verified`, unchanged OneAction resource/wrong-tool/fuel-feed/vegetation status meanings, `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, clean exit, and no fatal popup; report zip `docs/debug/evidence/GAME-SMOKE/20260609-172748.zip`.
  - 2026-06-09 native helper extraction log line: `GAME-SMOKE/20260609-174124` records `Feature.ActionCompletion = ready`, unchanged `Actions.OneActionComplete = verified` and `Actions.OneActionFuelFeed = verified`, `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, clean exit, and no fatal popup after `ActionCompletionService` moved from the experimental bridge static helper wrappers to `GameBridgeNativeHelpers`; latest report pointer is `docs/debug/evidence/GAME-SMOKE/20260609-174124/latest-report.txt`.
  - 2026-06-10 lifecycle isolation log line: `GAME-SMOKE/20260610-012400` records `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, unchanged `Actions.OneActionComplete` / `Actions.OneActionFuelFeed` meanings, clean exit, no fatal popup, and no `Lifecycle callback failed` entries after `AgentStateInteract.OnExit` fuel/feed callback isolation.
  - 2026-06-10 shared ToolCollider owner log line: `GAME-SMOKE/20260610-140419` records `Feature.ActionCompletion = ready`, `Feature.OilCoalDrop = ready`, unchanged `Actions.OneActionComplete = verified` and `Actions.OneActionFuelFeed = verified`, `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed` after `ToolCollider.HandleTools` Prefix/Postfix installation moved to `ToolColliderHitHookBridge`.
  - 2026-06-10 callback-isolation log line: `GAME-SMOKE/20260610-163813` records `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed` after the shared ToolCollider postfix split ActionCompletion and OilCoalDrop into independent safe wrappers.
  - Screenshot/report: verified positive smoke result `docs/debug/evidence/GAME-SMOKE/20260531-032318`; post-fuel/feed-hook positive smoke result `docs/debug/evidence/GAME-SMOKE/20260531-125529`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-125613`; first wrong-tool negative smoke result `docs/debug/evidence/GAME-SMOKE/20260531-130824`; full wrong-tool matrix result `docs/debug/evidence/GAME-SMOKE/20260531-133212`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-133256`; fuel/feed smoke result `docs/debug/evidence/GAME-SMOKE/20260531-140335`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-140416`; final status-text recheck `docs/debug/evidence/GAME-SMOKE/20260531-140944`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-141026`; vegetation exception smoke result `docs/debug/evidence/GAME-SMOKE/20260531-160900`; collected logs `docs/debug/evidence/GAME-SMOKE/20260531-160943`.
- Regression cases: ONEACTION-001, ONEACTION-002, ONEACTION-003

## Hook: Fishing.Automation

- Status: experimental
- Public surface: `IFishingAutomationApi.Configure`, `IFishingAutomationApi.SetEnabled`
- Game build: 23762374 public C416D4 baseline
- Game method/type: phase observation over `AgentStateFishingReady.OnEnter/OnPlay/NextState`, `AgentStateFishingCast.OnEnter`, `AgentStateFishingWait.OnEnter`, `AgentStateFishingWait.OnPlay`, `AgentStateFishingPull.OnEnter/OnExit`, `FishingGameScrollBar.StartGame/UpdateGame/StopGame`, native `BodyController.UseFishRod`, selected quick-slot rod placement, fishable pool lookup, `AgentStateFishingWait.RollFish`, native `InvokeFishOnHookTip`, bite-ready Pull/Battle routing, scoped `DolocUserInput` getter overrides during `FishingGameScrollBar.UpdateGame`, Ready charge `RedSaw.CastTimer` extra tick, Ready target hold/release through `DolocUserInput.NormalUseToolInProgress`, ready/cast/pull body and fish-rod animator speed writes, `FishRodRenderer.CastHook` hook physics scaling, and `FishRodRenderer.Pull` / `PullCancel` duration scaling.
- Patch type: Harmony Postfix/Prefix for phase evidence plus GameBridge-owned native auto-cast, bite timing, bite-ready routing, native minigame input override, and Cast/Pull animation-duration scaling.
- Hook/API owner: `FishingAutomationFeature` registers `IFishingAutomationApi` through `FishingAutomationCompatibilityAdapter`; `FishingAutomationHookBridge` owns the existing Fishing hook targets and publishes `Fishing.Automation`. The bridge and static callbacks depend only on internal `IFishingHookRuntime`. First-party sessions select `FishingPrimitiveHookRuntime`; only compatibility `SetEnabled(true)` may construct `LegacyFishingAutomationService`, and the two modes are mutually exclusive. Internal first-party flow is routed through `FishingNativeStateCache`, `FishingHookRouter`, `FishingNativeAdapter`, `FishingInputOverride`, `FishingAnimationController`, and `FishingPrimitivesService`.
- Why this point: AutoFishing must enter the game's native fishing state machine; GameBridge owns Hook-fed native state, cold pool discovery, and cached native transactions while the product owns F6 policy, movement cancel, recast timing, and independent feature settings.
- Failure behavior: policy/state can be registered; if no fishable water or selected rod is available, DTMAPI logs the state and does not fake fish/item rewards. Player toasts remain limited to toggle and movement cancel. The product loop uses native `BodyController.UseFishRod`, Wait/Bite, visible or skipped native result routing, Pull collection, and timed recast. `InstantBite` only changes the native wait strategy; `SkipMiniGame` only selects the native skip-result transaction and preserves success/failure. Each first-party cast supplies an independent `0..1` charge target before synchronous Ready entry. Zero releases use input immediately for exact minimum power; native `AgentStateFishingReady.NextState` still waits for `fishing_ready` animation completion before Cast. FastAnimations now explicitly scales Ready body/rod `fishing_ready` animators and the post-backswing native charge timer, then Cast hook physics and Pull timing/animators; it never changes the requested charge target. Compatibility clients retain their reviewed Ready hold/release behavior through `FishingAutomationOptions.CastChargeRatio`, and an old two-component internal lease defaults Ready to `1`. Hook velocity scales by `m`, Rigidbody2D gravity by `m²`, Pull duration by `1/m`, and all borrowed Ready/Cast/Pull animator/physics values restore to exact snapshots on native and lifecycle exits. Save/title boundaries clear sessions, input overrides, charge state, cached native handles, diagnostics throttles, and smoke state without destroying native objects or unpatching the shared Harmony owner.
- Manual QA follow-up UI contract: the first-party AutoFishing product exposes a Gameplay-scoped `ToggleKey` (default/reset `F6`, capture accepts the next key, clear remains available), `InstantBite`, independent `CastChargeRatio` (`0..1`), `SkipMiniGame`, and `FastAnimations` with `AnimationMultiplier` (`1..4`). Each cast passes the charge target before native Ready entry; zero keeps exact minimum power while the native animation-complete gate preserves the Ready backswing. FastAnimations accelerates that visible Ready backswing and the charge timer as well as Cast/Pull, but does not change the charge target.
- Owner policy: first-party AutoFishing holds one non-preemptive internal primitive session and stores charge/animation/input state on that session and its leases; it does not create legacy option/state owners. Experimental compatibility uses one effective enabled `FishingAutomationOptions` owner and does not merge policies. Movement cancel remains AutoFishingMod product behavior. Future stable behavior still needs deliberate public arbitration before ordinary mods can rely on multi-owner semantics.
- Mods/tests depending on it: `Yuuka.DTMAPI.AutoFishing`
- Evidence:
  - 2026-07-11 allocation-counter/visible-reel telemetry correction: `docs/debug/evidence/GAME-SMOKE/20260711-065929` passed one natural visible-minigame fish and retained `visibleReelQueued=1`, `visibleReelConsumed=1`, retries/timeouts zero after runtime release, with all title roots zero. Short calibration `GAME-SMOKE/20260711-070116` proved Unity Mono's resolved allocation method is nonfunctional (`4096`-byte payload, counter `0 -> 0`, delta `0`), emitted nullable allocation/Gen0 fields, outer `RunStatus=Blocked`, and clean title/process exit. Historical `012851`/`024427` zero-allocation fields are withdrawn; their non-allocation invariants remain. The 500 ms post-consumption retry is unchanged and still needs future native reel/energy side-effect fault injection.
  - 2026-07-11 reliability/0.5.3 preview gate: visible-reel input is consumption-aware and retryable, primitive acquire requires the complete Hook set and rolls back every pre-commit attachment, and Ready/Cast/Pull animation plus Cast Hook physics use a typed cache after warm-up. Final fifth-save fish evidence is `docs/debug/evidence/GAME-SMOKE/20260711-011246` (natural visible minigame), `20260711-011805` (Instant+Skip), and `20260711-012735` (full-charge Fast x4). Bounded performance evidence is the completed JSON under `20260711-012851` for inactive/no-consumer and final passing run `20260711-024427` for enabled/no-rod plus native `HorizontalMoveFactor=-1` cancellation. All accepted routes report zero accessor failures/deltas in their scoped checks and full title cleanup. No 100/500 fish or general soak ran; `ISSUE-010` remains open.
  - 2026-07-10 primitive Hook-runtime Mono gate: `docs/debug/evidence/GAME-SMOKE/20260710-235813` passed `RunStatus`, `AutoFishingPhase`, `AutoFishingLifecycle`, `AutoFishingMonoGate`, and process exit after exactly one visible-minigame fish with InstantBite=true, SkipMiniGame=false, Fast x4, and charge 0. It recorded 25 accessor builds, zero rebuild/build/invocation failures, then zero callback runtime, session, leases, scheduler, native references, and transients after ReturnedToTitle. Full unit coverage verifies primitive-only construction, legacy mutual exclusion even for the same owner, throwing-subscriber release, and cross-environment `UseFishRod` delegate reuse. No soak was run.
  - 2026-07-10 hot-path/native-cache source evidence: `20260710-0006` makes `GetSnapshot()` a pure cached struct read; replaces first-party minigame, bite/reel, and movement access with cached typed delegates; leaves primitive-only legacy option/state counts at zero; gates repeated lifecycle success; and adds a disabled strict 0/100/500 probe. Release build/unit/allocation/script-parse checks pass. No game was launched, so Mono accessor behavior and the runtime matrix remain pending.
  - Build: DTMAPI 0.2.3 local build passed 2026-06-03 with 0 errors.
  - Save: AutoFishing phase smokes now require explicit local slot 5 / index 4; other retained historical evidence used local slot 3 / index 2.
- Log line: `Keybind Yuuka.DTMAPI.AutoFishing/Yuuka.DTMAPI.AutoFishing.Toggle pressed trigger=F6`, `AutoFishing automation enabled reason=hotkey F6`, `Fishing automation auto-cast invoked native BodyController.UseFishRod`, `Smoke.AutoFishingInstantBite = verified`, `Smoke.AutoFishingMiniGameSkip = verified ... autoHook=AgentStateFishingPull`, `Smoke.AutoFishingMiniGameComplete = verified ... behavior=AutoPlayVisibleMiniGame`, `Smoke.AutoFishingAnimationSpeed = verified ... behavior=FastCastHookPhysics|FastAnimation`, and `Smoke.AutoFishingCastCharge = verified ... target=0.5 ... input=release`.
  - 0.2.3 toast-policy partial smoke: `docs/debug/evidence/GAME-SMOKE/20260603-030142` verified `toastPolicy=0.2.3-suppressed-no-water-no-rod-cast`, `ProcessExited=true`, and no fatal popup.
  - 0.2.4 direct skip=false minigame smoke: failed attempt `docs/debug/evidence/GAME-SMOKE/20260603-172124` rolled `waste_plastic_bottle` and correctly did not create the native minigame; passing attempt `docs/debug/evidence/GAME-SMOKE/20260603-173435` logged `FishingGameScrollBar`, `autoHook=AgentStateFishingBattle`, `fish=loach`, `isFish=True`, `forceFishForSmoke=True`, `currentGameStatus=Success`, `visibleSeconds=0.76`, clean exit, and no fatal popup.
  - 2026-06-09 animator restore smoke: `docs/debug/evidence/GAME-SMOKE/20260609-170646` records `AutoFishingHotkey=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `Fishing automation animation speed applied ... phase=Pull multiplier=3 animators=2`, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2`; report zip `docs/debug/evidence/GAME-SMOKE/20260609-170646.zip`.
  - 2026-06-10 lifecycle isolation smoke: `docs/debug/evidence/GAME-SMOKE/20260610-012052` records `AutoFishingHotkey=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, clean exit, no fatal popup, and no `Lifecycle callback failed` entries. `AgentStateFishingPull.OnExit` now keeps animator-speed restore in a finally-equivalent path after cooldown notification.
  - 2026-06-10 feature split smoke: `docs/debug/evidence/GAME-SMOKE/20260610-170839` records `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, `AutoFishingHotkey=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, clean process/fatal checks, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2` after API/service/hook ownership moved out of `DolocTownExperimentalBridgeApi`.
  - 2026-06-10 service failure throttle smoke: `docs/debug/evidence/GAME-SMOKE/20260610-202047` records `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, clean process/fatal checks, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and no `FishingAutomation service failed` entries on the passing path. Unit coverage verifies repeated `FishingAutomation.MiniGame.Update` service failures record one diagnostics error and suppress further hook-status rewrites after `failureCount=3`.
    - 2026-06-10 runtime state reset smoke: `docs/debug/evidence/GAME-SMOKE/20260610-203301` records `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, reset logs for `ReturnedToTitle`, `SaveLoaded`, and `DolocAPI.SetEnvCamera`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, clean process/fatal checks, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2`. Unit coverage verifies the reset helper clears mini-game handles, phase cooldowns, service failure episodes, smoke overrides, and animator snapshots.
  - 2026-06-10 native helper dependency smoke: `docs/debug/evidence/GAME-SMOKE/20260610-204134` records `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, reset logs for `ReturnedToTitle`, `SaveLoaded`, and `DolocAPI.SetEnvCamera`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, clean process/fatal checks, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2` after `FishingAutomationService` switched to `GameBridgeNativeHelpers`.
  - Options contract review: `docs/reviews/api/2026/20260610-fishing-options-contract-review.md` records that `AutoRecast` and `RequireSelectedFishingRod` are currently normalized to `true`; no runtime behavior changed in that docs-only branch.
  - 2026-06-12 independent-scenario follow-up: `GAME-SMOKE/20260612-204934` verifies `AutoCastOnly`; `GAME-SMOKE/20260612-205040` verifies `InstantBiteOnly`; `GAME-SMOKE/20260612-205147` verifies `SkipOnly`; `GAME-SMOKE/20260612-205248` verifies `AutoCompleteOnly`; `GAME-SMOKE/20260612-210544` verifies `FastAnimationsOnly` after auto-cast reachable animator paths were added; `GAME-SMOKE/20260612-210646` verifies `CombinedSkip`; and `GAME-SMOKE/20260612-210744` verifies `CombinedComplete`. Each third-save scenario records clean process/fatal checks and `AutoFishingReportExport=Passed`.
  - 2026-06-13 user-feedback fix: `20260613-0001` Release build/test passed after `AgentStateFishingWait.OnEnter` began applying wait automation and `InstantBite` began invoking native `InvokeFishOnHookTip`; expected status details include `nativeTipInvoked=True` and `source=AgentStateFishingWait.OnEnter Postfix`. `20260613-0002` Release build/test passed after forced bite duration switched from DTMAPI's unconditional `100f` timeout to native `PullTiming`; expected status details include `hookDuration=<native PullTiming>`. `20260613-0003` Release build/test passed after forced `InstantBite` began resolving to `action=ReelNativeBite`, with fish expected to route to `autoHook=AgentStateFishingBattle`; third-save visual confirmation remains pending.
  - 2026-06-13 native-loop rewrite: `20260613-0005` changes the experimental DTO to native-stage strategy enums, rewrites the migrated mod to F6 default full loop plus only three player switches, rewrites smoke scenarios to use the fifth-save real pond fixture, and adds unit coverage for default strategies, missing-input-edge native reel mirroring, skip result preservation, and Cast/Pull-only animation speed. Release build/test passed. Fifth-save `DefaultLoop` smoke `docs/debug/evidence/GAME-SMOKE/20260613-094928` first proved the fixture gate by failing with `selected=DolocTown.ItemTool`; after the fixture was saved with `carbon_fishrod`, fifth-save smokes passed for `DefaultLoop` (`GAME-SMOKE/20260613-102412`), `InstantBite` (`20260613-103216`), `SkipMiniGame` (`20260613-103328`), `FastAnimations` (`20260613-103427`), `CombinedInstantSkip` (`20260613-103531`), and `CombinedInstantComplete` (`20260613-103621`). Final HookProbe/exit smoke `docs/debug/evidence/GAME-SMOKE/20260613-095209` and all listed scenario smokes exited cleanly with no residual `DolocTown.exe`.
  - 2026-06-13 fifth-save revalidation: after the fifth save was confirmed to hold `carbon_fishrod`, Release build/test and `git diff --check` passed again, and the latest fifth-save smokes passed for `DefaultLoop` (`GAME-SMOKE/20260613-112900`), `InstantBite` (`20260613-113127`), `SkipMiniGame` (`20260613-113240`), `FastAnimations` (`20260613-113654`), `CombinedInstantSkip` (`20260613-113824`), and `CombinedInstantComplete` (`20260613-113937`). Logs record real pool `freshwater_forest`, `BodyController.UseFishRod`, native `NativeBiteReady`, `SkipMiniGameNativeResult -> AgentStateFishingPull` for skip routes, `FishingGameScrollBar.UpdateGame -> Success` for completion routes, Cast/Pull fast-animation samples, next AutoCast loop evidence, and clean process/fatal checks.
  - 2026-06-13 native minigame input and animation multiplier follow-up: `GAME-SMOKE/20260613-153632` verifies `AutoPlayVisibleMiniGame` with `stableHoldFrames=82`, `releaseFrames=49`, and no delayed direct-success evidence; user manual QA confirmed red/green/yellow handling succeeds. `GAME-SMOKE/20260613-153850` is retained as the rejected old-config path where missing `AnimationMultiplier` migrated to `1`; `GAME-SMOKE/20260613-154252` verifies Cast/Pull animator samples and `FishRodRenderer.Pull` duration, but is incomplete for visible cast speed because it did not observe hook physics. `GAME-SMOKE/20260613-160354` verifies `FastCastHookPhysics` with hook `Velocity` `18.55,29.68 -> 55.65,89.04`, hook `gravityScale` `12 -> 108`, Pull duration `0.147->0.049`, clean process/fatal checks, hook-physics restore, and report `dtmapi-report-20260613-160443.zip`. `GAME-SMOKE/20260613-163257` is retained as a smoke-harness false negative where Ready charge evidence existed but the old gate sampled a later Cast/Pull summary. Final Ready/Cast/Pull re-smoke `GAME-SMOKE/20260613-163812` verifies `FastReadyCharge` from `AgentStateFishingReady.OnPlay` (`castTimer.Progress:0->0.04;extraDt=0.04;powerBar.updated=2`), `FastCastHookPhysics` with hook `Velocity` `22.366,35.786 -> 67.098,107.357`, hook `gravityScale` `12 -> 108`, Pull duration `0.117->0.039`, `AutoFishingAnimationSpeed=Passed`, `AutoFishingMiniGameComplete=Passed`, final `readyCharge=True, fastAnimation=True`, clean process/fatal checks, and report `dtmapi-report-20260613-163900.zip`.
  - 2026-06-13 cast charge setting follow-up: `GAME-SMOKE/20260613-170514` is retained as a false positive because PowerShell integer `[Math]::Min/Max` overloads truncated the requested `0.5` ratio to `0`; `GAME-SMOKE/20260613-171104` is retained as the stricter-gate failure that exposed that clamp. Final fifth-save `FastAnimations` smoke `GAME-SMOKE/20260613-171405` verifies `AutoFishingCastCharge=Passed`, `target=0.5`, release at `progress=0.54`, Ready speed samples up to `castTimer.Progress:0.44->0.48`, `FastCastHookPhysics`, Pull duration scaling, final `readyCharge=True, fastAnimation=True`, clean process/fatal checks, and report `dtmapi-report-20260613-171455.zip`.
  - 2026-06-13 InstantBite energy-cost follow-up: unit `FishingAutomationInstantBiteDefersReelUntilWaitPlay` verifies `AgentStateFishingWait.OnEnter` prepares an instant bite without overwriting Battle/Pull and without `DolocAPI.CostEnergy`, then the following `Wait.OnPlay` advances to `AgentStateFishingBattle` and calls native fishing energy cost exactly once. Fifth-save in-game energy delta remains pending.
  - 2026-06-28 diagnostic log throttle: source change adds a separate AutoFishing success-diagnostic sampler for repeated `Smoke.AutoFishing*` success/experimental statuses and routine runtime logs. Unit coverage verifies repeated Ready-charge success evidence stops rewriting hook status after the first three samples while status changes still publish immediately. Long-run game soak remains pending.
  - 2026-07-08 lifecycle/boundary split evidence: inactive/no-consumer smoke `docs/debug/evidence/GAME-SMOKE/20260708-132338` passed with `Fishing.Automation=inactive/no-consumer`, no fishing hook requirement in lifecycle hook install signals, and `autoFishingRecords=0`. Fifth-save product smokes passed for `DefaultLoop` (`20260708-143733`), `InstantBite` (`20260708-143905`), `SkipMiniGame` (`20260708-144029`), and no-charge `FastAnimations` (`20260708-144556`). Retained failed `FastAnimations` `20260708-144151` documents the fixed smoke false-positive/no-real-cast class and Ready charge no-charge guard. Soak `20260708-144724` passed `soakLoops=8/8`, `autoCast=0->10`, `miniGameComplete=0->9`, and final ledger `recordCount=6`, `autoFishingRecords=4`, `snapshotBuilds=51`.
  - 2026-06-10 smoke case split: `docs/debug/evidence/GAME-SMOKE/20260610-205153` records `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, reset logs for `ReturnedToTitle`, `SaveLoaded`, and `DolocAPI.SetEnvCamera`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, clean process/fatal checks, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2` after only moving the smoke body into `Smoke/Cases/AutoFishingSmokeCase.cs`.
  - 2026-06-10 final hardening smoke: `docs/debug/evidence/GAME-SMOKE/20260610-205841` records `Feature.FishingAutomation = ready`, `Fishing.Automation = experimental`, reset logs for `ReturnedToTitle`, `SaveLoaded`, and `DolocAPI.SetEnvCamera`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `Smoke.AutoFishingAutoCast = verified`, `Smoke.AutoFishingPhase = verified`, `Smoke.AutoFishingMiniGameComplete = verified`, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2` after all FishingAutomation hardening branches were merged back to `Refactor`. Its `latest-report.txt` still points to stale `dtmapi-report-20260610-171030.zip`, so that report is not cited as fresh evidence.
  - 2026-06-10 lifecycle ownership cleanup smoke: `docs/debug/evidence/GAME-SMOKE/20260610-220230` records `Feature.FishingAutomation = ready` through `ReturnedToTitle`, `SaveLoaded`, and `EnvironmentReset`, unchanged `Fishing.Automation = experimental`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, clean process/fatal checks, unchanged `Smoke.AutoFishingAutoCast/Phase/MiniGameComplete` verified statuses, and `Experimental animator speeds restored reason=AgentStateFishingPull.OnExit restored=2`. The log has no `SaveLoaded.RestoreExperimentalAnimatorSpeeds` or `ReturnedToTitle.RestoreExperimentalAnimatorSpeeds` callback keys after save/title cleanup moved fully to `FishingAutomationFeature`.
  - 2026-06-10 service failure recovery smoke: `docs/debug/evidence/GAME-SMOKE/20260610-220835` records `Feature.FishingAutomation = ready`, unchanged `Fishing.Automation = experimental`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, clean process/fatal checks, unchanged AutoFishing smoke verified statuses, and no `Fishing automation service failed` / repeated / throttled service failure logs on the passing path. Unit coverage verifies a repeated `FishingAutomation.MiniGame.Update` failure episode clears after three stable successes and the next failure starts a fresh diagnostics episode.
  - 2026-06-10 fresh report export smoke: `docs/debug/evidence/GAME-SMOKE/20260610-221703` records `Feature.FishingAutomation = ready`, unchanged `Fishing.Automation = experimental`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`. The smoke now exports a fresh diagnostics report on success; `latest-report.txt` points to `dtmapi-report-20260610-221744.zip`, the file exists, and `Smoke.DiagnosticsSnapshot = verified` records matching `LatestReportPath`.
  - 2026-06-10 final Refactor fresh report smoke: `docs/debug/evidence/GAME-SMOKE/20260610-223354` records the same AutoFishing result after all follow-up branches merged to `Refactor`: `RunStatus=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`. `latest-report.txt` points to existing report `dtmapi-report-20260610-223436.zip`, and `Smoke.DiagnosticsSnapshot = verified` records `latestReport=D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-223436.zip`.
  - 2026-06-11 report-export result-field smoke: `docs/debug/evidence/GAME-SMOKE/20260611-000932` records the independent harness field `AutoFishingReportExport=Passed` alongside unchanged behavior fields `AutoFishingPhase=Passed` and `AutoFishingMiniGameComplete=Passed`; `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`. `latest-report.txt` points to existing report `dtmapi-report-20260611-001013.zip`, and `Smoke.DiagnosticsSnapshot = verified` records matching `latestReport`.
  - 2026-06-11 mid/long final Refactor smoke: `docs/debug/evidence/GAME-SMOKE/20260611-002930` records `RunStatus=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `AutoFishingReportExport=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed` after the Fishing docs/current-state, report-export field, API clarity, Manager view-model skeleton, and Camera gate refresh branches merged to `Refactor`. `latest-report.txt` points to existing report `dtmapi-report-20260611-003012.zip`, and `Smoke.DiagnosticsSnapshot = verified` records matching `latestReport`.
  - 2026-06-11 Manual QA Batch 1: config/UI wording was aligned with the current experimental service contract. Smoke `GAME-SMOKE/20260611-214536` records `RunStatus=Passed`, `AutoFishingMiniGameComplete=Passed`, `AutoFishingReportExport=Passed`, `DiagnosticsReportExport=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`; report `dtmapi-report-20260611-214620.zip`.
  - Native responsibility review: `docs/reviews/api/2026/20260610-fishing-native-responsibility.md` maps Ready/Cast/Wait/MiniGame/Pull/Cooldown owners and records that phase observation, native auto-cast, wait-phase intervention, minigame completion, movement cancel, fast-animation restore, and save/title cleanup have different ownership boundaries. `IFishingAutomationApi` remains Experimental, and a future feature split must preserve existing hook/status IDs while separating observation hooks from intervention callbacks.
  - Config screenshot: `docs/debug/evidence/GAME-SMOKE/20260603-052444/DTMAPI-evidence/UI-004/20260603-052523/title-settings-config-auto-fishing.png` shows same-row `自动完成小游戏` and `跳过小游戏`.
  - Screenshot/report: 0.2.3 movement/skip smoke `docs/debug/evidence/GAME-SMOKE/20260603-042437`; 0.2.4 skip=false minigame smoke `docs/debug/evidence/GAME-SMOKE/20260603-173435`; title config screenshot smoke `docs/debug/evidence/GAME-SMOKE/20260603-052444`; final old auto-cast/wait smoke `docs/debug/evidence/GAME-SMOKE/20260602-015720`; earlier wait-phase evidence retained under `GAME-SMOKE/20260531-035217` and external F6 evidence under `GAME-SMOKE/20260531-112959`.
- Regression cases: AUTOFISHING-HOOK-RUNTIME-MONO-GATE-20260710, AUTOFISHING-HOT-PATH-NATIVE-CACHE-20260710, TITLE-PAUSE-AUTOFISHING-ROOTFIX-20260613, INSTALL-AUTOFISHING-UI-DIAGNOSTICS-20260613, FISHING-FOLLOWUP-WEB-AUDIT-20260610, AUTOFISHING-SMOKE-REPORT-EXPORT-20260610, FISHING-SERVICE-FAILURE-RECOVERY-20260610, FISHING-LIFECYCLE-OWNERSHIP-20260610, FISHING-HARDENING-FOLLOWUP-20260610, AUTOFISHING-SMOKE-CASE-SPLIT-20260610, FISHING-OPTIONS-CONTRACT-REVIEW-20260610, FISHING-NATIVE-HELPER-DEPENDENCY-20260610, FISHING-RUNTIME-STATE-RESET-20260610, FISHING-SERVICE-FAILURE-THROTTLE-20260610, FISHINGAUTOMATION-FEATURE-SPLIT-20260610, FISHING-NATIVE-RESPONSIBILITY-REVIEW-20260610, AUTOFISH-001, INPUT-004, SMOKE-002, FISHING-ANIMATOR-RESTORE-20260609

## Hook: Items.FishRoeTooltip

- Status: verified
- Public surface: `IItemTooltipApi.ConfigureFishRoeProvider`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.Item.get_title`, `DolocTown.Item.get_description`, `DolocTown.Item.GetDetailInfo`, `DolocTown.ItemFishRoe.fishName` identity reader, and native `DolocAPI.QueryItemProto(fishId).Title` fallback for fish display names.
- Patch type: Harmony Postfix plus GameBridge reflection.
- Hook owner: `FishRoeTooltipFeature` / `FishRoeTooltipHookBridge`; API/service owner: `FishRoeTooltipService`.
- Why this point: FishBreedingAssistant provides lookup data while GameBridge owns item identity and tooltip rendering fragility. The shareable public FishBreedingAssistant lookup source is a placeholder, so GameBridge must not depend on private generated tables to recover `鱼卵 (鱼名称)`. The old `DolocTownExperimentalBridgeApi` no longer implements `IItemTooltipApi`.
- Failure behavior: lookup provider can be registered; if a provider lookup misses, GameBridge falls back to native `DolocAPI.QueryItemProto(fishId).Title` and logs one native lookup result/miss per fish id. If item hooks do not install or native lookup also fails, no tooltip text is changed and diagnostics stay pending/failed.
- Mods/tests depending on it: `Yuuka.DTMAPI.FishBreedingAssistant`
- Evidence:
  - Build: DTMAPI 0.2.1 local build passed 2026-06-01 with 0 errors; earlier 0.1.10 build/unit passed 2026-05-30; feature split branch Release build/test passed 2026-06-09.
  - Save: local slot 3 / index 2
  - Log line: `HookProbe HookStatusChanged OK Items.FishRoeTooltip=experimental`, `Smoke exercise FishRoeTooltip OK item=fish_roe title=鱼卵 (鱼) detail=Hatches: 鱼; Incubate: 4 小时; Grow: 6 小时`
  - 0.2.1 player-facing change: `Yuuka.DTMAPI.FishBreedingAssistant` now registers title decoration only; the old details toggle is removed from config and default options set `LabelFishRoeDetails=false`.
  - 2026-06-09 feature split smoke: `docs/debug/evidence/GAME-SMOKE/20260609-181533` records `ExperimentalHooks=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `Items.FishRoeTooltip = verified`, `Feature.FishRoeTooltip = ready`, `Smoke.FishRoeTooltip = verified`, `Smoke.AnimalViewerRendering = verified`, and `Smoke.ExperimentalHookExercise = verified`; the smoke harness registered a smoke-only fallback provider because the public FishBreedingAssistant lookup source is a placeholder and the local `Yuuka.DTMAPI.FishBreedingAssistant` config was disabled.
  - 2026-06-10 smoke case-file split: `docs/debug/evidence/GAME-SMOKE/20260610-021014` records unchanged `ExperimentalHooks=Passed`, `Items.FishRoeTooltip = verified`, `Feature.FishRoeTooltip = ready`, `Smoke.FishRoeTooltip = verified`, `Smoke.AnimalViewerRendering = verified`, and `Smoke.ExperimentalHookExercise = verified. FishRoeTooltip=True, AnimalViewerRendering=True.` after moving only the FishRoe smoke case body to `Smoke/Cases/FishRoeTooltipSmokeCase.cs`; report/evidence zip `docs/debug/evidence/GAME-SMOKE/20260610-021014.zip`.
  - 2026-06-13 native title fallback: `docs/debug/evidence/GAME-SMOKE/20260613-201326` records `RunStatus=Passed`, `SaveLoaded=Passed`, `ExperimentalHooks=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed` after removing the smoke-only fallback provider. Logs show `Fish roe native title lookup resolved fish -> 鱼`, `Smoke exercise FishRoeTooltip OK item=fish_roe title=鱼卵 (鱼) detail=`, `Smoke.FishRoeTooltip = verified`, and `Smoke.ExperimentalHookExercise = verified. FishRoeTooltip=True, AnimalViewerRendering=True.` Retained `GAME-SMOKE/20260613-200911` is a too-short `AutoExitAfterSeconds=20` harness timing failure with clean process/fatal checks.
  - Retained rejected precondition smoke: `docs/debug/evidence/GAME-SMOKE/20260609-180405` reached `Items.FishRoeTooltip = verified` and `Feature.FishRoeTooltip = ready`, but failed `Smoke.FishRoeTooltip` because no enabled public provider lookup produced decoration before the smoke-only fallback was added.
  - Screenshot/report: `docs/debug/evidence/HOOK-PROBE/20260530-150808`
- Regression cases: FISHROE-NATIVE-TITLE-FALLBACK-20260613, FISHROE-001, FISHROE-TOOLTIP-FEATURE-SPLIT-20260609, FISHROE-SMOKE-CASE-SPLIT-20260610

## Hook: Animals.ViewerRendering

- Status: verified
- Public surface: `IAnimalViewerApi.ConfigureSpecialProduceProgress`
- Game build: 23465763 workshop
- Game method/type: `DolocTown.UI.AnimalFullInfoData(Animal)` constructor, `DolocTown.UI.AnimalViewer.Show`, `DolocTown.UI.AnimalPanel.RefreshViewer`, native `ProgressBar` cloning, `Animal.husbandryValues`, `Animal.protoName`, and `DolocTown.Config.DolocConfig.Tables.TbHusbandry` enumeration/threshold lookup.
- Patch type: `AnimalViewerFeature`/`AnimalViewerHookBridge` owned Harmony constructor/Viewer/Panel Postfix plus `AnimalViewer.Show` Prefix stale-row cleanup, cached GameBridge reflection, and smoke-only official `AnimalPanelUiState` open path. The current hidden-produce path clears old cloned rows before native show, creates new independent cloned native progress rows inactive, disables localization-style components on those clones before first activation, pre-fills text while inactive, activates, then refreshes cloned text again in the same callback to reduce the historical first-frame `心情` flicker risk while keeping native mood/state data intact. As of 2026-06-20, SaveLoaded/ReturnedToTitle remain destructive clone-session boundaries, while high-frequency EnvironmentReset performs non-destructive validation/refresh and only clears if the observed clone/session is already invalid; parent scanning still removes `DTMAPI.AnimalProduceProgress.*` leftovers if render fails before a clone is registered.
- Why this point: AnimalHusbandryProgress stays an event/config mod while GameBridge owns private animal viewer data extraction and UI extension.
- Failure behavior: display policy can be registered; if viewer hooks do not install, no progress text is added and diagnostics stay pending/failed. The current path records whether hidden-produce rows were prefilled, independent, whether mood/state fields were overridden, how many localization components were disabled, whether the same-callback first-frame text guard observed a mood-title hit, and whether clone-session lifecycle reset/cleanup happened. Manual repeated animal-switch and long-session confirmation remain required before treating disappearance/flicker as fully solved.
- Mods/tests depending on it: `Yuuka.DTMAPI.AnimalHusbandryProgress`
- Evidence:
  - Build: DTMAPI 0.3.1 Release build/unit passed 2026-06-06 with 0 errors; earlier 0.2.3 build passed 2026-06-03
  - Save: local slot 3 / index 2
  - Log line: 2026-06-10 feature split smoke `GAME-SMOKE/20260610-103119` logs `Feature.AnimalViewer = ready`, `Animals.ViewerRendering = verified`, `Smoke.AnimalViewerProgressUi = verified. independent cloned ProgressBar prefilled rows=1, primary=羊毛脂 0/100, moodOverride=False, stateDescriptionOverride=False`, `Smoke.AnimalPanelUi = verified`, and `Smoke.AnimalViewerUi = verified`.
  - Smoke case split: `GAME-SMOKE/20260610-123042` logs the same `Feature.AnimalViewer = ready`, `Animals.ViewerRendering = verified`, `Smoke.AnimalViewerProgressUi = verified`, `Smoke.AnimalPanelUi = verified`, and `Smoke.AnimalViewerUi = verified` statuses after moving only the AnimalPanel/AnimalViewer smoke helpers into `Smoke/Cases/AnimalViewerSmokeCase.cs`; report `dtmapi-report-20260610-123118.zip`.
  - 2026-06-11 Manual QA Batch 1: `Smoke.AnimalViewerFirstFrameFlickerGuard` was added as a smoke/debug status for same-callback cloned-text validation after localization components are disabled. Smoke `GAME-SMOKE/20260611-214845` records `AnimalViewerUi=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, clean process/fatal checks, report `dtmapi-report-20260611-214922.zip`, and Animal summary `localizationDisabled=1`, `firstFrameGuard=sameCallbackTextCheck ... moodTitleHits=0`; manual repeated-switch confirmation remained pending at that point.
  - 2026-06-12 bottom-layer implementation: the prefix now only clears stale cloned rows and the postfix owns inactive-prefill-activate. Smoke `GAME-SMOKE/20260612-170819` passes `AnimalViewerUi`, `HookProbe`, `SaveLoaded`, clean process/fatal checks, and logs repeated `AnimalPanel.RefreshViewer` / `Select` with `Smoke.AnimalViewerRepeatedSwitchFirstFrame=verified`, `Smoke.AnimalViewerFirstFrameFlickerGuard=verified`, and `moodTitleHits=0`; screenshots/summary are under `ANIMAL-001/20260612-170858`.
  - 2026-06-16 lifecycle follow-up: unit coverage verified SaveLoaded/ReturnedToTitle/EnvironmentReset reset behavior at that point. Steam third-save smoke `GAME-SMOKE/20260616-120817` passed `AnimalViewerUi`, `HookProbe`, `SaveLoaded`, clean process/fatal checks, and logs `Animals.ViewerRenderingLifecycle=reset`, `Animals.ViewerRendering=verified`, `Smoke.AnimalViewerProgressUi=verified`, `Smoke.AnimalPanelUi=verified`, and `Smoke.AnimalViewerUi=verified`. The later 2026-06-20 lifecycle safety pass changed high-frequency EnvironmentReset to non-destructive validation and kept SaveLoaded/ReturnedToTitle as destructive boundaries; game validation for the new long-run behavior remains pending.
  - Earlier 0.3.1 Animal-only smoke `GAME-SMOKE/20260606-150721` logs the same independent cloned progress-row path and remains retained as pre-feature-host evidence.
  - Historical note: 0.2.7 `single-pass native moodBar` evidence is retained as the rejected mood-row strategy; 0.2.3 cloned-row evidence remains historical for the original UI extension path.
  - Screenshot/report: current feature-host evidence `docs/debug/evidence/GAME-SMOKE/20260610-103119`; report pointer `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-103155.zip`; config swatch/Custom screenshot evidence `docs/debug/evidence/GAME-SMOKE/20260606-150928/DTMAPI-evidence/UI-004/20260606-150959/title-settings-config-animal-husbandry-progress.png` and `title-settings-config-animal-husbandry-progress-custom.png`.
  - Exit check: `docs/debug/evidence/GAME-SMOKE/20260610-103119/process-check.txt` says no `DolocTown.exe`; fatal-window check says no fatal instance popup.
- Regression cases: ANIMAL-001, MANUALQA-027-ROOT-CAUSE, MANUALQA-028-README, MANUALQA-029-README, MANUALQA-031-REGRESSION-NEWCONTENT, CONFIG-008

## Diagnostic: Content.OfficialItemSourceIndex

- Status: experimental
- Public surface: `IContentQueryHelper.GetIndexedItems`, `IContentQueryHelper.GetIndexedItem`, `IContentItemInfo`, and source metadata on `InventoryDebugItem`.
- Game build: 23465763 workshop
- Game method/type: read-only filesystem scan of official local `MODS`, Steam Workshop `content/2285550`, `SAVE/mod_infos.json`, `info.json`, `Content/**/item_tbitem.json`, and related icon files; runtime give eligibility remains validated by `DolocTown.Config.DolocConfig.Tables.TbItem` and `DolocAPI.QueryItemProto`.
- Patch type: no Harmony patch; runtime/Core source indexing plus GameBridge runtime-table merge.
- Why this point: the Y console needs to show where official/Workshop items came from without modifying third-party files or assuming JSON-only rows are loaded by the game.
- Failure behavior: disabled source rows, JSON-only rows, missing runtime `TbItem`, illegal items, and full backpacks are displayed/logged as unavailable and are not given by DTMAPI.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseDebugInventory`.
- Evidence:
  - Build: DTMAPI 0.2.2 local build passed 2026-06-02 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: `Official content item source index = 108 item row(s) from 13 source mod(s)`, `Smoke exercise DebugInventory OK ... modItem=mod_butter ... sourceKind=Workshop ... sourceId=Workshop.3722791728 ... workshopRuntimeItem=verified`.
  - Screenshot/report: final butter Workshop item smoke `docs/debug/evidence/GAME-SMOKE/20260602-122848`; temporary enablement backup/restored in `docs/debug/evidence/WORKSHOP-MODINFO-BACKUP/20260602-122846`; earlier mineral-seed smoke `docs/debug/evidence/GAME-SMOKE/20260602-115735` remains retained.
- Regression cases: DEBUGITEMS-001

## Hook: Mail.ItemDelivery

- Status: experimental
- Public surface: `IMailDeliveryApi`, `MailItemDeliveryRequest`, and `MailItemDeliveryResult`.
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.SendItemAsEmail`, `DolocAPI.CountItem`, `DolocTown.EmailManager.emails`, `DolocTown.EmailAttachReward`, and `DolocTown.RewardItem`.
- Patch type: GameBridge reflection over native game objects. Public APIs expose DTOs and never raw decompiled game types.
- Why this point: migrated DTMAPI mods need safe item-mail delivery without directly editing saves or placing fragile `DolocAPI`/mail reflection inside ordinary mods.
- Failure behavior: invalid/missing items, disabled item sources, missing native methods, native item-generation failure, missing/unknown required content source, and native rejection return failed result DTOs and log reasons. Duplicate prevention first checks native backpack count, then scans unclaimed item-mail reward attachments; if a key already exists or is pending, delivery is skipped as a successful no-op. A request can require a specific enabled official content source so disabled mods cannot send attachment-less item mail.
- Mods/tests depending on it: none active. Historical archived sample: `DTMAPI.SecondMotorMod`; historical smoke flag `-AutoExerciseVehicle` now returns a blocked archived result.
- Evidence:
  - Build: DTMAPI 0.2.5 Release build/unit passed 2026-06-04 with 0 errors; only NU1900 vulnerability metadata warnings occurred under restricted network access.
  - Save: local slot 3 / index 2.
  - Log line: enabled 0.2.5 smoke `GAME-SMOKE/20260604-111533` records `Mail item delivery owner=DTMAPI.SecondMotorMod item=dtmapi_second_motor_key requested=1 sent=False skipped=True backpack=0 pendingMail=1 template=send_item_template source=Local.DTMAPI_SecondMotor sourceEnabled=True sourceKnown=True success=True`, proving source-aware duplicate detection. Disabled 0.2.5 smoke `GAME-SMOKE/20260604-111901` audits `Skip=2 Registration=0 Mail=0`, proving the disabled official source never reaches native mail delivery.
  - Screenshot/report: enabled vehicle/new-content smoke `docs/debug/evidence/GAME-SMOKE/20260604-111533`; disabled-mail smoke `docs/debug/evidence/GAME-SMOKE/20260604-111901`; both process checks say no `DolocTown.exe`. Earlier first-send proof remains `docs/debug/evidence/GAME-SMOKE/20260603-051204`.
- Regression cases: VEHICLE-001, MANUALQA-025-SECOND-MOTOR

## Hook: Vehicle.MotorApi

- Status: retired / removed from active runtime.
- Public surface: none active. The former `IMotorVehicleApi`, `CustomMotorDefinition`, `SecondMotorOptions`, `MotorVehicleState`, `MotorVehicleRegisterResult`, `MotorVehicleSummonResult`, `MotorVehicleRideResult`, and `MotorVehicleEventArgs` were removed from `DTMAPI.Abstractions`.
- Game build: historical 23465763 workshop evidence only.
- Game method/type: historical research touched `DolocTown.ItemMotorKey.OnUse`, `DolocTown.MotorInteractable.OnInteract`, `DolocTown.AgentControllerState.GetOnMotor/GetOffMotor`, `DolocTown.MotorController.OnFixedUpdate`, `DolocAPI.UnlockMotor`, `DolocAPI.SetMotorPosition`, `DolocAPI.EnterRoom`, `DolocAPI.Motor`, `DolocAPI.CurrentRoom`, and `DolocAPI.AgentPosition`. DTMAPI no longer installs these MotorVehicle-specific hooks.
- Patch type: retired. The active GameBridge no longer registers the vehicle API, no longer publishes `Vehicle.MotorApi`, and no longer patches native motor/riding/room-entry touchpoints for SecondMotor.
- Why retired: the only real consumer was the experimental `SecondMotorMod`. User manual QA on 2026-06-15 found abnormal light textures, abnormal farm-room textures, and severe cross-map texture pollution after the latest vehicle fixes. The native-owner review still says Doloc Town's native motor route is singleton-oriented, so future vehicle work needs a smaller fresh slice instead of reviving this route.
- Failure behavior: old failure DTOs and cleanup hooks were removed with the API. Active scripts must not install `DTMAPI_SecondMotor`, and `run-game-smoke.ps1 -AutoExerciseVehicle` remains a blocked archived command for compatibility.
- Mods/tests depending on it: none active as of 2026-06-15. Historical archived sample: `DTMAPI.SecondMotorMod`.
- Evidence:
  - Archive: `testmods/SecondMotorMod` moved to `archive/second-motor-20260615`; local packages moved outside the repo under `E:\Python_project\DTMAPI-local-archives\second-motor-20260615\MODS`.
  - Removal: cleanup Round 2 removed the public API/DTOs, GameBridge MotorVehicle implementation file, hook install/callback paths, vehicle smoke body, active `Vehicle.MotorApi` status publication, and unit coverage that only tested the retired API.
  - Historical support only: 2026-06-14/15 slot 8/9 smokes and earlier SecondMotor smokes are useful as native-owner research and regression history, but must not be cited as current completion proof.
- Regression cases: VEHICLE-001-ARCHIVED-20260615 and historical VEHICLE-001 rows only.

## Diagnostic: Config.PendingPreviewConditionalVisibility

- Status: experimental
- Public surface: `IConfigMenuPendingPreview` plus existing conditional `IConfigMenuItem.IsVisible/CanEdit` renderers.
- Game build: 23465763 workshop
- Game method/type: DTMAPI title settings retained Unity UI. The old fallback IMGUI overlay path was retired on 2026-06-15.
- Patch type: runtime/config registry preview scope; no game Harmony patch.
- Why this point: conditional rows such as AnimalHusbandryProgress `填充颜色` must respond to unsaved/pending selection of the `+`/Custom color swatch before the player presses Save.
- Failure behavior: if preview fails, committed preset visibility is shown and custom input does not appear until after saving; smoke status remains failed/pending.
- Mods/tests depending on it: `Yuuka.DTMAPI.AnimalHusbandryProgress`, `DTMAPI.UnitTests`.
- Evidence:
  - Build: DTMAPI 0.2.5 Release build/unit passed 2026-06-04 with 0 errors; only NU1900 vulnerability metadata warnings occurred under restricted network access.
  - Save: title homepage smoke, no save slot.
  - Log line: `Smoke staged AnimalHusbandryProgress Custom color preset for pending-preview screenshot`, followed by `Smoke.TitleSettingsConfigPageScreenshot.animal-husbandry-progress-custom = verified`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260603-160739/DTMAPI-evidence/UI-004/20260603-160812/title-settings-config-animal-husbandry-progress.png` hides the hex input for an ordinary preset; `title-settings-config-animal-husbandry-progress-custom.png` shows `填充颜色` and `F0F0F0` after staging Custom.
- Regression cases: MANUALQA-024-B, CONFIG-008

## Diagnostic: Debug.InstantSaveAndTeleportCsv

- Status: experimental
- Public surface: `IInstantSaveDebugApi`, `ITeleportDebugApi.ExportDestinationsCsv`, `TeleportDestination.SuggestedDisplayName`, `TeleportDestination.Source`, and `TeleportCsvExportResult`.
- Game build: 23465763 workshop
- Game method/type: native `DolocAPI.SaveGame(int)`, whitelisted native mark/station teleport destination enumeration, and DTMAPI evidence CSV writer. Historical debug reload evidence used `DolocAPI.LoadGame(int)`, but 0.2.6 disables immediate reload from the player-facing Y console.
- Patch type: reflected native calls plus debug-console UI actions; no raw save-file edits and no arbitrary coordinate exposure.
- Why this point: the player-visible Y console needs a discoverable "save here" action and a durable teleport audit file for manual name screening.
- Failure behavior: save-only snapshots are logged with before/after room and distance; `reloadAfterSave=true` returns `reload-disabled`; CSV failures return a structured result and keep the UI action experimental.
- Mods/tests depending on it: `DTMAPI.DebugConsoleMod`, smoke harness `-AutoExerciseInstantSave`, `-AutoExerciseDebugTeleport`.
- Evidence:
  - Build: DTMAPI 0.2.6 Release build/unit passed 2026-06-05 with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: 0.2.6 focused smoke `GAME-SMOKE/20260605-181224` logs `Smoke exercise InstantSave OK ... sameRoom=True, distance=0, reloadDisabled=True`, `Smoke exercise DebugTeleportCsv OK rows=80`, and `Smoke exercise DebugTeleport OK ... changedRoom=True`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260605-181224`; historical CSV proof remains `docs/debug/evidence/GAME-SMOKE/20260603-152451`.
- Regression cases: MANUALQA-024-D, SAVE-003, DEBUGTELEPORT-001, MANUALQA-026-MINE-Y-CONSOLE

## Hook: Fishing.MiniGameUpdate

- Status: experimental
- Public surface: `IFishingAutomationApi.Configure` experimental `FishingResultMode.AutoCompleteVisibleMiniGame`, `FishingResultMode.SkipMiniGameNativeResult`, `FishingAnimationMode.FastCastPull`, `FishingAutomationOptions.AnimationMultiplier`, and `FishingAutomationOptions.CastChargeRatio`.
- Game build: 23465763 workshop
- Game method/type: `AgentStateFishingReady.OnEnter/OnPlay/NextState`, `AgentStateFishingCast`, `AgentStateFishingWait`, `AgentStateFishingPull`, `FishingGameScrollBar.StartGame/UpdateGame/StopGame`, `DolocUserInput` fishing/tool/item getter reads, `DolocUserInput.NormalUseToolInProgress` Ready charge hold/release, selected rod renderer animator, `FishRodRenderer.CastHook`, and `FishRodRenderer.Pull` / `PullCancel`.
- Patch type: Harmony phase hooks plus `FishingGameScrollBar.UpdateGame` prefix/postfix observation, scoped native input getter prefixes during that update, GameBridge-owned reflected animator writes, positive-target Ready charge timer/input hold-release support, no-charge Ready guard, cast-hook physics scaling, and native pull-duration result scaling.
- Why this point: skip-minigame and visible auto-complete are different native result strategies. Skip routes a bite-ready state through the native no-minigame Pull/result path, while visible auto-complete now supplies native-style input to the real mini-game instead of writing success after a delay.
- Failure behavior: if the mini-game object/status/note data cannot be read safely, the bridge leaves the mini-game alone and records pending evidence rather than faking a fish reward. If a positive Ready charge target cannot be read, the bridge releases native input instead of forcing power. At target zero, DTMAPI neither writes the timer nor holds use input: the exact minimum target releases immediately, and the native `_isAnimationDone` gate keeps Ready until `fishing_ready` completes. Internal high-frequency service exceptions remain throttled. Mini-game and Ready handles clear on their native/save/title exits, animator/physics values restore from snapshots, and environment reset remains non-destructive.
- 2026-07-03 lifecycle attribution: `Fishing.Automation.Lifecycle` and `Refactor.AutoFishingLifecycle` are internal diagnostics only. They classify owner/config state separately from DTMAPI-held borrowed native handles, assert that transient native-keyed state is clear after save/title/disable/native exits, and expose runtime `CastChargeRatio`/fast-animation policy in summaries. They do not add Hook targets or change `IFishingAutomationApi` behavior.
- 2026-07-10 primitive routing: hook callbacks now route through the active `FishingRuntimeSession` and scoped input/animation leases. With no active session/lease they return immediately. `WaitEntered` and `WaitPlayable` are distinct, all mutating operations require the observed snapshot sequence, and stale/repeated actions are rejected before native cost/result transitions.
- 2026-07-10 input/charge follow-up: zero releases immediately and relies on native `_isAnimationDone`; positive-target FastAnimations stops adding Ready timer progress after target release; a Ready/Cast/WaitEntered phase whose native agent has already returned to an item-usable non-fishing state becomes `Interrupted`, allowing bounded product retry instead of a stuck session.
- 2026-07-10 patch ownership remains unchanged: base phase, input, minigame, and animation routes are separated in code only. They still use the existing unified Harmony ID; F6 disable, title return, owner cleanup, and runtime shutdown never call `UnpatchSelf`.
- Mods/tests depending on it: `Yuuka.DTMAPI.AutoFishing`.
- Evidence:
  - Build: DTMAPI 0.2.4 Release build/unit passed 2026-06-03; latest 2026-06-13 Release build/test passed after the native input rewrite, animation multiplier fix, and cast charge target setting.
  - Save: local slot 5 / index 4 for current AutoFishing fixture evidence; historical evidence used local slot 3 / index 2.
  - Log line: `Smoke.AutoFishingMiniGameComplete = verified ... behavior=AutoPlayVisibleMiniGame, status=Success, stableHoldFrames=82, releaseFrames=49`, `Smoke.AutoFishingAnimationSpeed = verified ... behavior=FastCastHookPhysics|FastAnimation, source=FishRodRenderer.CastHook|AgentStateFishingPull`, the retained 2026-07-08 no-charge scenario logs `readyCharge=False`, and historical experimental `Smoke.AutoFishingCastCharge = verified ... source=AgentStateFishingReady.NextState, target=0.5, progress=0.54, input=release`.
  - 2026-06-09 restore log line: `GAME-SMOKE/20260609-170646` records `Smoke.AutoFishingAnimationSpeedRestore = experimental. reason=AgentStateFishingPull.OnExit, restored=2` with `RunStatus=Passed`, `AutoFishingMiniGameComplete=Passed`, `ProcessExited=Passed`, and no fatal popup.
  - 2026-06-10 lifecycle isolation log line: `GAME-SMOKE/20260610-012052` records the AutoFishing hotkey/phase/minigame-complete route passed with clean exit and no `Lifecycle callback failed` entries after `FishingPullExitPostfix` switched to independent callback isolation plus finally-equivalent restore.
  - 2026-06-13 input/multiplier evidence: `docs/debug/evidence/GAME-SMOKE/20260613-153632` proves native input-driven minigame success without citing the old direct `visibleSeconds=0.76` success path, and user manual QA confirmed red/green/yellow handling succeeds. `docs/debug/evidence/GAME-SMOKE/20260613-154252` proves Cast/Pull animator and Pull-duration scaling but is incomplete for visible cast speed. `docs/debug/evidence/GAME-SMOKE/20260613-160354` proves cast-hook physics scaling (`FastCastHookPhysics`, hook velocity/gravity), Pull duration scaling, hook-physics restore, report export, clean exit, and no fatal popup. `docs/debug/evidence/GAME-SMOKE/20260613-163812` proves Ready charge native `_castTimer.Tick(extraDt)` through `FastReadyCharge`, Cast hook physics, Pull duration scaling, final `readyCharge=True, fastAnimation=True`, report export, clean exit, and no fatal popup. Failed `docs/debug/evidence/GAME-SMOKE/20260613-153850` is retained as the old missing-multiplier migration bug; failed `docs/debug/evidence/GAME-SMOKE/20260613-163257` is retained as the old smoke-gate false negative before Ready charge was tracked independently.
  - 2026-06-13 cast-charge evidence: `docs/debug/evidence/GAME-SMOKE/20260613-171405` proves configured target `0.5` through native Ready input hold/release (`Smoke.AutoFishingCastCharge = verified ... progress=0.54, input=release`) while FastAnimations still proves Ready timer speed, Cast hook physics, Pull duration scaling, report export, clean exit, and no fatal popup. Failed/false-positive `docs/debug/evidence/GAME-SMOKE/20260613-170514` and failed `docs/debug/evidence/GAME-SMOKE/20260613-171104` are retained as script-gate evidence around the fixed PowerShell clamp bug.
  - 2026-06-13 toggle-key evidence: `docs/debug/evidence/GAME-SMOKE/20260613-175055` proves the F7 rebound path. `summary.txt` records `AutoFishingToggleKey=F7`, and `DTMAPI-latest.log` records `AutoFishing native loop policy registered. Toggle=F7`, `Input F7 pressed dispatched to DTMAPI mods`, `AutoFishing automation enabled reason=hotkey F7`, plus final loop `AutoCast:True->Wait:True->BiteReady:True->BattleOrPull:True->PullExit:True->NextAutoCast:True`. The implementation is intentionally AutoFishingMod/config/input ownership: it uses existing `IDtmConfigMenuApi.AddKeybindOption`, `IInputHelper.RegisterButton`, and `IInputHelper.UnregisterButton`, and does not change the native fishing GameBridge owner path.
  - 2026-07-03 phase 4.5 lifecycle evidence: Release tests pass after adding internal lifecycle summary/ledger fields, `AutoFishingLifecycle`/`AutoFishingLifecycleSummary`/`AutoFishingSoak` smoke fields, and opt-in `-AutoFishingSoakLoops`. Fifth-save soak `docs/debug/evidence/GAME-SMOKE/20260703-194001` passed `RunStatus`, `AutoFishingPhase`, `AutoFishingMiniGameComplete`, `AutoFishingLifecycle`, `AutoFishingSoak`, `ProcessExited`, and `NoFatalInstanceWindow`; summary reported `charge=0;fast=False;mult=3`, `nativeTransientHandles=0`, `boundaryClearCount=0`, `miniGameHandles=0`, `readyChargeStates=0`, `animators=0`, `hookPhysics=0`, and `pendingCast=False`.
  - 2026-07-08 no-charge product and ledger-pressure evidence: `GAME-SMOKE/20260708-144151` is retained as the failed FastAnimations run that exposed smoke false success and Ready charge ticking despite `CastChargeRatio=0`; fixed FastAnimations `GAME-SMOKE/20260708-144556` passed with `readyCharge=False`, `FastCastHookPhysics`, Pull evidence, and real Wait/Bite/MiniGame/NextAutoCast flow. Soak `GAME-SMOKE/20260708-144724` passed `soakLoops=8/8`, `autoCast=0->10`, `miniGameComplete=0->9`, final `recordCount=6`, `autoFishingRecords=4`, and `snapshotBuilds=51`.
  - 2026-07-10 primitives/cutover evidence: `GAME-SMOKE/20260710-102651` passed DefaultLoop plus short soak; `103205` passed independent FastAnimations with native hook velocity `×3`, gravity `×9`, Pull scaling, and `instantBite=False`; `103350` passed independent InstantBite; `105201` passed independent native SkipMiniGame with `providerCalls=0`, no input lease, next cast, close cleanup, report export, and clean exit. Each final close summary had zero owner options/states, primitive sessions, input/animation leases, native transient handles, animators, and hook physics.
  - 2026-07-10 input/charge polish evidence: `GAME-SMOKE/20260710-135136` passed custom F7 input, charge-0 DefaultLoop, four casts, three real Wait/Bite/MiniGame/Pull completions, one soak loop, report export, close cleanup to zero, clean exit, and no fatal window. Partial `134805` passed target `0.5` release at `progress=0.52`, Fast Hook evidence, lifecycle, and clean exit, but the endpoint did not enter the current pond fixture and is not full-loop proof. Retained `134449` and `134005` exposed/fixed Fast post-release drift and external-key log-offset duplicate toggling.
  - Screenshot/report: current report `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260613-160443.zip`; historical skip/animation smoke `docs/debug/evidence/GAME-SMOKE/20260603-154816`; historical skip=false minigame smoke `docs/debug/evidence/GAME-SMOKE/20260603-173435`; animator restore smoke `docs/debug/evidence/GAME-SMOKE/20260609-170646`; current and historical process checks say no `DolocTown.exe`.
- Regression cases: AUTOFISHING-PRIMITIVES-CUTOVER-20260710, AUTOFISHING-LIFECYCLE-ATTRIBUTION-20260703, AUTOFISHING-TOGGLE-KEY-REBIND-20260613, AUTOFISHING-MINIGAME-INPUT-ANIMATION-20260613, FISHING-NATIVE-RESPONSIBILITY-REVIEW-20260610, MANUALQA-024-C, AUTOFISH-001, FISHING-ANIMATOR-RESTORE-20260609

## Hook: Resources.OilCoalDrop

- Status: experimental
- Public surface: OilMod content plus GameBridge resource-hit bridge; no stable public API yet.
- Game build: 23465763 workshop
- Game method/type: native `DolocTown.ToolCollider.HandleTools` prefix/postfix around resource removal, existing one-action resource-hit completion path, and native item generation/backpack placement for `crude_oil`.
- Patch type: `OilCoalDropFeature`/`OilCoalDropService` runtime logic using the shared `ToolCollider.HandleTools` Prefix/Postfix installed by `ToolColliderHitHookBridge`; no official/Workshop JSON mutation.
- Smoke owner: `Smoke/Cases/OilCoalDropSmokeCase.cs` owns Oil metadata, coal-resource preparation, and coal-drop smoke helpers; `Smoke/ContentSmoke.cs` keeps NewContent/Mine scheduling and shared helpers.
- Why this point: Oil should remain an official JSON item while DTMAPI supplies the fragile coal-drop behavior through the bridge.
- Failure behavior: if the selected resource is not recognized as coal or the item cannot be generated/placed, no extra drop is awarded and the result summary records the skipped/failed path. Pending coal-resource hits are cleared on SaveLoaded, ReturnedToTitle, and EnvironmentReset so stale collider/resource pairs do not survive save/title/environment boundaries.
- Mods/tests depending on it: `DTMAPI.OilMod`.
- Evidence:
  - Build: DTMAPI 0.2.8 Release build/unit passed 2026-06-06 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: 0.2.8 new-content smoke `GAME-SMOKE/20260606-031316` logs `OilMod content item=crude_oil fuelEnergy=1500 officialJson=item_tbitem.json`, `Smoke.NewContentOilItemMetadata = verified` with `id=crude_oil`, native probe `found crude_oil in DolocConfig.Tables.TbItem`, and `OilMod mining drop OK ... oilDrop=crude_oil ... placement={Placed crude_oil x1 through native backpack placement.}`.
  - Feature split log line: `GAME-SMOKE/20260610-124357` records `Feature.OilCoalDrop = ready`, `Resources.OilCoalDrop = experimental`, `OilMod mining drop OK source=native-tool-hit`, `OilMod.MiningDrop = experimental`, `Smoke.NewContentOilCoalDrop = verified`, `NewContentOilCoalDrop=Passed`, `NewContentApis=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed` after Oil coal-drop state and the smoke force flag moved into `OilCoalDropService`.
  - Shared-route regression: `GAME-SMOKE/20260610-124511` records `Feature.ActionCompletion = ready`, `Feature.OilCoalDrop = ready`, unchanged `Actions.OneActionComplete = verified` and `Actions.OneActionFuelFeed = verified`, plus `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, and clean process/fatal checks; this verifies the shared ToolCollider postfix still dispatches ActionCompletion before OilCoalDrop.
  - Lifecycle cleanup: unit coverage on 2026-06-10 seeds the private pending-hit cache and verifies `OilCoalDropFeature.SaveLoaded`, `ReturnedToTitle`, and `EnvironmentReset` clear it. DirectExe regression smoke `GAME-SMOKE/20260610-135456` keeps OneAction resource/wrong-tool/fuel/feed/vegetation paths passed with `Feature.ActionCompletion = ready` and `Feature.OilCoalDrop = ready`; `GAME-SMOKE/20260610-135605` keeps `Feature.OilCoalDrop = ready`, `Resources.OilCoalDrop = experimental`, `OilMod.MiningDrop = experimental`, `Smoke.NewContentOilCoalDrop = verified`, `NewContentOilCoalDrop=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
  - Shared hook-owner regression: `GAME-SMOKE/20260610-140419` records the unchanged OneAction callback path after the ToolCollider hook moved to `ToolColliderHitHookBridge`; `GAME-SMOKE/20260610-140641` records `Feature.OilCoalDrop = ready`, `Resources.OilCoalDrop = experimental`, `OilMod.MiningDrop = experimental`, `Smoke.NewContentOilCoalDrop = verified`, `NewContentOilCoalDrop=Passed`, `NewContentApis=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`, verifying Oil pre-hit capture and post-hit drop still run on the shared Prefix/Postfix route.
  - Smoke case split: `GAME-SMOKE/20260610-141632` records `SchemaVersion=2`, `NewContentOilItemMetadata=Passed`, `NewContentOilCoalDrop=Passed`, `NewContentEquipmentSlots=Passed`, `NewContentMineOfficialJson=Passed`, `NewContentMineProduction=Passed`, `NewContentApis=Passed`, `Feature.OilCoalDrop = ready`, `Resources.OilCoalDrop = experimental`, `Smoke.NewContentOilItemMetadata = verified`, `OilMod.MiningDrop = experimental`, `Smoke.NewContentOilCoalDrop = verified`, clean process/fatal checks, and unchanged result/status field meanings after the Oil smoke methods moved to `Smoke/Cases/OilCoalDropSmokeCase.cs`.
  - Callback-isolation regression: `GAME-SMOKE/20260610-163928` records `NewContentOilItemMetadata=Passed`, `NewContentOilCoalDrop=Passed`, `NewContentEquipmentSlots=Passed`, `NewContentMineOfficialJson=Passed`, `NewContentMineProduction=Passed`, `NewContentApis=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed` after the shared ToolCollider postfix split ActionCompletion and OilCoalDrop into independent safe wrappers.
  - Screenshot/report: current metadata + coal-drop evidence `docs/debug/evidence/GAME-SMOKE/20260606-031316` with `NewContentOilItemMetadata=true`, `NewContentOilCoalDrop=true`, clean exit, and no fatal popup. The 0.2.4 `dtmapi_oil` evidence is retained as historical pre-rename proof.
  - Report note: the 2026-06-10 Oil feature split, pending-cleanup, shared hook-owner, and smoke-case split smokes did not export fresh report zips; their `latest-report.txt` files point to previous diagnostics/AnimalViewer reports and are not cited as OilCoalDrop report evidence.
- Regression cases: NEWCONTENT-024-F, MANUALQA-028-README

## Hook: Machine.ProductionRuntimeLoop

- Status: experimental
- Public surface: `IMachineProductionApi`, `MachineDefinition`, `MachineRecipeInput`, `MachineOutputRule`, `MachineProductionState`, and `MachineRegisterResult`. `MachineDefinition` exposes experimental native-tech route hints, recipe inputs, and electric-only or fuel-capable machine settings for JSON-backed machines. `MachineProductionState` exposes experimental telemetry for item/equipment/recipe/group ids, visual scale, electric-only/fuel-capable state, default mode, fuel capacity/remaining, fuel costs when fuel mode is enabled, electric cycle costs, cycle minutes/TUs, next due TUs, mode, last output/costs, output target, machine-owned storage fill/capacity, storage line capacity, and native tech-tree summary.
- Game build: 23465763 workshop
- Game method/type: DTMAPI update loop, `DolocAPI.ArchiveData`, current/root/archive/farm room candidate equipment enumeration, placed `Equipment`/`Case` inventory, `LinearInventory.PlaceItemAt`, native equipment `IElectronicComponent` / `ElectronicComponentAppliance.Launch()`, `ArchiveDataHandle.PassTimeNoControl`-driven time jumps observed through the runtime loop, `DolocAPI.assets.techTrees`, and runtime `DolocConfig.Tables.TbTechNode` injection.
- Patch type: GameBridge runtime loop and reflection over room/equipment/tech-tree/recipe state; official JSON owns the mine item/equipment/recipe. 0.2.7+ smoke may force the poll/observation pass but does not force production due.
- Why this point: a stable public machine contract should not expose raw Doloc Town equipment types, while the bridge can own fragile placed-equipment discovery and output delivery.
- Failure behavior: missing archive/current room/equipment data leaves the API registered but production idle; Mine production fails rather than silently falling back to backpack when Mine-owned storage is unavailable or full. Electric mode calls the native component `Launch()` before producing; if native power is unavailable, due time is retained and the low-power reason is logged. If fuel mode is disabled, normalized state forces fuel capacity and fuel costs to `0` and reports `fuel=disabled`; fuel-capable definitions still retain the older fuel telemetry. Bounded catch-up stops at the safety cap with telemetry rather than silently discarding cycles. DTMAPI reports electric/cycle/storage/tech-tree state through experimental config/API telemetry rather than exposing raw native machine UI types.
- Mods/tests depending on it: `DTMAPI.MineMod`.
- Evidence:
  - Build: DTMAPI 0.3.1 Release build/unit passed 2026-06-06 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: 0.3.1 new-content smoke `GAME-SMOKE/20260606-150834` remains historical fuel-capable evidence. The 2026-06-12 bottom-layer smoke `GAME-SMOKE/20260612-170708` verifies electric-only/no-fuel (`electricOnly=True`, `fuel=disabled`, `electricPowerCost=10`), production/storage, official tech-tree UI, HookProbe, SaveLoaded, and clean process/fatal checks.
  - Screenshot/report: current 0.5.1 Mine evidence is `docs/debug/evidence/GAME-SMOKE/20260612-170708`; game-side screenshots are under `NEWCONTENT-025/20260612-170750`. Historical retained Mine/Oil/Equipment evidence remains `docs/debug/evidence/GAME-SMOKE/20260606-150834`.
- Regression cases: NEWCONTENT-024-G, MANUALQA-025-MINE-STORAGE, MANUALQA-026-MINE-Y-CONSOLE, MANUALQA-027-ROOT-CAUSE, MANUALQA-028-README, MANUALQA-029-README, MANUALQA-031-REGRESSION-NEWCONTENT

## Hook: Machine.MineVisualContainment

- Status: experimental
- Public surface: no new public API; this is GameBridge-owned visual containment for `IMachineProductionApi` Mine definitions.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.EquipmentRenderer.OnReuse`, `DolocTown.EquipmentBuilder.CreateIndicator`, `DolocTown.EquipmentBuilder.TurnIndicator`, runtime current-room equipment enumeration, and GameBridge environment-reset refresh.
- Patch type: Harmony postfix hooks plus instance-scoped reflected `Transform.localScale` writes.
- Why this point: Mine needs a visible 2x sprite and 2x placement preview, but scaling shared renderers/prefabs can contaminate unrelated machines, chests, or decorative equipment.
- Failure behavior: pooled non-Mine renderers are reset to `1x1x1`; preview scale applies only when the builder's equipment proto/id resolves to `dtmapi_mine`; environment reset forces a quick machine-production visual poll so placed Mine scale is reapplied after room/camera transitions; smoke fails if any non-Mine equipment remains scaled.
- Mods/tests depending on it: `DTMAPI.MineMod`, smoke harness `-AutoExerciseMineContentApis`.
- Evidence:
  - Build: DTMAPI 0.2.9 Release build/unit passed 2026-06-06 with 0 errors.
  - Save: local slot 3 / index 2.
  - Log line: 0.2.9 new-content smoke `GAME-SMOKE/20260606-053233` logs `Machine.VisualScale = verified. dtmapi_mine visualScale=2, rendererScale=2x2, applied=True`, `Machine.MineVisualContainment = verified. containment=True, contamination=False`, and `EquipmentRenderer.OnReuse resets localScale to 1`.
  - 2026-06-12 bottom-layer implementation: builder preview id resolution was widened, `Machine.MinePreviewScale` records preview scaling from `EquipmentBuilder.CreateIndicator/TurnIndicator`, and `DolocTownGameBridge.NotifyGameBridgeFeaturesEnvironmentReset` forces a visual poll before feature reset dispatch. Smoke `GAME-SMOKE/20260612-170708` logs `Machine.MineVisualContainment=verified` with `containment=True`, `contamination=False`, `mineScaled=1`, `nonMineScaled=0`, plus placement `rendererScale=2x2`.
  - Screenshot/report: current evidence is `docs/debug/evidence/GAME-SMOKE/20260612-170708`; placement screenshot is under game-side `NEWCONTENT-025/20260612-170750`; process check says no `DolocTown.exe`. Historical placement evidence remains `docs/debug/evidence/GAME-SMOKE/20260606-053233`.
- Regression cases: MANUALQA-026-MINE-Y-CONSOLE, MANUALQA-029-README

## Hook: Player.EquipmentSlotsApi

- Status: experimental
- Public surface: `IEquipmentSlotsApi`, `EquipmentSlotsOptions`, `EquipmentSlotsState`, `EquipmentSlotInfo`, `EquipmentSlotEquipResult`, `EquipmentSlotsRecoveryResult`.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.GameData.AgentEquipmentManager.ReloadParams`, `DolocTown.UI.AccessoriesBar.__Init`, `DolocTown.UI.AccessoriesBar.OnStartShow`, and reflected native `DolocTown.UI.AccessorySlot` clone click/hover binding. Shield hats are handled by the separate `Player.EquipmentSlotsShield` hit-path hook.
- Patch type: Harmony Postfix observation/update bridge plus interactive UI clone rendering; no raw equipment UI type exposed publicly.
- Why this point: `AgentEquipmentManager.ReloadParams` is the lowest-risk observed stats refresh point for DTMAPI-managed extra-slot attributes, while `AccessoriesBar` lifecycle hooks let DTMAPI render extra slot affordances without taking over vanilla visual equipment slots.
- Failure behavior: API state records hook installation, UI render state, stored/applied counts, click/hover availability, and recovery messages; populated extra-slot recovery returns items through native backpack placement with overflow email enabled. Runtime mutations mark DTMAPI protected sidecar storage dirty, but only a native SaveGame postfix persists sidecar JSON under the current save scope. SaveLoaded/ReturnedToTitle clear unsaved in-memory state. If Unity screenshot capture is unavailable in-save, logs keep `uiRendered`, occupied slot, interactive/hoverable counts, extra hat defense, and policy evidence.
- Mods/tests depending on it: `DTMAPI.MoreEquipmentSlotsMod`.
- Evidence:
  - Build: DTMAPI 0.3.1 Release build/unit passed 2026-06-06 with 0 errors.
  - Build: 2026-06-14 protected-storage branch Release build passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`; third-save basic equipment smoke `GAME-SMOKE/20260614-011222` passed; user manual QA confirmed no-copy/cross-save behavior and disabled-mod backpack recovery. 2026-06-14 mushroom-hat follow-up passed `git diff --check`, Release build/test, and third-save smoke `GAME-SMOKE/20260614-082151` after splitting official hat handling into `HatInfo.Skill` (`AgentEquipmentFunction`) and `HatInfo.Defense` (post-`ReloadParams` `AgentEquipmentAbility` merge) without writing native `hatItem`; manual QA for `mushroom_hat` is pending. Automated per-save orphan recovery, mail overflow, and hot-disable visual cleanup are pending.
  - Build: 2026-06-14 equipment hat table diagnostic passed `git diff --check` with line-ending warnings only, Release build, and third-save NewContent smoke `GAME-SMOKE/20260614-091839`.
  - Save: local slot 3 / index 2.
  - Log line: 0.3.1 new-content smoke `GAME-SMOKE/20260606-150834` logs `rendered=3, interactive=3, hoverable=3, readOnly=false`, passive `grandmas_button` equip/recover, attribute-only hat `straw_hat` equip/recover, native hat preserved `miner_helmet->miner_helmet->miner_helmet`, `preserveVanillaVisualSlots=true`, and final stored/recovered state `recoveredStored=0`. Protected-storage branch smoke `GAME-SMOKE/20260614-011222` revalidates those same equip/recover basics after local install and records legacy global storage adoption into save scope `slot-2`; user manual QA confirms stored-item save/reload, cross-save isolation, and disabled-mod backpack recovery. Latest NewContent smoke `GAME-SMOKE/20260614-082151` adds `mushroom_hat` and verifies native visual hat preservation plus `mushroomHatDefence=0->1->0`. Same-process disable still leaves inert extra-slot visuals until restart, so an unregister/hide follow-up is needed.
  - Log line: `GAME-SMOKE/20260614-091839` logs `Smoke.EquipmentHatTable = verified`, `hats=33`, `itemHatRows=33`, `hatsWithoutItems=0`, `itemRowsWithoutHatInfo=0`; generated JSON/CSV under `D:\Steam\steamapps\common\Doloc Town\DTMAPI\evidence\NEWCONTENT-025\20260614-091928`.
  - Screenshot/report: current evidence `docs/debug/evidence/GAME-SMOKE/20260606-150834` rechecks `NewContentEquipmentSlots=true`, `interactive=3`, `hoverable=3`, `readOnly=false`, `attributeOnly=true`, `preserveVanillaVisualSlots=true`, clean exit, no fatal popup, and no leftover `DolocTown.exe`. Earlier read-only player equipment strip evidence remains historical; the strip screenshot fallback still returned unavailable, so the retained proof is log/summary based.
- Regression cases: NEWCONTENT-024-H, MANUALQA-028-README, MANUALQA-029-README, MANUALQA-031-REGRESSION-NEWCONTENT, EQUIPMENT-HAT-TABLE-DIAGNOSTIC-20260614

## Hook: Player.EquipmentSlotsShield

- Status: experimental
- Public surface: no new public API; this is internal GameBridge behavior behind `IEquipmentSlotsApi`.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.BodyController.OnAttacked`, with read-only priority check through native `DolocTown.GameData.AgentEquipmentManager.TryGetShieldItem`.
- Patch type: Harmony Prefix on player hit path; returns to native immediately when a vanilla hat-slot shield exists.
- Why this point: native `AgentEquipmentFunctionShield.TryBlockAttack` clears the official hat slot with `DolocAPI.EquipHat(string.Empty)` when a shield breaks. Extra-slot shield hats therefore cannot safely be inserted into `AgentEquipmentManager.functions`; the player hit path is the native owner that can preserve vanilla shield priority while letting DTMAPI-managed sidecar shield charges block or partially block damage.
- Failure behavior: if the hook is absent, shield hats remain stored as managed entries but cannot block hits. If native metadata lacks `ItemFunctionHatShield.MaxShieldValue`, the item is rejected instead of attempting unsafe shield behavior. Runtime shield mutations mark protected sidecar storage dirty and wait for native SaveGame before persistence.
- Mods/tests depending on it: `DTMAPI.MoreEquipmentSlotsMod`, smoke harness `-AutoExerciseNewContentApis`.
- Evidence:
  - Build: 2026-06-14 Release tests passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`; unit coverage verifies native shield damage semantics for full-defend, shield-value consumption, and break residual damage.
  - Save: local slot 3 / index 2.
  - Game smoke: `GAME-SMOKE/20260614-120739` passed `NewContentEquipmentSlots`, `NewContentApis`, `ProcessExited`, and `NoFatalInstanceWindow`.
  - Log line: `Player.EquipmentSlotsShield = verified` and `Smoke.NewContentEquipmentSlotsShield = verified` with `box_hat`, `damage=1`, `blocked=True`, `blockedDamage=1`, `shield=80->79/80`, `nativeHat=miner_helmet->miner_helmet->miner_helmet`, `shieldHatRecover=1`, and `recoveredStored=0`.
  - Manual QA: `docs/reviews/manual-qa/2026/20260614-0003-equipment-slots-shield-hat-manual-qa.md` passed vanilla shield priority, one extra-slot shield plus two other hats, three extra-slot shield hats, and mushroom-hat defense loading. Known accepted visual difference: extra-slot shields do not render the official yellow shield bar.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260614-120739`; hat table JSON/CSV emitted under game-side `DTMAPI/evidence/NEWCONTENT-025/20260614-120844`.
- Regression cases: EQUIPMENT-SLOTS-SHIELD-HAT-PROTECTION-20260614, NEWCONTENT-024-H

## Hook: Player.EquipmentSlotsSaveTransaction

- Status: experimental
- Public surface: no new public API; this is GameBridge-owned persistence behavior behind `IEquipmentSlotsApi`.
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.SaveGame` / `DolocTown.GameData.DataPersistenceManager.SaveGame` postfix, `SaveLoaded`, and `ReturnedToTitle` runtime boundaries.
- Patch type: Harmony Postfix for native save plus DTMAPI runtime lifecycle callbacks.
- Why this point: equipment-slot sidecars must not commit no-save runtime mutations, must not be shared across saves, and still need to persist after the game has completed its own save.
- Failure behavior: sidecar JSON is not written when an equip/unequip occurs; dirty owner IDs are flushed only after native SaveGame. SaveLoaded and ReturnedToTitle clear in-memory state and dirty flags so unsaved mutations are discarded. New protected storage writes to `config/protected-items/equipment-slots/slot-<archiveIndex>/equipment-slots-<owner>.json` with archive/player/save-clock metadata; title-page registration waits for a loaded archive before reading sidecars; orphan recovery scans the current save scope and recovers tail slots first. Legacy global `equipment-slots-<owner>.json` is treated as migration input and archived after a successful per-save persist.
- Mods/tests depending on it: `DTMAPI.MoreEquipmentSlotsMod`, smoke harness `-AutoExerciseNewContentApis` and delayed `-AutoExerciseInstantSave`.
- Evidence:
  - Build: DTMAPI 0.2.9 Release build/unit passed 2026-06-06 with 0 errors.
  - Build: 2026-06-14 protected-storage branch Release build passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`; third-save basic equipment smoke `GAME-SMOKE/20260614-011222` passed and logged legacy global storage adoption/dirty state for save scope `slot-2`; user manual QA passed equip-save-reload, cross-save isolation, and disabled-mod backpack recovery. Automated disabled/unsubscribed orphan recovery, mail overflow, and hot-disable visual cleanup remain pending.
  - Save: local slot 3 / index 2.
  - Log line: no-save smoke `GAME-SMOKE/20260606-052246` logs `Player.EquipmentSlotsSaveTransaction = dirty` and no `SaveSaved`/`storage persisted`; the sidecar timestamp stayed at its prior value. Delayed instant-save smoke `GAME-SMOKE/20260606-053233` logs `Player.EquipmentSlotsSaveTransaction = dirty`, `SaveSaved hook dispatched`, `EquipmentSlots storage persisted owner=DTMAPI.MoreEquipmentSlotsMod reason=SaveSaved slot=2`, and `Player.EquipmentSlotsSaveTransaction = verified. Flushed 1 dirty equipment-slot owner(s) after native SaveGame completed.`
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260606-052246` and `docs/debug/evidence/GAME-SMOKE/20260606-053233`; process checks say no `DolocTown.exe`.
- Regression cases: NEWCONTENT-024-H, MANUALQA-029-README

## Hook: Save.MoreSlotsApi

- Status: experimental
- Public surface: `ISaveSlotsApi`, `SaveSlotsOptions`, `SaveSlotsState`, and `SaveSlotsRegisterResult`.
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.gameManager.archiveFileCount`, official `LocalSave.GetAllArchiveInfo`, and `GameDataUiState.Show -> GameDataPanel.Render`.
- Patch type: GameBridge runtime refresh/reflection plus Harmony `GameDataUiState.Show` Postfix for official save UI layout evidence; no direct save-file edits, no custom replacement save UI, and no inherited `GameDataPanel.Select(int)` hook in the fixed 12-slot path.
- Implementation owner: `SaveSlotsFeature` registers `ISaveSlotsApi`; `SaveSlotsService` owns registration state, throttled runtime `archiveFileCount` refresh, and expanded official-panel paging state; `DolocTownExperimentalBridgeApi` no longer implements the API.
- Why this point: More Saves should expand the official save screen while the game continues to own archive files, slot rendering, load, delete, and copy behavior.
- Failure behavior: if `DolocAPI.gameManager` is not available, registration reports pending and runtime refresh retries on a short 750 ms cadence until the manager appears. Once configured, refresh uses a 3 second heartbeat, and `SaveLoaded` forces a refresh. Enabled `SaveSlotsOptions` requests are normalized to exactly 12 total official slots for the MoreSaves player-facing path; disabling restores the vanilla target count of 6 but does not delete extra files. SaveSlots official-panel layout logic now rejects non-`DolocTown.UI.GameDataPanel` targets before calling `ResetLayoutSize`, so homepage and pause `DolocGridUI<T>` instances cannot be mistaken for save panels.
- Mods/tests depending on it: `DTMAPI.MoreSavesMod`, smoke harness official save UI evidence during save-slot selection.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors after the refresh throttle.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260610-100337` logs `Feature.SaveSlots = ready`, `Save.MoreSlotsApi = configured-official-archive-count`, one runtime correction `Official save slot count set 6->12 ... reason=runtime refresh native=6 target=12`, one load-boundary force refresh `Official save slot count set 12->12 ... reason=SaveLoaded native=12 target=12`, and `Smoke.MoreSavesOfficialSaveUi = verified. archiveFileCount=12, panelSlotCount=12, renderedSlots=12, path=DolocAPI.gameManager.archiveFileCount -> GameDataUiState.Show -> GameDataPanel.Render.` after moving the evidence recorder into `Smoke/Cases/SaveSlotsSmokeCase.cs`.
  - 2026-06-12 follow-up: MoreSaves no longer exposes a slot-count config option; old saved `SlotCount` values are migration-only and enabled requests normalize to 12. Unit coverage verifies enabled 6/24 requests both normalize to 12 and disabled requests normalize to vanilla 6. Third-save evidence `GAME-SMOKE/20260612-203558` records `MoreSavesOfficialSaveUi=Passed`, `MoreSavesOfficialSaveUiEvidence=Passed`, `archiveFileCount=12`, `panelSlotCount=12`, `renderedSlots=12`, and `visibleSlotsAfterPaging=12`.
  - 2026-06-12 bottom-layer implementation: `Save.MoreSlotsUiPaging` records `officialSaveUiPaging=True`, page bounds, and reason (`GameDataUiState.Show`, `GameDataPanel.Select`, or pager button). Smokes `GAME-SMOKE/20260612-170240`, `GAME-SMOKE/20260612-170331`, `GAME-SMOKE/20260612-170421`, and `GAME-SMOKE/20260612-170512` verify 6/12/18/24 layouts, `visibleSlotsAfterPaging=6/12/6/12`, and targets `1`, `1`, `1|13`, and `1|13|24`.
  - 2026-06-13 root fix: `GameDataPanel.Select` prefix was removed because Harmony resolved the inherited `DolocGridUI<T>.Select` path broadly and let homepage/pause grids enter SaveSlots layout code. Third-save evidence `GAME-SMOKE/20260613-144038` revalidated the fixed 12-slot official UI with `Save.MoreSlotsUiPaging = not-required`, `archiveFileCount=12`, `panelSlotCount=12`, `renderedSlots=12`, `visibleSlotsAfterPaging=12`, and no leftover process/fatal popup.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-100337` and report `dtmapi-report-20260610-100413.zip`; process/fatal checks say no `DolocTown.exe` and no fatal popup.
- Regression cases: MANUALQA-029-README, SAVE-001, OFFICIAL-001

## Hook: Save.MoreSlotsUiPaging

- Status: experimental
- Public surface: none beyond existing `ISaveSlotsApi`; this is an internal GameBridge UI adaptation for expanded official save panels.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.GameDataUiState.Show`.
- Patch type: Harmony Postfix on `GameDataUiState.Show`; the inherited `GameDataPanel.Select(int)` hook is intentionally not installed for the fixed 12-slot path.
- Why this point: fixed 12 save slots remain inside the official save panel while official `LocalSave`, `DataPersistenceManager`, `GameDataUiState`, and `GameDataPanel` keep ownership of archive data, rendering, load, delete, and copy behavior.
- Failure behavior: missing panel/slots/types leaves the official panel unmodified and records pending/failed status; non-`DolocTown.UI.GameDataPanel` callback targets are ignored. 6/12-slot panels restore original layout and destroy any stale DTMAPI pager.
- Mods/tests depending on it: `DTMAPI.MoreSavesMod`, smoke harness `-AutoExerciseMoreSavesOfficialSaveUi`.
- Evidence:
  - Build: 2026-06-12 bottom-layer Release build/test passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`; `git diff --check` passed with line-ending warnings only. 2026-06-13 root fix passed Release build and `git diff --check` after removing the Select hook.
  - Save: local slot 3 / index 2 for smoke evidence.
  - Log line: current fixed-12 evidence `GAME-SMOKE/20260613-144038` logs `Save.MoreSlotsUiPaging = not-required` and `MoreSaves official save UI evidence archiveFileCount=12, panelSlotCount=12, renderedSlots=12, visibleSlotsAfterPaging=12`. Historical expanded-paging smokes `GAME-SMOKE/20260612-170421` and `GAME-SMOKE/20260612-170512` remain pre-root-fix evidence only.
  - Screenshot/report: current SaveSlots screenshot evidence is under game-side `SAVESLOTS-UI/20260613-144113` and copied evidence in `docs/debug/evidence/GAME-SMOKE/20260613-144038`.
- Regression cases: BOTTOM-LAYER-REFACTOR-20260612, MANUALQA-029-README, SAVE-001, OFFICIAL-001

## Hook: Smoke.DiagnosticsSnapshot

- Status: verified
- Public surface: `IDtmDiagnosticsApi.GetSnapshot`
- Game build: 23465763 workshop
- Game method/type: no native game hook; this is a DTMAPI smoke status emitted after the runtime diagnostics API exports a report and reads the structured snapshot.
- Patch type: DTMAPI runtime diagnostics/status.
- Implementation owner: `DtmApiRuntime` registers `IDtmDiagnosticsApi`; `DiagnosticsService` stores errors, warnings, hook statuses, feature statuses, latest log path, latest report path, and internal report-only diagnostics aggregate counters; `DtmApiRuntime.CreateDiagnosticsSnapshot()` synthesizes discovered/loaded/disabled/error mod status rows and structured `StatusCode` values from runtime discovery, loaded mods, official enablement, and diagnostics errors; `DolocTownGameBridge` mirrors `Feature.<Id>` dispatch state into structured feature-status rows, throttling repeated successful `Update` publication to first/failure/recovery/non-`Update`/10-second heartbeat events.
- Why this point: verifies diagnostics snapshot consumers do not need to parse logs to find loaded mods, discovered/disabled/error mod statuses, structured status codes, warnings/errors, hooks, feature statuses, or report/log paths.
- Failure behavior: if an expected `Feature.<Id>` hook/status row is missing, if loaded mods do not have a loaded mod-status row with `StatusCode=loaded`, if any mod status row lacks a `StatusCode`, or latest log/report paths do not point to existing files, `Smoke.DiagnosticsSnapshot` is marked failed and the focused smoke fails.
- Mods/tests depending on it: smoke harness `-AutoExerciseZoom` and ActionSpeed interaction smoke; unit tests `DiagnosticsSnapshotApiExposesRuntimeState` and `OfficialLocalModPackagesRespectOfficialEnablement`.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2.
  - Camera log line: `GAME-SMOKE/20260610-043619` records `Smoke diagnostics snapshot OK scenario=Camera, expectedFeatures=Camera, loadedMods=14, mods=14, modStatusCodes=loaded=14, errors=0, warnings=0, hooks=60, features=5, latestLog=..., latestReport=...dtmapi-report-20260610-043801.zip` and `Smoke.DiagnosticsSnapshot = verified`.
  - ActionSpeed log line: `GAME-SMOKE/20260610-043830` records `Smoke diagnostics snapshot OK scenario=ActionSpeed, expectedFeatures=ActionSpeed, loadedMods=14, mods=14, modStatusCodes=loaded=14, errors=0, warnings=0, hooks=64, features=5, latestLog=..., latestReport=...dtmapi-report-20260610-043910.zip` and `Smoke.DiagnosticsSnapshot = verified`.
  - Unit status code rows: `DependencyVersionApiVersionAndCircularDependencyDiagnostics`, `DiagnosticsSnapshotApiExposesRuntimeState`, and `OfficialLocalModPackagesRespectOfficialEnablement` cover `loaded`, `disabled`, `missing-dependency`, `dependency-cycle`, `entry-dll-error`, `code-load-error`, `api-too-new`, and `unknown-error`.
  - 2026-06-17 API-version wording follow-up: the structured mod status still uses `api-too-new` for a `MinimumDTMApiVersion` block, but retained diagnostics text now says `DTMAPI 前置版本过旧` and includes `Run 1_install_dtmapi.bat`; player-facing UI should prefer that text over the internal status-code label.
  - Aggregate counter rows: 2026-06-10 unit coverage records 1005 repeated errors and warnings, keeps the retained error/warning windows capped at 1000, and verifies exported report summaries include two `DIAGNOSTIC-AGGREGATE` lines with total count and latest details. Camera snapshot smoke `GAME-SMOKE/20260610-142415` and ActionSpeed snapshot smoke `GAME-SMOKE/20260610-142626` verify the public snapshot surface stayed unchanged while matching report zips `dtmapi-report-20260610-142555.zip` and `dtmapi-report-20260610-142705.zip` were exported.
  - Report summary: both report zips contain `Errors: 0`, `Warnings: 0`, `LatestLogPath`, `LatestReportPath`, the matching `HOOK Feature.Camera` / `HOOK Feature.ActionSpeed` row, and the matching `FEATURE Camera` / `FEATURE ActionSpeed` structured row.
- Regression cases: DIAGNOSTICS-AGGREGATE-COUNTERS-20260610, DIAGNOSTICS-STATUS-CODES-20260610, DIAGNOSTICS-SNAPSHOT-20260609, DIAGNOSTICS-SNAPSHOT-MOD-STATUS-20260610, FEATURE-STATUS-PUBLISH-THROTTLE-20260610

## Hook: Feature.Camera

- Status: ready
- Public surface: internal GameBridge diagnostics/status only; public camera APIs remain `ICameraViewApi` and obsolete `ICameraZoomApi`.
- Game build: 23465763 workshop
- Game method/type: no native game hook; this status is emitted by the DTMAPI GameBridge feature host around CameraFeature dispatch.
- Patch type: GameBridge runtime diagnostics/status.
- Implementation owner: `DolocTownGameBridge` safe-dispatches every `IGameBridgeFeature` operation, records `Feature.<Id>` hook status, mirrors the same state into `DiagnosticsService` feature-status rows, and keeps an internal feature-status model with feature id, last operation, success/failure, failure count, and last error; repeated successful `Update` publication is throttled to first/failure/recovery/non-`Update`/10-second heartbeat events; `CameraFeature.Id` is `Camera`; Camera hook installation is dispatched through `InstallHooks`.
- Why this point: proves the Camera feature is registered with the feature host instead of being a special one-off bridge path.
- Failure behavior: `RegisterApis`, `PublishHookStatuses`, `InstallHooks`, `Update`, `SaveLoaded`, `ReturnedToTitle`, and `EnvironmentReset` dispatches are wrapped per feature. A feature exception records `DTMAPI.GameBridge.Feature.<Id>` diagnostics, marks `Feature.<Id>` failed, updates the internal failure count and last error, logs the exception type/message, and does not block the next feature. Successful `Update` dispatches update the internal model but publish hook/diagnostics status only on the 10-second heartbeat unless a failure/recovery or non-`Update` operation occurs.
- Mods/tests depending on it: internal Camera feature host smoke evidence; `DTMAPI.HookProbeMod` observes the hook status.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2.
  - Latest log line: `GAME-SMOKE/20260610-043619` logs `Feature.Camera = ready` for `PublishHookStatuses`, `InstallHooks`, `Update`, `ReturnedToTitle`, `SaveLoaded`, and `EnvironmentReset`, with `Feature status: id=Camera, lastOperation=..., success=True, failureCount=0, lastError=none`; `Smoke.DiagnosticsSnapshot = verified` confirms the structured diagnostics snapshot also contains `FEATURE Camera`, `mods=14`, and `modStatusCodes=loaded=14`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-043619`; report zip pointer `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-043801.zip`.
- Regression cases: CAMERA-HOOK-OWNER-FEATURE-20260609, GAMEBRIDGE-FEATURE-HOST-HARDENING-20260609, CAMERA-PLAYABLE, DIAGNOSTICS-SNAPSHOT-20260609, DIAGNOSTICS-SNAPSHOT-MOD-STATUS-20260610, DIAGNOSTICS-STATUS-CODES-20260610, FEATURE-STATUS-PUBLISH-THROTTLE-20260610

## Hook: Camera.ViewApi

- Status: experimental
- Public surface: `ICameraViewApi`, `ICameraViewLease`, `CameraViewRequest`, `CameraViewResult`, `CameraViewState`, and `GetSnapshot(string uniqueId)`.
- Game build: 23465763 workshop
- Game method/type: `DolocAPI.mainCamera.orthographicSize`; `DolocAPI.SetEnvCamera(...)` is observed only as a lifecycle boundary where DTMAPI reapplies the active playable-view orthographic size.
- Patch type: GameBridge runtime reflection plus Harmony Postfix on `DolocAPI.SetEnvCamera`; no raw Unity camera object or decompiled game type is exposed through the public API.
- Implementation owner: `DolocTownGameBridge` hosts `IGameBridgeFeature` instances and safe-dispatches `RegisterApis`, `PublishHookStatuses`, `InstallHooks`, `Update`, `SaveLoaded`, `ReturnedToTitle`, and `EnvironmentReset` to `CameraFeature`; `CameraFeature.Id` is `Camera`, owns `cameraViewSetEnvCameraPatched`, installs the `DolocAPI.SetEnvCamera` postfix, and registers `ICameraViewApi` through `CameraViewService` plus diagnostics through `CameraDiagnosticsService`; `DolocTownGameBridge` no longer stores `cameraZoomSetEnvCameraPatched`; `SmokeHarness` owns `SmokeUpdate()` scheduling only; `Smoke/Cases/CameraPlayableSmokeCase.cs` owns the `AutoExerciseZoom` / `Smoke.CameraPlayable` case implementation; `DolocTownExperimentalBridgeApi` no longer implements this camera API.
- Why this point: playable zoom should keep the native camera follow/range semantics intact. DTMAPI only changes the gameplay camera orthographic size and lets the native `CameraController.UpdateCamPosition(...)` path continue following the player. The old `CameraController.RefreshResolution()` / `SetPosition(...)` / `RefreshScanner()` / background/fog compensation path is intentionally not used for playable zoom because manual QA showed it mixes panorama semantics into normal play.
- Failure behavior: leases may report pending while the main camera is unavailable; runtime refresh retries the orthographic write. DTMAPI arbitrates active leases by highest priority, then latest update order. Releasing the active lease falls back to the next active lease or restores vanilla `1x`. `SaveLoaded`, `ReturnedToTitle`, explicit release/reset, and environment-camera transitions restore or reapply only the orthographic-size playable-view state. Feature dispatch exceptions are isolated by the GameBridge feature host and recorded under `DTMAPI.GameBridge.Feature.Camera`. UI scale remains unchanged.
- Mods/tests depending on it: `DTMAPI.ZoomMod`, smoke harness `-AutoExerciseZoom` / `Smoke.CameraPlayable`.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2 required.
  - Latest log line: `GAME-SMOKE/20260611-031502` includes `Feature.Camera = ready`, `Camera.ViewEnvironmentLifecycle = experimental`, `Camera.ViewApi = contract`, `Camera.ZoomApi = obsolete-compatibility`, `Smoke.CameraPlayable = verified`, `Smoke.Zoom = verified`, `Smoke.DiagnosticsSnapshot = verified`, and smoke result `DiagnosticsReportExport=Passed`; prior diagnostics snapshot detail evidence remains `GAME-SMOKE/20260610-043619`.
  - 2026-06-12 native-owner review: `docs/reviews/api/2026/20260612-camera-background-native-owner-review.md` keeps background/fog/panorama synchronization deferred. `DolocAPI.LoadBackground`, `BackgroundRenderer`, `BackgroundLayerRenderer`, `EnvCovariantController`, and `DepthFogController` are separate native owners from the playable orthographic-size lease, so no runtime patch was added in this batch. The bottom-layer branch records this deferral in update `20260612-0012`; Steam HookProbe regression `GAME-SMOKE/20260612-170928` still verifies `Feature.Camera=ready`, `Camera.ViewApi=vanilla/contract` lifecycle statuses, SaveLoaded, clean exit, and no leftover process.
  - Latest screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-040940`; camera screenshots, telemetry, and summary under `DTMAPI-evidence/CAMERA-PLAYABLE/20260610-041023`; report zip pointer `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-041125.zip`.
  - Manual QA gate: `docs/reviews/manual-qa/2026/20260610-0006-cameraview-manual-play-gate.md` is pending user confirmation for 2x/4x true-input movement, background flicker, native clamp, building transition, return-to-title reload, and ZoomMod hotkey/config interaction; the 2026-06-11 handoff keeps the gate pending, adds `docs/goals/2026/20260611-0001-cameraview-manual-play-handoff.md`, and uses `GAME-SMOKE/20260611-031502` as the latest supporting automated evidence only.
  - Latest case-file validation: `GAME-SMOKE/20260609-110528` plus `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/CameraPlayableSmokeCase.cs` with the same content hash as the previous `Smoke/CameraSmoke.cs`; result schema and screenshot/evidence names stayed unchanged.
  - Prior feature-host split log line: `GAME-SMOKE/20260609-031302` on `Refactor` includes `Smoke exercise CameraPlayable OK`, active 4x `DTMAPI.ZoomMod` lease, fallback 2x `DTMAPI.CameraViewCompetingSmoke` lease after high-priority release, reset to 1x, `nativeRefresh=not-called-playable`, `uiScale=unchanged`, `Smoke.CameraPlayable = verified`, and `Smoke.Zoom = verified`.
  - Prior feature-host split screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260609-031302`; camera screenshots, telemetry, and summary under `DTMAPI-evidence/CAMERA-PLAYABLE/20260609-031342`; report zip `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260609-031339.zip`.
  - Log line: `GAME-SMOKE/20260608-150914` includes `Smoke exercise CameraPlayable OK`, active 4x `DTMAPI.ZoomMod` lease, fallback 2x `DTMAPI.CameraViewCompetingSmoke` lease after high-priority release, reset to 1x, `nativeRefresh=not-called-playable`, `uiScale=unchanged`, `Smoke.CameraPlayable = verified`, and `Smoke.Zoom = verified`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260608-150914`; camera screenshots and summary under `D:\Steam\steamapps\common\Doloc Town\DTMAPI\evidence\CAMERA-PLAYABLE\20260608-150952`.
- Regression cases: CAMERA-PLAYABLE, CAMERAVIEW-MANUAL-QA-GATE-20260610, CAMERA-HOOK-OWNER-FEATURE-20260609, GAMEBRIDGE-FEATURE-HOST-HARDENING-20260609, ZOOM-030-F, ZOOM-042-API-REBUILD

## Hook: Camera.ZoomApi

- Status: obsolete-compatibility
- Public surface: `ICameraZoomApi`, `CameraZoomOptions`, `CameraZoomRegisterResult`, `CameraZoomResult`, `CameraZoomState`, and `GetSnapshot(string uniqueId)`.
- Game build: 23465763 workshop
- Game method/type: compatibility wrapper over `ICameraViewApi`; no direct CameraController/background/fog/scanner owner path.
- Patch type: API redirect only.
- Implementation owner: `DolocTownGameBridge` feature-host dispatch calls `CameraFeature.RegisterApis(...)`; `CameraFeature` registers `ICameraZoomApi` through `CameraZoomCompatibilityService`; `DolocTownExperimentalBridgeApi` no longer implements this obsolete compatibility API.
- Why this point: existing migrated mods can continue compiling while moving to lease-based playable camera view. New code should use `ICameraViewApi`.
- Failure behavior: calls are redirected to a per-owner compatibility lease. Obsolete `CameraZoomOptions.RefreshCameraController`, `CompensateBackground`, `CompensateDepthFog`, and `RefreshScanners` are ignored for playable zoom.
- Mods/tests depending on it: legacy callers only.
- Evidence:
  - Build: 2026-06-09 Release build/test passed with 0 warnings and 0 errors.
  - Save: n/a for compatibility wrapper by itself; `CAMERA-PLAYABLE` smoke validates the real playable path.
  - Log line: `GAME-SMOKE/20260609-031302` includes `Camera.ZoomApi = obsolete-compatibility`.
  - Screenshot/report: use `Camera.ViewApi` evidence from `GAME-SMOKE/20260609-031302` instead.
- Regression cases: CAMERA-PLAYABLE

## Hook: Inventory.ChestLocatorEnhancer

- Status: experimental
- Public surface: `IChestLocatorEnhancerApi`, `ChestLocatorEnhancerOptions`, `ChestLocatorEnhancerRegisterResult`, and `ChestLocatorEnhancerState`.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.GameData.ArchiveDataHandle.GetAvailableInventories(Vector2Int anchor, Vector2Int area, bool useBox)`, native `LinearInventory[]`, reflected current/root/farm/building-room enumeration, shared `Case` inventories, and shared `StorageShelf` item boxes.
- Patch type: Harmony postfix over the native inventory-array return value. `ChestLocatorEnhancerFeature` registers `IChestLocatorEnhancerApi` through `ChestLocatorEnhancerService`; `ChestLocatorEnhancerHookBridge` owns the patch installation. `Smoke/Cases/ChestLocatorEnhancerSmokeCase.cs` owns the `Smoke.ChestLocatorEnhancer` focused case implementation. GameBridge appends native inventory instances; ordinary mods register policy only and do not own reflection/Harmony traversal.
- Why this point: native recipe/material code already calls `CountItem`, `MaxCostItem`, and `TryCostItem` extension methods over the available-inventory array. Extending the array keeps native transaction behavior while letting shared chests outside the immediate room participate.
- Failure behavior: if the postfix is missing, the API state stays configured/pending and native behavior is unchanged. DTMAPI merges all enabled owner policies before each native callback: `Enabled` is any enabled owner; `IncludeSharedCases`, `IncludeSharedStorageShelfBoxes`, and `VerboseLogging` are any enabled true; `RespectNativeAutoUseBoxSetting` is all enabled true, so any owner can opt into forced shared box scanning. Effective owners and merged booleans are reported through existing state messages and runtime summaries. Inventories are deduped from live native objects on each callback; no cross-frame inventory cache or raw game type is exposed through public DTOs.
- Mods/tests depending on it: `DTMAPI.ChestLocatorEnhancerMod`, smoke harness `-AutoExerciseChestLocatorEnhancer`.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2.
  - Latest log line: `GAME-SMOKE/20260610-050506` logs `Feature.ChestLocatorEnhancer = ready`, `ChestLocatorEnhancer API register success=True`, `Inventory.ChestLocatorEnhancer = verified`, `ChestLocatorEnhancer inventories owner=DTMAPI.ChestLocatorEnhancerMod, effectiveOwners=DTMAPI.ChestLocatorEnhancerMod, includeSharedCases=True, includeSharedStorageShelfBoxes=True, respectNativeAutoUseBox=True, verboseLogging=True, useBox=True, nativeAutoUseBox=True, base=1, appended=5, roots=1, equipments=283, sharedCases=5, sharedStorageBoxes=0`, and `Smoke.ChestLocatorEnhancer = verified` with `item=dtmapi_mine, baseline=0, afterPlace=3, afterCost=1`.
  - Dual-owner policy unit evidence: `DTMAPI.UnitTests` method `ChestLocatorPoliciesMergeEnabledOwners` verifies deterministic `effectiveOwners=DTMAPI.Tests.ChestPolicyA|DTMAPI.Tests.ChestPolicyB`, any-true `IncludeSharedCases` / `IncludeSharedStorageShelfBoxes` / `VerboseLogging`, and all-true `RespectNativeAutoUseBoxSetting` behavior through existing `LastMessage`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-050506`; result has `ChestLocatorEnhancer=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`; evidence zip is `docs/debug/evidence/GAME-SMOKE/20260610-050506.zip`. This ChestLocator-only smoke did not export a fresh runtime report zip; its stale `latest-report.txt` pointer is not cited as branch report evidence.
- Regression cases: CHESTLOCATOR-SMOKE-CASE-SPLIT-20260610, CHESTLOCATOR-MERGED-POLICY-20260610, CHESTLOCATOR-030-G, CHESTLOCATOR-FEATURE-SPLIT-20260610

## Hook: Farming.StrongPlantingGun

- Status: experimental
- Public surface: `IStrongPlantingGunApi`, `StrongPlantingGunOptions`, `StrongPlantingGunRegisterResult`, and `StrongPlantingGunState`.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.ItemFarmingGun` constructors, `ItemFarmingGun.OnUseAsTool`, `DolocTown.FarmingGunUiState.HandlePlaceToOtherSide/HandleSwapOneItem`, official `ItemFunctionFarmingGun` capacity fields, native `LinearInventory`, and official private farming-gun `CheckCanInteract` / `DoInteract` basin checks.
- Patch type: Harmony constructor postfix and tool/UI transfer prefixes owned by `StrongPlantingGunHookBridge`. `StrongPlantingGunService` preserves or expands native inventory capacity only up to the verified fixed three-slot seed/film/fertilizer contract and routes those slots through native use checks; ordinary mods register policy only. Wider slot counts and real range expansion are not supported by this contract.
- Why this point: the official farming gun already owns basin area selection and per-item plant/film/fertilizer checks. Expanding its storage and delegating to those checks keeps slot behavior compatible with native farming rules instead of hand-rolling crop placement.
- Failure behavior: if tool/UI hooks are missing, registration reports configured/pending and native one-slot behavior remains unchanged. Inventory expansion is scoped to official farming gun instances, UI transfer hooks preserve native backpack cost/place transactions, and public DTOs expose only policy/status/count telemetry. Disabled state restores native one-slot behavior, but already-expanded visible UI slots may remain inactive until re-equip/restart; the migrated config UI now warns not to store items there while disabled.
- Mods/tests depending on it: `DTMAPI.StrongPlantingGunMod`, smoke harness `-AutoExerciseStrongPlantingGun`.
- Evidence:
  - Build: 2026-06-10 Release build/test passed with 0 warnings and 0 errors after the feature split.
  - Save: local slot 3 / index 2.
  - Log line: `GAME-SMOKE/20260610-101436` logs `Feature.StrongPlantingGun = ready`, `Farming.StrongPlantingGun = experimental`, `StrongPlantingGun API register success=True ... toolHook=True uiHook=True`, `StrongPlantingGun use owner=DTMAPI.StrongPlantingGunMod, slots=3, equipments=1, seedActions=1, filmActions=1, fertilizerActions=1, waterActions=0, consumed=3`, `Farming.StrongPlantingGun = verified`, and `Smoke.StrongPlantingGun = verified ... capacities=inventory:3/total:3/line:3 ... basinState=planted:True,protected:True,fertilized:True`.
  - Smoke case split: `GAME-SMOKE/20260610-122933` logs the same `Feature.StrongPlantingGun = ready`, `Farming.StrongPlantingGun = verified`, and `Smoke.StrongPlantingGun = verified` statuses after moving only the StrongPlantingGun smoke body/helpers into `Smoke/Cases/StrongPlantingGunSmokeCase.cs`; report `dtmapi-report-20260610-123010.zip`.
  - 2026-06-11 Manual QA Batch 1: service normalization now forces `SlotCount=3`, the migrated config no longer exposes unsupported slot count/range controls, and smoke `GAME-SMOKE/20260611-214654` records `StrongPlantingGun=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, clean process/fatal checks, and report `dtmapi-report-20260611-214731.zip`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260610-101436` and report `dtmapi-report-20260610-101512.zip`; result has `StrongPlantingGun=Passed`, `SaveLoaded=Passed`, `HookProbe=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Regression cases: STRONGPLANTINGGUN-FEATURE-SPLIT-20260610, STRONGPLANT-030-H

## Hook: Crops.HarvestingApi

- Status: experimental
- Public surface: `ICropHarvestingApi`, `CropHarvestRequest`, `CropHarvestResult`, `CropHarvestTargetResult`, `CropHarvestScope`, `CropHarvestTargetKind`, and `CropHarvestTargetStatus`.
- Game build: 23465763 workshop
- Game method/type: `DolocTown.PlantBasin.CouldHarvest`, `DolocTown.PlantBasin.IsCropMature`, `DolocTown.PlantBasin.Harvest(bool putInBackpack, bool sendMessage)`, diagnostic `DolocTown.Crop.isMature`, farm-root equipment containers, and farm building-room traversal.
- Patch type: explicit GameBridge API request through `CropHarvestingFeature` / `CropHarvestingService`; no Harmony patch is installed for ordinary crop harvesting.
- Why this point: official `PlantBasin.Harvest` owns crop output, broadcast, crop after-harvest state, and basin cleanup. DTMAPI should delegate to this native responsibility instead of hand-spawning harvested items or exposing raw `PlantBasin` / `Crop` objects to ordinary mods.
- Failure behavior: overlapping batches return `Busy`; no mature executable targets are successful no-ops; non-mature or already-harvested targets are reported without mutation; `TreeBasinCrop`/cocoa-style `PlantBasinTree` and `GrassForageBasin` are classified as scan-only/unsupported instead of executed; native invocation failures are recorded as diagnostics and `NativeHarvestFailed`.
- Mods/tests depending on it: `Yuuka.DTMAPI.AutoHarvest` sample mod, explicit-install developer-only `DTMAPI.CropHarvestingQaMod` manual QA fixture, smoke harness `-AutoExerciseCropHarvestingApi` using owner `DTMAPI.Smoke.CropHarvesting`.
- Evidence:
  - Build: 2026-06-12 Release build/test passed with 0 warnings and 0 errors.
  - Save: local slot 3 / index 2.
  - Manual QA handoff: `docs/reviews/manual-qa/2026/20260612-0003-crops-harvesting-real-field-manual-qa.md` is pending user confirmation and uses `CropHarvestingQaMod` for real-field ordinary/vine/mushroom-bag/bush/tree-basin/grass-forage checks. The fixture is not installed by default developer-local installs; use `install-to-game.ps1 -InstallQaFixtures`, then use the DTMAPI Settings buttons or manually bind optional hotkeys.
  - Cleanup log line: `GAME-SMOKE/20260612-101549` logs `CropHarvesting API Scan owner=DTMAPI.Smoke.CropHarvesting ... mature=0 ... failed=0`, then `Harvest owner=DTMAPI.Smoke.CropHarvesting ... mature=1 harvested=1 failed=0`, `CropHarvestingApi=Passed`, and `CropHarvestingApiEvidence=Passed`.
  - Hardening log line: `GAME-SMOKE/20260612-072544` logs `CropHarvesting API Scan ... mature=0 harvested=0 skipped=172 failed=0 targetFilter=1` and `noTargetScan={success=True ... mature=0 ... failed=0}`, then logs the ordinary transient `PlantBasinSimple` path with `scan={success=True ... mature=1 ... failed=0}`, `harvest={success=True ... harvested=1 ... failed=0}`, `Crops.HarvestingApi = verified`, and `Smoke.CropHarvestingApi = verified`.
  - Log line: `GAME-SMOKE/20260612-062154` logs `Feature.CropHarvesting = ready`, `Crops.HarvestingApi = scan-verified`, `Crops.HarvestingApi = verified`, and `Smoke.CropHarvestingApi = verified` with a transient `PlantBasinSimple`, `seed_endyam`, native planting setup, `mature=False->True`, `scan={success=True ... mature=1 harvested=0 skipped=171 failed=0 targetFilter=1}`, and `harvest={success=True ... mature=1 harvested=1 skipped=171 failed=0 targetFilter=1}`.
  - Screenshot/report: `docs/debug/evidence/GAME-SMOKE/20260612-062154`; process/fatal checks say no `DolocTown.exe` and no fatal popup.
- Regression cases: CROPS-HARVESTING-API-20260612

## Fishing Wait native reel acceptance (0.5.3-alpha)

- Game build: `23762374_public_C416D4`.
- Game method: protected `DolocTown.AgentStateFishingWait.NextState()`.
- Patch type: Harmony Postfix `DolocTownHookCallbacks.FishingWaitNextStatePostfix(object __instance, object __result)`; required by `FishingAutomationHookBridge.HooksReady` as `Wait.NextState`.
- Why this point: the native input branch calls `DolocAPI.CostEnergy(FishingEnergyCost)` before returning Battle or Pull. Input-getter consumption alone cannot confirm that transaction. A different returned state for the same pending Wait instance confirms the native effect entered synchronously before another frame can rearm the 500 ms visible edge.
- Failure behavior: callback exceptions are isolated. A null/same-state result or unrelated Wait instance does not confirm. Confirmed state clears pending input and is idempotent. The frozen legacy executor is a no-op for this seam because it does not use visible-edge retries.
- Tests/evidence: delayed-phase fake-clock injection advances 750 ms after one acceptance and verifies one simulated energy/reel effect, no rearm, no second input consumption, zero retry, and rejected duplicate confirmation. Final no-fish runtime baselines `GAME-SMOKE/20260711-080312` and `081532` verify complete Hook readiness plus bounded cleanup; no extra fish was run.
- Regression case: `AUTOFISHING-RUNTIME-MEMORY-NATIVE-REEL-20260711`.
- Update: `docs/updates/2026/20260711-0003-runtime-memory-trend-native-reel-acceptance.md`.
