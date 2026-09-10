# 20260608-0025 GameBridge Smoke Case Split

## Metadata

- Update ID: 20260608-0025
- Date: 2026-06-08
- Status: verified
- Source: Active goal: mechanically split `DolocTownGameBridge.Smoke.cs` case groups without behavior or log-text changes
- Owner: Codex

## Summary

- Split the large smoke harness partial into case-focused partial files under `src/DTMAPI.GameBridge.DolocTown/Smoke/`.
- Kept the smoke scheduler and `Update()` dispatcher in `SmokeHarness.cs`.
- Moved Camera, AutoFishing, DebugConsole, Vehicle, Content, and CustomEntity smoke case implementations into dedicated files.
- Removed the intermediate `Smoke/DolocTownGameBridge.Smoke.cs` file from the current refactor layout.
- Did not intentionally change smoke command flags, settings schema, scheduling order, case behavior, status keys, or key log text.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/CameraSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/AutoFishingSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/DebugConsoleSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/VehicleSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/ContentSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/CustomEntitySmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/DolocTownGameBridge.Smoke.cs` removed from this layout
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0025-gamebridge-smoke-case-split.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `git diff --check -- src/DTMAPI.GameBridge.DolocTown/Smoke`
- Passed: `tools/scripts/build.ps1`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: DirectExe third-save game smoke with `-SkipBuild -IncludeHookProbe -AutoExerciseZoom -AutoExerciseCustomEntityApis -TimeoutSeconds 300 -AutoExitAfterSecondsOverride 240`.
- Passed: DirectExe third-save game smoke with `-SkipBuild -AutoExerciseAutoFishingPhase -AutoExerciseAutoFishingMiniGameComplete -TimeoutSeconds 360 -AutoExitAfterSecondsOverride 270`.
- Passed: DirectExe third-save game smoke with `-SkipBuild -AutoExerciseDebugConsole -AutoExerciseDebugInventory -AutoExerciseDebugWeather -AutoExerciseDebugTeleport -AutoExerciseDebugTime -AutoExerciseDebugMovement -AutoExerciseAdvancedDebug -TimeoutSeconds 360 -AutoExitAfterSecondsOverride 300`.
- Passed: DirectExe third-save game smoke with `-SkipBuild -AutoExerciseVehicle -AutoExerciseNewContentApis -TimeoutSeconds 360 -AutoExitAfterSecondsOverride 300`.

## Evidence

- Structure check: `SmokeHarness.cs` contains `public void Update()`.
- Structure check: `CameraSmoke.cs` contains `TryExerciseZoomForSmoke`.
- Structure check: `AutoFishingSmoke.cs` contains `TryExerciseAutoFishingPhaseForSmoke`.
- Structure check: `DebugConsoleSmoke.cs` contains `TryExerciseDebugConsoleHotkeyForSmoke`.
- Structure check: `VehicleSmoke.cs` contains `TryExerciseVehicleForSmoke`.
- Structure check: `ContentSmoke.cs` contains `TryExerciseNewContentApisForSmoke`.
- Structure check: `CustomEntitySmoke.cs` contains `TryExerciseCustomEntityApisForSmoke`.
- Log-text spot check retained key strings including `Smoke exercise AutoFishingPhase OK`, `Smoke exercise DebugConsoleHotkey OK`, and `Smoke exercise CustomEntityApis OK`.
- Game smoke `docs/debug/evidence/GAME-SMOKE/20260608-212207` records `RunStatus=Passed`, `StartupLog=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `Zoom=Passed`, `CustomEntityApis=Passed`, `ProcessExited=Passed`, `ForcedClose=Passed`, and `NoFatalInstanceWindow=Passed`.
- Game smoke `docs/debug/evidence/GAME-SMOKE/20260608-212324` records `RunStatus=Passed`, `StartupLog=Passed`, `SaveLoaded=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, `ProcessExited=Passed`, `ForcedClose=Passed`, and `NoFatalInstanceWindow=Passed`.
- Game smoke `docs/debug/evidence/GAME-SMOKE/20260608-212425` records `RunStatus=Passed`, `StartupLog=Passed`, `SaveLoaded=Passed`, `DebugConsoleOpenY1=Passed`, `DebugConsoleCloseEscape=Passed`, `DebugConsoleOpenY2=Passed`, `DebugConsoleCloseY=Passed`, `DebugConsoleTenYShortTaps=Passed`, `DebugConsoleHoldYNoFlicker=Passed`, `DebugInventory=Passed`, `DebugWeather=Passed`, `DebugTeleportCsv=Passed`, `DebugTeleport=Passed`, `DebugTime=Passed`, `DebugMovement=Passed`, `AdvancedDebug=Passed`, `ProcessExited=Passed`, `ForcedClose=Passed`, and `NoFatalInstanceWindow=Passed`.
- Game smoke `docs/debug/evidence/GAME-SMOKE/20260608-212617` records `RunStatus=Passed`, `StartupLog=Passed`, `SaveLoaded=Passed`, `VehicleSecondMotor=Passed`, `NewContentApis=Passed`, `NewContentOilItemMetadata=Passed`, `NewContentOilCoalDrop=Passed`, `NewContentMineOfficialJson=Passed`, `NewContentEquipmentSlots=Passed`, `NewContentMineProduction=Passed`, `ProcessExited=Passed`, `ForcedClose=Passed`, and `NoFatalInstanceWindow=Passed`.
- Cleanup check: no `DolocTown.exe` process remained after validation.
- Cleanup check: timed-out combined attempt `docs/debug/evidence/GAME-SMOKE/20260608-210554` left smoke settings enabled; the backed-up AutoFishing config and `mod_infos.json` were restored and `tools/scripts/disable-smoke-settings.ps1` reset `smoke-settings.json` to `Enabled=false` before the passing focused runs.

## Related Records

- Previous Smoke directory split: `docs/updates/2026/20260608-0018-gamebridge-smoke-split.md`
- Diagnostics split: `docs/updates/2026/20260608-0019-gamebridge-diagnostics-split.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-SMOKE-CASE-001`

## Rollback Notes

- Recombine `CameraSmoke.cs`, `AutoFishingSmoke.cs`, `DebugConsoleSmoke.cs`, `VehicleSmoke.cs`, `ContentSmoke.cs`, and `CustomEntitySmoke.cs` into the former single smoke partial if the split complicates follow-up work.
- Keep `Update()` and scheduling in `SmokeHarness.cs`; rollback should not alter smoke flags, status keys, log text, or smoke settings schema.

## Follow-Up

- Keep future smoke behavior changes separate from mechanical file moves.
- If a later goal changes smoke result parsing or settings schema, record it as a separate smoke tooling update instead of folding it into this case-file split.
