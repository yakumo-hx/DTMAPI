# 20260609-0019 ActionSpeed Smoke Case File

## Metadata

- Update ID: 20260609-0019
- Date: 2026-06-09
- Status: verified
- Source: User route step to split the ActionSpeed smoke case after the lifecycle restore branch.
- Owner: Codex

## Scope

- Move only ActionSpeed smoke case implementation out of `SmokeHarness.cs`.
- Keep `SmokeHarness.cs` as scheduler and smoke result-field owner.
- Do not change ActionSpeed hook targets, hook IDs, hook status text, smoke result schema, or log/evidence meanings.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/ActionSpeedSmokeCase.cs`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/evidence/GAME-SMOKE/20260609-140738/latest-report.txt`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0019-actionspeed-smoke-case-file.md`

## Summary

- Moved 16 ActionSpeed smoke methods into `Smoke/Cases/ActionSpeedSmokeCase.cs`.
- Left shared helpers, scheduling, smoke settings, and result schema ownership in `SmokeHarness.cs`.
- Verified the moved method body hash before and after the split:
  - `492089ca05ff0faabff397f074a3d92a4af433c448042937409d69a14b31215e`
- Added `using System.Threading;` to the new case file for the existing `Thread.Sleep` call after the first build exposed the missing import.
- Fixed the generated `latest-report.txt` pointer for this smoke evidence to point at `docs/debug/evidence/GAME-SMOKE/20260609-140738.zip`.

## Validation

- Passed after import fix: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseActionSpeedTool -AutoExerciseActionSpeedConfigApply -AutoExerciseActionSpeedInteraction -SaveSlot 3 -TimeoutSeconds 360`.

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260609-140738`
- Report zip: `docs/debug/evidence/GAME-SMOKE/20260609-140738.zip`
- `result.json`: `SchemaVersion=2`, `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Log evidence: `Feature.ActionSpeed = ready`, `ActionSpeed.ToolAnimation = verified`, `ActionSpeed.InteractionAnimation = experimental`, `Smoke.ActionSpeedTool = verified`, `Smoke.ActionSpeedConfigApply = verified`, `Smoke.ActionSpeedAutoFillBottle = verified`, and `Smoke.ActionSpeedInteraction = verified`.
- Exit evidence: `process-check.txt` says no `DolocTown.exe` process was found; `fatal-window-check.txt` says no fatal instance popup was found.

## Related Records

- `docs/updates/2026/20260609-0018-actionspeed-lifecycle-restore.md`
- `docs/debug/regressions/smoke-matrix.md` row `ACTIONSPEED-SMOKE-CASE-SPLIT-20260609`
- `docs/hook-map/README.md` section `ActionSpeed.ToolAnimation`

## Rollback Notes

- Move the ActionSpeed smoke methods back into `SmokeHarness.cs` if a future merge requires a single-file smoke harness, preserving the verified method body hash and keeping result schema unchanged.

## Follow-Up

- Merge this branch back to `Refactor` after validation.
- Continue with `codex/api-feature-status-model`.
