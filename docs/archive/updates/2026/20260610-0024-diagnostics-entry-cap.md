# 20260610-0024 Diagnostics Entry Cap

## Status

Verified.

## Source Request

User requested the post-review stabilization route, second branch `codex/fix-diagnostics-entry-cap`.

## Summary

- Added an internal 1000-entry retention cap for diagnostics errors and warnings.
- Dropped the oldest diagnostics entries once a list exceeds the cap, while keeping the latest retained window available through `GetErrors()` and `GetWarnings()`.
- Tracked internal trim counts and appended a `DiagnosticsTrimmed` summary line to exported diagnostic reports when any trimming occurs.
- Kept the public diagnostics API surface unchanged; no new public members were added.
- Kept diagnostics snapshot, hook statuses, feature statuses, latest log path, and latest report path behavior unchanged.

## Changed Files

- `src/DTMAPI.Core/Diagnostics/DiagnosticsService.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0024-diagnostics-entry-cap.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Unit coverage records 1005 errors and 1005 warnings, then verifies:
  - retained errors are capped at 1000;
  - retained warnings are capped at 1000;
  - the oldest 5 entries are trimmed;
  - latest entries remain visible;
  - exported report summary includes `DiagnosticsTrimmed: errors=5, warnings=5, maxPerKind=1000.`
- DirectExe third-save diagnostics snapshot smokes passed:
  - Camera: `GAME-SMOKE/20260610-115014`, `Zoom=Passed`, `Smoke.DiagnosticsSnapshot=verified`, `Feature.Camera=ready`, `errors=0`, `warnings=0`, `mods=14`, `modStatusCodes=loaded=14`, latest report `dtmapi-report-20260610-115155.zip`.
  - ActionSpeed: `GAME-SMOKE/20260610-115225`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `Smoke.DiagnosticsSnapshot=verified`, `Feature.ActionSpeed=ready`, `errors=0`, `warnings=0`, `mods=14`, `modStatusCodes=loaded=14`, latest report `dtmapi-report-20260610-115304.zip`.
- Both smoke result folders record `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.

## Evidence Links

- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folders:
  - `docs/debug/evidence/GAME-SMOKE/20260610-115014`
  - `docs/debug/evidence/GAME-SMOKE/20260610-115225`
- Runtime report zips:
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-115155.zip`
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-115304.zip`

## Rollback

- Remove the bounded-list helper and trim counters from `DiagnosticsService`.
- Restore `RecordError` and `RecordWarning` to append without retention limits.
- Remove the diagnostics cap unit coverage and documentation references.

## Follow-Up

- Continue the post-review route with the SaveSlots native responsibility review.
