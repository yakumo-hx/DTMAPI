# 20260702-0002 Lifecycle Boundary Contract

## Metadata

- Date: 2026-07-02
- Status: implementation
- Source: User request to implement DTMAPI second-stage lifecycle layer convergence.
- Owner: DTMAPI

## Goal

Add an internal lifecycle boundary contract and diagnostics layer over the first-stage lifecycle observer. This stage defines and reports lifecycle rules for `Startup`, `TitleObserved`, `SaveLoaded`, `ReturnedToTitle`, `SecondSaveLoaded`, `LogExport`, and `Shutdown` without taking over existing runtime behavior.

## Scope

- Define internal-only lifecycle boundary policies for retained caches, required releases, registry refresh expectations, Hook install expectations, and title-return cleanup.
- Record phase, shadow registry refresh, Hook status publication, and returned-to-title retention diagnostics.
- Expose results through existing diagnostics feature statuses, warnings, runtime report context, and smoke result fields.
- Keep `RegistryTakesOver=false`; keep `ShadowResourceLoader=false`.

## Guardrails

- Do not add stable public APIs.
- Do not change `IDtmHelper`, manifest fields, content-pack JSON fields, CustomAnimals fields, AnimalVoice fields, or content-pack paths.
- Do not replace CustomAnimals runtime logic, AnimalVoice runtime logic, Hook patch installation, resource loading, or old fallback behavior.
- Do not reorder `SaveLoaded`, `ReturnedToTitle`, log export, or shutdown dispatch.
- Contract violations are diagnostics only; they must not block old runtime flows.
- If `LifecycleObservation=false`, the lifecycle boundary contract must not record or execute.

## Known Inputs

- First-stage scaffold validation passed source checks, slot 3 lifecycle smoke, slot 7 Hatch AnimalVoice smoke, user manual slot 7 custom-animal QA, user manual slot 8 paper-box QA, and one explicit 3600-second title-idle smoke.
- ISSUE-010 long-run Mono/GC crash remains open/evidence-improved. The first-stage long-idle pass proves the scaffold did not reproduce the crash in that gate; it does not prove the root cause is fixed.
- Slot 8 paper-box automatic smoke still has an interaction-fixture automation follow-up; user manual QA confirmed runtime behavior normal.

## Required Validation

- `tools/scripts/test.ps1 -Configuration Release`
- Unit coverage for lifecycle boundary phase order, duplicate registry refresh diagnostics, duplicate Hook install-signal diagnostics, runtime report context, and feature-flag disable behavior.
- Runtime smoke when practical:
  - `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseTitleButtonLifecycle -TimeoutSeconds 320 -SkipBuild`
  - `tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -AutoExerciseHatchAnimalVoice -TimeoutSeconds 260 -SkipBuild`
  - Slot 8 paper-box behavior may remain backed by user manual evidence unless the smoke fixture automation is repaired in a separate goal.
- Long title-idle `3600` seconds is explicit only and should be rerun after this lifecycle contract stage before treating the stage as release-ready.

## Acceptance

- Existing content-pack and code-mod loading behavior is unchanged.
- Lifecycle boundary contract reports `ok` or non-blocking `warning`; no `error` in normal short smoke.
- `RegistryTakesOver=false` remains visible in logs and result JSON.
- No duplicate Hook installation, repeated lifecycle-driven registry refresh, Fatal GC, red runtime errors, or leftover `DolocTown.exe` in validation evidence.
- Update record is added under `docs/updates/2026/` and linked from `docs/updates/INDEX.md`.
