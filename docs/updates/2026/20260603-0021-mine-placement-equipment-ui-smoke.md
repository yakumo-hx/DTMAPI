# 20260603-0021 - Mine Placement and Equipment UI Smoke

## Source Request / Goal

Continue the DTMAPI 0.2.4 manual-QA/new-content goal, especially Tasks G, H, and I. The remaining gaps were player-visible Mine placement/behavior evidence and More Equipment Slots player equipment UI evidence, after earlier smokes had already covered storage, recovery, and machine telemetry.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `testmods/MineMod/README.md`
- `testmods/MoreEquipmentSlotsMod/README.md`
- `readme.md`
- `docs/debug/INDEX.md`
- `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/INDEX.md`

## Implementation

- Tightened Mine official JSON smoke to require `dtmapi_mine` to generate a native `DolocTown.ItemEquipment`, not only table metadata.
- Added third-save Mine placement evidence capture after creating a temporary outdoor `dtmapi_mine`; the evidence records the player item type, placed decorator, footprint, scene asset, room, and screenshot.
- Extended `IEquipmentSlotsApi` runtime evidence with `DolocTown.UI.AccessoriesBar.__Init` and `OnStartShow` hooks.
- Added a read-only DTMAPI extra-slot strip by cloning native AccessoriesBar slot objects, rendering occupied DTMAPI extra slots, and disabling clone button interaction.
- Kept public equipment-slot API stable/experimental and free of raw decompiled UI types; GameBridge owns the fragile Unity/Harmony details.
- Extended new-content smoke summaries so Mine official JSON, Mine placement, Mine production, equipment-slot UI render state, equip, attribute-only application, and recovery are captured together.

## Version

The controlled project version remains `0.2.4`. The required patch bump from `0.2.3` to `0.2.4` is recorded in `20260603-0012-024-baseline-manual-regressions.md`; this record adds same-version closeout evidence for the Mine and More Equipment Slots slices.

## Validation

- Release build/unit: `powershell -ExecutionPolicy Bypass -File tools/scripts/build.ps1` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Passing third-save new-content smoke: `docs/debug/evidence/GAME-SMOKE/20260603-210216`.
- Result: `SaveLoaded=True`, `NewContentApis=True`, `NewContentOilItemMetadata=True`, `NewContentOilCoalDrop=True`, `NewContentMineOfficialJson=True`, `NewContentMineProduction=True`, `NewContentEquipmentSlots=True`, `NoFatalInstanceWindow=True`, `ProcessExited=True`, `ForcedClose=False`.
- Exit check: `docs/debug/evidence/GAME-SMOKE/20260603-210216/process-check.txt` says `No DolocTown.exe process found.`; `fatal-window-check.txt` says `No fatal instance popup found.`
- Non-positive retained attempt: `docs/debug/evidence/GAME-SMOKE/20260603-210359` was a later title-settings screenshot attempt that failed title menu gates but exited cleanly. Do not cite it as Mine or Equipment Slots proof.

## Evidence

- Mine official JSON: `Smoke exercise NewContentMineOfficialJson OK ... generatedItemType=DolocTown.ItemEquipment ... cover=8x6, baseWellCover=4x3, sceneAsset=sprite_equipment_well, recipeInputs=stonex80|iron_ingotx8|electric_wirex4|dtmapi_oilx5, recipeGroup=equipment_workbench includes=True, machineApi=...visualScale:2/fuel:0-7200/cycleMinutes:120/costs:120-20-10`.
- Mine placement: `Mine placement evidence OK playerItem=dtmapi_mine ItemEquipment, placed=dtmapi_mine/DolocTown.Decorator/index=108/anchor=2,1/cover=8x6/scene=sprite_equipment_well`.
- Mine screenshot: `docs/debug/evidence/GAME-SMOKE/20260603-210216/DTMAPI-evidence/NEWCONTENT-024/20260603-210256/mine-placed-dtmapi-mine.png`.
- Mine production: `MachineProduction cycle OK owner=DTMAPI.MineMod machine=dtmapi.mine equipment=dtmapi_mine output=copper_ore count=1 mode=electric fuelRemaining=7180 totalTUs=38955`; state records `fuel=7180/7200`, `cycleTUs=24`, `electricPower=10`, placed `1`, cycles `1`, and native backpack placement.
- Equipment API/UI hooks: `Player.EquipmentSlotsApi = experimental. Patched native equipment stat refresh and AccessoriesBar lifecycle... render a read-only player equipment strip without exposing raw game types`.
- Equipment UI render state: `reason=new-content smoke after equip, hooks=True, rendered=3, occupied=1, readOnly=true, attributeOnly=true, preserveVanillaVisualSlots=true, slots=dtmapi.more_equipment.1=grandmas_button;dtmapi.more_equipment.2=empty;dtmapi.more_equipment.3=empty`.
- Equipment recovery: `Smoke exercise NewContentEquipmentSlots OK give=grandmas_button 0->1 ... equipBackpack=1->0 ... recoverCount=1, recoverBackpack=0->1, recoveredStored=0`.

## Screenshot Limitation

Mine placement screenshot capture succeeded and was copied into the smoke evidence folder. The in-save equipment-strip screenshot fallback returned unavailable after rendering the UI clones, so the retained More Equipment Slots proof is log/summary evidence with `uiRendered=True`, `occupied=1`, and safe recovery. Manual visual screenshot capture remains useful polish, but it is no longer a blocker for the automated third-save contract.

## Related Records

- Debug issue: `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`.
- Smoke matrix: `NEWCONTENT-024-G`, `NEWCONTENT-024-H`.
- Hook map: `Machine.ProductionRuntimeLoop`, `Player.EquipmentSlotsApi`.
- API matrix: `IMachineProductionApi`, `IEquipmentSlotsApi`.
- Prior records: `20260603-0014-024-new-content-api-partial.md`, `20260603-0015-equipment-slots-storage-recovery-smoke.md`, `20260603-0017-oil-coal-drop-newcontent-smoke.md`, `20260603-0018-oil-metadata-fuel-smoke.md`, `20260603-0019-mine-json-telemetry-smoke.md`.

## Rollback

Remove the AccessoriesBar UI postfixes, read-only extra-slot clone renderer, and Mine placement screenshot smoke assertions. Keep the JSON/API registration paths and prior telemetry evidence if a future game build exposes a safer native equipment UI or machine panel integration point.

## Follow-Up

- Optional manual screenshot of the More Equipment Slots strip after opening the player equipment bar, because automated Unity screenshot capture was unavailable in that in-save context.
- Future polish may replace the read-only DTMAPI strip with a deeper native equipment-screen integration if the game exposes a stable extension point without raw decompiled type leakage.
