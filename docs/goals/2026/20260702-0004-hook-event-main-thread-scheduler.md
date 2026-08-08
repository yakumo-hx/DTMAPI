# 20260702-0004 Hook Event Main Thread Scheduler

## Metadata

- Date: 2026-07-02
- Status: implementation
- Source: User request to implement DTMAPI fourth-stage Hook / Event main-thread scheduling layer.
- Owner: DTMAPI

## Goal

Move DTMAPI farther toward a layered runtime by making Hook installation requests, Hook status publication, and safe event dispatch cross a runtime main-thread boundary. External triggers may register requests or enqueue safe events, but actual Harmony patching, Hook status event/log publication, and queued event dispatch happen from the runtime thread.

## Scope

- Add internal scaffold flags:
  - `HookInstallScheduler=true`
  - `HookStatusQueue=true`
  - `EventMainThreadBoundary=true`
  - `HookReadinessLayers=true`
- Add an internal `HookInstallScheduler` so `Initialize`, `AssemblyLoad`, and retry timer callbacks request Hook installation instead of directly patching from their source thread.
- Process pending Hook installation requests from `DolocTownGameBridge.Update()` on the runtime thread.
- Split Hook readiness diagnostics into core, feature, and smoke/diagnostics layers.
- Keep legacy `AllHookTargetsReady` as a compatibility diagnostic only; it no longer controls retry timer/AssemblyLoad lifetime when layered readiness is enabled.
- Add a Hook status publication queue: `DtmApiRuntime.SetHookStatus` updates diagnostics immediately, then queues event/log publication until runtime queue flush.
- Add an EventManager main-thread boundary:
  - safe lifecycle/save/workshop/log/HookStatus events from non-runtime threads are queued;
  - high-frequency update/input events from non-runtime threads are rejected and diagnosed.
- Add runtime report and smoke fields:
  - `HookScheduler`
  - `CoreHookReadiness`
  - `FeatureHookReadiness`
  - `HookStatusQueue`
  - `OffThreadHookRequests`
  - `AssemblyLoadSubscription`
  - `RetryTimerAlive`

## Guardrails

- Keep `RegistryTakesOver=false`.
- Do not change manifest fields, content-pack paths, CustomAnimals fields, AnimalVoice fields, or JSON semantics.
- Do not replace CustomAnimals gameplay logic.
- Do not replace AnimalVoice replacement logic.
- Do not add stable public APIs or change event argument public shapes.
- Do not add new Harmony targets or change Hook patch target signatures.
- Do not delete old fallback paths. Disabling a stage-four flag must restore the corresponding legacy synchronous behavior or leave only legacy diagnostics.
- Long title-idle testing remains a stage-end gate, not a repeated precondition matrix.

## Required Validation

- `tools/scripts/test.ps1 -Configuration Release`
- `git diff --check`
- PowerShell parser check for changed smoke/common scripts.
- Short smoke gate:
  - `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -AutoExerciseTitleButtonLifecycle -TimeoutSeconds 320 -SkipBuild`
  - `tools/scripts/run-game-smoke.ps1 -SaveSlot 7 -AutoExerciseHatchAnimalVoice -TimeoutSeconds 260 -SkipBuild`
- Stage-end long idle gate once short smoke passes:
  - `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -IncludeHookProbe -TitleIdleBeforeSaveSeconds 3600 -TimeoutSeconds 3900 -SkipBuild`

## Acceptance

- Core Hook readiness reaches `ready`.
- Feature Hook readiness may be `partial` without blocking global readiness.
- `AssemblyLoadSubscription` and `RetryTimerAlive` are `released` after core readiness is ready.
- Hook status diagnostics snapshots update immediately, while `HookStatusChanged` events and `Hook status:` log lines publish through runtime queue flush.
- Off-thread safe lifecycle/save/workshop/log/HookStatus events are queued; off-thread Update/Input events are rejected.
- No repeated Hook installation, no off-thread direct patching, no Hook status queue growth, no Fatal GC, and no CustomAnimals/AnimalVoice regression in short smoke.
