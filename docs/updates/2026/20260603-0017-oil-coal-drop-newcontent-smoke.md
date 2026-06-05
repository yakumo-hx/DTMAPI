# 20260603-0017 - Oil Coal Drop NewContent Smoke

## Source Request / Goal

Continue the 0.2.4 new-content goal, especially Task F: keep Oil as official JSON content and prove that coal mining can produce `dtmapi_oil` through a DTMAPI GameBridge hook, without copying old DLK code or mutating Workshop content.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `testmods/OilMod/Content/item_tbitem.json`
- `testmods/MineMod/Content/*.json`
- `testmods/OilMod/README.md`
- `testmods/MineMod/README.md`
- `testmods/MoreEquipmentSlotsMod/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/INDEX.md`

## Implementation

- Fixed the Oil official JSON row from invalid `$type: ItemFunctionNone` to the game's valid empty `ItemFunction`, allowing native `TbItem` validation to accept `dtmapi_oil`.
- Moved Oil/Mine official JSON rows to package `Content` root files and hardened DTMAPI-owned official-local installs so stale root JSON / non-DTMAPI child content from older package layouts are removed before copying the current package.
- Added a `ToolCollider.HandleTools` prefix capture for coal resources so the post-hit oil-drop hook still knows which resource was removed after native hit handling clears or detaches the renderer/collider path.
- Added an Oil smoke stage to `-AutoExerciseNewContentApis`: ensure/create a rendered `coal_mine`, force the smoke-only oil roll, invoke native `ToolCollider.HandleTools` with a generated pickaxe, and require native backpack placement of `dtmapi_oil`.
- Aligned smoke reload with the official debug path by using `ModManager.ReloadMods` followed by `DolocConfig.Reload`, then probing native `DolocAPI.QueryItemProto` before the Oil smoke.

## Validation

- Release build/unit: `powershell -ExecutionPolicy Bypass -File tools/scripts/build.ps1` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Passing third-save NewContent smoke: `docs/debug/evidence/GAME-SMOKE/20260603-184455`.
- Result: `SaveLoaded=True`, `NewContentOilCoalDrop=True`, `NewContentEquipmentSlots=True`, `NewContentMineProduction=True`, `NewContentApis=True`, `NoFatalInstanceWindow=True`, `ProcessExited=True`, `ForcedClose=False`.
- Exit check: `docs/debug/evidence/GAME-SMOKE/20260603-184455/process-check.txt` says `No DolocTown.exe process found.`

## Evidence

- Installed content check: `DTMAPI_Oil/Content/item_tbitem.json` contains `dtmapi_oil`, `electric_energy=900`, and `$type: ItemFunction`.
- Oil startup log: `OilMod content item=dtmapi_oil fuelEnergy=900 officialJson=item_tbitem.json`.
- Oil mining log: `OilMod mining drop OK source=native-tool-hit, resource=coal_mine, forced=True, roll=0.9050, oilDrop=dtmapi_oil, count=1, placement={Placed dtmapi_oil x1 through native backpack placement.}`.
- Oil smoke log: `Smoke.NewContentOilCoalDrop = verified`, with a transient rendered `coal_mine`, native `steel_pickaxe`, `removed=True`, `beforeDrops=0`, and `afterDrops=1`.
- Mine smoke log in the same run: `MachineProduction cycle OK owner=DTMAPI.MineMod machine=dtmapi.mine equipment=dtmapi_mine output=iron_ore count=1 mode=electric fuelRemaining=7180 totalTUs=38955`.
- Equipment-slot smoke log in the same run: `EquipmentSlots equip OK ... backpack=1->0 applied=True`, followed by `EquipmentSlots unequip OK ... recovered=1` and `Smoke.NewContentEquipmentSlots = verified`.

## Related Records

- Debug issue: `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`.
- Smoke matrix: `NEWCONTENT-024-F`, `NEWCONTENT-024-G`, `NEWCONTENT-024-H`.
- Hook map: `Resources.OilCoalDrop`, `Machine.ProductionRuntimeLoop`, `Player.EquipmentSlotsStatsRefresh`.
- API matrix: `IMachineProductionApi`, `IEquipmentSlotsApi`.
- Prior records: `20260603-0014-024-new-content-api-partial.md`, `20260603-0015-equipment-slots-storage-recovery-smoke.md`.

## Rollback

Revert the Oil JSON `$type` fix, remove the prefix-captured Oil coal-drop bridge and NewContent smoke stage, and restore the prior install layout if necessary. Keep failed evidence `GAME-SMOKE/20260603-182049` / `20260603-182805` as the diagnostic record for invalid or non-native Oil item loading.

## Follow-Up

- Oil player-visible Y-console category/sell/icon/localization review was closed later by `20260603-0018-oil-metadata-fuel-smoke.md` / `GAME-SMOKE/20260603-190919`.
- Mine still needs native fuel/electric player UI and true craft/place visual proof before the goal can be complete.
- More Equipment Slots still needs true native player equipment screen integration, not only the DTMAPI config-panel storage/recovery path.
- SecondMotor edge-transition smoke was later covered in `20260603-0020-second-motor-edge-transition-smoke.md`; manual visual crossing remains useful but is no longer a full-goal blocker.
