# 20260610-0011 Diagnostics Status Codes

## Metadata

- Update ID: 20260610-0011
- Date: 2026-06-10
- Status: verified
- Source: User requested the midterm Refactor hardening route, including structured diagnostics status/reason codes.
- Owner: Codex

## Scope

- Add a Diagnostic-level structured `StatusCode` to `IDtmModStatusInfo`.
- Preserve existing `Status` and `Reason` text fields.
- Keep `IDtmDiagnosticsSnapshot` Diagnostic; do not promote diagnostics APIs to Stable.
- Do not change mod loading behavior, hook IDs, smoke result schema, or feature status semantics.

## Changed Files

- `src/DTMAPI.Abstractions/Helpers.cs`
- `src/DTMAPI.Core/Diagnostics/DiagnosticsModels.cs`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0011-diagnostics-status-codes.md`

## Summary

- Added `IDtmModStatusInfo.StatusCode`.
- `DtmApiRuntime.CreateDiagnosticsSnapshot()` now classifies mod rows as:
  - `loaded`
  - `disabled`
  - `discovered`
  - `missing-dependency`
  - `dependency-cycle`
  - `entry-dll-error`
  - `code-load-error`
  - `api-too-new`
  - `unknown-error`
- The smoke diagnostics snapshot verifier now fails if mod rows lack `StatusCode`, and it verifies loaded mod rows use `loaded`.
- Smoke summary logs now include `modStatusCodes=...`.

## Validation

- `git diff --check` passed with only existing line-ending warnings.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- Unit coverage:
  - `DependencyVersionApiVersionAndCircularDependencyDiagnostics` verifies `loaded`, `missing-dependency`, `api-too-new`, and `dependency-cycle`.
  - `DiagnosticsSnapshotApiExposesRuntimeState` verifies `loaded`, `missing-dependency`, `entry-dll-error`, `api-too-new`, `code-load-error`, and `unknown-error`.
  - `OfficialLocalModPackagesRespectOfficialEnablement` verifies official-local `disabled` and `loaded`.
- DirectExe third-save Camera smoke:
  - `GAME-SMOKE/20260610-043619`
  - `Zoom=Passed`, `SaveLoaded=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`
  - `Smoke.DiagnosticsSnapshot = verified`
  - `Feature.Camera = ready`
  - diagnostics summary includes `loadedMods=14`, `mods=14`, `modStatusCodes=loaded=14`, `errors=0`, `warnings=0`, `hooks=60`, and `features=5`
  - report zip: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-043801.zip`
- DirectExe third-save ActionSpeed smoke:
  - `GAME-SMOKE/20260610-043830`
  - `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`
  - `Smoke.DiagnosticsSnapshot = verified`
  - `Feature.ActionSpeed = ready`
  - diagnostics summary includes `loadedMods=14`, `mods=14`, `modStatusCodes=loaded=14`, `errors=0`, `warnings=0`, `hooks=64`, and `features=5`
  - report zip: `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-043910.zip`
- Final process check found no leftover `DolocTown.exe`.

## Evidence

- `docs/debug/evidence/GAME-SMOKE/20260610-043619`
- `docs/debug/evidence/GAME-SMOKE/20260610-043830`

## Related Records

- `docs/updates/2026/20260610-0004-diagnostics-snapshot-mod-status.md`
- `docs/updates/2026/20260610-0009-feature-status-publish-throttle.md`

## Rollback Notes

- Revert the `StatusCode` property and mapping helper to return to text-only diagnostics mod status rows.
- Keep `IDtmDiagnosticsSnapshot.Mods` unless explicitly rolling back the earlier mod-status snapshot work.

## Follow-Up

- Continue the midterm route with CustomEntity status naming cleanup.
