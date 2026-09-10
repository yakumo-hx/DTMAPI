# 20260615-0008 Harmony Target Signature Round 3

Date: 2026-06-15
Status: verified-static
Branch: `codex/refactor-four-step-cleanup`

## Source Request

The user asked for the second-level cleanup branch to continue into maintainability refactoring after the SecondMotor/MotorVehicle removal, with sub-agent review after each step and rollback-friendly commits.

## Summary

Added an internal Harmony target-signature matcher so GameBridge hook installation can move away from fragile "method name + parameter count" selection toward checked declaring type, return type, and parameter type matching. Kept the legacy parameter-count entry points for compatibility, added signature overloads for ordinary methods, array-result postfixes, constructors, and closed generic targets, and migrated the first Save/Load hook group to the new signature path.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Hooking/HarmonyReflectionPatcher.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/debug/INDEX.md`
- `docs/updates/2026/20260615-0008-harmony-target-signature-round3.md`
- `docs/updates/INDEX.md`

## Details

- Added `HarmonyTargetSignature` with parameter-count compatibility plus exact matching by declaring type, return type, and parameter types.
- Added runtime type-name matching for game/native types that DTMAPI cannot compile against directly.
- Added signature overloads to `TryPatchPrefix`, `TryPatchPostfix`, `TryPatchArrayResultPostfix`, `TryPatchConstructorPostfix`, `TryPatchClosedGenericPrefix`, and `TryPatchClosedGenericPostfix`.
- Kept exact-signature failures strict: the signature path returns `false` instead of falling back to parameter-count patching.
- Migrated the `LoadGame` and `SaveGame` hook install group to the signature overloads as the first low-risk call-site.
- Added unit coverage for exact overload selection, wrong return-type rejection, wrong declaring-type rejection, legacy count lookup, and runtime type-name signatures.

## Validation

- Passed: `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\test.ps1 -Configuration Release` with 0 warnings, 0 errors, and `DTMAPI.UnitTests: OK`.
- Passed: `git diff --check` with line-ending warnings only.
- Passed: active `src` and `tests` search no longer finds active `IMotorVehicleApi`, MotorVehicle DTO names, `SecondMotor`, or `Vehicle.MotorApi` references.

No game smoke was run for this internal hook-matching utility step. More game smoke is required before migrating broader high-risk hooks such as fishing, chest locator, strong planting gun, or equipment shield attack hooks.

## Evidence And Related Records

- Debug index: `docs/debug/INDEX.md`
- Hook map: `docs/hook-map/README.md`
- Prior cleanup: `docs/updates/2026/20260615-0007-motorvehicle-api-retirement-round2.md`

## Rollback

Revert this commit to return `HarmonyReflectionPatcher` to parameter-count-only matching. If reverted after later hook migrations, also revert any call-sites that pass `HarmonyTargetSignature`.

## Follow-Up

- Add candidate-method diagnostics for signature mismatches before migrating large hook batches.
- Migrate high-risk hook groups in small batches with feature-specific game smoke evidence.
- Continue Round 3 with ConfigMenu callback/transaction isolation and pure structure splits after each smaller cut has tests.
