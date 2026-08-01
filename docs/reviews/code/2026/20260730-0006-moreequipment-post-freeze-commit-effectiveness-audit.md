# MoreEquipmentSlots Post-Freeze Commit Effectiveness Audit

Date: 2026-07-30
Status: `recorded`
Audited range: `6f9fd1bd..b3817244`
Owning implementation Update:
[`20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)

## Scope

This is the single independent audit requested after the lifecycle, claim,
cross-process, capture, empty-authority and replacement-package corrections.
It reviews the 32 commits after `6f9fd1bd`, the exact frozen Product package,
focused source checks and the documentation boundary.

No game, Runtime install, native save, complete Release, L0-L5, GC or long
test was run. This Review records findings only; later implementation and
validation belong in the existing Update and must not be appended here.

## Conclusion

The commits are materially effective but not release-acceptable.

Confirmed corrections include:

- production `SaveLoaded` and title callbacks now use one GameBridge feature
  fanout; the old dedicated EquipmentSlots dispatch methods are removed;
- the pre-schema route now has a durable game-root winner, cross-process
  operation lock, pending/completed evidence and atomic global capture;
- active-global reappearance, different archive bytes, interrupted capture,
  stale loser evidence and later Product revision have direct focused coverage;
- Product-loaded ownership suppresses dormant Compatibility takeover;
- the two rebuilt ZIP files are byte-identical and match the recorded package,
  DLL, manifest and Advanced-reference hashes;
- no new per-frame path or second receipt/checker family was introduced.

However, two newly confirmed P1 protocol gaps and one earlier P1 save-clock
validation gap remain in the exact frozen DLL. The replacement package and
0.5.5 release candidate therefore remain blocked.

## Findings

### P1: a successful identity-bearing GlobalFlat migration permanently blocks a different save's empty state

`FinalizeExactGlobalMigration` archives both `GlobalFlat` and
`PreSchemaGlobal`, but only `PreSchemaGlobal` publishes a permanent winner.
The empty guard later allows a deterministic global archive only when that
archive is bound through a validated completed winner:

- `EquipmentSlotDocumentStore.Migration.cs:449-552`
- `EquipmentSlotDocumentStore.Migration.cs:2225-2312`
- `EquipmentSlotDocumentStore.Migration.cs:2380-2403`

Sequence:

```text
save A, slot 2
  -> identity-bearing GlobalFlat migrates successfully
  -> Product A + exact backup + deterministic global archive
  -> no winner, because this is not PreSchemaGlobal

new save B, slot 3, no scoped/global data
  -> EnsureNoPendingPreSchemaGlobalClaimBeforeEmpty
  -> allowedTerminalArchive remains null
  -> A's valid GlobalFlat archive is classified as unbound residue
  -> B cannot obtain the normal in-memory empty state
```

The current `GlobalFlatFinalStateRejectsRecreatedAuthority` test covers a
global file recreated after archive publication. It does not cover
`A completed GlobalFlat -> B empty`. This is a player-visible multi-save
regression introduced by the generalized empty-residue guard.

The correction must bind an exact GlobalFlat archive to exactly one eligible
canonical Product authority, or use one generic terminal authority model for
both global migration kinds. It must not add another receipt family.

Minimum focused matrix:

- `GlobalFlat A completes -> B returns empty`;
- `B empty -> A cold reloads its exact Product`;
- missing/ambiguous/wrong-stamp Product keeps the archive fail-closed;
- absent Product with terminal data wakes Compatibility without falsely
  adopting or deleting another save's data.

### P1: completed-winner T1 reload falls back to the obsolete strict T0 validator on an evidence collision

The completed-winner fast path correctly accepts immutable winner revision
`T0` plus Product revision `T1 >= T0` through
`ValidateCompletedPreSchemaGlobalTerminalAuthorities`:

- `EquipmentSlotDocumentStore.Migration.cs:1446-1477`
- `EquipmentSlotDocumentStore.Migration.cs:2030-2092`

It then opportunistically publishes per-hash completed evidence. If an old
actor occupies the canonical evidence path during the pending-to-completed
transition, the catch path uses
`ValidateCompletedPreSchemaGlobalAuthorities` instead:

- `EquipmentSlotDocumentStore.Migration.cs:1583-1657`
- `EquipmentSlotDocumentStore.Migration.cs:1800-1829`
- `EquipmentSlotDocumentStore.Migration.cs:1972-2027`

That validator calls ordinary `TryLoadValidated` using winner scope `T0`.
Once Product `T1` is more than the 300-second anti-ahead tolerance beyond
`T0`, a valid terminal Product is rejected.

Existing tests cover the evidence collision at `T0`, and separately cover
winner `T0` plus Product `T1`. They do not combine the two states.

Minimum focused test:

```text
winner/evidence/Product complete at T0
-> Product advances to T1 > T0 + tolerance
-> same-hash pending evidence is restored
-> collision after evidence transition capture
-> reload succeeds from winner + terminal Product
-> winner/Product/archive bytes remain exact
-> conflicting optional evidence remains diagnostic only
```

The collision recovery must use the already-established terminal authority
rule, not the initial pending-completion revision rule.

### P1: Product v3 treats a missing native save clock as compatible

`RawScopeRevisionIsCompatible` returns `true` when either the Product document
or current native scope lacks `TotalGameSeconds`:

- `EquipmentSlotDocumentStore.cs:1066-1078`

The same missing-clock state can be produced by pre-schema conversion because
that path clones the current scope without requiring a valid clock:

- `EquipmentSlotLegacyMigration.cs:114-160`

This predates the audited 32-commit range, but the exact frozen package still
contains it. Product v3 is documented as preserving and reload-validating the
native save clock; no published Product-v3 compatibility input requires a
missing-clock wildcard. Accepting it removes the anti-ahead proof required by
the save-commit contract.

The Product reader and every global migration claim/publication path should
fail closed before writing authority when the current or stored save clock is
missing or negative. Add explicit missing-current-clock, missing-Product-clock
and pre-schema-no-clock tests.

### P2: cold Compatibility demand is cached across title and owner changes

`EquipmentSlotsService.HasColdCompatibilityStorage` caches its first result
for the process lifetime, while `ReturnedToTitle` and `EnvironmentReset` do
not invalidate it:

- `EquipmentSlotsService.cs:108-112`
- `EquipmentSlotsService.cs:152-178`

If the first probe suppresses a loaded Product and a later in-process
disable/reload removes that owner, the next save still uses cached `false` and
does not wake the Host for the newly orphaned sidecar. A transient directory
enumeration failure is also silently cached as `false`.

If Advanced Product deactivation is intentionally restart-only, that promise
must be explicit. Otherwise invalidate the probe at title/owner-list change,
make probe failure visible and cover:

```text
loaded owner -> cached false -> owner deactivated -> title
-> next SaveLoaded detects cold storage exactly once
```

This remains a lifecycle-only scan; no file watcher or per-frame polling is
needed.

### P2: evidence and Review lifecycle truth drift

The new production wiring test reaches the real Hook, feature fanout and
broker resident branch, but injects a fake backend. A separate physical
`net48` fixture covers the real Compatibility Host transaction and terminal
state. This layered evidence is reasonable, but it is not the single
Hook-to-real-Host combination fixture required literally by Review `0007`
lines 260-261. Correct that gate wording instead of building an unnecessarily
large combined fixture, unless a real integration defect later requires one.

The cross-process stale-claim actor is a real child process, but its final
assertion does not isolate the permanent winner as the rejection cause:

- `MoreEquipmentSlotsProductTests.cs:65-170`
- `MoreEquipmentSlotsProductTests.cs:1718-1893`

The parent republishes per-hash completed evidence before releasing the child,
and the child accepts any `InvalidDataException`. The test can therefore stay
green even if winner enforcement regresses. Delete the optional per-hash
evidence before release, or report and assert the exact rejection stage. This
is a test-causality gap, not evidence that the current winner implementation
is broken.

Seven of the nine new Reviews also received implementation narratives,
commit IDs and post-fix PASS blocks after implementation began. Examples are:

- `20260729-0009`, lines 82-96;
- `20260729-0010`, lines 109-123;
- `20260730-0001`, lines 151-182;
- `20260730-0005`, lines 121-155.

That conflicts with `document-governance.md:29-33` and
`docs/reviews/README.md:31-39`. The status was not falsely promoted, and no
second machine gate was created, but the audit narrative was duplicated
across Reviews, the Update, the Batch 6 contract and the Product README.

Do not add another per-correction Review or append later fixes here. Keep all
corrections and focused results in the existing Update, then perform one final
independent acceptance.

### P3: committed range has one whitespace defect

`git diff --check 6f9fd1bd..b3817244` reports an extra blank line at EOF in
`20260729-0007-moreequipment-claim-and-saveloaded-reacceptance-audit.md`.

## Validation

Executed at clean HEAD `b3817244`:

```text
Release Unit build = PASS
  errors = 0
  existing DebugConsole nullable warnings = 10
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host = PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing = PASS
check-product-catalog.ps1 = PASS (27 / 11 / 22 / 48)
check-doc-governance.ps1 = PASS (6031 checks)
check-test-artifact-governance.ps1 = PASS
git diff --check working tree = PASS
git diff --check 6f9fd1bd..HEAD = FAIL (one P3 above)
```

The governance script verifies structural links and authorities; it does not
detect implementation narrative appended to a Review, so the manual
governance finding is not contradicted by its PASS.

Package verification:

```text
two ZIPs = 64,756 bytes each
package SHA-256 =
  1E840AA5CEA1C872AEEEBE3A6FB56A9DD9CB16D1336C99D0AC1F060322BCAC2B
entries = 7
entry DLL SHA-256 =
  E79D5A5DC9DE8E7CFE9396BDC4BBF4AF9F5481537B5CCF9931DF9A1CE2F96194
Advanced reference receipt SHA-256 =
  B9C4F6D1844FB62AC3ABA7CBE3D1E1F31067935EEA3B34A0B5343A27E083B98C
```

The package is deterministic, but deterministic reproduction does not
override the P1 source findings.

## Bounded Next Gate

1. Correct the three P1 paths in one bounded implementation cycle and add
   only their missing cross-state tests.
2. Resolve the cold-probe restart-only decision or invalidate it at explicit
   lifecycle boundaries.
3. Run the same four focused Unit entries, Catalog, document governance and
   deterministic Product packaging once.
4. Obtain one source reacceptance. Only then continue the already-defined
   claim crash/resume, C0/U1/U3/U4 and migrated-save semantic spot check.

Do not run complete Release, L0-L5, GC or long tests for these source
corrections. Do not create a new receipt, checkpoint, checker family or
per-finding Review.
