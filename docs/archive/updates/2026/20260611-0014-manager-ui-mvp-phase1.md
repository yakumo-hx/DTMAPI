# 20260611-0014 Manager UI MVP Phase 1

Date: 2026-06-11

Status: verified

Area: core/manager-ui/smoke

## Summary

Wired the title-page DTMAPI Settings Manager MVP pages to the internal Manager view model. Status and Logs keep their existing real consumers, Mods/Errors/Hooks now read `CurrentManagerModel`, and a new Features tab reads structured feature rows. Added a focused Manager MVP smoke path that opens Status, Mods, Errors, Hooks, Features, and Logs, clicks Export logs on Logs, verifies exported/path-match state, and captures Status plus Logs screenshots.

## Source Request

User requested the medium/long target: implement `DTMAPI Manager UI MVP Phase 1` from clean `Refactor` / `5ee02f1`, on branch `codex/manager-ui-mvp-phase1`, without changing GameBridge gameplay, public mod APIs, or ConfigMenu default behavior.

## Changed Files

- `src/DTMAPI.Core/Manager/ManagerPageRowFormatter.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/workflows/player-feedback-community-loop.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Details

- Added internal Manager row formatting helpers for Mods, diagnostics, hooks, features, unavailable model text, and Logs export state.
- Added `DtmOverlayPage.Features` and a title Settings `Features` tab.
- Reworked `RenderMods`, `RenderErrors`, and `RenderHooks` to prefer `runtime.UI.CurrentManagerModel` and show `Manager model unavailable` instead of falling back to raw runtime snapshot/log parsing.
- Added `RenderFeatures` with `FeatureId / Status / LastOperation / FailureCount / LastError-or-Details` style rows using the existing sorted view model.
- Kept `OpenFromTitleButton()` opening Config, and did not change Config save/reset/cancel/keybind behavior.
- Added `run-game-smoke.ps1 -AutoOpenTitleSettingsManagerMvp` with result fields `ManagerModsPage`, `ManagerErrorsPage`, `ManagerHooksPage`, `ManagerFeaturesPage`, `ManagerLogsPage`, `ManagerLogsExportButton`, `ManagerLogsExportStateText`, `ManagerLogsPageScreenshot`, and `ManagerLogsPageScreenshotFile`.
- The Manager MVP smoke uses the existing Status result fields and verifies Logs export as `exported` with `pathMatch=matched`.

## Validation

- `tools/scripts/build.ps1 -Configuration Release`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed (`DTMAPI.UnitTests: OK`).
- PowerShell AST parse of `tools/scripts/run-game-smoke.ps1`: passed.
- Manager MVP DirectExe title smoke: `GAME-SMOKE/20260611-112148` passed.
- HookProbe DirectExe third-save runtime regression: `GAME-SMOKE/20260611-112402` passed.

## Evidence

- `GAME-SMOKE/20260611-112148/result.json` records `RunStatus=Passed`, `ManagerStatusPage=Passed`, `ManagerStatusSummaryText=Passed`, `ManagerStatusPageScreenshot=Passed`, `ManagerStatusPageScreenshotFile=Passed`, `ManagerModsPage=Passed`, `ManagerErrorsPage=Passed`, `ManagerHooksPage=Passed`, `ManagerFeaturesPage=Passed`, `ManagerLogsPage=Passed`, `ManagerLogsExportButton=Passed`, `ManagerLogsExportStateText=Passed`, `ManagerLogsPageScreenshot=Passed`, `ManagerLogsPageScreenshotFile=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- `GAME-SMOKE/20260611-112148` logs `Manager Logs export button OK status=exported pathMatch=matched path=D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260611-112232.zip`.
- `GAME-SMOKE/20260611-112402/result.json` records `RunStatus=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.

## Rollback Notes

Revert this update if title Settings page switching regresses or if `-AutoOpenTitleSettingsManagerMvp` cannot reliably capture Status/Logs screenshots. Runtime gameplay feature behavior and public API contracts were not changed by this branch.

## Follow-Up

- Final `Refactor` package refresh should use the post-merge Manager MVP smoke evidence.
- Copy Summary / Copy selected row and full visual redesign remain future Manager UI slices.
