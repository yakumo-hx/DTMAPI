# 20260703-0006 AutoFishing Lifecycle Attribution Audit

## Objective

Open phase 4.5 of the guarded rebuild: audit AutoFishing lifecycle/resource retention risk without rewriting fishing gameplay.

## Guardrails

- Keep F6 behavior unchanged.
- Keep `IFishingAutomationApi` semantics unchanged.
- Keep the fifth-save real-loop smoke as the AutoFishing behavior gate.
- Do not change Hook targets, Harmony signatures, config field names, content-pack paths, JSON semantics, Registry takeover, CustomAnimals, or AnimalVoice behavior.
- Add internal diagnostics only: lifecycle summary, resource ledger entries, boundary assertions, and smoke result fields.

## Required Checks

- Classify every `FishingAutomationService` `Dictionary`/`HashSet` and native object key by owner/lifetime.
- Verify `SaveLoaded`, `ReturnedToTitle`, `SetEnabled(false)`, `AgentStateFishingPull.OnExit`, `FishingGameScrollBar.StopGame`, and the pending-cast watchdog clear DTMAPI-owned transient state.
- Keep `EnvironmentReset` non-destructive because it is driven by high-frequency environment/camera refresh; document why this remains reasonable.
- Audit `AutoFishingMod` input registration, `ReturnedToTitle` disable, and config-save behavior for owner/lifecycle risk.
- Confirm fourth-stage Hook/Event main-thread scheduling does not change AutoFishing Hook readiness or status semantics.
- Investigate the reported "no charge still charges" symptom by exposing the runtime `CastChargeRatio` and fast-animation setting in smoke/lifecycle summaries.

## Outputs

- Internal-only AutoFishing lifecycle snapshot and summary.
- Resource ledger entries for owner policy/state and DTMAPI-held borrowed native handles.
- Boundary assertions that only warn/report.
- Smoke `result.json` fields: `AutoFishingLifecycle`, `AutoFishingLifecycleSummary`, `AutoFishingSoak`.
- Optional fifth-save short soak via fixed loop count, disabled by default.
- Update/review/debug records with validation and rollback notes.

## Validation

- `tools/scripts/test.ps1 -Configuration Release`
- `git diff --check`
- PowerShell parser check for changed scripts.
- Optional runtime gate after source validation:
  - `tools/scripts/run-game-smoke.ps1 -SaveSlot 5 -AutoExerciseAutoFishingPhase -AutoFishingSoakLoops 3 -TimeoutSeconds 420 -SkipBuild`

