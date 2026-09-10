# 20260610-0019 SaveSlots Smoke Case File

## Status

Verified.

## Source Request

User requested the post-midterm follow-up route, including a dedicated SaveSlots smoke case split before the next feature splits.

## Summary

- Moved MoreSaves official save UI evidence recording into `Smoke/Cases/SaveSlotsSmokeCase.cs`.
- Kept the scheduler and official save UI load path in `SmokeHarness.cs` unchanged.
- Preserved `Smoke.MoreSavesOfficialSaveUi`, `Save.MoreSlotsApi`, result schema, log text, status meanings, and MoreSaves/official save UI behavior.
- Verified the mechanical move through the same third-save HookProbe/save UI smoke path.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/SaveSlotsSmokeCase.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Validation

- `git diff --check` passed.
- An initial parallel build/test attempt failed with a transient Windows file lock on `tests/DTMAPI.UnitTests/obj/Release/net8.0/DTMAPI.UnitTests.dll`; build/test were then rerun sequentially.
- Sequential `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- Sequential `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- DirectExe third-save HookProbe/save UI smoke passed:
  - Command: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `GAME-SMOKE/20260610-100337`
  - Result: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
  - Logs: `Feature.SaveSlots = ready`; `Save.MoreSlotsApi = configured-official-archive-count`; `Smoke.MoreSavesOfficialSaveUi = verified. archiveFileCount=12, panelSlotCount=12, renderedSlots=12`; `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
  - Report pointer: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-100413.zip`.
  - Exit check: `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup.

## Evidence Links

- Hook map: `docs/hook-map/README.md#hook-savemoreslotsapi`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folder: `docs/debug/evidence/GAME-SMOKE/20260610-100337`

## Rollback

- Move `RecordMoreSavesOfficialSaveUiEvidence(...)` back into `SmokeHarness.cs`.
- Re-run the third-save HookProbe/save UI smoke and confirm `Smoke.MoreSavesOfficialSaveUi=verified`.

## Follow-Up

- Broaden SaveSlots coverage beyond official save UI rendering to create/load/delete/copy/restart lifecycle behavior before any stability promotion.
