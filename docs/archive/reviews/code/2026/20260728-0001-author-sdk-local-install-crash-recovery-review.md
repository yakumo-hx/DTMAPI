# 20260728-0001 Author SDK Local-Install Crash-Recovery Review

Status: recorded
Date: 2026-07-28
Scope: managed Advanced local-install lifecycle ownership, lost responses, process termination, deployment journal and source-state recovery
Owning Update: `docs/updates/2026/20260727-0001-dtmapi-055-prerelease-route.md`

## Source

The independent review after prerelease step 3 found two P1 gaps and one P2 evidence gap in commit `5f84bc5e`:

1. the outer installer replayed `install-local` when the JSON report was unreadable;
2. `install-local` retained the prior deployment journal and source-state bytes only in memory, so a real process termination after deployment commit could not restore the composite pre-state;
3. the focused matrix did not directly prove that a valid package for the wrong product makes no target change or that another product's source selection survives a failure.

This Review freezes the root cause and acceptance boundary. Completion remains owned by the linked Update.

## 1. Lost Response Was Treated As Permission To Mutate Again

The outer PowerShell installer invoked the complete `install-local` command a second time when the first command's JSON could not be parsed. An unreadable report does not distinguish “nothing committed” from “commit completed but the response was lost.” Replaying the command creates another deployment transaction and changes the baseline from which a later rollback operates.

Analysis: the Author SDK deployment journal and source-state are the lifecycle authorities. The outer installer may reconcile those authorities through a read-only command, but it must never replay a mutation merely because transport/output is ambiguous. If exact committed identity, version, package hash, destination tree and Local Development selection cannot be proved, the installer must fail closed and preserve recovery state.

Acceptance:

- exactly one `install-local` invocation per package attempt;
- unreadable output invokes only a read-only Author SDK reconciliation command;
- a corrupt-output fixture proves successful reconciliation after one real commit;
- an uncommitted/prepared state is not reported as success.

## 2. In-Process Rollback Was Not Process-Crash Recovery

The first correction captured `deployment journal before` and `source-state before` as `byte[]` values inside `InstallLocal`. Ordinary exceptions reached the catch block and restored them. A terminated process loses those bytes. In particular, deployment/update could durably commit the new destination and journal before Local Development source selection was written; no persisted composite phase connected the two authorities.

Analysis: the existing deployment journal must own the compound lifecycle before the first destructive move. It needs a current schema that embeds:

- exact previous journal existence/bytes;
- exact previous source-state existence/bytes;
- the attempted and prior deployment records;
- exact staging/recovery/failed paths;
- expected version and package SHA-256.

This is an extension of the existing journal, not a second receipt, checkpoint or recovery authority. `recover` must prefer this outer compound transaction over an inner deploy/update prepared phase and restore the exact pre-transaction destination, journal and source-state. A simulated process termination must bypass same-call rollback and be recovered by a separate CLI invocation.

Acceptance:

- process-style interruption after compound prepare, during update publication, after deployment commit and after source selection leaves durable `recovery-required`;
- a separate `recover` command restores the prior destination tree and exact journal/source-state bytes;
- a failure during recovery preserves the current journal and recovery material for retry;
- the final compound journal commit removes the recovery state atomically, after which read-only reconciliation proves the new commit.

## 3. Cross-Product And Wrong-Product Evidence Was Missing

The package hash mismatch test proved the immutable-copy binding but did not supply the correct hash for a valid package whose UniqueID differed from the requested Catalog row. The source-state snapshot also contained only the product under test.

Analysis: code inspection suggested both paths were safe, but the release route requires direct evidence because whole-file source-state restoration previously created a cross-product ownership risk.

Acceptance:

- a valid wrong-product package with its correct hash fails after deep package inspection and leaves the requested destination/journal/source-state unchanged;
- another product is installed and selected before the tested product;
- every ordinary failure and process-recovery case preserves that unrelated product's exact committed Local Development status.

## Rejected Conclusions

- A single CLI command is not automatically one durable transaction.
- An exception-injection test caught by the same method is not process-termination evidence.
- Retrying an idempotent-looking package operation is unsafe without an explicit idempotency/reconciliation contract.
- A whole-file source-state backup is safe only when its capture, mutation and recovery are serialized under the same game-root lock and its pre-state survives process termination.

## Validation Boundary

No game launch, save access, Runtime installation, subscription mutation or Workshop upload is needed. The focused proof uses SDK Unit fixtures plus the existing dual-PowerShell real Advanced package transaction matrix in isolated fake game roots. The complete Release suite is outside this Review.
