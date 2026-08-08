# 20260704-0004 Project Lifecycle Sweep

## Summary

Ran a broad DTMAPI object-lifecycle sweep for Unity/Mono object graph retention risk. The pass classifies process-long owners, title/save transients, save-bound state, shutdown-only native/Unity roots, and remaining suspects. It also applies small DTMAPI-owned cleanup fixes without changing public API behavior, registry takeover state, content-pack JSON semantics, CustomAnimals, AnimalVoice, AutoFishing gameplay, or native object ownership.

Short smoke passed across title lifecycle, title-return cycles, Hatch AnimalVoice, AutoFishing, AnimalViewer, and SaveSlots. A later requested one-hour-title-idle plus repeated SaveLoadCycle long smoke reproduced the known Fatal GC class after several successful cycles. ISSUE-010 remains open; this pass rules out new short-window regressions and keeps the remaining suspects explicit.

## Source Request / Goal

- Goal: continue refactor, but sweep DTMAPI broadly for Unity/Mono object graph retention, duplicate owners, cross-save/title contamination, GC mark-stack pressure, and long-run instability risk.
- Priority: small, definite fixes to DTMAPI-owned references, event subscriptions, per-save/per-title caches, and boundary diagnostics.
- Explicitly avoid dangerous takeover work or speculative destruction of native/unknown Unity objects.

## Changed Files

- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs`
- `docs/reviews/code/2026/20260704-0002-project-object-lifecycle-audit.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260704-0004-project-lifecycle-sweep.md`

## Behavior

- Added SaveLoaded and ReturnedToTitle cleanup calls for Bootstrap title settings UI and fallback input edge state.
- Added Bootstrap shutdown cleanup for Title Settings UI, Debug Console UI, GameBridge, and fallback input edge state before runtime shutdown notification.
- Title Settings UI now performs one boundary reset path for SaveLoaded, ReturnedToTitle, title-hidden, and Shutdown:
  - closes the DTMAPI runtime UI menu;
  - hides panel state;
  - destroys DTMAPI-owned fallback EventSystem;
  - destroys rendered/dynamic UI objects and clears dynamic binders;
  - clears input value/captured-key/page state;
  - cancels pending config changes through the existing config-menu transaction API.
- Title Settings UI now destroys its DTMAPI-owned icon Sprite and Texture2D when resetting UI references.
- Debug Console UI now has explicit Shutdown cleanup for root/panel/tooltip objects, fallback EventSystem, event binders, input fields, modal flag, and consumed-input state.
- GameBridge now has explicit Shutdown cleanup that stops hook retry sources, releases `AppDomain.AssemblyLoad`, removes the DTMAPI-owned SaveLoaded UnityEvent listener, clears static hook callback roots, and publishes `GameBridge.ShutdownCleanup`.
- `ReflectedUnityInput.ClearTransientState()` clears Win32 fallback key-edge state at save/title/shutdown boundaries.

## Community Checks

External checks are recorded in `docs/reviews/code/2026/20260704-0002-project-object-lifecycle-audit.md`.

Key conclusions:

- Unity asset reachability includes static roots and GameObject hierarchies, so persistent DTMAPI roots must be counted.
- `DontDestroyOnLoad` roots and child transforms survive scene changes, so title/debug UI roots need explicit lifecycle cleanup.
- Runtime UnityEvent listeners can retain callbacks when lifetimes differ; DTMAPI-owned UnityEvent listeners should be removed on shutdown.
- AssetBundle/controller caches should not be unloaded without evidence because Unity unload modes can destroy live loaded objects.
- UnityWebRequest audio and AudioClip-backed objects should remain counted as a distinct object family.
- Harmony patches are process-level; release DTMAPI static object roots on shutdown instead of unpatching during title/save boundaries.

## Validation

- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; restricted-network `NU1900` vulnerability-feed warnings only.
- `git diff --check`: passed with line-ending normalization warnings only.
- Shared runtime lock was acquired for runtime smoke and released afterward.
- Final process check found no leftover `DolocTown.exe`.
- Short game smoke: passed.

## Evidence

- Code audit: `docs/reviews/code/2026/20260704-0002-project-object-lifecycle-audit.md`
- Slot 3 title-button lifecycle: `docs/debug/evidence/GAME-SMOKE/20260704-151818`
- Slot 3 load/title/load cycle: `docs/debug/evidence/GAME-SMOKE/20260704-151937`
- Slot 3 three-cycle title return: `docs/debug/evidence/GAME-SMOKE/20260704-152059`
- Slot 7 Hatch AnimalVoice: `docs/debug/evidence/GAME-SMOKE/20260704-152238`
- Slot 5 AutoFishing short soak: `docs/debug/evidence/GAME-SMOKE/20260704-152339`
- Slot 4 AnimalViewer UI: `docs/debug/evidence/GAME-SMOKE/20260704-152523`
- Slot 3 SaveSlots UI: `docs/debug/evidence/GAME-SMOKE/20260704-152629`
- All seven short smokes reported `RunStatus=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Load/title/load evidence reported no duplicate LoadGame and no fatal windows: `GAME-SMOKE/20260704-151937` had `requests=2`, `duplicateRequests=0`, `nativeEnter=2`, `nativeReturn=2`, `saveLoaded=2`, `fatalWindows=0`; `GAME-SMOKE/20260704-152059` had `requests=3`, `duplicateRequests=0`, `nativeEnter=3`, `nativeReturn=3`, `saveLoaded=3`, `fatalWindows=0`.
- AutoFishing soak summary in `GAME-SMOKE/20260704-152339` showed `nativeTransientHandles=0`, `miniGameHandles=0`, `readyChargeStates=0`, `animators=0`, `hookPhysics=0`, `pendingCast=False`, and `soakLoops=3/3`.
- Long follow-up slot 3 SaveLoadCycle: `docs/debug/evidence/GAME-SMOKE/20260704-154234` reproduced `Fatal error in GC / Unexpected mark stack overflow` after one-hour title idle and four closed save/title cycles. The fifth native LoadGame entered and then the fatal popup was captured. Result fields: `RunStatus=Aborted`, `SaveLoadCycle=Failed`, `NoFatalInstanceWindow=Failed`, `SaveLoaded=Passed`, `SaveLoadRequestCoordinator=Passed`, `DuplicateLoadRequests=Passed`, `SaveLoadBoundary=Passed`, `TitleReturnBoundaryLedger=Passed`, `ProcessExited=Passed`, and `ForcedClose=Passed`. SaveLoad summary: `requests=5`, active `SL-0005`, `nativeEnter=5`, `nativeReturn=4`, `saveLoaded=4`, `duplicateRequests=0`, `suppressedDuplicates=0`, `timeouts=0`, `fatalWindows=0`. Title-return summary: `currentBoundary=TR-0005`, `events=47`, `objectSnapshots=20`, `fatalEvents=0`, latest event/snapshot `LoadGameNativeEnter`.

## Rollback Notes

- Revert the six source files listed above to remove this cleanup pass.
- Do not revert prior phase 8.5 title-return ledger telemetry unless replacing it with equivalent boundary snapshots.
- Do not add speculative AssetBundle, RuntimeAnimatorController, AudioClip, EventSystem, or native UI destruction as rollback. This pass only clears DTMAPI-owned roots and transients.

## Follow-Up

- Keep remaining suspects documented: CustomAnimals bundle/controller caches, AudioReplacement platform players/definitions, process-long Harmony patches, diagnostic ledgers, and native Doloc Town title-return object graph.
- The long follow-up reproduced Fatal GC after four successful cycles. Next evidence should compare the `TR-0005` `BeforeNextLoadGame`/`LoadGameNativeEnter` snapshots against earlier successful boundaries and then choose either a targeted DTMAPI-owned mitigation or native crash/root-set inspection if DTMAPI-owned counts remain bounded.
