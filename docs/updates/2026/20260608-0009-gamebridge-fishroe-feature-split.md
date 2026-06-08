# 20260608-0009 GameBridge FishRoeTooltip Feature Split

## Metadata

- Update ID: 20260608-0009
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Mechanical split of DTMAPI.GameBridge.DolocTown without behavior changes"
- Owner: Codex

## Summary

- Moved the FishRoeTooltip feature implementation from the large `DolocTownExperimentalBridgeApi.cs` file into `src/DTMAPI.GameBridge.DolocTown/Features/FishRoeTooltip/`.
- Kept hook installation, hook callback methods, public API shape, status strings, and log text unchanged.
- Moved only FishRoe-owned API methods, hook-installed flag setter, title/detail decorators, provider lookup helper, and `ItemFishRoe` identity helper.
- Left shared string/reflection helpers, Hooking callbacks, and smoke entry points in their existing files for this slice.
- Did not change FishBreedingAssistant behavior or tooltip composition semantics.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/FishRoeTooltip/DolocTownExperimentalBridgeApi.FishRoeTooltip.cs`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/2026/20260608-0009-gamebridge-fishroe-feature-split.md`
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
- Passed: `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'; tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseExperimentalHooks -SkipBuild -TimeoutSeconds 240`
  - The validating smoke temporarily set local `D:\Steam\steamapps\common\Doloc Town\DTMAPI\config\Yuuka.DTMAPI.FishBreedingAssistant.json` to `Enabled=true`, then restored the original `Enabled=false` config in a PowerShell `finally` block.

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260608-072617`
- Startup evidence: `StartupLog=true` in `result.json`.
- HookProbe evidence: `HookProbe=true`, `SaveLoaded=true`, and log line `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
- Hook install evidence: logs show `Items.FishRoeTooltip = verified. Patched item display paths for fish roe providers; verified by FISHROE-001.`
- Feature evidence: logs show `Fish roe tooltip bridge configured by Yuuka.DTMAPI.FishBreedingAssistant.`, `Fish roe title hook applied by Yuuka.DTMAPI.FishBreedingAssistant for fish.`, and `Smoke exercise FishRoeTooltip OK item=fish_roe title=鱼卵 (鱼) detail=`.
- Smoke evidence: logs show `Smoke.FishRoeTooltip = verified. Generated fish roe item and observed decorated tooltip text.` and `Smoke.ExperimentalHookExercise = verified. FishRoeTooltip=True, AnimalViewerRendering=True.`
- Exit evidence: `ProcessExited=true`, `ForcedClose=false`, `NoFatalInstanceWindow=true`, `process-check.txt` says no `DolocTown.exe`, and `fatal-window-check.txt` says no fatal instance popup.
- Local config restore evidence: after the smoke, `D:\Steam\steamapps\common\Doloc Town\DTMAPI\config\Yuuka.DTMAPI.FishBreedingAssistant.json` again contained `"Enabled": false`.
- Retained rejected smoke: `docs/debug/evidence/GAME-SMOKE/20260608-072336` had clean startup/exit but `Smoke.FishRoeTooltip=failed` because the local FishBreedingAssistant config was disabled before validation.

## Related Records

- Previous split slice: `docs/updates/2026/20260608-0008-gamebridge-saveslots-feature-split.md`
- Debug index: `docs/debug/INDEX.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` row `REFACTOR-FEATURE-FISHROE-001`
- Existing regression case: `docs/debug/regressions/smoke-matrix.md` row `FISHROE-001`
- Hook map: `docs/hook-map/README.md` row `Items.FishRoeTooltip`

## Rollback Notes

- Move `DolocTownExperimentalBridgeApi.FishRoeTooltip.cs` contents back into `DolocTownExperimentalBridgeApi.cs`.
- No hook IDs, callback paths, status text, log text, public API semantics, or tooltip behavior require rollback because this was a mechanical feature split.

## Follow-Up

- Continue moving one feature at a time into `Features/`.
- Keep FishRoeTooltip behavior or provider arbitration changes separate from this mechanical split.
- Split Smoke and Diagnostics only after feature slices remain build- and smoke-clean.
