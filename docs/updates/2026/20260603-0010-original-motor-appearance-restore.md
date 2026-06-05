# 20260603-0010 Original Motor Appearance Restore

## Source Request

User reported a small SecondMotor regression: restore the original motor appearance because the two motors currently look identical.

## Summary

- Stopped installing the official Workshop example `sprite_vehicle_motor*` replacement assets into the `DTMAPI_SecondMotor` official-local package.
- Added installer cleanup for the stale `Content/DTMAPI/official-vehicle-example` folder so old global replacement assets are removed on reinstall.
- Switched `DTMAPI.SecondMotorMod` to request custom visuals through `SecondMotorOptions.UseOriginalMotorVisuals = false`.
- Added GameBridge instance-scoped clone tinting through reflected `SpriteRenderer.color`, skipping the driver renderer so the player/rider sprites are not tinted.
- Added a third-save vehicle smoke appearance probe that fails if the original motor receives the second-motor scoped tint or if the second clone does not.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `testmods/SecondMotorMod/ModEntry.cs`
- `testmods/SecondMotorMod/README.md`
- `tools/scripts/install-to-game.ps1`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\scripts\build.ps1 -Configuration Release`
- Result: all projects and `DTMAPI.UnitTests` built with `0` warnings and `0` errors.
- Passed: `powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\scripts\run-game-smoke.ps1 -UseSteam -SaveSlot 3 -AutoExerciseVehicle -SkipBuild -TimeoutSeconds 240 -AutoExitAfterSecondsOverride 80`
- Result: third-save Steam smoke passed with `VehicleSecondMotor=true`, `SaveLoaded=true`, `ProcessExited=true`, `NoFatalInstanceWindow=true`, and no leftover `DolocTown.exe`.
- Local install check: `DTMAPI_SecondMotor/Content/DTMAPI/official-vehicle-example` is absent, `vehicle-appearance/official-vehicle-assets.txt` exists, and no `sprite_vehicle_motor*` files remain under `DTMAPI_SecondMotor/Content/DTMAPI`.

## Evidence Links

- Vehicle appearance smoke: `docs/debug/evidence/GAME-SMOKE/20260603-082749`.
- Installer output: smoke stdout records `Skipped global official vehicle example sprite assets for SecondMotor; removed stale assets from ...\DTMAPI_SecondMotor\Content\DTMAPI\official-vehicle-example`.
- Vehicle logs: `DTMAPI-latest.log` records `Vehicle.SecondMotorAppearance = experimental`, `appearance=instance-scoped-tint hex=#8CE6FF renderers=5/10 skippedDriver=5`, and the smoke probe `appearanceIsolated=True, originalScopedTint=0/10, originalSkippedDriver=5, secondScopedTint=5/10, secondSkippedDriver=5`.
- Vehicle behavior logs: same run records `Smoke.VehicleOriginalMotorKey = verified`, `Second motor key intercepted item=dtmapi_second_motor_key success=True`, `dualVisible=True`, `Second motor ride-on observed`, `Second motor ride-off restored original AgentControllerState.motorController and original motor snapshot`, `Motor vehicle original summon OK`, `speedMultiplier=2`, `baseMaxSpeed=25`, `effectiveMaxSpeed=50`, and unchanged endurance.
- Vehicle result: `result.json` has `VehicleSecondMotor=true`, `SaveLoaded=true`, `ProcessExited=true`, and `NoFatalInstanceWindow=true`.

## Known Facts And Rejected Hypotheses

- The decompiled `MotorController` renders through its own `SpriteRenderer` collection; the public motor API still has no scoped sprite adapter for a second vehicle.
- The official Workshop vehicle example assets use global `sprite_vehicle_motor` keys. Installing those files under an enabled official-local package can replace the original motor appearance too, so copying them is not safe for a second-motor-only skin.
- The installer previously updated files in place and did not delete the stale official vehicle example asset folder. Merely stopping future copies would not restore already-installed local packages.
- The new tint is intentionally a narrow interim visual: it proves second-motor-only rendering without touching raw game types or distributing copied official sprite assets. A real scoped sprite adapter remains future work.

## Rollback

- Revert this record and the changed files listed above.
- If rolling back only the visual tint, set `UseOriginalMotorVisuals = true` in `SecondMotorMod` and remove `Vehicle.SecondMotorAppearance` smoke assertions.
- Do not restore global `sprite_vehicle_motor*` asset copying unless accepting that the original Doloc Town motor may be visually replaced by the same package.

## Follow-up

- Replace the interim tint with a scoped sprite/animation adapter once GameBridge can bind per-instance motor sprites without global official asset replacement.
- Keep the appearance probe in VEHICLE-001 so future vehicle asset changes must prove original/second isolation.
