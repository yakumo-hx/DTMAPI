# 20260608-0012 GameBridge ActionSpeed Feature Split

## Metadata

- Update ID: 20260608-0012
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Moved the ActionSpeed feature implementation from the large `DolocTownExperimentalBridgeApi.cs` file into `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/`.
- Kept hook installation, hook callback methods, public API shape, hook IDs, status strings, and log text unchanged.
- Moved only ActionSpeed-owned API methods, hook-installed flag setters, policy lookup/classification helpers, tool/interact/eat/continuous-use handlers, auto-fill runtime handler, and ActionSpeed target classifiers.
- Left shared animator/reflection helpers, Hooking callbacks, smoke entry points, and unrelated runtime update calls in their existing files for this slice.
- Did not change ActionSpeed behavior, hook logic, smoke behavior, or CameraZoom behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/DolocTownExperimentalBridgeApi.ActionSpeed.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0012-gamebridge-actionspeed-feature-split.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: `git diff --check -- src/DTMAPI.GameBridge.DolocTown`
  - Only existing CRLF warning output was reported.
- Passed: `tools/scripts/build.ps1`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `tools/scripts/test.ps1`
  - Release build/test pass completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseActionSpeedTool -AutoExerciseActionSpeedConfigApply -AutoExerciseActionSpeedInteraction -SkipBuild -TimeoutSeconds 240 -AutoExitAfterSecondsOverride 270`
  - The validating smoke intentionally did not pass `-IncludeHookProbe`, matching historical ActionSpeed config/apply and interaction smoke paths that rely on smoke-written temporary ActionSpeed config.

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-082000`
- Startup evidence: `StartupLog=true` in `result.json`.
- Third-save evidence: logs show `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`.
- Test mod evidence: logs show `ActionSpeed SaveLoaded restore boundary OK slot=2`.
- Tool evidence: logs show `Smoke exercise ActionSpeedTool OK owner=Yuuka.DTMAPI.ActionSpeed, tool=old_pickaxe, multiplier=2, animators=3, samples=body:1->2;tool-renderer:1->2;tool-collider:1->2` and then the config-applied `multiplier=4` run.
- Config-apply evidence: logs show `Smoke exercise ActionSpeedConfigApply OK before=owner=Yuuka.DTMAPI.ActionSpeed, tool=old_pickaxe, multiplier=2 ... after=owner=Yuuka.DTMAPI.ActionSpeed, tool=old_pickaxe, multiplier=4 ...` and `Smoke.ActionSpeedConfigApply = verified`.
- Interaction evidence: logs show `Smoke.ActionSpeedInteraction = verified` with fuel machine, feeder, eat/drink, bottled-water right-click, bottle fill, in-water bottle fill, auto-fill, planting, crop harvest, resin, and vegetation harvest slices.
- Auto-fill evidence: logs show `Smoke.ActionSpeedAutoFillBottle = verified. branch=InteractiveWater.IsInWater ... bridge=owner=Yuuka.DTMAPI.ActionSpeed, behavior=AutoFillBottle ... strong=True ... applications=1`.
- Exit evidence: `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`, `process-check.txt` says no `DolocTown.exe`, and `fatal-window-check.txt` says no fatal instance popup.
- Config restore evidence: after the smoke, `D:\Steam\steamapps\common\Doloc Town\DTMAPI\config\Yuuka.DTMAPI.ActionSpeed.json` was restored to the pre-smoke local config with `"Enabled": false`.

## Related Records

- Previous split slice: `docs/updates/2026/20260608-0011-gamebridge-fishingautomation-feature-split.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-FEATURE-ACTIONSPEED-001`
- Existing regression cases: `docs/debug/regressions/smoke-matrix.md` rows `ACTIONSPEED-001` and `ACTIONSPEED-002`
- Hook map: `docs/hook-map/README.md` rows `ActionSpeed.ToolAnimation` and `ActionSpeed.InteractionAnimation`

## Rollback Notes

- Move `DolocTownExperimentalBridgeApi.ActionSpeed.cs` contents back into `DolocTownExperimentalBridgeApi.cs`.
- No hook IDs, callback paths, status text, log text, public API semantics, or ActionSpeed behavior require rollback because this was a mechanical feature split.

## Follow-Up

- Continue moving one feature at a time into `Features/`.
- Keep ActionSpeed behavior changes and smoke harness coordination separate from this mechanical split.
- Split Smoke and Diagnostics only after feature slices remain build- and smoke-clean.
