# 20260603-0018 - Oil Metadata Fuel Smoke

## Source Request / Goal

Continue the 0.2.4 new-content goal, especially Task F: Oil must remain official JSON content, appear correctly in the Y-console inventory classification/search path, expose sale/icon/localization/fuel metadata, and use a fuel value above the current highest base-game fuel item.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `tools/scripts/run-game-smoke.ps1`
- `testmods/OilMod/Content/item_tbitem.json`
- `testmods/OilMod/ModEntry.cs`
- `testmods/OilMod/README.md`
- `readme.md`
- `docs/debug/INDEX.md`
- `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/2026/20260603-0017-oil-coal-drop-newcontent-smoke.md`
- `docs/updates/INDEX.md`

## Implementation

- Raised `dtmapi_oil` from `electric_energy=900` to `electric_energy=1500`; the native base table's highest fuel item is currently `pumpkin=1200`, so Oil now satisfies the high-fuel requirement.
- Added `Smoke.NewContentOilItemMetadata` to the NewContent smoke flow. It checks native `DolocAPI.QueryItemProto` metadata and the same `IInventoryDebugApi.GetItems` source/category data consumed by the Y console.
- The metadata smoke asserts `sourceKind=DTMAPI`, `sourceId=Local.DTMAPI_Oil`, source-filter inclusion, category listing, mining/processing tags, localized title, description, salable price, buy price, stack overlay, native icon presence, and indexed official JSON icon key.
- Added the metadata result to `run-game-smoke.ps1` so `-AutoExerciseNewContentApis` cannot pass unless Oil metadata, Oil coal-drop, Mine production, and EquipmentSlots smoke all pass.
- Rejected an over-strict icon check: native `UiSpriteAsset.ToString()` reports the type name, so final validation requires a native sprite object plus the indexed official JSON `icon_item_coal` key.

## Validation

- Release build/unit: `powershell -ExecutionPolicy Bypass -File tools/scripts/build.ps1` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Passing third-save NewContent smoke: `docs/debug/evidence/GAME-SMOKE/20260603-190919`.
- Result: `SaveLoaded=True`, `NewContentOilItemMetadata=True`, `NewContentOilCoalDrop=True`, `NewContentEquipmentSlots=True`, `NewContentMineProduction=True`, `NewContentApis=True`, `NoFatalInstanceWindow=True`, `ProcessExited=True`, `ForcedClose=False`.
- Exit check: `docs/debug/evidence/GAME-SMOKE/20260603-190919/process-check.txt` says `No DolocTown.exe process found.`; `fatal-window-check.txt` says `No fatal instance popup found.`

## Evidence

- Startup/content log: `OilMod content item=dtmapi_oil fuelEnergy=1500 officialJson=item_tbitem.json`.
- Oil metadata log: `Smoke exercise NewContentOilItemMetadata OK ... sourceKind=DTMAPI, sourceId=Local.DTMAPI_Oil, sourceGroup=DTMAPI 石油/1, sourceFilterIncludesOil=True, category=material_ore, categoryListed=True, tags=...material_ore|mining|processing..., salable=True, sellingPrice=45, buyingPrice=240, fuelEnergy=1500, baseHighestFuel=pumpkin:1200, indexedIcon=icon_item_coal, title=石油, subType=material_ore, function=ItemFunction`.
- Oil mining log: `Smoke exercise NewContentOilCoalDrop OK ... resource=coal_mine ... oilDrop=dtmapi_oil, count=1, placement={Placed dtmapi_oil x1 through native backpack placement.}`.
- Same-run Mine log: `Smoke exercise NewContentMineProduction OK ... mine={dtmapi_mine/...} ... output=coal, count=2, mode=electric ... electricPowerCost=10, Placed coal x2 through native backpack placement.`
- Same-run EquipmentSlots log: `Smoke exercise NewContentEquipmentSlots OK give=grandmas_button 0->1 ... equipBackpack=1->0 ... recoverBackpack=0->1 ... recoveredStored=0`.

## Related Records

- Debug issue: `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`.
- Smoke matrix: `NEWCONTENT-024-F`, `NEWCONTENT-024-G`, `NEWCONTENT-024-H`.
- Hook map: `Resources.OilCoalDrop`, `Machine.ProductionRuntimeLoop`, `Player.EquipmentSlotsStatsRefresh`.
- API matrix: `IContentQueryHelper.GetIndexedItems/GetIndexedItem`, `IInventoryDebugApi.GetItems/GiveItem`, `IMachineProductionApi`, `IEquipmentSlotsApi`.
- Prior records: `20260603-0014-024-new-content-api-partial.md`, `20260603-0017-oil-coal-drop-newcontent-smoke.md`.

## Rollback

Revert the Oil fuel value to `900`, remove `Smoke.NewContentOilItemMetadata` and the runner result gate, and restore the previous docs if the official JSON value or metadata smoke causes a regression. Keep `GAME-SMOKE/20260603-190919` as the before/after evidence for fuel/category/sale/icon/localization behavior.

## Follow-Up

- Mine still needs native fuel/electric player UI and true craft/place visual proof before the full new-mod goal can be complete.
- More Equipment Slots still needs true native player equipment screen integration, not only the DTMAPI config-panel storage/recovery path.
- SecondMotor edge-transition smoke was later covered in `20260603-0020-second-motor-edge-transition-smoke.md`; manual visual crossing remains useful but is no longer a full-goal blocker.
