# Workshop Package And Subscription Test Matrix

Use this matrix before asking players to reinstall DTMAPI Runtime, after changing installer scripts, or after syncing a local upload package. It verifies the exact artifact players receive, not only the source tree.

## Package Roots

- Source root: repository working tree.
- Local upload root: `%USERPROFILE%\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`.
- Steam subscription root: `...\steamapps\workshop\content\2285550\3743016467`.

Do not point pressure tests at the real Doloc Town directory unless the task explicitly requires a real install. Use temporary fake game directories and `DTMAPI_GAME_DIR` for script tests.

## Hash Parity

Compare these files between local upload and Steam subscription roots:

- `1_install_dtmapi.bat`
- `2_uninstall_dtmapi.bat`
- `3_check_dtmapi_status.bat`
- `4_collect_dtmapi_logs.bat`
- `Content\DTMAPIInstaller\tools\common.ps1`
- `Content\DTMAPIInstaller\tools\release-common.ps1`
- `Content\DTMAPIInstaller\tools\install-to-game.ps1`
- `Content\DTMAPIInstaller\tools\install-bepinex.ps1`
- `Content\DTMAPIInstaller\tools\uninstall-dtmapi.ps1`
- `Content\DTMAPIInstaller\tools\check-dtmapi-status.ps1`
- `Content\DTMAPIInstaller\tools\collect-logs.ps1`
- `Content\DTMAPIInstaller\tools\analyze-startup-evidence.ps1`
- `Content\DTMAPIInstaller\tools\player-doctor\dtmapi-player-doctor.exe`
- `Content\DTMAPIInstaller\tools\player-doctor\dotnet-LICENSE.txt`
- `Content\DTMAPIInstaller\tools\player-doctor\dotnet-ThirdPartyNotices.txt`
- `Content\.tools\bepinex\BepInEx_win_x64_5.4.23.5.zip`
- `Content\DTMAPI\release-manifest.json`
- `Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI\DTMAPI.BepInExBootstrap.dll`
- `Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI\DTMAPI.Abstractions.dll`
- `Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI\DTMAPI.Core.dll`
- `Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI\DTMAPI.GameBridge.DolocTown.dll`
- `Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI\DTMAPI.ModConfigMenu.dll`
- `Content\DTMAPIInstaller\Payload\BepInEx\plugins\DTMAPI\assets\branding\dtmapi-icon.png`
- `info.json`
- `icon.png`
- `preview.png`

Any mismatch means the player-facing subscription package is not the package you just prepared.

## Parallel Review Matrix

Run these checks independently when possible:

| Lane | Scope | Required Result |
| --- | --- | --- |
| Root bat wrappers | Run `1_install_dtmapi.bat`, `2_uninstall_dtmapi.bat`, `3_check_dtmapi_status.bat`, and `4_collect_dtmapi_logs.bat` from a package path containing spaces/non-ASCII text through `cmd.exe /d /c call "<package>\<bat>" <NUL`, with `DTMAPI_GAME_DIR` pointing to temp fake game dirs. | Wrappers reach their target `.ps1` scripts instead of failing in CMD parsing. Expected non-zero exits must be from status semantics, not wrapper syntax. |
| Install dry-run | `install-to-game.ps1 -InstallBepInEx -DryRun -SkipOfficialLocalMods` with empty, partial BepInEx, and partial DTMAPI temp game dirs. | Exit `0`; no strict syntax failure; no real game writes. |
| Real temp install | `install-to-game.ps1 -InstallBepInEx -SkipOfficialLocalMods` with `DTMAPI_GAME_DIR` set to a temp directory containing spaces and non-ASCII text. | Exit `0`; `check-dtmapi-status.ps1 -GameDir <temp>` exits `0`; BepInEx/Doorstop files and DTMAPI runtime files exist. |
| Check/status | `check-dtmapi-status.ps1` against not-installed, half-installed, failed-state, and after-uninstall temp dirs. | Missing runtime files are reported clearly; helper parser failures are warning-only; no check script self-crash. |
| Player Doctor offline entry points | Install a generated Runtime package into a temp game, lock a Strict CodeMod fixture under `BepInEx/plugins`, then run the packaged `3_check_dtmapi_status.bat` and `4_collect_dtmapi_logs.bat`. Hash `BepInEx/plugins` and `Mods` before and after each entry point. | Status returns its documented invalid-state code and names the misplaced CodeMod; collection still exits `0` and exports Doctor JSON/text/summary with exit `2`; both scanned trees remain byte-identical and the uninstaller preserves the unknown fixture. |
| Uninstall | `uninstall-dtmapi.ps1 -GameDir <temp>` against not-installed, partial install, corrupt `install-state.json`, and path-with-space package wrapper. | Exit `0`; no unrelated files removed; backup/uninstall state is written when applicable. |
| Runtime-only ownership | In a temp `DTMAPI_DOLOC_PERSISTENT_ROOT`, populate third-party/copied/empty/malformed/legacy markers, forged and current-looking receipt files, unknown additions, and matching `mod_infos.json`; run dry-run, real uninstall, and the retired cleanup switch. | The whole `MODS` tree and `mod_infos.json` remain byte-identical; no marker scan or enablement edit occurs; the legacy switch warns and is a cleanup no-op. |
| Developer package/enablement transaction | Run `test-developer-official-local-install-transaction.ps1` against temp `DTMAPI_DOLOC_PERSISTENT_ROOT` trees under both PowerShell 7 and Windows PowerShell 5.1. Cover malformed JSON, object-shape failures, locked replacement, unchanged/repaired retries, an unknown file injected after the first rollback fingerprint, and a pre-existing foreign destination. | Malformed state blocks before package publication; write failure rolls back only the package published by that invocation; post-fingerprint drift is restored/preserved and never deleted; a foreign destination and its enablement authority remain unchanged; original bytes/backups match; no temp/staging residue remains; operator recovery succeeds with accurate per-invocation `BundledMods` receipts. Do not interpret this controlled-exception matrix as process-kill/power-loss crash consistency. |
| ContentOnly current-version projection | In the same dual-host transaction matrix, install all non-QA developer definitions into a fresh temp root and inspect Oil's source manifest/info, Catalog source row, publish-text row, staged manifest/info, and `release-manifest.json` receipt. Also invoke the shared parity assertion with deliberately divergent manifest/info values. | All current Oil projections equal `0.3.1-dtmapi`; Catalog keeps `targetVersion=1.0.0` and `PrototypeBlocked`; the installed package and receipt remain ContentOnly, DLL-free, and without `MinimumDTMApiVersion`; explicit version drift fails before that ContentOnly package is published. This lane does not authorize Oil upload or reconcile CodeMod product versions. |
| Offline logs | `4_collect_dtmapi_logs.bat` and `collect-logs.ps1` from a package path containing spaces, with missing logs, partial state, Doctor findings, and a missing/crashed/timed-out Doctor helper. | Bat reaches `collect-logs.ps1`; exit `0`; Desktop or requested output folder is created; Doctor exit `2` is retained as finding evidence, while helper failure is warning-only and missing unrelated logs do not crash collection. |
| Package layout | Inspect `Content\DTMAPIInstaller`, `Content\DTMAPI`, root bat files, and `Content\.tools\bepinex`. | Root launchers and installer tools exist; BepInEx zip opens and contains Doorstop/BepInEx core files. |
| PowerShell compatibility | Parse all package `.ps1` files under Windows PowerShell 5.1 and PowerShell 7 if installed. | Parser errors `0` for both hosts. |
| Path compatibility | Use temp game dirs with spaces, parentheses, and non-ASCII text. Also test invalid explicit `DTMAPI_GAME_DIR`. | Valid paths install/check; invalid explicit paths fail early instead of silently falling back to a real Steam install. |

## Known Interpretation Notes

- `3_check_dtmapi_status.bat` exits non-zero when DTMAPI is not installed. That is expected for install verification. If the player just ran the uninstaller, missing DTMAPI runtime files are expected.
- The in-game report export and `4_collect_dtmapi_logs.bat` are different paths. Always test the root bat wrapper because CMD quoting bugs do not appear in PowerShell parser checks.
- `dist\workshop-packages` can be stale. Prefer the local upload directory and subscription directory for player-facing parity checks.
