# 20260703-0004 PowerShell Host Self Diagnostics

## Summary

Expanded the install preflight probe with PowerShell host self diagnostics before the package parser matrix, so player reports can distinguish host startup/version/policy failures from DTMAPI script parser failures.

## Source Request

The user asked whether the probe could also check internal syntax and add more PowerShell version/body diagnostics. This followed evidence that Windows PowerShell 5.1 failed the old encoded parser route on a player machine while ordinary 5.1 self-checks may still work.

## Changed Files

- `tools/scripts/probe-install-preflight.ps1`
- `docs/updates/INDEX.md`
- `docs/debug/regressions/smoke-matrix.md`

Generated artifacts:

- `dist/player-probe-20260703-preflight-v3/DTMAPI`
- `dist/DTMAPI-player-probe-20260703-install-preflight-v3.zip`
- `dist/player-hotfix-20260703-pwsh-diagnostics-v3/DTMAPI`
- `dist/DTMAPI-player-hotfix-20260703-pwsh-diagnostics-v3.zip`

## Behavior

The probe now adds a `PowerShell host self diagnostics` step before parsing DTMAPI package scripts. For each discovered host, it records:

- executable path and file version info
- runtime `PSVersion`, `PSEdition`, `CLRVersion`, `PSHome`, host name/version, process path
- `LanguageMode`
- process bitness
- OS version
- effective `ExecutionPolicy`
- `Get-ExecutionPolicy -List`
- minimal `-Command` result
- temporary `-File` runtime-info result
- temporary `-File` `Parser.ParseInput('Write-Output 1')` result
- minimal `-EncodedCommand` result
- encoded-command `Parser.ParseInput('Write-Output 1')` result

The package parser matrix still checks each packaged `.ps1` with both:

- effective file-based validator result: `Ok` / `ExitCode`
- old encoded-command validator comparison: `EncodedCommandOk` / `EncodedCommandExitCode`

## Validation

- Windows PowerShell/Core syntax validation passed for updated `probe-install-preflight.ps1`.
- Probe v3 fake valid game test passed:
  - exit code `0`
  - no install entry point in probe-only package
  - no `BepInEx\plugins\DTMAPI` directory created
  - no game `DTMAPI` state directory created
  - self diagnostics recorded Windows PowerShell 5.1.26100.8655 Desktop, `FullLanguage`, `Bypass`, 64-bit, and all five local host self-tests exit `0`
- Full runtime package matrix for the v3 probe package passed with `Blockers: 0`.
- Full runtime package matrix for the v3 installer hotfix package passed with `Blockers: 0`.
- Affected player v3 probe report `dtmapi-install-preflight-probe-20260703-181031.json` confirmed the root cause:
  - Windows PowerShell 5.1 itself works for minimal `-Command`.
  - Windows PowerShell 5.1 works for temporary `-File` runtime diagnostics.
  - Windows PowerShell 5.1 works for file-based `Parser.ParseInput`.
  - Windows PowerShell 5.1 works for file-based parsing of every packaged DTMAPI `.ps1`, including all strict install scripts.
  - Windows PowerShell 5.1 fails even a minimal `-EncodedCommand` with `exit=-1` and no output.
  - Windows PowerShell 5.1 fails encoded-command `Parser.ParseInput` and encoded-command parsing of every packaged DTMAPI `.ps1` with `exit=-1` and no output.
  - PowerShell 7.6.3 passes all file-based and encoded-command checks.

Evidence:

- `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-probe-v3-test-20260703\probe-v3-valid.out.txt`
- `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-probe-v3-release-audit-20260703\DTMAPI Workshop Audit 20260703-174509\Results\stress-summary.md`
- `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-hotfix-v3-release-audit-20260703\DTMAPI Workshop Audit 20260703-174536\Results\stress-summary.md`
- `D:\下载\dtmapi-install-preflight-probe-20260703-181031.json`

Standalone v3 probe-only zip:

- `dist/DTMAPI-player-probe-20260703-install-preflight-v3.zip`
- SHA256: `BB82086CF6EB3AAC645DFD609BF5F0696A3DABA86C9A5F19A1E3D2E937EAD954`
- Packaged `Content\DTMAPIInstaller\tools\probe-install-preflight.ps1` SHA256: `89531FF48A38EDE7C6ADD40C4E7E62589379FA1662A050161B7613E459F0BE4D`

Standalone v3 installer zip:

- `dist/DTMAPI-player-hotfix-20260703-pwsh-diagnostics-v3.zip`
- SHA256: `F38751DF87215B215174617940E986BCA4D9AECEB789EF929256E4C1DB3F16B9`

## Not Run

- No live Doloc Town game smoke was run because this change affects installer diagnostics only.
- The local official upload folder and Steam Workshop upload/subscription package were not synchronized in this update.

## Rollback

Remove the `PowerShell host self diagnostics` step and helper functions from `probe-install-preflight.ps1`. The v2 file-validator comparison would still remain, but reports would again be less useful for distinguishing host runtime/policy failures from package parser failures.

## Follow-Up

- Ask affected players to run the v3 probe-only package and send the JSON. If `FileParseInput` succeeds but `EncodedParseInput` fails under Windows PowerShell, the old encoded-command validator route is the likely root cause.
- If both file and encoded self-tests pass but package file parsing fails, inspect the per-script parser output as a true script compatibility issue.
