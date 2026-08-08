# 20260702-0001 First Stage Refactor Scaffold

## Metadata

- Date: 2026-07-02
- Status: implementation
- Source: User request to implement the guarded first-stage DTMAPI refactor scaffold.
- Owner: DTMAPI

## Goal

Implement the first guarded refactor stage as observation-only and shadow-only scaffolding. Do not replace existing runtime behavior, content-pack loading, custom animal behavior, AnimalVoice replacement behavior, Hook installation, JSON field semantics, or content-pack paths.

## Scope

- Add internal feature flags for the refactor scaffold.
- Add lifecycle observation counters for startup, title baseline, save load, return to title, second save load, log export, and shutdown.
- Add a shadow content registry that reads the already discovered mods/content packs and reports manifest/custom-animal/audio-replacement summaries and diffs without taking over runtime behavior.
- Add smoke result fields for lifecycle observation, shadow content registry, refactor scaffold flags, and explicit long title-idle-before-save runs.
- Preserve current working content packs: Hatch, Shell Crab, Mole, Drecko, Oilfloater, and ordinary official/content-pack paths.

## Guardrails

- New code must be internal-only or diagnostics-only.
- No stable public API additions.
- No changes to `IDtmHelper`, manifest fields, `custom-animals.json`, `audio-replacements.json`, content-pack paths, custom animal GameBridge behavior, AnimalVoice behavior, or Hook patch installation.
- Feature flags must default to observation/shadow mode only:
  - `LifecycleObservation=true`
  - `ShadowContentRegistry=true`
  - `ShadowResourceLoader=false`
  - `RegistryTakesOver=false`
- If the scaffold is disabled, the old runtime path must keep running without relying on the new layers.

## Required Validation

- `tools/scripts/test.ps1 -Configuration Release`
- If runtime smoke is run, use the shared runtime lock and local third save unless the smoke requires a specific slot:
  - third-save baseline: `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -TimeoutSeconds 220`
  - Hatch AnimalVoice: `tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -AutoExerciseHatchAnimalVoice -TimeoutSeconds 260`
  - legacy audio replacement: `tools/scripts/run-game-smoke.ps1 -SaveSlot 10 -AutoExerciseAudioReplacement -TimeoutSeconds 260`
- Long title-idle-before-save remains explicit only and must not enter the default smoke matrix.

## Acceptance

- Existing mod/content-pack loading results are unchanged.
- Shadow registry only logs or reports diagnostics.
- Lifecycle observer only counts and reports phases.
- Smoke `result.json` remains schema version 2 and gains the new scaffold fields.
- Update record is added under `docs/updates/2026/` and linked from `docs/updates/INDEX.md`.
