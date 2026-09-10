# 20260610-0014 SaveSlots Feature Split

## Status

Verified.

## Source Request

User requested the midterm Refactor hardening route and selected `SaveSlotsFeature` as the next feature split after the ChestLocator smoke case split.

## Summary

- Split `ISaveSlotsApi` ownership out of `DolocTownExperimentalBridgeApi` into `SaveSlotsFeature` and `SaveSlotsService`.
- Kept the existing public API shape, DTOs, `Save.MoreSlotsApi` hook/status ID, MoreSaves behavior, official save UI semantics, and smoke result fields unchanged.
- Moved save-slot policy state and `DolocAPI.gameManager.archiveFileCount` runtime refresh into the feature service while keeping the native owner path `DolocAPI.gameManager.archiveFileCount -> GameDataUiState.Show -> GameDataPanel.Render`.
- Published the save-slot contract from the feature route and verified `Feature.SaveSlots = ready` in the real smoke logs.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Diagnostics/DolocTownExperimentalBridgeApi.Diagnostics.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/DolocTownExperimentalBridgeApi.SaveSlots.cs` (removed)
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- DirectExe third-save smoke passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `GAME-SMOKE/20260610-051735`
  - Result: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
  - Logs: `Feature.SaveSlots = ready`; `Save.MoreSlotsApi = configured-official-archive-count`; `Official save slot count set 6->12 through DolocAPI.gameManager.archiveFileCount`; `Smoke.MoreSavesOfficialSaveUi = verified. archiveFileCount=12, panelSlotCount=12, renderedSlots=12`.
  - Report pointer: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-051812.zip`.
  - Exit check: `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup.

## Evidence Links

- Hook map: `docs/hook-map/README.md#hook-savemoreslotsapi`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folder: `docs/debug/evidence/GAME-SMOKE/20260610-051735`

## Rollback

- Restore `ISaveSlotsApi` implementation and `saveSlotOptions`/`saveSlotStates` ownership to `DolocTownExperimentalBridgeApi`.
- Re-register `ISaveSlotsApi` directly on the experimental bridge.
- Re-add the experimental bridge `Save.MoreSlotsApi` contract publication and remove `SaveSlotsFeature` from the feature host list.

## Follow-Up

- Broaden SaveSlots evidence beyond title/save UI rendering to new-slot create/load/delete/copy/restart behavior before considering any stability promotion.
- Keep SaveSlots conflict policy explicit if ordinary third-party mods start registering different slot counts.
