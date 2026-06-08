# 20260608-0023 Smoke Result Schema V2

## Metadata

- Update ID: 20260608-0023
- Date: 2026-06-08
- Status: verified
- Source: Active goal: "Change `run-game-smoke.ps1` result.json from bools to Passed/Failed/Skipped/Blocked states."
- Owner: Codex

## Summary

- Changed `tools/scripts/run-game-smoke.ps1` result output to schema v2.
- Kept internal smoke flow control as booleans, but converted `result.json` check fields to string statuses.
- Added `SchemaVersion = 2` and `RunStatus` to result output.
- Added preflight `result.json` writing for blocked launches before the game is started.
- Kept `summary.txt` as line-oriented human-readable text.
- Updated the audit package README with the result schema.
- Synchronized the updated smoke script into the clean source and audit package folders.

## Result Schema

- Check fields use `Passed`, `Failed`, `Skipped`, or `Blocked`.
- `Passed`: requested evidence was found.
- `Failed`: requested evidence timed out or was missing, or exit/fatal checks failed.
- `Skipped`: the related AutoExercise/UI/HookProbe/save-load check was not requested.
- `Blocked`: the run could not start cleanly, such as an already-running `DolocTown.exe`.
- `RunStatus` uses `Passed`, `Failed`, `Blocked`, or `Aborted`; `Aborted` is used for fatal-window/process-cleanup failures.

## Changed Files

- `tools/scripts/run-game-smoke.ps1`
- `docs/updates/2026/20260608-0023-smoke-result-schema-v2.md`
- `docs/updates/INDEX.md`
- External package artifact: `E:\Python_project\DTMAPI-open-source-Refactor\tools\scripts\run-game-smoke.ps1`
- External package artifact: `E:\Python_project\DTMAPI-audit-package-Refactor\AUDIT-PACKAGE.md`
- External package artifact: `E:\Python_project\DTMAPI-audit-package-Refactor\tools\scripts\run-game-smoke.ps1`
- External package artifact: `E:\Python_project\DTMAPI-audit-package-Refactor\audit\tools\scripts\run-game-smoke.ps1`
- External package artifact: `E:\Python_project\DTMAPI-audit-package-Refactor.zip`

## Validation

- Passed: PowerShell parser check for `tools/scripts/run-game-smoke.ps1`.
- Passed: extracted status helper tests:
  - requested/pass -> `Passed`
  - requested/fail -> `Failed`
  - not requested -> `Skipped`
  - blocked -> `Blocked`
- Passed: isolated result-block simulation:
  - unrequested `Zoom` and `ExperimentalHooks` -> `Skipped`
  - requested passing `Zoom` and `ExperimentalHooks` -> `Passed`
  - requested failing `Zoom` and `ExperimentalHooks` -> `Failed`
  - forced close -> `RunStatus = Aborted` and `ForcedClose = Failed`
- Passed: blocked/fatal preflight result simulation:
  - `SchemaVersion = 2`
  - `RunStatus = Aborted`
  - `NoFatalInstanceWindow = Failed`
- Passed: `tools/scripts/build.ps1`
  - Release build completed with 0 warnings and 0 errors.
  - `DTMAPI.UnitTests: OK`

## Evidence

- No GameBridge, CameraView, or hook implementation files were changed for this schema update.
- The smoke script still writes `summary.txt` through the existing human-readable append/set-content lines.
- The audit package README now documents schema v2 and notes that the bundled historical CameraView report zip was captured before schema v2.

## Related Records

- Audit package creation: `docs/updates/2026/20260608-0021-open-source-audit-package.md`
- Public build package fix: `docs/updates/2026/20260608-0022-public-build-fishbreeding-exclusion.md`
- Current smoke matrix: `docs/debug/regressions/smoke-matrix.md`

## Rollback Notes

- Revert `tools/scripts/run-game-smoke.ps1` result construction to direct boolean fields if downstream tooling still requires legacy bools.
- Remove the schema v2 section from `E:\Python_project\DTMAPI-audit-package-Refactor\AUDIT-PACKAGE.md` if the audit package is rolled back to a legacy smoke script.
- Regenerate `E:\Python_project\DTMAPI-audit-package-Refactor.zip` after any package rollback.

## Follow-Up

- Update any downstream report parser that expects legacy boolean result fields to consume status strings from schema v2.
