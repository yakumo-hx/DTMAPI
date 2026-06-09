# 20260610-0013 ChestLocator Smoke Case File

## Metadata

- Update ID: 20260610-0013
- Date: 2026-06-10
- Status: verified
- Source: User-requested midterm stabilization plan, step 6: `codex/refactor-smoke-chestlocator-case`
- Owner: Codex

## Summary

- Moved the focused ChestLocatorEnhancer smoke implementation into `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/ChestLocatorEnhancerSmokeCase.cs`.
- Moved only `TryExerciseChestLocatorEnhancerForSmoke`, `CreateChestLocatorSmokeManifest`, and `FormatChestLocatorState`.
- Kept the existing scheduler in `SmokeHarness.cs` and kept `Smoke.ChestLocatorEnhancer`, `Inventory.ChestLocatorEnhancer`, result schema, log text, API/service behavior, and native `CountItem`/`CostItem` semantics unchanged.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Smoke/ContentSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/ChestLocatorEnhancerSmokeCase.cs`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260610-0013-chestlocator-smoke-case-file.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `git diff --check`
  - Only expected CRLF warnings were reported.
- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseChestLocatorEnhancer -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260610-050506`
  - Result JSON: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `ChestLocatorEnhancer=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`
- Passed: `tools/scripts/package-report.ps1 -CaseId GAME-SMOKE`
  - Evidence zip: `docs/debug/evidence/GAME-SMOKE/20260610-050506.zip`

## Evidence

- DTMAPI log:
  - `Feature.ChestLocatorEnhancer = ready`
  - `Inventory.ChestLocatorEnhancer = verified`
  - `Smoke exercise ChestLocatorEnhancer OK item=dtmapi_mine, baseline=0, afterPlace=3, afterCost=1, ... sharedCases=5, ... applications=4`
  - `Smoke.ChestLocatorEnhancer = verified`
- Process/fatal checks:
  - `process-check.txt`: `No DolocTown.exe process found.`
  - `fatal-window-check.txt`: `No fatal instance popup found.`
- Report note:
  - This ChestLocator-only smoke did not export a fresh runtime report zip; `latest-report.txt` still pointed at a prior diagnostics report, so it is not cited as branch report evidence.

## Known Facts And Rejected Hypotheses

- Known fact: this is a mechanical smoke case-file split. The ChestLocatorEnhancer feature, service, hook bridge, public API, policy merge rules, and native callback behavior are not changed.
- Rejected hypothesis: moving the smoke body should update result schema or log wording. This branch deliberately preserves them so prior evidence remains comparable.
- Rejected hypothesis: this branch is a ChestLocator policy/API change. Policy merging was already handled by `20260610-0010-chestlocator-merged-policy.md`.

## Related Records

- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Prior feature split: `docs/updates/2026/20260610-0005-chestlocator-feature-split.md`
- Prior policy merge: `docs/updates/2026/20260610-0010-chestlocator-merged-policy.md`

## Rollback Notes

- Move `TryExerciseChestLocatorEnhancerForSmoke`, `CreateChestLocatorSmokeManifest`, and `FormatChestLocatorState` back into `Smoke/ContentSmoke.cs` if the case-file split complicates follow-up work.
- No external package, report zip, evidence directory, binary, or decompiled source is part of this change.

## Follow-Up

- Continue the midterm route with `SaveSlotsFeature`.
