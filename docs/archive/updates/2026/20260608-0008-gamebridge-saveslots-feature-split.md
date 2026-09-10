# 20260608-0008 GameBridge SaveSlots Feature Split

## Metadata

- Update ID: 20260608-0008
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Moved the SaveSlots feature implementation from the large `DolocTownExperimentalBridgeApi.cs` file into `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/`.
- Kept runtime refresh call sites, public API shape, status strings, and log text unchanged.
- Moved only SaveSlots-owned API methods, runtime refresh/apply methods, state builders, option normalization, and native archive-slot manager helpers.
- Left shared helper methods and unrelated feature helpers in their existing files for this slice.
- Did not change MoreSaves behavior or the official save UI path.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/DolocTownExperimentalBridgeApi.SaveSlots.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0008-gamebridge-saveslots-feature-split.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `git diff --check -- src/DTMAPI.GameBridge.DolocTown`
  - Only existing CRLF warning output was reported.
- Passed: `tools/scripts/build.ps1`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1`
  - Release build/test pass completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -SkipBuild -TimeoutSeconds 240`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-071507`
- Startup evidence: `StartupLog=true` in `result.json`.
- HookProbe evidence: `HookProbe=true`, `SaveLoaded=true`, and log line `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
- SaveSlots registration evidence: logs show initial pending status while `DolocAPI.gameManager` was unavailable, then runtime refresh applied the requested count.
- Feature evidence: logs show `Save.MoreSlotsApi = configured-official-archive-count. Official save slot count set 6->12 through DolocAPI.gameManager.archiveFileCount; LocalSave and GameDataPanel keep owning archive files/UI. reason=runtime refresh native=6 target=12.`
- Official save UI evidence: logs show `MoreSaves official save UI evidence archiveFileCount=12, panelSlotCount=12, renderedSlots=12, path=DolocAPI.gameManager.archiveFileCount -> GameDataUiState.Show -> GameDataPanel.Render.`
- Smoke evidence: logs show `Smoke.MoreSavesOfficialSaveUi = verified. archiveFileCount=12, panelSlotCount=12, renderedSlots=12, path=DolocAPI.gameManager.archiveFileCount -> GameDataUiState.Show -> GameDataPanel.Render.`
- Exit evidence: `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`, `process-check.txt` says no `DolocTown.exe`, and `fatal-window-check.txt` says no fatal instance popup.

## Related Records

- Previous split slice: `docs/updates/2026/20260608-0007-gamebridge-camerazoom-feature-split.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-FEATURE-SAVESLOTS-001`
- Prior MoreSaves official save UI implementation: `docs/updates/2026/20260606-0004-029-readme-implementation.md`

## Rollback Notes

- Move `DolocTownExperimentalBridgeApi.SaveSlots.cs` contents back into `DolocTownExperimentalBridgeApi.cs`.
- No hook IDs, status text, log text, public API semantics, or official save UI behavior require rollback because this was a mechanical feature split.

## Follow-Up

- Continue moving one feature at a time into `Features/`.
- Keep SaveSlots/MoreSaves behavior changes separate from this mechanical split.
- Split Smoke and Diagnostics only after feature slices remain build- and smoke-clean.
