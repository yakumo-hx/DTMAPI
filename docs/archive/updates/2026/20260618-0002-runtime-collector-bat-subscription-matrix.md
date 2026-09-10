# 20260618-0002 Runtime Collector Bat And Subscription Matrix

Date: 2026-06-18

Area: DTMAPI Runtime installer, offline log collection, Workshop subscription validation

Source request: after checking the Steam subscription package hashes, run parallel sub-agent pressure tests against the subscription package, fix player-facing issues, and persist the upload/subscription test matrix as a durable document.

## Changed Files

- `tools/release/runtime-workshop/4_collect_dtmapi_logs.bat`
- `tools/release/runtime-workshop/3_check_dtmapi_status.bat`
- `tools/scripts/collect-logs.ps1`
- `tools/scripts/common.ps1`
- `tools/scripts/install-bepinex.ps1`
- `tools/scripts/install-to-game.ps1`
- `docs/workflows/workshop-package-subscription-test-matrix.md`
- `docs/updates/INDEX.md`
- `docs/debug/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`

## Summary

- Fixed the root `4_collect_dtmapi_logs.bat` timestamp path by moving Desktop timestamp output selection into `collect-logs.ps1 -DesktopTimestampOutput`. This avoids fragile CMD nested quoting before `collect-logs.ps1` starts.
- Made `collect-logs.ps1 -DesktopTimestampOutput` mutually exclusive with explicit `-OutputDirectory` so future callers cannot silently override a requested output location.
- Added a clearer `3_check_dtmapi_status.bat` message for the after-uninstall case, where missing DTMAPI runtime files are expected.
- Changed `Resolve-DolocTownGamePath` so an explicit but invalid `DTMAPI_GAME_DIR` or `local.settings.json` `GameDir` fails early instead of silently falling back to a real Steam install directory.
- Guarded `install-to-game.ps1` early trap handling so path-resolution failures report the original cause instead of trying to call the failure-state writer before it is defined.
- Replaced `install-bepinex.ps1` use of `Get-FileHash` with a .NET SHA256 implementation so Windows PowerShell hosts without the cmdlet can still verify the bundled offline zip.
- Added a durable upload/subscription package pressure-test matrix under `docs/workflows/`.

## Validation

- Windows PowerShell 5.1 parser check passed for the eight installer/helper scripts: `PARSE_ERRORS=0`.
- PowerShell 7 parser check passed after the fix: `PWSH_PARSE_ERRORS=0`.
- Windows PowerShell 5.1 direct `install-bepinex.ps1` test passed without `Get-FileHash`, installing from the verified local BepInEx zip and printing SHA256 `82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4`.
- Invalid explicit `DTMAPI_GAME_DIR=Z:\definitely-missing-dtmapi-test\Doloc Town` now exits `1` with the clear message `DTMAPI_GAME_DIR is set but does not exist`, and no longer falls back to the real Steam game path.
- Temporary runtime package generation passed with `build-release-workshop-packages.ps1 -SkipBuild -RuntimeOnly -OutputRoot <temp path with spaces and Chinese text>`.
- The generated package's root `4_collect_dtmapi_logs.bat` was run through `cmd.exe /d /c call "<package>\4_collect_dtmapi_logs.bat" <NUL` with `DTMAPI_GAME_DIR` pointing at a temp fake game path containing spaces and Chinese text. It exited `0` and printed a Desktop `DTMAPI-logs\yyyyMMdd-HHmmss` output path.
- The generated package's `install-to-game.ps1 -InstallBepInEx -SkipOfficialLocalMods` installed into a temp fake game path containing spaces and Chinese text, and packaged `check-dtmapi-status.ps1 -GameDir <temp>` exited `0` with `[OK] Required DTMAPI install files are present.`
- Root bat wrapper matrix from a generated package path containing spaces and Chinese text passed: `1_install_dtmapi.bat` exit `0`, installed-state `3_check_dtmapi_status.bat` exit `0`, `4_collect_dtmapi_logs.bat` exit `0`, `2_uninstall_dtmapi.bat` exit `0`, and post-uninstall `3_check_dtmapi_status.bat` exit `1` with the expected after-uninstall guidance.
- The fixed DTMAPI Runtime package was synced to the official local upload folder `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`.
- Source/local-upload hashes matched for root bat launchers, installer helper scripts, runtime payload DLLs, the DTMAPI title icon asset, and the bundled BepInEx zip. Verified BepInEx zip SHA256: `82F9878551030F54657792C0740D9D51A09500EEAE1FBA21106B0C441E6732C4`.
- Steam subscription hash parity remains pending until the fixed package is manually uploaded and the subscription copy is refreshed. A post-sync read of `D:\steam\steamapps\workshop\content\2285550\3743016467` still showed stale subscribed files: `3_check_dtmapi_status.bat`, `4_collect_dtmapi_logs.bat`, `install-bepinex.ps1`, `install-to-game.ps1`, and `collect-logs.ps1` differed from the local upload package, while `1_install_dtmapi.bat` and the bundled BepInEx zip already matched.

## Related Records

- Debug index: `docs/debug/INDEX.md`
- Regression row: `RUNTIME-COLLECTOR-BAT-SUBSCRIPTION-20260618`
- Test matrix: `docs/workflows/workshop-package-subscription-test-matrix.md`

## Evidence

- Previous subscription pressure test found that current subscription/package scripts parsed cleanly on Windows PowerShell 5.1 and PowerShell 7, and that explicit true temp installs with `-InstallBepInEx` succeeded.
- The same pressure test found the root `4_collect_dtmapi_logs.bat` wrapper could fail before calling `collect-logs.ps1` when launched from a path with spaces.
- Follow-up temp package evidence after the fix showed `COLLECT_EXIT=0 INSTALL_EXIT=0 CHECK_EXIT=0`.
- Official local upload sync evidence showed root bats, installer helpers, runtime payload DLLs, the icon asset, and bundled BepInEx zip matching source. Representative hash prefixes: `1_install_dtmapi.bat=0358F14F1BCA`, `install-bepinex.ps1=5D73C5C9EE50`, `collect-logs.ps1=88BAF6674EFF`, `DTMAPI.GameBridge.DolocTown.dll=EAB89D969C3E`, `BepInEx zip=82F987855103`.
- Current subscription read after local sync showed stale subscribed helper files: subscription `3_check_dtmapi_status.bat=3CB10B32E458`, `4_collect_dtmapi_logs.bat=7FBCA6737EBB`, `install-bepinex.ps1=78A5AD9E0555`, `install-to-game.ps1=93C0F6A4ADA5`, and `collect-logs.ps1=1834E0ABC9B2`, while local upload has the fixed hashes listed above.

## Rollback

Restore the prior `4_collect_dtmapi_logs.bat` timestamp command and the prior game-path fallback logic. This would reintroduce the path-with-spaces collector risk and the invalid-explicit-path fallback risk.

## Follow-Up

- Keep `dist\workshop-packages` out of manual upload decisions unless it was freshly rebuilt in the same turn.
- Consider making `3_check` classify "not installed after uninstall" separately from "broken install" if player support screenshots keep being confusing.
