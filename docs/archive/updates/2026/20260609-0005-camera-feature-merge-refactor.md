# 20260609-0005 Camera Feature Merge To Refactor

## Metadata

- Update ID: 20260609-0005
- Date: 2026-06-09
- Status: verified
- Source: User request to merge `codex/refactor-camera-feature` into `Refactor`, keep `.gitignore`, ensure Camera evidence/update/hook-map docs are in the workspace, rerun build/test/Camera smoke on `Refactor`, and remove the branch-specific packages after success.
- Owner: Codex

## Summary

- Fast-forward merged `codex/refactor-camera-feature` into `Refactor` at commit `a36b2c1`.
- Confirmed `.gitignore` existed and was not staged or modified before the feature commit, before merge, and after merge.
- Confirmed Camera update records and hook maps are workspace files, not audit-package-only files.
- Updated Camera evidence docs to include the post-merge `Refactor` smoke `GAME-SMOKE/20260609-030107`.
- Removed the branch-specific open-source and audit package directories/zips after merge validation passed.

## Changed Files

- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/hook-map/focused/Camera.md`
- `docs/updates/2026/20260609-0005-camera-feature-merge-refactor.md`
- `docs/updates/INDEX.md`

## Validation

- Passed: feature source commit on `codex/refactor-camera-feature`: `a36b2c1`.
- Passed: fast-forward merge into `Refactor`.
- Passed: `.gitignore` preservation check.
- Passed: workspace docs check for:
  - `docs/hook-map/focused/Camera.md`
  - `docs/updates/2026/20260609-0004-camera-feature-split.md`
  - `docs/debug/regressions/smoke-matrix.md` row `CAMERA-FEATURE-MERGE-REFACTOR-20260609`
- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseZoom -SaveSlot 3 -TimeoutSeconds 240`.

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260609-030107`
- `result.json`: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `Zoom=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`.
- HookProbe log: `HookProbe GameLaunched OK`; `HookProbe SaveLoaded OK slot=2 isNewGame=False`.
- Camera summary: `DTMAPI-evidence/CAMERA-PLAYABLE/20260609-030147/summary.txt`.
- 4x dynamic evidence: 30.018s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=67.5-67.5`, active owner `DTMAPI.ZoomMod`.
- 2x dynamic fallback evidence: 30.01s, 29 samples, `playerDistance=102.528`, `cameraDistance=67.705`, `orthographicSize=33.75-33.75`, active owner `DTMAPI.CameraViewCompetingSmoke`.
- Restore evidence: release of the lower-priority lease restored vanilla `1x`.
- Boundary evidence: summary and logs retain `nativeRefresh=not-called-playable` and `uiScale=unchanged`.
- Exit evidence: `process-check.txt` says no `DolocTown.exe` process was found.
- Complete report zip: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260609-030144.zip`.

## Related Records

- Camera feature split: `docs/updates/2026/20260609-0004-camera-feature-split.md`
- Hook map: `docs/hook-map/README.md` and `docs/hook-map/focused/Camera.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md` rows `CAMERA-FEATURE-SPLIT-20260609` and `CAMERA-FEATURE-MERGE-REFACTOR-20260609`

## Rollback Notes

- If the merge must be backed out, move `Refactor` back before `a36b2c1` or revert `a36b2c1` from `Refactor`; keep `.gitignore` untouched.
- Recreate the branch-specific open-source/audit packages from `codex/refactor-camera-feature` only if another external audit handoff is needed.

## Follow-Up

- None for this merge.
