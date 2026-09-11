# Runtime Workshop Installer Boundary

Status: canonical source boundary for both published 0.7.0 distributions, retaining the observed 0.6.1 provenance branch. Current publication observations belong to Catalog and [Update 20260911-0011](../updates/2026/20260911-0011-runtime-y-hotfix-publication.md); candidate execution belongs to [Update 20260910-0008](../updates/2026/20260910-0008-multiplatform-070-candidate.md).

Both published physical distributions install the same `DTMAPI.Runtime` product and the
same accepted 0.7.0 Runtime DLL, compatibility-component and installed-tool
bytes; their outer package layouts and installer hosts differ. A distribution
ID is package provenance, not another Mod `UniqueID`, product row, or
managed-admission identity. The
current subscription manifest names both observed physical distributions while
retaining one Runtime product identity.

## Published Windows Distribution Contract

The player-facing Runtime package keeps four familiar BAT entry points:

- `1_install_dtmapi.bat`
- `2_uninstall_dtmapi.bat`
- `3_check_dtmapi_status.bat`
- `4_collect_dtmapi_logs.bat`

They are presentation shims, not independent installers. Each shim owns only its action name, enables command extensions, safely quotes any path expanded inside a compound CMD block, and starts one clean child CMD with `/d /e:on /v:off`.

```text
Explorer / cmd.exe
    -> root BAT shim
    -> Content/DTMAPIInstaller/tools/invoke-dtmapi-action.cmd
    -> probe-powershell-host.ps1
    -> action PowerShell script
    -> common.ps1 compatibility primitives
    -> existing Runtime transaction / status / diagnostics engine
```

The probe receives `ToolsRoot`, the allowlisted action, and a one-use nonce/result-file pair. It derives the exact script set internally; CMD never serializes a list of absolute paths through punctuation-delimited environment variables. Before launching a candidate, CMD atomically claims a unique temporary session directory and places the result file inside it; identical `%RANDOM%` sequences retry directory allocation instead of deleting or consuming another process's proof. A candidate is accepted only when it exits zero and atomically writes the exact nonce-bound proof expected by the dispatcher.

## Experimental Multi-Platform Sibling Contract

`DTMAPI-多平台` is Workshop item `3792681186` and a separate local upload leaf. It is
Steam Deck/SteamOS and ordinary Linux x64 first, retains native Windows x64,
and supplies the Windows host for macOS CrossOver. It does not replace or
modify the published Windows distribution.

```text
Steam Deck / Linux terminal
    -> one of four root .sh action shims
    -> Content/DTMAPIInstaller/hosts/linux-x64/dtmapi-installer
    -> shared managed action/transaction engine

Windows Explorer / CrossOver Run Command
    -> root DTMAPI-MultiPlatform-Installer.exe
    -> shared managed action/transaction engine
```

The candidate contains exactly one x64 PE host at the package root and one x64
ELF host under `Content/DTMAPIInstaller/hosts/linux-x64`. These unsigned,
self-contained .NET 8 hosts are installer tools only: neither may be copied to,
receipted in, or executed from the game tree. Its four new root BAT shims call
the one root PE with a fixed action and propagate player arguments and the host
exit code; they do not reuse or alter the published Windows package's
BAT/CMD/PowerShell engine. Its four shell shims contain only fixed action names
and a fixed internal host path, repair the internal execute bit when possible,
and remain valid UTF-8/LF files without a BOM.

The package manifest
`Content/DTMAPIInstaller/multiplatform-package.json` binds the exact two host
hashes, the fixed BepInEx archive, the selected Runtime payload, and
the launch-integration contract. Install, uninstall, status/check and log
collection share one managed engine. They use the same Windows mutex name as
the published installer plus a game-root-visible lock, so a Windows host and a
Linux host cannot mutate one game concurrently. New visible transactions use
the shared canonical `.dtmapi-runtime-install-*` namespace and the existing
schema-1 receipt family, with exact multi-platform engine identities for
Runtime install, BepInEx repair, and Runtime-only uninstall. The managed engine
also scans its preview/genesis namespaces plus every accepted PowerShell
`DTMAPI/.runtime-install-transaction-*` state entry. It classifies the complete
set before any recovery mutation, never adopts a canonical root paired with a
legacy PowerShell state root, rejects ambiguous/multiple/linked/non-directory
state, and deletes only a revalidated sterile shell. Conversely, the accepted
PowerShell classifier treats a canonical multi-platform engine receipt as
invalid and fails closed. This bidirectional rule prevents either installer
from recovering the other's in-flight transaction.

The existing package and host-artifact manifest family has two explicit branches:

- Schema 1 retains the fixed observed 0.6.1 Windows source receipt, Steam manifest,
  20 shared files and original Catalog comparisons. It remains readable by the new host.
- Schema 2 accepts Runtime 0.7.0 Candidate input through explicit source/output
  paths. `RuntimeSource` records its real build commit, exact release-manifest hash
  and all 21 shared file receipts, including the product-definition JSON needed by
  r5 diagnostic tools. `ImportedRuntimePayload` records the selected Windows tree;
  candidate manifests contain no old Steam source-manifest or imported-publication claim.
  The auditor compares against the exact input artifact, without changing Catalog.
  `build-multiplatform-runtime-installer.ps1` verifies committed evaluated MSBuild
  inputs before/after both publishes and generates `installer-build.json`; the
  package carries these build facts in `host-artifacts.json`. The host checks its
  embedded informational-version commit against the candidate manifest.

Unknown schema/version, mixed provenance, changed shared bytes and mismatched
installer commits are rejected before game mutation. Install-state and transaction
schemas stay at 1. Upgrade from 0.6.1 is supported; direct downgrade is rejected.
Withdrawal means Runtime-only uninstall followed by an explicit older reinstall.

Install validates all 16 required BepInEx/Doorstop archive files and repairs
only missing or mismatched framework bytes. Existing Runtime state must prove
its game/plugin path, version, binary version, commit and recognized installer
distribution before replacement: unknown distributions are always rejected;
legacy state requires a corresponding, agreeing release manifest; downgrade is
rejected; and same-version multi-platform repair requires exact owned receipts.
Uninstall moves only the Runtime allowlist through its own durable transaction
and preserves external plugins, config, logs, Mods and ContentPacks. Status is
read-only. Log collection follows neither game-tree nor external Unity-log
symlink/reparse-point parents and records skipped sources in its manifest.

Steam Deck/Linux installation is not accepted until the player also puts this
exact value in the game's Steam launch options:

```text
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

The installer does not edit Steam settings. File health, launch integration,
and fresh Runtime observation are separate status axes; copied files alone do
not prove injection. Native Windows does not need the Proton launch option.
CrossOver instead runs the root PE in the existing Steam/game Bottle and sets
that Bottle's `winhttp` library override to `Native, then Builtin`.

The multi-platform distribution remains experimental until a real Steam Deck
run proves the launch option, fresh BepInEx/DTMAPI startup log and clean
restart. The historical 0.6.1 subscription manifest `5128092030483852458` proved
that the first 36-file download was byte-identical to both the repository
candidate and local upload content: 29,770,736 bytes with normalized tree
SHA-256 `e7b011d9e183e2da386f8e9c84c415f225e5ae41df8fd75c6b0481f9c56492a4`.
The current 0.7.0 BOM-patch subscription observation is owned by [Update 20260911-0011](../updates/2026/20260911-0011-runtime-y-hotfix-publication.md); Update 20260911-0001 retains the first 0.7.0 release.
These observations prove delivery, not injection or behavior.

Steam's uploader owns one root `workshop.json` in the official local upload
leaf. It must contain only `workshop_id=3792681186`; it is update-control state,
not Runtime payload. The 0.6.1 subscription omitted it, but the observed 0.7.0
subscription delivered the exact same root control file. Record the complete
download and the content receipt excluding that file separately. Repository
candidates remain free of `workshop.json`; a subscription audit can accept it
only with `-AllowDeliveredWorkshopControlFile` and the current frozen Catalog
content/control receipts. This allowance does not change installed ownership.
When rebuilding the already-bound official local upload leaf, the package
builder may preserve this exact validated file byte-for-byte, excludes it from
the content receipt, and fails closed on a missing/other ID, nested copy,
additional property, reparse point or duplicate. Current observation does not
authorize a later upload; future upload authority remains a separate release
decision.

Unless a later section explicitly names the sibling, `Ownership` through the
original 17-item `Required Package Tests` below remain the published Windows
BAT/CMD/PowerShell contract. The sibling is governed by the experimental
contract above and its appended focused matrix.

## Ownership

| Layer | Owns | Must not own |
| --- | --- | --- |
| Root BAT | stable filename, action name, clean CMD re-entry | host search, labels, PowerShell script lists, install semantics |
| Shared CMD dispatcher | action allowlist, candidate host order, host override, probe call, target invocation, pause and exit-code propagation | file hashing, ZIP extraction, game/runtime mutation |
| Host probe | host identity, language mode, required script existence/AST parsing, JSON and shared .NET capability checks | selecting game paths or changing install state |
| `common.ps1` | portable SHA-256, ZIP extraction, fixed-file download, bounded game-path resolution, coded bilingual messages, and read-only Runtime-transaction classification | product/package ownership decisions or action-specific mutation authority |
| Action scripts | install transaction, Runtime-only uninstall, status semantics, broad log collection | ambient CMD assumptions or duplicated host compatibility code |

The existing Runtime directory transaction, rollback receipts, Runtime-only player uninstall and fail-closed official-local ownership boundary remain authoritative. This redesign does not replace them.

## Normal Install Sequence

The `install` action has one pre-mutation selection phase and one mutating phase:

1. The root BAT forwards only `install` to the shared CMD dispatcher.
2. An explicit `DTMAPI_POWERSHELL_HOST` selects one forced candidate and fails closed if it is unusable. Without an override, the dispatcher tries native Windows PowerShell 5.1, then PowerShell 7 candidates. Each candidate runs the read-only host probe.
3. The probe validates `FullLanguage`, required-file existence, Windows PowerShell AST parsing, JSON conversion, portable SHA-256, and portable ZIP extraction, then writes its one-use invocation proof. It does not discover the game or mutate install state.
4. The selected host runs `install-to-game.ps1` exactly once. A failed action is never repeated under another host.
5. The installer resolves the game in one bounded order: `DTMAPI_GAME_DIR`; `local.settings.json`; a complete package placed beside `DolocTown.exe`; then, only when the package is under the standard Workshop shape, that package's own Steam library `appmanifest_2285550.acf`. It does not enumerate Registry Steam roots or `libraryfolders.vdf`. Explicit invalid, overlong, or malformed paths become `DTM-E1002` and fail closed instead of falling through to another machine location.
6. It rejects a running game and a non-game/drive-root mutation target, then acquires one process-lifetime mutex derived from the normalized game path. Install and uninstall for that game cannot overlap. Package validation and every later mutation remain inside that ownership window.
7. It validates the exact five Runtime DLLs and one optional Compatibility Host, verifies versions/hashes/manifest provenance, and rejects QA or EXE payloads.
8. The shared classifier handles any earlier transaction before a new one starts. Install restores a valid schema-1 receipt, removes only a revalidated sterile receiptless shell, and fails closed on unsafe/invalid/orphan state.
9. For a new transaction, the installer creates only the direct-child Runtime root, publishes and reads back the first schema-1 `transaction.json`, and validates every owned path before it creates the state transaction, candidate, or recovery directories. Transient `IOException`/`UnauthorizedAccessException` failures receive six total attempts with `200/400/800/1200/1600 ms` between retries and a fresh scratch name per attempt. Scratch cleanup is best-effort and cannot replace the primary failure.
10. Only after the first receipt is authoritative may candidate plugin, component, tool and receipt trees be staged. If BepInEx/Doorstop is incomplete, `install-bepinex.ps1` installs or repairs it using the source contract below. The Runtime transaction then swaps only DTMAPI-owned paths, validates committed hashes/versions, and rolls back its own changes on failure.
11. Status remains a separate read-only action. Install does not claim success merely because files were copied.

## Runtime Transaction Classification

Install, uninstall, and check call the same read-only classifier in
`common.ps1`; their mutation authority remains deliberately different:

| Classification | Install | Uninstall | Check |
| --- | --- | --- | --- |
| `Clean` | continue | continue | report normal install health |
| `RecoverableReceipt` | restore by the existing schema-1 recovery path, then continue | stop before mutation with `DTM-E1302` | `RECOVERY_PENDING`, exit `1` |
| `SterileNoReceipt` | precisely remove and continue with `DTM-W1301` | precisely remove and continue with `DTM-W1301` | `REPAIRABLE_STALE`, exit `1`, no mutation |
| `InvalidReceipt` / `UnsafeNoReceipt` | stop with `DTM-E1303` | stop with `DTM-E1303` | `BLOCKED`, exit `1` |
| `OrphanState` | stop with `DTM-E1303` | stop with `DTM-E1303` | `BLOCKED`, exit `1` |

A sterile shell must be a non-reparse direct child named
`.dtmapi-runtime-install-<valid stamp>`, have no final receipt and no matching
state transaction, and contain no entry except ordinary
`transaction.json.tmp-*` / `transaction.json.bak-*` files. Unknown files,
subdirectories, candidate/recovery material, malformed receipts, matching
state roots, or path escapes are never auto-deleted.

## BepInEx Source And Ownership Boundary

For a player package there are two possible BepInEx byte sources, in this order:

1. the bundled fixed-version ZIP under `Content/.tools/bepinex`;
2. the fixed official release URL, downloaded only when no valid bundled ZIP is available.

Both must match the tracked SHA-256 before extraction. A source-tree bootstrap ZIP is a developer-only fallback and is never a third player-package route. An already complete BepInEx installation is an accepted existing outcome, not another source.

Extraction occurs in a game-local session directory. Installation copies files individually, backs up only files it will overwrite, records files it creates, and on failure removes only those created files and restores only those overwritten files. It must not replace or back up the player's whole `BepInEx` tree, because unknown third-party plugins and configuration are outside DTMAPI ownership.

## Optional Player Doctor Boundary

`dtmapi-player-doctor.exe` is a self-contained .NET 8 read-only support utility. It is not BepInEx, not a DTMAPI Runtime assembly, and not required for install, status, collection, or game startup. The published Windows 0.6.1 Workshop package contains no EXE and the Runtime transaction never installs or receipts Player Doctor. The multi-platform sibling's one root PE is its allowlisted installer host, not Player Doctor, and likewise never enters the game tree. A separately supplied Doctor may still be used for an opt-in deep scan; its absence is informational.

The player-facing `4_collect_dtmapi_logs.bat` exports the newest ten DTMAPI current/history logs without a size cap. Each selected log is accepted only when the source remains stable and the destination length and SHA-256 match; the whole bundle is first built in a unique sibling staging directory and published by one directory rename. A failed collection therefore leaves no completed-looking partial bundle. Crash dumps still require an explicit `-IncludeCrashDumps` request. Internal evidence callers that pass `-OutputDirectory` retain their bounded merge behavior and are not the player-facing export contract.

## CMD Path Boundary

`cmd.exe` expands and parses an entire parenthesized compound command before it evaluates the enclosing `if` or executes a line. A value such as `E:\Program Files (x86)\...` can therefore terminate a block early when it is expanded into an unquoted command, even if that branch would not run. Enabling command extensions does not fix this grammar failure.

The installer contract consequently requires:

- no duplicated host logic or labels in public root BAT files;
- every path expansion inside a compound block to remain quoted;
- delayed expansion disabled so `!` in a player path is not consumed;
- an exact generated-package regression rooted under `Program Files (x86)\steam\steamapps\workshop\content\2285550\3743016467`.

## Supported Host Boundary

The package supports either Windows PowerShell 5.1 or PowerShell 7 when the candidate host can:

- run in `FullLanguage` mode;
- parse the action's required scripts;
- provide `ConvertFrom-Json` and `ConvertTo-Json`;
- execute the packaged .NET SHA-256 helper;
- for install, execute the packaged .NET ZIP extraction helper.

`Get-FileHash` and `Expand-Archive` are not player-host requirements. PowerShell module order, optional PowerShell 7 modules and user profiles must not decide whether those two capabilities exist. Every player action uses `-NoProfile -ExecutionPolicy Bypass`; policy enforcement outside that process boundary can still block execution and must produce a visible host-probe failure.

The package does not promise to repair a completely broken `.bat` file association, enterprise application-control denial, a non-FullLanguage PowerShell policy, unreadable game storage, or security software that removes payload files. It must keep the window/output useful enough to classify those states.

## Action Semantics

- Install is strict: invalid game path, invalid payload, running game, failed capability, transaction or committed-state mismatch exits nonzero.
- Check is read-only and reports missing/invalid required state without pretending uninstall state is healthy.
- Collect is broad: unrelated missing logs or a diagnostic helper finding must not prevent other evidence collection.
- Uninstall remains Runtime-only and preserves Workshop/local Mod ownership outside the Runtime receipt. It shares the per-game mutation lock, fails before moving live paths when an installer transaction is pending, and reports an explicit no-op when no owned Runtime target exists.
- A target action failure is never retried under a second host because that could repeat a partially entered transaction. Host fallback happens only during probe-only selection before the action starts.

PowerShell action output uses stable `DTM-E*`, `DTM-W*`, and `DTM-S*` codes,
with Chinese first, English second, then the original path/technical detail.
Local file access or endpoint-security interference is `DTM-E1102` and
explicitly is usually not Windows Firewall. `DTM-E1103` is reserved for an
actually entered online BepInEx fallback that fails. Install, uninstall, and
check end in one authoritative summary; the ASCII-only CMD bootstrap repeats
the authoritative code where it can do so without making legacy CMD parsing
code-page dependent.

## Required Package Tests

Every installer/package change must cover:

1. exact generated package copy under `Program Files (x86)\steam\steamapps\workshop\content\2285550\3743016467`;
2. paths with spaces/non-ASCII text and a separate path containing `&`;
3. outer `cmd.exe /d /e:off /v:off` for all four BAT files;
4. Windows PowerShell 5.1 forced through the shared dispatcher;
5. no direct packaged invocation of `Get-FileHash` or `Expand-Archive`;
6. install -> check -> collect -> uninstall against a temporary valid game-shaped directory;
7. missing and invalid explicit game directories;
8. ordinary default-host selection plus an explicit PowerShell 7 override matrix;
9. a deliberately unusable explicit override that fails before mutation, plus an unavailable/rejected ordinary candidate followed by a later compatible host before any action starts;
10. fixed-hash bundled BepInEx install, fixed-hash network fallback, invalid bundled ZIP rejection and partial-install repair without replacing unknown BepInEx files;
11. an intentionally invalid optional diagnostic helper that warns without blocking install, followed by a clean repair install;
12. zero EXE files and zero Player Doctor receipts/directories in the published Windows package and installed Runtime;
13. newest-ten full-log byte/hash parity, collision-resistant staged publication, no completed partial bundle, plus explicit crash-dump opt-in;
14. a held per-game mutation lock, pending-transaction uninstall rejection before mutation, repeated-uninstall no-op wording and unique receipts;
15. dual-host Runtime transaction fault injection, including first-receipt write/publish retry, retry exhaustion, best-effort cleanup, sterile/unsafe/invalid/orphan classification, next-run convergence, Runtime-only uninstall ownership and unexpected download-marker rejection;
16. coded parser/runtime-capability/local-access/network/package/transaction messages, one final action summary, exact host and exit code;
17. a source assertion that Registry Steam-root and `libraryfolders.vdf` discovery cannot return, while current-package-library `appmanifest_2285550.acf` discovery remains.

The multi-platform sibling additionally requires one focused package and
player-like matrix:

1. exact byte parity for every imported shared Runtime path: frozen schema-1
   0.6.1 or explicit schema-2 0.7.0 Candidate source;
2. exactly one root x64 PE, one internal x64 ELF, four BAT entries and four
   UTF-8/LF/no-BOM shell entries; repository/subscription have zero
   `workshop.json`, while the official upload leaf may have exactly one
   validated root control file for item `3792681186`;
3. Windows and Linux host install -> read-only status -> two collision-safe log
   collections -> Runtime-only uninstall -> repeated no-op uninstall against
   separate temporary game-shaped directories;
4. spaces, parentheses, `&`, `;` and non-ASCII paths, invalid explicit-path
   preflight, corrupt-payload preflight, and package-root escape rejection;
5. byte-identical preservation of external BepInEx plugins/config/logs,
   DTMAPI config/history logs, Mods and ContentPacks sentinels;
6. no PE, ELF, host manifest or multi-platform package manifest copied into or
   receipted by the game;
7. Windows PowerShell 5.1 package audit plus WSL Bash syntax and actual Linux
   host execution when WSL is available;
8. preserve the observed first-upload subscription receipt independently from
   any later local successor, and complete a real Steam Deck run proving the
   required launch option plus fresh startup evidence; byte parity alone is not
   player/runtime acceptance.
9. for a 0.7.0 candidate, supply `-PreviousPackageRoot` to the existing focused
   lifecycle script: execute original 0.6.1 install, new-reader old-package status,
   upgrade, uninstall/reinstall, downgrade/old-reader rejection and candidate
   provenance failures on Windows and available WSL Linux. Keep the existing
   transaction, cross-engine rejection, external sentinel and link/privacy matrix.
