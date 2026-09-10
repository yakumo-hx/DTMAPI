# 20260616-0001 Player Log Diagnostics, Console, Speed, And Animal Review

## Header

- Time: 2026-06-16 Asia/Shanghai
- Source: user supplied `D:\下载\DTMAPI-logs.zip` and summarized player feedback from an external player.
- Scope: pre-implementation review feeding the active implementation goal; this record does not prove any fix.
- Constraints: keep diagnostics lightweight; do not turn every bug into per-frame logging; split ActionSpeed bottom-layer rebuild from the smaller logging/console/movement/AnimalViewer fixes.
- Review record: `docs/reviews/manual-qa/2026/20260616-0001-player-log-diagnostics-console-speed-animal-review.md`

## Issue 1: Player logs and diagnostic sufficiency

Original feedback:
- We received the first detailed independent player log package.
- Analyze whether logs are enough and whether every detail bug needs new logging.

Review:
- User-confirmed facts: the player could export a useful report after problems occurred.
- Log observations: the package contained DTMAPI latest log, BepInEx log, Unity Player log, process/fatal checks, startup analysis, screenshots/summaries, and many historical startup entries.
- Code/doc facts: `DiagnosticsService.ExportLogs()` already includes current `DTMAPI-latest.log`, retained history logs, BepInEx log, Unity Player log, install/release state, and a summary. `latest.log` rotation is already documented in `RUNTIME-LATEST-LOG-ROTATION-20260616`.
- Codex inference: the missing layer is not more volume, but better failure-shape normalization and small domain snapshots. `TargetInvocationException` should be unfolded before being stored as the only visible failure detail.
- Rejected/unproven: no evidence that default per-frame verbose logs are needed or acceptable.
- Ownership: DTMAPI Core diagnostics plus GameBridge failure reporting.
- Needs update: update record, debug/smoke matrix, and possibly API matrix diagnostics row if public wording changes.
- Acceptance: a repeated reflected failure should export root/inner exception details and the relevant domain snapshot without unbounded log growth.
- Blocker: if the root exception cannot be extracted without changing public diagnostics API, keep the change internal and document the gap.

## Issue 2: AnimalViewer/animal bell display disappears during long play

Original feedback:
- During long play, animal husbandry/bell display disappears.

Review:
- User-confirmed facts: the display can disappear after a long session.
- Log observations: the player log repeatedly records `GameBridge feature 'AnimalViewer' failed during Update` and many `Feature.AnimalViewer = failed` rows, but the visible runtime log mostly shows the outer `TargetInvocationException`.
- Code/doc facts: `AnimalViewerFeature.Update()` calls `AnimalViewerService.RefreshAnimalProgressOverlayTexts(force: false)`. That service keeps active cloned ProgressBar objects in lists and refreshes them later. `RenderAnimalProgressOverlay()` creates inactive clones, disables localization components, prefills, activates, and validates first-frame text. Existing docs prove the first-frame flicker guard but not long-session clone lifecycle recovery.
- Codex inference: stale or destroyed clone objects, changed parent UI, or stale row/data mapping can throw during later Update refresh and mark the whole feature failed. Long-run disappearance is therefore likely GameBridge UI lifecycle ownership, not the ordinary AnimalHusbandryProgress mod body.
- Rejected/unproven: no evidence that native animal husbandry values stopped updating; no evidence that the mod intentionally disabled itself.
- Ownership: `DTMAPI.GameBridge.DolocTown` AnimalViewer service/hook lifecycle.
- Needs update: hook-map AnimalViewer row, smoke matrix AnimalViewer long-lifecycle/clone recovery row, update record, API matrix wording if boundary changes.
- Acceptance: destroyed/stale cloned rows are cleared or rebuilt without failing the feature; failure details include viewer/data/clone state if recovery cannot happen.
- Blocker: if native UI object ownership cannot be safely reconstructed, leave a diagnostic-only failure snapshot and do not claim the display is fixed.

## Issue 3: Y console search input closes console when typing `Y`

Original feedback:
- Searching in the Y console fails because typing `Y` closes the console.

Review:
- User-confirmed facts: pressing `Y` while the search box is active closes the console.
- Log observations: player logs contain open/close `reason=Y` entries while search text evidence is often empty.
- Code/doc facts: `ReflectedDebugConsoleUi.Update()` closes on `ReflectedUnityInput.GetKeyDown("Y")` whenever the console is open. `DebugConsoleMod.OnButtonPressed()` also toggles on `Y` without checking whether the Unity `InputField` has focus. `CreateInput()` creates a reflected `InputField` but does not track focus.
- Codex inference: the UI host and the ordinary mod hotkey both need focus-aware behavior. The host can suppress Y-close while an input field is focused; the ordinary mod should not toggle while the console reports text input focus or active menu consumption.
- Rejected/unproven: no evidence that the input helper API itself loses Y presses.
- Ownership: Bootstrap reflected debug console UI plus DebugConsoleMod hotkey policy.
- Needs update: debug docs/smoke matrix Y-console row and update record.
- Acceptance: typing `Y` in the search field changes/keeps search text and does not close the console; Escape still closes.
- Blocker: if Unity focus cannot be read reliably, use explicit focus state from input-field pointer/select events and record the fallback.

## Issue 4: Y console right-click give path can give 10 of the wrong item

Original feedback:
- Right-click拿东西 can give 10 of another item.

Review:
- User-confirmed facts: right-click item giving can target the wrong item.
- Log observations: player logs show `Debug console right-click give hit-test item=...` followed by `requested=10`.
- Code/doc facts: each item cell registers an `EventTrigger.PointerDown` right-click path, and `Update()` separately checks `Mouse1` and performs manual screen-position hit testing over cached rectangles. The same action therefore has two target routes with different freshness and coordinate assumptions.
- Codex inference: the manual hit-test path can drift after rebuild/page/filter/layout changes and should not be a production path. Keep one reliable path, ideally the Unity event target for the actual clicked cell.
- Rejected/unproven: `IInventoryDebugApi.GiveItem()` validates and places the requested item id; current evidence points to UI target selection rather than inventory API substituting another item.
- Ownership: Bootstrap reflected debug console UI.
- Needs update: debug docs/smoke matrix Y-console mouse-give row and update record.
- Acceptance: right-click give logs one source and one item id from the clicked cell; no duplicate/manual fallback path is active in normal UI.
- Blocker: if reflected `PointerDown` cannot deliver right-click in smoke, fallback must be explicitly smoke-only or hover-bound, not stale rectangle-bound.

## Issue 5: Y console movement speed loses effect after idle or wake

Original feedback:
- Standing in place for a long time can lose speed boost; waking up can sometimes lose speed boost.

Review:
- User-confirmed facts: the configured multiplier remains expected by the player, but native speed returns to normal after lifecycle/state changes.
- Log observations: player logs show many `Movement debug speed OK` entries when manually applied, but not reapplication after native state changes.
- Code/doc facts: `DolocTownExperimentalBridgeApi.SetSpeedMultiplier()` calls `MotionAbility.SetMoveScaler(multiplier - 1)` once and stores `movementSpeedMultiplier`. `GetState()` reports that stored multiplier plus current `MoveSpeed`; no lease/reapply loop or lifecycle reapply exists.
- Codex inference: native `MotionAbility` or its scaler can be reset by sleep, reload, ability refresh, or movement-state changes. The debug API should maintain a lightweight owner lease and reapply only when the remembered multiplier is not default and current native speed/scaler appears stale.
- Rejected/unproven: no evidence that ActionSpeed mod itself causes this movement issue.
- Ownership: `IMovementDebugApi` diagnostic GameBridge implementation.
- Needs update: public API matrix diagnostics wording, smoke matrix, update record.
- Acceptance: after setting 2x/3x/4x, repeated update/lifecycle refresh reapplies the multiplier when the native state resets; reset to 1x disables the lease.
- Blocker: if native scaler cannot be read, reapply at low frequency while non-default instead of writing every frame.

## Issue 6: ActionSpeed well/planting intermittency

Original feedback:
- Well water fill accelerates only with an empty bottle selected; planting acceleration is intermittent.

Review:
- User-confirmed facts: some action-speed paths are accelerated and some fall back to native speed.
- Log observations: player logs show ActionSpeed classifications and restores, but coverage gaps remain.
- Code/doc facts: `ActionSpeedService` classifies bottle fill from selected item/equipment and currently has explicit empty-bottle assumptions in auto-fill. Planting/tool paths are spread across multiple native states and helper classes.
- Codex inference: this is a real API/GameBridge boundary problem, not a small UI or config bug. It should be handled by a separate bottom-layer native-owner rebuild starting from action state and item/equipment responsibility functions.
- Rejected/unproven: do not patch individual symptoms with broader animator writes before the owner review.
- Ownership: ActionSpeed GameBridge/native action ownership.
- Needs update: dedicated ActionSpeed review/goal before implementation, smoke matrix for well/planting states, hook map after code changes.
- Acceptance: deferred; this review only records that ActionSpeed should be split out.
- Blocker: if no native owner can be isolated, leave `IActionSpeedApi` Experimental with explicit unsupported paths.
