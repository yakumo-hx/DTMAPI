# 20260610-0004 Diagnostics Snapshot Mod Status

## Metadata

- Update ID: 20260610-0004
- Date: 2026-06-10
- Status: verified
- Source: User requested the Refactor stability follow-up route, step `codex/api-diagnostics-snapshot-mod-status`.
- Owner: Codex

## Scope

- Keep existing `IDtmDiagnosticsSnapshot.LoadedMods`.
- Add Diagnostic-level `IDtmDiagnosticsSnapshot.Mods` and `IDtmModStatusInfo` for discovered, loaded, disabled, and error mod rows.
- Build status rows from runtime discovery, loaded mods, official enablement, and diagnostics errors rather than parsing logs.
- Keep diagnostics snapshot status Diagnostic-only; do not rename or remove existing public members.

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
- `docs/updates/2026/20260610-0004-diagnostics-snapshot-mod-status.md`

## Summary

- Added `IDtmModStatusInfo` with mod id/name/version/type/source, official enablement fields, entry dll/type, loaded flag, status, reason, manifest path, and root path.
- Added `DtmModStatusInfo` and `IDtmDiagnosticsSnapshot.Mods`.
- Synthesized mod-status rows in `DtmApiRuntime.CreateDiagnosticsSnapshot()` for loaded, discovered, official-disabled, and diagnostics-error states.
- Extended diagnostics snapshot smoke validation to require mod-status rows and loaded mod coverage.
- Extended unit coverage for loaded content-pack status, missing dependency errors, invalid entry-dll errors, and official-local disabled/enabled rows.

## Validation

- Passed: `git diff --check` with CRLF warnings only.
- Passed: `tools/scripts/build.ps1 -Configuration Release`
  - 0 warnings, 0 errors, `DTMAPI.UnitTests: OK`.
- Passed: `tools/scripts/test.ps1 -Configuration Release`
  - 0 warnings, 0 errors, `DTMAPI.UnitTests: OK`.
- Passed Camera third-save smoke:
  - `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseZoom -SaveSlot 3 -TimeoutSeconds 240`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260610-022332`
  - Report/evidence zip: `docs/debug/evidence/GAME-SMOKE/20260610-022332.zip`
- Passed ActionSpeed third-save smoke:
  - `tools/scripts/run-game-smoke.ps1 -DirectExe -AutoExerciseActionSpeedTool -AutoExerciseActionSpeedConfigApply -AutoExerciseActionSpeedInteraction -SaveSlot 3 -TimeoutSeconds 360`
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260610-022543`
  - Report/evidence zip: `docs/debug/evidence/GAME-SMOKE/20260610-022543.zip`

## Evidence

- Camera `result.json` records `RunStatus=Passed`, `SaveLoaded=Passed`, `Zoom=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Camera logs record `Feature.Camera = ready`, `Smoke.CameraPlayable = verified`, and `Smoke diagnostics snapshot OK scenario=Camera, expectedFeatures=Camera, loadedMods=14, mods=14, errors=0, warnings=0, hooks=59, features=4`.
- ActionSpeed `result.json` records `RunStatus=Passed`, `SaveLoaded=Passed`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- ActionSpeed logs record `Feature.ActionSpeed = ready` and `Smoke diagnostics snapshot OK scenario=ActionSpeed, expectedFeatures=ActionSpeed, loadedMods=14, mods=14, errors=0, warnings=0, hooks=63, features=4`.
- Unit tests cover `loaded`, required-dependency `error`, invalid EntryDll `error`, official-local `disabled`, and official-local `loaded` status rows.
- Exit checks record no leftover `DolocTown.exe` and no fatal instance popup.
- `latest-report.txt` files were corrected to point at the repo evidence zips for both smoke directories.

## Related Records

- `docs/updates/2026/20260609-0030-api-diagnostics-snapshot.md`
- `docs/updates/2026/20260609-0028-diagnostics-warning-model.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Rollback Notes

- Remove `IDtmDiagnosticsSnapshot.Mods`, `IDtmModStatusInfo`, `DtmModStatusInfo`, and `DtmApiRuntime.CreateModStatusSnapshot(...)` to return to the loaded-mod-only diagnostics snapshot.
- Revert the diagnostics smoke row-count check if the public Diagnostic surface is withdrawn.
- Existing `LoadedMods`, warnings, errors, hook statuses, feature statuses, latest log path, and latest report path can remain unchanged.

## Follow-Up

- Continue with the ChestLocator feature split after this branch merges back to `Refactor`.
