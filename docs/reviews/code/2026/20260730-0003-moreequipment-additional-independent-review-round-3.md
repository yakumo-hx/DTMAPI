# 20260730-0003: MoreEquipmentSlots Additional Independent Review Round 3

Status: `recorded / P0=0 P1=3 P2=2 / correction required`

## Scope

This is round 3 of the five additional independent subagent reviews requested
by the user. The read-only audit used clean HEAD
`bed6722441aced4e8dd46e9a4cebcad6a5ff492b` and deliberately challenged the
round-2 correction rather than treating it as accepted. It reviewed:

- pending versus completed winner semantics;
- permanent terminal artifacts versus incomplete migration residue;
- ordinary empty state for a different save;
- mandatory proxy/Compatibility Host demand and owner suppression;
- true-empty filesystem side effects;
- tests and fact-document claims.

No file was modified, no game or Runtime was launched, no save was changed and
no package or complete Release operation was run.

## Result

```text
P0 = 0
P1 = 3
P2 = 2
MoreEquipmentSlots = implemented / acceptance-open
```

## Findings

### P1-1: pending winner still bypasses Product census

`EnsurePreSchemaGlobalClaimCore` sees an existing `winner.json`, validates only
an exact pending winner and source state, then returns. The Product census is
still confined to the no-winner branch. Completion also skips the census for
any existing winner without distinguishing pending from completed.

Reproduction:

1. A exits at `after-preschema-claim-publish`, leaving pending winner/evidence
   and active global but no Product;
2. an old process or external recovery publishes B's canonical live or
   eligible-previous PreSchema Product;
3. A resumes, writes backup/Product/archive, and completes beside B.

Correction gate:

- pending winner is not terminal authority and must rerun canonical Product
  census before any Product/backup/archive mutation;
- completion reached from an already published Product must likewise census
  while the winner remains pending;
- cover late live and eligible previous, same/different source, with all
  authority bytes unchanged on rejection.

### P1-2: permanent terminal artifacts keep waking Compatibility Host

A successful migration intentionally keeps its deterministic global archive,
scoped legacy backup and completed winner/evidence. Round 2 made archive and
backup unconditional cold candidates. Their current path parser cannot assign
an owner, and the Host classifies them as unresolved residue before Product
owner suppression.

A normal loaded Product therefore wakes the dormant Host on first SaveLoaded,
keeps it resident and writes `orphan-recovery-blocked` for healthy terminal
evidence. The round-2 cold test explicitly freezes this wrong expectation.

Correction gate:

- distinguish durable terminal evidence from incomplete orphan residue;
- derive the artifact owner and apply loaded-owner suppression in the mandatory
  proxy;
- when exact current Product authority resolves the terminal artifact, a
  resident Host must not report it as orphan failure;
- Product loaded + exact Product/archive/backup/completed evidence requires
  zero cold candidate/demand/blocked status;
- Product absent with genuinely incomplete residue must still demand explicit
  diagnosis.

### P1-3: cross-save empty guard blocks legitimate new saves forever

The empty guard now rejects any global archive, any-scope claim/Product and any
canonical backup under the whole scoped root. After save A completes migration
with exact winner/Product/archive/backup, a never-used save B cannot create its
ordinary in-memory empty state.

This changes “the unscoped historical global may be adopted once” into “only
one save in the game root may use the product.” Identity-bearing Product and
backup data for A do not belong to B and must neither be adopted nor block B.

Correction gate:

- exact completed A authority plus new B permits B's in-memory empty state;
- other-scope identity-bearing Product/backup remains unchanged and is not
  adopted;
- current-scope pending/incomplete authority and genuinely unbound
  archive/capture remain fail closed;
- completed winner validation must remain exact before it can relax
  cross-scope empty blocking.

### P2-1: true-empty inspection leaves operation-lock artifacts

The guard creates the claim directory and `operation.lock` even when the
configuration is truly empty. Current negative control checks only that no
Product file was written.

Correction gate: snapshot the relevant tree and remove a newly created empty
lock/directory after the cross-process check, without deleting a pre-existing
or concurrently held artifact.

### P2-2: tests and fact documents encode the wrong terminal semantics

README, Update and Batch 6 wording says all archive/backup/cross-scope residue
correctly blocks empty state and routes Host demand. The focused tests require
the same behavior. Overall `implemented/acceptance-open` and “old package
superseded/no replacement package” remain correct.

## Confirmed Boundaries

- Production SaveLoaded remains single feature-fanout dispatch and SaveSaved
  remains a single EquipmentSlots bridge notification.
- Completed authority validation still requires active global absence.
- No-winner existing pending evidence now enters the operation lock and
  Product census.
- Exact winner-first handling of optional loser evidence remains intact.
- Catalog projection passes.

## Executed Checks

```text
HEAD = bed6722441aced4e8dd46e9a4cebcad6a5ff492b
DTMAPI_UNIT_TEST_FOCUS=moreequipment-product = PASS
DTMAPI_UNIT_TEST_FOCUS=moreequipment-cold-host = PASS
check-product-catalog.ps1 = PASS (27 / 11 / 22 / 48)
git status = clean
```

The cross-scope and terminal-residue tests were passing the wrong semantics
and are not closure evidence. The implementation lifecycle remains owned by
Update `20260723-0008`; later correction references must not be rewritten as
this review's independent acceptance.
