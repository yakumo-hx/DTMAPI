# 20260609-0028 Diagnostics Warning Model

## Metadata

- Update ID: 20260609-0028
- Date: 2026-06-09
- Status: verified
- Source: User follow-up plan, step `codex/diagnostics-warning-model`.
- Owner: Codex

## Scope

- Add a structured diagnostics warning model alongside existing errors and hook statuses.
- Expose warning reads through `IDiagnosticsHelper.GetWarnings()` and `RuntimeSnapshot.Warnings`.
- Include warning counts/details in exported diagnostic report summaries.
- Record `MinimumGameVersion` unverifiable state as a warning instead of log-only metadata.

## Changed Files

- `src/DTMAPI.Abstractions/Helpers.cs`
- `src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs`
- `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260609-0028-diagnostics-warning-model.md`

## Summary

- Added public experimental `IDtmWarningInfo` with time, owner, message, and details.
- Added public `IDiagnosticsHelper.GetWarnings()`.
- Added internal `DiagnosticsService.RecordWarning(...)` and warning storage guarded by the existing diagnostics lock.
- Added `RuntimeSnapshot.Warnings`.
- Added warning count and `WARNING ...` lines to `dtmapi-summary.txt` inside exported reports.
- Updated `CanLoadGameVersion(...)` to record a structured warning when a manifest declares `MinimumGameVersion` but the runtime host cannot detect the game version.

## Validation

- Passed: `tools/scripts/build.ps1 -Configuration Release`
- Passed: `tools/scripts/test.ps1 -Configuration Release`
- Passed: `git diff --check`

## Evidence

- Unit test `MinimumGameVersionWithoutDetectedGameVersionLogsWarning` now verifies:
  - the mod still loads when game-version detection is unavailable,
  - `RuntimeSnapshot.Warnings` contains the `MinimumGameVersion` warning,
  - `IDiagnosticsHelper.GetWarnings()` exposes the same structured warning,
  - the DTMAPI log still contains the explicit warn line,
  - exported `dtmapi-summary.txt` includes `Warnings: 1` and a `WARNING` entry.
- Build/test completed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- No game smoke was required for this core diagnostics/API-only branch.

## Related Records

- `docs/api/public-api-matrix.md` Diagnostics row
- `docs/debug/regressions/smoke-matrix.md` row `DIAGNOSTICS-WARNING-MODEL-20260609`

## Rollback Notes

- Remove `IDtmWarningInfo`, `GetWarnings()`, warning storage, `RuntimeSnapshot.Warnings`, report warning output, and the `CanLoadGameVersion(...)` `RecordWarning(...)` call.
- Keep the existing log warning behavior if rolling back only the structured model.

## Follow-Up

- Merge `codex/diagnostics-warning-model` back to `Refactor`.
- Continue with the `FishRoeTooltipFeature` split.
