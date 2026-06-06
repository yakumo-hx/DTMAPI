# 20260606-0004 - 0.2.9 readme implementation

## Status

Implemented. Final build verification was run after the code and docs updates.

## Source Request

- Continue the existing DTMAPI workspace after the write-permission pause.
- Do not restart the project from zero.
- Continue the `readme.md` 0.2.9 follow-up work.
- Do not execute motorcycle smoke automation.
- Recheck the previous blocker: `D:\...\Doloc Town\DTMAPI\smoke-settings.json` access denied.

## Summary

- Bumped controlled runtime, mod manifests, official local package metadata, and installer dependency versions to `0.2.9`.
- Added the experimental More Saves mod and `ISaveSlotsApi`; it expands the official save UI path by setting `DolocAPI.gameManager.archiveFileCount` so `LocalSave` and `GameDataPanel` remain the owner of save-file discovery, render, load, delete, and copy behavior.
- Tightened the Y console layout: 35 item cells, taller source/category lists, moved filter pagers down, and centered 60px icons inside the 100x74 item cells.
- Updated animal hidden-product rendering so cloned `ProgressBar` rows are filled while inactive, then activated, reducing the visible stale mood-text flicker path without overriding native mood/state text.
- Moved More Equipment Slots sidecar writes into a native save transaction: equip/unequip now marks dirty, `SaveGame` postfix flushes dirty owners, and `SaveLoaded` / `ReturnedToTitle` clears unsaved in-memory state.
- Simplified MoreEquipmentSlots player config to the single visible Enabled toggle while preserving old serialized fields for compatibility.
- Made Mine official JSON publish `EComProtoAppliance` power metadata and changed machine production to call the native electronic component `Launch()` path before producing.
- Added smoke harness support for delayed instant-save so combined new-content plus save-transaction evidence can be captured after runtime mutations.

## Changed Files

- `Directory.Build.props`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `testmods/MoreEquipmentSlotsMod/*`
- `testmods/MineMod/Content/equipment_tbequipment.json`
- `testmods/MoreSavesMod/*`
- `tools/scripts/build.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/run-game-smoke.ps1`
- docs touched by this record, the debug index, hook map, smoke matrix, and public API matrix.

## Validation

- Rechecked `D:\steam\steamapps\common\Doloc Town\DTMAPI\smoke-settings.json`: file was writable and `disable-smoke-settings.ps1` restored it to `{ "Enabled": false }`.
- `run-game-smoke.ps1 -DirectExe -SaveSlot 3 -AutoExerciseMineContentApis -DisableSecondMotorForSmoke -TimeoutSeconds 240` passed: `docs/debug/evidence/GAME-SMOKE/20260606-051653`.
- `run-game-smoke.ps1 -DirectExe -SaveSlot 3 -AutoOpenAnimalPanel -DisableSecondMotorForSmoke -TimeoutSeconds 240` passed: `docs/debug/evidence/GAME-SMOKE/20260606-051838`.
- `run-game-smoke.ps1 -DirectExe -SaveSlot 3 -AutoExerciseDebugConsole -AutoExerciseDebugConsoleMouseGive -AutoExerciseDebugInventory -AutoExerciseDebugWeather -AutoExerciseDebugTeleport -AutoExerciseDebugTime -AutoExerciseDebugMovement -DisableSecondMotorForSmoke -TimeoutSeconds 260` passed: `docs/debug/evidence/GAME-SMOKE/20260606-051958`.
- `run-game-smoke.ps1 -DirectExe -SaveSlot 3 -AutoExerciseNewContentApis -DisableSecondMotorForSmoke -TimeoutSeconds 300` passed: `docs/debug/evidence/GAME-SMOKE/20260606-052246`.
- `run-game-smoke.ps1 -DirectExe -SaveSlot 3 -AutoExerciseNewContentApis -AutoExerciseInstantSave -AutoExerciseInstantSaveDelaySeconds 8 -DisableSecondMotorForSmoke -TimeoutSeconds 320` passed after fixing the combined-smoke auto-exit condition: `docs/debug/evidence/GAME-SMOKE/20260606-053233`.
- All listed smokes used `DisableSecondMotorForSmoke=True` and did not pass `-AutoExerciseVehicle`.
- All listed process checks say no `DolocTown.exe` remained.

## Evidence Highlights

- More Saves official UI: `GAME-SMOKE/20260606-051653` logs `Smoke.MoreSavesOfficialSaveUi = verified` with `archiveFileCount=12`, `panelSlotCount=12`, and `renderedSlots=12`.
- Mine official electric path: `GAME-SMOKE/20260606-051653` and `GAME-SMOKE/20260606-052246` log `electronicComponent=EComProtoAppliance/10`, `Official electric component Launch OK`, then a low-power `Launch returned false` skip with due retained.
- Animal UI: `GAME-SMOKE/20260606-051838` logs `independent cloned ProgressBar prefilled rows=1`, `moodOverride=False`, and `stateDescriptionOverride=False`.
- Y console: `GAME-SMOKE/20260606-051958` logs Y/Esc/Y close/open evidence, left/right native backpack give, inventory/weather/teleport/time/movement checks, and screenshot `D:\steam\steamapps\common\Doloc Town\DTMAPI\evidence\DEBUG-CONSOLE-UI\20260606-052037\debug-console.png`.
- MoreEquipmentSlots no-save safety: `GAME-SMOKE/20260606-052246` logs sidecar `dirty` entries but no `SaveSaved` / `storage persisted`; the sidecar timestamp remained from before the run.
- MoreEquipmentSlots save transaction: `GAME-SMOKE/20260606-053233` logs `Player.EquipmentSlotsSaveTransaction = dirty`, then `SaveSaved hook dispatched`, `EquipmentSlots storage persisted`, and `Player.EquipmentSlotsSaveTransaction = verified. Flushed 1 dirty equipment-slot owner(s) after native SaveGame completed.`

## Related Records

- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`
- `docs/updates/2026/20260606-0003-public-preview-and-029-review-goal.md`

## Rollback Notes

- Revert the 0.2.9 version bump, the `ISaveSlotsApi` abstractions/registration, the `MoreSavesMod` package/install entries, and the save-slot `archiveFileCount` runtime refresh to remove More Saves.
- Revert the equipment-slot transaction changes only with care: old behavior wrote sidecars immediately and could persist no-save equip state.
- The Mine `EComProtoAppliance` JSON and native `Launch()` call are coupled; reverting one without the other would return to field-only electric checks or unsupported JSON metadata.
- The smoke harness delayed instant-save parameter is validation-only and can be kept independently.

## Follow-Up

- Manual visual QA should still inspect the More Saves official save page across delete/copy/load operations, because this pass verifies render/load path ownership and 12 visible slots, not every official context-menu action.
- MoreEquipmentSlots sidecar storage is still global per owner rather than per save slot; the 0.2.9 transaction prevents no-save writes but does not yet migrate historical global storage into per-save files.
