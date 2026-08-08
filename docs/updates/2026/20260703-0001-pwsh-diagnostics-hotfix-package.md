# 20260703-0001 Player Hotfix Pwsh Diagnostics Package

## Summary

Built a standalone DTMAPI Runtime hotfix package for a repeated player installer failure where the Steam subscription package still ran under Windows PowerShell 5.1 and reported `common.ps1` validation failure without enough host details to diagnose the real cause.

## Source Request

A player screenshot from 2026-07-03 showed `3_check_dtmapi_status.bat` selecting:

- `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe`

The status check found all BepInEx/DTMAPI runtime files missing, and the latest failed install state reported:

- `PowerShell syntax validation failed for ...\DTMAPIInstaller\tools\common.ps1`
- `Script cannot run in the current PowerShell environment.`

The user asked for a standalone delivery package that adds extra `pwsh` detection and always returns clearly how installation succeeded or failed.

## Changed Files

- `tools/release/runtime-workshop/1_install_dtmapi.bat`
- `tools/release/runtime-workshop/2_uninstall_dtmapi.bat`
- `tools/release/runtime-workshop/3_check_dtmapi_status.bat`
- `tools/release/runtime-workshop/4_collect_dtmapi_logs.bat`
- `tools/scripts/check-dtmapi-status.ps1`
- `tools/scripts/common.ps1`
- `tools/scripts/release-common.ps1`
- `docs/updates/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`

Generated standalone package artifacts:

- `dist/player-hotfix-20260703-pwsh-diagnostics/DTMAPI`
- `dist/DTMAPI-player-hotfix-20260703-pwsh-diagnostics.zip`

## Behavior

- Root `.bat` wrappers now probe fixed PowerShell 7 install paths before Windows PowerShell:
  - `%ProgramFiles%\PowerShell\7\pwsh.exe`
  - `%ProgramFiles(x86)%\PowerShell\7\pwsh.exe`
  - `where pwsh.exe`
  - Windows PowerShell fixed path and PATH lookup
- The probe validates the relevant packaged scripts for each entry point, not just `common.ps1`.
- Probe output now prints the candidate host, PowerShell version/edition, each checked script, parser errors when present, and `Probe OK` when the host is usable.
- Each root wrapper prints the selected host and the script it is about to run.
- Success paths now end with `[OK] ... finished` and `[OK] Host used: ...`.
- Failure paths now end with `[ERROR] ... failed`, `[ERROR] Host used: ...`, `[ERROR] Exit code: ...`, and a direction to include the whole output plus any printed `install-state.failed-*.json`.
- Script-side host helpers now also discover fixed PowerShell 7 install paths when PATH does not expose `pwsh.exe`.
- Status check now compares `install-state.failed-*.json` with the latest successful `install-state.json`; an older failure retained for diagnostics is printed as `[INFO] Previous failed install state found before the latest successful install` instead of a current `[WARN]`.

## Validation

- Windows PowerShell 5.1 parser validation passed for the install chain with Core fallback enabled.
- Runtime package generation passed:
  - `tools/scripts/build-release-workshop-packages.ps1 -SkipBuild -RuntimeOnly -OutputRoot dist/player-hotfix-20260703-pwsh-diagnostics`
- Standalone subscription-package matrix passed with `Blockers: 0`:
  - missing game dir: friendly failure
  - empty/non-game dir: friendly failure
  - valid game-shaped temp dir: install passed
  - post-install check: passed
  - log collection with no runtime logs: passed
  - uninstall: passed
  - post-uninstall check: expected failure because runtime files were removed
- PATH-stripped host discovery test passed:
  - with `PATH=C:\Windows\System32`, `3_check_dtmapi_status.bat` selected `C:\Program Files\PowerShell\7\pwsh.exe`
  - the output printed version `7.6.2 Core`, checked scripts, `Probe OK`, selected host, and explicit failure details for the intentionally missing temp game dir
- Stale failure-state status test passed:
  - after a successful temp install, a retained older `install-state.failed-*.json` was reported as `[INFO] Previous failed install state found before the latest successful install`
  - the same check still ended with `[OK] Required DTMAPI install files are present`, `[OK] DTMAPI status check finished`, and `[OK] Host used: C:\Program Files\PowerShell\7\pwsh.exe`

Evidence:

- `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-hotfix-audit-20260703-stale-state\DTMAPI Workshop Audit 20260703-135809\Results\stress-summary.md`
- `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-hotfix-audit-20260703-stale-state\DTMAPI Workshop Audit 20260703-135809\Results\04-install-valid-game-dir.out.txt`
- `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-hotfix-audit-20260703-stale-state\DTMAPI Workshop Audit 20260703-135809\Results\05-check-after-install.out.txt`
- `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-hotfix-audit-20260703-stale-state\DTMAPI Workshop Audit 20260703-135809\Results\10-check-with-stale-failure.out.txt`

Standalone zip:

- `dist/DTMAPI-player-hotfix-20260703-pwsh-diagnostics.zip`
- SHA256: `10AB93C2BFF0728E4FCC8405078A87A5B336A3B6CE7CB6901D306EBC61D3E898`

Packaged file spot checks:

- `1_install_dtmapi.bat` SHA256: `53DA7A583CD3A41576362E0591788F85E6C414D650C0C2F61EADB24E90BA4C2A`
- `Content\DTMAPIInstaller\tools\common.ps1` SHA256: `8046283A1CE21BE433D2A7F4CF099092341781053840B2B45CC51B1A68996999`
- `Content\DTMAPIInstaller\tools\check-dtmapi-status.ps1` SHA256: `73CD10C1161277E0AC382E33178CA0F32C12E782EAE46C6E8D96FF4EA8C03CBC`

## Not Run

- No live Doloc Town game smoke was run because this change is an installer/package diagnostic hotfix only.
- The local official upload folder and Steam Workshop upload/subscription package were not synchronized in this update.

## Rollback

Restore the previous root `.bat` host-selection/probe blocks and remove fixed-path PowerShell 7 discovery from `Get-DtmApiPowerShellHost` / `Test-DtmApiWindowsPowerShellSyntax`. This would restore the weaker diagnostics, so rollback is not recommended unless the new fixed-path probe causes a player-specific launch regression.

## Follow-Up

- If the player still fails with this hotfix, use the printed host, version, checked-script list, exit code, and `install-state.failed-*.json` path as the primary root-cause evidence.
- After the standalone package is manually confirmed, merge/sync this wrapper behavior into the official upload package and then compare the Steam subscription copy against the local upload folder.
