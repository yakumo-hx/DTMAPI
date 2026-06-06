# Update 20260606-0011: 0.3.0 Advanced Y Console Closure

- Date: 2026-06-06
- Status: implemented
- Area: gamebridge/y-console/debug-api/content/smoke
- Source request: active `/goal` for DTMAPI 0.3.0 official-console-informed Y Console expansion plus Zoom, Chest Locator Enhancer, and Strong Planting Gun.
- Version: no new bump in this slice. The current controlled runtime/package version remains `0.3.1` because `20260606-0007` already advanced the worktree after the original 0.3.0 bump.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `testmods/DebugConsoleMod/Content/item_tbitem.json`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `readme.md`

## Summary

- Closed the remaining advanced Y-console blockers from `20260606-0008`.
- Added GameBridge-owned Harmony callbacks for creative no-cost/no-energy checks and `Synthesizer.GetRecipeTime` no-time behavior.
- Creative mode now applies and restores native `GameInitConfig` debug flags for material cost, shop money verification, and spirit cost only while the Y-console toggle is enabled.
- Added official-local JSON content for `dtmapi_creative_generator` under `DTMAPI_YKeyConsole` so native `DolocConfig.Tables.TbItem` can load and spawn it.
- Switched monster spawn from a brittle direct interface lookup to the official whitelisted `DolocAPI.Command_GenerateMonster(string,int)` path after current-room and monster-id validation.
- Tightened `-AutoExerciseAdvancedDebug` smoke so creative hooks, generator give, monster spawn, and resource spawn are required for `Smoke.AdvancedDebug = verified`.

## Validation

- Release build/unit: `tools/scripts/build.ps1` passed with 0 errors. Only restricted-network NU1900 vulnerability-index warnings occurred.
- Passing third-save smoke: `GAME-SMOKE/20260606-172855`.
- Smoke command used a local environment path and DirectExe Steam identity:
  - `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'`
  - `tools/scripts/run-game-smoke.ps1 -DirectExe -SkipBuild -SaveSlot 3 -TimeoutSeconds 260 -AutoExerciseAdvancedDebug`
- Result highlights: `AdvancedDebug=true`, `SaveLoaded=true`, `GameLaunched=true`, `NoFatalInstanceWindow=true`, `ProcessExited=true`, and `ForcedClose=false`.
- Key log facts:
  - `Debug.CreativeMode = verified` with `ignoreMaterialCost=True`, `skipMoneyVerifyInShop=True`, `ignoreSpiritCost=True`, `canAffordMoneyIntMax=True`, `costEnergyNoChange=True`, `noTimeHookInstalled=True`, and `generatorAvailable=True`.
  - `Debug.CreativeGeneratorGive = verified. Gave 1 dtmapi_creative_generator through native backpack placement.`
  - `Debug.SpawnMonster = verified. Spawned monster aircraft count=1 through official Command_GenerateMonster.`
  - `Debug.SpawnResource = verified. Spawned resource alfalfa count=1 at -1,-2.`
  - `Smoke.AdvancedDebug = verified` with day advance, 4x/reset time scale, money, tech point, tech-tree unlock, crop maturity, creative, generator, monster, and resource evidence.
- Exit checks: `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup.

## Evidence Links

- Passing game smoke: `docs/debug/evidence/GAME-SMOKE/20260606-172855`
- Result: `docs/debug/evidence/GAME-SMOKE/20260606-172855/result.json`
- DTMAPI log: `docs/debug/evidence/GAME-SMOKE/20260606-172855/DTMAPI-latest.log`
- Process check: `docs/debug/evidence/GAME-SMOKE/20260606-172855/process-check.txt`

## Related Records

- Original advanced Y-console partial: `docs/updates/2026/20260606-0008-030-advanced-yconsole-partial.md`
- Zoom slice: `docs/updates/2026/20260606-0006-030-zoom-api-mod-slice.md`
- Chest Locator Enhancer slice: `docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md`
- Strong Planting Gun slice: `docs/updates/2026/20260606-0010-030-strong-planting-gun.md`
- Hook map: `docs/hook-map/README.md#hook-debugadvancedyconsoleapis`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- API matrix: `docs/api/public-api-matrix.md`

## Rollback Notes

- Remove the creative-mode Harmony patch registrations and callbacks to return creative mode to a status-only toggle.
- Remove `testmods/DebugConsoleMod/Content/item_tbitem.json` to withdraw the content-backed creative generator.
- Revert `SpawnMonster` to a failed/safe DTO path if the official command wrapper becomes incompatible with a future game build.
- Keep the dangerous raw console/Lua/load/reset/story commands excluded.

## Follow-Up

- Optional manual UI acceptance can still inspect the advanced Y-console panel text and generator item appearance, but the required automated third-save functionality is verified.
