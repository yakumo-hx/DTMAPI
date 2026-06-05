# 20260603-0004 Y Console Source Columns And Teleport Names

## Source Request

Continue the DTMAPI 0.2.3 manual-QA productization goal from `readme.md`, using the existing 0.2.3 partial worktree as authoritative. This pass focuses on task F: Y-key console layout, item source/category browsing, hover details, and detailed teleport destination labels.

## Summary

- Added experimental inventory source filtering/group DTOs: `InventoryDebugQuery.SourceId`, `InventoryDebugPage.Sources`, and `InventoryDebugSourceGroup`.
- Updated `IInventoryDebugApi.GetItems` filtering order so search and source groups feed a left source column (`本体`, `模组`, specific source names), then a subcategory column, then paged items.
- Enlarged the reflected Y-key console panel and replaced the old top category strip with source/category columns plus a nearby item detail tooltip.
- Kept item give on native `DolocAPI.QueryItemProto` / `CanPlaceItem` / `TryPlaceInBackpack`; left click uses Unity `Button`, right click uses `PointerClick`, but this pass does not yet have real mouse click smoke.
- Improved teleport destination display names by preserving native Chinese mark-point names and room suffixes instead of collapsing first-page entries to generic `农场` / `城镇`.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.BepInExBootstrap/DtmUiText.cs`
- `readme.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\scripts\build.ps1 -Configuration Release`
- Result: all projects and `DTMAPI.UnitTests` built with `0` warnings and `0` errors.
- Passed: `tools/scripts/run-game-smoke.ps1 -UseSteam -SaveSlot 3 -SkipBuild -AutoExerciseDebugConsole -AutoExerciseDebugInventory -AutoExerciseDebugTeleport -AutoExitAfterSecondsOverride 95 -TimeoutSeconds 260`
- Result: third-save Steam smoke passed with Y open/Esc close/Y reopen/Y close, ten short Y taps, hold-no-flicker, inventory give API, teleport, clean exit, and no fatal popup.

## Evidence Links

- Final targeted smoke: `docs/debug/evidence/GAME-SMOKE/20260603-032406`.
- UI screenshot: `docs/debug/evidence/GAME-SMOKE/20260603-032406/DTMAPI-evidence/DEBUG-CONSOLE-UI/20260603-032451/debug-console.png`.
- UI screenshot summary records `SourceColumns=True`, `HoverDetailTooltip=True`, `HoverEvidenceItem=dtmapi_second_motor_key`, and `SourceFilter=Local.DTMAPI_SecondMotor`.
- Inventory log: `DTMAPI-latest.log` records native backpack gives for `wood` `0->1` and `dtmapi_second_motor_key` `0->1`.
- Teleport log: `DTMAPI-latest.log` records `destination=上游丘陵1-下端`, before room `farm_大型集装箱...`, after room `city_郊区-上游丘陵1`, `changedRoom=True`.
- Exit evidence: `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup.

## Known Facts And Rejected Hypotheses

- The earlier failed/partial smoke `GAME-SMOKE/20260603-030142` remains useful only for AutoFishing/Inventory API evidence; it did not prove Y-console UI because external Y injection failed.
- `GAME-SMOKE/20260603-032406` proves source/category layout, hover-detail screenshot, Y key reliability, inventory API give, and native teleport. It does not prove a physical mouse click on an item cell called the UI give path.
- The source/category filter is intentionally experimental and based on stable DTOs rather than exposing raw `TbItem` or `StationInfo` game types.
- Detailed teleport names are still whitelist-based and still call `DolocAPI.DoTransport`; no arbitrary coordinates are exposed.

## Rollback

- Revert this record and the code/doc files listed above.
- If rolling back only the UI layout, keep the 0.2.3 version bump from `20260603-0003` intact unless the whole manual-QA pass is backed out.

## Follow-up

- Add smoke harness or manual evidence for real mouse left-click give-1 and right-click give-10 through the Y-console item cell.
- Continue remaining 0.2.3 blockers: Animal 0.2.3 visual recheck, AutoFishing movement/minigame proof, ActionSpeed strong auto-fill frequency proof, and second-motor dual-key/dual-instance 0.2.3 manual proof.
