# 20260610-0023 Feature Host Failure Throttle

## Status

Verified.

## Source Request

User requested the post-review stabilization route, starting with `codex/fix-feature-host-failure-throttle`.

## Summary

- Added operation-level throttling to the `DolocTownGameBridge` feature dispatch catch path.
- Kept the first failure for each `featureId + operation` as a full diagnostics error and error log.
- Kept the next two repeated failures as short runtime-monitor warnings, then suppressed repeated logging until a 30-second summary window is reached.
- Kept `GameBridgeFeatureStatus.FailureCount` and `LastError` updating for every failure so diagnostics snapshots and hook statuses can still report the current state.
- Did not change public APIs, feature service behavior, hook IDs, hook/status meanings, or smoke result schemas.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0023-feature-host-failure-throttle.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Unit coverage invokes the feature failure helper repeatedly for the same `UnitFeature/Update` operation and verifies:
  - only one `DTMAPI.GameBridge.Feature.UnitFeature` diagnostics error is recorded;
  - internal failure count still reaches 6;
  - latest error text remains visible in feature status.
- DirectExe third-save smokes passed:
  - Camera: `GAME-SMOKE/20260610-113540`, `Zoom=Passed`, `Smoke.DiagnosticsSnapshot=verified`, `Feature.Camera=ready`.
  - ActionSpeed: `GAME-SMOKE/20260610-113752`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `Smoke.DiagnosticsSnapshot=verified`, `Feature.ActionSpeed=ready`.
  - SaveSlots/HookProbe: `GAME-SMOKE/20260610-113902`, `HookProbe=Passed`, `Save.MoreSlotsApi=configured-official-archive-count`, `Feature.SaveSlots=ready`.
  - AnimalViewer/HookProbe: `GAME-SMOKE/20260610-114008`, `AnimalViewerUi=Passed`, `Animals.ViewerRendering=verified`, `Feature.AnimalViewer=ready`.
- All four smoke result folders record `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- The smoke logs contain no `GameBridge feature '<id>' failed`, `Repeated GameBridge feature failure`, or `Throttled GameBridge feature failures` entries.

## Evidence Links

- Hook map: `docs/hook-map/README.md#diagnostic-gamebridgefeaturefailurethrottle`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Runtime evidence folders:
  - `docs/debug/evidence/GAME-SMOKE/20260610-113540`
  - `docs/debug/evidence/GAME-SMOKE/20260610-113752`
  - `docs/debug/evidence/GAME-SMOKE/20260610-113902`
  - `docs/debug/evidence/GAME-SMOKE/20260610-114008`
- Runtime report zips:
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-113722.zip`
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-113832.zip`
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-113939.zip`
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-114044.zip`

## Rollback

- Remove the feature failure state dictionary and publication helper from `DolocTownGameBridge`.
- Restore the feature dispatch catch path to always record a diagnostics error and full runtime-monitor error for every feature failure.
- Remove the unit coverage that expects throttled diagnostics publication.

## Follow-Up

- Continue the post-review route with diagnostics entry caps.
