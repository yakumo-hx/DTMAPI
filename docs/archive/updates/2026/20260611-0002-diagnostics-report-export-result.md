# 20260611-0002 Diagnostics Report Export Result

## Summary

Added a global smoke result field, `DiagnosticsReportExport`, so diagnostics report-export proof is visible across export-capable smoke scenarios instead of only through the AutoFishing-specific compatibility field.

## Source Request

User requested the mid/long next-round plan with current code-level recommendations, including a unified diagnostics report-export smoke field while preserving existing behavior-specific smoke results.

## Changed Files

- `tools/scripts/run-game-smoke.ps1`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/INDEX.md`

## Details

- Added `DiagnosticsReportExport = Passed/Skipped/Failed` to `result.json`.
- The field is requested for smoke scenarios that currently export and verify diagnostics snapshots: Camera, ActionSpeed interaction, and AutoFishing.
- A requested diagnostics-export scenario must log `Smoke.DiagnosticsSnapshot = verified` for the global field to pass; if any requested scenario is missing that proof, the overall smoke run fails.
- Kept `AutoFishingReportExport` unchanged for compatibility with existing review/package scripts.

## Validation

- Passed: `git diff --check`
- Passed: PowerShell script syntax parsing for `tools/scripts/run-game-smoke.ps1`
- Passed: `tools/scripts/build.ps1 -Configuration Release`
- Passed: `tools/scripts/test.ps1 -Configuration Release`
- Passed: Camera smoke `GAME-SMOKE/20260611-024629`
- Passed: ActionSpeed smoke `GAME-SMOKE/20260611-024851`
- Passed: AutoFishing smoke `GAME-SMOKE/20260611-025010`

## Evidence

- Camera smoke `GAME-SMOKE/20260611-024629`: `Zoom=Passed`, `DiagnosticsReportExport=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, fresh report `dtmapi-report-20260611-024813.zip`.
- ActionSpeed smoke `GAME-SMOKE/20260611-024851`: `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `DiagnosticsReportExport=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, fresh report `dtmapi-report-20260611-024931.zip`.
- AutoFishing smoke `GAME-SMOKE/20260611-025010`: `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `AutoFishingReportExport=Passed`, `DiagnosticsReportExport=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, fresh report `dtmapi-report-20260611-025050.zip`.
- Process check after smoke found no residual `DolocTown.exe`.

## Rollback Notes

Remove the `DiagnosticsReportExport` result-field aggregation from `run-game-smoke.ps1` and revert the documentation rows. Existing AutoFishing report-export behavior remains available.
