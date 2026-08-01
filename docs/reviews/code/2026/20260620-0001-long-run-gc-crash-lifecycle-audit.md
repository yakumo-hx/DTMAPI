# 2026-06-20 Long-Run GC Crash Lifecycle Audit

## Source

- Player crash/log follow-up from 2026-06-18 to 2026-06-20.
- Local log folders reviewed:
  - `D:\下载\DTMAPI-logs\20260619-131746`
  - `D:\下载\20260618-205113\20260618-205113`
- Player screenshots included `Fatal error in GC / Unexpected mark stack overflow` and Unity crash windows.

## Executive Conclusion

The evidence points to a real Unity/Mono GC-level crash after long play, not an ordinary C# exception and not a DTMAPI startup failure. The current logs are not enough to prove the exact object that triggers the GC mark stack overflow because previous DTMAPI exports did not include Unity native crash dump directories.

DTMAPI is still a credible participant. Its bootstrap creates a process-long static root from hook callbacks to the runtime, bridge, feature host, and loaded mod instances. That design is intentional, but it means any feature dictionary, UI clone, UnityEvent delegate, or mod callback that keeps native/Unity objects alive becomes part of the long-lived GC object graph.

Highest-priority code risks found:

1. `SaveSlotsService` can retain native save-panel UI state and pager delegates across lifecycle boundaries.
2. `EquipmentSlots` can retain cloned UI objects and UnityEvent binders when save/title/environment lifecycle resets do not pass through the render-time cleanup path.
3. `SetEnvCamera -> EnvironmentReset` is high-frequency and fanouts into all GameBridge features, including a forced machine production poll that bypasses the normal throttle.
4. `FishingAutomation` has transient native/Unity object caches and can report `auto-cast invoked` without proving the native fishing state advanced.

No reviewer found a direct infinite C# recursion path. This looks more like long-lived object graph growth or frequent lifecycle scanning than a managed `StackOverflowException`.

## Existing Log Facts

`D:\下载\DTMAPI-logs\20260619-131746`:

- DTMAPI loaded as `0.5.2-alpha / 0.5.2.0`.
- Startup was normal: `Core.Start totalMs=1116`, `Bootstrap.Awake totalMs` below the known slow-start range.
- Enabled DTMAPI mods included `DTMAPI.MoreEquipmentSlotsMod`, `DTMAPI.MoreSavesMod`, `Yuuka.DTMAPI.AutoFishing`, `Yuuka.DTMAPI.ActionSpeed`, `Yuuka.DTMAPI.AnimalHusbandryProgress`, and the third-party `com.user.dolocstorageexpansion` paper-box capacity mod.
- `Unity-Player.log` contains native crash evidence, including `Crash!!!` and `mono_gc_register_root`.
- DTMAPI log contained hundreds of environment resets and returned-to-title/lifecycle fanout entries, but no managed fatal exception immediately explaining the native crash.
- The log package did not include `%TEMP%\RedSawGames\DolocTown\Crashes`, even though Unity Player logs can point there for native dump material.

## Code-Level Findings

### Static Root and Feature Fanout

- `DolocTownHookCallbacks` stores process-long static references to `Runtime` and `Bridge` in `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`.
- `DolocTownGameBridge.Initialize()` assigns those references, and the bridge keeps feature instances for the process in `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`.
- `DolocApiSetEnvCameraPostfix()` calls `NotifyGameBridgeFeaturesEnvironmentReset("DolocAPI.SetEnvCamera")`.
- `NotifyGameBridgeFeaturesEnvironmentReset()` dispatches every feature and currently invokes runtime automation before feature fanout.

Confirmed risk:

- The root chain is intentionally process-wide, but it amplifies every retained Unity/native object below bridge features or mod event handlers.
- `SetEnvCamera` is frequent during room/environment changes and is therefore an important pressure point for allocation, scanning, and cleanup correctness.

Not yet proven:

- This fanout alone does not prove a leak. It is a multiplier for leaks elsewhere.

### SaveSlots UI State

- `SaveSlotsService` keeps `officialSaveUiStates` keyed by native panel object in `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsService.cs`.
- `SaveSlotsPanelPagingState` stores pager roots and event binders.
- Pager button callbacks capture `panel` and `state`.
- `SaveSlotsFeature.ReturnedToTitle()` and `SaveSlotsFeature.EnvironmentReset()` are currently empty.

Confirmed risk:

- If any expanded/paged save UI state is created, old native panels, pager roots, and callbacks can remain strongly reachable after title/environment transitions unless `RestoreOfficialSavePanel()` runs.
- Current MoreSaves fixed 12-slot mode lowers the chance of this path, but the code path remains unsafe for future 18+/24 behavior or third-party interaction.

Recommended fix:

- Add `ClearOfficialSaveUiStates(reason)` and call it from `SaveLoaded`, `ReturnedToTitle`, and `EnvironmentReset`. It should destroy pager roots, clear event binders, and clear the dictionary.

### EquipmentSlots UI Clone and Binder State

- `DolocTownExperimentalBridgeApi` stores `activeEquipmentSlotUiObjects` and `equipmentSlotUiEventBinders`.
- Cloned slot GameObjects are added during render in `Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs`.
- UnityEvent binders/delegates are stored for cloned slots and capture slot/entry state.
- `ResetEquipmentSlotSessionState()` clears storage/session objects but does not unconditionally clear active UI clones or binders.
- `ClearEquipmentSlotsUi(parentTransform)` performs UI cleanup, but only when a current parent is available through the render path.

Confirmed risk:

- Save/title/environment boundaries can reset data while leaving cloned UI objects and binders strongly referenced until a later successful render cleanup.
- This is a DTMAPI-owned root path and should be fixed before blaming the game or another mod.

Recommended fix:

- Add lifecycle cleanup that destroys `activeEquipmentSlotUiObjects`, clears `equipmentSlotUiEventBinders`, resets UI-render flags, and logs clone/binder counts. Call it independently from storage session cleanup at `SaveLoaded`, `ReturnedToTitle`, and `EnvironmentReset`.

### AutoFishing Runtime State

- `FishingAutomationService` keeps dictionaries/hashsets for minigame handles, ready-charge states, animator speeds, and hook physics.
- `UseFishRod` success currently logs `auto-cast invoked` without confirming `AgentStateFishingReady/Cast/Wait/Battle/Pull` progression.
- `SetEnabled(false)` disables state but does not perform the same full transient reset as lifecycle boundaries.

Confirmed risk:

- AutoFishing can produce misleading logs: native `UseFishRod` invocation success is not a native state-machine progression proof.
- Native/Unity object caches are bounded by lifecycle hooks only; if a phase hook is missed, stale handles can survive until the next reset.

Recommended fix:

- Add a pending auto-cast watchdog. Record pending cast after `UseFishRod`, clear it on fishing phase/minigame/pull progression, and report a structured stall after a short timeout with current agent state, hook-installed status, and cache counts.
- Do not call `UseFishRod` while fishing hooks are not installed.
- Make `SetEnabled(false)` reset transient state and restore animator/hook physics.

### ActionSpeed and AnimalViewer

- `ActionSpeedService` retains animator original speeds and temporary animal-interaction state, but restore hooks and lifecycle resets exist.
- `AnimalViewerService` retains native animal data keys and cloned row objects, but current clone reset paths are stronger than earlier builds.

Current assessment:

- These remain worth tracking in counters, but they are not the highest-risk root from current code inspection.

## Logging Export Gap

Before this audit, both export paths missed Unity native crash dumps:

- `tools/scripts/collect-logs.ps1` collected DTMAPI logs, BepInEx logs, Player.log, Steam logs, process/fatal checks, install state, and DTMAPI evidence summaries.
- `DiagnosticsService.ExportLogs()` collected DTMAPI latest/history, BepInEx, Player.log, install/release state, and diagnostics summary.
- Neither path collected `%TEMP%\RedSawGames\DolocTown\Crashes`.

This is why the received logs can classify the crash as Unity/Mono GC-level but cannot identify the native object graph or root set.

## Implemented in This Follow-Up

- `collect-logs.ps1` now includes `Unity-Crashes/summary.txt` and copies recent Unity crash report directories for the current Windows user.
- Game-internal `DiagnosticsService.ExportLogs()` now includes the same recent Unity crash reports, `Unity-Player-prev.log` when present, and recent install/uninstall failure states.
- File copy is best-effort and bounded:
  - recent crash directories only;
  - file count limit per crash directory;
  - large files are skipped with a summary entry rather than failing collection.

## Next Fix Goal

Create a small runtime lifecycle cleanup goal before adding broad instrumentation:

1. Clear EquipmentSlots cloned UI/binder roots on save/title/environment boundaries.
2. Clear SaveSlots panel pager states on save/title/environment boundaries.
3. Add low-frequency lifecycle counters for `EnvironmentReset`, feature fanout duration, EquipmentSlots clone/binder count, SaveSlots panel-state count, AutoFishing cache counts, ActionSpeed animator cache count, and machine scan counts.
4. Add AutoFishing pending-cast watchdog.
5. Re-test long-play-sensitive flows: opening/closing equipment UI, title return/re-enter, FarmCase/RecipePanel/CraftQuantity flows, fishing loop, and repeated room transitions.

Do not mark the GC crash solved until a long-run player package or local soak run includes Unity crash directory evidence and shows whether the object-retention counters grow.
