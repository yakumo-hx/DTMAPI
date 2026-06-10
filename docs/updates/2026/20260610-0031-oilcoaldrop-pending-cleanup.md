# 20260610-0031 OilCoalDrop Pending Cleanup

## Status

Verified.

## Source Request

User requested the post-review midterm follow-up route, second branch `codex/fix-oilcoaldrop-pending-cleanup`, to clean OilCoalDrop pending hit state at save/title/environment lifecycle boundaries without changing Oil drop behavior.

## Summary

- Added an internal `OilCoalDropService.ClearPendingOilResourceHits(reason)` helper that clears the pending coal-resource hit cache and logs only when stale entries were actually removed.
- Routed `OilCoalDropFeature.SaveLoaded`, `ReturnedToTitle`, and `EnvironmentReset` through that helper.
- Added unit coverage that seeds the private pending-hit cache and verifies all three lifecycle entry points clear it.
- Preserved Oil roll probability, smoke-only force flag behavior, `Resources.OilCoalDrop`, `OilMod.MiningDrop`, `Smoke.NewContentOilCoalDrop`, and OneAction status meanings.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/OilCoalDrop/OilCoalDropFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/OilCoalDrop/OilCoalDropService.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0031-oilcoaldrop-pending-cleanup.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Unit coverage verifies `SaveLoaded`, `ReturnedToTitle`, and `EnvironmentReset` clear a seeded `OilCoalDropService` pending-hit cache.
- DirectExe third-save OneAction smoke `GAME-SMOKE/20260610-135456` passed: `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- DirectExe third-save NewContent/Oil smoke `GAME-SMOKE/20260610-135605` passed: `NewContentOilItemMetadata=Passed`, `NewContentOilCoalDrop=Passed`, `NewContentEquipmentSlots=Passed`, `NewContentMineOfficialJson=Passed`, `NewContentMineProduction=Passed`, `NewContentApis=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Logs show `Feature.OilCoalDrop = ready`, `Resources.OilCoalDrop = experimental`, `OilMod.MiningDrop = experimental`, `Smoke.NewContentOilCoalDrop = verified`, `Feature.ActionCompletion = ready`, `Actions.OneActionComplete = verified`, and `Actions.OneActionFuelFeed = verified`.
- Process checks for both smokes report no `DolocTown.exe`; fatal-window checks report no fatal instance popup.
- These two smoke modes did not export a fresh report zip. Their `latest-report.txt` files still point to `dtmapi-report-20260610-134442.zip`, so that stale report is not cited as OilCoalDrop evidence.

## Evidence Links

- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Hook map: `docs/hook-map/README.md`
- Runtime evidence folders:
  - `docs/debug/evidence/GAME-SMOKE/20260610-135456`
  - `docs/debug/evidence/GAME-SMOKE/20260610-135605`

## Rollback

- Remove the `ClearPendingOilResourceHits` helper and restore the empty `OilCoalDropFeature` lifecycle methods.
- Remove the unit coverage and matrix/update references to `GAME-SMOKE/20260610-135456` and `GAME-SMOKE/20260610-135605`.

## Follow-Up

- Move `ToolCollider.HandleTools` hook ownership into a shared hit hook bridge so ActionCompletion and OilCoalDrop no longer depend on one feature hook bridge for the shared route.
