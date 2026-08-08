# Runtime Workshop Installer Boundary

Status: canonical current source boundary for the 0.6.1 Runtime Workshop installer.

## Product Contract

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

## Ownership

| Layer | Owns | Must not own |
| --- | --- | --- |
| Root BAT | stable filename, action name, clean CMD re-entry | host search, labels, PowerShell script lists, install semantics |
| Shared CMD dispatcher | action allowlist, candidate host order, host override, probe call, target invocation, pause and exit-code propagation | file hashing, ZIP extraction, game/runtime mutation |
| Host probe | host identity, language mode, required script existence/AST parsing, JSON and shared .NET capability checks | selecting game paths or changing install state |
| `common.ps1` | portable SHA-256, ZIP extraction, fixed-file download and common path/runtime helpers | product/package ownership decisions |
| Action scripts | install transaction, Runtime-only uninstall, status semantics, broad log collection | ambient CMD assumptions or duplicated host compatibility code |

The existing Runtime directory transaction, rollback receipts, Runtime-only player uninstall and fail-closed official-local ownership boundary remain authoritative. This redesign does not replace them.

## Normal Install Sequence

The `install` action has one pre-mutation selection phase and one mutating phase:

1. The root BAT forwards only `install` to the shared CMD dispatcher.
2. An explicit `DTMAPI_POWERSHELL_HOST` selects one forced candidate and fails closed if it is unusable. Without an override, the dispatcher tries native Windows PowerShell 5.1, then PowerShell 7 candidates. Each candidate runs the read-only host probe.
3. The probe validates `FullLanguage`, required-file existence, Windows PowerShell AST parsing, JSON conversion, portable SHA-256, and portable ZIP extraction, then writes its one-use invocation proof. It does not discover the game or mutate install state.
4. The selected host runs `install-to-game.ps1` exactly once. A failed action is never repeated under another host.
5. The installer resolves the game from an explicit override/local setting first, then the current Workshop library's `appmanifest_2285550.acf`, then Steam registry/library metadata. Explicit invalid paths fail closed.
6. It rejects a running game and a non-game/drive-root mutation target, then acquires one process-lifetime mutex derived from the normalized game path. Install and uninstall for that game cannot overlap. Package validation and every later mutation remain inside that ownership window.
7. It validates the exact five Runtime DLLs and one optional Compatibility Host, verifies versions/hashes/manifest provenance, and rejects QA or EXE payloads.
8. If BepInEx/Doorstop is incomplete, `install-bepinex.ps1` installs or repairs it using the source contract below.
9. The Runtime transaction stages candidate plugin, component, tool and receipt directories; validates the staged tree; swaps only DTMAPI-owned directories; validates committed hashes/versions; and rolls back its own changes on failure.
10. Status remains a separate read-only action. Install does not claim success merely because files were copied.

## BepInEx Source And Ownership Boundary

For a player package there are two possible BepInEx byte sources, in this order:

1. the bundled fixed-version ZIP under `Content/.tools/bepinex`;
2. the fixed official release URL, downloaded only when no valid bundled ZIP is available.

Both must match the tracked SHA-256 before extraction. A source-tree bootstrap ZIP is a developer-only fallback and is never a third player-package route. An already complete BepInEx installation is an accepted existing outcome, not another source.

Extraction occurs in a game-local session directory. Installation copies files individually, backs up only files it will overwrite, records files it creates, and on failure removes only those created files and restores only those overwritten files. It must not replace or back up the player's whole `BepInEx` tree, because unknown third-party plugins and configuration are outside DTMAPI ownership.

## Optional Player Doctor Boundary

`dtmapi-player-doctor.exe` is a self-contained .NET 8 read-only support utility. It is not BepInEx, not a DTMAPI Runtime assembly, and not required for install, status, collection, or game startup. The normal 0.6.1 Workshop package contains no EXE and the Runtime transaction never installs or receipts Player Doctor. A separately supplied Doctor may still be used for an opt-in deep scan; its absence is informational.

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
12. zero EXE files and zero Player Doctor receipts/directories in the normal package and installed Runtime;
13. newest-ten full-log byte/hash parity, collision-resistant staged publication, no completed partial bundle, plus explicit crash-dump opt-in;
14. a held per-game mutation lock, pending-transaction uninstall rejection before mutation, repeated-uninstall no-op wording and unique receipts;
15. dual-host Runtime transaction fault injection, Runtime-only uninstall ownership and unexpected download-marker rejection;
16. parser and runtime-capability failure messages with exact host and exit code.
