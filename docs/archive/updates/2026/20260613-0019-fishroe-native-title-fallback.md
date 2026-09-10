# 20260613-0019 - FishRoe Native Title Fallback

Status: verified
Date: 2026-06-13
Branch: `Refactor`
Source request: User reported that fish roe tooltip information appeared to regress from `鱼卵（鱼名称）` to only `鱼卵`.

## Changed Files

- `src/DTMAPI.GameBridge.DolocTown/Features/FishRoeTooltip/FishRoeTooltipService.cs`
- `src/DTMAPI.GameBridge.DolocTown/Smoke/Cases/FishRoeTooltipSmokeCase.cs`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260613-0019-fishroe-native-title-fallback.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/api/public-api-matrix.md`

## Summary

- Root cause: the shareable `FishBreedingAssistantMod` generated lookup source is intentionally a public placeholder and returns no fish display data. The provider still registered successfully, so hooks/status could look healthy while the title stayed vanilla `鱼卵`.
- Added a GameBridge fallback after provider lookup misses: `FishRoeTooltipService` now uses native `DolocAPI.QueryItemProto(fishId).Title` to resolve the fish name from the fish roe's native `fishName` identity.
- Kept the public `IItemTooltipApi` contract unchanged and Experimental. The fallback is GameBridge-owned native tooltip rendering support, not a promoted public content-query guarantee.
- Removed the smoke-only fish roe fallback provider from `FishRoeTooltipSmokeCase`, so future smokes cannot mask an empty real provider/public placeholder lookup.
- Synchronized the rebuilt DTMAPI Runtime upload source under `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI` and the FishBreedingAssistant upload source under `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/Yuuka_DTMAPI_FishBreedingAssistant`, preserving each `workshop.json`.

## Validation

- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings and 0 errors.
- An initial parallel `build.ps1`/`test.ps1` attempt hit a transient Release `obj` file lock; the serial retry below is the valid test evidence.
- `tools/scripts/test.ps1 -Configuration Release` passed with `DTMAPI.UnitTests: OK`.
- `tools/scripts/build-release-workshop-packages.ps1 -Configuration Release` passed.
- `tools/scripts/install-to-game.ps1 -Configuration Release -InstallPublishedModsOnly` passed and installed the rebuilt runtime and published local mod packages.
- Verified matching GameBridge DLL hash prefix `EE8C67E44BC44945` for:
  - `D:/Steam/steamapps/common/Doloc Town/BepInEx/plugins/DTMAPI/DTMAPI.GameBridge.DolocTown.dll`
  - `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI/Content/DTMAPIInstaller/Payload/BepInEx/plugins/DTMAPI/DTMAPI.GameBridge.DolocTown.dll`
  - `dist/workshop-packages/DTMAPI/Content/DTMAPIInstaller/Payload/BepInEx/plugins/DTMAPI/DTMAPI.GameBridge.DolocTown.dll`
- Verified FishBreedingAssistant package hash prefix `128EE6AD1893A8C9` for the official local upload source and `dist/workshop-packages/DTMAPI-FishBreedingAssistant`.
- First Steam smoke `GAME-SMOKE/20260613-200911` is retained as a harness timing failure: `-AutoExitAfterSecondsOverride 20` exited before save/exercise completion, but it still found no residual `DolocTown.exe` and no fatal instance popup.
- Steam third-save experimental hook smoke `GAME-SMOKE/20260613-201326` passed `RunStatus`, `SaveLoaded`, `ExperimentalHooks`, `ProcessExited`, `NoFatalInstanceWindow`, and `ForcedClose`.
- Fish roe evidence from `GAME-SMOKE/20260613-201326`:
  - `Fish roe native title lookup resolved fish -> 鱼.`
  - `Smoke exercise FishRoeTooltip OK item=fish_roe title=鱼卵 (鱼) detail=`
  - `Smoke.FishRoeTooltip = verified`

## Evidence

- Game smoke: `docs/debug/evidence/GAME-SMOKE/20260613-201326`.
- Retained early-exit smoke: `docs/debug/evidence/GAME-SMOKE/20260613-200911`.
- Installed runtime: `D:/Steam/steamapps/common/Doloc Town/BepInEx/plugins/DTMAPI`.
- Official local upload sources:
  - `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI`
  - `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/Yuuka_DTMAPI_FishBreedingAssistant`

## Rollback

- Revert `FishRoeTooltipService` to provider-only lookup and restore the old smoke fallback provider if native item proto title fallback proves incompatible with a future Doloc Town build.

## Follow-Up

- The shareable FishBreedingAssistant generated lookup remains a placeholder. If later we rebuild a public, clean-room fish breeding data source, it should still go through `IItemTooltipApi` without exposing raw decompiled tables.
