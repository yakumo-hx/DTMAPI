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
- `Content\DTMAPIInstaller\tools\invoke-dtmapi-action.cmd`
- `Content\DTMAPIInstaller\tools\common.ps1`
- `Content\DTMAPIInstaller\tools\release-common.ps1`
- `Content\DTMAPIInstaller\tools\install-to-game.ps1`
- `Content\DTMAPIInstaller\tools\install-bepinex.ps1`
- `Content\DTMAPIInstaller\tools\uninstall-dtmapi.ps1`
- `Content\DTMAPIInstaller\tools\check-dtmapi-status.ps1`
- `Content\DTMAPIInstaller\tools\collect-logs.ps1`
- `Content\DTMAPIInstaller\tools\analyze-startup-evidence.ps1`
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
| Root bat wrappers | Copy the package to an exact temporary `Program Files (x86)\steam\steamapps\workshop\content\2285550\3743016467` shape, then run `1_install_dtmapi.bat`, `2_uninstall_dtmapi.bat`, `3_check_dtmapi_status.bat`, and `4_collect_dtmapi_logs.bat` through `cmd.exe /d /e:off /v:off /c call "<package>\<bat>" <NUL`, with `DTMAPI_GAME_DIR` pointing to a temp fake game path containing spaces/non-ASCII text and `&`. | Every label-free wrapper safely survives path parentheses, enables extensions, re-enters the packaged shared dispatcher under `/e:on`, and reaches its target `.ps1`. Expected non-zero exits must be action semantics, not wrapper syntax or missing-label errors. |
| Install dry-run | `install-to-game.ps1 -InstallBepInEx -DryRun -SkipOfficialLocalMods` with empty, partial BepInEx, and partial DTMAPI temp game dirs. | Exit `0`; no strict syntax failure; no real game writes. |
| Real temp install | `install-to-game.ps1 -InstallBepInEx -SkipOfficialLocalMods` with `DTMAPI_GAME_DIR` set to a temp directory containing spaces and non-ASCII text. | Exit `0`; `check-dtmapi-status.ps1 -GameDir <temp>` exits `0`; BepInEx/Doorstop files and DTMAPI runtime files exist. |
| Check/status | `check-dtmapi-status.ps1` against not-installed, half-installed, failed-state, and after-uninstall temp dirs. | Missing runtime files are reported clearly; helper parser failures are warning-only; no check script self-crash. |
| No-EXE Runtime boundary | Recursively inspect a generated Runtime package, install it into a temp game, and inspect install receipts and `DTMAPI/tools`. | Package EXE count is zero; no `player-doctor` directory or receipt is installed; status and collection remain usable and report Doctor absence as informational. |
| Uninstall | `uninstall-dtmapi.ps1 -GameDir <temp>` against not-installed, partial install, corrupt `install-state.json`, and path-with-space package wrapper. | Exit `0`; no unrelated files removed; backup/uninstall state is written when applicable. |
| Runtime-only ownership | In a temp `DTMAPI_DOLOC_PERSISTENT_ROOT`, populate third-party/copied/empty/malformed/legacy markers, forged and current-looking receipt files, unknown additions, and matching `mod_infos.json`; run dry-run, real uninstall, and the retired cleanup switch. | The whole `MODS` tree and `mod_infos.json` remain byte-identical; no marker scan or enablement edit occurs; the legacy switch warns and is a cleanup no-op. |
| Developer package/enablement transaction | Run `test-developer-official-local-install-transaction.ps1` against temp `DTMAPI_DOLOC_PERSISTENT_ROOT` trees under both PowerShell 7 and Windows PowerShell 5.1. Cover malformed JSON, object-shape failures, locked replacement, unchanged/repaired retries, an unknown file injected after the first rollback fingerprint, and a pre-existing foreign destination. | Malformed state blocks before package publication; write failure rolls back only the package published by that invocation; post-fingerprint drift is restored/preserved and never deleted; a foreign destination and its enablement authority remain unchanged; original bytes/backups match; no temp/staging residue remains; operator recovery succeeds with accurate per-invocation `BundledMods` receipts. Do not interpret this controlled-exception matrix as process-kill/power-loss crash consistency. |
| ContentOnly current-version projection | In the same dual-host transaction matrix, install all non-QA developer definitions into a fresh temp root and inspect Oil's source manifest/info, Catalog source row, publish-text row, staged manifest/info, and `release-manifest.json` receipt. Also invoke the shared parity assertion with deliberately divergent manifest/info values. | All current Oil projections equal `0.3.1-dtmapi`; Catalog keeps `targetVersion=1.0.0` and `PrototypeBlocked`; the installed package and receipt remain ContentOnly, DLL-free, and without `MinimumDTMApiVersion`; explicit version drift fails before that ContentOnly package is published. This lane does not authorize Oil upload or reconcile CodeMod product versions. |
| Offline logs | Run `4_collect_dtmapi_logs.bat` twice from a package path containing spaces with at least twelve current/history log candidates, including one larger than the old cap. Repeat the script-level collector once with `-IncludeCrashDumps`. | Each root-BAT run publishes a different unique directory; the player bundle contains exactly the newest ten DTMAPI logs as full byte/hash-equal copies; no `.partial` directory is presented as complete; missing unrelated diagnostics do not block collection; dumps are absent unless explicitly requested. Internal callers that pass `-OutputDirectory` keep the separate bounded merge behavior. |
| Package layout | Inspect `Content\DTMAPIInstaller`, `Content\DTMAPI`, root bat files, and `Content\.tools\bepinex`. | Exactly four public root launchers and `Content\DTMAPIInstaller\tools\invoke-dtmapi-action.cmd` exist; package EXE count is zero; BepInEx ZIP opens and contains Doorstop/BepInEx core files. |
| PowerShell compatibility | Parse all package `.ps1` files under Windows PowerShell 5.1 and PowerShell 7 if installed, reject direct player-script `Get-FileHash`/`Expand-Archive` commands, force the shared dispatcher to Windows PowerShell 5.1 for install/check/collect/uninstall, force `cmd.exe` as a silent-zero false host, then start at least 24 real mixed-action dispatchers against one shared temp root. | Parser errors `0`; host probe proves FullLanguage, JSON, portable SHA-256 and ZIP support; the real action chain succeeds without PowerShell 7 or optional hash/archive cmdlets; an executable that exits zero without the exact nonce-bound result is rejected before action or install-state mutation; every concurrent dispatcher atomically owns a separate probe session and reaches the real host before action semantics or the per-game lock decides its result. |
| Per-game mutation ownership | Hold the normalized-game mutex in one process, start an install in another, then create a controlled pending Runtime transaction marker and invoke uninstall. | The concurrent install exits nonzero before writing a competing failure state; uninstall exits nonzero before moving any live Runtime path; normal retry succeeds after each controlled blocker is released. |
| Uninstall no-op and receipt identity | Run normal uninstall, then run it again within the same second-scale window. | The first run removes only DTMAPI-owned Runtime targets; the second exits zero with explicit “no files were removed” wording; both receipts have unique collision-resistant names and earlier evidence is not overwritten. |
| Host selection | First set `DTMAPI_POWERSHELL_HOST` to an executable that is not PowerShell; then, in a separate ordinary-candidate fixture, make an earlier candidate unavailable or probe-invalid while a later compatible host remains. | The explicit override fails closed before mutation. Without an override, selection continues after an unusable ordinary candidate, and only the selected compatible host runs the action. |
| BepInEx sources and partial install | Test a valid package-local ZIP, an invalid package-local ZIP with a controlled valid fixed-hash fallback, and a fake partial `BepInEx\core\BepInEx.dll`; place unrelated plugin/config sentinels in the existing tree. | Valid bundled bytes are preferred; invalid bytes are never extracted; fallback repairs the partial core; unrelated plugin/config sentinels remain byte-identical; failure removes/restores only installer-owned writes. |
| Strict install / optional diagnostics | Deliberately introduce a parser error into packaged `analyze-startup-evidence.ps1`, run install, restore the package helper, and run install again as repair. | The diagnostic parser error is visible as a warning but does not block the first install; the second install repairs the installed helper and clean status passes. Scripts required for the install action remain strict. |
| Runtime transaction faults | Run `test-runtime-upgrade-transaction.ps1` under PowerShell 7 and Windows PowerShell 5.1 across every tracked install/rollback fault phase. | Every pre-commit fault preserves the previous live state, every committed/interrupted state is reconciled as specified, retry succeeds, and no transaction residue remains. |
| Path compatibility | Use the exact parenthesized Workshop package shape above plus temp game dirs with spaces, parentheses, `&`, and non-ASCII text. Also test invalid explicit `DTMAPI_GAME_DIR`. | Valid paths install/check; no compound-block CMD parser failure occurs; invalid explicit paths fail early instead of silently falling back to a real Steam install. |

## Known Interpretation Notes

- `3_check_dtmapi_status.bat` exits non-zero when DTMAPI is not installed. That is expected for install verification. If the player just ran the uninstaller, missing DTMAPI runtime files are expected.
- The in-game report export and `4_collect_dtmapi_logs.bat` are different paths. Always test the root bat wrapper because CMD quoting bugs do not appear in PowerShell parser checks.
- AST parsing proves syntax only. It does not prove module/cmdlet availability, so a parser-only Windows PowerShell 5.1 result is never sufficient installer acceptance.
- `dist\workshop-packages` can be stale. Prefer the local upload directory and subscription directory for player-facing parity checks.
