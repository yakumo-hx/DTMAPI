# 2026-06-28 Crash Log And Full Code Audit

## Source Request

User requested a rigorous outside-view audit of recurring Doloc Town DTMAPI crashes, starting from the available feedback logs and then reviewing DTMAPI core code plus local mods in this worktree. The request explicitly allowed parallel sliced review.

## Scope

- Evidence reviewed:
  - `docs/debug/evidence/player-logs/20260623-download-dtmapi-logs/`
  - `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
  - `docs/debug/issues/ISSUE-011-20260623-short-run-native-crash.md`
  - relevant update records from 2026-06-20, 2026-06-23, and 2026-06-28
- Code reviewed:
  - `src/DTMAPI.BepInExBootstrap`
  - `src/DTMAPI.Core`
  - `src/DTMAPI.GameBridge.DolocTown`
  - `src/DTMAPI.ModConfigMenu`
  - local mods under `testmods/`
- Validation:
  - Read-only audit. No build, install, launch, or game smoke was run for this report.
  - No shared runtime lock was required because the local game/runtime was not touched.

## Related Records

- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/issues/ISSUE-011-20260623-short-run-native-crash.md`
- `docs/reviews/code/2026/20260620-0001-long-run-gc-crash-lifecycle-audit.md`
- `docs/updates/2026/20260620-0001-long-run-gc-crash-review.md`
- `docs/updates/2026/20260620-0002-long-run-gc-crash-lifecycle-cleanup.md`
- `docs/updates/2026/20260620-0003-long-run-gc-crash-followup-instrumentation.md`
- `docs/updates/2026/20260623-0001-player-crash-analysis.md`
- `docs/updates/2026/20260628-0001-player-crash-diagnostics-export.md`
- `docs/updates/2026/20260628-0002-diagnostics-export-crash-dumps.md`

## Executive Conclusion

The available evidence points to at least two crash families, not one single proven root cause.

1. Long-run crashes: the 2026-06-18 and 2026-06-19 samples are Unity/Mono native crashes with `mono_gc_register_root` stacks and no managed fatal exception. The strongest DTMAPI-side signal is sustained high-frequency feature fanout: `EnvironmentReset`, `Feature.Update`, fishing automation state, and input dispatch all grow into the hundreds or thousands before crash. These logs do not prove a single leaking owner, but they do prove the crash is happening after a large volume of managed-to-native lifecycle and hook activity.

2. Short-run crash: the 2026-06-21 sample crashes after roughly 6.5 minutes. Its strongest distinguishing signals are the debug console item give path, active MoreEquipmentSlots state, NoWeeds Harmony failure, repeated movement input dispatch, InputSystem/EventSystem noise, and a third-party BepInEx plugin named `《多洛可小镇》增强功能 Mod 内置版 by Qiuzy 1.4.0` enabling multiple patches shortly before the crash. The same NoWeeds and InputSystem noise also appears in a normal-exit 2026-06-22 package, so those are not sufficient causes by themselves.

3. The main code-level risk pattern is not a single obvious null reference or C# exception. It is long-lived managed roots and repeated Unity object, delegate, Harmony, native state, and log-status churn around hooks, UI, input, and local mod features. This is exactly the kind of pattern that can line up with a native Mono/Unity GC crash while leaving no managed fatal exception.

4. A newly identified P1 code risk is the hook retry path. `DolocTownGameBridge` uses `System.Threading.Timer` to retry hook installation, which can call Harmony/reflection/status/event paths from a ThreadPool thread until all hook targets are ready. That is a better match for native Unity/Mono instability than log volume alone, especially because `SetHookStatus` can dispatch events without a runtime-thread guard.

5. Recent diagnostics updates improve observability, but build success and a short clean run are not enough to retire these issues. The next useful work is an isolation matrix with native crash dumps, per-owner counters, thread identity evidence, and controlled mod sets.

## Log Reconstruction

### 2026-06-16 mixed latest log

The original `DTMAPI-latest.log` spans multiple dates, from 2026-06-12 to 2026-06-16. It is not a single crash-session tail. It contains several useful historic signals:

- `SaveSlots.OfficialUi.SelectPaging` repeated `IndexOutOfRangeException`.
- `AnimalViewer` update failures rising to a very high count.
- `AutoFishing` API/type mismatch failures.
- Heavy `EnvironmentReset` and AutoFishing logging.

This file should not be treated as direct crash-scene evidence for one session.

### 2026-06-16 21:21 normal exit

The `20260616-212130` package looks like a short normal-exit run. It shows a debug-console Y key open/close pattern:

- `Debug console opened`
- immediately followed by `Debug console closed reason=Y`

That pattern is evidence for an input/UI edge bug, but not a native crash by itself.

### 2026-06-18 long-run crashes

Two long-run native crash packages show the same broad shape:

- 2026-06-18 11:27:59 to 17:10:16
- 2026-06-18 17:23:59 to 20:46:43
- Unity `Crash!!!`
- stack includes `mono_gc_register_root`
- no DTMAPI managed fatal exception immediately explaining the native crash

Observed high-volume counters before crash include:

- `EnvironmentReset`: roughly thousands in the longer run
- `Feature.Update`: roughly thousands
- fishing and crop automation reset paths: hundreds
- input dispatch: thousands to over ten thousand

This makes the long-run crash family strongly lifecycle/fanout related.

### 2026-06-19 long-run crash

The `20260619-131746` package is a roughly 42-minute native crash using DTMAPI `0.5.2-alpha`. It still shows the Unity/Mono native crash pattern. The counts are lower than the 2026-06-18 samples but still high:

- `EnvironmentReset`: hundreds
- `Feature.Update`: hundreds
- input dispatch: thousands
- AutoFishing activity present

This matters because it shows the crash survived some earlier fixes. It should not be considered solved by the 2026-06-20 cleanup work without a fresh long soak.

### 2026-06-21 short-run crash

The `20260621-224311` package is the main short-run native crash sample. Key facts:

- Crash after roughly 6.5 minutes.
- NoWeeds Harmony patch fails with `Owner can't be an array or an interface`.
- MoreEquipmentSlots loads stored slots including `fried_egg_hat`, `drone_66_mask`, and `skull_hat`.
- Debug console is used to give multiple items.
- Repeated movement input lines appear milliseconds apart.
- InputSystem/EventSystem initialization noise is present.
- Qiuzy/DolocPlus third-party BepInEx plugin loads and later enables many patches shortly before crash.

The Qiuzy plugin is especially important for this short-run sample because it is not a DTMAPI ordinary mod. It lives outside the DTMAPI mod loader cleanup model and applies Harmony patches directly from BepInEx.

### 2026-06-22 normal-exit comparison

The `20260622-101413` package exits normally while also showing NoWeeds failure and InputSystem noise. Therefore:

- NoWeeds failure alone is not enough to explain the crash.
- InputSystem initialization noise alone is not enough to explain the crash.
- Both remain risk amplifiers when combined with UI churn, input flood, MoreEquipmentSlots, and third-party BepInEx patches.

## Findings

### P1. Hook retry can run Harmony/status/event fanout off the Unity main thread

Files:

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs`
- `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`
- `src/DTMAPI.Core/Services/EventManager.cs`

`Initialize` creates a `System.Threading.Timer` that retries `InstallHarmonyHooks` every two seconds. The timer is only disposed when all hook targets are ready. Because the ready set includes many optional or experimental hooks, one missing or signature-shifted target can keep retry active for the process lifetime.

Risk:

- Hook install uses Harmony and reflection from a ThreadPool callback.
- Hook status updates can flow into runtime diagnostics and event dispatch.
- Event dispatch does not enforce Unity main-thread execution.
- Add/remove/snapshot of event handler lists is not protected against concurrent access.

This is a high-priority explanation candidate because it can plausibly produce native Unity/Mono failure without a managed fatal exception. The `hookGate` lock and status deduplication reduce the chance of simple log spam, but they do not prove this path is safe.

Recommended fix direction:

- Record thread ID and main-thread status for every hook retry and `SetHookStatus` call.
- Marshal hook retry work onto the bootstrap `Update` thread before calling Harmony, reflection-heavy bridge code, or diagnostics events.
- Split required hook readiness from optional/experimental hook readiness so optional targets cannot keep the retry timer alive forever.
- Make diagnostics/event dispatch main-thread owned or explicitly thread-safe.

### P1. Long-run evidence is consistent with managed roots and native object churn

Files:

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Features.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`

The bridge dispatches frequent operations such as `Update` and `EnvironmentReset` across all registered features. Hook-status logging is throttled for high-frequency operations, but the actual feature calls still happen every time. Long-run logs show these counts growing to hundreds or thousands before native crash.

The current architecture has several long-lived roots:

- static callback references from `DolocTownHookCallbacks.Runtime` and `.Bridge`
- hook retry timer and assembly-load subscription
- feature-level dictionaries keyed by native objects
- UI binders and Unity event delegates
- input registrations without owner cleanup

This is not proof that any one collection leaks unbounded today. It is proof that the long-run crash family needs per-owner counter evidence instead of more tail-log interpretation.

Recommended next checks:

- Add lifecycle counters for feature fanout by owner and phase.
- Emit periodic counts for native-object-key dictionaries and UI binders.
- Verify a clean shutdown path clears static callback roots and timers.
- Run 30 to 60 minute soak tests with controlled mod sets.

### P1. Input dispatch is globally registered, ownerless, and noisy

Files:

- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `testmods/AutoFishingMod/ModEntry.cs`
- `testmods/ActionSpeedMod/ModEntry.cs`

`InputService` stores registered buttons in global sets. It does not track which mod owns which registration. Failed mod cleanup explicitly reports `inputCleanupSupported=False`, so failed or disabled code mods can leave keys globally polled.

`RecordInputPressed` logs and dispatches every key-down notification after suppression checks. The 2026-06-21 log shows movement keys repeated within milliseconds. AutoFishing registers movement keys as manual cancel keys even when automatic fishing is not actively controlling the player. ActionSpeed can register `"None"` as a key-like value.

Risk:

- ordinary movement can become DTMAPI event traffic
- noisy input logs can mask more important signals
- failed or disabled mods can leave stale registered keys
- duplicate input edge handling can cause UI open/close behavior, as seen in the 2026-06-16 console sample

Recommended fix direction:

- Track input registration by owner and release owner registrations on unload/failure.
- Do not permanently register high-frequency movement keys for inactive features.
- Add edge guards or per-frame duplicate suppression in `RecordInputPressed`.
- Rate-limit or aggregate ordinary movement input logging.

### P1. Debug console item UI is high churn and matches the short-run crash scenario

File:

- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`

The debug console rebuild path destroys and recreates the panel, cells, buttons, event triggers, tooltip objects, and binders. Each item give marks the console dirty, causing a rebuild on the next frame. Hover tooltips also publish hook-status details with item/search context.

The short-run crash includes debug-console item give activity. This does not prove DebugConsole caused the native crash, but it is a credible accelerator when combined with MoreEquipmentSlots UI clones, input duplication, and third-party patches.

Recommended fix direction:

- Reuse item cells where practical instead of destroying/recreating the full grid after every give.
- Batch item-give UI refreshes.
- Add counters for rebuild count, live item cells, event binders, tooltip creates/destroys, and give count.
- Keep debug console disabled from normal player release packages unless explicitly requested.

### P1. MoreEquipmentSlots is much improved but still one of the highest-risk local mods

Files:

- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `testmods/MoreEquipmentSlotsMod/ModEntry.cs`

Current code has meaningful cleanup:

- old cloned UI objects are destroyed
- event binders are tracked and cleared
- native equipment functions can be removed
- save/load/title lifecycle clears session state

However, the feature still bridges deeply into native UI and equipment state:

- cloned native accessory slot UI
- generated native item objects used as function keys
- UnityEvent listener delegates
- hover/click binders
- storage restore and dirty save tracking

The 2026-06-21 crash had MoreEquipmentSlots active with stored slots. This makes it a primary isolation candidate, even though current cleanup is stronger than the old code.

Recommended next checks:

- Add periodic counts for active clone objects, event binders, equipment entries, storage owners, and native functions.
- Run short-run reproduction with MoreEquipmentSlots on/off while holding other factors fixed.
- Run title-return and save-load cycles to verify counts return to zero.

### P1. FishingAutomation remains central to the long-run hypothesis

Files:

- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationFeature.cs`
- `testmods/AutoFishingMod/ModEntry.cs`

FishingAutomation has several native-object-keyed dictionaries and sets, including minigame state, ready charge state, original animator speeds, hook gravity scales, and diagnostics. Current code includes important mitigations:

- watchdog and pending auto-cast state
- transient cleanup
- save-load/title reset
- diagnostic throttling

The long-run logs still make fishing automation a top suspect because AutoFishing and feature reset/update counters are repeatedly high before native crash. The code looks more defensible than earlier versions, but it needs current-build soak evidence.

Recommended next checks:

- Run long soak with AutoFishing disabled, then enabled, with all else equal.
- Emit counts for every FishingAutomation dictionary/hashset.
- Confirm all native object references clear on save load, title return, and feature disable.

### P1. The short-run sample has a non-DTMAPI BepInEx plugin isolation variable

File:

- `docs/debug/evidence/player-logs/20260623-download-dtmapi-logs/DTMAPI-logs/20260621-224311/BepInEx-LogOutput.log`

The short-run package loads `《多洛可小镇》增强功能 Mod 内置版 by Qiuzy 1.4.0`, which initializes and later enables many Harmony-patched features, including movement, time speed, automatic harvesting/collecting, chest access, and craft-count changes.

This plugin is outside the DTMAPI ordinary mod loader and is not cleaned up by DTMAPI. It is a major isolation variable for the 2026-06-21 short-run crash. It should not be used to explain the 2026-06-19 long-run crash if that package did not load it.

Recommended next checks:

- Reproduce short-run scenario with Qiuzy absent.
- Reproduce with Qiuzy present but features disabled.
- Reproduce with Qiuzy enabled and DTMAPI local mods disabled.
- Record a BepInEx plugin inventory in each crash report.

### P2. EventManager disables failing handlers but keeps their captured roots

File:

- `src/DTMAPI.Core/Services/EventManager.cs`

High-frequency event handlers are disabled after repeated failures, but the handler record stays in the list. That means the delegate and captured owner state can remain rooted for the process lifetime unless owner cleanup removes it. This also compounds the hook-retry risk because handler add/remove and dispatch snapshotting are not visibly guarded for concurrent off-main-thread status dispatch.

Risk:

- failed mods or failed high-frequency callbacks can retain Unity/native references
- disabled handlers still contribute to lifecycle memory pressure

Recommended fix direction:

- Remove disabled high-frequency handlers after threshold, or store enough owner metadata to release them during failure cleanup.
- Add diagnostics for active, disabled, and removed handler counts by event type and owner.

### P2. Failed code mod cleanup leaves input and third-party Harmony state behind

File:

- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`

`CleanupFailedCodeModOwner` cleans several DTMAPI-owned registries, but it explicitly reports that input cleanup and third-party Harmony cleanup are unsupported. The NoWeeds failure in the 2026-06-21 package is exactly the sort of partial-load case where this matters.

Recommended fix direction:

- Add owner-based input cleanup.
- For DTMAPI-loaded mods, require Harmony patch ownership and cleanup where possible.
- Emit failed-load cleanup reports with counts, not just supported/unsupported text.

### P2. Config preview applies real setters during render

Files:

- `src/DTMAPI.ModConfigMenu/ConfigMenuPage.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`

`PreviewPendingValues` applies pending values through real setters while the settings UI is rendering, then rolls back. This is risky if setters register input, patch Harmony, create Unity objects, alter native bridge state, or produce logs.

Recommended fix direction:

- Treat preview as pure display data when possible.
- If real setter preview remains necessary, mark preview mode and block side-effectful setters from mutating runtime services.

### P2. File logging and diagnostics export can add synchronous pressure

Files:

- `src/DTMAPI.Core/Logging/FileMonitor.cs`
- `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`

`FileMonitor` uses instance-level locks while multiple monitors may write to the same `latest.log`. Diagnostics export reads logs and crash artifacts synchronously. This is unlikely to be the root of a Unity native GC crash, but it can add stutter and make tail evidence harder to interpret.

Recommended fix direction:

- Use a per-path shared file lock or central log writer.
- Avoid expensive synchronous diagnostics collection during gameplay.
- Ensure crash dump collection is post-crash/export-time only.

### P2. Bootstrap error accounting and shutdown cleanup are too weak for native-crash diagnosis

Files:

- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`

Bootstrap records each component update error only once. This protects logs from spam, but it can hide recurring failure cadence. Shutdown logs `OnApplicationQuit`, but the reviewed code does not show a full bridge dispose path clearing all static callback roots, hook retry timer state, and subscriptions in every shutdown mode.

Recommended fix direction:

- Track recurring component error counts and last timestamps.
- Add explicit bridge/runtime dispose coverage and log counts of cleared roots.
- Include these counts in diagnostics export.

### P2. SaveSlots historic crash signal appears mitigated but not proven solved

Files:

- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsFeature.cs`
- `testmods/MoreSavesMod/ModEntry.cs`

Older logs show `SaveSlots.OfficialUi.SelectPaging` `IndexOutOfRangeException`. Current code clears official UI states on save load and title return, destroys pager objects, clears binders, and uses a fixed 12-slot MoreSaves configuration.

This looks like a real mitigation, but the prior error is serious enough that SaveSlots should stay in the soak counter set:

- official UI state count
- pager object count
- binder count
- page/index values during navigation

### P2. Local mods contain several smaller but real correctness risks

Files:

- `testmods/MineMod/ModEntry.cs`
- `testmods/ZoomMod/ModEntry.cs`
- `testmods/ManboCardboardAudioMod/ModEntry.cs`
- `testmods/ActionSpeedMod/ModEntry.cs`
- `testmods/AutoHarvestMod/ModEntry.cs`
- `testmods/CropHarvestingQaMod/ModEntry.cs`
- `testmods/StrongPlantingGunMod/ModEntry.cs`

Notable risks:

- Mine exposes `Enabled` config, but the machine appears to be registered regardless of that value. `MachineDefinition` does not carry an enabled flag.
- Zoom acquires/updates a camera view lease but reset only sets zoom scale back to 1; an explicit release path was not evident in the mod.
- ManboCardboardAudio registers with `Enabled = true` and `VerboseLogging = true` without a normal enabled config.
- ActionSpeed can register `"None"` as a button-like value.
- AutoHarvest depends on bridge checks and should be validated outside an active save.
- CropHarvestingQa is a QA fixture and should not be present in ordinary player packages unless intentionally testing.
- StrongPlantingGun is explicitly experimental and comments acknowledge visible slot state can persist until re-equip/restart.

These are not all crash root causes, but they are part of the current risk surface.

## Rejected Or Unproven Hypotheses

- NoWeeds alone caused the 2026-06-21 crash: unproven. The 2026-06-22 normal-exit package also shows NoWeeds failure.
- InputSystem noise alone caused the short crash: unproven. It also appears in the normal-exit comparison.
- Qiuzy explains the long-run crash family: unproven and likely false for samples that did not load Qiuzy.
- DebugConsole explains all crashes: unproven. It matches short-run activity better than long-run evidence.
- SaveSlots historic `IndexOutOfRangeException` explains current crashes: unproven. Current code appears to include lifecycle cleanup, but needs soak evidence.
- Build success proves this fixed: false. The crash family is runtime and native-lifecycle oriented.

## Recommended Isolation Matrix

All runs should use the shared runtime lock before installing, launching, writing local official mod folders, or smoke testing. Unless a task says otherwise, use the third local save slot.

Minimum evidence to collect for each run:

- DTMAPI startup log.
- BepInEx plugin inventory.
- DTMAPI loaded-mod inventory.
- Third-save load evidence.
- Periodic lifecycle counter snapshots.
- Unity crash dump directory collection.
- Process exit check: no leftover `DolocTown.exe`.
- Diagnostics export after crash or after clean exit.

Suggested runs:

1. Current DTMAPI core only, no local DTMAPI mods, no Qiuzy, no NoWeeds, 30 to 60 minutes.
2. Current DTMAPI with local mods enabled except AutoFishing, 30 to 60 minutes.
3. AutoFishing only, same save, 30 to 60 minutes.
4. MoreEquipmentSlots only, save-load/title-return cycles, 20 minutes.
5. DebugConsole only, scripted item give and hover stress, 10 to 20 minutes.
6. DebugConsole plus MoreEquipmentSlots, same scripted short-run path as 2026-06-21.
7. Qiuzy present with features disabled, DTMAPI local mods disabled.
8. Qiuzy present with the same features enabled as 2026-06-21, DTMAPI local mods disabled.
9. NoWeeds failure reproduction alone, then NoWeeds plus DebugConsole/MoreEquipmentSlots.

Counters to add before or during these runs:

- hook retry count, thread ID, main-thread flag, and unresolved required/optional hook list.
- `SetHookStatus` call count by thread ID and hook ID.
- `EnvironmentReset` and `Feature.Update` by feature.
- input registered keys by owner, active down keys, dispatch count by key.
- EventManager active/disabled/removed handlers by event type and owner.
- DebugConsole rebuild count, live cells, binders, tooltip objects, give count.
- EquipmentSlots clone objects, event binders, slot entries, storage owners, native functions.
- SaveSlots official UI states, pager objects, binders, current page/index.
- FishingAutomation dictionary/hashset counts and pending auto-cast state.
- ActionSpeed original animator speed count.
- managed memory and GC collection count if feasible.

## Recommended Implementation Order

1. Add observability first: hook retry thread identity, `SetHookStatus` thread identity, owner counters, native-object dictionary counts, and per-feature lifecycle snapshots.
2. Move hook retry/Harmony/status fanout onto the Unity main thread, and stop the retry loop from being held alive by optional hook targets.
3. Add owner-based input registration cleanup and reduce movement-key dispatch/log spam.
4. Harden DebugConsole rebuild and hover/give churn.
5. Add explicit bridge/runtime shutdown cleanup logging.
6. Run the isolation matrix and update `ISSUE-010` and `ISSUE-011` with dated evidence.
7. Fix local mod correctness issues that can be handled independently, especially Mine `Enabled`, ActionSpeed `"None"` registration, Zoom lease release, and Manbo verbose/default enabled behavior.

## Bottom Line

The logs do not support a single confident root-cause claim yet. They do support a narrower and more testable diagnosis:

- long-run crashes are most likely tied to cumulative DTMAPI lifecycle/hook/native-object pressure;
- short-run crashes are most likely a compound case involving DebugConsole, MoreEquipmentSlots, input duplication, partial third-party patch failures, and possibly Qiuzy's separate BepInEx patches;
- the next useful step is not more general log volume, but per-owner counters plus controlled isolation runs.
