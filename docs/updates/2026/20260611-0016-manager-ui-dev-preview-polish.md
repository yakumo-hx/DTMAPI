# 20260611-0016 Manager UI Developer Preview Polish

Date: 2026-06-11

Status: verified

Area: core/manager-ui/release

## Summary

Prepared the title-page Manager MVP for `0.5.0-alpha` Developer Preview support by adding Status Copy Summary fallback behavior, first-N row hints on compact pages, clearer Logs failure text, support-loop documentation, and a release checklist. This remains an internal product UI slice; it does not change public mod APIs, ConfigMenu behavior, GameBridge gameplay, or hook/status IDs.

## Source Request

User asked to do the medium/long target after the latest Manager MVP review. The active goal is `DTMAPI 0.5.0-alpha Developer Preview Preparation`, starting from the current `Refactor` Manager UI MVP Phase 1 baseline.

## Changed Files

- `src/DTMAPI.Core/Manager/ManagerPageRowFormatter.cs`
- `src/DTMAPI.Core/Manager/DtmManagerRuntimeModelProvider.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/workflows/player-feedback-community-loop.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/releases/0.5.0-alpha-developer-preview-checklist.md`
- `docs/updates/INDEX.md`

## Details

- Added a shared internal support summary formatter containing overall status, mod counts, diagnostics counts, failed/missing hooks, failed/degraded features, refresh state, report/export state, latest log state, and latest report path state.
- Added internal Status Copy Summary behavior. It tries the local clipboard through first-party UI code; if clipboard access is unavailable, it records `copy-unavailable`, writes the summary to the runtime log, and keeps the UI callback non-throwing.
- Added `showing first N of total` row hints for Mods, Errors/Warnings, Hooks, and Features.
- Kept the existing compact first-N page bodies, without row selection, scrolling, or detail panels.
- Clarified Logs `export-failed` display while preserving the existing report export path and snapshot/path-match state.
- Added smoke coverage for `ManagerStatusSummaryCopy`, using a forced copy-unavailable fallback so the support text is testable without relying on OS clipboard availability.
- Added Developer Preview checklist and clarified that Manager UI is an internal product UI over existing Diagnostic/Experimental data, not a public API promotion.

## Validation

- `git diff --check`: passed.
- `tools/scripts/build.ps1 -Configuration Release`: passed.
- `tools/scripts/test.ps1 -Configuration Release`: passed (`DTMAPI.UnitTests: OK`).
- DirectExe Manager MVP smoke with `-AutoOpenTitleSettingsManagerMvp`: `GAME-SMOKE/20260611-123852` passed.
- DirectExe HookProbe runtime smoke with `-IncludeHookProbe`: `GAME-SMOKE/20260611-124008` passed.

## Evidence

- `GAME-SMOKE/20260611-123852/result.json` records `RunStatus=Passed`, `ManagerStatusPage=Passed`, `ManagerStatusSummaryText=Passed`, `ManagerStatusSummaryCopy=Passed`, `ManagerModsPage=Passed`, `ManagerErrorsPage=Passed`, `ManagerHooksPage=Passed`, `ManagerFeaturesPage=Passed`, `ManagerLogsPage=Passed`, `ManagerLogsExportButton=Passed`, `ManagerLogsExportStateText=Passed`, `ManagerLogsPageScreenshot=Passed`, `ManagerLogsPageScreenshotFile=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`.
- `GAME-SMOKE/20260611-123852/DTMAPI-latest.log` records `Manager Status summary copy OK status=copy-unavailable`, `Manager Logs export button OK status=exported pathMatch=matched`, and `Manager Logs export state OK export=exported; pathMatch=matched`.
- `GAME-SMOKE/20260611-123852` includes title Settings Status and Logs screenshots under the copied UI evidence folder.
- `GAME-SMOKE/20260611-124008/result.json` records `RunStatus=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, and `NoFatalInstanceWindow=Passed`; the DTMAPI log records `HookProbe SaveLoaded OK slot=2 isNewGame=False`.

## Rollback Notes

Revert this update if Status Copy Summary introduces title UI callback instability or if Manager MVP smoke becomes timing-dependent. The rollback should remove only the internal UI polish, smoke field, and docs; public APIs and gameplay feature behavior were not changed.

## Follow-Up

- Add selected-row copy and row detail panels after Developer Preview feedback.
- Revisit visual density and truncation after real player reports with long paths, long mod names, and long diagnostics messages.
- Keep CameraView manual QA and all Experimental gameplay API promotions separate from Manager UI evidence.
