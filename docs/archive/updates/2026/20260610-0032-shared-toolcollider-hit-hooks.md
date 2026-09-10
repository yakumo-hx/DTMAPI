# 20260610-0032 Shared ToolCollider Hit Hooks

## Status

Verified.

## Source Request

User requested the post-review midterm follow-up route, third branch `codex/refactor-shared-toolcollider-hit-hooks`, to make the `ToolCollider.HandleTools` Prefix/Postfix route a shared hook owner instead of ActionCompletion-owned infrastructure.

## Summary

- Added `ToolColliderHitHookBridge` as the single owner that installs the `ToolCollider.HandleTools` Prefix/Postfix callbacks.
- Kept `DolocTownHookCallbacks.ToolColliderHandleToolsPrefix/Postfix` and callback order unchanged: Oil pre-hit capture, ActionCompletion post-hit, then OilCoalDrop post-hit only when OneAction did not handle the hit.
- Changed `ActionCompletionHookBridge` to consume the shared postfix ready state and publish `Actions.OneActionComplete` / `Actions.OneActionFuelFeed` statuses without installing ToolCollider hooks.
- Changed `OilCoalDropFeature` to consume the shared prefix+postfix route readiness for `Resources.OilCoalDrop`.
- Preserved public APIs, hook/status IDs, smoke result schema, and log meanings.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionCompletion/ActionCompletionFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionCompletion/ActionCompletionHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/ToolColliderHitHookBridge.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0032-shared-toolcollider-hit-hooks.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- DirectExe third-save OneAction smoke `GAME-SMOKE/20260610-140419` passed: `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- DirectExe third-save NewContent/Oil smoke `GAME-SMOKE/20260610-140641` passed: `NewContentOilItemMetadata=Passed`, `NewContentOilCoalDrop=Passed`, `NewContentEquipmentSlots=Passed`, `NewContentMineOfficialJson=Passed`, `NewContentMineProduction=Passed`, `NewContentApis=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Logs show `Feature.ActionCompletion = ready`, `Actions.OneActionComplete = verified`, `Actions.OneActionFuelFeed = verified`, `Feature.OilCoalDrop = ready`, `Resources.OilCoalDrop = experimental`, `OilMod.MiningDrop = experimental`, and `Smoke.NewContentOilCoalDrop = verified`.
- Process checks for both smokes report no `DolocTown.exe`; fatal-window checks report no fatal instance popup.
- These two smoke modes did not export a fresh report zip. Their `latest-report.txt` files still point to `dtmapi-report-20260610-134442.zip`, so that stale report is not cited as shared ToolCollider evidence.

## Evidence Links

- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Hook map: `docs/hook-map/README.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folders:
  - `docs/debug/evidence/GAME-SMOKE/20260610-140419`
  - `docs/debug/evidence/GAME-SMOKE/20260610-140641`

## Rollback

- Remove `ToolColliderHitHookBridge`, restore ToolCollider Prefix/Postfix installation to `ActionCompletionHookBridge`, and point OilCoalDrop route readiness back to the ActionCompletion hook bridge state.
- Remove the matrix/update references to `GAME-SMOKE/20260610-140419` and `GAME-SMOKE/20260610-140641`.

## Follow-Up

- Split OilCoalDrop smoke code into a dedicated smoke case file without changing NewContent/Oil result fields or log semantics.
