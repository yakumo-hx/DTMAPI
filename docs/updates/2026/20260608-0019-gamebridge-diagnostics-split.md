# 20260608-0019 GameBridge Diagnostics Split

## Metadata

- Update ID: 20260608-0019
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Moved the remaining diagnostic/debug API implementation from the large `DolocTownExperimentalBridgeApi.cs` file into `src/DTMAPI.GameBridge.DolocTown/Diagnostics/`.
- Kept hook installation, hook callback paths, hook IDs, status strings, log text, smoke settings, public API shape, and feature behavior unchanged.
- Moved `PublishHookStatuses`, advanced creative hook-state methods, `IInventoryDebugApi`, `IMailDeliveryApi`, `IWeatherDebugApi`, `ITeleportDebugApi`, `IInstantSaveDebugApi`, `ITimeDebugApi`, `IMovementDebugApi`, `IAdvancedDebugApi`, and their diagnostics-only helper methods.
- Left shared native placement, reflection, Unity component/vector, screenshot, runtime automation, and feature helper methods in the main/shared partial.
- Did not add APIs, change hook logic, change smoke flags, or alter CameraZoom behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Diagnostics/DolocTownExperimentalBridgeApi.Diagnostics.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0019-gamebridge-diagnostics-split.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `tools/scripts/test.ps1`
  - Release build/test pass completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseInstantSave -AutoExerciseDebugInventory -AutoExerciseDebugWeather -AutoExerciseDebugTeleport -AutoExerciseDebugTime -AutoExerciseDebugMovement -AutoExerciseAdvancedDebug -SkipBuild -TimeoutSeconds 420 -AutoExitAfterSecondsOverride 360`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-093428`
- Startup evidence: `DTMAPI-latest.log` starts with `DTMAPI runtime starting.`, and `result.json` records `StartupLog=true`.
- Third-save evidence: `summary.txt` records `SaveSlot=3`; logs show `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`.
- Debug API baseline evidence: logs show `Debug.InventoryApi`, `Debug.WeatherApi`, `Debug.TeleportApi`, `Debug.TimeApi`, `Debug.MovementApi`, and `Debug.CreativeModeHooks` statuses published with their unchanged text.
- Instant save evidence: logs show `Smoke.InstantSave = verified` with `slot/index=2`, `sameRoom=True`, `distance=0`, and `reloadDisabled=True`.
- Inventory evidence: logs show `Smoke.DebugInventory = verified` after native backpack placement for `wood` and `dtmapi_mine`.
- Weather evidence: logs show `Smoke.DebugWeather = verified` after `ArchiveDataHandle.SetWeather/PatchWeather`.
- Teleport evidence: logs show `Smoke.DebugTeleportCsv = verified` with `rows=80`, then `Smoke.DebugTeleport = verified` with destination `上游丘陵1-下端` and `changedRoom=True`.
- Time evidence: logs show `Smoke.DebugTime = verified` across three native weather-period transitions.
- Movement evidence: logs show `Smoke.DebugMovement = verified` after cycling 1x/2x/3x/4x and restoring to 1x.
- Advanced debug evidence: logs show `Smoke.AdvancedDebug = verified`, including native day advance, 4x/reset time scale, money and tech point grants, creative mode hook verification, creative generator give, monster spawn, resource spawn, tech-tree unlock, and crop maturity probes.
- Exit evidence: `result.json` records `ProcessExited=true`, `ForcedClose=false`, and `NoFatalInstanceWindow=true`; `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup.

## Related Records

- Previous split slice: `docs/updates/2026/20260608-0018-gamebridge-smoke-split.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-DIAGNOSTICS-001`
- Existing debug/API baselines: `docs/debug/regressions/smoke-matrix.md` rows `DEBUGITEMS-001`, `DEBUGWEATHER-001`, `DEBUGTELEPORT-001`, `DEBUGTIME-001`, `DEBUGMOVE-001`, and `YCONSOLE-030-ADVANCED`

## Rollback Notes

- Move `Diagnostics/DolocTownExperimentalBridgeApi.Diagnostics.cs` contents back into `DolocTownExperimentalBridgeApi.cs`.
- No hook IDs, callback paths, status text, log text, smoke settings schema, public API semantics, feature behavior, or CameraZoom behavior require rollback because this was a mechanical diagnostics split.

## Follow-Up

- Keep future Debug API behavior changes separate from this mechanical split.
- If the next goal touches individual debug APIs, use the existing debug smoke rows and dedicated review notes rather than treating this split as behavioral evidence.
