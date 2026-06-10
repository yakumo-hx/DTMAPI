# 20260610-0054 AutoFishing Report Export Result Field

## Status

Verified.

## Source Request

User requested the mid/long-term follow-up plan from `Refactor` baseline `c7c8498`. This branch is `codex/test-autofishing-report-export-result`.

## Summary

- Added `AutoFishingReportExport` to `tools/scripts/run-game-smoke.ps1` result output.
- Kept `AutoFishingMiniGameComplete` focused on the fishing behavior path and separated diagnostics report export into the new result field.
- Made requested AutoFishing report-export failure fail the overall `RunStatus` while preserving the true behavior result fields for easier triage.
- Did not change public APIs, hook/status IDs, AutoFishing behavior, game files, external package directories, report zip payloads, or reverse/decompiled source.

## Changed Files

- `tools/scripts/run-game-smoke.ps1`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0054-autofishing-report-export-result-field.md`

## Validation

- Passed: `git diff --check`.
- Passed: `tools/scripts/build.ps1 -Configuration Release`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoPressAutoFishingHotkey -AutoExerciseAutoFishingPhase -AutoExerciseAutoFishingMiniGameComplete -SaveSlot 3 -TimeoutSeconds 360`.

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260611-000932`.
- Result: `RunStatus=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `AutoFishingReportExport=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Fresh report pointer: `docs/debug/evidence/GAME-SMOKE/20260611-000932/latest-report.txt`.
- Report path: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260611-001013.zip`.
- Log proof: `Smoke.DiagnosticsSnapshot = verified` records `scenario=AutoFishing AutoFishingMiniGameComplete report export`, `expectedFeatures=FishingAutomation`, `mods=14`, `features=10`, `errors=0`, `warnings=0`, and matching `latestReport`.

## Rollback

Remove the `AutoFishingReportExport` variable, wait step, result field, and `RunStatus` aggregation term from `run-game-smoke.ps1`, then restore the previous docs that treated report export as implicit AutoFishing behavior evidence.

## Follow-Up

- After the branch is merged back to `Refactor`, rerun final AutoFishing smoke and point the compact web package defaults at the final evidence id.
