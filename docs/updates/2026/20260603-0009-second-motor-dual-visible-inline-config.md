# 20260603-0009 Second Motor Dual Visible Inline Config

## Source Request

Continue the DTMAPI 0.2.3 manual-QA productization goal from `readme.md`, specifically the remaining blockers after `20260603-0008`: prove the original motor and DTMAPI second motor can be visible after their respective key paths, and finish the same-row secondary config controls for ActionSpeed and AutoFishing.

## Summary

- Added an internal smoke-only original motor key exercise path that generates native `motor_key`, invokes `ItemMotorKey.OnUse`, and verifies the base-game motor key route before using the DTMAPI alternate key.
- Tightened `TryExerciseVehicleForSmoke` so dual-visible original/second motor state is a hard assertion instead of a soft note.
- Added experimental `IDtmConfigMenuApi.AddInlineBoolBoolOption` and a registry/title-menu renderer for same-row primary/secondary bool controls.
- Switched ActionSpeed `自动装水` / `强化自动装水` and AutoFishing `自动完成小游戏` / `跳过小游戏` to the new same-row config control.
- Extended title-settings smoke evidence to capture ActionSpeed, AutoFishing, AnimalHusbandryProgress, and SecondMotor config/status pages in one run.
- Fixed the title screenshot state machine so it continues through the SecondMotor page instead of stopping before stage 7.

## Changed Files

- `src/DTMAPI.Abstractions/ConfigMenu.cs`
- `src/DTMAPI.ModConfigMenu/ConfigMenuRegistry.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedTitleMenuSettingsUi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `testmods/ActionSpeedMod/ModEntry.cs`
- `testmods/AutoFishingMod/ModEntry.cs`
- `tools/scripts/run-game-smoke.ps1`
- `readme.md`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\scripts\build.ps1 -Configuration Release`
- Result: all projects and `DTMAPI.UnitTests` built with `0` warnings and `0` errors.
- Passed: `tools/scripts/run-game-smoke.ps1 -UseSteam -SaveSlot 3 -AutoExerciseVehicle -SkipBuild -TimeoutSeconds 240 -AutoExitAfterSecondsOverride 80`
- Result: third-save Steam smoke passed with `VehicleSecondMotor=true`, `SaveLoaded=true`, clean exit, no fatal popup, and no leftover `DolocTown.exe`.
- Passed: `tools/scripts/run-game-smoke.ps1 -UseSteam -SaveSlot 0 -AutoOpenTitleSettingsMenu -SkipBuild -TimeoutSeconds 260 -AutoExitAfterSecondsOverride 120`
- Result: title-settings Steam smoke passed and captured ActionSpeed, AutoFishing, AnimalHusbandryProgress, and SecondMotor config/status screenshots; visual inspection found the same-row bool controls and no obvious text/control overlap.

## Evidence Links

- Final vehicle smoke: `docs/debug/evidence/GAME-SMOKE/20260603-051204`.
- Vehicle logs: `DTMAPI-latest.log` records `Smoke.VehicleOriginalMotorKey = verified`, `Second motor key intercepted item=dtmapi_second_motor_key success=True`, `Smoke exercise VehicleSecondMotor dual-visible probe originalVisible=True originalRoom=farm_type1-平地 secondVisible=True secondRoom=farm_type1-平地 dualVisible=True`, `Second motor ride-on observed`, `Second motor ride-off restored original AgentControllerState.motorController and original motor snapshot`, `Motor vehicle original summon OK`, and `Smoke exercise VehicleSecondMotor OK ... originalVisibleAfterKey=True, secondVisibleAfterKey=True, dualVisibleAfterKey=True`.
- Vehicle result: `result.json` has `VehicleSecondMotor=true`, `SaveLoaded=true`, `ProcessExited=true`, and `NoFatalInstanceWindow=true`.
- Vehicle exit evidence: `process-check.txt` says no `DolocTown.exe`.
- Final title-settings smoke: `docs/debug/evidence/GAME-SMOKE/20260603-052444`.
- Config/status screenshots: `DTMAPI-evidence/UI-004/20260603-052523/title-settings-config-action-speed.png`, `title-settings-config-auto-fishing.png`, `title-settings-config-animal-husbandry-progress.png`, and `title-settings-config-second-motor.png`.
- Title-settings summary: `DTMAPI-evidence/UI-004/20260603-052523/summary.txt` records all four `ConfigPageUniqueId` screenshots.
- Title-settings exit evidence: `process-check.txt` says no `DolocTown.exe`.

## Known Facts And Rejected Hypotheses

- `GAME-SMOKE/20260603-050208` failed the old dual-visible probe because the original motor was still in `city_郊区-码头小径` while the player and second motor were on `farm_type1-平地`. That proved the previous smoke was insufficient; it did not prove simultaneous visibility was impossible.
- The corrected smoke first summons the original motor through the native original key route in the current outdoor room, then summons the DTMAPI second motor through the alternate key. In that state, both visibility probes report true.
- Native motor state remains singleton-oriented around `DolocAPI.Motor` and `AgentControllerState.motorController`, so `IMotorVehicleApi` remains experimental even though the requested 0.2.3 dual-visible proof now passes.
- The reflected title settings UI is the player-facing config host. The older dev-only IMGUI overlay is not promoted as evidence for the new inline bool/bool control.

## Rollback

- Revert this record and the changed files listed above.
- If rolling back only the config control, replace ActionSpeed/AutoFishing same-row registrations with separate bool rows and remove `AddInlineBoolBoolOption` from the public config API.
- If rolling back only the vehicle smoke strictness, remove the smoke-only original-key helper and restore the old soft dual-visible summary, but keep `20260603-050208` marked as a historical blocker.

## Follow-up

- Keep monitoring dual-motor behavior during manual play because the public motor API still relies on fragile routing/restoration around singleton native motor state.
- Consider adding a dev-overlay fallback renderer for `InlineBoolBool` only if the old diagnostics overlay becomes a supported player-facing config route again.
