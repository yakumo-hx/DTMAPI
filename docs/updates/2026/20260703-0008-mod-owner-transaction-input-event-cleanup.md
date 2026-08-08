# 20260703-0008 Mod Owner Transaction Input Event Cleanup

## Summary

Implemented DTMAPI phase 6 as an internal-only platform isolation layer for mod-owned registrations. The change adds owner attribution, Entry-time transactions, rollback diagnostics, owner-bound input registration, high-frequency event quarantine, and config-preview side-effect auditing.

This phase does not change CustomAnimals, AnimalVoice, AutoFishing gameplay, Hook targets, manifest/content-pack JSON fields, public API shapes, or `RegistryTakesOver=false`.

## Source Request / Goal

- User request: enter "DTMAPI 第 6 阶段：Mod Owner / Transaction / Input-Event Cleanup".
- Goal record: `docs/goals/2026/20260703-0008-mod-owner-transaction-input-event-cleanup.md`.
- Baseline: phase 4.5 AutoFishing lifecycle diagnostics and phase 5 SaveLoad coordinator were committed first in `bc6fdbf Add AutoFishing and SaveLoad lifecycle diagnostics`.

## Changed Files

- `src/DTMAPI.Core/Runtime/ModOwnerLedgerService.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/RefactorScaffoldOptions.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.Core/Services/EventManager.cs`
- `src/DTMAPI.Core/Services/RegistryAndHelpers.cs`
- `src/DTMAPI.Core/Services/CustomEntityRegistryService.cs`
- `src/DTMAPI.Abstractions/ConfigMenu.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuPage.cs`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/goals/2026/20260703-0008-mod-owner-transaction-input-event-cleanup.md`
- `docs/goals/2026/20260703-0008-mod-owner-transaction-input-event-cleanup.goal.txt`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/hook-map/README.md`

## Behavior

- Added internal scaffold flags, default on and locally configurable:
  - `ModOwnerLedger`
  - `ModLoadTransaction`
  - `OwnerBoundInput`
  - `EventHandlerQuarantine`
  - `ConfigPreviewAudit`
- Added an internal mod owner ledger with owner id, transaction id, registration kind, key, phase, cleanup policy, rollback result, and `NeedsRestart` reporting for raw Harmony/static/native side effects DTMAPI cannot prove safe to rollback.
- Wrapped code-mod `Entry(helper)` with an internal mod-load transaction. Successful Entry commits; Entry exceptions rollback DTMAPI-owned API/event/input/config/custom-entity registrations and report cleanup counts.
- Made `DtmHelper.Input` owner-bound without changing `IInputHelper`. The globally listened button set remains the union of all owner registrations. Owner cleanup removes only that owner's buttons; returned-to-title clears only transient input state.
- High-frequency event handlers that hit the existing consecutive-failure threshold move into quarantine when `EventHandlerQuarantine=true`, so they no longer remain in the active dispatch hot path. Low-frequency lifecycle/save handlers keep the existing diagnostic behavior.
- Config menu preview still calls the real setter/restore path as before, but now audits apply/restore owner, item, kind, success, and failure details.
- Added smoke `result.json` fields:
  - `ModOwnerLifecycle`
  - `ModOwnerLifecycleSummary`
  - `ModLoadTransaction`
  - `OwnerBoundInput`
  - `EventHandlerCleanup`
  - `ConfigPreviewAudit`
  - `FailedModRollback`

## Validation

- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`.
- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- `git diff --check`: passed; Git reported line-ending normalization warnings only.
- Slot 3 short lifecycle smoke passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseTitleButtonLifecycle -TimeoutSeconds 320 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260703-220819`
  - Key fields: `RunStatus=Passed`, `SaveLoaded=Passed`, `TitleButtonLifecycle=Passed`, `ModOwnerLifecycle=Passed`, `ModLoadTransaction=Passed`, `OwnerBoundInput=Passed`, `EventHandlerCleanup=Passed`, `ConfigPreviewAudit=Passed`, `FailedModRollback=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
  - Owner summary included `activeTransactions=0`, `cleanupFailures=0`, `needsRestart=0`, `OwnerBoundInput` with `buttons=17`, and `EventHandlerCleanup` with `quarantinedHandlers=0`.
- Slot 7 Hatch AnimalVoice smoke passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -AutoExerciseHatchAnimalVoice -TimeoutSeconds 260 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260703-220920`
  - Key fields: `RunStatus=Passed`, `SaveLoaded=Passed`, `HatchAnimalVoice=Passed`, all phase-six fields passed, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
- Slot 5 AutoFishing short soak passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 5 -AutoExerciseAutoFishingPhase -AutoFishingSoakLoops 3 -TimeoutSeconds 420 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260703-221013`
  - Key fields: `RunStatus=Passed`, `AutoFishingPhase=Passed`, `AutoFishingSoak=Passed`, all phase-six fields passed, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
- Final process check: no residual `DolocTown.exe` was running after the smoke batch.

## Rollback Notes

- Set any of these flags to `false` in `DTMAPI/config/refactor-scaffold.json` or through the matching environment override to disable that layer:
  - `ModOwnerLedger`
  - `ModLoadTransaction`
  - `OwnerBoundInput`
  - `EventHandlerQuarantine`
  - `ConfigPreviewAudit`
- Disabling `OwnerBoundInput` restores the legacy global input helper behavior.
- Disabling `EventHandlerQuarantine` keeps the old disabled-in-slot behavior.
- This stage does not remove already-loaded DLLs, raw Harmony patches, static state, or native objects owned by other mods; such leftovers are reported as `NeedsRestart`.

## Follow-Up

- ISSUE-010 remains open. Phase 6 reduces long-run owner/input/event residue risk, but does not by itself prove the Mono GC mark-stack crash is solved.
- A future failure package should compare owner ledger, event quarantine, input owner map, Hook queue, resource ledger, and SaveLoad request summary before choosing the next single-axis fix.
- Config preview remains audit-only. A later phase can decide whether to replace real setter preview with a side-effect-free preview model.
