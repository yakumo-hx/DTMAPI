# 20260618-0003 Player Hotfix PowerShell Host Fallback

## Summary

Built a standalone DTMAPI Runtime hotfix package for a player installer failure where Windows PowerShell reported only the wrapper `throw $message` line instead of the real script parser output, then promoted the same installer strategy into the source package and synchronized the official local DTMAPI upload folder.

## Source Request

Player screenshots showed `1_install_dtmapi.bat` failing with:

- `Windows PowerShell syntax validation failed for ...\DTMAPIInstaller\tools\common.ps1`
- failure displayed at `release-common.ps1` wrapper line instead of the real parser/host output
- `3_check_dtmapi_status.bat` correctly reported no DTMAPI/BepInEx runtime after the failed install

The requested fix was a standalone package that either installs successfully or prints enough diagnostic detail, with host capability probing and `pwsh.exe` fallback.

## Changed Files

- `tools/release/runtime-workshop/1_install_dtmapi.bat`
- `tools/release/runtime-workshop/2_uninstall_dtmapi.bat`
- `tools/release/runtime-workshop/3_check_dtmapi_status.bat`
- `tools/release/runtime-workshop/4_collect_dtmapi_logs.bat`
- `tools/scripts/common.ps1`
- `tools/scripts/install-to-game.ps1`
- `tools/scripts/check-dtmapi-status.ps1`
- `tools/scripts/release-common.ps1`

## Behavior

- Root `.bat` wrappers now probe a PowerShell host before selecting it.
- The root `.bat` probe validates the packaged `common.ps1`, so a broken Windows PowerShell host is skipped before install/check/log scripts run.
- Strict script validation now preserves per-host parser/host output instead of only surfacing the wrapper throw site.
- Strict validation tries Windows PowerShell first and then `pwsh.exe` when available before failing.
- Successful fallback validation is printed as `[INFO]`, not a yellow warning; only total validation failure remains a hard error.
- Invalid `DTMAPI_GAME_DIR`, `local.settings.json GameDir`, or Steam-discovered paths must contain `DolocTown.exe` and `DolocTown_Data` before install proceeds.

## Validation

- Windows PowerShell 5.1 parser check passed for packaged installer scripts.
- Release runtime package generation passed.
- Standalone package matrix passed after extracting the zip into a path with spaces and Chinese characters:
  - missing game dir: friendly failure
  - empty/non-game dir: friendly failure
  - valid game-shaped temp dir: install passed
  - post-install check: passed
  - log collection with no runtime logs: passed
  - uninstall and post-uninstall check: no parser/strict-mode crash

Evidence:

- `dist/player-hotfix-20260618-powershell-host/evidence/DTMAPI Workshop Audit 20260618-230234/Results/stress-summary.md`
- `dist/player-hotfix-20260618-powershell-host/evidence/DTMAPI Workshop Audit 20260618-230452/Results/stress-summary.md`
- `dist/player-hotfix-20260618-powershell-host/evidence/DTMAPI Workshop Audit 20260618-231149/Results/stress-summary.md`
- `dist/player-hotfix-20260618-powershell-host/evidence/DTMAPI Workshop Audit 20260618-233510/Results/stress-summary.md`
- `dist/local-upload-audit-20260619/DTMAPI Workshop Audit 20260619-002118/Results/stress-summary.md`

Fallback simulation:

- A temporary copied package had `common.ps1` prefixed with PowerShell 7-only syntax, then `3_check_dtmapi_status.bat` selected `C:\Program Files\PowerShell\7\pwsh.exe` directly instead of the Windows PowerShell host.

Standalone player zip:

- `dist/DTMAPI-player-hotfix-20260618-powershell-host.zip`

Local upload package:

- `%USERPROFILE%\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`
- Synced under runtime lock on 2026-06-19.
- Checked hash parity for root `.bat` files, installer tools, and DTMAPI runtime DLL payloads.

## Rollback

Restore the previous installer wrapper host-selection blocks and remove `-AllowCoreFallback` script-validation calls. This would also restore the weaker game-directory check, so rollback is not recommended unless a new host-selection regression is found.

## Follow-Up

After manual Workshop upload, compare the Steam subscription package against the local upload folder using `docs/workflows/workshop-package-subscription-test-matrix.md`.
