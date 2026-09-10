# 20260608-0017 GameBridge MotorVehicle Feature Split

## Metadata

- Update ID: 20260608-0017
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Moved the MotorVehicle feature implementation from the large `DolocTownExperimentalBridgeApi.cs` file into `src/DTMAPI.GameBridge.DolocTown/Features/MotorVehicle/`.
- Kept hook installation, hook callback methods, hook IDs, status strings, and log text unchanged.
- Moved only MotorVehicle-owned API methods, smoke helper entry points, hook callback targets, second-motor lifecycle cleanup, room-transition sync, second-motor clone/tint/ride helpers, original motor snapshot helpers, state builders, and MotorVehicle-only nested runtime classes.
- Left shared runtime dispatch in the main partial, including the `UpdateRuntimeAutomation` call site.
- Left shared native item generation, generic Unity clone/destroy helpers, screenshot helper, inventory enumeration, vector/reflection helpers, and runtime state fields in the main partial because other feature partials or shared dispatch still use them.
- Did not change MotorVehicle behavior, hook logic, smoke behavior, public API shape, CameraZoom behavior, Machine behavior, EquipmentSlots behavior, or Oil behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/MotorVehicle/DolocTownExperimentalBridgeApi.MotorVehicle.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0017-gamebridge-motorvehicle-feature-split.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `git diff --check -- src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs src/DTMAPI.GameBridge.DolocTown/Features/MotorVehicle/DolocTownExperimentalBridgeApi.MotorVehicle.cs`
  - Only existing CRLF warning output was reported.
- Passed: `tools/scripts/test.ps1`
  - Release build/test pass completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseVehicle -SkipBuild -TimeoutSeconds 300 -AutoExitAfterSecondsOverride 240`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-091119`
- Startup evidence: `DTMAPI-latest.log` starts with `DTMAPI runtime starting.`, and `result.json` records `StartupLog=true`.
- Third-save evidence: `summary.txt` records `SaveSlot=3`, and logs show `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`.
- Hook install evidence: logs show `Vehicle.MotorApi = experimental. Patched native motor key, riding, tuning, unlock, position, and room-entry touchpoints. Second-motor routing remains experimental and must be smoke-verified.`
- Registration evidence: logs show `DTMAPI.SecondMotorMod` registered the second motor on `SaveLoaded` and `Vehicle.SecondMotorRegistration = experimental`.
- Key evidence: logs show `Smoke.VehicleOriginalMotorKey = verified`, `Vehicle.SecondMotorKey = experimental`, and `Smoke.VehicleSecondMotorKey = verified`.
- Appearance evidence: logs show `Vehicle.SecondMotorAppearance = experimental. appearanceIsolated=True, originalScopedTint=0/10, secondScopedTint=5/10`.
- Ride evidence: logs show `Second motor ride-on observed vehicle=dtmapi.second_motor`, `Vehicle.SecondMotorRide = experimental`, and `ride=True`.
- Edge-transition evidence: logs show `Smoke.VehicleSecondMotorEdgeTransition = verified` with `changedRoom=True`, `secondStillRiding=True`, `secondInCurrentRoom=True`, `nearDestination=True`, `originalVisibleAfterTransition=False`, `originalAtNewEntry=False`, and `noStuck=True`.
- Dismount/restore evidence: logs show `Vehicle.SecondMotorRideOff = experimental`, `Vehicle.MotorDismount = experimental`, and `Vehicle.OriginalMotorSummon = experimental`.
- Smoke hook evidence: logs show `Smoke.VehicleSecondMotor = verified` with `dualVisibleAfterKey=True`, `appearanceIsolated=True`, `ride=True`, `dismount=True`, `speedMultiplier=2`, `baseMaxSpeed=25`, and `effectiveMaxSpeed=50`.
- Exit evidence: `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`, `process-check.txt` says no `DolocTown.exe`, and `fatal-window-check.txt` says no fatal instance popup.

## Related Records

- Previous split slice: `docs/updates/2026/20260608-0016-gamebridge-equipmentslots-feature-split.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-FEATURE-MOTORVEHICLE-001`
- Existing vehicle case: `docs/debug/regressions/smoke-matrix.md` row `VEHICLE-001`
- API matrix: `docs/api/public-api-matrix.md` row for MotorVehicle

## Rollback Notes

- Move `DolocTownExperimentalBridgeApi.MotorVehicle.cs` contents back into `DolocTownExperimentalBridgeApi.cs`.
- No hook IDs, callback paths, status text, log text, public API semantics, MotorVehicle behavior, smoke harness behavior, Machine behavior, EquipmentSlots behavior, Oil behavior, or CameraZoom behavior require rollback because this was a mechanical feature split.

## Follow-Up

- Continue the mechanical split with Smoke and Diagnostics only after feature slices remain build- and smoke-clean.
- Keep MotorVehicle behavior fixes, disabled-room behavior, key/mail behavior, and transition tuning separate from this mechanical split.
