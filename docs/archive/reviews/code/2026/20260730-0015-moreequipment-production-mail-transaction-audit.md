# MoreEquipmentSlots Production Mail Transaction Audit

Date: 2026-07-30
Status: `recorded / P0=0 P1=1 P2=1 / implementation required / publication blocked`
Audited HEAD: `471cd476b0fa569720c859c19cafac5b2eafef66`

Owning implementation Update:

- [`20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)

Immediate predecessor:

- [`20260730-0014`](20260730-0014-moreequipment-post-mail-closeout-audit.md)

## Scope

This is one independent sub-agent audit round. It checks both the direct
closure of Review `0014` and adjacent current-candidate correctness:

- the strict QA pending-mail reader and its executable malformed-object-graph
  coverage;
- ProductNative backpack/mail placement, gameplay unequip and durable
  recovery transactions;
- the linked Compatibility Host mail reader, working mutation, owner/orphan
  recovery and cold-recovery routes;
- no-save/native-save authority and retry behavior after an observation
  failure;
- the owning Update, Batch 6 contract and current lifecycle projection.

This Review is the only project file written by the audit. The audit did not
change implementation, the owning Update, an index, the Batch 6 contract,
Runtime/Product bytes or a package. It did not install Runtime, launch Doloc
Town, mutate a save, rebuild a candidate, run a complete Release, run GC or
start a second audit round.

## Accepted Direct Correction

Commit `8526b79a` materially closes Review `0014`'s QA-only finding.
`CountPendingMoreEquipmentSlotsMailFromArchiveForFixture` now requires
readable nested mail identity, attachment collection, acceptance state,
reward type, item identity and non-negative count. Missing members, wrong
types, null entries and enumeration failures propagate instead of becoming a
confirmed zero. The QA Unit sends a real reflected object graph through that
reader and includes the requested malformed nested cases.

No new defect was found in that bounded QA parser correction. The remaining
P1 is in a separate production reader and transaction path; the QA fix did not
change or exercise it.

## Findings

### P1-1: production mail evidence can report zero after native mutation, and both owners can preserve or retry the original item

The Product and Compatibility Host compile the same permissive
[`EquipmentSlotNativeMailEvidence.cs`](../../../../../products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotNativeMailEvidence.cs).
`CountUnacceptedDtmapiItemMail` returns `0` when the API/item identity is
unavailable, when `emails` is unreadable, or when nested entries cannot be
classified. It also:

- treats missing `Email.Id` as an unrelated template;
- skips missing/non-enumerable `emailAttaches`;
- treats unreadable `isAccept` as accepted;
- skips unreadable/wrong reward or item identity;
- turns a missing/wrong/negative count into zero.

This is not only a diagnostic false zero. In
[`EquipmentSlotNativePlacement.cs`](../../../../../products/first-party/MoreEquipmentSlots/src/Native/EquipmentSlotNativePlacement.cs),
`PlaceOne` reads the permissive count, invokes native mail, reads the same
permissive count again, and classifies a successful native call with an
observed zero delta as ordinary `Failure`. The native mutation has already
happened at that point.

That ordinary-failure classification is unsafe in several current production
paths:

- ProductNative `RequestUnequip` calls `PlaceOne`; on `Failure` it keeps the
  occupied Working slot. A later unrelated normal native save can therefore
  commit the newly created mail while the Committed sidecar still owns the
  same item.
- Product owner/orphan recovery can record that completed attempt as
  `NativePlacementKind.Failure`. `SaveSaved` still promotes/finalizes the
  journal, and finalization restores failed escrow. If mail was actually
  created, the terminal state is one native mail plus one restored sidecar
  item.
- Compatibility `RecoverEquipmentSlotEntryWorking` returns on the same
  false failure without clearing the slot. Its durable recovery path restores
  the entry and removes the journal after the false failure. Both paths can
  leave the native mail mutation beside the original authority.
- Compatibility cold recovery links the same reader and can retain/retry
  escrow after the same ambiguous post-mutation observation.

Consequently, an unreadable post-send observation is being represented as
“proved no destination” instead of “native mutation outcome unknown.” A
subsequent save can persist duplicate authority; a retry can send a second
copy. Merely changing the reader to throw is not sufficient: if the read fails
after `SendItemAsEmail`, blind retry remains unsafe unless the transaction
retains and reconciles the ambiguous mutation.

The existing focused tests do not cover this cause. The physical Product/Host
fixture only verifies that readable native mail filters unrelated templates,
non-reward attachment types and accepted rewards. The QA nested-negative
matrix exercises a different QA-only helper. The focused Product and cold-Host
routes remain green without a test where native mail succeeds and the
post-mutation observation becomes unreadable.

Minimum correction:

1. replace the production integer/fallback reader with an explicit strict
   observation result; unreadable authority must never equal count zero;
2. require a readable preflight before any backpack/mail mutation;
3. distinguish `FailureBeforeMutation` from
   `NativeMutationOutcomeUnknown`; after native invocation, an unreadable
   post-observation must retain an ambiguity guard or durable escrow and must
   not restore the slot, delete the journal or retry;
4. block `SaveSaving` and another placement while that ambiguity is
   unresolved, except that the ordinary no-save title boundary may discard
   the in-process mutation with native rollback;
5. when observation becomes readable, reconcile the exact before/after
   backpack and mail counts plus native-save fingerprint before choosing
   destination, rollback or retry;
6. apply the same state semantics to Product gameplay, Product durable
   recovery, old-ABI Compatibility working/recovery and Product-v3 cold
   recovery.

Required focused fault injection:

- unreadable preflight invokes no native mail and retains exactly one sidecar
  item;
- native mail succeeds, then the post-read fails: another action sends no
  second mail and normal save is rejected while authority is ambiguous;
- a later exact `+1` mail observation converges to one mail and zero sidecar
  copies;
- an unchanged native preimage/no-save title path converges back to one
  sidecar and zero mail copies;
- the same matrix runs through the real linked Compatibility Host, not only a
  fake placement policy.

This is a player-data consistency P1 and invalidates publication of candidate
bytes containing the permissive reader until the focused correction and
independent reacceptance pass. The retained healthy-shape U3Mail and `161355`
observations remain valid for what they actually observed; they do not cover
this fault window.

### P2-1: Batch 6 still says verified/closed while the canonical owning Update is implemented/open

The canonical Update metadata at audited HEAD is:

```text
Lifecycle Status: implemented
Related Issue State: open
```

Review `0014` and the Update's final implementation section also explicitly
say that no post-fix independent acceptance occurred and that the lifecycle
must remain `implemented/open`.

The current
[`batch6-managed-mod-identity-contract.md`](../../../../architecture/batch6-managed-mod-identity-contract.md)
contradicts that authority in three current-truth locations:

- its top status says all twelve admitted products are `verified/closed`;
- its Advanced classification row says all twelve, including
  MoreEquipmentSlots, are `verified/closed`;
- its current admission-state block labels MoreEquipmentSlots
  `VERIFIED/CLOSED`.

This was already stale after the QA reopen and becomes materially misleading
with P1-1: the frozen Runtime/Product candidate contains the production reader
under review. The architecture projection cannot remain closed while its
canonical Update is open and the candidate requires replacement.

Minimum correction:

1. keep the owning Update and July row `implemented/open` during repair;
2. change only the current Batch 6 projections to
   MoreEquipmentSlots `implemented/acceptance-open` and block publication of
   the affected 0.5.5 candidate;
3. preserve historical game/package evidence as scoped history rather than
   deleting it;
4. after source correction, focused Product/Host transaction tests,
   replacement deterministic bytes and a new independent acceptance, update
   the owning Update first and then project that accepted state into Batch 6.

## Focused Checks

The audit ran these existing focused routes against the exact audited HEAD:

```text
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product: PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host: PASS
```

They confirm that the finding is a missing fault case rather than an existing
red gate. No game, Runtime install, native save, package build or complete
Release was run.

## Verdict

`P0=0 / P1=1 / P2=1`.

The nested QA parser closure is accepted, but the same evidence boundary is
still fail-open in the production Product/Host transaction path. Because a
false zero can occur after native mail mutation and is treated as an ordinary
failure, retry or later native save can produce duplicate item authority.
Batch 6 also overstates the lifecycle relative to the canonical open Update.

Keep MoreEquipmentSlots and the affected publication candidate open. Repair
the production observation/ambiguity transaction, add the Product plus real
Host fault matrix, correct current-state projections, then obtain a fresh
independent acceptance before restoring `verified/closed`.

## Resolution Link

The implementation response is owned by the
[production mail transaction section of Update `20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md#2026-07-30-production-mail-transaction-reopen-and-implementation)
and source/test commit `10e74ed6`. This historical audit verdict is unchanged;
the owning lifecycle remains `implemented/open` pending replacement bytes and
separate independent acceptance.
