# 20260603-0014: 0.2.4 new content and API partial

## Summary

Added the 0.2.4 Oil, Mine, and More Equipment Slots research/implementation slice. Oil and Mine use official local JSON packages where possible; Machine and Equipment Slots APIs are experimental and do not expose raw Doloc Town types.

## Source Request

- User `/goal` on 2026-06-03: research and land 石油, 矿井, and 更多装备栏位 through official JSON plus DTMAPI APIs, while keeping fragile reflection/Harmony in GameBridge and ordinary mods outside `BepInEx/plugins`.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `testmods/OilMod/**`
- `testmods/MineMod/**`
- `testmods/MoreEquipmentSlotsMod/**`
- `tools/scripts/build.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/run-game-smoke.ps1`

## Validation

- Release build/unit: passed on 2026-06-03.
- Install script copied official local packages to `LocalLow/RedSawGames/DolocTown/MODS` and DTMAPI bootstrap only to `BepInEx/plugins/DTMAPI`.
- Third-save smoke `docs/debug/evidence/GAME-SMOKE/20260603-152451` passed with `AutoExerciseNewContentApis=True`, clean startup, save-load slot/index=2, and no leftover `DolocTown.exe`.
- Third-save smoke `docs/debug/evidence/GAME-SMOKE/20260603-164202` passed after restoring DirectExe child `SteamAppId=2285550;SteamGameId=2285550`; result has `SaveLoaded=True`, `NewContentApis=True`, `NewContentMineProduction=True`, `NoFatalInstanceWindow=True`, and `ProcessExited=True`.
- Follow-up third-save smoke `docs/debug/evidence/GAME-SMOKE/20260603-170813` passed after the equipment-slot storage/recovery implementation; result has `NewContentEquipmentSlots=True`, `NewContentMineProduction=True`, `NewContentApis=True`, `NoFatalInstanceWindow=True`, `ProcessExited=True`, and no leftover `DolocTown.exe`.

## Evidence

- Oil log: `OilMod content item=dtmapi_oil fuelEnergy=900 officialJson=item_tbitem.json`.
- Mine log: `Mine machine API register success=True reason=SaveLoaded slot=2 message=Registered machine definition dtmapi.mine with 4 output rules`.
- Mine production log: `MachineProduction cycle OK owner=DTMAPI.MineMod machine=dtmapi.mine equipment=dtmapi_mine output=copper_ore count=1 mode=electric fuelRemaining=7180 totalTUs=38955`, followed by `Smoke exercise NewContentMineProduction OK ... Placed copper_ore x1 through native backpack placement`.
- Equipment slots log before `20260603-0015`: `MoreEquipmentSlots API register success=True reason=SaveLoaded slot=2 message=configured-experimental-stats-hook`.
- Equipment slot follow-up log in `GAME-SMOKE/20260603-170813`: `EquipmentSlots equip OK owner=DTMAPI.MoreEquipmentSlotsMod slot=dtmapi.more_equipment.1 item=grandmas_button display=奶奶的纽扣 backpack=1->0 applied=True`, then `EquipmentSlots unequip OK ... recovered=1`, and `Smoke exercise NewContentEquipmentSlots OK ... recoverBackpack=0->1, recoveredStored=0`.

## Current Limits

- Oil: official JSON item/fuel value/load is proven; coal random-drop path is implemented but not yet smoke-proven by mining a coal resource.
- Mine: official JSON item/equipment/recipe, API registration, runtime loop discovery, electric-mode fuel/power accounting, and native backpack output delivery are smoke-proven with a temporary placed `dtmapi_mine`; native fuel/electric player UI remains experimental.
- More Equipment Slots: API registration, DTMAPI-managed config panel controls, storage, native attribute-function application, and populated recovery are smoke-proven in `GAME-SMOKE/20260603-170813`; true native player equipment screen integration remains unfinished.

## Related Records

- Debug issue: `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Hook map: `docs/hook-map/README.md`
- API matrix: `docs/api/public-api-matrix.md`

## Rollback

- Remove official local packages `DTMAPI_Oil`, `DTMAPI_Mine`, and `DTMAPI_MoreEquipmentSlots` from the install set, revert the experimental API additions, and keep existing migrated mod packages unchanged.

## Follow-Up

- Add a third-save mining smoke that forces or repeats a coal-resource hit until `dtmapi_oil` drop evidence is captured.
- Decide whether the DTMAPI config-panel slot manager is acceptable for the player-visible requirement, or add deeper native equipment-screen integration before claiming More Equipment Slots complete.
