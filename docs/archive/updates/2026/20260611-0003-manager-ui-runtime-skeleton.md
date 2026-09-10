# 20260611-0003 Manager UI Runtime Skeleton

## Summary

Added an internal Manager runtime model provider and connected it to `UiRuntimeService` so Manager page opens and report export use fresh diagnostics snapshots instead of stale model mutation.

## Source Request

User requested the mid/long next-round plan, including Manager UI runtime skeleton work that uses existing diagnostics/config/runtime surfaces without adding public API or implementing full visual UI.

## Changed Files

- `src/DTMAPI.Core/Manager/DtmManagerRuntimeModelProvider.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Services/WorkshopContentInputUi.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/updates/INDEX.md`

## Details

- Added internal `DtmManagerRuntimeModelProvider` over `IDtmDiagnosticsApi.GetSnapshot()` plus the existing report export delegate.
- `UiRuntimeService` now refreshes an internal Manager view model when DTMAPI Manager pages open.
- `UiRuntimeService.ExportLogs()` now exports through the existing runtime path, refreshes the snapshot, and records whether the exported path equals the refreshed model `LatestReportPath`.
- Added internal report-export states: `exported`, `missing-export-path`, and `report-path-mismatch`.
- No public API members, ConfigMenu behavior, official enablement writes, or full UI rendering were added.

## Validation

- Passed:
  - `git diff --check`
  - `tools/scripts/build.ps1 -Configuration Release`
  - `tools/scripts/test.ps1 -Configuration Release`

No game smoke is required unless the implementation touches real UI rendering; this branch only adds internal Core runtime model/provider plumbing and unit tests.

## Evidence

- Unit test: `ManagerRuntimeProviderRefreshesSnapshotAfterReportExport`
- Design note: `docs/design/dtmapi-manager-ui-mvp.md`

## Rollback Notes

Remove the internal provider, revert the `UiRuntimeService` provider wiring, and keep direct report export behavior. Public API and runtime diagnostics snapshot contracts are unaffected.
