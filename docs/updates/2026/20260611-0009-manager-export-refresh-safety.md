# 20260611-0009 Manager Export Refresh Safety

## Summary

Added internal exception safety around Manager report export and Manager model refresh so title-page UI callbacks do not throw when export, snapshot refresh, or file IO fails.

## Source Request

User requested the Manager long-term hardening plan from clean `Refactor` / `7d2d83d`, with the first branch focused on Manager export/refresh exception safety and no public API changes.

## Changed Files

- `src/DTMAPI.Core/Manager/DtmManagerRuntimeModelProvider.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Details

- `DtmManagerReportExportResult` now supports internal `export-failed` results with an error message.
- `UiRuntimeService.ExportLogs()` catches Manager provider/export/snapshot failures, returns an empty path on failure, records `DTMAPI.ManagerUI` diagnostics, and preserves the last known Manager model.
- `UiRuntimeService.RefreshDtmManagerModel()` catches snapshot refresh failures, records `LastManagerRefreshError`, and preserves the last known model.
- The title Status/Logs pages display refresh/export failure text instead of letting UI callbacks fail.
- No public API members, `IDtmDiagnosticsApi`, `IUiHelper`, ConfigMenu behavior, GameBridge hooks/features, hook/status IDs, or smoke result schema changed.

## Validation

- Passed:
  - `git diff --check`
  - `tools/scripts/build.ps1 -Configuration Release`
  - `tools/scripts/test.ps1 -Configuration Release`
- Passed title UI smoke:
  - Command: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoOpenTitleSettingsMenu -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260611-095311`

## Evidence

- Unit coverage verifies:
  - successful Manager report export still returns `exported`
  - report path mismatch still returns `report-path-mismatch`
  - export exceptions return `export-failed` without throwing
  - refresh exceptions preserve the last known model and record `DTMAPI.ManagerUI`
- Title UI smoke `GAME-SMOKE/20260611-095311` records `RunStatus=Passed`, `TitleSettingsButton=Passed`, `TitleSettingsButtonScreenshot=Passed`, `TitleSettingsButtonScreenshotFile=Passed`, `TitleSettingsMenu=Passed`, `TitleSettingsMenuScreenshot=Passed`, `TitleSettingsMenuScreenshotFile=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.

## Rollback Notes

Revert the internal `export-failed` result, the `UiRuntimeService` catch blocks, and Status/Logs failure display text. Public API rollback is not required because no public surface changed.

## Follow-Up

- Add a dedicated Manager Status page smoke field in the next branch.
- Refine Manager severity counts so missing hooks and degraded features are represented separately.
