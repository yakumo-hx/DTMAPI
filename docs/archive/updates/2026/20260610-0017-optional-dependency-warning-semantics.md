# 20260610-0017 Optional Dependency Warning Semantics

## Status

Verified.

## Source Request

User requested the post-midterm Refactor follow-up route and the review recommendation that optional dependency version mismatches be warnings instead of load errors.

## Summary

- Changed optional dependency version-too-low diagnostics from `RecordError` to `RecordWarning`.
- Kept required dependency version-too-low diagnostics as blocking errors.
- Preserved the loaded mod status model: a mod that loads with an optional dependency warning still reports `Status=loaded` and `StatusCode=loaded`.
- Did not add a new public status code such as `loaded-with-warnings`.
- Did not change manifest parsing, dependency sorting, API version blocking, cycle blocking, or public manifest contracts.

## Changed Files

- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Unit coverage verifies:
  - required dependency version mismatch still blocks loading and records an error;
  - optional dependency version mismatch records a structured warning and does not block loading;
  - diagnostics snapshot keeps the loaded optional-warning mod at `StatusCode=loaded`.
- DirectExe third-save diagnostics snapshot smokes passed:
  - Camera: `GAME-SMOKE/20260610-094039`, `Zoom=Passed`, `Smoke.DiagnosticsSnapshot=verified`, `modStatusCodes=loaded=14`, `errors=0`, `warnings=0`, `Feature.Camera=ready`.
  - ActionSpeed: `GAME-SMOKE/20260610-094313`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `Smoke.DiagnosticsSnapshot=verified`, `modStatusCodes=loaded=14`, `errors=0`, `warnings=0`, `Feature.ActionSpeed=ready`.
- Both smoke result folders record `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Exit check: no leftover `DolocTown.exe`; fatal-window check clean.

## Evidence Links

- Public API matrix: `docs/api/public-api-matrix.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Runtime evidence folders:
  - `docs/debug/evidence/GAME-SMOKE/20260610-094039`
  - `docs/debug/evidence/GAME-SMOKE/20260610-094313`

## Rollback

- Restore optional dependency version mismatch handling in `CanLoadDependencies` to `RecordError`.
- Remove optional dependency warning assertions from the unit tests.

## Follow-Up

- Keep `loaded-with-warnings` out of the public diagnostics surface unless multiple review cases show that authors need a distinct status code beyond the warnings list.
