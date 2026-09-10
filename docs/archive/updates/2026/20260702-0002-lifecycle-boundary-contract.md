# 20260702-0002 Lifecycle Boundary Contract

## Status

source-and-short-smoke-verified / long-idle-fatal-reproduced / paper-box-automation-follow-up

## Source Request

User requested DTMAPI second-stage lifecycle-layer convergence. The goal was to make lifecycle boundaries contractual and diagnostic while leaving the existing CustomAnimals, AnimalVoice, Registry, Hook installation, JSON semantics, content-pack paths, and old fallback behavior unchanged.

## Changed Files

- `docs/goals/2026/20260702-0002-lifecycle-boundary-contract.md`
- `docs/goals/2026/20260702-0002-lifecycle-boundary-contract.goal.txt`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `src/DTMAPI.Core/Runtime/LifecycleBoundaryContractService.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/AudioReplacement/AudioReplacementService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/CustomAnimals/CustomAnimalAnimatorBridgeService.cs`
- `tools/scripts/common.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/updates/INDEX.md`

## Implementation Notes

- Added an internal lifecycle boundary contract service for `Startup`, `TitleObserved`, `SaveLoaded`, `ReturnedToTitle`, `SecondSaveLoaded`, `LogExport`, and `Shutdown`.
- The contract records phase counts, registry refresh counts by phase and reason, Hook status/install-signal counts, known animal/audio resource event counts, timeline entries, and non-blocking diagnostics.
- The declared phase policies list retained caches, required releases, registry refresh limits, Hook install expectations, and return-to-title cleanup expectations for report context. They are diagnostic only.
- `DtmApiRuntime` now observes lifecycle boundaries from existing runtime entry points without changing dispatch order or public events.
- `NotifyReturnedToTitle()` keeps the old order: observe, clear `currentLoadingSlot`, clear custom entity runtime instances, run returned-to-title boundary cleanup, then raise the public event.
- Audio replacement and custom animal animator bridge now publish resource-count observations beside existing logs. These observations do not release, reload, suppress, or otherwise change resource behavior.
- The lifecycle contract is gated by the existing `LifecycleObservation` scaffold flag. When lifecycle observation is disabled, the second-stage contract does not execute.
- `RegistryTakesOver=false` remains the expected state and is still surfaced through smoke results and diagnostics. No stable public API was added.
- Smoke result parsing now reports `LifecycleBoundaryContract` and `LifecycleBoundaryContractSummary`.
- Fatal-window detection was hardened after a long-idle false-pass: the smoke harness now checks for `Fatal error in GC` and `Unexpected mark stack overflow` both through window text and the `DolocTown.exe` `MainWindowTitle` while the process is still alive.

## Validation

- Passed: `tools/scripts/test.ps1 -Configuration Release` on 2026-07-02.
  - `DTMAPI.UnitTests: OK`.
  - Warnings were restricted-network `NU1900` package vulnerability index warnings.
- Passed: PowerShell parser check for `tools/scripts/common.ps1` and `tools/scripts/run-game-smoke.ps1`.
- Unit coverage verifies:
  - lifecycle boundary phase ordering and second-save recognition;
  - duplicate registry refresh diagnostics;
  - duplicate actual Hook install-signal diagnostics;
  - resource event counting without warning spam;
  - runtime report context exposure;
  - all-refactor-flags-off behavior suppresses lifecycle/shadow diagnostics.
- Passed: slot 3 lifecycle short smoke `docs/debug/evidence/GAME-SMOKE/20260702-152614`.
  - `RunStatus`, `StartupLog`, `GameLaunched`, `SaveLoaded`, `TitleButtonLifecycle`, `LifecycleObservation`, `ShadowContentRegistry`, `RefactorScaffoldFlags`, `LifecycleBoundaryContract`, `ProcessExited`, and `NoFatalInstanceWindow` passed.
  - Lifecycle contract summary reported `status=ok`, `warnings=0`, and `errors=0`.
- Passed: slot 7 Hatch AnimalVoice short smoke `docs/debug/evidence/GAME-SMOKE/20260702-152728`.
  - `RunStatus`, `StartupLog`, `GameLaunched`, `SaveLoaded`, `HatchAnimalVoice`, `LifecycleObservation`, `ShadowContentRegistry`, `RefactorScaffoldFlags`, `LifecycleBoundaryContract`, `ProcessExited`, and `NoFatalInstanceWindow` passed.
  - The previous false Hook duplicate warning from `Audio.SoundEventReplacement=verified` was removed by narrowing install-signal detection.
- Paper-box automation remains a smoke-harness follow-up:
  - Slot 10 automatic smoke `docs/debug/evidence/GAME-SMOKE/20260702-151833` passed startup/save/lifecycle/contract/fatal/process checks but failed `AudioReplacement` because the scripted interaction did not trigger the fixture.
  - The user already confirmed on 2026-07-02 that the slot 8 paper-box manual path is normal. This was accepted as behavior evidence for this lifecycle-only stage and was not mixed with an automation repair.
- Long title-idle 3600-second special run `docs/debug/evidence/GAME-SMOKE/20260702-152855` reproduced the known GC crash symptom:
  - During the title idle window there was no continuous shadow registry scanning, no repeated Hook installation, and no continuing audio/resource load growth after the initial load burst.
  - After 3600 seconds, the smoke entered slot 3 and logged `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`.
  - The lifecycle boundary contract still reported `status=ok`, `warnings=0`, and `errors=0`.
  - At 2026-07-02 16:41 +08:00, the live `DolocTown.exe` process showed `MainWindowTitle : Fatal error in GC`.
  - Manual evidence was saved to `docs/debug/evidence/GAME-SMOKE/20260702-152855/long-title-idle-fatal-gc-observation.txt`.
  - The generated `result.json` from that run incorrectly says `NoFatalInstanceWindow=Passed` because the old smoke check only looked after the process was killed. The script was fixed after the run, so this evidence is classified as failed/reproduced despite the stale result field.

## Evidence

- Goal handoff: `docs/goals/2026/20260702-0002-lifecycle-boundary-contract.md`.
- Passed source validation: `tools/scripts/test.ps1 -Configuration Release`, 2026-07-02, `DTMAPI.UnitTests: OK`.
- Passed slot 3 lifecycle smoke: `docs/debug/evidence/GAME-SMOKE/20260702-152614`.
- Passed slot 7 Hatch AnimalVoice smoke: `docs/debug/evidence/GAME-SMOKE/20260702-152728`.
- Slot 10 automation-only paper-box failure with lifecycle contract passing: `docs/debug/evidence/GAME-SMOKE/20260702-151833`.
- Long title-idle Fatal GC reproduction: `docs/debug/evidence/GAME-SMOKE/20260702-152855/long-title-idle-fatal-gc-observation.txt`.
- Related long-run issue: `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`.

## Rollback

Disable the second-stage diagnostics without changing old runtime behavior by setting the existing scaffold flags off in `DTMAPI/config/refactor-scaffold.json`:

```json
{
  "LifecycleObservation": false,
  "ShadowContentRegistry": false,
  "ShadowResourceLoader": false,
  "RegistryTakesOver": false
}
```

For source rollback, remove the internal lifecycle contract service and the observation calls in `DtmApiRuntime`, `AudioReplacementService`, and `CustomAnimalAnimatorBridgeService`. No player-facing config fields, content-pack JSON fields, public APIs, or Hook patch installation paths were introduced.

## Follow-Up

- Keep ISSUE-010 open. The second-stage diagnostics show the long-idle crash can occur even when lifecycle contract status remains ok and no obvious repeated registry/Hook/resource loop appears in the DTMAPI logs.
- Use the new contract/resource counters as the starting evidence for the next resource/lifecycle cleanup investigation. The next phase should focus on title-return/save-load resource ownership and native/Unity object retention, not on replacing CustomAnimals or AnimalVoice behavior.
- Repair the slot 8 paper-box automation separately so it can produce a green automatic `AudioReplacement=Passed` result without manual input.
