# 20260611-0015 Manager UI MVP Phase 1 Web Audit Package

Date: 2026-06-11

Status: verified

Area: audit/package/web

## Summary

Refreshed the compact web audit package route after merging `codex/manager-ui-mvp-phase1` back to `Refactor`. The package remains web-only and now defaults to the final post-merge Manager MVP smoke plus the final HookProbe runtime regression from this round.

## Source Request

User requested the medium/long Manager UI MVP Phase 1 work from clean `Refactor` / `5ee02f1`, then final compact web audit package refresh only. Full package and full zip generation are intentionally out of scope.

## Changed Files

- `tools/scripts/update-audit-package.ps1`
- `docs/updates/2026/20260611-0015-manager-ui-mvp-phase1-web-audit-package.md`
- `docs/updates/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`

## Behavior

- Default web package evidence now includes:
  - `GAME-SMOKE/20260611-113018` final Manager UI MVP title smoke.
  - `GAME-SMOKE/20260611-031502` final Camera diagnostics report-export smoke.
  - `GAME-SMOKE/20260611-031721` final ActionSpeed diagnostics report-export smoke.
  - `GAME-SMOKE/20260611-031838` final AutoFishing behavior and report-export smoke.
  - `GAME-SMOKE/20260611-113156` final HookProbe runtime smoke.
- The compact web package preserves key logs, result files, startup analysis, process checks, and fatal-window checks.
- Screenshot-heavy evidence directories and report zip payloads remain omitted from the compact web package by design.

## Validation

- `git diff --check`: passed.
- `tools/scripts/build.ps1 -Configuration Release`: passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release`: passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Final DirectExe Manager MVP title smoke: passed.
- Final DirectExe HookProbe smoke: passed.
- Compact web package generation/self-audit:
  - `tools/scripts/update-audit-package.ps1 -WebOnly`
  - `tools/scripts/update-audit-package.ps1 -SelfAuditOnly -WebOnly`

## Evidence

- Final Manager MVP smoke `GAME-SMOKE/20260611-113018` records `RunStatus=Passed`, `ManagerStatusPage=Passed`, `ManagerStatusSummaryText=Passed`, `ManagerStatusPageScreenshot=Passed`, `ManagerStatusPageScreenshotFile=Passed`, `ManagerModsPage=Passed`, `ManagerErrorsPage=Passed`, `ManagerHooksPage=Passed`, `ManagerFeaturesPage=Passed`, `ManagerLogsPage=Passed`, `ManagerLogsExportButton=Passed`, `ManagerLogsExportStateText=Passed`, `ManagerLogsPageScreenshot=Passed`, `ManagerLogsPageScreenshotFile=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Final Manager MVP logs record `Manager Logs export button OK status=exported pathMatch=matched path=D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260611-113103.zip`.
- Final HookProbe smoke `GAME-SMOKE/20260611-113156` records `RunStatus=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Web package directory: `E:\Python_project\DTMAPI-audit-package-Refactor-web`.

## Related Records

- `docs/updates/2026/20260611-0014-manager-ui-mvp-phase1.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`

## Rollback Notes

Revert the default evidence IDs in `tools/scripts/update-audit-package.ps1` and remove this final audit package record. Runtime code and public APIs are unaffected.

## Follow-Up

Copy Summary, Copy selected row, richer page screenshots, and full Manager UI visual polish remain separate Manager UI slices.
