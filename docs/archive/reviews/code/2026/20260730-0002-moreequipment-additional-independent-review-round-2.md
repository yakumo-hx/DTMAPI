# 20260730-0002: MoreEquipmentSlots Additional Independent Review Round 2

Status: `recorded / P0=0 P1=2 P2=1 / correction required`

## Scope

This is round 2 of the five additional independent subagent reviews requested
by the user. The read-only audit used clean HEAD
`65aa71ac7723026d14da8f6a8979e7af7f481d0a`, including round-1 correction
`0ffb6a70`, and deliberately searched beyond the prior findings:

- pre-winner entry order before any Product write;
- pending evidence without a singleton;
- empty-state creation with historical Product or migration residue;
- Product/Compatibility cold demand and diagnostic routing;
- authority bytes, package provenance and fact-document claims.

The review process returned the exact findings and reproduction orders below,
then stalled while formatting its final response and was stopped. It did not
modify files, launch the game, install Runtime, mutate a save or run complete
Release.

## Result

```text
P0 = 0
P1 = 2
P2 = 1
MoreEquipmentSlots = implemented / acceptance-open
```

## Findings

### P1-1: existing pending evidence bypasses the pre-winner gate

At `EquipmentSlotDocumentStore.Migration.cs:813-827`,
`EnsurePreSchemaGlobalClaimCore` sees the current per-hash `claimPath`, checks
that `winner.json` is absent, validates only that claim and returns. It does
not acquire the operation lock, run the new canonical Product census or
publish/validate a winner.

Reproduction:

1. retain a valid pending same-hash claim without `winner.json`;
2. retain another canonical eligible Product authority, either live with a
   different source hash or through eligible `.previous`;
3. load the pending-claim owner against the still-active global.

The caller proceeds through backup and `WriteMigratedProduct`, and can archive
the global, before the later completion census detects the conflict. The gate
therefore fails only after it has published a second Product authority.

Correction gate:

- treat an existing per-hash claim only as evidence;
- still acquire the operation lock, classify winner first, reconcile/validate
  the no-winner directory and run the full Product census;
- publish and validate the singleton before returning to any Product write;
- prove that pending-claim/no-winner plus another live or eligible-previous
  Product changes no Product/global/archive/backup bytes.

### P1-2: empty-state creation ignores Product, archive and backup authority

`LoadOrMigrate` reaches
`EnsureNoPendingPreSchemaGlobalClaimBeforeEmpty` and then `CreateEmpty` when
the current Product, previous and global are missing. The guard now checks
capture, transition and claim residue, but it does not census:

- another canonical scoped live/eligible-previous Product carrying a
  PreSchemaGlobal migration stamp;
- deterministic
  `equipment-slots-<owner>.json.migrated-product-v3-<hash>` archives;
- canonical scoped `.legacy-migrations/<hash>.flat.json` backups.

This permits a new in-memory empty document and later persistence of an
unstamped empty Product beside a historical item-bearing authority. A later
backfill ignores that unstamped Product, so the ambiguity becomes durable.
Archive-only data cannot be assigned to a save automatically, but it must not
be silently treated as true empty. A current-scope backup carries an even
stronger scoped recovery binding.

The mandatory proxy and Compatibility Host scan enumerate capture and claim
residue after round 1, but still omit deterministic archives and scoped backup
residue.

Correction gate:

- run the empty-state census under the same operation lock;
- block empty creation for any eligible PreSchema Product, deterministic
  archive or canonical scoped backup residue;
- do not auto-adopt an unscoped archive;
- route archive/backup residue to explicit dormant Host demand/diagnostic;
- cover live, eligible previous, archive-only, current-scope backup-only,
  other-scope backup, later-write absence and true-empty negative controls.

### P2-1: round-1 closure wording omits remaining empty/pre-winner paths

The Update, README and Batch 6 contract correctly keep the product
`implemented/acceptance-open`, but their round-1 wording says pre-winner
census and migration-residue empty prevention are corrected without
disclosing the pending-evidence early return and archive/backup-only paths.
The superseded package status remains correct.

## Confirmed Boundaries

- Round-1 exact-winner-first completion remains effective for late
  different-hash and same-hash optional evidence already covered by the
  focused tests.
- Capture-only state now blocks Product empty creation and capture/claim
  residue can wake the mandatory proxy.
- The new Product census itself correctly recognizes canonical live and
  eligible previous authorities when it is actually reached.
- No new defect was established in normal save lifecycle fanout, Product
  live/previous validation or exact capture restart.

## Validation Boundary

This review was source-order analysis. The previous exact-HEAD focused checks
remain implementation evidence for round 1, not proof of the newly identified
orders. No additional game, Runtime, package, save or complete Release command
was claimed by the reviewer.

The implementation lifecycle remains owned by Update `20260723-0008`.
Post-review correction references must not be rewritten as this review's
independent acceptance.
