# 20260616-0003 GameBridge Hook Feature Partials

## Status

verified

## Area

gamebridge/hooks/maintainability

## Source Request / Goal

Continue refactoring code on `codex/refactor-four-step-cleanup` after the first four-step cleanup branch.
This slice targets `DolocTownGameBridge.cs`, which was still carrying both feature-host dispatch/status logic and the central Harmony hook installer in the same large file.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Features.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.Hooks.cs`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Summary

- Moved GameBridge feature dispatch, feature-status publication, failure throttling, and recovery bookkeeping into `DolocTownGameBridge.Features.cs`.
- Moved central Harmony hook installation and assembly-load retry handling into `DolocTownGameBridge.Hooks.cs`.
- Kept hook ids, status text, feature dispatch order, retry/timer behavior, and callback targets unchanged.
- Reduced `DolocTownGameBridge.cs` by moving two cohesive responsibilities out of the already-large host file.

## Validation

- `git diff --check`
  - Passed.
- `tools/scripts/test.ps1 -Configuration Release`
  - Passed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- `tools/scripts/run-game-smoke.ps1 -UseSteam -IncludeHookProbe -AutoExerciseInstantSave -SaveSlot 3 -TimeoutSeconds 240 -AutoExitAfterSecondsOverride 90`
  - Passed.

## Evidence

- Steam third-save smoke evidence: `docs/debug/evidence/GAME-SMOKE/20260616-082044`.
- Result fields passed: `RunStatus`, `StartupLog`, `GameLaunched`, `HookProbe`, `SaveLoaded`, `InstantSave`, `ProcessExited`, `NoFatalInstanceWindow`, and `ForcedClose`.
- Key log evidence:
  - `DTMAPI runtime starting.`
  - `HookProbe GameLaunched OK`
  - `Feature.Camera=ready`
  - `Feature.FishingAutomation=ready`
  - `Feature.SaveSlots=ready`
  - `Save.SaveSaving=experimental`
  - `Save.SaveSaved=experimental`
  - `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`
  - `HookProbe SaveLoaded OK slot=2 isNewGame=False`
  - `Smoke exercise InstantSave OK ... sameRoom=True, distance=0, reloadDisabled=True`
- Exit check: `process-check.txt` reports `No DolocTown.exe process found.`

## Related Records

- `docs/updates/2026/20260615-0008-harmony-target-signature-round3.md`
- `docs/updates/2026/20260615-0014-review-followup-save-content-input.md`
- `docs/debug/regressions/smoke-matrix.md`

## Rollback Notes

Rollback by moving the feature-host and hook-installer methods back into `DolocTownGameBridge.cs` and deleting the two new partial files.
No public API or hook target behavior is intentionally changed by this split.

## Follow-Up

- Continue splitting large GameBridge responsibilities in behavior-preserving slices.
- Good next candidates are smoke-harness scheduler state and remaining diagnostics/debug API surface, but each hook-bearing slice should keep its own smoke evidence.
