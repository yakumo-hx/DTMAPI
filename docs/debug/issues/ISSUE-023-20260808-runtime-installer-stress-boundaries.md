# ISSUE-023: Runtime installer stress boundaries can report false success or overlap mutation

## Status

- State: `mitigated`
- Opened: `2026-08-08`
- Severity: high
- Area: Runtime Workshop host selection, install/uninstall serialization and support-log publication
- Related review: `docs/reviews/code/2026/20260807-0002-runtime-installer-three-commit-stress-audit.md`
- Owning update: `docs/updates/2026/20260808-0001-runtime-installer-061-stress-boundary-fixes.md`

## Confirmed Symptoms

The isolated 0.6.1 stress audit confirmed five boundaries that the ordinary
package matrix did not freeze:

1. an executable such as `cmd.exe` can return zero without running the
   PowerShell probe or action, and the dispatcher then prints public success;
2. concurrent installs can inspect or recover another still-live Runtime
   transaction because there is no per-game cross-process mutation owner;
3. Runtime-only uninstall can report success while a valid interrupted Runtime
   recovery tree remains and can be restored by a later install;
4. two log collections in one second can publish into the same directory;
5. a never-installed game produces an uninstall receipt but the message claims
   Runtime files were removed.

These are separate from ISSUE-022. ISSUE-022 is the affected-player
`candidate\tools` access-denied symptom; the stress findings above do not prove
that endpoint security or directory moves caused every player failure.

## Required Correction

- Host acceptance must require an invocation-specific probe result, not exit
  code alone.
- A normalized game directory must have one install/uninstall mutation owner.
- Uninstall must either reconcile a pending Runtime transaction or fail closed
  before moving live paths. The 0.6.1 focused correction chooses fail-closed.
- Every uninstall receipt and desktop bundle must be unique.
- A published DTMAPI log file must be a full, stable byte-for-byte copy; desktop
  output must be staged and renamed only after the selected set completes.
- No-op uninstall output must state that no Runtime files were found.

## Evidence (2026-08-08)

- Windows PowerShell 5.1 parser validation passed for every changed/player
  script.
- A generated 0.6.1 Runtime-only package passed
  `test-runtime-workshop-installer-061.ps1` from the exact parenthesized,
  non-ASCII Workshop path. The matrix rejected `cmd.exe` as a false-success
  host, rejected a held per-game lock, rejected uninstall with a pending
  transaction, completed two unique newest-ten full-log exports, and verified
  a truthful unique-receipt no-op uninstall.
- Follow-up real-dispatcher pressure found that concurrent CMD processes can receive
  identical `%RANDOM%` sequences and collide on a flat nonce result path. The
  final correction atomically claims a per-invocation probe directory and adds
  24 simultaneous mixed public BAT actions; host selection must succeed for
  every process before action/lock semantics decide the result.
- `test-runtime-upgrade-transaction.ps1 -HostMatrixChild` passed all 17 cases
  independently under PowerShell 7 and Windows PowerShell 5.1.
- `test-player-runtime-only-uninstall.ps1` and
  `test-installer-invalid-target-failure.ps1` passed.
- Final package `BuildCommit=db5e518a6d7f` passed the independent subscription
  audit with zero blockers, was synchronized to local official upload while
  preserving Workshop ID `3743016467`, and installed successfully into the
  real game through Windows PowerShell 5.1. Status verified Runtime `0.6.1`,
  matching provenance and all five production DLLs. Deployment evidence is
  `tmp/test-runs/runtime-installer-061-deployment-20260808-080418-2a0ff434`.
  The synchronized upload copy independently passed with zero blockers at
  `tmp/test-runs/workshop-audit-061-upload/DTMAPI Workshop Audit
  20260808-080529/Results/stress-summary.md`.
- No game launch, save mutation, Steam upload/redownload or affected-player
  acceptance was performed.

## Acceptance

The source/package acceptance is complete. Keep the issue at `mitigated` until
the repaired package is deliberately published and receives affected-player
acceptance; publication is outside this focused source correction.
