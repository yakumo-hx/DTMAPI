# 20260618-0001 Installer Helper Validation Split

## Summary

Split DTMAPI Runtime installer validation into a strict pre-install install-chain check and warning-only post-install/helper checks. This prevents diagnostics and support helpers like `check-dtmapi-status.ps1`, `collect-logs.ps1`, and `analyze-startup-evidence.ps1` from blocking `1_install_dtmapi.bat`.

## Source Request

Player screenshots showed `1_install_dtmapi.bat` failing before install because Windows PowerShell syntax validation treated `analyze-startup-evidence.ps1` as a required install preflight script. User also asked whether BepInEx staying after uninstall meant an independent BepInEx install.

## Changed Files

- `tools/scripts/common.ps1`, `release-common.ps1`, `install-to-game.ps1`, `install-bepinex.ps1`, `uninstall-dtmapi.ps1`, `check-dtmapi-status.ps1`, `collect-logs.ps1`, `analyze-startup-evidence.ps1`
- `tools/scripts/build-release-workshop-packages.ps1`
- `tools/release/runtime-workshop/1_install_dtmapi.bat`, `2_uninstall_dtmapi.bat`, `3_check_dtmapi_status.bat`, `4_collect_dtmapi_logs.bat`

## Notes

- Synced the newer local-upload installer helper chain back into source so future packages do not regress recent failure-state, offline-install, helper, and collect-log fixes.
- `install-to-game.ps1` now validates only the install chain strictly before install: `common.ps1`, `release-common.ps1`, `install-to-game.ps1`, and `install-bepinex.ps1`.
- `uninstall-dtmapi.ps1`, `check-dtmapi-status.ps1`, `collect-logs.ps1`, and `analyze-startup-evidence.ps1` are post-install/support helpers. Missing or parser-broken copies warn but do not block `1_install_dtmapi.bat`.
- `check-dtmapi-status.ps1` aggregates BepInEx/Doorstop and DTMAPI runtime checks, treats installed helper scripts as optional support files, and skips install-state/helper checks when no DTMAPI install footprint exists.
- `Test-DtmApiWindowsPowerShellSyntax -WarningOnly` suppresses child parser stderr/stdout, emits concise warning details, and can return per-file results so status checks no longer print OK when optional helper parser warnings exist.
- `build-release-workshop-packages.ps1` now uses the same split as player install: only the install chain is strict; uninstall/check/log/analyzer support helpers are warning-only during package generation.
- `install-bepinex.ps1` now treats BepInEx as already installed only when the full Doorstop/BepInEx required file set and config are complete, not merely when `BepInEx.dll` exists.
- `Resolve-DolocTownGamePath` no longer fails under strict mode when `local.settings.json` exists but lacks `GameDir`.
- Runtime `.bat` launchers now print the PowerShell host they use and prefer the explicit Windows PowerShell 5.1 path before PATH `powershell.exe` and `pwsh.exe` fallback, so installing PowerShell 7 does not silently change or confuse the default player path.
- Fixed a `-DryRun` strict-mode bug where an empty legacy-detection result lacked `.Count`.
- The uninstall-state evidence shows `RemoveBepInExRequested=false`; default uninstall intentionally removes DTMAPI plugin/state and keeps BepInEx/Doorstop. The backed-up install state showed `BepInExDetectedBeforeInstall=true` and `BepInExInstalledByDTMAPI=false`, so the checked local game already had BepInEx before that install and the default uninstall correctly did not remove it.

## Validation

- Windows PowerShell 5 parser validation passed for the source installer scripts and runtime package scripts.
- PowerShell 7 parser validation passed for the same installer/support script set.
- Temporary install-script copies with intentionally broken post-install/support helpers produce warnings and complete `install-to-game.ps1 -DryRun -SkipOfficialLocalMods` with exit code 0; five parallel pressure-test passes cover broken check, broken uninstall, broken collect/analyze, missing support helpers, and no-install/after-uninstall status output.
- PowerShell 5.1 and PowerShell 7 AST parsing both pass for the script set in the local environment; package-local dry-runs also confirm broken `check-dtmapi-status.ps1` / `uninstall-dtmapi.ps1` no longer block player install preflight.
- Package-local dry-run with missing strict `install-bepinex.ps1` exits 1, proving the install-chain hard-fail boundary still works.
- A fake installed helper parser error now makes `check-dtmapi-status.ps1` print `WARN Installed helper scripts have optional syntax warnings`, not a misleading helper OK line.
- Direct `install-bepinex.ps1` against a temp game containing only a fake `BepInEx\core\BepInEx.dll` repaired the partial install and wrote `BepInEx`, `.doorstop_version`, `changelog.txt`, `doorstop_config.ini`, and `winhttp.dll`.
- `Resolve-DolocTownGamePath` with `{}` `local.settings.json` no longer throws a strict-mode missing-property error and continues normal path discovery.
- Synced the fixed scripts to `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI` under the runtime lock.
- Temporary runtime package generation with `build-release-workshop-packages.ps1 -SkipBuild -RuntimeOnly` passed. Source/local-upload SHA256 prefixes matched for the eight installer scripts and four runtime bat files, including `common.ps1=C6084CADE89F`, `release-common.ps1=3F6CD9F0E97A`, `install-to-game.ps1=93C0F6A4ADA5`, `install-bepinex.ps1=78A5AD9E0555`, `check-dtmapi-status.ps1=138E8CE1CEEC`, `1_install_dtmapi.bat=0358F14F1BCA`, and `4_collect_dtmapi_logs.bat=7FBCA6737EBB`.
- `git diff --check -- tools/scripts tools/release/runtime-workshop` reported line-ending warnings only.

## Rollback

Revert this update and rebuild the runtime package if strict installer validation should again include post-install/support helpers. That would restore the previous behavior where optional support scripts can block install.

## Follow-Up

- Consider adding an explicit player-facing uninstall option for "remove DTMAPI only" versus "remove DTMAPI and BepInEx" if support requests keep interpreting retained BepInEx as a failed uninstall.
- A full game launch smoke was not run because this change is installer/package validation only and does not touch runtime DLLs or hooks.
