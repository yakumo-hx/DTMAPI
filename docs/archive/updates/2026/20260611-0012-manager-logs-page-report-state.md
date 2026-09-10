# 20260611-0012 Manager Logs Page Report State

Status: verified

## Summary

Wired the title-page DTMAPI Settings `Logs` page to the internal Manager report-export result and current Manager model. The page now shows export status, exported path, refreshed snapshot report path, path-match state, snapshot report status, and export failure text while keeping the existing latest log path and Export logs button behavior.

## Source Request

User requested the Manager next-round mid/long hardening plan from clean `Refactor` / `7d2d83d`, including Logs page report state after Manager export/refresh safety, dedicated Status smoke, and severity-model work.

## Changed Files

- `src/DTMAPI.Core/Manager/DtmManagerViewModels.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Behavior

- Added internal `ManagerLogsPageState` over `LastManagerReportExport` and `CurrentManagerModel`.
- The Logs page now prefers the Manager export result and refreshed model for report state display.
- Logs page displays latest log path, export status, exported report path, snapshot `LatestReportPath`, path-match status, snapshot report status, and export failure error text when status is `export-failed`.
- The Export logs button still calls `runtime.UI.ExportLogs()` and does not change Config-first title-button behavior.
- No public mod API, diagnostics snapshot member, ConfigMenu contract, hook/status ID, or smoke result schema changed.

## Validation

- `tools/scripts/build.ps1 -Configuration Release`: passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release`: passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `git diff --check`: passed with expected CRLF warnings only.
- DirectExe title UI smoke: passed.
- DirectExe Status page smoke: passed.
- `Get-Process DolocTown`: no leftover process after smoke runs.

## Evidence

- Title UI smoke `GAME-SMOKE/20260611-102823` records `RunStatus=Passed`, `TitleSettingsButton=Passed`, `TitleSettingsButtonScreenshot=Passed`, `TitleSettingsButtonScreenshotFile=Passed`, `TitleSettingsMenu=Passed`, `TitleSettingsMenuScreenshot=Passed`, `TitleSettingsMenuScreenshotFile=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Status page smoke `GAME-SMOKE/20260611-102922` records `RunStatus=Passed`, `TitleSettingsButton=Passed`, `TitleSettingsButtonScreenshot=Passed`, `TitleSettingsButtonScreenshotFile=Passed`, `TitleSettingsMenu=Passed`, `TitleSettingsMenuScreenshot=Passed`, `TitleSettingsMenuScreenshotFile=Passed`, `ManagerStatusPage=Passed`, `ManagerStatusPageScreenshot=Passed`, `ManagerStatusPageScreenshotFile=Passed`, `ManagerStatusSummaryText=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Status smoke logs include `Manager Status summary text OK overall=ready; mods=loaded:16,blocked:0,disabled:0; diagnostics=errors:0,warnings:0; hooks=failed:0,missing:0; features=failed:0,degraded:0; report=ready; ...`.
- Unit coverage in `ManagerRuntimeProviderRefreshesSnapshotAfterReportExport` verifies `ManagerLogsPageState` for `not-exported`, `exported`, `missing-export-path`, `report-path-mismatch`, and `export-failed` states, including exported path, snapshot report path, path-match status, and error text.

## Related Records

- `docs/updates/2026/20260611-0009-manager-export-refresh-safety.md`
- `docs/updates/2026/20260611-0010-manager-status-page-smoke.md`
- `docs/updates/2026/20260611-0011-manager-status-severity-model.md`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/debug/regressions/smoke-matrix.md`

## Rollback Notes

Revert `ManagerLogsPageState`, the Logs page rendering additions, and the corresponding unit tests. The previous Logs page only showed the latest log path and last export path, with export mismatch/failure state visible only through Status/runtime state.

## Follow-Up

Final `Refactor` validation should rerun build/test, the dedicated Manager Status page smoke, and HookProbe runtime smoke, then refresh only the compact web audit package.
