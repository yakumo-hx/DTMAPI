# 20260610-0018 SaveSlots Refresh Throttle

## Status

Verified.

## Source Request

User requested the post-midterm follow-up route and asked for the mid-term implementation, including throttling SaveSlots runtime refresh before splitting additional smoke/feature owners.

## Summary

- Throttled `SaveSlotsService` runtime refresh so configured SaveSlots no longer re-applies `DolocAPI.gameManager.archiveFileCount` every frame.
- Kept pending native manager retry responsive at 750 ms while using a 3 second heartbeat once configured.
- Made `SaveLoaded` force a refresh so load boundaries still reassert the official archive count.
- Cleared the transient empty-owner pending state once the native manager becomes available, avoiding an extra pending retry after the first successful runtime correction.
- Kept `ISaveSlotsApi`, `Save.MoreSlotsApi`, MoreSaves behavior, official save UI semantics, and smoke result schema unchanged.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/SaveSlots/SaveSlotsService.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- DirectExe third-save HookProbe/save UI smoke passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `GAME-SMOKE/20260610-095455`
  - Result: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
  - Logs: `Feature.SaveSlots = ready`; `Save.MoreSlotsApi = configured-official-archive-count`; one runtime correction `Official save slot count set 6->12 ... reason=runtime refresh native=6 target=12`; one load-boundary force refresh `Official save slot count set 12->12 ... reason=SaveLoaded native=12 target=12`; `Smoke.MoreSavesOfficialSaveUi = verified. archiveFileCount=12, panelSlotCount=12, renderedSlots=12`; `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
  - Report pointer: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-095531.zip`.
  - Exit check: `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup.

## Evidence Links

- Hook map: `docs/hook-map/README.md#hook-savemoreslotsapi`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folder: `docs/debug/evidence/GAME-SMOKE/20260610-095455`

## Rollback

- Revert `SaveSlotsService.RefreshSaveSlotExpansionForRuntime(bool force, string reason)` back to the old frame-by-frame refresh path.
- Remove the pending/configured refresh interval fields and the native manager pending flag.
- Re-run the third-save HookProbe/save UI smoke to confirm the rollback still verifies `Smoke.MoreSavesOfficialSaveUi`.

## Follow-Up

- Split the SaveSlots smoke body into `Smoke/Cases/SaveSlotsSmokeCase.cs` without changing result schema or log text.
- Broaden SaveSlots coverage beyond official save UI rendering to create/load/delete/copy/restart lifecycle behavior before any stability promotion.
