# 20260610-0050 AutoFishing Smoke Report Export

## Status

Verified.

## Source Request

User requested the post-`e255191` Fishing follow-up plan. This branch is `codex/fix-autofishing-smoke-report-export`.

## Summary

- AutoFishing smoke now exports a diagnostics report after successful phase/minigame evidence by reusing the existing diagnostics snapshot smoke verifier.
- The smoke verifies that `IDtmDiagnosticsSnapshot.LatestReportPath` exists and equals the report path returned by `runtime.ExportLogs()`.
- The web audit package default evidence now points to fresh AutoFishing report-export evidence instead of the stale AutoFishing hardening smoke ids.
- Removed the package Markdown stale-report caveat that named `dtmapi-report-20260610-171030.zip`; web packages still explain that report zip payloads are omitted.
- Public APIs, hook/status IDs, and smoke result fields are unchanged.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AutoFishingSmokeCase.cs`
- `tools/scripts/update-audit-package.ps1`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0050-autofishing-smoke-report-export.md`

## Validation

- Passed: `git diff --check` (CRLF warnings only).
- Passed: `tools/scripts/build.ps1 -Configuration Release`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoPressAutoFishingHotkey -AutoExerciseAutoFishingPhase -AutoExerciseAutoFishingMiniGameComplete -SaveSlot 3 -TimeoutSeconds 360`.

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260610-221703`.
- Result: `RunStatus=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingHotkey=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Fresh report pointer: `docs/debug/evidence/GAME-SMOKE/20260610-221703/latest-report.txt`.
- Report path: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-221744.zip`.
- Log proof: `Smoke.DiagnosticsSnapshot = verified` records `scenario=AutoFishing AutoFishingMiniGameComplete report export`, `expectedFeatures=FishingAutomation`, `mods=14`, `features=10`, `errors=0`, `warnings=0`, and matching `latestReport`.

## Related Records

- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260610-0048-fishing-lifecycle-doc-consistency.md`
- `docs/updates/2026/20260610-0049-fishing-service-failure-recovery-policy.md`

## Rollback

Revert the AutoFishing report-export call sites and the `autoFishingReportExported` guard. Restore the audit package default evidence list only if the stale report caveat is also restored and clearly marked as non-fresh report evidence.

## Follow-Up

- The final `Refactor` pass should rerun AutoFishing smoke and update the web package default evidence to the final merged smoke id.
- Generate only the compact web audit package for this user-selected route.
