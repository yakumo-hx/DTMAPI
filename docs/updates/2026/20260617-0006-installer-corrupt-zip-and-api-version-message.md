# Installer Corrupt Zip And API Version Message

Date: 2026-06-17
Status: verified-local-package

## Source

- User provided player screenshots after the previous support fixes but before the offline package update:
  - `1_install_dtmapi.bat` failed inside `Microsoft.PowerShell.Archive` with `找不到中央目录结尾记录`, which is the typical error for an incomplete/corrupted zip archive.
  - `3_check_dtmapi_status.bat` then reported missing Doorstop/BepInEx/core files while DTMAPI plugin DLLs existed, matching a partial install after BepInEx extraction failed.
- User also reported player confusion around “api is too new”; the root issue is usually an old installed DTMAPI runtime with newly updated mods, not a too-new player API.

## Changed Files

- `src/DTMAPI.Core/Runtime/DtmApiRuntime.cs`
- `tests/DTMAPI.UnitTests/Program.cs`
- `tools/scripts/install-bepinex.ps1`
- `docs/api/public-api-matrix.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/hook-map/README.md`
- `docs/updates/INDEX.md`

## Summary

- Changed the player-facing `MinimumDTMApiVersion` failure message from a generic API-version mismatch to:
  - `DTMAPI 前置版本过旧。`
  - details explicitly say the mod requires a newer DTMAPI runtime and to run `1_install_dtmapi.bat`.
- Kept the internal diagnostics status code `api-too-new` unchanged for compatibility with existing diagnostics/smoke/status-code consumers.
- Hardened `install-bepinex.ps1` against damaged local zip caches:
  - verifies an existing cached zip before using it;
  - removes and replaces incomplete/corrupted cached zips;
  - copies the committed offline zip from `tools/release/bootstrap` when available;
  - catches `Expand-Archive` failures, clears zip/extract cache, and retries once;
  - reports the source and both extraction errors if recovery still fails.

## Validation

- Passed: PowerShell parser syntax check for `tools/scripts/install-bepinex.ps1`.
- Passed: `tools/scripts/test.ps1 -Configuration Release` with `DTMAPI.UnitTests: OK`.
- Passed: corrupt-cache installer simulation:
  - replaced `.tools/bepinex/BepInEx_win_x64_5.4.23.5.zip` with invalid text;
  - ran `tools/scripts/install-bepinex.ps1` with `DTMAPI_GAME_DIR` pointed to a temp directory;
  - installer warned `Cached BepInEx package is incomplete or corrupted; replacing it.`;
  - install summary source was `E:\Python_project\DTMAPI\tools\release\bootstrap\BepInEx_win_x64_5.4.23.5.zip`;
  - installed `BepInEx`, `.doorstop_version`, `changelog.txt`, `doorstop_config.ini`, and `winhttp.dll`;
  - original `.tools` cache was restored after the test.
- Passed: rebuilt the runtime local upload package to `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`.
- Passed: packaged offline installer simulation from the rebuilt local upload package with `DTMAPI_GAME_DIR` pointed to a temp directory. The install summary source was the package-local `Content\.tools\bepinex\BepInEx_win_x64_5.4.23.5.zip`.
- Passed: source vs local upload hash checks:
  - BepInEx zip: source/upload `82F987855103`
  - `install-bepinex.ps1` normalized text: source/upload `126A4042D317`
  - `DTMAPI.BepInExBootstrap.dll`: source/upload `E88BBDB95480`
  - `DTMAPI.Abstractions.dll`: source/upload `B39027114874`
  - `DTMAPI.Core.dll`: source/upload `5982266B5603`
  - `DTMAPI.GameBridge.DolocTown.dll`: source/upload `E936ACA7DBBD`
  - `DTMAPI.ModConfigMenu.dll`: source/upload `DEF38D3ADF5A`
- Passed: local upload `info.json` still contains `update 0617 / 优化离线安装` and preserves `workshop.json`.
- Not run: game startup smoke. This follow-up changes installer recovery and mod-load diagnostics wording; it was verified through Release tests, a corrupt-cache install simulation, a package-local offline install simulation, and source/upload hash checks.

## Evidence Notes

- `找不到中央目录结尾记录` is not a DTMAPI DLL problem. It means the zip reader reached the end of the archive without finding the zip central directory, usually because the zip is partial, corrupt, or not actually a zip.
- A partial install can leave `BepInEx/plugins/DTMAPI/*.dll` present while `winhttp.dll`, `doorstop_config.ini`, `BepInEx/core/BepInEx.dll`, the title icon asset, and DTMAPI install-state/release manifest remain missing.
- The old `api-too-new` status code is useful internally because it means the mod requires a future API surface. The player-facing action is the opposite wording: update the installed DTMAPI runtime.

## Rollback

- Revert the `CanLoadApiVersion` message/details text in `DtmApiRuntime.cs`.
- Revert the unit assertion update in `Program.cs`.
- Revert `install-bepinex.ps1` to single-pass hash/expand behavior.

## Follow-Up

- If player-facing UI still shows literal `api-too-new`, update the Manager/status page label mapping separately while preserving the internal status code.
- If players still hit archive corruption after the package-local zip is present, ask for `4_collect_dtmapi_logs.bat`, the package zip hash, and whether antivirus or cloud sync is rewriting files under the Workshop folder.
