# 20260612-0014 - Installer Windows PowerShell Compatibility

Status: verified
Date: 2026-06-12
Branch: `codex/bottom-layer-refactor-audit-20260612`
Source request: User reported player-side installer failures in `Content/DTMAPIInstaller/tools/release-common.ps1` with Chinese Windows PowerShell parser errors around package names, then asked to directly update the installer into a compatible form.

## Changed Files

- `tools/scripts/release-common.ps1`
- `tools/scripts/build-release-workshop-packages.ps1`
- `tools/scripts/install-to-game.ps1`
- `docs/updates/INDEX.md`
- `docs/updates/2026/20260612-0014-installer-windows-powershell-compat.md`

## Summary

- Added UTF-8 BOM text writing helpers for installer PowerShell scripts so Windows PowerShell 5.1 can read non-ASCII script content consistently.
- Changed Runtime Workshop package staging to copy installer `.ps1` tools through the UTF-8 BOM writer instead of raw `Copy-Item`.
- Changed installed local support tools copied by `install-to-game.ps1` to also use UTF-8 BOM output.
- Added a Windows PowerShell syntax validation helper that invokes `powershell.exe` and parses staged scripts with the PowerShell AST parser.
- The validator uses `-EncodedCommand` to avoid command-line quoting issues with paths, braces, and localized parser output.
- Preserved existing root-level `workshop.json` when regenerating package folders so local upload directories keep their Steam Workshop item binding.

## Validation

- Ran Windows PowerShell syntax validation on:
  - `tools/scripts/release-common.ps1`
  - `tools/scripts/build-release-workshop-packages.ps1`
  - `tools/scripts/install-to-game.ps1`
- Ran Runtime Workshop staging with `tools/scripts/build-release-workshop-packages.ps1 -SkipBuild -RuntimeOnly`.
- Ran full Runtime Workshop package generation to the local upload root `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS`.
- Re-ran package generation with `-SkipBuild -RuntimeOnly` after restoring `DTMAPI/workshop.json` to verify the generator preserves the Workshop item binding.
- Verified staged installer tools under `Content/DTMAPIInstaller/tools` are written with `EF BB BF` BOM:
  - `check-dtmapi-status.ps1`
  - `common.ps1`
  - `install-bepinex.ps1`
  - `install-to-game.ps1`
  - `release-common.ps1`
  - `uninstall-dtmapi.ps1`
- No game smoke was run because this change only affects installer/package script encoding and parser compatibility.

## Evidence

- Temporary staging output: `%TEMP%/dtmapi-workshop-compat-test2`.
- Successful validation output included `WindowsPowerShell syntax OK` and BOM checks of `EF BB BF` for every staged installer `.ps1`.
- Checked the currently subscribed Workshop folder `D:/Steam/steamapps/workshop/content/2285550/3743016467`: `info.json` reports `0.5.0-alpha`, packaged `.ps1` files have no UTF-8 BOM, and Windows PowerShell 5.1 reproduces the parser failure in `Content/DTMAPIInstaller/tools/release-common.ps1`.
- Copied that subscribed `release-common.ps1` to `%TEMP%`, added only UTF-8 BOM, and Windows PowerShell 5.1 parsed the temporary copy successfully.
- Local upload package generated at `%USERPROFILE%/AppData/LocalLow/RedSawGames/DolocTown/MODS/DTMAPI`.
- Final local upload package evidence: `info.json.version=0.5.1-alpha`, `workshop.json.workshop_id=3743016467`, every installer `.ps1` begins with `EF BB BF`, and Windows PowerShell 5.1 parses all installer tools successfully.

## Rollback

- Revert the UTF-8 BOM helper usage in the packaging/install scripts and remove the Windows PowerShell syntax validation helper.
- Revert the package-folder `workshop.json` preservation if generated local upload folders should always drop Workshop item bindings.
- Rebuild the Runtime Workshop package after rollback so the staged installer tools match source behavior.

## Follow-Up

- Rebuild and upload the Runtime Workshop item after this branch is merged so affected players receive regenerated installer scripts.
- If players still report failures, collect their exact `powershell.exe -Version` output and the generated `Content/DTMAPIInstaller/tools/release-common.ps1` bytes around the reported line.
