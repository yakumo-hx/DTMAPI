# 20260611-0010 Manager Status Page Smoke

Status: verified

## Summary

Added a dedicated Manager Status page smoke path so `run-game-smoke.ps1 -AutoOpenTitleSettingsStatusPage` opens the title-page DTMAPI Settings menu, switches to the internal Manager Status page, records summary text, captures a Status page screenshot, and reports the result through new smoke fields.

## Source Request

User requested the Manager next-round mid/long hardening plan from clean `Refactor` / `7d2d83d`, including a dedicated Manager Status page smoke before severity-model and Logs-page follow-ups.

## Changed Files

- `tools/scripts/run-game-smoke.ps1`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/updates/INDEX.md`

## Behavior

- Added `-AutoOpenTitleSettingsStatusPage`.
- Existing `TitleSettingsButton` and `TitleSettingsMenu` result fields are requested for either the legacy title menu smoke or the new Status-only smoke.
- Added result fields:
  - `ManagerStatusPage`
  - `ManagerStatusPageScreenshot`
  - `ManagerStatusPageScreenshotFile`
  - `ManagerStatusSummaryText`
- The Status-only path does not run the legacy Config page screenshot rotation.
- The Status-only path keeps the third-save command argument in the smoke summary but does not auto-load the save; this keeps title-page Status evidence isolated from the official save UI flow.
- GameBridge logs a Manager summary text line with overall status, mod counts, diagnostics counts, failed/missing hook counts, failed/degraded feature counts, and latest log/report path state.
- Status page screenshot capture is delayed after the generic title menu screenshot so Unity does not drop the first screenshot when two captures are requested too close together.

## Validation

- `tools/scripts/build.ps1 -Configuration Release`: passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release`: passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- PowerShell AST parse of `tools/scripts/run-game-smoke.ps1`: passed.
- `git diff --check`: passed with expected CRLF warnings only.
- DirectExe Status page smoke: passed.

## Evidence

- First attempted DirectExe Status smoke `GAME-SMOKE/20260611-100522` proved the Status page opened and summary/screenshot statuses passed, but failed `TitleSettingsMenuScreenshotFile` because the generic menu screenshot and Status screenshot were requested too close together; the final patch delayed Status capture and isolated Status-only smoke from auto-load.
- Final DirectExe Status smoke `GAME-SMOKE/20260611-100827` records `RunStatus=Passed`, `TitleSettingsButton=Passed`, `TitleSettingsButtonScreenshot=Passed`, `TitleSettingsButtonScreenshotFile=Passed`, `TitleSettingsMenu=Passed`, `TitleSettingsMenuScreenshot=Passed`, `TitleSettingsMenuScreenshotFile=Passed`, `ManagerStatusPage=Passed`, `ManagerStatusPageScreenshot=Passed`, `ManagerStatusPageScreenshotFile=Passed`, `ManagerStatusSummaryText=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Log evidence from `GAME-SMOKE/20260611-100827` includes `Smoke automation opened DTMAPI Manager Status page.`, `Manager Status summary text OK overall=ready; mods=loaded:16,blocked:0,disabled:0; diagnostics=errors:0,warnings:0; hooks=failed:0,missing:0; features=failed:0,degraded:0; report=ready; ...`, and `Manager Status page screenshot OK screenshot=...manager-status-page.png`.

## Related Records

- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`

## Rollback Notes

Revert the smoke script switch, the GameBridge smoke settings/member additions, and the `Smoke.ManagerStatus*` status recorders. This rollback leaves the existing `-AutoOpenTitleSettingsMenu` title menu smoke path unchanged.

## Follow-Up

The next branch should add the Manager severity model so the Status summary can expose missing hooks and degraded features through first-class `ManagerSummary` counters instead of only smoke-summary calculation.
