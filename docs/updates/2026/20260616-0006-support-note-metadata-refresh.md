# 20260616-0006 Support Note Metadata Refresh

## Status

verified-local-package

## Area

release/workshop/mod-metadata/support-notes

## Source Request

User requested three Workshop/local description updates:

- Y key console: append `update 0616` note for the occasional right-click x10 item-give fix.
- Animal bell hidden produce progress: append `update 0616` note for the long-play UI disappearance fix.
- DTMAPI runtime: add a support note saying that if double-clicking `1_install_dtmapi.bat` opens a black screen/window with no response, try switching networks and running it again.

## Changed Files

- `testmods/DebugConsoleMod/official-info.json`
- `testmods/AnimalHusbandryProgressMod/official-info.json`
- `tools/release/dtmapi-mod-publish-zh.json`
- `docs/updates/INDEX.md`

## Summary

- Added Simplified Chinese, Traditional Chinese, and English `update 0616` notes to the Y key console local/Workshop description.
- Added Simplified Chinese, Traditional Chinese, and English `update 0616` notes to the Animal bell hidden produce progress local/Workshop description.
- Added the DTMAPI runtime installer black-window/network-switch troubleshooting note to Simplified Chinese, Traditional Chinese, and English runtime descriptions.
- Kept the aggregate publish metadata aligned with the currently renamed DTMAPI manifest names for the affected published mods.
- Refreshed `dist/workshop-packages`, installed dev official-local packages, and synchronized the DTMAPI runtime local upload package while preserving `workshop.json`.

## Validation

- Parsed the changed source JSON files and refreshed local upload `info.json` files with `ConvertFrom-Json`.
- `tools/scripts/build-release-workshop-packages.ps1 -Configuration Release -SkipBuild` completed.
- `tools/scripts/install-to-game.ps1 -Configuration Release -SkipBuild -InstallAllDevOfficialMods` completed.
- Confirmed the new Simplified Chinese strings are present in:
  - `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI\info.json`
  - `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_YKeyConsole\info.json`
  - `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\Yuuka_DTMAPI_AnimalHusbandryProgress\info.json`
- Dist/local upload `info.json` SHA256 hashes matched:
  - `DTMAPI`: `75EEEDCC77E9`
  - `YKeyConsole`: `D1196705357F`
  - `AnimalHusbandryProgress`: `AEB48BB454D9`
- Release build was not rerun because this update changes package metadata text only and no runtime/source assemblies.

## Rollback Notes

Revert the three metadata files and rebuild/sync the local upload folders. No runtime logic or gameplay hooks are changed by this update.

## Follow-Up

After manual upload, resubscribe or restart the game if Steam keeps showing cached descriptions.
