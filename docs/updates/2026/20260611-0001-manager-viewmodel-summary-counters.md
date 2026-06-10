# 20260611-0001 Manager View-Model Summary Counters

## Summary

Added internal Manager view-model summary counters and support-oriented row sorting over the existing `IDtmDiagnosticsSnapshot` data. This keeps the Manager MVP ready for a real UI without adding public API members or changing diagnostics snapshot contracts.

## Source Request

User requested the mid/long next-round plan, with priority on current code-level recommendations, phase roadmap, long-term module route, community feedback loop, and the recommended next execution order.

## Changed Files

- `src/DTMAPI.Core/Manager/DtmManagerViewModels.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/updates/INDEX.md`

## Details

- Added an internal `ManagerSummary` with loaded/blocked/disabled mod counts, diagnostics counts, failed hook/feature counts, and overall `ready`/`warning`/`failed` status.
- Sorted Manager rows for UI consumption: blocked/error mods first, diagnostics newest first, failed/missing hooks first, and failed/degraded features first.
- Expanded unit coverage to verify sorting, summary counters, long path preservation, missing report status, and cumulative feature failure count preservation.

## Validation

- Passed: `git diff --check`
- Passed: `tools/scripts/build.ps1 -Configuration Release`
- Passed: `tools/scripts/test.ps1 -Configuration Release`

No game smoke was run because this is an internal Core view-model/test/docs change with no GameBridge hook or runtime gameplay behavior change.

## Evidence

- Unit test: `ManagerViewModelMapsDiagnosticsSnapshot`
- Design note: `docs/design/dtmapi-manager-ui-mvp.md`

## Rollback Notes

Revert the Manager view-model summary/sorting additions and test assertions. Public API and runtime diagnostics contracts are unaffected.
