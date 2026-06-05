# 20260603-0005 Y Console Mouse Give Smoke

## Source Request

Continue the DTMAPI 0.2.3 manual-QA productization goal from `readme.md`, specifically task F's remaining requirement: prove that real mouse left-click and right-click on the Y-console item cell reach the native inventory give path.

## Summary

- Added an opt-in smoke flag, `-AutoExerciseDebugConsoleMouseGive`, that opens the Y console in the third save and sends OS-level mouse clicks to the first visible item cell.
- Extended the smoke harness Win32 input helper with client-coordinate mouse targeting and `SendInput`-based mouse-button injection.
- Hardened reflected Unity input so `Mouse0`-`Mouse4` are also visible through the Win32 fallback path.
- Added a right-click give fallback in the Y-console host: a recent hovered/clicked item can receive `Mouse1` give-10 even when Unity's reflected `EventTrigger` does not deliver the right-button click event.
- Verified left-click give-1 and right-click give-10 against `dtmapi_second_motor_key` through `DTMAPI.DebugConsoleMod -> IInventoryDebugApi.GiveItem ->` native backpack placement.

## Changed Files

- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedUnityInput.cs`
- `tools/scripts/run-game-smoke.ps1`
- `readme.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\scripts\build.ps1 -Configuration Release`
- Result: all projects and `DTMAPI.UnitTests` built with `0` warnings and `0` errors.
- Passed: `tools/scripts/run-game-smoke.ps1 -UseSteam -SaveSlot 3 -SkipBuild -AutoExerciseDebugConsole -AutoExerciseDebugConsoleMouseGive -AutoExitAfterSecondsOverride 95 -TimeoutSeconds 260`
- Result: third-save Steam smoke passed with `DebugConsoleMouseGive=true`, Y open/Esc close/Y reopen/Y close, ten short Y taps, hold-no-flicker, clean exit, and no fatal popup.

## Evidence Links

- Final targeted smoke: `docs/debug/evidence/GAME-SMOKE/20260603-035653`.
- Result: `docs/debug/evidence/GAME-SMOKE/20260603-035653/result.json` has `DebugConsoleMouseGive=true`, `ProcessExited=true`, and `NoFatalInstanceWindow=true`.
- Summary: `docs/debug/evidence/GAME-SMOKE/20260603-035653/summary.txt` records `DebugConsoleMouseGiveLeft=... ok=True` and `DebugConsoleMouseGiveRight=... ok=True`.
- UI screenshot: `docs/debug/evidence/GAME-SMOKE/20260603-035653/DTMAPI-evidence/DEBUG-CONSOLE-UI/20260603-035737/debug-console.png`.
- Inventory logs: `DTMAPI-latest.log` records `owner=DTMAPI.DebugConsoleMod item=dtmapi_second_motor_key requested=1 placed=1 before=0 after=1 success=True` and `requested=10 placed=10 before=1 after=11 success=True`.
- Exit evidence: `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup.

## Known Facts And Rejected Hypotheses

- Failed retained smokes `GAME-SMOKE/20260603-034241`, `GAME-SMOKE/20260603-034621`, `GAME-SMOKE/20260603-035110`, and `GAME-SMOKE/20260603-035403` proved the item-cell coordinate was correct because left-click give-1 succeeded, but right-button delivery did not reach the UI/API path.
- The UI event callback alone was not sufficient for automated right-click evidence in this harness, so the host now supports a `Mouse1` fallback against the recently hovered/clicked item, guarded against duplicate right-click grants.
- The smoke remains a real OS-level mouse-input path; the final give lines are emitted by `DTMAPI.DebugConsoleMod` and native backpack placement, not by the older direct `DTMAPI.Smoke` API exercise.
- The change does not alter the public inventory API surface; it hardens the bootstrap UI/input path and smoke harness evidence.

## Rollback

- Revert this record and the changed files listed above.
- If rolling back only the smoke harness, remove `-AutoExerciseDebugConsoleMouseGive` and keep the UI right-click fallback only if manual testing still proves right-click give-10.

## Follow-up

- Continue remaining 0.2.3 blockers: Animal 0.2.3 visual recheck, AutoFishing movement/minigame proof, ActionSpeed strong auto-fill frequency proof, and second-motor dual-key/dual-instance 0.2.3 manual proof.
