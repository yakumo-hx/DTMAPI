# 20260703-0005 Root BAT File Host Probe

## Summary

Moved the root Workshop `.bat` PowerShell host probe from a long inline `-Command` body to a packaged `probe-powershell-host.ps1` helper launched with `-File`, then synced the fixed Runtime installer package to the local official upload folder.

## Source Request

The user asked to write the PowerShell compatibility optimization back to source, sync the upload scripts, verify, and commit. The key requirement was that missing PowerShell 7 must not break installs because most players only have Windows PowerShell 5.1.

Prior player evidence from `D:\下载\dtmapi-install-preflight-probe-20260703-181031.json` showed Windows PowerShell 5.1 could run file-based DTMAPI parser checks, but failed the old encoded-command route with `exit=-1`. After fixing installer internals, the remaining root-layer risk was the large inline `.bat` `-Command` probe used to select a host before launching each script.

## Changed Files

- `tools/scripts/probe-powershell-host.ps1`
- `tools/release/runtime-workshop/0_probe_dtmapi_install.bat`
- `tools/release/runtime-workshop/1_install_dtmapi.bat`
- `tools/release/runtime-workshop/2_uninstall_dtmapi.bat`
- `tools/release/runtime-workshop/3_check_dtmapi_status.bat`
- `tools/release/runtime-workshop/4_collect_dtmapi_logs.bat`
- `tools/scripts/build-release-workshop-packages.ps1`
- `tools/scripts/probe-install-preflight.ps1`
- `docs/debug/regressions/smoke-matrix.md`
- `docs/updates/INDEX.md`

Generated or synchronized artifacts:

- `dist/player-hotfix-20260703-root-file-probe/DTMAPI`
- `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`

## Behavior

- Each root entry point now defines `DTMAPI_PS_HOST_PROBE` as:
  - `Content\DTMAPIInstaller\tools\probe-powershell-host.ps1`
- If the helper is missing, the root entry point fails immediately with a clear missing-file message.
- Host selection still tries PowerShell 7 first:
  - `%ProgramFiles%\PowerShell\7\pwsh.exe`
  - `%ProgramFiles(x86)%\PowerShell\7\pwsh.exe`
  - `where pwsh.exe`
- If PowerShell 7 is not present, the root entry point continues to Windows PowerShell:
  - `%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe`
  - `where powershell.exe`
- The per-host parser probe now runs:
  - `<candidate> -NoProfile -ExecutionPolicy Bypass -File <probe-powershell-host.ps1> -ProbeList <scripts>`
- `probe-powershell-host.ps1` prints host version/edition, checks each required script path, reports parser errors with line/column, and exits with explicit codes.
- The build script copies `probe-powershell-host.ps1` into the Runtime package and validates it as a strict preflight file.
- The install preflight probe now includes `probe-powershell-host.ps1` in package-layout and strict parser matrix checks.

## Validation

- Direct Windows PowerShell 5.1 run of `probe-powershell-host.ps1` passed for:
  - `common.ps1`
  - `release-common.ps1`
  - `probe-powershell-host.ps1`
- Windows PowerShell syntax validation passed for:
  - `probe-powershell-host.ps1`
  - `probe-install-preflight.ps1`
  - `build-release-workshop-packages.ps1`
- Temporary Runtime package build passed:
  - `dist/player-hotfix-20260703-root-file-probe/DTMAPI`
- Full subscription-package matrix for the temporary package passed with `Blockers: 0`.
- A no-`pwsh.exe` root-entry simulation passed:
  - first selected host was `C:\WINDOWS\System32\WindowsPowerShell\v1.0\powershell.exe`
  - package path included spaces and Chinese characters
  - `0_probe_dtmapi_install.bat` exited `0`
- Local official upload folder was rebuilt under the shared runtime lock:
  - `C:\Users\Administrator\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`
- Full subscription-package matrix for the local official upload folder passed with `Blockers: 0`.
- Hash parity between the temporary package and local upload folder matched for all five root bats and packaged installer/helper scripts.

Evidence:

- Temp package audit:
  - `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-root-file-probe-audit-20260703\DTMAPI Workshop Audit 20260703-183901\Results\stress-summary.md`
- No-`pwsh.exe` root-entry simulation:
  - `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-no-pwsh-root-probe-20260703-cmdset2.out.txt`
  - `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-no-pwsh-root-probe-20260703-cmdset2.err.txt`
- Local upload audit:
  - `C:\Users\ADMINI~1\AppData\Local\Temp\DTMAPI-local-upload-root-file-probe-audit-20260703\DTMAPI Workshop Audit 20260703-184245\Results\stress-summary.md`

## Not Run

- No live Doloc Town game smoke was run because this change affects the installer package wrapper and preflight diagnostics only.
- Steam Workshop upload/resubscribe parity is still pending because the local official upload folder has been synchronized, but Steam has not been uploaded and redownloaded in this update.

## Rollback

Restore the root `.bat` host probe to the previous inline `-Command` parser body, remove `probe-powershell-host.ps1` from the package build, and remove it from the install preflight probe's strict matrix. This rollback is not recommended because the affected player evidence points away from script syntax and toward fragile command-launch routes.

## Follow-Up

- Upload the synchronized local `DTMAPI` Runtime package through the Steam Workshop flow.
- After Steam redownload/resubscribe, compare the subscription directory against the local upload folder and run the same subscription-package audit against the Steam subscription path.
