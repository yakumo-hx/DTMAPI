# 20260703-0006 AutoFishing Lifecycle Attribution Audit

## Summary

Implemented phase 4.5 as an internal-only AutoFishing lifecycle attribution layer. The change adds owner/transient classification, resource ledger observations, boundary assertions, and optional fifth-save soak fields without changing F6 behavior, `IFishingAutomationApi`, fishing Hook targets, config fields, JSON semantics, Registry takeover, CustomAnimals, or AnimalVoice behavior.

## Source Request / Goal

- User request: open "DTMAPI 第 4.5 阶段：AutoFishing 生命周期归因审计" and also check whether AutoFishing charge ratio appears hard-coded in upload/subscription behavior.
- Goal record: `docs/goals/2026/20260703-0006-autofishing-lifecycle-attribution-audit.md`.
- Review record: `docs/reviews/code/2026/20260703-0001-autofishing-lifecycle-attribution-audit.md`.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/FishingAutomationService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/goals/2026/20260703-0006-autofishing-lifecycle-attribution-audit.md`
- `docs/goals/2026/20260703-0006-autofishing-lifecycle-attribution-audit.goal.txt`
- `docs/reviews/code/2026/20260703-0001-autofishing-lifecycle-attribution-audit.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/issues/ISSUE-010-20260620-long-run-mono-gc-crash.md`
- `docs/hook-map/README.md`

## Behavior

- Added internal `FishingAutomationLifecycleSnapshot` and `Fishing.Automation.Lifecycle` diagnostics.
- Classified long-lived owner/config state separately from native-object-keyed transient state.
- Added resource lifecycle ledger entries for:
  - owner policies and owner state,
  - mini-game handles,
  - Ready charge state handles,
  - animator speed snapshots,
  - hook physics snapshots.
- Added report-only boundary assertions for `SetEnabled(false)`, `SaveLoaded`, `ReturnedToTitle`, `FishingGameScrollBar.StopGame`, `AgentStateFishingPull.OnExit`, and pending-cast watchdog release.
- Ready charge state references now close when the native phase leaves `Ready`; `FishingGameScrollBar.StopGame` records mini-game handle cleanup, while full transient-clear assertions remain on destructive/native-exit boundaries.
- Kept `EnvironmentReset` non-destructive and low-frequency observed only.
- Added smoke `result.json` fields:
  - `AutoFishingLifecycle`
  - `AutoFishingLifecycleSummary`
  - `AutoFishingSoak`
- Added explicit `-AutoFishingSoakLoops` smoke parameter. Default is `0`; no long AutoFishing matrix runs unless requested.

## Charge-Ratio Finding

No source hard-code of a nonzero charge ratio was found. The smoke harness defaults to `AutoFishingCastChargeRatio=0`, and `AutoFishingMod` passes `config.CastChargeRatio` into `FishingAutomationOptions`. The lifecycle summary now includes owner policy values like `charge=0`, `fast=True/False`, and multiplier so future package/upload evidence can distinguish stale config or fast-animation visual tick behavior from a real code defect.

Local runtime/package check on 2026-07-03 found `D:\Steam\steamapps\common\Doloc Town\DTMAPI\config\Yuuka.DTMAPI.AutoFishing.json` with `CastChargeRatio=0`, and no matching `Yuuka.DTMAPI.AutoFishing.json` was found under the local Steam Workshop subscription cache for app `2285550`.

## Validation

- `tools/scripts/test.ps1 -Configuration Release`: passed with `DTMAPI.UnitTests: OK`.
- `git diff --check`: passed; Git reported existing line-ending normalization warnings only.
- PowerShell parser check for `tools/scripts/run-game-smoke.ps1`: passed.
- Fifth-save short soak passed under runtime lock:
  - Command: `tools/scripts/run-game-smoke.ps1 -SaveSlot 5 -AutoExerciseAutoFishingPhase -AutoFishingSoakLoops 3 -TimeoutSeconds 420 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260703-194001`
  - Key result fields: `RunStatus=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `AutoFishingLifecycle=Passed`, `AutoFishingSoak=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`.
  - Lifecycle summary included `charge=0;fast=False;mult=3`, `nativeTransientHandles=0`, `boundaryClearCount=0`, `miniGameHandles=0`, `readyChargeStates=0`, `animators=0`, `hookPhysics=0`, and `pendingCast=False`.
- Validation emitted restricted-network `NU1900` vulnerability-feed warnings only.

## Rollback Notes

- Remove or ignore `Fishing.Automation.Lifecycle`, `Refactor.AutoFishingLifecycle`, and `Smoke.AutoFishingSoak` diagnostics to return to the prior source behavior.
- Do not delete existing AutoFishing reset/disable/watchdog paths; they are the old fallback and remain the behavior path.
- The new smoke `-AutoFishingSoakLoops` parameter is opt-in and can be left unused.

## Follow-Up

- No AutoFishing FSM rewrite is justified by this evidence alone.
- If ISSUE-010 remains after the next long-title gate, continue with the post-title-idle save-load transition axis rather than expanding AutoFishing into a broad matrix.
- If a player still reports "no charge still charges", collect the runtime lifecycle summary and the active `Yuuka.DTMAPI.AutoFishing.json` first; source and local runtime evidence currently show `CastChargeRatio=0`.
