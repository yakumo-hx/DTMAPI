# 20260705-0002 SaveLoad Per-Owner Delta And Idle Cadence

Date: 2026-07-05 +08:00

Status: source verified / no-idle and 10-minute-cadence smokes passed / ISSUE-010 open

## Source Request

Before the next long run, add per-owner event/input/config-page/UI/GameBridge deltas so a future UI split can identify the exact owner instead of only the feature group. Then run the minimal reproduced profile with direct 20 SaveLoad cycles, and again after 10 minutes of title idle with one save entry every 5 minutes until crash or the cap.

## Changed Files

- `src/DTMAPI.Core/Runtime/ModOwnerLedgerService.cs`
- `src/DTMAPI.Core/Runtime/TitleReturnBoundaryLedgerService.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/EventManager.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Features.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/reviews/code/2026/20260705-0001-phase86-saveload-cycle-accumulation-audit.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260705-0002-saveload-per-owner-delta-cadence.md`
- `docs/updates/INDEX.md`

## Implementation

- Added `ModOwner.byOwner` metrics for records, `EventHandler`, `InputButton`, `ConfigPage`, and `LoadedCodeMod`.
- Added current owner-root summaries for input buttons, event handlers, and config pages.
- Added UI owner summaries for Bootstrap Title Settings, Debug Console, SaveSlots, and EquipmentSlots with `Canvas`, `EventSystem`, `Button`, `InputField`, `ScrollRect`, `UnityEventListeners`, `DynamicBinders`, and `rootAlive`.
- Added GameBridge feature status/runtime-state counters under `GameBridge.featureById`.
- Added `ownerPrevDelta`, `ownerSameBoundaryDelta`, and `ownerNonZero` to object-delta formatting so owner/root fields are visible at `BeforeNextLoadGame` and `LoadGameNativeEnter` even when the generic top-N summary is dominated by dispatch counters.

## Validation

- PowerShell parser check for `tools/scripts/run-game-smoke.ps1` passed.
- `git diff --check` passed with line-ending normalization warnings only.
- `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK`; restricted-network `NU1900` warnings only.
- Runtime lock was acquired and released for both game smokes. Final checks found no `DolocTown.exe` process and no fatal instance window.

## Evidence

- Per-owner no-idle minimal-profile 20-cycle pass: `docs/debug/evidence/GAME-SMOKE/20260705-075428`.
  - `SaveLoadCycle=Passed`, `requests=20`, `nativeEnter=20`, `nativeReturn=20`, `saveLoaded=20`, `duplicateRequests=0`, `fatalWindows=0`.
  - `SaveLoadCycleObjectDeltaSummary` includes `ownerPrevDelta`, `ownerSameBoundaryDelta`, and `ownerNonZero`.
- Ten-minute initial title idle plus five-minute cadence pass: `docs/debug/evidence/GAME-SMOKE/20260705-080223`.
  - `SaveLoadCycle=Passed`, `cycles=20`, `initialIdleSeconds=600`, `intervalSeconds=300`, `inSaveSeconds=5`, `elapsedSeconds=6535`.
  - `requests=20`, `nativeEnter=20`, `nativeReturn=20`, `saveLoaded=20`, `duplicateRequests=0`, `fatalWindows=0`.
  - `NoFatalInstanceWindow=Passed`, `ProcessExited=Passed`.

## Findings

- In the minimal reproduced profile, no-idle 20 cycles still pass after per-owner diagnostics are enabled. Cycle count alone remains insufficient.
- The 10-minute idle plus five-minute cadence run also passed for roughly 109 minutes of smoke time. This is stronger evidence that uninterrupted title idle is the important variable; periodically entering and returning from a save appears to reset or avoid the fatal condition in this profile.
- Per-owner roots are now visible for the likely UI split. Current stable owner counts include `DTMAPI.DebugConsoleMod` with event/input/config roots, `DTMAPI.ZoomMod` with five input roots, `DTMAPI.MoreSavesMod` config ownership, and `DTMAPI.MoreEquipmentSlotsMod` event/config ownership. SaveSlots and EquipmentSlots UI binders/clones still return to zero at title in these runs.

## Source And Community Checks

- Unity `UnityEvent.RemoveListener` docs: <https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Events.UnityEvent.html>. Runtime listeners are removable; this supports counting DTMAPI-owned dynamic binders and listener roots.
- Unity Memory Profiler managed-shell docs: <https://docs.unity3d.com/Packages/com.unity.memoryprofiler@1.1/manual/managed-shell-objects.html>. Supports the current count-first approach for UnityEngine.Object managed wrappers.
- Unity mark-stack overflow discussion: <https://discussions.unity.com/t/fatal-error-in-gc-unexpected-mark-stack-overflow/760479>. Supports treating the crash as object-graph/root-pressure evidence when no managed stack is available.
- Unity Issue Tracker mark-stack overflow with many GameObjects: <https://issuetracker.unity3d.com/issues/the-player-crashes-on-unexpected-mark-stack-overflow-without-a-stacktrace-when-creating-a-large-amount-of-game-objects-and-the-il2cpp-scripting-backend-is-selected>. Supports keeping UI/GameObject families visible in the ledger.
- Harmony patch edge-case docs: <https://harmony.pardeike.net/articles/patching-edgecases.html>. Supports keeping Harmony hooks process-long and avoiding broad title/save unpatching.

## Rollback

The code changes are diagnostic-only. Reverting this update removes per-owner delta fields and owner UI summaries, but does not alter official JSON, player PNG/WAV, CustomAnimals, AnimalVoice, AutoFishing, or public API behavior.

## Follow-Up

- Run the UI split under the previously reproducing continuous one-hour title-idle route, not the 10-minute/five-minute cadence route.
- Add Zoom, MoreSaves, MoreEquipmentSlots, and YConsole/DebugConsole one at a time on top of the passing CustomAnimals+action+Manbo profile.
- If no single UI owner reproduces, test UI pairs and then escalate to external Unity object/root-set capture.
