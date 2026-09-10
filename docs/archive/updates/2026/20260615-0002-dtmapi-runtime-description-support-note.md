# 20260615-0002 DTMAPI Runtime Support Notes And Crash Logs

## Status

Verified local package sync.

## Source Request

User requested a revised DTMAPI Runtime Workshop/local description with clearer install/check/uninstall steps, main-menu configuration guidance, troubleshooting contact expectations, and repeated old-SMAPI uninstall warnings. A follow-up player report described slower startup and a fishing crash after installing a content mod, so the runtime package now also includes a player-facing post-crash log collection entry point.

## Changed Files

- `tools/release/runtime-workshop/4_collect_dtmapi_logs.bat`
- `tools/release/dtmapi-mod-publish-zh.json`
- `tools/scripts/build-release-workshop-packages.ps1`
- `tools/scripts/check-dtmapi-status.ps1`

## Implementation

- Updated the DTMAPI Runtime Simplified Chinese `gameDescription`, `steamDescription`, and `localizedDescription.schinese`.
- Updated Traditional Chinese and English localized descriptions with equivalent wording so the release script does not emit stale localized text.
- Updated the publish metadata date to `2026-06-15`.
- Normalized the duplicated phrase in the source request to `有问题请至少带着3_check截图、问题描述...`.
- Added `4_collect_dtmapi_logs.bat` to the runtime Workshop root. It runs outside the game and writes a timestamped support folder under `Desktop\DTMAPI-logs`.
- Added `collect-logs.ps1` and `analyze-startup-evidence.ps1` to the packaged installer tools so crash reports can include Unity `Player.log`, BepInEx `LogOutput.log`, DTMAPI `latest.log`, latest report pointers, Steam launch/upload log tails, process checks, fatal-window checks, and startup classification output when available.
- Updated `3_check_dtmapi_status.bat` guidance to tell players to run `4_collect_dtmapi_logs.bat` when a crash prevents in-game report export.

## Validation

- Parsed `tools/release/dtmapi-mod-publish-zh.json` with `ConvertFrom-Json`.
- Ran `tools/scripts/build-release-workshop-packages.ps1 -Configuration Release -SkipBuild -RuntimeOnly`.
- Ran the packaged `Content\DTMAPIInstaller\tools\collect-logs.ps1` from `dist/workshop-packages/DTMAPI` with a temp output directory; it collected Unity, BepInEx, DTMAPI, Steam log-tail, process/fatal-window, and startup-analysis files.
- Synced `dist/workshop-packages/DTMAPI` into `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI` while preserving `workshop.json`.
- Verified `dist/workshop-packages/DTMAPI/info.json` and the official local upload `MODS/DTMAPI/info.json` match:
  - SHA256 `5CB1A83FBE4DBDFE592F9B84EF6EB85666E4B7DE89C8BD36A77B9868DE67558A`
  - Length `8396`
- Verified the dist/local hashes match for `4_collect_dtmapi_logs.bat`, `collect-logs.ps1`, `analyze-startup-evidence.ps1`, and `check-dtmapi-status.ps1`.
- Verified `MODS/DTMAPI/workshop.json` still contains `workshop_id=3743016467`.

## Evidence

- Local upload path: `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`
- Staging path: `E:\Python_project\DTMAPI\dist\workshop-packages\DTMAPI`
- Player crash-log output path: `%USERPROFILE%\Desktop\DTMAPI-logs\<timestamp>`

## Rollback

Revert the DTMAPI Runtime entry in `tools/release/dtmapi-mod-publish-zh.json`, remove the packaged crash-log BAT/script additions, rebuild the runtime Workshop package, and resync the official local upload package.

## Follow-Up

Manual Workshop upload is still required from the official Mod UI after confirming the text and new `4_collect_dtmapi_logs.bat` entry in-game/local package.
