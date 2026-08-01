# Runtime Offline BepInEx Package

Date: 2026-06-17
Status: verified-local-package

## Source

- User requested bundling `BepInEx_win_x64_5.4.23.5.zip` in the DTMAPI installer package, updating the local upload package, appending `update 0617 / 优化离线安装` to the DTMAPI description, committing the result, and hash-checking source vs upload output.
- Known debug context: independent player reports showed first-run support needs clearer offline/log collection behavior. The previous installer path could fail on `Invoke-RestMethod` to GitHub before any local cache was usable.

## Changed Files

- `tools/release/bootstrap/BepInEx_win_x64_5.4.23.5.zip`
- `.gitignore`
- `tools/scripts/install-bepinex.ps1`
- `tools/scripts/build-release-workshop-packages.ps1`
- `tools/release/dtmapi-mod-publish-zh.json`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

## Summary

- Added the fixed BepInEx 5 x64 zip as a committed release asset.
- Changed `install-bepinex.ps1` from dynamic GitHub release lookup to fixed-version local-first installation:
  - package cache: `.tools/bepinex/BepInEx_win_x64_5.4.23.5.zip`;
  - source-tree fallback: `tools/release/bootstrap/BepInEx_win_x64_5.4.23.5.zip`;
  - network fallback: fixed GitHub release URL only if no local package is present;
  - SHA256 verification is required before extraction.
- Changed runtime Workshop package generation to include the zip under `Content/.tools/bepinex/`.
- Appended DTMAPI Runtime description notes in Simplified Chinese, Traditional Chinese, and English:
  - Simplified Chinese: `update 0617` / `优化离线安装。`
  - Traditional Chinese: `update 0617` / `優化離線安裝。`
  - English: `update 0617` / `Improved offline installation.`

## Validation

- Passed: `tools/scripts/test.ps1 -Configuration Release` with `DTMAPI.UnitTests: OK`.
- Passed: `git diff --check` reported line-ending warnings only.
- Passed: PowerShell parser syntax check for:
  - `tools/scripts/install-bepinex.ps1`
  - `tools/scripts/build-release-workshop-packages.ps1`
- Passed: runtime package rebuilt to `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`.
- Passed: temporary offline install simulation by running packaged `Content/DTMAPIInstaller/tools/install-bepinex.ps1` with `DTMAPI_GAME_DIR` set to a temp directory. The generated install summary reported:
  - `Release: v5.4.23.5`
  - `Asset: BepInEx_win_x64_5.4.23.5.zip`
  - `Source: C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI\Content\.tools\bepinex\BepInEx_win_x64_5.4.23.5.zip`
  - `Sha256: 82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4`
  - installed items: `BepInEx`, `.doorstop_version`, `changelog.txt`, `doorstop_config.ini`, `winhttp.dll`.
- Passed: source vs local upload hash checks:
  - BepInEx zip: source/upload `82F987855103`
  - `install-bepinex.ps1` normalized text: source/upload `C493EB8BC86F`
  - `DTMAPI.BepInExBootstrap.dll`: source/upload `FA09585F0644`
  - `DTMAPI.Abstractions.dll`: source/upload `2BD1F6EFF849`
  - `DTMAPI.Core.dll`: source/upload `2F2475AE5340`
  - `DTMAPI.GameBridge.DolocTown.dll`: source/upload `2D118CB7E2A8`
  - `DTMAPI.ModConfigMenu.dll`: source/upload `79B4A5401DE0`
- Passed: local upload `info.json` description and localized descriptions match `tools/release/dtmapi-mod-publish-zh.json`, contain `update 0617 / 优化离线安装`, and preserve `workshop.json`.
- Not run: game startup smoke. This change is installer/package layout only and was verified through Release tests plus an offline installer simulation against a temporary game directory.

## Evidence Notes

- The old dynamic release query was the real fragile point. A player with no GitHub API connectivity could fail before any cached BepInEx zip helped.
- The release package now has a deterministic offline path and a deterministic fallback URL; the installer no longer needs GitHub API access to determine which BepInEx asset to install.
- The bundled zip adds approximately `639,118` bytes to the runtime package.

## Rollback

- Remove `tools/release/bootstrap/BepInEx_win_x64_5.4.23.5.zip`.
- Revert `install-bepinex.ps1` to dynamic GitHub release lookup.
- Revert the package-copy block in `build-release-workshop-packages.ps1`.
- Remove the `update 0617` description note from runtime publish metadata.

## Follow-Up

- If players still report installer connection failures, ask for the exact script line and whether `Content/.tools/bepinex/BepInEx_win_x64_5.4.23.5.zip` exists in their subscribed DTMAPI folder.
- Consider replacing the player-facing “切换网络” wording in a future description pass now that the bundled offline package should cover the common first-install failure.
