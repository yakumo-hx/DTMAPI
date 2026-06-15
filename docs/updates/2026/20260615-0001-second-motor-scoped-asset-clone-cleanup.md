# Second Motor Scoped Asset Clone Cleanup

Date: 2026-06-15

Status: verified

## Source

- User feedback from vehicle worktree testing: the second motor key summon failed with `NullReferenceException`, the original motor appearance was replaced, and failed summon attempts could leave repeated motor stickers/objects across saves.
- Goal context: `docs/goals/2026/20260614-0001-multi-custom-motor-api.md`.
- Native-owner/API review: `docs/reviews/api/2026/20260614-0001-multi-custom-motor-native-owner-review.md`.

## Summary

- Changed the SecondMotor appearance asset path from the official global replacement names to DTMAPI-owned private names under `Content/DTMAPI/assets/second-motor/`.
- Updated the installer to remove stale `Content/DTMAPI/official-vehicle-example` and `Content/DTMAPI/vehicle-appearance` folders before copying private renamed assets.
- Hardened second-motor clone creation so GameBridge first tries to rebind cloned native fields (`rb`, `driverRenderer`, `motorLight`, `motorInteractable`) to clone-owned components and only invokes native `Init()` if rebinding cannot prove clone ownership.
- Added `motorRenderer`, `scannerGate`, `scannerInteractable`, and native `enduranceProgressCircle` ownership checks after the first follow-up smoke proved `MotorController.Reset()` can still fail when the cloned controller lacks its own UI progress circle.
- Tracks the clone immediately after creation so failed `Init`/`Reset`/`SetVisible`/appearance stages run DTMAPI cleanup instead of leaving orphaned GameObjects.
- Added a best-effort orphan sweep for `DTMAPI.SecondMotor.*` GameObjects at vehicle lifecycle boundaries and before creating a fresh clone.
- Relaxed `SecondMotorMod` key-source checks so the local development package and future Workshop source ids can both pass while disabled/missing content remains blocked.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/MotorVehicle/DolocTownExperimentalBridgeApi.MotorVehicle.cs`
- `testmods/SecondMotorMod/ModEntry.cs`
- `testmods/SecondMotorMod/README.md`
- `tools/scripts/install-to-game.ps1`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Validation

- `git diff --check`: passed with line-ending warnings only.
- `tools/scripts/build.ps1 -Configuration Release`: passed, 0 warnings / 0 errors, `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release`: passed, 0 warnings / 0 errors, `DTMAPI.UnitTests: OK`.
- Installed local runtime/package verification: source and installed GameBridge DLL hashes matched; source and installed SecondMotor DLL hashes matched; installed `DTMAPI_SecondMotor` had `OldGlobalAssetCount=0` and five private assets under `Content/DTMAPI/assets/second-motor`.
- Retained failed smoke `GAME-SMOKE/20260615-191720`: useful diagnosis showing the clone reached `ensure-stage=reset` because `enduranceProgressCircle` still required native init ownership.
- Save-slot 8 vehicle smoke `GAME-SMOKE/20260615-192916`: passed. Evidence includes custom key summon, repeated key same-vehicle summon, scoped sprite path `Content/DTMAPI/assets/second-motor/dtmapi_second_motor.png`, `appearanceIsolated=True`, `originalScopedTint=0/10`, ride/dismount, native 1x speed, `ProcessExited`, and `NoFatalInstanceWindow`.
- Save-slot 9 locked-native-vehicle smoke `GAME-SMOKE/20260615-193040`: passed. Evidence includes `nativeUnlockedBefore=False`, `nativeUnlockedAfterKey=False`, `nativeVisibleAfterKey=False`, custom motor summon/ride/dismount, scoped sprite isolation, native 1x speed, `ProcessExited`, and `NoFatalInstanceWindow`.

## Rollback

- Revert this update together with the SecondMotor scoped asset path change and installer cleanup. Do not keep the new private path without the installer rename/copy change, or the mod will fall back to tint because the scoped sprite file will be missing.

## Follow-Up

- Keep the new `ensure-stage=...` message in DTMAPI errors so future failures identify whether the failure is native init, reset, visibility, interactable resolution, or scoped appearance.
