# 20260609-0020 Feature Status Model

## Metadata

- Update ID: 20260609-0020
- Date: 2026-06-09
- Status: verified
- Source: User route step to add an internal host-maintained feature status model before splitting ActionCompletionFeature.
- Owner: Codex

## Scope

- Add an internal GameBridge feature-host status model.
- Record feature id, last operation, success/failure, failure count, and last error.
- Do not add public-like members to `IGameBridgeFeature`.
- Preserve existing `Feature.Camera` / `Feature.ActionSpeed` `ready` and `failed` status semantics.
- Do not change ActionSpeed hook targets, hook IDs, smoke result schema, CameraView behavior, or public API status.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/hook-map/focused/Camera.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/debug/evidence/GAME-SMOKE/20260609-141440/latest-report.txt`
- `docs/debug/evidence/GAME-SMOKE/20260609-141609/latest-report.txt`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0020-feature-status-model.md`

## Summary

- Added a private `Dictionary<string, GameBridgeFeatureStatus>` to `DolocTownGameBridge`.
- Added private host helpers to record successful and failed `IGameBridgeFeature` dispatches.
- Added private `GameBridgeFeatureStatus` with `Id`, `LastOperation`, `LastSucceeded`, `FailureCount`, and `LastError`.
- Extended `Feature.<Id>` hook status details with `Feature status: id=..., lastOperation=..., success=..., failureCount=..., lastError=...`.
- Kept `IGameBridgeFeature` unchanged and internal.
- Fixed the generated `latest-report.txt` pointers for the new smoke evidence to point at local workspace report zips.

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - Release build/test completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseActionSpeedTool -AutoExerciseActionSpeedConfigApply -AutoExerciseActionSpeedInteraction -SaveSlot 3 -TimeoutSeconds 360`.
- Passed: `tools/scripts/run-game-smoke.ps1 -DirectExe -IncludeHookProbe -AutoExerciseZoom -SaveSlot 3 -TimeoutSeconds 240`.

## Evidence

- ActionSpeed game smoke: `docs/debug/evidence/GAME-SMOKE/20260609-141440`
- ActionSpeed report zip: `docs/debug/evidence/GAME-SMOKE/20260609-141440.zip`
- ActionSpeed `result.json`: `SchemaVersion=2`, `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `SaveLoaded=Passed`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- ActionSpeed log evidence: `Feature.ActionSpeed = ready` with `Feature status: id=ActionSpeed, lastOperation=PublishHookStatuses/InstallHooks/Update/ReturnedToTitle/SaveLoaded/EnvironmentReset, success=True, failureCount=0, lastError=none`; existing `ActionSpeed.ToolAnimation`, `ActionSpeed.InteractionAnimation`, `Smoke.ActionSpeedTool`, `Smoke.ActionSpeedConfigApply`, `Smoke.ActionSpeedAutoFillBottle`, and `Smoke.ActionSpeedInteraction` meanings remain unchanged.
- Camera game smoke: `docs/debug/evidence/GAME-SMOKE/20260609-141609`
- Camera report zip: `docs/debug/evidence/GAME-SMOKE/20260609-141609.zip`
- Camera `result.json`: `SchemaVersion=2`, `RunStatus=Passed`, `StartupLog=Passed`, `GameLaunched=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `Zoom=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Camera log evidence: `Feature.Camera = ready` and `Feature.ActionSpeed = ready` with internal status details, `HookProbe GameLaunched OK`, `HookProbe SaveLoaded OK slot=2 isNewGame=False`, `Camera.ViewEnvironmentLifecycle = experimental`, `Smoke.CameraPlayable = verified`, and `Smoke.Zoom = verified`.
- Exit evidence: both `process-check.txt` files say no `DolocTown.exe` process was found; both `fatal-window-check.txt` files say no fatal instance popup was found.

## Related Records

- `docs/debug/regressions/smoke-matrix.md` row `FEATURE-STATUS-MODEL-20260609`
- `docs/hook-map/README.md` sections `Feature.Camera`, `Camera.ViewApi`, and `ActionSpeed.ToolAnimation`
- `docs/hook-map/focused/Camera.md`
- `docs/updates/2026/20260609-0019-actionspeed-smoke-case-file.md`

## Rollback Notes

- Remove the private `featureStatuses` dictionary, `GameBridgeFeatureStatus` class, and related record/format helpers from `DolocTownGameBridge`.
- Restore the previous `Feature.<Id>` hook status details if a future report parser unexpectedly requires the shorter text.
- No public API contract or feature interface rollback is needed because `IGameBridgeFeature` was not changed.

## Follow-Up

- Merge this branch back to `Refactor`.
- Continue with `codex/refactor-actioncompletion-feature`, moving `IActionCompletionApi` registration from the experimental bridge into a feature/service while preserving existing one-action hook status meanings.
