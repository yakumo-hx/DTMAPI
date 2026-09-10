# 20260610-0056 Manager UI View-Model Skeleton

## Status

Verified.

## Source Request

User requested the mid/long-term follow-up plan from `Refactor` baseline `c7c8498`. This branch is `codex/manager-ui-viewmodel-skeleton`.

## Summary

- Added an internal `DTMAPI.Core.Manager` view-model skeleton that maps `IDtmDiagnosticsSnapshot` into Mods, Errors, Warnings, Hooks, Features, latest log/report paths, and report-export path state.
- Added unit coverage for snapshot-to-view-model mapping, long path preservation, missing latest report status, and hook/feature/error/warning rows.
- Updated the Manager UI MVP design doc to record that an internal skeleton exists while the real UI remains unimplemented.
- Did not add public API, implement game UI, change ConfigMenu behavior, write official enablement state, parse logs as a primary source, or promote Diagnostic/Experimental surfaces.

## Changed Files

- `src/DTMAPI.Core/AssemblyInfo.cs`
- `src/DTMAPI.Core/Manager/DtmManagerViewModels.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/design/dtmapi-manager-ui-mvp.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0056-manager-ui-viewmodel-skeleton.md`

## Validation

- Passed: `git diff --check`.
- Passed: `tools/scripts/build.ps1 -Configuration Release`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`.
- No game smoke is required because no runtime hook, UI, or installed-game behavior changed.

## Evidence

- Unit test: `ManagerViewModelMapsDiagnosticsSnapshot`.
- Design doc: `docs/design/dtmapi-manager-ui-mvp.md`.

## Rollback

Remove the internal manager view-model classes, the unit test, the `InternalsVisibleTo("DTMAPI.UnitTests")` entry if no longer needed, the design-doc note, and this update record. No runtime/game rollback is needed.

## Follow-Up

- Future Manager UI implementation should consume this internal mapping layer first, then wire Config and Export Report actions through existing runtime/config services without adding public API.
