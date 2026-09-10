# 20260616-0005 Mod Config Display Name Sync

## Status

verified-local-package

## Area

release/workshop/mod-metadata/config-menu

## Source Request

User manual QA confirmed Y-console focused search, right-click item give, and debug movement speed load/title boundaries are currently OK, with AnimalViewer long-play validation still pending. The same pass found that several DTMAPI mod config pages still displayed internal/manual-test labels such as "core behavior verified" or "(DTMAPI)" instead of the current Steam page names.

## Root Cause

The DTMAPI config menu does not synthesize those suffixes. Each mod registers its config page through `IDtmConfigMenuApi.SetDisplayName`, and the affected mods resolve that display name from `i18n/*/mod.name`. Older manual-test labels were still present in the source localization files, while `official-info.json` already had the correct Steam names.

Players subscribed to the same published package can see the stale labels when their game language selects those localization files.

## Changed Files

- `testmods/ActionSpeedMod/manifest.json`
- `testmods/ActionSpeedMod/i18n/english.json`
- `testmods/ActionSpeedMod/i18n/schinese.json`
- `testmods/OneActionCompleteMod/manifest.json`
- `testmods/OneActionCompleteMod/i18n/english.json`
- `testmods/OneActionCompleteMod/i18n/schinese.json`
- `testmods/AnimalHusbandryProgressMod/manifest.json`
- `testmods/AnimalHusbandryProgressMod/i18n/english.json`
- `testmods/AnimalHusbandryProgressMod/i18n/schinese.json`
- `testmods/AutoFishingMod/manifest.json`
- `testmods/AutoFishingMod/i18n/english.json`
- `testmods/AutoFishingMod/i18n/schinese.json`
- `testmods/FishBreedingAssistantMod/manifest.json`
- `testmods/FishBreedingAssistantMod/i18n/english.json`
- `testmods/FishBreedingAssistantMod/i18n/schinese.json`
- `tools/scripts/release-common.ps1`
- `docs/updates/INDEX.md`

## Summary

- Unified the five affected config-page display names with their current Steam/local official names:
  - `DTMAPI动作加速` / `DTMAPI Action Speed`
  - `DTMAPI一键完成` / `DTMAPI One Action Complete`
  - `DTMAPI牧铃隐藏产物进度` / `DTMAPI Animal Bell Hidden Produce Progress`
  - `DTMAPI自动钓鱼` / `DTMAPI Auto Fishing`
  - `DTMAPI鱼卵信息显示` / `DTMAPI Fish Roe Info Display`
- Removed the public-facing manual-test suffixes from config page names and section titles for ActionSpeed and OneActionComplete.
- Updated release package definitions for Fish Roe Info Display and Animal Bell Hidden Produce Progress so future packaging/report text uses the same names.
- Refreshed `dist/workshop-packages`, installed the dev official-local packages, and synchronized the DTMAPI runtime local upload package while preserving its `workshop.json`.

## Validation

- Checked installed `D:\steam\steamapps\common\Doloc Town\DTMAPI\logs\latest.log`: no new exception/update-failure flood was observed, right-click give lines use `source=pointer-down`, and retained `latest-*.log` history shows rotation is active.
- `tools/scripts/build.ps1 -Configuration Release` passed with 0 warnings/0 errors and `DTMAPI.UnitTests: OK`.
- `tools/scripts/build-release-workshop-packages.ps1 -Configuration Release -SkipBuild` completed.
- `tools/scripts/install-to-game.ps1 -Configuration Release -SkipBuild -InstallAllDevOfficialMods` completed and refreshed the local official mod packages.
- `git diff --check` passed with line-ending warnings only.
- Source/local upload hashes matched for the five updated feature mod DLLs and Simplified Chinese i18n files:
  - `Yuuka_DTMAPI_ActionSpeed`: DLL `D500E7983C15`, i18n `C62AFD278570`
  - `Yuuka_DTMAPI_OneActionComplete`: DLL `0DF1FBC7FD56`, i18n `47E824846D15`
  - `Yuuka_DTMAPI_AnimalHusbandryProgress`: DLL `E5DAA3B385AD`, i18n `3E7A95BC229A`
  - `Yuuka_DTMAPI_AutoFishing`: DLL `48D26116C5C0`, i18n `15FFA0613EAC`
  - `Yuuka_DTMAPI_FishBreedingAssistant`: DLL `5AE2D347F25C`, i18n `C192B2B28AD9`
- DTMAPI runtime local upload package DLLs matched Release output:
  - `DTMAPI.BepInExBootstrap.dll`: `BEC68458D041`
  - `DTMAPI.Abstractions.dll`: `7AFDDC0F09C0`
  - `DTMAPI.Core.dll`: `CAA29C486D4C`
  - `DTMAPI.GameBridge.DolocTown.dll`: `1081D81D3511`
  - `DTMAPI.ModConfigMenu.dll`: `A92E2351C704`
- Searched source and local upload i18n for the stale public labels: no hits for the removed "已验证" display names or the old postfix-only names.

## Evidence / Local Paths

- Local upload root: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS`
- Refreshed packages:
  - `Yuuka_DTMAPI_ActionSpeed`
  - `Yuuka_DTMAPI_OneActionComplete`
  - `Yuuka_DTMAPI_AnimalHusbandryProgress`
  - `Yuuka_DTMAPI_AutoFishing`
  - `Yuuka_DTMAPI_FishBreedingAssistant`
  - `DTMAPI`

## Rollback Notes

Revert the manifest/i18n display-name edits and rerun the same package/install steps. This only changes metadata text; no runtime hook or gameplay behavior is affected.

## Follow-Up

Manual UI confirmation should verify the DTMAPI config page list now shows the five Steam-aligned names after a clean game restart.
