# 20260610-0034 Diagnostics Aggregate Counters

## Status

Verified.

## Source Request

User requested the post-review midterm follow-up route, fifth branch `codex/diagnostics-aggregate-counters`, to add internal diagnostics aggregate counters without changing the public diagnostics snapshot surface.

## Summary

- Added internal error/warning aggregate counters in `DiagnosticsService`, grouped by kind, owner, and message.
- Kept `GetErrors()`, `GetWarnings()`, `IDtmDiagnosticsSnapshot`, and existing retained-window behavior unchanged.
- Added report-only `DiagnosticsAggregates` summary lines to exported diagnostics reports so repeated failures can be ranked even when retained error/warning windows are capped.
- Extended unit coverage to record 1005 repeated errors and warnings, proving aggregate totals and last details survive while retained windows remain capped at 1000.

## Changed Files

- `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0034-diagnostics-aggregate-counters.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Unit coverage verifies 1005 repeated errors and 1005 repeated warnings keep retained `GetErrors()` / `GetWarnings()` windows capped at 1000, include `DiagnosticsTrimmed: errors=5, warnings=5, maxPerKind=1000.`, and add two aggregate summary rows with `count=1005` and latest details.
- DirectExe third-save Camera diagnostics smoke `GAME-SMOKE/20260610-142415` passed: `Zoom=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Camera logs show `Smoke.DiagnosticsSnapshot=verified`, `Feature.Camera=ready`, `loadedMods=14`, `mods=14`, `modStatusCodes=loaded=14`, `errors=0`, `warnings=0`, `hooks=64`, `features=9`, and report `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-142555.zip`.
- DirectExe third-save ActionSpeed diagnostics smoke `GAME-SMOKE/20260610-142626` passed: `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- ActionSpeed logs show `Smoke.DiagnosticsSnapshot=verified`, `Feature.ActionSpeed=ready`, `loadedMods=14`, `mods=14`, `modStatusCodes=loaded=14`, `errors=0`, `warnings=0`, `hooks=68`, `features=9`, and report `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-142705.zip`.
- Both smoke runs used local save slot 3 / index 2 and ended without leftover `DolocTown.exe` or fatal instance popup.

## Evidence Links

- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Hook map: `docs/hook-map/README.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folders:
  - `docs/debug/evidence/GAME-SMOKE/20260610-142415`
  - `docs/debug/evidence/GAME-SMOKE/20260610-142626`

## Rollback

- Remove the internal aggregate dictionary and report-summary rendering from `DiagnosticsService`.
- Revert the unit-test aggregate assertions while keeping the diagnostics entry cap tests intact.
- Remove `DIAGNOSTICS-AGGREGATE-COUNTERS-20260610` and related public API/hook-map/update references.

## Follow-Up

- Keep diagnostics aggregate counters report-only for now. If a future UI needs them, add a deliberate Diagnostic-level public model instead of scraping report text.
