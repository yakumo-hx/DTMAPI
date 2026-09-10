# 20260610-0033 OilCoalDrop Smoke Case File

## Status

Verified.

## Source Request

User requested the post-review midterm follow-up route, fourth branch `codex/refactor-smoke-oilcoaldrop-case`, to split OilCoalDrop smoke code into a dedicated case file without changing result schema or runtime behavior.

## Summary

- Added `Smoke/Cases/OilCoalDropSmokeCase.cs`.
- Moved Oil item metadata smoke, Oil coal-drop smoke, coal-resource preparation, and rendered coal-resource counting helpers out of `Smoke/ContentSmoke.cs`.
- Kept `ContentSmoke.cs` as the NewContent/Mine scheduler and shared helper owner.
- Preserved `NewContentOilItemMetadata`, `NewContentOilCoalDrop`, `Smoke.NewContentOilCoalDrop`, log text, status IDs, result schema, and OilCoalDrop service behavior.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Smoke/ContentSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/OilCoalDropSmokeCase.cs`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0033-oilcoaldrop-smoke-case-file.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- DirectExe third-save NewContent/Oil smoke `GAME-SMOKE/20260610-141632` passed: `SchemaVersion=2`, `NewContentOilItemMetadata=Passed`, `NewContentOilCoalDrop=Passed`, `NewContentEquipmentSlots=Passed`, `NewContentMineOfficialJson=Passed`, `NewContentMineProduction=Passed`, `NewContentApis=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Logs show `Feature.OilCoalDrop = ready`, `Resources.OilCoalDrop = experimental`, `Smoke.NewContentOilItemMetadata = verified`, `OilMod.MiningDrop = experimental`, and `Smoke.NewContentOilCoalDrop = verified`.
- Process check reports no `DolocTown.exe`; fatal-window check reports no fatal instance popup.
- This smoke mode did not export a fresh report zip. Its `latest-report.txt` still points to `dtmapi-report-20260610-134442.zip`, so that stale report is not cited as OilCoalDrop smoke-case evidence.

## Evidence Links

- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Hook map: `docs/hook-map/README.md`
- Runtime evidence folder:
  - `docs/debug/evidence/GAME-SMOKE/20260610-141632`

## Rollback

- Move the Oil smoke methods and helpers back into `Smoke/ContentSmoke.cs`.
- Remove `Smoke/Cases/OilCoalDropSmokeCase.cs` and matrix/update references to `GAME-SMOKE/20260610-141632`.

## Follow-Up

- Add diagnostics aggregate counters in `DiagnosticsService` while keeping the public diagnostics snapshot unchanged.
