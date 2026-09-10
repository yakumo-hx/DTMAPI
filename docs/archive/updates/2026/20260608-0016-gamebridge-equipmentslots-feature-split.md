# 20260608-0016 GameBridge EquipmentSlots Feature Split

## Metadata

- Update ID: 20260608-0016
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Moved the EquipmentSlots feature implementation from the large `DolocTownExperimentalBridgeApi.cs` file into `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/`.
- Kept hook installation, hook callback methods, hook IDs, status strings, and log text unchanged.
- Moved only EquipmentSlots-owned lifecycle boundary handlers, API equip/unequip/recovery methods, sidecar storage helpers, native stat-function application, AccessoriesBar UI rendering/click/hover helpers, smoke state helpers, and EquipmentSlots-only nested runtime/storage classes.
- Left shared runtime dispatch in the main partial, including the `UpdateRuntimeAutomation` call sites.
- Left shared native item generation, generic Unity helpers, screenshot helper, inventory enumeration, and vector/reflection helpers in the main partial because other features still use them.
- Did not change EquipmentSlots behavior, Mine behavior, Oil behavior, Machine behavior, hook logic, smoke behavior, public API shape, or CameraZoom behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0016-gamebridge-equipmentslots-feature-split.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `git diff --check -- src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs src/DTMAPI.GameBridge.DolocTown/Features/EquipmentSlots/DolocTownExperimentalBridgeApi.EquipmentSlots.cs`
  - Only existing CRLF warning output was reported.
- Passed: `tools/scripts/build.ps1`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1`
  - Release build/test pass completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseNewContentApis -SkipBuild -TimeoutSeconds 300 -AutoExitAfterSecondsOverride 240`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-090109`
- Startup evidence: `StartupLog=true` in `result.json`.
- Third-save evidence: `summary.txt` records `SaveSlot=3`, and logs show `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`.
- EquipmentSlots registration evidence: logs show `MoreEquipmentSlots API register success=True reason=SaveLoaded slot=2 message=configured-experimental-player-ui-storage-stats-hook`.
- Hook patch evidence: logs show `Player.EquipmentSlotsApi = experimental. Patched native equipment stat refresh and AccessoriesBar lifecycle...`.
- UI render/bind evidence: logs show `Player.EquipmentSlotsApi = configured-experimental-player-ui-storage-stats-hook. reason=AccessoriesBar.__Init, hooks=True, rendered=3, occupied=0, interactive=3, hoverable=3, readOnly=false, attributeOnly=true, preserveVanillaVisualSlots=true`.
- Stats evidence: logs show `EquipmentSlots stats refresh owner=DTMAPI.MoreEquipmentSlotsMod extraSlots=3 stored=1 applied=1 preserveVanillaVisualSlots=True visualsFromExtraSlots=False refreshCount=1`.
- Passive equip/recover evidence: logs show `EquipmentSlots equip OK owner=DTMAPI.MoreEquipmentSlotsMod slot=dtmapi.more_equipment.1 item=grandmas_button ... backpack=2->1 applied=True` and `EquipmentSlots unequip OK ... item=grandmas_button recovered=1`.
- Hat equip/recover evidence: logs show `EquipmentSlots equip OK owner=DTMAPI.MoreEquipmentSlotsMod slot=dtmapi.more_equipment.1 item=straw_hat ... backpack=2->1 applied=True` and `EquipmentSlots unequip OK ... item=straw_hat recovered=1`.
- UI smoke evidence: logs show `EquipmentSlots UI evidence unavailable rendered=True ... occupied=1, interactive=3, hoverable=3`; screenshot capture was unavailable, but rendered/bound UI state was verified by the smoke.
- Smoke hook evidence: logs show `Smoke exercise NewContentEquipmentSlots OK passiveGive=grandmas_button 1->2 ... passiveRecover=1 ... hatGive=straw_hat 1->2 ... hatRecover=1 ... recoveredStored=0 ... status=configured-experimental-player-ui-storage-stats-hook` and `Smoke.NewContentEquipmentSlots = verified`.
- Exit evidence: `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`, `process-check.txt` says no `DolocTown.exe`, and `fatal-window-check.txt` says no fatal instance popup.

## Related Records

- Previous split slice: `docs/updates/2026/20260608-0015-gamebridge-machineproduction-feature-split.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-FEATURE-EQUIPMENTSLOTS-001`
- Existing new-content case: `docs/debug/regressions/smoke-matrix.md` row `MANUALQA-031-REGRESSION-NEWCONTENT`
- Existing equipment transaction case: `docs/debug/regressions/smoke-matrix.md` row `MANUALQA-029-README`
- API matrix: `docs/api/public-api-matrix.md` row for EquipmentSlots

## Rollback Notes

- Move `DolocTownExperimentalBridgeApi.EquipmentSlots.cs` contents back into `DolocTownExperimentalBridgeApi.cs`.
- No hook IDs, callback paths, status text, log text, public API semantics, EquipmentSlots behavior, Mine behavior, Oil behavior, Machine behavior, or CameraZoom behavior require rollback because this was a mechanical feature split.

## Follow-Up

- Continue moving one feature at a time into `Features/`.
- Keep EquipmentSlots behavior fixes, transaction/storage changes, Mine/Oil changes, and smoke harness changes separate from this mechanical split.
- Split Smoke and Diagnostics only after feature slices remain build- and smoke-clean.
