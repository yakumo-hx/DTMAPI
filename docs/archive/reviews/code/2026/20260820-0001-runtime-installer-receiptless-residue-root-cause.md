# Runtime installer receiptless-residue root-cause review

- Date: `2026-08-20`
- Status: `recorded`
- Area: `release/workshop/installer/0.6.1/transaction/retry/player-messages`
- Source request: improve only the current PowerShell-based 0.6.1 installer without changing Runtime, Mod support, versions, or the four public actions
- Related issues: [ISSUE-022](../../../../debug/issues/ISSUE-022-20260807-runtime-tools-move-access-denied.md), [ISSUE-023](../../../../debug/issues/ISSUE-023-20260808-runtime-installer-stress-boundaries.md), [ISSUE-025](../../../../debug/issues/ISSUE-025-20260820-runtime-installer-receiptless-residue.md)
- Owning update: [20260820-0001](../../../updates/2026/20260820-0001-runtime-installer-061-reliability-ux.md)

This review freezes the root cause before another installer implementation. It
does not authorize a Runtime rebuild, V2 promotion, Steam upload, live-game
install, or change to the public four-action boundary.

## Player symptom translated from screenshots and support bundles

Several players reached the current PowerShell installer and then received:

```text
Interrupted DTMAPI Runtime transaction has no recovery receipt:
<game>\.dtmapi-runtime-install-<stamp>
```

Removing or renaming one reported directory did not permanently fix the
machine. A later attempt could leave a new directory with a new stamp and fail
the same way. In one fully isolated case, Lenovo PC Manager / Huorong endpoint
protection was the external blocker; disabling that product allowed the manual
Runtime files to load. Windows Firewall was not the owner of the local file
operation.

## Confirmed root cause

`install-to-game.ps1` creates the Runtime transaction directory before the
first durable `transaction.json` exists. The receipt writer uses a temporary
file followed by `Move` or `Replace`, with no transient retry. If endpoint
security, a file filter, or a short-lived handle denies that first write or
publication:

1. the transaction root already exists;
2. no final recovery receipt exists;
3. the outer trap sees an in-memory transaction and attempts normal rollback;
4. rollback tries to write another receipt and can fail for the same reason;
5. the root survives;
6. the next installer enumerates it and fails immediately because it has no
   receipt.

The later failure is therefore deterministic installer retry poisoning. The
external security product explains the initial access denial, but not why a
provably pre-mutation shell prevents every future retry.

The current receipt writer also removes temporary and backup files in a
`finally` block without protecting cleanup exceptions. A cleanup error can
replace the primary write/publish exception and make diagnosis worse.

## Required safety distinction

A receiptless directory is not automatically safe to delete. The installer
must distinguish:

- a sterile pre-receipt shell: exact direct child, valid stamp, no reparse
  point, no matching state transaction, no final receipt, and only empty or
  known receipt temp/backup files;
- an unsafe receiptless transaction: any unknown entry, child directory,
  candidate/recovery material, reparse point, or matching state root;
- a valid receipt transaction: schema and every expected path validate;
- an invalid receipt or orphaned state transaction: fail closed.

Only the first class proves that live Runtime/state mutation could not have
started under the current ordering. It may be removed after revalidation.

## Rejected fixes

- Blindly deleting every `.dtmapi-runtime-install-*` directory would erase the
  only recovery authority for a genuinely interrupted commit.
- Treating every receiptless root as a pending recoverable transaction leaves
  the retry-poisoning bug intact.
- Disabling Windows Firewall does not resolve an offline `WriteAllText`,
  `Move`, or `Replace` denial. Network/firewall guidance belongs only to the
  optional online BepInEx fallback.
- Enabling the isolated V2 candidate would change the current authority and
  would not be a bounded 0.6.1 correction.
- Rebuilding Runtime or changing the 0.6.1/0.6.1.0/0.5.3.0 authorities cannot
  fix this installer state-machine defect.

## Implementation boundary

- Establish, read back, and fully validate the first schema-1 receipt before
  creating a state transaction or any candidate/recovery directory.
- Retry only transient I/O/access failures, with bounded delays and fresh temp
  names. Cleanup is best-effort and never masks the primary exception.
- Share one read-only classifier among install, uninstall, and status.
- Install may recover valid receipts and remove sterile shells. Uninstall may
  remove sterile shells but remains fail-closed for real or unsafe
  transactions. Status reports classifications without mutation.
- Remove global Steam registry/library scanning. Explicit configuration,
  package-colocated discovery, and the current Workshop library are the only
  automatic sources.
- Add stable Chinese-first/English-second message codes and final summaries;
  preserve detailed exceptions below the summary.

## Acceptance

- Windows PowerShell 5.1 and PowerShell 7 independently pass consecutive
  first-write, first-publish, retry-exhaustion, cleanup, restart, recovery,
  uninstall, and read-only status cases.
- Exhausted first-receipt failure leaves no poison root when safe cleanup is
  possible; the next clean retry succeeds.
- Unsafe receiptless, invalid-receipt, and orphan-state fixtures never mutate
  live files.
- The frozen five Runtime DLLs, dormant Compatibility Host, all version
  authorities, BepInEx ownership, and four public actions remain unchanged.

## Implementation result

The bounded correction is implemented by
[Update 20260820-0001](../../../updates/2026/20260820-0001-runtime-installer-061-reliability-ux.md).
The shared classifier now preserves the safety distinction above, first
receipt publication is retried and read back before candidate/state creation,
and status remains read-only. The dual-host transaction matrix passed `28`
cases per host, including exhausted genesis followed by a successful clean
retry. A refreshed one-time package candidate passed the player-style
Workshop matrix and the independent subscription-package audit with `0`
blockers. This is local mitigation evidence, not an affected-player closure.
