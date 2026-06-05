# 20260603-0019 - Mine JSON Telemetry Smoke

## Source Request / Goal

Continue the 0.2.4 new-content goal, especially Task G: Mine should use official JSON for the content shell, remain separate from the water well, use the well art at 2x footprint, craft through the equipment workbench with Oil, and expose DTMAPI machine fuel/electric/time/output behavior without leaking raw Doloc Town types through the public API.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `testmods/MineMod/ModEntry.cs`
- `testmods/MineMod/i18n/schinese.json`
- `testmods/MineMod/i18n/english.json`
- `testmods/MineMod/README.md`
- `tools/scripts/run-game-smoke.ps1`
- `readme.md`
- `docs/debug/INDEX.md`
- `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/INDEX.md`

## Implementation

- Expanded experimental `MachineProductionState` with player-visible/read-only telemetry: item/equipment/recipe/group ids, visual scale, fuel capacity/remaining, fuel/electric cycle costs, cycle minutes/TUs, next due TUs, mode, and last-cycle fuel/electric cost.
- Populated that telemetry from the GameBridge machine runtime entry during registration, placed-equipment polling, and production cycles.
- Updated MineMod's config status paragraph to show registered/placed count, mode, fuel, cycle, electric cost, recipe group, visual scale, and last output.
- Added `Smoke.NewContentMineOfficialJson` to the NewContent smoke. It validates native `dtmapi_mine` item metadata, `EquipmentInfo.CoverSize`, base `well` cover size, `sprite_equipment_well`, `RecipeInfo` inputs/output, `equipment_workbench` recipe-group membership, and Machine API state.
- Enriched transient equipment smoke descriptions with cover size and scene asset URL, so the Mine production log directly records `cover=8x6` and `scene=sprite_equipment_well`.
- Added `NewContentMineOfficialJson` to `run-game-smoke.ps1` result gating.

## Version

The controlled project version for this round remains the already-bumped `0.2.4`; the required 0.2.3 -> 0.2.4 version bump is recorded in `20260603-0012-024-baseline-manual-regressions.md`.

## Validation

- Release build/unit: `powershell -ExecutionPolicy Bypass -File tools/scripts/build.ps1` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Passing third-save NewContent smoke: `docs/debug/evidence/GAME-SMOKE/20260603-192856`.
- Result: `SaveLoaded=True`, `NewContentMineOfficialJson=True`, `NewContentMineProduction=True`, `NewContentOilItemMetadata=True`, `NewContentOilCoalDrop=True`, `NewContentEquipmentSlots=True`, `NewContentApis=True`, `NoFatalInstanceWindow=True`, `ProcessExited=True`, `ForcedClose=False`.
- Exit check: `docs/debug/evidence/GAME-SMOKE/20260603-192856/process-check.txt` says `No DolocTown.exe process found.`; `fatal-window-check.txt` says `No fatal instance popup found.`

## Evidence

- Mine official JSON/API log: `Smoke exercise NewContentMineOfficialJson OK item=矿井, source=DTMAPI/Local.DTMAPI_Mine/DTMAPI 矿井, icon=icon_item_well, itemFunction=ItemFunctionEquipment, subtype=equipment_ornament, cover=8x6, baseWellCover=4x3, sceneAsset=sprite_equipment_well, equipmentFunction=EquipmentFuncDecorator, recipeOutput=dtmapi_mine, recipeInputs=stonex80|iron_ingotx8|electric_wirex4|dtmapi_oilx5, techPoint=30, defaultUnlock=True, recipeGroup=equipment_workbench includes=True, machineApi=...visualScale:2/fuel:0-7200/cycleMinutes:120/costs:120-20-10, outputRules=coal:18:vanilla|copper_ore:10:vanilla|iron_ore:6:vanilla|dtmapi_oil:2:DTMAPI.OilMod`.
- Mine production log: `MachineProduction cycle OK owner=DTMAPI.MineMod machine=dtmapi.mine equipment=dtmapi_mine output=copper_ore count=2 mode=electric fuelRemaining=7180 totalTUs=38955`.
- Production summary includes transient placement proof: `mine={dtmapi_mine/DolocTown.Decorator/index=108/anchor=2,1/cover=8x6/scene=sprite_equipment_well}` and state `fuel=7180/7200`, `cycleTUs=24`, `nextDueTUs=38979`, `electricPower=10`, `lastCost=20/10`, and native backpack placement for `copper_ore x2`.

## Related Records

- Debug issue: `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`.
- Smoke matrix: `NEWCONTENT-024-G`.
- Hook map: `Machine.ProductionRuntimeLoop`.
- API matrix: `IMachineProductionApi`, `MachineProductionState`.
- Prior records: `20260603-0014-024-new-content-api-partial.md`, `20260603-0017-oil-coal-drop-newcontent-smoke.md`, `20260603-0018-oil-metadata-fuel-smoke.md`.

## Rollback

Remove the added `MachineProductionState` telemetry fields and Mine status text, remove `Smoke.NewContentMineOfficialJson` and the smoke runner result gate, and restore previous docs if the extra reflection checks prove too strict for a later Doloc Town build. Keep `GAME-SMOKE/20260603-192856` as the before/after evidence for JSON metadata, workbench recipe membership, 2x cover proof, and runtime telemetry.

## Follow-Up

- Native/player fuel-electric UI remains unfinished; the current visible status is DTMAPI config/API telemetry, not an in-world native machine panel.
- Player-driven craft/place visual proof from the equipment workbench remains needed; the current placement proof is a smoke-created transient `dtmapi_mine`.
- More Equipment Slots still needs true native player equipment screen integration.
- SecondMotor edge-transition smoke was later covered in `20260603-0020-second-motor-edge-transition-smoke.md`; manual visual crossing remains useful but is no longer tracked as this record's follow-up blocker.
