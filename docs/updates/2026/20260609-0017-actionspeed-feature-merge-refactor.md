# 20260609-0017 ActionSpeed Feature Merge To Refactor

## Metadata

- Update ID: 20260609-0017
- Date: 2026-06-09
- Status: verified
- Source: User request to execute the full ActionSpeed merge and follow-up refactor route, starting by committing `codex/refactor-actionspeed-feature`, merging it into `Refactor`, and rerunning build/test/ActionSpeed smoke/Camera smoke.
- Owner: Codex

## Summary

- Merged `codex/refactor-actionspeed-feature` into `Refactor` with no-fast-forward merge commit `52a2ab1`.
- Preserved `.gitignore`; no `bin`, `obj`, `.tools`, external package directory, package zip, DLL, EXE, PDB, or NuGet payload was staged for this docs update.
- Updated workspace API matrix, hook map, and smoke matrix to cite the post-merge `Refactor` evidence IDs for ActionSpeed and Camera.
- Regenerated compact evidence zips for the post-merge ActionSpeed and Camera smoke runs and fixed their `latest-report.txt` pointers.

## Changed Files

- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/evidence/GAME-SMOKE/20260609-134246/latest-report.txt`
- `docs/debug/evidence/GAME-SMOKE/20260609-134412/latest-report.txt`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0017-actionspeed-feature-merge-refactor.md`

## Validation

- Passed: grouped feature branch commits before merge.
  - `eda3b79` - `refactor: add gamebridge feature host actionspeed split`
  - `83d8866` - `docs: record actionspeed audit package preflight`
- Passed: merge into `Refactor`.
  - `git merge --no-ff codex/refactor-actionspeed-feature`
  - Merge commit: `52a2ab1`
- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseActionSpeedTool -AutoExerciseActionSpeedConfigApply -AutoExerciseActionSpeedInteraction -SaveSlot 3 -TimeoutSeconds 360`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseZoom -SaveSlot 3 -TimeoutSeconds 240`.

## Evidence

- ActionSpeed game smoke: `docs/debug/evidence/GAME-SMOKE/20260609-134246`
- ActionSpeed report zip: `docs/debug/evidence/GAME-SMOKE/20260609-134246.zip`
- ActionSpeed `result.json`: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- ActionSpeed log lines: `Feature.ActionSpeed = ready`, `ActionSpeed.ToolAnimation = verified`, `ActionSpeed.InteractionAnimation = experimental`, `ActionSpeed SaveLoaded restore boundary OK slot=2`, `Smoke.ActionSpeedTool = verified`, `Smoke.ActionSpeedConfigApply = verified`, `Smoke.ActionSpeedAutoFillBottle = verified`, and `Smoke.ActionSpeedInteraction = verified`.
- Camera game smoke: `docs/debug/evidence/GAME-SMOKE/20260609-134412`
- Camera report zip: `docs/debug/evidence/GAME-SMOKE/20260609-134412.zip`
- Camera `result.json`: `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `Zoom=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`. This run did not include HookProbe, so `HookProbe=Skipped`.
- Camera log lines: `Feature.Camera = ready`, `Camera.ViewEnvironmentLifecycle = experimental`, `Camera.ViewApi = contract`, `Camera.ZoomApi = obsolete-compatibility`, `Smoke.CameraPlayable = verified`, and `Smoke.Zoom = verified`.
- Camera dynamic evidence: `DTMAPI-evidence/CAMERA-PLAYABLE/20260609-134452` includes required screenshots, `summary.txt`, and `camera-playable-dynamic-telemetry.csv`; the 4x pass ran 30.234s with 29 samples, `playerDistance=102.528`, `cameraDistance=67.7`, `orthographicSize=67.5-67.5`, active owner `DTMAPI.ZoomMod`; the 2x fallback pass ran 30.255s with 29 samples, `orthographicSize=33.75-33.75`, active owner `DTMAPI.CameraViewCompetingSmoke`; reset restored vanilla `1x`.
- Exit evidence: both smoke runs recorded no leftover `DolocTown.exe` and no fatal popup.

## Related Records

- `docs/updates/2026/20260609-0013-actionspeed-feature-host-split.md`
- `docs/updates/2026/20260609-0016-actionspeed-merge-preflight-audit-fixes.md`
- `docs/debug/regressions/smoke-matrix.md` row `ACTIONSPEED-MERGE-REFACTOR-20260609`
- `docs/hook-map/README.md` sections `ActionSpeed.ToolAnimation`, `Feature.Camera`, and `Camera.ViewApi`
- `docs/api/public-api-matrix.md`

## Rollback Notes

- Revert merge commit `52a2ab1` if the ActionSpeed feature-host split must be backed out from `Refactor`; keep `.gitignore` untouched.
- If only the documentation evidence pointers are wrong, update this record, the API matrix, hook map, and smoke matrix without reverting the merge.

## Follow-Up

- Continue with `codex/fix-actionspeed-lifecycle-restore`.
- Continue with `codex/refactor-smoke-actionspeed-case`.
- Continue with `codex/api-feature-status-model`.
- Continue with `codex/refactor-actioncompletion-feature`.
