# 20260610-0016 Hook Callback Failure Throttle

## Status

Verified.

## Source Request

User requested the post-midterm Refactor follow-up route, starting with operation-level throttling for repeated hook callback failures.

## Summary

- Added operation-level throttling to `DolocTownHookCallbacks.RecordHookCallbackFailure`.
- Kept the first failure for each operation as a full `DTMAPI.GameBridge.HookCallback` diagnostics error.
- Kept the next two repeated failures as short runtime-monitor warnings, then suppressed repeated logging until a 30-second summary window is reached.
- Preserved existing `SafeResult<T>`, `SafePrefix`, and `SafePostfix` fallback behavior: result callbacks return the fallback/original value, prefix callbacks allow native logic by default, and postfix callbacks do not throw back through Harmony/native code.
- Did not add public API, change hook IDs, auto-unhook callbacks, or change smoke result schemas.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Unit coverage verifies safe fallback values and confirms repeated failures for the same operation produce only one diagnostics error.
- DirectExe third-save smokes passed:
  - ChestLocator: `GAME-SMOKE/20260610-092620`, `ChestLocatorEnhancer=Passed`.
  - FishRoe/experimental hooks: `GAME-SMOKE/20260610-092819`, `ExperimentalHooks=Passed`.
  - ActionSpeed: `GAME-SMOKE/20260610-092928`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, and `ActionSpeedInteraction=Passed`.
  - OneAction: `GAME-SMOKE/20260610-093037`, all four OneAction cases passed.
  - AutoFishing: `GAME-SMOKE/20260610-093148`, `AutoFishingHotkey=Passed`, `AutoFishingInputLog=Passed`, `AutoFishingMovementCancel=Passed`, `AutoFishingPhase=Passed`, and `AutoFishingMiniGameComplete=Passed`.
- All smoke result folders record `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- `process-check.txt` says no `DolocTown.exe`; `fatal-window-check.txt` says no fatal instance popup.
- The five smoke logs contain no `Hook callback failed`, `Repeated hook callback failure`, or `Throttled hook callback failures` entries.

## Evidence Links

- Hook map: `docs/hook-map/README.md#diagnostic-hookcallbackfailurethrottle`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Runtime evidence folders:
  - `docs/debug/evidence/GAME-SMOKE/20260610-092620`
  - `docs/debug/evidence/GAME-SMOKE/20260610-092819`
  - `docs/debug/evidence/GAME-SMOKE/20260610-092928`
  - `docs/debug/evidence/GAME-SMOKE/20260610-093037`
  - `docs/debug/evidence/GAME-SMOKE/20260610-093148`

## Rollback

- Remove the hook callback failure state dictionary and publication helper from `DolocTownHookCallbacks`.
- Restore `RecordHookCallbackFailure` to always write a diagnostics error and full runtime-monitor error for every callback failure.
- Restore the previous unit-test assertion count.

## Follow-Up

- If a real repeated callback failure appears in manual play, consider exposing an aggregate diagnostic status that keeps count/last-error visible without adding per-operation hook IDs.
