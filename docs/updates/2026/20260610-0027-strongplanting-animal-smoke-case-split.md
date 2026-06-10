# 20260610-0027 StrongPlantingGun And AnimalViewer Smoke Case Split

## Status

Verified.

## Source Request

User requested the post-review stabilization route, fifth branch `codex/refactor-smoke-strongplanting-animal-cases`.

## Summary

- Moved the StrongPlantingGun smoke body and private smoke helpers into `Smoke/Cases/StrongPlantingGunSmokeCase.cs`.
- Moved the AnimalPanel open path, AnimalViewer data/progress preparation, and private animal viewer smoke helpers into `Smoke/Cases/AnimalViewerSmokeCase.cs`.
- Kept smoke scheduling, command flags, result fields, hook/status IDs, log meanings, feature services, public APIs, and gameplay behavior unchanged.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/StrongPlantingGunSmokeCase.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/AnimalViewerSmokeCase.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/ContentSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0027-strongplanting-animal-smoke-case-split.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- DirectExe third-save smokes passed:
  - StrongPlantingGun: `GAME-SMOKE/20260610-122933`, `StrongPlantingGun=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`, report `dtmapi-report-20260610-123010.zip`.
  - AnimalViewer: `GAME-SMOKE/20260610-123042`, `AnimalViewerUi=Passed`, `HookProbe=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, `ForcedClose=Passed`, report `dtmapi-report-20260610-123118.zip`.
- Process checks for both smokes report no `DolocTown.exe`; fatal-window checks report no fatal instance popup.

## Evidence Links

- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Hook map: `docs/hook-map/README.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folders:
  - `docs/debug/evidence/GAME-SMOKE/20260610-122933`
  - `docs/debug/evidence/GAME-SMOKE/20260610-123042`
- Runtime report zips:
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-123010.zip`
  - `D:\steam\steamapps\common\Doloc Town\DTMAPI\reports\dtmapi-report-20260610-123118.zip`

## Rollback

- Move the StrongPlantingGun smoke helpers back into `Smoke/ContentSmoke.cs`.
- Move the AnimalViewer/AnimalPanel smoke helpers back into `Smoke/SmokeHarness.cs`.
- Remove the matrix/update references to `GAME-SMOKE/20260610-122933` and `GAME-SMOKE/20260610-123042`.

## Follow-Up

- Continue the post-review route with the `OilCoalDropFeature` split.
