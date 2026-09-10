# 20260610-0028 OilCoalDrop Feature Split

## Status

Verified.

## Source Request

User requested the post-review stabilization route, sixth branch `codex/refactor-oilcoaldrop-feature`, including `OilCoalDropFeature` without adding a new public API.

## Summary

- Added `OilCoalDropFeature` and `OilCoalDropService` so Oil coal-drop state, pre-hit capture, post-hit roll, summary fields, and smoke force flag are owned by the GameBridge feature route.
- Kept `ToolCollider.HandleTools` hook ownership single-pointed through `ActionCompletionHookBridge`; the prefix captures Oil coal-drop state and the postfix runs ActionCompletion first, then OilCoalDrop only when OneAction did not handle the hit.
- Routed `ActionCompletionFeature` through an `OilCoalDropService.TryRollOilDropFromCoal` delegate instead of the old experimental-bridge partial.
- Updated NewContent/Oil smoke to use `Bridge.OilCoalDropService` while preserving `Resources.OilCoalDrop`, `OilMod.MiningDrop`, `Smoke.NewContentOilCoalDrop`, OneAction status IDs, result schema, and log meanings.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionCompletion/ActionCompletionHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/OilCoalDrop/OilCoalDropFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/OilCoalDrop/OilCoalDropService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/ContentSmoke.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0028-oilcoaldrop-feature-split.md`

## Validation

- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- DirectExe third-save NewContent/Oil smoke `GAME-SMOKE/20260610-124357` passed: `NewContentOilItemMetadata=Passed`, `NewContentOilCoalDrop=Passed`, `NewContentEquipmentSlots=Passed`, `NewContentMineOfficialJson=Passed`, `NewContentMineProduction=Passed`, `NewContentApis=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- DirectExe third-save OneAction regression smoke `GAME-SMOKE/20260610-124511` passed: `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Process checks for both smokes report no `DolocTown.exe`; fatal-window checks report no fatal instance popup.
- These two smoke modes did not export a fresh report zip. Their `latest-report.txt` files still point to the previous diagnostics/AnimalViewer report `dtmapi-report-20260610-123118.zip`, so that stale report is not cited as OilCoalDrop evidence.

## Evidence Links

- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Hook map: `docs/hook-map/README.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folders:
  - `docs/debug/evidence/GAME-SMOKE/20260610-124357`
  - `docs/debug/evidence/GAME-SMOKE/20260610-124511`

## Rollback

- Revert `OilCoalDropFeature`/`OilCoalDropService`, restore the old experimental-bridge partial owner, and return Oil coal-drop capture patching to the former bridge path.
- Revert the ActionCompletion delegate route to the old experimental-bridge method.
- Remove the matrix/update references to `GAME-SMOKE/20260610-124357` and `GAME-SMOKE/20260610-124511`.

## Follow-Up

- Refresh the final `Refactor` full/web audit packages and tag the post-review stabilization baseline.
