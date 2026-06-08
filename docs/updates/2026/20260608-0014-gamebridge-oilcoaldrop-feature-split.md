# 20260608-0014 GameBridge OilCoalDrop Feature Split

## Metadata

- Update ID: 20260608-0014
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Moved the Oil coal-drop feature implementation from the large `DolocTownExperimentalBridgeApi.cs` file into `src/DTMAPI.GameBridge.DolocTown/Features/OilCoalDrop/`.
- Kept hook installation, hook callback methods, hook IDs, status strings, and log text unchanged.
- Moved only Oil coal-drop owned ToolCollider pre-hit capture, post-hit drop handling, coal-resource roll helper, oil resource-hit key helper, coal-name classifier, and `PendingOilResourceHit`.
- Left `TryPlaceNativeItemInBackpack` in the shared main partial because Machine output placement still uses the same native backpack helper.
- Did not change Oil behavior, Machine behavior, OneAction behavior, hook logic, smoke behavior, or CameraZoom behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/OilCoalDrop/DolocTownExperimentalBridgeApi.OilCoalDrop.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0014-gamebridge-oilcoaldrop-feature-split.md`
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
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseNewContentApis -SkipBuild -TimeoutSeconds 300 -AutoExitAfterSecondsOverride 240`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-083946`
- Startup evidence: `StartupLog=true` in `result.json`.
- Third-save evidence: logs show `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`.
- Oil metadata evidence: logs show `Smoke exercise NewContentOilItemMetadata OK id=crude_oil, display=原油, sourceId=Local.DTMAPI_Oil, category=material_ore, salable=True, fuelEnergy=1500, baseHighestFuel=pumpkin:1200, nativeProbe={found crude_oil in DolocConfig.Tables.TbItem.}` and `Smoke.NewContentOilItemMetadata = verified`.
- Oil drop evidence: logs show `OilMod mining drop OK source=native-tool-hit, resource=coal_mine, forced=True, roll=0.7054, oilDrop=crude_oil, count=1, placement={Placed crude_oil x1 through native backpack placement.}`.
- Smoke hook evidence: logs show `Smoke exercise NewContentOilCoalDrop OK ... resource=coal_mine ... tool=steel_pickaxe ... afterDrops=1 ... bridge={source=native-tool-hit ... oilDrop=crude_oil ...}` and `Smoke.NewContentOilCoalDrop = verified`.
- Exit evidence: `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`, `process-check.txt` says no `DolocTown.exe`, and `fatal-window-check.txt` says no fatal instance popup.

## Related Records

- Previous split slice: `docs/updates/2026/20260608-0013-gamebridge-actioncompletion-feature-split.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-FEATURE-OILCOALDROP-001`
- Existing regression case: `docs/debug/regressions/smoke-matrix.md` row `NEWCONTENT-024-F`
- Hook map: `docs/hook-map/README.md` row `Resources.OilCoalDrop`

## Rollback Notes

- Move `DolocTownExperimentalBridgeApi.OilCoalDrop.cs` contents back into `DolocTownExperimentalBridgeApi.cs`.
- No hook IDs, callback paths, status text, log text, public API semantics, Oil coal-drop behavior, Machine behavior, or OneAction behavior require rollback because this was a mechanical feature split.

## Follow-Up

- Continue moving one feature at a time into `Features/`.
- Keep Oil behavior changes, Machine behavior changes, and smoke harness changes separate from this mechanical split.
- Split Smoke and Diagnostics only after feature slices remain build- and smoke-clean.
