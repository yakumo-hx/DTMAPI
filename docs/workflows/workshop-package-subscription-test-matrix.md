# Workshop Package And Subscription Test Matrix

Use this matrix before asking players to reinstall DTMAPI Runtime, after changing Runtime installer scripts, or after syncing a Runtime upload package. Ordinary product Mod upload-folder syncing follows [product validation](product-change-validation.md) and does not trigger this Runtime matrix. This matrix verifies the exact Runtime artifact players receive.

## Package Roots

- Source root: repository working tree.
- Published Windows local upload root: `%USERPROFILE%\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI`.
- Published Windows Steam subscription root: `...\steamapps\workshop\content\2285550\3743016467`.
- Multi-platform candidate: repository `dist\DTMAPI-MultiPlatform`.
- Multi-platform local upload root: `%USERPROFILE%\AppData\LocalLow\RedSawGames\DolocTown\MODS\DTMAPI_MultiPlatform`.
- Multi-platform Steam subscription root: `...\steamapps\workshop\content\2285550\3792681186`.

Do not point pressure tests at the real Doloc Town directory unless the task explicitly requires a real install. Use temporary fake game directories and `DTMAPI_GAME_DIR` for script tests.

## Hash Parity

For the published Windows distribution, compare these files between its local
upload and Steam subscription roots:

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

For the multi-platform package, run
`tools/scripts/test-dtmapi-multiplatform-package.ps1` against the repository
candidate without a control-file allowance and against the official local
upload leaf with `-AllowWorkshopControlFile`. Its one root `workshop.json` must
contain only `workshop_id=3792681186`, remain byte-preserved across rebuilds,
and be excluded from content receipts; no repository candidate or Steam
download may contain it. When source metadata still represents the subscribed
revision, the same complete audit may run against that subscription. When a
new local metadata successor already exists, calculate the subscription's
normalized content receipt read-only and compare it to the frozen
`steamDelivered*` Catalog fields instead; do not require old subscribed
metadata to match the new source projection. If local metadata is changed
after observation, preserve the last published receipt and mark the new local
receipt pending upload instead of claiming current subscription parity.

## Multi-Platform Candidate Matrix

| Lane | Scope | Required Result |
| --- | --- | --- |
| Shared Runtime provenance | Select the explicit source branch of `test-dtmapi-multiplatform-package.ps1`. | Default ObservedPublished retains the exact 0.6.1 source: 20 shared paths, source 28 files / 3,866,857 bytes / `b4ec6a44...e4aa`. `-SourceKind Candidate -AcceptedPackageRoot <exact Windows candidate>` checks schema 2 / 0.7.0, its real build identity and all 21 imported paths against that artifact. Candidate source is never labeled as a Steam observation. |
| Host/package boundary | Audit `DTMAPI-MultiPlatform-Installer.exe`, the internal Linux host, both host manifests, all eight public action shims, metadata and branding. | Exactly one x64 PE and one x64 ELF; four BAT shims pass `--pause`; four LF/no-BOM shell shims preserve the host exit and explain `noexec`; repository/subscription contain zero `workshop.json`, while the official upload leaf may contain exactly one validated root control file for `3792681186`; no QA or Player Doctor payload. All localized descriptions retain install/check/log, unsigned-tool and platform guidance. |
| Windows + Linux lifecycle | Run `tools/scripts/test-multiplatform-runtime-installer.ps1` against a bounded package candidate. | Real Windows host and, when WSL is available, real Linux host each pass install, read-only status, two log collections, uninstall and repeated no-op uninstall on independent fake games. |
| 0.7.0 version transition | Supply `-PreviousPackageRoot <exact schema-1 multi-platform package>` to the same lifecycle test. | Each executed host lane covers original 0.6.1 install, new-host old-package reading, upgrade to 0.7.0, withdrawal/reinstall, and rejection of downgrade, old-reader/new-schema and eight invalid candidate-provenance variants. |
| Ownership | Seed external BepInEx plugin/config/log, DTMAPI config/history logs, Mods and ContentPacks sentinels. Inspect install/uninstall receipts. | All sentinels remain byte-identical; host binaries/manifests never enter the game; uninstall removes only receipt-owned Runtime targets and preserves BepInEx. |
| Fail-closed preflight | Use an invalid explicit game path and corrupt one exact payload file. | `DTM-E1002` and `DTM-E1201` occur before game-tree mutation. |
| Transaction convergence | Fixture all six observed install phases, three uninstall phases, mixed own/legacy state, orphan/prefix-file state and the accepted PowerShell classifier. | Status is read-only; every pre-commit state restores exact old bytes; committed install keeps exact new bytes; committed uninstall finalizes into one retained backup; both engines reject the other's receipt before mutation. |
| Runtime provenance conflicts | Fixture same-version receipt/commit/version skew, lower-version unknown distribution, and lower-version legacy state without its release authority. | All eight variants fail as `DTM-E1202` with a byte-identical game-shaped tree; unknown distributions are never adopted and legacy state always requires an agreeing release manifest. |
| Link/privacy boundary | Place junction/symlink entries in uninstall backups, DTMAPI logs, cleanup parents and Linux Proton `compatdata/.../users` parents leading to a secret external `Player.log`. | Mutation rejects linked owned paths; log collection succeeds only by explicitly recording the skipped source; no external marker enters a bundle and cleanup never follows the link. |
| Steam Deck launch integration | From subscription item `3792681186`, install in desktop mode, set `WINEDLLOVERRIDES="winhttp=n,b" %command%`, start the game, then run status. | The subscribed content receipt is recorded independently from any later local metadata successor; file health is good; status repeats the exact required option; a fresh BepInEx/DTMAPI startup log proves injection. Copied files and subscription parity alone are not acceptance. |
| CrossOver | In the existing working game Bottle, run the root PE and set `winhttp` to `Native, then Builtin`; restart Bottle Steam/game and inspect fresh logs. | Installation remains inside the selected game Bottle, no Linux launch string is prescribed, and fresh Runtime evidence exists. Keep this lane experimental until a player completes it. |

## Parallel Review Matrix

Unless a row explicitly names the multi-platform sibling, the following table
is the published Windows BAT/CMD/PowerShell distribution matrix.

Run these checks independently when possible:

| Lane | Scope | Required Result |
| --- | --- | --- |
| Root bat wrappers | Copy the package to an exact temporary `Program Files (x86)\steam\steamapps\workshop\content\2285550\3743016467` shape, then run `1_install_dtmapi.bat`, `2_uninstall_dtmapi.bat`, `3_check_dtmapi_status.bat`, and `4_collect_dtmapi_logs.bat` through `cmd.exe /d /e:off /v:off /c call "<package>\<bat>" <NUL`, with `DTMAPI_GAME_DIR` pointing to a temp fake game path containing spaces/non-ASCII text and `&`. | Every label-free wrapper safely survives path parentheses, enables extensions, re-enters the packaged shared dispatcher under `/e:on`, and reaches its target `.ps1`. Expected non-zero exits must be action semantics, not wrapper syntax or missing-label errors. |
| Install dry-run | `install-to-game.ps1 -InstallBepInEx -DryRun -SkipOfficialLocalMods` with empty, partial BepInEx, and partial DTMAPI temp game dirs. | Exit `0`; no strict syntax failure; no real game writes. |
| Real temp install | `install-to-game.ps1 -InstallBepInEx -SkipOfficialLocalMods` with `DTMAPI_GAME_DIR` set to a temp directory containing spaces and non-ASCII text. | Exit `0`; `check-dtmapi-status.ps1 -GameDir <temp>` exits `0`; BepInEx/Doorstop files and DTMAPI runtime files exist. |
| Check/status | `check-dtmapi-status.ps1` against not-installed, half-installed, failed-state, after-uninstall, valid pending receipt, sterile receiptless, unsafe receiptless, invalid receipt, and orphan-state temp dirs. | The checker never mutates transaction fixtures; it emits exactly one final state. Healthy is `DTM-S3001`; pending is `RECOVERY_PENDING`; sterile is `REPAIRABLE_STALE`; unsafe/invalid/orphan is `BLOCKED`; existing not-installed/partial/update exit-code semantics remain intact. |
| Published Windows no-EXE Runtime boundary | Recursively inspect a generated published-Windows Runtime package, install it into a temp game, and inspect install receipts and `DTMAPI/tools`. | Published Windows package EXE count is zero; no `player-doctor` directory or receipt is installed; status and collection remain usable and report Doctor absence as informational. |
| Uninstall | `uninstall-dtmapi.ps1 -GameDir <temp>` against not-installed, partial install, corrupt `install-state.json`, and path-with-space package wrapper. | Exit `0`; no unrelated files removed; backup/uninstall state is written when applicable. |
| Runtime-only ownership | In a temp `DTMAPI_DOLOC_PERSISTENT_ROOT`, populate third-party/copied/empty/malformed/legacy markers, forged and current-looking receipt files, unknown additions, and matching `mod_infos.json`; run dry-run, real uninstall, and the retired cleanup switch. | The whole `MODS` tree and `mod_infos.json` remain byte-identical; no marker scan or enablement edit occurs; the legacy switch warns and is a cleanup no-op. |
| Developer package/enablement transaction | Run `test-developer-official-local-install-transaction.ps1` against temp `DTMAPI_DOLOC_PERSISTENT_ROOT` trees under both PowerShell 7 and Windows PowerShell 5.1. Cover malformed JSON, object-shape failures, locked replacement, unchanged/repaired retries, an unknown file injected after the first rollback fingerprint, and a pre-existing foreign destination. | Malformed state blocks before package publication; write failure rolls back only the package published by that invocation; post-fingerprint drift is restored/preserved and never deleted; a foreign destination and its enablement authority remain unchanged; original bytes/backups match; no temp/staging residue remains; operator recovery succeeds with accurate per-invocation `BundledMods` receipts. Do not interpret this controlled-exception matrix as process-kill/power-loss crash consistency. |
| ContentOnly current-version projection | In the same dual-host transaction matrix, install all non-QA developer definitions into a fresh temp root and inspect Oil's source manifest/info, Catalog source row, publish-text row, staged manifest/info, and `release-manifest.json` receipt. Also invoke the shared parity assertion with deliberately divergent manifest/info values. | All current Oil projections equal `0.3.1-dtmapi`; Catalog keeps `targetVersion=1.0.0` and `PrototypeBlocked`; the installed package and receipt remain ContentOnly, DLL-free, and without `MinimumDTMApiVersion`; explicit version drift fails before that ContentOnly package is published. This lane does not authorize Oil upload or reconcile CodeMod product versions. |
| Offline logs | Run `4_collect_dtmapi_logs.bat` twice from a package path containing spaces with at least twelve current/history log candidates, including one larger than the old cap. Repeat the script-level collector once with `-IncludeCrashDumps`. | Each root-BAT run publishes a different unique directory; the player bundle contains exactly the newest ten DTMAPI logs as full byte/hash-equal copies; no `.partial` directory is presented as complete; missing unrelated diagnostics do not block collection; dumps are absent unless explicitly requested. Internal callers that pass `-OutputDirectory` keep the separate bounded merge behavior. |
| Published Windows package layout | Inspect `Content\DTMAPIInstaller`, `Content\DTMAPI`, root bat files, and `Content\.tools\bepinex`. | Exactly four public root launchers and `Content\DTMAPIInstaller\tools\invoke-dtmapi-action.cmd` exist; published Windows package EXE count is zero; BepInEx ZIP opens and contains Doorstop/BepInEx core files. |
| PowerShell compatibility | Parse all package `.ps1` files under Windows PowerShell 5.1 and PowerShell 7 if installed, reject direct player-script `Get-FileHash`/`Expand-Archive` commands, force the shared dispatcher to Windows PowerShell 5.1 for install/check/collect/uninstall, force `cmd.exe` as a silent-zero false host, then start at least 24 real mixed-action dispatchers against one shared temp root. | Parser errors `0`; host probe proves FullLanguage, JSON, portable SHA-256 and ZIP support; the real action chain succeeds without PowerShell 7 or optional hash/archive cmdlets; an executable that exits zero without the exact nonce-bound result is rejected before action or install-state mutation; every concurrent dispatcher atomically owns a separate probe session and reaches the real host before action semantics or the per-game lock decides its result. |
| Per-game mutation ownership | Hold the normalized-game mutex in one process, start an install in another, then create a controlled pending Runtime transaction marker and invoke uninstall. | The concurrent install exits nonzero before writing a competing failure state; uninstall exits nonzero before moving any live Runtime path; normal retry succeeds after each controlled blocker is released. |
| Uninstall no-op and receipt identity | Run normal uninstall, then run it again within the same second-scale window. | The first run removes only DTMAPI-owned Runtime targets; the second exits zero with explicit “no files were removed” wording; both receipts have unique collision-resistant names and earlier evidence is not overwritten. |
| Host selection | First set `DTMAPI_POWERSHELL_HOST` to an executable that is not PowerShell; then, in a separate ordinary-candidate fixture, make an earlier candidate unavailable or probe-invalid while a later compatible host remains. | The explicit override fails closed before mutation. Without an override, selection continues after an unusable ordinary candidate, and only the selected compatible host runs the action. |
| BepInEx sources and partial install | Test a valid package-local ZIP, an invalid package-local ZIP with a controlled valid fixed-hash fallback, and a fake partial `BepInEx\core\BepInEx.dll`; place unrelated plugin/config sentinels in the existing tree. | Valid bundled bytes are preferred; invalid bytes are never extracted; fallback repairs the partial core; unrelated plugin/config sentinels remain byte-identical; failure removes/restores only installer-owned writes. |
| Strict install / optional diagnostics | Deliberately introduce a parser error into packaged `analyze-startup-evidence.ps1`, run install, restore the package helper, and run install again as repair. | The diagnostic parser error is visible as a warning but does not block the first install; the second install repairs the installed helper and clean status passes. Scripts required for the install action remain strict. |
| First-receipt genesis and classification | In `test-runtime-upgrade-transaction.ps1`, inject temporary first-write failure, temporary publish failure, all-attempt exhaustion, scratch-cleanup failure, and the next clean retry. Independently fixture an empty shell, temp-only shell, unknown file, child/candidate directory, matching state root, invalid receipt, orphan state, and valid old receipt. Run the complete sequence once under PowerShell 7 and once under Windows PowerShell 5.1. | Six-attempt retry converges; the first receipt is read back before candidate/state directories exist; exhausted genesis leaves no poison when the shell is provably sterile; only sterile shells are repairable; unsafe/invalid/orphan fixtures fail closed; valid old receipts still recover through install; uninstall refuses real recovery but may remove sterile shells; status is read-only and accurately coded. |
| Runtime transaction faults | Run `test-runtime-upgrade-transaction.ps1` under PowerShell 7 and Windows PowerShell 5.1 across every tracked install/rollback fault phase. | Every pre-commit fault preserves the previous live state, every committed/interrupted state is reconciled as specified, rollback-failure retry succeeds, and no safe-to-remove transaction residue remains. |
| Path compatibility and discovery | Use the exact parenthesized Workshop package shape above plus temp game dirs with spaces, parentheses, `&`, `;`, and non-ASCII text. Test malformed/invalid explicit `DTMAPI_GAME_DIR`, package-colocated discovery, and the current Workshop package library's `appmanifest_2285550.acf`. Assert the current source contains no Registry Steam-root or `libraryfolders.vdf` read. | Valid paths install/check; no compound-block CMD parser failure occurs; invalid explicit paths emit `DTM-E1002` and never fall through to another Steam install; discovery never scans unrelated Steam libraries. |
| Player messages and summaries | Exercise running-game/lock, path, host-policy, local file-access, online fallback, payload, first receipt, pending/unsafe transaction, success, no-op uninstall, and healthy check outcomes. | Stable codes match their owner; Chinese precedes English in PowerShell action output; technical path/exception remains visible; local `DTM-E1102` says the failure is usually not Windows Firewall; `DTM-E1103` appears only after an online fallback is attempted; each action has one authoritative final summary. |

## Known Interpretation Notes

- `3_check_dtmapi_status.bat` exits non-zero when DTMAPI is not installed. That is expected for install verification. If the player just ran the uninstaller, missing DTMAPI runtime files are expected.
- The in-game report export and `4_collect_dtmapi_logs.bat` are different paths. Always test the root bat wrapper because CMD quoting bugs do not appear in PowerShell parser checks.
- AST parsing proves syntax only. It does not prove module/cmdlet availability, so a parser-only Windows PowerShell 5.1 result is never sufficient installer acceptance.
- `dist\workshop-packages` can be stale. Prefer the local upload directory and subscription directory for player-facing parity checks.
- `SterileNoReceipt` is the only receiptless class that install/uninstall may remove. A status run that changes its fixture is a test failure.
- Do not classify an offline file lock, access denial, antivirus quarantine, or DLL load-policy block as a firewall problem. Network/firewall/proxy guidance is valid only when the fixed-hash online BepInEx fallback was actually entered and failed.
