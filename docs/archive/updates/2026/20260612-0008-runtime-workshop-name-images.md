# 20260612-0008 - Runtime Workshop Name and Images

## Status

Verified

## Source Request

User asked whether the DTMAPI Runtime upload package can be configured with images like the other mods, and whether the Workshop item can be named `DTMAPI` instead of `DTMAPI Runtime`.

## Changed Files

- `tools/scripts/build-release-workshop-packages.ps1`

## Summary

Updated the Runtime Workshop package generator so the player-facing package is named `DTMAPI` and includes root-level Workshop assets:

- `icon.png`
- `preview.png`

The runtime's internal role remains documented in the description and package metadata, but the Workshop title should now read simply as `DTMAPI`.

The generated staging folder is also `dist/workshop-packages/DTMAPI` rather than `DTMAPI_Runtime` so it matches the intended upload item name.

## Validation

- `git diff --check`
- PowerShell AST parse for `tools/scripts/build-release-workshop-packages.ps1`
- Regenerated the Runtime Workshop package
- Synced the package to `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI`
- Verified generated `info.json.name`, `icon.png`, `preview.png`, and release manifest commit

## Evidence

- Runtime package staging: `dist/workshop-packages/DTMAPI`
- Local upload path: `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI`

## Rollback

Restore the package output folder/name to `DTMAPI_Runtime`, remove the root-level image copies if necessary, regenerate the package, and resync the local upload folder.

## Follow-up

Keep the description explicit that DTMAPI is the runtime prerequisite even though the Workshop title is simply `DTMAPI`.
