# 20260603-0015 - Equipment Slots Storage/Recovery Smoke

## Source Request / Goal

Continue the 0.2.4 manual-QA/new-mod goal, specifically Task H: More Equipment Slots must keep vanilla visual slots, make extra slots attribute-only, and safely recover extra-slot items when disabled or missing instead of silently deleting them.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `testmods/MoreEquipmentSlotsMod/ModEntry.cs`
- `testmods/MoreEquipmentSlotsMod/i18n/english.json`
- `testmods/MoreEquipmentSlotsMod/i18n/schinese.json`
- `tools/scripts/run-game-smoke.ps1`
- `readme.md`
- `docs/debug/INDEX.md`
- `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260603-0014-024-new-content-api-partial.md`

## Implementation

- Expanded experimental `IEquipmentSlotsApi` with DTMAPI-managed slot listing, equip, unequip, and populated recovery results.
- Added GameBridge-owned storage under DTMAPI config, attribute-only passive item validation, native `AgentEquipmentFunction` application, and recovery through `DolocAPI.TryPlaceInBackpack(..., sendEmailOnOverflow: true)`.
- Added orphan-storage recovery when a save loads and stored extra-slot state exists for a mod that did not register.
- Upgraded `MoreEquipmentSlotsMod` with a DTMAPI config-panel slot manager: slot status, quick-equip item ID, equip-first-empty, and recover-all controls.
- Extended new-content smoke to give `grandmas_button`, equip it into `dtmapi.more_equipment.1`, verify native attribute application, recover it, then continue the Mine production smoke.

## Validation

- Release build/unit: `powershell -ExecutionPolicy Bypass -File tools/scripts/build.ps1` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Third-save game smoke: `docs/debug/evidence/GAME-SMOKE/20260603-170813`.
- Result: `SaveLoaded=True`, `NewContentApis=True`, `NewContentEquipmentSlots=True`, `NewContentMineProduction=True`, `NoFatalInstanceWindow=True`, `ProcessExited=True`.
- Exit check: `docs/debug/evidence/GAME-SMOKE/20260603-170813/process-check.txt` says `No DolocTown.exe process found.`

## Evidence

- `OilMod content item=dtmapi_oil fuelEnergy=900 officialJson=item_tbitem.json`.
- `MoreEquipmentSlots API register success=True reason=SaveLoaded slot=2 message=configured-experimental-ui-storage-stats-hook`.
- `EquipmentSlots equip OK owner=DTMAPI.MoreEquipmentSlotsMod slot=dtmapi.more_equipment.1 item=grandmas_button display=奶奶的纽扣 backpack=1->0 applied=True`.
- `EquipmentSlots unequip OK owner=DTMAPI.MoreEquipmentSlotsMod slot=dtmapi.more_equipment.1 item=grandmas_button recovered=1 reason=new-content smoke recovery`.
- `Smoke exercise NewContentEquipmentSlots OK give=grandmas_button 0->1 ... equipBackpack=1->0 ... recoverBackpack=0->1, recoveredStored=0`.
- `MachineProduction cycle OK owner=DTMAPI.MineMod machine=dtmapi.mine equipment=dtmapi_mine output=iron_ore count=1 mode=electric fuelRemaining=7180 totalTUs=38955`.

## Related Records

- Debug: `docs/debug/issues/ISSUE-005-20260603-manual-qa-024-regressions.md`.
- Smoke matrix: `NEWCONTENT-024-H`.
- Hook map: `Player.EquipmentSlotsStatsRefresh`.
- API matrix: `IEquipmentSlotsApi`.
- Prior partial record: `docs/updates/2026/20260603-0014-024-new-content-api-partial.md`.

## Rollback

Revert the experimental `IEquipmentSlotsApi` expansion, GameBridge storage/apply/recovery logic, MoreEquipmentSlotsMod config-panel controls, and smoke harness `NewContentEquipmentSlots` gate. If rolling back after a player used extra slots, run the recovery action first or keep orphan recovery long enough to return stored items.

## Follow-Up

- Decide whether the DTMAPI config-panel slot manager satisfies the player-visible equipment UI requirement, or add deeper native equipment-screen integration.
- Add a separate smoke/manual pass for disabling or removing `MoreEquipmentSlotsMod` with populated slot storage to prove orphan recovery end-to-end.
- Remaining goal blockers outside this slice at the time: Oil coal-drop mining evidence and Mine native fuel/electric player UI evidence. AutoFishing auto-complete with skip off was resolved later in `docs/updates/2026/20260603-0016-autofishing-skipfalse-minigame-smoke.md`; Oil coal-drop mining was resolved later in `docs/updates/2026/20260603-0017-oil-coal-drop-newcontent-smoke.md`.
