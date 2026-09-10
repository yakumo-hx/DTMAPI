# 20260610-0008 Hook Callback Safe Fallbacks

## Metadata

- Update ID: 20260610-0008
- Date: 2026-06-10
- Status: verified
- Source: User requested the midterm Refactor hardening route, starting with hook callback safe fallbacks.
- Owner: Codex

## Scope

- Add a reusable safety boundary for non-lifecycle Harmony callback paths in `DolocTownHookCallbacks`.
- Keep public APIs, hook IDs, hook status meanings, smoke result schema, and feature service behavior unchanged.
- Prefer native behavior when a callback fails:
  - postfix/result callback failure returns the original result,
  - prefix callback failure returns `true` so native logic runs,
  - void callback failure records diagnostics and does not throw back through Harmony/native code.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `tests/DTMAPI.UnitTests/DTMAPI.UnitTests.csproj`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0008-hook-callback-safe-fallbacks.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`

## Summary

- Added shared safe-fallback helpers for non-lifecycle hook callbacks:
  - `SafeResult<T>(...)`
  - `SafePrefix(...)`
  - `SafePostfix(...)`
- Wrapped the major non-lifecycle callback families that can otherwise leak exceptions into Harmony/native control flow:
  - ChestLocator inventory array postfix.
  - FishRoe item title/description/detail postfixes.
  - ActionSpeed enter and continuous-use callbacks.
  - ActionCompletion resource/oil tool-hit callbacks.
  - Fishing phase/minigame callbacks.
  - Motor, equipment, creative/debug, input-isolation, animal viewer, and StrongPlantingGun callbacks.
- Kept the existing lifecycle `SafeCallback(...)` path separate so lifecycle cleanup/restore evidence remains traceable under `20260610-0001`.
- Added unit coverage that invokes the private helper methods through reflection and verifies fallback values plus `DTMAPI.GameBridge.HookCallback` diagnostics.

## Validation

- `git diff --check` passed with only existing line-ending warnings.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `DTMAPI.UnitTests: OK`.
- DirectExe third-save smokes:
  - ChestLocator: `GAME-SMOKE/20260610-035216`, `ChestLocatorEnhancer=Passed`, clean exit.
  - FishRoe/AnimalViewer experimental hooks: `GAME-SMOKE/20260610-035326`, `ExperimentalHooks=Passed`, clean exit.
  - ActionSpeed: `GAME-SMOKE/20260610-035434`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, clean exit.
  - OneAction: `GAME-SMOKE/20260610-035639`, all four OneAction cases passed, clean exit.
  - AutoFishing: `GAME-SMOKE/20260610-035752`, `AutoFishingHotkey=Passed`, `AutoFishingPhase=Passed`, `AutoFishingMiniGameComplete=Passed`, clean exit.
- Final process check found no leftover `DolocTown.exe`.

## Evidence

- Smoke evidence directories:
  - `docs/debug/evidence/GAME-SMOKE/20260610-035216`
  - `docs/debug/evidence/GAME-SMOKE/20260610-035326`
  - `docs/debug/evidence/GAME-SMOKE/20260610-035434`
  - `docs/debug/evidence/GAME-SMOKE/20260610-035639`
  - `docs/debug/evidence/GAME-SMOKE/20260610-035752`
- Latest runtime report generated during this branch: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-035514.zip`.
- The later OneAction and AutoFishing smokes did not emit new report zips; their evidence is the collected smoke folder logs/results/process checks.

## Related Records

- `docs/updates/2026/20260610-0001-lifecycle-callback-isolation.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`

## Rollback Notes

- Revert this record and the `DolocTownHookCallbacks` safe-fallback wrappers if callback failures must temporarily fail hard during debugging.
- Keep the lifecycle callback isolation record separate; it covers save/title/agent-state boundary cleanup ordering, not ordinary hook prefix/postfix fallback policy.

## Follow-Up

- Continue the midterm route with feature-status publish throttling.
