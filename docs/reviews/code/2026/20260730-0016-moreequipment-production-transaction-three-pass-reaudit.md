# MoreEquipmentSlots Production Transaction Three-Pass Reaudit

Date: 2026-07-30
Status: `recorded / P0=0 P1=5 deferred P2=0 current / new-product publication blocked`
Audited HEAD: `748b833ae55dc54db2cd5b126db8008b30f9e376`
Audited range: `bb14f75b..748b833a`

Owning implementation Update:

- [`20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)

Immediate predecessor:

- [`20260730-0015`](20260730-0015-moreequipment-production-mail-transaction-audit.md)

## Scope and method

This is one durable Review produced from three independent read-only passes:

1. production ProductNative and linked Compatibility Host transaction review;
2. test-oracle, physical-fixture and fault-model review;
3. lifecycle, candidate and document-provenance review.

The parent pass then traced the findings through the current official reverse
reference, native-save cancellation path, Product transaction coordinator and
Compatibility cold-recovery policy.

The reviewed commits are:

```text
66c21bdf docs(moreequipment): reopen mail observer acceptance
879953d9 test(moreequipment): fail closed on unreadable mail
a5f95b14 docs(moreequipment): close mail observer acceptance
6c653017 docs(moreequipment): reopen nested mail parser
8526b79a test(moreequipment): parse nested mail evidence strictly
471cd476 docs(moreequipment): record nested mail parser fix
d05530dc docs(moreequipment): audit production mail transaction
10e74ed6 fix(moreequipment): reconcile unknown native mail writes
748b833a docs(moreequipment): record production mail transaction correction
```

At initial creation this Review was the audit's only project-file change. It
did not change implementation, tests, Catalog, Runtime/Product bytes, package
or release authority, and it ran no game, Runtime install, native save,
complete Release, L0-L5, GC or long test. The later user disposition is
projected into the owning Updates, planning, API and architecture documents;
those later documentation changes do not alter this audited source range.

## Accepted corrections

The direct QA corrections in `879953d9` and `8526b79a` are effective:

- the top-level QA pending-mail observer no longer equates an unavailable
  archive, farm, email manager or email collection with zero mail;
- the QA nested reader rejects strings used as collections, null entries,
  missing or wrong members, wrong reward type and invalid item counts;
- the QA Unit passes real reflected object graphs through the QA reader.

The main direction of `10e74ed6` is also sound:

- outbound placement now obtains strict backpack/mail preflight evidence;
- an invocation or post-read ambiguity is represented as
  `NativeMutationOutcomeUnknown` instead of ordinary failure;
- Product gameplay, Product durable recovery and the linked Compatibility
  Host retain a guard or journal rather than blindly replaying placement;
- unresolved ambiguity blocks the normal `SaveSaving` route;
- exact `+1` backpack or mail observations can converge without a second
  native placement call;
- title cleanup removes in-process guards, preserving the game's ordinary
  no-save rollback semantics.

The current lifecycle projection in the MoreEquipmentSlots Update and Batch 6
contract correctly remains `implemented / acceptance-open`; the affected
Runtime/Product candidate is superseded and Catalog still blocks publication.
Those facts must remain open while the findings below are corrected.

## Findings

### P1-1: the production mail reader still converts some unreadable authority into zero

[`EquipmentSlotNativeMailEvidence.cs`](../../../../products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotNativeMailEvidence.cs)
does not yet implement the same strict rules as the accepted QA oracle:

- `RequireEnumerable` accepts `string` because `string` implements
  `IEnumerable`; an empty `emailManager.emails` string executes zero
  iterations and returns `0`, while a string `emailAttaches` collection
  iterates characters which are skipped as unrelated attachments;
- an unaccepted exact `DolocTown.EmailAttachReward` whose reward is not an
  exact `DolocTown.RewardItem` is skipped instead of rejected as unreadable.

A read-only reflection probe against the compiled production method reproduced
the first case:

```text
emailManager.emails = ""
CountUnacceptedDtmapiItemMail(...) => 0
```

This false zero can still enter the exact duplication path from Review `0015`:
the native send may have happened, while Product or Host retains the sidecar
authority and later permits retry or save.

Minimum correction:

- reject `string` in every production collection reader;
- reject a wrong reward type for an unaccepted exact reward attachment;
- run the malformed string and wrong-reward cases through the real linked
  Product/Host fixture, not only the separate QA helper.

### P1-2: incoming `CostItem` withdrawal still trusts a boolean instead of the native before/after state

The newly hardened sidecar-to-native placement is not mirrored by the
native-to-sidecar equip path:

- Product `EquipmentSlotNativePlacement.TryWithdrawOne` delegates directly to
  `DolocAPI.CostItem`;
- Product gameplay equip accepts the returned boolean before placing the item
  into Working state;
- Product durable replacement reads an after count, but records
  `false + exact -1` as a failed incoming withdrawal and still permits the
  enclosing native save;
- the legacy public ABI path in
  `EquipmentSlotsCompatibilityService.TryCostNativeBackpackItem` also trusts
  only the boolean.

Under the same fault model already adopted for outbound native calls:

- false or an exception after mutation can remove the backpack item without
  recording sidecar ownership, and a later native save makes the loss
  permanent;
- true without the exact mutation can place the same logical item into
  sidecar state while it remains in the backpack.

The current official implementation normally returns a truthful boolean, so
this is not a claimed common-player reproduction. It is nevertheless a P1
transaction defect because the implementation now explicitly promises safe
handling of invocation and observation faults, and the failure can be
committed as item loss or duplication.

Minimum correction:

- capture a strict backpack count before and after withdrawal;
- treat exact `-1` as the authoritative success observation; the boolean is
  supporting evidence, not sole authority;
- treat every other delta, unreadable observation or invocation fault as an
  outcome-unknown withdrawal which blocks save and replay;
- cover empty/occupied/same-item replacement plus
  false-after-mutation, true-without-mutation and throw-after-mutation in both
  Product and the real Compatibility Host.

### P1-3: missing or unreadable native-save identity can compare equal and masquerade as an exact preimage

Compatibility
`EquipmentSlotsCompatibilityTransactions.GetNativeSaveFingerprint` catches
every failure and returns `string.Empty`. The Working guard and durable
unknown-journal paths then use plain string equality, so two failed reads
produce `empty == empty` and are accepted as an unchanged exact save preimage.
Journal creation also accepts this empty result.

Product has the equivalent issue in a different representation:
`EquipmentSlotNativeCommitFingerprint.Compute(null)` returns the fixed
`native-v2|current=missing|prev=missing|bak=missing` sentinel. New guards can
therefore compare two missing-path sentinels as exact authority.

This can authorize rollback, retry or reconciliation without proof of the
native commit boundary. A changed or already committed save can consequently
be paired with the wrong sidecar decision.

Minimum correction:

- make path resolution, file observation and hashing return an explicit
  unreadable result or throw;
- reject empty and all-missing fingerprints before native mutation and before
  every reconciliation decision;
- require a real exact current/prev/bak observation;
- test null path, path-method failure, hash I/O failure and an archive change
  between two unreadable observations.

### P1-4: Compatibility cold journal recovery still treats coarse count growth as proof of this transaction

`ReconcileEquipmentSlotJournal` still feeds a permissive backpack count into
the old `EquipmentSlotJournalRecoveryPolicy`. That policy:

- finalizes `NativeCommitted` and `Committed` without checking the stored
  post-save fingerprint or exact destination counts;
- finalizes a prepared journal after any save-fingerprint change when either
  backpack or mail is merely greater than its baseline;
- does not require exact `+1`, the other channel to remain unchanged, or the
  observed destination to match `journal.Placement`.

An unrelated gain of the same item, `+2`, simultaneous backpack and mail
growth, or a rolled-back native archive can therefore delete escrow as though
this exact placement committed. `CountNativeBackpackItem` also returns zero
when its native authority cannot be read.

Minimum correction:

- use strict observations in cold recovery;
- prepared recovery must resolve the exact expected destination and count;
- committed recovery must match the exact post-save fingerprint and terminal
  destination;
- every mixed, excess, unreadable or rolled-back state remains fail-closed;
- add focused `+2`, dual-channel `+1`, unrelated same-item gain,
  `CountItem` failure and committed-fingerprint rollback cases.

### P1-5: delayed count-only reconciliation is not isolated from unrelated same-item gameplay

An outcome-unknown exception stores item identity and aggregate backpack/mail
baselines. The later resolver decides solely from aggregate count deltas.
Although DTMAPI retry and `SaveSaving` are blocked, ordinary game actions are
not: the player may accept mail, consume or move the item, or obtain another
item with the same ID before the authority becomes readable.

Examples:

- native send succeeded, then the player accepts and consumes the item;
  counts can return to baseline and the resolver can retain the sidecar copy;
- native send did not occur, then the player independently obtains the same
  item; an exact `+1` can make the resolver clear the original sidecar item.

The native-save fingerprint covers disk commit state, not these unsaved
in-memory same-item mutations, so it does not disambiguate them.

Minimum safe correction:

- permit only an immediate, bounded same-call re-observation while no
  unrelated gameplay can intervene;
- if ambiguity remains, keep the session non-committable and require the
  ordinary NoNativeSave title/process rollback, unless a true native
  instance/transaction identity fence is implemented;
- test accept-plus-consume/move and an independent same-item gain before the
  next `SaveSaving`.

### P2-1: the canonical Runtime 0.5.5 prerelease Update still describes a superseded candidate as verified

The Batch 6 contract correctly states that `10e74ed6` changed mandatory
Compatibility Host bytes and superseded the previous Runtime candidate.
However,
[`20260727-0001-dtmapi-055-prerelease-route.md`](../../../updates/2026/20260727-0001-dtmapi-055-prerelease-route.md)
and its July ledger row still project:

```text
Lifecycle Status: verified
Runtime Validation: passed
Related Issue State: closed
```

The old complete Release remains valid historical evidence for its exact old
bytes, but it is not acceptance evidence for the current source. Catalog still
has an active release stop, so this truth drift has not yet become an upload
authorization defect.

Minimum correction:

- reopen the Runtime prerelease Update and monthly row as
  `implemented / partial / open`;
- append a dated note that the old Release applies only to the superseded
  candidate;
- restore `verified/closed` only after corrected replacement bytes and their
  final acceptance.

## Non-blocking documentation cleanup

- Review `20260730-0013` is historically accurate for its audited HEAD, but
  should append a short resolution link to Reviews `0014` and `0015`.
- The dated lightweight and 0.5.5 planning roadmaps still describe all twelve
  products and 0.5.5 as closed. They should route readers to the current Batch
  6 contract and prerelease Update instead of presenting that old snapshot as
  current truth.

These are P3 navigation/interpretation issues and do not require rewriting
historical analysis.

## Source-weight and candidate impact

Commit `10e74ed6` is not a test-only correction:

```text
all changed files:       +2333 / -171
Product production:       +833 / -116
mandatory Host source:    +581 /  -39
```

The Compatibility Host increase is part of mandatory Runtime source, and the
Product increase changes the separately shipped Mod. The previous Runtime and
MoreEquipmentSlots candidate bytes are therefore invalid for current
publication. Correctness should be closed first; only then should replacement
bytes and default-loaded cost be measured. This Review does not treat net line
growth as proof of runtime cost, but it also does not misclassify it as QA-only
weight.

## Validation

The independent test-oracle pass ran:

```text
DTMAPI.UnitTests Release build: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing: PASS
DTMAPI.QaUnitTests Release build: PASS, 0 warnings / 0 errors
DTMAPI.QaUnitTests: PASS
test-game-smoke-save-modes.ps1 under PowerShell 7: PASS
test-game-smoke-save-modes.ps1 under Windows PowerShell 5.1: PASS
Windows PowerShell 5.1 parser for runner and save-mode script: PASS
git diff --check: PASS
```

The parent pass additionally ran:

```text
check-product-catalog.ps1: PASS (27 products / 11 public / 22 Workshop / 48 API rows)
check-doc-governance.ps1 -Quiet: PASS
check-test-artifact-governance.ps1: PASS
git diff --check: PASS
```

The green checks prove the current intended paths and scripts still work. The
reflection reproduction and missing physical-fixture cases prove that they do
not cover all production fault states, so they do not override the findings.

## Verdict and next boundary

Initial technical verdict before the user disposition:
`P0=0 / P1=5 / P2=1`.

Keep MoreEquipmentSlots and Runtime 0.5.5 publication open. Do not rebuild a
final candidate, start game acceptance or run a complete Release yet.

Use one bounded implementation pass:

1. close the strict mail-reader tail;
2. give incoming withdrawal the same observed outcome-unknown semantics as
   outbound placement;
3. reject missing fingerprint authority;
4. replace coarse cold-journal recovery with exact evidence;
5. prevent delayed count-only reconciliation after unrelated gameplay;
6. add the Product and real-Host fault cases above;
7. run only focused Unit/QA/save-mode and PowerShell 5.1 checks;
8. obtain a fresh independent source acceptance.

After that source acceptance, rebuild deterministic Runtime/Product
candidates, update the canonical lifecycle facts, and run the smallest
remaining compatibility/acceptance boundary. One clean complete Release
belongs only at the final frozen candidate, not after each correction.

## 2026-07-30 User disposition

The user chose not to continue this correction chain for the 0.5.5 release.
The new MoreEquipmentSlots `1.0.0` candidate, its Product-v3 migration,
publication acceptance and Steam update are deferred. The five P1 findings
above remain open against that future product/Host design; deferral is not a
technical closure and does not convert the adversarial transaction model into
accepted behavior.

Workshop item `3744059735` therefore remains at the retained
`0.3.1-dtmapi` artifact. Runtime 0.5.5 keeps the exact frozen
`IEquipmentSlotsApi` ABI and demand-loaded Compatibility Host only to support
that already-published consumer. Its release gate is one exact-artifact,
ordinary-behavior compatibility smoke; that smoke does not accept the new
Product, migration, or the fault states recorded in this Review.

The discussed generic protected-storage capability is also paused. There is
no admitted or promised `IProtectedStorageApi` for 0.5.5. If the idea is
resumed after release, it must first establish one platform data authority and
one production adapter rather than generalizing the current duplicated
Product/Compatibility readers and state machines.

This disposition closes the documentation-truth P2 by selecting and naming
the unchanged `f96c9cc6` artifact instead of treating later Host/Product
source as its replacement. It does not close any of the five implementation
P1 findings.

The practical conclusion is that “sending one item by mail” was not itself
the large feature. The new implementation accumulated several independent
authority systems at once: a new fixed-three ProductNative product, the old
Workshop ABI and Compatibility Host, Working/Committed native-save semantics,
flat-to-nested sidecar migration and cold recovery, plus QA readers for each
generation. The five P1 findings split across cost/removal, missing
fingerprint authority, cold recovery and delayed reconciliation rather than
five repairs to one mail parser. That layering explains the repeated rounds,
but it does not justify publishing the new product or generalizing it into a
shared API for 0.5.5.

The exact old Workshop compatibility result is owned by the
[MoreEquipmentSlots Update](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)
and active smoke matrix. It validates only the retained `0.3.1-dtmapi`
ordinary path; it does not supersede the deferred P1 findings above.
