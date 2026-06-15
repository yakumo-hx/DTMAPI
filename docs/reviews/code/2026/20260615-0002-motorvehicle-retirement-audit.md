# 20260615-0002 MotorVehicle Retirement Audit

Date: 2026-06-15
Status: implementation-reviewed
Branch: `codex/refactor-four-step-cleanup`

## Scope

Round 2 of the cleanup branch reviewed whether the remaining MotorVehicle / SecondMotor API surface should stay active after `SecondMotorMod` was archived.

## Findings

1. Public contract exposure existed only as Experimental code.

   `IMotorVehicleApi` and its DTOs were still public in `DTMAPI.Abstractions`, but the API matrix and native-owner review never promoted them beyond Experimental. The only real consumer was the archived `SecondMotorMod`; no active shipped/local functional mod, release manifest, publish JSON, or active testmod depended on the API.

2. Runtime code was broader than the archived sample.

   The active GameBridge still registered `IMotorVehicleApi`, implemented `RegisterCustomMotor` / `RegisterSecondMotor`, installed native motor hooks, published `Vehicle.MotorApi`, ran SecondMotor lifecycle cleanup on save/title boundaries, and compiled vehicle smoke/unit coverage. Deleting only the archived sample would leave active runtime hooks for a rejected route.

3. Hook and smoke cleanup had to be all-or-nothing.

   Runtime audit found compile/runtime risk if only `Features/MotorVehicle` was deleted. Related references included API registration, `DolocTownHookCallbacks` vehicle callbacks, `AllHookTargetsReady`, `Smoke.VehicleSecondMotor`, the vehicle smoke body, and `MotorVehicleCustomApiNormalizesKeysAndNativeSpeedDefaults`.

4. Package/source consumers were clean, but generated payloads may lag.

   Active source/release definitions no longer include `SecondMotorMod`. Ignored `dist/` payloads and already-installed local DTMAPI packages may still contain old generated files until packages are rebuilt/installed from current source, so they must not be used as source truth.

## Decision

Retire and remove the active MotorVehicle public API/runtime surface instead of keeping it as Experimental research code. Preserve the archived sample and old smoke rows as historical/native-owner research only.

## Implementation Requirements

- Remove `IMotorVehicleApi` and MotorVehicle DTOs from `DTMAPI.Abstractions`.
- Remove GameBridge registration and `DolocTownExperimentalBridgeApi` implementation/fields.
- Remove native motor hook installation, callback methods, and `Vehicle.MotorApi` status publication.
- Remove SecondMotor lifecycle cleanup from SaveLoaded/ReturnedToTitle callbacks.
- Remove vehicle smoke body and active vehicle smoke scheduling.
- Remove MotorVehicle-only unit coverage.
- Keep `run-game-smoke.ps1 -AutoExerciseVehicle` as a blocked archived compatibility command.
- Update API matrix, hook map, debug index, smoke matrix, onboarding, archive notes, and update records.

## Validation Expectations

- `rg` over active `src` and `tests` should find no `IMotorVehicleApi`, MotorVehicle DTO, SecondMotor, or `Vehicle.MotorApi` implementation references.
- `tools/scripts/test.ps1` should pass with 0 warnings and 0 errors.
- Game smoke is not required for this static removal round, but future packaging/install checks must ensure generated payloads no longer reintroduce `DTMAPI_SecondMotor`.

## Related Records

- Archive update: `docs/updates/2026/20260615-0004-second-motor-archive.md`
- Native-owner review: `docs/reviews/api/2026/20260614-0001-multi-custom-motor-native-owner-review.md`
- Smoke matrix row: `VEHICLE-001-ARCHIVED-20260615`
