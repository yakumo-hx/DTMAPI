# 20260606-0009 - 0.3.0 Chest Locator Enhancer API and mod slice

## Status

Partial slice for the active 0.3.0 goal. Task G is build- and third-save-smoke verified; Strong Planting Gun was later verified in `20260606-0010`; the broader advanced Y-console blockers that were open at this slice were later closed in `20260606-0011`.

## Source Request

- User `/goal` for DTMAPI 0.3.0 official-console-informed Y Console expansion and utility mods.
- `readme.md` Task G: add a Chest Locator Enhancer official-local mod so crafting/material consumption can see shared chests across rooms without moving ordinary DTMAPI mods into `BepInEx/plugins`.

## Version

- Controlled runtime version before this slice: `0.3.1` on the current worktree.
- Controlled runtime version after this slice: `0.3.1`.
- No additional version bump was made in this slice because the earlier 0.3.1 regression/new-content round already advanced the controlled project version after the original 0.3.0 bump.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/HarmonyReflectionPatcher.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `testmods/ChestLocatorEnhancerMod/**`
- `tools/scripts/build.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/run-game-smoke.ps1`
- docs/index/matrix files updated with this record

## Implementation Notes

- Added experimental `IChestLocatorEnhancerApi` with `ChestLocatorEnhancerOptions`, `ChestLocatorEnhancerRegisterResult`, and `ChestLocatorEnhancerState`.
- Added `ChestLocatorEnhancerMod` as an ordinary official-local DTMAPI mod under `MODS/DTMAPI_ChestLocatorEnhancer`; it registers policy only and does not own Harmony, reflection, or Unity object traversal.
- Added a GameBridge-owned Harmony postfix for `ArchiveDataHandle.GetAvailableInventories(Vector2Int anchor, Vector2Int area, bool useBox)`.
- The postfix appends native `LinearInventory` instances for official shared `Case` containers and shared `StorageShelf` item boxes when the native `useBox`/`autoUseBox` path is active.
- Native `LinearInventory[]` extension methods still own `CountItem`, `MaxCostItem`, and `TryCostItem`; DTMAPI extends the inventory array instead of replacing transaction behavior.
- Reflection/Harmony and room/equipment enumeration stay in `DTMAPI.GameBridge.DolocTown`; public API DTOs do not expose decompiled Doloc Town or Unity types.
- Added `-AutoExerciseChestLocatorEnhancer` smoke support. The smoke creates a transient no-render shared `Case` in a building room, places a zero-baseline test item into it, verifies native `CountItem(..., checkBox:true)` and `CostItem(..., checkBox:true)`, then removes the transient case.

## Validation

- `tools/scripts/build.ps1 -Configuration Release`
  - Result: passed with 0 errors.
  - Unit tests: `DTMAPI.UnitTests: OK`.
  - Warnings: NU1900 only, because NuGet vulnerability metadata could not be fetched from `https://api.nuget.org/v3/index.json` in the restricted network environment.
- Third-save Chest Locator Enhancer smoke:
  - Command used an ephemeral `DTMAPI_GAME_DIR` environment variable and `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -DirectExe -AutoExerciseChestLocatorEnhancer -AutoExitAfterSecondsOverride 120 -TimeoutSeconds 180 -SkipBuild`.
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260606-163226`.
  - Result: `StartupLog=true`, `GameLaunched=true`, `SaveLoaded=true`, `ChestLocatorEnhancer=true`, `NoFatalInstanceWindow=true`, `ProcessExited=true`, `ForcedClose=false`.
  - Key log lines: `ChestLocatorEnhancer API register success=True`, `Inventory.ChestLocatorEnhancer = verified`, `ChestLocatorEnhancer inventories ... base=1, appended=5, sharedCases=5`, and `Smoke exercise ChestLocatorEnhancer OK item=dtmapi_mine, baseline=0, afterPlace=3, afterCost=1`.
- Exit check:
  - `process-check.txt` says `No DolocTown.exe process found.`
  - `fatal-window-check.txt` says `No fatal instance popup found.`

## Evidence Links

- Release build/unit: terminal output from 2026-06-06 after this change.
- Passing game smoke: `docs/debug/evidence/GAME-SMOKE/20260606-163226`.

## Related Records

- Goal record: `docs/updates/2026/20260606-0005-030-yconsole-newmods-goal.md`
- Advanced Y-console partial record: `docs/updates/2026/20260606-0008-030-advanced-yconsole-partial.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Hook map: `docs/hook-map/README.md`
- Public API matrix: `docs/api/public-api-matrix.md`

## Rollback

- Remove `IChestLocatorEnhancerApi` and its DTOs from `ExperimentalGameBridge.cs`.
- Remove the GameBridge `GetAvailableInventories` postfix and chest-locator smoke path.
- Remove `testmods/ChestLocatorEnhancerMod` and the `DTMAPI_ChestLocatorEnhancer` installer package entry.
- Remove `-AutoExerciseChestLocatorEnhancer` support from `run-game-smoke.ps1`.
- Leave unrelated 0.3.1 regression/new-content changes intact unless the whole current worktree is intentionally rolled back.

## Follow-Up

- Keep Chest Locator Enhancer coverage in the final combined pass. Strong Planting Gun was later implemented in `20260606-0010`; the remaining active blockers are the advanced Y-console creative/generator/monster gaps.
- Run a full combined third-save validation pass only after the remaining player-visible 0.3.0 features are implemented.
