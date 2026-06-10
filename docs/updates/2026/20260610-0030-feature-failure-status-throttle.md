# 20260610-0030 Feature Failure Status Throttle

## Status

Verified.

## Source Request

User requested the post-review midterm follow-up route, first branch `codex/fix-feature-failure-status-throttle`.

## Summary

- Decoupled GameBridge feature diagnostics snapshot updates from `runtime.SetHookStatus("Feature.<Id>")` publication.
- Kept internal `GameBridgeFeatureStatus` and diagnostics feature snapshots current on every feature dispatch success/failure.
- Kept feature failure direct diagnostics/log throttling behavior: first full error, the next two short warnings, then 30-second summary windows.
- Stopped `FailureCount` or `LastError` changes from forcing a `Feature.<Id>` hook-status/log rewrite on every repeated `Update()` failure.
- Did not change public APIs, feature service behavior, hook callback throttling, hook IDs, or smoke result schemas.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0030-feature-failure-status-throttle.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Unit coverage invokes the same `UnitFeature/Update` failure six times and verifies:
  - only one `DTMAPI.GameBridge.Feature.UnitFeature` diagnostics error is recorded;
  - internal failure count still reaches 6;
  - diagnostics feature snapshot exposes failure count 6 and latest error text;
  - `Feature.UnitFeature` hook-status publication stops at the third allowed failure publication instead of rewriting details for failure count 6.
- DirectExe third-save smokes passed:
  - Camera: `GAME-SMOKE/20260610-133933`, `Zoom=Passed`, `Feature.Camera=ready`.
  - ActionSpeed: `GAME-SMOKE/20260610-134145`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `Feature.ActionSpeed=ready`.
  - SaveSlots/HookProbe: `GAME-SMOKE/20260610-134259`, `HookProbe=Passed`, `Feature.SaveSlots=ready`.
  - AnimalViewer/HookProbe: `GAME-SMOKE/20260610-134406`, `AnimalViewerUi=Passed`, `Feature.AnimalViewer=ready`.
- All four smoke result folders record `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- All four smoke logs contain no `GameBridge feature '<id>' failed`, `Repeated GameBridge feature failure`, or `Throttled GameBridge feature failures` entries.

## Evidence Links

- Hook map: `docs/hook-map/README.md#diagnostic-gamebridgefeaturefailurethrottle`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Runtime evidence folders:
  - `docs/debug/evidence/GAME-SMOKE/20260610-133933`
  - `docs/debug/evidence/GAME-SMOKE/20260610-134145`
  - `docs/debug/evidence/GAME-SMOKE/20260610-134259`
  - `docs/debug/evidence/GAME-SMOKE/20260610-134406`
- Runtime report zips:
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-134113.zip`
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-134224.zip`
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-134335.zip`
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-134442.zip`

## Rollback

- Restore `PublishGameBridgeFeatureStatusIfNeeded(...)` to couple diagnostics feature status updates and hook-status publication.
- Restore failure-count/last-error changes as hook-status publication triggers.
- Remove the unit coverage that checks diagnostics snapshot freshness while hook-status publication is throttled.

## Follow-Up

- Continue the midterm follow-up route with OilCoalDrop pending-state lifecycle cleanup.
