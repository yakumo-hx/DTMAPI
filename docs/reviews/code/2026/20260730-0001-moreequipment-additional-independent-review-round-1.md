# 20260730-0001: MoreEquipmentSlots Additional Independent Review Round 1

Status: `recorded / P0=0 P1=3 P2=1 / correction required`

## Scope

This is round 1 of the five additional independent subagent reviews requested
by the user. The read-only audit used clean HEAD
`14b70114625a6f3d7d768d8fd5ab43c916ed4fe2` and reviewed:

- source-independent winner and cross-process operation lock;
- pending/completed evidence and interrupted transitions;
- historical live/eligible-previous Product authorities;
- global capture/archive crash recovery and empty-state creation;
- Product, Compatibility Host, lifecycle, Catalog, README, Batch 6 contract
  and Update truth boundaries.

The reviewer did not modify files, launch the game, install Runtime, mutate a
save or run the complete Release suite.

## Result

```text
P0 = 0
P1 = 3
P2 = 1
MoreEquipmentSlots = implemented / acceptance-open
```

The existing four focused entries and Catalog passed, but they do not cover
the three authority orders below.

## Findings

### P1-1: late loser evidence can block the completed winner

`CompletePreSchemaGlobalClaim` reconciles every transition and validates every
per-hash JSON against the current caller before it reads `winner.json`.
`ValidatePendingPreSchemaGlobalClaims` and
`ReconcilePreSchemaClaimTransitions` likewise apply the current owner to the
whole directory.

An old process can therefore publish a late different-hash claim, or occupy an
empty same-hash evidence path after the winner was published but before its
evidence was published, and exit. The legitimate winner then fails on that
loser artifact before reaching its own completed singleton. A late transition
has the same effect.

Correction gate:

- classify `winner.json` first while holding the operation lock;
- if it is the exact current winner, retain but do not adopt or delete
  non-winner evidence and transitions;
- if it does not match the caller, fail the caller;
- use the whole directory as conflict input only while no winner exists;
- cover a real late different-hash old actor and the same-hash
  winner-published/evidence-absent window.

### P1-2: pre-winner census omits eligible previous and different-hash Product

`EnsureNoConflictingPublishedPreSchemaProduct` enumerates only the live
Product basename. It does not inspect `.json.previous`, although the Product
load contract accepts an eligible previous, and it ignores otherwise valid
PreSchema Product authorities whose source hash differs from the current
candidate.

With no winner/evidence, Save A can have only an eligible stamped previous and
archive S1 while Save B has a live stamped Product and archive S2. Whichever
save loads first can establish the singleton. The existing recoverable
authority is therefore excluded by process order instead of a fail-closed
authority decision.

Correction gate:

- before creating a winner, enumerate canonical scoped live and previous
  candidates;
- apply each candidate's live/previous eligibility, scope, revision and stamp
  rules;
- any other eligible PreSchema Product, regardless of source hash, must block
  automatic winner publication;
- cover both load orders and a real two-process order while retaining all
  candidate bytes.

### P1-3: capture-only residue permits empty Product and has no Host demand

Capture recovery is entered only from an already loaded Product migration
stamp. If Product, previous and global are all absent,
`EnsureNoPendingPreSchemaGlobalClaimBeforeEmpty` inspects only the claim
directory and then permits `CreateEmpty`. It does not inspect
`<global>.migration-capture-*`.

This is reachable for identity-bearing `GlobalFlat`, which has no pre-schema
claim: exit after global-to-capture rename, then lose or isolate the Product.
The next Product load can create empty state. With Product absent, the
mandatory Compatibility proxy also enumerates only live and previous
sidecars, so the capture cannot demand the recovery Host.

Correction gate:

- any canonical capture residue must forbid empty Product creation when no
  Product stamp can prove ownership;
- do not auto-adopt a capture without exact Product scope/stamp;
- route capture/claim-transition/winner residue to Compatibility orphan
  recovery demand or an explicit fail-closed diagnostic;
- cover Product capture-only empty prevention, Product-absent Host demand and
  a true-empty negative control.

### P2-1: specific closure wording exceeds the tested boundary

The owning Update, product README and Batch 6 contract describe the final
winner/transition/capture correction without disclosing the late-old-evidence,
pre-winner eligible-previous census and capture-only empty/demand gaps.
Existing historical-winner tests remove old evidence, capture tests retain the
Product, and previous tests cover only the current Product fallback.

The overall `implemented/acceptance-open` status and existing execution/package
provenance remain correct. Correction must narrow the specific claims until
the source and focused matrix close these paths.

## Confirmed Boundaries

- The operation lock gives exactly one winner for cooperating new processes
  without old evidence; its OS handle is released after process exit.
- Current-owner transition capture, create collision and completed-publish
  restart retain bytes and fail closed. This finding is classification order,
  not a return of the old `File.Replace` defect.
- One exact capture plus exact Product/backup recovers after real exit 87;
  mismatch, multiple capture and replacement-global cases remain fail closed.
- `GlobalFlat` now rejects exact and different global recreation after
  archival.
- Current Product live/eligible-previous validation is exact; the missing
  boundary is the census of other Product authorities before winner creation.
- Production save lifecycle fanout and normal resident/cold Host live/previous
  routes showed no new regression.

## Executed Checks

```text
HEAD = 14b70114625a6f3d7d768d8fd5ab43c916ed4fe2
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host = PASS
DTMAPI_UNIT_TEST_FOCUS=compatibility-host = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-acceptance-routing = PASS
check-product-catalog.ps1 = PASS (27 / 11 / 22 / 48)
```

The implementation lifecycle remains owned by Update `20260723-0008`.
Post-review corrections and implementation-side checks may be linked later,
but must not be rewritten as this review's independent acceptance.
