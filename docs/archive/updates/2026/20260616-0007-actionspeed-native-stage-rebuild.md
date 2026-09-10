# 20260616-0007 ActionSpeed Native Stage Rebuild

## Status

implemented-build-pending-manual-smoke

## Area

gamebridge/actionspeed/hooks/smoke/api

## Source Request

User started `docs/goals/2026/20260616-0001-actionspeed-bottom-layer-rebuild.md` and requested a second-level branch rebuild before manual testing, then commit/merge only after hand-test acceptance.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/ActionSpeedService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/ActionSpeedHookBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Features/ActionSpeed/ActionSpeedFeature.cs`
- `src/DTMAPI.GameBridge.DolocTown/Hooking/DolocTownHookCallbacks.cs`
- `src/DTMAPI.GameBridge.DolocTown/DolocTownGameBridge.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/SmokeHarness.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/ActionSpeedSmokeCase.cs`
- `testmods/ActionSpeedMod/i18n/english.json`
- `testmods/ActionSpeedMod/i18n/schinese.json`
- `tests/DTMAPI.UnitTests/Program.cs`
- `docs/updates/INDEX.md`
- `docs/api/public-api-matrix.md`
- `docs/hook-map/README.md`
- `docs/debug/regressions/smoke-matrix.md`

## Summary

- Rebuilt ActionSpeed internal classification around native stages while keeping `IActionSpeedApi` Experimental and unchanged.
- Added the missing `AgentControllerState.InteractContinues(float dt)` prefix hook so water-container and interaction-held paths no longer depend only on `UseItemContinues`.
- Made continuous timer scaling shared between `UseItemContinues` and `InteractContinues`, with summaries that include the native stage and classified owner.
- Extended bottle-fill classification so `IWaterContainer` / well interaction can be accelerated even when the selected item is not already an empty bottle; native inventory/bottle success remains owned by the game.
- Extended planting classification to seed planting plus fertilizer and crop-film/protect stages through `PlantBasin` / `FlowerPot` native owners.
- Replaced last-writer-like policy lookup with deterministic provider precedence: enabled candidate, highest stage multiplier, then stable owner-id tie-break.
- Restored animator speed on `EnvironmentReset` in addition to save/title/state-exit boundaries.
- Updated ActionSpeed config text to describe native water-container and planting/fertilizer/film stages.
- Updated smoke readiness/status paths and water-container smoke to exercise `InteractContinues`.

## Validation

- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- The build script also ran `DTMAPI.UnitTests: OK`, including new ActionSpeed option-normalization and provider-precedence coverage.
- `tools/scripts/test.ps1 -Configuration Release` passed with 0 warnings and 0 errors and `DTMAPI.UnitTests: OK`.
- `git diff --check` exited 0; it printed only the repository's existing LF-to-CRLF working-copy warnings for touched files.
- `tools/scripts/install-to-game.ps1 -Configuration Release -SkipBuild -InstallPublishedModsOnly` installed the rebuilt runtime and published official-local packages for manual testing. `tools/scripts/check-dtmapi-status.ps1` then reported all required runtime files `[OK]`, installed DTMAPI `0.5.2-alpha`, and `Yuuka_DTMAPI_ActionSpeed` as `Yuuka.DTMAPI.ActionSpeed 1.3.4-dtmapi`.
- Third-save game smoke and user manual QA are still pending by design; this branch should not be submitted/merged until the requested hand-test passes.

## Parallel Review Notes

- Native-owner/API review found no blocking issue with the rebuild direction: public `IActionSpeedApi` stays Experimental and unchanged, fragile native classification remains inside GameBridge, and the new path is based on `AgentControllerState.InteractContinues` / `UseItemContinues` plus `ItemBottle`, `IWaterContainer`, `PlantBasin`, and `FlowerPot` responsibility boundaries.
- Hook/smoke review found the hook-readiness path complete at code level, but warned that automatic smoke currently proves only the selected-bottle water-container route when run. Backpack-only bottle, fertilizer, and crop-film stages remain manual/future-smoke requirements and must not be cited as solved before fresh third-save evidence.

## Rollback Notes

Revert this update if `AgentControllerState.InteractContinues` proves unstable in the current game build. The rollback should remove the new hook property/callback, restore the previous `UseItemContinues`-only smoke readiness text, and revert ActionSpeed stage classification changes together.

## Follow-Up

Manual test on the third save:

- Water well with a selected empty bottle accelerates and fills through native interaction.
- Water well with an empty bottle only in backpack accelerates the interaction and lets native logic decide success.
- Standing in water with held empty bottle still accelerates the in-water bottle-fill path.
- Planting, fertilizer, and crop-film/protect interactions accelerate without intermittent fallback to native speed.
- Tool, eat/drink, machine add, harvest, resin, and vegetation slices remain unchanged.
- Exit leaves no `DolocTown.exe`, no fatal window, and no Steam waiting-for-exit regression.
