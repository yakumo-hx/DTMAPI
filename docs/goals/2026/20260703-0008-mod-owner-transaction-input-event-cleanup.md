# 20260703-0008 Mod Owner Transaction Input Event Cleanup

## Goal

Implement DTMAPI phase 6: internal mod owner, load transaction, owner-bound input, event quarantine, and config-preview side-effect diagnostics. This phase makes DTMAPI-owned registrations attributable and rollback-capable without changing gameplay systems, public APIs, manifest/content-pack fields, or JSON semantics.

## Guardrails

- Do not change CustomAnimals, AnimalVoice, AutoFishing gameplay, content-pack paths, manifest fields, or public API shapes.
- Keep `RegistryTakesOver=false`.
- Do not unload already-loaded code mod DLLs when an official/Workshop enablement state changes during the same process.
- Do not clear long-lived Entry-time hotkey registrations on returned-to-title; only clear transient input down/pressed/suppressed state.
- Do not change config menu preview behavior in this phase; only audit and report real setter/restore side effects.
- Raw Harmony patches, static state, third-party native references, and unmanaged side effects remain `NeedsRestart` when DTMAPI cannot prove cleanup.

## Implementation Scope

- Add internal scaffold flags:
  - `ModOwnerLedger`
  - `ModLoadTransaction`
  - `OwnerBoundInput`
  - `EventHandlerQuarantine`
  - `ConfigPreviewAudit`
- Add an internal owner ledger and mod-load transaction summary for API, event, input, config menu, and custom entity registrations.
- Make `DtmHelper.Input` owner-bound while keeping `IInputHelper` unchanged.
- Upgrade failed Entry cleanup so DTMAPI-owned partial registrations are rolled back and reported by kind.
- Move failed high-frequency event handlers into quarantine instead of leaving disabled entries in the active dispatch path.
- Add config preview audit diagnostics for setter apply/restore counts and failures.
- Add smoke/result fields:
  - `ModOwnerLifecycle`
  - `ModOwnerLifecycleSummary`
  - `ModLoadTransaction`
  - `OwnerBoundInput`
  - `EventHandlerCleanup`
  - `ConfigPreviewAudit`
  - `FailedModRollback`

## Validation Plan

- Source checks:
  - `tools/scripts/test.ps1 -Configuration Release`
  - `git diff --check`
  - PowerShell parser check for changed smoke scripts.
- Unit coverage:
  - flag defaults, config overrides, and environment overrides;
  - owner-bound input multi-owner registration, unregister, and owner cleanup;
  - partial Entry failure rollback for API/event/input/config/custom entity surfaces;
  - high-frequency event quarantine and owner cleanup of active plus quarantined handlers;
  - config preview audit observes apply/restore without changing save/cancel/reset semantics;
  - all new flags disabled means new diagnostics do not execute.
- Short smoke gate:
  - slot 3: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseTitleButtonLifecycle -TimeoutSeconds 320 -SkipBuild`
  - slot 7: `tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -AutoExerciseHatchAnimalVoice -TimeoutSeconds 260 -SkipBuild`
  - slot 5: `tools/scripts/run-game-smoke.ps1 -SaveSlot 5 -AutoExerciseAutoFishingPhase -AutoFishingSoakLoops 3 -TimeoutSeconds 420 -SkipBuild`

## Expected Evidence

- `RegistryTakesOver=false` remains visible.
- F6 AutoFishing, Y/HookProbe, and migrated hotkeys still dispatch.
- A mod whose Entry throws after partial DTMAPI-owned registration leaves no API/event/input/config/custom entity registrations.
- Quarantined high-frequency handlers are not kept in the active dispatch hot path.
- Runtime report and smoke output list owner registrations, rollback results, and any `NeedsRestart` side effects.
