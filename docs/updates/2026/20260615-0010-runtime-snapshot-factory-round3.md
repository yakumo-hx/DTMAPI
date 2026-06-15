# 20260615-0010 Runtime Snapshot Factory Round 3

Date: 2026-06-15
Status: verified-static
Branch: `codex/refactor-four-step-cleanup`

## Source Request

The user asked for a second-level maintainability branch that improves new Codex handoff and source readability after retiring SecondMotor/MotorVehicle code. Round 3 sub-agent review recommended starting `DtmApiRuntime` cleanup with derived snapshot/status code before touching loader or content roots.

## Summary

Moved runtime and diagnostics snapshot construction out of `DtmApiRuntime.cs` into dedicated runtime snapshot files. `DtmApiRuntime.CreateSnapshot()` and `CreateDiagnosticsSnapshot()` remain the public/internal entry points, but the mod status row derivation and `RuntimeSnapshot` DTO no longer live inside the main runtime class.

## Changed Files

- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.Core/Runtime/RuntimeSnapshot.cs`
- `src/DTMAPI.Core/Runtime/RuntimeSnapshotFactory.cs`
- `docs/updates/2026/20260615-0010-runtime-snapshot-factory-round3.md`
- `docs/updates/INDEX.md`

## Details

- Extracted `RuntimeSnapshot` into its own file.
- Added internal `RuntimeSnapshotFactory` for:
  - runtime UI snapshot creation;
  - diagnostics snapshot creation;
  - discovered/loaded/error-based mod status row derivation;
  - mod error status-code classification.
- Kept `DtmApiRuntime.CreateSnapshot()`, `CreateDiagnosticsSnapshot()`, and `IDtmDiagnosticsApi.GetSnapshot()` signatures unchanged.
- Did not change mod discovery, loading, dependency checks, config page locks, diagnostics storage, report export, or public diagnostics DTOs.

## Validation

- Passed: `powershell -NoProfile -ExecutionPolicy Bypass -File tools\scripts\test.ps1 -Configuration Release` with 0 warnings, 0 errors, and `DTMAPI.UnitTests: OK`.
- Passed: `git diff --check` with line-ending warnings only.

No game smoke was run for this derived-data extraction. Runtime startup and title settings smoke were already verified in the preceding ConfigMenu transaction cut (`GAME-SMOKE/20260616-002528`), but this specific commit only changes snapshot construction.

## Evidence And Related Records

- Prior ConfigMenu verified smoke: `docs/debug/evidence/GAME-SMOKE/20260616-002528`
- Prior ConfigMenu update: `docs/updates/2026/20260615-0009-config-menu-transaction-guard-round3.md`

## Rollback

Revert this commit to return snapshot construction and `RuntimeSnapshot` to `DtmApiRuntime.cs`. No runtime data schema migration is involved.

## Follow-Up

- Continue `DtmApiRuntime` cleanup with service aggregation only after this extraction is reviewed.
- Defer loader/content root extraction until a separate validation plan is in place because those paths affect DLL loading and official enablement.
