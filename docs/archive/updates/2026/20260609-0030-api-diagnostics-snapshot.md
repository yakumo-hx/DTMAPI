# 20260609-0030 API Diagnostics Snapshot

## Status

Verified on 2026-06-09.

## Source Request

User asked to execute the `codex/api-diagnostics-snapshot` follow-up branch: add a Diagnostic runtime API that exposes a read-only snapshot of loaded mods, errors, warnings, hook statuses, feature statuses, and latest report/log path without parsing logs or promoting Experimental/Diagnostic APIs to Stable.

## Changed Files

- `src/DTMAPI.Abstractions/Helpers.cs`
- `src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs`
- `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/CameraPlayableSmokeCase.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/ActionSpeedSmokeCase.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Implementation Notes

- Added `IDtmDiagnosticsApi.GetSnapshot()` and read-only Diagnostic DTO interfaces:
  - `IDtmDiagnosticsSnapshot`
  - `IDtmLoadedModInfo`
  - `IDtmFeatureStatusInfo`
- Registered `IDtmDiagnosticsApi` under the `DTMAPI` runtime API owner.
- Added Core DTOs for loaded mod rows, diagnostics snapshots, and structured feature-status rows.
- Added `DiagnosticsService.GetFeatureStatuses()`, `SetFeatureStatus(...)`, `GetLatestReportPath()`, and report-summary output for features/latest paths.
- Updated `RuntimeSnapshot` to include feature statuses, latest log path, and latest report path.
- Updated `DolocTownGameBridge` feature-host dispatch so `Feature.<Id>` hook statuses and structured `DiagnosticsService` feature-status rows are kept in sync.
- Added smoke-only diagnostics snapshot verification for Camera and ActionSpeed success paths. The smoke exports a report, reads `IDtmDiagnosticsApi.GetSnapshot()`, verifies the expected `Feature.<Id>` hook and structured feature rows, verifies latest log/report paths exist, and marks `Smoke.DiagnosticsSnapshot` as verified or failed.

## Validation

- `tools/scripts/build.ps1 -Configuration Release`: passed, 0 warnings, 0 errors, `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release`: passed, 0 warnings, 0 errors, `DTMAPI.UnitTests: OK`.
- `git diff --check`: passed with CRLF warnings only.

Unit coverage:

- `DiagnosticsSnapshotApiExposesRuntimeState` verifies runtime API registration, loaded mod rows, structured warnings, hook statuses, feature statuses, latest log path, latest report path, `RuntimeSnapshot.FeatureStatuses`, and report summary feature/path output.

Camera smoke:

```powershell
tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseZoom -SaveSlot 3 -TimeoutSeconds 240
```

Evidence: `docs/debug/evidence/GAME-SMOKE/20260609-183542`

Result highlights:

- `RunStatus=Passed`
- `Zoom=Passed`
- `SaveLoaded=Passed`
- `ProcessExited=Passed`
- `NoFatalInstanceWindow=Passed`
- `ForcedClose=Passed`

Snapshot evidence:

- `Smoke.DiagnosticsSnapshot = verified`
- `scenario=Camera`
- `expectedFeatures=Camera`
- `loadedMods=14`
- `errors=0`
- `warnings=0`
- `hooks=59`
- `features=4`
- `latestLog=D:\steam\steamapps\common\Doloc Town\DTMAPI\logs\latest.log`
- `latestReport=D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260609-183724.zip`

Report summary evidence:

- `Errors: 0`
- `Warnings: 0`
- `Features: 4`
- `LatestLogPath: D:\steam\steamapps\common\Doloc Town\DTMAPI\logs\latest.log`
- `LatestReportPath: D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260609-183724.zip`
- `HOOK Feature.Camera: ready`
- `FEATURE Camera: ready`

ActionSpeed smoke:

```powershell
tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseActionSpeedTool -AutoExerciseActionSpeedConfigApply -AutoExerciseActionSpeedInteraction -SaveSlot 3 -TimeoutSeconds 360
```

Evidence: `docs/debug/evidence/GAME-SMOKE/20260609-183757`

Result highlights:

- `RunStatus=Passed`
- `ActionSpeedTool=Passed`
- `ActionSpeedConfigApply=Passed`
- `ActionSpeedInteraction=Passed`
- `SaveLoaded=Passed`
- `ProcessExited=Passed`
- `NoFatalInstanceWindow=Passed`
- `ForcedClose=Passed`

Snapshot evidence:

- `Smoke.DiagnosticsSnapshot = verified`
- `scenario=ActionSpeed`
- `expectedFeatures=ActionSpeed`
- `loadedMods=14`
- `errors=0`
- `warnings=0`
- `hooks=63`
- `features=4`
- `latestLog=D:\steam\steamapps\common\Doloc Town\DTMAPI\logs\latest.log`
- `latestReport=D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260609-183837.zip`

Report summary evidence:

- `Errors: 0`
- `Warnings: 0`
- `Features: 4`
- `LatestLogPath: D:\steam\steamapps\common\Doloc Town\DTMAPI\logs\latest.log`
- `LatestReportPath: D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260609-183837.zip`
- `HOOK Feature.ActionSpeed: ready`
- `FEATURE ActionSpeed: ready`

Process/fatal evidence:

- Both final smokes report no leftover `DolocTown.exe`.
- Both final smokes report no fatal instance popup.

## Rejected / Superseded Evidence

- Preliminary passing smokes `GAME-SMOKE/20260609-183023` and `GAME-SMOKE/20260609-183239` proved `Smoke.DiagnosticsSnapshot`, but the exported `dtmapi-summary.txt` still showed a stale `LatestReportPath` because `DiagnosticsService.ExportLogs()` assigned `LatestReportPath` after building the summary.
- The implementation now assigns `LatestReportPath` before `BuildSummary()`, and final smokes `GAME-SMOKE/20260609-183542` and `GAME-SMOKE/20260609-183757` supersede those preliminary runs.

## Related Records

- API matrix: `docs/api/public-api-matrix.md`, Diagnostics `IDtmDiagnosticsApi.GetSnapshot` row.
- Hook map: `docs/hook-map/README.md`, `Smoke.DiagnosticsSnapshot` and `Feature.Camera`.
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`, `DIAGNOSTICS-SNAPSHOT-20260609`.
- Warning model precursor: `docs/updates/2026/20260609-0028-diagnostics-warning-model.md`.

## Rollback Notes

- Remove `IDtmDiagnosticsApi` and the snapshot DTO interfaces from `DTMAPI.Abstractions`.
- Remove `DtmDiagnosticsSnapshot`, `DtmLoadedModInfo`, `DtmFeatureStatusInfo`, feature-status storage, and latest-report path summary additions from Core diagnostics.
- Stop registering `IDtmDiagnosticsApi` in `DtmApiRuntime.Start()`.
- Remove `RuntimeSnapshot` feature/log/report path additions.
- Remove `DiagnosticsService.SetFeatureStatus(...)` calls from `DolocTownGameBridge`.
- Remove `Smoke.DiagnosticsSnapshot` checks from Camera and ActionSpeed smoke.

## Follow-Up

- Keep this API documented as Diagnostic until the status UI and external review flow prove the snapshot shape is sufficient.
- Do not infer gameplay completion from hook or feature statuses alone; continue requiring focused gameplay smoke/manual evidence for gameplay APIs.
