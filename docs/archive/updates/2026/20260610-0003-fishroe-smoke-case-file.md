# 20260610-0003 FishRoeTooltip Smoke Case File

## Metadata

- Update ID: 20260610-0003
- Date: 2026-06-10
- Status: verified
- Source: User requested the Refactor stability follow-up route, step `codex/refactor-smoke-fishroe-case`.
- Owner: Codex

## Scope

- Move only the FishRoe tooltip smoke case implementation into `Smoke/Cases/FishRoeTooltipSmokeCase.cs`.
- Keep `TryExerciseExperimentalHooksForSmoke()` as the combined FishRoe + AnimalViewer scheduler/result owner in `SmokeHarness.cs`.
- Preserve `Smoke.FishRoeTooltip`, `Smoke.ExperimentalHookExercise`, `Items.FishRoeTooltip`, AnimalViewer co-smoke behavior, logs, result fields, and service behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/FishRoeTooltipSmokeCase.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0003-fishroe-smoke-case-file.md`

## Summary

- Moved `TryExerciseFishRoeTooltipForSmoke()` and `RegisterFishRoeSmokeProvider()` out of the large smoke harness partial and into a focused case file.
- Left the experimental-hook scheduler in place so the combined `FishRoeTooltip=True, AnimalViewerRendering=True` status remains owned by the existing harness path.
- Did not change hook targets, hook IDs, status text, result schema, FishRoeTooltipService behavior, or AnimalViewer co-smoke routing.

## Validation

- Passed: `git diff --check` with CRLF warnings only.
- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - 0 warnings, 0 errors, `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - 0 warnings, 0 errors, `DTMAPI.UnitTests: OK`.
- Passed third-save smoke:
  - `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseExperimentalHooks -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260610-021014`
  - Report/evidence zip: `docs/debug/evidence/GAME-SMOKE/20260610-021014.zip`

## Evidence

- `result.json` records `RunStatus=Passed`, `SaveLoaded=Passed`, `ExperimentalHooks=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Logs record:
  - `Items.FishRoeTooltip = verified`
  - `Feature.FishRoeTooltip = ready`
  - `Smoke fish roe fallback provider registered for public placeholder lookup validation`
  - `Smoke exercise FishRoeTooltip OK item=fish_roe title=鱼卵 (鱼) detail=`
  - `Smoke.FishRoeTooltip = verified`
  - `Smoke.AnimalViewerRendering = verified`
  - `Smoke.ExperimentalHookExercise = verified. FishRoeTooltip=True, AnimalViewerRendering=True.`
- Exit checks record no leftover `DolocTown.exe` and no fatal instance popup.
- `latest-report.txt` was corrected to point at `docs/debug/evidence/GAME-SMOKE/20260610-021014.zip`.

## Related Records

- `docs/updates/2026/20260609-0029-fishroe-tooltip-feature-split.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Rollback Notes

- Move `TryExerciseFishRoeTooltipForSmoke()` and `RegisterFishRoeSmokeProvider()` back into `SmokeHarness.cs` if the case-file split is discarded.
- No runtime hook/service rollback is required because this branch does not change runtime behavior.

## Follow-Up

- Continue with diagnostics snapshot mod status after this branch merges back to `Refactor`.
