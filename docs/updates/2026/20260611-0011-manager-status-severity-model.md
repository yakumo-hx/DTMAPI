# 20260611-0011 Manager Status Severity Model

Status: verified

## Summary

Refined the internal Manager severity model so Status page counters distinguish failed hooks from missing hooks and failed features from degraded features. The overall status now treats failed/error/blocked state as `failed`, while missing hooks, degraded features, warnings, disabled mods, or missing report/log state produce `warning`.

## Source Request

User requested the Manager next-round mid/long hardening plan from clean `Refactor` / `7d2d83d`, including a Manager severity model after the dedicated Status page smoke.

## Changed Files

- `src/DTMAPI.Core/Manager/DtmManagerViewModels.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Behavior

- `ManagerHookRow` now exposes `IsMissing` separately from `IsFailed`.
- `ManagerSummary` now exposes `MissingHookCount` and `DegradedFeatureCount`.
- `OverallStatus` remains `failed` for blocked mods, diagnostics errors, failed hooks, or failed features.
- `OverallStatus` becomes `warning` for warnings, disabled mods, missing hooks, degraded features, missing report path, missing log path, or unavailable report state.
- `ManagerModRow` severity classification now uses structured `StatusCode` or exact `Status`; free-text `Reason` no longer upgrades a loaded mod into warning/blocked severity.
- The title Status page displays hook failed/missing and feature failed/degraded counters on separate lines.

## Validation

- `tools/scripts/test.ps1 -Configuration Release`: passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- A parallel build attempt collided with the concurrent test process on `obj\Release\netstandard2.0\DTMAPI.Core.AssemblyInfoInputs.cache`; this was a transient Windows file lock from parallel validation and the checks were rerun sequentially.
- Sequential `tools/scripts/build.ps1 -Configuration Release`: passed with 0 warnings and 0 errors.
- Sequential `tools/scripts/test.ps1 -Configuration Release`: passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `git diff --check`: passed with expected CRLF warnings only.
- DirectExe Status page smoke: passed.

## Evidence

- DirectExe Status smoke `GAME-SMOKE/20260611-101818` records `RunStatus=Passed`, `TitleSettingsButton=Passed`, `TitleSettingsButtonScreenshot=Passed`, `TitleSettingsButtonScreenshotFile=Passed`, `TitleSettingsMenu=Passed`, `TitleSettingsMenuScreenshot=Passed`, `TitleSettingsMenuScreenshotFile=Passed`, `ManagerStatusPage=Passed`, `ManagerStatusPageScreenshot=Passed`, `ManagerStatusPageScreenshotFile=Passed`, `ManagerStatusSummaryText=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- Log evidence from `GAME-SMOKE/20260611-101818` includes `Manager Status summary text OK overall=ready; mods=loaded:16,blocked:0,disabled:0; diagnostics=errors:0,warnings:0; hooks=failed:0,missing:0; features=failed:0,degraded:0; report=ready; ...`.
- Unit coverage in `ManagerViewModelMapsDiagnosticsSnapshot` verifies failed and missing hooks are separate counters, degraded features are separate from failed features, missing hooks/degraded features produce warning overall status when no failed/error state exists, and free-text `Reason` containing warning/error/blocking words does not change structured loaded-mod severity.

## Related Records

- `docs/updates/2026/20260611-0010-manager-status-page-smoke.md`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`

## Rollback Notes

Revert the `ManagerSummary` counter additions, `ManagerHookRow.IsMissing`, exact structured mod severity checks, Status page text changes, and smoke summary field usage. The previous model counted missing hooks as failed hooks and did not expose degraded feature count in the summary.

## Follow-Up

The next branch should wire the Logs page to the Manager report export result so report/export mismatch and `export-failed` state are visible outside the Status page.
