# 20260605-0003 0.2.6 Mine and Y-console focused fixes

## Summary

Bumped DTMAPI from 0.2.5 to 0.2.6 and implemented the focused MineMod plus Y-key console manual-QA fixes from `readme.md`. This update intentionally did not exercise the SecondMotor smoke path.

## Source Request

The 2026-06-05 readme goal scoped this round to MineMod scale/research issues and Y-console reload/localization/hover/search issues. The user later clarified after context compaction: continue the readme goal and do not execute motor work.

## Changed Files

- `Directory.Build.props`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.BepInExBootstrap/DtmUiText.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedImGuiOverlay.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `testmods/MineMod/*`
- `testmods/DebugConsoleMod/*`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `docs/debug/issues/ISSUE-007-20260605-mine-yconsole-026.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/INDEX.md`

## Implementation Notes

- Version: controlled sources moved to `0.2.6`.
- Mine: official recipe research cost is now 1 point; the native tech-tree payload is kept recipe-only (`equipmentEntries=0`, `recipeEntries=1`) to prevent duplicate Mine unlock rows. Mine renderer scale is instance-scoped to `dtmapi_mine`, pooled equipment renderers reset to `1x1x1` on reuse, and placement preview hooks apply Mine-only preview scale through `EquipmentBuilder.CreateIndicator/TurnIndicator`.
- Y console: removed the player-visible reload button and disabled `IInstantSaveDebugApi.Save(... reloadAfterSave: true)` with an explicit `reload-disabled` result. `存这里` remains save-only through native `DolocAPI.SaveGame`.
- Y console UI: weather buttons are compact and one-row; language choices render as user-facing names; weather, teleport, and common category labels are localized/formatted; item hover displays the tooltip without mutating search/source filters for screenshot evidence.
- Smoke harness: added `-AutoExerciseMineContentApis` so Mine official JSON/tech/production/scale evidence can run without the broader Oil/EquipmentSlots new-content smoke and without `-AutoExerciseVehicle`.

## Validation

- Release build/unit: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\scripts\build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- Focused third-save smoke: `docs/debug/evidence/GAME-SMOKE/20260605-181224`.
  - Command used `-IncludeHookProbe -AutoExerciseInstantSave -AutoExerciseDebugConsole -AutoExerciseDebugConsoleMouseGive -AutoExerciseDebugInventory -AutoExerciseDebugWeather -AutoExerciseDebugTeleport -AutoExerciseDebugTime -AutoExerciseDebugMovement -AutoExerciseMineContentApis -SkipBuild`.
  - It did not pass `-AutoExerciseVehicle`; `summary.txt` records `AutoExerciseVehicle=False`, `AutoExerciseNewContentApis=False`, and `AutoExerciseMineContentApis=True`.
  - `result.json`: `StartupLog=true`, `GameLaunched=true`, `HookProbe=true`, `SaveLoaded=true`, `InstantSave=true`, `DebugConsoleOpenY1=true`, `DebugConsoleCloseEscape=true`, `DebugConsoleCloseY=true`, `DebugConsoleMouseGive=true`, `DebugConsoleTenYShortTaps=true`, `DebugConsoleHoldYNoFlicker=true`, `DebugInventory=true`, `DebugWeather=true`, `DebugTeleportCsv=true`, `DebugTeleport=true`, `DebugTime=true`, `DebugMovement=true`, `NewContentMineApis=true`, `NewContentMineOfficialJson=true`, `NewContentMineOfficialTechTreeUi=true`, `NewContentMineOfficialTechTreeUiScreenshotFile=true`, `NewContentMineProduction=true`, `ProcessExited=true`, `ForcedClose=false`, and `NoFatalInstanceWindow=true`.
  - Mine evidence: logs record `techPoint=1`, `unlockEntries=recipe-only`, `equipmentEntries=0`, `recipeEntries=1`, `costValues=SCIENCE:1`, official TechTree UI screenshot evidence, `mode=mine-only`, `oilItemMetadata={skipped-mine-only}`, `equipmentSlots={skipped-mine-only}`, `rendererScale=2x2`, `scaleContainment={containment=True, contamination=False, total=7, nonMineScaled=0}`, and production into Mine-owned storage.
  - Y-console evidence: first open logs `searchText=<empty> category=<empty> sourceFilter=__base`; screenshot summary records `HoverDetailTooltip=True`, `SearchText=`; logs show Esc close, Y close, ten short taps, hold-no-flicker, weather change, teleport, and `reloadDisabled=True` for save-only instant save.
  - Exit evidence: `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup.

## Evidence Links

- Focused pass: `docs/debug/evidence/GAME-SMOKE/20260605-181224`
- Y-console screenshot: `docs/debug/evidence/GAME-SMOKE/20260605-181224/DTMAPI-evidence/DEBUG-CONSOLE-UI/20260605-181315/debug-console.png`
- Mine official tech-tree screenshot: `docs/debug/evidence/GAME-SMOKE/20260605-181224/DTMAPI-evidence/NEWCONTENT-025/20260605-181312/mine-official-tech-tree-dtmapi-mine.png`
- Mine placement screenshot: `docs/debug/evidence/GAME-SMOKE/20260605-181224/DTMAPI-evidence/NEWCONTENT-025/20260605-181312/mine-placed-dtmapi-mine.png`

## Related Records

- Debug issue: `docs/debug/issues/ISSUE-007-20260605-mine-yconsole-026.md`
- Smoke matrix: `MANUALQA-026-MINE-Y-CONSOLE`
- Hook map: `UI.DebugConsoleHost`, `Debug.InstantSaveAndTeleportCsv`, `Debug.InventoryWeatherTeleportApis`, `Machine.ProductionRuntimeLoop`, `Machine.MineVisualContainment`
- API matrix: `IDebugConsoleApi`, `IInstantSaveDebugApi`, `IWeatherDebugApi`, `ITeleportDebugApi`, `IMachineProductionApi`

## Rollback Notes

Reverting this update must restore 0.2.5 version sources and remove the Mine-only smoke flag. That would re-open the player-reported Mine duplicate/cost/scale contamination and Y-console reload/search/localization issues. Do not revert only the docs.

## Follow-Up

No known blocker remains for this focused round. Optional manual visual QA can still inspect the same screens directly, but the current automated evidence now covers the official tech-tree UI screenshot, recipe-only payload/cost, removed reload button, compact weather row, hover tooltip, empty first-open search, and clean exit.
