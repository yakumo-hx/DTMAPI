# 20260608-0011 GameBridge FishingAutomation Feature Split

## Metadata

- Update ID: 20260608-0011
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Moved the FishingAutomation feature implementation from the large `DolocTownExperimentalBridgeApi.cs` file into `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/`.
- Kept hook installation, hook callback methods, public API shape, hook IDs, status strings, and log text unchanged.
- Moved only FishingAutomation-owned API methods, hook-installed flag setter, state/toggle helpers, auto-cast, wait-phase InstantBite, minigame completion, animation-speed helpers, fishing-pool checks, and fishing feedback helpers.
- Left shared reflection helpers, Hooking callbacks, smoke entry points, and unrelated runtime update calls in their existing files for this slice.
- Did not change AutoFishing behavior, hook logic, smoke behavior, or CameraZoom behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishingAutomation/DolocTownExperimentalBridgeApi.FishingAutomation.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0011-gamebridge-fishingautomation-feature-split.md`
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
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseAutoFishingPhase -AutoExerciseAutoFishingMiniGameComplete -SkipBuild -TimeoutSeconds 180 -AutoExitAfterSecondsOverride 90`
  - The validating smoke intentionally did not pass `-IncludeHookProbe` because HookProbe's config-page reset restores AutoFishing to safe defaults where `InstantBite=false`; historical AutoFishing phase/minigame evidence uses the same no-HookProbe smoke shape.

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-080848`
- Startup evidence: `StartupLog=true` in `result.json`.
- Third-save evidence: logs show `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`.
- Test mod evidence: logs show `AutoFishing SaveLoaded boundary OK slot=2`.
- Auto-cast evidence: logs show `Smoke exercise AutoFishingAutoCast OK ... bridge=owner=Yuuka.DTMAPI.AutoFishing, behavior=AutoCast, rod=carbon_fishrod, smokePoolOverride=default, applications=1`.
- Wait-phase evidence: logs show `Fishing phase hook observed phase=Wait source=AgentStateFishingWait.`, `Fishing automation instant-bite applied by Yuuka.DTMAPI.AutoFishing fish=golden_fish pool=default.`, and `Smoke exercise AutoFishingPhase OK owner=Yuuka.DTMAPI.AutoFishing, behavior=InstantBite, phase=Wait, autoHook=AgentStateFishingBattle, fish=golden_fish, isFish=True, pool=default, autoCompleteMiniGame=True, skipMiniGame=False, forceFishForSmoke=True, forceFishSatisfied=True, rollAttempts=1, applications=1`.
- Minigame evidence: logs show `Smoke.AutoFishingMiniGameComplete = verified. owner=Yuuka.DTMAPI.AutoFishing, behavior=AutoCompleteMiniGame, status=Success, skip=false, visibleSeconds=0.764, applications=2` and `Smoke exercise AutoFishingMiniGameComplete OK owner=Yuuka.DTMAPI.AutoFishing, behavior=AutoCompleteMiniGame, status=Success, skip=false, visibleSeconds=0.764, applications=2`.
- Exit evidence: `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`, `process-check.txt` says no `DolocTown.exe`, and `fatal-window-check.txt` says no fatal instance popup.
- Config restore evidence: after the smoke, `D:\Steam\steamapps\common\Doloc Town\DTMAPI\config\Yuuka.DTMAPI.AutoFishing.json` was restored to the pre-smoke local config.
- Retained diagnostic evidence: `GAME-SMOKE/20260608-074148`, `GAME-SMOKE/20260608-074834`, and `GAME-SMOKE/20260608-075738` timed out before formal log collection while using `-IncludeHookProbe`; the game-side log at `D:\Steam\steamapps\common\Doloc Town\DTMAPI\evidence\UI-SMOKE\20260608-075814\DTMAPI-latest.log` showed `Smoke.AutoFishingAutoCast=verified` but `Smoke.AutoFishingPhase=failed` after HookProbe reset the AutoFishing config to safe defaults. The local AutoFishing config was restored after each interrupted attempt.

## Related Records

- Previous split slice: `docs/updates/2026/20260608-0010-gamebridge-animalviewer-feature-split.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-FEATURE-FISHINGAUTOMATION-001`
- Existing regression case: `docs/debug/regressions/smoke-matrix.md` row `AUTOFISH-001`
- Hook map: `docs/hook-map/README.md` row `Fishing.Automation`

## Rollback Notes

- Move `DolocTownExperimentalBridgeApi.FishingAutomation.cs` contents back into `DolocTownExperimentalBridgeApi.cs`.
- No hook IDs, callback paths, status text, log text, public API semantics, or AutoFishing behavior require rollback because this was a mechanical feature split.

## Follow-Up

- Continue moving one feature at a time into `Features/`.
- Keep AutoFishing behavior changes, HookProbe/config-menu smoke coordination, and smoke harness timeout cleanup separate from this mechanical split.
- Split Smoke and Diagnostics only after feature slices remain build- and smoke-clean.
