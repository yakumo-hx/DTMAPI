# 20260610-0026 API Version 0.5.0-alpha And Helper Notes

## Status

Verified.

## Source Request

User requested the post-review stabilization route, fourth branch `codex/chore-api-version-050-alpha-helper-notes`.

## Summary

- Bumped the controlled DTMAPI API/runtime baseline to `0.5.0-alpha`.
- Kept assembly and file versions numeric as `0.5.0.0`.
- Kept BepInEx plugin metadata numeric through `DtmApiRuntime.BinaryVersion = 0.5.0.0` while `DtmApiRuntime.ApiVersion` remains `0.5.0-alpha`.
- Updated official-local install normalization so installed package manifests and DTMAPI dependency minimums use `0.5.0-alpha`.
- Kept `IDtmHelper` Stable while adding Notes that only the helper container shape is stable; child helper surfaces keep their own `DtmApiStatus`.
- Added unit coverage for `MinimumDTMApiVersion=0.5.0-alpha`, future alpha rejection, and legacy `0.4.2` / `0.3.1` compatibility.

## Changed Files

- `Directory.Build.props`
- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `src/DTMAPI.BepInExBootstrap/BootstrapPlugin.cs`
- `src/DTMAPI.Abstractions/Helpers.cs`
- `tools/scripts/install-to-game.ps1`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0026-api-version-050-alpha-helper-notes.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Failed attempt retained: first Camera smoke attempt `GAME-SMOKE/20260610-120641` produced no `result.json`; `BepInEx-LogOutput.log` recorded `Skipping type [DTMAPI.BepInExBootstrap.BootstrapPlugin] because its version is invalid.` because BepInEx plugin metadata used `0.5.0-alpha`. The branch then added numeric `DtmApiRuntime.BinaryVersion = 0.5.0.0` for `[BepInPlugin]` and reran validation.
- DirectExe third-save smokes passed:
  - Camera: `GAME-SMOKE/20260610-121420`, `Zoom=Passed`, `Smoke.DiagnosticsSnapshot=verified`, `Feature.Camera=ready`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`, latest report `dtmapi-report-20260610-121601.zip`.
  - ActionSpeed: `GAME-SMOKE/20260610-121631`, `ActionSpeedTool=Passed`, `ActionSpeedConfigApply=Passed`, `ActionSpeedInteraction=Passed`, `Smoke.DiagnosticsSnapshot=verified`, `Feature.ActionSpeed=ready`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`, latest report `dtmapi-report-20260610-121711.zip`.
- Effective BepInEx startup evidence: `BepInEx-LogOutput.log` in the passed smokes records `Loading [DTMAPI Bootstrap 0.5.0.0]`, while runtime UI/log metadata continues to report `DTMAPI 0.5.0-alpha`.

## Evidence Links

- Public API matrix: `docs/api/public-api-matrix.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Runtime evidence folders:
  - failed attempt: `docs/debug/evidence/GAME-SMOKE/20260610-120641`
  - passed Camera: `docs/debug/evidence/GAME-SMOKE/20260610-121420`
  - passed ActionSpeed: `docs/debug/evidence/GAME-SMOKE/20260610-121631`
- Runtime report zips:
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-121601.zip`
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-121711.zip`

## Rollback

- Restore `Directory.Build.props`, `DtmApiRuntime.ApiVersion`, and installer dependency normalization to the previous controlled version.
- Remove the `IDtmHelper` notes if rolling back the helper-status clarification.
- Remove the alpha-version unit coverage and matrix/update references.

## Follow-Up

- Continue the post-review route with the StrongPlantingGun/AnimalViewer smoke-case split branch.
