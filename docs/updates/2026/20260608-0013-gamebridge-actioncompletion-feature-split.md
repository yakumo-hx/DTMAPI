# 20260608-0013 GameBridge ActionCompletion Feature Split

## Metadata

- Update ID: 20260608-0013
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Moved the ActionCompletion feature implementation from the large `DolocTownExperimentalBridgeApi.cs` file into `src/DTMAPI.GameBridge.DolocTown/Features/ActionCompletion/`.
- Kept hook installation, hook callback methods, public API shape, hook IDs, status strings, and log text unchanged.
- Moved only ActionCompletion-owned API methods, hook-installed flag setter, resource-hit handling, fuel/feeder fill handling, native `ResourceFellData` validation, policy lookup helpers, and one-action-only native consume helpers.
- Left shared resource reflection helpers, Oil coal-drop behavior, Hooking callbacks, smoke entry points, and unrelated runtime update calls in their existing files for this slice.
- Did not change OneActionComplete behavior, hook logic, smoke behavior, Oil behavior, or CameraZoom behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionCompletion/DolocTownExperimentalBridgeApi.ActionCompletion.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0013-gamebridge-actioncompletion-feature-split.md`
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
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseOneActionResourceHit -AutoExerciseOneActionWrongTool -AutoExerciseOneActionFuelFeed -AutoExerciseOneActionVegetation -SkipBuild -TimeoutSeconds 300 -AutoExitAfterSecondsOverride 240`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-083048`
- Startup evidence: `StartupLog=true` in `result.json`.
- Third-save evidence: logs show `SaveLoaded hook dispatched. slot/index=2 isNewGame=False`.
- Test mod evidence: logs show `OneActionComplete SaveLoaded restore boundary OK slot=2`.
- Resource-hit evidence: logs show `One-action tool hook completed resource drone_bing for Yuuka.DTMAPI.OneActionComplete tool=old_pickaxe toolType=PICKAXE nativeDamage=4 paidExtraHits=2/2 damage=6` and `Smoke exercise OneActionResourceHit OK ... removed=True`.
- Wrong-tool evidence: logs show `Smoke exercise OneActionWrongTool OK coveredKinds=Tree,Ore,Garbage,Weeds, missingKinds=none, failedKinds=none` and each sample kept `oneActionDelta=0`.
- Fuel/feeder evidence: logs show `One-action fuel fill completed by Yuuka.DTMAPI.OneActionComplete item=wood extraConsumed=5 fuel=0.167->1`, `One-action feeder fill completed by Yuuka.DTMAPI.OneActionComplete item=roughage_feed extraConsumed=2 progress=0->1`, and `Smoke exercise OneActionFuelFeed OK ...`.
- Vegetation evidence: logs show `Smoke exercise OneActionVegetation OK ... path=ToolCollider.HandleTools->VegetationRenderer.OnFell->VegetationDandelion.OnFell->Vegetation.CheckToolConstraints ... wrongRemoved=False, correctRemoved=True, oneActionDeltaWrong=0, oneActionDeltaCorrect=0, resourcePath=DungeonResourceRenderer:none`.
- Exit evidence: `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`, `process-check.txt` says no `DolocTown.exe`, and `fatal-window-check.txt` says no fatal instance popup.
- Config restore evidence: the smoke harness restored the OneActionComplete config after the run.

## Related Records

- Previous split slice: `docs/updates/2026/20260608-0012-gamebridge-actionspeed-feature-split.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-FEATURE-ACTIONCOMPLETION-001`
- Existing regression cases: `docs/debug/regressions/smoke-matrix.md` rows `ONEACTION-001`, `ONEACTION-002`, and `ONEACTION-003`
- Hook map: `docs/hook-map/README.md` rows for OneAction resource/fuel-feed behavior

## Rollback Notes

- Move `DolocTownExperimentalBridgeApi.ActionCompletion.cs` contents back into `DolocTownExperimentalBridgeApi.cs`.
- No hook IDs, callback paths, status text, log text, public API semantics, OneActionComplete behavior, or Oil coal-drop behavior require rollback because this was a mechanical feature split.

## Follow-Up

- Continue moving one feature at a time into `Features/`.
- Keep OneAction behavior changes, Oil/Machine behavior changes, and smoke harness changes separate from this mechanical split.
- Split Smoke and Diagnostics only after feature slices remain build- and smoke-clean.
