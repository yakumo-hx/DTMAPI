# MoreEquipmentSlots Post-Mail Closeout Audit

Date: 2026-07-30
Status: `recorded / P0=0 P1=0 P2=1 / retained player evidence unchanged / publication unchanged`
Audited HEAD: `a5f95b14cc2323ef300f61543ac9f43100110f38`

Owning implementation Update:

- [`20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md)

Prior Reviews:

- [`20260730-0011`](20260730-0011-moreequipment-final-cold-oracle-route-reaudit.md)
- [`20260730-0012`](20260730-0012-moreequipment-generic-cold-oracle-fix-reaudit.md)
- [`20260730-0013`](20260730-0013-moreequipment-mail-observer-final-acceptance.md)

## Scope

This is the single user-requested post-closeout sub-agent audit. It checks more
than the previously known top-level pending-mail correction:

- the complete production-QA call chain from the cold observer and transition
  fixtures into `CountPendingMoreEquipmentSlotsMail`;
- reflection and enumeration failure semantics inside the mail collection;
- backpack, mail and sidecar conservation;
- the causal strength of the new QA Unit and runner/source-shape checks;
- Update lifecycle ordering, Review resolution/provenance, the July ledger and
  evidence-allowlist state;
- adjacent U1, U3, U4 and MigratedSave QA paths.

This Review is the only project file written by the audit. The audit did not
change implementation, the owning Update, an existing Review, the allowlist,
Runtime or Product bytes. It did not install Runtime, launch Doloc Town, mutate
a save, run a complete Release, freeze a package, run GC or start a second
review round.

## Accepted Corrections

The direct findings from Review `0012` are materially fixed:

- `archiveHandle`, `farmData`, `emailManager` and `emails` now fail closed
  when null, missing or non-enumerable;
- a string is not accepted merely because it implements `IEnumerable`;
- enumerator and reflected-property exceptions propagate to the fixture's
  outer failure path rather than being converted to zero;
- the cold-distribution policy rejects button and shield mail before its
  logical-total comparison;
- empty-Committed, shield-only and two-item projections execute the same
  C# policy used by the cold observer;
- the generic item equations remain correctly derived from the supplied
  backpack and Committed-slot baseline.

The lifecycle order is also compliant for that correction. Commit `66c21bdf`
returned the Update and July ledger to `implemented/open`; implementation
commit `879953d9` followed; Review `0013` independently audited that state; only
post-audit commit `a5f95b14` restored `verified/closed`. Review `0011` retains
its historical text and now has a short resolution link. Repository history
also confirms the recorded ownership:

- `c2c215e4` owns Review `0011`, the generic QA/runner correction and its
  allowlist refresh;
- `bb14f75b` changed only the owning Update;
- `66c21bdf` owns Review `0012`, the lifecycle reopen and its allowlist
  refresh;
- `879953d9` owns the pending-mail implementation/tests;
- `a5f95b14` owns Review `0013` and the post-audit lifecycle close.

The Update and July ledger metadata match at audited HEAD. The current
allowlist check is green. No provenance or lifecycle P0/P1/P2 was found.

## Finding

### P2-1: item-mail entry parsing still fails open below the enumerable collection

The correction stops at the `emails` collection boundary. Once that collection
is enumerable,
[`MoreEquipmentSlotsTransitionFixtureCase.cs`](../../../src/DTMAPI.GameBridge.DolocTown.QA/Scenarios/Fixtures/MoreEquipmentSlotsTransitionFixtureCase.cs)
still translates partially unreadable entries into an observed zero:

- line 1057 reads `Email.Id` with an empty-string fallback, so a null/missing
  identity is silently treated as unrelated mail;
- lines 1067-1072 skip a `send_item_template` entry when
  `emailAttaches` is missing, null or non-enumerable;
- lines 1078-1086 read `isAccept` with fallback `true`, so an unreadable
  acceptance state is silently treated as already accepted;
- lines 1090-1103 skip an unreadable `reward` or `itemName`;
- lines 1107-1113 read a missing/non-readable `itemCount` as zero and clamp it
  with `Math.Max`.

`ReadMember` returns null when a member is absent, while
`ReadStringMember`, `ReadBoolMember` and `ReadIntMember` apply exactly those
fallbacks. Only a getter or enumerator which actually throws reaches the outer
fixture failure. Therefore Review `0013`'s conclusion is too broad: the
top-level authority is fail-closed, but an enumerable mail authority containing
a partially unreadable item-mail entry can still return `0`.

The new negative tests do not close this cause:

- button-mail and shield-mail tests inject integer counts directly into
  `ValidateMoreEquipmentSlotsColdItemDistribution`; they never pass a native
  mail object graph through the reader;
- unreadable-chain tests stop at `archiveHandle`, `farmData`, `emailManager`
  or the `emails` collection itself;
- the readable control uses an empty collection;
- `test-game-smoke-save-modes.ps1` freezes source composition, but does not
  execute a parameterized mail graph or the dynamic runner matcher.

Thus the current suite can remain green while every nested fallback above is
present. This is a QA evidence-integrity defect, not evidence that the current
Product duplicated an item. The accepted `161355` two-item observation and
the U3Mail positive path used the current native shape, so this finding does
not retroactively invalidate them.

The impact is broader than the final cold observer because the same reader is
used by transition preparation/terminal checks and the per-item equations for
U1, U3, U4 and MigratedSave. A future reuse against an unreadable nested mail
entry could claim zero mail and accept an incomplete conservation equation.

Minimal correction:

1. use one strict item-mail parser which distinguishes an unrelated, readable
   mail/attachment from an unreadable target-relevant entry;
2. require a readable `Id`; for `send_item_template`, require enumerable
   `emailAttaches`; for an exact `DolocTown.EmailAttachReward`, require a
   readable boolean `isAccept`; for an unaccepted attachment, require an exact
   `DolocTown.RewardItem`, readable `itemName` and non-negative integer
   `itemCount`;
3. continue to ignore other *readable* templates and non-item attachment
   types, but throw on missing/invalid members needed to classify target item
   mail;
4. add object-graph tests which execute the reader for readable empty,
   button-mail, shield-mail, missing `Id`, missing/non-enumerable attachments,
   unreadable acceptance/reward/item/count, and a throwing enumerable;
5. keep both cold and transition fixtures on that same helper, then rerun only
   the affected QA build/Unit, acceptance-routing, save-mode/parser and
   document/allowlist checks.

No game rerun, Runtime/Product refreeze or complete Release is required for
this QA-only correction.

## Adjacent Boundaries

- The C# cold-item policy and PowerShell matcher currently parse the same item
  field, use case-sensitive identity and preserve multiplicity. No mismatch was
  found.
- The PowerShell dynamic matcher remains source-shape rather than
  parameterized executable coverage. Reviews `0012` and `0013` already state
  that limitation truthfully. Extracting a pure matcher builder would improve
  assurance, but no current calculation defect was found and it is not a
  separate blocker here.
- ProductNative and the Compatibility Host use a separate shared
  `EquipmentSlotNativeMailEvidence` helper with permissive zero fallbacks.
  That code predates this QA correction. This audit confirmed the source risk
  but did not reproduce an exact-current-build path where native mail succeeds
  while its post-send evidence is unreadable, so it is not promoted to a
  production P1/P2 finding in this bounded Review. It should receive focused
  fault injection before a future Product/Host rebuild rather than being
  silently treated as covered by the QA fix.

## Focused Validation

Checks run against audited HEAD before this Review was added:

```text
DTMAPI.GameBridge.DolocTown.QA / DTMAPI.QaUnitTests Release build:
  PASS, 0 warnings, 0 errors
DTMAPI.QaUnitTests:
  PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing:
  PASS
test-game-smoke-save-modes.ps1:
  PASS
build-evidence-retention-allowlist.ps1 -Check:
  PASS (490 source files / 896 smoke runs / 62 Runtime identities /
        19 durable roots)
```

These green results confirm the recorded existing coverage boundary; they do
not contradict P2-1 because none executes a partially unreadable nested mail
entry.

## Verdict

The prior top-level pending-mail correction, generic item totals, retained
player evidence and lifecycle/provenance closeout are accepted. No new P0 or
P1 was found.

One new QA-path P2 remains: nested item-mail classification can still turn
unreadable evidence into zero, and the focused negatives bypass that reader.
Before implementing the correction, return the owning Update and July row to
`implemented/open`; restore `verified/closed` only after the requested
post-fix independent acceptance. The product/package publication authority
remains unchanged.

## Resolution

Implemented by commit `8526b79a` and recorded in
[Update `20260723-0008`](../../../updates/2026/20260723-0008-more-equipment-slots-eighth-advanced-product.md).
The Update remains `implemented/open`; this single audit round did not perform
post-fix independent acceptance.
