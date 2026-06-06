# 20260606-0010 - 0.3.0 Strong Planting Gun API and mod slice

## Status

Partial slice for the active 0.3.0 goal. Task H is build- and third-save-smoke verified on the current 0.3.1 worktree; the broader advanced Y-console creative/generator/monster work that remained open at this slice was later closed in `20260606-0011`.

## Source Request

- User `/goal` for DTMAPI 0.3.0 official-console-informed Y Console expansion plus Zoom, Chest Locator Enhancer, and Strong Planting Gun utility mods.
- `readme.md` Task H from that 0.3.0 ledger: add a stronger planting gun mod that lets the official farming gun carry and apply seed, protective film, and fertilizer slots while keeping fragile Doloc Town interactions inside GameBridge.

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
- `testmods/StrongPlantingGunMod/**`
- `tools/scripts/build.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/run-game-smoke.ps1`
- docs/index/matrix files updated with this record

## Implementation Notes

- Added experimental `IStrongPlantingGunApi` with `StrongPlantingGunOptions`, `StrongPlantingGunRegisterResult`, and `StrongPlantingGunState`.
- Added `StrongPlantingGunMod` as an ordinary official-local DTMAPI mod under `MODS/DTMAPI_StrongPlantingGun`; it registers policy only and does not own Harmony, reflection, Unity object traversal, or decompiled game types.
- Added GameBridge-owned hooks for official farming gun construction/use and official `FarmingGunUiState` transfer paths.
- The bridge expands the official `ItemFarmingGun.inventory` with native `LinearInventory.ValidateCapacity` and updates the official farming-gun function capacity fields so the game's UI exposes three slots.
- Tool use stays on the official farming-gun area query. For each target basin and slot item, the bridge delegates validity and application to official private `CheckCanInteract(equipment, item)` and `DoInteract(equipment, item)` methods.
- The default policy applies seeds, protective film, and fertilizer; water remains opt-in because the current requested player-visible feature is the three material slots.
- UI transfer hooks avoid the stock one-slot hardcode when moving backpack items into the farming gun, while leaving native inventory transactions and item compatibility checks in charge.
- Added `-AutoExerciseStrongPlantingGun` smoke support. The smoke creates a transient plant basin in the third save, generates an official farming gun plus seed/film/fertilizer items, verifies three-slot capacity, applies the tool, checks planted/protected/fertilized basin state, then cleans up and exits.

## Validation

- `tools/scripts/build.ps1 -Configuration Release`
  - Result: passed with 0 errors.
  - Unit tests: `DTMAPI.UnitTests: OK`.
  - Warnings: NU1900 only, because NuGet vulnerability metadata could not be fetched from `https://api.nuget.org/v3/index.json` in the restricted network environment.
- Third-save Strong Planting Gun smoke:
  - Command used an ephemeral `DTMAPI_GAME_DIR` environment variable and `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -DirectExe -AutoExerciseStrongPlantingGun -AutoExitAfterSecondsOverride 120 -TimeoutSeconds 180 -SkipBuild`.
  - Evidence: `docs/debug/evidence/GAME-SMOKE/20260606-170334`.
  - Result: `StartupLog=true`, `GameLaunched=true`, `SaveLoaded=true`, `StrongPlantingGun=true`, `NoFatalInstanceWindow=true`, `ProcessExited=true`, `ForcedClose=false`.
  - Key log lines: `Farming.StrongPlantingGun = experimental`, `StrongPlantingGun expanded official farming gun storage ... slots=3`, `StrongPlantingGun use ... equipments=1, seedActions=1, filmActions=1, fertilizerActions=1, waterActions=0, consumed=3`, `Farming.StrongPlantingGun = verified`, and `Smoke exercise StrongPlantingGun OK ... capacities=inventory:3/total:3/line:3 ... basinState=planted:True,protected:True,fertilized:True`.
- Exit check:
  - `process-check.txt` says `No DolocTown.exe process found.`
  - `fatal-window-check.txt` says `No fatal instance popup found.`

## Evidence Links

- Release build/unit: terminal output from 2026-06-06 after this change.
- Passing game smoke: `docs/debug/evidence/GAME-SMOKE/20260606-170334`.
- Result: `docs/debug/evidence/GAME-SMOKE/20260606-170334/result.json`.
- DTMAPI log: `docs/debug/evidence/GAME-SMOKE/20260606-170334/DTMAPI-latest.log`.

## Related Records

- Goal record: `docs/updates/2026/20260606-0005-030-yconsole-newmods-goal.md`
- Advanced Y-console partial record: `docs/updates/2026/20260606-0008-030-advanced-yconsole-partial.md`
- Chest Locator Enhancer slice: `docs/updates/2026/20260606-0009-030-chest-locator-enhancer.md`
- Smoke matrix: `docs/debug/regressions/smoke-matrix.md`
- Hook map: `docs/hook-map/README.md`
- Public API matrix: `docs/api/public-api-matrix.md`

## Rollback

- Remove `IStrongPlantingGunApi` and its DTOs from `ExperimentalGameBridge.cs`.
- Remove the GameBridge farming-gun construction/use/UI transfer hooks and Strong Planting Gun smoke path.
- Remove `testmods/StrongPlantingGunMod` and the `DTMAPI_StrongPlantingGun` installer package entry.
- Remove `-AutoExerciseStrongPlantingGun` support from `run-game-smoke.ps1`.
- Leave unrelated 0.3.1 regression/new-content changes intact unless the whole current worktree is intentionally rolled back.

## Follow-Up

- Completed in `20260606-0011`: true creative no-cost/no-time hooks, content-backed creative generator, safe monster generation, and the final advanced Y-console third-save validation pass.
