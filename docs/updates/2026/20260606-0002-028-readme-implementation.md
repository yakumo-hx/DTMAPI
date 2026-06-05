# 20260606-0002 0.2.8 Readme Implementation

Status: implemented
Area: version/gamebridge/ui/content/tools/smoke

## Source Request

The user asked to continue the `readme.md` 0.2.8 manual-QA implementation goal after compaction, with two active constraints:

- Do not execute motorcycle/SecondMotor smoke in this continuation.
- Recheck the prior permission blocker where `D:\steam\steamapps\common\Doloc Town\DTMAPI\smoke-settings.json` was denied.

## Changed Files

- Version/runtime/package sources: `Directory.Build.props`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`, test mod manifests, and official-info files were bumped to 0.2.8.
- Y-console host and smoke bridge: `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs` and `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`.
- GameBridge/API: `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs` and `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`.
- Content mods: `testmods/MoreEquipmentSlotsMod`, `testmods/AnimalHusbandryProgressMod`, `testmods/MineMod`, and `testmods/OilMod`.
- Smoke/state tooling: `tools/scripts/common.ps1`, `tools/scripts/run-game-smoke.ps1`, `tools/scripts/collect-logs.ps1`, `tools/scripts/status.ps1`, `tools/scripts/run-hook-probe.ps1`, `tools/scripts/run-startup-observer.ps1`, `tools/scripts/disable-smoke-settings.ps1`, and `tools/scripts/README.md`.
- Traceability docs: `readme.md`, `docs/debug/INDEX.md`, `docs/debug/regressions/smoke-matrix.md`, `docs/hook-map/README.md`, `docs/api/public-api-matrix.md`, `testmods/MoreEquipmentSlotsMod/README.md`, and this update/index entry.

## Summary

- Bumped controlled DTMAPI sources and official-local package metadata from 0.2.7 to 0.2.8.
- Upgraded MoreEquipmentSlots from a read-only player strip to reflected native `AccessorySlot` clones with click/hover callbacks, passive accessory support, attribute-only hat support, vanilla hat visual preservation, and recovery.
- Reworked AnimalHusbandryProgress viewer rendering so hidden-produce progress uses independent cloned native `ProgressBar` rows instead of overwriting `moodInfo`, `moodProgress`, or `stateDescription`.
- Expanded the Y-console item grid/page size and removed the unusable `0.5x` movement option; the player-facing levels are now `1x/2x/3x/4x`.
- Renamed the active OilMod item id from `dtmapi_oil` to `crude_oil` across JSON, runtime, Mine output, coal-drop placement, smoke assertions, and docs.
- Converted MineMod to electric-only production, removed player-facing fuel/fuel-capacity options, added experimental recipe input registration through `MachineDefinition.RecipeInputs`, and updated the fallback recipe to `metal_framework x15`, `engine_core x10`, `steel_ingot x20`, `coal x100`.
- Added `DTMAPI_RUNTIME_DIR` / `DTMAPI_STATE_DIR` support for the runtime state directory and added `run-game-smoke.ps1 -DisableSecondMotorForSmoke`, which temporarily disables `Local.DTMAPI_SecondMotor` during non-vehicle smokes and restores it afterward.

## Validation

- The permission blocker was rechecked: `D:\steam\steamapps\common\Doloc Town\DTMAPI\smoke-settings.json` is writable again, and the file was restored to `{"Enabled": false}`.
- `tools/scripts/build.ps1` passed with 0 errors and UnitTests OK after the implementation. NuGet `NU1900` vulnerability-index warnings remained non-fatal.
- New-content third-save DirectExe smoke `GAME-SMOKE/20260606-031316` passed with `DisableSecondMotorForSmoke=True`, `AutoExerciseVehicle=False`, `NewContentOilItemMetadata=true`, `NewContentOilCoalDrop=true`, `NewContentMineOfficialJson=true`, `NewContentMineProduction=true`, `NewContentEquipmentSlots=true`, clean exit, and no fatal popup.
- Animal third-save DirectExe smoke `GAME-SMOKE/20260606-031522` passed with `DisableSecondMotorForSmoke=True`, `AutoOpenAnimalPanel=True`, `AnimalViewerUi=true`, clean exit, and no fatal popup.
- Y-console third-save DirectExe smoke `GAME-SMOKE/20260606-031919` passed with `DisableSecondMotorForSmoke=True`, `AutoExerciseVehicle=False`, `DebugConsoleMouseGive=true`, `DebugInventory=true`, `DebugWeather=true`, `DebugTeleport=true`, `DebugTime=true`, `DebugMovement=true`, clean exit, and no fatal popup.
- Mine-only third-save DirectExe smoke `GAME-SMOKE/20260606-032127` passed with `DisableSecondMotorForSmoke=True`, `AutoExerciseMineContentApis=True`, `NewContentMineOfficialJson=true`, `NewContentMineProduction=true`, clean exit, and no fatal popup.
- All four final evidence directories have `process-check.txt` entries saying no `DolocTown.exe` process was found. The temporary SecondMotor disable path restored `Local.DTMAPI_SecondMotor.enabled=true`.

## Evidence

- MoreEquipmentSlots: `GAME-SMOKE/20260606-031316` logs native clone bind diagnostics, `interactive=3`, `hoverable=3`, `readOnly=false`, passive `grandmas_button` equip/recover, hat `straw_hat` equip/recover, and `nativeHat=miner_helmet->miner_helmet->miner_helmet`.
- AnimalHusbandryProgress: `GAME-SMOKE/20260606-031522` logs `Smoke.AnimalViewerProgressUi = verified. independent cloned ProgressBar rows=1`, `moodOverride=False`, `stateDescriptionOverride=False`, and `renderPath=independent-cloned-progressbar`.
- Y-console: `GAME-SMOKE/20260606-031919` logs Y open/Esc/Y close, left/right mouse give, inventory/weather/teleport/time checks, and `Smoke.DebugMovement = verified. levels=1x:12,2x:24,3x:36,4x:48, restored=True, finalSpeed=12`.
- OilMod: `GAME-SMOKE/20260606-031316` logs `OilMod content item=crude_oil`, `Smoke.NewContentOilItemMetadata = verified`, native probe `found crude_oil in DolocConfig.Tables.TbItem`, and coal-drop placement `Placed crude_oil x1 through native backpack placement`.
- MineMod: `GAME-SMOKE/20260606-032127` logs `recipeInputs=metal_frameworkx15|engine_corex10|steel_ingotx20|coalx100`, `electricOnly=True`, `powerCost=10`, `outputRules=...|crude_oil:2:DTMAPI.OilMod`, five electric production cycles, `lastMode=electric`, `lastPowerCost=10`, and machine-owned storage output.
- Smoke safety: each final run used `DisableSecondMotorForSmoke=True`; no `-AutoExerciseVehicle` path was executed in this continuation.

## Related Records

- Review/goal ledger: `docs/updates/2026/20260606-0001-028-manual-qa-review-goal.md`.
- Prior 0.2.7 implementation: `docs/updates/2026/20260605-0006-027-manual-qa-root-cause.md`.
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`.
- API matrix: `docs/api/public-api-matrix.md`.
- Hook map: `docs/hook-map/README.md`.

## Rollback Notes

Revert the 0.2.8 version bump, GameBridge/UI changes, Mine/Oil content changes, smoke state-dir/SecondMotor-disable tooling, and documentation entries. If only the `crude_oil` rename is rolled back, also restore Mine recipes/output rules and smoke assertions to keep runtime item ids consistent.

## Follow-up

- Manual visual screenshot capture remains useful for the in-save MoreEquipmentSlots strip because the automated Unity screenshot fallback is still unavailable in that path, but the current log/summary evidence proves native clone hover/click wiring.
- The Mine OilMod replacement recipe path remains experimental and should be manually rechecked with OilMod enabled/disabled across a fresh game launch before declaring it stable public behavior.
