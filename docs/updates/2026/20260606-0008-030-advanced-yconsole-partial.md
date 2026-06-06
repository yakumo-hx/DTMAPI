# Update 20260606-0008: 0.3.0 Advanced Y Console Partial

- Date: 2026-06-06
- Status: partial
- Area: gamebridge/y-console/debug-api/smoke
- Source request: active `/goal` for DTMAPI 0.3.0 official-console-informed Y Console expansion and new utility mods; this record covers the advanced Y-console slice only.
- Version: no new bump in this slice. The worktree was already on controlled runtime version 0.3.1 from `20260606-0007`; this record validates previously planned 0.3.0 advanced-debug APIs on that 0.3.1 codebase.
- Closure: this partial blocker baseline was superseded by `20260606-0011`, which verifies creative no-cost/no-time hooks, the content-backed creative generator, and official-command monster spawn in third-save smoke.

## Changed Files

- `src/DTMAPI.Abstractions/ExperimentalGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownExperimentalBridgeApi.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.BepInExBootstrap/ReflectedDebugConsoleUi.cs`
- `testmods/DebugConsoleMod/ModEntry.cs`
- `tools/scripts/run-game-smoke.ps1`
- docs/index/matrix files updated with this partial record

## Summary

- Added experimental `IAdvancedDebugApi` and `IDebugConsoleApi.BindAdvanced` so the ordinary Y-console mod can request safe, whitelisted advanced debug actions through GameBridge DTOs instead of raw game/decompiled types.
- Added compact advanced Y-console controls for time advance, time scale, money, tech point, tech tree unlock, crop maturity, creative toggle, generator give, monster spawn, and resource spawn.
- Implemented and smoke-verified the required safe subset: +1 day, 4x/reset time scale, +1 money, +1 tech point, tech-tree unlock, crop maturity, creative toggle state, and current-room resource spawn.
- Kept dangerous or incomplete features explicit: creative mode is currently a toggle/status only and does not install no-cost/no-time hooks; `dtmapi_creative_generator` is not present in `TbItem`; monster spawn did not find a usable current-room native generation method in smoke.
- Added `-AutoExerciseAdvancedDebug` to the smoke harness and fixed the pre-existing trap variable issue so advanced-debug evidence can be collected cleanly.

## Validation

- Release build/unit: `tools/scripts/build.ps1 -Configuration Release` passed with 0 errors after the advanced-debug changes. Only NU1900 warnings were emitted because the restricted network could not fetch NuGet vulnerability metadata.
- Passing third-save smoke: `GAME-SMOKE/20260606-155802`.
- Smoke command used a local environment path and DirectExe Steam identity:
  - `$env:DTMAPI_GAME_DIR='D:\Steam\steamapps\common\Doloc Town'`
  - `tools/scripts/run-game-smoke.ps1 -SaveSlot 3 -DirectExe -AutoExerciseAdvancedDebug -AutoExitAfterSecondsOverride 120 -TimeoutSeconds 180 -SkipBuild`
- Result highlights: `AdvancedDebug=true`, `SaveLoaded=true`, `GameLaunched=true`, `NoFatalInstanceWindow=true`, `ProcessExited=true`, and `ForcedClose=false`.
- Key log facts:
  - `Advanced debug time OK ... kind=Day amount=1 seconds=1152 ...`
  - `Advanced debug time scale OK ... multiplier=4` and `time scale reset OK`
  - `Advanced debug money OK ... before=210726 after=210727`
  - `Advanced debug tech point OK ... ANIMAL +1 before=11 after=12`
  - `Advanced debug unlock tech tree OK ... affected=3`
  - `Advanced debug mature crops OK ... matured=169/171`
  - `Advanced debug resource spawn OK ... Spawned resource alfalfa count=1`
  - `Smoke.AdvancedDebug = verified-partial`

## Evidence Links

- Passing game smoke: `docs/debug/evidence/GAME-SMOKE/20260606-155802`
- Result: `docs/debug/evidence/GAME-SMOKE/20260606-155802/result.json`
- DTMAPI log: `docs/debug/evidence/GAME-SMOKE/20260606-155802/DTMAPI-latest.log`

## Known Blockers

- Creative mode is not yet the requested true creative mode: no runtime no-cost/no-time hooks are installed.
- The creative generator content item is absent from runtime `DolocConfig.Tables.TbItem`, so `GiveCreativeGenerator` returns `unknown-item`.
- Monster spawn failed in the verified smoke with `missing-generate-monster`; the resource spawn path worked, but monster generation needs further reverse/API research.
- At the time of this slice, the active 0.3.0 goal was still incomplete because true creative no-cost/no-time, creative generator, and monster generation remained blocked. Chest Locator Enhancer and Strong Planting Gun were later implemented and third-save verified in `20260606-0009` and `20260606-0010`; the advanced Y-console blockers were then closed in `20260606-0011`.

## Rollback Notes

- Remove `IAdvancedDebugApi` and `IDebugConsoleApi.BindAdvanced` from `ExperimentalGameBridge.cs` if the advanced Y-console surface must be withdrawn before release.
- Remove the reflected advanced panel and handlers from `ReflectedDebugConsoleUi` to keep the existing item/weather/teleport/time/movement console intact.
- Remove `-AutoExerciseAdvancedDebug` from `run-game-smoke.ps1` if the smoke harness must return to the pre-advanced-debug surface.

## Follow-Up

- Completed in `20260606-0011`: real no-cost/no-time hooks, official-local creative generator JSON, and official-command current-room monster generation.
- Keep this record as the historical partial smoke baseline for `GAME-SMOKE/20260606-155802`.
