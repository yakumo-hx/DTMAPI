# 20260613-0018 - Publish Metadata Writeback

Status: verified
Date: 2026-06-13
Branch: `Refactor`
Source request: User manually edited `tools/release/dtmapi-mod-publish-zh.json` after the branch was merged back to `Refactor`, then asked to write those changes back to the related packaging script and official local mod descriptions.

## Changed Files

- `tools/release/dtmapi-mod-publish-zh.json`
- `tools/scripts/build-release-workshop-packages.ps1`
- `tools/scripts/install-to-game.ps1`
- `testmods/*/official-info.json`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260613-0018-publish-metadata-writeback.md`

## Summary

- Treated `tools/release/dtmapi-mod-publish-zh.json` as the current publishing metadata source for player-facing Chinese names, descriptions, and Workshop/runtime text.
- Changed `build-release-workshop-packages.ps1` so the Runtime package description is read from the DTMAPI `steamDescription` entry in `dtmapi-mod-publish-zh.json` instead of duplicating a long embedded base64 string.
- Adjusted Runtime package generation so DTMAPI `info.json.description`, `steamDescription`, and `localized_description.schinese/tchinese/english` all use the longer DTMAPI Runtime publishing description. This matches the official Doloc Town Workshop uploader, which reads `description` and `localized_description` rather than the custom `steamDescription` field.
- Changed Workshop mod package generation so `official-info.json` versions are preserved when present, preventing generated `info.json` files from being overwritten by older internal manifest versions.
- Changed local official install/update so existing `SAVE/mod_infos.json` entries refresh their player-facing title instead of returning early, and so installed `info.json` versions preserve `official-info.json` when present.
- Wrote the 16 non-runtime entries back into their `testmods/*/official-info.json` files:
  - `name` and `localized_name.schinese` from `modName`
  - `author` from `author`
  - `version` from `version`
  - `description` and `localized_description.schinese` from `gameDescription`
- Synchronized `localized_name.tchinese`, `localized_name.english`, `localized_description.tchinese`, and `localized_description.english` with equivalent Traditional Chinese and English publishing copy so the local official mod descriptions no longer keep stale text while Simplified Chinese changes.
- Kept DTMAPI Runtime at `0.5.1-alpha`; all non-runtime official-info entries now use `1.0.0`.
- Synchronized the currently installed local official DTMAPI packages under `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS`:
  - The installed DTMAPI Runtime package `MODS/DTMAPI/info.json` was updated from the DTMAPI aggregate entry, including the long upload description in official uploader fields.
  - 14 existing DTMAPI-owned local package `info.json` files were updated from source `official-info.json`.
  - Existing `SAVE/mod_infos.json` titles were refreshed for the five enabled `Yuuka_DTMAPI_*` packages that still had pre-writeback titles.
  - Existing enablement states and priorities were preserved.

## Validation

- Parsed `tools/release/dtmapi-mod-publish-zh.json` and all 16 referenced `official-info.json` files with PowerShell `ConvertFrom-Json`.
- Verified DTMAPI Runtime `gameDescription` and `steamDescription` now both use the same long 423-character publishing description.
- Verified every referenced `official-info.json` matches the aggregate JSON for Simplified Chinese name, version, and game description.
- Verified all 16 referenced `official-info.json` files contain non-empty Traditional Chinese and English localized names/descriptions after the writeback.
- Ran `Test-DtmApiWindowsPowerShellSyntax` against `tools/scripts/build-release-workshop-packages.ps1`.
- Ran `build-release-workshop-packages.ps1 -SkipBuild` into a temporary output directory and verified:
  - Runtime `info.json` version stayed `0.5.1-alpha`.
  - Runtime `description`, `steamDescription`, and `localized_description.schinese` all match the long DTMAPI publishing description.
  - The 8 currently published Workshop mod packages generated `info.json` files with version `1.0.0`.
- Ran `Test-DtmApiWindowsPowerShellSyntax` against `tools/scripts/install-to-game.ps1`.
- Verified installed `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/*/info.json` for the 14 currently present DTMAPI-owned official local packages all report version `1.0.0`.
- Verified installed `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI/info.json` reports DTMAPI version `0.5.1-alpha`, uses the long DTMAPI Runtime publishing description for `description`, and has matching `localized_description.schinese/tchinese/english` values.
- Updated local subscribed Workshop content `D:/Steam/steamapps/workshop/content/2285550/3743016467/info.json` with the same DTMAPI Runtime description fields for local consistency; the official upload source remains the LocalLow `MODS/DTMAPI` package.
- Verified `SAVE/mod_infos.json` kept the existing enabled/priority values while updating the five `Yuuka_DTMAPI_*` titles to the new source names.
- No game smoke was run because this update changes publishing metadata and package descriptions only; it does not change runtime hooks, public APIs, gamebridge behavior, or installed-game code paths.

## Evidence

- Aggregate metadata: `tools/release/dtmapi-mod-publish-zh.json`.
- Source metadata: `testmods/*/official-info.json`.
- Packaging script: `tools/scripts/build-release-workshop-packages.ps1`.
- Install script: `tools/scripts/install-to-game.ps1`.
- Installed local official package metadata under `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS`.
- Existing official enablement titles under `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/SAVE/mod_infos.json`.
- Temporary package validation output confirmed `ModPackageCount=8` and `NonOneZeroVersions=0` for published mod package `info.json` files.

## Rollback

- Restore the previous `official-info.json` files and revert the packaging script to its embedded Runtime description if the aggregate JSON should stop owning publish metadata.

## Follow-Up

- If future releases need the internal DTMAPI manifest `Version` fields to match the player-facing Workshop version, handle that as a separate runtime/API compatibility decision instead of silently changing loader manifests in a metadata writeback.
