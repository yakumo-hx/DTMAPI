# 20260613-0020 - Mod Preview Cover Assets

Status: verified
Date: 2026-06-13
Branch: `Refactor`
Source request: User provided cover material under `D:\图片\封面图` and asked to update the named assets to the corresponding local official mods for manual Workshop upload, then noted that the in-game small mod list image had not changed.

## Changed Files

- `tools/scripts/build-release-workshop-packages.ps1`
- `tools/scripts/install-to-game.ps1`
- `testmods/ActionSpeedMod/icon.png`
- `testmods/ActionSpeedMod/preview.png`
- `testmods/MoreSavesMod/icon.png`
- `testmods/MoreSavesMod/preview.png`
- `testmods/OneActionCompleteMod/icon.png`
- `testmods/OneActionCompleteMod/preview.png`
- `testmods/DebugConsoleMod/icon.png`
- `testmods/DebugConsoleMod/preview.png`
- `testmods/ZoomMod/icon.png`
- `testmods/ZoomMod/preview.png`
- `testmods/AnimalHusbandryProgressMod/icon.png`
- `testmods/AnimalHusbandryProgressMod/preview.png`
- `testmods/FishBreedingAssistantMod/icon.png`
- `testmods/FishBreedingAssistantMod/preview.png`
- `testmods/AutoFishingMod/icon.png`
- `testmods/AutoFishingMod/preview.png`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260613-0020-mod-preview-cover-assets.md`

## Summary

- Added per-mod asset selection to `build-release-workshop-packages.ps1`: release staging now prefers `testmods/<Project>/preview.png` and `testmods/<Project>/icon.png`, falling back to the shared DTMAPI branding images when a mod-specific asset is absent.
- Added the same per-mod asset selection to `install-to-game.ps1`, so official local upload sources under `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS` use the matching mod preview and in-game icon.
- Generated square 512x512 `icon.png` files from the named cover sources because the in-game official mod list reads `icon.png`, while Workshop/upload cover previews read `preview.png`.
- Mapped named cover files:
  - `D:\图片\封面图\大视野.png` -> `testmods/ZoomMod/preview.png` and `icon.png` -> `MODS/DTMAPI_Zoom`
  - `D:\图片\封面图\牧铃信息显示.png` -> `testmods/AnimalHusbandryProgressMod/preview.png` and `icon.png` -> `MODS/Yuuka_DTMAPI_AnimalHusbandryProgress`
  - `D:\图片\封面图\鱼卵信息显示.png` -> `testmods/FishBreedingAssistantMod/preview.png` and `icon.png` -> `MODS/Yuuka_DTMAPI_FishBreedingAssistant`
  - `D:\图片\封面图\自动钓鱼.png` -> `testmods/AutoFishingMod/preview.png` and `icon.png` -> `MODS/Yuuka_DTMAPI_AutoFishing`
- Mapped the later added named cover files:
  - `D:\图片\封面图\动画加速.png` -> `testmods/ActionSpeedMod/preview.png` and `icon.png` -> `MODS/Yuuka_DTMAPI_ActionSpeed`
  - `D:\图片\封面图\更多存档.png` -> `testmods/MoreSavesMod/preview.png` and `icon.png` -> `MODS/DTMAPI_MoreSaves`
  - `D:\图片\封面图\一键完成.png` -> `testmods/OneActionCompleteMod/preview.png` and `icon.png` -> `MODS/Yuuka_DTMAPI_OneActionComplete`
  - `D:\图片\封面图\Y键控制台.png` -> `testmods/DebugConsoleMod/preview.png` and `icon.png` -> `MODS/DTMAPI_YKeyConsole`
- Left `D:\图片\封面图\屏幕截图 2026-06-13 202122.png` unused because it is not named as a distinct mod and overlaps the fish roe cover material.
- Left `D:\图片\封面图\屏幕截图 2026-06-13 205608.png` unused because it is screenshot-named rather than mod-named.
- Left `D:\图片\封面图\动画加速.jpg` unused because `D:\图片\封面图\动画加速.png` is the same mod-named source chosen for the ActionSpeed asset.
- Regenerated `dist/workshop-packages` for currently published mods. AutoFishing remains a developer/local official package in the current definitions, so it is verified in the local official upload source rather than `dist/workshop-packages`.

## Validation

- Ran `Test-DtmApiWindowsPowerShellSyntax` for:
  - `tools/scripts/build-release-workshop-packages.ps1`
  - `tools/scripts/install-to-game.ps1`
- Ran `tools/scripts/build-release-workshop-packages.ps1 -SkipBuild`.
- Ran `tools/scripts/install-to-game.ps1 -SkipBuild` before adding per-mod icons to verify preview sync. A later full rerun while the game was open was blocked by the locked runtime DLL `D:\steam\steamapps\common\Doloc Town\BepInEx\plugins\DTMAPI\DTMAPI.BepInExBootstrap.dll`; the four local official `icon.png` files were therefore copied directly from the repo source assets to the official local MODS folders and hash-verified.
- Verified SHA-256 prefix matches between source cover, repo source asset, release staging asset when applicable, and official local package asset:
  - Zoom preview / `大视野`: `154D835148C7`; icon: `BB69B35B4572`
  - AnimalHusbandryProgress preview / `牧铃信息显示`: `338F1EEF22A4`; icon: `3597CA3B503D`
  - FishBreedingAssistant preview / `鱼卵信息显示`: `2E84005E7FEA`; icon: `14FC5A8BA8EE`
  - AutoFishing preview / `自动钓鱼`: `D39142DCF22A`; icon: `1B54623E41C2`
  - ActionSpeed preview / `动画加速`: `58941308FB2C`; icon: `29EFA6B3AD0F`
  - MoreSaves preview / `更多存档`: `E6B6E9090B4A`; icon: `7DEB8F1F2AB8`
  - OneActionComplete preview / `一键完成`: `887FE11115C5`; icon: `7FFA48B17806`
  - DebugConsole preview / `Y键控制台`: `16E108CE0CD0`; icon: `6C07AEFAF2EC`
- No game smoke was run because this update changes Workshop preview/package assets and script asset selection only; it does not change runtime hooks, GameBridge behavior, public APIs, or in-game logic.

## Evidence

- Source cover directory: `D:\图片\封面图`.
- Repo source assets: `testmods/*/preview.png`.
- Release staging packages: `dist/workshop-packages`.
- Official local upload sources:
  - `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI_Zoom`
  - `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/Yuuka_DTMAPI_AnimalHusbandryProgress`
  - `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/Yuuka_DTMAPI_FishBreedingAssistant`
  - `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/Yuuka_DTMAPI_AutoFishing`

## Rollback

- Remove the per-mod `preview.png` files or revert the two script changes to return all package previews to `assets/branding/dtmapi-preview.png`.

## Follow-Up

- If AutoFishing should become part of the published release staging set, handle that as a separate release-definition change instead of bundling it into asset synchronization.
