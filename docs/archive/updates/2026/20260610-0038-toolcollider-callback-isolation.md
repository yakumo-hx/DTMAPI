# 20260610-0038 ToolCollider Callback Isolation

## Status

Verified.

## Source Request

User requested the e904d04 post-review stabilization route, first branch `codex/fix-toolcollider-callback-isolation`, to keep ActionCompletion and OilCoalDrop callback failures isolated inside the shared `ToolCollider.HandleTools` postfix route.

## Summary

- Split the `ToolCollider.HandleTools` postfix route so ActionCompletion and OilCoalDrop post-hit callbacks have independent safe wrappers.
- Added distinct diagnostics/throttle operation keys:
  - `ToolCollider.HandleTools.ActionCompletion`
  - `ToolCollider.HandleTools.OilCoalDrop.ApplyAfterHit`
  - `ToolCollider.HandleTools.OilCoalDrop.ClearCaptured`
- Preserved the existing runtime order: ActionCompletion post-hit first; if it does not handle the hit, run OilCoalDrop post-hit; if it does handle the hit, clear the captured OilCoalDrop state.
- Added unit coverage proving an ActionCompletion exception falls back to `oneActionHandled=false` and still runs the OilCoalDrop apply path.
- Did not change hook targets, public APIs, hook/status IDs, smoke result schema, or Oil/OneAction gameplay semantics.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260610-0038-toolcollider-callback-isolation.md`

## Validation

- `git diff --check` passed with CRLF warnings only.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors; `DTMAPI.UnitTests: OK`.
- Unit coverage invokes the internal ToolCollider postfix route helper and verifies:
  - ActionCompletion failure records its own diagnostics key.
  - OilCoalDrop apply still runs when ActionCompletion fails and falls back to false.
  - OilCoalDrop apply and clear failures record independent diagnostics keys.
- DirectExe third-save OneAction smoke `GAME-SMOKE/20260610-163813` passed: `OneActionResourceHit=Passed`, `OneActionWrongTool=Passed`, `OneActionFuelFeed=Passed`, `OneActionVegetation=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- DirectExe third-save NewContent/Oil smoke `GAME-SMOKE/20260610-163928` passed: `NewContentOilItemMetadata=Passed`, `NewContentOilCoalDrop=Passed`, `NewContentEquipmentSlots=Passed`, `NewContentMineOfficialJson=Passed`, `NewContentMineProduction=Passed`, `NewContentApis=Passed`, `SaveLoaded=Passed`, `RunStatus=Passed`, `ProcessExited=Passed`, `NoFatalInstanceWindow=Passed`, and `ForcedClose=Passed`.
- Process checks for both smokes report no `DolocTown.exe`; fatal-window checks report no fatal instance popup.
- These two smoke modes did not export a fresh report zip. Their `latest-report.txt` files still point to `dtmapi-report-20260610-142705.zip`, so that stale report is not cited as ToolCollider callback-isolation evidence.

## Evidence Links

- Hook map: `docs/hook-map/README.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Public API matrix: `docs/api/public-api-matrix.md`
- Runtime evidence folders:
  - `docs/debug/evidence/GAME-SMOKE/20260610-163813`
  - `docs/debug/evidence/GAME-SMOKE/20260610-163928`

## Rollback

- Restore the single `ToolCollider.HandleTools.ApplyActionCompletionOrOilDrop` safe wrapper inside `ToolColliderHandleToolsPostfix`.
- Remove the internal ToolCollider postfix route helper and its unit coverage.
- Remove the matrix/update references to `GAME-SMOKE/20260610-163813` and `GAME-SMOKE/20260610-163928`.

## Follow-Up

- Continue the post-review stabilization route with feature failure recovery episode policy.
