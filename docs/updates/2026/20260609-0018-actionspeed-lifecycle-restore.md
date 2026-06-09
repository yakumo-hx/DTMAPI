# 20260609-0018 ActionSpeed Lifecycle Restore

## Metadata

- Update ID: 20260609-0018
- Date: 2026-06-09
- Status: verified
- Source: User route step to fix ActionSpeed lifecycle restore after merging the ActionSpeed feature-host split into `Refactor`.
- Owner: Codex

## Known Facts Before Change

- `ActionSpeedService.RestoreActionSpeed(...)` already restored captured animator speeds for `AgentStateTool.OnExit`, `AgentStateInteract.OnExit`, `AgentStateBase.OnExit`, and smoke cleanup.
- After the feature-host split, `ActionSpeedFeature.SaveLoaded(bool isNewGame)` and `ActionSpeedFeature.ReturnedToTitle()` existed but were empty.
- `Feature.ActionSpeed` dispatch already reached `ReturnedToTitle` and `SaveLoaded`; the missing piece was calling the existing service restore at those boundaries.

## Rejected Directions

- No hook target changes.
- No hook ID, hook status text, or smoke result schema changes.
- No public `IActionSpeedApi` contract changes.
- No Fishing, Vehicle, Equipment, or other migrated feature edits.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/ActionSpeedFeature.cs`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/evidence/GAME-SMOKE/20260609-135903/latest-report.txt`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0018-actionspeed-lifecycle-restore.md`

## Summary

- `ActionSpeedFeature.SaveLoaded(...)` now calls `Service.RestoreActionSpeed("SaveLoaded")`.
- `ActionSpeedFeature.ReturnedToTitle()` now calls `Service.RestoreActionSpeed("ReturnedToTitle")`.
- Existing OnExit restore behavior, hook statuses, smoke fields, and ActionSpeed smoke semantics were left unchanged.
- Fixed the generated `latest-report.txt` pointer for this smoke evidence to point at `docs/debug/evidence/GAME-SMOKE/20260609-135903.zip`.

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseActionSpeedTool -AutoExerciseActionSpeedConfigApply -AutoExerciseActionSpeedInteraction -SaveSlot 3 -TimeoutSeconds 360`.

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260609-135903`
- Report zip: `docs/debug/evidence/GAME-SMOKE/20260609-135903.zip`
- `result.json`: `SchemaVersion=2`, `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Feature dispatch evidence: `Feature.ActionSpeed = ready` for `ReturnedToTitle`, `SaveLoaded`, `EnvironmentReset`, and `Update`.
- Restore evidence retained: `ActionSpeed animator speeds restored reason=AgentStateTool.OnExit restored=3`, `ActionSpeed animator speeds restored reason=AgentStateInteract.OnExit restored=1`, and `ActionSpeed animator speeds restored reason=AgentStateBase.OnExit restored=1`.
- Smoke status evidence retained: `ActionSpeed.ToolAnimation = verified`, `ActionSpeed.InteractionAnimation = experimental`, `Smoke.ActionSpeedTool = verified`, `Smoke.ActionSpeedConfigApply = verified`, `Smoke.ActionSpeedAutoFillBottle = verified`, and `Smoke.ActionSpeedInteraction = verified`.
- Exit evidence: `process-check.txt` says no `DolocTown.exe` process was found; `fatal-window-check.txt` says no fatal instance popup was found.

## Related Records

- `docs/updates/2026/20260609-0013-actionspeed-feature-host-split.md`
- `docs/updates/2026/20260609-0017-actionspeed-feature-merge-refactor.md`
- `docs/debug/regressions/smoke-matrix.md` row `ACTIONSPEED-LIFECYCLE-RESTORE-20260609`
- `docs/hook-map/README.md` section `ActionSpeed.ToolAnimation`

## Rollback Notes

- Revert the two `ActionSpeedFeature` lifecycle calls if the boundary restore causes a regression; keep the existing OnExit restore paths in `ActionSpeedService` intact.

## Follow-Up

- Merge this branch back to `Refactor` after validation.
- Continue with `codex/refactor-smoke-actionspeed-case`.
