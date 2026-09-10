# ISSUE-025: Runtime installer can poison all later retries with a receiptless transaction root

- State: `mitigated`
- Current boundary: First-receipt retry/readback and shared fail-closed classification passed the dual-host and package matrices; affected-player convergence remains pending.

## Status

- Opened: `2026-08-20`
- Mitigated locally: `2026-08-20`
- Severity: high
- Area: Runtime Workshop install transaction genesis, recovery classification, and player diagnostics
- Related review: `docs/reviews/code/2026/20260820-0001-runtime-installer-receiptless-residue-root-cause.md`
- Owning update: `docs/updates/2026/20260820-0001-runtime-installer-061-reliability-ux.md`

## Symptom

After an initial file-access or endpoint-security failure, players can retain
`<game>\.dtmapi-runtime-install-*` without `transaction.json`. Every later
install then stops on `has no recovery receipt`, even if the original blocking
condition was temporary. Repeated attempts can create new stamped remnants.

## Confirmed facts

- The current installer creates the Runtime transaction root before its first
  final receipt is durable.
- The first receipt write/publish has no transient retry.
- The outer failure path attempts normal rollback whenever an in-memory
  transaction object exists, even when no receipt was ever established.
- The next install treats every matching directory as a real transaction and
  requires a receipt, so a sterile pre-mutation shell becomes retry poison.
- Lenovo PC Manager / Huorong endpoint protection was confirmed as one player
  environment trigger. That does not excuse the persistent installer state.
- Windows Firewall is not the owner of these offline local file operations.

## Distinction from earlier issues

- ISSUE-022 owns player access-denied failures while moving transaction
  content, especially `candidate\tools`, and endpoint-security isolation.
- ISSUE-023 owns host proof, mutation locking, pending-transaction uninstall,
  unique log publication, and truthful no-op receipts.
- ISSUE-025 owns the missing first-receipt state, safe receiptless
  classification, retry convergence, and actionable bilingual output.

## Acceptance criteria

- The first receipt is written, read back, and path/schema validated before
  any candidate/state transaction or live mutation.
- Transient write/publish failures receive bounded retries under both supported
  PowerShell hosts.
- A sterile receiptless shell is safely repairable; any ambiguous content
  remains fail-closed.
- Install, uninstall, and status consume the same classification result while
  retaining their different mutation authorities.
- A failed attempt cannot force all future attempts to fail after the external
  blocker is gone.
- Player output differentiates game-running, path, policy, local access,
  network fallback, package, recovery, and unexpected failures in Chinese and
  English.

## Mitigation evidence

- Windows PowerShell `5.1.26100.9168` and PowerShell `7` each passed all `28`
  sequential Runtime transaction cases, including first-write/publish retry,
  all-attempt exhaustion, cleanup failure, next-run success, every shared
  classification, valid old recovery, uninstall refusal, and read-only check.
  Exhaustion left neither a transaction root nor an ordinary failure-state
  receipt before the next attempt.
- The exact 0.6.1 Workshop matrix passed from temporary paths containing
  parentheses, Chinese, spaces, `&`, and `;`; malformed explicit paths emitted
  `DTM-E1002` and did not fall through.
- The refreshed one-time package candidate passed the independent
  subscription-package audit with `0` blockers.
- Frozen Runtime/Compatibility bytes and `0.6.1`, `0.6.1.0`, `0.5.3.0`, and
  supported-version authorities remained byte-identical.
- No game was launched and no frozen package, local official directory, or
  Steam subscription/upload was changed.

The issue is locally mitigated, not closed. One later run on an affected
player machine must prove that removal of the external blocker followed by an
ordinary retry converges without another poison root.
