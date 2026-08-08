# MoreEquipmentSlots Mail Observer Final Acceptance

Date: 2026-07-30
Status: `recorded / independently accepted / P0=0 P1=0 P2=0 / publication unchanged`
Audited HEAD: `879953d9a9b85ba3b6d0315b6acfdcd927d184b1`
Audited commits: `66c21bdf`, `879953d9`

Owning implementation Update:

- [`20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)

Prior audit:

- [`20260730-0012`](20260730-0012-moreequipment-generic-cold-oracle-fix-reaudit.md)

## Scope

This is the requested independent final acceptance of the bounded
MoreEquipmentSlots QA pending-mail correction. It checks the unreadable native
mail chain, the causal separation of button-mail, shield-mail and unreadable
authority failures, the production QA composition, lifecycle ordering,
provenance and the affected focused gates.

The audit also inspected the changed area for new P0, P1 or P2 defects. It did
not install or freeze a Runtime, launch Doloc Town, mutate a save, build a
package, run a complete Release, GC ladder or long test.

## Independent Findings

No new P0, P1 or P2 was found.

### Pending-mail authority now fails closed

The production pending-mail reader obtains `archiveHandle` and delegates to
the same helper executed by QA Unit. The helper throws an
`InvalidOperationException` when any of these stages is null, missing or not
enumerable:

- `archiveHandle`;
- `archiveHandle.farmData`;
- `archiveHandle.farmData.emailManager`;
- `archiveHandle.farmData.emailManager.emails`.

A string is explicitly rejected even though it implements `IEnumerable`.
Zero is returned only after the native mail collection has been accepted as an
enumerable and its enumeration completes. Reflection/property and enumeration
exceptions are not translated into zero.

### Negative cases have distinct causes

The generalized cold-distribution policy calls the pending-mail rejection
policy before calculating or comparing logical totals. Its focused checks
therefore isolate:

- one pending `grandmas_button` mail, requiring the exact button-mail failure;
- one pending `box_hat` mail, requiring the exact shield-mail failure;
- a readable empty mail collection, which is the positive zero case;
- null `archiveHandle`, missing `farmData`, missing `emailManager`, missing
  `emails`, and non-enumerable `emails`, each of which must throw.

The button and shield cases assert the item-specific exception text, so they
cannot pass through the old duplicate-total failure. The unreadable-authority
fixtures are separate from both item-mail fixtures and from the readable-empty
control.

### The production QA path uses the reviewed policies

`MoreEquipmentSlotsNoNativeSaveColdObserver` calls the production native-mail
reader once for `grandmas_button` and once for `box_hat`, then passes both
counts into `ValidateMoreEquipmentSlotsColdItemDistribution`. That validator
invokes `RequireNoPendingMoreEquipmentSlotsMail` before the item distribution
can be accepted.

The executable QA Unit uses the same internal helper and validator. The
save-mode test additionally freezes the production source composition. This is
layered source/unit coverage; it is not described as a new game run.

### Lifecycle ordering is correct

Commit `66c21bdf` was created before implementation commit `879953d9` and
returned the owning Update to:

```text
Lifecycle Status: implemented
Related Issue State: open
```

The July monthly ledger contains the same lifecycle, validation, runtime and
issue dimensions. Those values remained unchanged throughout this independent
audit. The prior workflow-order P2 is therefore closed for this correction.

### Review 0012 provenance remains accurate

Repository history confirms:

- `c2c215e4` committed Review `0011`, the generalized QA/runner correction and
  the refreshed evidence allowlist;
- `bb14f75b` changed only the owning Update;
- `66c21bdf` committed Review `0012`, reopened the Update/monthly ledger and
  refreshed the existing allowlist;
- `879953d9` contains only the bounded QA implementation and focused test
  changes.

Review `0012` also correctly states that the PowerShell runner matcher is
source-shape coverage rather than a new parameterized executable matcher. This
correction does not expand that claim. The older Review `0011` remains
historical and was not rewritten during this acceptance, as requested.

The retained `161355` game result remains evidence only for its executed
two-item state. No new game, Runtime, Product, package or save claim is derived
from the source-only correction.

## Independent Focused Validation

The following passed at clean audited HEAD:

```text
DTMAPI.GameBridge.DolocTown.QA Release build:
  PASS, 0 warnings, 0 errors

DTMAPI.QaUnitTests Release build/run:
  PASS, 0 warnings, 0 errors

DTMAPI Unit focuses:
  moreequipment-product: PASS
  moreequipment-cold-host: PASS
  moreequipment-acceptance-routing: PASS
  build retained the 10 pre-existing DebugConsole nullable warnings, 0 errors

test-game-smoke-save-modes.ps1:
  PASS

PowerShell parse:
  current PowerShell / run-game-smoke.ps1: PASS
  current PowerShell / test-game-smoke-save-modes.ps1: PASS
  Windows PowerShell 5.1 / run-game-smoke.ps1: PASS
  Windows PowerShell 5.1 / test-game-smoke-save-modes.ps1: PASS

test-batch4-qa-semantic-inventory.ps1:
  PASS

check-product-catalog.ps1:
  PASS (27 products / 11 public / 22 Workshop items / 48 API rows)

check-test-artifact-governance.ps1:
  PASS

check-doc-governance.ps1:
  PASS (6031 checks)

build-evidence-retention-allowlist.ps1 -Check:
  PASS (490 source files / 896 smoke runs / 62 Runtime identities /
        19 durable roots)

git diff --check:
  PASS
```

The repository worktree was clean before this Review was created. The checks
left no tracked or untracked test residue, and no `DolocTown.exe` process was
running.

## Verdict

The residual QA evidence-integrity P2 from Review `0012` is closed. The
generic MoreEquipmentSlots cold observer no longer converts an unreadable
top-level native mail authority into a confirmed zero, and its button-mail,
shield-mail and unreadable-chain failures are causally distinct.

This bounded correction is independently accepted with
`P0=0 / P1=0 / P2=0`. The owning Update may now advance through its normal
post-audit lifecycle update. This Review does not itself change that Update,
the monthly ledger, Review `0011`, the evidence allowlist, product/package
bytes or publication authority.
