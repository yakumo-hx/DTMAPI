# ISSUE-022: Runtime tools transaction move is denied on some player systems

## Status

- State: `open / mitigated in the 0.6.1 candidate pending affected-player acceptance`
- Opened: `2026-08-07`
- Severity: high
- Area: Runtime Workshop install transaction / support tools / endpoint security compatibility
- Related review: `docs/reviews/manual-qa/2026/20260807-0001-runtime-tools-move-access-denied.md`
- Owning updates: `docs/updates/2026/20260807-0001-runtime-no-player-doctor-hotfix-package.md`, `docs/updates/2026/20260807-0002-runtime-installer-061-simplification.md`

## Symptom

Two players reached the PowerShell Runtime transaction successfully but repeatedly failed when the installer moved `DTMAPI\.runtime-install-transaction-*\candidate\tools` into `DTMAPI\tools`. The exception is access denied. One player retained four failure receipts; every rollback succeeded before Runtime commit.

## Current Classification

- Confirmed: failure is after host probing and candidate preparation, at the tools-directory `Directory.Move` boundary.
- Confirmed: it is not the earlier parenthesized Workshop-path CMD parse failure and not a missing PowerShell hash/archive capability.
- Confirmed: the candidate tools tree includes the self-contained unsigned Player Doctor executable in the normal package.
- Confirmed externally: one affected player could install the unchanged normal subscription package after disabling both Windows firewall and their PC-manager security product; adding trust alone had not worked.
- Supported class, exact mechanism unproven: endpoint-security interference now has direct player evidence. A real-time file/behavior scanner locking the newly copied executable remains the leading mechanism.
- Not isolated: firewall and PC manager were disabled together. Because this is an offline local directory move, the PC manager's file/behavior protection is more consistent with the failure than a network firewall, but no single-variable run or product event log identifies the component.
- Alternatives not yet excluded: Controlled Folder Access, ACL differences, indexing/synchronization software, or another process holding a child handle.

## Evidence

- `D:\下载\DTMAPI-logs夜黑魔铃\20260806-191015\DTMAPI-state\install-state.failed-20260806-185826-489-4582b904.json`
- The same evidence directory contains three later receipts with the identical phase/error at `19:03:16`, `19:06:20`, and `19:09:18`.
- A second player's `F:\steam` screenshots show the same access denial at the equivalent later source line during two attempts.
- The second player later reported that adding trust did not help, while simultaneously disabling firewall and PC manager allowed the normal subscription package to install. No security-product event log or single-variable result was supplied.

## Current Attempt

Build a standalone `0.6.0` Runtime-only hotfix package that contains no project-owned EXE and intentionally omits Player Doctor while preserving normal Runtime install/check/collect/uninstall semantics. This package is both a player workaround and a controlled isolation test; it is not yet the canonical Workshop package.

## 2026-08-07 Local Package Evidence

- The final ZIP contains zero EXEs and zero Player Doctor entries.
- Windows PowerShell 5.1 parsing and the disposable subscription matrix passed with zero blockers.
- The exact parenthesized Workshop path plus a Chinese/parenthesized/ampersand game path passed install, status, collection and uninstall with Windows PowerShell 5.1 forced.
- Local evidence validates the workaround package itself. The issue remains open because only an affected-player run can determine whether removing the EXE changes the external access denial.

## 2026-08-07 Affected-Player Security-State Evidence

- The affected player has not run the no-EXE hotfix package, so it has no external acceptance result yet.
- The unchanged normal subscription package succeeds on that machine when both firewall and PC manager are disabled. This supports the endpoint-security failure class but does not prove Player Doctor is the trigger.
- An ordinary trust rule can miss the destination transaction tree or behavior-protection layer; its failure does not eliminate endpoint security as the cause.
- The preferred isolation remains a no-EXE-package run with protections enabled. Success would strongly support EXE scanning/locking; the same move failure would point to broader PowerShell or transaction-directory interception.

## Acceptance Criteria

- The standalone ZIP contains zero `.exe` files.
- Windows PowerShell 5.1 parses all packaged scripts.
- A temporary valid game-shaped directory passes install, status, broad collection, uninstall, and expected post-uninstall status.
- Parenthesized package paths plus space/non-ASCII/`&` game paths reach each action without CMD failure.
- Affected-player execution determines whether removing the Doctor executable changes the real failure.

The issue remains open until external player evidence distinguishes the Doctor/EXE lock hypothesis from broader endpoint-security interception or the remaining access-denied causes. Disabling security protections is diagnostic evidence, not an accepted long-term workaround.

## 2026-08-07 0.6.1 Candidate Mitigation

- The canonical 0.6.1 Runtime candidate now contains zero EXEs; Player Doctor is a separate opt-in read-only support tool and is absent from normal package/install/status receipts.
- The built package passed a real four-BAT Windows PowerShell 5.1 matrix from a Workshop path containing spaces, parentheses, Chinese and semicolon into a game path additionally containing ampersand. The matrix exercised controlled BepInEx failure/rollback, successful offline install, status, bounded collection and uninstall while preserving third-party plugin/config sentinels.
- The default Workshop subscription audit passed with zero blockers: missing/empty game targets failed, valid install/status/collect/uninstall passed, and post-uninstall status returned a clear missing-Runtime result rather than success.
- The package has five exact Runtime assemblies, four BATs, nine PowerShell scripts and zero EXEs. These local results mitigate the package-side dependency and path/transaction risks, but are not a live-game or affected-player acceptance.
- ISSUE-022 therefore remains open. It may close only after a corrected normal subscription package succeeds on an affected machine with ordinary protection restored, or after equivalent external evidence identifies and resolves the remaining blocker.
