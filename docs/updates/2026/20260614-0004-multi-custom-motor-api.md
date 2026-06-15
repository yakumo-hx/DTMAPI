# Multi Custom Motor API

Date: 2026-06-14

Status: implemented and game-smoke verified

## Source

- User request: rebuild the vehicle API and second vehicle mod toward stable multi custom motorcycles, starting from a new branch/worktree.
- Goal file: `docs/goals/2026/20260614-0001-multi-custom-motor-api.md`.
- Review records:
  - `docs/reviews/manual-qa/2026/20260614-0002-multi-custom-motor-api-review.md`
  - `docs/reviews/api/2026/20260614-0001-multi-custom-motor-native-owner-review.md`

## Summary

- Added `IMotorVehicleApi.RegisterCustomMotor` with `CustomMotorDefinition` so ordinary mods describe a native flying-motor clone without receiving raw Doloc Town types.
- Kept `RegisterSecondMotor` as an experimental compatibility wrapper, but normalized new custom motor defaults to 1x native speed while preserving the historical second-motor 2x fallback only for old callers.
- Added multi-key registration so several item ids can summon the same DTMAPI-managed motor instance.
- Rebuilt `SecondMotorMod` to use the new custom motor API, no longer request mail delivery, and expose player-facing status for phone-booth key acquisition and scoped appearance.
- Added official `mod_tbmodstoreextension.json` phone-booth stock for `dtmapi_second_motor_key`.
- Changed installer support to copy the official example motor sprite into the SecondMotor private package path instead of installing global replacement assets. Follow-up `20260615-0001` renamed that sample path away from `sprite_vehicle_motor*` to avoid official content scanner collisions.
- Hardened scoped appearance application so a reflected Unity `SpriteRenderer.sprite` failure records diagnostics and falls back to instance-scoped tint instead of aborting vehicle summon.
- Rewrote vehicle smoke assumptions for explicit save-slot 8/9 fixtures and removed synthetic cross-map teleport from the current vehicle smoke path.
- Bumped controlled DTMAPI version sources from `0.5.1-alpha` / `0.5.1.0` to `0.5.2-alpha` / `0.5.2.0`.

## Changed Files

- `Directory.Build.props`
- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/MotorVehicle/DolocTownExperimentalBridgeApi.MotorVehicle.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/VehicleSmoke.cs`
- `testmods/SecondMotorMod/**`
- `tools/release/dtmapi-mod-publish-zh.json`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/release-common.ps1`
- `tools/scripts/run-game-smoke.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- docs listed in this update.

## Validation

- `tools/scripts/build.ps1 -Configuration Release -SkipTests`: passed, 0 warnings / 0 errors.
- `tools/scripts/test.ps1 -Configuration Release`: passed, 0 warnings / 0 errors, `DTMAPI.UnitTests: OK`.
- New unit coverage verifies custom motor multi-key normalization and the split between new API 1x default speed and old compatibility 2x fallback.
- `git diff --check`: passed with line-ending warnings only.
- Save-slot 8 vehicle smoke: passed in `docs/debug/evidence/GAME-SMOKE/20260614-063456`, proving the native-unlocked fixture can give the custom key, apply the owner-private scoped sprite, summon/ride/dismount the DTMAPI motor, keep speed at native 1x, and exit cleanly.
- Save-slot 9 locked-native-vehicle smoke: passed in `docs/debug/evidence/GAME-SMOKE/20260614-063639`, proving the custom key/scoped-sprite/summon/ride path works while the official vehicle remains locked.
- Both vehicle smokes include clean `ProcessExited`, `NoFatalInstanceWindow`, and no-leftover-process checks.
- Follow-up: `docs/updates/2026/20260615-0001-second-motor-scoped-asset-clone-cleanup.md` records the post-smoke fix for global asset-name leakage and failed-clone cleanup; save-slot 8 `GAME-SMOKE/20260615-192916` and save-slot 9 `GAME-SMOKE/20260615-193040` reverified the renamed scoped sprite path.

## Evidence

- Build/test output in the Codex run for branch `codex/multi-custom-motor-api-20260614`.
- Game smoke evidence:
  - `docs/debug/evidence/GAME-SMOKE/20260614-063456`
  - `docs/debug/evidence/GAME-SMOKE/20260614-063639`

## Rollback

- Revert the new `RegisterCustomMotor` API additions, `SecondMotorMod` phone-booth package changes, private vehicle appearance asset installation, vehicle smoke fixture gate, and version bump together.
- Do not partially keep the phone-booth key without the GameBridge multi-key registration route; that would leave a purchasable item with unclear runtime ownership.

## Follow-Up

- Decide whether scoped sprite appearance is enough for StableCandidate definition/content APIs, or whether it must stay Experimental until a second independent vehicle mod proves the contract.
- Remove or further isolate remaining historical edge-transition smoke helpers if the new slot-8/slot-9 smoke path fully replaces them.
