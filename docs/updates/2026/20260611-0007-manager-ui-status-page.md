# 20260611-0007 Manager UI Status Page

## Summary

Wired the title-page DTMAPI Settings `Status` tab to the internal Manager view model so the first real Manager UI page now shows support-oriented summary counters and report path state, with a manual Refresh button.

## Source Request

User requested the Manager Status page real wiring plan from clean `Refactor` / `dec70c0`: keep public APIs unchanged, expose Core internals only to the DTMAPI Bootstrap assembly, render Manager summary data on the title Status page, and verify build/test plus title UI/runtime smokes.

## Changed Files

- `src/DTMAPI.Core/AssemblyInfo.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Details

- Added `InternalsVisibleTo("DTMAPI.BepInExBootstrap")` so the first-party Bootstrap renderer can read the internal Manager view model without adding a public mod-facing API.
- Added `UiRuntimeService.RefreshDtmManagerModel()` and kept page-open refresh behavior routed through it.
- Changed the title settings `Status` tab to show `ManagerSummary` overall status, mod counts, diagnostics counts, failed hook/feature counts, report export status, latest log path status, and latest report path status.
- Added a `Refresh` button that refreshes the Manager model only; it does not export logs or change ConfigMenu behavior.
- Preserved the existing title button default path to Config, and did not rewrite Mods, Errors, Hooks, Logs, Config, GameBridge hooks/features, public API contracts, or smoke result schema.
- Adjusted the smoke harness setting for `-AutoOpenTitleSettingsMenu` so third-save title UI smoke waits long enough to capture the menu screenshot before auto-loading the save slot.

## Validation

- Passed:
  - `tools/scripts/build.ps1 -Configuration Release`
  - `tools/scripts/test.ps1 -Configuration Release`
- Passed after a transient parallel build/test lock was avoided by rerunning sequentially:
  - `tools/scripts/test.ps1 -Configuration Release`
- Passed title UI smoke:
  - Command: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoOpenTitleSettingsMenu -SaveSlot 3 -TimeoutSeconds 240 -SkipBuild`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260611-091239`
- Passed HookProbe runtime smoke:
  - Command: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260611-091349`

## Evidence

- Unit test: `ManagerRuntimeProviderRefreshesSnapshotAfterReportExport`
  - Verifies opening the DTMAPI Manager refreshes the internal model.
  - Verifies explicit `RefreshDtmManagerModel()` updates summary counters after diagnostics changes.
  - Verifies a UI service without a provider leaves the model unavailable for fallback UI.
- Title UI smoke `GAME-SMOKE/20260611-091239`:
  - `RunStatus=Passed`
  - `TitleSettingsButton=Passed`
  - `TitleSettingsButtonScreenshot=Passed`
  - `TitleSettingsButtonScreenshotFile=Passed`
  - `TitleSettingsMenu=Passed`
  - `TitleSettingsMenuScreenshot=Passed`
  - `TitleSettingsMenuScreenshotFile=Passed`
  - `ProcessExited=Passed`
  - `NoFatalInstanceWindow=Passed`
- HookProbe smoke `GAME-SMOKE/20260611-091349`:
  - `RunStatus=Passed`
  - `HookProbe=Passed`
  - `SaveLoaded=Passed`
  - `ProcessExited=Passed`
  - `NoFatalInstanceWindow=Passed`

## Rollback Notes

Remove the Bootstrap internals visibility, revert the `RefreshDtmManagerModel()` method and Status tab rendering changes, and restore the previous title smoke autoload delay. Public API and GameBridge runtime behavior are unaffected by the rollback.

## Follow-Up

- Keep Export Report exception safety as a separate branch.
- Keep Manager hook/feature severity refinement as a separate branch.
- The fallback ImGui overlay and the remaining Manager pages still use the older runtime snapshot route and should be handled by later UI slices.
