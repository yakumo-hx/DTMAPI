# 20260531-0009 ActionSpeed Config Apply Smoke

## Source Request / Goal

- Active goal: review and fix DTMAPI 0.1.12 player-visible issues.
- Acceptance target: configuration saves should affect implemented behavior without requiring a game restart.
- Test subject: the already verified ActionSpeed tool-animation path.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `tools/scripts/run-game-smoke.ps1`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Known Facts And Rejected Hypotheses

- Known: `ACTIONSPEED-001` already proves the tool-animation hook changes animator speed.
- Known: ActionSpeed's DTMAPI config save callback writes config and calls `RegisterActionSpeedPolicy("config-save")`.
- Rejected: using startup config alone as proof that player config saves apply at runtime.
- Rejected: treating title-menu visibility or config staging as proof; the evidence must show the implemented hook using the new value in the same game session.

## Implementation

- Added smoke setting `AutoExerciseActionSpeedConfigApply`.
- The smoke path enters the third save with ActionSpeed tool multiplier 2, exercises `AgentStateTool`, then stages and saves the ActionSpeed DTMAPI ConfigMenu page with tool multiplier 4.
- After the save callback runs, the same smoke path exercises `AgentStateTool` again without restarting and requires the new multiplier to appear in GameBridge evidence.
- `run-game-smoke.ps1 -AutoExerciseActionSpeedConfigApply` backs up/restores the ActionSpeed config and fails if the config-apply evidence line is missing.

## Validation

- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\build.ps1`
  - Passed with `DTMAPI.UnitTests: OK`.
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\run-game-smoke.ps1 -TimeoutSeconds 240 -AutoExerciseActionSpeedConfigApply -SkipBuild`
  - Passed.
  - Result: `docs/debug/evidence/GAME-SMOKE/20260531-045330/result.json`
  - Collected logs: `docs/debug/evidence/GAME-SMOKE/20260531-045414`

## Evidence

- Third-save load: `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`
- Before save: `Smoke exercise ActionSpeedTool OK owner=Yuuka.DTMAPI.ActionSpeed, tool=old_pickaxe, multiplier=2, animators=3, samples=body:1->2;tool-renderer:1->2;tool-collider:1->2`
- Config save callback: `ActionSpeed bridge policy OK reason=config-save status=configured-verified-tool-hook`
- Config save log: `ActionSpeed config saved through DTMAPI menu.`
- After save: `Smoke exercise ActionSpeedTool OK owner=Yuuka.DTMAPI.ActionSpeed, tool=old_pickaxe, multiplier=4, animators=3, samples=body:1->4;tool-renderer:1->4;tool-collider:1->4`
- Final status: `Smoke.ActionSpeedConfigApply = verified`
- Exit check: `docs/debug/evidence/GAME-SMOKE/20260531-045330/process-check.txt` says no `DolocTown.exe`.
- Fatal popup check: `docs/debug/evidence/GAME-SMOKE/20260531-045330/fatal-window-check.txt` says no fatal instance popup.

## Related Records

- `docs/debug/regressions/smoke-matrix.md`: `CONFIG-007`, `ACTIONSPEED-001`
- `docs/hook-map/README.md`: `ActionSpeed.ToolAnimation`
- `docs/updates/2026/20260531-0008-actionspeed-tool-animation-smoke.md`

## Rollback Notes

- Remove `AutoExerciseActionSpeedConfigApply` from smoke settings and `run-game-smoke.ps1`.
- If the ActionSpeed save callback regresses, keep `CONFIG-007` failed/pending and do not claim runtime config saves affect implemented behavior.

## Follow-Up

- This proves runtime config save for the verified tool-animation path only.
- Bottle fill, eat/drink, machine add, harvest, auto-fill bottle, and continuous drink still need separate implementation and evidence.
