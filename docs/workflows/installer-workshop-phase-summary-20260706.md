# Installer And Workshop Phase Summary - 2026-07-06

Status: historical docs-only synthesis through 2026-07-06; not current installer design authority

Current authority: [Runtime Workshop Installer Boundary](../architecture/runtime-workshop-installer-boundary.md) owns BAT/CMD/PowerShell responsibilities, and [Workshop Package And Subscription Test Matrix](workshop-package-subscription-test-matrix.md) owns current package acceptance. Keep the statements below as historical rationale; do not use their host-order or probe details to override the current boundary.

Sources:

- `tools/scripts/README.md`
- `docs/workflows/workshop-package-subscription-test-matrix.md`
- `docs/workflows/codex-runtime-lock.md`
- installer and release update records through `20260703-0005`

## Core Rule

Installer and Workshop validation must test the exact artifact a player receives. Source-tree script success is not enough.

## Package Roots

| Root | Purpose | Risk |
| --- | --- | --- |
| Source tree | Development scripts and payload assembly | Can pass while packaged BAT quoting or copied payload is stale. |
| Local official upload folder | What is prepared for Steam Workshop upload | Must be hash/parity checked against source and subscription expectations. |
| Steam subscription folder | What a player actually downloads | Best proof for package layout and wrapper behavior. |
| Temporary fake game directory | Safe installer/check/uninstall validation | Good for script paths and package mechanics; not a gameplay smoke. |

## Validation Lanes

| Lane | What it catches | Notes |
| --- | --- | --- |
| Root BAT wrappers | CMD quoting, host discovery, working directory bugs | Must be tested because PowerShell-only parse checks miss BAT failures. |
| Install dry-run and real temp install | Script control flow, payload placement, BepInEx zip handling | Keep strict install-chain failures separate from support-helper warnings. |
| Check/status | Installed/not-installed reporting | Nonzero not-installed status can be expected; interpret by scenario. |
| Uninstall | Cleanup and rollback behavior | Must not depend on local Steam paths. |
| Offline logs | Player support bundle collection | `4_collect_dtmapi_logs.bat` and in-game report export are different paths. |
| Package layout and hash parity | Stale upload/subscription files | Required before asking players to reinstall. |
| PowerShell compatibility | Windows PowerShell 5.1, PowerShell 7 fallback, language mode/parser issues | Player host variability remains a first-class risk. |
| Runtime-lock protected checks | Installing, uninstalling, launching game, writing local MODS, collecting runtime proof | Use shared lock scripts for local runtime operations. |

## Current Policy

- Use local-first BepInEx payloads and verified hashes for offline installs.
- Keep the install chain strict; keep support/log helper issues warning-only when possible.
- Prefer file-based PowerShell validation over nested encoded commands.
- Probe PowerShell hosts explicitly and allow PowerShell 7 fallback when Windows PowerShell is broken.
- Validate root BAT wrappers, not only underlying `.ps1` files.
- Use `tools/scripts/wait-runtime-lock.ps1` before shared runtime operations and release it when done.

## Technical Debt

- Steam upload and subscription parity still need careful manual discipline because local source success can hide stale packaged files.
- Native Steam submit/status timeout behavior is operationally fragile and should remain documented as a release risk.
- Player machines can have unusual PowerShell policies, broken parsers, missing hosts, or PATH differences. Probe packages help, but they do not eliminate the need for player logs.
- Runtime smoke and installer matrices are separate proof types. A clean installer does not prove hooks or gameplay APIs, and a game smoke does not prove the player package.
- `run-game-smoke.ps1` has several feature-specific paths. New smoke modes should update the script README, smoke matrix, and debug/update evidence together.

## Handoff Checklist

Before changing release scripts or asking a player to reinstall:

1. Read `docs/workflows/workshop-package-subscription-test-matrix.md`.
2. Check `tools/scripts/README.md` for the affected script path.
3. Rebuild or refresh the local upload package if package contents changed.
4. Run temp fake-game install/check/uninstall and root BAT wrapper validation.
5. Verify hash parity for packaged payloads.
6. Use the runtime lock before any operation touching the local game runtime.
7. Add an update record with validation and rollback notes.
