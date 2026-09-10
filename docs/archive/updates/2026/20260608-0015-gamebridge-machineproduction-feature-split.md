# 20260608-0015 GameBridge MachineProduction Feature Split

## Metadata

- Update ID: 20260608-0015
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Moved the MachineProduction feature implementation from the large `DolocTownExperimentalBridgeApi.cs` file into `src/DTMAPI.GameBridge.DolocTown/Features/MachineProduction/`.
- Kept hook installation, hook callback methods, hook IDs, status strings, and log text unchanged.
- Moved only MachineProduction-owned API registration/status methods, native Mine recipe/tech-tree helpers, runtime production loop, visual-scale helpers, smoke state helpers, and `MachineRuntimeEntry`.
- Left shared room/equipment enumeration helpers in the main partial because ChestLocatorEnhancer and advanced debug paths still use them.
- Left shared native backpack placement and shared random state in the main partial because OilCoalDrop and Machine output paths both still use them.
- Did not change Machine behavior, Mine content behavior, Oil behavior, EquipmentSlots behavior, hook logic, smoke behavior, public API shape, or CameraZoom behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/MachineProduction/DolocTownExperimentalBridgeApi.MachineProduction.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0015-gamebridge-machineproduction-feature-split.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `git diff --check -- src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs src/DTMAPI.GameBridge.DolocTown/Features/MachineProduction/DolocTownExperimentalBridgeApi.MachineProduction.cs`
  - Only existing CRLF warning output was reported.
- Passed: `tools/scripts/build.ps1`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1`
  - Release build/test pass completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseMineContentApis -SkipBuild -TimeoutSeconds 300 -AutoExitAfterSecondsOverride 240`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-085224`
- Startup evidence: `StartupLog=true` in `result.json`.
- Third-save evidence: `summary.txt` records `SaveSlot=3`, and logs show `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`.
- Mine official JSON evidence: logs show `Smoke exercise NewContentMineOfficialJson OK item=矿井 ... recipeInputs=metal_frameworkx10|engine_corex5|steel_ingotx20|crude_oilx10 ... machineApi=machine:dtmapi.mine/item:dtmapi_mine/equipment:dtmapi_mine/recipe:dtmapi_mine/group:equipment_workbench/visualScale:2/hybrid:True/defaultMode:electric/fuelCapacity:7200/fuelOnlyCost:120/electricFuelCost:20/cycleMinutes:120/powerCost:10`.
- Mine tech route evidence: logs show `Machine.MineTechTreeRoute = verified` with `unlockEntries=recipe-only`, `equipmentEntries=0`, `recipeEntries=1`, and `costValues=SCIENCE:1`.
- Mine recipe evidence: logs show `Machine.MineRecipeInputs = experimental. recipe=dtmapi_mine, inputs=metal_frameworkx10|engine_corex5|steel_ingotx20|crude_oilx10`.
- Mine visual evidence: logs show `Machine.VisualScale = verified. dtmapi_mine visualScale=2, rendererScale=2x2, applied=True` and `Machine.MineVisualContainment = verified. containment=True, contamination=False`.
- Mine production evidence: logs show `MachineProduction cycle OK owner=DTMAPI.MineMod machine=dtmapi.mine equipment=dtmapi_mine output=crude_oil count=1 mode=electric fuelCost=20 fuelRemaining=7180/7200 electricPowerCost=10`.
- Smoke hook evidence: logs show `Smoke exercise NewContentMineProduction OK mode=mine-only ... beforeCycles=0, afterCycles=1 ... status=configured-experimental-runtime-loop ... outputTarget=equipment-storage ... storage=1/16` and `Smoke.NewContentMineProduction = verified`.
- Exit evidence: `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`, `process-check.txt` says no `DolocTown.exe`, and `fatal-window-check.txt` says no fatal instance popup.

## Related Records

- Previous split slice: `docs/updates/2026/20260608-0014-gamebridge-oilcoaldrop-feature-split.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-FEATURE-MACHINEPRODUCTION-001`
- Existing Mine regression case: `docs/debug/regressions/smoke-matrix.md` row `MANUALQA-026-MINE-Y-CONSOLE`
- Existing new-content case: `docs/debug/regressions/smoke-matrix.md` row `MANUALQA-031-REGRESSION-NEWCONTENT`
- API matrix: `docs/api/public-api-matrix.md` row for MachineProduction

## Rollback Notes

- Move `DolocTownExperimentalBridgeApi.MachineProduction.cs` contents back into `DolocTownExperimentalBridgeApi.cs`.
- No hook IDs, callback paths, status text, log text, public API semantics, Machine behavior, Mine content behavior, Oil behavior, or EquipmentSlots behavior require rollback because this was a mechanical feature split.

## Follow-Up

- Continue moving one feature at a time into `Features/`.
- Keep Machine behavior fixes, Mine content changes, Oil changes, EquipmentSlots changes, and smoke harness changes separate from this mechanical split.
- Split Smoke and Diagnostics only after feature slices remain build- and smoke-clean.
