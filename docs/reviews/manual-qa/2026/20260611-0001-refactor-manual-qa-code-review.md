# Manual QA Review: Refactor Manual QA Code Review

## Review Header

- Time: 2026-06-11 21:16:16 +08:00
- Source: User manual QA feedback in this thread on branch `Refactor`; local code/docs inspection; read-only report zip path check.
- Scope: Manual QA record and code-path review only. No runtime, hook, UI, gameplay, package, or goal implementation was performed in this step.
- User constraints: Switch to/use `refactor` branch, preserve the manual QA groups A-F, record the hand-test content, and perform code review before the next bottom-layer refactor work.
- Related goal/update/debug records: `docs/workflows/codex-feedback-to-goal.md`, `docs/reviews/README.md`, `docs/reviews/templates/manual-qa-review.md`, `docs/reviews/manual-qa/2026/20260607-0002-ui-save-mine-animal-refactor-review.md`, `docs/reviews/manual-qa/2026/20260610-0006-cameraview-manual-play-gate.md`, `docs/debug/regressions/smoke-matrix.md`, `docs/api/public-api-matrix.md`, `docs/hook-map/README.md`, `docs/updates/INDEX.md`.
- Files/docs inspected: `AGENTS.md`, `PROJECT.md`, `readme.md`, `docs/workflows/codex-feedback-to-goal.md`, `docs/goals/README.md`, `docs/reviews/README.md`, `docs/reviews/templates/manual-qa-review.md`, `docs/updates/INDEX.md`, `docs/debug/INDEX.md`, `docs/debug/regressions/smoke-matrix.md`, `docs/hook-map/README.md`, `docs/api/public-api-matrix.md`, `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`, `src/DTMAPI.Core/Manager/DtmManagerViewModels.cs`, `src/DTMAPI.Core/Manager/ManagerPageRowFormatter.cs`, `src/DTMAPI.GameBridge.DolocTown/Features/Camera/*.cs`, `testmods/ZoomMod/ModEntry.cs`, `testmods/ZoomMod/README.md`, `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/*.cs`, `testmods/AutoFishingMod/ModEntry.cs`, `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/*.cs`, `testmods/MoreSavesMod/ModEntry.cs`, `src/DTMAPI.GameBridge.DolocTown/Features/StrongPlantingGun/*.cs`, `testmods/StrongPlantingGunMod/ModEntry.cs`, `src/DTMAPI.GameBridge.DolocTown/Features/AnimalViewer/*.cs`, `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`, `testmods/AnimalHusbandryProgressMod/ModEntry.cs`, `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/*.cs`, `src/DTMAPI.GameBridge.DolocTown/Features/OilCoalDrop/*.cs`, `src/DTMAPI.GameBridge.DolocTown/Features/ChestLocatorEnhancer/*.cs`.
- Not inspected: A live game rerun, the contents of the report zip, new screenshot files for this exact turn, and old private DLK implementation code. The report zip path was checked for existence only.

## Issue Review

### Manual Group A: Manager UI / Export Report

#### A1. Title Settings Entry

Original feedback:

- Good: all pages open; Status can show summary; Logs can show report status; no UI crash; no fatal window; no leftover `DolocTown.exe` after exit.
- Possible issue: Mods count may be large and maybe only 16 are shown, though the user currently only has 17; the hook page behaves similarly.

Screenshot/log transcription:

- This turn did not include new image files. The user labels this group as figures 1-3 and reports the visible Manager UI state in text.
- The reported process state after exit is clean.

Review record:

- User-confirmed facts: Manager title entry opened all expected pages; Status and Logs surfaces were readable; no fatal window, UI crash, or leftover process was observed.
- Screenshot/log observations: No new screenshot file was available to inspect in this turn; observations are user-provided text.
- Code/doc facts inspected: `ReflectedTitleMenuSettingsUi.RenderMods` hard-caps Mods rows at `rowLimit = 16` and formats the count as `Mods: showing first N of total`. `RenderHooks` and `RenderFeatures` cap their rows at 17. `ManagerPageRowFormatter.FormatShowingFirst` intentionally writes `showing first ... of ...`.
- Codex inference: The possible "only 16 mods" behavior is real by design in the current compact Manager MVP, not a crash. It is acceptable for current support summary visibility, but it is not a full inventory UI. If support needs all rows, Manager UI needs paging, scrolling, selected-row details, or export/copy affordances.
- Ownership: Bootstrap Manager UI plus Core Manager view model formatting.
- Root-cause hypotheses: Compact first-N Manager pages were added for Developer Preview safety and density; they do not yet have a list viewport or row navigation.
- Rejected/unproven hypotheses: No evidence that Manager failed to discover the 17th mod. The likely issue is display truncation, not mod loading.
- Required downstream updates: No immediate implementation goal was generated in this review-only turn. Future Manager UI enhancement should cite this record if full-row listing is required.
- Acceptance checks: A future full-list fix should show or expose every mod/hook/feature row while still showing total count, without overflowing the title UI.
- Blocker conditions: Do not claim Manager full-list support from first-N smoke evidence alone.

#### A2. Export Report

Original feedback:

- `Export status = exported`
- `Exported report path = D:\Steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260611-195533.zip`
- `Snapshot report path = D:\Steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260611-195533.zip`
- `Path match = matched | snapshot report: ready`
- The zip was really generated.

Screenshot/log transcription:

- User-provided text states the exported path and snapshot path match exactly.
- Read-only local path check confirmed the zip exists at the reported path, length `93629`, last write time `2026-06-11 19:55:33`.

Review record:

- User-confirmed facts: Report export succeeded, Manager Logs displayed `exported`, exported path matched snapshot path, and a zip file exists.
- Screenshot/log observations: No new screenshot file was available; the path and status are preserved above as text.
- Code/doc facts inspected: `RenderLogs` calls `runtime.UI.ExportLogs()`, reads `LastManagerReportExport`, and displays `Export status`, `Exported path`, `Snapshot report`, and `Path match`. `ManagerLogsPageState.GetPathMatchStatus` reports `matched` when `SnapshotReportPathMatched` is true and an export path exists.
- Codex inference: A2 passes both user manual QA and a local file existence check. This is valid Manager support evidence, not gameplay API evidence.
- Ownership: Core diagnostics export and Bootstrap Manager Logs page.
- Root-cause hypotheses: No defect indicated in this item.
- Rejected/unproven hypotheses: The zip contents were not inspected, so this record confirms existence/status/path match but not archive completeness.
- Required downstream updates: None for implementation; keep this as manual support evidence for Manager UI.
- Acceptance checks: Future report export changes should preserve exported status, path match, real zip creation, and no fatal UI behavior.
- Blocker conditions: If future exports show `matched` without a real file, treat that as a Manager Logs regression.

### Manual Group B: ZoomMod / CameraView

#### B1-B3. 2x/4x Real Movement, Vertical Movement, Background Flicker

Original feedback:

- 2x real movement and 4x real movement follow normally, character stays centered, boundary clamp is normal.
- No jumping and no flicker.

Screenshot/log transcription:

- No new screenshot file was available in this turn. User manual observations are the evidence.

Review record:

- User-confirmed facts: Playable 2x/4x CameraView movement, centering, clamp, and visible stability passed in manual testing.
- Screenshot/log observations: No new screenshot file available.
- Code/doc facts inspected: Camera diagnostics publish `Camera.ViewApi` as an orthographic-size-only playable camera view contract. `testmods/ZoomMod/README.md` states the playable zoom path writes only the world camera orthographic size and does not call `CameraController.RefreshResolution`, `CameraController.SetPosition`, `DolocAPI.RefreshScanner`, or background/fog panorama compensation.
- Codex inference: The current CameraView manual gate can treat movement/clamp/flicker as user-passed for these scenarios, while keeping the API Experimental.
- Ownership: GameBridge CameraView service and ZoomMod as consumer.
- Root-cause hypotheses: The safer orthographic-size-only approach avoids camera position fights and clamp regressions in these manual scenarios.
- Rejected/unproven hypotheses: This does not prove background/fog synchronization or UI scale/click mapping correctness.
- Required downstream updates: If this manual evidence is used for CameraView promotion or release notes, cite this review as user manual evidence and keep Experimental status unless API matrix criteria are separately satisfied.
- Acceptance checks: Repeated 2x/4x movement in third save should remain centered, clamp correctly, and avoid jump/flicker.
- Blocker conditions: Do not promote CameraView to stable only from this manual pass; background sync is explicitly outside current implementation.

#### B4. Return To Title And Background Sync

Original feedback:

- Returning to title keeps zoom normal, no leftover 4x, no camera-position abnormality, and no black screen/small-frame abnormality.
- Keeping zoom across entering/exiting buildings is expected design, and no obvious visible problem occurred.
- Extra issue: the background still uses the original background and is not synchronized/calculated/scaled.

Screenshot/log transcription:

- No new screenshot file was available. User reports the return-title and building-transition behavior in text.

Review record:

- User-confirmed facts: Title return reset behavior is good; no black screen/small-frame bug was observed; building transitions preserving zoom are acceptable. Background sync remains visually unsolved.
- Screenshot/log observations: No new screenshot file available.
- Code/doc facts inspected: `ZoomMod` resets zoom on `ReturnedToTitle` and `SaveLoaded`. Camera diagnostics explicitly say background/fog compensation is not called. The compatibility state still exposes historical fields such as background/fog/scanner statuses, but the current playable path is documented as orthographic-only.
- Codex inference: The background issue is not a newly discovered regression; it is a known non-goal or limitation of the current safe CameraView contract. If background sync becomes a requirement, it is a separate GameBridge camera/panorama design problem, not a quick ZoomMod config tweak.
- Ownership: GameBridge Camera/CameraView, potentially panorama/background native-owner research.
- Root-cause hypotheses: Background layers are owned by separate native panorama/fog/scanner logic and are not recomputed when only `mainCamera.orthographicSize` changes.
- Rejected/unproven hypotheses: No evidence that return-to-title cleanup is failing now.
- Required downstream updates: Future background sync work should start with native-owner review before runtime edits.
- Acceptance checks: A future background-sync fix must prove foreground movement, bounds clamp, building transitions, title return, and background/fog alignment together.
- Blocker conditions: Do not reintroduce old camera controller writes unless native-owner review shows they are safe across transitions.

### Manual Group C: AutoFishing / FishingAutomation

#### C1. Positive Automation Paths

Original feedback:

- Auto cast succeeds.
- Instant bite succeeds.
- Movement cancel succeeds.
- Without a rod or without selecting a rod, automation does not execute, does not error, does not block input, and works after switching to a rod.

Screenshot/log transcription:

- No new screenshot file was available. User manual observations are recorded as text.

Review record:

- User-confirmed facts: Auto-cast, instant bite, move-cancel, no-rod safety, and later rod recovery passed.
- Screenshot/log observations: No screenshot/log attached in this turn.
- Code/doc facts inspected: `UpdateFishingAutoCast` requires an enabled policy, normal game state, an available fishing pool, and a selected fishing rod. `ResolveFishingRodForAutomation` returns null when `RequireSelectedFishingRod` is true and no selected rod exists. `NormalizeFishingAutomationOptions` forces `AutoRecast = true` and `RequireSelectedFishingRod = true`. `SetAutomation` enables/disables the GameBridge policy through F6 and disables on return to title.
- Codex inference: The no-rod/no-selected-rod behavior matches code intent and current API safety policy. Positive user evidence supports the current Experimental implementation but does not close the option-semantics issues below.
- Ownership: GameBridge FishingAutomation service and AutoFishingMod hotkey/config consumer.
- Root-cause hypotheses: The safe-selected-rod policy avoids accidental inventory scans and explains why non-selected rods do not trigger automation.
- Rejected/unproven hypotheses: No evidence of input deadlock or crash from no-rod states.
- Required downstream updates: None for these pass paths unless future fishing goal changes option semantics.
- Acceptance checks: Auto-cast and instant bite must still work after any fishing refactor; no-rod states must remain no-op/no-error/no-input-lock.
- Blocker conditions: Do not "fix" fishing options in a way that makes no-rod states invoke native rod methods on null.

#### C2. MiniGame, Skip, Animation, And Reel Semantics

Original feedback:

- Auto-complete minigame currently reads the first animation bar then skips the minigame; it does not complete according to minigame progress.
- Skip minigame alone is ineffective.
- Enabling both auto-complete minigame and skip minigame is ineffective.
- Animation acceleration for reel/cast is ineffective.
- Without instant bite, automation does not auto reel when the fish bites; enabling auto fishing makes it reel on bite.

Screenshot/log transcription:

- No new screenshot/log file was attached. User reports behavior from manual play.

Review record:

- User-confirmed facts: Several config-visible fishing options do not behave as their labels imply, especially skip/minigame/animation acceleration.
- Screenshot/log observations: No screenshot/log attached in this turn.
- Code/doc facts inspected: `TryApplyFishingMiniGameAutomation` immediately returns when `!AutoCompleteMiniGame || SkipMiniGame`; otherwise it waits until the minigame has been visible for at least `0.75` seconds, then writes `currentGameStatus = Success`. `ApplyFishingWaitAutomation` returns unless `InstantBite` is enabled. `TryAdvanceFishingBite` goes to `AgentStateFishingPull` only when `SkipMiniGame` is true or the caught proto is not a fish; otherwise it goes to `AgentStateFishingBattle`. `TryApplyFishingAnimationSpeed` only runs on observed `Cast`/`Pull` phases and only if it can reflect writable animator speed fields from `body.animator` or `fishRodRenderer._animator`.
- Codex inference: The user's observations align with current code. `AutoCompleteMiniGame` is a delayed status-forcing shortcut, not a real minigame solver. `SkipMiniGame` is not an independent minigame hook; it only affects the instant-bite advance path. When both skip and auto-complete are enabled, the auto-complete hook is explicitly bypassed because `SkipMiniGame` is true. Fast animations may be no-op if the phase source lacks the expected animator members or if native timings are elsewhere.
- Ownership: GameBridge FishingAutomation service plus AutoFishingMod config labels/tooltips.
- Root-cause hypotheses: Current implementation was built as safe native-state forcing with smoke hooks, while the config exposes higher-level player expectations. The runtime and UI labels are out of sync.
- Rejected/unproven hypotheses: Not proven that Harmony patches are missing; the observed behavior can be explained by current conditional logic.
- Required downstream updates: A future fishing goal should either implement the advertised semantics or rename/disable unsupported options. It should update API matrix and smoke matrix because fishing remains Experimental.
- Acceptance checks: Each visible option must have one observable behavior: skip alone skips, auto-complete either truly completes or is labeled as delayed success forcing, fast animation visibly changes cast/reel speed or is hidden, and non-instant-bite auto reel behavior is specified.
- Blocker conditions: Do not claim a fishing option fixed from status logs alone; manual play must prove the option-specific behavior.

#### C3. No Rod / Rod Not Selected

Original feedback:

- Does not execute, does not error, does not block input, and switching to a rod allows auto fishing.

Screenshot/log transcription:

- No screenshot/log attached.

Review record:

- User-confirmed facts: No-rod safety behavior passed.
- Screenshot/log observations: No screenshot/log attached.
- Code/doc facts inspected: The selected-rod requirement is normalized to true both in AutoFishingMod config and GameBridge options.
- Codex inference: This pass case should be preserved as a safety invariant during fishing refactors.
- Ownership: GameBridge FishingAutomation and AutoFishingMod.
- Root-cause hypotheses: No defect indicated.
- Rejected/unproven hypotheses: No evidence of fallback inventory rod search in current mod because normalization disables it.
- Required downstream updates: Preserve in future fishing acceptance checks.
- Acceptance checks: No rod and wrong selected item must remain harmless no-ops.
- Blocker conditions: Any refactor that re-enables inventory rod search must explicitly test no-rod, wrong item, and manual input recovery.

#### C4. Panel Switches And F9 Info

Original feedback:

- There is no DTMAPI panel switch option for use by other mods, but disabling sub-features works and does not affect manual fishing. The main control is in-game hotkey.
- Extra: F9 fishing info is absent, but it is not urgent; the oldest info was weak.

Screenshot/log transcription:

- No screenshot/log attached.

Review record:

- User-confirmed facts: Manual fishing remains usable when sub-features are disabled; missing F9 fishing info is lower priority.
- Screenshot/log observations: No screenshot/log attached.
- Code/doc facts inspected: AutoFishingMod registers `InfoKey = F9`, but `OnButtonPressed` opens the mod config page and logs `fish-info boundary opened config page`. It does not implement a fishing-info overlay.
- Codex inference: F9 is currently a config-page shortcut, not an information feature. This is a known missing feature, not a broken hook.
- Ownership: AutoFishingMod UI/UX, optionally future GameBridge fishing info API if needed.
- Root-cause hypotheses: The old info feature was not rebuilt; current implementation prioritizes config/hotkey automation.
- Rejected/unproven hypotheses: No evidence that F9 input registration itself fails.
- Required downstream updates: Optional future UX task only; user marked it not urgent.
- Acceptance checks: If implemented later, F9 should show useful fishing state without disrupting manual fishing.
- Blocker conditions: Do not block the next bottom-layer refactor on F9 info unless the user promotes it.

### Manual Group D: ActionSpeed + OneAction / OilCoalDrop

#### D1. ActionSpeed

Original feedback:

- Speed changes.
- Closing/disabling restores speed.
- No animation residue.
- No unusable tools.

Screenshot/log transcription:

- No screenshot/log attached.

Review record:

- User-confirmed facts: ActionSpeed passed the manual behavior checks listed above.
- Screenshot/log observations: No screenshot/log attached.
- Code/doc facts inspected: Feature paths exist under `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed` and mod paths under `testmods/ActionSpeedMod`; no fresh defect was reported, so this review did not deep-dive implementation.
- Codex inference: Treat this group as a pass baseline and avoid carrying it into a refactor goal unless future code changes touch shared animation/action-speed lifecycle.
- Ownership: GameBridge ActionSpeed and ActionSpeedMod.
- Root-cause hypotheses: No defect indicated.
- Rejected/unproven hypotheses: No evidence of tool lock or residual animation after disable.
- Required downstream updates: None for this review.
- Acceptance checks: Future refactors touching animation/action speed should keep these manual pass points.
- Blocker conditions: A future regression here should require action lifecycle smoke/manual evidence before complete.

#### D2. OneAction

Original feedback:

- One-hit complete works.
- Wrong tool does not mis-trigger.
- Fuel/feed is normal.

Screenshot/log transcription:

- No screenshot/log attached.

Review record:

- User-confirmed facts: OneAction passed the reported manual behavior checks.
- Screenshot/log observations: No screenshot/log attached.
- Code/doc facts inspected: Feature paths are represented by `testmods/OneActionCompleteMod`; no fresh defect was reported, so this review did not deep-dive implementation.
- Codex inference: Preserve as pass baseline; do not carry it into the next refactor unless shared action hooks are touched.
- Ownership: OneActionCompleteMod and any shared GameBridge action hooks it consumes.
- Root-cause hypotheses: No defect indicated.
- Rejected/unproven hypotheses: No evidence of wrong-tool misfires in this manual run.
- Required downstream updates: None for this review.
- Acceptance checks: Future action refactors must preserve wrong-tool no-op and normal fuel/feed behavior.
- Blocker conditions: Do not accept a one-action fix if it breaks fuel/feed/native tool validation.

#### D3. OilCoalDrop

Original feedback:

- Current testing found no problem. Oil drops from coal mine, does not interfere with original drops, and does not affect other mods.

Screenshot/log transcription:

- No screenshot/log attached.

Review record:

- User-confirmed facts: OilCoalDrop passed the manual behavior checks.
- Screenshot/log observations: No screenshot/log attached.
- Code/doc facts inspected: Feature paths exist under `src/DTMAPI.GameBridge.DolocTown/Features/OilCoalDrop` and mod paths under `testmods/OilMod`; no fresh defect was reported, so this review did not deep-dive implementation.
- Codex inference: Treat as pass baseline.
- Ownership: OilCoalDrop feature and OilMod content/config.
- Root-cause hypotheses: No defect indicated.
- Rejected/unproven hypotheses: No evidence of drop table interference in this manual run.
- Required downstream updates: None for this review.
- Acceptance checks: Future drop changes should preserve original drops and compatibility.
- Blocker conditions: Do not modify official/Workshop content files for this pass case without an explicit user request.

### Manual Group E: SaveSlots / MoreSaves

#### E1. Slot Count And UI Layout

Original feedback:

- Slot count is correct.
- 18 slots or more runs off-screen and needs paging.
- 12-slot UI is normal; vanilla is 6 slots.
- Existing saves still display.

Screenshot/log transcription:

- No new screenshot file was attached in this turn. This confirms and narrows the older 24-slot overflow report: 12 is acceptable, 18+ overflows.

Review record:

- User-confirmed facts: Slot count and existing save display are correct; 12-slot UI is acceptable; 18+ overflows off-screen and needs paging.
- Screenshot/log observations: No new screenshot file attached.
- Code/doc facts inspected: `SaveSlotsService` only changes `DolocAPI.gameManager.archiveFileCount`, lets official `LocalSave` and `GameDataPanel` own archive discovery/render/load/delete/copy, and clamps requested slot count to 6-60. `SaveSlotsFeature.InstallHooks` is empty. `MoreSavesMod` default slot count is 12 and config allows 6-60. No paging/scroll hook exists in the inspected SaveSlots feature.
- Codex inference: Extra-slot storage/load path is functioning because official save infrastructure uses the raised archive count. The layout bug is expected once official static `GameDataPanel` is asked to render many slots without DTMAPI paging/scroll adaptation.
- Ownership: GameBridge SaveSlots plus official title save UI adaptation.
- Root-cause hypotheses: DTMAPI expanded the data count but has not implemented a viewport/page state over the official save grid.
- Rejected/unproven hypotheses: This manual run argues against file persistence/load being broken for extra slots.
- Required downstream updates: Future SaveSlots UI goal should cite this review and older `20260607-0002` review. It should update smoke matrix with 12 and 18+ UI evidence.
- Acceptance checks: 6, 12, and 18+ slot counts must stay contained; page/scroll controls must preserve selection, load, save, delete, and copy targeting.
- Blocker conditions: Do not reduce the feature to 12-only unless the user explicitly changes the requirement; 18+ paging is now a confirmed requirement.

#### E2. New Slot Save / Load

Original feedback:

- Fully normal; extra slots can be used freely.

Screenshot/log transcription:

- No screenshot/log attached.

Review record:

- User-confirmed facts: Extra-slot save/load works.
- Screenshot/log observations: No screenshot/log attached.
- Code/doc facts inspected: SaveSlots intentionally delegates file behavior to official `LocalSave` and `GameDataPanel` after raising `archiveFileCount`.
- Codex inference: Data-path behavior is stronger than the previous automated 12-slot-only evidence. The next SaveSlots work should focus on layout/paging, while protecting the now-user-confirmed extra-slot lifecycle.
- Ownership: GameBridge SaveSlots and official save UI wrapper/adaptation.
- Root-cause hypotheses: No data-path defect indicated.
- Rejected/unproven hypotheses: No evidence that extra slot delete/copy were tested in this item.
- Required downstream updates: Future save UI goal should include save/load/delete/copy acceptance separately.
- Acceptance checks: New slot save/load must remain fully normal after paging is added.
- Blocker conditions: Any paging fix that breaks extra-slot persistence must be treated as incomplete.

### Manual Group F: ChestLocator / StrongPlantingGun / AnimalViewer

#### F1. ChestLocator

Original feedback:

- First test found no issue.
- Consumption is normal.
- Can find materials across rooms.
- Normal after exiting save and closing.

Screenshot/log transcription:

- No screenshot/log attached.

Review record:

- User-confirmed facts: ChestLocator passed first manual checks including cross-room lookup and lifecycle exit/close.
- Screenshot/log observations: No screenshot/log attached.
- Code/doc facts inspected: Feature paths exist under `src/DTMAPI.GameBridge.DolocTown/Features/ChestLocatorEnhancer` and mod paths under `testmods/ChestLocatorEnhancerMod`; no fresh defect was reported, so this review did not deep-dive implementation.
- Codex inference: Treat as pass baseline and do not carry into a refactor goal unless future shared inventory/chest indexing changes are planned.
- Ownership: ChestLocatorEnhancer GameBridge feature and test mod.
- Root-cause hypotheses: No defect indicated.
- Rejected/unproven hypotheses: No evidence of lifecycle stale state after save exit in this manual run.
- Required downstream updates: None for this review.
- Acceptance checks: Future chest/inventory refactors should keep consumption, cross-room lookup, and exit cleanup.
- Blocker conditions: Do not mark a future chest index refactor complete without an exit/close cleanup check.

#### F2. StrongPlantingGun

Original feedback:

- Inserting three item types works.
- Placement is correct.
- Consumption is correct.
- UI does not freeze.
- Problem: after config is disabled, native behavior recovers, but the three slots remain; extra slots no longer work and only one item can be equipped.
- Current max is six slots, but it would be better fixed at three.
- Range zoom in/out succeeds, but using the gun immediately restores the original 4x4 range.

Screenshot/log transcription:

- No screenshot/log attached.

Review record:

- User-confirmed facts: Three-type placement/consumption/UI freeze checks pass. Disable cleanup and range/use consistency are defective.
- Screenshot/log observations: No screenshot/log attached.
- Code/doc facts inspected: `StrongPlantingGunService.Register` stores enabled/slot options and updates state. `TryGetStrongPlantingGunPolicy` returns only enabled policies, so disabled config stops DTMAPI tool/UI logic. `ExpandFarmingGunInventoryIfNeeded` uses `targetCapacity = Math.Max(currentCapacity, options.SlotCount)`, meaning it expands but does not shrink existing farming gun inventory. `StrongPlantingGunMod` config exposes 1-6 slots; GameBridge normalization accepts 1-12. Tool use delegates each equipment/item pair to official `CheckCanInteract` and `DoInteract`. No independent range/radius logic was found in the inspected StrongPlantingGun service path.
- Codex inference: The disable symptom is explained by one-way expansion: after disable, official/native use returns, but the already-expanded inventory stays expanded and only native single-slot behavior remains meaningful. The slot-count contract is inconsistent between mod UI (max 6), service (max 12), and user expectation (fixed 3). The range issue likely comes from preview/config state not being integrated with the official use path, which still uses native interaction checks and native range.
- Ownership: GameBridge StrongPlantingGun service/hook bridge plus StrongPlantingGunMod config surface.
- Root-cause hypotheses: There is no shrink/cleanup path for already-expanded farming guns; slot count should be fixed or constrained to the supported seed/film/fertilizer triad; range setting is either outside this feature or not applied to the native use path.
- Rejected/unproven hypotheses: No evidence that the three-slot active path is broken; user confirmed it works.
- Required downstream updates: Future StrongPlantingGun goal should decide fixed 3-slot policy, add disable cleanup or clear visual state, and define whether range is supported. Update hook/smoke docs if hooks change.
- Acceptance checks: Disable must restore both behavior and visible slot shape or document an unavoidable native limitation; supported slot count should match UI/API/user expectation; range preview and actual use must agree.
- Blocker conditions: Do not accept a fix that only hides extra slots while leaving unusable inventory state that can trap items.

#### F3. AnimalViewer

Original feedback:

- After switching animal, the special produce bar flashes `心情` once, then shows the produce.

Screenshot/log transcription:

- No screenshot/log attached. The visible symptom is a first-frame or short-lived label flicker from `心情` to the intended hidden produce.

Review record:

- User-confirmed facts: The AnimalViewer flicker persists and is repeatable on animal switching.
- Screenshot/log observations: No screenshot/log attached.
- Code/doc facts inspected: `AnimalViewerHookBridge` patches `AnimalFullInfoData(Animal)` constructor, `AnimalViewer.Show` prefix/postfix, and `AnimalPanel.RefreshViewer`. `DolocTownHookCallbacks.AnimalViewerShowPrefix` calls `PrepareAnimalProgressOverlayBeforeShow`; postfix calls `RenderAnimalProgressOverlay` and evidence recording. `RenderAnimalProgressOverlay` clones the native `moodBar` GameObject, disables the clone, sets parent/position, calls `RefreshAnimalProgressOverlayTexts(force: true)`, then activates the clone. `RefreshAnimalProgressOverlayTexts` sets `ProgressBar.SetTitle`, `SetProgress`, and child Unity text. `ApplyAnimalProgressSinglePassData` still exists but is not referenced by current search results. Smoke/evidence paths focus on final independent cloned progress rows.
- Codex inference: This is likely a lifecycle/order issue, not a missing final-state render. Native `AnimalViewer.Show` can still display the official mood row before DTMAPI's cloned produce row is fully stable, or a cloned localization/text component may briefly restore the native title when enabled. Existing smoke evidence is insufficient because final screenshot text can pass while first-frame flicker remains visible.
- Ownership: GameBridge AnimalViewer hook/runtime UI lifecycle; AnimalHusbandryProgressMod remains an ordinary consumer.
- Root-cause hypotheses: Prefix render may run before native Show finishes mutating the source mood bar; postfix render may be too late for the first visible frame; cloned text/localization components may need stripping or prefilled inactive activation guarantees.
- Rejected/unproven hypotheses: Current search did not show the legacy single-pass `moodInfo` override being called, so this is probably not the direct cause of the observed flicker.
- Required downstream updates: Future AnimalViewer goal should require first-frame/manual or video/timed screenshot validation, not only final screenshot evidence.
- Acceptance checks: Repeated animal switches must never visibly label the produce row as `心情` before correction; native mood information should remain intact separately.
- Blocker conditions: Do not mark solved from final-state smoke alone.

## Cross-Issue Summary

- Confirmed user facts: Manager pages and report export mostly pass; CameraView playable movement/title cleanup passes while background sync remains missing; AutoFishing has working auto-cast/instant-bite/no-rod safety but misleading mini-game/skip/animation semantics; ActionSpeed, OneAction, OilCoalDrop, ChestLocator pass current manual checks; SaveSlots data path works but 18+ UI overflows; StrongPlantingGun active three-slot behavior works but disable/range/slot-contract issues remain; AnimalViewer flicker persists.
- Screenshot/log facts: This turn did not provide new image files. The report zip path exists locally at `D:\Steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260611-195533.zip`.
- Code-path findings: Manager compact pages intentionally show first N rows; CameraView is orthographic-only without background/fog compensation; Fishing skip and auto-complete branches are mutually shaped by current conditionals; SaveSlots only raises official archive count; StrongPlantingGun expands one-way and delegates use to native checks; AnimalViewer final overlay rendering does not prove first-frame stability.
- Risks: Several pass items are final-state or happy-path evidence. The repeated problems are UI containment, lifecycle flicker, option semantics, and mismatch between visible config and supported behavior.
- Suggested goal shape: No implementation handoff was generated in this review-only turn. A future bottom-layer refactor goal should be split by risk unless the user explicitly wants one batch: Manager full-list/paging, Camera background native-owner research, Fishing option contract cleanup, SaveSlots paging, StrongPlantingGun disable/slot/range cleanup, AnimalViewer first-frame flicker fix.
- Items that should not be carried forward: Do not reopen ActionSpeed, OneAction, OilCoalDrop, or ChestLocator as defects from this review; they are pass baselines unless future shared refactors touch them.

## Code Review Findings

- [P2] `AutoFishing` exposes options whose behavior does not match player expectation: `SkipMiniGame` is not an independent minigame skip, `AutoCompleteMiniGame` force-sets success after a delay, and fast animation can silently no-op.
- [P2] `SaveSlots` supports extra save/load through official archive count, but has no paging/scroll layer for 18+ slots.
- [P2] `StrongPlantingGun` expands farming gun capacity one-way and has an inconsistent slot contract between user expectation, mod config, and GameBridge normalization.
- [P2] `AnimalViewer` final-state rendering evidence does not cover the reported first-frame `心情` flicker.
- [P3] `Manager UI` first-N row caps are intentional Developer Preview behavior but are not a full support listing.
- [P3] `CameraView` background sync is a known limitation of the current orthographic-only contract.

## Readme/Goal Decision

- Update goal file: no. The current user request was to record hand-test content and perform code review on `Refactor`; it did not explicitly request a new implementation handoff in this turn.
- Generate short `/goal`: no. Per `docs/workflows/codex-feedback-to-goal.md`, create a dedicated `docs/goals/YYYY/...md` and sibling `.goal.txt` only when the user asks for an implementation handoff.
- Suggested task titles if converted later: Manager full-list support; Camera background native-owner review; Fishing option semantics cleanup; SaveSlots 18+ paging; StrongPlantingGun disable/slot/range contract; AnimalViewer first-frame flicker fix; docs/smoke matrix updates.
- Completion standard for any future implementation: build/test plus third-save game smoke, user-visible evidence for lifecycle/flicker/layout, clean exit/no leftover process, and required `docs/updates`, `docs/debug`, smoke matrix, hook map, and API matrix updates where relevant.
