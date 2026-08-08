# 20260703-0003 PowerShell File Validator

## Summary

Reclassified the player PowerShell 5.1 failure as likely installer validation incompatibility rather than DTMAPI script syntax failure, then replaced the internal `-EncodedCommand` parser validation path with a temporary validator script launched through `-File`.

## Source Request

The user noted that similar player machines could run standalone Windows PowerShell 5.1 self-check commands successfully, so the repeated `exit=-1` result might still be caused by DTMAPI's validation method.

The preflight probe report confirmed:

- Windows PowerShell 5.1 child parser checks returned `exit=-1` with no output for every packaged `.ps1`.
- PowerShell 7.6.3 parsed every packaged `.ps1` successfully.
- The same machine could run the DTMAPI status/probe script through PowerShell 7, and earlier screenshots showed Windows PowerShell could run DTMAPI status scripts far enough to print diagnostics.

That points to the nested `powershell.exe -EncodedCommand ... Parser.ParseFile(...)` validation route as the suspicious compatibility boundary.

## Changed Files

- `tools/scripts/release-common.ps1`
- `tools/scripts/probe-install-preflight.ps1`
- `docs/updates/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`

Generated artifacts:

- `dist/player-hotfix-20260703-pwsh-diagnostics-v2/DTMAPI`
- `dist/DTMAPI-player-hotfix-20260703-pwsh-diagnostics-v2.zip`
- `dist/player-probe-20260703-preflight-v2/DTMAPI`
- `dist/DTMAPI-player-probe-20260703-install-preflight-v2.zip`

## Behavior

- `Test-DtmApiWindowsPowerShellSyntax` now writes a temporary UTF-8 BOM validator script under `%TEMP%` and launches it with:
  - `powershell.exe -NoProfile -ExecutionPolicy Bypass -File <validator.ps1> -Path <target.ps1>`
- The old `-EncodedCommand` validator is no longer used by the installer/support validation path.
- The validator temp file is removed after each validation run.
- The preflight probe now records both methods:
  - effective `Ok` / `ExitCode` from the file-based validator
  - `EncodedCommandOk` / `EncodedCommandExitCode` from the old encoded-command route
- If Windows PowerShell passes file-based parsing but fails encoded-command parsing, the probe reports:
  - `Windows PowerShell file-based parsing passed, but EncodedCommand parsing failed; old installer validation may be incompatible with this environment.`

## Validation

- Windows PowerShell/Core syntax validation passed for updated:
  - `tools/scripts/release-common.ps1`
  - `tools/scripts/probe-install-preflight.ps1`
- Probe v2 fake valid game test passed:
  - exit code `0`
  - no install entry point in probe-only package
  - no `BepInEx\plugins\DTMAPI` directory created
  - no game `DTMAPI` state directory created
  - Windows PowerShell file-based parser and encoded parser both passed on the local test machine
- Full runtime package matrix for the v2 probe package passed with `Blockers: 0`.
- Full runtime package matrix for the v2 installer hotfix package passed with `Blockers: 0`.

Evidence:

- `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-probe-v2-test-20260703\probe-v2-valid.out.txt`
- `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-probe-v2-release-audit-20260703\DTMAPI Workshop Audit 20260703-143030\Results\stress-summary.md`
- `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-hotfix-v2-release-audit-20260703\DTMAPI Workshop Audit 20260703-143131\Results\stress-summary.md`

Standalone v2 installer zip:

- `dist/DTMAPI-player-hotfix-20260703-pwsh-diagnostics-v2.zip`
- SHA256: `616A917CCDF297AD192F2E237C135E64D5C5C0BBF40DB9581570148F1953C563`
- Packaged `Content\DTMAPIInstaller\tools\release-common.ps1` SHA256: `A5B67131DB7DB88D796FBDD5AD661EA5020B7E3F82B167622B1C3E08C717C8A4`

Standalone v2 probe-only zip:

- `dist/DTMAPI-player-probe-20260703-install-preflight-v2.zip`
- SHA256: `85EF4521921FDD1A8C37831C93D8951DFC8E6D2F9F002D7771440B8D472F30AF`
- Packaged `Content\DTMAPIInstaller\tools\probe-install-preflight.ps1` SHA256: `58A10338C3B9FD153AA7161C2505995E7A5C2E02AAA154FB8BA2904E9DE503CC`

## Not Run

- No live Doloc Town game smoke was run because this change affects installer preflight validation only.
- The local official upload folder and Steam Workshop upload/subscription package were not synchronized in this update.

## Rollback

Restore `Test-DtmApiWindowsPowerShellSyntax` to launch the parser check through `-EncodedCommand` and remove the probe's file-validator comparison fields. This is not recommended because player evidence suggests `-EncodedCommand` can fail with `exit=-1` even when the scripts and PowerShell 7 fallback are fine.

## Follow-Up

- Ask the affected player to run the v2 probe-only package. If Windows PowerShell `EncodedCommandOk=false` but file-based `Ok=true`, the root cause is DTMAPI's former encoded-command validator route.
- After player confirmation, sync the v2 installer validation path into the official upload package and compare the Steam subscription copy after upload.
