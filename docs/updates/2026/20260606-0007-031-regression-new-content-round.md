# Update 20260606-0007: 0.3.1 Regression and New Content Round

- Date: 2026-06-06
- Status: implemented
- Area: version/gamebridge/ui/input/vehicle/content/machine/equipment/smoke
- Source request: `/goal` to turn the next DTMAPI version into a manual regression fix plus new-content Mod development round, preserving the new 4-item feedback as newer than 0.2.3/0.3.0 smoke evidence.
- Version: 0.3.0 -> 0.3.1.

## Changed Files

- Version/manifests: `Directory.Build.props`, `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`, `tools/scripts/install-to-game.ps1`, official-local `manifest.json` / `official-info.json` files for controlled DTMAPI mods.
- Animal UI: `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`, `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`, `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`.
- Y console smoke/input: `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`, `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`, `tools/scripts/run-game-smoke.ps1`.
- AutoFishing/ActionSpeed: `testmods/AutoFishingMod/ModEntry.cs`, `testmods/AutoFishingMod/README.md`.
- Mine/Oil/Equipment content: `testmods/MineMod/ModEntry.cs`, `testmods/MineMod/README.md`, `testmods/MineMod/i18n/english.json`, `testmods/MineMod/i18n/schinese.json`, `testmods/README.md`.
- Review/task/docs: `readme.md`, `docs/reviews/manual-qa/2026/20260606-0004-031-regression-new-content-review.md`, docs indexes/matrices.

## Summary

- Recorded the new 0.3.1 manual-QA round separately from the prior 0.2.3/0.3.0 verified claims.
- Added `AnimalViewer.Show` Prefix preparation so hidden-produce progress rows are prefilled before the viewer becomes visible, avoiding the brief native mood-text flash.
- Kept Animal color config behavior as staged/pending-aware: ordinary presets hide the hex input, while Custom/`+` shows it.
- Split AutoFishing `AutoCompleteMiniGame` and `SkipMiniGame` behavior so skip no longer depends on auto-complete, while the skip-false path still auto-completes the native minigame.
- Added an in-game Y-console hotkey smoke path through DTMAPI input plus the DebugConsole host state machine, avoiding flaky external key injection while preserving Y/Escape/Y/short-tap/held-Y evidence.
- Made vehicle smoke temporarily enable `Local.DTMAPI_SecondMotor` and restore the previous official enablement state after the run.
- Changed MineMod into a hybrid fuel/electric machine: fuel capacity 7200, fuel-only cost 120, electric-mode fuel cost 20, electric power cost 10, default electric mode, and official JSON/tech/recipe validation updated accordingly.
- Revalidated Oil `crude_oil`, Mine official JSON/runtime production, and MoreEquipmentSlots attribute-only extra slots with recovery.

## Validation

- Release build/unit: `tools/scripts/build.ps1 -Configuration Release` passed with 0 errors. Only NU1900 warnings were emitted because package vulnerability data could not be fetched from `https://api.nuget.org/v3/index.json` under restricted network.
- Title config smoke: `GAME-SMOKE/20260606-150928` passed and captured ActionSpeed, AutoFishing, Animal preset, and Animal Custom/`+` config screenshots.
- Third-save Animal smoke: `GAME-SMOKE/20260606-150721` passed with `AnimalViewerUi=true`, `independent cloned ProgressBar prefilled rows=1`, `moodOverride=False`, and `stateDescriptionOverride=False`.
- Third-save ActionSpeed smoke: `GAME-SMOKE/20260606-150543` passed with config apply `2 -> 4`, tool animation `body/tool-renderer/tool-collider`, and interaction/auto-fill evidence.
- Third-save AutoFishing smoke: `GAME-SMOKE/20260606-150631` passed with F6 input through DTMAPI input service, movement cancel, native auto-cast, fishing phase, and `AutoFishingMiniGameComplete=true`.
- Third-save Y console/debug smoke: `GAME-SMOKE/20260606-150210` passed with `InstantSave=true`, `DebugTeleportCsv=true`, `DebugTeleport=true`, `DebugInventory=true`, `DebugWeather=true`, `DebugTime=true`, `DebugMovement=true`, and `DebugConsoleHotkey=openCount=8, escapeCloseCount=1, yCloseCount=6, shortTaps=10, holdNoFlicker=True`.
- Third-save SecondMotor smoke: `GAME-SMOKE/20260606-150357` passed with dual-visible original/second motors, instance-scoped appearance, key summon, ride/dismount, edge transition, original not at the new map entry, `noStuck=True`, and official enablement restored afterward.
- Third-save new-content smoke: `GAME-SMOKE/20260606-150834` passed with Oil metadata/drop, Mine official JSON hybrid machine summary, Mine production `mode=electric fuelCost=20 fuelRemaining=7180/7200 electricPowerCost=10`, and MoreEquipmentSlots equip/recover evidence.
- Exit checks: all final process checks report no `DolocTown.exe`; no fatal popup windows were recorded.

## Evidence Links

- Review: `docs/reviews/manual-qa/2026/20260606-0004-031-regression-new-content-review.md`
- Smoke: `docs/debug/evidence/GAME-SMOKE/20260606-150210`
- Smoke: `docs/debug/evidence/GAME-SMOKE/20260606-150357`
- Smoke: `docs/debug/evidence/GAME-SMOKE/20260606-150543`
- Smoke: `docs/debug/evidence/GAME-SMOKE/20260606-150631`
- Smoke: `docs/debug/evidence/GAME-SMOKE/20260606-150721`
- Smoke: `docs/debug/evidence/GAME-SMOKE/20260606-150834`
- Smoke: `docs/debug/evidence/GAME-SMOKE/20260606-150928`

## Rollback Notes

- Revert the version bump and manifest version changes together if 0.3.1 packaging must be withdrawn.
- Revert the DebugConsole in-game smoke plumbing by removing the optional DebugConsole host dependency from `DolocTownGameBridge`; player-facing Y console logic remains in `DebugConsoleMod`.
- Revert Mine hybrid defaults by restoring MineMod config defaults and `MachineDefinition` fuel/electric fields; keep Oil and EquipmentSlots changes independent.
- The smoke script's SecondMotor enablement change is temporary and restores `mod_infos.json`; it should not be converted into product behavior that overrides user official Mod settings.

## Follow-Up

- Keep `IDebugConsoleApi`, `IMotorVehicleApi`, `IMachineProductionApi`, and `IEquipmentSlotsApi` experimental until additional non-test mods depend on the current contracts.
- Manual visual acceptance can still inspect the SecondMotor summon animation and title config screenshots, but the automated third-save requirements for this round are smoke-proven.
