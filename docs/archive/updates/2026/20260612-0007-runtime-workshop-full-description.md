# 20260612-0007 - Runtime Workshop Full Description

## Status

Verified

## Source Request

User clarified that the DTMAPI Runtime Workshop description must be complete, explaining what it is, what it does, how it differs from old DLKsmapi / DolocTownSMAPI, and that it will be maintained.

## Changed Files

- `tools/scripts/build-release-workshop-packages.ps1`

## Summary

Expanded the generated `DTMAPI_Runtime/info.json` description from a short Crops API note to a full player-facing Runtime description.

The new description states:

- DTMAPI Runtime is the prerequisite runtime for Doloc Town functional mods.
- It is based on BepInEx and provides DTMAPI Core, GameBridge, config menu, diagnostics, Manager status/report export, and mod APIs.
- The Runtime package itself is not a single gameplay mod; users subscribe and run `1_install_dtmapi.bat`.
- DTMAPI functional mods such as Zoom, ActionSpeed, OneActionComplete, MoreSaves, and Y key console depend on it.
- `0.5.0-alpha` adds the Experimental Crops / Harvesting API for crop-basin auto-harvest mod authors, while tree-basin auto-harvest is not implemented yet.
- DTMAPI is rebuilt from zero rather than copying old DLKsmapi / DolocTownSMAPI implementation.
- Install, uninstall, status check, log export, and mod enablement paths are clearer than the old runtime path.
- The project is a Developer Preview but will continue to be maintained.

The script stores the generated Chinese description as UTF-8 base64 so Windows PowerShell can parse the release script consistently while `info.json` still contains normal Chinese text.

## Validation

- `git diff --check`
- PowerShell AST parse for `tools/scripts/build-release-workshop-packages.ps1`
- Regenerated `dist/workshop-packages/DTMAPI_Runtime`
- Copied regenerated package to local upload path `MODS/DTMAPI_Runtime`
- Verified local upload `info.json.description` contains the full text and `release-manifest.json` points to the final source commit

## Evidence

- Runtime package staging: `dist/workshop-packages/DTMAPI_Runtime`
- Local upload path: `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI_Runtime`

## Rollback

Revert the `runtimeDescriptionBase64` value in `tools/scripts/build-release-workshop-packages.ps1`, regenerate the Runtime package, and resync the local upload directory.

## Follow-up

Keep future Workshop wording explicit that tree-basin/cocoa harvest execution needs a separate native-owner review before being advertised as implemented.
