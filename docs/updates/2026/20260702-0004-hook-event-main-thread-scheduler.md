# 20260702-0004 Hook Event Main Thread Scheduler

## Summary

Implemented the fourth-stage guarded runtime split for Hook installation, Hook readiness, Hook status publication, and EventManager dispatch. The change is internal-only: no public API, manifest field, content-pack path, CustomAnimals JSON, AnimalVoice JSON, or Hook target signature was changed, and `RegistryTakesOver=false` remains the required runtime mode.

## Source Request / Goal

- User request: implement "DTMAPI 第四阶段：Hook / Event 主线程调度层".
- Goal record: `docs/goals/2026/20260702-0004-hook-event-main-thread-scheduler.md`.

## Changed Files

- `src/DTMAPI.Core/Runtime/RefactorScaffoldOptions.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/HookStatusPublicationQueue.cs`
- `src/DTMAPI.Core/Services/EventManager.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/HookInstallScheduler.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Update.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tools/scripts/common.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/goals/2026/20260702-0004-hook-event-main-thread-scheduler.md`
- `docs/goals/2026/20260702-0004-hook-event-main-thread-scheduler.goal.txt`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`

## Behavior

- Added internal scaffold flags:
  - `HookInstallScheduler=true`
  - `HookStatusQueue=true`
  - `EventMainThreadBoundary=true`
  - `HookReadinessLayers=true`
- `Initialize`, `AssemblyLoad`, and the hook retry timer now request Hook installation through a scheduler when enabled.
- Actual `InstallHarmonyHooks()` runs from the runtime-thread `DolocTownGameBridge.Update()` path.
- Core Hook readiness is separated from feature and smoke/diagnostic readiness.
- Core readiness controls retry timer and AssemblyLoad subscription release. Feature readiness is diagnostic-only and may remain partial.
- `SetHookStatus` updates diagnostics snapshots immediately, then queues `HookStatusChanged` events and `Hook status:` log lines for runtime-thread flush.
- EventManager queues safe off-thread lifecycle/save/workshop/log/HookStatus events and rejects off-thread Update/Input events.
- Smoke `result.json` now includes `HookScheduler`, `CoreHookReadiness`, `FeatureHookReadiness`, `HookStatusQueue`, `OffThreadHookRequests`, `AssemblyLoadSubscription`, and `RetryTimerAlive`.

## Guardrails

- No `IDtmHelper`, `IEventsHelper`, manifest, content-pack JSON, CustomAnimals, AnimalVoice, or public event args shape change.
- No Registry takeover.
- No new Harmony targets or target signature changes.
- No CustomAnimals or AnimalVoice behavior migration.
- Old fallback behavior remains reachable through local/internal feature flags.

## Validation

- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`; only restricted-network `NU1900` NuGet vulnerability feed warnings appeared.
- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`, `tools/scripts/common.ps1`, and `tools/scripts/test.ps1`: passed.
- `git diff --check`: passed with line-ending warnings only.
- Slot 3 short lifecycle smoke `GAME-SMOKE/20260702-204727`: passed `RunStatus`, `SaveLoaded`, `TitleButtonLifecycle`, lifecycle/scaffold/resource gates, all fourth-stage Hook/Event fields, process exit, and fatal-window checks.
- Slot 7 Hatch AnimalVoice smoke `GAME-SMOKE/20260702-204851`: passed `RunStatus`, `SaveLoaded`, `HatchAnimalVoice`, lifecycle/scaffold/resource gates, all fourth-stage Hook/Event fields, process exit, and fatal-window checks.
- Slot 8 paper-box behavior: accepted from user manual QA plus runtime log evidence in `GAME-SMOKE/20260702-204954`; the automation still failed its `AudioReplacement` result window in `GAME-SMOKE/20260702-204954` and `GAME-SMOKE/20260702-205551`, so this remains smoke harness reliability follow-up, not a fourth-stage runtime behavior blocker.
- 3600-second long title-idle gate `GAME-SMOKE/20260702-210050`: failed and reproduced `Fatal error in GC / Unexpected mark stack overflow` after the title-idle period when loading slot 3. The result still passed `HookScheduler`, `CoreHookReadiness`, `FeatureHookReadiness`, `HookStatusQueue`, `OffThreadHookRequests`, `AssemblyLoadSubscription`, `RetryTimerAlive`, and `TitleIdleResourceGrowth`, so this stage did not introduce observable title-idle Hook/Event/resource growth.

## Evidence

- Source/unit evidence: Release test output from 2026-07-02 in the active Codex run.
- Slot 3 short lifecycle evidence: `docs/debug/evidence/GAME-SMOKE/20260702-204727/result.json`.
- Slot 7 Hatch AnimalVoice evidence: `docs/debug/evidence/GAME-SMOKE/20260702-204851/result.json`.
- Slot 8 paper-box evidence: user manual QA on 2026-07-02, plus `docs/debug/evidence/GAME-SMOKE/20260702-204954/DTMAPI-latest.log` lines showing `AudioReplacement paper-box OnInteract`, `played=True suppressed=True`, `Audio.SoundEventReplacement=verified`, and `Audio.PaperBoxNativeOwner=verified`.
- Long title-idle evidence: `docs/debug/evidence/GAME-SMOKE/20260702-210050/result.json` and `fatal-window-check.txt`. The log had four `LoadGame requested for slot/index 2` lines at 22:00:59, zero `SaveLoaded hook dispatched` lines, and the fatal window appeared before save load completed.
- Title-idle bounded-growth observations from the long gate: `AudioReplacement local WAV ready` stayed at 11, `Refactor shadow content registry` stayed at 4, `CustomAnimals.AnimatorBridge refreshed` stayed at 1, and the stage-four scheduler/readiness/status fields stayed passed.

## Rollback Notes

- Set `HookInstallScheduler=false` to use the legacy direct Hook install path.
- Set `HookStatusQueue=false` to restore synchronous Hook status event/log publication.
- Set `EventMainThreadBoundary=false` to disable new event queue/reject diagnostics.
- Set `HookReadinessLayers=false` to keep legacy mixed readiness behavior controlling retry-source shutdown.

## Follow-Up

- Do not mark ISSUE-010 solved. The stage-four long-idle gate still reproduced the native Fatal GC.
- Next isolation axis should be the post-title-idle save-load transition: debounce/ownership of `LoadGame` requests, native save-load boundary instrumentation, and why four load requests were emitted before `SaveLoaded`.
- Keep the Hook/Event scheduler structure. The long-idle evidence argues against a persistent AssemblyLoad retry, retry timer, Hook status queue, registry scan, WAV ready, or custom-animal animator refresh growth loop during title idle.
