# 20260703-0002 Install Preflight Probe

## Summary

Added a probe-only DTMAPI Runtime diagnostic entry point that replays the installer preflight checks without installing, uninstalling, copying runtime files, writing game state, or launching Doloc Town.

## Source Request

After a player install failure was bypassed by using PowerShell 7, the remaining unknown was the exact pre-install step that failed under the old Steam subscription package. The requested tool was a pure probe program that:

- does not install files
- reruns all pre-install checks
- prints exactly which step got stuck or failed
- skips later dependent checks when a step fails, except for the first entered/bootstrap step
- prints and writes a final record

## Changed Files

- `tools/release/runtime-workshop/0_probe_dtmapi_install.bat`
- `tools/scripts/probe-install-preflight.ps1`
- `tools/scripts/build-release-workshop-packages.ps1`
- `docs/updates/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`

Generated artifacts:

- `dist/player-probe-20260703-preflight/DTMAPI`
- `dist/DTMAPI-player-probe-20260703-install-preflight/DTMAPI-InstallProbe`
- `dist/DTMAPI-player-probe-20260703-install-preflight.zip`

## Behavior

- `0_probe_dtmapi_install.bat` selects a host with the same fixed PowerShell 7 first strategy as the hotfix installer wrappers.
- The probe root package contains no `1_install_dtmapi.bat`, so the probe-only zip has no install entry point.
- The probe prints step-by-step status with `OK`, `WARN`, `FAIL`, and `SKIPPED`.
- The probe records a JSON report named `dtmapi-install-preflight-probe-YYYYMMDD-HHMMSS.json`.
- Report writing prefers the probe package root and falls back to `%TEMP%`.
- The probe does not call `install-to-game.ps1`, `install-bepinex.ps1`, `uninstall-dtmapi.ps1`, copy helpers, remove helpers, or game-launch helpers.

Probe steps:

- package layout and current probe host
- PowerShell host and parser matrix for Windows PowerShell and PowerShell 7 candidates
- Doloc Town game folder resolution
- game process running check
- package runtime payload file check
- current BepInEx/Doorstop state
- bundled BepInEx offline zip presence and SHA256
- existing DTMAPI install/failure state inspection
- legacy DLK/SMAPI detection
- planned install destination summary

## Validation

- Windows PowerShell/Core parser validation passed for:
  - `tools/scripts/probe-install-preflight.ps1`
  - `tools/scripts/build-release-workshop-packages.ps1`
- Runtime package generation passed:
  - `tools/scripts/build-release-workshop-packages.ps1 -SkipBuild -RuntimeOnly -OutputRoot dist/player-probe-20260703-preflight`
- Probe-only fake valid game test passed:
  - exit code `0`
  - report JSON written
  - `1_install_dtmapi.bat` absent from the probe-only package
  - no `BepInEx\plugins\DTMAPI` directory created
  - no game `DTMAPI` state directory created
- Missing game dir test passed:
  - exit code `1`
  - game folder resolution reported `FAIL`
  - game-dependent checks reported `SKIPPED`
  - package payload and bundled BepInEx checks still ran
  - report JSON written
- Simulated Windows PowerShell parser failure passed:
  - temporary package `common.ps1` was given PowerShell 7-only syntax
  - Windows PowerShell parser matrix reported `FAIL common.ps1`
  - PowerShell 7 parser matrix passed
  - probe reported `Windows PowerShell failed strict pre-install parsing, but at least one fallback host passed`
  - later checks still ran and report JSON was written
- Full runtime package matrix with the new probe included passed with `Blockers: 0`.

Evidence:

- `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-probe-only-test-20260703\probe-only-valid.out.txt`
- `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-probe-test-20260703\probe-missing-game.out.txt`
- `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-probe-pwsh-fallback-test-20260703\probe-pwsh-fallback.out.txt`
- `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-probe-release-audit-20260703\DTMAPI Workshop Audit 20260703-141141\Results\stress-summary.md`

Standalone probe-only zip:

- `dist/DTMAPI-player-probe-20260703-install-preflight.zip`
- SHA256: `D0B9C1FCB5C1CEC598FC4FA75FE0A4128262A409762AEF1D10F0D5F94090B93A`

## Not Run

- No live Doloc Town game smoke was run because this is an installer preflight diagnostic probe only.
- The local official upload folder and Steam Workshop upload/subscription package were not synchronized in this update.

## Rollback

Remove `0_probe_dtmapi_install.bat`, remove `probe-install-preflight.ps1`, and remove both files from `build-release-workshop-packages.ps1`. This would not affect the existing install/check/uninstall wrappers, but it would remove the no-install diagnostic path.

## Follow-Up

- If a player still has a bad Windows PowerShell environment, run the probe-only zip first and inspect the JSON host/parser matrix before asking them to run the installer.
- After probe behavior is manually useful for a player failure, consider including `0_probe_dtmapi_install.bat` in the official upload package as a support-only entry point.
