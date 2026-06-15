# 20260615-0007 MotorVehicle API Retirement Round 2

Date: 2026-06-15
Status: implemented-static
Branch: `codex/refactor-four-step-cleanup`

## Source Request

The user asked for Round 2 of the second-level cleanup branch to audit and remove SecondMotor-related DTMAPI code if it only served the failed archived sample, while keeping each cleanup round independently reviewable and committed.

## Summary

Retired the active MotorVehicle / SecondMotor API and GameBridge code after audit confirmed no active functional mod, release definition, publish JSON, active testmod, or current package source depends on it. The old SecondMotor sample remains archived for research, and `run-game-smoke.ps1 -AutoExerciseVehicle` remains a blocked compatibility command.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Diagnostics/DolocTownExperimentalBridgeApi.Diagnostics.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/MotorVehicle/DolocTownExperimentalBridgeApi.MotorVehicle.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/DebugConsoleSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/VehicleSmoke.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/scripts/run-game-smoke.ps1`
- `tools/scripts/README.md`
- `archive/second-motor-20260615/README.md`
- `docs/onboarding/current-state.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/reviews/code/2026/20260615-0002-motorvehicle-retirement-audit.md`
- `docs/updates/2026/20260615-0006-low-risk-workspace-cleanup-round1.md`
- `docs/updates/2026/20260615-0007-motorvehicle-api-retirement-round2.md`
- `docs/updates/INDEX.md`

## Details

- Removed `IMotorVehicleApi` plus `CustomMotorDefinition`, `SecondMotorOptions`, `MotorVehicleState`, `MotorVehicleRegisterResult`, `MotorVehicleSummonResult`, `MotorVehicleRideResult`, and `MotorVehicleEventArgs` from public abstractions.
- Removed MotorVehicle runtime registration from `DolocTownGameBridge`.
- Removed MotorVehicle state fields, lifecycle cleanup, update snapshots, and implementation from `DolocTownExperimentalBridgeApi`.
- Removed native motor/riding/room-entry hook installation and callback methods for `ItemMotorKey`, `MotorInteractable`, `AgentControllerState.GetOnMotor/GetOffMotor`, `MotorController.OnFixedUpdate`, `DolocAPI.UnlockMotor`, `DolocAPI.SetMotorPosition`, and `DolocAPI.EnterRoom`.
- Removed `Vehicle.MotorApi` status publication and SecondMotor cleanup callbacks from save/title lifecycle.
- Removed the vehicle smoke case and active vehicle smoke scheduling, while keeping the external `-AutoExerciseVehicle` script flag blocked for compatibility.
- Removed retired native motor types from hook target diagnostics so future type-resolution reports do not imply active vehicle hook ownership.
- Marked old SecondMotor/MotorVehicle smoke-matrix rows as historical/superseded so they cannot be mistaken for current acceptance.
- Added an API-matrix migration note that there is no replacement vehicle API in this release and old experimental mods should remove retired type references.
- Removed MotorVehicle-only unit coverage.
- Moved `ContainsIgnoreCase` / `ContainsAny` smoke helpers out of the deleted vehicle smoke file into `SmokeHarness` because content/oil/debug smoke cases also use them.
- Updated current-state docs, API matrix, hook map, debug index, smoke matrix, archive README, and script README so future sessions see the API as retired/removed rather than retained Experimental research.

## Validation

- Passed: `tools/scripts/test.ps1` with 0 warnings, 0 errors, and `DTMAPI.UnitTests: OK`.
- Passed: active `src` and `tests` search no longer finds `IMotorVehicleApi`, MotorVehicle DTO names, `SecondMotor`, or `Vehicle.MotorApi` implementation references.
- Passed: PowerShell AST parser check for `tools/scripts/run-game-smoke.ps1`.
- Passed: `git diff --check` with line-ending warnings only.
- Passed: three read-only sub-agent reviews found no P0/P1 blockers; reported diagnostics and documentation ambiguity issues were fixed before commit.

No game smoke was run for this static removal round. Runtime vehicle behavior is intentionally removed, not repaired. Future packaging smoke should rebuild/install packages before checking generated payloads because ignored `dist/` and already-installed local DTMAPI package copies may lag behind source.

## Evidence And Related Records

- Review record: `docs/reviews/code/2026/20260615-0002-motorvehicle-retirement-audit.md`
- Prior archive update: `docs/updates/2026/20260615-0004-second-motor-archive.md`
- Native-owner review: `docs/reviews/api/2026/20260614-0001-multi-custom-motor-native-owner-review.md`
- API matrix: `docs/api/public-api-matrix.md`
- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`

## Rollback

Revert this commit if the retired API must be restored for deliberate vehicle research. Do not restore only the public DTOs or only the GameBridge implementation; the API, hooks, smoke, docs, package definitions, and manual QA plan must return together on a fresh research branch.

## Follow-Up

- Later packaging/install work should confirm generated local packages no longer carry stale `DTMAPI_SecondMotor` payloads from ignored build artifacts.
- Future vehicle work should begin with a new native-owner slice and fresh public contract instead of reviving `IMotorVehicleApi`.
